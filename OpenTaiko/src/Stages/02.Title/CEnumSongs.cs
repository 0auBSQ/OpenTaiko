using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace OpenTaiko;

internal class CEnumSongs                           // #27060 2011.2.7 yyagi 曲リストを取得するクラス
{                                                   // ファイルキャッシュ(songslist.db)からの取得と、ディスクからの取得を、この一つのクラスに集約。

	public CSongManager SongManager                     // 曲の探索結果はこのSongs管理に読み込まれる
	{
		get;
		private set;
	}

	public bool IsSongListEnumCompletelyDone        // 曲リスト探索と、実際の曲リストへの反映が完了した？
	{
		get {
			return (this.state == DTXEnumState.CompletelyDone);
		}
	}
	public bool IsEnumerating {
		get {
			if (thDTXFileEnumerate == null) {
				return false;
			}
			return thDTXFileEnumerate.IsAlive;
		}
	}
	public bool IsSongListEnumerated                // 曲リスト探索が完了したが、実際の曲リストへの反映はまだ？
	{
		get {
			return (this.state == DTXEnumState.Enumeratad);
		}
	}
	public bool IsSongListEnumStarted               // 曲リスト探索開始後？(探索完了も含む)
	{
		get {
			return (this.state != DTXEnumState.None);
		}
	}
	public void SongListEnumCompletelyDone() {
		this.state = DTXEnumState.CompletelyDone;
		this.SongManager = null;                        // GCはOSに任せる
	}
	public bool IsSlowdown                          // #PREMOVIE再生中は検索負荷を落とす
	{
		get {
			return this.SongManager.bIsSlowdown;
		}
		set {
			this.SongManager.bIsSlowdown = value;
		}
	}

	public void ChangeEnumeratePriority(ThreadPriority tp) {
		if (this.thDTXFileEnumerate != null && this.thDTXFileEnumerate.IsAlive == true) {
			this.thDTXFileEnumerate.Priority = tp;
		}
	}
	private readonly string strPathSongsDB = OpenTaiko.strEXEFolder + "songs.db";
	private readonly string strPathSongList = OpenTaiko.strEXEFolder + "songlist.db";

	public Thread thDTXFileEnumerate {
		get;
		private set;
	}
	public enum DTXEnumState {
		Canceled = -1,
		None = 0,
		Ongoing,
		Suspended,
		Enumeratad,             // 探索完了、現在の曲リストに未反映
		CompletelyDone          // 探索完了、現在の曲リストに反映完了
	}
	public DTXEnumState state { get; private set; } = DTXEnumState.None;


	/// <summary>
	/// Constractor
	/// </summary>
	public CEnumSongs() {
		this.SongManager = new CSongManager();
	}

	public void Init() {

	}

