using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FDK;
using NLua;

namespace OpenTaiko {
	/// <summary>
	/// A 3D render target for Lua stages, drawn like a <see cref="LuaTexture"/>/<see cref="LuaCanvas"/>.
	/// Owns a colour buffer (internal LuaCanvas) + a depth buffer + a camera, the retained scene
	/// data (objects, materials, lights, primitives, textures) and the active renderer.
	///
	/// Two renderers share this scene data behind <see cref="IRenderer"/>: the <see cref="Rasterizer"/>
	/// (default — flat/textured, perspective-correct, near-plane clipping, multithreaded) and the
	/// path-traced <see cref="Raytracer"/> (global illumination, materials, analytic + SDF prims).
	/// Switch with <see cref="SetMode"/>; the rest of the API is renderer-agnostic.
	///
	/// Lua keeps the scene logic and submits world-space triangles/quads (and, for the raytracer,
	/// lights/materials/primitives); the heavy per-pixel work runs natively here.
	/// </summary>
	public class Lua3DScene : IDisposable {
		internal LuaCanvas _canvas;
		internal HashSet<Lua3DScene>? _disposeList = null;

		internal int _w;   // mutable: RenderView temporarily shrinks the viewport for reduced-res reflections
		internal int _h;
		internal float[] _depth;

		// Camera, owned by the scene: position + orientation (yaw/pitch → basis) +
		// lens (fov → scale, near plane). Lua feeds these and reads the basis / Project back.
		internal double _camX, _camY, _camZ;
		internal double _Rx = 1, _Ry, _Rz, _Ux, _Uy = 1, _Uz, _Fx, _Fy, _Fz = 1;
		// ground-sprite (billboard mode 3) cast-shadow light: the horizontal direction shadows stretch in,
		// and a length scale. Set from Lua (SetGroundSpriteLight) to match the stage's shadow light.
		internal double _GsLx = 0, _GsLz = 1, _GsLen = 1.4;
		internal double _yaw, _pitch;
		internal double _fov = 70.0;
		internal double _near = 0.06;
		internal double _scale = 1.0;
		internal double _renderDist = 0.0;   // 0 = unlimited; else objects past this are culled
		internal int _threads = 1;   // rasterizer threads (screen split into this many row bands)
		internal ParallelOptions _po = new ParallelOptions { MaxDegreeOfParallelism = 1 };

		// Distance fog: pixels fade toward (_fogR,_fogG,_fogB) from _fogStart to _fogEnd
		// (camera-space depth). _fogInv = 1/(end-start). Off by default.
		internal bool _fog;
		internal double _fogR, _fogG, _fogB, _fogStart, _fogInv;
		// Distance BLUR (deprecated — too costly + the depth-texture FBO slowed the main pass). Kept as a no-op
		// stub so older stage scripts don't error; the diorama pass below replaces it.
		internal bool _distBlur;
		internal double _blurNear = 60, _blurFar = 200;
		// 'Diorama' grade (GPU rasterizer): a cheap screen-space tilt-shift + saturation/contrast/vignette
		// (diorama-style). No depth sample. Off by default.
		internal bool _diorama;
		internal double _hdTilt = 3.0, _hdSat = 1.25, _hdVig = 0.35, _hdBloom = 0.0;

		internal readonly Dictionary<int, int[]> _texPix = new();
		internal readonly Dictionary<int, int> _texW = new();
		internal readonly Dictionary<int, int> _texH = new();
		// bumped whenever a texture's pixels change (e.g. a per-frame mirror RTT); the GPU renderer compares
		// it to know when to re-upload that texture's GL copy instead of using its cached (stale) upload.
		internal readonly Dictionary<int, int> _texRev = new();
		internal void BumpTexRev(int id) { _texRev[id] = _texRev.TryGetValue(id, out var v) ? v + 1 : 1; }

		// ── Renderers ─────────────────────────────────────────────────────────────────
		// The rasterizer is created up front (the common case); the raytracer is created lazily
		// the first time raytrace mode is selected. _renderer points at the active one.
		private readonly Rasterizer _rasterizer = new();
		private Raytracer? _raytracer;
		private GpuRaytracer? _gpuRaytracer;
		private GpuRasterizer? _gpuRasterizer;
		private IRenderer _renderer;
		/// <summary>Set by the GPU rasterizer when it has rendered straight into the canvas texture, so
		/// <see cref="Upload"/> skips re-pushing the (now stale) CPU buffer over the GPU result.</summary>
		internal bool _gpuOwnsCanvas;
		/// <summary>Bumped on every camera / geometry / light / material / primitive edit; the
		/// raytracer compares it to know when to reset its progressive accumulation.</summary>
		internal int Revision;

		public Lua3DScene(int width, int height) {
			_canvas = new LuaCanvas(width, height);
			_w = _canvas._w;
			_h = _canvas._h;
			_depth = new float[_w * _h];
			_scale = (_h * 0.5) / Math.Tan(_fov * 0.5 * Math.PI / 180.0);
			// GPU hardware-pipeline rasterizer is the DEFAULT when GLES 3.1 is available; the CPU
			// Rasterizer remains the automatic fallback (and for "raster_cpu" / raytrace).
			_renderer = FDK.Game.ComputeShadersAvailable ? (_gpuRasterizer = new GpuRasterizer()) : _rasterizer;
		}

		public int Width => _w;
		public int Height => _h;

		#region Frame setup / 2D / textures
		public void Clear(int r, int g, int b, int a) => _canvas.Clear(r, g, b, a);
		public void FillRect(int x, int y, int w, int h, int r, int g, int b, int a) {
			if (_gpuOwnsCanvas && _gpuRasterizer != null) _gpuRasterizer.Rect2D(this, x, y, w, h, r, g, b, a);   // after GPU 3D: composite onto the canvas texture
			else _canvas.FillRect(x, y, w, h, r, g, b, a);
		}
		public void ClearDepth() => Array.Clear(_depth, 0, _depth.Length);

		// ── Scene-owned camera ────────────────────────────────────────────────────────
		// The scene holds the camera state and derives the basis + projection scale itself,
		// so Lua only feeds eye position, yaw/pitch and the lens, then reads the basis back
		// for movement / picking.

		/// <summary>Eye position in world space.</summary>
		public void SetCameraPosition(double x, double y, double z) { _camX = x; _camY = y; _camZ = z; Revision++; }

		/// <summary>Look direction in degrees. Yaw turns about +Y; +pitch looks up (clamped ±89.9).
		/// Rebuilds the right/up/forward basis used by the rasterizer and Project.</summary>
		public void SetCameraAngles(double yawDeg, double pitchDeg) {
			if (pitchDeg > 89.9) pitchDeg = 89.9; else if (pitchDeg < -89.9) pitchDeg = -89.9;
			_yaw = yawDeg; _pitch = pitchDeg;
			double y = yawDeg * Math.PI / 180.0, p = pitchDeg * Math.PI / 180.0;
			double cy = Math.Cos(y), sy = Math.Sin(y), cp = Math.Cos(p), sp = Math.Sin(p);
			_Fx = sy * cp;  _Fy = sp;  _Fz = cy * cp;   // forward
			_Rx = cy;       _Ry = 0;   _Rz = -sy;       // right
			_Ux = -sy * sp; _Uy = cp;  _Uz = -cy * sp;  // up
			Revision++;
		}

		/// <summary>Vertical field of view in degrees; updates the projection scale.</summary>
		public void SetCameraFov(double fovDeg) {
			_fov = fovDeg < 1 ? 1 : (fovDeg > 179 ? 179 : fovDeg);
			_scale = (_h * 0.5) / Math.Tan(_fov * 0.5 * Math.PI / 180.0);
			Revision++;
		}

		/// <summary>Near clip plane (camera-space z, > 0).</summary>
		public void SetCameraNear(double near) { _near = near < 1e-4 ? 1e-4 : near; Revision++; }

		/// <summary>Project a world point to scene-buffer pixel coords. Returns (x, y, depth); depth is
		/// camera-space z — &lt;= 0 means behind the camera / off-screen (ignore x,y then). Lets Lua place
		/// 2D overlays (HP bars, markers, a sun sprite) over the 3D view.</summary>
		public (double, double, double) WorldToScreen(double wx, double wy, double wz) {
			double rx = wx - _camX, ry = wy - _camY, rz = wz - _camZ;
			double cx = rx * _Rx + ry * _Ry + rz * _Rz;
			double cy = rx * _Ux + ry * _Uy + rz * _Uz;
			double cz = rx * _Fx + ry * _Fy + rz * _Fz;
			if (cz <= _near) return (0, 0, -1);
			double iz = 1.0 / cz;
			return (_w * 0.5 + cx * iz * _scale, _h * 0.5 - cy * iz * _scale, cz);
		}

