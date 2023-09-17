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

        public static CCachedFontRenderer tInstantiateFont(string fontName, int scale)
        {
            if (!string.IsNullOrEmpty(fontName))
                return (new CCachedFontRenderer(fontName, scale));
            return (new CCachedFontRenderer(DefaultFont, scale));
        }

        public static CCachedFontRenderer tInstantiateMainFont(int scale)
        {
            return tInstantiateFont(TJAPlayer3.ConfigIni.FontName, scale);
        }

        public static CCachedFontRenderer tInstantiateBoxFont(int scale)
        {
            return tInstantiateFont(TJAPlayer3.ConfigIni.BoxFontName, scale);
        }
    }
}