	/// <summary>
	/// 曲リストのキャッシュ(songlist.db)取得スレッドの開始
	/// </summary>
	public void StartEnumFromCache() {
		this.thDTXFileEnumerate = new Thread(new ThreadStart(this.EstablishSystemSounds));
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
	public void StartEnumFromDisk(bool hard_reload = false) {
		if (state == DTXEnumState.None || state == DTXEnumState.CompletelyDone) {
			Trace.TraceInformation("★曲データ検索スレッドを起動しました。");
			lock (this) {
				state = DTXEnumState.Ongoing;
			}
			// this.autoReset = new AutoResetEvent( true );

			if (this.SongManager == null)       // Enumerating Songs完了後、CONFIG画面から再スキャンしたときにこうなる
			{
				this.SongManager = new CSongManager();
			}
			if (hard_reload)
				this.thDTXFileEnumerate = new Thread(new ThreadStart(this.HardReloadSongList));
			else
				this.thDTXFileEnumerate = new Thread(new ThreadStart(this.ReloadSongList));
			this.thDTXFileEnumerate.Name = "曲リストの構築";
			this.thDTXFileEnumerate.IsBackground = true;
			this.thDTXFileEnumerate.Priority = System.Threading.ThreadPriority.Lowest;
			this.thDTXFileEnumerate.Start();
		}
	}

	// OpenTaiko song-file extensions the scan (CSongManager.tSongSearchListCreate) turns into song nodes;
	// this must match the extensions that increment nSearchFileCount so the progress total lines up.
	private static readonly HashSet<string> SongFileExtensions = new(StringComparer.OrdinalIgnoreCase) {
		".tja", ".tci", ".optktci", ".tcm", ".optktcm"
	};

	// Count song files under a folder, recursing manually so an inaccessible subfolder is skipped
	// rather than aborting the whole count (Directory.EnumerateFiles(AllDirectories) throws on the first).
	private static int CountSongFilesRecursive(string dir) {
		int n = 0;
		try {
			foreach (var f in Directory.EnumerateFiles(dir)) {
				if (SongFileExtensions.Contains(Path.GetExtension(f))) n++;
			}
			foreach (var d in Directory.EnumerateDirectories(dir))
				n += CountSongFilesRecursive(d);
		} catch { /* skip unreadable folders */ }
		return n;
	}

	private void HardReloadSongList() {
		this.LoadSongListStructure(true);
	}
	private void ReloadSongList() {
		this.LoadSongListStructure(false);
	}

	/// <summary>
	/// 曲探索スレッドのサスペンド
	/// </summary>
	public void Suspend() {
		if (this.state != DTXEnumState.CompletelyDone &&
			((thDTXFileEnumerate?.ThreadState & (System.Threading.ThreadState.Background)) != 0)) {
			// this.thDTXFileEnumerate.Suspend();		// obsoleteにつき使用中止
			this.SongManager.bIsSuspending = true;
			this.state = DTXEnumState.Suspended;
			Trace.TraceInformation("★曲データ検索スレッドを中断しました。");
		}
	}

	/// <summary>
	/// 曲探索スレッドのレジューム
	/// </summary>
	public void Resume() {
		if (this.state == DTXEnumState.Suspended) {
			if ((this.thDTXFileEnumerate.ThreadState & (System.Threading.ThreadState.WaitSleepJoin | System.Threading.ThreadState.StopRequested)) != 0) //
			{
				// this.thDTXFileEnumerate.Resume();	// obsoleteにつき使用中止
				this.SongManager.bIsSuspending = false;
				this.SongManager.AutoReset.Set();
				this.state = DTXEnumState.Ongoing;
				Trace.TraceInformation("★曲データ検索スレッドを再開しました。");
			}
		}
	}

	/// <summary>
	/// 曲探索スレッドにサスペンド指示を出してから、本当にサスペンド状態に遷移するまでの間、ブロックする
	/// 500ms * 10回＝5秒でタイムアウトし、サスペンド完了して無くてもブロック解除する
	/// </summary>
	public void WaitUntilSuspended() {
		// 曲検索が一時中断されるまで待機
		for (int i = 0; i < 10; i++) {
			if (this.state == DTXEnumState.CompletelyDone ||
				(thDTXFileEnumerate?.ThreadState & (System.Threading.ThreadState.WaitSleepJoin | System.Threading.ThreadState.Background | System.Threading.ThreadState.Stopped)) != 0) {
				break;
			}
			Trace.TraceInformation("★曲データ検索スレッドの中断待ちです: {0}", this.thDTXFileEnumerate?.ThreadState.ToString());
			Thread.Sleep(500);
		}

	}

	/// <summary>
	/// 曲探索スレッドを強制終了する
	/// </summary>
	public void Abort() {
		if (thDTXFileEnumerate != null) {
			this.SongManager.bIsCanceled = true;
			this.state = DTXEnumState.Canceled;
			try {
				thDTXFileEnumerate.Join();
			} catch (Exception ex) {
				Trace.TraceWarning(ex.ToString());
				Trace.TraceWarning("Error terminating song list loading thread; continue anyway.");
			}
			// wait until enum thread terminated

			// Songs管理を再初期化する (途中まで作った曲リストの最後に、一から重複して追記することにならないようにする。)
			thDTXFileEnumerate = null;
			this.SongManager = new CSongManager();
			this.state = DTXEnumState.None;
		}
	}



	/// <summary>
	/// songlist.dbからの曲リスト構築
	/// </summary>
	public void EstablishSystemSounds() {
		// ！注意！
		// 本メソッドは別スレッドで動作するが、プラグイン側でカレントディレクトリを変更しても大丈夫なように、
		// すべてのファイルアクセスは「絶対パス」で行うこと。(2010.9.16)
		// 構築が完了したら、DTXEnumerateState state を DTXEnumerateState.Done にすること。(2012.2.9)
		DateTime now = DateTime.Now;

		try {
			#region [ Establish System Sounds  ]
			//-----------------------------
			OpenTaiko.stageStartup.ePhaseID = CStage.EPhase.Startup_0_CreateSystemSound;

			Trace.TraceInformation("0) システムサウンドを構築します。");
			Trace.Indent();

			try {
				OpenTaiko.Skin.bgmStartupScreen.tPlay();
				OpenTaiko.Skin.PreloadSystemSounds();
				lock (OpenTaiko.stageStartup.listProgressString) {
					OpenTaiko.stageStartup.listProgressString.Add("SYSTEM SOUND...OK");
				}
			} finally {
				Trace.Unindent();
			}
			//-----------------------------
			#endregion

		} finally {
			TimeSpan span = (TimeSpan)(DateTime.Now - now);
			Trace.TraceInformation("起動所要時間: {0}", span.ToString());
			lock (this)                         // #28700 2012.6.12 yyagi; state change must be in finally{} for exiting as of compact mode.
			{
				state = DTXEnumState.CompletelyDone;
			}
			OpenTaiko.stageStartup.ePhaseID = CStage.EPhase.Startup_6_LoadTextures;
		}
	}


	/// <summary>
	/// 起動してタイトル画面に遷移した後にバックグラウンドで発生させる曲検索
	/// #27060 2012.2.6 yyagi
	/// </summary>
	private void LoadSongListStructure(bool hard_reload = false) {
		// ！注意！
		// 本メソッドは別スレッドで動作するが、プラグイン側でカレントディレクトリを変更しても大丈夫なように、
		// すべてのファイルアクセスは「絶対パス」で行うこと。(2010.9.16)
		// 構築が完了したら、DTXEnumerateState state を DTXEnumerateState.Done にすること。(2012.2.9)

		DateTime now = DateTime.Now;
#if DEBUG
		tTraceSongEnumMemory("before enum");
#endif

		try {
			if (hard_reload) {
				if (File.Exists($"{OpenTaiko.strEXEFolder}songlist.db"))
					File.Delete($"{OpenTaiko.strEXEFolder}songlist.db");
			}
			Deserialize();

			#region [ Search for songs data ]
			//-----------------------------
			//	base.eフェーズID = CStage.Eフェーズ.起動2_曲を検索してリストを作成する;

			Trace.TraceInformation("enum2) 曲データを検索します。");
			Trace.Indent();

			try {
				if (!string.IsNullOrEmpty(OpenTaiko.ConfigIni.strSongsPath)) {
					CSongDict.tClearSongNodes();
					string[] strArray = OpenTaiko.ConfigIni.strSongsPath.Split(new char[] { ';' });

					// Pre-count all song files (.tja/.dtx) across the search roots so the enumeration display
					// can show a real "loaded / total" progress bar as the scan below parses them.
					this.SongManager.nSearchFileCount = 0;
					int totalSongFiles = 0;
					foreach (string str in strArray) {
						string cp = Path.IsPathRooted(str) ? str : OpenTaiko.strEXEFolder + str;
						totalSongFiles += CountSongFilesRecursive(cp);
					}
					this.SongManager.nTotalSongFilesToSearch = totalSongFiles;

					if (strArray.Length > 0) {
						// 全パスについて…
						foreach (string str in strArray) {
							string path = str;
							if (!Path.IsPathRooted(path)) {
								path = OpenTaiko.strEXEFolder + str;  // 相対パスの場合、絶対パスに直す(2010.9.16)
							}

							if (!string.IsNullOrEmpty(path)) {
								Trace.TraceInformation("検索パス: " + path);
								Trace.Indent();

								try {
									this.SongManager.tSongSearchListCreate(path, true);
								} catch (OperationCanceledException) {
									throw; // forward cancellation
								} catch (Exception e) {
									Trace.TraceError(e.ToString());
									Trace.TraceError("例外が発生しましたが処理を継続します。 (105fd674-e722-4a4e-bd9a-e6f82ac0b1d3)");
								} finally {
									Trace.Unindent();
								}
							}
						}
					}
				} else {
					Trace.TraceWarning("曲データの検索パス(TJAPath)の指定がありません。");
				}
			} finally {
				Trace.TraceInformation("曲データの検索を完了しました。[{0}曲{1}スコア]", this.SongManager.nSearchSongNodeCount, this.SongManager.nSearchScoreCount);
				Trace.Unindent();
			}
			//	lock ( this.list進行文字列 )
			//	{
			//		this.list進行文字列.Add( string.Format( "{0} ... {1} scores ({2} songs)", "Enumerating songs", this..Songs管理_裏読.n検索されたスコア数, this.Songs管理_裏読.n検索された曲ノード数 ) );
			//	}
			//-----------------------------
			#endregion

			#region [ Song list Post Processing ]
			//-----------------------------
			//					base.eフェーズID = CStage.Eフェーズ.起動5_曲リストへ後処理を適用する;

			Trace.TraceInformation("enum5) 曲リストへの後処理を適用します。");
			Trace.Indent();

			try {
				this.SongManager.tSongListPostprocessing();
			} catch (OperationCanceledException) {
				throw; // forward cancellation
			} catch (Exception e) {
				Trace.TraceError(e.ToString());
				Trace.TraceError("例外が発生しましたが処理を継続します。 (6480ffa0-1cc1-40d4-9cc9-aceeecd0264b)");
			} finally {
				Trace.TraceInformation("曲リストへの後処理を完了しました。");
				Trace.Unindent();
			}
			//					lock ( this.list進行文字列 )
			//					{
			//						this.list進行文字列.Add( string.Format( "{0} ... OK", "Building songlists" ) );
			//					}
			//-----------------------------
			#endregion

			//				if ( !bSucceededFastBoot )	// songs2.db読み込みに成功したなら、songs2.dbを新たに作らない
			#region [ Serialize songlist.db ]		// #27060 2012.1.26 yyagi
			Trace.TraceInformation("enum7) 曲データの情報を songlist.db へ出力します。");
			Trace.Indent();

			SerializeSongList();
			Trace.TraceInformation("songlist.db への出力を完了しました。");
			Trace.Unindent();
			//-----------------------------
			#endregion
			//				}

			#region [Reload all lua song list objects]

			// Tests

			//LuaSongListSettings _sts = LuaSongListSettings.Generate();
			//LuaSongList _sl = new LuaSongList(_sts);
			//var _pg = _sl.GetCurrentlyDisplayedPage(10, 10);
			//Debug.Print(_pg.ToString());

			#endregion

		} catch (OperationCanceledException) { // canceled
			lock (this) {
				state = DTXEnumState.Canceled;
			}
			Trace.TraceInformation("Song list enumeration canceled.");
			return;
		} finally {
			//				base.eフェーズID = CStage.Eフェーズ.起動7_完了;
			TimeSpan span = (TimeSpan)(DateTime.Now - now);
			Trace.TraceInformation("曲探索所要時間: {0}", span.ToString());
		}
#if DEBUG
		tTraceSongEnumMemory("after enum+serialize");
#endif

		lock (this) {
			// state = DTXEnumState.Done;		// DoneにするのはCDTXMania.cs側にて。
			state = DTXEnumState.Enumeratad;
		}
	}

#if DEBUG
	private static void tTraceSongEnumMemory(string tag) {
		long managedMB = GC.GetTotalMemory(forceFullCollection: true) / (1024 * 1024);
		if (OperatingSystem.IsIOS() || OperatingSystem.IsAndroid()) {
			// Mobile does not support the System.Diagnostics.Process working-set / paged-memory APIs
			// (PlatformNotSupportedException); log managed memory only.
			Trace.TraceInformation("[ENUM_MEM] {0}: managed(after full GC)={1:N0}MB", tag, managedMB);
			return;
		}
		using var proc = System.Diagnostics.Process.GetCurrentProcess();
		Trace.TraceInformation(
			"[ENUM_MEM] {0}: managed(after full GC)={1:N0}MB, workingSet={2:N0}MB, paged={3:N0}MB",
			tag, managedMB, proc.WorkingSet64 / (1024 * 1024), proc.PagedMemorySize64 / (1024 * 1024));
	}
#endif


#pragma warning disable SYSLIB0011
	/// <summary>
	/// 曲リストのserialize
	/// </summary>
	private void SerializeSongList() {
		if (OperatingSystem.IsIOS() || OperatingSystem.IsAndroid()) return; // mobile: skip the songlist.db cache (BinaryFormatter is unsupported there)
		using Stream songlistdb = File.Create($"{OpenTaiko.strEXEFolder}songlist.db");
		WriteSongListCache(songlistdb, SongManager.listSongsDB);
	}

	/// <summary>
	/// Deserialize the song-list cache. If the cache predates or no longer matches the current
	/// serialized schema (e.g. a serialized field was renamed/added/removed), it is silently discarded
	/// so the caller rebuilds the list from disk. Users never have to delete songlist.db by hand.
	/// </summary>
	public void Deserialize() {
		if (OperatingSystem.IsIOS() || OperatingSystem.IsAndroid()) return; // BinaryFormatter not supported on mobile
		try {
			if (File.Exists($"{OpenTaiko.strEXEFolder}songlist.db")) {
				using Stream songlistdb = File.OpenRead($"{OpenTaiko.strEXEFolder}songlist.db");
				this.SongManager.listSongsDB = ReadSongListCache(songlistdb) ?? new();
			}
		} catch (Exception exception) {
			this.SongManager.listSongsDB = new();
		}
	}

	// ── songlist.db format: a schema signature, then the song dictionary ───────────────────────────────
	// The signature is a fingerprint of the serialized type graph (every serialized field's name + type),
	// so ANY change to the CSongListNode/CScore/… layout — including a rename — changes it and makes old
	// caches load as empty and rebuild automatically. This prevents BinaryFormatter from silently loading
	// a stale cache with mismatched field names (which leaves the renamed fields null).
	private static string _cacheSchemaSignature;
	internal static string SongListCacheSchemaSignature
		=> _cacheSchemaSignature ??= ComputeSchemaSignature(typeof(Dictionary<string, CSongListNode>));

	internal static void WriteSongListCache(Stream stream, Dictionary<string, CSongListNode> listSongsDB) {
		BinaryFormatter songlistdb_ = new BinaryFormatter();
		songlistdb_.Serialize(stream, SongListCacheSchemaSignature);
		songlistdb_.Serialize(stream, listSongsDB);
	}

	internal static Dictionary<string, CSongListNode> ReadSongListCache(Stream stream) {
		BinaryFormatter songlistdb_ = new BinaryFormatter();
		// A cache written before this header (or with a different schema) fails this check → rebuild.
		if (songlistdb_.Deserialize(stream) is not string signature || signature != SongListCacheSchemaSignature)
			return null;
		return (Dictionary<string, CSongListNode>)songlistdb_.Deserialize(stream);
	}

	/// <summary>Fingerprint of the serialized object graph reachable from <paramref name="root"/>
	/// (field names + field types, recursively through OpenTaiko types), stable across runs.</summary>
	private static string ComputeSchemaSignature(Type root) {
		var sb = new StringBuilder();
		var seen = new HashSet<Type>();
		void Consider(Type t) {
			if (t == null) return;
			if (t.IsArray) { Consider(t.GetElementType()); return; }
			if (t.IsGenericType) { foreach (var a in t.GetGenericArguments()) Consider(a); return; }
			if (t.Namespace != null && t.Namespace.StartsWith("OpenTaiko")) Walk(t);
		}
		void Walk(Type t) {
			if (!seen.Add(t)) return;
			sb.Append(t.FullName).Append('{');
			foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
								.Where(f => !f.IsNotSerialized)
								.OrderBy(f => f.Name, StringComparer.Ordinal)) {
				sb.Append(f.Name).Append(':').Append(f.FieldType.FullName).Append(';');
				Consider(f.FieldType);
			}
			sb.Append('}');
		}
		Consider(root);
		return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
	}
#pragma warning restore SYSLIB0011
}
