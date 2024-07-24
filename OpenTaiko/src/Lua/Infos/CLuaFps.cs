using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    internal class CLuaFps
    {
        public double deltaTime => TJAPlayer3.FPS.DeltaTime;
        public int fps => TJAPlayer3.FPS.NowFPS;
    }
}