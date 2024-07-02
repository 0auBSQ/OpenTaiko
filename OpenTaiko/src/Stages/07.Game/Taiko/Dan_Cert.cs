using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using FDK;
using System.IO;
using System.Linq;
using static TJAPlayer3.CActSelect曲リスト;

using Rectangle = System.Drawing.Rectangle;
using RectangleF = System.Drawing.RectangleF;
using Color = System.Drawing.Color;

namespace TJAPlayer3
{
    static internal class CExamInfo
    {
        // Includes the gauge exam, DanCert max number of exams is 6
        public static readonly int cMaxExam = 7;

        // Max number of songs for a Dan chart
        public static readonly int cExamMaxSongs = 9;
    }


    internal class Dan_Cert : CActivity
    {
        /// <summary>
        /// 段位認定
        /// </summary>
        public Dan_Cert()
        {
            base.IsDeActivated = true;
        }

        //
        Dan_C[] Challenge = new Dan_C[CExamInfo.cMaxExam];
        //

        public void Start(int number)
        {
            NowShowingNumber = number;
            if (number == 0)
            {
                Counter_Wait = new CCounter(0, 2299, 1, TJAPlayer3.Timer);
            }
            else
            {
                Counter_In = new CCounter(0, 999, 1, TJAPlayer3.Timer);
            }
            bExamChangeCheck = false;

            if (number == 0)
            {
                for (int i = 1; i < CExamInfo.cMaxExam; i++)
                    ExamChange[i] = false;

                for (int j = 0; j < CExamInfo.cMaxExam; j++)
                {
                    if (TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[0].Dan_C[j] != null)
                    {
                        Challenge[j] = TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[NowShowingNumber].Dan_C[j];
                        if (TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs.Count - 1].Dan_C[j] != null
                            && TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs.Count > 1) // Individual exams, not counted if dan is only a single song
                        {
                            if (TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[NowShowingNumber].Dan_C[j].GetExamRange() == Exam.Range.Less)
                            {
                                TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[NowShowingNumber].Dan_C[j].Amount = TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[NowShowingNumber].Dan_C[j].Value[0];
                            }
                            else
                            {
                                TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[NowShowingNumber].Dan_C[j].Amount = 0;
                            }

                            ExamChange[j] = true;
                        }
                    }
                }
            }

            ScreenPoint = new double[] { TJAPlayer3.Skin.Game_Lane_X[0] - TJAPlayer3.Tx.DanC_Screen.szTextureSize.Width / 2, TJAPlayer3.Skin.Resolution[0] };

            TJAPlayer3.stage演奏ドラム画面.ReSetScore(TJAPlayer3.DTX.List_DanSongs[NowShowingNumber].ScoreInit, TJAPlayer3.DTX.List_DanSongs[NowShowingNumber].ScoreDiff);

            TJAPlayer3.stage演奏ドラム画面.ftDanReSetScoreNiji(TJAPlayer3.DTX.nDan_NotesCount[NowShowingNumber], TJAPlayer3.DTX.nDan_BalloonCount[NowShowingNumber]);
            TJAPlayer3.stage演奏ドラム画面.ftDanReSetBranches(TJAPlayer3.DTX.bHasBranchDan[NowShowingNumber]);

            IsAnimating = true;

            //段位道場
            //TJAPlayer3.stage演奏ドラム画面.actPanel.SetPanelString(TJAPlayer3.DTX.List_DanSongs[NowShowingNumber].Title, TJAPlayer3.DTX.List_DanSongs[NowShowingNumber].Genre, 1 + NowShowingNumber + "曲目");
            TJAPlayer3.stage演奏ドラム画面.actPanel.SetPanelString(TJAPlayer3.DTX.List_DanSongs[NowShowingNumber].Title, 
                CLangManager.LangInstance.GetString("TITLE_MODE_DAN"), 
                1 + NowShowingNumber + "曲目");
            
            if (number == 0) Sound_Section_First?.PlayStart();
            else Sound_Section?.PlayStart();
        }

        public override void Activate()
        {
            for (int i = 0; i < CExamInfo.cMaxExam; i++)
            {
                if(TJAPlayer3.DTX.Dan_C[i] != null) Challenge[i] = new Dan_C(TJAPlayer3.DTX.Dan_C[i]);

                for (int j = 0; j < TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs.Count; j++)
                {
                    if (TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[j].Dan_C[i] != null)
                    {
                        TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[j].Dan_C[i] = new Dan_C(TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[j].Dan_C[i]);
                    }
                }
            }

            if(TJAPlayer3.stage演奏ドラム画面.ListDan_Number >= 1 && FirstSectionAnime)
                TJAPlayer3.stage演奏ドラム画面.ListDan_Number = 0;

            FirstSectionAnime = false;
            // 始点を決定する。
            // ExamCount = 0;

            songsnotesremain = new int[TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs.Count];
            this.ct虹アニメ = new CCounter(0, TJAPlayer3.Skin.Game_Gauge_Dan_Rainbow_Ptn - 1, 30, TJAPlayer3.Timer);
            this.ct虹透明度 = new CCounter(0, TJAPlayer3.Skin.Game_Gauge_Rainbow_Timer - 1, 1, TJAPlayer3.Timer);

            this.pfExamFont = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.Game_DanC_ExamFont_Size);

            this.ttkExams = new TitleTextureKey[(int)Exam.Type.Total];
            for (int i = 0; i < this.ttkExams.Length; i++)
            {
                this.ttkExams[i] = new TitleTextureKey(CLangManager.LangInstance.GetExamName(i), this.pfExamFont, Color.White, Color.SaddleBrown, 1000);
            }

            NowCymbolShowingNumber = 0;
            bExamChangeCheck = false;

            for (int i = 0; i < CExamInfo.cMaxExam; i++)
            {
                Status[i] = new ChallengeStatus();
                Status[i].Timer_Amount = new CCounter();
                Status[i].Timer_Gauge = new CCounter();
                Status[i].Timer_Failed = new CCounter();
            }
            
            IsEnded = new bool[TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs.Count];

