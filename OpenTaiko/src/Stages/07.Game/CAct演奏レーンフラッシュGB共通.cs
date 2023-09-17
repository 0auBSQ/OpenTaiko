using System;
using System.Collections.Generic;
using System.Text;
using FDK;

namespace TJAPlayer3
{
	internal class CAct演奏レーンフラッシュGB共通 : CActivity
	{
		// プロパティ

		// コンストラクタ

		public CAct演奏レーンフラッシュGB共通()
		{
			base.IsDeActivated = true;
		}


		// メソッド


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
	}
}
