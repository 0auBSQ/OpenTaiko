using System;
using System.Collections.Generic;
using System.Text;
using FDK;
using System.Drawing;
using System.Runtime.InteropServices;

namespace TJAPlayer3
{
	internal class CAct演奏スコア共通 : CActivity
	{
		// プロパティ

		protected STDGBVALUE<long>[] nスコアの増分;
		protected STDGBVALUE<double>[] n現在の本当のスコア;
		protected STDGBVALUE<long>[] n現在表示中のスコア;
		//protected CTexture txScore;

  //      protected CTexture txScore_1P;
        protected CCounter ctTimer;
        public CCounter[] ct点数アニメタイマ;

        public CCounter[] ctボーナス加算タイマ;

        protected STスコア[] stScore;
        protected int n現在表示中のAddScore;

        [StructLayout( LayoutKind.Sequential )]
        protected struct STスコア
        {
            public bool bAddEnd;
            public bool b使用中;
            public bool b表示中;
            public bool bBonusScore;
            public CCounter ctTimer;
            public int nAddScore;
            public int nPlayer;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ST文字位置
        {
            public char ch;
            public Point pt;
        }
        private ST文字位置[] stFont;


        public long GetScore(int player)
        {
            return n現在表示中のスコア[player].Taiko;
        }

		// コンストラクタ

		public CAct演奏スコア共通()
		{
            ST文字位置[] st文字位置Array = new ST文字位置[11];
            ST文字位置 st文字位置 = new ST文字位置();
            st文字位置.ch = '0';
            st文字位置.pt = new Point(0, 0);
            st文字位置Array[0] = st文字位置;
            ST文字位置 st文字位置2 = new ST文字位置();
            st文字位置2.ch = '1';
            st文字位置2.pt = new Point(24, 0);
            st文字位置Array[1] = st文字位置2;
            ST文字位置 st文字位置3 = new ST文字位置();
            st文字位置3.ch = '2';
            st文字位置3.pt = new Point(48, 0);
            st文字位置Array[2] = st文字位置3;
            ST文字位置 st文字位置4 = new ST文字位置();
            st文字位置4.ch = '3';
            st文字位置4.pt = new Point(72, 0);
            st文字位置Array[3] = st文字位置4;
            ST文字位置 st文字位置5 = new ST文字位置();
            st文字位置5.ch = '4';
            st文字位置5.pt = new Point(96, 0);
            st文字位置Array[4] = st文字位置5;
            ST文字位置 st文字位置6 = new ST文字位置();
            st文字位置6.ch = '5';
            st文字位置6.pt = new Point(120, 0);
            st文字位置Array[5] = st文字位置6;
            ST文字位置 st文字位置7 = new ST文字位置();
            st文字位置7.ch = '6';
            st文字位置7.pt = new Point(144, 0);
            st文字位置Array[6] = st文字位置7;
            ST文字位置 st文字位置8 = new ST文字位置();
            st文字位置8.ch = '7';
            st文字位置8.pt = new Point(168, 0);
            st文字位置Array[7] = st文字位置8;
            ST文字位置 st文字位置9 = new ST文字位置();
            st文字位置9.ch = '8';
            st文字位置9.pt = new Point(192, 0);
            st文字位置Array[8] = st文字位置9;
            ST文字位置 st文字位置10 = new ST文字位置();
            st文字位置10.ch = '9';
            st文字位置10.pt = new Point(216, 0);
            st文字位置Array[9] = st文字位置10;
            this.stFont = st文字位置Array;

            this.stScore = new STスコア[ 256 ];
			base.IsDeActivated = true;
		}


        // メソッド

        private float[,] n点数アニメ拡大率_座標 = new float[,]
    {
            {
                1.14f,
                -5f
            },
            {
                1.2f,
                -6f
            },
            {
                1.23f,
                -8f
            },
            {
                1.25f,
                -9f
            },
            {
                1.23f,
                -8f
            },
            {
                1.2f,
                -6f
            },
            {
                1.14f,
                -5f
            },
            {
                1.08f,
                -4f
            },
            {
                1.04f,
                -2f
            },
            {
                1.02f,
                -1f
            },
            {
                1.01f,
                -1f
            },
            {
                1f,
                0f
            }
        };

