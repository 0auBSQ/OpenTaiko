using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using SlimDX;
using FDK;

namespace TJAPlayer3
{
	internal class CActResultSongBar : CActivity
	{
		// コンストラクタ

		public CActResultSongBar()
		{
			base.b活性化してない = true;
		}


		// メソッド

		public void tアニメを完了させる()
		{
			this.ct登場用.n現在の値 = (int)this.ct登場用.n終了値;
		}


		// CActivity 実装

		public override void On活性化()
		{
            if( !string.IsNullOrEmpty( TJAPlayer3.ConfigIni.FontName) )
            {
                this.pfMusicName = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), TJAPlayer3.Skin.Result_MusicName_FontSize);
            }
            else
            {
                this.pfMusicName = new CPrivateFastFont(new FontFamily("MS UI Gothic"), TJAPlayer3.Skin.Result_MusicName_FontSize);
            }

		    // After performing calibration, inform the player that
		    // calibration has been completed, rather than
		    // displaying the song title as usual.


		    var title = TJAPlayer3.IsPerformingCalibration
		        ? $"Calibration complete. InputAdjustTime is now {TJAPlayer3.ConfigIni.nInputAdjustTimeMs}ms"
		        : TJAPlayer3.DTX.TITLE;

		    using (var bmpSongTitle = pfMusicName.DrawPrivateFont(title, TJAPlayer3.Skin.Result_MusicName_ForeColor, TJAPlayer3.Skin.Result_MusicName_BackColor))

		    {
		        this.txMusicName = TJAPlayer3.tテクスチャの生成(bmpSongTitle, false);
		        txMusicName.vc拡大縮小倍率.X = TJAPlayer3.GetSongNameXScaling(ref txMusicName);
		    }

			base.On活性化();
		}
		public override void On非活性化()
		{
			if( this.ct登場用 != null )
			{
				this.ct登場用 = null;
			}
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
                TJAPlayer3.t安全にDisposeする(ref this.pfMusicName);
                TJAPlayer3.tテクスチャの解放( ref this.txMusicName );

                base.OnManagedリソースの解放();
			}
		}
		public override int On進行描画()
		{
			if( base.b活性化してない )
			{
				return 0;
			}
			if( base.b初めての進行描画 )
			{
				this.ct登場用 = new CCounter( 0, 270, 4, TJAPlayer3.Timer );
				base.b初めての進行描画 = false;
			}
			this.ct登場用.t進行();

            if (TJAPlayer3.Skin.Result_MusicName_ReferencePoint == CSkin.ReferencePoint.Center)
            {
                this.txMusicName.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Result_MusicName_X - ((this.txMusicName.szテクスチャサイズ.Width * txMusicName.vc拡大縮小倍率.X) / 2), TJAPlayer3.Skin.Result_MusicName_Y);
            }
            else if (TJAPlayer3.Skin.Result_MusicName_ReferencePoint == CSkin.ReferencePoint.Left)
            {
                this.txMusicName.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Result_MusicName_X, TJAPlayer3.Skin.Result_MusicName_Y);
            }
            else
            {
                this.txMusicName.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Result_MusicName_X - this.txMusicName.szテクスチャサイズ.Width * txMusicName.vc拡大縮小倍率.X, TJAPlayer3.Skin.Result_MusicName_Y);
            }

			if( !this.ct登場用.b終了値に達した )
			{
				return 0;
			}
			return 1;
		}


		// その他

		#region [ private ]
		//-----------------
		private CCounter ct登場用;

        private CTexture txMusicName;
        private CPrivateFastFont pfMusicName;
        //-----------------
		#endregion
	}
}
