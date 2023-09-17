using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using FDK;

namespace TJAPlayer3
{
    internal class CAct演奏Drumsレーン : CActivity
    {
        public CAct演奏Drumsレーン()
        {
            base.IsDeActivated = true;
        }

        public override void Activate()
        {
            this.ct分岐アニメ進行 = new CCounter[ 5 ];
            this.nBefore = new CDTX.ECourse[ 5 ];
            this.nAfter = new CDTX.ECourse[ 5 ];
            for ( int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++ )
            {
                this.ct分岐アニメ進行[ i ] = new CCounter();
                this.nBefore = new CDTX.ECourse[ 5 ];
                this.nAfter = new CDTX.ECourse[ 5 ];
                this.bState[ i ] = false;
            }
            if (TJAPlayer3.Tx.Lane_Base[0] != null)
                TJAPlayer3.Tx.Lane_Base[0].Opacity = 255;

            base.Activate();
        }

        public override void DeActivate()
        {
            TJAPlayer3.t安全にDisposeする( ref this.ct分岐アニメ進行 );
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
            for( int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++ )
            {
                if( !this.ct分岐アニメ進行[ i ].IsStoped )
                {
                    this.ct分岐アニメ進行[ i ].Tick();
                    if( this.ct分岐アニメ進行[ i ].IsEnded )
                    {
                        this.bState[ i ] = false;
                        this.ct分岐アニメ進行[ i ].Stop();
                    }
                }
            }

            int[] x = new int[5];
            int[] y = new int[5];

            for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
                {
                    x[i] = TJAPlayer3.Skin.Game_Lane_5P[0] + (TJAPlayer3.Skin.Game_UIMove_5P[0] * i);
                    y[i] = TJAPlayer3.Skin.Game_Lane_5P[1] + (TJAPlayer3.Skin.Game_UIMove_5P[1] * i);
                }
                else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3)
                {
                    x[i] = TJAPlayer3.Skin.Game_Lane_4P[0] + (TJAPlayer3.Skin.Game_UIMove_4P[0] * i);
                    y[i] = TJAPlayer3.Skin.Game_Lane_4P[1] + (TJAPlayer3.Skin.Game_UIMove_4P[1] * i);
                }
                else
                {
                    x[i] = TJAPlayer3.Skin.Game_Lane_X[i];
                    y[i] = TJAPlayer3.Skin.Game_Lane_Y[i];
                }
            }

