using FDK;

namespace TJAPlayer3 {
	internal class CActSelectPreimageパネル : CActivity {
		// メソッド

		public CActSelectPreimageパネル() {
			base.IsDeActivated = true;
		}
		public void tSelectedSongChanged() {
			this.ctDelayedDisplay = new CCounter(-TJAPlayer3.ConfigIni.n曲が選択されてからプレビュー画像が表示開始されるまでのウェイトms, 100, 1, TJAPlayer3.Timer);
			this.bNewPreimageLoaded = false;
		}

		// CActivity 実装

		public override void Activate() {
			this.rCurrentlyDisplayedPreimage = this.txDefaultPreimage;
			this.strCurrentFilename = "";
			this.bNewPreimageLoaded = false;
			this.txPreimage = null;
			this.tUpdatePreimage(TJAPlayer3.stageSongSelect.r現在選択中のスコア);
			base.Activate();
		}
		public override void DeActivate() {
			TJAPlayer3.tテクスチャの解放(ref this.txPreimage);
			this.ctApparitionAnimation = null;
			this.ctDelayedDisplay = null;
			base.DeActivate();
		}
		public override void CreateManagedResource() {
			this.txDefaultPreimage = TJAPlayer3.tテクスチャの生成(CSkin.Path(@$"Graphics{Path.DirectorySeparatorChar}3_SongSelect{Path.DirectorySeparatorChar}PreImageDefault.png"), false);
			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource() {

			TJAPlayer3.tテクスチャの解放(ref this.txDefaultPreimage);
			base.ReleaseManagedResource();
		}
		public override int Draw() {
			if (!base.IsDeActivated) {
				if (base.IsFirstDraw) {
					this.ctApparitionAnimation = new CCounter(0, 100, 5, TJAPlayer3.Timer);
					base.IsFirstDraw = false;
				}
				this.ctApparitionAnimation.Tick();
				if ((!TJAPlayer3.stageSongSelect.bCurrentlyScrolling && (this.ctDelayedDisplay != null)) && this.ctDelayedDisplay.IsTicked) {
					this.ctDelayedDisplay.Tick();
					if ((this.ctDelayedDisplay.CurrentValue >= 0) && this.bNewPreimageStillLoading) {
						this.tUpdatePreimage(TJAPlayer3.stageSongSelect.r現在選択中のスコア);
						TJAPlayer3.Timer.Update();
						this.ctDelayedDisplay.NowTime = TJAPlayer3.Timer.NowTime;
						this.bNewPreimageLoaded = true;
					} else if (this.ctDelayedDisplay.IsEnded && this.ctDelayedDisplay.IsTicked) {
						this.ctDelayedDisplay.Stop();
					}
				}
				this.tDisplayPreimage();
			}
			return 0;
		}

		public CTexture? tGenerateAndGetPreimage(Cスコア? cScoreInst) {
			this.tUpdatePreimage(cScoreInst);
			return tGetPreimageTextureResized();
		}

		public CTexture? tGetPreimageTextureResized() {


			if (this.rCurrentlyDisplayedPreimage != null) {

				int width = TJAPlayer3.Skin.SongSelect_Preimage_Size[0];
				int height = TJAPlayer3.Skin.SongSelect_Preimage_Size[1];

				float xRatio = width / (float)this.rCurrentlyDisplayedPreimage.sz画像サイズ.Width;
				float yRatio = height / (float)this.rCurrentlyDisplayedPreimage.sz画像サイズ.Height;

				this.rCurrentlyDisplayedPreimage.Opacity = 255;
				this.rCurrentlyDisplayedPreimage.vcScaleRatio.X = xRatio;
				this.rCurrentlyDisplayedPreimage.vcScaleRatio.Y = xRatio;

			}
			return rCurrentlyDisplayedPreimage;
		}

		// その他

		#region [ private ]
		//-----------------
		private CCounter ctDelayedDisplay;
		private CCounter ctApparitionAnimation;
		private CTexture rCurrentlyDisplayedPreimage;
		private string strCurrentFilename;
		private CTexture txPreimage;
		private CTexture txDefaultPreimage;
		private bool bNewPreimageLoaded;
		private bool bNewPreimageStillLoading {
			get {
				return !this.bNewPreimageLoaded;
			}
			set {
				this.bNewPreimageLoaded = !value;
			}
		}

		private void tUpdatePreimage(Cスコア? cScoreInst) {
			if (cScoreInst != null && this.tBuildPreimageAssets(cScoreInst)) {
				return;
			}

			// If no preimage or preimage not found
			this.rCurrentlyDisplayedPreimage = this.txDefaultPreimage;
			this.strCurrentFilename = "";
		}
		private bool tBuildPreimageAssets(Cスコア cScoreInst) {
			if ((cScoreInst == null) || string.IsNullOrEmpty(cScoreInst.譜面情報.Preimage)) return false;

			string str = ((!Path.IsPathRooted(cScoreInst.譜面情報.Preimage)) ? cScoreInst.ファイル情報.フォルダの絶対パス : "") + cScoreInst.譜面情報.Preimage;
			if (!str.Equals(this.strCurrentFilename)) {
				TJAPlayer3.tテクスチャの解放(ref this.txPreimage);
				this.strCurrentFilename = str;
				if (!File.Exists(this.strCurrentFilename)) {
					LogNotification.PopWarning("Preimage not found ({0})".SafeFormat(this.strCurrentFilename));
					return false;
				}
				this.txPreimage = TJAPlayer3.tテクスチャの生成(this.strCurrentFilename, false);
				if (this.txPreimage != null) {
					this.rCurrentlyDisplayedPreimage = this.txPreimage;
				} else {
					this.rCurrentlyDisplayedPreimage = this.txDefaultPreimage;
				}
			}
			return true;
		}

		private void tDisplayPreimage() {
			if (!TJAPlayer3.stageSongSelect.bCurrentlyScrolling && (((this.ctDelayedDisplay != null) && (this.ctDelayedDisplay.CurrentValue > 0)) && !this.bNewPreimageStillLoading)) {

				float num3 = ((float)this.ctDelayedDisplay.CurrentValue) / 100f;
				float num4 = 0.9f + (0.1f * num3);

				if (this.rCurrentlyDisplayedPreimage != null) {

					int width = TJAPlayer3.Skin.SongSelect_Preimage_Size[0];
					int height = TJAPlayer3.Skin.SongSelect_Preimage_Size[1];

					float xRatio = width / (float)this.rCurrentlyDisplayedPreimage.sz画像サイズ.Width;
					float yRatio = height / (float)this.rCurrentlyDisplayedPreimage.sz画像サイズ.Height;

					this.rCurrentlyDisplayedPreimage.Opacity = (int)(255f * num3);
					this.rCurrentlyDisplayedPreimage.vcScaleRatio.X = num4 * xRatio;
					this.rCurrentlyDisplayedPreimage.vcScaleRatio.Y = num4 * xRatio;

					this.rCurrentlyDisplayedPreimage.t2D拡大率考慮中央基準描画(TJAPlayer3.Skin.SongSelect_Preimage[0], TJAPlayer3.Skin.SongSelect_Preimage[1]);
				}
			}
		}



		//-----------------
		#endregion
	}
}
