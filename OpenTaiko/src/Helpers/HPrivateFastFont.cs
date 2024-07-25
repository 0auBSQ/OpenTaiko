using FDK;

namespace TJAPlayer3 {
	internal class HPrivateFastFont {
		static string DefaultFont = CFontRenderer.DefaultFontName;

		public static bool FontExists(string fontpath) {
			return CFontRenderer.FontExists(fontpath);
		}

		public static CCachedFontRenderer tInstantiateFont(string fontName, int scale, CFontRenderer.FontStyle style = CFontRenderer.FontStyle.Regular) {
			if (FontExists(fontName))
				return (new CCachedFontRenderer(fontName, scale, style));
			return (new CCachedFontRenderer(DefaultFont, scale, style));
		}

		public static CCachedFontRenderer tInstantiateMainFont(int scale, CFontRenderer.FontStyle style = CFontRenderer.FontStyle.Regular) {
			if (FontExists(TJAPlayer3.Skin.FontName))
				return (new CCachedFontRenderer(TJAPlayer3.Skin.FontName, scale, style));
			if (FontExists(CLangManager.LangInstance.FontName))
				return (new CCachedFontRenderer(CLangManager.LangInstance.FontName, scale, style));
			return (new CCachedFontRenderer(DefaultFont, scale, style));
		}

		public static CCachedFontRenderer tInstantiateBoxFont(int scale, CFontRenderer.FontStyle style = CFontRenderer.FontStyle.Regular) {
			if (FontExists(TJAPlayer3.Skin.BoxFontName))
				return (new CCachedFontRenderer(TJAPlayer3.Skin.FontName, scale, style));
			if (FontExists(CLangManager.LangInstance.BoxFontName))
				return (new CCachedFontRenderer(CLangManager.LangInstance.FontName, scale, style));
			return (new CCachedFontRenderer(DefaultFont, scale, style));
		}
	}
}
