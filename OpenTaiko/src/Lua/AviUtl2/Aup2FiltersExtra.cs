using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace OpenTaiko.AviUtl2 {
	// The remaining picture effects.
	internal static class Aup2FiltersExtra {
		public static void Register(Dictionary<string, Aup2FilterFn> map) {
			map["基本出力"] = BasicOutput;
			map["グラデーションマップ"] = GradientMap;
			map["拡張色調補正"] = ExtendedTone;
			map["メッシュ変形"] = Mesh;
			map["モーションブラー"] = MotionBlur;
			map["ローテーション"] = QuarterTurns;
			map["周辺ボケ光量"] = Vignette;
			map["揺らぎ"] = Wobble;
			map["画像合成"] = ImageComposite;
		}

		#region Shaders
		private const string FsCopy = "void main(){ o = texture(u_src, v_uv); }";

		private const string FsMap = @"uniform float u_strength; uniform vec3 u_dark; uniform vec3 u_light; uniform int u_interp;
vec3 toHsv(vec3 c){
    vec4 k = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    vec4 p = mix(vec4(c.bg, k.wz), vec4(c.gb, k.xy), step(c.b, c.g));
    vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));
    float d = q.x - min(q.w, q.y);
    return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + 1.0e-10)), d / (q.x + 1.0e-10), q.x);
}
vec3 toRgb(vec3 c){
    vec3 p = abs(fract(c.xxx + vec3(1.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0);
    return c.z * mix(vec3(1.0), clamp(p - 1.0, 0.0, 1.0), c.y);
}
void main(){
    vec4 p = texture(u_src, v_uv);
    vec3 col = p.a > 0.0 ? p.rgb / p.a : vec3(0.0);
    float t = clamp(dot(col, vec3(0.299, 0.587, 0.114)), 0.0, 1.0);
    vec3 r;
    if (u_interp == 1) {
        // the short way around the hue circle
        vec3 a = toHsv(u_dark), b = toHsv(u_light);
        float dh = b.x - a.x;
        if (dh > 0.5) dh -= 1.0;
        if (dh < -0.5) dh += 1.0;
        r = toRgb(vec3(fract(a.x + dh * t), mix(a.yz, b.yz, t)));
    } else if (u_interp == 2) {
        r = pow(mix(pow(u_dark, vec3(2.2)), pow(u_light, vec3(2.2)), t), vec3(1.0 / 2.2));
    } else r = mix(u_dark, u_light, t);
    col = mix(col, r, u_strength);
    o = vec4(col * p.a, p.a);
}";

		// u_src is the colour bar, u_other the picture
		private const string FsMapBar = @"uniform sampler2D u_other; uniform float u_strength;
void main(){
    vec4 p = texture(u_other, v_uv);
    vec3 col = p.a > 0.0 ? p.rgb / p.a : vec3(0.0);
    float t = clamp(dot(col, vec3(0.299, 0.587, 0.114)), 0.0, 1.0);
    float e = 0.5 / u_srcSize.x;
    vec4 bar = texture(u_src, vec2(clamp(t, e, 1.0 - e), 0.5));
    vec3 r = bar.a > 0.0 ? bar.rgb / bar.a : col;
    col = mix(col, r, u_strength * bar.a);
    o = vec4(col * p.a, p.a);
}";

		// each vec4 is (gain, 1 / gamma, lift, offset)
		private const string FsTone = @"uniform vec4 u_y; uniform vec4 u_s; uniform vec4 u_r; uniform vec4 u_g; uniform vec4 u_b; uniform vec2 u_hue;
float grade(float v, vec4 k){
    v = v * k.x + k.z * (1.0 - v);
    return (v > 0.0 ? pow(v, k.y) : v) + k.w;
}
void main(){
    vec4 p = texture(u_src, v_uv);
    vec3 col = p.a > 0.0 ? p.rgb / p.a : vec3(0.0);
    float y = dot(col, vec3(0.299, 0.587, 0.114));
    vec2 ch = vec2(dot(col, vec3(-0.168736, -0.331264, 0.5)), dot(col, vec3(0.5, -0.418688, -0.081312)));
    y = grade(y, u_y);
    float s = length(ch) * 2.0;
    ch *= s > 1.0e-5 ? max(grade(s, u_s), 0.0) / s : 0.0;
    ch = vec2(ch.x * u_hue.x - ch.y * u_hue.y, ch.x * u_hue.y + ch.y * u_hue.x);
    col = vec3(y + 1.402 * ch.y, y - 0.344136 * ch.x - 0.714136 * ch.y, y + 1.772 * ch.x);
    col = vec3(grade(col.r, u_r), grade(col.g, u_g), grade(col.b, u_b));
    o = vec4(clamp(col, 0.0, 1.0) * p.a, p.a);
}";

		// u_grid holds how far each grid point moved, two texels a point (x then y), 16 bits each.
		// The place a pixel comes from is found by stepping back along the moves.
		private const string FsMesh = @"uniform sampler2D u_grid; uniform vec2 u_org; uniform vec2 u_rest; uniform vec2 u_cells;
vec2 point(int i, int j){
    vec4 a = texelFetch(u_grid, ivec2(i * 2, j), 0);
    vec4 b = texelFetch(u_grid, ivec2(i * 2 + 1, j), 0);
    vec2 v = vec2(floor(a.r * 255.0 + 0.5) * 256.0 + floor(a.g * 255.0 + 0.5), floor(b.r * 255.0 + 0.5) * 256.0 + floor(b.g * 255.0 + 0.5));
    return (v / 65535.0 - 0.5) * 8192.0;
}
vec2 moved(vec2 q){
    vec2 g = clamp(q / u_rest, 0.0, 1.0) * u_cells;
    vec2 i = min(floor(g), u_cells - 1.0);
    vec2 f = g - i;
    int x = int(i.x), y = int(i.y);
    return mix(mix(point(x, y), point(x + 1, y), f.x), mix(point(x, y + 1), point(x + 1, y + 1), f.x), f.y);
}
void main(){
    vec2 p = v_uv * u_size - u_org;
    vec2 q = p;
    for (int k = 0; k < 6; k++) q = p - moved(q);
    o = (q.x < 0.0 || q.y < 0.0 || q.x > u_rest.x || q.y > u_rest.y) ? vec4(0.0) : texture(u_src, (q + u_org) / u_srcSize);
}";

		// copies of the picture along u_d (pixels), the newest at the pixel itself
		private const string FsTrail = @"uniform vec2 u_d; uniform int u_n; uniform int u_ghost;
vec4 pick(vec2 pos){
    vec2 q = pos / u_srcSize;
    return (q.x < 0.0 || q.y < 0.0 || q.x > 1.0 || q.y > 1.0) ? vec4(0.0) : texture(u_src, q);
}
void main(){
    vec2 pos = v_uv * u_size;
    vec4 acc = vec4(0.0);
    float last = float(u_n - 1);
    for (int i = 0; i < 32; i++) {
        if (i >= u_n) break;
        // from the oldest copy to the newest
        float f = (last - float(i)) / last;
        vec4 s = pick(pos + u_d * f);
        if (u_ghost == 1) {
            s *= f > 0.0 ? (1.0 - f) * 0.6 : 1.0;
            acc = s + acc * (1.0 - s.a);
        } else acc += s;
    }
    o = u_ghost == 1 ? acc : acc / float(u_n);
}";

		private const string FsTurn = @"uniform int u_n;
void main(){
    vec2 q = u_n == 1 ? vec2(v_uv.y, 1.0 - v_uv.x) : (u_n == 2 ? vec2(1.0) - v_uv : vec2(1.0 - v_uv.y, v_uv.x));
    o = texture(u_src, q);
}";

		// one direction of a soft blur; bright pixels weigh more with u_light
		private const string FsSoft = @"uniform vec2 u_step; uniform float u_light;
void main(){
    vec4 acc = vec4(0.0);
    float total = 0.0;
    for (int i = -12; i <= 12; i++) {
        float f = float(i);
        vec4 p = texture(u_src, v_uv + u_step * f);
        float w = exp(-f * f / 50.0) * (1.0 + u_light * dot(p.rgb, vec3(0.299, 0.587, 0.114)));
        acc += p * w;
        total += w;
    }
    o = acc / total;
}";

		// u_other is the blurred picture; the edge reads past the picture's border as its last pixel
		private const string FsVignette = @"uniform sampler2D u_other; uniform vec2 u_c; uniform vec2 u_norm; uniform float u_radius; uniform float u_border;
uniform float u_dark; uniform float u_shift;
vec4 pick(vec2 pos, float m){
    vec2 q = pos / u_size;
    return mix(texture(u_src, q), texture(u_other, q), m);
}
void main(){
    vec2 pos = v_uv * u_size;
    vec2 rel = pos - u_c;
    float d = length(rel * u_norm);
    float m = smoothstep(0.0, 1.0, (d - u_radius) / u_border);
    vec4 r = pick(pos, m);
    if (u_shift != 0.0 && d > 0.0) {
        vec2 sh = normalize(rel) * u_shift * m;
        vec4 pr = pick(pos - sh, m), pb = pick(pos + sh, m);
        r = vec4(pr.r, r.g, pb.b, max(r.a, max(pr.a, pb.a)));
    }
    r.rgb = min(r.rgb * (1.0 + u_dark * m), vec3(r.a));
    o = r;
}";

		private const string FsWobble = @"uniform float u_amp; uniform float u_cell; uniform float u_t; uniform vec2 u_scroll; uniform float u_swell;
float hash(vec3 p){
    p = fract(p * vec3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yzx + 33.33);
    return fract((p.x + p.y) * p.z);
}
float noise(vec3 p){
    vec3 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = mix(mix(hash(i), hash(i + vec3(1.0, 0.0, 0.0)), f.x), mix(hash(i + vec3(0.0, 1.0, 0.0)), hash(i + vec3(1.0, 1.0, 0.0)), f.x), f.y);
    float b = mix(mix(hash(i + vec3(0.0, 0.0, 1.0)), hash(i + vec3(1.0, 0.0, 1.0)), f.x), mix(hash(i + vec3(0.0, 1.0, 1.0)), hash(i + vec3(1.0, 1.0, 1.0)), f.x), f.y);
    return mix(a, b, f.z);
}
void main(){
    vec2 pos = v_uv * u_size;
    vec2 g = (pos - u_size * 0.5 - u_scroll) / u_cell;
    vec2 d = vec2(noise(vec3(g, u_t)), noise(vec3(g + 37.7, u_t + 11.3))) - 0.5;
    // a slower, wider sway on top
    vec2 g2 = g * 0.35;
    d += u_swell * (vec2(noise(vec3(g2 + 5.1, u_t * 0.5)), noise(vec3(g2 + 19.1, u_t * 0.5 + 7.9))) - 0.5);
    vec2 q = (pos + d * 2.0 * u_amp) / u_srcSize;
    o = (q.x < 0.0 || q.y < 0.0 || q.x > 1.0 || q.y > 1.0) ? vec4(0.0) : texture(u_src, q);
}";

		// u_src is the image, u_other the picture
		private const string FsComposite = @"uniform sampler2D u_other; uniform vec2 u_c; uniform float u_scale; uniform int u_tile; uniform int u_mode; uniform int u_under;
float lum(vec3 c){ return dot(c, vec3(0.299, 0.587, 0.114)); }
vec3 blend(vec3 d, vec3 s){
    if (u_mode == 1) return d + s;
    if (u_mode == 2) return d - s;
    if (u_mode == 3) return d * s;
    if (u_mode == 4) return 1.0 - (1.0 - d) * (1.0 - s);
    if (u_mode == 10) return mix(2.0 * s * d, 1.0 - 2.0 * (1.0 - s) * (1.0 - d), step(0.5, d));
    if (u_mode == 11) return max(s, d);
    if (u_mode == 12) return min(s, d);
    if (u_mode == 13) return d + (lum(s) - lum(d));
    if (u_mode == 14) return s + (lum(d) - lum(s));
    if (u_mode == 15) return d * (0.5 + s);
    if (u_mode == 16) return mix(2.0 * s * d, 1.0 - 2.0 * (1.0 - s) * (1.0 - d), step(0.5, s));
    if (u_mode == 17) return abs(d - s);
    return s;
}
void main(){
    vec4 mine = texture(u_other, v_uv);
    vec2 q = ((v_uv * u_size - u_c) / u_scale + u_srcSize * 0.5) / u_srcSize;
    vec4 img = vec4(0.0);
    if (u_tile == 1) img = texture(u_src, fract(q));
    else if (q.x >= 0.0 && q.y >= 0.0 && q.x <= 1.0 && q.y <= 1.0) img = texture(u_src, q);
    vec4 top = u_under == 1 ? mine : img;
    vec4 low = u_under == 1 ? img : mine;
    vec3 tc = top.a > 0.0 ? top.rgb / top.a : vec3(0.0);
    vec3 lc = low.a > 0.0 ? low.rgb / low.a : vec3(0.0);
    vec3 both = clamp(blend(lc, tc), 0.0, 1.0);
    vec3 rgb = top.a * low.a * both + top.a * (1.0 - low.a) * tc + (1.0 - top.a) * low.a * lc;
    o = vec4(rgb, top.a + low.a - top.a * low.a);
}";
		#endregion

		#region Helpers
		private static double Finite(double v, double fallback = 0) => double.IsFinite(v) ? v : fallback;

		private static List<double> Numbers(string text) {
			var list = new List<double>();
			foreach (var piece in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				list.Add(double.TryParse(piece, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0);
			return list;
		}

		// Runs `fragment` with the image file as u_src and the current picture as u_other; the result, as
		// large as the picture, takes its place. False (the picture untouched) when the image cannot be read.
		private static bool WithImage(Aup2Context c, string path, string fragment, Aup2Uniforms uniforms) {
			path = path.Trim().Trim('"');
			if (path.Length == 0 || c.Host == null || !c.Pixels()) return false;
			var item = c.Item;
			var parts = item.Parts;
			double ox = item.OffsetX, oy = item.OffsetY;
			var mine = c.Gpu!.Copy(c.Surface!);
			bool ok;
			try { ok = c.Host.LoadImage(c, path); } catch { ok = false; }
			if (ok && c.Surface != null) {
				item.Parts = null;
				var t = c.Pass(fragment, uniforms.Tex("u_other", mine), mine.W, mine.H);
				if (t == null) { c.Replace(mine); mine = null; ok = false; } else c.Replace(t);
			}
			// loading an image forgets the pieces and the centre: the picture keeps its own
			item.Parts = parts; item.OffsetX = ox; item.OffsetY = oy;
			c.Gpu.Release(mine);
			return ok;
		}
		#endregion

		private static void BasicOutput(Aup2Context c) {
			var item = c.Item;
			item.X += Finite(c.V("X")); item.Y += Finite(c.V("Y"));
			double scale = Finite(c.V("拡大率", 100), 100) / 100;
			item.ScaleX *= scale; item.ScaleY *= scale;
			item.Opacity *= 1 - Math.Clamp(Finite(c.V("透明度")) / 100, 0, 1);
			item.Blend = Aup2Evaluator.BlendOf(c.S("合成モード", "通常")) ?? Aup2Blend.Normal;
		}

		// the picture's brightness picks a colour between the dark one and the light one, or along a colour bar
		private static void GradientMap(Aup2Context c) {
			double strength = Math.Clamp(Finite(c.V("強さ", 100)) / 100, 0, 1);
			if (strength <= 0 || !c.Pixels()) return;
			if (WithImage(c, c.S("パターン画像"), FsMapBar, new Aup2Uniforms().F("u_strength", strength))) return;
			string how = c.S("色補間");
			int interp = how.Contains("HSV", StringComparison.OrdinalIgnoreCase) ? 1
				: how.Contains("リニア", StringComparison.Ordinal) || how.Contains("linear", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
			c.Filter(FsMap, new Aup2Uniforms().F("u_strength", strength).V3("u_dark", c.Color("暗部色", Vector3.Zero)).V3("u_light", c.Color("明部色", Vector3.One))
				.I("u_interp", interp));
		}

		// gain, gamma, lift and offset of one row, in percent; false when the row changes nothing
		private static bool Grade(Aup2Context c, string row, out Vector4 k) {
			double gain = Finite(c.V(row + "::ゲイン", 100), 100) / 100, gamma = Math.Max(Finite(c.V(row + "::ガンマ", 100), 100), 1) / 100;
			double lift = Finite(c.V(row + "::リフト")) / 100, offset = Finite(c.V(row + "::オフセット")) / 100;
			k = new Vector4((float)gain, (float)(1 / gamma), (float)lift, (float)offset);
			return gain != 1 || gamma != 1 || lift != 0 || offset != 0;
		}

		private static void ExtendedTone(Aup2Context c) {
			double hue = Finite(c.V("色相::オフセット"));
			bool any = Grade(c, "輝度", out var y);
			any |= Grade(c, "彩度", out var s);
			any |= Grade(c, "赤", out var r);
			any |= Grade(c, "緑", out var g);
			any |= Grade(c, "青", out var b);
			if (!any && hue == 0) return;
			double a = hue * Math.PI / 180;
			c.Filter(FsTone, new Aup2Uniforms().V4("u_y", y.X, y.Y, y.Z, y.W).V4("u_s", s.X, s.Y, s.Z, s.W).V4("u_r", r.X, r.Y, r.Z, r.W)
				.V4("u_g", g.X, g.Y, g.Z, g.W).V4("u_b", b.X, b.Y, b.Z, b.W).V2("u_hue", Math.Cos(a), Math.Sin(a)));
		}

		// The picture pulled by a grid of points. The points are read as x, y pairs, row by row: places
		// from the picture's centre when they lie near the untouched grid, moves from it otherwise.
		private static void Mesh(Aup2Context c) {
			double rate = Finite(c.V("変形率", 100)) / 100;
			int nx = (int)Finite(c.V("横格子点")), ny = (int)Finite(c.V("縦格子点"));
			var n = Numbers(c.S("座標"));
			if (rate == 0 || n.Count < 8 || nx < 1 || ny < 1 || nx > 64 || ny > 64) return;
			// the counts may be cells and not points
			if (n.Count != nx * ny * 2 && n.Count == (nx + 1) * (ny + 1) * 2) { nx++; ny++; }
			if (nx < 2 || ny < 2 || n.Count != nx * ny * 2 || !c.Pixels()) return;
			double w = c.Surface!.W, h = c.Surface.H;
			int count = nx * ny;
			var rest = new double[count * 2];
			double asPlaces = 0, asMoves = 0;
			for (int i = 0; i < count; i++) {
				if (!double.IsFinite(n[i * 2]) || !double.IsFinite(n[i * 2 + 1])) return;
				rest[i * 2] = -w / 2 + w * (i % nx) / (nx - 1);
				rest[i * 2 + 1] = -h / 2 + h * (i / nx) / (ny - 1);
				asPlaces += Math.Abs(n[i * 2] - rest[i * 2]) + Math.Abs(n[i * 2 + 1] - rest[i * 2 + 1]);
				asMoves += Math.Abs(n[i * 2]) + Math.Abs(n[i * 2 + 1]);
			}
			bool places = asPlaces < asMoves;
			double minX = -w / 2, maxX = w / 2, minY = -h / 2, maxY = h / 2, largest = 0;
			var bytes = new byte[count * 8];
			for (int i = 0; i < count; i++) {
				double dx = Math.Clamp((places ? n[i * 2] - rest[i * 2] : n[i * 2]) * rate, -4096, 4096);
				double dy = Math.Clamp((places ? n[i * 2 + 1] - rest[i * 2 + 1] : n[i * 2 + 1]) * rate, -4096, 4096);
				largest = Math.Max(largest, Math.Max(Math.Abs(dx), Math.Abs(dy)));
				minX = Math.Min(minX, rest[i * 2] + dx); maxX = Math.Max(maxX, rest[i * 2] + dx);
				minY = Math.Min(minY, rest[i * 2 + 1] + dy); maxY = Math.Max(maxY, rest[i * 2 + 1] + dy);
				int ex = (int)Math.Round((dx + 4096) / 8192 * 65535), ey = (int)Math.Round((dy + 4096) / 8192 * 65535);
				bytes[i * 8] = (byte)(ex >> 8); bytes[i * 8 + 1] = (byte)(ex & 255); bytes[i * 8 + 3] = 255;
				bytes[i * 8 + 4] = (byte)(ey >> 8); bytes[i * 8 + 5] = (byte)(ey & 255); bytes[i * 8 + 7] = 255;
			}
			if (largest < 0.01) return;
			double left = 0, top = 0;
			if (c.Item.Kind != Aup2ItemKind.Filter) {
				// room for the points pulled out of the picture
				double offX = c.Item.OffsetX, offY = c.Item.OffsetY;
				c.Grow((int)Math.Ceiling(Math.Min(2048, -w / 2 - minX)), (int)Math.Ceiling(Math.Min(2048, -h / 2 - minY)),
					(int)Math.Ceiling(Math.Min(2048, maxX - w / 2)), (int)Math.Ceiling(Math.Min(2048, maxY - h / 2)));
				left = (c.Surface.W - w) / 2 - (c.Item.OffsetX - offX);
				top = (c.Surface.H - h) / 2 - (c.Item.OffsetY - offY);
			}
			var grid = c.Gpu!.Rent(nx * 2, ny);
			c.Gpu.Write(grid, bytes);
			c.Filter(FsMesh, new Aup2Uniforms().Tex("u_grid", grid).V2("u_org", left, top).V2("u_rest", w, h).V2("u_cells", nx - 1, ny - 1));
			c.Gpu.Release(grid);
		}

		// A trail along the way the object's own placement moved over the last frames. Only the X and Y of
		// its drawing settings are followed: turns, scale changes and other effects that move it are not.
		private static void MotionBlur(Aup2Context c) {
			if (c.Item.Kind is Aup2ItemKind.Filter or Aup2ItemKind.SceneChange) return;
			double interval = Math.Clamp(Finite(c.V("間隔", 1)), 0, 100);
			int samples = Math.Clamp((int)Finite(c.V("分解能", 8), 8), 2, 32);
			var draw = c.Object.Find(Aup2Evaluator.StandardDraw) ?? c.Object.Find("基本出力");
			if (interval <= 0 || draw == null) return;
			double now = c.Frame;
			double x1 = c.V(draw, "X"), y1 = c.V(draw, "Y");
			c.Frame = Math.Max(0, now - interval);
			double x0 = c.V(draw, "X"), y0 = c.V(draw, "Y");
			c.Frame = now;
			double dx = Finite(x1 - x0), dy = Finite(y1 - y0);
			if (dx * dx + dy * dy < 0.25) return;
			// the move as the picture sees it, before the object is turned and scaled
			double a = (c.Item.RotZ + Finite(c.V(draw, "Z軸回転"))) * Math.PI / 180, cs = Math.Cos(a), sn = Math.Sin(a);
			double scale = Finite(c.V(draw, "拡大率", 100), 100) / 100;
			double sx = Math.Abs(c.Item.ScaleX * scale), sy = Math.Abs(c.Item.ScaleY * scale);
			if (sx < 1e-3 || sy < 1e-3) return;
			double lx = (dx * cs + dy * sn) / sx, ly = (-dx * sn + dy * cs) / sy;
			if (c.Item.FlipX) lx = -lx;
			if (c.Item.FlipY) ly = -ly;
			double length = Math.Sqrt(lx * lx + ly * ly);
			if (length > 1024) { lx *= 1024 / length; ly *= 1024 / length; }
			if (!c.Pixels()) return;
			// the trail lies behind the object
			c.Grow((int)Math.Ceiling(Math.Max(0, lx)), (int)Math.Ceiling(Math.Max(0, ly)), (int)Math.Ceiling(Math.Max(0, -lx)), (int)Math.Ceiling(Math.Max(0, -ly)));
			c.Filter(FsTrail, new Aup2Uniforms().V2("u_d", lx, ly).I("u_n", samples).I("u_ghost", c.Flag("残像") ? 1 : 0));
		}

		// quarter turns, clockwise: the picture's width and height swap on odd counts
		private static void QuarterTurns(Aup2Context c) {
			int n = (((int)Math.Round(Math.Clamp(Finite(c.V("90度回転")), -1000, 1000)) % 4) + 4) % 4;
			if (n == 0 || !c.Pixels()) return;
			var s = c.Surface!;
			double ox = c.Item.OffsetX, oy = c.Item.OffsetY;
			double tx = n == 1 ? -oy : n == 2 ? -ox : oy, ty = n == 1 ? ox : n == 2 ? -oy : -ox;
			var t = c.Pass(FsTurn, new Aup2Uniforms().I("u_n", n), n == 2 ? s.W : s.H, n == 2 ? s.H : s.W);
			// pieces cut before the turn do not follow it
			c.Item.Parts = null;
			c.Replace(t, tx - ox, ty - oy);
		}

		// towards the edges the picture blurs, darkens (or brightens) and its red and blue drift apart
		private static void Vignette(Aup2Context c) {
			double blur = Math.Clamp(Finite(c.V("ボケ")), 0, 200), dark = Math.Clamp(Finite(c.V("光量")) / 100, -1, 1);
			double shift = Math.Clamp(Finite(c.V("色ずれ量")), -200, 200);
			if (blur < 0.5 && dark == 0 && shift == 0) return;
			if (!c.Pixels()) return;
			var s = c.Surface!;
			var gpu = c.Gpu!;
			double radius = Math.Max(0, Finite(c.V("半径", 50), 50)), border = Math.Max(0.01, Finite(c.V("境界", 50), 50));
			double light = Math.Clamp(Finite(c.V("光の強さ")) / 100, 0, 8) * 4;
			Aup2Surface? soft = null;
			if (blur >= 0.5) {
				// a wide blur runs on a smaller copy
				int f = Math.Max(1, (int)Math.Ceiling(blur / 16));
				int w = Math.Max(1, s.W / f), h = Math.Max(1, s.H / f);
				var small = f > 1 ? c.Pass(FsCopy, null, w, h) : null;
				var across = c.Pass(FsSoft, new Aup2Uniforms().V2("u_step", blur / 12 / s.W, 0).F("u_light", light), w, h, small ?? s);
				soft = c.Pass(FsSoft, new Aup2Uniforms().V2("u_step", 0, blur / 12 / s.H).F("u_light", light), w, h, across);
				gpu.Release(small);
				gpu.Release(across);
			}
			// 100 is the picture's edge: on each axis when the shape follows the picture, on the longer one otherwise
			double hw = Math.Max(1, s.W / 2.0), hh = Math.Max(1, s.H / 2.0), far = Math.Max(hw, hh);
			bool fit = c.Flag("縦横比補正");
			var t = c.Pass(FsVignette, new Aup2Uniforms().Tex("u_other", soft ?? s)
				.V2("u_c", s.W / 2.0 - c.Item.OffsetX, s.H / 2.0 - c.Item.OffsetY).V2("u_norm", 100 / (fit ? hw : far), 100 / (fit ? hh : far))
				.F("u_radius", radius).F("u_border", border).F("u_dark", dark).F("u_shift", shift));
			c.Replace(t);
			gpu.Release(soft);
		}

		// the picture pushed around by a noise that drifts and changes with time
		private static void Wobble(Aup2Context c) {
			double amp = Math.Clamp(Math.Abs(Finite(c.V("強さ", 10), 10)), 0, 512);
			if (amp < 0.01 || !c.Pixels()) return;
			double cell = Math.Max(1, Math.Abs(Finite(c.V("幅", 100), 100)));
			double swell = Math.Clamp(Finite(c.V("うねり")) / 100, 0, 4);
			double t = c.Time;
			if (c.Item.Kind != Aup2ItemKind.Filter) c.Grow((int)Math.Min(1024, Math.Ceiling(amp * (1 + swell))));
			c.Filter(FsWobble, new Aup2Uniforms().F("u_amp", amp).F("u_cell", cell).F("u_t", Finite(c.V("変化速度", 1), 1) * t)
				.V2("u_scroll", Finite(c.V("移動速度X")) * t, Finite(c.V("移動速度Y")) * t).F("u_swell", swell));
		}

		// an image file laid over the picture, or under it, inside the picture's own size
		private static void ImageComposite(Aup2Context c) {
			double scale = Finite(c.V("拡大率", 100), 100) / 100;
			if (scale <= 0 || !c.Pixels()) return;
			string place = c.S("配置");
			bool under = place.Contains('背') || place.Contains('後') || place.Contains('下') || place.Contains('奥');
			int mode = (int)(Aup2Evaluator.BlendOf(c.S("合成モード", "通常")) ?? Aup2Blend.Normal);
			var s = c.Surface!;
			WithImage(c, c.S("画像"), FsComposite, new Aup2Uniforms()
				.V2("u_c", s.W / 2.0 - c.Item.OffsetX + Finite(c.V("X")), s.H / 2.0 - c.Item.OffsetY + Finite(c.V("Y"))).F("u_scale", scale)
				.I("u_tile", c.Flag("ループ画像") ? 1 : 0).I("u_mode", mode).I("u_under", under ? 1 : 0));
		}
	}
}