        private float[] ScoreScale = new float[]
        {
           1f,
            1.050f,
            1.100f,
            1.110f,
            1.120f,
            1.125f,
            1.120f,
            1.080f,
            1.065f,
            1.030f,
            1.015f,
            1f
        };

        public double Get( E楽器パート part, int player )
		{
			return this.n現在の本当のスコア[ player ][ (int) part ];
		}
		public void Set( E楽器パート part, double nScore, int player )
		{
            //現状、TAIKOパートでの演奏記録を結果ステージに持っていけないので、ドラムパートにも加算することでお茶を濁している。
            if( part == E楽器パート.TAIKO )
                part = E楽器パート.DRUMS;

			int nPart = (int) part;
			if( this.n現在の本当のスコア[ player ][ nPart ] != nScore )
			{
				this.n現在の本当のスコア[ player ][ nPart ] = nScore;
				this.nスコアの増分[ player ][ nPart ] = (long) ( ( (double) ( this.n現在の本当のスコア[ player ][ nPart ] - this.n現在表示中のスコア[ player ][ nPart ] ) ) / 20.0 );
				this.nスコアの増分[ player ].Guitar = (long) ( ( (double) ( this.n現在の本当のスコア[ player ][ nPart ] - this.n現在表示中のスコア[ player ][ nPart ] ) ) );
				if( this.nスコアの増分[ player ][ nPart ] < 1L )
				{
					this.nスコアの増分[ player ][ nPart ] = 1L;
				}
			}

            if( part == E楽器パート.DRUMS )
                part = E楽器パート.TAIKO;

			nPart = (int) part;
			if( this.n現在の本当のスコア[ player ][ nPart ] != nScore )
			{
				this.n現在の本当のスコア[ player ][ nPart ] = nScore;
				this.nスコアの増分[ player ][ nPart ] = (long) ( ( (double) ( this.n現在の本当のスコア[ player ][ nPart ] - this.n現在表示中のスコア[ player ][ nPart ] ) ) / 20.0 );
                this.nスコアの増分[ player ].Guitar = (long) ( ( (double) ( this.n現在の本当のスコア[ player ][ nPart ] - this.n現在表示中のスコア[ player ][ nPart ] ) ) );
				if( this.nスコアの増分[ player ][ nPart ] < 1L )
				{
					this.nスコアの増分[ player ][ nPart ] = 1L;
				}
			}
            
		}
		/// <summary>
		/// 点数を加える(各種AUTO補正つき)
		/// </summary>
		/// <param name="part"></param>
		/// <param name="bAutoPlay"></param>
		/// <param name="delta"></param>
		public void Add( E楽器パート part, STAUTOPLAY bAutoPlay, long delta, int player )
        {
            if (TJAPlayer3.ConfigIni.bAIBattleMode && player == 1) return;

            double rev = 1.0;

            delta = (long)(delta * TJAPlayer3.stage選曲.actPlayOption.tGetModMultiplier(CActPlayOption.EBalancingType.SCORE, false, player));

			switch ( part )
			{
				#region [ Unknown ]
				case E楽器パート.UNKNOWN:
					throw new ArgumentException();
				#endregion
			}

            this.ctTimer = new CCounter( 0, 400, 1, TJAPlayer3.Timer );

            for( int sc = 0; sc < 1; sc++ )
            {
                for( int i = 0; i < 256; i++ )
                {
                    if( this.stScore[ i ].b使用中 == false )
                    {
                        this.stScore[ i ].b使用中 = true;
                        this.stScore[ i ].b表示中 = true;
                        this.stScore[ i ].nAddScore = (int)delta;
                        this.stScore[ i ].ctTimer = new CCounter( 0, 465, 2, TJAPlayer3.Timer );
                        this.stScore[ i ].bBonusScore = false;
                        this.stScore[ i ].bAddEnd = false;
                        this.stScore[ i ].nPlayer = player;
                        this.n現在表示中のAddScore++;
                        break;
                    }
                }
            }

			this.Set( part, this.Get( part, player ) + delta * rev, player );
		}

