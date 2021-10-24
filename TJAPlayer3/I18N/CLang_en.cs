using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TJAPlayer3
{
    internal class CLang_en : ILang
    {
        string ILang.ConfigChangeLanguage()
        {
            return "Change the displayed language\ningame and within the menus.";
        }

        string ILang.ConfigChangeLanguageHead()
        {
            return "System language";
        }
    }
}