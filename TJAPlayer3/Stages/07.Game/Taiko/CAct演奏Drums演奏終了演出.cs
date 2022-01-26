using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using FDK;

namespace TJAPlayer3
{
    internal class CAct演奏Drums演奏終了演出 : CActivity
    {
        /// <summary>
        /// 課題
        /// _クリア失敗 →素材不足(確保はできる。切り出しと加工をしてないだけ。)
        /// _
        /// </summary>
        public CAct演奏Drums演奏終了演出()
        {
            base.b活性化してない = true;
        }

        public void Start()
        {
            // this.ct進行メイン = new CCounter(0, 500, 1000 / 60, TJAPlayer3.Timer);

            this.ct進行メイン = new CCounter(0, 300, 22, TJAPlayer3.Timer);
            this.ctEnd_ClearFailed = new CCounter(0, 69, 30, TJAPlayer3.Timer);
            this.ctEnd_FullCombo = new CCounter(0, 66, 33, TJAPlayer3.Timer);
            this.ctEnd_FullComboLoop = new CCounter(0, 2, 30, TJAPlayer3.Timer);
            this.ctEnd_DondaFullCombo = new CCounter(0, 61, 33, TJAPlayer3.Timer);
            this.ctEnd_DondaFullComboLoop = new CCounter(0, 2, 30, TJAPlayer3.Timer);

            // モードの決定。クリア失敗・フルコンボも事前に作っとく。
            if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
            {
                if (CFloorManagement.CurrentNumberOfLives > 0)
                {
                    if (TJAPlayer3.stage演奏ドラム画面.CChartScore[0].nMiss == 0)
                    {
                        if (TJAPlayer3.stage演奏ドラム画面.CChartScore[0].nGood == 0)
                            this.Mode[0] = EndMode.StageDondaFullCombo;
                        else
                            this.Mode[0] = EndMode.StageFullCombo;
                    }
                    else
                        this.Mode[0] = EndMode.StageCleared;
                }
                else
                    this.Mode[0] = EndMode.StageFailed;
            }
            else if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan)
            {
                // 段位認定モード。
                if (!TJAPlayer3.stage演奏ドラム画面.actDan.GetFailedAllChallenges())
                {
                    // 段位認定モード、クリア成功
                    // this.Mode[0] = EndMode.StageCleared;

                    if (TJAPlayer3.stage演奏ドラム画面.CChartScore[0].nMiss == 0)
                    {
                        if (TJAPlayer3.stage演奏ドラム画面.CChartScore[0].nGood == 0)
                            this.Mode[0] = EndMode.StageDondaFullCombo;
                        else
                            this.Mode[0] = EndMode.StageFullCombo;
                    }
                    else
                        this.Mode[0] = EndMode.StageCleared;


                }
                else
                {
                    // 段位認定モード、クリア失敗
                    this.Mode[0] = EndMode.StageFailed;
                }
            }
            else
            {
                // 通常のモード。
                // ここでフルコンボフラグをチェックするが現時点ではない。
                // 今の段階では魂ゲージ80%以上でチェック。
                for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                {
                    if (TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[i] >= 80)
                    {
                        if (TJAPlayer3.stage演奏ドラム画面.CChartScore[i].nMiss == 0)
                        //if (TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含まない.Drums.Miss == 0)
                        {
                            if (TJAPlayer3.stage演奏ドラム画面.CChartScore[i].nGood == 0)
                                //if (TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含まない.Drums.Great == 0)
                            {
                                this.Mode[i] = EndMode.StageDondaFullCombo;
                            }
                            else
                            {
                                this.Mode[i] = EndMode.StageFullCombo;
                            }
                        }
                        else
                        {
                            this.Mode[i] = EndMode.StageCleared;
                        }
                    }
                    else
                    {
                        this.Mode[i] = EndMode.StageFailed;
                    }
                }
            }
        }

        public override void On活性化()
        {
            this.bリザルトボイス再生済み = false;
            this.Mode = new EndMode[2];
            base.On活性化();
        }

        public override void On非活性化()
        {
            this.ct進行メイン = null;
            base.On非活性化();
        }

