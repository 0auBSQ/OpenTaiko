using SkiaSharp;

namespace FDK;

// ── Streamed (deferred) texture loading ───────────────────────────────────────────────────────────────
// Loading a texture = PNG decode (slow, CPU) + GL upload (cheap, render-thread-only). A synchronous
// game-screen activation decodes ~150 PNGs in one blast → a multi-second freeze. Instead, while
// StreamingLoad is on, MakeTexture(path) on the render thread (see CTexture.cs) only QUEUES the pixel work;
// a background task decodes (SKBitmap.Decode — thread-safe) into _streamReady and the loading screen calls
// PumpUploads each frame to do the GL uploads within a time budget. The render loop keeps running → smooth
// real-time bar + responsive ESC + no freeze. Decode off-thread; GL upload on the render thread. Hooking
// MakeTexture itself means we capture EXACTLY what the activation loads (random picks + Lua-driven loads
// included) with zero path-matching.
//
// Wiring: CStageSongLoading (BeginStreaming → game-screen Activate → StartStreamDecode → PumpUploads per
// frame → EndStreaming/CancelStreaming) + the streaming branch at the top of MakeTexture(string).
public partial class CTexture {
	public static volatile bool StreamingLoad;
	private static readonly System.Collections.Concurrent.ConcurrentQueue<(CTexture tex, string path, bool black)> _streamPending = new();
	private static readonly System.Collections.Concurrent.ConcurrentQueue<(CTexture tex, SKBitmap? bmp, bool black)> _streamReady = new();
	private static int _streamTotal;
	private static int _streamUploaded;
	private static long _streamReadyBytes;
	private static int _streamThreadId;   // the activation/render thread that began streaming (Game.MainThreadID can differ from it)
	private static System.Threading.Tasks.Task? _streamDecodeTask;
	private static System.Threading.CancellationTokenSource? _streamCts;

	/// <summary>Real-time stream progress 0..1 (1 when nothing queued). Read each frame for the bar.</summary>
	public static float StreamFraction => _streamTotal <= 0 ? 1f : Math.Min(1f, _streamUploaded / (float)_streamTotal);
	/// <summary>True once every queued texture has been uploaded (or skipped on decode failure).</summary>
	public static bool StreamComplete => _streamUploaded >= _streamTotal;

	/// <summary>Queue a texture for streamed loading. Returns false only if the file is missing (→ the caller's
	/// synchronous path handles it). DELIBERATELY does NOT open/read the file to get dimensions: opening a file
	/// triggers per-file AV (Defender) scanning (~15-20ms each), so reading ~300 headers on the render thread
	/// during Activate is itself a multi-second freeze. The size is filled in when the decode result is
	/// uploaded (PumpUploads → MakeTexture(SKBitmap)); the game screen isn't drawn until streaming completes,
	/// so a transient zero size is invisible. (File.Exists is metadata-only ⇒ no AV scan ⇒ cheap.) Render
	/// thread only.</summary>
	private bool tQueueStreamedTexture(string strFileName, bool bBlackTransparent) {
		if (!File.Exists(strFileName))
			return false;
		// Pointer + size stay 0 until PumpUploads fills them; t2DDraw no-ops while Pointer == 0.
		_streamPending.Enqueue((this, strFileName, bBlackTransparent));
		System.Threading.Interlocked.Increment(ref _streamTotal);
		return true;
	}

	/// <summary>Enable streamed loading + reset counters. Call on the MAIN thread BEFORE the activation that
	/// should be streamed (e.g. the game-screen activation).</summary>
	public static void BeginStreaming() {
		tClearStreamQueues();
		_streamTotal = 0;
		_streamUploaded = 0;
		_streamThreadId = Thread.CurrentThread.ManagedThreadId;   // stream only on THIS thread (the activation thread)
		StreamingLoad = true;
		System.Diagnostics.Trace.TraceInformation($"[STREAM] begin  main={Game.MainThreadID} cur={_streamThreadId}");
	}

