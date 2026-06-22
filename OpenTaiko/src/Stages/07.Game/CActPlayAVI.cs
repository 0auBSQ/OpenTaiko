using FDK;

namespace OpenTaiko;

internal class CActPlayAVI : CActivity {
	// コンストラクタ

	public CActPlayAVI() {
		base.IsDeActivated = true;
		this.isCutScene = false;
	}


	// メソッド

	public void Start(CVideoDecoder rVD) {
		this.Start(rVD, false);
	}
	public void Start(CVideoDecoder rVD, bool isCutScene) {
		this.isCutScene = isCutScene;
		if (this.isCutScene || OpenTaiko.ConfigIni.bEnableAVI) {
			this.rVD = rVD;
			if (this.rVD != null) {
				this.ratio1 = Math.Min((float)GameWindowSize.Height / ((float)this.rVD.FrameSize.Height), (float)GameWindowSize.Width / ((float)this.rVD.FrameSize.Height));

				if (!rVD.bPlaying) this.rVD.Start();
			}
		}
	}
	public void Seek(int ms) => this.rVD?.Seek(ms);

	public void Stop() => this.rVD?.Stop();

	public void Pause() => this.rVD?.Pause();
	public void Resume() => this.rVD?.Resume();
	public void TogglePause() => this.rVD?.TogglePause();

	public override unsafe int Draw() {
		if (!base.IsDeActivated) {
			if (this.rVD == null || !(this.isCutScene || this.rVD.bDrawing))
				return 0;

			this.rVD.GetNowFrame(ref this.txDraw);

			this.txDraw.vcScaleRatio.X = this.ratio1;
			this.txDraw.vcScaleRatio.Y = this.ratio1;

			if (this.isCutScene || OpenTaiko.ConfigIni.eClipDispType.HasFlag(EClipDispType.BackgroundOnly)) {
				this.txDraw.t2DScaledDraw(CTexture.RefPnt.Center, GameWindowSize.Width / 2, GameWindowSize.Height / 2);
			}
		}
		return 0;
	}

	public void tWindowDisplay() {
		if (this.rVD == null || this.txDraw == null || !OpenTaiko.ConfigIni.eClipDispType.HasFlag(EClipDispType.WindowOnly))
			return;

		float[] fRatio = new float[] { (GameWindowSize.Width / 2) - 4.0f, (GameWindowSize.Height / 2) - 4.0f }; //中央下表示

		float ratio = Math.Min((float)(fRatio[0] / this.rVD.FrameSize.Width), (float)(fRatio[1] / this.rVD.FrameSize.Height));
		this.txDraw.vcScaleRatio.X = ratio;
		this.txDraw.vcScaleRatio.Y = ratio;

		this.txDraw.t2DScaledDraw(CTexture.RefPnt.Down, GameWindowSize.Width / 2, GameWindowSize.Height);
	}

	// CActivity 実装

	public override void Activate() {
		base.Activate();
	}
	public override void DeActivate() {
		if (this.txDraw != null) {
			this.txDraw.Dispose();
			this.txDraw = null;
		}
		base.DeActivate();
	}
	public override void CreateManagedResource() {
		base.CreateManagedResource();
	}
	public override void ReleaseManagedResource() {
		base.ReleaseManagedResource();
	}


	// その他

	#region [ private ]
	//-----------------
	private bool isCutScene;

	private float ratio1;

	private CTexture txDraw;

	public CVideoDecoder? rVD;

	//-----------------
	#endregion
}
