using System;
using System.Collections.Generic;

namespace OpenTaiko.AviUtl2 {
	// The colour effects: tone, tint, keys, noise, small picture repairs and the masks that cut a picture.
	internal static class Aup2FiltersColor {
		public static void Register(Dictionary<string, Aup2FilterFn> map) {
			map["色調補正"] = Tone;
			map["グラデーション"] = Gradient;
			map["単色化"] = Monochrome;
			map["特定色域変換"] = ColorRangeChange;
			// only changes how the colour settings of the other effects are entered
			map["拡張色設定"] = c => { };
			map["クロマキー"] = ChromaKey;
			map["ルミナンスキー"] = LuminanceKey;
			map["カラーキー"] = ColorKey;
			map["ノイズ"] = Noise;
			// the noise alone, the size of the object's picture, kept in the temporary buffer
			map["ノイズ(仮想バッファ)"] = c => {
				if (c.Host == null || c.Gpu == null || !c.Pixels() || c.Surface == null) return;
				var own = c.Surface;
				var white = c.Gpu.Rent(own.W, own.H);
				c.Gpu.Clear(white, 1, 1, 1, 1);
				c.Surface = white;
				Noise(c);
				c.Host.CopyBuffer(c, "tempbuffer", "object");
				c.Gpu.Release(c.Surface);
				c.Surface = own;
			};
			map["モザイク"] = Mosaic;
			map["シャープ"] = Sharpen;
			map["ノイズ除去"] = Denoise;
			map["インターレース解除"] = Deinterlace;
			map["マスク"] = Mask;
			map["斜めクリッピング"] = SlantClip;
			map["扇クリッピング"] = FanClip;
		}

		private static bool HasColor(Aup2Context c, string key) => c.S(key).Trim().Length == 6;

		private static void Tone(Aup2Context c) {
			double bright = c.V("明るさ", 100), contrast = c.V("コントラスト", 100), hue = c.V("色相"), gain = c.V("輝度", 100), sat = c.V("彩度", 100);
			if (bright == 100 && contrast == 100 && hue == 0 && gain == 100 && sat == 100) return;
			double a = hue * Math.PI / 180;
			c.Filter(@"uniform float u_bright; uniform float u_contrast; uniform float u_gain; uniform float u_sat; uniform vec2 u_hue;
void main(){
    vec4 p = texture(u_src, v_uv);
    vec3 col = p.a > 0.0 ? p.rgb / p.a : vec3(0.0);
    float y = dot(col, vec3(0.299, 0.587, 0.114));
    vec2 ch = vec2(dot(col, vec3(-0.168736, -0.331264, 0.5)), dot(col, vec3(0.5, -0.418688, -0.081312)));
    y += u_bright;
    y = (y - 0.5) * u_contrast + 0.5;
    y *= u_gain;
    ch = vec2(ch.x * u_hue.x - ch.y * u_hue.y, ch.x * u_hue.y + ch.y * u_hue.x) * u_sat;
    col = vec3(y + 1.402 * ch.y, y - 0.344136 * ch.x - 0.714136 * ch.y, y + 1.772 * ch.x);
    o = vec4(clamp(col, 0.0, 1.0) * p.a, p.a);
}", new Aup2Uniforms().F("u_bright", (bright - 100) / 100).F("u_contrast", Math.Max(0, contrast) / 100).F("u_gain", Math.Max(0, gain) / 100)
				.F("u_sat", Math.Max(0, sat) / 100).V2("u_hue", Math.Cos(a), Math.Sin(a)));
		}

