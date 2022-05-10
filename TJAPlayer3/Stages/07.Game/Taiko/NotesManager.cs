using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using FDK;

namespace TJAPlayer3
{

    // Simple class containing functions to simplify readability of CChip elements
    class NotesManager
    {

        #region [Parsing]

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
            ["C"] = 12, // Mine (Coming soon)
            ["D"] = 0, // Unused
            ["E"] = 5, // Konga clap roll (Coming soon)
            ["F"] = 15, // ADLib
            ["G"] = 0xF1, // Green (Purple) double hit note (Coming soon)
            ["H"] = 5, // Konga red roll (Coming soon)
            ["I"] = 5, // Konga yellow roll (Coming soon)
        };

        public static int GetNoteValueFromChar(string chr)
        {
            if (NoteCorrespondanceDictionnary.ContainsKey(chr))
                return NoteCorrespondanceDictionnary[chr];
            return -1;
        }

        #endregion

        #region [Gameplay]

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

        #endregion

        #region [General]

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

        public static bool IsSmallNote(CDTX.CChip chip)
        {
            if (chip == null) return false;
            return chip.nチャンネル番号 == 0x12 || chip.nチャンネル番号 == 0x11;
        }

        public static bool IsBigNote(CDTX.CChip chip)
        {
            if (chip == null) return false;
            return (chip.nチャンネル番号 == 0x13 || chip.nチャンネル番号 == 0x14 || chip.nチャンネル番号 == 0x1A || chip.nチャンネル番号 == 0x1B);
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
                IsKongaPink(chip, gt)                           // Konga Pink note
                || IsPurpleNote(chip)                       // Purple (Green) note
                );
        }

        public static bool IsKongaPink(CDTX.CChip chip, EGameType gt)
        {
            if (chip == null) return false;
            // Purple notes are treated as Pink in Konga
            return (chip.nチャンネル番号 == 0x13 || chip.nチャンネル番号 == 0x1A || IsPurpleNote(chip)) && gt == EGameType.KONGA;
        }
        public static bool IsPurpleNote(CDTX.CChip chip)
        {
            if (chip == null) return false;
            return (chip.nチャンネル番号 == 0x101);
        }

        public static bool IsKusudama(CDTX.CChip chip)
        {
            if (chip == null) return false;
            return chip.nチャンネル番号 == 0x19;
        }

        public static bool IsRollEnd(CDTX.CChip chip)
        {
            if (chip == null) return false;
            return chip.nチャンネル番号 == 0x18;
        }

        public static bool IsBalloon(CDTX.CChip chip)
        {
            if (chip == null) return false;
            return chip.nチャンネル番号 == 0x17;
        }

        public static bool IsBigRoll(CDTX.CChip chip)
        {
            if (chip == null) return false;
            return chip.nチャンネル番号 == 0x16;
        }

        public static bool IsSmallRoll(CDTX.CChip chip)
        {
            if (chip == null) return false;
            return chip.nチャンネル番号 == 0x15;
        }

        public static bool IsADLIB(CDTX.CChip chip)
        {
            if (chip == null) return false;
            return chip.nチャンネル番号 == 0x1F;
        }

        public static bool IsRoll(CDTX.CChip chip)
        {
            if (chip == null) return false;
            return IsBigRoll(chip) || IsSmallRoll(chip);
        }

        public static bool IsGenericRoll(CDTX.CChip chip)
        {
            if (chip == null) return false;
            return 0x15 <= chip.nチャンネル番号 && chip.nチャンネル番号 <= 0x19;
        }

        public static bool IsMissableNote(CDTX.CChip chip)
        {
            if (chip == null) return false;
            return (0x11 <= chip.nチャンネル番号 && chip.nチャンネル番号 <= 0x14) 
                || chip.nチャンネル番号 == 0x1A 
                || chip.nチャンネル番号 == 0x1B
                || chip.nチャンネル番号 == 0x101;
        }

        public static bool IsHittableNote(CDTX.CChip chip)
        {
            if (chip == null) return false;
            return IsMissableNote(chip)
                || IsGenericRoll(chip)
                || IsADLIB(chip)
                || IsMine(chip);
        }

        #endregion

        #region [Displayables]

        // Flying notes
        public static void DisplayNote(int player, int x, int y, int Lane)
        {
            EGameType _gt = TJAPlayer3.ConfigIni.nGameType[TJAPlayer3.GetActualPlayer(player)];

            TJAPlayer3.Tx.Notes[(int)_gt]?.t2D中心基準描画(TJAPlayer3.app.Device, x, y, new Rectangle(Lane * 130, 390, 130, 130));
        }

        // Regular display
        public static void DisplayNote(int player, int x, int y, CDTX.CChip chip, int frame, int length = 130)
        {
            if (TJAPlayer3.ConfigIni.eSTEALTH != Eステルスモード.OFF || !chip.bShow)
                return;

            EGameType _gt = TJAPlayer3.ConfigIni.nGameType[TJAPlayer3.GetActualPlayer(player)];

            int noteType = 1;
            if (IsSmallNote(chip, true)) noteType = 2;
            else if (IsBigDonTaiko(chip, _gt) || IsKongaPink(chip, _gt)) noteType = 3;
            else if (IsBigKaTaiko(chip, _gt) || IsClapKonga(chip, _gt)) noteType = 4;
            else if (IsBalloon(chip)) noteType = 11;

            else if (IsMine(chip))
            {
                TJAPlayer3.Tx.Note_Mine?.t2D描画(TJAPlayer3.app.Device, x, y);
                return;
            }
            else if (IsPurpleNote(chip))
            {
                if (TJAPlayer3.Tx.Notes[0] != null)
                {
                    int _oldOp = TJAPlayer3.Tx.Notes[0].Opacity;
                    TJAPlayer3.Tx.Notes[0]?.t2D描画(TJAPlayer3.app.Device, x, y, new Rectangle(130, frame, length, 130));
                    TJAPlayer3.Tx.Notes[0].Opacity = 127;
                    TJAPlayer3.Tx.Notes[0]?.t2D描画(TJAPlayer3.app.Device, x, y, new Rectangle(260, frame, length, 130));
                    TJAPlayer3.Tx.Notes[0].Opacity = _oldOp;
                }
                return;
            }

            TJAPlayer3.Tx.Notes[(int)_gt]?.t2D描画(TJAPlayer3.app.Device, x, y, new Rectangle(noteType * 130, frame, length, 130));
        }

        // Roll display
        public static void DisplayRoll(int player, int x, int y, CDTX.CChip chip, int frame, 
            SharpDX.Color4 normalColor, SharpDX.Color4 effectedColor, int x末端)
        {
            EGameType _gt = TJAPlayer3.ConfigIni.nGameType[TJAPlayer3.GetActualPlayer(player)];

            if (TJAPlayer3.ConfigIni.eSTEALTH != Eステルスモード.OFF || !chip.bShow || TJAPlayer3.Tx.Notes[(int)_gt] == null)
                return;

            int _offset = IsBigRoll(chip) ? 390 : 0;
            float _adjust = 65f;
            int index = x末端 - x;

            if (TJAPlayer3.Skin.Game_RollColorMode != CSkin.RollColorMode.None)
                TJAPlayer3.Tx.Notes[(int)_gt].color4 = effectedColor;
            else
                TJAPlayer3.Tx.Notes[(int)_gt].color4 = normalColor;
            TJAPlayer3.Tx.Notes[(int)_gt].vc拡大縮小倍率.X = (index - 65.0f + _adjust + 1) / 128.0f;
            TJAPlayer3.Tx.Notes[(int)_gt].t2D描画(TJAPlayer3.app.Device, x + 64, y, new Rectangle(781 + _offset, 0, 128, 130));
            TJAPlayer3.Tx.Notes[(int)_gt].vc拡大縮小倍率.X = 1.0f;
            TJAPlayer3.Tx.Notes[(int)_gt].t2D描画(TJAPlayer3.app.Device, x末端 + _adjust, y, 0, new Rectangle(910 + _offset, frame, 130, 130));
            if (TJAPlayer3.Skin.Game_RollColorMode == CSkin.RollColorMode.All)
                TJAPlayer3.Tx.Notes[(int)_gt].color4 = effectedColor;
            else
                TJAPlayer3.Tx.Notes[(int)_gt].color4 = normalColor;

            TJAPlayer3.Tx.Notes[(int)_gt].t2D描画(TJAPlayer3.app.Device, x, y, 0, new Rectangle(650 + _offset, frame, 130, 130));
            TJAPlayer3.Tx.Notes[(int)_gt].color4 = normalColor;
        }


        #endregion

    }
}
