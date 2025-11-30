using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OpenTaiko.CSongListNodeComparers;

namespace OpenTaiko;

[Serializable]
internal class CSongManager {
	// Properties

	public int nScoresAppliedFromScoreCache {
		get;
		set;
	}
	// Use a private field to allow atomic incrementing when parsing in parallel.
	private int _nScoresAppliedFromFile;
	public int nScoresAppliedFromFile {
		get => _nScoresAppliedFromFile;
		set => _nScoresAppliedFromFile = value;
	}
	public int nSearchScoreCount {
		get;
		set;
	}
	public int nSearchSongNodeCount {
		get;
		set;
	}
	// Enumeration progress (drives the song_enum ROActivity bar): OpenTaiko song files (.tja, .tci/.optktci,
	// .tcm/.optktcm) processed so far, and the total counted by the pre-parse walk. Backed by a field so the
	// parallel pre-parse can bump it with Interlocked.
	private int _nSearchFileCount;
	public int nSearchFileCount {
		get => _nSearchFileCount;
		set => _nSearchFileCount = value;
	}
	public int nTotalSongFilesToSearch {
		get;
		set;
	}
	public Dictionary<string, CSongListNode> listSongsDB;                   // songs.dbから構築されるlist
	[NonSerialized]
	private ConcurrentDictionary<string, (string hash, CSongListNode? node)>? _preparsed;   // unparented song nodes from the parallel pre-parse, keyed by TJA full path
	public CSongListNode? SongRootDownload = null;
	public List<CSongListNode> listSongRoot;         // 起動時にフォルダ検索して構築されるlist
	public HashSet<CSongListNode> SongRootsDan = [];
	public List<CSongListNode> listSongRoot_Dan = new List<CSongListNode>();          // 起動時にフォルダ検索して構築されるlist
	public HashSet<CSongListNode> SongRootsTower = [];
	public List<CSongListNode> listSongRoot_Tower = new List<CSongListNode>();          // 起動時にフォルダ検索して構築されるlist
	public static List<FDK.CTexture> listCustomBGs = new List<FDK.CTexture>();
	public bool bIsCanceled { get; set; }
	public bool bIsSuspending                           // 外部スレッドから、内部スレッドのsuspendを指示する時にtrueにする
	{                                                   // 再開時は、これをfalseにしてから、次のautoReset.Set()を実行する
		get;
		set;
	}
	public bool bIsSlowdown                             // #PREMOVIE再生時に曲検索を遅くする
	{
		get;
		set;
	}
	[NonSerialized]
	public AutoResetEvent AutoReset;

	private int searchCount;                            // #PREMOVIE中は検索n回実行したら少しスリープする

	// Constructor

	public CSongManager() {
		this.listSongsDB = new();
		this.listSongRoot = new List<CSongListNode>();
		this.nSearchSongNodeCount = 0;
		this.nSearchScoreCount = 0;
		this.nSearchFileCount = 0;
		this.nTotalSongFilesToSearch = 0;
		this.bIsSuspending = false;                     // #27060
		this.AutoReset = new AutoResetEvent(true);  // #27060
		this.searchCount = 0;
	}


	// メソッド

	#region [ Fetch song list ]
	//-----------------

	public void UpdateDownloadBox() {
		CSongListNode? downloadBox = OpenTaiko.SongManager.SongRootDownload;

		if (downloadBox != null && downloadBox.childrenList != null) {

			var (lastNode, count) = CActSelectSongList.GetFromFlattenList(downloadBox.childrenList);

			foreach (var node in downloadBox.childrenList) {
				CSongDict.tRemoveSongNode(node.uniqueId);
			}
			downloadBox.childrenList.Clear();


			var path = downloadBox.score[0].FileInfo.FolderAbsolutePath;

			if (count > 0) {
				tSongSearchListCreate(path, true, downloadBox.childrenList, downloadBox);
				this.tSongListPostprocessing(downloadBox.childrenList, $"/{downloadBox.ldTitle.GetString("")}/");
				downloadBox.childrenList.Insert(0, CSongDict.tGenerateBackButton(downloadBox, $"/{downloadBox.ldTitle.GetString("")}/"));
			}
		}

	}
	public void tSongSearchListCreate(string strBaseFolder, bool bChildBOXRecurse) {
		var deferredTcm = new List<(string filePath, List<CSongListNode> nodeList, CSongListNode? parent)>();
		this.tSongSearchListCreate(strBaseFolder, bChildBOXRecurse, this.listSongRoot, null, deferredTcm);
		this.ProcessDeferredTcm(deferredTcm);
	}

	// Uppercase-hex SHA1 of a chart file, used as the cache key alongside its path.
	private static string TjaHash(string filePath) {
		using SHA1 sha = SHA1.Create();
		using var fs = File.OpenRead(filePath);
		return Convert.ToHexString(sha.ComputeHash(fs));
	}

	// Song file extensions counted toward the progress total (must match the ones tSongSearchListCreate
	// turns into nodes and increments nSearchFileCount for).
	private static readonly HashSet<string> SongFileExtensions = new(StringComparer.OrdinalIgnoreCase) {
		".tja", ".tci", ".optktci", ".tcm", ".optktcm"
	};

	/// <summary>
	/// One folder walk that sets the enumeration progress total and turns every .tja into a song node
	/// across CPU cores, so the serial tree build below reuses the nodes instead of hashing and parsing
	/// one file at a time. nSearchFileCount advances during the parse, so the progress bar moves through
	/// this phase instead of sitting at 0%.
	/// </summary>
	public void PreparseTjaCharts(IEnumerable<string> roots) {
		var paths = new List<string>();
		int nonTja = 0;
		foreach (string root in roots) {
			try { nonTja += CollectCharts(root, paths); } catch (Exception e) { Trace.TraceWarning(e.Message); }
		}
		// Each .tja is counted twice, once here and once when the tree build turns it into a node.
		// The other song files are counted by the tree build alone.
		this.nTotalSongFilesToSearch = 2 * paths.Count + nonTja;
		this.nSearchFileCount = 0;
		var dict = new ConcurrentDictionary<string, (string, CSongListNode?)>();
		Parallel.ForEach(paths, p => {
			try {
				string hash = TjaHash(p);
				// Create song nodes except for the charts already in the cache.
				dict[p] = (hash, listSongsDB.ContainsKey(p + hash) ? null : ParseUnparentedSongNode(p));
			} catch { }
			Interlocked.Increment(ref _nSearchFileCount);
		});
		_preparsed = dict;
	}

