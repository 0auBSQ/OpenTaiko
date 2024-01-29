using System.Collections.Generic;

namespace TJAPlayer3.C曲リストノードComparers
{
    internal sealed class C曲リストノードComparerLevelIcon : IComparer<CSongListNode>
    {
        private readonly int _order;

        public C曲リストノードComparerLevelIcon(int order)
        {
            this._order = order;
        }

        public int Compare(CSongListNode n1, CSongListNode n2)
        {
            int _n1s = (n1.eノード種別 != CSongListNode.ENodeType.SCORE) ? 0 : 1;
            int _n2s = (n2.eノード種別 != CSongListNode.ENodeType.SCORE) ? 0 : 1;


            if (_n1s == 0 || _n2s == 0)
            {
                return 0;
            }
            return _order * _diffOf(n1).CompareTo(_diffOf(n2));
        }

        private int _diffOf(CSongListNode n1)
        {
            return (int)n1.nLevelIcon[TJAPlayer3.stageSongSelect.actSongList.tFetchDifficulty(n1)];
        }
    }
}