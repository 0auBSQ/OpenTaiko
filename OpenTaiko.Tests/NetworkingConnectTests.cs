using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// ── the connect path of the OTON core: signalling links, broker fallback, repeated offers, bind
	// fallback, failure reasons and session lifetime. Loopback only: the brokers are an in-process bus
	// or a local TcpListener that speaks just enough MQTT.
	[Collection("net")]
	public class NetworkingConnectTests : IDisposable {
		private static readonly string[] Brokers = { "a", "b", "c" };
		private readonly Func<IPAddress, int, TcpListener> _listenerFactory = LuaNetworking.ListenerFactory;

		public NetworkingConnectTests() {
			if (OpenTaiko.OpenTaiko.ConfigIni != null)
				OpenTaiko.OpenTaiko.ConfigIni.bAllowLuaNetworkingConnections = true;
			Bus.Reset();
			LuaNetworking.SignalFactory = b => new BusSignal(b);
		}

		public void Dispose() {
			Bus.Reset();
			LuaNetworking.SignalFactory = b => new BusSignal(b);
			LuaNetworking.ListenerFactory = _listenerFactory;
		}

		private static int _portSeq = 42600;
		private static int NextPort() => Interlocked.Increment(ref _portSeq);

		private static LuaNetworking NewNet(int port, params string[] brokers) {
			var n = new LuaNetworking {
				PortOverride = port,
				SignalBrokers = brokers.Length > 0 ? brokers : Brokers,
				JoinTimeoutMs = 4000, P2PTimeoutMs = 3000, OfferRepeatMs = 150, SignalCheckMs = 50, SignalRetryMs = 100,
			};
			n.SetLocalPlayer("{}");
			return n;
		}

		private static NetEvent WaitEvent(LuaNetworking n, string type, int timeoutMs = 5000) {
			var sw = Stopwatch.StartNew();
			while (sw.ElapsedMilliseconds < timeoutMs) {
				var e = n.Poll();
				if (e != null) { if (e.Type == type) return e; continue; }
				Thread.Sleep(15);
			}
			return null;
		}

		/// <summary>Every event that arrives within the time.</summary>
		private static List<NetEvent> Collect(LuaNetworking n, int ms) {
			var got = new List<NetEvent>();
			var sw = Stopwatch.StartNew();
			while (sw.ElapsedMilliseconds < ms) {
				var e = n.Poll();
				if (e != null) got.Add(e); else Thread.Sleep(15);
			}
			return got;
		}

		private static bool WaitUntil(Func<bool> cond, int timeoutMs = 5000) {
			var sw = Stopwatch.StartNew();
			while (sw.ElapsedMilliseconds < timeoutMs) {
				if (cond()) return true;
				Thread.Sleep(20);
			}
			return false;
		}

		// ── the in-process brokers: one bus per broker name; a broker can be down, and sends can be lost ──
		private static class Bus {
			public static readonly Dictionary<string, List<Action<byte[]>>> Subs = new();
			public static readonly HashSet<string> Down = new();
			public static int DropSends;                 // this many of the next sends are lost
			public static int Sends;
			public static void Reset() { lock (Subs) { Subs.Clear(); Down.Clear(); DropSends = 0; Sends = 0; } }
			/// <summary>The broker stops answering: its links die and it refuses new ones.</summary>
			public static void Kill(string broker) {
				lock (Subs) {
					Down.Add(broker);
					foreach (var k in Subs.Keys.Where(k => k.StartsWith(broker + "|")).ToList()) Subs.Remove(k);
				}
			}
			public static bool IsDown(string broker) { lock (Subs) return Down.Contains(broker); }
		}

		private sealed class BusSignal : LuaNetworking.IOtonSignal {
			private readonly string _broker;
			private readonly List<(string, Action<byte[]>)> _mine = new();
			public BusSignal(string broker) { _broker = broker ?? ""; }
			public bool Open() => !Bus.IsDown(_broker);
			public bool Alive => !Bus.IsDown(_broker);
			public string Broker => _broker;
			public void Listen(string channel, Action<byte[]> onMessage) {
				string key = _broker + "|" + channel;
				lock (Bus.Subs) {
					if (!Bus.Subs.TryGetValue(key, out var l)) Bus.Subs[key] = l = new List<Action<byte[]>>();
					l.Add(onMessage); _mine.Add((key, onMessage));
				}
			}
			public void Send(string channel, byte[] payload) {
				Action<byte[]>[] subs;
				lock (Bus.Subs) {
					Bus.Sends++;
					if (Bus.Down.Contains(_broker)) return;
					if (Bus.DropSends > 0) { Bus.DropSends--; return; }
					subs = Bus.Subs.TryGetValue(_broker + "|" + channel, out var l) ? l.ToArray() : Array.Empty<Action<byte[]>>();
				}
				foreach (var s in subs) System.Threading.Tasks.Task.Run(() => s(payload));
			}
			public void Dispose() {
				lock (Bus.Subs) foreach (var (k, a) in _mine) if (Bus.Subs.TryGetValue(k, out var l)) l.Remove(a);
			}
		}

		// ── a local broker that speaks just enough MQTT 3.1.1 for MiniMqtt ───────────────────────────────
		private sealed class FakeBroker : IDisposable {
			private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
			private readonly List<TcpClient> _clients = new();
			private volatile bool _stop;
			public readonly int Port;
			public int Connections, Subscribes;
			public volatile string LastTopic = "";
			public volatile bool AnswerPings = true;
			/// <summary>Whether the nth connection (from 1) is closed right after its SUBSCRIBE is acknowledged.</summary>
			public Func<int, bool> DropAfterSubscribe = _ => false;
			public string Address => "127.0.0.1:" + Port;

			public FakeBroker() {
				_listener.Start();
				Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
				new Thread(() => {
					while (!_stop) {
						TcpClient c;
						try { c = _listener.AcceptTcpClient(); } catch { break; }
						int n = Interlocked.Increment(ref Connections);
						lock (_clients) _clients.Add(c);
						new Thread(() => Serve(c, n)) { IsBackground = true }.Start();
					}
				}) { IsBackground = true }.Start();
			}

			/// <summary>Closes every connection; the broker keeps accepting new ones.</summary>
			public void CloseConnections() {
				lock (_clients) { foreach (var c in _clients) { try { c.Close(); } catch { } } _clients.Clear(); }
			}

			private void Serve(TcpClient c, int n) {
				try {
					var ns = c.GetStream();
					while (!_stop) {
						if (!c.Client.Poll(50000, SelectMode.SelectRead)) continue;
						if (c.Client.Available == 0) return;                 // closed by the client
						int h = ns.ReadByte();
						int len = 0, mult = 1;
						for (int i = 0; i < 4; i++) { int d = ns.ReadByte(); len += (d & 0x7F) * mult; if ((d & 0x80) == 0) break; mult *= 128; }
						byte[] body = new byte[len];
						for (int off = 0; off < len;) { int r = ns.Read(body, off, len - off); if (r <= 0) return; off += r; }
						switch (h & 0xF0) {
							case 0x10: ns.Write(new byte[] { 0x20, 0x02, 0x00, 0x00 }, 0, 4); break;
							case 0x80: {
								int tl = (body[2] << 8) | body[3];
								LastTopic = Encoding.UTF8.GetString(body, 4, tl);
								ns.Write(new byte[] { 0x90, 0x03, body[0], body[1], 0x00 }, 0, 5);
								Interlocked.Increment(ref Subscribes);
								if (DropAfterSubscribe(n)) return;
								break;
							}
							case 0xC0: if (AnswerPings) ns.Write(new byte[] { 0xD0, 0x00 }, 0, 2); break;
						}
					}
				} catch { } finally { try { c.Close(); } catch { } }
			}

			public void Dispose() { _stop = true; try { _listener.Stop(); } catch { } CloseConnections(); }
		}

		// ── the signalling link notices its own death ────────────────────────────────────────────────────
		[Fact]
		public void Mqtt_IsNoLongerAlive_OnceTheBrokerClosesTheConnection() {
			using var broker = new FakeBroker();
			using var m = new MiniMqtt();
			Assert.True(m.Connect(new[] { broker.Address }, 2000));
			m.Subscribe("t", _ => { });
			Assert.True(WaitUntil(() => broker.Subscribes == 1));
			Assert.True(m.Alive);
			broker.CloseConnections();
			Assert.True(WaitUntil(() => !m.Alive, 3000));
		}

		[Fact]
		public void Mqtt_IsNoLongerAlive_WhenThePingsGoUnanswered() {
			using var broker = new FakeBroker();
			using var answered = new MiniMqtt { PingIntervalMs = 100, PongTimeoutMs = 300 };
			Assert.True(answered.Connect(new[] { broker.Address }, 2000));
			Thread.Sleep(1000);                                // several pings, each answered
			Assert.True(answered.Alive);

			broker.AnswerPings = false;
			using var silent = new MiniMqtt { PingIntervalMs = 100, PongTimeoutMs = 300 };
			Assert.True(silent.Connect(new[] { broker.Address }, 2000));
			Assert.True(silent.Alive);
			Assert.True(WaitUntil(() => !silent.Alive, 3000));
			Assert.True(WaitUntil(() => !answered.Alive, 3000));   // the first link stops being answered too
		}

		[Fact]
		public void Host_SubscribesAgain_AfterTheBrokerDropsItsLink() {
			using var broker = new FakeBroker { DropAfterSubscribe = n => n == 1 };
			LuaNetworking.SignalFactory = b => new LuaNetworking.MqttSignal(b);
			var host = NewNet(NextPort(), broker.Address);
			try {
				Assert.False(string.IsNullOrEmpty(host.CreateRoom("s", "", 8)));
				Assert.Equal(broker.Address, host.RoomBroker);
				// the first link is closed under the host: it opens a second one and subscribes on it
				Assert.True(WaitUntil(() => broker.Connections >= 2 && broker.Subscribes >= 2, 6000));
				Assert.StartsWith("oton/v1/", broker.LastTopic);
				Thread.Sleep(400);
				Assert.Equal(2, broker.Connections);                 // and keeps that one
			} finally { host.Leave(); }
		}

		// ── more than one broker ─────────────────────────────────────────────────────────────────────────
		[Fact]
		public void Join_CompletesThroughAnotherBroker_WhenTheRoomsOwnIsDown() {
			int port = NextPort();
			var host = NewNet(port); var cli = NewNet(port);
			try {
				string code = host.CreateRoom("s", "", 8);
				Assert.Contains(host.RoomBroker, Brokers);
				Bus.Kill(host.RoomBroker);                           // the broker named in the code is gone
				cli.ForceP2POnly = true;
				Assert.True(cli.JoinRoom(code));
				Assert.NotNull(WaitEvent(cli, "connected"));
				Assert.True(WaitUntil(() => host.PeerCount() == 2 && cli.PeerCount() == 2));
				// the offer came through two brokers and more than once: one joiner all the same
				Assert.Single(Collect(host, 600).Where(e => e.Type == "joined"));
				Assert.Equal(2, host.PeerCount());
			} finally { cli.Leave(); host.Leave(); }
		}

		[Fact]
		public void Host_WarnsOnce_WhenNoBrokerAnswers() {
			foreach (string b in Brokers) Bus.Kill(b);
			var host = NewNet(NextPort());
			try {
				Assert.False(string.IsNullOrEmpty(host.CreateRoom("s", "", 8)));
				var errors = Collect(host, 1200).Where(e => e.Type == "error").ToList();   // a dozen failed rounds
				Assert.Single(errors);
				Assert.Contains("matchmaking service is unreachable", errors[0].Data);
			} finally { host.Leave(); }
		}

		[Fact]
		public void Join_SaysTheMatchmakingServiceIsUnreachable_WhenNoBrokerAnswers() {
			int port = NextPort();
			var host = NewNet(port); var cli = NewNet(port);
			try {
				string code = host.CreateRoom("s", "", 8);
				foreach (string b in Brokers) Bus.Kill(b);
				cli.ForceP2POnly = true;
				Assert.True(cli.JoinRoom(code));
				var e = WaitEvent(cli, "error");
				Assert.NotNull(e);
				Assert.Equal(LuaNetworking.JoinFailMessage(LuaNetworking.JoinFail.SignalUnreachable, null), e.Data);
			} finally { cli.Leave(); host.Leave(); }
		}

		// ── the offer is published again ─────────────────────────────────────────────────────────────────
		[Fact]
		public void Join_Completes_WhenTheFirstOfferIsLost() {
			int port = NextPort();
			var host = NewNet(port, "a"); var cli = NewNet(port, "a");
			try {
				string code = host.CreateRoom("s", "", 8);
				Bus.DropSends = 1;                                   // the joiner's first publish
				cli.ForceP2POnly = true;
				Assert.True(cli.JoinRoom(code));
				Assert.NotNull(WaitEvent(cli, "connected"));
				Assert.True(WaitUntil(() => host.PeerCount() == 2));
				Assert.Single(Collect(host, 600).Where(e => e.Type == "joined"));   // the later repeats add nobody
			} finally { cli.Leave(); host.Leave(); }
		}

		// ── the listener without IPv6 ────────────────────────────────────────────────────────────────────
		[Fact]
		public void Listeners_FallBackToIPv4_WhenTheDualStackBindFails() {
			var binds = new List<IPAddress>();
			LuaNetworking.ListenerFactory = (addr, p) => {
				lock (binds) binds.Add(addr);
				if (addr.Equals(IPAddress.IPv6Any)) throw new SocketException((int)SocketError.AddressFamilyNotSupported);
				return _listenerFactory(addr, p);
			};
			int port = NextPort();
			var host = NewNet(port); var direct = NewNet(port); var p2p = NewNet(port);
			try {
				string code = host.CreateRoom("s", "", 8);
				Assert.False(string.IsNullOrEmpty(code));
				lock (binds) Assert.Equal(new[] { IPAddress.IPv6Any, IPAddress.Any }, binds);
				Assert.True(direct.JoinRoom(code));
				Assert.NotNull(WaitEvent(direct, "connected"));
				p2p.ForceP2POnly = true;                             // its rendezvous listener falls back as well
				Assert.True(p2p.JoinRoom(code));
				Assert.NotNull(WaitEvent(p2p, "connected"));
				Assert.True(WaitUntil(() => host.PeerCount() == 3));
			} finally { p2p.Leave(); direct.Leave(); host.Leave(); }
		}

		[Fact]
		public void CreateRoom_OnAPortInUse_FailsWithTheReason() {
			int port = NextPort();
			var first = NewNet(port); var second = NewNet(port);
			try {
				Assert.False(string.IsNullOrEmpty(first.CreateRoom("s", "", 8)));
				Assert.Null(second.CreateRoom("s", "", 8));
				var e = WaitEvent(second, "error", 1000);
				Assert.NotNull(e);
				Assert.StartsWith("Could not host: ", e.Data);
				Assert.False(second.IsHost());
			} finally { second.Leave(); first.Leave(); }
		}

		// ── failure reasons ──────────────────────────────────────────────────────────────────────────────
		[Fact]
		public void Join_WithTheCodeOfAnEarlierRoom_SaysSo_AndANewCodeThenWorks() {
			int port = NextPort();
			var host = NewNet(port); var cli = NewNet(port);
			cli.P2PTimeoutMs = 600;
			try {
				string old = host.CreateRoom("s", "", 8);
				host.Leave();
				string code = host.CreateRoom("s", "", 8);           // same port, another room key
				Assert.True(cli.JoinRoom(old));
				var e = WaitEvent(cli, "error");
				Assert.NotNull(e);
				Assert.Equal(LuaNetworking.JoinFailMessage(LuaNetworking.JoinFail.StaleCode, null), e.Data);
				Assert.Contains("older room", e.Data);
				Assert.Equal(1, host.PeerCount());

				// the failed join leaves nothing behind: the next one gives the host exactly one more player
				Assert.True(cli.JoinRoom(code));
				Assert.NotNull(WaitEvent(cli, "connected"));
				Assert.True(WaitUntil(() => host.PeerCount() == 2));
				Assert.Single(Collect(host, 800).Where(ev => ev.Type == "joined"));
				Assert.Equal(2, host.PeerCount());
				Assert.DoesNotContain(Collect(cli, 200), ev => ev.Type == "error");
			} finally { cli.Leave(); host.Leave(); }
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void Join_DuringASong_IsRejectedWithTheReason(bool p2pOnly) {
			int port = NextPort();
			var host = NewNet(port); var cli = NewNet(port);
			try {
				string code = host.CreateRoom("s", "", 8);
				host.BeginPlaySync("H");
				cli.ForceP2POnly = p2pOnly;
				Assert.True(cli.JoinRoom(code));
				var e = WaitEvent(cli, "error");
				Assert.NotNull(e);
				Assert.Equal("A song is in progress, try again in a moment", e.Data);
				Assert.Equal(1, host.PeerCount());

				host.EndPlaySync();
				Assert.True(cli.JoinRoom(code));
				Assert.NotNull(WaitEvent(cli, "connected"));
				Assert.True(WaitUntil(() => host.PeerCount() == 2));
			} finally { cli.Leave(); host.Leave(); }
		}

		[Fact]
		public void Join_ToAHostOfAnotherVersion_SaysSo() {
			int port = NextPort();
			var host = NewNet(port); var cli = NewNet(port);
			cli.P2PTimeoutMs = 600;
			TcpListener other = null;
			try {
				string code = host.CreateRoom("s", "", 8);

				// this build's host answers a newer client with its header before closing
				using (var raw = new TcpClient("127.0.0.1", port)) {
					var ns = raw.GetStream(); ns.ReadTimeout = 3000;
					ns.Write(new byte[] { (byte)'O', (byte)'T', (byte)'O', (byte)'N', 0, 99 }, 0, 6);
					byte[] back = new byte[6];
					int got = 0;
					while (got < 6) { int r = ns.Read(back, got, 6 - got); if (r <= 0) break; got += r; }
					Assert.Equal(6, got);
					Assert.Equal("OTON", Encoding.ASCII.GetString(back, 0, 4));
					Assert.NotEqual(99, (back[4] << 8) | back[5]);
				}

				// and a host of another version makes the join fail with that reason
				host.Leave();
				other = new TcpListener(IPAddress.Any, port);
				other.Start();
				var listener = other;
				new Thread(() => {
					try {
						while (true) {
							using var c = listener.AcceptTcpClient();
							var ns = c.GetStream(); ns.ReadTimeout = 2000;
							byte[] hdr = new byte[6];
							for (int got = 0; got < 6;) { int r = ns.Read(hdr, got, 6 - got); if (r <= 0) break; got += r; }
							ns.Write(new byte[] { (byte)'O', (byte)'T', (byte)'O', (byte)'N', 0, 99 }, 0, 6);
							c.Client.Shutdown(SocketShutdown.Send);
							byte[] sink = new byte[512];
							try { while (ns.Read(sink, 0, sink.Length) > 0) { } } catch { }
						}
					} catch { }
				}) { IsBackground = true }.Start();
				Assert.True(cli.JoinRoom(code));
				var e = WaitEvent(cli, "error");
				Assert.NotNull(e);
				Assert.Equal(LuaNetworking.JoinFailMessage(LuaNetworking.JoinFail.VersionMismatch, null), e.Data);
				Assert.Contains("different game version", e.Data);
			} finally { try { other?.Stop(); } catch { } cli.Leave(); host.Leave(); }
		}

		[Fact]
		public void JoinFail_TheMostSpecificReasonWins_WhateverTheOrder() {
			var J = new[] {
				LuaNetworking.JoinFail.NoRoute, LuaNetworking.JoinFail.SignalUnreachable, LuaNetworking.JoinFail.StaleCode,
				LuaNetworking.JoinFail.VersionMismatch, LuaNetworking.JoinFail.Rejected,
			};
			// rejected by the host > version mismatch > stale code > matchmaking unreachable > no route
			for (int hi = 0; hi < J.Length; hi++)
				for (int lo = 0; lo <= hi; lo++) {
					Assert.Equal(J[hi], LuaNetworking.MoreSpecific(J[lo], J[hi]));
					Assert.Equal(J[hi], LuaNetworking.MoreSpecific(J[hi], J[lo]));
				}
			foreach (var j in J) Assert.Equal(j, LuaNetworking.MoreSpecific(LuaNetworking.JoinFail.None, j));
			Assert.Equal(LuaNetworking.JoinFail.Rejected, J.Reverse().Aggregate(LuaNetworking.JoinFail.None, LuaNetworking.MoreSpecific));
			Assert.Equal(LuaNetworking.JoinFail.VersionMismatch, new[] { J[3], J[0], J[2], J[1], J[0] }.Aggregate(LuaNetworking.JoinFail.None, LuaNetworking.MoreSpecific));

			// each reason has its own text; the host's reason is passed on as it is
			Assert.Equal("Room is full", LuaNetworking.JoinFailMessage(LuaNetworking.JoinFail.Rejected, "Room is full"));
			Assert.Contains("different game version", LuaNetworking.JoinFailMessage(LuaNetworking.JoinFail.VersionMismatch, "x"));
			Assert.Contains("older room", LuaNetworking.JoinFailMessage(LuaNetworking.JoinFail.StaleCode, "x"));
			Assert.Contains("matchmaking service is unreachable", LuaNetworking.JoinFailMessage(LuaNetworking.JoinFail.SignalUnreachable, "x"));
			Assert.Contains("Host did not respond at 1x.x.x.x", LuaNetworking.JoinFailMessage(LuaNetworking.JoinFail.NoRoute, "Host did not respond at 1x.x.x.x"));
			Assert.Contains("no route to the host", LuaNetworking.JoinFailMessage(LuaNetworking.JoinFail.NoRoute, null));
			Assert.Equal(5, J.Select(j => LuaNetworking.JoinFailMessage(j, "d")).Distinct().Count());
		}

		// ── session lifetime ─────────────────────────────────────────────────────────────────────────────
		[Fact]
		public void JoinAgain_WhileTheFirstJoinStillRuns_GivesTheHostOnePlayer() {
			int port = NextPort();
			var host = NewNet(port, "a"); var cli = NewNet(port, "a");
			cli.JoinTimeoutMs = 2000; cli.P2PTimeoutMs = 1500; cli.OfferRepeatMs = 100;
			try {
				string code = host.CreateRoom("s", "", 8);
				cli.ForceP2POnly = true;
				Bus.DropSends = int.MaxValue;                        // the first join never reaches the host
				Assert.True(cli.JoinRoom(code));
				Thread.Sleep(350);                                   // its worker is waiting and repeating its offer
				Assert.Equal(0, cli.SelfId());
				Bus.DropSends = 0;
				Assert.True(cli.JoinRoom(code));                     // a new session on the same instance
				Assert.NotNull(WaitEvent(cli, "connected"));
				Assert.True(WaitUntil(() => host.PeerCount() == 2));

				// past the first join's deadlines: its workers neither joined a second time nor reported a failure
				var atHost = Collect(host, 2600);
				var atCli = Collect(cli, 100);
				Assert.Single(atHost.Where(e => e.Type == "joined"));
				Assert.DoesNotContain(atHost, e => e.Type == "left");
				Assert.DoesNotContain(atCli, e => e.Type == "error" || e.Type == "connected");
				Assert.Equal(2, host.PeerCount());
				Assert.True(cli.Connected());
				Assert.Equal(2, cli.SelfId());
			} finally { cli.Leave(); host.Leave(); }
		}
	}
}
