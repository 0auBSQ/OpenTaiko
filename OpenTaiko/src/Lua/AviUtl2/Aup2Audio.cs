using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FDK;
using ManagedBass;
using ManagedBass.Opus;

namespace OpenTaiko.AviUtl2 {
	// The sound effects that are read by the code around them.
	internal static class Aup2AudioEffects {
		public const string Buffer = "オーディオバッファ", Mono = "モノラル化";

		// An audio buffer object: its volume effects work on the sounds heard so far. With the buffer
		// cleared they are changed in place and the result is exact. Without it the changed copy plays
		// over the sounds themselves: only its volume is added, and false is returned.
		public static bool OnBuffer(Aup2Context c, Aup2Object o, List<Aup2AudioItem> heard) {
			double gain = 1, pan = 0, mono = 0;
			foreach (var e in o.Effects) {
				switch (e.Name) {
					case Aup2Evaluator.AudioPlay:
						gain *= c.V(e, "音量", 100) / 100;
						pan += c.V(e, "左右") / 100;
						break;
					case "音量フェード": {
						double fin = c.V(e, "イン"), fout = c.V(e, "アウト");
						if (fin > 0) gain *= Math.Clamp(c.Time / fin, 0, 1);
						if (fout > 0) gain *= Math.Clamp(c.TimeLeft / fout, 0, 1);
						break;
					}
					case "音量調整":
						gain *= c.V(e, "音量", 100) / 100;
						pan += c.V(e, "左右") / 100;
						break;
					case Mono:
						mono = Math.Max(mono, Math.Clamp(c.V(e, "比率") / 100, 0, 1));
						break;
				}
			}
			gain = Math.Max(0, gain);
			bool clear = o.Effects[0].Section.GetInt("オーディオバッファをクリア") != 0;
			foreach (var a in heard) {
				if (clear) {
					a.Volume *= gain;
					a.Pan = Math.Clamp(a.Pan + pan, -1, 1);
					a.Mono = Math.Max(a.Mono, mono);
				} else {
					a.Volume *= 1 + gain;
				}
			}
			return clear;
		}
	}

	// Left and right of a playing sound mixed together by a share, on the sound's own streams.
	internal sealed class Aup2MonoMix {
		private static readonly FieldInfo? Pair = typeof(CSkin.CSystemSound).GetField("rSound", BindingFlags.Instance | BindingFlags.NonPublic);

		private readonly DSPProcedure _proc;
		private readonly List<(int stream, int dsp)> _set = new();
		private readonly List<int> _streams = new();
		private volatile float _ratio;

		public Aup2MonoMix() { _proc = Process; }

		// ratio: 0 as recorded .. 1 both sides the same. The streams are looked at again on every call,
		// since a sound makes new ones when the output device changes.
		public void Set(CSkin.CSystemSound? sound, double ratio) {
			_ratio = (float)Math.Clamp(ratio, 0, 1);
			_streams.Clear();
			if (sound != null) {
				if (Pair?.GetValue(sound) is CSound[] pair) {
					foreach (var s in pair) {
						int h = s == null ? 0 : (int)s.Pointer;
						if (h > 0 && !_streams.Contains(h)) _streams.Add(h);
					}
				} else if ((int)sound.Pointer > 0) {
					_streams.Add((int)sound.Pointer);
				}
			}
			bool same = _streams.Count == _set.Count;
			for (int i = 0; same && i < _set.Count; i++) same = _set[i].stream == _streams[i];
			if (same) return;
			Remove();
			foreach (int h in _streams) {
				if (!Bass.ChannelGetInfo(h, out var info) || info.Channels != 2) continue;
				// what the samples are: 1 float, 2 16-bit
				int format = (info.Flags & BassFlags.Float) != 0 || Bass.FloatingPointDSP ? 1 : (info.Flags & BassFlags.Byte) != 0 ? 0 : 2;
				if (format == 0) continue;
				int dsp = Bass.ChannelSetDSP(h, _proc, (IntPtr)format);
				if (dsp != 0) _set.Add((h, dsp));
			}
		}

		public void Remove() {
			foreach (var (stream, dsp) in _set) Bass.ChannelRemoveDSP(stream, dsp);
			_set.Clear();
		}

		private unsafe void Process(int handle, int channel, IntPtr buffer, int length, IntPtr user) {
			float k = _ratio * 0.5f;
			if (k <= 0 || buffer == IntPtr.Zero) return;
			if ((int)user == 1) {
				float* p = (float*)buffer;
				for (int n = length / 8; n > 0; n--, p += 2) {
					float l = p[0], r = p[1];
					p[0] = l + (r - l) * k;
					p[1] = r + (l - r) * k;
				}
			} else {
				short* p = (short*)buffer;
				for (int n = length / 4; n > 0; n--, p += 2) {
					float l = p[0], r = p[1];
					p[0] = (short)(l + (r - l) * k);
					p[1] = (short)(r + (l - r) * k);
				}
			}
		}
	}

