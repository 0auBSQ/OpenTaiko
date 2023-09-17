using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using FDK;

namespace TJAPlayer3
{
    internal class FlyingNotes : CActivity
    {
        // コンストラクタ

        public FlyingNotes()
        {
            base.IsDeActivated = true;
        }


        // メソッド
        public virtual void Start(int nLane, int nPlayer, bool isRoll = false)
        {
            if (TJAPlayer3.ConfigIni.nPlayerCount > 2) return;
            EGameType _gt = TJAPlayer3.ConfigIni.nGameType[TJAPlayer3.GetActualPlayer(nPlayer)];

            if (TJAPlayer3.Tx.Notes[(int)_gt] != null)
            {
                for (int i = 0; i < 128; i++)
                {
                    if (!Flying[i].IsUsing)
                    {
                        // 初期化
                        Flying[i].IsUsing = true;
                        Flying[i].Lane = nLane;
                        Flying[i].Player = nPlayer;
                        Flying[i].X = -100; //StartPointX[nPlayer];
                        Flying[i].Y = -100; //TJAPlayer3.Skin.Game_Effect_FlyingNotes_StartPoint_Y[nPlayer];
                        Flying[i].StartPointX = StartPointX[nPlayer];
                        Flying[i].StartPointY = TJAPlayer3.Skin.Game_Effect_FlyingNotes_StartPoint_Y[nPlayer];
                        Flying[i].OldValue = 0;
                        Flying[i].IsRoll = isRoll;
                        // 角度の決定
                        Flying[i].Height = Math.Abs(TJAPlayer3.Skin.Game_Effect_FlyingNotes_EndPoint_Y[nPlayer] - TJAPlayer3.Skin.Game_Effect_FlyingNotes_StartPoint_Y[nPlayer]);
                        Flying[i].Width = (Math.Abs((TJAPlayer3.Skin.Game_Effect_FlyingNotes_EndPoint_X[nPlayer] - StartPointX[nPlayer])) / 2);
                        //Console.WriteLine("{0}, {1}", width2P, height2P);
                        Flying[i].Theta = ((Math.Atan2(Flying[i].Height, Flying[i].Width) * 180.0) / Math.PI);
                        Flying[i].Counter = new CCounter(0, 140, TJAPlayer3.Skin.Game_Effect_FlyingNotes_Timer, TJAPlayer3.Timer);
                        //Flying[i].Counter = new CCounter(0, 200000, CDTXMania.Skin.Game_Effect_FlyingNotes_Timer, CDTXMania.Timer);

                        Flying[i].IncreaseX = (1.00 * Math.Abs((TJAPlayer3.Skin.Game_Effect_FlyingNotes_EndPoint_X[nPlayer] - StartPointX[nPlayer]))) / (180);
                        Flying[i].IncreaseY = (1.00 * Math.Abs((TJAPlayer3.Skin.Game_Effect_FlyingNotes_EndPoint_Y[nPlayer] - TJAPlayer3.Skin.Game_Effect_FlyingNotes_StartPoint_Y[nPlayer]))) / (180);
                        break;
                    }
                }
            }
        }

        // CActivity 実装

