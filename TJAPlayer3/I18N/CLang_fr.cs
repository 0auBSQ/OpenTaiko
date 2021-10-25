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
            [6] = "Nombre de joueurs",
            [7] = "Change le nombre de joueurs en jeu：\nEn le mettant à 2, il est possible de\njouer à 2 en mode écran scindé.\nDisponible seulement pour le mode partie \nrapide.",
            [8] = "Mort subite",
            [9] = "Mode mort subite :\nSi 1 ou plus, spécifiez le nombre de \nnotes ratées maximales autorisées avant \nde perdre la partie.\nSi 0 le mode mort subite est désactivé.",
            [10] = "Vitesse générale",
            [11] = "Change le coefficient multiplicateur de \nla vitesse générale de la musique en jeu." +
                "Par exemple, vous pouvez la diviser par \n" +
                "2 en l'établissant à 0.500 \n" +
                "afin de vous entraîner plus serainement.\n" +
                "\n" +
                "Note: Cette option change aussi le ton de la musique.\n" +
                "Si TimeStretch=ON, il peut y avoir du\n" +
                "lag si la vitesse générale est inférieure à x0.900.",
            [12] = "Étage atteint",
            [13] = "",
            [14] = "P",
            [15] = "Score",
        };
    }
}