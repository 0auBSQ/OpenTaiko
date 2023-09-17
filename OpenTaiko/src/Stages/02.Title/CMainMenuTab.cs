using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using static TJAPlayer3.CActSelect曲リスト;
using FDK;

namespace TJAPlayer3
{
    class CMainMenuTab
    {
        public static int __MenuCount = 14; // Number of existing menus
        public static CMainMenuTab[] __Menus;
        public static bool __BoxesProcessed = false;

        public TitleTextureKey ttkTitle;
        public TitleTextureKey ttkBoxText;
        public bool _1pRestricted;
        public bool implemented;
        public CTexture barTex;
        public CTexture barChara;
        public CStageタイトル.E戻り値 rp;

        public CMainMenuTab(int boxId, Color col, CCachedFontRenderer tpf, CCachedFontRenderer boxpf, CStageタイトル.E戻り値 returnPoint, bool _1Ponly, bool impl, CTexture[] modeSelect_Bar, CTexture[] modeSelect_Bar_Chara)
        {
            string title = CLangManager.LangInstance.GetString(100 + boxId);

            ttkTitle = new TitleTextureKey(title, tpf, Color.White, col, 1280, Color.Black);

            string boxText = CLangManager.LangInstance.GetString(150 + boxId);

            ttkBoxText = new TitleTextureKey(boxText, boxpf, Color.White, Color.Black, 1000);

            rp = returnPoint;

            _1pRestricted = _1Ponly;
            implemented = impl;
            barTex = (modeSelect_Bar.Length > boxId) ? modeSelect_Bar[boxId] : null;
            barChara = (modeSelect_Bar_Chara.Length > boxId) ? modeSelect_Bar_Chara[boxId] : null;
        }

        public static void tInitMenus(CCachedFontRenderer tpf, CCachedFontRenderer boxpf, CTexture[] modeSelect_Bar, CTexture[] modeSelect_Bar_Chara)
        {
            // Proceed the boxes only once

            if (__BoxesProcessed == false)
            {
                __Menus = new CMainMenuTab[__MenuCount];

                // Removed to avoid having to reload the game when changing language
                //__BoxesProcessed = true;

                #region [Menu Colors]

                Color[] __MenuColors =
                {
                    Color.FromArgb(233, 53, 71),
                    Color.FromArgb(71, 64, 135),
                    Color.FromArgb(255, 180, 42),
                    Color.FromArgb(16, 255, 255),
                    Color.FromArgb(128, 0, 128),
                    Color.FromArgb(24, 128, 24),
                    Color.FromArgb(128, 128, 128),
                    Color.FromArgb(72, 72, 72),
                    Color.FromArgb(199, 8, 119), // Online lounge red/pink
                    Color.FromArgb(181, 186, 28),  // Encyclopedia yellow
                    Color.FromArgb(78, 166, 171), // AI battle mode blue
                    Color.FromArgb(230, 230, 230), // Player stats white
                    Color.FromArgb(40, 40, 40), // Chart editor black
                    Color.FromArgb(120, 104, 56), // Toolbox brown
                };

                #endregion

                #region [Return points]

                CStageタイトル.E戻り値[] __rps =
                {
                    CStageタイトル.E戻り値.GAMESTART,
                    CStageタイトル.E戻り値.DANGAMESTART,
                    CStageタイトル.E戻り値.TAIKOTOWERSSTART,
                    CStageタイトル.E戻り値.SHOPSTART,
                    CStageタイトル.E戻り値.BOUKENSTART,
                    CStageタイトル.E戻り値.HEYA,
                    CStageタイトル.E戻り値.CONFIG,
                    CStageタイトル.E戻り値.EXIT,
                    CStageタイトル.E戻り値.ONLINELOUNGE,
                    CStageタイトル.E戻り値.ENCYCLOPEDIA,
                    CStageタイトル.E戻り値.AIBATTLEMODE,
                    CStageタイトル.E戻り値.PLAYERSTATS,
                    CStageタイトル.E戻り値.CHARTEDITOR,
                    CStageタイトル.E戻り値.TOOLBOX,
                };

                #endregion

                #region [Extra bools]

                bool[] _1PRestricts =
                {
                    false,
                    true,
                    true,
                    false,
                    true,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    true,
                    false,
                };

                // To edit while new features are implemented
                bool[] _implemented =
                {
                    true,
                    true,
                    true,
                    false,
                    false,
                    true,
                    true,
                    true,
                    true,
                    true,
                    true,
                    false,
                    false,
                    false,
                };

                #endregion

                for (int i = 0; i < __MenuCount; i++)
                {
                    CStageタイトル.E戻り値 _rp = (i >= __rps.Length) ? CStageタイトル.E戻り値.GAMESTART : __rps[i];
                    Color _mc = (i >= __MenuColors.Length) ? Color.White :__MenuColors[i];
                    bool _1pr = (i >= _1PRestricts.Length) ? false : _1PRestricts[i];
                    bool _impl = (i >= _implemented.Length) ? false : _implemented[i];

                    __Menus[i] = new CMainMenuTab(i, _mc, tpf, boxpf, _rp, _1pr, _impl, modeSelect_Bar, modeSelect_Bar_Chara);
                    
                }
            }
            
        }

    }
}
