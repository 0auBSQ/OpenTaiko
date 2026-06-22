using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using FDK;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	/// <summary>
	/// GPU progressive path tracer for <see cref="Lua3DScene"/> on a GLES 3.1 compute shader — the GPU
	/// counterpart of the CPU <see cref="Raytracer"/>. It mirrors that integrator exactly (Möller–Trumbore
	/// triangles + analytic sphere/plane/box + ray-marched SDF presets, diffuse/metal/glass/emissive
	/// materials, procedural + texture normal maps, next-event point lights, a sky gradient, Russian
	/// roulette, MaxDepth 8) but runs one sample-per-pixel per dispatch across the whole image in parallel
	/// instead of 8 CPU row-bands — the massive parallelism is the speedup.
	///
	/// Scene data is snapshotted into SSBOs (triangles / primitives / materials / lights) and a texture
	/// atlas, rebuilt only when <see cref="Lua3DScene.Revision"/> changes (camera/geometry/material/light
	/// edits). Radiance accumulates into a persistent rgba32f image (imageLoad+add+imageStore — each thread
	/// touches only its own texel, so no races); a fullscreen present pass divides by the sample count,
	/// Reinhard-tonemaps + gamma-encodes, and writes straight into the scene's canvas texture (so
	/// <see cref="Lua3DScene.Upload"/> skips the CPU buffer, as with the GPU rasterizer).
	/// </summary>
	internal sealed class GpuRaytracer : IRenderer {
		private GL Gl => Game.Gl;

		// flat-float SSBO strides (must match the GLSL `const int` values)
		private const int TRI = 23;   // A(3) B(3) C(3) uv0..2(6) fall(3) shade(1) mat(1) tex(1) tw(1) th(1)
		private const int PRM = 15;   // kind(1) c(3) abc(3) min(3) max(3) preset(1) material(1)
		private const int MTL = 12;   // type(1) rgb(3) rough(1) ior(1) emis(3) tex(1) nmap(1) ntex(1)
		private const int LGT = 6;    // pos(3) col(3)
		private const int MAXD = 8;
		// Adaptive samples-per-frame: the CPU could only afford 1 spp/frame, so the GPU doing 1/frame looked
		// identical at the same framerate; but a fixed batch (e.g. 16) tanks the framerate on the heavy SDF
		// scenes. Instead we MEASURE the GPU cost of 1 spp the moment the view settles (one glFinish) and pick
		// the spp that fits a frame-time budget — so simple scenes converge fast and heavy scenes stay smooth.
		private const double BUDGET_MS = 9.0;   // GPU time/frame to aim for when settled (leaves room for present + the rest of the frame)
		private const int MAX_SPP = 256;

		private bool _init;
		private uint _progRT, _progPresent, _vaoPresent;
		// compute uniforms
		private int _uEye, _uR, _uU, _uF, _uW, _uH, _uBase, _uSPP, _uScale;
		private int _uNumTri, _uNumPrim, _uNumMat, _uNumLight, _uNumRect, _uLightOff, _uRectOff, _uAtlas;
		private int _uSkyB, _uSkyT, _uSkyStrength;
		// present uniforms
		private int _pAccum, _pInv;

		// Two rgba32f accumulation images, ping-ponged each frame: GLES forbids a rgba32f image being
		// read AND written in one shader (only r32f/r32i/r32ui may be readwrite), so we read the previous
		// sum from one (readonly) and write the new sum to the other (writeonly), then swap.
		private uint _accumA, _accumB; private int _aw, _ah;
		private uint _fbo; private int _fboW, _fboH; private uint _fboColorTex;

		private uint _triSsbo, _primSsbo, _auxSsbo;   // aux = [materials][lights][rects] packed (≤4 SSBOs total)
		private int _nTri, _nPrim, _nMat, _nLight, _nRect, _lightOff, _rectOff;   // offsets are in floats into aux
		private uint _atlasTex; private int _atlasW, _atlasH;

		private int _lastRevision = -1, _w, _h, _samples;
		private long _contentSig = long.MinValue;   // when this changes, the SSBOs/atlas are rebuilt (NOT on camera-only moves)

		// adaptive spp state
		private readonly System.Diagnostics.Stopwatch _calSw = new();
		private bool _calibrated; private int _spp = 1; private int _calLog;

		// If the compute shader can't compile/link on this driver, fall back to the CPU path tracer so the
		// stage still renders (no crash, no blank screen). Tracks the failure so we don't retry every frame.
		private bool _broken;
		private Raytracer? _cpuFallback;

		/// <summary>Human-readable backend state for an on-screen readout: "GPU compute …" or "CPU fallback: …".</summary>
		public string Status { get; private set; } = "GPU compute (initialising)";

		/// <summary>Samples-per-pixel accumulated since the last reset (drives the convergence readout).</summary>
		public int SampleCount => _broken ? (_cpuFallback?.SampleCount ?? 0) : _samples;

		public void Invalidate() {
			_lastRevision = -1; _samples = 0; _contentSig = long.MinValue;
			_cpuFallback?.Invalidate();
			FreeFrameTargets();
			if (_triSsbo != 0) { Gl.DeleteBuffer(_triSsbo); _triSsbo = 0; }
			if (_primSsbo != 0) { Gl.DeleteBuffer(_primSsbo); _primSsbo = 0; }
			if (_auxSsbo != 0) { Gl.DeleteBuffer(_auxSsbo); _auxSsbo = 0; }
			if (_atlasTex != 0) { Gl.DeleteTexture(_atlasTex); _atlasTex = 0; }
		}
		private void FreeFrameTargets() {
			if (_accumA != 0) { Gl.DeleteTexture(_accumA); _accumA = 0; }
			if (_accumB != 0) { Gl.DeleteTexture(_accumB); _accumB = 0; }
			if (_fbo != 0) { Gl.DeleteFramebuffer(_fbo); _fbo = 0; }
			_aw = _ah = 0; _fboW = _fboH = 0; _fboColorTex = 0;
		}

		public void Dispose() {
			Invalidate();
			if (_init) {
				if (_vaoPresent != 0) Gl.DeleteVertexArray(_vaoPresent);
				if (_progRT != 0) Gl.DeleteProgram(_progRT);
				if (_progPresent != 0) Gl.DeleteProgram(_progPresent);
				_vaoPresent = _progRT = _progPresent = 0;
				_init = false;
			}
		}

		private bool _logged;
		public unsafe void Render(Lua3DScene s) {
			if (!_logged) { _logged = true; Trace.TraceInformation("GpuRaytracer ACTIVE (GLES 3.1 compute path tracer)"); }
			EnsureGL();
			if (_broken) { s._gpuOwnsCanvas = false; (_cpuFallback ??= new Raytracer()).Render(s); return; }   // driver couldn't build the kernel → CPU path tracer fills the canvas buffer

			bool resize = _w != s._w || _h != s._h;
			if (resize) { _w = s._w; _h = s._h; }
			EnsureAccum(s);   // recreates the accum image on resize (and resets _samples)
			// Rebuild the SSBOs / atlas ONLY when scene content changes — NOT on a camera move (which bumps
			// Revision but leaves geometry/materials/lights/textures untouched). This keeps moving the camera
			// cheap (no per-frame GL re-uploads); accumulation still resets on any Revision change below.
			long csig = ContentSig(s);
			if (csig != _contentSig) { Rebuild(s); _contentSig = csig; _calibrated = false; }   // scene changed → re-measure cost
			bool reset = resize || s.Revision != _lastRevision;
			if (reset) { _lastRevision = s.Revision; _samples = 0; _calibrated = false; }   // camera moved → reset accumulation + re-measure on settle
			// While moving: 1 spp (responsive, and the frame is discarded next reset anyway).
			// First settled frame: 1 spp + a glFinish to MEASURE the per-sample GPU cost, then derive _spp.
			// Subsequent settled frames: the measured _spp (fills the budget → fast convergence, stays smooth).
			bool measure = !reset && !_calibrated;
			int spp = (reset || measure) ? 1 : _spp;
			int baseSample = _samples;

			// capture + bind own targets
			Span<int> vp = stackalloc int[4];
			Gl.GetInteger(GLEnum.Viewport, vp);
			uint prevFbo = (uint)Gl.GetInteger(GLEnum.FramebufferBinding);

			// ── path-trace one sample into the accumulation image ────────────────────────────────
			if (measure) _calSw.Restart();
			Gl.UseProgram(_progRT);
			Gl.Uniform3(_uEye, (float)s._camX, (float)s._camY, (float)s._camZ);
			Gl.Uniform3(_uR, (float)s._Rx, (float)s._Ry, (float)s._Rz);
			Gl.Uniform3(_uU, (float)s._Ux, (float)s._Uy, (float)s._Uz);
			Gl.Uniform3(_uF, (float)s._Fx, (float)s._Fy, (float)s._Fz);
			Gl.Uniform1(_uW, s._w); Gl.Uniform1(_uH, s._h);
			Gl.Uniform1(_uBase, baseSample); Gl.Uniform1(_uSPP, spp);
			Gl.Uniform1(_uScale, (float)s._scale);
			Gl.Uniform1(_uNumTri, _nTri); Gl.Uniform1(_uNumPrim, _nPrim);
			Gl.Uniform1(_uNumMat, _nMat); Gl.Uniform1(_uNumLight, _nLight); Gl.Uniform1(_uNumRect, _nRect);
			Gl.Uniform1(_uLightOff, _lightOff); Gl.Uniform1(_uRectOff, _rectOff);
			Gl.Uniform3(_uSkyB, (float)s._skyBR, (float)s._skyBG, (float)s._skyBB);
			Gl.Uniform3(_uSkyT, (float)s._skyTR, (float)s._skyTG, (float)s._skyTB);
			Gl.Uniform1(_uSkyStrength, (float)s._skyStrength);

			Gl.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 1, _triSsbo);
			Gl.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 2, _primSsbo);
			Gl.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 3, _auxSsbo);
			Gl.ActiveTexture(TextureUnit.Texture0);
			Gl.BindTexture(TextureTarget.Texture2D, _atlasTex);
			Gl.Uniform1(_uAtlas, 0);
			// ping-pong: read the running sum from A (readonly), write the new sum to B (writeonly)
			uint outImg = _accumB;
			Gl.BindImageTexture(0, _accumA, 0, false, 0, GLEnum.ReadOnly, GLEnum.Rgba32f);
			Gl.BindImageTexture(1, outImg, 0, false, 0, GLEnum.WriteOnly, GLEnum.Rgba32f);

			uint gx = (uint)((s._w + 7) / 8), gy = (uint)((s._h + 7) / 8);
			Gl.DispatchCompute(gx, gy, 1);
			Gl.MemoryBarrier((uint)(MemoryBarrierMask.ShaderImageAccessBarrierBit | MemoryBarrierMask.TextureFetchBarrierBit));
			_samples += spp;   // total accumulated samples now in the image

			// ── present: accum/samples → Reinhard+gamma → canvas texture ─────────────────────────
			EnsureFbo(s);
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
			Gl.Viewport(0, 0, (uint)s._w, (uint)s._h);
			Gl.Disable(EnableCap.DepthTest); Gl.DepthMask(false); Gl.Disable(EnableCap.Blend); Gl.Disable(EnableCap.CullFace);
			Gl.UseProgram(_progPresent); Gl.BindVertexArray(_vaoPresent);
			Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, outImg);   // present the freshly-written sum
			Gl.Uniform1(_pAccum, 0); Gl.Uniform1(_pInv, 1.0f / _samples);
			Gl.DrawArrays(PrimitiveType.Triangles, 0, 3);

			// Calibrate once when the view settles: glFinish to get the TRUE GPU time of this 1-spp frame
			// (independent of vsync), then pick the spp that fits the budget. One small hitch per stop.
			if (measure) {
				Gl.Finish();
				double ms = _calSw.Elapsed.TotalMilliseconds;
				_spp = (int)Math.Clamp(BUDGET_MS / Math.Max(ms, 0.05), 1.0, MAX_SPP);
				_calibrated = true;
				Status = $"GPU compute: {ms:F2} ms/spp → {_spp} spp/fr ({_nTri} tris, {_nPrim} prims)";
				if (_calLog < 12) { _calLog++; Trace.TraceInformation($"GpuRaytracer calib: 1 spp = {ms:F2} ms @ {s._w}x{s._h}, {_nTri} tris / {_nPrim} prims → {_spp} spp/frame"); }
			}

			// restore GL state for the 2D / ImGui pipeline (same contract as the GPU rasterizer)
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, prevFbo);
			Gl.Viewport(vp[0], vp[1], (uint)vp[2], (uint)vp[3]);
			Gl.UseProgram(0); Gl.BindVertexArray(0); Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
			Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, 0);
			Gl.Disable(EnableCap.DepthTest); Gl.DepthMask(true); Gl.Disable(EnableCap.CullFace);
			Gl.Enable(EnableCap.Blend); BlendHelper.SetBlend(BlendType.Normal);
			s._gpuOwnsCanvas = true;   // Upload() will skip the (stale) CPU buffer
			(_accumA, _accumB) = (_accumB, _accumA);   // ping-pong: this frame's output becomes next frame's input
		}

		// ── GL setup ──────────────────────────────────────────────────────────────────────────
		private void EnsureGL() {
			if (_init || _broken) return;
			try {
				_progRT = ShaderHelper.CreateComputeProgramFromSource(BuildComputeSource());
				int U(string n) => Gl.GetUniformLocation(_progRT, n);
				_uEye = U("uEye"); _uR = U("uR"); _uU = U("uU"); _uF = U("uF");
				_uW = U("uW"); _uH = U("uH"); _uBase = U("uBase"); _uSPP = U("uSPP"); _uScale = U("uScale");
				_uNumTri = U("uNumTri"); _uNumPrim = U("uNumPrim"); _uNumMat = U("uNumMat");
				_uNumLight = U("uNumLight"); _uNumRect = U("uNumRect"); _uLightOff = U("uLightOff"); _uRectOff = U("uRectOff"); _uAtlas = U("uAtlas");
				_uSkyB = U("uSkyB"); _uSkyT = U("uSkyT"); _uSkyStrength = U("uSkyStrength");

				_progPresent = ShaderHelper.CreateShaderProgramFromSource(VS_PRESENT, FS_PRESENT);
				_pAccum = Gl.GetUniformLocation(_progPresent, "uAccum");
				_pInv = Gl.GetUniformLocation(_progPresent, "uInv");
				_vaoPresent = Gl.GenVertexArray();
				_init = true;
			} catch (Exception e) {
				_broken = true;   // fall back to the CPU path tracer; log the compiler/linker message for diagnosis
				string msg = e.Message ?? "build error";
				if (msg.Length > 120) msg = msg.Substring(0, 120);
				Status = "CPU fallback: " + msg;
				Trace.TraceError("GpuRaytracer compute shader build failed — falling back to the CPU raytracer.\n" + e);
			}
		}

		// Hash the scene's RAYTRACED content (geometry meta, primitives, materials, lights, texture set) so
		// the GL buffers are rebuilt only when that changes. A camera move bumps Revision (→ accumulation
		// reset) but does NOT change this signature, so it doesn't trigger a (wasteful) per-frame re-upload.
		private static long ContentSig(Lua3DScene s) {
			unchecked {
				long h = 1469598103934665603L;
				void M(long v) { h = (h ^ v) * 1099511628211L; }
				void MD(double v) => M(BitConverter.DoubleToInt64Bits(v));
				foreach (var kv in s._objects) {
					var o = kv.Value;
					M(kv.Key); M(o.GeomVersion); M(o.N); M(o.Kind); M(o.Material); M(o.Visible ? 1 : 0);
					if (o.Transform != null) for (int i = 0; i < 12; i++) MD(o.Transform[i]);
				}
				var pr = s._primitives; M(pr.Count);
				for (int i = 0; i < pr.Count; i++) {
					var p = pr[i]; M(p.Kind); M(p.SdfPreset); M(p.Material);
					MD(p.X); MD(p.Y); MD(p.Z); MD(p.A); MD(p.B); MD(p.C);
					MD(p.MinX); MD(p.MinY); MD(p.MinZ); MD(p.MaxX); MD(p.MaxY); MD(p.MaxZ);
				}
				var mt = s._materials; M(mt.Count);
				for (int i = 0; i < mt.Count; i++) {
					var m = mt[i]; M(m.Type); M(m.TexId); M(m.NormalMap); M(m.NormalTex);
					MD(m.R); MD(m.G); MD(m.B); MD(m.Rough); MD(m.Ior); MD(m.ER); MD(m.EG); MD(m.EB);
				}
				var lt = s._lights; M(lt.Count);
				for (int i = 0; i < lt.Count; i++) { var l = lt[i]; MD(l.X); MD(l.Y); MD(l.Z); MD(l.R); MD(l.G); MD(l.B); }
				M(s._texPix.Count);
				foreach (var id in s._texPix.Keys) { M(id); M(s._texW[id]); M(s._texH[id]); }
				return h;
			}
		}

		private unsafe void EnsureAccum(Lua3DScene s) {
			if (_accumA != 0 && _aw == s._w && _ah == s._h) return;
			if (_accumA != 0) { Gl.DeleteTexture(_accumA); _accumA = 0; }
			if (_accumB != 0) { Gl.DeleteTexture(_accumB); _accumB = 0; }
			_accumA = MakeAccumTex(s._w, s._h);
			_accumB = MakeAccumTex(s._w, s._h);
			_aw = s._w; _ah = s._h;
			_samples = 0;   // size changed → restart accumulation
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

		// ── scene snapshot → SSBOs + texture atlas (only on Revision change) ────────────────────
		private readonly List<float> _triBuf = new(4096);

		private static double Srgb(double c) => Math.Pow(c < 0 ? 0 : c, 2.2);

		private static (double x, double y, double z) Xf(double[]? t, double x, double y, double z) {
			if (t == null) return (x, y, z);
			return (t[0] * x + t[1] * y + t[2] * z + t[3],
					t[4] * x + t[5] * y + t[6] * z + t[7],
					t[8] * x + t[9] * y + t[10] * z + t[11]);
		}

		private void PushTri(Lua3DScene s,
			(double x, double y, double z) a, (double x, double y, double z) b, (double x, double y, double z) c,
			double u0, double v0, double u1, double v1, double u2, double v2,
			int mat, int texId, double shade, double fr255, double fg255, double fb255) {
			var L = _triBuf;
			L.Add((float)a.x); L.Add((float)a.y); L.Add((float)a.z);
			L.Add((float)b.x); L.Add((float)b.y); L.Add((float)b.z);
			L.Add((float)c.x); L.Add((float)c.y); L.Add((float)c.z);
			L.Add((float)u0); L.Add((float)v0); L.Add((float)u1); L.Add((float)v1); L.Add((float)u2); L.Add((float)v2);
			L.Add((float)Srgb(fr255 / 255.0)); L.Add((float)Srgb(fg255 / 255.0)); L.Add((float)Srgb(fb255 / 255.0));
			L.Add((float)shade); L.Add(mat); L.Add(texId);
			int tw = texId >= 0 && s._texW.TryGetValue(texId, out var w) ? w : 0;
			int th = texId >= 0 && s._texH.TryGetValue(texId, out var hh) ? hh : 0;
			L.Add(tw); L.Add(th);
		}

		private unsafe void Rebuild(Lua3DScene s) {
			// ── triangles (world-space), mirroring Raytracer.Rebuild ──
			_triBuf.Clear();
			foreach (var o in s._objects.Values) {
				if (!o.Visible || o.N == 0) continue;
				var d = o.D; var t = o.Transform;
				if (o.Kind == 0) {
					for (int i = 0; i < o.N; i++) {
						int k = i * 16; int texId = (int)d[k + 12];
						bool hasTex = s._texPix.ContainsKey(texId);
						double uMax = d[k + 13], vMax = d[k + 14], shade = d[k + 15];
						var p0 = Xf(t, d[k], d[k + 1], d[k + 2]);
						var p1 = Xf(t, d[k + 3], d[k + 4], d[k + 5]);
						var p2 = Xf(t, d[k + 6], d[k + 7], d[k + 8]);
						var p3 = Xf(t, d[k + 9], d[k + 10], d[k + 11]);
						int tex = hasTex ? texId : -1;
						PushTri(s, p0, p1, p2, 0, vMax, uMax, vMax, uMax, 0, o.Material, tex, shade, o.R, o.G, o.B);
						PushTri(s, p0, p2, p3, 0, vMax, uMax, 0, 0, 0, o.Material, tex, shade, o.R, o.G, o.B);
					}
				} else if (o.Kind == 1) {
					for (int i = 0; i < o.N; i++) {
						int k = i * 12;
						var p0 = Xf(t, d[k], d[k + 1], d[k + 2]);
						var p1 = Xf(t, d[k + 3], d[k + 4], d[k + 5]);
						var p2 = Xf(t, d[k + 6], d[k + 7], d[k + 8]);
						var p3 = Xf(t, d[k + 9], d[k + 10], d[k + 11]);
						PushTri(s, p0, p1, p2, 0, 0, 0, 0, 0, 0, o.Material, -1, 1, o.R, o.G, o.B);
						PushTri(s, p0, p2, p3, 0, 0, 0, 0, 0, 0, o.Material, -1, 1, o.R, o.G, o.B);
					}
				} else if (o.Kind == 2) {
					for (int i = 0; i < o.N; i++) {
						int k = i * 17; int texId = (int)d[k + 15];
						bool hasTex = s._texPix.ContainsKey(texId);
						double shade = d[k + 16];
						var p0 = Xf(t, d[k], d[k + 1], d[k + 2]);
						var p1 = Xf(t, d[k + 5], d[k + 6], d[k + 7]);
						var p2 = Xf(t, d[k + 10], d[k + 11], d[k + 12]);
						PushTri(s, p0, p1, p2, d[k + 3], d[k + 4], d[k + 8], d[k + 9], d[k + 13], d[k + 14],
							o.Material, hasTex ? texId : -1, shade, o.R, o.G, o.B);
					}
				}
			}
			_nTri = _triBuf.Count / TRI;
			UploadSsbo(ref _triSsbo, _triBuf, TRI);

			// ── primitives ──
			var prims = s._primitives; _nPrim = prims.Count;
			var pb = new float[Math.Max(1, _nPrim) * PRM];
			for (int i = 0; i < _nPrim; i++) {
				var pr = prims[i]; int b = i * PRM;
				pb[b] = pr.Kind; pb[b + 1] = (float)pr.X; pb[b + 2] = (float)pr.Y; pb[b + 3] = (float)pr.Z;
				pb[b + 4] = (float)pr.A; pb[b + 5] = (float)pr.B; pb[b + 6] = (float)pr.C;
				pb[b + 7] = (float)pr.MinX; pb[b + 8] = (float)pr.MinY; pb[b + 9] = (float)pr.MinZ;
				pb[b + 10] = (float)pr.MaxX; pb[b + 11] = (float)pr.MaxY; pb[b + 12] = (float)pr.MaxZ;
				pb[b + 13] = pr.SdfPreset; pb[b + 14] = pr.Material;
			}
			UploadSsboArr(ref _primSsbo, pb);

			// ── materials, lights, texture rects → ONE packed "aux" SSBO ──────────────────────────────
			// (3 SSBOs total — tris, prims, aux — keeps us within the GLES 3.1 guaranteed minimum of 4
			// compute storage blocks; 5 separate SSBOs silently failed to link on ANGLE → CPU fallback.)
			var mats = s._materials; _nMat = mats.Count;
			var mb = new float[_nMat * MTL];
			for (int i = 0; i < _nMat; i++) {
				var m = mats[i]; int b = i * MTL;
				mb[b] = m.Type; mb[b + 1] = (float)m.R; mb[b + 2] = (float)m.G; mb[b + 3] = (float)m.B;
				mb[b + 4] = (float)m.Rough; mb[b + 5] = (float)m.Ior;
				mb[b + 6] = (float)m.ER; mb[b + 7] = (float)m.EG; mb[b + 8] = (float)m.EB;
				mb[b + 9] = m.TexId; mb[b + 10] = m.NormalMap; mb[b + 11] = m.NormalTex;
			}
			var lights = s._lights; _nLight = lights.Count;
			var lb = new float[_nLight * LGT];
			for (int i = 0; i < _nLight; i++) {
				var l = lights[i]; int b = i * LGT;
				lb[b] = (float)l.X; lb[b + 1] = (float)l.Y; lb[b + 2] = (float)l.Z;
				lb[b + 3] = (float)l.R; lb[b + 4] = (float)l.G; lb[b + 5] = (float)l.B;
			}
			float[] rects = BuildAtlas(s);   // uploads the atlas texture, returns per-texId (ox,oy,w,h) rects; sets _nRect

			_lightOff = _nMat * MTL;
			_rectOff = _lightOff + _nLight * LGT;
			var aux = new float[Math.Max(1, _rectOff + rects.Length)];
			Array.Copy(mb, 0, aux, 0, mb.Length);
			Array.Copy(lb, 0, aux, _lightOff, lb.Length);
			Array.Copy(rects, 0, aux, _rectOff, rects.Length);
			UploadSsboArr(ref _auxSsbo, aux);
		}

		private unsafe void UploadSsbo(ref uint ssbo, List<float> data, int stride) {
			if (ssbo == 0) ssbo = Gl.GenBuffer();
			Gl.BindBuffer(BufferTargetARB.ShaderStorageBuffer, ssbo);
			if (data.Count == 0) { float[] z = new float[stride]; Gl.BufferData<float>(BufferTargetARB.ShaderStorageBuffer, (ReadOnlySpan<float>)z, BufferUsageARB.DynamicDraw); }
			else Gl.BufferData<float>(BufferTargetARB.ShaderStorageBuffer, (ReadOnlySpan<float>)CollectionsMarshal.AsSpan(data), BufferUsageARB.DynamicDraw);
		}
		private unsafe void UploadSsboArr(ref uint ssbo, float[] data) {
			if (ssbo == 0) ssbo = Gl.GenBuffer();
			Gl.BindBuffer(BufferTargetARB.ShaderStorageBuffer, ssbo);
			Gl.BufferData<float>(BufferTargetARB.ShaderStorageBuffer, (ReadOnlySpan<float>)data, BufferUsageARB.DynamicDraw);
		}

		// Pack every registered texture into a single RGBA8 atlas (shelf packer); UPLOAD the atlas texture
		// and RETURN each id's (offsetX, offsetY, w, h) rects (indexed by texId) for the caller to fold into
		// the aux SSBO. The shader samples by texelFetch with manual REPEAT wrap. Sizes are tiny (16/32/64).
		private unsafe float[] BuildAtlas(Lua3DScene s) {
			int maxId = -1;
			foreach (var id in s._texPix.Keys) if (id > maxId) maxId = id;
			_nRect = maxId + 1;
			var rects = new float[Math.Max(1, _nRect) * 4];

			const int ATLAS_W = 512;
			int cx = 0, cy = 0, shelfH = 0, usedW = 0;
			// first pass: shelf-place to compute offsets + atlas height
			var place = new Dictionary<int, (int ox, int oy)>();
			foreach (var kv in s._texPix) {
				int id = kv.Key; int w = s._texW[id], h = s._texH[id];
				if (cx + w > ATLAS_W) { cx = 0; cy += shelfH; shelfH = 0; }
				place[id] = (cx, cy);
				if (id >= 0 && id < _nRect) { int b = id * 4; rects[b] = cx; rects[b + 1] = cy; rects[b + 2] = w; rects[b + 3] = h; }
				cx += w; if (h > shelfH) shelfH = h; if (cx > usedW) usedW = cx;
			}
			int atlasH = cy + shelfH;
			if (usedW < 1) usedW = 1; if (atlasH < 1) atlasH = 1;

			var pix = new byte[usedW * atlasH * 4];   // transparent default
			foreach (var kv in s._texPix) {
				int id = kv.Key; int w = s._texW[id], h = s._texH[id]; var src = kv.Value;
				var (ox, oy) = place[id];
				for (int y = 0; y < h; y++) {
					int drow = ((oy + y) * usedW + ox) * 4;
					int srow = y * w;
					for (int x = 0; x < w; x++) {
						int p = src[srow + x];
						int o = drow + x * 4;
						pix[o] = (byte)(p >> 16); pix[o + 1] = (byte)(p >> 8); pix[o + 2] = (byte)p; pix[o + 3] = 255;
					}
				}
			}

			if (_atlasTex != 0) { Gl.DeleteTexture(_atlasTex); _atlasTex = 0; }
			_atlasTex = Gl.GenTexture();
			Gl.BindTexture(TextureTarget.Texture2D, _atlasTex);
			fixed (byte* dp = pix)
				Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8, (uint)usedW, (uint)atlasH, 0, PixelFormat.Rgba, PixelType.UnsignedByte, dp);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
			_atlasW = usedW; _atlasH = atlasH;
			return rects;
		}

		// ── present (fullscreen triangle) ───────────────────────────────────────────────────────
		private const string VS_PRESENT = @"#version 300 es
