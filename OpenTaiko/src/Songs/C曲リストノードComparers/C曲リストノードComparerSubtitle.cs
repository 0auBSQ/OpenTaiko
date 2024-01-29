﻿using System.Collections.Generic;

namespace TJAPlayer3.C曲リストノードComparers
{
    internal sealed class C曲リストノードComparerSubtitle : IComparer<CSongListNode>
    {
        private readonly int _order;

        public C曲リストノードComparerSubtitle(int order)
        {
            this._order = order;
        }

        public int Compare(CSongListNode n1, CSongListNode n2)
        {
            return _order * n1.strサブタイトル.CompareTo(n2.strサブタイトル);
        }
    }
}