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
	/// rasterizer path. OBJ is static, so it has no animations — <see cref="Pose"/> writes the flat-shaded
	/// faces and places them with the object's transform (translation, Y-rotation, uniform scale). Faces without a
	/// material (before any usemtl, or naming one the .mtl lacks) use a neutral default colour.
	///
	/// Not handled (kept intentionally small): image textures (map_Kd), smoothing groups/vertex normals
	/// (faces are flat-shaded from the geometric normal, matching GltfModel), and free-form surfaces.
	/// </summary>
	public sealed class ObjModel : IModel {
		// one triangle-soup per material: V = 3 floats/vertex, Uv = 2 floats/vertex, 3 verts per triangle.
		private sealed class Group { public List<float> V = new(); public List<float> Uv = new(); public int Mat; }

		private readonly List<Group> _groups = new();
		private int[] _matRgb = Array.Empty<int>();
		private string _key = "";   // the model file: names its block of texture ids in each scene
		private const int DefaultRgb = 0xB0A090;   // faces without a material (Mat -1)

		// the triangles as they were read: nine numbers each into `positions`, one colour each into `colours`
		public void CopyTriangles(List<float> positions, List<int> colours) {
			foreach (var g in _groups) {
				int rgb = g.Mat >= 0 && g.Mat < _matRgb.Length ? _matRgb[g.Mat] : DefaultRgb;
				positions.AddRange(g.V);
				for (int t = 0; t < g.V.Count / 9; t++) colours.Add(rgb);
			}
		}

		public int AnimCount() => 0;
		public string AnimName(int i) => "";
		public double Duration(int i) => 0;

		// material m is texture TexBase(scene) + m, in the scene's block for this model file; the default
		// material comes right after the file's own
		private int TexBase(Lua3DScene scene) => scene.ModelTextureBase(_key, _matRgb.Length + 1);
		public void Register(Lua3DScene scene) {
			int tb = TexBase(scene);
			for (int m = 0; m <= _matRgb.Length; m++) {
				int rgb = m < _matRgb.Length ? _matRgb[m] : DefaultRgb;
				scene.RegisterTexturePixels(tb + m, new int[4] { rgb, rgb, rgb, rgb }, 2, 2);
			}
		}

		public void Pose(Lua3DScene scene, int objId, int anim, double time, double x, double y, double z, double yawDeg, double scale) {
			// the triangles stay in model space and the object's transform places them (same convention as GltfModel);
			// they are rewritten only when the yaw (the face shades) or the mirror changed since the last Pose
			bool mirror = scale < 0;
			if (!scene.PoseIsCurrent(objId, this, -1, 0, yawDeg, mirror, null)) {
				ModelPlacement.Light(yawDeg, out double lx, out double ly, out double lz);
				double sg = mirror ? -1 : 1;
				scene.ObjBegin(objId);
				int tb = TexBase(scene);
				foreach (var g in _groups) {
					int texId = tb + (g.Mat >= 0 ? g.Mat : _matRgb.Length);
					var V = g.V; var Uv = g.Uv; int triCount = V.Count / 9;
					for (int t = 0; t < triCount; t++) {
						int p = t * 9, q = t * 6;
						double x0 = sg * V[p],     y0 = sg * V[p + 1], z0 = sg * V[p + 2];
						double x1 = sg * V[p + 3], y1 = sg * V[p + 4], z1 = sg * V[p + 5];
						double x2 = sg * V[p + 6], y2 = sg * V[p + 7], z2 = sg * V[p + 8];
						double shade = ModelPlacement.Shade(x0, y0, z0, x1, y1, z1, x2, y2, z2, lx, ly, lz);
						scene.ObjAddTriTex(objId,
							x0, y0, z0, Uv[q],     Uv[q + 1],
							x1, y1, z1, Uv[q + 2], Uv[q + 3],
							x2, y2, z2, Uv[q + 4], Uv[q + 5],
							texId, shade);
					}
				}
				scene.StampPose(objId, this, -1, 0, yawDeg, mirror, null);
			}
			scene.PlaceModel(objId, x, y, z, yawDeg, scale);
		}

		// ── parsing ────────────────────────────────────────────────────────────────────────────────
		// Like GltfModel, Load returns one shared instance per file until PurgeFileCache, so reloading a
		// model on every map load does not re-parse it.
		private static readonly Dictionary<string, ObjModel> _fileCache = new(StringComparer.OrdinalIgnoreCase);
		public static void PurgeFileCache() { lock (_fileCache) _fileCache.Clear(); }
		private static string KeyOf(string path) { try { return Path.GetFullPath(path); } catch { return path; } }
		public static ObjModel Load(string path) {
			string key = KeyOf(path);
			lock (_fileCache) if (_fileCache.TryGetValue(key, out var hit)) return hit;
			var fresh = LoadUncached(path);
			lock (_fileCache) _fileCache[key] = fresh;
			return fresh;
		}
		internal static ObjModel LoadUncached(string path) {
			var g = new ObjModel { _key = KeyOf(path) };
			var pos = new List<float>();   // v   (x y z)
			var uv = new List<float>();    // vt  (u v)
			var matIndex = new Dictionary<string, int>();
			var matColor = new List<int>();
			Group? cur = null;
			var groupByMat = new Dictionary<int, Group>();

			Group GroupFor(int mat) {
				if (!groupByMat.TryGetValue(mat, out var grp)) { grp = new Group { Mat = mat }; groupByMat[mat] = grp; g._groups.Add(grp); }
				return grp;
			}
			cur = GroupFor(-1);   // the default material until a usemtl appears

			// .mtl: newmtl name / Kd r g b  →  packed colour per material name
			void LoadMtl(string mtlPath) {
				if (!File.Exists(mtlPath)) return;
				string? name = null;
				foreach (var raw in File.ReadLines(mtlPath)) {
					var line = raw.Trim();
					if (line.Length == 0 || line[0] == '#') continue;
					var tok = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
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
				var tok = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
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
						else cur = GroupFor(-1);
						break;
					case "f":
						EmitFace(tok, pos, uv, cur);
						break;
				}
			}

			// finalise materials → packed colour array (index order matches matIndex)
			g._matRgb = matColor.ToArray();
			// drop empty groups (e.g. the default group if every face had a material)
			g._groups.RemoveAll(grp => grp.V.Count == 0);
			if (g._groups.Count == 0) g._groups.Add(new Group { Mat = -1 });
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
