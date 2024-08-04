using System.Runtime.InteropServices;
using FDK;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using RectangleF = System.Drawing.RectangleF;

namespace OpenTaiko {
	/// <summary>
	/// 難易度選択画面。
	/// この難易度選択画面はAC7～AC14のような方式であり、WiiまたはAC15移行の方式とは異なる。
	/// </summary>
	internal class CActSelect難易度選択画面 : CActivity {
		// プロパティ

		public bool bIsDifficltSelect;

		// コンストラクタ

		public CActSelect難易度選択画面() {
			for (int i = 0; i < 10; i++) {
				st小文字位置[i].ptX = i * 18;
				st小文字位置[i].ch = i.ToString().ToCharArray()[0];
			}
			base.IsDeActivated = true;
		}

		public void t次に移動(int player) {
			if (n現在の選択行[player] < 5) {
				ctBarAnime[player].Start(0, 180, 1, OpenTaiko.Timer);
				if (!b裏譜面) {
					n現在の選択行[player]++;
				} else {
					if (n現在の選択行[player] == 4) {
						n現在の選択行[player] += 2;
					} else {
						n現在の選択行[player]++;
					}
				}
			} else if (n現在の選択行[player] >= 5) {
				if (OpenTaiko.stageSongSelect.r現在選択中のスコア.譜面情報.nレベル[4] < 0 || OpenTaiko.stageSongSelect.r現在選択中のスコア.譜面情報.nレベル[3] < 0)
					return;

				if (nスイッチカウント < 0) {
					nスイッチカウント++;
				} else if (nスイッチカウント == 0) {
					for (int i = 0; i < 5; i++) {
						if (!bSelect[i]) {
							if (n現在の選択行[i] == 5) {
								// Extreme to Extra
								OpenTaiko.stageSongSelect.actExExtraTransAnime.BeginAnime(true);
								n現在の選択行[i] = 6;
							} else if (n現在の選択行[i] == 6) {
								//Extra to Extreme
								OpenTaiko.stageSongSelect.actExExtraTransAnime.BeginAnime(false);
								n現在の選択行[i] = 5;
							}
						}
					}

					b裏譜面 = !b裏譜面;
					nスイッチカウント = 0;
				}
			}
		}

		public void t前に移動(int player) {
			if (n現在の選択行[player] - 1 >= 0) {
				ctBarAnime[player].Start(0, 180, 1, OpenTaiko.Timer);
				nスイッチカウント = 0;
				if (n現在の選択行[player] == 6)
					n現在の選択行[player] -= 2;
				else
					n現在の選択行[player]--;
			}
		}

		public void t選択画面初期化() {
			this.txTitle = OpenTaiko.tテクスチャの生成(pfTitle.DrawText(OpenTaiko.stageSongSelect.rNowSelectedSong.ldTitle.GetString(""), Color.White, Color.Black, null, 30));
			this.txSubTitle = OpenTaiko.tテクスチャの生成(pfSubTitle.DrawText(OpenTaiko.stageSongSelect.rNowSelectedSong.ldSubtitle.GetString(""), Color.White, Color.Black, null, 30));

			this.n現在の選択行 = new int[5];
			this.bSelect[0] = false;
			this.bSelect[1] = false;
			this.bSelect[2] = false;
			this.bSelect[3] = false;
			this.bSelect[4] = false;

			this.b裏譜面 = (OpenTaiko.stageSongSelect.r現在選択中のスコア.譜面情報.nレベル[(int)Difficulty.Edit] >= 0 && OpenTaiko.stageSongSelect.r現在選択中のスコア.譜面情報.nレベル[(int)Difficulty.Oni] < 0);

			this.IsFirstDraw = true;
		}

		// CActivity 実装

		public override void Activate() {
			if (this.IsActivated)
				return;
			ctBarAnime = new CCounter[5];
			ctBarAnime[0] = new CCounter();
			ctBarAnime[1] = new CCounter();
			ctBarAnime[2] = new CCounter();
			ctBarAnime[3] = new CCounter();
			ctBarAnime[4] = new CCounter();

			base.Activate();
		}
		public override void DeActivate() {
			if (this.IsDeActivated)
				return;

			ctBarAnime = null;

			base.DeActivate();
		}
		public override void CreateManagedResource() {
			this.pfTitle = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.SongSelect_MusicName_Scale);
			this.pfSubTitle = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.SongSelect_Subtitle_Scale);

