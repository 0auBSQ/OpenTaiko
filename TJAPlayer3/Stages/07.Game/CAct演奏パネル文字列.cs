using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using SlimDX;
using FDK;

namespace TJAPlayer3
{
	internal class CAct演奏パネル文字列 : CActivity
	{

		// コンストラクタ

		public CAct演奏パネル文字列()
		{
			base.b活性化してない = true;
			this.Start();
		}


        // メソッド

        /// <summary>
        /// 右上の曲名、曲数表示の更新を行います。
        /// </summary>
        /// <param name="songName">曲名</param>
        /// <param name="genreName">ジャンル名</param>
        /// <param name="stageText">曲数</param>
        public void SetPanelString(string songName, string genreName, string stageText = null)
		{
			if( base.b活性化してる )
			{
				TJAPlayer3.tテクスチャの解放( ref this.txPanel );
				if( (songName != null ) && (songName.Length > 0 ) )
				{
					try
					{
					    using (var bmpSongTitle = pfMusicName.DrawPrivateFont(songName, TJAPlayer3.Skin.Game_MusicName_ForeColor, TJAPlayer3.Skin.Game_MusicName_BackColor))
					    {
					        this.txMusicName = TJAPlayer3.tテクスチャの生成( bmpSongTitle, false );
					    }
                        if (txMusicName != null)
                        {
                            this.txMusicName.vc拡大縮小倍率.X = TJAPlayer3.GetSongNameXScaling(ref txMusicName);
                        }
                    
                        Bitmap bmpDiff;
                        string strDiff = "";
                        if (TJAPlayer3.Skin.eDiffDispMode == E難易度表示タイプ.n曲目に表示)
                        {
                            switch (TJAPlayer3.stage選曲.n確定された曲の難易度[0])
                            {
                                case 0:
                                    strDiff = "かんたん ";
                                    break;
                                case 1:
                                    strDiff = "ふつう ";
                                    break;
                                case 2:
                                    strDiff = "むずかしい ";
                                    break;
                                case 3:
                                    strDiff = "おに ";
                                    break;
                                case 4:
                                    strDiff = "えでぃと ";
                                    break;
                                default:
                                    strDiff = "おに ";
                                    break;
                            }
                            bmpDiff = pfMusicName.DrawPrivateFont(strDiff + stageText, TJAPlayer3.Skin.Game_StageText_ForeColor, TJAPlayer3.Skin.Game_StageText_BackColor );
                        }
                        else
                        {
                            bmpDiff = pfMusicName.DrawPrivateFont(stageText, TJAPlayer3.Skin.Game_StageText_ForeColor, TJAPlayer3.Skin.Game_StageText_BackColor );
                        }

					    using (bmpDiff)
					    {
                            txStage = TJAPlayer3.Tx.TxCGen("Songs");
                        }
					}
					catch( CTextureCreateFailedException e )
					{
						Trace.TraceError( e.ToString() );
						Trace.TraceError( "パネル文字列テクスチャの生成に失敗しました。" );
						this.txPanel = null;
					}
				}
                if( !string.IsNullOrEmpty(genreName) )
                {
                    if (genreName.Equals("J-POP"))
                    {
                        this.txGENRE = TJAPlayer3.Tx.TxCGen("Pops");
                    }
                    else if (genreName.Equals( "アニメ" ) )
                    {
                        this.txGENRE = TJAPlayer3.Tx.TxCGen("Anime");
                    }
                    else if(genreName.Equals( "ゲームミュージック" ) )
                    {
                        this.txGENRE = TJAPlayer3.Tx.TxCGen("Game");
                    }
                    else if(genreName.Equals( "ナムコオリジナル" ) )
                    {
                        this.txGENRE = TJAPlayer3.Tx.TxCGen("Namco");
                    }
                    else if(genreName.Equals( "クラシック" ) )
                    {
                        this.txGENRE = TJAPlayer3.Tx.TxCGen("Classic");
                    }
                    else if(genreName.Equals( "バラエティ" ) )
                    {
                        this.txGENRE = TJAPlayer3.Tx.TxCGen("Variety");
                    }
                    else if(genreName.Equals( "どうよう" ) )
                    {
                        this.txGENRE = TJAPlayer3.Tx.TxCGen("Child");
                    }
                    else if(genreName.Equals( "バラエティ" ) )
                    {
                        this.txGENRE = TJAPlayer3.Tx.TxCGen("Variety");
                    }
                    else if(genreName.Equals( "ボーカロイド" ) || genreName.Equals("Vocaloid") )
                    {
                        this.txGENRE = TJAPlayer3.Tx.TxCGen("Vocaloid");
                    }
                    else
                    {
                        using (var bmpDummy = new Bitmap( 1, 1 ))
                        {
                            this.txGENRE = TJAPlayer3.tテクスチャの生成( bmpDummy, true );
                        }
                    }
                }

			    this.ct進行用 = new CCounter( 0, 2000, 2, TJAPlayer3.Timer );
				this.Start();



			}
		}

