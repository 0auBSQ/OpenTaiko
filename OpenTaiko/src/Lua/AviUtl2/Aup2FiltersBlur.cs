using System;
using System.Collections.Generic;
using System.Numerics;

namespace OpenTaiko.AviUtl2 {
	// The blur, light and edge effects.
	internal static class Aup2FiltersBlur {
		public static void Register(Dictionary<string, Aup2FilterFn> map) {
			map["ぼかし"] = BlurEffect;
			map["レンズブラー"] = LensBlur;
			map["放射ブラー"] = RadialBlur;
			map["方向ブラー"] = DirectionalBlur;
			map["境界ぼかし"] = BorderBlur;
			map["グロー"] = Glow;
			map["発光"] = Luminescence;
			map["閃光"] = Flash;
			map["拡散光"] = DiffuseLight;
			map["ライト"] = Light;
			map["色ずれ"] = ColorShift;
			map["縁取り"] = Outline;
			map["枠線"] = FrameLine;
			map["ドロップシャドウ"] = DropShadow;
			map["凸エッジ"] = Bevel;
			map["エッジ抽出"] = EdgeExtract;
		}

		#region Shaders
		// a 4x4 box over the source pixels each target pixel covers
		private const string FsDown = @"uniform vec2 u_f;
void main(){
    vec2 c = v_uv * u_srcSize;
    vec4 acc = vec4(0.0);
    for (int y = 0; y < 4; y++) {
        for (int x = 0; x < 4; x++) {
            vec2 d = (vec2(float(x), float(y)) + 0.5) / 4.0 - 0.5;
            acc += texture(u_src, (c + d * u_f) / u_srcSize);
        }
    }
    o = acc / 16.0;
}";

		private const string FsCopy = @"void main(){ o = texture(u_src, v_uv); }";

		private const string FsPad = @"uniform vec2 u_off;
void main(){
    vec2 p = (v_uv * u_size - u_off) / u_srcSize;
    o = (p.x < 0.0 || p.y < 0.0 || p.x > 1.0 || p.y > 1.0) ? vec4(0.0) : texture(u_src, p);
}";

		// one direction of a gaussian, u_radius pixels wide on each side; u_light lets bright pixels weigh more
		private const string FsGauss = @"uniform vec2 u_dir; uniform float u_radius; uniform float u_light;
void main(){
    vec2 pos = v_uv * u_srcSize;
    float n = clamp(ceil(u_radius), 1.0, 16.0);
    float stepPx = u_radius / n;
    vec4 acc = vec4(0.0);
    float sum = 0.0;
    for (int i = -16; i <= 16; i++) {
        float fi = float(i);
        if (abs(fi) > n) continue;
        float t = fi / n;
        vec4 s = texture(u_src, (pos + u_dir * fi * stepPx) / u_srcSize);
        float w = exp(-2.0 * t * t) * (1.0 + u_light * dot(s.rgb, vec3(0.299, 0.587, 0.114)));
        acc += s * w;
        sum += w;
    }
    o = acc / max(sum, 0.0001);
}";

		// taps along a line, u_step pixels apart; u_tri makes the far ones weigh less
		private const string FsLine = @"uniform vec2 u_step; uniform float u_n; uniform float u_tri;
void main(){
    vec2 pos = v_uv * u_srcSize;
    vec4 acc = vec4(0.0);
    float sum = 0.0;
    for (int i = -16; i <= 16; i++) {
        float fi = float(i);
        if (abs(fi) > u_n) continue;
        float w = 1.0 - u_tri * abs(fi) / (u_n + 1.0);
        acc += texture(u_src, (pos + u_step * fi) / u_srcSize) * w;
        sum += w;
    }
    o = acc / max(sum, 0.0001);
}";

		// taps from each pixel towards u_c, over the share u_k of the way
		private const string FsRadial = @"uniform vec2 u_c; uniform float u_k; uniform float u_decay;
void main(){
    vec2 pos = v_uv * u_srcSize;
    vec4 acc = vec4(0.0);
    float sum = 0.0;
    for (int i = 0; i < 32; i++) {
        float t = float(i) / 31.0;
        float w = 1.0 - u_decay * t;
        acc += texture(u_src, (u_c + (pos - u_c) * (1.0 - u_k * t)) / u_srcSize) * w;
        sum += w;
    }
    o = acc / max(sum, 0.0001);
}";