	// Parses a chart file into an unparented song node, without retaining the CTja object.
	private CSongListNode? ParseUnparentedSongNode(string filePath) {
		CTja dtx = new CTja(filePath); // NOTICE: #COMPAT: from box.def is not applied here. Metadata relying on COMPAT might be inaccurate and might need to be avoided
		CSongListNode? node = CreateUnparentedSongNode(dtx, filePath);
		dtx.DeActivate();
		return node;
	}

	/// <summary>
	/// Builds the song node for one parsed chart, or returns null when the chart has no course. The node
	/// carries only the fields the chart itself provides, so this runs before the parent box is known. The
	/// caller must pass the node to ApplySongNodeParent to complete it.
	/// </summary>
	private CSongListNode? CreateUnparentedSongNode(CTja dtx, string filePath) {
		var fileinfo = new FileInfo(filePath);
		string strBaseFolder = fileinfo.DirectoryName + Path.DirectorySeparatorChar;
		CSongListNode node = new CSongListNode();
		node.nodeType = CSongListNode.ENodeType.SCORE;

		bool hasAnyDifficultyProcessed = false;
		for (int n = 0; n < (int)Difficulty.Total; n++) {
			if (!dtx.bChartExists[n]) continue;

			node.difficultiesCount++;

			node.ldTitle = dtx.TITLE;
			node.ldSubtitle = dtx.SUBTITLE;
			node.strMaker = dtx.MAKER;
			node.strNotesDesigner = dtx.SongListCourseMetadata.Select(m => m.NOTESDESIGNER.Equals("") ? node.strMaker : m.NOTESDESIGNER).ToArray();
			node.nSide = dtx.SIDE;
			node.bExplicit = dtx.EXPLICIT;
			node.bMovie = !string.IsNullOrEmpty(dtx.strBGVIDEO_PATH);

			// Shallow copy, works well because dict<string, string> but wouldn't if dict<string, T>
			node.customMetadataGScope = new Dictionary<string, string>(dtx.GlobalCustomMetadata);
			node.customMetadataCScope = dtx.SongListCourseMetadata.Select(meta => new Dictionary<string, string>(meta.CustomMetadata)).ToArray();

			node.DanSongs = new();
			if (dtx.List_DanSongs != null) {
				for (int i = 0; i < dtx.List_DanSongs.Count; i++) {
					node.DanSongs.Add(dtx.List_DanSongs[i]);
				}
			}

			if (dtx.Dan_C != null)
				node.Dan_C = dtx.Dan_C;

			// The chart's own genre. ApplySongNodeParent lets the parent box override it.
			node.songGenre = dtx.GENRE ?? "";
			node.songGenrePanel = dtx.GENRE ?? "";

			// Left null when the chart has none, so ApplySongNodeParent can fall back to the parent box.
			node.strSelectBGPath = (dtx.SELECTBG != null && File.Exists(strBaseFolder + dtx.SELECTBG))
				? strBaseFolder + dtx.SELECTBG : null;

			node.nLevel = dtx.SongListCourseMetadata.Select(m => m.LEVELtaiko).ToArray();
			node.dLevel = dtx.SongListCourseMetadata.Select(m => m.LEVELtaikoDecimal).ToArray();
			node.nLevelIcon = dtx.SongListCourseMetadata.Select(m => m.LEVELtaikoIcon).ToArray();
			node.uniqueId = dtx.uniqueID;

			node.CutSceneIntro = dtx.CutSceneIntro;
			node.CutSceneOutros = dtx.CutSceneOutros;

			node.score[n] = new CScore();
			node.score[n].FileInfo.FileAbsolutePath = strBaseFolder + fileinfo.Name;
			node.score[n].FileInfo.FolderAbsolutePath = strBaseFolder;
			node.score[n].FileInfo.FileSize = fileinfo.Length;
			node.score[n].FileInfo.LastUpdateDateTime = fileinfo.LastWriteTime;

			LoadChartInfo(node, dtx, n);

			hasAnyDifficultyProcessed = true;
		}

		return hasAnyDifficultyProcessed ? node : null;
	}

	// Applies the song node fields that come from the parent box.
	private static void ApplySongNodeParent(CSongListNode node, CSongListNode? parent, string filePath) {
		node.rParentNode = parent;
		node.strBreadcrumbs = (parent == null) ? filePath : parent.strBreadcrumbs + " > " + filePath;

		string chartGenre = node.songGenre;
		string? parentGenre = string.IsNullOrEmpty(parent?.songGenre) ? null : parent.songGenre;
		node.songGenre = parentGenre ?? chartGenre;
		node.songGenrePanel = (!string.IsNullOrEmpty(chartGenre) ? chartGenre : parentGenre) ?? "";

		if (node.strSelectBGPath == null) node.strSelectBGPath = parent?.strSelectBGPath;
		if (!File.Exists(node.strSelectBGPath)) node.strSelectBGPath = null;

		ApplyParentSettings(node, parent);

		// The parent's preimage fills in every course that has none.
		if (parent?.score[0] != null) {
			for (int n = 0; n < (int)Difficulty.Total; n++) {
				if (node.score[n] != null && string.IsNullOrEmpty(node.score[n].ChartInfo.Preimage))
					node.score[n].ChartInfo.Preimage = parent.score[0].ChartInfo.Preimage;
			}
		}
	}