	// The mono mixes of a project's sound objects, by object.
	internal sealed class Aup2MonoMixes {
		private readonly Dictionary<int, Aup2MonoMix> _byObject = new();

		public void Set(int objectId, CSkin.CSystemSound? sound, double ratio) {
			if (!_byObject.TryGetValue(objectId, out var mix)) {
				if (ratio <= 0) return;
				_byObject[objectId] = mix = new Aup2MonoMix();
			}
			mix.Set(sound, ratio);
		}

		// before the object's sound is closed
		public void Drop(int objectId) {
			if (_byObject.Remove(objectId, out var mix)) mix.Remove();
		}

		public void Clear() {
			foreach (var mix in _byObject.Values) mix.Remove();
			_byObject.Clear();
		}
	}

	// A sound file as mono samples at a low rate, for the objects that draw sound. It is decoded once,
	// off the render thread, and holds at most MostSamples 16-bit samples whatever the file's length.
	internal sealed class Aup2SampleTable {
		public const int MostSamples = 2_000_000;
		private const double BestRate = 22050;

		private short[]? _data;
		private int _count;
		private volatile bool _ready, _dropped;

		public double Rate { get; private set; } = 1;
		public bool Ready => _ready;

		public static Aup2SampleTable Load(string path) {
			var t = new Aup2SampleTable();
			Task.Run(() => {
				try { t.Decode(path); } catch (Exception) { }
			});
			return t;
		}

		// the samples are let go; a decode still running stops
		public void Drop() {
			_dropped = true;
			_ready = false;
			_data = null;
		}

		private void Decode(string path) {
			bool opus = string.Equals(Path.GetExtension(path), ".opus", StringComparison.OrdinalIgnoreCase);
			const BassFlags flags = BassFlags.Decode | BassFlags.Float;
			int h = opus ? BassOpus.CreateStream(path, 0, 0, flags) : Bass.CreateStream(path, 0, 0, flags);
			if (h == 0) return;
			try {
				if (!Bass.ChannelGetInfo(h, out var info) || info.Channels < 1 || info.Frequency <= 0) return;
				int channels = info.Channels;
				long bytes = Bass.ChannelGetLength(h);
				long frames = bytes > 0 ? bytes / 4 / channels : 0;
				// this many frames of the file make one sample of the table
				int block = Math.Max(1, (int)Math.Ceiling(info.Frequency / BestRate));
				if (frames > 0) block = (int)Math.Max(block, (frames + MostSamples - 1) / MostSamples);
				else block = Math.Max(block, (int)Math.Ceiling(info.Frequency / 8000.0));
				var data = new short[frames > 0 ? (int)Math.Min(MostSamples, frames / block + 16) : 1 << 16];
				var buffer = new float[channels * 16384];
				int count = 0, held = 0;
				double sum = 0;
				bool full = false;
				while (!_dropped && !full) {
					int got = Bass.ChannelGetData(h, buffer, buffer.Length * 4);
					if (got <= 0) break;
					int n = got / 4 / channels;
					for (int i = 0; i < n; i++) {
						double m = 0;
						for (int k = 0; k < channels; k++) m += buffer[i * channels + k];
						sum += m / channels;
						if (++held < block) continue;
						if (count == data.Length) {
							if (count >= MostSamples) { full = true; break; }
							Array.Resize(ref data, Math.Min(MostSamples, count * 2));
						}
						data[count++] = (short)Math.Clamp(sum / block * 32767, -32767, 32767);
						held = 0;
						sum = 0;
					}
				}
				if (_dropped) return;
				Rate = (double)info.Frequency / block;
				_count = count;
				_data = data;
				_ready = true;
				if (_dropped) _data = null;
			} finally {
				Bass.StreamFree(h);
			}
		}

		// into[i] += the sound at (start + i * step) seconds, times gain; nothing outside the file
		public void AddTo(float[] into, int count, double start, double step, double gain) {
			var data = _data;
			if (!_ready || data == null) return;
			int n = Math.Min(_count, data.Length);
			float g = (float)(gain / 32767);
			for (int i = 0; i < count; i++) {
				double at = (start + i * step) * Rate;
				if (at < 0 || at >= n - 1) continue;
				int a = (int)at;
				float f = (float)(at - a);
				into[i] += (data[a] + (data[a + 1] - data[a]) * f) * g;
			}
		}
	}

	// Who decodes the sound files of a project for the objects that draw them.
	internal interface IAup2SampleSource {
		// the file a stored path means, as samples; null when there is no such file. Not ready at once.
		Aup2SampleTable? Samples(string storedPath);
	}

	// The sample source of each scene being played, so an object finds it from its scene alone.
	internal static class Aup2Samples {
		private static readonly ConditionalWeakTable<Aup2Scene, IAup2SampleSource> Sources = new();

		public static void Attach(IEnumerable<Aup2Scene> scenes, IAup2SampleSource source) {
			foreach (var s in scenes) Sources.AddOrUpdate(s, source);
		}
		public static void Detach(IEnumerable<Aup2Scene> scenes) {
			foreach (var s in scenes) Sources.Remove(s);
		}
		public static IAup2SampleSource? Of(Aup2Scene scene) => Sources.TryGetValue(scene, out var s) ? s : null;
	}