            //アニメーション中の分岐レイヤー(背景)の描画を行う。
            for ( int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++ )
            {
                if( TJAPlayer3.stage演奏ドラム画面.bUseBranch[ i ] == true )
                {

                    #region NullCheck

                    bool _laneNull = false;

                    for (int j = 0; j < TJAPlayer3.Tx.Lane_Base.Length; j++)
                    {
                        if (TJAPlayer3.Tx.Lane_Base[j] == null)
                        {
                            _laneNull = true;
                            break;
                        }
                    }

                    #endregion

                    if( this.ct分岐アニメ進行[ i ].IsTicked && !_laneNull)
                    {
                        #region[ 普通譜面_レベルアップ ]
                        //普通→玄人
                        if (nBefore[i] == 0 && nAfter[i] == CDTX.ECourse.eNormal)
                        {
                            TJAPlayer3.Tx.Lane_Base[1].Opacity = this.ct分岐アニメ進行[ i ].CurrentValue > 100 ? 255 : ( ( this.ct分岐アニメ進行[ i ].CurrentValue * 0xff ) / 100 );
                            TJAPlayer3.Tx.Lane_Base[0].t2D描画( x[ i ], y[ i ] );
                            TJAPlayer3.Tx.Lane_Base[1].t2D描画( x[ i ], y[ i ] );
                        }
                        //普通→達人
                        if (nBefore[i] == 0 && nAfter[i] == CDTX.ECourse.eMaster)
                        {
                            TJAPlayer3.Tx.Lane_Base[0].t2D描画( x[ i ], y[ i ] );
                            if( this.ct分岐アニメ進行[ i ].CurrentValue < 100 )
                            {
                                TJAPlayer3.Tx.Lane_Base[1].Opacity = this.ct分岐アニメ進行[ i ].CurrentValue > 100 ? 255 : ( ( this.ct分岐アニメ進行[ i ].CurrentValue * 0xff ) / 100 );
                                TJAPlayer3.Tx.Lane_Base[1].t2D描画( x[ i ], y[ i ] );
                            }
                            else if( this.ct分岐アニメ進行[ i ].CurrentValue >= 100 && this.ct分岐アニメ進行[ i ].CurrentValue < 150 )
                            {
                                TJAPlayer3.Tx.Lane_Base[1].Opacity = 255;
                                TJAPlayer3.Tx.Lane_Base[1].t2D描画( x[ i ], y[ i ] );
                            }
                            else if( this.ct分岐アニメ進行[ i ].CurrentValue >= 150 )
                            {
                                TJAPlayer3.Tx.Lane_Base[1].t2D描画( x[ i ], y[ i ] );
                                TJAPlayer3.Tx.Lane_Base[2].Opacity = this.ct分岐アニメ進行[ i ].CurrentValue > 250 ? 255 : ( ( (this.ct分岐アニメ進行[ i ].CurrentValue - 150) * 0xff ) / 100 );
                                TJAPlayer3.Tx.Lane_Base[2].t2D描画( x[ i ], y[ i ] );
                            }
                        }
                        #endregion

                        #region[ 玄人譜面_レベルアップ ]
                        if (nBefore[i] == CDTX.ECourse.eExpert && nAfter[i] == CDTX.ECourse.eMaster)
                        {
                            TJAPlayer3.Tx.Lane_Base[1].t2D描画( x[ i ], y[ i ] );
                            TJAPlayer3.Tx.Lane_Base[2].Opacity = this.ct分岐アニメ進行[ i ].CurrentValue > 100 ? 255 : ( ( this.ct分岐アニメ進行[ i ].CurrentValue * 0xff ) / 100 );
                            TJAPlayer3.Tx.Lane_Base[2].t2D描画( x[ i ], y[ i ] );
                        }
                        #endregion

                        #region[ 玄人譜面_レベルダウン ]
                        if (nBefore[i] == CDTX.ECourse.eExpert && nAfter[i] == CDTX.ECourse.eNormal)
                        {
                            TJAPlayer3.Tx.Lane_Base[1].t2D描画( x[ i ], y[ i ] );
                            TJAPlayer3.Tx.Lane_Base[0].Opacity = this.ct分岐アニメ進行[ i ].CurrentValue > 100 ? 255 : ( ( this.ct分岐アニメ進行[ i ].CurrentValue * 0xff ) / 100 );
                            TJAPlayer3.Tx.Lane_Base[0].t2D描画( x[ i ], y[ i ] );
                        }
                        #endregion

                        #region[ 達人譜面_レベルダウン ]
                        if (nBefore[i] == CDTX.ECourse.eMaster && nAfter[i] == CDTX.ECourse.eNormal)
                        {
                            TJAPlayer3.Tx.Lane_Base[2].t2D描画( x[ i ], y[ i ] );
                            if( this.ct分岐アニメ進行[ i ].CurrentValue < 100 )
                            {
                                TJAPlayer3.Tx.Lane_Base[1].Opacity = this.ct分岐アニメ進行[ i ].CurrentValue > 100 ? 255 : ( ( this.ct分岐アニメ進行[ i ].CurrentValue * 0xff ) / 100 );
                                TJAPlayer3.Tx.Lane_Base[1].t2D描画( x[ i ], y[ i ] );
                            }
                            else if( this.ct分岐アニメ進行[ i ].CurrentValue >= 100 && this.ct分岐アニメ進行[ i ].CurrentValue < 150 )
                            {
                                TJAPlayer3.Tx.Lane_Base[1].Opacity = 255;
                                TJAPlayer3.Tx.Lane_Base[1].t2D描画( x[ i ], y[ i ] );
                            }
                            else if( this.ct分岐アニメ進行[ i ].CurrentValue >= 150 )
                            {
                                TJAPlayer3.Tx.Lane_Base[1].t2D描画( x[ i ], y[ i ] );
                                TJAPlayer3.Tx.Lane_Base[0].Opacity = this.ct分岐アニメ進行[ i ].CurrentValue > 250 ? 255 : ( ( ( this.ct分岐アニメ進行[ i ].CurrentValue - 150 ) * 0xff ) / 100 );
                                TJAPlayer3.Tx.Lane_Base[0].t2D描画( x[ i ], y[ i ] );
                            }
                        }
                        if (nBefore[i] == CDTX.ECourse.eMaster && nAfter[i] == CDTX.ECourse.eExpert)
                        {
                            TJAPlayer3.Tx.Lane_Base[2].t2D描画( x[ i ], y[ i ] );
                            TJAPlayer3.Tx.Lane_Base[2].Opacity = this.ct分岐アニメ進行[ i ].CurrentValue > 100 ? 255 : ( ( this.ct分岐アニメ進行[ i ].CurrentValue * 0xff ) / 100 );
                            TJAPlayer3.Tx.Lane_Base[2].t2D描画( x[ i ], y[ i ] );
                        }
                        #endregion
                    }
                }
            }
            return base.Draw();
        }

        public virtual void t分岐レイヤー_コース変化(CDTX.ECourse n現在, CDTX.ECourse n次回, int player)
        {
            if ( n現在 == n次回 ) {
                return;
            }
            this.ct分岐アニメ進行[ player ] = new CCounter( 0, 300, 2, TJAPlayer3.Timer );
            this.bState[ player ] = true;

            this.nBefore[ player ] = n現在;
            this.nAfter[ player ] = n次回;

        }

        #region[ private ]
        //-----------------
        public bool[] bState = new bool[5];
        public CCounter[] ct分岐アニメ進行 = new CCounter[5];
        private CDTX.ECourse[] nBefore;
        private CDTX.ECourse[] nAfter;
        private int[] n透明度 = new int[5];
        //private CTexture[] tx普通譜面 = new CTexture[2];
        //private CTexture[] tx玄人譜面 = new CTexture[2];
        //private CTexture[] tx達人譜面 = new CTexture[2];
        //-----------------
        #endregion
    }
}
