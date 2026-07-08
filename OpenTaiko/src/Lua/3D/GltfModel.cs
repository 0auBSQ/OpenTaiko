using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace OpenTaiko {
	/// <summary>
	/// A minimal glTF/GLB importer + CPU skeletal-animation skinner for <see cref="Lua3DScene"/>.
	/// Handles the subset our procedural generator emits: indexed triangle primitives with
	/// POSITION/TEXCOORD_0/JOINTS_0/WEIGHTS_0, one skin (joints + inverse-bind matrices), a node
	/// hierarchy with TRS, LINEAR TRS animations, and flat material base colours (no image textures).
	///
	/// Pose() samples an animation at a time, skins every vertex on the CPU (linear blend skinning),
	/// places the result with a world transform, and writes the triangles into a scene object — so
	/// animated models render through the existing rasterizer with no new draw path.
	/// </summary>
	public sealed class GltfModel : IModel {
		// Node/Skinned: which scene node placed this primitive (glTF node TRS applies to non-skinned
		// meshes; skinned meshes are posed purely by their joints per the glTF spec). Node -1 = raw
		// (legacy models whose meshes are not referenced by any node).
		private sealed class Prim { public float[] Pos, Uv, Wt; public int[] Jt, Idx; public int Mat; public int Node = -1; public bool Skinned; }
		private sealed class Node { public double[] T = {0,0,0}; public double[] R = {0,0,0,1}; public double[] S = {1,1,1}; public int[] Children = Array.Empty<int>(); }
		private sealed class Channel { public int Node, Path, Stride; public double[] Times; public float[] Vals; } // Path 0=T,1=R,2=S
		private sealed class Anim { public string Name = ""; public double Dur; public List<Channel> Ch = new(); }

		private readonly List<Prim> _prims = new();
		private int[] _jointNodes = Array.Empty<int>();
		private double[][] _ibm = Array.Empty<double[]>();
		private Node[] _nodes = Array.Empty<Node>();
		private int[] _parent = Array.Empty<int>();
		private readonly List<int> _roots = new();
		private readonly List<Anim> _anims = new();
		private int[] _matRgb = Array.Empty<int>();
		private string[] _matNames = Array.Empty<string>();
		private double[][] _matEmissive = Array.Empty<double[]>();   // glTF emissiveFactor per material (rgb 0..1)
		private int _texBase = -1;
		// optional part→scene-object routing (OWM3d per-part material flags): parts not in the map go to
		// the Pose call's default objId. A "part" is one primitive instance (see PartCount()).
		private readonly Dictionary<int, int> _partObj = new();

		// scratch (reused each Pose; Pose is called single-threaded from the stage update)
		private double[][] _local = Array.Empty<double[]>();
		private double[][] _global = Array.Empty<double[]>();
		private double[][] _bone = Array.Empty<double[]>();

		public int AnimCount() => _anims.Count;
		public string AnimName(int i) => (i >= 0 && i < _anims.Count) ? _anims[i].Name : "";
		public double Duration(int i) => (i >= 0 && i < _anims.Count) ? _anims[i].Dur : 0;

		// each loaded model claims its own texture-id range so several models can coexist in a scene
		private static int _nextBase = 4000;
		// per-material decoded base-colour TEXTURE (null = flat colour). Transparent texels are encoded
		// as NEGATIVE ints — the engine's cutout convention across both renderers.
		private int[][] _matPix; private int[] _matTexW, _matTexH;

		// register a 2×2 solid texture per material into the scene (call once per scene)
		public void Register(Lua3DScene scene) {
			if (_texBase < 0) { _texBase = _nextBase; _nextBase += 256; }
			for (int m = 0; m < _matRgb.Length; m++) {
				if (_matPix != null && _matPix[m] != null) {     // real base-colour texture (VRM etc.)
					scene.RegisterTexturePixels(_texBase + m, _matPix[m], _matTexW[m], _matTexH[m]);
					continue;
				}
				int rgb = _matRgb[m];
				var px = new int[4] { rgb, rgb, rgb, rgb };
				scene.RegisterTexturePixels(_texBase + m, px, 2, 2);
			}
			if (_matRgb.Length == 0) { scene.RegisterTexturePixels(_texBase, new int[4] { 0xB0A090, 0xB0A090, 0xB0A090, 0xB0A090 }, 2, 2); }
		}

		// ── pose math shared by Pose / BuildCollider / GetBounds ─────────────────────────────

		// node local→global matrices (bind pose, or sampled from animation `anim` at `time`) + joint bones
		private void ComputeGlobals(int anim, double time) {
			for (int i = 0; i < _nodes.Length; i++) {
				var n = _nodes[i];
				_tT[0]=n.T[0]; _tT[1]=n.T[1]; _tT[2]=n.T[2];
				_tR[0]=n.R[0]; _tR[1]=n.R[1]; _tR[2]=n.R[2]; _tR[3]=n.R[3];
				_tS[0]=n.S[0]; _tS[1]=n.S[1]; _tS[2]=n.S[2];
				if (anim >= 0 && anim < _anims.Count) SampleNode(_anims[anim], i, time);
				TRS(_tT, _tR, _tS, _local[i]);
			}
			for (int i = 0; i < _nodes.Length; i++) {
				int p = _parent[i];
				if (p < 0) Array.Copy(_local[i], _global[i], 16);
				else Mul(_global[p], _local[i], _global[i]);
			}
			for (int j = 0; j < _jointNodes.Length; j++) Mul(_global[_jointNodes[j]], _ibm[j], _bone[j]);
		}

		// transform one primitive's vertices to MODEL space into _skx/_sky/_skz:
		//  • skinned prim  → linear blend skinning (joints already produce model space)
		//  • node-placed   → the node's global TRS (static glTF meshes: chairs, buildings, …)
		//  • raw           → pass-through (legacy models whose meshes have no referencing node)
		private void TransformPrim(Prim pr) {
			int nv = pr.Pos.Length / 3;
			EnsureSkin(nv);
			bool skin = pr.Skinned && _bone.Length > 0;
			double[] g = (!skin && pr.Node >= 0 && pr.Node < _global.Length) ? _global[pr.Node] : null;
			for (int v = 0; v < nv; v++) {
				double px = pr.Pos[v*3], py = pr.Pos[v*3+1], pz = pr.Pos[v*3+2];
				double ax, ay, az;
				if (skin) {
					ax = 0; ay = 0; az = 0;
					for (int k = 0; k < 4; k++) {
						double wgt = pr.Wt[v*4+k];
						if (wgt == 0) continue;
						int jb = pr.Jt[v*4+k];
						if (jb < 0 || jb >= _bone.Length) continue;
						var b = _bone[jb];
						ax += wgt * (b[0]*px + b[4]*py + b[8]*pz + b[12]);
						ay += wgt * (b[1]*px + b[5]*py + b[9]*pz + b[13]);
						az += wgt * (b[2]*px + b[6]*py + b[10]*pz + b[14]);
					}
				} else if (g != null) {
					ax = g[0]*px + g[4]*py + g[8]*pz + g[12];
					ay = g[1]*px + g[5]*py + g[9]*pz + g[13];
					az = g[2]*px + g[6]*py + g[10]*pz + g[14];
				} else {
					ax = px; ay = py; az = pz;
				}
				_skx[v] = ax; _sky[v] = ay; _skz[v] = az;
			}
		}

		// ── public skinning entry: write the posed model into scene object(s) ───────────────
		// Parts routed elsewhere via SetPartObject land in their own object (per-part material flags);
		// everything else goes to `objId`.
		public void Pose(Lua3DScene scene, int objId, int anim, double time, double x, double y, double z, double yawDeg, double scale) {
			ComputeGlobals(anim, time);

			// instance world matrix W = T(x,y,z) * Ry(yaw) * S(scale) — column-major
			double yr = yawDeg * Math.PI / 180.0, c = Math.Cos(yr), s = Math.Sin(yr);
			double w0 = c * scale, w2 = -s * scale, w8 = s * scale, w10 = c * scale, w5 = scale;

			// begin the default object + every distinct routed object exactly once
			scene.ObjBegin(objId);
			if (_partObj.Count > 0) {
				foreach (var kv in _partObj)
					if (kv.Value != objId) scene.ObjBegin(kv.Value);
			}

			var light = (lx: 0.35, ly: 0.82, lz: 0.45);
			for (int p = 0; p < _prims.Count; p++) {
				var pr = _prims[p];
				int target = (_partObj.Count > 0 && _partObj.TryGetValue(p, out int mapped)) ? mapped : objId;
				int texId = _texBase + (pr.Mat >= 0 ? pr.Mat : 0);
				TransformPrim(pr);
				int nv = pr.Pos.Length / 3;
				for (int v = 0; v < nv; v++) {          // model space → world (instance transform)
					double ax = _skx[v], ay = _sky[v], az = _skz[v];
					_skx[v] = w0*ax + w8*az + x;
					_sky[v] = w5*ay + y;
					_skz[v] = w2*ax + w10*az + z;
				}
				var idx = pr.Idx;
				for (int t = 0; t < idx.Length; t += 3) {
					int i0 = idx[t], i1 = idx[t+1], i2 = idx[t+2];
					double x0=_skx[i0], y0=_sky[i0], z0=_skz[i0];
					double x1=_skx[i1], y1=_sky[i1], z1=_skz[i1];
					double x2=_skx[i2], y2=_sky[i2], z2=_skz[i2];
					// flat shade from the world-space face normal
					double ex1=x1-x0, ey1=y1-y0, ez1=z1-z0, ex2=x2-x0, ey2=y2-y0, ez2=z2-z0;
					double nx=ey1*ez2-ez1*ey2, ny=ez1*ex2-ex1*ez2, nz=ex1*ey2-ey1*ex2;
					double nl=Math.Sqrt(nx*nx+ny*ny+nz*nz); if (nl>1e-9){nx/=nl;ny/=nl;nz/=nl;}
					double nd=nx*light.lx+ny*light.ly+nz*light.lz; if (nd<0) nd=-nd;
					double shade=0.5+0.5*nd;
					scene.ObjAddTriTex(target,
						x0,y0,z0, pr.Uv[i0*2], pr.Uv[i0*2+1],
						x1,y1,z1, pr.Uv[i1*2], pr.Uv[i1*2+1],
						x2,y2,z2, pr.Uv[i2*2], pr.Uv[i2*2+1],
						texId, shade);
				}
			}
		}

		// ── OWM3d per-part API (parts = primitive instances) ────────────────────────────────
		public int PartCount() => _prims.Count;
		public int PartMaterial(int part) => (part >= 0 && part < _prims.Count) ? _prims[part].Mat : -1;
		public string MaterialName(int mat) => (mat >= 0 && mat < _matNames.Length) ? _matNames[mat] : "";
		public int MaterialCount() => _matRgb.Length;
		/// <summary>glTF emissiveFactor of a material (0,0,0 when none) — lets stages auto-mark glowing parts.</summary>
		public (double r, double g, double b) MaterialEmissive(int mat) {
			if (mat < 0 || mat >= _matEmissive.Length || _matEmissive[mat] == null) return (0, 0, 0);
			var e = _matEmissive[mat];
			return (e[0], e[1], e[2]);
		}
		/// <summary>Route a part's triangles into their own scene object on Pose (so the stage can set
		/// transparency/emissive/mirror flags on just that part). objId -1 clears the routing.</summary>
		public void SetPartObject(int part, int objId) {
			if (part < 0 || part >= _prims.Count) return;
			if (objId < 0) _partObj.Remove(part); else _partObj[part] = objId;
		}
		/// <summary>Drop ALL part→object routing. Models are file-cached (shared instances), so a world
		/// that used SetPartObject must clear the routing on unload — object ids restart after a scene
		/// clear, and stale routing would pour parts into unrelated new objects.</summary>
		public void ClearPartObjects() => _partObj.Clear();

		/// <summary>Model-space bind-pose AABB (for auto box colliders / placement bounds).</summary>
		public (double minX, double minY, double minZ, double maxX, double maxY, double maxZ) GetBounds() {
			ComputeGlobals(-1, 0);
			double mnx = double.MaxValue, mny = double.MaxValue, mnz = double.MaxValue;
			double mxx = double.MinValue, mxy = double.MinValue, mxz = double.MinValue;
			foreach (var pr in _prims) {
				TransformPrim(pr);
				int nv = pr.Pos.Length / 3;
				for (int v = 0; v < nv; v++) {
					if (_skx[v] < mnx) mnx = _skx[v]; if (_skx[v] > mxx) mxx = _skx[v];
					if (_sky[v] < mny) mny = _sky[v]; if (_sky[v] > mxy) mxy = _sky[v];
					if (_skz[v] < mnz) mnz = _skz[v]; if (_skz[v] > mxz) mxz = _skz[v];
				}
			}
			if (mnx > mxx) return (0, 0, 0, 0, 0, 0);   // no geometry
			return (mnx, mny, mnz, mxx, mxy, mxz);
		}

		/// <summary>Bake the bind-pose triangles, placed at the given world transform, into a
		/// <see cref="MeshCollider"/> (feed it to PhysicsWorld:AddMesh between BeginStatic/EndStatic).</summary>
		public void BuildCollider(MeshCollider mc, double x, double y, double z, double yawDeg, double scale) {
			if (mc == null) return;
			if (scale < 0) {   // a mirrored bake flips the winding → inward normals → broken collision
				System.Diagnostics.Trace.TraceWarning("GltfModel.BuildCollider: negative scale is not supported; using its magnitude.");
				scale = -scale;
			}
			ComputeGlobals(-1, 0);
			double yr = yawDeg * Math.PI / 180.0, c = Math.Cos(yr), s = Math.Sin(yr);
			double w0 = c * scale, w2 = -s * scale, w8 = s * scale, w10 = c * scale, w5 = scale;
			foreach (var pr in _prims) {
				TransformPrim(pr);
				int nv = pr.Pos.Length / 3;
				for (int v = 0; v < nv; v++) {
					double ax = _skx[v], ay = _sky[v], az = _skz[v];
					_skx[v] = w0*ax + w8*az + x;
					_sky[v] = w5*ay + y;
					_skz[v] = w2*ax + w10*az + z;
				}
				var idx = pr.Idx;
				for (int t = 0; t < idx.Length; t += 3) {
					int i0 = idx[t], i1 = idx[t+1], i2 = idx[t+2];
					mc.AddTri(_skx[i0], _sky[i0], _skz[i0],
					          _skx[i1], _sky[i1], _skz[i1],
					          _skx[i2], _sky[i2], _skz[i2]);
				}
			}
		}

		private double[] _skx = Array.Empty<double>(), _sky = Array.Empty<double>(), _skz = Array.Empty<double>();
		private readonly double[] _tT = new double[3], _tR = new double[4], _tS = new double[3];
		private void EnsureSkin(int n) { if (_skx.Length < n) { _skx = new double[n]; _sky = new double[n]; _skz = new double[n]; } }

		private void SampleNode(Anim a, int node, double time) {
			foreach (var ch in a.Ch) {
				if (ch.Node != node) continue;
				int n = ch.Times.Length;
				double tt = time; if (tt < ch.Times[0]) tt = ch.Times[0]; else if (tt > ch.Times[n-1]) tt = ch.Times[n-1];
				int i = 0; while (i < n - 1 && ch.Times[i+1] < tt) i++;
				int j = Math.Min(i + 1, n - 1);
				double span = ch.Times[j] - ch.Times[i];
				double f = (span > 1e-9) ? (tt - ch.Times[i]) / span : 0;
				int s = ch.Stride;
				if (ch.Path == 1) { // rotation quaternion (slerp)
					Slerp(ch.Vals, i*s, j*s, f, _tR);
				} else {
					var dst = (ch.Path == 0) ? _tT : _tS;
					for (int k = 0; k < s; k++) dst[k] = ch.Vals[i*s+k] + (ch.Vals[j*s+k] - ch.Vals[i*s+k]) * f;
				}
			}
		}

		// ── math (column-major mat4 as double[16]) ──────────────────────────────────────────
		private static void TRS(double[] t, double[] r, double[] s, double[] m) {
			double x=r[0],y=r[1],z=r[2],w=r[3];
			double xx=x*x,yy=y*y,zz=z*z,xy=x*y,xz=x*z,yz=y*z,wx=w*x,wy=w*y,wz=w*z;
			m[0]=(1-2*(yy+zz))*s[0]; m[1]=(2*(xy+wz))*s[0];   m[2]=(2*(xz-wy))*s[0];   m[3]=0;
			m[4]=(2*(xy-wz))*s[1];   m[5]=(1-2*(xx+zz))*s[1]; m[6]=(2*(yz+wx))*s[1];   m[7]=0;
			m[8]=(2*(xz+wy))*s[2];   m[9]=(2*(yz-wx))*s[2];   m[10]=(1-2*(xx+yy))*s[2]; m[11]=0;
			m[12]=t[0]; m[13]=t[1]; m[14]=t[2]; m[15]=1;
		}
		private static void Mul(double[] a, double[] b, double[] o) {
			for (int col = 0; col < 4; col++)
				for (int row = 0; row < 4; row++)
					o[col*4+row] = a[row]*b[col*4] + a[4+row]*b[col*4+1] + a[8+row]*b[col*4+2] + a[12+row]*b[col*4+3];
		}
		private static void Slerp(float[] v, int i, int j, double f, double[] o) {
			double ax=v[i],ay=v[i+1],az=v[i+2],aw=v[i+3], bx=v[j],by=v[j+1],bz=v[j+2],bw=v[j+3];
			double d=ax*bx+ay*by+az*bz+aw*bw;
			if (d<0){bx=-bx;by=-by;bz=-bz;bw=-bw;d=-d;}
			double k0,k1;
			if (d>0.9995){k0=1-f;k1=f;} else {double th=Math.Acos(d),st=Math.Sin(th);k0=Math.Sin((1-f)*th)/st;k1=Math.Sin(f*th)/st;}
			o[0]=ax*k0+bx*k1;o[1]=ay*k0+by*k1;o[2]=az*k0+bz*k1;o[3]=aw*k0+bw*k1;
			double l=Math.Sqrt(o[0]*o[0]+o[1]*o[1]+o[2]*o[2]+o[3]*o[3]); if(l>1e-9){o[0]/=l;o[1]/=l;o[2]/=l;o[3]/=l;}
		}

		// ── GLB parsing ──────────────────────────────────────────────────────────────────────
		// FILE CACHE: Load returns one shared instance per file for the process lifetime. Reparsing the
		// same GLB on every map/stage load was the dominant memory leak: each reload claimed a fresh
		// 256-id texture range (_nextBase) and re-decoded every base-colour texture into new pixel
		// buffers that the scene registry then kept forever. Sharing is safe — geometry/anims are
		// read-only after parse, Register() re-registers the same ids into any scene, and Pose targets
		// caller-supplied objects. The one shared MUTABLE bit is the SetPartObject routing map, which is
		// per-file now: clear it (ClearPartObjects) when a world unloads so stale object ids never route
		// parts into a rebuilt scene. CPU pixel buffers are kept so later scenes (model-icon previews)
		// can Register too — bounded at ≤ ~1 MB × the distinct files shipped.
		private static readonly Dictionary<string, GltfModel> _fileCache = new(StringComparer.OrdinalIgnoreCase);
		public static void PurgeFileCache() { lock (_fileCache) _fileCache.Clear(); }
		public static GltfModel Load(string path) {
			string key;
			try { key = Path.GetFullPath(path); } catch { key = path; }
			lock (_fileCache) if (_fileCache.TryGetValue(key, out var hit)) return hit;
			var fresh = LoadUncached(path);
			lock (_fileCache) _fileCache[key] = fresh;
			return fresh;
		}
		private static GltfModel LoadUncached(string path) {
			byte[] bytes = File.ReadAllBytes(path);
			byte[] bin; string json;
			if (BitConverter.ToUInt32(bytes, 0) == 0x46546C67) {                 // "glTF" → GLB
				int p = 12; json = null; bin = null;
				while (p < bytes.Length) {
					uint clen = BitConverter.ToUInt32(bytes, p); uint ctype = BitConverter.ToUInt32(bytes, p + 4); p += 8;
					if (ctype == 0x4E4F534A) json = Encoding.UTF8.GetString(bytes, p, (int)clen);          // JSON
					else if (ctype == 0x004E4942) { bin = new byte[clen]; Array.Copy(bytes, p, bin, 0, (int)clen); } // BIN
					p += (int)clen;
				}
			} else { json = Encoding.UTF8.GetString(bytes); bin = Array.Empty<byte>(); }

			var g = new GltfModel();
			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;
			var accs = root.GetProperty("accessors");
			var views = root.GetProperty("bufferViews");

			float[] ReadF(int ai, int comps) {
				var a = accs[ai]; int bv = a.GetProperty("bufferView").GetInt32();
				int aOff = a.TryGetProperty("byteOffset", out var ao) ? ao.GetInt32() : 0;
				int count = a.GetProperty("count").GetInt32();
				var view = views[bv]; int vOff = view.TryGetProperty("byteOffset", out var vo) ? vo.GetInt32() : 0;
				int off = vOff + aOff;
				var r = new float[count * comps];
				for (int i = 0; i < r.Length; i++) r[i] = BitConverter.ToSingle(bin, off + i * 4);
				return r;
			}
			int[] ReadI(int ai) {
				var a = accs[ai]; int bv = a.GetProperty("bufferView").GetInt32();
				int aOff = a.TryGetProperty("byteOffset", out var ao) ? ao.GetInt32() : 0;
				int count = a.GetProperty("count").GetInt32();
				int ct = a.GetProperty("componentType").GetInt32();
				string type = a.GetProperty("type").GetString();
				int comps = type == "VEC4" ? 4 : type == "VEC3" ? 3 : type == "VEC2" ? 2 : 1;
				var view = views[bv]; int vOff = view.TryGetProperty("byteOffset", out var vo) ? vo.GetInt32() : 0;
				int off = vOff + aOff; int total = count * comps;
				var r = new int[total];
				for (int i = 0; i < total; i++) {
					if (ct == 5125) r[i] = BitConverter.ToInt32(bin, off + i * 4);
					else if (ct == 5123) r[i] = BitConverter.ToUInt16(bin, off + i * 2);
					else if (ct == 5121) r[i] = bin[off + i];
					else r[i] = (int)BitConverter.ToSingle(bin, off + i * 4);
				}
				return r;
			}

			// materials → packed base colour, plus the base-colour TEXTURE when present (decoded once;
			// transparent texels become negative ints = the engine's cutout convention)
			if (root.TryGetProperty("materials", out var mats)) {
				int nm = mats.GetArrayLength();
				g._matRgb = new int[nm];
				g._matNames = new string[nm];
				g._matEmissive = new double[nm][];
				g._matPix = new int[nm][]; g._matTexW = new int[nm]; g._matTexH = new int[nm];
				for (int m = 0; m < nm; m++) {
					g._matNames[m] = mats[m].TryGetProperty("name", out var mnEl) ? (mnEl.GetString() ?? "") : "";
					if (mats[m].TryGetProperty("emissiveFactor", out var emEl) && emEl.GetArrayLength() >= 3)
						g._matEmissive[m] = new[] { emEl[0].GetDouble(), emEl[1].GetDouble(), emEl[2].GetDouble() };
					int rgb = 0xC0C0C0; int texIdx = -1;
					if (mats[m].TryGetProperty("pbrMetallicRoughness", out var pbr)) {
						if (pbr.TryGetProperty("baseColorFactor", out var bc)) {
							int r = (int)(bc[0].GetDouble() * 255), gg = (int)(bc[1].GetDouble() * 255), b = (int)(bc[2].GetDouble() * 255);
							rgb = (Math.Clamp(r,0,255) << 16) | (Math.Clamp(gg,0,255) << 8) | Math.Clamp(b,0,255);
						}
						if (pbr.TryGetProperty("baseColorTexture", out var bct) && bct.TryGetProperty("index", out var bti))
							texIdx = bti.GetInt32();
					}
					g._matRgb[m] = rgb;
					if (texIdx >= 0 && root.TryGetProperty("textures", out var texsEl) && texIdx < texsEl.GetArrayLength()
						&& texsEl[texIdx].TryGetProperty("source", out var srcEl)
						&& root.TryGetProperty("images", out var imgsEl)) {
						int imgIdx = srcEl.GetInt32();
						if (imgIdx < imgsEl.GetArrayLength() && imgsEl[imgIdx].TryGetProperty("bufferView", out var ibv)) {
							var view = views[ibv.GetInt32()];
							int vOff = view.TryGetProperty("byteOffset", out var vo2) ? vo2.GetInt32() : 0;
							int vLen = view.GetProperty("byteLength").GetInt32();
							try {
								var imgBytes = new byte[vLen];
								Array.Copy(bin, vOff, imgBytes, 0, vLen);
								using var bmp = SkiaSharp.SKBitmap.Decode(imgBytes);
								if (bmp != null) {
									int tw = bmp.Width, th = bmp.Height;
									// cap huge textures (VRoid ships 2048²); 512² is plenty at chara scale
									int max = 512;
									SkiaSharp.SKBitmap use = bmp;
									if (tw > max || th > max) {
										int nw = Math.Max(1, tw * max / Math.Max(tw, th)), nh = Math.Max(1, th * max / Math.Max(tw, th));
										use = bmp.Resize(new SkiaSharp.SKImageInfo(nw, nh), SkiaSharp.SKFilterQuality.Medium) ?? bmp;
										tw = use.Width; th = use.Height;
									}
									var px2 = new int[tw * th];
									var cols = use.Pixels;
									for (int i = 0; i < px2.Length; i++) {
										var c2 = cols[i];
										int packed = (c2.Red << 16) | (c2.Green << 8) | c2.Blue;
										px2[i] = c2.Alpha < 128 ? (packed | unchecked((int)0x80000000)) : packed;
									}
									g._matPix[m] = px2; g._matTexW[m] = tw; g._matTexH[m] = th;
									if (!ReferenceEquals(use, bmp)) use.Dispose();
								}
							} catch (Exception e) { System.Diagnostics.Trace.TraceWarning("GltfModel texture decode failed: " + e.Message); }
						}
					}
				}
			}

			// meshes → primitive TEMPLATES (per mesh); instanced per referencing node below so static
			// glTF models (chairs, buildings — geometry placed via node TRS) come out assembled
			var meshTemplates = new List<List<Prim>>();
			if (root.TryGetProperty("meshes", out var meshes)) {
				foreach (var mesh in meshes.EnumerateArray()) {
					var list = new List<Prim>();
					foreach (var prim in mesh.GetProperty("primitives").EnumerateArray()) {
						var att = prim.GetProperty("attributes");
						if (!att.TryGetProperty("POSITION", out var posA)) continue;
						if (!prim.TryGetProperty("indices", out var idxA)) continue;   // non-indexed: skip (rare)
						var pr = new Prim {
							Pos = ReadF(posA.GetInt32(), 3),
							Uv  = att.TryGetProperty("TEXCOORD_0", out var uvA) ? ReadF(uvA.GetInt32(), 2) : null,
							Wt  = att.TryGetProperty("WEIGHTS_0", out var wA) ? ReadF(wA.GetInt32(), 4) : null,
							Jt  = att.TryGetProperty("JOINTS_0", out var jA) ? ReadI(jA.GetInt32()) : null,
							Idx = ReadI(idxA.GetInt32()),
							Mat = prim.TryGetProperty("material", out var mi) ? mi.GetInt32() : 0,
						};
						int nv = pr.Pos.Length / 3;
						if (pr.Uv == null) pr.Uv = new float[nv * 2];
						if (pr.Wt == null) { pr.Wt = new float[nv * 4]; for (int v = 0; v < nv; v++) pr.Wt[v*4] = 1; }
						if (pr.Jt == null) pr.Jt = new int[nv * 4];
						list.Add(pr);
					}
					meshTemplates.Add(list);
				}
			}

			// nodes (+ which mesh/skin each node references, for the expansion below)
			int[] nodeMesh = Array.Empty<int>();
			bool[] nodeSkin = Array.Empty<bool>();
			if (root.TryGetProperty("nodes", out var nodesEl)) {
				int nc = nodesEl.GetArrayLength();
				g._nodes = new Node[nc]; g._parent = new int[nc];
				nodeMesh = new int[nc]; nodeSkin = new bool[nc];
				for (int i = 0; i < nc; i++) {
					var nd = nodesEl[i]; var node = new Node();
					if (nd.TryGetProperty("translation", out var tt)) node.T = new[]{ tt[0].GetDouble(), tt[1].GetDouble(), tt[2].GetDouble() };
					if (nd.TryGetProperty("rotation", out var rr)) node.R = new[]{ rr[0].GetDouble(), rr[1].GetDouble(), rr[2].GetDouble(), rr[3].GetDouble() };
					if (nd.TryGetProperty("scale", out var ss)) node.S = new[]{ ss[0].GetDouble(), ss[1].GetDouble(), ss[2].GetDouble() };
					if (nd.TryGetProperty("children", out var ch)) {
						var c = new int[ch.GetArrayLength()];
						for (int k = 0; k < c.Length; k++) c[k] = ch[k].GetInt32();
						node.Children = c;
					}
					nodeMesh[i] = nd.TryGetProperty("mesh", out var nm2) ? nm2.GetInt32() : -1;
					nodeSkin[i] = nd.TryGetProperty("skin", out _);
					g._nodes[i] = node; g._parent[i] = -1;
				}
				for (int i = 0; i < nc; i++) foreach (var c in g._nodes[i].Children) g._parent[c] = i;
				for (int i = 0; i < nc; i++) if (g._parent[i] < 0) g._roots.Add(i);
			}

			// expand node→mesh references into primitive instances (Node carries the placement TRS;
			// skinned nodes pose via joints instead, per the glTF spec). Models whose meshes aren't
			// referenced by any node (our legacy procedural generator) keep the old all-meshes behaviour.
			bool anyNodeMesh = false;
			for (int i = 0; i < nodeMesh.Length; i++) {
				int mi2 = nodeMesh[i];
				if (mi2 < 0 || mi2 >= meshTemplates.Count) continue;
				anyNodeMesh = true;
				foreach (var tpl in meshTemplates[mi2]) {
					g._prims.Add(new Prim {
						Pos = tpl.Pos, Uv = tpl.Uv, Wt = tpl.Wt, Jt = tpl.Jt, Idx = tpl.Idx, Mat = tpl.Mat,
						Node = i, Skinned = nodeSkin[i],
					});
				}
			}
			if (!anyNodeMesh) {
				bool hasSkin = root.TryGetProperty("skins", out var sk0) && sk0.GetArrayLength() > 0;
				foreach (var list in meshTemplates)
					foreach (var tpl in list) { tpl.Node = -1; tpl.Skinned = hasSkin; g._prims.Add(tpl); }
			}

			// skin
			if (root.TryGetProperty("skins", out var skins) && skins.GetArrayLength() > 0) {
				var sk = skins[0];
				var jts = sk.GetProperty("joints");
				g._jointNodes = new int[jts.GetArrayLength()];
				for (int i = 0; i < g._jointNodes.Length; i++) g._jointNodes[i] = jts[i].GetInt32();
				var ibmF = ReadF(sk.GetProperty("inverseBindMatrices").GetInt32(), 16);
				g._ibm = new double[g._jointNodes.Length][];
				for (int j = 0; j < g._jointNodes.Length; j++) {
					var m = new double[16];
					for (int k = 0; k < 16; k++) m[k] = ibmF[j*16+k];
					g._ibm[j] = m;
				}
			}

			// animations
			if (root.TryGetProperty("animations", out var animsEl)) {
				foreach (var an in animsEl.EnumerateArray()) {
					var a = new Anim { Name = an.TryGetProperty("name", out var nm) ? nm.GetString() : "anim" };
					var samplers = an.GetProperty("samplers");
					foreach (var chEl in an.GetProperty("channels").EnumerateArray()) {
						var tgt = chEl.GetProperty("target");
						if (!tgt.TryGetProperty("node", out var nodeEl)) continue;
						string pathS = tgt.GetProperty("path").GetString();
						int pth = pathS == "rotation" ? 1 : pathS == "scale" ? 2 : 0;
						int si = chEl.GetProperty("sampler").GetInt32();
						var samp = samplers[si];
						var inF = ReadF(samp.GetProperty("input").GetInt32(), 1);
						int stride = pth == 1 ? 4 : 3;
						var outF = ReadF(samp.GetProperty("output").GetInt32(), stride);
						var times = new double[inF.Length];
						for (int k = 0; k < inF.Length; k++) { times[k] = inF[k]; if (times[k] > a.Dur) a.Dur = times[k]; }
						a.Ch.Add(new Channel { Node = nodeEl.GetInt32(), Path = pth, Stride = stride, Times = times, Vals = outF });
					}
					g._anims.Add(a);
				}
			}

			int nn = g._nodes.Length;
			g._local = new double[nn][]; g._global = new double[nn][];
			for (int i = 0; i < nn; i++) { g._local[i] = new double[16]; g._global[i] = new double[16]; }
			g._bone = new double[g._jointNodes.Length][];
			for (int j = 0; j < g._jointNodes.Length; j++) g._bone[j] = new double[16];
			return g;
		}
	}
}
