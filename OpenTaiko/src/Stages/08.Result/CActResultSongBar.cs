using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using FDK;

namespace TJAPlayer3
{
	internal class CActResultSongBar : CActivity
	{
		// コンストラクタ

		public CActResultSongBar()
		{
			base.IsDeActivated = true;
		}


		// メソッド

		public void tアニメを完了させる()
		{
			this.ct登場用.CurrentValue = (int)this.ct登場用.EndValue;
		}


		// CActivity 実装

		public override void Activate()
		{

		    // After performing calibration, inform the player that
		    // calibration has been completed, rather than
		    // displaying the song title as usual.


		    var title = TJAPlayer3.IsPerformingCalibration
		        ? $"Calibration complete. InputAdjustTime is now {TJAPlayer3.ConfigIni.nInputAdjustTimeMs}ms (Note : InputAdjust is deprecated, please transfer the value to GlobalOffset and reload the songs"
		        : TJAPlayer3.DTX.TITLE;

		    using (var bmpSongTitle = pfMusicName.DrawText(title, TJAPlayer3.Skin.Result_MusicName_ForeColor, TJAPlayer3.Skin.Result_MusicName_BackColor, null, 30))

		    {
		        this.txMusicName = TJAPlayer3.tテクスチャの生成(bmpSongTitle, false);
		        txMusicName.vcScaleRatio.X = TJAPlayer3.GetSongNameXScaling(ref txMusicName, TJAPlayer3.Skin.Result_MusicName_MaxSize);
		    }

			base.Activate();
		}
		public override void DeActivate()
		{
			if( this.ct登場用 != null )
			{
				this.ct登場用 = null;
			}
			
            TJAPlayer3.tテクスチャの解放( ref this.txMusicName );
			base.DeActivate();
		}
		public override void CreateManagedResource()
		{
			this.pfMusicName = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.Result_MusicName_FontSize);
			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource()
		{
            TJAPlayer3.tDisposeSafely(ref this.pfMusicName);
            base.ReleaseManagedResource();
		}
		public override int Draw()
		{
			if( base.IsDeActivated )
			{
				return 0;
			}
			if( base.IsFirstDraw )
			{
				this.ct登場用 = new CCounter( 0, 270, 4, TJAPlayer3.Timer );
				base.IsFirstDraw = false;
			}
			this.ct登場用.Tick();

            if (TJAPlayer3.Skin.Result_MusicName_ReferencePoint == CSkin.ReferencePoint.Center)
            {
                this.txMusicName.t2D描画(TJAPlayer3.Skin.Result_MusicName_X - ((this.txMusicName.szTextureSize.Width * txMusicName.vcScaleRatio.X) / 2), TJAPlayer3.Skin.Result_MusicName_Y);
            }
            else if (TJAPlayer3.Skin.Result_MusicName_ReferencePoint == CSkin.ReferencePoint.Left)
            {
                this.txMusicName.t2D描画(TJAPlayer3.Skin.Result_MusicName_X, TJAPlayer3.Skin.Result_MusicName_Y);
            }
            else
            {
                this.txMusicName.t2D描画(TJAPlayer3.Skin.Result_MusicName_X - this.txMusicName.szTextureSize.Width * txMusicName.vcScaleRatio.X, TJAPlayer3.Skin.Result_MusicName_Y);
            }

			if( !this.ct登場用.IsEnded )
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
        private CCachedFontRenderer pfMusicName;
        //-----------------
		#endregion
	}
}
