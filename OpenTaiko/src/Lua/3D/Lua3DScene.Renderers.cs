using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenTaiko {
	// The renderer host: mode selection, rendering, the failure policy and stats.
	public partial class Lua3DScene {
		internal int _threads = 1;   // rasterizer threads (screen split into this many row bands)
		internal ParallelOptions _po = new ParallelOptions { MaxDegreeOfParallelism = 1 };

		// ── Renderers ─────────────────────────────────────────────────────────────────
		// The rasterizer is created up front (the common case); the raytracer is created lazily
		// the first time raytrace mode is selected. _renderer points at the active one.
		private readonly Rasterizer _rasterizer = new();
		private Raytracer? _raytracer;
		private GpuRaytracer? _gpuRaytracer;
		private GpuRasterizer? _gpuRasterizer;
		private IRenderer _renderer;
		// every renderer created, in order: cache drops and Dispose reach them all, active or not
		private readonly List<IRenderer> _made = new(4);
		// the renderer that composites 2D drawing onto a canvas a GPU renderer drew into
		private ICanvasOverlay? _overlay2d;
		// a renderer just created, kept in _made
		private T Made<T>(T r) where T : IRenderer {
			_made.Add(r);
			if (r is ICanvasOverlay o) _overlay2d ??= o;
			return r;
		}
		/// <summary>Set by the GPU rasterizer when it has rendered straight into the canvas texture, so
		/// <see cref="Upload"/> skips re-pushing the (now stale) CPU buffer over the GPU result.</summary>
		internal bool _gpuOwnsCanvas;

		/// <summary>Number of CPU threads the rasterizer uses (screen split into this many
		/// horizontal row bands), at most the processor count. 1 = single-threaded.</summary>
		public void SetThreads(int n) {
			_threads = Math.Clamp(n, 1, Environment.ProcessorCount);
			_po = new ParallelOptions { MaxDegreeOfParallelism = _threads };
		}

		/// <summary>Choose the renderer: "raster" (default) or "raytrace"/"rt" (path tracer).
		/// Switching drops any cached/accumulated state.</summary>
		public void SetMode(string mode) {
			if (_disposedValue) return;
			IRenderer want;
			if (mode == "raytrace_cpu" || mode == "rt_cpu") want = (_raytracer ??= Made(new Raytracer()));   // force the CPU path tracer
			else if (mode == "raytrace" || mode == "rt")                                                      // GPU compute path tracer (CPU fallback)
				want = FDK.Game.ComputeShadersAvailable ? (_gpuRaytracer ??= Made(new GpuRaytracer())) : (IRenderer)(_raytracer ??= Made(new Raytracer()));
			else if (mode == "raster_cpu") want = _rasterizer;     // force the CPU rasterizer (e.g. mirror surfaces)
			else if (FDK.Game.ComputeShadersAvailable) want = (_gpuRasterizer ??= Made(new GpuRasterizer()));   // "raster"/"raster_gpu"/"gpu" → GPU (the default)
			else want = _rasterizer;                               // CPU fallback when GLES 3.1 is unavailable
			if (want != _renderer) {
				TraceUntraced();
				_renderer = want; _renderer.Invalidate();
				_renderFails = 0; _renderTraceAt = 0; _fallbackNote = "";
			}
		}
		/// <summary>"raster" (GPU, the default), "raster_cpu" (CPU fallback) or "raytrace". After a renderer
		/// failure this names the renderer that took over.</summary>
		public string GetMode() => _renderer.Mode;

		/// <summary>Accumulated samples-per-pixel of the path tracer since its last reset (0 in
		/// raster mode or right after a camera/scene change). Useful for a convergence readout.</summary>
		public int GetSampleCount() => _renderer.SampleCount;

		/// <summary>Backend readout for raytrace mode: e.g. "GPU compute: 0.8 ms/spp → 11 spp/fr", or
		/// "CPU fallback: …" if the compute shader couldn't build or a tracer failed while rendering, or
		/// "CPU path tracer" when forced.</summary>
		public string GetRaytracerStatus() => _renderer.TracerStatus(_fallbackNote);

		/// <summary>Drop every REBUILDABLE render cache: the CPU reflection canvases/depths/pixel
		/// buffers and (on the GPU renderer) the per-object VBO cache, merged static buffers and
		/// reflection FBO targets. Registered textures/sprites and compiled programs are kept.
		/// Call on map unload; everything re-creates lazily on the next frame that needs it.</summary>
		public void ClearRenderCaches() {
			if (_disposedValue) return;
			foreach (var c in _rttCanvases.Values) c.Dispose();
			_rttCanvases.Clear(); _rttDepths.Clear(); _rttBufs.Clear();
			_rttCanvas = null; _rttDepth = null;
			foreach (var r in _made) r.ClearTransient();
		}

		/// <summary>Memory readout for debug overlays / leak tests:
		/// (texture count, texture bytes, sprite count, object count, grass blades, live particles).</summary>
		public (int, long, int, int, int, int) GetMemoryStats() {
			long texBytes = 0; int texs = 0, sprites = 0;
			foreach (var e in _tex.Values) if (e.Pix != null) { texs++; texBytes += (long)e.Pix.Length * 4; }
			foreach (var e in _sprites.Values) if (e.Pix != null) sprites++;
			int parts = 0;
			foreach (var ps in _particleSystems) parts += ps.Count;
			return (texs, texBytes, sprites, _objects.Count, _grass.Count / 6, parts);
		}

		/// <summary>Renderer-side cache counts ("glTex=N glSprite=N rtt=N vbo=N merge=N"), or "cpu". After a renderer
		/// failure it adds " fallback=" and the reason, and " failing" while the active renderer's frames fail.</summary>
		public string GetRendererStats() {
			string st = _renderer.Stats;
			if (_fallbackNote.Length > 0) st += " fallback=" + _fallbackNote;
			return _renderFails > 0 ? st + " failing" : st;
		}

		/// <summary>Clear depth (raster) / reset accumulation (raytrace) as needed, then render
		/// every visible object with the active renderer. Does not touch the colour buffer's
		/// background (draw your sky/clear first) and does not Upload (call Upload after).</summary>
		public void Render() {
			if (_disposedValue) return;
			// a renderer that takes over after a failure renders the same frame (at most one step per renderer in the chain)
			for (int attempt = 0; attempt < 3; attempt++) {
				try {
					_renderer.Render(this);
					// CPU path: the toon outline runs as a canvas post-op (GPU scenes ink it in-shader)
					if (_outlineTh > 0 && !_gpuOwnsCanvas) ApplyOutline(_outlineR, _outlineG, _outlineB, (int)Math.Round(_outlineTh));
					_renderFails = 0;
					if (_renderUntraced > 0 && Environment.TickCount64 >= _renderTraceAt) TraceUntraced();
					return;
				} catch (Exception e) {
					if (!FallBack(e)) return;
				}
			}
		}

		// ── Renderer failure policy ──
		// A failed Render is traced (a full report at most every few seconds) and its frame is skipped. The next
		// renderer down the chain takes over (GPU rasterizer -> CPU rasterizer, GPU path tracer -> CPU path tracer
		// -> CPU rasterizer) at once for a GL or driver failure, and after RenderFailLimit failed frames in a row
		// for a data error, which would usually fail the same way on every renderer.
		private const int RenderFailLimit = 10;
		private int _renderFails;                 // consecutive failed frames of the active renderer
		private long _renderTraceAt;              // Environment.TickCount64 before which failures are only counted
		private int _renderUntraced;              // failures counted since the last trace
		private string _fallbackNote = "";        // why the active renderer took over ("" = no fallback)

		internal static bool IsDataError(Exception e) {
			if (e is AggregateException ae) {   // a failure inside a Parallel.For band
				var inner = ae.Flatten().InnerExceptions;
				foreach (var ie in inner) if (!IsDataError(ie)) return false;
				return inner.Count > 0;
			}
			return e is IndexOutOfRangeException || e is ArgumentException
				|| e is KeyNotFoundException || e is InvalidCastException || e is NullReferenceException
				|| e is ArithmeticException || e is InvalidOperationException;
		}

		/// <summary>Trace a failed Render and switch to the next renderer when the policy says so. Returns true
		/// when a new renderer should render the frame, false to skip it.</summary>
		private bool FallBack(Exception e) {
			_renderFails++;
			string name = _renderer.Name;
			bool sw = _renderer != _rasterizer && (!IsDataError(e) || _renderFails >= RenderFailLimit);
			IRenderer? next = sw ? (_renderer == (IRenderer?)_gpuRaytracer ? (_raytracer ??= Made(new Raytracer())) : _rasterizer) : null;
			long now = Environment.TickCount64;
			if (next != null || now >= _renderTraceAt) {
				string more = _renderUntraced > 0 ? $" ({_renderUntraced} more failures since the last report)" : "";
				string what = next != null ? $"switching to the {next.Name}" : "frame skipped";
				string detail = next != null || _renderFails == 1 ? e.ToString() : e.GetType().Name + ": " + e.Message;
				System.Diagnostics.Trace.TraceError($"Lua3DScene: the {name} failed ({_renderFails} in a row), {what}{more}.\n{detail}");
				_renderUntraced = 0; _renderTraceAt = now + 5000;
			} else {
				_renderUntraced++;
			}
			if (next == null) return false;
			string note = $"{name} failed: {e.GetType().Name}: {e.Message}".Replace('\n', ' ').Replace('\r', ' ');
			_fallbackNote = note.Length > 120 ? note.Substring(0, 120) : note;
			_renderer = next; _renderer.Invalidate();
			_renderFails = 0; _renderTraceAt = 0; _gpuOwnsCanvas = false;   // the next renderer's first failure is traced in full
			return true;
		}

		/// <summary>Trace the failures counted since the last report, if any.</summary>
		private void TraceUntraced() {
			if (_renderUntraced == 0) return;
			System.Diagnostics.Trace.TraceError($"Lua3DScene: the {_renderer.Name} failed {_renderUntraced} more times since the last report.");
			_renderUntraced = 0; _renderTraceAt = Environment.TickCount64 + 5000;
		}
	}
}
