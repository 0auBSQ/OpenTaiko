using System;
using System.IO;
using System.Diagnostics;
using FDK;
using System.Drawing;
using System.Collections.Generic;

namespace TJAPlayer3
{
	internal class CStage結果 : CStage
	{
		// プロパティ

		public STDGBVALUE<bool> b新記録スキル;
		public STDGBVALUE<bool> b新記録スコア;
		public STDGBVALUE<bool> b新記録ランク;
		public STDGBVALUE<float> fPerfect率;
		public STDGBVALUE<float> fGreat率;
		public STDGBVALUE<float> fGood率;
		public STDGBVALUE<float> fPoor率;
		public STDGBVALUE<float> fMiss率;
		public STDGBVALUE<bool> bオート;        // #23596 10.11.16 add ikanick
											 //        10.11.17 change (int to bool) ikanick

		public STDGBVALUE<int> nランク値;
		public STDGBVALUE<int> n演奏回数;
		public STDGBVALUE<int> nScoreRank;
		public int n総合ランク値;
		public int nクリア;        //0:未クリア 1:クリア 2:フルコンボ 3:ドンダフルコンボ
		public int nスコアランク;  //0:未取得 1:白粋 2:銅粋 3:銀粋 4:金雅 5:桃雅 6:紫雅 7:虹極
		public CDTX.CChip[] r空うちドラムチップ;
		public STDGBVALUE<CScoreIni.C演奏記録> st演奏記録;


		// コンストラクタ

		public CStage結果()
		{
			this.st演奏記録.Drums = new CScoreIni.C演奏記録();
			this.st演奏記録.Guitar = new CScoreIni.C演奏記録();
			this.st演奏記録.Bass = new CScoreIni.C演奏記録();
			this.st演奏記録.Taiko = new CScoreIni.C演奏記録();
			this.r空うちドラムチップ = new CDTX.CChip[10];
			this.n総合ランク値 = -1;
			this.nチャンネル0Atoレーン07 = new int[] { 1, 2, 3, 4, 5, 7, 6, 1, 7, 0 };
			base.eステージID = CStage.Eステージ.結果;
			base.eフェーズID = CStage.Eフェーズ.共通_通常状態;
			base.b活性化してない = true;
			base.list子Activities.Add(this.actParameterPanel = new CActResultParameterPanel());
			base.list子Activities.Add(this.actSongBar = new CActResultSongBar());
			base.list子Activities.Add(this.actOption = new CActオプションパネル());
			base.list子Activities.Add(this.actFI = new CActFIFOResult());
			base.list子Activities.Add(this.actFO = new CActFIFOBlack());
		}


		// CStage 実装

		public override void On活性化()
		{

			if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Tower)
				TJAPlayer3.Skin.bgmリザルトイン音.t再生する();
			
