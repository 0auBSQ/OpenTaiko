using System.Diagnostics;
using FDK;
using SkiaSharp;
using Color = System.Drawing.Color;

namespace OpenTaiko;

internal class CActPlayPanelString : CActivity {
	public static int tToArgb(int r, int g, int b) {
		return (b * 65536 + g * 256 + r);
	}

	// コンストラクタ
	public CActPlayPanelString() {
		base.IsDeActivated = true;
		this.Start();
	}


	// メソッド

	/// <summary>
	/// 右上の曲名、曲数表示の更新を行います。
	/// </summary>
	/// <param name="songName">曲名</param>
	/// <param name="genreName">ジャンル名</param>
	/// <param name="stageText">曲数</param>
	public void SetPanelString(string songName, string genreName, string stageText = null, CSongListNode songNode = null) {
		if (base.IsActivated) {
			OpenTaiko.tTextureRelease(ref this.txPanel);
			if ((songName != null) && (songName.Length > 0)) {
				try {
					using (var bmpSongTitle = pfMusicName.DrawText(songName, OpenTaiko.Skin.Game_MusicName_ForeColor, OpenTaiko.Skin.Game_MusicName_BackColor, null, 30)) {
						this.txMusicName = OpenTaiko.tTextureCreate(bmpSongTitle, false);
					}
					if (txMusicName != null) {
						this.txMusicName.vcScaleRatio.X = OpenTaiko.GetSongNameXScaling(ref txMusicName);
					}

					SKBitmap bmpDiff;
					string strDiff = "";
					if (OpenTaiko.Skin.eDiffDispMode == EDifficultyDisplayType.TextOnNthSong) {
						switch (OpenTaiko.SongMount.nChoosenSongDifficulty[0]) {
							case 0:
								strDiff = "かんたん ";
								break;
							case 1:
								strDiff = "ふつう ";
								break;
							case 2:
								strDiff = "むずかしい ";
								break;
							case 3:
								strDiff = "おに ";
								break;
							case 4:
								strDiff = "えでぃと ";
								break;
							default:
								strDiff = "おに ";
								break;
						}
						bmpDiff = pfMusicName.DrawText(strDiff + stageText, OpenTaiko.Skin.Game_StageText_ForeColor, OpenTaiko.Skin.Game_StageText_BackColor, null, 30);
					} else {
						bmpDiff = pfMusicName.DrawText(stageText, OpenTaiko.Skin.Game_StageText_ForeColor, OpenTaiko.Skin.Game_StageText_BackColor, null, 30);
					}

					using (bmpDiff) {
						txStage = OpenTaiko.Tx.TxCGen("Songs");
					}
				} catch (CTextureCreateFailedException e) {
					Trace.TraceError(e.ToString());
					Trace.TraceError("パネル文字列テクスチャの生成に失敗しました。");
					this.txPanel = null;
				}
			}

			this.txGENRE = OpenTaiko.Tx.TxCGen("Template");

			// Genre text color comes from the box's color (boxdef); default to white otherwise.
			Color stageColor = Color.White;
			if (songNode != null && songNode.isChangedBoxColor)
				stageColor = songNode.BoxColor;

			this.txGENRE.color4 = CConversion.ColorToColor4(stageColor);

			pfGENRE = HPrivateFastFont.tInstantiateBoxFont(OpenTaiko.Skin.Game_GenreText_FontSize);

			this.ttkGENRE = new TitleTextureKey(genreName, this.pfGENRE, Color.White, Color.Black, 1000);

			this.ctForProgress = new CCounter(0, 2000, 2, OpenTaiko.Timer);
			this.Start();



		}
	}

	public void tLyricsTextureCreate(SKBitmap bmplyric) {
		OpenTaiko.tDisposeSafely(ref this.txLyricsTexture);
		this.txLyricsTexture = OpenTaiko.tTextureCreate(bmplyric);
	}
	public void tLyricsTextureRemove() {
		OpenTaiko.tTextureRelease(ref this.txLyricsTexture);
	}
	/// <summary>
	/// レイヤー管理のため、On進行描画から分離。
	/// </summary>
	public void tLyricsTextureDraw() {
		if (this.txLyricsTexture != null) {
			if (OpenTaiko.Skin.Game_Lyric_ReferencePoint == CSkin.ReferencePoint.Left) {
				this.txLyricsTexture.t2DDraw(OpenTaiko.Skin.Game_Lyric_X, OpenTaiko.Skin.Game_Lyric_Y - (this.txLyricsTexture.szTextureSize.Height));
			} else if (OpenTaiko.Skin.Game_Lyric_ReferencePoint == CSkin.ReferencePoint.Right) {
				this.txLyricsTexture.t2DDraw(OpenTaiko.Skin.Game_Lyric_X - this.txLyricsTexture.szTextureSize.Width, OpenTaiko.Skin.Game_Lyric_Y - (this.txLyricsTexture.szTextureSize.Height));
			} else {
				this.txLyricsTexture.t2DDraw(OpenTaiko.Skin.Game_Lyric_X - (this.txLyricsTexture.szTextureSize.Width / 2), OpenTaiko.Skin.Game_Lyric_Y - (this.txLyricsTexture.szTextureSize.Height));
			}
		}
	}

	public void Stop() {
		this.bMute = true;
	}
	public void Start() {
		this.bMute = false;
	}


	// CActivity 実装

