using FDK;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;

namespace OpenTaiko {
	class CStageHeya : CStage {
		public CStageHeya() {
			base.eStageID = EStage.Heya;
			base.ePhaseID = CStage.EPhase.Common_NORMAL;

			base.ChildActivities.Add(this.actFOtoTitle = new CActFIFOBlack());

			base.ChildActivities.Add(this.PuchiChara = new PuchiChara());
		}


		public override void Activate() {
			if (base.IsActivated)
				return;

			base.ePhaseID = CStage.EPhase.Common_NORMAL;
			this.eフェードアウト完了時の戻り値 = E戻り値.継続;

			ctChara_In = new CCounter();
			//ctChara_Normal = new CCounter(0, TJAPlayer3.Tx.SongSelect_Chara_Normal.Length - 1, 1000 / 45, TJAPlayer3.Timer);

			CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.NORMAL);

			bInSongPlayed = false;

			this.pfHeyaFont = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.Heya_Font_Scale);

			ScrollCounter = new CCounter(0, 1000, 0.15f, OpenTaiko.Timer);

			// 1P, configure later for default 2P
			iPlayer = OpenTaiko.SaveFile;

			#region [Main menu]

			this.ttkMainMenuOpt = new TitleTextureKey[5];

			this.ttkMainMenuOpt[0] = new TitleTextureKey(CLangManager.LangInstance.GetString("MENU_RETURN"), this.pfHeyaFont, Color.White, Color.DarkGreen, 1000);
			this.ttkMainMenuOpt[1] = new TitleTextureKey(CLangManager.LangInstance.GetString("HEYA_PUCHI"), this.pfHeyaFont, Color.White, Color.DarkGreen, 1000);
			this.ttkMainMenuOpt[2] = new TitleTextureKey(CLangManager.LangInstance.GetString("HEYA_CHARA"), this.pfHeyaFont, Color.White, Color.DarkGreen, 1000);
			this.ttkMainMenuOpt[3] = new TitleTextureKey(CLangManager.LangInstance.GetString("HEYA_DAN"), this.pfHeyaFont, Color.White, Color.DarkGreen, 1000);
			this.ttkMainMenuOpt[4] = new TitleTextureKey(CLangManager.LangInstance.GetString("HEYA_NAMEPLATE"), this.pfHeyaFont, Color.White, Color.DarkGreen, 1000);
			//for (int i = 1; i < ttkMainMenuOpt.Length; i++)
			//{
			//    this.ttkMainMenuOpt[i] = new TitleTextureKey(CLangManager.LangInstance.GetString(1030 + i), this.pfHeyaFont, Color.White, Color.DarkGreen, 1000);
			//}

			#endregion

			#region [Dan title]

			int amount = 1;
			if (OpenTaiko.SaveFileInstances[iPlayer].data.DanTitles != null)
				amount += OpenTaiko.SaveFileInstances[iPlayer].data.DanTitles.Count;

			this.ttkDanTitles = new TitleTextureKey[amount];
			this.sDanTitles = new string[amount];

			// Silver Shinjin (default rank) always avaliable by default
			this.ttkDanTitles[0] = new TitleTextureKey("新人", this.pfHeyaFont, Color.White, Color.Black, 1000);
			this.sDanTitles[0] = "新人";

			int idx = 1;
			if (OpenTaiko.SaveFileInstances[iPlayer].data.DanTitles != null) {
				foreach (var item in OpenTaiko.SaveFileInstances[iPlayer].data.DanTitles) {
					this.sDanTitles[idx] = item.Key;
					if (item.Value.isGold == true)
						this.ttkDanTitles[idx] = new TitleTextureKey($"<g.#FFE34A.#EA9622>{item.Key}</g>", this.pfHeyaFont, Color.Gold, Color.Black, 1000);
					else
						this.ttkDanTitles[idx] = new TitleTextureKey(item.Key, this.pfHeyaFont, Color.White, Color.Black, 1000);
					idx++;
				}
			}

			#endregion

			#region [Plate title]

			amount = 1;
			if (OpenTaiko.SaveFileInstances[iPlayer].data.UnlockedNameplateIds != null)
				amount += OpenTaiko.SaveFileInstances[iPlayer].data.UnlockedNameplateIds.Count;

			this.ttkTitles = new TitleTextureKey[amount];
			this.titlesKeys = new int[amount];

			// Wood shoshinsha (default title) always avaliable by default
			this.ttkTitles[0] = new TitleTextureKey("初心者", this.pfHeyaFont, Color.Black, Color.Transparent, 1000);
			this.titlesKeys[0] = 0; // Regular nameplate unlockable start by 1 (Important)

			idx = 1;
			if (OpenTaiko.SaveFileInstances[iPlayer].data.UnlockedNameplateIds != null) {
				foreach (var _ref in OpenTaiko.SaveFileInstances[iPlayer].data.UnlockedNameplateIds) {
					var item = OpenTaiko.Databases.DBNameplateUnlockables.data?[_ref];
					if (item != null) {
						this.ttkTitles[idx] = new TitleTextureKey(item.nameplateInfo.cld.GetString(""), this.pfHeyaFont, Color.Black, Color.Transparent, 1000);
						this.titlesKeys[idx] = _ref;
						idx++;
					}

				}
			}

			#endregion

			// -1 : Main Menu, >= 0 : See Main menu opt
			iCurrentMenu = -1;
			iMainMenuCurrent = 0;

			#region [PuchiChara stuff]

			iPuchiCharaCount = OpenTaiko.Skin.Puchichara_Ptn;

			ttkPuchiCharaNames = new TitleTextureKey[iPuchiCharaCount];
			ttkPuchiCharaAuthors = new TitleTextureKey[iPuchiCharaCount];

			for (int i = 0; i < iPuchiCharaCount; i++) {
				var textColor = HRarity.tRarityToColor(OpenTaiko.Tx.Puchichara[i].metadata.Rarity);
				ttkPuchiCharaNames[i] = new TitleTextureKey(OpenTaiko.Tx.Puchichara[i].metadata.tGetName(), this.pfHeyaFont, textColor, Color.Black, 1000);
				ttkPuchiCharaAuthors[i] = new TitleTextureKey(OpenTaiko.Tx.Puchichara[i].metadata.tGetAuthor(), this.pfHeyaFont, Color.White, Color.Black, 1000);
			}

