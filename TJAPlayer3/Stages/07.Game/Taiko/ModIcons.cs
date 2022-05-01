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
            // +30 x/y
            int actual = TJAPlayer3.GetActualPlayer(player);

            tDisplayHSIcon(x, y, actual); // 1st icon
            PLACEHOLDER_tDisplayNoneIcon(x + 30, y, player); // 2nd icon
            PLACEHOLDER_tDisplayNoneIcon(x + 60, y, player); // 3rd icon
            PLACEHOLDER_tDisplayNoneIcon(x + 90, y, player); // 4th icon
            PLACEHOLDER_tDisplayNoneIcon(x, y + 30, player); // 5th icon
            tDisplayTimingIcon(x + 30, y + 30, actual); // 6th icon
            tDisplaySongSpeedIcon(x + 60, y + 30, player); // 7th icon
            tDisplayAutoIcon(x + 90, y + 30, player); // 8th icon
        }

        static public void tDisplayModsMenu(int x, int y, int player)
        {
            if (TJAPlayer3.Tx.Mod_None != null)
                TJAPlayer3.Tx.Mod_None.Opacity = 0;

            int actual = TJAPlayer3.GetActualPlayer(player);

            tDisplayHSIcon(x, y, actual); // 1st icon
            PLACEHOLDER_tDisplayNoneIcon(x + 30, y, player); // 2nd icon
            PLACEHOLDER_tDisplayNoneIcon(x + 60, y, player); // 3rd icon
            PLACEHOLDER_tDisplayNoneIcon(x + 90, y, player); // 4th icon
            PLACEHOLDER_tDisplayNoneIcon(x + 120, y, player); // 5th icon
            tDisplayTimingIcon(x + 150, y, actual); // 6th icon
            tDisplaySongSpeedIcon(x + 180, y, player); // 7th icon
            tDisplayAutoIcon(x + 210, y, player); // 8th icon

            if (TJAPlayer3.Tx.Mod_None != null)
                TJAPlayer3.Tx.Mod_None.Opacity = 255;
        }

        static private void tDisplayHSIcon(int x, int y, int player)
        {
            var _vals = new int[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 24, 29, 34, 39, 44, 49 };
            int _i = -1;

            for (int j = 0; j < _vals.Length; j++)
            {
                if (TJAPlayer3.ConfigIni.nScrollSpeed[player] >= _vals[j] && j < TJAPlayer3.Tx.HiSp.Length)
                    _i = j;
                else
                    break;
            }

            if (_i >= 0)
                TJAPlayer3.Tx.HiSp[_i]?.t2D描画(TJAPlayer3.app.Device, x, y);
            else
                TJAPlayer3.Tx.Mod_None?.t2D描画(TJAPlayer3.app.Device, x, y);
        }

        static private void tDisplayAutoIcon(int x, int y, int player)
        {
            bool _displayed = false;

            if (player == 0 && TJAPlayer3.ConfigIni.b太鼓パートAutoPlay)
                _displayed = true;
            else if (player == 1 && TJAPlayer3.ConfigIni.b太鼓パートAutoPlay2P)
                _displayed = true;

            if (_displayed == true)
                TJAPlayer3.Tx.Mod_Auto?.t2D描画(TJAPlayer3.app.Device, x, y);
            else
                TJAPlayer3.Tx.Mod_None?.t2D描画(TJAPlayer3.app.Device, x, y);
        }

        static private void tDisplaySongSpeedIcon(int x, int y, int player)
        {
            if (TJAPlayer3.ConfigIni.n演奏速度 > 20)
                TJAPlayer3.Tx.Mod_SongSpeed[1]?.t2D描画(TJAPlayer3.app.Device, x, y);
            else if (TJAPlayer3.ConfigIni.n演奏速度 < 20)
                TJAPlayer3.Tx.Mod_SongSpeed[0]?.t2D描画(TJAPlayer3.app.Device, x, y);
            else
                TJAPlayer3.Tx.Mod_None?.t2D描画(TJAPlayer3.app.Device, x, y);
        }

        static private void tDisplayTimingIcon(int x, int y, int player)
        {
            int zones = TJAPlayer3.ConfigIni.nTimingZones[player];

            if (zones != 2)
                TJAPlayer3.Tx.Mod_Timing[zones]?.t2D描画(TJAPlayer3.app.Device, x, y);
            else
                TJAPlayer3.Tx.Mod_None?.t2D描画(TJAPlayer3.app.Device, x, y);
        }

        static private void PLACEHOLDER_tDisplayNoneIcon(int x, int y, int player)
        {
            TJAPlayer3.Tx.Mod_None?.t2D描画(TJAPlayer3.app.Device, x, y);
        }

    }
}
