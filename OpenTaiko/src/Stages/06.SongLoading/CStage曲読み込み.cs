using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Drawing.Text;
using Silk.NET.Maths;
using FDK;

using RectangleF = System.Drawing.RectangleF;
using Color = System.Drawing.Color;

namespace TJAPlayer3
{
	internal class CStage曲読み込み : CStage
	{
		// コンストラクタ

		public CStage曲読み込み()
		{
			base.eステージID = CStage.Eステージ.曲読み込み;
			base.eフェーズID = CStage.Eフェーズ.共通_通常状態;
			base.IsDeActivated = true;
			//base.list子Activities.Add( this.actFI = new CActFIFOBlack() );	// #27787 2012.3.10 yyagi 曲読み込み画面のフェードインの省略
			//base.list子Activities.Add( this.actFO = new CActFIFOBlack() );
		}


		// CStage 実装

		public override void Activate()
		{
			Trace.TraceInformation( "曲読み込みステージを活性化します。" );
			Trace.Indent();
			try
			{
				this.str曲タイトル = "";
				this.strSTAGEFILE = "";
				this.nBGM再生開始時刻 = -1;
				this.nBGMの総再生時間ms = 0;
				if( this.sd読み込み音 != null )
				{
					TJAPlayer3.Sound管理.tDisposeSound( this.sd読み込み音 );
					this.sd読み込み音 = null;
				}

			    if (TJAPlayer3.bコンパクトモード)
			    {
			        string strDTXファイルパス = TJAPlayer3.strコンパクトモードファイル;
				
			        CDTX cdtx = new CDTX( strDTXファイルパス, true, 1.0, 0, 0 );

			        if( File.Exists( cdtx.strフォルダ名 + @"set.def" ) )
			            cdtx = new CDTX( strDTXファイルパス, true, 1.0, 0, 1 );

			        this.str曲タイトル = cdtx.TITLE;
			        this.strサブタイトル = cdtx.SUBTITLE;

			        cdtx.DeActivate();
			    }
			    else
			    {
			        string strDTXファイルパス = TJAPlayer3.stage選曲.r確定されたスコア.ファイル情報.ファイルの絶対パス;

			        var strフォルダ名 = Path.GetDirectoryName(strDTXファイルパス) + Path.DirectorySeparatorChar;

			        if (File.Exists(strフォルダ名 + @"set.def"))
			        {
			            var cdtx = new CDTX(strDTXファイルパス, true, 1.0, 0, 1);

			            this.str曲タイトル = cdtx.TITLE;
			            this.strサブタイトル = cdtx.SUBTITLE;

			            cdtx.DeActivate();
			        }
			        else
			        {
			            var 譜面情報 = TJAPlayer3.stage選曲.r確定されたスコア.譜面情報;
			            this.str曲タイトル = 譜面情報.タイトル;
			            this.strサブタイトル = 譜面情報.strサブタイトル;
			        }
			    }

			    // For the moment, detect that we are performing
			    // calibration via there being an actual single
			    // player and the special song title and subtitle
			    // of the .tja used to perform input calibration
			    TJAPlayer3.IsPerformingCalibration =
			        !TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[0] &&
			        TJAPlayer3.ConfigIni.nPlayerCount == 1 &&
			        str曲タイトル == "Input Calibration" &&
			        strサブタイトル == "TJAPlayer3 Developers";

				this.strSTAGEFILE = CSkin.Path(@$"Graphics{Path.DirectorySeparatorChar}4_SongLoading{Path.DirectorySeparatorChar}Background.png");
				

				float wait = 600f;
				if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan)
					wait = 1000f;
				else if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
					wait = 1200f;

				this.ct待機 = new CCounter( 0, wait, 5, TJAPlayer3.Timer );
				this.ct曲名表示 = new CCounter( 1, 30, 30, TJAPlayer3.Timer );
				try
				{
					// When performing calibration, inform the player that
					// calibration is about to begin, rather than
					// displaying the song title and subtitle as usual.

					var タイトル = TJAPlayer3.IsPerformingCalibration
						? "Input calibration is about to begin."
						: this.str曲タイトル;

					var サブタイトル = TJAPlayer3.IsPerformingCalibration
						? "Please play as accurately as possible."
						: this.strサブタイトル;

					if( !string.IsNullOrEmpty(タイトル) )
					{
						//this.txタイトル = new CTexture( CDTXMania.app.Device, image, CDTXMania.TextureFormat );
						//this.txタイトル.vc拡大縮小倍率 = new Vector3( 0.5f, 0.5f, 1f );


						using (var bmpSongTitle = this.pfTITLE.DrawText( タイトル, TJAPlayer3.Skin.SongLoading_Title_ForeColor, TJAPlayer3.Skin.SongLoading_Title_BackColor, null, 30 ))

						{
							this.txタイトル = new CTexture( bmpSongTitle );
								txタイトル.vc拡大縮小倍率.X = TJAPlayer3.GetSongNameXScaling(ref txタイトル, 710);
						}

						using (var bmpSongSubTitle = this.pfSUBTITLE.DrawText( サブタイトル, TJAPlayer3.Skin.SongLoading_SubTitle_ForeColor, TJAPlayer3.Skin.SongLoading_SubTitle_BackColor, null, 30 ))


						{
								this.txサブタイトル = new CTexture( bmpSongSubTitle );
						}
					}
					else
					{
						this.txタイトル = null;
						this.txサブタイトル = null;
					}

				}
				catch ( CTextureCreateFailedException e )
				{
					Trace.TraceError( e.ToString() );
					Trace.TraceError( "テクスチャの生成に失敗しました。({0})", new object[] { this.strSTAGEFILE } );
					this.txタイトル = null;
					this.txサブタイトル = null;
					this.tx背景 = null;
				}

				base.Activate();
			}
			finally
			{
				Trace.TraceInformation( "曲読み込みステージの活性化を完了しました。" );
				Trace.Unindent();
			}
		}
		public override void DeActivate()
		{
			Trace.TraceInformation( "曲読み込みステージを非活性化します。" );
			Trace.Indent();
			try
			{
				TJAPlayer3.tテクスチャの解放( ref this.txタイトル );
				//CDTXMania.tテクスチャの解放( ref this.txSongnamePlate );
				TJAPlayer3.tテクスチャの解放( ref this.txサブタイトル );
                base.DeActivate();
			}
			finally
			{
				Trace.TraceInformation( "曲読み込みステージの非活性化を完了しました。" );
				Trace.Unindent();
			}
		}
		public override void CreateManagedResource()
		{
            if( !string.IsNullOrEmpty( TJAPlayer3.ConfigIni.FontName ) )
            {
                this.pfTITLE = new CCachedFontRenderer( TJAPlayer3.ConfigIni.FontName , TJAPlayer3.Skin.SongLoading_Title_FontSize );
                this.pfSUBTITLE = new CCachedFontRenderer( TJAPlayer3.ConfigIni.FontName , TJAPlayer3.Skin.SongLoading_SubTitle_FontSize);
            }
            else
            {
                this.pfTITLE = new CCachedFontRenderer(CFontRenderer.DefaultFontName, TJAPlayer3.Skin.SongLoading_Title_FontSize);
                this.pfSUBTITLE = new CCachedFontRenderer(CFontRenderer.DefaultFontName, TJAPlayer3.Skin.SongLoading_SubTitle_FontSize);
            }
			
            if (!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName))
            {
                pfDanTitle = new CCachedFontRenderer(TJAPlayer3.ConfigIni.FontName, TJAPlayer3.Skin.Game_DanC_Title_Size);
                pfDanSubTitle = new CCachedFontRenderer(TJAPlayer3.ConfigIni.FontName, TJAPlayer3.Skin.Game_DanC_SubTitle_Size);
            }
            else
            {
                pfDanTitle = new CCachedFontRenderer(CFontRenderer.DefaultFontName, TJAPlayer3.Skin.Game_DanC_Title_Size);
                pfDanSubTitle = new CCachedFontRenderer(CFontRenderer.DefaultFontName, TJAPlayer3.Skin.Game_DanC_SubTitle_Size);
            }

