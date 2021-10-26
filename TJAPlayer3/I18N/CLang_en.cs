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
                return "[!] Index not found in dictionnary";

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
            [9] = "Risky mode:\nSet over 1, in case you'd like to specify\n the number of Poor/Miss times to be\n FAILED.\nSet 0 to disable Risky mode.",
            [10] = "Song speed",
            [11] = "It changes the song speed.\n" +
                "For example, you can play in half\n" +
                " speed by setting PlaySpeed = 0.500\n" +
                " for your practice.\n" +
                "\n" +
                "Note: It also changes the songs' pitch.\n" +
                "In case TimeStretch=ON, some sound\n" +
                "lag occurs slower than x0.900.",
            [12] = "Reached floor",
            [13] = "F",
            [14] = "P",
            [15] = "Score",
            [16] = "Layout type",
            [17] = "You can change the layout type \ndisplayed on the song select screen.\n" +
                "0 : Regular (Up to down diagonal)\n" +
                "1 : Vertical\n" +
                "2 : Down to up diagonal\n" +
                "3 : Half-circle facing right\n" +
                "4 : Half-circle facing left",
            [18] = "Rhythm Game",
            [19] = "Exam Dojo",
            [20] = "Taiko towers",
            [21] = "Shop",
            [22] = "Taiko adventure",
            [23] = "Settings",
            [24] = "Exit",
        };
    }
}