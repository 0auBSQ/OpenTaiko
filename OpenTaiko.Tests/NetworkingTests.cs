using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using Newtonsoft.Json.Linq;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// ── loopback integration tests for the OTON P2P core ─────────────────────────────────────────────
	// Each test runs a real room over 127.0.0.1 on its own port: real TCP, real handshake, real
	// AES-GCM frames — exactly the code path two machines would exercise, minus the LAN.
	[Collection("net")]   // sequential: every test owns its port + the wall clock
	public class NetworkingTests {
		public NetworkingTests() {
			// TjaParsingTests installs a process-wide ConfigIni (CTja needs one) whose DEFAULT gates
			// LuaNetworking off — which silently killed every networking test that ran after it.
			// The gate is a product safety default, not what these tests probe: force it open here.
			if (OpenTaiko.OpenTaiko.ConfigIni != null)
				OpenTaiko.OpenTaiko.ConfigIni.bAllowLuaNetworkingConnections = true;
		}

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

		// ── online play-sync transport (guards the in-game/results sync the lobby relies on) ──────────
		[Fact]
		public void PlaySync_RemoteSpot_LiveScoreComboAndFinishCounts_RoundTrip() {
			// Reproduces the play round end to end at the wire level: the host renders a remote player on a
			// virtual spot, that player streams its live score/combo + judge mix, then reports its final
			// authoritative tally. This is the exact path the results screen reads (great/good/bad, rolls,
			// balloons, adlibs, combo, score) — so it regression-guards the "judges/rolls/combo not synced"
			// fixes without needing the gameplay screen.
			int port = NextPort();
			var host = NewNet(port, "{\"name\":\"H\"}");
			var cli = NewNet(port, "{\"name\":\"C\"}");
			try {
				string code = host.CreateRoom("onlinelobby", "", 8);
				Assert.True(cli.JoinRoom(code));
				Assert.NotNull(WaitEvent(cli, "connected"));
				Assert.NotNull(WaitEvent(host, "joined"));
				Assert.True(WaitUntil(() => host.PeerCount() == 2 && cli.PeerCount() == 2));

				// host: spot 0 = itself (id 1), spot 1 = the remote client (id 2) — mirrors LO.launchPlay order
				host.SetPlaySpots("[1,2]");
				host.BeginPlaySync("H");
				cli.SetPlaySpots("[2,1]");
				cli.BeginPlaySync("C");
				Assert.True(host.IsRemoteSpot(1));
				Assert.False(host.IsRemoteSpot(0));

				// LIVE: client streams running score + combo + good/ok/bad → host snaps them onto spot 1
				cli.PushPlayScore("{\"n\":\"C\",\"s\":123456,\"g\":50.0,\"a\":97.5,\"gr\":80,\"gd\":15,\"ms\":5,\"co\":42}");
				Assert.True(WaitUntil(() => host.GetSpotPlayJson(1) != ""));
				var live = JObject.Parse(host.GetSpotPlayJson(1));
				Assert.Equal(123456, (long)live["s"]);
				Assert.Equal(42, (int)live["co"]);          // combo crosses the wire (was dropped before)
				Assert.Equal(50, host.GetSpotBadOdds(1));    // 5 / 100 → 50 per mille
				Assert.Equal(150, host.GetSpotGoodOdds(1));  // 15 / 100 → 150 per mille

				// a frame without the Timed fields (a player not under Timed, or an older build): no timer for the spot
				var (noTimer, noAdded) = OnlinePlaySync.ReadTimer(live);
				Assert.Null(noTimer);
				Assert.Null(noAdded);

				// TIMED: the owner's timer value and the shown added seconds ride the same frame; the host only reads them
				var timed = JObject.Parse("{\"n\":\"C\",\"s\":130000,\"g\":50.0,\"a\":97.5,\"gr\":85,\"gd\":15,\"ms\":5,\"co\":47}");
				OnlinePlaySync.WriteTimer(timed, (21500, 12));
				cli.PushPlayScore(timed.ToString(Newtonsoft.Json.Formatting.None));
				Assert.True(WaitUntil(() => host.GetSpotPlayJson(1).Contains("\"tm\"")));
				live = JObject.Parse(host.GetSpotPlayJson(1));
				var (msTimer, addedSeconds) = OnlinePlaySync.ReadTimer(live);
				Assert.Equal(21500, msTimer);
				Assert.Equal(12, addedSeconds);
				Assert.Equal(47, (int)live["co"]);           // the other fields are untouched

				// hidden digits (-1) with nothing added, and a player not under Timed (no field written)
				var held = new JObject();
				OnlinePlaySync.WriteTimer(held, (-1, null));
				Assert.Equal(-1, OnlinePlaySync.ReadTimer(held).msElapsed);
				Assert.Null(OnlinePlaySync.ReadTimer(held).addedSeconds);
				var none = new JObject();
				OnlinePlaySync.WriteTimer(none, null);
				Assert.False(none.HasValues);

				// FAILED: nothing is written before the failure (so the frames above carry none), then "f" rides the same frame
				Assert.False(OnlinePlaySync.ReadFailed(live));
				OnlinePlaySync.WriteFailed(none, false);
				Assert.False(none.HasValues);
				var failed = JObject.Parse("{\"n\":\"C\",\"s\":130000,\"g\":0.0,\"a\":97.5,\"gr\":85,\"gd\":15,\"ms\":6,\"co\":0}");
				OnlinePlaySync.WriteFailed(failed, true);
				cli.PushPlayScore(failed.ToString(Newtonsoft.Json.Formatting.None));
				Assert.True(WaitUntil(() => host.GetSpotPlayJson(1).Contains("\"f\"")));
				Assert.True(OnlinePlaySync.ReadFailed(JObject.Parse(host.GetSpotPlayJson(1))));

				// the receiver parses a frame once: the same string comes back until the next frame arrives
				string kept = host.GetSpotPlayJson(1);
				Assert.Same(kept, host.GetSpotPlayJson(1));
				Assert.True(OnlinePlaySync.TryParseFrame(kept, out var frame));
				Assert.Equal(new OnlinePlaySync.PlayFrame(130000, 0.0, 0, null, null, true), frame);

				// FINISH: client reports its real final tally → host can read every result field for that spot
				cli.ReportFinished("{\"cl\":true,\"fc\":false,\"pf\":false,\"mx\":false,\"gr\":820,\"gd\":30,\"ms\":4,\"rl\":12,\"bl\":3,\"ad\":2,\"hc\":777,\"sc\":998877}");
				Assert.True(WaitUntil(() => host.GetSpotJudge(1, "gr") >= 0));
				Assert.Equal(820, host.GetSpotJudge(1, "gr"));
				Assert.Equal(30, host.GetSpotJudge(1, "gd"));
				Assert.Equal(4, host.GetSpotJudge(1, "ms"));
				Assert.Equal(12, host.GetSpotJudge(1, "rl"));    // rolls
				Assert.Equal(3, host.GetSpotJudge(1, "bl"));     // balloon hits
				Assert.Equal(2, host.GetSpotJudge(1, "ad"));     // adlibs
				Assert.Equal(777, host.GetSpotJudge(1, "hc"));   // highest combo
				Assert.Equal(998877, host.GetSpotJudge(1, "sc")); // final score
				Assert.Equal(1, host.GetSpotClearLevel(1));      // cl=true, mx=false → cleared (not rainbow)
			} finally { cli.Leave(); host.Leave(); }
		}

		// ── the parsed "ps" frame and the remote combo rule ───────────────────────────────────────────
		[Fact]
		public void PlayFrame_ReadsEveryField_AndLeavesOutTheMissingOnes() {
			var full = JObject.Parse("{\"n\":\"C\",\"s\":130000,\"g\":62.5,\"a\":97.5,\"gr\":85,\"gd\":15,\"ms\":5,\"co\":47}");
			OnlinePlaySync.WriteTimer(full, (21500, 12));
			OnlinePlaySync.WriteFailed(full, true);
			Assert.True(OnlinePlaySync.TryParseFrame(full.ToString(Newtonsoft.Json.Formatting.None), out var frame));
			Assert.Equal(new OnlinePlaySync.PlayFrame(130000, 62.5, 47, 21500, 12, true), frame);

			// an older build's frame, or a player not under Timed and not failed
			Assert.True(OnlinePlaySync.TryParseFrame("{\"n\":\"C\",\"s\":10}", out frame));
			Assert.Equal(new OnlinePlaySync.PlayFrame(10, null, null, null, null, false), frame);

			Assert.False(OnlinePlaySync.TryParseFrame("{\"s\":", out _));
			Assert.False(OnlinePlaySync.TryParseFrame("{\"s\":\"x\"}", out _));
		}

		[Theory]
		[InlineData(52, 3, 50, 3)]      // the wire dropped: the lane drops to it
		[InlineData(2, 3, 50, 3)]
		[InlineData(0, 0, 50, 0)]
		[InlineData(40, 47, 42, 47)]    // the lane is behind a new wire value: it rises to it
		[InlineData(0, 47, 42, 47)]
		[InlineData(52, 47, 42, 52)]    // the lane is ahead: it keeps counting
		[InlineData(47, 47, 42, 47)]
		[InlineData(60, 47, 47, 60)]    // the wire did not change: nothing happens, in either direction
		[InlineData(0, 47, 47, 0)]
		[InlineData(5, 0, 0, 5)]
		public void FollowCombo_ReactsOnlyToANewWireValue(int local, int wire, int lastWire, int expected) {
			Assert.Equal(expected, OnlinePlaySync.FollowCombo(local, wire, lastWire));
		}

		[Theory]
		[InlineData(0, 12, 12)]         // the first frame raises a lane that is behind
		[InlineData(15, 12, 15)]        // and leaves one that is ahead
		[InlineData(3, 0, 3)]
		public void FollowCombo_FirstFrame(int local, int wire, int expected) {
			Assert.Equal(expected, OnlinePlaySync.FollowCombo(local, wire, null));
		}

		[Theory]
		[InlineData(3, 50, true)]
		[InlineData(50, 50, false)]
		[InlineData(51, 50, false)]
		[InlineData(0, null, false)]     // the first frame is never a drop
		public void WireDropped_OnlyWhenTheWireWentDown(int wire, int? lastWire, bool expected) {
			Assert.Equal(expected, OnlinePlaySync.WireDropped(wire, lastWire));
		}

		// a new connection numbers its rounds from 1 again: the same number on another connection is a new round
		[Fact]
		public void IsNewRound_SeesANewNumberOrANewConnection() {
			var a = new LuaNetworking();
			var b = new LuaNetworking();
			try {
				a.BeginPlaySync("A");
				b.BeginPlaySync("B");
				Assert.Equal(a.PlaySyncEpoch, b.PlaySyncEpoch);
				Assert.True(OnlinePlaySync.IsNewRound(null, -1, a));
				Assert.False(OnlinePlaySync.IsNewRound(a, a.PlaySyncEpoch, a));
				Assert.True(OnlinePlaySync.IsNewRound(a, a.PlaySyncEpoch, b));
				int first = a.PlaySyncEpoch;
				a.BeginPlaySync("A");
				Assert.True(OnlinePlaySync.IsNewRound(a, first, a));
			} finally { a.Leave(); b.Leave(); }
		}

		// ── pure helpers (privacy + zero-config rendezvous agreement) ─────────────────────────────────
		[Theory]
		[InlineData("198.51.100.23", "1x.x.x.x")]
		[InlineData("203.0.113.7:41234", "2x.x.x.x")]      // port stripped, then masked
		[InlineData("2001:db8:abcd:12::1", "2x:x")]
		[InlineData("[2001:db8::5]:41234", "2x:x")]        // bracketed v6 with port
		public void MaskIp_KeepsOnlyTheFirstDigit(string input, string expected) {
			Assert.Equal(expected, LuaNetworking.MaskIp(input));
		}

		[Fact]
		public void DerivedSignalBroker_IsDeterministicPerToken() {
			// host and joiner derive the rendezvous broker from the room token alone (no coordination),
			// so the same token MUST always map to the same broker, and always to a real configured one.
			string a = LuaNetworking.DerivedSignalBroker("abc123");
			Assert.Equal(a, LuaNetworking.DerivedSignalBroker("abc123"));   // stable
			Assert.Contains(a, MiniMqtt.PublicBrokers);                     // a real broker
			// different tokens generally land on (any of) the configured brokers — never crash / empty
			foreach (var tok in new[] { "", "0", "ffffff", "deadbeef", "OTON" })
				Assert.Contains(LuaNetworking.DerivedSignalBroker(tok), MiniMqtt.PublicBrokers);
		}
	}

	[CollectionDefinition("net", DisableParallelization = true)]
	public class NetCollection { }
}