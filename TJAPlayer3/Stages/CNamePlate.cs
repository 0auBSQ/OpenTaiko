using FDK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TJAPlayer3.CActSelect曲リスト;

namespace TJAPlayer3
{
    class CNamePlate
    {
        public void RefleshSkin()
        {
            for (int player = 0; player < 5; player++)
            {
                this.pfName[player]?.Dispose();

                if (!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName))
                {
                    if (TJAPlayer3.NamePlateConfig.data.Title[player] == "" || TJAPlayer3.NamePlateConfig.data.Title[player] == null)
                        this.pfName[player] = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), TJAPlayer3.Skin.NamePlate_Font_Name_Size_Normal);
                    else
                        this.pfName[player] = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), TJAPlayer3.Skin.NamePlate_Font_Name_Size_WithTitle);
                }
                else
                {
                    if (TJAPlayer3.NamePlateConfig.data.Title[player] == "" || TJAPlayer3.NamePlateConfig.data.Title[player] == null)
                        this.pfName[player] = new CPrivateFastFont(new FontFamily("MS UI Gothic"), TJAPlayer3.Skin.NamePlate_Font_Name_Size_Normal);
                    else
                        this.pfName[player] = new CPrivateFastFont(new FontFamily("MS UI Gothic"), TJAPlayer3.Skin.NamePlate_Font_Name_Size_WithTitle);
                }
            }

            this.pfTitle?.Dispose();
            this.pfdan?.Dispose();

            if (!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName))
            {
                this.pfTitle = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), TJAPlayer3.Skin.NamePlate_Font_Title_Size);
                this.pfdan = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), TJAPlayer3.Skin.NamePlate_Font_Dan_Size);
            }
            else
            {
                this.pfTitle = new CPrivateFastFont(new FontFamily("MS UI Gothic"), TJAPlayer3.Skin.NamePlate_Font_Title_Size);
                this.pfdan = new CPrivateFastFont(new FontFamily("MS UI Gothic"), TJAPlayer3.Skin.NamePlate_Font_Dan_Size);
            }
        }

        public CNamePlate()
        {
            RefleshSkin();

            for (int player = 0; player < 5; player++)
            {
                if (TJAPlayer3.NamePlateConfig.data.DanType[player] < 0) TJAPlayer3.NamePlateConfig.data.DanType[player] = 0;
                else if (TJAPlayer3.NamePlateConfig.data.DanType[player] > 2) TJAPlayer3.NamePlateConfig.data.DanType[player] = 2;

                if (TJAPlayer3.NamePlateConfig.data.TitleType[player] < 0) TJAPlayer3.NamePlateConfig.data.TitleType[player] = 0;

                tNamePlateRefreshTitles(player);
            }

            ctNamePlateEffect = new CCounter(0, 120, 16.6f, TJAPlayer3.Timer);
            ctAnimatedNamePlateTitle = new CCounter(0, 10000, 60.0f, TJAPlayer3.Timer);
        }

        public void tNamePlateDisplayNamePlateBase(int x, int y, int item)
        {
            int namePlateBaseX = TJAPlayer3.Tx.NamePlateBase.szテクスチャサイズ.Width;
            int namePlateBaseY = TJAPlayer3.Tx.NamePlateBase.szテクスチャサイズ.Height / 12;

            TJAPlayer3.Tx.NamePlateBase?.t2D描画(TJAPlayer3.app.Device, x, y, new RectangleF(0, item * namePlateBaseY, namePlateBaseX, namePlateBaseY));

        }

        public void tNamePlateDisplayNamePlate_Extension(int x, int y, int item)
        {
            int namePlateBaseX = TJAPlayer3.Tx.NamePlate_Extension.szテクスチャサイズ.Width;
            int namePlateBaseY = TJAPlayer3.Tx.NamePlate_Extension.szテクスチャサイズ.Height / 12;

            TJAPlayer3.Tx.NamePlate_Extension?.t2D描画(TJAPlayer3.app.Device, x, y, new RectangleF(0, item * namePlateBaseY, namePlateBaseX, namePlateBaseY));

        }

        public void tNamePlateRefreshTitles(int player)
        {
            int actualPlayer = TJAPlayer3.GetActualPlayer(player);

            string[] stages = { "初", "二", "三", "四", "五", "六", "七", "八", "九", "極" };

            string name = CLangManager.LangInstance.GetString(910);
            string title = CLangManager.LangInstance.GetString(911);
            string dan = stages[Math.Max(0, TJAPlayer3.ConfigIni.nAILevel - 1)] + "面";

            if (!TJAPlayer3.ConfigIni.bAIBattleMode || actualPlayer == 0)
            {
                name = TJAPlayer3.NamePlateConfig.data.Name[player];
                title = TJAPlayer3.NamePlateConfig.data.Title[player];
                dan = TJAPlayer3.NamePlateConfig.data.Dan[player];
            }

            txTitle[player] = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(new TitleTextureKey(title, pfTitle, Color.Black, Color.Empty, 1000));
            txName[player] = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(new TitleTextureKey(name, pfName[player], Color.White, Color.Black, 1000));
            txdan[player] = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(new TitleTextureKey(dan, pfdan, Color.White, Color.Black, 1000));
        }


        public void tNamePlateDraw(int x, int y, int player, bool bTitle = false, int Opacity = 255)
        {
            int basePlayer = player;
            player = TJAPlayer3.GetActualPlayer(player);

            tNamePlateRefreshTitles(player);

            ctNamePlateEffect.t進行Loop();
            ctAnimatedNamePlateTitle.t進行Loop();

            this.txName[player].Opacity = Opacity;
            this.txTitle[player].Opacity = Opacity;
            this.txdan[player].Opacity = Opacity;

            TJAPlayer3.Tx.NamePlateBase.Opacity = Opacity;


            for (int i = 0; i < 5; i++)
                TJAPlayer3.Tx.NamePlate_Effect[i].Opacity = Opacity;

            // White background
            tNamePlateDisplayNamePlateBase(x, y, 3);

            // Upper (title) plate
            if (TJAPlayer3.NamePlateConfig.data.Title[player] != "" && TJAPlayer3.NamePlateConfig.data.Title[player] != null)
            {
                int tt = TJAPlayer3.NamePlateConfig.data.TitleType[player];
                if (tt >= 0 && tt < TJAPlayer3.Skin.Config_NamePlate_Ptn_Title)
                {
                    var _tex = TJAPlayer3.Tx.NamePlate_Title[tt][ctAnimatedNamePlateTitle.n現在の値 % TJAPlayer3.Skin.Config_NamePlate_Ptn_Title_Boxes[tt]];

                    if (_tex != null)
                    {
                        _tex.Opacity = Opacity;
                        _tex.t2D描画(TJAPlayer3.app.Device, x - 2, y - 2);
                    }
                }
            }

            // Dan plate
            if (TJAPlayer3.NamePlateConfig.data.Dan[player] != "" && TJAPlayer3.NamePlateConfig.data.Dan[player] != null)
            {
                tNamePlateDisplayNamePlateBase(x, y, 7);
                tNamePlateDisplayNamePlateBase(x, y, (8 + TJAPlayer3.NamePlateConfig.data.DanType[player]));
            }

            // Glow
            tNamePlateDraw(player, x, y, Opacity);

            // Player number
            if (TJAPlayer3.PlayerSide == 0 || TJAPlayer3.ConfigIni.nPlayerCount > 1)
            {
                if (basePlayer < 2)
                {
                    tNamePlateDisplayNamePlateBase(x, y, basePlayer == 1 ? 2 : 0);
                }
                else
                {
                    tNamePlateDisplayNamePlate_Extension(x, y, basePlayer - 2);
                }
            }
            else
                tNamePlateDisplayNamePlateBase(x, y, 1);

            // Name text squash (to add to skin config)
            if (TJAPlayer3.NamePlateConfig.data.Dan[player] != "" && TJAPlayer3.NamePlateConfig.data.Dan[player] != null)
            {
                if (txName[player].szテクスチャサイズ.Width >= 120.0f)
                    txName[player].vc拡大縮小倍率.X = 120.0f / txName[player].szテクスチャサイズ.Width;
            }
            else
            {
                if (txName[player].szテクスチャサイズ.Width >= 220.0f)
                    txName[player].vc拡大縮小倍率.X = 220.0f / txName[player].szテクスチャサイズ.Width;
            }

            // Dan text squash (to add to skin config)
            if (txdan[player].szテクスチャサイズ.Width >= 66.0f)
                txdan[player].vc拡大縮小倍率.X = 66.0f / txdan[player].szテクスチャサイズ.Width;

            // Dan text
            if (TJAPlayer3.NamePlateConfig.data.Dan[player] != "" && TJAPlayer3.NamePlateConfig.data.Dan[player] != null)
            {
                this.txdan[player].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x + TJAPlayer3.Skin.NamePlate_Dan_Offset[0], y + TJAPlayer3.Skin.NamePlate_Dan_Offset[1]);

                if (TJAPlayer3.NamePlateConfig.data.DanGold[player])
                {
                    TJAPlayer3.Tx.NamePlateBase.b乗算合成 = true;
                    tNamePlateDisplayNamePlateBase(x, y, 11);
                    TJAPlayer3.Tx.NamePlateBase.b乗算合成 = false;
                }
            }

            // Title text
            if (TJAPlayer3.NamePlateConfig.data.Title[player] != "" && TJAPlayer3.NamePlateConfig.data.Title[player] != null)
            {
                if (txTitle[player].szテクスチャサイズ.Width >= 160)
                {
                    txTitle[player].vc拡大縮小倍率.X = 160.0f / txTitle[player].szテクスチャサイズ.Width;
                    txTitle[player].vc拡大縮小倍率.Y = 160.0f / txTitle[player].szテクスチャサイズ.Width;
                }

                txTitle[player].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x + TJAPlayer3.Skin.NamePlate_Title_Offset[0], y + TJAPlayer3.Skin.NamePlate_Title_Offset[1]);

                // Name text
                if (TJAPlayer3.NamePlateConfig.data.Dan[player] == "" || TJAPlayer3.NamePlateConfig.data.Dan[player] == null)
                    this.txName[player].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x + TJAPlayer3.Skin.NamePlate_Name_Offset_WithTitle[0], y + TJAPlayer3.Skin.NamePlate_Name_Offset_WithTitle[1]);
                else
                    this.txName[player].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x + TJAPlayer3.Skin.NamePlate_Name_Offset_Full[0], y + TJAPlayer3.Skin.NamePlate_Name_Offset_Full[1]);
            }
            else
                this.txName[player].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x + TJAPlayer3.Skin.NamePlate_Name_Offset_Normal[0], y + TJAPlayer3.Skin.NamePlate_Name_Offset_Normal[1]);

            
            // Overlap frame
            tNamePlateDisplayNamePlateBase(x, y, 4);
        }

        private void tNamePlateDraw(int player, int x, int y, int Opacity = 255)
        {
            if (Opacity == 0)
                return;

            if(TJAPlayer3.NamePlateConfig.data.TitleType[player] != 0)
            {
                int Type = TJAPlayer3.NamePlateConfig.data.TitleType[player] - 1;
                if (this.ctNamePlateEffect.n現在の値 <= 10)
                {
                    tNamePlateStarDraw(player, 1.0f - (ctNamePlateEffect.n現在の値 / 10f * 1.0f), x + 63, y + 25);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 3 && this.ctNamePlateEffect.n現在の値 <= 10)
                {
                    tNamePlateStarDraw(player, 1.0f - ((ctNamePlateEffect.n現在の値 - 3) / 7f * 1.0f), x + 38, y + 7);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 6 && this.ctNamePlateEffect.n現在の値 <= 10)
                {
                    tNamePlateStarDraw(player, 1.0f - ((ctNamePlateEffect.n現在の値 - 6) / 4f * 1.0f), x + 51, y + 5);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 8 && this.ctNamePlateEffect.n現在の値 <= 10)
                {
                    tNamePlateStarDraw(player, 0.3f - ((ctNamePlateEffect.n現在の値 - 8) / 2f * 0.3f), x + 110, y + 25);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 11 && this.ctNamePlateEffect.n現在の値 <= 13)
                {
                    tNamePlateStarDraw(player, 1.0f - ((ctNamePlateEffect.n現在の値 - 11) / 2f * 1.0f), x + 38, y + 7);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 11 && this.ctNamePlateEffect.n現在の値 <= 15)
                {
                    tNamePlateStarDraw(player, 1.0f, x + 51, y + 5);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 11 && this.ctNamePlateEffect.n現在の値 <= 17)
                {
                    tNamePlateStarDraw(player, 1.0f - ((ctNamePlateEffect.n現在の値 - 11) / 7f * 1.0f), x + 110, y + 25);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 16 && this.ctNamePlateEffect.n現在の値 <= 20)
                {
                    tNamePlateStarDraw(player, 0.2f - ((ctNamePlateEffect.n現在の値 - 16) / 4f * 0.2f), x + 63, y + 25);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 17 && this.ctNamePlateEffect.n現在の値 <= 20)
                {
                    tNamePlateStarDraw(player, 1.0f - ((ctNamePlateEffect.n現在の値 - 17) / 3f * 1.0f), x + 99, y + 1);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 20 && this.ctNamePlateEffect.n現在の値 <= 24)
                {
                    tNamePlateStarDraw(player, 0.4f, x + 63, y + 25);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 20 && this.ctNamePlateEffect.n現在の値 <= 25)
                {
                    tNamePlateStarDraw(player, 1.0f, x + 99, y + 1);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 20 && this.ctNamePlateEffect.n現在の値 <= 30)
                {
                    tNamePlateStarDraw(player, 0.5f - ((this.ctNamePlateEffect.n現在の値 - 20) / 10f * 0.5f), x + 152, y + 7);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 31 && this.ctNamePlateEffect.n現在の値 <= 37)
                {
                    tNamePlateStarDraw(player, 0.5f - ((this.ctNamePlateEffect.n現在の値 - 31) / 6f * 0.5f), x + 176, y + 8);
                    tNamePlateStarDraw(player, 1.0f - ((this.ctNamePlateEffect.n現在の値 - 31) / 6f * 1.0f), x + 175, y + 25);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 31 && this.ctNamePlateEffect.n現在の値 <= 40)
                {
                    tNamePlateStarDraw(player, 0.9f - ((this.ctNamePlateEffect.n現在の値 - 31) / 9f * 0.9f), x + 136, y + 24);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 34 && this.ctNamePlateEffect.n現在の値 <= 40)
                {
                    tNamePlateStarDraw(player, 0.7f - ((this.ctNamePlateEffect.n現在の値 - 34) / 6f * 0.7f), x + 159, y + 25);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 41 && this.ctNamePlateEffect.n現在の値 <= 42)
                {
                    tNamePlateStarDraw(player, 0.7f, x + 159, y + 25);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 43 && this.ctNamePlateEffect.n現在の値 <= 50)
                {
                    tNamePlateStarDraw(player, 0.8f - ((this.ctNamePlateEffect.n現在の値 - 43) / 7f * 0.8f), x + 196, y + 23);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 51 && this.ctNamePlateEffect.n現在の値 <= 57)
                {
                    tNamePlateStarDraw(player, 0.8f - ((this.ctNamePlateEffect.n現在の値 - 51) / 6f * 0.8f), x + 51, y + 5);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 51 && this.ctNamePlateEffect.n現在の値 <= 52)
                {
                    tNamePlateStarDraw(player, 0.2f, x + 166, y + 22);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 51 && this.ctNamePlateEffect.n現在の値 <= 53)
                {
                    tNamePlateStarDraw(player, 0.8f, x + 136, y + 24);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 51 && this.ctNamePlateEffect.n現在の値 <= 55)
                {
                    tNamePlateStarDraw(player, 1.0f, x + 176, y + 8);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 51 && this.ctNamePlateEffect.n現在の値 <= 55)
                {
                    tNamePlateStarDraw(player, 1.0f, x + 176, y + 8);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 61 && this.ctNamePlateEffect.n現在の値 <= 70)
                {
                    tNamePlateStarDraw(player, 1.0f - ((this.ctNamePlateEffect.n現在の値 - 61) / 9f * 1.0f), x + 196, y + 23);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 61 && this.ctNamePlateEffect.n現在の値 <= 67)
                {
                    tNamePlateStarDraw(player, 0.7f - ((this.ctNamePlateEffect.n現在の値 - 61) / 6f * 0.7f), x + 214, y + 14);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 63 && this.ctNamePlateEffect.n現在の値 <= 70)
                {
                    tNamePlateStarDraw(player, 0.5f - ((this.ctNamePlateEffect.n現在の値 - 63) / 7f * 0.5f), x + 129, y + 24);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 63 && this.ctNamePlateEffect.n現在の値 <= 70)
                {
                    tNamePlateStarDraw(player, 0.5f - ((this.ctNamePlateEffect.n現在の値 - 63) / 7f * 0.5f), x + 129, y + 24);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 65 && this.ctNamePlateEffect.n現在の値 <= 70)
                {
                    tNamePlateStarDraw(player, 0.8f - ((this.ctNamePlateEffect.n現在の値 - 65) / 5f * 0.8f), x + 117, y + 7);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 71 && this.ctNamePlateEffect.n現在の値 <= 72)
                {
                    tNamePlateStarDraw(player, 0.8f, x + 151, y + 25);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 71 && this.ctNamePlateEffect.n現在の値 <= 74)
                {
                    tNamePlateStarDraw(player, 0.8f, x + 117, y + 7);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 85 && this.ctNamePlateEffect.n現在の値 <= 112)
                {
                    TJAPlayer3.Tx.NamePlate_Effect[4].Opacity = (int)(1400 - (this.ctNamePlateEffect.n現在の値 - 85) * 50f);

                    TJAPlayer3.Tx.NamePlate_Effect[4].t2D描画(TJAPlayer3.app.Device, x + ((this.ctNamePlateEffect.n現在の値 - 85) * (150f / 27f)), y + 7);
                }
                if (this.ctNamePlateEffect.n現在の値 >= 105 && this.ctNamePlateEffect.n現在の値 <= 120)
                {
                    /*
                    TJAPlayer3.Tx.NamePlate_Effect[TJAPlayer3.NamePlateConfig.data.TitleType[player] + 1].Opacity = this.ctNamePlateEffect.n現在の値 >= 112 ? (int)(255 - (this.ctNamePlateEffect.n現在の値 - 112) * 31.875f) : 255;
                    TJAPlayer3.Tx.NamePlate_Effect[TJAPlayer3.NamePlateConfig.data.TitleType[player] + 1].vc拡大縮小倍率.X = this.ctNamePlateEffect.n現在の値 >= 112 ? 1.0f : (this.ctNamePlateEffect.n現在の値 - 105) / 8f;
                    TJAPlayer3.Tx.NamePlate_Effect[TJAPlayer3.NamePlateConfig.data.TitleType[player] + 1].vc拡大縮小倍率.Y = this.ctNamePlateEffect.n現在の値 >= 112 ? 1.0f : (this.ctNamePlateEffect.n現在の値 - 105) / 8f;
                    TJAPlayer3.Tx.NamePlate_Effect[TJAPlayer3.NamePlateConfig.data.TitleType[player] + 1].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x + 193, y + 6);
                    */

                    int tt = TJAPlayer3.NamePlateConfig.data.TitleType[player];
                    if (tt >= 0 && tt < TJAPlayer3.Skin.Config_NamePlate_Ptn_Title && TJAPlayer3.Tx.NamePlate_Title_Big[tt] != null) {
                        TJAPlayer3.Tx.NamePlate_Title_Big[tt].Opacity = this.ctNamePlateEffect.n現在の値 >= 112 ? (int)(255 - (this.ctNamePlateEffect.n現在の値 - 112) * 31.875f) : 255;
                        TJAPlayer3.Tx.NamePlate_Title_Big[tt].vc拡大縮小倍率.X = this.ctNamePlateEffect.n現在の値 >= 112 ? 1.0f : (this.ctNamePlateEffect.n現在の値 - 105) / 8f;
                        TJAPlayer3.Tx.NamePlate_Title_Big[tt].vc拡大縮小倍率.Y = this.ctNamePlateEffect.n現在の値 >= 112 ? 1.0f : (this.ctNamePlateEffect.n現在の値 - 105) / 8f;
                        TJAPlayer3.Tx.NamePlate_Title_Big[tt].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x + 193, y + 6);
                    }

                }
            }
        }

        private void tNamePlateStarDraw(int player, float Scale, int x, int y)
        {
            /*
            TJAPlayer3.Tx.NamePlate_Effect[TJAPlayer3.NamePlateConfig.data.TitleType[player] - 1].vc拡大縮小倍率.X = Scale;
            TJAPlayer3.Tx.NamePlate_Effect[TJAPlayer3.NamePlateConfig.data.TitleType[player] - 1].vc拡大縮小倍率.Y = Scale;
            TJAPlayer3.Tx.NamePlate_Effect[TJAPlayer3.NamePlateConfig.data.TitleType[player] - 1].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x, y);
            */
            int tt = TJAPlayer3.NamePlateConfig.data.TitleType[player];
            if (tt >= 0 && tt < TJAPlayer3.Skin.Config_NamePlate_Ptn_Title && TJAPlayer3.Tx.NamePlate_Title_Small[tt] != null)
            {
                TJAPlayer3.Tx.NamePlate_Title_Small[tt].vc拡大縮小倍率.X = Scale;
                TJAPlayer3.Tx.NamePlate_Title_Small[tt].vc拡大縮小倍率.Y = Scale;
                TJAPlayer3.Tx.NamePlate_Title_Small[tt].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x, y);
            }

        }

        private CPrivateFastFont[] pfName = new CPrivateFastFont[5];
        private CPrivateFastFont pfTitle;
        private CPrivateFastFont pfdan;
        private CCounter ctNamePlateEffect;

        public CCounter ctAnimatedNamePlateTitle;

        private CTexture[] txName = new CTexture[5];
        private CTexture[] txTitle = new CTexture[5];
        private CTexture[] txdan = new CTexture[5];
    }
}
