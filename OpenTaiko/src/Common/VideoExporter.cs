using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FDK;
using FFmpeg.AutoGen;
using ManagedBass;
using ManagedBass.Mix;
using Silk.NET.OpenGLES;

namespace OpenTaiko;

// ═══════════════════════════════════════════════════════════════════════════════════════════════
// Offline gameplay video exporter ("--mode=record --uid <chartUniqueId> --difficulties 3,4").
//
// Boots the game with a hidden window, waits for the song list, jumps straight into an auto-play
// of the requested chart (one player per difficulty, up to 5), and renders the whole play on a
// VIRTUAL CLOCK: one fixed timestep per rendered frame (FDK.Game.VirtualClockMs), so the output
// video is perfectly smooth no matter how fast or slow the machine renders. Every frame is read
// back from the GL backbuffer and encoded into an MP4 via the FFmpeg libraries that already ship
// with the game; every sound the engine actually plays during the take is logged at its virtual
// time (FDK.CSound.SoundPlayCapture) and reproduced offline into the audio track — so hit sounds,
// voices, balloon pops and the BGM land exactly where the game put them.
//
// Everything export-specific lives in this file; the rest of the game carries only tiny hooks
// (Program.cs arg parse, OpenTaiko.Configuration overrides, one Tick call in OpenTaiko.Draw, and
// a Config.ini save gate on exit).
// ═══════════════════════════════════════════════════════════════════════════════════════════════
internal static class VideoExporter {
	public static bool Active { get; private set; }

	private static string _uid = "";
	private static int[] _diffs = Array.Empty<int>();
	private static int _fps = 60;
	private static int _reqW = 0, _reqH = 0;
	private static int _sampleRate = 48000;
	private static string _pathOut = "";

	private enum Phase { Boot, WaitSongs, Loading, Capturing, Done }
	private static Phase _phase = Phase.Boot;
	private static double _msFrameworkTime0;     // virtual time of the first captured frame
	private static long _frames;
	private static FFVideoWriter? _video;
	private static byte[]? _bgra;
	private static string _pathTempVideo = "";

	private readonly record struct SoundEvent(string Path, double msFrameworkTime, double msFxSeek, float Vol, float Pan, double Speed, double msFrameworkTimeEnd = double.PositiveInfinity);
	private static readonly List<SoundEvent> _events = new();
	private static readonly object _eventsLock = new();

	// ── console progress ─────────────────────────────────────────────────────────────────────────
	// OpenTaiko is a WinExe: launched from a terminal it has no console, so stdout is lost unless we
	// attach back to the parent's. Done once when export args are detected.
	[DllImport("kernel32.dll")] private static extern bool AttachConsole(int dwProcessId);
	private const int ATTACH_PARENT_PROCESS = -1;

	private static double _msChartEnd;             // estimated play length (last chip time + outro pad)
	private static long _tickWallStart;            // wall clock at capture start (for speed/ETA)
	private static long _tickLastBar;              // last progress repaint (wall)
	private static int _percentLastBar = -1;       // last printed percent (redirected mode)
	private static bool _lineMode;                 // true when stdout is piped → print lines, not \r

	private static void Status(string msg) {
		Console.WriteLine($"[export] {msg}");
		try {
			Console.Out.Flush();
		} catch { }
	}

	private static void DrawProgress(bool final) {
		long now = Environment.TickCount64;
		if (!final && now - _tickLastBar < 500) // repaint at most twice a second
			return;
		_tickLastBar = now;

		double secVideo = (double)_frames / _fps;
		double secTotal = Math.Max(secVideo, _msChartEnd / 1000.0);
		int pct = Math.Min(final ? 100 : 99, (int)(secVideo * 100 / Math.Max(1.0, secTotal)));
		double secWall = (now - _tickWallStart) / 1000.0;
		double speed = secWall > 0.2 ? secVideo / secWall : 0;
		double secEta = (speed > 0.01 && !final) ? (secTotal - secVideo) / speed : 0;

		if (_lineMode) {
			if (pct == _percentLastBar && !final) // piped output: one line per percent step
				return;
			if (pct % 5 != 0 && !final)
				return;
			_percentLastBar = pct;
			Status($"capturing {pct,3}%  {Fmt(secVideo)}/{Fmt(secTotal)}  {speed:F2}x  ETA {Fmt(secEta)}");
		} else {
			const int W = 28;
			int fill = pct * W / 100;
			string bar = new string('#', fill) + new string('.', W - fill);   // ASCII: survives any codepage
			Console.Write($"\r[export] [{bar}] {pct,3}%  {Fmt(secVideo)}/{Fmt(secTotal)}  {speed:F2}x  ETA {Fmt(secEta)}   ");
			if (final)
				Console.WriteLine();
		}
	}

	private static string Fmt(double sec) => $"{(int)sec / 60}:{(int)sec % 60:00}";

	// ── activation (from --mode=record; args already parsed by CommandLineArgs) ────────────────────

