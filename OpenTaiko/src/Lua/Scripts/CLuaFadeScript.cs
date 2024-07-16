using NLua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    internal class CLuaFadeScript : CLuaScript
    {
        private LuaFunction lfFadeIn;
        private LuaFunction lfFadeOut;
        private LuaFunction lfUpdate;
        private LuaFunction lfDraw;

        public CLuaFadeInfo luaFadeInfo { get; private set; }



        public CLuaFadeScript(string dir, string? texturesDir = null, string? soundsDir = null, bool loadAssets = true) : base(dir, texturesDir, soundsDir, loadAssets)
        {
            LuaScript["fadeinfo"] = luaFadeInfo = new CLuaFadeInfo();

            lfFadeIn = (LuaFunction)LuaScript["fadeIn"];
            lfFadeOut = (LuaFunction)LuaScript["fadeOut"];
            lfUpdate = (LuaFunction)LuaScript["update"];
            lfDraw = (LuaFunction)LuaScript["draw"];
        }

        public void FadeIn(params object[] args)
        {
            if (!Avaibale) return;

            RunLuaCode(lfFadeIn, args);
        }

        public void FadeOut(params object[] args)
        {
            if (!Avaibale) return;

            RunLuaCode(lfFadeOut, args);
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