		private static void Gradient(Aup2Context c) {
			double strength = Math.Clamp(c.V("強さ", 100) / 100, 0, 1);
			bool hasStart = HasColor(c, "開始色"), hasEnd = HasColor(c, "終了色");
			if (strength <= 0 || (!hasStart && !hasEnd)) return;
			int shape = c.S("形状", "線形") switch { "円" => 1, "四角形" => 2, "凸形" => 3, _ => 0 };
			int mode = (int)(Aup2Evaluator.BlendOf(c.S("合成モード", "通常")) ?? Aup2Blend.Normal);
			double a = c.V("角度") * Math.PI / 180;
			var start = c.Color("開始色");
			var end = c.Color("終了色");
			c.Filter(@"uniform float u_strength; uniform vec2 u_centre; uniform vec2 u_dir; uniform float u_width; uniform int u_shape; uniform int u_mode;
uniform vec4 u_start; uniform vec4 u_end;
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
    vec4 p = texture(u_src, v_uv);
    vec3 col = p.a > 0.0 ? p.rgb / p.a : vec3(0.0);
    vec2 q = v_uv * u_size - u_size * 0.5 - u_centre;
    // along the gradient and across it
    float along = dot(q, u_dir), across = dot(q, vec2(u_dir.y, -u_dir.x));
    float t;
    if (u_shape == 0) t = along / u_width + 0.5;
    else if (u_shape == 1) t = length(q) * 2.0 / u_width;
    else if (u_shape == 2) t = max(abs(along), abs(across)) * 2.0 / u_width;
    else t = abs(along) * 2.0 / u_width;
    vec4 g = mix(u_start, u_end, clamp(t, 0.0, 1.0));
    vec3 r = clamp(blend(col, g.rgb), 0.0, 1.0);
    col = mix(col, r, g.a * u_strength);
    o = vec4(col * p.a, p.a);
}", new Aup2Uniforms().F("u_strength", strength).V2("u_centre", c.V("中心X"), c.V("中心Y")).V2("u_dir", -Math.Sin(a), Math.Cos(a))
				.F("u_width", Math.Max(0.001, Math.Abs(c.V("幅", 100)))).I("u_shape", shape).I("u_mode", mode)
				// an end without a colour leaves the picture as it is there
				.V4("u_start", hasStart ? start.X : end.X, hasStart ? start.Y : end.Y, hasStart ? start.Z : end.Z, hasStart ? 1 : 0)
				.V4("u_end", hasEnd ? end.X : start.X, hasEnd ? end.Y : start.Y, hasEnd ? end.Z : start.Z, hasEnd ? 1 : 0));
		}

