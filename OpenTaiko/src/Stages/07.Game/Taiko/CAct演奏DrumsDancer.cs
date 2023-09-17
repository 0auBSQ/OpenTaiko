using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using FDK;
using System.Diagnostics;

namespace TJAPlayer3
{
    internal class CAct演奏DrumsDancer : CActivity
    {
        /// <summary>
        /// 踊り子
        /// </summary>
        public CAct演奏DrumsDancer()
        {
            base.IsDeActivated = true;
        }

        public override void Activate()
        {
            //this.ct踊り子モーション = new CCounter();
            
            Random random = new Random();
            Dancer = new CTexture[5][];

            var dancerOrigindir = CSkin.Path($"{TextureLoader.BASE}{TextureLoader.GAME}{TextureLoader.DANCER}");
            if (System.IO.Directory.Exists($@"{dancerOrigindir}"))
            {
                var dirs = System.IO.Directory.GetDirectories($@"{dancerOrigindir}");
                if (dirs.Length > 0)
                {
                    var path = dirs[random.Next(0, dirs.Length)];
                    LoadDancerConifg(path);

                    nDancerPtn = TJAPlayer3.t連番画像の枚数を数える($@"{path}{Path.DirectorySeparatorChar}1{Path.DirectorySeparatorChar}");
                    if (nDancerPtn != 0)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            Dancer[i] = new CTexture[nDancerPtn];
                            for (int p = 0; p < nDancerPtn; p++)
                            {
                                Dancer[i][p] = TJAPlayer3.tテクスチャの生成($@"{path}{Path.DirectorySeparatorChar}{(i + 1)}{Path.DirectorySeparatorChar}{p}.png");
                            }
                        }
                    }
                }
            }

            this.ar踊り子モーション番号 = CConversion.StringToIntArray(TJAPlayer3.Skin.Game_Dancer_Motion);
            if(this.ar踊り子モーション番号 == null) ar踊り子モーション番号 = CConversion.StringToIntArray("0,0");

            nNowDancerCounter = 0;
            nNowDancerFrame = 0;

            base.Activate();
        }

        public override void DeActivate()
        {
            //this.ct踊り子モーション = null;

            for (int i = 0; i < 5; i++)
            {
                TJAPlayer3.t安全にDisposeする(ref Dancer[i]);
            }

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
            if( this.IsFirstDraw )
            {
                this.IsFirstDraw = true;
            }

            if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Tower && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan)
            {
                if (TJAPlayer3.ConfigIni.ShowDancer && (this.ar踊り子モーション番号.Length - 1) != 0)
                {
                    if (!TJAPlayer3.stage演奏ドラム画面.bPAUSE) nNowDancerCounter += (Math.Abs((float)TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM[0] / 60.0f) * (float)TJAPlayer3.FPS.DeltaTime) * (this.ar踊り子モーション番号.Length - 1) / nDancerBeat;
                    nNowDancerFrame = (int)nNowDancerCounter;
                    nNowDancerFrame = Math.Min(nNowDancerFrame, (this.ar踊り子モーション番号.Length - 1));
                    bool endAnime = nNowDancerFrame >= (this.ar踊り子モーション番号.Length - 1) - 1;

                    if (endAnime)
                    {
                        nNowDancerCounter = 0;
                        nNowDancerFrame = 0;
                    }
                    for (int i = 0; i < 5; i++)
                    {
                        if (this.Dancer[i] != null && this.Dancer[i][this.ar踊り子モーション番号[nNowDancerFrame]] != null)
                        {
                            if ((int)TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[0] >= TJAPlayer3.Skin.Game_Dancer_Gauge[i])
                                this.Dancer[i][this.ar踊り子モーション番号[nNowDancerFrame]].t2D中心基準描画(TJAPlayer3.Skin.Game_Dancer_X[i], TJAPlayer3.Skin.Game_Dancer_Y[i]);
                        }
                    }
                }
            }

            return base.Draw();
        }

        #region[ private ]
        //-----------------
        private float nNowDancerCounter;
        private int nNowDancerFrame;
        private int nDancerPtn;
        private float nDancerBeat;
        private int[] ar踊り子モーション番号;
        //public CCounter ct踊り子モーション;
        private CTexture[][] Dancer;

        private void LoadDancerConifg(string dancerPath)
        {
            var _str = "";
            TJAPlayer3.Skin.LoadSkinConfigFromFile(dancerPath + @$"{Path.DirectorySeparatorChar}DancerConfig.txt", ref _str);

            string[] delimiter = { "\n" };
            string[] strSingleLine = _str.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

            TJAPlayer3.Skin.Game_Dancer_X = new int[] { 640, 430, 856, 215, 1070 };
            TJAPlayer3.Skin.Game_Dancer_Y = new int[] { 500, 500, 500, 500, 500 };

            foreach (string s in strSingleLine)
            {
                string str = s.Replace('\t', ' ').TrimStart(new char[] { '\t', ' ' });
                if ((str.Length != 0) && (str[0] != ';'))
                {
                    try
                    {
                        string strCommand;
                        string strParam;
                        string[] strArray = str.Split(new char[] { '=' });

                        if (strArray.Length == 2)
                        {
                            strCommand = strArray[0].Trim();
                            strParam = strArray[1].Trim();

                            if (strCommand == "Game_Dancer_X")
                            {
                                string[] strSplit = strParam.Split(',');
                                for (int i = 0; i < 5; i++)
                                {
                                    TJAPlayer3.Skin.Game_Dancer_X[i] = int.Parse(strSplit[i]);
                                }
                            }
                            else if (strCommand == "Game_Dancer_Y")
                            {
                                string[] strSplit = strParam.Split(',');
                                for (int i = 0; i < 5; i++)
                                {
                                    TJAPlayer3.Skin.Game_Dancer_Y[i] = int.Parse(strSplit[i]);
                                }
                            }
                            else if (strCommand == "Game_Dancer_Motion")
                            {
                                TJAPlayer3.Skin.Game_Dancer_Motion = strParam;
                            }
                            // Game_Dancer_PtnはTextrueLoader.csで反映されます。
                            else if (strCommand == "Game_Dancer_Beat")
                            {
                                nDancerBeat = int.Parse(strParam);
                            }
                            else if (strCommand == "Game_Dancer_Gauge")
                            {
                                string[] strSplit = strParam.Split(',');
                                for (int i = 0; i < 5; i++)
                                {
                                    TJAPlayer3.Skin.Game_Dancer_Gauge[i] = int.Parse(strSplit[i]);
                                }
                            }

                        }
                        continue;
                    }
                    catch (Exception exception)
                    {
                        Trace.TraceError(exception.ToString());
                        Trace.TraceError("例外が発生しましたが処理を継続します。 (6a32cc37-1527-412e-968a-512c1f0135cd)");
                        continue;
                    }
                }
            }

        }
        //-----------------
        #endregion
    }
}
