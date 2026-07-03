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

	private class Phase {
		public ConcurrentQueue<StreamItem> Pending = new();
		public int Done = 0;

		public int Total => Pending.Count + Volatile.Read(ref Done);
		public float Fraction {
			get {
				var done = Volatile.Read(ref Done);
				return this.Pending.IsEmpty ? 1f
					: done <= 0 ? 0f
					: Math.Min(1f, 1 / (float)(this.Pending.Count / (float)done + 1)); // = done / (count + done)
			}
		}
	};

	private struct StreamItem { public CTexture tex; public string path; public bool black; public int maxDim; public Phase? phase; }
	private static readonly ConcurrentQueue<Phase> _phaseOlds = new();
	private static Phase _phaseNow = new();
	private static readonly ManualResetEventSlim _canDecodeBytes = new(true);
	private static long _readyBytes;                                       // decoded-but-not-uploaded bytes (backpressure)
	private static readonly int _maxWorkers = Math.Max(2, Environment.ProcessorCount / 2);
	private static readonly SemaphoreSlim _spareDecodeWorkers = new(_maxWorkers, _maxWorkers);

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
	public static float StreamFraction => Volatile.Read(ref _phaseNow).Fraction;
	/// <summary>True once every texture queued during the current phase has been uploaded (or skipped).</summary>
	public static bool StreamComplete => Volatile.Read(ref _phaseNow).Pending.IsEmpty;

	/// <summary>Queue a path-load for background decode + render-thread upload. Returns false only if the file is
	/// missing (→ the caller's inline path handles it / throws). DELIBERATELY does NOT open the file to read the
	/// size: opening triggers per-file AV scanning (~15-20ms), so reading hundreds of headers during an Activate
	/// is itself a multi-second freeze. Pointer + size stay 0 until the upload fills them; t2DDraw no-ops
	/// meanwhile, and a load phase isn't considered done (StreamComplete) until every item uploads. Render thread.</summary>
	private bool tQueueAsyncTexture(string strFileName, bool bBlackTransparent, int maxDimension = 0) {
		if (!FileExistsCached(strFileName))
			return false;
		var phase = Volatile.Read(ref _phaseNow);
		phase.Pending.Enqueue(new StreamItem { tex = this, path = strFileName, black = bBlackTransparent, maxDim = maxDimension, phase = phase });
		EnsureDecodeWorkers();
		return true;
	}

	// Ensure up to _maxWorkers decode tasks are draining _pending. Decode-only on a capped pool → leaves cores
	// for the render thread; tasks exit when the queue empties and restart on new work.
	private static void EnsureDecodeWorkers() {
		if (_spareDecodeWorkers.Wait(0))
			System.Threading.Tasks.Task.Run(DecodeLoop);
	}

	private static void DecodeLoop() {
		const long readyByteCap = 96L * 1024 * 1024;
		var phaseNow = Volatile.Read(ref _phaseNow);
		try {
			void dequeue(Phase phase) {
				while (phase.Pending.TryDequeue(out var item)) {
					// Backpressure: don't decode faster than the render thread uploads (bounded memory).
					bool multipleWorkers = _maxWorkers - _spareDecodeWorkers.CurrentCount > 1;
					while (multipleWorkers && Volatile.Read(ref _readyBytes) >= readyByteCap) {
						_canDecodeBytes.Reset();
						_canDecodeBytes.Wait();
					}
					if (item.tex.bDisposeCompleteDone) { CompleteItem(item); continue; }   // disposed before decode → drop
					SKBitmap? bmp = tClampToMaxDimension(tDecodeForUpload(item.path), item.maxDim);
					if (bmp != null)
						Interlocked.Add(ref _readyBytes, bmp.ByteCount);
					var captured = item;
					Game.AsyncActions.Enqueue(() => UploadOne(captured, bmp));
				}
			}
			while (_phaseOlds.TryDequeue(out var phase)) {
				if (phase != null)
					dequeue(phase);
			}
			dequeue(Volatile.Read(ref _phaseNow));
		} finally {
			try {
				_spareDecodeWorkers.Release();
			} catch (SemaphoreFullException) {
				// ignore unpaired Wait/Release
			}
			if (!Volatile.Read(ref _phaseNow).Pending.IsEmpty)
				EnsureDecodeWorkers();   // an item raced in just as this worker exited
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
			if (bmp != null) {
				Interlocked.Add(ref _readyBytes, -bmp.ByteCount);
				_canDecodeBytes.Set();
				bmp.Dispose();
			}
			CompleteItem(item);
		}
	}

	// Count a phase item toward the bar — but only for the CURRENT generation, so stale items from an ended or
	// cancelled phase can't corrupt a new phase's progress.
	private static void CompleteItem(StreamItem item) {
		if (item.phase != null)
			Interlocked.Increment(ref item.phase.Done);
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

	private static void NextPhase() {
		var phase = Interlocked.Exchange(ref _phaseNow, new());
		if (phase != null && !phase.Pending.IsEmpty)
			_phaseOlds.Enqueue(phase);
	}

	// ── Load-phase API (CAsyncLoad / CStageSongLoading) ─────────────────────────────────────────────
	/// <summary>Begin a load phase: queued textures count toward the bar. A new generation detaches any still
	/// in-flight items from a previous phase. Render thread, before the streamed activation.</summary>
	public static void BeginStreaming() {
		NextPhase();
		StreamingLoad = true;
	}

	/// <summary>No-op: decode now starts per item as it is queued (kept for call-site compatibility).</summary>
	public static void StartStreamDecode() { }
	/// <summary>No-op: uploads finalize via Game.AsyncActions now (kept for call-site compatibility).</summary>
	public static void PumpUploads(double budgetMs) { }

	/// <summary>End a load phase normally (all items uploaded).</summary>
	public static void EndStreaming() {
		StreamingLoad = false;
		NextPhase();
	}

	/// <summary>Cancel a load phase (ESC mid-load): stop counting + reset the bar. In-flight uploads to the
	/// torn-down textures skip via the disposed check; the new generation detaches their counting. Call BEFORE
	/// the screen's DeActivate disposes the stub textures.</summary>
	public static void CancelStreaming() {
		StreamingLoad = false;
		NextPhase();
	}
}
