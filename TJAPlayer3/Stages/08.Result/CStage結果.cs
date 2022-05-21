using System;
using System.IO;
using System.Diagnostics;
using FDK;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using static TJAPlayer3.CActSelect曲リスト;

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

		public int[] nクリア = { 0, 0 };        //0:未クリア 1:クリア 2:フルコンボ 3:ドンダフルコンボ
		public int[] nスコアランク = { 0, 0 };  //0:未取得 1:白粋 2:銅粋 3:銀粋 4:金雅 5:桃雅 6:紫雅 7:虹極
		public int[] nHighScore = { 0, 0 };

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


		public bool isAutoDisabled(int player)
        {
			return ((player == 0 && !TJAPlayer3.ConfigIni.b太鼓パートAutoPlay)
					|| (player == 1 && !TJAPlayer3.ConfigIni.b太鼓パートAutoPlay2P && TJAPlayer3.ConfigIni.nAILevel == 0));
		}


		public int GetTowerScoreRank()
        {
			int tmpClear = 0;
			double progress = CFloorManagement.LastRegisteredFloor / (double)TJAPlayer3.stage選曲.r確定された曲.arスコア[5].譜面情報.nTotalFloor;

			// Clear badges : 10% (E), 25% (D), 50% (C), 75% (B), Clear (A), FC (S), DFC (X)
			bool[] conditions =
			{
				progress >= 0.1,
				progress >= 0.25,
				progress >= 0.5,
				progress >= 0.75,
				progress == 1 && CFloorManagement.CurrentNumberOfLives > 0,
				this.st演奏記録.Drums.nMiss数 == 0,
				this.st演奏記録.Drums.nGreat数 == 0
			};

			for (int i = 0; i < conditions.Length; i++)
			{
				if (conditions[i] == true)
					tmpClear++;
				else
					break;
			}

			return tmpClear;
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

                    #region [ Results calculus ]
                    //---------------------

                    #region [ Maybe legacy ? ]

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

					#endregion

					if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Tower)
					{
						for (int p = 0; p < TJAPlayer3.ConfigIni.nPlayerCount; p++)
                        {
							var ccf = TJAPlayer3.stage演奏ドラム画面.CChartScore[p];

							this.nクリア[p] = (ccf.nMiss == 0 && TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[p] == 100) 
								? ccf.nGood == 0 
								? 3 : 2 
								: TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[p] >= 80 
								? 1 
								: 0;

							if ((int)TJAPlayer3.stage演奏ドラム画面.actScore.Get(E楽器パート.DRUMS, p) < 500000)
							{
								this.nスコアランク[p] = 0;
							}
							else
							{
								var sr = (p == 0) ? TJAPlayer3.stage演奏ドラム画面.ScoreRank.ScoreRank : TJAPlayer3.stage演奏ドラム画面.ScoreRank.ScoreRank2P;

								for (int i = 0; i < 7; i++)
								{
									if ((int)TJAPlayer3.stage演奏ドラム画面.actScore.Get(E楽器パート.DRUMS, p) >= sr[i])
									{
										this.nスコアランク[p] = i + 1;
									}
								}
							}
						}

						
					}

					//---------------------
					#endregion



					#region [ .score.ini file output ]
					//---------------------

					int currentSaveFile = TJAPlayer3.SaveFile + 1;
					int secondSaveFile = (currentSaveFile == 1) ? 2 : 1;

					string[] str = {
						TJAPlayer3.DTX.strファイル名の絶対パス + currentSaveFile.ToString() + "P.score.ini",
						TJAPlayer3.DTX.strファイル名の絶対パス + secondSaveFile.ToString() + "P.score.ini"
					};

					#region [Transfer legacy file format to new file format (P1)]

					string legacyStr = TJAPlayer3.DTX.strファイル名の絶対パス + ".score.ini";

					if (!File.Exists(str[TJAPlayer3.GetActualPlayer(0)]) && File.Exists(legacyStr))
                    {
						if (TJAPlayer3.ConfigIni.bScoreIniを出力する)
                        {
							CScoreIni tmpini = new CScoreIni(legacyStr);
							tmpini.t書き出し(str[TJAPlayer3.GetActualPlayer(0)]);
						}
                    }

					#endregion

					CScoreIni[] ini = {
						new CScoreIni(str[0]),
						new CScoreIni(str[1])
					};

					bool[] b今までにフルコンボしたことがある = new bool[] { false, false, false };

					// フルコンボチェックならびに新記録ランクチェックは、ini.Record[] が、スコアチェックや演奏型スキルチェックの IF 内で書き直されてしまうよりも前に行う。(2010.9.10)

					b今までにフルコンボしたことがある[0] = ini[0].stセクション[0].bフルコンボである | ini[0].stセクション[0].bフルコンボである;

					// #24459 上記の条件だと[HiSkill.***]でのランクしかチェックしていないので、BestRankと比較するよう変更。
					if (this.nランク値[0] >= 0 && ini[0].stファイル.BestRank[0] > this.nランク値[0])       // #24459 2011.3.1 yyagi update BestRank
					{
						this.b新記録ランク[0] = true;
						ini[0].stファイル.BestRank[0] = this.nランク値[0];
					}


					// Clear and score ranks

					if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Tower)
                    {
                        // Regular (Ensou game) Score and Score Rank saves

                        #region [Regular saves]

                        CScoreIni.C演奏記録[] baseScores =
						{
							ini[0].stセクション[0],
							ini[1].stセクション[0]
						};

						for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                        {
							var ccf = TJAPlayer3.stage演奏ドラム画面.CChartScore[i];

							int diff = TJAPlayer3.stage選曲.n確定された曲の難易度[i];

							var clear = Math.Max(ini[i].stセクション[0].nクリア[diff], this.nクリア[i]);
							var scoreRank = Math.Max(ini[i].stセクション[0].nスコアランク[diff], this.nスコアランク[i]);
							var highscore = Math.Max(ini[i].stセクション[0].nハイスコア[diff], (int)TJAPlayer3.stage演奏ドラム画面.actScore.Get(E楽器パート.DRUMS, i));

							if (isAutoDisabled(i))
							{
								ini[i].stセクション[0].nクリア[diff] = clear;
								ini[i].stセクション[0].nスコアランク[diff] = scoreRank;
								ini[i].stセクション[0].nハイスコア[diff] = highscore;

								if (TJAPlayer3.ConfigIni.bScoreIniを出力する)
									ini[i].t書き出し(str[i]);
							}
						}

						#endregion

						#region [Legacy]
						/*

						this.st演奏記録[0].nクリア[TJAPlayer3.stage選曲.n確定された曲の難易度[0]] = Math.Max(ini[0].stセクション[0].nクリア[TJAPlayer3.stage選曲.n確定された曲の難易度[0]], this.nクリア);
						this.st演奏記録[0].nスコアランク[TJAPlayer3.stage選曲.n確定された曲の難易度[0]] = Math.Max(ini[0].stセクション[0].nスコアランク[TJAPlayer3.stage選曲.n確定された曲の難易度[0]], this.nスコアランク);

						for (int i = 0; i < 5; i++)
						{
							if (i != TJAPlayer3.stage選曲.n確定された曲の難易度[0])
							{
								this.st演奏記録[0].nクリア[i] = ini[0].stセクション[0].nクリア[i];
								this.st演奏記録[0].nスコアランク[i] = ini[0].stセクション[0].nスコアランク[i];
							}

							// ini.stセクション[0].nクリア[i] = this.st演奏記録[0].nクリア[i];
							// ini.stセクション[0].nスコアランク[i] = this.st演奏記録[0].nスコアランク[i];
						}

						*/
						#endregion
					}
					else if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan)
					{
						/* == Specific format for DaniDoujou charts ==
						**
						** Higher is better, takes the Clear0 spot (Usually the spot allocated for Kantan Clear crowns)
						**
						** 0 (Fugoukaku, no insign)
						** Silver Iki (Clear) : 1 (Red Goukaku) / 2 (Gold Goukaku)
						** Gold Iki (Full Combo) : 3 (Red Goukaku) / 4 (Gold Goukaku)
						** Rainbow Iki (Donda Full Combo) : 5 (Red Goukaku) / 6 (Gold Goukaku)
						**
						*/

						#region [Dan scores]

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

						if (isAutoDisabled(0))
						{
							ini[0].stセクション[0].nクリア[0] = Math.Max(ini[0].stセクション[0].nクリア[0], clearValue);
							ini[0].stセクション[0].nハイスコア[0] = Math.Max(ini[0].stセクション[0].nハイスコア[0], (int)TJAPlayer3.stage演奏ドラム画面.actScore.Get(E楽器パート.DRUMS, 0)); ;

							if (TJAPlayer3.ConfigIni.bScoreIniを出力する)
								ini[0].t書き出し(str[0]);
						}

						// this.st演奏記録[0].nクリア[0] = Math.Max(ini[0].stセクション[0].nクリア[0], clearValue);

						// Unlock dan grade
						if (clearValue > 0 && !TJAPlayer3.ConfigIni.b太鼓パートAutoPlay)
						{
							this.newGradeGranted = TJAPlayer3.NamePlateConfig.tUpdateDanTitle(TJAPlayer3.stage選曲.r確定された曲.strタイトル.Substring(0, 2),
								clearValue % 2 == 0,
								(clearValue - 1) / 2,
								TJAPlayer3.SaveFile);
						}

						#endregion

					}
					else if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
					{
						// Clear if top reached, then FC or DFC like any regular chart
						// Score Rank cointains highest reached floor

						#region [Tower scores]

						int tmpClear = GetTowerScoreRank();

						if (isAutoDisabled(0))
						{
							ini[0].stセクション[0].nクリア[0] = Math.Max(ini[0].stセクション[0].nクリア[0], tmpClear);
							ini[0].stセクション[0].nスコアランク[0] = Math.Max(ini[0].stセクション[0].nスコアランク[0], CFloorManagement.LastRegisteredFloor);
							ini[0].stセクション[0].nハイスコア[0] = Math.Max(ini[0].stセクション[0].nハイスコア[0], (int)TJAPlayer3.stage演奏ドラム画面.actScore.Get(E楽器パート.DRUMS, 0)); ;

							if (TJAPlayer3.ConfigIni.bScoreIniを出力する)
								ini[0].t書き出し(str[0]);
						}


						// this.st演奏記録[0].nクリア[0] = Math.Max(ini[0].stセクション[0].nクリア[0], tmpClear);
						// this.st演奏記録[0].nスコアランク[0] = Math.Max(ini[0].stセクション[0].nスコアランク[0], CFloorManagement.LastRegisteredFloor);

						#endregion

					}
					else
                    {
						#region [Legacy]


						
						/*

                        // 新記録スコアチェック
                        if ((this.st演奏記録[0].nスコア > ini[0].stセクション[0].nスコア) && !TJAPlayer3.ConfigIni.b太鼓パートAutoPlay)
						{
							this.b新記録スコア[0] = true;
							ini[0].stセクション[0] = this.st演奏記録[0];
						}

						// Header hi-score
						//if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Tower)
						//	if (this.st演奏記録[0].nスコア > ini[0].stセクション[0].nスコア)
						//		this.st演奏記録[0].nハイスコア[TJAPlayer3.stage選曲.n確定された曲の難易度[0]] = (int)st演奏記録[0].nスコア;


						// 新記録スキルチェック
						if (this.st演奏記録[0].db演奏型スキル値 > ini[0].stセクション[0].db演奏型スキル値)
						{
							this.b新記録スキル[0] = true;
							ini[0].stセクション[0] = this.st演奏記録[0];
						}


						// ラストプレイ #23595 2011.1.9 ikanick
						// オートじゃなければプレイ結果を書き込む
						if (TJAPlayer3.ConfigIni.b太鼓パートAutoPlay == false)
						{
							ini[0].stセクション[0] = this.st演奏記録[0];
						}

						// #23596 10.11.16 add ikanick オートじゃないならクリア回数を1増やす
						//        11.02.05 bオート to t更新条件を取得する use      ikanick
						bool[] b更新が必要か否か = new bool[3];
						CScoreIni.t更新条件を取得する(out b更新が必要か否か[0], out b更新が必要か否か[1], out b更新が必要か否か[2]);

						if (b更新が必要か否か[0])
						{
							ini[0].stファイル.ClearCountDrums++;
						}

						//---------------------------------------------------------------------/

						if (TJAPlayer3.ConfigIni.bScoreIniを出力する)
							ini[0].t書き出し(str[0]);

						*/

						#endregion



					}





                    //---------------------
                    #endregion

                    #region [ リザルト画面への演奏回数の更新 #24281 2011.1.30 yyagi]
                    if (TJAPlayer3.ConfigIni.bScoreIniを出力する)
					{
						this.n演奏回数.Drums = ini[0].stファイル.PlayCountDrums;
						this.n演奏回数.Guitar = ini[0].stファイル.PlayCountGuitar;
						this.n演奏回数.Bass = ini[0].stファイル.PlayCountBass;
					}
					#endregion
				}

				// Discord Presenseの更新
				Discord.UpdatePresence(TJAPlayer3.ConfigIni.SendDiscordPlayingInformation ? TJAPlayer3.stage選曲.r確定された曲.strタイトル
					+ Discord.DiffToString(TJAPlayer3.stage選曲.n確定された曲の難易度[0])
					: "",
					Properties.Discord.Stage_Result + (TJAPlayer3.ConfigIni.b太鼓パートAutoPlay == true ? " (" + Properties.Discord.Info_IsAuto + ")" : ""), 
					TJAPlayer3.StartupTime);


				#region [Earned medals]

				this.nEarnedMedalsCount[0] = 0;
				this.nEarnedMedalsCount[1] = 0;

				// Medals

				int nTotalHits = this.st演奏記録.Drums.nGreat数 + this.st演奏記録.Drums.nMiss数 + this.st演奏記録.Drums.nPerfect数;

				double dAccuracyRate = Math.Pow((50 * this.st演奏記録.Drums.nGreat数 + 100 * this.st演奏記録.Drums.nPerfect数) / (double)(100 * nTotalHits), 3);

				int diffModifier;
				float starRate;
				float redStarRate;

				float[] modMultipliers =
				{
					TJAPlayer3.stage選曲.actPlayOption.tGetModMultiplier(CActPlayOption.EBalancingType.COINS, false, 0),
					TJAPlayer3.stage選曲.actPlayOption.tGetModMultiplier(CActPlayOption.EBalancingType.COINS, false, 1)
				};

				if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
				{
					diffModifier = 3;

					int stars = TJAPlayer3.stage選曲.r確定された曲.arスコア[(int)Difficulty.Tower].譜面情報.nレベル[(int)Difficulty.Tower];

					starRate = Math.Min(10, stars) / 2;
					redStarRate = Math.Max(0, stars - 10) * 4;

					int maxFloors = TJAPlayer3.stage選曲.r確定された曲.arスコア[(int)Difficulty.Tower].譜面情報.nTotalFloor;

					double floorRate = Math.Pow(CFloorManagement.LastRegisteredFloor / (double)maxFloors, 2);
					double lengthBonus = Math.Max(1, maxFloors / 140.0);

					#region [Clear modifier]

					int clearModifier = 0;

					if (this.st演奏記録.Drums.nMiss数 == 0)
					{
						clearModifier = (int)(5 * lengthBonus);
						if (this.st演奏記録.Drums.nGreat数 == 0)
							clearModifier = (int)(12 * lengthBonus);
					}

					#endregion

					// this.nEarnedMedalsCount[0] = stars;
					this.nEarnedMedalsCount[0] = 5 + (int)((diffModifier * (starRate + redStarRate)) * (floorRate * lengthBonus)) + clearModifier;
					this.nEarnedMedalsCount[0] = Math.Max(5, (int)(this.nEarnedMedalsCount[0] * modMultipliers[0]));
				}
				else if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan)
				{
					int partialScore = 0;

					#region [Clear and Goukaku modifier]

					Exam.Status examStatus = TJAPlayer3.stage演奏ドラム画面.actDan.GetExamStatus(TJAPlayer3.stage結果.st演奏記録.Drums.Dan_C);

					int clearModifier = -1;
					int goukakuModifier = 0;

					if (examStatus != Exam.Status.Failure)
					{
						clearModifier = 0;
						if (this.st演奏記録.Drums.nMiss数 == 0)
						{
							clearModifier = 4;
							if (this.st演奏記録.Drums.nGreat数 == 0)
								clearModifier = 6;
						}

						if (examStatus == Exam.Status.Better_Success)
							goukakuModifier = 20;
					}

					#endregion

					#region [Partial scores]

					for (int i = 0; i < TJAPlayer3.stage選曲.r確定された曲.DanSongs.Count; i++)
                    {
						if (TJAPlayer3.stage選曲.r確定された曲.DanSongs[i] != null)
                        {
							int diff = TJAPlayer3.stage選曲.r確定された曲.DanSongs[i].Difficulty;
							int stars = TJAPlayer3.stage選曲.r確定された曲.DanSongs[i].Level;

							diffModifier = Math.Max(1, Math.Min(3, diff));

							starRate = Math.Min(10, stars) / 2;
							redStarRate = Math.Max(0, stars - 10) * 4;

							partialScore += (int)(diffModifier * (starRate + redStarRate));
						}

					}

					#endregion

					if (clearModifier < 0)
						this.nEarnedMedalsCount[0] = 10;
					else
					{
						this.nEarnedMedalsCount[0] = 10 + goukakuModifier + clearModifier + (int)(partialScore * dAccuracyRate);
						this.nEarnedMedalsCount[0] = Math.Max(10, (int)(this.nEarnedMedalsCount[0] * modMultipliers[0]));
					}
				}
				else
				{
					for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                    {
						int diff = TJAPlayer3.stage選曲.n確定された曲の難易度[i];
						int stars = TJAPlayer3.stage選曲.r確定された曲.arスコア[diff].譜面情報.nレベル[diff];

						diffModifier = Math.Max(1, Math.Min(3, diff));

						starRate = Math.Min(10, stars) / 2;
						redStarRate = Math.Max(0, stars - 10) * 4;

						#region [Clear modifier]

						int[] modifiers = { -1, 0, 2, 3 };

						int clearModifier = modifiers[0];

						if (TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[i] >= 80)
                        {
							clearModifier = modifiers[1] * diffModifier;
							if (TJAPlayer3.stage演奏ドラム画面.CChartScore[i].nMiss == 0)
                            {
								clearModifier = modifiers[2] * diffModifier;
								if (TJAPlayer3.stage演奏ドラム画面.CChartScore[i].nGood == 0)
									clearModifier = modifiers[3] * diffModifier;
							}
						}
							
						#endregion

						#region [Score rank modifier]

						int[] srModifiers = { 0, 0, 0, 0, 1, 1, 2, 3 };

						// int s = TJAPlayer3.stage演奏ドラム画面.ScoreRank.ScoreRank[1];

						int scoreRankModifier = srModifiers[0] * diffModifier;

						for (int j = 1; j < 8; j++)
                        {
							if (i == 0)
								if (TJAPlayer3.stage演奏ドラム画面.actScore.GetScore(i) >= TJAPlayer3.stage演奏ドラム画面.ScoreRank.ScoreRank[j - 1])
									scoreRankModifier = srModifiers[j] * diffModifier;
							else
								if (TJAPlayer3.stage演奏ドラム画面.actScore.GetScore(i) >= TJAPlayer3.stage演奏ドラム画面.ScoreRank.ScoreRank2P[j - 1])
									scoreRankModifier = srModifiers[j] * diffModifier;
						}

						#endregion

						nTotalHits = TJAPlayer3.stage演奏ドラム画面.CChartScore[i].nGood + TJAPlayer3.stage演奏ドラム画面.CChartScore[i].nMiss + TJAPlayer3.stage演奏ドラム画面.CChartScore[i].nGreat;

						dAccuracyRate = Math.Pow((50 * TJAPlayer3.stage演奏ドラム画面.CChartScore[i].nGood + 100 * TJAPlayer3.stage演奏ドラム画面.CChartScore[i].nGreat) / (double)(100 * nTotalHits), 3);

						if (clearModifier < 0)
							this.nEarnedMedalsCount[i] = 5;
						else
						{
							this.nEarnedMedalsCount[i] = 5 + (int)((diffModifier * (starRate + redStarRate)) * dAccuracyRate) + clearModifier + scoreRankModifier;
							this.nEarnedMedalsCount[i] = Math.Max(5, (int)(this.nEarnedMedalsCount[i] * modMultipliers[i]));
						}
					}
				}

				// ADLIB bonuses : 1 coin per ADLIB
				for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                {
					this.nEarnedMedalsCount[i] += TJAPlayer3.stage演奏ドラム画面.CChartScore[i].nADLIB;
                }

				if (TJAPlayer3.ConfigIni.b太鼓パートAutoPlay)
					this.nEarnedMedalsCount[0] = 0;
				if (TJAPlayer3.ConfigIni.b太鼓パートAutoPlay2P || TJAPlayer3.ConfigIni.nAILevel > 0)
					this.nEarnedMedalsCount[1] = 0;

				TJAPlayer3.NamePlateConfig.tEarnCoins(this.nEarnedMedalsCount);

                #endregion

                #region [Modals preprocessing]

                mqModals = new ModalQueue((TJAPlayer3.ConfigIni.nPlayerCount > 1) ? Modal.EModalFormat.Half : Modal.EModalFormat.Full);

				for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                {
					if (this.nEarnedMedalsCount[i] > 0)
						mqModals.tAddModal(
							new Modal(
								Modal.EModalType.Coin, 
								0,
								this.nEarnedMedalsCount[i]), 
							i);
                }

				displayedModals = new Modal[] { null, null };

				#endregion

				TJAPlayer3.stage選曲.act曲リスト.bFirstCrownLoad = false;

				this.ctPhase1 = null;
				this.ctPhase2 = null;
				this.ctPhase3 = null;
				examsShift = 0;

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

				ctShine_Plate = new CCounter(0, 1000, 1, TJAPlayer3.Timer);
				ctWork_Plate = new CCounter(0, 4000, 1, TJAPlayer3.Timer);

				if (TJAPlayer3.Tx.TowerResult_Background != null)
					ctTower_Animation = new CCounter(0, TJAPlayer3.Tx.TowerResult_Background.szテクスチャサイズ.Height - 720, 25, TJAPlayer3.Timer);
				else
					ctTower_Animation = new CCounter();

				Dan_Plate = TJAPlayer3.tテクスチャの生成(Path.GetDirectoryName(TJAPlayer3.DTX.strファイル名の絶対パス) + @"\Dan_Plate.png");

				if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
                {
					if (!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName))
					{
						this.pfTowerText = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 28);
						this.pfTowerText48 = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 48);
						this.pfTowerText72 = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 72);
					}
					else
					{
						this.pfTowerText = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 28);
						this.pfTowerText48 = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 48);
						this.pfTowerText72 = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 72);
					}

					this.ttkMaxFloors = new TitleTextureKey("/" + TJAPlayer3.stage選曲.r確定された曲.arスコア[5].譜面情報.nTotalFloor.ToString() + CLangManager.LangInstance.GetString(1001), pfTowerText48, Color.Black, Color.Transparent, 700);
					this.ttkToutatsu = new TitleTextureKey(CLangManager.LangInstance.GetString(1000), pfTowerText48, Color.White, Color.Black, 700);
					this.ttkTen = new TitleTextureKey(CLangManager.LangInstance.GetString(1002), pfTowerText, Color.Black, Color.Transparent, 700);
					this.ttkReachedFloor = new TitleTextureKey(CFloorManagement.LastRegisteredFloor.ToString(), pfTowerText72, Color.Orange, Color.Black, 700);
					this.ttkScore = new TitleTextureKey(CLangManager.LangInstance.GetString(1003), pfTowerText, Color.Black, Color.Transparent, 700);
					this.ttkRemaningLifes = new TitleTextureKey(CFloorManagement.CurrentNumberOfLives.ToString() + " / " + CFloorManagement.MaxNumberOfLives.ToString(), pfTowerText, Color.Black, Color.Transparent, 700);
					this.ttkScoreCount = new TitleTextureKey(TJAPlayer3.stage結果.st演奏記録.Drums.nスコア.ToString(), pfTowerText, Color.Black, Color.Transparent, 700);
				}
				else if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan) 
				{
					if (!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName))
					{
						this.pfDanTitles = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 24);
					}
					else
					{
						this.pfDanTitles = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 16);
					}

					this.ttkDanTitles = new TitleTextureKey[TJAPlayer3.stage選曲.r確定された曲.DanSongs.Count];

					for (int i = 0; i < TJAPlayer3.stage選曲.r確定された曲.DanSongs.Count; i++)
					{
						this.ttkDanTitles[i] = new TitleTextureKey(TJAPlayer3.stage選曲.r確定された曲.DanSongs[i].bTitleShow 
							? "???" 
							: TJAPlayer3.stage選曲.r確定された曲.DanSongs[i].Title, 
							pfDanTitles, 
							Color.White, 
							Color.Black, 
							700);
					}
				}

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

				Dan_Plate?.Dispose();

				if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
				{
					TJAPlayer3.t安全にDisposeする(ref pfTowerText);
					TJAPlayer3.t安全にDisposeする(ref pfTowerText48);
					TJAPlayer3.t安全にDisposeする(ref pfTowerText72);
				}
				else if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan)
				{
					TJAPlayer3.t安全にDisposeする(ref pfDanTitles);
				}

				base.OnManagedリソースの解放();
			}
		}
		public override int On進行描画()
		{
			if (!base.b活性化してない)
			{

				ctShine_Plate.t進行Loop();

				// int num;
				
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
						bool is1P = (TJAPlayer3.ConfigIni.nPlayerCount == 1);
						bool is2PSide = TJAPlayer3.P1IsBlue();
						int mountainTexId = (is2PSide && is1P) ? 2 : 0;

						int CloudType = 0;
						float MountainAppearValue = this.actParameterPanel.MountainAppearValue;

						//2000 + (this.actParameterPanel.ctゲージアニメ.n終了値 * 66) + 8360 - 85;
						int gaugeAnimFactors = 0;

						if (this.actParameterPanel.ct全体進行.n現在の値 >= MountainAppearValue && is1P)
						{
							#region [Mountain Bump (1P only)]

							if (this.st演奏記録.Drums.fゲージ >= 80.0)
							{
								//int gaugeAnimationFactor = (this.actParameterPanel.ct全体進行.n現在の値 - (10275 + ((int)this.actParameterPanel.ctゲージアニメ[0].n終了値 * 66))) * 3;

								gaugeAnimFactors = (this.actParameterPanel.ct全体進行.n現在の値 - (int)MountainAppearValue) * 3;

								TJAPlayer3.Tx.Result_Background[1].Opacity = gaugeAnimFactors;
								
								TJAPlayer3.Tx.Result_Mountain[mountainTexId + 1].Opacity = gaugeAnimFactors;
								TJAPlayer3.Tx.Result_Mountain[mountainTexId + 0].Opacity = 255 - gaugeAnimFactors;

								if (this.actParameterPanel.ctMountain_ClearIn.n現在の値 <= 90)
								{
									TJAPlayer3.Tx.Result_Mountain[mountainTexId + 1].vc拡大縮小倍率.Y = 1.0f - (float)Math.Sin((float)this.actParameterPanel.ctMountain_ClearIn.n現在の値 * (Math.PI / 180)) * 0.18f;
								}
								else if (this.actParameterPanel.ctMountain_ClearIn.n現在の値 <= 225)
								{
									TJAPlayer3.Tx.Result_Mountain[mountainTexId + 1].vc拡大縮小倍率.Y = 0.82f + (float)Math.Sin((float)(this.actParameterPanel.ctMountain_ClearIn.n現在の値 - 90) / 1.5f * (Math.PI / 180)) * 0.58f;
								}
								else if (this.actParameterPanel.ctMountain_ClearIn.n現在の値 <= 245)
								{
									TJAPlayer3.Tx.Result_Mountain[mountainTexId + 1].vc拡大縮小倍率.Y = 1.4f;
								}
								else if (this.actParameterPanel.ctMountain_ClearIn.n現在の値 <= 335)
								{
									TJAPlayer3.Tx.Result_Mountain[mountainTexId + 1].vc拡大縮小倍率.Y = 0.9f + (float)Math.Sin((float)(this.actParameterPanel.ctMountain_ClearIn.n現在の値 - 155) * (Math.PI / 180)) * 0.5f;
								}
								else if (this.actParameterPanel.ctMountain_ClearIn.n現在の値 <= 515)
								{
									TJAPlayer3.Tx.Result_Mountain[mountainTexId + 1].vc拡大縮小倍率.Y = 0.9f + (float)Math.Sin((float)(this.actParameterPanel.ctMountain_ClearIn.n現在の値 - 335) * (Math.PI / 180)) * 0.4f;
								}
							}

							#endregion
						}
						else if (is1P)
						{

							TJAPlayer3.Tx.Result_Background[1].Opacity = 0;
							TJAPlayer3.Tx.Result_Mountain[mountainTexId + 0].Opacity = 255;
							TJAPlayer3.Tx.Result_Mountain[mountainTexId + 1].Opacity = 0;
						}

						#region [Display background]

						if (is1P)
                        {
							if (is2PSide)
								TJAPlayer3.Tx.Result_Background[2].t2D描画(TJAPlayer3.app.Device, 0, 0);
							else
								TJAPlayer3.Tx.Result_Background[0].t2D描画(TJAPlayer3.app.Device, 0, 0);
							TJAPlayer3.Tx.Result_Background[1].t2D描画(TJAPlayer3.app.Device, 0, 0);
						}
						else
                        {
							gaugeAnimFactors = (this.actParameterPanel.ct全体進行.n現在の値 - (int)MountainAppearValue) * 3;

							for (int i = 0; i < 2; i++)
                            {
								TJAPlayer3.Tx.Result_Background[2 * i].t2D描画(TJAPlayer3.app.Device, 640 * i, 0, new Rectangle(640 * i, 0, 640, 720));
								if (TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[i] >= 80.0f)
                                {
									TJAPlayer3.Tx.Result_Background[1].Opacity = gaugeAnimFactors;
									TJAPlayer3.Tx.Result_Background[1].t2D描画(TJAPlayer3.app.Device, 640 * i, 0, new Rectangle(640 * i, 0, 640, 720));
								}
							}
						}

						#endregion

						if (is1P)
                        {
							TJAPlayer3.Tx.Result_Mountain[mountainTexId + 0].t2D描画(TJAPlayer3.app.Device, 0, 0);
							TJAPlayer3.Tx.Result_Mountain[mountainTexId + 1].t2D拡大率考慮下基準描画(TJAPlayer3.app.Device, 0, 720);

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

								int cloudOffset = (is2PSide) ? 720 : 0;

								TJAPlayer3.Tx.Result_Cloud.vc拡大縮小倍率.X = 0.65f;
								TJAPlayer3.Tx.Result_Cloud.vc拡大縮小倍率.Y = 0.65f;
								TJAPlayer3.Tx.Result_Cloud.Opacity = CloudType;

								TJAPlayer3.Tx.Result_Cloud.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, CloudXPos[i] - CurMoveGold, CloudYPos[i], new Rectangle(i * 1200, 360, 1200, 360));

								TJAPlayer3.Tx.Result_Cloud.Opacity = 255 - CloudType;

								TJAPlayer3.Tx.Result_Cloud.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, CloudXPos[i] - CurMoveRed, CloudYPos[i], new Rectangle(i * 1200, cloudOffset, 1200, 360));
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

									if (!is2PSide)
										TJAPlayer3.Tx.Result_Shine.t2D中心基準描画(TJAPlayer3.app.Device, ShinePXPos[i] + 80, ShinePYPos[i]);
									else
										TJAPlayer3.Tx.Result_Shine.t2D中心基準描画(TJAPlayer3.app.Device, 1280 - (ShinePXPos[i] + 80), ShinePYPos[i]);
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

										if (!is2PSide)
											TJAPlayer3.Tx.Result_Work[i].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, WorksPosX[i], WorksPosY[i]);
										else
											TJAPlayer3.Tx.Result_Work[i].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 1280 - WorksPosX[i], WorksPosY[i]);
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

										if (!is2PSide)
											TJAPlayer3.Tx.Result_Work[i].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, WorksPosX[i], WorksPosY[i]);
										else
											TJAPlayer3.Tx.Result_Work[i].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 1280 - WorksPosX[i], WorksPosY[i]);
									}
								}

								#endregion

							}
						}

						

					}

					if (this.ct登場用.b進行中 && (TJAPlayer3.Tx.Result_Header != null))
					{
						double num2 = ((double)this.ct登場用.n現在の値) / 100.0;
						double num3 = Math.Sin(Math.PI / 2 * num2);
						
						// num = ((int)(TJAPlayer3.Tx.Result_Header.sz画像サイズ.Height * num3)) - TJAPlayer3.Tx.Result_Header.sz画像サイズ.Height;
					}
					/*
					else
					{
						num = 0;
					}
					*/

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

						#region [Counter processings]

						int songCount = TJAPlayer3.stage選曲.r確定された曲.DanSongs.Count;

						/*
						**	1600 => Dan plate 
						**  3200 + 300 * count => Songs display
						**  5500 + 300 * count => Exams plate display
						**	8200 + 300 * count => Goukaku/Fugoukaku display => Step 2 (Prompt the user to tap enter and let them swaping between informations hitting kas)
						**  ??? => Success/Fail animation
						*/
						if (ctPhase1 == null)
                        {
							ctPhase1 = new CCounter(0, 8200 + songCount * 300, 0.5f, TJAPlayer3.Timer);
							ctPhase1.n現在の値 = 0;
						}
							
						ctPhase1.t進行();

						if (ctPhase2 != null)
							ctPhase2.t進行();

						#endregion


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

						int plateOffset = Math.Max(0, 1600 - ctPhase1.n現在の値) * 2;

						CActSelect段位リスト.tDisplayDanPlate(Dan_Plate,
							null,
							138,
							220 - plateOffset);

						#endregion

						#region [Charts Individual Results]

						for (int i = 0; i < songCount; i++)
                        {
							int songOffset = Math.Max(0, 3200 + 300 * i - ctPhase1.n現在の値);

							ftDanDisplaySongInfo(i, songOffset);
						}

						#endregion

						#region [Exam informations]

						int examsOffset = 0;

						if (ctPhase2 != null && examsShift != 0)
							examsOffset = (examsShift < 0) ? 1280 - ctPhase2.n現在の値 : ctPhase2.n現在の値;
						else if (ctPhase1.b終了値に達してない)
							examsOffset = Math.Max(0, 5500 + 300 * songCount - ctPhase1.n現在の値) * 2;

						ftDanDisplayExamInfo(examsOffset);

						#endregion

						#region [PassLogo]

						Exam.Status examStatus = TJAPlayer3.stage演奏ドラム画面.actDan.GetExamStatus(TJAPlayer3.stage結果.st演奏記録.Drums.Dan_C);

						int unitsBeforeAppearance = Math.Max(0, 8200 + 300 * songCount - ctPhase1.n現在の値);

						if (unitsBeforeAppearance <= 270)
                        {
							TJAPlayer3.Tx.DanResult_Rank.Opacity = 255;

							if (examStatus != Exam.Status.Failure)
							{
								#region [Goukaku]

								#region [ Appear animation ]

								if (unitsBeforeAppearance >= 90)
								{
									TJAPlayer3.Tx.DanResult_Rank.Opacity = (int)((270 - unitsBeforeAppearance) / 180.0f * 255.0f);
									TJAPlayer3.Tx.DanResult_Rank.vc拡大縮小倍率.X = 1.0f + (float)Math.Sin((360 - unitsBeforeAppearance) / 1.5f * (Math.PI / 180)) * 1.4f;
									TJAPlayer3.Tx.DanResult_Rank.vc拡大縮小倍率.Y = 1.0f + (float)Math.Sin((360 - unitsBeforeAppearance) / 1.5f * (Math.PI / 180)) * 1.4f;
								}
								else if (unitsBeforeAppearance > 0)
								{
									TJAPlayer3.Tx.Result_ScoreRankEffect.vc拡大縮小倍率.X = 0.5f + (float)Math.Sin((float)(90 - unitsBeforeAppearance) * (Math.PI / 180)) * 0.5f;
									TJAPlayer3.Tx.Result_ScoreRankEffect.vc拡大縮小倍率.Y = 0.5f + (float)Math.Sin((float)(90 - unitsBeforeAppearance) * (Math.PI / 180)) * 0.5f;
								}
								else
								{
									TJAPlayer3.Tx.DanResult_Rank.vc拡大縮小倍率.X = 1f;
									TJAPlayer3.Tx.DanResult_Rank.vc拡大縮小倍率.Y = 1f;
								}

								#endregion

								#region [ Goukaku plate type calculus ]

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

								#endregion

								TJAPlayer3.Tx.DanResult_Rank.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 130, 380, new Rectangle(334 * (2 * comboType + successType + 1), 0, 334, 334));

								#endregion
							}
							else
							{
								#region [Fugoukaku]

								#region [ Appear animation ]

								if (unitsBeforeAppearance >= 90)
								{
									TJAPlayer3.Tx.DanResult_Rank.Opacity = (int)((270 - unitsBeforeAppearance) / 180.0f * 255.0f);
								}

								TJAPlayer3.Tx.DanResult_Rank.vc拡大縮小倍率.X = 1f;
								TJAPlayer3.Tx.DanResult_Rank.vc拡大縮小倍率.Y = 1f;

								#endregion

								TJAPlayer3.Tx.DanResult_Rank.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 130, 380 - (unitsBeforeAppearance / 10f), new Rectangle(0, 0, 334, 334));

								#endregion
							}
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

                        #region [Tower background]

                        if (TJAPlayer3.Skin.Game_Tower_Ptn_Result > 0)
                        {
							int xFactor = 0;
							float yFactor = 1f;

							int currentTowerType = TJAPlayer3.stage選曲.r確定された曲.arスコア[5].譜面情報.nTowerType;

							if (currentTowerType < 0 || currentTowerType >= TJAPlayer3.Skin.Game_Tower_Ptn_Result)
								currentTowerType = 0;

							if (TJAPlayer3.Tx.TowerResult_Background != null && TJAPlayer3.Tx.TowerResult_Tower[currentTowerType] != null)
							{
								xFactor = (TJAPlayer3.Tx.TowerResult_Background.szテクスチャサイズ.Width - TJAPlayer3.Tx.TowerResult_Tower[currentTowerType].szテクスチャサイズ.Width) / 2;
								yFactor = TJAPlayer3.Tx.TowerResult_Tower[currentTowerType].szテクスチャサイズ.Height / (float)TJAPlayer3.Tx.TowerResult_Background.szテクスチャサイズ.Height;
							}

							TJAPlayer3.Tx.TowerResult_Background?.t2D描画(TJAPlayer3.app.Device, 0, -1 * this.ctTower_Animation.n現在の値);
							TJAPlayer3.Tx.TowerResult_Tower[currentTowerType]?.t2D描画(TJAPlayer3.app.Device, xFactor, -1 * yFactor * this.ctTower_Animation.n現在の値);
						}

						#endregion

						TJAPlayer3.Tx.TowerResult_Panel?.t2D描画(TJAPlayer3.app.Device, 0, 0);

						#region [Score Rank]

						int sc = GetTowerScoreRank() - 1;

						TJAPlayer3.act文字コンソール.tPrint(0, 40, C文字コンソール.Eフォント種別.白, sc.ToString());

						if (sc >= 0 && TJAPlayer3.Tx.TowerResult_ScoreRankEffect != null)
                        {
							TJAPlayer3.Tx.TowerResult_ScoreRankEffect.Opacity = 255;
							TJAPlayer3.Tx.TowerResult_ScoreRankEffect.vc拡大縮小倍率.X = 1f;
							TJAPlayer3.Tx.TowerResult_ScoreRankEffect.vc拡大縮小倍率.Y = 1f;
							TJAPlayer3.Tx.TowerResult_ScoreRankEffect.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device,
								1000,
								220,
								new Rectangle(sc * 229, 0, 229, 194));
						}
							

						#endregion


						#region [Text elements]

						int firstRowY = 394;
						int secondRowY = firstRowY + 96;

						TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(this.ttkToutatsu)?.t2D描画(TJAPlayer3.app.Device, 196, 160);
						TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(this.ttkMaxFloors)?.t2D描画(TJAPlayer3.app.Device, 616, 296);
						TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(this.ttkTen)?.t2D描画(TJAPlayer3.app.Device, 982, firstRowY);
						TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(this.ttkScore)?.t2D描画(TJAPlayer3.app.Device, 248, firstRowY);

						CTexture tmpScoreCount = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(this.ttkScoreCount);
						CTexture tmpCurrentFloor = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(this.ttkReachedFloor);
						CTexture tmpRemainingLifes = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(this.ttkRemaningLifes);

						tmpCurrentFloor?.t2D描画(TJAPlayer3.app.Device, 616 - tmpCurrentFloor.szテクスチャサイズ.Width + 72, 258);
						tmpScoreCount?.t2D描画(TJAPlayer3.app.Device, 1014 - tmpScoreCount.szテクスチャサイズ.Width + 12, firstRowY);
						tmpRemainingLifes?.t2D描画(TJAPlayer3.app.Device, 1014 - tmpRemainingLifes.szテクスチャサイズ.Width + 54, secondRowY);

						TJAPlayer3.Tx.Gauge_Soul?.t2D描画(TJAPlayer3.app.Device, 248, secondRowY - 16, new Rectangle(0, 0, 80, 80));

						#endregion

						if (!b音声再生 && !TJAPlayer3.Skin.bgmTowerResult.b再生中)
						{
							TJAPlayer3.Skin.bgmTowerResult.t再生する();
							b音声再生 = true;
						}


						#endregion
					}


                }

				// Display medals debug

				// TJAPlayer3.act文字コンソール.tPrint(0, 12, C文字コンソール.Eフォント種別.白, this.nEarnedMedalsCount[0].ToString());
				TJAPlayer3.act文字コンソール.tPrint(0, 25, C文字コンソール.Eフォント種別.白, this.nEarnedMedalsCount[1].ToString());



				if (this.actParameterPanel.On進行描画() == 0)
				{
					this.bアニメが完了 = false;
				}

				if (this.actSongBar.On進行描画() == 0)
				{
					this.bアニメが完了 = false;
				}

				#region Nameplate

				for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
				{
					int pos = i;
					if (TJAPlayer3.P1IsBlue() && TJAPlayer3.stage選曲.n確定された曲の難易度[0] < (int)Difficulty.Tower)
						pos = 1;

					TJAPlayer3.NamePlate.tNamePlateDraw((pos == 1) ? 1280 - 28 - TJAPlayer3.Tx.NamePlateBase.szテクスチャサイズ.Width : 28, 621, i);

					#region Mods

					ModIcons.tDisplayModsMenu((pos == 1) ? 1280 - 32 - TJAPlayer3.Tx.NamePlateBase.szテクスチャサイズ.Width : 32, 678, i);

					#endregion
				}

				#endregion




				#region [Display modals]

				// Display modal is present
				for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
				{
					if (displayedModals[i] != null)
						displayedModals[i].tDisplayModal();
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
							#region [ Return to song select screen (Faster method) ]

							TJAPlayer3.Skin.bgmリザルト音.t停止する();
							TJAPlayer3.Skin.bgmDanResult.t停止する();
							TJAPlayer3.Skin.bgmTowerResult.t停止する();
							TJAPlayer3.Skin.sound決定音.t再生する();
							actFI.tフェードアウト開始();
							
							if (TJAPlayer3.latestSongSelect == TJAPlayer3.stage選曲)// TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan)
								if (TJAPlayer3.stage選曲.r現在選択中の曲.r親ノード != null)
									TJAPlayer3.stage選曲.act曲リスト.tBOXを出る();

							t後処理();
							base.eフェーズID = CStage.Eフェーズ.共通_フェードアウト;
							this.eフェードアウト完了時の戻り値 = E戻り値.完了;

							#endregion
						}
						if (((TJAPlayer3.Pad.b押されたDGB(Eパッド.CY) 
							|| TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RD)) 
							|| (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LC) 
							|| (TJAPlayer3.Pad.b押されたDGB(Eパッド.LRed) 
							|| (TJAPlayer3.Pad.b押されたDGB(Eパッド.RRed) 
							|| TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.Return))))))
						{
							TJAPlayer3.Skin.sound決定音.t再生する();

                            #region [ Skip animations ]

                            if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] < (int)Difficulty.Tower
								&& this.actParameterPanel.ct全体進行.n現在の値 < this.actParameterPanel.MountainAppearValue)
                            {
								this.actParameterPanel.tSkipResultAnimations();
                            }
							else if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan
								&& (ctPhase1 != null && ctPhase1.b終了値に達してない))
                            {
								ctPhase1.n現在の値 = (int)ctPhase1.n終了値;
                            }

							#endregion

							else
							{
								if (!mqModals.tIsQueueEmpty(0) 
									&& (
										TJAPlayer3.Pad.b押されたDGB(Eパッド.LRed)
										|| TJAPlayer3.Pad.b押されたDGB(Eパッド.RRed)
										|| TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.Return)
										)
									)
								{
									displayedModals[0] = mqModals.tPopModal(0);
									displayedModals[0]?.tPlayModalSfx();
								}
								else if (TJAPlayer3.ConfigIni.nPlayerCount == 1 || mqModals.tIsQueueEmpty(1))
								{
									#region [ Return to song select screen ]

									actFI.tフェードアウト開始();

									if (TJAPlayer3.latestSongSelect == TJAPlayer3.stage選曲)
										if (TJAPlayer3.stage選曲.r現在選択中の曲.r親ノード != null)
											TJAPlayer3.stage選曲.act曲リスト.tBOXを出る();

									t後処理();

									{
										base.eフェーズID = CStage.Eフェーズ.共通_フェードアウト;
										this.eフェードアウト完了時の戻り値 = E戻り値.完了;
										TJAPlayer3.Skin.bgmリザルト音.t停止する();
										TJAPlayer3.Skin.bgmDanResult.t停止する();
										TJAPlayer3.Skin.bgmTowerResult.t停止する();
									}

									#endregion
								}
							}
						}
						else if ((TJAPlayer3.ConfigIni.nPlayerCount > 1 && (
								TJAPlayer3.Pad.b押されたDGB(Eパッド.LRed2P)
								|| TJAPlayer3.Pad.b押されたDGB(Eパッド.RRed2P)
							))) {
							if (!mqModals.tIsQueueEmpty(1) && this.actParameterPanel.ct全体進行.n現在の値 >= this.actParameterPanel.MountainAppearValue)
							{
								TJAPlayer3.Skin.sound決定音.t再生する();

								displayedModals[1] = mqModals.tPopModal(1);
								displayedModals[1]?.tPlayModalSfx();
							}
						}


						if (TJAPlayer3.Input管理.Keyboard.bキーが押されている((int)SlimDXKeys.Key.LeftArrow) ||
								TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LBlue) ||
							TJAPlayer3.Input管理.Keyboard.bキーが押されている((int)SlimDXKeys.Key.RightArrow) ||
								TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RBlue))
						{
							if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan)
                            {
								#region [ Phase 2 (Swap freely between Exams and Songs) ]

								if (ctPhase1 != null && ctPhase1.b終了値に達した && (ctPhase2 == null || ctPhase2.b終了値に達した))
                                {
									ctPhase2 = new CCounter(0, 1280, 0.5f, TJAPlayer3.Timer);
									ctPhase2.n現在の値 = 0;

									if (examsShift == 0)
										examsShift = 1;
									else
										examsShift = -examsShift;

									TJAPlayer3.Skin.sound変更音.t再生する();
								}

								#endregion
							}
						}
					}
				}
			}
			return 0;
		}

		#region [Dan result exam information]

		private void ftDanDisplayExamInfo(int offset = 0)
        {
			int baseX = offset;
			int baseY = -4;

			TJAPlayer3.Tx.DanResult_StatePanel_Base.t2D描画(TJAPlayer3.app.Device, baseX, baseY);
			TJAPlayer3.Tx.DanResult_StatePanel_Main.t2D描画(TJAPlayer3.app.Device, baseX, baseY);

			#region [ Global scores ]

			int smoothBaseX = baseX;
			int smoothBaseY = 0;

			int totalHit = TJAPlayer3.stage演奏ドラム画面.CChartScore[0].nGreat
				+ TJAPlayer3.stage演奏ドラム画面.CChartScore[0].nGood
				+ TJAPlayer3.stage演奏ドラム画面.GetRoll(0);

			string[] scoresArr = 
			{
				TJAPlayer3.stage演奏ドラム画面.actScore.Get(E楽器パート.DRUMS, 0).ToString(),
				TJAPlayer3.stage演奏ドラム画面.CChartScore[0].nGreat.ToString(),
				TJAPlayer3.stage演奏ドラム画面.CChartScore[0].nGood.ToString(),
				TJAPlayer3.stage演奏ドラム画面.CChartScore[0].nMiss.ToString(),
				TJAPlayer3.stage演奏ドラム画面.GetRoll(0).ToString(),
				TJAPlayer3.stage演奏ドラム画面.actCombo.n現在のコンボ数.最高値[0].ToString(),
				totalHit.ToString()
			};

			var totalZahyou = new Point[]
			{
				new Point(smoothBaseX + 584, smoothBaseY + 124),
				new Point(smoothBaseX + 842, smoothBaseY + 106),
				new Point(smoothBaseX + 842, smoothBaseY + 148),
				new Point(smoothBaseX + 842, smoothBaseY + 190),
				new Point(smoothBaseX + 1144, smoothBaseY + 106),
				new Point(smoothBaseX + 1144, smoothBaseY + 148),
				new Point(smoothBaseX + 1144, smoothBaseY + 190),
			};

			// Small digits
			for (int i = 1; i < 7; i++)
            {
				this.actParameterPanel.t小文字表示(totalZahyou[i].X - 122, totalZahyou[i].Y - 11, string.Format("{0,5:####0}", scoresArr[i]));
			}

			// Large digits
			this.actParameterPanel.tスコア文字表示(totalZahyou[0].X - 18, totalZahyou[0].Y - 5, string.Format("{0,7:######0}", scoresArr[0]));

			#endregion

			#region [ Display exams ]

			TJAPlayer3.stage演奏ドラム画面.actDan.DrawExam(TJAPlayer3.stage結果.st演奏記録.Drums.Dan_C, true, offset);

			#endregion
		}

        #endregion


        #region [Dan result individual song information]

        private void ftDanDisplaySongInfo(int i, int offset = 0)
        {
			int baseX = 255 + offset;
			int baseY = 100 + 183 * i;

			var song = TJAPlayer3.stage選曲.r確定された曲.DanSongs[i];

			// TJAPlayer3.Tx.Dani_Difficulty_Cymbol.t2D中心基準描画(TJAPlayer3.app.Device, scroll + 377, 180 + i * 73, new Rectangle(song.Difficulty * 53, 0, 53, 53));

			TJAPlayer3.Tx.DanResult_SongPanel_Main.t2D描画(TJAPlayer3.app.Device, baseX, baseY, new Rectangle(0, 1 + 170 * Math.Min(i, 2), 960, 170));

			TJAPlayer3.Tx.Dani_Difficulty_Cymbol.t2D中心基準描画(TJAPlayer3.app.Device, baseX + 122, baseY + 46, new Rectangle(song.Difficulty * 53, 0, 53, 53));

			TJAPlayer3.stage段位選択.段位リスト.tLevelNumberDraw(baseX + 128, baseY + 73, song.Level.ToString());

			string[] scoresArr =
			{
				TJAPlayer3.stage演奏ドラム画面.n良[i].ToString(),
				TJAPlayer3.stage演奏ドラム画面.n可[i].ToString(),
				TJAPlayer3.stage演奏ドラム画面.n不可[i].ToString(),
				TJAPlayer3.stage演奏ドラム画面.n連打[i].ToString()
			};

			for (int j = 0; j < 4; j++)
				this.actParameterPanel.t小文字表示(baseX + 200 + 211 * j, baseY + 104, string.Format("{0,4:###0}", scoresArr[j]));

			TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(this.ttkDanTitles[i]).t2D描画(TJAPlayer3.app.Device, baseX + 146, baseY + 39);

		}

		#endregion


		public void t後処理()
        {

			if (!b最近遊んだ曲追加済み)
			{
				#region [ Apply new local status for song select screens ]
				//---------------------
				if (!TJAPlayer3.bコンパクトモード)
				{
					if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Tower)
					{
                        #region [Update status]

                        Cスコア cScore = TJAPlayer3.stage選曲.r確定されたスコア;

						for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                        {
							if ((i == 0 && TJAPlayer3.ConfigIni.b太鼓パートAutoPlay)
								|| (i == 1 && (TJAPlayer3.ConfigIni.b太鼓パートAutoPlay2P || TJAPlayer3.ConfigIni.nAILevel > 0)))
								continue;

							int actualPlayer = TJAPlayer3.GetActualPlayer(i);

							if (cScore.GPInfo[actualPlayer].nClear[TJAPlayer3.stage選曲.n確定された曲の難易度[i]] < nクリア[i])
								cScore.GPInfo[actualPlayer].nClear[TJAPlayer3.stage選曲.n確定された曲の難易度[i]] = nクリア[i];

							if (cScore.GPInfo[actualPlayer].nScoreRank[TJAPlayer3.stage選曲.n確定された曲の難易度[i]] < nスコアランク[i])
								cScore.GPInfo[actualPlayer].nScoreRank[TJAPlayer3.stage選曲.n確定された曲の難易度[i]] = nスコアランク[i];

							if (cScore.GPInfo[actualPlayer].nHighScore[TJAPlayer3.stage選曲.n確定された曲の難易度[i]] < (int)TJAPlayer3.stage演奏ドラム画面.actScore.Get(E楽器パート.DRUMS, i))
								cScore.GPInfo[actualPlayer].nHighScore[TJAPlayer3.stage選曲.n確定された曲の難易度[i]] = (int)TJAPlayer3.stage演奏ドラム画面.actScore.Get(E楽器パート.DRUMS, i);
						}

						#endregion

					}
					else if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan)
                    {
                        #region [Dan update status]

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

						int actualPlayer = TJAPlayer3.SaveFile;

						if (!TJAPlayer3.ConfigIni.b太鼓パートAutoPlay)
                        {
							cスコア.GPInfo[actualPlayer].nClear[0] = Math.Max(cスコア.GPInfo[actualPlayer].nClear[0], clearValue);

							if (cスコア.GPInfo[actualPlayer].nHighScore[0] < (int)TJAPlayer3.stage演奏ドラム画面.actScore.Get(E楽器パート.DRUMS, 0))
								cスコア.GPInfo[actualPlayer].nHighScore[0] = (int)TJAPlayer3.stage演奏ドラム画面.actScore.Get(E楽器パート.DRUMS, 0);
						}

						#endregion

						//cスコア.譜面情報.nクリア[0] = Math.Max(cスコア.譜面情報.nクリア[0], clearValue);
					}
					else if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
					{
                        #region [Update Tower status]

                        Cスコア cスコア = TJAPlayer3.stage選曲.r確定されたスコア;
						int actualPlayer = TJAPlayer3.SaveFile;

						int tmpClear = GetTowerScoreRank();

						if (!TJAPlayer3.ConfigIni.b太鼓パートAutoPlay)
						{
							cスコア.GPInfo[actualPlayer].nClear[0] = Math.Max(cスコア.GPInfo[actualPlayer].nClear[0], tmpClear);
							cスコア.GPInfo[actualPlayer].nScoreRank[0] = Math.Max(cスコア.GPInfo[actualPlayer].nScoreRank[0], CFloorManagement.LastRegisteredFloor);

							if (cスコア.GPInfo[actualPlayer].nHighScore[0] < (int)TJAPlayer3.stage演奏ドラム画面.actScore.Get(E楽器パート.DRUMS, 0))
								cスコア.GPInfo[actualPlayer].nHighScore[0] = (int)TJAPlayer3.stage演奏ドラム画面.actScore.Get(E楽器パート.DRUMS, 0);
						}

						#endregion
					}
				}
				//---------------------
				#endregion

				// Song added to recently added songs here

				TJAPlayer3.RecentlyPlayedSongs.tAddChart(TJAPlayer3.stage選曲.r確定された曲.uniqueId.data.id);

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
		private TitleTextureKey[] ttkDanTitles;
		private CPrivateFastFont pfDanTitles;
		private CCounter ctPhase1; // Info display
		private CCounter ctPhase2; // Free swipe
		private CCounter ctPhase3; // Background & grade granted if changes
		private int examsShift = 0;
		private bool newGradeGranted = false;

		// Tower informations
		private CCounter ctTower_Animation;
		private TitleTextureKey ttkMaxFloors;
		private TitleTextureKey ttkToutatsu;
		private TitleTextureKey ttkTen;
		private TitleTextureKey ttkReachedFloor;
		private TitleTextureKey ttkScore;
		private TitleTextureKey ttkRemaningLifes;
		private TitleTextureKey ttkScoreCount;
		private CPrivateFastFont pfTowerText;
		private CPrivateFastFont pfTowerText48;
		private CPrivateFastFont pfTowerText72;

		// Modal queues
		private ModalQueue mqModals;
		private Modal[] displayedModals;

		// Coins information 
		private int[] nEarnedMedalsCount = { 0, 0 };

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