		// ── Render-to-texture cameras ───────────────────────────────────────────────────
		// Render the scene from an arbitrary camera into texture `texId` (same resolution as the
		// scene), so a quad can display that view (mirrors, portals, security monitors, …). Mirror
		// and overlay objects are skipped inside the render so reflections don't recurse on themselves.
		internal bool _rttPass;
		private readonly Dictionary<int, LuaCanvas> _rttCanvases = new();   // per-texId (two live portals share nothing)
		private readonly Dictionary<int, float[]> _rttDepths = new();
		private LuaCanvas? _rttCanvas; private float[]? _rttDepth;
		private readonly Dictionary<int, int[]> _rttBufs = new();
		// Reflection/portal views (RenderView) are re-renders of the whole scene; they only show on a
		// small surface, so rendering them at a fraction of the main resolution is a big win for little
		// visual loss. _rttShift = 0 full, 1 half (¼ the pixels), 2 quarter (1/16). Screen-tex sampling
		// maps the main pixel (px,py) → reduced texel (px>>shift, py>>shift).
		internal int _rttShift = 1;
		/// <summary>Resolution of reflection/portal re-renders relative to the main view: 0=full,
		/// 1=half (default, ¼ the work), 2=quarter (1/16). Higher = faster, blockier reflections.</summary>
		public void SetReflectionScale(int shift) { _rttShift = shift < 0 ? 0 : (shift > 3 ? 3 : shift); }

		// Optional post-process antialiasing (FXAA-lite). Off by default; smooths a low-res render.
		internal bool _aa;
		/// <summary>Enable/disable post-process edge smoothing on the final colour buffer.</summary>
		public void SetAntialias(bool on) { _aa = on; }

		internal double _shadowHalf = 60.0;
		internal int _outlineR, _outlineG, _outlineB; internal double _outlineTh = 0;
		/// <summary>Half-extent (world units) of the sun shadow map's box around the focus. Smaller = sharper
		/// shadows over a tighter area (e.g. small iso maps want ~18); default 60.</summary>
		public void SetShadowArea(double half) { _shadowHalf = half < 4 ? 4 : half; }

		/// <summary>Toon OUTLINE for the rendered scene: silhouette ink of the given colour and pixel
		/// thickness with an anti-aliased rim. GPU scenes run it as a post shader; CPU scenes fall back
		/// to ApplyOutline after the raster. 0 thickness = off.</summary>
		public void SetOutline(int r, int g, int b, double thicknessPx) {
			_outlineR = r; _outlineG = g; _outlineB = b; _outlineTh = thicknessPx < 0 ? 0 : thicknessPx;
		}

		public void RenderView(int texId, double cx, double cy, double cz, double yawDeg, double pitchDeg,
			double fovDeg, int clr, int clg, int clb, int flipX) {
			int rw = _w >> _rttShift, rh = _h >> _rttShift;   // reduced render size
			if (rw < 1) rw = 1; if (rh < 1) rh = 1;
			int ow = _w, oh = _h;
			double ox = _camX, oy = _camY, oz = _camZ, oyaw = _yaw, opit = _pitch, ofov = _fov;

			_w = rw; _h = rh;                                 // projection aspect/scale matches the RTT viewport
			_rttPass = true;
			SetCameraPosition(cx, cy, cz); SetCameraFov(fovDeg); SetCameraAngles(yawDeg, pitchDeg);
			// a planar mirror is a reflected (left-handed) camera: flip the right axis so the image is
			// mirrored. Without this the reflection pans the wrong way as the player turns.
			if (flipX != 0) { _Rx = -_Rx; _Ry = -_Ry; _Rz = -_Rz; }

			// Reflections/portals always render with the CPU rasterizer into a small canvas, then upload as
			// texId. The GPU off-screen path (GpuRasterizer.RenderToTexture) had persistent driver issues with
			// >1 live reflection on this hardware (the main view lost geometry + portals failed to capture), so
			// the reliable CPU capture is used regardless of the main renderer — the main view still runs on
			// the GPU; only these small reduced-res reflection re-renders are CPU. Keep them cheap via
			// SetReflectionScale (the FPS stage uses 1/4-res reflections).
			{
				var sc = _canvas; var sd = _depth;
				if (!_rttCanvases.TryGetValue(texId, out _rttCanvas) || _rttCanvas._w != rw || _rttCanvas._h != rh) {
					_rttCanvas = new LuaCanvas(rw, rh); _rttCanvases[texId] = _rttCanvas;
					_rttDepth = new float[rw * rh]; _rttDepths[texId] = _rttDepth;
				} else {
					_rttDepth = _rttDepths[texId];
				}
				_canvas = _rttCanvas; _depth = _rttDepth;
				_rttCanvas.Clear(clr, clg, clb, 255);
				_rasterizer.Render(this);
				if (!_rttBufs.TryGetValue(texId, out var px) || px.Length != rw * rh) { px = new int[rw * rh]; _rttBufs[texId] = px; }
				var buf = _rttCanvas._buf;
				for (int i = 0; i < px.Length; i++) { int o = i * 4; px[i] = (buf[o] << 16) | (buf[o + 1] << 8) | buf[o + 2]; }
				RegisterTexturePixels(texId, px, rw, rh);
				_canvas = sc; _depth = sd;
			}
			_rttPass = false;

			_w = ow; _h = oh;
			SetCameraPosition(ox, oy, oz); SetCameraFov(ofov); SetCameraAngles(oyaw, opit);
		}

		/// <summary>HYBRID rendering: like <see cref="RenderView"/>, but the inset view is rendered by the
		/// CPU PATH TRACER instead of the rasterizer — true raytraced output (soft shadows, GI-ish bounce)
		/// composited into the rasterized main scene via a screen-sampled quad (mirrors, monitors, …).
		/// Renders at a quarter of the reflection resolution (path tracing is per-pixel expensive).</summary>
		public void RenderViewRT(int texId, double cx, double cy, double cz, double yawDeg, double pitchDeg,
			double fovDeg, int clr, int clg, int clb, int flipX) {
			int shift = _rttShift + 2;                         // RT insets render at 1/8: path tracing is per-pixel expensive
			int rw = _w >> shift, rh = _h >> shift;
			if (rw < 1) rw = 1; if (rh < 1) rh = 1;
			int ow = _w, oh = _h;
			double ox = _camX, oy = _camY, oz = _camZ, oyaw = _yaw, opit = _pitch, ofov = _fov;
			_w = rw; _h = rh;
			_rttPass = true;
			SetCameraPosition(cx, cy, cz); SetCameraFov(fovDeg); SetCameraAngles(yawDeg, pitchDeg);
			if (flipX != 0) { _Rx = -_Rx; _Ry = -_Ry; _Rz = -_Rz; }
			{
				var sc = _canvas; var sd = _depth;
				if (_rttCanvas == null || _rttCanvas._w != rw || _rttCanvas._h != rh) {
					_rttCanvas = new LuaCanvas(rw, rh); _rttDepth = new float[rw * rh];
				}
				_canvas = _rttCanvas; _depth = _rttDepth;
				_rttCanvas.Clear(clr, clg, clb, 255);
				try {
					var rt = _raytracer ??= new Raytracer();
					rt.MaxDepthOverride = 3;                   // 3 bounces is plenty for a mirror inset
					rt.Render(this);
					rt.MaxDepthOverride = 0;
				}
				catch { _rasterizer.Render(this); }            // RT path unavailable → raster fallback
				if (!_rttBufs.TryGetValue(texId, out var px) || px.Length != rw * rh) { px = new int[rw * rh]; _rttBufs[texId] = px; }
				var buf = _rttCanvas._buf;
				for (int i = 0; i < px.Length; i++) { int o = i * 4; px[i] = (buf[o] << 16) | (buf[o + 1] << 8) | buf[o + 2]; }
				RegisterTexturePixels(texId, px, rw, rh);
				_canvas = sc; _depth = sd;
			}
			_rttPass = false;
			_w = ow; _h = oh;
			SetCameraPosition(ox, oy, oz); SetCameraFov(ofov); SetCameraAngles(oyaw, opit);
		}

