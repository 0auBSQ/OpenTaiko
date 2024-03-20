using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;

namespace TJAPlayer3
{
	internal class CEnumSongs							// #27060 2011.2.7 yyagi 曲リストを取得するクラス
	{													// ファイルキャッシュ(songslist.db)からの取得と、ディスクからの取得を、この一つのクラスに集約。

		public CSongs管理 Songs管理						// 曲の探索結果はこのSongs管理に読み込まれる
		{
			get;
			private set;
		}

		public bool IsSongListEnumCompletelyDone		// 曲リスト探索と、実際の曲リストへの反映が完了した？
		{
			get
			{
				return ( this.state == DTXEnumState.CompletelyDone );
			}
		}
		public bool IsEnumerating
		{
			get
			{
				if ( thDTXFileEnumerate == null )
				{
					return false;
				}
				return thDTXFileEnumerate.IsAlive;
			}
		}
		public bool IsSongListEnumerated				// 曲リスト探索が完了したが、実際の曲リストへの反映はまだ？
		{
			get
			{
				return ( this.state == DTXEnumState.Enumeratad );
			}
		}
		public bool IsSongListEnumStarted				// 曲リスト探索開始後？(探索完了も含む)
		{
			get
			{
				return ( this.state != DTXEnumState.None );
			}
		}
		public void SongListEnumCompletelyDone()
		{
			this.state = DTXEnumState.CompletelyDone;
			this.Songs管理 = null;						// GCはOSに任せる
		}
		public bool IsSlowdown							// #PREMOVIE再生中は検索負荷を落とす
		{
			get
			{
				return this.Songs管理.bIsSlowdown;
			}
			set
			{
				this.Songs管理.bIsSlowdown = value;
			}
		}

		public void ChangeEnumeratePriority( ThreadPriority tp )
		{
			if ( this.thDTXFileEnumerate != null && this.thDTXFileEnumerate.IsAlive == true )
			{
				this.thDTXFileEnumerate.Priority = tp;
			}
		}
		private readonly string strPathSongsDB = TJAPlayer3.strEXEのあるフォルダ + "songs.db";
		private readonly string strPathSongList = TJAPlayer3.strEXEのあるフォルダ + "songlist.db";

		public Thread thDTXFileEnumerate
		{
			get;
			private set;
		}
		private enum DTXEnumState
		{
			None,
			Ongoing,
			Suspended,
			Enumeratad,				// 探索完了、現在の曲リストに未反映
			CompletelyDone			// 探索完了、現在の曲リストに反映完了
		}
		private DTXEnumState state = DTXEnumState.None;


		/// <summary>
		/// Constractor
		/// </summary>
		public CEnumSongs()
		{
			this.Songs管理 = new CSongs管理();
		}

		public void Init()
		{

		}

		/// <summary>
		/// 曲リストのキャッシュ(songlist.db)取得スレッドの開始
		/// </summary>
		public void StartEnumFromCache()
		{
			this.thDTXFileEnumerate = new Thread( new ThreadStart( this.t曲リストの構築1 ) );
			this.thDTXFileEnumerate.Name = "曲リストの構築";
			this.thDTXFileEnumerate.IsBackground = true;
			this.thDTXFileEnumerate.Start();
		}

		/// <summary>
		/// 
		/// </summary>
		public delegate void AsyncDelegate();

		/// <summary>
		/// 曲検索スレッドの開始
		/// </summary>
		public void StartEnumFromDisk(bool hard_reload = false)
		{
			if ( state == DTXEnumState.None || state == DTXEnumState.CompletelyDone )
			{
				Trace.TraceInformation( "★曲データ検索スレッドを起動しました。" );
				lock ( this )
				{
					state = DTXEnumState.Ongoing;
				}
				// this.autoReset = new AutoResetEvent( true );

				if ( this.Songs管理 == null )		// Enumerating Songs完了後、CONFIG画面から再スキャンしたときにこうなる
				{
					this.Songs管理 = new CSongs管理();
				}
				if (hard_reload)
					this.thDTXFileEnumerate = new Thread( new ThreadStart( this.HardReloadSongList ) );
				else
					this.thDTXFileEnumerate = new Thread( new ThreadStart( this.ReloadSongList ) );
				this.thDTXFileEnumerate.Name = "曲リストの構築";
				this.thDTXFileEnumerate.IsBackground = true;
				this.thDTXFileEnumerate.Priority = System.Threading.ThreadPriority.Lowest;
				this.thDTXFileEnumerate.Start();
			}
		}

		private void HardReloadSongList()
		{
			this.t曲リストの構築2(true);
		}
		private void ReloadSongList()
		{
			this.t曲リストの構築2(false);
		}

