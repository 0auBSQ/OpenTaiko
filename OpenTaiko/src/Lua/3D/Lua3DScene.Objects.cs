using System;
using System.Collections.Generic;
using NLua;

namespace OpenTaiko {
	// Scene objects: the retained primitive groups and their per-object settings.
	public partial class Lua3DScene {
		// The scene retains a set of objects (groups of primitives). Lua creates/edits/removes
		// them and may supply bounds (ObjAdd* keeps automatic ones otherwise) + an optional
		// facing-normal; Render() then culls, sorts and rasterizes everything natively — so the
		// per-frame Lua↔C# traffic is just the camera
		// update + one Render() call, and all the "calculation" lives in the renderer.
		//
		// Primitive kinds (one per object): 0 = textured quad, 1 = flat quad (colour from the object),
		// 2 = textured triangle (models), 3 = billboard sprite, 4 = sprite on a given quad; PrimLayout
		// gives each kind's layout. SceneObject and PrimLayout live in SceneData.cs (shared by every renderer).

		internal readonly Dictionary<int, SceneObject> _objects = new();
		private int _nextObjId = 1;
		/// <summary>Bumped whenever the GPU rasterizer's static merge may change: an object turns into or out of a
		/// merge candidate (<see cref="SceneObject.MergeCandidate"/>), or a candidate's geometry or bounds change,
		/// or it is deleted. The renderer rebuilds its merge only when this moved.</summary>
		internal int _geomRevision;
		// bump the merge revision when an edit turned the object into or out of a merge candidate
		private void MergeFlip(SceneObject o, bool was) { if (o.MergeCandidate != was) _geomRevision++; }

		private SceneObject? Obj(int id) => _objects.TryGetValue(id, out var o) ? o : null;
		/// <summary>Grow the object's primitive array to hold at least one more primitive of this stride.</summary>
		internal static void EnsureCap(SceneObject o, int stride) {
			int need = (o.N + 1) * stride;
			if (need > o.D.Length)
				Array.Resize(ref o.D, Math.Max(need, o.D.Length == 0 ? stride * 64 : o.D.Length * 2));
		}
		/// <summary>The object an ObjAdd* of this primitive kind writes into, or null. An object holds one kind
		/// at a time: an add of another kind is ignored (traced once per object) until ObjBegin clears it.</summary>
		private SceneObject? ObjFor(int id, int kind) {
			if (_disposedValue || !_objects.TryGetValue(id, out var o)) return null;
			if (o.Kind < 0 || o.Kind == kind) return o;
			if (!o.KindRefused) {
				o.KindRefused = true;
				System.Diagnostics.Trace.TraceWarning($"Lua3DScene: object {id} holds kind {o.Kind} primitives, so an add of kind {kind} was ignored; call ObjBegin before refilling it with another kind.");
			}
			return null;
		}

		/// <summary>Create an empty object; returns its id (0 after Dispose). Visible by default, opaque pass.</summary>
		public int NewObject() {
			if (_disposedValue) return 0;
			int id = _nextObjId++; _objects[id] = new SceneObject(); return id;
		}
		/// <summary>Remove an object and its geometry.</summary>
		public void DeleteObject(int id) {
			if (_objects.Remove(id, out var o)) { if (o.MergeCandidate) _geomRevision++; _rtRevision++; }
		}
		/// <summary>Remove every object and particle system (e.g. when restarting a level). Textures,
		/// lights and camera are kept. Object and particle system ids keep counting, so an id kept from
		/// before the clear never names a new object or system. Also drops the renderer's transient
		/// caches (per-object VBOs, merged static buffers, reflection RTT targets) and the CPU-side
		/// reflection canvases — they rebuild on demand, so restart-style callers lose nothing while
		/// map-switch callers stop leaking per-map GPU/CPU buffers.</summary>
		public void ClearObjects() {
			_objects.Clear();
			_psIdBase += _particleSystems.Count; _particleSystems.Clear();
			ClearRenderCaches(); RtEdit(); _geomRevision++;
		}