		// taps spread over a disc or a polygon of u_sides sides
		private const string FsDisc = @"uniform float u_radius; uniform float u_light; uniform float u_sides;
void main(){
    vec2 pos = v_uv * u_srcSize;
    vec4 acc = vec4(0.0);
    float sum = 0.0;
    for (int i = 0; i < 32; i++) {
        float fi = float(i);
        float r = sqrt((fi + 0.5) / 32.0);
        float a = fi * 2.39996323;
        if (u_sides > 2.5) {
            float seg = 6.2831853 / u_sides;
            r *= cos(seg * 0.5) / cos(mod(a, seg) - seg * 0.5);
        }
        vec4 s = texture(u_src, (pos + vec2(cos(a), sin(a)) * r * u_radius) / u_srcSize);
        float y = dot(s.rgb, vec3(0.299, 0.587, 0.114));
        float w = 1.0 + u_light * y * y;
        acc += s * w;
        sum += w;
    }
    o = acc / max(sum, 0.0001);
}";

		// what is brighter than the threshold, in its own colour or in u_tint
		private const string FsBright = @"uniform float u_thr; uniform vec3 u_tint; uniform float u_useTint;
void main(){
    vec4 p = texture(u_src, v_uv);
    vec3 col = p.a > 0.0 ? p.rgb / p.a : vec3(0.0);
    float y = dot(col, vec3(0.299, 0.587, 0.114));
    float k = u_thr >= 1.0 ? 0.0 : clamp((y - u_thr) / (1.0 - u_thr), 0.0, 1.0);
    if (u_useTint > 0.5) col = u_tint;
    float a = p.a * k;
    o = vec4(col * a, a);
}";

		// the picture and a light layer: 0 added, 1 screened, 2 the light behind
		private const string FsCombine = @"uniform sampler2D u_other; uniform float u_k; uniform float u_keep; uniform int u_mode;
void main(){
    vec4 p = texture(u_src, v_uv) * u_keep;
    vec4 g = texture(u_other, v_uv) * u_k;
    if (u_mode == 2) {
        g = min(g, vec4(1.0));
        p = min(p, vec4(1.0));
        o = p + g * (1.0 - p.a);
        return;
    }
    vec3 rgb = u_mode == 1 ? p.rgb + min(g.rgb, vec3(1.0)) * (1.0 - min(p.rgb, vec3(1.0))) : p.rgb + g.rgb;
    rgb = min(rgb, vec3(1.0));
    float a = clamp(p.a + g.a * (1.0 - p.a), 0.0, 1.0);
    o = vec4(rgb, max(a, max(rgb.r, max(rgb.g, rgb.b))));
}";

		// the picture over the alpha of another one, moved and coloured
		private const string FsUnder = @"uniform sampler2D u_other; uniform vec2 u_off; uniform vec3 u_col; uniform float u_k;
void main(){
    vec4 p = texture(u_src, v_uv);
    vec2 q = (v_uv * u_size - u_off) / u_size;
    float s = (q.x < 0.0 || q.y < 0.0 || q.x > 1.0 || q.y > 1.0) ? 0.0 : texture(u_other, q).a;
    s = clamp(s * u_k, 0.0, 1.0);
    o = p + vec4(u_col * s, s) * (1.0 - p.a);
}";

		private const string FsBorder = @"uniform sampler2D u_other; uniform vec2 u_r; uniform float u_useAlpha;
void main(){
    vec4 p = texture(u_src, v_uv);
    vec2 pos = v_uv * u_size;
    vec2 d = min(pos, u_size - pos);
    float f = clamp(d.x / max(u_r.x, 0.001), 0.0, 1.0) * clamp(d.y / max(u_r.y, 0.001), 0.0, 1.0);
    if (u_useAlpha > 0.5) {
        float b = texture(u_other, v_uv).a;
        f *= clamp(2.0 * b / max(p.a, 0.001) - 1.0, 0.0, 1.0);
    }
    o = p * f;
}";

		// a halo from the blurred shape, and a lit rim inside it unless the light is behind
		private const string FsLight = @"uniform sampler2D u_other; uniform vec3 u_col; uniform float u_k; uniform float u_back;
void main(){
    vec4 p = texture(u_src, v_uv);
    float b = texture(u_other, v_uv).a;
    float halo = clamp(b * u_k, 0.0, 1.0);
    vec4 r = p + vec4(u_col * halo, halo) * (1.0 - p.a);
    if (u_back < 0.5) {
        float rim = clamp((1.0 - b) * 2.0 * u_k, 0.0, 1.0) * p.a;
        r.rgb = min(r.rgb + u_col * rim, vec3(r.a));
    }
    o = r;
}";