		/// <summary>
		/// 曲探索スレッドのサスペンド
		/// </summary>
		public void Suspend()
		{
			if ( this.state != DTXEnumState.CompletelyDone &&
				( ( thDTXFileEnumerate.ThreadState & ( System.Threading.ThreadState.Background ) ) != 0 ) )
			{
				// this.thDTXFileEnumerate.Suspend();		// obsoleteにつき使用中止
				this.Songs管理.bIsSuspending = true;
				this.state = DTXEnumState.Suspended;
				Trace.TraceInformation( "★曲データ検索スレッドを中断しました。" );
			}
		}

		/// <summary>
		/// 曲探索スレッドのレジューム
		/// </summary>
		public void Resume()
		{
			if ( this.state == DTXEnumState.Suspended )
			{
				if ( ( this.thDTXFileEnumerate.ThreadState & ( System.Threading.ThreadState.WaitSleepJoin | System.Threading.ThreadState.StopRequested ) ) != 0 )	//
				{
					// this.thDTXFileEnumerate.Resume();	// obsoleteにつき使用中止
					this.Songs管理.bIsSuspending = false;
					this.Songs管理.AutoReset.Set();
					this.state = DTXEnumState.Ongoing;
					Trace.TraceInformation( "★曲データ検索スレッドを再開しました。" );
				}
			}
		}

		/// <summary>
		/// 曲探索スレッドにサスペンド指示を出してから、本当にサスペンド状態に遷移するまでの間、ブロックする
		/// 500ms * 10回＝5秒でタイムアウトし、サスペンド完了して無くてもブロック解除する
		/// </summary>
		public void WaitUntilSuspended()
		{
			// 曲検索が一時中断されるまで待機
			for ( int i = 0; i < 10; i++ )
			{
				if ( this.state == DTXEnumState.CompletelyDone ||
					( thDTXFileEnumerate.ThreadState & ( System.Threading.ThreadState.WaitSleepJoin | System.Threading.ThreadState.Background | System.Threading.ThreadState.Stopped ) ) != 0 )
				{
					break;
				}
				Trace.TraceInformation( "★曲データ検索スレッドの中断待ちです: {0}", this.thDTXFileEnumerate.ThreadState.ToString() );
				Thread.Sleep( 500 );
			}

		}

		/// <summary>
		/// 曲探索スレッドを強制終了する
		/// </summary>
		public void Abort()
		{
			if ( thDTXFileEnumerate != null )
			{
				thDTXFileEnumerate.Abort();
				thDTXFileEnumerate = null;
				this.state = DTXEnumState.None;

				this.Songs管理 = null;					// Songs管理を再初期化する (途中まで作った曲リストの最後に、一から重複して追記することにならないようにする。)
				this.Songs管理 = new CSongs管理();
			}
		}



		/// <summary>
		/// songlist.dbからの曲リスト構築
		/// </summary>
		public void t曲リストの構築1()
		{
			// ！注意！
			// 本メソッドは別スレッドで動作するが、プラグイン側でカレントディレクトリを変更しても大丈夫なように、
			// すべてのファイルアクセスは「絶対パス」で行うこと。(2010.9.16)
			// 構築が完了したら、DTXEnumerateState state を DTXEnumerateState.Done にすること。(2012.2.9)
			DateTime now = DateTime.Now;

			try
			{
				#region [ 0) システムサウンドの構築  ]
				//-----------------------------
				TJAPlayer3.stage起動.ePhaseID = CStage.EPhase.Startup_0_CreateSystemSound;

				Trace.TraceInformation( "0) システムサウンドを構築します。" );
				Trace.Indent();

				try
				{
					TJAPlayer3.Skin.bgm起動画面.tPlay();
					for ( int i = 0; i < TJAPlayer3.Skin.nシステムサウンド数; i++ )
					{
						if ( !TJAPlayer3.Skin[ i ].bExclusive )	// BGM系以外のみ読み込む。(BGM系は必要になったときに読み込む)
						{
							CSkin.CSystemSound cシステムサウンド = TJAPlayer3.Skin[ i ];
							if ( !TJAPlayer3.bコンパクトモード || cシステムサウンド.bCompact対象 )
							{
								try
								{
									cシステムサウンド.tLoading();
									Trace.TraceInformation( "システムサウンドを読み込みました。({0})", cシステムサウンド.strFileName );
									//if ( ( cシステムサウンド == CDTXMania.Skin.bgm起動画面 ) && cシステムサウンド.b読み込み成功 )
									//{
									//	cシステムサウンド.t再生する();
									//}
								}
								catch ( FileNotFoundException )
								{
									Trace.TraceWarning( "システムサウンドが存在しません。({0})", cシステムサウンド.strFileName );
								}
								catch ( Exception e )
								{
									Trace.TraceWarning( e.ToString() );
									Trace.TraceWarning( "システムサウンドの読み込みに失敗しました。({0})", cシステムサウンド.strFileName );
								}
							}
						}
					}
					lock ( TJAPlayer3.stage起動.list進行文字列 )
					{
						TJAPlayer3.stage起動.list進行文字列.Add( "SYSTEM SOUND...OK" );
					}
				}
				finally
				{
					Trace.Unindent();
				}
				//-----------------------------
				#endregion

				if ( TJAPlayer3.bコンパクトモード )
				{
					Trace.TraceInformation( "コンパクトモードなので残りの起動処理は省略します。" );
					return;
				}
			}
			finally
			{
				TJAPlayer3.stage起動.ePhaseID = CStage.EPhase.Startup_6_LoadTextures;
				TimeSpan span = (TimeSpan) ( DateTime.Now - now );
				Trace.TraceInformation( "起動所要時間: {0}", span.ToString() );
				lock ( this )							// #28700 2012.6.12 yyagi; state change must be in finally{} for exiting as of compact mode.
				{
					state = DTXEnumState.CompletelyDone;
				}
			}
		}


