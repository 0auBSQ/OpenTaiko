using System;
using System.Collections.Generic;
using System.Numerics;

namespace OpenTaiko.AviUtl2 {
	// Draws a scene change: `old` is the picture it starts from, `now` the one it ends on, p runs 0..1.
	// The settings are read from c (the scene change's own effect). Returns a new frame-sized surface.
	internal delegate Aup2Surface? Aup2ChangeFn(Aup2Context c, Aup2Surface old, Aup2Surface now, double p);

	// The scene changes that are effects of their own, by effect name.
	internal static class Aup2SceneChanges {
		public static readonly Dictionary<string, Aup2ChangeFn> Kinds = Build();

		private static Dictionary<string, Aup2ChangeFn> Build() {
			var map = new Dictionary<string, Aup2ChangeFn>();
			map["ドア(シーンチェンジ)"] = Door;
			map["ページめくり(シーンチェンジ)"] = PageTurn;
			map["リール回転(シーンチェンジ)"] = Reel;
			map["レンズブラー(シーンチェンジ)"] = LensBlur;
			map["図形で隠す(シーンチェンジ)"] = (c, old, now, p) => ShapeHide(c, old, now, p, false);
			map["図形で隠す(放射)(シーンチェンジ)"] = (c, old, now, p) => ShapeHide(c, old, now, p, true);
			map["図形ワイプ(シーンチェンジ)"] = ShapeWipe;
			map["発光(シーンチェンジ)"] = Glow;
			map["砕け散る(シーンチェンジ)"] = Shatter;
			map["起き上がる(シーンチェンジ)"] = Rise;
			return map;
		}

		#region Helpers
		private static readonly Vector4 WholeUv = new(0, 0, 1, 1);

		// pixels from the frame's centre (x right, y down, z away) to clip space, the eye 1024 back
		private static Matrix4x4 ToClip(int w, int h) {
			const float f = 1024, near = 1, far = 100000;
			return Matrix4x4.CreateTranslation(0, 0, 1024) * new Matrix4x4(
				2 * f / w, 0, 0, 0,
				0, 2 * f / h, 0, 0,
				0, 0, (far + near) / (far - near), 1,
				0, 0, -2 * far * near / (far - near), 0);
		}

		// the unit square as the rectangle from (x, y), w wide and h high
		private static Matrix4x4 Rect(double x, double y, double w, double h)
			=> Matrix4x4.CreateScale((float)w, (float)h, 1) * Matrix4x4.CreateTranslation((float)x, (float)y, 0);

		private static double Smooth(double p) => p * p * (3 - 2 * p);

		private static double Finite(double v, double fallback = 0) => double.IsFinite(v) ? v : fallback;
		#endregion

		#region Door, rise
		// the old picture swings open about its left edge, the new one behind it
		private static Aup2Surface? Door(Aup2Context c, Aup2Surface old, Aup2Surface now, double p) {
			var g = c.Gpu;
			if (g == null) return null;
			int w = now.W, h = now.H;
			var t = g.Copy(now);
			double angle = 90 * Smooth(p);
			if (!c.Flag("手前に開く")) angle = -angle;
			var m = Rect(0, -h / 2.0, w, h) * Aup2Evaluator.Turn(0, angle, 0) * Matrix4x4.CreateTranslation(-w / 2f, 0, 0) * ToClip(w, h);
			float shade = (float)(1 - 0.5 * p);
			g.Quad(t, old.Tex, m, WholeUv, 1, Aup2Blend.Normal, false, null, new Vector3(shade));
			return t;
		}

		// the new picture swings up about its bottom edge from lying flat, over the old one
		private static Aup2Surface? Rise(Aup2Context c, Aup2Surface old, Aup2Surface now, double p) {
			var g = c.Gpu;
			if (g == null) return null;
			int w = now.W, h = now.H;
			var t = g.Copy(old);
			double spring = Math.Clamp(Finite(c.V("勢い", 100)) / 100, 0, 4);
			// past upright and back, less each time, upright at the end
			double angle = -90 * Math.Cos(Math.PI / 2 * (1 + 2 * spring) * p) * Math.Exp(-3 * spring * p) * (1 - p);
			var m = Rect(-w / 2.0, -h, w, h) * Aup2Evaluator.Turn(angle, 0, 0) * Matrix4x4.CreateTranslation(0, h / 2f, 0) * ToClip(w, h);
			float shade = (float)(1 - 0.5 * Math.Min(1, Math.Abs(angle) / 90));
			g.Quad(t, now.Tex, m, WholeUv, 1, Aup2Blend.Normal, false, null, new Vector3(shade));
			return t;
		}
		#endregion