		private const string FsShift = @"uniform vec2 u_d; uniform int u_type; uniform int u_split; uniform float u_k;
void main(){
    vec4 p0 = texture(u_src, v_uv);
    vec4 p1 = texture(u_src, v_uv + u_d / u_srcSize);
    vec4 p2 = texture(u_src, v_uv - u_d / u_srcSize);
    vec3 still = u_split == 1 ? (p1.rgb + p2.rgb) * 0.5 : p0.rgb;
    vec3 rgb;
    if (u_type == 0) rgb = vec3(p1.r, p2.g, still.b);
    else if (u_type == 1) rgb = vec3(p1.r, still.g, p2.b);
    else rgb = vec3(still.r, p1.g, p2.b);
    float a = max(p0.a, max(p1.a, p2.a));
    o = mix(p0, vec4(rgb, a), u_k);
}";

		// the alpha grown by u_r pixels all around
		private const string FsDilate = @"uniform float u_r;
void main(){
    float m = texture(u_src, v_uv).a;
    for (int i = 0; i < 16; i++) {
        float a = float(i) * 0.39269908;
        m = max(m, texture(u_src, v_uv + vec2(cos(a), sin(a)) * u_r / u_srcSize).a);
    }
    for (int i = 0; i < 8; i++) {
        float a = (float(i) + 0.5) * 0.78539816;
        m = max(m, texture(u_src, v_uv + vec2(cos(a), sin(a)) * u_r * 0.5 / u_srcSize).a);
    }
    o = vec4(m);
}";

		private const string FsFrame = @"uniform float u_t; uniform float u_round; uniform vec3 u_col;
float box(vec2 p, vec2 hs, float r){
    vec2 q = abs(p) - hs + r;
    return length(max(q, vec2(0.0))) + min(max(q.x, q.y), 0.0) - r;
}
void main(){
    vec4 p = texture(u_src, v_uv);
    vec2 hs = u_size * 0.5;
    vec2 pos = v_uv * u_size - hs;
    float dOut = box(pos, hs, u_round);
    float dIn = box(pos, max(hs - u_t, vec2(0.0)), max(u_round - u_t, 0.0));
    float f = clamp(0.5 - dOut, 0.0, 1.0) * clamp(0.5 + dIn, 0.0, 1.0);
    o = vec4(u_col * f, f) + p * (1.0 - f);
}";

		// light and shade from the slope of the blurred alpha
		private const string FsBevel = @"uniform sampler2D u_other; uniform vec2 u_otherSize; uniform float u_m; uniform float u_w; uniform vec2 u_light; uniform float u_h;
void main(){
    vec4 p = texture(u_src, v_uv);
    vec2 q = v_uv * u_size + u_m;
    float e = max(u_w * 0.5, 0.5);
    float gx = texture(u_other, (q + vec2(e, 0.0)) / u_otherSize).a - texture(u_other, (q - vec2(e, 0.0)) / u_otherSize).a;
    float gy = texture(u_other, (q + vec2(0.0, e)) / u_otherSize).a - texture(u_other, (q - vec2(0.0, e)) / u_otherSize).a;
    float d = -(gx * u_light.x + gy * u_light.y) * 2.0 * u_h;
    vec3 col = p.a > 0.0 ? p.rgb / p.a : vec3(0.0);
    col = d > 0.0 ? mix(col, vec3(1.0), clamp(d, 0.0, 1.0)) : col * (1.0 - clamp(-d, 0.0, 1.0));
    o = vec4(col * p.a, p.a);
}";

