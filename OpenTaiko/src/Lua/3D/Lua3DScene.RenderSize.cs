using System;

namespace OpenTaiko {
	// The 3D view's own render resolution, below the game's.
	public partial class Lua3DScene {
		private double _viewScale = 1.0;

		/// <summary>Render the 3D view at <paramref name="scale"/> (0.25 to 1) of its usual pixel size (the size the scene
		/// was made with, times the game's render scale) and present it at the same logical size, upscaled: fewer pixels to
		/// fill on a weak GPU. 2D the stage draws over the view stays sharp, and every coordinate Lua sees (WorldToScreen,
		/// the view rectangles) stays logical. Mirror and portal views follow the new size. 1 (the default) is the usual
		/// size; call it between frames.</summary>
		public void SetRenderScale(double scale) {
			if (_disposedValue || _rttPass) return;
			scale = scale <= 0 || double.IsNaN(scale) ? 1.0 : Math.Clamp(scale, 0.25, 1.0);
			_viewScale = scale;
			double rs = FDK.Game.RenderScale * scale;
			int pw = Math.Max(1, (int)Math.Round(_logicalW * rs)), ph = Math.Max(1, (int)Math.Round(_logicalH * rs));
			if (pw == _w && ph == _h) return;
			if (_gpuOwnsCanvas) _overlay2d?.End2D();   // 2D still open on the old canvas goes to it first
			var old = _canvas;
			_canvas = new LuaCanvas(pw, ph, _logicalW, _logicalH);
			_w = _canvas._w; _h = _canvas._h;
			_depth = new float[_w * _h];
			_scale = (_h * 0.5) / Math.Tan(_fov * 0.5 * Math.PI / 180.0);
			_gpuOwnsCanvas = false;
			_rtRevision++;
			old?.Dispose();
		}

		/// <summary>The scale <see cref="SetRenderScale"/> last set (1 by default).</summary>
		public double GetRenderScale() => _viewScale;

		/// <summary>The 3D view's pixel size now: width, height.</summary>
		public (int, int) GetRenderSize() => (_w, _h);

		/// <summary>What the GPU rasterizer's last frame drew: opaque objects drawn one by one, transparent objects, merged
		/// static buckets, particles, and the objects and vertices re-uploaded because their geometry changed (posed
		/// models, rebuilt surfaces). All 0 with another renderer.</summary>
		public (int, int, int, int, int, int) GetFrameStats()
			=> _renderer is GpuRasterizer g ? g.FrameStats : (0, 0, 0, 0, 0, 0);
	}
}
