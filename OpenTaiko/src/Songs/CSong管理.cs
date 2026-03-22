using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using OpenTaiko.CSongListNodeComparers;

namespace OpenTaiko;

[Serializable]
internal class CSongs管理 {
	// Properties

	public int nスコアキャッシュから反映できたスコア数 {
		get;
		set;
	}
	public int nファイルから反映できたスコア数 {
		get;
		set;
	}
	public int n検索されたスコア数 {
		get;
		set;
	}
	public int n検索された曲ノード数 {
		get;
		set;
	}
	public Dictionary<string, CSongListNode> listSongsDB;                   // songs.dbから構築されるlist
	public List<CSongListNode> list曲ルート;         // 起動時にフォルダ検索して構築されるlist
	public List<CSongListNode> list曲ルート_Dan = new List<CSongListNode>();          // 起動時にフォルダ検索して構築されるlist
	public List<CSongListNode> list曲ルート_Tower = new List<CSongListNode>();          // 起動時にフォルダ検索して構築されるlist
	public static List<FDK.CTexture> listCustomBGs = new List<FDK.CTexture>();
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

	public CSongs管理() {
		this.listSongsDB = new();
		this.list曲ルート = new List<CSongListNode>();
		this.n検索された曲ノード数 = 0;
		this.n検索されたスコア数 = 0;
		this.bIsSuspending = false;                     // #27060
		this.AutoReset = new AutoResetEvent(true);  // #27060
		this.searchCount = 0;
	}


	// メソッド

	#region [ Fetch song list ]
	//-----------------

