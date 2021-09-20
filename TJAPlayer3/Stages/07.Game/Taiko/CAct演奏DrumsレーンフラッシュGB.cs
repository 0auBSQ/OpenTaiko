using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using FDK;

namespace TJAPlayer3
{
	internal class CAct演奏DrumsレーンフラッシュGB : CAct演奏レーンフラッシュGB共通
	{
		// CActivity 実装（共通クラスからの差分のみ）

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
		//-----------------
		#endregion
	}
}
