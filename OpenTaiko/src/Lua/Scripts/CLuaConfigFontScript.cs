using NLua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TJAPlayer3;

namespace TJAPlayer3
{
    internal class CLuaConfigFontScript : CLuaScript
    {
        private LuaFunction lfDrawText;

        public CLuaConfigFontScript(string dir, string? texturesDir = null, string? soundsDir = null, bool loadAssets = true) : base(dir, texturesDir, soundsDir, loadAssets)
        {
            lfDrawText = (LuaFunction)LuaScript["drawText"];
        }

        public void tDrawText(int x, int y, string str, bool b強調, float fScale)
        {
            if (!Avaibale) return;

            RunLuaCode(lfDrawText, x, y, str, b強調, fScale);
        }
    }
}
