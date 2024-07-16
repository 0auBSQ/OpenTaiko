using NLua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TJAPlayer3;
using TJAPlayer3.Animations;

namespace TJAPlayer3
{
    internal class CLuaSongLoadingFadeScript : CLuaFadeScript
    {
        private LuaFunction lfGenTitle;

        public CLuaSongLoadingFadeScript(string dir, string? texturesDir = null, string? soundsDir = null, bool loadAssets = true) : base(dir, texturesDir, soundsDir, loadAssets)
        {
            lfGenTitle = (LuaFunction)LuaScript["genTitle"];
        }

        public void GenTitle(params object[] args)
        {
            if (!Avaibale) return;

            RunLuaCode(lfGenTitle, args);
        }
    }
}
