using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;
using FDK;
using System.Reflection;
using static TJAPlayer3.CActSelect曲リスト;

namespace TJAPlayer3
{
	internal class CStageタイトル : CStage
	{
		// コンストラクタ

		public CStageタイトル()
		{
			base.eStageID = CStage.EStage.Title;
			base.IsDeActivated = true;
			base.ChildActivities.Add(this.actFIfromSetup = new CActFIFOBlack());
			base.ChildActivities.Add(this.actFI = new CActFIFOBlack());
			base.ChildActivities.Add(this.actFO = new CActFIFOBlack());

			base.ChildActivities.Add(this.PuchiChara = new PuchiChara());

		}


		// CStage 実装

		public override void Activate()
		{
			Trace.TraceInformation("タイトルステージを活性化します。");
			Trace.Indent();
			try
			{
				UnloadSaveFile();

				this.PuchiChara.IdleAnimation();

				SkipSaveFileStep();

				usedMenus = new int[] {
					0,
					1,
					2,
					10,
					5,
					3,
					9,
					8,
					6,
					7,

					// -- Debug
					/*
					11,
					12,
					13,
					*/
				};

				usedMenusCount = usedMenus.Length;

				usedMenusPos = new int[usedMenusCount];
				for (int i = 0; i < usedMenusCount; i++)
				{
					usedMenusPos[i] = i + 1 - n現在の選択行モード選択;
				}

				// Init Menus
				tReloadMenus();

				Background = new ScriptBG(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.TITLE}Script.lua"));
				Background.Init();


				b音声再生 = false;
				if (bSaveFileLoaded == false)
					TJAPlayer3.Skin.soundEntry.tPlay();
				if (TJAPlayer3.ConfigIni.bBGM音を発声する)
					TJAPlayer3.Skin.bgmタイトルイン.tPlay();
				base.Activate();
			}
			finally
			{
				Trace.TraceInformation("タイトルステージの活性化を完了しました。");
				Trace.Unindent();
			}
		}
		public override void DeActivate()
		{
			Trace.TraceInformation("タイトルステージを非活性化します。");
			Trace.Indent();
			try
			{
				TJAPlayer3.tDisposeSafely(ref Background);
			}
			finally
			{
				Trace.TraceInformation("タイトルステージの非活性化を完了しました。");
				Trace.Unindent();
			}
			base.DeActivate();
		}

		public void tReloadMenus()
        {
			if (this.pfMenuTitle != null && this.pfBoxText != null)
				CMainMenuTab.tInitMenus(this.pfMenuTitle, this.pfBoxText, TJAPlayer3.Tx.ModeSelect_Bar, TJAPlayer3.Tx.ModeSelect_Bar_Chara);
		}

