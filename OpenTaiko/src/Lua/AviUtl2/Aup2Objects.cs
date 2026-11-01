using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Silk.NET.OpenGLES;

namespace OpenTaiko.AviUtl2 {
	// The objects that draw their own picture: counters, polygons, weather, flares.
	internal static class Aup2Objects {
		public static void Register(Dictionary<string, Aup2FilterFn> map) {
			map["カウンター"] = Counter;
			map["多角形"] = Polygon;
			map["扇型"] = Fan;
			map["星"] = Stars;
			map["雨"] = Rain;
			map["雪"] = Snow;
			map["雲"] = Clouds;
			map["水面"] = Water;
			map["集中線"] = FocusLines;
			map["走査線"] = ScanLines;
			map["フレア"] = Flare;
			map["レンズフレア"] = LensFlare;
			map["ライン(移動軌跡)"] = Trail;
			map["ランダム小物配置(カメラ制御)"] = Scatter;
			map["音声波形表示"] = Waveform;
		}

		#region Helpers
		private const int MostPieces = 2000;
		// the eye stands 1024 in front of the frame: pieces stay ahead of it
		private const double Eye = 1024, Nearest = -900;

		private static bool Make(Aup2Context c, double w, double h, string fragment, Aup2Uniforms uniforms) {
			int iw = (int)Math.Clamp(Math.Ceiling(w), 1, Aup2Gpu.MaxSide), ih = (int)Math.Clamp(Math.Ceiling(h), 1, Aup2Gpu.MaxSide);
			c.Item.ShapeWidth = iw; c.Item.ShapeHeight = ih;
			return c.Make(iw, ih, fragment, uniforms);
		}

		private static double Finite(double v, double fallback = 0) => double.IsFinite(v) ? v : fallback;

		// a shape's name as the piece shader numbers it
		private static int KindOf(string name) => name switch {
			"四角形" => 2, "三角形" => 3, "五角形" => 5, "六角形" => 6, "星型" => 10, _ => 1,
		};

		// v brought back into -half..half
		private static double Wrap(double v, double half) {
			if (half <= 0) return 0;
			return v - 2 * half * Math.Floor((v + half) / (2 * half));
		}

		// A piece that shows the rectangle (rx, ry, rw, rh) of a w x h picture with its centre at (x, y, z)
		// from the object's centre.
		private static Aup2Part Piece(double rx, double ry, double rw, double rh, double w, double h, double x, double y, double z) => new Aup2Part {
			RectX = rx, RectY = ry, RectW = rw, RectH = rh,
			X = x - (rx + rw / 2 - w / 2), Y = y - (ry + rh / 2 - h / 2), Z = z,
		};

		private static void Finish(Aup2Context c, List<Aup2Part> parts) {
			if (parts.Count == 0) { c.Skip = true; return; }
			for (int i = 0; i < parts.Count; i++) { parts[i].Index = i; parts[i].Count = parts.Count; }
			c.Item.Parts = parts;
		}

		// One shape per cell, the cells side by side: u_kind as in KindOf, u_soft from a hard edge (0) to a
		// glowing dot (1), u_hue gives each cell its own colour.
		private const string PieceFragment = @"uniform int u_kind; uniform vec3 u_col; uniform float u_soft; uniform float u_cells; uniform int u_hue;
const float PI = 3.14159265;
float sdPoly(vec2 p, float r, float n){
    float an = PI / n;
    float a = atan(p.x, -p.y);
    float seg = mod(a + an, 2.0 * an) - an;
    return length(p) * cos(seg) - r * cos(an);
}
float sdStar(vec2 p, float r){
    const float n = 5.0; const float inner = 0.382;
    float an = PI / n;
    float a = atan(p.x, -p.y);
    float seg = abs(mod(a + an, 2.0 * an) - an);
    vec2 q = length(p) * vec2(cos(seg), sin(seg));
    vec2 a0 = vec2(r, 0.0), b0 = r * inner * vec2(cos(an), sin(an));
    vec2 e = b0 - a0, w = q - a0;
    vec2 d = w - e * clamp(dot(w, e) / dot(e, e), 0.0, 1.0);
    float s = sign(w.x * e.y - w.y * e.x);
    return length(d) * -s;
}
vec3 hue(float x){ return clamp(vec3(abs(x * 6.0 - 3.0) - 1.0, 2.0 - abs(x * 6.0 - 2.0), 2.0 - abs(x * 6.0 - 4.0)), 0.0, 1.0); }
void main(){
    float cw = u_size.x / u_cells;
    vec2 pos = v_uv * u_size;
    float cell = min(floor(pos.x / cw), u_cells - 1.0);
    vec2 p = vec2(pos.x - (cell + 0.5) * cw, pos.y - u_size.y * 0.5);
    // two clear pixels around the shape keep the cells apart
    float m = max(min(cw, u_size.y) * 0.5 - 2.0, 0.5);
    float d;
    if (u_kind == 2) d = max(abs(p.x), abs(p.y)) - m;
    else if (u_kind == 3) d = sdPoly(vec2(p.x, p.y + m * 0.25), m, 3.0);
    else if (u_kind == 10) d = sdStar(p, m);
    else if (u_kind > 3) d = sdPoly(p, m, float(u_kind));
    else d = length(p) - m;
    float a = clamp(0.5 - d, 0.0, 1.0);
    if (u_soft > 0.0) {
        float g = clamp(-d / m, 0.0, 1.0);
        a = mix(a, g * g, u_soft);
    }
    vec3 col = u_hue == 1 ? hue(cell / u_cells) : u_col;
    o = vec4(col * a, a);
}";
		#endregion

		#region Counter
		// a number that runs with the object's time, drawn as a text
		private static void Counter(Aup2Context c) {
			double value = Finite(c.V("初期値") + c.V("速度", 1) * c.Time);
			var col = c.Color("文字色");
			var edge = c.Color("影・縁色", Vector3.Zero);
			var item = c.Item;
			item.Kind = Aup2ItemKind.Text;
			item.Made = false;
			item.Text = new Aup2TextStyle {
				Text = CounterText(value, c.S("表示形式", "標準")),
				Font = c.S("フォント名"),
				Size = Math.Max(1, Finite(c.V("サイズ", 34), 34)),
				R = (int)Math.Round(col.X * 255), G = (int)Math.Round(col.Y * 255), B = (int)Math.Round(col.Z * 255),
				ER = (int)Math.Round(edge.X * 255), EG = (int)Math.Round(edge.Y * 255), EB = (int)Math.Round(edge.Z * 255),
				Decoration = c.S("装飾タイプ", "標準文字"),
				Align = "中央揃え[中]",
			};
		}

		private static readonly string[] CounterFormats = {
			"標準", "桁揃え(2桁)", "桁揃え(3桁)", "桁揃え(4桁)", "時間(分秒)", "時間(分秒ミリ)", "時間(時分秒)", "時間(時分秒ミリ)",
		};

