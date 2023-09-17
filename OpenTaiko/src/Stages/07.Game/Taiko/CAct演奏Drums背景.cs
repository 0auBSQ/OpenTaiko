using System;
using FDK;
using System.Drawing;
using static TJAPlayer3.CActSelect曲リスト;

using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using System.Collections.Generic;

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
            if (CFloorManagement.InvincibilityFrames != null && CFloorManagement.InvincibilityFrames.CurrentValue < CFloorManagement.InvincibilityDuration)
                return;

            if (CFloorManagement.CurrentNumberOfLives > 0)
            {
                CFloorManagement.InvincibilityFrames = new CCounter(0, CFloorManagement.InvincibilityDuration + 1000, 1, TJAPlayer3.Timer);
                CFloorManagement.CurrentNumberOfLives--;
                //TJAPlayer3.Skin.soundTowerMiss.t再生する();
                TJAPlayer3.Skin.voiceTowerMiss[TJAPlayer3.SaveFile]?.t再生する();
            }
        }

        public static bool isBlinking()
        {
            if (CFloorManagement.InvincibilityFrames == null || CFloorManagement.InvincibilityFrames.CurrentValue >= CFloorManagement.InvincibilityDuration)
                return false;

            if (CFloorManagement.InvincibilityFrames.CurrentValue % 200 > 100)
                return false;

            return true;
        }

        public static void loopFrames()
        {
            if (CFloorManagement.InvincibilityFrames != null)
                CFloorManagement.InvincibilityFrames.Tick();
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
            base.IsDeActivated = true;
        }

        public void tFadeIn(int player)
        {
            //this.ct上背景クリアインタイマー[player] = new CCounter(0, 100, 2, TJAPlayer3.Timer);
            this.eFadeMode = EFIFOモード.フェードイン;
        }

        //public void tFadeOut(int player)
        //{
        //    this.ct上背景フェードタイマー[player] = new CCounter( 0, 100, 6, CDTXMania.Timer );
        //    this.eFadeMode = EFIFOモード.フェードアウト;
        //}

        public void ClearIn(int player)
        {
            /*this.ct上背景クリアインタイマー[player] = new CCounter(0, 100, 2, TJAPlayer3.Timer);
            this.ct上背景クリアインタイマー[player].n現在の値 = 0;
            this.ct上背景FIFOタイマー = new CCounter(0, 100, 2, TJAPlayer3.Timer);
            this.ct上背景FIFOタイマー.n現在の値 = 0;*/
            UpScript?.ClearIn(player);
            DownScript?.ClearIn(player);
        }

        public void ClearOut(int player)
        {
            UpScript?.ClearOut(player);
            DownScript?.ClearOut(player);
        }

        public override void Activate()
        {
            if (!this.IsDeActivated)
                return;

            var bgOrigindir = CSkin.Path($"{TextureLoader.BASE}{TextureLoader.GAME}{TextureLoader.BACKGROUND}");
            string presetSection = "";
            if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
            {
                bgOrigindir += "Tower";
            }
            else if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan)
            {
                bgOrigindir += "Dan";
                presetSection = "Dan";
            }
            else if (TJAPlayer3.ConfigIni.bAIBattleMode)
            {
                bgOrigindir += "AI";
            }
            else
            {
                bgOrigindir += "Normal";
                presetSection = "Regular";
            }

            object _ps = null;

            switch (presetSection)
            {
                case "Regular":
                    _ps = TJAPlayer3.Skin.Game_SkinScenes.Regular;
                    break;
                case "Dan":
                    _ps = TJAPlayer3.Skin.Game_SkinScenes.Dan;
                    break;
                default:
                    break;
            };

            var preset = (_ps != null 
                    && TJAPlayer3.stage選曲.r確定された曲.strScenePreset != null 
                    && ((Dictionary<string, DBSkinPreset.SkinScene>)_ps).ContainsKey(TJAPlayer3.stage選曲.r確定された曲.strScenePreset)) 
                ? ((Dictionary<string,DBSkinPreset.SkinScene>)_ps)[TJAPlayer3.stage選曲.r確定された曲.strScenePreset] 
                : null;

            if (_ps != null
                    && TJAPlayer3.DTX.scenePreset != null
                    && ((Dictionary<string, DBSkinPreset.SkinScene>)_ps).ContainsKey(TJAPlayer3.DTX.scenePreset)) // If currently selected song has valid SCENEPRESET metadata within TJA
            {
                preset = ((Dictionary<string, DBSkinPreset.SkinScene>)_ps)[TJAPlayer3.DTX.scenePreset];
            }

            Random random = new Random();

            if (System.IO.Directory.Exists($@"{bgOrigindir}{Path.DirectorySeparatorChar}Up"))
            {
                var upDirs = System.IO.Directory.GetDirectories($@"{bgOrigindir}{Path.DirectorySeparatorChar}Up");

                // If there is a preset upper background and this preset exists on the skin use it, else random upper background
                var _presetPath = (preset != null) ? $@"{bgOrigindir}{Path.DirectorySeparatorChar}Up{Path.DirectorySeparatorChar}" + preset.UpperBackground : "";
                var upPath = (preset != null && System.IO.Directory.Exists(_presetPath)) 
                    ?  _presetPath
                    : upDirs[random.Next(0, upDirs.Length)];

                UpScript = new ScriptBG($@"{upPath}{Path.DirectorySeparatorChar}Script.lua");
                UpScript.Init();

                IsUpNotFound = false;
            }
            else
            {
                IsUpNotFound = true;
            }

            if (System.IO.Directory.Exists($@"{bgOrigindir}{Path.DirectorySeparatorChar}Down"))
            {
                var downDirs = System.IO.Directory.GetDirectories($@"{bgOrigindir}{Path.DirectorySeparatorChar}Down");

                // If there is a preset lower background and this preset exists on the skin use it, else random upper background
                var _presetPath = (preset != null) ? $@"{bgOrigindir}{Path.DirectorySeparatorChar}Down{Path.DirectorySeparatorChar}" + preset.LowerBackground : "";
                var downPath = (preset != null && System.IO.Directory.Exists(_presetPath))
                    ? _presetPath
                    : downDirs[random.Next(0, downDirs.Length)];

                DownScript = new ScriptBG($@"{downPath}{Path.DirectorySeparatorChar}Script.lua");
                DownScript.Init();

                IsDownNotFound = false;
            }
            else
            {
                IsDownNotFound = true;
            }

            this.pfTowerText = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.Game_Tower_Font_TowerText);

            /*
            if (!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName))
            {
                this.pfTowerText = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), TJAPlayer3.Skin.Game_Tower_Font_TowerText);
            }
            else
            { 
                this.pfTowerText = new CPrivateFastFont(new FontFamily("MS UI Gothic"), TJAPlayer3.Skin.Game_Tower_Font_TowerText);
            }
            */

            this.ttkTouTatsuKaiSuu = new TitleTextureKey(CLangManager.LangInstance.GetString(1000), pfTowerText, Color.White, Color.Black, 700);
            this.ttkKai = new TitleTextureKey(CLangManager.LangInstance.GetString(1001), pfTowerText, Color.White, Color.Black, 700);

            this.ct炎 = new CCounter(0, 6, 50, TJAPlayer3.Timer);
            

            this.ctSlideAnimation = new CCounter();
            this.ctClimbAnimation = new CCounter();
            this.ctDonAnimation = new CCounter(0, 1000, 24000f / ((float)TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM[0] * TJAPlayer3.ConfigIni.n演奏速度 / 20), TJAPlayer3.Timer);


            base.Activate();
        }

        public override void DeActivate()
        {
            if (this.IsDeActivated)
                return;

            TJAPlayer3.t安全にDisposeする(ref UpScript);
            TJAPlayer3.t安全にDisposeする(ref DownScript);

            TJAPlayer3.t安全にDisposeする(ref pfTowerText);

            base.DeActivate();
        }

        public override void CreateManagedResource()
        {
            base.CreateManagedResource();
        }

        public override void ReleaseManagedResource()
        {
            base.ReleaseManagedResource();
        }

        public override int Draw()
        {
            if (base.IsDeActivated)
                return 0;


            //this.ct上背景FIFOタイマー?.t進行();


            #region [Tower specific variables declaration]

            float currentFloorPositionMax140 = 0;

            #endregion

            // fNow_Measure_s (/ m)

            #region [Upper background]

            if (!IsUpNotFound)
            {
                if (!TJAPlayer3.stage演奏ドラム画面.bPAUSE) UpScript.Update();
                UpScript.Draw();
                if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
                {
                    #region [Tower animations variables]

                    this.bFloorChanged = CFloorManagement.LastRegisteredFloor > 0 && (CFloorManagement.LastRegisteredFloor < TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] + 1);

                    int maxFloor = TJAPlayer3.stage選曲.r確定された曲.arスコア[5].譜面情報.nTotalFloor;
                    int nightTime = Math.Max(140, maxFloor / 2);

                    currentFloorPositionMax140 = Math.Min(TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] / (float)nightTime, 1f);

                    #endregion

                    #region [Tower background informations]

                    if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
                    {
                        TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(ttkTouTatsuKaiSuu).t2D描画(TJAPlayer3.Skin.Game_Tower_Font_TouTatsuKaiSuu[0], TJAPlayer3.Skin.Game_Tower_Font_TouTatsuKaiSuu[1]);
                        TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(ttkKai).t2D描画(TJAPlayer3.Skin.Game_Tower_Font_Kai[0], TJAPlayer3.Skin.Game_Tower_Font_Kai[1]);

                        this.ct炎.TickLoop();
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

                            TJAPlayer3.Tx.Taiko_Combo[0].t2D描画(TJAPlayer3.Skin.Game_Tower_Floor_Number[0] - ((digitLength - 8) * (len - idx) * 1.4f),
                                TJAPlayer3.Skin.Game_Tower_Floor_Number[1],
                                new Rectangle(digitLength * currentNum, 0,
                                    digitLength, TJAPlayer3.Tx.Taiko_Combo[0].szテクスチャサイズ.Height));
                        }

                        #endregion

                        #region [Life Tamashii icon]

                        int soulfire_width = TJAPlayer3.Tx.Gauge_Soul_Fire.szテクスチャサイズ.Width / 8;
                        int soulfire_height = TJAPlayer3.Tx.Gauge_Soul_Fire.szテクスチャサイズ.Height;

                        int soul_height = TJAPlayer3.Tx.Gauge_Soul.szテクスチャサイズ.Height / 2;

                        TJAPlayer3.Tx.Gauge_Soul_Fire?.t2D描画(TJAPlayer3.Skin.Gauge_Soul_Fire_X_Tower, TJAPlayer3.Skin.Gauge_Soul_Fire_Y_Tower, new Rectangle(soulfire_width * (this.ct炎.CurrentValue), 0, soulfire_width, soulfire_height));
                        TJAPlayer3.Tx.Gauge_Soul?.t2D描画(TJAPlayer3.Skin.Gauge_Soul_X_Tower, TJAPlayer3.Skin.Gauge_Soul_Y_Tower, new Rectangle(0, soul_height, TJAPlayer3.Tx.Gauge_Soul.szテクスチャサイズ.Width, soul_height));

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

                            TJAPlayer3.Tx.Taiko_Combo[0].t2D描画(TJAPlayer3.Skin.Game_Tower_Life_Number[0] + ((digitLength - 8) * (len - idx) * 1.1f),
                                TJAPlayer3.Skin.Game_Tower_Life_Number[1],
                                new Rectangle(digitLength * currentNum, 0,
                                    digitLength, TJAPlayer3.Tx.Taiko_Combo[0].szテクスチャサイズ.Height));
                        }

                        TJAPlayer3.Tx.Taiko_Combo[0].color4 = new Color4(1f, 1f, 1f, 1f);

                        #endregion

                    }

                    #endregion
                }
            }

            #endregion

            #region [Lower background]


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
                    ctSlideAnimation.Start(0, 1000, 120f / ((float)TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM[0] * TJAPlayer3.ConfigIni.n演奏速度 / 20), TJAPlayer3.Timer);

                float progressFactor = (nextPositionMax140 - currentFloorPositionMax140) * (ctSlideAnimation.CurrentValue / 1000f);



                #region [Skybox]

                int skyboxYPosition = (int)((TJAPlayer3.Tx.Tower_Sky_Gradient.szテクスチャサイズ.Height - TJAPlayer3.Skin.Game_Tower_Sky_Gradient_Size[1]) * (1f - (currentFloorPositionMax140 + progressFactor)));

                TJAPlayer3.Tx.Tower_Sky_Gradient?.t2D描画(TJAPlayer3.Skin.Game_Tower_Sky_Gradient[0], TJAPlayer3.Skin.Game_Tower_Sky_Gradient[1], 
                    new Rectangle(0, skyboxYPosition, TJAPlayer3.Skin.Game_Tower_Sky_Gradient_Size[0], TJAPlayer3.Skin.Game_Tower_Sky_Gradient_Size[1]));

                #endregion


                #region [Tower body]

                progressFactor = ctSlideAnimation.CurrentValue / 1000f;

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

                int widthChange = (int)(progressFactor * TJAPlayer3.Skin.Game_Tower_Floors_Move[0]);
                int heightChange = (int)(progressFactor * TJAPlayer3.Skin.Game_Tower_Floors_Move[1]);

                // Current trunk
                if (TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] < maxFloor)
                    TJAPlayer3.Tx.Tower_Base[currentTower][towerBase]?.t2D下中央基準描画(
                        TJAPlayer3.Skin.Game_Tower_Floors_Body[0] + widthChange, 
                        TJAPlayer3.Skin.Game_Tower_Floors_Body[1] + heightChange); // 316 + 360
                else
                    TJAPlayer3.Tx.Tower_Top[currentTower]?.t2D下中央基準描画(
                        TJAPlayer3.Skin.Game_Tower_Floors_Body[0] + widthChange, 
                        TJAPlayer3.Skin.Game_Tower_Floors_Body[1] + heightChange);

                // Current deco
                TJAPlayer3.Tx.Tower_Deco[currentTower][currentDeco]?.t2D下中央基準描画(
                    TJAPlayer3.Skin.Game_Tower_Floors_Deco[0] + widthChange,
                    TJAPlayer3.Skin.Game_Tower_Floors_Deco[1] + heightChange);

                int originY = TJAPlayer3.Skin.Game_Tower_Floors_Move[1] - heightChange;

                // Next trunk
                if (TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] + 1 < maxFloor)
                    TJAPlayer3.Tx.Tower_Base[currentTower][nextTowerBase]?.t2D下中央基準描画(
                        TJAPlayer3.Skin.Game_Tower_Floors_Body[0] - TJAPlayer3.Skin.Game_Tower_Floors_Move[0] + widthChange, 
                        TJAPlayer3.Skin.Game_Tower_Floors_Body[1] - TJAPlayer3.Skin.Game_Tower_Floors_Move[1] + heightChange,
                        new Rectangle(0, originY, TJAPlayer3.Tx.Tower_Base[currentTower][nextTowerBase].szテクスチャサイズ.Width, TJAPlayer3.Tx.Tower_Base[currentTower][nextTowerBase].szテクスチャサイズ.Height - originY));
                else if (TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] + 1 == maxFloor)
                {
                    TJAPlayer3.Tx.Tower_Top[currentTower]?.t2D下中央基準描画(
                        TJAPlayer3.Skin.Game_Tower_Floors_Body[0] - TJAPlayer3.Skin.Game_Tower_Floors_Move[0] + widthChange,
                        TJAPlayer3.Skin.Game_Tower_Floors_Body[1] - TJAPlayer3.Skin.Game_Tower_Floors_Move[1] + heightChange,
                        new Rectangle(0, originY, TJAPlayer3.Tx.Tower_Top[currentTower].szテクスチャサイズ.Width, TJAPlayer3.Tx.Tower_Top[currentTower].szテクスチャサイズ.Height - originY));
                }

                // Next deco
                if (TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] + 1 <= maxFloor)
                    TJAPlayer3.Tx.Tower_Deco[currentTower][nextDeco]?.t2D下中央基準描画(
                        TJAPlayer3.Skin.Game_Tower_Floors_Deco[0] - TJAPlayer3.Skin.Game_Tower_Floors_Move[0] + widthChange,
                        TJAPlayer3.Skin.Game_Tower_Floors_Deco[1] - TJAPlayer3.Skin.Game_Tower_Floors_Move[1] + heightChange);


                #endregion

                #region [Climbing don]

                // Will be added in a future skinning update
                int currentDon = 0;

                if (bFloorChanged == true)
                {
                    ctClimbAnimation.Start(0, 1500, 120f / ((float)TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM[0] * TJAPlayer3.ConfigIni.n演奏速度 / 20), TJAPlayer3.Timer);
                    ctDonAnimation.Start(0, 1000, 24000f / ((float)TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM[0] * TJAPlayer3.ConfigIni.n演奏速度 / 20), TJAPlayer3.Timer);
                }


                if (ctClimbAnimation.CurrentValue == 0 || ctClimbAnimation.CurrentValue == 1500)
                {
                    int animDon = ctDonAnimation.CurrentValue % TJAPlayer3.Skin.Game_Tower_Ptn_Don_Standing[currentDon];
                    TJAPlayer3.Tx.Tower_Don_Standing[currentDon][animDon]?.t2D下中央基準描画(TJAPlayer3.Skin.Game_Tower_Don[0], TJAPlayer3.Skin.Game_Tower_Don[1]); // Center X - 50
                }
                else if (ctClimbAnimation.CurrentValue <= 1000)
                {
                    int animDon = ctDonAnimation.CurrentValue % TJAPlayer3.Skin.Game_Tower_Ptn_Don_Climbing[currentDon];
                    int distDonX = (int)(ctClimbAnimation.CurrentValue * (TJAPlayer3.Skin.Game_Tower_Don_Move[0] / 1000f));
                    int distDonY = (int)(ctClimbAnimation.CurrentValue * (TJAPlayer3.Skin.Game_Tower_Don_Move[1] / 1000f));
                    TJAPlayer3.Tx.Tower_Don_Climbing[currentDon][animDon]?.t2D下中央基準描画(TJAPlayer3.Skin.Game_Tower_Don[0] + distDonX, TJAPlayer3.Skin.Game_Tower_Don[1] + distDonY);
                }
                else
                {
                    int animDon = ctDonAnimation.CurrentValue % TJAPlayer3.Skin.Game_Tower_Ptn_Don_Running[currentDon];
                    int distDonX = (int)((1500 - ctClimbAnimation.CurrentValue) * (TJAPlayer3.Skin.Game_Tower_Don_Move[0] / 500f));
                    int distDonY = (int)((1500 - ctClimbAnimation.CurrentValue) * (TJAPlayer3.Skin.Game_Tower_Don_Move[1] / 500f));
                    TJAPlayer3.Tx.Tower_Don_Running[currentDon][animDon]?.t2D下中央基準描画(TJAPlayer3.Skin.Game_Tower_Don[0] + distDonX, TJAPlayer3.Skin.Game_Tower_Don[1] + distDonY);
                }

                #endregion

                #region [Miss icon]

                if (CFloorManagement.InvincibilityFrames != null && CFloorManagement.InvincibilityFrames.CurrentValue < CFloorManagement.InvincibilityDuration)
                {
                    if (TJAPlayer3.Tx.Tower_Miss != null)
                        TJAPlayer3.Tx.Tower_Miss.Opacity = Math.Min(255, 1000 - CFloorManagement.InvincibilityFrames.CurrentValue);
                    TJAPlayer3.Tx.Tower_Miss?.t2D下中央基準描画(TJAPlayer3.Skin.Game_Tower_Miss[0], TJAPlayer3.Skin.Game_Tower_Miss[1]);
                }

                #endregion

                ctSlideAnimation?.Tick();
                ctClimbAnimation?.Tick();
                ctDonAnimation?.TickLoop();

                #endregion
            }
            else if (!TJAPlayer3.stage演奏ドラム画面.bDoublePlay && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan)
            {
                if (!IsDownNotFound)
                {
                    if (!TJAPlayer3.stage演奏ドラム画面.bPAUSE) DownScript.Update();
                    DownScript.Draw();
                }
            }


            #endregion

            return base.Draw();
        }

        #region[ private ]
        //-----------------

        #region 背景
        /*private CTexture Background,
            Background_Down,
            Background_Down_Clear,
            Background_Down_Scroll;
        private CTexture[] Background_Up_1st,
                          Background_Up_2nd,
                          Background_Up_3rd,
                          Background_Up_Dan = new CTexture[6],
                          Background_Up_Tower = new CTexture[8];*/
        #endregion

        /*private CCounter[] ct上背景スクロール用タイマー1st; //上背景のX方向スクロール用
        private CCounter[] ct上背景スクロール用タイマー2nd; //上背景のY方向スクロール用
        private CCounter[] ct上背景スクロール用タイマー3rd; //上背景のY方向スクロール用
        private CCounter ct下背景スクロール用タイマー1; //下背景パーツ1のX方向スクロール用
        private CCounter ct上背景FIFOタイマー;
        private CCounter[] ct上背景クリアインタイマー;
        private CCounter[] ct上背景スクロール用タイマー1stDan;   //上背景のX方向スクロール用
        private CCounter ct上背景スクロール用タイマー2stDan;   //上背景のY方向スクロール用
        
        private CCounter[] ct上背景スクロール用タイマー1stTower;   //上背景のX方向スクロール用
        private CCounter ct上背景スクロール用タイマー2stTower;   //上背景のX方向スクロール用
        */
        //private CTexture tx上背景メイン;
        //private CTexture tx上背景クリアメイン;
        //private CTexture tx下背景メイン;
        //private CTexture tx下背景クリアメイン;
        //private CTexture tx下背景クリアサブ1;

        private ScriptBG UpScript;
        private ScriptBG DownScript;

        private TitleTextureKey ttkTouTatsuKaiSuu;
        private TitleTextureKey ttkKai;
        private CCachedFontRenderer pfTowerText;

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