		private const string FsEdge = @"uniform float u_k; uniform float u_thr; uniform vec3 u_col; uniform float u_luma; uniform float u_alpha;
vec2 val(vec2 off){
    vec4 p = texture(u_src, v_uv + off / u_srcSize);
    return vec2(dot(p.rgb, vec3(0.299, 0.587, 0.114)), p.a);
}
void main(){
    vec2 tl = val(vec2(-1.0, -1.0)), tc = val(vec2(0.0, -1.0)), tr = val(vec2(1.0, -1.0));
    vec2 ml = val(vec2(-1.0, 0.0)), mr = val(vec2(1.0, 0.0));
    vec2 bl = val(vec2(-1.0, 1.0)), bc = val(vec2(0.0, 1.0)), br = val(vec2(1.0, 1.0));
    vec2 gx = (tr + 2.0 * mr + br) - (tl + 2.0 * ml + bl);
    vec2 gy = (bl + 2.0 * bc + br) - (tl + 2.0 * tc + tr);
    float e = u_luma * length(vec2(gx.x, gy.x)) + u_alpha * length(vec2(gx.y, gy.y));
    float a = clamp(e * 0.5 * u_k - u_thr, 0.0, 1.0);
    o = vec4(u_col * a, a);
}";
		#endregion

		#region Helpers
		// effects that work on the frame itself never change its size
		private static bool Keep(Aup2Context c) => c.Flag("サイズ固定") || c.Item.Kind == Aup2ItemKind.Filter;

		private static double Finite(double v, double fallback = 0) => double.IsFinite(v) ? v : fallback;

		// angles count clockwise from straight up
		private static (double x, double y) Dir(double degrees) {
			double a = Finite(degrees) * Math.PI / 180;
			return (Math.Sin(a), -Math.Cos(a));
		}

		// margins that keep the picture inside the largest surface
		private static void Spread(Aup2Context c, double left, double top, double right, double bottom) {
			if (!c.Pixels()) return;
			var s = c.Surface!;
			int l = (int)Math.Ceiling(Math.Clamp(Finite(left), 0, Aup2Gpu.MaxSide)), r = (int)Math.Ceiling(Math.Clamp(Finite(right), 0, Aup2Gpu.MaxSide));
			int t = (int)Math.Ceiling(Math.Clamp(Finite(top), 0, Aup2Gpu.MaxSide)), b = (int)Math.Ceiling(Math.Clamp(Finite(bottom), 0, Aup2Gpu.MaxSide));
			int roomW = Math.Max(0, Aup2Gpu.MaxSide - s.W), roomH = Math.Max(0, Aup2Gpu.MaxSide - s.H);
			if (l + r > roomW) { double k = roomW / (double)(l + r); l = (int)(l * k); r = (int)(r * k); }
			if (t + b > roomH) { double k = roomH / (double)(t + b); t = (int)(t * k); b = (int)(b * k); }
			c.Grow(l, t, r, b);
		}
		private static void Spread(Aup2Context c, double x, double y) => Spread(c, x, y, x, y);

		// a new surface: src blurred by rx, ry pixels. A wide blur runs on a smaller copy.
		private static Aup2Surface Blur(Aup2Context c, Aup2Surface src, double rx, double ry, double light = 0) {
			var gpu = c.Gpu!;
			rx = Math.Clamp(Finite(rx), 0, Aup2Gpu.MaxSide);
			ry = Math.Clamp(Finite(ry), 0, Aup2Gpu.MaxSide);
			if (rx < 0.01 && ry < 0.01) return gpu.Copy(src);
			int f = Math.Max(1, (int)Math.Ceiling(Math.Max(rx, ry) / 16));
			var cur = src;
			if (f > 1) {
				int w = Math.Max(1, (src.W + f - 1) / f), h = Math.Max(1, (src.H + f - 1) / f);
				cur = c.Pass(FsDown, new Aup2Uniforms().V2("u_f", (double)src.W / w, (double)src.H / h), w, h, src)!;
			}
			double sx = rx * cur.W / src.W, sy = ry * cur.H / src.H;
			if (sx >= 0.01) {
				var t = c.Pass(FsGauss, new Aup2Uniforms().V2("u_dir", 1, 0).F("u_radius", sx).F("u_light", light), 0, 0, cur)!;
				if (cur != src) gpu.Release(cur);
				cur = t;
				light = 0;
			}
			if (sy >= 0.01) {
				var t = c.Pass(FsGauss, new Aup2Uniforms().V2("u_dir", 0, 1).F("u_radius", sy).F("u_light", light), 0, 0, cur)!;
				if (cur != src) gpu.Release(cur);
				cur = t;
			}
			if (cur.W != src.W || cur.H != src.H) {
				var t = c.Pass(FsCopy, null, src.W, src.H, cur)!;
				gpu.Release(cur);
				cur = t;
			}
			return cur == src ? gpu.Copy(src) : cur;
		}

