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
            OptionType[4] = OptionTypeTx(CLangManager.LangInstance.GetString(500), Color.White, Color.Black);
            OptionType[5] = OptionTypeTx(CLangManager.LangInstance.GetString(9012), Color.White, Color.Black);
            OptionType[6] = OptionTypeTx(CLangManager.LangInstance.GetString(9013), Color.White, Color.Black);
            OptionType[7] = OptionTypeTx(CLangManager.LangInstance.GetString(9014), Color.White, Color.Black);
            OptionType[8] = OptionTypeTx(CLangManager.LangInstance.GetString(9015), Color.White, Color.Black);

            for (int i = 0; i < 5; i++)
            {
                txTiming[i] = OptionTypeTx(CLangManager.LangInstance.GetString(501 + i), Color.White, Color.Black);
            }

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

            if (ctOpen.n現在の値 == 0)
                Init(player);

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

            // Temporary textures, to reimplement to fit the new menu
            TJAPlayer3.Tx.Difficulty_Option.t2D描画(TJAPlayer3.app.Device, 0, y);
            

            float baseX = (player == 0) ? 200 : 1180;
            float baseY = 659.9f + y - nOptionCount * 40.7f;

            var _textures = new CTexture[]
            {
                txSpeed[nSpeedCount],
                txSwitch[nStealth],
                txSwitch[nAbekobe],
                txRandom[nRandom],
                txTiming[nTiming],
                txGameMode[nGameMode],
                txSwitch[nAutoMode],
                txNone,
                txNone,
            };

            TJAPlayer3.Tx.Difficulty_Option_Select.t2D描画(TJAPlayer3.app.Device, baseX - 200, baseY - 375 + NowCount * 40.7f);

            for (int i = 0; i < OptionType.Length; i++)
            {
                OptionType[i].t2D描画(TJAPlayer3.app.Device, baseX - 184, baseY + 4 + i * 40.8f);
            }

            for (int i = 0; i < _textures.Length; i++)
            {
                _textures[i]?.t2D拡大率考慮描画(TJAPlayer3.app.Device, CTexture.RefPnt.Up, baseX, baseY + i * 40.7f);
            }

            if (ctClose.n現在の値 >= 50)
            {
                Decision(player);
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
                bool _leftDrum = (player == 0)
                    ? TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LBlue)
                    : TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LBlue2P);

                bool _rightDrum = (player == 0)
                    ? TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RBlue)
                    : TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RBlue2P);

                bool _centerDrum = (player == 0)
                    ? (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LRed) || TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RRed))
                    : (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LRed2P) || TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RRed2P));


                if (_leftDrum) { OptionSelect(true); TJAPlayer3.Skin.sound変更音.t再生する(); };
                if (_rightDrum) { OptionSelect(false); TJAPlayer3.Skin.sound変更音.t再生する(); };

                if (_centerDrum && ctOpen.n現在の値 >= ctOpen.n終了値)
                {
                    TJAPlayer3.Skin.sound決定音.t再生する();
                    if (NowCount < nOptionCount)
                    {
                        NowCount++;
                    }
                    else if (NowCount >= nOptionCount && !bEnd)
                    {
                        bEnd = true;
                        ctClose.t開始(0, 50, 6, TJAPlayer3.Timer);
                    }
                }
            }

           
            #endregion

            return 0;
        }

        public int nOptionCount = 8;

        public CCounter ctOpen;
        public CCounter ctClose;
        public CTexture[] OptionType = new CTexture[9];

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

        public CTexture[] txTiming = new CTexture[5];
        public int nTiming = 2;

        public CTexture OptionTypeTx(string str文字, Color forecolor, Color backcolor)
        {
            using (var bmp = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 13).DrawPrivateFont(str文字, forecolor, backcolor))
            {
                return TJAPlayer3.tテクスチャの生成(bmp);
            }
        }

        private void ShiftVal(bool left, ref int value, int capUp, int capDown)
        {
            if (left)
            {
                if (value > capDown) value--;
                else value = capUp;
            }
            else
            {
                if (value < capUp) value++;
                else value = capDown;
            }
        }

        public void OptionSelect(bool left)
        {
            switch (NowCount)
            {
                case 0:
                    ShiftVal(left, ref nSpeedCount, 15, 0);
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
                    ShiftVal(left, ref nRandom, 2, 0);
                    break;
                case 4:
                    ShiftVal(left, ref nTiming, 4, 0);
                    break;
                case 5:
                    if (nGameMode == 0) nGameMode = 1;
                    else nGameMode = 0;
                    break;
                case 6:
                    if (nAutoMode == 0) nAutoMode = 1;
                    else nAutoMode = 0;
                    break;

            }
        }

        public void Init(int player)
        {
            int actual = TJAPlayer3.GetActualPlayer(player);

            #region [ Speed ]

            int speed = TJAPlayer3.ConfigIni.nScrollSpeed[actual];

            if (speed <= 4)
                nSpeedCount = 0;
            else if (speed <= 19)
                nSpeedCount = speed - 8;
            else if (speed <= 24)
                nSpeedCount = 12;
            else if (speed <= 29)
                nSpeedCount = 13;
            else if (speed <= 34)
                nSpeedCount = 14;
            else
                nSpeedCount = 15;

            #endregion

            #region [ Doron ]

            if (TJAPlayer3.ConfigIni.eSTEALTH == Eステルスモード.OFF)
                nStealth = 0;
            else if (TJAPlayer3.ConfigIni.eSTEALTH == Eステルスモード.DORON)
                nStealth = 1;

            #endregion

            #region [ Random ]

            var rand_ = TJAPlayer3.ConfigIni.eRandom.Taiko;

            if (rand_ == Eランダムモード.HYPERRANDOM)
            {
                nRandom = 2;
                nAbekobe = 1;
            }
            else if (rand_ == Eランダムモード.SUPERRANDOM)
            {
                nRandom = 2;
                nAbekobe = 0;
            }
            else if (rand_ == Eランダムモード.RANDOM)
            {
                nRandom = 1;
                nAbekobe = 0;
            }
            else if (rand_ == Eランダムモード.MIRROR)
            {
                nRandom = 0;
                nAbekobe = 1;
            }
            else if (rand_ == Eランダムモード.OFF)
            {
                nRandom = 0;
                nAbekobe = 0;
            }

            #endregion

            #region [ Timing ]

            nTiming = TJAPlayer3.ConfigIni.nTimingZones[actual];

            #endregion

            #region [ GameMode ]

            if (TJAPlayer3.ConfigIni.bTokkunMode == true)
                nGameMode = 1;
            else
                nGameMode = 0;

            #endregion

            #region [ AutoMode ]

            bool _auto = (player == 0)
                ? TJAPlayer3.ConfigIni.b太鼓パートAutoPlay
                : TJAPlayer3.ConfigIni.b太鼓パートAutoPlay2P;

            if (_auto == true)
                nAutoMode = 1;
            else
                nAutoMode = 0;

            #endregion

        }

        public void Decision(int player)
        {
            int actual = TJAPlayer3.GetActualPlayer(player);

            #region [ Speed ]

            if (nSpeedCount == 0)
            {
                TJAPlayer3.ConfigIni.nScrollSpeed[actual] = 4;
            }
            else if (nSpeedCount > 0 && nSpeedCount <= 11)
            {
                TJAPlayer3.ConfigIni.nScrollSpeed[actual] = nSpeedCount + 8;
            }
            else if (nSpeedCount == 12)
            {
                TJAPlayer3.ConfigIni.nScrollSpeed[actual] = 24;
            }
            else if (nSpeedCount == 13)
            {
                TJAPlayer3.ConfigIni.nScrollSpeed[actual] = 29;
            }
            else if (nSpeedCount == 14)
            {
                TJAPlayer3.ConfigIni.nScrollSpeed[actual] = 34;
            }
            else if (nSpeedCount == 15)
            {
                TJAPlayer3.ConfigIni.nScrollSpeed[actual] = 39;
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

            if (nRandom == 2 && nAbekobe == 1)
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

            #region [ Timing ]

            TJAPlayer3.ConfigIni.nTimingZones[actual] = nTiming;

            #endregion

            #region [ GameMode ]

            if (nGameMode == 0)
            {
                TJAPlayer3.ConfigIni.bTokkunMode = false;
            }
            else
            {
                TJAPlayer3.ConfigIni.bTokkunMode = true;
            }

            #endregion

            #region [ AutoMode ]

            if (nAutoMode == 1)
            {
                if (player == 0)
                    TJAPlayer3.ConfigIni.b太鼓パートAutoPlay = true;
                else
                    TJAPlayer3.ConfigIni.b太鼓パートAutoPlay2P = true;
            }
            else
            {
                if (player == 0)
                    TJAPlayer3.ConfigIni.b太鼓パートAutoPlay = false;
                else
                    TJAPlayer3.ConfigIni.b太鼓パートAutoPlay2P = false;
            }

            #endregion
        }

    }
}
