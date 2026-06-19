using FDK;

namespace OpenTaiko;

/// <summary>
/// A keyed set of textures of which only a few are ever shown at once (e.g. per-genre or
/// per-difficulty full-screen backgrounds). Instead of loading every texture up front and holding
/// them all in GPU memory, paths are registered cheaply and the actual <see cref="CTexture"/> is
/// created on first access, keeping only the most-recently-used <c>maxResident</c> resident and
/// disposing the rest. Preserves <c>tGetGenreBar</c>'s fallback-to-"0" lookup semantics.
/// </summary>
public class CLazyTextureMap {
	private readonly Dictionary<string, string> _paths = new();
	private readonly Dictionary<string, CTexture> _loaded = new();
	private readonly LinkedList<string> _lru = new();
	private readonly int _maxResident;
	private readonly Func<string, CTexture?> _load;

	/// <param name="maxResident">How many textures may stay loaded at once. Must be at least the
	/// number of distinct keys drawn in a single frame (e.g. current + previous for a cross-fade).</param>
	/// <param name="load">Creates a texture from a registered path. Must run on the GL thread.</param>
	public CLazyTextureMap(int maxResident, Func<string, CTexture?> load) {
		_maxResident = Math.Max(1, maxResident);
		_load = load;
	}

	public void Register(string key, string path) => _paths[key] = path;

	/// <summary>Resolve a key (falling back to "0" like the old dictionary lookup), loading the
	/// texture on demand and evicting the least-recently-used beyond the resident cap.</summary>
	public CTexture? Get(string? key) {
		string? k = (key != null && _paths.ContainsKey(key)) ? key
				  : (_paths.ContainsKey("0") ? "0" : null);
		if (k == null)
			return null;

		if (_loaded.TryGetValue(k, out CTexture? cached) && cached != null) {
			_lru.Remove(k);
			_lru.AddFirst(k);
			return cached;
		}

		CTexture? tex;
		try {
			tex = _load(_paths[k]);
		} catch {
			return null;
		}
		_loaded[k] = tex!;
		_lru.AddFirst(k);

		while (_lru.Count > _maxResident) {
			string evict = _lru.Last!.Value;
			_lru.RemoveLast();
			if (_loaded.TryGetValue(evict, out CTexture? old)) {
				old?.Dispose();
				_loaded.Remove(evict);
			}
		}
		return tex;
	}

	/// <summary>Dispose all resident textures (called on skin teardown — these are not tracked in
	/// TextureLoader.listTexture).</summary>
	public void Dispose() {
		foreach (CTexture? t in _loaded.Values)
			t?.Dispose();
		_loaded.Clear();
		_lru.Clear();
	}
}
