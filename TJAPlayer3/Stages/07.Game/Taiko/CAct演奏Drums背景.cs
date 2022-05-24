using System;
using FDK;
using System.Drawing;
using SharpDX;
using static TJAPlayer3.CActSelect曲リスト;

using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;

namespace TJAPlayer3
{
    // Small static class which refers to the Tower mode important informations
    static internal class CFloorManagement
    {
        public static void reinitialize(int life)
        {
            CFloorManagement.LastRegisteredFloor = 1;
            CFloorManagement.MaxNumberOfLives = life;
            CFloorManagement.CurrentNumberOfLives = life;
            CFloorManagement.InvincibilityFrames = null;
        }

        public static void damage()
        {
            if (CFloorManagement.InvincibilityFrames != null && CFloorManagement.InvincibilityFrames.n現在の値 < CFloorManagement.InvincibilityDuration)
                return;

            if (CFloorManagement.CurrentNumberOfLives > 0)
            {
                CFloorManagement.InvincibilityFrames = new CCounter(0, CFloorManagement.InvincibilityDuration + 1000, 1, TJAPlayer3.Timer);
                CFloorManagement.CurrentNumberOfLives--;
                TJAPlayer3.Skin.soundTowerMiss.t再生する();
            }
        }

        public static bool isBlinking()
        {
            if (CFloorManagement.InvincibilityFrames == null || CFloorManagement.InvincibilityFrames.n現在の値 >= CFloorManagement.InvincibilityDuration)
                return false;

            if (CFloorManagement.InvincibilityFrames.n現在の値 % 200 > 100)
                return false;

            return true;
        }

        public static void loopFrames()
        {
            if (CFloorManagement.InvincibilityFrames != null)
                CFloorManagement.InvincibilityFrames.t進行();
        }

        public static int LastRegisteredFloor = 1;
        public static int MaxNumberOfLives = 5;
        public static int CurrentNumberOfLives = 5;

        // ms
        public static readonly int InvincibilityDuration = 2000;
        public static CCounter InvincibilityFrames = null;
    }

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
            if (!this.b活性化してない)
                return;