	/// <summary>Arm the exporter from the parsed CLI. Exits the process with code 2 on a usage error
	/// (missing/invalid options). Returns true once armed.</summary>
	public static bool Begin(CommandLineArgs cli) {
		if (string.IsNullOrWhiteSpace(cli.Uid)) {
			Console.Error.WriteLine("--mode=record requires --uid <chartUniqueId>.");
			Environment.Exit(2);
		}
		if (cli.DifficultiesError != null) {
			Console.Error.WriteLine(cli.DifficultiesError);
			Environment.Exit(2);
		}
		foreach (var warn in cli.Warns)
			Console.Error.WriteLine(warn);

		_uid = cli.Uid;
		_diffs = cli.Difficulties;
		_fps = cli.Fps;
		_reqW = cli.Width; _reqH = cli.Height;
		_sampleRate = cli.SampleRate;
		_pathOut = cli.OutPath;
		Active = true;

		// WinExe → reattach stdout to the calling terminal (Windows only; on Linux/macOS the process
		// already inherits the terminal's stdout, and kernel32 doesn't exist there).
		if (OperatingSystem.IsWindows()) {
			try {
				AttachConsole(ATTACH_PARENT_PROCESS);
			} catch { }
		}
		try {
			Console.OutputEncoding = System.Text.Encoding.UTF8;
		} catch { /* legacy console */ }
		_lineMode = Console.IsOutputRedirected;
		Console.WriteLine();
		Status($"video export: uid={_uid} difficulties={string.Join(",", _diffs)} fps={_fps} sample-rate={_sampleRate} -> {_pathOut}");
		Status("booting the game (hidden)...");
		return true;
	}

	// ── boot-time config overrides (called from OpenTaiko.Configuration before window creation) ──

	public static void ApplyBootOverrides(OpenTaiko game) {
		var cfg = OpenTaiko.ConfigIni;
		cfg.bFullScreen = false;
		cfg.bEnableVSync = false;
		cfg.bUseOSTimer = true;                  // CSoundTimer follows Game.TimeMs → our virtual clock
		cfg.fRenderScale = 1f;                   // frames are read back from the window at full size
		cfg.bTokkunMode = false;
		cfg.bAIBattleMode = false;
		cfg.nPlayerCount = _diffs.Length;
		for (int i = 0; i < cfg.bAutoPlay.Length; ++i)
			cfg.bAutoPlay[i] = true;
		if (_reqW > 0 && _reqH > 0) {
			cfg.nWindowWidth = _reqW;
			cfg.nWindowHeight = _reqH;
		}
		game.StartHidden = true;
	}

	// ── per-frame tick (called from OpenTaiko.Draw right after the stage drew) ───────────────────

	/// <summary>Drives the export; returns the (possibly overridden) stage return value.</summary>
	public static int Tick(OpenTaiko game, int stageReturn) {
		if (_phase == Phase.Done)
			return stageReturn;
		try {
			switch (_phase) {
				case Phase.Boot:
					// the boot/startup/title stages run on REAL time so they behave like normal;
					// the virtual clock only takes over once we enter the play flow.
					// Mute at the device master (WASAPI/ASIO bypass Bass.GlobalStreamVolume) — during
					// export the live audio is meaningless anyway (it follows the wall clock while the
					// game runs on the virtual one); the real audio is mixed offline at the end.
					SoundManager.MuteMasterOutput();
					try {
						Bass.GlobalStreamVolume = 0;
					} catch { /* keep exporting even if mute fails */ }
					CSound.SoundPlayCapture = OnSoundPlay;
					CSound.SoundStopCapture = OnSoundStop;
					Status("waiting for the song list...");
					_phase = Phase.WaitSongs;
					break;

				case Phase.WaitSongs:
					// the song list enumerates in the background during the title stage; once it is
					// reflected, resolve the uid and jump straight into song loading.
					if (OpenTaiko.EnumSongs != null && OpenTaiko.EnumSongs.IsSongListEnumCompletelyDone
						&& OpenTaiko.SongManager != null && OpenTaiko.rCurrentStage?.eStageID == CStage.EStage.CUSTOM
						) {
						CSongListNode? node = FindByUid(OpenTaiko.SongManager.listSongRoot, _uid);
						if (node == null) {
							Fail(game, $"No chart with unique id '{_uid}' was found in the song list.");
							break;
						}

						var missing = _diffs.Where(d => node.score[d] == null).ToArray();
						if (missing.Length > 0) {
							var avail = Enumerable.Range(0, 5).Where(d => node.score[d] != null);
							Fail(game, $"Difficulty(ies) {string.Join(",", missing)} not present in this chart. Available: {string.Join(",", avail)}.");
							break;
						}

						OpenTaiko.ConfigIni.nPlayerCount = _diffs.Length;
						for (int i = 0; i < OpenTaiko.ConfigIni.bAutoPlay.Length; ++i)
							OpenTaiko.ConfigIni.bAutoPlay[i] = true;
						OpenTaiko.SongMount.rChoosenSong = node;
						OpenTaiko.SongMount.rChosenScore = node.score[_diffs[0]];
						OpenTaiko.SongMount.strChosenSongGenre = node.songGenre;
						for (int i = 0; i < _diffs.Length; ++i)
							OpenTaiko.SongMount.nChoosenSongDifficulty[i] = _diffs[i];

						// from here on, game time = frame index / fps (smoothness by construction)
						Game.VirtualClockMs = Game.dbTimeMs;
						Game.VirtualClockEnabled = true;

						Status($"chart found: \"{node.ldTitle.GetString("")}\" - loading...");
						game.UnmountAndChangeStage(OpenTaiko.stageSongLoading, $"Video export: {node.ldTitle.GetString("")} [{string.Join(",", _diffs)}]");
						stageReturn = 0;
						_phase = Phase.Loading;
					}
					break;

				case Phase.Loading:
					if (ReferenceEquals(OpenTaiko.rCurrentStage, OpenTaiko.stageGameScreen)) {
						StartCapture();
						_msFrameworkTime0 = Game.VirtualClockMs;
						_phase = Phase.Capturing;
						CaptureFrame();          // the stage already drew its first frame this tick
					}
					break;

				case Phase.Capturing:
					CaptureFrame();
					DrawProgress(false);
					if (stageReturn == (int)EGameplayScreenReturnValue.StageCleared
						|| stageReturn == (int)EGameplayScreenReturnValue.StageFailed
						|| stageReturn == (int)EGameplayScreenReturnValue.PerformanceInterrupted
						) {
						stageReturn = 0;         // suppress the results screen; we are done
						Finish(game);
					}
					break;
			}
		} catch (Exception ex) {
			Fail(game, ex.ToString());
			stageReturn = 0;
		}

		if (_phase != Phase.Done && Game.VirtualClockEnabled)
			Game.VirtualClockMs += 1000.0 / _fps;
		return stageReturn;
	}

