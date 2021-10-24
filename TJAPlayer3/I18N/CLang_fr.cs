using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TJAPlayer3
{
    internal class CLang_fr : ILang
    {
        string ILang.ConfigChangeLanguage()
        {
            return "Changer la langue affichée\ndans les menus et en jeu.";
        }

        string ILang.ConfigChangeLanguageHead()
        {
            return "Langue du système";
        }
    }
}