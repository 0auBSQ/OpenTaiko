using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using SlimDX.Direct3D9;
using FDK;

using Device = SampleFramework.DeviceCache;

namespace TJAPlayer3
{
	internal class CActオプションパネル : CActivity
	{
		// CActivity 実装

		public override void On非活性化()
		{
			if( !base.b活性化してない )
			{
				TJAPlayer3.tテクスチャの解放( ref this.txオプションパネル );
				base.On非活性化();
			}
		}
		public override void OnManagedリソースの作成()
		{
			if( !base.b活性化してない )
			{
				this.txオプションパネル = TJAPlayer3.tテクスチャの生成( CSkin.Path( @"Graphics\Screen option panels.png" ), false );
				base.OnManagedリソースの作成();
			}
		}
		public override int On進行描画()
		{
			if( !base.b活性化してない )
			{
				Device device = TJAPlayer3.app.Device;
				CConfigIni configIni = TJAPlayer3.ConfigIni;
				if( this.txオプションパネル != null )
				{
					#region [ ScrollSpeed ]
					int drums = configIni.n譜面スクロール速度.Drums;
					if( drums > 15 )
					{
						drums = 15;
					}
					this.txオプションパネル.t2D描画( device, 0x171, 12, this.rc譜面スピード[ drums ] );
					int guitar = configIni.n譜面スクロール速度.Guitar;
					if( guitar > 15 )
					{
						guitar = 15;
					}
					this.txオプションパネル.t2D描画( device, 0x171, 0x18, this.rc譜面スピード[ guitar ] );
					int bass = configIni.n譜面スクロール速度.Bass;
					if( bass > 15 )
					{
						bass = 15;
					}
					this.txオプションパネル.t2D描画( device, 0x171, 0x24, this.rc譜面スピード[ bass ] );
					#endregion
					#region [ Sud/Hid/Invisible ]
					this.txオプションパネル.t2D描画( device, 0x189, 12, this.rcHS[ ( configIni.bHidden.Drums ? 1 : 0 ) + ( configIni.bSudden.Drums ? 2 : 0 ) +
																					( configIni.eInvisible.Drums == EInvisible.SEMI ? 4 : 0 ) +
																					( configIni.eInvisible.Drums == EInvisible.FULL ? 5 : 0 ) ] );
					this.txオプションパネル.t2D描画( device, 0x189, 0x18, this.rcHS[ ( configIni.bHidden.Guitar ? 1 : 0 ) + ( configIni.bSudden.Guitar ? 2 : 0 ) +
																					( configIni.eInvisible.Guitar == EInvisible.SEMI ? 4 : 0 ) +
																					( configIni.eInvisible.Guitar == EInvisible.FULL ? 5 : 0 ) ] );
					this.txオプションパネル.t2D描画( device, 0x189, 0x24, this.rcHS[ ( configIni.bHidden.Bass ? 1 : 0 ) + ( configIni.bSudden.Bass ? 2 : 0 ) +
																					( configIni.eInvisible.Bass == EInvisible.SEMI ? 4 : 0 ) +
																					( configIni.eInvisible.Bass == EInvisible.FULL ? 5 : 0 ) ] );
					#endregion
					#region [ Dark ]
					this.txオプションパネル.t2D描画( device, 0x1a1, 12, this.rcDark[ (int) configIni.eDark ] );
					this.txオプションパネル.t2D描画( device, 0x1a1, 0x18, this.rcDark[ (int) configIni.eDark ] );
					this.txオプションパネル.t2D描画( device, 0x1a1, 0x24, this.rcDark[ (int) configIni.eDark ] );
					#endregion
					#region [ Reverse ]
					this.txオプションパネル.t2D描画( device, 0x1b9, 12, this.rcReverse[ configIni.bReverse.Drums ? 1 : 0 ] );
					this.txオプションパネル.t2D描画( device, 0x1b9, 0x18, this.rcReverse[ configIni.bReverse.Guitar ? 1 : 0 ] );
					this.txオプションパネル.t2D描画( device, 0x1b9, 0x24, this.rcReverse[ configIni.bReverse.Bass ? 1 : 0 ] );
					#endregion
					#region [ Position ]
					this.txオプションパネル.t2D描画( device, 0x1d1, 12, this.rcPosition[ (int) configIni.判定文字表示位置.Drums ] );
					this.txオプションパネル.t2D描画( device, 0x1d1, 0x18, this.rcPosition[ (int) configIni.判定文字表示位置.Guitar ] );
					this.txオプションパネル.t2D描画( device, 0x1d1, 0x24, this.rcPosition[ (int) configIni.判定文字表示位置.Bass ] );
					#endregion
					#region [ Tight ]
					this.txオプションパネル.t2D描画( device, 0x1e9, 12, this.rcTight[ configIni.bTight ? 1 : 0 ] );
					#endregion
					#region [ Random ]
					this.txオプションパネル.t2D描画( device, 0x1e9, 0x18, this.rcRandom[ (int) configIni.eRandom.Guitar ] );
					this.txオプションパネル.t2D描画( device, 0x1e9, 0x24, this.rcRandom[ (int) configIni.eRandom.Bass ] );
					#endregion
					#region [ ComboPosition ]
					this.txオプションパネル.t2D描画( device, 0x201, 12, new Rectangle(0, 0, 0, 0) );
					#endregion
					#region [ Light ]
					this.txオプションパネル.t2D描画( device, 0x201, 0x18, this.rcLight[ configIni.bLight.Guitar ? 1 : 0 ] );
					this.txオプションパネル.t2D描画( device, 0x201, 0x24, this.rcLight[ configIni.bLight.Bass ? 1 : 0 ] );
					#endregion
					#region [ Left ]
					this.txオプションパネル.t2D描画( device, 0x219, 0x18, this.rcLeft[ configIni.bLeft.Guitar ? 1 : 0 ] );
					this.txオプションパネル.t2D描画( device, 0x219, 0x24, this.rcLeft[ configIni.bLeft.Bass ? 1 : 0 ] );
					#endregion
				}
			}
			return 0;
		}

		
		// その他

