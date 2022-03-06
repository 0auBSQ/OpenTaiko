using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using FDK;

namespace TJAPlayer3
{
    internal class CActPlayOption : CActivity
    {
        public CActPlayOption()
        {
            base.b活性化してない = true;
        }

        public override void On活性化()
        {
            if (this.b活性化してる)
                return;

            ctOpen = new CCounter();
            ctClose = new CCounter();

            for (int i = 0; i < OptionType.Length; i++)
                OptionType[i] = new CTexture();

            #region [ Speed ]

            txSpeed[0] = OptionTypeTx("0.5", Color.White, Color.Black);
            txSpeed[1] = OptionTypeTx("1.0", Color.White, Color.Black);
            txSpeed[2] = OptionTypeTx("1.1", Color.White, Color.Black);
            txSpeed[3] = OptionTypeTx("1.2", Color.White, Color.Black);
            txSpeed[4] = OptionTypeTx("1.3", Color.White, Color.Black);
            txSpeed[5] = OptionTypeTx("1.4", Color.White, Color.Black);
            txSpeed[6] = OptionTypeTx("1.5", Color.White, Color.Black);
            txSpeed[7] = OptionTypeTx("1.6", Color.White, Color.Black);
            txSpeed[8] = OptionTypeTx("1.7", Color.White, Color.Black);
            txSpeed[9] = OptionTypeTx("1.8", Color.White, Color.Black);
            txSpeed[10] = OptionTypeTx("1.9", Color.White, Color.Black);
            txSpeed[11] = OptionTypeTx("2.0", Color.White, Color.Black);
            txSpeed[12] = OptionTypeTx("2.5", Color.White, Color.Black);
            txSpeed[13] = OptionTypeTx("3.0", Color.White, Color.Black);
            txSpeed[14] = OptionTypeTx("3.5", Color.White, Color.Black);
            txSpeed[15] = OptionTypeTx("4.0", Color.White, Color.Black);

            #endregion

            txSwitch[0] = OptionTypeTx(CLangManager.LangInstance.GetString(9000), Color.White, Color.Black);
            txSwitch[1] = OptionTypeTx(CLangManager.LangInstance.GetString(9001), Color.White, Color.Black);

            txRandom[0] = OptionTypeTx(CLangManager.LangInstance.GetString(9002), Color.White, Color.Black);
            txRandom[1] = OptionTypeTx(CLangManager.LangInstance.GetString(9003), Color.White, Color.Black);
            txRandom[2] = OptionTypeTx(CLangManager.LangInstance.GetString(9004), Color.White, Color.Black);

            txGameMode[0] = OptionTypeTx(CLangManager.LangInstance.GetString(9002), Color.White, Color.Black);
            txGameMode[1] = OptionTypeTx(CLangManager.LangInstance.GetString(9006), Color.White, Color.Black);

            txNone = OptionTypeTx(CLangManager.LangInstance.GetString(9007), Color.White, Color.Black);

            OptionType[0] = OptionTypeTx(CLangManager.LangInstance.GetString(9008), Color.White, Color.Black);
            OptionType[1] = OptionTypeTx(CLangManager.LangInstance.GetString(9009), Color.White, Color.Black);
            OptionType[2] = OptionTypeTx(CLangManager.LangInstance.GetString(9010), Color.White, Color.Black);
            OptionType[3] = OptionTypeTx(CLangManager.LangInstance.GetString(9011), Color.White, Color.Black);
            OptionType[4] = OptionTypeTx(CLangManager.LangInstance.GetString(9012), Color.White, Color.Black);
            OptionType[5] = OptionTypeTx(CLangManager.LangInstance.GetString(9013), Color.White, Color.Black);
            OptionType[6] = OptionTypeTx(CLangManager.LangInstance.GetString(9014), Color.White, Color.Black);
            OptionType[7] = OptionTypeTx(CLangManager.LangInstance.GetString(9015), Color.White, Color.Black);

            for (int i = 0; i < OptionType.Length; i++)
                OptionType[i].vc拡大縮小倍率.X = 0.96f;

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
            if (this.b活性化してない)
                return;
            base.OnManagedリソースの解放();
        }

