using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace OpenTaiko.AviUtl2 {
	// The shape-changing effects, and the ones that cut an object into pieces or place its pieces.
	internal static class Aup2FiltersDistort {
		private const int MaxParts = 4096;

		public static void Register(Dictionary<string, Aup2FilterFn> map) {
			map["波紋"] = Ripple;
			map["ラスター"] = Raster;
			map["極座標変換"] = Polar;
			map["ディスプレイスメントマップ"] = Displacement;
			map["簡易変形"] = c => Corners(c, false);
			map["簡易変形(カメラ制御)"] = c => Corners(c, true);
			map["万華鏡"] = Kaleidoscope;
			map["ミラー"] = Mirror;
			map["画像ループ"] = Loop;
			map["立方体(カメラ制御)"] = Cube;
			map["球体(カメラ制御)"] = Sphere;
			map["オブジェクト分割"] = Split;
			map["座標の拡大縮小(個別オブジェクト)"] = ScalePositions;
			map["座標の回転(個別オブジェクト)"] = TurnPositions;
			map["円形配置"] = Circle;
			map["ランダム配置"] = Scatter;
		}

		#region Pixel effects
		// rings that leave the centre and push the picture along their radius
		private static void Ripple(Aup2Context c) {
			if (!c.Pixels()) return;
			var s = c.Surface!;
			double width = Math.Max(1, Math.Abs(c.V("幅", 30))), height = c.V("高さ", 15), speed = c.V("速度", 150);
			if (height == 0) return;
			double front = speed == 0 ? 1e9 : Math.Abs(speed) * c.Time;
			c.Filter(@"uniform vec2 u_c; uniform float u_wl; uniform float u_amp; uniform float u_front; uniform float u_count; uniform float u_gap;
void main(){
    vec2 pos = v_uv * u_size;
    vec2 d = pos - u_c;
    float r = length(d);
    float t = u_front - r;
    float k = 0.0;
    if (t > 0.0) {
        float cyc = u_wl + u_gap;
        float n = floor(t / cyc);
        float f = t - n * cyc;
        if ((u_count <= 0.0 || n < u_count) && f < u_wl) k = sin(f / u_wl * 6.2831853) * u_amp;
    }
    vec2 q = (pos + (r > 0.0 ? d / r : vec2(0.0)) * k) / u_srcSize;
    o = (q.x < 0.0 || q.y < 0.0 || q.x > 1.0 || q.y > 1.0) ? vec4(0.0) : texture(u_src, q);
}", new Aup2Uniforms().V2("u_c", s.W / 2.0 + c.V("中心X"), s.H / 2.0 + c.V("中心Y")).F("u_wl", width).F("u_amp", height)
				.F("u_front", front).F("u_count", Math.Max(0, c.V("波紋数"))).F("u_gap", Math.Max(0, c.V("波紋間隔"))));
		}

		// each row slides sideways along a sine (each column up and down when vertical)
		private static void Raster(Aup2Context c) {
			double amp = Math.Clamp(Math.Abs(c.V("横幅", 100)), 0, 1024), period = c.V("高さ", 100), rate = c.V("周期", 1);
			if (amp < 0.5 || !c.Pixels()) return;
			bool vertical = c.Flag("縦ラスター");
			if (c.Flag("ランダム振幅")) amp *= c.Random("raster", (long)Math.Floor(c.Frame));
			int m = (int)Math.Ceiling(amp);
			if (vertical) c.Grow(0, m, 0, m); else c.Grow(m, 0, m, 0);
			if (Math.Abs(period) < 1) period = 1;
			c.Filter(@"uniform float u_amp; uniform float u_period; uniform float u_phase; uniform int u_vertical;
void main(){
    vec2 pos = v_uv * u_size;
    float along = u_vertical == 1 ? pos.x : pos.y;
    float k = sin((along / u_period + u_phase) * 6.2831853) * u_amp;
    vec2 q = (u_vertical == 1 ? vec2(pos.x, pos.y - k) : vec2(pos.x - k, pos.y)) / u_srcSize;
    o = (q.x < 0.0 || q.y < 0.0 || q.x > 1.0 || q.y > 1.0) ? vec4(0.0) : texture(u_src, q);
}", new Aup2Uniforms().F("u_amp", amp).F("u_period", period).F("u_phase", c.Time * rate).I("u_vertical", vertical ? 1 : 0));
		}

		// the picture wound around a circle: its width goes around, its top is the outer edge
		private static void Polar(Aup2Context c) {
			if (!c.Pixels()) return;
			var s = c.Surface!;
			double hole = Math.Max(0, c.V("中心幅")), scale = Math.Max(0, c.V("拡大率", 100)) / 100;
			double outer = s.H + hole;
			int side = (int)Math.Ceiling(2 * outer * scale);
			if (side < 1) { c.Skip = true; return; }
			side = Math.Min(side, Aup2Gpu.MaxSide);
			var t = c.Pass(@"uniform float u_hole; uniform float u_outer; uniform float u_turn; uniform float u_swirl;
void main(){
    vec2 d = (v_uv - 0.5) * 2.0 * u_outer;
    float r = length(d);
    if (r < u_hole || r > u_outer) { o = vec4(0.0); return; }
    float depth = (u_outer - r) / max(u_outer - u_hole, 1.0);
    float a = atan(d.x, -d.y) / 6.2831853 + 0.5;
    o = texture(u_src, vec2(fract(a + u_turn + u_swirl * (1.0 - depth)), depth));
}", new Aup2Uniforms().F("u_hole", hole).F("u_outer", outer).F("u_turn", -c.V("回転") / 360).F("u_swirl", c.V("渦巻")), side, side);
			c.Replace(t);
		}

		private static readonly Dictionary<string, int> MapKinds = new() {
			["背景"] = 0, ["円"] = 1, ["四角形"] = 2, ["三角形"] = 3, ["五角形"] = 5, ["六角形"] = 6, ["星型"] = 10,
		};

		// a soft shape says how far each pixel is moved, zoomed or turned
		private static void Displacement(Aup2Context c) {
			if (!MapKinds.TryGetValue(c.S("マップの種類", "円"), out int kind)) return;
			int mode = c.S("変形方法", "移動変形") switch { "拡大変形" => 1, "回転変形" => 2, _ => 0 };
			if (!c.Pixels()) return;
			var s = c.Surface!;
			double size = Math.Max(0, c.V("サイズ", 200)), aspect = c.V("縦横比");
			double hx = size / 2 * (aspect > 0 ? 1 - aspect / 100 : 1), hy = size / 2 * (aspect < 0 ? 1 + aspect / 100 : 1);
			if (c.Flag("元のサイズに合わせる")) { hx = s.W / 2.0; hy = s.H / 2.0; }
			if (kind != 0 && (hx < 0.5 || hy < 0.5)) return;
			double zoom = c.V("拡大変形") / 100, zoomAspect = c.V("拡大縦横");
			c.Filter(@"uniform int u_kind; uniform int u_mode; uniform vec2 u_c; uniform vec2 u_half; uniform float u_rot; uniform float u_blur;
uniform vec2 u_move; uniform vec2 u_zoom; uniform float u_twist;
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
void main(){
    vec2 pos = v_uv * u_size;
    vec2 rel = pos - u_c;
    float cs = cos(u_rot), sn = sin(u_rot);
    vec2 p = vec2(rel.x * cs + rel.y * sn, -rel.x * sn + rel.y * cs);
    float d;
    float m = min(u_half.x, u_half.y);
    if (u_kind == 0) d = -1.0e6;
    else if (u_kind == 1) d = (length(p / u_half) - 1.0) * m;
    else if (u_kind == 2) { vec2 q = abs(p) - u_half; d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0); }
    else {
        vec2 q = p / u_half * m;
        d = u_kind == 10 ? sdStar(q, m) : sdPoly(q, m, float(u_kind));
        if (u_kind == 3) d = sdPoly(vec2(q.x, q.y + m * 0.25), m, 3.0);
    }
    // the map's strength: 1 inside, fading over the blurred edge
    float k = clamp(0.5 - d / max(2.0 * u_blur, 1.0), 0.0, 1.0);
    vec2 q2;
    if (u_mode == 0) q2 = pos - u_move * k;
    else if (u_mode == 1) q2 = u_c + rel / max(vec2(1.0) + u_zoom * k, vec2(0.01));
    else { float a = -u_twist * k; float ca = cos(a), sa = sin(a); q2 = u_c + vec2(rel.x * ca - rel.y * sa, rel.x * sa + rel.y * ca); }
    q2 /= u_srcSize;
    o = (q2.x < 0.0 || q2.y < 0.0 || q2.x > 1.0 || q2.y > 1.0) ? vec4(0.0) : texture(u_src, q2);
}", new Aup2Uniforms().I("u_kind", kind).I("u_mode", mode).V2("u_c", s.W / 2.0 + c.V("X"), s.H / 2.0 + c.V("Y")).V2("u_half", Math.Max(hx, 0.5), Math.Max(hy, 0.5))
				.F("u_rot", c.V("回転") * Math.PI / 180).F("u_blur", Math.Max(0, c.V("ぼかし")))
				.V2("u_move", c.V("変形X"), c.V("変形Y"))
				.V2("u_zoom", zoom * (zoomAspect > 0 ? 1 - zoomAspect / 100 : 1), zoom * (zoomAspect < 0 ? 1 + zoomAspect / 100 : 1))
				.F("u_twist", c.V("回転変形") * Math.PI / 180));
		}

		// the picture stretched between four corners given from its centre: top left, top right, bottom
		// right, bottom left. With depth, the corners are seen from 1024 away.
		private static void Corners(Aup2Context c, bool depth) {
			var n = Numbers(c.S("座標"));
			int per = depth ? 3 : 2;
			if (n.Count < per * 4 || !c.Pixels()) return;
			Span<double> x = stackalloc double[4], y = stackalloc double[4];
			double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
			for (int i = 0; i < 4; i++) {
				double px = n[i * per], py = n[i * per + 1];
				if (depth) {
					double f = 1024 / Math.Max(1, 1024 + n[i * per + 2]);
					px *= f; py *= f;
				}
				if (!double.IsFinite(px) || !double.IsFinite(py)) return;
				x[i] = px; y[i] = py;
				minX = Math.Min(minX, px); maxX = Math.Max(maxX, px);
				minY = Math.Min(minY, py); maxY = Math.Max(maxY, py);
			}
			double spanX = maxX - minX, spanY = maxY - minY;
			if (spanX < 1 || spanY < 1) { c.Skip = true; return; }
			int w = (int)Math.Min(Aup2Gpu.MaxSide, Math.Ceiling(spanX)), h = (int)Math.Min(Aup2Gpu.MaxSide, Math.Ceiling(spanY));
			var t = c.Pass(@"uniform vec2 u_span; uniform vec2 u_a; uniform vec2 u_b; uniform vec2 u_c; uniform vec2 u_d;
float cross2(vec2 a, vec2 b){ return a.x * b.y - a.y * b.x; }
float along(vec2 h, vec2 e, vec2 f, vec2 g, float v){
    vec2 den = e + g * v;
    vec2 num = h - f * v;
    if (abs(den.x) > abs(den.y)) return num.x / den.x;
    return abs(den.y) > 0.0 ? num.y / den.y : -1.0;
}
void main(){
    vec2 p = v_uv * u_span;
    vec2 e = u_b - u_a, f = u_d - u_a, g = u_a - u_b + u_c - u_d, h = p - u_a;
    float k2 = cross2(g, f), k1 = cross2(e, f) + cross2(h, g), k0 = cross2(h, e);
    float u = -1.0, v = -1.0;
    if (abs(k2) < 1.0e-4 * (abs(k1) + 1.0)) {
        if (abs(k1) > 0.0) { v = -k0 / k1; u = along(h, e, f, g, v); }
    } else {
        float w = k1 * k1 - 4.0 * k0 * k2;
        if (w >= 0.0) {
            w = sqrt(w);
            v = (-k1 - w) * 0.5 / k2;
            u = along(h, e, f, g, v);
            if (u < 0.0 || u > 1.0 || v < 0.0 || v > 1.0) { v = (-k1 + w) * 0.5 / k2; u = along(h, e, f, g, v); }
        }
    }
    o = (u < 0.0 || u > 1.0 || v < 0.0 || v > 1.0) ? vec4(0.0) : texture(u_src, vec2(u, v));
}", new Aup2Uniforms().V2("u_span", spanX, spanY)
				.V2("u_a", x[0] - minX, y[0] - minY).V2("u_b", x[1] - minX, y[1] - minY)
				.V2("u_c", x[2] - minX, y[2] - minY).V2("u_d", x[3] - minX, y[3] - minY), w, h);
			c.Replace(t, (minX + maxX) / 2, (minY + maxY) / 2);
		}

		// a wedge of the picture mirrored around the centre, and mirrored again every `length` outwards
		private static void Kaleidoscope(Aup2Context c) {
			if (!c.Pixels()) return;
			var s = c.Surface!;
			double length = Math.Max(1, c.V("長さ", 100)), fixedSize = c.V("固定サイズ");
			int corners = Math.Clamp((int)c.V("角数(偶数)", 6) / 2 * 2, 2, 64);
			int repeat = Math.Clamp((int)c.V("繰り返し回数", 2), 1, 64);
			int side = (int)Math.Clamp(Math.Ceiling(fixedSize > 0 ? fixedSize : 2 * length * repeat), 1, Aup2Gpu.MaxSide);
			var t = c.Pass(@"uniform vec2 u_c; uniform float u_len; uniform float u_rot; uniform float u_wedge; uniform int u_mask;
void main(){
    vec2 d = (v_uv - 0.5) * u_size;
    float r = length(d);
    if (u_mask == 1 && r > u_size.x * 0.5) { o = vec4(0.0); return; }
    float a = mod(atan(d.y, d.x), u_wedge);
    if (a > u_wedge * 0.5) a = u_wedge - a;
    r = u_len - abs(mod(r, 2.0 * u_len) - u_len);
    vec2 q = (u_c + r * vec2(cos(a + u_rot), sin(a + u_rot))) / u_srcSize;
    o = (q.x < 0.0 || q.y < 0.0 || q.x > 1.0 || q.y > 1.0) ? vec4(0.0) : texture(u_src, q);
}", new Aup2Uniforms().V2("u_c", s.W / 2.0 + c.V("中心X"), s.H / 2.0 + c.V("中心Y")).F("u_len", length)
				.F("u_rot", c.V("回転") * Math.PI / 180).F("u_wedge", 2 * Math.PI / corners).I("u_mask", c.Flag("円形マスク") ? 1 : 0), side, side);
			c.Replace(t);
		}

		// a reflection added on one side
		private static void Mirror(Aup2Context c) {
			if (!c.Pixels()) return;
			var s = c.Surface!;
			int dir = c.S("ミラーの方向", "下側") switch { "上側" => 1, "右側" => 2, "左側" => 3, _ => 0 };
			bool horizontal = dir >= 2;
			int len = horizontal ? s.W : s.H;
			int gap = (int)Math.Max(-len, Math.Round(c.V("境目調整")));
			int total = Math.Min(Aup2Gpu.MaxSide, 2 * len + gap);
			if (total <= len) return;
			var t = c.Pass(@"uniform int u_dir; uniform float u_len; uniform float u_total; uniform float u_alpha; uniform float u_decay;
vec4 read(float along, float across){
    if (along < 0.0 || along > u_len) return vec4(0.0);
    vec2 q = u_dir >= 2 ? vec2(along, across) : vec2(across, along);
    return texture(u_src, q / u_srcSize);
}
void main(){
    vec2 pos = v_uv * u_size;
    float along = u_dir >= 2 ? pos.x : pos.y;
    float across = u_dir >= 2 ? pos.y : pos.x;
    // the reflection is on the far side: the near side first for the up and left mirrors
    if (u_dir == 1 || u_dir == 3) along = u_total - along;
    vec4 a = read(along, across);
    float far = u_total - along;
    float fade = u_alpha * clamp(1.0 - u_decay * (u_len - far) / max(u_len, 1.0), 0.0, 1.0);
    vec4 b = read(far, across) * fade;
    o = a + b * (1.0 - a.a);
}", new Aup2Uniforms().I("u_dir", dir).F("u_len", len).F("u_total", total)
				.F("u_alpha", 1 - Math.Clamp(c.V("透明度") / 100, 0, 1)).F("u_decay", Math.Max(0, c.V("減衰")) / 100),
				horizontal ? total : s.W, horizontal ? s.H : total);
			double shift = c.Flag("中心の位置を変更") ? 0 : (total - len) / 2.0;
			if (dir == 1 || dir == 3) shift = -shift;
			c.Replace(t, horizontal ? shift : 0, horizontal ? 0 : shift);
		}

		// the picture repeated side by side
		private static void Loop(Aup2Context c) {
			if (!c.Pixels()) return;
			var s = c.Surface!;
			int nx = Math.Clamp((int)c.V("横回数", 1), 1, Math.Max(1, Aup2Gpu.MaxSide / s.W));
			int ny = Math.Clamp((int)c.V("縦回数", 1), 1, Math.Max(1, Aup2Gpu.MaxSide / s.H));
			double ox = c.V("オフセットX"), oy = c.V("オフセットY");
			if (nx == 1 && ny == 1 && ox == 0 && oy == 0) return;
			int tw = s.W, th = s.H;
			var t = c.Pass("uniform vec2 u_off; void main(){ o = texture(u_src, fract((v_uv * u_size - u_off) / u_srcSize)); }",
				new Aup2Uniforms().V2("u_off", ox, oy), tw * nx, th * ny);
			c.Replace(t);
			if (c.Flag("個別オブジェクト") && nx * ny > 1 && nx * ny <= MaxParts) c.Item.Parts = Grid(tw * nx, th * ny, nx, ny);
		}
		#endregion

		#region Solids
		// the turn the object has, taken away from it so that the solid turns and not its flat picture
		private static Matrix4x4 TakeTurn(Aup2Context c) {
			var item = c.Item;
			double rx = item.RotX, ry = item.RotY, rz = item.RotZ;
			item.RotX = item.RotY = item.RotZ = 0;
			var draw = c.Object.Find(Aup2Evaluator.StandardDraw);
			if (draw != null && c.Object.Effects.IndexOf(draw) > c.Object.Effects.IndexOf(c.Effect)) {
				// the standard draw runs later and adds its turn then
				double ax = c.V(draw, "X軸回転"), ay = c.V(draw, "Y軸回転"), az = c.V(draw, "Z軸回転");
				rx += ax; ry += ay; rz += az;
				item.RotX -= ax; item.RotY -= ay; item.RotZ -= az;
			}
			if (item.UnderCamera) item.Billboard = true;
			return Aup2Evaluator.Turn(rx, ry, rz);
		}

		private static Aup2Uniforms Rows(Matrix4x4 m) => new Aup2Uniforms()
			.V3("u_r0", m.M11, m.M12, m.M13).V3("u_r1", m.M21, m.M22, m.M23).V3("u_r2", m.M31, m.M32, m.M33);

		// a cube seen straight on, the picture on each face
		private static void Cube(Aup2Context c) {
			double size = c.V("サイズ", 200);
			if (size < 1) { c.Skip = true; return; }
			if (!c.Pixels()) return;
			int side = (int)Math.Min(Aup2Gpu.MaxSide, Math.Ceiling(size * 1.7321));
			var t = c.Pass(@"uniform vec3 u_r0; uniform vec3 u_r1; uniform vec3 u_r2;
vec3 toObj(vec3 v){ return vec3(dot(v, u_r0), dot(v, u_r1), dot(v, u_r2)); }
float safe(float x){ return abs(x) < 1.0e-6 ? 1.0e-6 : x; }
vec4 face(vec2 p){
    vec3 ro = toObj(vec3(p, -4.0));
    vec3 rd = toObj(vec3(0.0, 0.0, 1.0));
    vec3 inv = 1.0 / vec3(safe(rd.x), safe(rd.y), safe(rd.z));
    vec3 t0 = (vec3(-1.0) - ro) * inv, t1 = (vec3(1.0) - ro) * inv;
    vec3 tn = min(t0, t1), tf = max(t0, t1);
    float a = max(max(tn.x, tn.y), tn.z), b = min(min(tf.x, tf.y), tf.z);
    if (a >= b) return vec4(0.0);
    vec3 h = ro + rd * a;
    vec3 ah = abs(h);
    vec2 uv; float lit;
    if (ah.z >= ah.x && ah.z >= ah.y) { uv = h.z < 0.0 ? vec2(h.x, h.y) : vec2(-h.x, h.y); lit = abs(rd.z); }
    else if (ah.x >= ah.y) { uv = h.x > 0.0 ? vec2(h.z, h.y) : vec2(-h.z, h.y); lit = abs(rd.x); }
    else { uv = h.y < 0.0 ? vec2(h.x, -h.z) : vec2(h.x, h.z); lit = abs(rd.y); }
    vec4 col = texture(u_src, clamp(uv * 0.5 + 0.5, 0.0, 1.0));
    return vec4(col.rgb * (0.6 + 0.4 * lit), col.a);
}
void main(){
    vec4 sum = vec4(0.0);
    for (int i = 0; i < 4; i++) {
        vec2 off = vec2(i == 0 || i == 2 ? -0.25 : 0.25, i < 2 ? -0.25 : 0.25);
        sum += face(((v_uv * u_size + off) / u_size * 2.0 - 1.0) * 1.7321);
    }
    o = sum * 0.25;
}", Rows(TakeTurn(c)), side, side);
			c.Replace(t);
		}

		// a ball seen straight on, the picture wrapped around it
		private static void Sphere(Aup2Context c) {
			double size = c.V("サイズ", 200);
			if (size < 1) { c.Skip = true; return; }
			if (!c.Pixels()) return;
			int side = (int)Math.Min(Aup2Gpu.MaxSide, Math.Ceiling(size));
			var t = c.Pass(@"uniform vec3 u_r0; uniform vec3 u_r1; uniform vec3 u_r2;
void main(){
    vec2 p = v_uv * 2.0 - 1.0;
    float r = length(p);
    if (r >= 1.0) { o = vec4(0.0); return; }
    vec3 n = vec3(p, -sqrt(1.0 - r * r));
    vec3 m = vec3(dot(n, u_r0), dot(n, u_r1), dot(n, u_r2));
    vec2 uv = vec2(atan(m.x, -m.z) / 6.2831853 + 0.5, asin(clamp(m.y, -1.0, 1.0)) / 3.14159265 + 0.5);
    vec4 col = texture(u_src, uv);
    float edge = clamp((1.0 - r) * u_size.x * 0.5, 0.0, 1.0);
    o = vec4(col.rgb * (0.6 - 0.4 * n.z), col.a) * edge;
}", Rows(TakeTurn(c)), side, side);
			c.Replace(t);
		}
		#endregion

		#region Pieces
		private static List<double> Numbers(string text) {
			var list = new List<double>();
			foreach (var piece in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				list.Add(double.TryParse(piece, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0);
			return list;
		}

		private static List<Aup2Part> Grid(int w, int h, int nx, int ny) {
			var parts = new List<Aup2Part>(nx * ny);
			for (int j = 0; j < ny; j++) {
				for (int i = 0; i < nx; i++) {
					double x0 = Math.Floor((double)w * i / nx), x1 = Math.Floor((double)w * (i + 1) / nx);
					double y0 = Math.Floor((double)h * j / ny), y1 = Math.Floor((double)h * (j + 1) / ny);
					parts.Add(new Aup2Part { Index = j * nx + i, Count = nx * ny, RectX = x0, RectY = y0, RectW = x1 - x0, RectH = y1 - y0 });
				}
			}
			return parts;
		}

		// the object's pieces; the whole picture as one piece when it has none. Null when there is no picture.
		private static List<Aup2Part>? Parts(Aup2Context c) {
			if (!c.Pixels()) return null;
			var s = c.Surface!;
			return c.Item.Parts ??= new List<Aup2Part> { new Aup2Part { RectW = s.W, RectH = s.H } };
		}

		// a single piece becomes n pieces that show the same rectangle
		private static void Repeat(List<Aup2Part> parts, int n) {
			if (parts.Count != 1 || n <= 1) {
				if (parts.Count == 1) parts[0].Count = 1;
				return;
			}
			var p = parts[0];
			p.Index = 0; p.Count = n;
			for (int i = 1; i < n; i++) {
				parts.Add(new Aup2Part {
					Index = i, Count = n, RectX = p.RectX, RectY = p.RectY, RectW = p.RectW, RectH = p.RectH,
					X = p.X, Y = p.Y, Z = p.Z, ScaleX = p.ScaleX, ScaleY = p.ScaleY, RotX = p.RotX, RotY = p.RotY, RotZ = p.RotZ, Opacity = p.Opacity,
				});
			}
		}

		private static void Split(Aup2Context c) {
			if (!c.Pixels()) return;
			var s = c.Surface!;
			int nx = Math.Clamp((int)c.V("横分割数", 10), 1, s.W), ny = Math.Clamp((int)c.V("縦分割数", 10), 1, s.H);
			while ((long)nx * ny > MaxParts) {
				if (nx >= ny) nx = Math.Max(1, nx / 2); else ny = Math.Max(1, ny / 2);
			}
			c.Item.Parts = Grid(s.W, s.H, nx, ny);
		}

		// where each piece stands moves away from, or towards, a point of the object
		private static void ScalePositions(Aup2Context c) {
			var parts = Parts(c);
			if (parts == null) return;
			var s = c.Surface!;
			double k = c.V("拡大率", 100) / 100, cx = c.V("中心X"), cy = c.V("中心Y");
			foreach (var p in parts) {
				double x = p.RectX + p.RectW / 2 - s.W / 2.0 + c.Item.OffsetX + p.X - cx;
				double y = p.RectY + p.RectH / 2 - s.H / 2.0 + c.Item.OffsetY + p.Y - cy;
				p.X += x * (k - 1);
				p.Y += y * (k - 1);
			}
		}

		// where each piece stands turns about a point of the object; the pieces keep their own angle
		private static void TurnPositions(Aup2Context c) {
			var parts = Parts(c);
			if (parts == null) return;
			var s = c.Surface!;
			double a = c.V("角度") * Math.PI / 180, cs = Math.Cos(a), sn = Math.Sin(a), cx = c.V("中心X"), cy = c.V("中心Y");
			foreach (var p in parts) {
				double x = p.RectX + p.RectW / 2 - s.W / 2.0 + c.Item.OffsetX + p.X - cx;
				double y = p.RectY + p.RectH / 2 - s.H / 2.0 + c.Item.OffsetY + p.Y - cy;
				p.X += x * cs - y * sn - x;
				p.Y += x * sn + y * cs - y;
			}
		}

		// the pieces around a circle, the first at the top, clockwise; one picture is repeated
		private static void Circle(Aup2Context c) {
			var parts = Parts(c);
			if (parts == null) return;
			var s = c.Surface!;
			Repeat(parts, Math.Clamp((int)c.V("数", 8), 1, MaxParts));
			int n = parts.Count;
			double radius = c.V("半径", 100), arc = c.V("円周", 100) / 100 * 360;
			double step = arc / n, start = c.V("回転") + c.V("ずらし") / 100 * step;
			bool flat = c.Flag("水平配置(カメラ制御)"), reversed = c.Flag("向き反転"), still = c.Flag("回転無し");
			for (int i = 0; i < n; i++) {
				var p = parts[i];
				double deg = start + step * i, a = deg * Math.PI / 180;
				double ox = radius * Math.Sin(a), oy = -radius * Math.Cos(a);
				p.X = ox - (p.RectX + p.RectW / 2 - s.W / 2.0);
				if (flat) {
					// the circle lies down: its top is the far side
					p.Y = 0 - (p.RectY + p.RectH / 2 - s.H / 2.0);
					p.Z = -oy;
					if (!still) p.RotY += -deg + (reversed ? 180 : 0);
				} else {
					p.Y = oy - (p.RectY + p.RectH / 2 - s.H / 2.0);
					if (!still) p.RotZ += deg + (reversed ? 180 : 0);
				}
			}
		}

		// the pieces thrown at random inside a square (a cube with depth); one picture is repeated
		private static void Scatter(Aup2Context c) {
			var parts = Parts(c);
			if (parts == null) return;
			Repeat(parts, Math.Clamp((int)c.V("数", 50), 1, MaxParts));
			double range = c.V("範囲", 800), spread = 1 + c.V("拡散") / 100;
			double a = c.V("回転") * Math.PI / 180, cs = Math.Cos(a), sn = Math.Sin(a);
			bool deep = c.Flag("3D配置(カメラ制御)"), angles = c.Flag("ランダム角度");
			for (int i = 0; i < parts.Count; i++) {
				var p = parts[i];
				double x = (c.Random("scatter x", i) - 0.5) * range * spread, y = (c.Random("scatter y", i) - 0.5) * range * spread;
				p.X += x * cs - y * sn;
				p.Y += x * sn + y * cs;
				if (deep) p.Z += (c.Random("scatter z", i) - 0.5) * range * spread;
				if (angles) {
					p.RotZ += c.Random("scatter turn", i) * 360;
					if (deep) {
						p.RotX += c.Random("scatter turn x", i) * 360;
						p.RotY += c.Random("scatter turn y", i) * 360;
					}
				}
			}
		}
		#endregion
	}
}