		public override void CreateManagedResource()
		{
			this.pfMenuTitle = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.Title_ModeSelect_Title_Scale[0]);
			this.pfBoxText = HPrivateFastFont.tInstantiateBoxFont(TJAPlayer3.Skin.Title_ModeSelect_Title_Scale[1]);

			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource()
		{
            
			TJAPlayer3.tDisposeSafely(ref pfMenuTitle);
			TJAPlayer3.tDisposeSafely(ref pfBoxText);

			base.ReleaseManagedResource();
		}
		public override int Draw()
		{
			if (!base.IsDeActivated)
			{
				#region [ 初めての進行描画 ]
				//---------------------
				if (base.IsFirstDraw)
				{
					if (TJAPlayer3.r直前のステージ == TJAPlayer3.stage起動)
					{
						this.actFIfromSetup.tフェードイン開始();
						base.ePhaseID = CStage.EPhase.Title_FadeIn;
					}
					else
					{
						this.actFI.tフェードイン開始();
						base.ePhaseID = CStage.EPhase.Common_FADEIN;
					}
					base.IsFirstDraw = false;
				}
				//---------------------
				#endregion

				this.ctコインイン待機.TickLoop();
				this.ctSaveLoaded.Tick();
				this.ctSaveLoadingFailed.Tick();
				this.ctエントリーバー点滅.TickLoop();
				this.ctエントリーバー決定点滅.Tick();
				this.ctキャライン.Tick();
				this.ctBarMove.Tick();

				if (!TJAPlayer3.Skin.bgmタイトルイン.bIsPlaying)
				{
					if (TJAPlayer3.ConfigIni.bBGM音を発声する && !b音声再生)
					{
						TJAPlayer3.Skin.bgmタイトル.tPlay();
						b音声再生 = true;
					}
				}

				// 進行

				#region [ キー関係 ]

				if (base.ePhaseID == CStage.EPhase.Common_NORMAL        // 通常状態、かつ
					&& TJAPlayer3.act現在入力を占有中のプラグイン == null)    // プラグインの入力占有がない
				{
					if (TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape) || TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.Cancel))
					{
						if (bモード選択)
						{
							TJAPlayer3.Skin.soundCancelSFX.tPlay();
							bSaveFileLoaded = false;
							UnloadSaveFile();
							if (bSaveFileLoaded == false)
								TJAPlayer3.Skin.soundEntry.tPlay();
						}
						else
						{
							TJAPlayer3.Skin.soundDecideSFX.tPlay();
							n現在の選択行モード選択 = (int)E戻り値.EXIT + 1;
							this.actFO.tフェードアウト開始(0, 500);
							base.ePhaseID = CStage.EPhase.Common_FADEOUT;
						}
					}


					// Disable F1 keybind since menu is accessible from main menu
					/*
					if ((TJAPlayer3.Input管理.Keyboard.bキーが押されている((int)SlimDXKeys.Key.RightShift) || TJAPlayer3.Input管理.Keyboard.bキーが押されている((int)SlimDXKeys.Key.LeftShift)) && TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.F1))
					{
						TJAPlayer3.Skin.soundEntry.t停止する();
						
						n現在の選択行モード選択 = (int)E戻り値.CONFIG - 1;

						this.actFO.tフェードアウト開始();
						base.eフェーズID = CStage.Eフェーズ.共通_フェードアウト;
						TJAPlayer3.Skin.sound取消音.t再生する();
					}
					*/

					// 1st step (Save file loading)
					if (!bSaveIsLoading && !bSaveFailedToLoad)
					{

						if (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.Decide) ||
							TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RRed) || TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LRed))
						{
							// Hit 1P save
							TJAPlayer3.SaveFile = 0;
							CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.ENTRY_NORMAL);
							this.ctSaveLoading.Start(0, 600, 1, TJAPlayer3.Timer);
							this.ctSaveLoading.CurrentValue = (int)this.ctSaveLoading.EndValue;
							for (int i = 0; i < 2; i++)
								TJAPlayer3.NamePlate.tNamePlateRefreshTitles(i);
						}
						else if (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RRed2P) || TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LRed2P))
						{
							// Hit 2P save
							TJAPlayer3.SaveFile = 1;
							CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.ENTRY_NORMAL);
							this.ctSaveLoading.Start(0, 600, 1, TJAPlayer3.Timer);
							this.ctSaveLoading.CurrentValue = (int)this.ctSaveLoading.EndValue;
							for (int i = 0; i < 2; i++)
								TJAPlayer3.NamePlate.tNamePlateRefreshTitles(i);
						}
						else if (TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.P)) // In case "P" is already binded to another pad
						{
							// Hit 1P save
							TJAPlayer3.SaveFile = 0;
							CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.ENTRY_NORMAL);
							this.ctSaveLoading.Start(0, 600, 1, TJAPlayer3.Timer);
							this.ctSaveLoading.CurrentValue = (int)this.ctSaveLoading.EndValue;
							for (int i = 0; i < 2; i++)
								TJAPlayer3.NamePlate.tNamePlateRefreshTitles(i);
						}
					}

					if (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RightChange) || TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.RightArrow))
					{
						if (bプレイヤーエントリー && !bプレイヤーエントリー決定 && this.ctSaveLoaded.IsEnded)
						{
							if (n現在の選択行プレイヤーエントリー + 1 <= 2)
							{
								TJAPlayer3.Skin.soundChangeSFX.tPlay();
								n現在の選択行プレイヤーエントリー += 1;
							}
						}

						if (bモード選択)
						{
							//if (n現在の選択行モード選択 < this.nbModes - 1)
							if (n現在の選択行モード選択 < usedMenusCount - 1)
							{
								TJAPlayer3.Skin.soundChangeSFX.tPlay();
								ctBarMove.Start(0, 250, 1.2f, TJAPlayer3.Timer);
								n現在の選択行モード選択++;
								this.bDownPushed = true;

								for (int i = 0; i < usedMenusCount; i++)
								{
									usedMenusPos[i] = i + 1 - n現在の選択行モード選択;
								}
							}
						}
					}

					if (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LeftChange) || TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.LeftArrow))
					{
						if (bプレイヤーエントリー && !bプレイヤーエントリー決定 && this.ctSaveLoaded.IsEnded)
						{
							if (n現在の選択行プレイヤーエントリー - 1 >= 0)
							{
								TJAPlayer3.Skin.soundChangeSFX.tPlay();
								n現在の選択行プレイヤーエントリー -= 1;
							}
						}

						if (bモード選択)
						{
							if (n現在の選択行モード選択 > 0)
							{
								TJAPlayer3.Skin.soundChangeSFX.tPlay();
								ctBarMove.Start(0, 250, 1.2f, TJAPlayer3.Timer);
								n現在の選択行モード選択--;
								this.bDownPushed = false;

								for (int i = 0; i < usedMenusCount; i++)
								{
									usedMenusPos[i] = i + 1 - n現在の選択行モード選択;
								}
							}
						}
					}


					if (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.Decide)
						|| TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return))
					{
						if (bプレイヤーエントリー && this.ctSaveLoaded.IsEnded)
						{
							if (n現在の選択行プレイヤーエントリー == 0 || n現在の選択行プレイヤーエントリー == 2)
							{
								if (!bプレイヤーエントリー決定)
								{
									TJAPlayer3.Skin.soundDecideSFX.tPlay();
									ctエントリーバー決定点滅.Start(0, 1055, 1, TJAPlayer3.Timer);
									bプレイヤーエントリー決定 = true;
									TJAPlayer3.PlayerSide = (n現在の選択行プレイヤーエントリー == 2) ? 1 : 0;
									if (TJAPlayer3.PlayerSide == 1)
										TJAPlayer3.ConfigIni.nPlayerCount = 1;
									bSaveFileLoaded = true;
								}
							}
							else
							{
								TJAPlayer3.Skin.soundDecideSFX.tPlay();
								bプレイヤーエントリー = false;
								bSaveIsLoading = false;
								TJAPlayer3.Skin.SoundBanapas.bPlayed = false;
								ctSaveLoaded = new CCounter();
								ctSaveLoading = new CCounter();
							}
						}
						if (bモード選択)
						{
							bool operationSucceded = false;

							if (CMainMenuTab.__Menus[usedMenus[this.n現在の選択行モード選択]].rp == E戻り値.DANGAMESTART || CMainMenuTab.__Menus[usedMenus[this.n現在の選択行モード選択]].rp == E戻り値.TAIKOTOWERSSTART)
							{
								if (TJAPlayer3.Songs管理.list曲ルート_Dan.Count > 0 && TJAPlayer3.ConfigIni.nPlayerCount == 1)
									operationSucceded = true;
							}
							else if (CMainMenuTab.__Menus[usedMenus[this.n現在の選択行モード選択]].implemented == true
								&& (CMainMenuTab.__Menus[usedMenus[this.n現在の選択行モード選択]]._1pRestricted == false
								|| TJAPlayer3.ConfigIni.nPlayerCount == 1))
								operationSucceded = true;

							if (operationSucceded == true)
							{
								TJAPlayer3.Skin.soundDecideSFX.tPlay();
								this.actFO.tフェードアウト開始(0, 500);
								base.ePhaseID = CStage.EPhase.Common_FADEOUT;
							}
							else
								TJAPlayer3.Skin.soundError.tPlay();
						}
					}

					if (ctSaveLoading.CurrentValue >= 500)
					{
						if (!bSaveIsLoading)
						{
							TJAPlayer3.Skin.soundEntry.tStop();
							ctSaveLoaded.Start(0, 3655, 1, TJAPlayer3.Timer);
							bSaveIsLoading = true;
							bキャラカウンター初期化 = false;
						}
					}

					if (ctエントリーバー決定点滅.CurrentValue >= 1055)
					{
						if (!bモード選択)
						{
							/*
							if (!TJAPlayer3.Skin.soundsanka.bPlayed)
								TJAPlayer3.Skin.soundsanka.t再生する();
							*/

							if (TJAPlayer3.Skin.voiceTitleSanka[TJAPlayer3.SaveFile] != null && !TJAPlayer3.Skin.voiceTitleSanka[TJAPlayer3.SaveFile].bPlayed)
								TJAPlayer3.Skin.voiceTitleSanka[TJAPlayer3.SaveFile]?.tPlay();

							ctキャライン.Start(0, 180, 2, TJAPlayer3.Timer);
							ctBarAnimeIn.Start(0, 1295, 1, TJAPlayer3.Timer);
							bモード選択 = true;
						}
					}
				}

				#endregion

				#region [ 背景描画 ]

				Background.Update();
				Background.Draw();

				//if (Title_Background != null)
				//	Title_Background.t2D描画(0, 0);

				#endregion

				if (bSaveFileLoaded == false)
				{
					#region [ Save Loading ]

					if (!bSaveIsLoading && !bSaveFailedToLoad)
					{
						TJAPlayer3.Tx.Entry_Bar.t2D描画(0, 0);

						if (this.ctコインイン待機.CurrentValue <= 255)
							TJAPlayer3.Tx.Entry_Bar_Text.Opacity = this.ctコインイン待機.CurrentValue;
						else if (this.ctコインイン待機.CurrentValue <= 2000 - 355)
							TJAPlayer3.Tx.Entry_Bar_Text.Opacity = 255;
						else
							TJAPlayer3.Tx.Entry_Bar_Text.Opacity = 255 - (this.ctコインイン待機.CurrentValue - (2000 - 355));

						TJAPlayer3.Tx.Entry_Bar_Text.t2D描画(TJAPlayer3.Skin.Title_Entry_Bar_Text_X[0], TJAPlayer3.Skin.Title_Entry_Bar_Text_Y[0], new RectangleF(0, 0, TJAPlayer3.Tx.Entry_Bar_Text.sz画像サイズ.Width, TJAPlayer3.Tx.Entry_Bar_Text.sz画像サイズ.Height / 2));
						TJAPlayer3.Tx.Entry_Bar_Text.t2D描画(TJAPlayer3.Skin.Title_Entry_Bar_Text_X[1], TJAPlayer3.Skin.Title_Entry_Bar_Text_Y[1], new RectangleF(0, TJAPlayer3.Tx.Entry_Bar_Text.sz画像サイズ.Height / 2, TJAPlayer3.Tx.Entry_Bar_Text.sz画像サイズ.Width, TJAPlayer3.Tx.Entry_Bar_Text.sz画像サイズ.Height / 2));
					}
					else
					{
						if (this.ctSaveLoaded.CurrentValue <= 1000 && this.ctSaveLoadingFailed.CurrentValue <= 1128)
						{
							if (bSaveIsLoading)
							{
								TJAPlayer3.Tx.Tile_Black.Opacity = this.ctSaveLoaded.CurrentValue <= 2972 ? 128 : 128 - (this.ctSaveLoaded.CurrentValue - 2972);

								for (int i = 0; i < TJAPlayer3.Skin.Resolution[0] / TJAPlayer3.Tx.Tile_Black.szTextureSize.Width + 1; i++)
									for (int j = 0; j < TJAPlayer3.Skin.Resolution[1] / TJAPlayer3.Tx.Tile_Black.szTextureSize.Height + 1; j++)
										TJAPlayer3.Tx.Tile_Black.t2D描画(i * TJAPlayer3.Tx.Tile_Black.szTextureSize.Width, j * TJAPlayer3.Tx.Tile_Black.szTextureSize.Height);

								TJAPlayer3.Tx.Banapas_Load[0].Opacity = ctSaveLoaded.CurrentValue >= 872 ? 255 - (ctSaveLoaded.CurrentValue - 872) * 2 : ctSaveLoaded.CurrentValue * 2;
								TJAPlayer3.Tx.Banapas_Load[0].vcScaleRatio.Y = ctSaveLoaded.CurrentValue <= 100 ? ctSaveLoaded.CurrentValue * 0.01f : 1.0f;
								TJAPlayer3.Tx.Banapas_Load[0].t2D描画(0, 0);

								TJAPlayer3.Tx.Banapas_Load[1].Opacity = ctSaveLoaded.CurrentValue >= 872 ? 255 - (ctSaveLoaded.CurrentValue - 872) * 2 : ctSaveLoaded.CurrentValue <= 96 ? (int)((ctSaveLoaded.CurrentValue - 96) * 7.96875f) : 255;
								TJAPlayer3.Tx.Banapas_Load[1].t2D描画(0, 0);

								if (TJAPlayer3.Tx.Banapas_Load[2] != null)
								{
                                    int step = TJAPlayer3.Tx.Banapas_Load[2].szTextureSize.Width / TJAPlayer3.Skin.Title_LoadingPinFrameCount;
									int cycle = TJAPlayer3.Skin.Title_LoadingPinCycle;
									int _stamp = (ctSaveLoaded.CurrentValue - 200) % (TJAPlayer3.Skin.Title_LoadingPinInstances * cycle);

                                    for (int i = 0; i < TJAPlayer3.Skin.Title_LoadingPinInstances; i++)
                                    {
                                        TJAPlayer3.Tx.Banapas_Load[2].Opacity = ctSaveLoaded.CurrentValue >= 872 ? 255 - (ctSaveLoaded.CurrentValue - 872) * 2 : ctSaveLoaded.CurrentValue <= 96 ? (int)((ctSaveLoaded.CurrentValue - 96) * 7.96875f) : 255;


                                        TJAPlayer3.Tx.Banapas_Load[2].t2D拡大率考慮中央基準描画(
                                            TJAPlayer3.Skin.Title_LoadingPinBase[0] + TJAPlayer3.Skin.Title_LoadingPinDiff[0] * i,
                                            TJAPlayer3.Skin.Title_LoadingPinBase[1] + TJAPlayer3.Skin.Title_LoadingPinDiff[1] * i,
                                            new Rectangle(step
                                                    * (_stamp >= i * cycle
                                                        ? _stamp <= (i + 1) * cycle
                                                            ? (_stamp + i * cycle) / (cycle / TJAPlayer3.Skin.Title_LoadingPinFrameCount)
                                                            : 0
                                                        : 0),
                                                0,
                                                step,
                                                TJAPlayer3.Tx.Banapas_Load[2].szTextureSize.Height));
                                    }
                                }
                                
							}
							if (bSaveFailedToLoad)
							{
								TJAPlayer3.Tx.Tile_Black.Opacity = this.ctSaveLoadingFailed.CurrentValue <= 1000 ? 128 : 128 - (this.ctSaveLoadingFailed.CurrentValue - 1000);

								for (int i = 0; i < TJAPlayer3.Skin.Resolution[0] / TJAPlayer3.Tx.Tile_Black.szTextureSize.Width + 1; i++)
									for (int j = 0; j < TJAPlayer3.Skin.Resolution[1] / TJAPlayer3.Tx.Tile_Black.szTextureSize.Height + 1; j++)
										TJAPlayer3.Tx.Tile_Black.t2D描画(i * TJAPlayer3.Tx.Tile_Black.szTextureSize.Width, j * TJAPlayer3.Tx.Tile_Black.szTextureSize.Height);

								if (!TJAPlayer3.Skin.soundError.bPlayed)
									TJAPlayer3.Skin.soundError.tPlay();

								int count = this.ctSaveLoadingFailed.CurrentValue;
								TJAPlayer3.Tx.Banapas_Load_Failure[0].Opacity = count >= 872 ? 255 - (count - 872) * 2 : count * 2;
								TJAPlayer3.Tx.Banapas_Load_Failure[0].vcScaleRatio.Y = count <= 100 ? count * 0.01f : 1.0f;
								TJAPlayer3.Tx.Banapas_Load_Failure[0].t2D描画(0, 0);

								if (ctSaveLoadingFailed.CurrentValue >= 1128)
								{
									bSaveFailedToLoad = false;
									TJAPlayer3.Skin.soundError.bPlayed = false;
								}
							}
						}
						else
						{
							if (bSaveIsLoading)
							{
								TJAPlayer3.Tx.Tile_Black.Opacity = this.ctSaveLoaded.CurrentValue <= 2972 ? 128 : 128 - (this.ctSaveLoaded.CurrentValue - 2972);

								for (int i = 0; i < TJAPlayer3.Skin.Resolution[0] / TJAPlayer3.Tx.Tile_Black.szTextureSize.Width + 1; i++)
									for (int j = 0; j < TJAPlayer3.Skin.Resolution[1] / TJAPlayer3.Tx.Tile_Black.szTextureSize.Height + 1; j++)
										TJAPlayer3.Tx.Tile_Black.t2D描画(i * TJAPlayer3.Tx.Tile_Black.szTextureSize.Width, j * TJAPlayer3.Tx.Tile_Black.szTextureSize.Height);

								if (!TJAPlayer3.Skin.SoundBanapas.bPlayed)
									TJAPlayer3.Skin.SoundBanapas.tPlay();

								int count = this.ctSaveLoaded.CurrentValue - 1000;
								TJAPlayer3.Tx.Banapas_Load_Clear[0].Opacity = count >= 1872 ? 255 - (count - 1872) * 2 : count * 2;
								TJAPlayer3.Tx.Banapas_Load_Clear[0].vcScaleRatio.Y = count <= 100 ? count * 0.01f : 1.0f;
								TJAPlayer3.Tx.Banapas_Load_Clear[0].t2D描画(0, 0);

								float anime = 0f;
								float scalex = 0f;
								float scaley = 0f;

								if (count >= 300)
								{
									if (count <= 300 + 270)
									{
										anime = (float)Math.Sin((float)(count - 300) / 1.5f * (Math.PI / 180)) * 95f;
										scalex = -(float)Math.Sin((float)(count - 300) / 1.5f * (Math.PI / 180)) * 0.15f;
										scaley = (float)Math.Sin((float)(count - 300) / 1.5f * (Math.PI / 180)) * 0.2f;
									}
									else if (count <= 300 + 270 + 100)
									{
										scalex = (float)Math.Sin((float)(count - (300 + 270)) * 1.8f * (Math.PI / 180)) * 0.13f;
										scaley = -(float)Math.Sin((float)(count - (300 + 270)) * 1.8f * (Math.PI / 180)) * 0.1f;
										anime = 0;
									}
									else if (count <= 300 + 540 + 100)
									{
										anime = (float)Math.Sin((float)(count - (300 + 270 + 100)) / 1.5f * (Math.PI / 180)) * 95f;
										scalex = -(float)Math.Sin((float)(count - (300 + 270 + 100)) / 1.5f * (Math.PI / 180)) * 0.15f;
										scaley = (float)Math.Sin((float)(count - (300 + 270 + 100)) / 1.5f * (Math.PI / 180)) * 0.2f;
									}
									else if (count <= 300 + 540 + 100 + 100)
									{
										scalex = (float)Math.Sin((float)(count - (300 + 540 + 100)) * 1.8f * (Math.PI / 180)) * 0.13f;
										scaley = -(float)Math.Sin((float)(count - (300 + 540 + 100)) * 1.8f * (Math.PI / 180)) * 0.1f;
									}
								}

								TJAPlayer3.Tx.Banapas_Load_Clear[1].vcScaleRatio.X = 1.0f + scalex;
								TJAPlayer3.Tx.Banapas_Load_Clear[1].vcScaleRatio.Y = 1.0f + scaley;
								TJAPlayer3.Tx.Banapas_Load_Clear[1].Opacity = count >= 1872 ? 255 - (count - 1872) * 2 : count * 2;
								TJAPlayer3.Tx.Banapas_Load_Clear[1].t2D拡大率考慮下中心基準描画(TJAPlayer3.Skin.Title_Banapas_Load_Clear_Anime[0], TJAPlayer3.Skin.Title_Banapas_Load_Clear_Anime[1] - anime);

								if (ctSaveLoaded.CurrentValue >= 2000)
								{
									bプレイヤーエントリー = true;
								}
							}
						}
					}

					#endregion
				}

				#region [ プレイヤーエントリー ]

				if (bプレイヤーエントリー)
				{
					if (!this.bキャラカウンター初期化)
					{
						//this.ctキャラエントリーループ = new CCounter(0, Chara_Entry.Length - 1, 1000 / 60, TJAPlayer3.Timer);
						CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.ENTRY);

						this.bキャラカウンター初期化 = true;
					}

					int alpha = ctエントリーバー決定点滅.CurrentValue >= 800 ? 255 - (ctエントリーバー決定点滅.CurrentValue - 800) : (this.ctSaveLoaded.CurrentValue - 3400);

					TJAPlayer3.Tx.Entry_Player[0].Opacity = alpha;
					TJAPlayer3.Tx.Entry_Player[1].Opacity = alpha;

					/*
					var ___ttx = CMenuCharacter._getReferenceArray(0, CMenuCharacter.ECharacterAnimation.ENTRY)
						[CMenuCharacter._getReferenceCounter(CMenuCharacter.ECharacterAnimation.ENTRY)[0].n現在の値];
					___ttx.Opacity = alpha;
					*/

					//Chara_Entry[this.ctキャラエントリーループ.n現在の値].Opacity = alpha;

					TJAPlayer3.Tx.Entry_Player[0].t2D描画(0, 0);

					//Chara_Entry[this.ctキャラエントリーループ.n現在の値].t2D描画(485, 140);

					int _actual = TJAPlayer3.GetActualPlayer(0);

                    int _charaId = TJAPlayer3.SaveFileInstances[_actual].data.Character;

					int chara_x = TJAPlayer3.Skin.Title_Entry_NamePlate[0] + TJAPlayer3.Tx.NamePlateBase.szTextureSize.Width / 2;
					int chara_y = TJAPlayer3.Skin.Title_Entry_NamePlate[1];

                    int puchi_x = chara_x + TJAPlayer3.Skin.Adjustments_MenuPuchichara_X[0];
                    int puchi_y = chara_y + TJAPlayer3.Skin.Adjustments_MenuPuchichara_Y[0];

                    CMenuCharacter.tMenuDisplayCharacter(
						0, 
						chara_x, 
						chara_y, 
						CMenuCharacter.ECharacterAnimation.ENTRY, alpha
						);

					/*
                    CMenuCharacter.tMenuDisplayCharacter(
                        0,
                        TJAPlayer3.Skin.Characters_Title_Entry_X[_charaId][_actual],
                        TJAPlayer3.Skin.Characters_Title_Entry_Y[_charaId][_actual],
                        CMenuCharacter.ECharacterAnimation.ENTRY, alpha
                        );
					*/

                    //___ttx.Opacity = 255;


                    //this.PuchiChara.On進行描画(485 + 100, 140 + 190, false, alpha);
                    this.PuchiChara.On進行描画(puchi_x, puchi_y, false, alpha);

                    TJAPlayer3.Tx.Entry_Player[2].Opacity = ctエントリーバー決定点滅.CurrentValue >= 800 ? 255 - (ctエントリーバー決定点滅.CurrentValue - 800) : (this.ctSaveLoaded.CurrentValue - 3400) - (this.ctエントリーバー点滅.CurrentValue <= 255 ? this.ctエントリーバー点滅.CurrentValue : 255 - (this.ctエントリーバー点滅.CurrentValue - 255));
					TJAPlayer3.Tx.Entry_Player[2].t2D描画(TJAPlayer3.Skin.Title_Entry_Player_Select_X[n現在の選択行プレイヤーエントリー], TJAPlayer3.Skin.Title_Entry_Player_Select_Y[n現在の選択行プレイヤーエントリー],
						new RectangleF(TJAPlayer3.Skin.Title_Entry_Player_Select_Rect[0][n現在の選択行プレイヤーエントリー == 1 ? 1 : 0][0],
						TJAPlayer3.Skin.Title_Entry_Player_Select_Rect[0][n現在の選択行プレイヤーエントリー == 1 ? 1 : 0][1],
						TJAPlayer3.Skin.Title_Entry_Player_Select_Rect[0][n現在の選択行プレイヤーエントリー == 1 ? 1 : 0][2],
						TJAPlayer3.Skin.Title_Entry_Player_Select_Rect[0][n現在の選択行プレイヤーエントリー == 1 ? 1 : 0][3]
						));

					TJAPlayer3.Tx.Entry_Player[2].Opacity = alpha;
					TJAPlayer3.Tx.Entry_Player[2].t2D描画(TJAPlayer3.Skin.Title_Entry_Player_Select_X[n現在の選択行プレイヤーエントリー], TJAPlayer3.Skin.Title_Entry_Player_Select_Y[n現在の選択行プレイヤーエントリー],
						new RectangleF(TJAPlayer3.Skin.Title_Entry_Player_Select_Rect[1][n現在の選択行プレイヤーエントリー == 1 ? 1 : 0][0],
						TJAPlayer3.Skin.Title_Entry_Player_Select_Rect[1][n現在の選択行プレイヤーエントリー == 1 ? 1 : 0][1],
						TJAPlayer3.Skin.Title_Entry_Player_Select_Rect[1][n現在の選択行プレイヤーエントリー == 1 ? 1 : 0][2],
						TJAPlayer3.Skin.Title_Entry_Player_Select_Rect[1][n現在の選択行プレイヤーエントリー == 1 ? 1 : 0][3]
						));

					TJAPlayer3.Tx.Entry_Player[1].t2D描画(0, 0);

					#region [ 透明度 ]

					int Opacity = 0;

					if (ctエントリーバー決定点滅.CurrentValue <= 100)
						Opacity = (int)(ctエントリーバー決定点滅.CurrentValue * 2.55f);
					else if (ctエントリーバー決定点滅.CurrentValue <= 200)
						Opacity = 255 - (int)((ctエントリーバー決定点滅.CurrentValue - 100) * 2.55f);
					else if (ctエントリーバー決定点滅.CurrentValue <= 300)
						Opacity = (int)((ctエントリーバー決定点滅.CurrentValue - 200) * 2.55f);
					else if (ctエントリーバー決定点滅.CurrentValue <= 400)
						Opacity = 255 - (int)((ctエントリーバー決定点滅.CurrentValue - 300) * 2.55f);
					else if (ctエントリーバー決定点滅.CurrentValue <= 500)
						Opacity = (int)((ctエントリーバー決定点滅.CurrentValue - 400) * 2.55f);
					else if (ctエントリーバー決定点滅.CurrentValue <= 600)
						Opacity = 255 - (int)((ctエントリーバー決定点滅.CurrentValue - 500) * 2.55f);

					#endregion

					TJAPlayer3.Tx.Entry_Player[2].Opacity = Opacity;
					TJAPlayer3.Tx.Entry_Player[2].t2D描画(TJAPlayer3.Skin.Title_Entry_Player_Select_X[n現在の選択行プレイヤーエントリー], TJAPlayer3.Skin.Title_Entry_Player_Select_Y[n現在の選択行プレイヤーエントリー],
						new RectangleF(TJAPlayer3.Skin.Title_Entry_Player_Select_Rect[2][n現在の選択行プレイヤーエントリー == 1 ? 1 : 0][0],
						TJAPlayer3.Skin.Title_Entry_Player_Select_Rect[2][n現在の選択行プレイヤーエントリー == 1 ? 1 : 0][1],
						TJAPlayer3.Skin.Title_Entry_Player_Select_Rect[2][n現在の選択行プレイヤーエントリー == 1 ? 1 : 0][2],
						TJAPlayer3.Skin.Title_Entry_Player_Select_Rect[2][n現在の選択行プレイヤーエントリー == 1 ? 1 : 0][3]
						));

					Opacity = ctエントリーバー決定点滅.CurrentValue >= 800 ? 255 - (ctエントリーバー決定点滅.CurrentValue - 800) : (this.ctSaveLoaded.CurrentValue - 3400);
					if (Opacity > 0)
						TJAPlayer3.NamePlate.tNamePlateDraw(TJAPlayer3.Skin.Title_Entry_NamePlate[0], TJAPlayer3.Skin.Title_Entry_NamePlate[1], 0, true, Opacity);
				}

				#endregion

				#region [ モード選択 ]

				if (bモード選択)
				{
					this.ctBarAnimeIn.Tick();

					#region [ キャラ描画 ]

					for (int player = 0; player < TJAPlayer3.ConfigIni.nPlayerCount; player++)
					{
						if (player >= 2) continue;

						float CharaX = 0f, CharaY = 0f;

						CharaX = -200 + ((float)Math.Sin(ctキャライン.CurrentValue / 2 * (Math.PI / 180)) * 200f);
						CharaY = ((float)Math.Sin((90 + (ctキャライン.CurrentValue / 2)) * (Math.PI / 180)) * 150f);
						if (player == 1) CharaX *= -1;

						int _charaId = TJAPlayer3.SaveFileInstances[TJAPlayer3.GetActualPlayer(player)].data.Character;

						//int chara_x = (int)(TJAPlayer3.Skin.Characters_Title_Normal_X[_charaId][player] + CharaX);
						//int chara_y = (int)(TJAPlayer3.Skin.Characters_Title_Normal_Y[_charaId][player] - CharaY);


                        int chara_x = (int)CharaX + TJAPlayer3.Skin.SongSelect_NamePlate_X[player] + TJAPlayer3.Tx.NamePlateBase.szTextureSize.Width / 2;
                        int chara_y = TJAPlayer3.Skin.SongSelect_NamePlate_Y[player] - (int)CharaY;

                        int puchi_x = chara_x + TJAPlayer3.Skin.Adjustments_MenuPuchichara_X[player];
                        int puchi_y = chara_y + TJAPlayer3.Skin.Adjustments_MenuPuchichara_Y[player];

                        //Entry_Chara_Normal[ctキャラループ.n現在の値].t2D描画(-200 + CharaX, 341 - CharaY);
                        CMenuCharacter.tMenuDisplayCharacter(player, chara_x, chara_y, CMenuCharacter.ECharacterAnimation.ENTRY_NORMAL);

						//int puchi_x = TJAPlayer3.Skin.Characters_Menu_X[_charaId][player] + TJAPlayer3.Skin.Adjustments_MenuPuchichara_X[player];
						//int puchi_y = TJAPlayer3.Skin.Characters_Menu_Y[_charaId][player] + TJAPlayer3.Skin.Adjustments_MenuPuchichara_Y[player];

                        this.PuchiChara.On進行描画(puchi_x, puchi_y, false, player: player);
					}

					#endregion

					if (ctBarAnimeIn.CurrentValue >= (int)(16 * 16.6f))
					{
						// TJAPlayer3.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.白, ctBarMove.n現在の値.ToString());

						//for (int i = 0; i < this.nbModes; i++)
						for (int i = 0; i < usedMenusCount; i++)
						{
							// Get Menu reference
							CMainMenuTab _menu = CMainMenuTab.__Menus[usedMenus[i]];
							CTexture _bar = _menu.barTex;
							CTexture _chara = _menu.barChara;

							#region [Disable visualy 1p specific buttons if 2p]

							if ((_menu._1pRestricted == true && TJAPlayer3.ConfigIni.nPlayerCount > 1)
								|| _menu.implemented == false)
							{
								if (_bar != null)
									_bar.color4 = CConversion.ColorToColor4(Color.DarkGray);
								if (_chara != null)
									_chara.color4 = CConversion.ColorToColor4(Color.DarkGray);
								TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(_menu.ttkBoxText, TJAPlayer3.Skin.Title_VerticalText, true).color4 = CConversion.ColorToColor4(Color.DarkGray);
								TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(_menu.ttkTitle, TJAPlayer3.Skin.Title_VerticalText).color4 = CConversion.ColorToColor4(Color.DarkGray);
							}
							else
							{
								if (_bar != null)
									_bar.color4 = CConversion.ColorToColor4(Color.White);
								if (_chara != null)
									_chara.color4 = CConversion.ColorToColor4(Color.White);
								TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(_menu.ttkBoxText, TJAPlayer3.Skin.Title_VerticalText, true).color4 = CConversion.ColorToColor4(Color.White);
								TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(_menu.ttkTitle, TJAPlayer3.Skin.Title_VerticalText).color4 = CConversion.ColorToColor4(Color.White);
							}

							#endregion

							// if (this.stModeBar[i].n現在存在している行 == 1 && ctBarMove.n現在の値 >= 150)
							if (usedMenusPos[i] == 1 && ctBarMove.CurrentValue >= 150)
							{
								float barAnimef = (ctBarMove.CurrentValue / 100.0f) - 1.5f;

								float barAnime = TJAPlayer3.Skin.Title_ModeSelect_Bar_Move[0] +
									(barAnimef * (TJAPlayer3.Skin.Title_ModeSelect_Bar_Move[1] - TJAPlayer3.Skin.Title_ModeSelect_Bar_Move[0]));

								float barAnimeX = TJAPlayer3.Skin.Title_ModeSelect_Bar_Move_X[0] +
									(barAnimef * (TJAPlayer3.Skin.Title_ModeSelect_Bar_Move_X[1] - TJAPlayer3.Skin.Title_ModeSelect_Bar_Move_X[0]));

								float overlayAnime = TJAPlayer3.Skin.Title_ModeSelect_Overlay_Move[0] +
									(barAnimef * (TJAPlayer3.Skin.Title_ModeSelect_Overlay_Move[1] - TJAPlayer3.Skin.Title_ModeSelect_Overlay_Move[0]));

								float overlayAnimeX = TJAPlayer3.Skin.Title_ModeSelect_Overlay_Move_X[0] +
									(barAnimef * (TJAPlayer3.Skin.Title_ModeSelect_Overlay_Move_X[1] - TJAPlayer3.Skin.Title_ModeSelect_Overlay_Move_X[0]));



								//int BarAnime = ctBarAnimeIn.n現在の値 >= (int)(26 * 16.6f) + 100 ? 0 : ctBarAnimeIn.n現在の値 >= (int)(26 * 16.6f) && ctBarAnimeIn.n現在の値 <= (int)(26 * 16.6f) + 100 ? 40 + (int)((ctBarAnimeIn.n現在の値 - (26 * 16.6)) / 100f * 71f) : ctBarAnimeIn.n現在の値 < (int)(26 * 16.6f) ? 40 : 111;
								//int BarAnime1 = BarAnime == 0 ? ctBarMove.n現在の値 >= 150 ? 40 + (int)((ctBarMove.n現在の値 - 150) / 100f * 71f) : ctBarMove.n現在の値 < 150 ? 40 : 111 : 0;

								if (_bar != null)
								{
									_bar.Opacity = 255;
									_bar.vcScaleRatio.X = 1.0f;
									_bar.vcScaleRatio.Y = 1.0f;
									_bar.t2D描画(TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_X[0] - (TJAPlayer3.Skin.Title_VerticalBar ? barAnimeX : 0), 
										TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_Y[0] - (TJAPlayer3.Skin.Title_VerticalBar ? 0 : barAnime), 
										new Rectangle(TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_Rect[0][0], 
										TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_Rect[0][1], 
										TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_Rect[0][2],
										TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_Rect[0][3]));
									_bar.t2D描画(TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_X[1] + (TJAPlayer3.Skin.Title_VerticalBar ? barAnimeX : 0), 
										TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_Y[1] + (TJAPlayer3.Skin.Title_VerticalBar ? 0 : barAnime), 
										new Rectangle(TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_Rect[1][0],
										TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_Rect[1][1],
										TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_Rect[1][2],
										TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_Rect[1][3]));

									if (TJAPlayer3.Skin.Title_VerticalBar)
									{
										_bar.vcScaleRatio.X = (barAnimeX / TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_Rect[2][2]) * 2.0f;
									}
                                    else
									{
										_bar.vcScaleRatio.Y = (barAnime / TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_Rect[2][3]) * 2.0f;
									}

									_bar.t2D拡大率考慮中央基準描画(TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_X[2], TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_Y[2], 
										new Rectangle(TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_Rect[2][0],
										TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_Rect[2][1],
										TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_Rect[2][2],
										TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_Rect[2][3]));
								}


								if (TJAPlayer3.Tx.ModeSelect_Bar[CMainMenuTab.__MenuCount] != null)
								{
									CTexture _overlap = TJAPlayer3.Tx.ModeSelect_Bar[CMainMenuTab.__MenuCount];

									_overlap.vcScaleRatio.X = 1.0f;
									_overlap.vcScaleRatio.Y = 1.0f;
									_overlap.t2D描画(TJAPlayer3.Skin.Title_ModeSelect_Bar_Overlay_X[0], TJAPlayer3.Skin.Title_ModeSelect_Bar_Overlay_Y[0], 
										new Rectangle(TJAPlayer3.Skin.Title_ModeSelect_Bar_Overlay_Rect[0][0],
										TJAPlayer3.Skin.Title_ModeSelect_Bar_Overlay_Rect[0][1],
										TJAPlayer3.Skin.Title_ModeSelect_Bar_Overlay_Rect[0][2],
										TJAPlayer3.Skin.Title_ModeSelect_Bar_Overlay_Rect[0][3]));
									_overlap.t2D描画(TJAPlayer3.Skin.Title_ModeSelect_Bar_Overlay_X[1] + (TJAPlayer3.Skin.Title_VerticalBar ? overlayAnimeX : 0), 
										TJAPlayer3.Skin.Title_ModeSelect_Bar_Overlay_Y[1] + (TJAPlayer3.Skin.Title_VerticalBar ? 0 : overlayAnime), 
										new Rectangle(TJAPlayer3.Skin.Title_ModeSelect_Bar_Overlay_Rect[1][0],
										TJAPlayer3.Skin.Title_ModeSelect_Bar_Overlay_Rect[1][1],
										TJAPlayer3.Skin.Title_ModeSelect_Bar_Overlay_Rect[1][2],
										TJAPlayer3.Skin.Title_ModeSelect_Bar_Overlay_Rect[1][3]));

									if (TJAPlayer3.Skin.Title_VerticalBar)
									{
										_overlap.vcScaleRatio.X = (overlayAnimeX / TJAPlayer3.Skin.Title_ModeSelect_Bar_Overlay_Rect[2][2]);
									}
                                    else
									{
										_overlap.vcScaleRatio.Y = (overlayAnime / TJAPlayer3.Skin.Title_ModeSelect_Bar_Overlay_Rect[2][3]);
									}

									_overlap.t2D拡大率考慮上中央基準描画(TJAPlayer3.Skin.Title_ModeSelect_Bar_Overlay_X[2], TJAPlayer3.Skin.Title_ModeSelect_Bar_Overlay_Y[2], 
										new Rectangle(TJAPlayer3.Skin.Title_ModeSelect_Bar_Overlay_Rect[2][0],
										TJAPlayer3.Skin.Title_ModeSelect_Bar_Overlay_Rect[2][1],
										TJAPlayer3.Skin.Title_ModeSelect_Bar_Overlay_Rect[2][2],
										TJAPlayer3.Skin.Title_ModeSelect_Bar_Overlay_Rect[2][3]));

								}


								float anime = 0;
								float BarAnimeCount = (this.ctBarMove.CurrentValue - 150) / 100.0f;

								if (BarAnimeCount <= 0.45)
									anime = BarAnimeCount * 3.333333333f;
								else
									anime = 1.50f - (BarAnimeCount - 0.45f) * 0.61764705f;
								anime *= TJAPlayer3.Skin.Title_ModeSelect_Bar_Chara_Move;

								if (_chara != null)
								{
									_chara.Opacity = (int)(BarAnimeCount * 255f) + (int)(barAnimef * 2.5f);
									_chara.t2D中心基準描画(TJAPlayer3.Skin.Title_ModeSelect_Bar_Chara_X[0] - anime, TJAPlayer3.Skin.Title_ModeSelect_Bar_Chara_Y[0],
										new Rectangle(0, 0, _chara.szTextureSize.Width / 2, _chara.szTextureSize.Height));
									_chara.t2D中心基準描画(TJAPlayer3.Skin.Title_ModeSelect_Bar_Chara_X[1] + anime, TJAPlayer3.Skin.Title_ModeSelect_Bar_Chara_Y[1],
										new Rectangle(_chara.szTextureSize.Width / 2, 0, _chara.szTextureSize.Width / 2, _chara.szTextureSize.Height));
								}

								TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(_menu.ttkTitle, TJAPlayer3.Skin.Title_VerticalText)?.t2D中心基準描画(
									TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_Title[0] + (TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_Title_Move_X * BarAnimeCount),
									TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_Title[1] - (TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_Title_Move * BarAnimeCount));

								CTexture currentText = TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(_menu.ttkBoxText, TJAPlayer3.Skin.Title_VerticalText, true);
								if (currentText != null)
								{
									currentText.Opacity = (int)(BarAnimeCount * 255f);
									currentText?.t2D中心基準描画(TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_BoxText[0], TJAPlayer3.Skin.Title_ModeSelect_Bar_Center_BoxText[1]);
								}

							}
							else
							{
								int BarAnimeY = ctBarAnimeIn.CurrentValue >= (int)(26 * 16.6f) + 100 && ctBarAnimeIn.CurrentValue <= (int)(26 * 16.6f) + 299 ? 600 - (ctBarAnimeIn.CurrentValue - (int)(26 * 16.6f + 100)) * 3 : ctBarAnimeIn.CurrentValue >= (int)(26 * 16.6f) + 100 ? 0 : 600;
								int BarAnimeX = ctBarAnimeIn.CurrentValue >= (int)(26 * 16.6f) + 100 && ctBarAnimeIn.CurrentValue <= (int)(26 * 16.6f) + 299 ? 100 - (int)((ctBarAnimeIn.CurrentValue - (int)(26 * 16.6f + 100)) * 0.5f) : ctBarAnimeIn.CurrentValue >= (int)(26 * 16.6f) + 100 ? 0 : 100;

								int BarMoveX = 0;
								int BarMoveY = 0;

								#region [Position precalculation]

								//int CurrentPos = this.stModeBar[i].n現在存在している行;
								int CurrentPos = usedMenusPos[i];
								int Selected;

								if (this.bDownPushed)
									Selected = CurrentPos + 1;
								else
									Selected = CurrentPos - 1;

								Point pos = this.getFixedPositionForBar(CurrentPos);
								Point posSelect = this.getFixedPositionForBar(Selected);

								#endregion

								BarMoveX = ctBarMove.CurrentValue <= 100 ? (int)(pos.X - posSelect.X) - (int)(ctBarMove.CurrentValue / 100f * (pos.X - posSelect.X)) : 0;
								BarMoveY = ctBarMove.CurrentValue <= 100 ? (int)(pos.Y - posSelect.Y) - (int)(ctBarMove.CurrentValue / 100f * (pos.Y - posSelect.Y)) : 0;


								if (_bar != null)
								{
									_bar.vcScaleRatio.X = 1.0f;
									_bar.vcScaleRatio.Y = 1.0f;
									_bar.t2D描画(pos.X + BarAnimeX - BarMoveX, pos.Y + BarAnimeY - BarMoveY);
								}

								if (TJAPlayer3.Tx.ModeSelect_Bar[CMainMenuTab.__MenuCount] != null)
								{
									CTexture _overlap = TJAPlayer3.Tx.ModeSelect_Bar[CMainMenuTab.__MenuCount];

									_overlap.vcScaleRatio.X = 1.0f;
									_overlap.vcScaleRatio.Y = 1.0f;
									_overlap.t2D描画(pos.X + BarAnimeX - BarMoveX, pos.Y + BarAnimeY - BarMoveY);
								}



								TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(_menu.ttkTitle, TJAPlayer3.Skin.Title_VerticalText)?.t2D中心基準描画(pos.X + BarAnimeX - BarMoveX + TJAPlayer3.Skin.Title_ModeSelect_Title_Offset[0], pos.Y + BarAnimeY - BarMoveY + TJAPlayer3.Skin.Title_ModeSelect_Title_Offset[1]);
							}
						}
					}

					for (int player = 0; player < TJAPlayer3.ConfigIni.nPlayerCount; player++)
					{
						if (player >= 2) continue;

						TJAPlayer3.NamePlate.tNamePlateDraw(TJAPlayer3.Skin.SongSelect_NamePlate_X[player], TJAPlayer3.Skin.SongSelect_NamePlate_Y[player], player, false, 255);
					}
				}

				#endregion

				#region[ バージョン表示 ]