	// ── sound capture ────────────────────────────────────────────────────────────────────────────

	private static readonly Dictionary<CSound, int> _sndToEventIdx = [];
	private static readonly double _volZero = CSound.LufsToLinear(CSound.MinimumLufs);

	private static void OnSoundPlay(CSound snd, double msFrameworkTime) {
		const float volErr = 1e-9f;
		if (_phase != Phase.Capturing && _phase != Phase.Loading)
			return;
		string? f = snd.FileName;
		if (string.IsNullOrEmpty(f) || !File.Exists(f))
			return;
		var (vol, pan) = snd.tGetChannelLevels();
		if (vol <= _volZero + volErr)
			return;
		snd.tGetPlayPosition(out var bytesSeek, out var msSeek);
		lock (_eventsLock) {
			_sndToEventIdx[snd] = _events.Count;
			_events.Add(new SoundEvent(f, msFrameworkTime, msSeek, vol, pan, snd.PlaySpeed * snd.Frequency));
		}
	}

	private static void OnSoundStop(CSound snd, double msFrameworkTime) {
		if (_phase != Phase.Capturing && _phase != Phase.Loading)
			return;
		lock (_eventsLock) {
			if (!_sndToEventIdx.TryGetValue(snd, out int idx))
				return;
			_events[idx] = _events[idx] with { msFrameworkTimeEnd = msFrameworkTime };
			_sndToEventIdx.Remove(snd);
		}
	}

	// ── chart lookup ─────────────────────────────────────────────────────────────────────────────

	private static CSongListNode? FindByUid(List<CSongListNode>? roots, string uid) {
		if (roots == null)
			return null;
		var stack = new Stack<CSongListNode>(roots);
		while (stack.Count > 0) {
			var n = stack.Pop();
			if (n == null)
				continue;
			string id = n.tGetUniqueId();
			if (!string.IsNullOrEmpty(id) && id == uid)
				return n;
			if (n.childrenList != null) {
				foreach (var c in n.childrenList)
					stack.Push(c);
			}
		}
		return null;
	}

	// ── frame capture ────────────────────────────────────────────────────────────────────────────

	private static unsafe void StartCapture() {
		int w = Game.ViewPortSize.X & ~1,
			h = Game.ViewPortSize.Y & ~1;   // even for yuv420
		if (w < 16 || h < 16)
			throw new InvalidOperationException($"Viewport too small to export ({w}x{h}).");
		_pathTempVideo = _pathOut + ".video.tmp.mp4";
		_video = new FFVideoWriter(_pathTempVideo, w, h, _fps);
		_bgra = new byte[w * h * 4];

		// the last chip in the (time-sorted) chart marks the play's end → a real percentage is possible
		try {
			var chips = OpenTaiko.TJA?.listChip;
			_msChartEnd = (chips != null && chips.Count > 0 ? chips[chips.Count - 1].nSoundTimems : 120000) + 4000;   // + outro fade pad
		} catch { _msChartEnd = 120000; }
		_tickWallStart = Environment.TickCount64;

		Trace.TraceInformation($"[export] capture started: {w}x{h}@{_fps} → {_pathOut}");
		Status($"capturing {w}x{h}@{_fps} (~{Fmt(_msChartEnd / 1000.0)} of play):");
	}

	private static unsafe void CaptureFrame() {
		if (_video == null || _bgra == null)
			return;
		fixed (byte* p = _bgra) {
			Game.Gl.ReadPixels(Game.ViewPortOffset.X, Game.ViewPortOffset.Y,
				(uint)_video.Width, (uint)_video.Height, PixelFormat.Bgra, GLEnum.UnsignedByte, p);
		}
		_video.WriteFrame(_bgra);     // bottom-up; the writer flips via negative stride
		++_frames;
	}

	// ── finalization ─────────────────────────────────────────────────────────────────────────────

	private static void EndAudioCapture() {
		lock (_eventsLock) {
			CSound.SoundPlayCapture = null;
			CSound.SoundStopCapture = null;
			_sndToEventIdx.Clear();
		}
	}

	private static void Finish(OpenTaiko game) {
		DrawProgress(true);
		Trace.TraceInformation($"[export] play finished after {_frames} frames; mixing audio + muxing...");
		Status($"play captured ({_frames} frames). finalizing video...");
		EndAudioCapture();
		_video?.FinishVideo(); // can throw errors

		double secMixLength = (double)_frames / _fps;
		int eventCount; lock (_eventsLock) eventCount = _events.Count;
		Status($"mixing audio ({eventCount} sound events)...");
		float[] pcm = MixAudio(secMixLength, _sampleRate);
		lock (_eventsLock)
			_events.Clear();

		Status("muxing final mp4...");
		FFVideoWriter.MuxWithAudio(_pathTempVideo, _pathOut, pcm, _sampleRate);
		try {
			File.Delete(_pathTempVideo);
		} catch { }

		string full = Path.GetFullPath(_pathOut);
		double wallTotal = (Environment.TickCount64 - _tickWallStart) / 1000.0;
		Trace.TraceInformation($"[export] done: {full} ({secMixLength:F1}s)");
		Status($"done: {full}");
		Status($"{secMixLength:F1}s of video in {Fmt(wallTotal)} ({secMixLength / Math.Max(0.1, wallTotal):F2}x realtime)");
		_phase = Phase.Done;
		// hard exit: the output is written and a one-shot export process has nothing worth saving;
		// the normal teardown path (GL/DB/Lua finalizers) is not worth crashing over after the fact
		Trace.Flush();
		Environment.Exit(0);
	}

