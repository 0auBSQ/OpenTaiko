using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TJAPlayer3
{
    internal class CLang_jp : ILang
    {
        string ILang.GetString(int idx)
        {
            if (!dictionnary.ContainsKey(idx))
                return "[!] �����ŋ��߂�w�����������܂���ł���";

            return dictionnary[idx];
        }


        private static readonly Dictionary<int, string> dictionnary = new Dictionary<int, string>
        {
            [0] = "�v���C���⃁�j���[��\n�\������錾���ύX�B",
            [1] = "�V�X�e������",
            [2] = "<< �߂�",
            [3] = "�����̃��j���[�ɖ߂�܂��B",
            [4] = "�ȃf�[�^�ēǍ���",
            [5] = "�ȃf�[�^�̈ꗗ�����擾�������܂��B",
        };
    }
}