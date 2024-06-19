using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using FDK;

namespace TJAPlayer3
{
	internal class CActImplScore : CAct演奏スコア共通
	{
		// CActivity 実装（共通クラスからの差分のみ）

		public unsafe override int Draw()
        {
            if (!base.IsDeActivated)
            {
                if (base.IsFirstDraw)
                {
                    base.IsFirstDraw = false;
                }
                long num = FDK.SoundManager.PlayTimer.NowTime;


                if( !this.ctTimer.IsStoped )
                {
                    this.ctTimer.Tick();
                    if( this.ctTimer.IsEnded )
                    {
                        this.ctTimer.Stop();
                    }

                    //base.t小文字表示( 20, 150, string.Format( "{0,7:######0}", this.nスコアの増分.Guitar ) );
                }

                for (int i = 0; i < 5; i++)
                {
                    if (!this.ct点数アニメタイマ[i].IsStoped)
                    {
                        this.ct点数アニメタイマ[i].Tick();
                        if (this.ct点数アニメタイマ[i].IsEnded)
                        {
                            this.ct点数アニメタイマ[i].Stop();
                        }
                    }
                }

                for (int i = 0; i < 5; i++)
                {
                    if (!this.ctボーナス加算タイマ[i].IsStoped)
                    {
                        this.ctボーナス加算タイマ[i].Tick();
                        if (this.ctボーナス加算タイマ[i].IsEnded)
                        {
                            TJAPlayer3.stage演奏ドラム画面.actScore.BonusAdd(i);
                            this.ctボーナス加算タイマ[i].Stop();
                        }
                    }
                }

                int[] x = new int[5];
                int[] y = new int[5];
                int[] add_x = new int[5];
                int[] add_y = new int[5];
                int[] addBonus_x = new int[5];
                int[] addBonus_y = new int[5];

                for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                {
                    if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
                    {
                        x[i] = TJAPlayer3.Skin.Game_Score_5P[0] + (TJAPlayer3.Skin.Game_UIMove_5P[0] * i);
                        y[i] = TJAPlayer3.Skin.Game_Score_5P[1] + (TJAPlayer3.Skin.Game_UIMove_5P[1] * i);
                        add_x[i] = TJAPlayer3.Skin.Game_Score_Add_5P[0] + (TJAPlayer3.Skin.Game_UIMove_5P[0] * i);
                        add_y[i] = TJAPlayer3.Skin.Game_Score_Add_5P[1] + (TJAPlayer3.Skin.Game_UIMove_5P[1] * i);
                        addBonus_x[i] = TJAPlayer3.Skin.Game_Score_AddBonus_5P[0] + (TJAPlayer3.Skin.Game_UIMove_5P[0] * i);
                        addBonus_y[i] = TJAPlayer3.Skin.Game_Score_AddBonus_5P[1] + (TJAPlayer3.Skin.Game_UIMove_5P[1] * i);
                    }
                    else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3)
                    {
                        x[i] = TJAPlayer3.Skin.Game_Score_4P[0] + (TJAPlayer3.Skin.Game_UIMove_4P[0] * i);
                        y[i] = TJAPlayer3.Skin.Game_Score_4P[1] + (TJAPlayer3.Skin.Game_UIMove_4P[1] * i);
                        add_x[i] = TJAPlayer3.Skin.Game_Score_Add_4P[0] + (TJAPlayer3.Skin.Game_UIMove_4P[0] * i);
                        add_y[i] = TJAPlayer3.Skin.Game_Score_Add_4P[1] + (TJAPlayer3.Skin.Game_UIMove_4P[1] * i);
                        addBonus_x[i] = TJAPlayer3.Skin.Game_Score_AddBonus_4P[0] + (TJAPlayer3.Skin.Game_UIMove_4P[0] * i);
                        addBonus_y[i] = TJAPlayer3.Skin.Game_Score_AddBonus_4P[1] + (TJAPlayer3.Skin.Game_UIMove_4P[1] * i);
                    }
                    else
                    {
                        x[i] = TJAPlayer3.Skin.Game_Score_X[i];
                        y[i] = TJAPlayer3.Skin.Game_Score_Y[i];
                        add_x[i] = TJAPlayer3.Skin.Game_Score_Add_X[i];
                        add_y[i] = TJAPlayer3.Skin.Game_Score_Add_Y[i];
                        addBonus_x[i] = TJAPlayer3.Skin.Game_Score_AddBonus_X[i];
                        addBonus_y[i] = TJAPlayer3.Skin.Game_Score_AddBonus_Y[i];
                    }
                }

                //CDTXMania.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.白, this.ctボーナス加算タイマ[0].n現在の値.ToString());

                for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                {
                    if (i == 1 && TJAPlayer3.ConfigIni.bAIBattleMode) break;

                    base.t小文字表示(x[i], y[i], string.Format("{0,7:######0}", this.nCurrentlyDisplayedScore[i]), 0, 256, i);
                }

                for( int i = 0; i < 256; i++ )
                {
                    if( this.stScore[ i ].b使用中 )
                    {
                        if( !this.stScore[ i ].ctTimer.IsStoped )
                        {
                            this.stScore[ i ].ctTimer.Tick();
                            if( this.stScore[ i ].ctTimer.IsEnded )
                            {
                                if( this.stScore[ i ].b表示中 == true )
                                    this.nNowDisplayedAddScore--;
                                this.stScore[ i ].ctTimer.Stop();
                                this.stScore[ i ].b使用中 = false;
                                TJAPlayer3.stage演奏ドラム画面.actDan.Update();
                            }

                            if (!stScore[i].bAddEnd)
                            {
                                this.nCurrentlyDisplayedScore[this.stScore[i].nPlayer] += (long)this.stScore[i].nAddScore;
                                stScore[i].bAddEnd = true;
                                if (ct点数アニメタイマ[stScore[i].nPlayer].IsUnEnded)
                                {
                                    this.ct点数アニメタイマ[stScore[i].nPlayer] = new CCounter(0, 11, 13, TJAPlayer3.Timer);
                                    this.ct点数アニメタイマ[stScore[i].nPlayer].CurrentValue = 1;
                                }
                                else
                                {
                                    this.ct点数アニメタイマ[stScore[i].nPlayer] = new CCounter(0, 11, 13, TJAPlayer3.Timer);
                                }
                            }

                            int xAdd = 0;
                            int yAdd = 0;
                            int alpha = 0;

                            if ( this.stScore[i].ctTimer.CurrentValue < 10)
                            {
                                xAdd = 25;
                                alpha = 150;
                            } else if (this.stScore[i].ctTimer.CurrentValue < 20)
                            {
                                xAdd = 10;
                                alpha = 200;
                            } else if (this.stScore[i].ctTimer.CurrentValue < 30)
                            {
                                xAdd = -5;
                                alpha = 250;
                            } else if (this.stScore[i].ctTimer.CurrentValue < 40)
                            {
                                xAdd = -9;
                                alpha = 256;
                            } else if (this.stScore[i].ctTimer.CurrentValue < 50)
                            {
                                xAdd = -10;
                                alpha = 256;
                            } else if (this.stScore[i].ctTimer.CurrentValue < 60)
                            {
                                xAdd = -9;
                                alpha = 256;
                            } else if (this.stScore[i].ctTimer.CurrentValue < 70)
                            {
                                xAdd = -5;
                                alpha = 256;
                            } else if (this.stScore[i].ctTimer.CurrentValue < 80)
                            {
                                xAdd = -3;
                                alpha = 256;
                            } else
                            {
                                xAdd = 0;
                                alpha = 256;
                            }



                            if ( this.stScore[ i ].ctTimer.CurrentValue > 120 )
                            {
                                yAdd = -1;
                            }
                            if (this.stScore[i].ctTimer.CurrentValue > 130)
                            {
                                yAdd = -5;
                            }
                            if (this.stScore[i].ctTimer.CurrentValue > 140)
                            {
                                yAdd = -7;
                            }
                            if (this.stScore[i].ctTimer.CurrentValue > 150)
                            {
                                yAdd = -8;
                            }
                            if (this.stScore[i].ctTimer.CurrentValue > 160)
                            {
                                yAdd = -8;
                                alpha = 256;
                            }
                            if (this.stScore[i].ctTimer.CurrentValue > 170)
                            {
                                yAdd = -6;
                                alpha = 256;
                            }
                            if (this.stScore[i].ctTimer.CurrentValue > 180)
                            {
                                yAdd = 0;
                                alpha = 256;
                            }
                            if (this.stScore[i].ctTimer.CurrentValue > 190)
                            {
                                yAdd = 5;
                                alpha = 200;
                            }
                            if (this.stScore[i].ctTimer.CurrentValue > 200)
                            {
                                yAdd = 12;
                                alpha = 150;
                            }
                            if (this.stScore[i].ctTimer.CurrentValue > 210)
                            {
                                yAdd = 20;
                                alpha = 0;
                            }

                            int pl = stScore[i].nPlayer;
                            if (TJAPlayer3.PlayerSide == 1 && TJAPlayer3.ConfigIni.nPlayerCount == 1)
                                pl = 1;

                            if ( this.nNowDisplayedAddScore < 10 && this.stScore[ i ].bBonusScore == false && !TJAPlayer3.ConfigIni.SimpleMode)
                                base.t小文字表示(add_x[this.stScore[i].nPlayer] + xAdd, this.stScore[ i ].nPlayer == 0 && TJAPlayer3.ConfigIni.nPlayerCount <= 2 ? add_y[ this.stScore[ i ].nPlayer ] + yAdd : add_y[ this.stScore[ i ].nPlayer ] - yAdd, string.Format( "{0,7:######0}", this.stScore[ i ].nAddScore ), pl + 1 , alpha, stScore[i].nPlayer);
                            if( this.nNowDisplayedAddScore < 10 && this.stScore[ i ].bBonusScore == true && !TJAPlayer3.ConfigIni.SimpleMode)
                                base.t小文字表示(addBonus_x[this.stScore[i].nPlayer] + xAdd, addBonus_y[ this.stScore[ i ].nPlayer ], string.Format( "{0,7:######0}", this.stScore[ i ].nAddScore ), pl + 1 , alpha, stScore[i].nPlayer);
                            else
                            {
                                this.nNowDisplayedAddScore--;
                                this.stScore[ i ].b表示中 = false;
                            }
                        }
                    }
                    //CDTXMania.act文字コンソール.tPrint(50, 0, C文字コンソール.Eフォント種別.白, this.ct点数アニメタイマ[0].n現在の値.ToString());
                    //CDTXMania.act文字コンソール.tPrint(50, 20, C文字コンソール.Eフォント種別.白, this.ct点数アニメタイマ[0].b進行中.ToString());
                }


            }
            return 0;
        }
	}
}
