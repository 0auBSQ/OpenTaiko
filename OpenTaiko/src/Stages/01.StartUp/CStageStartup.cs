using System.Diagnostics;
using FDK;

namespace OpenTaiko;

internal class CStageStartup : CStage {
	// Constructor

	public CStageStartup() {
		base.eStageID = CStage.EStage.StartUp;
		base.IsDeActivated = true;
	}

	public List<string> listProgressString;

	// CStage 実装

	public override void Activate() {
		Trace.TraceInformation("起動ステージを活性化します。");
		Trace.Indent();
		try {
			Background = new ScriptBG(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.STARTUP}Script.lua"));
			Background.Init();

			// Note: CLoadingProgress.Begin() is NOT called here — the boot stage is activated twice during
			// startup, which would reset the bar mid-preload. Begin() is called once in tStartupProcess,
			// before the module preload (the first thing the boot bar tracks).

			this.listProgressString = new List<string>();
			base.ePhaseID = CStage.EPhase.Common_NORMAL;
			base.Activate();
			Trace.TraceInformation("起動ステージの活性化を完了しました。");
		} finally {
			Trace.Unindent();
		}
	}
	public override void DeActivate() {
		Trace.TraceInformation("起動ステージを非活性化します。");
		Trace.Indent();
		try {
			OpenTaiko.tDisposeSafely(ref Background);

			this.listProgressString = null;
			if (es != null) {
				if ((es.thDTXFileEnumerate != null) && es.thDTXFileEnumerate.IsAlive) {
					Trace.TraceWarning("リスト構築スレッドを強制停止します。");
					es.thDTXFileEnumerate.Abort();
					es.thDTXFileEnumerate.Join();
				}
			}
			base.DeActivate();
			Trace.TraceInformation("起動ステージの非活性化を完了しました。");
		} finally {
			Trace.Unindent();
		}
	}
	public override void CreateManagedResource() {
		base.CreateManagedResource();
	}
	public override void ReleaseManagedResource() {
		base.ReleaseManagedResource();
	}