		/// <summary>TERRAIN SPLAT shading for an object (the terrain floor): the GPU blends four layer
		/// textures per PIXEL by the weight sprite (an ARGB image, R/G/B/A = layer 1..4 — see
		/// LuaSplatmap.RegisterSprite), so painted paths/beaches feather smoothly instead of cutting
		/// at quad edges. worldW/H = the splat's world coverage; uvScale = repeats per world unit
		/// (1/cellSize). splatSpriteId &lt; 0 disables (per-quad dominant texturing, the CPU look).</summary>
		public void ObjSetTerrainSplat(int id, int splatSpriteId, int tex1, int tex2, int tex3, int tex4,
			double worldW, double worldH, double uvScale) {
			var o = Obj(id); if (o == null) return;
			bool was = o.MergeCandidate;
			o.SplatSprite = splatSpriteId;
			o.LayerTex1 = tex1; o.LayerTex2 = tex2; o.LayerTex3 = tex3; o.LayerTex4 = tex4;
			o.SplatW = (float)(worldW <= 0 ? 1 : worldW); o.SplatH = (float)(worldH <= 0 ? 1 : worldH);
			o.LayerUvScale = (float)(uvScale <= 0 ? 1 : uvScale);
			MergeFlip(o, was);
		}

		/// <summary>Emissive add for an object's surfaces: col += base * (r,g,b). Values &gt; 1 push the
		/// pixel over the diorama bloom threshold → glowing windows/lamps/signs. 0,0,0 = off (default;
		/// non-zero drops the object out of the static merge, so keep it for actual emitters).</summary>
		public void ObjSetEmissive(int id, double r, double g, double b) {
			var o = Obj(id); if (o != null) { bool was = o.MergeCandidate; if (o.EmR != r || o.EmG != g || o.EmB != b) RtEdit(); o.EmR = r; o.EmG = g; o.EmB = b; MergeFlip(o, was); }
		}

		/// <summary>X-RAY silhouette for a sprite object: where its billboards are OCCLUDED by world
		/// geometry they re-draw as a faint ghost (depth-greater pass at this alpha, 0..1; 0 = off).
		/// Keeps the player/NPCs readable behind walls, hills and buildings without moving the camera.</summary>
		public void ObjSetXray(int id, double alpha) {
			var o = Obj(id); if (o != null) { o.XrayAlpha = alpha < 0 ? 0 : (alpha > 1 ? 1 : alpha); }
		}

