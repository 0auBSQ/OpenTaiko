using System;
using System.Collections.Generic;
using System.Numerics;
using FDK;
using Silk.NET.OpenGLES;
using SkiaSharp;

namespace OpenTaiko.AviUtl2 {
	// A GL texture the renderer may read: an image file, a video frame.
	internal struct Aup2Texture {
		public uint Id;
		public int W, H;
		public bool Straight;                   // colours are not premultiplied
		public bool Ok => Id != 0 && W > 0 && H > 0;
	}

	// Where the pictures of a project come from; the Lua object owns the files.
	internal interface IAup2Sources {
		// objectId: the object asking, 0 when no object is (a script naming a file)
		Aup2Texture Image(string storedPath, int objectId = 0);
		Aup2Texture Video(int objectId);
		Aup2Evaluator? SceneEvaluator(int index);
		// an image a script names, by a path from the script folder or the project's
		Aup2Texture ScriptImage(string path);
		// a model file's triangles, null when it cannot be read
		Aup2Mesh? Model(string storedPath);
	}

	// A model's triangles: position, facing and colour per corner (nine numbers).
	internal sealed class Aup2Mesh {
		public float[] Data = Array.Empty<float>();
		public int Corners => Data.Length / 9;
		public bool Coloured;   // a material gave it colours
		public uint Vao, Vbo;   // made by the drawer on first use
	}

	// the frame a scene change starts from, kept while the change plays
	internal sealed class Aup2Held {
		public Aup2Surface Surface = null!;
		public long LastUse;
	}

	// Draws the frames of a scene: every object into the frame surface, through its effects, with its
	// blend mode and, under a camera control, in depth order.
	internal sealed class Aup2Renderer : IAup2Host, IDisposable {
		public Aup2Gpu Gpu { get; }
		Aup2Gpu? IAup2Host.Gpu => Gpu;

		private readonly IAup2Sources _sources;
		private readonly bool _ownsGpu;
		private readonly int _depth;
		private readonly Aup2Renderer _root;      // the renderer that keeps the text pictures
		private Aup2Scene _scene = null!;
		private Vector4? _asked;                // the background the caller asked for
		private Aup2Surface? _frame;
		private Aup2Surface? _previous;         // the last object's picture, for 直前オブジェクト
		private bool _keepPrevious;

		private Aup2Evaluator _evaluator = null!;
		private readonly Dictionary<int, Aup2Held> _held = new();
		private long _frameTick;
		private Aup2Camera? _camera;
		private readonly List<Pending> _pending = new();
		private readonly List<Aup2Light> _lights = new();

		private sealed class Pending {
			public Aup2Item Item = null!;
			public Aup2Surface? Surface;        // owned until drawn
			public Aup2Texture Direct;
			public Aup2Mesh? Mesh;
			public double Depth;
		}

		public Aup2Renderer(IAup2Sources sources, Aup2Renderer? parent = null) {
			_sources = sources;
			Gpu = parent?.Gpu ?? new Aup2Gpu();
			_ownsGpu = parent == null;
			_depth = parent == null ? 0 : parent._depth + 1;
			_root = parent?._root ?? this;
		}

		#region Text pictures
		private sealed class TextEntry {
			public uint Tex;
			public int W, H;
			public List<SKRect> Letters = new();
			public double AnchorX, AnchorY;
			public long LastUse;
			public long Bytes => (long)W * H * 4;
		}

		private readonly Dictionary<string, TextEntry> _texts = new();
		private long _textBytes, _textTick;
		public long TextBudgetBytes = 64L * 1024 * 1024;
		public long TextBytes => _textBytes;

		private TextEntry TextPicture(Aup2TextStyle style) {
			if (_root != this) return _root.TextPicture(style);
			string key = Aup2TextRaster.Key(style, Aup2TextRaster.ShownCount(style));
			_textTick++;
			if (_texts.TryGetValue(key, out var e)) { e.LastUse = _textTick; return e; }
			using var image = Aup2TextRaster.Render(style);
			e = new TextEntry { W = image.Bitmap.Width, H = image.Bitmap.Height, AnchorX = image.AnchorX, AnchorY = image.AnchorY, LastUse = _textTick };
			e.Letters.AddRange(image.Letters);
			var gl = Game.Gl;
			e.Tex = gl.GenTexture();
			gl.BindTexture(TextureTarget.Texture2D, e.Tex);
			unsafe { gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)e.W, (uint)e.H, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (void*)image.Bitmap.GetPixels()); }
			gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
			gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
			gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
			gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
			_texts[key] = e;
			_textBytes += e.Bytes;
			// the least recently shown texts go first
			while (_textBytes > TextBudgetBytes && _texts.Count > 1) {
				string? oldest = null;
				long at = long.MaxValue;
				foreach (var kv in _texts)
					if (kv.Value != e && kv.Value.LastUse < at) { at = kv.Value.LastUse; oldest = kv.Key; }
				if (oldest == null) break;
				gl.DeleteTexture(_texts[oldest].Tex);
				_textBytes -= _texts[oldest].Bytes;
				_texts.Remove(oldest);
			}
			return e;
		}
		#endregion

		// The frame of `evaluator`'s scene at `frame`, as a surface the caller releases. Null when the
		// renderer could not start.
		public Aup2Surface? RenderFrame(Aup2Evaluator evaluator, double frame, Vector4? background, out Aup2Frame result, int layerLimit = int.MaxValue, int layerFrom = -1) {
			_scene = evaluator.Scene;
			_evaluator = evaluator;
			_frameTick++;
			_keepPrevious = false;
			foreach (var o in _scene.Objects)
				if (o.Effects.Count > 0 && o.Effects[0].Name == Aup2Evaluator.Previous) { _keepPrevious = true; break; }
			_frame = Gpu.Rent(_scene.Width, _scene.Height, depth: true);
			// no colour asked for: the scene's own, and without one the frame is see-through
			_asked = background;
			var bg = background ?? (_scene.Background is Vector3 own ? new Vector4(own, 1) : Vector4.Zero);
			Gpu.Clear(_frame, bg.X * bg.W, bg.Y * bg.W, bg.Z * bg.W, bg.W);
			result = evaluator.Evaluate(frame, this, layerLimit, layerFrom);
			Gpu.Release(_previous);
			_previous = null;
			EndScriptFrame();
			// a held picture whose scene change is over goes back to the pool
			if (_held.Count > 0) {
				List<int>? over = null;
				foreach (var kv in _held)
					if (kv.Value.LastUse != _frameTick) (over ??= new()).Add(kv.Key);
				if (over != null)
					foreach (int id in over) { Gpu.Release(_held[id].Surface); _held.Remove(id); }
			}
			var done = _frame;
			_frame = null;
			return done;
		}

		#region IAup2Host
		public Aup2Surface? FrameCopy() {
			if (_frame == null) return null;
			Flush();   // what the camera still holds is part of the frame so far
			return Gpu.Copy(_frame);
		}

		public void ReplaceFrame(Aup2Surface s) {
			if (_frame == null) return;
			Gpu.Pass(_frame, "void main(){ o = texture(u_src, v_uv); }", s);
		}

		public void BeginCamera(Aup2Camera camera) {
			Flush();
			_lights.Clear();
			_camera = camera;
			if (_frame != null) Gpu.ClearDepth(_frame);   // each camera starts with nothing in front
		}

		public void AddLight(Aup2Light light) {
			if (_camera != null && _lights.Count < 10) _lights.Add(light);
		}

		public Aup2Camera? CurrentCamera => _camera;

		public void EndCamera() {
			Flush();
			_lights.Clear();
			_camera = null;
		}

		private const string Import = @"uniform vec4 u_rect; uniform int u_straight; uniform vec2 u_flip;
