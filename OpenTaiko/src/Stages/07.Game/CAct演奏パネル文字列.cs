using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using SkiaSharp;
using FDK;
using static TJAPlayer3.CActSelect曲リスト;

using Color = System.Drawing.Color;

namespace TJAPlayer3
{

    internal class CAct演奏パネル文字列 : CActivity
	{
        public static int tToArgb(int r, int g, int b)
        {
            return (b * 65536 + g * 256 + r);
        }
        
        // コンストラクタ
        public CAct演奏パネル文字列()
		{
			base.IsDeActivated = true;
			this.Start();
		}

        private readonly Dictionary<string, Color4> tTagDict = new Dictionary<string, Color4>
        {
            ["アニメ"] = new Color4(tToArgb(253, 145, 208)),
            ["Anime"] = new Color4(tToArgb(253, 145, 208)),
            ["クラシック"] = new Color4(tToArgb(221, 172, 4)),
            ["Classical"] = new Color4(tToArgb(221, 172, 4)),
            ["バラエティ"] = new Color4(tToArgb(32, 218, 56)),
            ["Variety"] = new Color4(tToArgb(32, 218, 56)),
            ["どうよう"] = new Color4(tToArgb(254, 191, 3)),
            ["キッズ"] = new Color4(tToArgb(254, 191, 3)),
            ["Children & Folk"] = new Color4(tToArgb(254, 191, 3)),
            ["ボーカロイド"] = new Color4(tToArgb(204, 207, 222)),
            ["VOCALOID"] = new Color4(tToArgb(204, 207, 222)),
            ["Vocaloid"] = new Color4(tToArgb(204, 207, 222)),
            ["ゲームミュージック"] = new Color4(tToArgb(205, 138, 237)),
            ["ゲームバラエティ"] = new Color4(tToArgb(205, 138, 237)),
            ["Game Music"] = new Color4(tToArgb(205, 138, 237)),
            ["J-POP"] = new Color4(tToArgb(68, 192, 209)),
            ["POP"] = new Color4(tToArgb(68, 192, 209)),
            ["ナムコオリジナル"] = new Color4(tToArgb(255, 70, 28)),
            ["OpenTaikoオリジナル"] = new Color4(tToArgb(249, 255, 40)),
            ["OpenTaiko Original"] = new Color4(tToArgb(249, 255, 40)),
            ["ポップス"] = new Color4(tToArgb(68, 192, 209)),
            ["太鼓タワー"] = new Color4(tToArgb(254, 191, 3)),
            ["Taiko Towers"] = new Color4(tToArgb(254, 191, 3)), // Temporary, will use CLang
            ["段位道場"] = new Color4(tToArgb(42, 122, 169)),
        };


        // メソッド

