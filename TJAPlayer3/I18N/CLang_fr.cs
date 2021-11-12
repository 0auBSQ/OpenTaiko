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
            [16] = "Disposition des blocs",
            [17] = "Cette option détermine l'ordonnancement \ndes blocs dans le menu de selection \n des musiques en mode partie rapide.\n" +
                "0 : Standard (Diagonale haut-bas)\n" +
                "1 : Vertical\n" +
                "2 : Diagonale bas-haut\n" +
                "3 : Demi-cercle orienté à droite\n" +
                "4 : Demi-cercle orienté à gauche",

            [100] = "Partie rapide",
            [101] = "Défis du Dojo",
            [102] = "Tours rhytmiques",
            [103] = "Magasin",
            [104] = "Aventure",
            [105] = "Salon",
            [106] = "Paramètres",
            [107] = "Quitter le jeu",

            [150] = "Jouez vos sons favoris\nà votre propre rhythme !",
            [151] = "Jouez plusieurs sons à la suite\nen suivant des règles exigentes\ndans le but de reussir le défi !",
            [152] = "Jouez de longs sons avec un\nnombre de vies limité et\natteignez le sommet de la tour !",
            [153] = "Achetez de nouveaux sons ou personnages\ngrâce aux médailles acquises en jeu !",
            [154] = "Surmontez une multitude d'obstables\nafin de découvrir du nouveau contenu\net de nouveaux horizons !",
            [155] = "Changez votre personnage\nou les informations de votre\nplaque nominative !",
            [156] = "Changez votre style de jeu\n ou les paramètres généraux !",
            [157] = "Quitter le jeu.\nÀ bientôt !",

            [1000] = "Étage atteint",
            [1001] = "",
            [1002] = "P",
            [1003] = "Score",

            [1010] = "Jauge d'âme",
            [1011] = "Nombre de Parfait",
            [1012] = "Nombre de Bon",
            [1013] = "Nombre de Mauvais",
            [1014] = "Score",
            [1015] = "Frappes successives",
            [1016] = "Nombre de frappes",
            [1017] = "Combo",
            [1018] = "Précision",
        };
    }
}