using System.Collections.Generic;

namespace TJAPlayer3.C曲リストノードComparers
{
    internal sealed class C曲リストノードComparerタイトル : IComparer<C曲リストノード>
    {
        private readonly int _order;

        public C曲リストノードComparerタイトル(int order)
        {
            this._order = order;
        }

        public int Compare(C曲リストノード n1, C曲リストノード n2)
        {
            return _order * n1.strタイトル.CompareTo( n2.strタイトル );
        }
    }
}