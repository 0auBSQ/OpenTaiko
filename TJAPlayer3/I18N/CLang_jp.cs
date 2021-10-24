using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TJAPlayer3
{
    internal class CLang_jp : ILang
    {
        string ILang.GetString(int idx)
        {
            if (!dictionnary.ContainsKey(idx))
                return "[!] 辞書で求める指数を見つけられませんでした";

            return dictionnary[idx];
        }


        private static readonly Dictionary<int, string> dictionnary = new Dictionary<int, string>
        {
            [0] = "プレイ中やメニューの\n表示される言語を変更。",
            [1] = "システム言語",
            [2] = "<< 戻る",
            [3] = "左側のメニューに戻ります。",
            [4] = "曲データ再読込み",
            [5] = "曲データの一覧情報を取得し直します。",
        };
    }
}