﻿using System.Drawing;
using FDK;
using Silk.NET.Maths;
using Rectangle = System.Drawing.Rectangle;

namespace OpenTaiko {
	class CActSelect段位リスト : CStage {
		public CActSelect段位リスト() {
			for (int i = 0; i < 10; i++) {
				stLevel[i].ch = i.ToString().ToCharArray()[0];
				stLevel[i].pt = new Point(i * 14, 0);

				stSoulNumber[i].ch = i.ToString().ToCharArray()[0];
				stSoulNumber[i].pt = new Point(i * 23, 0);

				stExamNumber[i].ch = i.ToString().ToCharArray()[0];
				stExamNumber[i].pt = new Point(i * 19, 0);
			}
		}

		public bool bスクロール中 {
			get {
				return ctDaniMoveAnime.IsTicked;
			}
		}

		public override void Activate() {
			if (this.IsActivated)
				return;

			DaniInAnime = false;

			ctDaniMoveAnime = new CCounter();
			ctDanAnimeIn = new CCounter();
			ctDaniIn = new CCounter(0, 6000, 1, OpenTaiko.Timer);

			ctDanTick = new CCounter(0, 510, 3, OpenTaiko.Timer);

			ctExamConditionsAnim = new CCounter(0, 4000, 1, OpenTaiko.Timer);

			this.ttkExams = new TitleTextureKey[(int)Exam.Type.Total];
			for (int i = 0; i < this.ttkExams.Length; i++) {
				this.ttkExams[i] = new TitleTextureKey(CLangManager.LangInstance.GetExamName(i), this.pfExamFont, Color.White, Color.SaddleBrown, 1000);
			}

			listSongs = OpenTaiko.Songs管理.list曲ルート_Dan;
			tUpdateSongs();

			base.Activate();
		}

		public override void DeActivate() {

			base.DeActivate();
		}

