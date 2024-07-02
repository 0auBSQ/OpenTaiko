using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    internal class CLuaFps
    {
        public double deltaTime
        {
            get
            {
                return TJAPlayer3.FPS.DeltaTime;
            }
        }


        public int fps
        {
            get
            {
                return TJAPlayer3.FPS.NowFPS;
            }
        }
    }
}
