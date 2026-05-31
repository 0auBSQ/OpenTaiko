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
		private sealed class Prim { public float[] Pos, Uv, Wt; public int[] Jt, Idx; public int Mat; }
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
		private int _texBase = -1;

		// scratch (reused each Pose; Pose is called single-threaded from the stage update)
		private double[][] _local = Array.Empty<double[]>();
		private double[][] _global = Array.Empty<double[]>();
		private double[][] _bone = Array.Empty<double[]>();

		public int AnimCount() => _anims.Count;
		public string AnimName(int i) => (i >= 0 && i < _anims.Count) ? _anims[i].Name : "";
		public double Duration(int i) => (i >= 0 && i < _anims.Count) ? _anims[i].Dur : 0;

		// each loaded model claims its own texture-id range so several models can coexist in a scene
		private static int _nextBase = 4000;

		// register a 2×2 solid texture per material into the scene (call once per scene)
		public void Register(Lua3DScene scene) {
			if (_texBase < 0) { _texBase = _nextBase; _nextBase += 256; }
			for (int m = 0; m < _matRgb.Length; m++) {
				int rgb = _matRgb[m];
				var px = new int[4] { rgb, rgb, rgb, rgb };
				scene.RegisterTexturePixels(_texBase + m, px, 2, 2);
			}
			if (_matRgb.Length == 0) { scene.RegisterTexturePixels(_texBase, new int[4] { 0xB0A090, 0xB0A090, 0xB0A090, 0xB0A090 }, 2, 2); }
		}

		// ── public skinning entry: write the posed model into scene object `objId` ──────────
		public void Pose(Lua3DScene scene, int objId, int anim, double time, double x, double y, double z, double yawDeg, double scale) {
			// local node matrices from bind, overridden by the animation
			for (int i = 0; i < _nodes.Length; i++) {
				var n = _nodes[i];
				_tT[0]=n.T[0]; _tT[1]=n.T[1]; _tT[2]=n.T[2];
				_tR[0]=n.R[0]; _tR[1]=n.R[1]; _tR[2]=n.R[2]; _tR[3]=n.R[3];
				_tS[0]=n.S[0]; _tS[1]=n.S[1]; _tS[2]=n.S[2];
                if (anim >= 0 && anim < _anims.Count) SampleNode(_anims[anim], i, time);
				TRS(_tT, _tR, _tS, _local[i]);
			}
			// global matrices (parents already precede children in our generator, but be safe)
			for (int i = 0; i < _nodes.Length; i++) {
				int p = _parent[i];
				if (p < 0) Array.Copy(_local[i], _global[i], 16);
				else Mul(_global[p], _local[i], _global[i]);
			}
			for (int j = 0; j < _jointNodes.Length; j++) Mul(_global[_jointNodes[j]], _ibm[j], _bone[j]);

			// instance world matrix W = T(x,y,z) * Ry(yaw) * S(scale) — column-major
			double yr = yawDeg * Math.PI / 180.0, c = Math.Cos(yr), s = Math.Sin(yr);
			double w0 = c * scale, w2 = -s * scale, w8 = s * scale, w10 = c * scale, w5 = scale;

			scene.ObjBegin(objId);
			var light = (lx: 0.35, ly: 0.82, lz: 0.45);
			foreach (var pr in _prims) {
				int texId = _texBase + (pr.Mat >= 0 ? pr.Mat : 0);
				int nv = pr.Pos.Length / 3;
				EnsureSkin(nv);
				// skin every vertex once
				for (int v = 0; v < nv; v++) {
					double px = pr.Pos[v*3], py = pr.Pos[v*3+1], pz = pr.Pos[v*3+2];
					double ax=0, ay=0, az=0;
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
					// apply instance transform W
					_skx[v] = w0*ax + w8*az + x;
					_sky[v] = w5*ay + y;
					_skz[v] = w2*ax + w10*az + z;
				}
				// emit triangles
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
					scene.ObjAddTriTex(objId,
						x0,y0,z0, pr.Uv[i0*2], pr.Uv[i0*2+1],
						x1,y1,z1, pr.Uv[i1*2], pr.Uv[i1*2+1],
						x2,y2,z2, pr.Uv[i2*2], pr.Uv[i2*2+1],
						texId, shade);
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
		public static GltfModel Load(string path) {
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

			// materials → packed base colour
			if (root.TryGetProperty("materials", out var mats)) {
				g._matRgb = new int[mats.GetArrayLength()];
				for (int m = 0; m < g._matRgb.Length; m++) {
					int rgb = 0xC0C0C0;
					if (mats[m].TryGetProperty("pbrMetallicRoughness", out var pbr) && pbr.TryGetProperty("baseColorFactor", out var bc)) {
						int r = (int)(bc[0].GetDouble() * 255), gg = (int)(bc[1].GetDouble() * 255), b = (int)(bc[2].GetDouble() * 255);
						rgb = (Math.Clamp(r,0,255) << 16) | (Math.Clamp(gg,0,255) << 8) | Math.Clamp(b,0,255);
					}
					g._matRgb[m] = rgb;
				}
			}

			// meshes → primitives (gathers all meshes' primitives; nodes reference a mesh but for our
			// single-skinned-mesh generator the geometry is in mesh 0 / a few primitives)
			if (root.TryGetProperty("meshes", out var meshes)) {
				foreach (var mesh in meshes.EnumerateArray()) {
					foreach (var prim in mesh.GetProperty("primitives").EnumerateArray()) {
						var att = prim.GetProperty("attributes");
						var pr = new Prim {
							Pos = ReadF(att.GetProperty("POSITION").GetInt32(), 3),
							Uv  = att.TryGetProperty("TEXCOORD_0", out var uvA) ? ReadF(uvA.GetInt32(), 2) : null,
							Wt  = att.TryGetProperty("WEIGHTS_0", out var wA) ? ReadF(wA.GetInt32(), 4) : null,
							Jt  = att.TryGetProperty("JOINTS_0", out var jA) ? ReadI(jA.GetInt32()) : null,
							Idx = ReadI(prim.GetProperty("indices").GetInt32()),
							Mat = prim.TryGetProperty("material", out var mi) ? mi.GetInt32() : 0,
						};
						int nv = pr.Pos.Length / 3;
						if (pr.Uv == null) pr.Uv = new float[nv * 2];
						if (pr.Wt == null) { pr.Wt = new float[nv * 4]; for (int v = 0; v < nv; v++) pr.Wt[v*4] = 1; }
						if (pr.Jt == null) pr.Jt = new int[nv * 4];
						g._prims.Add(pr);
					}
				}
			}

			// nodes
			if (root.TryGetProperty("nodes", out var nodesEl)) {
				int nc = nodesEl.GetArrayLength();
				g._nodes = new Node[nc]; g._parent = new int[nc];
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
					g._nodes[i] = node; g._parent[i] = -1;
				}
				for (int i = 0; i < nc; i++) foreach (var c in g._nodes[i].Children) g._parent[c] = i;
				for (int i = 0; i < nc; i++) if (g._parent[i] < 0) g._roots.Add(i);
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
