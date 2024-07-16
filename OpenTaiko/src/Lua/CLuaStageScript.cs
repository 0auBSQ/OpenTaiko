using NLua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    internal class CLuaStageScript : CLuaScript
    {
        private LuaFunction lfInit;
        private LuaFunction lfFinal;
        private LuaFunction lfUpdate;
        private LuaFunction lfDraw;

        public CLuaStageScript(string dir, string? texturesDir = null, string? soundsDir = null, bool loadAssets = true) : base(dir, texturesDir, soundsDir, loadAssets)
        {
            lfInit = (LuaFunction)LuaScript["init"];
            lfFinal = (LuaFunction)LuaScript["final"];
            lfUpdate = (LuaFunction)LuaScript["update"];
            lfDraw = (LuaFunction)LuaScript["draw"];
        }

        public void Init(params object[] args)
        {
            bCrashed = false;
            if (!Avaibale) return;

            RunLuaCode(lfInit, args);

        }

        public void Final(params object[] args)
        {
            if (!Avaibale) return;

            RunLuaCode(lfFinal, args);
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
