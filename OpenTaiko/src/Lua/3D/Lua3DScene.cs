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

		internal readonly int _w;
		internal readonly int _h;
		internal float[] _depth;

		// Camera, owned by the scene: position + orientation (yaw/pitch → basis) +
		// lens (fov → scale, near plane). Lua feeds these and reads the basis / Project back.
		internal double _camX, _camY, _camZ;
		internal double _Rx = 1, _Ry, _Rz, _Ux, _Uy = 1, _Uz, _Fx, _Fy, _Fz = 1;
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

		internal readonly Dictionary<int, int[]> _texPix = new();
		internal readonly Dictionary<int, int> _texW = new();
		internal readonly Dictionary<int, int> _texH = new();

		// ── Renderers ─────────────────────────────────────────────────────────────────
		// The rasterizer is created up front (the common case); the raytracer is created lazily
		// the first time raytrace mode is selected. _renderer points at the active one.
		private readonly Rasterizer _rasterizer = new();
		private Raytracer? _raytracer;
		private IRenderer _renderer;
		/// <summary>Bumped on every camera / geometry / light / material / primitive edit; the
		/// raytracer compares it to know when to reset its progressive accumulation.</summary>
		internal int Revision;

		public Lua3DScene(int width, int height) {
			_canvas = new LuaCanvas(width, height);
			_w = _canvas._w;
			_h = _canvas._h;
			_depth = new float[_w * _h];
			_scale = (_h * 0.5) / Math.Tan(_fov * 0.5 * Math.PI / 180.0);
			_renderer = _rasterizer;
		}

		public int Width => _w;
		public int Height => _h;

		#region Frame setup / 2D / textures
		public void Clear(int r, int g, int b, int a) => _canvas.Clear(r, g, b, a);
		public void FillRect(int x, int y, int w, int h, int r, int g, int b, int a) => _canvas.FillRect(x, y, w, h, r, g, b, a);
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

		/// <summary>Number of CPU threads the rasterizer uses (screen split into this many
		/// horizontal row bands). 1 = single-threaded.</summary>
		public void SetThreads(int n) {
			_threads = n < 1 ? 1 : n;
			_po = new ParallelOptions { MaxDegreeOfParallelism = _threads };
		}

		/// <summary>Choose the renderer: "raster" (default) or "raytrace"/"rt" (path tracer).
		/// Switching drops any cached/accumulated state.</summary>
		public void SetMode(string mode) {
			IRenderer want = (mode == "raytrace" || mode == "rt") ? (_raytracer ??= new Raytracer()) : _rasterizer;
			if (want != _renderer) { _renderer = want; _renderer.Invalidate(); Revision++; }
		}
		/// <summary>"raster" or "raytrace".</summary>
		public string GetMode() => _renderer == _rasterizer ? "raster" : "raytrace";

		/// <summary>Accumulated samples-per-pixel of the path tracer since its last reset (0 in
		/// raster mode or right after a camera/scene change). Useful for a convergence readout.</summary>
		public int GetSampleCount() => _raytracer?.SampleCount ?? 0;

		public double GetCameraFov() => _fov;
		public double GetCameraNear() => _near;
		public double GetCameraYaw() => _yaw;
		public double GetCameraPitch() => _pitch;
		public (double, double, double) GetCameraPosition() => (_camX, _camY, _camZ);
		public (double, double, double) GetCameraForward() => (_Fx, _Fy, _Fz);
		public (double, double, double) GetCameraRight() => (_Rx, _Ry, _Rz);
		public (double, double, double) GetCameraUp() => (_Ux, _Uy, _Uz);

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
			_texPix[id] = px; _texW[id] = w; _texH[id] = h;
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
			_texPix[id] = px; _texW[id] = w; _texH[id] = h;
		}

		/// <summary>2D line in the colour buffer (screen pixels), drawn on top (no depth).</summary>
		public void DrawLine(int x0, int y0, int x1, int y1, int r, int g, int b) {
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

		private SceneObject? Obj(int id) => _objects.TryGetValue(id, out var o) ? o : null;
		private static void EnsureCap(SceneObject o, int stride) {
			if ((o.N + 1) * stride > o.D.Length)
				Array.Resize(ref o.D, o.D.Length == 0 ? stride * 64 : o.D.Length * 2);
		}

		/// <summary>Create an empty object; returns its id. Visible by default, opaque pass.</summary>
		public int NewObject() { int id = _nextObjId++; _objects[id] = new SceneObject(); Revision++; return id; }
		/// <summary>Remove an object and its geometry.</summary>
		public void DeleteObject(int id) { _objects.Remove(id); Revision++; }
		/// <summary>Clear an object's primitives so it can be refilled (kind resets).</summary>
		public void ObjBegin(int id) { var o = Obj(id); if (o != null) { o.N = 0; o.Kind = -1; Revision++; } }
		public void ObjSetVisible(int id, bool v) { var o = Obj(id); if (o != null) { o.Visible = v; Revision++; } }
		/// <summary>Axis-aligned bounds used for frustum (behind-camera) culling.</summary>
		public void ObjSetBounds(int id, double minX, double minY, double minZ, double maxX, double maxY, double maxZ) {
			var o = Obj(id); if (o == null) return;
			o.MinX = minX; o.MinY = minY; o.MinZ = minZ; o.MaxX = maxX; o.MaxY = maxY; o.MaxZ = maxZ; o.HasBounds = true;
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

		/// <summary>Clear depth (raster) / reset accumulation (raytrace) as needed, then render
		/// every visible object with the active renderer. Does not touch the colour buffer's
		/// background (draw your sky/clear first) and does not Upload (call Upload after).</summary>
		public void Render() => _renderer.Render(this);
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
		/// <summary>Add a point light at (x,y,z), colour (r,g,b) linear 0-1 × intensity.</summary>
		public void AddLight(double x, double y, double z, double r, double g, double b, double intensity) {
			_lights.Add(new SceneLight { X = x, Y = y, Z = z, R = r * intensity, G = g * intensity, B = b * intensity }); Revision++;
		}
		public void ClearLights() { _lights.Clear(); Revision++; }

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
		public void Upload() => _canvas.Upload();
		public void Draw(int x, int y) => _canvas.Draw(x, y);
		public void DrawAtAnchor(int x, int y, string anchor) => _canvas.DrawAtAnchor(x, y, anchor);
		public void SetScale(float sx, float sy) => _canvas.SetScale(sx, sy);
		public void SetOpacity(float o) => _canvas.SetOpacity(o);
		public void SetColor(float r, float g, float b) => _canvas.SetColor(r, g, b);
		public uint Pointer => _canvas.Pointer;
		#endregion

		#region Dispose
		private bool _disposedValue;
		protected virtual void Dispose(bool disposing) {
			if (!_disposedValue) {
				_canvas?.Dispose();
				_disposeList?.Remove(this);
				_depth = Array.Empty<float>();
				_texPix.Clear(); _texW.Clear(); _texH.Clear();
				_objects.Clear();
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
