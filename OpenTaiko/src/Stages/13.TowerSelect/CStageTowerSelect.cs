using System.Diagnostics;
using System.Drawing;
using System.Text;
using FDK;

// Minimalist menu class to use for custom menus
namespace OpenTaiko;

class CStageTowerSelect : CStage {
	public CStageTowerSelect() {
		base.eStageID = EStage.TaikoTowers;
		base.ePhaseID = CStage.EPhase.Common_NORMAL;
		this.Cursor = new() { UpdateInfo = () => this.tUpdateBarInfos() };

		// Load CActivity objects here
		// base.list子Activities.Add(this.act = new CAct());

		base.ChildActivities.Add(this.actFOtoNowLoading = new CActFIFOStart());
		base.ChildActivities.Add(this.actFOtoTitle = new CActFIFOBlack());

	}

	public override void Activate() {
		// On activation

		if (base.IsActivated)
			return;

		base.ePhaseID = CStage.EPhase.Common_NORMAL;
		this.eFadeOutCompleteWhenReturnValue = EReturnValue.Continuation;

		this.Cursor.Activate(OpenTaiko.SongManager.listSongRoot_Tower);
		tUpdateBarInfos();

		Background = new ScriptBG(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.TOWERSELECT}Script.lua"));
		Background.Init();

		base.Activate();
	}

	public override void DeActivate() {
		// On de-activation

		OpenTaiko.tDisposeSafely(ref Background);

		base.DeActivate();
	}

