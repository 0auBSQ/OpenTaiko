using System;

namespace OpenTaiko {
	// Shared scene data used by both renderers (Rasterizer + Raytracer). Lua3DScene owns the
	// collections; the renderers read them.

	/// <summary>A retained group of primitives (textured/flat quads, textured triangles).</summary>
	internal sealed class SceneObject {
		public double[] D = Array.Empty<double>();   // N primitives laid out as PrimLayout describes
		public int N;                 // primitive count
		public int Kind = -1;         // 0 tex-quad, 1 flat-quad, 2 tex-tri, 3 sprite, 4 sprite-quad (PrimLayout)
		public bool KindRefused;      // an add of another kind was ignored and traced (once per object)
		public bool Visible = true;
		public int Pass;              // 0 opaque (front->back), 1 transparent (back->front)
		public double R, G, B; public int A = 255;       // flat-quad colour
		public bool HasBounds;
		public bool BoundsSet;        // ObjSetBounds gave them; otherwise ObjAdd* grows them over the quads and triangles
		public double MinX, MinY, MinZ, MaxX, MaxY, MaxZ;   // in the object's own space: Transform moves them
		public double CenX, CenY, CenZ, Radius;   // bounding sphere, precomputed whenever the bounds change (cull hot path)
		public bool HasNormal; public double Nx, Ny, Nz; // axis-aligned planar back-face cull
		public double[] Transform;    // null = identity (row-major 4x4 for models)
		public int Material = -1;     // raytracer material id (-1 = use a default); rasterizer ignores
		public bool Lit = true;       // rasterizer forward lighting applies (false = render at shade only)
		public bool CastShadow = true; // the object casts into the sun shadow map (false = UI markers/bubbles)
		public float DepthBias;        // polygon-offset units (GPU): <0 pulls toward camera (decals on top), >0 pushes away. 0 = off
		public double TintR = 1, TintG = 1, TintB = 1;   // per-channel colour multiply (textured surfaces and sprites)
		public double FlatR = -1, FlatG, FlatB;          // flat albedo OVERRIDE (replaces material colour before lighting; FlatR<0 = off) — recolours one GLB part
		public double EmR, EmG, EmB;  // emissive add: col += base * emissive; values >1 feed the diorama bloom
		public bool Overlay;          // drawn last over a cleared depth buffer (e.g. first-person viewmodel)
		public bool ScreenTex;        // sample the texture by screen pixel, not UV (mirrors / portals)
		public double RippleAmp, RippleFreq, RippleSpeed;   // screen-tex sample distortion (animated water reflection)
		public double XrayAlpha;      // >0: sprites re-draw where occluded (depth-greater silhouette at this alpha)
		public double FresnelF0 = -1; // screen-tex mirrors: per-pixel Schlick reflectivity (F0; <0 = legacy per-vertex shade)
		public double DeepR = -1, DeepG, DeepB;   // screen-tex water: deep colour (vShade lerps base→deep; DeepR<0 = off)
		public string SurfaceSrc;     // screen-tex surfaces: optional CUSTOM fragment shader source
		                              // (replaces the built-in ripple/fresnel path; null = built-in)
		// TERRAIN SPLAT shading (GPU): blend 4 layer textures per pixel by a weight sprite instead
		// of the per-quad dominant texture (smooth painted paths). SplatSprite < 0 = off.
		public int SplatSprite = -1;
		public int LayerTex1 = -1, LayerTex2 = -1, LayerTex3 = -1, LayerTex4 = -1;
		public float SplatW = 1, SplatH = 1;      // splat coverage in world units (world.xz → splat uv)
		public float LayerUvScale = 1;            // world units → layer-texture uv repeat (1/cellSize)
		public double Dist;           // scratch: squared distance to camera (set each Render)
		public int GeomVersion;       // bumped on ObjBegin and every ObjAdd*; GPU rasterizer re-uploads this object's VBO only when it changes
		public PoseStamp? Pose;       // what a model's Pose last wrote here (null = not posed)
		// GPU rasterizer: the GeomVersion it last saw, the frame that changed it and how many changes came a frame or
		// two apart; GeomBusy keeps geometry the stage rebuilds every frame out of the static merge until it settles
		public int GeomSeen = int.MinValue, GeomChangedAt, GeomStreak;
		public bool GeomBusy;

