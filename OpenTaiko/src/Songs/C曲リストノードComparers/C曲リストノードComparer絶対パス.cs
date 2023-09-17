using System.Collections.Generic;

namespace TJAPlayer3.C曲リストノードComparers
{
    internal sealed class C曲リストノードComparer絶対パス : IComparer<C曲リストノード>
    {
        private readonly int _order;

        public C曲リストノードComparer絶対パス(int order)
        {
            this._order = order;
        }

        public int Compare(C曲リストノード n1, C曲リストノード n2)
        {
            if( ( n1.eノード種別 == C曲リストノード.Eノード種別.BOX ) && ( n2.eノード種別 == C曲リストノード.Eノード種別.BOX ) )
            {
                return _order * n1.arスコア[ 0 ].ファイル情報.フォルダの絶対パス.CompareTo( n2.arスコア[ 0 ].ファイル情報.フォルダの絶対パス );
            }

            var str = strファイルの絶対パス(n1);
            var strB = strファイルの絶対パス(n2);

            return _order * str.CompareTo( strB );
        }

        private static string strファイルの絶対パス(C曲リストノード c曲リストノード)
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