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

	// Pending holds items not yet picked up by a decode worker; an item counts as done only once it is uploaded
	// (or dropped), so a phase is complete when Done catches up with Queued, not when Pending is empty.
	private class Phase {
		public ConcurrentQueue<StreamItem> Pending = new();
		public int Queued = 0;
		public int Done = 0;

		public bool Complete => Volatile.Read(ref Done) >= Volatile.Read(ref Queued);
		public float Fraction {
			get {
				int queued = Volatile.Read(ref Queued);
				return queued <= 0 ? 1f : Math.Min(1f, Volatile.Read(ref Done) / (float)queued);
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
	public static bool StreamComplete => Volatile.Read(ref _phaseNow).Complete;

	/// <summary>Queue a path-load for background decode + render-thread upload. Returns false only if the file is
	/// missing (→ the caller's inline path handles it / throws). Queueing does not open the file; the size comes
	/// from the decode worker, or from the file header if something reads it before that (once, on the reading
	/// thread). Pointer stays 0 until the upload; t2DDraw no-ops meanwhile, and a load phase isn't considered done
	/// (StreamComplete) until every item uploads.</summary>
	private bool tQueueAsyncTexture(string strFileName, bool bBlackTransparent, int maxDimension = 0) {
		if (!FileExistsCached(strFileName))
			return false;
		// both before the enqueue, so the upload is always the one that clears them
		_sizePending = true;
		_uploadPending = true;
		var phase = Volatile.Read(ref _phaseNow);
		Interlocked.Increment(ref phase.Queued);   // before the enqueue, so Done can never pass Queued
		phase.Pending.Enqueue(new StreamItem { tex = this, path = strFileName, black = bBlackTransparent, maxDim = maxDimension, phase = phase });
		// another thread may have ended this phase meanwhile (NextPhase skips empty phases): keep it reachable
		if (!ReferenceEquals(Volatile.Read(ref _phaseNow), phase))
			_phaseOlds.Enqueue(phase);
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
				while (true) {
					// Backpressure: don't decode faster than the render thread uploads (bounded memory). Wait BEFORE
					// taking an item and only briefly per try: a wakeup lost between Reset and Wait then costs one short
					// wait, never a taken item stuck forever (its phase could not complete).
					while (_maxWorkers - _spareDecodeWorkers.CurrentCount > 1 && Volatile.Read(ref _readyBytes) >= readyByteCap) {
						_canDecodeBytes.Reset();
						if (Volatile.Read(ref _readyBytes) < readyByteCap) break;
						_canDecodeBytes.Wait(20);
					}
					if (!phase.Pending.TryDequeue(out var item)) break;
					if (item.tex.bDisposeCompleteDone) { CompleteItem(item); continue; }   // disposed before decode → drop
					SKBitmap? bmp;
					try {
						bmp = tClampToMaxDimension(tDecodeForUpload(item.path), item.maxDim);
					} catch {
						bmp = null;   // the upload below still completes the item, so the phase can finish
					}
					if (bmp != null) {
						Interlocked.Add(ref _readyBytes, bmp.ByteCount);
						item.tex.tPublishPendingSize(bmp.Width, bmp.Height);   // later size reads need no file read
					}
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
			if (!_phaseOlds.IsEmpty || !Volatile.Read(ref _phaseNow).Pending.IsEmpty)
				EnsureDecodeWorkers();   // an item raced in just as this worker exited
		}
	}

	// Render-thread GL upload (drained from Game.AsyncActions). Skips textures disposed since queueing (e.g. an
	// ESC-cancelled song load tore down the half-loaded game screen).
	private static void UploadOne(StreamItem item, SKBitmap? bmp) {
		// taken first: a failed upload disposes the texture, and what waited for it must still hear about it
		Action? after = item.tex.bDisposeCompleteDone ? null : item.tex.tTakeUploadCallbacks();
		try {
			if (!item.tex.bDisposeCompleteDone) {
				if (bmp != null) {
					item.tex.MakeTexture(bmp, item.black);
				} else {
					// failed decode: a clear 10x10 placeholder like a sync load, so a finished load never leaves Width 0;
					// a size already read from the header stays (the placeholder only draws nothing at it)
					System.Diagnostics.Trace.TraceWarning($"Texture: cannot decode '{item.path}', using a 10x10 placeholder.");
					System.Drawing.Size? known = item.tex.tSizeKnown ? item.tex.szImageSize : null;
					using var blank = new SKBitmap(10, 10);
					blank.Erase(SKColors.Transparent);
					item.tex.MakeTexture(blank, item.black);
					if (known is System.Drawing.Size k && k.Width > 0 && k.Height > 0) item.tex.SetLogicalSize(k.Width, k.Height);
				}
			}
		} catch { /* leave the stub blank on upload failure */ }
		finally {
			if (bmp != null) {
				Interlocked.Add(ref _readyBytes, -bmp.ByteCount);
				_canDecodeBytes.Set();
				bmp.Dispose();
			}
			item.tex.tEndUpload();
			// what waited for the pixels runs before the phase counts the item, so a load waits for it too
			try {
				after?.Invoke();
			} catch (Exception e) {
				System.Diagnostics.Trace.TraceWarning("Texture: an after-upload callback failed: " + e.Message);
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