	public void ClearPreparse() => _preparsed = null;

	// Walk a search root once: collect .tja paths for the parallel parse and return the count of the
	// other song files (.tci/.tcm). Skips unreadable folders rather than aborting the whole walk.
	private static int CollectCharts(string dir, List<string> tjaInto) {
		int nonTja = 0;
		try {
			foreach (string f in Directory.EnumerateFiles(dir)) {
				string ext = Path.GetExtension(f);
				if (!SongFileExtensions.Contains(ext)) continue;
				if (ext.Equals(".tja", StringComparison.OrdinalIgnoreCase)) tjaInto.Add(Path.GetFullPath(f));
				else nonTja++;
			}
			foreach (string d in Directory.EnumerateDirectories(dir))
				nonTja += CollectCharts(d, tjaInto);
		} catch { /* skip unreadable folders */ }
		return nonTja;
	}

	private void tSongSearchListCreate(string strBaseFolder, bool bChildBOXRecurse, List<CSongListNode> listNodeList, CSongListNode nodeParent, List<(string filePath, List<CSongListNode> nodeList, CSongListNode? parent)>? deferredTcm = null) {
		if (!strBaseFolder.EndsWith(Path.DirectorySeparatorChar))
			strBaseFolder = strBaseFolder + Path.DirectorySeparatorChar;

		DirectoryInfo info = new DirectoryInfo(strBaseFolder);

		if (OpenTaiko.ConfigIni.bOutputSongSearchLog)
			Trace.TraceInformation("基点フォルダ: " + strBaseFolder);

		#region [ Make song nodes from individual chart files ]
		// [ a.フォルダ内に set.def が存在する場合 → 1フォルダ内のtjaファイル無制限] (now non-functional?)
		// [ b.フォルダ内に set.def が存在しない場合 → 個別ファイルからノード作成 ]
		string path = strBaseFolder + "set.def";
		bool hasSetDef = File.Exists(path);

		if (hasSetDef && OpenTaiko.ConfigIni.bOutputSongSearchLog) {
			Trace.TraceInformation("set.def検出 : {0}", path);
			Trace.Indent();
		}

		try {
			foreach (FileInfo fileinfo in info.GetFiles()) {
				SlowOrSuspendSearchTask();      // #27060 中断要求があったら、解除要求が来るまで待機, #PREMOVIE再生中は検索負荷を落とす
				string strExt = fileinfo.Extension.ToLower();

				if (strExt.Equals(".tja")) {
					this.nSearchFileCount++;    // enumeration progress (song files parsed so far)
					// 2017.06.02 kairera0467 廃止。

					#region[ 新処理 ]

					string filePath = strBaseFolder + fileinfo.Name;

					// Use the pre-parse result if possible.
					string hash; CSongListNode? preNode;
					if (_preparsed != null && _preparsed.TryGetValue(fileinfo.FullName, out var pp)) {
						hash = pp.hash; preNode = pp.node;
					} else {
						hash = TjaHash(filePath); preNode = null;
					}

					if (listSongsDB.TryGetValue(filePath + hash, out CSongListNode value)) {
						this.nSearchScoreCount++;
						listNodeList.Add(value);
						CSongDict.tAddSongNode(value.uniqueId, value);
						value.rParentNode = nodeParent;

						if (value.rParentNode != null) {
							value.strScenePresets = value.rParentNode.strScenePresets;
							if (value.rParentNode.IsChangedForeColor) {
								value.ForeColor = value.rParentNode.ForeColor;
								value.IsChangedForeColor = true;
							}
							if (value.rParentNode.IsChangedBackColor) {
								value.BackColor = value.rParentNode.BackColor;
								value.IsChangedBackColor = true;
							}
							if (value.rParentNode.isChangedBoxColor) {
								value.BoxColor = value.rParentNode.BoxColor;
								value.isChangedBoxColor = true;
							}
							if (value.rParentNode.isChangedBgColor) {
								value.BgColor = value.rParentNode.BgColor;
								value.isChangedBgColor = true;
							}
							if (value.rParentNode.isChangedBgType) {
								value.BgType = value.rParentNode.BgType;
								value.isChangedBgType = true;
							}
							if (value.rParentNode.isChangedBoxType) {
								value.BoxType = value.rParentNode.BoxType;
								value.isChangedBoxType = true;
							}
							if (value.rParentNode.isChangedBoxChara) {
								value.BoxChara = value.rParentNode.BoxChara;
								value.isChangedBoxChara = true;
							}
							if (value.rParentNode.isChangedCompat) {
								value.Compat = value.rParentNode.Compat;
								value.isChangedCompat = true;
							}
						}

						this.nSearchSongNodeCount++;
					} else {
						CSongListNode? cSongListNode = preNode ?? ParseUnparentedSongNode(filePath);

						if (cSongListNode != null) {
							ApplySongNodeParent(cSongListNode, nodeParent, filePath);
							CSongDict.tAddSongNode(cSongListNode.uniqueId, cSongListNode);

							this.nSearchScoreCount++;
							listNodeList.Add(cSongListNode);
							if (!listSongsDB.ContainsKey(filePath + hash)) listSongsDB.Add(filePath + hash, cSongListNode);
							this.nSearchSongNodeCount++;
						}
					}
					#endregion
				} else if (strExt.Equals(".optktci") || strExt.Equals(".tci")) {
					this.nSearchFileCount++;    // enumeration progress (song files parsed so far)
					string filePath = strBaseFolder + fileinfo.Name;
					using SHA1 tciHash = SHA1.Create();
					using var tciFs = File.OpenRead(filePath);
					byte[] tciRawHash = tciHash.ComputeHash(tciFs);
					string tciHashStr = string.Concat(tciRawHash.Select(b => b.ToString("X2")));

					if (listSongsDB.TryGetValue(filePath + tciHashStr, out CSongListNode tciCached)) {
						this.nSearchScoreCount++;
						listNodeList.Add(tciCached);
						CSongDict.tAddSongNode(tciCached.uniqueId, tciCached);
						tciCached.rParentNode = nodeParent;
						ApplyParentSettings(tciCached, nodeParent);
						this.nSearchSongNodeCount++;
					} else {
						CTci tci = new CTci(filePath);
						if (tci.Courses.Count > 0) {
							CSongListNode tciNode = tci.BuildSongListNode();
							tciNode.rParentNode = nodeParent;
							tciNode.strBreadcrumbs = (tciNode.rParentNode == null)
								? filePath : tciNode.rParentNode.strBreadcrumbs + " > " + filePath;
							ApplyParentSettings(tciNode, nodeParent);
							CSongDict.tAddSongNode(tciNode.uniqueId, tciNode);
							if (!listSongsDB.ContainsKey(filePath + tciHashStr))
								listSongsDB.Add(filePath + tciHashStr, tciNode);
							listNodeList.Add(tciNode);
							this.nSearchScoreCount++;
							this.nSearchSongNodeCount++;
						}
					}
				} else if ((strExt.Equals(".optktcm") || strExt.Equals(".tcm")) && deferredTcm != null) {
					deferredTcm.Add((strBaseFolder + fileinfo.Name, listNodeList, nodeParent));
				}
			}
		} finally {
			if (hasSetDef && OpenTaiko.ConfigIni.bOutputSongSearchLog) {
				Trace.Unindent();
			}
		}
		#endregion

		foreach (DirectoryInfo infoDir in info.GetDirectories()) {
			SlowOrSuspendSearchTask();      // #27060 中断要求があったら、解除要求が来るまで待機, #PREMOVIE再生中は検索負荷を落とす


			#region [ a.box.def を含むフォルダの場合  ]
			//-----------------------------
			if (File.Exists(infoDir.FullName + @$"{Path.DirectorySeparatorChar}box.def")) {
				CBoxDef boxdef = new CBoxDef(infoDir.FullName + @$"{Path.DirectorySeparatorChar}box.def");
				CSongListNode cSongListNode = new CSongListNode();
				cSongListNode.nodeType = CSongListNode.ENodeType.BOX;
				cSongListNode.ldTitle = boxdef.Title;
				cSongListNode.songGenre = boxdef.Genre;
				cSongListNode.strScenePresets = boxdef.ScenePreset;
				cSongListNode.strSelectBGPath = infoDir.FullName + Path.DirectorySeparatorChar + boxdef.SelectBG;
				if (!File.Exists(cSongListNode.strSelectBGPath)) cSongListNode.strSelectBGPath = null;

				if (boxdef.IsChangedForeColor) {
					cSongListNode.ForeColor = boxdef.ForeColor;
					cSongListNode.IsChangedForeColor = true;
				}
				if (boxdef.IsChangedBackColor) {
					cSongListNode.BackColor = boxdef.BackColor;
					cSongListNode.IsChangedBackColor = true;
				}
				if (boxdef.IsChangedBoxColor) {
					cSongListNode.BoxColor = boxdef.BoxColor;
					cSongListNode.isChangedBoxColor = true;
				}
				if (boxdef.IsChangedBgColor) {
					cSongListNode.BgColor = boxdef.BgColor;
					cSongListNode.isChangedBgColor = true;
				}
				if (boxdef.IsChangedBgType) {
					cSongListNode.BgType = boxdef.BgType;
					cSongListNode.isChangedBgType = true;
				}
				if (boxdef.IsChangedBoxType) {
					cSongListNode.BoxType = boxdef.BoxType;
					cSongListNode.isChangedBoxType = true;
				}
				if (boxdef.IsChangedBoxChara) {
					cSongListNode.BoxChara = boxdef.BoxChara;
					cSongListNode.isChangedBoxChara = true;
				}
				if (boxdef.IsChangedCompat) {
					cSongListNode.Compat = boxdef.Compat;
					cSongListNode.isChangedCompat = true;
				}



				for (int i = 0; i < 3; i++) {
					if ((boxdef.strBoxText[i] != null)) {
						cSongListNode.strBoxText[i] = boxdef.strBoxText[i];
					}
				}



				cSongListNode.difficultiesCount = 1;
				cSongListNode.score[0] = new CScore();
				cSongListNode.score[0].FileInfo.FolderAbsolutePath = infoDir.FullName + Path.DirectorySeparatorChar;
				cSongListNode.score[0].ChartInfo.Title = boxdef.Title.GetString("");
				cSongListNode.score[0].ChartInfo.Genre = boxdef.Genre;
				if (!String.IsNullOrEmpty(boxdef.DefaultPreimage))
					cSongListNode.score[0].ChartInfo.Preimage = boxdef.DefaultPreimage;
				cSongListNode.rParentNode = nodeParent;


				cSongListNode.strBreadcrumbs = (cSongListNode.rParentNode == null) ?
					cSongListNode.ldTitle.GetString("") : cSongListNode.rParentNode.strBreadcrumbs + " > " + cSongListNode.ldTitle.GetString("");


				cSongListNode.childrenList = new List<CSongListNode>();
				// OPTK Shortcut File
				foreach (string shortcutpath in Directory.GetFiles(infoDir.FullName, "*.optksc", SearchOption.TopDirectoryOnly)) {
					cSongListNode.shortcutIds.AddRange(File.ReadAllLines(shortcutpath));
				}

				listNodeList.Add(cSongListNode);
				if (OpenTaiko.ConfigIni.bOutputSongSearchLog) {
					Trace.TraceInformation("box.def検出 : {0}", infoDir.FullName + @$"{Path.DirectorySeparatorChar}box.def");
					Trace.Indent();
					try {
						StringBuilder sb = new StringBuilder(0x400);
						sb.Append(string.Format("nID#{0:D3}", cSongListNode.nID));
						if (cSongListNode.rParentNode != null) {
							sb.Append(string.Format("(in#{0:D3}):", cSongListNode.rParentNode.nID));
						} else {
							sb.Append("(onRoot):");
						}
						sb.Append("BOX, Title=" + cSongListNode.ldTitle.GetString(""));
						if ((cSongListNode.songGenre != null) && (cSongListNode.songGenre.Length > 0)) {
							sb.Append(", Genre=" + cSongListNode.songGenre);
						}
						if (cSongListNode.IsChangedForeColor) {
							sb.Append(", ForeColor=" + cSongListNode.ForeColor.ToString());
						}
						if (cSongListNode.IsChangedBackColor) {
							sb.Append(", BackColor=" + cSongListNode.BackColor.ToString());
						}
						if (cSongListNode.isChangedBoxColor) {
							sb.Append(", BoxColor=" + cSongListNode.BoxColor.ToString());
						}
						if (cSongListNode.isChangedBgColor) {
							sb.Append(", BgColor=" + cSongListNode.BgColor.ToString());
						}
						if (cSongListNode.isChangedBoxType) {
							sb.Append(", BoxType=" + cSongListNode.BoxType.ToString());
						}
						if (cSongListNode.isChangedBgType) {
							sb.Append(", BgType=" + cSongListNode.BgType.ToString());
						}
						if (cSongListNode.isChangedBoxChara) {
							sb.Append(", BoxChara=" + cSongListNode.BoxChara.ToString());
						}
						if (cSongListNode.isChangedCompat) {
							sb.Append(", Compat=" + cSongListNode.Compat.ToString());
						}
						Trace.TraceInformation(sb.ToString());
					} finally {
						Trace.Unindent();
					}
				}
				if (bChildBOXRecurse) {
					this.tSongSearchListCreate(infoDir.FullName + Path.DirectorySeparatorChar, bChildBOXRecurse, cSongListNode.childrenList, cSongListNode, deferredTcm);
				}
			}
			//-----------------------------
			#endregion

			#region [ c.通常フォルダの場合 ]
			//-----------------------------
			else {
				this.tSongSearchListCreate(infoDir.FullName + Path.DirectorySeparatorChar, bChildBOXRecurse, listNodeList, nodeParent, deferredTcm);
			}
			//-----------------------------
			#endregion
		}
	}
	//-----------------
	#endregion