const vec2 P[3] = vec2[3](vec2(-1.0,-1.0), vec2(3.0,-1.0), vec2(-1.0,3.0));
void main(){ gl_Position = vec4(P[gl_VertexID], 0.0, 1.0); }";
		private const string FS_PRESENT = @"#version 300 es
precision highp float;
uniform highp sampler2D uAccum; uniform float uInv;
out vec4 frag;
void main(){
    vec3 c = texelFetch(uAccum, ivec2(gl_FragCoord.xy), 0).rgb * uInv;
    c = c / (1.0 + c);                       // Reinhard tonemap (matches CPU Out)
    c = pow(max(c, vec3(0.0)), vec3(1.0/2.2));
    frag = vec4(c, 1.0);
}";

		// Round brilliant-cut gem facets (same as Raytracer.BuildGemFacets), emitted as a GLSL const array.
		private static string GemFacetsGlsl() {
			var list = new List<double[]>();
			list.Add(new[] { 0.0, 0.0, 1.0, 0.42 });                       // table
			for (int k = 0; k < 8; k++) { double az = k * (Math.PI / 4); list.Add(new[] { Math.Cos(az) * 0.545, Math.Sin(az) * 0.545, 0.838, 0.62 }); }
			for (int k = 0; k < 16; k++) { double az = k * (Math.PI / 8); list.Add(new[] { Math.Cos(az), Math.Sin(az), 0.0, 0.5 }); }
			for (int k = 0; k < 8; k++) { double az = k * (Math.PI / 4) + (Math.PI / 8); list.Add(new[] { Math.Cos(az) * 0.643, Math.Sin(az) * 0.643, -0.766, 0.5 }); }
			var sb = new StringBuilder();
			sb.Append("const vec4 GEM[33]=vec4[33](");
			for (int i = 0; i < list.Count; i++) {
				var f = list[i];
				sb.Append(i == 0 ? "" : ",").Append("vec4(")
				  .Append(G(f[0])).Append(',').Append(G(f[1])).Append(',').Append(G(f[2])).Append(',').Append(G(f[3])).Append(')');
			}
			sb.Append(");");
			return sb.ToString();
		}
		// Format a double as a GLSL float literal WITHOUT scientific notation (some GLSL compilers reject
		// the `E-17`-style literals that round-trip formatting produces for near-zero cos/sin facet values)
		// and always with a decimal point. Tiny magnitudes collapse to 0.0.
		private static string G(double v) {
			if (Math.Abs(v) < 1e-7) return "0.0";
			string s = v.ToString("0.0########", System.Globalization.CultureInfo.InvariantCulture);
			return s;
		}

		private static string BuildComputeSource() {
			string gem = GemFacetsGlsl();   // const vec4 GEM[33]=vec4[33](...);
			return @"#version 310 es
precision highp float;
precision highp int;
layout(local_size_x=8, local_size_y=8) in;
// GLES forbids readwrite on rgba32f images -- ping-pong a readonly previous + a writeonly output image.
layout(rgba32f, binding=0) readonly  uniform highp image2D uPrev;
layout(rgba32f, binding=1) writeonly uniform highp image2D uOut;

layout(std430, binding=1) readonly buffer Tris  { float tri[]; };
layout(std430, binding=2) readonly buffer Prims { float prim[]; };
// One packed buffer for the small arrays: [materials][lights][rects]. GLES 3.1 only guarantees 4 compute
// storage blocks (ANGLE/D3D11 maps SSBOs to UAVs), so keeping the count low (3 + the image) is essential.
// 5 separate SSBOs silently failed to link and dropped the whole stage to the CPU tracer.
layout(std430, binding=3) readonly buffer Aux   { float aux[]; };
uniform highp sampler2D uAtlas;

uniform vec3 uEye, uR, uU, uF;
uniform int uW, uH, uBase, uSPP;
uniform float uScale;
uniform int uNumTri, uNumPrim, uNumMat, uNumLight, uNumRect, uLightOff, uRectOff;
uniform vec3 uSkyB, uSkyT; uniform float uSkyStrength;

const int TRI=23, PRM=15, MTL=12, LGT=6, MAXD=8;
const float PI=3.14159265358979;
" + gem + @"

struct Hit { float t; vec3 p; vec3 n; int mat; bool hasUV; vec2 uv; int tex; float shade; vec3 fall; int ti; };

uint hashu(uint x){ x^=x>>16; x*=0x7feb352du; x^=x>>15; x*=0x846ca68bu; x^=x>>16; return x; }
float nextd(inout uint s){ s^=s<<13; s^=s>>17; s^=s<<5; return float(s & 0xffffffu)/16777216.0; }

vec3 sky(vec3 d){ float t=clamp(0.5*(d.y+1.0),0.0,1.0); return mix(uSkyB,uSkyT,t)*uSkyStrength; }

vec3 sampleAlbedo(int id, vec2 uv){
    if(id<0 || id>=uNumRect) return vec3(0.8);
    int b=uRectOff+id*4; float tw=aux[b+2], th=aux[b+3];
    if(tw<1.0) return vec3(0.8);
    int itw=int(tw), ith=int(th);
    int x=int(uv.x*tw); x=((x%itw)+itw)%itw;
    int y=int(uv.y*th); y=((y%ith)+ith)%ith;
    vec3 c=texelFetch(uAtlas, ivec2(int(aux[b])+x, int(aux[b+1])+y), 0).rgb;
    return pow(max(c,vec3(0.0)), vec3(2.2));
}
vec3 texNormalRaw(int id, vec2 uv){
    int b=uRectOff+id*4; float tw=aux[b+2], th=aux[b+3];
    int itw=int(tw), ith=int(th);
    int x=int(uv.x*tw); x=((x%itw)+itw)%itw;
    int y=int(uv.y*th); y=((y%ith)+ith)%ith;
    vec3 c=texelFetch(uAtlas, ivec2(int(aux[b])+x, int(aux[b+1])+y), 0).rgb;
    return c*2.0-1.0;
}

bool boxHit(vec3 o, vec3 d, vec3 bmin, vec3 bmax, float tmin, float tmax, out float t, out vec3 n){
    t=0.0; n=vec3(0.0);
    float tx1=(bmin.x-o.x)/d.x, tx2=(bmax.x-o.x)/d.x;
    float tlo=min(tx1,tx2), thi=max(tx1,tx2); int axis=0; float sgn=(tx1>tx2)?1.0:-1.0;
    float ty1=(bmin.y-o.y)/d.y, ty2=(bmax.y-o.y)/d.y;
    float tylo=min(ty1,ty2), tyhi=max(ty1,ty2);
    if(tylo>tlo){ tlo=tylo; axis=1; sgn=(ty1>ty2)?1.0:-1.0; }
    thi=min(thi,tyhi);
    float tz1=(bmin.z-o.z)/d.z, tz2=(bmax.z-o.z)/d.z;
    float tzlo=min(tz1,tz2), tzhi=max(tz1,tz2);
    if(tzlo>tlo){ tlo=tzlo; axis=2; sgn=(tz1>tz2)?1.0:-1.0; }
    thi=min(thi,tzhi);
    if(thi<tlo||thi<tmin) return false;
    bool ent=tlo>tmin;
    float tt=ent?tlo:thi;
    if(tt<=tmin||tt>=tmax) return false;
    t=tt; float sg=ent?sgn:-sgn;
    n = (axis==0)?vec3(sg,0.0,0.0):((axis==1)?vec3(0.0,sg,0.0):vec3(0.0,0.0,sg));
    return true;
}

float torusSDF(vec3 q, float R, float r, int axis){
    float a,b;
    if(axis==0){ a=sqrt(q.y*q.y+q.z*q.z)-R; b=q.x; }
    else if(axis==2){ a=sqrt(q.x*q.x+q.y*q.y)-R; b=q.z; }
    else { a=sqrt(q.x*q.x+q.z*q.z)-R; b=q.y; }
    return sqrt(a*a+b*b)-r;
}
float gemSDF(vec3 q, float A){
    float s=1.0/max(A,1e-3); vec3 p=q*s; float d=-1e30;
    for(int i=0;i<33;i++){ vec4 f=GEM[i]; d=max(d, f.x*p.x+f.y*p.y+f.z*p.z-f.w); }
    return d*A;
}
float primSDF(int b, vec3 p){
    vec3 c=vec3(prim[b+1],prim[b+2],prim[b+3]); vec3 q=p-c;
    int kind=int(prim[b]); float A=prim[b+4], B=prim[b+5], C=prim[b+6];
    if(kind==3) return torusSDF(q,A,B,int(C));
    int preset=int(prim[b+13]);
    if(preset==1){ float r=0.15*A; float dx=abs(q.x)-(A-r), dy=abs(q.y)-(B-r), dz=abs(q.z)-(C-r);
        float ox=max(dx,0.0),oy=max(dy,0.0),oz=max(dz,0.0);
        return sqrt(ox*ox+oy*oy+oz*oz)+min(max(dx,max(dy,dz)),0.0)-r; }
    if(preset==2) return torusSDF(q,A,B,1);
    if(preset==3){ float yy=q.y; if(yy>A) yy-=A; else if(yy<-A) yy+=A; else yy=0.0; return sqrt(q.x*q.x+yy*yy+q.z*q.z)-B; }
    if(preset==4){ float f=1.0/max(A,1e-3); float g=sin(q.x*f)*cos(q.y*f)+sin(q.y*f)*cos(q.z*f)+sin(q.z*f)*cos(q.x*f);
        float shell=(abs(g)-0.25)*A*0.5; float bound=length(q)-A*2.2; return max(shell,bound); }
    if(preset==5){ float m=abs(q.x)+abs(q.y)+abs(q.z)-A; return m*0.57735026; }
    if(preset==6) return gemSDF(q,A);
    return length(q)-A;
}
float sceneSDF(vec3 p, out int idx){
    float best=1e30; idx=-1;
    for(int i=0;i<uNumPrim;i++){ int b=i*PRM; if(int(prim[b])<3) continue; float dd=primSDF(b,p); if(dd<best){best=dd; idx=i;} }
    return best;
}
bool marchHit(vec3 o, vec3 d, float tmin, float tmax, out float tHit, out vec3 n, out int mat){
    tHit=0.0; n=vec3(0.0); mat=-1;
    bool any=false; for(int i=0;i<uNumPrim;i++){ if(int(prim[i*PRM])>=3){ any=true; break; } }
    if(!any) return false;
    int dd;
    float s0 = (sceneSDF(o+d*tmin, dd) < 0.0) ? -1.0 : 1.0;
    float t=tmin;
    for(int i=0;i<160 && t<tmax;i++){
        vec3 p=o+d*t; int idx; float dist=sceneSDF(p, idx);
        if(abs(dist) < 1e-4*(1.0+t)){
            tHit=t; mat=int(prim[idx*PRM+14]);
            float e=5e-4; int z;
            float nx=sceneSDF(vec3(p.x+e,p.y,p.z),z)-sceneSDF(vec3(p.x-e,p.y,p.z),z);
            float ny=sceneSDF(vec3(p.x,p.y+e,p.z),z)-sceneSDF(vec3(p.x,p.y-e,p.z),z);
            float nz=sceneSDF(vec3(p.x,p.y,p.z+e),z)-sceneSDF(vec3(p.x,p.y,p.z-e),z);
            n=normalize(vec3(nx,ny,nz)); return true;
        }
        t += s0*dist;
    }
    return false;
}

bool closest(vec3 o, vec3 d, float tmin, inout Hit h){
    bool hit=false;
    for(int i=0;i<uNumTri;i++){
        int b=i*TRI;
        vec3 A=vec3(tri[b],tri[b+1],tri[b+2]);
        vec3 B=vec3(tri[b+3],tri[b+4],tri[b+5]);
        vec3 C=vec3(tri[b+6],tri[b+7],tri[b+8]);
        vec3 e1=B-A, e2=C-A;
        vec3 pv=cross(d,e2); float det=dot(e1,pv);
        if(abs(det)<1e-12) continue;
        float idet=1.0/det;
        vec3 tv=o-A; float u=dot(tv,pv)*idet; if(u<0.0||u>1.0) continue;
        vec3 qv=cross(tv,e1); float v=dot(d,qv)*idet; if(v<0.0||u+v>1.0) continue;
        float tt=dot(e2,qv)*idet; if(tt<=tmin||tt>=h.t) continue;
        float w=1.0-u-v;
        h.t=tt; hit=true; h.p=o+d*tt; h.n=normalize(cross(e1,e2));
        h.mat=int(tri[b+19]); h.shade=tri[b+18]; h.ti=i;
        h.tex=int(tri[b+20]); h.hasUV=(h.tex>=0);
        h.uv=vec2(tri[b+9]*w+tri[b+11]*u+tri[b+13]*v, tri[b+10]*w+tri[b+12]*u+tri[b+14]*v);
        h.fall=vec3(tri[b+15],tri[b+16],tri[b+17]);
    }
    for(int i=0;i<uNumPrim;i++){
        int b=i*PRM; int kind=int(prim[b]); vec3 c=vec3(prim[b+1],prim[b+2],prim[b+3]);
        if(kind==0){
            float rad=prim[b+4]; vec3 oc=o-c; float bb=dot(oc,d), cc=dot(oc,oc)-rad*rad; float disc=bb*bb-cc; if(disc<0.0) continue;
            float sq=sqrt(disc); float tt=-bb-sq; if(tt<=tmin) tt=-bb+sq; if(tt<=tmin||tt>=h.t) continue;
            h.t=tt; hit=true; h.p=o+d*tt; h.n=normalize((h.p-c)/rad); h.mat=int(prim[b+14]); h.hasUV=false; h.tex=-1; h.ti=-1;
        } else if(kind==1){
            vec3 nrm=vec3(prim[b+4],prim[b+5],prim[b+6]); float den=dot(d,nrm); if(abs(den)<1e-9) continue;
            float tt=dot(c-o,nrm)/den; if(tt<=tmin||tt>=h.t) continue;
            h.t=tt; hit=true; h.p=o+d*tt; h.n=nrm; h.mat=int(prim[b+14]); h.hasUV=false; h.tex=-1; h.ti=-1;
        } else if(kind==2){
            vec3 bmin=vec3(prim[b+7],prim[b+8],prim[b+9]), bmax=vec3(prim[b+10],prim[b+11],prim[b+12]);
            float tt; vec3 bn;
            if(boxHit(o,d,bmin,bmax,tmin,h.t,tt,bn)){ h.t=tt; hit=true; h.p=o+d*tt; h.n=bn; h.mat=int(prim[b+14]); h.hasUV=false; h.tex=-1; h.ti=-1; }
        }
    }
    float mt; vec3 mn; int mm;
    if(marchHit(o,d,tmin,h.t,mt,mn,mm)){ h.t=mt; hit=true; h.p=o+d*mt; h.n=mn; h.mat=mm; h.hasUV=false; h.tex=-1; h.ti=-1; }
    return hit;
}

bool occluded(vec3 o, vec3 d, float tmax){
    for(int i=0;i<uNumTri;i++){
        int b=i*TRI;
        vec3 A=vec3(tri[b],tri[b+1],tri[b+2]), B=vec3(tri[b+3],tri[b+4],tri[b+5]), C=vec3(tri[b+6],tri[b+7],tri[b+8]);
        vec3 e1=B-A, e2=C-A; vec3 pv=cross(d,e2); float det=dot(e1,pv); if(abs(det)<1e-12) continue;
        float idet=1.0/det; vec3 tv=o-A; float u=dot(tv,pv)*idet; if(u<0.0||u>1.0) continue;
        vec3 qv=cross(tv,e1); float v=dot(d,qv)*idet; if(v<0.0||u+v>1.0) continue;
        float tt=dot(e2,qv)*idet; if(tt>1e-4 && tt<tmax) return true;
    }
    for(int i=0;i<uNumPrim;i++){
        int b=i*PRM; int kind=int(prim[b]);
        if(kind==0){ vec3 c=vec3(prim[b+1],prim[b+2],prim[b+3]); float rad=prim[b+4]; vec3 oc=o-c; float bb=dot(oc,d), cc=dot(oc,oc)-rad*rad; float disc=bb*bb-cc; if(disc<0.0) continue; float sq=sqrt(disc); float tt=-bb-sq; if(tt<=1e-4) tt=-bb+sq; if(tt>1e-4&&tt<tmax) return true; }
        else if(kind==2){ vec3 bmin=vec3(prim[b+7],prim[b+8],prim[b+9]), bmax=vec3(prim[b+10],prim[b+11],prim[b+12]); float tt; vec3 bn; if(boxHit(o,d,bmin,bmax,1e-4,tmax,tt,bn)) return true; }
    }
    float mt; vec3 mn; int mm; if(marchHit(o,d,1e-4,tmax,mt,mn,mm)) return true;
    return false;
}

vec3 directLight(vec3 p, vec3 n){
    vec3 sum=vec3(0.0);
    for(int i=0;i<uNumLight;i++){
        int b=uLightOff+i*LGT; vec3 lp=vec3(aux[b],aux[b+1],aux[b+2]);
        vec3 to=lp-p; float dist=length(to); if(dist<1e-6) continue;
        vec3 l=to/dist; float ndl=dot(n,l); if(ndl<=0.0) continue;
        if(occluded(p+n*1e-4, l, dist-2e-3)) continue;
        float inv=ndl/(PI*dist*dist);
        sum += vec3(aux[b+3],aux[b+4],aux[b+5])*inv;
    }
    return sum;
}

uint vhash(int x,int y,int z){ int v=x*374761393+y*668265263+z*1274126177; uint h=uint(v); h=(h^(h>>13))*1274126177u; return (h^(h>>16))&0xffffffu; }
float vhashf(int x,int y,int z){ return float(vhash(x,y,z))/16777216.0; }
float noise3(vec3 p){
    int xi=int(floor(p.x)),yi=int(floor(p.y)),zi=int(floor(p.z));
    float fx=p.x-float(xi),fy=p.y-float(yi),fz=p.z-float(zi);
    float sx=fx*fx*(3.0-2.0*fx),sy=fy*fy*(3.0-2.0*fy),sz=fz*fz*(3.0-2.0*fz);
    float c000=vhashf(xi,yi,zi),c100=vhashf(xi+1,yi,zi),c010=vhashf(xi,yi+1,zi),c110=vhashf(xi+1,yi+1,zi);
    float c001=vhashf(xi,yi,zi+1),c101=vhashf(xi+1,yi,zi+1),c011=vhashf(xi,yi+1,zi+1),c111=vhashf(xi+1,yi+1,zi+1);
    float x00=mix(c000,c100,sx),x10=mix(c010,c110,sx),x01=mix(c001,c101,sx),x11=mix(c011,c111,sx);
    float y0=mix(x00,x10,sy),y1=mix(x01,x11,sy);
    return mix(y0,y1,sz)*2.0-1.0;
}
float fbm(vec3 p){ return noise3(p)*0.5+noise3(p*2.03)*0.25+noise3(p*4.01)*0.125; }
float heightField(vec3 p, int kind){
    if(kind==1){ float rad=sqrt(p.x*p.x+p.z*p.z); return sin(rad*14.0+sin(p.y*3.0)*1.5)*0.05; }
    if(kind==3){ return (sin(p.x*5.0)+sin(p.z*5.0+p.x*1.3)+sin((p.x+p.z)*3.5))*0.05; }
    return fbm(p*2.2)*0.5;
}
vec3 perturbNormal(vec3 p, vec3 n, int kind){
    float e=0.04, amp=0.6; float f0=heightField(p,kind);
    vec3 g=vec3((heightField(p+vec3(e,0.0,0.0),kind)-f0)/e,(heightField(p+vec3(0.0,e,0.0),kind)-f0)/e,(heightField(p+vec3(0.0,0.0,e),kind)-f0)/e);
    vec3 gt=g-n*dot(g,n);
    return normalize(n-gt*amp);
}
vec3 woodAlbedo(vec3 p, vec3 baseAlb){
    float rad=sqrt(p.x*p.x+p.z*p.z); float rings=sin(rad*14.0+sin(p.y*3.0)*1.5);
    float f=0.55+0.45*(rings*0.5+0.5); return baseAlb*f;
}
vec3 texNormal(int ti, vec3 n, int texId, vec2 uv){
    if(texId<0||texId>=uNumRect) return n;
    int b=ti*TRI;
    vec3 A=vec3(tri[b],tri[b+1],tri[b+2]), B=vec3(tri[b+3],tri[b+4],tri[b+5]), C=vec3(tri[b+6],tri[b+7],tri[b+8]);
    vec3 e1=B-A, e2=C-A;
    float du1=tri[b+11]-tri[b+9], dv1=tri[b+12]-tri[b+10], du2=tri[b+13]-tri[b+9], dv2=tri[b+14]-tri[b+10];
    float det=du1*dv2-du2*dv1; if(abs(det)<1e-12) return n;
    float r=1.0/det;
    vec3 tang=(e1*dv2 - e2*dv1)*r;
    tang=normalize(tang - n*dot(tang,n));
    vec3 bit=cross(n,tang);
    vec3 nm=texNormalRaw(texId, uv);
    return normalize(tang*nm.x + bit*nm.y + n*max(nm.z,0.05));
}

vec3 cosineHemisphere(vec3 n, inout uint seed){
    float r1=nextd(seed), r2=nextd(seed);
    float r=sqrt(r1), phi=2.0*PI*r2;
    float x=r*cos(phi), y=r*sin(phi), z=sqrt(max(0.0,1.0-r1));
    vec3 w=n; vec3 a=(abs(w.x)>0.9)?vec3(0.0,1.0,0.0):vec3(1.0,0.0,0.0);
    vec3 u=normalize(cross(a,w)); vec3 v=cross(w,u);
    return normalize(u*x+v*y+w*z);
}
vec3 randUnit(inout uint seed){
    float z=nextd(seed)*2.0-1.0, a=nextd(seed)*2.0*PI, r=sqrt(max(0.0,1.0-z*z));
    return vec3(r*cos(a), r*sin(a), z);
}

void resolve(Hit h, out int type, out vec3 alb, out vec3 emis, out float rough, out float ior, out int nmap, out int normTex){
    type=0; rough=1.0; ior=1.5; nmap=0; normTex=-1; emis=vec3(0.0); alb=vec3(0.8);
    if(h.mat>=0 && h.mat<uNumMat){
        int b=h.mat*MTL;   // materials live at the start of the aux buffer (offset 0)
        type=int(aux[b]); rough=aux[b+4]; ior=aux[b+5]; nmap=int(aux[b+10]); normTex=int(aux[b+11]);
        emis=vec3(aux[b+6],aux[b+7],aux[b+8]);
        int mtex=int(aux[b+9]);
        if(mtex>=0 && h.hasUV) alb=sampleAlbedo(mtex, h.uv);
        else alb=vec3(aux[b+1],aux[b+2],aux[b+3]);
    } else if(h.tex>=0){
        alb=sampleAlbedo(h.tex, h.uv)*h.shade;
    } else {
        alb=h.fall;
    }
}

vec3 trace(vec3 o, vec3 d, inout uint seed){
    vec3 L=vec3(0.0), thr=vec3(1.0);
    for(int bounce=0; bounce<MAXD; bounce++){
        Hit h; h.t=1e30; h.ti=-1; h.mat=-1; h.hasUV=false; h.tex=-1; h.shade=1.0; h.fall=vec3(0.8); h.uv=vec2(0.0);
        if(!closest(o,d,1e-4,h)){ L+=thr*sky(d); break; }
        int type,nmap,normTex; vec3 alb,emis; float rough,ior;
        resolve(h,type,alb,emis,rough,ior,nmap,normTex);
        vec3 ng=h.n; float cosI=-dot(d,ng); bool inside=cosI<0.0;
        vec3 n = inside ? -ng : ng;
        if(normTex>=0 && h.ti>=0) n=texNormal(h.ti,n,normTex,h.uv);
        else if(nmap!=0){ if(nmap==1) alb=woodAlbedo(h.p,alb); n=perturbNormal(h.p,n,nmap); }
        L+=thr*emis;
        if(type==3) break;
        vec3 p=h.p;
        if(type==2){
            float eta = inside ? ior : 1.0/ior;
            float c=abs(cosI);
            float k=1.0-eta*eta*(1.0-c*c);
            vec3 refl=reflect(d,n);
            if(k<0.0){ o=p+n*1e-4; d=refl; }
            else {
                float cosT=sqrt(k);
                float r0=(eta-1.0)/(eta+1.0); r0*=r0;
                float fr=r0+(1.0-r0)*pow(1.0-c,5.0);
                if(nextd(seed)<fr){ o=p+n*1e-4; d=refl; }
                else { vec3 rt=normalize(d*eta + n*(eta*c-cosT)); o=p-n*1e-4; d=rt; }
            }
            thr*=alb;
        } else if(type==1){
            vec3 refl=reflect(d,n);
            if(rough>0.0) refl=normalize(refl + randUnit(seed)*rough);
            if(dot(refl,n)<0.0) refl=reflect(d,n);
            o=p+n*1e-4; d=refl; thr*=alb;
        } else {
            L+=thr*alb*directLight(p,n);
            vec3 nd=cosineHemisphere(n,seed);
            o=p+n*1e-4; d=nd; thr*=alb;
        }
        if(bounce>=3){
            float q=max(thr.x,max(thr.y,thr.z));
            if(q<1.0){ if(nextd(seed)>q) break; thr*=1.0/max(q,1e-4); }
        }
    }
    return L;
}

void main(){
    ivec2 gid=ivec2(gl_GlobalInvocationID.xy);
    if(gid.x>=uW || gid.y>=uH) return;
    int px=gid.x, py=gid.y;
    uint idx=uint(py*uW+px);
    float hw=float(uW)*0.5, hh=float(uH)*0.5;
    vec3 sum=vec3(0.0);
    for(int sp=0; sp<uSPP; sp++){
        uint si=uint(uBase+sp+1);                       // 1-based global sample index (matches the CPU seed)
        uint seed=hashu(idx*2654435761u ^ si*40503u);
        float jx=nextd(seed), jy=nextd(seed);
        float sx=(float(px)+jx-hw)/uScale;
        float sy=-(float(py)+jy-hh)/uScale;
        vec3 dir=normalize(uR*sx + uU*sy + uF);
        sum += trace(uEye, dir, seed);
    }
    vec3 prev = (uBase==0) ? vec3(0.0) : imageLoad(uPrev, gid).rgb;   // first frame after a reset overwrites
    imageStore(uOut, gid, vec4(prev+sum, 1.0));
}";
		}
	}
}
