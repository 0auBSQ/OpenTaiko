using NLua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    internal class AnimeBG : ScriptBG
    {
        private LuaFunction LuaPlayAnimation;
        public AnimeBG(string filePath) : base(filePath)
        {
            if (LuaScript != null)
            {
                LuaPlayAnimation = LuaScript.GetFunction("playAnime");
            }
        }
        public new void Dispose()
        {
            base.Dispose();
            LuaPlayAnimation?.Dispose();
        }

        public void PlayAnimation()
        {
            if (LuaScript == null) return;
            try
            {
                LuaPlayAnimation.Call();
            }
            catch (Exception ex)
            {
            }
        }
    }
}
