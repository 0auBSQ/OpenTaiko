using System;
using System.Collections.Generic;

namespace TJAPlayer3.C曲リストノードComparers
{
    internal sealed class C曲リストノードComparerノード種別 : IComparer<C曲リストノード>
    {
        public int Compare(C曲リストノード x, C曲リストノード y)
        {
            return ToComparable(x.eノード種別).CompareTo(ToComparable(y.eノード種別));
        }

        private static int ToComparable(C曲リストノード.Eノード種別 eノード種別)
        {
            switch (eノード種別)
            {
                case C曲リストノード.Eノード種別.BOX:
                    return 0;
                case C曲リストノード.Eノード種別.SCORE:
                case C曲リストノード.Eノード種別.SCORE_MIDI:
                    return 1;
                case C曲リストノード.Eノード種別.UNKNOWN:
                    return 2;
                case C曲リストノード.Eノード種別.RANDOM:
                    return 3;
                case C曲リストノード.Eノード種別.BACKBOX:
                    return 4;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
