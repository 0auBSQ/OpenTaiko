using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace OpenTaiko {
	/// <summary>
	/// A minimal Wavefront OBJ importer for <see cref="Lua3DScene"/>. Parses positions (v), texcoords
	/// (vt) and faces (f, any polygon — triangulated as a fan), grouped by material (usemtl), and reads
	/// flat diffuse colours (Kd) from the referenced .mtl (mtllib). Like <see cref="GltfModel"/> it
	/// represents each material as a solid 2×2 texture, so OBJ models render through the exact same
	/// rasterizer path. OBJ is static, so it has no animations — <see cref="Pose"/> just applies the
	/// world transform (translation, Y-rotation, uniform scale) and flat-shades each face.
	///
	/// Not handled (kept intentionally small): image textures (map_Kd), smoothing groups/vertex normals
	/// (faces are flat-shaded from the geometric normal, matching GltfModel), and free-form surfaces.
	/// </summary>
	public sealed class ObjModel : IModel {
		// one triangle-soup per material: V = 3 floats/vertex, Uv = 2 floats/vertex, 3 verts per triangle.
		private sealed class Group { public List<float> V = new(); public List<float> Uv = new(); public int Mat; }

		private readonly List<Group> _groups = new();
		private int[] _matRgb = Array.Empty<int>();
		private int _texBase = -1;
		private static int _nextBase = 6000;   // own texture-id range (distinct from GltfModel's 4000+)

		public int AnimCount() => 0;
		public string AnimName(int i) => "";
		public double Duration(int i) => 0;

		public void Register(Lua3DScene scene) {
			if (_texBase < 0) { _texBase = _nextBase; _nextBase += 256; }
			int n = Math.Max(1, _matRgb.Length);
			for (int m = 0; m < n; m++) {
				int rgb = m < _matRgb.Length ? _matRgb[m] : 0xB0A090;
				scene.RegisterTexturePixels(_texBase + m, new int[4] { rgb, rgb, rgb, rgb }, 2, 2);
			}
		}

		public void Pose(Lua3DScene scene, int objId, int anim, double time, double x, double y, double z, double yawDeg, double scale) {
			// world transform W = T(x,y,z) · Ry(yaw) · S(scale)  (same convention as GltfModel)
			double yr = yawDeg * Math.PI / 180.0, c = Math.Cos(yr), s = Math.Sin(yr);
			double a0 = c * scale, a8 = s * scale, a2 = -s * scale, a10 = c * scale, a5 = scale;
			var light = (lx: 0.35, ly: 0.82, lz: 0.45);

			scene.ObjBegin(objId);
			foreach (var g in _groups) {
				int texId = _texBase + (g.Mat >= 0 ? g.Mat : 0);
				var V = g.V; var Uv = g.Uv; int triCount = V.Count / 9;
				for (int t = 0; t < triCount; t++) {
					int p = t * 9, q = t * 6;
					double px0 = V[p],     py0 = V[p + 1], pz0 = V[p + 2];
					double px1 = V[p + 3], py1 = V[p + 4], pz1 = V[p + 5];
					double px2 = V[p + 6], py2 = V[p + 7], pz2 = V[p + 8];
					double x0 = a0 * px0 + a8 * pz0 + x, y0 = a5 * py0 + y, z0 = a2 * px0 + a10 * pz0 + z;
					double x1 = a0 * px1 + a8 * pz1 + x, y1 = a5 * py1 + y, z1 = a2 * px1 + a10 * pz1 + z;
					double x2 = a0 * px2 + a8 * pz2 + x, y2 = a5 * py2 + y, z2 = a2 * px2 + a10 * pz2 + z;
					// flat shade from the world-space face normal
					double ex1 = x1 - x0, ey1 = y1 - y0, ez1 = z1 - z0, ex2 = x2 - x0, ey2 = y2 - y0, ez2 = z2 - z0;
					double nx = ey1 * ez2 - ez1 * ey2, ny = ez1 * ex2 - ex1 * ez2, nz = ex1 * ey2 - ey1 * ex2;
					double nl = Math.Sqrt(nx * nx + ny * ny + nz * nz); if (nl > 1e-9) { nx /= nl; ny /= nl; nz /= nl; }
					double nd = nx * light.lx + ny * light.ly + nz * light.lz; if (nd < 0) nd = -nd;
					double shade = 0.5 + 0.5 * nd;
					scene.ObjAddTriTex(objId,
						x0, y0, z0, Uv[q],     Uv[q + 1],
						x1, y1, z1, Uv[q + 2], Uv[q + 3],
						x2, y2, z2, Uv[q + 4], Uv[q + 5],
						texId, shade);
				}
			}
		}

		// ── parsing ────────────────────────────────────────────────────────────────────────────────
		public static ObjModel Load(string path) {
			var g = new ObjModel();
			var pos = new List<float>();   // v   (x y z)
			var uv = new List<float>();    // vt  (u v)
			var matIndex = new Dictionary<string, int>();
			var matColor = new List<int>();
			Group cur = null;
			var groupByMat = new Dictionary<int, Group>();

			Group GroupFor(int mat) {
				if (!groupByMat.TryGetValue(mat, out var grp)) { grp = new Group { Mat = mat }; groupByMat[mat] = grp; g._groups.Add(grp); }
				return grp;
			}
			cur = GroupFor(0);   // default material 0 until a usemtl appears

			// .mtl: newmtl name / Kd r g b  →  packed colour per material name
			void LoadMtl(string mtlPath) {
				if (!File.Exists(mtlPath)) return;
				string name = null;
				foreach (var raw in File.ReadLines(mtlPath)) {
					var line = raw.Trim();
					if (line.Length == 0 || line[0] == '#') continue;
					var tok = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
					if (tok[0] == "newmtl" && tok.Length > 1) {
						name = tok[1];
						if (!matIndex.ContainsKey(name)) { matIndex[name] = matColor.Count; matColor.Add(0xC0C0C0); }
					} else if (tok[0] == "Kd" && tok.Length >= 4 && name != null) {
						int r = Col(tok[1]), gg = Col(tok[2]), b = Col(tok[3]);
						matColor[matIndex[name]] = (r << 16) | (gg << 8) | b;
					}
				}
			}

			foreach (var raw in File.ReadLines(path)) {
				var line = raw.Trim();
				if (line.Length == 0 || line[0] == '#') continue;
				var tok = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
				switch (tok[0]) {
					case "v":
						if (tok.Length >= 4) { pos.Add(F(tok[1])); pos.Add(F(tok[2])); pos.Add(F(tok[3])); }
						break;
					case "vt":
						if (tok.Length >= 3) { uv.Add(F(tok[1])); uv.Add(1f - F(tok[2])); }   // OBJ vt origin is bottom-left
						else if (tok.Length == 2) { uv.Add(F(tok[1])); uv.Add(0f); }
						break;
					case "mtllib":
						if (tok.Length > 1) LoadMtl(Path.Combine(Path.GetDirectoryName(path) ?? ".", line.Substring(line.IndexOf(' ') + 1).Trim()));
						break;
					case "usemtl":
						if (tok.Length > 1 && matIndex.TryGetValue(tok[1], out var mi)) cur = GroupFor(mi);
						else cur = GroupFor(0);
						break;
					case "f":
						EmitFace(tok, pos, uv, cur);
						break;
				}
			}

			// finalise materials → packed colour array (index order matches matIndex)
			g._matRgb = matColor.Count > 0 ? matColor.ToArray() : new int[] { 0xB0A090 };
			// drop empty groups (e.g. the default group if every face had a material)
			g._groups.RemoveAll(grp => grp.V.Count == 0);
			if (g._groups.Count == 0) g._groups.Add(new Group { Mat = 0 });
			return g;
		}

		// parse one face line into triangles (fan), pushing flattened verts into the group
		private static void EmitFace(string[] tok, List<float> pos, List<float> uv, Group grp) {
			int nv = tok.Length - 1;
			if (nv < 3) return;
			int vCount = pos.Count / 3, vtCount = uv.Count / 2;
			Span<int> vi = nv <= 16 ? stackalloc int[nv] : new int[nv];
			Span<int> ti = nv <= 16 ? stackalloc int[nv] : new int[nv];
			for (int k = 0; k < nv; k++) {
				var parts = tok[k + 1].Split('/');
				vi[k] = Idx(parts[0], vCount);
				ti[k] = parts.Length > 1 && parts[1].Length > 0 ? Idx(parts[1], vtCount) : -1;
			}
			// triangle fan: (0, k, k+1)
			for (int k = 1; k < nv - 1; k++) {
				PushVert(grp, pos, uv, vi[0], ti[0]);
				PushVert(grp, pos, uv, vi[k], ti[k]);
				PushVert(grp, pos, uv, vi[k + 1], ti[k + 1]);
			}
		}

		private static void PushVert(Group grp, List<float> pos, List<float> uv, int vi, int ti) {
			if (vi < 0 || vi * 3 + 2 >= pos.Count) { grp.V.Add(0); grp.V.Add(0); grp.V.Add(0); }
			else { grp.V.Add(pos[vi * 3]); grp.V.Add(pos[vi * 3 + 1]); grp.V.Add(pos[vi * 3 + 2]); }
			if (ti < 0 || ti * 2 + 1 >= uv.Count) { grp.Uv.Add(0); grp.Uv.Add(0); }
			else { grp.Uv.Add(uv[ti * 2]); grp.Uv.Add(uv[ti * 2 + 1]); }
		}

		// OBJ indices are 1-based; negative = relative to the end of the current list
		private static int Idx(string s, int count) {
			if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i)) return -1;
			return i > 0 ? i - 1 : (i < 0 ? count + i : -1);
		}
		private static float F(string s) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ? f : 0f;
		private static int Col(string s) { float v = F(s); int c = (int)(v * 255 + 0.5f); return c < 0 ? 0 : (c > 255 ? 255 : c); }
	}
}
