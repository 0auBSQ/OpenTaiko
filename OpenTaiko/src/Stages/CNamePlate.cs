using System.Drawing;
using FDK;
using static TJAPlayer3.CActSelect曲リスト;

namespace TJAPlayer3 {
	class CNamePlate {
		public void RefleshSkin() {
			for (int player = 0; player < 5; player++) {
				this.pfName[player]?.Dispose();

				if (TJAPlayer3.SaveFileInstances[player].data.Title == "" || TJAPlayer3.SaveFileInstances[player].data.Title == null)
					this.pfName[player] = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.NamePlate_Font_Name_Size_Normal);
				else
					this.pfName[player] = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.NamePlate_Font_Name_Size_WithTitle);
			}

			this.pfTitle?.Dispose();
			this.pfdan?.Dispose();

			this.pfTitle = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.NamePlate_Font_Title_Size);
			this.pfdan = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.NamePlate_Font_Dan_Size);
		}

		public CNamePlate() {
			RefleshSkin();

			for (int player = 0; player < 5; player++) {
				if (TJAPlayer3.SaveFileInstances[player].data.DanType < 0) TJAPlayer3.SaveFileInstances[player].data.DanType = 0;
				else if (TJAPlayer3.SaveFileInstances[player].data.DanType > 2) TJAPlayer3.SaveFileInstances[player].data.DanType = 2;

				if (TJAPlayer3.SaveFileInstances[player].data.TitleType < 0) TJAPlayer3.SaveFileInstances[player].data.TitleType = 0;

				tNamePlateRefreshTitles(player);
			}

			ctNamePlateEffect = new CCounter(0, 120, 16.6f, TJAPlayer3.Timer);
			ctAnimatedNamePlateTitle = new CCounter(0, 10000, 60.0f, TJAPlayer3.Timer);
		}

		public void tNamePlateDisplayNamePlateBase(int x, int y, int item) {
			int namePlateBaseX = TJAPlayer3.Tx.NamePlateBase.szTextureSize.Width;
			int namePlateBaseY = TJAPlayer3.Tx.NamePlateBase.szTextureSize.Height / 12;

			TJAPlayer3.Tx.NamePlateBase?.t2D描画(x, y, new RectangleF(0, item * namePlateBaseY, namePlateBaseX, namePlateBaseY));

		}

		public void tNamePlateDisplayNamePlate_Extension(int x, int y, int item) {
			int namePlateBaseX = TJAPlayer3.Tx.NamePlate_Extension.szTextureSize.Width;
			int namePlateBaseY = TJAPlayer3.Tx.NamePlate_Extension.szTextureSize.Height / 12;

			TJAPlayer3.Tx.NamePlate_Extension?.t2D描画(x, y, new RectangleF(0, item * namePlateBaseY, namePlateBaseX, namePlateBaseY));

		}

		public void tNamePlateRefreshTitles(int player) {
			int actualPlayer = TJAPlayer3.GetActualPlayer(player);

			string[] stages = { "初", "二", "三", "四", "五", "六", "七", "八", "九", "極" };

			string name = CLangManager.LangInstance.GetString("AI_NAME");
			string title = CLangManager.LangInstance.GetString("AI_TITLE");
			string dan = stages[Math.Max(0, TJAPlayer3.ConfigIni.nAILevel - 1)] + "面";

			if (!TJAPlayer3.ConfigIni.bAIBattleMode || actualPlayer == 0) {
				name = TJAPlayer3.SaveFileInstances[player].data.Name;
				title = TJAPlayer3.SaveFileInstances[player].data.Title;
				dan = TJAPlayer3.SaveFileInstances[player].data.Dan;
			}

			txTitle[player] = TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(new TitleTextureKey(title, pfTitle, Color.Black, Color.Empty, 1000));
			txName[player] = TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(new TitleTextureKey(name, pfName[player], Color.White, Color.Black, 1000));
			if (TJAPlayer3.SaveFileInstances[player].data.DanGold) txdan[player] = TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(new TitleTextureKey($"<g.#FFE34A.#EA9622>{dan}</g>", pfdan, Color.White, Color.Black, 1000));
			else txdan[player] = TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(new TitleTextureKey(dan, pfdan, Color.White, Color.Black, 1000));
		}


		public void tNamePlateDraw(int x, int y, int player, bool bTitle = false, int Opacity = 255) {
			float resolutionScaleX = TJAPlayer3.Skin.Resolution[0] / 1280.0f;
			float resolutionScaleY = TJAPlayer3.Skin.Resolution[1] / 720.0f;

			int basePlayer = player;
			player = TJAPlayer3.GetActualPlayer(player);

			tNamePlateRefreshTitles(player);

			ctNamePlateEffect.TickLoop();
			ctAnimatedNamePlateTitle.TickLoop();

			this.txName[player].Opacity = Opacity;
			this.txTitle[player].Opacity = Opacity;
			this.txdan[player].Opacity = Opacity;

			TJAPlayer3.Tx.NamePlateBase.Opacity = Opacity;


			for (int i = 0; i < 5; i++)
				TJAPlayer3.Tx.NamePlate_Effect[i].Opacity = Opacity;

			// White background
			tNamePlateDisplayNamePlateBase(x, y, 3);

			// Upper (title) plate
			if (TJAPlayer3.SaveFileInstances[player].data.Title != "" && TJAPlayer3.SaveFileInstances[player].data.Title != null) {
				int tt = TJAPlayer3.SaveFileInstances[player].data.TitleType;
				if (tt >= 0 && tt < TJAPlayer3.Skin.Config_NamePlate_Ptn_Title) {
					var _tex = TJAPlayer3.Tx.NamePlate_Title[tt][ctAnimatedNamePlateTitle.CurrentValue % TJAPlayer3.Skin.Config_NamePlate_Ptn_Title_Boxes[tt]];

					if (_tex != null) {
						_tex.Opacity = Opacity;
						_tex.t2D描画(x - (2 * resolutionScaleX), y - (2 * resolutionScaleY));
					}
				}
			}

			// Dan plate
			if (TJAPlayer3.SaveFileInstances[player].data.Dan != "" && TJAPlayer3.SaveFileInstances[player].data.Dan != null) {
				tNamePlateDisplayNamePlateBase(x, y, 7);
				tNamePlateDisplayNamePlateBase(x, y, (8 + TJAPlayer3.SaveFileInstances[player].data.DanType));
			}

			// Glow
			tNamePlateDraw(player, x, y, Opacity);

			// Player number
			if (TJAPlayer3.PlayerSide == 0 || TJAPlayer3.ConfigIni.nPlayerCount > 1) {
				if (basePlayer < 2) {
					tNamePlateDisplayNamePlateBase(x, y, basePlayer == 1 ? 2 : 0);
				} else {
					tNamePlateDisplayNamePlate_Extension(x, y, basePlayer - 2);
				}
			} else
				tNamePlateDisplayNamePlateBase(x, y, 1);

			// Name text squash (to add to skin config)
			if (TJAPlayer3.SaveFileInstances[player].data.Dan != "" && TJAPlayer3.SaveFileInstances[player].data.Dan != null) {
				if (txName[player].szTextureSize.Width > TJAPlayer3.Skin.NamePlate_Name_Width_Full)
					txName[player].vcScaleRatio.X = (float)TJAPlayer3.Skin.NamePlate_Name_Width_Full / txName[player].szTextureSize.Width;
			} else {
				if (txName[player].szTextureSize.Width > TJAPlayer3.Skin.NamePlate_Name_Width_Normal)
					txName[player].vcScaleRatio.X = (float)TJAPlayer3.Skin.NamePlate_Name_Width_Normal / txName[player].szTextureSize.Width;
			}

			// Dan text squash (to add to skin config)
			if (txdan[player].szTextureSize.Width > TJAPlayer3.Skin.NamePlate_Dan_Width)
				txdan[player].vcScaleRatio.X = (float)TJAPlayer3.Skin.NamePlate_Dan_Width / txdan[player].szTextureSize.Width;

			// Dan text
			if (TJAPlayer3.SaveFileInstances[player].data.Dan != "" && TJAPlayer3.SaveFileInstances[player].data.Dan != null) {
				this.txdan[player].t2D拡大率考慮中央基準描画(x + TJAPlayer3.Skin.NamePlate_Dan_Offset[0], y + TJAPlayer3.Skin.NamePlate_Dan_Offset[1]);
			}

			// Title text
			if (TJAPlayer3.SaveFileInstances[player].data.Title != "" && TJAPlayer3.SaveFileInstances[player].data.Title != null) {
				if (txTitle[player].szTextureSize.Width > TJAPlayer3.Skin.NamePlate_Title_Width) {
					txTitle[player].vcScaleRatio.X = (float)TJAPlayer3.Skin.NamePlate_Title_Width / txTitle[player].szTextureSize.Width;
					txTitle[player].vcScaleRatio.Y = (float)TJAPlayer3.Skin.NamePlate_Title_Width / txTitle[player].szTextureSize.Width;
				}

				txTitle[player].t2D拡大率考慮中央基準描画(x + TJAPlayer3.Skin.NamePlate_Title_Offset[0], y + TJAPlayer3.Skin.NamePlate_Title_Offset[1]);

				// Name text
				if (TJAPlayer3.SaveFileInstances[player].data.Dan == "" || TJAPlayer3.SaveFileInstances[player].data.Dan == null)
					this.txName[player].t2D拡大率考慮中央基準描画(x + TJAPlayer3.Skin.NamePlate_Name_Offset_WithTitle[0], y + TJAPlayer3.Skin.NamePlate_Name_Offset_WithTitle[1]);
				else
					this.txName[player].t2D拡大率考慮中央基準描画(x + TJAPlayer3.Skin.NamePlate_Name_Offset_Full[0], y + TJAPlayer3.Skin.NamePlate_Name_Offset_Full[1]);
			} else
				this.txName[player].t2D拡大率考慮中央基準描画(x + TJAPlayer3.Skin.NamePlate_Name_Offset_Normal[0], y + TJAPlayer3.Skin.NamePlate_Name_Offset_Normal[1]);


			// Overlap frame
			tNamePlateDisplayNamePlateBase(x, y, 4);
		}

		private void tNamePlateDraw(int player, int x, int y, int Opacity = 255) {
			if (Opacity == 0)
				return;

			float resolutionScaleX = TJAPlayer3.Skin.Resolution[0] / 1280.0f;
			float resolutionScaleY = TJAPlayer3.Skin.Resolution[1] / 720.0f;

			if (TJAPlayer3.SaveFileInstances[player].data.TitleType != 0 && !TJAPlayer3.ConfigIni.SimpleMode) {
				int Type = TJAPlayer3.SaveFileInstances[player].data.TitleType - 1;
				if (this.ctNamePlateEffect.CurrentValue <= 10) {
					tNamePlateStarDraw(player, 1.0f - (ctNamePlateEffect.CurrentValue / 10f * 1.0f), x + (63 * resolutionScaleX), y + (25 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 3 && this.ctNamePlateEffect.CurrentValue <= 10) {
					tNamePlateStarDraw(player, 1.0f - ((ctNamePlateEffect.CurrentValue - 3) / 7f * 1.0f), x + (38 * resolutionScaleX), y + (7 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 6 && this.ctNamePlateEffect.CurrentValue <= 10) {
					tNamePlateStarDraw(player, 1.0f - ((ctNamePlateEffect.CurrentValue - 6) / 4f * 1.0f), x + (51 * resolutionScaleX), y + (5 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 8 && this.ctNamePlateEffect.CurrentValue <= 10) {
					tNamePlateStarDraw(player, 0.3f - ((ctNamePlateEffect.CurrentValue - 8) / 2f * 0.3f), x + (110 * resolutionScaleX), y + (25 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 11 && this.ctNamePlateEffect.CurrentValue <= 13) {
					tNamePlateStarDraw(player, 1.0f - ((ctNamePlateEffect.CurrentValue - 11) / 2f * 1.0f), x + (38 * resolutionScaleX), y + (7 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 11 && this.ctNamePlateEffect.CurrentValue <= 15) {
					tNamePlateStarDraw(player, 1.0f, x + (51 * resolutionScaleX), y + 5);
				}
				if (this.ctNamePlateEffect.CurrentValue >= 11 && this.ctNamePlateEffect.CurrentValue <= 17) {
					tNamePlateStarDraw(player, 1.0f - ((ctNamePlateEffect.CurrentValue - 11) / 7f * 1.0f), x + (110 * resolutionScaleX), y + (25 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 16 && this.ctNamePlateEffect.CurrentValue <= 20) {
					tNamePlateStarDraw(player, 0.2f - ((ctNamePlateEffect.CurrentValue - 16) / 4f * 0.2f), x + (63 * resolutionScaleX), y + (25 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 17 && this.ctNamePlateEffect.CurrentValue <= 20) {
					tNamePlateStarDraw(player, 1.0f - ((ctNamePlateEffect.CurrentValue - 17) / 3f * 1.0f), x + (99 * resolutionScaleX), y + (1 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 20 && this.ctNamePlateEffect.CurrentValue <= 24) {
					tNamePlateStarDraw(player, 0.4f, x + (63 * resolutionScaleX), y + 25);
				}
				if (this.ctNamePlateEffect.CurrentValue >= 20 && this.ctNamePlateEffect.CurrentValue <= 25) {
					tNamePlateStarDraw(player, 1.0f, x + (99 * resolutionScaleX), y + 1);
				}
				if (this.ctNamePlateEffect.CurrentValue >= 20 && this.ctNamePlateEffect.CurrentValue <= 30) {
					tNamePlateStarDraw(player, 0.5f - ((this.ctNamePlateEffect.CurrentValue - 20) / 10f * 0.5f), x + (152 * resolutionScaleX), y + (7 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 31 && this.ctNamePlateEffect.CurrentValue <= 37) {
					tNamePlateStarDraw(player, 0.5f - ((this.ctNamePlateEffect.CurrentValue - 31) / 6f * 0.5f), x + (176 * resolutionScaleX), y + (8 * resolutionScaleY));
					tNamePlateStarDraw(player, 1.0f - ((this.ctNamePlateEffect.CurrentValue - 31) / 6f * 1.0f), x + (175 * resolutionScaleX), y + (25 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 31 && this.ctNamePlateEffect.CurrentValue <= 40) {
					tNamePlateStarDraw(player, 0.9f - ((this.ctNamePlateEffect.CurrentValue - 31) / 9f * 0.9f), x + (136 * resolutionScaleX), y + (24 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 34 && this.ctNamePlateEffect.CurrentValue <= 40) {
					tNamePlateStarDraw(player, 0.7f - ((this.ctNamePlateEffect.CurrentValue - 34) / 6f * 0.7f), x + (159 * resolutionScaleX), y + (25 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 41 && this.ctNamePlateEffect.CurrentValue <= 42) {
					tNamePlateStarDraw(player, 0.7f, x + (159 * resolutionScaleX), y + (25 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 43 && this.ctNamePlateEffect.CurrentValue <= 50) {
					tNamePlateStarDraw(player, 0.8f - ((this.ctNamePlateEffect.CurrentValue - 43) / 7f * 0.8f), x + (196 * resolutionScaleX), y + (23 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 51 && this.ctNamePlateEffect.CurrentValue <= 57) {
					tNamePlateStarDraw(player, 0.8f - ((this.ctNamePlateEffect.CurrentValue - 51) / 6f * 0.8f), x + (51 * resolutionScaleX), y + (5 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 51 && this.ctNamePlateEffect.CurrentValue <= 52) {
					tNamePlateStarDraw(player, 0.2f, x + (166 * resolutionScaleX), y + (22 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 51 && this.ctNamePlateEffect.CurrentValue <= 53) {
					tNamePlateStarDraw(player, 0.8f, x + (136 * resolutionScaleX), y + (24 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 51 && this.ctNamePlateEffect.CurrentValue <= 55) {
					tNamePlateStarDraw(player, 1.0f, x + (176 * resolutionScaleX), y + (8 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 51 && this.ctNamePlateEffect.CurrentValue <= 55) {
					tNamePlateStarDraw(player, 1.0f, x + (176 * resolutionScaleX), y + (8 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 61 && this.ctNamePlateEffect.CurrentValue <= 70) {
					tNamePlateStarDraw(player, 1.0f - ((this.ctNamePlateEffect.CurrentValue - 61) / 9f * 1.0f), x + (196 * resolutionScaleX), y + (23 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 61 && this.ctNamePlateEffect.CurrentValue <= 67) {
					tNamePlateStarDraw(player, 0.7f - ((this.ctNamePlateEffect.CurrentValue - 61) / 6f * 0.7f), x + (214 * resolutionScaleX), y + (14 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 63 && this.ctNamePlateEffect.CurrentValue <= 70) {
					tNamePlateStarDraw(player, 0.5f - ((this.ctNamePlateEffect.CurrentValue - 63) / 7f * 0.5f), x + (129 * resolutionScaleX), y + (24 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 63 && this.ctNamePlateEffect.CurrentValue <= 70) {
					tNamePlateStarDraw(player, 0.5f - ((this.ctNamePlateEffect.CurrentValue - 63) / 7f * 0.5f), x + (129 * resolutionScaleX), y + (24 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 65 && this.ctNamePlateEffect.CurrentValue <= 70) {
					tNamePlateStarDraw(player, 0.8f - ((this.ctNamePlateEffect.CurrentValue - 65) / 5f * 0.8f), x + (117 * resolutionScaleX), y + (7 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 71 && this.ctNamePlateEffect.CurrentValue <= 72) {
					tNamePlateStarDraw(player, 0.8f, x + (151 * resolutionScaleX), y + (25 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 71 && this.ctNamePlateEffect.CurrentValue <= 74) {
					tNamePlateStarDraw(player, 0.8f, x + (117 * resolutionScaleX), y + (7 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 85 && this.ctNamePlateEffect.CurrentValue <= 112) {
					TJAPlayer3.Tx.NamePlate_Effect[4].Opacity = (int)(1400 - (this.ctNamePlateEffect.CurrentValue - 85) * 50f);

					TJAPlayer3.Tx.NamePlate_Effect[4].t2D描画(x + ((((this.ctNamePlateEffect.CurrentValue - 85) * (150f / 27f))) * resolutionScaleX), y + (7 * resolutionScaleY));
				}
				if (this.ctNamePlateEffect.CurrentValue >= 105 && this.ctNamePlateEffect.CurrentValue <= 120) {
					/*
                    TJAPlayer3.Tx.NamePlate_Effect[TJAPlayer3.NamePlateConfig.data.TitleType[player] + 1].Opacity = this.ctNamePlateEffect.n現在の値 >= 112 ? (int)(255 - (this.ctNamePlateEffect.n現在の値 - 112) * 31.875f) : 255;
                    TJAPlayer3.Tx.NamePlate_Effect[TJAPlayer3.NamePlateConfig.data.TitleType[player] + 1].vc拡大縮小倍率.X = this.ctNamePlateEffect.n現在の値 >= 112 ? 1.0f : (this.ctNamePlateEffect.n現在の値 - 105) / 8f;
                    TJAPlayer3.Tx.NamePlate_Effect[TJAPlayer3.NamePlateConfig.data.TitleType[player] + 1].vc拡大縮小倍率.Y = this.ctNamePlateEffect.n現在の値 >= 112 ? 1.0f : (this.ctNamePlateEffect.n現在の値 - 105) / 8f;
                    TJAPlayer3.Tx.NamePlate_Effect[TJAPlayer3.NamePlateConfig.data.TitleType[player] + 1].t2D拡大率考慮中央基準描画(x + 193, y + 6);
                    */

					int tt = TJAPlayer3.SaveFileInstances[player].data.TitleType;
					if (tt >= 0 && tt < TJAPlayer3.Skin.Config_NamePlate_Ptn_Title && TJAPlayer3.Tx.NamePlate_Title_Big[tt] != null) {
						TJAPlayer3.Tx.NamePlate_Title_Big[tt].Opacity = this.ctNamePlateEffect.CurrentValue >= 112 ? (int)(255 - (this.ctNamePlateEffect.CurrentValue - 112) * 31.875f) : 255;
						TJAPlayer3.Tx.NamePlate_Title_Big[tt].vcScaleRatio.X = this.ctNamePlateEffect.CurrentValue >= 112 ? 1.0f : (this.ctNamePlateEffect.CurrentValue - 105) / 8f;
						TJAPlayer3.Tx.NamePlate_Title_Big[tt].vcScaleRatio.Y = this.ctNamePlateEffect.CurrentValue >= 112 ? 1.0f : (this.ctNamePlateEffect.CurrentValue - 105) / 8f;
						TJAPlayer3.Tx.NamePlate_Title_Big[tt].t2D拡大率考慮中央基準描画(x + (193 * resolutionScaleX), y + (6 * resolutionScaleY));
					}

				}
			}
		}

		private void tNamePlateStarDraw(int player, float Scale, float x, float y) {
			/*
            TJAPlayer3.Tx.NamePlate_Effect[TJAPlayer3.NamePlateConfig.data.TitleType[player] - 1].vc拡大縮小倍率.X = Scale;
            TJAPlayer3.Tx.NamePlate_Effect[TJAPlayer3.NamePlateConfig.data.TitleType[player] - 1].vc拡大縮小倍率.Y = Scale;
            TJAPlayer3.Tx.NamePlate_Effect[TJAPlayer3.NamePlateConfig.data.TitleType[player] - 1].t2D拡大率考慮中央基準描画(x, y);
            */
			int tt = TJAPlayer3.SaveFileInstances[player].data.TitleType;
			if (tt >= 0 && tt < TJAPlayer3.Skin.Config_NamePlate_Ptn_Title && TJAPlayer3.Tx.NamePlate_Title_Small[tt] != null) {
				TJAPlayer3.Tx.NamePlate_Title_Small[tt].vcScaleRatio.X = Scale;
				TJAPlayer3.Tx.NamePlate_Title_Small[tt].vcScaleRatio.Y = Scale;
				TJAPlayer3.Tx.NamePlate_Title_Small[tt].t2D拡大率考慮中央基準描画(x, y);
			}

		}

		private CCachedFontRenderer[] pfName = new CCachedFontRenderer[5];
		private CCachedFontRenderer pfTitle;
		private CCachedFontRenderer pfdan;
		private CCounter ctNamePlateEffect;

		public CCounter ctAnimatedNamePlateTitle;

		private CTexture[] txName = new CTexture[5];
		private CTexture[] txTitle = new CTexture[5];
		private CTexture[] txdan = new CTexture[5];
	}
}
