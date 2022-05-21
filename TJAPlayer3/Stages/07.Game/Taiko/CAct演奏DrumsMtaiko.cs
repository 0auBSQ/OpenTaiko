﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using FDK;
using SharpDX;

using Rectangle = System.Drawing.Rectangle;

namespace TJAPlayer3
{
    internal class CAct演奏DrumsMtaiko : CActivity
    {
        /// <summary>
        /// mtaiko部分を描画するクラス。左側だけ。
        /// 
        /// </summary>
        public CAct演奏DrumsMtaiko()
        {
            base.b活性化してない = true;
        }

        public override void On活性化()
        {
			for( int i = 0; i < 16; i++ )
			{
				STパッド状態 stパッド状態 = new STパッド状態();
				stパッド状態.n明るさ = 0;
				this.stパッド状態[ i ] = stパッド状態;
			}
            base.On活性化();
        }

        public override void On非活性化()
        {
            base.On非活性化();
        }

        public override void OnManagedリソースの作成()
        {
             this.ctレベルアップダウン = new CCounter[ 4 ];
            this.After = new CDTX.ECourse[ 4 ];
            this.Before = new CDTX.ECourse[ 4 ];
            for ( int i = 0; i < 4; i++ )
            {
                this.ctレベルアップダウン[ i ] = new CCounter();
            }


            base.OnManagedリソースの作成();
        }

        public override void OnManagedリソースの解放()
        {
            this.ctレベルアップダウン = null;

            base.OnManagedリソースの解放();
        }

        public override int On進行描画()
        {
            if( base.b初めての進行描画 )
			{
                this.nフラッシュ制御タイマ = (long)(CSound管理.rc演奏用タイマ.n現在時刻 * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0));
                base.b初めての進行描画 = false;
            }

            long num = (long)(CSound管理.rc演奏用タイマ.n現在時刻 * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0));
            if ( num < this.nフラッシュ制御タイマ )
			{
				this.nフラッシュ制御タイマ = num;
			}
			while( ( num - this.nフラッシュ制御タイマ ) >= 20 )
			{
				for( int j = 0; j < 16; j++ )
				{
					if( this.stパッド状態[ j ].n明るさ > 0 )
					{
						this.stパッド状態[ j ].n明るさ--;
					}
				}
				this.nフラッシュ制御タイマ += 20;
		    }


            //this.nHS = TJAPlayer3.ConfigIni.nScrollSpeed.Drums < 8 ? TJAPlayer3.ConfigIni.nScrollSpeed.Drums : 7;