		#region Page turn
		// u_src: the new picture. The old one rolls off to the left around a cylinder at u_c of radius u_r.
		private const string PageFragment = @"uniform sampler2D u_old; uniform float u_c; uniform float u_r; uniform float u_shade;
const float PI = 3.14159265;
vec4 backOf(float x, float light){
    // the back of the sheet: the picture seen through paper
    vec4 b = texture(u_old, vec2(x / u_size.x, v_uv.y));
    return vec4(mix(b.rgb, vec3(b.a), 0.55) * light, b.a);
}
void main(){
    vec2 pos = v_uv * u_size;
    vec4 nw = texture(u_src, v_uv);
    float d = pos.x - u_c;
    if (d > u_r) {
        // the roll's shadow on the new picture
        float sh = exp(-(d - u_r) / (u_r * 0.6 + 1.0)) * 0.5 * u_shade;
        o = vec4(nw.rgb * (1.0 - sh), nw.a);
        return;
    }
    if (d > 0.0) {
        float th = asin(clamp(d / u_r, 0.0, 1.0));
        float xf = u_c + u_r * th, xb = u_c + u_r * (PI - th);
        vec4 under = nw;
        if (xf <= u_size.x) {
            vec4 f = texture(u_old, vec2(xf / u_size.x, v_uv.y));
            under = vec4(f.rgb * (1.0 - 0.6 * u_shade * (1.0 - cos(th))), f.a);
        }
        if (xb <= u_size.x) {
            vec4 b = backOf(xb, 1.0 - 0.45 * u_shade * cos(th));
            under = b + under * (1.0 - b.a);
        }
        o = under;
        return;
    }
    // still flat, with what has come over the top of the roll lying on it
    vec4 under = texture(u_old, v_uv);
    float xo = u_c + PI * u_r - d;
    if (xo <= u_size.x) {
        vec4 b = backOf(xo, 1.0 - 0.25 * u_shade);
        under = b + under * (1.0 - b.a);
    } else {
        float sh = exp(-(xo - u_size.x) / (u_r * 0.6 + 1.0)) * 0.4 * u_shade;
        under.rgb *= 1.0 - sh;
    }
    o = under;
}";

		private static Aup2Surface? PageTurn(Aup2Context c, Aup2Surface old, Aup2Surface now, double p) {
			var g = c.Gpu;
			if (g == null) return null;
			int w = now.W, h = now.H;
			double shade = Math.Clamp(Finite(c.V("陰影", 100)) / 100, 0, 2);
			double r = Math.Max(2, w * 0.08 * Math.Max(0, Finite(c.V("奥行き", 100))) / 100);
			// from the right edge until the whole sheet has gone round
			double axis = w + (-(1 + Math.PI) * r - w) * p;
			var t = g.Rent(w, h);
			g.Pass(t, PageFragment, now, new Aup2Uniforms().Tex("u_old", old).F("u_c", axis).F("u_r", r).F("u_shade", shade));
			return t;
		}
		#endregion

		#region Reel
		// u_src: the new picture. A strip of pictures (old, new, old .. new) runs past; u_q: how many have gone.
		private const string ReelFragment = @"uniform sampler2D u_old; uniform float u_q; uniform float u_n; uniform float u_len; uniform int u_h; uniform float u_dir;
void main(){
    float base = u_h == 1 ? v_uv.x : v_uv.y;
    if (u_dir < 0.0) base = 1.0 - base;
    vec4 sum = vec4(0.0);
    for (int i = 0; i < 24; i++) {
        float t = base + u_q + u_len * (float(i) / 23.0 - 0.5);
        t = clamp(t, 0.0, u_n + 0.9999);
        float k = floor(t), f = fract(t);
        if (u_dir < 0.0) f = 1.0 - f;
        vec2 uv = u_h == 1 ? vec2(f, v_uv.y) : vec2(v_uv.x, f);
        if (mod(k, 2.0) < 0.5) sum += texture(u_old, uv);
        else sum += texture(u_src, uv);
    }
    o = sum / 24.0;
}";