        public void t歌詞テクスチャを生成する( Bitmap bmplyric )
        {
            TJAPlayer3.t安全にDisposeする(ref this.tx歌詞テクスチャ);
            this.tx歌詞テクスチャ = TJAPlayer3.tテクスチャの生成( bmplyric );
        }
        public void t歌詞テクスチャを削除する()
        {
            TJAPlayer3.tテクスチャの解放(ref this.tx歌詞テクスチャ);
        }
        /// <summary>
        /// レイヤー管理のため、On進行描画から分離。
        /// </summary>
        public void t歌詞テクスチャを描画する()
        {
            if( this.tx歌詞テクスチャ != null )
            {
                if (TJAPlayer3.Skin.Game_Lyric_ReferencePoint == CSkin.ReferencePoint.Left)
                {
                this.tx歌詞テクスチャ.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Lyric_X , TJAPlayer3.Skin.Game_Lyric_Y);
                }
                else if (TJAPlayer3.Skin.Game_Lyric_ReferencePoint == CSkin.ReferencePoint.Right)
                {
                this.tx歌詞テクスチャ.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Lyric_X - this.tx歌詞テクスチャ.szテクスチャサイズ.Width, TJAPlayer3.Skin.Game_Lyric_Y);
                }
                else
                {
                this.tx歌詞テクスチャ.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Lyric_X - (this.tx歌詞テクスチャ.szテクスチャサイズ.Width / 2), TJAPlayer3.Skin.Game_Lyric_Y);
                }
            }
        }

		public void Stop()
		{
			this.bMute = true;
		}
		public void Start()
		{
			this.bMute = false;
		}


		// CActivity 実装

		public override void On活性化()
		{
            if( !string.IsNullOrEmpty( TJAPlayer3.ConfigIni.FontName ) )
            {
                this.pfMusicName = new CPrivateFastFont( new FontFamily( TJAPlayer3.ConfigIni.FontName), TJAPlayer3.Skin.Game_MusicName_FontSize);
                //this.pf縦書きテスト = new CPrivateFastFont( new FontFamily( CDTXMania.ConfigIni.strPrivateFontで使うフォント名 ), 22 );
            }

			this.txPanel = null;
			this.ct進行用 = new CCounter();
			this.Start();
            this.bFirst = true;
			base.On活性化();
		}
		public override void On非活性化()
		{
			this.ct進行用 = null;
			base.On非活性化();
		}
		public override void OnManagedリソースの作成()
		{
			if( !base.b活性化してない )
			{
				base.OnManagedリソースの作成();
			}
		}
		public override void OnManagedリソースの解放()
		{
			if( !base.b活性化してない )
			{
				TJAPlayer3.t安全にDisposeする( ref this.txPanel );
				TJAPlayer3.t安全にDisposeする( ref this.txMusicName );
                TJAPlayer3.t安全にDisposeする( ref this.txGENRE );
                TJAPlayer3.t安全にDisposeする(ref this.txPanel);
                TJAPlayer3.t安全にDisposeする(ref this.tx歌詞テクスチャ);
                TJAPlayer3.t安全にDisposeする(ref this.pfMusicName);
                TJAPlayer3.t安全にDisposeする(ref this.pf歌詞フォント);
                base.OnManagedリソースの解放();
			}
		}
		public override int On進行描画()
		{
			throw new InvalidOperationException( "t進行描画(x,y)のほうを使用してください。" );
		}
		public int t進行描画( int x, int y )
		{
            if (TJAPlayer3.stage演奏ドラム画面.actDan.IsAnimating) return 0;
			if( !base.b活性化してない && !this.bMute )
			{
				this.ct進行用.t進行Loop();
                if( this.txGENRE != null )
                    this.txGENRE.t2D描画( TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Genre_X, TJAPlayer3.Skin.Game_Genre_Y );
                if( this.txStage != null )
                    this.txStage.t2D描画( TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Genre_X, TJAPlayer3.Skin.Game_Genre_Y );

                if( TJAPlayer3.Skin.b現在のステージ数を表示しない )
                {
                    if( this.txMusicName != null )
                    {
                        float fRate = 660.0f / this.txMusicName.szテクスチャサイズ.Width;
                        if (this.txMusicName.szテクスチャサイズ.Width <= 660.0f)
                            fRate = 1.0f;

                        this.txMusicName.vc拡大縮小倍率.X = fRate;

                        if (this.txMusicName.szテクスチャサイズ.Width >= 195)
                            this.txMusicName.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_MusicName_X - ((this.txMusicName.szテクスチャサイズ.Width * fRate) / 2) - (this.txMusicName.szテクスチャサイズ.Width / 2), TJAPlayer3.Skin.Game_MusicName_Y);
                        else
                            this.txMusicName.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_MusicName_X - ((this.txMusicName.szテクスチャサイズ.Width * fRate) / 2), TJAPlayer3.Skin.Game_MusicName_Y);
                    }
                }
                else
                {
                    #region[ 透明度制御 ]

                    if( this.ct進行用.n現在の値 < 745 )
                    {
                        if( this.txStage != null )
                            this.txStage.Opacity = 0;
                    }
                    else if( this.ct進行用.n現在の値 >= 745 && this.ct進行用.n現在の値 < 1000 )
                    {
                        if( this.txStage != null )
                            this.txStage.Opacity = ( this.ct進行用.n現在の値 - 745 );
                    }
                    else if( this.ct進行用.n現在の値 >= 1000 && this.ct進行用.n現在の値 <= 1745 )
                    {
                        if( this.txStage != null )
                            this.txStage.Opacity = 255;
                    }
                    else if( this.ct進行用.n現在の値 >= 1745 )
                    {
                        if( this.txStage != null )
                            this.txStage.Opacity = 255 - (this.ct進行用.n現在の値 - 1745);
                    }
                    #endregion

                    if( this.txMusicName != null )
                    {
                        if(this.b初めての進行描画)
                        {
                            b初めての進行描画 = false;
                        }
                        if (this.txMusicName != null)
                        {
                            float fRate = 660.0f / this.txMusicName.szテクスチャサイズ.Width;
                            if (this.txMusicName.szテクスチャサイズ.Width <= 660.0f)
                                fRate = 1.0f;

                            this.txMusicName.vc拡大縮小倍率.X = fRate;

                            if (this.txMusicName.szテクスチャサイズ.Width >= 195)
                                this.txMusicName.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_MusicName_X - ((this.txMusicName.szテクスチャサイズ.Width * fRate) / 2) - ((this.txMusicName.szテクスチャサイズ.Width - 195) / 2), TJAPlayer3.Skin.Game_MusicName_Y);
                            else
                                this.txMusicName.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_MusicName_X - ((this.txMusicName.szテクスチャサイズ.Width * fRate) / 2), TJAPlayer3.Skin.Game_MusicName_Y);
                        }
                    }
                }

                //CDTXMania.act文字コンソール.tPrint( 0, 0, C文字コンソール.Eフォント種別.白, this.ct進行用.n現在の値.ToString() );

				//this.txMusicName.t2D描画( CDTXMania.app.Device, 1250 - this.txMusicName.szテクスチャサイズ.Width, 14 );
			}
			return 0;
		}


		// その他

		#region [ private ]
		//-----------------
		private CCounter ct進行用;

		private CTexture txPanel;
		private bool bMute;
        private bool bFirst;

        private CTexture txMusicName;
        private CTexture txStage;
        private CTexture txGENRE;
        private CTexture tx歌詞テクスチャ;
        private CPrivateFastFont pfMusicName;
        private CPrivateFastFont pf歌詞フォント;
		//-----------------
		#endregion
	}
}
　
