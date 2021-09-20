﻿using System;
using FDK;

namespace TJAPlayer3
{
    internal class CAct演奏Drums背景 : CActivity
    {
        // 本家っぽい背景を表示させるメソッド。
        //
        // 拡張性とかないんで。はい、ヨロシクゥ!
        //
        public CAct演奏Drums背景()
        {
            base.b活性化してない = true;
        }

        public void tFadeIn(int player)
        {
            this.ct上背景クリアインタイマー[player] = new CCounter(0, 100, 2, TJAPlayer3.Timer);
            this.eFadeMode = EFIFOモード.フェードイン;
        }

        //public void tFadeOut(int player)
        //{
        //    this.ct上背景フェードタイマー[player] = new CCounter( 0, 100, 6, CDTXMania.Timer );
        //    this.eFadeMode = EFIFOモード.フェードアウト;
        //}

        public void ClearIn(int player)
        {
            this.ct上背景クリアインタイマー[player] = new CCounter(0, 100, 2, TJAPlayer3.Timer);
            this.ct上背景クリアインタイマー[player].n現在の値 = 0;
            this.ct上背景FIFOタイマー = new CCounter(0, 100, 2, TJAPlayer3.Timer);
            this.ct上背景FIFOタイマー.n現在の値 = 0;
        }

        public override void On活性化()
        {
            base.On活性化();
        }

        public override void On非活性化()
        {
            TJAPlayer3.t安全にDisposeする(ref this.ct上背景FIFOタイマー);
            for (int i = 0; i < 2; i++)
            {
                ct上背景スクロール用タイマー1st[i] = null;
                ct上背景スクロール用タイマー2nd[i] = null;
                ct上背景スクロール用タイマー3rd[i] = null;
            }
            TJAPlayer3.t安全にDisposeする(ref this.ct下背景スクロール用タイマー1);
            base.On非活性化();
        }

        public override void OnManagedリソースの作成()
        {
            this.ct上背景スクロール用タイマー1st = new CCounter[2];
            this.ct上背景スクロール用タイマー2nd = new CCounter[2];
            this.ct上背景スクロール用タイマー3rd = new CCounter[2];
            this.ct上背景クリアインタイマー = new CCounter[2];
            ct上背景スクロール用タイマー1stDan = new CCounter[4];

            for (int i = 0; i < 2; i++)
            {
                if (TJAPlayer3.Tx.Background_Up_3rd[i] != null)
                {
                    this.ct上背景スクロール用タイマー1st[i] = new CCounter(1, TJAPlayer3.Tx.Background_Up_1st[i].szテクスチャサイズ.Width, 16, TJAPlayer3.Timer);
                    this.ct上背景スクロール用タイマー2nd[i] = new CCounter(1, TJAPlayer3.Tx.Background_Up_2nd[i].szテクスチャサイズ.Height, 70, TJAPlayer3.Timer);
                    this.ct上背景スクロール用タイマー3rd[i] = new CCounter(1, 600, 3, TJAPlayer3.Timer);
                    this.ct上背景クリアインタイマー[i] = new CCounter();
                }
            }

            this.ct上背景スクロール用タイマー1stDan[0] = new CCounter(0, TJAPlayer3.Tx.Background_Up_Dan[1].szテクスチャサイズ.Width, 8.453f * 2, TJAPlayer3.Timer);
            this.ct上背景スクロール用タイマー1stDan[1] = new CCounter(0, TJAPlayer3.Tx.Background_Up_Dan[2].szテクスチャサイズ.Width, 10.885f * 2, TJAPlayer3.Timer);
            this.ct上背景スクロール用タイマー1stDan[2] = new CCounter(0, TJAPlayer3.Tx.Background_Up_Dan[3].szテクスチャサイズ.Width, 11.4f * 2, TJAPlayer3.Timer);
            this.ct上背景スクロール用タイマー1stDan[3] = new CCounter(0, TJAPlayer3.Tx.Background_Up_Dan[5].szテクスチャサイズ.Width, 33.88f, TJAPlayer3.Timer);
            this.ct上背景スクロール用タイマー2stDan = new CCounter(0, TJAPlayer3.Tx.Background_Up_Dan[4].szテクスチャサイズ.Width + 200, 10, TJAPlayer3.Timer);

            if (TJAPlayer3.Tx.Background_Down_Scroll != null)
                this.ct下背景スクロール用タイマー1 = new CCounter(1, TJAPlayer3.Tx.Background_Down_Scroll.szテクスチャサイズ.Width, 4, TJAPlayer3.Timer);

            this.ct上背景FIFOタイマー = new CCounter();
            base.OnManagedリソースの作成();
        }