		#region [ private ]
		//-----------------
		private readonly Rectangle[] rcComboPos = new Rectangle[] { new Rectangle( 0x30, 0x48, 0x18, 12 ), new Rectangle( 0x30, 60, 0x18, 12 ), new Rectangle( 0x30, 0x30, 0x18, 12 ), new Rectangle( 0x18, 0x48, 0x18, 12 ) };
		private readonly Rectangle[] rcDark = new Rectangle[] { new Rectangle( 0x18, 0, 0x18, 12 ), new Rectangle( 0x18, 12, 0x18, 12 ), new Rectangle( 0x18, 0x54, 0x18, 12 ) };
		private readonly Rectangle[] rcHS = new Rectangle[] {
			new Rectangle( 0, 0, 0x18, 12 ),		// OFF
			new Rectangle( 0, 12, 0x18, 12 ),		// Hidden
			new Rectangle( 0, 0x18, 0x18, 12 ),		// Sudden
			new Rectangle( 0, 0x24, 0x18, 12 ),		// H/S
			new Rectangle(0x60, 0x54, 0x18, 12 ),	// Semi-Invisible
			new Rectangle( 120, 0x54, 0x18, 12 )	// Full-Invisible
		};
		private readonly Rectangle[] rcLeft = new Rectangle[] { new Rectangle( 0x60, 0x48, 0x18, 12 ), new Rectangle( 120, 0x48, 0x18, 12 ) };
		private readonly Rectangle[] rcLight = new Rectangle[] { new Rectangle( 120, 0x30, 0x18, 12 ), new Rectangle( 120, 60, 0x18, 12 ) };
		private readonly Rectangle[] rcPosition = new Rectangle[] {
			new Rectangle(  0, 48, 24, 12 ),		// P-A
			new Rectangle(  0, 60, 24, 12 ),		// P-B
			new Rectangle(  0, 72, 24, 12 ),		// P-B
			new Rectangle( 24, 72, 24, 12 )			// OFF
		};
		private readonly Rectangle[] rcRandom = new Rectangle[] { new Rectangle( 0x48, 0x30, 0x18, 12 ), new Rectangle( 0x48, 60, 0x18, 12 ), new Rectangle( 0x48, 0x48, 0x18, 12 ), new Rectangle( 0x48, 0x54, 0x18, 12 ) };
		private readonly Rectangle[] rcReverse = new Rectangle[] { new Rectangle( 0x18, 0x18, 0x18, 12 ), new Rectangle( 0x18, 0x24, 0x18, 12 ) };
		private readonly Rectangle[] rcTight = new Rectangle[] { new Rectangle( 0x60, 0x30, 0x18, 12 ), new Rectangle( 0x60, 60, 0x18, 12 ) };
		private readonly Rectangle[] rc譜面スピード = new Rectangle[] { new Rectangle( 0x30, 0, 0x18, 12 ), new Rectangle( 0x30, 12, 0x18, 12 ), new Rectangle( 0x30, 0x18, 0x18, 12 ), new Rectangle( 0x30, 0x24, 0x18, 12 ), new Rectangle( 0x48, 0, 0x18, 12 ), new Rectangle( 0x48, 12, 0x18, 12 ), new Rectangle( 0x48, 0x18, 0x18, 12 ), new Rectangle( 0x48, 0x24, 0x18, 12 ), new Rectangle( 0x60, 0, 0x18, 12 ), new Rectangle( 0x60, 12, 0x18, 12 ), new Rectangle( 0x60, 0x18, 0x18, 12 ), new Rectangle( 0x60, 0x24, 0x18, 12 ), new Rectangle( 120, 0, 0x18, 12 ), new Rectangle( 120, 12, 0x18, 12 ), new Rectangle( 120, 0x18, 0x18, 12 ), new Rectangle( 120, 0x24, 0x18, 12 ) };
		private CTexture txオプションパネル;
		//-----------------
		#endregion
	}
}
