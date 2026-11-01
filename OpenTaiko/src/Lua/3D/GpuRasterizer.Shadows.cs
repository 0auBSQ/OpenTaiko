using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	// The sun shadow map pass.
	internal sealed partial class GpuRasterizer {
		// ── sun SHADOW MAP. The lit fragment shader is compiled in TWO variants: the DEFAULT _prog has NO shadow
		// code at all (so stages that don't use shadows are byte-identical to before — no predicated texture
		// taps), and _progShadow adds the depth-map sampling. We SWITCH PROGRAMS per frame (BindWorldProgram)
		// rather than branch on a uniform inside one shared shader: a uniform branch is PREDICATED on GLES/ANGLE,
		// so the 4 shadow taps + the light-space transform would run for EVERY fragment of EVERY stage — that was
		// the 10× regression. Variants confine the cost to scenes that actually want shadows.
		private uint _progDepth, _progDepthSpr, _progShadow, _shadowFbo, _shadowTex;
		private int _uDLightVP, _uDModel, _uDSLightVP, _uDSTex, _uDSModel;
		private int _uLightVP, _uShadow, _uShadowTexel;
		private bool _shadowOk = true;
		private bool _worldShadow;       // which world variant is currently bound (so a mid-pass rebind matches)
		private const int SHADOW_SIZE = 1920;   // full-HD shadow map over a tight player-centred box (HALF 60) = ~16 texels/unit
		// shadow half-extent now lives on the scene (Lua3DScene._shadowHalf, SetShadowArea)
		private readonly float[] _lightVP = new float[16];

		// sun-facing silhouette casters for billboard characters, rebuilt per shadow frame: a camera-facing quad
		// presents the wrong outline to the sun (and thins/warps as you orbit), so the depth pass instead draws a
		// quad turned to face the sun's azimuth → a stable, correctly-shaped cast shadow on the ground.
		private uint _casterVbo;
		// the silhouettes' hash, the frame it last changed and whether they count as static (see _shadowSig); the hash of
		// what _casterVbo holds
		private ulong _casterSig, _casterVboSig; private int _casterAt = int.MinValue / 2;
		private bool _casterSeen, _casterStatic, _casterVboSet;
		private struct CBatch { public uint Tex; public int Sprite, Start, Count; }
		private readonly List<float> _casterVerts = new(512);
		private readonly List<CBatch> _casterBatches = new(16);

		// The shadow map is redrawn only when something it shows changed, told by two hashes. The static one covers the
		// light matrix, the merge buckets, and the objects and sprite silhouettes whose cast has not changed for
		// SHADOW_SETTLE frames; the dynamic one the objects and silhouettes that changed since. A static change redraws the
		// whole map. A dynamic change only mends it: the texels the dynamic casters covered and cover now are copied back
		// from a map of the static casters alone, then the dynamic casters are drawn over them.
		private bool _shadowValid; private ulong _shadowSig;
		private readonly List<MergeBuf> _shadowBuckets = new(256);
		// solid: every batch draws in the plain depth program (flat, palette, or a texture with no cut-out texel)
		private readonly List<(SceneObject o, ObjCache c, bool solid)> _shadowObjs = new(256), _shadowDyn = new(16);
		private readonly List<SceneObject> _casters = new(256);   // lit opaque objects that may cast, before view culling
		private const int SHADOW_SETTLE = 60;
		private ulong _sigStatic, _sigDyn, _shadowDynSig;
		private uint _shadowStaticFbo, _shadowStaticTex; private bool _shadowStaticOk = true, _shadowStaticValid; private ulong _shadowStaticSig;
		// texel rectangles, X1 and Y1 exclusive: what the dynamic casters cover now, what they covered in the map, the copy
		internal struct SRect { public int X0, Y0, X1, Y1; }
		private List<SRect> _dynRects = new(16), _drawnRects = new(16);
		private readonly List<SRect> _mend = new(16);

		private bool EnsureShadowFbo() {
			if (!_shadowOk) return false;
			if (_shadowFbo != 0) return true;
			_shadowValid = false;
			_shadowOk = MakeShadowTarget(out _shadowTex, out _shadowFbo);
			return _shadowOk;
		}
		// the static casters' map, made the first time dynamic casters mend the map
		private bool EnsureShadowStatic() {
			if (!_shadowStaticOk) return false;
			if (_shadowStaticFbo != 0) return true;
			_shadowStaticValid = false;
			_shadowStaticOk = MakeShadowTarget(out _shadowStaticTex, out _shadowStaticFbo);
			return _shadowStaticOk;
		}
		private unsafe bool MakeShadowTarget(out uint tex, out uint fbo) {
			tex = fbo = 0;
			try {
				tex = Gl.GenTexture();
				Gl.BindTexture(TextureTarget.Texture2D, tex);
				Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.DepthComponent24, SHADOW_SIZE, SHADOW_SIZE, 0, PixelFormat.DepthComponent, PixelType.UnsignedInt, (void*)0);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
				fbo = Gl.GenFramebuffer();
				Gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
				Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, tex, 0);
				var st = Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
				Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
				if (st != GLEnum.FramebufferComplete) { Trace.TraceError("Shadow FBO incomplete: " + st); return false; }
				return true;
			} catch (Exception e) { Trace.TraceError("Shadow FBO failed: " + e); return false; }
		}

		// orthographic light view-projection (column-major), looking along the sun, covering an area in front of
		// the camera. Maps a world point so clip.xy∈[-1,1] over ±HALF and clip.z (=p.z after *0.5+0.5) over [0,1].
		private void BuildLightVP(Lua3DScene s) {
			const double DEPTH = 260.0; double HALF = s._shadowHalf;
			// centre on the player (SetShadowFocus) when given — the faked-ortho iso camera sits far back, so
			// camera+forward*38 lands behind the action and the map would miss the player entirely.
			double fxC, fyC, fzC;
			if (s._shadowFocusSet) { fxC = s._sfx; fyC = s._sfy; fzC = s._sfz; }
			else { fxC = s._camX + s._Fx * 38; fyC = s._camY + s._Fy * 38; fzC = s._camZ + s._Fz * 38; }
			double lx = s._sunX, ly = s._sunY, lz = s._sunZ;
			double ll = Math.Sqrt(lx * lx + ly * ly + lz * lz); if (ll < 1e-6) { lx = 0; ly = 1; lz = 0; } else { lx /= ll; ly /= ll; lz /= ll; }
			double fX = -lx, fY = -ly, fZ = -lz;
			double ex = fxC + lx * DEPTH * 0.5, ey = fyC + ly * DEPTH * 0.5, ez = fzC + lz * DEPTH * 0.5;
			double upx = 0, upy = 1, upz = 0; if (Math.Abs(fY) > 0.95) { upx = 0; upy = 0; upz = 1; }
			double rX = fY * upz - fZ * upy, rY = fZ * upx - fX * upz, rZ = fX * upy - fY * upx;
			double rl = Math.Sqrt(rX * rX + rY * rY + rZ * rZ); rX /= rl; rY /= rl; rZ /= rl;
			double uX = rY * fZ - rZ * fY, uY = rZ * fX - rX * fZ, uZ = rX * fY - rY * fX;
			double sH = 1.0 / HALF, sD = 2.0 / DEPTH;
			double er = ex * rX + ey * rY + ez * rZ, eu = ex * uX + ey * uY + ez * uZ, ef = ex * fX + ey * fY + ez * fZ;
			// TEXEL-SNAP the light-space origin: the map is player-centred, so without snapping every
			// step re-rasterizes the depth map on a sub-texel offset and all shadow edges shimmer/crawl
			double texel = (2.0 * HALF) / SHADOW_SIZE;
			er = Math.Floor(er / texel) * texel;
			eu = Math.Floor(eu / texel) * texel;
			var m = _lightVP;   // column-major (m[col*4+row])
			m[0] = (float)(rX * sH); m[1] = (float)(uX * sH); m[2] = (float)(fX * sD); m[3] = 0;
			m[4] = (float)(rY * sH); m[5] = (float)(uY * sH); m[6] = (float)(fY * sD); m[7] = 0;
			m[8] = (float)(rZ * sH); m[9] = (float)(uZ * sH); m[10] = (float)(fZ * sD); m[11] = 0;
			m[12] = (float)(-er * sH); m[13] = (float)(-eu * sH); m[14] = (float)(-ef * sD - 1.0); m[15] = 1;
		}

		// whether a world box (centre, half extents) reaches into the shadow map's light box (clip -1..1 on all
		// three axes; the depth pass clips anything outside it anyway)
		private bool BoxInLight(double cx, double cy, double cz, double hx, double hy, double hz) {
			var m = _lightVP;
			for (int i = 0; i < 3; i++) {
				double c = m[i] * cx + m[4 + i] * cy + m[8 + i] * cz + m[12 + i];
				double e = Math.Abs(m[i]) * hx + Math.Abs(m[4 + i]) * hy + Math.Abs(m[8 + i]) * hz;
				if (c - e > 1.0 || c + e < -1.0) return false;
			}
			return true;
		}
		// the same for a world sphere: each light axis is a scaled unit vector, so the sphere spans radius x its scale
		private bool SphereInLight(double cx, double cy, double cz, double r) {
			var m = _lightVP;
			for (int i = 0; i < 3; i++) {
				double c = m[i] * cx + m[4 + i] * cy + m[8 + i] * cz + m[12 + i];
				double e = r * Math.Sqrt(m[i] * m[i] + m[4 + i] * m[4 + i] + m[8 + i] * m[8 + i]);
				if (c - e > 1.0 || c + e < -1.0) return false;
			}
			return true;
		}

		// Build SUN-FACING silhouette quads for the upright billboard sprites (characters/NPCs/props), used only
		// as shadow-map casters. A standing billboard is camera-facing in the main pass, which would present an
		// edge-on or skewed outline to the sun (a thin/warped, orbit-dependent shadow). Here each sprite becomes a
		// vertical quad whose width axis is perpendicular to the sun's azimuth, so the sun sees its full outline
		// and the cast shadow keeps the character's shape and stretches away from the light. Grouped by texture
		// for the alpha-test discard in the depth-sprite program.
		private void BuildCasters(Lua3DScene s) {
			_casterVerts.Clear(); _casterBatches.Clear();
			double sdx = s._sunX, sdz = s._sunZ;
			double shl = Math.Sqrt(sdx * sdx + sdz * sdz);
			double rx, rz; if (shl > 1e-6) { rx = sdz / shl; rz = -sdx / shl; } else { rx = 1; rz = 0; }   // horizontal ⟂ to sun
			foreach (var o in s._objects.Values) {
				if (o.Kind != 3 || o.N <= 0 || !o.Visible || !o.CastShadow) continue;   // only billboard sprites (Kind 3); Kind 4 = flat decals; CastShadow=false skips UI markers/bubbles
				var d = o.D; var t = o.Transform;
				uint curTex = uint.MaxValue; int curSprite = -1, start = _casterVerts.Count / STRIDE;
				void Flush() { int cnt = _casterVerts.Count / STRIDE - start; if (cnt > 0) _casterBatches.Add(new CBatch { Tex = curTex, Sprite = curSprite, Start = start, Count = cnt }); start = _casterVerts.Count / STRIDE; }
				for (int i = 0; i < o.N; i++) {
					int k = i * PrimLayout.SpriteStride; uint gt = SpriteFor(s, (int)d[k + PrimLayout.SpriteId]); if (gt == 0) continue;
					int bb = (int)d[k + PrimLayout.SpriteBillboard]; if (bb != 1 && bb != 2) continue;   // only UPRIGHT billboards cast (skip ground-flat bb=3)
					double w = d[k + PrimLayout.SpriteW], hgt = d[k + PrimLayout.SpriteH];
					double cx, cy, cz;
					if (t == null) { cx = d[k]; cy = d[k + 1]; cz = d[k + 2]; }
					else { cx = t[0] * d[k] + t[1] * d[k + 1] + t[2] * d[k + 2] + t[3]; cy = t[4] * d[k] + t[5] * d[k + 1] + t[6] * d[k + 2] + t[7]; cz = t[8] * d[k] + t[9] * d[k + 1] + t[10] * d[k + 2] + t[11]; }
					if (gt != curTex) { Flush(); curTex = gt; curSprite = (int)d[k + PrimLayout.SpriteId]; }
					double hw = w * 0.5;
					double b0x = cx - rx * hw, b0z = cz - rz * hw, b1x = cx + rx * hw, b1z = cz + rz * hw;
					Vtx(_casterVerts, b0x, cy, b0z, 0, 1, 1, 0, 0, 0); Vtx(_casterVerts, b1x, cy, b1z, 1, 1, 1, 0, 0, 0); Vtx(_casterVerts, b1x, cy + hgt, b1z, 1, 0, 1, 0, 0, 0);
					Vtx(_casterVerts, b0x, cy, b0z, 0, 1, 1, 0, 0, 0); Vtx(_casterVerts, b1x, cy + hgt, b1z, 1, 0, 1, 0, 0, 0); Vtx(_casterVerts, b0x, cy + hgt, b0z, 0, 0, 1, 0, 0, 0);
				}
				Flush();
			}
		}

		// The shadow pass's casters, split into static and dynamic (see _shadowSig), with the two hashes and the texels the
		// dynamic casters cover. The hashes cover everything that decides the map: the light matrix, which merge buckets
		// and objects cast, their geometry and transforms, and the sprite silhouettes.
		private void GatherCasters(Lua3DScene s) {
			_shadowBuckets.Clear(); _shadowObjs.Clear(); _shadowDyn.Clear(); _dynRects.Clear();
			ulong h = 14695981039346656037UL;
			foreach (float f in _lightVP) h = Mix(h, (uint)BitConverter.SingleToInt32Bits(f));
			// only the geometry inside the light box casts into the map → skip merge buckets whose vertex box
			// misses it; the box is tested in light space, so tall casters upwind of a low sun stay.
			foreach (var kv in _merge) {
				var mb = kv.Value;
				if (mb.Count == 0 || !HasTex(s, kv.Key.tex)) continue;   // a bucket that draws nothing casts nothing
				if (!BoxInLight(mb.Cx, mb.Cy, mb.Cz, mb.Hx, mb.Hy, mb.Hz)) continue;
				_shadowBuckets.Add(mb); h = Mix(h, (ulong)mb.Rev);
			}
			_sigStatic = h; _sigDyn = 14695981039346656037UL;
			// splat-shaded terrain draws solo (not merged) but must still cast — hills shadow valleys
			foreach (var o in s._objects.Values) {
				if (o.SplatSprite < 0 || !o.Visible || o.N <= 0 || !o.CastShadow) continue;
				if (o.WorldSphere(out double scx, out double scy, out double scz, out double sr) && !SphereInLight(scx, scy, scz, sr)) continue;
				AddCaster(s, o);
			}
			// the other lit opaque objects outside the merge (models and props placed by a transform, tinted,
			// emissive or recoloured objects, flat quads) cast with their own model matrix, on screen or not. Unlit
			// objects ignore the sun and cast nothing.
			foreach (var o in _casters) {
				if (o.WorldSphere(out double ocx, out double ocy, out double ocz, out double orr) && !SphereInLight(ocx, ocy, ocz, orr)) continue;
				AddCaster(s, o);
			}
			BuildCasters(s);   // sun-facing silhouette quads for the characters (camera-facing sprites cast wrong)
			h = 14695981039346656037UL;
			foreach (float f in _casterVerts) h = Mix(h, (uint)BitConverter.SingleToInt32Bits(f));
			foreach (var bt in _casterBatches) {
				int rev = s._sprites.TryGetValue(bt.Sprite, out var se) ? se.Rev : 0;
				h = Mix(h, bt.Tex); h = Mix(h, (uint)bt.Count); h = Mix(h, (uint)rev);
			}
			if (!_casterSeen) { _casterSeen = true; _casterSig = h; }
			else if (h != _casterSig) { _casterSig = h; _casterAt = _frame; }
			_casterStatic = _frame - _casterAt >= SHADOW_SETTLE;
			if (_casterStatic) { _sigStatic = Mix(_sigStatic, h); return; }
			_sigDyn = Mix(_sigDyn, h);
			var v = CollectionsMarshal.AsSpan(_casterVerts);
			for (int k = 0; k + 6 * STRIDE <= v.Length; k += 6 * STRIDE) {   // each silhouette is six vertices
				double x0 = double.MaxValue, y0 = double.MaxValue, x1 = double.MinValue, y1 = double.MinValue;
				for (int i = k; i < k + 6 * STRIDE; i += STRIDE) LightBox(_lightVP, v[i], v[i + 1], v[i + 2], ref x0, ref y0, ref x1, ref y1);
				if (TexelRect(x0, y0, x1, y1, out var r)) _dynRects.Add(r);
			}
		}
		private void AddCaster(Lua3DScene s, SceneObject o) {
			var c = EnsureObj(o);
			if (c.Batches.Count == 0) return;
			ulong h = 14695981039346656037UL;
			h = Mix(h, (uint)RuntimeHelpers.GetHashCode(o)); h = Mix(h, (uint)o.GeomVersion); h = Mix(h, (uint)c.Serial);
			var t = o.Transform;
			if (t != null) foreach (double v in t) h = Mix(h, (ulong)BitConverter.DoubleToInt64Bits(v));
			bool solid = true;
			if (o.SplatSprite < 0)   // the batches whose texture is missing are skipped, the others cast through their texture's alpha
				for (int i = 0; i < c.Batches.Count; i++) {
					var bt = c.Batches[i];
					if (bt.Mode == 0 || bt.Pal) continue;
					if (!HasTex(s, bt.TexId)) { h = Mix(h, (uint)i + 1); solid = false; continue; }
					if (s._tex.TryGetValue(bt.TexId, out var te)) h = Mix(h, (uint)te.Rev);
					if (solid && !OpaqueTex(s, bt.TexId)) solid = false;
				}
			if (!c.ShadowSeen) { c.ShadowSeen = true; c.ShadowSig = h; }
			else if (h != c.ShadowSig) { c.ShadowSig = h; c.ShadowAt = _frame; }
			if (_frame - c.ShadowAt >= SHADOW_SETTLE) { _shadowObjs.Add((o, c, solid)); _sigStatic = Mix(_sigStatic, h); return; }
			_shadowDyn.Add((o, c, solid)); _sigDyn = Mix(_sigDyn, h);
			if (ObjTexels(_lightVP, o, out var r)) _dynRects.Add(r);
		}
		private static ulong Mix(ulong h, ulong v) => (h ^ (v + 0x9E3779B97F4A7C15UL + (h << 6) + (h >> 2))) * 0x100000001B3UL;

		// widen a light-space xy box by a world point, through the light matrix m
		private static void LightBox(float[] m, double x, double y, double z, ref double x0, ref double y0, ref double x1, ref double y1) {
			double lx = m[0] * x + m[4] * y + m[8] * z + m[12], ly = m[1] * x + m[5] * y + m[9] * z + m[13];
			if (lx < x0) x0 = lx; if (lx > x1) x1 = lx;
			if (ly < y0) y0 = ly; if (ly > y1) y1 = ly;
		}
		/// <summary>The shadow-map texels an object covers through the light matrix <paramref name="m"/>: the corners of its
		/// box in its own space (its automatic bounds, or the box of its primitives when the stage set the bounds) through
		/// its transform. False when it misses the map.</summary>
		internal static bool ObjTexels(float[] m, SceneObject o, out SRect r) {
			double bx0, by0, bz0, bx1, by1, bz1;
			if (!o.BoundsSet) { bx0 = o.MinX; by0 = o.MinY; bz0 = o.MinZ; bx1 = o.MaxX; by1 = o.MaxY; bz1 = o.MaxZ; }
			else {
				bx0 = by0 = bz0 = double.MaxValue; bx1 = by1 = bz1 = double.MinValue;
				var d = o.D; int stride = PrimLayout.Stride(o.Kind), corners = PrimLayout.Corners(o.Kind), step = PrimLayout.CornerStep(o.Kind);
				for (int p = 0; p < o.N; p++)
					for (int i = 0, k = p * stride; i < corners; i++, k += step) {
						bx0 = Math.Min(bx0, d[k]); by0 = Math.Min(by0, d[k + 1]); bz0 = Math.Min(bz0, d[k + 2]);
						bx1 = Math.Max(bx1, d[k]); by1 = Math.Max(by1, d[k + 1]); bz1 = Math.Max(bz1, d[k + 2]);
					}
			}
			double x0 = double.MaxValue, y0 = double.MaxValue, x1 = double.MinValue, y1 = double.MinValue;
			for (int i = 0; i < 8; i++) {
				XfPt(o.Transform, (i & 1) == 0 ? bx0 : bx1, (i & 2) == 0 ? by0 : by1, (i & 4) == 0 ? bz0 : bz1, out double wx, out double wy, out double wz);
				LightBox(m, wx, wy, wz, ref x0, ref y0, ref x1, ref y1);
			}
			return TexelRect(x0, y0, x1, y1, out r);
		}
		/// <summary>A light-space xy box (the map spans -1..1) as map texels, two texels wider on each side; the whole map
		/// when the box is not finite. False when it misses the map.</summary>
		internal static bool TexelRect(double x0, double y0, double x1, double y1, out SRect r) {
			const int N = SHADOW_SIZE;
			r = new SRect { X1 = N, Y1 = N };
			if (!(double.IsFinite(x0) && double.IsFinite(y0) && double.IsFinite(x1) && double.IsFinite(y1))) return true;
			if (x1 < -1.0 || x0 > 1.0 || y1 < -1.0 || y0 > 1.0) return false;
			r = new SRect {
				X0 = (int)Math.Max(0, Math.Floor((x0 * 0.5 + 0.5) * N) - 2), Y0 = (int)Math.Max(0, Math.Floor((y0 * 0.5 + 0.5) * N) - 2),
				X1 = (int)Math.Min(N, Math.Ceiling((x1 * 0.5 + 0.5) * N) + 2), Y1 = (int)Math.Min(N, Math.Ceiling((y1 * 0.5 + 0.5) * N) + 2),
			};
			return r.X0 < r.X1 && r.Y0 < r.Y1;
		}

		// Bring the map up to date (see _shadowSig): redrawn whole, mended where the dynamic casters were and are, or kept.
		private void UpdateShadowMap(Lua3DScene s) {
			bool whole = !_shadowValid || _sigStatic != _shadowSig;
			if (!whole && _sigDyn == _shadowDynSig) return;
			if (!whole && !EnsureShadowStatic()) whole = true;
			if (whole) {
				GlPass.Target(Gl, _shadowFbo, SHADOW_SIZE, SHADOW_SIZE);
				GlPass.ClearDepth(Gl);
				DrawCasters(s, true, true);
				_shadowSig = _sigStatic; _shadowValid = true;
			} else {
				if (!_shadowStaticValid || _shadowStaticSig != _sigStatic) {
					GlPass.Target(Gl, _shadowStaticFbo, SHADOW_SIZE, SHADOW_SIZE);
					GlPass.ClearDepth(Gl);
					DrawCasters(s, true, false);
					_shadowStaticSig = _sigStatic; _shadowStaticValid = true;
				}
				// the texels to restore: every rectangle of the last and the current dynamic casters, overlapping ones joined
				_mend.Clear();
				foreach (var r in _drawnRects) JoinRect(_mend, r);
				foreach (var r in _dynRects) JoinRect(_mend, r);
				if (_mend.Count > 8) {
					var u = _mend[0];
					foreach (var r in _mend) u = Union(u, r);
					_mend.Clear(); _mend.Add(u);
				}
				Gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _shadowStaticFbo);
				Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _shadowFbo);
				foreach (var r in _mend) Gl.BlitFramebuffer(r.X0, r.Y0, r.X1, r.Y1, r.X0, r.Y0, r.X1, r.Y1, (uint)ClearBufferMask.DepthBufferBit, GLEnum.Nearest);
				GlPass.Target(Gl, _shadowFbo, SHADOW_SIZE, SHADOW_SIZE);
				DrawCasters(s, false, true);
			}
			_shadowDynSig = _sigDyn;
			(_drawnRects, _dynRects) = (_dynRects, _drawnRects);
		}
		private static SRect Union(SRect a, SRect b) => new SRect { X0 = Math.Min(a.X0, b.X0), Y0 = Math.Min(a.Y0, b.Y0), X1 = Math.Max(a.X1, b.X1), Y1 = Math.Max(a.Y1, b.Y1) };
		/// <summary>Add a rectangle to a list, joined with every rectangle of it that it overlaps or touches.</summary>
		internal static void JoinRect(List<SRect> into, SRect r) {
			for (int i = 0; i < into.Count; i++) {
				var m = into[i];
				if (r.X0 > m.X1 || m.X0 > r.X1 || r.Y0 > m.Y1 || m.Y0 > r.Y1) continue;
				r = Union(r, m); into.RemoveAt(i); i = -1;
			}
			into.Add(r);
		}

		// Draw casters into the bound shadow target: the static ones (the merge buckets and _shadowObjs) and/or the dynamic
		// ones (_shadowDyn), with the sprite silhouettes in their group. The depth test keeps the nearest, so the order
		// changes nothing.
		private void DrawCasters(Lua3DScene s, bool statics, bool dynamics) {
			GlPass.DepthTested(Gl); Gl.DepthMask(true);
			Gl.Disable(EnableCap.CullFace); Gl.Disable(EnableCap.Blend);
			Gl.UseProgram(_progDepth); Gl.BindVertexArray(_vao);
			Gl.UniformMatrix4(_uDLightVP, 1, false, (ReadOnlySpan<float>)_lightVP);
			Gl.UniformMatrix4(_uDModel, 1, false, (ReadOnlySpan<float>)_ident);
			bool textured = false;
			if (statics) {
				foreach (var mb in _shadowBuckets) DrawMerged(mb);
				textured |= DepthSolid(_shadowObjs);
			}
			if (dynamics) textured |= DepthSolid(_shadowDyn);
			bool sprites = _casterVerts.Count > 0 && (_casterStatic ? statics : dynamics);
			if (!textured && !sprites) return;
			// the alpha-tested casters: the objects' textured batches, whose cut-out texels the main pass discards, then
			// the sprite silhouettes
			Gl.UseProgram(_progDepthSpr); Gl.BindVertexArray(_vao);
			Gl.UniformMatrix4(_uDSLightVP, 1, false, (ReadOnlySpan<float>)_lightVP);
			Gl.Uniform1(_uDSTex, 0); Gl.ActiveTexture(TextureUnit.Texture0);
			if (textured) {
				if (statics) DepthTextured(s, _shadowObjs);
				if (dynamics) DepthTextured(s, _shadowDyn);
			}
			if (sprites) {
				if (_casterVbo == 0) { _casterVbo = Gl.GenBuffer(); _casterVboSet = false; }
				if (!_casterVboSet || _casterVboSig != _casterSig) {   // a buffer the GPU may still read is only rewritten when the silhouettes changed
					_casterVboSet = true; _casterVboSig = _casterSig;
					Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _casterVbo);
					Gl.BufferData<float>(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)CollectionsMarshal.AsSpan(_casterVerts), BufferUsageARB.StreamDraw);
				}
				Bind3D(_casterVbo);
				Gl.UniformMatrix4(_uDSModel, 1, false, (ReadOnlySpan<float>)_ident);
				foreach (var bt in _casterBatches) { Gl.BindTexture(TextureTarget.Texture2D, bt.Tex); Gl.DrawArrays(PrimitiveType.Triangles, bt.Start, (uint)bt.Count); }
			}
		}
		// the casters' depth in the plain depth program; true when textured batches are left for DepthTextured
		private bool DepthSolid(List<(SceneObject o, ObjCache c, bool solid)> objs) {
			bool textured = false;
			foreach (var (o, c, solid) in objs) {
				Gl.UniformMatrix4(_uDModel, 1, false, (ReadOnlySpan<float>)ModelMatrix(o.Transform));
				Bind3D(c.Vbo);
				if (solid && o.SplatSprite < 0) {
					int all = 0; foreach (var bt in c.Batches) all += bt.Count;
					Gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)all);
					continue;
				}
				if (o.SplatSprite >= 0) {   // splat terrain: the whole object in one call, like its main-pass draw
					int tot = 0; foreach (var bt in c.Batches) tot += bt.Count;
					Gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)tot);
					continue;
				}
				foreach (var bt in c.Batches) {
					if (bt.Mode != 0) { textured = true; continue; }   // drawn alpha-tested below
					Gl.DrawArrays(PrimitiveType.Triangles, bt.Start, (uint)bt.Count);
				}
			}
			return textured;
		}
		// the casters' textured batches in the alpha-tested depth program (bound)
		private void DepthTextured(Lua3DScene s, List<(SceneObject o, ObjCache c, bool solid)> objs) {
			foreach (var (o, c, solid) in objs) {
				if (o.SplatSprite >= 0 || solid) continue;
				bool bound = false;
				foreach (var bt in c.Batches) {
					if (bt.Mode == 0) continue;
					uint gt = bt.Pal ? PaletteTex() : TexFor(s, bt.TexId); if (gt == 0) continue;   // the main pass skips it too
					if (!bound) { bound = true; Gl.UniformMatrix4(_uDSModel, 1, false, (ReadOnlySpan<float>)ModelMatrix(o.Transform)); Bind3D(c.Vbo); }
					Gl.BindTexture(TextureTarget.Texture2D, gt);
					Gl.DrawArrays(PrimitiveType.Triangles, bt.Start, (uint)bt.Count);
				}
			}
		}
	}
}