		// a new surface: src smeared over `length` pixels each way along (dx, dy)
		private static Aup2Surface LineBlur(Aup2Context c, Aup2Surface src, double dx, double dy, double length, bool fade) {
			length = Math.Clamp(Finite(length), 0.01, Aup2Gpu.MaxSide);
			double n = Math.Clamp(Math.Ceiling(length), 1, 16), step = length / n;
			var cur = c.Pass(FsLine, new Aup2Uniforms().V2("u_step", dx * step, dy * step).F("u_n", n).F("u_tri", fade ? 1 : 0), 0, 0, src)!;
			if (step > 1.5) {
				// fills the gaps between the taps of the first pass
				double half = step / 2, n2 = Math.Clamp(Math.Ceiling(half), 1, 16), step2 = half / n2;
				var t = c.Pass(FsLine, new Aup2Uniforms().V2("u_step", dx * step2, dy * step2).F("u_n", n2).F("u_tri", 0), 0, 0, cur)!;
				c.Gpu!.Release(cur);
				cur = t;
			}
			return cur;
		}

		// a new surface: src smeared away from (cx, cy), each pixel reading up to the share k of the way to it
		private static Aup2Surface Radial(Aup2Context c, Aup2Surface src, double cx, double cy, double k, double decay) {
			var cur = c.Pass(FsRadial, new Aup2Uniforms().V2("u_c", cx, cy).F("u_k", k).F("u_decay", decay), 0, 0, src)!;
			double far = Math.Max(Math.Max(cx, src.W - cx), 0) + Math.Max(Math.Max(cy, src.H - cy), 0);
			if (k * far / 31 > 1.5) {
				var t = c.Pass(FsRadial, new Aup2Uniforms().V2("u_c", cx, cy).F("u_k", k / 31).F("u_decay", 0), 0, 0, cur)!;
				c.Gpu!.Release(cur);
				cur = t;
			}
			return cur;
		}

		// the object's centre in its picture, moved by the effect's X and Y
		private static (double x, double y) Centre(Aup2Context c) {
			var s = c.Surface!;
			return (s.W / 2.0 - c.Item.OffsetX + Finite(c.V("X")), s.H / 2.0 - c.Item.OffsetY + Finite(c.V("Y")));
		}

		private static Aup2Surface Bright(Aup2Context c, double threshold, string colourKey) {
			string text = c.S(colourKey).Trim();
			bool tint = text.Length == 6;
			var col = Aup2Context.ParseColor(text, Vector3.One);
			return c.Pass(FsBright, new Aup2Uniforms().F("u_thr", threshold).V3("u_tint", col).F("u_useTint", tint ? 1 : 0))!;
		}

		// the picture with a light layer of the same size; the layer is released
		private static void Lit(Aup2Context c, Aup2Surface light, int mode, double k, bool lightOnly = false) {
			var t = c.Pass(FsCombine, new Aup2Uniforms().Tex("u_other", light).F("u_k", k).F("u_keep", lightOnly ? 0 : 1).I("u_mode", mode));
			c.Replace(t);
			c.Gpu!.Release(light);
		}

		private static void Aspect(double r, double aspect, out double rx, out double ry) {
			aspect = Math.Clamp(Finite(aspect), -100, 100);
			rx = r * (aspect > 0 ? 1 - aspect / 100 : 1);
			ry = r * (aspect < 0 ? 1 + aspect / 100 : 1);
		}
		#endregion

		private static void BlurEffect(Aup2Context c) {
			double r = Finite(c.V("範囲"));
			if (r <= 0 || !c.Pixels()) return;
			Aspect(r, c.V("縦横比"), out double rx, out double ry);
			if (!Keep(c)) Spread(c, rx, ry);
			c.Replace(Blur(c, c.Surface!, rx, ry, Math.Max(0, Finite(c.V("光の強さ"))) / 16));
		}