		internal static string CounterText(double value, string format) {
			// the format by its name, or by its place in the list
			if (int.TryParse(format, NumberStyles.Integer, CultureInfo.InvariantCulture, out int at) && at >= 0 && at < CounterFormats.Length)
				format = CounterFormats[at];
			var inv = CultureInfo.InvariantCulture;
			string sign = value < 0 ? "-" : "";
			double v = Math.Min(Math.Abs(value), 9e15);
			if (!format.StartsWith("時間", StringComparison.Ordinal)) {
				long whole = (long)Math.Floor(v + 1e-9);
				int digits = format.Contains("2桁") ? 2 : format.Contains("3桁") ? 3 : format.Contains("4桁") ? 4 : 1;
				return (whole == 0 ? "" : sign) + whole.ToString(inv).PadLeft(digits, '0');
			}
			bool hours = format.Contains("時分"), fraction = format.Contains("ミリ");
			long hundredths = (long)Math.Floor(v * 100 + 1e-6);
			long seconds = hundredths / 100, minutes = seconds / 60;
			string text = hours
				? (minutes / 60).ToString("00", inv) + ":" + (minutes % 60).ToString("00", inv) + ":" + (seconds % 60).ToString("00", inv)
				: minutes.ToString("00", inv) + ":" + (seconds % 60).ToString("00", inv);
			if (fraction) text += "." + (hundredths % 100).ToString("00", inv);
			return sign + text;
		}
		#endregion

		#region Shapes
		// up to 16 corners, two per uniform; the copies are the same outline turned around the centre
		private const string PolygonFragment = @"uniform vec4 u_a0; uniform vec4 u_a1; uniform vec4 u_a2; uniform vec4 u_a3;
uniform vec4 u_a4; uniform vec4 u_a5; uniform vec4 u_a6; uniform vec4 u_a7;
uniform int u_n; uniform float u_line; uniform int u_copies; uniform float u_step; uniform vec3 u_col;
float seg(vec2 p, vec2 a, vec2 b){
    vec2 e = b - a, w = p - a;
    float t = clamp(dot(w, e) / max(dot(e, e), 1.0e-6), 0.0, 1.0);
    return length(w - e * t);
}
void main(){
    vec2 v[16];
    v[0] = u_a0.xy; v[1] = u_a0.zw; v[2] = u_a1.xy; v[3] = u_a1.zw;
    v[4] = u_a2.xy; v[5] = u_a2.zw; v[6] = u_a3.xy; v[7] = u_a3.zw;
    v[8] = u_a4.xy; v[9] = u_a4.zw; v[10] = u_a5.xy; v[11] = u_a5.zw;
    v[12] = u_a6.xy; v[13] = u_a6.zw; v[14] = u_a7.xy; v[15] = u_a7.zw;
    vec2 p0 = (v_uv - 0.5) * u_size;
    float a = 0.0;
    for (int k = 0; k < 12; k++) {
        if (k >= u_copies) break;
        float r = u_step * float(k);
        float cs = cos(r), sn = sin(r);
        vec2 p = vec2(p0.x * cs + p0.y * sn, -p0.x * sn + p0.y * cs);
        float d = 1.0e9;
        bool inside = false;
        for (int i = 0; i < 16; i++) {
            if (i >= u_n) break;
            int j = (i + 1 == u_n) ? 0 : i + 1;
            vec2 A = v[i], B = v[j];
            d = min(d, seg(p, A, B));
            if ((A.y > p.y) != (B.y > p.y) && p.x < (B.x - A.x) * (p.y - A.y) / (B.y - A.y) + A.x) inside = !inside;
        }
        float sd = inside ? -d : d;
        // the line runs along the inside of the outline; a wide one fills the shape
        a = max(a, clamp(0.5 - sd, 0.0, 1.0) * clamp(0.5 + sd + u_line, 0.0, 1.0));
    }
    o = vec4(u_col * a, a);
}";

		private static void Polygon(Aup2Context c) {
			var pts = new List<Vector2>();
			// the corners as written, x then y, from the object's centre
			var numbers = new List<double>();
			foreach (var t in c.S("座標").Split(new[] { ',', '{', '}', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries))
				if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && double.IsFinite(v)) numbers.Add(v);
			int wanted = (int)Math.Clamp(Finite(c.V("頂点数", numbers.Count >= 6 ? numbers.Count / 2 : 5), 5), 3, 16);
			bool free = numbers.Count >= 6 && numbers.Count / 2 >= wanted;
			if (free) {
				for (int i = 0; i < wanted; i++) pts.Add(new Vector2((float)numbers[2 * i], (float)numbers[2 * i + 1]));
			} else {
				// no corners to read: a regular outline, one corner up
				double size = Finite(c.V("固定ｻｲｽﾞ"));
				if (size <= 1) size = 200;
				double aspect = Math.Clamp(Finite(c.V("縦横比")), -100, 100);
				double rx = size / 2 * (aspect > 0 ? 1 - aspect / 100 : 1), ry = size / 2 * (aspect < 0 ? 1 + aspect / 100 : 1);
				for (int i = 0; i < wanted; i++) {
					double a = (i * 360.0 / wanted - 90) * Math.PI / 180;
					pts.Add(new Vector2((float)(Math.Cos(a) * rx), (float)(Math.Sin(a) * ry)));
				}
			}
			double far = 1;
			foreach (var p in pts) far = Math.Max(far, p.Length());
			far = Math.Min(far, Aup2Gpu.MaxSide / 2 - 4);
			double line = Finite(c.V("ライン幅", 4000), 4000);
			if (line <= 0 || c.Flag("簡易塗り潰し")) line = 4000;
			int copies = (int)Math.Clamp(Finite(c.V("繰り返し描画数", 1), 1), 1, 12);
			double around = Finite(c.V("繰り返し分周", copies), copies);
			if (Math.Abs(around) < 1e-6) around = copies;
			var u = new Aup2Uniforms().I("u_n", pts.Count).F("u_line", line).I("u_copies", copies).F("u_step", 2 * Math.PI / around).V3("u_col", c.Color("色"));
			for (int i = 0; i < 8; i++) {
				var a = 2 * i < pts.Count ? pts[2 * i] : Vector2.Zero;
				var b = 2 * i + 1 < pts.Count ? pts[2 * i + 1] : Vector2.Zero;
				u.V4("u_a" + i.ToString(CultureInfo.InvariantCulture), a.X, a.Y, b.X, b.Y);
			}
			double side = 2 * Math.Ceiling(far) + 4;
			Make(c, side, side, PolygonFragment, u);
		}

