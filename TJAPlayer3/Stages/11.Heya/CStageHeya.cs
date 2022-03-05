using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using FDK;
using SharpDX;
using static TJAPlayer3.CActSelect曲リスト;

using Rectangle = System.Drawing.Rectangle;
using RectangleF = System.Drawing.RectangleF;
using Color = System.Drawing.Color;

namespace TJAPlayer3
{
    class CStageHeya : CStage
    {
        public CStageHeya()
        {
            base.eステージID = Eステージ.Heya;
            base.eフェーズID = CStage.Eフェーズ.共通_通常状態;

            base.list子Activities.Add(this.actFOtoTitle = new CActFIFOBlack());

            base.list子Activities.Add(this.PuchiChara = new PuchiChara());
        }


        public override void On活性化()
        {
            if (base.b活性化してる)
                return;

            base.eフェーズID = CStage.Eフェーズ.共通_通常状態;
            this.eフェードアウト完了時の戻り値 = E戻り値.継続;

            ctDonchan_In = new CCounter();
            ctDonchan_Normal = new CCounter(0, TJAPlayer3.Tx.SongSelect_Donchan_Normal.Length - 1, 1000 / 45, TJAPlayer3.Timer);

            bInSongPlayed = false;


            if (!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName))
                this.pfHeyaFont = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 14);
            else
                this.pfHeyaFont = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 14);


            // 1P, configure later for default 2P
            iPlayer = TJAPlayer3.SaveFile;

            #region [Main menu]

            this.ttkMainMenuOpt = new TitleTextureKey[5];
            
            for (int i = 0; i < ttkMainMenuOpt.Length; i++)
            {
                this.ttkMainMenuOpt[i] = new TitleTextureKey(CLangManager.LangInstance.GetString(1030 + i), this.pfHeyaFont, Color.White, Color.DarkGreen, 1000);
            }

            #endregion

            #region [Dan title]

            int amount = 1;
            if (TJAPlayer3.NamePlateConfig.data.DanTitles[iPlayer] != null)
                amount += TJAPlayer3.NamePlateConfig.data.DanTitles[iPlayer].Count;

            this.ttkDanTitles = new TitleTextureKey[amount];

            // Silver Shinjin (default rank) always avaliable by default
            this.ttkDanTitles[0] = new TitleTextureKey("新人", this.pfHeyaFont, Color.White, Color.Black, 1000);

            int idx = 1;
            if (TJAPlayer3.NamePlateConfig.data.DanTitles[iPlayer] != null)
            {
                foreach (var item in TJAPlayer3.NamePlateConfig.data.DanTitles[iPlayer])
                {
                    if (item.Value.isGold == true)
                        this.ttkDanTitles[idx] = new TitleTextureKey(item.Key, this.pfHeyaFont, Color.Gold, Color.Black, 1000);
                    else 
                        this.ttkDanTitles[idx] = new TitleTextureKey(item.Key, this.pfHeyaFont, Color.White, Color.Black, 1000);
                    idx++;
                }
            }

            #endregion

            #region [Plate title]

            amount = 1;
            if (TJAPlayer3.NamePlateConfig.data.NamePlateTitles[iPlayer] != null)
                amount += TJAPlayer3.NamePlateConfig.data.NamePlateTitles[iPlayer].Count;

            this.ttkTitles = new TitleTextureKey[amount];

            // Wood shojinsha (default title) always avaliable by default
            this.ttkTitles[0] = new TitleTextureKey("初心者", this.pfHeyaFont, Color.Black, Color.Transparent, 1000);

            idx = 1;
            if (TJAPlayer3.NamePlateConfig.data.NamePlateTitles[iPlayer] != null)
            {
                foreach (var item in TJAPlayer3.NamePlateConfig.data.NamePlateTitles[iPlayer])
                {
                    this.ttkTitles[idx] = new TitleTextureKey(item.Key, this.pfHeyaFont, Color.Black, Color.Transparent, 1000);
                    idx++;
                }
            }

            #endregion

            // -1 : Main Menu, >= 0 : See Main menu opt
            iCurrentMenu = -1;
            iMainMenuCurrent = 0;

            #region [PuchiChara stuff]

            // Tmp variables
            iPuchiCharaCount = 120;

            ttkPuchiCharaNames = new TitleTextureKey[iPuchiCharaCount];
            ttkPuchiCharaAuthors = new TitleTextureKey[iPuchiCharaCount];

            var RarityToColor = new Dictionary<string, Color>
            {
                ["Common"] = Color.White,
                ["Uncommon"] = Color.Lime,
                ["Rare"] = Color.Blue,
                ["Epic"] = Color.Purple,
                ["Legendary"] = Color.Orange,
            };

            var dbData = TJAPlayer3.Databases.DBPuchichara.data;

            for (int i = 0; i < iPuchiCharaCount; i++)
            {
                if (dbData.ContainsKey(i))
                {
                    Color textColor = Color.White;

                    string rarity = dbData[i].Rarity;

                    if (RarityToColor.ContainsKey(rarity))
                        textColor = RarityToColor[rarity];

                    ttkPuchiCharaNames[i] = new TitleTextureKey(dbData[i].Name, this.pfHeyaFont, textColor, Color.Black, 1000);
                    ttkPuchiCharaAuthors[i] = new TitleTextureKey(dbData[i].Author, this.pfHeyaFont, Color.White, Color.Black, 1000);
                }
            }

            #endregion

            iCharacterCount = TJAPlayer3.Skin.Characters_Ptn;

            this.tResetOpts();

            this.PuchiChara.IdleAnimation();

            base.On活性化();
        }

        public override void On非活性化()
        {
            base.On非活性化();
        }

        public override void OnManagedリソースの作成()
        {
            base.OnManagedリソースの作成();
        }

        public override void OnManagedリソースの解放()
        {
            base.OnManagedリソースの解放();
        }

        public override int On進行描画()
        {
            ctDonchan_Normal.t進行Loop();
            ctDonchan_In.t進行();

            TJAPlayer3.Tx.Heya_Background.t2D描画(TJAPlayer3.app.Device, 0, 0);

            #region [Menus display]

            #region [Main menu (Side bar)]

            for (int i = 0; i < this.ttkMainMenuOpt.Length; i++)
            {
                CTexture tmpTex = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(this.ttkMainMenuOpt[i]);

                if (iCurrentMenu != -1 || iMainMenuCurrent != i)
                {
                    tmpTex.color4 = C変換.ColorToColor4(Color.DarkGray);
                    TJAPlayer3.Tx.Heya_Side_Menu.color4 = C変換.ColorToColor4(Color.DarkGray);
                }
                else
                {
                    tmpTex.color4 = C変換.ColorToColor4(Color.White);
                    TJAPlayer3.Tx.Heya_Side_Menu.color4 = C変換.ColorToColor4(Color.White);
                }

                TJAPlayer3.Tx.Heya_Side_Menu.t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, 164, 26 + 80 * i);
                tmpTex.t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, 164, 40 + 80 * i);
            }

            #endregion

            #region [Petit chara]

            if (iCurrentMenu == 0)
            {
                for (int i = -5; i < 6; i++)
                {
                    int pos = (iPuchiCharaCount * 5 + iPuchiCharaCurrent + i) % iPuchiCharaCount;

                    if (i != 0)
                    {
                        TJAPlayer3.Tx.PuchiChara.color4 = C変換.ColorToColor4(Color.DarkGray);
                        TJAPlayer3.Tx.Heya_Center_Menu_Box_Slot.color4 = C変換.ColorToColor4(Color.DarkGray);
                        TJAPlayer3.Tx.Heya_Lock.color4 = C変換.ColorToColor4(Color.DarkGray);
                    }
                    else
                    {
                        TJAPlayer3.Tx.PuchiChara.color4 = C変換.ColorToColor4(Color.White);
                        TJAPlayer3.Tx.Heya_Center_Menu_Box_Slot.color4 = C変換.ColorToColor4(Color.White);
                        TJAPlayer3.Tx.Heya_Lock.color4 = C変換.ColorToColor4(Color.White);
                    }

                    TJAPlayer3.Tx.Heya_Center_Menu_Box_Slot.t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, 620 + 302 * i, 200);

                    int puriColumn = pos % 5;
                    int puriRow = pos / 5;
                    
                    TJAPlayer3.Tx.PuchiChara.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 620 + 302 * i, 320 + (int)(PuchiChara.sineY), 
                        new Rectangle((PuchiChara.Counter.n現在の値 + 2 * puriColumn) * TJAPlayer3.Skin.Game_PuchiChara[0], 
                        puriRow * TJAPlayer3.Skin.Game_PuchiChara[1], 
                        TJAPlayer3.Skin.Game_PuchiChara[0], 
                        TJAPlayer3.Skin.Game_PuchiChara[1]));

                    TJAPlayer3.Tx.PuchiChara.color4 = C変換.ColorToColor4(Color.White);

                    #region [Database related values]

                    if (ttkPuchiCharaNames[pos] != null)
                    {
                        CTexture tmpTex = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(ttkPuchiCharaNames[pos]);

                        tmpTex.t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, 620 + 302 * i, 434);
                    }

                    if (ttkPuchiCharaAuthors[pos] != null)
                    {
                        CTexture tmpTex = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(ttkPuchiCharaAuthors[pos]);

                        tmpTex.t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, 620 + 302 * i, 460);
                    }

                    if (puchiUnlockables.ContainsKey(pos)
                        && !TJAPlayer3.NamePlateConfig.data.UnlockedPuchicharas[iPlayer].Contains(pos))
                        TJAPlayer3.Tx.Heya_Lock?.t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, 620 + 302 * i, 200);

                    #endregion


                }
            }

            #endregion

            #region [Character]

            if (iCurrentMenu == 1)
            {
                for (int i = -5; i < 6; i++)
                {
                    int pos = (iCharacterCount * 5 + iCharacterCurrent + i) % iCharacterCount;

                    if (i != 0)
                    {
                        if (TJAPlayer3.Tx.Characters_Heya_Preview[pos] != null)
                            TJAPlayer3.Tx.Characters_Heya_Preview[pos].color4 = C変換.ColorToColor4(Color.DarkGray);
                        TJAPlayer3.Tx.Heya_Center_Menu_Box_Slot.color4 = C変換.ColorToColor4(Color.DarkGray);
                    }
                    else
                    {
                        if (TJAPlayer3.Tx.Characters_Heya_Preview[pos] != null)
                            TJAPlayer3.Tx.Characters_Heya_Preview[pos].color4 = C変換.ColorToColor4(Color.White);
                        TJAPlayer3.Tx.Heya_Center_Menu_Box_Slot.color4 = C変換.ColorToColor4(Color.White);
                    }

                    TJAPlayer3.Tx.Heya_Center_Menu_Box_Slot.t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, 620 + 302 * i, 200);

                    TJAPlayer3.Tx.Characters_Heya_Preview[pos]?.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 620 + 302 * i, 320);

                    if (TJAPlayer3.Tx.Characters_Heya_Preview[pos] != null)
                        TJAPlayer3.Tx.Characters_Heya_Preview[pos].color4 = C変換.ColorToColor4(Color.White);
                }
            }

            #endregion

            #region [Dan title]

            if (iCurrentMenu == 2)
            {
                for (int i = -5; i < 6; i++)
                {
                    int pos = (this.ttkDanTitles.Length * 5 + iDanTitleCurrent + i) % this.ttkDanTitles.Length;

                    CTexture tmpTex = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(this.ttkDanTitles[pos]);

                    if (i != 0)
                    {
                        tmpTex.color4 = C変換.ColorToColor4(Color.DarkGray);
                        TJAPlayer3.Tx.Heya_Side_Menu.color4 = C変換.ColorToColor4(Color.DarkGray);
                        TJAPlayer3.Tx.NamePlateBase.color4 = C変換.ColorToColor4(Color.DarkGray);
                    }
                    else
                    {
                        tmpTex.color4 = C変換.ColorToColor4(Color.White);
                        TJAPlayer3.Tx.Heya_Side_Menu.color4 = C変換.ColorToColor4(Color.White);
                        TJAPlayer3.Tx.NamePlateBase.color4 = C変換.ColorToColor4(Color.White);
                    }

                    int danGrade = 0;
                    if (pos > 0)
                    {
                        danGrade = TJAPlayer3.NamePlateConfig.data.DanTitles[iPlayer][this.ttkDanTitles[pos].str文字].clearStatus;
                    }

                    TJAPlayer3.Tx.Heya_Side_Menu.t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, 730 + -10 * Math.Abs(i), 340 + 70 * i);

                    TJAPlayer3.Tx.NamePlateBase.t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device,
                        718 + -10 * Math.Abs(i),
                        331 + 70 * i,
                        new RectangleF(0, (8 + danGrade) * 54, 220, 54));
                    TJAPlayer3.Tx.NamePlateBase.color4 = C変換.ColorToColor4(Color.White);

                    tmpTex.t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, 730 + -10 * Math.Abs(i), 354 + 70 * i);

                    
                }
            }

            #endregion

            #region [Title plate]

            if (iCurrentMenu == 3)
            {
                for (int i = -5; i < 6; i++)
                {
                    int pos = (this.ttkTitles.Length * 5 + iTitleCurrent + i) % this.ttkTitles.Length;

                    CTexture tmpTex = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(this.ttkTitles[pos]);

                    if (i != 0)
                    {
                        tmpTex.color4 = C変換.ColorToColor4(Color.DarkGray);
                        TJAPlayer3.Tx.Heya_Side_Menu.color4 = C変換.ColorToColor4(Color.DarkGray);
                    }
                    else
                    {
                        tmpTex.color4 = C変換.ColorToColor4(Color.White);
                        TJAPlayer3.Tx.Heya_Side_Menu.color4 = C変換.ColorToColor4(Color.White);
                    }

                    TJAPlayer3.Tx.Heya_Side_Menu.t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, 730 + -10 * Math.Abs(i), 340 + 70 * i);

                    int iType = -1;

                    if (TJAPlayer3.NamePlateConfig.data.NamePlateTitles[iPlayer] != null &&
                        TJAPlayer3.NamePlateConfig.data.NamePlateTitles[iPlayer].ContainsKey(this.ttkTitles[pos].str文字))
                        iType = TJAPlayer3.NamePlateConfig.data.NamePlateTitles[iPlayer][this.ttkTitles[pos].str文字].iType;
                    else if (pos == 0)
                        iType = 0;

                    if (iType >= 0 && iType < TJAPlayer3.Skin.Config_NamePlate_Ptn_Title)
                    {
                        TJAPlayer3.Tx.NamePlate_Title[iType][TJAPlayer3.NamePlate.ctAnimatedNamePlateTitle.n現在の値 % TJAPlayer3.Skin.Config_NamePlate_Ptn_Title_Boxes[iType]].t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device,
                            730 + -10 * Math.Abs(i),
                            348 + 70 * i);
                    } 

                    tmpTex.t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, 730 + -10 * Math.Abs(i), 354 + 70 * i);

                }
            }

            #endregion


            #endregion

            #region [Unlockable information zone]

            if (iCurrentMenu >= 0)
            {
                if (this.ttkInfoSection != null && this.ttkInfoSection.str文字 != "")
                    TJAPlayer3.Tx.Heya_Box?.t2D描画(TJAPlayer3.app.Device, 0, 0);

                if (this.ttkInfoSection != null)
                    TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(this.ttkInfoSection)
                        .t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, 620, 560);
            }

            #endregion

            #region [General Don animations]

            if (!ctDonchan_In.b開始した)
            {
                TJAPlayer3.Skin.soundHeyaBGM.t再生する();
                ctDonchan_In.t開始(0, 180, 1.25f, TJAPlayer3.Timer);    
            }

            TJAPlayer3.NamePlate.tNamePlateDraw(TJAPlayer3.Skin.SongSelect_NamePlate_X[0], TJAPlayer3.Skin.SongSelect_NamePlate_Y[0] + 5, 0);

            #region [ どんちゃん関連 ]

            if (ctDonchan_In.n現在の値 != 90)
                {
                    float DonchanX = 0f, DonchanY = 0f;

                    DonchanX = (float)Math.Sin(ctDonchan_In.n現在の値 / 2 * (Math.PI / 180)) * 200f;
                    DonchanY = ((float)Math.Sin((90 + (ctDonchan_In.n現在の値 / 2)) * (Math.PI / 180)) * 150f);

                    TJAPlayer3.Tx.SongSelect_Donchan_Normal[ctDonchan_Normal.n現在の値].Opacity = ctDonchan_In.n現在の値 * 2;
                    TJAPlayer3.Tx.SongSelect_Donchan_Normal[ctDonchan_Normal.n現在の値].t2D描画(TJAPlayer3.app.Device, -200 + DonchanX, 336 - DonchanY);

                    #region [PuchiChara]

                    this.PuchiChara.On進行描画(0 + 100, 336 + 230, false);

                    #endregion
                }

            #endregion

            #endregion

            #region [ キー関連 ]

            if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.RightArrow) ||
                TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RBlue))
            {
                if (this.tMove(1))
                {
                    TJAPlayer3.Skin.sound変更音.t再生する();
                }
            }

            else if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.LeftArrow) ||
                TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LBlue))
            {
                if (this.tMove(-1))
                {
                    TJAPlayer3.Skin.sound変更音.t再生する();
                }
            }

            else if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.Return) ||
                TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LRed) ||
                TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RRed))
            {

                #region [Decide]

                ESelectStatus ess = ESelectStatus.SELECTED;

                // Return to main menu
                if (iCurrentMenu == -1 && iMainMenuCurrent == 0)
                {
                    TJAPlayer3.Skin.soundHeyaBGM.t停止する();
                    this.eフェードアウト完了時の戻り値 = E戻り値.タイトルに戻る;
                    this.actFOtoTitle.tフェードアウト開始();
                    base.eフェーズID = CStage.Eフェーズ.共通_フェードアウト;
                }

                else if (iCurrentMenu == -1)
                {
                    iCurrentMenu = iMainMenuCurrent - 1;

                    if (iCurrentMenu == 0)
                        this.tUpdateUnlockableTextPuchi();
                }

                else if (iCurrentMenu == 0)
                {
                    ess = this.tSelectPuchi();

                    if (ess == ESelectStatus.SELECTED)
                    {
                        TJAPlayer3.NamePlateConfig.data.PuchiChara[iPlayer] = iPuchiCharaCurrent;

                        TJAPlayer3.NamePlateConfig.tApplyHeyaChanges();

                        iCurrentMenu = -1;
                        this.tResetOpts();
                    }
                    else if (ess == ESelectStatus.SUCCESS)
                    {
                        TJAPlayer3.NamePlateConfig.data.UnlockedPuchicharas[iPlayer].Add(iPuchiCharaCurrent);

                        TJAPlayer3.NamePlateConfig.tSpendCoins(puchiUnlockables[iPuchiCharaCurrent].Values[0], iPlayer);
                    }
                }

                else if (iCurrentMenu == 1)
                {
                    // Reload character, a bit time expensive but with a O(N) memory complexity instead of O(N * M)
                    TJAPlayer3.Tx.ReloadCharacter(TJAPlayer3.NamePlateConfig.data.Character[iPlayer], iCharacterCurrent, iPlayer);

                    TJAPlayer3.NamePlateConfig.data.Character[iPlayer] = iCharacterCurrent;

                    TJAPlayer3.NamePlateConfig.tApplyHeyaChanges();

                    iCurrentMenu = -1;
                    this.tResetOpts();
                }

                else if (iCurrentMenu == 2)
                {
                    bool iG = false;
                    int cs = 0;

                    if (iDanTitleCurrent > 0)
                    {
                        iG = TJAPlayer3.NamePlateConfig.data.DanTitles[iPlayer][this.ttkDanTitles[iDanTitleCurrent].str文字].isGold;
                        cs = TJAPlayer3.NamePlateConfig.data.DanTitles[iPlayer][this.ttkDanTitles[iDanTitleCurrent].str文字].clearStatus;
                    }

                    TJAPlayer3.NamePlateConfig.data.Dan[iPlayer] = this.ttkDanTitles[iDanTitleCurrent].str文字;
                    TJAPlayer3.NamePlateConfig.data.DanGold[iPlayer] = iG;
                    TJAPlayer3.NamePlateConfig.data.DanType[iPlayer] = cs;

                    TJAPlayer3.NamePlate.tNamePlateRefreshTitles(iPlayer);

                    TJAPlayer3.NamePlateConfig.tApplyHeyaChanges();

                    iCurrentMenu = -1;
                    this.tResetOpts();
                }

                else if (iCurrentMenu == 3)
                {
                    TJAPlayer3.NamePlateConfig.data.Title[iPlayer] = this.ttkTitles[iTitleCurrent].str文字;

                    if (TJAPlayer3.NamePlateConfig.data.NamePlateTitles[iPlayer] != null
                        && TJAPlayer3.NamePlateConfig.data.NamePlateTitles[iPlayer].ContainsKey(this.ttkTitles[iTitleCurrent].str文字))
                        TJAPlayer3.NamePlateConfig.data.TitleType[iPlayer] = TJAPlayer3.NamePlateConfig.data.NamePlateTitles[iPlayer][this.ttkTitles[iTitleCurrent].str文字].iType;
                    else if (iTitleCurrent == 0)
                        TJAPlayer3.NamePlateConfig.data.TitleType[iPlayer] = 0;
                    else
                        TJAPlayer3.NamePlateConfig.data.TitleType[iPlayer] = -1;

                    TJAPlayer3.NamePlate.tNamePlateRefreshTitles(iPlayer);

                    TJAPlayer3.NamePlateConfig.tApplyHeyaChanges();

                    iCurrentMenu = -1;
                    this.tResetOpts();
                }

                if (ess == ESelectStatus.SELECTED)
                    TJAPlayer3.Skin.sound決定音.t再生する();
                else if (ess == ESelectStatus.FAILED)
                    TJAPlayer3.Skin.soundError.t再生する();
                else
                    TJAPlayer3.Skin.SoundBanapas.t再生する(); // To change with a more appropriate sfx sooner or later

                #endregion
            }

            else if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.Escape))
            {
                
                TJAPlayer3.Skin.sound取消音.t再生する();

                if (iCurrentMenu == -1)
                {
                    TJAPlayer3.Skin.soundHeyaBGM.t停止する();
                    this.eフェードアウト完了時の戻り値 = E戻り値.タイトルに戻る;
                    this.actFOtoTitle.tフェードアウト開始();
                    base.eフェーズID = CStage.Eフェーズ.共通_フェードアウト;
                }
                else
                {
                    iCurrentMenu = -1;
                    this.ttkInfoSection = null;
                    this.tResetOpts();
                }
                    

                return 0;
            }

            #endregion

            switch (base.eフェーズID)
            {
                case CStage.Eフェーズ.共通_フェードアウト:
                    if (this.actFOtoTitle.On進行描画() == 0)
                    {
                        break;
                    }
                    return (int)this.eフェードアウト完了時の戻り値;

            }

            return 0;
        }

        public enum E戻り値 : int
        {
            継続,
            タイトルに戻る,
            選曲した
        }

        public bool bInSongPlayed;

        private CCounter ctDonchan_In;
        private CCounter ctDonchan_Normal;

        private PuchiChara PuchiChara;

        private int iPlayer;

        private int iMainMenuCurrent;
        private int iPuchiCharaCurrent;

        private TitleTextureKey[] ttkPuchiCharaNames;
        private TitleTextureKey[] ttkPuchiCharaAuthors;
        private TitleTextureKey ttkInfoSection;

        private int iCharacterCurrent;
        private int iDanTitleCurrent;
        private int iTitleCurrent;

        private int iCurrentMenu;

        private void tResetOpts()
        {
            // Retrieve titles if they exist
            iTitleCurrent = 0;
            iDanTitleCurrent = 0;

            iCharacterCurrent = TJAPlayer3.NamePlateConfig.data.Character[this.iPlayer];
            iPuchiCharaCurrent = TJAPlayer3.NamePlateConfig.data.PuchiChara[this.iPlayer];
        }

        private bool tMove(int off)
        {
            if (iCurrentMenu == -1)
                iMainMenuCurrent = (this.ttkMainMenuOpt.Length + iMainMenuCurrent + off) % this.ttkMainMenuOpt.Length;
            else if (iCurrentMenu == 0)
            {
                iPuchiCharaCurrent = (iPuchiCharaCount + iPuchiCharaCurrent + off) % iPuchiCharaCount;
                tUpdateUnlockableTextPuchi();
            }
            else if (iCurrentMenu == 1)
                iCharacterCurrent = (iCharacterCount + iCharacterCurrent + off) % iCharacterCount;
            else if (iCurrentMenu == 2)
                iDanTitleCurrent = (this.ttkDanTitles.Length + iDanTitleCurrent + off) % this.ttkDanTitles.Length;
            else if (iCurrentMenu == 3)
                iTitleCurrent = (this.ttkTitles.Length + iTitleCurrent + off) % this.ttkTitles.Length;
            else
                return false;

            return true;
        }

        #region [Puchi unlockables]
        private void tUpdateUnlockableTextPuchi()
        {
            #region [Check unlockable]

            if (puchiUnlockables.ContainsKey(iPuchiCharaCurrent)
                && !TJAPlayer3.NamePlateConfig.data.UnlockedPuchicharas[iPlayer].Contains(iPuchiCharaCurrent))
            {
                // To update then when bought unlockables will be implemented
                this.ttkInfoSection = new TitleTextureKey(puchiUnlockables[iPuchiCharaCurrent].tConditionMessage()
                    , this.pfHeyaFont, Color.White, Color.Black, 1000);
            }
            else
                this.ttkInfoSection = null;

            #endregion
        }

        private ESelectStatus tSelectPuchi()
        {
            // Add "If unlocked" to select directly
            if (puchiUnlockables.ContainsKey(iPuchiCharaCurrent) 
                && !TJAPlayer3.NamePlateConfig.data.UnlockedPuchicharas[iPlayer].Contains(iPuchiCharaCurrent))
            {

                (bool, string) response = puchiUnlockables[iPuchiCharaCurrent].tConditionMet(new int[]{ TJAPlayer3.NamePlateConfig.data.Medals[TJAPlayer3.SaveFile] } );
                Color responseColor = (response.Item1) ? Color.Lime : Color.Red;

                // Send coins here for the unlock, considering that only coin-paid puchicharas can be unlocked directly from the Heya menu

                this.ttkInfoSection = new TitleTextureKey(response.Item2, this.pfHeyaFont, responseColor, Color.Black, 1000);

                return (response.Item1) ? ESelectStatus.SUCCESS : ESelectStatus.FAILED;
            }

            this.ttkInfoSection = null;
            return ESelectStatus.SELECTED;
        }

        /*
         *  FAILED : Selection/Purchase failed (failed condition)
         *  SUCCESS : Purchase succeed (without selection)
         *  SELECTED : Selection succeed
        */
        private enum ESelectStatus
        {
            FAILED,
            SUCCESS,
            SELECTED
        };

        #endregion

        private TitleTextureKey[] ttkMainMenuOpt;
        private CPrivateFastFont pfHeyaFont;

        private Dictionary<int, DBUnlockables.CUnlockConditions> puchiUnlockables = TJAPlayer3.Databases.DBUnlockables.data.Puchichara;

        private TitleTextureKey[] ttkDanTitles;

        private TitleTextureKey[] ttkTitles;

        private int iPuchiCharaCount;
        private int iCharacterCount;

        public E戻り値 eフェードアウト完了時の戻り値;

        public CActFIFOBlack actFOtoTitle;
    }
}
