using SkiaSharp;
using System.Collections.Concurrent;

namespace FDK;

// ── Asynchronous texture loading ──────────────────────────────────────────────────────────────────────
// Loading a texture = PNG decode (slow, CPU) + GL upload (cheap, render-thread-only). To never block the render
// thread, MakeTexture(path) on the render thread QUEUES the work instead of doing it inline: a small background
// pool decodes (SKBitmap.Decode, thread-safe) and enqueues the GL upload onto Game.AsyncActions — the ONE
// render-thread "finalize" queue, drained each frame within a time budget (Game.AsyncBudgetMs; raised behind a
// loading screen). The texture stays blank (Pointer==0 ⇒ t2DDraw no-ops) until uploaded, so consumers naturally
// "show nothing until it's ready". Decode off-thread; GL upload on the render thread. Hooking MakeTexture itself
// captures EXACTLY what is loaded (random picks + Lua-driven loads) with zero path-matching.
//
// Three flags decide whether a MakeTexture(path) on the render thread queues (vs loads inline):
//   • AsyncLoad     — a runtime async load (Lua TEXTURE:CreateTexture): queue, non-blocking, not counted.
//   • StreamingLoad — a load PHASE (boot / song-load game screen): queue AND count the item for the loading bar.
//   • SyncForce     — overrides both: load inline now (CreateTextureSync — pixels needed immediately, e.g. a
//                     sprite registered from the texture via GPU readback).
public partial class CTexture {
	public static volatile bool StreamingLoad;   // a load phase is active → queued items count toward the bar
	// Per-thread: set around a load that should QUEUE (Lua TEXTURE:CreateTexture on the render thread, or the
	// off-thread chart parse's chart-object textures). ThreadStatic so the off-thread set is isolated from the
	// render thread (and vice-versa) — each thread sets+reads its own copy in MakeTexture(path).
	[ThreadStatic] public static bool AsyncLoad;
	public static volatile bool SyncForce;       // force inline decode + upload (CreateTextureSync)

	private struct StreamItem { public CTexture tex; public string path; public bool black; public bool inPhase; public int gen; }
	private static readonly ConcurrentQueue<StreamItem> _pending = new();
	private static int _phaseTotal, _phaseDone, _phaseGen;
	private static int _decodeWorkers;
	private static long _readyBytes;                                       // decoded-but-not-uploaded bytes (backpressure)
	private static readonly int _maxWorkers = Math.Max(2, Environment.ProcessorCount / 2);

	// ── Cached file existence ─────────────────────────────────────────────────────────────────────
	// A stage activation checks existence for hundreds of texture paths. On systems where the game folder is
	// scanned by antivirus, each per-file metadata hit is slow; enumerating a directory ONCE (metadata only,
	// not file contents → not AV-scanned) and caching the name set turns N per-file checks into ~1 per dir.
	private static readonly ConcurrentDictionary<string, System.Collections.Generic.HashSet<string>> _dirListCache
		= new(StringComparer.OrdinalIgnoreCase);

	/// <summary>File.Exists via a cached per-directory listing. Falls back to File.Exists on any error.</summary>
	public static bool FileExistsCached(string path) {
		if (string.IsNullOrEmpty(path)) return false;
		try {
			string dir = Path.GetDirectoryName(path);
			if (string.IsNullOrEmpty(dir)) return File.Exists(path);
			var names = _dirListCache.GetOrAdd(dir, d => {
				var set = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
				try {
					if (Directory.Exists(d))
						foreach (var f in Directory.EnumerateFiles(d))
							set.Add(Path.GetFileName(f));
				} catch { /* leave empty → treated as missing */ }
				return set;
			});
			return names.Contains(Path.GetFileName(path));
		} catch {
			return File.Exists(path);
		}
	}

	/// <summary>Drop the cached directory listings (call after a skin reload / when on-disk assets change).</summary>
	public static void ClearFileListCache() => _dirListCache.Clear();

	/// <summary>Loading-bar progress 0..1 for the current load phase (1 when nothing is queued).</summary>
	public static float StreamFraction => Volatile.Read(ref _phaseTotal) <= 0 ? 1f
		: Math.Min(1f, Volatile.Read(ref _phaseDone) / (float)Volatile.Read(ref _phaseTotal));
	/// <summary>True once every texture queued during the current phase has been uploaded (or skipped).</summary>
	public static bool StreamComplete => Volatile.Read(ref _phaseDone) >= Volatile.Read(ref _phaseTotal);

	/// <summary>Queue a path-load for background decode + render-thread upload. Returns false only if the file is
	/// missing (→ the caller's inline path handles it / throws). DELIBERATELY does NOT open the file to read the
	/// size: opening triggers per-file AV scanning (~15-20ms), so reading hundreds of headers during an Activate
	/// is itself a multi-second freeze. Pointer + size stay 0 until the upload fills them; t2DDraw no-ops
	/// meanwhile, and a load phase isn't considered done (StreamComplete) until every item uploads. Render thread.</summary>
	private bool tQueueAsyncTexture(string strFileName, bool bBlackTransparent) {
		if (!FileExistsCached(strFileName))
			return false;
		bool inPhase = StreamingLoad;
		if (inPhase) Interlocked.Increment(ref _phaseTotal);
		_pending.Enqueue(new StreamItem { tex = this, path = strFileName, black = bBlackTransparent, inPhase = inPhase, gen = Volatile.Read(ref _phaseGen) });
		EnsureDecodeWorkers();
		return true;
	}