	/// <summary>After the streamed activation has finished queueing (StreamingLoad turned back off), kick off
	/// the background decode of everything queued. Decode-only on CAPPED threads → safe + leaves cores for the
	/// render thread. Backpressure keeps the decoded-but-not-yet-uploaded set bounded; a decode failure still
	/// enqueues a (tex, null) so the count completes (no hang).</summary>
	public static void StartStreamDecode() {
		var items = _streamPending.ToArray();
		while (_streamPending.TryDequeue(out _)) { }   // drain (snapshot taken above)
		System.Diagnostics.Trace.TraceInformation($"[STREAM] decode start queued={items.Length} (streamTotal={_streamTotal})");
		if (items.Length == 0)
			return;
		_streamCts = new System.Threading.CancellationTokenSource();
		var ct = _streamCts.Token;
		_streamDecodeTask = System.Threading.Tasks.Task.Run(() => {
			const long readyByteCap = 96L * 1024 * 1024;
			const int readyCountCap = 48;
			var opts = new System.Threading.Tasks.ParallelOptions {
				MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount / 2),
				CancellationToken = ct,
			};
			try {
				System.Threading.Tasks.Parallel.ForEach(items, opts, item => {
					// Backpressure: don't decode faster than the render thread uploads (bounded memory).
					while (!ct.IsCancellationRequested
						&& (_streamReady.Count >= readyCountCap || System.Threading.Interlocked.Read(ref _streamReadyBytes) >= readyByteCap))
						System.Threading.Thread.Sleep(1);
					if (ct.IsCancellationRequested) return;

					SKBitmap? bmp = null;
					try {
						if (File.Exists(item.path))
							bmp = SKBitmap.Decode(item.path);
					} catch { bmp = null; }
					if (bmp != null)
						System.Threading.Interlocked.Add(ref _streamReadyBytes, bmp.ByteCount);
					_streamReady.Enqueue((item.tex, bmp, item.black));
				});
			} catch (OperationCanceledException) { /* cancelled */ }
		}, ct);
	}

	/// <summary>Drain decoded bitmaps to the GPU within a per-frame time budget (render thread). Call each
	/// frame from the loading screen until StreamComplete.</summary>
	public static void PumpUploads(double budgetMs) {
		var sw = System.Diagnostics.Stopwatch.StartNew();
		do {
			if (!_streamReady.TryDequeue(out var item))
				break;
			long bytes = 0;
			try {
				if (item.bmp != null) {
					bytes = item.bmp.ByteCount;
					item.tex.MakeTexture(item.bmp, item.black);   // render thread ⇒ synchronous GL upload
				}
			} catch { /* leave the stub blank on upload failure */ }
			finally {
				item.bmp?.Dispose();
				if (bytes != 0)
					System.Threading.Interlocked.Add(ref _streamReadyBytes, -bytes);
				System.Threading.Interlocked.Increment(ref _streamUploaded);
			}
		} while (sw.Elapsed.TotalMilliseconds < budgetMs);
	}

	/// <summary>Normal completion: stop the (finished) decode task and clear residue.</summary>
	public static void EndStreaming() => tStopStreaming(resetCounters: false);

	/// <summary>Cancellation (ESC mid-stream): stop the decode task, drop all pending/ready work, reset.
	/// Call BEFORE the game screen's DeActivate disposes the stub textures.</summary>
	public static void CancelStreaming() => tStopStreaming(resetCounters: true);

	private static void tStopStreaming(bool resetCounters) {
		StreamingLoad = false;
		_streamCts?.Cancel();
		try { _streamDecodeTask?.Wait(500); } catch { /* ignore cancellation/decode errors */ }
		_streamCts?.Dispose();
		_streamCts = null;
		_streamDecodeTask = null;
		tClearStreamQueues();
		if (resetCounters) {
			_streamTotal = 0;
			_streamUploaded = 0;
		}
	}

	private static void tClearStreamQueues() {
		while (_streamPending.TryDequeue(out _)) { }
		while (_streamReady.TryDequeue(out var item))
			item.bmp?.Dispose();
		System.Threading.Interlocked.Exchange(ref _streamReadyBytes, 0);
	}
}
