using FDK;

namespace OpenTaiko {
	public class LuaSharedResource<T> where T : class, IDisposable, new() {
		private readonly object _lock = new();
		private volatile T _resource = new();
		private readonly List<T> _pendingResources = new();
		private int _currentReloadId = 0;
		private int _lastCompletedReloadId = 0;

		public void Reload(string path, Func<string, object?[], T> factory, Action<T>? onCreate, params object?[] args) {
			int thisReloadId;

			lock (_lock) {
				for (int i = _pendingResources.Count - 1; i >= 0; i--) {
					if (!ReferenceEquals(_pendingResources[i], _resource)) {
						_pendingResources[i].Dispose();
						_pendingResources.RemoveAt(i);
					}
				}

				thisReloadId = ++_currentReloadId;
			}

			Task.Run(() => {
				var newResource = factory(path, args);

				lock (_lock) {
					if (thisReloadId > _lastCompletedReloadId) {
						_resource = newResource;
						_lastCompletedReloadId = thisReloadId;
						if (onCreate != null) {
							Action callbackOnMainThread = () => {
								onCreate.Invoke(_resource);
							};
							Game.AsyncActions.Enqueue(callbackOnMainThread);
						}
					}
				}

				_pendingResources.Add(newResource);
			});
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

		public LuaTexture GetSharedTexture(string key) {
			if (SharedTextures.ContainsKey(key)) return SharedTextures[key].Get();
			return new LuaTexture();
		}

		public LuaSound GetSharedSound(string key) {
			if (SharedSounds.ContainsKey(key)) return SharedSounds[key].Get();
			return new LuaSound();
		}

		public void SetSharedTexture(string key, string path, Action<LuaTexture>? onCreate = null) {
			LuaSharedResource<LuaTexture> _sharedTexture;

			if (SharedTextures.ContainsKey(key)) _sharedTexture = SharedTextures[key];
			else _sharedTexture = new LuaSharedResource<LuaTexture>();

			_sharedTexture.Reload(path, _luaTextureFunc.CreateTexture, onCreate);
			SharedTextures[key] = _sharedTexture;
		}

		public void SetSharedTextureUsingAbsolutePath(string key, string path, Action<LuaTexture>? onCreate = null) {
			LuaSharedResource<LuaTexture> _sharedTexture;

			if (SharedTextures.ContainsKey(key)) _sharedTexture = SharedTextures[key];
			else _sharedTexture = new LuaSharedResource<LuaTexture>();

			_sharedTexture.Reload(path, _luaTextureFunc.CreateTextureFromAbsolutePath, onCreate);
			SharedTextures[key] = _sharedTexture;
		}

		public void SetSharedSFX(string key, string path, Action<LuaSound>? onCreate = null) {
			LuaSharedResource<LuaSound> _sharedSound;

			if (SharedSounds.ContainsKey(key)) _sharedSound = SharedSounds[key];
			else _sharedSound = new LuaSharedResource<LuaSound>();

			_sharedSound.Reload(path, _luaSoundFunc.CreateSFX, onCreate);
			SharedSounds[key] = _sharedSound;
		}

		public void SetSharedBGM(string key, string path, Action<LuaSound>? onCreate = null) {
			LuaSharedResource<LuaSound> _sharedSound;

			if (SharedSounds.ContainsKey(key)) _sharedSound = SharedSounds[key];
			else _sharedSound = new LuaSharedResource<LuaSound>();

			_sharedSound.Reload(path, _luaSoundFunc.CreateBGM, onCreate);
			SharedSounds[key] = _sharedSound;
		}

		public void SetSharedVoice(string key, string path, Action<LuaSound>? onCreate = null) {
			LuaSharedResource<LuaSound> _sharedSound;

			if (SharedSounds.ContainsKey(key)) _sharedSound = SharedSounds[key];
			else _sharedSound = new LuaSharedResource<LuaSound>();

			_sharedSound.Reload(path, _luaSoundFunc.CreateVoice, onCreate);
			SharedSounds[key] = _sharedSound;
		}

		public void SetSharedPreview(string key, string path, Action<LuaSound>? onCreate = null) {
			LuaSharedResource<LuaSound> _sharedSound;

			if (SharedSounds.ContainsKey(key)) _sharedSound = SharedSounds[key];
			else _sharedSound = new LuaSharedResource<LuaSound>();

			_sharedSound.Reload(path, _luaSoundFunc.CreatePreview, onCreate);
			SharedSounds[key] = _sharedSound;
		}

		public void SetSharedSFXUsingAbsolutePath(string key, string path, Action<LuaSound>? onCreate = null) {
			LuaSharedResource<LuaSound> _sharedSound;

			if (SharedSounds.ContainsKey(key)) _sharedSound = SharedSounds[key];
			else _sharedSound = new LuaSharedResource<LuaSound>();

			_sharedSound.Reload(path, _luaSoundFunc.CreateSFXFromAbsolutePath, onCreate);
			SharedSounds[key] = _sharedSound;
		}

		public void SetSharedBGMUsingAbsolutePath(string key, string path, Action<LuaSound>? onCreate = null) {
			LuaSharedResource<LuaSound> _sharedSound;

			if (SharedSounds.ContainsKey(key)) _sharedSound = SharedSounds[key];
			else _sharedSound = new LuaSharedResource<LuaSound>();

			_sharedSound.Reload(path, _luaSoundFunc.CreateBGMFromAbsolutePath, onCreate);
			SharedSounds[key] = _sharedSound;
		}

		public void SetSharedVoiceUsingAbsolutePath(string key, string path, Action<LuaSound>? onCreate = null) {
			LuaSharedResource<LuaSound> _sharedSound;

			if (SharedSounds.ContainsKey(key)) _sharedSound = SharedSounds[key];
			else _sharedSound = new LuaSharedResource<LuaSound>();

			_sharedSound.Reload(path, _luaSoundFunc.CreateVoiceFromAbsolutePath, onCreate);
			SharedSounds[key] = _sharedSound;
		}

		public void SetSharedPreviewUsingAbsolutePath(string key, string path, Action<LuaSound>? onCreate = null) {
			LuaSharedResource<LuaSound> _sharedSound;

			if (SharedSounds.ContainsKey(key)) _sharedSound = SharedSounds[key];
			else _sharedSound = new LuaSharedResource<LuaSound>();

			_sharedSound.Reload(path, _luaSoundFunc.CreatePreviewFromAbsolutePath, onCreate);
			SharedSounds[key] = _sharedSound;
		}
	}
}
