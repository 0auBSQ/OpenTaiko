using System;
using System.Collections.Generic;
using System.Diagnostics;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	// terrain program, shared opt-in state
	internal sealed partial class GpuRasterizer {
		private static string FSTERRAIN => ShaderSources.Get("terrain.frag");
		// above the world program's units 0..6
		private const int UNIT_SHADOW_CMP = 7, UNIT_TERR_NORMAL = 8, UNIT_TERR_TINT = 9, UNIT_SURF_TEX2 = 12;

		private sealed class TerrProg {
			public uint Prog; public FrameLoc Frame; public int Stamp = -1;
			public int Model, NormalMat, Tex, Tex2, Tex3, Tex4, Splat, NormalMap, TintMap, SplatSize, MapSize, TexScale, Avg, Terr, Tint, Alpha;
			public int SunDir, SunCol, Amb, NumLights, LightPosR, LightCol, Wet, Time, Clip, LightVP, ShadowCmp, ShadowTexel, ShadowF, Cel;
		}
		private TerrProg? _terr, _terrSh;
		private bool _terrFailed;
		private int _passSerial;   // bumped per PreparePass
		private uint _shadowCmpSampler, _neutralNormal, _neutralTint;
		private bool _shadowCmpBound;
		private readonly Dictionary<int, (int rev, float r, float g, float b)> _texAvg = new();
		private readonly float[] _avg4 = new float[12];

		private TerrProg? TerrFor(bool shadow) {
			if (_terrFailed) return null;
			var have = shadow ? _terrSh : _terr;
			if (have != null) return have;
			try {
				var tp = new TerrProg { Prog = CachedProg(Ascii(VS), Ascii("#version 300 es\n" + (shadow ? "#define SHADOWS\n" : "") + FSTERRAIN)) };
				int g(string n) => Gl.GetUniformLocation(tp.Prog, n);
				tp.Frame = FrameLoc.Of(Gl, tp.Prog);
				tp.Model = g("uModel"); tp.NormalMat = g("uNormalMat");
				tp.Tex = g("uTex"); tp.Tex2 = g("uTex2"); tp.Tex3 = g("uTex3"); tp.Tex4 = g("uTex4"); tp.Splat = g("uSplat");
				tp.NormalMap = g("uNormalMap"); tp.TintMap = g("uTintMap");
				tp.SplatSize = g("uSplatSize"); tp.MapSize = g("uMapSize"); tp.TexScale = g("uTexScale");
				tp.Avg = g("uAvg[0]"); tp.Terr = g("uTerr[0]"); tp.Tint = g("uTint"); tp.Alpha = g("uAlpha");
				tp.SunDir = g("uSunDir"); tp.SunCol = g("uSunCol"); tp.Amb = g("uAmb");
				tp.NumLights = g("uNumLights"); tp.LightPosR = g("uLightPosR[0]"); tp.LightCol = g("uLightCol[0]");
				tp.Wet = g("uWet"); tp.Time = g("uTime"); tp.Clip = g("uClip");
				tp.LightVP = g("uLightVP"); tp.ShadowCmp = g("uShadowCmp"); tp.ShadowTexel = g("uShadowTexel"); tp.ShadowF = g("uShadowF"); tp.Cel = g("uCel");
				if (shadow) _terrSh = tp; else _terr = tp;
				return tp;
			} catch (Exception e) {
				_terrFailed = true; LastShaderError = "terrain: " + e.Message;
				Trace.TraceError("The terrain program failed to compile, using the splat shading: " + e);
				return null;
			}
		}

		private void SetSunAndLights(Lua3DScene s, int sunDir, int sunCol, int amb, int numLights, int lightPosR, int lightCol) {
			Gl.Uniform3(sunDir, (float)s._sunX, (float)s._sunY, (float)s._sunZ);
			Gl.Uniform3(sunCol, (float)s._sunR, (float)s._sunG, (float)s._sunB);
			Gl.Uniform3(amb, (float)s._ambR, (float)s._ambG, (float)s._ambB);
			int nl = s._useLights ? _passLights : 0;
			Gl.Uniform1(numLights, nl);
			if (nl > 0) { Gl.Uniform4(lightPosR, (uint)nl, new ReadOnlySpan<float>(_lpos, 0, nl * 4)); Gl.Uniform3(lightCol, (uint)nl, new ReadOnlySpan<float>(_lcol, 0, nl * 3)); }
		}

		// sampler2DShadow unit, until UnbindShadowCmp
		private void BindShadowCmp() {
			if (_shadowCmpBound) return;
			if (_shadowCmpSampler == 0) {
				_shadowCmpSampler = Gl.GenSampler();
				Gl.SamplerParameter(_shadowCmpSampler, SamplerParameterI.MinFilter, (int)GLEnum.Linear);
				Gl.SamplerParameter(_shadowCmpSampler, SamplerParameterI.MagFilter, (int)GLEnum.Linear);
				Gl.SamplerParameter(_shadowCmpSampler, SamplerParameterI.WrapS, (int)GLEnum.ClampToEdge);
				Gl.SamplerParameter(_shadowCmpSampler, SamplerParameterI.WrapT, (int)GLEnum.ClampToEdge);
				Gl.SamplerParameter(_shadowCmpSampler, (SamplerParameterI)GLEnum.TextureCompareMode, (int)GLEnum.CompareRefToTexture);
				Gl.SamplerParameter(_shadowCmpSampler, (SamplerParameterI)GLEnum.TextureCompareFunc, (int)GLEnum.Lequal);
			}
			Gl.ActiveTexture(TextureUnit.Texture0 + UNIT_SHADOW_CMP); Gl.BindTexture(TextureTarget.Texture2D, _shadowTex);
			Gl.BindSampler(UNIT_SHADOW_CMP, _shadowCmpSampler);
			Gl.ActiveTexture(TextureUnit.Texture0);
			_shadowCmpBound = true;
		}
		private void UnbindShadowCmp() {
			if (!_shadowCmpBound) return;
			_shadowCmpBound = false;
			Gl.BindSampler(UNIT_SHADOW_CMP, 0);
		}

		private unsafe uint OneTexel(byte r, byte g, byte b) {
			uint t = Gl.GenTexture();
			Gl.BindTexture(TextureTarget.Texture2D, t);
			byte* px = stackalloc byte[4]; px[0] = r; px[1] = g; px[2] = b; px[3] = 255;
			Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8, 1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, px);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
			return t;
		}

		// opaque texels only, cached per revision
		private (float r, float g, float b) TexAverage(Lua3DScene s, int id) {
			if (!s._tex.TryGetValue(id, out var te)) return (0.5f, 0.5f, 0.5f);
			if (te.Pix == null) return te.GpuOnly ? (te.AvR, te.AvG, te.AvB) : (0.5f, 0.5f, 0.5f);
			if (_texAvg.TryGetValue(id, out var a) && a.rev == te.Rev) return (a.r, a.g, a.b);
			long r = 0, g = 0, b = 0, n = 0;
			var px = te.Pix; int len = Math.Min(px.Length, te.W * te.H);
			for (int i = 0; i < len; i++) { int p = px[i]; if (p < 0) continue; r += (p >> 16) & 255; g += (p >> 8) & 255; b += p & 255; n++; }
			if (n == 0) n = 1;
			a = (te.Rev, r / (255f * n), g / (255f * n), b / (255f * n));
			_texAvg[id] = a;
			return (a.r, a.g, a.b);
		}

		// false: no terrain program; rebinds the world program
		private bool DrawTerrainObj(Lua3DScene s, SceneObject o, ObjCache c, uint t1, uint t2, uint t3, uint t4, uint sp) {
			var tp = TerrFor(_worldShadow);
			if (tp == null) return false;
			// resolve first: uploads bind on the active unit
			uint nm = o.TerrNormalTex >= 0 ? TexFor(s, o.TerrNormalTex) : 0, tm = o.TerrTintTex >= 0 ? TexFor(s, o.TerrTintTex) : 0;
			if (nm == 0) nm = _neutralNormal != 0 ? _neutralNormal : (_neutralNormal = OneTexel(128, 255, 128));
			if (tm == 0) tm = _neutralTint != 0 ? _neutralTint : (_neutralTint = OneTexel(128, 128, 128));
			Gl.UseProgram(tp.Prog);
			if (tp.Stamp != _passSerial) {
				tp.Stamp = _passSerial;
				SetCamera(tp.Frame, s); SetFog(tp.Frame, s);
				SetSunAndLights(s, tp.SunDir, tp.SunCol, tp.Amb, tp.NumLights, tp.LightPosR, tp.LightCol);
				SendLightGrid(tp.Prog, s._useLights);
				Gl.Uniform1(tp.Wet, (float)s._wetness); Gl.Uniform1(tp.Time, (float)s._shaderTime);
				if (s._clipOn) Gl.Uniform4(tp.Clip, (float)s._clipNx, (float)s._clipNy, (float)s._clipNz, (float)s._clipD);
				else Gl.Uniform4(tp.Clip, 0f, 0f, 0f, -1e30f);
				Gl.Uniform4(tp.Terr, 6u, (ReadOnlySpan<float>)s._terrP);
				Gl.Uniform1(tp.Cel, s._cel);
				Gl.Uniform1(tp.Tex, 0); Gl.Uniform1(tp.Tex2, 2); Gl.Uniform1(tp.Tex3, 3); Gl.Uniform1(tp.Tex4, 4); Gl.Uniform1(tp.Splat, 5);
				Gl.Uniform1(tp.NormalMap, UNIT_TERR_NORMAL); Gl.Uniform1(tp.TintMap, UNIT_TERR_TINT);
				if (_worldShadow) {
					BindShadowCmp();
					Gl.UniformMatrix4(tp.LightVP, 1, false, (ReadOnlySpan<float>)_lightVP);
					Gl.Uniform1(tp.ShadowCmp, UNIT_SHADOW_CMP); Gl.Uniform1(tp.ShadowTexel, 1.0f / SHADOW_SIZE);
					Gl.Uniform2(tp.ShadowF, s._shadowSoft, s._shadowFade);
				}
			}
			Gl.UniformMatrix4(tp.Model, 1, false, (ReadOnlySpan<float>)ModelMatrix(o.Transform));
			Gl.UniformMatrix3(tp.NormalMat, 1, false, (ReadOnlySpan<float>)(o.Transform == null ? _ident3 : NormalMatrix(o.Transform, _nmtx)));
			Gl.Uniform3(tp.Tint, (float)o.TintR, (float)o.TintG, (float)o.TintB);
			Gl.Uniform1(tp.Alpha, o.A / 255.0f);
			Gl.Uniform2(tp.SplatSize, o.SplatW, o.SplatH);
			Gl.Uniform1(tp.MapSize, o.TerrMapSize);
			Gl.Uniform1(tp.TexScale, o.LayerUvScale);
			for (int i = 0; i < 4; i++) {
				int lid = i == 0 || (i == 1 ? t2 : i == 2 ? t3 : t4) == 0 ? o.LayerTex1 : i == 1 ? o.LayerTex2 : i == 2 ? o.LayerTex3 : o.LayerTex4;
				var (r, g, b) = TexAverage(s, lid); _avg4[i * 3] = r; _avg4[i * 3 + 1] = g; _avg4[i * 3 + 2] = b;
			}
			Gl.Uniform3(tp.Avg, 4u, (ReadOnlySpan<float>)_avg4);
			Gl.ActiveTexture(TextureUnit.Texture2); Gl.BindTexture(TextureTarget.Texture2D, t2 != 0 ? t2 : t1);
			Gl.ActiveTexture(TextureUnit.Texture3); Gl.BindTexture(TextureTarget.Texture2D, t3 != 0 ? t3 : t1);
			Gl.ActiveTexture(TextureUnit.Texture4); Gl.BindTexture(TextureTarget.Texture2D, t4 != 0 ? t4 : t1);
			Gl.ActiveTexture(TextureUnit.Texture5); Gl.BindTexture(TextureTarget.Texture2D, sp);
			Gl.ActiveTexture(TextureUnit.Texture0 + UNIT_TERR_NORMAL); Gl.BindTexture(TextureTarget.Texture2D, nm);
			Gl.ActiveTexture(TextureUnit.Texture0 + UNIT_TERR_TINT); Gl.BindTexture(TextureTarget.Texture2D, tm);
			Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, t1);
			Bind3D(c.Vbo);
			int total = 0; foreach (var bt in c.Batches) total += bt.Count;
			Gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)total);
			Gl.UseProgram(WorldProg);
			return true;
		}

		// programs stay in the shared cache
		private void ClearOptIn() {
			_terr = _terrSh = null; _terrFailed = false;
			_extMade = false; _extOk = true;
			if (_shadowCmpSampler != 0) { Gl.DeleteSampler(_shadowCmpSampler); _shadowCmpSampler = 0; }
			if (_neutralNormal != 0) { Gl.DeleteTexture(_neutralNormal); _neutralNormal = 0; }
			if (_neutralTint != 0) { Gl.DeleteTexture(_neutralTint); _neutralTint = 0; }
			_texAvg.Clear();
			ClearGrassTile();
			ClearLightGrid();
		}
	}
}
