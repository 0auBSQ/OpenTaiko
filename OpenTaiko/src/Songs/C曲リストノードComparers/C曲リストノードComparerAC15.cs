using System.Collections.Generic;

namespace TJAPlayer3.C曲リストノードComparers
{
    internal sealed class C曲リストノードComparerAC15 : IComparer<CSongListNode>
    {
        public int Compare(CSongListNode n1, CSongListNode n2)
        {
            return CStrジャンルtoNum.ForAC15(n1.strジャンル).CompareTo(CStrジャンルtoNum.ForAC15(n2.strジャンル));
        }
    }
}