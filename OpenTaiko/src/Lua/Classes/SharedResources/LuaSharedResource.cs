using FDK;
using NLua;

namespace OpenTaiko {
	public class LuaSharedResource<T> where T : class, IDisposable, new() {
		private readonly object _lock = new();
		private T _resource = new();
		private T? _pending;                 // a ReloadWhenReady resource not shown yet
		private volatile int _version = 0;

		// under _lock: a newer load, Clear or Adopt drops the resource still waiting to show (its decode is skipped)
		private T? tTakePending() {
			var p = _pending;
			_pending = null;
			return p;
		}
		private static void tDispose(T? r) {
			if (r == null) return;
			try { r.Dispose(); } catch { }
		}

		// the slot empties at once (so a Reload right after it stands); only the old resource's disposal waits
		public void Clear() {
			T old;
			T? pending;
			lock (_lock) {
				_version++;
				old = _resource;
				_resource = new T();
				pending = tTakePending();
			}
			tDispose(pending);
			Game.AsyncActions.Enqueue(() => tDispose(old));
		}

		// Swap in a resource built by the caller once whenReady says it can show (the old one stays until then);
		// a later Reload, Clear or Adopt wins. whenReady runs its action later, on the render thread.
		public void ReloadWhenReady(T resource, Action<Action> whenReady, Action<T>? onCreate) {
			int capturedVersion;
			T? stale;
			lock (_lock) {
				capturedVersion = ++_version;
				stale = tTakePending();
				_pending = resource;
			}
			if (!ReferenceEquals(stale, resource)) tDispose(stale);
			whenReady(() => {
				T? old = null;
				bool current;
				lock (_lock) {
					if (ReferenceEquals(_pending, resource)) _pending = null;
					current = _version == capturedVersion;
					if (current) {
						old = _resource;
						_resource = resource;
						_version++;
					}
				}
				if (!current) {
					tDispose(resource);
					return;
				}
				if (!ReferenceEquals(old, resource)) tDispose(old);
				onCreate?.Invoke(resource);
			});
		}

		// Sounds: the factory runs here (render thread) and queues the sound's own build on Game.AsyncActions; the
		// swap is queued after it (the queue is FIFO), so it happens once the sound is built. During a load phase it is
		// counted so the bar waits for it; the version counter discards stale loads.
		public void Reload(string path, Func<string, T> factory, Action<T>? onCreate) {
			int capturedVersion;
			lock (_lock) {
				capturedVersion = ++_version;
			}

			bool track = CAsyncLoad.ShouldDefer;
			if (track) CAsyncLoad.NotePending();

			T? newResource = null;
			try {
				newResource = factory(path);
			} catch (Exception e) {
				System.Diagnostics.Trace.TraceWarning("[SharedResource] factory failed: " + e.Message);
			}

			Game.AsyncActions.Enqueue(() => {
				try {
					if (newResource == null) return;
					T old;
					lock (_lock) {
						if (_version != capturedVersion) {
							old = newResource;          // stale: drop the new one instead
							newResource = null;
						} else {
							old = _resource;
							_resource = newResource;
							_version++;
						}
					}
					tDispose(old);
					if (newResource != null) onCreate?.Invoke(newResource);
				} finally {
					if (track) CAsyncLoad.NoteDone();
				}
			});
		}

		public T Get() => _resource;

		/// <summary>Replace the held resource with an already-built one (e.g. a texture captured from a
		/// rendered scene rather than loaded from a path). Disposes the previous resource and bumps the
		/// version so any in-flight async load is discarded.</summary>
		public void Adopt(T resource) {
			T? old, pending;
			lock (_lock) {
				old = _resource;
				_resource = resource;
				_version++;
				pending = tTakePending();
			}
			if (!ReferenceEquals(pending, resource)) tDispose(pending);
			if (old != null && !ReferenceEquals(old, resource)) tDispose(old);
		}
	}