		private static void LensBlur(Aup2Context c) {
			double r = Math.Min(Finite(c.V("範囲")), 1024);
			if (r <= 0 || !c.Pixels()) return;
			if (!Keep(c)) Spread(c, r, r);
			var gpu = c.Gpu!;
			var src = c.Surface!;
			string shape = c.S("形状", "円");
			int sides = shape.Contains('三') ? 3 : shape.Contains('四') ? 4 : shape.Contains('五') ? 5 : shape.Contains('六') ? 6 : shape.Contains('八') ? 8 : 0;
			int f = Math.Max(1, (int)Math.Ceiling(r / 8));
			var small = src;
			if (f > 1) {
				int w = Math.Max(1, (src.W + f - 1) / f), h = Math.Max(1, (src.H + f - 1) / f);
				small = c.Pass(FsDown, new Aup2Uniforms().V2("u_f", (double)src.W / w, (double)src.H / h), w, h)!;
			}
			double low = r * small.W / src.W;
			var disc = c.Pass(FsDisc, new Aup2Uniforms().F("u_radius", low).F("u_light", Math.Max(0, Finite(c.V("光の強さ"))) / 16).F("u_sides", sides), 0, 0, small)!;
			if (small != src) gpu.Release(small);
			// hides the grain of the 32 taps
			double soften = Math.Min(1.5, low * 0.3);
			var soft = Blur(c, disc, soften, soften);
			gpu.Release(disc);
			if (soft.W != src.W || soft.H != src.H) {
				var up = c.Pass(FsCopy, null, src.W, src.H, soft)!;
				gpu.Release(soft);
				soft = up;
			}
			c.Replace(soft);
		}

		private static void RadialBlur(Aup2Context c) {
			double k = Math.Clamp(Finite(c.V("範囲")) / 100, 0, 0.9);
			if (k <= 0 || !c.Pixels()) return;
			if (!Keep(c)) {
				var s = c.Surface!;
				double reach = Math.Max(s.W, s.H) / 2.0 + Math.Max(Math.Abs(Finite(c.V("X"))), Math.Abs(Finite(c.V("Y"))));
				double m = Math.Min(1024, reach * k / (1 - k));
				Spread(c, m, m);
			}
			var (cx, cy) = Centre(c);
			c.Replace(Radial(c, c.Surface!, cx, cy, k, 0));
		}

		private static void DirectionalBlur(Aup2Context c) {
			double r = Math.Min(Finite(c.V("範囲")), 1024);
			if (r <= 0 || !c.Pixels()) return;
			var (dx, dy) = Dir(c.V("角度"));
			if (!Keep(c)) Spread(c, Math.Abs(dx) * r, Math.Abs(dy) * r);
			c.Replace(LineBlur(c, c.Surface!, dx, dy, r, false));
		}

		private static void BorderBlur(Aup2Context c) {
			double r = Finite(c.V("範囲"));
			if (r <= 0 || !c.Pixels()) return;
			Aspect(r, c.V("縦横比"), out double rx, out double ry);
			bool alpha = c.Flag("透明度の境界をぼかす");
			var other = alpha ? Blur(c, c.Surface!, rx, ry) : c.Surface!;
			var t = c.Pass(FsBorder, new Aup2Uniforms().Tex("u_other", other).V2("u_r", rx, ry).F("u_useAlpha", alpha ? 1 : 0));
			if (alpha) c.Gpu!.Release(other);
			c.Replace(t);
		}

		private static void Glow(Aup2Context c) {
			double k = Finite(c.V("強さ")) / 100, r = Math.Min(Finite(c.V("拡散")), 1024);
			if (k <= 0 || r <= 0 || !c.Pixels()) return;
			string shape = c.S("形状", "通常");
			int lines = shape.Contains('8') ? 4 : shape.Contains('4') ? 2 : shape.Contains("ライン") ? 1 : 0;
			double angle = Finite(c.V("角度")) + (shape.Contains("斜め") ? 45 : 0) + (shape.Contains('横') ? 90 : 0);
			double length = lines > 0 ? r * 2 : r;
			if (!Keep(c)) Spread(c, length, length);
			var gpu = c.Gpu!;
			var bright = Bright(c, Finite(c.V("しきい値")) / 100, "光色");
			Aup2Surface light;
			if (lines == 0) {
				light = Blur(c, bright, r, r);
			} else {
				Aup2Surface? acc = null;
				for (int i = 0; i < lines; i++) {
					var (dx, dy) = Dir(angle + i * 180.0 / lines);
					var line = LineBlur(c, bright, dx, dy, length, true);
					if (acc == null) { acc = line; continue; }
					var sum = c.Pass(FsCombine, new Aup2Uniforms().Tex("u_other", line).F("u_k", 1).F("u_keep", 1).I("u_mode", 0), 0, 0, acc)!;
					gpu.Release(acc);
					gpu.Release(line);
					acc = sum;
				}
				light = acc!;
				if (c.V("ぼかし", 1) > 0) {
					var soft = Blur(c, light, 2, 2);
					gpu.Release(light);
					light = soft;
				}
				// the rays are thin: they are raised to be seen, and share the light with a round halo
				double ratio = Math.Clamp(Finite(c.V("比率", 100)) / 100, 0, 1);
				var halo = ratio < 1 ? Blur(c, bright, r, r) : light;
				var mixed = c.Pass(FsCombine, new Aup2Uniforms().Tex("u_other", halo).F("u_k", ratio < 1 ? 1 - ratio : 0).F("u_keep", 3 * ratio).I("u_mode", 0), 0, 0, light)!;
				if (halo != light) gpu.Release(halo);
				gpu.Release(light);
				light = mixed;
			}
			gpu.Release(bright);
			Lit(c, light, 0, k, c.Flag("光成分のみ"));
		}

