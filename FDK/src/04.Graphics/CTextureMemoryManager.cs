namespace FDK;

/// <summary>
/// Global GPU-texture memory accounting and memory-pressure eviction for <see cref="CTexture"/>.
/// CTexture owns each texture's own instance state (bytes, lazy path, draw tick/generation) and
/// notifies this manager: register on upload, unregister on release/dispose, touch on draw, and
/// query the byte budget for eviction decisions. The manager owns only the cross-texture concerns:
/// the total byte/count tallies, the scene generation, and the registry of evictable textures.
/// </summary>
internal static class CTextureMemoryManager {
	// Tracks the total decoded RGBA8 bytes uploaded to GL across all live textures, so memory-pressure
	// eviction can target a byte budget and so texture memory can be attributed at startup vs. gameplay.
	private static long _totalTextureBytes;
	private static int _liveTextureCount;

	// Scene generation, bumped on every stage change (BeginNewScene). Textures drawn in the current
	// generation are the active working set and are never evicted — so memory pressure never causes
	// unload/reload churn within a scene; only textures left over from previous scenes are released.
	private static int _sceneGeneration;

	// Registry of currently-uploaded deferred textures, so memory-pressure eviction can pick the
	// least-recently-drawn ones. Touched only on the main/GL thread, but locked defensively.
	private static readonly object _lazyLock = new();
	private static readonly List<CTexture> _uploadedLazy = new();

	public static long TotalTextureBytes => System.Threading.Interlocked.Read(ref _totalTextureBytes);
	public static int LiveTextureCount => _liveTextureCount;

	/// <summary>Current scene generation. A texture stamps this onto itself when drawn so the
	/// eviction pass can recognise (and protect) the current scene's working set.</summary>
	public static int SceneGeneration => _sceneGeneration;

	/// <summary>Begin a new scene (call on stage change). Textures drawn before this point become
	/// eligible for memory-pressure eviction; those drawn after it are protected as the working set.</summary>
	public static void BeginNewScene() => _sceneGeneration++;

	/// <summary>Account for <paramref name="bytes"/> newly uploaded to GL by one texture.</summary>
	public static void AddUploadedBytes(long bytes) {
		System.Threading.Interlocked.Add(ref _totalTextureBytes, bytes);
		System.Threading.Interlocked.Increment(ref _liveTextureCount);
	}

	/// <summary>Drop the accounting for <paramref name="bytes"/> a texture no longer has uploaded.</summary>
	public static void RemoveUploadedBytes(long bytes) {
		System.Threading.Interlocked.Add(ref _totalTextureBytes, -bytes);
		System.Threading.Interlocked.Decrement(ref _liveTextureCount);
	}

	/// <summary>Track a deferred texture as evictable once it has uploaded.</summary>
	public static void Register(CTexture texture) {
		lock (_lazyLock)
			_uploadedLazy.Add(texture);
	}

	/// <summary>Stop tracking a texture (released GPU memory, or disposed).</summary>
	public static void Unregister(CTexture texture) {
		lock (_lazyLock)
			_uploadedLazy.Remove(texture);
	}

	/// <summary>
	/// Free the GPU memory of the least-recently-drawn deferred textures until the total uploaded
	/// texture memory is at or below <paramref name="targetBytes"/>. Evicted textures keep their path
	/// and re-upload automatically the next time they are drawn. Intended to be called from the iOS
	/// memory-warning callback (on the GL thread, context current). Returns the bytes freed.
	/// </summary>
	public static long EvictLeastRecentlyDrawnDownTo(long targetBytes) {
		if (System.Threading.Interlocked.Read(ref _totalTextureBytes) <= targetBytes)
			return 0;

		CTexture[] snapshot;
		lock (_lazyLock)
			snapshot = _uploadedLazy.ToArray();
		Array.Sort(snapshot, static (a, b) => a.LastDrawnTick.CompareTo(b.LastDrawnTick)); // oldest first

		long freed = 0;
		foreach (CTexture t in snapshot) {
			if (System.Threading.Interlocked.Read(ref _totalTextureBytes) <= targetBytes)
				break;
			if (t.IsPinned) // never evict preloaded gameplay-critical textures
				continue;
			if (t.LastDrawnGeneration == _sceneGeneration) // never evict the current scene's working set
				continue;
			long bytes = t.UploadedBytes;
			t.ReleaseGpu();
			freed += bytes;
		}
		// If we're still over target, the current scene alone exceeds it — we deliberately do NOT
		// evict the active working set (that would thrash). Memory simply stays high (and may OOM).
		return freed;
	}
}