	private static void Fail(OpenTaiko game, string message) {
		if (!_lineMode)
			Console.WriteLine();
		Trace.TraceError("[export] FAILED: " + message);
		Status("FAILED: " + message);
		Console.Error.WriteLine("Export failed: " + message);
		EndAudioCapture();
		try {
			_video?.FinishVideo();
		} catch { }
		try {
			if (_pathTempVideo != "" && File.Exists(_pathTempVideo))
				File.Delete(_pathTempVideo);
		} catch { }
		_phase = Phase.Done;
		Trace.Flush();
		Environment.Exit(2);
	}

	// ── offline audio mix (BASS decode → resampled stereo float, events at their virtual times) ─────

	private static readonly Dictionary<string, float[]> _pcmCache = new();

	private static float[] DecodeToStereoResampled(string path, int sampleRate) {
		if (_pcmCache.TryGetValue(path, out var cached))
			return cached;
		float[] result = Array.Empty<float>();
		int src = Bass.CreateStream(path, 0, 0, BassFlags.Decode | BassFlags.Float | BassFlags.Prescan);
		if (src != 0) {
			int mix = BassMix.CreateMixerStream(sampleRate, 2, BassFlags.Decode | BassFlags.Float | BassFlags.MixerEnd);
			if (mix != 0) {
				BassMix.MixerAddChannel(mix, src, BassFlags.MixerDownMix);
				var chunks = new List<float[]>();
				var buf = new float[sampleRate * 2];
				long total = 0;
				while (true) {
					int bytes = Bass.ChannelGetData(mix, buf, buf.Length * 4);
					if (bytes <= 0)
						break;
					int n = bytes / 4;
					var c = new float[n];
					Array.Copy(buf, c, n);
					chunks.Add(c);
					total += n;
				}
				result = new float[total];
				long off = 0;
				foreach (var c in chunks) {
					Array.Copy(c, 0, result, off, c.Length);
					off += c.Length;
				}
				Bass.StreamFree(mix);
			}
			Bass.StreamFree(src);
		}
		_pcmCache[path] = result;
		return result;
	}

	private static float[] MixAudio(double secMixLength, int samplingRate) {
		long frameMixLength = (long)(secMixLength * samplingRate) + 4800; // small tail headroom
		var mixL = new float[frameMixLength * 2];             // interleaved L/R

		SoundEvent[] events;
		lock (_eventsLock) events = _events.ToArray();
		Trace.TraceInformation($"[export] mixing {events.Length} sound events in {_sampleRate} Hz");
		foreach (var e in events.Take(12))
			Trace.TraceInformation($"[export]   t={e.msFrameworkTime - _msFrameworkTime0,8:F1}ms - {e.msFrameworkTimeEnd - _msFrameworkTime0,8:F1}ms vol={e.Vol:F2} pan={e.Pan:F2} {Path.GetFileName(e.Path)}");

		foreach (var ev in events) {
			// fx time is related to a single fx audio, mix time is related to the whole mix audio
			double msMixStart = ev.msFrameworkTime - _msFrameworkTime0;
			if (msMixStart < -0.5)              // pre-capture noise (loading screen)
				continue;
			if (msMixStart < 0)
				msMixStart = 0;
			long frameMixStart = (long)(msMixStart / 1000.0 * samplingRate);
			if (frameMixStart >= frameMixLength)
				continue;

			double msMixEnd = ev.msFrameworkTimeEnd - _msFrameworkTime0;
			double frameMixEnd = Math.Truncate(msMixEnd / 1000.0 * samplingRate);

			long frameFxSeek = (long)(samplingRate * (ev.msFxSeek / 1000.0));
			float[] pcm = DecodeToStereoResampled(ev.Path, samplingRate);
			long frameFxLength = pcm.Length / 2;
			if (frameFxLength < frameFxSeek)
				continue;

			float volL = ev.Vol * Math.Min(1f, 1f - ev.Pan);
			float volR = ev.Vol * Math.Min(1f, 1f + ev.Pan);
			double speed = (ev.Speed > 0.01 && Math.Abs(ev.Speed - 1.0) > 0.001) ? ev.Speed : 1.0;

			long frameMixSustained = (frameMixEnd - frameMixStart >= long.MaxValue) ? long.MaxValue : (long)(frameMixEnd - frameMixStart);
			long frameFxSustained = (frameMixSustained * speed >= long.MaxValue) ? long.MaxValue : (long)(frameMixSustained * speed);
			long frameFxPlayed = Math.Min(frameFxLength - frameFxSeek, frameFxSustained);
			long frameMixPlayed = (long)(frameFxPlayed / speed);
			long frameMixPlayable = frameMixLength - frameMixStart;
			if (frameMixPlayed > frameMixPlayable)
				frameMixPlayed = frameMixPlayable;

			for (long frameMixI = 0; frameMixI < frameMixPlayed; ++frameMixI) {
				long frameFxRelI = ((speed == 1.0) ? frameMixI : (long)(frameMixI * speed));
				long frameFxI = frameFxSeek + frameFxRelI;
				if (frameFxRelI >= frameFxPlayed)
					break;
				long sampleMixI = (frameMixStart + frameMixI) * 2;
				mixL[sampleMixI] += pcm[frameFxI * 2] * volL;
				mixL[sampleMixI + 1] += pcm[frameFxI * 2 + 1] * volR;
			}
		}

		// gentle fade-out over the last half second (avoids a click where the BGM is truncated)
		long frameMixFadeLength = Math.Min(frameMixLength, samplingRate / 2);
		for (long i = 0; i < frameMixFadeLength; ++i) {
			float g = (float)i / frameMixFadeLength;
			long o = (frameMixLength - 1 - i) * 2;
			mixL[o] *= g; mixL[o + 1] *= g;
		}
		return mixL;
	}
}

