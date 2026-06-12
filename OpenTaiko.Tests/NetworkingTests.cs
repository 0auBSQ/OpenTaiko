using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// ── loopback integration tests for the OTON P2P core ─────────────────────────────────────────────
	// Each test runs a real room over 127.0.0.1 on its own port: real TCP, real handshake, real
	// AES-GCM frames — exactly the code path two machines would exercise, minus the LAN.
	[Collection("net")]   // sequential: every test owns its port + the wall clock
	public class NetworkingTests {
		private static int _portSeq = 42400;
		private static int NextPort() => Interlocked.Increment(ref _portSeq);

		private static LuaNetworking NewNet(int port, string info = "{}") {
			var n = new LuaNetworking { PortOverride = port };
			n.SetLocalPlayer(info);
			return n;
		}

		/// <summary>Drain Poll() until an event of the given type arrives (or time runs out).</summary>
		private static NetEvent WaitEvent(LuaNetworking n, string type, int timeoutMs = 5000) {
			var sw = Stopwatch.StartNew();
			while (sw.ElapsedMilliseconds < timeoutMs) {
				var e = n.Poll();
				if (e != null) { if (e.Type == type) return e; continue; }
				Thread.Sleep(15);
			}
			return null;
		}

		private static bool WaitUntil(Func<bool> cond, int timeoutMs = 5000) {
			var sw = Stopwatch.StartNew();
			while (sw.ElapsedMilliseconds < timeoutMs) {
				if (cond()) return true;
				Thread.Sleep(25);
			}
			return false;
		}

		[Fact]
		public void HostAndJoin_RosterEventsAndIds() {
			int port = NextPort();
			var host = NewNet(port, "{\"name\":\"H\"}");
			var cli = NewNet(port, "{\"name\":\"C\"}");
			try {
				string code = host.CreateRoom("onlinelobby", "pay", 8);
				Assert.False(string.IsNullOrEmpty(code));
				Assert.Equal("onlinelobby", host.PeekStageId(code));
				Assert.True(cli.JoinRoom(code));
				Assert.NotNull(WaitEvent(cli, "connected"));
				Assert.NotNull(WaitEvent(host, "joined"));
				Assert.Equal(1, host.SelfId());
				Assert.Equal(2, cli.SelfId());
				Assert.True(WaitUntil(() => host.PeerCount() == 2 && cli.PeerCount() == 2));
				// info is embedded as a JSON *string* value, so its quotes arrive escaped
				Assert.Contains("\\\"name\\\":\\\"C\\\"", host.PeersJson());
			} finally { cli.Leave(); host.Leave(); }
		}

		[Fact]
		public void Broadcast_ReachesEveryoneButSender() {
			int port = NextPort();
			var host = NewNet(port); var a = NewNet(port); var b = NewNet(port);
			try {
				string code = host.CreateRoom("s", "", 8);
				a.JoinRoom(code); Assert.NotNull(WaitEvent(a, "connected"));
				b.JoinRoom(code); Assert.NotNull(WaitEvent(b, "connected"));
				a.Broadcast("chat", "hello");
				var eh = WaitEvent(host, "message"); var eb = WaitEvent(b, "message");
				Assert.NotNull(eh); Assert.NotNull(eb);
				Assert.Equal("chat", eh.Channel); Assert.Equal("hello", eh.Data); Assert.Equal(2, eh.Peer);
				Assert.Equal("hello", eb.Data); Assert.Equal(2, eb.Peer);   // relayed with the true sender id
				Assert.Null(WaitEvent(a, "message", 600));                  // not echoed to the sender
			} finally { b.Leave(); a.Leave(); host.Leave(); }
		}

		[Fact]
		public void SendTo_IsTargeted() {
			int port = NextPort();
			var host = NewNet(port); var a = NewNet(port); var b = NewNet(port);
			try {
				string code = host.CreateRoom("s", "", 8);
				a.JoinRoom(code); WaitEvent(a, "connected");
				b.JoinRoom(code); WaitEvent(b, "connected");
				host.SendTo(3, "dm", "justB");                              // b is the second joiner → id 3
				Assert.NotNull(WaitEvent(b, "message"));
				Assert.Null(WaitEvent(a, "message", 600));
			} finally { b.Leave(); a.Leave(); host.Leave(); }
		}

		[Fact]
		public void RoomFull_IsRejected() {
			int port = NextPort();
			var host = NewNet(port); var a = NewNet(port); var b = NewNet(port);
			try {
				string code = host.CreateRoom("s", "", 2);                  // host + 1
				a.JoinRoom(code); Assert.NotNull(WaitEvent(a, "connected"));
				b.JoinRoom(code);
				var e = WaitEvent(b, "error");
				Assert.NotNull(e);
				Assert.Contains("full", e.Data, StringComparison.OrdinalIgnoreCase);
			} finally { b.Leave(); a.Leave(); host.Leave(); }
		}

		[Fact]
		public void NonProtocolClient_IsIgnored() {
			int port = NextPort();
			var host = NewNet(port);
			try {
				string code = host.CreateRoom("s", "", 8);
				using (var raw = new TcpClient("127.0.0.1", port)) {        // port-scanner / wrong game
					var garbage = new byte[64];
					new Random(7).NextBytes(garbage);
					raw.GetStream().Write(garbage, 0, garbage.Length);
				}
				// the room must shrug it off and still accept a real joiner
				var cli = NewNet(port);
				cli.JoinRoom(code);
				Assert.NotNull(WaitEvent(cli, "connected"));
				cli.Leave();
			} finally { host.Leave(); }
		}

		[Fact]
		public void HostRole_RotatesInJoinOrder() {
			int port = NextPort();
			var host = NewNet(port); var a = NewNet(port);
			try {
				string code = host.CreateRoom("s", "", 8);
				a.JoinRoom(code); WaitEvent(a, "connected");
				Assert.True(host.HasHostRole());
				host.RotateHost();
				Assert.True(WaitUntil(() => a.HostRoleId() == 2));
				Assert.True(a.HasHostRole());
				host.RotateHost();
				Assert.True(WaitUntil(() => host.HostRoleId() == 1));       // wraps back to the host
			} finally { a.Leave(); host.Leave(); }
		}

		[Fact]
		public void OversizedHello_IsCappedNotRelayed() {
			int port = NextPort();
			var host = NewNet(port);
			var big = NewNet(port, "{\"pad\":\"" + new string('x', 200000) + "\"}");
			try {
				string code = host.CreateRoom("s", "", 8);
				big.JoinRoom(code);
				Assert.NotNull(WaitEvent(big, "connected"));
				Assert.True(WaitUntil(() => host.PeerCount() == 2));
				Assert.DoesNotContain("xxxxxxxxxx", host.PeersJson());      // the 200KB blob was dropped
			} finally { big.Leave(); host.Leave(); }
		}

		[Fact]
		public void LoadBarrier_ToleratesEarlyLd() {
			// THE race this protects: a fast peer reports "ld" before the slow peer's BeginPlaySync.
			int port = NextPort();
			var host = NewNet(port); var a = NewNet(port);
			try {
				string code = host.CreateRoom("s", "", 8);
				a.JoinRoom(code); WaitEvent(a, "connected");
				WaitUntil(() => host.PeerCount() == 2);
				host.BeginPlaySync("H");
				host.ReportLoaded();                                        // arrives at `a` BEFORE its Begin
				Thread.Sleep(400);
				a.BeginPlaySync("A");                                       // must NOT wipe the early report
				a.ReportLoaded();
				Assert.True(WaitUntil(() => a.LoadBarrierReady(30000) && host.LoadBarrierReady(30000), 5000));
				host.EndPlaySync(); a.EndPlaySync();
			} finally { a.Leave(); host.Leave(); }
		}

		[Fact]
		public void Migration_GracefulHostLeave_SuccessorTakesOver() {
			int port = NextPort();
			var host = NewNet(port, "{\"name\":\"H\"}");
			var a = NewNet(port, "{\"name\":\"A\"}");
			var b = NewNet(port, "{\"name\":\"B\"}");
			try {
				string code = host.CreateRoom("stage", "meta", 8);
				a.JoinRoom(code); Assert.NotNull(WaitEvent(a, "connected"));
				b.JoinRoom(code); Assert.NotNull(WaitEvent(b, "connected"));
				WaitUntil(() => host.PeerCount() == 3);
				host.Leave();                                               // deliberate handover (OP_MIGRATE)
				// a (lowest surviving id = 2) must become the room's socket owner
				Assert.True(WaitUntil(() => a.IsHost(), 15000), "successor did not take over");
				// b must reconnect to the successor with its identity intact
				Assert.True(WaitUntil(() => b.Connected() && b.SelfId() == 3 && a.PeerCount() == 2, 20000),
					"survivor did not rejoin the migrated room");
				// the migrated room still works end to end
				b.Broadcast("chat", "post-migration");
				var e = WaitEvent(a, "message", 5000);
				Assert.NotNull(e); Assert.Equal("post-migration", e.Data);
			} finally { b.Leave(); a.Leave(); host.Leave(); }
		}

		// ── in-process rendezvous bus (stands in for the public MQTT broker) ──────────────────────
		// pinned for the whole class: keeps every test hermetic (no real broker connections) and makes
		// CreateRoom's broker-pin wait instant
		static NetworkingTests() {
			OpenTaiko.LuaNetworking.SignalFactory = _ => new MemorySignal();
		}

		private sealed class MemorySignal : OpenTaiko.LuaNetworking.IOtonSignal {
			private static readonly Dictionary<string, List<Action<byte[]>>> _bus = new();
			private readonly List<(string, Action<byte[]>)> _mine = new();
			public bool Open() => true;
			public bool Alive => true;
			public string Broker => "memory";
			public void Listen(string channel, Action<byte[]> onMessage) {
				lock (_bus) {
					if (!_bus.TryGetValue(channel, out var l)) _bus[channel] = l = new List<Action<byte[]>>();
					l.Add(onMessage); _mine.Add((channel, onMessage));
				}
			}
			public void Send(string channel, byte[] payload) {
				Action<byte[]>[] subs;
				lock (_bus) subs = _bus.TryGetValue(channel, out var l) ? l.ToArray() : Array.Empty<Action<byte[]>>();
				foreach (var s in subs) System.Threading.Tasks.Task.Run(() => s(payload));
			}
			public void Dispose() {
				lock (_bus) foreach (var (c, a) in _mine) if (_bus.TryGetValue(c, out var l)) l.Remove(a);
			}
		}

		[Fact]
		public void P2PJoin_HostConnectsBackThroughRendezvous() {
			// the pure-P2P internet path: the joiner has NO direct route (forced), publishes its
			// candidates on the rendezvous channel, and the HOST dials back; handshake + traffic
			// then flow over that reverse-established direct socket.
			int port = NextPort();
			var host = NewNet(port, "{\"name\":\"H\"}");
			var cli = NewNet(port, "{\"name\":\"C\"}");
			try {
				string code = host.CreateRoom("p2pstage", "pay", 8);
				Assert.False(string.IsNullOrEmpty(code));
				cli.ForceP2POnly = true;                       // no direct candidates, no relay
				Assert.True(cli.JoinRoom(code));
				Assert.NotNull(WaitEvent(cli, "connected", 15000));
				Assert.NotNull(WaitEvent(host, "joined", 15000));
				Assert.True(WaitUntil(() => host.PeerCount() == 2 && cli.PeerCount() == 2));
				cli.Broadcast("chat", "reverse-p2p");
				var e = WaitEvent(host, "message");
				Assert.NotNull(e); Assert.Equal("reverse-p2p", e.Data); Assert.Equal(2, e.Peer);
				host.SendTo(2, "dm", "direct-back");
				var e2 = WaitEvent(cli, "message");
				Assert.NotNull(e2); Assert.Equal("direct-back", e2.Data);
			} finally { cli.Leave(); host.Leave(); }
		}

		[Fact]
		public void RoomCode_TamperAndGarbage_AreRejected() {
			int port = NextPort();
			var host = NewNet(port);
			try {
				string code = host.CreateRoom("stagex", "", 8);
				Assert.Equal("stagex", host.PeekStageId(code));
				char[] chars = code.ToCharArray();
				chars[code.Length / 2] = chars[code.Length / 2] == 'A' ? 'B' : 'A';
				Assert.Null(host.PeekStageId(new string(chars)));           // GCM tag fails → invalid
				Assert.Null(host.PeekStageId("not a code at all"));
				Assert.False(host.JoinRoom("OTON1.garbage!!"));
			} finally { host.Leave(); }
		}
	}

	[CollectionDefinition("net", DisableParallelization = true)]
	public class NetCollection { }
}