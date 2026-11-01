using System;
using System.Collections.Generic;

namespace OpenTaiko {
	// Reflection and portal views rendered into textures.
	public partial class Lua3DScene {
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

		public void RenderView(int texId, double cx, double cy, double cz, double yawDeg, double pitchDeg,
			double fovDeg, int clr, int clg, int clb, int flipX) {
			if (_disposedValue) return;
			_reflectTrue.Remove(texId);                       // legacy path reclaims this id's flip convention
			int rw = _w >> _rttShift, rh = _h >> _rttShift;   // reduced render size
			if (rw < 1) rw = 1; if (rh < 1) rh = 1;
			var cam = SaveCamera();
			int rtRev = _rtRevision;                          // the camera round trip leaves the raytraced view as it was

			_w = rw; _h = rh;                                 // projection aspect/scale matches the RTT viewport
			_rttPass = true;
			SetCameraPosition(cx, cy, cz); SetCameraFov(fovDeg); SetCameraAngles(yawDeg, pitchDeg);
			// a planar mirror is a reflected (left-handed) camera: flip the right axis so the image is
			// mirrored. Without this the reflection pans the wrong way as the player turns.
			if (flipX != 0) { _Rx = -_Rx; _Ry = -_Ry; _Rz = -_Rz; }

			// GPU path (opt-in, SetReflectionGpu): render into a dedicated FBO/texture that the mirror
			// surface samples directly — no CPU rasterise, no pixel upload. Falls through to the CPU
			// capture when the GPU renderer isn't active.
			if (_reflectGpu && _renderer is IRenderTargets gpu) {
				try { gpu.RenderToTexture(this, texId, rw, rh, clr, clg, clb); }
				catch (Exception e) { ReflectGpuFailed(e); }   // the CPU capture takes over from the next frame
			} else {
				// Reflections/portals always render with the CPU rasterizer into a small canvas, then upload as
				// texId. The GPU off-screen path (GpuRasterizer.RenderToTexture) had persistent driver issues with
				// >1 live reflection on this hardware (the main view lost geometry + portals failed to capture), so
				// the reliable CPU capture is used regardless of the main renderer — the main view still runs on
				// the GPU; only these small reduced-res reflection re-renders are CPU. Keep them cheap via
				// SetReflectionScale (the FPS stage uses 1/4-res reflections).
				CaptureCpuRtt(texId, rw, rh, clr, clg, clb);
				rtRev++;                                      // texId has new pixels: that change stays
			}
			_rttPass = false;
			RestoreCamera(cam);
			_rtRevision = rtRev;
		}