			Trace.TraceInformation("結果ステージを活性化します。");
			Trace.Indent();
			b最近遊んだ曲追加済み = false;
			try
			{
				{
					#region [ 初期化 ]
					//---------------------
					this.eフェードアウト完了時の戻り値 = E戻り値.継続;
					this.bアニメが完了 = false;
					this.bIsCheckedWhetherResultScreenShouldSaveOrNot = false;              // #24609 2011.3.14 yyagi
					this.n最後に再生したHHのWAV番号 = -1;
					this.n最後に再生したHHのチャンネル番号 = 0;
					
					for (int i = 0; i < 3; i++)
					{
						this.b新記録スキル[i] = false;
						this.b新記録スコア[i] = false;
						this.b新記録ランク[i] = false;
					}
					//---------------------
					#endregion

					#region [ 結果の計算 ]
					//---------------------
					for (int i = 0; i < 3; i++)
					{
						this.nランク値[i] = -1;
						this.fPerfect率[i] = this.fGreat率[i] = this.fGood率[i] = this.fPoor率[i] = this.fMiss率[i] = 0.0f;  // #28500 2011.5.24 yyagi
						if ((((i != 0) || (TJAPlayer3.DTX.bチップがある.Drums))))
						{
							CScoreIni.C演奏記録 part = this.st演奏記録[i];
							bool bIsAutoPlay = true;
							switch (i)
							{
								case 0:
									bIsAutoPlay = TJAPlayer3.ConfigIni.b太鼓パートAutoPlay;
									break;

								case 1:
									bIsAutoPlay = TJAPlayer3.ConfigIni.b太鼓パートAutoPlay;
									break;

								case 2:
									bIsAutoPlay = TJAPlayer3.ConfigIni.b太鼓パートAutoPlay;
									break;
							}
							this.fPerfect率[i] = bIsAutoPlay ? 0f : ((100f * part.nPerfect数) / ((float)part.n全チップ数));
							this.fGreat率[i] = bIsAutoPlay ? 0f : ((100f * part.nGreat数) / ((float)part.n全チップ数));
							this.fGood率[i] = bIsAutoPlay ? 0f : ((100f * part.nGood数) / ((float)part.n全チップ数));
							this.fPoor率[i] = bIsAutoPlay ? 0f : ((100f * part.nPoor数) / ((float)part.n全チップ数));
							this.fMiss率[i] = bIsAutoPlay ? 0f : ((100f * part.nMiss数) / ((float)part.n全チップ数));
							this.bオート[i] = bIsAutoPlay; // #23596 10.11.16 add ikanick そのパートがオートなら1
														//        10.11.17 change (int to bool) ikanick
							this.nランク値[i] = CScoreIni.tランク値を計算して返す(part);
						}
					}
					this.n総合ランク値 = CScoreIni.t総合ランク値を計算して返す(this.st演奏記録.Drums, this.st演奏記録.Guitar, this.st演奏記録.Bass);
					if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Tower)
					{
						this.nクリア = (this.st演奏記録.Drums.nMiss数 == 0 && this.st演奏記録.Drums.fゲージ == 100) ? this.st演奏記録.Drums.nGreat数 == 0 ? 3 : 2 : this.st演奏記録.Drums.fゲージ >= 80 ? 1 : 0;

						if (this.st演奏記録.Drums.nスコア < 500000)
						{
							this.nスコアランク = 0;
						}
						else
						{
							for (int i = 0; i < 7; i++)
							{
								if (this.st演奏記録.Drums.nスコア >= TJAPlayer3.stage演奏ドラム画面.ScoreRank.ScoreRank[i])
								{
									this.nスコアランク = i + 1;
								}
							}
						}
					}
					//---------------------
					#endregion

					#region [ .score.ini の作成と出力 ]
					//---------------------
					string str = TJAPlayer3.DTX.strファイル名の絶対パス + ".score.ini";
					CScoreIni ini = new CScoreIni(str);

					bool[] b今までにフルコンボしたことがある = new bool[] { false, false, false };

					// フルコンボチェックならびに新記録ランクチェックは、ini.Record[] が、スコアチェックや演奏型スキルチェックの IF 内で書き直されてしまうよりも前に行う。(2010.9.10)

					b今までにフルコンボしたことがある[0] = ini.stセクション[0].bフルコンボである | ini.stセクション[0].bフルコンボである;

					// #24459 上記の条件だと[HiSkill.***]でのランクしかチェックしていないので、BestRankと比較するよう変更。
					if (this.nランク値[0] >= 0 && ini.stファイル.BestRank[0] > this.nランク値[0])       // #24459 2011.3.1 yyagi update BestRank
					{
						this.b新記録ランク[0] = true;
						ini.stファイル.BestRank[0] = this.nランク値[0];
					}


					// Clear and score ranks

					if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Tower)
                    {
						// Regular (Ensou game) Score and Score Rank saves

						this.st演奏記録[0].nクリア[TJAPlayer3.stage選曲.n確定された曲の難易度[0]] = Math.Max(ini.stセクション[0].nクリア[TJAPlayer3.stage選曲.n確定された曲の難易度[0]], this.nクリア);
						this.st演奏記録[0].nスコアランク[TJAPlayer3.stage選曲.n確定された曲の難易度[0]] = Math.Max(ini.stセクション[0].nスコアランク[TJAPlayer3.stage選曲.n確定された曲の難易度[0]], this.nスコアランク);

						for (int i = 0; i < 5; i++)
						{
							if (i != TJAPlayer3.stage選曲.n確定された曲の難易度[0])
							{
								this.st演奏記録[0].nクリア[i] = ini.stセクション[0].nクリア[i];
								this.st演奏記録[0].nスコアランク[i] = ini.stセクション[0].nスコアランク[i];
							}

							ini.stセクション[0].nクリア[i] = this.st演奏記録[0].nクリア[i];
							ini.stセクション[0].nスコアランク[i] = this.st演奏記録[0].nスコアランク[i];
						}
					}
					else if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan)
                    {
						/* == Specific format for DaniDoujou charts ==
						**
						** Higher is better, takes the Clear1 spot (Usually the spot allocated for Kantan Clear crowns)
						**
						** 0 (Fugoukaku, no insign)
						** Silver Iki (Clear) : 1 (Red Goukaku) / 2 (Gold Goukaku)
						** Gold Iki (Full Combo) : 3 (Red Goukaku) / 4 (Gold Goukaku)
						** Rainbow Iki (Donda Full Combo) : 5 (Red Goukaku) / 6 (Gold Goukaku)
						**
						*/

						Exam.Status examStatus = TJAPlayer3.stage演奏ドラム画面.actDan.GetExamStatus(TJAPlayer3.stage結果.st演奏記録.Drums.Dan_C);

						int clearValue = 0;

						if (examStatus != Exam.Status.Failure)
						{
							// Red Goukaku
							clearValue += 1;

							// Gold Goukaku
							if (examStatus == Exam.Status.Better_Success)
								clearValue += 1;

							// Gold Iki
							if (this.st演奏記録.Drums.nMiss数 == 0)
                            {
								clearValue += 2;

								// Rainbow Iki
								if (this.st演奏記録.Drums.nGreat数 == 0)
									clearValue += 2;
							}
						}

						this.st演奏記録[0].nクリア[0] = Math.Max(ini.stセクション[0].nクリア[0], clearValue);
					}
					else // Tower
                    {
						// Clear if top reached, then FC or DFC like any regular chart
						// Score Rank cointains highest reached floor
						if (CFloorManagement.CurrentNumberOfLives > 0)
							this.st演奏記録[0].nクリア[0] = Math.Max(ini.stセクション[0].nクリア[0], this.nクリア);
						this.st演奏記録[0].nスコアランク[0] = Math.Max(ini.stセクション[0].nスコアランク[0], CFloorManagement.LastRegisteredFloor);

					}

					// 新記録スコアチェック
					if ((this.st演奏記録[0].nスコア > ini.stセクション[0].nスコア) && !TJAPlayer3.ConfigIni.b太鼓パートAutoPlay)
					{
						this.b新記録スコア[0] = true;
						ini.stセクション[0] = this.st演奏記録[0];
					}

