using System.Drawing;
using FDK;

namespace TJAPlayer3 {
	internal class CActオプションパネル : CActivity {
		// CActivity 実装

		public override void DeActivate() {
			if (!base.IsDeActivated) {
				base.DeActivate();
			}
		}
		public override void CreateManagedResource() {
			this.txオプションパネル = TJAPlayer3.tテクスチャの生成(CSkin.Path(@"Graphics\Screen option panels.png"), false);
			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource() {
			TJAPlayer3.tテクスチャの解放(ref this.txオプションパネル);
			base.ReleaseManagedResource();
		}
		public override int Draw() {
			if (!base.IsDeActivated) {
				CConfigIni configIni = TJAPlayer3.ConfigIni;
				/*
				if( this.txオプションパネル != null )
				{

					
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
				*/
			}
			return 0;
		}


		// その他

		#region [ private ]
		//-----------------
		private readonly Rectangle[] rcComboPos = new Rectangle[] { new Rectangle(0x30, 0x48, 0x18, 12), new Rectangle(0x30, 60, 0x18, 12), new Rectangle(0x30, 0x30, 0x18, 12), new Rectangle(0x18, 0x48, 0x18, 12) };
		private readonly Rectangle[] rcDark = new Rectangle[] { new Rectangle(0x18, 0, 0x18, 12), new Rectangle(0x18, 12, 0x18, 12), new Rectangle(0x18, 0x54, 0x18, 12) };
		private readonly Rectangle[] rcHS = new Rectangle[] {
			new Rectangle( 0, 0, 0x18, 12 ),		// OFF
			new Rectangle( 0, 12, 0x18, 12 ),		// Hidden
			new Rectangle( 0, 0x18, 0x18, 12 ),		// Sudden
			new Rectangle( 0, 0x24, 0x18, 12 ),		// H/S
			new Rectangle(0x60, 0x54, 0x18, 12 ),	// Semi-Invisible
			new Rectangle( 120, 0x54, 0x18, 12 )	// Full-Invisible
		};
		private readonly Rectangle[] rcLeft = new Rectangle[] { new Rectangle(0x60, 0x48, 0x18, 12), new Rectangle(120, 0x48, 0x18, 12) };
		private readonly Rectangle[] rcLight = new Rectangle[] { new Rectangle(120, 0x30, 0x18, 12), new Rectangle(120, 60, 0x18, 12) };
		private readonly Rectangle[] rcPosition = new Rectangle[] {
			new Rectangle(  0, 48, 24, 12 ),		// P-A
			new Rectangle(  0, 60, 24, 12 ),		// P-B
			new Rectangle(  0, 72, 24, 12 ),		// P-B
			new Rectangle( 24, 72, 24, 12 )			// OFF
		};
		private readonly Rectangle[] rcRandom = new Rectangle[] { new Rectangle(0x48, 0x30, 0x18, 12), new Rectangle(0x48, 60, 0x18, 12), new Rectangle(0x48, 0x48, 0x18, 12), new Rectangle(0x48, 0x54, 0x18, 12) };
		private readonly Rectangle[] rcReverse = new Rectangle[] { new Rectangle(0x18, 0x18, 0x18, 12), new Rectangle(0x18, 0x24, 0x18, 12) };
		private readonly Rectangle[] rcTight = new Rectangle[] { new Rectangle(0x60, 0x30, 0x18, 12), new Rectangle(0x60, 60, 0x18, 12) };
		private readonly Rectangle[] rc譜面スピード = new Rectangle[] { new Rectangle(0x30, 0, 0x18, 12), new Rectangle(0x30, 12, 0x18, 12), new Rectangle(0x30, 0x18, 0x18, 12), new Rectangle(0x30, 0x24, 0x18, 12), new Rectangle(0x48, 0, 0x18, 12), new Rectangle(0x48, 12, 0x18, 12), new Rectangle(0x48, 0x18, 0x18, 12), new Rectangle(0x48, 0x24, 0x18, 12), new Rectangle(0x60, 0, 0x18, 12), new Rectangle(0x60, 12, 0x18, 12), new Rectangle(0x60, 0x18, 0x18, 12), new Rectangle(0x60, 0x24, 0x18, 12), new Rectangle(120, 0, 0x18, 12), new Rectangle(120, 12, 0x18, 12), new Rectangle(120, 0x18, 0x18, 12), new Rectangle(120, 0x24, 0x18, 12) };
		private CTexture txオプションパネル;
		//-----------------
		#endregion
	}
}