			// this.soundSelectAnnounce = TJAPlayer3.Sound管理.tサウンドを生成する( CSkin.Path( @"Sounds{Path.DirectorySeparatorChar}DiffSelect.ogg" ), ESoundGroup.SoundEffect );

			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource() {
			pfTitle.Dispose();
			pfSubTitle.Dispose();

			// TJAPlayer3.t安全にDisposeする( ref this.soundSelectAnnounce );

			base.ReleaseManagedResource();
		}
		public override int Draw() {
			if (this.IsDeActivated)
				return 0;

			#region [ 初めての進行描画 ]
			//-----------------
			if (this.IsFirstDraw) {
				ctBarAnimeIn = new CCounter(0, 170, 4, OpenTaiko.Timer);
				// this.soundSelectAnnounce?.tサウンドを再生する();
				//TJAPlayer3.Skin.soundSelectAnnounce.t再生する();
				OpenTaiko.Skin.voiceMenuDiffSelect[OpenTaiko.SaveFile]?.tPlay();
				base.IsFirstDraw = false;
			}
			//-----------------
			#endregion

			ctBarAnimeIn.Tick();
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				ctBarAnime[i].Tick();
			}

			bool uraExists = OpenTaiko.stageSongSelect.r現在選択中のスコア.譜面情報.nレベル[(int)Difficulty.Edit] >= 0;
			bool omoteExists = OpenTaiko.stageSongSelect.r現在選択中のスコア.譜面情報.nレベル[(int)Difficulty.Oni] >= 0;

			#region [ キー入力 ]

