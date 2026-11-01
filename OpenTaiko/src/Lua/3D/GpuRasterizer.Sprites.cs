using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	// Billboard and quad sprites, rebuilt every pass.
	internal sealed partial class GpuRasterizer {
		private readonly List<float> _sprVerts = new(2048);
		private struct SprBatch { public SceneObject Obj; public int Cutout, Start, Count, Pass; public uint Tex; public double Key; }   // Key: squared distance from the eye (transparent billboards)
		private readonly List<SprBatch> _sprBatches = new(64);

		private void BuildSprites(Lua3DScene s) {
			_sprVerts.Clear(); _sprBatches.Clear();
			foreach (var o in s._objects.Values) {
				if ((o.Kind != 3 && o.Kind != 4) || o.N <= 0 || !o.Visible) continue;
				var d = o.D; var t = o.Transform;
				int curCut = -1; uint curTex = uint.MaxValue; int start = _sprVerts.Count / STRIDE;
				void Flush() {
					int cnt = _sprVerts.Count / STRIDE - start;
					if (cnt > 0) _sprBatches.Add(new SprBatch { Obj = o, Cutout = curCut, Tex = curTex, Start = start, Count = cnt, Pass = o.Pass, Key = o.Pass == 1 && o.Kind == 3 ? SprKey(start, cnt) : 0 });
					start = _sprVerts.Count / STRIDE;
				}
				void Use(uint tex, int cut) { if (tex != curTex || cut != curCut) { Flush(); curTex = tex; curCut = cut; } }
				if (o.Kind == 4) {
					// sprite drawn on an EXPLICIT world quad (4 corners) — e.g. a shadow draped over the terrain
					for (int i = 0; i < o.N; i++) {
						int k = i * PrimLayout.SpriteQuadStride; uint gt = SpriteFor(s, (int)d[k + PrimLayout.SpriteQuadId]); if (gt == 0) continue;
						int cut = d[k + PrimLayout.SpriteQuadCutout] != 0 ? 1 : 0; Use(gt, cut);
						XfPt(t, d[k], d[k + 1], d[k + 2], out double q0x, out double q0y, out double q0z);
						XfPt(t, d[k + 3], d[k + 4], d[k + 5], out double q1x, out double q1y, out double q1z);
						XfPt(t, d[k + 6], d[k + 7], d[k + 8], out double q2x, out double q2y, out double q2z);
						XfPt(t, d[k + 9], d[k + 10], d[k + 11], out double q3x, out double q3y, out double q3z);
						ReachPt(q0x, q0y, q0z); ReachPt(q1x, q1y, q1z); ReachPt(q2x, q2y, q2z); ReachPt(q3x, q3y, q3z);
						Vtx(_sprVerts, q0x, q0y, q0z, 0, 1, 1, 0, 0, 0); Vtx(_sprVerts, q1x, q1y, q1z, 1, 1, 1, 0, 0, 0); Vtx(_sprVerts, q2x, q2y, q2z, 1, 0, 1, 0, 0, 0);
						Vtx(_sprVerts, q0x, q0y, q0z, 0, 1, 1, 0, 0, 0); Vtx(_sprVerts, q2x, q2y, q2z, 1, 0, 1, 0, 0, 0); Vtx(_sprVerts, q3x, q3y, q3z, 0, 0, 1, 0, 0, 0);
					}
					Flush(); continue;
				}
				for (int i = 0; i < o.N; i++) {
					int k = i * PrimLayout.SpriteStride; uint gt = SpriteFor(s, (int)d[k + PrimLayout.SpriteId]); if (gt == 0) continue;
					double w = d[k + PrimLayout.SpriteW], hgt = d[k + PrimLayout.SpriteH]; int bb = (int)d[k + PrimLayout.SpriteBillboard]; int cut = d[k + PrimLayout.SpriteCutout] != 0 ? 1 : 0;
					double cx, cy, cz;
					if (t == null) { cx = d[k]; cy = d[k + 1]; cz = d[k + 2]; }
					else { cx = t[0] * d[k] + t[1] * d[k + 1] + t[2] * d[k + 2] + t[3]; cy = t[4] * d[k] + t[5] * d[k + 1] + t[6] * d[k + 2] + t[7]; cz = t[8] * d[k] + t[9] * d[k + 1] + t[10] * d[k + 2] + t[11]; }
					double rx, ry, rz, ux, uy, uz;
					if (bb == 2) { rx = s._Rx; ry = s._Ry; rz = s._Rz; ux = s._Ux; uy = s._Uy; uz = s._Uz; }
					else if (bb == 1) { rx = s._Rx; ry = 0; rz = s._Rz; double rl = Math.Sqrt(rx * rx + rz * rz); if (rl > 1e-6) { rx /= rl; rz /= rl; } ux = 0; uy = 1; uz = 0; }
					else if (bb == 3) { rx = s._GsLz; ry = 0; rz = -s._GsLx; ux = s._GsLx * s._GsLen; uy = 0; uz = s._GsLz * s._GsLen; }   // ground-flat cast shadow: lies on XZ, stretched along the light
					else { rx = 1; ry = 0; rz = 0; ux = 0; uy = 1; uz = 0; }
					double hw = w * 0.5;
					double b0x = cx - rx * hw, b0y = cy - ry * hw, b0z = cz - rz * hw;
					double b1x = cx + rx * hw, b1y = cy + ry * hw, b1z = cz + rz * hw;
					double tx = ux * hgt, ty = uy * hgt, tz = uz * hgt;
					ReachPt(b0x, b0y, b0z); ReachPt(b1x, b1y, b1z); ReachPt(b0x + tx, b0y + ty, b0z + tz); ReachPt(b1x + tx, b1y + ty, b1z + tz);
					Use(gt, cut);
					Vtx(_sprVerts, b0x, b0y, b0z, 0, 1, 1, 0, 0, 0); Vtx(_sprVerts, b1x, b1y, b1z, 1, 1, 1, 0, 0, 0); Vtx(_sprVerts, b1x + tx, b1y + ty, b1z + tz, 1, 0, 1, 0, 0, 0);
					Vtx(_sprVerts, b0x, b0y, b0z, 0, 1, 1, 0, 0, 0); Vtx(_sprVerts, b1x + tx, b1y + ty, b1z + tz, 1, 0, 1, 0, 0, 0); Vtx(_sprVerts, b0x + tx, b0y + ty, b0z + tz, 0, 0, 1, 0, 0, 0);
				}
				Flush();
			}
			if (_sprVerts.Count > 0) { Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _spriteVbo); Gl.BufferData<float>(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)CollectionsMarshal.AsSpan(_sprVerts), BufferUsageARB.StreamDraw); }
		}
		// squared distance from the eye to the centre of the box around the sprite vertices [start, start + count)
		private double SprKey(int start, int count) {
			float mnx = float.MaxValue, mny = float.MaxValue, mnz = float.MaxValue, mxx = float.MinValue, mxy = float.MinValue, mxz = float.MinValue;
			for (int i = start * STRIDE, end = (start + count) * STRIDE; i < end; i += STRIDE) {
				float x = _sprVerts[i], y = _sprVerts[i + 1], z = _sprVerts[i + 2];
				if (x < mnx) mnx = x; if (x > mxx) mxx = x;
				if (y < mny) mny = y; if (y > mxy) mxy = y;
				if (z < mnz) mnz = z; if (z > mxz) mxz = z;
			}
			return _view.DistSq((mnx + (double)mxx) * 0.5, (mny + (double)mxy) * 0.5, (mnz + (double)mxz) * 0.5);
		}

		private void DrawSprite(SprBatch bt) {
			var o = bt.Obj;
			UModel(null);
			UMode(2); UCutout(bt.Cutout); ULit(0);
			UTint((float)o.TintR, (float)o.TintG, (float)o.TintB); UAlpha(o.A / 255.0f);
			Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, bt.Tex);
			Gl.DrawArrays(PrimitiveType.Triangles, bt.Start, (uint)bt.Count);
		}
	}
}
