using System;
using System.Collections.Generic;
using System.Text;
using FDK;

namespace TJAPlayer3
{
	public class CStage : CActivity
	{
		// プロパティ

		internal enum Eヒット判定
		{
			PERFECT,
			GREAT,
			GOOD,
			POOR,
			MISS,
			BAD,
			AUTO
		}

		internal Eステージ eステージID;
		public enum Eステージ
		{
			何もしない,
			起動,
			タイトル,	// Title screen
			オプション,
			コンフィグ,
			選曲,		// Ensou song select
			段位選択,	// Dan-i dojo menu
			曲読み込み,
			演奏,		// In-game
			結果,		// Result screen
			ChangeSkin,						// #28195 2011.5.4 yyagi
			Heya,
			TaikoTowers,
			BoukenTitle,
			BoukenMap,
			OnlineLounge,
			Encyclopedia,
			AIBattleMode,
			PlayerStats,
			ChartEditor,
			Toolbox,
			Template,			// No effect, for template class
			終了
		}
		
		internal Eフェーズ eフェーズID;
		public enum Eフェーズ
		{
			共通_通常状態,
			共通_フェードイン,
			共通_フェードアウト,
			共通_終了状態,
			起動0_システムサウンドを構築,
			起動00_songlistから曲リストを作成する,
			//起動1_SongsDBからスコアキャッシュを構築,
			起動2_曲を検索してリストを作成する,
			起動3_スコアキャッシュをリストに反映する,
			起動4_スコアキャッシュになかった曲をファイルから読み込んで反映する,
			起動5_曲リストへ後処理を適用する,
			//起動6_スコアキャッシュをSongsDBに出力する,
            起動_テクスチャの読み込み,
			起動7_完了,
			タイトル_起動画面からのフェードイン,
			選曲_結果画面からのフェードイン,
            選曲_コース選択画面へのフェードアウト, //2016.10.20 kairera0467
			選曲_NowLoading画面へのフェードアウト,
			NOWLOADING_DTXファイルを読み込む,
            NOWLOADING_WAV読み込み待機,
			NOWLOADING_WAVファイルを読み込む,
			NOWLOADING_BMPファイルを読み込む,
			NOWLOADING_システムサウンドBGMの完了を待つ,
			演奏_STAGE_FAILED,
			演奏_STAGE_FAILED_フェードアウト,
			演奏_STAGE_CLEAR_フェードアウト,
            演奏_演奏終了演出, //2016.07.15 kairera0467
			演奏_再読込
		}
	}
}
