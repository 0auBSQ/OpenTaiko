using System;
using FDK;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	/// <summary>
	/// Lua-facing 2D graphics state: a TRUE scissor CLIP for UI drawing. Everything drawn between
	/// SetClip and ClearClip — textures, canvases, glyph text, skin module draws — is pixel-cut at
	/// the rectangle's edge (GL scissor test), which is what scrolling panels need: partial rows are
	/// clipped cleanly instead of popping/fading at the boundary.
	///
	///   GRAPHICS:SetClip(x, y, w, h)   -- LOGICAL screen coords (the 1920x1080 space Lua draws in)
	///   ... row/text/plate draws ...
	///   GRAPHICS:ClearClip()
	///
	/// Nested use: SetClip replaces the previous rectangle (no stack) — keep pairs balanced. The
	/// frame loop force-disables the scissor at frame start, so a missed ClearClip can never bleed
	/// into the next frame (it would still eat the REST of the current frame — always pair them).
	/// The logical→framebuffer mapping reads the CURRENT viewport, so render-scale and letterboxed
	/// window modes clip identically.
	/// </summary>
	public class LuaGraphicsFunc {
		public void SetClip(double x, double y, double w, double h) {
			var gl = Game.Gl;
			if (gl == null) return;
			Span<int> vp = stackalloc int[4];
			gl.GetInteger(GLEnum.Viewport, vp);
			double sx = (double)vp[2] / GameWindowSize.Width;
			double sy = (double)vp[3] / GameWindowSize.Height;
			int fx = vp[0] + (int)Math.Floor(x * sx);
			int fy = vp[1] + vp[3] - (int)Math.Ceiling((y + h) * sy);   // GL origin is bottom-left
			int fw = (int)Math.Ceiling(w * sx);
			int fh = (int)Math.Ceiling(h * sy);
			if (fw < 0) fw = 0;
			if (fh < 0) fh = 0;
			gl.Enable(EnableCap.ScissorTest);
			gl.Scissor(fx, fy, (uint)fw, (uint)fh);
		}

		public void ClearClip() {
			Game.Gl?.Disable(EnableCap.ScissorTest);
		}

#if DEBUG
		/// <summary>Next frame (2D + 3D) to Capture_img/&lt;name&gt;.png; false for a bad or taken name. Debug builds only.</summary>
		public bool SaveScreenPng(string name) {
			string? path = CapturePath(name);
			if (path == null) return false;
			Game.FrameGrab = (rgba, w, h) => WritePng(path, rgba, w, h);
			return true;
		}
#endif

		internal static string? CapturePath(string name) {
			if (string.IsNullOrEmpty(name) || name.Length > 64) return null;
			foreach (char c in name)
				if (!(char.IsAsciiLetterOrDigit(c) || c == '_' || c == '-')) return null;
			string path = Path.Combine(OpenTaiko.strEXEFolder, "Capture_img", name + ".png");
			return File.Exists(path) ? null : path;
		}

		// rgba: top-left origin; written opaque
		internal static bool WritePng(string path, byte[] rgba, int w, int h) {
			try {
				if (w <= 0 || h <= 0 || rgba.Length < w * h * 4) return false;
				Directory.CreateDirectory(Path.GetDirectoryName(path)!);
				var info = new SkiaSharp.SKImageInfo(w, h, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Opaque);
				using var image = SkiaSharp.SKImage.FromPixelCopy(info, rgba);
				using var data = image?.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
				if (data == null) return false;
				using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
				data.SaveTo(file);
				return true;
			} catch (Exception e) {
				System.Diagnostics.Trace.TraceWarning($"Capture to {path} failed: {e.Message}");
				return false;
			}
		}
	}
}
