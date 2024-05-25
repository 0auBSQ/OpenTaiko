using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using FDK;
using ManagedBass;
using SampleFramework;

namespace TJAPlayer3
{
    internal class CAct演奏Drums連打キャラ : CActivity
    {
        // コンストラクタ

        public CAct演奏Drums連打キャラ()
        {
            base.IsDeActivated = true;
        }


        // メソッド
        public virtual void Start(int player)
        {
            if (TJAPlayer3.ConfigIni.SimpleMode) return;

            //if( CDTXMania.Tx.Effects_Roll[0] != null )
            //{
            //    int[] arXseed = new int[] { 56, -10, 200, 345, 100, 451, 600, 260, -30, 534, 156, 363 };
            //    for (int i = 0; i < 1; i++)
            //    {
            //        for (int j = 0; j < 64; j++)
            //        {
            //            if (!this.st連打キャラ[j].b使用中)
            //            {
            //                this.st連打キャラ[j].b使用中 = true;
            //                if(this.nTex枚数 <= 1) this.st連打キャラ[j].nColor = 0;
            //                else this.st連打キャラ[j].nColor = CDTXMania.Random.Next( 0, this.nTex枚数 - 1);
            //                this.st連打キャラ[j].ct進行 = new CCounter( 0, 1000, 4, CDTXMania.Timer); // カウンタ

            //                //位置生成(β版)
            //                int nXseed = CDTXMania.Random.Next(12);
            //                this.st連打キャラ[ j ].fX開始点 = arXseed[ nXseed ];
            //                this.st連打キャラ[j].fX = arXseed[ nXseed ];
            //                this.st連打キャラ[j].fY = 720;
            //                this.st連打キャラ[j].fX加速度 = 5/2;
            //                this.st連打キャラ[j].fY加速度 = 5/2;
            //                break;
            //            }
            //        }
            //    }
            //}
            for (int i = 0; i < 128; i++)
            {
                if (!RollCharas[i].IsUsing)
                {
                    RollCharas[i].IsUsing = true;
                    RollCharas[i].Type = random.Next(0, Game_Effect_Roll_Ptn);
                    RollCharas[i].OldValue = 0;
                    RollCharas[i].Counter = new CCounter(0, 5000, 1, TJAPlayer3.Timer);
                    if (TJAPlayer3.stage演奏ドラム画面.bDoublePlay)
                    {
                        switch (player)
                        {
                            case 0:
                                RollCharas[i].X = Game_Effect_Roll_StartPoint_1P_X[random.Next(0, Game_Effect_Roll_StartPoint_1P_X.Length)];
                                RollCharas[i].Y = Game_Effect_Roll_StartPoint_1P_Y[random.Next(0, Game_Effect_Roll_StartPoint_1P_Y.Length)];
                                RollCharas[i].XAdd = Game_Effect_Roll_Speed_1P_X[random.Next(0, Game_Effect_Roll_Speed_1P_X.Length)];
                                RollCharas[i].YAdd = Game_Effect_Roll_Speed_1P_Y[random.Next(0, Game_Effect_Roll_Speed_1P_Y.Length)];
                                break;
                            case 1:
                                RollCharas[i].X = Game_Effect_Roll_StartPoint_2P_X[random.Next(0, Game_Effect_Roll_StartPoint_2P_X.Length)];
                                RollCharas[i].Y = Game_Effect_Roll_StartPoint_2P_Y[random.Next(0, Game_Effect_Roll_StartPoint_2P_Y.Length)];
                                RollCharas[i].XAdd = Game_Effect_Roll_Speed_2P_X[random.Next(0, Game_Effect_Roll_Speed_2P_X.Length)];
                                RollCharas[i].YAdd = Game_Effect_Roll_Speed_2P_Y[random.Next(0, Game_Effect_Roll_Speed_2P_Y.Length)];
                                break;
                            default:
                                return;
                        }
                    }
                    else
                    {
                        RollCharas[i].X = Game_Effect_Roll_StartPoint_X[random.Next(0, Game_Effect_Roll_StartPoint_X.Length)];
                        RollCharas[i].Y = Game_Effect_Roll_StartPoint_Y[random.Next(0, Game_Effect_Roll_StartPoint_Y.Length)];
                        RollCharas[i].XAdd = Game_Effect_Roll_Speed_X[random.Next(0, Game_Effect_Roll_Speed_X.Length)];
                        RollCharas[i].YAdd = Game_Effect_Roll_Speed_Y[random.Next(0, Game_Effect_Roll_Speed_Y.Length)];
                    }
                    break;
                }
            }

        }

