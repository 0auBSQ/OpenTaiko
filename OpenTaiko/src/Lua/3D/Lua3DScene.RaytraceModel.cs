using System;
using System.Collections.Generic;

namespace OpenTaiko {
	// The path tracers' model: revision, sky, materials, point lights and primitives.
	public partial class Lua3DScene {
		/// <summary>Bumped only when something the path tracers draw changes (camera, sky, sun, ambient, objects'
		/// geometry, transform, visibility, material, colours or alpha, materials, lights, primitives, textures, normal
		/// maps, sprites); the tracers restart their progressive accumulation when it moves.</summary>
		internal int _rtRevision;
		// an edit that changes the raytraced image
		private void RtEdit() { _rtRevision++; }

		// Used only by the Raytracer; the Rasterizer ignores all of this. Lua builds materials,
		// point lights and analytic / SDF primitives, and tags objects with a material id.
		// Every edit bumps _rtRevision so the path tracer resets its accumulation.

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
			_skyStrength = strength < 0 ? 0 : strength; RtEdit();
		}

		// ── Materials ───────────────────────────────────────────────────────────────────
		/// <summary>Create a material (default: diffuse mid-grey); returns its id.</summary>
		public int NewMaterial() { _materials.Add(new SceneMaterial()); RtEdit(); return _materials.Count - 1; }
		private SceneMaterial? Mat(int id) => (id >= 0 && id < _materials.Count) ? _materials[id] : null;
		/// <summary>"diffuse" | "metal" | "glass" | "emissive".</summary>
		public void MatSetType(int id, string type) {
			var m = Mat(id); if (m == null) return;
			m.Type = type switch { "metal" => 1, "glass" => 2, "emissive" => 3, _ => 0 }; RtEdit();
		}
		/// <summary>Albedo / base colour (linear 0-1). For metal this tints the reflection.</summary>
		public void MatSetAlbedo(int id, double r, double g, double b) { var m = Mat(id); if (m == null) return; m.R = r; m.G = g; m.B = b; RtEdit(); }
		/// <summary>0 = sharp mirror / clear glass, 1 = fully rough.</summary>
		public void MatSetRoughness(int id, double rough) { var m = Mat(id); if (m == null) return; m.Rough = rough < 0 ? 0 : (rough > 1 ? 1 : rough); RtEdit(); }
		/// <summary>Glass index of refraction (e.g. 1.5 for crown glass, 1.33 water, 2.4 diamond).</summary>
		public void MatSetIOR(int id, double ior) { var m = Mat(id); if (m == null) return; m.Ior = ior < 1 ? 1 : ior; RtEdit(); }
		/// <summary>Emission colour (linear 0-1) × strength. Lights the scene via GI.</summary>
		public void MatSetEmission(int id, double r, double g, double b, double strength) {
			var m = Mat(id); if (m == null) return; m.ER = r * strength; m.EG = g * strength; m.EB = b * strength; RtEdit();
		}
		/// <summary>Optional albedo texture (a RegisterTexture id), sampled by uv. -1 = none.</summary>
		public void MatSetTexture(int id, int texId) { var m = Mat(id); if (m == null) return; m.TexId = texId; RtEdit(); }
		/// <summary>Procedural normal-map preset: "none" | "wood" | "perlin" | "waves".</summary>
		public void MatSetNormalMap(int id, string preset) {
			var m = Mat(id); if (m == null) return;
			m.NormalMap = preset switch { "wood" => 1, "perlin" => 2, "waves" => 3, _ => 0 }; RtEdit();
		}
		/// <summary>Tangent-space normal-map texture (a RegisterTexture id, RGB = encoded normal).
		/// Applies to textured geometry (triangles/quads with UVs); overrides the procedural preset.
		/// Pass -1 to clear.</summary>
		public void MatSetNormalMapTexture(int id, int texId) { var m = Mat(id); if (m == null) return; m.NormalTex = texId; RtEdit(); }