	private void LoadChartInfo(CSongListNode cSongListNode, CTja cdtx, int i) {
		if ((cSongListNode.score[i] != null) && !cSongListNode.score[i].bHadCacheInSongDB) {
			#region [ DTX ファイルのヘッダだけ読み込み、Cスコア.譜面情報 を設定する ]
			//-----------------
			string path = cSongListNode.score[i].FileInfo.FileAbsolutePath;
			if (File.Exists(path)) {
				try {
					cSongListNode.score[i].ChartInfo.Title = cdtx.TITLE.GetString("");


					cSongListNode.score[i].ChartInfo.ArtistName = cdtx.ARTIST;
					cSongListNode.score[i].ChartInfo.Comment = cdtx.COMMENT;
					cSongListNode.score[i].ChartInfo.Genre = cdtx.GENRE;
					if (!String.IsNullOrEmpty(cdtx.PREIMAGE))
						cSongListNode.score[i].ChartInfo.Preimage = cdtx.PREIMAGE;
					cSongListNode.score[i].ChartInfo.Presound = cdtx.PREVIEW;
					cSongListNode.score[i].ChartInfo.Backgound = cdtx.BACKGROUND;
					cSongListNode.score[i].ChartInfo.Level = cdtx.LEVEL;
					cSongListNode.score[i].ChartInfo.LevelHide = cdtx.HIDDENLEVEL;
					cSongListNode.score[i].ChartInfo.Bpm = cdtx.BPM;
					cSongListNode.score[i].ChartInfo.BaseBpm = cdtx.BASEBPM;
					cSongListNode.score[i].ChartInfo.MinBpm = cdtx.MinBPM;
					cSongListNode.score[i].ChartInfo.MaxBpm = cdtx.MaxBPM;
					cSongListNode.score[i].ChartInfo.Duration = 0;    //  (cdtx.listChip == null)? 0 : cdtx.listChip[ cdtx.listChip.Count - 1 ].n発声時刻ms;
					cSongListNode.score[i].ChartInfo.strBGMFileName = cdtx.strBGM_PATH;
					cSongListNode.score[i].ChartInfo.SongVol = cdtx.SongVol;
					cSongListNode.score[i].ChartInfo.SongLoudnessMetadata = cdtx.SongLoudnessMetadata;
					cSongListNode.score[i].ChartInfo.nDemoBGMOffset = cdtx.nDemoBGMOffset;
					cSongListNode.score[i].ChartInfo.strSubtitle = cdtx.SUBTITLE.GetString("");
					for (int k = 0; k < (int)Difficulty.Total; k++) {
						cSongListNode.score[i].ChartInfo.bChartBranch[k] = cdtx.SongListCourseMetadata[k].bHIDDENBRANCH ? false : cdtx.SongListCourseMetadata[k].bHasBranch;
						cSongListNode.score[i].ChartInfo.nLevel[k] = cdtx.SongListCourseMetadata[k].LEVELtaiko;
						cSongListNode.score[i].ChartInfo.nLevelIcon[k] = cdtx.SongListCourseMetadata[k].LEVELtaikoIcon;
					}

					// Tower Lives
					cSongListNode.score[i].ChartInfo.nLife = cdtx.LIFE;

					cSongListNode.score[i].ChartInfo.nTowerType = cdtx.TOWERTYPE;

					cSongListNode.score[i].ChartInfo.nDanTick = cdtx.DANTICK;
					cSongListNode.score[i].ChartInfo.cDanTickColor = cdtx.DANTICKCOLOR;

					cSongListNode.score[i].ChartInfo.nTotalFloor = 0;
					if ((Difficulty)i is Difficulty.Tower) {
						for (int k = 0; k < cdtx.listChip.Count; k++) {
							CChip pChip = cdtx.listChip[k];

							if (pChip.nIntValue_InternalNumber > cSongListNode.score[i].ChartInfo.nTotalFloor && pChip.nChannelNo == 0x50)
								cSongListNode.score[i].ChartInfo.nTotalFloor = pChip.nIntValue_InternalNumber;
						}
						cSongListNode.score[i].ChartInfo.nTotalFloor++;
					}



					Interlocked.Increment(ref _nScoresAppliedFromFile);
					cdtx.DeActivate();
					#region [ 曲検索ログ出力 ]
					//-----------------
					if (OpenTaiko.ConfigIni.bOutputSongSearchLog) {
						StringBuilder sb = new StringBuilder(0x400);
						sb.Append(string.Format("曲データファイルから譜面情報を転記しました。({0})", path));
						sb.Append("(title=" + cSongListNode.score[i].ChartInfo.Title);
						sb.Append(", artist=" + cSongListNode.score[i].ChartInfo.ArtistName);
						sb.Append(", comment=" + cSongListNode.score[i].ChartInfo.Comment);
						sb.Append(", genre=" + cSongListNode.score[i].ChartInfo.Genre);
						sb.Append(", preimage=" + cSongListNode.score[i].ChartInfo.Preimage);
						sb.Append(", premovie=" + cSongListNode.score[i].ChartInfo.Premovie);
						sb.Append(", presound=" + cSongListNode.score[i].ChartInfo.Presound);
						sb.Append(", background=" + cSongListNode.score[i].ChartInfo.Backgound);
						sb.Append(", lvDr=" + cSongListNode.score[i].ChartInfo.Level);
						sb.Append(", lvHide=" + cSongListNode.score[i].ChartInfo.LevelHide);
						sb.Append(", bpm=" + cSongListNode.score[i].ChartInfo.Bpm);
						sb.Append(", basebpm=" + cSongListNode.score[i].ChartInfo.BaseBpm);
						sb.Append(", minbpm=" + cSongListNode.score[i].ChartInfo.MinBpm);
						sb.Append(", maxbpm=" + cSongListNode.score[i].ChartInfo.MaxBpm);
						//	sb.Append( ", duration=" + c曲リストノード.arスコア[ i ].譜面情報.Duration );
						Trace.TraceInformation(sb.ToString());
					}
					//-----------------
					#endregion
				} catch (Exception exception) {
					Trace.TraceError(exception.ToString());
					cSongListNode.score[i] = null;
					cSongListNode.difficultiesCount--;
					this.nSearchScoreCount--;
					Trace.TraceError("曲データファイルの読み込みに失敗しました。({0})", path);
				}
			}
			//-----------------
			#endregion
		}
	}

