using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SkiaSharp;

namespace OpenTaiko.AviUtl2 {
	// The settings of a text object (テキスト) at one moment.
	public sealed class Aup2TextStyle {
		public string Text = "";
		public string Font = "";
		public double Size = 40, Spacing, Leading, Speed, Time;
		public int R = 255, G = 255, B = 255;       // 文字色
		public int ER, EG, EB;                      // 影・縁色
		public string Decoration = "標準文字";
		public string Align = "左寄せ[上]";
		public bool Bold, Italic, PerChar;

		internal static Aup2TextStyle Read(Aup2Context c) {
			var col = c.Color("文字色");
			var edge = c.Color("影・縁色", System.Numerics.Vector3.Zero);
			return new Aup2TextStyle {
				Text = Unescape(c.S("テキスト")),
				Font = c.S("フォント"),
				Size = Math.Max(1, c.V("サイズ", 40)), Spacing = c.V("字間"), Leading = c.V("行間"),
				Speed = c.V("表示速度"), Time = c.Time,
				R = (int)Math.Round(col.X * 255), G = (int)Math.Round(col.Y * 255), B = (int)Math.Round(col.Z * 255),
				ER = (int)Math.Round(edge.X * 255), EG = (int)Math.Round(edge.Y * 255), EB = (int)Math.Round(edge.Z * 255),
				Decoration = c.S("文字装飾", "標準文字"), Align = c.S("文字揃え", "左寄せ[上]"),
				Bold = c.Flag("B"), Italic = c.Flag("I"), PerChar = c.Flag("文字毎に個別オブジェクト"),
			};
		}

		// "\n" is a line break and "\\" a backslash
		private static string Unescape(string s) {
			if (s.IndexOf('\\') < 0) return s;
			var sb = new StringBuilder(s.Length);
			for (int i = 0; i < s.Length; i++) {
				if (s[i] == '\\' && i + 1 < s.Length) {
					char n = s[i + 1];
					if (n == 'n') { sb.Append('\n'); i++; continue; }
					if (n == '\\') { sb.Append('\\'); i++; continue; }
				}
				sb.Append(s[i]);
			}
			return sb.ToString();
		}
	}

	// A rendered text: the bitmap, each letter's rectangle in it, and where the object's point is.
	internal sealed class Aup2TextImage : IDisposable {
		public SKBitmap Bitmap = null!;
		public List<SKRect> Letters = new();
		// the object's position inside the bitmap, from its top left
		public double AnchorX, AnchorY;
		public void Dispose() => Bitmap?.Dispose();
	}

	// Draws a text object with Skia: the control tags inside the text (<#rrggbb>, <s size,font,BI>,
	// <r speed>, <w seconds>, <p x,y>), the letters shown so far, the alignment and the decoration.
	internal static class Aup2TextRaster {
		private sealed class Glyph {
			public string Text = "";
			public float X, Y, W, H, Ascent;
			public SKColor Color;
			public float Size;
			public SKTypeface Face = null!;
			public bool Shown = true;
			public int Line;
		}

		private static readonly Dictionary<string, SKTypeface> Faces = new();