		/// <summary>Cull objects whose bounds lie entirely beyond this distance from the camera
		/// (0 = unlimited). Lowers the drawn geometry for big worlds.</summary>
		public void SetRenderDistance(double d) { _renderDist = d < 0 ? 0 : d; Revision++; }

		/// <summary>Distance fog: pixels fade to (r,g,b) (0-255) between camera depths
		/// <paramref name="start"/> and <paramref name="end"/>. <paramref name="on"/>=false disables.</summary>
		public void SetFog(bool on, double r, double g, double b, double start, double end) {
			_fog = on;
			_fogR = r; _fogG = g; _fogB = b; _fogStart = start;
			_fogInv = end > start ? 1.0 / (end - start) : 0.0;
			Revision++;
		}
		/// <summary>Deprecated no-op (the old distance blur). Kept so older scripts don't break — use SetDiorama.</summary>
		public void SetDistanceBlur(bool on, double near, double far) { _distBlur = false; }
		/// <summary>diorama 'diorama' post grade (GPU rasterizer): a cheap screen-space tilt-shift (sharp middle band,
		/// blur to the top/bottom) + saturation/contrast/vignette + bloom. <paramref name="tilt"/> = max blur px
		/// (0 = none), <paramref name="sat"/> = saturation (1 = none), <paramref name="vig"/> = vignette strength,
		/// <paramref name="bloom"/> = glow strength (0 = off; the diorama-style highlight halo).</summary>
		public void SetDiorama(bool on, double tilt, double sat, double vig, double bloom = 0.0) {
			_diorama = on; _hdTilt = tilt; _hdSat = sat; _hdVig = vig; _hdBloom = bloom; Revision++;
		}

		/// <summary>Number of CPU threads the rasterizer uses (screen split into this many
		/// horizontal row bands). 1 = single-threaded.</summary>
		public void SetThreads(int n) {
			_threads = n < 1 ? 1 : n;
			_po = new ParallelOptions { MaxDegreeOfParallelism = _threads };
		}

		/// <summary>Choose the renderer: "raster" (default) or "raytrace"/"rt" (path tracer).
		/// Switching drops any cached/accumulated state.</summary>
		public void SetMode(string mode) {
			IRenderer want;
			if (mode == "raytrace_cpu" || mode == "rt_cpu") want = (_raytracer ??= new Raytracer());   // force the CPU path tracer
			else if (mode == "raytrace" || mode == "rt")                                                // GPU compute path tracer (CPU fallback)
				want = FDK.Game.ComputeShadersAvailable ? (_gpuRaytracer ??= new GpuRaytracer()) : (IRenderer)(_raytracer ??= new Raytracer());
			else if (mode == "raster_cpu") want = _rasterizer;     // force the CPU rasterizer (e.g. mirror surfaces)
			else if (FDK.Game.ComputeShadersAvailable) want = (_gpuRasterizer ??= new GpuRasterizer());   // "raster"/"raster_gpu"/"gpu" → GPU (the default)
			else want = _rasterizer;                               // CPU fallback when GLES 3.1 is unavailable
			if (want != _renderer) { _renderer = want; _renderer.Invalidate(); Revision++; }
		}
		/// <summary>"raster" (GPU, the default), "raster_cpu" (CPU fallback) or "raytrace".</summary>
		public string GetMode() => _renderer == (IRenderer?)_gpuRasterizer ? "raster" : (_renderer == _rasterizer ? "raster_cpu" : "raytrace");

		/// <summary>Accumulated samples-per-pixel of the path tracer since its last reset (0 in
		/// raster mode or right after a camera/scene change). Useful for a convergence readout.</summary>
		public int GetSampleCount() => _renderer is GpuRaytracer g ? g.SampleCount : (_renderer is Raytracer r ? r.SampleCount : 0);

		/// <summary>Backend readout for raytrace mode: e.g. "GPU compute: 0.8 ms/spp → 11 spp/fr", or
		/// "CPU fallback: …" if the compute shader couldn't build, or "CPU path tracer" when forced.</summary>
		public string GetRaytracerStatus() => _renderer is GpuRaytracer gr ? gr.Status : (_renderer is Raytracer ? "CPU path tracer" : "");

		public double GetCameraFov() => _fov;
		public double GetCameraNear() => _near;
		public double GetCameraYaw() => _yaw;
		public double GetCameraPitch() => _pitch;
		public (double, double, double) GetCameraPosition() => (_camX, _camY, _camZ);
		public (double, double, double) GetCameraForward() => (_Fx, _Fy, _Fz);
		public (double, double, double) GetCameraRight() => (_Rx, _Ry, _Rz);
		public (double, double, double) GetCameraUp() => (_Ux, _Uy, _Uz);

		/// <summary>Set the cast-shadow light for ground-flat sprites (billboard mode 3): the horizontal
		/// direction the silhouette stretches in (dx,dz) and a length scale (≈ 1/tan(sun elevation)). A
		/// transparency-aware shadow is drawn by adding the actor's own sprite to a black-tinted object in
		/// this mode — it lies on the floor with the sprite's cutout alpha as the shape.</summary>
		public void SetGroundSpriteLight(double dx, double dz, double lenScale) {
			double l = Math.Sqrt(dx * dx + dz * dz);
			if (l > 1e-6) { _GsLx = dx / l; _GsLz = dz / l; }
			_GsLen = lenScale > 0.05 ? lenScale : 0.05;
		}

		/// <summary>Project a world point to screen pixels using the scene camera:
		/// returns (sx, sy, inFront). inFront is false when the point is behind the near plane.</summary>
		public (double, double, bool) Project(double wx, double wy, double wz) {
			double rx = wx - _camX, ry = wy - _camY, rz = wz - _camZ;
			double cz = rx * _Fx + ry * _Fy + rz * _Fz;
			if (cz < _near) return (0, 0, false);
			double cx = rx * _Rx + ry * _Ry + rz * _Rz;
			double cyv = rx * _Ux + ry * _Uy + rz * _Uz;
			return (_w * 0.5 + cx / cz * _scale, _h * 0.5 - cyv / cz * _scale, true);
		}

		public void RegisterTexture(int id, LuaTable pixels, int w, int h) {
			if (pixels == null || w <= 0 || h <= 0) return;
			var px = new int[w * h];
			for (int i = 0; i < px.Length; i++) {
				object o = pixels[i + 1];
				px[i] = o switch { long l => (int)l, double d => (int)d, int ii => ii, _ => 0 };
			}
			_texPix[id] = px; _texW[id] = w; _texH[id] = h; BumpTexRev(id);
		}

		/// <summary>
		/// Register a texture for the rasterizer from an existing <see cref="LuaTexture"/> by
		/// reading its pixels back from the GPU (alpha is dropped — the rasterizer treats textures
		/// as opaque). One-off cost; call at load time, not per frame.
		/// </summary>
		public void RegisterTextureFromImage(int id, LuaTexture tex) {
			if (tex?._texture == null) return;
			byte[]? rgba = tex.GetCachedPixels(out int w, out int h);
			if (rgba == null || w <= 0 || h <= 0) return;
			var px = new int[w * h];
			for (int i = 0; i < px.Length; i++) {
				int o = i * 4;
				px[i] = (rgba[o] << 16) | (rgba[o + 1] << 8) | rgba[o + 2];
			}
			_texPix[id] = px; _texW[id] = w; _texH[id] = h; BumpTexRev(id);
		}

		/// <summary>Register a texture from a raw packed-RGB int[] (length w*h). For engine-internal
		/// use (e.g. the glTF model loader registering solid material-colour textures).</summary>
		internal void RegisterTexturePixels(int id, int[] px, int w, int h) {
			if (px == null || w <= 0 || h <= 0 || px.Length < w * h) return;
			_texPix[id] = px; _texW[id] = w; _texH[id] = h; BumpTexRev(id);
		}

