using System;

namespace OpenTaiko {
	/// <summary>
	/// A renderer for a <see cref="Lua3DScene"/>. Implementations read the scene's shared data
	/// (camera, objects, lights, primitives, textures, buffers) and write the colour buffer.
	/// The scene swaps between implementations via <c>SetMode</c>. A member with a body is what a renderer without
	/// that feature reports or does.
	/// </summary>
	internal interface IRenderer : IDisposable {
		/// <summary>Render the scene into its colour buffer (does not Upload).</summary>
		void Render(Lua3DScene scene);

		/// <summary>Drop any cached / accumulated state (called on mode switch or resize).</summary>
		void Invalidate();

		/// <summary>The renderer's name in failure reports, e.g. "GPU rasterizer".</summary>
		string Name { get; }

		/// <summary>What <see cref="Lua3DScene.GetMode"/> reports while this renderer is active: "raster",
		/// "raster_cpu" or "raytrace".</summary>
		string Mode { get; }

		/// <summary>Samples per pixel accumulated since the last reset (path tracers).</summary>
		int SampleCount => 0;

		/// <summary>The <see cref="Lua3DScene.GetRaytracerStatus"/> readout. <paramref name="fallbackNote"/> says why
		/// this renderer took over after another one failed ("" when it did not).</summary>
		string TracerStatus(string fallbackNote) => "";

		/// <summary>Cache counts for <see cref="Lua3DScene.GetRendererStats"/>.</summary>
		string Stats => "cpu";

		/// <summary>Whether this renderer draws the scene's sky shader (SetSkyShader).</summary>
		bool DrawsSkyShader => false;

		/// <summary>The last sky, post or surface shader compile error ("" when clean).</summary>
		string ShaderError => "";

		/// <summary>Forget this renderer's copy of texture <paramref name="id"/> (the scene unregistered it).</summary>
		void DropTexture(int id) { }

		/// <summary>Forget this renderer's copy of sprite <paramref name="id"/> (the scene unregistered it).</summary>
		void DropSprite(int id) { }

		/// <summary>Free every cache that rebuilds on demand (map unload).</summary>
		void ClearTransient() { }
	}

	/// <summary>A renderer that renders the scene from the current camera into a GPU target of its own for a texture
	/// id (mirrors, portals, water), which surfaces then sample in place of the id's registered pixels.</summary>
	internal interface IRenderTargets {
		/// <summary>Render the scene into texture <paramref name="texId"/>'s target of <paramref name="rw"/> x
		/// <paramref name="rh"/> pixels, cleared to (clr, clg, clb).</summary>
		void RenderToTexture(Lua3DScene scene, int texId, int rw, int rh, int clr, int clg, int clb);

		/// <summary>Delete the target bound to <paramref name="texId"/>, so the id's registered pixels show again.</summary>
		void DropRenderTarget(int texId);
	}

	/// <summary>2D drawing composited onto a canvas that a GPU renderer drew into, whose CPU colour buffer is stale.
	/// Coordinates are canvas pixels.</summary>
	internal interface ICanvasOverlay {
		void Rect2D(Lua3DScene scene, int x, int y, int w, int h, int r, int g, int b, int a);
		void Line2D(Lua3DScene scene, int x0, int y0, int x1, int y1, int r, int g, int b);
		/// <summary>Finish the 2D drawing: the caller's GL state comes back.</summary>
		void End2D();
	}
}
