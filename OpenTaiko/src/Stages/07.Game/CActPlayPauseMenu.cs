using System.Diagnostics;
using FDK;

namespace OpenTaiko;

internal class CActPlayPauseMenu : CActSelectPopupMenu {
	// コンストラクタ

	public CActPlayPauseMenu() {
		CActPlayPauseMenuMain();
	}

	private void CActPlayPauseMenuMain() {
		this.bEscEnabled = false;
		lci = new List<List<List<CItemBase>>>();                                    // この画面に来る度に、メニューを作り直す。
		for (int nConfSet = 0; nConfSet < (OpenTaiko.SongMount.nChoosenSongDifficulty[0] != (int)Difficulty.Dan ? 3 : 2); nConfSet++) {
			lci.Add(new List<List<CItemBase>>());                                   // ConfSet用の3つ分の枠。
			for (int nInst = 0; nInst < 3; nInst++) {
				lci[nConfSet].Add(null);                                        // Drum/Guitar/Bassで3つ分、枠を作っておく
				lci[nConfSet][nInst] = MakeListCItemBase(nConfSet, nInst);
			}
		}
		base.Initialize(lci[nCurrentConfigSet][0], true, CLangManager.LangInstance.GetString("PAUSE_TITLE"), 0);    // ConfSet=0, nInst=Drums
	}

	private List<CItemBase> MakeListCItemBase(int nConfigSet, int nInst) {
		List<CItemBase> l = new List<CItemBase>();

		#region [ 共通 SET切り替え/More/Return ]
		l.Add(new CSwitchItemList(CLangManager.LangInstance.GetString("PAUSE_RESUME"), CItemBase.EPanelType.Normal, 0, "", "", new string[] { "" }));
		if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] != (int)Difficulty.Dan) l.Add(new CSwitchItemList(CLangManager.LangInstance.GetString("PAUSE_RESTART"), CItemBase.EPanelType.Normal, 0, "", "", new string[] { "" }));
		l.Add(new CSwitchItemList(CLangManager.LangInstance.GetString("PAUSE_EXIT"), CItemBase.EPanelType.Normal, 0, "", "", new string[] { "", "" }));
		#endregion

		return l;
	}

	// メソッド
	public override void tActivatePopupMenu(EInstrumentPad einst) {
		this.CActPlayPauseMenuMain();
		CActSelectPopupMenu.bSelected = false;
		this.bRetrySelected = false;
		base.tActivatePopupMenu(einst);
	}
	//public void tDeativatePopupMenu()
	//{
	//	base.tDeativatePopupMenu();
	//}

	public override void UpdateSub() {
		if (this.bRetrySelected) {
			if (!sw.IsRunning)
				this.sw = Stopwatch.StartNew();
			if (sw.ElapsedMilliseconds > 1500) {
				OpenTaiko.stageGameScreen.tPlayRetry();

				this.tDeativatePopupMenu();
				this.sw.Reset();
			}
		}
	}

	public override void tEnterPressedMain(int nSortOrder) {
		switch (nCurrentSelectedLine) {
			case (int)EOrder.Continue:
				OpenTaiko.stageGameScreen.Resume();
				CActSelectPopupMenu.bSelected = true;
				this.tDeativatePopupMenu();
				break;

			case (int)EOrder.Redoing:
				if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
					goto case (int)EOrder.Return;
				this.bRetrySelected = true;
				CActSelectPopupMenu.bSelected = true;
				break;

			case (int)EOrder.Return:
				OpenTaiko.stageGameScreen.tPlayAbort();
				CActSelectPopupMenu.bSelected = true;
				this.tDeativatePopupMenu();
				break;
			default:
				break;
		}
	}

	public override void tCancel() {
	}

	// CActivity 実装

	public override void Activate() {
		base.Activate();
		this.bGotoDetailConfig = false;
		this.sw = new Stopwatch();
	}
	public override void DeActivate() {
		base.DeActivate();
	}
	public override void CreateManagedResource() {
		string pathPanelBody = CSkin.Path(@$"Graphics{Path.DirectorySeparatorChar}ScreenSelect popup auto settings.png");
		if (File.Exists(pathPanelBody)) {
			this.txPanelBody = OpenTaiko.tTextureCreate(pathPanelBody, true);
		}

		base.CreateManagedResource();
	}
	public override void ReleaseManagedResource() {
		OpenTaiko.tTextureRelease(ref this.txPanelBody);
		OpenTaiko.tTextureRelease(ref this.txStringPanel);
		base.ReleaseManagedResource();
	}

	#region [ private ]
	//-----------------
	private int nCurrentTarget = 0;
	private int nCurrentConfigSet = 0;
	private List<List<List<CItemBase>>> lci;
	private enum EOrder : int {
		Continue,
		Redoing,
		Return, END,
		Default = 99
	};

	private bool bSelected;
	private CTexture txPanelBody;
	private CTexture txStringPanel;
	private Stopwatch sw;
	private bool bRetrySelected;
	//-----------------
	#endregion
}
