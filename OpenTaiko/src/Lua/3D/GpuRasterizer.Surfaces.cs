using System;
using System.Collections.Generic;
using System.Diagnostics;
using FDK;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	// Screen-texture surfaces (mirrors, portals, water) and their custom shaders.
	internal sealed partial class GpuRasterizer {
		// ── custom SURFACE shader programs (ObjSetSurfaceShader) ──────────────────────────────────
		// One compiled program per DISTINCT fragment source (surfaces sharing a source share it). The
		// standard world VS feeds the same varyings the built-in FS gets, so a custom water/mirror
		// shader is a drop-in replacement for the built-in ripple/fresnel path.
		// a compiled surface program; Dead once freed, so an object that kept it looks it up again
		private sealed class SurfProg { public uint Prog; public FrameLoc Frame; public int Model, NormalMat, Tex, Time, User, Rsh, RttFlip, Alpha, Tint, Flat, SunDir, SunCol, Amb, SkyHor, SkyZen, Tex2, LightVP, Shadow, ShadowOn; public long Used; public bool Dead; }
		private readonly Dictionary<string, SurfProg> _surfProgs = new();
		private const int SURF_CAP = 16;   // distinct sources kept; past this the least recently used is freed, never one drawn this frame
		private const string SURF_PRELUDE = "#version 300 es\nprecision highp float;\n" +
			"in vec2 vUV; in float vShade; in float vCamZ; in vec3 vWorld; in vec3 vNormal;\n" +
			"uniform sampler2D uTex; uniform int uRsh; uniform int uRttFlip;\n" +
			"uniform vec3 uCam, uTint, uFlat; uniform float uTime, uAlpha; uniform vec4 uUser[4];\n" +
			"out vec4 frag;\n";
		private SurfProg SurfFor(string src) {
			if (_surfProgs.TryGetValue(src, out var hit)) { hit.Used = _frame; return hit; }
			if (_surfProgs.Count >= SURF_CAP) {   // full: free the least recently used source's program, unless all were drawn this frame
				string? oldest = null; long least = _frame;
				foreach (var kv in _surfProgs) if (kv.Value.Used < least) { least = kv.Value.Used; oldest = kv.Key; }
				if (oldest != null && _surfProgs.Remove(oldest, out var old)) { old.Dead = true; if (old.Prog != 0) Gl.DeleteProgram(old.Prog); }
			}
			var sp = new SurfProg { Used = _frame };
			string full = src.Contains("#version") ? src : SURF_PRELUDE + src;
			try {
				sp.Prog = ShaderHelper.CreateShaderProgramFromSource(Ascii(VS), Ascii(full));
				int g(string nm) => Gl.GetUniformLocation(sp.Prog, nm);
				sp.Frame = FrameLoc.Of(Gl, sp.Prog);
				sp.Model = g("uModel"); sp.NormalMat = g("uNormalMat");
				sp.Tex = g("uTex"); sp.Time = g("uTime"); sp.User = g("uUser[0]");
				sp.Rsh = g("uRsh"); sp.RttFlip = g("uRttFlip");
				sp.Alpha = g("uAlpha"); sp.Tint = g("uTint"); sp.Flat = g("uFlat");
				sp.SunDir = g("uSunDir"); sp.SunCol = g("uSunCol"); sp.Amb = g("uAmb"); sp.SkyHor = g("uSkyHor"); sp.SkyZen = g("uSkyZen");
				sp.Tex2 = g("uTex2"); sp.LightVP = g("uLightVP"); sp.Shadow = g("uShadow"); sp.ShadowOn = g("uShadowOn");
			} catch (Exception e) {
				LastShaderError = "surface: " + e.Message;
				Trace.TraceError("ObjSetSurfaceShader: surface shader failed to compile, using the built-in path.\n" + e);
				sp.Prog = 0;
			}
			_surfProgs[src] = sp;   // failures cache too (no per-frame recompile storm)
			return sp;
		}
		// draw one surface through its custom program, then hand the pass back to the world program
		private void DrawSurfaceCustom(Lua3DScene s, SceneObject o, ObjCache c, SurfProg sp, int texId, uint gt) {
			Gl.UseProgram(sp.Prog);
			SetCamera(sp.Frame, s);
			Gl.UniformMatrix4(sp.Model, 1, false, (ReadOnlySpan<float>)ModelMatrix(o.Transform));
			Gl.UniformMatrix3(sp.NormalMat, 1, false, (ReadOnlySpan<float>)(o.Transform == null ? _ident3 : NormalMatrix(o.Transform, _nmtx)));
			Gl.Uniform1(sp.Tex, 0);
			Gl.Uniform1(sp.Time, (float)s._shaderTime);
			if (sp.User >= 0) Gl.Uniform4(sp.User, 4u, (ReadOnlySpan<float>)s._surfUser);
			Gl.Uniform1(sp.Rsh, s._rttShift);
			bool gpuT = _rttColor.ContainsKey(texId);
			bool trueR = s._reflectTrue.Contains(texId);
			Gl.Uniform1(sp.RttFlip, gpuT != trueR ? 1 : 0);
			Gl.Uniform1(sp.Alpha, o.A / 255.0f);
			Gl.Uniform3(sp.Tint, (float)o.TintR, (float)o.TintG, (float)o.TintB);
			Gl.Uniform3(sp.Flat, (float)(o.R / 255.0), (float)(o.G / 255.0), (float)(o.B / 255.0));
			SetFog(sp.Frame, s);
			if (sp.SunDir >= 0) Gl.Uniform3(sp.SunDir, (float)s._sunX, (float)s._sunY, (float)s._sunZ);
			if (sp.SunCol >= 0) Gl.Uniform3(sp.SunCol, (float)s._sunR, (float)s._sunG, (float)s._sunB);
			if (sp.Amb >= 0) Gl.Uniform3(sp.Amb, (float)s._ambR, (float)s._ambG, (float)s._ambB);
			if (sp.SkyHor >= 0) Gl.Uniform3(sp.SkyHor, s._skyCol[0], s._skyCol[1], s._skyCol[2]);
			if (sp.SkyZen >= 0) Gl.Uniform3(sp.SkyZen, s._skyCol[3], s._skyCol[4], s._skyCol[5]);
			if (sp.Shadow >= 0) {   // unit 1 in a shadowed main pass
				Gl.Uniform1(sp.Shadow, 1);
				if (sp.LightVP >= 0) Gl.UniformMatrix4(sp.LightVP, 1, false, (ReadOnlySpan<float>)_lightVP);
			}
			if (sp.ShadowOn >= 0) Gl.Uniform1(sp.ShadowOn, _worldShadow ? 1f : 0f);
			if (sp.Tex2 >= 0) {     // first texture when none
				uint t2 = o.SurfaceTex2 >= 0 ? TexFor(s, o.SurfaceTex2) : 0;
				Gl.Uniform1(sp.Tex2, UNIT_SURF_TEX2);
				Gl.ActiveTexture(TextureUnit.Texture0 + UNIT_SURF_TEX2); Gl.BindTexture(TextureTarget.Texture2D, t2 != 0 ? t2 : gt);
			}
			Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, gt);
			Bind3D(c.Vbo);
			int total = 0; foreach (var bt in c.Batches) total += bt.Count;
			Gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)total);
			Gl.UseProgram(WorldProg);
		}

		// A screen-texture mirror/portal surface (uMode 3): the FS samples the live RTT texture by screen
		// pixel and blends it over the base colour by the per-channel mirror factor (the object's tint). The
		// texture is the per-frame RenderView capture, re-uploaded by TexFor when its revision changes.
		private void DrawScreenTex(Lua3DScene s, SceneObject o) {
			var c = EnsureObj(o);
			if (c.Batches.Count == 0) return;
			int texId = (int)o.D[PrimLayout.TexQuadTex];   // Kind-0 quad texId = the mirror RTT
			uint gt = TexFor(s, texId); if (gt == 0) return;
			if (o.SurfaceSrc != null) {
				// the program found for this object's source last time, unless the source changed or it was freed
				var sp = c.Surf;
				if (sp == null || sp.Dead || !ReferenceEquals(c.SurfSrc, o.SurfaceSrc)) { sp = SurfFor(o.SurfaceSrc); c.Surf = sp; c.SurfSrc = o.SurfaceSrc; }
				else sp.Used = _frame;
				if (sp.Prog != 0) { DrawSurfaceCustom(s, o, c, sp, texId, gt); return; }
			}
			Bind3D(c.Vbo);
			UModel(o.Transform);
			UMode(3); UCutout(0); ULit(0);
			UTint((float)o.TintR, (float)o.TintG, (float)o.TintB);                        // mirror factor (fm)
			UFlat((float)(o.R / 255.0), (float)(o.G / 255.0), (float)(o.B / 255.0));       // base colour under the mirror
			UAlpha(o.A / 255.0f);
			Gl.Uniform3(_uRipple, (float)o.RippleAmp, (float)o.RippleFreq, (float)o.RippleSpeed);   // water wobble (0 = flat mirror)
			// flip convention: legacy captures need the row-order swap only on the GPU-FBO path; TRUE
			// reflections (RenderViewReflect) are same-pixel-exact on the GPU path and need the swap
			// only for the top-down CPU upload — i.e. the two flags XOR.
			bool gpuTarget = _rttColor.ContainsKey(texId);
			bool trueReflect = s._reflectTrue.Contains(texId);
			Gl.Uniform1(_uRttFlip, gpuTarget != trueReflect ? 1 : 0);
			Gl.Uniform2(_uFres, (float)(o.FresnelF0 < 0 ? 0 : o.FresnelF0), o.FresnelF0 < 0 ? 0f : 1f);
			if (o.DeepR >= 0) {
				Gl.Uniform1(_uDeepOn, 1);
				Gl.Uniform3(_uFlat2, (float)(o.DeepR / 255.0), (float)(o.DeepG / 255.0), (float)(o.DeepB / 255.0));
			} else Gl.Uniform1(_uDeepOn, 0);
			Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, gt);
			int total = 0; foreach (var bt in c.Batches) total += bt.Count;
			Gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)total);   // single mirror texture → one contiguous draw
		}
	}
}