	#region [ 曲リストへ後処理を適用する ]
	//-----------------
	public static int DistanceFromRoots(CSongListNode? node, HashSet<CSongListNode>? roots = null) {
		int dist = 0;
		for (; node != null; node = node.rParentNode, ++dist) {
			if (roots != null && roots.Contains(node))
				return dist;
		}
		if (roots == null) // from the root song folder
			return dist;
		return -1; // negative for unreachable
	}

	private void ResetSongRoots() {
		this.SongRootDownload = null;
		this.SongRootsDan = [];
		this.listSongRoot_Dan = [];
		this.SongRootsTower = [];
		this.listSongRoot_Tower = [];
	}

	public void tSongListPostprocessing() {
		listStrBoxDefSkinSubfolderFullName = new List<string>();
		if (OpenTaiko.Skin.strBoxDefSkinSubfolders != null) {
			foreach (string b in OpenTaiko.Skin.strBoxDefSkinSubfolders) {
				listStrBoxDefSkinSubfolderFullName.Add(b);
			}
		}

		this.ResetSongRoots();
		this.tSongListPostprocessing(this.listSongRoot);

		for (int p = 0; p < listSongRoot.Count; p++) {
			var cSongListNode = listSongRoot[p];
			if (cSongListNode.nodeType == CSongListNode.ENodeType.BOX) {
				if (cSongListNode.songGenre == "段位道場") {
					if (OpenTaiko.ConfigIni.bDanTowerHide) {
						listSongRoot.Remove(cSongListNode);
						p--;
					}

					// Add to dojo
					if (DistanceFromRoots(cSongListNode, this.SongRootsDan) < 0) {
						this.SongRootsDan.Add(cSongListNode);
						listSongRoot_Dan.AddRange(cSongListNode.childrenList);
					}
				} else if (cSongListNode.songGenre == "太鼓タワー") {
					if (OpenTaiko.ConfigIni.bDanTowerHide) {
						listSongRoot.Remove(cSongListNode);
						p--;
					}

					if (DistanceFromRoots(cSongListNode, this.SongRootsTower) < 0) {
						this.SongRootsTower.Add(cSongListNode);
						listSongRoot_Tower.AddRange(cSongListNode.childrenList);
					}
				} else if (cSongListNode.songGenre == "Download") {
					// use the closest from the root song folder
					if (this.SongRootDownload == null || DistanceFromRoots(cSongListNode) < DistanceFromRoots(this.SongRootDownload))
						this.SongRootDownload = cSongListNode;
				} else {
					for (int i = 0; i < cSongListNode.childrenList.Count; i++) {
						if (cSongListNode.childrenList[i].score[6] != null) {
							listSongRoot_Dan.Add(cSongListNode.childrenList[i]);

							if (OpenTaiko.ConfigIni.bDanTowerHide)
								cSongListNode.childrenList.Remove(cSongListNode.childrenList[i]);

							continue;
						}
						if (cSongListNode.childrenList[i].score[5] != null) {
							listSongRoot_Tower.Add(cSongListNode.childrenList[i]);

							if (OpenTaiko.ConfigIni.bDanTowerHide)
								cSongListNode.childrenList.Remove(cSongListNode.childrenList[i]);
							continue;
						}
					}
				}
			}
		}

		#region [ skin名で比較して、systemスキンとboxdefスキンに重複があれば、boxdefスキン側を削除する ]
		string[] systemSkinNames = CSkin.GetSkinName(OpenTaiko.Skin.strSystemSkinSubfolders);
		List<string> l = new List<string>(listStrBoxDefSkinSubfolderFullName);
		foreach (string boxdefSkinSubfolderFullName in l) {
			if (Array.BinarySearch(systemSkinNames,
					CSkin.GetSkinName(boxdefSkinSubfolderFullName),
					StringComparer.InvariantCultureIgnoreCase) >= 0) {
				listStrBoxDefSkinSubfolderFullName.Remove(boxdefSkinSubfolderFullName);
			}
		}
		#endregion
		string[] ba = listStrBoxDefSkinSubfolderFullName.ToArray();
		Array.Sort(ba);
		OpenTaiko.Skin.strBoxDefSkinSubfolders = ba;
	}


