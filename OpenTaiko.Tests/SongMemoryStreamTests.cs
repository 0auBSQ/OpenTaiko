using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using FDK;
using ManagedBass;
using ManagedBass.Mix;
using Xunit;

namespace OpenTaikoTests {
	// A chart's audio of 1 MB or more is read into memory once and decoded from there instead of from its file. It
	// plays through a mixer like any sound, and its pinned bytes go when the sound is disposed.
	public class SongMemoryStreamTests {
		private static bool BassReady() {
			string libs = Path.Combine(AppContext.BaseDirectory, "Libs", "win-x64");
			if (!OperatingSystem.IsWindows() || !Directory.Exists(libs)) return false;
			NativeLibrary.Load(Path.Combine(libs, "bass.dll"));
			NativeLibrary.Load(Path.Combine(libs, "bassmix.dll"));
			NativeLibrary.Load(Path.Combine(libs, "bass_fx.dll"));
			if (!Bass.Init(0, 44100, DeviceInitFlags.Default, IntPtr.Zero))
				Assert.Equal(Errors.Already, Bass.LastError);
			return true;
		}

		// 12 s of a 440 Hz sine, 16-bit stereo at 44.1 kHz: about 2 MB
		private static string MakeWav() {
			const int rate = 44100, channels = 2, seconds = 12;
			int samples = rate * seconds;
			int dataBytes = samples * channels * 2;
			string path = Path.Combine(Path.GetTempPath(), "opentaiko_song_" + Guid.NewGuid().ToString("N") + ".wav");
			using var w = new BinaryWriter(File.Create(path));
			w.Write("RIFF"u8); w.Write(36 + dataBytes); w.Write("WAVE"u8);
			w.Write("fmt "u8); w.Write(16); w.Write((short)1); w.Write((short)channels); w.Write(rate);
			w.Write(rate * channels * 2); w.Write((short)(channels * 2)); w.Write((short)16);
			w.Write("data"u8); w.Write(dataBytes);
			for (int i = 0; i < samples; i++) {
				short v = (short)(Math.Sin(2 * Math.PI * 440 * i / rate) * 16000);
				w.Write(v);
				w.Write(v);
			}
			return path;
		}

		private static bool IsPinned(CSound sound)
			=> ((GCHandle)typeof(CSound).GetField("hGC", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(sound)!).IsAllocated;

		[Fact]
		public void SongPlayback_DecodesFromPinnedMemory_AndFreesItOnDispose() {
			if (!BassReady()) return;
			string wav = MakeWav();
			int mixer = 0;
			try {
				Assert.True(new FileInfo(wav).Length >= 1024 * 1024);
				mixer = BassMix.CreateMixerStream(44100, 2, BassFlags.MixerNonStop | BassFlags.Decode | BassFlags.Float);
				Assert.True(mixer != 0, Bass.LastError.ToString());

				// menu music (no chart loading) still streams from its file
				var menu = new CSound(ESoundGroup.SongPlayback);
				menu.CreateBassSound(wav, mixer);
				Assert.False(IsPinned(menu));
				menu.Dispose();

				var song = new CSound(ESoundGroup.SongPlayback);
				CSound effect;
				CSound.ReadSongFileWhole = true;
				try {
					song.CreateBassSound(wav, mixer);
					// a sound effect of the same size streams from its file even while a chart loads
					effect = new CSound(ESoundGroup.SoundEffect);
					effect.CreateBassSound(wav, mixer);
				} finally {
					CSound.ReadSongFileWhole = false;
				}
				Assert.True(IsPinned(song));
				Assert.False(IsPinned(effect));
				effect.Dispose();

				Assert.True(song.AddBassSoundFromMixer(), Bass.LastError.ToString());
				song.tPlay();
				var buffer = new float[44100 * 2 / 10];   // 100 ms of the mixer's output
				float peak = 0;
				for (int i = 0; i < 20; i++) {
					int got = Bass.ChannelGetData(mixer, buffer, buffer.Length * sizeof(float));
					Assert.True(got > 0, Bass.LastError.ToString());
					foreach (float s in buffer) peak = Math.Max(peak, Math.Abs(s));
				}
				song.tGetPlayPosition(out _, out double ms);
				Assert.InRange(ms, 1500.0, 2600.0);   // the 2 s pulled through the mixer
				Assert.True(peak > 0.2f, $"the mix is silent (peak {peak})");

				song.Dispose();
				Assert.False(IsPinned(song));
				// the mixer goes on without it
				Assert.True(Bass.ChannelGetData(mixer, buffer, buffer.Length * sizeof(float)) > 0, Bass.LastError.ToString());
			} finally {
				if (mixer != 0) Bass.StreamFree(mixer);
				try { File.Delete(wav); } catch (IOException) { }
			}
		}
	}
}