	public override int Draw() {
		if (!base.IsDeActivated) {
			if (base.IsFirstDraw) {
				this.listProgressString.Add("DTXManiaXG Ver.K powered by YAMAHA Silent Session Drums");
				this.listProgressString.Add("Product by.kairera0467");
				this.listProgressString.Add("Release: " + OpenTaiko.VERSION + " [" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + "]");

				this.listProgressString.Add("");
				this.listProgressString.Add("TJAPlayer3-Develop-ReWrite forked TJAPlayer3(@aioilight)");
				this.listProgressString.Add("OpenTaiko forked TJAPlayer3-Develop-ReWrite(@TouhouRenren)");
				this.listProgressString.Add("OpenTaiko edited by 0AuBSQ");
				this.listProgressString.Add("");

				// Load the skin's Lua modules incrementally over the next frames via the shared CLoadSession,
				// driven by this render loop (so the bar animates normally). The session opens a CAsyncLoad phase
				// so the modules' onStart textures stream in + their SOUND/Shared loads defer; the per-texture
				// trace flush is batched here. The song-list enum is started once module loading finishes (below).
				_session = new CLoadSession(new EnumeratorStep(OpenTaiko.Skin.LoadModulesIncrementally()));
				_session.Begin();
				_savedAutoFlush = System.Diagnostics.Trace.AutoFlush;
				System.Diagnostics.Trace.AutoFlush = false;
				base.IsFirstDraw = false;
				return 0;
			}

			// CSongs管理 s管理 = CDTXMania.Songs管理;

			Background.Update();
			Background.Draw();
			CLoadingScreen.Draw();   // engine loading bar overlay

			#region [ Incremental Lua module loading (0-55% bar) + texture stream drain (55-60%), before the song enum ]
			if (_session != null) {
				bool more = _session.Step();
				// 0-55%: module onStart;  55-60%: streamed onStart-texture upload drain.
				CLoadingProgress.Report(_session.SourceDone ? 0.55f + 0.05f * _session.AssetFraction
				                                            : 0.55f * _session.SourceProgress);
				if (!more) {
					_session.End();
					_session = null;
					System.Diagnostics.Trace.AutoFlush = _savedAutoFlush;
					System.Diagnostics.Trace.Flush();
					OpenTaiko.NamePlate.RefleshSkin();
					OpenTaiko.ModalManager.RefleshSkin();
					es = new CEnumSongs();
					es.StartEnumFromCache();   // 曲リスト取得(別スレッドで実行される)
				}
			}
			#endregion

			#region [ this.str現在進行中 の決定 ]
			//-----------------
			switch (base.ePhaseID) {
				case CStage.EPhase.Startup_0_CreateSystemSound:
					this.strCurrentProgress = "SYSTEM SOUND...";
					break;

				case CStage.EPhase.Startup_1_InitializeSonglist:
					this.strCurrentProgress = "SONG LIST...";
					break;

				case CStage.EPhase.Startup_2_EnumerateSongs:
					this.strCurrentProgress = string.Format("{0} ... {1}", "Enumerating songs", es.SongManager.nSearchScoreCount);
					break;

				case CStage.EPhase.Startup_3_ApplyScoreCache:
					this.strCurrentProgress = string.Format("{0} ... {1}/{2}", "Loading score properties from songs.db", es.SongManager.nScoresAppliedFromScoreCache, es.SongManager.nSearchScoreCount);
					break;

				case CStage.EPhase.Startup_4_LoadSongsNotSeenInScoreCacheAndApplyThem:
					this.strCurrentProgress = string.Format("{0} ... {1}/{2}", "Loading score properties from files", es.SongManager.nScoresAppliedFromFile, es.SongManager.nSearchScoreCount - es.SongManager.nScoresAppliedFromScoreCache);
					break;

				case CStage.EPhase.Startup_5_PostProcessSonglist:
					this.strCurrentProgress = string.Format("{0} ... ", "Building songlists");
					break;

				case CStage.EPhase.Startup_6_LoadTextures:
					if (!bIsLoadingTextures) {
						void loadTexture() {
							this.listProgressString.Add("LOADING TEXTURES...");

							try {
								OpenTaiko.Tx.LoadTexture();
								CLoadingProgress.End();   // textures done → snap the boot bar to 100%

								this.listProgressString.Add("LOADING TEXTURES...OK");
								this.strCurrentProgress = "Setup done.";
								this.ePhaseID = EPhase.Startup_Complete;
								OpenTaiko.Skin.bgmStartupScreen.tStop();
							} catch (Exception exception) {
								OpenTaiko.Skin.bgmStartupScreen.tStop();

								Trace.TraceError(exception.ToString());
								this.listProgressString.Add("LOADING TEXTURES...NG");
								foreach (var text in exception.ToString().Split('\n')) {
									this.listProgressString.Add(text);
								}
							}

							this.listProgressString.Add("LOADING TEXTURES...OK");
							this.strCurrentProgress = "Setup done.";
							this.ePhaseID = EPhase.Startup_Complete;
							OpenTaiko.Skin.bgmStartupScreen.tStop();
						}
						if (OpenTaiko.ConfigIni.ASyncTextureLoad && !OperatingSystem.IsIOS()) {
							Task.Run(loadTexture);
						} else {
							loadTexture();
						}
					}
					bIsLoadingTextures = true;
					break;
			}
			//-----------------
			#endregion

			if (ePhaseID != EPhase.Startup_Complete) {
				#region [ this.list進行文字列＋this.現在進行中 の表示 ]
				//-----------------
				int x = (int)(320 * OpenTaiko.Skin.Resolution[0] / 1280.0);
				int y = (int)(20 * OpenTaiko.Skin.Resolution[1] / 720.0);
				int dy = (int)((OpenTaiko.actTextConsole.fontHeight + 8) * OpenTaiko.Skin.Resolution[1] / 720.0);
				for (int i = 0; i < this.listProgressString.Count; i++) {
					y = OpenTaiko.actTextConsole.Print(x, y, CTextConsole.EFontType.White, this.listProgressString[i]).y;
					y += dy;
				}
				//-----------------
				#endregion
			} else {
				if (es != null && es.IsSongListEnumCompletelyDone)                          // 曲リスト作成が終わったら
				{
					OpenTaiko.SongManager = (es != null) ? es.SongManager : null;      // 最後に、曲リストを拾い上げる

					return 1;
				}
			}

		}
		return 0;
	}


	// その他

	#region [ private ]
	//-----------------
	private string strCurrentProgress = "";
	private ScriptBG Background;
	private CEnumSongs es;
	private bool bIsLoadingTextures;
	private CLoadSession _session;       // drives the incremental skin-module load + onStart-texture stream
	private bool _savedAutoFlush;        // Trace.AutoFlush state to restore after the batched module-load phase

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
