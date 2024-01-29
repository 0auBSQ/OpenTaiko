using System.Collections.Generic;

namespace TJAPlayer3.C曲リストノードComparers
{
    internal sealed class C曲リストノードComparerAC8_14 : IComparer<CSongListNode>
    {
        public int Compare(CSongListNode n1, CSongListNode n2)
        {
            return CStrジャンルtoNum.ForAC8_14(n1.strジャンル).CompareTo(CStrジャンルtoNum.ForAC8_14(n2.strジャンル));
        }
    }
}