using FDK;

namespace OpenTaiko {
	public class LuaSharedResource<T> where T : class, IDisposable, new() {
		private readonly object _lock = new();
		private volatile T _resource = new();
		private readonly HashSet<T> _pendingResources = new();
		private Action? _reloadAction;

		public void Clear() {
			lock (_lock) {
				_resource?.Dispose();
				_resource = new T();

				foreach (var res in _pendingResources) {
					try {
						res.Dispose();
					} catch { }
				}
				_pendingResources.Clear();
			}
		}

		public void Reload(string path, Func<string, object?[], T> factory, Action<T>? onCreate, params object?[] args) {
			lock (_lock) {
				if (_pendingResources.TryGetValue(this._resource, out var sharedResource)) {
					sharedResource.Dispose();
					_pendingResources.Remove(this._resource);
				}
			}

			// update action
			Interlocked.Exchange(ref this._reloadAction, () => {
				var newResource = factory(path, args);

				lock (_lock) {
					this._resource = newResource;
					if (onCreate != null) {
						Game.AsyncActions.Enqueue(() => onCreate.Invoke(newResource));
					}
					_pendingResources.Add(newResource);
				}
			});
			// consume action
			Task.Run(() => Interlocked.Exchange(ref this._reloadAction, null)?.Invoke());
		}

		public void Reload(string path, Func<string, T> factory, Action<T>? onCreate) => Reload(path, (p, _) => factory(p), onCreate);

		public T Get() {
			return _resource;
		}
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
