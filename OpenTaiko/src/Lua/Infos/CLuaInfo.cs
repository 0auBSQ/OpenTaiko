using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    internal class CLuaInfo
    {
        public int playerCount
        {
            get
            {
                return TJAPlayer3.ConfigIni.nPlayerCount;
            }
        }

        public string lang
        {
            get
            {
                return TJAPlayer3.ConfigIni.sLang;
            }
        }

        public bool simplemode
        {
            get
            {
                return TJAPlayer3.ConfigIni.SimpleMode;
            }
        }

        public bool p1IsBlue
        {
            get
            {
                return TJAPlayer3.P1IsBlue();
            }
        }

        public string dir { get; init; }

        public CLuaInfo(string dir)
        {
            this.dir = dir;
        }
    }
}