		// a slice of a disc or of a ring, from the top, clockwise
		private static void Fan(Aup2Context c) {
			double size = Math.Clamp(Finite(c.V("サイズ", 100), 100), 0, Aup2Gpu.MaxSide - 4);
			double angle = Math.Clamp(Finite(c.V("中心角", 270), 270), -360, 360);
			double line = Finite(c.V("ライン幅", 2000), 2000);
			if (size < 1 || Math.Abs(angle) < 1e-3) { c.Skip = true; return; }
			Make(c, size + 4, size + 4, @"uniform float u_r; uniform float u_ang; uniform float u_line; uniform vec3 u_col;
void main(){
    vec2 p = (v_uv - 0.5) * u_size;
    // a negative angle opens the other way
    if (u_ang < 0.0) p.x = -p.x;
    float ang = abs(u_ang);
    float d = length(p) - u_r;
    float a = clamp(0.5 - d, 0.0, 1.0) * clamp(0.5 + d + u_line, 0.0, 1.0);
    if (ang < 6.2828) {
        vec2 e = vec2(sin(ang), -cos(ang));
        float s0 = p.x;
        float s1 = e.y * p.x - e.x * p.y;
        float inside = ang <= 3.14159265 ? min(s0, s1) : max(s0, s1);
        a *= clamp(0.5 + inside, 0.0, 1.0);
    }
    o = vec4(u_col * a, a);
}", new Aup2Uniforms().F("u_r", size / 2).F("u_ang", angle * Math.PI / 180).F("u_line", line <= 0 ? 4000 : line).V3("u_col", c.Color("色")));
		}
		#endregion

		#region Particles
		// stars that come from far away towards the eye, each one again and again
		private static void Stars(Aup2Context c) {
			int count = (int)Math.Clamp(Finite(c.V("個数", 1500), 1500), 0, MostPieces);
			double speed = Finite(c.V("速度", 6)) * 100, spread = Math.Abs(Finite(c.V("広がり", 12), 12)) * 100;
			double depth = Math.Max(100, Math.Abs(Finite(c.V("奥行き", 20), 20)) * 100);
			double size = Math.Clamp(Finite(c.V("サイズ", 30), 30), 1, 500);
			double fin = Math.Max(0, Finite(c.V("フェードイン時間", 0.15))), fout = Math.Max(0, Finite(c.V("フェードアウト時間", 0.15)));
			string shape = c.S("形状", "円");
			if (count == 0) { c.Skip = true; return; }
			double side = size + 4;
			if (!Make(c, side, side, PieceFragment, new Aup2Uniforms().I("u_kind", KindOf(shape)).V3("u_col", c.Color("色", new Vector3(0.867f, 0.867f, 1f)))
				.F("u_soft", KindOf(shape) == 1 ? 1 : 0).F("u_cells", 1).I("u_hue", 0))) return;
			double w = c.Item.ShapeWidth, h = c.Item.ShapeHeight;
			// one trip through the depth, and the share of it each fade takes
			double trip = Math.Abs(speed) > 1e-6 ? depth / Math.Abs(speed) : 0;
			double far = Nearest + depth;
			var parts = new List<Aup2Part>(count);
			for (int i = 0; i < count; i++) {
				double phase = c.Random("star.phase", i) + (trip > 0 ? c.Time * speed / depth : 0);
				phase -= Math.Floor(phase);
				double opacity = 1;
				if (trip > 0) {
					if (fin > 0) opacity = Math.Min(opacity, phase * trip / fin);
					if (fout > 0) opacity = Math.Min(opacity, (1 - phase) * trip / fout);
				}
				if (opacity <= 0) continue;
				var p = Piece(0, 0, w, h, w, h, (c.Random("star.x", i) * 2 - 1) * spread, (c.Random("star.y", i) * 2 - 1) * spread, far - phase * depth);
				p.Opacity = Math.Min(1, opacity);
				p.ScaleX = p.ScaleY = 0.5 + 0.5 * c.Random("star.size", i);
				parts.Add(p);
			}
			Finish(c, parts);
		}

		// streaks that fall across the frame and come back in from the other side
		private static void Rain(Aup2Context c) {
			int count = (int)Math.Clamp(Finite(c.V("個数", 300), 300), 0, MostPieces);
			double speed = Finite(c.V("速度", 30)) * 30, dir = Finite(c.V("方向")), spread = Finite(c.V("拡散"));
			double depth = Math.Max(0, Finite(c.V("奥行き", 500), 500)), length = Math.Clamp(Finite(c.V("長さ", 80), 80), 2, 1000);
			if (count == 0) { c.Skip = true; return; }
			if (!Make(c, 6, length + 4, @"uniform vec3 u_col;
void main(){
    vec2 p = v_uv * u_size;
    // thin, bright at the lower end, fading up its tail
    float across = clamp(1.5 - abs(p.x - u_size.x * 0.5), 0.0, 1.0);
    float along = clamp((p.y - 2.0) / max(u_size.y - 4.0, 1.0), 0.0, 1.0);
    float ends = clamp(p.y - 1.5, 0.0, 1.0) * clamp(u_size.y - 1.5 - p.y, 0.0, 1.0);
    float a = across * along * ends;
    o = vec4(u_col * a, a);
}", new Aup2Uniforms().V3("u_col", c.Color("色")))) return;
			double w = c.Item.ShapeWidth, h = c.Item.ShapeHeight;
			double hw = c.FrameW / 2.0 + length, hh = c.FrameH / 2.0 + length;
			var parts = new List<Aup2Part>(count);
			for (int i = 0; i < count; i++) {
				double a = (dir + (c.Random("rain.dir", i) - 0.5) * spread) * Math.PI / 180;
				double z = Math.Max(Nearest, c.Random("rain.z", i) * depth), k = (Eye + z) / Eye;
				double v = speed * (0.8 + 0.4 * c.Random("rain.v", i)) * c.Time;
				// placed on the frame, then pushed back to its depth so the frame stays covered
				double x = Wrap((c.Random("rain.x", i) * 2 - 1) * hw + Math.Sin(a) * v, hw);
				double y = Wrap((c.Random("rain.y", i) * 2 - 1) * hh + Math.Cos(a) * v, hh);
				var p = Piece(0, 0, w, h, w, h, x * k, y * k, z);
				p.RotZ = -a * 180 / Math.PI;
				p.ScaleX = p.ScaleY = k;
				p.Opacity = (0.35 + 0.65 * c.Random("rain.o", i)) / k;
				parts.Add(p);
			}
			Finish(c, parts);
		}

		// flakes that drift down, swaying from side to side
		private static void Snow(Aup2Context c) {
			int count = (int)Math.Clamp(Finite(c.V("個数", 300), 300), 0, MostPieces);
			double speed = Finite(c.V("速度", 3)) * 30, dir = Finite(c.V("方向")) * Math.PI / 180;
			double sway = Finite(c.V("ゆらぎ幅", 30)), swaySpeed = Finite(c.V("ゆらぎ速度", 1));
			double depth = Math.Max(0, Finite(c.V("奥行き", 500), 500)), size = Math.Clamp(Finite(c.V("サイズ", 12), 12), 1, 500);
			string shape = c.S("形状", "円");
			if (count == 0) { c.Skip = true; return; }
			double side = size + 4;
			if (!Make(c, side, side, PieceFragment, new Aup2Uniforms().I("u_kind", KindOf(shape)).V3("u_col", c.Color("色"))
				.F("u_soft", KindOf(shape) == 1 ? 0.6 : 0).F("u_cells", 1).I("u_hue", 0))) return;
			double w = c.Item.ShapeWidth, h = c.Item.ShapeHeight;
			double margin = size + Math.Abs(sway);
			double hw = c.FrameW / 2.0 + margin, hh = c.FrameH / 2.0 + margin;
			var parts = new List<Aup2Part>(count);
			for (int i = 0; i < count; i++) {
				double z = Math.Max(Nearest, c.Random("snow.z", i) * depth), k = (Eye + z) / Eye;
				double v = speed * (0.6 + 0.8 * c.Random("snow.v", i)) * c.Time;
				double swing = Math.Sin((c.Time * swaySpeed * (0.5 + c.Random("snow.f", i)) + c.Random("snow.p", i)) * Math.PI * 2) * sway;
				double x = Wrap((c.Random("snow.x", i) * 2 - 1) * hw + Math.Sin(dir) * v + Math.Cos(dir) * swing, hw);
				double y = Wrap((c.Random("snow.y", i) * 2 - 1) * hh + Math.Cos(dir) * v - Math.Sin(dir) * swing, hh);
				var p = Piece(0, 0, w, h, w, h, x * k, y * k, z);
				p.ScaleX = p.ScaleY = (0.4 + 0.6 * c.Random("snow.s", i)) * k;
				p.RotZ = c.Random("snow.r", i) * 360 + c.Time * 40 * (c.Random("snow.t", i) - 0.5);
				p.Opacity = 0.5 + 0.5 * c.Random("snow.o", i);
				parts.Add(p);
			}
			Finish(c, parts);
		}

		// small shapes spread through a box around the object, each turning on its own
		private static void Scatter(Aup2Context c) {
			int count = (int)Math.Clamp(Finite(c.V("数", 50), 50), 0, MostPieces);
			double range = Math.Abs(Finite(c.V("範囲", 500), 500)), size = Math.Clamp(Finite(c.V("サイズ", 30), 30), 1, 500);
			double spin = Finite(c.V("回転速度", 100));
			bool colours = c.Flag("ランダム色");
			if (count == 0) { c.Skip = true; return; }
			int cells = colours ? 8 : 1;
			double side = Math.Ceiling(size) + 4;
			if (!Make(c, side * cells, side, PieceFragment, new Aup2Uniforms().I("u_kind", KindOf(c.S("形状", "四角形"))).V3("u_col", c.Color("色"))
				.F("u_soft", 0).F("u_cells", cells).I("u_hue", colours ? 1 : 0))) return;
			double w = c.Item.ShapeWidth, h = c.Item.ShapeHeight, cw = w / cells;
			bool camera = c.Item.UnderCamera;
			var parts = new List<Aup2Part>(count);
			for (int i = 0; i < count; i++) {
				int cell = Math.Min(cells - 1, (int)(c.Random("prop.c", i) * cells));
				double z = (c.Random("prop.z", i) * 2 - 1) * range;
				if (!camera) z = Math.Max(Nearest, z);
				var p = Piece(cell * cw, 0, cw, h, w, h, (c.Random("prop.x", i) * 2 - 1) * range, (c.Random("prop.y", i) * 2 - 1) * range, z);
				p.RotX = c.Random("prop.rx", i) * 360 + c.Time * spin * (c.Random("prop.sx", i) * 2 - 1);
				p.RotY = c.Random("prop.ry", i) * 360 + c.Time * spin * (c.Random("prop.sy", i) * 2 - 1);
				p.RotZ = c.Random("prop.rz", i) * 360 + c.Time * spin * (c.Random("prop.sz", i) * 2 - 1);
				parts.Add(p);
			}
			Finish(c, parts);
		}
		#endregion

		#region Procedural pictures
		private static void Clouds(Aup2Context c) {
			double speed = Finite(c.V("速度", 10)) * 10, dir = Finite(c.V("方向")) * Math.PI / 180;
			double density = Math.Clamp(Finite(c.V("濃さ", 50), 50) / 100, 0, 1);
			double tilt = Math.Clamp(Finite(c.V("傾き")), -85, 85) * Math.PI / 180;
			double air = Math.Max(0, Finite(c.V("空気遠近"))) / 100 * 3;
			double shift = Finite(c.V("位置補正")), seed = Finite(c.V("形状"));
			double travel = speed * c.Time + shift;
			Make(c, c.FrameW, c.FrameH, @"uniform vec2 u_off; uniform float u_dens; uniform float u_tilt; uniform float u_air; uniform float u_morph; uniform float u_seed; uniform vec3 u_col;
float h(vec2 p){ p = fract(p * vec2(0.1031, 0.1030)); p += dot(p, p.yx + 33.33); return fract((p.x + p.y) * p.x); }
float n(vec2 p){
    vec2 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(h(i), h(i + vec2(1.0, 0.0)), f.x), mix(h(i + vec2(0.0, 1.0)), h(i + vec2(1.0, 1.0)), f.x), f.y);
}
float fbm(vec2 p){
    float s = 0.0, a = 0.5;
    for (int i = 0; i < 5; i++) {
        // each layer slides its own way as time goes: the shapes change
        s += a * n(p + u_morph * vec2(float(i) * 0.37 + 0.2, -0.31 * float(i) - 0.1));
        p = p * 2.03 + vec2(17.0, 9.0);
        a *= 0.5;
    }
    return s;
}
void main(){
    vec2 p = (v_uv - 0.5) * u_size;
    // the clouds lie on a plane leaning away from the eye
    const float f = 1024.0;
    float cs = cos(u_tilt), sn = sin(u_tilt);
    float den = f * cs - p.y * sn;
    if (den <= 1.0) { o = vec4(0.0); return; }
    float v = p.y * f / den;
    float depth = v * sn;
    float u = p.x * (f + depth) / f;
    float cl = fbm((vec2(u, v) + u_off) / 256.0 + u_seed * 37.0);
    float thr = (1.0 - u_dens) * 0.9;
    float a = smoothstep(thr - 0.15, thr + 0.25, cl);
    a *= exp(-u_air * max(depth, 0.0) / f);
    // the far side meets the horizon softly
    a *= clamp(den / (f * 0.15), 0.0, 1.0);
    vec3 col = u_col * (0.72 + 0.28 * smoothstep(thr, thr + 0.5, cl));
    o = vec4(col * a, a);
}", new Aup2Uniforms().V2("u_off", -Math.Sin(dir) * travel, Math.Cos(dir) * travel).F("u_dens", density).F("u_tilt", tilt).F("u_air", air)
				.F("u_morph", c.Time * 0.15 * Finite(c.V("変化速度", 1), 1)).F("u_seed", seed % 64).V3("u_col", c.Color("色")));
		}

		private static void Water(Aup2Context c) {
			double speed = Finite(c.V("速度", 10)) * 10, dir = Finite(c.V("方向")) * Math.PI / 180;
			double tilt = Math.Clamp(Finite(c.V("傾き")), -85, 85) * Math.PI / 180;
			double ripple = Finite(c.V("波紋")) / 100, width = Math.Max(1, Math.Abs(Finite(c.V("波紋幅", 30), 30)));
			double travel = speed * c.Time + Finite(c.V("位置補正"));
			Make(c, c.FrameW, c.FrameH, @"uniform vec2 u_off; uniform float u_tilt; uniform float u_time; uniform float u_ripple; uniform float u_rwidth; uniform float u_rpos;
uniform float u_rphase; uniform float u_blur; uniform vec3 u_col;
float h(vec2 p){ p = fract(p * vec2(0.1031, 0.1030)); p += dot(p, p.yx + 33.33); return fract((p.x + p.y) * p.x); }
float n(vec2 p){
    vec2 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(h(i), h(i + vec2(1.0, 0.0)), f.x), mix(h(i + vec2(0.0, 1.0)), h(i + vec2(1.0, 1.0)), f.x), f.y);
}
// the height of the water at a place of its plane, in pixels
float height(vec2 q){
    vec2 p = (q + u_off) / 90.0;
    float s = 0.0, a = 0.5;
    for (int i = 0; i < 4; i++) {
        s += a * (1.0 - abs(n(p + u_time * vec2(0.21 * float(i) + 0.1, 0.17)) * 2.0 - 1.0));
        p = p * 1.9 + vec2(11.0, 5.0);
        a *= 0.5;
    }
    float r = length(q - vec2(0.0, u_rpos));
    s += u_ripple * sin(r / u_rwidth * 6.2831853 - u_rphase) * exp(-r / (u_rwidth * 8.0));
    return s;
}
void main(){
    vec2 p = (v_uv - 0.5) * u_size;
    const float f = 1024.0;
    float cs = cos(u_tilt), sn = sin(u_tilt);
    float den = f * cs - p.y * sn;
    if (den <= 1.0) { o = vec4(0.0); return; }
    float v = p.y * f / den;
    float u = p.x * (f + v * sn) / f;
    vec2 q = vec2(u, v);
    float h0 = height(q);
    vec2 g = vec2(height(q + vec2(3.0, 0.0)) - h0, height(q + vec2(0.0, 3.0)) - h0);
    // lit from the upper left, with a glint on the steep sides
    float light = clamp(0.62 + h0 * 0.3 + dot(g, vec2(-1.6, -2.2)), 0.0, 1.5);
    float glint = pow(clamp(dot(g, vec2(-3.0, -5.0)), 0.0, 1.0), 3.0);
    vec3 col = clamp(u_col * light + vec3(glint) * 0.6, 0.0, 1.0);
    float a = abs(sn) > 1.0e-4 ? clamp(den / abs(sn) / max(u_blur, 0.5), 0.0, 1.0) : 1.0;
    o = vec4(col * a, a);
}", new Aup2Uniforms().V2("u_off", -Math.Sin(dir) * travel, Math.Cos(dir) * travel).F("u_tilt", tilt).F("u_time", c.Time * 0.4)
				.F("u_ripple", ripple).F("u_rwidth", width).F("u_rpos", Finite(c.V("波紋位置"))).F("u_rphase", (c.Time * Finite(c.V("波紋速度", 5), 5)) % (Math.PI * 2000))
				.F("u_blur", Math.Max(0, Finite(c.V("水平境界ぼかし", 20), 20))).V3("u_col", c.Color("色", new Vector3(0.2f, 0.5f, 0.8f))));
		}
		#endregion

		#region Overlays
		// wedges that point at the centre of the frame, drawn anew a few times a second
		private static void FocusLines(Aup2Context c) {
			double density = Math.Clamp(Finite(c.V("濃さ", 40), 40) / 100, 0, 1), speed = Math.Max(0, Finite(c.V("速さ", 25), 25));
			double inner = Math.Max(0, Finite(c.V("中心幅", 300), 300)) / 2;
			if (density <= 0) { c.Skip = true; return; }
			Make(c, c.FrameW, c.FrameH, @"uniform float u_dens; uniform float u_seed; uniform float u_inner; uniform vec3 u_col;
float h(float x){ return fract(sin(x * 127.1 + u_seed * 31.7) * 43758.5453); }
void main(){
    vec2 p = (v_uv - 0.5) * u_size;
    float r = length(p);
    float ang = atan(p.y, p.x) / 6.2831853 + 0.5;
    float reach = length(u_size) * 0.5;
    float a = 0.0;
    for (int k = 0; k < 2; k++) {
        float n = k == 0 ? 240.0 : 97.0;
        float cell = min(floor(ang * n), n - 1.0) + float(k) * 1000.0;
        float f = fract(ang * n) - 0.5;
        if (h(cell) < u_dens) {
            // each line starts a little way from the centre and widens outwards
            float start = u_inner * (0.6 + 0.8 * h(cell + 517.0));
            float grow = clamp((r - start) / max(reach - start, 1.0), 0.0, 1.0);
            float halfw = 0.5 * grow * (0.3 + 0.7 * h(cell + 233.0));
            float px = r * 6.2831853 / n;
            a = max(a, clamp((halfw - abs(f)) * px + 0.5, 0.0, 1.0) * step(start, r));
        }
    }
    o = vec4(u_col * a, a);
}", new Aup2Uniforms().F("u_dens", density).F("u_seed", Math.Floor(c.Time * speed) % 4096 + c.Random("focus") * 16).F("u_inner", inner).V3("u_col", c.Color("色")));
		}

		private static void ScanLines(Aup2Context c) {
			double alpha = Math.Clamp(Finite(c.V("濃さ", 50), 50) / 100, 0, 1), width = Math.Max(1, Finite(c.V("ライン幅", 2), 2));
			double period = Math.Max(width + 1, width * 2 + Finite(c.V("間隔補正")));
			if (alpha <= 0) { c.Skip = true; return; }
			Make(c, c.FrameW, c.FrameH, @"uniform float u_w; uniform float u_period; uniform float u_a; uniform vec3 u_col;
void main(){
    float y = v_uv.y * u_size.y;
    float a = mod(y, u_period) < u_w ? u_a : 0.0;
    o = vec4(u_col * a, a);
}", new Aup2Uniforms().F("u_w", width).F("u_period", period).F("u_a", alpha).V3("u_col", c.Color("色", Vector3.Zero)));
		}
		#endregion

		#region Flares
		// how bright a blinking light is on this frame
		private static double Flicker(Aup2Context c, double amount) =>
			1 - Math.Clamp(amount, 0, 1) * c.Random("blink", (long)Math.Floor(c.Frame));

		// a glowing core, rays around it and long streaks to both sides
		private static void Flare(Aup2Context c) {
			double radius = Math.Clamp(Finite(c.V("サイズ", 300), 300), 2, 4000) / 2;
			double reach = radius * (1 + Math.Max(0, Finite(c.V("光芒長さ", 85), 85)) / 100);
			double rays = Math.Clamp(Math.Round(Finite(c.V("光芒数", 35), 35)), 0, 400), amount = Math.Max(0, Finite(c.V("光芒強さ", 0.5), 0.5));
			double streak = Math.Max(0, Finite(c.V("水平光芒", 800), 800)) / 2, streakHeight = Math.Max(0.5, Math.Abs(Finite(c.V("水平光芒高さ", 0.1), 0.1)) * radius);
			int streaks = (int)Math.Clamp(Finite(c.V("水平光芒数", 1), 1), 0, 8);
			double w = 2 * Math.Max(reach, streaks > 0 ? streak : 0) + 4, h = 2 * Math.Max(reach, streaks > 0 ? Math.Min(streakHeight * 3 * streaks, 2000) : 0) + 4;
			Make(c, w, h, @"uniform float u_core; uniform float u_reach; uniform float u_rays; uniform float u_amt; uniform float u_t;
uniform float u_hlen; uniform float u_hh; uniform int u_hnum; uniform float u_gain; uniform vec3 u_col;
float h(vec2 p){ return fract(sin(dot(p, vec2(91.3458, 47.9898))) * 43758.5453); }
// a noise that closes on itself after u_rays steps
float ring(float x, float t){
    float i = floor(x), f = fract(x), j = floor(t), g = fract(t);
    f = f * f * (3.0 - 2.0 * f); g = g * g * (3.0 - 2.0 * g);
    float a = mod(i, u_rays), b = mod(i + 1.0, u_rays);
    return mix(mix(h(vec2(a, j)), h(vec2(b, j)), f), mix(h(vec2(a, j + 1.0)), h(vec2(b, j + 1.0)), f), g);
}
void main(){
    vec2 p = (v_uv - 0.5) * u_size;
    float r = length(p);
    float glow = pow(clamp(1.0 - r / u_core, 0.0, 1.0), 2.0);
    float hot = exp(-pow(r / (u_core * 0.15), 2.0));
    float ray = 0.0;
    if (u_rays >= 1.0) {
        float ang = atan(p.y, p.x) / 6.2831853 + 0.5;
        ray = pow(ring(ang * u_rays, u_t), 3.0) * pow(clamp(1.0 - r / u_reach, 0.0, 1.0), 2.0) * u_amt * 2.0;
    }
    float side = 0.0;
    for (int k = 0; k < 8; k++) {
        if (k >= u_hnum) break;
        float fk = float(k);
        float len = u_hlen / (1.0 + 0.6 * fk), tall = u_hh * (1.0 + 1.2 * fk);
        side += exp(-pow(p.y / tall, 2.0)) * pow(clamp(1.0 - abs(p.x) / max(len, 1.0), 0.0, 1.0), 2.0) / (1.0 + fk);
    }
    vec3 col = (u_col * (glow + ray + side * 0.8) + vec3(hot)) * u_gain;
    col = clamp(col, 0.0, 1.0);
    o = vec4(col, max(col.r, max(col.g, col.b)));
}", new Aup2Uniforms().F("u_core", radius).F("u_reach", reach).F("u_rays", rays).F("u_amt", amount).F("u_t", (c.Time * Finite(c.V("光芒変化速度", 0.2), 0.2) * 4) % 4096)
				.F("u_hlen", streak).F("u_hh", streakHeight).I("u_hnum", streaks).F("u_gain", Flicker(c, Finite(c.V("点滅"))))
				.V3("u_col", c.Color("光芒色", new Vector3(0.6f, 0.6f, 1f))));
		}

		// a light with rays, rainbow rings around it and a row of pale shapes along a direction
		private static void LensFlare(Aup2Context c) {
			double dx = Finite(c.V("方向X", 350), 350), dy = Finite(c.V("方向Y", 300), 300);
			double size = Math.Clamp(Finite(c.V("サイズ", 230), 230), 2, 4000);
			double rayZoom = Math.Max(0.01, Math.Abs(Finite(c.V("光芒拡大率", 1.1), 1.1))), rayLen = Math.Max(0.01, Math.Abs(Finite(c.V("光芒長さ", 1.3), 1.3)));
			double rays = Math.Clamp(Math.Round(Finite(c.V("光芒数", 30), 30)), 0, 400);
			double rainbowZoom = Math.Max(0.01, Math.Abs(Finite(c.V("虹輪拡大率", 1.2), 1.2))), haloZoom = Math.Max(0.01, Math.Abs(Finite(c.V("光輪拡大率", 1), 1)));
			int rainbows = (int)Math.Clamp(Finite(c.V("虹輪数", 1), 1), 0, 4);
			string shape = c.S("形状", "円");
			int sides = shape switch { "三角形" => 3, "四角形" => 4, "五角形" => 5, "六角形" => 6, _ => 0 };
			double core = size / 2 * rayZoom;
			double ghost = size * haloZoom * 0.4, ringFar = size * rainbowZoom * (1 + 0.35 * Math.Max(0, rainbows - 1)) * 1.4;
			double hx = Math.Min(2048, Math.Max(Math.Max(core * rayLen, ringFar), Math.Abs(dx) * 1.7 + ghost) + 2);
			double hy = Math.Min(2048, Math.Max(Math.Max(core * rayLen, ringFar), Math.Abs(dy) * 1.7 + ghost) + 2);
			Make(c, hx * 2, hy * 2, @"uniform vec2 u_dir; uniform float u_core; uniform float u_reach; uniform float u_rays; uniform float u_amt; uniform float u_t;
uniform float u_rbAmt; uniform float u_rbRad; uniform float u_rbOpen; uniform int u_rbN;
uniform float u_haloAmt; uniform float u_haloRad; uniform float u_rot; uniform int u_sides; uniform int u_screen; uniform float u_gain; uniform vec3 u_col;
float h(vec2 p){ return fract(sin(dot(p, vec2(91.3458, 47.9898))) * 43758.5453); }
float ring(float x, float t){
    float i = floor(x), f = fract(x), j = floor(t), g = fract(t);
    f = f * f * (3.0 - 2.0 * f); g = g * g * (3.0 - 2.0 * g);
    float a = mod(i, u_rays), b = mod(i + 1.0, u_rays);
    return mix(mix(h(vec2(a, j)), h(vec2(b, j)), f), mix(h(vec2(a, j + 1.0)), h(vec2(b, j + 1.0)), f), g);
}
vec3 spectrum(float x){ return clamp(vec3(abs(x * 6.0 - 3.0) - 1.0, 2.0 - abs(x * 6.0 - 2.0), 2.0 - abs(x * 6.0 - 4.0)), 0.0, 1.0); }
// how far q is across a shape of radius 1: a disc, or a polygon turned by u_rot
float across(vec2 q){
    if (u_sides < 3) return length(q);
    float an = 3.14159265 / float(u_sides);
    float a = atan(q.x, -q.y) + u_rot;
    float s = mod(a + an, 2.0 * an) - an;
    return length(q) * cos(s) / cos(an);
}
void main(){
    vec2 p = (v_uv - 0.5) * u_size;
    float r = length(p);
    float glow = pow(clamp(1.0 - r / u_core, 0.0, 1.0), 2.0);
    float hot = exp(-pow(r / (u_core * 0.12), 2.0));
    float ray = 0.0;
    if (u_rays >= 1.0) {
        float ang = atan(p.y, p.x) / 6.2831853 + 0.5;
        ray = pow(ring(ang * u_rays, u_t), 3.0) * pow(clamp(1.0 - r / u_reach, 0.0, 1.0), 2.0);
    }
    vec3 acc = u_col * (glow * 0.6 + ray) * u_amt * 2.0 + vec3(hot);
    // the rainbow rings open towards the direction
    float dl = length(u_dir);
    float facing = dl > 0.001 && r > 0.001 ? dot(p / r, u_dir / dl) * 0.5 + 0.5 : 1.0;
    float arc = smoothstep(1.0 - u_rbOpen - 0.2, 1.0 - u_rbOpen, facing);
    for (int k = 0; k < 4; k++) {
        if (k >= u_rbN) break;
        float rad = u_rbRad * (1.0 + 0.35 * float(k));
        float wide = rad * 0.12;
        float x = (r - rad) / wide;
        acc += spectrum(clamp(0.5 - x * 0.25, 0.0, 0.83)) * exp(-x * x) * arc * u_rbAmt / (1.0 + float(k));
    }
    // the pale shapes strung along the direction
    for (int i = 0; i < 7; i++) {
        float fi = float(i);
        float t = i == 6 ? -0.3 : 0.3 + fi * 0.24;
        float rad = u_haloRad * (0.1 + 0.3 * h(vec2(fi, 3.0)));
        float s = across((p - u_dir * t) / max(rad, 1.0));
        float fill = smoothstep(1.0, 0.92, s) * (0.4 + 0.6 * s * s);
        vec3 g = mix(u_col, spectrum(h(vec2(fi, 9.0)) * 0.83), 0.35) * fill * u_haloAmt;
        acc = u_screen == 1 ? 1.0 - (1.0 - clamp(acc, 0.0, 1.0)) * (1.0 - clamp(g, 0.0, 1.0)) : acc + g;
    }
    vec3 col = clamp(acc * u_gain, 0.0, 1.0);
    o = vec4(col, max(col.r, max(col.g, col.b)));
}", new Aup2Uniforms().V2("u_dir", dx, dy).F("u_core", core).F("u_reach", core * rayLen).F("u_rays", rays).F("u_amt", Math.Max(0, Finite(c.V("光芒強さ", 0.5), 0.5)))
				.F("u_t", (c.Time * Finite(c.V("光芒変化速度", 1.1), 1.1) * 4) % 4096)
				.F("u_rbAmt", Math.Max(0, Finite(c.V("虹輪強さ", 0.6), 0.6))).F("u_rbRad", size * rainbowZoom).F("u_rbOpen", Finite(c.V("虹輪開閉率", 1.1), 1.1)).I("u_rbN", rainbows)
				.F("u_haloAmt", Math.Max(0, Finite(c.V("光輪強さ", 0.15), 0.15))).F("u_haloRad", size * haloZoom).F("u_rot", Finite(c.V("形状角度", 20)) * Math.PI / 180)
				.I("u_sides", sides).I("u_screen", c.Flag("光輪ｽｸﾘｰﾝ合成") ? 1 : 0).F("u_gain", Flicker(c, Finite(c.V("点滅", 0.2), 0.2)))
				.V3("u_col", c.Color("光芒色")));
		}
		#endregion

		#region Sound
		private static double At(Aup2Object o, Aup2Effect e, string key, double frame, double fallback = 0) =>
			Finite(e.Track(key, fallback).Evaluate(frame, o.Frames, o.Id * 131 + key.GetHashCode(StringComparison.Ordinal)), fallback);

		// where a sound is in its own time: a playback range says it outright, a position counts on at the speed
		private static double SoundTime(Aup2Object o, Aup2Effect e, double frame, double rate, double speed) {
			var track = e.Track("再生位置");
			double at = At(o, e, "再生位置", frame);
			if (track.Motion == Aup2Motion.PlayRange && track.Values.Length >= 2 && track.Values[0] != track.Values[^1]) return at;
			return at + frame / rate * speed;
		}

		// The sound of a file at the object's time, as a line or as bars: the samples of one frame, or how
		// loud each band is. With no file it is every sound object of the scene playing on this frame.
		private static void Waveform(Aup2Context c) {
			int w = (int)Math.Clamp(Math.Ceiling(Finite(c.V("横幅", 800), 800)), 1, Aup2Gpu.MaxSide);
			int h = (int)Math.Clamp(Math.Ceiling(Finite(c.V("高さ", 400), 400)), 1, Aup2Gpu.MaxSide);
			c.Item.ShapeWidth = w; c.Item.ShapeHeight = h;
			if (c.Gpu == null) return;
			var source = Aup2Samples.Of(c.Scene);
			if (source == null) { c.Skip = true; return; }

			double gain = Math.Max(0, Finite(c.V("音量", 100), 100)) / 100;
			double speed = Finite(c.V("再生速度", 100), 100) / 100;
			var sounds = new List<Aup2Waveform.Sound>();
			string file = c.S("ファイル");
			if (file.Length > 0) {
				var table = source.Samples(file);
				if (table != null && table.Ready)
					sounds.Add(new Aup2Waveform.Sound { Table = table, At = SoundTime(c.Object, c.Effect, c.Frame, c.Scene.Rate, speed), Gain = gain });
			} else {
				speed = 1;
				foreach (var o in c.Scene.Objects) {
					if (o.Effects.Count == 0 || o.Effects[0].Name != Aup2Evaluator.AudioFile || c.Now < o.Start || c.Now >= o.End + 1) continue;
					var e = o.Effects[0];
					var table = source.Samples(e.Get("ファイル") ?? "");
					if (table == null || !table.Ready) continue;
					double f = c.Now - o.Start;
					var play = o.Find(Aup2Evaluator.AudioPlay);
					double own = play == null ? 1 : At(o, play, "音量", f, 100) / 100;
					sounds.Add(new Aup2Waveform.Sound { Table = table, At = SoundTime(o, e, f, c.Scene.Rate, At(o, e, "再生速度", f, 100) / 100), Gain = gain * own });
				}
			}
			if (sounds.Count == 0) { c.Skip = true; return; }

			int across = (int)Math.Clamp(Finite(c.V("横解像度")), 0, 2048), levels = (int)Math.Clamp(Finite(c.V("縦解像度")), 0, 4096);
			bool spectrum = c.Flag("スペクトラム表示");
			int n = across > 0 ? Math.Min(across, w) : Math.Min(w, spectrum ? 256 : 2048);
			var lo = new float[n];
			var hi = new float[n];
			if (spectrum) Aup2Waveform.Spectrum(sounds, c.Flag("ミラー表示"), lo, hi);
			else Aup2Waveform.Wave(sounds, Math.Max(0.01, Math.Abs(speed)) / c.Scene.Rate, across == 0, lo, hi);

			// the columns go to the shader as a picture one pixel high
			var bytes = Aup2Waveform.Pack(lo, hi);
			var gl = FDK.Game.Gl;
			uint tex = gl.GenTexture();
			gl.BindTexture(TextureTarget.Texture2D, tex);
			unsafe { fixed (byte* p = bytes) gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)n, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, p); }
			gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
			gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
			gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
			gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
			Make(c, w, h, @"uniform sampler2D u_data; uniform float u_n; uniform vec2 u_gap; uniform float u_levels; uniform float u_thin; uniform vec3 u_col;
void main(){
    vec2 p = vec2(v_uv.x, 1.0 - v_uv.y) * u_size;
    float cw = u_size.x / u_n;
    float i = min(floor(p.x / cw), u_n - 1.0);
    float a = 1.0;
    if (u_gap.x > 0.0 && cw - u_gap.x >= 1.0 && p.x - i * cw >= cw - u_gap.x) a = 0.0;
    vec4 t = texelFetch(u_data, ivec2(int(i), 0), 0) * 255.0;
    float lo = (t.r * 256.0 + t.g) / 65535.0, hi = (t.b * 256.0 + t.a) / 65535.0;
    if (u_levels > 0.0) {
        // the height counts in whole steps, with a gap under each
        lo = floor(lo * u_levels + 0.5) / u_levels;
        hi = floor(hi * u_levels + 0.5) / u_levels;
        float step_ = u_size.y / u_levels;
        if (u_gap.y > 0.0 && step_ - u_gap.y >= 1.0 && mod(p.y, step_) < u_gap.y) a = 0.0;
    }
    float l = lo * u_size.y, h = hi * u_size.y;
    if (h - l < u_thin) { float m = (l + h) * 0.5; l = m - u_thin * 0.5; h = m + u_thin * 0.5; }
    a *= h - l > 0.01 ? clamp(p.y - l + 0.5, 0.0, 1.0) * clamp(h - p.y + 0.5, 0.0, 1.0) : 0.0;
    o = vec4(u_col * a, a);
}", new Aup2Uniforms().Tex("u_data", tex).F("u_n", n).V2("u_gap", Math.Max(0, Finite(c.V("横スペース"))), Math.Max(0, Finite(c.V("縦スペース"))))
				.F("u_levels", levels > 0 && levels < h ? levels : 0).F("u_thin", spectrum ? 0 : 1.5).V3("u_col", c.Color("波形の色")));
			gl.BindTexture(TextureTarget.Texture2D, 0);
			gl.DeleteTexture(tex);
		}
		#endregion

		#region Trail
		// A line along the way the object's own placing moves it. The picture holds a bar, a dot and an
		// arrow head; the line is the bar laid along each step of the way, with a dot on every joint.
		private static void Trail(Aup2Context c) {
			var draw = c.Object.Find(Aup2Evaluator.StandardDraw);
			if (draw == null) { c.Skip = true; return; }
			var tx = draw.Track("X");
			var ty = draw.Track("Y");
			// an object that does not move has no way to follow
			if (!tx.IsAnimated && !ty.IsAnimated) { c.Skip = true; return; }
			int total = Math.Max(1, c.Object.End - c.Object.Start);
			int n = Math.Min(total, 600);
			int seedX = c.Seed("X"), seedY = c.Seed("Y");
			var pts = new Vector2[n + 1];
			var run = new double[n + 1];
			for (int i = 0; i <= n; i++) {
				double f = (double)total * i / n;
				pts[i] = new Vector2((float)Finite(tx.Evaluate(f, c.Object.Frames, seedX)), (float)Finite(ty.Evaluate(f, c.Object.Frames, seedY)));
				run[i] = i == 0 ? 0 : run[i - 1] + (pts[i] - pts[i - 1]).Length();
			}
			if (run[n] < 0.5) { c.Skip = true; return; }
			var here = new Vector2((float)Finite(tx.Evaluate(c.Frame, c.Object.Frames, seedX)), (float)Finite(ty.Evaluate(c.Frame, c.Object.Frames, seedY)));

			// how much of the way is drawn: by time, or by distance at a steady speed
			double share = Math.Clamp(Finite(c.V("主線描画(%)", 100), 100) / 100, 0, 1);
			double stop;
			if (c.Flag("固定速度")) {
				double target = share * run[n];
				int k = 0;
				while (k < n - 1 && run[k + 1] < target) k++;
				double step = run[k + 1] - run[k];
				stop = k + (step > 1e-9 ? Math.Clamp((target - run[k]) / step, 0, 1) : 0);
			} else stop = share * n;
			if (stop <= 1e-6) { c.Skip = true; return; }

			double line = Math.Clamp(Finite(c.V("ライン幅", 4), 4), 1, 500);
			double tip = Finite(c.V("先端"));
			if (tip > 0) tip = Math.Clamp(Math.Max(tip, line * 3), 4, 1000);
			else tip = 0;
			double s = Math.Ceiling(line) + 4, t = tip > 0 ? Math.Ceiling(tip) + 4 : 0;
			double tipAt = 24 + s;
			if (!Make(c, tipAt + t + 4, Math.Max(s, t), @"uniform float u_lw; uniform float u_tip; uniform vec3 u_col;
void main(){
    vec2 p = v_uv * u_size;
    float s = ceil(u_lw) + 4.0;
    float a = 0.0;
    if (p.x < 16.0) {
        // the bar: only its long edges are soft
        if (p.y < s) a = clamp(u_lw * 0.5 + 0.5 - abs(p.y - s * 0.5), 0.0, 1.0);
    } else if (p.x >= 20.0 && p.x < 20.0 + s) {
        if (p.y < s) a = clamp(u_lw * 0.5 + 0.5 - length(p - vec2(20.0 + s * 0.5, s * 0.5)), 0.0, 1.0);
    } else if (p.x >= 24.0 + s && u_tip > 0.0) {
        // the head points right
        float t = ceil(u_tip) + 4.0;
        float hh = u_tip * 0.5;
        vec2 q = p - vec2(24.0 + s + t * 0.5, t * 0.5);
        float d = max(-q.x - hh, abs(q.y) * 0.8944 + (q.x - hh) * 0.4472);
        if (p.y < t) a = clamp(0.5 - d, 0.0, 1.0);
    }
    o = vec4(u_col * a, a);
}", new Aup2Uniforms().F("u_lw", line).F("u_tip", tip).V3("u_col", c.Color("色")))) return;
			double w = c.Item.ShapeWidth, h = c.Item.ShapeHeight;

			var parts = new List<Aup2Part>();
			int last = (int)Math.Min(n - 1, Math.Floor(stop));
			Vector2 end = pts[0], heading = new Vector2(1, 0);
			for (int i = 0; i <= last; i++) {
				double part = Math.Clamp(stop - i, 0, 1);
				if (part <= 0) break;
				Vector2 a = pts[i], b = pts[i] + (pts[i + 1] - pts[i]) * (float)part;
				var d = b - a;
				double len = d.Length();
				end = b;
				if (i == 0) parts.Add(Piece(20, 0, s, s, w, h, a.X - here.X, a.Y - here.Y, 0));
				if (len < 1e-3) continue;
				heading = d / (float)len;
				var mid = (a + b) / 2 - here;
				var bar = Piece(4, 0, 8, s, w, h, mid.X, mid.Y, 0);
				bar.ScaleX = len / 8;
				bar.RotZ = Math.Atan2(d.Y, d.X) * 180 / Math.PI;
				parts.Add(bar);
				parts.Add(Piece(20, 0, s, s, w, h, b.X - here.X, b.Y - here.Y, 0));
			}
			if (tip > 0) {
				var at = end - here + heading * (float)Finite(c.V("先端位置補正"));
				var head = Piece(tipAt, 0, t, t, w, h, at.X, at.Y, 0);
				head.RotZ = Math.Atan2(heading.Y, heading.X) * 180 / Math.PI + Finite(c.V("先端角度"));
				parts.Add(head);
			}
			Finish(c, parts);
		}
		#endregion
	}
}