	private void tSongListPostprocessing(List<CSongListNode> nodeList, string parentName = "/", bool isGlobal = true) {

		if (isGlobal && nodeList.Count > 0) {
			var randomNode = CSongDict.tGenerateRandomButton(nodeList[0].rParentNode, parentName);
			nodeList.Add(randomNode);

		}

		// Don't sort songs if the folder isn't global
		// Call back reinsert back folders if sort called ?
		if (isGlobal) {
			tSongListSortByPath(nodeList);
		}

		// すべてのノードについて…
		foreach (CSongListNode songNode in nodeList) {
			SlowOrSuspendSearchTask();      // #27060 中断要求があったら、解除要求が来るまで待機, #PREMOVIE再生中は検索負荷を落とす

			#region [ Append "Back" buttons to the included folders ]
			//-----------------------------
			if (songNode.nodeType == CSongListNode.ENodeType.BOX) {

				tSongListSortByPath(songNode.childrenList);

				string newPath = parentName + songNode.ldTitle.GetString("") + "/";

				CSongDict.tReinsertBackButtons(songNode, songNode.childrenList, newPath, listStrBoxDefSkinSubfolderFullName);

				// Process subfolders recussively
				tSongListPostprocessing(songNode.childrenList, newPath, false);

				continue;
			}

			//-----------------------------
			#endregion

			#region [ If no node title found, try to fetch it within the score objects ]
			//-----------------------------
			if (string.IsNullOrEmpty(songNode.ldTitle.GetString(""))) {
				for (int j = 0; j < (int)Difficulty.Total; j++) {
					if ((songNode.score[j] != null) && !string.IsNullOrEmpty(songNode.score[j].ChartInfo.Title)) {
						songNode.ldTitle = new CLocalizationData();

						if (OpenTaiko.ConfigIni.bOutputSongSearchLog)
							Trace.TraceInformation("タイトルを設定しました。(nID#{0:D3}, title={1})", songNode.nID, songNode.ldTitle.GetString(""));

						break;
					}
				}
			}
			//-----------------------------
			#endregion



		}

	}
	//-----------------
	#endregion

