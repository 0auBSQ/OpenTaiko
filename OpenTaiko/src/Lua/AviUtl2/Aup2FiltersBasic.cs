using System;
using System.Collections.Generic;

namespace OpenTaiko.AviUtl2 {
	// The effects that place an object: where, how big, how turned, how see-through, and what part of it.
	internal static class Aup2FiltersBasic {
		public static void Register(Dictionary<string, Aup2FilterFn> map) {
			map["標準描画"] = StandardDraw;
			map["クリッピング"] = Clipping;
			map["反転"] = Flip;
			map["透明度"] = c => c.Item.Opacity *= 1 - Math.Clamp(c.V("透明度") / 100, 0, 1);
			map["座標"] = c => { c.Item.X += c.V("X"); c.Item.Y += c.V("Y"); c.Item.Z += c.V("Z"); };
			map["拡大率"] = Scale;
			map["リサイズ"] = Resize;
			map["回転"] = c => { c.Item.RotX += c.V("X"); c.Item.RotY += c.V("Y"); c.Item.RotZ += c.V("Z"); };
			map["フェード"] = Fade;
			map["領域拡張"] = Extend;
			map["オフスクリーン描画"] = c => c.Pixels();
			map["カメラ制御オプション"] = c => {
				if (c.S("オブジェクトの向き", "標準") != "標準") c.Item.Billboard = true;
				if (c.Flag("裏面を表示しない")) c.Item.CullBack = true;
			};
			map["材質設定(カメラ制御)"] = c => { };
			// the picture goes to the temporary buffer instead of the frame
			map["仮想バッファ出力"] = c => {
				if (c.Host != null && c.Pixels()) c.Host.CopyBuffer(c, "tempbuffer", "object");
				c.Skip = true;
			};
		}

		private static void StandardDraw(Aup2Context c) {
			var item = c.Item;
			item.X += c.V("X"); item.Y += c.V("Y"); item.Z += c.V("Z");
			item.PivotX += c.V("中心X"); item.PivotY += c.V("中心Y"); item.PivotZ += c.V("中心Z");
			double scale = c.V("拡大率", 100) / 100, aspect = c.V("縦横比");
			item.ScaleX *= scale * (aspect > 0 ? 1 - aspect / 100 : 1);
			item.ScaleY *= scale * (aspect < 0 ? 1 + aspect / 100 : 1);
			item.RotX += c.V("X軸回転"); item.RotY += c.V("Y軸回転"); item.RotZ += c.V("Z軸回転");
			item.Opacity *= 1 - Math.Clamp(c.V("透明度") / 100, 0, 1);
			item.Blend = Aup2Evaluator.BlendOf(c.S("合成モード", "通常")) ?? Aup2Blend.Normal;
		}

		private static void Clipping(Aup2Context c) {
			double top = Math.Max(0, c.V("上")), bottom = Math.Max(0, c.V("下")), left = Math.Max(0, c.V("左")), right = Math.Max(0, c.V("右"));
			bool recentre = c.Flag("中心の位置を変更");
			if (c.Surface == null) {
				// nothing drew yet: the crop waits for whoever reads the picture
				c.Item.CropTop += top; c.Item.CropBottom += bottom; c.Item.CropLeft += left; c.Item.CropRight += right;
				if (recentre) c.Item.Recentre = true;
				return;
			}
			var s = c.Surface;
			int w = (int)Math.Round(s.W - left - right), h = (int)Math.Round(s.H - top - bottom);
			if (w <= 0 || h <= 0) { c.Skip = true; return; }
			var t = c.Pass("uniform vec2 u_off; void main(){ o = texture(u_src, (v_uv * u_size + u_off) / u_srcSize); }",
				new Aup2Uniforms().V2("u_off", left, top), w, h);
			c.Replace(t, recentre ? 0 : (left - right) / 2, recentre ? 0 : (top - bottom) / 2);
		}

		private static void Flip(Aup2Context c) {
			if (c.Flag("上下反転")) c.Item.FlipY = !c.Item.FlipY;
			if (c.Flag("左右反転")) c.Item.FlipX = !c.Item.FlipX;
			bool luma = c.Flag("輝度反転"), hue = c.Flag("色相反転"), alpha = c.Flag("透明度反転");
			if (!luma && !hue && !alpha) return;
			c.Filter(@"uniform int u_luma; uniform int u_hue; uniform int u_alpha;
void main(){
    vec4 p = texture(u_src, v_uv);
    vec3 col = p.a > 0.0 ? p.rgb / p.a : vec3(0.0);
    if (u_luma == 1 && u_hue == 1) col = 1.0 - col;
    else if (u_luma == 1) { float y = dot(col, vec3(0.299, 0.587, 0.114)); col = clamp(col + (1.0 - 2.0 * y), 0.0, 1.0); }
    else if (u_hue == 1) { float y = dot(col, vec3(0.299, 0.587, 0.114)); col = clamp(2.0 * y - col, 0.0, 1.0); }
    // the opacity turned over: what was empty becomes solid white, what was drawn keeps its light
    if (u_alpha == 1) o = vec4(col * p.a + (1.0 - p.a), 1.0 - p.a);
    else o = vec4(col * p.a, p.a);
}", new Aup2Uniforms().I("u_luma", luma ? 1 : 0).I("u_hue", hue ? 1 : 0).I("u_alpha", alpha ? 1 : 0));
		}

		private static void Scale(Aup2Context c) {
			double all = c.V("拡大率", 100) / 100;
			c.Item.ScaleX *= all * c.V("X", 100) / 100;
			c.Item.ScaleY *= all * c.V("Y", 100) / 100;
		}

		private static void Resize(Aup2Context c) {
			double all = c.V("拡大率", 100) / 100, x = c.V("X", 100), y = c.V("Y", 100);
			if (c.Flag("ピクセル数でサイズ指定") || c.Flag("ドット数でサイズ指定")) {
				// X and Y are the size wanted, in pixels
				if (!c.Pixels()) return;
				c.Item.ScaleX *= all * x / c.Surface!.W;
				c.Item.ScaleY *= all * y / c.Surface.H;
			} else {
				c.Item.ScaleX *= all * x / 100;
				c.Item.ScaleY *= all * y / 100;
			}
			if (c.Flag("補間なし") && c.Pixels()) c.Gpu!.SetSmooth(c.Surface!, false);
		}

		private static void Fade(Aup2Context c) {
			double fin = c.V("イン"), fout = c.V("アウト");
			if (fin > 0) c.Item.Opacity *= Math.Clamp(c.Time / fin, 0, 1);
			if (fout > 0) c.Item.Opacity *= Math.Clamp(c.TimeLeft / fout, 0, 1);
		}

		private static void Extend(Aup2Context c) {
			int top = (int)c.V("上"), bottom = (int)c.V("下"), left = (int)c.V("左"), right = (int)c.V("右");
			if (top + bottom + left + right <= 0 || !c.Pixels()) return;
			if (!c.Flag("塗りつぶし")) { c.Grow(left, top, right, bottom); return; }
			// the edge pixels fill the new border
			var s = c.Surface!;
			var t = c.Pass("uniform vec2 u_off; void main(){ o = texture(u_src, clamp((v_uv * u_size - u_off) / u_srcSize, vec2(0.5) / u_srcSize, 1.0 - vec2(0.5) / u_srcSize)); }",
				new Aup2Uniforms().V2("u_off", left, top), Math.Min(Aup2Gpu.MaxSide, s.W + left + right), Math.Min(Aup2Gpu.MaxSide, s.H + top + bottom));
			c.Replace(t, (right - left) / 2.0, (bottom - top) / 2.0);
		}
	}
}