	public void UpdateDownloadBox() {

		CSongListNode downloadBox = null;
		for (int i = 0; i < OpenTaiko.Songs管理.list曲ルート.Count; i++) {
			if (OpenTaiko.Songs管理.list曲ルート[i].songGenre == "Download") {
				downloadBox = OpenTaiko.Songs管理.list曲ルート[i];
				if (downloadBox.rParentNode != null) downloadBox = downloadBox.rParentNode;
			}

		}

		if (downloadBox != null && downloadBox.childrenList != null) {

			var flatten = OpenTaiko.stageSongSelect.actSongList.flattenList(downloadBox.childrenList);

			// Works because flattenList creates a new List
			for (int i = 0; i < downloadBox.childrenList.Count; i++) {
				CSongDict.tRemoveSongNode(downloadBox.childrenList[i].uniqueId);
				downloadBox.childrenList.Remove(downloadBox.childrenList[i]);
				i--;
			}


			var path = downloadBox.score[0].ファイル情報.フォルダの絶対パス;

			if (flatten.Count > 0) {
				int index = list曲ルート.IndexOf(flatten[0]);
				if (!list曲ルート.Contains(downloadBox)) {
					this.list曲ルート = this.list曲ルート.Except(flatten).ToList();
					list曲ルート.Insert(index, downloadBox);
				}

				t曲を検索してリストを作成する(path, true, downloadBox.childrenList, downloadBox);
				this.tSongListPostprocessing(downloadBox.childrenList, $"/{downloadBox.ldTitle.GetString("")}/");
				downloadBox.childrenList.Insert(0, CSongDict.tGenerateBackButton(downloadBox, $"/{downloadBox.ldTitle.GetString("")}/"));
			}
		}

	}
	public void t曲を検索してリストを作成する(string str基点フォルダ, bool b子BOXへ再帰する) {
		this.t曲を検索してリストを作成する(str基点フォルダ, b子BOXへ再帰する, this.list曲ルート, null);
	}
	private void t曲を検索してリストを作成する(string str基点フォルダ, bool b子BOXへ再帰する, List<CSongListNode> listノードリスト, CSongListNode node親) {
		if (!str基点フォルダ.EndsWith(Path.DirectorySeparatorChar))
			str基点フォルダ = str基点フォルダ + Path.DirectorySeparatorChar;

		DirectoryInfo info = new DirectoryInfo(str基点フォルダ);

		if (OpenTaiko.ConfigIni.bOutputSongSearchLog)
			Trace.TraceInformation("基点フォルダ: " + str基点フォルダ);

		#region [ Make song nodes from individual chart files ]
		// [ a.フォルダ内に set.def が存在する場合 → 1フォルダ内のtjaファイル無制限] (now non-functional?)
		// [ b.フォルダ内に set.def が存在しない場合 → 個別ファイルからノード作成 ]
		string path = str基点フォルダ + "set.def";
		bool hasSetDef = File.Exists(path);

		if (hasSetDef && OpenTaiko.ConfigIni.bOutputSongSearchLog) {
			Trace.TraceInformation("set.def検出 : {0}", path);
			Trace.Indent();
		}

		try {
			foreach (FileInfo fileinfo in info.GetFiles()) {
				SlowOrSuspendSearchTask();      // #27060 中断要求があったら、解除要求が来るまで待機, #PREMOVIE再生中は検索負荷を落とす
				string strExt = fileinfo.Extension.ToLower();

				if ((strExt.Equals(".tja") || strExt.Equals(".dtx"))) {
					// 2017.06.02 kairera0467 廃止。

					#region[ 新処理 ]

					string filePath = str基点フォルダ + fileinfo.Name;

					using SHA1 hashProvider = SHA1.Create();
					var fs = File.OpenRead(filePath);
					byte[] rawhash = hashProvider.ComputeHash(fs);
					string hash = "";
					for (int i = 0; i < rawhash.Length; i++) {
						hash += string.Format("{0:X2}", rawhash[i]);
					}

					fs.Dispose();

					if (listSongsDB.TryGetValue(filePath + hash, out CSongListNode value)) {
						this.n検索されたスコア数++;
						listノードリスト.Add(value);
						CSongDict.tAddSongNode(value.uniqueId, value);
						value.rParentNode = node親;

						if (value.rParentNode != null) {
							value.strScenePreset = value.rParentNode.strScenePreset;
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
						}

						this.n検索された曲ノード数++;
					} else {
						CTja dtx = new CTja(filePath);
						CSongListNode c曲リストノード = new CSongListNode();
						c曲リストノード.nodeType = CSongListNode.ENodeType.SCORE;

						bool hasAnyDifficultyProcessed = false;
						for (int n = 0; n < (int)Difficulty.Total; n++) {
							if (dtx.b譜面が存在する[n]) {
								c曲リストノード.difficultiesCount++;
								c曲リストノード.rParentNode = node親;
								c曲リストノード.strBreadcrumbs = (c曲リストノード.rParentNode == null) ?
									str基点フォルダ + fileinfo.Name : c曲リストノード.rParentNode.strBreadcrumbs + " > " + str基点フォルダ + fileinfo.Name;

								c曲リストノード.ldTitle = dtx.TITLE;
								c曲リストノード.ldSubtitle = dtx.SUBTITLE;
								c曲リストノード.strMaker = dtx.MAKER;
								c曲リストノード.strNotesDesigner = dtx.NOTESDESIGNER.Select(x => x.Equals("") ? c曲リストノード.strMaker : x).ToArray();
								c曲リストノード.nSide = dtx.SIDE;
								c曲リストノード.bExplicit = dtx.EXPLICIT;
								c曲リストノード.bMovie = !string.IsNullOrEmpty(dtx.strBGVIDEO_PATH);

								c曲リストノード.DanSongs = new();
								if (dtx.List_DanSongs != null) {
									for (int i = 0; i < dtx.List_DanSongs.Count; i++) {
										c曲リストノード.DanSongs.Add(dtx.List_DanSongs[i]);
									}
								}

								if (dtx.Dan_C != null)
									c曲リストノード.Dan_C = dtx.Dan_C;

								string? songGenreParent = string.IsNullOrEmpty(c曲リストノード.rParentNode?.songGenre) ? null
									: c曲リストノード.rParentNode.songGenre;
								c曲リストノード.songGenre = songGenreParent ?? dtx.GENRE ?? "";
								c曲リストノード.songGenrePanel = (!string.IsNullOrEmpty(dtx.GENRE) ? dtx.GENRE : songGenreParent) ?? "";

								if (!(dtx.SELECTBG != null && File.Exists(str基点フォルダ + dtx.SELECTBG))) {
									c曲リストノード.strSelectBGPath = c曲リストノード.rParentNode?.strSelectBGPath;
								} else {
									c曲リストノード.strSelectBGPath = str基点フォルダ + dtx.SELECTBG;
								}
								if (!File.Exists(c曲リストノード.strSelectBGPath)) c曲リストノード.strSelectBGPath = null;

								if (c曲リストノード.rParentNode != null) {
									c曲リストノード.strScenePreset = c曲リストノード.rParentNode.strScenePreset;
									if (c曲リストノード.rParentNode.IsChangedForeColor) {
										c曲リストノード.ForeColor = c曲リストノード.rParentNode.ForeColor;
										c曲リストノード.IsChangedForeColor = true;
									}
									if (c曲リストノード.rParentNode.IsChangedBackColor) {
										c曲リストノード.BackColor = c曲リストノード.rParentNode.BackColor;
										c曲リストノード.IsChangedBackColor = true;
									}
									if (c曲リストノード.rParentNode.isChangedBoxColor) {
										c曲リストノード.BoxColor = c曲リストノード.rParentNode.BoxColor;
										c曲リストノード.isChangedBoxColor = true;
									}
									if (c曲リストノード.rParentNode.isChangedBgColor) {
										c曲リストノード.BgColor = c曲リストノード.rParentNode.BgColor;
										c曲リストノード.isChangedBgColor = true;
									}
									if (c曲リストノード.rParentNode.isChangedBgType) {
										c曲リストノード.BgType = c曲リストノード.rParentNode.BgType;
										c曲リストノード.isChangedBgType = true;
									}
									if (c曲リストノード.rParentNode.isChangedBoxType) {
										c曲リストノード.BoxType = c曲リストノード.rParentNode.BoxType;
										c曲リストノード.isChangedBoxType = true;
									}
									if (c曲リストノード.rParentNode.isChangedBoxChara) {
										c曲リストノード.BoxChara = c曲リストノード.rParentNode.BoxChara;
										c曲リストノード.isChangedBoxChara = true;
									}


								}


								switch (CStrジャンルtoNum.ForAC15(c曲リストノード.songGenre)) {
									case 0:
										c曲リストノード.ForeColor = OpenTaiko.Skin.SongSelect_ForeColor_JPOP;
										c曲リストノード.BackColor = OpenTaiko.Skin.SongSelect_BackColor_JPOP;
										break;
									case 1:
										c曲リストノード.ForeColor = OpenTaiko.Skin.SongSelect_ForeColor_Anime;
										c曲リストノード.BackColor = OpenTaiko.Skin.SongSelect_BackColor_Anime;
										break;
									case 2:
										c曲リストノード.ForeColor = OpenTaiko.Skin.SongSelect_ForeColor_VOCALOID;
										c曲リストノード.BackColor = OpenTaiko.Skin.SongSelect_BackColor_VOCALOID;
										break;
									case 3:
										c曲リストノード.ForeColor = OpenTaiko.Skin.SongSelect_ForeColor_Children;
										c曲リストノード.BackColor = OpenTaiko.Skin.SongSelect_BackColor_Children;
										break;
									case 4:
										c曲リストノード.ForeColor = OpenTaiko.Skin.SongSelect_ForeColor_Variety;
										c曲リストノード.BackColor = OpenTaiko.Skin.SongSelect_BackColor_Variety;
										break;
									case 5:
										c曲リストノード.ForeColor = OpenTaiko.Skin.SongSelect_ForeColor_Classic;
										c曲リストノード.BackColor = OpenTaiko.Skin.SongSelect_BackColor_Classic;
										break;
									case 6:
										c曲リストノード.ForeColor = OpenTaiko.Skin.SongSelect_ForeColor_GameMusic;
										c曲リストノード.BackColor = OpenTaiko.Skin.SongSelect_BackColor_GameMusic;
										break;
									case 7:
										c曲リストノード.ForeColor = OpenTaiko.Skin.SongSelect_ForeColor_Namco;
										c曲リストノード.BackColor = OpenTaiko.Skin.SongSelect_BackColor_Namco;
										break;
									default:
										break;
								}


								c曲リストノード.nLevel = dtx.LEVELtaiko;
								c曲リストノード.nLevelIcon = dtx.LEVELtaikoIcon;
								c曲リストノード.uniqueId = dtx.uniqueID;

								c曲リストノード.CutSceneIntro = dtx.CutSceneIntro;
								c曲リストノード.CutSceneOutros = dtx.CutSceneOutros;

								CSongDict.tAddSongNode(c曲リストノード.uniqueId, c曲リストノード);

								c曲リストノード.score[n] = new CScore();
								c曲リストノード.score[n].ファイル情報.ファイルの絶対パス = str基点フォルダ + fileinfo.Name;
								c曲リストノード.score[n].ファイル情報.フォルダの絶対パス = str基点フォルダ;
								c曲リストノード.score[n].ファイル情報.ファイルサイズ = fileinfo.Length;
								c曲リストノード.score[n].ファイル情報.最終更新日時 = fileinfo.LastWriteTime;

								if (c曲リストノード.rParentNode != null && String.IsNullOrEmpty(c曲リストノード.score[n].譜面情報.Preimage)) {
									c曲リストノード.score[n].譜面情報.Preimage = c曲リストノード.rParentNode.score[0].譜面情報.Preimage;
								}

								LoadChartInfo(c曲リストノード, dtx, n);

								if (hasAnyDifficultyProcessed == false) {
									this.n検索されたスコア数++;
									listノードリスト.Add(c曲リストノード);
									if (!listSongsDB.ContainsKey(filePath + hash)) listSongsDB.Add(filePath + hash, c曲リストノード);
									this.n検索された曲ノード数++;
									hasAnyDifficultyProcessed = true;
								}
							}
						}
					}
					#endregion
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
				CSongListNode c曲リストノード = new CSongListNode();
				c曲リストノード.nodeType = CSongListNode.ENodeType.BOX;
				c曲リストノード.ldTitle = boxdef.Title;
				c曲リストノード.songGenre = boxdef.Genre;
				c曲リストノード.strScenePreset = boxdef.ScenePreset;
				c曲リストノード.strSelectBGPath = infoDir.FullName + Path.DirectorySeparatorChar + boxdef.SelectBG;
				if (!File.Exists(c曲リストノード.strSelectBGPath)) c曲リストノード.strSelectBGPath = null;

				if (boxdef.IsChangedForeColor) {
					c曲リストノード.ForeColor = boxdef.ForeColor;
					c曲リストノード.IsChangedForeColor = true;
				}
				if (boxdef.IsChangedBackColor) {
					c曲リストノード.BackColor = boxdef.BackColor;
					c曲リストノード.IsChangedBackColor = true;
				}
				if (boxdef.IsChangedBoxColor) {
					c曲リストノード.BoxColor = boxdef.BoxColor;
					c曲リストノード.isChangedBoxColor = true;
				}
				if (boxdef.IsChangedBgColor) {
					c曲リストノード.BgColor = boxdef.BgColor;
					c曲リストノード.isChangedBgColor = true;
				}
				if (boxdef.IsChangedBgType) {
					c曲リストノード.BgType = boxdef.BgType;
					c曲リストノード.isChangedBgType = true;
				}
				if (boxdef.IsChangedBoxType) {
					c曲リストノード.BoxType = boxdef.BoxType;
					c曲リストノード.isChangedBoxType = true;
				}
				if (boxdef.IsChangedBoxChara) {
					c曲リストノード.BoxChara = boxdef.BoxChara;
					c曲リストノード.isChangedBoxChara = true;
				}



				for (int i = 0; i < 3; i++) {
					if ((boxdef.strBoxText[i] != null)) {
						c曲リストノード.strBoxText[i] = boxdef.strBoxText[i];
					}
				}
				switch (CStrジャンルtoNum.ForAC15(c曲リストノード.songGenre)) {
					case 0:
						c曲リストノード.ForeColor = OpenTaiko.Skin.SongSelect_ForeColor_JPOP;
						c曲リストノード.BackColor = OpenTaiko.Skin.SongSelect_BackColor_JPOP;
						break;
					case 1:
						c曲リストノード.ForeColor = OpenTaiko.Skin.SongSelect_ForeColor_Anime;
						c曲リストノード.BackColor = OpenTaiko.Skin.SongSelect_BackColor_Anime;
						break;
					case 2:
						c曲リストノード.ForeColor = OpenTaiko.Skin.SongSelect_ForeColor_VOCALOID;
						c曲リストノード.BackColor = OpenTaiko.Skin.SongSelect_BackColor_VOCALOID;
						break;
					case 3:
						c曲リストノード.ForeColor = OpenTaiko.Skin.SongSelect_ForeColor_Children;
						c曲リストノード.BackColor = OpenTaiko.Skin.SongSelect_BackColor_Children;
						break;
					case 4:
						c曲リストノード.ForeColor = OpenTaiko.Skin.SongSelect_ForeColor_Variety;
						c曲リストノード.BackColor = OpenTaiko.Skin.SongSelect_BackColor_Variety;
						break;
					case 5:
						c曲リストノード.ForeColor = OpenTaiko.Skin.SongSelect_ForeColor_Classic;
						c曲リストノード.BackColor = OpenTaiko.Skin.SongSelect_BackColor_Classic;
						break;
					case 6:
						c曲リストノード.ForeColor = OpenTaiko.Skin.SongSelect_ForeColor_GameMusic;
						c曲リストノード.BackColor = OpenTaiko.Skin.SongSelect_BackColor_GameMusic;
						break;
					case 7:
						c曲リストノード.ForeColor = OpenTaiko.Skin.SongSelect_ForeColor_Namco;
						c曲リストノード.BackColor = OpenTaiko.Skin.SongSelect_BackColor_Namco;
						break;
					default:
						break;
				}



				c曲リストノード.difficultiesCount = 1;
				c曲リストノード.score[0] = new CScore();
				c曲リストノード.score[0].ファイル情報.フォルダの絶対パス = infoDir.FullName + Path.DirectorySeparatorChar;
				c曲リストノード.score[0].譜面情報.タイトル = boxdef.Title.GetString("");
				c曲リストノード.score[0].譜面情報.ジャンル = boxdef.Genre;
				if (!String.IsNullOrEmpty(boxdef.DefaultPreimage))
					c曲リストノード.score[0].譜面情報.Preimage = boxdef.DefaultPreimage;
				c曲リストノード.rParentNode = node親;


				c曲リストノード.strBreadcrumbs = (c曲リストノード.rParentNode == null) ?
					c曲リストノード.ldTitle.GetString("") : c曲リストノード.rParentNode.strBreadcrumbs + " > " + c曲リストノード.ldTitle.GetString("");


				c曲リストノード.childrenList = new List<CSongListNode>();
				// OPTK Shortcut File
				foreach (string shortcutpath in Directory.GetFiles(infoDir.FullName, "*.optksc", SearchOption.TopDirectoryOnly)) {
					c曲リストノード.shortcutIds.AddRange(File.ReadAllLines(shortcutpath));
				}

				listノードリスト.Add(c曲リストノード);
				if (OpenTaiko.ConfigIni.bOutputSongSearchLog) {
					Trace.TraceInformation("box.def検出 : {0}", infoDir.FullName + @$"{Path.DirectorySeparatorChar}box.def");
					Trace.Indent();
					try {
						StringBuilder sb = new StringBuilder(0x400);
						sb.Append(string.Format("nID#{0:D3}", c曲リストノード.nID));
						if (c曲リストノード.rParentNode != null) {
							sb.Append(string.Format("(in#{0:D3}):", c曲リストノード.rParentNode.nID));
						} else {
							sb.Append("(onRoot):");
						}
						sb.Append("BOX, Title=" + c曲リストノード.ldTitle.GetString(""));
						if ((c曲リストノード.songGenre != null) && (c曲リストノード.songGenre.Length > 0)) {
							sb.Append(", Genre=" + c曲リストノード.songGenre);
						}
						if (c曲リストノード.IsChangedForeColor) {
							sb.Append(", ForeColor=" + c曲リストノード.ForeColor.ToString());
						}
						if (c曲リストノード.IsChangedBackColor) {
							sb.Append(", BackColor=" + c曲リストノード.BackColor.ToString());
						}
						if (c曲リストノード.isChangedBoxColor) {
							sb.Append(", BoxColor=" + c曲リストノード.BoxColor.ToString());
						}
						if (c曲リストノード.isChangedBgColor) {
							sb.Append(", BgColor=" + c曲リストノード.BgColor.ToString());
						}
						if (c曲リストノード.isChangedBoxType) {
							sb.Append(", BoxType=" + c曲リストノード.BoxType.ToString());
						}
						if (c曲リストノード.isChangedBgType) {
							sb.Append(", BgType=" + c曲リストノード.BgType.ToString());
						}
						if (c曲リストノード.isChangedBoxChara) {
							sb.Append(", BoxChara=" + c曲リストノード.BoxChara.ToString());
						}
						Trace.TraceInformation(sb.ToString());
					} finally {
						Trace.Unindent();
					}
				}
				if (b子BOXへ再帰する) {
					this.t曲を検索してリストを作成する(infoDir.FullName + Path.DirectorySeparatorChar, b子BOXへ再帰する, c曲リストノード.childrenList, c曲リストノード);
				}
			}
			//-----------------------------
			#endregion

			#region [ c.通常フォルダの場合 ]
			//-----------------------------
			else {
				this.t曲を検索してリストを作成する(infoDir.FullName + Path.DirectorySeparatorChar, b子BOXへ再帰する, listノードリスト, node親);
			}
			//-----------------------------
			#endregion
		}
	}
	//-----------------
	#endregion

	private void LoadChartInfo(CSongListNode c曲リストノード, CTja cdtx, int i) {
		if ((c曲リストノード.score[i] != null) && !c曲リストノード.score[i].bSongDBにキャッシュがあった) {
			#region [ DTX ファイルのヘッダだけ読み込み、Cスコア.譜面情報 を設定する ]
			//-----------------
			string path = c曲リストノード.score[i].ファイル情報.ファイルの絶対パス;
			if (File.Exists(path)) {
				try {
					c曲リストノード.score[i].譜面情報.タイトル = cdtx.TITLE.GetString("");


					c曲リストノード.score[i].譜面情報.アーティスト名 = cdtx.ARTIST;
					c曲リストノード.score[i].譜面情報.コメント = cdtx.COMMENT;
					c曲リストノード.score[i].譜面情報.ジャンル = cdtx.GENRE;
					if (!String.IsNullOrEmpty(cdtx.PREIMAGE))
						c曲リストノード.score[i].譜面情報.Preimage = cdtx.PREIMAGE;
					c曲リストノード.score[i].譜面情報.Presound = cdtx.PREVIEW;
					c曲リストノード.score[i].譜面情報.Backgound = cdtx.BACKGROUND;
					c曲リストノード.score[i].譜面情報.レベル.Drums = cdtx.LEVEL.Drums;
					c曲リストノード.score[i].譜面情報.レベルを非表示にする = cdtx.HIDDENLEVEL;
					c曲リストノード.score[i].譜面情報.Bpm = cdtx.BPM;
					c曲リストノード.score[i].譜面情報.BaseBpm = cdtx.BASEBPM;
					c曲リストノード.score[i].譜面情報.MinBpm = cdtx.MinBPM;
					c曲リストノード.score[i].譜面情報.MaxBpm = cdtx.MaxBPM;
					c曲リストノード.score[i].譜面情報.Duration = 0;    //  (cdtx.listChip == null)? 0 : cdtx.listChip[ cdtx.listChip.Count - 1 ].n発声時刻ms;
					c曲リストノード.score[i].譜面情報.strBGMファイル名 = cdtx.strBGM_PATH;
					c曲リストノード.score[i].譜面情報.SongVol = cdtx.SongVol;
					c曲リストノード.score[i].譜面情報.SongLoudnessMetadata = cdtx.SongLoudnessMetadata;
					c曲リストノード.score[i].譜面情報.nデモBGMオフセット = cdtx.nデモBGMオフセット;
					c曲リストノード.score[i].譜面情報.strサブタイトル = cdtx.SUBTITLE.GetString("");
					for (int k = 0; k < (int)Difficulty.Total; k++) {
						c曲リストノード.score[i].譜面情報.b譜面分岐[k] = cdtx.bHIDDENBRANCH ? false : cdtx.bHasBranch[k];
						c曲リストノード.score[i].譜面情報.nレベル[k] = cdtx.LEVELtaiko[k];
						c曲リストノード.score[i].譜面情報.nLevelIcon[k] = cdtx.LEVELtaikoIcon[k];
					}

					// Tower Lives
					c曲リストノード.score[i].譜面情報.nLife = cdtx.LIFE;

					c曲リストノード.score[i].譜面情報.nTowerType = cdtx.TOWERTYPE;

					c曲リストノード.score[i].譜面情報.nDanTick = cdtx.DANTICK;
					c曲リストノード.score[i].譜面情報.cDanTickColor = cdtx.DANTICKCOLOR;

					c曲リストノード.score[i].譜面情報.nTotalFloor = 0;
					if ((Difficulty)i is Difficulty.Tower) {
						for (int k = 0; k < cdtx.listChip.Count; k++) {
							CChip pChip = cdtx.listChip[k];

							if (pChip.n整数値_内部番号 > c曲リストノード.score[i].譜面情報.nTotalFloor && pChip.nChannelNo == 0x50)
								c曲リストノード.score[i].譜面情報.nTotalFloor = pChip.n整数値_内部番号;
						}
						c曲リストノード.score[i].譜面情報.nTotalFloor++;
					}



					this.nファイルから反映できたスコア数++;
					cdtx.DeActivate();
					#region [ 曲検索ログ出力 ]
					//-----------------
					if (OpenTaiko.ConfigIni.bOutputSongSearchLog) {
						StringBuilder sb = new StringBuilder(0x400);
						sb.Append(string.Format("曲データファイルから譜面情報を転記しました。({0})", path));
						sb.Append("(title=" + c曲リストノード.score[i].譜面情報.タイトル);
						sb.Append(", artist=" + c曲リストノード.score[i].譜面情報.アーティスト名);
						sb.Append(", comment=" + c曲リストノード.score[i].譜面情報.コメント);
						sb.Append(", genre=" + c曲リストノード.score[i].譜面情報.ジャンル);
						sb.Append(", preimage=" + c曲リストノード.score[i].譜面情報.Preimage);
						sb.Append(", premovie=" + c曲リストノード.score[i].譜面情報.Premovie);
						sb.Append(", presound=" + c曲リストノード.score[i].譜面情報.Presound);
						sb.Append(", background=" + c曲リストノード.score[i].譜面情報.Backgound);
						sb.Append(", lvDr=" + c曲リストノード.score[i].譜面情報.レベル.Drums);
						sb.Append(", lvHide=" + c曲リストノード.score[i].譜面情報.レベルを非表示にする);
						sb.Append(", bpm=" + c曲リストノード.score[i].譜面情報.Bpm);
						sb.Append(", basebpm=" + c曲リストノード.score[i].譜面情報.BaseBpm);
						sb.Append(", minbpm=" + c曲リストノード.score[i].譜面情報.MinBpm);
						sb.Append(", maxbpm=" + c曲リストノード.score[i].譜面情報.MaxBpm);
						//	sb.Append( ", duration=" + c曲リストノード.arスコア[ i ].譜面情報.Duration );
						Trace.TraceInformation(sb.ToString());
					}
					//-----------------
					#endregion
				} catch (Exception exception) {
					Trace.TraceError(exception.ToString());
					c曲リストノード.score[i] = null;
					c曲リストノード.difficultiesCount--;
					this.n検索されたスコア数--;
					Trace.TraceError("曲データファイルの読み込みに失敗しました。({0})", path);
				}
			}
			//-----------------
			#endregion
		}
	}

	#region [ 曲リストへ後処理を適用する ]
	//-----------------
	public void tSongListPostprocessing() {
		listStrBoxDefSkinSubfolderFullName = new List<string>();
		if (OpenTaiko.Skin.strBoxDefSkinSubfolders != null) {
			foreach (string b in OpenTaiko.Skin.strBoxDefSkinSubfolders) {
				listStrBoxDefSkinSubfolderFullName.Add(b);
			}
		}

		this.tSongListPostprocessing(this.list曲ルート);

		for (int p = 0; p < list曲ルート.Count; p++) {
			var c曲リストノード = list曲ルート[p];
			if (c曲リストノード.nodeType == CSongListNode.ENodeType.BOX) {
				if (c曲リストノード.songGenre == "段位道場") {
					if (OpenTaiko.ConfigIni.bDanTowerHide) {
						list曲ルート.Remove(c曲リストノード);
						p--;
					}

					// Add to dojo
					list曲ルート_Dan = c曲リストノード.childrenList;
				} else if (c曲リストノード.songGenre == "太鼓タワー") {
					if (OpenTaiko.ConfigIni.bDanTowerHide) {
						list曲ルート.Remove(c曲リストノード);
						p--;
					}

					list曲ルート_Tower = c曲リストノード.childrenList;
				} else {
					for (int i = 0; i < c曲リストノード.childrenList.Count; i++) {
						if (c曲リストノード.childrenList[i].score[6] != null) {
							list曲ルート_Dan.Add(c曲リストノード.childrenList[i]);

							if (OpenTaiko.ConfigIni.bDanTowerHide)
								c曲リストノード.childrenList.Remove(c曲リストノード.childrenList[i]);

							continue;
						}
						if (c曲リストノード.childrenList[i].score[5] != null) {
							list曲ルート_Tower.Add(c曲リストノード.childrenList[i]);

							if (OpenTaiko.ConfigIni.bDanTowerHide)
								c曲リストノード.childrenList.Remove(c曲リストノード.childrenList[i]);
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
					if ((songNode.score[j] != null) && !string.IsNullOrEmpty(songNode.score[j].譜面情報.タイトル)) {
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
}
