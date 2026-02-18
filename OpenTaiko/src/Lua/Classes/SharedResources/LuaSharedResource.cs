using FDK;

namespace OpenTaiko {
	public class LuaSharedResource<T> where T : class, IDisposable, new() {
		private readonly object _lock = new();
		private T _resource = new();
		private volatile int _version = 0;

		public void Clear() {
			Game.AsyncActions.Enqueue(() => {
				_resource.Dispose();
				_resource = new T();
				_version++;
			});
		}

		public void Reload(string path, Func<string, T> factory, Action<T>? onCreate) {
			int capturedVersion;
			lock (_lock) {
				capturedVersion = ++_version;
			}

			Task.Run(() => {
				var newResource = factory(path);

				Game.AsyncActions.Enqueue(() => {
					if (_version != capturedVersion) {
						try { newResource.Dispose(); } catch { }
						return;
					}

					_resource.Dispose();
					_resource = newResource;
					_version++;
					onCreate?.Invoke(newResource);
				});
			});
		}

		public T Get() => _resource;
	}

	public class LuaSharedResourceFunc {
		private Dictionary<string, LuaSharedResource<LuaTexture>> SharedTextures;
		private Dictionary<string, LuaSharedResource<LuaSound>> SharedSounds;
		private LuaTextureFunc _luaTextureFunc;
		private LuaSoundFunc _luaSoundFunc;
		private string DirPath;

		public LuaSharedResourceFunc(Dictionary<string, LuaSharedResource<LuaTexture>> st, Dictionary<string, LuaSharedResource<LuaSound>> ss, LuaTextureFunc ltf, LuaSoundFunc lsf, string dirPath) {
			SharedTextures = st;
			SharedSounds = ss;
			_luaTextureFunc = ltf;
			_luaSoundFunc = lsf;
			DirPath = dirPath;
		}

		public void ClearSharedTexture(string key) {
			if (SharedTextures.ContainsKey(key)) SharedTextures[key].Clear();
		}

		public void ClearSharedSound(string key) {
			if (SharedTextures.ContainsKey(key)) SharedSounds[key].Clear();
		}

		public LuaTexture GetSharedTexture(string key) {
			if (SharedTextures.ContainsKey(key)) return SharedTextures[key].Get();
			return new LuaTexture();
		}

		public LuaSound GetSharedSound(string key) {
			if (SharedSounds.ContainsKey(key)) return SharedSounds[key].Get();
			return new LuaSound();
		}

		internal void SetSharedTextureGeneric(string key, string path, Action<LuaTexture>? onCreate, Func<string, LuaTexture> factory) {
			LuaSharedResource<LuaTexture> _sharedTexture;

			if (SharedTextures.ContainsKey(key)) _sharedTexture = SharedTextures[key];
			else _sharedTexture = new LuaSharedResource<LuaTexture>();

			_sharedTexture.Reload(path, factory, onCreate);
			SharedTextures[key] = _sharedTexture;
		}

		public void SetSharedTexture(string key, string path, Action<LuaTexture>? onCreate = null)
			=> SetSharedTextureGeneric(key, path, onCreate, (path) => _luaTextureFunc.CreateTexture(path, autoDispose: false));
		public void SetSharedTextureUsingAbsolutePath(string key, string path, Action<LuaTexture>? onCreate = null)
			=> SetSharedTextureGeneric(key, path, onCreate, (path) => _luaTextureFunc.CreateTextureFromAbsolutePath(path, autoDispose: false));

		internal void SetSharedSoundGeneric(string key, string path, Action<LuaSound>? onCreate, Func<string, LuaSound> factory) {
			LuaSharedResource<LuaSound> _sharedSound;

			if (SharedSounds.ContainsKey(key)) _sharedSound = SharedSounds[key];
			else _sharedSound = new LuaSharedResource<LuaSound>();

			_sharedSound.Reload(path, factory, onCreate);
			SharedSounds[key] = _sharedSound;
		}

		public void SetSharedSFX(string key, string path, Action<LuaSound>? onCreate = null)
			=> SetSharedSoundGeneric(key, path, onCreate, (path) => _luaSoundFunc.CreateSound(path, ESoundGroup.SoundEffect, autoDispose: false));
		public void SetSharedBGM(string key, string path, Action<LuaSound>? onCreate = null)
			=> SetSharedSoundGeneric(key, path, onCreate, (path) => _luaSoundFunc.CreateSound(path, ESoundGroup.SongPlayback, autoDispose: false));
		public void SetSharedVoice(string key, string path, Action<LuaSound>? onCreate = null)
			=> SetSharedSoundGeneric(key, path, onCreate, (path) => _luaSoundFunc.CreateSound(path, ESoundGroup.Voice, autoDispose: false));
		public void SetSharedPreview(string key, string path, Action<LuaSound>? onCreate = null)
			=> SetSharedSoundGeneric(key, path, onCreate, (path) => _luaSoundFunc.CreateSound(path, ESoundGroup.SongPreview, autoDispose: false));
		public void SetSharedSFXUsingAbsolutePath(string key, string path, Action<LuaSound>? onCreate = null)
			=> SetSharedSoundGeneric(key, path, onCreate, (path) => _luaSoundFunc.CreateSoundFromAbsolutePath(path, ESoundGroup.SoundEffect, autoDispose: false));
		public void SetSharedBGMUsingAbsolutePath(string key, string path, Action<LuaSound>? onCreate = null)
			=> SetSharedSoundGeneric(key, path, onCreate, (path) => _luaSoundFunc.CreateSoundFromAbsolutePath(path, ESoundGroup.SongPlayback, autoDispose: false));
		public void SetSharedVoiceUsingAbsolutePath(string key, string path, Action<LuaSound>? onCreate = null)
			=> SetSharedSoundGeneric(key, path, onCreate, (path) => _luaSoundFunc.CreateSoundFromAbsolutePath(path, ESoundGroup.Voice, autoDispose: false));
		public void SetSharedPreviewUsingAbsolutePath(string key, string path, Action<LuaSound>? onCreate = null)
			=> SetSharedSoundGeneric(key, path, onCreate, (path) => _luaSoundFunc.CreateSoundFromAbsolutePath(path, ESoundGroup.SongPreview, autoDispose: false));
	}
}