		/// <summary>2D line in the colour buffer (screen pixels), drawn on top (no depth).</summary>
		public void DrawLine(int x0, int y0, int x1, int y1, int r, int g, int b) {
			if (_gpuOwnsCanvas && _gpuRasterizer != null) { _gpuRasterizer.Line2D(this, x0, y0, x1, y1, r, g, b); return; }   // after GPU 3D: composite onto the canvas texture
			byte br = RenderUtil.CB(r), bg = RenderUtil.CB(g), bb = RenderUtil.CB(b);
			int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
			int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
			int err = dx + dy;
			int minx = _w, miny = _h, maxx = -1, maxy = -1;
			byte[] buf = _canvas._buf;
			while (true) {
				if ((uint)x0 < (uint)_w && (uint)y0 < (uint)_h) {
					int o = (y0 * _w + x0) * 4;
					buf[o] = br; buf[o + 1] = bg; buf[o + 2] = bb; buf[o + 3] = 255;
					if (x0 < minx) minx = x0; if (x0 > maxx) maxx = x0;
					if (y0 < miny) miny = y0; if (y0 > maxy) maxy = y0;
				}
				if (x0 == x1 && y0 == y1) break;
				int e2 = 2 * err;
				if (e2 >= dy) { err += dy; x0 += sx; }
				if (e2 <= dx) { err += dx; y0 += sy; }
			}
			if (maxx >= 0) _canvas.MarkDirty(minx, miny, maxx, maxy);
		}
		#endregion

		#region Scene objects (retained mode)
		// The scene retains a set of objects (groups of primitives). Lua creates/edits/removes
		// them and supplies bounds + an optional facing-normal; Render() then culls, sorts and
		// rasterizes everything natively — so the per-frame Lua↔C# traffic is just the camera
		// update + one Render() call, and all the "calculation" lives in the renderer.
		//
		// Primitive kinds (one per object): 0 = textured quad (stride 16: 12 coords + texId,
		// uMax, vMax, shade), 1 = flat quad (stride 12: 12 coords; colour from the object),
		// 2 = textured triangle (stride 17: 3×(xyz+uv) + texId + shade) — for models.
		// SceneObject lives in SceneData.cs (shared by both renderers).

		internal readonly Dictionary<int, SceneObject> _objects = new();
		private int _nextObjId = 1;
		/// <summary>Bumped only when object GEOMETRY (verts / transforms / add / delete) changes — NOT on
		/// camera/tint/visibility edits. The GPU rasterizer rebuilds its retained vertex buffer only when
		/// this changes, so a static scene costs nothing to re-upload per frame.</summary>
		internal int _geomRevision;

		private SceneObject? Obj(int id) => _objects.TryGetValue(id, out var o) ? o : null;
		private static void EnsureCap(SceneObject o, int stride) {
			if ((o.N + 1) * stride > o.D.Length)
				Array.Resize(ref o.D, o.D.Length == 0 ? stride * 64 : o.D.Length * 2);
		}

		/// <summary>Create an empty object; returns its id. Visible by default, opaque pass.</summary>
		public int NewObject() { int id = _nextObjId++; _objects[id] = new SceneObject(); Revision++; _geomRevision++; return id; }
		/// <summary>Remove an object and its geometry.</summary>
		public void DeleteObject(int id) { _objects.Remove(id); Revision++; _geomRevision++; }
		/// <summary>Remove every object and particle system (e.g. when restarting a level). Textures,
		/// lights and camera are kept. Object ids restart from 1.</summary>
		public void ClearObjects() { _objects.Clear(); _nextObjId = 1; _particleSystems.Clear(); Revision++; _geomRevision++; }
		/// <summary>Clear an object's primitives so it can be refilled (kind resets).</summary>
		public void ObjBegin(int id) { var o = Obj(id); if (o != null) { o.N = 0; o.Kind = -1; o.GeomVersion++; Revision++; _geomRevision++; } }
		public void ObjSetVisible(int id, bool v) { var o = Obj(id); if (o != null) { o.Visible = v; Revision++; } }
		/// <summary>Per-channel colour multiply for an object's textured surfaces (1,1,1 = none).
		/// Cheap way to flash/tint a model (e.g. a hurt enemy red). Applies in the unlit path.</summary>
		public void ObjSetTint(int id, double r, double g, double b) { var o = Obj(id); if (o != null) { o.TintR = r; o.TintG = g; o.TintB = b; } }
		/// <summary>Mark an object as an overlay: drawn last, over a freshly-cleared depth buffer, so it
		/// always renders on top of the world (first-person weapon viewmodels).</summary>
		public void ObjSetOverlay(int id, bool on) { var o = Obj(id); if (o != null) { o.Overlay = on; Revision++; } }
		/// <summary>Sample this object's texture by screen pixel instead of UV. With a texture produced
		/// by <see cref="RenderView"/> (same resolution as the scene), this makes a flat quad show that
		/// rendered view aligned to the screen — used for planar mirrors and portal surfaces.</summary>
		public void ObjSetScreenTex(int id, bool on) { var o = Obj(id); if (o != null) { o.ScreenTex = on; Revision++; } }
		/// <summary>Axis-aligned bounds used for frustum (behind-camera) culling.</summary>
		public void ObjSetBounds(int id, double minX, double minY, double minZ, double maxX, double maxY, double maxZ) {
			var o = Obj(id); if (o == null) return;
			o.MinX = minX; o.MinY = minY; o.MinZ = minZ; o.MaxX = maxX; o.MaxY = maxY; o.MaxZ = maxZ; o.HasBounds = true;
			o.CenX = (minX + maxX) * 0.5; o.CenY = (minY + maxY) * 0.5; o.CenZ = (minZ + maxZ) * 0.5;
			double rx = (maxX - minX) * 0.5, ry = (maxY - minY) * 0.5, rz = (maxZ - minZ) * 0.5;
			o.Radius = Math.Sqrt(rx * rx + ry * ry + rz * rz);   // once here, not per-frame in cull
		}
		/// <summary>Mark this object as a planar group facing (nx,ny,nz); it's skipped when the
		/// camera is on its back side (axis-aligned back-face culling). Pass 0,0,0 to disable.</summary>
		public void ObjSetNormal(int id, double nx, double ny, double nz) {
			var o = Obj(id); if (o == null) return;
			o.Nx = nx; o.Ny = ny; o.Nz = nz; o.HasNormal = (nx != 0 || ny != 0 || nz != 0);
		}
		/// <summary>pass 0 = opaque (drawn front→back), 1 = transparent (back→front). r,g,b,a is the
		/// colour used by flat-quad objects.</summary>
		public void ObjSetPass(int id, int pass, double r, double g, double b, int a) {
			var o = Obj(id); if (o == null) return;
			o.Pass = pass; o.R = r; o.G = g; o.B = b; o.A = a;
		}
		/// <summary>Raytracer material id for this object (rasterizer ignores it). -1 = default.</summary>
		public void ObjSetMaterial(int id, int matId) { var o = Obj(id); if (o != null) { o.Material = matId; Revision++; } }
		/// <summary>Optional model matrix (Lua table of 16 numbers, row-major). nil/empty = identity.</summary>
		public void ObjSetTransform(int id, LuaTable m) {
			var o = Obj(id); if (o == null) return;
			if (m == null) { o.Transform = null; Revision++; return; }
			var t = new double[16];
			for (int i = 0; i < 16; i++) {
				object v = m[i + 1];
				t[i] = v switch { double d => d, long l => l, int ii => ii, _ => (i % 5 == 0 ? 1.0 : 0.0) };
			}
			o.Transform = t; Revision++;
		}
		public void ObjClearTransform(int id) { var o = Obj(id); if (o != null) { o.Transform = null; Revision++; } }

		/// <summary>Fast per-frame transform setter: 16 row-major doubles, no Lua-table marshaling and
		/// no per-call allocation (reuses the object's matrix array). Use this on the hot path instead
		/// of <see cref="ObjSetTransform"/>, which boxes a LuaTable and allocates every call.</summary>
		public void ObjSetTransform16(int id,
			double m0, double m1, double m2, double m3, double m4, double m5, double m6, double m7,
			double m8, double m9, double m10, double m11, double m12, double m13, double m14, double m15) {
			var o = Obj(id); if (o == null) return;
			var t = o.Transform; if (t == null || t.Length != 16) { t = new double[16]; o.Transform = t; }
			t[0] = m0; t[1] = m1; t[2] = m2; t[3] = m3; t[4] = m4; t[5] = m5; t[6] = m6; t[7] = m7;
			t[8] = m8; t[9] = m9; t[10] = m10; t[11] = m11; t[12] = m12; t[13] = m13; t[14] = m14; t[15] = m15;
			Revision++;
		}


