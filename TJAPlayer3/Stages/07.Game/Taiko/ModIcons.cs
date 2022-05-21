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
            tDisplayDoronIcon(x + 30, y, actual); // 2nd icon
            tDisplayRandomIcon(x + 60, y, actual); // 3rd icon
            PLACEHOLDER_tDisplayNoneIcon(x + 90, y, player); // 4th icon
            tDisplayJustIcon(x, y + 30, actual); // 5th icon
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
            tDisplayDoronIcon(x + 30, y, actual); // 2nd icon
            tDisplayRandomIcon(x + 60, y, actual); // 3rd icon
            PLACEHOLDER_tDisplayNoneIcon(x + 60, y, player); // 4th icon
            tDisplayJustIcon(x + 120, y, actual); // 5th icon
            tDisplayTimingIcon(x + 150, y, actual); // 6th icon
            tDisplaySongSpeedIcon(x + 180, y, player); // 7th icon
            tDisplayAutoIcon(x + 210, y, player); // 8th icon

            if (TJAPlayer3.Tx.Mod_None != null)
                TJAPlayer3.Tx.Mod_None.Opacity = 255;
        }

        static private void tDisplayHSIcon(int x, int y, int player)
        {
            var _vals = new int[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 24, 29, 34, 39 };
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
        
        static private void tDisplayDoronIcon(int x, int y, int player)
        {
            var conf_ = TJAPlayer3.ConfigIni.eSTEALTH[player];

            if (conf_ == Eステルスモード.DORON)
                TJAPlayer3.Tx.Mod_Doron?.t2D描画(TJAPlayer3.app.Device, x, y);
            else if (conf_ == Eステルスモード.STEALTH)
                TJAPlayer3.Tx.Mod_Stealth?.t2D描画(TJAPlayer3.app.Device, x, y);
            else
                TJAPlayer3.Tx.Mod_None?.t2D描画(TJAPlayer3.app.Device, x, y);
        }

        static private void tDisplayJustIcon(int x, int y, int player)
        {
            var conf_ = TJAPlayer3.ConfigIni.bJust[player];

            if (conf_ == 1)
                TJAPlayer3.Tx.Mod_Just?.t2D描画(TJAPlayer3.app.Device, x, y);
            else if (conf_ == 2)
                TJAPlayer3.Tx.Mod_Safe?.t2D描画(TJAPlayer3.app.Device, x, y);
            else
                TJAPlayer3.Tx.Mod_None?.t2D描画(TJAPlayer3.app.Device, x, y);
        }

        static private void tDisplayRandomIcon(int x, int y, int player)
        {
            var rand_ = TJAPlayer3.ConfigIni.eRandom[player];

            if (rand_ == Eランダムモード.MIRROR)
                TJAPlayer3.Tx.Mod_Mirror?.t2D描画(TJAPlayer3.app.Device, x, y);
            else if (rand_ == Eランダムモード.RANDOM)
                TJAPlayer3.Tx.Mod_Random?.t2D描画(TJAPlayer3.app.Device, x, y);
            else if (rand_ == Eランダムモード.SUPERRANDOM)
                TJAPlayer3.Tx.Mod_Super?.t2D描画(TJAPlayer3.app.Device, x, y);
            else if (rand_ == Eランダムモード.HYPERRANDOM)
                TJAPlayer3.Tx.Mod_Hyper?.t2D描画(TJAPlayer3.app.Device, x, y);
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