        public int On進行描画(int player)
        {
            if (this.b活性化してない)
                return 0;

            ctOpen.t進行();
            ctClose.t進行();

            if (!ctOpen.b進行中) ctOpen.t開始(0, 50, 6, TJAPlayer3.Timer);

            var act難易度 = TJAPlayer3.stage選曲.act難易度選択画面;

            #region [ Open & Close ]

            float oy1 = ctOpen.n現在の値 * 18;
            float oy2 = (ctOpen.n現在の値 - 30) * 4;
            float oy3 = ctOpen.n現在の値 < 30 ? 410 - oy1 : -80 + oy2;

            float cy1 = ctClose.n現在の値 * 3;
            float cy2 = (ctClose.n現在の値 - 20) * 16;
            float cy3 = ctClose.n現在の値 < 20 ? 0 - cy1 : 20 + cy2;

            float y = oy3 + cy3;

            #endregion

            TJAPlayer3.Tx.Difficulty_Option.t2D描画(TJAPlayer3.app.Device, 0, y);

            TJAPlayer3.Tx.Difficulty_Option_Select.t2D描画(TJAPlayer3.app.Device, 0, y + NowCount * 40.7f);


            for (int i = 0; i < OptionType.Length; i++)
            {
                OptionType[i].t2D描画(TJAPlayer3.app.Device, 16, 379 + i * 40.8f + y);
            }

            txSpeed[nSpeedCount].t2D拡大率考慮描画(TJAPlayer3.app.Device, CTexture.RefPnt.Up, 200, 375 + y);
            txSwitch[nStealth].t2D拡大率考慮描画(TJAPlayer3.app.Device, CTexture.RefPnt.Up, 200, 375 + y + 1 * 40.7f);
            txSwitch[nAbekobe].t2D拡大率考慮描画(TJAPlayer3.app.Device, CTexture.RefPnt.Up, 200, 375 + y + 2 * 40.7f);
            txRandom[nRandom].t2D拡大率考慮描画(TJAPlayer3.app.Device, CTexture.RefPnt.Up, 200, 375 + y + 3 * 40.7f);
            txGameMode[nGameMode].t2D拡大率考慮描画(TJAPlayer3.app.Device, CTexture.RefPnt.Up, 200, 375 + y + 4 * 40.7f);
            txSwitch[nAutoMode].t2D拡大率考慮描画(TJAPlayer3.app.Device, CTexture.RefPnt.Up, 200, 375 + y + 5 * 40.7f);

            for (int i = 6; i < 8; i++)
            {
                txNone.t2D拡大率考慮描画(TJAPlayer3.app.Device, CTexture.RefPnt.Up, 200, 375 + y + i * 40.7f);
            }

            if (ctClose.n現在の値 >= 50)
            {
                Decision();
                NowCount = 0;
                ctOpen.t停止();
                ctOpen.n現在の値 = 0;
                ctClose.t停止();
                ctClose.n現在の値 = 0;
                bEnd = false;
                act難易度.bOption[player] = false;
            }

            #region [ Key ]

            if (!ctClose.b進行中)
            {
                if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LBlue)) { OptionSelect(true); TJAPlayer3.Skin.sound変更音.t再生する(); };
                if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RBlue)) { OptionSelect(false); TJAPlayer3.Skin.sound変更音.t再生する(); };

                if ((TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LRed) || TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RRed)) && ctOpen.n現在の値 >= ctOpen.n終了値)
                {
                    TJAPlayer3.Skin.sound決定音.t再生する();
                    if (NowCount < 7)
                    {
                        NowCount++;
                    }
                    else if (NowCount >= 7 && !bEnd)
                    {
                        bEnd = true;
                        ctClose.t開始(0, 50, 6, TJAPlayer3.Timer);
                    }
                }
            }

           
            #endregion
            return 0;
        }


        public CCounter ctOpen;
        public CCounter ctClose;
        public CTexture[] OptionType = new CTexture[8];

        public int NowCount;
        public int[] NowCountType = new int[8];

        public bool bEnd;

        public CTexture[] txSpeed = new CTexture[16];
        public int nSpeedCount = 1;

        public int nStealth = 0;
        public int nAbekobe = 0;

        public CTexture[] txRandom = new CTexture[3];
        public int nRandom = 0;

        public CTexture[] txGameMode = new CTexture[2];
        public int nGameMode;

        public CTexture[] txAutoMode = new CTexture[2];
        public int nAutoMode = 0;
        public CTexture txNone = new CTexture();

        public CTexture[] txSwitch = new CTexture[2];

        public CTexture OptionTypeTx(string str文字, Color forecolor, Color backcolor)
        {
            using (var bmp = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 13).DrawPrivateFont(str文字, forecolor, backcolor))
            {
                return TJAPlayer3.tテクスチャの生成(bmp);
            }
        }

        public void OptionSelect(bool left)
        {
            switch (NowCount)
            {
                case 0:
                    if (left)
                    {
                        if (nSpeedCount > 0) nSpeedCount--;
                        else nSpeedCount = 15;
                    }
                    else
                    {
                        if (nSpeedCount < 15) nSpeedCount++;
                        else nSpeedCount = 0;
                    }
                    break;
                case 1:
                    if (nStealth == 0) nStealth = 1;
                    else nStealth = 0;
                    break;
                case 2:
                    if (nAbekobe == 0) nAbekobe = 1;
                    else nAbekobe = 0;
                    break;
                case 3:
                    if (left)
                    {
                        if (nRandom > 0) nRandom--;
                        else nRandom = 2;
                    }
                    else
                    {
                        if (nRandom < 2) nRandom++;
                        else nRandom = 0;
                    }
                    break;
                case 4:
                    if (nGameMode == 0) nGameMode = 1;
                    else nGameMode = 0;
                    break;
                case 5:
                    if (nAutoMode == 0) nAutoMode = 1;
                    else nAutoMode = 0;
                    break;

            }
        }
        public void Decision()
        {
            #region [ Speed ]

            if (nSpeedCount == 0)
            {
                TJAPlayer3.ConfigIni.n譜面スクロール速度[0] = 4;
            }
            else if (nSpeedCount > 0 && nSpeedCount <= 11)
            {
                TJAPlayer3.ConfigIni.n譜面スクロール速度[0] = nSpeedCount + 8;
            }
            else if (nSpeedCount == 12)
            {
                TJAPlayer3.ConfigIni.n譜面スクロール速度[0] = 20 + 4;
            }
            else if (nSpeedCount == 13)
            {
                TJAPlayer3.ConfigIni.n譜面スクロール速度[0] = 20 + 9;
            }
            else if (nSpeedCount == 14)
            {
                TJAPlayer3.ConfigIni.n譜面スクロール速度[0] = 20 + 14;
            }
            else if (nSpeedCount == 15)
            {
                TJAPlayer3.ConfigIni.n譜面スクロール速度[0] = 20 + 19;
            }

            #endregion
            #region [ Doron ]
            if (nStealth == 0)
            {
                TJAPlayer3.ConfigIni.eSTEALTH = Eステルスモード.OFF;
            }
            else
            {
                TJAPlayer3.ConfigIni.eSTEALTH = Eステルスモード.DORON;
            }
            #endregion
            #region [ Random ]
            if(nRandom == 2 && nAbekobe == 1)
            {
                TJAPlayer3.ConfigIni.eRandom.Taiko = Eランダムモード.HYPERRANDOM;
            }
            else if (nRandom == 2 && nAbekobe == 0)
            {
                TJAPlayer3.ConfigIni.eRandom.Taiko = Eランダムモード.SUPERRANDOM;
            }
            else if (nRandom == 1 && nAbekobe == 1)
            {
                TJAPlayer3.ConfigIni.eRandom.Taiko = Eランダムモード.RANDOM;
            }
            else if (nRandom == 1 && nAbekobe == 0)
            {
                TJAPlayer3.ConfigIni.eRandom.Taiko = Eランダムモード.RANDOM;
            }
            else if (nRandom == 0 && nAbekobe == 1)
            {
                TJAPlayer3.ConfigIni.eRandom.Taiko = Eランダムモード.MIRROR;
            }
            else if (nRandom == 0 && nAbekobe == 0)
            {
                TJAPlayer3.ConfigIni.eRandom.Taiko = Eランダムモード.OFF;
            }
            #endregion
            #region [ GameMode ]
            if(nGameMode == 0)
            {
                TJAPlayer3.ConfigIni.bTokkunMode = false;
            }
            else
            {
                TJAPlayer3.ConfigIni.bTokkunMode = true;
            }
            #endregion
            #region [ AutoMode ]
            if(nAutoMode == 1)
            {
                TJAPlayer3.ConfigIni.b太鼓パートAutoPlay = true;
            }
            else
            {
                TJAPlayer3.ConfigIni.b太鼓パートAutoPlay = false;
            }
            #endregion
        }

    }
}