        public override void OnManagedリソースの解放()
        {
            //CDTXMania.tテクスチャの解放( ref this.tx上背景メイン );
            //CDTXMania.tテクスチャの解放( ref this.tx上背景クリアメイン );
            //CDTXMania.tテクスチャの解放( ref this.tx下背景メイン );
            //CDTXMania.tテクスチャの解放( ref this.tx下背景クリアメイン );
            //CDTXMania.tテクスチャの解放( ref this.tx下背景クリアサブ1 );
            //Trace.TraceInformation("CActDrums背景 リソースの開放");
            base.OnManagedリソースの解放();
        }

        public override int On進行描画()
        {
            this.ct上背景FIFOタイマー.t進行();

            for (int i = 0; i < 2; i++)
            {
                if (this.ct上背景クリアインタイマー[i] != null)
                    this.ct上背景クリアインタイマー[i].t進行();
            }
            for (int i = 0; i < 2; i++)
            {
                if (this.ct上背景スクロール用タイマー1st[i] != null)
                    this.ct上背景スクロール用タイマー1st[i].t進行Loop();
            }
            for (int i = 0; i < 2; i++)
            {
                if (this.ct上背景スクロール用タイマー2nd[i] != null)
                    this.ct上背景スクロール用タイマー2nd[i].t進行Loop();
            }
            for (int i = 0; i < 2; i++)
            {
                if (this.ct上背景スクロール用タイマー3rd[i] != null)
                    this.ct上背景スクロール用タイマー3rd[i].t進行Loop();
            }
            for (int i = 0; i < 4; i++)
            {
                if (this.ct上背景スクロール用タイマー1stDan[i] != null)
                    this.ct上背景スクロール用タイマー1stDan[i].t進行Loop();
            }
            if (this.ct下背景スクロール用タイマー1 != null)
                this.ct下背景スクロール用タイマー1.t進行Loop();

            if (this.ct上背景スクロール用タイマー2stDan != null)
                this.ct上背景スクロール用タイマー2stDan.t進行Loop();

            #region 1P-2P-上背景

            if(TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan)
            {
                #region [ 通常背景 ]

            for (int i = 0; i < 2; i++)
            {
                if (this.ct上背景スクロール用タイマー1st[i] != null)
                {
                    double TexSizeL = 1280 / TJAPlayer3.Tx.Background_Up_1st[i].szテクスチャサイズ.Width;
                    double TexSizeW = 308 / TJAPlayer3.Tx.Background_Up_2nd[i].szテクスチャサイズ.Height;
                    double TexSizeF = 1280 / TJAPlayer3.Tx.Background_Up_3rd[i].szテクスチャサイズ.Width;
                    // 1280をテクスチャサイズで割ったものを切り上げて、プラス+1足す。
                    int ForLoopL = (int)Math.Ceiling(TexSizeL) + 1;
                    int ForLoopW = (int)Math.Ceiling(TexSizeW) + 1;
                    int ForLoopF = (int)Math.Ceiling(TexSizeF) + 1;
                    //int nループ幅 = 328;

                    #region [ 上背景-Back ]

                    for (int W = 1; W < ForLoopW + 1; W++)
                    {
                        TJAPlayer3.Tx.Background_Up_1st[i].t2D描画(TJAPlayer3.app.Device, 0 - this.ct上背景スクロール用タイマー1st[i].n現在の値, (185 + i * 600) - (W * TJAPlayer3.Tx.Background_Up_1st[i].szテクスチャサイズ.Height) + ct上背景スクロール用タイマー2nd[i].n現在の値);
                    }
                    for (int l = 1; l < ForLoopL + 1; l++)
                    {
                        for (int W = 1; W < ForLoopW + 1; W++)
                        {
                            TJAPlayer3.Tx.Background_Up_1st[i].t2D描画(TJAPlayer3.app.Device, +(l * TJAPlayer3.Tx.Background_Up_1st[i].szテクスチャサイズ.Width) - this.ct上背景スクロール用タイマー1st[i].n現在の値, (185 + i * 600) - (W * TJAPlayer3.Tx.Background_Up_1st[i].szテクスチャサイズ.Height) + ct上背景スクロール用タイマー2nd[i].n現在の値);
                        }
                    }

                    for (int W = 1; W < ForLoopW + 1; W++)
                    {
                        TJAPlayer3.Tx.Background_Up_2nd[i].t2D描画(TJAPlayer3.app.Device, 0 - this.ct上背景スクロール用タイマー1st[i].n現在の値, (370 + i * 600) - (W * TJAPlayer3.Tx.Background_Up_2nd[i].szテクスチャサイズ.Height) - ct上背景スクロール用タイマー2nd[i].n現在の値);
                    }
                    for (int l = 1; l < ForLoopL + 1; l++)
                    {
                        for (int W = 1; W < ForLoopW + 1; W++)
                        {
                            TJAPlayer3.Tx.Background_Up_2nd[i].t2D描画(TJAPlayer3.app.Device, +(l * TJAPlayer3.Tx.Background_Up_2nd[i].szテクスチャサイズ.Width) - this.ct上背景スクロール用タイマー1st[i].n現在の値, (370 + i * 600) - (W * TJAPlayer3.Tx.Background_Up_2nd[i].szテクスチャサイズ.Height) - ct上背景スクロール用タイマー2nd[i].n現在の値);
                        }
                    }

                    #endregion

                    #region [ 上背景-Front ]

                    float thirdy = 0;
                    float thirdx = 0;

                    if (this.ct上背景スクロール用タイマー3rd[i].n現在の値 <= 270)
                    {
                        thirdx = this.ct上背景スクロール用タイマー3rd[i].n現在の値 * 0.9258f;
                        thirdy = (float)Math.Sin((float)this.ct上背景スクロール用タイマー3rd[i].n現在の値 * (Math.PI / 270.0f)) * 40.0f;
                    }
                    else
                    {
                        thirdx = 250 + (ct上背景スクロール用タイマー3rd[i].n現在の値 - 270) * 0.24f;

                        if (this.ct上背景スクロール用タイマー3rd[i].n現在の値 <= 490) thirdy = -(float)Math.Sin((float)(this.ct上背景スクロール用タイマー3rd[i].n現在の値 - 270) * (Math.PI / 170.0f)) * 15.0f;
                        else thirdy = -((float)Math.Sin((float)220f * (Math.PI / 170.0f)) * 15.0f) + (float)(((this.ct上背景スクロール用タイマー3rd[i].n現在の値 - 490) / 110f) * ((float)Math.Sin((float)220f * (Math.PI / 170.0f)) * 15.0f));
                    }

                    TJAPlayer3.Tx.Background_Up_3rd[i].t2D描画(TJAPlayer3.app.Device, 0 - thirdx, 0 + i * 540 - thirdy);

                    for (int l = 1; l < ForLoopF + 1; l++)
                    {
                        TJAPlayer3.Tx.Background_Up_3rd[i].t2D描画(TJAPlayer3.app.Device, +(l * TJAPlayer3.Tx.Background_Up_3rd[i].szテクスチャサイズ.Width) - thirdx, 0 + i * 540 - thirdy);
                    }

                    #endregion
                }

                if (this.ct上背景スクロール用タイマー1st[i] != null)
                {
                    if (TJAPlayer3.stage演奏ドラム画面.bIsAlreadyCleared[i])
                    {
                        TJAPlayer3.Tx.Background_Up_1st[2].Opacity = ((this.ct上背景クリアインタイマー[i].n現在の値 * 0xff) / 100);
                        TJAPlayer3.Tx.Background_Up_2nd[2].Opacity = ((this.ct上背景クリアインタイマー[i].n現在の値 * 0xff) / 100);
                        TJAPlayer3.Tx.Background_Up_3rd[2].Opacity = ((this.ct上背景クリアインタイマー[i].n現在の値 * 0xff) / 100);
                    }
                    else
                    {
                        TJAPlayer3.Tx.Background_Up_1st[2].Opacity = 0;
                        TJAPlayer3.Tx.Background_Up_2nd[2].Opacity = 0;
                        TJAPlayer3.Tx.Background_Up_3rd[2].Opacity = 0;
                    }

                    double TexSizeL = 1280 / TJAPlayer3.Tx.Background_Up_1st[2].szテクスチャサイズ.Width;
                    double TexSizeW = 308 / TJAPlayer3.Tx.Background_Up_2nd[2].szテクスチャサイズ.Height;
                    double TexSizeF = 1280 / TJAPlayer3.Tx.Background_Up_3rd[2].szテクスチャサイズ.Width;
                    // 1280をテクスチャサイズで割ったものを切り上げて、プラス+1足す。
                    int ForLoopL = (int)Math.Ceiling(TexSizeL) + 1;
                    int ForLoopW = (int)Math.Ceiling(TexSizeW) + 1;
                    int ForLoopF = (int)Math.Ceiling(TexSizeF) + 1;

                    #region [ 上背景-Back ]

                    for (int W = 1; W < ForLoopW + 1; W++)
                    {
                        TJAPlayer3.Tx.Background_Up_1st[2].t2D描画(TJAPlayer3.app.Device, 0 - this.ct上背景スクロール用タイマー1st[i].n現在の値, (185 + i * 600) - (W * TJAPlayer3.Tx.Background_Up_1st[2].szテクスチャサイズ.Height) + ct上背景スクロール用タイマー2nd[i].n現在の値);
                    }
                    for (int l = 1; l < ForLoopL + 1; l++)
                    {
                        for (int W = 1; W < ForLoopW + 1; W++)
                        {
                            TJAPlayer3.Tx.Background_Up_1st[2].t2D描画(TJAPlayer3.app.Device, +(l * TJAPlayer3.Tx.Background_Up_1st[2].szテクスチャサイズ.Width) - this.ct上背景スクロール用タイマー1st[i].n現在の値, (185 + i * 600) - (W * TJAPlayer3.Tx.Background_Up_1st[2].szテクスチャサイズ.Height) + ct上背景スクロール用タイマー2nd[i].n現在の値);
                        }
                    }

                    for (int W = 1; W < ForLoopW + 1; W++)
                    {
                        TJAPlayer3.Tx.Background_Up_2nd[2].t2D描画(TJAPlayer3.app.Device, 0 - this.ct上背景スクロール用タイマー1st[i].n現在の値, (370 + i * 600) - (W * TJAPlayer3.Tx.Background_Up_2nd[2].szテクスチャサイズ.Height) - ct上背景スクロール用タイマー2nd[i].n現在の値);
                    }
                    for (int l = 1; l < ForLoopL + 1; l++)
                    {
                        for (int W = 1; W < ForLoopW + 1; W++)
                        {
                            TJAPlayer3.Tx.Background_Up_2nd[2].t2D描画(TJAPlayer3.app.Device, +(l * TJAPlayer3.Tx.Background_Up_2nd[2].szテクスチャサイズ.Width) - this.ct上背景スクロール用タイマー1st[i].n現在の値, (370 + i * 600) - (W * TJAPlayer3.Tx.Background_Up_2nd[2].szテクスチャサイズ.Height) - ct上背景スクロール用タイマー2nd[i].n現在の値);
                        }
                    }

                    #endregion

                    #region [ 上背景-Front ]

                    float thirdy = 0;
                    float thirdx = 0;

                    if (this.ct上背景スクロール用タイマー3rd[i].n現在の値 <= 270)
                    {
                        thirdx = this.ct上背景スクロール用タイマー3rd[i].n現在の値 * 0.9258f;
                        thirdy = (float)Math.Sin((float)this.ct上背景スクロール用タイマー3rd[i].n現在の値 * (Math.PI / 270.0f)) * 40.0f;
                    }
                    else
                    {
                        thirdx = 250 + (ct上背景スクロール用タイマー3rd[i].n現在の値 - 270) * 0.24f;

                        if (this.ct上背景スクロール用タイマー3rd[i].n現在の値 <= 490) thirdy = -(float)Math.Sin((float)(this.ct上背景スクロール用タイマー3rd[i].n現在の値 - 270) * (Math.PI / 170.0f)) * 15.0f;
                        else thirdy = -((float)Math.Sin((float)220f * (Math.PI / 170.0f)) * 15.0f) + (float)(((this.ct上背景スクロール用タイマー3rd[i].n現在の値 - 490) / 110f) * ((float)Math.Sin((float)220f * (Math.PI / 170.0f)) * 15.0f));
                    }

                    TJAPlayer3.Tx.Background_Up_3rd[2].t2D描画(TJAPlayer3.app.Device, 0 - thirdx, 0 + i * 540 - thirdy);

                    for (int l = 1; l < ForLoopF + 1; l++)
                    {
                        TJAPlayer3.Tx.Background_Up_3rd[2].t2D描画(TJAPlayer3.app.Device, +(l * TJAPlayer3.Tx.Background_Up_3rd[2].szテクスチャサイズ.Width) - thirdx, 0 + i * 540 - thirdy);
                    }

                    #endregion
                }
            }

                #endregion
            }
            else
            {
                #region [ 段位拝啓 ]

                TJAPlayer3.Tx.Background_Up_Dan[0].t2D描画(TJAPlayer3.app.Device, 0, 0);

                for(int i = 0; i < 1280 / TJAPlayer3.Tx.Background_Up_Dan[1].szテクスチャサイズ.Width + 2; i++)
                    TJAPlayer3.Tx.Background_Up_Dan[1].t2D描画(TJAPlayer3.app.Device, (i * TJAPlayer3.Tx.Background_Up_Dan[1].szテクスチャサイズ.Width) - ct上背景スクロール用タイマー1stDan[0].n現在の値, 0);

                for(int i = 0; i < 1280 / TJAPlayer3.Tx.Background_Up_Dan[2].szテクスチャサイズ.Width + 2; i++)
                    TJAPlayer3.Tx.Background_Up_Dan[2].t2D描画(TJAPlayer3.app.Device, (i * TJAPlayer3.Tx.Background_Up_Dan[2].szテクスチャサイズ.Width) - ct上背景スクロール用タイマー1stDan[1].n現在の値, 0);

                for(int i = 0; i < 1280 / TJAPlayer3.Tx.Background_Up_Dan[3].szテクスチャサイズ.Width + 2; i++)
                    TJAPlayer3.Tx.Background_Up_Dan[3].t2D描画(TJAPlayer3.app.Device, (i * TJAPlayer3.Tx.Background_Up_Dan[3].szテクスチャサイズ.Width) - ct上背景スクロール用タイマー1stDan[2].n現在の値, 0);

                for(int i = 0; i < 1280 / TJAPlayer3.Tx.Background_Up_Dan[4].szテクスチャサイズ.Width + 2; i++)
                    TJAPlayer3.Tx.Background_Up_Dan[4].t2D描画(TJAPlayer3.app.Device, + (i * TJAPlayer3.Tx.Background_Up_Dan[4].szテクスチャサイズ.Width) - ct上背景スクロール用タイマー2stDan.n現在の値, -200 + ct上背景スクロール用タイマー2stDan.n現在の値);

                for(int i = 0; i < 1280 / TJAPlayer3.Tx.Background_Up_Dan[5].szテクスチャサイズ.Width + 2; i++)
                    TJAPlayer3.Tx.Background_Up_Dan[5].t2D描画(TJAPlayer3.app.Device, (i * TJAPlayer3.Tx.Background_Up_Dan[5].szテクスチャサイズ.Width) - ct上背景スクロール用タイマー1stDan[3].n現在の値, 0);

                #endregion
            }

            #endregion
            #region 1P-下背景
            if (!TJAPlayer3.stage演奏ドラム画面.bDoublePlay)
            {
                if (TJAPlayer3.Tx.Background_Down != null)
                {
                    TJAPlayer3.Tx.Background_Down.t2D描画(TJAPlayer3.app.Device, 0, 360);
                }
                if (TJAPlayer3.stage演奏ドラム画面.bIsAlreadyCleared[0])
                {
                    if (TJAPlayer3.Tx.Background_Down_Clear != null && TJAPlayer3.Tx.Background_Down_Scroll != null && ct下背景スクロール用タイマー1 != null)
                    {
                        TJAPlayer3.Tx.Background_Down_Clear.Opacity = ((this.ct上背景FIFOタイマー.n現在の値 * 0xff) / 100);
                        TJAPlayer3.Tx.Background_Down_Scroll.Opacity = ((this.ct上背景FIFOタイマー.n現在の値 * 0xff) / 100);
                        TJAPlayer3.Tx.Background_Down_Clear.t2D描画(TJAPlayer3.app.Device, 0, 360);

                        //int nループ幅 = 1257;
                        //CDTXMania.Tx.Background_Down_Scroll.t2D描画( CDTXMania.app.Device, 0 - this.ct下背景スクロール用タイマー1.n現在の値, 360 );
                        //CDTXMania.Tx.Background_Down_Scroll.t2D描画(CDTXMania.app.Device, (1 * nループ幅) - this.ct下背景スクロール用タイマー1.n現在の値, 360);
                        double TexSize = 1280 / TJAPlayer3.Tx.Background_Down_Scroll.szテクスチャサイズ.Width;
                        // 1280をテクスチャサイズで割ったものを切り上げて、プラス+1足す。
                        int ForLoop = (int)Math.Ceiling(TexSize) + 1;

                        //int nループ幅 = 328;
                        TJAPlayer3.Tx.Background_Down_Scroll.t2D描画(TJAPlayer3.app.Device, 0 - this.ct下背景スクロール用タイマー1.n現在の値, 360);
                        for (int l = 1; l < ForLoop + 1; l++)
                        {
                            TJAPlayer3.Tx.Background_Down_Scroll.t2D描画(TJAPlayer3.app.Device, +(l * TJAPlayer3.Tx.Background_Down_Scroll.szテクスチャサイズ.Width) - this.ct下背景スクロール用タイマー1.n現在の値, 360);
                        }

                    }
                }
            }
            #endregion

            return base.On進行描画();
        }

        #region[ private ]
        //-----------------
        private CCounter[] ct上背景スクロール用タイマー1st; //上背景のX方向スクロール用
        private CCounter[] ct上背景スクロール用タイマー2nd; //上背景のY方向スクロール用
        private CCounter[] ct上背景スクロール用タイマー3rd; //上背景のY方向スクロール用
        private CCounter ct下背景スクロール用タイマー1; //下背景パーツ1のX方向スクロール用
        private CCounter ct上背景FIFOタイマー;
        private CCounter[] ct上背景クリアインタイマー;
        private CCounter[] ct上背景スクロール用タイマー1stDan;   //上背景のX方向スクロール用
        private CCounter ct上背景スクロール用タイマー2stDan;   //上背景のY方向スクロール用
        //private CTexture tx上背景メイン;
        //private CTexture tx上背景クリアメイン;
        //private CTexture tx下背景メイン;
        //private CTexture tx下背景クリアメイン;
        //private CTexture tx下背景クリアサブ1;
        private EFIFOモード eFadeMode;
        //-----------------
        #endregion
    }
}
