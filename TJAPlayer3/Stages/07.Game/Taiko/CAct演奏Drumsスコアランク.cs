using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;

namespace TJAPlayer3
{
    class CAct演奏Drumsスコアランク : CActivity
    {
        public override void On活性化()
        {
            double RollTimems = 0;
            foreach (var chip in TJAPlayer3.DTX.listChip)
            {
                //if (chip.nチャンネル番号 == 21 || chip.nチャンネル番号 == 22)
                if (NotesManager.IsRoll(chip))
                {
                    RollTimems += (chip.nノーツ終了時刻ms - chip.n発声時刻ms) / 1000.0;
                }
            }

            this.ScoreRank = new int[] { 500000, 600000, 700000, 800000, 900000, 950000, 
                Math.Max(1000000, (int)(TJAPlayer3.stage演奏ドラム画面.nAddScoreNiji[0] * TJAPlayer3.stage演奏ドラム画面.nNoteCount[0]) + (int)(TJAPlayer3.stage演奏ドラム画面.nBalloonCount[0] * 100) + (int)(Math.Ceiling(RollTimems * 16.6 / 10) * 100 * 10)) };

            this.ScoreRank2P = new int[] { 500000, 600000, 700000, 800000, 900000, 950000, 
                Math.Max(1000000, (int)(TJAPlayer3.stage演奏ドラム画面.nAddScoreNiji[1] * TJAPlayer3.stage演奏ドラム画面.nNoteCount[1]) + (int)(TJAPlayer3.stage演奏ドラム画面.nBalloonCount[1] * 100) + (int)(Math.Ceiling(RollTimems * 16.6 / 10) * 100 * 10)) };

            for (int i = 0; i < 7; i++)
            {
                this.counter[i] = new CCounter();
                this.counterJ2[i] = new CCounter();
            }
            base.On活性化();
        }

        public override void On非活性化()
        {
            base.On非活性化();
        }