		/// <summary>Whether the GPU rasterizer may merge this object into its static geometry: lit, opaque, textured
		/// quads or triangles with no transform, tint, emissive, colour override, terrain splat or special drawing.</summary>
		public bool MergeCandidate => (Kind == 0 || Kind == 2) && N > 0 && Visible && Pass == 0 && Transform == null && Lit
			&& !Overlay && !ScreenTex && A == 255 && TintR == 1.0 && TintG == 1.0 && TintB == 1.0
			&& EmR == 0.0 && EmG == 0.0 && EmB == 0.0 && FlatR < 0 && SplatSprite < 0 && CastShadow;

		/// <summary>Whether a camera at (x, y, z) is behind this planar group's facing plane (ObjSetNormal), tested
		/// against its bounds in its own space. False without a normal, or when the transform cannot be inverted.</summary>
		public bool FacesAway(double x, double y, double z) {
			if (!HasNormal) return false;
			if (Transform != null && !ToLocal(x, y, z, out x, out y, out z)) return false;
			if (Nx > 0 && x <= MinX) return true;
			if (Nx < 0 && x >= MaxX) return true;
			if (Ny > 0 && y <= MinY) return true;
			if (Ny < 0 && y >= MaxY) return true;
			if (Nz > 0 && z <= MinZ) return true;
			if (Nz < 0 && z >= MaxZ) return true;
			return false;
		}

		/// <summary>Widen the automatic bounds (unless ObjSetBounds set them) to hold one primitive's corners, read
		/// from <see cref="D"/> as <paramref name="n"/> xyz triples starting at <paramref name="k"/>, <paramref name="step"/> apart.</summary>
		public void GrowBounds(int k, int n, int step) {
			if (BoundsSet) return;
			var d = D; bool grew = false;
			for (int i = 0; i < n; i++, k += step) {
				double x = d[k], y = d[k + 1], z = d[k + 2];
				if (!HasBounds) { MinX = MaxX = x; MinY = MaxY = y; MinZ = MaxZ = z; HasBounds = true; grew = true; continue; }
				if (x < MinX) { MinX = x; grew = true; } else if (x > MaxX) { MaxX = x; grew = true; }
				if (y < MinY) { MinY = y; grew = true; } else if (y > MaxY) { MaxY = y; grew = true; }
				if (z < MinZ) { MinZ = z; grew = true; } else if (z > MaxZ) { MaxZ = z; grew = true; }
			}
			if (grew) SetSphere();
		}

		/// <summary>The bounding sphere of the bounds box.</summary>
		public void SetSphere() {
			CenX = (MinX + MaxX) * 0.5; CenY = (MinY + MaxY) * 0.5; CenZ = (MinZ + MaxZ) * 0.5;
			double rx = (MaxX - MinX) * 0.5, ry = (MaxY - MinY) * 0.5, rz = (MaxZ - MinZ) * 0.5;
			Radius = Math.Sqrt(rx * rx + ry * ry + rz * rz);
		}

		/// <summary>The bounding sphere in world space: the bounds' sphere moved by <see cref="Transform"/>, its radius
		/// grown by the transform's largest stretch. False when the object has no bounds (it is never culled); the
		/// centre is then its origin.</summary>
		public bool WorldSphere(out double x, out double y, out double z, out double r) {
			double cx = HasBounds ? CenX : 0, cy = HasBounds ? CenY : 0, cz = HasBounds ? CenZ : 0;
			r = HasBounds ? Radius : 0;
			var t = Transform;
			if (t == null) { x = cx; y = cy; z = cz; return HasBounds; }
			x = t[0] * cx + t[1] * cy + t[2] * cz + t[3];
			y = t[4] * cx + t[5] * cy + t[6] * cz + t[7];
			z = t[8] * cx + t[9] * cy + t[10] * cz + t[11];
			r *= MaxStretch(t);
			return HasBounds;
		}

		/// <summary>An upper bound of how far the linear part of a row-major transform stretches any vector (its
		/// largest singular value): the Gershgorin bound on the Gram matrix of its columns, exact for a rotation
		/// times an axis scale.</summary>
		public static double MaxStretch(double[] t) {
			double ax = t[0], ay = t[4], az = t[8], bx = t[1], by = t[5], bz = t[9], cx = t[2], cy = t[6], cz = t[10];
			double aa = ax * ax + ay * ay + az * az, bb = bx * bx + by * by + bz * bz, cc = cx * cx + cy * cy + cz * cz;
			double ab = Math.Abs(ax * bx + ay * by + az * bz), ac = Math.Abs(ax * cx + ay * cy + az * cz), bc = Math.Abs(bx * cx + by * cy + bz * cz);
			return Math.Sqrt(Math.Max(aa + ab + ac, Math.Max(bb + ab + bc, cc + ac + bc)));
		}

