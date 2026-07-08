using System;

namespace OpenTaiko {
	// Shared scene data used by both renderers (Rasterizer + Raytracer). Lua3DScene owns the
	// collections; the renderers read them.

	/// <summary>A retained group of primitives (textured/flat quads, textured triangles).</summary>
	internal sealed class SceneObject {
		public double[] D = Array.Empty<double>();
		public int N;                 // primitive count
		public int Kind = -1;         // 0 tex-quad, 1 flat-quad, 2 tex-tri
		public bool Visible = true;
		public int Pass;              // 0 opaque (front->back), 1 transparent (back->front)
		public double R, G, B; public int A = 255;       // flat-quad colour
		public bool HasBounds;
		public double MinX, MinY, MinZ, MaxX, MaxY, MaxZ;
		public double CenX, CenY, CenZ, Radius;   // bounding sphere, precomputed by ObjSetBounds (cull hot path)
		public bool HasNormal; public double Nx, Ny, Nz; // axis-aligned planar back-face cull
		public double[] Transform;    // null = identity (row-major 4x4 for models)
		public int Material = -1;     // raytracer material id (-1 = use a default); rasterizer ignores
		public bool Lit = true;       // rasterizer forward lighting applies (false = render at shade only)
		public bool CastShadow = true; // billboard sprites in this object cast into the sun shadow map (false = UI markers/bubbles)
		public float DepthBias;        // polygon-offset units (GPU): <0 pulls toward camera (decals on top), >0 pushes away. 0 = off
		public double TintR = 1, TintG = 1, TintB = 1;   // per-channel colour multiply (unlit textured)
		public double FlatR = -1, FlatG, FlatB;          // flat albedo OVERRIDE (replaces material colour before lighting; FlatR<0 = off) — recolours one GLB part
		public double EmR, EmG, EmB;  // emissive add (GPU): col += base * emissive; values >1 feed the diorama bloom
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
		public int GeomVersion;       // bumped on ObjBegin; GPU rasterizer re-uploads this object's VBO only when it changes
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