        /// <summary>
        /// 右上の曲名、曲数表示の更新を行います。
        /// </summary>
        /// <param name="songName">曲名</param>
        /// <param name="genreName">ジャンル名</param>
        /// <param name="stageText">曲数</param>
        public void SetPanelString(string songName, string genreName, string stageText = null, CSongListNode songNode = null)
		{
			if( base.IsActivated )
			{
				TJAPlayer3.tテクスチャの解放( ref this.txPanel );
				if( (songName != null ) && (songName.Length > 0 ) )
				{
					try
					{
					    using (var bmpSongTitle = pfMusicName.DrawText(songName, TJAPlayer3.Skin.Game_MusicName_ForeColor, TJAPlayer3.Skin.Game_MusicName_BackColor, null, 30))
					    {
					        this.txMusicName = TJAPlayer3.tテクスチャの生成( bmpSongTitle, false );
					    }
                        if (txMusicName != null)
                        {
                            this.txMusicName.vcScaleRatio.X = TJAPlayer3.GetSongNameXScaling(ref txMusicName);
                        }
                    
                        SKBitmap bmpDiff;
                        string strDiff = "";
                        if (TJAPlayer3.Skin.eDiffDispMode == E難易度表示タイプ.n曲目に表示)
                        {
                            switch (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0])
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
                            bmpDiff = pfMusicName.DrawText(strDiff + stageText, TJAPlayer3.Skin.Game_StageText_ForeColor, TJAPlayer3.Skin.Game_StageText_BackColor, null, 30 );
                        }
                        else
                        {
                            bmpDiff = pfMusicName.DrawText(stageText, TJAPlayer3.Skin.Game_StageText_ForeColor, TJAPlayer3.Skin.Game_StageText_BackColor, null, 30 );
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

                this.txGENRE = TJAPlayer3.Tx.TxCGen("Template");

                Color stageColor = Color.White;
                if (songNode != null && songNode.isChangedBoxColor)
                    stageColor = songNode.BoxColor;

                if (!(songNode != null && songNode.isChangedBoxColor) 
                    && tTagDict != null 
                    && tTagDict.ContainsKey(genreName))
                {
                    this.txGENRE.color4 = tTagDict[genreName];
                }
                else if (genreName == CLangManager.LangInstance.GetString(101))
                {
                    this.txGENRE.color4 = tTagDict["段位道場"];
                }
                else
                {
                    this.txGENRE.color4 = CConversion.ColorToColor4(stageColor);
                }
                
                pfGENRE = HPrivateFastFont.tInstantiateBoxFont(TJAPlayer3.Skin.Game_GenreText_FontSize);

                this.ttkGENRE = new TitleTextureKey(genreName, this.pfGENRE, Color.White, Color.Black, 1000);

                this.ct進行用 = new CCounter( 0, 2000, 2, TJAPlayer3.Timer );
				this.Start();



			}
		}

        public void t歌詞テクスチャを生成する( SKBitmap bmplyric )
        {
            TJAPlayer3.tDisposeSafely(ref this.tx歌詞テクスチャ);
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
                this.tx歌詞テクスチャ.t2D描画(TJAPlayer3.Skin.Game_Lyric_X , TJAPlayer3.Skin.Game_Lyric_Y - (this.tx歌詞テクスチャ.szTextureSize.Height));
                }
                else if (TJAPlayer3.Skin.Game_Lyric_ReferencePoint == CSkin.ReferencePoint.Right)
                {
                this.tx歌詞テクスチャ.t2D描画(TJAPlayer3.Skin.Game_Lyric_X - this.tx歌詞テクスチャ.szTextureSize.Width, TJAPlayer3.Skin.Game_Lyric_Y - (this.tx歌詞テクスチャ.szTextureSize.Height));
                }
                else
                {
                this.tx歌詞テクスチャ.t2D描画(TJAPlayer3.Skin.Game_Lyric_X - (this.tx歌詞テクスチャ.szTextureSize.Width / 2), TJAPlayer3.Skin.Game_Lyric_Y - (this.tx歌詞テクスチャ.szTextureSize.Height));
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

		public override void Activate()
		{
            this.pfMusicName = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.Game_MusicName_FontSize);
            this.txPanel = null;
			this.ct進行用 = new CCounter();
			this.Start();
            this.bFirst = true;
			base.Activate();
		}
		public override void DeActivate()
		{
			this.ct進行用 = null;
			TJAPlayer3.tDisposeSafely( ref this.txPanel );
			TJAPlayer3.tDisposeSafely( ref this.txMusicName );
            TJAPlayer3.tDisposeSafely( ref this.txGENRE );
            TJAPlayer3.tDisposeSafely(ref this.pfGENRE);
            TJAPlayer3.tDisposeSafely(ref this.txPanel);
            TJAPlayer3.tDisposeSafely(ref this.pfMusicName);
            TJAPlayer3.tDisposeSafely(ref this.pf歌詞フォント);
            TJAPlayer3.tDisposeSafely(ref this.tx歌詞テクスチャ);
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
			throw new InvalidOperationException( "t進行描画(x,y)のほうを使用してください。" );
		}
		public int t進行描画( int x, int y )
		{
            if (TJAPlayer3.stage演奏ドラム画面.actDan.IsAnimating || TJAPlayer3.ConfigIni.nPlayerCount > 2) return 0;
			if( !base.IsDeActivated && !this.bMute )
			{
				this.ct進行用.TickLoop();

                if( this.txGENRE != null )
                {
                    this.txGENRE.t2D描画(TJAPlayer3.Skin.Game_Genre_X, TJAPlayer3.Skin.Game_Genre_Y);
                    TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(this.ttkGENRE).t2D拡大率考慮中央基準描画(TJAPlayer3.Skin.Game_Genre_X + TJAPlayer3.Skin.Game_GenreText_Offset[0], TJAPlayer3.Skin.Game_Genre_Y + TJAPlayer3.Skin.Game_GenreText_Offset[1]);
                }
                if( this.txStage != null )
                    this.txStage.t2D描画( TJAPlayer3.Skin.Game_Genre_X, TJAPlayer3.Skin.Game_Genre_Y );

                if( TJAPlayer3.Skin.b現在のステージ数を表示しない )
                {
                    if( this.txMusicName != null )
                    {
                        float fRate = (float)TJAPlayer3.Skin.Game_MusicName_MaxWidth / this.txMusicName.szTextureSize.Width;
                        if (this.txMusicName.szTextureSize.Width <= TJAPlayer3.Skin.Game_MusicName_MaxWidth)
                            fRate = 1.0f;

                        this.txMusicName.vcScaleRatio.X = fRate;

                        this.txMusicName.t2D描画(TJAPlayer3.Skin.Game_MusicName_X - (this.txMusicName.szTextureSize.Width * fRate), TJAPlayer3.Skin.Game_MusicName_Y);
                    }
                }
                else
                {
                    #region[ 透明度制御 ]

                    if( this.ct進行用.CurrentValue < 745 )
                    {
                        if( this.txStage != null )
                            this.txStage.Opacity = 0;
                    }
                    else if( this.ct進行用.CurrentValue >= 745 && this.ct進行用.CurrentValue < 1000 )
                    {
                        if( this.txStage != null )
                            this.txStage.Opacity = ( this.ct進行用.CurrentValue - 745 );
                    }
                    else if( this.ct進行用.CurrentValue >= 1000 && this.ct進行用.CurrentValue <= 1745 )
                    {
                        if( this.txStage != null )
                            this.txStage.Opacity = 255;
                    }
                    else if( this.ct進行用.CurrentValue >= 1745 )
                    {
                        if( this.txStage != null )
                            this.txStage.Opacity = 255 - (this.ct進行用.CurrentValue - 1745);
                    }
                    #endregion

                    if( this.txMusicName != null )
                    {
                        if(this.IsFirstDraw)
                        {
                            IsFirstDraw = false;
                        }
                        if (this.txMusicName != null)
                        {
                            float fRate = (float)TJAPlayer3.Skin.Game_MusicName_MaxWidth / this.txMusicName.szTextureSize.Width;
                            if (this.txMusicName.szTextureSize.Width <= TJAPlayer3.Skin.Game_MusicName_MaxWidth)
                                fRate = 1.0f;

                            this.txMusicName.vcScaleRatio.X = fRate;

                            this.txMusicName.t2D描画(TJAPlayer3.Skin.Game_MusicName_X - (this.txMusicName.szTextureSize.Width * fRate), TJAPlayer3.Skin.Game_MusicName_Y);
                        }
                    }
                }

                //CDTXMania.act文字コンソール.tPrint( 0, 0, C文字コンソール.Eフォント種別.白, this.ct進行用.n現在の値.ToString() );

				//this.txMusicName.t2D描画( CDTXMania.app.Device, 1250 - this.txMusicName.szテクスチャサイズ.Width, 14 );
			}
			return 0;
		}

        public enum ESongType
        {
            REGULAR,
            DAN,
            TOWER,
            BOSS,
            TOTAL,
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
        private CCachedFontRenderer pfGENRE;
        private TitleTextureKey ttkGENRE;
        private CTexture tx歌詞テクスチャ;
        private CCachedFontRenderer pfMusicName;
        private CCachedFontRenderer pf歌詞フォント;
		//-----------------
		#endregion
	}
}
　