		public override void CreateManagedResource() {
			this.pfDanFolder = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.DaniSelect_Font_DanFolder_Size[0]);
			this.pfDanFolderDesc = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.DaniSelect_Font_DanFolder_Size[1]);
			this.pfDanSong = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.DaniSelect_Font_DanSong_Size);
			this.pfExamFont = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.DaniSelect_Font_Exam_Size);

			base.CreateManagedResource();
		}

		public override void ReleaseManagedResource() {
			OpenTaiko.tDisposeSafely(ref pfDanFolder);
			OpenTaiko.tDisposeSafely(ref pfDanFolderDesc);
			OpenTaiko.tDisposeSafely(ref pfDanSong);
			OpenTaiko.tDisposeSafely(ref pfExamFont);

			base.ReleaseManagedResource();
		}

		public override int Draw() {
			ctDaniMoveAnime.Tick();
			ctDaniIn.Tick();
			ctDanAnimeIn.Tick();
			ctDanTick.TickLoop();

			ctExamConditionsAnim.TickLoop();

			if (ctDaniIn.CurrentValue == 6000) {
				if (!DaniInAnime) {
					ctDanAnimeIn.Start(0, 90, 2f, OpenTaiko.Timer);
					DaniInAnime = true;
				}
			}

			#region [ バー表示 ]

			if (stバー情報.Length != 0 && ctDaniIn.CurrentValue == 6000) {
				OpenTaiko.Tx.DanC_ExamType.vcScaleRatio.X = 0.81f;
				OpenTaiko.Tx.DanC_ExamType.vcScaleRatio.Y = 0.81f;

				float Anime = ctDanAnimeIn.CurrentValue == 90 ? bLeftMove ? (float)Math.Sin(ctDaniMoveAnime.CurrentValue * (Math.PI / 180)) * OpenTaiko.Skin.Resolution[0] : -((float)Math.Sin(ctDaniMoveAnime.CurrentValue * (Math.PI / 180)) * OpenTaiko.Skin.Resolution[0]) : OpenTaiko.Skin.Resolution[0] - (float)Math.Sin(ctDanAnimeIn.CurrentValue * (Math.PI / 180)) * OpenTaiko.Skin.Resolution[0];

				tDrawDanSelectedLevel(Anime);

				if (bLeftMove && n現在の選択行 - 1 >= 0)
					tDrawDanSelectedLevel(Anime, -1);
				if (!bLeftMove && n現在の選択行 + 1 <= stバー情報.Length - 1)
					tDrawDanSelectedLevel(Anime, 1);
			}

			#endregion

			#region [ バー移動 ]

			if (ctDaniMoveAnime.CurrentValue == 90) {
				if (bLeftMove) {
					this.n現在の選択行 -= n現在の選択行 - 1 >= 0 ? 1 : 0;
				} else {
					this.n現在の選択行 += n現在の選択行 + 1 < this.stバー情報.Length ? 1 : 0;
				}
				ctDaniMoveAnime.Stop();
				ctDaniMoveAnime.CurrentValue = 0;
			}

			#endregion

			// To do : Display the 27 (max) bars one by one
			if (ctDaniIn.CurrentValue < 5000)
				return 0;

			#region [Upper plates]

			// stバー情報[n現在の選択行]

			int tickWidth = OpenTaiko.Tx.Dani_Plate.szTextureSize.Width / 7;
			int tickHeight = OpenTaiko.Tx.Dani_Plate.szTextureSize.Height;
			int tickExtraWidth = OpenTaiko.Tx.Dani_Plate_Extra.szTextureSize.Width / 3;
			int tickExtraHeight = OpenTaiko.Tx.Dani_Plate_Extra.szTextureSize.Height;

			for (int idx = -13; idx < 14; idx++) {

				if (ctDaniIn.CurrentValue < 5000 + (idx + 13) * 33)
					break;

				int currentSong = n現在の選択行 + idx;

				if (currentSong < 0)
					continue;
				if (currentSong >= stバー情報.Length)
					break;

				int xPos = OpenTaiko.Skin.DaniSelect_Plate[0] + (idx == 0 ? OpenTaiko.Skin.DaniSelect_Plate_Center_Move[0] : 0) + idx * OpenTaiko.Skin.DaniSelect_Plate_Move[0];
				int yPos = OpenTaiko.Skin.DaniSelect_Plate[1] + (idx == 0 ? OpenTaiko.Skin.DaniSelect_Plate_Center_Move[1] : 0) + idx * OpenTaiko.Skin.DaniSelect_Plate_Move[1];



				#region [Plate background]

				int tick = Math.Max(0, Math.Min(5, stバー情報[currentSong].nDanTick));
				Color tickColor = stバー情報[currentSong].cDanTickColor;

				switch (stバー情報[currentSong].eノード種別) {
					case CSongListNode.ENodeType.BACKBOX: {
							OpenTaiko.Tx.Dani_Plate_Extra?.tUpdateOpacity(255);
							OpenTaiko.Tx.Dani_Plate_Extra?.tUpdateColor4(CConversion.ColorToColor4(tickColor));
							OpenTaiko.Tx.Dani_Plate_Extra?.t2D拡大率考慮上中央基準描画(xPos, yPos, new Rectangle(0, 0, tickExtraWidth, tickExtraHeight));
							break;
						}
					case CSongListNode.ENodeType.BOX: {
							OpenTaiko.Tx.Dani_Plate_Extra?.tUpdateOpacity(255);
							OpenTaiko.Tx.Dani_Plate_Extra?.tUpdateColor4(CConversion.ColorToColor4(tickColor));
							OpenTaiko.Tx.Dani_Plate_Extra?.t2D拡大率考慮上中央基準描画(xPos, yPos, new Rectangle(tickExtraWidth, 0, tickExtraWidth, tickExtraHeight));
							break;
						}
					case CSongListNode.ENodeType.RANDOM: {
							OpenTaiko.Tx.Dani_Plate_Extra?.tUpdateOpacity(255);
							OpenTaiko.Tx.Dani_Plate_Extra?.tUpdateColor4(CConversion.ColorToColor4(tickColor));
							OpenTaiko.Tx.Dani_Plate_Extra?.t2D拡大率考慮上中央基準描画(xPos, yPos, new Rectangle(tickExtraWidth * 2, 0, tickExtraWidth, tickExtraHeight));
							break;
						}
					default: {
							OpenTaiko.Tx.Dani_Plate?.tUpdateOpacity(255);
							OpenTaiko.Tx.Dani_Plate?.tUpdateColor4(CConversion.ColorToColor4(tickColor));
							OpenTaiko.Tx.Dani_Plate?.t2D拡大率考慮上中央基準描画(xPos, yPos, new Rectangle(tickWidth * tick, 0, tickWidth, tickHeight));
							break;
						}
				}

				// Reset color for plate flash
				OpenTaiko.Tx.Dani_Plate?.tUpdateColor4(CConversion.ColorToColor4(Color.White));

				#endregion

				#region [Dan grade title]
				if (stバー情報[currentSong].eノード種別 == CSongListNode.ENodeType.SCORE)
					TitleTextureKey.ResolveTitleTextureTate(stバー情報[currentSong].ttkタイトル[stバー情報[currentSong].ttkタイトル.Length - 1])
						.t2D拡大率考慮上中央基準描画(xPos + OpenTaiko.Skin.DaniSelect_Plate_Title_Offset[0], yPos + OpenTaiko.Skin.DaniSelect_Plate_Title_Offset[1]);

				#endregion


				#region [Plate flash]

				if (idx == 0) {
					OpenTaiko.Tx.Dani_Plate?.tUpdateOpacity(Math.Abs(255 - ctDanTick.CurrentValue));
					OpenTaiko.Tx.Dani_Plate?.t2D拡大率考慮上中央基準描画(xPos, yPos, new Rectangle(tickWidth * 6, 0, tickWidth, tickHeight));
				}

				#endregion

				#region [Goukaku plate]

				int currentRank = Math.Min(stバー情報[currentSong].clearGrade, 8) - 3;

				if (currentRank >= 0) {
					OpenTaiko.Tx.DanResult_Rank.vcScaleRatio.X = 0.20f;
					OpenTaiko.Tx.DanResult_Rank.vcScaleRatio.Y = 0.20f;
					int rank_width = OpenTaiko.Tx.DanResult_Rank.szTextureSize.Width / 7;
					int rank_height = OpenTaiko.Tx.DanResult_Rank.szTextureSize.Height;
					OpenTaiko.Tx.DanResult_Rank.t2D拡大率考慮上中央基準描画(xPos - 2, yPos - 14, new Rectangle(rank_width * (currentRank + 1), 0, rank_width, rank_height));
				}

				#endregion
			}

			#endregion

			return 0;
		}

		#region [private]

		private CCounter ctExamConditionsAnim;

		private bool DaniInAnime;
		public CCounter ctDaniIn;

		private CCounter ctDanAnimeIn;

		private CCounter ctDanTick;

		private CCounter ctDaniMoveAnime;
		public int n現在の選択行;

		private bool bLeftMove;

		private CCachedFontRenderer pfDanFolder, pfDanFolderDesc, pfDanSong, pfExamFont;
		public TitleTextureKey[] ttkExams;

		private CStage選曲.STNumber[] stLevel = new CStage選曲.STNumber[10];
		private CStage選曲.STNumber[] stSoulNumber = new CStage選曲.STNumber[10];
		private CStage選曲.STNumber[] stExamNumber = new CStage選曲.STNumber[10];

		public List<CSongListNode> listSongs;
		public STバー情報[] stバー情報;

		public struct STバー情報 {
			public TitleTextureKey[] ttkタイトル;
			public int[] n曲難易度;
			public int[] n曲レベル;
			public CSongListNode.ENodeType eノード種別;
			public List<CDTX.DanSongs> List_DanSongs;
			public CTexture txBarCenter;
			public CTexture txDanPlate;

			// Extra parameters
			public int clearGrade;
			public int nDanTick;
			public Color cDanTickColor;
		}

		public CSongListNode currentBar {
			get {
				return listSongs[n現在の選択行];
			}
		}

		static CCachedFontRenderer pfDanPlateTitle = null;
		static CCachedFontRenderer pfDanIconTitle = null;

		private Dictionary<string, CTexture> BarTexCache = new Dictionary<string, CTexture>();

		public static void RefleshSkin() {
			OpenTaiko.tDisposeSafely(ref pfDanPlateTitle);
			OpenTaiko.tDisposeSafely(ref pfDanIconTitle);
		}

		public static void tDisplayDanPlate(CTexture givenPlate, STバー情報? songNode, int x, int y) {
			if (givenPlate != null) {
				givenPlate.Opacity = 255;
				givenPlate.t2D中心基準描画(x, y);
			} else {
				// Default Dan Plate

				int danTick = 0;
				Color danTickColor = Color.White;

				if (OpenTaiko.stageSongSelect.r確定されたスコア != null) {
					danTick = OpenTaiko.stageSongSelect.r確定されたスコア.譜面情報.nDanTick;
					danTickColor = OpenTaiko.stageSongSelect.r確定されたスコア.譜面情報.cDanTickColor;
				}
				if (songNode != null) {
					STバー情報 stNode = (STバー情報)songNode;

					danTick = stNode.nDanTick;
					danTickColor = stNode.cDanTickColor;
				}


				int unit = OpenTaiko.Tx.Dani_DanPlates.szTextureSize.Width / 6;

				if (OpenTaiko.Tx.Dani_DanPlates != null) {
					OpenTaiko.Tx.Dani_DanPlates.Opacity = 255;
					OpenTaiko.Tx.Dani_DanPlates.color4 = CConversion.ColorToColor4(danTickColor);
				}
				OpenTaiko.Tx.Dani_DanPlates_Back?.t2D中心基準描画(x, y, new Rectangle(
					unit * danTick,
					0,
					unit,
					OpenTaiko.Tx.Dani_DanPlates_Back.szTextureSize.Height
				));
				OpenTaiko.Tx.Dani_DanPlates?.t2D中心基準描画(x, y, new Rectangle(
					unit * danTick,
					0,
					unit,
					OpenTaiko.Tx.Dani_DanPlates.szTextureSize.Height
				));

				if (pfDanPlateTitle == null)
					pfDanPlateTitle = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.DaniSelect_DanPlateTitle_Size);

				string titleTmp = "";

				if (OpenTaiko.stageSongSelect.r確定されたスコア != null)
					titleTmp = OpenTaiko.stageSongSelect.rChoosenSong.ldTitle.GetString("");
				if (songNode != null) {
					STバー情報 stNode = (STバー情報)songNode;

					titleTmp = stNode.ttkタイトル[stNode.ttkタイトル.Length - 1].str;
				}

				TitleTextureKey ttkTmp = new TitleTextureKey(titleTmp.TrimStringWithTags(2), pfDanPlateTitle, Color.White, Color.Black, 1000);
				TitleTextureKey.ResolveTitleTextureTate(ttkTmp).t2D中心基準描画(x + OpenTaiko.Skin.DaniSelect_DanPlateTitle_Offset[0], y + OpenTaiko.Skin.DaniSelect_DanPlateTitle_Offset[1]);
			}
		}

		public static void tDisplayDanIcon(int count, float x, float y, int opacity, float scale, bool showFade = false) {
			if (pfDanIconTitle == null)
				pfDanIconTitle = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.DaniSelect_DanIconTitle_Size);

			string ex = "th";
			switch (count) {
				case 1:
					ex = "st";
					break;
				case 2:
					ex = "nd";
					break;
				case 3:
					ex = "rd";
					break;
			}

			TitleTextureKey ttkTmp = new TitleTextureKey(count.ToString() + ex, pfDanIconTitle, Color.White, Color.Black, 1000);

			if (showFade) {
				OpenTaiko.Tx.Dani_DanIcon_Fade.vcScaleRatio = new Vector3D<float>(scale, scale, 1.0f);
				OpenTaiko.Tx.Dani_DanIcon_Fade.Opacity = opacity;
				OpenTaiko.Tx.Dani_DanIcon_Fade.color4 = CConversion.ColorToColor4(OpenTaiko.Skin.DaniSelect_DanIcon_Color[Math.Min(count - 1, OpenTaiko.Skin.DaniSelect_DanIcon_Color.Length - 1)]);
				OpenTaiko.Tx.Dani_DanIcon_Fade.t2D拡大率考慮描画(CTexture.RefPnt.Left, x - ((OpenTaiko.Tx.Dani_DanIcon.szTextureSize.Width / 2) * scale), y);
				OpenTaiko.Tx.Dani_DanIcon_Fade.Opacity = 255;
			}

			OpenTaiko.Tx.Dani_DanIcon.vcScaleRatio = new Vector3D<float>(scale, scale, 1.0f);
			OpenTaiko.Tx.Dani_DanIcon.Opacity = opacity;
			OpenTaiko.Tx.Dani_DanIcon.color4 = CConversion.ColorToColor4(OpenTaiko.Skin.DaniSelect_DanIcon_Color[Math.Min(count - 1, OpenTaiko.Skin.DaniSelect_DanIcon_Color.Length - 1)]);
			OpenTaiko.Tx.Dani_DanIcon.t2D拡大率考慮中央基準描画(x, y);
			OpenTaiko.Tx.Dani_DanIcon.Opacity = 255;

			TitleTextureKey.ResolveTitleTexture(ttkTmp).vcScaleRatio = new Vector3D<float>(scale, scale, 1.0f);
			TitleTextureKey.ResolveTitleTexture(ttkTmp).Opacity = opacity;
			TitleTextureKey.ResolveTitleTexture(ttkTmp).t2D拡大率考慮中央基準描画(x + OpenTaiko.Skin.DaniSelect_DanIconTitle_Offset[0], y + OpenTaiko.Skin.DaniSelect_DanIconTitle_Offset[1]);
			TitleTextureKey.ResolveTitleTexture(ttkTmp).Opacity = 255;
		}

		private void tDrawDanSelectedLevel(float Anime, int modifier = 0) {
			int scroll = OpenTaiko.Skin.Resolution[0] * modifier;
			int currentSong = Math.Clamp(n現在の選択行 + modifier, 0, stバー情報.Length - 1);
			bool over4 = false;

			switch (stバー情報[currentSong].eノード種別) {
				case CSongListNode.ENodeType.SCORE: {
						#region [Center bar and Dan plate]

						int danTick = stバー情報[currentSong].nDanTick;
						Color danTickColor = stバー情報[currentSong].cDanTickColor;

						// Use the given bar center if provided, else use a default one
						if (stバー情報[currentSong].txBarCenter != null) {
							stバー情報[currentSong].txBarCenter.t2D描画(scroll + Anime, 0);
						} else {
							int unit = OpenTaiko.Tx.Dani_DanSides.szTextureSize.Width / 6;
							OpenTaiko.Tx.Dani_DanSides.color4 = CConversion.ColorToColor4(danTickColor);

							OpenTaiko.Tx.Dani_Bar_Center.t2D描画(scroll + Anime, 0);

							// Bar sides
							OpenTaiko.Tx.Dani_DanSides.t2D描画((int)(scroll + Anime) + OpenTaiko.Skin.DaniSelect_DanSides_X[0], OpenTaiko.Skin.DaniSelect_DanSides_Y[0], new Rectangle(
								unit * danTick,
								0,
								unit,
								OpenTaiko.Tx.Dani_DanSides.szTextureSize.Height
							));

							OpenTaiko.Tx.Dani_DanSides.t2D左右反転描画((int)(scroll + Anime) + OpenTaiko.Skin.DaniSelect_DanSides_X[1], OpenTaiko.Skin.DaniSelect_DanSides_Y[1], new Rectangle(
								unit * danTick,
								0,
								unit,
								OpenTaiko.Tx.Dani_DanSides.szTextureSize.Height
							));
						}

						CActSelect段位リスト.tDisplayDanPlate(stバー情報[currentSong].txDanPlate, stバー情報[currentSong], (int)(scroll + Anime) + OpenTaiko.Skin.DaniSelect_DanPlate[0], OpenTaiko.Skin.DaniSelect_DanPlate[1]);

						#endregion

						#region [Goukaku plate]

						int currentRank = Math.Min(stバー情報[currentSong].clearGrade, 8) - 3;

						if (currentRank >= 0) {
							OpenTaiko.Tx.DanResult_Rank.vcScaleRatio.X = 0.8f;
							OpenTaiko.Tx.DanResult_Rank.vcScaleRatio.Y = 0.8f;

							int rank_width = OpenTaiko.Tx.DanResult_Rank.szTextureSize.Width / 7;
							int rank_height = OpenTaiko.Tx.DanResult_Rank.szTextureSize.Height;

							OpenTaiko.Tx.DanResult_Rank.t2D拡大率考慮中央基準描画(scroll + Anime + OpenTaiko.Skin.DaniSelect_Rank[0], OpenTaiko.Skin.DaniSelect_Rank[1], new Rectangle(rank_width * (currentRank + 1), 0, rank_width, rank_height));
						}

						#endregion

						#region [Soul gauge condition]

						OpenTaiko.Tx.Dani_Bloc[2]?.t2D描画(scroll + Anime + OpenTaiko.Skin.DaniSelect_Bloc2[0], OpenTaiko.Skin.DaniSelect_Bloc2[1]);

						if (stバー情報[currentSong].List_DanSongs[0].Dan_C[0] != null)
							tSoulDraw(scroll + Anime + OpenTaiko.Skin.DaniSelect_Value_Gauge[0], OpenTaiko.Skin.DaniSelect_Value_Gauge[1], stバー情報[currentSong].List_DanSongs[0].Dan_C[0].Value[0]);

						//TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(this.ttkExams[0]).t2D下中央基準描画((int)(scroll + 396 + Anime), 452);
						TitleTextureKey.ResolveTitleTexture(this.ttkExams[0]).t2D拡大率考慮中央基準描画((int)(scroll + Anime) + OpenTaiko.Skin.DaniSelect_Text_Gauge[0], OpenTaiko.Skin.DaniSelect_Text_Gauge[1]);

						#endregion

						#region [Song information]

						int getOpacity(int index, int sections = 2) {
							int current_section = index / 3;
							int animJauge = ctExamConditionsAnim.CurrentValue;
							int split = 4000 / sections;
							int begin = split * current_section;
							int end = split * (current_section + 1);
							if (animJauge < begin || animJauge > end) return 0;

							double sinus = Math.Abs(Math.Sin(animJauge * Math.PI / split));

							if (sinus == 0) return 0;
							return (int)(Math.Abs(Math.Pow(sinus, 1.2) / sinus) * 255);

							/*
                            int opacity = 255;
                            int half = index / 3;



                            if (half == 0)
                            {
                                if (animJauge > 3745)
                                    opacity = animJauge - 3745;
                                else if (animJauge > 1745)
                                    opacity = 2000 - animJauge;
                            }
                            else
                            {
                                if (animJauge > 3745)
                                    opacity = 4000 - animJauge;
                                else if (animJauge > 1745)
                                    opacity = animJauge - 1745;
                                else
                                    opacity = 0;
                            }

                            return opacity;
                            */
						}

						int difficulty_cymbol_width = OpenTaiko.Tx.Dani_Difficulty_Cymbol.szTextureSize.Width / 5;
						int difficulty_cymbol_height = OpenTaiko.Tx.Dani_Difficulty_Cymbol.szTextureSize.Height;
						int sections_count = 1 + ((stバー情報[currentSong].n曲レベル.Length - 1) / 3);

						for (int i = 0; i < stバー情報[currentSong].ttkタイトル.Length - 1; i++) {
							int pos = i % 3;
							int opacity = 255;
							if (stバー情報[currentSong].ttkタイトル.Length - 1 > 3) {
								opacity = getOpacity(i, sections_count);
							}
							TitleTextureKey.ResolveTitleTexture(stバー情報[currentSong].ttkタイトル[i]).Opacity = opacity;
							TitleTextureKey.ResolveTitleTexture(stバー情報[currentSong].ttkタイトル[i]).t2D描画(scroll + Anime + OpenTaiko.Skin.DaniSelect_Title_X[pos], OpenTaiko.Skin.DaniSelect_Title_Y[pos]);
							TitleTextureKey.ResolveTitleTexture(stバー情報[currentSong].ttkタイトル[i]).Opacity = 255;

							tDisplayDanIcon(i + 1, scroll + Anime + OpenTaiko.Skin.DaniSelect_DanIcon_X[pos], OpenTaiko.Skin.DaniSelect_DanIcon_Y[pos], opacity, 1.0f);
						}

						for (int i = 0; i < stバー情報[currentSong].n曲難易度.Length; i++) {
							int pos = i % 3;
							if (stバー情報[currentSong].n曲難易度.Length > 3) {
								OpenTaiko.Tx.Dani_Difficulty_Cymbol.Opacity = getOpacity(i, sections_count);
							}
							OpenTaiko.Tx.Dani_Difficulty_Cymbol.t2D中心基準描画(scroll + Anime + OpenTaiko.Skin.DaniSelect_Difficulty_Cymbol_X[pos], OpenTaiko.Skin.DaniSelect_Difficulty_Cymbol_Y[pos], new Rectangle(stバー情報[currentSong].n曲難易度[i] * difficulty_cymbol_width, 0, difficulty_cymbol_width, difficulty_cymbol_height));
							OpenTaiko.Tx.Dani_Difficulty_Cymbol.Opacity = 255;
						}

						for (int i = 0; i < stバー情報[currentSong].n曲レベル.Length; i++) {
							int pos = i % 3;
							if (stバー情報[currentSong].n曲レベル.Length > 3) {
								OpenTaiko.Tx.Dani_Level_Number.Opacity = getOpacity(i, sections_count);
							}
							this.tLevelNumberDraw(scroll + Anime + OpenTaiko.Skin.DaniSelect_Level_Number_X[pos], OpenTaiko.Skin.DaniSelect_Level_Number_Y[pos], stバー情報[currentSong].n曲レベル[i]);
							OpenTaiko.Tx.Dani_Level_Number.Opacity = 255;
						}


						#endregion

						#region [Check if one of the EXAM5,6,7 slots are used]

						for (int j = 4; j < CExamInfo.cMaxExam; j++) {
							if (stバー情報[currentSong].List_DanSongs[0].Dan_C[j] != null) {
								over4 = true;
								break;
							}
						}

						#endregion

						#region [Display dan conditions]

						for (int j = 1; j < CExamInfo.cMaxExam; j++)  //段位条件のループ(魂ゲージを除く) 縦(y)
						{
							// Inner index within the exam 3-set
							int index = (j - 1) % 3;

							#region [Alter opacity if multi-screen exam display]

							int opacity = 255;

							if (over4 == true) {
								int half = (j - 1) / 3;

								int animJauge = ctExamConditionsAnim.CurrentValue;

								if (half == 0) {
									if (animJauge > 3745)
										opacity = animJauge - 3745;
									else if (animJauge > 1745)
										opacity = 2000 - animJauge;
								} else {
									if (animJauge > 3745)
										opacity = 4000 - animJauge;
									else if (animJauge > 1745)
										opacity = animJauge - 1745;
									else
										opacity = 0;
								}
							}

							#endregion

							#region [Exam value (individual included)]

							for (int i = 0; i < stバー情報[currentSong].List_DanSongs.Count; i++)  //曲ごとのループ(魂ゲージを除く) 横(x)
							{
								if (stバー情報[currentSong].List_DanSongs[i].Dan_C[j] != null) {
									OpenTaiko.Tx.Dani_Exam_Number.Opacity = opacity;

									if (stバー情報[currentSong].List_DanSongs[stバー情報[currentSong].List_DanSongs.Count - 1].Dan_C[j] != null) {
										//個別の条件がありますよー

										int moveX = OpenTaiko.Skin.DaniSelect_Exam_Interval[0];
										int moveY = OpenTaiko.Skin.DaniSelect_Exam_Interval[1];
										int x = OpenTaiko.Skin.DaniSelect_Exam_Bloc_X[index];
										int y = OpenTaiko.Skin.DaniSelect_Exam_Bloc_Y[index];

										int exam_x = OpenTaiko.Skin.DaniSelect_Exam_X[index];
										int exam_y = OpenTaiko.Skin.DaniSelect_Exam_Y[index];

										CTexture tex = null;
										switch (stバー情報[currentSong].List_DanSongs.Count) {
											case 1:
												tex = OpenTaiko.Tx.Dani_Bloc[0];
												break;
											case 2:
											case 3:
												tex = OpenTaiko.Tx.Dani_Bloc[1];
												break;
											case 4:
											case 5:
											case 6:
											default:
												tex = OpenTaiko.Tx.Dani_Bloc[3];
												moveX /= 2;
												moveY /= 2;
												exam_x = OpenTaiko.Skin.DaniSelect_Exam_X_Ex[index];
												exam_y = OpenTaiko.Skin.DaniSelect_Exam_Y_Ex[index];
												break;
										}

										if (i == 0) {
											if (tex != null)
												tex.Opacity = opacity;
											tex?.t2D描画(
												scroll + Anime + x,
												y);
										}

										if (i < 6)
											tExamDraw(scroll + Anime + exam_x + (i * moveX),
												exam_y + (i * moveY),
												stバー情報[currentSong].List_DanSongs[i].Dan_C[j].Value[0], stバー情報[currentSong].List_DanSongs[i].Dan_C[j].GetExamRange());
									} else {
										//全体の条件ですよー

										if (i == 0) {
											if (OpenTaiko.Tx.Dani_Bloc[0] != null)
												OpenTaiko.Tx.Dani_Bloc[0].Opacity = opacity;
											OpenTaiko.Tx.Dani_Bloc[0]?.t2D描画(
											scroll + Anime + OpenTaiko.Skin.DaniSelect_Exam_Bloc_X[index],
											OpenTaiko.Skin.DaniSelect_Exam_Bloc_Y[index]);
										}

										tExamDraw(scroll + Anime + OpenTaiko.Skin.DaniSelect_Exam_X[index], OpenTaiko.Skin.DaniSelect_Exam_Y[index], stバー情報[currentSong].List_DanSongs[0].Dan_C[j].Value[0], stバー情報[currentSong].List_DanSongs[0].Dan_C[j].GetExamRange());

									}

									OpenTaiko.Tx.Dani_Exam_Number.Opacity = 255;
								}
							}

							#endregion

							#region [Exam title]

							if (stバー情報[currentSong].List_DanSongs[0].Dan_C[j] != null) {
								CTexture tmpTex = TitleTextureKey.ResolveTitleTexture(this.ttkExams[(int)stバー情報[currentSong].List_DanSongs[0].Dan_C[j].GetExamType()]);

								tmpTex.Opacity = opacity;
								//tmpTex.t2D下中央基準描画((int)(scroll + 614 + Anime), 452 + index * 88);

								tmpTex.t2D拡大率考慮中央基準描画((int)(scroll + Anime) + OpenTaiko.Skin.DaniSelect_Exam_Title_X[index], OpenTaiko.Skin.DaniSelect_Exam_Title_Y[index]);
							}

							#endregion
						}

						#endregion
					}
					break;
				case CSongListNode.ENodeType.BACKBOX: {
						OpenTaiko.Tx.Dani_Bar_Back?.t2D描画(scroll + Anime, 0);
						break;
					}
				case CSongListNode.ENodeType.BOX: {
						OpenTaiko.Tx.Dani_Bar_Folder_Back?.t2D描画(scroll + Anime, 0);
						OpenTaiko.Tx.Dani_Bar_Folder?.t2D描画(scroll + Anime, 0);
						TitleTextureKey.ResolveTitleTexture(stバー情報[currentSong].ttkタイトル[0])
						.t2D拡大率考慮上中央基準描画((int)(scroll + Anime + OpenTaiko.Skin.DaniSelect_FolderText_X[0]), OpenTaiko.Skin.DaniSelect_FolderText_Y[0]);
						for (int desc = 1; desc < 4; desc++)
							TitleTextureKey.ResolveTitleTexture(stバー情報[currentSong].ttkタイトル[desc])
							.t2D拡大率考慮上中央基準描画((int)(scroll + Anime + OpenTaiko.Skin.DaniSelect_FolderText_X[desc]), OpenTaiko.Skin.DaniSelect_FolderText_Y[desc]);
						break;
					}
				case CSongListNode.ENodeType.RANDOM: {
						OpenTaiko.Tx.Dani_Bar_Random?.t2D描画(scroll + Anime, 0);
						break;
					}
			}
		}

		public void tOpenFolder(CSongListNode song) {
			listSongs = song.list子リスト;
			n現在の選択行 = 0;
			tUpdateSongs();
		}

		public void tCloseFolder(CSongListNode song) {
			listSongs = song.rParentNode.rParentNode.list子リスト;
			n現在の選択行 = 0;
			tUpdateSongs();
		}

		private void tUpdateSongs() {
			stバー情報 = new STバー情報[listSongs.Count];
			this.tバーの初期化();
		}

		private void tバーの初期化() {
			for (int i = 0; i < stバー情報.Length; i++) {
				var song = listSongs[i];

				stバー情報[i].eノード種別 = song.eノード種別;
				switch (song.eノード種別) {
					case CSongListNode.ENodeType.SCORE: {
							stバー情報[i].ttkタイトル = new TitleTextureKey[listSongs[i].DanSongs.Count + 1];
							stバー情報[i].n曲難易度 = new int[listSongs[i].DanSongs.Count];
							stバー情報[i].n曲レベル = new int[listSongs[i].DanSongs.Count];
							for (int j = 0; j < listSongs[i].DanSongs.Count; j++) {
								stバー情報[i].ttkタイトル[j] = new TitleTextureKey(song.DanSongs[j].bTitleShow ? "???" : song.DanSongs[j].Title, pfDanSong, Color.White, Color.Black, 700);
								stバー情報[i].n曲難易度[j] = song.DanSongs[j].Difficulty;
								stバー情報[i].n曲レベル[j] = song.DanSongs[j].Level;
								stバー情報[i].List_DanSongs = song.DanSongs;
							}

							// Two char header, will be used for grade unlocking too
							string tmp = song.ldTitle.GetString("").TrimStringWithTags(2);

							stバー情報[i].ttkタイトル[listSongs[i].DanSongs.Count] = new TitleTextureKey(tmp, pfDanSong, Color.Black, Color.Transparent, 700);

							stバー情報[i].nDanTick = song.arスコア[6].譜面情報.nDanTick;
							stバー情報[i].cDanTickColor = song.arスコア[6].譜面情報.cDanTickColor;

							//stバー情報[i].clearGrade = song.arスコア[6].譜面情報.nクリア[0];
							var TableEntry = OpenTaiko.SaveFileInstances[OpenTaiko.SaveFile].data.tGetSongSelectTableEntry(song.tGetUniqueId());
							stバー情報[i].clearGrade = TableEntry.ClearStatuses[(int)Difficulty.Dan];

							string barCenter = Path.GetDirectoryName(song.arスコア[6].ファイル情報.ファイルの絶対パス) + @$"${Path.DirectorySeparatorChar}Bar_Center.png";
							if (BarTexCache.TryGetValue(barCenter, out CTexture texture1)) {
								stバー情報[i].txBarCenter = texture1;
							} else {
								stバー情報[i].txBarCenter = OpenTaiko.tテクスチャの生成(barCenter);
								BarTexCache.Add(barCenter, stバー情報[i].txBarCenter);
							}

							string danPlate = Path.GetDirectoryName(song.arスコア[6].ファイル情報.ファイルの絶対パス) + @$"${Path.DirectorySeparatorChar}Dan_Plate.png";
							if (BarTexCache.TryGetValue(danPlate, out CTexture texture2)) {
								stバー情報[i].txDanPlate = texture2;
							} else {
								stバー情報[i].txDanPlate = OpenTaiko.tテクスチャの生成(danPlate);
								BarTexCache.Add(danPlate, stバー情報[i].txDanPlate);
							}
						}
						break;
					case CSongListNode.ENodeType.BOX: {
							OpenTaiko.Tx.Dani_Bar_Folder?.tUpdateColor4(CConversion.ColorToColor4(song.BoxColor));

							stバー情報[i].ttkタイトル = new TitleTextureKey[4];
							stバー情報[i].ttkタイトル[0] = new TitleTextureKey(song.ldTitle.GetString(""), pfDanFolder, Color.White, Color.Black, OpenTaiko.Skin.Resolution[0]);
							for (int boxdesc = 0; boxdesc < 3; boxdesc++)
								if (song.strBoxText[boxdesc] != null)
									stバー情報[i].ttkタイトル[boxdesc + 1] = new TitleTextureKey(song.strBoxText[boxdesc].GetString(""), pfDanFolderDesc, song.ForeColor, song.BackColor, OpenTaiko.Skin.Resolution[0]);
								else
									stバー情報[i].ttkタイトル[boxdesc + 1] = new TitleTextureKey("", pfDanFolderDesc, Color.White, Color.Black, OpenTaiko.Skin.Resolution[0]);
							stバー情報[i].cDanTickColor = song.BoxColor;
						}
						break;
					case CSongListNode.ENodeType.BACKBOX: {
							stバー情報[i].ttkタイトル = new TitleTextureKey[1];
							stバー情報[i].ttkタイトル[0] = new TitleTextureKey(CLangManager.LangInstance.GetString("MENU_RETURN"), pfDanSong, Color.White, Color.Black, 700);
							stバー情報[i].cDanTickColor = Color.FromArgb(180, 150, 70);
						}
						break;
					case CSongListNode.ENodeType.RANDOM: {
							stバー情報[i].ttkタイトル = new TitleTextureKey[1];
							stバー情報[i].ttkタイトル[0] = new TitleTextureKey(CLangManager.LangInstance.GetString("SONGSELECT_RANDOM"), pfDanSong, Color.White, Color.Black, 700);
							stバー情報[i].cDanTickColor = Color.FromArgb(150, 250, 255);
						}
						break;
				}

			}
		}

		public void t右に移動() {
			if (n現在の選択行 < stバー情報.Length - 1) {
				OpenTaiko.Skin.soundChangeSFX.tPlay();
				this.bLeftMove = false;
				this.ctDaniMoveAnime.Start(0, 90, 2f, OpenTaiko.Timer);
			}
		}

		public void t左に移動() {
			if (n現在の選択行 > 0) {
				OpenTaiko.Skin.soundChangeSFX.tPlay();
				this.bLeftMove = true;
				this.ctDaniMoveAnime.Start(0, 90, 2f, OpenTaiko.Timer);
			}
		}

		public void tLevelNumberDraw(float x, float y, int num, float scale = 1.0f) {
			/*
            for (int j = 0; j < str.Length; j++)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (str[j] == stLevel[i].ch)
                    {
                        TJAPlayer3.Tx.Dani_Level_Number.t2D描画(x - (str.Length * 14 + 10 * str.Length - str.Length * 14) / 2 + 14 / 2, (float)y - 18 / 2, new RectangleF(stLevel[i].pt.X, stLevel[i].pt.Y, 14, 18));
                        x += 10;
                    }
                }
            }*/

			float width = OpenTaiko.Tx.Dani_Level_Number.sz画像サイズ.Width / 10.0f;
			float height = OpenTaiko.Tx.Dani_Level_Number.sz画像サイズ.Height;

			int[] nums = CConversion.SeparateDigits(num);
			for (int j = 0; j < nums.Length; j++) {
				float offset = j;

				float _x = x - (((OpenTaiko.Skin.DaniSelect_Level_Number_Interval[0] * offset) + (width / 2)) * scale);
				float _y = y - (((OpenTaiko.Skin.DaniSelect_Level_Number_Interval[1] * offset) - (width / 2)) * scale);

				OpenTaiko.Tx.Dani_Level_Number.vcScaleRatio.X = scale;
				OpenTaiko.Tx.Dani_Level_Number.vcScaleRatio.Y = scale;
				OpenTaiko.Tx.Dani_Level_Number.t2D描画(_x, _y,
					new RectangleF(width * nums[j], 0, width, height));
				OpenTaiko.Tx.Dani_Level_Number.vcScaleRatio.X = 1;
				OpenTaiko.Tx.Dani_Level_Number.vcScaleRatio.Y = 1;
			}
		}

		public void tSoulDraw(float x, float y, int num) {
			/*
            TJAPlayer3.Tx.Dani_Soul_Number.t2D描画(x + 16 * str.Length, y - 30 / 2, new RectangleF(0, 30, 80, 30));

            for (int j = 0; j < str.Length; j++)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (str[j] == stSoulNumber[i].ch)
                    {
                        TJAPlayer3.Tx.Dani_Soul_Number.t2D描画(x - (str.Length * 23 + 18 * str.Length - str.Length * 23) / 2 + 23 / 2, (float)y - 30 / 2, new RectangleF(stSoulNumber[i].pt.X, stSoulNumber[i].pt.Y, 23, 30));
                        x += 16;
                    }
                }
            }*/

			int[] nums = CConversion.SeparateDigits(num);

			float width = OpenTaiko.Tx.Dani_Soul_Number.sz画像サイズ.Width / 10.0f;
			float height = OpenTaiko.Tx.Dani_Soul_Number.sz画像サイズ.Height / 2.0f;

			float text_width = OpenTaiko.Skin.DaniSelect_Soul_Number_Text_Width;

			OpenTaiko.Tx.Dani_Soul_Number.t2D描画(x + OpenTaiko.Skin.DaniSelect_Soul_Number_Interval[0] + (width / 2),
				y + OpenTaiko.Skin.DaniSelect_Soul_Number_Interval[1] - (height / 2),
				new RectangleF(0, height, text_width, height));

			for (int j = 0; j < nums.Length; j++) {
				float offset = j;

				float _x = x - (OpenTaiko.Skin.DaniSelect_Soul_Number_Interval[0] * offset) + (width / 2);
				float _y = y - (OpenTaiko.Skin.DaniSelect_Soul_Number_Interval[1] * offset) - (height / 2);

				OpenTaiko.Tx.Dani_Soul_Number.t2D描画(_x, _y,
					new RectangleF(width * nums[j], 0, width, height));
			}
		}

		public void tExamDraw(float x, float y, int num, Exam.Range Range, float scale = 1.0f) {
			/*
            TJAPlayer3.Tx.Dani_Exam_Number.t2D描画(x + 19 * str.Length, y - 24 / 2, new RectangleF(45 * (int)Range, 24, 45, 24));

            for (int j = 0; j < str.Length; j++)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (str[j] == stExamNumber[i].ch)
                    {
                        TJAPlayer3.Tx.Dani_Exam_Number.t2D描画(x, (float)y - 24 / 2, new RectangleF(stExamNumber[i].pt.X, stExamNumber[i].pt.Y, 19, 24));
                        x += 16;
                    }
                }
            }
            */


			int[] nums = CConversion.SeparateDigits(num);

			float width = OpenTaiko.Tx.Dani_Exam_Number.sz画像サイズ.Width / 10.0f;
			float height = OpenTaiko.Tx.Dani_Exam_Number.sz画像サイズ.Height / 2.0f;

			float text_width = OpenTaiko.Skin.DaniSelect_Exam_Number_Text_Width;

			OpenTaiko.Tx.Dani_Exam_Number.vcScaleRatio.X = scale;
			OpenTaiko.Tx.Dani_Exam_Number.vcScaleRatio.Y = scale;

			OpenTaiko.Tx.Dani_Exam_Number.t2D描画(
				x + ((OpenTaiko.Skin.DaniSelect_Exam_Number_Interval[0] + (width / 2)) * scale),
				y + ((OpenTaiko.Skin.DaniSelect_Exam_Number_Interval[1] + (height / 2)) * scale),
				new RectangleF(text_width * (int)Range, height, text_width, height));

			for (int j = 0; j < nums.Length; j++) {
				float offset = j;

				float _x = x - (((OpenTaiko.Skin.DaniSelect_Exam_Number_Interval[0] * offset) + (width / 2)) * scale);
				float _y = y - (((OpenTaiko.Skin.DaniSelect_Exam_Number_Interval[1] * offset) - (height / 2)) * scale);

				OpenTaiko.Tx.Dani_Exam_Number.t2D描画(_x, _y,
					new RectangleF(width * nums[j], 0, width, height));
			}
		}

		#endregion

	}

}
