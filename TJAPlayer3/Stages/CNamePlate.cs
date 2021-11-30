using FDK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    class CNamePlate
    {
        public CNamePlate()
        {
            for (int player = 0; player < 2; player++)
            {
                if (TJAPlayer3.NamePlateConfig.data.DanType[player] < 0) TJAPlayer3.NamePlateConfig.data.DanType[player] = 0;
                else if (TJAPlayer3.NamePlateConfig.data.DanType[player] > 2) TJAPlayer3.NamePlateConfig.data.DanType[player] = 2;

                if (TJAPlayer3.NamePlateConfig.data.TitleType[player] < 0) TJAPlayer3.NamePlateConfig.data.TitleType[player] = 0;

                if (!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName))
                {
                    if (TJAPlayer3.NamePlateConfig.data.Title[player] == "" || TJAPlayer3.NamePlateConfig.data.Title[player] == null)
                        this.pfName = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 15);
                    else
                        this.pfName = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 12);

                    this.pfTitle = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 11);
                    this.pfdan = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 12);
                }
                else
                {
                    if (TJAPlayer3.NamePlateConfig.data.Title[player] == "" || TJAPlayer3.NamePlateConfig.data.Title[player] == null)
                        this.pfName = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 15);
                    else
                        this.pfName = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 12);

                    this.pfTitle = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 11);
                    this.pfdan = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 12);
                }

                tNamePlateRefreshTitles(player);

            }

            ctNamePlateEffect = new CCounter(0, 120, 16.6f, TJAPlayer3.Timer);
            ctAnimatedNamePlateTitle = new CCounter(0, 10000, 60.0f, TJAPlayer3.Timer);
        }

        public void tNamePlateRefreshTitles(int player)
        {
            string[] stages = { "初", "二", "三", "四", "五", "六", "七", "八", "九", "極" };

            string name = "AIドン";
            string title = "デウス・エクス・マキナ";
            string dan = stages[Math.Max(0, TJAPlayer3.ConfigIni.nAILevel - 1)] + "面";

            if (TJAPlayer3.ConfigIni.nAILevel == 0 || player == 0)
            {
                name = TJAPlayer3.NamePlateConfig.data.Name[player];
                title = TJAPlayer3.NamePlateConfig.data.Title[player];
                dan = TJAPlayer3.NamePlateConfig.data.Dan[player];
            }

            using (var tex = pfName.DrawPrivateFont(name, Color.White, Color.Black, 25))
                txName[player] = TJAPlayer3.tテクスチャの生成(tex);

            using (var tex = pfTitle.DrawPrivateFont(title, Color.Black, Color.Empty))
                txTitle[player] = TJAPlayer3.tテクスチャの生成(tex);

            using (var tex = pfdan.DrawPrivateFont(dan, Color.White, Color.Black, 22))
                txdan[player] = TJAPlayer3.tテクスチャの生成(tex);
        }


        public void tNamePlateDraw(int x, int y, int player, bool bTitle = false, int Opacity = 255)
        {
            ctNamePlateEffect.t進行Loop();
            ctAnimatedNamePlateTitle.t進行Loop();

            this.txName[player].Opacity = Opacity;
            this.txTitle[player].Opacity = Opacity;
            this.txdan[player].Opacity = Opacity;

            TJAPlayer3.Tx.NamePlateBase.Opacity = Opacity;

            for (int i = 0; i < 5; i++)
                TJAPlayer3.Tx.NamePlate_Effect[i].Opacity = Opacity;

            if (bTitle)
            {
                //220, 54
                TJAPlayer3.Tx.NamePlateBase.t2D描画(TJAPlayer3.app.Device, x, y, new RectangleF(0, 3 * 54, 220, 54));

                if (TJAPlayer3.NamePlateConfig.data.Dan[player] != "" && TJAPlayer3.NamePlateConfig.data.Dan[player] != null)
                {
                    if (TJAPlayer3.NamePlateConfig.data.Title[player] != "" && TJAPlayer3.NamePlateConfig.data.Title[player] != null)
                    {
                        int tt = TJAPlayer3.NamePlateConfig.data.TitleType[player];
                        if (tt >= 0 && tt < TJAPlayer3.Skin.Config_NamePlate_Ptn_Title)
                        {
                            TJAPlayer3.Tx.NamePlate_Title[tt][ctAnimatedNamePlateTitle.n現在の値 % TJAPlayer3.Skin.Config_NamePlate_Ptn_Title_Boxes[tt]].Opacity = Opacity;
                            TJAPlayer3.Tx.NamePlate_Title[tt][ctAnimatedNamePlateTitle.n現在の値 % TJAPlayer3.Skin.Config_NamePlate_Ptn_Title_Boxes[tt]].t2D描画(
                                TJAPlayer3.app.Device, x - 2, y - 2);
                        }

                        //TJAPlayer3.Tx.NamePlateBase.t2D描画(TJAPlayer3.app.Device, x, y, new RectangleF(0, (4 + TJAPlayer3.NamePlateConfig.data.TitleType[player]) * 54, 220, 54));
                    }
                        
                }

                if (TJAPlayer3.NamePlateConfig.data.Dan[player] != "" && TJAPlayer3.NamePlateConfig.data.Dan[player] != null)
                {
                    TJAPlayer3.Tx.NamePlateBase.t2D描画(TJAPlayer3.app.Device, x, y, new RectangleF(0, 7 * 54, 220, 54));
                    TJAPlayer3.Tx.NamePlateBase.t2D描画(TJAPlayer3.app.Device, x, y, new RectangleF(0, (8 + TJAPlayer3.NamePlateConfig.data.DanType[player]) * 54, 220, 54));
                }

                tNamePlateDraw(player, x, y, Opacity);

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

                if (txdan[player].szテクスチャサイズ.Width >= 66.0f)
                    txdan[player].vc拡大縮小倍率.X = 66.0f / txdan[player].szテクスチャサイズ.Width;

                if (TJAPlayer3.NamePlateConfig.data.Dan[player] != "" && TJAPlayer3.NamePlateConfig.data.Dan[player] != null)
                {
                    this.txdan[player].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x + 69, y + 45);
                    if (TJAPlayer3.NamePlateConfig.data.DanGold[player])
                    {
                        TJAPlayer3.Tx.NamePlateBase.b乗算合成 = true;
                        TJAPlayer3.Tx.NamePlateBase.t2D描画(TJAPlayer3.app.Device, x, y, new RectangleF(0, 11 * 54, 220, 54));
                        TJAPlayer3.Tx.NamePlateBase.b乗算合成 = false;
                    }
                }

                if (TJAPlayer3.NamePlateConfig.data.Title[player] != "" && TJAPlayer3.NamePlateConfig.data.Title[player] != null)
                {
                    if (txTitle[player].szテクスチャサイズ.Width >= 160)
                    {
                        txTitle[player].vc拡大縮小倍率.X = 160.0f / txTitle[player].szテクスチャサイズ.Width;
                        txTitle[player].vc拡大縮小倍率.Y = 160.0f / txTitle[player].szテクスチャサイズ.Width;
                    }

                    txTitle[player].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x + 115, y + 21);
                    if (TJAPlayer3.NamePlateConfig.data.Dan[player] == "" || TJAPlayer3.NamePlateConfig.data.Dan[player] == null)
                        this.txName[player].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x + 100, y + 45);
                    else
                        this.txName[player].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x + 149, y + 45);
                }
                else
                    this.txName[player].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x + 121, y + 36);
            }
            else
            {
                //220, 54
                TJAPlayer3.Tx.NamePlateBase.t2D描画(TJAPlayer3.app.Device, x, y, new RectangleF(0, 3 * 54, 220, 54));

                if (TJAPlayer3.NamePlateConfig.data.Dan[player] != "" && TJAPlayer3.NamePlateConfig.data.Dan[player] != null)
                {
                    if (TJAPlayer3.NamePlateConfig.data.Title[player] != "" && TJAPlayer3.NamePlateConfig.data.Title[player] != null)
                    {
                        int tt = TJAPlayer3.NamePlateConfig.data.TitleType[player];
                        if (tt >= 0 && tt < TJAPlayer3.Skin.Config_NamePlate_Ptn_Title)
                        {
                            TJAPlayer3.Tx.NamePlate_Title[tt][ctAnimatedNamePlateTitle.n現在の値 % TJAPlayer3.Skin.Config_NamePlate_Ptn_Title_Boxes[tt]].Opacity = Opacity;
                            TJAPlayer3.Tx.NamePlate_Title[tt][ctAnimatedNamePlateTitle.n現在の値 % TJAPlayer3.Skin.Config_NamePlate_Ptn_Title_Boxes[tt]].t2D描画(
                                TJAPlayer3.app.Device, x - 2, y - 2);
                        }

                        //TJAPlayer3.Tx.NamePlateBase.t2D描画(TJAPlayer3.app.Device, x, y, new RectangleF(0, (4 + TJAPlayer3.NamePlateConfig.data.TitleType[player]) * 54, 220, 54));
                    }
                        
                }

                if (TJAPlayer3.NamePlateConfig.data.Dan[player] != "" && TJAPlayer3.NamePlateConfig.data.Dan[player] != null)
                {
                    TJAPlayer3.Tx.NamePlateBase.t2D描画(TJAPlayer3.app.Device, x, y, new RectangleF(0, 7 * 54, 220, 54));
                    TJAPlayer3.Tx.NamePlateBase.t2D描画(TJAPlayer3.app.Device, x, y, new RectangleF(0, (8 + TJAPlayer3.NamePlateConfig.data.DanType[player]) * 54, 220, 54));
                }

                tNamePlateDraw(player, x, y, Opacity);

                TJAPlayer3.Tx.NamePlateBase.t2D描画(TJAPlayer3.app.Device, x, y, new RectangleF(0, player == 1 ? 2 * 54 : 0, 220, 54));

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

                if (txdan[player].szテクスチャサイズ.Width >= 66.0f)
                    txdan[player].vc拡大縮小倍率.X = 66.0f / txdan[player].szテクスチャサイズ.Width;

                if (TJAPlayer3.NamePlateConfig.data.Dan[player] != "" && TJAPlayer3.NamePlateConfig.data.Dan[player] != null)
                {
                    this.txdan[player].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x + 69, y + 44);

                    if (TJAPlayer3.NamePlateConfig.data.DanGold[player])
                    {
                        TJAPlayer3.Tx.NamePlateBase.b乗算合成 = true;
                        TJAPlayer3.Tx.NamePlateBase.t2D描画(TJAPlayer3.app.Device, x, y, new RectangleF(0, 11 * 54, 220, 54));
                        TJAPlayer3.Tx.NamePlateBase.b乗算合成 = false;
                    }
                }

                if (TJAPlayer3.NamePlateConfig.data.Title[player] != "" && TJAPlayer3.NamePlateConfig.data.Title[player] != null)
                {
                    if (txTitle[player].szテクスチャサイズ.Width >= 160)
                    {
                        txTitle[player].vc拡大縮小倍率.X = 160.0f / txTitle[player].szテクスチャサイズ.Width;
                        txTitle[player].vc拡大縮小倍率.Y = 160.0f / txTitle[player].szテクスチャサイズ.Width;
                    }

                    txTitle[player].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x + 124, y + 22);

                    
                    if (TJAPlayer3.NamePlateConfig.data.Dan[player] == "" || TJAPlayer3.NamePlateConfig.data.Dan[player] == null)
                        this.txName[player].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x + 121, y + 44);
                    else
                        this.txName[player].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x + 144, y + 44);
                }
                else
                {
                    this.txName[player].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x + 121, y + 36);
                }

            }

            TJAPlayer3.Tx.NamePlateBase.t2D描画(TJAPlayer3.app.Device, x, y, new RectangleF(0, 4 * 54 + 3, 220, 54));
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

        private CPrivateFastFont pfName;
        private CPrivateFastFont pfTitle;
        private CPrivateFastFont pfdan;
        private CCounter ctNamePlateEffect;

        public CCounter ctAnimatedNamePlateTitle;

        private CTexture[] txName = new CTexture[2];
        private CTexture[] txTitle = new CTexture[2];
        private CTexture[] txdan = new CTexture[2];
    }
}