		private static void Monochrome(Aup2Context c) {
			double strength = Math.Clamp(c.V("強さ", 100) / 100, 0, 1);
			if (strength <= 0) return;
			c.Filter(@"uniform float u_strength; uniform vec3 u_col; uniform int u_keep;
void main(){
    vec4 p = texture(u_src, v_uv);
    vec3 col = p.a > 0.0 ? p.rgb / p.a : vec3(0.0);
    vec3 r = u_col;
    if (u_keep == 1) {
        // the colour, moved up or down to the pixel's brightness
        float y = dot(col, vec3(0.299, 0.587, 0.114));
        float yc = dot(u_col, vec3(0.299, 0.587, 0.114));
        r = u_col + (y - yc);
    }
    col = mix(col, clamp(r, 0.0, 1.0), u_strength);
    o = vec4(col * p.a, p.a);
}", new Aup2Uniforms().F("u_strength", strength).V3("u_col", c.Color("色")).I("u_keep", c.Flag("輝度を保持する") ? 1 : 0));
		}

		private static void ColorRangeChange(Aup2Context c) {
			if (!HasColor(c, "変換前の色") || !HasColor(c, "変換後の色")) return;
			c.Filter(@"uniform vec3 u_from; uniform vec3 u_to; uniform float u_hueTol; uniform float u_satTol; uniform float u_edge;
const float PI = 3.14159265;
vec2 chroma(vec3 c){ return vec2(dot(c, vec3(-0.168736, -0.331264, 0.5)), dot(c, vec3(0.5, -0.418688, -0.081312))); }
// how much a pixel's chroma belongs to the key colour's range, 0..1
float keyWeight(vec2 ch, vec2 k, float extra){
    float ks = length(k), s = length(ch);
    if (ks < 1.0e-3) return 1.0 - smoothstep(u_satTol, u_satTol * (1.0 + 0.25 * u_edge) + 0.002, s);
    float dh = s > 1.0e-5 ? acos(clamp(dot(ch, k) / (s * ks), -1.0, 1.0)) : PI;
    float softH = u_hueTol * (0.25 * u_edge + extra) + 0.002;
    float wh = 1.0 - smoothstep(u_hueTol, u_hueTol + softH, dh);
    float need = min(u_satTol, ks * 0.5);
    float ws = smoothstep(need * (1.0 - 0.2 * u_edge - 0.5 * extra), need + 0.0005, s);
    return wh * ws;
}
void main(){
    vec4 p = texture(u_src, v_uv);
    vec3 col = p.a > 0.0 ? p.rgb / p.a : vec3(0.0);
    float y = dot(col, vec3(0.299, 0.587, 0.114));
    vec2 ch = chroma(col);
    vec2 kf = chroma(u_from), kt = chroma(u_to);
    float w = keyWeight(ch, kf, 0.0);
    // the pixel keeps how far it was from the old colour, around the new one
    ch += (kt - kf) * w;
    col = vec3(y + 1.402 * ch.y, y - 0.344136 * ch.x - 0.714136 * ch.y, y + 1.772 * ch.x);
    o = vec4(clamp(col, 0.0, 1.0) * p.a, p.a);
}", new Aup2Uniforms().V3("u_from", c.Color("変換前の色")).V3("u_to", c.Color("変換後の色"))
				.F("u_hueTol", Math.Clamp(c.V("色相範囲", 16), 0, 256) / 256 * Math.PI).F("u_satTol", Math.Max(0, c.V("彩度範囲", 96)) / 4096)
				.F("u_edge", Math.Clamp(c.V("境界補正", 1), 0, 5)));
		}

		private static void ChromaKey(Aup2Context c) {
			if (!HasColor(c, "基準色")) return;
			c.Filter(@"uniform vec3 u_key; uniform float u_hueTol; uniform float u_satTol; uniform float u_edge; uniform int u_spill; uniform int u_soft;
const float PI = 3.14159265;
vec2 chroma(vec3 c){ return vec2(dot(c, vec3(-0.168736, -0.331264, 0.5)), dot(c, vec3(0.5, -0.418688, -0.081312))); }
// how much a pixel's chroma belongs to the key colour's range, 0..1
float keyWeight(vec2 ch, vec2 k, float extra){
    float ks = length(k), s = length(ch);
    if (ks < 1.0e-3) return 1.0 - smoothstep(u_satTol, u_satTol * (1.0 + 0.25 * u_edge + extra) + 0.002, s);
    float dh = s > 1.0e-5 ? acos(clamp(dot(ch, k) / (s * ks), -1.0, 1.0)) : PI;
    float softH = u_hueTol * (0.25 * u_edge + extra) + 0.002;
    float wh = 1.0 - smoothstep(u_hueTol, u_hueTol + softH, dh);
    float need = min(u_satTol, ks * 0.5);
    float ws = smoothstep(need * (1.0 - 0.2 * u_edge - 0.5 * extra), need + 0.0005, s);
    return wh * ws;
}
void main(){
    vec4 p = texture(u_src, v_uv);
    vec3 col = p.a > 0.0 ? p.rgb / p.a : vec3(0.0);
    float y = dot(col, vec3(0.299, 0.587, 0.114));
    vec2 ch = chroma(col);
    vec2 k = chroma(u_key);
    float w = keyWeight(ch, k, u_soft == 1 ? 1.0 : 0.0);
    float ks = length(k);
    if (u_spill == 1 && ks > 1.0e-3) {
        // what is left loses the part of its chroma that points at the key colour
        vec2 kd = k / ks;
        float near = keyWeight(ch, k, 2.0);
        ch -= kd * max(dot(ch, kd), 0.0) * near;
        col = clamp(vec3(y + 1.402 * ch.y, y - 0.344136 * ch.x - 0.714136 * ch.y, y + 1.772 * ch.x), 0.0, 1.0);
    }
    float a = p.a * (1.0 - w);
    o = vec4(col * a, a);
}", new Aup2Uniforms().V3("u_key", c.Color("基準色"))
				.F("u_hueTol", Math.Clamp(c.V("色相範囲", 16), 0, 256) / 256 * Math.PI).F("u_satTol", Math.Max(0, c.V("彩度範囲", 96)) / 4096)
				.F("u_edge", Math.Clamp(c.V("境界補正", 1), 0, 5)).I("u_spill", c.Flag("色彩補正") ? 1 : 0).I("u_soft", c.Flag("透過補正") ? 1 : 0));
		}

		private static void LuminanceKey(Aup2Context c) {
			string name = c.S("モード", "暗い部分を透過");
			int mode = name.Contains("ぼかしなし") ? 3 : name.Contains("明暗") ? 2 : name.Contains("明るい") ? 1 : 0;
			c.Filter(@"uniform float u_ref; uniform float u_range; uniform int u_mode;
void main(){
    vec4 p = texture(u_src, v_uv);
    vec3 col = p.a > 0.0 ? p.rgb / p.a : vec3(0.0);
    float y = dot(col, vec3(0.299, 0.587, 0.114));
    float k;
    if (u_mode == 0) k = clamp((y - u_ref) / u_range + 0.5, 0.0, 1.0);
    else if (u_mode == 1) k = clamp((u_ref - y) / u_range + 0.5, 0.0, 1.0);
    else if (u_mode == 2) k = clamp(1.5 - abs(y - u_ref) / u_range, 0.0, 1.0);
    else k = abs(y - u_ref) <= u_range ? 1.0 : 0.0;
    float a = p.a * k;
    o = vec4(col * a, a);
}", new Aup2Uniforms().F("u_ref", c.V("基準輝度", 2048) / 4096).F("u_range", Math.Max(1, c.V("輝度範囲", 512)) / 4096).I("u_mode", mode));
		}

		private static void ColorKey(Aup2Context c) {
			if (!HasColor(c, "基準色")) return;
			c.Filter(@"uniform vec3 u_key; uniform float u_yTol; uniform float u_cTol; uniform float u_edge;
vec2 chroma(vec3 c){ return vec2(dot(c, vec3(-0.168736, -0.331264, 0.5)), dot(c, vec3(0.5, -0.418688, -0.081312))); }
void main(){
    vec4 p = texture(u_src, v_uv);
    vec3 col = p.a > 0.0 ? p.rgb / p.a : vec3(0.0);
    float dy = abs(dot(col - u_key, vec3(0.299, 0.587, 0.114)));
    float dc = length(chroma(col) - chroma(u_key));
    float soft = 0.0005 + u_edge * 0.008;
    float w = (1.0 - smoothstep(u_yTol, u_yTol + soft, dy)) * (1.0 - smoothstep(u_cTol, u_cTol + soft, dc));
    float a = p.a * (1.0 - w);
    o = vec4(col * a, a);
}", new Aup2Uniforms().V3("u_key", c.Color("基準色")).F("u_yTol", Math.Max(0, c.V("輝度範囲", 400)) / 4096)
				.F("u_cTol", Math.Max(0, c.V("色差範囲", 400)) / 4096).F("u_edge", Math.Clamp(c.V("境界補正", 1), 0, 5)));
		}

		private static void Noise(Aup2Context c) {
			double strength = Math.Max(0, c.V("強さ", 100)) / 100;
			if (strength <= 0) return;
			int type = c.S("ノイズの種類", "Type1") switch { "Type2" => 2, "Type3" => 3, "Type4" => 4, "Type5" => 5, "Type6" => 6, _ => 1 };
			double seed = c.V("シード");
			// a negative seed is shared by every object, the others are the object's own
			int seedInt = seed < 0 ? (int)Math.Max(-1e6, seed) : (int)Math.Min(1e6, seed) + (c.Object.Id & 0xffff) * 37 + 1;
			double t = c.Time;
			c.Filter(@"uniform float u_strength; uniform vec2 u_freq; uniform vec3 u_off; uniform float u_th; uniform int u_type; uniform int u_mode; uniform int u_seed;
float hash(vec3 cell){
    uvec3 q = uvec3(ivec3(cell));
    uint h = q.x * 1597334677u ^ q.y * 3812015801u ^ q.z * 2798796415u ^ uint(u_seed) * 747796405u;
    h ^= h >> 16; h *= 0x7feb352du; h ^= h >> 15; h *= 0x846ca68bu; h ^= h >> 16;
    return float(h >> 8) / 16777216.0;
}
float vnoise(vec3 p){
    vec3 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = mix(mix(hash(i), hash(i + vec3(1.0, 0.0, 0.0)), f.x), mix(hash(i + vec3(0.0, 1.0, 0.0)), hash(i + vec3(1.0, 1.0, 0.0)), f.x), f.y);
    float b = mix(mix(hash(i + vec3(0.0, 0.0, 1.0)), hash(i + vec3(1.0, 0.0, 1.0)), f.x), mix(hash(i + vec3(0.0, 1.0, 1.0)), hash(i + vec3(1.0, 1.0, 1.0)), f.x), f.y);
    return mix(a, b, f.z);
}
void main(){
    vec4 p = texture(u_src, v_uv);
    vec2 pos = v_uv * u_size;
    vec3 q = vec3(pos * u_freq + u_off.xy, u_off.z);
    float n;
    if (u_type == 2) n = vnoise(q);
    else if (u_type == 5) n = hash(floor(q));
    else if (u_type == 6) n = hash(vec3(floor(pos + u_off.xy / max(u_freq, vec2(1.0e-6))), floor(u_off.z * 30.0)));
    else {
        // four layers, each twice as fine and half as strong
        float sum = 0.0, amp = 0.5;
        for (int i = 0; i < 4; i++) {
            float v = vnoise(q);
            if (u_type == 3) v = abs(v * 2.0 - 1.0);
            else if (u_type == 4) v = 1.0 - abs(v * 2.0 - 1.0);
            sum += v * amp;
            q = q * 2.0 + vec3(17.0, 31.0, 5.0);
            amp *= 0.5;
        }
        n = sum / 0.9375;
    }
    n = u_th < 1.0 ? clamp((n - u_th) / (1.0 - u_th), 0.0, 1.0) : 0.0;
    float f = max(0.0, 1.0 + (n - 1.0) * u_strength);
    if (u_mode == 0) o = p * min(f, 1.0);
    else o = vec4(min(p.rgb * f, vec3(p.a)), p.a);
}", new Aup2Uniforms().F("u_strength", strength)
				.V2("u_freq", Math.Max(0, c.V("周期X", 1)) / 100, Math.Max(0, c.V("周期Y", 1)) / 100)
				.V3("u_off", -c.V("速度X") * t, -c.V("速度Y") * t, c.V("変化速度") * t)
				.F("u_th", Math.Clamp(c.V("しきい値") / 100, 0, 1)).I("u_type", type)
				.I("u_mode", c.S("合成モード", "アルファ値と乗算").Contains("輝度") ? 1 : 0).I("u_seed", seedInt));
		}

		private static void Mosaic(Aup2Context c) {
			double size = c.V("サイズ", 12);
			if (size <= 1) return;
			// each block shows the mean of a grid of points inside it
			c.Filter(@"uniform float u_block;
void main(){
    vec2 corner = floor(v_uv * u_size / u_block) * u_block;
    vec4 sum = vec4(0.0);
    for (int j = 0; j < 6; j++) {
        for (int i = 0; i < 6; i++) {
            vec2 q = corner + (vec2(float(i), float(j)) + 0.5) * u_block / 6.0;
            sum += texture(u_src, min(q, u_srcSize - 0.5) / u_srcSize);
        }
    }
    o = sum / 36.0;
}", new Aup2Uniforms().F("u_block", Math.Min(size, 4096)));
		}

		private static void Sharpen(Aup2Context c) {
			double strength = c.V("強さ", 50) / 100, range = c.V("範囲", 5);
			if (strength <= 0 || range <= 0) return;
			// the picture, plus its difference from a blurred copy of itself
			c.Filter(@"uniform float u_strength; uniform float u_range;
void main(){
    vec4 p = texture(u_src, v_uv);
    vec4 sum = vec4(0.0);
    float total = 0.0;
    for (int j = -2; j <= 2; j++) {
        for (int i = -2; i <= 2; i++) {
            vec2 d = vec2(float(i), float(j)) * 0.5;
            float w = exp(-dot(d, d) * 1.5);
            sum += texture(u_src, v_uv + d * u_range / u_srcSize) * w;
            total += w;
        }
    }
    vec4 blur = sum / total;
    vec3 rgb = p.rgb + (p.rgb - blur.rgb) * u_strength;
    o = vec4(clamp(rgb, vec3(0.0), vec3(p.a)), p.a);
}", new Aup2Uniforms().F("u_strength", Math.Min(strength, 10)).F("u_range", Math.Min(range, 64)));
		}

		private static void Denoise(Aup2Context c) {
			double strength = Math.Clamp(c.V("強さ", 100) / 100, 0, 1);
			int range = (int)Math.Clamp(Math.Round(c.V("範囲", 2)), 0, 3);
			if (strength <= 0 || range <= 0) return;
			// the mean of the neighbours that are close to the pixel's own colour
			c.Filter(@"uniform float u_strength; uniform int u_range; uniform float u_th;
void main(){
    vec4 p = texture(u_src, v_uv);
    vec4 sum = vec4(0.0);
    float total = 0.0;
    for (int j = -3; j <= 3; j++) {
        for (int i = -3; i <= 3; i++) {
            if (abs(i) > u_range || abs(j) > u_range) continue;
            vec4 s = texture(u_src, v_uv + vec2(float(i), float(j)) / u_srcSize);
            vec4 d = abs(s - p);
            float w = max(max(d.r, d.g), max(d.b, d.a)) <= u_th ? 1.0 : 0.0;
            sum += s * w;
            total += w;
        }
    }
    o = total > 0.0 ? mix(p, sum / total, u_strength) : p;
}", new Aup2Uniforms().F("u_strength", strength).I("u_range", range).F("u_th", Math.Max(0, c.V("しきい値", 20)) / 1024));
		}

		private static void Deinterlace(Aup2Context c) {
			string how = c.S("解除方法", "奇数");
			int mode = how.Contains("偶数") ? 1 : how.Contains("二重") ? 2 : 0;
			c.Filter(@"uniform int u_mode;
void main(){
    vec2 px = 1.0 / u_srcSize;
    vec4 p = texture(u_src, v_uv);
    vec4 up = texture(u_src, v_uv - vec2(0.0, px.y));
    vec4 down = texture(u_src, v_uv + vec2(0.0, px.y));
    int line = int(floor(v_uv.y * u_size.y));
    if (u_mode == 2) o = (up + p * 2.0 + down) * 0.25;
    else {
        // one field stays, the lines of the other come from their neighbours
        bool keep = (line % 2 == 0) == (u_mode == 0);
        o = keep ? p : (up + down) * 0.5;
    }
}", new Aup2Uniforms().I("u_mode", mode));
		}

		private static void Mask(Aup2Context c) {
			int kind = c.S("マスクの種類", "四角形") switch {
				"背景" => 0, "円" => 1, "四角形" => 2, "三角形" => 3, "五角形" => 5, "六角形" => 6, "星型" => 10, _ => -1,
			};
			// a mask read from a file or a scene is not drawn
			if (kind < 0) return;
			bool invert = c.Flag("マスクの反転");
			double size = Math.Max(0, c.V("サイズ", 100)), aspect = Math.Clamp(c.V("縦横比"), -100, 100);
			double w = size * (aspect > 0 ? 1 - aspect / 100 : 1), h = size * (aspect < 0 ? 1 + aspect / 100 : 1);
			if (kind == 0) {
				if (invert) c.Skip = true;
				return;
			}
			if (w <= 0 || h <= 0) {
				if (!invert) c.Skip = true;
				return;
			}
			double a = c.V("回転") * Math.PI / 180;
			c.Filter(@"uniform int u_kind; uniform vec2 u_centre; uniform vec2 u_half; uniform vec2 u_rot; uniform float u_blur; uniform int u_invert;
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
    vec4 src = texture(u_src, v_uv);
    vec2 p = v_uv * u_size - u_size * 0.5 - u_centre;
    // into the mask's own axes
    p = vec2(p.x * u_rot.x + p.y * u_rot.y, p.y * u_rot.x - p.x * u_rot.y);
    float m = min(u_half.x, u_half.y);
    float d;
    if (u_kind == 1) d = (length(p / u_half) - 1.0) * m;
    else if (u_kind == 2) {
        vec2 q = abs(p) - u_half;
        d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0);
    } else {
        vec2 q = p / u_half * m;
        if (u_kind == 10) d = sdStar(q, m);
        else if (u_kind == 3) d = sdPoly(vec2(q.x, q.y + m * 0.25), m, 3.0);
        else d = sdPoly(q, m, float(u_kind));
    }
    float k = clamp(0.5 - d / max(u_blur, 1.0), 0.0, 1.0);
    if (u_invert == 1) k = 1.0 - k;
    o = src * k;
}", new Aup2Uniforms().I("u_kind", kind).V2("u_centre", c.V("X"), c.V("Y")).V2("u_half", w / 2, h / 2)
				.V2("u_rot", Math.Cos(a), Math.Sin(a)).F("u_blur", Math.Max(0, c.V("ぼかし"))).I("u_invert", invert ? 1 : 0));
		}

		private static void SlantClip(Aup2Context c) {
			double a = c.V("角度") * Math.PI / 180;
			// the side the angle points at goes; with a width, a band around the line stays (or goes, when negative)
			c.Filter(@"uniform vec2 u_centre; uniform vec2 u_dir; uniform float u_blur; uniform float u_width;
void main(){
    vec4 src = texture(u_src, v_uv);
    float d = dot(v_uv * u_size - u_size * 0.5 - u_centre, u_dir);
    float soft = max(u_blur, 1.0e-3);
    float k;
    if (u_width > 0.0) k = clamp(0.5 - (abs(d) - u_width * 0.5) / soft, 0.0, 1.0);
    else if (u_width < 0.0) k = clamp(0.5 + (abs(d) + u_width * 0.5) / soft, 0.0, 1.0);
    else k = clamp(0.5 - d / soft, 0.0, 1.0);
    o = src * k;
}", new Aup2Uniforms().V2("u_centre", c.V("中心X"), c.V("中心Y")).V2("u_dir", -Math.Sin(a), Math.Cos(a))
				.F("u_blur", Math.Max(0, c.V("ぼかし", 1))).F("u_width", c.V("幅")));
		}

		private static void FanClip(Aup2Context c) {
			double start = c.V("基準角"), range = c.V("範囲角", 90);
			if (range < 0) { start += range; range = -range; }
			if (range <= 0) return;
			if (range >= 360) { c.Skip = true; return; }
			// the middle of the fan, clockwise from straight up
			double mid = (c.Flag("基準角を中央にする") ? start : start + range / 2) * Math.PI / 180;
			double half = range / 2 * Math.PI / 180;
			c.Filter(@"uniform vec2 u_centre; uniform vec2 u_axis; uniform vec2 u_edge; uniform float u_half; uniform float u_blur;
void main(){
    vec4 src = texture(u_src, v_uv);
    vec2 p = v_uv * u_size - u_size * 0.5 - u_centre;
    // x across the fan's middle line, y along it
    vec2 q = vec2(abs(p.x * u_axis.y - p.y * u_axis.x), p.x * u_axis.x + p.y * u_axis.y);
    float along = dot(q, u_edge);
    float d = along > 0.0 ? abs(q.x * u_edge.y - q.y * u_edge.x) : length(q);
    if (atan(q.x, q.y) < u_half) d = -d;
    // inside the fan nothing is left
    o = src * clamp(0.5 + d / max(u_blur, 1.0e-3), 0.0, 1.0);
}", new Aup2Uniforms().V2("u_centre", c.V("中心X"), c.V("中心Y")).V2("u_axis", Math.Sin(mid), -Math.Cos(mid))
				.V2("u_edge", Math.Sin(half), Math.Cos(half)).F("u_half", half).F("u_blur", Math.Max(0, c.V("ぼかし", 1))));
		}
	}
}