		public void ObjAddQuadTex(int id,
			double x0, double y0, double z0, double x1, double y1, double z1,
			double x2, double y2, double z2, double x3, double y3, double z3,
			int texId, double uMax, double vMax, double shade) {
			var o = Obj(id); if (o == null) return;
			o.Kind = 0; EnsureCap(o, 16);
			int k = o.N * 16; var d = o.D;
			d[k] = x0; d[k+1] = y0; d[k+2] = z0; d[k+3] = x1; d[k+4] = y1; d[k+5] = z1;
			d[k+6] = x2; d[k+7] = y2; d[k+8] = z2; d[k+9] = x3; d[k+10] = y3; d[k+11] = z3;
			d[k+12] = texId; d[k+13] = uMax; d[k+14] = vMax; d[k+15] = shade;
			o.N++;
		}
		public void ObjAddQuadFlat(int id,
			double x0, double y0, double z0, double x1, double y1, double z1,
			double x2, double y2, double z2, double x3, double y3, double z3) {
			var o = Obj(id); if (o == null) return;
			o.Kind = 1; EnsureCap(o, 12);
			int k = o.N * 12; var d = o.D;
			d[k] = x0; d[k+1] = y0; d[k+2] = z0; d[k+3] = x1; d[k+4] = y1; d[k+5] = z1;
			d[k+6] = x2; d[k+7] = y2; d[k+8] = z2; d[k+9] = x3; d[k+10] = y3; d[k+11] = z3;
			o.N++;
		}
		public void ObjAddTriTex(int id,
			double x0, double y0, double z0, double u0, double v0,
			double x1, double y1, double z1, double u1, double v1,
			double x2, double y2, double z2, double u2, double v2,
			int texId, double shade) {
			var o = Obj(id); if (o == null) return;
			o.Kind = 2; EnsureCap(o, 17);
			int k = o.N * 17; var d = o.D;
			d[k]=x0; d[k+1]=y0; d[k+2]=z0; d[k+3]=u0; d[k+4]=v0;
			d[k+5]=x1; d[k+6]=y1; d[k+7]=z1; d[k+8]=u1; d[k+9]=v1;
			d[k+10]=x2; d[k+11]=y2; d[k+12]=z2; d[k+13]=u2; d[k+14]=v2;
			d[k+15]=texId; d[k+16]=shade;
			o.N++;
		}

		/// <summary>Add a billboarded 2D sprite (2D-in-3D) to an object: an alpha-textured quad of
		/// world size <paramref name="w"/>×<paramref name="h"/> standing with its foot at (cx,cy,cz),
		/// drawn from the sprite registered as <paramref name="spriteId"/> (RGBA — e.g. via
		/// RegisterSpriteFromTexture or MakeSoftCircle). <paramref name="billboard"/>: 0 = fixed (XY
		/// plane), 1 = Y-axis (faces the camera but stays upright — the usual 2D-in-3D character/tree),
		/// 2 = full (always faces the camera). <paramref name="cutout"/>!=0 = alpha-test + depth write
		/// (crisp edges that occlude, put the object in the opaque pass); 0 = alpha blend (transparent
		/// pass). The object's tint (ObjSetTint) multiplies the sprite; its alpha (ObjSetPass a) fades it.</summary>
		public void ObjAddSprite(int id, double cx, double cy, double cz, double w, double h, int spriteId, int billboard, int cutout) {
			var o = Obj(id); if (o == null) return;
			o.Kind = 3; EnsureCap(o, 8);
			int k = o.N * 8; var d = o.D;
			d[k]=cx; d[k+1]=cy; d[k+2]=cz; d[k+3]=w; d[k+4]=h; d[k+5]=spriteId; d[k+6]=billboard; d[k+7]=cutout;
			o.N++; Revision++;
		}

		/// <summary>Add a sprite drawn on an EXPLICIT world quad given by its four corners (winding
		/// 0→1→2→3, UVs (0,1)(1,1)(1,0)(0,0)). Unlike ObjAddSprite this does NOT billboard — you place the
		/// corners yourself, e.g. dropped onto the terrain so a character's silhouette shadow DRAPES over
		/// slopes/stairs. Sampled + tinted exactly like a sprite (object tint multiplies; alpha fades; cutout
		/// alpha-tests). An object holding these must hold ONLY sprite-quads (its own kind).</summary>
		public void ObjAddSpriteQuad(int id, double x0, double y0, double z0, double x1, double y1, double z1,
			double x2, double y2, double z2, double x3, double y3, double z3, int spriteId, int cutout) {
			var o = Obj(id); if (o == null) return;
			o.Kind = 4; EnsureCap(o, 14);
			int k = o.N * 14; var d = o.D;
			d[k]=x0; d[k+1]=y0; d[k+2]=z0; d[k+3]=x1; d[k+4]=y1; d[k+5]=z1;
			d[k+6]=x2; d[k+7]=y2; d[k+8]=z2; d[k+9]=x3; d[k+10]=y3; d[k+11]=z3;
			d[k+12]=spriteId; d[k+13]=cutout;
			o.N++; Revision++;
		}

		/// <summary>Clear depth (raster) / reset accumulation (raytrace) as needed, then render
		/// every visible object with the active renderer. Does not touch the colour buffer's
		/// background (draw your sky/clear first) and does not Upload (call Upload after).</summary>
		public void Render() {
			try {
				_renderer.Render(this);
				// CPU path: the toon outline runs as a canvas post-op (GPU scenes ink it in-shader)
				if (_outlineTh > 0 && !_gpuOwnsCanvas) ApplyOutline(_outlineR, _outlineG, _outlineB, (int)Math.Round(_outlineTh));
			} catch (Exception e) {
				// A GPU-renderer failure (e.g. a shader that won't compile on this driver) must not take the
				// whole stage down: fall back to the CPU rasterizer for the rest of the session and carry on.
				if (_renderer != _rasterizer) {
					System.Diagnostics.Trace.TraceError("Lua3DScene: GPU renderer failed, falling back to the CPU rasterizer.\n" + e);
					_renderer = _rasterizer;
					try { _renderer.Render(this); } catch { }
				}
			}
		}
		#endregion

		#region Raytracer scene model (materials / lights / primitives)
		// Used only by the Raytracer; the Rasterizer ignores all of this. Lua builds materials,
		// point lights and analytic / SDF primitives, and tags objects with a material id.
		// Every edit bumps Revision so the path tracer resets its accumulation.

		internal readonly List<SceneMaterial> _materials = new();
		internal readonly List<SceneLight> _lights = new();
		internal readonly List<ScenePrimitive> _primitives = new();

		// Background / sky: a vertical gradient (top → bottom) that also serves as ambient light
		// for the path tracer when a ray escapes. Defaults to a soft daylight sky.
		internal double _skyTR = 0.55, _skyTG = 0.70, _skyTB = 1.00;
		internal double _skyBR = 0.85, _skyBG = 0.88, _skyBB = 0.95;
		internal double _skyStrength = 1.0;

		/// <summary>Sky/ambient gradient (linear 0-1 rgb) from horizon-up <paramref name="t*"/> to
		/// horizon-down <paramref name="b*"/>, scaled by <paramref name="strength"/>. Lights the
		/// path tracer when rays escape; set strength 0 for a black studio.</summary>
		public void SetSky(double tr, double tg, double tb, double br, double bg, double bb, double strength) {
			_skyTR = tr; _skyTG = tg; _skyTB = tb; _skyBR = br; _skyBG = bg; _skyBB = bb;
			_skyStrength = strength < 0 ? 0 : strength; Revision++;
		}

