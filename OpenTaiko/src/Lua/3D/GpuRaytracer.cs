using System;
using System.Collections.Generic;
using System.Diagnostics;
using FDK;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	/// <summary>
	/// GPU progressive path tracer for <see cref="Lua3DScene"/> on a GLES 3.1 compute shader — the GPU
	/// counterpart of the CPU <see cref="Raytracer"/>. It mirrors that integrator exactly (Möller–Trumbore
	/// triangles through the shared <see cref="RtScene"/> two-level BVH with alpha-tested holes + analytic
	/// sphere/plane/box + bounded ray-marched SDF presets, diffuse/GGX metal/glass/emissive materials, procedural +
	/// texture normal maps, next-event estimation of point lights, the sun and emissive spheres, a sky gradient with
	/// the ambient light, Russian roulette, MaxDepth 8) but traces every pixel in parallel.
	///
	/// The snapshot lives in three storage buffers (BVH nodes + triangles; primitives + instances + top-level nodes;
	/// materials + lights + texture rects) and a texture atlas, each updated only where <see cref="RtScene.Update"/>
	/// saw a change; accumulation restarts on any <see cref="Lua3DScene._rtRevision"/> change (camera moves included).
	/// Radiance and the per-pixel sample count accumulate into two ping-ponged rgba32f images; a fullscreen present
	/// pass divides, Reinhard-tonemaps + gamma-encodes, and writes straight into the scene's canvas texture (so
	/// <see cref="Lua3DScene.Upload"/> skips the CPU buffer, as with the GPU rasterizer).
	///
	/// The samples per frame follow the measured GPU cost of a sample. A scene whose single sample overruns the frame
	/// budget traces a quarter of the pixels while moving and row bands over several frames while still, and every
	/// dispatch is cut into strips short enough to stay clear of a driver watchdog. After <see cref="MaxSamples"/>
	/// samples a still view only presents.
	/// </summary>
	internal sealed class GpuRaytracer : IRenderer {
		private GL Gl => Game.Gl;

		// Adaptive samples-per-frame: the GPU cost of a sample is measured (glFinish around the dispatch) when the view
		// settles and again every RecalFrames still frames, and the spp per frame fills a frame-time budget — so simple
		// scenes converge fast and heavy scenes stay smooth.
		private const double BUDGET_MS = 9.0;   // GPU time/frame to aim for when settled (leaves room for present + the rest of the frame)
		private const int MAX_SPP = 256;
		/// <summary>Samples per pixel after which a still view stops tracing and only presents.</summary>
		internal const int MaxSamples = Raytracer.MaxSamples;
		private const double StripMs = 50.0;   // estimated GPU time one dispatch may take: far below a driver watchdog's limit
		private const int RecalFrames = 60;    // still frames between two cost measurements

		private bool _init;
		private uint _progRT, _progPresent, _vaoPresent;
		private int _variant = -1;
		// compute uniforms
		private int _uEye, _uR, _uU, _uF, _uW, _uH, _uSPP, _uReset, _uHalf, _uRowOff, _uTraceY0, _uTraceY1, _uScale;
		private int _uTriOff, _uNumInst, _uInstOff, _uTlasOff, _uNumPrim, _uNumMat, _uNumLight, _uNumRect, _uLightOff, _uRectOff, _uAtlas;
		private int _uSkyB, _uSkyT, _uSkyStrength, _uSunDir, _uSunCol, _uAmb;
		// present uniforms
		private int _pAccum;

		// Two rgba32f accumulation images, ping-ponged each frame: GLES forbids a rgba32f image being
		// read AND written in one shader (only r32f/r32i/r32ui may be readwrite), so we read the previous
		// sum from one (readonly) and write the new sum to the other (writeonly), then swap. _accumA holds the latest.
		private uint _accumA, _accumB; private int _aw, _ah;
		private uint _fbo; private int _fboW, _fboH; private uint _fboColorTex;

		private readonly RtScene _rt = new();
		private uint _geoSsbo, _topSsbo, _auxSsbo;   // 3 SSBOs: the GLES 3.1 minimum is 4 compute storage blocks
		private int _geoCap, _topCap, _auxCap;       // their sizes in floats
		private float[] _aux = new float[1];         // [materials][lights][rects]
		private int _lightOff, _rectOff, _nRect;     // offsets in floats into aux, and the rects it holds
		private uint _atlasTex;
		private int[] _rect = Array.Empty<int>();    // per slot: atlas x, y, packed w, h, and the downscale shift (w = 0: not packed)
		internal const int RECT = 6;                 // floats per slot in aux: atlas x, y, w, h, cut mode, normal-map slot
		private bool _atlasWarned;

		private int _lastRevision = -1, _w, _h, _samples, _band;
		private bool _reset = true;

		// measured cost of one sample per pixel over the whole image, the fixed cost of a dispatch, and the spp/frame
		private readonly Stopwatch _calSw = new();
		private double _costMs = -1, _overheadMs, _t1;
		private int _spp = 1, _calStage = 1, _sinceCal, _calLog;   // _calStage: 1 = measure 1 spp next, 2 = then 4 spp, 0 = done

		// If the compute shader can't compile/link on this driver, fall back to the CPU path tracer so the
		// stage still renders (no crash, no blank screen). Tracks the failure so we don't retry every frame.
		private bool _broken;
		private Raytracer? _cpuFallback;

		/// <summary>Human-readable backend state for an on-screen readout: "GPU compute …" or "CPU fallback: …".</summary>
		public string Status { get; private set; } = "GPU compute (initialising)";

		/// <summary>Samples-per-pixel accumulated since the last reset (drives the convergence readout).</summary>
		public int SampleCount => _broken ? (_cpuFallback?.SampleCount ?? 0) : _samples;
		public string Name => "GPU path tracer";
		public string Mode => "raytrace";
		public string TracerStatus(string fallbackNote) => Status;

		public void Invalidate() {
			_lastRevision = -1; _samples = 0; _band = 0; _reset = true;
			_costMs = -1; _overheadMs = 0; _calStage = 1; _sinceCal = 0;
			_cpuFallback?.Invalidate();
			FreeFrameTargets();
			if (_geoSsbo != 0) { Gl.DeleteBuffer(_geoSsbo); _geoSsbo = 0; }
			if (_topSsbo != 0) { Gl.DeleteBuffer(_topSsbo); _topSsbo = 0; }
			if (_auxSsbo != 0) { Gl.DeleteBuffer(_auxSsbo); _auxSsbo = 0; }
			_geoCap = _topCap = _auxCap = 0;
			if (_atlasTex != 0) { Gl.DeleteTexture(_atlasTex); _atlasTex = 0; }
			_rt.MarkAllChanged();   // every array goes up again
		}
		private void FreeFrameTargets() {
			if (_accumA != 0) { Gl.DeleteTexture(_accumA); _accumA = 0; }
			if (_accumB != 0) { Gl.DeleteTexture(_accumB); _accumB = 0; }
			if (_fbo != 0) { Gl.DeleteFramebuffer(_fbo); _fbo = 0; }
			_aw = _ah = 0; _fboW = _fboH = 0; _fboColorTex = 0;
		}

		public void Dispose() {
			Invalidate();
			if (_vaoPresent != 0) { Gl.DeleteVertexArray(_vaoPresent); _vaoPresent = 0; }
			// the programs are shared through s_progCache: the next scene reuses them
			_progRT = _progPresent = 0; _variant = -1;
			_init = false;
		}

		private bool _logged;
		public unsafe void Render(Lua3DScene s) {
			if (!_logged) { _logged = true; Trace.TraceInformation("GpuRaytracer ACTIVE (GLES 3.1 compute path tracer)"); }
			EnsureGL();
			if (!_broken) Prepare(s);
			if (_broken) { s._gpuOwnsCanvas = false; (_cpuFallback ??= new Raytracer()).Render(s); return; }   // driver couldn't build the kernel → CPU path tracer fills the canvas buffer

			// the caller's GL state, put back after the frame; the buffer and image bindings of the trace are released
			Span<int> vp = stackalloc int[4];
			Gl.GetInteger(GLEnum.Viewport, vp);
			uint prevFbo = (uint)Gl.GetInteger(GLEnum.FramebufferBinding);
			uint prevProg = (uint)Gl.GetInteger(GLEnum.CurrentProgram);
			uint prevVao = (uint)Gl.GetInteger(GLEnum.VertexArrayBinding);
			int prevUnit = Gl.GetInteger(GLEnum.ActiveTexture);
			Gl.ActiveTexture(TextureUnit.Texture0);
			uint prevTex = (uint)Gl.GetInteger(GLEnum.TextureBinding2D);
			bool blend = Gl.IsEnabled(EnableCap.Blend), depthTest = Gl.IsEnabled(EnableCap.DepthTest);
			bool cull = Gl.IsEnabled(EnableCap.CullFace), scissor = Gl.IsEnabled(EnableCap.ScissorTest);
			bool depthMask = Gl.GetBoolean(GLEnum.DepthWritemask);
			Gl.Disable(EnableCap.ScissorTest);   // a UI clip is in screen space: it must not cut the present pass
			try { TraceFrame(s); }
			finally {
				Gl.BindFramebuffer(FramebufferTarget.Framebuffer, prevFbo);
				Gl.Viewport(vp[0], vp[1], (uint)vp[2], (uint)vp[3]);
				for (uint i = 1; i <= 3; i++) Gl.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, i, 0);
				Gl.BindBuffer(BufferTargetARB.ShaderStorageBuffer, 0);
				Gl.BindImageTexture(0, 0, 0, false, 0, GLEnum.ReadOnly, GLEnum.Rgba32f);
				Gl.BindImageTexture(1, 0, 0, false, 0, GLEnum.WriteOnly, GLEnum.Rgba32f);
				Gl.UseProgram(prevProg); Gl.BindVertexArray(prevVao);
				Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, prevTex);
				Gl.ActiveTexture((TextureUnit)prevUnit);
				SetCap(EnableCap.Blend, blend); SetCap(EnableCap.DepthTest, depthTest);
				SetCap(EnableCap.CullFace, cull); SetCap(EnableCap.ScissorTest, scissor);
				Gl.DepthMask(depthMask);
			}
			s._gpuOwnsCanvas = true;   // Upload() will skip the (stale) CPU buffer
		}
		private void SetCap(EnableCap cap, bool on) { if (on) Gl.Enable(cap); else Gl.Disable(cap); }

		// The scene snapshot (CPU side, only after an RT edit) and the kernel variant it needs; the GL uploads follow in TraceFrame.
		private void Prepare(Lua3DScene s) {
			bool resize = _w != s._w || _h != s._h;
			if (resize) { _w = s._w; _h = s._h; }
			if (resize || s._rtRevision != _lastRevision) {
				_lastRevision = s._rtRevision;
				// new content or size: measure the cost again from scratch once the view settles; the same content seen
				// from a new view: one re-measurement on the first still frame
				if (_rt.Update(s) || resize) _calStage = 1;
				else _sinceCal = RecalFrames;
				_reset = true; _samples = 0; _band = 0;
			}
			int variant = (_rt.HasSdf ? 1 : 0) | (_rt.HasNormalMaps ? 2 : 0) | (_rt.HasCutout ? 4 : 0);
			if (variant != _variant || !Gl.IsProgram(_progRT)) UseVariant(variant);
		}

		// one batch of samples into the accumulation image, then the present pass into the canvas texture
		private unsafe void TraceFrame(Lua3DScene s) {
			EnsureAccum(s);   // a new size restarts the accumulation
			UploadScene();
			if (_reset || _samples < MaxSamples) Dispatch(s);
			Present(s);
		}

		// ── path-trace the frame's samples into the accumulation image ───────────────────
		private void Dispatch(Lua3DScene s) {
			// While moving (each frame a reset): 1 spp, a quarter of the pixels when one sample costs more than the budget.
			// Settled: the two calibration frames, then the measured spp, or row bands over several frames in a heavy scene.
			bool heavy = _costMs > BUDGET_MS, half = false, measure = false;
			int spp = 1, y0 = 0, y1 = _h;
			if (_reset) half = heavy;
			else if (_calStage == 1) measure = true;
			else if (_calStage == 2) { spp = Math.Min(4, MaxSamples - _samples); measure = true; }
			else if (heavy) { y0 = _band; y1 = Math.Min(_h, _band + BandRows()); measure = ++_sinceCal >= RecalFrames; }
			else { spp = Math.Max(1, Math.Min(_spp, MaxSamples - _samples)); measure = ++_sinceCal >= RecalFrames; }

			Gl.UseProgram(_progRT);
			Gl.Uniform3(_uEye, (float)s._camX, (float)s._camY, (float)s._camZ);
			Gl.Uniform3(_uR, (float)s._Rx, (float)s._Ry, (float)s._Rz);
			Gl.Uniform3(_uU, (float)s._Ux, (float)s._Uy, (float)s._Uz);
			Gl.Uniform3(_uF, (float)s._Fx, (float)s._Fy, (float)s._Fz);
			Gl.Uniform1(_uW, s._w); Gl.Uniform1(_uH, s._h);
			Gl.Uniform1(_uSPP, spp); Gl.Uniform1(_uReset, _reset ? 1 : 0); Gl.Uniform1(_uHalf, half ? 1 : 0);
			Gl.Uniform1(_uTraceY0, y0); Gl.Uniform1(_uTraceY1, y1);
			Gl.Uniform1(_uScale, (float)s._scale);
			Gl.Uniform1(_uTriOff, _rt.TriOff); Gl.Uniform1(_uNumInst, _rt.NumInst); Gl.Uniform1(_uInstOff, _rt.InstOff); Gl.Uniform1(_uTlasOff, _rt.TlasOff);
			Gl.Uniform1(_uNumPrim, _rt.NumPrim); Gl.Uniform1(_uNumMat, _rt.NumMat); Gl.Uniform1(_uNumLight, _rt.NumLight);
			Gl.Uniform1(_uNumRect, _nRect); Gl.Uniform1(_uLightOff, _lightOff); Gl.Uniform1(_uRectOff, _rectOff);
			Gl.Uniform3(_uSkyB, (float)s._skyBR, (float)s._skyBG, (float)s._skyBB);
			Gl.Uniform3(_uSkyT, (float)s._skyTR, (float)s._skyTG, (float)s._skyTB);
			Gl.Uniform1(_uSkyStrength, (float)s._skyStrength);
			Gl.Uniform3(_uSunDir, (float)s._sunX, (float)s._sunY, (float)s._sunZ);
			Gl.Uniform3(_uSunCol, (float)s._sunR, (float)s._sunG, (float)s._sunB);
			Gl.Uniform3(_uAmb, (float)s._ambR, (float)s._ambG, (float)s._ambB);

			Gl.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 1, _geoSsbo);
			Gl.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 2, _topSsbo);
			Gl.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 3, _auxSsbo);
			Gl.ActiveTexture(TextureUnit.Texture0);
			Gl.BindTexture(TextureTarget.Texture2D, _atlasTex);
			Gl.Uniform1(_uAtlas, 0);
			// ping-pong: read the running sum from A (readonly), write the new sum to B (writeonly)
			Gl.BindImageTexture(0, _accumA, 0, false, 0, GLEnum.ReadOnly, GLEnum.Rgba32f);
			Gl.BindImageTexture(1, _accumB, 0, false, 0, GLEnum.WriteOnly, GLEnum.Rgba32f);

			// every pixel is written each frame (untraced ones copy their sum), in strips that each stay short
			if (measure) { Gl.Finish(); _calSw.Restart(); }   // nothing queued: the clock sees only this frame's dispatches
			int rows = StripRows(spp, half);
			uint gx = (uint)((_w + 7) / 8);
			for (int r = 0; r < _h; r += rows) {
				Gl.Uniform1(_uRowOff, r);
				Gl.DispatchCompute(gx, (uint)((Math.Min(rows, _h - r) + 7) / 8), 1);
				if (r + rows < _h) Gl.Flush();   // each strip reaches the GPU on its own
			}
			Gl.MemoryBarrier((uint)(MemoryBarrierMask.ShaderImageAccessBarrierBit | MemoryBarrierMask.TextureFetchBarrierBit));
			if (measure) { Gl.Finish(); Measured(_calSw.Elapsed.TotalMilliseconds, spp, y1 - y0); }

			if (half) { }                                     // no pixel of the skipped three got a sample: no full pass yet
			else if (y0 > 0 || y1 < _h) { if (y1 >= _h) { _samples++; _band = 0; } else _band = y1; }
			else _samples += spp;
			_reset = false;
			(_accumA, _accumB) = (_accumB, _accumA);   // ping-pong: this frame's output becomes next frame's input
		}

		// rows of a band in a heavy scene: about one budget of work, in whole workgroups
		private int BandRows() {
			int rows = (int)(_h * BUDGET_MS / Math.Max(_costMs, 1e-3)) / 8 * 8;
			return Math.Clamp(rows, 8, Math.Max(8, _h));
		}

		// rows per dispatch so that one dispatch is estimated under StripMs (128 while the cost is unknown or belongs to
		// content or a size that has not been measured yet)
		private int StripRows(int spp, bool half) {
			if (_costMs <= 0 || _calStage == 1) return 128;
			double perRow = _costMs * spp * (half ? 0.25 : 1.0) / Math.Max(1, _h);
			int rows = (int)Math.Min(StripMs / Math.Max(perRow, 1e-9), _h + 8.0) / 8 * 8;
			return Math.Max(8, rows);
		}

		// A measured frame (ms of GPU time for spp samples over `rows` traced rows): the two calibration steps (1 spp, then
		// 4 spp, whose difference leaves out the fixed cost of a dispatch), or a periodic re-measurement that is smoothed in.
		private void Measured(double ms, int spp, int rows) {
			if (_calStage == 1) {
				_t1 = ms;
				if (ms > BUDGET_MS) { _costMs = ms; _overheadMs = 0; _calStage = 0; }   // heavy: bands, no 4-spp frame
				else _calStage = 2;
			} else if (_calStage == 2) {
				_costMs = Math.Max(spp > 1 ? (ms - _t1) / (spp - 1) : ms, 0.01);
				_overheadMs = Math.Clamp(_t1 - _costMs, 0, _t1);
				_calStage = 0; _sinceCal = 0;
			} else {
				double est = rows < _h ? ms * _h / Math.Max(rows, 1) : (ms - _overheadMs) / spp;
				est = Math.Max(est, 0.01);
				_costMs = _costMs > 0 ? _costMs * 0.7 + est * 0.3 : est;
				_sinceCal = 0;
			}
			if (_calStage == 2) return;
			_spp = (int)Math.Clamp((BUDGET_MS - _overheadMs) / Math.Max(_costMs, 0.01), 1.0, MAX_SPP);
			Status = _costMs > BUDGET_MS
				? $"GPU compute: {_costMs:F2} ms/spp → 1 spp per {(_h + BandRows() - 1) / BandRows()} frames ({_rt.NumTri} tris, {_rt.NumPrim} prims)"
				: $"GPU compute: {_costMs:F2} ms/spp → {_spp} spp/fr ({_rt.NumTri} tris, {_rt.NumPrim} prims)";
			if (_calLog < 12) { _calLog++; Trace.TraceInformation($"GpuRaytracer calib: 1 spp = {_costMs:F2} ms + {_overheadMs:F2} ms per dispatch @ {_w}x{_h}, {_rt.NumTri} tris / {_rt.NumPrim} prims → {_spp} spp/frame"); }
		}

		// ── present: accum / samples → Reinhard+gamma → canvas texture ───────────────────
		private void Present(Lua3DScene s) {
			EnsureFbo(s);
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
			Gl.Viewport(0, 0, (uint)s._w, (uint)s._h);
			Gl.Disable(EnableCap.DepthTest); Gl.DepthMask(false); Gl.Disable(EnableCap.Blend); Gl.Disable(EnableCap.CullFace);
			Gl.UseProgram(_progPresent); Gl.BindVertexArray(_vaoPresent);
			Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, _accumA);   // the latest sum
			Gl.Uniform1(_pAccum, 0);
			Gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
		}

		// ── GL setup ──────────────────────────────────────────────────────────────────────────
		// Programs are shared by every GpuRaytracer (one per scene): the kernel is large and slow to compile, so each
		// source compiles once per GL context. A handle that is no longer a program (a new context) compiles again.
		private static readonly Dictionary<string, uint> s_progCache = new();
		private uint CachedProgram(string src, string? fs) {
			string key = fs == null ? src : src + "\n" + fs;
			if (s_progCache.TryGetValue(key, out var p) && p != 0 && Gl.IsProgram(p)) return p;
			p = fs == null ? ShaderHelper.CreateComputeProgramFromSource(src) : ShaderHelper.CreateShaderProgramFromSource(src, fs);
			s_progCache[key] = p;
			return p;
		}

		private void EnsureGL() {
			if (_init || _broken) return;
			try {
				_progPresent = CachedProgram(VS_PRESENT, FS_PRESENT);
				_pAccum = Gl.GetUniformLocation(_progPresent, "uAccum");
				_vaoPresent = Gl.GenVertexArray();
				_init = true;
			} catch (Exception e) { Broken(e); }
		}

		// the kernel compiled with only the parts the scene uses (SDF marching, normal maps, texture holes)
		private void UseVariant(int variant) {
			try { _progRT = CachedProgram(BuildComputeSource((variant & 1) != 0, (variant & 2) != 0, (variant & 4) != 0), null); }
			catch (Exception e) { Broken(e); return; }
			_variant = variant;
			int U(string n) => Gl.GetUniformLocation(_progRT, n);
			_uEye = U("uEye"); _uR = U("uR"); _uU = U("uU"); _uF = U("uF");
			_uW = U("uW"); _uH = U("uH"); _uSPP = U("uSPP"); _uReset = U("uReset"); _uHalf = U("uHalf");
			_uRowOff = U("uRowOff"); _uTraceY0 = U("uTraceY0"); _uTraceY1 = U("uTraceY1"); _uScale = U("uScale");
			_uTriOff = U("uTriOff"); _uNumInst = U("uNumInst"); _uInstOff = U("uInstOff"); _uTlasOff = U("uTlasOff");
			_uNumPrim = U("uNumPrim"); _uNumMat = U("uNumMat"); _uNumLight = U("uNumLight"); _uNumRect = U("uNumRect");
			_uLightOff = U("uLightOff"); _uRectOff = U("uRectOff"); _uAtlas = U("uAtlas");
			_uSkyB = U("uSkyB"); _uSkyT = U("uSkyT"); _uSkyStrength = U("uSkyStrength");
			_uSunDir = U("uSunDir"); _uSunCol = U("uSunCol"); _uAmb = U("uAmb");
		}

		private void Broken(Exception e) {
			_broken = true;   // fall back to the CPU path tracer; log the compiler/linker message for diagnosis
			string msg = e.Message ?? "build error";
			if (msg.Length > 120) msg = msg.Substring(0, 120);
			Status = "CPU fallback: " + msg;
			Trace.TraceError("GpuRaytracer compute shader build failed — falling back to the CPU raytracer.\n" + e);
		}

		private unsafe void EnsureAccum(Lua3DScene s) {
			if (_accumA != 0 && _aw == s._w && _ah == s._h) return;
			if (_accumA != 0) { Gl.DeleteTexture(_accumA); _accumA = 0; }
			if (_accumB != 0) { Gl.DeleteTexture(_accumB); _accumB = 0; }
			_accumA = MakeAccumTex(s._w, s._h);
			_accumB = MakeAccumTex(s._w, s._h);
			_aw = s._w; _ah = s._h;
			_samples = 0; _band = 0; _reset = true;   // size changed → restart accumulation
		}
		private uint MakeAccumTex(int w, int h) {
			uint t = Gl.GenTexture();
			Gl.BindTexture(TextureTarget.Texture2D, t);
			Gl.TexStorage2D(TextureTarget.Texture2D, 1, SizedInternalFormat.Rgba32f, (uint)w, (uint)h);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
			return t;
		}

		private void EnsureFbo(Lua3DScene s) {
			uint colorTex = s._canvas.Pointer;
			if (_fbo != 0 && _fboW == s._w && _fboH == s._h && _fboColorTex == colorTex) return;
			if (_fbo == 0) _fbo = Gl.GenFramebuffer();
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
			Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, colorTex, 0);
			_fboW = s._w; _fboH = s._h; _fboColorTex = colorTex;
		}

		// ── snapshot → SSBOs + texture atlas (only what changed) ────────────────────────────
		private void UploadScene() {
			var rt = _rt;
			if (rt.GeoFull || _geoSsbo == 0) Upload(ref _geoSsbo, ref _geoCap, rt.Geo, rt.GeoLen);
			else foreach (var (off, len) in rt.GeoRanges) SubUpload(_geoSsbo, rt.Geo, off, len);   // objects rebuilt at the same size
			if (rt.TopChanged || _topSsbo == 0) Upload(ref _topSsbo, ref _topCap, rt.Top, rt.TopLen);
			bool aux = rt.MatsChanged || rt.LightsChanged || rt.SlotInfoChanged || _auxSsbo == 0;
			if (rt.TexLayoutChanged || _atlasTex == 0) { BuildAtlas(); aux = true; }
			else foreach (int k in rt.TexDirty) UpdateAtlasSlot(k);   // new pixels at the same size: just that rect
			if (aux) {
				int nm = rt.NumMat * RtScene.MTL, nl = rt.NumLight * RtScene.LGT, nr = rt.SlotCount * RECT;
				_lightOff = nm; _rectOff = nm + nl; _nRect = rt.SlotCount;
				int len = _rectOff + nr;
				if (_aux.Length < Math.Max(1, len)) _aux = new float[Math.Max(1, len)];
				Array.Copy(rt.Mats, 0, _aux, 0, nm);
				Array.Copy(rt.Lights, 0, _aux, _lightOff, nl);
				for (int k = 0; k < rt.SlotCount; k++) {
					int b = _rectOff + k * RECT;
					for (int c = 0; c < 4; c++) _aux[b + c] = k * 5 + c < _rect.Length ? _rect[k * 5 + c] : 0;
					_aux[b + 4] = rt.SlotCut(k); _aux[b + 5] = rt.SlotNormal(k);
				}
				Upload(ref _auxSsbo, ref _auxCap, _aux, len);
			}
			rt.ClearChanges();
		}

		// the first len floats of data into the buffer, which is reallocated only to grow (or to shrink a lot)
		private unsafe void Upload(ref uint ssbo, ref int cap, float[] data, int len) {
			if (ssbo == 0) { ssbo = Gl.GenBuffer(); cap = 0; }
			Gl.BindBuffer(BufferTargetARB.ShaderStorageBuffer, ssbo);
			if (len > cap || cap == 0 || cap > 4 * len + 65536) {
				cap = Math.Max(len + len / 4, 64);
				Gl.BufferData(BufferTargetARB.ShaderStorageBuffer, (nuint)(cap * sizeof(float)), null, BufferUsageARB.DynamicDraw);
			}
			if (len > 0) Gl.BufferSubData<float>(BufferTargetARB.ShaderStorageBuffer, 0, new ReadOnlySpan<float>(data, 0, len));
		}
		private void SubUpload(uint ssbo, float[] data, int off, int len) {
			if (len <= 0) return;
			Gl.BindBuffer(BufferTargetARB.ShaderStorageBuffer, ssbo);
			Gl.BufferSubData<float>(BufferTargetARB.ShaderStorageBuffer, (nint)off * sizeof(float), new ReadOnlySpan<float>(data, off, len));
		}

		// Pack the used, registered textures into one RGBA8 atlas no larger than GL_MAX_TEXTURE_SIZE (skyline packer);
		// when they don't fit, the largest are halved until they do (traced once). The shader samples by texelFetch with
		// manual repeat wrap through each slot's (x, y, w, h) rect.
		private unsafe void BuildAtlas() {
			int n = _rt.SlotCount;
			if (_rect.Length < n * 5) _rect = new int[n * 5 + 40];
			Array.Clear(_rect);
			var slots = new List<int>();
			for (int k = 0; k < n; k++) if (_rt.SlotPixels(k, out _, out _) != null) slots.Add(k);
			int maxSize = Gl.GetInteger(GLEnum.MaxTextureSize);
			maxSize = Math.Min(maxSize >= 2048 ? maxSize : 2048, 8192);
			int m = slots.Count;
			var pw = new int[m]; var ph = new int[m]; var shift = new int[m]; var px = new int[m]; var py = new int[m];
			int aw = 1, ah = 1; bool fits = m == 0, shrunk = false;
			for (int iter = 0; iter < 64 && !fits; iter++) {
				int big = 0;
				for (int i = 0; i < m; i++) {
					_rt.SlotPixels(slots[i], out int w, out int h);
					pw[i] = Math.Max(1, w >> shift[i]); ph[i] = Math.Max(1, h >> shift[i]);
					big = Math.Max(big, Math.Max(pw[i], ph[i]));
				}
				fits = Pack(pw, ph, m, maxSize, px, py, out aw, out ah);
				if (fits || big <= 1) break;
				for (int i = 0; i < m; i++) if (Math.Max(pw[i], ph[i]) == big) shift[i]++;   // halve the largest
				shrunk = true;
			}
			if (!fits) {
				m = 0; aw = ah = 1;
				Trace.TraceError($"GpuRaytracer: {slots.Count} textures do not fit a {maxSize}px atlas; the path tracer draws them untextured.");
			} else if (shrunk && !_atlasWarned) {
				_atlasWarned = true;
				Trace.TraceWarning($"GpuRaytracer: the textures do not fit a {maxSize}px atlas at full size; the largest are sampled at a reduced size.");
			}
			var pix = new byte[aw * ah * 4];
			for (int i = 0; i < m; i++) {
				int k = slots[i], b = k * 5;
				_rect[b] = px[i]; _rect[b + 1] = py[i]; _rect[b + 2] = pw[i]; _rect[b + 3] = ph[i]; _rect[b + 4] = shift[i];
				byte[] src = SlotBytes(k, shift[i], pw[i], ph[i]);
				for (int y = 0; y < ph[i]; y++) System.Buffer.BlockCopy(src, y * pw[i] * 4, pix, ((py[i] + y) * aw + px[i]) * 4, pw[i] * 4);
			}
			if (_atlasTex == 0) _atlasTex = Gl.GenTexture();
			Gl.BindTexture(TextureTarget.Texture2D, _atlasTex);
			for (int i = 0; i < 8 && Gl.GetError() != GLEnum.NoError; i++) { }   // earlier errors are not this upload's
			fixed (byte* dp = pix)
				Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8, (uint)aw, (uint)ah, 0, PixelFormat.Rgba, PixelType.UnsignedByte, dp);
			var err = Gl.GetError();
			if (err != GLEnum.NoError) Trace.TraceError($"GpuRaytracer: the {aw}x{ah} texture atlas failed to upload ({err}).");
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
		}

		// new pixels of an already packed slot (same size), uploaded into its rect only
		private unsafe void UpdateAtlasSlot(int k) {
			int b = k * 5;
			if (_atlasTex == 0 || b + 4 >= _rect.Length || _rect[b + 2] == 0) return;
			byte[] src = SlotBytes(k, _rect[b + 4], _rect[b + 2], _rect[b + 3]);
			Gl.BindTexture(TextureTarget.Texture2D, _atlasTex);
			fixed (byte* dp = src)
				Gl.TexSubImage2D(TextureTarget.Texture2D, 0, _rect[b], _rect[b + 1], (uint)_rect[b + 2], (uint)_rect[b + 3], PixelFormat.Rgba, PixelType.UnsignedByte, dp);
		}

		// a slot's RGBA bytes at its packed size: each texel the average of its 2^shift square, the colour weighted by
		// alpha (a texture's negative texels have alpha 0, a sprite keeps its own)
		private byte[] SlotBytes(int k, int shift, int pw, int ph) {
			var src = _rt.SlotPixels(k, out int w, out int h)!;
			bool sprite = _rt.SlotIsSprite(k);
			var o = new byte[pw * ph * 4];
			int s = 1 << shift;
			for (int y = 0; y < ph; y++) {
				for (int x = 0; x < pw; x++) {
					long r = 0, g = 0, bl = 0, ar = 0, ag = 0, ab = 0, asum = 0; int c = 0;
					for (int yy = y * s; yy < Math.Min(h, y * s + s); yy++)
						for (int xx = x * s; xx < Math.Min(w, x * s + s); xx++) {
							int p = src[yy * w + xx];
							int pr = (p >> 16) & 255, pg = (p >> 8) & 255, pb = p & 255, a = sprite ? (p >> 24) & 255 : (p < 0 ? 0 : 255);
							r += pr; g += pg; bl += pb; ar += pr * a; ag += pg * a; ab += pb * a; asum += a; c++;
						}
					c = Math.Max(c, 1);
					int q = (y * pw + x) * 4;
					if (asum > 0) { o[q] = (byte)(ar / asum); o[q + 1] = (byte)(ag / asum); o[q + 2] = (byte)(ab / asum); }
					else { o[q] = (byte)(r / c); o[q + 1] = (byte)(g / c); o[q + 2] = (byte)(bl / c); }
					o[q + 3] = (byte)(asum / c);
				}
			}
			return o;
		}

		// Skyline bottom-left packing of m rects into the narrowest power-of-two width (up to maxSize) whose height stays
		// within maxSize. Positions go to px, py; false when no width works.
		internal static bool Pack(int[] pw, int[] ph, int m, int maxSize, int[] px, int[] py, out int aw, out int ah) {
			aw = ah = 1;
			if (m == 0) return true;
			long area = 0; int widest = 1;
			for (int i = 0; i < m; i++) { area += (long)pw[i] * ph[i]; widest = Math.Max(widest, pw[i]); }
			if (widest > maxSize) return false;
			var idx = new int[m];
			for (int i = 0; i < m; i++) idx[i] = i;
			Array.Sort(idx, (a, b) => ph[b] != ph[a] ? ph[b].CompareTo(ph[a]) : pw[b].CompareTo(pw[a]));   // tallest first
			int w0 = 1;
			while (w0 < widest || (long)w0 * w0 < area) w0 *= 2;
			for (int W = Math.Min(w0, maxSize); W <= maxSize; W *= 2) {
				int h = Skyline(idx, pw, ph, m, W, maxSize, px, py);
				if (h <= maxSize) { aw = W; ah = h; return true; }
				if (W == maxSize) break;
			}
			return false;
		}

		// one skyline pass at width W: each rect goes where its top ends lowest (then leftmost); returns the height
		// used, or int.MaxValue past maxH
		private static int Skyline(int[] idx, int[] pw, int[] ph, int m, int W, int maxH, int[] px, int[] py) {
			var sx = new List<int> { 0 }; var sy = new List<int> { 0 }; var sw = new List<int> { W };
			int height = 0;
			for (int ii = 0; ii < m; ii++) {
				int i = idx[ii], w = pw[i], h = ph[i];
				int bestY = int.MaxValue, bestX = 0, bestSeg = -1;
				for (int s = 0; s < sx.Count; s++) {
					if (sx[s] + w > W) break;
					int y = 0, span = 0;
					for (int k = s; span < w; k++) { y = Math.Max(y, sy[k]); span += sw[k]; }
					if (y < bestY) { bestY = y; bestX = sx[s]; bestSeg = s; }
				}
				if (bestSeg < 0 || bestY + h > maxH) return int.MaxValue;
				px[i] = bestX; py[i] = bestY; height = Math.Max(height, bestY + h);
				// the rect's top becomes a segment; the segments under it shrink or go
				int end = bestX + w;
				sx.Insert(bestSeg, bestX); sy.Insert(bestSeg, bestY + h); sw.Insert(bestSeg, w);
				int j = bestSeg + 1;
				while (j < sx.Count && sx[j] < end) {
					int segEnd = sx[j] + sw[j];
					if (segEnd <= end) { sx.RemoveAt(j); sy.RemoveAt(j); sw.RemoveAt(j); }
					else { sw[j] = segEnd - end; sx[j] = end; break; }
				}
				for (int k = sx.Count - 1; k > 0; k--)   // merge neighbours at the same height
					if (sy[k] == sy[k - 1]) { sw[k - 1] += sw[k]; sx.RemoveAt(k); sy.RemoveAt(k); sw.RemoveAt(k); }
			}
			return height;
		}

		// ── present (fullscreen triangle) ───────────────────────────────────────────────────────
		private static string VS_PRESENT => ShaderSources.Get("rt_present.vert");
		private static string FS_PRESENT => ShaderSources.Get("rt_present.frag");

		/// <summary>The kernel with every optional part compiled in.</summary>
		internal static string BuildComputeSource() => BuildComputeSource(true, true, true);
		/// <summary>The kernel without SDF marching, normal maps and texture holes.</summary>
		internal static string BuildComputeSourceLean() => BuildComputeSource(false, false, false);

		// The kernel (Shaders/raytrace.comp) with #defines after its #version line: `sdf` compiles in the SDF marching,
		// `nmaps` the procedural and texture normal maps, `cut` the alpha test of textures and sprites. The kernel's
		// layout constants follow RtScene and RECT, and its GEM table Raytracer.GemFacets (ShaderSourcesTests checks both).
		private static string BuildComputeSource(bool sdf, bool nmaps, bool cut) {
			string defs = (sdf ? "#define HAS_SDF\n" : "") + (nmaps ? "#define HAS_NMAP\n" : "") + (cut ? "#define HAS_CUT\n" : "");
			string kernel = ShaderSources.Get("raytrace.comp");
			int at = kernel.IndexOf('\n') + 1;   // after the #version line
			return kernel.Substring(0, at) + defs + kernel.Substring(at);
		}
	}
}
