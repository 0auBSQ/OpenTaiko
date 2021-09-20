using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using SlimDX;
using FDK;

namespace TJAPlayer3
{
	internal abstract class CAct演奏チップファイアGB : CActivity
	{
		// コンストラクタ

		public CAct演奏チップファイアGB()
		{
			base.b活性化してない = true;
		}


		// メソッド

		public virtual void Start( int nLane, int n中央X, int n中央Y, C演奏判定ライン座標共通 演奏判定ライン座標 )
		{
		}

		public abstract void Start( int nLane, C演奏判定ライン座標共通 演奏判定ライン座標 );
//		public abstract void Start( int nLane );

		// CActivity 実装

		public override void On活性化()
		{
			base.On活性化();
		}
		public override void On非活性化()
		{
			base.On非活性化();
		}
		public override void OnManagedリソースの作成()
		{
			if( !base.b活性化してない )
			{
				base.OnManagedリソースの作成();
			}
		}
		public override void OnManagedリソースの解放()
		{
			if( !base.b活性化してない )
			{
				base.OnManagedリソースの解放();
			}
		}
		public override int On進行描画()
		{
			if( !base.b活性化してない )
			{
			}
			return 0;
		}


		// その他

		#region [ private ]
		//-----------------
		//private STDGBVALUE<int> nJudgeLinePosY_delta = new STDGBVALUE<int>();
		C演奏判定ライン座標共通 _演奏判定ライン座標 = new C演奏判定ライン座標共通();
		//-----------------
		#endregion
	}
}