		private static Aup2Surface? Reel(Aup2Context c, Aup2Surface old, Aup2Surface now, double p) {
			var g = c.Gpu;
			if (g == null) return null;
			double turns = Finite(c.V("回転数")), blur = Math.Max(0, Finite(c.V("ブラー", 100))) / 100;
			// an odd count of steps, so the strip stops on the new picture
			double n = 1 + 2 * Math.Min(50, Math.Round(Math.Abs(turns)));
			double q = n * Smooth(p);
			double frames = Math.Max(1, c.Duration * c.Scene.Rate);
			// the way travelled during one frame
			double len = n * 6 * p * (1 - p) / frames * blur;
			var t = g.Rent(now.W, now.H);
			g.Pass(t, ReelFragment, now, new Aup2Uniforms().Tex("u_old", old).F("u_q", q).F("u_n", n).F("u_len", len)
				.I("u_h", c.Flag("横回転") ? 1 : 0).F("u_dir", turns < 0 ? -1 : 1));
			return t;
		}
		#endregion

		#region Lens blur, glow
		// u_src: the new picture; a disc of samples over the mix of the two
		private const string LensFragment = @"uniform sampler2D u_old; uniform float u_mix; uniform float u_r;
void main(){
    vec4 sum = vec4(0.0);
    for (int i = 0; i < 48; i++) {
        float fi = float(i);
        float rad = sqrt((fi + 0.5) / 48.0) * u_r;
        float an = fi * 2.3999632;
        vec2 uv = v_uv + vec2(cos(an), sin(an)) * rad / u_size;
        sum += mix(texture(u_old, uv), texture(u_src, uv), u_mix);
    }
    o = sum / 48.0;
}";
		// a second, smaller disc that fills the gaps between the first one's samples
		private const string LensSmoothFragment = @"uniform float u_r;
void main(){
    vec4 sum = vec4(0.0);
    for (int i = 0; i < 24; i++) {
        float fi = float(i);
        float rad = sqrt((fi + 0.5) / 24.0) * u_r;
        float an = fi * 2.3999632 + 1.0;
        sum += texture(u_src, v_uv + vec2(cos(an), sin(an)) * rad / u_size);
    }
    o = sum / 24.0;
}";

		private static Aup2Surface? LensBlur(Aup2Context c, Aup2Surface old, Aup2Surface now, double p) {
			var g = c.Gpu;
			if (g == null) return null;
			int w = now.W, h = now.H;
			double radius = Math.Sin(Math.PI * p) * h * 0.05;
			double mix = Math.Clamp((p - 0.3) / 0.4, 0, 1);
			var first = g.Rent(w, h);
			g.Pass(first, LensFragment, now, new Aup2Uniforms().Tex("u_old", old).F("u_mix", Smooth(mix)).F("u_r", radius));
			if (radius < 2) return first;
			var t = g.Rent(w, h);
			g.Pass(t, LensSmoothFragment, first, new Aup2Uniforms().F("u_r", radius * 0.3));
			g.Release(first);
			return t;
		}

		// u_src: the new picture. Both brighten to white in the middle of the change.
		private const string GlowFragment = @"uniform sampler2D u_old; uniform float u_mix; uniform float u_f;
void main(){
    vec4 v = mix(texture(u_old, v_uv), texture(u_src, v_uv), u_mix);
    float a = max(v.a, clamp(u_f * u_f, 0.0, 1.0));
    vec3 rgb = v.rgb * (1.0 + 2.0 * u_f) + vec3(u_f * u_f);
    o = vec4(min(rgb, vec3(a)), a);
}";

		private static Aup2Surface? Glow(Aup2Context c, Aup2Surface old, Aup2Surface now, double p) {
			var g = c.Gpu;
			if (g == null) return null;
			double light = Math.Max(0, Finite(c.V("明るさ", 100))) / 100;
			double mix = Smooth(Math.Clamp((p - 0.35) / 0.3, 0, 1));
			var t = g.Rent(now.W, now.H);
			g.Pass(t, GlowFragment, now, new Aup2Uniforms().Tex("u_old", old).F("u_mix", mix).F("u_f", Math.Sin(Math.PI * p) * light));
			return t;
		}
		#endregion

		#region Shapes
		// the shader's kind and how large the shape must be, in cells, to cover its own cell
		private static readonly Dictionary<string, (int kind, double cover)> ShapeKinds = new() {
			["円"] = (1, 0.7072), ["四角形"] = (2, 1.0), ["三角形"] = (3, 1.4143), ["五角形"] = (5, 0.8741), ["六角形"] = (6, 0.8166), ["星型"] = (10, 1.96),
		};