		// ── Materials ───────────────────────────────────────────────────────────────────
		/// <summary>Create a material (default: diffuse mid-grey); returns its id.</summary>
		public int NewMaterial() { _materials.Add(new SceneMaterial()); Revision++; return _materials.Count - 1; }
		private SceneMaterial? Mat(int id) => (id >= 0 && id < _materials.Count) ? _materials[id] : null;
		/// <summary>"diffuse" | "metal" | "glass" | "emissive".</summary>
		public void MatSetType(int id, string type) {
			var m = Mat(id); if (m == null) return;
			m.Type = type switch { "metal" => 1, "glass" => 2, "emissive" => 3, _ => 0 }; Revision++;
		}
		/// <summary>Albedo / base colour (linear 0-1). For metal this tints the reflection.</summary>
		public void MatSetAlbedo(int id, double r, double g, double b) { var m = Mat(id); if (m == null) return; m.R = r; m.G = g; m.B = b; Revision++; }
		/// <summary>0 = sharp mirror / clear glass, 1 = fully rough.</summary>
		public void MatSetRoughness(int id, double rough) { var m = Mat(id); if (m == null) return; m.Rough = rough < 0 ? 0 : (rough > 1 ? 1 : rough); Revision++; }
		/// <summary>Glass index of refraction (e.g. 1.5 for crown glass, 1.33 water, 2.4 diamond).</summary>
		public void MatSetIOR(int id, double ior) { var m = Mat(id); if (m == null) return; m.Ior = ior < 1 ? 1 : ior; Revision++; }
		/// <summary>Emission colour (linear 0-1) × strength. Lights the scene via GI.</summary>
		public void MatSetEmission(int id, double r, double g, double b, double strength) {
			var m = Mat(id); if (m == null) return; m.ER = r * strength; m.EG = g * strength; m.EB = b * strength; Revision++;
		}
		/// <summary>Optional albedo texture (a RegisterTexture id), sampled by uv. -1 = none.</summary>
		public void MatSetTexture(int id, int texId) { var m = Mat(id); if (m == null) return; m.TexId = texId; Revision++; }
		/// <summary>Procedural normal-map preset: "none" | "wood" | "perlin" | "waves".</summary>
		public void MatSetNormalMap(int id, string preset) {
			var m = Mat(id); if (m == null) return;
			m.NormalMap = preset switch { "wood" => 1, "perlin" => 2, "waves" => 3, _ => 0 }; Revision++;
		}
		/// <summary>Tangent-space normal-map texture (a RegisterTexture id, RGB = encoded normal).
		/// Applies to textured geometry (triangles/quads with UVs); overrides the procedural preset.
		/// Pass -1 to clear.</summary>
		public void MatSetNormalMapTexture(int id, int texId) { var m = Mat(id); if (m == null) return; m.NormalTex = texId; Revision++; }

		// ── Lights ──────────────────────────────────────────────────────────────────────
		/// <summary>Add a point light at (x,y,z), colour (r,g,b) linear 0-1 × intensity. The
		/// raytracer uses inverse-square falloff; the rasterizer (if lit) too.</summary>
		public void AddLight(double x, double y, double z, double r, double g, double b, double intensity) {
			_lights.Add(new SceneLight { X = x, Y = y, Z = z, R = r * intensity, G = g * intensity, B = b * intensity }); Revision++;
		}
		/// <summary>Like <see cref="AddLight"/> but with a finite falloff radius (light reaches zero
		/// at <paramref name="range"/>) — used by the rasterizer's forward lighting (e.g. voxel
		/// torches). The raytracer ignores the range and uses inverse-square.</summary>
		public void AddLightRanged(double x, double y, double z, double r, double g, double b, double intensity, double range) {
			_lights.Add(new SceneLight { X = x, Y = y, Z = z, R = r * intensity, G = g * intensity, B = b * intensity, Range = range }); Revision++;
		}
		public void ClearLights() { _lights.Clear(); Revision++; }

		// ── Particle systems ──────────────────────────────────────────────────────────────
		// A reusable billboard particle pool. Lua creates a system, emits bursts, and calls
		// PsUpdate(dt) each frame; live particles render automatically in the transparent pass.
		internal readonly List<ParticleSystem> _particleSystems = new();
		private readonly Random _prng = new();

		/// <summary>Create a particle system and return its handle (index).</summary>
		public int NewParticleSystem() { _particleSystems.Add(new ParticleSystem()); return _particleSystems.Count - 1; }
		/// <summary>Remove all live particles from a system.</summary>
		public void PsClear(int id) { if (id >= 0 && id < _particleSystems.Count) _particleSystems[id].Clear(); }
		/// <summary>Advance a system's simulation by dt seconds (gravity, drag, motion, ageing).</summary>
		public void PsUpdate(int id, double dt) { if (id >= 0 && id < _particleSystems.Count) _particleSystems[id].Update(dt); }
		/// <summary>Live particle count (e.g. to know when an effect has finished).</summary>
		public int PsCount(int id) => (id >= 0 && id < _particleSystems.Count) ? _particleSystems[id].Count : 0;
		/// <summary>Max live particles a system will hold (default 4000). Raise it for heavy showcases;
		/// extra emits beyond the cap are dropped.</summary>
		public void PsSetCap(int id, int cap) { if (id >= 0 && id < _particleSystems.Count) _particleSystems[id].Cap = cap < 0 ? 0 : cap; }

		/// <summary>Emit one particle. Colour r,g,b are 0-255; a0 is start opacity 0-1; size0→size1
		/// is the billboard size (world units) over its life; additive!=0 glows (else alpha-blends).</summary>
		public void PsEmit(int id, double x, double y, double z, double vx, double vy, double vz,
			double r, double g, double b, double a0, double size0, double size1, double life,
			double gravity, double drag, int additive) {
			if (id < 0 || id >= _particleSystems.Count) return;
			var psy = _particleSystems[id];
			psy.Add(new Particle {
				X = x, Y = y, Z = z, VX = vx, VY = vy, VZ = vz,
				R = r, G = g, B = b, A0 = a0, Size0 = size0, Size1 = size1,
				Life = life, MaxLife = life, Gravity = gravity, Drag = drag, Additive = additive != 0,
				Sprite = psy.CurSprite,
			});
		}

		/// <summary>Emit <paramref name="count"/> particles from (x,y,z) in random directions (biased
		/// toward dir if its length &gt; 0) at up to <paramref name="speed"/>, with colour jitter and
		/// per-particle life variation. A one-call helper for impacts / puffs / muzzle flashes.</summary>
		public void PsBurst(int id, double x, double y, double z, int count,
			double dirX, double dirY, double dirZ, double spread, double speed,
			double r, double g, double b, double colorJitter, double a0, double size0, double size1,
			double life, double lifeVar, double gravity, double drag, int additive) {
			if (id < 0 || id >= _particleSystems.Count) return;
			var ps = _particleSystems[id];
			double dl = Math.Sqrt(dirX * dirX + dirY * dirY + dirZ * dirZ);
			bool hasDir = dl > 1e-6;
			if (hasDir) { dirX /= dl; dirY /= dl; dirZ /= dl; }
			for (int i = 0; i < count; i++) {
				// random unit direction, then blend toward dir by (1-spread)
				double ux, uy, uz;
				do { ux = _prng.NextDouble() * 2 - 1; uy = _prng.NextDouble() * 2 - 1; uz = _prng.NextDouble() * 2 - 1; }
				while (ux * ux + uy * uy + uz * uz > 1.0 || ux * ux + uy * uy + uz * uz < 1e-6);
				double ul = Math.Sqrt(ux * ux + uy * uy + uz * uz); ux /= ul; uy /= ul; uz /= ul;
				if (hasDir) {
					ux = dirX + (ux - dirX) * spread; uy = dirY + (uy - dirY) * spread; uz = dirZ + (uz - dirZ) * spread;
					double bl = Math.Sqrt(ux * ux + uy * uy + uz * uz); if (bl > 1e-6) { ux /= bl; uy /= bl; uz /= bl; }
				}
				double sp = speed * (0.4 + 0.6 * _prng.NextDouble());
				double jit = (_prng.NextDouble() * 2 - 1) * colorJitter;
				double lifeF = life * (1.0 - lifeVar + 2 * lifeVar * _prng.NextDouble());
				ps.Add(new Particle {
					X = x, Y = y, Z = z, VX = ux * sp, VY = uy * sp, VZ = uz * sp,
					R = r + jit, G = g + jit, B = b + jit, A0 = a0,
					Size0 = size0, Size1 = size1, Life = lifeF, MaxLife = lifeF,
					Gravity = gravity, Drag = drag, Additive = additive != 0,
					Sprite = ps.CurSprite,
				});
			}
			Revision++;
		}

		// ── particle sprites (alpha-textured billboards) ─────────────────────────────────
		// A sprite is an ARGB image (a<<24 | r<<16 | g<<8 | b). Particles whose Sprite>=0 billboard the
		// sprite — its alpha (× the particle's opacity) shapes a soft circle / crystal / glow / any
		// Lua-supplied image, instead of a flat square. The particle's colour tints the sprite.
		internal readonly Dictionary<int, int[]> _spritePix = new();
		internal readonly Dictionary<int, int> _spriteW = new();
		internal readonly Dictionary<int, int> _spriteH = new();

