using System.Drawing;

namespace OpenTaiko;

[Serializable]
internal class CSongListNode {
	// Properties

	public ENodeType nodeType = ENodeType.UNKNOWN;
	public enum ENodeType {
		SCORE,
		SCORE_MIDI,
		BOX,
		BACKBOX,
		RANDOM,
		UNKNOWN
	}
	public int nID { get; private set; }
	public CScore[] score = new CScore[(int)Difficulty.Total];

	public string[] difficultyLabel = new string[(int)Difficulty.Total];

	public Color ForeColor = Color.White;
	public Color BackColor = Color.Black;
	public Color BoxColor = Color.White;

	public Color BgColor = Color.White;
	public bool isChangedBgColor;
	public bool isChangedBgType;
	public bool isChangedBoxType;
	public string BoxType;
	public string BgType;
	public string BoxChara;
	public bool isChangedBoxChara;

	public bool IsChangedForeColor;
	public bool IsChangedBackColor;
	public bool isChangedBoxColor;
	public List<CSongListNode> randomList;
	public List<CSongListNode> childrenList;

	public int difficultiesCount; // 4~5 if AD

	public CSongListNode rParentNode;

	// Internal
	public int Openindex;
	public bool bIsOpenFolder;
	public string strBreadcrumbs = "";      // Removable?
	public string strSkinPath = "";         // Removable?

	// Metadata
	public string songGenre = "";
	public string songGenrePanel = ""; // Used only for the panel under the song title
	public CLocalizationData ldTitle = new CLocalizationData();
	public CLocalizationData ldSubtitle = new CLocalizationData();
	public string strMaker = "";
	public string[] strNotesDesigner = new string[(int)Difficulty.Total] { "", "", "", "", "", "", "" };
	public CTja.ESide nSide = CTja.ESide.eEx;
	public bool bExplicit = false;
	public bool bMovie = false;
	public int[] nLevel = new int[(int)Difficulty.Total] { 0, 0, 0, 0, 0, 0, 0 };
	public CTja.ELevelIcon[] nLevelIcon = new CTja.ELevelIcon[(int)Difficulty.Total] { CTja.ELevelIcon.eNone, CTja.ELevelIcon.eNone, CTja.ELevelIcon.eNone, CTja.ELevelIcon.eNone, CTja.ELevelIcon.eNone, CTja.ELevelIcon.eNone, CTja.ELevelIcon.eNone };

	// Branches
	public bool bBranch = false;

	// Dan
	public List<CTja.DanSongs> DanSongs;
	public Dan_C[] Dan_C;

	// Tower Lives
	public int nLife = 5;
	public int nTotalFloor = 140;
	public string nTowerType;

	// Unique id
	public CSongUniqueID uniqueId;
	public List<string> shortcutIds = [];
	public bool shortcutIsParsed;

	public int nDanTick = 0;
	public Color cDanTickColor = Color.White;

	public CLocalizationData[] strBoxText = new CLocalizationData[3] { new CLocalizationData(), new CLocalizationData(), new CLocalizationData() };

	public string strSelectBGPath;

	// In-game visuals

	public string strScenePreset = null;

	#region [ OpenTaiko-Exclusive TJA Extension Data ]

	public CTja.CutSceneDef? CutSceneIntro = null;
	public List<CTja.CutSceneDef> CutSceneOutros = [];

	#endregion

	public string tGetUniqueId() {
		return uniqueId?.data.id ?? "";
	}

	// Constructor

	public CSongListNode() {
		this.nID = id++;
	}

	public CSongListNode Clone() {
		return (CSongListNode)MemberwiseClone();
	}

	public override bool Equals(object other) {
		if (other.GetType() == typeof(CSongListNode)) {
			CSongListNode obj = (CSongListNode)other;
			return this.nID == obj.nID;
		}
		return this.GetHashCode() == other.GetHashCode();
	}

	public override int GetHashCode() {
		return base.GetHashCode();
	}


	// その他

	#region [ private ]
	//-----------------
	private static int id;
	//-----------------
	#endregion
}
