using System;
using System.Diagnostics;
using FDK;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	// The sky pass and the post chain (diorama grade, custom post shader, FXAA, outline).
	internal sealed partial class GpuRasterizer {
		// FXAA-lite post pass (optional, SetAntialias): edge-aware blend. _fxaaTex is the temp target every post pass
		// ping-pongs with the canvas (PostChain)
		private uint _progPost, _vaoPost, _fxaaTex, _fxaaFbo; private int _postTex, _postMode, _postRes; private int _fxaaW, _fxaaH;
		private uint _postSampler;   // post passes read the canvas and the temp target alike: nearest, repeat
		private uint _postLinSampler;   // FXAA reads between pixels: linear, clamped
		// the frame's 3D is drawn into the temp target instead of the canvas (DrawFrame, PostChain)
		private bool _frameInTemp;
		// diorama post pass (SetDiorama, "diorama"): a cheap SCREEN-SPACE tilt-shift (blur grows toward
		// the top/bottom, a sharp middle band) + saturation/contrast/vignette grade. No depth sample (the old
		// depth blur was too costly + the depth-texture FBO slowed the main pass), so the main FBO is back to a
		// plain depth renderbuffer.
		private uint _progDiorama; private int _hTex, _hRes, _hTilt, _hSat, _hVig, _hBloom, _hExp, _hGrade;
		// Lua-attached CUSTOM post pass (SetPostShader): compiled lazily when the source revision changes;
		// a compile failure traces once and disables the pass until the source changes again.
		private uint _progUser; private int _upTex, _upRes, _upTime, _upUser; private int _userPostRev = -1; private bool _userPostOk;
		// Lua-attached SKY pass (SetSkyShader): same lifecycle, drawn behind the opaque world (DrawWorld).
		private uint _progSky; private int _skRes, _skTime, _skUser, _skNoise; private int _skyRev = -1; private bool _skyOk;
		// the noise texture a sky shader reads through uNoise when it declares it: a random byte per point of a 256 x 256
		// lattice that wraps, and texel (x, y) holds the bytes of points (x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1)
		// in r, g, b, a, so one fetch gives a lattice cell's four corners (NoiseTex)
		private uint _noiseTex; private const int NOISE = 256, NOISE_UNIT = 7;

		// toon OUTLINE post pass (3D characters): true-distance silhouette ink with an AA rim
		private uint _progOutline; private int _oTex, _oRes, _oColor, _oThick;

		private unsafe void EnsureFxaaTarget(Lua3DScene s) {
			if (_fxaaTex != 0 && _fxaaW == s._w && _fxaaH == s._h) return;
			if (_fxaaTex == 0) _fxaaTex = Gl.GenTexture();
			Gl.BindTexture(TextureTarget.Texture2D, _fxaaTex);
			Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8, (uint)s._w, (uint)s._h, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (void*)0);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
			Gl.BindTexture(TextureTarget.Texture2D, 0);   // the 3D may draw into it next: a target left bound for sampling is a feedback loop
			if (_fxaaFbo == 0) _fxaaFbo = Gl.GenFramebuffer();
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fxaaFbo);
			Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _fxaaTex, 0);
			// the main depth buffer too, for the frames whose 3D is drawn here
			Gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _depthRb);
			_fxaaW = s._w; _fxaaH = s._h;
		}

		// (re)compile the Lua-attached post shader when its source revision changes. Accepts a full
		// GLSL ES 3.00 fragment source, or just a main() body-holder — the standard prelude (uTex =
		// the rendered frame, uRes, uTime, uUser[4], out frag) is prepended when no #version is given.
		private void EnsureUserPost(Lua3DScene s) {
			if (s._postSrcRev == _userPostRev) return;
			_userPostRev = s._postSrcRev;
			if (_progUser != 0) { Gl.DeleteProgram(_progUser); _progUser = 0; }
			_userPostOk = false;
			if (string.IsNullOrEmpty(s._postSrc)) return;
			string src = s._postSrc.Contains("#version") ? s._postSrc
				: "#version 300 es\nprecision highp float;\nuniform sampler2D uTex; uniform vec2 uRes; uniform float uTime; uniform vec4 uUser[4];\nout vec4 frag;\n" + s._postSrc;
			try {
				_progUser = ShaderHelper.CreateShaderProgramFromSource(Ascii(VSPOST), Ascii(src));
				_upTex = Gl.GetUniformLocation(_progUser, "uTex"); _upRes = Gl.GetUniformLocation(_progUser, "uRes");
				_upTime = Gl.GetUniformLocation(_progUser, "uTime"); _upUser = Gl.GetUniformLocation(_progUser, "uUser[0]");
				_userPostOk = true;
			} catch (Exception e) {
				LastShaderError = "post: " + e.Message;
				Trace.TraceError("SetPostShader: custom post shader failed to compile, pass disabled.\n" + e);
			}
		}

		// (re)compile the Lua-attached SKY shader (same prelude rules as the post pass, minus uTex;
		// the sky gets EIGHT uUser slots — camera basis + sun + palette + clouds/stars need the room)
		private void EnsureSky(Lua3DScene s) {
			if (s._skySrcRev == _skyRev) return;
			_skyRev = s._skySrcRev;
			if (_progSky != 0) { Gl.DeleteProgram(_progSky); _progSky = 0; }
			_skyOk = false;
			if (string.IsNullOrEmpty(s._skySrc)) { s._skyCompiled = null; return; }
			string src = s._skySrc.Contains("#version") ? s._skySrc
				: "#version 300 es\nprecision highp float;\nuniform vec2 uRes; uniform float uTime; uniform vec4 uUser[8];\nout vec4 frag;\n" + s._skySrc;
			try {
				_progSky = ShaderHelper.CreateShaderProgramFromSource(Ascii(VSSKY), Ascii(src));
				_skRes = Gl.GetUniformLocation(_progSky, "uRes");
				_skTime = Gl.GetUniformLocation(_progSky, "uTime");
				_skUser = Gl.GetUniformLocation(_progSky, "uUser[0]");
				_skNoise = Gl.GetUniformLocation(_progSky, "uNoise");
				_skyOk = true;
			} catch (Exception e) {
				LastShaderError = "sky: " + e.Message;
				Trace.TraceError("SetSkyShader: sky shader failed to compile, pass disabled.\n" + e);
			}
			s._skyCompiled = _skyOk;   // SkyShaderActive tells the stage to fall back to 2D bands on failure
		}

		// the sky's noise texture, made on first use (nearest; sky shaders read it with texelFetch)
		private unsafe uint NoiseTex() {
			if (_noiseTex != 0) return _noiseTex;
			var px = NoiseTexels();
			_noiseTex = Gl.GenTexture();
			Gl.BindTexture(TextureTarget.Texture2D, _noiseTex);
			fixed (byte* p = px) Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8, NOISE, NOISE, 0, PixelFormat.Rgba, PixelType.UnsignedByte, p);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
			return _noiseTex;
		}
		// The noise texture's RGBA texels, row by row. For each v the lattice points (37k, v + 17k), k = 0..255, form a
		// line (the lattice wraps), and a sky shader reads 3D point (x, y, z) at (x + 37z, y + 17z), so a step in z is a
		// step along a line. Along a line the bytes are the last step of an integer hash of a counter: neighbours in z
		// alternate high and low with a slow drift, neighbours in x and y are unrelated.
		internal static byte[] NoiseTexels() {
			const int M = NOISE - 1;
			var r = new byte[NOISE * NOISE];
			for (int v = 0; v < NOISE; v++) {
				uint q = LineStart(v);
				for (int k = 0; k < NOISE; k++) r[((v + 17 * k) & M) * NOISE + ((37 * k) & M)] = HashStep(unchecked(q + (uint)k));
			}
			var px = new byte[NOISE * NOISE * 4];
			for (int y = 0; y < NOISE; y++)
				for (int x = 0; x < NOISE; x++) {
					int o = (y * NOISE + x) * 4, x1 = (x + 1) & M, y1 = (y + 1) & M;
					px[o] = r[y * NOISE + x]; px[o + 1] = r[y * NOISE + x1];
					px[o + 2] = r[y1 * NOISE + x]; px[o + 3] = r[y1 * NOISE + x1];
				}
			return px;
		}
		// a line's random start: the first steps of an integer hash of v
		private static uint LineStart(int v) {
			unchecked {
				uint q = 2891336453u;
				q = (q ^ (q >> 16)) * 2246822519u + (uint)v;
				return (q ^ (q >> 13)) * 3266489917u;
			}
		}
		// the hash's last step, as a byte
		private static byte HashStep(uint q) {
			unchecked {
				q = (q ^ (q >> 16)) * 2246822519u;
				return (byte)((q ^ (q >> 15)) >> 24);
			}
		}

		// The sky shader on the far plane of the bound target (w x h pixels), drawn after the opaque world: the depth test
		// leaves it only the pixels nothing covers. It ray-marches from the camera basis that sky.lua packs into
		// uUser[0..2] for the main view; a reflection pass (ownBasis) puts its own reflected camera there, so the mirrored
		// sky faces the right way.
		private void DrawSky(Lua3DScene s, int w, int h, bool ownBasis) {
			GlPass.Backdrop(Gl);
			Gl.UseProgram(_progSky); Gl.BindVertexArray(_vaoPost);
			Gl.Uniform2(_skRes, (float)w, (float)h);
			Gl.Uniform1(_skTime, (float)s._shaderTime);
			if (_skNoise >= 0) {
				Gl.ActiveTexture(TextureUnit.Texture0 + NOISE_UNIT); Gl.BindTexture(TextureTarget.Texture2D, NoiseTex());
				Gl.Uniform1(_skNoise, NOISE_UNIT);
				Gl.ActiveTexture(TextureUnit.Texture0);
			}
			if (_skUser >= 0 && !ownBasis) Gl.Uniform4(_skUser, 8u, (ReadOnlySpan<float>)s._skyUser);
			else if (_skUser >= 0) {
				Span<float> su = stackalloc float[32];
				for (int i = 0; i < 32; i++) su[i] = s._skyUser[i];
				su[0] = (float)s._Fx; su[1] = (float)s._Fy; su[2] = (float)s._Fz;      // [0].xyz forward
				su[4] = (float)s._Rx; su[5] = (float)s._Ry; su[6] = (float)s._Rz;      // [1].xyz right
				su[8] = (float)s._Ux; su[9] = (float)s._Uy; su[10] = (float)s._Uz;     // [2].xyz up
				Gl.Uniform4(_skUser, 8u, (ReadOnlySpan<float>)su);
			}
			Gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
		}

		// The post effects in order (diorama grade, the stage's post shader, FXAA, outline). Each reads the frame from
		// one target and writes the other, the canvas and the temp target in turn, so k effects cost k passes. The last
		// effect must write the canvas, so one copy into the other target comes first when the frame starts in the wrong
		// one: the 3D in the canvas and k odd, or the 3D in the temp target (FrameInTemp) and k even.
		// A sampler gives both sources the temp target's nearest/repeat sampling.
		private void PostChain(Lua3DScene s) {
			int k = PostPasses(s, out bool dio, out bool user, out bool aa, out bool outline);
			bool inCanvas = !_frameInTemp;   // where the frame is now
			if (k == 0 && inCanvas) return;
			EnsureFxaaTarget(s);
			GlPass.FullScreen(Gl);
			Gl.BindVertexArray(_vaoPost);
			Gl.ActiveTexture(TextureUnit.Texture0);
			Gl.BindSampler(0, _postSampler);
			if (((k & 1) == 1) == inCanvas) {   // copy into the other target
				Gl.UseProgram(_progPost);
				Gl.Uniform1(_postTex, 0); Gl.Uniform2(_postRes, (float)s._w, (float)s._h); Gl.Uniform1(_postMode, 0);
				PostPass(s, ref inCanvas);
			}
			// diorama post pass ('diorama' grade): tilt-shift + colour grade. Only diorama scenes opt in.
			if (dio) {
				Gl.UseProgram(_progDiorama);
				Gl.Uniform1(_hTex, 0); Gl.Uniform2(_hRes, (float)s._w, (float)s._h);
				Gl.Uniform1(_hTilt, (float)s._hdTilt); Gl.Uniform1(_hSat, (float)s._hdSat); Gl.Uniform1(_hVig, (float)s._hdVig);
				Gl.Uniform1(_hBloom, (float)s._hdBloom);
				Gl.Uniform1(_hExp, (float)s._gradeExp); Gl.Uniform3(_hGrade, (float)s._gradeR, (float)s._gradeG, (float)s._gradeB);
				PostPass(s, ref inCanvas);
			}
			// Lua-attached custom post pass (SetPostShader): after the diorama grade (it sees the graded frame), before
			// FXAA/outline.
			if (user) {
				Gl.UseProgram(_progUser);
				Gl.Uniform1(_upTex, 0); Gl.Uniform2(_upRes, (float)s._w, (float)s._h);
				Gl.Uniform1(_upTime, (float)s._shaderTime);
				if (_upUser >= 0) Gl.Uniform4(_upUser, 4u, (ReadOnlySpan<float>)s._postUser);
				PostPass(s, ref inCanvas);
			}
			// FXAA post pass (before the stage's 2D overlay so the crosshair stays sharp): edge search along each
			// edge, then a sample shifted across it
			if (aa) {
				Gl.UseProgram(_progPost);
				Gl.Uniform1(_postTex, 0); Gl.Uniform2(_postRes, (float)s._w, (float)s._h); Gl.Uniform1(_postMode, 1);
				Gl.BindSampler(0, _postLinSampler);
				PostPass(s, ref inCanvas);
				Gl.BindSampler(0, _postSampler);
			}
			// toon outline post pass (3D characters): ink around the shapes
			if (outline) {
				Gl.UseProgram(_progOutline);
				Gl.Uniform1(_oTex, 0); Gl.Uniform2(_oRes, (float)s._w, (float)s._h);
				Gl.Uniform3(_oColor, (float)(s._outlineR / 255.0), (float)(s._outlineG / 255.0), (float)(s._outlineB / 255.0));
				Gl.Uniform1(_oThick, (float)s._outlineTh);
				PostPass(s, ref inCanvas);
			}
			Gl.BindSampler(0, 0);
		}
		// the number of post effects this frame runs, and which
		private int PostPasses(Lua3DScene s, out bool dio, out bool user, out bool aa, out bool outline) {
			dio = s._diorama && !s._rttPass;
			EnsureUserPost(s);
			user = _userPostOk && !s._rttPass; aa = s._aa; outline = s._outlineTh > 0 && !s._rttPass;
			return (dio ? 1 : 0) + (user ? 1 : 0) + (aa ? 1 : 0) + (outline ? 1 : 0);
		}
		// The frame's 3D can go straight into the temp target, so the post chain ends in the canvas with no first copy,
		// when the chain has an odd number of effects and a sky shader is attached (the frame starts from a clear, so
		// nothing in the canvas shows through). Not with the stage's post shader: where it discards, the canvas keeps
		// what it held, and that must be this frame's 3D.
		private bool FrameInTemp(Lua3DScene s) => _skyOk && (PostPasses(s, out _, out bool user, out _, out _) & 1) == 1 && !user;
		// one full-screen pass from the target holding the frame into the other one
		private void PostPass(Lua3DScene s, ref bool inCanvas) {
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, inCanvas ? _fxaaFbo : _fbo);
			Gl.BindTexture(TextureTarget.Texture2D, inCanvas ? s._canvas.Pointer : _fxaaTex);
			Gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
			inCanvas = !inCanvas;
		}
	}
}
