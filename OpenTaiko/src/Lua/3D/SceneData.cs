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
		public bool HasNormal; public double Nx, Ny, Nz; // axis-aligned planar back-face cull
		public double[] Transform;    // null = identity (row-major 4x4 for models)
		public int Material = -1;     // raytracer material id (-1 = use a default); rasterizer ignores
		public double Dist;           // scratch: squared distance to camera (set each Render)
	}

	/// <summary>A point light (raytracer only).</summary>
	internal struct SceneLight {
		public double X, Y, Z;
		public double R, G, B;        // colour * intensity
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