// ═══════════════════════════════════════════════════════════════════════════════════════════════
// Minimal MP4 writer on the FFmpeg libraries that ship with the game (FFmpeg.AutoGen).
// Phase A: BGRA frames → H.264 (h264_mf on Windows; libx264/openh264/mpeg4 fallbacks) in a temp MP4.
// Phase B (static MuxWithAudio): temp video stream copied + the mixed PCM encoded to AAC,
// interleaved into the final file.
// ═══════════════════════════════════════════════════════════════════════════════════════════════
internal unsafe sealed class FFVideoWriter {
	public int Width { get; }
	public int Height { get; }
	private readonly int _fps;
	private readonly string _path;

	// all FFmpeg state is owned by the worker thread: it is MTA (the game's main thread is STA,
	// which makes Media Foundation refuse hardware MFTs and print "COM must not be in STA mode"),
	// and running there overlaps encoding with the game's rendering.
	private readonly Thread _worker;
	private readonly BlockingCollection<byte[]> _queue = new(boundedCapacity: 4);
	private readonly ConcurrentBag<byte[]> _pool = new();
	private readonly ManualResetEventSlim _ready = new(false);
	private volatile Exception? _error;

	private AVFormatContext* _fmt;
	private AVCodecContext* _enc;
	private AVStream* _stream;
	private SwsContext* _sws;
	private AVFrame* _frame;
	private AVPacket* _pkt;
	private long _pts;

	private static readonly string[] VideoCodecPreference = { "h264_mf", "libx264", "libopenh264", "mpeg4" };

	private static void Warn(string msg) {
		msg = $"[{nameof(VideoExporter)}] {msg}";
		Console.Error.WriteLine(msg);
		try {
			Console.Error.Flush();
		} catch { }
		Trace.TraceWarning(msg);
	}

	private static int Check(int err, string what) {
		if (err < 0) {
			byte* buf = stackalloc byte[1024];
			ffmpeg.av_strerror(err, buf, 1024);
			throw new InvalidOperationException($"{what} failed: {Marshal.PtrToStringAnsi((IntPtr)buf)} ({err})");
		}
		return err;
	}

	public FFVideoWriter(string path, int w, int h, int fps) {
		Width = w; Height = h; _fps = fps; _path = path;
		_worker = new Thread(WorkerMain) { Name = "ExportVideoEncoder", IsBackground = true };
		_worker.SetApartmentState(ApartmentState.MTA);
		_worker.Start();
		_ready.Wait();                                       // surface init failures synchronously
		if (_error != null)
			throw _error;
	}

	/// <summary>Queues one BGRA frame stored bottom-up (straight from glReadPixels). Blocks only
	/// when the encoder is more than 4 frames behind (backpressure).</summary>
	public void WriteFrame(byte[] bgraBottomUp) {
		if (_error != null)
			throw _error;
		if (!_pool.TryTake(out var buf) || buf.Length != bgraBottomUp.Length)
			buf = new byte[bgraBottomUp.Length];
		System.Buffer.BlockCopy(bgraBottomUp, 0, buf, 0, bgraBottomUp.Length);
		_queue.Add(buf);
	}

	public void FinishVideo() {
		if (!_queue.IsAddingCompleted)
			_queue.CompleteAdding();
		if (_worker.IsAlive)
			_worker.Join();
		if (_error != null)
			throw _error;
	}

	private void WorkerMain() {
		try {
			InitFfmpeg();
		} catch (Exception ex) {
			_error = ex;
			_ready.Set();
			return;
		}
		_ready.Set();
		try {
			foreach (var buf in _queue.GetConsumingEnumerable()) {
				EncodeFrame(buf);
				_pool.Add(buf);
			}
			// flush + trailer + teardown
			ffmpeg.avcodec_send_frame(_enc, null);
			DrainVideo();
			ffmpeg.av_write_trailer(_fmt);
			var pkt = _pkt; ffmpeg.av_packet_free(&pkt); _pkt = null;
			var frame = _frame; ffmpeg.av_frame_free(&frame); _frame = null;
			ffmpeg.sws_freeContext(_sws); _sws = null;
			var enc = _enc; ffmpeg.avcodec_free_context(&enc); _enc = null;
			ffmpeg.avio_closep(&_fmt->pb);
			ffmpeg.avformat_free_context(_fmt); _fmt = null;
		} catch (Exception ex) { _error = ex; }
	}

