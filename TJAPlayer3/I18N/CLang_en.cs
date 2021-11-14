using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TJAPlayer3
{
    internal class CLang_en : ILang
    {
        string ILang.GetString(int idx)
        {
            if (!dictionnary.ContainsKey(idx))
                return "[!] Index not found in dictionary";

            return dictionnary[idx];
        }


        private static readonly Dictionary<int, string> dictionnary = new Dictionary<int, string>
        {
            [0] = "Change the displayed language\ningame and within the menus.",
            [1] = "System language",
            [2] = "<< Return to Menu",
            [3] = "Return to left menu.",
            [4] = "Reload song data",
            [5] = "Retrieve and update the song list.",
            [6] = "Player count",
            [7] = "Change the ingame player countF\nSetting it to 2 makes able to play\nregular charts at 2 players by splitting \nthe screen in half.",
            [8] = "Risky",
            [9] = "Risky mode:\nSet it over 1, in case you'd like to specify\n the number of Poor/Miss times to be\n FAILED.\nSet 0 to disable Risky mode.",
            [10] = "Song speed",
            [11] = "It changes the song speed.\n" +
                "For example, you can play in half\n" +
                " speed by setting PlaySpeed = 0.500\n" +
                " for your practice.\n" +
                "\n" +
                "Note: It also changes the songs' pitch.\n" +
                "In case TimeStretch=ON, some audio\n" +
                "lag occurs if slower than x0.900.",
            [16] = "Layout type",
            [17] = "You can change the layout of the songs \ndisplayed on the song select screen.\n" +
                "0 : Regular (Up to down diagonal)\n" +
                "1 : Vertical\n" +
                "2 : Down to up diagonal\n" +
                "3 : Half-circle facing right\n" +
                "4 : Half-circle facing left",

            [100] = "Taiko Mode",
            [101] = "Dan-i Dojo",
            [102] = "Taiko Towers",
            [103] = "Shop",
            [104] = "Taiko Adventure",
            [105] = "My Room",
            [106] = "Settings",
            [107] = "Exit",

            [150] = "Play your favorite\nsongs at your own pace !",
            [151] = "Play multiple charts in continuation\nfollowing challenging exams\nin order to get a PASS rank !",
            [152] = "Play long charts within a limited\ncount of lives and reach\nthe top of the tower !",
            [153] = "Buy new songs, petit-chara or characters\nusing the medals you earned in game !",
            [154] = "Surpass various obstacles and\nunlock new content and horizons !",
            [155] = "Change your nameplate info\n or your character visuals !",
            [156] = "Change your game style\n or general settings !",
            [157] = "Quit the game.\nSee you next time !",

            [200] = "Return",
            [201] = "Recently played songs",
            [202] = "Play recently played songs !",

            [1000] = "Reached floor",
            [1001] = "F",
            [1002] = "P",
            [1003] = "Score",

            [1010] = "Soul gauge",
            [1011] = "Perfect count",
            [1012] = "Good count",
            [1013] = "Bad count",
            [1014] = "Score",
            [1015] = "Rolls count",
            [1016] = "Hit count",
            [1017] = "Combo",
            [1018] = "Accuracy",

            [1030] = "Return",
            [1031] = "Petit-Chara",
            [1032] = "Character",
            [1033] = "Dan Title",
            [1034] = "Nameplate Title",
        };
    }
}
