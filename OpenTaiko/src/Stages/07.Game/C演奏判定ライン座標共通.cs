using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace TJAPlayer3
{
	/// <summary>
	/// 判定ライン関係の座標処理をまとめたクラス
	/// </summary>
	internal class C演奏判定ライン座標共通
	{
		/// <summary>
		/// 判定ラインのy座標
		/// </summary>
		private STDGBVALUE<int>[,,] n判定ラインY座標元データ = null;			// 補正無しの時の座標データ
		private STDGBVALUE<int>[,,] n演奏RGBボタンY座標元データ = null;

		/// <summary>
		/// 表示位置の補正データ
		/// 初期化は外部から行うこと。
		/// </summary>
		public STDGBVALUE<int> nJudgeLinePosY_delta;

		/// <summary>
		/// 判定ライン表示位置を、Vシリーズ互換にするかどうか。
		/// 設定は外部から行うこと。
		/// </summary>
		public STDGBVALUE<E判定位置> n判定位置;

		/// <summary>
		/// コンストラクタ(座標値の初期化)
		/// </summary>
		public C演奏判定ライン座標共通()
		{
			n判定ラインY座標元データ = new STDGBVALUE<int>[ 2, 2, 2 ];
			#region [ 判定ライン座標の初期化]
			// Normal, Drums画面, 判定ライン
			n判定ラインY座標元データ[ 0, 0, 0 ].Drums  = 0;		//未使用
			n判定ラインY座標元データ[ 0, 0, 0 ].Guitar = 95;
			n判定ラインY座標元データ[ 0, 0, 0 ].Bass   = 95;
			// Reverse, Drums画面, 判定ライン
			n判定ラインY座標元データ[ 1, 0, 0 ].Drums  = 0;		//未使用
			n判定ラインY座標元データ[ 1, 0, 0 ].Guitar = 374;
			n判定ラインY座標元データ[ 1, 0, 0 ].Bass   = 374;
			// Normal, Drums画面, Wailing枠
			n判定ラインY座標元データ[ 0, 0, 1 ].Drums  = 0;		//未使用
			n判定ラインY座標元データ[ 0, 0, 1 ].Guitar = 69;
			n判定ラインY座標元データ[ 0, 0, 1 ].Bass   = 69;
			// Reverse, Drums画面, Wailing枠
			n判定ラインY座標元データ[ 1, 0, 1 ].Drums  = 0;		//未使用
			n判定ラインY座標元データ[ 1, 0, 1 ].Guitar = 350;
			n判定ラインY座標元データ[ 1, 0, 1 ].Bass   = 350;

			// Normal, GR画面, 判定ライン
			n判定ラインY座標元データ[ 0, 1, 0 ].Drums  = 0;		//未使用
			n判定ラインY座標元データ[ 0, 1, 0 ].Guitar = 40;
			n判定ラインY座標元データ[ 0, 1, 0 ].Bass   = 40;
			// Reverse, GR画面, 判定ライン
			n判定ラインY座標元データ[ 1, 1, 0 ].Drums  = 0;		//未使用
			n判定ラインY座標元データ[ 1, 1, 0 ].Guitar = 369;
			n判定ラインY座標元データ[ 1, 1, 0 ].Bass   = 369;
			// Normal, GR画面, Wailing枠
			n判定ラインY座標元データ[ 0, 1, 1 ].Drums  = 0;		//未使用
			n判定ラインY座標元データ[ 0, 1, 1 ].Guitar = 11;
			n判定ラインY座標元データ[ 0, 1, 1 ].Bass   = 11;
			// Reverse, GR画面, Wailing枠
			n判定ラインY座標元データ[ 1, 1, 1 ].Drums  = 0;		//未使用
			n判定ラインY座標元データ[ 1, 1, 1 ].Guitar = 340;
			n判定ラインY座標元データ[ 1, 1, 1 ].Bass   = 340;
			#endregion

			n演奏RGBボタンY座標元データ = new STDGBVALUE<int>[ 2, 2, 2 ];
			#region [ RGBボタン座標の初期化]
			// Normal, Drums画面, RGBボタン
			n演奏RGBボタンY座標元データ[ 0, 0, 0 ].Drums  = 0;		// 未使用
			n演奏RGBボタンY座標元データ[ 0, 0, 0 ].Guitar = 57;
			n演奏RGBボタンY座標元データ[ 0, 0, 0 ].Bass   = 57;
			// Reverse, Drums画面, RGBボタン
			n演奏RGBボタンY座標元データ[ 1, 0, 0 ].Drums  = 0;		// 未使用
			n演奏RGBボタンY座標元データ[ 1, 0, 0 ].Guitar = 57;
			n演奏RGBボタンY座標元データ[ 1, 0, 0 ].Bass   = 57;
			// Normal, Drums画面, RGBボタン(Vシリーズ)
			n演奏RGBボタンY座標元データ[ 0, 0, 1 ].Drums  = 0;		// 未使用
			n演奏RGBボタンY座標元データ[ 0, 0, 1 ].Guitar = 107;
			n演奏RGBボタンY座標元データ[ 0, 0, 1 ].Bass   = 107;
			// Reverse, Drums画面, RGBボタン(Vシリーズ)
			n演奏RGBボタンY座標元データ[ 1, 0, 1 ].Drums  = 0;		// 未使用
			n演奏RGBボタンY座標元データ[ 1, 0, 1 ].Guitar = 107;
			n演奏RGBボタンY座標元データ[ 1, 0, 1 ].Bass   = 107;

			// Normal, GR画面, RGBボタン
			n演奏RGBボタンY座標元データ[ 0, 1, 0 ].Drums  = 0;		// 未使用
			n演奏RGBボタンY座標元データ[ 0, 1, 0 ].Guitar = 3;
			n演奏RGBボタンY座標元データ[ 0, 1, 0 ].Bass   = 3;
			// Reverse, GR画面, RGBボタン
			n演奏RGBボタンY座標元データ[ 1, 1, 0 ].Drums  = 0;		// 未使用
			n演奏RGBボタンY座標元データ[ 1, 1, 0 ].Guitar = 3;
			n演奏RGBボタンY座標元データ[ 1, 1, 0 ].Bass   = 3;
			// Normal, GR画面, RGBボタン(Vシリーズ)
			n演奏RGBボタンY座標元データ[ 0, 1, 1 ].Drums  = 0;		// 未使用
			n演奏RGBボタンY座標元データ[ 0, 1, 1 ].Guitar = 44;
			n演奏RGBボタンY座標元データ[ 0, 1, 1 ].Bass   = 44;
			// Reverse, GR画面, RGBボタン(Vシリーズ)
			n演奏RGBボタンY座標元データ[ 1, 1, 1 ].Drums  = 0;		// 未使用
			n演奏RGBボタンY座標元データ[ 1, 1, 1 ].Guitar = 44;
			n演奏RGBボタンY座標元データ[ 1, 1, 1 ].Bass   = 44;
			#endregion

			n判定位置 = new STDGBVALUE<E判定位置>();
			n判定位置.Drums  = E判定位置.標準;
			n判定位置.Guitar = E判定位置.標準;
			n判定位置.Bass   = E判定位置.標準;

			// 補正値は、Normal/Reverse, Drums/GR画面共通
			nJudgeLinePosY_delta.Drums  = 0;
			nJudgeLinePosY_delta.Guitar = 0;
			nJudgeLinePosY_delta.Bass   = 0;
		}


	
		/// <summary>
		/// 判定ラインのY座標を返す。とりあえずGuitar/Bassのみ対応。
		/// </summary>
		/// <param name="eInst">E楽器パート</param>
		/// <param name="bGRmode">GRmodeか否か</param>
		/// <param name="bReverse">Reverseか否か</param>
		/// <returns></returns>
		public int n判定ラインY座標( E楽器パート eInst, bool bGRmode, bool bReverse )
		{
			return n判定ラインY座標( eInst, bGRmode, bReverse, false );
		}

		public int n判定ラインY座標( E楽器パート eInst, bool bGRmode, bool bReverse, bool bWailingFrame )
		{
			return n判定ラインY座標( eInst, bGRmode, bReverse, bWailingFrame, false );
		}

		/// <summary>
		/// 判定ラインのY座標を返す。とりあえずGuitar/Bassのみ対応。
		/// </summary>
		/// <param name="eInst">E楽器パート</param>
		/// <param name="bGRmode">GRmodeか否か</param>
		/// <param name="bReverse">Reverseか否か</param>
		/// <param name="bWailingFrame">Wailing枠の座標か、判定ラインの座標か</param>
		/// <param name="b補正あり">プレーヤーのライン表示位置補正情報を加えるかどうか</param>
		/// <returns></returns>
		public int n判定ラインY座標( E楽器パート eInst, bool bGRmode, bool bReverse, bool bWailingFrame, bool b補正あり )
		{
			if ( eInst == E楽器パート.DRUMS )
			{
				throw new NotImplementedException();
			}
			else if ( eInst == E楽器パート.UNKNOWN )
			{
				throw new ArgumentOutOfRangeException();
			}
			else
			{
				int nReverse = bReverse ?      1 : 0;
				int nGRmode  = bGRmode ?       1 : 0;
				int nWailing = bWailingFrame ? 1 : 0;
				int nInst    = (int) eInst;
				int ret = this.n判定ラインY座標元データ[ nReverse, nGRmode, nWailing ][ nInst ];		// 補正無しの値
				if ( bReverse )
				{
					if ( n判定位置[ nInst ] == E判定位置.Lower ) ret += 13;
					if ( b補正あり )		ret += nJudgeLinePosY_delta[ nInst ];
				}
				else
				{
					if ( n判定位置[ nInst ] == E判定位置.Lower ) ret += 52;
					if ( b補正あり )		ret -= nJudgeLinePosY_delta[ nInst ];
				}
				return ret;
			}
		}

		public int n演奏RGBボタンY座標( E楽器パート eInst, bool bGRmode, bool bReverse )
		{
			if ( eInst == E楽器パート.DRUMS )
			{
				throw new NotImplementedException();
			}
			else if ( eInst == E楽器パート.UNKNOWN )
			{
				throw new ArgumentOutOfRangeException();
			}
			else
			{
				int nInst    = (int) eInst;
				int nGRmode  = bGRmode ?  1 : 0;
				int nReverse = bReverse ? 1 : 0;
				int nVseries = (int) n判定位置[ nInst ];
				int ret = n演奏RGBボタンY座標元データ[ nReverse, nGRmode, nVseries ][ nInst ];		// 補正無しの値

				return ret;
			}
		}
	}
}