	private void InitFfmpeg() {
		string path = _path;
		int w = Width, h = Height, fps = _fps;
		ffmpeg.av_log_set_level(ffmpeg.AV_LOG_ERROR);        // hush the MFT info chatter

		AVFormatContext* fmt;
		Check(ffmpeg.avformat_alloc_output_context2(&fmt, null, null, path), "alloc output");
		_fmt = fmt;

		// some encoders are registered but refuse to open on a given machine (h264_mf is moody),
		// so actually try-open each candidate — with and without GLOBAL_HEADER — before giving up
		AVPixelFormat dstFmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
		foreach (string name in VideoCodecPreference) {
			AVCodec* codec = ffmpeg.avcodec_find_encoder_by_name(name);
			if (codec == null)
				continue;
			bool wantGlobal = (_fmt->oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) != 0;
			for (int attempt = 0; attempt < (wantGlobal ? 2 : 1); ++attempt) {
				AVCodecContext* enc = ffmpeg.avcodec_alloc_context3(codec);
				enc->width = w;
				enc->height = h;
				enc->time_base = new AVRational { num = 1, den = fps };
				enc->framerate = new AVRational { num = fps, den = 1 };
				enc->gop_size = fps * 2;
				enc->max_b_frames = 0;
				enc->bit_rate = (long)w * h * fps * 12 / 100;        // ≈8 Mbps at 720p60, scales with size
				AVPixelFormat fmtTry = AVPixelFormat.AV_PIX_FMT_YUV420P;
				if (codec->pix_fmts != null && *codec->pix_fmts != AVPixelFormat.AV_PIX_FMT_NONE)
					fmtTry = *codec->pix_fmts;
				enc->pix_fmt = fmtTry;
				if (wantGlobal && attempt == 0)
					enc->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
				if (ffmpeg.avcodec_open2(enc, codec, null) >= 0) {
					_enc = enc;
					dstFmt = fmtTry;
					Trace.TraceInformation($"[export] video encoder: {name}{(attempt == 1 ? " (no global header)" : "")}");
					break;
				}
				ffmpeg.avcodec_free_context(&enc);
			}
			if (_enc != null)
				break;
		}
		if (_enc == null)
			throw new InvalidOperationException("No usable H.264/MPEG-4 encoder in the shipped FFmpeg (tried: " + string.Join(", ", VideoCodecPreference) + ").");

		_stream = ffmpeg.avformat_new_stream(_fmt, null);
		Check(ffmpeg.avcodec_parameters_from_context(_stream->codecpar, _enc), "stream params");
		_stream->time_base = _enc->time_base;

		Check(ffmpeg.avio_open(&_fmt->pb, path, ffmpeg.AVIO_FLAG_WRITE), "open file");
		Check(ffmpeg.avformat_write_header(_fmt, null), "write header");

		_sws = ffmpeg.sws_getContext(w, h, AVPixelFormat.AV_PIX_FMT_BGRA, w, h, dstFmt,
			ffmpeg.SWS_BILINEAR, null, null, null);
		if (_sws == null)
			throw new InvalidOperationException("sws_getContext failed.");

		_frame = ffmpeg.av_frame_alloc();
		_frame->format = (int)dstFmt;
		_frame->width = w;
		_frame->height = h;
		Check(ffmpeg.av_frame_get_buffer(_frame, 0), "frame buffer");
		_pkt = ffmpeg.av_packet_alloc();
	}

	private void EncodeFrame(byte[] bgraBottomUp) {
		fixed (byte* p = bgraBottomUp) {
			// negative stride: hand sws the LAST row and walk upwards = vertical flip for free
			byte*[] src = { p + (Height - 1) * Width * 4, null, null, null };
			int[] stride = { -Width * 4, 0, 0, 0 };
			byte*[] dst = { _frame->data[0], _frame->data[1], _frame->data[2], _frame->data[3] };
			int[] dstStride = { _frame->linesize[0], _frame->linesize[1], _frame->linesize[2], _frame->linesize[3] };
			ffmpeg.sws_scale(_sws, src, stride, 0, Height, dst, dstStride);
		}
		_frame->pts = _pts; ++_pts;
		Check(ffmpeg.avcodec_send_frame(_enc, _frame), "send frame");
		DrainVideo();
	}

	private void DrainVideo() {
		while (true) {
			int r = ffmpeg.avcodec_receive_packet(_enc, _pkt);
			if (r == ffmpeg.AVERROR(ffmpeg.EAGAIN) || r == ffmpeg.AVERROR_EOF)
				return;
			Check(r, "receive packet");
			ffmpeg.av_packet_rescale_ts(_pkt, _enc->time_base, _stream->time_base);
			_pkt->stream_index = _stream->index;
			Check(ffmpeg.av_interleaved_write_frame(_fmt, _pkt), "write packet");
			ffmpeg.av_packet_unref(_pkt);
		}
	}

	// ── phase B: copy the temp video stream + encode the mixed PCM to AAC into the final MP4 ────
	private const double dBFSMax = -1; // headroom for AAC encoder reshaping the waveform
	private static readonly float ampHardClip = (float)double.Pow(10, dBFSMax / 20);
	private static readonly double ampLimitTarget = double.Pow(10, dBFSMax / 20);

