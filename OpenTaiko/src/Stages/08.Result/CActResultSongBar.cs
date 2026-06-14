using FDK;

namespace OpenTaiko;

internal class CActResultSongBar : CActivity {
	// コンストラクタ

	public CActResultSongBar() {
		base.IsDeActivated = true;
	}


	// メソッド

	public void tAnimeComplete() {
		this.ctAppear.CurrentValue = (int)this.ctAppear.EndValue;
	}


	// CActivity 実装

	public override void Activate() {

		var title = OpenTaiko.TJA.TITLE.GetString("");

		using (var bmpSongTitle = pfMusicName.DrawText(title, OpenTaiko.Skin.Result_MusicName_ForeColor, OpenTaiko.Skin.Result_MusicName_BackColor, null, 30)) {
			this.txMusicName = OpenTaiko.tTextureCreate(bmpSongTitle, false);
			txMusicName.vcScaleRatio.X = OpenTaiko.GetSongNameXScaling(ref txMusicName, OpenTaiko.Skin.Result_MusicName_MaxSize);
		}

		base.Activate();
	}
	public override void DeActivate() {
		if (this.ctAppear != null) {
			this.ctAppear = null;
		}

		OpenTaiko.tTextureRelease(ref this.txMusicName);
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
			this.ctAppear = new CCounter(0, 270, 4, OpenTaiko.Timer);
			base.IsFirstDraw = false;
		}
		this.ctAppear.Tick();

		if (OpenTaiko.Skin.Result_MusicName_ReferencePoint == CSkin.ReferencePoint.Center) {
			this.txMusicName.t2DDraw(OpenTaiko.Skin.Result_MusicName_X - ((this.txMusicName.szTextureSize.Width * txMusicName.vcScaleRatio.X) / 2), OpenTaiko.Skin.Result_MusicName_Y);
		} else if (OpenTaiko.Skin.Result_MusicName_ReferencePoint == CSkin.ReferencePoint.Left) {
			this.txMusicName.t2DDraw(OpenTaiko.Skin.Result_MusicName_X, OpenTaiko.Skin.Result_MusicName_Y);
		} else {
			this.txMusicName.t2DDraw(OpenTaiko.Skin.Result_MusicName_X - this.txMusicName.szTextureSize.Width * txMusicName.vcScaleRatio.X, OpenTaiko.Skin.Result_MusicName_Y);
		}

		if (!this.ctAppear.IsEnded) {
			return 0;
		}
		return 1;
	}


	// その他

	#region [ private ]
	//-----------------
	private CCounter ctAppear;

	private CTexture txMusicName;
	private CCachedFontRenderer pfMusicName;
	//-----------------
	#endregion
}