            var bgOrigindir = CSkin.Path($"{TextureLoader.BASE}{TextureLoader.GAME}{TextureLoader.BACKGROUND}");
            if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
            {
                bgOrigindir += "Tower";
            }
            else if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan)
            {
                bgOrigindir += "Dan";
            }
            else
            {
                bgOrigindir += "Normal";
            }

            Random random = new Random();

            if (System.IO.Directory.Exists($@"{bgOrigindir}\Up"))
            {
                var upDirs = System.IO.Directory.GetDirectories($@"{bgOrigindir}\Up");
                var upPath = upDirs[random.Next(0, upDirs.Length)];

                Background_Up_1st = new CTexture[3];
                Background_Up_1st[0] = TJAPlayer3.tテクスチャの生成($@"{upPath}\1P_Up_1st.png");
                Background_Up_1st[1] = TJAPlayer3.tテクスチャの生成($@"{upPath}\2P_Up_1st.png");
                Background_Up_1st[2] = TJAPlayer3.tテクスチャの生成($@"{upPath}\Clear_Up_1st.png");

                Background_Up_2nd = new CTexture[3];
                Background_Up_2nd[0] = TJAPlayer3.tテクスチャの生成($@"{upPath}\1P_Up_2nd.png");
                Background_Up_2nd[1] = TJAPlayer3.tテクスチャの生成($@"{upPath}\2P_Up_2nd.png");
                Background_Up_2nd[2] = TJAPlayer3.tテクスチャの生成($@"{upPath}\Clear_Up_2nd.png");

                Background_Up_3rd = new CTexture[3];
                Background_Up_3rd[0] = TJAPlayer3.tテクスチャの生成($@"{upPath}\1P_Up_3rd.png");
                Background_Up_3rd[1] = TJAPlayer3.tテクスチャの生成($@"{upPath}\2P_Up_3rd.png");
                Background_Up_3rd[2] = TJAPlayer3.tテクスチャの生成($@"{upPath}\Clear_Up_3rd.png");

                for (int i = 0; i < Background_Up_Dan.Length; i++)
                    Background_Up_Dan[i] = TJAPlayer3.tテクスチャの生成($@"{upPath}\{i}.png");
                for (int i = 0; i < Background_Up_Tower.Length; i++)
                    Background_Up_Tower[i] = TJAPlayer3.tテクスチャの生成($@"{upPath}\{i}.png");

                IsUpNotFound = false;
            }
            else
            {
                IsUpNotFound = true;
            }

            if (System.IO.Directory.Exists($@"{bgOrigindir}\Down"))
            {
                var downDirs = System.IO.Directory.GetDirectories($@"{bgOrigindir}\Down");
                var downPath = downDirs[random.Next(0, downDirs.Length)];

                Background = TJAPlayer3.tテクスチャの生成($@"{bgOrigindir}Background.png");

                Background_Down = TJAPlayer3.tテクスチャの生成($@"{downPath}\Down.png");
                Background_Down_Clear = TJAPlayer3.tテクスチャの生成($@"{downPath}\Down_Clear.png");
                Background_Down_Scroll = TJAPlayer3.tテクスチャの生成($@"{downPath}\Down_Scroll.png");

                IsDownNotFound = false;
            }
            else
            {
                IsDownNotFound = true;
            }


            if (!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName))
            {
                this.pfTowerText = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 28);
            }
            else
            {
                this.pfTowerText = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 28);
            }

            this.ttkTouTatsuKaiSuu = new TitleTextureKey(CLangManager.LangInstance.GetString(1000), pfTowerText, Color.White, Color.Black, 700);
            this.ttkKai = new TitleTextureKey(CLangManager.LangInstance.GetString(1001), pfTowerText, Color.White, Color.Black, 700);

            this.ct炎 = new CCounter(0, 6, 50, TJAPlayer3.Timer);

            base.On活性化();
        }

        public override void On非活性化()
        {
            if (this.b活性化してない)
                return;

            TJAPlayer3.t安全にDisposeする(ref Background);

            TJAPlayer3.t安全にDisposeする(ref Background_Up_1st);
            TJAPlayer3.t安全にDisposeする(ref Background_Up_2nd);
            TJAPlayer3.t安全にDisposeする(ref Background_Up_3rd);

            TJAPlayer3.t安全にDisposeする(ref Background_Down);
            TJAPlayer3.t安全にDisposeする(ref Background_Down_Clear);
            TJAPlayer3.t安全にDisposeする(ref Background_Down_Scroll);

            for (int i = 0; i < Background_Up_Dan.Length; i++)
                TJAPlayer3.t安全にDisposeする(ref Background_Up_Dan[i]);
            for (int i = 0; i < Background_Up_Tower.Length; i++)
                TJAPlayer3.t安全にDisposeする(ref Background_Up_Tower[i]);


            TJAPlayer3.t安全にDisposeする(ref pfTowerText);

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
            if (!base.b活性化してない)
            {
                this.ct上背景スクロール用タイマー1st = new CCounter[2];
                this.ct上背景スクロール用タイマー2nd = new CCounter[2];
                this.ct上背景スクロール用タイマー3rd = new CCounter[2];
                this.ct上背景クリアインタイマー = new CCounter[2];
                ct上背景スクロール用タイマー1stDan = new CCounter[4];
                ct上背景スクロール用タイマー1stTower = new CCounter[4];

                if (!IsUpNotFound)
                {
                    if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan)
                    {
                        this.ct上背景スクロール用タイマー1stDan[0] = new CCounter(0, this.Background_Up_Dan[1].szテクスチャサイズ.Width, 8.453f * 2, TJAPlayer3.Timer);
                        this.ct上背景スクロール用タイマー1stDan[1] = new CCounter(0, this.Background_Up_Dan[2].szテクスチャサイズ.Width, 10.885f * 2, TJAPlayer3.Timer);
                        this.ct上背景スクロール用タイマー1stDan[2] = new CCounter(0, this.Background_Up_Dan[3].szテクスチャサイズ.Width, 11.4f * 2, TJAPlayer3.Timer);
                        this.ct上背景スクロール用タイマー1stDan[3] = new CCounter(0, this.Background_Up_Dan[5].szテクスチャサイズ.Width, 33.88f, TJAPlayer3.Timer);
                        this.ct上背景スクロール用タイマー2stDan = new CCounter(0, this.Background_Up_Dan[4].szテクスチャサイズ.Width + 200, 10, TJAPlayer3.Timer);
                    }
                    else if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
                    {
                        this.ct上背景スクロール用タイマー1stTower[0] = new CCounter(0, this.Background_Up_Tower[1].szテクスチャサイズ.Width, 8.453f * 2, TJAPlayer3.Timer);
                        this.ct上背景スクロール用タイマー1stTower[1] = new CCounter(0, this.Background_Up_Tower[2].szテクスチャサイズ.Width, 10.885f * 2, TJAPlayer3.Timer);
                        this.ct上背景スクロール用タイマー1stTower[2] = new CCounter(0, this.Background_Up_Tower[3].szテクスチャサイズ.Width, 11.4f * 2, TJAPlayer3.Timer);
                        this.ct上背景スクロール用タイマー1stTower[3] = new CCounter(0, this.Background_Up_Tower[5].szテクスチャサイズ.Width, 33.88f, TJAPlayer3.Timer);
                        this.ct上背景スクロール用タイマー2stTower = new CCounter(0, this.Background_Up_Tower[4].szテクスチャサイズ.Width + 200, 10, TJAPlayer3.Timer);
                    }
                    else
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            if (this.Background_Up_3rd[i] != null)
                            {
                                this.ct上背景スクロール用タイマー1st[i] = new CCounter(1, this.Background_Up_1st[i].szテクスチャサイズ.Width, 16, TJAPlayer3.Timer);
                                this.ct上背景スクロール用タイマー2nd[i] = new CCounter(1, this.Background_Up_2nd[i].szテクスチャサイズ.Height, 70, TJAPlayer3.Timer);
                                this.ct上背景スクロール用タイマー3rd[i] = new CCounter(1, 600, 3, TJAPlayer3.Timer);
                                this.ct上背景クリアインタイマー[i] = new CCounter();
                            }
                        }
                    }
                }

                if (!IsDownNotFound)
                {
                    if (this.Background_Down_Scroll != null)
                        this.ct下背景スクロール用タイマー1 = new CCounter(1, this.Background_Down_Scroll.szテクスチャサイズ.Width, 4, TJAPlayer3.Timer);

                    this.ct上背景FIFOタイマー = new CCounter();
                }

                this.ctSlideAnimation = new CCounter();
                this.ctClimbAnimation = new CCounter();
                this.ctDonAnimation = new CCounter(0, 1000, 24000f / ((float)TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM * TJAPlayer3.ConfigIni.n演奏速度 / 20), TJAPlayer3.Timer);

                base.OnManagedリソースの作成();
            }
        }

        public override void OnManagedリソースの解放()
        {
            if (!base.b活性化してない)
                base.OnManagedリソースの解放();
        }

        public override int On進行描画()
        {
            if (base.b活性化してない)
                return 0;


            this.ct上背景FIFOタイマー?.t進行();

            #region [Scroll counters]

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
            for (int i = 0; i < 4; i++)
            {
                if (this.ct上背景スクロール用タイマー1stTower[i] != null)
                    this.ct上背景スクロール用タイマー1stTower[i].t進行Loop();
            }
            if (this.ct下背景スクロール用タイマー1 != null)
                this.ct下背景スクロール用タイマー1.t進行Loop();

            if (this.ct上背景スクロール用タイマー2stDan != null)
                this.ct上背景スクロール用タイマー2stDan.t進行Loop();

            if (this.ct上背景スクロール用タイマー2stTower != null)
                this.ct上背景スクロール用タイマー2stTower.t進行Loop();

            #endregion

            #region [Tower specific variables declaration]

            float currentFloorPositionMax140 = 0;

            #endregion

            // fNow_Measure_s (/ m)

            #region [Upper background]

            if (!IsUpNotFound)
            {
                if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
                {
                    #region [Tower animations variables]

                    this.bFloorChanged = CFloorManagement.LastRegisteredFloor > 0 && (CFloorManagement.LastRegisteredFloor < TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] + 1);

                    int maxFloor = TJAPlayer3.stage選曲.r確定された曲.arスコア[5].譜面情報.nTotalFloor;
                    int nightTime = Math.Max(140, maxFloor / 2);

                    currentFloorPositionMax140 = Math.Min(TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] / (float)nightTime, 1f);

                    #endregion

                    #region [Tower HAIKEI]

                    this.Background_Up_Tower[0]?.t2D描画(TJAPlayer3.app.Device, 0, 0);

                    if (this.Background_Up_Tower[7] != null)
                        this.Background_Up_Tower[7].Opacity = (int)(255f * currentFloorPositionMax140);


                    this.Background_Up_Tower[7]?.t2D描画(TJAPlayer3.app.Device, 0, 0);

                    if (this.Background_Up_Tower[1] != null && this.Background_Up_Tower[2] != null && this.Background_Up_Tower[3] != null)
                    {
                        float colorTmp = 0.5f + (1f - currentFloorPositionMax140) * 0.5f;

                        this.Background_Up_Tower[1].color4 = new Color4(colorTmp, colorTmp, colorTmp, 1f);
                        this.Background_Up_Tower[2].color4 = new Color4(colorTmp, colorTmp, colorTmp, 1f);
                        this.Background_Up_Tower[3].color4 = new Color4(colorTmp, colorTmp, colorTmp, 1f);

                        this.Background_Up_Tower[1].Opacity = (int)(255f * colorTmp);
                        this.Background_Up_Tower[2].Opacity = (int)(255f * colorTmp);
                        this.Background_Up_Tower[3].Opacity = (int)(255f * colorTmp);
                    }

                    if (this.Background_Up_Tower[1] != null)
                        for (int i = 0; i < 1280 / this.Background_Up_Tower[1].szテクスチャサイズ.Width + 2; i++)
                            this.Background_Up_Tower[1].t2D描画(TJAPlayer3.app.Device, (i * this.Background_Up_Tower[1].szテクスチャサイズ.Width) - ct上背景スクロール用タイマー1stTower[0].n現在の値, 0);

                    if (this.Background_Up_Tower[2] != null)
                        for (int i = 0; i < 1280 / this.Background_Up_Tower[2].szテクスチャサイズ.Width + 2; i++)
                            this.Background_Up_Tower[2].t2D描画(TJAPlayer3.app.Device, (i * this.Background_Up_Tower[2].szテクスチャサイズ.Width) - ct上背景スクロール用タイマー1stTower[1].n現在の値, 0);

                    if (this.Background_Up_Tower[3] != null)
                        for (int i = 0; i < 1280 / this.Background_Up_Tower[3].szテクスチャサイズ.Width + 2; i++)
                            this.Background_Up_Tower[3].t2D描画(TJAPlayer3.app.Device, (i * this.Background_Up_Tower[3].szテクスチャサイズ.Width) - ct上背景スクロール用タイマー1stTower[2].n現在の値, 0);

                    this.Background_Up_Tower[6]?.t2D描画(TJAPlayer3.app.Device, 0, 0);

                    if (this.Background_Up_Tower[4] != null)
                        for (int i = 0; i < 1280 / this.Background_Up_Tower[4].szテクスチャサイズ.Width + 2; i++)
                            this.Background_Up_Tower[4].t2D描画(TJAPlayer3.app.Device, +(i * this.Background_Up_Tower[4].szテクスチャサイズ.Width) - ct上背景スクロール用タイマー2stTower.n現在の値, -200 + ct上背景スクロール用タイマー2stTower.n現在の値);

                    if (this.Background_Up_Tower[5] != null)
                        for (int i = 0; i < 1280 / this.Background_Up_Tower[5].szテクスチャサイズ.Width + 2; i++)
                            this.Background_Up_Tower[5].t2D描画(TJAPlayer3.app.Device, (i * this.Background_Up_Tower[5].szテクスチャサイズ.Width) - ct上背景スクロール用タイマー1stTower[3].n現在の値, 0);



                    #endregion

                    #region [Tower background informations]

                    if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
                    {
                        TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(ttkTouTatsuKaiSuu).t2D描画(TJAPlayer3.app.Device, 350, 32);
                        TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(ttkKai).t2D描画(TJAPlayer3.app.Device, 550, 104);

                        this.ct炎.t進行Loop();
                        CFloorManagement.loopFrames();

                        #region [Floor number]

                        if (CFloorManagement.CurrentNumberOfLives > 0)
                            CFloorManagement.LastRegisteredFloor = TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] + 1;

                        string floorStr = CFloorManagement.LastRegisteredFloor.ToString();

                        int len = floorStr.Length;

                        int digitLength = TJAPlayer3.Tx.Taiko_Combo[0].szテクスチャサイズ.Width / 10;

                        TJAPlayer3.Tx.Taiko_Combo[0].color4 = new Color4(1f, 0.6f, 0.2f, 1f);
                        TJAPlayer3.Tx.Taiko_Combo[0].vc拡大縮小倍率.X = 1.4f;
                        TJAPlayer3.Tx.Taiko_Combo[0].vc拡大縮小倍率.Y = 1.4f;

                        for (int idx = len - 1; idx >= 0; idx--)
                        {
                            int currentNum = int.Parse(floorStr[idx].ToString());

                            TJAPlayer3.Tx.Taiko_Combo[0].t2D描画(TJAPlayer3.app.Device, 556 - ((digitLength - 8) * (len - idx) * 1.4f),
                                84,
                                new Rectangle(digitLength * currentNum, 0,
                                    digitLength, TJAPlayer3.Tx.Taiko_Combo[0].szテクスチャサイズ.Height));
                        }

                        #endregion

                        #region [Life Tamashii icon]

                        int baseX = 886;
                        int baseY = 22;

                        TJAPlayer3.Tx.Gauge_Soul_Fire?.t2D描画(TJAPlayer3.app.Device, baseX, baseY, new Rectangle(230 * (this.ct炎.n現在の値), 0, 230, 230));
                        TJAPlayer3.Tx.Gauge_Soul?.t2D描画(TJAPlayer3.app.Device, baseX + 72, baseY + 73, new Rectangle(0, 0, 80, 80));

                        #endregion

                        #region [Life number]

                        if (CFloorManagement.MaxNumberOfLives <= 0)
                        {
                            CFloorManagement.MaxNumberOfLives = 5;
                            CFloorManagement.CurrentNumberOfLives = 5;
                        }

                        string lifeStr = CFloorManagement.CurrentNumberOfLives.ToString();

                        len = lifeStr.Length;

                        bool lifeSpecialCase = CFloorManagement.CurrentNumberOfLives == 1 && CFloorManagement.MaxNumberOfLives != 1;
                        float lifeRatio = CFloorManagement.CurrentNumberOfLives / (float)CFloorManagement.MaxNumberOfLives;

                        Color4 lifeColor = (lifeRatio > 0.5f && !lifeSpecialCase) ? new Color4(0.2f, 1f, 0.2f, 1f)
                                : ((lifeRatio >= 0.2f && !lifeSpecialCase) ? new Color4(1f, 1f, 0.2f, 1f)
                                : new Color4(1f, 0.2f, 0.2f, 1f));

                        TJAPlayer3.Tx.Taiko_Combo[0].color4 = lifeColor;
                        TJAPlayer3.Tx.Taiko_Combo[0].vc拡大縮小倍率.X = 1.1f;
                        TJAPlayer3.Tx.Taiko_Combo[0].vc拡大縮小倍率.Y = 1.1f;

                        for (int idx = 0; idx < len; idx++)
                        {
                            int currentNum = int.Parse(lifeStr[len - idx - 1].ToString());

                            TJAPlayer3.Tx.Taiko_Combo[0].t2D描画(TJAPlayer3.app.Device, 996 + ((digitLength - 8) * (len - idx) * 1.1f),
                                106,
                                new Rectangle(digitLength * currentNum, 0,
                                    digitLength, TJAPlayer3.Tx.Taiko_Combo[0].szテクスチャサイズ.Height));
                        }

                        TJAPlayer3.Tx.Taiko_Combo[0].color4 = new Color4(1f, 1f, 1f, 1f);

                        #endregion

                    }

                    #endregion
                }
                else if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan)
                {
                    // Multiple background handling will be here 
                    #region [ Regular background ]

                    for (int j = 0; j < TJAPlayer3.ConfigIni.nPlayerCount; j++)
                    {
                        int i = j;
                        if (TJAPlayer3.P1IsBlue())
                            i = 1;

                        if (this.ct上背景スクロール用タイマー1st[j] != null)
                        {
                            double TexSizeL = 1280 / this.Background_Up_1st[j].szテクスチャサイズ.Width;
                            double TexSizeW = 308 / this.Background_Up_2nd[j].szテクスチャサイズ.Height;
                            double TexSizeF = 1280 / this.Background_Up_3rd[j].szテクスチャサイズ.Width;
                            // 1280をテクスチャサイズで割ったものを切り上げて、プラス+1足す。
                            int ForLoopL = (int)Math.Ceiling(TexSizeL) + 1;
                            int ForLoopW = (int)Math.Ceiling(TexSizeW) + 1;
                            int ForLoopF = (int)Math.Ceiling(TexSizeF) + 1;
                            //int nループ幅 = 328;

                            #region [ 上背景-Back ]

                            for (int W = 1; W < ForLoopW + 1; W++)
                            {
                                this.Background_Up_1st[i].t2D描画(TJAPlayer3.app.Device, 0 - this.ct上背景スクロール用タイマー1st[j].n現在の値, (185 + j * 600) - (W * this.Background_Up_1st[i].szテクスチャサイズ.Height) + ct上背景スクロール用タイマー2nd[j].n現在の値);
                            }
                            for (int l = 1; l < ForLoopL + 1; l++)
                            {
                                for (int W = 1; W < ForLoopW + 1; W++)
                                {
                                    this.Background_Up_1st[i].t2D描画(TJAPlayer3.app.Device, +(l * this.Background_Up_1st[i].szテクスチャサイズ.Width) - this.ct上背景スクロール用タイマー1st[j].n現在の値, (185 + j * 600) - (W * this.Background_Up_1st[i].szテクスチャサイズ.Height) + ct上背景スクロール用タイマー2nd[j].n現在の値);
                                }
                            }

                            for (int W = 1; W < ForLoopW + 1; W++)
                            {
                                this.Background_Up_2nd[i].t2D描画(TJAPlayer3.app.Device, 0 - this.ct上背景スクロール用タイマー1st[j].n現在の値, (370 + j * 600) - (W * this.Background_Up_2nd[i].szテクスチャサイズ.Height) - ct上背景スクロール用タイマー2nd[j].n現在の値);
                            }
                            for (int l = 1; l < ForLoopL + 1; l++)
                            {
                                for (int W = 1; W < ForLoopW + 1; W++)
                                {
                                    this.Background_Up_2nd[i].t2D描画(TJAPlayer3.app.Device, +(l * this.Background_Up_2nd[i].szテクスチャサイズ.Width) - this.ct上背景スクロール用タイマー1st[j].n現在の値, (370 + j * 600) - (W * this.Background_Up_2nd[i].szテクスチャサイズ.Height) - ct上背景スクロール用タイマー2nd[j].n現在の値);
                                }
                            }

                            #endregion

                            #region [ 上背景-Front ]

                            float thirdy = 0;
                            float thirdx = 0;

                            if (this.ct上背景スクロール用タイマー3rd[i].n現在の値 <= 270)
                            {
                                thirdx = this.ct上背景スクロール用タイマー3rd[i].n現在の値 * 0.9258f;
                                thirdy = (float)Math.Sin((float)this.ct上背景スクロール用タイマー3rd[j].n現在の値 * (Math.PI / 270.0f)) * 40.0f;
                            }
                            else
                            {
                                thirdx = 250 + (ct上背景スクロール用タイマー3rd[j].n現在の値 - 270) * 0.24f;

                                if (this.ct上背景スクロール用タイマー3rd[j].n現在の値 <= 490) thirdy = -(float)Math.Sin((float)(this.ct上背景スクロール用タイマー3rd[j].n現在の値 - 270) * (Math.PI / 170.0f)) * 15.0f;
                                else thirdy = -((float)Math.Sin((float)220f * (Math.PI / 170.0f)) * 15.0f) + (float)(((this.ct上背景スクロール用タイマー3rd[j].n現在の値 - 490) / 110f) * ((float)Math.Sin((float)220f * (Math.PI / 170.0f)) * 15.0f));
                            }

                            this.Background_Up_3rd[i].t2D描画(TJAPlayer3.app.Device, 0 - thirdx, 0 + j * 540 - thirdy);

                            for (int l = 1; l < ForLoopF + 1; l++)
                            {
                                this.Background_Up_3rd[i].t2D描画(TJAPlayer3.app.Device, +(l * this.Background_Up_3rd[i].szテクスチャサイズ.Width) - thirdx, 0 + j * 540 - thirdy);
                            }

                            #endregion
                        }

                        if (this.ct上背景スクロール用タイマー1st[j] != null)
                        {
                            if (TJAPlayer3.stage演奏ドラム画面.bIsAlreadyCleared[j])
                            {
                                this.Background_Up_1st[2].Opacity = ((this.ct上背景クリアインタイマー[j].n現在の値 * 0xff) / 100);
                                this.Background_Up_2nd[2].Opacity = ((this.ct上背景クリアインタイマー[j].n現在の値 * 0xff) / 100);
                                this.Background_Up_3rd[2].Opacity = ((this.ct上背景クリアインタイマー[j].n現在の値 * 0xff) / 100);
                            }
                            else
                            {
                                this.Background_Up_1st[2].Opacity = 0;
                                this.Background_Up_2nd[2].Opacity = 0;
                                this.Background_Up_3rd[2].Opacity = 0;
                            }

                            double TexSizeL = 1280 / this.Background_Up_1st[2].szテクスチャサイズ.Width;
                            double TexSizeW = 308 / this.Background_Up_2nd[2].szテクスチャサイズ.Height;
                            double TexSizeF = 1280 / this.Background_Up_3rd[2].szテクスチャサイズ.Width;
                            // 1280をテクスチャサイズで割ったものを切り上げて、プラス+1足す。
                            int ForLoopL = (int)Math.Ceiling(TexSizeL) + 1;
                            int ForLoopW = (int)Math.Ceiling(TexSizeW) + 1;
                            int ForLoopF = (int)Math.Ceiling(TexSizeF) + 1;

                            #region [ 上背景-Back ]

                            for (int W = 1; W < ForLoopW + 1; W++)
                            {
                                this.Background_Up_1st[2].t2D描画(TJAPlayer3.app.Device, 0 - this.ct上背景スクロール用タイマー1st[j].n現在の値, (185 + j * 600) - (W * this.Background_Up_1st[2].szテクスチャサイズ.Height) + ct上背景スクロール用タイマー2nd[j].n現在の値);
                            }
                            for (int l = 1; l < ForLoopL + 1; l++)
                            {
                                for (int W = 1; W < ForLoopW + 1; W++)
                                {
                                    this.Background_Up_1st[2].t2D描画(TJAPlayer3.app.Device, +(l * this.Background_Up_1st[2].szテクスチャサイズ.Width) - this.ct上背景スクロール用タイマー1st[j].n現在の値, (185 + j * 600) - (W * this.Background_Up_1st[2].szテクスチャサイズ.Height) + ct上背景スクロール用タイマー2nd[j].n現在の値);
                                }
                            }

                            for (int W = 1; W < ForLoopW + 1; W++)
                            {
                                this.Background_Up_2nd[2].t2D描画(TJAPlayer3.app.Device, 0 - this.ct上背景スクロール用タイマー1st[j].n現在の値, (370 + j * 600) - (W * this.Background_Up_2nd[2].szテクスチャサイズ.Height) - ct上背景スクロール用タイマー2nd[j].n現在の値);
                            }
                            for (int l = 1; l < ForLoopL + 1; l++)
                            {
                                for (int W = 1; W < ForLoopW + 1; W++)
                                {
                                    this.Background_Up_2nd[2].t2D描画(TJAPlayer3.app.Device, +(l * this.Background_Up_2nd[2].szテクスチャサイズ.Width) - this.ct上背景スクロール用タイマー1st[j].n現在の値, (370 + j * 600) - (W * this.Background_Up_2nd[2].szテクスチャサイズ.Height) - ct上背景スクロール用タイマー2nd[j].n現在の値);
                                }
                            }

                            #endregion

                            #region [ 上背景-Front ]

                            float thirdy = 0;
                            float thirdx = 0;

                            if (this.ct上背景スクロール用タイマー3rd[j].n現在の値 <= 270)
                            {
                                thirdx = this.ct上背景スクロール用タイマー3rd[j].n現在の値 * 0.9258f;
                                thirdy = (float)Math.Sin((float)this.ct上背景スクロール用タイマー3rd[j].n現在の値 * (Math.PI / 270.0f)) * 40.0f;
                            }
                            else
                            {
                                thirdx = 250 + (ct上背景スクロール用タイマー3rd[j].n現在の値 - 270) * 0.24f;

                                if (this.ct上背景スクロール用タイマー3rd[j].n現在の値 <= 490) thirdy = -(float)Math.Sin((float)(this.ct上背景スクロール用タイマー3rd[j].n現在の値 - 270) * (Math.PI / 170.0f)) * 15.0f;
                                else thirdy = -((float)Math.Sin((float)220f * (Math.PI / 170.0f)) * 15.0f) + (float)(((this.ct上背景スクロール用タイマー3rd[j].n現在の値 - 490) / 110f) * ((float)Math.Sin((float)220f * (Math.PI / 170.0f)) * 15.0f));
                            }

                            this.Background_Up_3rd[2].t2D描画(TJAPlayer3.app.Device, 0 - thirdx, 0 + i * 540 - thirdy);

                            for (int l = 1; l < ForLoopF + 1; l++)
                            {
                                this.Background_Up_3rd[2].t2D描画(TJAPlayer3.app.Device, +(l * this.Background_Up_3rd[2].szテクスチャサイズ.Width) - thirdx, 0 + j * 540 - thirdy);
                            }

                            #endregion
                        }
                    }

                    #endregion



                }
                else
                {
                    // Not 拝啓 but 背景 w, apparently Kanji typos are pretty common, "Dear DanI" lol
                    #region [ 段位拝啓 ]

                    this.Background_Up_Dan[0].t2D描画(TJAPlayer3.app.Device, 0, 0);

                    for (int i = 0; i < 1280 / this.Background_Up_Dan[1].szテクスチャサイズ.Width + 2; i++)
                        this.Background_Up_Dan[1].t2D描画(TJAPlayer3.app.Device, (i * this.Background_Up_Dan[1].szテクスチャサイズ.Width) - ct上背景スクロール用タイマー1stDan[0].n現在の値, 0);

                    for (int i = 0; i < 1280 / this.Background_Up_Dan[2].szテクスチャサイズ.Width + 2; i++)
                        this.Background_Up_Dan[2].t2D描画(TJAPlayer3.app.Device, (i * this.Background_Up_Dan[2].szテクスチャサイズ.Width) - ct上背景スクロール用タイマー1stDan[1].n現在の値, 0);

                    for (int i = 0; i < 1280 / this.Background_Up_Dan[3].szテクスチャサイズ.Width + 2; i++)
                        this.Background_Up_Dan[3].t2D描画(TJAPlayer3.app.Device, (i * this.Background_Up_Dan[3].szテクスチャサイズ.Width) - ct上背景スクロール用タイマー1stDan[2].n現在の値, 0);

                    for (int i = 0; i < 1280 / this.Background_Up_Dan[4].szテクスチャサイズ.Width + 2; i++)
                        this.Background_Up_Dan[4].t2D描画(TJAPlayer3.app.Device, +(i * this.Background_Up_Dan[4].szテクスチャサイズ.Width) - ct上背景スクロール用タイマー2stDan.n現在の値, -200 + ct上背景スクロール用タイマー2stDan.n現在の値);

                    for (int i = 0; i < 1280 / this.Background_Up_Dan[5].szテクスチャサイズ.Width + 2; i++)
                        this.Background_Up_Dan[5].t2D描画(TJAPlayer3.app.Device, (i * this.Background_Up_Dan[5].szテクスチャサイズ.Width) - ct上背景スクロール用タイマー1stDan[3].n現在の値, 0);

                    #endregion
                }
            }

            #endregion

            #region [Lower background]


            if (!IsUpNotFound)
            {
                if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
                {
                    int maxFloor = TJAPlayer3.stage選曲.r確定された曲.arスコア[5].譜面情報.nTotalFloor;

                    TJAPlayer3.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.白, maxFloor.ToString());

                    int nightTime = Math.Max(140, maxFloor / 2);

                    int currentTowerType = TJAPlayer3.stage選曲.r確定された曲.arスコア[5].譜面情報.nTowerType;

                    if (currentTowerType < 0 || currentTowerType >= TJAPlayer3.Skin.Game_Tower_Ptn)
                        currentTowerType = 0;

                    #region [Tower lower background]

                    float nextPositionMax140 = Math.Min((TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] + 1) / (float)nightTime, 1f);

                    if (bFloorChanged == true)
                        ctSlideAnimation.t開始(0, 1000, 120f / ((float)TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM * TJAPlayer3.ConfigIni.n演奏速度 / 20), TJAPlayer3.Timer);

                    float progressFactor = (nextPositionMax140 - currentFloorPositionMax140) * (ctSlideAnimation.n現在の値 / 1000f);



                    #region [Skybox]

                    int skyboxYPosition = (int)(5000 * (1f - (currentFloorPositionMax140 + progressFactor)));

                    TJAPlayer3.Tx.Tower_Sky_Gradient?.t2D描画(TJAPlayer3.app.Device, 0, 360, new Rectangle(0, skyboxYPosition, 1280, 316));

                    #endregion


                    #region [Tower body]

                    progressFactor = ctSlideAnimation.n現在の値 / 1000f;

                    int currentTower = currentTowerType;

                    // Will implement the roof later, need the beforehand total floor count calculation before
                    int nextTowerBase = ((TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] + 1) / 10) % TJAPlayer3.Skin.Game_Tower_Ptn_Base[currentTower];
                    int towerBase = (TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] / 10) % TJAPlayer3.Skin.Game_Tower_Ptn_Base[currentTower];

                    int currentDeco = TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] % TJAPlayer3.Skin.Game_Tower_Ptn_Deco[currentTower];
                    int nextDeco = (TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] + 1) % TJAPlayer3.Skin.Game_Tower_Ptn_Deco[currentTower];

                    // Microfix for the first floor suddenly changing texture
                    if (TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] == 0 && TJAPlayer3.Skin.Game_Tower_Ptn_Deco[currentTower] > 1)
                        currentDeco++;
                    if (TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] == 0 && TJAPlayer3.Skin.Game_Tower_Ptn_Base[currentTower] > 1)
                        towerBase++;

                    int heightChange = (int)(progressFactor * 288f);

                    // Current trunk
                    if (TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] < maxFloor)
                        TJAPlayer3.Tx.Tower_Base[currentTower][towerBase]?.t2D下中央基準描画(TJAPlayer3.app.Device, 640, 676 + heightChange); // 316 + 360
                    else
                        TJAPlayer3.Tx.Tower_Top[currentTower]?.t2D下中央基準描画(TJAPlayer3.app.Device, 640, 676 + heightChange);

                    // Current deco
                    TJAPlayer3.Tx.Tower_Deco[currentTower][currentDeco]?.t2D下中央基準描画(TJAPlayer3.app.Device, 460, 640 + heightChange);


                    // Next trunk
                    if (TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] + 1 < maxFloor)
                        TJAPlayer3.Tx.Tower_Base[currentTower][nextTowerBase]?.t2D下中央基準描画(TJAPlayer3.app.Device, 640, 388 + heightChange, // Current - 288  
                            new Rectangle(0, 288 - heightChange,
                                TJAPlayer3.Tx.Tower_Base[currentTower][nextTowerBase].szテクスチャサイズ.Width,
                                Math.Min(TJAPlayer3.Tx.Tower_Base[currentTower][nextTowerBase].szテクスチャサイズ.Height, heightChange + 28)));
                    else if (TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] + 1 == maxFloor)
                    {
                        TJAPlayer3.Tx.Tower_Top[currentTower]?.t2D下中央基準描画(TJAPlayer3.app.Device, 640, 388 + heightChange, // Current - 288  
                            new Rectangle(0, 351 - heightChange,
                                TJAPlayer3.Tx.Tower_Top[currentTower].szテクスチャサイズ.Width,
                                Math.Min(TJAPlayer3.Tx.Tower_Top[currentTower].szテクスチャサイズ.Height, heightChange + 28)));
                    }

                    // Next deco
                    if (heightChange > 46 && TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] + 1 <= maxFloor)
                        TJAPlayer3.Tx.Tower_Deco[currentTower][nextDeco]?.t2D下中央基準描画(TJAPlayer3.app.Device, 460, 352 + heightChange);


                    #endregion

                    #region [Climbing don]

                    // Will be added in a future skinning update
                    int currentDon = 0;

                    if (bFloorChanged == true)
                    {
                        ctClimbAnimation.t開始(0, 1500, 120f / ((float)TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM * TJAPlayer3.ConfigIni.n演奏速度 / 20), TJAPlayer3.Timer);
                        ctDonAnimation.t開始(0, 1000, 24000f / ((float)TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM * TJAPlayer3.ConfigIni.n演奏速度 / 20), TJAPlayer3.Timer);
                    }


                    if (ctClimbAnimation.n現在の値 == 0 || ctClimbAnimation.n現在の値 == 1500)
                    {
                        int animDon = ctDonAnimation.n現在の値 % TJAPlayer3.Skin.Game_Tower_Ptn_Don_Standing[currentDon];
                        TJAPlayer3.Tx.Tower_Don_Standing[currentDon][animDon]?.t2D下中央基準描画(TJAPlayer3.app.Device, 590, 648); // Center X - 50
                    }
                    else if (ctClimbAnimation.n現在の値 <= 1000)
                    {
                        int animDon = ctDonAnimation.n現在の値 % TJAPlayer3.Skin.Game_Tower_Ptn_Don_Climbing[currentDon];
                        int distDon = (int)(ctClimbAnimation.n現在の値 * (300 / 1000f));
                        TJAPlayer3.Tx.Tower_Don_Climbing[currentDon][animDon]?.t2D下中央基準描画(TJAPlayer3.app.Device, 590 + distDon, 648);
                    }
                    else
                    {
                        int animDon = ctDonAnimation.n現在の値 % TJAPlayer3.Skin.Game_Tower_Ptn_Don_Running[currentDon];
                        int distDon = (int)((1500 - ctClimbAnimation.n現在の値) * (300 / 500f));
                        TJAPlayer3.Tx.Tower_Don_Running[currentDon][animDon]?.t2D下中央基準描画(TJAPlayer3.app.Device, 590 + distDon, 648);
                    }

                    #endregion

                    #region [Miss icon]

                    if (CFloorManagement.InvincibilityFrames != null && CFloorManagement.InvincibilityFrames.n現在の値 < CFloorManagement.InvincibilityDuration)
                    {
                        if (TJAPlayer3.Tx.Tower_Miss != null)
                            TJAPlayer3.Tx.Tower_Miss.Opacity = Math.Min(255, 1000 - CFloorManagement.InvincibilityFrames.n現在の値);
                        TJAPlayer3.Tx.Tower_Miss?.t2D下中央基準描画(TJAPlayer3.app.Device, 640, 520);
                    }

                    #endregion

                    ctSlideAnimation?.t進行();
                    ctClimbAnimation?.t進行();
                    ctDonAnimation?.t進行Loop();

                    #endregion
                }
                else if (!TJAPlayer3.stage演奏ドラム画面.bDoublePlay && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan)
                {

                    #region [Ensou lower background]

                    if (this.Background_Down != null)
                    {
                        this.Background_Down.t2D描画(TJAPlayer3.app.Device, 0, 360);
                    }
                    if (TJAPlayer3.stage演奏ドラム画面.bIsAlreadyCleared[0])
                    {
                        if (this.Background_Down_Clear != null && this.Background_Down_Scroll != null && ct下背景スクロール用タイマー1 != null)
                        {
                            this.Background_Down_Clear.Opacity = ((this.ct上背景FIFOタイマー.n現在の値 * 0xff) / 100);
                            this.Background_Down_Scroll.Opacity = ((this.ct上背景FIFOタイマー.n現在の値 * 0xff) / 100);
                            this.Background_Down_Clear.t2D描画(TJAPlayer3.app.Device, 0, 360);

                            //int nループ幅 = 1257;
                            //CDTXMania.Tx.Background_Down_Scroll.t2D描画( CDTXMania.app.Device, 0 - this.ct下背景スクロール用タイマー1.n現在の値, 360 );
                            //CDTXMania.Tx.Background_Down_Scroll.t2D描画(CDTXMania.app.Device, (1 * nループ幅) - this.ct下背景スクロール用タイマー1.n現在の値, 360);
                            double TexSize = 1280 / this.Background_Down_Scroll.szテクスチャサイズ.Width;
                            // 1280をテクスチャサイズで割ったものを切り上げて、プラス+1足す。
                            int ForLoop = (int)Math.Ceiling(TexSize) + 1;

                            //int nループ幅 = 328;
                            this.Background_Down_Scroll.t2D描画(TJAPlayer3.app.Device, 0 - this.ct下背景スクロール用タイマー1.n現在の値, 360);
                            for (int l = 1; l < ForLoop + 1; l++)
                            {
                                this.Background_Down_Scroll.t2D描画(TJAPlayer3.app.Device, +(l * this.Background_Down_Scroll.szテクスチャサイズ.Width) - this.ct下背景スクロール用タイマー1.n現在の値, 360);
                            }

                        }
                    }

                    #endregion
                }
            }


            #endregion

            return base.On進行描画();
        }

        #region[ private ]
        //-----------------

        #region 背景
        private CTexture Background,
            Background_Down,
            Background_Down_Clear,
            Background_Down_Scroll;
        private CTexture[] Background_Up_1st,
                          Background_Up_2nd,
                          Background_Up_3rd,
                          Background_Up_Dan = new CTexture[6],
                          Background_Up_Tower = new CTexture[8];
        #endregion

        private CCounter[] ct上背景スクロール用タイマー1st; //上背景のX方向スクロール用
        private CCounter[] ct上背景スクロール用タイマー2nd; //上背景のY方向スクロール用
        private CCounter[] ct上背景スクロール用タイマー3rd; //上背景のY方向スクロール用
        private CCounter ct下背景スクロール用タイマー1; //下背景パーツ1のX方向スクロール用
        private CCounter ct上背景FIFOタイマー;
        private CCounter[] ct上背景クリアインタイマー;
        private CCounter[] ct上背景スクロール用タイマー1stDan;   //上背景のX方向スクロール用
        private CCounter ct上背景スクロール用タイマー2stDan;   //上背景のY方向スクロール用
        private CCounter[] ct上背景スクロール用タイマー1stTower;   //上背景のX方向スクロール用
        private CCounter ct上背景スクロール用タイマー2stTower;   //上背景のX方向スクロール用
        //private CTexture tx上背景メイン;
        //private CTexture tx上背景クリアメイン;
        //private CTexture tx下背景メイン;
        //private CTexture tx下背景クリアメイン;
        //private CTexture tx下背景クリアサブ1;
        private TitleTextureKey ttkTouTatsuKaiSuu;
        private TitleTextureKey ttkKai;
        private CPrivateFastFont pfTowerText;

        private bool bFloorChanged = false;
        private CCounter ctSlideAnimation;
        private CCounter ctDonAnimation;
        private CCounter ctClimbAnimation;

        private CCounter ct炎;

        private bool IsUpNotFound;
        private bool IsDownNotFound;

        private EFIFOモード eFadeMode;
        //-----------------
        #endregion
    }
}