        public override void OnManagedリソースの作成()
        {
            this.b再生済み = false;
            if (!base.b活性化してない)
            {
                this.soundClear = TJAPlayer3.Sound管理.tサウンドを生成する(CSkin.Path(@"Sounds\Clear.ogg"), ESoundGroup.SoundEffect);
                this.soundFailed = TJAPlayer3.Sound管理.tサウンドを生成する(CSkin.Path(@"Sounds\Failed.ogg"), ESoundGroup.SoundEffect);
                this.soundFullCombo = TJAPlayer3.Sound管理.tサウンドを生成する(CSkin.Path(@"Sounds\Full combo.ogg"), ESoundGroup.SoundEffect);
                this.soundDondaFullCombo = TJAPlayer3.Sound管理.tサウンドを生成する(CSkin.Path(@"Sounds\Donda Full Combo.ogg"), ESoundGroup.SoundEffect);
                base.OnManagedリソースの作成();
            }
        }

        public override void OnManagedリソースの解放()
        {
            if (!base.b活性化してない)
            {
                if (this.soundClear != null)
                    this.soundClear.t解放する();
                if (this.soundFailed != null)
                    this.soundFailed.t解放する();
                if (this.soundFullCombo != null)
                    this.soundFullCombo.t解放する();
                if (this.soundDondaFullCombo != null)
                    this.soundDondaFullCombo.t解放する();
                base.OnManagedリソースの解放();
            }
        }