		/// <summary>Stamp this sprite onto particles emitted next into the system (-1 = flat square).</summary>
		public void PsSetSprite(int id, int spriteId) { if (id >= 0 && id < _particleSystems.Count) _particleSystems[id].CurSprite = spriteId; }

		/// <summary>Register a particle sprite from a flat ARGB array (length w*h, top-left origin).</summary>
		public void RegisterSprite(int spriteId, int[] argb, int w, int h) {
			if (argb == null || w <= 0 || h <= 0) return;
			_spritePix[spriteId] = argb; _spriteW[spriteId] = w; _spriteH[spriteId] = h; Revision++;
		}

		/// <summary>Register a particle sprite from a Lua array of ARGB ints (1-based, length w*h).</summary>
		public void SetSpriteRGBA(int spriteId, LuaTable px, int w, int h) {
			if (px == null || w <= 0 || h <= 0) return;
			var a = new int[w * h];
			for (int i = 0; i < a.Length; i++) { object v = px[i + 1]; a[i] = v is long l ? (int)l : (v is double d ? (int)d : 0); }
			RegisterSprite(spriteId, a, w, h);
		}

		/// <summary>Capture a LuaTexture's pixels as a particle sprite, so any drawn/loaded image can be
		/// used as a particle (main-thread; one-off at load).</summary>
		public void RegisterSpriteFromTexture(int spriteId, LuaTexture tex) {
			if (tex == null) return;
			var rgba = tex.GetCachedPixels(out int w, out int h);
			if (rgba == null || w <= 0 || h <= 0) return;
			var a = new int[w * h];
			for (int i = 0; i < a.Length; i++) { int o = i * 4; a[i] = (rgba[o + 3] << 24) | (rgba[o] << 16) | (rgba[o + 1] << 8) | rgba[o + 2]; }
			RegisterSprite(spriteId, a, w, h);
		}

		/// <summary>Generate a soft round particle: white, opaque at the centre fading to 0 at the edge.
		/// <paramref name="edge"/> shapes the falloff (1≈linear, 2-3 = soft glow). res ~32-64 is plenty.</summary>
		public void MakeSoftCircle(int spriteId, int res, double edge) {
			if (res < 2) res = 2; if (edge <= 0) edge = 1;
			var a = new int[res * res]; double c = (res - 1) * 0.5;
			for (int y = 0; y < res; y++) for (int x = 0; x < res; x++) {
				double dx = (x - c) / c, dy = (y - c) / c; double d = Math.Sqrt(dx * dx + dy * dy);
				double f = d >= 1 ? 0 : Math.Pow(1 - d, edge);
				int al = (int)(f * 255 + 0.5); if (al < 0) al = 0; else if (al > 255) al = 255;
				a[y * res + x] = (al << 24) | 0xFFFFFF;
			}
			RegisterSprite(spriteId, a, res, res);
		}

		/// <summary>Generate a six-armed ice-crystal/snowflake sprite (white-blue) for snow/frost.</summary>
		public void MakeCrystal(int spriteId, int res) {
			if (res < 4) res = 4;
			var a = new int[res * res]; double c = (res - 1) * 0.5;
			for (int y = 0; y < res; y++) for (int x = 0; x < res; x++) {
				double dx = (x - c) / c, dy = (y - c) / c; double r = Math.Sqrt(dx * dx + dy * dy);
				double ang = Math.Atan2(dy, dx);
				double arm = Math.Pow(Math.Abs(Math.Cos(3 * ang)), 8);   // 6 sharp arms
				double core = Math.Exp(-(r * r) / 0.02);                 // small bright centre
				double val = Math.Max(core, arm * (r < 1 ? 1 - r : 0));
				int al = (int)(val * 255 + 0.5); if (al < 0) al = 0; else if (al > 255) al = 255;
				a[y * res + x] = (al << 24) | 0x00E6F0FF;                // tint white-blue (230,240,255)
			}
			RegisterSprite(spriteId, a, res, res);
		}

		// ── Rasterizer forward lighting ───────────────────────────────────────────────────
		internal bool _useLights;     // master switch for the rasterizer's forward lighting
		// Directional sun + global ambient (cheap, per-face): lit pixel = texel * (ambient +
		// shade * sunColour * max(0, N·sunDir) + Σ point lights). `shade` is the per-face sky
		// exposure (gates the sun so it doesn't leak into caves); point lights add on top.
		internal double _ambR, _ambG, _ambB;
		internal double _sunX, _sunY = 1, _sunZ, _sunR, _sunG, _sunB;   // sunDir points TO the sun

		/// <summary>Enable/disable the rasterizer's forward lighting (off by default; the raytracer
		/// has its own lighting). When on, faces are lit by the ambient + sun + point lights.</summary>
		public void SetLighting(bool on) { _useLights = on; }
		internal bool _shadows;
		internal bool _shadowFocusSet; internal double _sfx, _sfy, _sfz;   // where to centre the shadow map (the player)
		/// <summary>Enable sun shadow MAPPING on the GPU rasterizer: a depth pass from the sun (terrain +
		/// objects + the sun-facing character silhouettes) sampled in a SEPARATE lit-shader VARIANT. Needs
		/// SetLighting(true) + a non-zero SetSun. Perf-safe — only shadow-enabled scenes compile/run the shadow
		/// path, so other stages are byte-identical to no-shadows. Degrades to no-shadows if unsupported.</summary>
		public void SetShadows(bool on) { _shadows = on; }
		/// <summary>Centre the sun shadow map on this world point (normally the player) each frame, so the
		/// limited-coverage depth map always covers the action. Without it the map is centred in front of the
		/// camera, which misses the play area when the camera is pulled far back (the faked-ortho iso rig).</summary>
		public void SetShadowFocus(double x, double y, double z) { _shadowFocusSet = true; _sfx = x; _sfy = y; _sfz = z; }
		/// <summary>Global ambient light (linear, added to every lit face so nothing is pure black).</summary>
		public void SetAmbient(double r, double g, double b) { _ambR = r; _ambG = g; _ambB = b; }
		/// <summary>Directional "sun": (dx,dy,dz) is the direction TO the sun (auto-normalised),
		/// colour (r,g,b) × brightness. Cheap — one dot product per face, gated by each face's sky
		/// exposure. Animate it for a day/night cycle. Set colour to 0 to disable.</summary>
		public void SetSun(double dx, double dy, double dz, double r, double g, double b) {
			double l = Math.Sqrt(dx * dx + dy * dy + dz * dz); if (l > 1e-9) { dx /= l; dy /= l; dz /= l; }
			_sunX = dx; _sunY = dy; _sunZ = dz; _sunR = r; _sunG = g; _sunB = b;
		}
		/// <summary>Exempt an object from forward lighting (renders at its baked shade only) — e.g.
		/// the voxel torch posts, which are the light sources themselves.</summary>
		public void ObjSetLit(int id, bool lit) { var o = Obj(id); if (o != null) o.Lit = lit; }
		/// <summary>Whether this object's billboard sprites cast into the sun shadow map (default true). Set false
		/// for UI markers / talk bubbles / floating arrows that shouldn't drop a shadow. (GPU rasterizer only.)</summary>
		public void ObjSetCastShadow(int id, bool on) { var o = Obj(id); if (o != null) o.CastShadow = on; }
		/// <summary>Per-object GPU depth bias (polygon offset). Negative pulls toward the camera (a decal/marking
		/// drawn cleanly ON TOP of a coplanar surface), positive pushes away. 0 = off. Fixes z-fighting between
		/// intentionally-coplanar layers (road stripes on the road, ground shadows, etc.). (GPU rasterizer only.)</summary>
		public void ObjSetDepthBias(int id, double bias) { var o = Obj(id); if (o != null) o.DepthBias = (float)bias; }