		/// <summary>Clear an object's primitives so it can be refilled (kind resets; automatic bounds restart).</summary>
		public void ObjBegin(int id) {
			var o = Obj(id); if (o == null) return;
			if (o.MergeCandidate) _geomRevision++;
			o.N = 0; o.Kind = -1; o.GeomVersion++; RtEdit();
			if (!o.BoundsSet) o.HasBounds = false;
		}
		public void ObjSetVisible(int id, bool v) { var o = Obj(id); if (o != null) { bool was = o.MergeCandidate; if (o.Visible != v) _rtRevision++; o.Visible = v; MergeFlip(o, was); } }
		/// <summary>Per-channel colour multiply for an object's textured surfaces (1,1,1 = none).
		/// Cheap way to flash/tint a model (e.g. a hurt enemy red).</summary>
		public void ObjSetTint(int id, double r, double g, double b) { var o = Obj(id); if (o != null) { bool was = o.MergeCandidate; if (o.TintR != r || o.TintG != g || o.TintB != b) RtEdit(); o.TintR = r; o.TintG = g; o.TintB = b; MergeFlip(o, was); } }
		/// <summary>Flat albedo OVERRIDE: replace the object's material/texture colour with (r,g,b) before lighting
		/// (it still shades). Pass r&lt;0 to clear. Used to recolour a single routed GLB part (e.g. blue cab → red).</summary>
		public void ObjSetColor(int id, double r, double g, double b) { var o = Obj(id); if (o != null) { bool was = o.MergeCandidate; if (o.FlatR != r || o.FlatG != g || o.FlatB != b) RtEdit(); o.FlatR = r; o.FlatG = g; o.FlatB = b; MergeFlip(o, was); } }
		/// <summary>Mark an object as an overlay: drawn last, over a freshly-cleared depth buffer, so it
		/// always renders on top of the world (first-person weapon viewmodels).</summary>
		public void ObjSetOverlay(int id, bool on) { var o = Obj(id); if (o != null) { bool was = o.MergeCandidate; o.Overlay = on; MergeFlip(o, was); } }
		/// <summary>Sample this object's texture by screen pixel instead of UV. With a texture produced
		/// by <see cref="RenderView"/> (same resolution as the scene), this makes a flat quad show that
		/// rendered view aligned to the screen — used for planar mirrors and portal surfaces.</summary>
		public void ObjSetScreenTex(int id, bool on) { var o = Obj(id); if (o != null) { bool was = o.MergeCandidate; o.ScreenTex = on; MergeFlip(o, was); } }
		/// <summary>Axis-aligned bounds in the object's own space (its Transform moves them), used for behind-camera
		/// culling, the render distance and depth sorting. They replace the automatic bounds that ObjAdd* keeps over
		/// the object's quads and triangles, for good.</summary>
		public void ObjSetBounds(int id, double minX, double minY, double minZ, double maxX, double maxY, double maxZ) {
			var o = Obj(id); if (o == null) return;
			o.MinX = minX; o.MinY = minY; o.MinZ = minZ; o.MaxX = maxX; o.MaxY = maxY; o.MaxZ = maxZ; o.HasBounds = true; o.BoundsSet = true;
			o.SetSphere();   // once here, not per-frame in cull
			if (o.MergeCandidate) _geomRevision++;   // the centre picks the merge bucket
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
			bool was = o.MergeCandidate;
			int na = a < 0 ? 0 : (a > 255 ? 255 : a);
			if (o.R != r || o.G != g || o.B != b || (o.A != na && o.Kind >= 3)) _rtRevision++;   // the path tracers draw the flat colour and a sprite's alpha, not the pass
			o.Pass = pass; o.R = r; o.G = g; o.B = b; o.A = na;
			MergeFlip(o, was);
		}
		/// <summary>Raytracer material id for this object (rasterizer ignores it). -1 = default.</summary>
		public void ObjSetMaterial(int id, int matId) { var o = Obj(id); if (o != null) { if (o.Material != matId) _rtRevision++; o.Material = matId; } }
		/// <summary>Optional model matrix (Lua table of 16 numbers, row-major). nil/empty = identity.</summary>
		public void ObjSetTransform(int id, LuaTable m) {
			var o = Obj(id); if (o == null) return;
			bool was = o.MergeCandidate;
			if (m == null) { o.Transform = null; RtEdit(); MergeFlip(o, was); return; }
			var t = new double[16];
			for (int i = 0; i < 16; i++) {
				object v = m[i + 1];
				t[i] = v switch { double d => d, long l => l, int ii => ii, _ => (i % 5 == 0 ? 1.0 : 0.0) };
			}
			o.Transform = t; RtEdit(); MergeFlip(o, was);
		}
		public void ObjClearTransform(int id) { var o = Obj(id); if (o != null) { bool was = o.MergeCandidate; o.Transform = null; RtEdit(); MergeFlip(o, was); } }

		/// <summary>Fast per-frame transform setter: 16 row-major doubles, no Lua-table marshaling and
		/// no per-call allocation (reuses the object's matrix array). Use this on the hot path instead
		/// of <see cref="ObjSetTransform"/>, which boxes a LuaTable and allocates every call.</summary>
		public void ObjSetTransform16(int id,
			double m0, double m1, double m2, double m3, double m4, double m5, double m6, double m7,
			double m8, double m9, double m10, double m11, double m12, double m13, double m14, double m15) {
			var o = Obj(id); if (o == null) return;
			bool was = o.MergeCandidate;
			var t = o.Transform; if (t == null || t.Length != 16) { t = new double[16]; o.Transform = t; _rtRevision++; }
			else if (t[0] != m0 || t[1] != m1 || t[2] != m2 || t[3] != m3 || t[4] != m4 || t[5] != m5
				|| t[6] != m6 || t[7] != m7 || t[8] != m8 || t[9] != m9 || t[10] != m10 || t[11] != m11) _rtRevision++;   // the tracers read the top three rows
			t[0] = m0; t[1] = m1; t[2] = m2; t[3] = m3; t[4] = m4; t[5] = m5; t[6] = m6; t[7] = m7;
			t[8] = m8; t[9] = m9; t[10] = m10; t[11] = m11; t[12] = m12; t[13] = m13; t[14] = m14; t[15] = m15;
			MergeFlip(o, was);
		}

