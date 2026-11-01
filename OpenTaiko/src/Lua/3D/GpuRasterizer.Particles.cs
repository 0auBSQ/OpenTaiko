using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	// The particle pass.
	internal sealed partial class GpuRasterizer {
		// particles: camera-facing billboards with per-particle RGBA, additive or alpha blend; own shader
		private uint _progP, _vaoP, _partVbo;
		private FrameLoc _pFrame; private int _pTextured, _pTex, _pAdd;
		// this pass's live particles: a record each, then one run per place in the transparent queue, blend and sprite
		// (Key: place, then alpha before additive, then sprite group), each run's particles in slots At..At+N-1 of _pOrd,
		// the alpha ones back to front (_pFar), and slot k's six vertices at 6k
		private struct PartRec { public int Sys, Idx, Run; public float A, Hw; public uint Far; }
		private struct PartRun { public long Key; public int Id, Gap, Add, Tex, At, N; }
		private PartRec[] _pRec = new PartRec[256]; private uint[] _pFar = new uint[256]; private int[] _pOrd = new int[256]; private int _pN;
		private int[] _runRank = new int[32];                                      // run id -> its index in key order
		private readonly Dictionary<(int add, int tex), int> _partGroup = new();   // (blend, sprite) -> this pass's group number
		private readonly List<int> _partGroupTex = new();                          // group number -> sprite
		private readonly Dictionary<long, int> _partRunOf = new();                 // run key -> run id
		private readonly List<PartRun> _partRuns = new(32);
		private readonly List<float> _partAll = new(4096);   // every run back to back: one upload per pass

		// ── particles: camera-facing billboards, rebuilt every pass ─────────────────────────────────────
		private static void PV(List<float> d, double x, double y, double z, double u, double v, double r, double g, double b, double a) {
			d.Add((float)x); d.Add((float)y); d.Add((float)z); d.Add((float)u); d.Add((float)v); d.Add((float)r); d.Add((float)g); d.Add((float)b); d.Add((float)a);
		}
		// a float's bits as an unsigned number in the float's order
		private static uint OrderBits(float f) { uint b = (uint)BitConverter.SingleToInt32Bits(f); return (b & 0x80000000u) != 0 ? ~b : b | 0x80000000u; }
		// Every live particle gets its place in the transparent queue (GapOf); then the vertices go out in runs of one place,
		// blend and sprite. In each place the alpha-blended runs come first, each back to front, then the additive ones.
		private void BuildParticles(Lua3DScene s) {
			_pN = 0; _partRuns.Clear(); _partRunOf.Clear(); _partAll.Clear(); _partGroup.Clear(); _partGroupTex.Clear();
			var systems = s._particleSystems; if (systems == null) return;
			var planes = (ReadOnlySpan<TPlane>)CollectionsMarshal.AsSpan(_tqPlanes);
			int nq = _tq.Count, lastAdd = -1, lastSpr = 0, lastGrp = 0, lastRun = -1;   // particles in a row mostly share their group and run
			long lastKey = -1;
			for (int si = 0; si < systems.Count; si++) {
				var ps = systems[si]; var arr = ps.P; int cnt = ps.Count;
				for (int i = 0; i < cnt; i++) {
					ref var p = ref arr[i];
					double lf = p.LifeLeft;
					double a = p.A0 * p.FadeAt(lf); if (a <= 0) continue; if (a > 1) a = 1;   // opacity and size over life
					double hw = p.SizeAt(lf) * 0.5;
					if (hw <= 0) continue;
					double depth = _view.Depth(p.X, p.Y, p.Z);
					Reach(depth + hw * 1.4142136);   // the quad's corners lie within hw * sqrt(2) of its centre
					int add = p.Additive ? 1 : 0, grp = lastGrp;
					if (add != lastAdd || p.Sprite != lastSpr) {
						if (!_partGroup.TryGetValue((add, p.Sprite), out grp)) { grp = _partGroupTex.Count; _partGroup[(add, p.Sprite)] = grp; _partGroupTex.Add(p.Sprite); }
						lastAdd = add; lastSpr = p.Sprite; lastGrp = grp;
					}
					int gap = 0;
					if (nq > 0) {
						double u = 0, v = 0, r = -1;   // the particle's circle on the camera plane, for the planes
						if (planes.Length > 0 && OnCameraPlane(s, p.X, p.Y, p.Z, out u, out v)) r = hw * 1.4142136 / depth;
						gap = GapOf(_tqKeys, nq, planes, _view.DistSq(p.X, p.Y, p.Z), p.X, p.Y, p.Z, u, v, r);
					}
					long key = (long)gap << 32 | (long)add << 31 | (long)grp;
					if (key != lastKey) {
						if (!_partRunOf.TryGetValue(key, out lastRun)) {
							lastRun = _partRuns.Count; _partRunOf[key] = lastRun;
							_partRuns.Add(new PartRun { Key = key, Id = lastRun, Gap = gap, Add = add, Tex = _partGroupTex[grp] });
						}
						lastKey = key;
					}
					CollectionsMarshal.AsSpan(_partRuns)[lastRun].N++;
					if (_pN == _pRec.Length) { Array.Resize(ref _pRec, _pN * 2); Array.Resize(ref _pFar, _pN * 2); Array.Resize(ref _pOrd, _pN * 2); }
					_pRec[_pN++] = new PartRec { Sys = si, Idx = i, Run = lastRun, A = (float)a, Hw = (float)hw, Far = add == 1 ? 0u : ~OrderBits((float)depth) };   // alpha: the farthest first
				}
			}
			if (_pN == 0) return;
			// the runs in key order, each particle into the next slot of its run, then each alpha run back to front
			_partRuns.Sort(_runOrder);
			var runs = CollectionsMarshal.AsSpan(_partRuns);
			if (_runRank.Length < runs.Length) _runRank = new int[Math.Max(runs.Length, _runRank.Length * 2)];
			for (int r = 0, at = 0; r < runs.Length; r++) { _runRank[runs[r].Id] = r; runs[r].At = at; at += runs[r].N; }
			for (int k = 0; k < _pN; k++) {
				ref var run = ref runs[_runRank[_pRec[k].Run]];
				_pFar[run.At] = _pRec[k].Far; _pOrd[run.At++] = k;
			}
			foreach (ref var run in runs) {
				run.At -= run.N;
				if (run.Add != 0) continue;
				for (int k = run.At + 1, end = run.At + run.N; k < end; k++)
					if (_pFar[k] < _pFar[k - 1]) { Array.Sort(_pFar, _pOrd, run.At, run.N); break; }
			}
			double Rx = s._Rx, Ry = s._Ry, Rz = s._Rz, Ux = s._Ux, Uy = s._Uy, Uz = s._Uz;
			for (int k = 0; k < _pN; k++) {
				ref var rec = ref _pRec[_pOrd[k]];
				ref var p = ref systems[rec.Sys].P[rec.Idx];
				double hw = rec.Hw, a = rec.A, cx = p.X, cy = p.Y, cz = p.Z;
				// per-particle spin: rotate the billboard axes in the camera plane
				double axRx = Rx, axRy = Ry, axRz = Rz, axUx = Ux, axUy = Uy, axUz = Uz;
				if (p.RotVel != 0 || p.Rot != 0) {
					double ang = p.Angle;
					double ca = Math.Cos(ang), sa = Math.Sin(ang);
					axRx = Rx * ca + Ux * sa; axRy = Ry * ca + Uy * sa; axRz = Rz * ca + Uz * sa;
					axUx = Ux * ca - Rx * sa; axUy = Uy * ca - Ry * sa; axUz = Uz * ca - Rz * sa;
				}
				double rX = axRx * hw, rY = axRy * hw, rZ = axRz * hw, uX = axUx * hw, uY = axUy * hw, uZ = axUz * hw;
				double cr = p.R / 255.0, cg = p.G / 255.0, cb = p.B / 255.0;
				var L = _partAll;
				PV(L, cx - rX - uX, cy - rY - uY, cz - rZ - uZ, 0, 1, cr, cg, cb, a);
				PV(L, cx + rX - uX, cy + rY - uY, cz + rZ - uZ, 1, 1, cr, cg, cb, a);
				PV(L, cx + rX + uX, cy + rY + uY, cz + rZ + uZ, 1, 0, cr, cg, cb, a);
				PV(L, cx - rX - uX, cy - rY - uY, cz - rZ - uZ, 0, 1, cr, cg, cb, a);
				PV(L, cx + rX + uX, cy + rY + uY, cz + rZ + uZ, 1, 0, cr, cg, cb, a);
				PV(L, cx - rX + uX, cy - rY + uY, cz - rZ + uZ, 0, 0, cr, cg, cb, a);
			}
		}
		private static readonly Comparison<PartRun> _runOrder = (a, b) => a.Key.CompareTo(b.Key);

		// the particle program with this pass's camera and fog, and this pass's particle vertices
		private void BeginParticles(Lua3DScene s) {
			Gl.UseProgram(_progP);
			SetCamera(_pFrame, s); Gl.Uniform1(_pTex, 0);
			SetFog(_pFrame, s);
			Gl.BindVertexArray(_vaoP);
			Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _partVbo);
			Gl.BufferData<float>(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)CollectionsMarshal.AsSpan(_partAll), BufferUsageARB.StreamDraw);
		}
		// one run of particles; a run whose sprite is not registered draws nothing
		private void DrawParticleRun(Lua3DScene s, in PartRun run) {
			uint gt = 0;
			if (run.Tex >= 0) { Gl.ActiveTexture(TextureUnit.Texture0); gt = SpriteFor(s, run.Tex); if (gt == 0) return; }
			if (run.Add == 1) GlPass.BlendAdd(Gl);
			else GlPass.BlendOver(Gl);
			Gl.Uniform1(_pTextured, run.Tex >= 0 ? 1 : 0);
			Gl.Uniform1(_pAdd, run.Add);
			if (run.Tex >= 0) Gl.BindTexture(TextureTarget.Texture2D, gt);
			Gl.DrawArrays(PrimitiveType.Triangles, run.At * 6, (uint)(run.N * 6));
		}
		// back to the world program after particle runs
		private void EndParticles() { Gl.UseProgram(_worldShadow ? _progShadow : _prog); Gl.BindVertexArray(_vao); GlPass.BlendOver(Gl); }
	}
}