		private static void Luminescence(Aup2Context c) {
			double k = Finite(c.V("強さ")) / 100, r = Math.Min(Finite(c.V("拡散")), 1024);
			if (k <= 0 || !c.Pixels()) return;
			r = Math.Max(r, 0);
			if (!Keep(c)) Spread(c, r, r);
			var bright = Bright(c, Finite(c.V("しきい値")) / 100, "光色");
			var light = Blur(c, bright, r, r);
			c.Gpu!.Release(bright);
			Lit(c, light, 0, k);
		}

		private static void Flash(Aup2Context c) {
			double strength = Finite(c.V("強さ"));
			if (strength <= 0 || !c.Pixels()) return;
			double k = Math.Min(strength / (strength + 100), 0.9);
			if (!Keep(c)) {
				var s = c.Surface!;
				double reach = Math.Max(s.W, s.H) / 2.0 + Math.Max(Math.Abs(Finite(c.V("X"))), Math.Abs(Finite(c.V("Y"))));
				double m = Math.Min(1024, reach * k / (1 - k));
				Spread(c, m, m);
			}
			var (cx, cy) = Centre(c);
			var bright = Bright(c, -1000, "光色");
			var rays = Radial(c, bright, cx, cy, k, 1);
			c.Gpu!.Release(bright);
			string mode = c.S("合成モード", "前方に合成");
			bool only = mode.Contains("のみ");
			Lit(c, rays, mode.Contains("後方") ? 2 : 0, 2, only);
		}

		private static void DiffuseLight(Aup2Context c) {
			double k = Finite(c.V("強さ")) / 100, r = Math.Min(Finite(c.V("拡散")), 1024);
			if (k <= 0 || r <= 0 || !c.Pixels()) return;
			if (!Keep(c)) Spread(c, r, r);
			Lit(c, Blur(c, c.Surface!, r, r), 1, k);
		}

		private static void Light(Aup2Context c) {
			double k = Finite(c.V("強さ")) / 100, r = Math.Min(Finite(c.V("拡散")), 1024);
			if (k <= 0 || r <= 0 || !c.Pixels()) return;
			Aspect(r, c.V("比率"), out double rx, out double ry);
			if (!Keep(c)) Spread(c, rx, ry);
			var shape = Blur(c, c.Surface!, rx, ry);
			var t = c.Pass(FsLight, new Aup2Uniforms().Tex("u_other", shape).V3("u_col", c.Color("色")).F("u_k", k).F("u_back", c.Flag("逆光") ? 1 : 0));
			c.Gpu!.Release(shape);
			c.Replace(t);
		}

		private static void ColorShift(Aup2Context c) {
			double width = Math.Clamp(Finite(c.V("ずれ幅")), -1024, 1024), k = Math.Clamp(Finite(c.V("強さ", 100)) / 100, 0, 1);
			if (Math.Abs(width) < 0.01 || k <= 0 || !c.Pixels()) return;
			var (dx, dy) = Dir(c.V("角度"));
			if (!Keep(c)) Spread(c, Math.Abs(dx * width), Math.Abs(dy * width));
			string kind = c.S("色ずれの種類", "赤緑A");
			int type = kind.StartsWith("赤緑", StringComparison.Ordinal) ? 0 : kind.StartsWith("赤青", StringComparison.Ordinal) ? 1 : 2;
			c.Filter(FsShift, new Aup2Uniforms().V2("u_d", dx * width, dy * width).I("u_type", type).I("u_split", kind.EndsWith('B') ? 1 : 0).F("u_k", k));
		}