			this.tx背景 = TJAPlayer3.tテクスチャの生成( this.strSTAGEFILE, false );
			//this.txSongnamePlate = CDTXMania.tテクスチャの生成( CSkin.Path( @$"Graphics{Path.DirectorySeparatorChar}6_SongnamePlate.png" ) );
			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource()
		{
            TJAPlayer3.t安全にDisposeする(ref this.pfTITLE);
            TJAPlayer3.t安全にDisposeする(ref this.pfSUBTITLE);

            pfDanTitle?.Dispose();
            pfDanSubTitle?.Dispose();

			TJAPlayer3.tテクスチャの解放( ref this.tx背景 );
			base.ReleaseManagedResource();
		}
		public override int Draw()
		{
			string str;

			if( base.IsDeActivated )
				return 0;

			#region [ 初めての進行描画 ]
			//-----------------------------
			if( base.IsFirstDraw )
			{
				Cスコア cスコア1 = TJAPlayer3.stage選曲.r確定されたスコア;
				if( this.sd読み込み音 != null )
				{
					if( TJAPlayer3.Skin.sound曲読込開始音.b排他 && ( CSkin.Cシステムサウンド.r最後に再生した排他システムサウンド != null ) )
					{
						CSkin.Cシステムサウンド.r最後に再生した排他システムサウンド.t停止する();
					}
					this.sd読み込み音.PlayStart();
					this.nBGM再生開始時刻 = SoundManager.PlayTimer.NowTime;
					this.nBGMの総再生時間ms = this.sd読み込み音.TotalPlayTime;
				}
				else
				{
					TJAPlayer3.Skin.sound曲読込開始音.t再生する();
					this.nBGM再生開始時刻 = SoundManager.PlayTimer.NowTime;
					this.nBGMの総再生時間ms = TJAPlayer3.Skin.sound曲読込開始音.n長さ_現在のサウンド;
				}
				//this.actFI.tフェードイン開始();							// #27787 2012.3.10 yyagi 曲読み込み画面のフェードインの省略
				base.eフェーズID = CStage.Eフェーズ.共通_フェードイン;
				base.IsFirstDraw = false;

				nWAVcount = 1;
			}
			//-----------------------------
			#endregion
            this.ct待機.Tick();

			#region [ Cancel loading with esc ]
			if ( tキー入力() )
			{
				if ( this.sd読み込み音 != null )
				{
					this.sd読み込み音.tStopSound();
					this.sd読み込み音.tDispose();
				}
				return (int) E曲読込画面の戻り値.読込中止;
			}
			#endregion

            if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan)
			{
				void drawPlate()
                {
					if (TJAPlayer3.Tx.SongLoading_Plate != null)
					{
						TJAPlayer3.Tx.SongLoading_Plate.bスクリーン合成 = TJAPlayer3.Skin.SongLoading_Plate_ScreenBlend; //あまりにも出番が無い
						TJAPlayer3.Tx.SongLoading_Plate.Opacity = 255;
						if (TJAPlayer3.Skin.SongLoading_Plate_ReferencePoint == CSkin.ReferencePoint.Left)
						{
							TJAPlayer3.Tx.SongLoading_Plate.t2D描画(TJAPlayer3.Skin.SongLoading_Plate_X, TJAPlayer3.Skin.SongLoading_Plate_Y - (TJAPlayer3.Tx.SongLoading_Plate.sz画像サイズ.Height / 2));
						}
						else if (TJAPlayer3.Skin.SongLoading_Plate_ReferencePoint == CSkin.ReferencePoint.Right)
						{
							TJAPlayer3.Tx.SongLoading_Plate.t2D描画(TJAPlayer3.Skin.SongLoading_Plate_X - TJAPlayer3.Tx.SongLoading_Plate.sz画像サイズ.Width, TJAPlayer3.Skin.SongLoading_Plate_Y - (TJAPlayer3.Tx.SongLoading_Plate.sz画像サイズ.Height / 2));
						}
						else
						{
							TJAPlayer3.Tx.SongLoading_Plate.t2D描画(TJAPlayer3.Skin.SongLoading_Plate_X - (TJAPlayer3.Tx.SongLoading_Plate.sz画像サイズ.Width / 2), TJAPlayer3.Skin.SongLoading_Plate_Y - (TJAPlayer3.Tx.SongLoading_Plate.sz画像サイズ.Height / 2));
						}
					}
					//CDTXMania.act文字コンソール.tPrint( 0, 16, C文字コンソール.Eフォント種別.灰, C変換.nParsentTo255( ( this.ct曲名表示.n現在の値 / 30.0 ) ).ToString() );


					int y = 720 - 45;
					if (this.txタイトル != null)
					{
						int nサブタイトル補正 = string.IsNullOrEmpty(TJAPlayer3.stage選曲.r確定されたスコア.譜面情報.strサブタイトル) ? 15 : 0;

						this.txタイトル.Opacity = 255;
						if (TJAPlayer3.Skin.SongLoading_Title_ReferencePoint == CSkin.ReferencePoint.Left)
						{
							this.txタイトル.t2D描画(TJAPlayer3.Skin.SongLoading_Title_X, TJAPlayer3.Skin.SongLoading_Title_Y - (this.txタイトル.sz画像サイズ.Height / 2) + nサブタイトル補正);
						}
						else if (TJAPlayer3.Skin.SongLoading_Title_ReferencePoint == CSkin.ReferencePoint.Right)
						{
							this.txタイトル.t2D描画(TJAPlayer3.Skin.SongLoading_Title_X - (this.txタイトル.sz画像サイズ.Width * txタイトル.vc拡大縮小倍率.X), TJAPlayer3.Skin.SongLoading_Title_Y - (this.txタイトル.sz画像サイズ.Height / 2) + nサブタイトル補正);
						}
						else
						{
							this.txタイトル.t2D描画((TJAPlayer3.Skin.SongLoading_Title_X - ((this.txタイトル.sz画像サイズ.Width * txタイトル.vc拡大縮小倍率.X) / 2)), TJAPlayer3.Skin.SongLoading_Title_Y - (this.txタイトル.sz画像サイズ.Height / 2) + nサブタイトル補正);
						}
					}
					if (this.txサブタイトル != null)
					{
						this.txサブタイトル.Opacity = 255;
						if (TJAPlayer3.Skin.SongLoading_SubTitle_ReferencePoint == CSkin.ReferencePoint.Left)
						{
							this.txサブタイトル.t2D描画(TJAPlayer3.Skin.SongLoading_SubTitle_X, TJAPlayer3.Skin.SongLoading_SubTitle_Y - (this.txサブタイトル.sz画像サイズ.Height / 2));
						}
						else if (TJAPlayer3.Skin.SongLoading_Title_ReferencePoint == CSkin.ReferencePoint.Right)
						{
							this.txサブタイトル.t2D描画(TJAPlayer3.Skin.SongLoading_SubTitle_X - (this.txサブタイトル.sz画像サイズ.Width * txタイトル.vc拡大縮小倍率.X), TJAPlayer3.Skin.SongLoading_SubTitle_Y - (this.txサブタイトル.sz画像サイズ.Height / 2));
						}
						else
						{
							this.txサブタイトル.t2D描画((TJAPlayer3.Skin.SongLoading_SubTitle_X - ((this.txサブタイトル.sz画像サイズ.Width * txサブタイトル.vc拡大縮小倍率.X) / 2)), TJAPlayer3.Skin.SongLoading_SubTitle_Y - (this.txサブタイトル.sz画像サイズ.Height / 2));
						}
					}
				}

				void drawPlate_AI()
				{
					if (TJAPlayer3.Tx.SongLoading_Plate_AI != null)
					{
						TJAPlayer3.Tx.SongLoading_Plate_AI.bスクリーン合成 = TJAPlayer3.Skin.SongLoading_Plate_ScreenBlend; //あまりにも出番が無い
						TJAPlayer3.Tx.SongLoading_Plate_AI.Opacity = 255;
						if (TJAPlayer3.Skin.SongLoading_Plate_ReferencePoint == CSkin.ReferencePoint.Left)
						{
							TJAPlayer3.Tx.SongLoading_Plate_AI.t2D描画(TJAPlayer3.Skin.SongLoading_Plate_X_AI, TJAPlayer3.Skin.SongLoading_Plate_Y_AI - (TJAPlayer3.Tx.SongLoading_Plate_AI.sz画像サイズ.Height / 2));
						}
						else if (TJAPlayer3.Skin.SongLoading_Plate_ReferencePoint == CSkin.ReferencePoint.Right)
						{
							TJAPlayer3.Tx.SongLoading_Plate_AI.t2D描画(TJAPlayer3.Skin.SongLoading_Plate_X_AI - TJAPlayer3.Tx.SongLoading_Plate_AI.sz画像サイズ.Width, TJAPlayer3.Skin.SongLoading_Plate_Y_AI - (TJAPlayer3.Tx.SongLoading_Plate_AI.sz画像サイズ.Height / 2));
						}
						else
						{
							TJAPlayer3.Tx.SongLoading_Plate_AI.t2D描画(TJAPlayer3.Skin.SongLoading_Plate_X_AI - (TJAPlayer3.Tx.SongLoading_Plate_AI.sz画像サイズ.Width / 2), TJAPlayer3.Skin.SongLoading_Plate_Y_AI - (TJAPlayer3.Tx.SongLoading_Plate_AI.sz画像サイズ.Height / 2));
						}
					}
					//CDTXMania.act文字コンソール.tPrint( 0, 16, C文字コンソール.Eフォント種別.灰, C変換.nParsentTo255( ( this.ct曲名表示.n現在の値 / 30.0 ) ).ToString() );


					int y = 720 - 45;
					if (this.txタイトル != null)
					{
						int nサブタイトル補正 = string.IsNullOrEmpty(TJAPlayer3.stage選曲.r確定されたスコア.譜面情報.strサブタイトル) ? 15 : 0;

						this.txタイトル.Opacity = 255;
						if (TJAPlayer3.Skin.SongLoading_Title_ReferencePoint == CSkin.ReferencePoint.Left)
						{
							this.txタイトル.t2D描画(TJAPlayer3.Skin.SongLoading_Title_X_AI, TJAPlayer3.Skin.SongLoading_Title_Y_AI - (this.txタイトル.sz画像サイズ.Height / 2) + nサブタイトル補正);
						}
						else if (TJAPlayer3.Skin.SongLoading_Title_ReferencePoint == CSkin.ReferencePoint.Right)
						{
							this.txタイトル.t2D描画(TJAPlayer3.Skin.SongLoading_Title_X_AI - (this.txタイトル.sz画像サイズ.Width * txタイトル.vc拡大縮小倍率.X), TJAPlayer3.Skin.SongLoading_Title_Y_AI - (this.txタイトル.sz画像サイズ.Height / 2) + nサブタイトル補正);
						}
						else
						{
							this.txタイトル.t2D描画((TJAPlayer3.Skin.SongLoading_Title_X_AI - ((this.txタイトル.sz画像サイズ.Width * txタイトル.vc拡大縮小倍率.X) / 2)), TJAPlayer3.Skin.SongLoading_Title_Y_AI - (this.txタイトル.sz画像サイズ.Height / 2) + nサブタイトル補正);
						}
					}
					if (this.txサブタイトル != null)
					{
						this.txサブタイトル.Opacity = 255;
						if (TJAPlayer3.Skin.SongLoading_SubTitle_ReferencePoint == CSkin.ReferencePoint.Left)
						{
							this.txサブタイトル.t2D描画(TJAPlayer3.Skin.SongLoading_SubTitle_X_AI, TJAPlayer3.Skin.SongLoading_SubTitle_Y_AI - (this.txサブタイトル.sz画像サイズ.Height / 2));
						}
						else if (TJAPlayer3.Skin.SongLoading_Title_ReferencePoint == CSkin.ReferencePoint.Right)
						{
							this.txサブタイトル.t2D描画(TJAPlayer3.Skin.SongLoading_SubTitle_X_AI - (this.txサブタイトル.sz画像サイズ.Width * txタイトル.vc拡大縮小倍率.X), TJAPlayer3.Skin.SongLoading_SubTitle_Y_AI - (this.txサブタイトル.sz画像サイズ.Height / 2));
						}
						else
						{
							this.txサブタイトル.t2D描画((TJAPlayer3.Skin.SongLoading_SubTitle_X_AI - ((this.txサブタイトル.sz画像サイズ.Width * txサブタイトル.vc拡大縮小倍率.X) / 2)), TJAPlayer3.Skin.SongLoading_SubTitle_Y_AI - (this.txサブタイトル.sz画像サイズ.Height / 2));
						}
					}
				}

				#region [ Loading screen (except dan) ]
				//-----------------------------
				this.ct曲名表示.Tick();

				if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
				{
					#region [Tower loading screen]

					if (TJAPlayer3.Skin.Game_Tower_Ptn_Result > 0)
					{
						int xFactor = 0;
						float yFactor = 1f;

						int currentTowerType = TJAPlayer3.stage選曲.r確定された曲.arスコア[5].譜面情報.nTowerType;

						if (currentTowerType < 0 || currentTowerType >= TJAPlayer3.Skin.Game_Tower_Ptn_Result)
							currentTowerType = 0;

						if (TJAPlayer3.Tx.TowerResult_Background != null && TJAPlayer3.Tx.TowerResult_Tower[currentTowerType] != null)
						{
							xFactor = (TJAPlayer3.Tx.TowerResult_Background.szテクスチャサイズ.Width - TJAPlayer3.Tx.TowerResult_Tower[currentTowerType].szテクスチャサイズ.Width) / 2;
							yFactor = TJAPlayer3.Tx.TowerResult_Tower[currentTowerType].szテクスチャサイズ.Height / (float)TJAPlayer3.Tx.TowerResult_Background.szテクスチャサイズ.Height;
						}

						float pos = (TJAPlayer3.Tx.TowerResult_Background.szテクスチャサイズ.Height - TJAPlayer3.Skin.Resolution[1]) -
							((ct待機.CurrentValue <= 1200 ? ct待機.CurrentValue / 10f : 120) / 120f * (TJAPlayer3.Tx.TowerResult_Background.szテクスチャサイズ.Height - TJAPlayer3.Skin.Resolution[1]));

						TJAPlayer3.Tx.TowerResult_Background?.t2D描画(0, -1 * pos);
						TJAPlayer3.Tx.TowerResult_Tower[currentTowerType]?.t2D描画(xFactor, -1 * yFactor * pos);
					}

					#endregion
					drawPlate();
				}
				else if (TJAPlayer3.ConfigIni.bAIBattleMode)
				{
					TJAPlayer3.Tx.SongLoading_Bg_AI_Wait.t2D描画(0, 0);
					drawPlate_AI();
				}
				else
				{
					#region [Ensou loading screen]

					if (TJAPlayer3.Tx.SongLoading_BgWait != null) TJAPlayer3.Tx.SongLoading_BgWait.t2D描画(0, 0);
					if (TJAPlayer3.Tx.SongLoading_Chara != null) TJAPlayer3.Tx.SongLoading_Chara.t2D描画(0, 0);

					drawPlate();

					#endregion
				}

				//CDTXMania.act文字コンソール.tPrint( 0, 0, C文字コンソール.Eフォント種別.灰, this.ct曲名表示.n現在の値.ToString() );

				//-----------------------------
				#endregion
			}
            else
            {
				#region [ Dan Loading screen　]

				TJAPlayer3.Tx.SongLoading_Bg_Dan.t2D描画(0, 0 - (ct待機.CurrentValue <= 600 ? ct待機.CurrentValue / 10f : 60));

				CTexture dp = (TJAPlayer3.stage段位選択.段位リスト.stバー情報 != null)
					? TJAPlayer3.stage段位選択.段位リスト.stバー情報[TJAPlayer3.stage段位選択.段位リスト.n現在の選択行].txDanPlate
					: null;

				CActSelect段位リスト.tDisplayDanPlate(dp,
					null, 
					TJAPlayer3.Skin.SongLoading_DanPlate[0],
					TJAPlayer3.Skin.SongLoading_DanPlate[1]);

				if (TJAPlayer3.Tx.Tile_Black != null)
				{
					TJAPlayer3.Tx.Tile_Black.Opacity = (int)(ct待機.CurrentValue <= 51 ? (255 - ct待機.CurrentValue / 0.2f) : (this.ct待機.CurrentValue - 949) / 0.2);
					for (int i = 0; i <= (SampleFramework.GameWindowSize.Width / TJAPlayer3.Tx.Tile_Black.szテクスチャサイズ.Width); i++)      // #23510 2010.10.31 yyagi: change "clientSize.Width" to "640" to fix FIFO drawing size
					{
						for (int j = 0; j <= (SampleFramework.GameWindowSize.Height / TJAPlayer3.Tx.Tile_Black.szテクスチャサイズ.Height); j++) // #23510 2010.10.31 yyagi: change "clientSize.Height" to "480" to fix FIFO drawing size
						{
							TJAPlayer3.Tx.Tile_Black.t2D描画(i * TJAPlayer3.Tx.Tile_Black.szテクスチャサイズ.Width, j * TJAPlayer3.Tx.Tile_Black.szテクスチャサイズ.Height);
						}
					}
				}

				#endregion
			}

