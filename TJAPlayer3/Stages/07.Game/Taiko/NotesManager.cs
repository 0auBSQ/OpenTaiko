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

        public static bool IsSmallNote(CDTX.CChip chip, bool blue)
        {
            return blue ? chip.nチャンネル番号 == 0x12 : chip.nチャンネル番号 == 0x11;
        }

        public static bool IsBigKaTaiko(CDTX.CChip chip, EGameType gt)
        {
            return (chip.nチャンネル番号 == 0x14 || chip.nチャンネル番号 == 0x1B) && gt == EGameType.TAIKO;
        }

        public static bool IsBigDonTaiko(CDTX.CChip chip, EGameType gt)
        {
            return (chip.nチャンネル番号 == 0x13 || chip.nチャンネル番号 == 0x1A) && gt == EGameType.TAIKO;
        }

        public static bool IsClapKonga(CDTX.CChip chip, EGameType gt)
        {
            return (chip.nチャンネル番号 == 0x14 || chip.nチャンネル番号 == 0x1B) && gt == EGameType.KONGA;
        }

        public static bool IsSwapNote(CDTX.CChip chip, EGameType gt)
        {
            return (
                ((chip.nチャンネル番号 == 0x13 || chip.nチャンネル番号 == 0x1A) && gt == EGameType.KONGA)        // Konga Pink note
                || IsPurpleNote(chip, gt)                                                                      // Purple (Green) note
                );
        }

        // Not implemented yet
        public static bool IsPurpleNote(CDTX.CChip chip, EGameType gt)
        {
            return false;
        }


    }
}
