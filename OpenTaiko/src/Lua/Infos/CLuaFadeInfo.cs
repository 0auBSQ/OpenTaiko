using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    internal class CLuaFadeInfo
    {
        public double dbValue;
        public EFIFOモード eFIFOMode;
        public bool bEnded => dbValue == 1.0f;
        public string strState
        {
            get
            {
                switch (eFIFOMode)
                {
                    case EFIFOモード.フェードイン:
                        return bEnded ? "none" : "in";
                    case EFIFOモード.フェードアウト:
                    default:
                        return bEnded ? "idle" : "out";
                }
            }
        }
    }
}
