using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FDK;
using ManagedBass;
using Xunit;

namespace OpenTaikoTests {
	// A video opened with an audio factory decodes its audio track through FFmpeg, pushes it into a BASS
	// stream, and takes that stream's playback position as its clock. Runs against a clip generated with
	// the ffmpeg CLI (skipped where the CLI or the Windows libraries are missing).
	public class VideoAudioTests {
		private static string? MakeClip() {
			string dir = Path.Combine(AppContext.BaseDirectory, "FFmpeg", "win-x64");
			if (!OperatingSystem.IsWindows() || !Directory.Exists(dir)) return null;
			string clip = Path.Combine(Path.GetTempPath(), "opentaiko_video_audio_" + Guid.NewGuid().ToString("N") + ".mp4");
			try {
				var p = Process.Start(new ProcessStartInfo("ffmpeg",
					$"-y -v error -f lavfi -i testsrc=size=64x64:rate=30 -f lavfi -i sine=frequency=997:sample_rate=48000 -t 4 -c:v libx264 -pix_fmt yuv420p -c:a aac -ac 2 -shortest \"{clip}\"") {
					UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true });
				if (p == null) return null;
				p.WaitForExit(60000);
				if (p.ExitCode != 0 || !File.Exists(clip)) return null;
			} catch (Exception) {
				return null;   // no ffmpeg CLI on this machine
			}
			ffmpeg_setup();
			return clip;
		}

		private static void ffmpeg_setup() {
			FFmpeg.AutoGen.ffmpeg.RootPath = Path.Combine(AppContext.BaseDirectory, "FFmpeg", "win-x64") + Path.DirectorySeparatorChar;
			NativeLibrary.Load(Path.Combine(AppContext.BaseDirectory, "Libs", "win-x64", "bass.dll"));
			NativeLibrary.Load(Path.Combine(AppContext.BaseDirectory, "Libs", "win-x64", "bassmix.dll"));
			NativeLibrary.Load(Path.Combine(AppContext.BaseDirectory, "Libs", "win-x64", "bass_fx.dll"));
			if (!Bass.Init(0, 48000, DeviceInitFlags.Default, IntPtr.Zero))
				Assert.Equal(Errors.Already, Bass.LastError);
		}

		// the frame texture needs a GL context to free, which this process has none of
		private static void DisposeWithoutGl(CVideoDecoder video) {
			try { video.Dispose(); } catch (NullReferenceException) { }
		}

		// the game's path: the pushed sound is a decoding source inside a BASS mixer, which is what plays
		[Fact]
		public void AudioTrack_PlaysThroughAMixer() {
			string? clip = MakeClip();
			if (clip == null) return;
			int mixer = 0;
			try {
				mixer = ManagedBass.Mix.BassMix.CreateMixerStream(48000, 2, BassFlags.Default);
				Assert.True(mixer != 0, Bass.LastError.ToString());
				Assert.True(Bass.ChannelPlay(mixer, false), Bass.LastError.ToString());
				var video = new CVideoDecoder(clip, (rate, channels, seconds, proc) => {
					var snd = new CSound(ESoundGroup.SongPlayback);
					snd.CreateBassUserSound(rate, channels, seconds, mixer, proc);
					return snd;
				});
				Assert.True(video.HasAudio);
				video.InitRead();
				var sw = Stopwatch.StartNew();
				while (video.AudioQueuedMs <= 0 && sw.ElapsedMilliseconds < 3000) Thread.Sleep(10);
				Assert.True(video.AudioQueuedMs > 0, "no audio decoded before playback");
				video.Start();
				Thread.Sleep(400);
				Assert.InRange(video.msPlayPosition, 150.0, 700.0);
				video.Stop();
				DisposeWithoutGl(video);
			} finally {
				if (mixer != 0) Bass.StreamFree(mixer);
				try { File.Delete(clip); } catch (IOException) { }
			}
		}

		// the game's exact shape: a decoding mixer pulled by a stream procedure, the decoder opened on a
		// worker thread (as LuaVideo does), Start on the main thread
		[Fact]
		public async Task AudioTrack_PlaysThroughADecodeMixerFromAWorkerThread() {
			string? clip = MakeClip();
			if (clip == null) return;
			int mixer = 0, main = 0;
			StreamProcedure? proc = null;
			try {
				mixer = ManagedBass.Mix.BassMix.CreateMixerStream(48000, 2, BassFlags.MixerNonStop | BassFlags.Decode);
				Assert.True(mixer != 0, Bass.LastError.ToString());
				int mixerHandle = mixer;
				proc = (handle, buffer, length, user) => { int n = Bass.ChannelGetData(mixerHandle, buffer, length); return n < 0 ? 0 : n; };
				main = Bass.CreateStream(48000, 2, BassFlags.Default, proc, IntPtr.Zero);
				Assert.True(main != 0, Bass.LastError.ToString());
				Assert.True(Bass.ChannelPlay(main, false), Bass.LastError.ToString());

				CVideoDecoder? video = null;
				var opened = System.Threading.Tasks.Task.Run(() => {
					var v = new CVideoDecoder(clip, (rate, channels, seconds, proc) => {
						var snd = new CSound(ESoundGroup.SongPlayback);
						snd.CreateBassUserSound(rate, channels, seconds, mixerHandle, proc);
						return snd;
					});
					v.InitRead();
					video = v;
				});
				await opened.WaitAsync(TimeSpan.FromSeconds(10));   // a TimeoutException when the open does not finish
				Assert.True(video!.HasAudio);
				var sw = Stopwatch.StartNew();
				while (video.AudioQueuedMs <= 0 && sw.ElapsedMilliseconds < 3000) Thread.Sleep(10);
				Assert.True(video.AudioQueuedMs > 0, "no audio decoded before playback");
				video.Start();
				Thread.Sleep(400);
				Assert.InRange(video.msPlayPosition, 150.0, 700.0);
				video.Stop();
				DisposeWithoutGl(video);
			} finally {
				if (main != 0) Bass.StreamFree(main);
				if (mixer != 0) Bass.StreamFree(mixer);
				GC.KeepAlive(proc);
				try { File.Delete(clip); } catch (IOException) { }
			}
		}