            switch ( base.eフェーズID )
			{
				case CStage.Eフェーズ.共通_フェードイン:
					//if( this.actFI.On進行描画() != 0 )			    // #27787 2012.3.10 yyagi 曲読み込み画面のフェードインの省略
																		// 必ず一度「CStaeg.Eフェーズ.共通_フェードイン」フェーズを経由させること。
																		// さもないと、曲読み込みが完了するまで、曲読み込み画面が描画されない。 
						base.eフェーズID = CStage.Eフェーズ.NOWLOADING_DTXファイルを読み込む;
					return (int) E曲読込画面の戻り値.継続;

				case CStage.Eフェーズ.NOWLOADING_DTXファイルを読み込む:
					{
						timeBeginLoad = DateTime.Now;
						TimeSpan span;
						str = null;
						if( !TJAPlayer3.bコンパクトモード )
							str = TJAPlayer3.stage選曲.r確定されたスコア.ファイル情報.ファイルの絶対パス;
						else
							str = TJAPlayer3.strコンパクトモードファイル;

						CScoreIni ini = new CScoreIni( str + ".score.ini" );
						ini.t全演奏記録セクションの整合性をチェックし不整合があればリセットする();

						if( ( TJAPlayer3.DTX != null ) && TJAPlayer3.DTX.IsActivated )
							TJAPlayer3.DTX.DeActivate();

                        //if( CDTXMania.DTX == null )
                        {
							TJAPlayer3.DTX = new CDTX(str, false, 1.0, ini.stファイル.BGMAdjust, 0, 0, true, TJAPlayer3.stage選曲.n確定された曲の難易度[0]);
							if ( TJAPlayer3.ConfigIni.nPlayerCount >= 2 )
								TJAPlayer3.DTX_2P = new CDTX(str, false, 1.0, ini.stファイル.BGMAdjust, 0, 1, true, TJAPlayer3.stage選曲.n確定された曲の難易度[1]);
							if (TJAPlayer3.ConfigIni.nPlayerCount >= 3)
								TJAPlayer3.DTX_3P = new CDTX(str, false, 1.0, ini.stファイル.BGMAdjust, 0, 2, true, TJAPlayer3.stage選曲.n確定された曲の難易度[2]);
							if (TJAPlayer3.ConfigIni.nPlayerCount >= 4)
								TJAPlayer3.DTX_4P = new CDTX(str, false, 1.0, ini.stファイル.BGMAdjust, 0, 3, true, TJAPlayer3.stage選曲.n確定された曲の難易度[3]);
							if (TJAPlayer3.ConfigIni.nPlayerCount >= 5)
								TJAPlayer3.DTX_5P = new CDTX(str, false, 1.0, ini.stファイル.BGMAdjust, 0, 4, true, TJAPlayer3.stage選曲.n確定された曲の難易度[4]);

							if (TJAPlayer3.DTX.listErrors.Count != 0)
                            {
								string message = "";
								foreach (var text in TJAPlayer3.DTX.listErrors)
								{
									TJAPlayer3.VisualLogManager.PushCard(CVisualLogManager.ELogCardType.LogError, text);
									//System.Windows.Forms.MessageBox.Show(text, "譜面にエラーが見つかりました");
								}
							}

							Trace.TraceInformation( "---- Song information -----------------" );
				    		Trace.TraceInformation( "TITLE: {0}", TJAPlayer3.DTX.TITLE );
			    			Trace.TraceInformation( "FILE: {0}",  TJAPlayer3.DTX.strファイル名の絶対パス );
		    				Trace.TraceInformation( "---------------------------" );

	    					span = (TimeSpan) ( DateTime.Now - timeBeginLoad );
    						Trace.TraceInformation( "Chart loading time:           {0}", span.ToString() );

							// 段位認定モード用。
							#region [dan setup]
							if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan && TJAPlayer3.DTX.List_DanSongs != null)
                            {

                                var titleForeColor = TJAPlayer3.Skin.Game_DanC_Title_ForeColor;
                                var titleBackColor = TJAPlayer3.Skin.Game_DanC_Title_BackColor;
                                var subtitleForeColor = TJAPlayer3.Skin.Game_DanC_SubTitle_ForeColor;
                                var subtitleBackColor = TJAPlayer3.Skin.Game_DanC_SubTitle_BackColor;

                                for (int i = 0; i < TJAPlayer3.DTX.List_DanSongs.Count; i++)
                                {
                                    if (!string.IsNullOrEmpty(TJAPlayer3.DTX.List_DanSongs[i].Title))
                                    {
                                        using (var bmpSongTitle = pfDanTitle.DrawText(TJAPlayer3.DTX.List_DanSongs[i].Title, titleForeColor, titleBackColor, null, 30))
                                        {
                                            TJAPlayer3.DTX.List_DanSongs[i].TitleTex = TJAPlayer3.tテクスチャの生成(bmpSongTitle, false);
                                            TJAPlayer3.DTX.List_DanSongs[i].TitleTex.vc拡大縮小倍率.X = TJAPlayer3.GetSongNameXScaling(ref TJAPlayer3.DTX.List_DanSongs[i].TitleTex, TJAPlayer3.Skin.Game_DanC_Title_MaxWidth);
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(TJAPlayer3.DTX.List_DanSongs[i].SubTitle))
                                    {
                                        using (var bmpSongSubTitle = pfDanSubTitle.DrawText(TJAPlayer3.DTX.List_DanSongs[i].SubTitle, subtitleForeColor, subtitleBackColor, null, 30))
                                        {
                                            TJAPlayer3.DTX.List_DanSongs[i].SubTitleTex = TJAPlayer3.tテクスチャの生成(bmpSongSubTitle, false);
                                            TJAPlayer3.DTX.List_DanSongs[i].SubTitleTex.vc拡大縮小倍率.X = TJAPlayer3.GetSongNameXScaling(ref TJAPlayer3.DTX.List_DanSongs[i].SubTitleTex, TJAPlayer3.Skin.Game_DanC_SubTitle_MaxWidth);
                                        }
                                    }

                                }
                            }
							#endregion
						}

                        base.eフェーズID = CStage.Eフェーズ.NOWLOADING_WAV読み込み待機;
						timeBeginLoadWAV = DateTime.Now;
						return (int) E曲読込画面の戻り値.継続;
					}

                case CStage.Eフェーズ.NOWLOADING_WAV読み込み待機:
                    {
                        if( this.ct待機.CurrentValue > 260 )
                        {
						    base.eフェーズID = CStage.Eフェーズ.NOWLOADING_WAVファイルを読み込む;
                        }
						return (int) E曲読込画面の戻り値.継続;
                    }

				case CStage.Eフェーズ.NOWLOADING_WAVファイルを読み込む:
					{
						int looptime = (TJAPlayer3.ConfigIni.b垂直帰線待ちを行う)? 3 : 1;	// VSyncWait=ON時は1frame(1/60s)あたり3つ読むようにする
						for ( int i = 0; i < looptime && nWAVcount <= TJAPlayer3.DTX.listWAV.Count; i++ )
						{
							if ( TJAPlayer3.DTX.listWAV[ nWAVcount ].listこのWAVを使用するチャンネル番号の集合.Count > 0 )	// #28674 2012.5.8 yyagi
							{
								TJAPlayer3.DTX.tWAVの読み込み( TJAPlayer3.DTX.listWAV[ nWAVcount ] );
							}
							nWAVcount++;
						}
						if ( nWAVcount > TJAPlayer3.DTX.listWAV.Count )
						{
							TimeSpan span = ( TimeSpan ) ( DateTime.Now - timeBeginLoadWAV );
							Trace.TraceInformation("Song loading time({0,4}):     {1}", TJAPlayer3.DTX.listWAV.Count, span.ToString() );
							timeBeginLoadWAV = DateTime.Now;

							if ( TJAPlayer3.ConfigIni.bDynamicBassMixerManagement )
							{
								TJAPlayer3.DTX.PlanToAddMixerChannel();
							}

							var _dtx = new CDTX[5]{ TJAPlayer3.DTX, TJAPlayer3.DTX_2P, TJAPlayer3.DTX_3P, TJAPlayer3.DTX_4P, TJAPlayer3.DTX_5P };
							
							for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                            {
								_dtx[i]?.tRandomizeTaikoChips(i);
								_dtx[i]?.tApplyFunMods(i);
								TJAPlayer3.ReplayInstances[i] = new CSongReplay(_dtx[i].strファイル名の絶対パス, i);
							}
							CDTX.tManageKusudama(_dtx);

							TJAPlayer3.stage演奏ドラム画面.Activate();

							span = (TimeSpan) ( DateTime.Now - timeBeginLoadWAV );

							base.eフェーズID = CStage.Eフェーズ.NOWLOADING_BMPファイルを読み込む;
						}
						return (int) E曲読込画面の戻り値.継続;
					}

				case CStage.Eフェーズ.NOWLOADING_BMPファイルを読み込む:
					{
						TimeSpan span;
						DateTime timeBeginLoadBMPAVI = DateTime.Now;

						if ( TJAPlayer3.ConfigIni.bAVI有効 )
							TJAPlayer3.DTX.tAVIの読み込み();
						span = ( TimeSpan ) ( DateTime.Now - timeBeginLoadBMPAVI );

						span = ( TimeSpan ) ( DateTime.Now - timeBeginLoad );
						Trace.TraceInformation( "総読込時間:                {0}", span.ToString() );

                        if(TJAPlayer3.ConfigIni.FastRender)
                        {
                            var fastRender = new FastRender();
                            fastRender.Render();
                            fastRender = null;
                        }


						TJAPlayer3.Timer.Update();
                        //CSound管理.rc演奏用タイマ.t更新();
						base.eフェーズID = CStage.Eフェーズ.NOWLOADING_システムサウンドBGMの完了を待つ;
						return (int) E曲読込画面の戻り値.継続;
					}

				case CStage.Eフェーズ.NOWLOADING_システムサウンドBGMの完了を待つ:
					{
						long nCurrentTime = TJAPlayer3.Timer.NowTime;
						if( nCurrentTime < this.nBGM再生開始時刻 )
							this.nBGM再生開始時刻 = nCurrentTime;

//						if ( ( nCurrentTime - this.nBGM再生開始時刻 ) > ( this.nBGMの総再生時間ms - 1000 ) )
						if ( ( nCurrentTime - this.nBGM再生開始時刻 ) >= ( this.nBGMの総再生時間ms ) )	// #27787 2012.3.10 yyagi 1000ms == フェードイン分の時間
						{
							base.eフェーズID = CStage.Eフェーズ.共通_フェードアウト;
						}
						return (int) E曲読込画面の戻り値.継続;
					}

				case CStage.Eフェーズ.共通_フェードアウト:
					if ( this.ct待機.IsUnEnded )		// DTXVモード時は、フェードアウト省略
						return (int)E曲読込画面の戻り値.継続;

					if ( this.sd読み込み音 != null )
					{
						this.sd読み込み音.tDispose();
					}
					return (int) E曲読込画面の戻り値.読込完了;
			}
			return (int) E曲読込画面の戻り値.継続;
		}

		/// <summary>
		/// ESC押下時、trueを返す
		/// </summary>
		/// <returns></returns>
		protected bool tキー入力()
		{
			IInputDevice keyboard = TJAPlayer3.Input管理.Keyboard;
			if 	( keyboard.KeyPressed( (int)SlimDXKeys.Key.Escape ) )		// escape (exit)
			{
				return true;
			}
			return false;
		}

		// その他

		#region [ private ]
		//-----------------
		//private CActFIFOBlack actFI;
		//private CActFIFOBlack actFO;
		private long nBGMの総再生時間ms;
		private long nBGM再生開始時刻;
		private CSound sd読み込み音;
		private string strSTAGEFILE;
		private string str曲タイトル;
        private string strサブタイトル;
		private CTexture txタイトル;
        private CTexture txサブタイトル;
		private CTexture tx背景;
        //private CTexture txSongnamePlate;
		private DateTime timeBeginLoad;
		private DateTime timeBeginLoadWAV;
		private int nWAVcount;
        private CCounter ct待機;
        private CCounter ct曲名表示;

        private CCachedFontRenderer pfTITLE;
        private CCachedFontRenderer pfSUBTITLE;

        private CCachedFontRenderer pfDanTitle = null;
        private CCachedFontRenderer pfDanSubTitle = null;
		//-----------------
		#endregion
	}
}
