using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace OpenTaiko.AviUtl2 {
	// the fixed-function ones first; from 10 on the mode reads the picture underneath
	public enum Aup2Blend {
		Normal = 0, Add = 1, Subtract = 2, Multiply = 3, Screen = 4,
		Overlay = 10, Lighten = 11, Darken = 12, Luminance = 13, ColorDifference = 14, Shade = 15, LightDark = 16, Difference = 17,
	}

	public enum Aup2ItemKind { Image, Video, Shape, Text, Scene, FrameBuffer, Previous, Filter, SceneChange, Model, Compose }

	// A piece of an object drawn on its own: a letter of a text, a tile of a split picture. Its rectangle
	// is in the object's picture, in pixels; its own move, turn, scale and opacity add to the object's.
	public sealed class Aup2Part {
		public int Index, Count = 1;
		public double RectX, RectY, RectW, RectH;
		public double X, Y, Z;
		public double ScaleX = 1, ScaleY = 1;
		public double RotX, RotY, RotZ;
		public double Opacity = 1;
	}

	// One object of a frame, in the scene's own pixels: (X, Y, Z) is where the object's centre goes, counted
	// from the centre of the frame, x right, y down, z away. Turns are in degrees, clockwise for Z.
	public sealed class Aup2Item {
		public Aup2ItemKind Kind;
		public int ObjectId;
		public int Layer;
		public string Path = "";
		public double SourceMs;                 // a video's own time
		public double CropLeft, CropTop, CropRight, CropBottom;
		public bool Recentre;                   // the cropped part takes the object's centre
		public double X, Y, Z;
		public double PivotX, PivotY, PivotZ;
		public double ScaleX = 1, ScaleY = 1;
		public double RotX, RotY, RotZ;
		public double Rotation { get => RotZ; set => RotZ = value; }
		public double Opacity = 1;
		public Aup2Blend Blend = Aup2Blend.Normal;
		public bool FlipX, FlipY;
		// where the picture's centre is from the object's, after filters that grew it to one side
		public double OffsetX, OffsetY;
		public bool Made;                       // its picture comes from the object's own maker, not from a file
		public bool UnderCamera;                // drawn through a camera control
		public bool Billboard;                  // faces the camera
		public bool CullBack;                   // not drawn when seen from behind
		public bool ModelAdjust;                // a model file read with its up and its front turned to this space
		public int ComposeTo;                   // the last layer an image composition gathers
		public List<Aup2Part>? Parts;

		// a shape has no file
		public string ShapeKind = "";
		public double ShapeWidth, ShapeHeight, ShapeLine;
		public int R = 255, G = 255, B = 255;
		// a text
		public Aup2TextStyle? Text;
		// a scene inside the scene
		public int SceneIndex;
		public double SceneFrame;
		// a scene change: how far it is (0..1), its kind, and the frame the old picture is taken from
		public double ChangeProgress;
		public string ChangeKind = "";
		public bool ChangeReverse;
		// a filter kept to a shape of the frame (部分フィルタ): the shape, its place and its soft edge
		public bool Masked;
		public double MaskX, MaskY, MaskBlur;
		public bool MaskInvert;

		public double BoxWidth => ShapeWidth;
		public double BoxHeight => ShapeHeight;

		// The part of a srcW x srcH source that shows, and where its centre lands in the frame (pixels
		// from the frame's top left) for an object seen flat. False when the crop leaves nothing.
		public bool Place(double srcW, double srcH, double frameW, double frameH,
			out double cropX, out double cropY, out double cropW, out double cropH, out double centreX, out double centreY) {
			cropX = Math.Clamp(CropLeft, 0, srcW);
			cropY = Math.Clamp(CropTop, 0, srcH);
			cropW = srcW - cropX - Math.Clamp(CropRight, 0, srcW);
			cropH = srcH - cropY - Math.Clamp(CropBottom, 0, srcH);
			centreX = centreY = 0;
			if (cropW <= 0 || cropH <= 0) return false;
			double lx = Recentre ? 0 : cropX + cropW / 2 - srcW / 2;
			double ly = Recentre ? 0 : cropY + cropH / 2 - srcH / 2;
			if (FlipX) lx = -lx;
			if (FlipY) ly = -ly;
			lx = (lx + OffsetX - PivotX) * ScaleX;
			ly = (ly + OffsetY - PivotY) * ScaleY;
			double a = RotZ * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a);
			centreX = frameW / 2 + X + lx * c - ly * s;
			centreY = frameH / 2 + Y + lx * s + ly * c;
			return true;
		}
	}

	// A sound that plays in a frame.
	public sealed class Aup2AudioItem {
		public int ObjectId;
		public string Path = "";
		public double OffsetMs;                 // the sound's own time
		public double Volume = 1;
		public double Pan;                      // -1 left .. 1 right
		public double Speed = 1;
		public bool Loop;
		public double Mono;                     // 0 as recorded .. 1 left and right the same
	}

	// The camera of a camera control: where it stands, what it looks at.
	public sealed class Aup2Camera {
		public Vector3 Eye = new(0, 0, -1024), Target = Vector3.Zero;
		public double Roll, FieldOfView;
		public int Layer, Count;
		// How far ahead of the eye a picture shows at its own size: 1024 with no angle set, else the
		// distance at which the frame's height fills that angle.
		public double ScreenDistance(int frameHeight) => FieldOfView > 0 && FieldOfView < 179
			? frameHeight / 2.0 / Math.Tan(FieldOfView * Math.PI / 360) : 1024;
		// depth blur: how strong, and how far from the eye things are sharp
		public double Blur, FocusDistance;
	}

	public enum Aup2LightKind { Ambient, Point, Spot, Parallel }

	// A light of a camera control: it shades the objects the camera draws.
	public sealed class Aup2Light {
		public Aup2LightKind Kind;
		public Vector3 Position, Target, Direction, Color = Vector3.One;
		public double Brightness = 1, Falloff, Range = 45, Soft;
	}

	public sealed class Aup2Frame {
		public List<Aup2Item> Draws { get; } = new();
		public List<Aup2AudioItem> Audio { get; } = new();
		public List<Aup2Camera> Cameras { get; } = new();
		public List<Aup2Light> Lights { get; } = new();
	}

	// What draws: it owns the pictures. The evaluator calls it object by object, in drawing order.
	internal interface IAup2Host {
		Aup2Gpu? Gpu { get; }
		// the object's picture as a surface the filters can read and replace; false when there is none yet
		bool Materialize(Aup2Context c);
		void Compose(Aup2Context c);
		// the frame drawn so far, for the objects that read it
		Aup2Surface? FrameCopy();
		void ReplaceFrame(Aup2Surface s);
		void BeginCamera(Aup2Camera camera);
		void EndCamera();
		// a light for the objects of the camera in force
		void AddLight(Aup2Light light);

		// --- what scripts ask for ---
		// The object's picture drawn now, moved, scaled and turned by the arguments on top of the object's
		// own place. Into the frame, or into the shared temporary buffer when that is the draw target
		// (there the object's own place does not count: only the arguments do).
		void ScriptDraw(Aup2Context c, double ox, double oy, double oz, double zoom, double alpha, double rx, double ry, double rz);
		// A face of 3 or 4 corners. `corners` are in the object's own space (pixels from its centre, y down,
		// z away), clockwise seen from the front; `uv` are the matching places in the picture, in pixels.
		void ScriptPoly(Aup2Context c, ReadOnlySpan<Vector3> corners, ReadOnlySpan<Vector2> uv, double alpha);
		// temp: drawing goes to the temporary buffer (made w x h and cleared when both are > 0), else to the frame
		void SetDrawTarget(Aup2Context c, bool temp, int w, int h);
		// Copies between "object", "tempbuffer", "framebuffer", "cache:<name>" and (source only)
		// "image:<path>". The destination takes the source's size. False when either side is missing.
		bool CopyBuffer(Aup2Context c, string dst, string src);
		// the object's picture becomes a shape, a text, an image file; false when it could not
		bool LoadFigure(Aup2Context c, string kind, int rgb, double size, double line);
		bool LoadText(Aup2Context c, Aup2TextStyle style);
		bool LoadImage(Aup2Context c, string path);
		// the object's picture as straight RGBA bytes, top row first (null when there is none), and back
		byte[]? ReadPixels(Aup2Context c, out int w, out int h);
		void WritePixels(Aup2Context c, byte[] rgba, int w, int h);
		// the camera in force for the object (null: the default view)
		Aup2Camera? CurrentCamera { get; }
	}

	// An object while its effects run: the item they shape, the effect being run and, when something
	// draws, its picture.
	internal sealed class Aup2Context {
		public Aup2Item Item = null!;
		public Aup2Object Object = null!;
		public Aup2Effect Effect = null!;
		public Aup2Scene Scene = null!;
		public IAup2Host? Host;
		public Aup2Gpu? Gpu => Host?.Gpu;
		// the picture, premultiplied; null until a filter asks for it
		public Aup2Surface? Surface;
		public double Frame;                    // frames since the object's first
		public double Time => Frame / Scene.Rate;
		public double Duration => (Object.End + 1 - Object.Start) / Scene.Rate;
		public double TimeLeft => Duration - Time;
		public int FrameW => Scene.Width;
		public int FrameH => Scene.Height;
		public bool Skip;                       // nothing to draw any more
		public bool Drawn;                      // a script drew the object itself: it is not drawn again
		public bool CullBack;                   // faces seen from behind are not drawn
		public bool Flat;                       // drawing goes to the temporary buffer
		public double Now;                      // the scene's frame the object is evaluated at
		public int EffectIndex;                 // the running effect's place in the object

		// Runs one of the built-in effects on the object as if it were in the project, with these settings
		// ("name", "value" pairs as they would be written in the file). False when there is no such effect.
		public bool RunEffect(string name, IReadOnlyList<KeyValuePair<string, string>> settings) {
			if (!Aup2Evaluator.Filters.TryGetValue(name, out var fn)) return false;
			var section = new Aup2Section("script");
			section.Items.Add(new("effect.name", name));
			foreach (var kv in settings) section.Items.Add(kv);
			var saved = Effect;
			Effect = new Aup2Effect(section);
			try { fn(this); } finally { Effect = saved; }
			return true;
		}

		public int Seed(string key) => Object.Id * 131 + key.GetHashCode(StringComparison.Ordinal);

		// a setting of the running effect, now
		public double V(string key, double fallback = 0) => V(Effect, key, fallback);
		public double V(Aup2Effect e, string key, double fallback = 0) => e.Track(key, fallback).Evaluate(Frame, Object.Frames, Seed(key));
		public string S(string key, string fallback = "") => Effect.Get(key) ?? fallback;
		public bool Flag(string key) => Effect.Section.GetInt(key) != 0;
		public bool Has(string key) => Effect.Has(key);

		// "rrggbb" as 0..1
		public Vector3 Color(string key, Vector3? fallback = null) => ParseColor(Effect.Get(key), fallback ?? Vector3.One);
		public static Vector3 ParseColor(string? text, Vector3 fallback) {
			if (text != null && text.Length == 6 && int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
				return new Vector3(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
			return fallback;
		}

		// a number in 0..1 that depends on the object, a name and an index, the same every time
		public double Random(string name, long index = 0) {
			unchecked {
				ulong x = (ulong)index * 0x9E3779B97F4A7C15UL + (ulong)(uint)Seed(name) * 0xC2B2AE3D27D4EB4FUL + 0x165667B19E3779F9UL;
				x ^= x >> 31; x *= 0xD6E8FEB86659FD93UL; x ^= x >> 32;
				return (x >> 11) / (double)(1UL << 53);
			}
		}

		// Makes sure Surface holds the picture (cropped and flipped as the item says). False when there is
		// nothing to read: no renderer, or a source that is not ready.
		public bool Pixels() {
			if (Surface != null) return true;
			if (Host == null || Host.Gpu == null || Skip) return false;
			return Host.Materialize(this) && Surface != null;
		}

		// A new picture from the current one through a shader. w, h default to the current size. The
		// current picture stays; Replace swaps it.
		public Aup2Surface? Pass(string fragment, Aup2Uniforms? uniforms = null, int w = 0, int h = 0, Aup2Surface? src = null) {
			if (!Pixels() && src == null) return null;
			var from = src ?? Surface!;
			var t = Gpu!.Rent(w > 0 ? w : from.W, h > 0 ? h : from.H);
			Gpu.Pass(t, fragment, from, uniforms);
			return t;
		}

		// The new picture takes the place of the current one. (dx, dy): how far its centre is from the
		// old one's, in pixels (a shadow that grows the picture to the right moves the centre right).
		public void Replace(Aup2Surface? s, double dx = 0, double dy = 0) {
			if (s == null || s == Surface) return;
			// the pieces follow a picture that changes size: their rectangles stretch with it
			if (Surface != null && Item.Parts != null && (s.W != Surface.W || s.H != Surface.H)) {
				double kx = (double)s.W / Surface.W, ky = (double)s.H / Surface.H;
				foreach (var p in Item.Parts) { p.RectX *= kx; p.RectW *= kx; p.RectY *= ky; p.RectH *= ky; }
			}
			Gpu?.Release(Surface);
			Surface = s;
			Item.OffsetX += dx;
			Item.OffsetY += dy;
		}

		// one shader over the whole picture, same size
		public bool Filter(string fragment, Aup2Uniforms? uniforms = null) {
			var t = Pass(fragment, uniforms);
			if (t == null) return false;
			Replace(t);
			return true;
		}

		// A new picture drawn by a shader alone (no source), w x h: what an object that makes its own
		// picture calls. It becomes the object's picture. False when nothing draws (no renderer).
		public bool Make(int w, int h, string fragment, Aup2Uniforms? uniforms = null) {
			if (Host == null || Gpu == null) return false;
			w = Math.Clamp(w, 1, Aup2Gpu.MaxSide); h = Math.Clamp(h, 1, Aup2Gpu.MaxSide);
			var t = Gpu.Rent(w, h);
			Gpu.Pass(t, fragment, 0, 1, 1, uniforms);
			Gpu.Release(Surface);
			Surface = t;
			Item.ShapeWidth = w; Item.ShapeHeight = h;
			return true;
		}

		// see-through margins around the picture, in pixels (before a blur, a glow, a shadow)
		public bool Grow(int left, int top, int right, int bottom) {
			if (!Pixels()) return false;
			left = Math.Max(0, left); top = Math.Max(0, top); right = Math.Max(0, right); bottom = Math.Max(0, bottom);
			// no surface is larger than the limit: the margins shrink to fit
			int roomX = Math.Max(0, Aup2Gpu.MaxSide - Surface!.W), roomY = Math.Max(0, Aup2Gpu.MaxSide - Surface.H);
			if (left + right > roomX) { double k = (double)roomX / (left + right); left = (int)(left * k); right = (int)(right * k); }
			if (top + bottom > roomY) { double k = (double)roomY / (top + bottom); top = (int)(top * k); bottom = (int)(bottom * k); }
			if (left + top + right + bottom == 0) return true;
			int w = Surface.W + left + right, h = Surface.H + top + bottom;
			var parts = Item.Parts;
			Item.Parts = null;   // margins move the pieces, they do not stretch them
			var t = Gpu!.Rent(w, h);
			Gpu.Pass(t, "uniform vec2 u_off; void main(){ vec2 p = (v_uv * u_size - u_off) / u_srcSize; o = (p.x < 0.0 || p.y < 0.0 || p.x > 1.0 || p.y > 1.0) ? vec4(0.0) : texture(u_src, p); }",
				Surface, new Aup2Uniforms().V2("u_off", left, top));
			Replace(t, (right - left) / 2.0, (bottom - top) / 2.0);
			if (parts != null) {
				foreach (var p in parts) { p.RectX += left; p.RectY += top; }
				Item.Parts = parts;
			}
			return true;
		}
		public bool Grow(int margin) => Grow(margin, margin, margin, margin);

		public void Dispose() {
			Gpu?.Release(Surface);
			Surface = null;
		}
	}

	internal delegate void Aup2FilterFn(Aup2Context c);

	// The script files a project can call: effects and objects written in Lua.
	internal interface IAup2Scripts {
		bool Knows(string effectName);
		// runs the script the running effect names; nothing happens when there is none
		void Run(Aup2Context c);
	}

	// Turns a scene and a frame number into what to draw and play.
	public sealed class Aup2Evaluator {
		public Aup2Scene Scene { get; }
		// what the project uses that this player does not draw, and what it only draws roughly
		public List<string> Unsupported { get; } = new();
		public List<string> Approximated { get; } = new();

		private readonly List<Aup2Object> _byLayer;

		internal const string ImageFile = "画像ファイル", VideoFile = "動画ファイル", AudioFile = "音声ファイル";
		internal const string Shape = "図形", Text = "テキスト", GroupControl = "グループ制御", CameraControl = "カメラ制御";
		internal const string TimeControl = "時間制御", TimeControlObject = "時間制御(オブジェクト)", SceneObject = "シーン", SceneAudio = "シーン(音声)", FrameBuffer = "フレームバッファ";
		internal const string AmbientLight = "環境光(カメラ制御)", PointLight = "点光源(カメラ制御)", SpotLight = "スポット光源(カメラ制御)", ParallelLight = "平行光源(カメラ制御)";
		internal const string AudioGroup = "グループ制御(音声)", FilterObject = "フィルタオブジェクト", ModelFile = "モデルファイル", ComposeObject = "画像合成(オブジェクト)";
		internal const string Previous = "直前オブジェクト", SceneChange = "シーンチェンジ", PartialFilter = "部分フィルタ";
		internal const string StandardDraw = "標準描画", VideoPlay = "映像再生", AudioPlay = "音声再生";

		private static readonly HashSet<string> Inputs = new() {
			ImageFile, VideoFile, AudioFile, Shape, Text, GroupControl, CameraControl, TimeControl, SceneObject, FrameBuffer, Previous,
			SceneChange, PartialFilter, TimeControlObject, AmbientLight, PointLight, SpotLight, ParallelLight, AudioGroup, FilterObject, ModelFile, ComposeObject,
			Aup2AudioEffects.Buffer,
		};

		internal static readonly Dictionary<string, Aup2FilterFn> Filters = BuildFilters();
		// the project's script files, when it has any
		internal IAup2Scripts? Scripts;
		// effects that are read by the code around them, not run as filters
		private static readonly HashSet<string> Silent = new() { VideoPlay, AudioPlay, "音量フェード", "音量調整", Aup2AudioEffects.Mono, "目標中心回転", "目標サイズ固定視野角", "手ぶれ" };

		private static Dictionary<string, Aup2FilterFn> BuildFilters() {
			var map = new Dictionary<string, Aup2FilterFn>();
			Aup2FiltersBasic.Register(map);
			Aup2FiltersColor.Register(map);
			Aup2FiltersBlur.Register(map);
			Aup2FiltersDistort.Register(map);
			Aup2FiltersMotion.Register(map);
			Aup2FiltersExtra.Register(map);
			Aup2FiltersCamera.Register(map);
			return map;
		}

		// objects that make their own picture, by the name of their first effect
		internal static readonly Dictionary<string, Aup2FilterFn> Makers = BuildMakers();
		private static Dictionary<string, Aup2FilterFn> BuildMakers() {
			var map = new Dictionary<string, Aup2FilterFn>();
			Aup2Objects.Register(map);
			return map;
		}

		public static bool Knows(string effect) => Filters.ContainsKey(effect) || Inputs.Contains(effect) || Silent.Contains(effect) || Makers.ContainsKey(effect) || Aup2SceneChanges.Kinds.ContainsKey(effect);

		public Aup2Evaluator(Aup2Scene scene) : this(scene, null) { }

		internal Aup2Evaluator(Aup2Scene scene, IAup2Scripts? scripts) {
			Scene = scene;
			Scripts = scripts;
			_byLayer = new List<Aup2Object>(scene.Objects);
			_byLayer.Sort((a, b) => a.Layer != b.Layer ? a.Layer.CompareTo(b.Layer) : a.Start.CompareTo(b.Start));
			Survey();
		}

		private void Note(List<string> list, string text) {
			if (!list.Contains(text)) list.Add(text);
		}

		// one pass over the whole scene, so the lists are complete before anything plays
		private void Survey() {
			foreach (var o in Scene.Objects) {
				int sections = o.Frames.Length - 1;
				for (int i = 0; i < o.Effects.Count; i++) {
					var e = o.Effects[i];
					bool scripted = Scripts != null && Scripts.Knows(e.Name);
					if (i == 0 && !Inputs.Contains(e.Name) && !Filters.ContainsKey(e.Name) && !Makers.ContainsKey(e.Name) && !Aup2SceneChanges.Kinds.ContainsKey(e.Name) && !scripted) Note(Unsupported, "object: " + e.Name);
					else if (i > 0 && !Knows(e.Name) && !scripted) Note(Unsupported, "effect: " + e.Name);
					if (e.Name == Shape && !Aup2Shapes.Knows(e.Get("図形の種類") ?? "")) Note(Unsupported, "shape: " + e.Get("図形の種類"));
					if (e.Name == StandardDraw) {
						string blend = e.Get("合成モード") ?? "通常";
						if (BlendOf(blend) == null) Note(Unsupported, "blend mode: " + blend);
					}
					foreach (var kv in e.Section.Items) {
						if (kv.Key == "effect.name") continue;
						var t = e.Track(kv.Key);
						if (!t.IsAnimated) continue;
						if (t.Motion == Aup2Motion.Unknown) Note(Unsupported, "movement: " + t.MotionName);
						if (t.Motion is Aup2Motion.Rotation or Aup2Motion.Step or Aup2Motion.Repeat or Aup2Motion.Random)
							Note(Approximated, "movement: " + t.MotionName);
						if (t.TimeControl && !t.CurveKnown(sections))
							Note(Unsupported, "time control curve with " + t.Curve.Length.ToString(CultureInfo.InvariantCulture) + " numbers");
					}
				}
			}
		}

		internal static Aup2Blend? BlendOf(string name) => name switch {
			"通常" => Aup2Blend.Normal,
			"加算" => Aup2Blend.Add,
			"減算" => Aup2Blend.Subtract,
			"乗算" => Aup2Blend.Multiply,
			"スクリーン" => Aup2Blend.Screen,
			"オーバーレイ" => Aup2Blend.Overlay,
			"比較(明)" => Aup2Blend.Lighten,
			"比較(暗)" => Aup2Blend.Darken,
			"輝度" => Aup2Blend.Luminance,
			"色差" => Aup2Blend.ColorDifference,
			"陰影" => Aup2Blend.Shade,
			"明暗" => Aup2Blend.LightDark,
			"差分" => Aup2Blend.Difference,
			_ => null,
		};

		private sealed class Group {
			public Aup2Object Object = null!;
			public int Layer, Count;
			public double X, Y, Z, Scale = 1, RotX, RotY, RotZ;
		}

		private sealed class TimeShift {
			public int Layer, Count;
			public double Frame;
		}

		private sealed class Volume {
			public int Layer, Count;
			public double Gain = 1, Pan;
		}

		// where the object standing on a layer is at a frame (its 標準描画), null when there is none
		private Vector3? PlaceOf(int layer, double frame) {
			foreach (var o in _byLayer) {
				if (o.Layer != layer || frame < o.Start || frame >= o.End + 1) continue;
				var draw = o.Find(StandardDraw);
				if (draw == null) return null;
				double f = frame - o.Start;
				double V(string key) => draw.Track(key).Evaluate(f, o.Frames, o.Id * 131 + key.GetHashCode(StringComparison.Ordinal));
				return new Vector3((float)V("X"), (float)V("Y"), (float)V("Z"));
			}
			return null;
		}

		// the camera of a camera control object, with the camera effects it carries
		private Aup2Camera ReadCamera(Aup2Context c, Aup2Object o, Aup2Effect input, double frame) {
			var cam = new Aup2Camera {
				Layer = o.Layer, Count = input.Section.GetInt("対象レイヤー数"),
				Eye = new Vector3((float)c.V("X"), (float)c.V("Y"), (float)c.V("Z", -1024)),
				Target = new Vector3((float)c.V("目標X"), (float)c.V("目標Y"), (float)c.V("目標Z")),
				Roll = c.V("傾き"), FieldOfView = c.V("視野角"), Blur = Math.Max(0, c.V("深度ぼけ")),
			};
			// the target values count from the object on the target layer (layers are numbered from 1 here,
			// none is the origin, the camera's own layer is the camera); a moving value slides between layers
			Vector3? Base(double layer) {
				if (layer < 0.5) return null;
				Vector3 At(int n) => n - 1 == o.Layer ? cam.Eye : PlaceOf(n - 1, frame) ?? Vector3.Zero;
				int low = (int)Math.Floor(layer);
				double part = layer - low;
				return part < 1e-6 ? At(low) : Vector3.Lerp(At(Math.Max(1, low)), At(low + 1), (float)part);
			}
			var focusBase = Base(c.V("焦点レイヤー"));
			if (Base(c.V("目標レイヤー")) is Vector3 at) cam.Target += at;
			for (int i = 1; i < o.Effects.Count; i++) {
				var e = o.Effects[i];
				c.Effect = e;
				switch (e.Name) {
					case "目標中心回転": {
						// the eye goes round the target: sideways, up and down, nearer or further
						var d = cam.Eye - cam.Target;
						double dist = Math.Max(1, d.Length() + c.V("距離"));
						double yaw = Math.Atan2(d.X, -d.Z) + c.V("水平回転") * Math.PI / 180;
						double pitch = Math.Asin(Math.Clamp(-d.Y / Math.Max(1e-6, d.Length()), -1, 1)) + c.V("垂直回転") * Math.PI / 180;
						pitch = Math.Clamp(pitch, -1.55, 1.55);
						cam.Eye = cam.Target + new Vector3((float)(Math.Sin(yaw) * Math.Cos(pitch) * dist), (float)(-Math.Sin(pitch) * dist), (float)(-Math.Cos(yaw) * Math.Cos(pitch) * dist));
						break;
					}
					case "目標サイズ固定視野角": {
						// the angle changes and the eye moves so the target keeps its size on screen
						double before = cam.FieldOfView > 0 ? cam.FieldOfView : 2 * Math.Atan(Scene.Height / 2.0 / 1024) * 180 / Math.PI;
						double after = Math.Clamp(before + c.V("増減値"), 1, 170);
						var d = cam.Eye - cam.Target;
						double k = Math.Tan(before * Math.PI / 360) / Math.Tan(after * Math.PI / 360);
						cam.Eye = cam.Target + d * (float)k;
						cam.FieldOfView = after;
						break;
					}
					case "手ぶれ": {
						double amp = c.V("振幅", 5), angle = c.V("角度", 1), every = c.V("間隔");
						double clock = every > 0 ? c.Frame / every : c.Frame / 6;
						double W(string n) {
							double fl = Math.Floor(clock), t = clock - fl;
							double a = c.Random(n, (long)fl) * 2 - 1, b = c.Random(n, (long)fl + 1) * 2 - 1;
							t = t * t * (3 - 2 * t);
							return a + (b - a) * t;
						}
						var shake = new Vector3((float)(W("hx") * amp), (float)(W("hy") * amp), 0);
						cam.Eye += shake; cam.Target += shake;
						cam.Roll += W("hr") * angle;
						break;
					}
				}
			}
			c.Effect = input;
			if (cam.Blur > 0) Note(Approximated, "camera: 深度ぼけ");
			cam.FocusDistance = (cam.Target - cam.Eye).Length();
			if (focusBase is Vector3 focus && (focus - cam.Eye).Length() > 1) cam.FocusDistance = (focus - cam.Eye).Length();
			return cam;
		}

		public Aup2Frame Evaluate(double frame) => Evaluate(frame, null);

		private sealed class Attached {
			public readonly HashSet<Aup2Object> Filters = new();
			public readonly Dictionary<Aup2Object, List<Aup2Object>> ByTarget = new();
		}

		private static readonly HashSet<string> Pictures = new() { ImageFile, VideoFile, Shape, Text, SceneObject, ModelFile, ComposeObject };

		// Under a camera an object made of filters only does not work on the frame: its effects run on
		// the object showing on the nearest layer above it. Null when the frame has none.
		private Attached? AttachedFilters(double frame) {
			Attached? found = null;
			Aup2Object? camera = null, last = null;
			foreach (var o in _byLayer) {
				if (o.Effects.Count == 0 || frame < o.Start || frame >= o.End + 1) continue;
				string name = o.Effects[0].Name;
				if (camera != null) {
					int count = camera.Effects[0].Section.GetInt("対象レイヤー数");
					if (count > 0 && o.Layer > camera.Layer + count) { camera = null; last = null; }
				}
				if (name == CameraControl) { camera = o; last = null; continue; }
				if (camera == null) continue;
				if (Pictures.Contains(name) || Makers.ContainsKey(name)) { last = o; continue; }
				if (last == null || Inputs.Contains(name) || !Filters.ContainsKey(name)) continue;
				found ??= new Attached();
				found.Filters.Add(o);
				if (!found.ByTarget.TryGetValue(last, out var list)) found.ByTarget[last] = list = new List<Aup2Object>();
				list.Add(o);
			}
			return found;
		}

		// layerLimit: only the layers under it are drawn (the picture a scene change starts from)
		// layerFrom: only the layers after it are drawn (what an image composition gathers)
		internal Aup2Frame Evaluate(double frame, IAup2Host? host, int layerLimit = int.MaxValue, int layerFrom = -1) {
			var result = new Aup2Frame();
			var groups = new List<Group>();
			var shifts = new List<TimeShift>();
			var volumes = new List<Volume>();
			Aup2Camera? camera = null;

			var attached = AttachedFilters(frame);
			int gatheredFrom = -1, gatheredTo = -1;
			foreach (var o in _byLayer) {
				if (o.Effects.Count == 0 || o.Layer >= layerLimit || o.Layer <= layerFrom) continue;
				// what an image composition gathered is drawn inside its picture (sounds still play)
				if (o.Layer > gatheredFrom && o.Layer <= gatheredTo && o.Effects[0].Name != AudioFile) continue;
				// a camera stops at the end of its layers
				if (camera != null && camera.Count > 0 && o.Layer > camera.Layer + camera.Count) {
					host?.EndCamera();
					camera = null;
				}
				// a time control above moves the clock of this object
				double now = frame;
				for (int s = shifts.Count - 1; s >= 0; s--) {
					var ts = shifts[s];
					if (o.Layer > ts.Layer && (ts.Count == 0 || o.Layer <= ts.Layer + ts.Count)) { now = ts.Frame; break; }
				}
				if (now < o.Start || now >= o.End + 1) continue;

				var input = o.Effects[0];
				var c = new Aup2Context { Object = o, Effect = input, Scene = Scene, Host = host, Frame = now - o.Start, Now = now };
				var item = c.Item = new Aup2Item { ObjectId = o.Id, Layer = o.Layer, UnderCamera = camera != null };
				double seconds = c.Time;
				int first = 1;

				switch (input.Name) {
					case GroupControl:
						groups.Add(new Group {
							Object = o, Layer = o.Layer, Count = input.Section.GetInt("対象レイヤー数"),
							X = c.V("X"), Y = c.V("Y"), Z = c.V("Z"), Scale = c.V("拡大率", 100) / 100,
							RotX = c.V("X軸回転"), RotY = c.V("Y軸回転"), RotZ = c.V("Z軸回転"),
						});
						continue;
					case TimeControl:
					case TimeControlObject: {
						// 位置 is where the clock of the layers below stands, in percent of this object
						double pos = c.V("位置", 0) / 100;
						shifts.Add(new TimeShift { Layer = o.Layer, Count = input.Section.GetInt("対象レイヤー数"), Frame = o.Start + pos * (o.End - o.Start) });
						continue;
					}
					case CameraControl:
						if (camera != null) host?.EndCamera();
						camera = ReadCamera(c, o, input, now);
						result.Cameras.Add(camera);
						host?.BeginCamera(camera);
						continue;
					case AmbientLight:
					case PointLight:
					case SpotLight:
					case ParallelLight: {
						if (camera == null) continue;
						var light = new Aup2Light { Color = c.Color("色"), Brightness = c.V("明るさ", 100) / 100 };
						if (input.Name == AmbientLight) light.Kind = Aup2LightKind.Ambient;
						else if (input.Name == ParallelLight) {
							// latitude and longitude of where the light comes from
							light.Kind = Aup2LightKind.Parallel;
							double lat = c.V("緯度", 45) * Math.PI / 180, lon = c.V("経度") * Math.PI / 180;
							light.Direction = Vector3.Normalize(new Vector3((float)(Math.Cos(lat) * Math.Sin(lon)), (float)-Math.Sin(lat), (float)(-Math.Cos(lat) * Math.Cos(lon))));
						} else {
							light.Kind = input.Name == SpotLight ? Aup2LightKind.Spot : Aup2LightKind.Point;
							light.Position = new Vector3((float)c.V("X"), (float)c.V("Y"), (float)c.V("Z", -500));
							light.Target = new Vector3((float)c.V("目標X"), (float)c.V("目標Y"), (float)c.V("目標Z"));
							light.Falloff = Math.Max(0, c.V("減衰"));
							light.Range = c.V("範囲", 45); light.Soft = Math.Max(0, c.V("ぼかし"));
						}
						result.Lights.Add(light);
						host?.AddLight(light);
						continue;
					}
					case AudioGroup:
						volumes.Add(new Volume { Layer = o.Layer, Count = input.Section.GetInt("対象レイヤー数"), Gain = c.V("音量", 100) / 100, Pan = c.V("左右") / 100 });
						continue;
					case FilterObject:
						// its effects work on the frame drawn so far
						item.Kind = Aup2ItemKind.Filter;
						break;
					case AudioFile: {
						var a = new Aup2AudioItem {
							ObjectId = o.Id, Path = input.Get("ファイル") ?? "",
							Speed = c.V("再生速度", 100) / 100, Loop = input.Section.GetInt("ループ再生") != 0,
						};
						a.OffsetMs = MediaSeconds(c, input, seconds, a.Speed) * 1000;
						var play = o.Find(AudioPlay);
						if (play != null) {
							a.Volume = c.V(play, "音量", 100) / 100;
							a.Pan = c.V(play, "左右") / 100;
						}
						// the sound's own volume effects, then the audio groups on the layers above
						foreach (var e in o.Effects) {
							if (e.Name == "音量フェード") {
								double fin = c.V(e, "イン"), fout = c.V(e, "アウト");
								if (fin > 0) a.Volume *= Math.Clamp(seconds / fin, 0, 1);
								if (fout > 0) a.Volume *= Math.Clamp(c.TimeLeft / fout, 0, 1);
							} else if (e.Name == "音量調整") {
								a.Volume *= c.V(e, "音量", 100) / 100;
								a.Pan += c.V(e, "左右") / 100;
							} else if (e.Name == Aup2AudioEffects.Mono) {
								a.Mono = Math.Max(a.Mono, Math.Clamp(c.V(e, "比率") / 100, 0, 1));
							}
						}
						foreach (var v in volumes) {
							if (o.Layer <= v.Layer || (v.Count > 0 && o.Layer > v.Layer + v.Count)) continue;
							a.Volume *= v.Gain;
							a.Pan += v.Pan;
						}
						a.Pan = Math.Clamp(a.Pan, -1, 1);
						result.Audio.Add(a);
						continue;
					}
					case Aup2AudioEffects.Buffer:
						// its volume effects work on the sounds heard so far
						if (!Aup2AudioEffects.OnBuffer(c, o, result.Audio)) Note(Approximated, "object: " + input.Name);
						continue;
					case ImageFile:
						item.Kind = Aup2ItemKind.Image;
						item.Path = input.Get("ファイル") ?? "";
						if (input.Section.GetInt("連番ファイル") != 0)
							item.Path = Numbered(item.Path, (long)Math.Floor(c.V("表示番号") + seconds * Scene.Rate * c.V("再生速度", 100) / 100));
						break;
					case VideoFile:
						item.Kind = Aup2ItemKind.Video;
						item.Path = input.Get("ファイル") ?? "";
						item.SourceMs = MediaSeconds(c, input, seconds, c.V("再生速度", 100) / 100) * 1000;
						break;
					case Shape:
						if (!Aup2Shapes.Read(c)) continue;
						break;
					case Text:
						item.Kind = Aup2ItemKind.Text;
						item.Text = Aup2TextStyle.Read(c);
						break;
					case SceneObject:
						item.Kind = Aup2ItemKind.Scene;
						item.SceneIndex = input.Section.GetInt("シーン", input.Section.GetInt("scene", 1));
						item.SceneFrame = (c.V("再生位置") + seconds * c.V("再生速度", 100) / 100) * Scene.Rate;
						break;
					case FrameBuffer:
						item.Kind = Aup2ItemKind.FrameBuffer;
						break;
					case Previous:
						item.Kind = Aup2ItemKind.Previous;
						break;
					case SceneChange:
						// from the picture just before the object to the current one
						item.Kind = Aup2ItemKind.SceneChange;
						item.SceneFrame = o.Start - 1;
						item.ChangeProgress = Math.Clamp((now - o.Start) / Math.Max(1, o.End - o.Start), 0, 1);
						item.ChangeKind = input.Get("シーンチェンジの種類") ?? input.Get("種類") ?? input.Get("name") ?? "クロスフェード";
						item.ChangeReverse = input.Section.GetInt("反転") != 0;
						break;
					case ComposeObject: {
						int count = input.Section.GetInt("対象レイヤー数");
						item.Kind = Aup2ItemKind.Compose;
						item.SceneFrame = now;
						item.ComposeTo = count > 0 ? o.Layer + count : int.MaxValue;
						gatheredFrom = o.Layer; gatheredTo = item.ComposeTo;
						break;
					}
					case ModelFile:
						Note(Approximated, "object: " + ModelFile);
						item.Kind = Aup2ItemKind.Model;
						item.Path = input.Get("ファイル") ?? "";
						item.ModelAdjust = input.Section.GetInt("座標系を調整") != 0;
						break;
					case PartialFilter:
						// the effects after it change the frame inside a shape only
						item.Kind = Aup2ItemKind.Filter;
						item.Masked = true;
						item.ShapeKind = input.Get("マスクの種類") ?? input.Get("図形の種類") ?? "四角形";
						if (!Aup2Shapes.Knows(item.ShapeKind)) item.ShapeKind = "四角形";
						{
							double size = Math.Max(0, c.V("サイズ", 200)), aspect = c.V("縦横比");
							item.ShapeWidth = size * (aspect > 0 ? 1 - aspect / 100 : 1);
							item.ShapeHeight = size * (aspect < 0 ? 1 + aspect / 100 : 1);
							item.ShapeLine = 4000;
							item.MaskX = c.V("X"); item.MaskY = c.V("Y"); item.MaskBlur = Math.Max(0, c.V("ぼかし"));
							item.MaskInvert = input.Section.GetInt("マスクの反転") != 0;
							item.Path = "0";
						}
						break;
					default:
						if (Aup2SceneChanges.Kinds.ContainsKey(input.Name)) {
							item.Kind = Aup2ItemKind.SceneChange;
							item.SceneFrame = o.Start - 1;
							item.ChangeProgress = Math.Clamp((now - o.Start) / Math.Max(1, o.End - o.Start), 0, 1);
							item.ChangeKind = input.Name;
							break;
						}
						if (Makers.TryGetValue(input.Name, out var maker)) {
							// the object draws its own picture: the maker sets its size, and its surface when something draws
							item.Kind = Aup2ItemKind.Shape;
							item.ShapeKind = "背景";
							item.ShapeWidth = item.ShapeHeight = 0;
							item.Made = true;
							maker(c);
							break;
						}
						if (Scripts != null && Scripts.Knows(input.Name) && !Filters.ContainsKey(input.Name)) {
							// an object written as a script: it starts with no picture and makes its own
							item.Kind = Aup2ItemKind.Shape;
							item.ShapeKind = "背景";
							item.ShapeWidth = item.ShapeHeight = 0;
							first = 0;
							break;
						}
						if (!Filters.ContainsKey(input.Name)) continue;
						// under a camera its effects went to the object above it
						if (attached != null && attached.Filters.Contains(o)) continue;
						// an object made of filters only works on the frame drawn so far
						item.Kind = Aup2ItemKind.Filter;
						first = 0;
						break;
				}

				for (int i = first; i < o.Effects.Count && !c.Skip && !c.Drawn; i++) {
					c.Effect = o.Effects[i];
					c.EffectIndex = i;
					if (Filters.TryGetValue(c.Effect.Name, out var fn)) fn(c);
					else Scripts?.Run(c);
				}

				// the filter objects that follow it under a camera, each on its own clock
				if (attached != null && attached.ByTarget.TryGetValue(o, out var extra)) {
					var own = c.Object;
					double ownFrame = c.Frame;
					foreach (var filter in extra) {
						c.Object = filter;
						c.Frame = frame - filter.Start;
						for (int i = 0; i < filter.Effects.Count && !c.Skip && !c.Drawn; i++) {
							c.Effect = filter.Effects[i];
							c.EffectIndex = i;
							if (Filters.TryGetValue(c.Effect.Name, out var fn)) fn(c);
							else Scripts?.Run(c);
						}
					}
					c.Object = own;
					c.Frame = ownFrame;
				}

				// group controls on the layers above, nearest first: the effects a group control carries run
				// on each object it holds, on the group's own clock, then the group places the object
				for (int g = groups.Count - 1; g >= 0; g--) {
					var gr = groups[g];
					if (o.Layer <= gr.Layer || (gr.Count > 0 && o.Layer > gr.Layer + gr.Count)) continue;
					if (gr.Object.Effects.Count > 1 && item.Kind != Aup2ItemKind.Filter && item.Kind != Aup2ItemKind.SceneChange) {
						var own = c.Object;
						double ownFrame = c.Frame;
						c.Object = gr.Object;
						c.Frame = frame - gr.Object.Start;
						for (int i = 1; i < gr.Object.Effects.Count && !c.Skip && !c.Drawn; i++) {
							c.Effect = gr.Object.Effects[i];
							c.EffectIndex = i;
							if (Filters.TryGetValue(c.Effect.Name, out var fn)) fn(c);
							else Scripts?.Run(c);
						}
						c.Object = own;
						c.Frame = ownFrame;
					}
					var p = Vector3.Transform(new Vector3((float)(item.X * gr.Scale), (float)(item.Y * gr.Scale), (float)(item.Z * gr.Scale)), Turn(gr.RotX, gr.RotY, gr.RotZ));
					item.X = gr.X + p.X; item.Y = gr.Y + p.Y; item.Z = gr.Z + p.Z;
					item.ScaleX *= gr.Scale; item.ScaleY *= gr.Scale;
					item.RotX += gr.RotX; item.RotY += gr.RotY; item.RotZ += gr.RotZ;
				}

				if (!c.Skip && item.Opacity > 0) {
					result.Draws.Add(item);
					if (!c.Drawn) host?.Compose(c);
				}
				c.Dispose();
			}
			if (camera != null) host?.EndCamera();
			return result;
		}

		// Where a video or a sound is in its own time. A playback range (start and end written on the
		// position) says it outright; a single position counts on from there at the playback speed.
		private static double MediaSeconds(Aup2Context c, Aup2Effect input, double seconds, double speed) {
			var track = input.Track("再生位置");
			if (track.Motion == Aup2Motion.PlayRange && track.Values.Length >= 2 && track.Values[0] != track.Values[^1]) return c.V("再生位置");
			return c.V("再生位置") + seconds * speed;
		}

		// a numbered file's neighbour: the digits before the extension move on by `step`, keeping their width
		internal static string Numbered(string path, long step) {
			int dot = path.LastIndexOf('.');
			if (dot < 0) dot = path.Length;
			int start = dot;
			while (start > 0 && char.IsDigit(path[start - 1])) start--;
			if (start == dot || step == 0) return path;
			if (!long.TryParse(path.AsSpan(start, dot - start), NumberStyles.Integer, CultureInfo.InvariantCulture, out long n)) return path;
			string digits = Math.Max(0, n + step).ToString(CultureInfo.InvariantCulture).PadLeft(dot - start, '0');
			return path.Substring(0, start) + digits + path.Substring(dot);
		}

		// the turn AviUtl applies: about Z, then Y, then X, in degrees, y down
		internal static Matrix4x4 Turn(double rx, double ry, double rz) {
			const float d = (float)(Math.PI / 180);
			return Matrix4x4.CreateRotationZ((float)rz * d) * Matrix4x4.CreateRotationY((float)ry * d) * Matrix4x4.CreateRotationX((float)rx * d);
		}
	}
}