#if DEBUG

				//string strVersion = "KTT:J:A:I:2017072200";
				string strCreator = "https://github.com/0AuBSQ/OpenTaiko";
				AssemblyName asmApp = Assembly.GetExecutingAssembly().GetName();
				TJAPlayer3.actTextConsole.tPrint(4, 44, CTextConsole.EFontType.White, "DEBUG BUILD");
				TJAPlayer3.actTextConsole.tPrint(4, 4, CTextConsole.EFontType.White, asmApp.Name + " Ver." + TJAPlayer3.VERSION + " (" + strCreator + ")");
				TJAPlayer3.actTextConsole.tPrint(4, 24, CTextConsole.EFontType.White, "Skin:" + TJAPlayer3.Skin.Skin_Name + " Ver." + TJAPlayer3.Skin.Skin_Version + " (" + TJAPlayer3.Skin.Skin_Creator + ")");
				//CDTXMania.act文字コンソール.tPrint(4, 24, C文字コンソール.Eフォント種別.白, strSubTitle);
				TJAPlayer3.actTextConsole.tPrint(4, (TJAPlayer3.Skin.Resolution[1] - 24), CTextConsole.EFontType.White, "TJAPlayer3 forked TJAPlayer2 forPC(kairera0467)");

#endif
				#endregion

				CStage.EPhase eフェーズid = base.ePhaseID;
				switch (eフェーズid)
				{
					case CStage.EPhase.Common_FADEIN:
						if (this.actFI.Draw() != 0)
						{
							base.ePhaseID = CStage.EPhase.Common_NORMAL;
						}
						break;

					case CStage.EPhase.Common_FADEOUT:
						if (this.actFO.Draw() == 0)
						{
							TJAPlayer3.Skin.bgmタイトル.tStop();
							TJAPlayer3.Skin.bgmタイトルイン.tStop();
							break;
						}
						base.ePhaseID = CStage.EPhase.Common_EXIT;


						// Select Menu here

						return ((int)CMainMenuTab.__Menus[usedMenus[this.n現在の選択行モード選択]].rp);

					case CStage.EPhase.Title_FadeIn:
						if (this.actFIfromSetup.Draw() != 0)
						{
							base.ePhaseID = CStage.EPhase.Common_NORMAL;
						}
						break;
				}
			}
			return 0;
		}
		public enum E戻り値
		{
			継続 = 0,
			GAMESTART,
			DANGAMESTART,
			TAIKOTOWERSSTART,
			SHOPSTART,
			BOUKENSTART,
			HEYA,
			CONFIG,
			EXIT,
			ONLINELOUNGE,
			ENCYCLOPEDIA,
			AIBATTLEMODE,
			PLAYERSTATS,
			CHARTEDITOR,
			TOOLBOX,
		}


		// その他

		#region [ private ]
		//-----------------

		private ScriptBG Background;

		// Directly propose the different game options if the save file is already loaded, go back to save file select by pressing "Escape"
		private void SkipSaveFileStep()
		{
			if (bSaveFileLoaded == true)
			{
				bモード選択 = true;
				// bプレイヤーエントリー = true;
				bSaveIsLoading = true;
				bプレイヤーエントリー決定 = true;
				bキャラカウンター初期化 = true;

				this.ctSaveLoading.Start(0, 600, 1, TJAPlayer3.Timer);
				this.ctSaveLoading.CurrentValue = (int)this.ctSaveLoading.EndValue;
				ctエントリーバー決定点滅.Start(0, 1055, 1, TJAPlayer3.Timer);
				ctエントリーバー決定点滅.CurrentValue = (int)ctエントリーバー決定点滅.CurrentValue;
				ctSaveLoaded.Start(0, 3655, 1, TJAPlayer3.Timer);
				ctSaveLoaded.CurrentValue = (int)ctSaveLoaded.EndValue;

				ctキャライン.Start(0, 180, 2, TJAPlayer3.Timer);
				ctBarAnimeIn.Start(0, 1295, 1, TJAPlayer3.Timer);

				ctコインイン待機.CurrentValue = (int)ctコインイン待機.EndValue;
				ctエントリーバー点滅.CurrentValue = (int)ctエントリーバー点滅.EndValue;

				TJAPlayer3.Skin.SoundBanapas.bPlayed = true;
				//TJAPlayer3.Skin.soundsanka.bPlayed = true;
				
				if (TJAPlayer3.Skin.voiceTitleSanka[TJAPlayer3.SaveFile] != null)
					TJAPlayer3.Skin.voiceTitleSanka[TJAPlayer3.SaveFile].bPlayed = true;
			}
		}

		// Restore the title screen to the "Taiko hit start" screen
		private void UnloadSaveFile()
		{
			this.ctSaveLoading = new CCounter();
			this.ctコインイン待機 = new CCounter(0, 2000, 1, TJAPlayer3.Timer);
			this.ctSaveLoaded = new CCounter();
			this.ctSaveLoadingFailed = new CCounter();
			this.ctエントリーバー点滅 = new CCounter(0, 510, 2, TJAPlayer3.Timer);
			this.ctエントリーバー決定点滅 = new CCounter();

			//this.ctキャラエントリーループ = new CCounter();
			CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.ENTRY);
			this.ctキャライン = new CCounter();
			//this.ctキャラループ = new CCounter(0, Entry_Chara_Normal.Length - 1, 1000 / 30, TJAPlayer3.Timer);
			CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.ENTRY_NORMAL);


			this.ctBarAnimeIn = new CCounter();
			this.ctBarMove = new CCounter();
			this.ctBarMove.CurrentValue = 250;

			this.bSaveIsLoading = false;
			this.bSaveFailedToLoad = false;
			this.bプレイヤーエントリー = false;
			this.bプレイヤーエントリー決定 = false;
			this.bモード選択 = false;
			this.bキャラカウンター初期化 = false;
			this.n現在の選択行プレイヤーエントリー = 1;

			TJAPlayer3.Skin.SoundBanapas.bPlayed = false;
		}

		private static bool bSaveFileLoaded = false;

		private CCounter ctコインイン待機;

		private CCounter ctSaveLoading;

		private CCounter ctSaveLoaded;
		private CCounter ctSaveLoadingFailed;

		private CCounter ctエントリーバー点滅;
		private CCounter ctエントリーバー決定点滅;

		//private CCounter ctキャラエントリーループ;
		private CCounter ctキャライン;
		//private CCounter ctキャラループ;

		private CCounter ctBarAnimeIn;
		private CCounter ctBarMove;

		private bool bDownPushed;

		private PuchiChara PuchiChara;

		private CCachedFontRenderer pfMenuTitle;
		private CCachedFontRenderer pfBoxText;

		private int[] usedMenus;
		private int[] usedMenusPos;
		private int usedMenusCount;

		private bool bSaveIsLoading;
		private bool bSaveFailedToLoad;
		private bool bプレイヤーエントリー;
		private bool bプレイヤーエントリー決定;
		private bool bモード選択;
		private bool bキャラカウンター初期化;

		private int n現在の選択行プレイヤーエントリー;
		private int n現在の選択行モード選択;

		/*private Point[] ptプレイヤーエントリーバー座標 =
			{ new Point(337, 488), new Point( 529, 487), new Point(743, 486) };

		private Point[] ptモード選択バー座標 =
			{ new Point(290, 107), new Point(319, 306), new Point(356, 513) };*/

		private Point getFixedPositionForBar(int CurrentPos)
		{
			int posX;
			int posY;

			if (CurrentPos >= 0 && CurrentPos < 3)
			{
				posX = TJAPlayer3.Skin.Title_ModeSelect_Bar_X[CurrentPos];
				posY = TJAPlayer3.Skin.Title_ModeSelect_Bar_Y[CurrentPos];
			}
			else if (CurrentPos < 0)
			{
				posX = TJAPlayer3.Skin.Title_ModeSelect_Bar_X[0] + CurrentPos * TJAPlayer3.Skin.Title_ModeSelect_Bar_Offset[0];
				posY = TJAPlayer3.Skin.Title_ModeSelect_Bar_Y[0] + CurrentPos * TJAPlayer3.Skin.Title_ModeSelect_Bar_Offset[1];
			}
			else
			{
				posX = TJAPlayer3.Skin.Title_ModeSelect_Bar_X[2] + (CurrentPos - 2) * TJAPlayer3.Skin.Title_ModeSelect_Bar_Offset[0];
				posY = TJAPlayer3.Skin.Title_ModeSelect_Bar_Y[2] + (CurrentPos - 2) * TJAPlayer3.Skin.Title_ModeSelect_Bar_Offset[1];
			}

			return new Point(posX, posY);
		}

		private bool b音声再生;
		private CActFIFOBlack actFI;
		private CActFIFOBlack actFIfromSetup;
		private CActFIFOBlack actFO;
		//-----------------
		#endregion
	}
}