		// ── Lights ──────────────────────────────────────────────────────────────────────
		/// <summary>Add a point light at (x,y,z), colour (r,g,b) linear 0-1 × intensity. The
		/// raytracer uses inverse-square falloff; the rasterizer (if lit) too.</summary>
		public void AddLight(double x, double y, double z, double r, double g, double b, double intensity) {
			_lights.Add(new SceneLight { X = x, Y = y, Z = z, R = r * intensity, G = g * intensity, B = b * intensity }); RtEdit();
		}
		/// <summary>Like <see cref="AddLight"/> but with a finite falloff radius (light reaches zero
		/// at <paramref name="range"/>) — used by the rasterizer's forward lighting (e.g. voxel
		/// torches). The raytracer ignores the range and uses inverse-square.</summary>
		public void AddLightRanged(double x, double y, double z, double r, double g, double b, double intensity, double range) {
			_lights.Add(new SceneLight { X = x, Y = y, Z = z, R = r * intensity, G = g * intensity, B = b * intensity, Range = range }); RtEdit();
		}
		public void ClearLights() { _lights.Clear(); RtEdit(); }

		// ── Primitives (analytic + SDF) ───────────────────────────────────────────────────
		/// <summary>Sphere centred (cx,cy,cz) radius r with material <paramref name="mat"/> (r is kept above 0: a
		/// negative radius would turn the normals inward).</summary>
		public int AddSphere(double cx, double cy, double cz, double r, int mat) {
			if (!(r >= 1e-6)) r = 1e-6;
			_primitives.Add(new ScenePrimitive { Kind = 0, X = cx, Y = cy, Z = cz, A = r, Material = mat }); RtEdit(); return _primitives.Count - 1;
		}
		/// <summary>Infinite plane through (px,py,pz) with normal (nx,ny,nz) (auto-normalised).</summary>
		public int AddPlane(double px, double py, double pz, double nx, double ny, double nz, int mat) {
			double l = Math.Sqrt(nx * nx + ny * ny + nz * nz); if (l > 1e-12) { nx /= l; ny /= l; nz /= l; }
			_primitives.Add(new ScenePrimitive { Kind = 1, X = px, Y = py, Z = pz, A = nx, B = ny, C = nz, Material = mat }); RtEdit(); return _primitives.Count - 1;
		}
		/// <summary>Axis-aligned box from (minx,miny,minz) to (maxx,maxy,maxz).</summary>
		public int AddBox(double minx, double miny, double minz, double maxx, double maxy, double maxz, int mat) {
			_primitives.Add(new ScenePrimitive {
				Kind = 2, MinX = minx, MinY = miny, MinZ = minz, MaxX = maxx, MaxY = maxy, MaxZ = maxz,
				X = (minx + maxx) * 0.5, Y = (miny + maxy) * 0.5, Z = (minz + maxz) * 0.5, Material = mat
			}); RtEdit(); return _primitives.Count - 1;
		}
		/// <summary>Torus at (cx,cy,cz): major radius R, minor radius r, axis 0=X,1=Y,2=Z.</summary>
		public int AddTorus(double cx, double cy, double cz, double R, double r, int axis, int mat) {
			_primitives.Add(new ScenePrimitive { Kind = 3, X = cx, Y = cy, Z = cz, A = R, B = r, C = axis, Material = mat }); RtEdit(); return _primitives.Count - 1;
		}
		/// <summary>Ray-marched SDF preset: "sphere" | "roundbox" | "torus" | "capsule" | "gyroid"
		/// | "octahedron" | "gem"/"diamond" (round brilliant cut, axis +z) at (cx,cy,cz) with
		/// per-axis scale (sx,sy,sz).</summary>
		public int AddSDF(string preset, double cx, double cy, double cz, double sx, double sy, double sz, int mat) {
			int p = preset switch { "roundbox" => 1, "torus" => 2, "capsule" => 3, "gyroid" => 4, "octahedron" => 5, "gem" => 6, "diamond" => 6, _ => 0 };
			_primitives.Add(new ScenePrimitive { Kind = 4, X = cx, Y = cy, Z = cz, A = sx, B = sy, C = sz, SdfPreset = p, Material = mat }); RtEdit(); return _primitives.Count - 1;
		}
		public void ClearPrimitives() { _primitives.Clear(); RtEdit(); }
		/// <summary>Reset the whole raytracer model (materials, lights, primitives) for a scene swap.</summary>
		public void ClearRaytraceModel() { _materials.Clear(); _lights.Clear(); _primitives.Clear(); RtEdit(); }
	}
}
