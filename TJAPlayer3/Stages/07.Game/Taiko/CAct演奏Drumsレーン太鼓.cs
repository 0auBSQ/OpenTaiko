using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Diagnostics;
using SlimDX;
using FDK;

namespace TJAPlayer3
{
    internal class CAct演奏Drumsレーン太鼓 : CActivity
    {
        /// <summary>
        /// レーンを描画するクラス。
        /// 
        /// 
        /// </summary>
        public CAct演奏Drumsレーン太鼓()
        {
            base.b活性化してない = true;
        }

        public override void On活性化()
        {
            for (int i = 0; i < 4; i++)
            {
                this.st状態[i].ct進行 = new CCounter();
                this.stBranch[i].ct分岐アニメ進行 = new CCounter();
                this.stBranch[i].nフラッシュ制御タイマ = -1;
                this.stBranch[i].nBranchレイヤー透明度 = 0;
                this.stBranch[i].nBranch文字透明度 = 0;
                this.stBranch[i].nY座標 = 0;
            }
            this.ctゴーゴー = new CCounter();


            this.n総移動時間 = -1;
            this.n総移動時間2 = -1;
            this.nDefaultJudgePos[0, 0] = TJAPlayer3.Skin.nScrollFieldX[0];
            this.nDefaultJudgePos[0, 1] = TJAPlayer3.Skin.nScrollFieldY[0];
            this.nDefaultJudgePos[1, 0] = TJAPlayer3.Skin.nScrollFieldX[1];
            this.nDefaultJudgePos[1, 1] = TJAPlayer3.Skin.nScrollFieldY[1];
            this.ctゴーゴー炎 = new CCounter(0, 6, 50, TJAPlayer3.Timer);
            base.On活性化();
        }

        public override void On非活性化()
        {
            for (int i = 0; i < 4; i++)
            {
                this.st状態[i].ct進行 = null;
                this.stBranch[i].ct分岐アニメ進行 = null;
            }
            TJAPlayer3.Skin.nScrollFieldX[0] = this.nDefaultJudgePos[0, 0];
            TJAPlayer3.Skin.nScrollFieldY[0] = this.nDefaultJudgePos[0, 1];
            TJAPlayer3.Skin.nScrollFieldX[1] = this.nDefaultJudgePos[1, 0];
            TJAPlayer3.Skin.nScrollFieldY[1] = this.nDefaultJudgePos[1, 1];
            this.ctゴーゴー = null;

            base.On非活性化();
        }

        public override void OnManagedリソースの作成()
        {
            //this.txLane = CDTXMania.tテクスチャの生成(CSkin.Path(@"Graphics\7_fieldbgA.png"));
            //this.txLaneB = CDTXMania.tテクスチャの生成(CSkin.Path(@"Graphics\7_fieldbgB.png"));
            //this.txゴーゴー = CDTXMania.tテクスチャの生成(CSkin.Path(@"Graphics\7_fieldbgC.png"));
            //this.tx普通譜面[0] = CDTXMania.tテクスチャの生成(CSkin.Path(@"Graphics\7_field_normal_base.png"));
            //this.tx玄人譜面[0] = CDTXMania.tテクスチャの生成(CSkin.Path(@"Graphics\7_field_expert_base.png"));
            //this.tx達人譜面[0] = CDTXMania.tテクスチャの生成(CSkin.Path(@"Graphics\7_field_master_base.png"));
            //this.tx普通譜面[1] = CDTXMania.tテクスチャの生成(CSkin.Path(@"Graphics\7_field_normal.png"));
            //this.tx玄人譜面[1] = CDTXMania.tテクスチャの生成(CSkin.Path(@"Graphics\7_field_expert.png"));
            //this.tx達人譜面[1] = CDTXMania.tテクスチャの生成(CSkin.Path(@"Graphics\7_field_master.png"));
            //this.tx枠線 = CDTXMania.tテクスチャの生成(CSkin.Path(@"Graphics\7_mtaiko_B.png"));
            //this.tx判定枠 = CDTXMania.tテクスチャの生成(CSkin.Path(@"Graphics\7_taiko_notes.png"));
            //this.txアタックエフェクトLower = CDTXMania.tテクスチャの生成Af( CSkin.Path( @"Graphics\7_explosion_lower.png" ) );

            //this.txゴーゴー炎 = CDTXMania.tテクスチャの生成(CSkin.Path(@"Graphics\7_gogo_fire.png"));

            //this.txゴーゴースプラッシュ = new CTexture[29];
            //for (int i = 0; i < 29; i++)
            //{
            //    this.txゴーゴースプラッシュ[i] = CDTXMania.tテクスチャの生成(CSkin.Path(@"Graphics\Dancer\gogo_splash\" + i.ToString() + ".png"));
            //    this.txゴーゴースプラッシュ[i].b加算合成 = true;
            //}

            //this.txArアタックエフェクトLower_A = new CTexture[15];
            //this.txArアタックエフェクトLower_B = new CTexture[15];
            //this.txArアタックエフェクトLower_C = new CTexture[15];
            //this.txArアタックエフェクトLower_D = new CTexture[15];
            //for (int i = 0; i < 15; i++)
            //{
            //    this.txArアタックエフェクトLower_A[i] = CDTXMania.tテクスチャの生成(CSkin.Path(@"Graphics\Effect\lower_1_" + i.ToString() + ".png"));
            //    this.txArアタックエフェクトLower_B[i] = CDTXMania.tテクスチャの生成(CSkin.Path(@"Graphics\Effect\lower_2_" + i.ToString() + ".png"));
            //    this.txArアタックエフェクトLower_C[i] = CDTXMania.tテクスチャの生成(CSkin.Path(@"Graphics\Effect\lower_3_" + i.ToString() + ".png"));
            //    this.txArアタックエフェクトLower_D[i] = CDTXMania.tテクスチャの生成(CSkin.Path(@"Graphics\Effect\lower_4_" + i.ToString() + ".png"));

            //    //this.txArアタックエフェクトLower_A[ i ].b加算合成 = true;
            //    //this.txArアタックエフェクトLower_B[ i ].b加算合成 = true;
            //    //this.txArアタックエフェクトLower_C[ i ].b加算合成 = true;
            //    //this.txArアタックエフェクトLower_D[ i ].b加算合成 = true;
            //}
            base.OnManagedリソースの作成();
        }