        public void BonusAdd( int player )
        {
            if (TJAPlayer3.ConfigIni.bAIBattleMode && player == 1) return;

            for ( int sc = 0; sc < 1; sc++ )
            {
                for( int i = 0; i < 256; i++ )
                {
                    if( this.stScore[ i ].b使用中 == false )
                    {
                        this.stScore[ i ].b使用中 = true;
                        this.stScore[ i ].b表示中 = true;
                        this.stScore[ i ].nAddScore = 10000;
                        this.stScore[ i ].ctTimer = new CCounter( 0, 100, 4, TJAPlayer3.Timer );
                        this.stScore[ i ].bBonusScore = true;
                        this.stScore[ i ].bAddEnd = false;
                        this.stScore[ i ].nPlayer = player;
                        this.n現在表示中のAddScore++;
                        break;
                    }
                }
            }

            this.Set( E楽器パート.TAIKO, this.Get( E楽器パート.TAIKO, player ) + 10000, player );
        }

		// CActivity 実装

		public override void Activate()
		{
            this.n現在表示中のスコア = new STDGBVALUE<long>[ 5 ];
            this.n現在の本当のスコア = new STDGBVALUE<double>[ 5 ];
            this.nスコアの増分 = new STDGBVALUE<long>[ 5 ];
			for( int i = 0; i < 5; i++ )
            {
                for (int j = 0; j < 4; j++)
                {
                    this.n現在表示中のスコア[i][j] = 0L;
                    this.n現在の本当のスコア[i][j] = 0L;
                    this.nスコアの増分[i][j] = 0L;
                }
			}
            for( int sc = 0; sc < 256; sc++ )
            {
                this.stScore[ sc ].b使用中 = false;
                this.stScore[ sc ].ctTimer = new CCounter();
                this.stScore[ sc ].nAddScore = 0;
                this.stScore[ sc ].bBonusScore = false;
                this.stScore[ sc ].bAddEnd = false;
            }

            this.n現在表示中のAddScore = 0;

            this.ctTimer = new CCounter();

            this.ct点数アニメタイマ = new CCounter[5];
            for (int i = 0; i < 5; i++)
            {
                this.ct点数アニメタイマ[i] = new CCounter();
            }
            this.ctボーナス加算タイマ = new CCounter[5];
            for (int i = 0; i < 5; i++)
            {
                this.ctボーナス加算タイマ[i] = new CCounter();
            }
            base.Activate();
		}
		public override void CreateManagedResource()
		{
			//this.txScore = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\7_Score_number.png" ) );
    //      this.txScore_1P = CDTXMania.tテクスチャの生成(CSkin.Path(@"Graphics\7_Score_number_1P.png"));
			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource()
		{
			//CDTXMania.tテクスチャの解放( ref this.txScore );
    //      CDTXMania.tテクスチャの解放(ref this.txScore_1P);
			base.ReleaseManagedResource();
		}

        protected void t小文字表示( int x, int y, string str, int mode , int alpha, int player )
        {
            foreach( char ch in str )
            {
                for( int i = 0; i < this.stFont.Length; i++ )
                {
                    if( this.stFont[ i ].ch == ch )
                    {
                        Rectangle rectangle = new Rectangle(TJAPlayer3.Skin.Game_Score_Size[0] * i, 0, TJAPlayer3.Skin.Game_Score_Size[0], TJAPlayer3.Skin.Game_Score_Size[1]);
                        switch( mode )
                        {
                            case 0:
                                if( TJAPlayer3.Tx.Taiko_Score[0] != null )
                                {
                                    //this.txScore.color4 = new SlimDX.Color4( 1.0f, 1.0f, 1.0f );
                                    TJAPlayer3.Tx.Taiko_Score[0].Opacity = alpha;
                                    if (TJAPlayer3.ConfigIni.SimpleMode)
                                    {
                                        TJAPlayer3.Tx.Taiko_Score[0].vc拡大縮小倍率.Y = 1;
                                    }
                                    else
                                    {
                                        TJAPlayer3.Tx.Taiko_Score[0].vc拡大縮小倍率.Y = ScoreScale[this.ct点数アニメタイマ[player].CurrentValue];
                                    }
                                    TJAPlayer3.Tx.Taiko_Score[0].t2D拡大率考慮下基準描画( x , y, rectangle );
                                    
                                }
                                break;
                            case 1:
                                if(TJAPlayer3.Tx.Taiko_Score[1] != null )
                                {
                                    //this.txScore.color4 = new SlimDX.Color4( 1.0f, 0.5f, 0.4f );
                                    //this.txScore.color4 = CDTXMania.Skin.cScoreColor1P;
                                    TJAPlayer3.Tx.Taiko_Score[1].Opacity = alpha;
                                    TJAPlayer3.Tx.Taiko_Score[1].vc拡大縮小倍率.Y = 1;
                                    TJAPlayer3.Tx.Taiko_Score[1].t2D拡大率考慮下基準描画( x, y, rectangle );
                                }
                                break;
                            case 2:
                                if (TJAPlayer3.Tx.Taiko_Score[2] != null)
                                {
                                    //this.txScore.color4 = new SlimDX.Color4( 0.4f, 0.5f, 1.0f );
                                    //this.txScore.color4 = CDTXMania.Skin.cScoreColor2P;
                                    TJAPlayer3.Tx.Taiko_Score[2].Opacity = alpha;
                                    TJAPlayer3.Tx.Taiko_Score[2].vc拡大縮小倍率.Y = 1;
                                    TJAPlayer3.Tx.Taiko_Score[2].t2D拡大率考慮下基準描画(x, y, rectangle);
                                }
                                break;
                            case 3:
                                if (TJAPlayer3.Tx.Taiko_Score[3] != null)
                                {
                                    //this.txScore.color4 = new SlimDX.Color4( 0.4f, 0.5f, 1.0f );
                                    //this.txScore.color4 = CDTXMania.Skin.cScoreColor2P;
                                    TJAPlayer3.Tx.Taiko_Score[3].Opacity = alpha;
                                    TJAPlayer3.Tx.Taiko_Score[3].vc拡大縮小倍率.Y = 1;
                                    TJAPlayer3.Tx.Taiko_Score[3].t2D拡大率考慮下基準描画(x, y, rectangle);
                                }
                                break;
                            case 4:
                                if (TJAPlayer3.Tx.Taiko_Score[4] != null)
                                {
                                    //this.txScore.color4 = new SlimDX.Color4( 0.4f, 0.5f, 1.0f );
                                    //this.txScore.color4 = CDTXMania.Skin.cScoreColor2P;
                                    TJAPlayer3.Tx.Taiko_Score[4].Opacity = alpha;
                                    TJAPlayer3.Tx.Taiko_Score[4].vc拡大縮小倍率.Y = 1;
                                    TJAPlayer3.Tx.Taiko_Score[4].t2D拡大率考慮下基準描画(x, y, rectangle);
                                }
                                break;
                            case 5:
                                if (TJAPlayer3.Tx.Taiko_Score[5] != null)
                                {
                                    //this.txScore.color4 = new SlimDX.Color4( 0.4f, 0.5f, 1.0f );
                                    //this.txScore.color4 = CDTXMania.Skin.cScoreColor2P;
                                    TJAPlayer3.Tx.Taiko_Score[5].Opacity = alpha;
                                    TJAPlayer3.Tx.Taiko_Score[5].vc拡大縮小倍率.Y = 1;
                                    TJAPlayer3.Tx.Taiko_Score[5].t2D拡大率考慮下基準描画(x, y, rectangle);
                                }
                                break;
                        }
                        break;
                    }
                }
                x += TJAPlayer3.Skin.Game_Score_Padding;
            }
        }
	}
}