		// ── model posing (IModel.Pose): triangles in model space, placed by the object's transform ──
		/// <summary>True when object <paramref name="id"/> still holds what a Pose of this model with the same clip,
		/// time, yaw, mirror and part routing wrote into it, so the new Pose only has to place it.</summary>
		internal bool PoseIsCurrent(int id, object model, int anim, double time, double yawDeg, bool mirror, int[]? route) {
			var o = Obj(id); var p = o?.Pose;
			if (p == null || p.Version != o!.GeomVersion || !ReferenceEquals(p.Model, model) || p.Anim != anim
				|| !p.Time.Equals(time) || !p.Yaw.Equals(yawDeg) || p.Mirror != mirror) return false;
			if (p.Route == null || route == null) return p.Route == null && route == null;
			return p.Route.AsSpan().SequenceEqual(route);
		}
		/// <summary>True when object <paramref name="id"/> holds what a Pose of this model with the same yaw, mirror and
		/// part routing wrote into it while its parts sat at <paramref name="mats"/> (their placement matrices), at any
		/// clip time: a Pose whose parts did not move leaves it as it is.</summary>
		internal bool PoseHolds(int id, object model, double yawDeg, bool mirror, int[]? route, ReadOnlySpan<double> mats) {
			var o = Obj(id); var p = o?.Pose;
			if (p == null || p.Version != o!.GeomVersion || !ReferenceEquals(p.Model, model) || p.MatsLen != mats.Length
				|| !p.Yaw.Equals(yawDeg) || p.Mirror != mirror) return false;
			if ((p.Route == null) != (route == null) || (route != null && !p.Route.AsSpan().SequenceEqual(route))) return false;
			return p.Mats.AsSpan(0, p.MatsLen).SequenceEqual(mats);
		}
		/// <summary>Record what a Pose just wrote into object <paramref name="id"/> (see <see cref="PoseIsCurrent"/>), with
		/// the placement matrices of its parts when <paramref name="hasMats"/> (see <see cref="PoseHolds"/>).</summary>
		internal void StampPose(int id, object model, int anim, double time, double yawDeg, bool mirror, int[]? route,
			ReadOnlySpan<double> mats = default, bool hasMats = false) {
			var o = Obj(id); if (o == null) return;
			var p = o.Pose ??= new PoseStamp();
			p.Model = model; p.Anim = anim; p.Time = time; p.Yaw = yawDeg; p.Mirror = mirror; p.Version = o.GeomVersion;
			if (route == null) p.Route = null;
			else {
				if (p.Route == null || p.Route.Length != route.Length) p.Route = new int[route.Length];
				route.CopyTo(p.Route, 0);
			}
			if (!hasMats) { p.MatsLen = -1; return; }
			if (p.Mats == null || p.Mats.Length < mats.Length) p.Mats = new double[mats.Length];
			mats.CopyTo(p.Mats); p.MatsLen = mats.Length;
		}
		/// <summary>Give object <paramref name="id"/> the transform of a model posed at (x, y, z), turned by
		/// <paramref name="yawDeg"/> and scaled by <paramref name="scale"/> (see <see cref="ModelPlacement"/>).</summary>
		internal void PlaceModel(int id, double x, double y, double z, double yawDeg, double scale) {
			var o = Obj(id); if (o == null) return;
			Span<double> m = stackalloc double[16];
			ModelPlacement.Matrix(x, y, z, yawDeg, scale, m);
			var t = o.Transform;
			if (t != null && t.Length == 16 && m.SequenceEqual(t)) return;
			bool was = o.MergeCandidate;
			if (t == null || t.Length != 16) { t = new double[16]; o.Transform = t; }
			m.CopyTo(t);
			RtEdit(); MergeFlip(o, was);
		}

