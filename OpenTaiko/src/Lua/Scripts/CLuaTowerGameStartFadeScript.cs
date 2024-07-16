using NLua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    internal class CLuaTowerGameStartFadeScript : CLuaSongLoadingFadeScript
    {
        private LuaFunction lfSetTowerTexture;
        public CLuaTowerGameStartFadeScript(string dir, string? texturesDir = null, string? soundsDir = null, bool loadAssets = true) : base(dir, texturesDir, soundsDir, loadAssets)
        {
            lfSetTowerTexture = (LuaFunction)LuaScript["setTowerTexture"];
        }

        public void SetTowerTexture(params object[] args)
        {
            if (!Avaibale) return;

            RunLuaCode(lfSetTowerTexture, args);
        }
    }
}
