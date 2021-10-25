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
            [6] = "プレイ人数",
            [7] = "プレイ人数切り替え：\n2にすると演奏画面が2人プレイ専用のレイアウトになり、\n2P専用譜面を読み込むようになります。",
            [8] = "Risky",
            [9] = "Riskyモードの設定:\n1以上の値にすると、その回数分の\nPoor/MissでFAILEDとなります。\n0にすると無効になり、\nDamageLevelに従ったゲージ増減と\nなります。\nStageFailedの設定と併用できます。",
            [10] = "再生速度",
            [11] = "曲の演奏速度を、速くしたり遅くした\n" +
                "りすることができます。\n" +
                "（※一部のサウンドカードでは正しく\n" +
                "　再生できない可能性があります。）\n" +
                "\n" +
                "TimeStretchがONのときに、演奏\n" +
                "速度をx0.850以下にすると、チップの\n" +
                "ズレが大きくなります。",
            [12] = "到達階数",
            [13] = "階",
            [14] = "点",
            [15] = "スコア",
            [16] = "選曲画面設計",
            [17] = "選曲画面の設計のの変更ができます。\n" +
                "０＝＞通常の設計（上下斜）\n" +
                "１＝＞垂直\n" +
                "２＝＞下上斜\n" +
                "３＝＞右向け半丸\n" +
                "４＝＞左向け半丸",
        };
    }
}