            if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan)  // Dan-i Dojo
                TJAPlayer3.Tx.Taiko_Background[2]?.t2D描画(TJAPlayer3.app.Device, 0, 184);
            else if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower) // Taiko Towers 
                TJAPlayer3.Tx.Taiko_Background[3]?.t2D描画(TJAPlayer3.app.Device, 0, 184);
            else if (!TJAPlayer3.ConfigIni.bTokkunMode 
	                || TJAPlayer3.Tx.Taiko_Background[5] == null 
	                || TJAPlayer3.Tx.Taiko_Background[6] == null)
            {
                // Taiko Mode
                if (TJAPlayer3.stage演奏ドラム画面.bDoublePlay)
                {
                    // 2P
                    if (TJAPlayer3.ConfigIni.nAILevel == 0 || TJAPlayer3.Tx.Taiko_Background[9] == null)
                        TJAPlayer3.Tx.Taiko_Background[1]?.t2D描画(TJAPlayer3.app.Device, 0, 360);
                    else
                        TJAPlayer3.Tx.Taiko_Background[9]?.t2D描画(TJAPlayer3.app.Device, 0, 360);
                }
                if (TJAPlayer3.P1IsBlue())
                     TJAPlayer3.Tx.Taiko_Background[4]?.t2D描画(TJAPlayer3.app.Device, 0, 184);
                else
                    TJAPlayer3.Tx.Taiko_Background[0]?.t2D描画(TJAPlayer3.app.Device, 0, 184);
            }
            else 
            {
                // Training Mode
	            if (TJAPlayer3.P1IsBlue())
                    TJAPlayer3.Tx.Taiko_Background[6]?.t2D描画(TJAPlayer3.app.Device, 0, 184);
                else
                    TJAPlayer3.Tx.Taiko_Background[5]?.t2D描画(TJAPlayer3.app.Device, 0, 184);
            }

            for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                int _actual = TJAPlayer3.GetActualPlayer(i);
                EGameType _gt = TJAPlayer3.ConfigIni.nGameType[_actual];
                int playerShift = i * 5;

                // Drum base
                TJAPlayer3.Tx.Taiko_Base[(int)_gt]?.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Taiko_X[i], TJAPlayer3.Skin.Game_Taiko_Y[i]);

                // Taiko hits
                if (_gt == EGameType.TAIKO)
                {
                    if (TJAPlayer3.Tx.Taiko_Don_Left != null && TJAPlayer3.Tx.Taiko_Don_Right != null && TJAPlayer3.Tx.Taiko_Ka_Left != null && TJAPlayer3.Tx.Taiko_Ka_Right != null)
                    {
                        TJAPlayer3.Tx.Taiko_Ka_Left.Opacity = this.stパッド状態[playerShift].n明るさ * 73;
                        TJAPlayer3.Tx.Taiko_Ka_Right.Opacity = this.stパッド状態[1 + playerShift].n明るさ * 73;
                        TJAPlayer3.Tx.Taiko_Don_Left.Opacity = this.stパッド状態[2 + playerShift].n明るさ * 73;
                        TJAPlayer3.Tx.Taiko_Don_Right.Opacity = this.stパッド状態[3 + playerShift].n明るさ * 73;

                        TJAPlayer3.Tx.Taiko_Ka_Left.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Taiko_X[i], TJAPlayer3.Skin.Game_Taiko_Y[i], new Rectangle(0, 0, TJAPlayer3.Tx.Taiko_Ka_Right.szテクスチャサイズ.Width / 2, TJAPlayer3.Tx.Taiko_Ka_Right.szテクスチャサイズ.Height));
                        TJAPlayer3.Tx.Taiko_Ka_Right.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Taiko_X[i] + TJAPlayer3.Tx.Taiko_Ka_Right.szテクスチャサイズ.Width / 2, TJAPlayer3.Skin.Game_Taiko_Y[i], new Rectangle(TJAPlayer3.Tx.Taiko_Ka_Right.szテクスチャサイズ.Width / 2, 0, TJAPlayer3.Tx.Taiko_Ka_Right.szテクスチャサイズ.Width / 2, TJAPlayer3.Tx.Taiko_Ka_Right.szテクスチャサイズ.Height));
                        TJAPlayer3.Tx.Taiko_Don_Left.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Taiko_X[i], TJAPlayer3.Skin.Game_Taiko_Y[i], new Rectangle(0, 0, TJAPlayer3.Tx.Taiko_Ka_Right.szテクスチャサイズ.Width / 2, TJAPlayer3.Tx.Taiko_Ka_Right.szテクスチャサイズ.Height));
                        TJAPlayer3.Tx.Taiko_Don_Right.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Taiko_X[i] + TJAPlayer3.Tx.Taiko_Ka_Right.szテクスチャサイズ.Width / 2, TJAPlayer3.Skin.Game_Taiko_Y[i], new Rectangle(TJAPlayer3.Tx.Taiko_Ka_Right.szテクスチャサイズ.Width / 2, 0, TJAPlayer3.Tx.Taiko_Ka_Right.szテクスチャサイズ.Width / 2, TJAPlayer3.Tx.Taiko_Ka_Right.szテクスチャサイズ.Height));
                    }
                }
                else if (_gt == EGameType.KONGA)
                {
                    if (TJAPlayer3.Tx.Taiko_Konga_Clap != null && TJAPlayer3.Tx.Taiko_Konga_Don != null && TJAPlayer3.Tx.Taiko_Konga_Ka != null)
                    {
                        TJAPlayer3.Tx.Taiko_Konga_Clap.Opacity = this.stパッド状態[4 + playerShift].n明るさ * 73;
                        TJAPlayer3.Tx.Taiko_Konga_Don.Opacity = Math.Max(this.stパッド状態[2 + playerShift].n明るさ, this.stパッド状態[3 + playerShift].n明るさ) * 73;
                        TJAPlayer3.Tx.Taiko_Konga_Ka.Opacity = Math.Max(this.stパッド状態[playerShift].n明るさ, this.stパッド状態[1 + playerShift].n明るさ) * 73;

                        TJAPlayer3.Tx.Taiko_Konga_Ka.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Taiko_X[i], TJAPlayer3.Skin.Game_Taiko_Y[i]);
                        TJAPlayer3.Tx.Taiko_Konga_Don.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Taiko_X[i], TJAPlayer3.Skin.Game_Taiko_Y[i]);
                        TJAPlayer3.Tx.Taiko_Konga_Clap.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Taiko_X[i], TJAPlayer3.Skin.Game_Taiko_Y[i]);
                    }
                }

            }


            int[] nLVUPY = new int[] { 127, 127, 0, 0 };

            for ( int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++ )
            {
                if( !this.ctレベルアップダウン[ i ].b停止中 )
                {
                    this.ctレベルアップダウン[ i ].t進行();
                    if( this.ctレベルアップダウン[ i ].b終了値に達した ) {
                        this.ctレベルアップダウン[ i ].t停止();
                    }
                }
                if( ( this.ctレベルアップダウン[ i ].b進行中 && ( TJAPlayer3.Tx.Taiko_LevelUp != null && TJAPlayer3.Tx.Taiko_LevelDown != null ) ) && !TJAPlayer3.ConfigIni.bNoInfo )
                {
                    //this.ctレベルアップダウン[ i ].n現在の値 = 110;

                    //2017.08.21 kairera0467 t3D描画に変更。
                    float fScale = 1.0f;
                    int nAlpha = 255;
                    float[] fY = new float[] { 206, -206, 0, 0 };
                    if( this.ctレベルアップダウン[ i ].n現在の値 >= 0 && this.ctレベルアップダウン[ i ].n現在の値 <= 20 )
                    {
                        nAlpha = 60;
                        fScale = 1.14f;
                    }
                    else if( this.ctレベルアップダウン[ i ].n現在の値 >= 21 && this.ctレベルアップダウン[ i ].n現在の値 <= 40 )
                    {
                        nAlpha = 60;
                        fScale = 1.19f;
                    }
                    else if( this.ctレベルアップダウン[ i ].n現在の値 >= 41 && this.ctレベルアップダウン[ i ].n現在の値 <= 60 )
                    {
                        nAlpha = 220;
                        fScale = 1.23f;
                    }
                    else if( this.ctレベルアップダウン[ i ].n現在の値 >= 61 && this.ctレベルアップダウン[ i ].n現在の値 <= 80 )
                    {
                        nAlpha = 230;
                        fScale = 1.19f;
                    }
                    else if( this.ctレベルアップダウン[ i ].n現在の値 >= 81 && this.ctレベルアップダウン[ i ].n現在の値 <= 100 )
                    {
                        nAlpha = 240;
                        fScale = 1.14f;
                    }
                    else if( this.ctレベルアップダウン[ i ].n現在の値 >= 101 && this.ctレベルアップダウン[ i ].n現在の値 <= 120 )
                    {
                        nAlpha = 255;
                        fScale = 1.04f;
                    }
                    else
                    {
                        nAlpha = 255;
                        fScale = 1.0f;
                    }

                    Matrix mat = Matrix.Identity;
                    mat *= Matrix.Scaling( fScale, fScale, 1.0f );
                    mat *= Matrix.Translation( -329, fY[ i ], 0 );
                    if( this.After[ i ] - this.Before[ i ] >= 0 )
                    {
                        //レベルアップ
                        TJAPlayer3.Tx.Taiko_LevelUp.Opacity = nAlpha;
                        TJAPlayer3.Tx.Taiko_LevelUp.t3D描画( TJAPlayer3.app.Device, mat );
                    }
                    else
                    {
                        TJAPlayer3.Tx.Taiko_LevelDown.Opacity = nAlpha;
                        TJAPlayer3.Tx.Taiko_LevelDown.t3D描画( TJAPlayer3.app.Device, mat );
                    }
                }
            }

            for( int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++ )
            {

                ModIcons.tDisplayMods(80, 236 + i * 190, i);

                if (TJAPlayer3.Tx.Couse_Symbol[TJAPlayer3.stage選曲.n確定された曲の難易度[i]] != null)
                {
                    TJAPlayer3.Tx.Couse_Symbol[TJAPlayer3.stage選曲.n確定された曲の難易度[i]].t2D描画(TJAPlayer3.app.Device,
                        TJAPlayer3.Skin.Game_CourseSymbol_X[i],
                        TJAPlayer3.Skin.Game_CourseSymbol_Y[i]
                        );
                }


                if (TJAPlayer3.ConfigIni.ShinuchiMode)
                {
                    if (TJAPlayer3.Tx.Couse_Symbol[(int)Difficulty.Total] != null)
                    {
                        TJAPlayer3.Tx.Couse_Symbol[(int)Difficulty.Total].t2D描画(TJAPlayer3.app.Device,
                            TJAPlayer3.Skin.Game_CourseSymbol_X[i],
                            TJAPlayer3.Skin.Game_CourseSymbol_Y[i]
                            );
                    }

                }
            }

            TJAPlayer3.NamePlate.tNamePlateDraw(TJAPlayer3.Skin.Game_Taiko_NamePlate_X[0], TJAPlayer3.Skin.Game_Taiko_NamePlate_Y[0], 0);

            if(TJAPlayer3.stage演奏ドラム画面.bDoublePlay)
            {
                TJAPlayer3.NamePlate.tNamePlateDraw(TJAPlayer3.Skin.Game_Taiko_NamePlate_X[1], TJAPlayer3.Skin.Game_Taiko_NamePlate_Y[1], 1);
            }

            if (TJAPlayer3.Tx.Taiko_PlayerNumber[0] != null)
            {
                TJAPlayer3.Tx.Taiko_PlayerNumber[0].t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Taiko_PlayerNumber_X[0], TJAPlayer3.Skin.Game_Taiko_PlayerNumber_Y[0]);
            }
            if (TJAPlayer3.stage演奏ドラム画面.bDoublePlay && TJAPlayer3.Tx.Taiko_PlayerNumber[1] != null)
            {
                TJAPlayer3.Tx.Taiko_PlayerNumber[1].t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Taiko_PlayerNumber_X[1], TJAPlayer3.Skin.Game_Taiko_PlayerNumber_Y[1]);
            }
            return base.On進行描画();
        }

        public void tMtaikoEvent( int nChannel, int nHand, int nPlayer )
        {
            CConfigIni configIni = TJAPlayer3.ConfigIni;
            bool bAutoPlay = false;
            int playerShift = 5 * nPlayer;
            var _gt = configIni.nGameType[TJAPlayer3.GetActualPlayer(nPlayer)];

            switch (nPlayer)
            {
                case 0:
                    bAutoPlay = configIni.b太鼓パートAutoPlay;
                    break;
                case 1:
                    bAutoPlay = configIni.b太鼓パートAutoPlay2P || configIni.nAILevel > 0;
                    break;
                default:
                    break;
            }

            if ( !bAutoPlay )
            {
                switch( nChannel )
                {
                    case 0x11:
                    case 0x13:
                    case 0x15:
                    case 0x16:
                    case 0x17:
                        {
                            this.stパッド状態[ 2 + nHand + playerShift].n明るさ = 8;
                        }
                        break;
                    case 0x12:
                        {
                            this.stパッド状態[nHand + playerShift].n明るさ = 8;
                        }
                        break;
                    case 0x14:
                        {
                            if (_gt == EGameType.KONGA)
                            {
                                this.stパッド状態[4 + playerShift].n明るさ = 8;
                            }
                            else
                            {
                                this.stパッド状態[nHand + playerShift].n明るさ = 8;
                            }
                        }
                        break;

                }
            }
            else
            {
                switch( nChannel )
                {
                    case 0x11:
                    case 0x15:
                    case 0x16:
                    case 0x17:
                    case 0x1F:
                        {
                            this.stパッド状態[ 2 + nHand + playerShift].n明るさ = 8;
                        }
                        break;
                            
                    case 0x13:
                    case 0x1A:
                        {
                            if (_gt == EGameType.KONGA)
                            {
                                this.stパッド状態[0 + playerShift].n明るさ = 8;
                                this.stパッド状態[2 + playerShift].n明るさ = 8;
                            }
                            else
                            {
                                this.stパッド状態[2 + playerShift].n明るさ = 8;
                                this.stパッド状態[3 + playerShift].n明るさ = 8;
                            }
                        }
                        break;

                    case 0x12:
                        {
                            this.stパッド状態[ nHand + playerShift].n明るさ = 8;
                        }
                        break;

                    case 0x14:
                    case 0x1B:
                        {
                            if (_gt == EGameType.KONGA)
                            {
                                this.stパッド状態[4 + playerShift].n明るさ = 8;
                            }
                            else
                            {
                                this.stパッド状態[0 + playerShift].n明るさ = 8;
                                this.stパッド状態[1 + playerShift].n明るさ = 8;
                            }
                            
                        }
                        break;

                    case 0x101:
                        {
                            this.stパッド状態[nHand + playerShift].n明るさ = 8;
                            this.stパッド状態[2 + (nHand == 0 ? 1 : 0) + playerShift].n明るさ = 8;
                            break;
                        }
                }
            }

        }

        public void tBranchEvent(CDTX.ECourse Before, CDTX.ECourse After, int player)
        {
            if ( After != Before )
                this.ctレベルアップダウン[ player ] = new CCounter( 0, 1000, 1, TJAPlayer3.Timer );

            this.After[ player ] = After;
            this.Before[ player ] = Before;
        }


        #region[ private ]
        //-----------------
        //構造体
        [StructLayout(LayoutKind.Sequential)]
        private struct STパッド状態
        {
            public int n明るさ;
        }

        //太鼓
        private STパッド状態[] stパッド状態 = new STパッド状態[ 4 * 5 ];
        private long nフラッシュ制御タイマ;

        //private CTexture[] txコースシンボル = new CTexture[ 6 ];
        private string[] strCourseSymbolFileName;

        //オプション
        private CTexture txオプションパネル_HS;
        private CTexture txオプションパネル_RANMIR;
        private CTexture txオプションパネル_特殊;
        private int nHS;

        //譜面分岐
        private CCounter[] ctレベルアップダウン;
        public CDTX.ECourse[] After;
        public CDTX.ECourse[] Before;
        //-----------------
        #endregion

    }
}
　
