using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
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

		// a user-stream sound with no mixer: it plays straight on BASS's silent device, so its position advances
		private static CSound UserSound(int rate, int channels, double seconds, StreamProcedure proc) {
			var s = new CSound(ESoundGroup.SongPlayback);
			s.CreateBassUserSound(rate, channels, seconds, CSound.NoMixerHandle, proc);
			return s;
		}

		[Fact]
		public void AudioTrack_IsDecodedPushedAndClocksTheVideo() {
			string? clip = MakeClip();
			if (clip == null) return;
			try {
				int made = 0;
				var video = new CVideoDecoder(clip, (rate, channels, seconds, proc) => { made++; Assert.Equal(48000, rate); Assert.Equal(2, channels); return UserSound(rate, channels, seconds, proc); });
				Assert.True(video.HasAudio);
				Assert.Equal(1, made);
				video.InitRead();

				// the reader runs ahead of playback: samples are queued before Start
				var sw = Stopwatch.StartNew();
				while (video.AudioQueuedMs <= 0 && sw.ElapsedMilliseconds < 3000) Thread.Sleep(10);
				double queued = video.AudioQueuedMs;
				Assert.True(queued > 0, "no audio decoded before playback");
				Assert.Equal(0.0, video.msPlayPosition, 1);   // the clock waits for the sound to start

				video.Start();
				Thread.Sleep(400);
				double pos = video.msPlayPosition;
				Assert.InRange(pos, 150.0, 700.0);   // the clock is the sound's own position, in media time
				Assert.True(video.AudioQueuedMs < queued + 1000, "the queue keeps pace instead of growing without bound");

				video.Pause();
				double paused = video.msPlayPosition;
				Thread.Sleep(150);
				Assert.InRange(video.msPlayPosition, paused - 1, paused + 40);   // paused audio stops the clock
				video.Stop();
				DisposeWithoutGl(video);
			} finally {
				// without a GL context the decoder's disposal stops early and may still hold the file
				try { File.Delete(clip); } catch (IOException) { }
			}
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
		public void AudioTrack_PlaysThroughADecodeMixerFromAWorkerThread() {
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
				Assert.True(opened.Wait(10000), "open did not finish");
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

		// the boot intro muxes a run of video packets ahead of its first audio packet: the reader must keep
		// reading past a full frame queue so the sound gets data, or picture and sound wait on each other
		[Fact]
		public void VideoPacketsMuxedAheadOfAudio_StillStart() {
			if (!OperatingSystem.IsWindows()) return;
			string intro = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "OpenTaiko", "System", "Open-World Memories", "Modules", "Stages", "_boot", "Videos", "intro.mp4"));
			if (!File.Exists(intro)) return;
			ffmpeg_setup();
			int mixer = 0, main = 0;
			StreamProcedure? proc = null;
			try {
				mixer = ManagedBass.Mix.BassMix.CreateMixerStream(48000, 2, BassFlags.MixerNonStop | BassFlags.Decode);
				int mixerHandle = mixer;
				proc = (handle, buffer, length, user) => { int n = Bass.ChannelGetData(mixerHandle, buffer, length); return n < 0 ? 0 : n; };
				main = Bass.CreateStream(48000, 2, BassFlags.Default, proc, IntPtr.Zero);
				Assert.True(Bass.ChannelPlay(main, false), Bass.LastError.ToString());
				CVideoDecoder? video = null;
				var opened = System.Threading.Tasks.Task.Run(() => {
					var v = new CVideoDecoder(intro, (rate, channels, seconds, proc) => {
						var snd = new CSound(ESoundGroup.SongPlayback);
						snd.CreateBassUserSound(rate, channels, seconds, mixerHandle, proc);
						return snd;
					});
					v.InitRead();
					video = v;
				});
				Assert.True(opened.Wait(15000), "open did not finish");
				var sw = Stopwatch.StartNew();
				while (video!.AudioQueuedMs < 100 && sw.ElapsedMilliseconds < 5000) Thread.Sleep(10);
				Assert.True(video.AudioQueuedMs >= 100, $"only {video.AudioQueuedMs:F0} ms of audio queued before playback");
				video.Start();
				Thread.Sleep(500);
				Assert.InRange(video.msPlayPosition, 200.0, 900.0);

				// the controls a player needs, on the real file: seek lands near the target and keeps going
				video.Seek(60000);
				Thread.Sleep(600);
				Assert.InRange(video.msPlayPosition, 60150.0, 61100.0);

				// pause holds the clock, resume releases it
				video.Pause();
				double held = video.msPlayPosition;
				Thread.Sleep(300);
				Assert.InRange(video.msPlayPosition, held - 1, held + 40);
				video.Resume();
				Thread.Sleep(400);
				Assert.InRange(video.msPlayPosition, held + 200, held + 900);

				// speed changes the rate the clock advances at, from the current time
				double before = video.msPlayPosition;
				video.dbPlaySpeed = 2.0;
				Thread.Sleep(800);
				double advanced = video.msPlayPosition - before;
				Assert.InRange(advanced, 1100.0, 2200.0);
				video.dbPlaySpeed = 1.0;

				// volume survives a seek, and Start after Stop begins again from the top
				video.Audio!.SetGain(40);
				video.Seek(5000);
				Assert.InRange(video.Audio!.GetGainPercent(), 39.0, 41.0);
				video.Stop();
				video.Start();
				Thread.Sleep(500);
				Assert.InRange(video.msPlayPosition, 100.0, 900.0);
				video.Stop();
				DisposeWithoutGl(video);
			} finally {
				if (main != 0) Bass.StreamFree(main);
				if (mixer != 0) Bass.StreamFree(mixer);
				GC.KeepAlive(proc);
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