	// What a sound looks like at a time: one low and one high end per column, each 0 (bottom) .. 1 (top).
	internal static class Aup2Waveform {
		public struct Sound {
			public Aup2SampleTable Table;
			public double At, Gain;
		}

		private const int FftSize = 1024;

		// The wave over `seconds` from each sound's time. joined: a column reaches its neighbour, so the
		// columns read as one line.
		public static void Wave(List<Sound> sounds, double seconds, bool joined, float[] lo, float[] hi) {
			int n = lo.Length;
			double rate = 1;
			foreach (var s in sounds) rate = Math.Max(rate, s.Table.Rate);
			int per = (int)Math.Clamp(Math.Ceiling(seconds * rate / n), 1, 16);
			var buffer = new float[n * per + 1];
			double step = seconds / (n * per);
			foreach (var s in sounds) s.Table.AddTo(buffer, buffer.Length, s.At, step, s.Gain);
			for (int i = 0; i < n; i++) {
				float min = float.MaxValue, max = float.MinValue;
				int last = joined ? per : per - 1;
				for (int k = 0; k <= last; k++) {
					float v = buffer[i * per + k];
					if (v < min) min = v;
					if (v > max) max = v;
				}
				lo[i] = Math.Clamp(min * 0.5f + 0.5f, 0, 1);
				hi[i] = Math.Clamp(max * 0.5f + 0.5f, 0, 1);
			}
		}

		// How loud each band is around each sound's time, low notes on the left: 0 at 60 dB under full
		// scale, 1 at full scale. mirror: the bars grow both ways from the middle.
		public static void Spectrum(List<Sound> sounds, bool mirror, float[] lo, float[] hi) {
			int n = lo.Length;
			double rate = double.MaxValue;
			foreach (var s in sounds) rate = Math.Min(rate, s.Table.Rate);
			var re = new float[FftSize];
			var im = new float[FftSize];
			foreach (var s in sounds) s.Table.AddTo(re, FftSize, s.At - FftSize / 2 / rate, 1 / rate, s.Gain);
			for (int i = 0; i < FftSize; i++) re[i] *= (float)(0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (FftSize - 1)));
			Fft(re, im);
			int bins = FftSize / 2 - 1;
			for (int i = 0; i < n; i++) {
				int b0 = 1 + (int)((long)i * bins / n), b1 = Math.Max(b0 + 1, 1 + (int)((long)(i + 1) * bins / n));
				double peak = 0;
				for (int b = b0; b < b1 && b < FftSize / 2; b++) peak = Math.Max(peak, Math.Sqrt(re[b] * re[b] + im[b] * im[b]));
				double db = 20 * Math.Log10(peak / (FftSize / 4.0) + 1e-9);
				float m = (float)Math.Clamp((db + 60) / 60, 0, 1);
				lo[i] = mirror ? 0.5f - m / 2 : 0;
				hi[i] = mirror ? 0.5f + m / 2 : m;
			}
		}

		// the columns as the bytes of a picture one pixel high: each end as 16 bits, high byte first
		public static byte[] Pack(float[] lo, float[] hi) {
			var bytes = new byte[lo.Length * 4];
			for (int i = 0; i < lo.Length; i++) {
				int a = (int)Math.Round(Math.Clamp(lo[i], 0, 1) * 65535), b = (int)Math.Round(Math.Clamp(hi[i], 0, 1) * 65535);
				bytes[i * 4] = (byte)(a >> 8); bytes[i * 4 + 1] = (byte)a;
				bytes[i * 4 + 2] = (byte)(b >> 8); bytes[i * 4 + 3] = (byte)b;
			}
			return bytes;
		}

		private static void Fft(float[] re, float[] im) {
			int n = re.Length;
			for (int i = 1, j = 0; i < n; i++) {
				int bit = n >> 1;
				for (; (j & bit) != 0; bit >>= 1) j ^= bit;
				j ^= bit;
				if (i < j) { (re[i], re[j]) = (re[j], re[i]); (im[i], im[j]) = (im[j], im[i]); }
			}
			for (int len = 2; len <= n; len <<= 1) {
				double ang = -2 * Math.PI / len;
				float wr = (float)Math.Cos(ang), wi = (float)Math.Sin(ang);
				for (int i = 0; i < n; i += len) {
					float cr = 1, ci = 0;
					for (int k = 0; k < len / 2; k++) {
						int a = i + k, b = a + len / 2;
						float xr = re[b] * cr - im[b] * ci, xi = re[b] * ci + im[b] * cr;
						re[b] = re[a] - xr; im[b] = im[a] - xi;
						re[a] += xr; im[a] += xi;
						float t = cr * wr - ci * wi;
						ci = cr * wi + ci * wr;
						cr = t;
					}
				}
			}
		}
	}
}
