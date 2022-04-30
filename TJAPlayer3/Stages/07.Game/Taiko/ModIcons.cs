using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    class ModIcons
    {
        static public void tDisplayMods(int x, int y, int player)
        {
            tDisplayHSIcon(x, y, player);
        }

        static private void tDisplayHSIcon(int x, int y, int player)
        {
            var _vals = new int[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 24, 29, 34, 39, 44, 49 };
            int _i = -1;

            for (int j = 0; j < _vals.Length; j++)
            {
                if (TJAPlayer3.ConfigIni.nScrollSpeed[player] >= _vals[j])
                    _i = j;
                else
                    break;
            }

            if (_i >= 0)
                TJAPlayer3.Tx.HiSp[_i]?.t2D描画(TJAPlayer3.app.Device, x, y);
        } 

    }
}