        // CActivity 実装

        public override void Activate()
        {
            //for (int i = 0; i < 64; i++)
            //{
            //    this.st連打キャラ[i] = new ST連打キャラ();
            //    this.st連打キャラ[i].b使用中 = false;
            //    this.st連打キャラ[i].ct進行 = new CCounter();
            //}
            for (int i = 0; i < 128; i++)
            {
                RollCharas[i] = new RollChara();
                RollCharas[i].IsUsing = false;
                RollCharas[i].Counter = new CCounter();
            }
            // SkinConfigで指定されたいくつかの変数からこのクラスに合ったものに変換していく
            var preset = HScenePreset.GetBGPreset();

            Random random = new Random();

            var rollEffectOrigindir = CSkin.Path($"{TextureLoader.BASE}{TextureLoader.GAME}{TextureLoader.EFFECTS}Roll{Path.DirectorySeparatorChar}");
            if (System.IO.Directory.Exists($@"{rollEffectOrigindir}"))
            {
                var dirs = System.IO.Directory.GetDirectories($@"{rollEffectOrigindir}");
                if (dirs.Length > 0)
                {
                    var _presetPath = (preset != null && preset.RollEffectSet != null) ? $@"{rollEffectOrigindir}" + preset.RollEffectSet[random.Next(0, preset.RollEffectSet.Length)] : "";
                    var path = (preset != null && System.IO.Directory.Exists(_presetPath))
                        ? _presetPath
                        : dirs[random.Next(0, dirs.Length)];
                    Game_Effect_Roll_Ptn = TJAPlayer3.t連番画像の枚数を数える($@"{path}{Path.DirectorySeparatorChar}");
                    Effects_Roll = new CTexture[Game_Effect_Roll_Ptn];
                    for (int i = 0; i < Game_Effect_Roll_Ptn; i++)
                    {
                        Effects_Roll[i] = TJAPlayer3.tテクスチャの生成($@"{path}{Path.DirectorySeparatorChar}{i.ToString()}.png");
                    }

                    //EffectConfig.txt
                    if (System.IO.File.Exists($@"{path}{Path.DirectorySeparatorChar}EffectConfig.txt"))
                    {
                        var _str = "";
                        TJAPlayer3.Skin.LoadSkinConfigFromFile($@"{path}{Path.DirectorySeparatorChar}EffectConfig.txt", ref _str);

                        string[] delimiter = { "\n", "\r" };
                        string[] strSingleLine = _str.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string line in strSingleLine)
                        {
                            var line2 = line.Replace(" ", "");
                            if (line2.StartsWith("Game_Effect_Roll_StartPoint_X="))
                            {
                                int[] values = line2.Substring(30).Trim().Split(',').Select(int.Parse).ToArray();
                                Game_Effect_Roll_StartPoint_X = values;
                            }
                            else if (line2.StartsWith("Game_Effect_Roll_StartPoint_Y="))
                            {
                                int[] values = line2.Substring(30).Trim().Split(',').Select(int.Parse).ToArray();
                                Game_Effect_Roll_StartPoint_Y = values;
                            }
                            else if (line2.StartsWith("Game_Effect_Roll_StartPoint_1P_X="))
                            {
                                int[] values = line2.Substring(33).Trim().Split(',').Select(int.Parse).ToArray();
                                Game_Effect_Roll_StartPoint_1P_X = values;
                            }
                            else if (line2.StartsWith("Game_Effect_Roll_StartPoint_1P_Y="))
                            {
                                int[] values = line2.Substring(33).Trim().Split(',').Select(int.Parse).ToArray();
                                Game_Effect_Roll_StartPoint_1P_Y = values;
                            }
                            else if (line2.StartsWith("Game_Effect_Roll_StartPoint_2P_X="))
                            {
                                int[] values = line2.Substring(33).Trim().Split(',').Select(int.Parse).ToArray();
                                Game_Effect_Roll_StartPoint_2P_X = values;
                            }
                            else if (line2.StartsWith("Game_Effect_Roll_StartPoint_2P_Y="))
                            {
                                int[] values = line2.Substring(33).Trim().Split(',').Select(int.Parse).ToArray();
                                Game_Effect_Roll_StartPoint_2P_Y = values;
                            }
                            else if (line2.StartsWith("Game_Effect_Roll_Speed_X="))
                            {
                                float[] values = line2.Substring(25).Trim().Split(',').Select(float.Parse).ToArray();
                                Game_Effect_Roll_Speed_X = values;
                            }
                            else if (line2.StartsWith("Game_Effect_Roll_Speed_Y="))
                            {
                                float[] values = line2.Substring(25).Trim().Split(',').Select(float.Parse).ToArray();
                                Game_Effect_Roll_Speed_Y = values;
                            }
                            else if (line2.StartsWith("Game_Effect_Roll_Speed_1P_X="))
                            {
                                float[] values = line2.Substring(28).Trim().Split(',').Select(float.Parse).ToArray();
                                Game_Effect_Roll_Speed_1P_X = values;
                            }
                            else if (line2.StartsWith("Game_Effect_Roll_Speed_1P_Y="))
                            {
                                float[] values = line2.Substring(28).Trim().Split(',').Select(float.Parse).ToArray();
                                Game_Effect_Roll_Speed_1P_Y = values;
                            }
                            else if (line2.StartsWith("Game_Effect_Roll_Speed_2P_X="))
                            {
                                float[] values = line2.Substring(28).Trim().Split(',').Select(float.Parse).ToArray();
                                Game_Effect_Roll_Speed_2P_X = values;
                            }
                            else if (line2.StartsWith("Game_Effect_Roll_Speed_2P_Y="))
                            {
                                float[] values = line2.Substring(28).Trim().Split(',').Select(float.Parse).ToArray();
                                Game_Effect_Roll_Speed_2P_Y = values;
                            }
                        }
                    }
                }
            }
            base.Activate();
        }
        public override void DeActivate()
        {
            //for (int i = 0; i < 64; i++)
            //{
            //    this.st連打キャラ[i].ct進行 = null;
            //}
            for (int i = 0; i < 128; i++)
            {
                RollCharas[i].Counter = null;
            }
            base.DeActivate();
        }
        public override void CreateManagedResource()
        {
            //this.nTex枚数 = 4;
            //this.txChara = new CTexture[ this.nTex枚数 ];

            //for (int i = 0; i < this.nTex枚数; i++)
            //{
            //    this.txChara[ i ] = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\RollEffect\00\" + i.ToString() + ".png" ) );
            //}
            base.CreateManagedResource();
        }
        public override void ReleaseManagedResource()
        {
            //        for (int i = 0; i < this.nTex枚数; i++)
            //        {
            //CDTXMania.tテクスチャの解放( ref this.txChara[ i ] );
            //        }
            base.ReleaseManagedResource();
        }
        public override int Draw()
        {
            if (!base.IsDeActivated && !TJAPlayer3.ConfigIni.SimpleMode)
            {
                //for( int i = 0; i < 64; i++ )
                //{
                //    if( this.st連打キャラ[i].b使用中 )
                //    {
                //        this.st連打キャラ[i].n前回のValue = this.st連打キャラ[i].ct進行.n現在の値;
                //        this.st連打キャラ[i].ct進行.t進行();
                //        if (this.st連打キャラ[i].ct進行.b終了値に達した)
                //        {
                //            this.st連打キャラ[i].ct進行.t停止();
                //            this.st連打キャラ[i].b使用中 = false;
                //        }
                //        for (int n = this.st連打キャラ[i].n前回のValue; n < this.st連打キャラ[i].ct進行.n現在の値; n++)
                //        {
                //            this.st連打キャラ[i].fX += this.st連打キャラ[i].fX加速度;
                //            this.st連打キャラ[i].fY -= this.st連打キャラ[i].fY加速度;
                //        }

                //        if(CDTXMania.Tx.Effects_Roll[ this.st連打キャラ[ i ].nColor ] != null )
                //        {
                //            CDTXMania.Tx.Effects_Roll[ this.st連打キャラ[ i ].nColor ].t2D描画( CDTXMania.app.Device, (int)this.st連打キャラ[i].fX, (int)this.st連打キャラ[i].fY, new Rectangle( this.st連打キャラ[i].nColor * 0, 0, 128, 128 ) );
                //        }
                //    }

                //}

                if (TJAPlayer3.ConfigIni.nPlayerCount > 2) return 0;

                for (int i = 0; i < 128; i++)
                {
                    if (RollCharas[i].IsUsing)
                    {
                        RollCharas[i].OldValue = RollCharas[i].Counter.CurrentValue;
                        RollCharas[i].Counter.Tick();
                        if (RollCharas[i].Counter.IsEnded)
                        {
                            RollCharas[i].Counter.Stop();
                            RollCharas[i].IsUsing = false;
                        }
                        for (int l = RollCharas[i].OldValue; l < RollCharas[i].Counter.CurrentValue; l++)
                        {
                            RollCharas[i].X += RollCharas[i].XAdd;
                            RollCharas[i].Y += RollCharas[i].YAdd;
                        }

                        if (Effects_Roll[RollCharas[i].Type] != null)
                        {
                            Effects_Roll[RollCharas[i].Type]?.t2D描画(RollCharas[i].X, RollCharas[i].Y);

                            // 画面外にいたら描画をやめさせる
                            if (RollCharas[i].X < 0 - Effects_Roll[RollCharas[i].Type].szTextureSize.Width || RollCharas[i].X > TJAPlayer3.Skin.Resolution[0])
                            {
                                RollCharas[i].Counter.Stop();
                                RollCharas[i].IsUsing = false;
                            }

                            if (RollCharas[i].Y < 0 - Effects_Roll[RollCharas[i].Type].szTextureSize.Height || RollCharas[i].Y > TJAPlayer3.Skin.Resolution[1])
                            {
                                RollCharas[i].Counter.Stop();
                                RollCharas[i].IsUsing = false;
                            }
                        }


                    }
                }
            }
            return 0;
        }