		// A grid of shapes over u_src, each grown by how far the change is at its cell. The shapes show
		// u_top, or the colour u_col. u_t: 0..1; u_spread: how much later the last cell starts than the first.
		private const string ShapesFragment = @"uniform sampler2D u_top; uniform int u_fill; uniform vec3 u_col; uniform int u_kind; uniform float u_cell; uniform float u_scale;
uniform float u_rot; uniform float u_t; uniform float u_spread; uniform vec2 u_dir; uniform int u_radial; uniform int u_shrink;
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
float shape(vec2 q, float r){
    if (u_kind == 1) return length(q) - r;
    if (u_kind == 2) return max(abs(q.x), abs(q.y)) - r * 0.70710678;
    if (u_kind == 10) return sdStar(q, r);
    return sdPoly(q, r, float(u_kind));
}
void main(){
    vec2 pos = (v_uv - 0.5) * u_size;
    vec2 id = floor(pos / u_cell + 0.5);
    float cs = cos(u_rot), sn = sin(u_rot);
    float ext = max(abs(u_dir.x) * u_size.x * 0.5 + abs(u_dir.y) * u_size.y * 0.5, 1.0);
    float m = 0.0;
    for (int j = -2; j <= 2; j++) {
        for (int i = -2; i <= 2; i++) {
            vec2 cc = (id + vec2(float(i), float(j))) * u_cell;
            float d = u_radial == 1 ? length(cc) / (0.5 * length(u_size)) : 0.5 + 0.5 * dot(cc, u_dir) / ext;
            d = clamp(d, 0.0, 1.0);
            float g = clamp(u_t * (1.0 + u_spread) - d * u_spread, 0.0, 1.0);
            if (u_shrink == 1) g = 1.0 - g;
            float r = u_cell * u_scale * g;
            if (r <= 0.0) continue;
            vec2 q = pos - cc;
            q = vec2(q.x * cs + q.y * sn, -q.x * sn + q.y * cs);
            m = max(m, clamp(0.5 - shape(q, r), 0.0, 1.0));
        }
    }
    vec4 under = texture(u_src, v_uv);
    vec4 top = u_fill == 1 ? vec4(u_col, 1.0) : texture(u_top, v_uv);
    o = mix(under, top, m);
}";

		// the settings the shape changes share
		private static Aup2Uniforms ShapeUniforms(Aup2Context c, double cell) {
			if (!ShapeKinds.TryGetValue(c.S("図形名", "円"), out var kind)) kind = ShapeKinds["円"];
			double scale = Math.Max(0, Finite(c.V("拡大率", 100), 100)) / 100;
			double turn = (Finite(c.V("角度")) + Finite(c.V("回転速度")) * c.Time) * Math.PI / 180;
			return new Aup2Uniforms().I("u_kind", kind.kind).F("u_cell", Math.Max(4, cell)).F("u_scale", kind.cover * 1.03 * scale).F("u_rot", turn);
		}

		// shapes of one colour grow until they hide the old picture, then shrink away from the new one
		private static Aup2Surface? ShapeHide(Aup2Context c, Aup2Surface old, Aup2Surface now, double p, bool radial) {
			var g = c.Gpu;
			if (g == null) return null;
			int w = now.W, h = now.H;
			double count = Math.Clamp(Finite(c.V("数", 8), 8), 1, 200);
			double dir = (radial ? 0 : Finite(c.V("方向"))) + Finite(c.V("エッジ角度"));
			double spread = Math.Clamp(Finite(c.V("エッジ幅", 100), 100) / 100, 0, 8);
			if (radial) spread *= 100 / Math.Clamp(Finite(c.V("速度", 100), 100), 5, 2000);
			var col = c.Color("色");
			bool second = p >= 0.5;
			var u = ShapeUniforms(c, w / count).Tex("u_top", now).I("u_fill", 1).V3("u_col", col)
				.F("u_t", second ? p * 2 - 1 : p * 2).F("u_spread", spread).V2("u_dir", Math.Cos(dir * Math.PI / 180), Math.Sin(dir * Math.PI / 180))
				.I("u_radial", radial ? 1 : 0).I("u_shrink", second ? 1 : 0);
			var t = g.Rent(w, h);
			g.Pass(t, ShapesFragment, second ? now : old, u);
			return t;
		}

		// windows in the shape of the figure open onto the new picture, later along the direction
		private static Aup2Surface? ShapeWipe(Aup2Context c, Aup2Surface old, Aup2Surface now, double p) {
			var g = c.Gpu;
			if (g == null) return null;
			double size = Math.Max(4, Finite(c.V("サイズ", 100), 100));
			double dir = Finite(c.V("方向")) * Math.PI / 180;
			// the delay between the first and the last window, in seconds
			double held = Math.Clamp(Math.Abs(Finite(c.V("ディレイ時間"))) / Math.Max(1e-6, c.Duration), 0, 0.95);
			var u = ShapeUniforms(c, size).Tex("u_top", now).I("u_fill", 0).V3("u_col", Vector3.One)
				.F("u_t", p).F("u_spread", held / (1 - held)).V2("u_dir", Math.Cos(dir), Math.Sin(dir)).I("u_radial", 0).I("u_shrink", 0);
			var t = g.Rent(now.W, now.H);
			g.Pass(t, ShapesFragment, old, u);
			return t;
		}
		#endregion

