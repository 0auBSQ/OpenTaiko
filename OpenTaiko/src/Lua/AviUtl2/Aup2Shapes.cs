using System;
using System.Collections.Generic;

namespace OpenTaiko.AviUtl2 {
	// The shape object (図形): a background, a circle, a rectangle, regular polygons and a star, filled or
	// as a line, drawn by a shader from the shape's distance function.
	internal static class Aup2Shapes {
		private static readonly Dictionary<string, int> Kinds = new() {
			["背景"] = 0, ["円"] = 1, ["四角形"] = 2, ["三角形"] = 3, ["五角形"] = 5, ["六角形"] = 6, ["星型"] = 10,
		};

		public static bool Knows(string kind) => Kinds.ContainsKey(kind);

		// fills the item from the shape's settings; false when the kind is not one this player draws
		public static bool Read(Aup2Context c) {
			string kind = c.S("図形の種類", c.S("図形", "四角形"));
			if (!Kinds.ContainsKey(kind)) return false;
			var item = c.Item;
			item.Kind = Aup2ItemKind.Shape;
			item.ShapeKind = kind;
			if (kind == "背景") {
				item.ShapeWidth = c.FrameW; item.ShapeHeight = c.FrameH;
			} else {
				double size = Math.Max(0, c.V("サイズ", 100)), aspect = c.V("縦横比");
				item.ShapeWidth = size * (aspect > 0 ? 1 - aspect / 100 : 1);
				item.ShapeHeight = size * (aspect < 0 ? 1 + aspect / 100 : 1);
			}
			item.ShapeLine = c.V("ライン幅", 4000);
			var col = c.Color("色");
			item.R = (int)Math.Round(col.X * 255); item.G = (int)Math.Round(col.Y * 255); item.B = (int)Math.Round(col.Z * 255);
			item.Path = c.V("角を丸くする").ToString(System.Globalization.CultureInfo.InvariantCulture);   // the corner radius rides along
			return true;
		}

		// u_kind as in Kinds; u_line: the line's width in pixels (large = filled); u_round: corner radius
		public const string Fragment = @"uniform int u_kind; uniform float u_line; uniform float u_round; uniform vec3 u_col;
const float PI = 3.14159265;
float sdPoly(vec2 p, float r, float n){
    // regular polygon, one corner up
    float an = PI / n;
    float a = atan(p.x, -p.y);
    float seg = mod(a + an, 2.0 * an) - an;
    return length(p) * cos(seg) - r * cos(an);
}
float sdStar(vec2 p, float r){
    // five points, one up
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
    vec2 p = (v_uv - 0.5) * u_size;
    vec2 half_ = u_size * 0.5;
    float d;
    if (u_kind == 0) d = -1.0e6;
    else if (u_kind == 1) {
        // an ellipse: the distance scaled to the smaller radius
        float m = min(half_.x, half_.y);
        d = (length(p / half_) - 1.0) * m;
    } else if (u_kind == 2) {
        float r = min(u_round, min(half_.x, half_.y));
        vec2 q = abs(p) - half_ + r;
        d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
    } else {
        // the polygons and the star are drawn in a square, then stretched
        float m = min(half_.x, half_.y);
        vec2 q = p / half_ * m;
        d = u_kind == 10 ? sdStar(q, m) : sdPoly(q, m, float(u_kind));
        if (u_kind == 3) d = sdPoly(vec2(q.x, q.y + m * 0.25), m, 3.0);
    }
    float a = clamp(0.5 - d, 0.0, 1.0);
    if (u_kind != 0) a *= clamp(0.5 + d + u_line, 0.0, 1.0);
    o = vec4(u_col * a, a);
}";

		public static Aup2Uniforms Uniforms(Aup2Item item) {
			double.TryParse(item.Path, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double round);
			return new Aup2Uniforms().I("u_kind", Kinds[item.ShapeKind]).F("u_line", item.ShapeLine).F("u_round", round)
				.V3("u_col", item.R / 255.0, item.G / 255.0, item.B / 255.0);
		}
	}
}
