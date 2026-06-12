using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace OpenTaiko {
	// ═══════════════════════════════════════════════════════════════════════════════════════════════
	// MiniMqtt — the smallest possible MQTT 3.1.1 client (QoS 0 only), used as a free, zero-setup
	// SIGNALING channel for pure-P2P joins: host and joiner meet on a public broker topic to exchange
	// connection candidates (~1 KB, AES-GCM encrypted with the room-code secret), then talk directly.
	// Game traffic never passes through the broker. No external packages: the protocol at QoS 0 is
	// just CONNECT/CONNACK, SUBSCRIBE/SUBACK, PUBLISH and PING over a TCP stream.
	// ═══════════════════════════════════════════════════════════════════════════════════════════════
	internal sealed class MiniMqtt : IDisposable {
		// well-known free public brokers; the first that answers wins (signaling only, tiny payloads)
		public static readonly string[] PublicBrokers = { "test.mosquitto.org:1883", "broker.hivemq.com:1883", "broker.emqx.io:1883" };

		private TcpClient _tcp;
		private NetworkStream _ns;
		private readonly object _writeLock = new();
		private Thread _rx;
		private volatile bool _running;
		private ushort _packetId = 1;
		public string ConnectedBroker { get; private set; } = "";

		private readonly Dictionary<string, Action<byte[]>> _subs = new();

		/// <summary>Connects to the first reachable broker of the list; false if none answered.</summary>
		public bool Connect(string[] brokers, int timeoutMsPerBroker = 4000) {
			foreach (string b in brokers) {
				int i = b.LastIndexOf(':');
				string host = i > 0 ? b.Substring(0, i) : b;
				int port = i > 0 && int.TryParse(b.Substring(i + 1), out int p) ? p : 1883;
				try {
					var tcp = new TcpClient();
					var ar = tcp.BeginConnect(host, port, null, null);
					if (!ar.AsyncWaitHandle.WaitOne(timeoutMsPerBroker)) { tcp.Close(); continue; }
					tcp.EndConnect(ar); tcp.NoDelay = true;
					var ns = tcp.GetStream(); ns.ReadTimeout = timeoutMsPerBroker;

					// CONNECT (clean session, keepalive 60s, random client id)
					string cid = "oton-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(6)).ToLowerInvariant();
					var vh = new List<byte>();
					AddString(vh, "MQTT"); vh.Add(4); vh.Add(0x02); vh.Add(0); vh.Add(60);
					AddString(vh, cid);
					Send(ns, 0x10, vh.ToArray());

					// CONNACK
					var (type, body) = ReadPacket(ns);
					if (type != 0x20 || body.Length < 2 || body[1] != 0) { tcp.Close(); continue; }

					_tcp = tcp; _ns = ns; _running = true; ConnectedBroker = b;
					ns.ReadTimeout = 90000;
					_rx = new Thread(RxLoop) { IsBackground = true, Name = "OTON-mqtt" };
					_rx.Start();
					return true;
				} catch { /* next broker */ }
			}
			return false;
		}

		public void Subscribe(string topic, Action<byte[]> onMessage) {
			lock (_subs) _subs[topic] = onMessage;
			var vh = new List<byte>();
			ushort id = _packetId++;
			vh.Add((byte)(id >> 8)); vh.Add((byte)id);
			AddString(vh, topic); vh.Add(0);                 // QoS 0
			Send(_ns, 0x82, vh.ToArray());                   // SUBACK is consumed by RxLoop
		}

		public void Publish(string topic, byte[] payload) {
			var vh = new List<byte>();
			AddString(vh, topic);
			vh.AddRange(payload);
			Send(_ns, 0x30, vh.ToArray());
		}

		private void RxLoop() {
			long lastPing = Environment.TickCount64;
			while (_running) {
				try {
					if (Environment.TickCount64 - lastPing > 30000) { Send(_ns, 0xC0, Array.Empty<byte>()); lastPing = Environment.TickCount64; }
					if (!_ns.DataAvailable) { Thread.Sleep(50); continue; }
					var (type, body) = ReadPacket(_ns);
					if (type < 0) break;
					if (type == 0x30) {                       // PUBLISH (QoS 0: no packet id)
						if (body.Length < 2) continue;
						int tl = (body[0] << 8) | body[1];
						if (body.Length < 2 + tl) continue;
						string topic = Encoding.UTF8.GetString(body, 2, tl);
						byte[] payload = new byte[body.Length - 2 - tl];
						Buffer.BlockCopy(body, 2 + tl, payload, 0, payload.Length);
						Action<byte[]> cb = null;
						lock (_subs) _subs.TryGetValue(topic, out cb);
						try { cb?.Invoke(payload); } catch { }
					}
					// CONNACK/SUBACK/PINGRESP need no handling at QoS 0
				} catch { break; }
			}
			_running = false;
		}

		// ── wire helpers ─────────────────────────────────────────────────────────────────────────────
		private static void AddString(List<byte> b, string s) {
			byte[] u = Encoding.UTF8.GetBytes(s);
			b.Add((byte)(u.Length >> 8)); b.Add((byte)u.Length); b.AddRange(u);
		}

		private void Send(NetworkStream ns, byte header, byte[] body) {
			var pkt = new List<byte> { header };
			int len = body.Length;                            // remaining-length varint
			do { byte d = (byte)(len % 128); len /= 128; if (len > 0) d |= 0x80; pkt.Add(d); } while (len > 0);
			pkt.AddRange(body);
			byte[] raw = pkt.ToArray();
			lock (_writeLock) ns.Write(raw, 0, raw.Length);
		}

		private static (int type, byte[] body) ReadPacket(NetworkStream ns) {
			int h = ns.ReadByte();
			if (h < 0) return (-1, null);
			int len = 0, mult = 1;
			for (int i = 0; i < 4; i++) {
				int d = ns.ReadByte();
				if (d < 0) return (-1, null);
				len += (d & 0x7F) * mult;
				if ((d & 0x80) == 0) break;
				mult *= 128;
			}
			if (len > 1 << 20) return (-1, null);
			byte[] body = new byte[len];
			int off = 0;
			while (off < len) {
				int r = ns.Read(body, off, len - off);
				if (r <= 0) return (-1, null);
				off += r;
			}
			return (h & 0xF0, body);
		}

		public void Dispose() {
			_running = false;
			try { _tcp?.Close(); } catch { }
		}
	}
}