	public class LuaSharedResourceFunc {
		private Dictionary<string, LuaSharedResource<LuaTexture>> SharedTextures;
		private Dictionary<string, LuaSharedResource<LuaSound>> SharedSounds;
		private Dictionary<string, string> SharedStrings;
		private Dictionary<string, Lua3DScene> SharedScenes;
		private LuaTextureFunc _luaTextureFunc;
		private LuaSoundFunc _luaSoundFunc;
		private string DirPath;

		public LuaSharedResourceFunc(Dictionary<string, LuaSharedResource<LuaTexture>> st, Dictionary<string, LuaSharedResource<LuaSound>> ss, Dictionary<string, string> strs, Dictionary<string, Lua3DScene> scenes, LuaTextureFunc ltf, LuaSoundFunc lsf, string dirPath) {
			SharedTextures = st;
			SharedSounds = ss;
			SharedStrings = strs;
			SharedScenes = scenes;
			_luaTextureFunc = ltf;
			_luaSoundFunc = lsf;
			DirPath = dirPath;
		}

		public void SetSharedString(string key, string value) {
			SharedStrings[key] = value;
		}

		public string GetSharedString(string key) {
			return SharedStrings.TryGetValue(key, out var val) ? val : "";
		}

		// the scene is still freed with the module that made it
		public void SetSharedScene(string key, Lua3DScene? scene) {
			if (scene == null) SharedScenes.Remove(key);
			else SharedScenes[key] = scene;
		}

		public Lua3DScene? GetSharedScene(string key) {
			return SharedScenes.TryGetValue(key, out var scene) && !scene.IsDisposed ? scene : null;
		}

		public void ClearSharedTexture(string key) {
			if (SharedTextures.ContainsKey(key)) SharedTextures[key].Clear();
		}

		public void ClearSharedSound(string key) {
			if (SharedSounds.ContainsKey(key)) SharedSounds[key].Clear();
		}

		public LuaTexture GetSharedTexture(string key) {
			if (SharedTextures.ContainsKey(key)) return SharedTextures[key].Get();
			return new LuaTexture();
		}

		public LuaSound GetSharedSound(string key) {
			if (SharedSounds.ContainsKey(key)) return SharedSounds[key].Get();
			return new LuaSound();
		}

		// The texture is made here, on the render thread, like TEXTURE:CreateTexture: its size is known at once and its
		// pixels decode in the background. It replaces the key's texture (and onCreate runs) once they are uploaded.
		internal void SetSharedTextureGeneric(string key, string path, Action<LuaTexture>? onCreate, Func<string, LuaTexture> factory) {
			if (!SharedTextures.TryGetValue(key, out var shared)) {
				shared = new LuaSharedResource<LuaTexture>();
				SharedTextures[key] = shared;
			}
			bool prev = CTexture.AsyncLoad;
			CTexture.AsyncLoad = true;
			LuaTexture tex;
			try { tex = factory(path); }
			catch (Exception e) {
				System.Diagnostics.Trace.TraceWarning("[SharedResource] texture load failed: " + e.Message);
				tex = new LuaTexture();
			}
			finally { CTexture.AsyncLoad = prev; }
			shared.ReloadWhenReady(tex, tex.WhenReady, onCreate);
		}

		public void SetSharedTexture(string key, string path, LuaFunction? onCreate = null)
			=> SetSharedTextureGeneric(key, path, onCreate.AsAction<LuaTexture>(), (path) => _luaTextureFunc.CreateTexture(path, autoDispose: false));
		public void SetSharedTextureUsingAbsolutePath(string key, string path, LuaFunction? onCreate = null)
			=> SetSharedTextureGeneric(key, path, onCreate.AsAction<LuaTexture>(), (path) => _luaTextureFunc.CreateTextureFromAbsolutePath(path, autoDispose: false));