        public override void OnManagedリソースの解放()
        {
            //CDTXMania.tテクスチャの解放(ref this.txLane);
            //CDTXMania.tテクスチャの解放(ref this.txLaneB);
            //CDTXMania.tテクスチャの解放(ref this.txゴーゴー);
            //CDTXMania.tテクスチャの解放(ref this.tx普通譜面[0]);
            //CDTXMania.tテクスチャの解放(ref this.tx玄人譜面[0]);
            //CDTXMania.tテクスチャの解放(ref this.tx達人譜面[0]);
            //CDTXMania.tテクスチャの解放(ref this.tx普通譜面[1]);
            //CDTXMania.tテクスチャの解放(ref this.tx玄人譜面[1]);
            //CDTXMania.tテクスチャの解放(ref this.tx達人譜面[1]);

            //CDTXMania.tテクスチャの解放(ref this.tx枠線);
            //CDTXMania.tテクスチャの解放(ref this.tx判定枠);
            ////CDTXMania.tテクスチャの解放( ref this.txアタックエフェクトLower );

            //CDTXMania.tテクスチャの解放(ref this.txゴーゴー炎);

            //for (int i = 0; i < 29; i++)
            //{
            //    CDTXMania.tテクスチャの解放(ref this.txゴーゴースプラッシュ[i]);
            //}

            //for (int i = 0; i < 15; i++)
            //{
            //    CDTXMania.tテクスチャの解放(ref this.txArアタックエフェクトLower_A[i]);
            //    CDTXMania.tテクスチャの解放(ref this.txArアタックエフェクトLower_B[i]);
            //    CDTXMania.tテクスチャの解放(ref this.txArアタックエフェクトLower_C[i]);
            //    CDTXMania.tテクスチャの解放(ref this.txArアタックエフェクトLower_D[i]);
            //}

            base.OnManagedリソースの解放();
        }

        public override int On進行描画()
        {
            if (base.b初めての進行描画)
            {
                for (int i = 0; i < 4; i++)
                    this.stBranch[i].nフラッシュ制御タイマ = (long)(CSound管理.rc演奏用タイマ.n現在時刻 * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0));
                base.b初めての進行描画 = false;
            }

            //それぞれが独立したレイヤーでないといけないのでforループはパーツごとに分離すること。

            #region[ レーン本体 ]
            if (TJAPlayer3.Tx.Lane_Background_Main != null)
            {
                for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                {
                    TJAPlayer3.Tx.Lane_Background_Main.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.nScrollFieldBGX[i], TJAPlayer3.Skin.nScrollFieldY[i]);
                }
            }
            #endregion
            for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                #region[ 分岐アニメ制御タイマー ]
                long num = FDK.CSound管理.rc演奏用タイマ.n現在時刻;
                if (num < this.stBranch[i].nフラッシュ制御タイマ)
                {
                    this.stBranch[i].nフラッシュ制御タイマ = num;
                }
                while ((num - this.stBranch[i].nフラッシュ制御タイマ) >= 30)
                {
                    if (this.stBranch[i].nBranchレイヤー透明度 <= 255)
                    {
                        this.stBranch[i].nBranchレイヤー透明度 += 10;
                    }

                    if (this.stBranch[i].nBranch文字透明度 >= 0)
                    {
                        this.stBranch[i].nBranch文字透明度 -= 10;
                    }

                    if (this.stBranch[i].nY座標 != 0 && this.stBranch[i].nY座標 <= 20)
                    {
                        this.stBranch[i].nY座標++;
                    }

                    this.stBranch[i].nフラッシュ制御タイマ += 8;
                }