			if (this.ctBarAnimeIn.IsEnded && exextraAnimation == 0) // Prevent player actions if animation is active
			{
				for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
					if (!bSelect[i] && !isOnOption()) {
						bool right = false;
						bool left = false;
						bool decide = false;

						bool cancel = false;

						switch (i) {
							case 0:
								right = (OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RightChange) || OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.RightArrow));
								left = (OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LeftChange) || OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.LeftArrow));
								decide = (OpenTaiko.Pad.bPressedDGB(EPad.Decide) ||
									(OpenTaiko.ConfigIni.bEnterがキー割り当てのどこにも使用されていない && OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return)));
								cancel = (OpenTaiko.Pad.bPressedDGB(EPad.Cancel) || OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape));
								break;
							case 1:
								if (!OpenTaiko.ConfigIni.bAIBattleMode) {
									right = (OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RBlue2P));
									left = (OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LBlue2P));
									decide = (OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LRed2P) || OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RRed2P));
								}
								break;
							case 2:
								right = (OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RBlue3P));
								left = (OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LBlue3P));
								decide = (OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LRed3P) || OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RRed3P));
								break;
							case 3:
								right = (OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RBlue4P));
								left = (OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LBlue4P));
								decide = (OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LRed4P) || OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RRed4P));
								break;
							case 4:
								right = (OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RBlue5P));
								left = (OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LBlue5P));
								decide = (OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LRed5P) || OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RRed5P));
								break;
						}

						if (right) {
							OpenTaiko.Skin.soundChangeSFX.tPlay();
							this.t次に移動(i);
						} else if (left) {
							OpenTaiko.Skin.soundChangeSFX.tPlay();
							this.t前に移動(i);
						}
						if (decide) {
							if (n現在の選択行[i] == 0) {
								OpenTaiko.Skin.soundDecideSFX.tPlay();
								OpenTaiko.stageSongSelect.actSongList.ctBarOpen.Start(100, 260, 2, OpenTaiko.Timer);
								this.bIsDifficltSelect = false;
							} else if (n現在の選択行[i] == 1) {
								OpenTaiko.Skin.soundDecideSFX.tPlay();
								bOption[i] = true;
							} else {
								if (OpenTaiko.stageSongSelect.r現在選択中のスコア.譜面情報.nレベル[n現在の選択行[i] - 2] > 0) {
									//TJAPlayer3.stage選曲.ctChara_Jump[0].t開始(0, SongSelect_Chara_Jump.Length - 1, 1000 / 45, TJAPlayer3.Timer);


									this.bSelect[i] = true;

									bool allPlayerSelected = true;

									for (int i2 = 0; i2 < OpenTaiko.ConfigIni.nPlayerCount; i2++) {
										if (OpenTaiko.ConfigIni.bAIBattleMode && i2 == 1) break;

										if (!bSelect[i2]) {
											allPlayerSelected = false;
											break;
										}
									}

									if (allPlayerSelected) {
										if (OpenTaiko.Skin.soundSongDecide_AI.bLoadedSuccessfuly && OpenTaiko.ConfigIni.bAIBattleMode) {
											OpenTaiko.Skin.soundSongDecide_AI.tPlay();
										} else if (OpenTaiko.Skin.sound曲決定音.bLoadedSuccessfuly) {
											OpenTaiko.Skin.sound曲決定音.tPlay();
										} else {
											OpenTaiko.Skin.soundDecideSFX.tPlay();
										}

										for (int i2 = 0; i2 < OpenTaiko.ConfigIni.nPlayerCount; i2++) {
											if (OpenTaiko.ConfigIni.bAIBattleMode) {
												OpenTaiko.Skin.voiceMenuSongDecide_AI[OpenTaiko.GetActualPlayer(i2)]?.tPlay();
											} else {
												OpenTaiko.Skin.voiceMenuSongDecide[OpenTaiko.GetActualPlayer(i2)]?.tPlay();
											}
											CMenuCharacter.tMenuResetTimer(i2, CMenuCharacter.ECharacterAnimation.START);
											if (OpenTaiko.ConfigIni.bAIBattleMode) {
												OpenTaiko.stageSongSelect.t曲を選択する(n現在の選択行[0] - 2, i2);
											} else {
												OpenTaiko.stageSongSelect.t曲を選択する(n現在の選択行[i2] - 2, i2);
											}
										}
									} else {
										CMenuCharacter.tMenuResetTimer(i, CMenuCharacter.ECharacterAnimation.WAIT);
										OpenTaiko.Skin.soundDecideSFX.tPlay();
									}
								}
							}
						}
						if (cancel) {
							OpenTaiko.Skin.soundDecideSFX.tPlay();
							OpenTaiko.stageSongSelect.actSongList.ctBarOpen.Start(100, 260, 2, OpenTaiko.Timer);
							this.bIsDifficltSelect = false;
						}
					}
				}
			}

			#endregion

			bool consideMultiPlay = OpenTaiko.ConfigIni.nPlayerCount >= 2 && !OpenTaiko.ConfigIni.bAIBattleMode;

			#region [ 画像描画 ]


			// int boxType = nStrジャンルtoNum(TJAPlayer3.stage選曲.r現在選択中の曲.strジャンル);
			var difficulty_back = HGenreBar.tGetGenreBar(OpenTaiko.stageSongSelect.rNowSelectedSong.BoxType, OpenTaiko.Tx.Difficulty_Back);


			difficulty_back.Opacity =
				(OpenTaiko.stageSongSelect.actSongList.ctDifficultyIn.CurrentValue - 1255);
			OpenTaiko.Tx.Difficulty_Bar.Opacity = (OpenTaiko.stageSongSelect.actSongList.ctDifficultyIn.CurrentValue - 1255);
			OpenTaiko.Tx.Difficulty_Number.Opacity = (OpenTaiko.stageSongSelect.actSongList.ctDifficultyIn.CurrentValue - 1255);
			OpenTaiko.Tx.Difficulty_Crown.Opacity = (OpenTaiko.stageSongSelect.actSongList.ctDifficultyIn.CurrentValue - 1255);
			OpenTaiko.Tx.SongSelect_ScoreRank.vcScaleRatio.X = 0.65f;
			OpenTaiko.Tx.SongSelect_ScoreRank.vcScaleRatio.Y = 0.65f;
			OpenTaiko.Tx.SongSelect_ScoreRank.Opacity = (OpenTaiko.stageSongSelect.actSongList.ctDifficultyIn.CurrentValue - 1255);
			OpenTaiko.Tx.Difficulty_Star.Opacity = (OpenTaiko.stageSongSelect.actSongList.ctDifficultyIn.CurrentValue - 1255);

			difficulty_back.color4 = CConversion.ColorToColor4(OpenTaiko.stageSongSelect.rNowSelectedSong.BoxColor);

			difficulty_back.t2D中心基準描画(OpenTaiko.Skin.SongSelect_Difficulty_Back[0], OpenTaiko.Skin.SongSelect_Difficulty_Back[1]);

			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				if (OpenTaiko.ConfigIni.bAIBattleMode && i == 1) break;

				/*
                Difficulty_Select_Bar[i].Opacity = (int)(ctBarAnimeIn.n現在の値 >= 80 ? (ctBarAnimeIn.n現在の値 - 80) * 2.84f : 0);
                Difficulty_Select_Bar[i].t2D描画((float)this.BarX[n現在の選択行[i]], 242, new RectangleF(0, (n現在の選択行[i] >= 2 ? 114 : 387), 259, 275 - (n現在の選択行[i] >= 2 ? 0 : 164)));
                */
				OpenTaiko.Tx.Difficulty_Select_Bar[i].Opacity = (int)(ctBarAnimeIn.CurrentValue >= 80 ? (ctBarAnimeIn.CurrentValue - 80) * 2.84f : 0);

				int backType = n現在の選択行[i] >= 2 ? 1 : 2;

				OpenTaiko.Tx.Difficulty_Select_Bar[i].t2D描画(
					OpenTaiko.Skin.SongSelect_Difficulty_Select_Bar_Back_X[n現在の選択行[i]],
					OpenTaiko.Skin.SongSelect_Difficulty_Select_Bar_Back_Y[n現在の選択行[i]],
					new RectangleF(OpenTaiko.Skin.SongSelect_Difficulty_Select_Bar_Rect[backType][0], OpenTaiko.Skin.SongSelect_Difficulty_Select_Bar_Rect[backType][1],
					OpenTaiko.Skin.SongSelect_Difficulty_Select_Bar_Rect[backType][2], OpenTaiko.Skin.SongSelect_Difficulty_Select_Bar_Rect[backType][3]));
			}

			OpenTaiko.Tx.Difficulty_Bar.color4 = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
			//Difficulty_Bar.t2D描画(255, 270, new RectangleF(0, 0, 171, 236));    //閉じる、演奏オプション
			for (int i = 0; i < 2; i++) {
				OpenTaiko.Tx.Difficulty_Bar.t2D描画(OpenTaiko.Skin.SongSelect_Difficulty_Bar_X[i], OpenTaiko.Skin.SongSelect_Difficulty_Bar_Y[i],
					new RectangleF(OpenTaiko.Skin.SongSelect_Difficulty_Bar_Rect[i][0],
					OpenTaiko.Skin.SongSelect_Difficulty_Bar_Rect[i][1],
					OpenTaiko.Skin.SongSelect_Difficulty_Bar_Rect[i][2],
					OpenTaiko.Skin.SongSelect_Difficulty_Bar_Rect[i][3]));    //閉じる
			}

			exextraAnimation = OpenTaiko.stageSongSelect.actExExtraTransAnime.Draw();

			for (int i = 0; i <= (int)Difficulty.Edit; i++) {
				if (i == (int)Difficulty.Edit && (!uraExists || !b裏譜面))
					break;
				else if (i == (int)Difficulty.Oni && (!omoteExists && uraExists || b裏譜面))
					continue;

				int screenPos = Math.Min((int)Difficulty.Oni, i);
				int level = OpenTaiko.stageSongSelect.r現在選択中のスコア.譜面情報.nレベル[i];
				bool avaliable = OpenTaiko.stageSongSelect.r現在選択中のスコア.譜面情報.nレベル[i] >= 0;

				if (avaliable)
					OpenTaiko.Tx.Difficulty_Bar.color4 = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
				else
					OpenTaiko.Tx.Difficulty_Bar.color4 = new Color4(0.5f, 0.5f, 0.5f, 1.0f);

				if (!(i >= (int)Difficulty.Oni && exextraAnimation > 0)) // Hide Oni/Ura during transition
				{
					OpenTaiko.Tx.Difficulty_Bar.t2D描画(OpenTaiko.Skin.SongSelect_Difficulty_Bar_X[i + 2], OpenTaiko.Skin.SongSelect_Difficulty_Bar_Y[i + 2],
						new RectangleF(OpenTaiko.Skin.SongSelect_Difficulty_Bar_Rect[i + 2][0],
						OpenTaiko.Skin.SongSelect_Difficulty_Bar_Rect[i + 2][1],
						OpenTaiko.Skin.SongSelect_Difficulty_Bar_Rect[i + 2][2],
						OpenTaiko.Skin.SongSelect_Difficulty_Bar_Rect[i + 2][3]));
				}

				if (!avaliable)
					continue;


				for (int j = 0; j < OpenTaiko.ConfigIni.nPlayerCount; j++) {
					if (j >= 2) continue;

					if (OpenTaiko.ConfigIni.bAIBattleMode && i == 1) break;

					int p = OpenTaiko.GetActualPlayer(j);

					Cスコア.ST譜面情報 idx = OpenTaiko.stageSongSelect.r現在選択中のスコア.譜面情報;

					//var GPInfo = TJAPlayer3.stageSongSelect.r現在選択中のスコア.GPInfo[p];

					var TableEntry = OpenTaiko.SaveFileInstances[p].data.tGetSongSelectTableEntry(OpenTaiko.stageSongSelect.rNowSelectedSong.tGetUniqueId());

					//Difficulty_Crown.t2D描画(445 + screenPos * 144, 284, new RectangleF(idx.nクリア[i] * 24.5f, 0, 24.5f, 26));

					int crown_width = OpenTaiko.Tx.Difficulty_Crown.szTextureSize.Width / 5;
					int crown_height = OpenTaiko.Tx.Difficulty_Crown.szTextureSize.Height;
					OpenTaiko.Tx.Difficulty_Crown.t2D描画(
						OpenTaiko.Skin.SongSelect_Difficulty_Crown_X[j][i],
						OpenTaiko.Skin.SongSelect_Difficulty_Crown_Y[j][i],
						new RectangleF(TableEntry.ClearStatuses[i] * crown_width, 0, crown_width, crown_height));

					int scoreRank_width = OpenTaiko.Tx.SongSelect_ScoreRank.szTextureSize.Width;
					int scoreRank_height = OpenTaiko.Tx.SongSelect_ScoreRank.szTextureSize.Height / 7;

					if (TableEntry.ScoreRanks[i] != 0)
						OpenTaiko.Tx.SongSelect_ScoreRank.t2D描画(
							OpenTaiko.Skin.SongSelect_Difficulty_ScoreRank_X[j][i],
							OpenTaiko.Skin.SongSelect_Difficulty_ScoreRank_Y[j][i],
							new RectangleF(0, (TableEntry.ScoreRanks[i] - 1) * scoreRank_height, scoreRank_width, scoreRank_height));

					/*
                    if (idx.nスコアランク[i] != 0)
                        SongSelect_ScoreRank.t2D描画(467 + screenPos * 144, 281, new RectangleF(0, (idx.nスコアランク[i] - 1) * 42.71f, 50, 42.71f));
                    */
				}

				if (level >= 0 && (!(i >= (int)Difficulty.Oni && exextraAnimation > 0)))
					t小文字表示(OpenTaiko.stageSongSelect.r現在選択中のスコア.譜面情報.nレベル[i],
						OpenTaiko.Skin.SongSelect_Difficulty_Number_X[i],
						OpenTaiko.Skin.SongSelect_Difficulty_Number_Y[i],
						i,
						OpenTaiko.stageSongSelect.r現在選択中のスコア.譜面情報.nLevelIcon[i]
						);

				if (!(i >= (int)Difficulty.Oni && exextraAnimation > 0)) {
					for (int g = 0; g < 10; g++) {
						if (level > g + 10) {
							OpenTaiko.Tx.Difficulty_Star.color4 = new Color4(1f, 0.2f, 0.2f, 1.0f);
							OpenTaiko.Tx.Difficulty_Star?.t2D描画(OpenTaiko.Skin.SongSelect_Difficulty_Star_X[i] + g * OpenTaiko.Skin.SongSelect_Difficulty_Star_Interval[0], OpenTaiko.Skin.SongSelect_Difficulty_Star_Y[i] + g * OpenTaiko.Skin.SongSelect_Difficulty_Star_Interval[1]);
						} else if (level > g) {
							OpenTaiko.Tx.Difficulty_Star.color4 = new Color4(1f, 1f, 1f, 1.0f);
							OpenTaiko.Tx.Difficulty_Star?.t2D描画(OpenTaiko.Skin.SongSelect_Difficulty_Star_X[i] + g * OpenTaiko.Skin.SongSelect_Difficulty_Star_Interval[0], OpenTaiko.Skin.SongSelect_Difficulty_Star_Y[i] + g * OpenTaiko.Skin.SongSelect_Difficulty_Star_Interval[1]);
						}

					}
				}

				if (OpenTaiko.stageSongSelect.r現在選択中のスコア.譜面情報.b譜面分岐[i])
					OpenTaiko.Tx.SongSelect_Branch_Text?.t2D描画(

						OpenTaiko.Skin.SongSelect_Difficulty_Bar_X[i + 2] + OpenTaiko.Skin.SongSelect_Branch_Text_Offset[0],
						OpenTaiko.Skin.SongSelect_Difficulty_Bar_Y[i + 2] + OpenTaiko.Skin.SongSelect_Branch_Text_Offset[1]
					);
			}

			this.txTitle.t2D中心基準描画(OpenTaiko.Skin.SongSelect_Difficulty_Select_Title[0], OpenTaiko.Skin.SongSelect_Difficulty_Select_Title[1]);
			this.txSubTitle.t2D中心基準描画(OpenTaiko.Skin.SongSelect_Difficulty_Select_SubTitle[0], OpenTaiko.Skin.SongSelect_Difficulty_Select_SubTitle[1]);

			#region [ バーの描画 ]

			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				if (OpenTaiko.ConfigIni.bAIBattleMode && i == 1) break;

				/*
                Difficulty_Select_Bar[i].t2D描画(
                    TJAPlayer3.ConfigIni.nPlayerCount == 2 ? n現在の選択行[0] != n現在の選択行[1] ? (float)this.BarX[n現在の選択行[i]] : i == 0 ? (float)this.BarX[n現在の選択行[i]] - 25 : (float)this.BarX[n現在の選択行[i]] + 25 : (float)this.BarX[n現在の選択行[i]], 
                    126 + ((float)Math.Sin((float)(ctBarAnimeIn.n現在の値 >= 80 ? (ctBarAnimeIn.n現在の値 - 80) : 0) * (Math.PI / 180)) * 50) + (float)Math.Sin((float)ctBarAnime[i].n現在の値 * (Math.PI / 180)) * 10, 
                    new RectangleF(0, 0, 259, 114));
                */

				//bool overlap = n現在の選択行[0] == n現在の選択行[1] && consideMultiPlay;

				int overlapCount = 0;
				int nowOverlapIndex = 0;

				for (int j = 0; j < OpenTaiko.ConfigIni.nPlayerCount; j++) {
					if (n現在の選択行[i] == n現在の選択行[j] && j != i) overlapCount++;
					if (j == i) {
						nowOverlapIndex = overlapCount;
					}
				}

				/*float moveX = overlap ? (i == 0 ? -TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Move[0] : TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Move[0]) : 0;
                moveX += (((float)Math.Sin((float)ctBarAnime[i].n現在の値 * (Math.PI / 180)) * TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Anime[0]) -
                    (((float)Math.Cos((float)ctBarAnimeIn.n現在の値 * (Math.PI / 170)) + 1.0f) * TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_AnimeIn[0]));

                float moveY = overlap ? (i == 0 ? -TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Move[1] : TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Move[1]) : 0;
                moveY += (((float)Math.Sin((float)ctBarAnime[i].n現在の値 * (Math.PI / 180)) * TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Anime[1]) -
                    (((float)Math.Cos((float)ctBarAnimeIn.n現在の値 * (Math.PI / 170)) + 1.0f) * TJAPlayer3.Skin.SongSelect_Difficulty_Select_Bar_Anime[1]));
                */

				float moveX = OpenTaiko.Skin.SongSelect_Difficulty_Select_Bar_Move[0] * (nowOverlapIndex - (overlapCount / 2.0f)) * (2.0f / Math.Max(overlapCount, 1));
				float moveY = OpenTaiko.Skin.SongSelect_Difficulty_Select_Bar_Move[1] * (nowOverlapIndex - (overlapCount / 2.0f)) * (2.0f / Math.Max(overlapCount, 1));

				if (!consideMultiPlay) {
					moveX = 0;
					moveY = 0;
				}

				moveX += (((float)Math.Sin((float)ctBarAnime[i].CurrentValue * (Math.PI / 180)) * OpenTaiko.Skin.SongSelect_Difficulty_Select_Bar_Anime[0]) -
					(((float)Math.Cos((float)ctBarAnimeIn.CurrentValue * (Math.PI / 170)) + 1.0f) * OpenTaiko.Skin.SongSelect_Difficulty_Select_Bar_AnimeIn[0]));
				moveY += (((float)Math.Sin((float)ctBarAnime[i].CurrentValue * (Math.PI / 180)) * OpenTaiko.Skin.SongSelect_Difficulty_Select_Bar_Anime[1]) -
					(((float)Math.Cos((float)ctBarAnimeIn.CurrentValue * (Math.PI / 170)) + 1.0f) * OpenTaiko.Skin.SongSelect_Difficulty_Select_Bar_Anime[1]));



				OpenTaiko.Tx.Difficulty_Select_Bar[i].t2D描画(
					OpenTaiko.Skin.SongSelect_Difficulty_Select_Bar_X[n現在の選択行[i]] + moveX,
					OpenTaiko.Skin.SongSelect_Difficulty_Select_Bar_Y[n現在の選択行[i]] + moveY,
					new RectangleF(OpenTaiko.Skin.SongSelect_Difficulty_Select_Bar_Rect[0][0], OpenTaiko.Skin.SongSelect_Difficulty_Select_Bar_Rect[0][1],
					OpenTaiko.Skin.SongSelect_Difficulty_Select_Bar_Rect[0][2], OpenTaiko.Skin.SongSelect_Difficulty_Select_Bar_Rect[0][3]));
			}

			#endregion

			#endregion

			return 0;
		}

		// その他

		#region [ private ]
		//-----------------

		public bool[] bSelect = new bool[5];
		public bool[] bOption = new bool[5];

		private CCachedFontRenderer pfTitle;
		private CCachedFontRenderer pfSubTitle;
		private CTexture txTitle;
		private CTexture txSubTitle;

		private CCounter ctBarAnimeIn;
		private CCounter[] ctBarAnime = new CCounter[2];

		private int exextraAnimation;

		//0 閉じる 1 演奏オプション 2~ 難易度
		public int[] n現在の選択行;
		private int nスイッチカウント;

		private bool b裏譜面;
		//176
		private int[] BarX = new int[] { 163, 251, 367, 510, 653, 797, 797 };

		private CSound soundSelectAnnounce;

		[StructLayout(LayoutKind.Sequential)]
		private struct STレベル数字 {
			public char ch;
			public int ptX;
		}
		private STレベル数字[] st小文字位置 = new STレベル数字[10];

		private void t小文字表示(int num, float x, float y, int diff, CDTX.ELevelIcon icon) {
			int[] nums = CConversion.SeparateDigits(num);
			float[] icon_coords = new float[2] { -999, -999 };
			for (int j = 0; j < nums.Length; j++) {
				float offset = j - (nums.Length / 2.0f);
				float _x = x - (OpenTaiko.Skin.SongSelect_Difficulty_Number_Interval[0] * offset);
				float _y = y - (OpenTaiko.Skin.SongSelect_Difficulty_Number_Interval[1] * offset);

				int width = OpenTaiko.Tx.Difficulty_Number.sz画像サイズ.Width / 10;
				int height = OpenTaiko.Tx.Difficulty_Number.sz画像サイズ.Height;

				icon_coords[0] = Math.Max(icon_coords[0], _x + width);
				icon_coords[1] = _y;

				OpenTaiko.Tx.Difficulty_Number.t2D描画(_x, _y, new Rectangle(width * nums[j], 0, width, height));

				if (OpenTaiko.Tx.Difficulty_Number_Colored != null) {
					OpenTaiko.Tx.Difficulty_Number_Colored.color4 = CConversion.ColorToColor4(OpenTaiko.Skin.SongSelect_Difficulty_Colors[diff]);
					OpenTaiko.Tx.Difficulty_Number_Colored.t2D描画(_x, _y, new RectangleF(width * nums[j], 0, width, height));
				}
			}
			OpenTaiko.stageSongSelect.actSongList.tDisplayLevelIcon((int)icon_coords[0], (int)icon_coords[1], icon, OpenTaiko.Tx.Difficulty_Number_Icon);
		}

		private bool isOnOption() {
			return bOption[0] || bOption[1] || bOption[2] || bOption[3] || bOption[4];
		}

		//-----------------
		#endregion
	}
}
