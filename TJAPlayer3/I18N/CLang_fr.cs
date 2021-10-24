using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TJAPlayer3
{
    internal class CLang_fr : ILang
    {
        string ILang.GetString(int idx)
        {
            if (!dictionnary.ContainsKey(idx))
                return "[!] Index non trouvé dans le dictionnaire";

            return dictionnary[idx];
        }


        private static readonly Dictionary<int, string> dictionnary = new Dictionary<int, string>
        {
            [0] = "Changer la langue affichée\ndans les menus et en jeu.",
            [1] = "Langue du système",
            [2] = "<< Retour au menu",
            [3] = "Retour au menu principal.",
            [4] = "Recharger les sons",
            [5] = "Met à jour et récupère les\nmodifications effectuées sur\nla liste de sons.",
        };
    }
}