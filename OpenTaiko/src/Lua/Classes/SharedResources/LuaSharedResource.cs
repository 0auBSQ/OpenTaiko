namespace OpenTaiko {
	public class LuaSharedResource<T> where T : class, IDisposable, new() {
		private readonly object _lock = new();
		private T _resource = new T();
		private readonly List<T> _pendingResources = new();

		public void Reload(string path, Func<string, object?[], T> factory, params object?[] args) {
			lock (_lock) {
				// Dispose all finished resources that are not the current one
				for (int i = _pendingResources.Count - 1; i >= 0; i--) {
					if (!ReferenceEquals(_pendingResources[i], _resource)) {
						_pendingResources[i].Dispose();
						_pendingResources.RemoveAt(i);
					}
				}
			}

			Task.Run(() => {
				var newResource = factory(path, args);

				lock (_lock) {
					_resource = newResource;
					_pendingResources.Add(newResource);
				}
			});
		}

		public void Reload(string path, Func<string, T> factory) => Reload(path, (p, _) => factory(p));

		public T Get() {
			lock (_lock) {
				return _resource;
			}
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

		public LuaSharedResource<LuaTexture> GetSharedTextureAsSharedResource(string key) {
			if (SharedTextures.ContainsKey(key)) return SharedTextures[key];
			return new LuaSharedResource<LuaTexture>();
		}

		public LuaSharedResource<LuaSound> GetSharedSoundAsSharedResource(string key) {
			if (SharedSounds.ContainsKey(key)) return SharedSounds[key];
			return new LuaSharedResource<LuaSound>();
		}

		public void SetSharedTexture(string key, string path) {
			LuaSharedResource<LuaTexture> _sharedTexture;

			if (SharedTextures.ContainsKey(key)) _sharedTexture = SharedTextures[key];
			else _sharedTexture = new LuaSharedResource<LuaTexture>();

			_sharedTexture.Reload(path, _luaTextureFunc.CreateTexture);
			SharedTextures[key] = _sharedTexture;
		}

		public void SetSharedSFX(string key, string path) {
			LuaSharedResource<LuaSound> _sharedSound;

			if (SharedSounds.ContainsKey(key)) _sharedSound = SharedSounds[key];
			else _sharedSound = new LuaSharedResource<LuaSound>();

			_sharedSound.Reload(path, _luaSoundFunc.CreateSFX);
			SharedSounds[key] = _sharedSound;
		}

		public void SetSharedBGM(string key, string path) {
			LuaSharedResource<LuaSound> _sharedSound;

			if (SharedSounds.ContainsKey(key)) _sharedSound = SharedSounds[key];
			else _sharedSound = new LuaSharedResource<LuaSound>();

			_sharedSound.Reload(path, _luaSoundFunc.CreateBGM);
			SharedSounds[key] = _sharedSound;
		}

		public void SetSharedVoice(string key, string path) {
			LuaSharedResource<LuaSound> _sharedSound;

			if (SharedSounds.ContainsKey(key)) _sharedSound = SharedSounds[key];
			else _sharedSound = new LuaSharedResource<LuaSound>();

			_sharedSound.Reload(path, _luaSoundFunc.CreateVoice);
			SharedSounds[key] = _sharedSound;
		}

		public void SetSharedPreview(string key, string path) {
			LuaSharedResource<LuaSound> _sharedSound;

			if (SharedSounds.ContainsKey(key)) _sharedSound = SharedSounds[key];
			else _sharedSound = new LuaSharedResource<LuaSound>();

			_sharedSound.Reload(path, _luaSoundFunc.CreatePreview);
			SharedSounds[key] = _sharedSound;
		}
	}
}