            if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) IsAnimating = true;
            
            Dan_Plate = TJAPlayer3.tテクスチャの生成(Path.GetDirectoryName(TJAPlayer3.DTX.strファイル名の絶対パス) + @$"{Path.DirectorySeparatorChar}Dan_Plate.png");
            
            base.Activate();
        }

        public void Update()
        {
            for (int i = 0; i < CExamInfo.cMaxExam; i++)
            {
                if (Challenge[i] == null || !Challenge[i].GetEnable()) continue;
                if (ExamChange[i] && Challenge[i] != TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[NowShowingNumber].Dan_C[i]) continue;

                var oldReached = Challenge[i].GetReached();
                var isChangedAmount = false;

                int totalGoods = (int)TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含む.Drums.Perfect + TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含まない.Drums.Perfect;
                int totalOks = (int)TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含む.Drums.Great + TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含まない.Drums.Great;
                int totalBads = (int)TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含まない.Drums.Miss;
                int totalCombo = (int)TJAPlayer3.stage演奏ドラム画面.actCombo.n現在のコンボ数.最高値[0];

                int individualGoods = TJAPlayer3.stage演奏ドラム画面.n良[NowShowingNumber];
                int individualOks = TJAPlayer3.stage演奏ドラム画面.n可[NowShowingNumber];
                int individualBads = TJAPlayer3.stage演奏ドラム画面.n不可[NowShowingNumber];
                int individualCombo = TJAPlayer3.stage演奏ドラム画面.nHighestCombo[NowShowingNumber];

                int totalADLIBs = TJAPlayer3.stage演奏ドラム画面.CChartScore[0].nADLIB;
                int totalMines = TJAPlayer3.stage演奏ドラム画面.CChartScore[0].nMine;

                int individualADLIBs = TJAPlayer3.stage演奏ドラム画面.nADLIB[NowShowingNumber];
                int individualMines = TJAPlayer3.stage演奏ドラム画面.nMine[NowShowingNumber];

                double accuracy = (totalGoods * 100 + totalOks * 50) / (double)(totalGoods + totalOks + totalBads);
                double individualAccuracy = (individualGoods * 100 + individualOks * 50) / (double)(individualGoods + individualOks + individualBads);

                switch (Challenge[i].GetExamType())
                {
                    case Exam.Type.Gauge:
                        isChangedAmount = Challenge[i].Update((int)TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[0]);
                        break;
                    case Exam.Type.JudgePerfect:
                        isChangedAmount = Challenge[i].Update(ExamChange[i] ? individualGoods : totalGoods);
                        break;
                    case Exam.Type.JudgeGood:
                        isChangedAmount = Challenge[i].Update(ExamChange[i] ? individualOks : totalOks);
                        break;
                    case Exam.Type.JudgeBad:
                        isChangedAmount = Challenge[i].Update(ExamChange[i] ? individualBads : totalBads);
                        break;
                    case Exam.Type.JudgeADLIB:
                        isChangedAmount = Challenge[i].Update(ExamChange[i] ? individualADLIBs : totalADLIBs);
                        break;
                    case Exam.Type.JudgeMine:
                        isChangedAmount = Challenge[i].Update(ExamChange[i] ? individualMines : totalMines);
                        break;
                    case Exam.Type.Score:
                        isChangedAmount = Challenge[i].Update((int)TJAPlayer3.stage演奏ドラム画面.actScore.GetScore(0));
                        break;
                    case Exam.Type.Roll:
                        isChangedAmount = Challenge[i].Update(ExamChange[i] ? TJAPlayer3.stage演奏ドラム画面.n連打[NowShowingNumber] : (int)(TJAPlayer3.stage演奏ドラム画面.GetRoll(0)));
                        break;
                    case Exam.Type.Hit:
                        isChangedAmount = Challenge[i].Update(ExamChange[i] ? TJAPlayer3.stage演奏ドラム画面.n良[NowShowingNumber] + TJAPlayer3.stage演奏ドラム画面.n可[NowShowingNumber] + TJAPlayer3.stage演奏ドラム画面.n連打[NowShowingNumber] : (int)(TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含む.Drums.Perfect + TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含まない.Drums.Perfect + TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含む.Drums.Great + TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含まない.Drums.Great + TJAPlayer3.stage演奏ドラム画面.GetRoll(0)));
                        break;
                    case Exam.Type.Combo:
                        isChangedAmount = Challenge[i].Update(ExamChange[i] ? individualCombo : totalCombo);
                        break;
                    case Exam.Type.Accuracy:
                        isChangedAmount = Challenge[i].Update(ExamChange[i] ? (int)individualAccuracy : (int)accuracy);
                        break;
                    default:
                        break;
                }

                // 値が変更されていたらアニメーションを行う。
                if (isChangedAmount)
                {
                    if(Status[i].Timer_Amount != null && Status[i].Timer_Amount.IsUnEnded)
                    {
                        Status[i].Timer_Amount = new CCounter(0, 11, 12, TJAPlayer3.Timer);
                        Status[i].Timer_Amount.CurrentValue = 1;
                    }
                    else
                    {
                        Status[i].Timer_Amount = new CCounter(0, 11, 12, TJAPlayer3.Timer);
                    }
                }

                // 条件の達成見込みがあるかどうか判断する。
                if (Challenge[i].GetExamRange() == Exam.Range.Less)
                {
                    Challenge[i].SetReached(!Challenge[i].IsCleared[0]);
                }
                else
                {
                    songsnotesremain[NowShowingNumber] = TJAPlayer3.DTX.nDan_NotesCount[NowShowingNumber] 
                        - (TJAPlayer3.stage演奏ドラム画面.n良[NowShowingNumber] 
                            + TJAPlayer3.stage演奏ドラム画面.n可[NowShowingNumber] 
                            + TJAPlayer3.stage演奏ドラム画面.n不可[NowShowingNumber]);

                    /*
                    notesremain = TJAPlayer3.DTX.nノーツ数[3] 
                        - (TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含む.Drums.Perfect 
                            + TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含まない.Drums.Perfect) 
                        - (TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含む.Drums.Great 
                            + TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含まない.Drums.Great) 
                        - (TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含む.Drums.Miss 
                            + TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含まない.Drums.Miss);
                    */

                    notesremain = TJAPlayer3.DTX.nノーツ数[3]
                        - (TJAPlayer3.stage演奏ドラム画面.CChartScore[0].nGood
                            + TJAPlayer3.stage演奏ドラム画面.CChartScore[0].nGreat
                            + TJAPlayer3.stage演奏ドラム画面.CChartScore[0].nMiss);

                    // 残り音符数が0になったときに判断されるやつ

                    // Challenges that are judged when there are no remaining notes
                    if (ExamChange[i] ? songsnotesremain[NowShowingNumber] <= 0 : notesremain <= 0)
                    {
                        // 残り音符数ゼロ
                        switch (Challenge[i].GetExamType())
                        {
                            case Exam.Type.Gauge:
                                if (Challenge[i].Amount < Challenge[i].Value[0]) Challenge[i].SetReached(true);
                                break;
                            case Exam.Type.Accuracy:
                                if (Challenge[i].Amount < Challenge[i].Value[0]) Challenge[i].SetReached(true);
                                break;
                            default:
                                // 何もしない
                                break;
                        }
                    }
                    
                    // Challenges that are monitored in live
                    switch (Challenge[i].GetExamType())
                    {
                        case Exam.Type.JudgePerfect:
                        case Exam.Type.JudgeGood:
                        case Exam.Type.JudgeBad:
                            if (ExamChange[i] 
                                ? songsnotesremain[NowShowingNumber] < (Challenge[i].Value[0] - Challenge[i].Amount) 
                                : notesremain < (Challenge[i].Value[0] - Challenge[i].Amount)) Challenge[i].SetReached(true);
                            break;
                        case Exam.Type.Combo:
                            if (notesremain + TJAPlayer3.stage演奏ドラム画面.actCombo.n現在のコンボ数.P1 < ((Challenge[i].Value[0])) 
                                && TJAPlayer3.stage演奏ドラム画面.actCombo.n現在のコンボ数.最高値[0] < (Challenge[i].Value[0])) Challenge[i].SetReached(true);
                            break;
                        default:
                            break;
                    }

                    // 音源が終了したやつの分岐。
                    // ( CDTXMania.DTX.listChip.Count > 0 ) ? CDTXMania.DTX.listChip[ CDTXMania.DTX.listChip.Count - 1 ].n発声時刻ms : 0;

                    // Check challenge fails at the end of each songs

                    if (TJAPlayer3.DTX.listChip.Count > 0)
                    {
                        if (ExamChange[i]
                            ? TJAPlayer3.DTX.pDan_LastChip[NowShowingNumber].n発声時刻ms <= SoundManager.PlayTimer.NowTime//TJAPlayer3.Timer.n現在時刻 
                            : TJAPlayer3.DTX.listChip[TJAPlayer3.DTX.listChip.Count - 1].n発声時刻ms <= SoundManager.PlayTimer.NowTime)//TJAPlayer3.Timer.n現在時刻)
                        {
                            switch (Challenge[i].GetExamType())
                            {
                                case Exam.Type.Score:
                                case Exam.Type.Hit:
                                // Should be checked in live "If no remaining roll"
                                case Exam.Type.Roll:
                                // Should be checked in live "If no remaining ADLIB/Mine"
                                case Exam.Type.JudgeADLIB:
                                case Exam.Type.JudgeMine:
                                    if (Challenge[i].Amount < Challenge[i].Value[0]) Challenge[i].SetReached(true);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    /*
                    if (!IsEnded[NowShowingNumber])
                    {
                        if (TJAPlayer3.DTX.listChip.Count <= 0) continue;
                        if (ExamChange[i] 
                            ? TJAPlayer3.DTX.pDan_LastChip[NowShowingNumber].n発声時刻ms <= CSound管理.rc演奏用タイマ.n現在時刻//TJAPlayer3.Timer.n現在時刻 
                            : TJAPlayer3.DTX.listChip[TJAPlayer3.DTX.listChip.Count - 1].n発声時刻ms <= CSound管理.rc演奏用タイマ.n現在時刻)//TJAPlayer3.Timer.n現在時刻)
                        {
                            IsEnded[NowShowingNumber] = true;
                        }
                    }
                    */
                }
                if (oldReached == false && Challenge[i].GetReached() == true)
                {
                    Sound_Failed?.PlayStart();
                }
            }
        }

        public override void DeActivate()
        {
            for (int i = 0; i < CExamInfo.cMaxExam; i++)
            {
                Challenge[i] = null;
            }

            for (int i = 0; i < CExamInfo.cMaxExam; i++)
            {
                Status[i].Timer_Amount = null;
                Status[i].Timer_Gauge = null;
                Status[i].Timer_Failed = null;
            }
            for(int i = 0; i < IsEnded.Length; i++)
                IsEnded[i] = false;

            TJAPlayer3.tDisposeSafely(ref this.pfExamFont);
            
            Dan_Plate?.Dispose();

            base.DeActivate();
        }

        public override void CreateManagedResource()
        {
            Sound_Section = TJAPlayer3.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Section.ogg"), ESoundGroup.SoundEffect);
            Sound_Section_First = TJAPlayer3.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Section_First.wav"), ESoundGroup.SoundEffect);
            Sound_Failed = TJAPlayer3.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Failed.ogg"), ESoundGroup.SoundEffect);
            base.CreateManagedResource();
        }

        public override void ReleaseManagedResource()
        {
            Sound_Section_First?.Dispose();
            Sound_Section?.tDispose();
            Sound_Failed?.tDispose();
            base.ReleaseManagedResource();
        }

        public override int Draw()
        {
            if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] != (int)Difficulty.Dan) return base.Draw();
            Counter_In?.Tick();
            Counter_Wait?.Tick();
            Counter_Out?.Tick();
            Counter_Text?.Tick();

            if (Counter_Text != null)
            {
                if (Counter_Text.CurrentValue >= 2000)
                {
                    for (int i = Counter_Text_Old; i < Counter_Text.CurrentValue; i++)
                    {
                        if (i % 2 == 0)
                        {
                            if (TJAPlayer3.DTX.List_DanSongs[NowShowingNumber].TitleTex != null)
                            {
                                TJAPlayer3.DTX.List_DanSongs[NowShowingNumber].TitleTex.Opacity--;
                            }
                            if (TJAPlayer3.DTX.List_DanSongs[NowShowingNumber].SubTitleTex != null)
                            {
                                TJAPlayer3.DTX.List_DanSongs[NowShowingNumber].SubTitleTex.Opacity--;
                            }
                        }
                    }
                }
                else
                {
                    if (TJAPlayer3.DTX.List_DanSongs[NowShowingNumber].TitleTex != null)
                    {
                        TJAPlayer3.DTX.List_DanSongs[NowShowingNumber].TitleTex.Opacity = 255;
                    }
                    if (TJAPlayer3.DTX.List_DanSongs[NowShowingNumber].SubTitleTex != null)
                    {
                        TJAPlayer3.DTX.List_DanSongs[NowShowingNumber].SubTitleTex.Opacity = 255;
                    }
                }
                Counter_Text_Old = Counter_Text.CurrentValue;
            }

            for (int i = 0; i < CExamInfo.cMaxExam; i++)
            {
                Status[i].Timer_Amount?.Tick();
            }

            // 背景を描画する。

            TJAPlayer3.Tx.DanC_Background?.t2D描画(0, 0);

            DrawExam(Challenge);

            // 幕のアニメーション
            if (Counter_In != null)
            {
                if (Counter_In.IsUnEnded)
                {
                    for (int i = Counter_In_Old; i < Counter_In.CurrentValue; i++)
                    {
                        ScreenPoint[0] += (TJAPlayer3.Skin.Game_Lane_X[0] - ScreenPoint[0]) / 180.0;
                        ScreenPoint[1] += ((TJAPlayer3.Skin.Resolution[0] / 2 + TJAPlayer3.Skin.Game_Lane_X[0] / 2) - ScreenPoint[1]) / 180.0;
                    }
                    Counter_In_Old = Counter_In.CurrentValue;
                    TJAPlayer3.Tx.DanC_Screen?.t2D描画((int)ScreenPoint[0], TJAPlayer3.Skin.Game_Lane_Y[0], new Rectangle(0, 0, TJAPlayer3.Tx.DanC_Screen.szTextureSize.Width / 2, TJAPlayer3.Tx.DanC_Screen.szTextureSize.Height));
                    TJAPlayer3.Tx.DanC_Screen?.t2D描画((int)ScreenPoint[1], TJAPlayer3.Skin.Game_Lane_Y[0], new Rectangle(TJAPlayer3.Tx.DanC_Screen.szTextureSize.Width / 2, 0, TJAPlayer3.Tx.DanC_Screen.szTextureSize.Width / 2, TJAPlayer3.Tx.DanC_Screen.szTextureSize.Height));
                    //CDTXMania.act文字コンソール.tPrint(0, 420, C文字コンソール.Eフォント種別.白, String.Format("{0} : {1}", ScreenPoint[0], ScreenPoint[1]));
                }
                if (Counter_In.IsEnded)
                {
                    Counter_In = null;
                    Counter_Wait = new CCounter(0, 2299, 1, TJAPlayer3.Timer);
                }
            }

            if (Counter_Wait != null)
            {
                if (Counter_Wait.IsUnEnded)
                {
                    TJAPlayer3.Tx.DanC_Screen?.t2D描画(TJAPlayer3.Skin.Game_Lane_X[0], TJAPlayer3.Skin.Game_Lane_Y[0]);

                    if (NowShowingNumber != 0)
                    {
                        if (Counter_Wait.CurrentValue >= 800)
                        {
                            if (!bExamChangeCheck)
                            {
                                for (int i = 0; i < CExamInfo.cMaxExam; i++)
                                    ExamChange[i] = false;

                                for (int j = 0; j < CExamInfo.cMaxExam; j++)
                                {
                                    if (TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[0].Dan_C[j] != null)
                                    {
                                        if (TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs.Count - 1].Dan_C[j] != null) //個別の条件がありますよー
                                        {
                                            Challenge[j] = TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[NowShowingNumber].Dan_C[j];
                                            ExamChange[j] = true;
                                        }
                                    }
                                }
                                NowCymbolShowingNumber = NowShowingNumber;
                                bExamChangeCheck = true;
                            }
                        }
                    }
                }
                if (Counter_Wait.IsEnded)
                {
                    Counter_Wait = null;
                    Counter_Out = new CCounter(0, 90, 3, TJAPlayer3.Timer);
                    Counter_Text = new CCounter(0, 2899, 1, TJAPlayer3.Timer);
                }
            }
            if (Counter_Text != null)
            {
                if (Counter_Text.IsUnEnded)
                {
                    var title = TJAPlayer3.DTX.List_DanSongs[NowShowingNumber].TitleTex;
                    var subTitle = TJAPlayer3.DTX.List_DanSongs[NowShowingNumber].SubTitleTex;
                    if (subTitle == null)
                        title?.t2D拡大率考慮中央基準描画(TJAPlayer3.Skin.Game_DanC_Title_X[0], TJAPlayer3.Skin.Game_DanC_Title_Y[0]);
                    else
                    {
                        title?.t2D拡大率考慮中央基準描画(TJAPlayer3.Skin.Game_DanC_Title_X[1], TJAPlayer3.Skin.Game_DanC_Title_Y[1]);
                        subTitle?.t2D拡大率考慮中央基準描画(TJAPlayer3.Skin.Game_DanC_SubTitle[0], TJAPlayer3.Skin.Game_DanC_SubTitle[1]);
                    }
                }
                if (Counter_Text.IsEnded)
                {
                    Counter_Text = null;
                    IsAnimating = false;
                }
            }
            if (Counter_Out != null)
            {
                if (Counter_Out.IsUnEnded)
                {
                    ScreenPoint[0] = TJAPlayer3.Skin.Game_Lane_X[0] - Math.Sin(Counter_Out.CurrentValue * (Math.PI / 180)) * 500;
                    ScreenPoint[1] = TJAPlayer3.Skin.Game_Lane_X[0] + TJAPlayer3.Tx.DanC_Screen.szTextureSize.Width / 2 + Math.Sin(Counter_Out.CurrentValue * (Math.PI / 180)) * 500;
                    TJAPlayer3.Tx.DanC_Screen?.t2D描画((int)ScreenPoint[0], TJAPlayer3.Skin.Game_Lane_Y[0], new Rectangle(0, 0, TJAPlayer3.Tx.DanC_Screen.szTextureSize.Width / 2, TJAPlayer3.Tx.DanC_Screen.szTextureSize.Height));
                    TJAPlayer3.Tx.DanC_Screen?.t2D描画((int)ScreenPoint[1], TJAPlayer3.Skin.Game_Lane_Y[0], new Rectangle(TJAPlayer3.Tx.DanC_Screen.szTextureSize.Width / 2, 0, TJAPlayer3.Tx.DanC_Screen.szTextureSize.Width / 2, TJAPlayer3.Tx.DanC_Screen.szTextureSize.Height));
                    //CDTXMania.act文字コンソール.tPrint(0, 420, C文字コンソール.Eフォント種別.白, String.Format("{0} : {1}", ScreenPoint[0], ScreenPoint[1]));
                }
                if (Counter_Out.IsEnded)
                {
                    Counter_Out = null;
                }
            }

            #region [Dan Plate]

            CActSelect段位リスト.tDisplayDanPlate(Dan_Plate, 
                null, 
                TJAPlayer3.Skin.Game_DanC_Dan_Plate[0], 
                TJAPlayer3.Skin.Game_DanC_Dan_Plate[1]);

            #endregion

            /*
            TJAPlayer3.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.白, TJAPlayer3.DTX.pDan_LastChip[NowShowingNumber].n発声時刻ms + " / " + CSound管理.rc演奏用タイマ.n現在時刻);

            TJAPlayer3.act文字コンソール.tPrint(100, 20, C文字コンソール.Eフォント種別.白, TJAPlayer3.DTX.pDan_LastChip[NowShowingNumber].n発声時刻ms.ToString());
            TJAPlayer3.act文字コンソール.tPrint(100, 40, C文字コンソール.Eフォント種別.白, TJAPlayer3.DTX.listChip[TJAPlayer3.DTX.listChip.Count - 1].n発声時刻ms.ToString());
            TJAPlayer3.act文字コンソール.tPrint(100, 60, C文字コンソール.Eフォント種別.白, TJAPlayer3.Timer.n現在時刻.ToString());
            */

            // Challenges that are judged when the song stops

            return base.Draw();
        }

        // Regular ingame exams draw
        public void DrawExam(Dan_C[] dan_C, bool isResult = false, int offX = 0)
        {
            int count = 0;
            int countNoGauge = 0;

            // Count exams, both with and without gauge
            for (int i = 0; i < CExamInfo.cMaxExam; i++)
            {
                if (dan_C[i] != null && dan_C[i].GetEnable() == true)
                {
                    count++;
                    if (dan_C[i].GetExamType() != Exam.Type.Gauge)
                        countNoGauge++;
                }
                    
            }

            // Bar position on the cert
            int currentPosition = -1;

            for (int i = 0; i < CExamInfo.cMaxExam; i++)
            {
                if (dan_C[i] == null || dan_C[i].GetEnable() != true)
                    continue ;

                if (dan_C[i].GetExamType() != Exam.Type.Gauge
                    || isResult)
                {
                    if (dan_C[i].GetExamType() != Exam.Type.Gauge)
                        currentPosition++;

                    // Determines if a small bar will be used to optimise the display layout
                    bool isSmallGauge = currentPosition >= 3 || (countNoGauge > 3 && countNoGauge % 3 > currentPosition) || countNoGauge == 6;

                    // Y index of the gauge
                    int yIndex = (currentPosition % 3) + 1;

                    // Specific case for gauge
                    if (dan_C[i].GetExamType() == Exam.Type.Gauge)
                    {
                        yIndex = 0;
                        isSmallGauge = false;
                    }
                        

                    // Panel origin
                    int xOrigin = (isResult) ? TJAPlayer3.Skin.DanResult_Exam[0] + offX : TJAPlayer3.Skin.Game_DanC_X[1];
                    int yOrigin = (isResult) ? TJAPlayer3.Skin.DanResult_Exam[1] : TJAPlayer3.Skin.Game_DanC_Y[1];

                    // Origin position which will be used as a reference for bar elements
                    int barXOffset = xOrigin + (currentPosition >= 3 ? TJAPlayer3.Skin.Game_DanC_Base_Offset_X[1] : TJAPlayer3.Skin.Game_DanC_Base_Offset_X[0]);
                    int barYOffset = yOrigin + (currentPosition >= 3 ? TJAPlayer3.Skin.Game_DanC_Base_Offset_Y[1] : TJAPlayer3.Skin.Game_DanC_Base_Offset_Y[0]) + TJAPlayer3.Skin.Game_DanC_Size[1] * yIndex + (yIndex * TJAPlayer3.Skin.Game_DanC_Padding);
                    
                    // Small bar
                    int lowerBarYOffset = barYOffset + TJAPlayer3.Skin.Game_DanC_Size[1] + TJAPlayer3.Skin.Game_DanC_Padding;

                    // Skin X : 70
                    // Skin Y : 292


                    #region [Gauge base]

                    if (!isSmallGauge)
                        TJAPlayer3.Tx.DanC_Base?.t2D描画(barXOffset, barYOffset, new RectangleF(0, ExamChange[i] ? TJAPlayer3.Tx.DanC_Base.szTextureSize.Height / 2 : 0, TJAPlayer3.Tx.DanC_Base.szTextureSize.Width, TJAPlayer3.Tx.DanC_Base.szTextureSize.Height / 2));
                    else
                        TJAPlayer3.Tx.DanC_Base_Small?.t2D描画(barXOffset, barYOffset, new RectangleF(0, ExamChange[i] ? TJAPlayer3.Tx.DanC_Base_Small.szTextureSize.Height / 2 : 0, TJAPlayer3.Tx.DanC_Base_Small.szTextureSize.Width, TJAPlayer3.Tx.DanC_Base_Small.szTextureSize.Height / 2));

                    #endregion

                    #region [Counter wait variables]

                    int counter800 = (Counter_Wait != null ? Counter_Wait.CurrentValue - 800 : 0);
                    int counter255M255 = (Counter_Wait != null ? 255 - (Counter_Wait.CurrentValue - (800 - 255)) : 0);

                    #endregion

                    #region [Small bars]

                    if (ExamChange[i] == true)
                    {
                        for (int j = 1; j < TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs.Count; j++)
                        {

                            if (!(TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[j - 1].Dan_C[i] != null && TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[NowShowingNumber].Dan_C[i] != null))
                                continue;

                            // rainbowBetterSuccess (bool) : is current minibar better success ? | drawGaugeTypetwo (int) : Gauge style [0,2]
                            #region [Success type variables]
        
                            bool rainbowBetterSuccess = GetExamStatus(TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[j - 1].Dan_C[i]) == Exam.Status.Better_Success 
                                && GetExamConfirmStatus(TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[j - 1].Dan_C[i]);

                            int amountToPercent;
                            int drawGaugeTypetwo = 0;

                            if (!rainbowBetterSuccess)
                            {
                                amountToPercent = TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[j - 1].Dan_C[i].GetAmountToPercent();

                                if (amountToPercent >= 100)
                                    drawGaugeTypetwo = 2;
                                else if (TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[j - 1].Dan_C[i].GetExamRange() == Exam.Range.More && amountToPercent >= 70 || amountToPercent > 70)
                                    drawGaugeTypetwo = 1;
                            }

                            #endregion

                            // Small bar elements base opacity
                            #region [Default opacity]

                            TJAPlayer3.Tx.DanC_SmallBase.Opacity = 255;
                            TJAPlayer3.Tx.DanC_Small_ExamCymbol.Opacity = 255;

                            TJAPlayer3.Tx.Gauge_Dan_Rainbow[0].Opacity = 255;
                            TJAPlayer3.Tx.DanC_MiniNumber.Opacity = 255;

                            TJAPlayer3.Tx.DanC_Gauge[drawGaugeTypetwo].Opacity = 255;

                            int miniIconOpacity = 255;

                            #endregion

                            // Currently showing song parameters
                            if (NowShowingNumber == j)
                            {
                                if (Counter_Wait != null && Counter_Wait.CurrentValue >= 800)
                                {
                                    #region [counter800 opacity]

                                    TJAPlayer3.Tx.DanC_SmallBase.Opacity = counter800;
                                    TJAPlayer3.Tx.DanC_Small_ExamCymbol.Opacity = counter800;

                                    TJAPlayer3.Tx.Gauge_Dan_Rainbow[0].Opacity = counter800;
                                    TJAPlayer3.Tx.DanC_MiniNumber.Opacity = counter800;

                                    TJAPlayer3.Tx.DanC_Gauge[drawGaugeTypetwo].Opacity = counter800;

                                    miniIconOpacity = counter800;

                                    #endregion
                                }
                                else if (Counter_In != null || (Counter_Wait != null && Counter_Wait.CurrentValue < 800))
                                {
                                    #region [0 opacity]

                                    TJAPlayer3.Tx.DanC_SmallBase.Opacity = 0;
                                    TJAPlayer3.Tx.DanC_Small_ExamCymbol.Opacity = 0;

                                    TJAPlayer3.Tx.Gauge_Dan_Rainbow[0].Opacity = 0;
                                    TJAPlayer3.Tx.DanC_MiniNumber.Opacity = 0;

                                    TJAPlayer3.Tx.DanC_Gauge[drawGaugeTypetwo].Opacity = 0;

                                    miniIconOpacity = 0;

                                    #endregion
                                }
                            }

                            // Bars starting from the song N
                            if (NowShowingNumber >= j && (j - NowShowingNumber) > -2)
                            {
                                // Determine bars width
                                TJAPlayer3.Tx.DanC_SmallBase.vcScaleRatio.X = isSmallGauge ? 0.34f : 1f;

                                int smallBarGap = (int)(33f * TJAPlayer3.Skin.Resolution[1] / 720f);

                                // 815 : Small base (70 + 745)
                                int miniBarPositionX = barXOffset + (isSmallGauge ? TJAPlayer3.Skin.Game_DanC_SmallBase_Offset_X[1] : TJAPlayer3.Skin.Game_DanC_SmallBase_Offset_X[0]);

                                // 613 + (j - 1) * 33 : Small base (barYoffset for 3rd exam : 494 + 119 + Local song offset (j - 1) * 33)
                                int miniBarPositionY = (barYOffset + (isSmallGauge ? TJAPlayer3.Skin.Game_DanC_SmallBase_Offset_Y[1] : TJAPlayer3.Skin.Game_DanC_SmallBase_Offset_Y[0])) + ((j - 1) % 2) * smallBarGap - (TJAPlayer3.Skin.Game_DanC_Size[1] + (TJAPlayer3.Skin.Game_DanC_Padding));

                                // Display bars
                                #region [Displayables]

                                // Display mini-bar base and small symbol
                                TJAPlayer3.Tx.DanC_SmallBase?.t2D描画(miniBarPositionX, miniBarPositionY);
                                TJAPlayer3.Tx.DanC_Small_ExamCymbol?.t2D描画(miniBarPositionX - 30, miniBarPositionY - 3, new RectangleF(0, (j - 1) * 28, 30, 28));

                                // Display bar content
                                if (rainbowBetterSuccess)
                                {
                                    TJAPlayer3.Tx.Gauge_Dan_Rainbow[0].vcScaleRatio.X = 0.23875f * TJAPlayer3.Tx.DanC_SmallBase.vcScaleRatio.X * (isSmallGauge ? 0.94f : 1f);
                                    TJAPlayer3.Tx.Gauge_Dan_Rainbow[0].vcScaleRatio.Y = 0.35185f;

                                    TJAPlayer3.Tx.Gauge_Dan_Rainbow[0]?.t2D描画(miniBarPositionX + 3, miniBarPositionY + 2,
                                        new Rectangle(0, 0, (int)(TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[j - 1].Dan_C[i].GetAmountToPercent() * (TJAPlayer3.Tx.Gauge_Dan_Rainbow[0].szTextureSize.Width / 100.0)), TJAPlayer3.Tx.Gauge_Dan_Rainbow[0].szTextureSize.Height));
                                }
                                else
                                {
                                    TJAPlayer3.Tx.DanC_Gauge[drawGaugeTypetwo].vcScaleRatio.X = 0.23875f * TJAPlayer3.Tx.DanC_SmallBase.vcScaleRatio.X * (isSmallGauge ? 0.94f : 1f);
                                    TJAPlayer3.Tx.DanC_Gauge[drawGaugeTypetwo].vcScaleRatio.Y = 0.35185f;

                                    TJAPlayer3.Tx.DanC_Gauge[drawGaugeTypetwo]?.t2D描画(miniBarPositionX + 3, miniBarPositionY + 2,
                                        new Rectangle(0, 0, (int)(TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[j - 1].Dan_C[i].GetAmountToPercent() * (TJAPlayer3.Tx.DanC_Gauge[drawGaugeTypetwo].szTextureSize.Width / 100.0)), TJAPlayer3.Tx.DanC_Gauge[drawGaugeTypetwo].szTextureSize.Height));
                                }

                                int _tmpMiniPadding = (int)(14f * TJAPlayer3.Skin.Resolution[0] / 1280f);

                                // Usually +23 for gold and +17 for white, to test
                                DrawMiniNumber(
                                    TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[j - 1].Dan_C[i].GetAmount(), 
                                    miniBarPositionX + 11, 
                                    miniBarPositionY + 20,
                                    _tmpMiniPadding, 
                                    TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[j - 1].Dan_C[i]);

                                CActSelect段位リスト.tDisplayDanIcon(j, miniBarPositionX + TJAPlayer3.Skin.Game_DanC_DanIcon_Offset_Mini[0], miniBarPositionY + TJAPlayer3.Skin.Game_DanC_DanIcon_Offset_Mini[1], miniIconOpacity, 0.5f, false);

                                #endregion
                            }
                        }
                    }

                    #endregion

                    #region [Currently playing song icons]

                    TJAPlayer3.Tx.DanC_ExamCymbol.Opacity = 255;

                    if (ExamChange[i] && NowShowingNumber != 0)
                    {
                        if (Counter_Wait != null)
                        {
                            if (Counter_Wait.CurrentValue >= 800)
                                TJAPlayer3.Tx.DanC_ExamCymbol.Opacity = counter800;
                            else if (Counter_Wait.CurrentValue >= 800 - 255)
                                TJAPlayer3.Tx.DanC_ExamCymbol.Opacity = counter255M255;
                        }
                    }

                    //75, 418
                    // 292 - 228 = 64
                    if (ExamChange[i])
                    {
                        TJAPlayer3.Tx.DanC_ExamCymbol.t2D描画(barXOffset + 5, lowerBarYOffset - 64, new RectangleF(0, 41 * NowCymbolShowingNumber, 197, 41));
                    }

                    #endregion

                    #region [Large bars]

                    // LrainbowBetterSuccess (bool) : is current minibar better success ? | LdrawGaugeTypetwo (int) : Gauge style [0,2]
                    #region [Success type variables]

                    bool LrainbowBetterSuccess = GetExamStatus(dan_C[i]) == Exam.Status.Better_Success && GetExamConfirmStatus(dan_C[i]);

                    int LamountToPercent;
                    int LdrawGaugeTypetwo = 0;

                    if (!LrainbowBetterSuccess)
                    {
                        LamountToPercent = dan_C[i].GetAmountToPercent();

                        if (LamountToPercent >= 100)
                            LdrawGaugeTypetwo = 2;
                        else if (dan_C[i].GetExamRange() == Exam.Range.More && LamountToPercent >= 70 || LamountToPercent > 70)
                            LdrawGaugeTypetwo = 1;
                    }



                    #endregion

                    // rainbowIndex : Rainbow bar texture to display (int), rainbowBase : same as rainbowIndex, but 0 if the counter is maxed
                    #region [Rainbow gauge counter]

                    int rainbowIndex = 0;
                    int rainbowBase = 0;
                    if (LrainbowBetterSuccess)
                    {
                        this.ct虹アニメ.TickLoop();
                        this.ct虹透明度.TickLoop();

                        rainbowIndex = this.ct虹アニメ.CurrentValue;

                        rainbowBase = rainbowIndex;
                        if (rainbowBase == ct虹アニメ.EndValue) rainbowBase = 0;
                    }

                    #endregion

                    #region [Default opacity]

                    TJAPlayer3.Tx.DanC_Gauge[LdrawGaugeTypetwo].Opacity = 255;

                    TJAPlayer3.Tx.Gauge_Dan_Rainbow[rainbowIndex].Opacity = 255;

                    TJAPlayer3.Tx.DanC_Number.Opacity = 255;
                    TJAPlayer3.Tx.DanC_ExamRange.Opacity = 255;
                    TJAPlayer3.Tx.DanC_Small_Number.Opacity = 255;

                    #endregion

                    int iconOpacity = 255;

                    if (ExamChange[i] && NowShowingNumber != 0 && Counter_Wait != null)
                    {
                        if (Counter_Wait.CurrentValue >= 800)
                        {
                            #region [counter800 opacity]

                            TJAPlayer3.Tx.DanC_Gauge[LdrawGaugeTypetwo].Opacity = counter800;

                            TJAPlayer3.Tx.Gauge_Dan_Rainbow[rainbowIndex].Opacity = counter800;

                            TJAPlayer3.Tx.DanC_Number.Opacity = counter800;
                            TJAPlayer3.Tx.DanC_ExamRange.Opacity = counter800;
                            TJAPlayer3.Tx.DanC_Small_Number.Opacity = counter800;

                            iconOpacity = counter800;

                            #endregion
                        }
                        else if (Counter_Wait.CurrentValue >= 800 - 255)
                        {
                            #region [counter255M255 opacity]

                            TJAPlayer3.Tx.DanC_Gauge[LdrawGaugeTypetwo].Opacity = counter255M255;

                            TJAPlayer3.Tx.Gauge_Dan_Rainbow[rainbowIndex].Opacity = counter255M255;

                            TJAPlayer3.Tx.DanC_Number.Opacity = counter255M255;
                            TJAPlayer3.Tx.DanC_ExamRange.Opacity = counter255M255;
                            TJAPlayer3.Tx.DanC_Small_Number.Opacity = counter255M255;

                            iconOpacity = counter255M255;

                            #endregion
                        }
                    }

                    #region [Displayables]

                    // Non individual : 209 / 650 : 0.32154f
                    // Individual : 97 / 432 : 0.22454f

                    float xExtend = ExamChange[i] ? (isSmallGauge ? 0.215f * 0.663333333f : 0.663333333f) : (isSmallGauge ? 0.32154f : 1.0f);

                    if (LrainbowBetterSuccess)
                    {
                        #region [Rainbow gauge display]

                        TJAPlayer3.Tx.Gauge_Dan_Rainbow[rainbowIndex].vcScaleRatio.X = xExtend;

                        // Reset base since it was used for minibars
                        TJAPlayer3.Tx.Gauge_Dan_Rainbow[0].vcScaleRatio.X = xExtend;
                        TJAPlayer3.Tx.Gauge_Dan_Rainbow[0].vcScaleRatio.Y = 1.0f;

                        if (Counter_Wait != null && !(Counter_Wait.CurrentValue <= 1055 && Counter_Wait.CurrentValue >= 800 - 255))
                        {
                            TJAPlayer3.Tx.Gauge_Dan_Rainbow[rainbowIndex].Opacity = 255;
                        }

                        TJAPlayer3.Tx.Gauge_Dan_Rainbow[rainbowIndex]?.t2D拡大率考慮下基準描画(
                            barXOffset + TJAPlayer3.Skin.Game_DanC_Offset[0], lowerBarYOffset - TJAPlayer3.Skin.Game_DanC_Offset[1],
                            new Rectangle(0, 0, (int)(dan_C[i].GetAmountToPercent() * (TJAPlayer3.Tx.Gauge_Dan_Rainbow[rainbowIndex].szTextureSize.Width / 100.0)), TJAPlayer3.Tx.Gauge_Dan_Rainbow[rainbowIndex].szTextureSize.Height));

                        if (Counter_Wait != null && !(Counter_Wait.CurrentValue <= 1055 && Counter_Wait.CurrentValue >= 800 - 255))
                        {
                            TJAPlayer3.Tx.Gauge_Dan_Rainbow[rainbowBase].Opacity = (ct虹透明度.CurrentValue * 255 / (int)ct虹透明度.EndValue) / 1;
                        }

                        TJAPlayer3.Tx.Gauge_Dan_Rainbow[rainbowBase]?.t2D拡大率考慮下基準描画(
                            barXOffset + TJAPlayer3.Skin.Game_DanC_Offset[0], lowerBarYOffset - TJAPlayer3.Skin.Game_DanC_Offset[1],
                            new Rectangle(0, 0, (int)(dan_C[i].GetAmountToPercent() * (TJAPlayer3.Tx.Gauge_Dan_Rainbow[rainbowBase].szTextureSize.Width / 100.0)), TJAPlayer3.Tx.Gauge_Dan_Rainbow[rainbowIndex].szTextureSize.Height));

                        #endregion
                    }
                    else
                    {
                        #region [Regular gauge display]

                        TJAPlayer3.Tx.DanC_Gauge[LdrawGaugeTypetwo].vcScaleRatio.X = xExtend;
                        TJAPlayer3.Tx.DanC_Gauge[LdrawGaugeTypetwo].vcScaleRatio.Y = 1.0f;
                        TJAPlayer3.Tx.DanC_Gauge[LdrawGaugeTypetwo]?.t2D拡大率考慮下基準描画(
                            barXOffset + TJAPlayer3.Skin.Game_DanC_Offset[0], lowerBarYOffset - TJAPlayer3.Skin.Game_DanC_Offset[1],
                            new Rectangle(0, 0, (int)(dan_C[i].GetAmountToPercent() * (TJAPlayer3.Tx.DanC_Gauge[LdrawGaugeTypetwo].szTextureSize.Width / 100.0)), TJAPlayer3.Tx.DanC_Gauge[LdrawGaugeTypetwo].szTextureSize.Height));

                        #endregion
                    }

                    #endregion


                    #endregion

                    #region [Print the current value number]

                    int nowAmount = dan_C[i].Amount;

                    if (dan_C[i].GetExamRange() == Exam.Range.Less)
                        nowAmount = dan_C[i].Value[0] - dan_C[i].Amount;

                    if (nowAmount < 0) nowAmount = 0;

                    float numberXScale = isSmallGauge ? TJAPlayer3.Skin.Game_DanC_Number_Small_Scale * 0.6f : TJAPlayer3.Skin.Game_DanC_Number_Small_Scale;
                    float numberYScale = isSmallGauge ? TJAPlayer3.Skin.Game_DanC_Number_Small_Scale * 0.8f : TJAPlayer3.Skin.Game_DanC_Number_Small_Scale;
                    int numberPadding = (int)(TJAPlayer3.Skin.Game_DanC_Number_Padding * (isSmallGauge ? 0.6f : 1f));

                    DrawNumber(nowAmount,
                        barXOffset + TJAPlayer3.Skin.Game_DanC_Number_Small_Number_Offset[0],
                        lowerBarYOffset - TJAPlayer3.Skin.Game_DanC_Number_Small_Number_Offset[1],
                        numberPadding, 
                        true, 
                        dan_C[i], 
                        numberXScale, 
                        numberYScale, 
                        (Status[i].Timer_Amount != null ? ScoreScale[Status[i].Timer_Amount.CurrentValue] : 0f));

                    #endregion

                    if (ExamChange[i])
                    {
                        CActSelect段位リスト.tDisplayDanIcon(NowShowingNumber + 1, barXOffset + TJAPlayer3.Skin.Game_DanC_DanIcon_Offset[0], barYOffset + TJAPlayer3.Skin.Game_DanC_DanIcon_Offset[1], iconOpacity, 0.6f, true);
                    }


                    #region [Dan conditions display]

                    int offset = TJAPlayer3.Skin.Game_DanC_Exam_Offset[0];

                    TJAPlayer3.Tx.DanC_ExamType.vcScaleRatio.X = 1.0f;
                    TJAPlayer3.Tx.DanC_ExamType.vcScaleRatio.Y = 1.0f;

                    // Exam range (Less than/More)
                    TJAPlayer3.Tx.DanC_ExamRange?.t2D拡大率考慮下基準描画(
                        barXOffset + offset - TJAPlayer3.Tx.DanC_ExamRange.szTextureSize.Width,
                        lowerBarYOffset - TJAPlayer3.Skin.Game_DanC_Exam_Offset[1], 
                        new Rectangle(0, TJAPlayer3.Skin.Game_DanC_ExamRange_Size[1] * (int)dan_C[i].GetExamRange(), TJAPlayer3.Skin.Game_DanC_ExamRange_Size[0], TJAPlayer3.Skin.Game_DanC_ExamRange_Size[1]));
                    
                    offset -= TJAPlayer3.Skin.Game_DanC_ExamRange_Padding;

                    // Condition number
                    DrawNumber(
                        dan_C[i].Value[0],
                        barXOffset + offset - dan_C[i].Value[0].ToString().Length * (int)(TJAPlayer3.Skin.Game_DanC_Number_Small_Padding * TJAPlayer3.Skin.Game_DanC_Exam_Number_Scale),
                        lowerBarYOffset - TJAPlayer3.Skin.Game_DanC_Exam_Offset[1] - 1, 
                        (int)(TJAPlayer3.Skin.Game_DanC_Number_Small_Padding * TJAPlayer3.Skin.Game_DanC_Exam_Number_Scale), 
                        false,
                        dan_C[i]);

                    int _offexX = (int)(22f * TJAPlayer3.Skin.Resolution[0] / 1280f);
                    int _offexY = (int)(48f * TJAPlayer3.Skin.Resolution[1] / 720f);
                    int _examX = barXOffset + TJAPlayer3.Skin.Game_DanC_Exam_Offset[0] - TJAPlayer3.Tx.DanC_ExamType.szTextureSize.Width + _offexX;
                    int _examY = lowerBarYOffset - TJAPlayer3.Skin.Game_DanC_Exam_Offset[1] - _offexY;

                    // Exam type flag
                    TJAPlayer3.Tx.DanC_ExamType?.t2D拡大率考慮下基準描画(
                        _examX,
                        _examY,
                        new Rectangle(0, 0, TJAPlayer3.Skin.Game_DanC_ExamType_Size[0], TJAPlayer3.Skin.Game_DanC_ExamType_Size[1]));

                    if ((int)dan_C[i].GetExamType() < this.ttkExams.Length)
                        TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(this.ttkExams[(int)dan_C[i].GetExamType()]).t2D拡大率考慮中央基準描画(
                            _examX + TJAPlayer3.Skin.Game_DanC_ExamType_Size[0] / 2,
                            _examY - TJAPlayer3.Skin.Game_DanC_ExamType_Size[1] / 2);


                    /*
                    TJAPlayer3.Tx.DanC_ExamType?.t2D拡大率考慮下基準描画(
                        barXOffset + TJAPlayer3.Skin.Game_DanC_Exam_Offset[0] - TJAPlayer3.Tx.DanC_ExamType.szテクスチャサイズ.Width + 22,
                        lowerBarYOffset - TJAPlayer3.Skin.Game_DanC_Exam_Offset[1] - 48, 
                        new Rectangle(0, TJAPlayer3.Skin.Game_DanC_ExamType_Size[1] * (int)dan_C[i].GetExamType(), TJAPlayer3.Skin.Game_DanC_ExamType_Size[0], TJAPlayer3.Skin.Game_DanC_ExamType_Size[1]));
                    */

                    #endregion

                    #region [Failed condition box]

                    TJAPlayer3.Tx.DanC_Failed.vcScaleRatio.X = isSmallGauge ? 0.33f : 1f;

                    if (dan_C[i].GetReached())
                    {
                        TJAPlayer3.Tx.DanC_Failed.t2D拡大率考慮下基準描画(
                            barXOffset + TJAPlayer3.Skin.Game_DanC_Offset[0],
                            lowerBarYOffset - TJAPlayer3.Skin.Game_DanC_Offset[1]);
                    }

                    #endregion
                
                }
                else
                {
                    #region [Gauge dan condition]

                    int _scale = (int)(14f * TJAPlayer3.Skin.Resolution[0] / 1280f);
                    int _nbX = (int)(292f * TJAPlayer3.Skin.Resolution[0] / 1280f);
                    int _nbY = (int)(64f * TJAPlayer3.Skin.Resolution[0] / 1280f);
                    int _offexX = (int)(104f * TJAPlayer3.Skin.Resolution[0] / 1280f);
                    int _offexY = (int)(21f * TJAPlayer3.Skin.Resolution[1] / 720f);

                    TJAPlayer3.Tx.DanC_Gauge_Base?.t2D描画(
                        TJAPlayer3.Skin.Game_DanC_X[0] - ((50 - dan_C[i].GetValue(false) / 2) * _scale) + 4, 
                        TJAPlayer3.Skin.Game_DanC_Y[0]);

                    TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(this.ttkExams[(int)Exam.Type.Gauge]).t2D拡大率考慮中央基準描画(
                            TJAPlayer3.Skin.Game_DanC_X[0] - ((50 - dan_C[i].GetValue(false) / 2) * _scale) + _offexX,
                            TJAPlayer3.Skin.Game_DanC_Y[0] + _offexY);

                    // Display percentage here
                    DrawNumber(
                        dan_C[i].Value[0],
                        TJAPlayer3.Skin.Game_DanC_X[0] - ((50 - dan_C[i].GetValue(false) / 2) * _scale) + _nbX - dan_C[i].Value[0].ToString().Length * (int)(TJAPlayer3.Skin.Game_DanC_Number_Small_Padding * TJAPlayer3.Skin.Game_DanC_Exam_Number_Scale),
                        TJAPlayer3.Skin.Game_DanC_Y[0] - TJAPlayer3.Skin.Game_DanC_Exam_Offset[1] + _nbY,
                        (int)(TJAPlayer3.Skin.Game_DanC_Number_Small_Padding * TJAPlayer3.Skin.Game_DanC_Exam_Number_Scale),
                        false,
                        dan_C[i]);

                    #endregion
                }
            }
        }

        /// <summary>
        /// 段位チャレンジの数字フォントで数字を描画します。
        /// </summary>
        /// <param name="value">値。</param>
        /// <param name="x">一桁目のX座標。</param>
        /// <param name="y">一桁目のY座標</param>
        /// <param name="padding">桁数間の字間</param>
        /// <param name="scaleX">拡大率X</param>
        /// <param name="scaleY">拡大率Y</param>
        /// <param name="scaleJump">アニメーション用拡大率(Yに加算される)。</param>
        private void DrawNumber(int value, int x, int y, int padding,　bool bBig, Dan_C dan_c, float scaleX = 1.0f, float scaleY = 1.0f, float scaleJump = 0.0f)
        {

            if (TJAPlayer3.Tx.DanC_Number == null || TJAPlayer3.Tx.DanC_Small_Number == null || value < 0)
                return;

            if (value == 0)
            {
                TJAPlayer3.Tx.DanC_Number.color4 = CConversion.ColorToColor4(Color.Gray);
                TJAPlayer3.Tx.DanC_Small_Number.color4 = CConversion.ColorToColor4(Color.Gray);
            }
            else
            {
                TJAPlayer3.Tx.DanC_Number.color4 = CConversion.ColorToColor4(Color.White);
                TJAPlayer3.Tx.DanC_Small_Number.color4 = CConversion.ColorToColor4(Color.White);
            }
                
            if (bBig)
            {
                var notesRemainDigit = 0;
                for (int i = 0; i < value.ToString().Length; i++)
                {
                    var number = Convert.ToInt32(value.ToString()[i].ToString());
                    Rectangle rectangle = new Rectangle(TJAPlayer3.Skin.Game_DanC_Number_Size[0] * number - 1, GetExamConfirmStatus(dan_c) ? TJAPlayer3.Skin.Game_DanC_Number_Size[1] : 0, TJAPlayer3.Skin.Game_DanC_Number_Size[0], TJAPlayer3.Skin.Game_DanC_Number_Size[1]);
                    if (TJAPlayer3.Tx.DanC_Number != null)
                    {
                        TJAPlayer3.Tx.DanC_Number.vcScaleRatio.X = scaleX;
                        TJAPlayer3.Tx.DanC_Number.vcScaleRatio.Y = scaleY + scaleJump;
                    }
                    TJAPlayer3.Tx.DanC_Number?.t2D拡大率考慮下中心基準描画(x - (notesRemainDigit * padding), y, rectangle);
                    notesRemainDigit--;
                }
            }
            else
            {
                var notesRemainDigit = 0;
                for (int i = 0; i < value.ToString().Length; i++)
                {
                    var number = Convert.ToInt32(value.ToString()[i].ToString());
                    Rectangle rectangle = new Rectangle(TJAPlayer3.Skin.Game_DanC_Small_Number_Size[0] * number - 1, 0, TJAPlayer3.Skin.Game_DanC_Small_Number_Size[0], TJAPlayer3.Skin.Game_DanC_Small_Number_Size[1]);
                    if (TJAPlayer3.Tx.DanC_Small_Number != null)
                    {
                        TJAPlayer3.Tx.DanC_Small_Number.vcScaleRatio.X = scaleX;
                        TJAPlayer3.Tx.DanC_Small_Number.vcScaleRatio.Y = scaleY + scaleJump;
                    }
                    TJAPlayer3.Tx.DanC_Small_Number?.t2D拡大率考慮下中心基準描画(x - (notesRemainDigit * padding), y, rectangle);
                    notesRemainDigit--;
                }
            }
        }

        public void DrawMiniNumber(int value, int x, int y, int padding, Dan_C dan_c)
        {
            if (TJAPlayer3.Tx.DanC_MiniNumber == null || value < 0)
                return;

            var notesRemainDigit = 0;
            if (value < 0)
                return;
            for (int i = 0; i < value.ToString().Length; i++)
            {
                var number = Convert.ToInt32(value.ToString()[i].ToString());
                Rectangle rectangle = new Rectangle(TJAPlayer3.Skin.Game_DanC_MiniNumber_Size[0] * number - 1, GetExamConfirmStatus(dan_c) ? TJAPlayer3.Skin.Game_DanC_MiniNumber_Size[1] : 0, TJAPlayer3.Skin.Game_DanC_MiniNumber_Size[0], TJAPlayer3.Skin.Game_DanC_MiniNumber_Size[1]);
                TJAPlayer3.Tx.DanC_MiniNumber.t2D拡大率考慮下中心基準描画(x - (notesRemainDigit * padding), y, rectangle);
                notesRemainDigit--;
            }
        }

        /// <summary>
        /// n個の条件がひとつ以上達成失敗しているかどうかを返します。
        /// </summary>
        /// <returns>n個の条件がひとつ以上達成失敗しているか。</returns>
        public bool GetFailedAllChallenges()
        {
            var isFailed = false;
            for (int i = 0; i < CExamInfo.cMaxExam; i++)
            {
                if (Challenge[i] == null) continue;
                
                for(int j = 0; j < TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs.Count; j++ )
                {
                    if(TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[j].Dan_C[i] != null)
                    {
                        if (TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[j].Dan_C[i].GetReached()) isFailed = true;
                    }
                }
                if (Challenge[i].GetReached()) isFailed = true;
            }
            return isFailed;
        }

        /// <summary>
        /// n個の条件で段位認定モードのステータスを返します。
        /// </summary>
        /// <param name="dan_C">条件。</param>
        /// <returns>ExamStatus。</returns>
        public Exam.Status GetExamStatus(Dan_C[] dan_C)
        {
            var status = Exam.Status.Better_Success;
            
            for (int i = 0; i < CExamInfo.cMaxExam; i++)
            {

                
                if (ExamChange[i] == true)
                {
                    for (int j = 1; j < TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs.Count; j++)
                    {
                        if (!(TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[j - 1].Dan_C[i] != null))
                            continue;

                        bool rainbowBetterSuccess = GetExamStatus(TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[j - 1].Dan_C[i]) == Exam.Status.Better_Success;

                        if (!rainbowBetterSuccess) status = Exam.Status.Success;
                    }
                }
                

                if (dan_C[i] == null || dan_C[i].GetEnable() != true)
                    continue;

                if (!dan_C[i].GetCleared()[1]) status = Exam.Status.Success;
                if (!dan_C[i].GetCleared()[0]) return (Exam.Status.Failure);
            }

            return status;
        }

        public Exam.Status GetExamStatus(Dan_C dan_C)
        {
            var status = Exam.Status.Better_Success;
            if (!dan_C.GetCleared()[1]) status = Exam.Status.Success;
            if (!dan_C.GetCleared()[0]) status = Exam.Status.Failure;
            return status;
        }

        public bool GetExamConfirmStatus(Dan_C dan_C)
        {
            switch (dan_C.GetExamRange())
            {
                case Exam.Range.Less:
                    {
                        if (GetExamStatus(dan_C) == Exam.Status.Better_Success && notesremain == 0)
                            return true;
                        else
                            return false;
                    }

                case Exam.Range.More:
                    {
                        if (dan_C.GetExamType() == Exam.Type.Accuracy && notesremain != 0)
                            return false;
                        else if (GetExamStatus(dan_C) == Exam.Status.Better_Success)
                            return true;
                        else
                            return false;
                    }
            }
            return false;
        }

        public Dan_C[] GetExam()
        {
            return Challenge;
        }


        private readonly float[] ScoreScale = new float[]
        {
            0.000f,
            0.111f, // リピート
            0.222f,
            0.185f,
            0.148f,
            0.129f,
            0.111f,
            0.074f,
            0.065f,
            0.033f,
            0.015f,
            0.000f
        };

        [StructLayout(LayoutKind.Sequential)]
        struct ChallengeStatus
        {
            public Color4 Color;
            public CCounter Timer_Gauge;
            public CCounter Timer_Amount;
            public CCounter Timer_Failed;
        }

        #region[ private ]
        //-----------------

        private bool bExamChangeCheck;
        private int notesremain;
        private int[] songsnotesremain;
        private bool[] ExamChange = new bool[CExamInfo.cMaxExam];
        private int ExamCount;
        private ChallengeStatus[] Status = new ChallengeStatus[CExamInfo.cMaxExam];
        private CTexture Dan_Plate;
        private bool[] IsEnded;
        public bool FirstSectionAnime;

        // アニメ関連
        public int NowShowingNumber;
        public int NowCymbolShowingNumber;
        private CCounter Counter_In, Counter_Wait, Counter_Out, Counter_Text;
        private double[] ScreenPoint;
        private int Counter_In_Old, Counter_Out_Old, Counter_Text_Old;
        public bool IsAnimating;

        //音声関連
        private CSound Sound_Section;
        private CSound Sound_Section_First;
        private CSound Sound_Failed;

        private CCounter ct虹アニメ;
        private CCounter ct虹透明度;

        private CCachedFontRenderer pfExamFont;
        private TitleTextureKey[] ttkExams;

        //-----------------
        #endregion
    }
}
