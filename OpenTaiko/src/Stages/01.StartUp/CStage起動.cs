using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.IO;
using FDK;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Input;

namespace TJAPlayer3
{
	internal class CStage起動 : CStage
	{
		// コンストラクタ

		public CStage起動()
		{
			base.eStageID = CStage.EStage.StartUp;
			base.IsDeActivated = true;
		}

		public List<string> list進行文字列;

		// CStage 実装

		public override void Activate()
		{
			Trace.TraceInformation( "起動ステージを活性化します。" );
			Trace.Indent();
			try
			{
				Background = new ScriptBG(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.STARTUP}Script.lua"));
				Background.Init();

				this.list進行文字列 = new List<string>();
				base.ePhaseID = CStage.EPhase.Common_NORMAL;
				base.Activate();
				Trace.TraceInformation( "起動ステージの活性化を完了しました。" );
			}
			finally
			{
				Trace.Unindent();
			}
		}
		public override void DeActivate()
		{
			Trace.TraceInformation( "起動ステージを非活性化します。" );
			Trace.Indent();
			try
			{
				TJAPlayer3.t安全にDisposeする(ref Background);

				this.list進行文字列 = null;
				if ( es != null )
				{
					if ( ( es.thDTXFileEnumerate != null ) && es.thDTXFileEnumerate.IsAlive )
					{
						Trace.TraceWarning( "リスト構築スレッドを強制停止します。" );
						es.thDTXFileEnumerate.Abort();
						es.thDTXFileEnumerate.Join();
					}
				}
				base.DeActivate();
				Trace.TraceInformation( "起動ステージの非活性化を完了しました。" );
			}
			finally
			{
				Trace.Unindent();
			}
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
			if( !base.IsDeActivated )
			{
				if( base.IsFirstDraw )
				{
					this.list進行文字列.Add( "DTXManiaXG Ver.K powered by YAMAHA Silent Session Drums\n" );
					this.list進行文字列.Add( "Product by.kairera0467\n" );
					this.list進行文字列.Add( "Release: " + TJAPlayer3.VERSION + " [" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + "]" );

                    this.list進行文字列.Add("");
                    this.list進行文字列.Add("TJAPlayer3-Develop-ReWrite forked TJAPlayer3(@aioilight)");
					this.list進行文字列.Add("OpenTaiko forked TJAPlayer3-Develop-ReWrite(@TouhouRenren)");
					this.list進行文字列.Add("OpenTaiko edited by 0AuBSQ");
                    this.list進行文字列.Add("");

                    es = new CEnumSongs();
					es.StartEnumFromCache();										// 曲リスト取得(別スレッドで実行される)
					base.IsFirstDraw = false;
					return 0;
				}

				// CSongs管理 s管理 = CDTXMania.Songs管理;

				Background.Update();
				Background.Draw();

				#region [ this.str現在進行中 の決定 ]
				//-----------------
				switch( base.ePhaseID )
				{
					case CStage.EPhase.Startup_0_CreateSystemSound:
						this.str現在進行中 = "SYSTEM SOUND...";
						break;

					case CStage.EPhase.Startup_1_InitializeSonglist:
						this.str現在進行中 = "SONG LIST...";
						break;

					case CStage.EPhase.Startup_2_EnumerateSongs:
						this.str現在進行中 = string.Format( "{0} ... {1}", "Enumerating songs", es.Songs管理.n検索されたスコア数 );
						break;

					case CStage.EPhase.Startup_3_ApplyScoreCache:
						this.str現在進行中 = string.Format( "{0} ... {1}/{2}", "Loading score properties from songs.db", es.Songs管理.nスコアキャッシュから反映できたスコア数, es.Songs管理.n検索されたスコア数 );
						break;

					case CStage.EPhase.Startup_4_LoadSongsNotSeenInScoreCacheAndApplyThem:
						this.str現在進行中 = string.Format( "{0} ... {1}/{2}", "Loading score properties from files", es.Songs管理.nファイルから反映できたスコア数, es.Songs管理.n検索されたスコア数 - es.Songs管理.nスコアキャッシュから反映できたスコア数 );
						break;

					case CStage.EPhase.Startup_5_PostProcessSonglist:
						this.str現在進行中 = string.Format( "{0} ... ", "Building songlists" );
						break;

					case CStage.EPhase.Startup_6_LoadTextures:
						if (!bIsLoadingTextures)
						{
							void loadTexture()
							{
								this.list進行文字列.Add("LOADING TEXTURES...");

								try
								{
									TJAPlayer3.Tx.LoadTexture();
									
									this.list進行文字列.Add("LOADING TEXTURES...OK");
									this.str現在進行中 = "Setup done.";
									this.ePhaseID = EPhase.Startup_Complete;
									TJAPlayer3.Skin.bgm起動画面.t停止する();
								}
								catch(Exception exception)
								{
									TJAPlayer3.Skin.bgm起動画面.t停止する();

									Trace.TraceError( exception.ToString() );
									this.list進行文字列.Add("LOADING TEXTURES...NG");
									foreach(var text in exception.ToString().Split('\n'))
									{
										this.list進行文字列.Add(text);
									}
								}

								this.list進行文字列.Add("LOADING TEXTURES...OK");
								this.str現在進行中 = "Setup done.";
								this.ePhaseID = EPhase.Startup_Complete;
								TJAPlayer3.Skin.bgm起動画面.t停止する();
							}
							if (TJAPlayer3.ConfigIni.ASyncTextureLoad)
							{
								Task.Run(loadTexture);
							}
							else 
							{
								loadTexture();
							}
						}
						bIsLoadingTextures = true;
                        break;
				}
				//-----------------
				#endregion

				if(ePhaseID != EPhase.Startup_Complete)
                {
					#region [ this.list進行文字列＋this.現在進行中 の表示 ]
					//-----------------
					int x = 320;
					int y = 20;
					for(int i = 0; i < this.list進行文字列.Count; i++)
					{
						TJAPlayer3.act文字コンソール.tPrint((int)(x * TJAPlayer3.Skin.Resolution[0] / 1280.0), (int)(y * TJAPlayer3.Skin.Resolution[1] / 720.0), C文字コンソール.Eフォント種別.白, this.list進行文字列[i]);
						y += 24;
					}
					TJAPlayer3.act文字コンソール.tPrint(x, y, C文字コンソール.Eフォント種別.白, this.str現在進行中);
					//-----------------
					#endregion
				}
                else
                {
					if ( es != null && es.IsSongListEnumCompletelyDone )							// 曲リスト作成が終わったら
					{
						TJAPlayer3.Songs管理 = ( es != null ) ? es.Songs管理 : null;		// 最後に、曲リストを拾い上げる

						if(TJAPlayer3.Input管理.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return))
						{
							TJAPlayer3.Skin.sound決定音.t再生する();
							return 1;
						}
					}

					TJAPlayer3.Tx.Readme.t2D描画(0, 0);
				}

			}
			return 0;
		}


