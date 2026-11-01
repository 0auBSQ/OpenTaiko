using System;
using System.Collections.Generic;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	// The grass pass.
	internal sealed partial class GpuRasterizer {
		// grass: one static buffer of blades (one instance each), billboarded + wind-swayed in the vertex shader.
		// The blades are sorted into square cells, so a frame draws only the cells within the fade distance and view.
		private uint _progGrass, _grassVbo;
		private FrameLoc _gFrame; private int _gTime, _gTex, _gSun, _gAmb, _gFade;
		private int _grassRevSeen = -1; private int _grassN;
		private const int GRASS_STRIDE = 6;   // foot xyz, height, phase, shade
		private struct GrassCell { public int Start, Count; public float MinX, MinY, MinZ, MaxX, MaxY, MaxZ; }
		private readonly List<GrassCell> _grassCells = new();
		private readonly List<(int start, int count)> _grassRuns = new();   // this pass's visible cells, adjacent ones joined
		private float _grassMaxH;

		private uint _vaoGrass;

		// (re)build the grass buffer when blades were added or removed: one instance per blade, sorted into square
		// cells so a pass draws only the cells near enough and in view (GatherGrass)
		private const float GRASS_CELL = 16f;
		private const int GRASS_MAX_CELLS = 65536;
		private void EnsureGrass(Lua3DScene s) {
			if (s._grassRev == _grassRevSeen) return;
			_grassRevSeen = s._grassRev;
			_grassCells.Clear(); _grassMaxH = 0;
			var src = s._grass;
			int n = src.Count / 6;
			_grassN = n;
			if (n == 0) return;
			static float Hash(float a, float b) {
				double nH = Math.Sin(a * 127.1 + b * 311.7) * 43758.5453;
				return (float)(nH - Math.Floor(nH));
			}
			// the grid of cells over the feet; the cell grows until the grid stays small
			float mnx = float.MaxValue, mnz = float.MaxValue, mxx = float.MinValue, mxz = float.MinValue;
			for (int i = 0; i < n; i++) {
				float x = src[i * 6], z = src[i * 6 + 2];
				if (float.IsFinite(x)) { if (x < mnx) mnx = x; if (x > mxx) mxx = x; }
				if (float.IsFinite(z)) { if (z < mnz) mnz = z; if (z > mxz) mxz = z; }
			}
			if (mxx < mnx) mnx = mxx = 0;
			if (mxz < mnz) mnz = mxz = 0;
			double cs = GRASS_CELL, gwD, ghD;
			while ((gwD = Math.Floor(((double)mxx - mnx) / cs) + 1) * (ghD = Math.Floor(((double)mxz - mnz) / cs) + 1) > GRASS_MAX_CELLS) cs *= 2;
			int gw = (int)gwD, gh = (int)ghD, cells = gw * gh;
			var start = new int[cells + 1];
			var cellOf = new int[n];
			for (int i = 0; i < n; i++) {
				int cx = Math.Clamp((int)Math.Floor((src[i * 6] - mnx) / cs), 0, gw - 1);
				int cz = Math.Clamp((int)Math.Floor((src[i * 6 + 2] - mnz) / cs), 0, gh - 1);
				int c = cz * gw + cx; cellOf[i] = c; start[c + 1]++;
			}
			for (int c = 0; c < cells; c++) start[c + 1] += start[c];
			var next = new int[cells];
			Array.Copy(start, next, cells);
			int need = n * GRASS_STRIDE;
			var inst = new float[need];
			for (int i = 0; i < n; i++) {
				int k = i * 6;
				float bx = src[k], by = src[k + 1], bz = src[k + 2];
				float h = src[k + 3], ph = src[k + 4], sh = src[k + 5];
				// de-grid: jitter the foot position and re-hash the sway phase from the position, so
				// even grid-planted callers (hand-placed rows) read organic
				bx += (Hash(bx * 3.1f, bz * 5.7f) - 0.5f) * 0.44f;
				bz += (Hash(bz * 7.3f, bx * 2.3f) - 0.5f) * 0.44f;
				ph += Hash(bx, bz) * 6.2831853f;
				int o = next[cellOf[i]]++ * GRASS_STRIDE;
				inst[o] = bx; inst[o + 1] = by; inst[o + 2] = bz; inst[o + 3] = h; inst[o + 4] = ph; inst[o + 5] = sh;
			}
			// each cell's box around its (jittered) feet
			for (int c = 0; c < cells; c++) {
				int a = start[c], b = start[c + 1];
				if (b == a) continue;
				var cell = new GrassCell { Start = a, Count = b - a, MinX = float.MaxValue, MinY = float.MaxValue, MinZ = float.MaxValue, MaxX = float.MinValue, MaxY = float.MinValue, MaxZ = float.MinValue };
				for (int i = a; i < b; i++) {
					int o = i * GRASS_STRIDE;
					float x = inst[o], y = inst[o + 1], z = inst[o + 2], h = Math.Abs(inst[o + 3]);
					if (x < cell.MinX) cell.MinX = x; if (x > cell.MaxX) cell.MaxX = x;
					if (y < cell.MinY) cell.MinY = y; if (y > cell.MaxY) cell.MaxY = y;
					if (z < cell.MinZ) cell.MinZ = z; if (z > cell.MaxZ) cell.MaxZ = z;
					if (h > _grassMaxH) _grassMaxH = h;
				}
				_grassCells.Add(cell);
			}
			Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _grassVbo);
			Gl.BufferData<float>(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(inst, 0, need), BufferUsageARB.StaticDraw);
		}

		// this pass's grass: the cells holding a blade closer than the fade end and in view, adjacent cells joined into
		// one draw. A blade whose foot lies past the fade end has no height, so a cell with no closer foot draws nothing.
		private void GatherGrass(Lua3DScene s) {
			_grassRuns.Clear();
			EnsureGrass(s);
			if (_grassN == 0 || s._grassSprite < 0) return;
			double fade = s._grassFadeEnd, pad = _grassMaxH + 0.5;   // a blade reaches its height (sway included) around its foot
			foreach (var c in _grassCells) {
				double cx = (c.MinX + (double)c.MaxX) * 0.5, cy = (c.MinY + (double)c.MaxY) * 0.5, cz = (c.MinZ + (double)c.MaxZ) * 0.5;
				double hx = (c.MaxX - (double)c.MinX) * 0.5, hy = (c.MaxY - (double)c.MinY) * 0.5, hz = (c.MaxZ - (double)c.MinZ) * 0.5;
				if (!(_view.BoxDistSq(cx, cy, cz, hx, hy, hz) <= fade * fade)) continue;
				if (!_view.BoxInView(cx, cy, cz, hx + pad, hy + pad, hz + pad)) continue;
				Reach(_view.FarDepth(cx, cy, cz, hx + pad, hy + pad, hz + pad));
				int last = _grassRuns.Count - 1;
				if (last >= 0 && _grassRuns[last].start + _grassRuns[last].count == c.Start) _grassRuns[last] = (_grassRuns[last].start, _grassRuns[last].count + c.Count);
				else _grassRuns.Add((c.Start, c.Count));
			}
		}

		private void DrawGrass(Lua3DScene s) {
			if (_grassRuns.Count == 0) return;
			uint gt = SpriteFor(s, s._grassSprite); if (gt == 0) return;
			Gl.UseProgram(_progGrass);
			SetCamera(_gFrame, s);
			Gl.Uniform1(_gTime, (float)s._shaderTime);
			Gl.Uniform2(_gFade, (float)s._grassFadeStart, (float)s._grassFadeEnd);
			Gl.Uniform3(_gSun, (float)s._sunR, (float)s._sunG, (float)s._sunB);
			Gl.Uniform3(_gAmb, (float)s._ambR, (float)s._ambG, (float)s._ambB);
			SetFog(_gFrame, s);
			Gl.Uniform1(_gTex, 0);
			Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, gt);
			Gl.BindVertexArray(_vaoGrass);
			Gl.Disable(EnableCap.Blend); Gl.DepthMask(true);
			foreach (var (start, count) in _grassRuns) {
				Gl.BindVertexBuffer(0, _grassVbo, (nint)(start * GRASS_STRIDE * sizeof(float)), (uint)(GRASS_STRIDE * sizeof(float)));
				Gl.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, (uint)count);
			}
			// restore the world program's VAO for the passes that follow
			Gl.UseProgram(_worldShadow ? _progShadow : _prog); Gl.BindVertexArray(_vao);
		}

		private void ClearGrass() {
			_grassRevSeen = -1; _grassN = 0;
			_grassCells.Clear(); _grassRuns.Clear();
		}
	}
}