        #region [effects]
        // ------------------------------------
        private void showEndEffect_Failed(int i)
        {
            int[] y = new int[] { 0, 176 };

            this.ctEnd_ClearFailed.t進行();
            if (this.ctEnd_ClearFailed.n現在の値 <= 20 || TJAPlayer3.Tx.ClearFailed == null)
            {
                TJAPlayer3.Tx.End_ClearFailed[Math.Min(this.ctEnd_ClearFailed.n現在の値, TJAPlayer3.Tx.End_ClearFailed.Length - 1)]?.t2D描画(TJAPlayer3.app.Device, 505, y[i] + 145);
            }
            else if (this.ctEnd_ClearFailed.n現在の値 >= 20 && this.ctEnd_ClearFailed.n現在の値 <= 67)
            {
                TJAPlayer3.Tx.ClearFailed?.t2D描画(TJAPlayer3.app.Device, 502, y[i] + 192);
            }
            else if (this.ctEnd_ClearFailed.n現在の値 == 68)
            {
                TJAPlayer3.Tx.ClearFailed1?.t2D描画(TJAPlayer3.app.Device, 502, y[i] + 192);
            }
            else if (this.ctEnd_ClearFailed.n現在の値 >= 69)
            {
                TJAPlayer3.Tx.ClearFailed2?.t2D描画(TJAPlayer3.app.Device, 502, y[i] + 192);
            }
        }
        private void showEndEffect_Clear(int i)
        {
            int[] y = new int[] { 210, 386 };
            #region[ 文字 ]
            //登場アニメは20フレーム。うち最初の5フレームは半透過状態。
            float[] f文字拡大率 = new float[] { 1.04f, 1.11f, 1.15f, 1.19f, 1.23f, 1.26f, 1.30f, 1.31f, 1.32f, 1.32f, 1.32f, 1.30f, 1.30f, 1.26f, 1.25f, 1.19f, 1.15f, 1.11f, 1.05f, 1.0f };
            int[] n透明度 = new int[] { 43, 85, 128, 170, 213, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 };
            if (this.ct進行メイン.n現在の値 >= 17)
            {
                if (this.ct進行メイン.n現在の値 <= 36)
                {
                    TJAPlayer3.Tx.End_Clear_Text_.vc拡大縮小倍率.Y = f文字拡大率[this.ct進行メイン.n現在の値 - 17];
                    TJAPlayer3.Tx.End_Clear_Text_.Opacity = n透明度[this.ct進行メイン.n現在の値 - 17];
                    TJAPlayer3.Tx.End_Clear_Text_.t2D描画(TJAPlayer3.app.Device, 634, (int)(y[i] - ((90 * f文字拡大率[this.ct進行メイン.n現在の値 - 17]) - 90)), new Rectangle(0, 0, 90, 90));
                }
                else
                {
                    TJAPlayer3.Tx.End_Clear_Text_.vc拡大縮小倍率.Y = 1.0f;
                    TJAPlayer3.Tx.End_Clear_Text_.t2D描画(TJAPlayer3.app.Device, 634, y[i], new Rectangle(0, 0, 90, 90));
                }
            }
            if (this.ct進行メイン.n現在の値 >= 19)
            {
                if (this.ct進行メイン.n現在の値 <= 38)
                {
                    TJAPlayer3.Tx.End_Clear_Text_.vc拡大縮小倍率.Y = f文字拡大率[this.ct進行メイン.n現在の値 - 19];
                    TJAPlayer3.Tx.End_Clear_Text_.Opacity = n透明度[this.ct進行メイン.n現在の値 - 19];
                    TJAPlayer3.Tx.End_Clear_Text_.t2D描画(TJAPlayer3.app.Device, 692, (int)(y[i] - ((90 * f文字拡大率[this.ct進行メイン.n現在の値 - 19]) - 90)), new Rectangle(90, 0, 90, 90));
                }
                else
                {
                    TJAPlayer3.Tx.End_Clear_Text_.vc拡大縮小倍率.Y = 1.0f;
                    TJAPlayer3.Tx.End_Clear_Text_.t2D描画(TJAPlayer3.app.Device, 692, y[i], new Rectangle(90, 0, 90, 90));
                }
            }
            TJAPlayer3.Tx.End_Clear_Text_.vc拡大縮小倍率.Y = 1.0f;
            if (this.ct進行メイン.n現在の値 >= 21)
            {
                if (this.ct進行メイン.n現在の値 <= 40)
                {
                    TJAPlayer3.Tx.End_Clear_Text_.vc拡大縮小倍率.Y = f文字拡大率[this.ct進行メイン.n現在の値 - 21];
                    TJAPlayer3.Tx.End_Clear_Text_.Opacity = n透明度[this.ct進行メイン.n現在の値 - 21];
                    TJAPlayer3.Tx.End_Clear_Text_.t2D描画(TJAPlayer3.app.Device, 750, y[i] - (int)((90 * f文字拡大率[this.ct進行メイン.n現在の値 - 21]) - 90), new Rectangle(180, 0, 90, 90));
                }
                else
                {
                    TJAPlayer3.Tx.End_Clear_Text_.vc拡大縮小倍率.Y = 1.0f;
                    TJAPlayer3.Tx.End_Clear_Text_.t2D描画(TJAPlayer3.app.Device, 750, y[i], new Rectangle(180, 0, 90, 90));
                }
            }
            if (this.ct進行メイン.n現在の値 >= 23)
            {
                if (this.ct進行メイン.n現在の値 <= 42)
                {
                    TJAPlayer3.Tx.End_Clear_Text_.vc拡大縮小倍率.Y = f文字拡大率[this.ct進行メイン.n現在の値 - 23];
                    TJAPlayer3.Tx.End_Clear_Text_.Opacity = n透明度[this.ct進行メイン.n現在の値 - 23];
                    TJAPlayer3.Tx.End_Clear_Text_.t2D描画(TJAPlayer3.app.Device, 819, y[i] - (int)((90 * f文字拡大率[this.ct進行メイン.n現在の値 - 23]) - 90), new Rectangle(270, 0, 90, 90));
                }
                else
                {
                    TJAPlayer3.Tx.End_Clear_Text_.vc拡大縮小倍率.Y = 1.0f;
                    TJAPlayer3.Tx.End_Clear_Text_.t2D描画(TJAPlayer3.app.Device, 819, y[i], new Rectangle(270, 0, 90, 90));
                }
            }
            if (this.ct進行メイン.n現在の値 >= 25)
            {
                if (this.ct進行メイン.n現在の値 <= 44)
                {
                    TJAPlayer3.Tx.End_Clear_Text_.vc拡大縮小倍率.Y = f文字拡大率[this.ct進行メイン.n現在の値 - 25];
                    TJAPlayer3.Tx.End_Clear_Text_.Opacity = n透明度[this.ct進行メイン.n現在の値 - 25];
                    TJAPlayer3.Tx.End_Clear_Text_.t2D描画(TJAPlayer3.app.Device, 890, (y[i] + 2) - (int)((90 * f文字拡大率[this.ct進行メイン.n現在の値 - 25]) - 90), new Rectangle(360, 0, 90, 90));
                }
                else
                {
                    TJAPlayer3.Tx.End_Clear_Text_.vc拡大縮小倍率.Y = 1.0f;
                    TJAPlayer3.Tx.End_Clear_Text_.t2D描画(TJAPlayer3.app.Device, 890, y[i] + 2, new Rectangle(360, 0, 90, 90));
                }
            }
            if (this.ct進行メイン.n現在の値 >= 50 && this.ct進行メイン.n現在の値 < 90)
            {
                if (this.ct進行メイン.n現在の値 < 70)
                {
                    TJAPlayer3.Tx.End_Clear_Text_Effect.Opacity = (this.ct進行メイン.n現在の値 - 50) * (255 / 20);
                    TJAPlayer3.Tx.End_Clear_Text_Effect.t2D描画(TJAPlayer3.app.Device, 634, y[i] - 2);
                }
                else
                {
                    TJAPlayer3.Tx.End_Clear_Text_Effect.Opacity = 255 - ((this.ct進行メイン.n現在の値 - 70) * (255 / 20));
                    TJAPlayer3.Tx.End_Clear_Text_Effect.t2D描画(TJAPlayer3.app.Device, 634, y[i] - 2);
                }
            }
            #endregion
            #region[ バチお ]
            if (this.ct進行メイン.n現在の値 <= 11)
            {
                if (TJAPlayer3.Tx.End_Clear_L[1] != null)
                {
                    TJAPlayer3.Tx.End_Clear_L[1].t2D描画(TJAPlayer3.app.Device, 697, y[i] - 30);
                    TJAPlayer3.Tx.End_Clear_L[1].Opacity = (int)(11.0 / this.ct進行メイン.n現在の値) * 255;
                }
                if (TJAPlayer3.Tx.End_Clear_R[1] != null)
                {
                    TJAPlayer3.Tx.End_Clear_R[1].t2D描画(TJAPlayer3.app.Device, 738, y[i] - 30);
                    TJAPlayer3.Tx.End_Clear_R[1].Opacity = (int)(11.0 / this.ct進行メイン.n現在の値) * 255;
                }
            }
            else if (this.ct進行メイン.n現在の値 <= 35)
            {
                if (TJAPlayer3.Tx.End_Clear_L[0] != null)
                    TJAPlayer3.Tx.End_Clear_L[0].t2D描画(TJAPlayer3.app.Device, 697 - (int)((this.ct進行メイン.n現在の値 - 12) * 10), y[i] - 30);
                if (TJAPlayer3.Tx.End_Clear_R[0] != null)
                    TJAPlayer3.Tx.End_Clear_R[0].t2D描画(TJAPlayer3.app.Device, 738 + (int)((this.ct進行メイン.n現在の値 - 12) * 10), y[i] - 30);
            }
            else if (this.ct進行メイン.n現在の値 <= 46)
            {
                if (TJAPlayer3.Tx.End_Clear_L[0] != null)
                {
                    //2016.07.16 kairera0467 またも原始的...
                    float[] fRet = new float[] { 1.0f, 0.99f, 0.98f, 0.97f, 0.96f, 0.95f, 0.96f, 0.97f, 0.98f, 0.99f, 1.0f };
                    TJAPlayer3.Tx.End_Clear_L[0].t2D描画(TJAPlayer3.app.Device, 466, y[i] - 30);
                    TJAPlayer3.Tx.End_Clear_L[0].vc拡大縮小倍率 = new SlimDX.Vector3(fRet[this.ct進行メイン.n現在の値 - 36], 1.0f, 1.0f);
                    //CDTXMania.Tx.End_Clear_R[ 0 ].t2D描画( CDTXMania.app.Device, 956 + (( this.ct進行メイン.n現在の値 - 36 ) / 2), 180 );
                    TJAPlayer3.Tx.End_Clear_R[0].t2D描画(TJAPlayer3.app.Device, 1136 - 180 * fRet[this.ct進行メイン.n現在の値 - 36], y[i] - 30);
                    TJAPlayer3.Tx.End_Clear_R[0].vc拡大縮小倍率 = new SlimDX.Vector3(fRet[this.ct進行メイン.n現在の値 - 36], 1.0f, 1.0f);
                }
            }
            else if (this.ct進行メイン.n現在の値 <= 49)
            {
                if (TJAPlayer3.Tx.End_Clear_L[1] != null)
                    TJAPlayer3.Tx.End_Clear_L[1].t2D描画(TJAPlayer3.app.Device, 466, y[i] - 30);
                if (TJAPlayer3.Tx.End_Clear_R[1] != null)
                    TJAPlayer3.Tx.End_Clear_R[1].t2D描画(TJAPlayer3.app.Device, 956, y[i] - 30);
            }
            else if (this.ct進行メイン.n現在の値 <= 54)
            {
                if (TJAPlayer3.Tx.End_Clear_L[2] != null)
                    TJAPlayer3.Tx.End_Clear_L[2].t2D描画(TJAPlayer3.app.Device, 466, y[i] - 30);
                if (TJAPlayer3.Tx.End_Clear_R[2] != null)
                    TJAPlayer3.Tx.End_Clear_R[2].t2D描画(TJAPlayer3.app.Device, 956, y[i] - 30);
            }
            else if (this.ct進行メイン.n現在の値 <= 58)
            {
                if (TJAPlayer3.Tx.End_Clear_L[3] != null)
                    TJAPlayer3.Tx.End_Clear_L[3].t2D描画(TJAPlayer3.app.Device, 466, y[i] - 30);
                if (TJAPlayer3.Tx.End_Clear_R[3] != null)
                    TJAPlayer3.Tx.End_Clear_R[3].t2D描画(TJAPlayer3.app.Device, 956, y[i] - 30);
            }
            else
            {
                if (TJAPlayer3.Tx.End_Clear_L[4] != null)
                    TJAPlayer3.Tx.End_Clear_L[4].t2D描画(TJAPlayer3.app.Device, 466, y[i] - 30);
                if (TJAPlayer3.Tx.End_Clear_R[4] != null)
                    TJAPlayer3.Tx.End_Clear_R[4].t2D描画(TJAPlayer3.app.Device, 956, y[i] - 30);
            }
            #endregion
        }