	#region [ Sort Song List ]
	//-----------------

	public static void tSongListSortByPath(List<CSongListNode> nodeList) {
		tSongListSortByPath(nodeList, 1, 0);

		foreach (CSongListNode songNode in nodeList) {
			if ((songNode.childrenList != null) && (songNode.childrenList.Count > 1)) {
				tSongListSortByPath(songNode.childrenList);
			}
		}
	}

	public static void tSongListSortByPath(List<CSongListNode> nodeList, int order, params object[] p) {
		var comparer = new ComparerChain<CSongListNode>(
			new CSongListNodeComparerNodeType(),
			new CSongListNodeComparerUnlockStatus(),
			new CSongListNodeComparerPath(order),
			new CSongListNodeComparerTitle(order),
			new CSongListNodeComparerSubtitle(order));

		nodeList.Sort(comparer);
	}

	public static void tSongListSortByTitle(List<CSongListNode> nodeList, int order, params object[] p) {
		var comparer = new ComparerChain<CSongListNode>(
			new CSongListNodeComparerNodeType(),
			new CSongListNodeComparerUnlockStatus(),
			new CSongListNodeComparerTitle(order),
			new CSongListNodeComparerSubtitle(order),
			new CSongListNodeComparerPath(order));

		nodeList.Sort(comparer);
	}

	public static void tSongListSortBySubtitle(List<CSongListNode> nodeList, int order, params object[] p) {
		var comparer = new ComparerChain<CSongListNode>(
			new CSongListNodeComparerNodeType(),
			new CSongListNodeComparerUnlockStatus(),
			new CSongListNodeComparerSubtitle(order),
			new CSongListNodeComparerTitle(order),
			new CSongListNodeComparerPath(order));

		nodeList.Sort(comparer);
	}

