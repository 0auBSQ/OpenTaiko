using FDK;

namespace OpenTaiko {
	internal class CActResultSongBar : CActivity {
		// コンストラクタ

		public CActResultSongBar() {
			base.IsDeActivated = true;
		}


		// メソッド

		public void tアニメを完了させる() {
			this.ct登場用.CurrentValue = (int)this.ct登場用.EndValue;
		}


		// CActivity 実装

		public override void Activate() {

			// After performing calibration, inform the player that
			// calibration has been completed, rather than
			// displaying the song title as usual.


			var title = OpenTaiko.IsPerformingCalibration
				? $"Calibration complete. InputAdjustTime is now {OpenTaiko.ConfigIni.nInputAdjustTimeMs}ms (Note : InputAdjust is deprecated, please transfer the value to GlobalOffset and reload the songs"
				: OpenTaiko.DTX.TITLE.GetString("");

			using (var bmpSongTitle = pfMusicName.DrawText(title, OpenTaiko.Skin.Result_MusicName_ForeColor, OpenTaiko.Skin.Result_MusicName_BackColor, null, 30)) {
				this.txMusicName = OpenTaiko.tテクスチャの生成(bmpSongTitle, false);
				txMusicName.vcScaleRatio.X = OpenTaiko.GetSongNameXScaling(ref txMusicName, OpenTaiko.Skin.Result_MusicName_MaxSize);
			}

			base.Activate();
		}
		public override void DeActivate() {
			if (this.ct登場用 != null) {
				this.ct登場用 = null;
			}

			OpenTaiko.tテクスチャの解放(ref this.txMusicName);
			base.DeActivate();
		}
		public override void CreateManagedResource() {
			this.pfMusicName = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.Result_MusicName_FontSize);
			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource() {
			OpenTaiko.tDisposeSafely(ref this.pfMusicName);
			base.ReleaseManagedResource();
		}
		public override int Draw() {
			if (base.IsDeActivated) {
				return 0;
			}
			if (base.IsFirstDraw) {
				this.ct登場用 = new CCounter(0, 270, 4, OpenTaiko.Timer);
				base.IsFirstDraw = false;
			}
			this.ct登場用.Tick();

			if (OpenTaiko.Skin.Result_MusicName_ReferencePoint == CSkin.ReferencePoint.Center) {
				this.txMusicName.t2D描画(OpenTaiko.Skin.Result_MusicName_X - ((this.txMusicName.szTextureSize.Width * txMusicName.vcScaleRatio.X) / 2), OpenTaiko.Skin.Result_MusicName_Y);
			} else if (OpenTaiko.Skin.Result_MusicName_ReferencePoint == CSkin.ReferencePoint.Left) {
				this.txMusicName.t2D描画(OpenTaiko.Skin.Result_MusicName_X, OpenTaiko.Skin.Result_MusicName_Y);
			} else {
				this.txMusicName.t2D描画(OpenTaiko.Skin.Result_MusicName_X - this.txMusicName.szTextureSize.Width * txMusicName.vcScaleRatio.X, OpenTaiko.Skin.Result_MusicName_Y);
			}

			if (!this.ct登場用.IsEnded) {
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