		/// <summary>World point (x, y, z) in the object's own space, through the inverse of <see cref="Transform"/>.
		/// False when the object has no transform or a singular one; the point then comes back unchanged.</summary>
		public bool ToLocal(double x, double y, double z, out double lx, out double ly, out double lz) {
			lx = x; ly = y; lz = z;
			var t = Transform; if (t == null) return false;
			Span<double> adj = stackalloc double[9];
			double det = Adjugate(t, adj);
			if (det == 0 || !double.IsFinite(det)) return false;
			double dx = x - t[3], dy = y - t[7], dz = z - t[11], inv = 1.0 / det;
			lx = (adj[0] * dx + adj[1] * dy + adj[2] * dz) * inv;
			ly = (adj[3] * dx + adj[4] * dy + adj[5] * dz) * inv;
			lz = (adj[6] * dx + adj[7] * dy + adj[8] * dz) * inv;
			return true;
		}

		/// <summary>Turn a normal of the object's own-space surfaces into world space through <see cref="Transform"/>
		/// (its inverse transpose, left unnormalised). Unchanged without a transform.</summary>
		public void NormalToWorld(ref double nx, ref double ny, ref double nz) {
			var t = Transform; if (t == null) return;
			Span<double> adj = stackalloc double[9];
			double sg = Adjugate(t, adj) < 0 ? -1 : 1;
			double wx = adj[0] * nx + adj[3] * ny + adj[6] * nz;
			double wy = adj[1] * nx + adj[4] * ny + adj[7] * nz;
			double wz = adj[2] * nx + adj[5] * ny + adj[8] * nz;
			nx = sg * wx; ny = sg * wy; nz = sg * wz;
		}

		// the adjugate (row-major) of a row-major transform's linear part, whose columns are the cross products
		// b×c, c×a and a×b of its rows a, b, c; returns the determinant (adjugate / determinant = the inverse)
		private static double Adjugate(double[] t, Span<double> adj) {
			double ax = t[0], ay = t[1], az = t[2], bx = t[4], by = t[5], bz = t[6], cx = t[8], cy = t[9], cz = t[10];
			adj[0] = by * cz - bz * cy; adj[3] = bz * cx - bx * cz; adj[6] = bx * cy - by * cx;
			adj[1] = cy * az - cz * ay; adj[4] = cz * ax - cx * az; adj[7] = cx * ay - cy * ax;
			adj[2] = ay * bz - az * by; adj[5] = az * bx - ax * bz; adj[8] = ax * by - ay * bx;
			return ax * adj[0] + ay * adj[3] + az * adj[6];
		}
	}

	/// <summary>The layout of <see cref="SceneObject.D"/>, shared by every renderer: an object holds primitives of one
	/// kind, each a run of doubles of the kind's stride. Quads keep their four corners as xyz at 0, 3, 6 and 9 (winding
	/// 0-1-2-3); textured triangles keep three corners as xyz then uv at 0, 5 and 10.</summary>
	internal static class PrimLayout {
		/// <summary>Textured quad: the corners, the texture id, the uv extent and the shade. The corners take the uvs
		/// (0, vMax), (uMax, vMax), (uMax, 0) and (0, 0).</summary>
		public const int TexQuadStride = 16, TexQuadTex = 12, TexQuadUMax = 13, TexQuadVMax = 14, TexQuadShade = 15;
		/// <summary>Flat quad: the corners (the object holds the colour).</summary>
		public const int FlatQuadStride = 12;
		/// <summary>Textured triangle: the corners with their uvs, the texture id and the shade.</summary>
		public const int TexTriStride = 17, TexTriTex = 15, TexTriShade = 16;
		/// <summary>Billboard sprite: the foot xyz, width, height, sprite id, billboard mode and cutout flag.</summary>
		public const int SpriteStride = 8, SpriteW = 3, SpriteH = 4, SpriteId = 5, SpriteBillboard = 6, SpriteCutout = 7;
		/// <summary>Sprite on a given quad: the corners, the sprite id and the cutout flag.</summary>
		public const int SpriteQuadStride = 14, SpriteQuadId = 12, SpriteQuadCutout = 13;