		public void ObjAddQuadTex(int id,
			double x0, double y0, double z0, double x1, double y1, double z1,
			double x2, double y2, double z2, double x3, double y3, double z3,
			int texId, double uMax, double vMax, double shade) {
			var o = ObjFor(id, 0); if (o == null) return;
			o.Kind = 0; EnsureCap(o, PrimLayout.TexQuadStride);
			int k = o.N * PrimLayout.TexQuadStride; var d = o.D;
			d[k] = x0; d[k+1] = y0; d[k+2] = z0; d[k+3] = x1; d[k+4] = y1; d[k+5] = z1;
			d[k+6] = x2; d[k+7] = y2; d[k+8] = z2; d[k+9] = x3; d[k+10] = y3; d[k+11] = z3;
			d[k + PrimLayout.TexQuadTex] = texId; d[k + PrimLayout.TexQuadUMax] = uMax; d[k + PrimLayout.TexQuadVMax] = vMax; d[k + PrimLayout.TexQuadShade] = shade;
			o.GrowBounds(k, 4, 3);
			o.N++; o.GeomVersion++; RtEdit();
			if (o.MergeCandidate) _geomRevision++;
		}
		public void ObjAddQuadFlat(int id,
			double x0, double y0, double z0, double x1, double y1, double z1,
			double x2, double y2, double z2, double x3, double y3, double z3) {
			var o = ObjFor(id, 1); if (o == null) return;
			o.Kind = 1; EnsureCap(o, PrimLayout.FlatQuadStride);
			int k = o.N * PrimLayout.FlatQuadStride; var d = o.D;
			d[k] = x0; d[k+1] = y0; d[k+2] = z0; d[k+3] = x1; d[k+4] = y1; d[k+5] = z1;
			d[k+6] = x2; d[k+7] = y2; d[k+8] = z2; d[k+9] = x3; d[k+10] = y3; d[k+11] = z3;
			o.GrowBounds(k, 4, 3);
			o.N++; o.GeomVersion++; RtEdit();
		}
		public void ObjAddTriTex(int id,
			double x0, double y0, double z0, double u0, double v0,
			double x1, double y1, double z1, double u1, double v1,
			double x2, double y2, double z2, double u2, double v2,
			int texId, double shade) {
			var o = ObjFor(id, 2); if (o == null) return;
			o.Kind = 2; EnsureCap(o, PrimLayout.TexTriStride);
			int k = o.N * PrimLayout.TexTriStride; var d = o.D;
			d[k]=x0; d[k+1]=y0; d[k+2]=z0; d[k+3]=u0; d[k+4]=v0;
			d[k+5]=x1; d[k+6]=y1; d[k+7]=z1; d[k+8]=u1; d[k+9]=v1;
			d[k+10]=x2; d[k+11]=y2; d[k+12]=z2; d[k+13]=u2; d[k+14]=v2;
			d[k + PrimLayout.TexTriTex] = texId; d[k + PrimLayout.TexTriShade] = shade;
			o.GrowBounds(k, 3, 5);
			o.N++; o.GeomVersion++; RtEdit();
			if (o.MergeCandidate) _geomRevision++;
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
			var o = ObjFor(id, 3); if (o == null) return;
			o.Kind = 3; EnsureCap(o, PrimLayout.SpriteStride);
			int k = o.N * PrimLayout.SpriteStride; var d = o.D;
			d[k]=cx; d[k+1]=cy; d[k+2]=cz; d[k + PrimLayout.SpriteW] = w; d[k + PrimLayout.SpriteH] = h;
			d[k + PrimLayout.SpriteId] = spriteId; d[k + PrimLayout.SpriteBillboard] = billboard; d[k + PrimLayout.SpriteCutout] = cutout;
			o.N++; o.GeomVersion++; RtEdit();
		}

		/// <summary>Add a sprite drawn on an EXPLICIT world quad given by its four corners (winding
		/// 0→1→2→3, UVs (0,1)(1,1)(1,0)(0,0)). Unlike ObjAddSprite this does NOT billboard — you place the
		/// corners yourself, e.g. dropped onto the terrain so a character's silhouette shadow DRAPES over
		/// slopes/stairs. Sampled + tinted exactly like a sprite (object tint multiplies; alpha fades; cutout
		/// alpha-tests). An object holding these must hold ONLY sprite-quads (its own kind).</summary>
		public void ObjAddSpriteQuad(int id, double x0, double y0, double z0, double x1, double y1, double z1,
			double x2, double y2, double z2, double x3, double y3, double z3, int spriteId, int cutout) {
			var o = ObjFor(id, 4); if (o == null) return;
			o.Kind = 4; EnsureCap(o, PrimLayout.SpriteQuadStride);
			int k = o.N * PrimLayout.SpriteQuadStride; var d = o.D;
			d[k]=x0; d[k+1]=y0; d[k+2]=z0; d[k+3]=x1; d[k+4]=y1; d[k+5]=z1;
			d[k+6]=x2; d[k+7]=y2; d[k+8]=z2; d[k+9]=x3; d[k+10]=y3; d[k+11]=z3;
			d[k + PrimLayout.SpriteQuadId] = spriteId; d[k + PrimLayout.SpriteQuadCutout] = cutout;
			o.GrowBounds(k, 4, 3);
			o.N++; o.GeomVersion++; RtEdit();
		}
	}
}