		private static SKTypeface Face(string family, bool bold, bool italic) {
			string key = family + "|" + (bold ? "b" : "") + (italic ? "i" : "");
			lock (Faces) {
				if (Faces.TryGetValue(key, out var f)) return f;
				var style = new SKFontStyle(bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal, SKFontStyleWidth.Normal,
					italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
				f = (family.Length > 0 ? SKTypeface.FromFamilyName(family, style) : null) ?? SKTypeface.FromFamilyName(null, style) ?? SKTypeface.Default;
				Faces[key] = f;
				return f;
			}
		}

		// a key for the picture a style gives: two styles with the same key draw the same bitmap
		public static string Key(Aup2TextStyle s, int shown) =>
			string.Join("\u0001", s.Text, s.Font, s.Size.ToString("F2", CultureInfo.InvariantCulture), s.Spacing.ToString("F2", CultureInfo.InvariantCulture),
				s.Leading.ToString("F2", CultureInfo.InvariantCulture), s.R, s.G, s.B, s.ER, s.EG, s.EB, s.Decoration, s.Align, s.Bold, s.Italic, shown);

		// how many letters show at the style's time (the same count gives the same picture)
		public static int ShownCount(Aup2TextStyle s) {
			var glyphs = Layout(s, out _, out _, measureOnly: true);
			int n = 0;
			foreach (var g in glyphs) if (g.Shown) n++;
			return n;
		}

		private static List<Glyph> Layout(Aup2TextStyle s, out float width, out float height, bool measureOnly = false) {
			var glyphs = new List<Glyph>();
			bool vertical = s.Align.Contains("縦書");
			float size = (float)s.Size;
			string family = s.Font;
			bool bold = s.Bold, italic = s.Italic;
			var color = new SKColor((byte)s.R, (byte)s.G, (byte)s.B);
			double speed = s.Speed, clock = 0;
			float penX = 0, penY = 0, lineAdvance = 0;
			int line = 0;
			var lineStarts = new List<int> { 0 };
			using var paint = new SKPaint { IsAntialias = true };

			void NewLine() {
				if (lineAdvance <= 0) lineAdvance = size;
				if (vertical) { penX -= lineAdvance + (float)s.Leading; penY = 0; }
				else { penY += lineAdvance + (float)s.Leading; penX = 0; }
				lineAdvance = 0;
				line++;
				lineStarts.Add(glyphs.Count);
			}

			string text = s.Text;
			for (int i = 0; i < text.Length;) {
				char ch = text[i];
				if (ch == '<') {
					int end = text.IndexOf('>', i);
					if (end > i && Tag(text.Substring(i + 1, end - i - 1), s, ref size, ref family, ref bold, ref italic, ref color, ref speed, ref clock, ref penX, ref penY)) {
						i = end + 1;
						continue;
					}
				}
				if (ch == '\r') { i++; continue; }
				if (ch == '\n') { NewLine(); i++; continue; }
				string cluster = char.IsHighSurrogate(ch) && i + 1 < text.Length ? text.Substring(i, 2) : ch.ToString();
				i += cluster.Length;

				var face = Face(family, bold, italic);
				paint.Typeface = face;
				paint.TextSize = size;
				var m = paint.FontMetrics;
				// a letter the font lacks comes from a fallback font, on the font's own line
				int codepoint = cluster.Length == 2 && char.IsSurrogatePair(cluster[0], cluster[1]) ? char.ConvertToUtf32(cluster[0], cluster[1]) : cluster[0];
				face = FDK.CFontRenderer.TypefaceFor(face, codepoint);
				paint.Typeface = face;
				float adv = paint.MeasureText(cluster);
				float h = m.Descent - m.Ascent;
				var g = new Glyph { Text = cluster, Color = color, Size = size, Face = face, W = adv, H = h, Ascent = -m.Ascent, Line = line };
				if (vertical) {
					g.X = penX - adv / 2; g.Y = penY;
					penY += h + (float)s.Spacing;
					lineAdvance = Math.Max(lineAdvance, size);
				} else {
					g.X = penX; g.Y = penY;
					penX += adv + (float)s.Spacing;
					lineAdvance = Math.Max(lineAdvance, h);
				}
				// a letter shows once the clock reaches it
				if (speed > 0) {
					clock += 1.0 / speed;
					g.Shown = clock <= s.Time + 1e-9;
				} else {
					g.Shown = clock <= s.Time + 1e-9;
				}
				glyphs.Add(g);
			}

			// lines are aligned among themselves, then the block takes its place
			float minX = 0, minY = 0, maxX = 0, maxY = 0;
			bool any = false;
			foreach (var g in glyphs) {
				if (!any) { minX = g.X; minY = g.Y; maxX = g.X + g.W; maxY = g.Y + g.H; any = true; continue; }
				minX = Math.Min(minX, g.X); minY = Math.Min(minY, g.Y); maxX = Math.Max(maxX, g.X + g.W); maxY = Math.Max(maxY, g.Y + g.H);
			}
			width = maxX - minX; height = maxY - minY;
			if (!vertical && !s.Align.StartsWith("左寄せ", StringComparison.Ordinal)) {
				float k = s.Align.StartsWith("右寄せ", StringComparison.Ordinal) ? 1f : 0.5f;
				for (int l = 0; l < lineStarts.Count; l++) {
					int a = lineStarts[l], b = l + 1 < lineStarts.Count ? lineStarts[l + 1] : glyphs.Count;
					if (a >= b) continue;
					float right = 0;
					for (int j = a; j < b; j++) right = Math.Max(right, glyphs[j].X + glyphs[j].W);
					float shift = (maxX - right) * k;
					for (int j = a; j < b; j++) glyphs[j].X += shift;
				}
			}
			foreach (var g in glyphs) { g.X -= minX; g.Y -= minY; }
			return glyphs;
		}

		private static bool Tag(string body, Aup2TextStyle s, ref float size, ref string family, ref bool bold, ref bool italic, ref SKColor color,
			ref double speed, ref double clock, ref float penX, ref float penY) {
			if (body.Length == 0) return false;
			char kind = body[0];
			string arg = body.Substring(1);
			switch (kind) {
				case '#':
					if (arg.Length == 0) { color = new SKColor((byte)s.R, (byte)s.G, (byte)s.B); return true; }
					if (arg.Length >= 6 && int.TryParse(arg.AsSpan(0, 6), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb)) {
						color = new SKColor((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
						return true;
					}
					return false;
				case 's': {
					if (arg.Length == 0) { size = (float)s.Size; family = s.Font; bold = s.Bold; italic = s.Italic; return true; }
					var p = arg.Split(',');
					if (p[0].Trim().Length > 0) {
						if (!float.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float v)) return false;
						size = Math.Max(1, v);
					}
					if (p.Length > 1 && p[1].Trim().Length > 0) family = p[1].Trim();
					if (p.Length > 2) { bold = p[2].Contains('B'); italic = p[2].Contains('I'); }
					return true;
				}
				case 'r':
					if (arg.Length == 0) { speed = s.Speed; return true; }
					if (!double.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out double r)) return false;
					speed = r;
					return true;
				case 'w': {
					string t = arg.TrimStart('*');
					if (!double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double w)) return false;
					clock += w;
					return true;
				}
				case 'p': {
					var p = arg.Split(',');
					if (p.Length < 2) return false;
					if (!float.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
						!float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) return false;
					// a sign makes it a move from here
					penX = (p[0].StartsWith('+') || p[0].StartsWith('-')) ? penX + x : x;
					penY = (p[1].StartsWith('+') || p[1].StartsWith('-')) ? penY + y : y;
					return true;
				}
				case 'c':
					return arg.Length == 0;
				case '?':
					return true;   // a script: not run
			}
			return false;
		}