		// the output stops taking samples (headphones unplugged): the picture runs on, the waiting sound is
		// dropped at the same pace, and the sound clocks the video again once the output takes samples again
		[Fact]
		public void AudioOutputStalls_TheVideoRunsOnAndTheSoundTakesOverAgain() {
			string? clip = MakeClip();
			if (clip == null) return;
			int mixer = 0, main = 0;
			StreamProcedure? proc = null;
			var clock = Stopwatch.StartNew();
			double t0 = Game.dbTimeMs;
			void Tick() { Game.dbTimeMs = t0 + clock.Elapsed.TotalMilliseconds; Game.TimeMs = (long)Game.dbTimeMs; }
			try {
				mixer = ManagedBass.Mix.BassMix.CreateMixerStream(48000, 2, BassFlags.MixerNonStop | BassFlags.Decode);
				Assert.True(mixer != 0, Bass.LastError.ToString());
				int mixerHandle = mixer;
				proc = (handle, buffer, length, user) => { int n = Bass.ChannelGetData(mixerHandle, buffer, length); return n < 0 ? 0 : n; };
				main = Bass.CreateStream(48000, 2, BassFlags.Default, proc, IntPtr.Zero);
				Assert.True(Bass.ChannelPlay(main, false), Bass.LastError.ToString());
				Tick();
				var video = new CVideoDecoder(clip, (rate, channels, seconds, proc) => {
					var snd = new CSound(ESoundGroup.SongPlayback);
					snd.CreateBassUserSound(rate, channels, seconds, mixerHandle, proc);
					return snd;
				});
				video.InitRead();
				var sw = Stopwatch.StartNew();
				while (video.AudioQueuedMs < 500 && sw.ElapsedMilliseconds < 3000) Thread.Sleep(10);

				// the clock read about once a frame while the game time moves
				double Run(int ms) {
					var run = Stopwatch.StartNew();
					while (run.ElapsedMilliseconds < ms) { Tick(); _ = video.msPlayPosition; Thread.Sleep(15); }
					Tick();
					return video.msPlayPosition;
				}
				video.Start();
				double playing = Run(500);
				Assert.InRange(playing, 250.0, 800.0);

				Bass.ChannelPause(main);   // the output stops taking samples
				double stalled = Run(1500);
				Assert.InRange(stalled, playing + 1050.0, playing + 1800.0);   // ran on through the stall

				Assert.True(Bass.ChannelPlay(main, false), Bass.LastError.ToString());   // and takes them again
				double resumed = Run(600);
				Assert.InRange(resumed, stalled + 250.0, stalled + 1000.0);   // on from there, clocked by the sound
				video.Stop();
				DisposeWithoutGl(video);
			} finally {
				if (main != 0) Bass.StreamFree(main);
				if (mixer != 0) Bass.StreamFree(mixer);
				GC.KeepAlive(proc);
				try { File.Delete(clip); } catch (IOException) { }
			}
		}

		[Fact]
		public void SilentClip_SeekLandsWhereAsked_AndASpeedChangeKeepsThePosition() {
			string? clip = MakeClip();
			if (clip == null) return;
			try {
				var video = new CVideoDecoder(clip);
				video.Seek(1500);
				Assert.Equal(1500.0, video.msPlayPosition, 3);
				video.dbPlaySpeed = 1.5;
				Assert.Equal(1500.0, video.msPlayPosition, 3);
				video.dbPlaySpeed = 0.75;
				Assert.Equal(1500.0, video.msPlayPosition, 3);
				video.Seek(2000);                 // a seek lands where asked at any speed
				Assert.Equal(2000.0, video.msPlayPosition, 3);
				video.dbPlaySpeed = 1.5;
				video.Seek(3000);
				Assert.Equal(3000.0, video.msPlayPosition, 3);
				DisposeWithoutGl(video);
			} finally {
				try { File.Delete(clip); } catch (IOException) { }
			}
		}

		[Fact]
		public void WithoutAudioFactory_NoTrackIsOpened() {
			string? clip = MakeClip();
			if (clip == null) return;
			try {
				var video = new CVideoDecoder(clip);
				Assert.False(video.HasAudio);
				Assert.Null(video.Audio);
				DisposeWithoutGl(video);
			} finally {
				// without a GL context the decoder's disposal stops early and may still hold the file
				try { File.Delete(clip); } catch (IOException) { }
			}
		}
	}
}