        private void showEndEffect_FullCombo(int i)
        {
            int[] y = new int[] { 0, 176 };

            this.ctEnd_FullCombo.t進行();
            TJAPlayer3.Tx.End_FullCombo[this.ctEnd_FullCombo.n現在の値].t2D描画(TJAPlayer3.app.Device, 330, y[i] + 50);

            if (this.ctEnd_FullCombo.b終了値に達した && TJAPlayer3.Tx.End_FullComboLoop[0] != null)
            {
                this.ctEnd_FullComboLoop.t進行Loop();
                TJAPlayer3.Tx.End_FullComboLoop[this.ctEnd_FullComboLoop.n現在の値].t2D描画(TJAPlayer3.app.Device, 330, y[i] + 196);
            }
        }

        private void showEndEffect_DondaFullCombo(int i)
        {
            int[] y = new int[] { 0, 176 };

            this.ctEnd_DondaFullCombo.t進行();
            if (this.ctEnd_DondaFullCombo.n現在の値 >= 34) TJAPlayer3.Tx.End_DondaFullComboBg.t2D描画(TJAPlayer3.app.Device, 332, y[i] + 192);
            TJAPlayer3.Tx.End_DondaFullCombo[this.ctEnd_DondaFullCombo.n現在の値].t2D描画(TJAPlayer3.app.Device, 330, y[i] + 50);

            /*
            if (this.ctEnd_DondaFullCombo.b終了値に達した)
            {
                this.ctEnd_DondaFullComboLoop.t進行Loop();
                TJAPlayer3.Tx.End_DondaFullComboLoop[this.ctEnd_DondaFullComboLoop.n現在の値].t2D描画(TJAPlayer3.app.Device, 330, 196);
            }
            */
        }
        // ------------------------------------
        #endregion