		/// <summary>
		/// 起動してタイトル画面に遷移した後にバックグラウンドで発生させる曲検索
		/// #27060 2012.2.6 yyagi
		/// </summary>
		private void t曲リストの構築2(bool hard_reload = false)
		{
			// ！注意！
			// 本メソッドは別スレッドで動作するが、プラグイン側でカレントディレクトリを変更しても大丈夫なように、
			// すべてのファイルアクセスは「絶対パス」で行うこと。(2010.9.16)
			// 構築が完了したら、DTXEnumerateState state を DTXEnumerateState.Done にすること。(2012.2.9)

			DateTime now = DateTime.Now;

			try
			{
				if (hard_reload)
				{
					if (File.Exists($"{TJAPlayer3.strEXEのあるフォルダ}songlist.db"))
						File.Delete($"{TJAPlayer3.strEXEのあるフォルダ}songlist.db");
				}
				Deserialize();

				#region [ 2) 曲データの検索 ]
				//-----------------------------
				//	base.eフェーズID = CStage.Eフェーズ.起動2_曲を検索してリストを作成する;

				Trace.TraceInformation( "enum2) 曲データを検索します。" );
				Trace.Indent();

				try
				{
					if ( !string.IsNullOrEmpty( TJAPlayer3.ConfigIni.str曲データ検索パス ) )
					{
						CSongDict.tClearSongNodes();
						string[] strArray = TJAPlayer3.ConfigIni.str曲データ検索パス.Split( new char[] { ';' } );
						if ( strArray.Length > 0 )
						{
							// 全パスについて…
							foreach ( string str in strArray )
							{
								string path = str;
								if ( !Path.IsPathRooted( path ) )
								{
									path = TJAPlayer3.strEXEのあるフォルダ + str;	// 相対パスの場合、絶対パスに直す(2010.9.16)
								}

								if ( !string.IsNullOrEmpty( path ) )
								{
									Trace.TraceInformation( "検索パス: " + path );
									Trace.Indent();

									try
									{
                                        this.Songs管理.t曲を検索してリストを作成する(path, true);
									}
									catch ( Exception e )
									{
										Trace.TraceError( e.ToString() );
										Trace.TraceError( "例外が発生しましたが処理を継続します。 (105fd674-e722-4a4e-bd9a-e6f82ac0b1d3)" );
								}
										finally
									{
										Trace.Unindent();
									}
								}
							}
						}
					}
					else
					{
						Trace.TraceWarning( "曲データの検索パス(TJAPath)の指定がありません。" );
					}
				}
				finally
				{
					Trace.TraceInformation( "曲データの検索を完了しました。[{0}曲{1}スコア]", this.Songs管理.n検索された曲ノード数, this.Songs管理.n検索されたスコア数 );
					Trace.Unindent();
				}
				//	lock ( this.list進行文字列 )
				//	{
				//		this.list進行文字列.Add( string.Format( "{0} ... {1} scores ({2} songs)", "Enumerating songs", this..Songs管理_裏読.n検索されたスコア数, this.Songs管理_裏読.n検索された曲ノード数 ) );
				//	}
				//-----------------------------
				#endregion
				#region [ 4) songs.db になかった曲データをファイルから読み込んで反映 ]
				//-----------------------------
				//					base.eフェーズID = CStage.Eフェーズ.起動4_スコアキャッシュになかった曲をファイルから読み込んで反映する;

				/*
				int num2 = this.Songs管理.n検索されたスコア数 - this.Songs管理.nスコアキャッシュから反映できたスコア数;

				Trace.TraceInformation( "{0}, {1}", this.Songs管理.n検索されたスコア数, this.Songs管理.nスコアキャッシュから反映できたスコア数 );
				Trace.TraceInformation( "enum4) songs.db になかった曲データ[{0}スコア]の情報をファイルから読み込んで反映します。", num2 );
				Trace.Indent();

				try
				{
					this.Songs管理.tSongsDBになかった曲をファイルから読み込んで反映する();
				}
				catch ( Exception e )
				{
					Trace.TraceError( e.ToString() );
					Trace.TraceError( "例外が発生しましたが処理を継続します。 (276bb40f-6406-40c1-9f03-e2a9869dbc88)" );
				}
				finally
				{
					Trace.TraceInformation( "曲データへの反映を完了しました。[{0}/{1}スコア]", this.Songs管理.nファイルから反映できたスコア数, num2 );
					Trace.Unindent();
				}
				//					lock ( this.list進行文字列 )
				//					{
				//						this.list進行文字列.Add( string.Format( "{0} ... {1}/{2}", "Loading score properties from files", CDTXMania.Songs管理_裏読.nファイルから反映できたスコア数, CDTXMania.Songs管理_裏読.n検索されたスコア数 - cs.nスコアキャッシュから反映できたスコア数 ) );
				//					}
				*/
				//-----------------------------
				#endregion
				#region [ 5) 曲リストへの後処理の適用 ]
				//-----------------------------
				//					base.eフェーズID = CStage.Eフェーズ.起動5_曲リストへ後処理を適用する;

				Trace.TraceInformation( "enum5) 曲リストへの後処理を適用します。" );
				Trace.Indent();

				try
				{
					this.Songs管理.t曲リストへ後処理を適用する();
				}
				catch ( Exception e )
				{
					Trace.TraceError( e.ToString() );
					Trace.TraceError( "例外が発生しましたが処理を継続します。 (6480ffa0-1cc1-40d4-9cc9-aceeecd0264b)" );
				}
				finally
				{
					Trace.TraceInformation( "曲リストへの後処理を完了しました。" );
					Trace.Unindent();
				}
				//					lock ( this.list進行文字列 )
				//					{
				//						this.list進行文字列.Add( string.Format( "{0} ... OK", "Building songlists" ) );
				//					}
				//-----------------------------
				#endregion

				//				if ( !bSucceededFastBoot )	// songs2.db読み込みに成功したなら、songs2.dbを新たに作らない
				#region [ 7) songs2.db への保存 ]		// #27060 2012.1.26 yyagi
				Trace.TraceInformation( "enum7) 曲データの情報を songlist.db へ出力します。" );
				Trace.Indent();

				SerializeSongList();
				Trace.TraceInformation("songlist.db への出力を完了しました。");
				Trace.Unindent();
				//-----------------------------
				#endregion
				//				}

			}
			finally
			{
				//				base.eフェーズID = CStage.Eフェーズ.起動7_完了;
				TimeSpan span = (TimeSpan) ( DateTime.Now - now );
				Trace.TraceInformation( "曲探索所要時間: {0}", span.ToString() );
			}
			lock ( this )
			{
				// state = DTXEnumState.Done;		// DoneにするのはCDTXMania.cs側にて。
				state = DTXEnumState.Enumeratad;
			}
		}


#pragma warning disable SYSLIB0011
		/// <summary>
		/// 曲リストのserialize
		/// </summary>
		private void SerializeSongList()
		{
			BinaryFormatter songlistdb_ = new BinaryFormatter();
			using Stream songlistdb = File.OpenWrite($"{TJAPlayer3.strEXEのあるフォルダ}songlist.db");
			songlistdb_.Serialize(songlistdb, Songs管理.listSongsDB);
		}

		/// <summary>
		/// 曲リストのdeserialize
		/// </summary>
		/// <param name="songs管理"></param>
		/// <param name="strPathSongList"></param>
		public void Deserialize()
		{
				try
				{
					if (File.Exists($"{TJAPlayer3.strEXEのあるフォルダ}songlist.db"))
					{
						BinaryFormatter songlistdb_ = new BinaryFormatter();
						using Stream songlistdb = File.OpenRead($"{TJAPlayer3.strEXEのあるフォルダ}songlist.db");
						this.Songs管理.listSongsDB = (Dictionary<string, CSongListNode>)songlistdb_.Deserialize(songlistdb);
					}
				}
				catch(Exception exception)
				{
					this.Songs管理.listSongsDB = new();
				}
				finally
				{
				}
		}
		#pragma warning restore SYSLIB0011
	}
}
