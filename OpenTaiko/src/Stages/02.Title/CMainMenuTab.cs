using System.Drawing;
using FDK;

namespace OpenTaiko;

class CMainMenuTab {
	public static int __MenuCount = 14; // Number of existing menus
	public static CMainMenuTab[] __Menus;
	public static bool __BoxesProcessed = false;

	public TitleTextureKey ttkTitle;
	public TitleTextureKey ttkBoxText;
	public bool _1pRestricted;
	public bool implemented;
	public CTexture barTex;
	public CTexture barChara;
	public CStageTitle.EReturnValue rp;

	public CMainMenuTab(int boxId, Color col, CCachedFontRenderer tpf, CCachedFontRenderer boxpf, CStageTitle.EReturnValue returnPoint, bool _1Ponly, bool impl, CTexture[] modeSelect_Bar, CTexture[] modeSelect_Bar_Chara) {
		string title = GetBoxText(boxId);

		ttkTitle = new TitleTextureKey(title, tpf, Color.White, col, 1280, Color.Black);

		string boxText = GetBoxText(boxId, false);

		ttkBoxText = new TitleTextureKey(boxText, boxpf, Color.White, Color.Black, 1000);

		rp = returnPoint;

		_1pRestricted = _1Ponly;
		implemented = impl;
		barTex = (modeSelect_Bar.Length > boxId) ? modeSelect_Bar[boxId] : null;
		barChara = (modeSelect_Bar_Chara.Length > boxId) ? modeSelect_Bar_Chara[boxId] : null;
	}

	private static string GetBoxText(int boxid, bool isTitle = true) {
		string append = isTitle ? "" : "_DESC";
		switch (boxid) {
			case 0:
			default:
				return CLangManager.LangInstance.GetString($"TITLE_MODE_TAIKO{append}");
			case 1:
				return CLangManager.LangInstance.GetString($"TITLE_MODE_DAN{append}");
			case 2:
				return CLangManager.LangInstance.GetString($"TITLE_MODE_TOWER{append}");
			case 3:
				return CLangManager.LangInstance.GetString($"TITLE_MODE_SHOP{append}");
			case 4:
				return CLangManager.LangInstance.GetString($"TITLE_MODE_STORY{append}");
			case 5:
				return CLangManager.LangInstance.GetString($"TITLE_MODE_HEYA{append}");
			case 6:
				return CLangManager.LangInstance.GetString($"TITLE_MODE_SETTINGS{append}");
			case 7:
				return CLangManager.LangInstance.GetString($"TITLE_MODE_EXIT{append}");
			case 8:
				return CLangManager.LangInstance.GetString($"TITLE_MODE_ONLINE{append}");
			case 9:
				return CLangManager.LangInstance.GetString($"TITLE_MODE_DOCUMENT{append}");
			case 10:
				return CLangManager.LangInstance.GetString($"TITLE_MODE_AI{append}");
			case 11:
				return CLangManager.LangInstance.GetString($"TITLE_MODE_STATS{append}");
			case 12:
				return CLangManager.LangInstance.GetString($"TITLE_MODE_EDITOR{append}");
			case 13:
				return CLangManager.LangInstance.GetString($"TITLE_MODE_TOOLS{append}");
		}
	}

	public static void tInitMenus(CCachedFontRenderer tpf, CCachedFontRenderer boxpf, CTexture[] modeSelect_Bar, CTexture[] modeSelect_Bar_Chara) {
		// Proceed the boxes only once

		if (__BoxesProcessed == false) {
			__Menus = new CMainMenuTab[__MenuCount];

			// Removed to avoid having to reload the game when changing language
			//__BoxesProcessed = true;

			#region [Menu Colors]

			Color[] __MenuColors =
			{
				Color.FromArgb(233, 53, 71),
				Color.FromArgb(71, 64, 135),
				Color.FromArgb(255, 180, 42),
				Color.FromArgb(16, 255, 255),
				Color.FromArgb(128, 0, 128),
				Color.FromArgb(24, 128, 24),
				Color.FromArgb(128, 128, 128),
				Color.FromArgb(72, 72, 72),
				Color.FromArgb(199, 8, 119), // Online lounge red/pink
				Color.FromArgb(181, 186, 28),  // Encyclopedia yellow
				Color.FromArgb(78, 166, 171), // AI battle mode blue
				Color.FromArgb(230, 230, 230), // Player stats white
				Color.FromArgb(40, 40, 40), // Chart editor black
				Color.FromArgb(120, 104, 56), // Toolbox brown
			};

			#endregion

			#region [Return points]

			CStageTitle.EReturnValue[] __rps =
			{
				CStageTitle.EReturnValue.GAMESTART,
				CStageTitle.EReturnValue.DANGAMESTART,
				CStageTitle.EReturnValue.TAIKOTOWERSSTART,
				CStageTitle.EReturnValue.SHOPSTART,
				CStageTitle.EReturnValue.BOUKENSTART,
				CStageTitle.EReturnValue.HEYA,
				CStageTitle.EReturnValue.CONFIG,
				CStageTitle.EReturnValue.EXIT,
				CStageTitle.EReturnValue.ONLINELOUNGE,
				CStageTitle.EReturnValue.ENCYCLOPEDIA,
				CStageTitle.EReturnValue.AIBATTLEMODE,
				CStageTitle.EReturnValue.PLAYERSTATS,
				CStageTitle.EReturnValue.CHARTEDITOR,
				CStageTitle.EReturnValue.TOOLBOX,
			};

			#endregion

			#region [Extra bools]

			bool[] _1PRestricts =
			{
				false,
				true,
				true,
				false,
				true,
				false,
				false,
				false,
				false,
				false,
				false,
				false,
				true,
				false,
			};

			// To edit while new features are implemented
			bool[] _implemented =
			{
				true,
				true,
				true,
				false,
				false,
				true,
				true,
				true,
				true,
				false,
				true,
				false,
				false,
				false,
			};

			#endregion

			for (int i = 0; i < __MenuCount; i++) {
				CStageTitle.EReturnValue _rp = (i >= __rps.Length) ? CStageTitle.EReturnValue.GAMESTART : __rps[i];
				Color _mc = (i >= __MenuColors.Length) ? Color.White : __MenuColors[i];
				bool _1pr = (i >= _1PRestricts.Length) ? false : _1PRestricts[i];
				bool _impl = (i >= _implemented.Length) ? false : _implemented[i];

				__Menus[i] = new CMainMenuTab(i, _mc, tpf, boxpf, _rp, _1pr, _impl, modeSelect_Bar, modeSelect_Bar_Chara);

			}
		}

	}

}