        // その他

        #region [ private ]
        //-----------------
        //private CTexture[] txChara;
        private int nTex枚数;

        [StructLayout(LayoutKind.Sequential)]
        private struct ST連打キャラ
        {
            public int nColor;
            public bool b使用中;
            public CCounter ct進行;
            public int n前回のValue;
            public float fX;
            public float fY;
            public float fX開始点;
            public float fY開始点;
            public float f進行方向; //進行方向 0:左→右 1:左下→右上 2:右→左
            public float fX加速度;
            public float fY加速度;
        }
        private ST連打キャラ[] st連打キャラ = new ST連打キャラ[64];

        [StructLayout(LayoutKind.Sequential)]
        private struct RollChara
        {
            public CCounter Counter;
            public int Type;
            public bool IsUsing;
            public float X;
            public float Y;
            public float XAdd;
            public float YAdd;
            public int OldValue;
        }

        private RollChara[] RollCharas = new RollChara[128];

        private CTexture[] Effects_Roll;
        private int Game_Effect_Roll_Ptn;

        //RollEffects
        private int[] Game_Effect_Roll_StartPoint_X = TJAPlayer3.Skin.Game_Effect_Roll_StartPoint_X;
        private int[] Game_Effect_Roll_StartPoint_Y = TJAPlayer3.Skin.Game_Effect_Roll_StartPoint_Y;
        private int[] Game_Effect_Roll_StartPoint_1P_X = TJAPlayer3.Skin.Game_Effect_Roll_StartPoint_1P_X;
        private int[] Game_Effect_Roll_StartPoint_1P_Y = TJAPlayer3.Skin.Game_Effect_Roll_StartPoint_1P_Y;
        private int[] Game_Effect_Roll_StartPoint_2P_X = TJAPlayer3.Skin.Game_Effect_Roll_StartPoint_2P_X;
        private int[] Game_Effect_Roll_StartPoint_2P_Y = TJAPlayer3.Skin.Game_Effect_Roll_StartPoint_2P_Y;
        public float[] Game_Effect_Roll_Speed_X = TJAPlayer3.Skin.Game_Effect_Roll_Speed_X;
        public float[] Game_Effect_Roll_Speed_Y = TJAPlayer3.Skin.Game_Effect_Roll_Speed_Y;
        public float[] Game_Effect_Roll_Speed_1P_X = TJAPlayer3.Skin.Game_Effect_Roll_Speed_1P_X;
        public float[] Game_Effect_Roll_Speed_1P_Y = TJAPlayer3.Skin.Game_Effect_Roll_Speed_1P_Y;
        public float[] Game_Effect_Roll_Speed_2P_X = TJAPlayer3.Skin.Game_Effect_Roll_Speed_2P_X;
        public float[] Game_Effect_Roll_Speed_2P_Y = TJAPlayer3.Skin.Game_Effect_Roll_Speed_2P_Y;



        private Random random = new Random();

        private int[,] StartPoint;
        private int[,] StartPoint_1P;
        private int[,] StartPoint_2P;
        private float[,] Speed;
        private float[,] Speed_1P;
        private float[,] Speed_2P;
        private int CharaPtn;
        //-----------------
        #endregion
    }
}