	// Ensure up to _maxWorkers decode tasks are draining _pending. Decode-only on a capped pool → leaves cores
	// for the render thread; tasks exit when the queue empties and restart on new work.
	private static void EnsureDecodeWorkers() {
		while (true) {
			int cur = Volatile.Read(ref _decodeWorkers);
			if (cur >= _maxWorkers) return;
			if (Interlocked.CompareExchange(ref _decodeWorkers, cur + 1, cur) == cur) break;
		}
		System.Threading.Tasks.Task.Run(DecodeLoop);
	}

	private static void DecodeLoop() {
		const long readyByteCap = 96L * 1024 * 1024;
		try {
			while (_pending.TryDequeue(out var item)) {
				// Backpressure: don't decode faster than the render thread uploads (bounded memory).
				while (Volatile.Read(ref _readyBytes) >= readyByteCap) System.Threading.Thread.Sleep(1);
				if (item.tex.bDisposeCompleteDone) { CompleteItem(item); continue; }   // disposed before decode → drop
				SKBitmap? bmp = tDecodeForUpload(item.path);
				if (bmp != null) Interlocked.Add(ref _readyBytes, bmp.ByteCount);
				var captured = item;
				Game.AsyncActions.Enqueue(() => UploadOne(captured, bmp));
			}
		} finally {
			Interlocked.Decrement(ref _decodeWorkers);
			if (!_pending.IsEmpty) EnsureDecodeWorkers();   // an item raced in just as this worker exited
		}
	}

	// Render-thread GL upload (drained from Game.AsyncActions). Skips textures disposed since queueing (e.g. an
	// ESC-cancelled song load tore down the half-loaded game screen).
	private static void UploadOne(StreamItem item, SKBitmap? bmp) {
		try {
			if (bmp != null && !item.tex.bDisposeCompleteDone)
				item.tex.MakeTexture(bmp, item.black);
		} catch { /* leave the stub blank on upload failure */ }
		finally {
			if (bmp != null) { Interlocked.Add(ref _readyBytes, -bmp.ByteCount); bmp.Dispose(); }
			CompleteItem(item);
		}
	}

	// Count a phase item toward the bar — but only for the CURRENT generation, so stale items from an ended or
	// cancelled phase can't corrupt a new phase's progress.
	private static void CompleteItem(StreamItem item) {
		if (item.inPhase && item.gen == Volatile.Read(ref _phaseGen))
			Interlocked.Increment(ref _phaseDone);
	}

	/// <summary>Decode an image to BGRA8888 UNPREMUL so the GL upload is zero-copy (GetPixels) and matches the
	/// engine's straight-alpha blending. Falls back to a plain decode. Off-thread (decode worker).</summary>
	private static SKBitmap? tDecodeForUpload(string path) {
		try {
			using var fs = File.OpenRead(path);
			using var codec = SKCodec.Create(fs);
			if (codec != null) {
				var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
				var bmp = new SKBitmap(info);
				var res = codec.GetPixels(info, bmp.GetPixels());
				if (res == SKCodecResult.Success || res == SKCodecResult.IncompleteInput)
					return bmp;
				bmp.Dispose();
			}
		} catch { /* fall through to the plain decode */ }
		try { return SKBitmap.Decode(path); } catch { return null; }
	}

	// ── Load-phase API (CAsyncLoad / CStageSongLoading) ─────────────────────────────────────────────
	/// <summary>Begin a load phase: queued textures count toward the bar. A new generation detaches any still
	/// in-flight items from a previous phase. Render thread, before the streamed activation.</summary>
	public static void BeginStreaming() {
		Interlocked.Increment(ref _phaseGen);
		Interlocked.Exchange(ref _phaseTotal, 0);
		Interlocked.Exchange(ref _phaseDone, 0);
		StreamingLoad = true;
	}

	/// <summary>No-op: decode now starts per item as it is queued (kept for call-site compatibility).</summary>
	public static void StartStreamDecode() { }
	/// <summary>No-op: uploads finalize via Game.AsyncActions now (kept for call-site compatibility).</summary>
	public static void PumpUploads(double budgetMs) { }

	/// <summary>End a load phase normally (all items uploaded).</summary>
	public static void EndStreaming() { StreamingLoad = false; }

	/// <summary>Cancel a load phase (ESC mid-load): stop counting + reset the bar. In-flight uploads to the
	/// torn-down textures skip via the disposed check; the new generation detaches their counting. Call BEFORE
	/// the screen's DeActivate disposes the stub textures.</summary>
	public static void CancelStreaming() {
		StreamingLoad = false;
		Interlocked.Increment(ref _phaseGen);
		Interlocked.Exchange(ref _phaseTotal, 0);
		Interlocked.Exchange(ref _phaseDone, 0);
	}
}