	public static void MuxWithAudio(string videoPath, string finalPath, float[] pcmInterleaved, int sampleRate) {
		AVFormatContext* inFmt = null;
		Check(ffmpeg.avformat_open_input(&inFmt, videoPath, null, null), "open temp video");
		Check(ffmpeg.avformat_find_stream_info(inFmt, null), "stream info");
		int vIdx = Check(ffmpeg.av_find_best_stream(inFmt, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, null, 0), "find video");
		AVStream* inV = inFmt->streams[vIdx];

		AVFormatContext* outFmt;
		Check(ffmpeg.avformat_alloc_output_context2(&outFmt, null, null, finalPath), "alloc final output");

		// video: stream copy
		AVStream* outV = ffmpeg.avformat_new_stream(outFmt, null);
		Check(ffmpeg.avcodec_parameters_copy(outV->codecpar, inV->codecpar), "copy video params");
		outV->codecpar->codec_tag = 0;
		outV->time_base = inV->time_base;

		// audio: AAC resampled stereo
		AVCodec* aac = ffmpeg.avcodec_find_encoder_by_name("aac");
		if (aac == null)
			throw new InvalidOperationException("AAC encoder unavailable in the shipped FFmpeg.");
		AVCodecContext* aenc = ffmpeg.avcodec_alloc_context3(aac);
		aenc->sample_rate = sampleRate;
		ffmpeg.av_channel_layout_default(&aenc->ch_layout, 2);
		aenc->sample_fmt = AVSampleFormat.AV_SAMPLE_FMT_FLTP;
		aenc->bit_rate = 192_000;
		aenc->time_base = new AVRational { num = 1, den = sampleRate };
		if ((outFmt->oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
			aenc->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
		Check(ffmpeg.avcodec_open2(aenc, aac, null), "open aac");

		AVStream* outA = ffmpeg.avformat_new_stream(outFmt, null);
		Check(ffmpeg.avcodec_parameters_from_context(outA->codecpar, aenc), "audio params");
		outA->time_base = new AVRational { num = 1, den = sampleRate };

		Check(ffmpeg.avio_open(&outFmt->pb, finalPath, ffmpeg.AVIO_FLAG_WRITE), "open final file");
		Check(ffmpeg.avformat_write_header(outFmt, null), "final header");

		AVPacket* pkt = ffmpeg.av_packet_alloc();
		AVFrame* af = ffmpeg.av_frame_alloc();
		AVFrame* afFilter = ffmpeg.av_frame_alloc();
		int frameSize = aenc->frame_size > 0 ? aenc->frame_size : 1024;
		long totalSamples = pcmInterleaved.Length / 2;
		long sent = 0;
		long sentFiltered = 0;

		AVFilterGraph* agraph = null;
		AVFilterContext* asrc = null, asink = null;
		try {
			InitFilterGraph(aenc, frameSize, out agraph, out asrc, out asink);
		} catch (Exception e) {
			Warn($"init volume limiter filters failed, proceed with simple volume clamp: {e}");
		}

		// static local functions: pointer params keep them closure-free (CS1686)
		static void DrainAudio(AVCodecContext* aenc, AVPacket* pkt, AVFormatContext* outFmt, AVStream* outA) {
			while (true) {
				int r = ffmpeg.avcodec_receive_packet(aenc, pkt);
				if (r == ffmpeg.AVERROR(ffmpeg.EAGAIN) || r == ffmpeg.AVERROR_EOF)
					return;
				Check(r, "receive audio packet");
				ffmpeg.av_packet_rescale_ts(pkt, aenc->time_base, outA->time_base);
				pkt->stream_index = outA->index;
				Check(ffmpeg.av_interleaved_write_frame(outFmt, pkt), "write audio packet");
				ffmpeg.av_packet_unref(pkt);
			}
		}

		static void DrainAudioFilter(AVCodecContext* aenc, AVPacket* pkt, AVFormatContext* outFmt, AVStream* outA, AVFilterContext* asink, AVFrame* afFilter, ref long sentFiltered) {
			while (true) {
				int err = ffmpeg.av_buffersink_get_frame(asink, afFilter);
				if (err == ffmpeg.AVERROR(ffmpeg.EAGAIN) || err == ffmpeg.AVERROR_EOF)
					return;
				Check(err, "filter audio data");
				try {
					afFilter->pts = sentFiltered;
					Check(ffmpeg.avcodec_send_frame(aenc, afFilter), "send audio frame");
					DrainAudio(aenc, pkt, outFmt, outA);
					sentFiltered += afFilter->nb_samples;
				} finally {
					ffmpeg.av_frame_unref(afFilter);
				}
			}
		}

		// hard safety clamp
		static void HardClipAudio(int nSamples, float* l, float* r) {
			for (int i = 0; i < nSamples; ++i) {
				l[i] = float.Clamp(l[i], -ampHardClip, ampHardClip);
				r[i] = float.Clamp(r[i], -ampHardClip, ampHardClip);
			}
		}

		static void SendNextAudioChunk(AVCodecContext* aenc, AVFrame* af, AVPacket* pkt, AVFormatContext* outFmt, AVStream* outA, float[] pcm,
			int sampleRate, int frameSize, long totalSamples, ref long sent, AVFilterGraph* agraph, AVFilterContext* asrc,
			AVFilterContext* asink, AVFrame* afFilter, ref long sentFiltered
			) {
			int n = (int)Math.Min(frameSize, totalSamples - sent);
			if (n <= 0)
				return;
			af->nb_samples = n;
			af->format = (int)AVSampleFormat.AV_SAMPLE_FMT_FLTP;
			af->sample_rate = sampleRate;
			af->time_base = new AVRational { num = 1, den = sampleRate };
			ffmpeg.av_channel_layout_default(&af->ch_layout, 2);
			Check(ffmpeg.av_frame_get_buffer(af, 0), "audio frame buffer");
			float* l = (float*)af->data[0];
			float* r = (float*)af->data[1];
			for (int i = 0; i < n; ++i) {
				l[i] = pcm[(sent + i) * 2];
				r[i] = pcm[(sent + i) * 2 + 1];
			}
			af->pts = sent;
			sent += n;

			if (agraph == null) { // filter not available
				HardClipAudio(n, l, r);
				Check(ffmpeg.avcodec_send_frame(aenc, af), "send audio frame");
			} else {
				Check(ffmpeg.av_buffersrc_write_frame(asrc, af), "submit frame to filtergraph");
				DrainAudioFilter(aenc, pkt, outFmt, outA, asink, afFilter, ref sentFiltered);
			}
			ffmpeg.av_frame_unref(af);
			DrainAudio(aenc, pkt, outFmt, outA);
		}

		// interleave: keep the audio clock just ahead of each video packet's time
		while (true) {
			int r = ffmpeg.av_read_frame(inFmt, pkt);
			if (r == ffmpeg.AVERROR_EOF)
				break;
			Check(r, "read video packet");
			if (pkt->stream_index != vIdx) {
				ffmpeg.av_packet_unref(pkt);
				continue;
			}

			double secVid = pkt->dts != ffmpeg.AV_NOPTS_VALUE ? pkt->dts * ffmpeg.av_q2d(inV->time_base) : 0;
			// copy the packet aside while we top up audio (pkt is reused by the audio drain)
			AVPacket* vcopy = ffmpeg.av_packet_clone(pkt);
			ffmpeg.av_packet_unref(pkt);

			while (sent < totalSamples && (double)sent / sampleRate <= secVid)
				SendNextAudioChunk(aenc, af, pkt, outFmt, outA, pcmInterleaved, sampleRate, frameSize, totalSamples, ref sent, agraph, asrc, asink, afFilter, ref sentFiltered);

			ffmpeg.av_packet_rescale_ts(vcopy, inV->time_base, outV->time_base);
			vcopy->stream_index = outV->index;
			Check(ffmpeg.av_interleaved_write_frame(outFmt, vcopy), "write video packet");
			ffmpeg.av_packet_free(&vcopy);
		}
		while (sent < totalSamples)
			SendNextAudioChunk(aenc, af, pkt, outFmt, outA, pcmInterleaved, sampleRate, frameSize, totalSamples, ref sent, agraph, asrc, asink, afFilter, ref sentFiltered);
		if (agraph != null) {
			ffmpeg.av_buffersrc_write_frame(asrc, null);
			DrainAudioFilter(aenc, pkt, outFmt, outA, asink, afFilter, ref sentFiltered);
		}
		ffmpeg.avcodec_send_frame(aenc, null);
		DrainAudio(aenc, pkt, outFmt, outA);

		ffmpeg.av_write_trailer(outFmt);

		ffmpeg.av_packet_free(&pkt);
		ffmpeg.av_frame_free(&af);
		ffmpeg.av_frame_free(&afFilter);
		ffmpeg.avfilter_graph_free(&agraph);
		ffmpeg.avcodec_free_context(&aenc);
		ffmpeg.avio_closep(&outFmt->pb);
		ffmpeg.avformat_free_context(outFmt);
		ffmpeg.avformat_close_input(&inFmt);
	}

	// (input) -> abuffer(src) -> alimiter -> aformat -> abuffersink -> (output)
	// * abuffer(src): to feed the decoded samples
	// * alimiter: to prevent input volume from exceeding the threshold
	// * aformat: make the sample input format same as input (otherwise the float input would become double output)
	// * abuffersink: to read the filtered samples
	private static void InitFilterGraph(AVCodecContext* aenc, int frameSize, out AVFilterGraph* agraph, out AVFilterContext* asrc, out AVFilterContext* asink) {
		AVFilterGraph* filter_graph = ffmpeg.avfilter_graph_alloc();
		if (filter_graph == null)
			Check(ffmpeg.AVERROR(ffmpeg.ENOMEM), "create filter graph");

		try {
			// abuffer(src) filter
			AVFilter* abuffer = ffmpeg.avfilter_get_by_name("abuffer");
			if (abuffer == null)
				Check(ffmpeg.AVERROR_FILTER_NOT_FOUND, "find abuffer filter");
			AVFilterContext* abuffer_ctx = ffmpeg.avfilter_graph_alloc_filter(filter_graph, abuffer, "src");
			if (abuffer_ctx == null)
				Check(ffmpeg.AVERROR(ffmpeg.ENOMEM), "alloc abuffer instance");
			AVBufferSrcParameters* srcParams = ffmpeg.av_buffersrc_parameters_alloc();
			if (srcParams == null)
				Check(ffmpeg.AVERROR(ffmpeg.ENOMEM), "alloc abuffer parameters instance");
			try {
				srcParams->format = (int)aenc->sample_fmt;
				srcParams->time_base = aenc->time_base;
				srcParams->sample_rate = aenc->sample_rate;
				Check(ffmpeg.av_channel_layout_copy(&srcParams->ch_layout, &aenc->ch_layout), "copy abuffer channel layout params");
				Check(ffmpeg.av_buffersrc_parameters_set(abuffer_ctx, srcParams), "set abuffer filter parameters");
				Check(ffmpeg.avfilter_init_str(abuffer_ctx, null), "init abuffer filter");
			} finally {
				ffmpeg.av_free(srcParams);
			}

			AVFilterContext*[] ctxs = [abuffer_ctx, null, null, null];
			foreach (var (i, filterName, nodeName, args) in new[] {
				// alimiter available options: https://ffmpeg.org/doxygen/5.1/af__alimiter_8c_source.html#l00081
				(1, "alimiter", "alimiter", $"limit={ampLimitTarget}:asc=1:asc_level=1:level=0:latency=1"),
				(2, "aformat", "aformat", $"sample_fmts={ffmpeg.av_get_sample_fmt_name(aenc->sample_fmt)}"),
				(3, "abuffersink", "sink", null),
				}) {
				AVFilter* filter = ffmpeg.avfilter_get_by_name(filterName);
				if (filter == null)
					Check(ffmpeg.AVERROR_FILTER_NOT_FOUND, $"find {filterName} filter for {nodeName}");
				fixed (AVFilterContext** pctx = &ctxs[i])
					Check(ffmpeg.avfilter_graph_create_filter(pctx, filter, nodeName, args, null, filter_graph), $"create {filterName} filter for {nodeName}");
			}

			for (int i = 1; i < ctxs.Length; ++i)
				Check(ffmpeg.avfilter_link(ctxs[i - 1], 0, ctxs[i], 0), "connect filters");

			Check(ffmpeg.avfilter_graph_config(filter_graph, null), "configure filter graph");

			AVFilterContext* abuffersink_ctx = ctxs[ctxs.Length - 1];
			ffmpeg.av_buffersink_set_frame_size(abuffersink_ctx, (uint)frameSize); // same frame size except last needed for AAC encoder

			agraph = filter_graph;
			filter_graph = null; // ensure agraph will not be freed
			asrc = abuffer_ctx;
			asink = abuffersink_ctx;
		} finally {
			ffmpeg.avfilter_graph_free(&filter_graph);
		}
	}
}
