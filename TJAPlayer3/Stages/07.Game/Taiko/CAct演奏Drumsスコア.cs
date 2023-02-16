using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using FDK;

namespace TJAPlayer3
{
	internal class CAct演奏Drumsスコア : CAct演奏スコア共通
	{
		// CActivity 実装（共通クラスからの差分のみ）

		public unsafe override int On進行描画()
        {
            if (!base.b活性化してない)
            {
                if (base.b初めての進行描画)
                {
                    base.b初めての進行描画 = false;
                }
                long num = FDK.CSound管理.rc演奏用タイマ.n現在時刻;


                if( !this.ctTimer.b停止中 )
                {
                    this.ctTimer.t進行();
                    if( this.ctTimer.b終了値に達した )
                    {
                        this.ctTimer.t停止();
                    }

                    //base.t小文字表示( 20, 150, string.Format( "{0,7:######0}", this.nスコアの増分.Guitar ) );
                }

                for (int i = 0; i < 5; i++)
                {
                    if (!this.ct点数アニメタイマ[i].b停止中)
                    {
                        this.ct点数アニメタイマ[i].t進行();
                        if (this.ct点数アニメタイマ[i].b終了値に達した)
                        {
                            this.ct点数アニメタイマ[i].t停止();
                        }
                    }
                }

                for (int i = 0; i < 5; i++)
                {
                    if (!this.ctボーナス加算タイマ[i].b停止中)
                    {
                        this.ctボーナス加算タイマ[i].t進行();
                        if (this.ctボーナス加算タイマ[i].b終了値に達した)
                        {
                            TJAPlayer3.stage演奏ドラム画面.actScore.BonusAdd(i);
                            this.ctボーナス加算タイマ[i].t停止();
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

                    base.t小文字表示(x[i], y[i], string.Format("{0,7:######0}", this.n現在表示中のスコア[i].Taiko), 0, 256, i);
                }

                for( int i = 0; i < 256; i++ )
                {
                    if( this.stScore[ i ].b使用中 )
                    {
                        if( !this.stScore[ i ].ctTimer.b停止中 )
                        {
                            this.stScore[ i ].ctTimer.t進行();
                            if( this.stScore[ i ].ctTimer.b終了値に達した )
                            {
                                if( this.stScore[ i ].b表示中 == true )
                                    this.n現在表示中のAddScore--;
                                this.stScore[ i ].ctTimer.t停止();
                                this.stScore[ i ].b使用中 = false;
                                TJAPlayer3.stage演奏ドラム画面.actDan.Update();
                            }

                            if (!stScore[i].bAddEnd)
                            {
                                this.n現在表示中のスコア[this.stScore[i].nPlayer].Taiko += (long)this.stScore[i].nAddScore;
                                stScore[i].bAddEnd = true;
                                if (ct点数アニメタイマ[stScore[i].nPlayer].b終了値に達してない)
                                {
                                    this.ct点数アニメタイマ[stScore[i].nPlayer] = new CCounter(0, 11, 13, TJAPlayer3.Timer);
                                    this.ct点数アニメタイマ[stScore[i].nPlayer].n現在の値 = 1;
                                }
                                else
                                {
                                    this.ct点数アニメタイマ[stScore[i].nPlayer] = new CCounter(0, 11, 13, TJAPlayer3.Timer);
                                }
                            }

                            int xAdd = 0;
                            int yAdd = 0;
                            int alpha = 0;

                            if ( this.stScore[i].ctTimer.n現在の値 < 10)
                            {
                                xAdd = 25;
                                alpha = 150;
                            } else if (this.stScore[i].ctTimer.n現在の値 < 20)
                            {
                                xAdd = 10;
                                alpha = 200;
                            } else if (this.stScore[i].ctTimer.n現在の値 < 30)
                            {
                                xAdd = -5;
                                alpha = 250;
                            } else if (this.stScore[i].ctTimer.n現在の値 < 40)
                            {
                                xAdd = -9;
                                alpha = 256;
                            } else if (this.stScore[i].ctTimer.n現在の値 < 50)
                            {
                                xAdd = -10;
                                alpha = 256;
                            } else if (this.stScore[i].ctTimer.n現在の値 < 60)
                            {
                                xAdd = -9;
                                alpha = 256;
                            } else if (this.stScore[i].ctTimer.n現在の値 < 70)
                            {
                                xAdd = -5;
                                alpha = 256;
                            } else if (this.stScore[i].ctTimer.n現在の値 < 80)
                            {
                                xAdd = -3;
                                alpha = 256;
                            } else
                            {
                                xAdd = 0;
                                alpha = 256;
                            }



                            if ( this.stScore[ i ].ctTimer.n現在の値 > 120 )
                            {
                                yAdd = -1;
                            }
                            if (this.stScore[i].ctTimer.n現在の値 > 130)
                            {
                                yAdd = -5;
                            }
                            if (this.stScore[i].ctTimer.n現在の値 > 140)
                            {
                                yAdd = -7;
                            }
                            if (this.stScore[i].ctTimer.n現在の値 > 150)
                            {
                                yAdd = -8;
                            }
                            if (this.stScore[i].ctTimer.n現在の値 > 160)
                            {
                                yAdd = -8;
                                alpha = 256;
                            }
                            if (this.stScore[i].ctTimer.n現在の値 > 170)
                            {
                                yAdd = -6;
                                alpha = 256;
                            }
                            if (this.stScore[i].ctTimer.n現在の値 > 180)
                            {
                                yAdd = 0;
                                alpha = 256;
                            }
                            if (this.stScore[i].ctTimer.n現在の値 > 190)
                            {
                                yAdd = 5;
                                alpha = 200;
                            }
                            if (this.stScore[i].ctTimer.n現在の値 > 200)
                            {
                                yAdd = 12;
                                alpha = 150;
                            }
                            if (this.stScore[i].ctTimer.n現在の値 > 210)
                            {
                                yAdd = 20;
                                alpha = 0;
                            }

                            int pl = stScore[i].nPlayer;
                            if (TJAPlayer3.PlayerSide == 1 && TJAPlayer3.ConfigIni.nPlayerCount == 1)
                                pl = 1;

                            if ( this.n現在表示中のAddScore < 10 && this.stScore[ i ].bBonusScore == false )
                                base.t小文字表示(add_x[this.stScore[i].nPlayer] + xAdd, this.stScore[ i ].nPlayer == 0 && TJAPlayer3.ConfigIni.nPlayerCount <= 2 ? add_y[ this.stScore[ i ].nPlayer ] + yAdd : add_y[ this.stScore[ i ].nPlayer ] - yAdd, string.Format( "{0,7:######0}", this.stScore[ i ].nAddScore ), pl + 1 , alpha, stScore[i].nPlayer);
                            if( this.n現在表示中のAddScore < 10 && this.stScore[ i ].bBonusScore == true )
                                base.t小文字表示(addBonus_x[this.stScore[i].nPlayer] + xAdd, addBonus_y[ this.stScore[ i ].nPlayer ], string.Format( "{0,7:######0}", this.stScore[ i ].nAddScore ), pl + 1 , alpha, stScore[i].nPlayer);
                            else
                            {
                                this.n現在表示中のAddScore--;
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