			#endregion

			#region [Character stuff]

			iCharacterCount = OpenTaiko.Skin.Characters_Ptn;

			ttkCharacterAuthors = new TitleTextureKey[iCharacterCount];
			ttkCharacterNames = new TitleTextureKey[iCharacterCount];

			for (int i = 0; i < iCharacterCount; i++) {
				var textColor = HRarity.tRarityToColor(OpenTaiko.Tx.Characters[i].metadata.Rarity);
				ttkCharacterNames[i] = new TitleTextureKey(OpenTaiko.Tx.Characters[i].metadata.tGetName(), this.pfHeyaFont, textColor, Color.Black, 1000);
				ttkCharacterAuthors[i] = new TitleTextureKey(OpenTaiko.Tx.Characters[i].metadata.tGetAuthor(), this.pfHeyaFont, Color.White, Color.Black, 1000);
			}

			#endregion

			this.tResetOpts();

			this.PuchiChara.IdleAnimation();

			Background = new ScriptBG(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.HEYA}Script.lua"));
			Background.Init();

			base.Activate();
		}

		public override void DeActivate() {
			OpenTaiko.tDisposeSafely(ref Background);

			base.DeActivate();
		}

		public override void CreateManagedResource() {


			base.CreateManagedResource();
		}

		public override void ReleaseManagedResource() {

			base.ReleaseManagedResource();
		}

		public override int Draw() {
			//ctChara_Normal.t進行Loop();
			ctChara_In.Tick();

			ScrollCounter.Tick();

			Background.Update();
			Background.Draw();
			//Heya_Background.t2D描画(0, 0);

			#region [Main menu (Side bar)]

			for (int i = 0; i < this.ttkMainMenuOpt.Length; i++) {
				CTexture tmpTex = TitleTextureKey.ResolveTitleTexture(this.ttkMainMenuOpt[i]);

				if (iCurrentMenu != -1 || iMainMenuCurrent != i) {
					tmpTex.color4 = CConversion.ColorToColor4(Color.DarkGray);
					OpenTaiko.Tx.Heya_Side_Menu?.tUpdateColor4(CConversion.ColorToColor4(Color.DarkGray));
				} else {
					tmpTex.color4 = CConversion.ColorToColor4(Color.White);
					OpenTaiko.Tx.Heya_Side_Menu?.tUpdateColor4(CConversion.ColorToColor4(Color.White));
				}

				OpenTaiko.Tx.Heya_Side_Menu?.t2D拡大率考慮上中央基準描画(OpenTaiko.Skin.Heya_Main_Menu_X[i], OpenTaiko.Skin.Heya_Main_Menu_Y[i]);
				tmpTex.t2D拡大率考慮上中央基準描画(OpenTaiko.Skin.Heya_Main_Menu_X[i] + OpenTaiko.Skin.Heya_Main_Menu_Font_Offset[0], OpenTaiko.Skin.Heya_Main_Menu_Y[i] + OpenTaiko.Skin.Heya_Main_Menu_Font_Offset[1]);
			}

			#endregion

			#region [Background center]

			if (iCurrentMenu >= 0) {
				OpenTaiko.Tx.Heya_Center_Menu_Background?.t2D描画(0, 0);
			}

			#endregion

			#region [Render field]

			float renderRatioX = 1.0f;
			float renderRatioY = 1.0f;

			if (OpenTaiko.Skin.Characters_Resolution[iCharacterCurrent] != null) {
				renderRatioX = OpenTaiko.Skin.Resolution[0] / (float)OpenTaiko.Skin.Characters_Resolution[iCharacterCurrent][0];
				renderRatioY = OpenTaiko.Skin.Resolution[1] / (float)OpenTaiko.Skin.Characters_Resolution[iCharacterCurrent][1];
			}

			if (OpenTaiko.Tx.Characters_Heya_Render[iCharacterCurrent] != null) {
				OpenTaiko.Tx.Characters_Heya_Render[iCharacterCurrent].vcScaleRatio.X = renderRatioX;
				OpenTaiko.Tx.Characters_Heya_Render[iCharacterCurrent].vcScaleRatio.Y = renderRatioY;
			}
			if (iCurrentMenu == 0 || iCurrentMenu == 1) OpenTaiko.Tx.Heya_Render_Field?.t2D描画(0, 0);
			if (iCurrentMenu == 0) OpenTaiko.Tx.Puchichara[iPuchiCharaCurrent].render?.t2D描画(0, 0);
			if (iCurrentMenu == 1) OpenTaiko.Tx.Characters_Heya_Render[iCharacterCurrent]?.t2D描画(OpenTaiko.Skin.Characters_Heya_Render_Offset[iCharacterCurrent][0] * renderRatioX, OpenTaiko.Skin.Characters_Heya_Render_Offset[iCharacterCurrent][1] * renderRatioY);

			#endregion

			#region [Menus display]

			#region [Petit chara]

			if (iCurrentMenu == 0) {
				for (int i = -(OpenTaiko.Skin.Heya_Center_Menu_Box_Count / 2); i < (OpenTaiko.Skin.Heya_Center_Menu_Box_Count / 2) + 1; i++) {
					int pos = (iPuchiCharaCount * 5 + iPuchiCharaCurrent + i) % iPuchiCharaCount;

					if (i != 0) {
						OpenTaiko.Tx.Puchichara[pos].tx?.tUpdateColor4(CConversion.ColorToColor4(Color.DarkGray));
						OpenTaiko.Tx.Heya_Center_Menu_Box_Slot?.tUpdateColor4(CConversion.ColorToColor4(Color.DarkGray));
						OpenTaiko.Tx.Heya_Lock?.tUpdateColor4(CConversion.ColorToColor4(Color.DarkGray));
					} else {
						OpenTaiko.Tx.Puchichara[pos].tx?.tUpdateColor4(CConversion.ColorToColor4(Color.White));
						OpenTaiko.Tx.Heya_Center_Menu_Box_Slot?.tUpdateColor4(CConversion.ColorToColor4(Color.White));
						OpenTaiko.Tx.Heya_Lock?.tUpdateColor4(CConversion.ColorToColor4(Color.White));
					}

					var scroll = DrawBox_Slot(i + (OpenTaiko.Skin.Heya_Center_Menu_Box_Count / 2));

					int puriColumn = pos % 5;
					int puriRow = pos / 5;

					if (OpenTaiko.Tx.Puchichara[pos].tx != null) {
						float puchiScale = OpenTaiko.Skin.Resolution[1] / 1080.0f;

						OpenTaiko.Tx.Puchichara[pos].tx.vcScaleRatio.X = puchiScale;
						OpenTaiko.Tx.Puchichara[pos].tx.vcScaleRatio.Y = puchiScale;
					}

					OpenTaiko.Tx.Puchichara[pos].tx?.t2D拡大率考慮中央基準描画(scroll.Item1 + OpenTaiko.Skin.Heya_Center_Menu_Box_Item_Offset[0],
						scroll.Item2 + OpenTaiko.Skin.Heya_Center_Menu_Box_Item_Offset[1] + (int)(PuchiChara.sineY),
						new Rectangle((PuchiChara.Counter.CurrentValue + 2 * puriColumn) * OpenTaiko.Skin.Game_PuchiChara[0],
						puriRow * OpenTaiko.Skin.Game_PuchiChara[1],
						OpenTaiko.Skin.Game_PuchiChara[0],
						OpenTaiko.Skin.Game_PuchiChara[1]));

					OpenTaiko.Tx.Puchichara[pos].tx?.tUpdateColor4(CConversion.ColorToColor4(Color.White));

					#region [Database related values]

					if (ttkPuchiCharaNames[pos] != null) {
						CTexture tmpTex = TitleTextureKey.ResolveTitleTexture(ttkPuchiCharaNames[pos]);

						tmpTex.t2D拡大率考慮上中央基準描画(scroll.Item1 + OpenTaiko.Skin.Heya_Center_Menu_Box_Name_Offset[0],
							scroll.Item2 + OpenTaiko.Skin.Heya_Center_Menu_Box_Name_Offset[1]);
					}

					if (ttkPuchiCharaAuthors[pos] != null) {
						CTexture tmpTex = TitleTextureKey.ResolveTitleTexture(ttkPuchiCharaAuthors[pos]);

						tmpTex.t2D拡大率考慮上中央基準描画(scroll.Item1 + OpenTaiko.Skin.Heya_Center_Menu_Box_Authors_Offset[0],
							scroll.Item2 + OpenTaiko.Skin.Heya_Center_Menu_Box_Authors_Offset[1]);
					}

					if (OpenTaiko.Tx.Puchichara[pos].unlock != null
						&& !OpenTaiko.SaveFileInstances[iPlayer].data.UnlockedPuchicharas.Contains(OpenTaiko.Skin.Puchicharas_Name[pos]))
						OpenTaiko.Tx.Heya_Lock?.t2D拡大率考慮上中央基準描画(scroll.Item1, scroll.Item2);

					#endregion


				}
			}

			#endregion

			#region [Character]

			if (iCurrentMenu == 1) {
				for (int i = -(OpenTaiko.Skin.Heya_Center_Menu_Box_Count / 2); i < (OpenTaiko.Skin.Heya_Center_Menu_Box_Count / 2) + 1; i++) {
					int pos = (iCharacterCount * 5 + iCharacterCurrent + i) % iCharacterCount;

					float charaRatioX = 1.0f;
					float charaRatioY = 1.0f;

					if (i != 0) {
						OpenTaiko.Tx.Characters_Heya_Preview[pos]?.tUpdateColor4(CConversion.ColorToColor4(Color.DarkGray));
						OpenTaiko.Tx.Heya_Center_Menu_Box_Slot?.tUpdateColor4(CConversion.ColorToColor4(Color.DarkGray));
						OpenTaiko.Tx.Heya_Lock?.tUpdateColor4(CConversion.ColorToColor4(Color.DarkGray));
					} else {
						OpenTaiko.Tx.Characters_Heya_Preview[pos]?.tUpdateColor4(CConversion.ColorToColor4(Color.White));
						OpenTaiko.Tx.Heya_Center_Menu_Box_Slot?.tUpdateColor4(CConversion.ColorToColor4(Color.White));
						OpenTaiko.Tx.Heya_Lock?.tUpdateColor4(CConversion.ColorToColor4(Color.White));
					}

					var scroll = DrawBox_Slot(i + (OpenTaiko.Skin.Heya_Center_Menu_Box_Count / 2));

					if (OpenTaiko.Skin.Characters_Resolution[pos] != null) {
						charaRatioX = OpenTaiko.Skin.Resolution[0] / (float)OpenTaiko.Skin.Characters_Resolution[pos][0];
						charaRatioY = OpenTaiko.Skin.Resolution[1] / (float)OpenTaiko.Skin.Characters_Resolution[pos][1];
					}

					if (OpenTaiko.Tx.Characters_Heya_Preview[pos] != null) {
						OpenTaiko.Tx.Characters_Heya_Preview[pos].vcScaleRatio.X = charaRatioX;
						OpenTaiko.Tx.Characters_Heya_Preview[pos].vcScaleRatio.Y = charaRatioY;
					}

					OpenTaiko.Tx.Characters_Heya_Preview[pos]?.t2D拡大率考慮中央基準描画(scroll.Item1 + OpenTaiko.Skin.Heya_Center_Menu_Box_Item_Offset[0],
						scroll.Item2 + OpenTaiko.Skin.Heya_Center_Menu_Box_Item_Offset[1]);

					OpenTaiko.Tx.Characters_Heya_Preview[pos]?.tUpdateColor4(CConversion.ColorToColor4(Color.White));

					#region [Database related values]

					if (ttkCharacterNames[pos] != null) {
						CTexture tmpTex = TitleTextureKey.ResolveTitleTexture(ttkCharacterNames[pos]);

						tmpTex.t2D拡大率考慮上中央基準描画(scroll.Item1 + OpenTaiko.Skin.Heya_Center_Menu_Box_Name_Offset[0],
							scroll.Item2 + OpenTaiko.Skin.Heya_Center_Menu_Box_Name_Offset[1]);
					}

					if (ttkCharacterAuthors[pos] != null) {
						CTexture tmpTex = TitleTextureKey.ResolveTitleTexture(ttkCharacterAuthors[pos]);

						tmpTex.t2D拡大率考慮上中央基準描画(scroll.Item1 + OpenTaiko.Skin.Heya_Center_Menu_Box_Authors_Offset[0],
							scroll.Item2 + OpenTaiko.Skin.Heya_Center_Menu_Box_Authors_Offset[1]);
					}

					if (OpenTaiko.Tx.Characters[pos].unlock != null
						&& !OpenTaiko.SaveFileInstances[iPlayer].data.UnlockedCharacters.Contains(OpenTaiko.Skin.Characters_DirName[pos]))
						OpenTaiko.Tx.Heya_Lock?.t2D拡大率考慮上中央基準描画(scroll.Item1, scroll.Item2);

					#endregion
				}
			}

			#endregion

			#region [Dan title]

			if (iCurrentMenu == 2) {
				for (int i = -(OpenTaiko.Skin.Heya_Side_Menu_Count / 2); i < (OpenTaiko.Skin.Heya_Side_Menu_Count / 2) + 1; i++) {
					int pos = (this.ttkDanTitles.Length * 5 + iDanTitleCurrent + i) % this.ttkDanTitles.Length;

					CTexture tmpTex = TitleTextureKey.ResolveTitleTexture(this.ttkDanTitles[pos]);

					if (i != 0) {
						tmpTex.color4 = CConversion.ColorToColor4(Color.DarkGray);
						OpenTaiko.Tx.Heya_Side_Menu.color4 = CConversion.ColorToColor4(Color.DarkGray);
						//TJAPlayer3.Tx.NamePlateBase.color4 = CConversion.ColorToColor4(Color.DarkGray);
					} else {
						tmpTex.color4 = CConversion.ColorToColor4(Color.White);
						OpenTaiko.Tx.Heya_Side_Menu.color4 = CConversion.ColorToColor4(Color.White);
						//TJAPlayer3.Tx.NamePlateBase.color4 = CConversion.ColorToColor4(Color.White);
					}

					int danGrade = 0;
					if (pos > 0) {
						danGrade = OpenTaiko.SaveFileInstances[iPlayer].data.DanTitles[this.sDanTitles[pos]].clearStatus;
					}

					var scroll = DrawSide_Menu(i + (OpenTaiko.Skin.Heya_Side_Menu_Count / 2));

					/*
                    TJAPlayer3.NamePlate.tNamePlateDisplayNamePlateBase(
                        scroll.Item1 - TJAPlayer3.Tx.NamePlateBase.szTextureSize.Width / 2,
                        scroll.Item2 - TJAPlayer3.Tx.NamePlateBase.szTextureSize.Height / 24,
                        (8 + danGrade));
                    TJAPlayer3.Tx.NamePlateBase.color4 = CConversion.ColorToColor4(Color.White);

                    tmpTex.t2D拡大率考慮上中央基準描画(scroll.Item1 + TJAPlayer3.Skin.Heya_Side_Menu_Font_Offset[0], scroll.Item2 + TJAPlayer3.Skin.Heya_Side_Menu_Font_Offset[1]);
                    */
					OpenTaiko.NamePlate.lcNamePlate.DrawDan(scroll.Item1, scroll.Item2, 255, danGrade, tmpTex);

				}
			}

			#endregion

			#region [Title plate]

			if (iCurrentMenu == 3) {
				for (int i = -(OpenTaiko.Skin.Heya_Side_Menu_Count / 2); i < (OpenTaiko.Skin.Heya_Side_Menu_Count / 2) + 1; i++) {
					int pos = (this.ttkTitles.Length * 5 + iTitleCurrent + i) % this.ttkTitles.Length;

					CTexture tmpTex = TitleTextureKey.ResolveTitleTexture(this.ttkTitles[pos]);

					if (i != 0) {
						tmpTex.color4 = CConversion.ColorToColor4(Color.DarkGray);
						OpenTaiko.Tx.Heya_Side_Menu.color4 = CConversion.ColorToColor4(Color.DarkGray);
					} else {
						tmpTex.color4 = CConversion.ColorToColor4(Color.White);
						OpenTaiko.Tx.Heya_Side_Menu.color4 = CConversion.ColorToColor4(Color.White);
					}

					var scroll = DrawSide_Menu(i + (OpenTaiko.Skin.Heya_Side_Menu_Count / 2));

					int iType = -1;
					int _rarity = 1;
					int _titleid = -1;

					if (OpenTaiko.SaveFileInstances[iPlayer].data.UnlockedNameplateIds != null &&
						OpenTaiko.SaveFileInstances[iPlayer].data.UnlockedNameplateIds.Contains(this.titlesKeys[pos])) {
						var _dc = OpenTaiko.Databases.DBNameplateUnlockables.data[this.titlesKeys[pos]];
						iType = _dc.nameplateInfo.iType;
						_rarity = HRarity.tRarityToLangInt(_dc.rarity);
						_titleid = this.titlesKeys[pos];
						//iType = TJAPlayer3.SaveFileInstances[iPlayer].data.NamePlateTitles[this.titlesKeys[pos]].iType;
					} else if (pos == 0)
						iType = 0;

					/*
                    if (iType >= 0 && iType < TJAPlayer3.Skin.Config_NamePlate_Ptn_Title)
                    {
                        TJAPlayer3.Tx.NamePlate_Title[iType][TJAPlayer3.NamePlate.ctAnimatedNamePlateTitle.CurrentValue % TJAPlayer3.Skin.Config_NamePlate_Ptn_Title_Boxes[iType]].t2D拡大率考慮上中央基準描画(
                            scroll.Item1,
                            scroll.Item2);

                    }
                    */
					OpenTaiko.NamePlate.lcNamePlate.DrawTitlePlate(scroll.Item1, scroll.Item2, 255, iType, tmpTex, _rarity, _titleid);

					//tmpTex.t2D拡大率考慮上中央基準描画(scroll.Item1 + TJAPlayer3.Skin.Heya_Side_Menu_Font_Offset[0], scroll.Item2 + TJAPlayer3.Skin.Heya_Side_Menu_Font_Offset[1]);

				}
			}

			#endregion

			#endregion

			#region [Description area]

			if (iCurrentMenu >= 0) {
				#region [Unlockable information zone]

				if (this.ttkInfoSection != null && this.ttkInfoSection.str != "")
					OpenTaiko.Tx.Heya_Box?.t2D描画(0, 0);

				if (this.ttkInfoSection != null)
					TitleTextureKey.ResolveTitleTexture(this.ttkInfoSection)
						.t2D拡大率考慮上中央基準描画(OpenTaiko.Skin.Heya_InfoSection[0], OpenTaiko.Skin.Heya_InfoSection[1]);

				#endregion

				#region [Asset description]

				if (this.ttkInfoSection == null || this.ttkInfoSection.str == "") {
					if (iCurrentMenu == 0) CHeyaDisplayAssetInformations.DisplayPuchicharaInfo(this.pfHeyaFont, OpenTaiko.Tx.Puchichara[iPuchiCharaCurrent]);
					if (iCurrentMenu == 1) CHeyaDisplayAssetInformations.DisplayCharacterInfo(this.pfHeyaFont, OpenTaiko.Tx.Characters[iCharacterCurrent]);
				}

				#endregion
			}

			#endregion

			#region [General Chara animations]

			if (!ctChara_In.IsStarted) {
				OpenTaiko.Skin.soundHeyaBGM.tPlay();
				ctChara_In.Start(0, 180, 1.25f, OpenTaiko.Timer);
			}

			#region [ キャラ関連 ]

			if (ctChara_In.CurrentValue != 90) {
				float CharaX = 0f, CharaY = 0f;

				CharaX = -200 + (float)Math.Sin(ctChara_In.CurrentValue / 2 * (Math.PI / 180)) * 200f;
				CharaY = ((float)Math.Sin((90 + (ctChara_In.CurrentValue / 2)) * (Math.PI / 180)) * 150f);

				//int _charaId = TJAPlayer3.NamePlateConfig.data.Character[TJAPlayer3.GetActualPlayer(0)];

				//int chara_x = (int)(TJAPlayer3.Skin.Characters_Menu_X[_charaId][0] + (-200 + CharaX));
				//int chara_y = (int)(TJAPlayer3.Skin.Characters_Menu_Y[_charaId][0] - CharaY);

				int chara_x = (int)CharaX + OpenTaiko.Skin.SongSelect_NamePlate_X[0] + OpenTaiko.Tx.NamePlateBase.szTextureSize.Width / 2;
				int chara_y = OpenTaiko.Skin.SongSelect_NamePlate_Y[0] - (int)CharaY;

				int puchi_x = chara_x + OpenTaiko.Skin.Adjustments_MenuPuchichara_X[0];
				int puchi_y = chara_y + OpenTaiko.Skin.Adjustments_MenuPuchichara_Y[0];

				//TJAPlayer3.Tx.SongSelect_Chara_Normal[ctChara_Normal.n現在の値].Opacity = ctChara_In.n現在の値 * 2;
				//TJAPlayer3.Tx.SongSelect_Chara_Normal[ctChara_Normal.n現在の値].t2D描画(-200 + CharaX, 336 - CharaY);

				CMenuCharacter.tMenuDisplayCharacter(0, chara_x, chara_y, CMenuCharacter.ECharacterAnimation.NORMAL);

				#region [PuchiChara]

				this.PuchiChara.On進行描画(puchi_x, puchi_y, false);

				#endregion
			}

			#endregion

			OpenTaiko.NamePlate.tNamePlateDraw(OpenTaiko.Skin.SongSelect_NamePlate_X[0], OpenTaiko.Skin.SongSelect_NamePlate_Y[0] + 5, 0);

			#endregion



			#region [ Inputs ]

			if (OpenTaiko.InputManager.Keyboard.KeyPressing((int)SlimDXKeys.Key.RightArrow) ||
				OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RightChange)) {
				if (this.tMove(1)) {
					OpenTaiko.Skin.soundChangeSFX.tPlay();
				}
			} else if (OpenTaiko.InputManager.Keyboard.KeyPressing((int)SlimDXKeys.Key.LeftArrow) ||
				  OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LeftChange)) {
				if (this.tMove(-1)) {
					OpenTaiko.Skin.soundChangeSFX.tPlay();
				}
			} else if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return) ||
				  OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.Decide)) {

				#region [Decide]

				ESelectStatus ess = ESelectStatus.SELECTED;

				// Return to main menu
				if (iCurrentMenu == -1 && iMainMenuCurrent == 0) {
					OpenTaiko.Skin.soundHeyaBGM.tStop();
					this.eフェードアウト完了時の戻り値 = E戻り値.タイトルに戻る;
					this.actFOtoTitle.tフェードアウト開始();
					base.ePhaseID = CStage.EPhase.Common_FADEOUT;
				} else if (iCurrentMenu == -1) {
					iCurrentMenu = iMainMenuCurrent - 1;

					if (iCurrentMenu == 0) {
						this.tUpdateUnlockableTextChara();
						this.tUpdateUnlockableTextPuchi();
					}
				} else if (iCurrentMenu == 0) {
					ess = this.tSelectPuchi();

					if (ess == ESelectStatus.SELECTED) {
						//PuchiChara.tGetPuchiCharaIndexByName(p);
						//TJAPlayer3.NamePlateConfig.data.PuchiChara[iPlayer] = TJAPlayer3.Skin.Puchicharas_Name[iPuchiCharaCurrent];// iPuchiCharaCurrent;
						//TJAPlayer3.NamePlateConfig.tApplyHeyaChanges();
						OpenTaiko.SaveFileInstances[iPlayer].data.PuchiChara = OpenTaiko.Skin.Puchicharas_Name[iPuchiCharaCurrent];// iPuchiCharaCurrent;
						OpenTaiko.SaveFileInstances[iPlayer].tApplyHeyaChanges();
						OpenTaiko.Tx.Puchichara[iPuchiCharaCurrent].welcome.tPlay();

						iCurrentMenu = -1;
						this.tResetOpts();
					} else if (ess == ESelectStatus.SUCCESS) {
						//TJAPlayer3.NamePlateConfig.data.UnlockedPuchicharas[iPlayer].Add(TJAPlayer3.Skin.Puchicharas_Name[iPuchiCharaCurrent]);
						//TJAPlayer3.NamePlateConfig.tSpendCoins(TJAPlayer3.Tx.Puchichara[iPuchiCharaCurrent].unlock.Values[0], iPlayer);
						OpenTaiko.SaveFileInstances[iPlayer].data.UnlockedPuchicharas.Add(OpenTaiko.Skin.Puchicharas_Name[iPuchiCharaCurrent]);
						DBSaves.RegisterStringUnlockedAsset(OpenTaiko.SaveFileInstances[iPlayer].data.SaveId, "unlocked_puchicharas", OpenTaiko.Skin.Puchicharas_Name[iPuchiCharaCurrent]);
						OpenTaiko.SaveFileInstances[iPlayer].tSpendCoins(OpenTaiko.Tx.Puchichara[iPuchiCharaCurrent].unlock.Values[0]);

					}
				} else if (iCurrentMenu == 1) {
					ess = this.tSelectChara();

					if (ess == ESelectStatus.SELECTED) {
						//TJAPlayer3.Tx.Loading?.t2D描画(18, 7);

						// Reload character, a bit time expensive but with a O(N) memory complexity instead of O(N * M)
						OpenTaiko.Tx.ReloadCharacter(OpenTaiko.SaveFileInstances[iPlayer].data.Character, iCharacterCurrent, iPlayer);
						OpenTaiko.SaveFileInstances[iPlayer].data.Character = iCharacterCurrent;

						// Update the character
						OpenTaiko.SaveFileInstances[iPlayer].tUpdateCharacterName(OpenTaiko.Skin.Characters_DirName[iCharacterCurrent]);

						// Welcome voice using Sanka
						OpenTaiko.Skin.voiceTitleSanka[iPlayer]?.tPlay();

						CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.NORMAL);

						OpenTaiko.SaveFileInstances[iPlayer].tApplyHeyaChanges();

						iCurrentMenu = -1;
						this.tResetOpts();
					} else if (ess == ESelectStatus.SUCCESS) {
						OpenTaiko.SaveFileInstances[iPlayer].data.UnlockedCharacters.Add(OpenTaiko.Skin.Characters_DirName[iCharacterCurrent]);
						DBSaves.RegisterStringUnlockedAsset(OpenTaiko.SaveFileInstances[iPlayer].data.SaveId, "unlocked_characters", OpenTaiko.Skin.Characters_DirName[iCharacterCurrent]);
						OpenTaiko.SaveFileInstances[iPlayer].tSpendCoins(OpenTaiko.Tx.Characters[iCharacterCurrent].unlock.Values[0]);
						// Play modal animation here ?
					}
				} else if (iCurrentMenu == 2) {
					bool iG = false;
					int cs = 0;

					if (iDanTitleCurrent > 0) {
						iG = OpenTaiko.SaveFileInstances[iPlayer].data.DanTitles[this.sDanTitles[iDanTitleCurrent]].isGold;
						cs = OpenTaiko.SaveFileInstances[iPlayer].data.DanTitles[this.sDanTitles[iDanTitleCurrent]].clearStatus;
					}

					OpenTaiko.SaveFileInstances[iPlayer].data.Dan = this.sDanTitles[iDanTitleCurrent];
					OpenTaiko.SaveFileInstances[iPlayer].data.DanGold = iG;
					OpenTaiko.SaveFileInstances[iPlayer].data.DanType = cs;

					OpenTaiko.NamePlate.tNamePlateRefreshTitles(0);

					OpenTaiko.SaveFileInstances[iPlayer].tApplyHeyaChanges();

					iCurrentMenu = -1;
					this.tResetOpts();
				} else if (iCurrentMenu == 3) {
					OpenTaiko.SaveFileInstances[iPlayer].data.Title = this.ttkTitles[iTitleCurrent].str;

					if (OpenTaiko.SaveFileInstances[iPlayer].data.UnlockedNameplateIds != null
						&& OpenTaiko.SaveFileInstances[iPlayer].data.UnlockedNameplateIds.Contains(this.titlesKeys[iTitleCurrent])) {
						var _dc = OpenTaiko.Databases.DBNameplateUnlockables.data[this.titlesKeys[iTitleCurrent]];
						OpenTaiko.SaveFileInstances[iPlayer].data.TitleType = _dc.nameplateInfo.iType;
						OpenTaiko.SaveFileInstances[iPlayer].data.TitleId = this.titlesKeys[iTitleCurrent];
						OpenTaiko.SaveFileInstances[iPlayer].data.TitleRarityInt = HRarity.tRarityToLangInt(_dc.rarity);
					} else if (iTitleCurrent == 0) {
						OpenTaiko.SaveFileInstances[iPlayer].data.TitleType = 0;
						OpenTaiko.SaveFileInstances[iPlayer].data.TitleId = -1;
						OpenTaiko.SaveFileInstances[iPlayer].data.TitleRarityInt = 1;
					} else {
						OpenTaiko.SaveFileInstances[iPlayer].data.TitleType = -1;
						OpenTaiko.SaveFileInstances[iPlayer].data.TitleId = -1;
						OpenTaiko.SaveFileInstances[iPlayer].data.TitleRarityInt = 1;
					}


					OpenTaiko.NamePlate.tNamePlateRefreshTitles(0);

					OpenTaiko.SaveFileInstances[iPlayer].tApplyHeyaChanges();

					iCurrentMenu = -1;
					this.tResetOpts();
				}

				if (ess == ESelectStatus.SELECTED)
					OpenTaiko.Skin.soundDecideSFX.tPlay();
				else if (ess == ESelectStatus.FAILED)
					OpenTaiko.Skin.soundError.tPlay();
				else
					OpenTaiko.Skin.SoundBanapas.tPlay(); // To change with a more appropriate sfx sooner or later

				#endregion
			} else if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape) ||
				  OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.Cancel)) {

				OpenTaiko.Skin.soundCancelSFX.tPlay();

				if (iCurrentMenu == -1) {
					OpenTaiko.Skin.soundHeyaBGM.tStop();
					this.eフェードアウト完了時の戻り値 = E戻り値.タイトルに戻る;
					this.actFOtoTitle.tフェードアウト開始();
					base.ePhaseID = CStage.EPhase.Common_FADEOUT;
				} else {
					iCurrentMenu = -1;
					this.ttkInfoSection = null;
					this.tResetOpts();
				}


				return 0;
			}

			#endregion

			switch (base.ePhaseID) {
				case CStage.EPhase.Common_FADEOUT:
					if (this.actFOtoTitle.Draw() == 0) {
						break;
					}
					return (int)this.eフェードアウト完了時の戻り値;

			}

			return 0;
		}

		public enum E戻り値 : int {
			継続,
			タイトルに戻る,
			選曲した
		}

		public bool bInSongPlayed;

		private CCounter ctChara_In;
		//private CCounter ctChara_Normal;

		private PuchiChara PuchiChara;

		private int iPlayer;

		private int iMainMenuCurrent;
		private int iPuchiCharaCurrent;

		private TitleTextureKey[] ttkPuchiCharaNames;
		private TitleTextureKey[] ttkPuchiCharaAuthors;
		private TitleTextureKey[] ttkCharacterNames;
		private TitleTextureKey[] ttkCharacterAuthors;
		private TitleTextureKey ttkInfoSection;

		private int iCharacterCurrent;
		private int iDanTitleCurrent;
		private int iTitleCurrent;

		private int iCurrentMenu;

		private void tResetOpts() {
			// Retrieve titles if they exist
			//var _titles = TJAPlayer3.SaveFileInstances[iPlayer].data.NamePlateTitles;
			var _titles = OpenTaiko.SaveFileInstances[iPlayer].data.UnlockedNameplateIds;
			var _title = OpenTaiko.SaveFileInstances[iPlayer].data.Title;
			var _dans = OpenTaiko.SaveFileInstances[iPlayer].data.DanTitles;
			var _dan = OpenTaiko.SaveFileInstances[iPlayer].data.Dan;

			iTitleCurrent = 0;

			// Think of a replacement later
			/*
            if (_titles != null && _titles.ContainsKey(_title)) { }
                iTitleCurrent = _titles.Keys.ToList().IndexOf(_title) + 1;
            */

			iDanTitleCurrent = 0;

			if (_dans != null && _dans.ContainsKey(_dan))
				iDanTitleCurrent = _dans.Keys.ToList().IndexOf(_dan) + 1;

			foreach (var plate in _titles.Select((value, i) => new { i, value })) {
				if (OpenTaiko.SaveFileInstances[iPlayer].data.TitleId == plate.value)
					iTitleCurrent = plate.i + 1;
			}

			iCharacterCurrent = Math.Max(0, Math.Min(OpenTaiko.Skin.Characters_Ptn - 1, OpenTaiko.SaveFileInstances[iPlayer].data.Character));

			//iPuchiCharaCurrent = Math.Max(0, Math.Min(TJAPlayer3.Skin.Puchichara_Ptn - 1, TJAPlayer3.NamePlateConfig.data.PuchiChara[this.iPlayer]));
			iPuchiCharaCurrent = PuchiChara.tGetPuchiCharaIndexByName(this.iPlayer);
		}



		private bool tMove(int off) {
			if (ScrollCounter.CurrentValue < ScrollCounter.EndValue
				&& (OpenTaiko.InputManager.Keyboard.KeyPressing((int)SlimDXKeys.Key.RightArrow)
				|| OpenTaiko.InputManager.Keyboard.KeyPressing((int)SlimDXKeys.Key.LeftArrow)))
				return false;

			ScrollMode = off;
			ScrollCounter.CurrentValue = 0;

			if (iCurrentMenu == -1)
				iMainMenuCurrent = (this.ttkMainMenuOpt.Length + iMainMenuCurrent + off) % this.ttkMainMenuOpt.Length;
			else if (iCurrentMenu == 0) {
				iPuchiCharaCurrent = (iPuchiCharaCount + iPuchiCharaCurrent + off) % iPuchiCharaCount;
				tUpdateUnlockableTextPuchi();
			} else if (iCurrentMenu == 1) {
				iCharacterCurrent = (iCharacterCount + iCharacterCurrent + off) % iCharacterCount;
				tUpdateUnlockableTextChara();
			} else if (iCurrentMenu == 2)
				iDanTitleCurrent = (this.ttkDanTitles.Length + iDanTitleCurrent + off) % this.ttkDanTitles.Length;
			else if (iCurrentMenu == 3)
				iTitleCurrent = (this.ttkTitles.Length + iTitleCurrent + off) % this.ttkTitles.Length;
			else
				return false;

			return true;
		}

		private (int, int) DrawBox_Slot(int i) {
			double value = (1.0 - Math.Sin((((ScrollCounter.CurrentValue) / 2000.0)) * Math.PI));

			int nextIndex = i + ScrollMode;
			nextIndex = Math.Min(OpenTaiko.Skin.Heya_Center_Menu_Box_Count - 1, nextIndex);
			nextIndex = Math.Max(0, nextIndex);

			int x = OpenTaiko.Skin.Heya_Center_Menu_Box_X[i] + (int)((OpenTaiko.Skin.Heya_Center_Menu_Box_X[nextIndex] - OpenTaiko.Skin.Heya_Center_Menu_Box_X[i]) * value);
			int y = OpenTaiko.Skin.Heya_Center_Menu_Box_Y[i] + (int)((OpenTaiko.Skin.Heya_Center_Menu_Box_Y[nextIndex] - OpenTaiko.Skin.Heya_Center_Menu_Box_Y[i]) * value);

			OpenTaiko.Tx.Heya_Center_Menu_Box_Slot?.t2D拡大率考慮上中央基準描画(x, y);
			return (x, y);
		}

		private (int, int) DrawSide_Menu(int i) {
			double value = (1.0 - Math.Sin((((ScrollCounter.CurrentValue) / 2000.0)) * Math.PI));

			int nextIndex = i + ScrollMode;
			nextIndex = Math.Min(OpenTaiko.Skin.Heya_Side_Menu_Count - 1, nextIndex);
			nextIndex = Math.Max(0, nextIndex);

			int x = OpenTaiko.Skin.Heya_Side_Menu_X[i] + (int)((OpenTaiko.Skin.Heya_Side_Menu_X[nextIndex] - OpenTaiko.Skin.Heya_Side_Menu_X[i]) * value);
			int y = OpenTaiko.Skin.Heya_Side_Menu_Y[i] + (int)((OpenTaiko.Skin.Heya_Side_Menu_Y[nextIndex] - OpenTaiko.Skin.Heya_Side_Menu_Y[i]) * value);

			OpenTaiko.Tx.Heya_Side_Menu.t2D拡大率考慮上中央基準描画(x, y);
			return (x, y);
		}

		#region [Unlockables]

		/*
         *  FAILED : Selection/Purchase failed (failed condition)
         *  SUCCESS : Purchase succeed (without selection)
         *  SELECTED : Selection succeed
        */
		private enum ESelectStatus {
			FAILED,
			SUCCESS,
			SELECTED
		};


		#region [Chara unlockables]

		private void tUpdateUnlockableTextChara() {
			#region [Check unlockable]

			if (OpenTaiko.Tx.Characters[iCharacterCurrent].unlock != null
				&& !OpenTaiko.SaveFileInstances[iPlayer].data.UnlockedCharacters.Contains(OpenTaiko.Skin.Characters_DirName[iCharacterCurrent])) {
				string _cond = "???";
				if (HRarity.tRarityToModalInt(OpenTaiko.Tx.Characters[iCharacterCurrent].metadata.Rarity)
					< HRarity.tRarityToModalInt("Epic"))
					_cond = OpenTaiko.Tx.Characters[iCharacterCurrent].unlock.tConditionMessage();
				this.ttkInfoSection = new TitleTextureKey(_cond, this.pfHeyaFont, Color.White, Color.Black, 1000);
			} else
				this.ttkInfoSection = null;

			#endregion
		}
		private ESelectStatus tSelectChara() {
			// Add "If unlocked" to select directly

			if (OpenTaiko.Tx.Characters[iCharacterCurrent].unlock != null
				&& !OpenTaiko.SaveFileInstances[iPlayer].data.UnlockedCharacters.Contains(OpenTaiko.Skin.Characters_DirName[iCharacterCurrent])) {
				(bool, string?) response = OpenTaiko.Tx.Characters[iCharacterCurrent].unlock.tConditionMetWrapper(OpenTaiko.SaveFile);
				//TJAPlayer3.Tx.Characters[iCharacterCurrent].unlock.tConditionMet(
				//new int[] { TJAPlayer3.SaveFileInstances[TJAPlayer3.SaveFile].data.Medals });

				Color responseColor = (response.Item1) ? Color.Lime : Color.Red;

				// Send coins here for the unlock, considering that only coin-paid puchicharas can be unlocked directly from the Heya menu

				this.ttkInfoSection = new TitleTextureKey(response.Item2 ?? this.ttkInfoSection.str, this.pfHeyaFont, responseColor, Color.Black, 1000);

				return (response.Item1) ? ESelectStatus.SUCCESS : ESelectStatus.FAILED;
			}

			this.ttkInfoSection = null;
			return ESelectStatus.SELECTED;
		}

		#endregion

		#region [Puchi unlockables]
		private void tUpdateUnlockableTextPuchi() {
			#region [Check unlockable]

			if (OpenTaiko.Tx.Puchichara[iPuchiCharaCurrent].unlock != null
				&& !OpenTaiko.SaveFileInstances[iPlayer].data.UnlockedPuchicharas.Contains(OpenTaiko.Skin.Puchicharas_Name[iPuchiCharaCurrent])) {
				string _cond = "???";
				if (HRarity.tRarityToModalInt(OpenTaiko.Tx.Puchichara[iPuchiCharaCurrent].metadata.Rarity)
					< HRarity.tRarityToModalInt("Epic"))
					_cond = OpenTaiko.Tx.Puchichara[iPuchiCharaCurrent].unlock.tConditionMessage();
				this.ttkInfoSection = new TitleTextureKey(_cond, this.pfHeyaFont, Color.White, Color.Black, 1000);
			} else
				this.ttkInfoSection = null;

			#endregion
		}

		private ESelectStatus tSelectPuchi() {
			// Add "If unlocked" to select directly

			if (OpenTaiko.Tx.Puchichara[iPuchiCharaCurrent].unlock != null
				&& !OpenTaiko.SaveFileInstances[iPlayer].data.UnlockedPuchicharas.Contains(OpenTaiko.Skin.Puchicharas_Name[iPuchiCharaCurrent])) {
				(bool, string?) response = OpenTaiko.Tx.Puchichara[iPuchiCharaCurrent].unlock.tConditionMetWrapper(OpenTaiko.SaveFile);
				//tConditionMet(
				//new int[] { TJAPlayer3.SaveFileInstances[TJAPlayer3.SaveFile].data.Medals });

				Color responseColor = (response.Item1) ? Color.Lime : Color.Red;

				// Send coins here for the unlock, considering that only coin-paid puchicharas can be unlocked directly from the Heya menu

				this.ttkInfoSection = new TitleTextureKey(response.Item2 ?? this.ttkInfoSection.str, this.pfHeyaFont, responseColor, Color.Black, 1000);

				return (response.Item1) ? ESelectStatus.SUCCESS : ESelectStatus.FAILED;
			}

			this.ttkInfoSection = null;
			return ESelectStatus.SELECTED;
		}

		#endregion

		#endregion

		private ScriptBG Background;

		private TitleTextureKey[] ttkMainMenuOpt;
		private CCachedFontRenderer pfHeyaFont;

		private TitleTextureKey[] ttkDanTitles;
		private string[] sDanTitles;

		private TitleTextureKey[] ttkTitles;
		private int[] titlesKeys;

		private int iPuchiCharaCount;
		private int iCharacterCount;

		private CCounter ScrollCounter;
		private const int SideInterval_X = 10;
		private const int SideInterval_Y = 70;
		private int ScrollMode;

		public E戻り値 eフェードアウト完了時の戻り値;

		public CActFIFOBlack actFOtoTitle;
	}
}
