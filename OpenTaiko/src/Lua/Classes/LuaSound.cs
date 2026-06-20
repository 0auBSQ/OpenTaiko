using System.Diagnostics;
using FDK;

namespace OpenTaiko {
	public class LuaSound : IDisposable {
		internal CSkin.CSystemSound? _sound;
		internal HashSet<LuaSound>? _disposeList = null;

		// State set on the stub before the deferred sound is built (async-load path) — applied on LoadDeferred,
		// so e.g. SetLoop(true)/Play() called in onStart aren't lost while the BASS stream is still being created.
		private bool? _pendingLoop;
		private int? _pendingVolume;
		private int? _pendingPan;
		private int? _pendingTimestamp;
		private double? _pendingSpeed;
		private bool _pendingPlay;

		public string Path { get; private set; } = "";
		public LuaSound() {
			_sound = null;
		}
		public LuaSound(string path, ESoundGroup group) {
			Path = path;
			_sound = new(path, false, false, false, group);
			_sound.tLoading();
		}

		// Build the underlying sound in-place (async-load path: created empty, filled on the render thread).
		// Until this runs, every method is a harmless no-op.
		internal void LoadDeferred(string path, ESoundGroup group) {
			Path = path;
			var s = new CSkin.CSystemSound(path, false, false, false, group);
			s.tLoading();
			_sound = s;
			// Apply anything set on the stub while it was still loading.
			if (_pendingLoop.HasValue) s.bLoop = _pendingLoop.Value;
			if (_pendingVolume.HasValue) s.SetVolume(_pendingVolume.Value);
			if (_pendingPan.HasValue) s.SetPanning(_pendingPan.Value);
			if (_pendingTimestamp.HasValue) s.SetTimestamp(_pendingTimestamp.Value);
			if (_pendingSpeed.HasValue) s.SetSpeed(_pendingSpeed.Value);
			if (_pendingPlay) s.tPlay();
			_pendingLoop = null; _pendingVolume = null; _pendingPan = null; _pendingTimestamp = null; _pendingSpeed = null; _pendingPlay = false;
		}

		#region Sound
		public void Play() { if (_sound != null) _sound.tPlay(); else _pendingPlay = true; }
		public void Stop() { if (_sound != null) _sound.tStop(); else _pendingPlay = false; }
		#endregion
		#region Gets
		public bool Loaded => _sound != null;
		public bool IsPlaying => _sound?.bIsPlaying ?? false;
		public bool GetLoop() => _sound?.bLoop ?? false;
		public int GetPan() => _sound?.nPosition_CurrentlyPlayingSound ?? 0;
		/// <summary>Returns the total duration of the sound in milliseconds, or 0 if not loaded.</summary>
		public int GetDurationMs() => (int)(_sound?.nLength_CurrentSound ?? 0);
		/// <summary>Returns the current playback position in milliseconds.</summary>
		public int GetTimestampMs() => (int)(_sound?.msTimeStamp_nowSound ?? 0);
		#endregion
		#region Sets
		public void SetLoop(bool loop) {
			if (_sound != null) _sound.bLoop = loop; else _pendingLoop = loop;
		}
		public void SetPan(int panning) {
			if (_sound != null) _sound.SetPanning(panning); else _pendingPan = panning;
		}

		public void SetTimestamp(int ms) {
			if (_sound != null) _sound.SetTimestamp(ms); else _pendingTimestamp = ms;
		}

		public void SetVolume(int vol) {
			if (_sound != null) _sound.SetVolume(vol); else _pendingVolume = vol;
		}

		/// <summary>Sets the playback speed multiplier (1.0 = normal).</summary>
		public void SetSpeed(double speed) {
			if (_sound != null) _sound.SetSpeed(speed); else _pendingSpeed = speed;
		}

		#endregion
		#region Dispose
		private bool _disposedValue;
		protected virtual void Dispose(bool disposing) {
			if (!_disposedValue) {
				if (disposing) {
					Path = string.Empty;
				}
				_sound?.Dispose();
				_sound = null;
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
	public class LuaSoundFunc {
		private HashSet<LuaSound> Sounds;
		private string DirPath;

		public LuaSoundFunc(HashSet<LuaSound> sounds, string dirPath) {
			Sounds = sounds;
			DirPath = dirPath;
		}

		public LuaSound CreateSFX(string path) => CreateSound(path, ESoundGroup.SoundEffect);
		public LuaSound CreateVoice(string path) => CreateSound(path, ESoundGroup.Voice);
		public LuaSound CreateBGM(string path) => CreateSound(path, ESoundGroup.SongPlayback);
		public LuaSound CreatePreview(string path) => CreateSound(path, ESoundGroup.SongPreview);
		public LuaSound CreateSFXFromAbsolutePath(string path) => CreateSoundFromAbsolutePath(path, ESoundGroup.SoundEffect);
		public LuaSound CreateVoiceFromAbsolutePath(string path) => CreateSoundFromAbsolutePath(path, ESoundGroup.Voice);
		public LuaSound CreateBGMFromAbsolutePath(string path) => CreateSoundFromAbsolutePath(path, ESoundGroup.SongPlayback);
		public LuaSound CreatePreviewFromAbsolutePath(string path) => CreateSoundFromAbsolutePath(path, ESoundGroup.SongPreview);
		internal LuaSound CreateSound(string path, ESoundGroup group, bool autoDispose = true)
			=> CreateSoundFromAbsolutePath($@"{DirPath}{Path.DirectorySeparatorChar}{path}", group, autoDispose);

		internal LuaSound CreateSoundFromAbsolutePath(string path, ESoundGroup group, bool autoDispose = true) {
			string full_path = $@"{path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)}";
#if DEBUG
			Trace.TraceInformation($"[ALLOC_SND] {full_path}");
#endif

			// Return an empty stub + create the BASS stream non-blocking on the render thread (BASS is sync-
			// critical, so it is NOT moved off-thread — just spread across frames). Until it's built every method
			// is a harmless no-op and Play/Set* are buffered, so the sound simply starts working when ready.
			LuaSound sound = new();
			Sounds.Add(sound);
			if (autoDispose)
				sound._disposeList = this.Sounds;
			Action build = () => {
				try { sound.LoadDeferred(full_path, group); }
				catch (Exception e) { LogNotification.PopError($"Lua Sound failed to load: {e}"); }
			};
			if (CAsyncLoad.ShouldDefer)
				CAsyncLoad.TrackRenderThread(build);   // in a load phase → also counts toward the loading bar
			else
				Game.AsyncActions.Enqueue(build);      // runtime → just the per-frame finalize budget, non-blocking
			return sound;
		}
	}
}
