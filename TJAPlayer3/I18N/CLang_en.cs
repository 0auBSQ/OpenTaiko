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
        };
    }
}