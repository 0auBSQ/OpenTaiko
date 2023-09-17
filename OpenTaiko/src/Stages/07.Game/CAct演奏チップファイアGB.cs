using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using FDK;

namespace TJAPlayer3
{
	internal abstract class CAct演奏チップファイアGB : CActivity
	{
		// コンストラクタ

		public CAct演奏チップファイアGB()
		{
			base.IsDeActivated = true;
		}


		// メソッド

		public virtual void Start( int nLane, int n中央X, int n中央Y, C演奏判定ライン座標共通 演奏判定ライン座標 )
		{
		}

		public abstract void Start( int nLane, C演奏判定ライン座標共通 演奏判定ライン座標 );
//		public abstract void Start( int nLane );

		// CActivity 実装

		public override void Activate()
		{
			base.Activate();
		}
		public override void DeActivate()
		{
			base.DeActivate();
		}
		public override void CreateManagedResource()
		{
			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource()
		{
			base.ReleaseManagedResource();
		}
		public override int Draw()
		{
			if( !base.IsDeActivated )
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
