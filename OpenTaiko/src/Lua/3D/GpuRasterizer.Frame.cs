using System;
using System.Collections.Generic;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	// The uniforms a pass sets once per program: camera, projection, fog and lights.
	internal sealed partial class GpuRasterizer {
		private const int MAXL = 64;         // max point lights sent to the shader
		private readonly int[] _lightPick = new int[MAXL];
		private double[] _lightKey = Array.Empty<double>(); private int[] _lightIdx = Array.Empty<int>();

		private readonly float[] _lpos = new float[MAXL * 4], _lcol = new float[MAXL * 3];

		// the projection's depth terms for this pass: clip z = A * camera z + B maps [near, _far] to [-1, 1]
		private void DepthAB(Lua3DScene s, out float A, out float B) {
			double n = s._near, f = _far;
			A = (float)((f + n) / (f - n)); B = (float)(-2.0 * f * n / (f - n));
		}

		// the uniforms a pass sets once on the bound world program: camera, projection, fog, lighting and the defaults
		// of the per-object ones
		private void SetFrameUniforms(Lua3DScene s) {
			SetCamera(_uFrame, s); Gl.Uniform1(_uTex, 0);
			Gl.Uniform1(_uRsh, s._rttShift);   // mirror screen-tex sampling shift
			Gl.Uniform1(_uWet, (float)s._wetness);
			Gl.Uniform1(_uTimeW, (float)s._shaderTime);
			UEmissive(0f, 0f, 0f);       // per-object; DrawObj overrides
			UFlatCol(0f, 0f, 0f, 0f);    // per-object flat-albedo override off by default; DrawObj sets it
			Gl.Uniform3(_uRipple, 0f, 0f, 0f);     // per-surface; DrawScreenTex overrides
			// reflection clip plane: active only inside a RenderViewReflect pass
			if (s._clipOn) Gl.Uniform4(_uClip, (float)s._clipNx, (float)s._clipNy, (float)s._clipNz, (float)s._clipD);
			else Gl.Uniform4(_uClip, 0f, 0f, 0f, -1e30f);
			Gl.Uniform2(_uFres, 0f, 0f);           // per-surface; DrawScreenTex overrides
			Gl.Uniform1(_uDeepOn, 0);
			SetFog(_uFrame, s);
			// lighting: sun + ambient + point lights
			Gl.Uniform3(_uSunDir, (float)s._sunX, (float)s._sunY, (float)s._sunZ);
			Gl.Uniform3(_uSunCol, (float)s._sunR, (float)s._sunG, (float)s._sunB);
			Gl.Uniform3(_uAmb, (float)s._ambR, (float)s._ambG, (float)s._ambB);
			var lights = s._lights;
			int nl = PickLights(lights, s._camX, s._camY, s._camZ, MAXL, _lightPick, ref _lightKey, ref _lightIdx);
			for (int i = 0; i < nl; i++) {
				var L = lights[_lightPick[i]];
				_lpos[i * 4] = (float)L.X; _lpos[i * 4 + 1] = (float)L.Y; _lpos[i * 4 + 2] = (float)L.Z; _lpos[i * 4 + 3] = (float)L.Range;
				_lcol[i * 3] = (float)L.R; _lcol[i * 3 + 1] = (float)L.G; _lcol[i * 3 + 2] = (float)L.B;
			}
			Gl.Uniform1(_uNumLights, nl);
			if (nl > 0) { Gl.Uniform4(_uLightPosR, (uint)nl, new ReadOnlySpan<float>(_lpos, 0, nl * 4)); Gl.Uniform3(_uLightCol, (uint)nl, new ReadOnlySpan<float>(_lcol, 0, nl * 3)); }
			// (the shadow map + light matrix are bound by the main pass only when the shadow variant is active;
			// the default variant has no shadow uniforms at all, so there's nothing to reset here.)
		}

		/// <summary>The uniforms every world-space program declares, located in one program: the camera and projection
		/// (uCam, uR, uU, uF, uSx, uSy, uA, uB) and the distance fog (uFog, uFogCol, uFogStart, uFogInv; -1 where a
		/// program has none). <see cref="SetCamera"/> and <see cref="SetFog"/> send a pass's values to them.</summary>
		private struct FrameLoc {
			public int Cam, R, U, F, Sx, Sy, A, B, Fog, FogCol, FogStart, FogInv;
			public static FrameLoc Of(GL gl, uint p) {
				int g(string n) => gl.GetUniformLocation(p, n);
				return new FrameLoc {
					Cam = g("uCam"), R = g("uR"), U = g("uU"), F = g("uF"), Sx = g("uSx"), Sy = g("uSy"), A = g("uA"), B = g("uB"),
					Fog = g("uFog"), FogCol = g("uFogCol"), FogStart = g("uFogStart"), FogInv = g("uFogInv"),
				};
			}
		}
		// this pass's camera and projection into the bound program
		private void SetCamera(in FrameLoc l, Lua3DScene s) {
			Gl.Uniform3(l.Cam, (float)s._camX, (float)s._camY, (float)s._camZ);
			Gl.Uniform3(l.R, (float)s._Rx, (float)s._Ry, (float)s._Rz);
			Gl.Uniform3(l.U, (float)s._Ux, (float)s._Uy, (float)s._Uz);
			Gl.Uniform3(l.F, (float)s._Fx, (float)s._Fy, (float)s._Fz);
			Gl.Uniform1(l.Sx, (float)(2.0 * s._scale / s._w));
			Gl.Uniform1(l.Sy, (float)(-2.0 * s._scale / s._h));
			DepthAB(s, out float A, out float B);
			Gl.Uniform1(l.A, A); Gl.Uniform1(l.B, B);
		}
		// the scene's distance fog into the bound program. The fog colour is stored in 0-255 (the CPU blends in pixel
		// space); the shaders work in 0-1.
		private void SetFog(in FrameLoc l, Lua3DScene s) {
			if (s._fog) { Gl.Uniform1(l.Fog, 1); Gl.Uniform3(l.FogCol, (float)(s._fogR / 255.0), (float)(s._fogG / 255.0), (float)(s._fogB / 255.0)); Gl.Uniform1(l.FogStart, (float)s._fogStart); Gl.Uniform1(l.FogInv, (float)s._fogInv); }
			else Gl.Uniform1(l.Fog, 0);
		}

		/// <summary>The point lights a pass sends: every light when there are at most <paramref name="max"/>, else the
		/// <paramref name="max"/> nearest the eye (a ranged light by the distance to the edge of its reach). Fills
		/// <paramref name="pick"/> with their indices in list order and returns how many; keys and idx are scratch.</summary>
		internal static int PickLights(List<SceneLight> lights, double ex, double ey, double ez, int max, int[] pick, ref double[] keys, ref int[] idx) {
			int n = lights.Count;
			if (n <= max) { for (int i = 0; i < n; i++) pick[i] = i; return n; }
			if (keys.Length < n) { keys = new double[n]; idx = new int[n]; }
			for (int i = 0; i < n; i++) {
				var L = lights[i];
				double dx = L.X - ex, dy = L.Y - ey, dz = L.Z - ez, d = Math.Sqrt(dx * dx + dy * dy + dz * dz);
				keys[i] = L.Range > 0 ? d - L.Range : d;
				idx[i] = i;
			}
			Array.Sort(keys, idx, 0, n);
			Array.Copy(idx, pick, max);
			Array.Sort(pick, 0, max);
			return max;
		}
	}
}