		// ── Primitives (analytic + SDF) ───────────────────────────────────────────────────
		/// <summary>Sphere centred (cx,cy,cz) radius r with material <paramref name="mat"/>.</summary>
		public int AddSphere(double cx, double cy, double cz, double r, int mat) {
			_primitives.Add(new ScenePrimitive { Kind = 0, X = cx, Y = cy, Z = cz, A = r, Material = mat }); Revision++; return _primitives.Count - 1;
		}
		/// <summary>Infinite plane through (px,py,pz) with normal (nx,ny,nz) (auto-normalised).</summary>
		public int AddPlane(double px, double py, double pz, double nx, double ny, double nz, int mat) {
			double l = Math.Sqrt(nx * nx + ny * ny + nz * nz); if (l > 1e-12) { nx /= l; ny /= l; nz /= l; }
			_primitives.Add(new ScenePrimitive { Kind = 1, X = px, Y = py, Z = pz, A = nx, B = ny, C = nz, Material = mat }); Revision++; return _primitives.Count - 1;
		}
		/// <summary>Axis-aligned box from (minx,miny,minz) to (maxx,maxy,maxz).</summary>
		public int AddBox(double minx, double miny, double minz, double maxx, double maxy, double maxz, int mat) {
			_primitives.Add(new ScenePrimitive {
				Kind = 2, MinX = minx, MinY = miny, MinZ = minz, MaxX = maxx, MaxY = maxy, MaxZ = maxz,
				X = (minx + maxx) * 0.5, Y = (miny + maxy) * 0.5, Z = (minz + maxz) * 0.5, Material = mat
			}); Revision++; return _primitives.Count - 1;
		}
		/// <summary>Torus at (cx,cy,cz): major radius R, minor radius r, axis 0=X,1=Y,2=Z.</summary>
		public int AddTorus(double cx, double cy, double cz, double R, double r, int axis, int mat) {
			_primitives.Add(new ScenePrimitive { Kind = 3, X = cx, Y = cy, Z = cz, A = R, B = r, C = axis, Material = mat }); Revision++; return _primitives.Count - 1;
		}
		/// <summary>Ray-marched SDF preset: "sphere" | "roundbox" | "torus" | "capsule" | "gyroid"
		/// | "octahedron" | "gem"/"diamond" (round brilliant cut, axis +z) at (cx,cy,cz) with
		/// per-axis scale (sx,sy,sz).</summary>
		public int AddSDF(string preset, double cx, double cy, double cz, double sx, double sy, double sz, int mat) {
			int p = preset switch { "roundbox" => 1, "torus" => 2, "capsule" => 3, "gyroid" => 4, "octahedron" => 5, "gem" => 6, "diamond" => 6, _ => 0 };
			_primitives.Add(new ScenePrimitive { Kind = 4, X = cx, Y = cy, Z = cz, A = sx, B = sy, C = sz, SdfPreset = p, Material = mat }); Revision++; return _primitives.Count - 1;
		}
		public void ClearPrimitives() { _primitives.Clear(); Revision++; }
		/// <summary>Reset the whole raytracer model (materials, lights, primitives) for a scene swap.</summary>
		public void ClearRaytraceModel() { _materials.Clear(); _lights.Clear(); _primitives.Clear(); Revision++; }
		#endregion

		#region Display
		public void Upload() { if (_gpuOwnsCanvas) { _gpuRasterizer?.End2D(); _gpuOwnsCanvas = false; return; } _canvas.Upload(); }
		/// <summary>Silhouette OUTLINE post-pass on the CPU canvas: inks every transparent pixel within
		/// <paramref name="thickness"/> px of the rendered shape, with a ~1px ANTI-ALIASED falloff at the
		/// rim. Implemented as a 3-4 chamfer distance transform (two scans, cost independent of the
		/// thickness — the naive per-ring dilation was a frame-rate killer).</summary>
		public void ApplyOutline(int r, int g, int b, int thickness) {
			byte[] buf = _canvas._buf;
			int w = _w, h = _h, n = w * h;
			if (_outlineDist == null || _outlineDist.Length < n) _outlineDist = new int[n];
			var dist = _outlineDist;
			const int INF = 1 << 28;
			for (int i = 0; i < n; i++) dist[i] = buf[i * 4 + 3] == 255 ? 0 : INF;
			// forward chamfer pass (3 orthogonal / 4 diagonal)
			for (int y = 0; y < h; y++) {
				int row = y * w;
				for (int x = 0; x < w; x++) {
					int i = row + x; int d = dist[i]; if (d == 0) continue;
					if (x > 0 && dist[i - 1] + 3 < d) d = dist[i - 1] + 3;
					if (y > 0) {
						if (dist[i - w] + 3 < d) d = dist[i - w] + 3;
						if (x > 0 && dist[i - w - 1] + 4 < d) d = dist[i - w - 1] + 4;
						if (x < w - 1 && dist[i - w + 1] + 4 < d) d = dist[i - w + 1] + 4;
					}
					dist[i] = d;
				}
			}
			// backward pass
			for (int y = h - 1; y >= 0; y--) {
				int row = y * w;
				for (int x = w - 1; x >= 0; x--) {
					int i = row + x; int d = dist[i]; if (d == 0) continue;
					if (x < w - 1 && dist[i + 1] + 3 < d) d = dist[i + 1] + 3;
					if (y < h - 1) {
						if (dist[i + w] + 3 < d) d = dist[i + w] + 3;
						if (x < w - 1 && dist[i + w + 1] + 4 < d) d = dist[i + w + 1] + 4;
						if (x > 0 && dist[i + w - 1] + 4 < d) d = dist[i + w - 1] + 4;
					}
					dist[i] = d;
				}
			}
			byte br = RenderUtil.CB(r), bg = RenderUtil.CB(g), bb = RenderUtil.CB(b);
			double th3 = thickness * 3.0;                       // chamfer units (3 per pixel)
			for (int i = 0; i < n; i++) {
				int o = i * 4;
				if (buf[o + 3] != 0) continue;
				double d = dist[i];
				if (d > th3 + 3.0) continue;
				double a2 = d <= th3 ? 1.0 : 1.0 - (d - th3) / 3.0;   // soft 1px rim = anti-aliased edge
				if (a2 <= 0) continue;
				buf[o] = br; buf[o + 1] = bg; buf[o + 2] = bb; buf[o + 3] = (byte)(a2 * 255.0);
			}
		}
		private int[] _outlineDist;

		public void Draw(int x, int y) => _canvas.Draw(x, y);
		public void DrawAtAnchor(int x, int y, string anchor) => _canvas.DrawAtAnchor(x, y, anchor);
		/// <summary>Blit the rendered scene scaled/tinted/faded at an anchor — used by 3D CHARACTERS,
		/// whose draw contract (scale/opacity/colour) comes from the 2D chara pipeline.</summary>
		public void DrawScaled(int x, int y, double scaleX, double scaleY, double opacity, double r, double g, double b, string anchor) {
			_canvas.SetScale((float)scaleX, (float)scaleY);
			_canvas.SetOpacity((float)opacity);
			_canvas.SetColor((float)r, (float)g, (float)b);
			_canvas.DrawAtAnchor(x, y, anchor ?? "bottom");
			_canvas.SetScale(1f, 1f); _canvas.SetOpacity(1f); _canvas.SetColor(1f, 1f, 1f);
		}
		public void SetScale(float sx, float sy) => _canvas.SetScale(sx, sy);
		public void SetOpacity(float o) => _canvas.SetOpacity(o);
		public void SetColor(float r, float g, float b) => _canvas.SetColor(r, g, b);
		public uint Pointer => _canvas.Pointer;
		#endregion

		#region Dispose
		private bool _disposedValue;
		protected virtual void Dispose(bool disposing) {
			if (!_disposedValue) {
				_gpuRasterizer?.Dispose();   // free the GPU renderer's GL resources (FBO, VBOs, textures, programs)
				_gpuRaytracer?.Dispose();    // free the GPU path tracer's GL resources (compute program, SSBOs, accum/atlas textures, FBO)
				_canvas?.Dispose();
				_disposeList?.Remove(this);
				_depth = Array.Empty<float>();
				_texPix.Clear(); _texW.Clear(); _texH.Clear();
				_objects.Clear(); _particleSystems.Clear();
				_disposedValue = true;
			}
		}
		public void Dispose() { Dispose(disposing: true); GC.SuppressFinalize(this); }
		#endregion
	}

	public class Lua3DSceneFunc {
		private HashSet<Lua3DScene> Scenes;
		public Lua3DSceneFunc(HashSet<Lua3DScene> scenes) { Scenes = scenes; }
		public Lua3DScene CreateScene(int width, int height) {
			Lua3DScene scene = new(width, height);
			scene._disposeList = Scenes;
			Scenes.Add(scene);
			return scene;
		}
	}
}
