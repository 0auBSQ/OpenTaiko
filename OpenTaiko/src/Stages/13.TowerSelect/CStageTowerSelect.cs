using System.Diagnostics;
using System.Drawing;
using System.Text;
using FDK;

// Minimalist menu class to use for custom menus
namespace OpenTaiko {
	class CStageTowerSelect : CStage {
		public CStageTowerSelect() {
			base.eStageID = EStage.TaikoTowers;
			base.ePhaseID = CStage.EPhase.Common_NORMAL;

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
			this.eフェードアウト完了時の戻り値 = EReturnValue.Continuation;

			if (listSongs == null)
				listSongs = OpenTaiko.Songs管理.list曲ルート_Tower;

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
				int currentSong = nCurrentSongIndex + i - ((OpenTaiko.Skin.TowerSelect_Bar_Count - 1) / 2);
				if (currentSong < 0 || currentSong >= BarInfos.Length) continue;
				var bar = BarInfos[currentSong];

				int x = OpenTaiko.Skin.TowerSelect_Bar_X[i];
				int y = OpenTaiko.Skin.TowerSelect_Bar_Y[i];
				tDrawTower(x, y, bar);
			}

			#region [Input]

			if (this.eフェードアウト完了時の戻り値 == EReturnValue.Continuation) {
				int returnTitle() {
					OpenTaiko.Skin.soundCancelSFX.tPlay();
					this.eフェードアウト完了時の戻り値 = EReturnValue.ReturnToTitle;
					this.actFOtoTitle.tフェードアウト開始();
					base.ePhaseID = CStage.EPhase.Common_FADEOUT;
					return 0;
				}

				if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.RightArrow) ||
					OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RightChange)) {
					OpenTaiko.Skin.soundChangeSFX.tPlay();

					if (nCurrentSongIndex < BarInfos.Length - 1) {
						nCurrentSongIndex++;
					}
				} else if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.LeftArrow) ||
					  OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LeftChange)) {
					OpenTaiko.Skin.soundChangeSFX.tPlay();

					if (nCurrentSongIndex > 0) {
						nCurrentSongIndex--;
					}
				} else if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape) ||
				  OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.Cancel)) {

					#region [Fast return (Escape)]

					OpenTaiko.Skin.soundCancelSFX.tPlay();
					returnTitle();

					#endregion
				} else if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return) ||
					  OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.Decide)) {
					#region [Decide]

					OpenTaiko.Skin.soundDecideSFX.tPlay();

					switch (currentSong.eノード種別) {
						case CSongListNode.ENodeType.SCORE:
							tSelectSong();
							break;
						case CSongListNode.ENodeType.RANDOM:
							tSelectSongRandomly();
							break;
						case CSongListNode.ENodeType.BOX:
							tOpenFolder(currentSong);
							break;
						case CSongListNode.ENodeType.BACKBOX: {
								if (OpenTaiko.Songs管理.list曲ルート.Contains(currentSong.rParentNode) && currentSong.rParentNode.strジャンル == "太鼓タワー") {
									returnTitle();
								} else {
									tCloseFolder(currentSong);
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
					return (int)this.eフェードアウト完了時の戻り値;
				case CStage.EPhase.Common_FADEOUT:
					if (this.actFOtoTitle.Draw() == 0) {
						break;
					}
					return (int)this.eフェードアウト完了時の戻り値;

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
			OpenTaiko.stageSongSelect.rChoosenSong = listSongs[nCurrentSongIndex];
			OpenTaiko.stageSongSelect.r確定されたスコア = listSongs[nCurrentSongIndex].arスコア[(int)Difficulty.Tower];
			OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] = (int)Difficulty.Tower;
			OpenTaiko.stageSongSelect.str確定された曲のジャンル = listSongs[nCurrentSongIndex].strジャンル;
			if ((OpenTaiko.stageSongSelect.rChoosenSong != null) && (OpenTaiko.stageSongSelect.r確定されたスコア != null)) {
				CFloorManagement.reinitialize(OpenTaiko.stageSongSelect.rChoosenSong.arスコア[(int)Difficulty.Tower].譜面情報.nLife);
				this.eフェードアウト完了時の戻り値 = EReturnValue.SongChoosen;
				this.actFOtoNowLoading.tフェードアウト開始();                // #27787 2012.3.10 yyagi 曲決定時の画面フェードアウトの省略
				base.ePhaseID = CStage.EPhase.SongSelect_FadeOutToNowLoading;
			}
			// TJAPlayer3.Skin.bgm選曲画面.t停止する();
			CSongSelectSongManager.stopSong();
		}

		private bool tSelectSongRandomly() {
			var mandatoryDiffs = new List<int>();
			CSongListNode song = currentSong;

			List<CSongListNode> songs = new List<CSongListNode>();
			OpenTaiko.stageSongSelect.t指定された曲の子リストの曲を列挙する_孫リスト含む(song.rParentNode, ref songs, ref mandatoryDiffs, true, Difficulty.Tower);
			song.listランダム用ノードリスト = songs;

			int selectableSongCount = song.listランダム用ノードリスト.Count;

			if (selectableSongCount == 0) {
				return false;
			}

			int randomSongIndex = OpenTaiko.Random.Next(selectableSongCount);

			if (OpenTaiko.ConfigIni.bLogDTX詳細ログ出力) {
				StringBuilder builder = new StringBuilder(0x400);
				builder.Append(string.Format("Total number of songs to randomly choose from {0}. Randomly selected index {0}.", selectableSongCount, randomSongIndex));
				Trace.TraceInformation(builder.ToString());
			}

			// Third assignment
			OpenTaiko.stageSongSelect.rChoosenSong = song.listランダム用ノードリスト[randomSongIndex];
			OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] = (int)Difficulty.Tower;

			CFloorManagement.reinitialize(OpenTaiko.stageSongSelect.rChoosenSong.arスコア[(int)Difficulty.Tower].譜面情報.nLife);
			OpenTaiko.stageSongSelect.r確定されたスコア = OpenTaiko.stageSongSelect.rChoosenSong.arスコア[OpenTaiko.stageSongSelect.actSongList.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(OpenTaiko.stageSongSelect.rChoosenSong)];
			OpenTaiko.stageSongSelect.str確定された曲のジャンル = OpenTaiko.stageSongSelect.rChoosenSong.strジャンル;

			//TJAPlayer3.Skin.sound曲決定音.t再生する();

			this.eフェードアウト完了時の戻り値 = EReturnValue.SongChoosen;
			this.actFOtoNowLoading.tフェードアウト開始();                    // #27787 2012.3.10 yyagi 曲決定時の画面フェードアウトの省略
			base.ePhaseID = CStage.EPhase.SongSelect_FadeOutToNowLoading;

			CSongSelectSongManager.stopSong();

			return true;
		}

		private void tDrawTower(int x, int y, BarInfo barInfo) {
			switch (barInfo.eノード種別) {
				case CSongListNode.ENodeType.SCORE:
					OpenTaiko.Tx.TowerSelect_Tower.t2D中心基準描画(x, y);
					break;
				case CSongListNode.ENodeType.RANDOM:
					OpenTaiko.Tx.TowerSelect_Tower.t2D中心基準描画(x, y);
					break;
				case CSongListNode.ENodeType.BOX:
					OpenTaiko.Tx.TowerSelect_Tower.t2D中心基準描画(x, y);
					break;
				case CSongListNode.ENodeType.BACKBOX:
					OpenTaiko.Tx.TowerSelect_Tower.t2D中心基準描画(x, y);
					break;
			}

			TitleTextureKey.ResolveTitleTexture(barInfo.ttkTitle).t2D拡大率考慮中央基準描画(x + OpenTaiko.Skin.TowerSelect_Title_Offset[0], y + OpenTaiko.Skin.TowerSelect_Title_Offset[1]);
			TitleTextureKey.ResolveTitleTexture(barInfo.ttkSubTitle).t2D拡大率考慮中央基準描画(x + OpenTaiko.Skin.TowerSelect_SubTitle_Offset[0], y + OpenTaiko.Skin.TowerSelect_SubTitle_Offset[1]);
		}

		private void tUpdateBarInfos() {
			BarInfos = new BarInfo[listSongs.Count];
			tSetBarInfos();
		}

		private void tOpenFolder(CSongListNode song) {
			nCurrentSongIndex = 0;
			listSongs = song.list子リスト;
			tUpdateBarInfos();
		}

		private void tCloseFolder(CSongListNode song) {
			nCurrentSongIndex = 0;
			listSongs = song.rParentNode.rParentNode.list子リスト;
			tUpdateBarInfos();
		}

		private void tSetBarInfos() {
			for (int i = 0; i < BarInfos.Length; i++) {
				BarInfos[i] = new BarInfo();
				BarInfo bar = BarInfos[i];
				CSongListNode song = listSongs[i];

				bar.strTitle = song.ldTitle.GetString("");
				bar.strSubTitle = song.ldSubtitle.GetString("");
				bar.eノード種別 = song.eノード種別;

				bar.ttkTitle = new TitleTextureKey(bar.strTitle, pfTitleFont, Color.Black, Color.Transparent, OpenTaiko.Skin.TowerSelect_Title_MaxWidth);
				bar.ttkSubTitle = new TitleTextureKey(bar.strSubTitle, pfTitleFont, Color.Black, Color.Transparent, OpenTaiko.Skin.TowerSelect_SubTitle_MaxWidth);
			}
		}

		private BarInfo[] BarInfos;
		private List<CSongListNode> listSongs;

		private ScriptBG Background;

		private CCachedFontRenderer pfTitleFont;
		private CCachedFontRenderer pfSubTitleFont;

		public EReturnValue eフェードアウト完了時の戻り値;
		public CActFIFOStart actFOtoNowLoading;
		public CActFIFOBlack actFOtoTitle;

		private int nCurrentSongIndex;
		private CSongListNode currentSong {
			get {
				return listSongs[nCurrentSongIndex];
			}
		}

		#endregion
	}
}
