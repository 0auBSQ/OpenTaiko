using System;
using System.Collections.Generic;

namespace OpenTaiko {
	/// <summary>
	/// A 3D render target for Lua stages, drawn like a <see cref="LuaTexture"/>/<see cref="LuaCanvas"/>.
	/// Owns a colour buffer (internal LuaCanvas) + a depth buffer + a camera, the retained scene
	/// data (objects, materials, lights, primitives, textures) and the active renderer.
	///
	/// Four renderers share this scene data behind <see cref="IRenderer"/>: the <see cref="GpuRasterizer"/>
	/// (the default when GLES 3.1 is available), the CPU <see cref="Rasterizer"/> (flat/textured,
	/// perspective-correct, near-plane clipping, multithreaded), and the <see cref="GpuRaytracer"/> and CPU
	/// <see cref="Raytracer"/> path tracers (global illumination, materials, analytic + SDF prims).
	/// Switch with <see cref="SetMode"/>; the rest of the API is renderer-agnostic.
	///
	/// Lua keeps the scene logic and submits world-space triangles/quads (and, for the raytracer,
	/// lights/materials/primitives); the heavy per-pixel work runs natively here.
	///
	/// The class is split by component over the Lua3DScene.*.cs files.
	/// </summary>
	public partial class Lua3DScene : IDisposable {
		internal LuaCanvas _canvas;
		internal HashSet<Lua3DScene>? _disposeList = null;

		internal int _w;   // PIXEL size (rasterise + project here); mutable: RenderView temporarily shrinks it for reduced-res reflections
		internal int _h;
		internal int _logicalW = 1, _logicalH = 1;   // LOGICAL size: what Lua requested / sees / the surface presents at (render-scale)
		internal float[] _depth;

		public Lua3DScene(int width, int height) {
			_logicalW = Math.Max(1, width);
			_logicalH = Math.Max(1, height);
			// Render-scale: rasterise into a reduced-resolution buffer (GPU win on low-end machines) but keep the
			// LOGICAL size for projection-readback + presentation, so Lua stages/characters need no changes.
			double rs = FDK.Game.RenderScale;
			int pw = Math.Max(1, (int)Math.Round(_logicalW * rs));
			int ph = Math.Max(1, (int)Math.Round(_logicalH * rs));
			_canvas = new LuaCanvas(pw, ph, _logicalW, _logicalH);
			_w = _canvas._w;
			_h = _canvas._h;
			_depth = new float[_w * _h];
			_scale = (_h * 0.5) / Math.Tan(_fov * 0.5 * Math.PI / 180.0);
			// GPU hardware-pipeline rasterizer is the DEFAULT when GLES 3.1 is available; the CPU
			// Rasterizer remains the automatic fallback (and for "raster_cpu" / raytrace).
			Made(_rasterizer);
			_renderer = FDK.Game.ComputeShadersAvailable ? (_gpuRasterizer = Made(new GpuRasterizer())) : _rasterizer;
		}

		public int Width => _logicalW;
		public int Height => _logicalH;

		private bool _disposedValue;
		public bool IsDisposed => _disposedValue;
		protected virtual void Dispose(bool disposing) {
			if (!_disposedValue) {
				TraceUntraced();
				foreach (var r in _made) r.Dispose();   // the GPU renderers free their GL resources (FBOs, buffers, textures)
				_canvas?.Dispose();
				foreach (var c in _rttCanvases.Values) c.Dispose();
				_rttCanvases.Clear(); _rttDepths.Clear(); _rttBufs.Clear();
				_rttCanvas?.Dispose(); _rttCanvas = null; _rttDepth = null;
				_disposeList?.Remove(this);
				_depth = Array.Empty<float>();
				_tex.Clear(); _sprites.Clear(); _reflectTrue.Clear();
				_materials.Clear(); _lights.Clear(); _primitives.Clear();
				_objects.Clear(); _particleSystems.Clear(); _grass.Clear(); _grass.TrimExcess();
				_disposedValue = true;
			}
		}
		public void Dispose() { Dispose(disposing: true); GC.SuppressFinalize(this); }
	}

	public class Lua3DSceneFunc {
		private HashSet<Lua3DScene> Scenes;
		public Lua3DSceneFunc(HashSet<Lua3DScene> scenes) { Scenes = scenes; }
		public Lua3DScene CreateScene(int width, int height) {
			Lua3DScene scene = new(width, height);
			scene._disposeList = Scenes;
			Scenes.Add(scene);
			return scene;
		}
	}
}
