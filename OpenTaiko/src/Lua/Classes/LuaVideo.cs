using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;

namespace OpenTaiko {
	public class LuaVideo : IDisposable, ILuaSeekableInMs {
		private CVideoDecoder? _video = null;
		internal CTexture? _tmpTex = null;
		internal HashSet<LuaVideo>? _disposeList = null;

		// Play intent buffered while the decoder opens asynchronously (off-thread); applied in SetVideo so a
		// Start()/seek/speed issued right after CreateVideo (before the decoder is ready) isn't lost.
		private bool _pendingStart;
		private double? _pendingSeek;
		private double? _pendingSpeed;
		private double? _pendingVolume;

		public LuaVideo() {
			_video = null;
		}

		public LuaVideo(CVideoDecoder video) {
			_video = video;
		}

		// Attach the decoder once it's been opened off-thread (async-load path) + apply any buffered play state.
		// Until then every method no-ops and Texture returns empty, so the video just starts when it's ready.
		internal void SetVideo(CVideoDecoder video) {
			if (_disposedValue) { System.Diagnostics.Trace.TraceInformation("[vopen] attached after dispose; dropping"); video.Dispose(); return; }   // disposed before the open finished → don't leak
			System.Diagnostics.Trace.TraceInformation("[vopen] attached"); // DEBUG probe
			_video = video;
			if (_pendingSeek.HasValue) video.Seek((long)_pendingSeek.Value);
			if (_pendingSpeed.HasValue) video.dbPlaySpeed = _pendingSpeed.Value;
			if (_pendingVolume.HasValue) video.Audio?.SetGain((int)Math.Clamp(_pendingVolume.Value, 0, 100));
			if (_pendingStart) video.Start();
			_pendingStart = false; _pendingSeek = null; _pendingSpeed = null; _pendingVolume = null;
		}

		public void Start() {
			if (_video != null) _video.Start(); else _pendingStart = true;
		}

		public void Resume() {
			if (_video != null) _video.Resume(); else _pendingStart = true;
		}

		public void Pause() {
			if (_video != null) _video.Pause(); else _pendingStart = false;
		}

		public void Stop() {
			if (_video != null) _video.Stop(); else _pendingStart = false;
		}

		public void Reset() {
			if (_video != null) _video.Seek(0); else _pendingSeek = 0;
		}


		#region Gets
		public int Width => _video?.FrameSize.Width ?? -1;
		public int Height => _video?.FrameSize.Height ?? -1;
		public double DurationMs => Duration * 1000.0;
		public double Duration => _video?.Duration ?? 1; // older API
		public LuaTexture Texture {
			get {
				if (_video != null) {
					_video.GetNowFrame(ref _tmpTex);

					LuaTexture frametex = new LuaTexture(_tmpTex);
					return frametex;
				}

				LuaTexture emptytex = new LuaTexture();
				return emptytex;
			}
		}

		public double GetTimestampMs() => _video?.msPlayPosition ?? 0;
		public double GetPlayPosition() => GetTimestampMs() / 1000.0; // older API

		public double GetSpeed() => _video?.dbPlaySpeed ?? 1;
		public double GetPlaySpeed() => GetSpeed(); // older API

		// the audio track, when the video was opened with one
		public bool HasAudio => _video?.HasAudio ?? false;
		public double GetVolumePercent() => _video?.Audio?.GetGainPercent() ?? 100;

		// End-of-video signal that does not depend on the play-position timer.
		public bool IsFinished() {
			return _video?.IsFinishedPlaying ?? false;
		}
		#endregion
		#region Sets
		public void SetTimestampMs(double ms) {
			if (_video != null) _video.Seek((long)ms); else _pendingSeek = ms;
		}
		public void SetPlayPosition(double position) => SetTimestampMs(position * 1000.0); // older API

