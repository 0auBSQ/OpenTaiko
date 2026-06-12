using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace OpenTaiko {
	// ── LuaNetworking - OpenTaiko's P2P online core (src/Lua/Networking) ───────────────────────────────────
	// The room CREATOR is the server/relay (a TCP listener);
	// joiners connect to it (a star topology). On top of that sits a rotating "HOST ROLE" - the lobby authority
	// that picks the track/map/song and starts the play - which hands off after each play independently of who
	// owns the socket. If the server itself leaves, the room closes for everyone (a "roomclosed" event); that's
	// the accepted fallback (true socket migration is a future step).
	//
	// Protocol = "OpenTaiko Online" (OTON):
	//   • A connecting client must lead with the magic "OTON" + protocol version, or it's rejected outright -
	//     so a non-OpenTaiko client (random port-scanner, wrong game) can never join.
	//   • Right after, both sides do an ephemeral ECDH (P-256) key exchange and derive a per-SESSION AES-256-GCM
	//     key. EVERY message after the handshake is an AES-GCM frame (encrypted + authenticated), so a passive
	//     eavesdropper can't read traffic and a tamperer/MITM can't forge frames (the GCM tag fails). NOTE: the
	//     game is open source, so the *room-code* obfuscation key is discoverable - the room code resists casual
	//     hand-decoding, not a determined reader; the live SESSION is what's genuinely protected by the ECDH.
	//   • The connection string (room code) is the object {ip, stageid, payload} AES-GCM-encrypted under a fixed
	//     app key and Base64'd - copy/paste-able, and tamper-evident (a bad code just fails to decode).
	//
	// Lua API (global NET): all data crosses the boundary as STRINGS (stages use JSON), so there's no LuaTable
	// marshalling. The network runs on background threads and pushes events into a queue the stage drains each
	// frame via NET:Poll() - no Lua-side threading.
	//   NET:SetLocalPlayer(infoJson)                         -- your nameplate/character/etc., sent on join
	//   local code = NET:CreateRoom(stageId, payload [,max]) -- become host+server; returns the room code
	//   NET:JoinRoom(code)  / NET:PeekStageId(code)          -- join (async; result via events) / route by stage
	//   NET:Broadcast(channel, data) / NET:SendTo(id, channel, data)
	//   NET:RotateHost() / NET:SetHostRole(id)               -- (server only) hand the host role on
	//   local e = NET:Poll()  -> NetEvent{Type,Peer,Channel,Data} or nil   (drain in a while loop each frame)
	//   NET:Leave(), NET:IsHost(), NET:HasHostRole(), NET:HostRoleId(), NET:SelfId(), NET:Connected(),
	//   NET:PeerCount(), NET:PeersJson()
	// Event Types: "connected" (you joined; Data={selfId,stageId,payload,hostRoleId}), "joined" (Peer=id,
	//   Data=info), "left" (Peer=id), "message" (Peer=from,Channel,Data), "hostrole" (Peer=newHostId),
	//   "roomclosed", "error" (Data=msg).

	public sealed class NetEvent {
		public string Type = "";
		public int Peer;
		public string Channel = "";
		public string Data = "";
	}

	public sealed class LuaNetworking {
		// ── protocol constants ──────────────────────────────────────────────────────────────────────────
		private static readonly byte[] MAGIC = Encoding.ASCII.GetBytes("OTON");
		private const ushort PROTO_VER = 1;
		private const int DEFAULT_PORT = 41234;
		/// <summary>Test/advanced hook: when > 0, this room uses this port instead of the default.</summary>
		public int PortOverride = 0;

		public LuaNetworking() { KickDiscovery(); }   // public IP + UPnP lookup, once per process

		private bool _upnpMapped;                      // we asked the router for a forward → undo on Leave
		private int Port => PortOverride > 0 ? PortOverride : DEFAULT_PORT;
		// fixed key for the ROOM CODE only (OpenTaiko being open-source, simple obfuscation + integrity not secrecy)
		private static readonly byte[] APP_KEY = SHA256.HashData(Encoding.UTF8.GetBytes("OpenTaiko Online :: room code :: v1"));
		// wire opcodes (1 byte before the JSON body, inside each encrypted frame)
		private const byte OP_HELLO = 1, OP_WELCOME = 2, OP_PEERJOIN = 3, OP_PEERLEFT = 4, OP_HOSTROLE = 5, OP_DATA = 6, OP_REJECT = 7, OP_CLOSED = 8, OP_MIGRATE = 9;

		// ── state (guarded by _lock for the roster/peers; events via a concurrent queue) ─────────────────
		private readonly object _lock = new();
		private readonly ConcurrentQueue<NetEvent> _events = new();
		private readonly Dictionary<int, PeerConn> _peers = new();   // host: all clients; client: just {hostId:hostConn}
		private readonly Dictionary<int, string> _roster = new();    // id -> player info json (incl. self)
		private readonly List<int> _order = new();                   // join order (for host-role rotation), self included
		private TcpListener _listener;
		private Thread _acceptThread;
		private volatile bool _running;
		private bool _isHost;
		private int _selfId;
		private int _hostRoleId;
		private int _maxPlayers = 8;
		private int _nextId = 2;            // host assigns ids; host itself is 1
		private string _localInfo = "{}";
		private string _stageId = "", _payload = "";
		// ── migration + reconnect state ──────────────────────────────────────────────────────────────
		// the room SECRET authenticates rejoins (id reuse) and is shared over the encrypted channel;
		// the address book (id → ip as observed by the server) lets survivors find the successor.
		private string _roomSecret = "";
		private readonly Dictionary<int, string> _peerAddr = new();
		private int _serverPeerId = 1;            // the roster id of the current socket owner
		private string _serverIp = "";
		private volatile int _migrateTo = -1;     // successor announced by a gracefully-leaving server
		private volatile bool _recovering;

		// ── live in-game score sync ──────────────────────────────────────────────────────────────────────
		// During an online song the Lua lobby is suspended, so the C# gameplay screen streams the local
		// player's running score on a side channel ("ps") that BYPASSES the Poll() event queue and lands in
		// _liveScores instead - so it never pollutes the lobby's event stream. Bracketed by BeginPlaySync/
		// EndPlaySync around the play round; the gameplay overlay reaches the connected instance via Active.
		public static LuaNetworking Active;
		private volatile bool _playSync;
		private int _playEpoch;
		private volatile string _selfPlayScore = "";
		private string _selfPlayName = "";
		private readonly ConcurrentDictionary<int, string> _liveScores = new();
		// online play SPOT mapping: _spotPeers[i] = the peerId rendered in player spot i (index 0 = self). The
		// gameplay screen renders spots 1.. as REMOTE players (notes suppressed, score/gauge fed from the wire).
		private int[] _spotPeers;
		private readonly ConcurrentDictionary<int, string> _spotLive = new();   // spotIndex -> last "ps" json
		// per-spot judge odds (per mille) derived from each remote spot's broadcast good/ok/bad counts - fed to
		// the gameplay AlterJudgement so remote lanes auto-hit with a realistic judge mix (AI-battle-style).
		private readonly int[] _spotBadOdds = new int[8];
		private readonly int[] _spotGoodOdds = new int[8];
		public bool PlaySyncActive => _playSync;
		public int PlaySyncEpoch => _playEpoch;
		public string SelfPlayName => _selfPlayName;
		// ── play-flow barriers (loading sync + finish sync) + per-spot final result (for the clear animation) ──
		private readonly HashSet<int> _loadedPeers = new();     // peers that reported "ld" (passed song loading)
		private readonly HashSet<int> _finishedPeers = new();   // peers that reported "fn" (finished the song)
		private readonly ConcurrentDictionary<int, string> _spotResult = new();   // spotIndex -> final result json
		private long _barrierStart;                             // tick of the first BarrierReport* (timeout clock)
		private volatile bool _ldSent, _fnSent;

		// ── a single peer connection (host has many; client has one to the host) ─────────────────────────
		private sealed class PeerConn {
			public int Id;
			public TcpClient Tcp;
			public NetworkStream Stream;
			public AesGcm Rx, Tx;          // separate instances: Rx used only by the reader thread, Tx under SendLock
			public readonly object SendLock = new();
			public Thread Reader;
			public volatile bool Alive = true;
		}

		// ── Lua: identity / lifecycle ────────────────────────────────────────────────────────────────────
		public void SetLocalPlayer(string infoJson) { _localInfo = string.IsNullOrEmpty(infoJson) ? "{}" : infoJson; }

		/// <summary>Host a room: start the TCP server, become host (id 1) + initial host-role holder, and return
		/// the encrypted room code carrying {ip, stageid, payload}. maxPlayers caps joiners (default 8).</summary>
		public string CreateRoom(string stageId, string payload, int maxPlayers) {
			Leave();
			lock (_lock) {
				_isHost = true; _selfId = 1; _hostRoleId = 1; _nextId = 2;
				_maxPlayers = maxPlayers > 0 ? maxPlayers : 8;
				_stageId = stageId ?? ""; _payload = payload ?? "";
				_roster[1] = _localInfo; _order.Clear(); _order.Add(1);
				_roomSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
				_peerAddr.Clear(); _peerAddr[1] = LocalIPv4(); _serverPeerId = 1;
			}
			try {
				_listener = new TcpListener(IPAddress.Any, Port);
				_listener.Start();
			} catch (Exception e) { Enqueue("error", 0, "", "Could not host: " + e.Message); _isHost = false; return null; }
			_running = true;
			_acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "OTON-accept" };
			_acceptThread.Start();
			Active = this;
			if (PortOverride == 0) { UpnpMapPort(Port); _upnpMapped = true; }   // ask the router to forward us (real game only)
			var hostIps = LocalIPv4s();
			var obj = new JObject { ["ip"] = hostIps[0], ["ips"] = new JArray(hostIps), ["stageid"] = _stageId, ["payload"] = _payload };
			string pub = _publicIp;
			if (!string.IsNullOrEmpty(pub) && !hostIps.Contains(pub)) obj["pub"] = pub;   // direct internet candidate
			return EncodeRoom(obj.ToString(Newtonsoft.Json.Formatting.None));
		}
		public string CreateRoom(string stageId, string payload) => CreateRoom(stageId, payload, 8);

		/// <summary>Decode a room code's stage id WITHOUT connecting (so the join UI can route to the right stage),
		/// or null if the code is invalid.</summary>
		public string PeekStageId(string roomCode) {
			var o = DecodeRoom(roomCode); return o?["stageid"]?.ToString();
		}

		/// <summary>Join a room from its code (async - success/failure arrives as a "connected"/"error" event).
		/// Returns false immediately only if the code itself is malformed.</summary>
		public bool JoinRoom(string roomCode) {
			var o = DecodeRoom(roomCode);
			if (o == null) return false;
			string stage = o["stageid"]?.ToString() ?? ""; string pay = o["payload"]?.ToString() ?? "";
			// newer codes carry every host address ("ips" + public "pub"); older ones just the primary ("ip")
			var ips = new List<string>();
			if (o["ips"] is JArray arr) foreach (var v in arr) { string s = v?.ToString(); if (!string.IsNullOrEmpty(s) && !ips.Contains(s)) ips.Add(s); }
			string one = o["ip"]?.ToString();
			if (!string.IsNullOrEmpty(one) && !ips.Contains(one)) ips.Insert(0, one);
			string pub = o["pub"]?.ToString();
			if (!string.IsNullOrEmpty(pub) && !ips.Contains(pub)) ips.Add(pub);   // internet candidate last: LAN/VPN joins stay instant
			if (ips.Count == 0) return false;
			Leave();
			_stageId = stage; _payload = pay;   // keep the room metadata: a migration SUCCESSOR re-hosts with it
			_isHost = false; _running = true;
			Active = this;
			var t = new Thread(() => {
				string lastErr = null;
				foreach (string ip in ips) {
					if (!_running) return;
					lastErr = ClientConnect(ip, Port, stage, pay, 0, ips.Count > 1 ? 3000 : 6000);
					if (lastErr == null) return;   // connected
				}
				bool unreachable = lastErr != null && lastErr.Contains("did not respond");
				string hint = unreachable
					? $" — same network/VPN: check the host's firewall allows OpenTaiko (inbound TCP {Port}); over the internet: the host needs UPnP or port forwarding (TCP {Port}), or use a VPN like Tailscale/Radmin"
					: "";
				Enqueue("error", 0, "", $"{lastErr} (tried {string.Join(", ", ips)}){hint}");
			}) { IsBackground = true, Name = "OTON-connect" };
			t.Start();
			return true;
		}

		/// <summary>Disconnect / close the room. (Server leaving closes it for everyone.)</summary>
		public void Leave() {
			_running = false;
			if (_upnpMapped) { _upnpMapped = false; UpnpUnmapPort(Port); }
			try { _listener?.Stop(); } catch { } _listener = null;
			List<PeerConn> conns; int successor = -1;
			lock (_lock) {
				conns = new List<PeerConn>(_peers.Values);
				if (_isHost) foreach (var id in _order) if (id != _selfId && _roster.ContainsKey(id) && (successor < 0 || id < successor)) successor = id;
				_peers.Clear(); _roster.Clear(); _order.Clear(); _peerAddr.Clear();
			}
			// a deliberately-leaving server HANDS THE ROOM OVER instead of closing it: announce the
			// successor (lowest surviving id); the clients migrate to it (socket migration).
			if (_isHost && successor > 0) {
				var mig = new JObject { ["id"] = successor };
				foreach (var c in conns) { try { SendFrame(c, OP_MIGRATE, mig); } catch { } }
			}
			foreach (var c in conns) CloseConn(c, (_isHost && successor <= 0) ? OP_CLOSED : (byte)0);
			_isHost = false; _selfId = 0; _hostRoleId = 0;
			_playSync = false; _liveScores.Clear(); _spotLive.Clear(); _spotPeers = null;
			Array.Clear(_spotBadOdds, 0, _spotBadOdds.Length); Array.Clear(_spotGoodOdds, 0, _spotGoodOdds.Length);
			BarrierReset();
			if (Active == this) Active = null;
			// drain stale events so a fresh session starts clean
			while (_events.TryDequeue(out _)) { }
		}

		// ── Lua: messaging ───────────────────────────────────────────────────────────────────────────────
		public void Broadcast(string channel, string data) => SendTo(-1, channel, data);

		public void SendTo(int peerId, string channel, string data) {
			if (!_running) return;
			var body = new JObject { ["f"] = _selfId, ["t"] = peerId, ["c"] = channel ?? "", ["d"] = data ?? "" };
			if (_isHost) RelayFromHost(_selfId, peerId, body);
			else { lock (_lock) { foreach (var c in _peers.Values) SendFrame(c, OP_DATA, body); } }   // client -> host relays
		}

		// ── Lua/C#: live in-game score side-channel (safe no-ops outside a play round) ──────────────────────
		/// <summary>Open the live-score round (clears prior scores). Call right before Exit("play").</summary>
		public void BeginPlaySync(string selfName) { _selfPlayName = selfName ?? ""; _selfPlayScore = ""; _liveScores.Clear(); _spotLive.Clear(); Array.Clear(_spotBadOdds, 0, _spotBadOdds.Length); Array.Clear(_spotGoodOdds, 0, _spotGoodOdds.Length); _ldSent = false; _fnSent = false; _barrierStart = 0; _playEpoch++; _playSync = true; }
	// NOTE: the loaded/finished sets are NOT cleared here — a fast peer's "ld" may arrive before our
	// own BeginPlaySync (stage-transition lag), and wiping it would stall the load barrier to timeout.
	// They are cleared when the previous round ENDS (EndPlaySync) and on Leave().
		/// <summary>Set the spot→peer mapping for the upcoming play (JSON int array; index 0 = self). The gameplay
		/// screen renders spots 1.. as remote players. Call alongside BeginPlaySync.</summary>
		public void SetPlaySpots(string json) {
			try { var arr = JArray.Parse(json); var list = new int[arr.Count]; for (int i = 0; i < arr.Count; i++) list[i] = (int)arr[i]; _spotPeers = list; }
			catch { _spotPeers = null; }
		}
		public int PlaySpotCount() => _spotPeers?.Length ?? 0;
		/// <summary>True if player spot <paramref name="spot"/> is a remote online player (so the gameplay screen
		/// should suppress its notes and feed its score/gauge from the wire). Always false outside a play round.</summary>
		public bool IsRemoteSpot(int spot) => _playSync && _spotPeers != null && spot >= 1 && spot < _spotPeers.Length;
		/// <summary>Latest "ps" json for a remote spot (score/gauge), or "" - for the gameplay overlay.</summary>
		public string GetSpotPlayJson(int spot) => _spotLive.TryGetValue(spot, out var v) ? v : "";
		/// <summary>Per-mille odds (0..1000) that a remote spot's next auto-hit is judged Bad / Good (rest = Great),
		/// from its broadcast good/ok/bad counts. Used by the gameplay AlterJudgement.</summary>
		public int GetSpotBadOdds(int spot) => (spot >= 0 && spot < _spotBadOdds.Length) ? _spotBadOdds[spot] : 0;
		public int GetSpotGoodOdds(int spot) => (spot >= 0 && spot < _spotGoodOdds.Length) ? _spotGoodOdds[spot] : 0;

		// ── play-flow barriers (loading + finish), per-spot final result, live disconnect ───────────────────
		public void BarrierReset() {
			lock (_loadedPeers) _loadedPeers.Clear();
			lock (_finishedPeers) _finishedPeers.Clear();
			_spotResult.Clear(); _ldSent = false; _fnSent = false; _barrierStart = 0;
		}
		/// <summary>Report (once) that this client passed song loading, and broadcast it.</summary>
		public void ReportLoaded() { if (_ldSent) return; _ldSent = true; if (_barrierStart == 0) _barrierStart = Environment.TickCount64; Broadcast("ld", "{}"); }
		/// <summary>True once every still-connected peer reported "loaded", or timeoutMs elapsed (the host then drops
		/// the laggards so they vanish from the play). Call each frame while holding the loading screen.</summary>
		public bool LoadBarrierReady(int timeoutMs) {
			if (!_ldSent) return false;
			if (AllPeersIn(_loadedPeers)) return true;
			if (Environment.TickCount64 - _barrierStart >= timeoutMs) { DropPeersNotIn(_loadedPeers); return true; }
			return false;
		}
		/// <summary>Report (once) that this client finished the song, carrying its final result json; broadcast it.</summary>
		public void ReportFinished(string resultJson) { if (_fnSent) return; _fnSent = true; _barrierStart = Environment.TickCount64; Broadcast("fn", resultJson ?? "{}"); }
		/// <summary>True once every still-connected peer reported "finished", or timeoutMs elapsed.</summary>
		public bool FinishBarrierReady(int timeoutMs) {
			if (!_fnSent) return false;
			if (AllPeersIn(_finishedPeers)) return true;
			return Environment.TickCount64 - _barrierStart >= timeoutMs;
		}
		/// <summary>Final result json for a remote spot (from its "fn"), or "" - used to drive its clear animation.</summary>
		public string GetSpotResultJson(int spot) => (_spotPeers != null && spot >= 1 && spot < _spotPeers.Length && _spotResult.TryGetValue(spot, out var v)) ? v : "";
		/// <summary>Clear level of a remote spot from its broadcast finish result: 2=rainbow/max, 1=clear, 0=fail,
		/// -1=unknown (no "fn" yet → caller falls back to the local sim). Drives the correct clear animation.</summary>
		public int GetSpotClearLevel(int spot) {
			var j = GetSpotResultJson(spot);
			if (string.IsNullOrEmpty(j)) return -1;
			try { var o = JObject.Parse(j); if ((bool?)o["mx"] == true) return 2; if ((bool?)o["cl"] == true) return 1; return 0; } catch { return -1; }
		}
		/// <summary>A remote spot's final judge count from its broadcast finish result ("gr"=great, "gd"=good,
		/// "ms"=bad), or -1 if there's no result yet. Lets the results screen show the real player's judges
		/// instead of the local auto-play's.</summary>
		public int GetSpotJudge(int spot, string key) {
			var j = GetSpotResultJson(spot);
			if (string.IsNullOrEmpty(j)) return -1;
			try { var o = JObject.Parse(j); return (int?)o[key] ?? -1; } catch { return -1; }
		}
		/// <summary>Is the peer rendered in this spot still connected? (false = dropped mid-play → hide that spot.)</summary>
		public bool IsSpotActive(int spot) {
			if (_spotPeers == null || spot < 0 || spot >= _spotPeers.Length) return false;
			if (spot == 0) return true;
			int pid = _spotPeers[spot];
			lock (_lock) return _roster.ContainsKey(pid);
		}
		private bool AllPeersIn(HashSet<int> set) {
			lock (_lock) { lock (set) { foreach (var id in _roster.Keys) if (id != _selfId && !set.Contains(id)) return false; } }
			return true;
		}
		private void DropPeersNotIn(HashSet<int> set) {
			if (!_isHost) return;
			List<PeerConn> drop = new();
			lock (_lock) { lock (set) { foreach (var c in _peers.Values) if (!set.Contains(c.Id)) drop.Add(c); } }
			foreach (var c in drop) OnConnDropped(c);   // removes from roster + tells everyone (they see "left")
		}
		/// <summary>Close the live-score round. Call when the lobby regains control after the song.</summary>
		public void EndPlaySync() { _playSync = false; BarrierReset(); }
		/// <summary>Broadcast the local player's running score (called by the C# gameplay screen ~6-7x/sec).</summary>
		public void PushPlayScore(string json) { _selfPlayScore = json ?? ""; Broadcast("ps", json ?? ""); }
		public string GetSelfPlayScore() => _selfPlayScore;
		/// <summary>C# overlay: snapshot of peerId -> last score json (excludes self).</summary>
		public Dictionary<int, string> GetLivePlayScoresCopy() => new Dictionary<int, string>(_liveScores);
		/// <summary>Lua: final standings after the song (self + each peer's last broadcast) as a JSON array.</summary>
		public string LivePlayScoresJson() {
			var arr = new JArray();
			if (!string.IsNullOrEmpty(_selfPlayScore)) arr.Add(new JObject { ["id"] = _selfId, ["d"] = _selfPlayScore });
			foreach (var kv in _liveScores) arr.Add(new JObject { ["id"] = kv.Key, ["d"] = kv.Value });
			return arr.ToString(Newtonsoft.Json.Formatting.None);
		}

		// ── Lua: queries / host role ─────────────────────────────────────────────────────────────────────
		public NetEvent Poll() => _events.TryDequeue(out var e) ? e : null;
		public bool IsHost() => _isHost;
		public bool HasHostRole() => _selfId != 0 && _selfId == _hostRoleId;
		public int HostRoleId() => _hostRoleId;
		public int SelfId() => _selfId;
		public bool Connected() { lock (_lock) return _selfId != 0 && (_isHost || _peers.Count > 0); }
		public int PeerCount() { lock (_lock) return _roster.Count; }

		public string PeersJson() {
			var arr = new JArray();
			lock (_lock) {
				foreach (var id in _order) {
					if (!_roster.TryGetValue(id, out var info)) continue;
					arr.Add(new JObject { ["id"] = id, ["info"] = info, ["isHost"] = (id == 1), ["hostRole"] = (id == _hostRoleId) });
				}
			}
			return arr.ToString(Newtonsoft.Json.Formatting.None);
		}

		/// <summary>(Server only) hand the host role to the next player in join order - call after each play.</summary>
		public void RotateHost() {
			if (!_isHost) return;
			int next;
			lock (_lock) {
				if (_order.Count == 0) return;
				int idx = _order.IndexOf(_hostRoleId);
				next = _order[(idx + 1) % _order.Count];
			}
			SetHostRole(next);
		}
		/// <summary>(Server only) set the host role to a specific peer and announce it.</summary>
		public void SetHostRole(int peerId) {
			if (!_isHost) return;
			lock (_lock) { if (!_roster.ContainsKey(peerId)) return; _hostRoleId = peerId; }
			var msg = new JObject { ["id"] = peerId };
			lock (_lock) foreach (var c in _peers.Values) SendFrame(c, OP_HOSTROLE, msg);
			Enqueue("hostrole", peerId, "", "");
		}

		// ── host: accept + relay ─────────────────────────────────────────────────────────────────────────
		private void AcceptLoop() {
			while (_running) {
				TcpClient cli;
				try { cli = _listener.AcceptTcpClient(); } catch { break; }
				try { cli.NoDelay = true; } catch { }
				var th = new Thread(() => HostHandshake(cli)) { IsBackground = true, Name = "OTON-hs" };
				th.Start();
			}
		}

		private void HostHandshake(TcpClient cli) {
			try {
				var ns = cli.GetStream(); ns.ReadTimeout = 8000;
				// validate the OpenTaiko Online magic + version (rejects any non-OT / wrong-version client)
				byte[] hdr = ReadExact(ns, 6);
				if (hdr == null || hdr[0] != MAGIC[0] || hdr[1] != MAGIC[1] || hdr[2] != MAGIC[2] || hdr[3] != MAGIC[3]
					|| (ushort)((hdr[4] << 8) | hdr[5]) != PROTO_VER) { try { cli.Close(); } catch { } return; }
				byte[] cliPub = ReadBlock(ns); if (cliPub == null) { cli.Close(); return; }
				using var dh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
				byte[] myPub = dh.ExportSubjectPublicKeyInfo();
				WriteHeader(ns); WriteBlock(ns, myPub);
				byte[] key = DeriveSession(dh, cliPub);
				var conn = new PeerConn { Tcp = cli, Stream = ns, Rx = new AesGcm(key, 16), Tx = new AesGcm(key, 16) };
				ns.ReadTimeout = Timeout.Infinite;
				// first frame must be HELLO with the joiner's player info
				if (!ReadFrame(conn, out byte op, out JObject hello) || op != OP_HELLO) { CloseConn(conn, 0); return; }
				int id; bool full;
				lock (_lock) {
					full = _roster.Count >= _maxPlayers;
					id = full ? 0 : _nextId++;
				}
				if (full) { SendFrame(conn, OP_REJECT, new JObject { ["why"] = "Room is full" }); CloseConn(conn, 0); return; }
				// REJOIN: a dropped peer (or a migration survivor) presents its old id + the room secret
				int rid = hello["rid"] != null ? (int)hello["rid"] : 0;
				string sec = hello["sec"]?.ToString() ?? "";
				bool isRejoin = rid > 0 && sec == _roomSecret && _roomSecret.Length > 0;
				bool wasKnown = false;
				if (isRejoin) {
					PeerConn zombie = null;
					lock (_lock) {
						if (_peers.TryGetValue(rid, out zombie)) _peers.Remove(rid);   // replace a half-dead conn
						wasKnown = _roster.ContainsKey(rid);
						id = rid; if (rid >= _nextId) _nextId = rid + 1;
					}
					if (zombie != null) CloseConn(zombie, 0);
				}
				conn.Id = id;
				string info = hello["i"]?.ToString() ?? "{}";
				if (info.Length > 16384) info = "{}";   // relayed to the whole room: cap the amplification
				string addr = "";
				try { addr = ((IPEndPoint)cli.Client.RemoteEndPoint).Address.ToString(); } catch { }
				JArray roster = new JArray();
				lock (_lock) {
					_peers[id] = conn; _roster[id] = info;
					if (!_order.Contains(id)) _order.Add(id);
					_peerAddr[id] = addr;
					foreach (var oid in _order) if (_roster.TryGetValue(oid, out var oinfo))
						roster.Add(new JObject { ["id"] = oid, ["i"] = oinfo, ["a"] = _peerAddr.TryGetValue(oid, out var oa) ? oa : "" });
				}
				// welcome the joiner (their id, the host-role, the stage/payload, the roster, the room
				// secret + the server's own roster id — everything a survivor needs to migrate later)
				SendFrame(conn, OP_WELCOME, new JObject { ["id"] = id, ["h"] = _hostRoleId, ["s"] = _stageId, ["p"] = _payload, ["r"] = roster, ["sec"] = _roomSecret, ["sid"] = _selfId });
				// tell everyone else a peer joined; tell ourselves (host) via the event queue
				var pj = new JObject { ["id"] = id, ["i"] = info, ["a"] = addr };
				lock (_lock) foreach (var c in _peers.Values) if (c.Id != id) SendFrame(c, OP_PEERJOIN, pj);
				if (!wasKnown) Enqueue("joined", id, "", info);
				conn.Reader = new Thread(() => ReaderLoop(conn)) { IsBackground = true, Name = "OTON-rx" + id };
				conn.Reader.Start();
			} catch { try { cli.Close(); } catch { } }
		}

		// host: route a DATA message coming from `from` (could be the host itself) to `to` (-1 = everyone else)
		private void RelayFromHost(int from, int to, JObject body) {
			body["f"] = from;
			List<PeerConn> targets = new();
			bool toHost = false;
			lock (_lock) {
				if (to == -1) { foreach (var c in _peers.Values) if (c.Id != from) targets.Add(c); if (from != _selfId) toHost = true; }
				else if (to == _selfId) toHost = true;
				else if (_peers.TryGetValue(to, out var c)) targets.Add(c);
			}
			foreach (var c in targets) SendFrame(c, OP_DATA, body);
			if (toHost) DeliverMessage(from, body["c"]?.ToString() ?? "", body["d"]?.ToString() ?? "");
		}

		// ── client: connect + handshake ──────────────────────────────────────────────────────────────────
		/// <summary>One connection attempt; returns null on success or the error text (callers decide
		/// whether/how to surface it — the joiner aggregates over several candidate addresses).</summary>
		private string ClientConnect(string ip, int port, string stage, string payload, int rejoinId, int connectTimeoutMs = 6000) {
			TcpClient cli = null;
			try {
				cli = new TcpClient();
				var ar = cli.BeginConnect(ip, port, null, null);
				if (!ar.AsyncWaitHandle.WaitOne(connectTimeoutMs)) { throw new Exception($"Host did not respond at {ip}"); }
				try { cli.EndConnect(ar); } catch (SocketException se) {
					throw new Exception(se.SocketErrorCode == SocketError.ConnectionRefused
						? $"Connection refused at {ip} (the room is closed, or another program owns the port)"
						: $"Could not connect to {ip}: {se.SocketErrorCode}");
				}
				if (!cli.Connected) throw new Exception($"Host did not respond at {ip}");
				cli.NoDelay = true;
				var ns = cli.GetStream(); ns.ReadTimeout = 8000;
				WriteHeader(ns);
				using var dh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
				WriteBlock(ns, dh.ExportSubjectPublicKeyInfo());
				byte[] hdr = ReadExact(ns, 6);
				if (hdr == null || hdr[0] != MAGIC[0] || hdr[1] != MAGIC[1] || hdr[2] != MAGIC[2] || hdr[3] != MAGIC[3]
					|| (ushort)((hdr[4] << 8) | hdr[5]) != PROTO_VER) throw new Exception("Not an OpenTaiko host (or version mismatch)");
				byte[] hostPub = ReadBlock(ns); if (hostPub == null) throw new Exception("Bad handshake");
				byte[] key = DeriveSession(dh, hostPub);
				var conn = new PeerConn { Id = 1, Tcp = cli, Stream = ns, Rx = new AesGcm(key, 16), Tx = new AesGcm(key, 16) };
				var helloMsg = new JObject { ["i"] = _localInfo };
				if (rejoinId > 0 && _roomSecret.Length > 0) { helloMsg["rid"] = rejoinId; helloMsg["sec"] = _roomSecret; }
				SendFrame(conn, OP_HELLO, helloMsg);
				ns.ReadTimeout = 12000;
				if (!ReadFrame(conn, out byte op, out JObject msg)) throw new Exception("No reply from host");
				if (op == OP_REJECT) throw new Exception(msg["why"]?.ToString() ?? "Rejected");
				if (op != OP_WELCOME) throw new Exception("Unexpected handshake reply");
				ns.ReadTimeout = Timeout.Infinite;
				lock (_lock) {
					_selfId = (int)msg["id"]; _hostRoleId = (int)msg["h"];
					_roomSecret = msg["sec"]?.ToString() ?? "";
					_serverPeerId = msg["sid"] != null ? (int)msg["sid"] : 1;
					_serverIp = ip; _migrateTo = -1;
					_peers[1] = conn; _roster.Clear(); _order.Clear(); _peerAddr.Clear();
					foreach (var r in (JArray)msg["r"]) {
						int rid2 = (int)r["id"]; _roster[rid2] = r["i"]?.ToString() ?? "{}"; _order.Add(rid2);
						_peerAddr[rid2] = r["a"]?.ToString() ?? "";
					}
					if (!_order.Contains(_selfId)) { _order.Add(_selfId); _roster[_selfId] = _localInfo; }
				}
				var info = new JObject { ["selfId"] = _selfId, ["stageId"] = msg["s"]?.ToString() ?? stage, ["payload"] = msg["p"]?.ToString() ?? payload, ["hostRoleId"] = _hostRoleId };
				Enqueue("connected", _selfId, "", info.ToString(Newtonsoft.Json.Formatting.None));
				conn.Reader = new Thread(() => ReaderLoop(conn)) { IsBackground = true, Name = "OTON-rx" };
				conn.Reader.Start();
				return null;
			} catch (Exception e) {
				try { cli?.Close(); } catch { }
				return e.Message;
			}
		}

		// ── shared reader loop ───────────────────────────────────────────────────────────────────────────
		private void ReaderLoop(PeerConn conn) {
			while (_running && conn.Alive) {
				if (!ReadFrame(conn, out byte op, out JObject body)) break;
				HandleFrame(conn, op, body);
			}
			OnConnDropped(conn);
		}

		private void HandleFrame(PeerConn conn, byte op, JObject body) {
			switch (op) {
				case OP_DATA:
					if (_isHost) RelayFromHost(conn.Id, body["t"] != null ? (int)body["t"] : -1, body);
					else DeliverMessage(body["f"] != null ? (int)body["f"] : 0, body["c"]?.ToString() ?? "", body["d"]?.ToString() ?? "");
					break;
				case OP_PEERJOIN: {   // client side
					int id = (int)body["id"]; string info = body["i"]?.ToString() ?? "{}";
					bool isNew;
					lock (_lock) {
						isNew = !_roster.ContainsKey(id); if (isNew) _order.Add(id); _roster[id] = info;
						_peerAddr[id] = body["a"]?.ToString() ?? "";
					}
					if (isNew) Enqueue("joined", id, "", info);   // join-race duplicates carry no news
					break;
				}
				case OP_PEERLEFT: {   // client side
					int id = (int)body["id"];
					lock (_lock) { _roster.Remove(id); _order.Remove(id); }
					Enqueue("left", id, "", "");
					break;
				}
				case OP_HOSTROLE:
					_hostRoleId = (int)body["id"]; Enqueue("hostrole", _hostRoleId, "", "");
					break;
				case OP_MIGRATE:
					_migrateTo = body["id"] != null ? (int)body["id"] : -1;   // graceful handover: skip the
					conn.Alive = false;                                       // retry phase, go straight to it
					Recover(conn);
					break;
				case OP_CLOSED:
					conn.Alive = false; Enqueue("roomclosed", 0, "", "");
					break;
			}
		}

		private void OnConnDropped(PeerConn conn) {
			if (!conn.Alive && !_isHost) { return; }   // already handled (e.g. OP_CLOSED)
			conn.Alive = false;
			if (_isHost) {
				bool wasHostRole;
				lock (_lock) { _peers.Remove(conn.Id); _roster.Remove(conn.Id); _order.Remove(conn.Id); wasHostRole = conn.Id == _hostRoleId; }
				CloseConn(conn, 0);
				if (!_running) return;
				var pl = new JObject { ["id"] = conn.Id };
				lock (_lock) foreach (var c in _peers.Values) SendFrame(c, OP_PEERLEFT, pl);
				Enqueue("left", conn.Id, "", "");
				if (wasHostRole) RotateHost();
			} else {
				CloseConn(conn, 0);
				if (_running) Recover(conn);   // lost the server unexpectedly → reconnect / migrate
			}
		}

		// ── reconnect + socket migration ─────────────────────────────────────────────────────────────────
		// Runs when a CLIENT loses the server. Phase 1 (skipped on a graceful OP_MIGRATE handover):
		// retry the same server — covers a network blip on OUR side. Phase 2: everyone deterministically
		// agrees the successor = the LOWEST surviving roster id; the successor re-binds the port and the
		// rest reconnect to its known address with their old id + the room secret, so identities survive.
		private void Recover(PeerConn deadConn) {
			if (_recovering || !_running) return;
			_recovering = true;
			var t = new Thread(() => RecoverLoop()) { IsBackground = true, Name = "OTON-recover" };
			t.Start();
		}
		private void RecoverLoop() {
			try {
				int deadServer; string serverIp; int migrateTo = _migrateTo; int myId = _selfId;
				lock (_lock) { deadServer = _serverPeerId; serverIp = _serverIp; _peers.Clear(); }
				Enqueue("error", 0, "", migrateTo > 0 ? "Host left — migrating the room..." : "Connection lost — reconnecting...");
				// phase 1: the server may still be there (our blip) — unless it told us it left
				if (migrateTo < 0) {
					for (int a = 0; a < 3 && _running; a++) {
						if (TryRejoin(serverIp, myId)) { _recovering = false; return; }
						Thread.Sleep(2500);
					}
				}
				// phase 2: deterministic successor among survivors
				int succ = migrateTo;
				if (succ <= 0) {
					lock (_lock) foreach (var id in _roster.Keys) if (id != deadServer && (succ <= 0 || id < succ)) succ = id;
				}
				if (succ <= 0) { GiveUp(); return; }
				if (succ == myId) { BecomeServer(deadServer); _recovering = false; return; }
				string succIp; lock (_lock) _peerAddr.TryGetValue(succ, out succIp);
				if (string.IsNullOrEmpty(succIp)) { GiveUp(); return; }
				for (int a = 0; a < 8 && _running; a++) {        // the successor needs a moment to re-bind
					Thread.Sleep(2500);
					if (TryRejoin(succIp, myId)) { _recovering = false; return; }
				}
				GiveUp();
			} catch { GiveUp(); }
		}
		private bool TryRejoin(string ip, int myId) {
			try {
				// synchronous; per-attempt errors stay silent (RecoverLoop already told the user once)
				return ClientConnect(ip, Port, _stageId, _payload, myId) == null && Connected();
			} catch { return false; }
		}
		private void GiveUp() { _recovering = false; if (_running) Enqueue("roomclosed", 0, "", ""); }
		// the successor: become the room's socket owner, keep every surviving identity, and give the
		// stragglers a grace window to rejoin before declaring them gone.
		private void BecomeServer(int deadServer) {
			List<int> expected = new();
			lock (_lock) {
				_isHost = true; _serverPeerId = _selfId; _serverIp = LocalIPv4(); _peerAddr[_selfId] = _serverIp;
				_roster.Remove(deadServer); _order.Remove(deadServer); _peerAddr.Remove(deadServer);
				int mx = 1; foreach (var id in _roster.Keys) { if (id > mx) mx = id; if (id != _selfId) expected.Add(id); }
				_nextId = mx + 1;
				if (!_roster.ContainsKey(_hostRoleId)) _hostRoleId = _selfId;
			}
			try {
				_listener = new TcpListener(IPAddress.Any, Port);
				_listener.Start();
			} catch { GiveUp(); return; }
			_acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "OTON-accept" };
			_acceptThread.Start();
			Enqueue("left", deadServer, "", "");
			Enqueue("hostrole", _hostRoleId, "", "");
			Enqueue("error", 0, "", "You now host the room.");
			// grace watchdog: drop survivors that never made it over
			var wd = new Thread(() => {
				Thread.Sleep(25000);
				if (!_running || !_isHost) return;
				List<int> gone = new();
				lock (_lock) { foreach (var id in expected) if (_roster.ContainsKey(id) && !_peers.ContainsKey(id)) gone.Add(id); }
				foreach (var id in gone) {
					lock (_lock) { _roster.Remove(id); _order.Remove(id); _peerAddr.Remove(id); }
					var pl = new JObject { ["id"] = id };
					lock (_lock) foreach (var c in _peers.Values) SendFrame(c, OP_PEERLEFT, pl);
					Enqueue("left", id, "", "");
				}
			}) { IsBackground = true, Name = "OTON-grace" };
			wd.Start();
		}

		// ── framing + crypto ─────────────────────────────────────────────────────────────────────────────
		private void SendFrame(PeerConn conn, byte op, JObject body) {
			if (conn == null || !conn.Alive) return;
			try {
				byte[] json = Encoding.UTF8.GetBytes(body.ToString(Newtonsoft.Json.Formatting.None));
				byte[] plain = new byte[1 + json.Length]; plain[0] = op; Buffer.BlockCopy(json, 0, plain, 1, json.Length);
				byte[] nonce = RandomNumberGenerator.GetBytes(12);
				byte[] ct = new byte[plain.Length]; byte[] tag = new byte[16];
				lock (conn.SendLock) {
					conn.Tx.Encrypt(nonce, plain, ct, tag);
					byte[] frame = new byte[4 + 12 + ct.Length + 16];
					int n = 12 + ct.Length + 16;
					frame[0] = (byte)(n >> 24); frame[1] = (byte)(n >> 16); frame[2] = (byte)(n >> 8); frame[3] = (byte)n;
					Buffer.BlockCopy(nonce, 0, frame, 4, 12);
					Buffer.BlockCopy(ct, 0, frame, 16, ct.Length);
					Buffer.BlockCopy(tag, 0, frame, 16 + ct.Length, 16);
					conn.Stream.Write(frame, 0, frame.Length);
				}
			} catch { conn.Alive = false; }
		}

		private bool ReadFrame(PeerConn conn, out byte op, out JObject body) {
			op = 0; body = null;
			try {
				byte[] lenb = ReadExact(conn.Stream, 4); if (lenb == null) return false;
				int n = (lenb[0] << 24) | (lenb[1] << 16) | (lenb[2] << 8) | lenb[3];
				if (n < 12 + 16 || n > 8 * 1024 * 1024) return false;   // sanity cap (8 MB/frame)
				byte[] buf = ReadExact(conn.Stream, n); if (buf == null) return false;
				byte[] nonce = new byte[12]; Buffer.BlockCopy(buf, 0, nonce, 0, 12);
				int ctLen = n - 12 - 16;
				byte[] ct = new byte[ctLen]; Buffer.BlockCopy(buf, 12, ct, 0, ctLen);
				byte[] tag = new byte[16]; Buffer.BlockCopy(buf, 12 + ctLen, tag, 0, 16);
				byte[] plain = new byte[ctLen];
				conn.Rx.Decrypt(nonce, ct, tag, plain);   // throws on a forged/tampered frame -> drop the connection
				op = plain[0];
				body = JObject.Parse(Encoding.UTF8.GetString(plain, 1, plain.Length - 1));
				return true;
			} catch { return false; }
		}

		private void CloseConn(PeerConn conn, byte announce) {
			if (conn == null) return;
			conn.Alive = false;
			if (announce == OP_CLOSED) { try { SendFrame(conn, OP_CLOSED, new JObject()); } catch { } }
			try { conn.Stream?.Close(); } catch { }
			try { conn.Tcp?.Close(); } catch { }
			try { conn.Rx?.Dispose(); conn.Tx?.Dispose(); } catch { }
		}

		private void Enqueue(string type, int peer, string channel, string data) =>
			_events.Enqueue(new NetEvent { Type = type, Peer = peer, Channel = channel ?? "", Data = data ?? "" });

		// deliver an incoming DATA message: live-score frames ("ps") during a play round bypass the Poll()
		// queue and land in _liveScores instead, so the lobby's event stream stays clean.
		private void DeliverMessage(int from, string channel, string data) {
			if (_playSync && channel == "ps") {
				if (from != _selfId) {
					_liveScores[from] = data ?? "";
					if (_spotPeers != null) {
						int sp = Array.IndexOf(_spotPeers, from);
						if (sp >= 1) {
							_spotLive[sp] = data ?? "";
							if (sp < _spotBadOdds.Length) {
								try {
									var o = JObject.Parse(data);
									int gr = (int?)o["gr"] ?? 0, gd = (int?)o["gd"] ?? 0, ms = (int?)o["ms"] ?? 0;
									int tot = gr + gd + ms;
									if (tot > 0) { _spotBadOdds[sp] = (int)Math.Round(1000.0 * ms / tot); _spotGoodOdds[sp] = (int)Math.Round(1000.0 * gd / tot); }
								} catch { }
							}
						}
					}
				}
				return;
			}
			// barrier reports register regardless of _playSync: they can legitimately arrive BEFORE this
			// client's own BeginPlaySync (see the note there) and must not stall the round
			if (channel == "ld") { lock (_loadedPeers) _loadedPeers.Add(from); return; }
			if (channel == "fn") {
				lock (_finishedPeers) _finishedPeers.Add(from);
				if (_spotPeers != null) { int sp = Array.IndexOf(_spotPeers, from); if (sp >= 1) _spotResult[sp] = data ?? ""; }
				return;
			}
			Enqueue("message", from, channel ?? "", data ?? "");
		}

		// header = MAGIC(4) + version(2, big-endian)
		private static void WriteHeader(NetworkStream ns) {
			byte[] h = new byte[6]; Buffer.BlockCopy(MAGIC, 0, h, 0, 4); h[4] = (byte)(PROTO_VER >> 8); h[5] = (byte)PROTO_VER;
			ns.Write(h, 0, 6);
		}
		// length-prefixed (2-byte BE) raw block, used only for the plaintext ECDH public keys in the handshake
		private static void WriteBlock(NetworkStream ns, byte[] data) {
			byte[] len = { (byte)(data.Length >> 8), (byte)data.Length };
			ns.Write(len, 0, 2); ns.Write(data, 0, data.Length);
		}
		private static byte[] ReadBlock(NetworkStream ns) {
			byte[] len = ReadExact(ns, 2); if (len == null) return null;
			int n = (len[0] << 8) | len[1]; if (n <= 0 || n > 4096) return null;
			return ReadExact(ns, n);
		}
		private static byte[] ReadExact(NetworkStream ns, int n) {
			byte[] b = new byte[n]; int got = 0;
			while (got < n) { int r; try { r = ns.Read(b, got, n - got); } catch { return null; } if (r <= 0) return null; got += r; }
			return b;
		}

		private static byte[] DeriveSession(ECDiffieHellman mine, byte[] otherPubDer) {
			using var other = ECDiffieHellman.Create();
			other.ImportSubjectPublicKeyInfo(otherPubDer, out _);
			return mine.DeriveKeyFromHash(other.PublicKey, HashAlgorithmName.SHA256);   // 32 bytes
		}

		private static string EncodeRoom(string json) {
			byte[] pt = Encoding.UTF8.GetBytes(json);
			byte[] nonce = RandomNumberGenerator.GetBytes(12);
			byte[] ct = new byte[pt.Length]; byte[] tag = new byte[16];
			using (var gcm = new AesGcm(APP_KEY, 16)) gcm.Encrypt(nonce, pt, ct, tag);
			byte[] outb = new byte[12 + ct.Length + 16];
			Buffer.BlockCopy(nonce, 0, outb, 0, 12); Buffer.BlockCopy(ct, 0, outb, 12, ct.Length); Buffer.BlockCopy(tag, 0, outb, 12 + ct.Length, 16);
			return "OTON1." + Convert.ToBase64String(outb);
		}
		private static JObject DecodeRoom(string code) {
			if (string.IsNullOrWhiteSpace(code)) return null;
			code = code.Trim();
			if (code.StartsWith("OTON1.")) code = code.Substring(6);
			byte[] inb;
			try { inb = Convert.FromBase64String(code); } catch { return null; }
			if (inb.Length < 12 + 16) return null;
			byte[] nonce = new byte[12]; Buffer.BlockCopy(inb, 0, nonce, 0, 12);
			int ctLen = inb.Length - 12 - 16;
			byte[] ct = new byte[ctLen]; Buffer.BlockCopy(inb, 12, ct, 0, ctLen);
			byte[] tag = new byte[16]; Buffer.BlockCopy(inb, 12 + ctLen, tag, 0, 16);
			byte[] pt = new byte[ctLen];
			try { using var gcm = new AesGcm(APP_KEY, 16); gcm.Decrypt(nonce, ct, tag, pt); } catch { return null; }
			try { return JObject.Parse(Encoding.UTF8.GetString(pt)); } catch { return null; }
		}

		private static string LocalIPv4() {
			try {
				using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				s.Connect("8.8.8.8", 65530);   // no packet is sent; this just selects the primary outbound interface
				return ((IPEndPoint)s.LocalEndPoint).Address.ToString();
			} catch { return "127.0.0.1"; }
		}

		// ── internet play: public IP + UPnP (best effort) ───────────────────────────────────────────────
		// Discovered ONCE per process on a background thread (kicked at first LuaNetworking construction so
		// it is long done by the time a human hosts a room). The public IP goes into the room code ("pub")
		// and UPnP asks the router to forward the port — together they make direct internet hosting work on
		// cooperative routers. Hosts behind CGNAT still need a VPN; the join error says so.
		private static volatile string _publicIp;             // null until discovered (or undiscoverable)
		private static string _upnpControlUrl, _upnpServiceType;
		private static int _discoveryKicked;

		internal static void KickDiscovery() {
			if (Interlocked.Exchange(ref _discoveryKicked, 1) != 0) return;
			new Thread(() => {
				try {
					using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(4) };
					foreach (var url in new[] { "https://api.ipify.org", "https://checkip.amazonaws.com" }) {
						try {
							string s = http.GetStringAsync(url).GetAwaiter().GetResult().Trim();
							if (IPAddress.TryParse(s, out var a) && a.AddressFamily == AddressFamily.InterNetwork) { _publicIp = s; break; }
						} catch { }
					}
				} catch { }
				try { UpnpDiscover(); } catch { }
			}) { IsBackground = true, Name = "OTON-discover" }.Start();
		}

		private static void UpnpDiscover() {
			// SSDP M-SEARCH for an Internet Gateway Device, then fetch its control URL
			using var udp = new UdpClient();
			udp.Client.ReceiveTimeout = 2500;
			byte[] req = Encoding.ASCII.GetBytes(
				"M-SEARCH * HTTP/1.1\r\nHOST: 239.255.255.250:1900\r\nMAN: \"ssdp:discover\"\r\nMX: 2\r\n" +
				"ST: urn:schemas-upnp-org:device:InternetGatewayDevice:1\r\n\r\n");
			udp.Send(req, req.Length, new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900));
			var from = new IPEndPoint(IPAddress.Any, 0);
			string location = null;
			long until = Environment.TickCount64 + 2500;
			while (Environment.TickCount64 < until && location == null) {
				try {
					string resp = Encoding.ASCII.GetString(udp.Receive(ref from));
					foreach (var line in resp.Split('\n'))
						if (line.StartsWith("LOCATION:", StringComparison.OrdinalIgnoreCase)) { location = line.Substring(9).Trim(); break; }
				} catch { break; }
			}
			if (location == null) return;
			using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(4) };
			string xml = http.GetStringAsync(location).GetAwaiter().GetResult();
			var doc = System.Xml.Linq.XDocument.Parse(xml);
			foreach (var svc in doc.Descendants()) {
				if (!svc.Name.LocalName.Equals("service")) continue;
				string st = svc.Elements().FirstOrDefault(e => e.Name.LocalName == "serviceType")?.Value ?? "";
				if (!st.Contains("WANIPConnection") && !st.Contains("WANPPPConnection")) continue;
				string ctl = svc.Elements().FirstOrDefault(e => e.Name.LocalName == "controlURL")?.Value;
				if (string.IsNullOrEmpty(ctl)) continue;
				var baseUri = new Uri(location);
				_upnpControlUrl = new Uri(baseUri, ctl).ToString();
				_upnpServiceType = st;
				return;
			}
		}

		private static void UpnpSoap(string action, string args) {
			if (_upnpControlUrl == null) return;
			string body =
				"<?xml version=\"1.0\"?><s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" " +
				"s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\"><s:Body>" +
				$"<u:{action} xmlns:u=\"{_upnpServiceType}\">{args}</u:{action}></s:Body></s:Envelope>";
			using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(4) };
			var content = new System.Net.Http.StringContent(body, Encoding.UTF8, "text/xml");
			content.Headers.Add("SOAPACTION", $"\"{_upnpServiceType}#{action}\"");
			http.PostAsync(_upnpControlUrl, content).GetAwaiter().GetResult();
		}

		/// <summary>Asks the router to forward our port to this machine (fire-and-forget).</summary>
		private static void UpnpMapPort(int port) {
			new Thread(() => {
				try {
					UpnpSoap("AddPortMapping",
						$"<NewRemoteHost></NewRemoteHost><NewExternalPort>{port}</NewExternalPort><NewProtocol>TCP</NewProtocol>" +
						$"<NewInternalPort>{port}</NewInternalPort><NewInternalClient>{LocalIPv4()}</NewInternalClient>" +
						"<NewEnabled>1</NewEnabled><NewPortMappingDescription>OpenTaiko Online</NewPortMappingDescription><NewLeaseDuration>86400</NewLeaseDuration>");
				} catch { }
			}) { IsBackground = true, Name = "OTON-upnp" }.Start();
		}

		private static void UpnpUnmapPort(int port) {
			new Thread(() => {
				try {
					UpnpSoap("DeletePortMapping",
						$"<NewRemoteHost></NewRemoteHost><NewExternalPort>{port}</NewExternalPort><NewProtocol>TCP</NewProtocol>");
				} catch { }
			}) { IsBackground = true, Name = "OTON-upnp" }.Start();
		}

		/// <summary>All plausible host addresses, primary-outbound first. The room code carries every one
		/// of them because the right one depends on how the JOINER reaches us: over a VPN (Radmin, Tailscale,
		/// Hamachi…) the VPN adapter's IP is the reachable one, NOT the internet-facing adapter that the
		/// 8.8.8.8 trick selects — that mismatch used to make every VPN join die with "Host did not respond".</summary>
		private static List<string> LocalIPv4s() {
			var ips = new List<string> { LocalIPv4() };
			try {
				foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()) {
					if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
					if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback) continue;
					foreach (var ua in ni.GetIPProperties().UnicastAddresses) {
						if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
						string a = ua.Address.ToString();
						if (a.StartsWith("169.254.") || a == "127.0.0.1") continue;   // link-local / loopback
						if (!ips.Contains(a)) ips.Add(a);
					}
				}
			} catch { /* the primary alone still works */ }
			if (ips.Count > 5) ips.RemoveRange(5, ips.Count - 5);   // keep the room code short
			return ips;
		}
	}
}
