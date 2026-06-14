using System.Drawing;
using System.Runtime.InteropServices;
using FDK;

namespace OpenTaiko;

internal class CActSelectInformation : CActivity {
	// Constructor

	public CActSelectInformation() {
		base.IsDeActivated = true;
	}


	// CActivity 実装

	public override void Activate() {
		this.n画像Index上 = -1;
		this.n画像Index下 = 0;

		this.bFirst = true;
		this.ctForProgress = new CCounter(0, 3000, 3, OpenTaiko.Timer);
		base.Activate();
	}
	public override void DeActivate() {
		this.ctスクロール用 = null;
		base.DeActivate();
	}
	public override void CreateManagedResource() {
		this.txInfo_Back = OpenTaiko.tTextureCreate(CSkin.Path(@$"Graphics{Path.DirectorySeparatorChar}5_information_BG.png"));
		this.txInfo[0] = OpenTaiko.tTextureCreate(CSkin.Path(@$"Graphics{Path.DirectorySeparatorChar}5_information.png"));
		this.txInfo[1] = OpenTaiko.tTextureCreate(CSkin.Path(@$"Graphics{Path.DirectorySeparatorChar}5_information2.png"));
		base.CreateManagedResource();
	}
	public override void ReleaseManagedResource() {
		OpenTaiko.tTextureRelease(ref this.txInfo_Back);
		OpenTaiko.tTextureRelease(ref this.txInfo[0]);
		OpenTaiko.tTextureRelease(ref this.txInfo[1]);
		base.ReleaseManagedResource();
	}
	public override int Draw() {
		if (!base.IsDeActivated) {
			if (base.IsFirstDraw) {
				base.IsFirstDraw = false;
			}

			if (this.txInfo_Back != null)
				this.txInfo_Back.t2DDraw(340, 600);


			this.ctForProgress.TickLoop();
			if (this.bFirst) {
				this.ctForProgress.CurrentValue = 300;
			}

			#region[ 透明度制御 ]
			if (this.txInfo[0] != null && this.txInfo[1] != null) {
				if (this.ctForProgress.CurrentValue < 255) {
					this.txInfo[0].Opacity = this.ctForProgress.CurrentValue;
					this.txInfo[1].Opacity = 255 - this.ctForProgress.CurrentValue;
				} else if (this.ctForProgress.CurrentValue >= 255 && this.ctForProgress.CurrentValue < 1245) {
					this.bFirst = false;
					this.txInfo[0].Opacity = 255;
					this.txInfo[1].Opacity = 0;
				} else if (this.ctForProgress.CurrentValue >= 1245 && this.ctForProgress.CurrentValue < 1500) {
					this.txInfo[0].Opacity = 255 - (this.ctForProgress.CurrentValue - 1245);
					this.txInfo[1].Opacity = this.ctForProgress.CurrentValue - 1245;
				} else if (this.ctForProgress.CurrentValue >= 1500 && this.ctForProgress.CurrentValue <= 3000) {
					this.txInfo[0].Opacity = 0;
					this.txInfo[1].Opacity = 255;
				}

				this.txInfo[0].t2DDraw(340, 600);
				this.txInfo[1].t2DDraw(340, 600);
			}

			#endregion


		}
		return 0;
	}


	// その他

	#region [ private ]
	//-----------------
	[StructLayout(LayoutKind.Sequential)]
	private struct STINFO {
		public int nTexture番号;
		public Point pt左上座標;
		public STINFO(int nTexture番号, int x, int y) {
			this.nTexture番号 = nTexture番号;
			this.pt左上座標 = new Point(x, y);
		}
	}

	private CCounter ctスクロール用;
	private int n画像Index下;
	private int n画像Index上;
	private readonly STINFO[] stInfo = new STINFO[] {
		new STINFO( 0, 0, 0 ),
		new STINFO( 0, 0, 49 ),
		new STINFO( 0, 0, 97 ),
		new STINFO( 0, 0, 147 ),
		new STINFO( 0, 0, 196 ),
		new STINFO( 1, 0, 0 ),
		new STINFO( 1, 0, 49 ),
		new STINFO( 1, 0, 97 ),
		new STINFO( 1, 0, 147 )
	};
	private CTexture txInfo_Back;
	private CTexture[] txInfo = new CTexture[2];
	private bool bFirst;
	private CCounter ctForProgress;
	//-----------------
	#endregion
}
