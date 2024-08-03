using System.Drawing;
using FDK;

// Minimalist menu class to use for custom menus
namespace TJAPlayer3 {
	class CActSelectDanInfo : CStage {
		public CActSelectDanInfo() {
			base.IsDeActivated = true;
		}

		public override void Activate() {
			// On activation

			if (base.IsActivated)
				return;

			ctStep = new CCounter(0, 1000, 2, TJAPlayer3.Timer);
			ctStepFade = new CCounter(0, 255, 0.5, TJAPlayer3.Timer);

			ttkExams = new TitleTextureKey[(int)Exam.Type.Total];
			for (int i = 0; i < ttkExams.Length; i++) {
				ttkExams[i] = new TitleTextureKey(CLangManager.LangInstance.GetExamName(i), pfExamFont, Color.Black, Color.Transparent, 700);
			}

			base.Activate();
		}

		public override void DeActivate() {
			// On de-activation

			base.DeActivate();
		}

		public override void CreateManagedResource() {
			// Ressource allocation
			pfTitleFont = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.SongSelect_DanInfo_Title_Size);
			pfExamFont = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.SongSelect_DanInfo_Exam_Size);

			base.CreateManagedResource();
		}

		public override void ReleaseManagedResource() {
			// Ressource freeing
			TJAPlayer3.tDisposeSafely(ref pfTitleFont);
			TJAPlayer3.tDisposeSafely(ref pfExamFont);

			base.ReleaseManagedResource();
		}

		public override int Draw() {
			ctStep.Tick();
			ctStepFade.Tick();
			if (ctStep.CurrentValue == ctStep.EndValue) {
				ctStep = new CCounter(0, 1000, 2, TJAPlayer3.Timer);
				tNextStep();
			}

			if (TJAPlayer3.Skin.SongSelect_DanInfo_Show) {
				for (int i = 0; i < TJAPlayer3.stageSongSelect.rNowSelectedSong.DanSongs.Count; i++) {
					var dan = TJAPlayer3.stageSongSelect.rNowSelectedSong.DanSongs[i];
					int songIndex = i / 3;
					int opacity = 255;
					if (TJAPlayer3.stageSongSelect.rNowSelectedSong.DanSongs.Count > 3) {
						if (nNowSongIndex == songIndex) {
							opacity = ctStepFade.CurrentValue;
						} else if (nPrevSongIndex == songIndex) {
							opacity = 255 - ctStepFade.CurrentValue;
						} else {
							opacity = 0;
						}
					}

					int pos = i % 3;
					CActSelect段位リスト.tDisplayDanIcon(i + 1, TJAPlayer3.Skin.SongSelect_DanInfo_Icon_X[pos], TJAPlayer3.Skin.SongSelect_DanInfo_Icon_Y[pos], opacity, TJAPlayer3.Skin.SongSelect_DanInfo_Icon_Scale, false);

					int difficulty_cymbol_width = TJAPlayer3.Tx.Dani_Difficulty_Cymbol.szTextureSize.Width / 5;
					int difficulty_cymbol_height = TJAPlayer3.Tx.Dani_Difficulty_Cymbol.szTextureSize.Height;

					TJAPlayer3.Tx.Dani_Difficulty_Cymbol.Opacity = opacity;
					TJAPlayer3.Tx.Dani_Difficulty_Cymbol.vcScaleRatio.X = TJAPlayer3.Skin.SongSelect_DanInfo_Difficulty_Cymbol_Scale;
					TJAPlayer3.Tx.Dani_Difficulty_Cymbol.vcScaleRatio.Y = TJAPlayer3.Skin.SongSelect_DanInfo_Difficulty_Cymbol_Scale;
					TJAPlayer3.Tx.Dani_Difficulty_Cymbol.t2D拡大率考慮中央基準描画(TJAPlayer3.Skin.SongSelect_DanInfo_Difficulty_Cymbol_X[pos], TJAPlayer3.Skin.SongSelect_DanInfo_Difficulty_Cymbol_Y[pos], new Rectangle(dan.Difficulty * difficulty_cymbol_width, 0, difficulty_cymbol_width, difficulty_cymbol_height));
					TJAPlayer3.Tx.Dani_Difficulty_Cymbol.Opacity = 255;
					TJAPlayer3.Tx.Dani_Difficulty_Cymbol.vcScaleRatio.X = 1;
					TJAPlayer3.Tx.Dani_Difficulty_Cymbol.vcScaleRatio.Y = 1;

					TJAPlayer3.Tx.Dani_Level_Number.Opacity = opacity;
					TJAPlayer3.stage段位選択.段位リスト.tLevelNumberDraw(TJAPlayer3.Skin.SongSelect_DanInfo_Level_Number_X[pos], TJAPlayer3.Skin.SongSelect_DanInfo_Level_Number_Y[pos], dan.Level, TJAPlayer3.Skin.SongSelect_DanInfo_Level_Number_Scale);
					TJAPlayer3.Tx.Dani_Level_Number.Opacity = 255;

					TitleTextureKey.ResolveTitleTexture(ttkTitles[i]).Opacity = opacity;
					TitleTextureKey.ResolveTitleTexture(ttkTitles[i]).t2D描画(TJAPlayer3.Skin.SongSelect_DanInfo_Title_X[pos], TJAPlayer3.Skin.SongSelect_DanInfo_Title_Y[pos]);


				}

				for (int j = 0; j < CExamInfo.cMaxExam; j++) {
					int index = j;
					Dan_C danc0 = TJAPlayer3.stageSongSelect.rNowSelectedSong.DanSongs[0].Dan_C[j];

					if (danc0 != null) {
						TitleTextureKey.ResolveTitleTexture(this.ttkExams[(int)danc0.GetExamType()]).t2D中心基準描画(TJAPlayer3.Skin.SongSelect_DanInfo_Exam_X[index], TJAPlayer3.Skin.SongSelect_DanInfo_Exam_Y[index]);
					}

					if (TJAPlayer3.stageSongSelect.rNowSelectedSong.DanSongs[TJAPlayer3.stageSongSelect.rNowSelectedSong.DanSongs.Count - 1].Dan_C[j] == null) {
						Dan_C danc = TJAPlayer3.stageSongSelect.rNowSelectedSong.DanSongs[0].Dan_C[j];
						if (danc != null) {
							TJAPlayer3.stage段位選択.段位リスト.tExamDraw(TJAPlayer3.Skin.SongSelect_DanInfo_Exam_Value_X[0], TJAPlayer3.Skin.SongSelect_DanInfo_Exam_Value_Y[index], danc.Value[0], danc.GetExamRange(), TJAPlayer3.Skin.SongSelect_DanInfo_Exam_Value_Scale);
						}
					} else {
						for (int i = 0; i < TJAPlayer3.stageSongSelect.rNowSelectedSong.DanSongs.Count; i++) {
							Dan_C danc = TJAPlayer3.stageSongSelect.rNowSelectedSong.DanSongs[i].Dan_C[j];
							if (danc != null) {
								int opacity = 255;
								if (TJAPlayer3.stageSongSelect.rNowSelectedSong.DanSongs.Count > 3) {
									if (nNowSongIndex == i / 3) {
										opacity = ctStepFade.CurrentValue;
									} else if (nPrevSongIndex == i / 3) {
										opacity = 255 - ctStepFade.CurrentValue;
									} else {
										opacity = 0;
									}
								}

								TJAPlayer3.Tx.Dani_Exam_Number.Opacity = opacity;
								TJAPlayer3.stage段位選択.段位リスト.tExamDraw(TJAPlayer3.Skin.SongSelect_DanInfo_Exam_Value_X[i % 3], TJAPlayer3.Skin.SongSelect_DanInfo_Exam_Value_Y[index], danc.Value[0], danc.GetExamRange(), TJAPlayer3.Skin.SongSelect_DanInfo_Exam_Value_Scale);
								TJAPlayer3.Tx.Dani_Exam_Number.Opacity = 255;
							}
						}
					}
				}
			}

			return 0;
		}

		public void UpdateSong() {
			if (TJAPlayer3.stageSongSelect.rNowSelectedSong == null || TJAPlayer3.stageSongSelect.rNowSelectedSong.DanSongs == null) return;

			ttkTitles = new TitleTextureKey[TJAPlayer3.stageSongSelect.rNowSelectedSong.DanSongs.Count];
			for (int i = 0; i < TJAPlayer3.stageSongSelect.rNowSelectedSong.DanSongs.Count; i++) {
				var dan = TJAPlayer3.stageSongSelect.rNowSelectedSong.DanSongs[i];
				ttkTitles[i] = new TitleTextureKey(dan.bTitleShow ? "???" : dan.Title, pfTitleFont, Color.Black, Color.Transparent, 700);
			}
		}

		#region [Private]

		private TitleTextureKey[] ttkTitles;
		private TitleTextureKey[] ttkExams;
		private CCachedFontRenderer pfTitleFont;
		private CCachedFontRenderer pfExamFont;

		private CCounter ctStep;
		private CCounter ctStepFade;

		private int nPrevSongIndex;
		private int nNowSongIndex;

		private void tNextStep() {
			nPrevSongIndex = nNowSongIndex;
			nNowSongIndex = (nNowSongIndex + 1) % (int)Math.Ceiling(TJAPlayer3.stageSongSelect.rNowSelectedSong.DanSongs.Count / 3.0);
			ctStepFade = new CCounter(0, 255, 1, TJAPlayer3.Timer);
		}

		#endregion
	}
}