        private void displayScoreRank(int i, int player, float x, int mode = 0)
        {
            CCounter cct = this.counter[i];
            if (player == 1)
                cct = this.counterJ2[i];

            CTexture tex = TJAPlayer3.Tx.ScoreRank;
            if (mode == 1) // tower
                tex = TJAPlayer3.Tx.TowerResult_ScoreRankEffect;

            if (tex == null)
                return;

            if (!cct.b進行中)
            {
                cct.t開始(0, 3000, 1, TJAPlayer3.Timer);
            }
            if (cct.n現在の値 <= 255)
            {
                tex.Opacity = cct.n現在の値;
                x = 51 - (cct.n現在の値 / 5.0f);
            }
            if (cct.n現在の値 > 255 && cct.n現在の値 <= 255 + 180)
            {
                tex.Opacity = 255;

                float newSize = 1.0f + (float)Math.Sin((cct.n現在の値 - 255) * (Math.PI / 180)) * 0.2f;
                tex.vc拡大縮小倍率.X = newSize;
                tex.vc拡大縮小倍率.Y = newSize;
                x = 0;
            }
            if (cct.n現在の値 > 255 + 180 && cct.n現在の値 <= 2745)
            {
                tex.Opacity = 255;
                tex.vc拡大縮小倍率.X = 1.0f;
                tex.vc拡大縮小倍率.Y = 1.0f;
                x = 0;
            }
            if (cct.n現在の値 >= 2745 && cct.n現在の値 <= 3000)
            {
                tex.Opacity = 255 - ((cct.n現在の値 - 2745));
                x = -((cct.n現在の値 - 2745) / 5.0f);
            }

            var ypos = (player == 0) ? 98 + (int)x : 720 - (98 + (int)x);

            if (mode == 0)
                tex.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 87, ypos, new System.Drawing.Rectangle(0, i == 0 ? i * 114 : i * 120, 140, i == 0 ? 114 : 120));
            else if (mode == 1 && player == 0)
                tex.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 87, ypos, new System.Drawing.Rectangle(i * 229, 0, 229, 194));
        }

        public override int On進行描画()
        {
            if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan)
            {
                float x = 0;

                for (int i = 0; i < 7; i++)
                {
                    if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Tower)
                    {
                        #region [Ensou score ranks]

                        counter[i].t進行();
                        if (TJAPlayer3.stage演奏ドラム画面.actScore.GetScore(0) >= ScoreRank[i])
                        {
                            displayScoreRank(i, 0, x);

                            #region [Legacy]

                                /*
                                if (!this.counter[i].b進行中)
                                {
                                    this.counter[i].t開始(0, 3000, 1, TJAPlayer3.Timer);
                                }
                                if (counter[i].n現在の値 <= 255)
                                {
                                    TJAPlayer3.Tx.ScoreRank.Opacity = counter[i].n現在の値;
                                    x = 51 - (counter[i].n現在の値 / 5.0f);
                                }
                                if (counter[i].n現在の値 > 255 && counter[i].n現在の値 <= 255 + 180)
                                {
                                    TJAPlayer3.Tx.ScoreRank.Opacity = 255;
                                    TJAPlayer3.Tx.ScoreRank.vc拡大縮小倍率.X = 1.0f + (float)Math.Sin((counter[i].n現在の値 - 255) * (Math.PI / 180)) * 0.2f;
                                    TJAPlayer3.Tx.ScoreRank.vc拡大縮小倍率.Y = 1.0f + (float)Math.Sin((counter[i].n現在の値 - 255) * (Math.PI / 180)) * 0.2f;
                                    x = 0;
                                }
                                if (counter[i].n現在の値 > 255 + 180 && counter[i].n現在の値 <= 2745)
                                {
                                    TJAPlayer3.Tx.ScoreRank.Opacity = 255;
                                    TJAPlayer3.Tx.ScoreRank.vc拡大縮小倍率.X = 1.0f;
                                    TJAPlayer3.Tx.ScoreRank.vc拡大縮小倍率.Y = 1.0f;
                                    x = 0;
                                }
                                if (counter[i].n現在の値 >= 2745 && counter[i].n現在の値 <= 3000)
                                {
                                    TJAPlayer3.Tx.ScoreRank.Opacity = 255 - ((counter[i].n現在の値 - 2745));
                                    x = -((counter[i].n現在の値 - 2745) / 5.0f);
                                }

                                TJAPlayer3.Tx.ScoreRank.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 87, 98 + (int)x, new System.Drawing.Rectangle(0, i == 0 ? i * 114 : i * 120, 140, i == 0 ? 114 : 120));
                                */

                                #endregion
                        }

                        x = 0;
                        counterJ2[i].t進行();
                        if (TJAPlayer3.stage演奏ドラム画面.actScore.GetScore(1) >= ScoreRank2P[i])
                        {
                            displayScoreRank(i, 1, x);

                            #region [Legacy]

                                /*
                                if (!this.counterJ2[i].b進行中)
                                {
                                    this.counterJ2[i].t開始(0, 3000, 1, TJAPlayer3.Timer);
                                }
                                if (counterJ2[i].n現在の値 <= 255)
                                {
                                    TJAPlayer3.Tx.ScoreRank.Opacity = counterJ2[i].n現在の値;
                                    x = 51 - (counterJ2[i].n現在の値 / 5.0f);
                                }
                                if (counterJ2[i].n現在の値 > 255 && counterJ2[i].n現在の値 <= 255 + 180)
                                {
                                    TJAPlayer3.Tx.ScoreRank.Opacity = 255;
                                    TJAPlayer3.Tx.ScoreRank.vc拡大縮小倍率.X = 1.0f + (float)Math.Sin((counterJ2[i].n現在の値 - 255) * (Math.PI / 180)) * 0.2f;
                                    TJAPlayer3.Tx.ScoreRank.vc拡大縮小倍率.Y = 1.0f + (float)Math.Sin((counterJ2[i].n現在の値 - 255) * (Math.PI / 180)) * 0.2f;
                                    x = 0;
                                }
                                if (counterJ2[i].n現在の値 > 255 + 180 && counterJ2[i].n現在の値 <= 2745)
                                {
                                    TJAPlayer3.Tx.ScoreRank.Opacity = 255;
                                    TJAPlayer3.Tx.ScoreRank.vc拡大縮小倍率.X = 1.0f;
                                    TJAPlayer3.Tx.ScoreRank.vc拡大縮小倍率.Y = 1.0f;
                                    x = 0;
                                }
                                if (counterJ2[i].n現在の値 >= 2745 && counterJ2[i].n現在の値 <= 3000)
                                {
                                    TJAPlayer3.Tx.ScoreRank.Opacity = 255 - ((counterJ2[i].n現在の値 - 2745));
                                    x = -((counterJ2[i].n現在の値 - 2745) / 5.0f);
                                }

                                TJAPlayer3.Tx.ScoreRank.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 87, 720 - (98 + (int)x), new System.Drawing.Rectangle(0, i == 0 ? i * 114 : i * 120, 140, i == 0 ? 114 : 120));
                                */

                                #endregion
                        }
                        #endregion
                    }
                    else if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
                    {
                        #region [Tower score ranks]

                        double progress = CFloorManagement.LastRegisteredFloor / (double)TJAPlayer3.stage選曲.r確定された曲.arスコア[5].譜面情報.nTotalFloor;

                        bool[] conditions =
                        {
                            progress >= 0.1,
                            progress >= 0.25,
                            progress >= 0.5,
                            progress >= 0.75,
                            progress == 1 && CFloorManagement.CurrentNumberOfLives > 0,
                            TJAPlayer3.stage演奏ドラム画面.CChartScore[0].nMiss == 0,
                            TJAPlayer3.stage演奏ドラム画面.CChartScore[0].nGood == 0
                        };

                        counter[i].t進行();

                        bool satisfied = true;
                        for (int j = 0; j <= i; j++)
                            if (conditions[j] == false)
                            {
                                satisfied = false;
                                break;
                            }
                                

                        if (satisfied == true)
                        {
                            displayScoreRank(i, 0, x, 1);
                        }

                        #endregion
                    }
                }
                
                
            }

            //TJAPlayer3.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.白, ScoreRank[6].ToString());
            //TJAPlayer3.act文字コンソール.tPrint(0, 10, C文字コンソール.Eフォント種別.白, ScoreRank2P[6].ToString());

            return base.On進行描画();
        }

        public int[] ScoreRank;
        public int[] ScoreRank2P;
        private CCounter[] counter = new CCounter[7];
        private CCounter[] counterJ2 = new CCounter[7];
    }
}