        public override void Activate()
        {
            for (int i = 0; i < 128; i++)
            {
                Flying[i] = new Status();
                Flying[i].IsUsing = false;
                Flying[i].Counter = new CCounter();
            }
            for (int i = 0; i < 2; i++)
            {
                StartPointX[i] = TJAPlayer3.Skin.Game_Effect_FlyingNotes_StartPoint_X[i];
            }
            base.Activate();
        }
        public override void DeActivate()
        {
            for (int i = 0; i < 128; i++)
            {
                Flying[i].Counter = null;
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
            if (!base.IsDeActivated)
            {
                for (int i = 0; i < 128; i++)
                {
                    if (Flying[i].IsUsing)
                    {
                        Flying[i].OldValue = Flying[i].Counter.CurrentValue;
                        Flying[i].Counter.Tick();
                        if (Flying[i].Counter.IsEnded)
                        {
                            Flying[i].Counter.Stop();
                            Flying[i].IsUsing = false;
                            TJAPlayer3.stage演奏ドラム画面.actGauge.Start(Flying[i].Lane, E判定.Perfect, Flying[i].Player);
                            TJAPlayer3.stage演奏ドラム画面.actChipEffects.Start(Flying[i].Player, Flying[i].Lane);
                        }
                        for (int n = Flying[i].OldValue; n < Flying[i].Counter.CurrentValue; n += 16)
                        {
                            int endX;
                            int endY;

                            if (TJAPlayer3.ConfigIni.bAIBattleMode)
                            {
                                endX = TJAPlayer3.Skin.Game_Effect_FlyingNotes_EndPoint_X_AI[Flying[i].Player];
                                endY = TJAPlayer3.Skin.Game_Effect_FlyingNotes_EndPoint_Y_AI[Flying[i].Player];
                            }
                            else
                            {
                                endX = TJAPlayer3.Skin.Game_Effect_FlyingNotes_EndPoint_X[Flying[i].Player];
                                endY = TJAPlayer3.Skin.Game_Effect_FlyingNotes_EndPoint_Y[Flying[i].Player];
                            }

                            int movingDistanceX = endX - StartPointX[Flying[i].Player];
                            int movingDistanceY = endY - TJAPlayer3.Skin.Game_Effect_FlyingNotes_StartPoint_Y[Flying[i].Player];

                            /*
                            if (TJAPlayer3.Skin.Game_Effect_FlyingNotes_IsUsingEasing)
                            {
                                Flying[i].X = (Flying[i].StartPointX + movingDistanceX + ((-Math.Cos(Flying[i].Counter.n現在の値 * (Math.PI / 180)) * movingDistanceX))) - 85;
                                //Flying[i].X += (Math.Cos(Flying[i].Counter.n現在の値 * (Math.PI / 180))) * Flying[i].Increase;
                            }
                            else
                            {
                                Flying[i].X += Flying[i].IncreaseX;
                            }
                            */

                            double value = (Flying[i].Counter.CurrentValue / 140.0);

                            Flying[i].X = StartPointX[Flying[i].Player] + TJAPlayer3.stage演奏ドラム画面.GetJPOSCROLLX(Flying[i].Player) + (movingDistanceX * value);
                            Flying[i].Y = TJAPlayer3.Skin.Game_Effect_FlyingNotes_StartPoint_Y[Flying[i].Player] + TJAPlayer3.stage演奏ドラム画面.GetJPOSCROLLY(Flying[i].Player) + (int)(movingDistanceY * value);
                            
                            if (TJAPlayer3.ConfigIni.bAIBattleMode)
                            {
                                Flying[i].Y += Math.Sin(value * Math.PI) * ((Flying[i].Player == 0 ? -TJAPlayer3.Skin.Game_Effect_FlyingNotes_Sine : TJAPlayer3.Skin.Game_Effect_FlyingNotes_Sine) / 3.0);
                            }
                            else
                            {
                                Flying[i].Y += Math.Sin(value * Math.PI) * (Flying[i].Player == 0 ? -TJAPlayer3.Skin.Game_Effect_FlyingNotes_Sine : TJAPlayer3.Skin.Game_Effect_FlyingNotes_Sine);
                            }

                            if (TJAPlayer3.Skin.Game_Effect_FlyingNotes_IsUsingEasing)
                            {
                            }
                            else
                            {
                            }

                            if (n % TJAPlayer3.Skin.Game_Effect_FireWorks_Timing == 0 && !Flying[i].IsRoll && Flying[i].Counter.CurrentValue > 18)
                            {
                                if (Flying[i].Lane == 3 || Flying[i].Lane == 4)
                                {
                                    TJAPlayer3.stage演奏ドラム画面.FireWorks.Start(Flying[i].Lane, Flying[i].Player, Flying[i].X, Flying[i].Y);
                                }
                            }

                            /*
                            if (Flying[i].Player == 0)
                            {
                                Flying[i].Y = ((TJAPlayer3.Skin.Game_Effect_FlyingNotes_StartPoint_Y[Flying[i].Player]) + -Math.Sin(Flying[i].Counter.n現在の値 * (Math.PI / 180)) * 559) + 329;
                                Flying[i].Y -= Flying[i].IncreaseY * Flying[i].Counter.n現在の値;
                            }
                            else
                            {
                                Flying[i].Y = ((TJAPlayer3.Skin.Game_Effect_FlyingNotes_StartPoint_Y[Flying[i].Player]) + Math.Sin(Flying[i].Counter.n現在の値 * (Math.PI / 180)) * 559) - 329;
                                Flying[i].Y += Flying[i].IncreaseY * Flying[i].Counter.n現在の値;
                            }
                            */
                        }
                        //Flying[i].OldValue = Flying[i].Counter.n現在の値;

                        NotesManager.DisplayNote(Flying[i].Player, (int)Flying[i].X, (int)Flying[i].Y, Flying[i].Lane);

                        /*
                        EGameType _gt = TJAPlayer3.ConfigIni.nGameType[TJAPlayer3.GetActualPlayer(Flying[i].Player)];

                        TJAPlayer3.Tx.Notes[(int)_gt]?.t2D中心基準描画((int)Flying[i].X, (int)Flying[i].Y, new Rectangle(Flying[i].Lane * 130, 0, 130, 130));
                        */

                        /*
                        if (Flying[i].Player == 0)
                        {
                            
                        }
                        else if (Flying[i].Player == 1)
                        {
                            //
                            TJAPlayer3.Tx.Notes?.t2D中心基準描画((int)Flying[i].X, (int)Flying[i].Y, new Rectangle(Flying[i].Lane * 130, 0, 130, 130));
                        }
                        */
                    }
                }
            }
            return base.Draw();
        }


        #region [ private ]
        //-----------------

        [StructLayout(LayoutKind.Sequential)]
        private struct Status
        {
            public int Lane;
            public int Player;
            public bool IsUsing;
            public CCounter Counter;
            public int OldValue;
            public double X;
            public double Y;
            public int Height;
            public int Width;
            public double IncreaseX;
            public double IncreaseY;
            public bool IsRoll;
            public int StartPointX;
            public int StartPointY;
            public double Theta;
        }

        private Status[] Flying = new Status[128];

        public readonly int[] StartPointX = new int[2];

        //-----------------
        #endregion
    }
}