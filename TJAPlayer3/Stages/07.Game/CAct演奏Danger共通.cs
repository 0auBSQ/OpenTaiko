using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using FDK;

namespace TJAPlayer3
{
	internal abstract class CAct演奏Danger共通 : CActivity
	{
		// コンストラクタ

		public CAct演奏Danger共通()
		{
			base.b活性化してない = true;
		}


		// CActivity 実装

		public override void On活性化()
		{
			for ( int i = 0; i < 3; i++ )
			{
				this.bDanger中[i] = false;
			}
//			this.ct移動用 = new CCounter();
//			this.ct透明度用 = new CCounter();
			this.ct移動用 = null;
			this.ct透明度用 = null;

			base.On活性化();
		}
		public override void On非活性化()
		{
			this.ct移動用 = null;
			this.ct透明度用 = null;
			base.On非活性化();
		}

		/// <summary>
		/// DANGER描画
		/// </summary>
		/// <param name="bIsDangerDrums">DrumsがDangerならtrue</param>
		/// <param name="bIsDamgerGuitar">GuitarがDangerならtrue</param>
		/// <param name="bIsDangerBass">BassがDangerならtrue</param>
		/// <returns></returns>
		public abstract int t進行描画( bool bIsDangerDrums, bool bIsDamgerGuitar, bool bIsDangerBass );



		// その他

		#region [ private ]
		//-----------------
		protected bool[] bDanger中 = { false, false, false};
		protected CCounter ct移動用;
		protected CCounter ct透明度用;
		//-----------------
		#endregion
	
	}
}
