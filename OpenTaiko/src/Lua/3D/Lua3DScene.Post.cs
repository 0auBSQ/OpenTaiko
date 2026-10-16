namespace OpenTaiko {
	// Fog, outline, antialiasing, the diorama grade, and the sky, post and surface shaders.
	public partial class Lua3DScene {
		// Distance fog: pixels fade toward (_fogR,_fogG,_fogB) from _fogStart to _fogEnd
		// (camera-space depth). _fogInv = 1/(end-start). Off by default.
		internal bool _fog;
		internal double _fogR, _fogG, _fogB, _fogStart, _fogInv;
		// Distance BLUR (deprecated — too costly + the depth-texture FBO slowed the main pass). Kept as a no-op
		// stub so older stage scripts don't error; the diorama pass below replaces it.
		internal bool _distBlur;
		internal double _blurNear = 60, _blurFar = 200;
		// 'Diorama' grade (GPU rasterizer): a cheap screen-space tilt-shift + saturation/contrast/vignette
		// (diorama-style). No depth sample. Off by default.
		internal bool _diorama;
		internal double _hdTilt = 3.0, _hdSat = 1.25, _hdVig = 0.35, _hdBloom = 0.0;

		// Optional post-process antialiasing (FXAA-lite). Off by default; smooths a low-res render.
		internal bool _aa;
		/// <summary>Enable/disable post-process edge smoothing on the final colour buffer.</summary>
		public void SetAntialias(bool on) { _aa = on; }

		internal int _outlineR, _outlineG, _outlineB; internal double _outlineTh = 0;

		/// <summary>Toon OUTLINE for the rendered scene: silhouette ink of the given colour and pixel
		/// thickness with an anti-aliased rim. GPU scenes run it as a post shader; CPU scenes fall back
		/// to ApplyOutline after the raster. 0 thickness = off.</summary>
		public void SetOutline(int r, int g, int b, double thicknessPx) {
			_outlineR = r; _outlineG = g; _outlineB = b; _outlineTh = thicknessPx < 0 ? 0 : thicknessPx;
		}

		// ── custom SURFACE shaders (GPU rasterizer) ─────────────────────────────────────────────
		// The sky (SetSkyShader) and post pass (SetPostShader) already take raw GLSL; this extends
		// the same principle to screen-tex SURFACES (water, mirrors, portals) so stages can replace
		// the built-in ripple/fresnel look outright instead of tweaking baked-in parameters.
		internal readonly float[] _surfUser = new float[16];

		/// <summary>Attach a CUSTOM fragment shader to a screen-tex surface. Pass full GLSL ES 3.00
		/// source, or just a main() body — the prelude is prepended when no #version is present:
		///   in vec2 vUV; in float vShade; in float vCamZ; in vec3 vWorld; in vec3 vNormal;
		///   uniform sampler2D uTex;       // the live reflection RTT (sample by gl_FragCoord >> uRsh;
		///   uniform int uRsh, uRttFlip;   //  flip rows when uRttFlip == 1)
		///   uniform vec3 uCam, uTint, uFlat; uniform float uTime, uAlpha; uniform vec4 uUser[4];
		///   out vec4 frag;
		/// Compile failure traces, surfaces GetShaderError, and falls back to the built-in path.
		/// nil/"" detaches. Programs are cached per distinct source and freed with the render caches.</summary>
		public void ObjSetSurfaceShader(int id, string fragSrc) {
			var o = Obj(id); if (o == null) return;
			o.SurfaceSrc = string.IsNullOrWhiteSpace(fragSrc) ? null : fragSrc;
		}

		/// <summary>Set uUser[slot] (slot 0..3) shared by every custom surface shader.</summary>
		public void SetSurfaceUniform(int slot, double x, double y, double z, double w) {
			if (slot < 0 || slot > 3) return;
			int k = slot * 4;
			_surfUser[k] = (float)x; _surfUser[k + 1] = (float)y; _surfUser[k + 2] = (float)z; _surfUser[k + 3] = (float)w;
		}

		/// <summary>Distance fog: pixels fade to (r,g,b) (0-255) between camera depths
		/// <paramref name="start"/> and <paramref name="end"/>. <paramref name="on"/>=false disables.</summary>
		public void SetFog(bool on, double r, double g, double b, double start, double end) {
			_fog = on;
			_fogR = r; _fogG = g; _fogB = b; _fogStart = start;
			_fogInv = end > start ? 1.0 / (end - start) : 0.0;
		}
		/// <summary>Deprecated no-op (the old distance blur). Kept so older scripts don't break — use SetDiorama.</summary>
		public void SetDistanceBlur(bool on, double near, double far) { _distBlur = false; }
		/// <summary>diorama 'diorama' post grade (GPU rasterizer): a cheap screen-space tilt-shift (sharp middle band,
		/// blur to the top/bottom) + saturation/contrast/vignette + bloom. <paramref name="tilt"/> = max blur px
		/// (0 = none), <paramref name="sat"/> = saturation (1 = none), <paramref name="vig"/> = vignette strength,
		/// <paramref name="bloom"/> = glow strength (0 = off; the diorama-style highlight halo).</summary>
		public void SetDiorama(bool on, double tilt, double sat, double vig, double bloom = 0.0) {
			_diorama = on; _hdTilt = tilt; _hdSat = sat; _hdVig = vig; _hdBloom = bloom;
		}

		// ── stylized-render extensions (GPU rasterizer; ObjSetEmissive applies on every renderer) ──
		internal double _wetness;
		internal double _shaderTime;
		internal double _gradeExp = 1.0, _gradeR = 1.0, _gradeG = 1.0, _gradeB = 1.0;
		internal string _postSrc; internal int _postSrcRev;
		internal readonly float[] _postUser = new float[16];

		/// <summary>Global surface wetness 0..1 (rain look): darkens lit diffuse and adds a sun
		/// specular sheen. 0 = dry (default, costs nothing).</summary>
		public void SetWetness(double w) { _wetness = w < 0 ? 0 : (w > 1 ? 1 : w); }

		/// <summary>Shader clock in seconds — drives the screen-tex ripple and the custom post pass
		/// (uTime). Feed your stage clock once per frame.</summary>
		public void SetTime(double t) { _shaderTime = t; }

		/// <summary>Colour grade folded into the diorama post pass: exposure multiply + per-channel
		/// tint (day/night mood — dusk oranges, night blues). 1,1,1,1 = neutral. Needs SetDiorama on.</summary>
		public void SetColorGrade(double exposure, double r, double g, double b) {
			_gradeExp = exposure < 0 ? 0 : exposure; _gradeR = r; _gradeG = g; _gradeB = b;
		}

		/// <summary>Attach a CUSTOM full-screen post-process fragment shader (GPU rasterizer). Pass either a
		/// full GLSL ES 3.00 fragment source, or just a body containing <c>void main()</c> — the standard
		/// prelude (uTex = the rendered frame, uRes, uTime, uUser[4], out vec4 frag) is prepended when no
		/// #version line is present. Runs after the diorama grade. Compile failure traces and disables the
		/// pass (the frame renders ungraded rather than dying). nil/"" detaches.</summary>
		public void SetPostShader(string fragSrc) {
			_postSrc = string.IsNullOrWhiteSpace(fragSrc) ? null : fragSrc;
			_postSrcRev++;
		}

		/// <summary>Set uUser[slot] (slot 0..3) for the custom post shader.</summary>
		public void SetPostUniform(int slot, double x, double y, double z, double w) {
			if (slot < 0 || slot > 3) return;
			int k = slot * 4;
			_postUser[k] = (float)x; _postUser[k + 1] = (float)y; _postUser[k + 2] = (float)z; _postUser[k + 3] = (float)w;
		}

		// ── procedural SKY pass (GPU rasterizer) ─────────────────────────────────────────────────
		internal string _skySrc; internal int _skySrcRev;
		internal readonly float[] _skyUser = new float[32];
		/// <summary>Attach a full-screen SKY fragment shader, drawn into the canvas before the 3D pass
		/// (same source rules as <see cref="SetPostShader"/>: full GLSL ES 3.00 source, or a main() body
		/// that uses the prelude's uRes/uTime/uUser[8] — no uTex; the sky has no input image). While
		/// attached it replaces the stage's 2D sky fill. nil/"" detaches. Compile failure disables it.</summary>
		public void SetSkyShader(string fragSrc) {
			_skySrc = string.IsNullOrWhiteSpace(fragSrc) ? null : fragSrc;
			_skySrcRev++;
		}
		/// <summary>Set uUser[slot] (slot 0..7) for the sky shader.</summary>
		public void SetSkyUniform(int slot, double x, double y, double z, double w) {
			if (slot < 0 || slot > 7) return;
			int k = slot * 4;
			_skyUser[k] = (float)x; _skyUser[k + 1] = (float)y; _skyUser[k + 2] = (float)z; _skyUser[k + 3] = (float)w;
		}
		/// <summary>Last sky/post shader compile error from the GPU renderer ("" when clean).</summary>
		public string GetShaderError() {
			foreach (var r in _made) if (r.ShaderError.Length > 0) return r.ShaderError;
			return "";
		}
		internal bool? _skyCompiled;   // set by the renderer after the (re)compile attempt
		/// <summary>True when the GPU renderer will draw the attached sky shader this frame — the stage
		/// can skip its 2D gradient fill then. False before attach, on the CPU renderer, or after a
		/// compile failure (the stage's band fallback takes over on the next frame).</summary>
		public bool SkyShaderActive() => _skySrc != null && _renderer.DrawsSkyShader && _skyCompiled != false;
	}
}
