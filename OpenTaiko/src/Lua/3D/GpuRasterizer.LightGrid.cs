using System;
using System.Collections.Generic;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	// light grid: ranged point lights per ground cell
	internal sealed partial class GpuRasterizer {
		private const int LG_CELLS = 64;        // cells per side
		private const float LG_CELL = 8f;       // world units
		private const int LG_SLOTS = 8;         // two RGBA texels: top, bottom half
		private const int LG_MIN = 6;           // fewer ranged lights: plain loop
		private const int UNIT_LIGHT_GRID = 13;
		private uint _lgTex;
		private readonly byte[] _lgBuf = new byte[LG_CELLS * LG_CELLS * 2 * 4];
		private readonly byte[] _lgCount = new byte[LG_CELLS * LG_CELLS];
		private bool _lgOn;
		private float _lgX0, _lgZ0;
		private int _lgGlobal;                  // leading unranged lights
		private int _lgOverflow;
		private struct LgLoc { public int Grid, Xf, Global; }
		private readonly Dictionary<uint, LgLoc> _lgLocs = new();
		private readonly float[] _lgPos = new float[MAXL * 4], _lgCol = new float[MAXL * 3];

		// after SetFrameUniforms; moves unranged lights first (shaders take them by count)
		private unsafe void BuildLightGrid(Lua3DScene s, int nl) {
			_lgOn = false; _lgGlobal = 0;
			if (!s._lightGrid || nl < LG_MIN) return;
			int ranged = 0;
			for (int i = 0; i < nl; i++) if (_lpos[i * 4 + 3] > 0f) ranged++;
			if (ranged < LG_MIN) return;
			int at = 0;
			for (int pass = 0; pass < 2; pass++)
				for (int i = 0; i < nl; i++) {
					bool hasRange = _lpos[i * 4 + 3] > 0f;
					if (hasRange != (pass == 1)) continue;
					Array.Copy(_lpos, i * 4, _lgPos, at * 4, 4); Array.Copy(_lcol, i * 3, _lgCol, at * 3, 3);
					at++;
				}
			Array.Copy(_lgPos, _lpos, nl * 4); Array.Copy(_lgCol, _lcol, nl * 3);
			_lgGlobal = nl - ranged;

			// grid ahead of the eye, world-fixed cell borders
			double fx = s._Fx, fz = s._Fz, fl = Math.Sqrt(fx * fx + fz * fz);
			if (fl > 1e-6) { fx /= fl; fz /= fl; } else { fx = 0; fz = 0; }
			double half = LG_CELLS * LG_CELL * 0.5, ahead = half - 146.0;
			_lgX0 = (float)(Math.Floor((s._camX + fx * ahead - half) / LG_CELL) * LG_CELL);
			_lgZ0 = (float)(Math.Floor((s._camZ + fz * ahead - half) / LG_CELL) * LG_CELL);
			Array.Clear(_lgBuf); Array.Clear(_lgCount);
			int over = 0;
			for (int i = _lgGlobal; i < nl; i++) {
				float x = _lpos[i * 4], z = _lpos[i * 4 + 2], r = _lpos[i * 4 + 3];
				int c0 = (int)Math.Floor((x - r - _lgX0) / LG_CELL), c1 = (int)Math.Floor((x + r - _lgX0) / LG_CELL);
				int r0 = (int)Math.Floor((z - r - _lgZ0) / LG_CELL), r1 = (int)Math.Floor((z + r - _lgZ0) / LG_CELL);
				if (c1 < 0 || r1 < 0 || c0 >= LG_CELLS || r0 >= LG_CELLS) continue;
				if (c0 < 0) c0 = 0; if (r0 < 0) r0 = 0; if (c1 >= LG_CELLS) c1 = LG_CELLS - 1; if (r1 >= LG_CELLS) r1 = LG_CELLS - 1;
				for (int cz = r0; cz <= r1; cz++)
					for (int cx = c0; cx <= c1; cx++) {
						float nx = Math.Clamp(x, _lgX0 + cx * LG_CELL, _lgX0 + (cx + 1) * LG_CELL), nz = Math.Clamp(z, _lgZ0 + cz * LG_CELL, _lgZ0 + (cz + 1) * LG_CELL);
						if ((nx - x) * (nx - x) + (nz - z) * (nz - z) >= r * r) continue;
						int cell = cz * LG_CELLS + cx, n = _lgCount[cell];
						if (n >= LG_SLOTS) { over++; continue; }
						_lgCount[cell] = (byte)(n + 1);
						_lgBuf[((cz + (n >= 4 ? LG_CELLS : 0)) * LG_CELLS + cx) * 4 + (n & 3)] = (byte)(i + 1);
					}
			}
			if (over > 0 && _lgOverflow == 0)
				System.Diagnostics.Trace.TraceWarning("GpuRasterizer: the light grid had cells with more than " + LG_SLOTS + " lights; the extra ones are not drawn there");
			_lgOverflow = over;

			bool fresh = _lgTex == 0;
			if (fresh) _lgTex = Gl.GenTexture();
			Gl.ActiveTexture(TextureUnit.Texture0 + UNIT_LIGHT_GRID);
			Gl.BindTexture(TextureTarget.Texture2D, _lgTex);
			fixed (byte* p = _lgBuf) {
				if (fresh) {
					Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8, LG_CELLS, LG_CELLS * 2, 0, PixelFormat.Rgba, PixelType.UnsignedByte, p);
					Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
					Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
					Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
					Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
				} else Gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, LG_CELLS, LG_CELLS * 2, PixelFormat.Rgba, PixelType.UnsignedByte, p);
			}
			Gl.ActiveTexture(TextureUnit.Texture0);
			_lgOn = true;
		}

		// bound program; no-op without the uniforms
		private void SendLightGrid(uint prog, bool lit) {
			if (!_lgLocs.TryGetValue(prog, out var l)) {
				l = new LgLoc { Grid = Gl.GetUniformLocation(prog, "uLightGrid"), Xf = Gl.GetUniformLocation(prog, "uLightGridXf"), Global = Gl.GetUniformLocation(prog, "uLightGlobal") };
				_lgLocs[prog] = l;
			}
			if (l.Xf < 0) return;
			Gl.Uniform1(l.Grid, UNIT_LIGHT_GRID);
			if (_lgOn && lit) { Gl.Uniform4(l.Xf, _lgX0, _lgZ0, 1f / LG_CELL, (float)LG_CELLS); Gl.Uniform1(l.Global, _lgGlobal); }
			else { Gl.Uniform4(l.Xf, 0f, 0f, 0f, 0f); Gl.Uniform1(l.Global, 0); }
		}

		private void ClearLightGrid() {
			if (_lgTex != 0) { Gl.DeleteTexture(_lgTex); _lgTex = 0; }
			_lgLocs.Clear(); _lgOn = false;
		}
	}

	public partial class Lua3DScene {
		internal bool _lightGrid;
		/// <summary>Per-cell ranged point lights for terrain, EXT world program and grass tile. Off by default.
		/// Exact up to 8 lights per 8-unit cell.</summary>
		public void SetLightGrid(bool on) { _lightGrid = on; }
	}
}