		/// <summary>Doubles per primitive of <paramref name="kind"/> (0 for no kind).</summary>
		public static int Stride(int kind) => kind switch {
			0 => TexQuadStride, 1 => FlatQuadStride, 2 => TexTriStride, 3 => SpriteStride, 4 => SpriteQuadStride, _ => 0
		};
		/// <summary>Corners of a quad or triangle kind (0, 1, 2, 4).</summary>
		public static int Corners(int kind) => kind == 2 ? 3 : 4;
		/// <summary>Doubles from one corner to the next in a quad or triangle kind.</summary>
		public static int CornerStep(int kind) => kind == 2 ? 5 : 3;
		/// <summary>Where a primitive of a textured kind (0, 2) keeps its texture id; -1 for the other kinds.</summary>
		public static int TexAt(int kind) => kind == 0 ? TexQuadTex : kind == 2 ? TexTriTex : -1;
	}

	/// <summary>What a model's Pose wrote into an object: a later Pose with the same model, clip, time, yaw,
	/// mirror and part routing, while <see cref="Version"/> still matches the object, only moves it.</summary>
	internal sealed class PoseStamp {
		public object? Model;
		public int Anim;
		public double Time, Yaw;
		public bool Mirror;
		public int[]? Route;          // target object per part, or null when every part went to the posed object
		public int Version;           // the object's GeomVersion right after the Pose wrote it
		// the placement matrices of the object's own parts at that Pose (Mats[0..MatsLen)), MatsLen -1 when unknown: a later
		// Pose whose parts sit the same keeps the triangles even at another clip time
		public double[]? Mats;
		public int MatsLen = -1;
	}

	/// <summary>What the scene keeps for one texture or sprite id: its pixels and the settings that go with the id.
	/// An entry can hold settings before any pixels are registered.</summary>
	internal sealed class TexEntry {
		public int[]? Pix;            // packed pixels (textures RGB, a negative texel cut out; sprites ARGB); null = not registered
		public int W, H;
		public int Filter;            // GPU sampling filter: 0 nearest, 1 linear, 2 mipmap (textures only)
		public int Normal = -1;       // textures: the normal map's texture id (SetTextureNormalMap), -1 = none
		public int Rev;               // bumped whenever the pixels or the filter change; the GPU re-uploads its copy then
	}

	/// <summary>A point light. Used by the raytracer (inverse-square) and the rasterizer's
	/// optional forward lighting (smooth finite falloff when <see cref="Range"/> &gt; 0).</summary>
	internal struct SceneLight {
		public double X, Y, Z;
		public double R, G, B;        // colour * intensity
		public double Range;          // rasterizer: falloff radius (0 = inverse-square); raytracer ignores
	}

	/// <summary>Material for the raytracer (rasterizer ignores it).</summary>
	internal sealed class SceneMaterial {
		public int Type;              // 0 diffuse, 1 metal, 2 glass, 3 emissive
		public double R = 0.8, G = 0.8, B = 0.8;   // albedo
		public double Rough;          // 0 = sharp mirror/clear, 1 = fully rough
		public double Ior = 1.5;      // glass index of refraction
		public double ER, EG, EB;     // emission (already * strength)
		public int TexId = -1;        // optional albedo texture
		public int NormalMap;         // procedural normal: 0 none, 1 wood, 2 perlin, 3 waves
		public int NormalTex = -1;    // optional tangent-space normal-map texture (overrides procedural)
	}

	/// <summary>An analytic or SDF primitive (raytracer only).</summary>
	internal sealed class ScenePrimitive {
		// Kind: 0 sphere, 1 plane, 2 box (AABB), 3 torus, 4 SDF
		public int Kind;
		public double X, Y, Z;        // centre / plane point
		public double A, B, C;        // sphere: A=radius; plane: normal; box: half-extents; torus: A=R,B=r,C=axis(0/1/2); sdf: scale xyz
		public double MinX, MinY, MinZ, MaxX, MaxY, MaxZ;  // box bounds (Kind 2)
		public int SdfPreset;         // 0 sphere,1 roundbox,2 torus,3 capsule,4 gyroid
		public int Material = -1;
	}

	internal static class RenderUtil {
		public static byte CB(double v) => v <= 0 ? (byte)0 : (v >= 255 ? (byte)255 : (byte)v);
	}
}
