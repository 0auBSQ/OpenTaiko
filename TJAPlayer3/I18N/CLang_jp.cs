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
            return "�v���C���⃁�j���[��\n�\������錾���ύX�B";
        }

        string ILang.ConfigChangeLanguageHead()
        {
            return "�V�X�e������";
        }
    }
}