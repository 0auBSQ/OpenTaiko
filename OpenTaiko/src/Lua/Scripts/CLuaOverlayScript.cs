using NLua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    internal class CLuaOverlayScript : CLuaScript
    {
        private LuaFunction lfUpdate;
        private LuaFunction lfDraw;

        public CLuaOverlayScript(string dir, string? texturesDir = null, string? soundsDir = null, bool loadAssets = true) : base(dir, texturesDir, soundsDir, loadAssets)
        {
            lfUpdate = (LuaFunction)LuaScript["update"];
            lfDraw = (LuaFunction)LuaScript["draw"];
        }

        public void Update(params object[] args)
        {
            if (!Avaibale) return;

            RunLuaCode(lfUpdate, args);
        }

        public void Draw(params object[] args)
        {
            if (!Avaibale) return;

            RunLuaCode(lfDraw, args);
        }
    }
}
