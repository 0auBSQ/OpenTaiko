using System.Collections.Generic;

namespace TJAPlayer3.C曲リストノードComparers
{
    internal sealed class C曲リストノードComparer絶対パス : IComparer<CSongListNode>
    {
        private readonly int _order;

        public C曲リストノードComparer絶対パス(int order)
        {
            this._order = order;
        }

        public int Compare(CSongListNode n1, CSongListNode n2)
        {
            if( ( n1.eノード種別 == CSongListNode.ENodeType.BOX ) && ( n2.eノード種別 == CSongListNode.ENodeType.BOX ) )
            {
                return _order * n1.arスコア[ 0 ].ファイル情報.フォルダの絶対パス.CompareTo( n2.arスコア[ 0 ].ファイル情報.フォルダの絶対パス );
            }

            var str = strファイルの絶対パス(n1);
            var strB = strファイルの絶対パス(n2);

            return _order * str.CompareTo( strB );
        }

        private static string strファイルの絶対パス(CSongListNode c曲リストノード)
        {
            for (int i = 0; i < (int)Difficulty.Total; i++)
            {
                if (c曲リストノード.arスコア[i] != null)
                {
                    return c曲リストノード.arスコア[i].ファイル情報.ファイルの絶対パス ?? "";
                }
            }

            return "";
        }
    }
}