		#region Shatter
		private const int MaxPieces = 400;

		// the old picture breaks into tiles that fly off and fall, over the new one
		private static Aup2Surface? Shatter(Aup2Context c, Aup2Surface old, Aup2Surface now, double p) {
			var g = c.Gpu;
			if (g == null) return null;
			int w = now.W, h = now.H;
			var t = g.Copy(now);
			if (p >= 1) return t;
			double size = Math.Max(Math.Max(4, Finite(c.V("サイズ", 50), 50)), Math.Sqrt((double)w * h / MaxPieces));
			int cols = Math.Max(1, (int)Math.Ceiling(w / size)), rows = Math.Max(1, (int)Math.Ceiling(h / size));
			while (cols * rows > MaxPieces) { if (cols >= rows) cols--; else rows--; }
			double cx = w / 2.0 + Finite(c.V("中心X")), cy = h / 2.0 + Finite(c.V("中心Y")), cz = Finite(c.V("中心Z"));
			double speed = Finite(c.V("速度", 100)), gravity = Finite(c.V("重力", 100)), lag = Finite(c.V("時間差", 100)) / 100, reach = Finite(c.V("距離影響", 100)) / 100;
			double spin = Finite(c.V("ランダム回転", 100)) / 100, stray = Finite(c.V("ランダム方向", 100)) / 100;
			double time = p * c.Duration * Finite(c.V("再生速度", 1), 1);
			double far = Math.Max(1, Math.Sqrt((double)w * w + (double)h * h) / 2);
			double pw = (double)w / cols, ph = (double)h / rows;
			int n = cols * rows;
			var matrices = new Matrix4x4[n];
			var depth = new float[n];
			var order = new int[n];
			var clip = ToClip(w, h);
			for (int j = 0; j < rows; j++) {
				for (int i = 0; i < cols; i++) {
					int idx = j * cols + i;
					double px = (i + 0.5) * pw, py = (j + 0.5) * ph;
					double dx = px - cx, dy = py - cy;
					double dist = Math.Sqrt(dx * dx + dy * dy);
					double x = 0, y = 0, z = 0, rx = 0, ry = 0, rz = 0;
					// pieces near the centre leave first
					double local = time - dist / far * lag;
					if (local > 0) {
						double a = Math.Atan2(dy, dx) + (c.Random("dir", idx) - 0.5) * Math.PI * stray;
						double v = speed * 4 * (1 + (1 - dist / far) * reach) * (0.6 + 0.8 * c.Random("vel", idx));
						x = Math.Cos(a) * v * local;
						y = Math.Sin(a) * v * local + gravity * 5 * local * local;
						// a centre in front of the picture pushes the pieces away from the eye
						z = ((c.Random("z", idx) - 0.5) * 0.5 - cz / far) * v * local;
						rx = (c.Random("rx", idx) - 0.5) * 720 * spin * local;
						ry = (c.Random("ry", idx) - 0.5) * 720 * spin * local;
						rz = (c.Random("rz", idx) - 0.5) * 720 * spin * local;
					}
					matrices[idx] = Rect(-pw / 2, -ph / 2, pw, ph) * Aup2Evaluator.Turn(rx, ry, rz)
						* Matrix4x4.CreateTranslation((float)(px - w / 2.0 + x), (float)(py - h / 2.0 + y), (float)z) * clip;
					depth[idx] = (float)z;
					order[idx] = idx;
				}
			}
			// the far pieces first
			Array.Sort(order, (a, b) => depth[a] != depth[b] ? depth[b].CompareTo(depth[a]) : a.CompareTo(b));
			// what is still in sight fades at the very end
			double opacity = Math.Clamp((1 - p) / 0.15, 0, 1);
			foreach (int idx in order) {
				// behind the eye
				if (depth[idx] <= -1000) continue;
				var uv = new Vector4((float)(idx % cols) / cols, (float)(idx / cols) / rows, 1f / cols, 1f / rows);
				g.Quad(t, old.Tex, matrices[idx], uv, opacity, Aup2Blend.Normal, false);
			}
			return t;
		}
		#endregion
	}
}