        public override int On進行描画()
        {
            if (base.b初めての進行描画)
            {
                base.b初めての進行描画 = false;
            }
            if (this.ct進行メイン != null && (TJAPlayer3.stage演奏ドラム画面.eフェーズID == CStage.Eフェーズ.演奏_演奏終了演出 || TJAPlayer3.stage演奏ドラム画面.eフェーズID == CStage.Eフェーズ.演奏_STAGE_CLEAR_フェードアウト))
            {
                this.ct進行メイン.t進行();

                //CDTXMania.act文字コンソール.tPrint( 0, 0, C文字コンソール.Eフォント種別.灰, this.ct進行メイン.n現在の値.ToString() );
                //仮置き
                for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                {
                    switch (this.Mode[i])
                    {
                        case EndMode.StageFailed:
                            //this.ct進行メイン.n現在の値 = 18;
                            if (this.soundFailed != null && !this.b再生済み)
                            {
                                this.soundFailed.t再生を開始する();
                                this.b再生済み = true;
                            }
                            this.showEndEffect_Failed(i);
                            break;
                        case EndMode.StageCleared:
                            //this.ct進行メイン.n現在の値 = 18;
                            if (this.soundClear != null && !this.b再生済み)
                            {
                                this.soundClear.t再生を開始する();
                                this.b再生済み = true;
                            }
                            this.showEndEffect_Clear(i);
                            break;
                        case EndMode.StageFullCombo:
                            //this.ct進行メイン.n現在の値 = 18;
                            if (this.soundFullCombo != null && !this.b再生済み)
                            {
                                this.soundFullCombo.t再生を開始する();
                                this.b再生済み = true;
                            }
                            this.showEndEffect_FullCombo(i);
                            break;
                        case EndMode.StageDondaFullCombo:
                            //this.ct進行メイン.n現在の値 = 18;
                            if (this.soundDondaFullCombo != null && !this.b再生済み)
                            {
                                this.soundDondaFullCombo.t再生を開始する();
                                this.b再生済み = true;
                            }
                            this.showEndEffect_DondaFullCombo(i);
                            break;
                        default:
                            break;
                    }

                }



                if (this.ct進行メイン.b終了値に達した)
                {
                    return 1;
                }
            }

            return 0;
        }