					// Header hi-score
					if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Tower)
						if (this.st演奏記録[0].nスコア > ini.stセクション[0].nスコア)
							this.st演奏記録[0].nハイスコア[TJAPlayer3.stage選曲.n確定された曲の難易度[0]] = (int)st演奏記録[0].nスコア;

					// 新記録スキルチェック
					if (this.st演奏記録[0].db演奏型スキル値 > ini.stセクション[0].db演奏型スキル値)
					{
						this.b新記録スキル[0] = true;
						ini.stセクション[0] = this.st演奏記録[0];
					}

					// Clear & Score rank (Legacy)
					/*
					if(TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Tower)
					{
						if (this.nクリア > ini.stセクション[0].nクリア[TJAPlayer3.stage選曲.n確定された曲の難易度[0]])
							ini.stセクション[0].nクリア[TJAPlayer3.stage選曲.n確定された曲の難易度[0]] = this.nクリア;
						if (this.nスコアランク > ini.stセクション[0].nスコアランク[TJAPlayer3.stage選曲.n確定された曲の難易度[0]])
							ini.stセクション[0].nスコアランク[TJAPlayer3.stage選曲.n確定された曲の難易度[0]] = this.nスコアランク;
					}
					*/

					// ラストプレイ #23595 2011.1.9 ikanick
					// オートじゃなければプレイ結果を書き込む
					if (TJAPlayer3.ConfigIni.b太鼓パートAutoPlay == false)
					{
						ini.stセクション[0] = this.st演奏記録[0];
					}

					// #23596 10.11.16 add ikanick オートじゃないならクリア回数を1増やす
					//        11.02.05 bオート to t更新条件を取得する use      ikanick
					bool[] b更新が必要か否か = new bool[3];
					CScoreIni.t更新条件を取得する(out b更新が必要か否か[0], out b更新が必要か否か[1], out b更新が必要か否か[2]);

					if (b更新が必要か否か[0])
					{
						ini.stファイル.ClearCountDrums++;
					}
					//---------------------------------------------------------------------/
					if (TJAPlayer3.ConfigIni.bScoreIniを出力する)
						ini.t書き出し(str);

					//---------------------
					#endregion

					#region [ リザルト画面への演奏回数の更新 #24281 2011.1.30 yyagi]
					if (TJAPlayer3.ConfigIni.bScoreIniを出力する)
					{
						this.n演奏回数.Drums = ini.stファイル.PlayCountDrums;
						this.n演奏回数.Guitar = ini.stファイル.PlayCountGuitar;
						this.n演奏回数.Bass = ini.stファイル.PlayCountBass;
					}
					#endregion
				}

				// Discord Presenseの更新
				Discord.UpdatePresence(TJAPlayer3.DTX.TITLE + ".tja", Properties.Discord.Stage_Result + (TJAPlayer3.ConfigIni.b太鼓パートAutoPlay == true ? " (" + Properties.Discord.Info_IsAuto + ")" : ""), TJAPlayer3.StartupTime);

				base.On活性化();
			}
			finally
			{
				Trace.TraceInformation("結果ステージの活性化を完了しました。");
				Trace.Unindent();
			}
		}
		public override void On非活性化()
		{
			if (this.rResultSound != null)
			{
				TJAPlayer3.Sound管理.tサウンドを破棄する(this.rResultSound);
				this.rResultSound = null;
			}
			base.On非活性化();
		}
		public override void OnManagedリソースの作成()
		{
			if (!base.b活性化してない)
			{
				b音声再生 = false;
				this.EndAnime = false;
				//this.tx背景 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\8_background.png" ) );
				//this.tx上部パネル = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\8_header.png" ) );
				//this.tx下部パネル = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\8_footer panel.png" ), true );
				//this.txオプションパネル = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\Screen option panels.png" ) );

				ctShine_Plate = new CCounter(0, 1000, 1, TJAPlayer3.Timer);
				ctWork_Plate = new CCounter(0, 4000, 1, TJAPlayer3.Timer);

				if (TJAPlayer3.Tx.TowerResult_Background != null)
					ctTower_Animation = new CCounter(0, TJAPlayer3.Tx.TowerResult_Background.szテクスチャサイズ.Height - 720, 25, TJAPlayer3.Timer);
				else
					ctTower_Animation = new CCounter();

				Dan_Plate = TJAPlayer3.tテクスチャの生成(Path.GetDirectoryName(TJAPlayer3.DTX.strファイル名の絶対パス) + @"\Dan_Plate.png");


				base.OnManagedリソースの作成();
			}
		}
		public override void OnManagedリソースの解放()
		{
			if (!base.b活性化してない)
			{
				if (this.ct登場用 != null)
				{
					this.ct登場用 = null;
				}
				//CDTXMania.tテクスチャの解放( ref this.tx背景 );
				//CDTXMania.tテクスチャの解放( ref this.tx上部パネル );
				//CDTXMania.tテクスチャの解放( ref this.tx下部パネル );
				//CDTXMania.tテクスチャの解放( ref this.txオプションパネル );
				Dan_Plate?.Dispose();

				base.OnManagedリソースの解放();
			}
		}
		public override int On進行描画()
		{
			if (!base.b活性化してない)
			{

				ctShine_Plate.t進行Loop();

				int num;
				if (base.b初めての進行描画)
				{
					this.ct登場用 = new CCounter(0, 100, 5, TJAPlayer3.Timer);
					this.actFI.tフェードイン開始();
					base.eフェーズID = CStage.Eフェーズ.共通_フェードイン;

					if (this.rResultSound != null)
					{
						this.rResultSound.t再生を開始する();
					}

					base.b初めての進行描画 = false;
				}
				this.bアニメが完了 = true;
				if (this.ct登場用.b進行中)
				{
					this.ct登場用.t進行();
					if (this.ct登場用.b終了値に達した)
					{
						this.ct登場用.t停止();
					}
					else
					{
						this.bアニメが完了 = false;
					}
				}

				// 描画


				if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Tower)
                {
                    #region [Ensou game result screen]

                    if (!b音声再生 && !TJAPlayer3.Skin.bgmリザルトイン音.b再生中)
					{
						TJAPlayer3.Skin.bgmリザルト音.t再生する();
						b音声再生 = true;
					}

					if (TJAPlayer3.Tx.Result_Background != null)
					{

						int CloudType = 0;
						float MountainAppearValue = 2000 + (this.actParameterPanel.ctゲージアニメ.n終了値 * 66) + 8360 - 85;

						if (this.actParameterPanel.ct全体進行.n現在の値 >= MountainAppearValue)
						{
							#region [Mountain Bump]

							if (this.st演奏記録.Drums.fゲージ >= 80.0)
							{
								TJAPlayer3.Tx.Result_Background[1].Opacity = (this.actParameterPanel.ct全体進行.n現在の値 - (10275 + ((int)this.actParameterPanel.ctゲージアニメ.n終了値 * 66))) * 3;
								TJAPlayer3.Tx.Result_Mountain[1].Opacity = (this.actParameterPanel.ct全体進行.n現在の値 - (10275 + ((int)this.actParameterPanel.ctゲージアニメ.n終了値 * 66))) * 3;
								TJAPlayer3.Tx.Result_Mountain[0].Opacity = 255 - (this.actParameterPanel.ct全体進行.n現在の値 - (10275 + ((int)this.actParameterPanel.ctゲージアニメ.n終了値 * 66))) * 3;

								if (this.actParameterPanel.ctMountain_ClearIn.n現在の値 <= 90)
								{
									TJAPlayer3.Tx.Result_Mountain[1].vc拡大縮小倍率.Y = 1.0f - (float)Math.Sin((float)this.actParameterPanel.ctMountain_ClearIn.n現在の値 * (Math.PI / 180)) * 0.18f;
								}
								else if (this.actParameterPanel.ctMountain_ClearIn.n現在の値 <= 225)
								{
									TJAPlayer3.Tx.Result_Mountain[1].vc拡大縮小倍率.Y = 0.82f + (float)Math.Sin((float)(this.actParameterPanel.ctMountain_ClearIn.n現在の値 - 90) / 1.5f * (Math.PI / 180)) * 0.58f;
								}
								else if (this.actParameterPanel.ctMountain_ClearIn.n現在の値 <= 245)
								{
									TJAPlayer3.Tx.Result_Mountain[1].vc拡大縮小倍率.Y = 1.4f;
								}
								else if (this.actParameterPanel.ctMountain_ClearIn.n現在の値 <= 335)
								{
									TJAPlayer3.Tx.Result_Mountain[1].vc拡大縮小倍率.Y = 0.9f + (float)Math.Sin((float)(this.actParameterPanel.ctMountain_ClearIn.n現在の値 - 155) * (Math.PI / 180)) * 0.5f;
								}
								else if (this.actParameterPanel.ctMountain_ClearIn.n現在の値 <= 515)
								{
									TJAPlayer3.Tx.Result_Mountain[1].vc拡大縮小倍率.Y = 0.9f + (float)Math.Sin((float)(this.actParameterPanel.ctMountain_ClearIn.n現在の値 - 335) * (Math.PI / 180)) * 0.4f;
								}
							}

							#endregion
						}
						else
						{

							TJAPlayer3.Tx.Result_Background[1].Opacity = 0;
							TJAPlayer3.Tx.Result_Mountain[0].Opacity = 255;
							TJAPlayer3.Tx.Result_Mountain[1].Opacity = 0;
						}

						TJAPlayer3.Tx.Result_Background[0].t2D描画(TJAPlayer3.app.Device, 0, 0);
						TJAPlayer3.Tx.Result_Background[1].t2D描画(TJAPlayer3.app.Device, 0, 0);
						TJAPlayer3.Tx.Result_Mountain[0].t2D描画(TJAPlayer3.app.Device, 0, 0);
						TJAPlayer3.Tx.Result_Mountain[1].t2D拡大率考慮下基準描画(TJAPlayer3.app.Device, 0, 720);

						// TJAPlayer3.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.白, ctShine_Plate.n現在の値.ToString());
						// TJAPlayer3.act文字コンソール.tPrint(10, 10, C文字コンソール.Eフォント種別.白, this.actParameterPanel.ct全体進行.n現在の値.ToString());

						#region [Background Clouds]

						if (this.st演奏記録.Drums.fゲージ >= 80.0 && this.actParameterPanel.ct全体進行.n現在の値 >= MountainAppearValue)
						{
							CloudType = Math.Min(255, Math.Max(0, (int)this.actParameterPanel.ct全体進行.n現在の値 - (int)MountainAppearValue));
						}

						for (int i = 10; i >= 0; i--)
						{
							int CurMoveRed = (int)((double)CloudMaxMove[i] * Math.Tanh((double)this.actParameterPanel.ct全体進行.n現在の値 / 10000));
							int CurMoveGold = (int)((double)CloudMaxMove[i] * Math.Tanh(Math.Max(0, (double)this.actParameterPanel.ct全体進行.n現在の値 - (double)MountainAppearValue) / 10000));

							TJAPlayer3.Tx.Result_Cloud.vc拡大縮小倍率.X = 0.65f;
							TJAPlayer3.Tx.Result_Cloud.vc拡大縮小倍率.Y = 0.65f;
							TJAPlayer3.Tx.Result_Cloud.Opacity = CloudType;

							TJAPlayer3.Tx.Result_Cloud.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, CloudXPos[i] - CurMoveGold, CloudYPos[i], new Rectangle(i * 1200, 360, 1200, 360));

							TJAPlayer3.Tx.Result_Cloud.Opacity = 255 - CloudType;

							TJAPlayer3.Tx.Result_Cloud.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, CloudXPos[i] - CurMoveRed, CloudYPos[i], new Rectangle(i * 1200, 0, 1200, 360));
						}

						#endregion

						if (TJAPlayer3.stage結果.st演奏記録[0].fゲージ >= 80.0f && this.actParameterPanel.ct全体進行.n現在の値 >= MountainAppearValue)
						{

							#region [Background shines]

							int ShineTime = (int)ctShine_Plate.n現在の値;
							int Quadrant500 = ShineTime % 500;

							for (int i = 0; i < 6; i++)
							{
								if (i < 2 && ShineTime >= 500 || i >= 2 && ShineTime < 500)
									TJAPlayer3.Tx.Result_Shine.Opacity = 0;
								else if (Quadrant500 >= ShinePFade && Quadrant500 <= 500 - ShinePFade)
									TJAPlayer3.Tx.Result_Shine.Opacity = 255;
								else
									TJAPlayer3.Tx.Result_Shine.Opacity = (255 * Math.Min(Quadrant500, 500 - Quadrant500)) / ShinePFade;

								TJAPlayer3.Tx.Result_Shine.vc拡大縮小倍率.X = ShinePSize[i];
								TJAPlayer3.Tx.Result_Shine.vc拡大縮小倍率.Y = ShinePSize[i];

								TJAPlayer3.Tx.Result_Shine.t2D中心基準描画(TJAPlayer3.app.Device, ShinePXPos[i] + 80, ShinePYPos[i]);
							}

							#endregion

							#region [Fireworks]

							// Primary pop
							if (this.actParameterPanel.ct全体進行.n現在の値 <= MountainAppearValue + 1000)
							{
								for (int i = 0; i < 3; i++)
								{
									if (this.actParameterPanel.ct全体進行.n現在の値 <= MountainAppearValue + 255)
									{
										int TmpTimer = (int)(this.actParameterPanel.ct全体進行.n現在の値 - MountainAppearValue);

										TJAPlayer3.Tx.Result_Work[i].Opacity = TmpTimer;
										TJAPlayer3.Tx.Result_Work[i].vc拡大縮小倍率.X = 0.6f * ((float)TmpTimer / 225f);
										TJAPlayer3.Tx.Result_Work[i].vc拡大縮小倍率.Y = 0.6f * ((float)TmpTimer / 225f);
									}
									else
									{
										int TmpTimer = Math.Max(0, (2 * 255) - (int)(this.actParameterPanel.ct全体進行.n現在の値 - MountainAppearValue - 255));

										TJAPlayer3.Tx.Result_Work[i].Opacity = TmpTimer / 2;
										TJAPlayer3.Tx.Result_Work[i].vc拡大縮小倍率.X = 0.6f;
										TJAPlayer3.Tx.Result_Work[i].vc拡大縮小倍率.Y = 0.6f;
									}
									TJAPlayer3.Tx.Result_Work[i].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, WorksPosX[i], WorksPosY[i]);
								}
							}
							else
							{
								ctWork_Plate.t進行Loop();

								for (int i = 0; i < 3; i++)
								{
									int TmpStamp = WorksTimeStamp[i];

									if (ctWork_Plate.n現在の値 <= TmpStamp + 255)
									{
										int TmpTimer = (int)(ctWork_Plate.n現在の値 - TmpStamp);

										TJAPlayer3.Tx.Result_Work[i].Opacity = TmpTimer;
										TJAPlayer3.Tx.Result_Work[i].vc拡大縮小倍率.X = 0.6f * ((float)TmpTimer / 225f);
										TJAPlayer3.Tx.Result_Work[i].vc拡大縮小倍率.Y = 0.6f * ((float)TmpTimer / 225f);
									}
									else
									{
										int TmpTimer = Math.Max(0, (2 * 255) - (int)(ctWork_Plate.n現在の値 - TmpStamp - 255));

										TJAPlayer3.Tx.Result_Work[i].Opacity = TmpTimer / 2;
										TJAPlayer3.Tx.Result_Work[i].vc拡大縮小倍率.X = 0.6f;
										TJAPlayer3.Tx.Result_Work[i].vc拡大縮小倍率.Y = 0.6f;
									}
									TJAPlayer3.Tx.Result_Work[i].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, WorksPosX[i], WorksPosY[i]);
								}
							}

							#endregion

						}

					}

					if (this.ct登場用.b進行中 && (TJAPlayer3.Tx.Result_Header != null))
					{
						double num2 = ((double)this.ct登場用.n現在の値) / 100.0;
						double num3 = Math.Sin(Math.PI / 2 * num2);
						num = ((int)(TJAPlayer3.Tx.Result_Header.sz画像サイズ.Height * num3)) - TJAPlayer3.Tx.Result_Header.sz画像サイズ.Height;
					}
					else
					{
						num = 0;
					}

					if (!b音声再生 && !TJAPlayer3.Skin.bgmリザルトイン音.b再生中)
					{
						TJAPlayer3.Skin.bgmリザルト音.t再生する();
						b音声再生 = true;
					}

					if (TJAPlayer3.Tx.Result_Header != null)
					{
						TJAPlayer3.Tx.Result_Header.t2D描画(TJAPlayer3.app.Device, 0, 0);
					}

					#endregion

				}
				else
                {
					if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan)
                    {

						#region [DaniDoujou result screen]

						if (!b音声再生 && !TJAPlayer3.Skin.bgmDanResult.b再生中)
						{
							TJAPlayer3.Skin.bgmDanResult.t再生する();
							b音声再生 = true;
						}

						TJAPlayer3.Tx.DanResult_Background.t2D描画(TJAPlayer3.app.Device, 0, 0);
						TJAPlayer3.Tx.DanResult_SongPanel_Base.t2D描画(TJAPlayer3.app.Device, 0, 0);

						#region [DanPlate]

						// To add : Animation at 1 sec

						Dan_Plate?.t2D中心基準描画(TJAPlayer3.app.Device, 138, 220);

						#endregion

						#region [Charts Individual Results]

						for (int i = 0; i < TJAPlayer3.stage選曲.r確定された曲.DanSongs.Count; i++)
                        {
							// To alter in order to shift the whole tab
							int baseX = 255;

							int baseY = 100 + 183 * i;

							TJAPlayer3.Tx.DanResult_SongPanel_Main.t2D描画(TJAPlayer3.app.Device, baseX, baseY, new Rectangle(0, 1 + 170 * Math.Min(i, 2), 960, 170));
						}

						#endregion

						/*
						int TmpTimer = Math.Max(0, (2 * 255) - (int)(this.actParameterPanel.ct全体進行.n現在の値 - MountainAppearValue - 255));
						*/

						// TJAPlayer3.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.白, ctMob.n現在の値.ToString());

						#region [PassLogo]

						Exam.Status examStatus = TJAPlayer3.stage演奏ドラム画面.actDan.GetExamStatus(TJAPlayer3.stage結果.st演奏記録.Drums.Dan_C);

						if (examStatus != Exam.Status.Failure)
                        {
							int successType = 0;

							if (examStatus == Exam.Status.Better_Success)
								successType += 1;

							int comboType = 0;
							if (this.st演奏記録.Drums.nMiss数 == 0)
                            {
								comboType += 1;

								if (this.st演奏記録.Drums.nGreat数 == 0)
									comboType += 1;
							}

							TJAPlayer3.Tx.DanResult_Rank.vc拡大縮小倍率.X = 1f;
							TJAPlayer3.Tx.DanResult_Rank.vc拡大縮小倍率.Y = 1f;
							TJAPlayer3.Tx.DanResult_Rank.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 130, 380, new Rectangle(334 * (2 * comboType + successType), 0, 334, 334));

						}

						#endregion

						if (!b音声再生 && !TJAPlayer3.Skin.bgmDanResult.b再生中)
						{
							TJAPlayer3.Skin.bgmDanResult.t再生する();
							b音声再生 = true;
						}

						#endregion

					}
                    else
                    {
						#region [Tower result screen]

						if (!b音声再生 && !TJAPlayer3.Skin.bgmTowerResult.b再生中)
						{
							TJAPlayer3.Skin.bgmTowerResult.t再生する();
							b音声再生 = true;
						}

						// Pictures here

						this.ctTower_Animation.t進行();

						int xFactor = 0;
						float yFactor = 1f;
						if (TJAPlayer3.Tx.TowerResult_Background != null && TJAPlayer3.Tx.TowerResult_Tower[0] != null)
                        {
							xFactor = (TJAPlayer3.Tx.TowerResult_Background.szテクスチャサイズ.Width - TJAPlayer3.Tx.TowerResult_Tower[0].szテクスチャサイズ.Width) / 2;
							yFactor = TJAPlayer3.Tx.TowerResult_Tower[0].szテクスチャサイズ.Height / (float)TJAPlayer3.Tx.TowerResult_Background.szテクスチャサイズ.Height;
						}

						TJAPlayer3.Tx.TowerResult_Background?.t2D描画(TJAPlayer3.app.Device, 0, -1 * this.ctTower_Animation.n現在の値);
						TJAPlayer3.Tx.TowerResult_Tower[0]?.t2D描画(TJAPlayer3.app.Device, xFactor, -1 * yFactor * this.ctTower_Animation.n現在の値);
						TJAPlayer3.Tx.TowerResult_Panel?.t2D描画(TJAPlayer3.app.Device, 0, 0);

						if (!b音声再生 && !TJAPlayer3.Skin.bgmTowerResult.b再生中)
						{
							TJAPlayer3.Skin.bgmTowerResult.t再生する();
							b音声再生 = true;
						}


						#endregion
					}


                }



                

				if (this.actParameterPanel.On進行描画() == 0)
				{
					this.bアニメが完了 = false;
				}

				if (this.actSongBar.On進行描画() == 0)
				{
					this.bアニメが完了 = false;
				}

				#region ネームプレート
				for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
				{
					// To change while implementing the 2P result screen

					TJAPlayer3.NamePlate.tNamePlateDraw(28, 621, 0);
					// TJAPlayer3.NamePlate.tNamePlateDraw(28, 621, i);
				}
				#endregion

				if (base.eフェーズID == CStage.Eフェーズ.共通_フェードイン)
				{
					if (this.actFI.On進行描画() != 0)
					{
						base.eフェーズID = CStage.Eフェーズ.共通_通常状態;
					}
				}
				else if ((base.eフェーズID == CStage.Eフェーズ.共通_フェードアウト))         //&& ( this.actFO.On進行描画() != 0 ) )
				{
					return (int)this.eフェードアウト完了時の戻り値;
				}

				#region [ #24609 2011.3.14 yyagi ランク更新or演奏型スキル更新時、リザルト画像をpngで保存する ]
				if (this.bアニメが完了 == true && this.bIsCheckedWhetherResultScreenShouldSaveOrNot == false  // #24609 2011.3.14 yyagi; to save result screen in case BestRank or HiSkill.
					&& TJAPlayer3.ConfigIni.bScoreIniを出力する
					&& TJAPlayer3.ConfigIni.bIsAutoResultCapture)                                               // #25399 2011.6.9 yyagi
				{
					CheckAndSaveResultScreen(true);
					this.bIsCheckedWhetherResultScreenShouldSaveOrNot = true;
				}
				#endregion

				// キー入力

				if (TJAPlayer3.act現在入力を占有中のプラグイン == null)
				{
					if (base.eフェーズID == CStage.Eフェーズ.共通_通常状態)
					{
						if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.Escape))
						{
							TJAPlayer3.Skin.bgmリザルト音.t停止する();
							TJAPlayer3.Skin.sound決定音.t再生する();
							actFI.tフェードアウト開始();
							t後処理();
							base.eフェーズID = CStage.Eフェーズ.共通_フェードアウト;
							this.eフェードアウト完了時の戻り値 = E戻り値.完了;
						}
						if (((TJAPlayer3.Pad.b押されたDGB(Eパッド.CY) || TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RD)) || (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LC) || (TJAPlayer3.Pad.b押されたDGB(Eパッド.LRed) || (TJAPlayer3.Pad.b押されたDGB(Eパッド.RRed) || TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.Return))))))
						{
							TJAPlayer3.Skin.sound決定音.t再生する();
							actFI.tフェードアウト開始();

							if(TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan)
								if (TJAPlayer3.stage選曲.r現在選択中の曲.r親ノード != null)
									TJAPlayer3.stage選曲.act曲リスト.tBOXを出る();

							t後処理();

							{
								base.eフェーズID = CStage.Eフェーズ.共通_フェードアウト;
								this.eフェードアウト完了時の戻り値 = E戻り値.完了;
								TJAPlayer3.Skin.bgmリザルト音.t停止する();
								TJAPlayer3.Skin.bgmDanResult.t停止する();
								TJAPlayer3.Skin.bgmTowerResult.t停止する();
								TJAPlayer3.Skin.sound決定音.t再生する();
							}
						}
					}
				}
			}
			return 0;
		}

		public void t後処理()
        {
			// To check and correct later
			if (!TJAPlayer3.ConfigIni.bAutoPlay[0])
			{
				if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Tower)
				{
					if (nスコアランク != 0)
					{
						if (TJAPlayer3.stage選曲.r確定されたスコア.譜面情報.nスコアランク[TJAPlayer3.stage選曲.n確定された曲の難易度[0]] == 0)
						{
							TJAPlayer3.stage選曲.act曲リスト.ScoreRankCount[nスコアランク - 1] += 1;
						}
						else if (TJAPlayer3.stage選曲.r確定されたスコア.譜面情報.nスコアランク[TJAPlayer3.stage選曲.n確定された曲の難易度[0]] < nスコアランク)
						{
							TJAPlayer3.stage選曲.act曲リスト.ScoreRankCount[nスコアランク - 1] += 1;
							TJAPlayer3.stage選曲.act曲リスト.ScoreRankCount[TJAPlayer3.stage選曲.r確定されたスコア.譜面情報.nスコアランク[TJAPlayer3.stage選曲.n確定された曲の難易度[0]] - 1] -= 1;
						}
					}

					if (nクリア != 0)
					{
						if (TJAPlayer3.stage選曲.r確定されたスコア.譜面情報.nクリア[TJAPlayer3.stage選曲.n確定された曲の難易度[0]] == 0)
						{
							TJAPlayer3.stage選曲.act曲リスト.CrownCount[nクリア - 1] += 1;
						}
						else if (TJAPlayer3.stage選曲.r確定されたスコア.譜面情報.nクリア[TJAPlayer3.stage選曲.n確定された曲の難易度[0]] < nクリア)
						{
							TJAPlayer3.stage選曲.act曲リスト.CrownCount[nクリア - 1] += 1;
							TJAPlayer3.stage選曲.act曲リスト.CrownCount[TJAPlayer3.stage選曲.r確定されたスコア.譜面情報.nクリア[TJAPlayer3.stage選曲.n確定された曲の難易度[0]] - 1] -= 1;
						}

					}
				}
			}

			if (!b最近遊んだ曲追加済み)
			{
				#region [ 選曲画面の譜面情報の更新 ]
				//---------------------
				if (!TJAPlayer3.bコンパクトモード)
				{
					if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Tower)
					{
						Cスコア cスコア = TJAPlayer3.stage選曲.r確定されたスコア;

						if (cスコア.譜面情報.nクリア[TJAPlayer3.stage選曲.n確定された曲の難易度[0]] < nクリア)
							cスコア.譜面情報.nクリア[TJAPlayer3.stage選曲.n確定された曲の難易度[0]] = this.nクリア;

						if (cスコア.譜面情報.nスコアランク[TJAPlayer3.stage選曲.n確定された曲の難易度[0]] < nスコアランク)
							cスコア.譜面情報.nスコアランク[TJAPlayer3.stage選曲.n確定された曲の難易度[0]] = this.nスコアランク;
					}
					else if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan)
                    {
						Cスコア cスコア = TJAPlayer3.stage選曲.r確定されたスコア;

						Exam.Status examStatus = TJAPlayer3.stage演奏ドラム画面.actDan.GetExamStatus(TJAPlayer3.stage結果.st演奏記録.Drums.Dan_C);

						int clearValue = 0;

						if (examStatus != Exam.Status.Failure)
						{
							// Red Goukaku
							clearValue += 1;

							// Gold Goukaku
							if (examStatus == Exam.Status.Better_Success)
								clearValue += 1;

							// Gold Iki
							if (this.st演奏記録.Drums.nMiss数 == 0)
							{
								clearValue += 2;

								// Rainbow Iki
								if (this.st演奏記録.Drums.nGreat数 == 0)
									clearValue += 2;
							}
						}
						cスコア.譜面情報.nクリア[0] = Math.Max(cスコア.譜面情報.nクリア[0], clearValue);
					}
				}
				//---------------------
				#endregion

				foreach (var song in TJAPlayer3.Songs管理.list曲ルート)
				{
					if (song.strジャンル == "最近遊んだ曲" && song.eノード種別 == C曲リストノード.Eノード種別.BOX)
					{
						song.list子リスト.Add(TJAPlayer3.stage選曲.r確定された曲.Clone());

						foreach (var song2 in song.list子リスト)
						{
							song2.r親ノード = song;
							song2.strジャンル = "最近遊んだ曲";

							if (song2.eノード種別 != C曲リストノード.Eノード種別.BACKBOX)
								song2.BackColor = ColorTranslator.FromHtml("#164748");
						}

						if (song.list子リスト.Count >= 6)
						{
							song.list子リスト.RemoveAt(1);
						}
					}
				}

				b最近遊んだ曲追加済み = true;
			}

		}

		public enum E戻り値 : int
		{
			継続,
			完了
		}

		// その他

		#region [ private ]
		//-----------------

		public bool b最近遊んだ曲追加済み;
		public bool b音声再生;
		public bool EndAnime;

		private CCounter ct登場用;
		private E戻り値 eフェードアウト完了時の戻り値;
		private CActFIFOResult actFI;
		private CActFIFOBlack actFO;
		private CActオプションパネル actOption;
		private CActResultParameterPanel actParameterPanel;

		private CActResultRank actRank;
		private CActResultImage actResultImage;

		private CActResultSongBar actSongBar;
		private bool bアニメが完了;
		private bool bIsCheckedWhetherResultScreenShouldSaveOrNot;              // #24509 2011.3.14 yyagi
		private readonly int[] nチャンネル0Atoレーン07;
		private int n最後に再生したHHのWAV番号;
		private int n最後に再生したHHのチャンネル番号;
		private CSound rResultSound;

		// Cloud informations
		private int[] CloudXPos = { 642, 612, 652, 1148, 1180, 112, 8, 1088, 1100, 32, 412 };
		private int[] CloudYPos = { 202, 424, 636, 530, 636, 636, 102, 52, 108, 326, 644 };
		private int[] CloudMaxMove = { 150, 120, 180, 60, 90, 150, 120, 50, 45, 120, 180 };

		// Shines informations
		private CCounter ctShine_Plate;
		private int[] ShinePXPos = { 805, 1175, 645, 810, 1078, 1060 };
		private int[] ShinePYPos = { 650, 405, 645, 420, 202, 585 };
		private float[] ShinePSize = { 0.44f, 0.6f, 0.4f, 0.15f, 0.35f, 0.6f };
		private int ShinePFade = 100;

		// Fireworks informations
		private CCounter ctWork_Plate;
		private int[] WorksPosX = { 800, 900, 1160 };
		private int[] WorksPosY = { 435, 185, 260 };
		private int[] WorksTimeStamp = { 1000, 2000, 3000 };

		// Dan informations
		private CTexture Dan_Plate;

		// Tower informations
		private CCounter ctTower_Animation;

		private CCounter ctAutoReturn;
		//private CTexture txオプションパネル;
		//private CTexture tx下部パネル;
		//private CTexture tx上部パネル;
		//private CTexture tx背景;

		#region [ #24609 リザルト画像をpngで保存する ]		// #24609 2011.3.14 yyagi; to save result screen in case BestRank or HiSkill.
		/// <summary>
		/// リザルト画像のキャプチャと保存。
		/// 自動保存モード時は、ランク更新or演奏型スキル更新時に自動保存。
		/// 手動保存モード時は、ランクに依らず保存。
		/// </summary>
		/// <param name="bIsAutoSave">true=自動保存モード, false=手動保存モード</param>
		private void CheckAndSaveResultScreen(bool bIsAutoSave)
		{
			string path = Path.GetDirectoryName(TJAPlayer3.DTX.strファイル名の絶対パス);
			string datetime = DateTime.Now.ToString("yyyyMMddHHmmss");
			if (bIsAutoSave)
			{
				// リザルト画像を自動保存するときは、dtxファイル名.yyMMddHHmmss_DRUMS_SS.png という形式で保存。
				for (int i = 0; i < 3; i++)
				{
					if (this.b新記録ランク[i] == true || this.b新記録スキル[i] == true)
					{
						string strPart = ((E楽器パート)(i)).ToString();
						string strRank = ((CScoreIni.ERANK)(this.nランク値[i])).ToString();
						string strFullPath = TJAPlayer3.DTX.strファイル名の絶対パス + "." + datetime + "_" + strPart + "_" + strRank + ".png";
						//Surface.ToFile( pSurface, strFullPath, ImageFileFormat.Png );
						TJAPlayer3.app.SaveResultScreen(strFullPath);
					}
				}
			}
			#region [ #24609 2011.4.11 yyagi; リザルトの手動保存ロジックは、CDTXManiaに移管した。]
			//			else
			//			{
			//				// リザルト画像を手動保存するときは、dtxファイル名.yyMMddHHmmss_SS.png という形式で保存。(楽器名無し)
			//				string strRank = ( (CScoreIni.ERANK) ( CDTXMania.stage結果.n総合ランク値 ) ).ToString();
			//				string strSavePath = CDTXMania.strEXEのあるフォルダ + "\\" + "Capture_img";
			//				if ( !Directory.Exists( strSavePath ) )
			//				{
			//					try
			//					{
			//						Directory.CreateDirectory( strSavePath );
			//					}
			//					catch
			//					{
			//					}
			//				}
			//				string strFullPath = strSavePath + "\\" + CDTXMania.DTX.TITLE +
			//					"." + datetime + "_" + strRank + ".png";
			//				// Surface.ToFile( pSurface, strFullPath, ImageFileFormat.Png );
			//				CDTXMania.app.SaveResultScreen( strFullPath );
			//			}
			#endregion
		}
		#endregion
		//-----------------
		#endregion
	}
}
