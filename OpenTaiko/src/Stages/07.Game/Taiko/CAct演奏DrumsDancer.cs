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

            if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower || TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
                return;

            var preset = HScenePreset.GetBGPreset();

            Random random = new Random();

            Dancer_In = new CTexture[5][];
            Dancer_Out = new CTexture[5][];
            Dancer = new CTexture[5][];
            DancerStates = new int[5];
            nNowDancerInCounter = new float[5];
            nNowDancerOutCounter = new float[5];



            var dancerOrigindir = CSkin.Path($"{TextureLoader.BASE}{TextureLoader.GAME}{TextureLoader.DANCER}");
            if (System.IO.Directory.Exists($@"{dancerOrigindir}"))
            {
                var dirs = System.IO.Directory.GetDirectories($@"{dancerOrigindir}");
                if (dirs.Length > 0)
                {
                    var _presetPath = (preset != null && preset.DancerSet != null) ? $@"{dancerOrigindir}" + preset.DancerSet[random.Next(0, preset.DancerSet.Length)] : "";
                    var path = (preset != null && System.IO.Directory.Exists(_presetPath)) 
                        ?  _presetPath
                        : dirs[random.Next(0, dirs.Length)];

                    LoadDancerConifg(path);

                    Dancer_In = new CTexture[nDancerCount][];
                    Dancer_Out = new CTexture[nDancerCount][];
                    Dancer = new CTexture[nDancerCount][];
                    DancerStates = new int[nDancerCount];
                    nNowDancerInCounter = new float[nDancerCount];
                    nNowDancerOutCounter = new float[nDancerCount];

                    nDancerInPtn = TJAPlayer3.t連番画像の枚数を数える($@"{path}{Path.DirectorySeparatorChar}1_In{Path.DirectorySeparatorChar}");
                    if (nDancerInPtn != 0)
                    {
                        for (int i = 0; i < nDancerCount; i++)
                        {
                            Dancer_In[i] = new CTexture[nDancerInPtn];
                            for (int p = 0; p < nDancerInPtn; p++)
                            {
                                Dancer_In[i][p] = TJAPlayer3.tテクスチャの生成($@"{path}{Path.DirectorySeparatorChar}{(i + 1)}_In{Path.DirectorySeparatorChar}{p}.png");
                            }
                        }
                    }

                    nDancerOutPtn = TJAPlayer3.t連番画像の枚数を数える($@"{path}{Path.DirectorySeparatorChar}1_Out{Path.DirectorySeparatorChar}");
                    if (nDancerOutPtn != 0)
                    {
                        for (int i = 0; i < nDancerCount; i++)
                        {
                            Dancer_Out[i] = new CTexture[nDancerOutPtn];
                            for (int p = 0; p < nDancerOutPtn; p++)
                            {
                                Dancer_Out[i][p] = TJAPlayer3.tテクスチャの生成($@"{path}{Path.DirectorySeparatorChar}{(i + 1)}_Out{Path.DirectorySeparatorChar}{p}.png");
                            }
                        }
                    }

                    nDancerPtn = TJAPlayer3.t連番画像の枚数を数える($@"{path}{Path.DirectorySeparatorChar}1{Path.DirectorySeparatorChar}");
                    if (nDancerPtn != 0)
                    {
                        for (int i = 0; i < nDancerCount; i++)
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

            arMotionArray_In = CConversion.StringToIntArray(Game_Dancer_In_Motion);
            if(this.arMotionArray_In == null) arMotionArray_In = CConversion.StringToIntArray("0,0");

            arMotionArray_Out = CConversion.StringToIntArray(Game_Dancer_Out_Motion);
            if(this.arMotionArray_Out == null) arMotionArray_Out = CConversion.StringToIntArray("0,0");

            this.ar踊り子モーション番号 = CConversion.StringToIntArray(TJAPlayer3.Skin.Game_Dancer_Motion);
            if(this.ar踊り子モーション番号 == null) ar踊り子モーション番号 = CConversion.StringToIntArray("0,0");

            nNowDancerCounter = 0;
            nNowDancerFrame = 0;

            base.Activate();
        }

        public override void DeActivate()
        {
            if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower || TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
                return;
                
            //this.ct踊り子モーション = null;

            for (int i = 0; i < nDancerCount; i++)
            {
                TJAPlayer3.t安全にDisposeする(ref Dancer_In[i]);
                TJAPlayer3.t安全にDisposeする(ref Dancer_Out[i]);
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

            if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] != (int)Difficulty.Tower && TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] != (int)Difficulty.Dan)
            {
                if (TJAPlayer3.ConfigIni.ShowDancer && (this.ar踊り子モーション番号.Length - 1) != 0)
                {
                    if (!TJAPlayer3.stage演奏ドラム画面.bPAUSE) nNowDancerCounter += Math.Abs((float)TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM[0] / 60.0f) * (float)TJAPlayer3.FPS.DeltaTime / nDancerBeat;
                    if (nNowDancerCounter >= 1)
                    {
                        nNowDancerCounter = 0;
                    }
                    nNowDancerFrame = (int)(nNowDancerCounter * (this.ar踊り子モーション番号.Length - 1));

                    for (int i = 0; i < nDancerCount; i++)
                    {
                        if ((int)TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[0] >= TJAPlayer3.Skin.Game_Dancer_Gauge[i])
                        {
                            if (DancerStates[i] == 0)
                            {
                                DancerStates[i] = 1;
                                nNowDancerInCounter[i] = 0;
                            }
                        }
                        else
                        {
                            if (DancerStates[i] == 3)
                            {
                                DancerStates[i] = 2;
                                nNowDancerOutCounter[i] = 0;
                            }
                        }
                        
                        switch(DancerStates[i])
                        {
                            case 0:
                            break;
                            case 1:
                            {
                                if (nDancerInInterval == 0)
                                {
                                    DancerStates[i] = 3;
                                }
                                else
                                { 
                                    if (!TJAPlayer3.stage演奏ドラム画面.bPAUSE) nNowDancerInCounter[i] += Math.Abs((float)TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM[0] / nDancerInInterval) * (float)TJAPlayer3.FPS.DeltaTime;

                                    if (nNowDancerInCounter[i] >= 1)
                                    {
                                        nNowDancerInCounter[i] = 1;
                                        DancerStates[i] = 3;
                                    }
                        
                                    int frame = (int)(nNowDancerInCounter[i] * (this.arMotionArray_In.Length - 1));
                                    if (this.Dancer_In[i] != null && this.Dancer_In[i].Length > 0 && this.Dancer_In[i][this.arMotionArray_In[frame]] != null)
                                    {
                                        this.Dancer_In[i][this.arMotionArray_In[frame]].t2D中心基準描画(TJAPlayer3.Skin.Game_Dancer_X[i], TJAPlayer3.Skin.Game_Dancer_Y[i]);
                                    }
                                }
                               
                            }
                            break;
                            case 2:
                            {
                                if (nDancerOutInterval == 0)
                                {
                                    DancerStates[i] = 0;
                                }
                                else
                                { 
                                    if (!TJAPlayer3.stage演奏ドラム画面.bPAUSE) nNowDancerOutCounter[i] += Math.Abs((float)TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM[0] / nDancerOutInterval) * (float)TJAPlayer3.FPS.DeltaTime;

                                    if (nNowDancerOutCounter[i] >= 1)
                                    {
                                        nNowDancerOutCounter[i] = 1;
                                        DancerStates[i] = 0;
                                    }

                                    int frame = (int)(nNowDancerOutCounter[i] * (this.arMotionArray_Out.Length - 1));
                                    if (this.Dancer_Out[i] != null && this.Dancer_Out[i].Length > 0 && this.Dancer_Out[i][this.arMotionArray_Out[frame]] != null)
                                    {
                                        this.Dancer_Out[i][this.arMotionArray_Out[frame]].t2D中心基準描画(TJAPlayer3.Skin.Game_Dancer_X[i], TJAPlayer3.Skin.Game_Dancer_Y[i]);
                                    }
                                }
                            }
                            break;
                            case 3:
                            if (this.Dancer[i] != null && this.Dancer[i].Length > 0 && this.Dancer[i][this.ar踊り子モーション番号[nNowDancerFrame]] != null)
                            {
                                this.Dancer[i][this.ar踊り子モーション番号[nNowDancerFrame]].t2D中心基準描画(TJAPlayer3.Skin.Game_Dancer_X[i], TJAPlayer3.Skin.Game_Dancer_Y[i]);
                            }
                            break;
                        }
                    }
                }
            }

            return base.Draw();
        }

        #region[ private ]
        //-----------------
        private int nDancerCount;
        private float[] nNowDancerInCounter;
        private float[] nNowDancerOutCounter;
        private float nNowDancerCounter;
        private int nNowDancerFrame;
        private int nDancerInPtn;
        private int nDancerOutPtn;
        private int nDancerPtn;
        private float nDancerBeat;
        private float nDancerInInterval;
        private float nDancerOutInterval;
        private int[] arMotionArray_In;
        private int[] arMotionArray_Out;
        private int[] ar踊り子モーション番号;
        //public CCounter ct踊り子モーション;
        private CTexture[][] Dancer_In;
        private CTexture[][] Dancer_Out;
        private CTexture[][] Dancer;
        private int[] DancerStates;
        private string Game_Dancer_In_Motion;
        private string Game_Dancer_Out_Motion;

        private void LoadDancerConifg(string dancerPath)
        {
            var _str = "";
            TJAPlayer3.Skin.LoadSkinConfigFromFile(dancerPath + @$"{Path.DirectorySeparatorChar}DancerConfig.txt", ref _str);

            string[] delimiter = { "\n" };
            string[] strSingleLine = _str.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

            TJAPlayer3.Skin.Game_Dancer_X = new int[] { 640, 430, 856, 215, 1070 };
            TJAPlayer3.Skin.Game_Dancer_Y = new int[] { 500, 500, 500, 500, 500 };
            nDancerCount = 5;
            nDancerInInterval = 0;
            nDancerOutInterval = 0;

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

                            if (strCommand == "Game_Dancer_Count")
                            {
                                nDancerCount = int.Parse(strParam);
                                TJAPlayer3.Skin.Game_Dancer_X = new int[nDancerCount];
                                TJAPlayer3.Skin.Game_Dancer_Y = new int[nDancerCount];
                            }
                            else if (strCommand == "Game_Dancer_X")
                            {
                                string[] strSplit = strParam.Split(',');
                                for (int i = 0; i < nDancerCount; i++)
                                {
                                    TJAPlayer3.Skin.Game_Dancer_X[i] = int.Parse(strSplit[i]);
                                }
                            }
                            else if (strCommand == "Game_Dancer_Y")
                            {
                                string[] strSplit = strParam.Split(',');
                                for (int i = 0; i < nDancerCount; i++)
                                {
                                    TJAPlayer3.Skin.Game_Dancer_Y[i] = int.Parse(strSplit[i]);
                                }
                            }
                            else if (strCommand == "Game_Dancer_Motion")
                            {
                                TJAPlayer3.Skin.Game_Dancer_Motion = strParam;
                            }
                            else if (strCommand == "Game_Dancer_In_Motion")
                            {
                                Game_Dancer_In_Motion = strParam;
                            }
                            else if (strCommand == "Game_Dancer_Out_Motion")
                            {
                                Game_Dancer_Out_Motion = strParam;
                            }
                            else if (strCommand == "Game_Dancer_Beat")
                            {
                                nDancerBeat = int.Parse(strParam);
                            }
                            else if (strCommand == "Game_Dancer_In_Interval")
                            {
                                nDancerInInterval = int.Parse(strParam);
                            }
                            else if (strCommand == "Game_Dancer_Out_Interval")
                            {
                                nDancerOutInterval = int.Parse(strParam);
                            }
                            else if (strCommand == "Game_Dancer_Gauge")
                            {
                                string[] strSplit = strParam.Split(',');
                                for (int i = 0; i < nDancerCount; i++)
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