		public static Aup2TextImage Render(Aup2TextStyle s) {
			var glyphs = Layout(s, out float width, out float height);
			string deco = s.Decoration;
			bool shadow = deco.StartsWith("影付き", StringComparison.Ordinal), outline = deco.StartsWith("縁取り", StringComparison.Ordinal);
			float baseSize = (float)s.Size;
			float edge = 0;
			if (outline) edge = Math.Max(1, baseSize * (deco.Contains("細") ? 0.03f : deco.Contains("太") ? 0.12f : 0.06f));
			float drop = shadow ? Math.Max(1, baseSize * 0.05f) : 0;
			int pad = (int)Math.Ceiling(Math.Max(edge, drop) + baseSize * 0.2f) + 2;   // italics and overhangs
			int w = Math.Max(1, (int)Math.Ceiling(width) + 2 * pad), h = Math.Max(1, (int)Math.Ceiling(height) + 2 * pad);
			w = Math.Min(w, Aup2Gpu.MaxSide); h = Math.Min(h, Aup2Gpu.MaxSide);

			var image = new Aup2TextImage { Bitmap = new SKBitmap(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul)) };
			using (var canvas = new SKCanvas(image.Bitmap)) {
				canvas.Clear(SKColors.Transparent);
				using var paint = new SKPaint { IsAntialias = true, SubpixelText = true };
				var edgeColor = new SKColor((byte)s.ER, (byte)s.EG, (byte)s.EB, (byte)(deco.Contains("薄") ? 128 : 255));
				// the decoration goes under every letter, then the letters
				for (int pass = 0; pass < 2; pass++) {
					foreach (var g in glyphs) {
						if (!g.Shown) continue;
						paint.Typeface = g.Face;
						paint.TextSize = g.Size;
						float x = pad + g.X, y = pad + g.Y + g.Ascent;
						if (pass == 0) {
							if (outline) {
								paint.Style = SKPaintStyle.Stroke;
								paint.StrokeWidth = edge * 2;
								paint.StrokeJoin = deco.Contains("角") ? SKStrokeJoin.Miter : SKStrokeJoin.Round;
								paint.Color = edgeColor;
								canvas.DrawText(g.Text, x, y, paint);
							} else if (shadow) {
								paint.Style = SKPaintStyle.Fill;
								paint.Color = edgeColor;
								canvas.DrawText(g.Text, x + drop, y + drop, paint);
							}
						} else {
							paint.Style = SKPaintStyle.Fill;
							paint.Color = g.Color;
							canvas.DrawText(g.Text, x, y, paint);
						}
					}
				}
			}
			foreach (var g in glyphs)
				if (g.Shown) image.Letters.Add(new SKRect(pad + g.X - edge, pad + g.Y - edge, pad + g.X + g.W + edge + drop, pad + g.Y + g.H + edge + drop));

			// the object's point: the side the text is aligned to, and the height named in the brackets
			bool vertical = s.Align.Contains("縦書");
			double ax = 0, ay = 0;
			if (!vertical) {
				ax = s.Align.StartsWith("右寄せ", StringComparison.Ordinal) ? width : s.Align.StartsWith("中央", StringComparison.Ordinal) ? width / 2 : 0;
				ay = s.Align.Contains("[下]") ? height : s.Align.Contains("[中]") ? height / 2 : 0;
			} else {
				ax = s.Align.Contains("[左]") ? 0 : s.Align.Contains("[中]") ? width / 2 : width;
				ay = s.Align.Contains("下寄") ? height : s.Align.Contains("中央") ? height / 2 : 0;
			}
			image.AnchorX = pad + ax;
			image.AnchorY = pad + ay;
			return image;
		}
	}
}