	public override void Activate() {
		this.pfMusicName = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.Game_MusicName_FontSize);
		this.txPanel = null;
		this.ctForProgress = new CCounter();
		this.Start();
		this.bFirst = true;
		base.Activate();
	}
	public override void DeActivate() {
		this.ctForProgress = null;
		OpenTaiko.tDisposeSafely(ref this.txPanel);
		OpenTaiko.tDisposeSafely(ref this.txMusicName);
		OpenTaiko.tDisposeSafely(ref this.txGENRE);
		OpenTaiko.tDisposeSafely(ref this.pfGENRE);
		OpenTaiko.tDisposeSafely(ref this.txPanel);
		OpenTaiko.tDisposeSafely(ref this.pfMusicName);
		OpenTaiko.tDisposeSafely(ref this.pfLyricsFont);
		OpenTaiko.tDisposeSafely(ref this.txLyricsTexture);
		base.DeActivate();
	}
	public override void CreateManagedResource() {
		base.CreateManagedResource();
	}
	public override void ReleaseManagedResource() {
		base.ReleaseManagedResource();
	}
	public override int Draw() {
		if (OpenTaiko.stageGameScreen.actDan.IsAnimating || OpenTaiko.ConfigIni.nPlayerCount > 2) return 0;
		if (!base.IsDeActivated && !this.bMute) {
			this.ctForProgress.TickLoop();

			if (this.txGENRE != null) {
				this.txGENRE.t2DDraw(OpenTaiko.Skin.Game_Genre_X, OpenTaiko.Skin.Game_Genre_Y);
				TitleTextureKey.ResolveTitleTexture(this.ttkGENRE).t2DScaledCenterBasedDraw(OpenTaiko.Skin.Game_Genre_X + OpenTaiko.Skin.Game_GenreText_Offset[0], OpenTaiko.Skin.Game_Genre_Y + OpenTaiko.Skin.Game_GenreText_Offset[1]);
			}
			if (this.txStage != null)
				this.txStage.t2DDraw(OpenTaiko.Skin.Game_Genre_X, OpenTaiko.Skin.Game_Genre_Y);

			if (OpenTaiko.Skin.bCurrentStageCountDisplay) {
				if (this.txMusicName != null) {
					float fRate = (float)OpenTaiko.Skin.Game_MusicName_MaxWidth / this.txMusicName.szTextureSize.Width;
					if (this.txMusicName.szTextureSize.Width <= OpenTaiko.Skin.Game_MusicName_MaxWidth)
						fRate = 1.0f;

					this.txMusicName.vcScaleRatio.X = fRate;

					this.txMusicName.t2DDraw(OpenTaiko.Skin.Game_MusicName_X - (this.txMusicName.szTextureSize.Width * fRate), OpenTaiko.Skin.Game_MusicName_Y);
				}
			} else {
				#region[ 透明度制御 ]

				if (this.ctForProgress.CurrentValue < 745) {
					if (this.txStage != null)
						this.txStage.Opacity = 0;
				} else if (this.ctForProgress.CurrentValue >= 745 && this.ctForProgress.CurrentValue < 1000) {
					if (this.txStage != null)
						this.txStage.Opacity = (this.ctForProgress.CurrentValue - 745);
				} else if (this.ctForProgress.CurrentValue >= 1000 && this.ctForProgress.CurrentValue <= 1745) {
					if (this.txStage != null)
						this.txStage.Opacity = 255;
				} else if (this.ctForProgress.CurrentValue >= 1745) {
					if (this.txStage != null)
						this.txStage.Opacity = 255 - (this.ctForProgress.CurrentValue - 1745);
				}
				#endregion

				if (this.txMusicName != null) {
					if (this.IsFirstDraw) {
						IsFirstDraw = false;
					}
					if (this.txMusicName != null) {
						float fRate = (float)OpenTaiko.Skin.Game_MusicName_MaxWidth / this.txMusicName.szTextureSize.Width;
						if (this.txMusicName.szTextureSize.Width <= OpenTaiko.Skin.Game_MusicName_MaxWidth)
							fRate = 1.0f;

						this.txMusicName.vcScaleRatio.X = fRate;

						this.txMusicName.t2DDraw(OpenTaiko.Skin.Game_MusicName_X - (this.txMusicName.szTextureSize.Width * fRate), OpenTaiko.Skin.Game_MusicName_Y);
					}
				}
			}

			//CDTXMania.act文字コンソール.tPrint( 0, 0, C文字コンソール.Eフォント種別.白, this.ct進行用.n現在の値.ToString() );

			//this.txMusicName.t2D描画( CDTXMania.app.Device, 1250 - this.txMusicName.szテクスチャサイズ.Width, 14 );
		}
		return 0;
	}

	public enum ESongType {
		REGULAR,
		DAN,
		TOWER,
		BOSS,
		TOTAL,
	}


	// その他

	#region [ private ]
	//-----------------
	private CCounter ctForProgress;

	private CTexture txPanel;
	private bool bMute;
	private bool bFirst;

	private CTexture txMusicName;
	private CTexture txStage;
	private CTexture txGENRE;
	private CCachedFontRenderer pfGENRE;
	private TitleTextureKey ttkGENRE;
	private CTexture txLyricsTexture;
	private CCachedFontRenderer pfMusicName;
	private CCachedFontRenderer pfLyricsFont;
	//-----------------
	#endregion
}
