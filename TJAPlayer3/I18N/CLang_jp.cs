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
            [6] = "�v���C�l��",
            [7] = "�v���C�l���؂�ւ��F\n2�ɂ���Ɖ��t��ʂ�2�l�v���C��p�̃��C�A�E�g�ɂȂ�A\n2P��p���ʂ�ǂݍ��ނ悤�ɂȂ�܂��B",
            [8] = "Risky",
            [9] = "Risky���[�h�̐ݒ�:\n1�ȏ�̒l�ɂ���ƁA���̉񐔕���\nPoor/Miss��FAILED�ƂȂ�܂��B\n0�ɂ���Ɩ����ɂȂ�A\nDamageLevel�ɏ]�����Q�[�W������\n�Ȃ�܂��B\nStageFailed�̐ݒ�ƕ��p�ł��܂��B",
            [10] = "�Đ����x",
            [11] = "�Ȃ̉��t���x���A����������x������\n" +
                "�肷�邱�Ƃ��ł��܂��B\n" +
                "�i���ꕔ�̃T�E���h�J�[�h�ł͐�����\n" +
                "�@�Đ��ł��Ȃ��\��������܂��B�j\n" +
                "\n" +
                "TimeStretch��ON�̂Ƃ��ɁA���t\n" +
                "���x��x0.850�ȉ��ɂ���ƁA�`�b�v��\n" +
                "�Y�����傫���Ȃ�܂��B",
            [12] = "���B�K��",
            [13] = "�K",
            [14] = "�_",
            [15] = "�X�R�A",
            [16] = "�I�ȉ�ʐ݌v",
            [17] = "�I�ȉ�ʂ̐݌v�̂̕ύX���ł��܂��B\n" +
                "�O�����ʏ�̐݌v�i�㉺�΁j\n" +
                "�P��������\n" +
                "�Q���������\n" +
                "�R�����E��������\n" +
                "�S��������������",
        };
    }
}