using NLua;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TJAPlayer3;
using static TJAPlayer3.CTextConsole;

namespace TJAPlayer3
{
    internal class CLuaConsoleFontScript : CLuaScript
    {
        private LuaFunction lfDrawText;

        public CLuaConsoleFontScript(string dir, string? texturesDir = null, string? soundsDir = null, bool loadAssets = true) : base(dir, texturesDir, soundsDir, loadAssets)
        {
            lfDrawText = (LuaFunction)LuaScript["drawText"];
        }

        public void tDrawText(int x, int y, EFontType font, string strAlphanumericString)
        {
            if (!Avaibale) return;

            string type;
            switch(font)
            {
                case EFontType.White:
                    type = "white";
                    break;
                case EFontType.Cyan:
                    type = "cyan";
                    break;
                case EFontType.Gray:
                    type = "gray";
                    break;
                case EFontType.WhiteSlim:
                    type = "whiteslim";
                    break;
                case EFontType.CyanSlim:
                    type = "cyanslim";
                    break;
                case EFontType.GraySlim:
                    type = "grayslim";
                    break;
                default:
                    type = "unknown";
                    break;
            }
            RunLuaCode(lfDrawText, x, y, type, strAlphanumericString);
        }
    }
}