	public override void CreateManagedResource() {
		// Ressource allocation

		pfTitleFont = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.TowerSelect_Title_Size);
		pfSubTitleFont = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.TowerSelect_SubTitle_Size);

		base.CreateManagedResource();
	}

	public override void ReleaseManagedResource() {
		// Ressource freeing

		pfTitleFont?.Dispose();
		pfSubTitleFont?.Dispose();

		base.ReleaseManagedResource();
	}

	public override int Draw() {
		Background.Update();
		Background.Draw();

		for (int i = 0; i < OpenTaiko.Skin.TowerSelect_Bar_Count; i++) {
			int currentSong = Cursor.IdxItem + i - ((OpenTaiko.Skin.TowerSelect_Bar_Count - 1) / 2);
			if (currentSong < 0 || currentSong >= BarInfos.Length) continue;
			var bar = BarInfos[currentSong];

			int x = OpenTaiko.Skin.TowerSelect_Bar_X[i];
			int y = OpenTaiko.Skin.TowerSelect_Bar_Y[i];
			tDrawTower(x, y, bar);
		}

		#region [Input]

		if (this.eFadeOutCompleteWhenReturnValue == EReturnValue.Continuation) {
			int returnTitle() {
				OpenTaiko.Skin.soundDecideSFX.tStop(); // cancel if played
				OpenTaiko.Skin.soundCancelSFX.tPlay();
				this.eFadeOutCompleteWhenReturnValue = EReturnValue.BackToTitle;
				this.actFOtoTitle.tFadeOutStart();
				base.ePhaseID = CStage.EPhase.Common_FADEOUT;
				return 0;
			}

			if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.RightArrow) ||
				OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, EPad.RightChange)) {
				OpenTaiko.Skin.soundChangeSFX.tPlay();

				if (Cursor.IdxItem < BarInfos.Length - 1) {
					Cursor.IdxItem++;
				}
			} else if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.LeftArrow) ||
					   OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, EPad.LeftChange)) {
				OpenTaiko.Skin.soundChangeSFX.tPlay();

				if (Cursor.IdxItem > 0) {
					Cursor.IdxItem--;
				}
			} else if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape) ||
					   OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, EPad.Cancel)) {

				#region [Fast return (Escape)]
				returnTitle();
				#endregion
			} else if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return) ||
					   OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, EPad.Decide)) {
				#region [Decide]

				OpenTaiko.Skin.soundDecideSFX.tPlay();

				Cursor.tSelectItem();
				switch (Cursor.Item?.nodeType ?? CSongListNode.ENodeType.BACKBOX) {
					case CSongListNode.ENodeType.SCORE:
						tSelectSong();
						break;
					case CSongListNode.ENodeType.RANDOM:
						tSelectSongRandomly();
						break;
					case CSongListNode.ENodeType.BOX:
						Cursor.tOpenFolder(Cursor.Item!);
						break;
					case CSongListNode.ENodeType.BACKBOX: {
							if (this.Cursor.IsInRootFolder("太鼓タワー")) {
								returnTitle();
							} else {
								Cursor.tCloseFolder();
							}
						}
						break;
				}

				#endregion
			}
		}

		#endregion



		// Menu exit fade out transition
		switch (base.ePhaseID) {
			case CStage.EPhase.SongSelect_FadeOutToNowLoading:
				if (this.actFOtoNowLoading.Draw() == 0) {
					break;
				}
				return (int)this.eFadeOutCompleteWhenReturnValue;
			case CStage.EPhase.Common_FADEOUT:
				if (this.actFOtoTitle.Draw() == 0) {
					break;
				}
				return (int)this.eFadeOutCompleteWhenReturnValue;

		}

		return 0;
	}

	#region [Private]

	private class BarInfo {
		public string strTitle;
		public string strSubTitle;
		public CSongListNode.ENodeType eノード種別;
		public TitleTextureKey ttkTitle;
		public TitleTextureKey ttkSubTitle;
	}

	public void tSelectSong() {
		OpenTaiko.ConfigIni.bTokkunMode = false;
		OpenTaiko.SongMount.rChoosenSong = Cursor.Item!;
		OpenTaiko.SongMount.rChosenScore = Cursor.Item!.score[(int)Difficulty.Tower];
		OpenTaiko.SongMount.nChoosenSongDifficulty[0] = (int)Difficulty.Tower;
		OpenTaiko.SongMount.strChosenSongGenre = Cursor.Item!.songGenre;
		if ((OpenTaiko.SongMount.rChoosenSong != null) && (OpenTaiko.SongMount.rChosenScore != null)) {
			this.eFadeOutCompleteWhenReturnValue = EReturnValue.SongSelected;
			this.actFOtoNowLoading.tFadeOutStart();                // #27787 2012.3.10 yyagi 曲決定時の画面フェードアウトの省略
			base.ePhaseID = CStage.EPhase.SongSelect_FadeOutToNowLoading;
		}
		// TJAPlayer3.Skin.bgm選曲画面.t停止する();
		CSongSelectSongManager.stopSong();
	}

	private bool tSelectSongRandomly() {
		var mandatoryDiffs = new List<int>();
		CSongListNode song = Cursor.Item!;

		List<CSongListNode> songs = new List<CSongListNode>();
		OpenTaiko.stageSongSelect.t指定された曲の子リストの曲を列挙する_孫リスト含む(song.rParentNode, ref songs, ref mandatoryDiffs, true, Difficulty.Tower);
		song.randomList = songs;

		int selectableSongCount = song.randomList.Count;

		if (selectableSongCount == 0) {
			return false;
		}

		int randomSongIndex = OpenTaiko.Random.Next(selectableSongCount);

		if (OpenTaiko.ConfigIni.bOutputDetailedDTXLog) {
			StringBuilder builder = new StringBuilder(0x400);
			builder.Append(string.Format("Total number of songs to randomly choose from {0}. Randomly selected index {0}.", selectableSongCount, randomSongIndex));
			Trace.TraceInformation(builder.ToString());
		}

		// Third assignment
		OpenTaiko.SongMount.rChoosenSong = song.randomList[randomSongIndex];
		OpenTaiko.SongMount.nChoosenSongDifficulty[0] = (int)Difficulty.Tower;

		OpenTaiko.SongMount.rChosenScore = OpenTaiko.SongMount.rChoosenSong.score[OpenTaiko.SongMount.FindClosestDifficultyToAnchor(OpenTaiko.SongMount.rChoosenSong)];
		OpenTaiko.SongMount.strChosenSongGenre = OpenTaiko.SongMount.rChoosenSong.songGenre;

		//TJAPlayer3.Skin.sound曲決定音.t再生する();

		this.eFadeOutCompleteWhenReturnValue = EReturnValue.SongSelected;
		this.actFOtoNowLoading.tFadeOutStart();                    // #27787 2012.3.10 yyagi 曲決定時の画面フェードアウトの省略
		base.ePhaseID = CStage.EPhase.SongSelect_FadeOutToNowLoading;

		CSongSelectSongManager.stopSong();

		return true;
	}

	private void tDrawTower(int x, int y, BarInfo barInfo) {
		switch (barInfo.eノード種別) {
			case CSongListNode.ENodeType.SCORE:
				OpenTaiko.Tx.TowerSelect_Tower.t2DCenterBasedDraw(x, y);
				break;
			case CSongListNode.ENodeType.RANDOM:
				OpenTaiko.Tx.TowerSelect_Tower.t2DCenterBasedDraw(x, y);
				break;
			case CSongListNode.ENodeType.BOX:
				OpenTaiko.Tx.TowerSelect_Tower.t2DCenterBasedDraw(x, y);
				break;
			case CSongListNode.ENodeType.BACKBOX:
				OpenTaiko.Tx.TowerSelect_Tower.t2DCenterBasedDraw(x, y);
				break;
		}

		TitleTextureKey.ResolveTitleTexture(barInfo.ttkTitle).t2DScaledCenterBasedDraw(x + OpenTaiko.Skin.TowerSelect_Title_Offset[0], y + OpenTaiko.Skin.TowerSelect_Title_Offset[1]);
		TitleTextureKey.ResolveTitleTexture(barInfo.ttkSubTitle).t2DScaledCenterBasedDraw(x + OpenTaiko.Skin.TowerSelect_SubTitle_Offset[0], y + OpenTaiko.Skin.TowerSelect_SubTitle_Offset[1]);
	}

	private void tUpdateBarInfos() {
		BarInfos = new BarInfo[Cursor.Items.Count];
		tSetBarInfos();
	}

	private void tSetBarInfos() {
		for (int i = 0; i < BarInfos.Length; i++) {
			BarInfos[i] = new BarInfo();
			BarInfo bar = BarInfos[i];
			CSongListNode song = Cursor.Items[i];

			bar.strTitle = song.ldTitle.GetString("");
			bar.strSubTitle = song.ldSubtitle.GetString("");
			bar.eノード種別 = song.nodeType;

			bar.ttkTitle = new TitleTextureKey(bar.strTitle, pfTitleFont, Color.Black, Color.Transparent, OpenTaiko.Skin.TowerSelect_Title_MaxWidth);
			bar.ttkSubTitle = new TitleTextureKey(bar.strSubTitle, pfTitleFont, Color.Black, Color.Transparent, OpenTaiko.Skin.TowerSelect_SubTitle_MaxWidth);
		}
	}

	private SongSelectCursor Cursor;
	private BarInfo[] BarInfos;

	private ScriptBG Background;

	private CCachedFontRenderer pfTitleFont;
	private CCachedFontRenderer pfSubTitleFont;

	public EReturnValue eFadeOutCompleteWhenReturnValue;
	public CActFIFOStart actFOtoNowLoading;
	public CActFIFOBlack actFOtoTitle;
	#endregion
}