void main(){
    vec2 q = mix(v_uv, 1.0 - v_uv, u_flip);
    o = texture(u_src, u_rect.xy + q * u_rect.zw);
    if (u_straight == 1) o.rgb *= o.a;
}";

		public bool Materialize(Aup2Context c) {
			var item = c.Item;
			switch (item.Kind) {
				case Aup2ItemKind.Image:
				case Aup2ItemKind.Video: {
					var tex = item.Kind == Aup2ItemKind.Image ? _sources.Image(item.Path, item.ObjectId) : _sources.Video(item.ObjectId);
					if (!tex.Ok) { c.Skip = true; return false; }
					double cx = Math.Clamp(item.CropLeft, 0, tex.W), cy = Math.Clamp(item.CropTop, 0, tex.H);
					double cw = tex.W - cx - Math.Clamp(item.CropRight, 0, tex.W), ch = tex.H - cy - Math.Clamp(item.CropBottom, 0, tex.H);
					if (cw < 1 || ch < 1) { c.Skip = true; return false; }
					var s = Gpu.Rent((int)Math.Round(cw), (int)Math.Round(ch));
					Gpu.Pass(s, Import, tex.Id, tex.W, tex.H, new Aup2Uniforms()
						.V4("u_rect", cx / tex.W, cy / tex.H, cw / tex.W, ch / tex.H).I("u_straight", tex.Straight ? 1 : 0)
						.V2("u_flip", item.FlipX ? 1 : 0, item.FlipY ? 1 : 0));
					// the crop and the flips are in the picture now
					if (!item.Recentre) {
						double ox = cx + cw / 2 - tex.W / 2.0, oy = cy + ch / 2 - tex.H / 2.0;
						item.OffsetX += item.FlipX ? -ox : ox;
						item.OffsetY += item.FlipY ? -oy : oy;
					}
					item.CropLeft = item.CropTop = item.CropRight = item.CropBottom = 0;
					item.Recentre = false; item.FlipX = item.FlipY = false;
					c.Surface = s;
					return true;
				}
				case Aup2ItemKind.Shape: {
					int w = (int)Math.Ceiling(item.ShapeWidth), h = (int)Math.Ceiling(item.ShapeHeight);
					if (w < 1 || h < 1) { c.Skip = true; return false; }
					var s = Gpu.Rent(w, h);
					Gpu.Pass(s, Aup2Shapes.Fragment, 0, 1, 1, Aup2Shapes.Uniforms(item));
					c.Surface = s;
					return true;
				}
				case Aup2ItemKind.Text: {
					if (item.Text == null) { c.Skip = true; return false; }
					var e = TextPicture(item.Text);
					var s = Gpu.Rent(e.W, e.H);
					Gpu.Pass(s, "void main(){ o = texture(u_src, v_uv); }", e.Tex, e.W, e.H);
					item.OffsetX += e.W / 2.0 - e.AnchorX;
					item.OffsetY += e.H / 2.0 - e.AnchorY;
					if (item.Text.PerChar && e.Letters.Count > 0) {
						item.Parts = new List<Aup2Part>();
						for (int i = 0; i < e.Letters.Count; i++) {
							var r = e.Letters[i];
							item.Parts.Add(new Aup2Part { Index = i, Count = e.Letters.Count, RectX = r.Left, RectY = r.Top, RectW = r.Width, RectH = r.Height });
						}
					}
					c.Surface = s;
					return true;
				}
				case Aup2ItemKind.FrameBuffer:
				case Aup2ItemKind.Filter:
					c.Surface = FrameCopy();
					return c.Surface != null;
				case Aup2ItemKind.SceneChange: {
					if (_frame == null) return false;
					if (!_held.TryGetValue(item.ObjectId, out var held)) {
						// the layers under the change, one frame before it starts, drawn once
						if (_depth >= 4) { c.Skip = true; return false; }
						using var inner = new Aup2Renderer(_sources, this);
						var before = inner.RenderFrame(_evaluator, Math.Max(0, item.SceneFrame), _asked, out _, item.Layer);
						if (before == null) { c.Skip = true; return false; }
						held = new Aup2Held { Surface = before };
						_held[item.ObjectId] = held;
					}
					held.LastUse = _frameTick;
					if (Aup2SceneChanges.Kinds.TryGetValue(item.ChangeKind, out var change)) {
						var at = c.Effect;
						c.Effect = c.Object.Effects[0];
						c.Surface = change(c, held.Surface, _frame, item.ChangeReverse ? 1 - item.ChangeProgress : item.ChangeProgress);
						c.Effect = at;
						if (c.Surface != null) return true;
					}
					c.Surface = Gpu.Rent(_frame.W, _frame.H);
					Gpu.Pass(c.Surface, ChangeFragment, _frame, new Aup2Uniforms().Tex("u_old", held.Surface)
						.F("u_p", item.ChangeReverse ? 1 - item.ChangeProgress : item.ChangeProgress).I("u_kind", ChangeKindOf(item.ChangeKind)).I("u_rev", item.ChangeReverse ? 1 : 0));
					return true;
				}
				case Aup2ItemKind.Compose: {
					// the layers it gathers, drawn by themselves into one picture
					if (_depth >= 4) { c.Skip = true; return false; }
					using var inner = new Aup2Renderer(_sources, this);
					c.Surface = inner.RenderFrame(_evaluator, item.SceneFrame, Vector4.Zero, out _, item.ComposeTo == int.MaxValue ? int.MaxValue : item.ComposeTo + 1, item.Layer);
					return c.Surface != null;
				}
				case Aup2ItemKind.Model:
					// a model has no flat picture: pixel effects leave it as it is
					return false;
				case Aup2ItemKind.Previous:
					if (_previous == null) { c.Skip = true; return false; }
					c.Surface = Gpu.Copy(_previous);
					return true;
				case Aup2ItemKind.Scene: {
					var ev = _depth < 4 ? _sources.SceneEvaluator(item.SceneIndex) : null;
					if (ev == null || ev.Scene == _scene) { c.Skip = true; return false; }
					using var inner = new Aup2Renderer(_sources, this);
					c.Surface = inner.RenderFrame(ev, item.SceneFrame, null, out _);
					return c.Surface != null;
				}
			}
			c.Skip = true;
			return false;
		}

		public void Compose(Aup2Context c) {
			var item = c.Item;
			if (_frame == null) return;
			if (item.Kind == Aup2ItemKind.SceneChange) {
				if (c.Pixels() && c.Surface != null) ReplaceFrame(c.Surface);
				return;
			}
			if (item.Kind == Aup2ItemKind.Filter && item.Masked && c.Surface != null) {
				// the filtered frame shows inside the shape only
				int mw = Math.Max(1, (int)Math.Ceiling(item.ShapeWidth)), mh = Math.Max(1, (int)Math.Ceiling(item.ShapeHeight));
				var shape = Gpu.Rent(Math.Min(mw, Aup2Gpu.MaxSide), Math.Min(mh, Aup2Gpu.MaxSide));
				Gpu.Pass(shape, Aup2Shapes.Fragment, 0, 1, 1, Aup2Shapes.Uniforms(item));
				var mixed = Gpu.Rent(_frame.W, _frame.H);
				Gpu.Pass(mixed, @"uniform sampler2D u_new; uniform sampler2D u_mask; uniform vec2 u_newSize; uniform vec2 u_newOff;
uniform vec2 u_maskSize; uniform vec2 u_maskCentre; uniform float u_blur; uniform int u_inv;
void main(){
    vec2 pos = v_uv * u_size;
    vec4 was = texture(u_src, v_uv);
    vec4 now = texture(u_new, (pos + u_newOff) / u_newSize);
    vec2 q = (pos - u_maskCentre) / u_maskSize + 0.5;
    float m = 0.0;
    if (u_blur < 0.5) {
        m = (q.x < 0.0 || q.y < 0.0 || q.x > 1.0 || q.y > 1.0) ? 0.0 : texture(u_mask, q).a;
    } else {
        // a soft edge: the shape read around the pixel
        for (int j = -2; j <= 2; j++) for (int i = -2; i <= 2; i++) {
            vec2 r = q + vec2(float(i), float(j)) * u_blur * 0.5 / u_maskSize;
            m += (r.x < 0.0 || r.y < 0.0 || r.x > 1.0 || r.y > 1.0) ? 0.0 : texture(u_mask, r).a;
        }
        m /= 25.0;
    }
    if (u_inv == 1) m = 1.0 - m;
    o = mix(was, now, m);
}", _frame, new Aup2Uniforms().Tex("u_new", c.Surface).Tex("u_mask", shape).V2("u_newSize", c.Surface.W, c.Surface.H)
					.V2("u_newOff", (c.Surface.W - _frame.W) / 2.0 - item.OffsetX, (c.Surface.H - _frame.H) / 2.0 - item.OffsetY)
					.V2("u_maskSize", shape.W, shape.H).V2("u_maskCentre", _frame.W / 2.0 + item.MaskX, _frame.H / 2.0 + item.MaskY)
					.F("u_blur", item.MaskBlur).I("u_inv", item.MaskInvert ? 1 : 0));
				ReplaceFrame(mixed);
				Gpu.Release(mixed);
				Gpu.Release(shape);
				return;
			}
			if (item.Kind == Aup2ItemKind.Filter && c.Surface != null && Moved(item)) {
				// effects that move, turn, scale, fade or split: the frame so far is drawn again as one picture
				Gpu.Clear(_frame);
				Draw(item, c.Surface, default, _frame, false);
				return;
			}
			if (item.Kind == Aup2ItemKind.Filter) {
				// the filtered frame goes back at its own size: what grew past the frame is cut
				if (c.Surface != null)
					Gpu.Pass(_frame, "uniform vec2 u_off; void main(){ o = texture(u_src, (v_uv * u_size + u_off) / u_srcSize); }", c.Surface,
						new Aup2Uniforms().V2("u_off", (c.Surface.W - _frame.W) / 2.0 - item.OffsetX, (c.Surface.H - _frame.H) / 2.0 - item.OffsetY));
				return;
			}
			if (item.Kind == Aup2ItemKind.Model && c.Surface == null) {
				var mesh = _sources.Model(item.Path);
				if (mesh == null || mesh.Corners == 0) return;
				if (_camera != null) {
					var at = Vector3.Transform(new Vector3((float)item.X, (float)item.Y, (float)item.Z), View(_camera));
					_pending.Add(new Pending { Item = item, Mesh = mesh, Depth = at.Z });
				} else {
					DrawMesh(item, mesh);
				}
				return;
			}
			var direct = default(Aup2Texture);
			if (c.Surface == null) {
				// an image or a video with no pixel effect is drawn straight from its texture
				if (item.Kind == Aup2ItemKind.Image) direct = _sources.Image(item.Path, item.ObjectId);
				else if (item.Kind == Aup2ItemKind.Video) direct = _sources.Video(item.ObjectId);
				else if (!c.Pixels()) return;
				if (c.Surface == null && !direct.Ok) return;
			}
			if (_keepPrevious && c.Surface != null) {
				Gpu.Release(_previous);
				_previous = Gpu.Copy(c.Surface);
			}
			if (_camera != null) {
				var p = new Pending { Item = item, Surface = c.Surface, Direct = direct };
				c.Surface = null;   // kept until the camera's objects are drawn, far ones first
				var world = Vector3.Transform(new Vector3((float)item.X, (float)item.Y, (float)item.Z), View(_camera));
				p.Depth = world.Z;
				_pending.Add(p);
				return;
			}
			Draw(item, c.Surface, direct);
		}

		private static bool Moved(Aup2Item i) =>
			i.X != 0 || i.Y != 0 || i.Z != 0 || i.ScaleX != 1 || i.ScaleY != 1 || i.RotX != 0 || i.RotY != 0 || i.RotZ != 0
			|| i.Opacity < 1 || i.Parts != null || i.PivotX != 0 || i.PivotY != 0 || i.FlipX || i.FlipY;

		private void Flush() {
			if (_pending.Count == 0) return;
			_pending.Sort((a, b) => b.Depth.CompareTo(a.Depth));
			foreach (var p in _pending) {
				if (p.Mesh != null) { DrawMesh(p.Item, p.Mesh); continue; }
				if (_camera != null && _camera.Blur > 0) Defocus(p, _camera);
				Draw(p.Item, p.Surface, p.Direct);
				Gpu.Release(p.Surface);
			}
			_pending.Clear();
		}

		// a model's own units shown at this many pixels
		private const float ModelUnit = 100;
		private static readonly List<Aup2Light> NoLights = new();

		private void DrawMesh(Aup2Item item, Aup2Mesh mesh) {
			if (_frame == null) return;
			var cam = item.UnderCamera ? _camera : null;
			if (cam == null) Gpu.ClearDepth(_frame);
			// a model's up is the screen's up; adjusting also turns its front round
			float depth = item.ModelAdjust ? -1 : 1;
			float sx = (float)item.ScaleX, sy = (float)item.ScaleY, sz = Math.Max(Math.Abs(sx), Math.Abs(sy));
			var model = Matrix4x4.CreateScale(ModelUnit, -ModelUnit, ModelUnit * depth)
				* Matrix4x4.CreateTranslation((float)-item.PivotX, (float)-item.PivotY, (float)-item.PivotZ)
				* Matrix4x4.CreateScale(sx, sy, sz)
				* Aup2Evaluator.Turn(item.RotX, item.RotY, item.RotZ)
				* Matrix4x4.CreateTranslation((float)item.X, (float)item.Y, (float)item.Z);
			Gpu.Mesh(_frame, mesh, model, View(cam) * Projection(cam, _frame.W, _frame.H), cam?.Eye ?? new Vector3(0, 0, -1024), item.Opacity, cam != null ? _lights : NoLights);
		}

		private const string FsDefocus = @"uniform vec2 u_dir; uniform float u_r; uniform vec2 u_pad;
