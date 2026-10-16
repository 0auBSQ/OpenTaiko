using System;
using System.Diagnostics;
using FDK;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	/// <summary>
	/// GPU rasterizer for <see cref="Lua3DScene"/> on the hardware graphics pipeline (GLES 3.1
	/// vertex/fragment shaders + HW triangle raster, depth, blending) — replacing the CPU
	/// <see cref="Rasterizer"/>. The per-pixel fill runs on the GPU; the CPU only assembles vertices,
	/// and that work is RETAINED so a static scene re-uploads nothing per frame.
	///
	/// Draw-call reduction is the key to matching/beating the CPU: static opaque geometry (textured, no
	/// transform, white tint, full alpha, lit) is MERGED by texture into a few big buffers, so e.g. a 500+
	/// object voxel world draws in ~one call per block texture. Everything else (transparent, transformed,
	/// tinted, flat, unlit) draws per object from its own retained VBO; billboard sprites rebuild per frame.
	///
	/// Forward lighting matches the CPU: per-vertex normal + per-fragment sqrt(shade)·(ambient +
	/// max(0,N·sun)·sun) + Σ point-lights (range or inverse-square). Cutout transparency: a negative texel
	/// (packed&lt;0) uploads as alpha 0 and is alpha-tested away. Output goes into the scene's canvas GL
	/// texture via an FBO over the Lua-drawn background; projection matches the CPU (vertically flipped to
	/// match the 2D draw).
	///
	/// The class is split by pass over the GpuRasterizer.*.cs files.
	/// </summary>
	internal sealed partial class GpuRasterizer : IRenderer, IRenderTargets, ICanvasOverlay {
		private GL Gl => Game.Gl;
		private const int STRIDE = 9;        // pos(3) uv(2) shade(1) normal(3)
		// Largest projection far plane. Each pass pulls its far plane (_far) in to the farthest camera depth that anything
		// it draws reaches (bounds of the drawn objects, buckets, sprites, particles and grass cells), so nothing drawn is
		// ever clipped by it; this cap keeps one huge object from stretching the range.
		private const double FAR = 3000.0;
		private double _far = FAR;           // this pass's far plane
		private double _reach;               // farthest camera depth reached by what this pass draws
		private ViewFrustum _view;           // this pass's camera, for culling and _reach

		private bool _init;
		private uint _prog, _vao, _spriteVbo;

		private uint _fbo, _depthRb;
		private int _fboW, _fboH; private uint _fboColorTex;

		private int _frame, _pruneAt;

		private void EnsureGL() {
			if (_init) return;
			_prog = CachedProg(Ascii(VS), Ascii("#version 300 es\n" + FS));
			_prog2d = CachedProg(VS2D, FS2D);
			_uRes2d = Gl.GetUniformLocation(_prog2d, "uRes"); _uColor2d = Gl.GetUniformLocation(_prog2d, "uColor");
			_vbo2d = Gl.GenBuffer();
			_locDef = QueryWLoc(_prog);
			try {   // the lit shader's SHADOWS variant + the two depth-pass programs (degrade to no-shadows on failure)
				_progShadow = CachedProg(Ascii(VS), Ascii("#version 300 es\n#define SHADOWS\n" + FS));
				_locSh = QueryWLoc(_progShadow);
				_progDepth = CachedProg(VSD, FSD);
				_uDLightVP = Gl.GetUniformLocation(_progDepth, "uLightVP"); _uDModel = Gl.GetUniformLocation(_progDepth, "uModel");
				_progDepthSpr = CachedProg(VSDS, FSDS);
				_uDSLightVP = Gl.GetUniformLocation(_progDepthSpr, "uLightVP"); _uDSTex = Gl.GetUniformLocation(_progDepthSpr, "uTex");
				_uDSModel = Gl.GetUniformLocation(_progDepthSpr, "uModel");
			} catch (Exception e) { Trace.TraceError("Shadow programs failed: " + e); _shadowOk = false; }
			BindWorldProgram(false);   // populate the active _u* set from the default variant
			// 3D VAO uses separate attribute format (GLES 3.1): the vertex layout is fixed once here, so
			// switching geometry buffers per draw is a single cheap glBindVertexBuffer (binding index 0).
			_vao = Gl.GenVertexArray();
			Gl.BindVertexArray(_vao);
			Gl.EnableVertexAttribArray(0); Gl.VertexAttribFormat(0, 3, VertexAttribType.Float, false, 0); Gl.VertexAttribBinding(0, 0);
			Gl.EnableVertexAttribArray(1); Gl.VertexAttribFormat(1, 2, VertexAttribType.Float, false, (uint)(3 * sizeof(float))); Gl.VertexAttribBinding(1, 0);
			Gl.EnableVertexAttribArray(2); Gl.VertexAttribFormat(2, 1, VertexAttribType.Float, false, (uint)(5 * sizeof(float))); Gl.VertexAttribBinding(2, 0);
			Gl.EnableVertexAttribArray(3); Gl.VertexAttribFormat(3, 3, VertexAttribType.Float, false, (uint)(6 * sizeof(float))); Gl.VertexAttribBinding(3, 0);
			Gl.BindVertexArray(0);
			_vao2d = Gl.GenVertexArray();   // 2D overlay (classic attrib pointers, set in Begin2D)
			_spriteVbo = Gl.GenBuffer();
			// particle program + its own VAO (pos3, uv2, rgba4)
			_progP = CachedProg(VSP, FSP);
			int P(string n) => Gl.GetUniformLocation(_progP, n);
			_pFrame = FrameLoc.Of(Gl, _progP); _pTextured = P("uTextured"); _pTex = P("uTex"); _pAdd = P("uAdd");
			_vaoP = Gl.GenVertexArray(); _partVbo = Gl.GenBuffer();
			Gl.BindVertexArray(_vaoP); Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _partVbo);
			unsafe {
				Gl.EnableVertexAttribArray(0); Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), (void*)0);
				Gl.EnableVertexAttribArray(1); Gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 9 * sizeof(float), (void*)(3 * sizeof(float)));
				Gl.EnableVertexAttribArray(2); Gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, 9 * sizeof(float), (void*)(5 * sizeof(float)));
			}
			Gl.BindVertexArray(0);
			// FXAA post program + an empty VAO (fullscreen triangle is generated from gl_VertexID)
			_progPost = CachedProg(Ascii(VSPOST), Ascii(FSPOST));
			_postTex = Gl.GetUniformLocation(_progPost, "uTex"); _postMode = Gl.GetUniformLocation(_progPost, "uMode"); _postRes = Gl.GetUniformLocation(_progPost, "uRes");
			_progDiorama = CachedProg(Ascii(VSPOST), Ascii(FSDIORAMA));
			_progOutline = CachedProg(Ascii(VSPOST), Ascii(FSOUTLINE));
			_oTex = Gl.GetUniformLocation(_progOutline, "uTex"); _oRes = Gl.GetUniformLocation(_progOutline, "uRes");
			_oColor = Gl.GetUniformLocation(_progOutline, "uColor"); _oThick = Gl.GetUniformLocation(_progOutline, "uThick");
			_hTex = Gl.GetUniformLocation(_progDiorama, "uTex"); _hRes = Gl.GetUniformLocation(_progDiorama, "uRes");
			_hTilt = Gl.GetUniformLocation(_progDiorama, "uTilt"); _hSat = Gl.GetUniformLocation(_progDiorama, "uSat"); _hVig = Gl.GetUniformLocation(_progDiorama, "uVig");
			_hBloom = Gl.GetUniformLocation(_progDiorama, "uBloom");
			_hExp = Gl.GetUniformLocation(_progDiorama, "uExposure"); _hGrade = Gl.GetUniformLocation(_progDiorama, "uGrade");
			_vaoPost = Gl.GenVertexArray();
			_postSampler = Gl.GenSampler();
			Gl.SamplerParameter(_postSampler, SamplerParameterI.MinFilter, (int)GLEnum.Nearest);
			Gl.SamplerParameter(_postSampler, SamplerParameterI.MagFilter, (int)GLEnum.Nearest);
			Gl.SamplerParameter(_postSampler, SamplerParameterI.WrapS, (int)GLEnum.Repeat);
			Gl.SamplerParameter(_postSampler, SamplerParameterI.WrapT, (int)GLEnum.Repeat);
			_postLinSampler = Gl.GenSampler();
			Gl.SamplerParameter(_postLinSampler, SamplerParameterI.MinFilter, (int)GLEnum.Linear);
			Gl.SamplerParameter(_postLinSampler, SamplerParameterI.MagFilter, (int)GLEnum.Linear);
			Gl.SamplerParameter(_postLinSampler, SamplerParameterI.WrapS, (int)GLEnum.ClampToEdge);
			Gl.SamplerParameter(_postLinSampler, SamplerParameterI.WrapT, (int)GLEnum.ClampToEdge);
			// grass program + VAO: one instance per blade (pos3, height+phase 2, shade1), separate format so a draw of a
			// run of cells only moves the buffer offset
			_progGrass = CachedProg(Ascii(VSGRASS), Ascii(FSGRASS));
			int G(string n) => Gl.GetUniformLocation(_progGrass, n);
			_gFrame = FrameLoc.Of(Gl, _progGrass);
			_gTime = G("uTime"); _gTex = G("uTex"); _gSun = G("uSunCol"); _gAmb = G("uAmb"); _gFade = G("uFade");
			_grassVbo = Gl.GenBuffer();
			_vaoGrass = Gl.GenVertexArray();
			Gl.BindVertexArray(_vaoGrass);
			Gl.EnableVertexAttribArray(0); Gl.VertexAttribFormat(0, 3, VertexAttribType.Float, false, 0); Gl.VertexAttribBinding(0, 0);
			Gl.EnableVertexAttribArray(1); Gl.VertexAttribFormat(1, 2, VertexAttribType.Float, false, (uint)(3 * sizeof(float))); Gl.VertexAttribBinding(1, 0);
			Gl.EnableVertexAttribArray(2); Gl.VertexAttribFormat(2, 1, VertexAttribType.Float, false, (uint)(5 * sizeof(float))); Gl.VertexAttribBinding(2, 0);
			Gl.VertexBindingDivisor(0, 1);
			Gl.BindVertexArray(0);
			_init = true;
		}

		// point the 3D VAO (separate format) at a vertex buffer — one call per draw
		private void Bind3D(uint vbo) => Gl.BindVertexBuffer(0, vbo, 0, (uint)(STRIDE * sizeof(float)));

		/// <summary>Last sky/post shader compile error ("" when clean) — surfaced to Lua so a broken
		/// stage shader is debuggable in-game instead of silently degrading.</summary>
		public string LastShaderError = "";
		public string ShaderError => LastShaderError;
		public string Name => "GPU rasterizer";
		public string Mode => "raster";
		public bool DrawsSkyShader => true;

		private void EnsureFbo(Lua3DScene s) {
			uint colorTex = s._canvas.Pointer;
			if (_fbo != 0 && _fboW == s._w && _fboH == s._h && _fboColorTex == colorTex) return;
			if (_fbo == 0) _fbo = Gl.GenFramebuffer();
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
			Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, colorTex, 0);
			// depth RENDERBUFFER (24-bit; avoids the edge z-fighting the old 16-bit buffer had). Plain renderbuffer
			// keeps the main pass fast (a sampleable depth texture lost a driver fast path → the volcano slowdown).
			if (_depthRb == 0) _depthRb = Gl.GenRenderbuffer();
			Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthRb);
			Gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24, (uint)s._w, (uint)s._h);
			Gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _depthRb);
			_fboW = s._w; _fboH = s._h; _fboColorTex = colorTex;
		}

		private bool _logged;
		public unsafe void Render(Lua3DScene s) {
			if (!_logged) { _logged = true; Trace.TraceInformation("GpuRasterizer ACTIVE (hardware pipeline, merged + lit)"); }
			EnsureGL();
			End2D();   // 2D drawn after an earlier Render goes back to its target first
			_frame++;
			var pass = GlPass.Begin(Gl);
			try { DrawFrame(s); }
			finally { pass.End(Gl); }
			s._gpuOwnsCanvas = true;

			if (_frame >= _pruneAt) { _pruneAt = _frame + 120; Prune(s); }
		}

		// every pass of one frame into the canvas; the caller restores its GL state around it
		private void DrawFrame(Lua3DScene s) {
			s._canvas.Upload();
			EnsureFbo(s);
			PreparePass(s);

			// ── SUN SHADOW MAP: depth from the light's POV. Casters = merged terrain, splat terrain, the other lit
			// opaque objects (models, props) + the character sprites (alpha-tested silhouette). Skipped unless the
			// scene opted in + is lit + has a sun, and redrawn only when what it shows changed.
			bool shadows = s._shadows && s._useLights && (s._sunR + s._sunG + s._sunB) > 0.0 && !s._rttPass && EnsureShadowFbo();
			if (shadows) {
				BuildLightVP(s);
				GatherCasters(s);
				UpdateShadowMap(s);
			}

			GlPass.Target(Gl, _fbo, s._w, s._h);
			// procedural SKY pass (replaces the 2D gradient fill): DrawWorld draws it behind the opaque world, and the frame
			// under it starts opaque black
			EnsureSky(s);
			_frameInTemp = FrameInTemp(s);
			if (_frameInTemp) { EnsureFxaaTarget(s); GlPass.Target(Gl, _fxaaFbo, s._w, s._h); }
			if (_skyOk) GlPass.ClearColorDepth(Gl, 0, 0, 0);
			else GlPass.ClearDepth(Gl);
			GlPass.DepthTested(Gl); Gl.DepthMask(true); Gl.Disable(EnableCap.CullFace);
			BindWorldProgram(shadows); Gl.BindVertexArray(_vao);
			SetFrameUniforms(s);
			if (_worldShadow) {   // only the shadow variant reads these: the light matrix + the depth map on unit 1
				Gl.UniformMatrix4(_uLightVP, 1, false, (ReadOnlySpan<float>)_lightVP);
				Gl.Uniform1(_uShadowTexel, 1.0f / SHADOW_SIZE);
				Gl.ActiveTexture(TextureUnit.Texture1); Gl.BindTexture(TextureTarget.Texture2D, _shadowTex); Gl.Uniform1(_uShadow, 1);
				Gl.ActiveTexture(TextureUnit.Texture0);
			}

			int litScene = s._useLights ? 1 : 0;
			DrawWorld(s, litScene, s._w, s._h, false);

			// overlay pass: viewmodels over a freshly-cleared depth buffer → always on top of the world
			if (_overlay.Count > 0) {
				BindWorldProgram(false); Gl.BindVertexArray(_vao);   // viewmodels: no world shadows
				SetFrameUniforms(s);                                 // this program may still hold another pass's camera
				GlPass.ClearDepth(Gl);
				Gl.Enable(EnableCap.DepthTest); Gl.Enable(EnableCap.Blend); GlPass.BlendCover(Gl);
				foreach (var o in _overlay) DrawObj(s, o, litScene);
			}
			InvalidateDepth();   // nothing reads this frame's depth again: a tiler need not store it
			PostChain(s);
		}

		// What a pass draws, gathered from its camera: the view, the sprites, the current merge, the objects, merge
		// buckets and grass cells in view, the transparent queue with the particles placed in it, and the far plane that
		// holds everything drawn.
		private void PreparePass(Lua3DScene s) {
			_scene = s;
			_view = ViewFrustum.Of(s);
			_reach = 0;
			BuildSprites(s);
			UpdateMerged(s);   // synchronous, per-bucket → no spike
			GatherObjects(s);
			BuildQueue(s);
			BuildParticles(s);
			GatherGrass(s);
			// rounded up to a quarter octave, so the depth mapping (and any coplanar z-fight pattern) holds still while
			// a particle or a walking sprite moves the farthest point a little
			double far = Math.Max(_reach * 1.02 + 1.0, s._near + 1.0);
			_far = Math.Min(FAR, Math.Pow(2, Math.Ceiling(Math.Log2(far) * 4) / 4));
		}
		private void Reach(double depth) { if (depth > _reach) _reach = depth; }
		private void ReachPt(double x, double y, double z) => Reach(_view.Depth(x, y, z));

		// the bound target's depth is not needed any more: the driver may drop it instead of storing it
		private unsafe void InvalidateDepth() {
			var att = InvalidateFramebufferAttachment.DepthAttachment;
			Gl.InvalidateFramebuffer(FramebufferTarget.Framebuffer, 1, &att);
		}

		/// <summary>Free every REBUILDABLE buffer: per-object VBOs, merged static buffers, reflection
		/// RTT targets and the grass VBO state. Programs, the main FBO, the shadow map and uploaded
		/// textures/sprites survive (per-stage). Everything re-creates lazily next frame.</summary>
		public void ClearTransient() {
			foreach (var c in _cache.Values) FreeBuffers(c);
			_cache.Clear();
			ClearPalette();
			foreach (var sp in _surfProgs.Values) { sp.Dead = true; if (sp.Prog != 0) Gl.DeleteProgram(sp.Prog); }
			_surfProgs.Clear();
			ClearMerge();
			_tmp.Clear(); _tmp.TrimExcess();
			_uploadBuf = null;
			_sprVerts.Clear(); _sprVerts.TrimExcess(); _sprBatches.Clear();
			_partGroup.Clear(); _partGroupTex.Clear(); _partRunOf.Clear(); _partRuns.Clear(); _partAll.Clear(); _partAll.TrimExcess();
			_pRec = new PartRec[256]; _pFar = new uint[256]; _pOrd = new int[256]; _pN = 0; _runRank = new int[32];
			foreach (var f in _rttFbos.Values) Gl.DeleteFramebuffer(f); _rttFbos.Clear();
			foreach (var d in _rttDepth.Values) Gl.DeleteRenderbuffer(d); _rttDepth.Clear();
			foreach (var t in _rttColor.Values) Gl.DeleteTexture(t); _rttColor.Clear();
			_rttW = _rttH = 0; _rttRev++;
			ClearGrass();
		}

		/// <summary>Cache counts for the memory readout.</summary>
		public string Stats => $"glTex={_glTex.Count} glSprite={_glSprite.Count} rtt={_rttColor.Count} vbo={_cache.Count} merge={_merge.Count}";

		public void Invalidate() {
			foreach (var sp in _surfProgs.Values) { sp.Dead = true; if (sp.Prog != 0) Gl.DeleteProgram(sp.Prog); }
			_surfProgs.Clear();
			if (_progUser != 0) { Gl.DeleteProgram(_progUser); _progUser = 0; }
			_userPostRev = -1; _userPostOk = false;   // force a recompile against the (possibly new) GL context
			if (_progSky != 0) { Gl.DeleteProgram(_progSky); _progSky = 0; }
			_skyRev = -1; _skyOk = false;
			if (_noiseTex != 0) { Gl.DeleteTexture(_noiseTex); _noiseTex = 0; }
			ClearGrass();                             // grass VBO re-uploads on next use
			if (_fbo != 0) { Gl.DeleteFramebuffer(_fbo); _fbo = 0; }
			if (_depthRb != 0) { Gl.DeleteRenderbuffer(_depthRb); _depthRb = 0; }
			if (_shadowFbo != 0) { Gl.DeleteFramebuffer(_shadowFbo); _shadowFbo = 0; }
			if (_shadowTex != 0) { Gl.DeleteTexture(_shadowTex); _shadowTex = 0; }
			if (_shadowStaticFbo != 0) { Gl.DeleteFramebuffer(_shadowStaticFbo); _shadowStaticFbo = 0; }
			if (_shadowStaticTex != 0) { Gl.DeleteTexture(_shadowStaticTex); _shadowStaticTex = 0; }
			_casterVboSet = false;
			if (_fxaaFbo != 0) { Gl.DeleteFramebuffer(_fxaaFbo); _fxaaFbo = 0; }
			if (_fxaaTex != 0) { Gl.DeleteTexture(_fxaaTex); _fxaaTex = 0; }
			foreach (var f in _rttFbos.Values) Gl.DeleteFramebuffer(f); _rttFbos.Clear();
			foreach (var d in _rttDepth.Values) Gl.DeleteRenderbuffer(d); _rttDepth.Clear();
			foreach (var t in _rttColor.Values) Gl.DeleteTexture(t);
			_rttColor.Clear(); _rttW = _rttH = 0; _rttRev++;
			_fboW = _fboH = 0; _fboColorTex = 0; _fxaaW = _fxaaH = 0;
			foreach (var c in _cache.Values) FreeBuffers(c);
			_cache.Clear();
			ClearPalette();
			ClearMerge();
			foreach (var e in _glTex.Values) Gl.DeleteTexture(e.Tex);
			foreach (var e in _glSprite.Values) Gl.DeleteTexture(e.Tex);
			_glTex.Clear(); _glSprite.Clear(); _uploadBuf = null;
		}

		// Free EVERY GL resource, including the one-time programs/VAOs/buffers EnsureGL creates. Called
		// from Lua3DScene.Dispose so a stage exit doesn't leak the renderer's GL objects.
		public void Dispose() {
			if (!_init) { Invalidate(); return; }
			Invalidate();
			if (_vao != 0) Gl.DeleteVertexArray(_vao);
			if (_vao2d != 0) Gl.DeleteVertexArray(_vao2d);
			if (_vaoP != 0) Gl.DeleteVertexArray(_vaoP);
			if (_vaoPost != 0) Gl.DeleteVertexArray(_vaoPost);
			if (_vaoGrass != 0) Gl.DeleteVertexArray(_vaoGrass);
			if (_grassVbo != 0) Gl.DeleteBuffer(_grassVbo);
			if (_spriteVbo != 0) Gl.DeleteBuffer(_spriteVbo);
			if (_casterVbo != 0) Gl.DeleteBuffer(_casterVbo);
			if (_vbo2d != 0) Gl.DeleteBuffer(_vbo2d);
			if (_partVbo != 0) Gl.DeleteBuffer(_partVbo);
			if (_postSampler != 0) { Gl.DeleteSampler(_postSampler); _postSampler = 0; }
			if (_postLinSampler != 0) { Gl.DeleteSampler(_postLinSampler); _postLinSampler = 0; }
			// NOTE: the shader PROGRAMS (_prog/_progShadow/_progDepth/_progDepthSpr/_prog2d/_progP/_progPost/
			// _progDiorama/_progOutline/_progGrass) are SHARED via s_progCache (compiled once for the whole
			// context) — do NOT delete them here or the next scene's cache hit would be a dangling handle.
			_vaoGrass = _grassVbo = _progGrass = 0;
			_vao = _vao2d = _vaoP = _vaoPost = _spriteVbo = _casterVbo = _vbo2d = _partVbo = 0;
			_prog = _progShadow = _progDepth = _progDepthSpr = _prog2d = _progP = _progPost = _progDiorama = _progOutline = 0;
			_init = false;
		}
	}
}
