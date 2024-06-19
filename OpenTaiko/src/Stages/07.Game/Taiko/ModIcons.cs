using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TJAPlayer3
{
    class ModIcons
    {
        static Dictionary<int, Action<int, int, int, int>> __methods = new Dictionary<int, Action<int, int, int, int>>()
        {
            {0, (x, y, a, p) => tDisplayHSIcon(x, y, a) },
            {1, (x, y, a, p) => tDisplayDoronIcon(x, y, a) },
            {2, (x, y, a, p) => tDisplayRandomIcon(x, y, a) },
            {3, (x, y, a, p) => tDisplayFunModIcon(x, y, a) },
            {4, (x, y, a, p) => tDisplayJustIcon(x, y, a) },
            {5, (x, y, a, p) => tDisplayTimingIcon(x, y, a) },
            {6, (x, y, a, p) => tDisplaySongSpeedIcon(x, y, p) },
            {7, (x, y, a, p) => tDisplayAutoIcon(x, y, p) },
        };

        static public void tDisplayMods(int x, int y, int player)
        {
            // +30 x/y
            int actual = TJAPlayer3.GetActualPlayer(player);

            for (int i = 0; i < 8; i++)
            {
                __methods[i](x + TJAPlayer3.Skin.ModIcons_OffsetX[i], y + TJAPlayer3.Skin.ModIcons_OffsetY[i], actual, player);
            }
        }

        static public void tDisplayModsMenu(int x, int y, int player)
        {
            if (TJAPlayer3.Tx.Mod_None != null)
                TJAPlayer3.Tx.Mod_None.Opacity = 0;

            int actual = TJAPlayer3.GetActualPlayer(player);

            for (int i = 0; i < 8; i++)
            {
                __methods[i](x + TJAPlayer3.Skin.ModIcons_OffsetX_Menu[i], y + TJAPlayer3.Skin.ModIcons_OffsetY_Menu[i], actual, player);
            }

            if (TJAPlayer3.Tx.Mod_None != null)
                TJAPlayer3.Tx.Mod_None.Opacity = 255;
        }

        #region [Displayables]

        static private void tDisplayHSIcon(int x, int y, int player)
        {
            // TO DO : Add HS x0.5 icon (_vals == 4)
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
                TJAPlayer3.Tx.HiSp[_i]?.t2D描画(x, y);
            else
                TJAPlayer3.Tx.Mod_None?.t2D描画(x, y);
        }

        static private void tDisplayAutoIcon(int x, int y, int player)
        {
            bool _displayed = false;

            if (TJAPlayer3.ConfigIni.bAutoPlay[player])
                _displayed = true;

            if (_displayed == true)
                TJAPlayer3.Tx.Mod_Auto?.t2D描画(x, y);
            else
                TJAPlayer3.Tx.Mod_None?.t2D描画(x, y);
        }
        
        static private void tDisplayDoronIcon(int x, int y, int player)
        {
            var conf_ = TJAPlayer3.ConfigIni.eSTEALTH[player];

            if (conf_ == EStealthMode.DORON)
                TJAPlayer3.Tx.Mod_Doron?.t2D描画(x, y);
            else if (conf_ == EStealthMode.STEALTH)
                TJAPlayer3.Tx.Mod_Stealth?.t2D描画(x, y);
            else
                TJAPlayer3.Tx.Mod_None?.t2D描画(x, y);
        }

        static private void tDisplayJustIcon(int x, int y, int player)
        {
            var conf_ = TJAPlayer3.ConfigIni.bJust[player];

            if (conf_ == 1)
                TJAPlayer3.Tx.Mod_Just?.t2D描画(x, y);
            else if (conf_ == 2)
                TJAPlayer3.Tx.Mod_Safe?.t2D描画(x, y);
            else
                TJAPlayer3.Tx.Mod_None?.t2D描画(x, y);
        }

        static private void tDisplayRandomIcon(int x, int y, int player)
        {
            var rand_ = TJAPlayer3.ConfigIni.eRandom[player];

            if (rand_ == ERandomMode.MIRROR)
                TJAPlayer3.Tx.Mod_Mirror?.t2D描画(x, y);
            else if (rand_ == ERandomMode.RANDOM)
                TJAPlayer3.Tx.Mod_Random?.t2D描画(x, y);
            else if (rand_ == ERandomMode.SUPERRANDOM)
                TJAPlayer3.Tx.Mod_Super?.t2D描画(x, y);
            else if (rand_ == ERandomMode.MIRRORRANDOM)
                TJAPlayer3.Tx.Mod_Hyper?.t2D描画(x, y);
            else
                TJAPlayer3.Tx.Mod_None?.t2D描画(x, y);
        }
        
        static private void tDisplaySongSpeedIcon(int x, int y, int player)
        {
            if (TJAPlayer3.ConfigIni.nSongSpeed > 20)
                TJAPlayer3.Tx.Mod_SongSpeed[1]?.t2D描画(x, y);
            else if (TJAPlayer3.ConfigIni.nSongSpeed < 20)
                TJAPlayer3.Tx.Mod_SongSpeed[0]?.t2D描画(x, y);
            else
                TJAPlayer3.Tx.Mod_None?.t2D描画(x, y);
        }

        static private void tDisplayFunModIcon(int x, int y, int player)
        {
            int nFun = (int)TJAPlayer3.ConfigIni.nFunMods[player];

            if (nFun > 0)
                TJAPlayer3.Tx.Mod_Fun[nFun]?.t2D描画(x, y);
            else
                TJAPlayer3.Tx.Mod_None?.t2D描画(x, y);
        }

        static private void tDisplayTimingIcon(int x, int y, int player)
        {
            int zones = TJAPlayer3.ConfigIni.nTimingZones[player];

            if (zones != 2)
                TJAPlayer3.Tx.Mod_Timing[zones]?.t2D描画(x, y);
            else
                TJAPlayer3.Tx.Mod_None?.t2D描画(x, y);
        }

        static private void PLACEHOLDER_tDisplayNoneIcon(int x, int y, int player)
        {
            TJAPlayer3.Tx.Mod_None?.t2D描画(x, y);
        }

        #endregion

        #region [Mod flags]

        static public bool tPlayIsStock(int player)
        {
            int actual = TJAPlayer3.GetActualPlayer(player);

            if (TJAPlayer3.ConfigIni.nFunMods[actual] != EFunMods.NONE) return false;
            if (TJAPlayer3.ConfigIni.bJust[actual] != 0) return false;
            if (TJAPlayer3.ConfigIni.nTimingZones[actual] != 2) return false;
            if (TJAPlayer3.ConfigIni.nSongSpeed != 20) return false;
            if (TJAPlayer3.ConfigIni.eRandom[actual] != ERandomMode.OFF) return false;
            if (TJAPlayer3.ConfigIni.eSTEALTH[actual] != EStealthMode.OFF) return false;
            if (TJAPlayer3.ConfigIni.nScrollSpeed[actual] != 9) return false;

            return true;
        }
        static public Int64 tModsToPlayModsFlags(int player)
        {
            byte[] _flags = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 }; 
            int actual = TJAPlayer3.GetActualPlayer(player);

            _flags[0] = (byte)Math.Min(255, TJAPlayer3.ConfigIni.nScrollSpeed[actual]);
            _flags[1] = (byte)TJAPlayer3.ConfigIni.eSTEALTH[actual];
            _flags[2] = (byte)TJAPlayer3.ConfigIni.eRandom[actual];
            _flags[3] = (byte)Math.Min(255, TJAPlayer3.ConfigIni.nSongSpeed);
            _flags[4] = (byte)TJAPlayer3.ConfigIni.nTimingZones[actual];
            _flags[5] = (byte)TJAPlayer3.ConfigIni.bJust[actual];
            _flags[7] = (byte)TJAPlayer3.ConfigIni.nFunMods[actual];
            
            return BitConverter.ToInt64(_flags, 0);
        }


        #endregion
    }
}
