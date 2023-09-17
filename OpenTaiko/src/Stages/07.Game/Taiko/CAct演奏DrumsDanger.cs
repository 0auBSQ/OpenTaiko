using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using FDK;

namespace TJAPlayer3
{
	internal class CAct演奏DrumsDanger : CAct演奏Danger共通
	{
		// コンストラクタ

		//public CAct演奏DrumsDanger()
		//{
		//    base.b活性化してない = true;
		//}


		// CActivity 実装

		//public override void On活性化()
		//{
		//    this.bDanger中 = false;
		//    this.ct移動用 = new CCounter();
		//    this.ct透明度用 = new CCounter();
		//    base.On活性化();
		//}
		//public override void On非活性化()
		//{
		//    this.ct移動用 = null;
		//    this.ct透明度用 = null;
		//    base.On非活性化();
		//}
		public override void CreateManagedResource()
		{
			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource()
		{
			TJAPlayer3.tテクスチャの解放( ref this.txDANGER );
			base.ReleaseManagedResource();
		}
		public override int Draw()
		{
			throw new InvalidOperationException( "t進行描画(bool)のほうを使用してください。" );
		}
		/// <summary>
		/// ドラム画面のDANGER描画
		/// </summary>
		/// <param name="bIsDangerDrums">DrumsのゲージがDangerかどうか(Guitar/Bassと共用のゲージ)</param>
		/// <param name="bIsDangerGuitar">Guitarのゲージ(未使用)</param>
		/// <param name="bIsDangerBass">Bassのゲージ(未使用)</param>
		/// <returns></returns>
		public override int t進行描画( bool bIsDangerDrums, bool bIsDangerGuitar, bool bIsDangerBass )
		{
			return 0;
		}


		// その他

		#region [ private ]
		//-----------------
		//private bool bDanger中;
		//private CCounter ct移動用;
		//private CCounter ct透明度用;
//		private const int n右位置 = 0x12a;
//		private const int n左位置 = 0x26;
		private readonly Rectangle[] rc領域 = new Rectangle[] { new Rectangle( 0, 0, 0x20, 0x40 ), new Rectangle( 0x20, 0, 0x20, 0x40 ) };
		private CTexture txDANGER;
		//-----------------
		#endregion
	}
}
