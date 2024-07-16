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
    internal class CLuaDanGameStartFadeScript : CLuaSongLoadingFadeScript
    {
        private LuaFunction lfSetDanPlateTexture;
        public CLuaDanGameStartFadeScript(string dir, string? texturesDir = null, string? soundsDir = null, bool loadAssets = true) : base(dir, texturesDir, soundsDir, loadAssets)
        {
            lfSetDanPlateTexture = (LuaFunction)LuaScript["setDanPlateTexture"];
        }

        public void SetDanPlateTexture(params object[] args)
        {
            if (!Avaibale) return;

            RunLuaCode(lfSetDanPlateTexture, args);
        }
    }
}