		// The CPU capture of a reflection pass: the CPU rasterizer renders the current camera into texture id texId's
		// own canvas of rw x rh pixels, cleared to (clr, clg, clb), and its pixels become the id's texture.
		private void CaptureCpuRtt(int texId, int rw, int rh, int clr, int clg, int clb) {
			foreach (var r in _made) if (r is IRenderTargets t) t.DropRenderTarget(texId);   // a GPU target for this id would hide the CPU capture
			var sc = _canvas; var sd = _depth;
			if (!_rttCanvases.TryGetValue(texId, out _rttCanvas) || _rttCanvas._w != rw || _rttCanvas._h != rh) {
				_rttCanvas?.Dispose();   // stale-size canvas for this texId (render-scale change) — free, don't orphan
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

		// ── TRUE planar reflection (correct-by-construction) ─────────────────────────────────────────
		// The legacy RenderView(flipX) only negates the camera RIGHT axis — that is a mirror composed
		// with a 180° roll, which uRttFlip then half-compensates; horizontal planes (water) pan the
		// wrong way. Here the eye AND all three basis vectors are reflected about the plane (det −1),
		// which guarantees pixel-project(reflected cam, mirror(P)) == pixel-project(main cam, P) — the
		// mode-3 same-pixel sampling is then exact. A fragment clip plane discards geometry below the
		// mirror so it can't occlude the reflection. egg's mirrors/portals keep the old RenderView.
		internal bool _clipOn; internal double _clipNx, _clipNy, _clipNz, _clipD;
		internal readonly HashSet<int> _reflectTrue = new();   // texIds captured with the TRUE reflected basis

		/// <summary>Reflect a direction vector about the (unit) plane normal, in place.</summary>
		public static void ReflectVec(double nx, double ny, double nz, ref double x, ref double y, ref double z) {
			double d = 2 * (x * nx + y * ny + z * nz);
			x -= d * nx; y -= d * ny; z -= d * nz;
		}

		/// <summary>Render the scene's TRUE planar reflection about the plane through (px,py,pz) with
		/// normal (nx,ny,nz) into texture <paramref name="texId"/> (reduced res per SetReflectionScale).
		/// GPU off-screen pass when SetReflectionGpu is on; CPU capture fallback otherwise (both clip
		/// geometry below the plane). clr/g/b clears the backdrop (pass the sky horizon).</summary>
		public void RenderViewReflect(int texId, double px, double py, double pz,
			double nx, double ny, double nz, int clr, int clg, int clb) {
			if (_disposedValue) return;
			double nl = Math.Sqrt(nx * nx + ny * ny + nz * nz);
			if (nl < 1e-9) return;
			nx /= nl; ny /= nl; nz /= nl;
			int rw = _w >> _rttShift, rh = _h >> _rttShift;
			if (rw < 1) rw = 1; if (rh < 1) rh = 1;
			var cam = SaveCamera();

			_w = rw; _h = rh;
			_scale = (_h * 0.5) / Math.Tan(_fov * 0.5 * Math.PI / 180.0);
			_rttPass = true;
			double dist = (_camX - px) * nx + (_camY - py) * ny + (_camZ - pz) * nz;
			_camX -= 2 * dist * nx; _camY -= 2 * dist * ny; _camZ -= 2 * dist * nz;
			ReflectVec(nx, ny, nz, ref _Rx, ref _Ry, ref _Rz);
			ReflectVec(nx, ny, nz, ref _Ux, ref _Uy, ref _Uz);
			ReflectVec(nx, ny, nz, ref _Fx, ref _Fy, ref _Fz);
			_clipOn = true; _clipNx = nx; _clipNy = ny; _clipNz = nz;
			_clipD = -(px * nx + py * ny + pz * nz) + 0.02;   // 2 cm skirt below the plane avoids waterline cracks
			_reflectTrue.Add(texId);

			bool gpuDone = false;
			if (_reflectGpu && _renderer is IRenderTargets gpu) {
				try { gpu.RenderToTexture(this, texId, rw, rh, clr, clg, clb); gpuDone = true; }
				catch (Exception e) { ReflectGpuFailed(e); }
			}
			if (!gpuDone) CaptureCpuRtt(texId, rw, rh, clr, clg, clb);   // the CPU rasterizer cuts the geometry at the clip plane

			_clipOn = false;
			_rttPass = false;
			RestoreCamera(cam);
		}

		/// <summary>Per-pixel Schlick fresnel reflectivity for a screen-tex mirror surface (water):
		/// grazing views mirror fully, top-down shows the water colour. <paramref name="f0"/> ≈ 0.04
		/// for water. Negative disables (legacy per-vertex shade reflectivity).</summary>
		public void ObjSetFresnel(int id, double f0) {
			var o = Obj(id); if (o != null) { o.FresnelF0 = f0; }
		}

		/// <summary>Depth tint for a screen-tex water surface: the per-vertex shade slot (0..1) lerps
		/// the under-mirror colour from the object's base colour (shallow) to this deep colour.</summary>
		public void ObjSetWaterColors(int id, double dr, double dg, double db) {
			var o = Obj(id); if (o != null) { o.DeepR = dr; o.DeepG = dg; o.DeepB = db; }
		}

		/// <summary>Animated distortion of a screen-tex (mirror) surface's sampling — water reflection
		/// ripple. <paramref name="amp"/> = max offset in RTT pixels (0 = off), <paramref name="freq"/> =
		/// world-space wave frequency, <paramref name="speed"/> = animation rate (uses SetTime's clock).</summary>
		public void ObjSetScreenTexRipple(int id, double amp, double freq, double speed) {
			var o = Obj(id); if (o != null) { o.RippleAmp = amp < 0 ? 0 : amp; o.RippleFreq = freq; o.RippleSpeed = speed; }
		}

		// ── reflections on the GPU ───────────────────────────────────────────────────────────────
		internal bool _reflectGpu;
		/// <summary>Route <see cref="RenderView"/> re-renders through the GPU off-screen pass instead of
		/// the CPU rasterizer (much faster; each texId gets its own FBO). Off by default — existing
		/// stages keep the proven CPU path; the OWM3d water opts in.</summary>
		public void SetReflectionGpu(bool on) { _reflectGpu = on; }
		private void ReflectGpuFailed(Exception e) {
			_reflectGpu = false;
			System.Diagnostics.Trace.TraceError("Lua3DScene: GPU reflection pass failed, reflections use the CPU capture from now on.\n" + e);
		}
	}
}