        #region[ private ]
        //-----------------
        bool b再生済み;
        bool bリザルトボイス再生済み;
        CCounter ct進行メイン;

        CCounter ctEnd_ClearFailed;
        CCounter ctEnd_FullCombo;
        CCounter ctEnd_FullComboLoop;
        CCounter ctEnd_DondaFullCombo;
        CCounter ctEnd_DondaFullComboLoop;

        CCounter ct進行Loop;
        CSound soundClear;
        CSound soundFailed;
        CSound soundFullCombo;
        CSound soundDondaFullCombo;
        EndMode[] Mode;
        enum EndMode
        {
            StageFailed,
            StageCleared,
            StageFullCombo,
            StageDondaFullCombo
        }

        void StarDraw(int x, int y, int count, int starttime = 0, int Endtime = 20)
        {
            if (count >= 0 && count <= Endtime)
            {
                count += starttime;

                if (count <= 11)
                {
                    TJAPlayer3.Tx.End_Star.vc拡大縮小倍率.X = count * 0.09f;
                    TJAPlayer3.Tx.End_Star.vc拡大縮小倍率.Y = count * 0.09f;
                    TJAPlayer3.Tx.End_Star.Opacity = 255;
                    TJAPlayer3.Tx.End_Star.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x, y);
                }
                else if (count <= 20)
                {
                    TJAPlayer3.Tx.End_Star.vc拡大縮小倍率.X = 1.0f;
                    TJAPlayer3.Tx.End_Star.vc拡大縮小倍率.Y = 1.0f;
                    TJAPlayer3.Tx.End_Star.Opacity = (int)(255 - (255.0f / 9.0f) * (count - 11));
                    TJAPlayer3.Tx.End_Star.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x, y);
                }
            }
        }

        //-----------------
        #endregion
    }
}
