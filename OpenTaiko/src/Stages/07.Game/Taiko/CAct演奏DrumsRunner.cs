using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using FDK;
using System.Diagnostics;

namespace TJAPlayer3
{
    internal class CAct演奏DrumsRunner : CActivity
    {
        /// <summary>
        /// ランナー
        /// </summary>
        public CAct演奏DrumsRunner()
        {
            base.IsDeActivated = true;
        }

        public void Start(int Player, bool IsMiss, CDTX.CChip pChip)
        {
            if (Runner != null && !TJAPlayer3.ConfigIni.SimpleMode)
            {
                while (stRunners[Index].b使用中)
                {
                    Index += 1;
                    if (Index >= 128)
                    {
                        Index = 0;
                        break; // 2018.6.15 IMARER 無限ループが発生するので修正
                    }
                }
                if (pChip.nチャンネル番号 < 0x15 || (pChip.nチャンネル番号 >= 0x1A))
                {
                    if (!stRunners[Index].b使用中)
                    {
                        stRunners[Index].b使用中 = true;
                        stRunners[Index].nPlayer = Player;
                        if (IsMiss == true)
                        {
                            stRunners[Index].nType = 0;
                        }
                        else
                        {
                            stRunners[Index].nType = random.Next(1, Type + 1);
                        }
                        stRunners[Index].ct進行 = new CCounter(0, TJAPlayer3.Skin.Resolution[0], Timer, TJAPlayer3.Timer);
                        stRunners[Index].nOldValue = 0;
                        stRunners[Index].nNowPtn = 0;
                        stRunners[Index].fX = 0;
                    }

                }
            }
        }

        public override void Activate()
        {
            if (TJAPlayer3.ConfigIni.SimpleMode)
            {
                base.Activate();
                return;
            }

            for (int i = 0; i < 128; i++)
            {
                stRunners[i] = new STRunner();
                stRunners[i].b使用中 = false;
                stRunners[i].ct進行 = new CCounter();
            }
            
            var preset = HScenePreset.GetBGPreset();
            
            Random random = new Random();

            var dancerOrigindir = CSkin.Path($"{TextureLoader.BASE}{TextureLoader.GAME}{TextureLoader.RUNNER}");
            if (System.IO.Directory.Exists($@"{dancerOrigindir}"))
            {
                var dirs = System.IO.Directory.GetDirectories($@"{dancerOrigindir}");
                if (dirs.Length > 0)
                {
                    var _presetPath = (preset != null && preset.RunnerSet != null) ? $@"{dancerOrigindir}" + preset.RunnerSet[random.Next(0, preset.RunnerSet.Length)] : "";
                    var path = (preset != null && System.IO.Directory.Exists(_presetPath)) 
                        ?  _presetPath
                        : dirs[random.Next(0, dirs.Length)];
                    LoadRunnerConifg(path);

                    Runner = TJAPlayer3.tテクスチャの生成($@"{path}{Path.DirectorySeparatorChar}Runner.png");
                }
            }

            // フィールド上で代入してたためこちらへ移動。
            base.Activate();
        }

        public override void DeActivate()
        {
            if (TJAPlayer3.ConfigIni.SimpleMode)
            {
                base.DeActivate();
                return;
            }

            for (int i = 0; i < 128; i++)
            {
                stRunners[i].ct進行 = null;
            }
            
            TJAPlayer3.t安全にDisposeする(ref Runner);
            
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
            if (TJAPlayer3.ConfigIni.SimpleMode)
            {
                return base.Draw();
            }

            for (int i = 0; i < 128; i++)
            {
                if (stRunners[i].b使用中)
                {
                    stRunners[i].nOldValue = stRunners[i].ct進行.CurrentValue;
                    stRunners[i].ct進行.Tick();
                    if (stRunners[i].ct進行.IsEnded || stRunners[i].fX > TJAPlayer3.Skin.Resolution[0])
                    {
                        stRunners[i].ct進行.Stop();
                        stRunners[i].b使用中 = false;
                    }
                    for (int n = stRunners[i].nOldValue; n < stRunners[i].ct進行.CurrentValue; n++)
                    {
                        stRunners[i].fX += (float)TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM[stRunners[i].nPlayer] / 18;
                        int Width = TJAPlayer3.Skin.Resolution[0] / Ptn;
                        stRunners[i].nNowPtn = (int)stRunners[i].fX / Width;
                    }
                    if (Runner != null)
                    {
                        if (stRunners[i].nPlayer == 0)
                        {
                            Runner.t2D描画((int)(StartPoint_X[0] + stRunners[i].fX), StartPoint_Y[0], new Rectangle(stRunners[i].nNowPtn * Size[0], stRunners[i].nType * Size[1], Size[0], Size[1]));
                        }
                        else
                        {
                            Runner.t2D描画((int)(StartPoint_X[1] + stRunners[i].fX), StartPoint_Y[1], new Rectangle(stRunners[i].nNowPtn * Size[0], stRunners[i].nType * Size[1], Size[0], Size[1]));
                        }
                    }
                }
            }
            return base.Draw();
        }

        #region[ private ]
        //-----------------
        [StructLayout(LayoutKind.Sequential)]
        private struct STRunner
        {
            public bool b使用中;
            public int nPlayer;
            public int nType;
            public int nOldValue;
            public int nNowPtn;
            public float fX;
            public CCounter ct進行;
        }
        private STRunner[] stRunners = new STRunner[128];
        Random random = new Random();
        int Index = 0;

        private CTexture Runner;

        private void LoadRunnerConifg(string dancerPath)
        {
            var _str = "";
            TJAPlayer3.Skin.LoadSkinConfigFromFile(dancerPath + @"\RunnerConfig.txt", ref _str);

            string[] delimiter = { "\n" };
            string[] strSingleLine = _str.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

            Size = new int[2] { 60, 125 };
            Ptn = 48;
            Type = 4;
            StartPoint_X = new int[2] { 175, 175 };
            StartPoint_Y = new int[2] { 40, 560 };
            Timer = 16;

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

                            if (strCommand == "Game_Runner_Size")
                            {
                                string[] strSplit = strParam.Split(',');
                                for (int i = 0; i < 2; i++)
                                {
                                    Size[i] = int.Parse(strSplit[i]);
                                }
                            }
                            else if (strCommand == "Game_Runner_Ptn")
                            {
                                Ptn = int.Parse(strParam);
                            }
                            else if (strCommand == "Game_Runner_Type")
                            {
                                Type = int.Parse(strParam);
                            }
                            else if (strCommand == "Game_Runner_Timer")
                            {
                                Timer = int.Parse(strParam);
                            }
                            else if (strCommand == "Game_Runner_StartPoint_X")
                            {
                                string[] strSplit = strParam.Split(',');
                                for (int i = 0; i < 2; i++)
                                {
                                    StartPoint_X[i] = int.Parse(strSplit[i]);
                                }
                            }
                            else if (strCommand == "Game_Runner_StartPoint_Y")
                            {
                                string[] strSplit = strParam.Split(',');
                                for (int i = 0; i < 2; i++)
                                {
                                    StartPoint_Y[i] = int.Parse(strSplit[i]);
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

        // ランナー画像のサイズ。 X, Y
        private int[] Size;
        // ランナーのコマ数
        private int Ptn;
        // ランナーのキャラクターのバリエーション(ミス時を含まない)。
        private int Type;
        private int Timer;
        // スタート地点のX座標 1P, 2P
        private int[] StartPoint_X;
        // スタート地点のY座標 1P, 2P
        private int[] StartPoint_Y;

        //-----------------
        #endregion
    }
}