void main(){
    vec4 s = vec4(0.0); float sum = 0.0;
    for (int i = -12; i <= 12; i++) {
        float t = float(i) / 12.0;
        float w = exp(-4.5 * t * t);
        vec2 p = (v_uv * u_size - u_pad + u_dir * t * u_r * 2.0) / u_srcSize;
        if (p.x >= 0.0 && p.y >= 0.0 && p.x <= 1.0 && p.y <= 1.0) s += texture(u_src, p) * w;
        sum += w;
    }
    o = s / sum;
}";

		// depth blur: a picture is blurred by how far it stands from the distance in focus
		private void Defocus(Pending p, Aup2Camera cam) {
			var item = p.Item;
			if (item.Parts != null) return;
			double focus = Math.Max(1, cam.FocusDistance);
			double r = Math.Min(64, cam.Blur * Math.Abs(p.Depth - focus) / focus * 0.25);
			if (r < 0.5) return;
			var src = p.Surface;
			if (src == null) {
				if (!p.Direct.Ok || item.CropLeft != 0 || item.CropTop != 0 || item.CropRight != 0 || item.CropBottom != 0 || item.FlipX || item.FlipY) return;
				src = Gpu.Rent(p.Direct.W, p.Direct.H);
				Gpu.Pass(src, Import, p.Direct.Id, p.Direct.W, p.Direct.H,
					new Aup2Uniforms().V4("u_rect", 0, 0, 1, 1).I("u_straight", p.Direct.Straight ? 1 : 0).V2("u_flip", 0, 0));
				p.Direct = default;
			}
			int pad = (int)Math.Ceiling(r * 2);
			int w = Math.Min(Aup2Gpu.MaxSide, src.W + pad * 2), h = Math.Min(Aup2Gpu.MaxSide, src.H + pad * 2);
			var a = Gpu.Rent(w, h);
			Gpu.Pass(a, FsDefocus, src, new Aup2Uniforms().V2("u_dir", 1, 0).F("u_r", r).V2("u_pad", (w - src.W) / 2.0, (h - src.H) / 2.0));
			var b = Gpu.Rent(w, h);
			Gpu.Pass(b, FsDefocus, a, new Aup2Uniforms().V2("u_dir", 0, 1).F("u_r", r).V2("u_pad", 0, 0));
			Gpu.Release(a);
			Gpu.Release(src);
			p.Surface = b;
		}

		// what the camera's lights make of a flat picture standing at `at` and facing `normal`
		private Vector3 Shade(Vector3 at, Vector3 normal) {
			var sum = Vector3.Zero;
			foreach (var l in _lights) {
				double k = l.Brightness;
				if (l.Kind != Aup2LightKind.Ambient) {
					Vector3 from;
					if (l.Kind == Aup2LightKind.Parallel) from = l.Direction;
					else {
						var d = l.Position - at;
						float dist = d.Length();
						from = dist > 1e-4f ? d / dist : -normal;
						k /= 1 + l.Falloff / 100 * (dist / 1024) * (dist / 1024);
						if (l.Kind == Aup2LightKind.Spot) {
							var axis = l.Target - l.Position;
							if (axis.LengthSquared() > 1e-6f) {
								double angle = Math.Acos(Math.Clamp(Vector3.Dot(Vector3.Normalize(axis), -from), -1, 1)) * 180 / Math.PI;
								double half = Math.Max(0.1, l.Range / 2), soft = Math.Max(0.01, half * Math.Min(1, l.Soft / 100));
								k *= Math.Clamp((half - angle) / soft, 0, 1);
							}
						}
					}
					// a flat picture is lit from either side
					k *= Math.Abs(Vector3.Dot(normal, from));
				}
				sum += l.Color * (float)k;
			}
			return Vector3.Min(sum, new Vector3(4));
		}
		#endregion

		#region What scripts ask for
		private Aup2Surface? _temp;             // the temporary buffer every object shares within a frame
		private readonly Dictionary<string, Aup2Surface> _caches = new();

		private void EndScriptFrame() {
			Gpu.Release(_temp);
			_temp = null;
			foreach (var s in _caches.Values) Gpu.Release(s);
			_caches.Clear();
		}

		public void SetDrawTarget(Aup2Context c, bool temp, int w, int h) {
			c.Flat = temp;
			if (!temp || w <= 0 || h <= 0) return;
			Gpu.Release(_temp);
			_temp = Gpu.Rent(w, h);
			Gpu.Clear(_temp);
		}

		public void ScriptDraw(Aup2Context c, double ox, double oy, double oz, double zoom, double alpha, double rx, double ry, double rz) {
			if (!c.Pixels() || c.Surface == null) return;
			var src = c.Item;
			Aup2Item item;
			if (c.Flat) {
				if (_temp == null) return;
				item = new Aup2Item { Kind = src.Kind, X = ox, Y = oy, Z = oz, ScaleX = zoom, ScaleY = zoom, RotX = rx, RotY = ry, RotZ = rz, Opacity = alpha, Blend = src.Blend };
				Draw(item, c.Surface, default, _temp, false);
				return;
			}
			if (_frame == null) return;
			c.Drawn = true;
			item = new Aup2Item {
				Kind = src.Kind, ObjectId = src.ObjectId, Layer = src.Layer, UnderCamera = src.UnderCamera, Billboard = src.Billboard, CullBack = src.CullBack,
				X = src.X + ox, Y = src.Y + oy, Z = src.Z + oz, PivotX = src.PivotX, PivotY = src.PivotY, PivotZ = src.PivotZ,
				ScaleX = src.ScaleX * zoom, ScaleY = src.ScaleY * zoom, RotX = src.RotX + rx, RotY = src.RotY + ry, RotZ = src.RotZ + rz,
				Opacity = src.Opacity * alpha, Blend = src.Blend, OffsetX = src.OffsetX, OffsetY = src.OffsetY, Parts = src.Parts,
			};
			Draw(item, c.Surface, default, _frame, src.UnderCamera && _camera != null);
		}

		public void ScriptPoly(Aup2Context c, ReadOnlySpan<Vector3> corners, ReadOnlySpan<Vector2> uv, double alpha) {
			int n = Math.Min(corners.Length, 4);
			if (n < 3 || uv.Length < n || !c.Pixels() || c.Surface == null) return;
			var item = c.Item;
			var target = c.Flat ? _temp : _frame;
			if (target == null) return;
			Matrix4x4 m;
			bool depth = false;
			if (c.Flat) {
				m = View(null) * Projection(null, target.W, target.H);
			} else {
				c.Drawn = true;
				var cam = item.UnderCamera ? _camera : null;
				depth = cam != null;
				m = Matrix4x4.CreateScale((float)item.ScaleX, (float)item.ScaleY, 1)
					* Aup2Evaluator.Turn(item.RotX, item.RotY, item.RotZ)
					* Matrix4x4.CreateTranslation((float)item.X, (float)item.Y, (float)item.Z)
					* View(cam) * Projection(cam, target.W, target.H);
			}
			Span<Vector4> clip = stackalloc Vector4[4];
			Span<Vector2> t = stackalloc Vector2[4];
			for (int i = 0; i < n; i++) {
				clip[i] = Vector4.Transform(new Vector4(corners[i], 1), m);
				t[i] = new Vector2(uv[i].X / c.Surface.W, uv[i].Y / c.Surface.H);
			}
			Aup2Surface? backdrop = (int)item.Blend >= 10 ? Gpu.Copy(target) : null;
			Gpu.Poly(target, c.Surface.Tex, clip[..n], t[..n], (c.Flat ? 1 : item.Opacity) * alpha, item.Blend, false, backdrop, depth, c.CullBack);
			Gpu.Release(backdrop);
		}

		private Aup2Surface? Named(Aup2Context c, string name, bool forWrite, int w, int h) {
			if (name == "object" || name == "obj") {
				if (forWrite) { c.Replace(Gpu.Rent(w, h)); if (c.Surface == null) c.Surface = Gpu.Rent(w, h); return c.Surface; }
				return c.Pixels() ? c.Surface : null;
			}
			if (name == "tempbuffer" || name == "tmp") {
				if (forWrite) { Gpu.Release(_temp); _temp = Gpu.Rent(w, h); }
				return _temp;
			}
			if (name == "framebuffer" || name == "frm") return _frame;
			if (name.StartsWith("cache:", StringComparison.Ordinal)) {
				if (forWrite) {
					if (_caches.TryGetValue(name, out var old)) Gpu.Release(old);
					_caches[name] = Gpu.Rent(w, h);
				}
				return _caches.GetValueOrDefault(name);
			}
			return null;
		}

		public bool CopyBuffer(Aup2Context c, string dst, string src) {
			uint tex; int w, h; bool straight = false;
			if (src.StartsWith("image:", StringComparison.Ordinal)) {
				var image = _sources.ScriptImage(src.Substring(6));
				if (!image.Ok) return false;
				tex = image.Id; w = image.W; h = image.H; straight = image.Straight;
			} else {
				var from = Named(c, src, false, 0, 0);
				if (from == null) return false;
				// the source may be the destination's old surface: read from a copy
				from = Gpu.Copy(from);
				tex = from.Tex; w = from.W; h = from.H;
				var to2 = dst == "framebuffer" || dst == "frm" ? _frame : Named(c, dst, true, w, h);
				if (to2 != null) Gpu.Pass(to2, "void main(){ o = texture(u_src, v_uv); }", tex, w, h);
				Gpu.Release(from);
				return to2 != null;
			}
			var to = dst == "framebuffer" || dst == "frm" ? _frame : Named(c, dst, true, w, h);
			if (to == null) return false;
			Gpu.Pass(to, "uniform int u_straight; void main(){ o = texture(u_src, v_uv); if (u_straight == 1) o.rgb *= o.a; }", tex, w, h,
				new Aup2Uniforms().I("u_straight", straight ? 1 : 0));
			return true;
		}

		private bool Reload(Aup2Context c, Action<Aup2Item> fill) {
			var keep = c.Item;
			var probe = new Aup2Item { ObjectId = keep.ObjectId, Layer = keep.Layer };
			fill(probe);
			var saved = c.Surface;
			c.Surface = null;
			c.Item = probe;
			bool skipWas = c.Skip;
			bool ok = Materialize(c) && c.Surface != null;
			var made = c.Surface;
			c.Item = keep;
			c.Skip = skipWas;
			c.Surface = saved;
			if (!ok || made == null) return false;
			// the new picture takes the old one's place; the object keeps where it stands
			Gpu.Release(c.Surface);
			c.Surface = made;
			keep.Parts = null;
			keep.OffsetX = probe.OffsetX; keep.OffsetY = probe.OffsetY;
			keep.CropLeft = keep.CropTop = keep.CropRight = keep.CropBottom = 0;
			keep.Kind = probe.Kind == Aup2ItemKind.Image ? Aup2ItemKind.Shape : probe.Kind;
			if (keep.Kind == Aup2ItemKind.Shape) { keep.ShapeKind = "背景"; keep.ShapeWidth = made.W; keep.ShapeHeight = made.H; }
			keep.Text = probe.Text;
			return true;
		}

		public bool LoadFigure(Aup2Context c, string kind, int rgb, double size, double line) {
			if (!Aup2Shapes.Knows(kind)) return false;
			return Reload(c, it => {
				it.Kind = Aup2ItemKind.Shape; it.ShapeKind = kind;
				it.ShapeWidth = kind == "背景" ? _scene.Width : size; it.ShapeHeight = kind == "背景" ? _scene.Height : size;
				it.ShapeLine = line <= 0 ? 4000 : line; it.Path = "0";
				it.R = (rgb >> 16) & 255; it.G = (rgb >> 8) & 255; it.B = rgb & 255;
			});
		}

		public bool LoadText(Aup2Context c, Aup2TextStyle style) => Reload(c, it => { it.Kind = Aup2ItemKind.Text; it.Text = style; });

		public bool LoadImage(Aup2Context c, string path) {
			var image = _sources.ScriptImage(path);
			if (!image.Ok) return false;
			var s = Gpu.Rent(image.W, image.H);
			Gpu.Pass(s, "uniform int u_straight; void main(){ o = texture(u_src, v_uv); if (u_straight == 1) o.rgb *= o.a; }", image.Id, image.W, image.H,
				new Aup2Uniforms().I("u_straight", image.Straight ? 1 : 0));
			Gpu.Release(c.Surface);
			c.Surface = s;
			c.Item.Parts = null;
			c.Item.OffsetX = c.Item.OffsetY = 0;
			return true;
		}

		public byte[]? ReadPixels(Aup2Context c, out int w, out int h) {
			w = h = 0;
			if (!c.Pixels() || c.Surface == null) return null;
			w = c.Surface.W; h = c.Surface.H;
			return Gpu.Read(c.Surface);
		}

		public void WritePixels(Aup2Context c, byte[] rgba, int w, int h) {
			if (w <= 0 || h <= 0 || rgba.Length < w * h * 4) return;
			if (c.Surface == null || c.Surface.W != w || c.Surface.H != h) {
				var s = Gpu.Rent(w, h);
				if (c.Surface == null) c.Surface = s; else c.Replace(s);
			}
			Gpu.Write(c.Surface!, rgba);
		}
		#endregion

		#region Scene changes
		private static int ChangeKindOf(string name) {
			if (name.Contains("ワイプ(円)")) return 1;
			if (name.Contains("ワイプ(四角)")) return 2;
			if (name.Contains("ワイプ(時計)")) return 3;
			if (name.Contains("ワイプ(横)")) return 4;
			if (name.Contains("ワイプ(縦)")) return 5;
			if (name.Contains("スライド") || name.Contains("押し出し(横)")) return 6;
			if (name.Contains("押し出し(縦)")) return 7;
			if (name.Contains("フェードアウトイン")) return 8;
			if (name.Contains("縮小")) return 9;
			return 0;   // a cross fade, also for the kinds this player has no drawing for
		}

		// u_src: the new picture; u_old: the one the change starts from; u_p: 0 old .. 1 new
		private const string ChangeFragment = @"uniform sampler2D u_old; uniform float u_p; uniform int u_kind; uniform int u_rev;
