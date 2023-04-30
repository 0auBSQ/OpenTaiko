using System.Collections.Generic;

namespace TJAPlayer3.C曲リストノードComparers
{
    internal sealed class C曲リストノードComparerLevelIcon : IComparer<C曲リストノード>
    {
        private readonly int _order;

        public C曲リストノードComparerLevelIcon(int order)
        {
            this._order = order;
        }

        public int Compare(C曲リストノード n1, C曲リストノード n2)
        {
            int _n1s = (n1.eノード種別 != C曲リストノード.Eノード種別.SCORE) ? 0 : 1;
            int _n2s = (n2.eノード種別 != C曲リストノード.Eノード種別.SCORE) ? 0 : 1;


            if (_n1s == 0 || _n2s == 0)
            {
                return 0;
            }
            return _order * _diffOf(n1).CompareTo(_diffOf(n2));
        }

        private int _diffOf(C曲リストノード n1)
        {
            return (int)n1.nLevelIcon[TJAPlayer3.stage選曲.act曲リスト.tFetchDifficulty(n1)];
        }
    }
}