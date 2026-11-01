using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace OpenTaiko {
	/// <summary>
	/// A minimal glTF/GLB importer + CPU skeletal-animation skinner for <see cref="Lua3DScene"/>.
	/// Handles indexed triangle primitives with POSITION/TEXCOORD_0/JOINTS_0/WEIGHTS_0 (any component
	/// type, interleaved or not), one skin (joints + inverse-bind matrices), a node hierarchy with TRS or
	/// matrix transforms, TRS animations (LINEAR, STEP, CUBICSPLINE), material base colours and
	/// base-colour textures. A .gltf file's buffers and images may be files next to it or data URIs.
	///
	/// Pose() samples an animation at a time, skins every vertex on the CPU (linear blend skinning),
	/// writes the model-space triangles into a scene object and places that object with its transform —
	/// so animated models render through the existing rasterizer with no new draw path. The triangles
	/// are rebuilt only when the pose changed since that object's last Pose; a move only sets the transform.
	/// </summary>
	public sealed class GltfModel : IModel {
		// Node/Skinned: which scene node placed this primitive (glTF node TRS applies to non-skinned
		// meshes; skinned meshes are posed purely by their joints per the glTF spec). Node -1 = raw
		// (legacy models whose meshes are not referenced by any node).
		private sealed class Prim { public float[] Pos = null!, Uv = null!, Wt = null!; public int[] Jt = null!, Idx = null!; public int Mat; public int Node = -1; public bool Skinned; }
		private sealed class Node { public double[] T = {0,0,0}; public double[] R = {0,0,0,1}; public double[] S = {1,1,1}; public int[] Children = Array.Empty<int>(); }
		// Path 0=T,1=R,2=S; Interp 0=LINEAR,1=STEP,2=CUBICSPLINE (3 values per key: in-tangent, value, out-tangent)
		private sealed class Channel { public int Path, Stride, Interp; public double[] Times = null!; public float[] Vals = null!; }
		// ByNode[node]: the channels that animate that node, in file order (null when none)
		private sealed class Anim { public string Name = ""; public double Dur; public Channel[][] ByNode = Array.Empty<Channel[]>(); }

		private readonly List<Prim> _prims = new();
		private int[] _jointNodes = Array.Empty<int>();
		private double[][] _ibm = Array.Empty<double[]>();
		private Node[] _nodes = Array.Empty<Node>();
		private int[] _parent = Array.Empty<int>();
		private int[] _order = Array.Empty<int>();   // node indices with every parent before its children
		private readonly List<Anim> _anims = new();
		private int[] _matRgb = Array.Empty<int>();
		private string[] _matNames = Array.Empty<string>();
		private double[][] _matEmissive = Array.Empty<double[]>();   // glTF emissiveFactor per material (rgb 0..1)
		private string _key = "";   // the model file: names its block of texture ids in each scene
		// optional part→scene-object routing (OWM3d per-part material flags): parts not in the map go to
		// the Pose call's default objId. A "part" is one primitive instance (see PartCount()).
		private readonly Dictionary<int, int> _partObj = new();

		// scratch (reused each Pose; Pose is called single-threaded from the stage update)
		private double[][] _local = Array.Empty<double[]>();
		private double[][] _global = Array.Empty<double[]>();
		private double[][] _bone = Array.Empty<double[]>();
		private int[] _route = Array.Empty<int>();   // target object per part for the current Pose

		public int AnimCount() => _anims.Count;
		public string AnimName(int i) => (i >= 0 && i < _anims.Count) ? _anims[i].Name : "";
		public double Duration(int i) => (i >= 0 && i < _anims.Count) ? _anims[i].Dur : 0;

		// per-material decoded base-colour TEXTURE (null = flat colour). Transparent texels are encoded
		// as NEGATIVE ints — the engine's cutout convention across both renderers.
		private int[][]? _matPix; private int[]? _matTexW, _matTexH;

		// material m is texture TexBase(scene) + m, in the scene's block for this model file
		private int TexBase(Lua3DScene scene) => scene.ModelTextureBase(_key, Math.Max(1, _matRgb.Length));

		// register a 2×2 solid texture per material into the scene (call once per scene)
		public void Register(Lua3DScene scene) {
			int tb = TexBase(scene);
			for (int m = 0; m < _matRgb.Length; m++) {
				if (_matPix != null && _matPix[m] != null) {     // real base-colour texture (VRM etc.)
					scene.RegisterTexturePixels(tb + m, _matPix[m], _matTexW![m], _matTexH![m]);
					continue;
				}
				int rgb = _matRgb[m];
				var px = new int[4] { rgb, rgb, rgb, rgb };
				scene.RegisterTexturePixels(tb + m, px, 2, 2);
			}
			if (_matRgb.Length == 0) { scene.RegisterTexturePixels(tb, new int[4] { 0xB0A090, 0xB0A090, 0xB0A090, 0xB0A090 }, 2, 2); }
		}

		// ── pose math shared by Pose / BuildCollider / GetBounds ─────────────────────────────

		// node local→global matrices (bind pose, or sampled from animation `anim` at `time`) + joint bones
		private void ComputeGlobals(int anim, double time) {
			Anim? a = (anim >= 0 && anim < _anims.Count) ? _anims[anim] : null;
			for (int i = 0; i < _nodes.Length; i++) {
				var n = _nodes[i];
				_tT[0]=n.T[0]; _tT[1]=n.T[1]; _tT[2]=n.T[2];
				_tR[0]=n.R[0]; _tR[1]=n.R[1]; _tR[2]=n.R[2]; _tR[3]=n.R[3];
				_tS[0]=n.S[0]; _tS[1]=n.S[1]; _tS[2]=n.S[2];
				if (a != null) SampleNode(a, i, time);
				TRS(_tT, _tR, _tS, _local[i]);
			}
			foreach (int i in _order) {
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
			double[]? g = (!skin && pr.Node >= 0 && pr.Node < _global.Length) ? _global[pr.Node] : null;
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
		// everything else goes to `objId`. Every target object gets the same placement transform.
		public void Pose(Lua3DScene scene, int objId, int anim, double time, double x, double y, double z, double yawDeg, double scale) {
			if (anim < 0 || anim >= _anims.Count) { anim = -1; time = 0; }   // the bind pose ignores the time
			bool mirror = scale < 0;
			int[]? route = null;
			if (_partObj.Count > 0) {
				if (_route.Length != _prims.Count) _route = new int[_prims.Count];
				for (int p = 0; p < _route.Length; p++) _route[p] = _partObj.TryGetValue(p, out int mapped) ? mapped : objId;
				route = _route;
			}
			bool current = scene.PoseIsCurrent(objId, this, anim, time, yawDeg, mirror, route);
			if (current && route != null)
				foreach (var kv in _partObj)
					if (kv.Value != objId && !scene.PoseIsCurrent(kv.Value, this, anim, time, yawDeg, mirror, route)) { current = false; break; }
			if (!current) Emit(scene, objId, route, anim, time, yawDeg, mirror);

			scene.PlaceModel(objId, x, y, z, yawDeg, scale);
			if (route != null)
				foreach (var kv in _partObj)
					if (kv.Value != objId) scene.PlaceModel(kv.Value, x, y, z, yawDeg, scale);
		}

		// write the posed triangles in model space (mirrored through the origin for a negative scale) into the
		// default object and every routed one, each face flat-shaded as the placed model would be in the world
		private void Emit(Lua3DScene scene, int objId, int[]? route, int anim, double time, double yawDeg, bool mirror) {
			ComputeGlobals(anim, time);

			// begin the default object + every distinct routed object exactly once
			// (an object whose parts sit where they sat when it was last written keeps its triangles: none of their nodes moved)
			GatherTargets(objId, route);
			for (int i = 0; i < _tgt.Count; i++) {
				var fp = _tgtFp[i];
				_tgtKeep[i] = _tgtHasFp[i] && scene.PoseHolds(_tgt[i], this, yawDeg, mirror, route, CollectionsMarshal.AsSpan(fp));
				if (!_tgtKeep[i]) scene.ObjBegin(_tgt[i]);
			}

			ModelPlacement.Light(yawDeg, out double lx, out double ly, out double lz);
			double sg = mirror ? -1 : 1;
			int tb = TexBase(scene);
			for (int p = 0; p < _prims.Count; p++) {
				var pr = _prims[p];
				int target = route != null ? route[p] : objId;
				if (_tgtKeep[TargetIndex(target)]) continue;
				int texId = tb + (pr.Mat >= 0 && pr.Mat < _matRgb.Length ? pr.Mat : 0);
				TransformPrim(pr);
				var idx = pr.Idx;
				for (int t = 0; t < idx.Length; t += 3) {
					int i0 = idx[t], i1 = idx[t+1], i2 = idx[t+2];
					double x0=sg*_skx[i0], y0=sg*_sky[i0], z0=sg*_skz[i0];
					double x1=sg*_skx[i1], y1=sg*_sky[i1], z1=sg*_skz[i1];
					double x2=sg*_skx[i2], y2=sg*_sky[i2], z2=sg*_skz[i2];
					double shade = ModelPlacement.Shade(x0, y0, z0, x1, y1, z1, x2, y2, z2, lx, ly, lz);
					scene.ObjAddTriTex(target,
						x0,y0,z0, pr.Uv[i0*2], pr.Uv[i0*2+1],
						x1,y1,z1, pr.Uv[i1*2], pr.Uv[i1*2+1],
						x2,y2,z2, pr.Uv[i2*2], pr.Uv[i2*2+1],
						texId, shade);
				}
			}

			for (int i = 0; i < _tgt.Count; i++)
				scene.StampPose(_tgt[i], this, anim, time, yawDeg, mirror, route, CollectionsMarshal.AsSpan(_tgtFp[i]), _tgtHasFp[i]);
		}

		// the objects one Emit writes (the default one, then each routed one once), and for each the placement matrices of
		// its parts in part order (_tgtFp; none when a part is skinned, its joints place it) and whether it keeps its triangles
		private readonly List<int> _tgt = new();
		private readonly List<List<double>> _tgtFp = new();
		private readonly List<bool> _tgtHasFp = new(), _tgtKeep = new();
		private int TargetIndex(int id) {
			for (int i = 0; i < _tgt.Count; i++) if (_tgt[i] == id) return i;
			return -1;
		}
		private int AddTarget(int id) {
			int i = TargetIndex(id);
			if (i >= 0) return i;
			i = _tgt.Count; _tgt.Add(id); _tgtHasFp.Add(true); _tgtKeep.Add(false);
			if (_tgtFp.Count <= i) _tgtFp.Add(new List<double>(32));
			_tgtFp[i].Clear();
			return i;
		}
		// needs ComputeGlobals for this pose first
		private void GatherTargets(int objId, int[]? route) {
			_tgt.Clear(); _tgtHasFp.Clear(); _tgtKeep.Clear();
			AddTarget(objId);
			for (int p = 0; p < _prims.Count; p++) {
				var pr = _prims[p];
				int i = AddTarget(route != null ? route[p] : objId);
				if (pr.Skinned && _bone.Length > 0) _tgtHasFp[i] = false;
				else if (pr.Node >= 0 && pr.Node < _global.Length) _tgtFp[i].AddRange(_global[pr.Node]);
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
		/// that used SetPartObject must clear the routing on unload — a new scene numbers its objects
		/// from 1 again, and stale routing would pour parts into unrelated new objects.</summary>
		public void ClearPartObjects() => _partObj.Clear();

		private (double, double, double, double, double, double)? _bounds;   // the bind pose never changes after load

		/// <summary>Model-space bind-pose AABB (for auto box colliders / placement bounds).</summary>
		public (double minX, double minY, double minZ, double maxX, double maxY, double maxZ) GetBounds() {
			if (_bounds is { } known) return known;
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
			_bounds = mnx > mxx ? (0.0, 0.0, 0.0, 0.0, 0.0, 0.0) : (mnx, mny, mnz, mxx, mxy, mxz);   // all zero: no geometry
			return _bounds.Value;
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
			var list = a.ByNode[node];
			if (list == null) return;
			foreach (var ch in list) {
				var tm = ch.Times; var v = ch.Vals;
				int n = tm.Length, s = ch.Stride;
				double tt = time; if (!(tt >= tm[0])) tt = tm[0]; else if (tt > tm[n-1]) tt = tm[n-1];
				int i = KeyAt(tm, tt), j = Math.Min(i + 1, n - 1);
				double span = tm[j] - tm[i];
				double f = (span > 1e-9) ? (tt - tm[i]) / span : 0;
				var dst = ch.Path == 0 ? _tT : ch.Path == 1 ? _tR : _tS;
				if (ch.Interp == 1 || j == i) {      // STEP, or the last key: that key's value as is
					int b = (ch.Interp == 2 ? 3*i + 1 : i) * s;
					for (int k = 0; k < s; k++) dst[k] = v[b+k];
					if (ch.Path == 1) NormalizeQuat(_tR);
				} else if (ch.Interp == 2) {          // CUBICSPLINE: Hermite from key i's value and out-tangent to key j's in-tangent and value
					double f2 = f*f, f3 = f2*f;
					double h00 = 2*f3 - 3*f2 + 1, h10 = (f3 - 2*f2 + f) * span, h01 = 3*f2 - 2*f3, h11 = (f3 - f2) * span;
					int vi = (3*i + 1) * s, bi = (3*i + 2) * s, aj = 3*j * s, vj = (3*j + 1) * s;
					for (int k = 0; k < s; k++) dst[k] = h00*v[vi+k] + h10*v[bi+k] + h01*v[vj+k] + h11*v[aj+k];
					if (ch.Path == 1) NormalizeQuat(_tR);
				} else if (ch.Path == 1) {            // rotation quaternion (slerp)
					Slerp(v, i*s, j*s, f, _tR);
				} else {
					for (int k = 0; k < s; k++) dst[k] = v[i*s+k] + (v[j*s+k] - v[i*s+k]) * f;
				}
			}
		}

		// index of the last key at or before t (t already clamped to the keys' range)
		private static int KeyAt(double[] tm, double t) {
			int lo = 0, hi = tm.Length - 1;
			while (lo < hi) { int mid = (lo + hi + 1) >> 1; if (tm[mid] <= t) lo = mid; else hi = mid - 1; }
			return lo;
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
		private static void NormalizeQuat(double[] q) {
			double l=Math.Sqrt(q[0]*q[0]+q[1]*q[1]+q[2]*q[2]+q[3]*q[3]); if(l>1e-9){q[0]/=l;q[1]/=l;q[2]/=l;q[3]/=l;}
		}
		// column-major TRS matrix → translation, rotation quaternion (x,y,z,w) and scale; a mirroring
		// matrix (negative determinant) keeps its mirror in the X scale
		private static void Decompose(double[] m, double[] t, double[] r, double[] s) {
			t[0]=m[12]; t[1]=m[13]; t[2]=m[14];
			double sx=Math.Sqrt(m[0]*m[0]+m[1]*m[1]+m[2]*m[2]), sy=Math.Sqrt(m[4]*m[4]+m[5]*m[5]+m[6]*m[6]), sz=Math.Sqrt(m[8]*m[8]+m[9]*m[9]+m[10]*m[10]);
			double det=m[0]*(m[5]*m[10]-m[9]*m[6]) - m[4]*(m[1]*m[10]-m[9]*m[2]) + m[8]*(m[1]*m[6]-m[5]*m[2]);
			if (det<0) sx=-sx;
			s[0]=sx; s[1]=sy; s[2]=sz;
			double ix=sx!=0?1/sx:0, iy=sy!=0?1/sy:0, iz=sz!=0?1/sz:0;
			// rotation matrix entries rRC (row R, column C)
			double r00=m[0]*ix, r10=m[1]*ix, r20=m[2]*ix, r01=m[4]*iy, r11=m[5]*iy, r21=m[6]*iy, r02=m[8]*iz, r12=m[9]*iz, r22=m[10]*iz;
			double tr=r00+r11+r22, qx, qy, qz, qw;
			if (tr>0) { double k=Math.Sqrt(tr+1)*2; qw=0.25*k; qx=(r21-r12)/k; qy=(r02-r20)/k; qz=(r10-r01)/k; }
			else if (r00>r11 && r00>r22) { double k=Math.Sqrt(1+r00-r11-r22)*2; qw=(r21-r12)/k; qx=0.25*k; qy=(r01+r10)/k; qz=(r02+r20)/k; }
			else if (r11>r22) { double k=Math.Sqrt(1+r11-r00-r22)*2; qw=(r02-r20)/k; qx=(r01+r10)/k; qy=0.25*k; qz=(r12+r21)/k; }
			else { double k=Math.Sqrt(1+r22-r00-r11)*2; qw=(r10-r01)/k; qx=(r02+r20)/k; qy=(r12+r21)/k; qz=0.25*k; }
			r[0]=qx; r[1]=qy; r[2]=qz; r[3]=qw;
			NormalizeQuat(r);
		}
		// nodes ordered so every parent comes before its children (glTF allows any array order); a
		// parent cycle is cut by making one of its nodes a root
		internal static int[] NodeOrder(int[] parent) {
			int n = parent.Length;
			var order = new List<int>(n);
			var state = new byte[n];   // 0 unvisited, 1 on the current chain, 2 placed
			var chain = new Stack<int>();
			for (int i = 0; i < n; i++) {
				int c = i;
				while (c >= 0 && state[c] == 0) { state[c] = 1; chain.Push(c); c = parent[c]; }
				if (c >= 0 && state[c] == 1) parent[chain.Peek()] = -1;
				while (chain.Count > 0) { int k = chain.Pop(); state[k] = 2; order.Add(k); }
			}
			return order.ToArray();
		}

		// ── GLB parsing ──────────────────────────────────────────────────────────────────────
		// FILE CACHE: Load returns one shared instance per file for the process lifetime. Reparsing the
		// same GLB on every map/stage load was the dominant memory leak: each reload re-decoded every
		// base-colour texture into new pixel buffers that the scene registry then kept forever.
		// Sharing is safe — geometry/anims are read-only after parse, Register() registers the file's
		// textures under the scene's block of ids for that file, and Pose targets
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
			var fresh = LoadUncached(path, key);
			lock (_fileCache) _fileCache[key] = fresh;
			return fresh;
		}
		// Coordinates are used exactly as the file stores them, with no axis flip and no winding change:
		// glTF's right-handed axes land as they are in the scene's left-handed camera basis (+X right,
		// +Y up, +Z forward at yaw 0).
		private static GltfModel LoadUncached(string path, string key) {
			byte[] bytes = File.ReadAllBytes(path);
			byte[]? glbBin = null; string? json = null;
			if (bytes.Length >= 12 && BitConverter.ToUInt32(bytes, 0) == 0x46546C67) {   // "glTF" → GLB
				int p = 12;
				while (p + 8 <= bytes.Length) {
					uint clen = BitConverter.ToUInt32(bytes, p); uint ctype = BitConverter.ToUInt32(bytes, p + 4); p += 8;
					if (clen > (uint)(bytes.Length - p)) break;
					if (ctype == 0x4E4F534A) json = Encoding.UTF8.GetString(bytes, p, (int)clen);          // JSON
					else if (ctype == 0x004E4942 && glbBin == null) { glbBin = new byte[clen]; Array.Copy(bytes, p, glbBin, 0, (int)clen); } // BIN
					p += (int)clen;
				}
				if (json == null) throw new InvalidDataException("GLB without a JSON chunk");
			} else {                                                                    // .gltf text, maybe with a UTF-8 BOM
				int bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
				json = Encoding.UTF8.GetString(bytes, bom, bytes.Length - bom);
			}
			string dir = Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".";

			var g = new GltfModel { _key = key };
			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;
			var accs = root.GetProperty("accessors");
			var views = root.GetProperty("bufferViews");

			// buffers: the GLB's BIN chunk (a buffer without uri), or a file next to the model / a data URI
			root.TryGetProperty("buffers", out var bufsEl);
			var bufs = new Dictionary<int, byte[]>();
			byte[] Buf(int bi) {
				if (bufs.TryGetValue(bi, out var b)) return b;
				bool listed = bufsEl.ValueKind == JsonValueKind.Array && bi >= 0 && bi < bufsEl.GetArrayLength();
				if (listed && bufsEl[bi].TryGetProperty("uri", out var uriEl)) b = ReadUri(uriEl.GetString() ?? "", dir);
				else if (bi == 0 && glbBin != null) b = glbBin;
				else throw new InvalidDataException("glTF buffer " + bi + " has no data");
				bufs[bi] = b;
				return b;
			}
			// where accessor ai's elements are: data (null = no bufferView, all zeros), first byte, byte step
			// between elements (the view's byteStride for interleaved data), count, components, component type
			(byte[]? d, int off, int stride, int count, int comps, int ct, bool norm) Acc(int ai) {
				var a = accs[ai];
				int count = a.GetProperty("count").GetInt32();
				int ct = a.GetProperty("componentType").GetInt32();
				int comps = TypeComps(a.GetProperty("type").GetString() ?? "");
				bool norm = a.TryGetProperty("normalized", out var nEl) && nEl.ValueKind == JsonValueKind.True;
				if (!a.TryGetProperty("bufferView", out var bvEl)) return (null, 0, 0, count, comps, ct, norm);
				var view = views[bvEl.GetInt32()];
				var d = Buf(view.TryGetProperty("buffer", out var bEl) ? bEl.GetInt32() : 0);
				int off = (view.TryGetProperty("byteOffset", out var vo) ? vo.GetInt32() : 0) + (a.TryGetProperty("byteOffset", out var ao) ? ao.GetInt32() : 0);
				int elem = CompSize(ct) * comps;
				int stride = view.TryGetProperty("byteStride", out var bs) && bs.GetInt32() > 0 ? bs.GetInt32() : elem;
				if (count > 0 && off + (long)stride * (count - 1) + elem > d.Length)
					throw new InvalidDataException("glTF accessor " + ai + " reads past the end of its buffer");
				return (d, off, stride, count, comps, ct, norm);
			}
			// `comps` floats per element whatever the stored type; normalized integers map to 0..1 / -1..1
			float[] ReadF(int ai, int comps) {
				var (d, off, stride, count, ac, ct, norm) = Acc(ai);
				var r = new float[count * comps];
				if (d == null) return r;
				int cs = CompSize(ct), nc = Math.Min(comps, ac);
				for (int i = 0; i < count; i++)
					for (int k = 0; k < nc; k++) r[i * comps + k] = (float)Comp(d, off + i * stride + k * cs, ct, norm);
				return r;
			}
			int[] ReadI(int ai) {
				var (d, off, stride, count, ac, ct, _) = Acc(ai);
				var r = new int[count * ac];
				if (d == null) return r;
				int cs = CompSize(ct);
				for (int i = 0; i < count; i++)
					for (int k = 0; k < ac; k++) r[i * ac + k] = (int)Comp(d, off + i * stride + k * cs, ct, false);
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
						if (imgIdx >= 0 && imgIdx < imgsEl.GetArrayLength()) {
							var img = imgsEl[imgIdx];
							try {
								byte[]? imgBytes = null;
								if (img.TryGetProperty("bufferView", out var ibv)) {
									var view = views[ibv.GetInt32()];
									var src = Buf(view.TryGetProperty("buffer", out var ib) ? ib.GetInt32() : 0);
									int vOff = view.TryGetProperty("byteOffset", out var vo2) ? vo2.GetInt32() : 0;
									int vLen = view.GetProperty("byteLength").GetInt32();
									imgBytes = new byte[vLen];
									Array.Copy(src, vOff, imgBytes, 0, vLen);
								} else if (img.TryGetProperty("uri", out var iu)) imgBytes = ReadUri(iu.GetString() ?? "", dir);
								using var bmp = imgBytes != null ? SkiaSharp.SKBitmap.Decode(imgBytes) : null;
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
							Uv  = att.TryGetProperty("TEXCOORD_0", out var uvA) ? ReadF(uvA.GetInt32(), 2) : null!,
							Wt  = att.TryGetProperty("WEIGHTS_0", out var wA) ? ReadF(wA.GetInt32(), 4) : null!,
							Jt  = att.TryGetProperty("JOINTS_0", out var jA) ? ReadI(jA.GetInt32()) : null!,
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
					if (nd.TryGetProperty("matrix", out var mx) && mx.GetArrayLength() == 16) {
						var m = new double[16];
						for (int k = 0; k < 16; k++) m[k] = mx[k].GetDouble();
						Decompose(m, node.T, node.R, node.S);
					}
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
				for (int i = 0; i < nc; i++) foreach (var c in g._nodes[i].Children) if (c >= 0 && c < nc) g._parent[c] = i;
				g._order = NodeOrder(g._parent);
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

			// animations: translation/rotation/scale channels, grouped per node. Morph-target "weights"
			// channels are skipped (no morph support) but still count towards the clip's duration.
			if (root.TryGetProperty("animations", out var animsEl)) {
				int nodeCount = g._nodes.Length;
				foreach (var an in animsEl.EnumerateArray()) {
					var a = new Anim { Name = an.TryGetProperty("name", out var nm) ? nm.GetString()! : "anim" };
					var byNode = new List<Channel>[nodeCount];
					var samplers = an.GetProperty("samplers");
					foreach (var chEl in an.GetProperty("channels").EnumerateArray()) {
						var tgt = chEl.GetProperty("target");
						if (!tgt.TryGetProperty("node", out var nodeEl)) continue;
						int si = chEl.GetProperty("sampler").GetInt32();
						var samp = samplers[si];
						var inF = ReadF(samp.GetProperty("input").GetInt32(), 1);
						for (int k = 0; k < inF.Length; k++) if (inF[k] > a.Dur) a.Dur = inF[k];
						string? pathS = tgt.GetProperty("path").GetString();
						int pth = pathS == "translation" ? 0 : pathS == "rotation" ? 1 : pathS == "scale" ? 2 : -1;
						int node = nodeEl.GetInt32();
						if (pth < 0 || node < 0 || node >= nodeCount || inF.Length == 0) continue;
						string? interpS = samp.TryGetProperty("interpolation", out var ipEl) ? ipEl.GetString() : "LINEAR";
						int interp = interpS == "STEP" ? 1 : interpS == "CUBICSPLINE" ? 2 : 0;
						int stride = pth == 1 ? 4 : 3;
						var outF = ReadF(samp.GetProperty("output").GetInt32(), stride);
						if (outF.Length < inF.Length * stride * (interp == 2 ? 3 : 1)) continue;   // fewer values than keys
						var times = new double[inF.Length];
						for (int k = 0; k < inF.Length; k++) times[k] = inF[k];
						(byNode[node] ??= new List<Channel>()).Add(new Channel { Path = pth, Stride = stride, Interp = interp, Times = times, Vals = outF });
					}
					a.ByNode = new Channel[nodeCount][];
					for (int i = 0; i < nodeCount; i++) if (byNode[i] != null) a.ByNode[i] = byNode[i].ToArray();
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

		private static int TypeComps(string type) => type switch {
			"VEC2" => 2, "VEC3" => 3, "VEC4" => 4, "MAT2" => 4, "MAT3" => 9, "MAT4" => 16, _ => 1,
		};
		private static int CompSize(int ct) => ct == 5126 || ct == 5125 ? 4 : (ct == 5122 || ct == 5123 ? 2 : 1);
		// one stored component as a number; `norm` maps an integer type onto 0..1 (unsigned) or -1..1 (signed)
		private static double Comp(byte[] d, int p, int ct, bool norm) => ct switch {
			5126 => BitConverter.ToSingle(d, p),
			5121 => norm ? d[p] / 255.0 : d[p],
			5120 => norm ? Math.Max((sbyte)d[p] / 127.0, -1.0) : (sbyte)d[p],
			5123 => norm ? BitConverter.ToUInt16(d, p) / 65535.0 : BitConverter.ToUInt16(d, p),
			5122 => norm ? Math.Max(BitConverter.ToInt16(d, p) / 32767.0, -1.0) : BitConverter.ToInt16(d, p),
			5125 => BitConverter.ToUInt32(d, p),
			_ => throw new InvalidDataException("unknown glTF componentType " + ct),
		};
		// a .gltf buffer or image uri: a base64 data URI, or a percent-encoded path relative to the model file
		private static byte[] ReadUri(string uri, string dir) {
			if (uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) {
				int comma = uri.IndexOf(',');
				if (comma < 0 || !uri.Substring(0, comma).EndsWith(";base64", StringComparison.OrdinalIgnoreCase))
					throw new InvalidDataException("glTF data URI is not base64");
				return Convert.FromBase64String(uri.Substring(comma + 1));
			}
			return File.ReadAllBytes(Path.Combine(dir, Uri.UnescapeDataString(uri)));
		}
	}
}
