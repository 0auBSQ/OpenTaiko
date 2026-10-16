using System;
using System.Collections.Generic;
using FDK;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	// The world program and its uniform locations, the shader sources and the program cache.
	internal sealed partial class GpuRasterizer {
		private FrameLoc _uFrame;                                      // the bound world program's camera, projection and fog
		private int _uModel, _uNormalMat;
		private int _uMode, _uTex, _uCutout, _uTint, _uAlpha, _uFlat, _uRsh;
		private int _uLit, _uSunDir, _uSunCol, _uAmb, _uNumLights, _uLightPosR, _uLightCol;
		private int _uEmissive, _uWet, _uTimeW, _uRipple, _uRttFlip;   // stylized-world uniforms (emissive / wetness / clock / mirror ripple / RTT row order)
		private int _uClip, _uFres, _uFlat2, _uDeepOn;                 // reflection clip plane + water fresnel/depth tint
		private int _uFlatCol;                                         // per-object flat albedo override (recolour a routed GLB part)
		private int _uTex2, _uTex3, _uTex4, _uSplat, _uSplatSize, _uTexScale;   // terrain splat blending
		private int _uNormOn;                                                   // normal-mapped textures

		// cached world-program uniform locations per variant; BindWorldProgram copies the active set into _u*
		private struct WLoc { public FrameLoc Frame; public int Model, NormalMat, Mode, Tex, Cutout, Tint, Alpha, Flat, Rsh, Lit, SunDir, SunCol, Amb, NumLights, LightPosR, LightCol, LightVP, Shadow, ShadowTexel, Emissive, Wet, Time, Ripple, RttFlip, Clip, Fres, Flat2, DeepOn, Tex2, Tex3, Tex4, Splat, SplatSize, TexScale, FlatCol, NormOn, NormTex; }
		private WLoc _locDef, _locSh;

		// Last values sent to the bound world program's per-draw uniforms, so a draw skips the ones that did not
		// change. Reset whenever a world program is bound: programs are shared between scenes.
		private int _cMode, _cCutout, _cLit, _cNormOn;
		private float _cTr, _cTg, _cTb, _cAlpha, _cEr, _cEg, _cEb, _cFr, _cFg, _cFb, _cKr, _cKg, _cKb, _cKa;
		private bool _cIdent;
		private double[]? _cModel;   // the transform last sent, by reference (nothing changes a transform during a pass)

		// the shader sources (src/Lua/3D/Shaders, embedded in the assembly)
		private static string VS => ShaderSources.Get("world.vert");
		// Lit/textured world FS. Compiled TWICE: as-is (default — no shadow code) and with `#define SHADOWS`
		// prepended (adds the sun depth-map sampling). The `#version` is prepended at compile time so the define
		// can sit right after it. A separate VARIANT — not a `uShadowOn` uniform branch — is what keeps the shadow
		// taps from running (predicated) on every stage's fragments. In the default variant shf folds to 1.0.
		private static string FS => ShaderSources.Get("world.frag");

		// shadow depth pass — opaque casters (terrain/objects): write depth from the light's POV
		private static string VSD => ShaderSources.Get("depth.vert");
		private static string FSD => ShaderSources.Get("depth.frag");
		// shadow depth pass — alpha-tested casters (the character sprites, textured object batches): the cut-out texels cast nothing
		private static string VSDS => ShaderSources.Get("depth_sprite.vert");
		private static string FSDS => ShaderSources.Get("depth_sprite.frag");

		private static string VSP => ShaderSources.Get("particle.vert");
		// distance fog: alpha-blended particles fade to the fog colour, additive glows fade out (fog toward black)
		private static string FSP => ShaderSources.Get("particle.frag");
		private static string VSPOST => ShaderSources.Get("fullscreen.vert");
		private static string VSSKY => ShaderSources.Get("sky.vert");
		private static string FSPOST => ShaderSources.Get("post.frag");
		private static string FSOUTLINE => ShaderSources.Get("outline.frag");
		// 'Diorama' grade: SCREEN-SPACE tilt-shift (sharp middle band, blur ramping to the
		// top/bottom — only the edge bands pay the 4 taps, the centre is 1 tap) + saturation/contrast/vignette.
		// No depth sample, so it's cheap and needs no special FBO.
		private static string FSDIORAMA => ShaderSources.Get("diorama.frag");
		// grass: one instance per blade (foot pos, height, phase, shade), its six corners from gl_VertexID; the VS
		// billboards each blade around its foot with the camera right axis, sways the top with two crossed sines, and
		// shrinks blades toward zero between uFade.x..uFade.y so the field pops nothing at the draw distance.
		private static string VSGRASS => ShaderSources.Get("grass.vert");
		private static string FSGRASS => ShaderSources.Get("grass.frag");
		private static string VS2D => ShaderSources.Get("overlay2d.vert");
		private static string FS2D => ShaderSources.Get("overlay2d.frag");

		// ANGLE's GLSL ES translator rejects ANY byte >127 (even inside // comments) -> the shader fails to
		// compile -> EnsureGL throws -> the WHOLE GPU rasterizer silently falls back to the CPU one (no shadows,
		// CPU-speed lag). That bug has bitten twice (a stray arrow/em-dash in a comment), so strip non-ASCII
		// defensively here — harmless in comments, and code shouldn't contain any.
		private static string Ascii(string s) {
			bool clean = true; foreach (char c in s) if (c > 127) { clean = false; break; }
			if (clean) return s;
			var sb = new System.Text.StringBuilder(s.Length);
			foreach (char c in s) if (c <= 127) sb.Append(c);
			return sb.ToString();
		}

		// look up every world-program uniform for one variant (locations differ between the two compiled programs)
		private WLoc QueryWLoc(uint p) {
			int g(string n) => Gl.GetUniformLocation(p, n);
			return new WLoc {
				Frame = FrameLoc.Of(Gl, p), Model = g("uModel"),
				NormalMat = g("uNormalMat"), Mode = g("uMode"), Tex = g("uTex"), Cutout = g("uCutout"), Tint = g("uTint"), Alpha = g("uAlpha"), Flat = g("uFlat"),
				Rsh = g("uRsh"),
				Lit = g("uLit"), SunDir = g("uSunDir"), SunCol = g("uSunCol"), Amb = g("uAmb"),
				NumLights = g("uNumLights"), LightPosR = g("uLightPosR[0]"), LightCol = g("uLightCol[0]"),
				LightVP = g("uLightVP"), Shadow = g("uShadow"), ShadowTexel = g("uShadowTexel"),
				Emissive = g("uEmissive"), Wet = g("uWet"), Time = g("uTime"), Ripple = g("uRipple"),
				RttFlip = g("uRttFlip"),
				Clip = g("uClip"), Fres = g("uFres"), Flat2 = g("uFlat2"), DeepOn = g("uDeepOn"),
				Tex2 = g("uTex2"), Tex3 = g("uTex3"), Tex4 = g("uTex4"), Splat = g("uSplat"),
				SplatSize = g("uSplatSize"), TexScale = g("uTexScale"), FlatCol = g("uFlatCol"),
				NormOn = g("uNormOn"), NormTex = g("uNormTex"),
			};
		}

		// bind the chosen world variant AND point the active _u* fields at that program's locations, so every
		// Gl.Uniform* in the draw code targets the bound program (locations are NOT guaranteed equal across variants)
		private void BindWorldProgram(bool shadow) {
			_worldShadow = shadow && _shadowOk;
			Gl.UseProgram(_worldShadow ? _progShadow : _prog);
			var L = _worldShadow ? _locSh : _locDef;
			_uFrame = L.Frame; _uModel = L.Model; _uNormalMat = L.NormalMat;
			_uMode = L.Mode; _uTex = L.Tex; _uCutout = L.Cutout; _uTint = L.Tint; _uAlpha = L.Alpha; _uFlat = L.Flat;
			_uRsh = L.Rsh;
			_uLit = L.Lit; _uSunDir = L.SunDir; _uSunCol = L.SunCol; _uAmb = L.Amb;
			_uNumLights = L.NumLights; _uLightPosR = L.LightPosR; _uLightCol = L.LightCol;
			_uLightVP = L.LightVP; _uShadow = L.Shadow; _uShadowTexel = L.ShadowTexel;
			_uEmissive = L.Emissive; _uWet = L.Wet; _uTimeW = L.Time; _uRipple = L.Ripple; _uRttFlip = L.RttFlip;
			_uClip = L.Clip; _uFres = L.Fres; _uFlat2 = L.Flat2; _uDeepOn = L.DeepOn;
			_uTex2 = L.Tex2; _uTex3 = L.Tex3; _uTex4 = L.Tex4; _uSplat = L.Splat;
			_uSplatSize = L.SplatSize; _uTexScale = L.TexScale; _uFlatCol = L.FlatCol;
			_uNormOn = L.NormOn;
			Gl.Uniform1(L.NormTex, 6);   // normal maps sit on texture unit 6
			ResetUniformCache();
			UNormOn(0);
		}

		// ── per-draw uniforms of the world program, sent only when they change (see _cMode) ──
		private void ResetUniformCache() {
			_cMode = _cCutout = _cLit = _cNormOn = int.MinValue;
			_cTr = _cTg = _cTb = _cAlpha = _cEr = _cEg = _cEb = _cFr = _cFg = _cFb = _cKr = _cKg = _cKb = _cKa = float.NaN;
			_cIdent = false; _cModel = null;
		}
		private void UMode(int v) { if (v != _cMode) { _cMode = v; Gl.Uniform1(_uMode, v); } }
		private void UCutout(int v) { if (v != _cCutout) { _cCutout = v; Gl.Uniform1(_uCutout, v); } }
		private void ULit(int v) { if (v != _cLit) { _cLit = v; Gl.Uniform1(_uLit, v); } }
		private void UNormOn(int v) { if (v != _cNormOn) { _cNormOn = v; Gl.Uniform1(_uNormOn, v); } }
		private void UAlpha(float v) { if (v != _cAlpha) { _cAlpha = v; Gl.Uniform1(_uAlpha, v); } }
		private void UTint(float r, float g, float b) {
			if (r != _cTr || g != _cTg || b != _cTb) { _cTr = r; _cTg = g; _cTb = b; Gl.Uniform3(_uTint, r, g, b); }
		}
		private void UEmissive(float r, float g, float b) {
			if (r != _cEr || g != _cEg || b != _cEb) { _cEr = r; _cEg = g; _cEb = b; Gl.Uniform3(_uEmissive, r, g, b); }
		}
		private void UFlat(float r, float g, float b) {
			if (r != _cFr || g != _cFg || b != _cFb) { _cFr = r; _cFg = g; _cFb = b; Gl.Uniform3(_uFlat, r, g, b); }
		}
		private void UFlatCol(float r, float g, float b, float a) {
			if (r != _cKr || g != _cKg || b != _cKb || a != _cKa) { _cKr = r; _cKg = g; _cKb = b; _cKa = a; Gl.Uniform4(_uFlatCol, r, g, b, a); }
		}
		// the model matrix and its normal matrix; null = identity
		private void UModel(double[]? t) {
			if (t == null) {
				if (_cIdent) return;
				_cIdent = true; _cModel = null;
				Gl.UniformMatrix4(_uModel, 1, false, (ReadOnlySpan<float>)_ident);
				Gl.UniformMatrix3(_uNormalMat, 1, false, (ReadOnlySpan<float>)_ident3);
				return;
			}
			if (ReferenceEquals(t, _cModel)) return;
			_cIdent = false; _cModel = t;
			Gl.UniformMatrix4(_uModel, 1, false, (ReadOnlySpan<float>)ModelMatrix(t));
			Gl.UniformMatrix3(_uNormalMat, 1, false, (ReadOnlySpan<float>)NormalMatrix(t, _nmtx));
		}

		// The GLSL programs are byte-identical for every GpuRasterizer and stay valid for the whole GL
		// context, yet each offscreen scene (every ModelIcon preview) used to be a fresh renderer that
		// RECOMPILED all ~10 programs — GLSL compile+link is 100s of ms each, so a handful of previews
		// stalled for seconds (and a shop of previews would be unusable). Compile each source pair ONCE
		// and share the handle. The glIsProgram guard recompiles automatically if the GL context is
		// recreated (the old handle is no longer a program), so context loss stays correct.
		private static readonly Dictionary<string, uint> s_progCache = new();
		private uint CachedProg(string vs, string fs) {   // instance method: Gl is a per-instance handle to the shared context
			string key = vs + " " + fs;
			if (s_progCache.TryGetValue(key, out var p) && p != 0 && Gl.IsProgram(p)) return p;
			p = ShaderHelper.CreateShaderProgramFromSource(vs, fs);
			s_progCache[key] = p;
			return p;
		}
	}
}
