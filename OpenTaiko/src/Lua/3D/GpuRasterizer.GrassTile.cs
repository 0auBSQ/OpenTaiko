using System;
using System.Collections.Generic;
using System.Diagnostics;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	// grass tile pass: card buffer wrapped around the focus
	internal sealed partial class GpuRasterizer {
		private static string VSGT => ShaderSources.Get("grasstile.vert");
		private static string FSGT => ShaderSources.Get("grasstile.frag");
		private const int UNIT_GT_GROUND = 2, UNIT_GT_SPLAT = 5, UNIT_GT_HEIGHT = 10, UNIT_GT_MASK = 11;
		private const int GT_STRIDE = 4;          // x, z, r1, r2
		private const float GT_CELL = 12f;        // world units
		private const int GT_MAX = 600_000;
		private const int GT_BLOCK = 4;           // heightfield cells per block side

		private sealed class GtProg {
			public uint Prog; public FrameLoc Frame;
			public int Time, Tile, Field, SplatP, P, Cell, AvgGround, AvgAtlas, Terr, SunDir, SunCol, Amb, NumLights, LightPosR, LightCol;
			public int Height, Splat, Ground, TintMap, Mask, AtlasTex, LightVP, Shadow, ShadowF;
		}
		private GtProg? _gt, _gtSh;
		private bool _gtFailed;
		private uint _gtVao, _gtVbo, _gtHeightTex;
		private int _gtBufSeen = -1, _gtHeightSeen = -1;
		private float _gtSize; private int _gtGrid;
		private struct GtCell { public int Start, Count; public int[] Q; }   // Q[i]: cards with thinning number < i / 16
		private GtCell[] _gtCellList = Array.Empty<GtCell>();
		private readonly List<(int start, int count, double depth)> _gtRuns = new();
		private float[] _gtLo = Array.Empty<float>(), _gtHi = Array.Empty<float>();
		private int _gtBW, _gtBH;

		private GtProg? GtFor(bool shadow) {
			if (_gtFailed) return null;
			var have = shadow ? _gtSh : _gt;
			if (have != null) return have;
			try {
				string head = "#version 300 es\n" + (shadow ? "#define SHADOWS\n" : "");
				var gp = new GtProg { Prog = CachedProg(Ascii(head + VSGT), Ascii("#version 300 es\n" + FSGT)) };
				int g(string n) => Gl.GetUniformLocation(gp.Prog, n);
				gp.Frame = FrameLoc.Of(Gl, gp.Prog);
				gp.Time = g("uTime"); gp.Tile = g("uTile"); gp.Field = g("uField"); gp.SplatP = g("uSplatP");
				gp.P = g("uP[0]"); gp.Cell = g("uCell[0]"); gp.AvgGround = g("uAvgGround"); gp.AvgAtlas = g("uAvgAtlas"); gp.Terr = g("uTerr[0]");
				gp.SunDir = g("uSunDir"); gp.SunCol = g("uSunCol"); gp.Amb = g("uAmb");
				gp.NumLights = g("uNumLights"); gp.LightPosR = g("uLightPosR[0]"); gp.LightCol = g("uLightCol[0]");
				gp.Height = g("uHeight"); gp.Splat = g("uSplat"); gp.Ground = g("uGround"); gp.TintMap = g("uTintMap"); gp.Mask = g("uMask");
				gp.AtlasTex = g("uAtlasTex"); gp.LightVP = g("uLightVP"); gp.Shadow = g("uShadow"); gp.ShadowF = g("uShadowF");
				if (shadow) _gtSh = gp; else _gt = gp;
				return gp;
			} catch (Exception e) {
				_gtFailed = true; LastShaderError = "grass tile: " + e.Message;
				Trace.TraceError("The grass tile program failed to compile; no grass tile: " + e);
				return null;
			}
		}

		private unsafe void EnsureGrassTile(Lua3DScene s) {
			if (_gtVao == 0) {
				_gtVao = Gl.GenVertexArray(); _gtVbo = Gl.GenBuffer();
				Gl.BindVertexArray(_gtVao);
				Gl.EnableVertexAttribArray(0); Gl.VertexAttribFormat(0, 4, VertexAttribType.Float, false, 0); Gl.VertexAttribBinding(0, 0);
				Gl.VertexBindingDivisor(0, 1);
				Gl.BindVertexArray(0);
				_gtBufSeen = -1;
			}
			if (_gtBufSeen != s._gtBufRev) {
				_gtBufSeen = s._gtBufRev;
				float density = Math.Clamp(s._gtP[0], 0f, 40f) * Math.Clamp(s._gtP[1], 0.05f, 4f);
				float end = Math.Clamp(s._gtP[5], 8f, 250f);
				int grid = (int)Math.Ceiling((end * 2 + GT_CELL * 2) / GT_CELL);
				float size = grid * GT_CELL;
				// jittered grid: no rows
				int m = (int)Math.Ceiling(Math.Sqrt(Math.Min(density * size * (double)size, GT_MAX)));
				int n = m * m;
				_gtSize = size; _gtGrid = grid; s._gtCount = n;
				_gtCellList = new GtCell[grid * grid];
				if (n == 0) { for (int i = 0; i < _gtCellList.Length; i++) _gtCellList[i].Q = new int[17]; return; }
				var rnd = new Random(20261115);
				var keys = new double[n]; var raw = new float[n * GT_STRIDE]; var order = new int[n];
				double step = size / (double)m;
				for (int i = 0; i < n; i++) {
					float x = (float)((i % m + rnd.NextDouble()) * step), z = (float)((i / m + rnd.NextDouble()) * step);
					float r1 = (float)rnd.NextDouble(), r2 = (float)rnd.NextDouble();
					int c = Math.Min(grid - 1, (int)(z / GT_CELL)) * grid + Math.Min(grid - 1, (int)(x / GT_CELL));
					keys[i] = c + (double)r1; order[i] = i;
					raw[i * 4] = x; raw[i * 4 + 1] = z; raw[i * 4 + 2] = r1; raw[i * 4 + 3] = r2;
				}
				Array.Sort(keys, order);
				var inst = new float[n * GT_STRIDE];
				for (int i = 0; i < n; i++) Array.Copy(raw, order[i] * 4, inst, i * 4, 4);
				for (int i = 0; i < _gtCellList.Length; i++) _gtCellList[i] = new GtCell { Start = -1, Q = new int[17] };
				for (int i = 0; i < n; i++) {
					int c = (int)keys[i]; ref var cell = ref _gtCellList[c];
					if (cell.Start < 0) cell.Start = i;
					cell.Count++;
					int q = (int)((keys[i] - c) * 16);
					for (int j = q + 1; j <= 16; j++) cell.Q[j]++;
				}
				Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _gtVbo);
				Gl.BufferData<float>(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)inst, BufferUsageARB.StaticDraw);
			}
			if (_gtHeightSeen != s._gtHeightRev) {
				_gtHeightSeen = s._gtHeightRev;
				var hts = s._gtHeights;
				if (hts == null) { _gtLo = _gtHi = Array.Empty<float>(); _gtBW = _gtBH = 0; return; }
				int w = s._gtHW, h = s._gtHH;
				if (_gtHeightTex == 0) _gtHeightTex = Gl.GenTexture();
				Gl.BindTexture(TextureTarget.Texture2D, _gtHeightTex);
				fixed (float* p = hts) Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.R32f, (uint)w, (uint)h, 0, PixelFormat.Red, PixelType.Float, p);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
				_gtBW = (w - 1 + GT_BLOCK - 1) / GT_BLOCK; _gtBH = (h - 1 + GT_BLOCK - 1) / GT_BLOCK;
				_gtLo = new float[_gtBW * _gtBH]; _gtHi = new float[_gtBW * _gtBH];
				for (int bz = 0; bz < _gtBH; bz++)
					for (int bx = 0; bx < _gtBW; bx++) {
						float lo = float.MaxValue, hi = float.MinValue;
						int z1 = Math.Min(h - 1, (bz + 1) * GT_BLOCK), x1 = Math.Min(w - 1, (bx + 1) * GT_BLOCK);
						for (int z = bz * GT_BLOCK; z <= z1; z++)
							for (int x = bx * GT_BLOCK; x <= x1; x++) { float v = hts[z * w + x]; if (v < lo) lo = v; if (v > hi) hi = v; }
						_gtLo[bz * _gtBW + bx] = lo; _gtHi[bz * _gtBW + bx] = hi;
					}
			}
		}

		private void GroundRange(Lua3DScene s, double x0, double z0, double x1, double z1, out double lo, out double hi) {
			double k = 1.0 / (s._gtHCell * GT_BLOCK);
			int bx0 = Math.Clamp((int)Math.Floor(x0 * k), 0, _gtBW - 1), bx1 = Math.Clamp((int)Math.Floor(x1 * k), 0, _gtBW - 1);
			int bz0 = Math.Clamp((int)Math.Floor(z0 * k), 0, _gtBH - 1), bz1 = Math.Clamp((int)Math.Floor(z1 * k), 0, _gtBH - 1);
			lo = double.MaxValue; hi = double.MinValue;
			for (int bz = bz0; bz <= bz1; bz++)
				for (int bx = bx0; bx <= bx1; bx++) {
					int i = bz * _gtBW + bx;
					if (_gtLo[i] < lo) lo = _gtLo[i];
					if (_gtHi[i] > hi) hi = _gtHi[i];
				}
		}

		private void GatherGrassTile(Lua3DScene s) {
			_gtRuns.Clear();
			if (!s._gtOn || s._rttPass || s._gtHeights == null || s._gtAtlas < 0 || s._gtSplat < 0 || s._gtGround < 0) return;
			if (s._gtOwner > 0 && (!s._objects.TryGetValue(s._gtOwner, out var owner) || !owner.Visible)) return;
			EnsureGrassTile(s);
			s._gtDrawn = 0;
			if (_gtBW == 0 || s._gtCount == 0) return;
			double fx = s._gtFx, fz = s._gtFz, size = _gtSize, half = size * 0.5, hc = GT_CELL * 0.5;
			double start = s._gtP[4], end = Math.Max(Math.Clamp(s._gtP[5], 8f, 250f), start + 1);
			const double PAD = 1.3, TALL = 1.6;   // card reach: side, up
			int grid = _gtGrid;
			for (int cz = 0; cz < grid; cz++)
				for (int cx = 0; cx < grid; cx++) {
					ref var c = ref _gtCellList[cz * grid + cx];
					if (c.Count == 0) continue;
					double wx = fx + Wrap((cx + 0.5) * GT_CELL - fx + half, size) - half;
					double wz = fz + Wrap((cz + 0.5) * GT_CELL - fz + half, size) - half;
					double dx = Math.Max(Math.Abs(wx - fx) - hc, 0), dz = Math.Max(Math.Abs(wz - fz) - hc, 0);
					double d = Math.Sqrt(dx * dx + dz * dz);
					if (d >= end) continue;   // also drops wrap-split cells
					double t = Math.Clamp((d - start) / (end - start), 0, 1);
					double thr = 1 - t * t * (3 - 2 * t);
					int n = c.Q[Math.Min(16, (int)Math.Ceiling(thr * 16))];
					if (n <= 0) continue;
					GroundRange(s, wx - hc, wz - hc, wx + hc, wz + hc, out double lo, out double hi);
					double cy = (lo + hi + TALL) * 0.5, hy = (hi + TALL - lo) * 0.5 + 0.2;
					if (!_view.BoxInView(wx, cy, wz, hc + PAD, hy, hc + PAD)) continue;
					Reach(_view.FarDepth(wx, cy, wz, hc + PAD, hy, hc + PAD));
					_gtRuns.Add((c.Start, n, _view.Depth(wx, cy, wz)));
					s._gtDrawn += n;
				}
			_gtRuns.Sort(_gtNearFirst);   // cut-out cards fill depth early
		}
		private static readonly Comparison<(int start, int count, double depth)> _gtNearFirst = (a, b) => a.depth.CompareTo(b.depth);
		private static double Wrap(double v, double m) { v %= m; return v < 0 ? v + m : v; }

		private void DrawGrassTile(Lua3DScene s) {
			if (_gtRuns.Count == 0) return;
			var gp = GtFor(_worldShadow);
			if (gp == null || _gtHeightTex == 0) return;
			Gl.ActiveTexture(TextureUnit.Texture0);
			uint atlas = SpriteFor(s, s._gtAtlas), splat = SpriteFor(s, s._gtSplat), ground = TexFor(s, s._gtGround);
			if (atlas == 0 || splat == 0 || ground == 0) return;
			uint tint = s._gtTint >= 0 ? TexFor(s, s._gtTint) : 0, mask = s._gtMask >= 0 ? TexFor(s, s._gtMask) : 0;
			if (_neutralTint == 0) _neutralTint = OneTexel(128, 128, 128);
			if (tint == 0) tint = _neutralTint;
			if (mask == 0) mask = _neutralTint;
			Gl.UseProgram(gp.Prog);
			SetCamera(gp.Frame, s); SetFog(gp.Frame, s);
			SetSunAndLights(s, gp.SunDir, gp.SunCol, gp.Amb, gp.NumLights, gp.LightPosR, gp.LightCol);
			SendLightGrid(gp.Prog, s._useLights);
			Gl.Uniform1(gp.Time, (float)s._shaderTime);
			Gl.Uniform4(gp.Tile, (float)s._gtFx, (float)s._gtFz, _gtSize, 1f / s._gtMapSize);
			Gl.Uniform4(gp.Field, (float)(s._gtHW - 1), (float)(s._gtHH - 1), 1f / s._gtHCell, s._gtTexScale);
			Gl.Uniform4(gp.SplatP, 1f / s._gtSplatW, 1f / s._gtSplatH, s._gtP[36], s._gtP[37]);
			Gl.Uniform4(gp.P, 10u, (ReadOnlySpan<float>)s._gtP);
			Gl.Uniform4(gp.Cell, 16u, (ReadOnlySpan<float>)s._gtCells);
			Gl.Uniform4(gp.Terr, 6u, (ReadOnlySpan<float>)s._terrP);
			var (ar, ag, ab) = TexAverage(s, s._gtGround);
			Gl.Uniform3(gp.AvgGround, ar, ag, ab);
			Gl.Uniform3(gp.AvgAtlas, s._gtAtlasAvg[0], s._gtAtlasAvg[1], s._gtAtlasAvg[2]);
			Gl.Uniform1(gp.AtlasTex, 0); Gl.Uniform1(gp.Ground, UNIT_GT_GROUND); Gl.Uniform1(gp.Splat, UNIT_GT_SPLAT);
			Gl.Uniform1(gp.TintMap, UNIT_TERR_TINT); Gl.Uniform1(gp.Height, UNIT_GT_HEIGHT); Gl.Uniform1(gp.Mask, UNIT_GT_MASK);
			if (_worldShadow) {
				Gl.UniformMatrix4(gp.LightVP, 1, false, (ReadOnlySpan<float>)_lightVP);
				Gl.Uniform1(gp.Shadow, 1); Gl.Uniform2(gp.ShadowF, s._shadowSoft, s._shadowFade);
			}
			Gl.ActiveTexture(TextureUnit.Texture0 + UNIT_GT_GROUND); Gl.BindTexture(TextureTarget.Texture2D, ground);
			Gl.ActiveTexture(TextureUnit.Texture0 + UNIT_GT_SPLAT); Gl.BindTexture(TextureTarget.Texture2D, splat);
			Gl.ActiveTexture(TextureUnit.Texture0 + UNIT_TERR_TINT); Gl.BindTexture(TextureTarget.Texture2D, tint);
			Gl.ActiveTexture(TextureUnit.Texture0 + UNIT_GT_HEIGHT); Gl.BindTexture(TextureTarget.Texture2D, _gtHeightTex);
			Gl.ActiveTexture(TextureUnit.Texture0 + UNIT_GT_MASK); Gl.BindTexture(TextureTarget.Texture2D, mask);
			Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, atlas);
			Gl.BindVertexArray(_gtVao);
			Gl.Disable(EnableCap.Blend); Gl.DepthMask(true);
			foreach (var (start, count, _) in _gtRuns) {
				Gl.BindVertexBuffer(0, _gtVbo, (nint)(start * GT_STRIDE * sizeof(float)), (uint)(GT_STRIDE * sizeof(float)));
				Gl.DrawArraysInstanced(PrimitiveType.TriangleStrip, 0, 4, (uint)count);
			}
			Gl.UseProgram(WorldProg); Gl.BindVertexArray(_vao);
		}

		private void ClearGrassTile() {
			_gt = _gtSh = null; _gtFailed = false;
			if (_gtVao != 0) { Gl.DeleteVertexArray(_gtVao); _gtVao = 0; }
			if (_gtVbo != 0) { Gl.DeleteBuffer(_gtVbo); _gtVbo = 0; }
			if (_gtHeightTex != 0) { Gl.DeleteTexture(_gtHeightTex); _gtHeightTex = 0; }
			_gtBufSeen = _gtHeightSeen = -1;
			_gtCellList = Array.Empty<GtCell>(); _gtRuns.Clear();
		}
	}
}