	public static void tSongListSortByLevel(List<CSongListNode> nodeList, int order, params object[] p) {
		var comparer = new ComparerChain<CSongListNode>(
			new CSongListNodeComparerNodeType(),
			new CSongListNodeComparerUnlockStatus(),
			new CSongListNodeComparerLevel(order),
			new CSongListNodeComparerLevelIcon(order),
			new CSongListNodeComparerTitle(order),
			new CSongListNodeComparerSubtitle(order),
			new CSongListNodeComparerPath(order));

		nodeList.Sort(comparer);
	}


#if TEST_SORTBGM
		public static void t曲リストのソート9_BPM順( List<C曲リストノード> ノードリスト, E楽器パート part, int order, params object[] p )
		{
			order = -order;
			int nL12345 = (int) p[ 0 ];
			if ( part != E楽器パート.UNKNOWN )
			{
				ノードリスト.Sort( delegate( C曲リストノード n1, C曲リストノード n2 )
				{
		#region [ 共通処理 ]
					if ( n1 == n2 )
					{
						return 0;
					}
					int num = this.t比較0_共通( n1, n2 );
					if ( num != 0 )
					{
						return num;
					}
					if ( ( n1.eノード種別 == C曲リストノード.Eノード種別.BOX ) && ( n2.eノード種別 == C曲リストノード.Eノード種別.BOX ) )
					{
						return order * n1.arスコア[ 0 ].ファイル情報.フォルダの絶対パス.CompareTo( n2.arスコア[ 0 ].ファイル情報.フォルダの絶対パス );
					}
		#endregion
					double dBPMn1 = 0.0, dBPMn2 = 0.0;
					if ( n1.arスコア[ nL12345 ] != null )
					{
						dBPMn1 = n1.arスコア[ nL12345 ].譜面情報.bpm;
					}
					if ( n2.arスコア[ nL12345 ] != null )
					{
						dBPMn2 = n2.arスコア[ nL12345 ].譜面情報.bpm;
					}
					double d = dBPMn1- dBPMn2;
					if ( d != 0 )
					{
						return order * System.Math.Sign( d );
					}
					return order * n1.strタイトル.CompareTo( n2.strタイトル );
				} );
				foreach ( C曲リストノード c曲リストノード in ノードリスト )
				{
					double dBPM = 0;
					if ( c曲リストノード.arスコア[ nL12345 ] != null )
					{
						dBPM = c曲リストノード.arスコア[ nL12345 ].譜面情報.bpm;
					}
Debug.WriteLine( dBPM + ":" + c曲リストノード.strタイトル );
				}
			}
		}
#endif
	//-----------------
	#endregion

	// その他


	#region [ private ]
	//-----------------
	public List<string> listStrBoxDefSkinSubfolderFullName {
		get;
		private set;
	}

	/// <summary>
	/// 検索を中断_スローダウンする
	/// </summary>
	private void SlowOrSuspendSearchTask() {
		if (this.bIsCanceled)
			throw new OperationCanceledException();
		if (this.bIsSuspending)     // #27060 中断要求があったら、解除要求が来るまで待機
		{
			AutoReset.WaitOne();
		}
		if (this.bIsSlowdown && ++this.searchCount > 10)            // #27060 #PREMOVIE再生中は検索負荷を下げる
		{
			Thread.Sleep(100);
			this.searchCount = 0;
		}
	}

	//-----------------
	#endregion

	private void ProcessDeferredTcm(List<(string filePath, List<CSongListNode> nodeList, CSongListNode? parent)> deferred) {
		foreach (var (filePath, nodeList, parent) in deferred) {
			SlowOrSuspendSearchTask();
			this.nSearchFileCount++;    // enumeration progress (deferred .tcm song files parsed so far)
			try {
				using SHA1 tcmHash = SHA1.Create();
				using var tcmFs = File.OpenRead(filePath);
				byte[] tcmRawHash = tcmHash.ComputeHash(tcmFs);
				string tcmHashStr = string.Concat(tcmRawHash.Select(b => b.ToString("X2")));

				if (listSongsDB.TryGetValue(filePath + tcmHashStr, out CSongListNode tcmCached)) {
					this.nSearchScoreCount++;
					nodeList.Add(tcmCached);
					CSongDict.tAddSongNode(tcmCached.uniqueId, tcmCached);
					tcmCached.rParentNode = parent;
					ApplyParentSettings(tcmCached, parent);
					this.nSearchSongNodeCount++;
				} else {
					CTcm tcm = new CTcm(filePath);
					CSongListNode? tcmNode = tcm.BuildSongListNode();
					if (tcmNode != null) {
						tcmNode.rParentNode = parent;
						tcmNode.strBreadcrumbs = (tcmNode.rParentNode == null)
							? filePath : tcmNode.rParentNode.strBreadcrumbs + " > " + filePath;
						ApplyParentSettings(tcmNode, parent);
						CSongDict.tAddSongNode(tcmNode.uniqueId, tcmNode);
						if (!listSongsDB.ContainsKey(filePath + tcmHashStr))
							listSongsDB.Add(filePath + tcmHashStr, tcmNode);
						nodeList.Add(tcmNode);
						this.nSearchScoreCount++;
						this.nSearchSongNodeCount++;
					}
				}
			} catch (Exception ex) {
				Trace.TraceError($"[CSongs管理] Failed to process TCM file '{filePath}': {ex}");
			}
		}
	}

	private static void ApplyParentSettings(CSongListNode node, CSongListNode? parent) {
		if (parent == null) return;
		node.strScenePresets = parent.strScenePresets;
		if (parent.IsChangedForeColor) { node.ForeColor = parent.ForeColor; node.IsChangedForeColor = true; }
		if (parent.IsChangedBackColor) { node.BackColor = parent.BackColor; node.IsChangedBackColor = true; }
		if (parent.isChangedBoxColor) { node.BoxColor = parent.BoxColor; node.isChangedBoxColor = true; }
		if (parent.isChangedBgColor) { node.BgColor = parent.BgColor; node.isChangedBgColor = true; }
		if (parent.isChangedBgType) { node.BgType = parent.BgType; node.isChangedBgType = true; }
		if (parent.isChangedBoxType) { node.BoxType = parent.BoxType; node.isChangedBoxType = true; }
		if (parent.isChangedBoxChara) { node.BoxChara = parent.BoxChara; node.isChangedBoxChara = true; }
		if (node.score[0] != null && parent.score[0] != null && string.IsNullOrEmpty(node.score[0].ChartInfo.Preimage))
			node.score[0].ChartInfo.Preimage = parent.score[0].ChartInfo.Preimage;
	}

}