		public void SetSpeed(double speed) {
			if (_video != null) _video.dbPlaySpeed = speed; else _pendingSpeed = speed;
		}
		public void SetPlaySpeed(double playSpeed) => SetSpeed(playSpeed); // older API

		public void SetVolumePercent(double vol) {
			if (_video != null) _video.Audio?.SetGain((int)Math.Clamp(vol, 0, 100)); else _pendingVolume = vol;
		}

		#endregion

		#region Dispose
		private bool _disposedValue;
		protected virtual void Dispose(bool disposing) {
			if (!_disposedValue) {
				OpenTaiko.tDisposeSafely(ref _video);
				OpenTaiko.tDisposeSafely(ref _tmpTex);
				_disposeList?.Remove(this);
				_disposedValue = true;
			}
		}
		public void Dispose() {
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
	public class LuaVideoFunc {
		private HashSet<LuaVideo> Videos;
		private string DirPath;

		public LuaVideoFunc(HashSet<LuaVideo> videos, string dirPath) {
			Videos = videos;
			DirPath = dirPath;
		}

		internal LuaVideo Create(string path, bool autoDispose) => Create(path, autoDispose, withAudio: false);

		// withAudio: the file's own audio track plays with it (music volume group) and clocks the picture
		internal LuaVideo Create(string path, bool autoDispose, bool withAudio)
			=> Open($@"{DirPath}{Path.DirectorySeparatorChar}{path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)}", path, autoDispose, withAudio);

		internal LuaVideo CreateFromAbsolutePath(string path, bool autoDispose, bool withAudio)
			=> Open(path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar), path, autoDispose, withAudio);

		private LuaVideo Open(string full_path, string path, bool autoDispose, bool withAudio) {
			LuaVideo luavid = new();
			if (File.Exists(full_path)) {
				Videos.Add(luavid);
				if (autoDispose)
					luavid._disposeList = this.Videos;
				// Open the decoder OFF the render thread (avformat + InitRead are GL-free — the GL upload happens
				// later in GetNowFrame, which is render-thread-safe), then attach on the render thread when ready.
				// Non-blocking: the video appears + plays once loaded (Start() is buffered until then). During a
				// load phase the bar waits for it (NotePending/NoteDone). Works from onStart OR activate.
				bool inPhase = CAsyncLoad.Active;
				if (inPhase) CAsyncLoad.NotePending();
				System.Diagnostics.Trace.TraceInformation($"[vopen] queue {path} inPhase={inPhase}"); // DEBUG probe
				Task.Run(() => {
					try {
						var vid = new CVideoDecoder(full_path, withAudio
							? (rate, channels, seconds, proc) => OpenTaiko.SoundManager.tCreateUserSound(rate, channels, seconds, proc, ESoundGroup.SongPlayback)
							: null);
						vid.InitRead();
						Game.AsyncActions.Enqueue(() => {
							try { luavid.SetVideo(vid); }
							finally { if (inPhase) CAsyncLoad.NoteDone(); }
						});
					} catch (Exception e) {
						LogNotification.PopWarning($"Lua Video failed to load: {e}");
						if (inPhase) CAsyncLoad.NoteDone();
					}
				});
			}
			else if (Path.Exists(full_path)) {
				LogNotification.PopWarning($"Lua Video failed to load because '{full_path}' is not a file.");
			}
			else {
				LogNotification.PopWarning($"Lua Video failed to load because the file located at '{full_path}' does not exist.");
			}
			return luavid;
		}

		public LuaVideo CreateVideo(string path) => Create(path, autoDispose: true);
		public LuaVideo CreateVideo(string path, bool withAudio) => Create(path, autoDispose: true, withAudio: withAudio);
		public LuaVideo CreateVideoFromAbsolutePath(string path) => CreateFromAbsolutePath(path, autoDispose: true, withAudio: false);
		public LuaVideo CreateVideoFromAbsolutePath(string path, bool withAudio) => CreateFromAbsolutePath(path, autoDispose: true, withAudio: withAudio);
	}
}
