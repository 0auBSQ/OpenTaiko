using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;

namespace TJAPlayer3
{
    internal class HPrivateFastFont
    {
        static string DefaultFont = CFontRenderer.DefaultFontName;

        public static bool FontExists(string fontpath)
        {
            return CFontRenderer.FontExists(fontpath);
        }

        public static CCachedFontRenderer tInstantiateFont(string fontName, int scale)
        {
            if (FontExists(fontName))
                return (new CCachedFontRenderer(fontName, scale));
            return (new CCachedFontRenderer(DefaultFont, scale));
        }

        public static CCachedFontRenderer tInstantiateMainFont(int scale)
        {
            return tInstantiateFont(TJAPlayer3.Skin.FontName, scale);
        }

        public static CCachedFontRenderer tInstantiateBoxFont(int scale)
        {
            return tInstantiateFont(TJAPlayer3.Skin.BoxFontName, scale);
        }
    }
}