		private static void Outline(Aup2Context c) {
			double size = Math.Min(Finite(c.V("サイズ")), 512);
			if (size <= 0 || !c.Pixels()) return;
			double blur = size * Math.Clamp(Finite(c.V("ぼかし")), 0, 100) / 50;
			if (!Keep(c)) { double m = size + blur + 1; Spread(c, m, m); }
			var gpu = c.Gpu!;
			int passes = Math.Clamp((int)Math.Ceiling(size / 4), 1, 8);
			var cur = c.Surface!;
			for (int i = 0; i < passes; i++) {
				var t = c.Pass(FsDilate, new Aup2Uniforms().F("u_r", size / passes), 0, 0, cur)!;
				if (cur != c.Surface) gpu.Release(cur);
				cur = t;
			}
			if (blur >= 0.01) {
				var t = Blur(c, cur, blur, blur);
				gpu.Release(cur);
				cur = t;
			}
			var r = c.Pass(FsUnder, new Aup2Uniforms().Tex("u_other", cur).V2("u_off", 0, 0).V3("u_col", c.Color("縁色")).F("u_k", 1));
			gpu.Release(cur);
			c.Replace(r);
		}

		private static void FrameLine(Aup2Context c) {
			double size = Math.Min(Finite(c.V("サイズ")), 1024);
			if (size <= 0 || !c.Pixels()) return;
			if (!Keep(c)) Spread(c, Finite(c.V("X")) + size, Finite(c.V("Y")) + size);
			var s = c.Surface!;
			double round = Math.Clamp(Finite(c.V("丸み")), 0, Math.Min(s.W, s.H) / 2.0);
			c.Filter(FsFrame, new Aup2Uniforms().F("u_t", size).F("u_round", round).V3("u_col", c.Color("枠色")));
		}

		private static void DropShadow(Aup2Context c) {
			double k = Math.Clamp(Finite(c.V("濃さ")) / 100, 0, 1);
			if (k <= 0 || !c.Pixels()) return;
			double x = Math.Clamp(Finite(c.V("X")), -2048, 2048), y = Math.Clamp(Finite(c.V("Y")), -2048, 2048);
			double r = Math.Clamp(Finite(c.V("拡散")), 0, 1024);
			if (!Keep(c)) Spread(c, Math.Max(0, -x) + r, Math.Max(0, -y) + r, Math.Max(0, x) + r, Math.Max(0, y) + r);
			var shade = Blur(c, c.Surface!, r, r);
			var t = c.Pass(FsUnder, new Aup2Uniforms().Tex("u_other", shade).V2("u_off", x, y).V3("u_col", c.Color("影色", Vector3.Zero)).F("u_k", k));
			c.Gpu!.Release(shade);
			c.Replace(t);
		}

		private static void Bevel(Aup2Context c) {
			double width = Math.Clamp(Finite(c.V("幅")), 0, 256), height = Finite(c.V("高さ"));
			if (width <= 0 || height == 0 || !c.Pixels()) return;
			var gpu = c.Gpu!;
			var s = c.Surface!;
			// the picture's own border counts as an edge, so the alpha is blurred with room around it
			int m = (int)Math.Ceiling(width) + 1;
			int w = Math.Min(Aup2Gpu.MaxSide, s.W + 2 * m), h = Math.Min(Aup2Gpu.MaxSide, s.H + 2 * m);
			var padded = c.Pass(FsPad, new Aup2Uniforms().V2("u_off", m, m), w, h)!;
			var soft = Blur(c, padded, width, width);
			gpu.Release(padded);
			double a = Finite(c.V("角度")) * Math.PI / 180;
			var t = c.Pass(FsBevel, new Aup2Uniforms().Tex("u_other", soft).V2("u_otherSize", soft.W, soft.H).F("u_m", m).F("u_w", width)
				.V2("u_light", -Math.Sin(a), -Math.Cos(a)).F("u_h", height));
			gpu.Release(soft);
			c.Replace(t);
		}

		private static void EdgeExtract(Aup2Context c) {
			if (!c.Pixels()) return;
			c.Filter(FsEdge, new Aup2Uniforms().F("u_k", Math.Max(0, Finite(c.V("強さ", 100))) / 100).F("u_thr", Finite(c.V("しきい値")) / 100)
				.V3("u_col", c.Color("色")).F("u_luma", c.Flag("輝度エッジを抽出") ? 1 : 0).F("u_alpha", c.Flag("透明度エッジを抽出") ? 1 : 0));
		}
	}
}
