using FDK;

namespace OpenTaiko {
	public class LuaSound : IDisposable {
		private CSkin.CSystemSound? _sound;
		internal HashSet<LuaSound>? _disposeList = null;

		public string Path { get; private set; } = "";
		public LuaSound() {
			_sound = null;
		}
		public LuaSound(string path, ESoundGroup group) {
			Path = path;
			_sound = new(path, false, false, false, group);
			_sound.tLoading();
		}

		#region Sound
		public void Play() {
			_sound?.tPlay();
		}
		public void Stop() {
			_sound?.tStop();
		}
		#endregion
		#region Gets
		public bool IsPlaying => _sound != null ? _sound.bIsPlaying : false;
		public bool GetLoop() {
			return _sound != null ? _sound.bLoop : false;
		}
		public int GetPan() {
			return _sound != null ? _sound.nPosition_CurrentlyPlayingSound : 0;
		}
		#endregion
		#region Sets
		public void SetLoop(bool loop) {
			if (_sound != null) _sound.bLoop = loop;
		}
		public void SetPan(int panning) {
			_sound?.SetPanning(panning);
		}

		public void SetTimestamp(int ms) {
			_sound?.SetTimestamp(ms);
		}

		public void SetVolume(int vol) {
			_sound?.SetVolume(vol);
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
		private LuaSound CreateSound(string path, ESoundGroup group) {
			string full_path = $@"{DirPath}{Path.DirectorySeparatorChar}{path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)}";

			LuaSound sound = new();

			try {
				sound = new(full_path, group);
				Sounds.Add(sound);
				sound._disposeList = this.Sounds;
			} catch (Exception e) {
				LogNotification.PopError($"Lua Sound failed to load: {e}");
				sound?.Dispose();
				sound = new();
			}
			return sound;
		}

		private LuaSound CreateSoundFromAbsolutePath(string path, ESoundGroup group) {
			string full_path = $@"{path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)}";

			LuaSound sound = new();

			try {
				sound = new(full_path, group);
				Sounds.Add(sound);
				sound._disposeList = this.Sounds;
			} catch (Exception e) {
				LogNotification.PopError($"Lua Sound failed to load: {e}");
				sound?.Dispose();
				sound = new();
			}
			return sound;
		}
	}
}