void main(){
    vec4 a = texture(u_old, v_uv), b = texture(u_src, v_uv);
    if (u_rev == 1) { vec4 t = a; a = b; b = t; }
    vec2 q = (v_uv - 0.5) * u_size;
    float m = u_p;
    if (u_kind == 1) m = step(length(q), u_p * length(u_size) * 0.5);
    else if (u_kind == 2) { vec2 e = abs(q) / (u_size * 0.5); m = step(max(e.x, e.y), u_p); }
    else if (u_kind == 3) { float an = atan(q.x, -q.y) / 6.2831853; if (an < 0.0) an += 1.0; m = step(an, u_p); }
    else if (u_kind == 4) m = step(v_uv.x, u_p);
    else if (u_kind == 5) m = step(v_uv.y, u_p);
    else if (u_kind == 6) {
        // the new picture pushes the old one out to the left
        vec2 ua = v_uv + vec2(u_p, 0.0), ub = v_uv + vec2(u_p - 1.0, 0.0);
        o = ua.x <= 1.0 ? texture(u_old, ua) : texture(u_src, ub);
        if (u_rev == 1) o = ua.x <= 1.0 ? texture(u_src, ua) : texture(u_old, ub);
        return;
    } else if (u_kind == 7) {
        vec2 ua = v_uv + vec2(0.0, u_p), ub = v_uv + vec2(0.0, u_p - 1.0);
        o = ua.y <= 1.0 ? texture(u_old, ua) : texture(u_src, ub);
        if (u_rev == 1) o = ua.y <= 1.0 ? texture(u_src, ua) : texture(u_old, ub);
        return;
    } else if (u_kind == 8) {
        // out to black, then in
        o = u_p < 0.5 ? a * (1.0 - u_p * 2.0) : b * (u_p * 2.0 - 1.0);
        o.a = max(o.a, max(a.a, b.a));
        return;
    } else if (u_kind == 9) {
        // the old picture shrinks away over the new one
        vec2 ua = (v_uv - 0.5) / max(1.0 - u_p, 0.001) + 0.5;
        bool inside = ua.x >= 0.0 && ua.y >= 0.0 && ua.x <= 1.0 && ua.y <= 1.0;
        o = inside ? texture(u_old, ua) : b;
        return;
    }
    o = mix(a, b, m);
}";
		#endregion

		#region Placing
		// the camera's space: x right, y down, z ahead of the eye
		private static Matrix4x4 View(Aup2Camera? cam) {
			if (cam == null) return Matrix4x4.CreateTranslation(0, 0, 1024);
			var forward = cam.Target - cam.Eye;
			if (forward.LengthSquared() < 1e-6f) forward = new Vector3(0, 0, 1);
			forward = Vector3.Normalize(forward);
			var down = new Vector3(0, 1, 0);
			var right = Vector3.Cross(down, forward);
			if (right.LengthSquared() < 1e-6f) right = new Vector3(1, 0, 0);
			right = Vector3.Normalize(right);
			down = Vector3.Cross(forward, right);
			if (cam.Roll != 0) {
				// a positive tilt turns the picture clockwise: the camera itself turns the other way
				var roll = Matrix4x4.CreateFromAxisAngle(forward, (float)(-cam.Roll * Math.PI / 180));
				right = Vector3.TransformNormal(right, roll);
				down = Vector3.TransformNormal(down, roll);
			}
			var e = cam.Eye;
			return new Matrix4x4(
				right.X, down.X, forward.X, 0,
				right.Y, down.Y, forward.Y, 0,
				right.Z, down.Z, forward.Z, 0,
				-Vector3.Dot(e, right), -Vector3.Dot(e, down), -Vector3.Dot(e, forward), 1);
		}

		// the plane 1024 ahead of the eye shows at its own size, unless the camera sets another angle
		private Matrix4x4 Projection(Aup2Camera? cam, int w, int h) {
			double f = cam?.ScreenDistance(h) ?? 1024;
			// depth runs from 1 to 100000 ahead of the eye
			const float near = 1, far = 100000;
			return new Matrix4x4(
				(float)(2 * f / w), 0, 0, 0,
				0, (float)(2 * f / h), 0, 0,
				0, 0, (far + near) / (far - near), 1,
				0, 0, -2 * far * near / (far - near), 0);
		}

		private void Draw(Aup2Item item, Aup2Surface? surface, Aup2Texture direct) => Draw(item, surface, direct, _frame, item.UnderCamera && _camera != null);

		// target: the frame, or the temporary buffer (seen flat, no camera)
		private void Draw(Aup2Item item, Aup2Surface? surface, Aup2Texture direct, Aup2Surface? target, bool depth) {
			if (target == null) return;
			uint tex; double w, h; bool straight;
			double rx = 0, ry = 0, rw, rh;          // the part of the texture that is the picture
			bool flipX = false, flipY = false;
			double offX = item.OffsetX, offY = item.OffsetY;
			if (surface != null) {
				tex = surface.Tex; w = surface.W; h = surface.H; straight = false; rw = w; rh = h;
			} else {
				tex = direct.Id; w = direct.W; h = direct.H; straight = direct.Straight;
				rx = Math.Clamp(item.CropLeft, 0, w); ry = Math.Clamp(item.CropTop, 0, h);
				rw = w - rx - Math.Clamp(item.CropRight, 0, w); rh = h - ry - Math.Clamp(item.CropBottom, 0, h);
				if (rw <= 0 || rh <= 0) return;
				if (!item.Recentre) {
					double ox = rx + rw / 2 - w / 2, oy = ry + rh / 2 - h / 2;
					offX += item.FlipX ? -ox : ox;
					offY += item.FlipY ? -oy : oy;
				}
				flipX = item.FlipX; flipY = item.FlipY;
			}

			var cam = item.UnderCamera && target == _frame ? _camera : null;
			var view = View(cam);
			var turn = Aup2Evaluator.Turn(item.RotX, item.RotY, item.RotZ);
			if (item.Billboard && cam != null) {
				// the camera's own turn undone: the picture faces it
				var inv = view; inv.M41 = inv.M42 = inv.M43 = 0;
				Matrix4x4.Invert(inv, out turn);
			}
			Vector3? tint = null;
			if (cam != null && (item.CullBack || _lights.Count > 0)) {
				var at = new Vector3((float)item.X, (float)item.Y, (float)item.Z);
				var normal = Vector3.TransformNormal(new Vector3(0, 0, -1), turn);
				if (item.CullBack && Vector3.Dot(normal, cam.Eye - at) <= 0) return;
				if (_lights.Count > 0) tint = Shade(at, Vector3.Normalize(normal));
			}
			var outer = Matrix4x4.CreateTranslation((float)(offX - item.PivotX), (float)(offY - item.PivotY), (float)-item.PivotZ)
				* Matrix4x4.CreateScale((float)item.ScaleX, (float)item.ScaleY, 1)
				* turn
				* Matrix4x4.CreateTranslation((float)item.X, (float)item.Y, (float)item.Z)
				* view * Projection(cam, target.W, target.H);

			Aup2Surface? backdrop = (int)item.Blend >= 10 ? Gpu.Copy(target) : null;
			var whole = new Aup2Part { RectX = 0, RectY = 0, RectW = rw, RectH = rh };
			IEnumerable<Aup2Part> parts = item.Parts != null && surface != null ? item.Parts : new[] { whole };
			foreach (var p in parts) {
				if (p.RectW <= 0 || p.RectH <= 0 || p.Opacity <= 0) continue;
				double pcx = p.RectX + p.RectW / 2 - rw / 2, pcy = p.RectY + p.RectH / 2 - rh / 2;
				var m = Matrix4x4.CreateTranslation(-0.5f, -0.5f, 0)
					* Matrix4x4.CreateScale((float)(p.RectW * p.ScaleX * (flipX ? -1 : 1)), (float)(p.RectH * p.ScaleY * (flipY ? -1 : 1)), 1)
					* Aup2Evaluator.Turn(p.RotX, p.RotY, p.RotZ)
					* Matrix4x4.CreateTranslation((float)(pcx + p.X), (float)(pcy + p.Y), (float)p.Z)
					* outer;
				var uv = new Vector4((float)((rx + p.RectX) / w), (float)((ry + p.RectY) / h), (float)(p.RectW / w), (float)(p.RectH / h));
				Gpu.Quad(target, tex, m, uv, item.Opacity * p.Opacity, item.Blend, straight, backdrop, tint, depth);
			}
			Gpu.Release(backdrop);
		}
		#endregion

		// Shows a frame surface on the caller's target: its top left at (x, y) in the game's own
		// coordinates, w x h there.
		public void Present(Aup2Surface frame, double x, double y, double w, double h, double opacity) {
			Gpu.BindCallerTarget();
			float gw = GameWindowSize.Width, gh = GameWindowSize.Height;
			var m = new Matrix4x4(
				(float)(2 * w / gw), 0, 0, 0,
				0, (float)(-2 * h / gh), 0, 0,
				0, 0, 1, 0,
				(float)(2 * x / gw - 1), (float)(1 - 2 * y / gh), 0, 1);
			var cam = Game.Camera;
			var camera = new Matrix4x4(
				cam.M11, cam.M12, cam.M13, cam.M14, cam.M21, cam.M22, cam.M23, cam.M24,
				cam.M31, cam.M32, cam.M33, cam.M34, cam.M41, cam.M42, cam.M43, cam.M44);
			Gpu.QuadToCaller(frame.Tex, m * camera, new Vector4(0, 0, 1, 1), opacity);
			Gpu.LeaveCallerTarget();
		}

		public void Dispose() {
			foreach (var p in _pending) Gpu.Release(p.Surface);
			_pending.Clear();
			foreach (var h in _held.Values) Gpu.Release(h.Surface);
			_held.Clear();
			EndScriptFrame();
			Gpu.Release(_previous);
			_previous = null;
			if (_ownsGpu) {
				foreach (var e in _texts.Values) Game.Gl.DeleteTexture(e.Tex);
				_texts.Clear();
				_textBytes = 0;
				Gpu.Dispose();
			}
		}
	}
}