		// その他

		#region [ private ]
		//-----------------
		private string str現在進行中 = "";
		private ScriptBG Background;
		private CEnumSongs es;
		private bool bIsLoadingTextures;

#if false
		private void t曲リストの構築()
		{
			// ！注意！
			// 本メソッドは別スレッドで動作するが、プラグイン側でカレントディレクトリを変更しても大丈夫なように、
			// すべてのファイルアクセスは「絶対パス」で行うこと。(2010.9.16)

			DateTime now = DateTime.Now;
			string strPathSongsDB = CDTXMania.strEXEのあるフォルダ + "songs.db";
			string strPathSongList = CDTXMania.strEXEのあるフォルダ + "songlist.db";

			try
			{
				#region [ 0) システムサウンドの構築  ]
				//-----------------------------
				base.eフェーズID = CStage.Eフェーズ.起動0_システムサウンドを構築;

				Trace.TraceInformation( "0) システムサウンドを構築します。" );
				Trace.Indent();

				try
				{
					for( int i = 0; i < CDTXMania.Skin.nシステムサウンド数; i++ )
					{
						CSkin.Cシステムサウンド cシステムサウンド = CDTXMania.Skin[ i ];
						if( !CDTXMania.bコンパクトモード || cシステムサウンド.bCompact対象 )
						{
							try
							{
								cシステムサウンド.t読み込み();
								Trace.TraceInformation( "システムサウンドを読み込みました。({0})", new object[] { cシステムサウンド.strファイル名 } );
								if( ( cシステムサウンド == CDTXMania.Skin.bgm起動画面 ) && cシステムサウンド.b読み込み成功 )
								{
									cシステムサウンド.t再生する();
								}
							}
							catch( FileNotFoundException )
							{
								Trace.TraceWarning( "システムサウンドが存在しません。({0})", new object[] { cシステムサウンド.strファイル名 } );
							}
							catch( Exception exception )
							{
								Trace.TraceError( exception.Message );
								Trace.TraceWarning( "システムサウンドの読み込みに失敗しました。({0})", new object[] { cシステムサウンド.strファイル名 } );
							}
						}
					}
					lock( this.list進行文字列 )
					{
						this.list進行文字列.Add( "Loading system sounds ... OK " );
					}
				}
				finally
				{
					Trace.Unindent();
				}
				//-----------------------------
				#endregion

				if( CDTXMania.bコンパクトモード )
				{
					Trace.TraceInformation( "コンパクトモードなので残りの起動処理は省略します。" );
					return;
				}

				#region [ 00) songlist.dbの読み込みによる曲リストの構築  ]
				//-----------------------------
				base.eフェーズID = CStage.Eフェーズ.起動00_songlistから曲リストを作成する;

				Trace.TraceInformation( "1) songlist.dbを読み込みます。" );
				Trace.Indent();

				try
				{
					if ( !CDTXMania.ConfigIni.bConfigIniがないかDTXManiaのバージョンが異なる )
					{
						try
						{
							CDTXMania.Songs管理.tSongListDBを読み込む( strPathSongList );
						}
						catch
						{
							Trace.TraceError( "songlist.db の読み込みに失敗しました。" );
						}

						int scores = ( CDTXMania.Songs管理 == null ) ? 0 : CDTXMania.Songs管理.n検索されたスコア数;		// 読み込み途中でアプリ終了した場合など、CDTXMania.Songs管理 がnullの場合があるので注意
						Trace.TraceInformation( "songlist.db の読み込みを完了しました。[{0}スコア]", scores );
						lock ( this.list進行文字列 )
						{
							this.list進行文字列.Add( "Loading songlist.db ... OK" );
						}
					}
					else
					{
						Trace.TraceInformation( "初回の起動であるかまたはDTXManiaのバージョンが上がったため、songlist.db の読み込みをスキップします。" );
						lock ( this.list進行文字列 )
						{
							this.list進行文字列.Add( "Loading songlist.db ... Skip" );
						}
					}
				}
				finally
				{
					Trace.Unindent();
				}

				#endregion

				#region [ 1) songs.db の読み込み ]
				//-----------------------------
				base.eフェーズID = CStage.Eフェーズ.起動1_SongsDBからスコアキャッシュを構築;

				Trace.TraceInformation( "2) songs.db を読み込みます。" );
				Trace.Indent();

				try
				{
					if ( !CDTXMania.ConfigIni.bConfigIniがないかDTXManiaのバージョンが異なる )
					{
						try
						{
							CDTXMania.Songs管理.tSongsDBを読み込む( strPathSongsDB );
						}
						catch
						{
							Trace.TraceError( "songs.db の読み込みに失敗しました。" );
						}

						int scores = ( CDTXMania.Songs管理 == null ) ? 0 : CDTXMania.Songs管理.nSongsDBから取得できたスコア数;	// 読み込み途中でアプリ終了した場合など、CDTXMania.Songs管理 がnullの場合があるので注意
						Trace.TraceInformation( "songs.db の読み込みを完了しました。[{0}スコア]", scores );
						lock ( this.list進行文字列 )
						{
							this.list進行文字列.Add( "Loading songs.db ... OK" );
						}
					}
					else
					{
						Trace.TraceInformation( "初回の起動であるかまたはDTXManiaのバージョンが上がったため、songs.db の読み込みをスキップします。" );
						lock ( this.list進行文字列 )
						{
							this.list進行文字列.Add( "Loading songs.db ... Skip" );
						}
					}
				}
				finally
				{
					Trace.Unindent();
				}
				//-----------------------------
				#endregion

			}
			finally
			{
				base.eフェーズID = CStage.Eフェーズ.起動7_完了;
				TimeSpan span = (TimeSpan) ( DateTime.Now - now );
				Trace.TraceInformation( "起動所要時間: {0}", new object[] { span.ToString() } );
			}
		}
#endif
		#endregion
	}
}