		// Options-table variants ({ maxSize = N } clamps the decoded long side — see LuaTextureFunc.tParseMaxSize).
		public void SetSharedTexture(string key, string path, NLua.LuaTable options, LuaFunction? onCreate = null) {
			int maxDim = LuaTextureFunc.tParseMaxSize(options);
			SetSharedTextureGeneric(key, path, onCreate.AsAction<LuaTexture>(), (path) => _luaTextureFunc.CreateTexture(path, autoDispose: false, maxDim));
		}
		public void SetSharedTextureUsingAbsolutePath(string key, string path, NLua.LuaTable options, LuaFunction? onCreate = null) {
			int maxDim = LuaTextureFunc.tParseMaxSize(options);
			SetSharedTextureGeneric(key, path, onCreate.AsAction<LuaTexture>(), (path) => _luaTextureFunc.CreateTextureFromAbsolutePath(path, autoDispose: false, maxDim));
		}

		internal void SetSharedSoundGeneric(string key, string path, Action<LuaSound>? onCreate, Func<string, LuaSound> factory) {
			LuaSharedResource<LuaSound> _sharedSound;

			if (SharedSounds.ContainsKey(key)) _sharedSound = SharedSounds[key];
			else _sharedSound = new LuaSharedResource<LuaSound>();

			_sharedSound.Reload(path, factory, onCreate);
			SharedSounds[key] = _sharedSound;
		}

		public void SetSharedSFX(string key, string path, LuaFunction? onCreate = null)
			=> SetSharedSoundGeneric(key, path, onCreate.AsAction<LuaSound>(), (path) => _luaSoundFunc.CreateSound(path, ESoundGroup.SoundEffect, autoDispose: false));
		public void SetSharedBGM(string key, string path, LuaFunction? onCreate = null)
			=> SetSharedSoundGeneric(key, path, onCreate.AsAction<LuaSound>(), (path) => _luaSoundFunc.CreateSound(path, ESoundGroup.SongPlayback, autoDispose: false));
		public void SetSharedVoice(string key, string path, LuaFunction? onCreate = null)
			=> SetSharedSoundGeneric(key, path, onCreate.AsAction<LuaSound>(), (path) => _luaSoundFunc.CreateSound(path, ESoundGroup.Voice, autoDispose: false));
		public void SetSharedPreview(string key, string path, LuaFunction? onCreate = null)
			=> SetSharedSoundGeneric(key, path, onCreate.AsAction<LuaSound>(), (path) => _luaSoundFunc.CreateSound(path, ESoundGroup.SongPreview, autoDispose: false));
		public void SetSharedSFXUsingAbsolutePath(string key, string path, LuaFunction? onCreate = null)
			=> SetSharedSoundGeneric(key, path, onCreate.AsAction<LuaSound>(), (path) => _luaSoundFunc.CreateSoundFromAbsolutePath(path, ESoundGroup.SoundEffect, autoDispose: false));
		public void SetSharedBGMUsingAbsolutePath(string key, string path, LuaFunction? onCreate = null)
			=> SetSharedSoundGeneric(key, path, onCreate.AsAction<LuaSound>(), (path) => _luaSoundFunc.CreateSoundFromAbsolutePath(path, ESoundGroup.SongPlayback, autoDispose: false));
		public void SetSharedVoiceUsingAbsolutePath(string key, string path, LuaFunction? onCreate = null)
			=> SetSharedSoundGeneric(key, path, onCreate.AsAction<LuaSound>(), (path) => _luaSoundFunc.CreateSoundFromAbsolutePath(path, ESoundGroup.Voice, autoDispose: false));
		public void SetSharedPreviewUsingAbsolutePath(string key, string path, LuaFunction? onCreate = null)
			=> SetSharedSoundGeneric(key, path, onCreate.AsAction<LuaSound>(), (path) => _luaSoundFunc.CreateSoundFromAbsolutePath(path, ESoundGroup.SongPreview, autoDispose: false));

	}
}