                if (!this.stBranch[i].ct分岐アニメ進行.b停止中)
                {
                    this.stBranch[i].ct分岐アニメ進行.t進行();
                    if (this.stBranch[i].ct分岐アニメ進行.b終了値に達した)
                    {
                        this.stBranch[i].ct分岐アニメ進行.t停止();
                    }
                }
                #endregion
            }
            #region[ 分岐レイヤー ]
            for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                if (TJAPlayer3.stage演奏ドラム画面.bUseBranch[i] == true)
                {
                    #region[ 動いていない ]
                    switch (TJAPlayer3.stage演奏ドラム画面.nレーン用表示コース[i])
                    {
                        case CDTX.ECourse.eNormal:
                            if (TJAPlayer3.Tx.Lane_Base[0] != null)
                            {
                                TJAPlayer3.Tx.Lane_Base[0].Opacity = 255;
                                TJAPlayer3.Tx.Lane_Base[0].t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.nScrollFieldBGX[i], TJAPlayer3.Skin.nScrollFieldY[i]);
                            }
                            break;
                        case CDTX.ECourse.eExpert:
                            if (TJAPlayer3.Tx.Lane_Base[1] != null)
                            {
                                TJAPlayer3.Tx.Lane_Base[1].Opacity = 255;
                                TJAPlayer3.Tx.Lane_Base[1].t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.nScrollFieldBGX[i], TJAPlayer3.Skin.nScrollFieldY[i]);
                            }
                            break;
                        case CDTX.ECourse.eMaster:
                            if (TJAPlayer3.Tx.Lane_Base[2] != null)
                            {
                                TJAPlayer3.Tx.Lane_Base[2].Opacity = 255;
                                TJAPlayer3.Tx.Lane_Base[2].t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.nScrollFieldBGX[i], TJAPlayer3.Skin.nScrollFieldY[i]);
                            }
                            break;
                    }
                    #endregion

                    if (TJAPlayer3.ConfigIni.nBranchAnime == 1)
                    {
                        #region[ AC7～14風の背後レイヤー ]
                        if (this.stBranch[i].ct分岐アニメ進行.b進行中)
                        {
                            int n透明度 = ((100 - this.stBranch[i].ct分岐アニメ進行.n現在の値) * 0xff) / 100;

                            if (this.stBranch[i].ct分岐アニメ進行.b終了値に達した)
                            {
                                n透明度 = 255;
                                this.stBranch[i].ct分岐アニメ進行.t停止();
                            }

                            #region[ 普通譜面_レベルアップ ]
                            //普通→玄人
                            if (this.stBranch[i].nBefore == CDTX.ECourse.eNormal && this.stBranch[i].nAfter == CDTX.ECourse.eExpert)
                            {
                                if (TJAPlayer3.Tx.Lane_Base[0] != null && TJAPlayer3.Tx.Lane_Base[1] != null)
                                {
                                    TJAPlayer3.Tx.Lane_Base[0].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                    TJAPlayer3.Tx.Lane_Base[1].Opacity = this.stBranch[i].nBranchレイヤー透明度;
                                    TJAPlayer3.Tx.Lane_Base[1].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                }
                            }
                            //普通→達人
                            if (this.stBranch[i].nBefore == CDTX.ECourse.eNormal && this.stBranch[i].nAfter == CDTX.ECourse.eMaster)
                            {
                                if (this.stBranch[i].ct分岐アニメ進行.n現在の値 < 100)
                                {
                                    n透明度 = ((100 - this.stBranch[i].ct分岐アニメ進行.n現在の値) * 0xff) / 100;
                                }
                                if (TJAPlayer3.Tx.Lane_Base[0] != null && TJAPlayer3.Tx.Lane_Base[2] != null)
                                {
                                    TJAPlayer3.Tx.Lane_Base[0].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                    TJAPlayer3.Tx.Lane_Base[2].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                    TJAPlayer3.Tx.Lane_Base[2].Opacity = this.stBranch[i].nBranchレイヤー透明度;
                                }
                            }
                            #endregion
                            #region[ 玄人譜面_レベルアップ ]
                            if (this.stBranch[i].nBefore == CDTX.ECourse.eExpert && this.stBranch[i].nAfter == CDTX.ECourse.eMaster)
                            {
                                if (TJAPlayer3.Tx.Lane_Base[1] != null && TJAPlayer3.Tx.Lane_Base[2] != null)
                                {
                                    TJAPlayer3.Tx.Lane_Base[1].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                    TJAPlayer3.Tx.Lane_Base[2].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                    TJAPlayer3.Tx.Lane_Base[2].Opacity = this.stBranch[i].nBranchレイヤー透明度;
                                }
                            }
                            #endregion
                            #region[ 玄人譜面_レベルダウン ]
                            if (this.stBranch[i].nBefore == CDTX.ECourse.eExpert && this.stBranch[i].nAfter == CDTX.ECourse.eNormal)
                            {
                                if (TJAPlayer3.Tx.Lane_Base[1] != null && TJAPlayer3.Tx.Lane_Base[0] != null)
                                {
                                    TJAPlayer3.Tx.Lane_Base[1].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                    TJAPlayer3.Tx.Lane_Base[0].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                    TJAPlayer3.Tx.Lane_Base[0].Opacity = this.stBranch[i].nBranchレイヤー透明度;
                                }
                            }
                            #endregion
                            #region[ 達人譜面_レベルダウン ]
                            if (this.stBranch[i].nBefore == CDTX.ECourse.eMaster && this.stBranch[i].nAfter == CDTX.ECourse.eNormal)
                            {
                                if (TJAPlayer3.Tx.Lane_Base[2] != null && TJAPlayer3.Tx.Lane_Base[0] != null)
                                {
                                    TJAPlayer3.Tx.Lane_Base[2].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                    TJAPlayer3.Tx.Lane_Base[0].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                    TJAPlayer3.Tx.Lane_Base[0].Opacity = this.stBranch[i].nBranchレイヤー透明度;
                                }
                            }
                            #endregion
                        }
                        #endregion
                    }
                    else if (TJAPlayer3.ConfigIni.nBranchAnime == 0)
                    {
                        TJAPlayer3.stage演奏ドラム画面.actLane.On進行描画();
                    }
                }
            }
            #endregion
            for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                #region[ ゴーゴータイムレーン背景レイヤー ]
                if (TJAPlayer3.Tx.Lane_Background_GoGo != null && TJAPlayer3.stage演奏ドラム画面.bIsGOGOTIME[i])
                {
                    if (!this.ctゴーゴー.b停止中)
                    {
                        this.ctゴーゴー.t進行();
                    }

                    if (this.ctゴーゴー.n現在の値 <= 4)
                    {
                        TJAPlayer3.Tx.Lane_Background_GoGo.vc拡大縮小倍率.Y = 0.2f;
                        TJAPlayer3.Tx.Lane_Background_GoGo.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.nScrollFieldBGX[i], TJAPlayer3.Skin.nScrollFieldY[i] + 54);
                    }
                    else if (this.ctゴーゴー.n現在の値 <= 5)
                    {
                        TJAPlayer3.Tx.Lane_Background_GoGo.vc拡大縮小倍率.Y = 0.4f;
                        TJAPlayer3.Tx.Lane_Background_GoGo.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.nScrollFieldBGX[i], TJAPlayer3.Skin.nScrollFieldY[i] + 40);
                    }
                    else if (this.ctゴーゴー.n現在の値 <= 6)
                    {
                        TJAPlayer3.Tx.Lane_Background_GoGo.vc拡大縮小倍率.Y = 0.6f;
                        TJAPlayer3.Tx.Lane_Background_GoGo.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.nScrollFieldBGX[i], TJAPlayer3.Skin.nScrollFieldY[i] + 26);
                    }
                    else if (this.ctゴーゴー.n現在の値 <= 8)
                    {
                        TJAPlayer3.Tx.Lane_Background_GoGo.vc拡大縮小倍率.Y = 0.8f;
                        TJAPlayer3.Tx.Lane_Background_GoGo.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.nScrollFieldBGX[i], TJAPlayer3.Skin.nScrollFieldY[i] + 13);
                    }
                    else if (this.ctゴーゴー.n現在の値 >= 9)
                    {
                        TJAPlayer3.Tx.Lane_Background_GoGo.vc拡大縮小倍率.Y = 1.0f;
                        TJAPlayer3.Tx.Lane_Background_GoGo.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.nScrollFieldBGX[i], TJAPlayer3.Skin.nScrollFieldY[i]);
                    }
                }
                #endregion
            }
            
            for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                if (TJAPlayer3.stage演奏ドラム画面.bUseBranch[i] == true)
                {
                    if (TJAPlayer3.ConfigIni.nBranchAnime == 0)
                    {
                        if (!this.stBranch[i].ct分岐アニメ進行.b進行中)
                        {
                            switch (TJAPlayer3.stage演奏ドラム画面.nレーン用表示コース[i])
                            {
                                case CDTX.ECourse.eNormal:
                                    TJAPlayer3.Tx.Lane_Text[0].Opacity = 255;
                                    TJAPlayer3.Tx.Lane_Text[0].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                    break;
                                case CDTX.ECourse.eExpert:
                                    TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
                                    TJAPlayer3.Tx.Lane_Text[1].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                    break;
                                case CDTX.ECourse.eMaster:
                                    TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;
                                    TJAPlayer3.Tx.Lane_Text[2].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                    break;
                            }
                        }
                        if (this.stBranch[i].ct分岐アニメ進行.b進行中)
                        {
                            #region[ 普通譜面_レベルアップ ]
                            //普通→玄人
                            if (this.stBranch[i].nBefore == 0 && this.stBranch[i].nAfter == CDTX.ECourse.eExpert)
                            {
                                TJAPlayer3.Tx.Lane_Text[0].Opacity = 255;
                                TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
                                TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;

                                TJAPlayer3.Tx.Lane_Text[0].Opacity = this.stBranch[i].ct分岐アニメ進行.n現在の値 > 100 ? 0 : (255 - ((this.stBranch[i].ct分岐アニメ進行.n現在の値 * 0xff) / 60));
                                //CDTXMania.Tx.Lane_Text[1].n透明度 = this.ct分岐アニメ進行.n現在の値 > 100 ? 255 : ( ( ( this.ct分岐アニメ進行.n現在の値 * 0xff ) / 60 ) );
                                if (this.stBranch[i].ct分岐アニメ進行.n現在の値 < 60)
                                {
                                    this.stBranch[i].nY = this.stBranch[i].ct分岐アニメ進行.n現在の値 / 2;
                                    TJAPlayer3.Tx.Lane_Text[0].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i] + this.stBranch[i].nY);
                                    TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
                                    TJAPlayer3.Tx.Lane_Text[1].t2D描画(TJAPlayer3.app.Device, 333, (TJAPlayer3.Skin.nScrollFieldY[i] - 30) + this.stBranch[i].nY);
                                }
                                else
                                {
                                    TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
                                    TJAPlayer3.Tx.Lane_Text[1].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                }

                            }

                            //普通→達人
                            if (this.stBranch[i].nBefore == 0 && this.stBranch[i].nAfter == CDTX.ECourse.eMaster)
                            {
                                TJAPlayer3.Tx.Lane_Text[0].Opacity = 255;
                                TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
                                TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;
                                if (this.stBranch[i].ct分岐アニメ進行.n現在の値 < 60)
                                {
                                    this.stBranch[i].nY = this.stBranch[i].ct分岐アニメ進行.n現在の値 / 2;
                                    TJAPlayer3.Tx.Lane_Text[0].t2D描画(TJAPlayer3.app.Device, 333, (TJAPlayer3.Skin.nScrollFieldY[i] - 12) + this.stBranch[i].nY);
                                    TJAPlayer3.Tx.Lane_Text[0].Opacity = this.stBranch[i].ct分岐アニメ進行.n現在の値 > 100 ? 0 : (255 - ((this.stBranch[i].ct分岐アニメ進行.n現在の値 * 0xff) / 100));
                                    TJAPlayer3.Tx.Lane_Text[1].t2D描画(TJAPlayer3.app.Device, 333, (TJAPlayer3.Skin.nScrollFieldY[i] - 20) + this.stBranch[i].nY);
                                }
                                //if( this.stBranch[ i ].ct分岐アニメ進行.n現在の値 >= 5 && this.stBranch[ i ].ct分岐アニメ進行.n現在の値 < 60 )
                                //{
                                //    this.stBranch[ i ].nY = this.stBranch[ i ].ct分岐アニメ進行.n現在の値 / 2;
                                //    this.tx普通譜面[ 1 ].t2D描画(CDTXMania.app.Device, 333, CDTXMania.Skin.nScrollFieldY[ i ] + this.stBranch[ i ].nY);
                                //    this.tx普通譜面[ 1 ].n透明度 = this.stBranch[ i ].ct分岐アニメ進行.n現在の値 > 100 ? 0 : ( 255 - ( ( this.stBranch[ i ].ct分岐アニメ進行.n現在の値 * 0xff) / 100));
                                //    this.tx玄人譜面[ 1 ].t2D描画(CDTXMania.app.Device, 333, ( CDTXMania.Skin.nScrollFieldY[ i ] - 10 ) + this.stBranch[ i ].nY);
                                //}
                                else if (this.stBranch[i].ct分岐アニメ進行.n現在の値 >= 60 && this.stBranch[i].ct分岐アニメ進行.n現在の値 < 150)
                                {
                                    this.stBranch[i].nY = 21;
                                    TJAPlayer3.Tx.Lane_Text[1].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                    TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
                                    TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;
                                }
                                else if (this.stBranch[i].ct分岐アニメ進行.n現在の値 >= 150 && this.stBranch[i].ct分岐アニメ進行.n現在の値 < 210)
                                {
                                    this.stBranch[i].nY = ((this.stBranch[i].ct分岐アニメ進行.n現在の値 - 150) / 2);
                                    TJAPlayer3.Tx.Lane_Text[1].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i] + this.stBranch[i].nY);
                                    TJAPlayer3.Tx.Lane_Text[1].Opacity = this.stBranch[i].ct分岐アニメ進行.n現在の値 > 100 ? 0 : (255 - ((this.stBranch[i].ct分岐アニメ進行.n現在の値 * 0xff) / 100));
                                    TJAPlayer3.Tx.Lane_Text[2].t2D描画(TJAPlayer3.app.Device, 333, (TJAPlayer3.Skin.nScrollFieldY[i] - 20) + this.stBranch[i].nY);
                                }
                                else
                                {
                                    TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;
                                    TJAPlayer3.Tx.Lane_Text[2].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                }
                            }
                            #endregion
                            #region[ 玄人譜面_レベルアップ ]
                            //玄人→達人
                            if (this.stBranch[i].nBefore == CDTX.ECourse.eExpert && this.stBranch[i].nAfter == CDTX.ECourse.eMaster)
                            {
                                TJAPlayer3.Tx.Lane_Text[0].Opacity = 255;
                                TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
                                TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;

                                TJAPlayer3.Tx.Lane_Text[1].Opacity = this.stBranch[i].ct分岐アニメ進行.n現在の値 > 100 ? 0 : (255 - ((this.stBranch[i].ct分岐アニメ進行.n現在の値 * 0xff) / 60));
                                if (this.stBranch[i].ct分岐アニメ進行.n現在の値 < 60)
                                {
                                    this.stBranch[i].nY = this.stBranch[i].ct分岐アニメ進行.n現在の値 / 2;
                                    TJAPlayer3.Tx.Lane_Text[1].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i] + this.stBranch[i].nY);
                                    TJAPlayer3.Tx.Lane_Text[2].t2D描画(TJAPlayer3.app.Device, 333, (TJAPlayer3.Skin.nScrollFieldY[i] - 20) + this.stBranch[i].nY);
                                }
                                else
                                {
                                    TJAPlayer3.Tx.Lane_Text[2].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                }
                            }
                            #endregion
                            #region[ 玄人譜面_レベルダウン ]
                            if (this.stBranch[i].nBefore == CDTX.ECourse.eExpert && this.stBranch[i].nAfter == CDTX.ECourse.eNormal)
                            {
                                TJAPlayer3.Tx.Lane_Text[0].Opacity = 255;
                                TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
                                TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;

                                TJAPlayer3.Tx.Lane_Text[1].Opacity = this.stBranch[i].ct分岐アニメ進行.n現在の値 > 100 ? 0 : (255 - ((this.stBranch[i].ct分岐アニメ進行.n現在の値 * 0xff) / 60));
                                if (this.stBranch[i].ct分岐アニメ進行.n現在の値 < 60)
                                {
                                    this.stBranch[i].nY = this.stBranch[i].ct分岐アニメ進行.n現在の値 / 2;
                                    TJAPlayer3.Tx.Lane_Text[1].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i] - this.stBranch[i].nY);
                                    TJAPlayer3.Tx.Lane_Text[0].t2D描画(TJAPlayer3.app.Device, 333, (TJAPlayer3.Skin.nScrollFieldY[i] + 30) - this.stBranch[i].nY);
                                }
                                else
                                {
                                    TJAPlayer3.Tx.Lane_Text[0].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                }
                            }
                            #endregion
                            #region[ 達人譜面_レベルダウン ]
                            if (this.stBranch[i].nBefore == CDTX.ECourse.eMaster && this.stBranch[i].nAfter == CDTX.ECourse.eNormal)
                            {
                                TJAPlayer3.Tx.Lane_Text[0].Opacity = 255;
                                TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
                                TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;

                                if (this.stBranch[i].ct分岐アニメ進行.n現在の値 < 60)
                                {
                                    this.stBranch[i].nY = this.stBranch[i].ct分岐アニメ進行.n現在の値 / 2;
                                    TJAPlayer3.Tx.Lane_Text[2].Opacity = this.stBranch[i].ct分岐アニメ進行.n現在の値 > 100 ? 0 : (255 - ((this.stBranch[i].ct分岐アニメ進行.n現在の値 * 0xff) / 60));
                                    TJAPlayer3.Tx.Lane_Text[2].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i] - this.stBranch[i].nY);
                                    TJAPlayer3.Tx.Lane_Text[1].t2D描画(TJAPlayer3.app.Device, 333, (TJAPlayer3.Skin.nScrollFieldY[i] + 30) - this.stBranch[i].nY);
                                }
                                else if (this.stBranch[i].ct分岐アニメ進行.n現在の値 >= 60 && this.stBranch[i].ct分岐アニメ進行.n現在の値 < 150)
                                {
                                    this.stBranch[i].nY = 21;
                                    TJAPlayer3.Tx.Lane_Text[1].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                    TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
                                    TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;
                                }
                                else if (this.stBranch[i].ct分岐アニメ進行.n現在の値 >= 150 && this.stBranch[i].ct分岐アニメ進行.n現在の値 < 210)
                                {
                                    this.stBranch[i].nY = ((this.stBranch[i].ct分岐アニメ進行.n現在の値 - 150) / 2);
                                    TJAPlayer3.Tx.Lane_Text[1].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i] - this.stBranch[i].nY);
                                    TJAPlayer3.Tx.Lane_Text[1].Opacity = this.stBranch[i].ct分岐アニメ進行.n現在の値 > 100 ? 0 : (255 - ((this.stBranch[i].ct分岐アニメ進行.n現在の値 * 0xff) / 100));
                                    TJAPlayer3.Tx.Lane_Text[0].t2D描画(TJAPlayer3.app.Device, 333, (TJAPlayer3.Skin.nScrollFieldY[i] + 30) - this.stBranch[i].nY);
                                }
                                else if (this.stBranch[i].ct分岐アニメ進行.n現在の値 >= 210)
                                {
                                    TJAPlayer3.Tx.Lane_Text[0].Opacity = 255;
                                    TJAPlayer3.Tx.Lane_Text[0].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                }
                            }
                            if (this.stBranch[i].nBefore == CDTX.ECourse.eMaster && this.stBranch[i].nAfter == CDTX.ECourse.eExpert)
                            {
                                TJAPlayer3.Tx.Lane_Text[0].Opacity = 255;
                                TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
                                TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;

                                TJAPlayer3.Tx.Lane_Text[2].Opacity = this.stBranch[i].ct分岐アニメ進行.n現在の値 > 100 ? 0 : (255 - ((this.stBranch[i].ct分岐アニメ進行.n現在の値 * 0xff) / 60));
                                if (this.stBranch[i].ct分岐アニメ進行.n現在の値 < 60)
                                {
                                    this.stBranch[i].nY = this.stBranch[i].ct分岐アニメ進行.n現在の値 / 2;
                                    TJAPlayer3.Tx.Lane_Text[2].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i] - this.stBranch[i].nY);
                                    TJAPlayer3.Tx.Lane_Text[1].t2D描画(TJAPlayer3.app.Device, 333, (TJAPlayer3.Skin.nScrollFieldY[i] + 30) - this.stBranch[i].nY);
                                }
                                else
                                {
                                    TJAPlayer3.Tx.Lane_Text[1].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                }

                            }
                            #endregion
                        }
                    }
                    else
                    {
                        if (this.stBranch[i].nY座標 == 21)
                        {
                            this.stBranch[i].nY座標 = 0;
                        }

                        if (this.stBranch[i].nY座標 == 0)
                        {
                            switch (TJAPlayer3.stage演奏ドラム画面.nレーン用表示コース[i])
                            {
                                case CDTX.ECourse.eNormal:
                                    TJAPlayer3.Tx.Lane_Text[0].Opacity = 255;
                                    TJAPlayer3.Tx.Lane_Text[0].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                    break;
                                case CDTX.ECourse.eExpert:
                                    TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
                                    TJAPlayer3.Tx.Lane_Text[1].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                    break;
                                case CDTX.ECourse.eMaster:
                                    TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;
                                    TJAPlayer3.Tx.Lane_Text[2].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i]);
                                    break;
                            }
                        }

                        //fuck
                        if (this.stBranch[i].nY座標 != 0)
                        {
                            #region[ 普通譜面_レベルアップ ]
                            //普通→玄人
                            if (this.stBranch[i].nBefore == CDTX.ECourse.eNormal && this.stBranch[i].nAfter == CDTX.ECourse.eExpert)
                            {
                                TJAPlayer3.Tx.Lane_Text[0].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i] - this.stBranch[i].nY座標);
                                TJAPlayer3.Tx.Lane_Text[1].t2D描画(TJAPlayer3.app.Device, 333, (TJAPlayer3.Skin.nScrollFieldY[i] + 20) - this.stBranch[i].nY座標);
                                TJAPlayer3.Tx.Lane_Text[0].Opacity = this.stBranch[i].nBranchレイヤー透明度;
                            }
                            //普通→達人
                            if (this.stBranch[i].nBefore == CDTX.ECourse.eNormal && this.stBranch[i].nAfter == CDTX.ECourse.eMaster)
                            {
                                TJAPlayer3.Tx.Lane_Text[0].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i] - this.stBranch[i].nY座標);
                                TJAPlayer3.Tx.Lane_Text[2].t2D描画(TJAPlayer3.app.Device, 333, (TJAPlayer3.Skin.nScrollFieldY[i] + 20) - this.stBranch[i].nY座標);
                                TJAPlayer3.Tx.Lane_Text[0].Opacity = this.stBranch[i].nBranchレイヤー透明度;
                            }
                            #endregion
                            #region[ 玄人譜面_レベルアップ ]
                            //玄人→達人
                            if (this.stBranch[i].nBefore == CDTX.ECourse.eExpert && this.stBranch[i].nAfter == CDTX.ECourse.eMaster)
                            {
                                TJAPlayer3.Tx.Lane_Text[1].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i] - this.stBranch[i].nY座標);
                                TJAPlayer3.Tx.Lane_Text[2].t2D描画(TJAPlayer3.app.Device, 333, (TJAPlayer3.Skin.nScrollFieldY[i] + 20) - this.stBranch[i].nY座標);
                                TJAPlayer3.Tx.Lane_Text[1].Opacity = this.stBranch[i].nBranchレイヤー透明度;
                            }
                            #endregion
                            #region[ 玄人譜面_レベルダウン ]
                            if (this.stBranch[i].nBefore == CDTX.ECourse.eExpert && this.stBranch[i].nAfter == CDTX.ECourse.eNormal)
                            {
                                TJAPlayer3.Tx.Lane_Text[1].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i] + this.stBranch[i].nY座標);
								TJAPlayer3.Tx.Lane_Text[0].t2D描画(TJAPlayer3.app.Device, 333, (TJAPlayer3.Skin.nScrollFieldY[i] - 24) + this.stBranch[i].nY座標);
                                TJAPlayer3.Tx.Lane_Text[1].Opacity = this.stBranch[i].nBranchレイヤー透明度;
                            }
                            #endregion
                            #region[ 達人譜面_レベルダウン ]
                            if (this.stBranch[i].nBefore == CDTX.ECourse.eMaster && this.stBranch[i].nAfter == CDTX.ECourse.eNormal)
                            {
                                TJAPlayer3.Tx.Lane_Text[2].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i] + this.stBranch[i].nY座標);
                                TJAPlayer3.Tx.Lane_Text[0].t2D描画(TJAPlayer3.app.Device, 333, (TJAPlayer3.Skin.nScrollFieldY[i] - 24) + this.stBranch[i].nY座標);
                                TJAPlayer3.Tx.Lane_Text[2].Opacity = this.stBranch[i].nBranchレイヤー透明度;
                            }
                            if (this.stBranch[i].nBefore == CDTX.ECourse.eMaster && this.stBranch[i].nAfter == CDTX.ECourse.eExpert)
                            {
                                TJAPlayer3.Tx.Lane_Text[2].t2D描画(TJAPlayer3.app.Device, 333, TJAPlayer3.Skin.nScrollFieldY[i] + this.stBranch[i].nY座標);
                                TJAPlayer3.Tx.Lane_Text[1].t2D描画(TJAPlayer3.app.Device, 333, (TJAPlayer3.Skin.nScrollFieldY[i] - 24) + this.stBranch[i].nY座標);
                                TJAPlayer3.Tx.Lane_Text[2].Opacity = this.stBranch[i].nBranchレイヤー透明度;
                            }
                            #endregion
                        }
                    }

                }
            }



            if (TJAPlayer3.Tx.Lane_Background_Sub != null)
            {
                TJAPlayer3.Tx.Lane_Background_Sub.t2D描画(TJAPlayer3.app.Device, 333, 326);
                if (TJAPlayer3.stage演奏ドラム画面.bDoublePlay)
                {
                    TJAPlayer3.Tx.Lane_Background_Sub.t2D描画(TJAPlayer3.app.Device, 333, 502);
                }
            }


            TJAPlayer3.stage演奏ドラム画面.actTaikoLaneFlash.On進行描画();



            if (TJAPlayer3.Tx.Taiko_Frame[0] != null)
            {
                // Tower frame (without tamashii jauge) if playing a tower chart
                if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower && TJAPlayer3.Tx.Taiko_Frame[2] != null)
                    TJAPlayer3.Tx.Taiko_Frame[2].t2D描画(TJAPlayer3.app.Device, 329, 136);
                else
                    TJAPlayer3.Tx.Taiko_Frame[0].t2D描画(TJAPlayer3.app.Device, 329, 136);

                if (TJAPlayer3.stage演奏ドラム画面.bDoublePlay)
                    TJAPlayer3.Tx.Taiko_Frame[1]?.t2D描画(TJAPlayer3.app.Device, 329, 360);
            }
            var nTime = (long)(CSound管理.rc演奏用タイマ.n現在時刻 * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0));

            if (this.n総移動時間 != -1)
            {
                if (n移動方向 == 1)
                {
                    TJAPlayer3.Skin.nScrollFieldX[0] = this.n移動開始X + (int)((((int)nTime - this.n移動開始時刻) / (double)(this.n総移動時間)) * this.n移動距離px);
                    TJAPlayer3.stage演奏ドラム画面.FlyingNotes.StartPointX[0] = this.n移動開始X + (int)((((int)nTime - this.n移動開始時刻) / (double)(this.n総移動時間)) * this.n移動距離px);
                }
                else
                {
                    TJAPlayer3.Skin.nScrollFieldX[0] = this.n移動開始X - (int)((((int)nTime - this.n移動開始時刻) / (double)(this.n総移動時間)) * this.n移動距離px);
                    TJAPlayer3.stage演奏ドラム画面.FlyingNotes.StartPointX[0] = this.n移動開始X - (int)((((int)nTime - this.n移動開始時刻) / (double)(this.n総移動時間)) * this.n移動距離px);
                }

                if (((int)nTime) > this.n移動開始時刻 + this.n総移動時間)
                {
                    this.n総移動時間 = -1;
                    TJAPlayer3.Skin.nScrollFieldX[0] = this.n移動目的場所X;
                    TJAPlayer3.stage演奏ドラム画面.FlyingNotes.StartPointX[0] = this.n移動目的場所X;
                }
            }
            if (this.n総移動時間2 != -1)
            {
                if (n移動方向2 == 1)
                {
                    TJAPlayer3.Skin.nScrollFieldX[1] = this.n移動開始X2 + (int)((((int)nTime - this.n移動開始時刻2) / (double)(this.n総移動時間2)) * this.n移動距離px2);
                    TJAPlayer3.Skin.Game_Effect_FlyingNotes_StartPoint_X[1] = this.n移動開始X2 + (int)((((int)nTime - this.n移動開始時刻2) / (double)(this.n総移動時間2)) * this.n移動距離px2);
                }
                else
                {
                    TJAPlayer3.Skin.nScrollFieldX[1] = this.n移動開始X2 - (int)((((int)nTime - this.n移動開始時刻2) / (double)(this.n総移動時間2)) * this.n移動距離px2);
                    TJAPlayer3.Skin.Game_Effect_FlyingNotes_StartPoint_X[1] = this.n移動開始X2 - (int)((((int)nTime - this.n移動開始時刻2) / (double)(this.n総移動時間2)) * this.n移動距離px2);
                }


                if (((int)nTime) > this.n移動開始時刻2 + this.n総移動時間2)
                {
                    this.n総移動時間2 = -1;
                    TJAPlayer3.Skin.nScrollFieldX[1] = this.n移動目的場所X2;
                    TJAPlayer3.stage演奏ドラム画面.FlyingNotes.StartPointX[1] = this.n移動目的場所X2;
                }
            }




            if (TJAPlayer3.ConfigIni.bAVI有効 && TJAPlayer3.DTX.listAVI.Count > 0)
            {
                if (TJAPlayer3.Tx.Lane_Background_Main != null) TJAPlayer3.Tx.Lane_Background_Main.Opacity = TJAPlayer3.ConfigIni.nBGAlpha;
                if(TJAPlayer3.Tx.Lane_Background_Sub != null) TJAPlayer3.Tx.Lane_Background_Sub.Opacity = TJAPlayer3.ConfigIni.nBGAlpha;
                if (TJAPlayer3.Tx.Lane_Background_GoGo != null) TJAPlayer3.Tx.Lane_Background_GoGo.Opacity = TJAPlayer3.ConfigIni.nBGAlpha;
            }
            else
            {
                if (TJAPlayer3.Tx.Lane_Background_Main != null) TJAPlayer3.Tx.Lane_Background_Main.Opacity = 255;
                if (TJAPlayer3.Tx.Lane_Background_Sub != null) TJAPlayer3.Tx.Lane_Background_Sub.Opacity = 255;
                if (TJAPlayer3.Tx.Lane_Background_GoGo != null) TJAPlayer3.Tx.Lane_Background_GoGo.Opacity = 255;
            }

            //CDTXMania.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.白, this.nBranchレイヤー透明度.ToString());
            //CDTXMania.act文字コンソール.tPrint(0, 16, C文字コンソール.Eフォント種別.白, this.ct分岐アニメ進行.n現在の値.ToString());
            //CDTXMania.act文字コンソール.tPrint(0, 32, C文字コンソール.Eフォント種別.白, this.ct分岐アニメ進行.n終了値.ToString());

            //CDTXMania.act文字コンソール.tPrint(0, 32, C文字コンソール.Eフォント種別.白, this.ctゴーゴースプラッシュ.n現在の値.ToString());

            /*#region[ ゴーゴースプラッシュ ]
            for (int i = 0; i < CDTXMania.ConfigIni.nPlayerCount; i++)
            {
                if (CDTXMania.stage演奏ドラム画面.bIsGOGOTIME[i])
                {

                    if (this.txゴーゴースプラッシュ != null)
                    {
                        this.txゴーゴースプラッシュ[(int)this.ctゴーゴースプラッシュ.n現在の値].t2D描画(CDTXMania.app.Device, 0, 260);
                        this.ctゴーゴースプラッシュ.n現在の値++;
                        if(this.ctゴーゴースプラッシュ.b終了値に達した)
                        {
                            this.ctゴーゴースプラッシュ.t停止();
                            this.ctゴーゴースプラッシュ.n現在の値 = 0;
                        }
                    }
                    
         
                    this.ctゴーゴースプラッシュ.t進行Loop();
                if (this.txゴーゴースプラッシュ != null)
                {
                    if (this.ctゴーゴースプラッシュ.b終了値に達してない)
                    {
                        this.txゴーゴースプラッシュ[(int)this.ctゴーゴースプラッシュ.n現在の値].t2D描画(CDTXMania.app.Device, 0, 260);
                    }

                }
                }
            }
            #endregion */
            /*
            for (int i = 0; i < CDTXMania.ConfigIni.nPlayerCount; i++)
            {
                #region[ ゴーゴースプラッシュ ]
                if (this.txゴーゴースプラッシュ != null && CDTXMania.stage演奏ドラム画面.bIsGOGOTIME[i])
                {
                    if (!this.ctゴーゴースプラッシュ.b停止中)
                    {
                        this.ctゴーゴースプラッシュ.t進行();
                    }
                    if (this.ctゴーゴースプラッシュ.n現在の値 < 28)
                    {
                        for (int v = 0; v < 6; v++)
                        {
                            this.txゴーゴースプラッシュ[this.ctゴーゴースプラッシュ.n現在の値].t2D描画(CDTXMania.app.Device, 0 + (v * 213), 260);
                        }

                    }
                    else
                    {
                        this.txゴーゴースプラッシュ[this.ctゴーゴースプラッシュ.n現在の値].n透明度 = 100;
                    }

                }
                #endregion
            } */
            return base.On進行描画();
        }

        public void ゴーゴー炎()
        {
            //判定枠
            if (TJAPlayer3.Tx.Notes != null)
            {
                int nJudgeX = TJAPlayer3.Skin.nScrollFieldX[0] - (130 / 2); //元の値は349なんだけど...
                int nJudgeY = TJAPlayer3.Skin.nScrollFieldY[0]; //元の値は349なんだけど...
                TJAPlayer3.Tx.Judge_Frame.b加算合成 = TJAPlayer3.Skin.Game_JudgeFrame_AddBlend;
                TJAPlayer3.Tx.Judge_Frame.t2D描画(TJAPlayer3.app.Device, nJudgeX, nJudgeY, new Rectangle(0, 0, 130, 130));

                if (TJAPlayer3.stage演奏ドラム画面.bDoublePlay)
                    TJAPlayer3.Tx.Judge_Frame.t2D描画(TJAPlayer3.app.Device, nJudgeX, nJudgeY + 176, new Rectangle(0, 0, 130, 130));
            }


            #region[ ゴーゴー炎 ]
            for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                if (TJAPlayer3.stage演奏ドラム画面.bIsGOGOTIME[i])
                {
                    this.ctゴーゴー炎.t進行Loop();

                    if (TJAPlayer3.Tx.Effects_Fire != null)
                    {
                        float f倍率 = 1.0f;

                        float[] ar倍率 = new float[] { 0.8f, 1.2f, 1.7f, 2.5f, 2.3f, 2.2f, 2.0f, 1.8f, 1.7f, 1.6f, 1.6f, 1.5f, 1.5f, 1.4f, 1.3f, 1.2f, 1.1f, 1.0f };

                        f倍率 = ar倍率[this.ctゴーゴー.n現在の値];

                        Matrix mat = Matrix.Identity;
                        mat *= Matrix.Scaling(f倍率, f倍率, 1.0f);
                        mat *= Matrix.Translation(TJAPlayer3.Skin.nScrollFieldX[i] - SampleFramework.GameWindowSize.Width / 2.0f, -(TJAPlayer3.Skin.nJudgePointY[i] - SampleFramework.GameWindowSize.Height / 2.0f), 0f);

                        //this.txゴーゴー炎.b加算合成 = true;

                        //this.ctゴーゴー.n現在の値 = 6;
                        if (this.ctゴーゴー.b終了値に達した)
                        {
                            TJAPlayer3.Tx.Effects_Fire.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.nScrollFieldX[i] - 180, TJAPlayer3.Skin.nJudgePointY[i] - (TJAPlayer3.Tx.Effects_Fire.szテクスチャサイズ.Height / 2), new Rectangle(360 * (this.ctゴーゴー炎.n現在の値), 0, 360, 370));
                        }
                        else
                        {
                            TJAPlayer3.Tx.Effects_Fire.t3D描画(TJAPlayer3.app.Device, mat, new Rectangle(360 * (this.ctゴーゴー炎.n現在の値), 0, 360, 370));
                        }
                    }
                }
            }
            #endregion
            for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                if (!this.st状態[i].ct進行.b停止中)
                {
                    this.st状態[i].ct進行.t進行();
                    if (this.st状態[i].ct進行.b終了値に達した)
                    {
                        this.st状態[i].ct進行.t停止();
                    }
                    //if( this.txアタックエフェクトLower != null )
                    {
                        //this.txアタックエフェクトLower.b加算合成 = true;
                        int n = this.st状態[i].nIsBig == 1 ? 520 : 0;

                        switch (st状態[i].judge)
                        {
                            case E判定.Perfect:
                            case E判定.Great:
                            case E判定.Auto:
                                //this.txアタックエフェクトLower.t2D描画( CDTXMania.app.Device, 285, 127, new Rectangle( this.st状態[ i ].ct進行.n現在の値 * 260, n, 260, 260 ) );
                                if (this.st状態[i].nIsBig == 1 && TJAPlayer3.Tx.Effects_Hit_Great_Big[this.st状態[i].ct進行.n現在の値] != null)
                                    TJAPlayer3.Tx.Effects_Hit_Great_Big[this.st状態[i].ct進行.n現在の値].t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.nScrollFieldX[0] - TJAPlayer3.Tx.Effects_Hit_Great_Big[0].szテクスチャサイズ.Width / 2, TJAPlayer3.Skin.nJudgePointY[i] - TJAPlayer3.Tx.Effects_Hit_Great_Big[0].szテクスチャサイズ.Width / 2);
                                else if (TJAPlayer3.Tx.Effects_Hit_Great[this.st状態[i].ct進行.n現在の値] != null)
                                    TJAPlayer3.Tx.Effects_Hit_Great[this.st状態[i].ct進行.n現在の値].t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.nScrollFieldX[0] - TJAPlayer3.Tx.Effects_Hit_Great[0].szテクスチャサイズ.Width / 2, TJAPlayer3.Skin.nJudgePointY[i] - TJAPlayer3.Tx.Effects_Hit_Great[0].szテクスチャサイズ.Width / 2);
                                break;

                            case E判定.Good:
                                //this.txアタックエフェクトLower.t2D描画( CDTXMania.app.Device, 285, 127, new Rectangle( this.st状態[ i ].ct進行.n現在の値 * 260, n + 260, 260, 260 ) );
                                if (this.st状態[i].nIsBig == 1 && TJAPlayer3.Tx.Effects_Hit_Good_Big[this.st状態[i].ct進行.n現在の値] != null)
                                    TJAPlayer3.Tx.Effects_Hit_Good_Big[this.st状態[i].ct進行.n現在の値].t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.nScrollFieldX[0] - TJAPlayer3.Tx.Effects_Hit_Good_Big[0].szテクスチャサイズ.Width / 2, TJAPlayer3.Skin.nJudgePointY[i] - TJAPlayer3.Tx.Effects_Hit_Good_Big[0].szテクスチャサイズ.Width / 2);
                                else if (TJAPlayer3.Tx.Effects_Hit_Good[this.st状態[i].ct進行.n現在の値] != null)
                                    TJAPlayer3.Tx.Effects_Hit_Good[this.st状態[i].ct進行.n現在の値].t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.nScrollFieldX[0] - TJAPlayer3.Tx.Effects_Hit_Good[0].szテクスチャサイズ.Width / 2, TJAPlayer3.Skin.nJudgePointY[i] - TJAPlayer3.Tx.Effects_Hit_Good[0].szテクスチャサイズ.Width / 2);
                                break;

                            case E判定.Miss:
                            case E判定.Bad:
                                break;
                        }
                    }
                }
            }


        }

        public virtual void Start(int nLane, E判定 judge, bool b両手入力, int nPlayer)
        {
            //2017.08.15 kairera0467 排他なので番地をそのまま各レーンの状態として扱う

            //for( int n = 0; n < 1; n++ )
            {
                this.st状態[nPlayer].ct進行 = new CCounter(0, 14, 20, TJAPlayer3.Timer);
                this.st状態[nPlayer].judge = judge;
                this.st状態[nPlayer].nPlayer = nPlayer;

                switch (nLane)
                {
                    case 0x11:
                    case 0x12:
                        this.st状態[nPlayer].nIsBig = 0;
                        break;
                    case 0x13:
                    case 0x14:
                    case 0x1A:
                    case 0x1B:
                        {
                            if (b両手入力)
                                this.st状態[nPlayer].nIsBig = 1;
                            else
                                this.st状態[nPlayer].nIsBig = 0;
                        }
                        break;
                }
            }
        }


        public void GOGOSTART()
        {
            this.ctゴーゴー = new CCounter(0, 17, 18, TJAPlayer3.Timer);
            if(TJAPlayer3.ConfigIni.nPlayerCount == 1 && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan) TJAPlayer3.stage演奏ドラム画面.GoGoSplash.StartSplash();
        }


        public void t分岐レイヤー_コース変化(CDTX.ECourse n現在, CDTX.ECourse n次回, int nPlayer)
        {
            if (n現在 == n次回)
            {
                return;
            }
            this.stBranch[nPlayer].ct分岐アニメ進行 = new CCounter(0, 300, 2, TJAPlayer3.Timer);

            this.stBranch[nPlayer].nBranchレイヤー透明度 = 6;
            this.stBranch[nPlayer].nY座標 = 1;

            this.stBranch[nPlayer].nBefore = n現在;
            this.stBranch[nPlayer].nAfter = n次回;

            TJAPlayer3.stage演奏ドラム画面.actLane.t分岐レイヤー_コース変化(n現在, n次回, nPlayer);
        }

        public void t判定枠移動(double db移動時間, int n移動px, int n移動方向)
        {
            this.n移動開始時刻 = (int)(CSound管理.rc演奏用タイマ.n現在時刻 * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0));
            this.n移動開始X = TJAPlayer3.Skin.nScrollFieldX[0];
            this.n総移動時間 = (int)(db移動時間 * 1000);
            this.n移動方向 = n移動方向;
            this.n移動距離px = n移動px;
            if (n移動方向 == 0)
                this.n移動目的場所X = TJAPlayer3.Skin.nScrollFieldX[0] - n移動px;
            else
                this.n移動目的場所X = TJAPlayer3.Skin.nScrollFieldX[0] + n移動px;
        }

        public void t判定枠移動2(double db移動時間, int n移動px, int n移動方向)
        {
            this.n移動開始時刻2 = (int)(CSound管理.rc演奏用タイマ.n現在時刻 * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0));
            this.n移動開始X2 = TJAPlayer3.Skin.nScrollFieldX[1];
            this.n総移動時間2 = (int)(db移動時間 * 1000);
            this.n移動方向2 = n移動方向;
            this.n移動距離px2 = n移動px;
            if (n移動方向 == 0)
                this.n移動目的場所X2 = TJAPlayer3.Skin.nScrollFieldX[1] - n移動px;
            else
                this.n移動目的場所X2 = TJAPlayer3.Skin.nScrollFieldX[1] + n移動px;
        }
        #region[ private ]
        //-----------------
        //private CTexture txLane;
        //private CTexture txLaneB;
        //private CTexture tx枠線;
        //private CTexture tx判定枠;
        //private CTexture txゴーゴー;
        //private CTexture txゴーゴー炎;
        //private CTexture[] txArゴーゴー炎;
        //private CTexture[] txArアタックエフェクトLower_A;
        //private CTexture[] txArアタックエフェクトLower_B;
        //private CTexture[] txArアタックエフェクトLower_C;
        //private CTexture[] txArアタックエフェクトLower_D;

        //private CTexture[] txLaneFlush = new CTexture[3];

        //private CTexture[] tx普通譜面 = new CTexture[2];
        //private CTexture[] tx玄人譜面 = new CTexture[2];
        //private CTexture[] tx達人譜面 = new CTexture[2];

        //private CTextureAf txアタックエフェクトLower;

        protected STSTATUS[] st状態 = new STSTATUS[4];

        //private CTexture[] txゴーゴースプラッシュ;

        [StructLayout(LayoutKind.Sequential)]
        protected struct STSTATUS
        {
            public bool b使用中;
            public CCounter ct進行;
            public E判定 judge;
            public int nIsBig;
            public int n透明度;
            public int nPlayer;
        }
        private CCounter ctゴーゴー;
        private CCounter ctゴーゴー炎;



        public STBRANCH[] stBranch = new STBRANCH[4];
        [StructLayout(LayoutKind.Sequential)]
        public struct STBRANCH
        {
            public CCounter ct分岐アニメ進行;
            public CDTX.ECourse nBefore;
            public CDTX.ECourse nAfter;

            public long nフラッシュ制御タイマ;
            public int nBranchレイヤー透明度;
            public int nBranch文字透明度;
            public int nY座標;
            public int nY;
        }


        private int n総移動時間;
        private int n移動開始X;
        private int n移動開始時刻;
        private int n移動距離px;
        private int n移動目的場所X;
        private int n移動方向;
        private int n総移動時間2;
        private int n移動開始X2;
        private int n移動開始時刻2;
        private int n移動距離px2;
        private int n移動目的場所X2;
        private int n移動方向2;

        internal int[,] nDefaultJudgePos = new int[2, 2];


        //-----------------
        #endregion
    }
}
