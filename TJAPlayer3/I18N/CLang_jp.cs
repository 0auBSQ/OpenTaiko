using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TJAPlayer3
{
    internal class CLang_jp : ILang
    {
        string ILang.ConfigChangeLanguage()
        {
            return "プレイ中やメニューの\n表示される言語を変更。";
        }

        string ILang.ConfigChangeLanguageHead()
        {
            return "システム言語";
        }
    }
}