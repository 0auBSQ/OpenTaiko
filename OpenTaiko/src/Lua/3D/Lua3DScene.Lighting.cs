using System;

namespace OpenTaiko {
	// Raster lighting: sun, ambient, shadows and the ground-sprite shadow light.
	public partial class Lua3DScene {
		// ground-sprite (billboard mode 3) cast-shadow light: the horizontal direction shadows stretch in,
		// and a length scale. Set from Lua (SetGroundSpriteLight) to match the stage's shadow light.
		internal double _GsLx = 0, _GsLz = 1, _GsLen = 1.4;

		internal double _shadowHalf = 60.0;

		/// <summary>Half-extent (world units) of the sun shadow map's box around the focus. Smaller = sharper
		/// shadows over a tighter area (e.g. small iso maps want ~18); default 60.</summary>
		public void SetShadowArea(double half) { _shadowHalf = half < 4 ? 4 : half; }

		/// <summary>Set the cast-shadow light for ground-flat sprites (billboard mode 3): the horizontal
		/// direction the silhouette stretches in (dx,dz) and a length scale (≈ 1/tan(sun elevation)). A
		/// transparency-aware shadow is drawn by adding the actor's own sprite to a black-tinted object in
		/// this mode — it lies on the floor with the sprite's cutout alpha as the shape.</summary>
		public void SetGroundSpriteLight(double dx, double dz, double lenScale) {
			double l = Math.Sqrt(dx * dx + dz * dz);
			if (l > 1e-6) { _GsLx = dx / l; _GsLz = dz / l; }
			_GsLen = lenScale > 0.05 ? lenScale : 0.05;
		}

		// ── Rasterizer forward lighting ───────────────────────────────────────────────────
		internal bool _useLights;     // master switch for the rasterizer's forward lighting
		// Directional sun + global ambient (cheap, per-face): lit pixel = texel * (ambient +
		// shade * sunColour * max(0, N·sunDir) + Σ point lights). `shade` is the per-face sky
		// exposure (gates the sun so it doesn't leak into caves); point lights add on top.
		internal double _ambR, _ambG, _ambB;
		internal double _sunX, _sunY = 1, _sunZ, _sunR, _sunG, _sunB;   // sunDir points TO the sun

		/// <summary>Enable/disable the rasterizer's forward lighting (off by default; the raytracer
		/// has its own lighting). When on, faces are lit by the ambient + sun + point lights.</summary>
		public void SetLighting(bool on) { _useLights = on; }
		internal bool _shadows;
		internal bool _shadowFocusSet; internal double _sfx, _sfy, _sfz;   // where to centre the shadow map (the player)
		/// <summary>Enable sun shadow MAPPING on the GPU rasterizer: a depth pass from the sun (terrain +
		/// objects + the sun-facing character silhouettes) sampled in a SEPARATE lit-shader VARIANT. Needs
		/// SetLighting(true) + a non-zero SetSun. Perf-safe — only shadow-enabled scenes compile/run the shadow
		/// path, so other stages are byte-identical to no-shadows. Degrades to no-shadows if unsupported.</summary>
		public void SetShadows(bool on) { _shadows = on; }
		/// <summary>Centre the sun shadow map on this world point (normally the player) each frame, so the
		/// limited-coverage depth map always covers the action. Without it the map is centred in front of the
		/// camera, which misses the play area when the camera is pulled far back (the faked-ortho iso rig).</summary>
		public void SetShadowFocus(double x, double y, double z) { _shadowFocusSet = true; _sfx = x; _sfy = y; _sfz = z; }
		/// <summary>Global ambient light (linear, added to every lit face so nothing is pure black).</summary>
		public void SetAmbient(double r, double g, double b) { if (r != _ambR || g != _ambG || b != _ambB) RtEdit(); _ambR = r; _ambG = g; _ambB = b; }
		/// <summary>Directional "sun": (dx,dy,dz) is the direction TO the sun (auto-normalised),
		/// colour (r,g,b) × brightness. Cheap — one dot product per face, gated by each face's sky
		/// exposure. Animate it for a day/night cycle. Set colour to 0 to disable.</summary>
		public void SetSun(double dx, double dy, double dz, double r, double g, double b) {
			double l = Math.Sqrt(dx * dx + dy * dy + dz * dz); if (l > 1e-9) { dx /= l; dy /= l; dz /= l; }
			if (dx != _sunX || dy != _sunY || dz != _sunZ || r != _sunR || g != _sunG || b != _sunB) RtEdit();
			_sunX = dx; _sunY = dy; _sunZ = dz; _sunR = r; _sunG = g; _sunB = b;
		}
		/// <summary>Exempt an object from forward lighting (renders at its baked shade only) — e.g.
		/// the voxel torch posts, which are the light sources themselves.</summary>
		public void ObjSetLit(int id, bool lit) { var o = Obj(id); if (o != null) { bool was = o.MergeCandidate; o.Lit = lit; MergeFlip(o, was); } }
		/// <summary>Whether this object casts into the sun shadow map (default true): its billboard sprites, and its
		/// quads and triangles when it is lit and opaque. Set false for UI markers / talk bubbles / floating arrows
		/// that shouldn't drop a shadow. (GPU rasterizer only.)</summary>
		public void ObjSetCastShadow(int id, bool on) { var o = Obj(id); if (o != null) { bool was = o.MergeCandidate; o.CastShadow = on; MergeFlip(o, was); } }
		/// <summary>Per-object GPU depth bias (polygon offset). Negative pulls toward the camera (a decal/marking
		/// drawn cleanly ON TOP of a coplanar surface), positive pushes away. 0 = off. Fixes z-fighting between
		/// intentionally-coplanar layers (road stripes on the road, ground shadows, etc.). (GPU rasterizer only.)</summary>
		public void ObjSetDepthBias(int id, double bias) { var o = Obj(id); if (o != null) o.DepthBias = (float)bias; }
	}
}
