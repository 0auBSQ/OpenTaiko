using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    internal class CLuaExitStageScript : CLuaStageScript
    {
        public CLuaExitStageScript(string dir, string? texturesDir = null, string? soundsDir = null, bool loadAssets = true) : base(dir, texturesDir, soundsDir, loadAssets)
        {
        }
    }
}
