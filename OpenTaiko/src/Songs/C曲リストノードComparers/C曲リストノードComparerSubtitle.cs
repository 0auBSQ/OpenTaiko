using System.Collections.Generic;

namespace TJAPlayer3.C曲リストノードComparers
{
    internal sealed class C曲リストノードComparerSubtitle : IComparer<C曲リストノード>
    {
        private readonly int _order;

        public C曲リストノードComparerSubtitle(int order)
        {
            this._order = order;
        }

        public int Compare(C曲リストノード n1, C曲リストノード n2)
        {
            return _order * n1.strサブタイトル.CompareTo(n2.strサブタイトル);
        }
    }
}