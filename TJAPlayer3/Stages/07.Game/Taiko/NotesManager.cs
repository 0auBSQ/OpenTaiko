using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{

    // Simple class containing functions to simplify readability of CChip elements
    class NotesManager
    {
        public static Dictionary<string, int> NoteCorrespondanceDictionnary = new Dictionary<string, int>()
        {
            ["0"] = 0, // Empty
            ["1"] = 1, // Small Don (Taiko) | Red (right) hit (Konga)
            ["2"] = 2, // Small Ka (Taiko) | Yellow (left) hit (Konga)
            ["3"] = 3, // Big Don (Taiko) | Pink note (Konga)
            ["4"] = 4, // Big Ka (Taiko) | Clap (Konga)
            ["5"] = 5, // Small roll start
            ["6"] = 6, // Big roll start
            ["7"] = 7, // Balloon
            ["8"] = 8, // Roll/Balloon end
            ["9"] = 7, // Kusudama (Currently treated as balloon)
            ["A"] = 10, // Joint Big Don (2P)
            ["B"] = 11, // Joint Big Ka (2P)
            ["C"] = 0, // Mine (Coming soon)
            ["D"] = 0, // Unused
            ["E"] = 6, // Konga clap roll (Coming soon)
            ["F"] = 15, // ADLib
            ["G"] = 3, // Green (Purple) double hit note (Coming soon)
            ["H"] = 5, // Konga red roll (Coming soon)
            ["I"] = 5, // Konga yellow roll (Coming soon)
        };

        public static int GetNoteValueFromChar(string chr)
        {
            if (NoteCorrespondanceDictionnary.ContainsKey(chr))
                return NoteCorrespondanceDictionnary[chr];
            return -1;
        }

        public static bool IsExpectedPad(int stored, int hit, CDTX.CChip chip, EGameType gt)
        {
            var inPad = (Eパッド)hit;
            var onPad = (Eパッド)stored;

            if (chip == null) return false;

            if (IsBigKaTaiko(chip, gt))
            {
                return (inPad == Eパッド.LBlue && onPad == Eパッド.RBlue)
                    || (inPad == Eパッド.RBlue && onPad == Eパッド.LBlue);
            }

            if (IsBigDonTaiko(chip, gt))
            {
                return (inPad == Eパッド.LRed && onPad == Eパッド.RRed)
                    || (inPad == Eパッド.RRed && onPad == Eパッド.LRed);
            }

            if (IsSwapNote(chip, gt))
            {
                bool hitBlue = inPad == Eパッド.LBlue || inPad == Eパッド.RBlue;
                bool hitRed = inPad == Eパッド.LRed || inPad == Eパッド.RRed;
                bool storedBlue = onPad == Eパッド.LBlue || onPad == Eパッド.RBlue;
                bool storedRed = onPad == Eパッド.LRed || onPad == Eパッド.RRed;

                return (storedRed && hitBlue)
                    || (storedBlue && hitRed);
            }

            return false;
        }

        public static bool IsCommonNote(CDTX.CChip chip)
        {
            if (chip == null) return false;
            return chip.nチャンネル番号 >= 0x11 && chip.nチャンネル番号 < 0x18;
        }
        public static bool IsMine(CDTX.CChip chip)
        {
            if (chip == null) return false;
            return chip.nチャンネル番号 == 0x1C;
        }

        public static bool IsSmallNote(CDTX.CChip chip, bool blue)
        {
            if (chip == null) return false;
            return blue ? chip.nチャンネル番号 == 0x12 : chip.nチャンネル番号 == 0x11;
        }

        public static bool IsBigKaTaiko(CDTX.CChip chip, EGameType gt)
        {
            if (chip == null) return false;
            return (chip.nチャンネル番号 == 0x14 || chip.nチャンネル番号 == 0x1B) && gt == EGameType.TAIKO;
        }

        public static bool IsBigDonTaiko(CDTX.CChip chip, EGameType gt)
        {
            if (chip == null) return false;
            return (chip.nチャンネル番号 == 0x13 || chip.nチャンネル番号 == 0x1A) && gt == EGameType.TAIKO;
        }

        public static bool IsClapKonga(CDTX.CChip chip, EGameType gt)
        {
            if (chip == null) return false;
            return (chip.nチャンネル番号 == 0x14 || chip.nチャンネル番号 == 0x1B) && gt == EGameType.KONGA;
        }

        public static bool IsSwapNote(CDTX.CChip chip, EGameType gt)
        {
            if (chip == null) return false;
            return (
                ((chip.nチャンネル番号 == 0x13 || chip.nチャンネル番号 == 0x1A) && gt == EGameType.KONGA)        // Konga Pink note
                || IsPurpleNote(chip, gt)                                                                      // Purple (Green) note
                );
        }

        // Not implemented yet
        public static bool IsPurpleNote(CDTX.CChip chip, EGameType gt)
        {
            if (chip == null) return false;
            return false;
        }


    }
}
