using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    internal class HPrivateFastFont
    {
        static string DefaultFont = "MS UI Gothic";

        public static CPrivateFastFont tInstantiateFont(string fontName, int scale)
        {
            if (!string.IsNullOrEmpty(fontName))
                return (new CPrivateFastFont(new FontFamily(fontName), scale));
            return (new CPrivateFastFont(new FontFamily(DefaultFont), scale));
        }

        public static CPrivateFastFont tInstantiateMainFont(int scale)
        {
            return tInstantiateFont(TJAPlayer3.ConfigIni.FontName, scale);
        }

        public static CPrivateFastFont tInstantiateBoxFont(int scale)
        {
            return tInstantiateFont(TJAPlayer3.ConfigIni.BoxFontName, scale);
        }
    }
}
