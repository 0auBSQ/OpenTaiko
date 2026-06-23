using System.Drawing;
using FDK;
using SkiaSharp;

namespace OpenTaiko;

internal class CActConfigList : CActivity {
	// Properties

	public bool bIsKeyAssignSelected        // #24525 2011.3.15 yyagi
	{
		get {
			EMenuType e = this.eMenuType;
			if (e == EMenuType.KeyAssignDrums || e == EMenuType.KeyAssignSystem || e == EMenuType.KeyAssignTraining) {
				return true;
			} else {
				return false;
			}
		}
	}
	public bool bIsFocusingParameter        // #32059 2013.9.17 yyagi
	{
		get {
			return bElementValueFocus;
		}
	}
	public bool bCurrentSelectedItemReturnToMenu {
		get {
			CItemBase currentItem = this.listItemList[this.nCurrentSelectedItem];
			if (currentItem == this.iSystemReturnToMenu || currentItem == this.iDrumsReturnToMenu || currentItem == this.iThemeReturnToMenu) {
				return true;
			} else {
				return false;
			}
		}
	}
	public CItemBase ibCurrentSelectedItem {
		get {
			return this.listItemList[this.nCurrentSelectedItem];
		}
	}
	public int nCurrentSelectedItem;

	private static int GraphicsDeviceFromString(string device) {
		switch (device) {
			case "OpenGL": return 0;
			case "DirectX11": return 1;
			case "Vulkan": return 2;
			case "Metal": return 3;
			default: return 0;
		}
	}
	private static string GraphicsDeviceFromInt(int device) {
		switch (device) {
			case 0: return "OpenGL";
			case 1: return "DirectX11";
			case 2: return "Vulkan";
			case 3: return "Metal";
			default: return "OpenGL";
		}
	}

	private static string[] AvailableGraphicsDevices {
		get {
			if (OperatingSystem.IsWindows()) return ["OpenGL", "DirectX11", "Vulkan"];
			if (OperatingSystem.IsMacOS()) return ["OpenGL", "Metal"];
			if (OperatingSystem.IsLinux()) return ["OpenGL", "Vulkan"];
			return ["OpenGL", "DirectX11", "Vulkan", "Metal"];
		}
	}

	private static int GraphicsDeviceIntFromConfigInt() {
		return Math.Max(0, Array.IndexOf(AvailableGraphicsDevices, GraphicsDeviceFromInt(OpenTaiko.ConfigIni.nGraphicsDeviceType)));
	}


	// General system options
	#region [ t項目リストの設定_System() ]

	public void tItemListSettings_System(bool refresh = true) {
		this.tConfigIniRecord();
		this.listItemList.Clear();

		// #27029 2012.1.5 from: 説明文は最大9行→13行に変更。

		this.iSystemReturnToMenu = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_MENU_RETURN"), CItemBase.EPanelType.Other,
			CLangManager.LangInstance.GetString("SETTINGS_MENU_RETURN_DESC"));
		this.listItemList.Add(this.iSystemReturnToMenu);

		this.iSystemReloadDTX = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_RELOADSONG"), CItemBase.EPanelType.Normal,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_RELOADSONG_DESC"));
		this.listItemList.Add(this.iSystemReloadDTX);

		this.iSystemHardReloadDTX = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_RELOADSONGCACHE"), CItemBase.EPanelType.Normal,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_RELOADSONGCACHE_DESC"));
		this.listItemList.Add(this.iSystemHardReloadDTX);

		this.isSystemImportingScore = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_IMPORTSCOREINI"), CItemBase.EPanelType.Normal,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_IMPORTSCOREINI_DESC"));
		this.listItemList.Add(this.isSystemImportingScore);

		this.iSystemLanguage = new CItemList(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_LANGUAGE"), CItemList.EPanelType.Normal, CLangManager.langToInt(OpenTaiko.ConfigIni.sLang),
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_LANGUAGE_DESC"),
			CLangManager.Languages);
		this.listItemList.Add(this.iSystemLanguage);

		this.iTaikoPlayerCount = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_PLAYERCOUNT"), 1, 5, OpenTaiko.ConfigIni.nPlayerCount,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_PLAYERCOUNT_DESC"));
		this.listItemList.Add(this.iTaikoPlayerCount);

		this.iDanTowerHide = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_HIDEDANTOWER"), OpenTaiko.ConfigIni.bDanTowerHide,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_HIDEDANTOWER_DESC"));
		this.listItemList.Add(this.iDanTowerHide);

		this.iCommonPlaySpeed = new CItemInteger(CLangManager.LangInstance.GetString("MOD_SONGSPEED"), CConfigIni.MinimumSongSpeed, CConfigIni.MaximumSongSpeed, OpenTaiko.ConfigIni.nSongSpeed,
			CLangManager.LangInstance.GetString("SETTINGS_MOD_SONGSPEED_DESC"));
		this.listItemList.Add(this.iCommonPlaySpeed);

		this.iSystemTimeStretch = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_TIMESTRETCH"), OpenTaiko.ConfigIni.bTimeStretch,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_TIMESTRETCH_DESC"));
		this.listItemList.Add(this.iSystemTimeStretch);

		this.iSystemGraphicsType = new CItemList(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_GRAPHICSAPI"), CItemList.EPanelType.Normal, GraphicsDeviceIntFromConfigInt(),
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_GRAPHICSAPI_DESC"),
			AvailableGraphicsDevices);
		this.listItemList.Add(this.iSystemGraphicsType);

		this.iSystemWindowMode = new CItemList(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_WINDOWMODE"), CItemList.EPanelType.Normal, OpenTaiko.ConfigIni.nWindowMode,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_WINDOWMODE_DESC"),
			new string[] {
				CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_WINDOWMODE_WINDOWED"),
				CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_WINDOWMODE_FULLSCREEN"),
				CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_WINDOWMODE_BORDERLESS")
			});
		this.listItemList.Add(this.iSystemWindowMode);

		this.iSystemRandomFromSubBox = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_RANDOMSUBFOLDER"), OpenTaiko.ConfigIni.bIncludeSubfoldersOnRandomSelect,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_RANDOMSUBFOLDER_DESC"));
		this.listItemList.Add(this.iSystemRandomFromSubBox);

		this.iSystemVSyncWait = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_VSYNC"), OpenTaiko.ConfigIni.bEnableVSync,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_VSYNC_DESC"));
		this.listItemList.Add(this.iSystemVSyncWait);

		this.iSystemAVI = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BGMOVIE"), OpenTaiko.ConfigIni.bEnableAVI,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BGMOVIE_DESC"));
		this.listItemList.Add(this.iSystemAVI);

		this.iSystemAVIDisplayMode = new CItemList(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BGMOVIEDISPLAY"), CItemList.EPanelType.Normal, (int)OpenTaiko.ConfigIni.eClipDispType,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BGMOVIEDISPLAY_DESC"),
			new string[] {
				CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BGMOVIEDISPLAY_NONE"),
				CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BGMOVIEDISPLAY_FULL"),
				CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BGMOVIEDISPLAY_MINI"),
				CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BGMOVIEDISPLAY_BOTH")
			});
		this.listItemList.Add(this.iSystemAVIDisplayMode);

		this.iSystemBGA = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BGA"), OpenTaiko.ConfigIni.bEnableBGA,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BGA_DESC"));
		this.listItemList.Add(this.iSystemBGA);

		this.iSystemPreviewSoundWait = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SONGPREVIEWBUFFER"), 0, 0x2710, OpenTaiko.ConfigIni.nMsWaitPreviewSoundFromSongSelected,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SONGPREVIEWBUFFER_DESC"));
		this.listItemList.Add(this.iSystemPreviewSoundWait);

		this.iSystemPreviewImageWait = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_IMAGEPREVIEWBUFFER"), 0, 0x2710, OpenTaiko.ConfigIni.nMsWaitPreviewImageFromSongSelected,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_IMAGEPREVIEWBUFFER_DESC"));
		this.listItemList.Add(this.iSystemPreviewImageWait);

		this.iSystemDebugInfo = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DEBUGMODE"), OpenTaiko.ConfigIni.bDisplayDebugInfo,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DEBUGMODE_DESC"));
		this.listItemList.Add(this.iSystemDebugInfo);

		this.iSystemBGAlpha = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_LANEOPACITY"), 0, 0xff, OpenTaiko.ConfigIni.nBackgroundTransparency,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_LANEOPACITY_DESC"));
		this.listItemList.Add(this.iSystemBGAlpha);

		this.iSystemBGMSound = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SONGPLAYBACK"), OpenTaiko.ConfigIni.bBGMPlayVoiceSound,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SONGPLAYBACK_DESC"));
		this.listItemList.Add(this.iSystemBGMSound);

		this.iSystemApplySongVol = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_USESONGVOL"), OpenTaiko.ConfigIni.ApplySongVol,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_USESONGVOL_DESC"));
		this.listItemList.Add(this.iSystemApplySongVol);

		this.iSystemMasterLevel = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_MASTERVOL"), CSound.MinimumGroupLevel, CSound.MaximumGroupLevel, OpenTaiko.ConfigIni.MasterLevel,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_MASTERVOL_DESC"));
		this.listItemList.Add(this.iSystemMasterLevel);

		this.iSystemSoundEffectLevel = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SEVOL"), CSound.MinimumGroupLevel, CSound.MaximumGroupLevel, OpenTaiko.ConfigIni.SoundEffectLevel,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SEVOL_DESC"));
		this.listItemList.Add(this.iSystemSoundEffectLevel);

		this.iSystemVoiceLevel = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_VOICEVOL"), CSound.MinimumGroupLevel, CSound.MaximumGroupLevel, OpenTaiko.ConfigIni.VoiceLevel,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_VOICEVOL_DESC"));
		this.listItemList.Add(this.iSystemVoiceLevel);

		this.iSystemSongPreviewLevel = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SONGPREVIEWVOL"), CSound.MinimumGroupLevel, CSound.MaximumGroupLevel, OpenTaiko.ConfigIni.SongPreviewLevel,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SONGPREVIEWVOL_DESC"));
		this.listItemList.Add(this.iSystemSongPreviewLevel);

		this.iSystemSongPlaybackLevel = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SONGVOL"), CSound.MinimumGroupLevel, CSound.MaximumGroupLevel, OpenTaiko.ConfigIni.SongPlaybackLevel,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SONGVOL_DESC"));
		this.listItemList.Add(this.iSystemSongPlaybackLevel);

		this.iSystemKeyboardSoundLevelIncrement = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_VOLINCREMENT"), 1, 20, OpenTaiko.ConfigIni.KeyboardSoundLevelIncrement,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_VOLINCREMENT_DESC"));
		this.listItemList.Add(this.iSystemKeyboardSoundLevelIncrement);

		this.MusicPreTimeMs = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SONGPLAYBACKBUFFER"), 0, 10000, OpenTaiko.ConfigIni.MusicPreTimeMs,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SONGPLAYBACKBUFFER_DESC"));
		this.listItemList.Add(this.MusicPreTimeMs);

		this.iSystemAutoResultCapture = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_AUTOSCREENSHOT"), OpenTaiko.ConfigIni.bIsAutoResultCapture,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_AUTOSCREENSHOT_DESC"));
		this.listItemList.Add(this.iSystemAutoResultCapture);

		SendDiscordPlayingInformation = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISCORDRPC"),
			OpenTaiko.ConfigIni.SendDiscordPlayingInformation,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISCORDRPC_DESC"));
		listItemList.Add(SendDiscordPlayingInformation);

		this.iSystemGameEventBroadcasting = new CItemToggle("Game Event Broadcasting", OpenTaiko.ConfigIni.bEnableGameEventBroadcasting,
			"Enable broadcasting game events via HTTP.");
		this.listItemList.Add(this.iSystemGameEventBroadcasting);

		this.iSystemGameEventBroadcastingPort = new CItemInteger("Broadcasting Port", 0, 65535, OpenTaiko.ConfigIni.nGameEventBroadcastingPort,
			"Port number for game event broadcasting.");
		this.listItemList.Add(this.iSystemGameEventBroadcastingPort);

		this.iLogOutputLog = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_LOG"), OpenTaiko.ConfigIni.bOutputLogs,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_LOG_DESC"));
		this.listItemList.Add(this.iLogOutputLog);

		// #24820 2013.1.3 yyagi

		// Hide this option for non-Windows users since all other sound device options are Windows-exclusive.
		if (OperatingSystem.IsWindows()) {
			this.iSystemSoundType = new CItemList(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_AUDIOPLAYBACK"), CItemList.EPanelType.Normal, OpenTaiko.ConfigIni.nSoundDeviceType,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_AUDIOPLAYBACK_DESC"),
			new string[] { "Bass", "ASIO", "WASAPI Exclusive", "WASAPI Shared" });
			this.listItemList.Add(this.iSystemSoundType);
		}

		// #24820 2013.1.15 yyagi
		this.iSystemBassBufferSizeMs = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BASSBUFFER"), 0, 99999, OpenTaiko.ConfigIni.nBassBufferSizeMs,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BASSBUFFER_DESC"));
		this.listItemList.Add(this.iSystemBassBufferSizeMs);

		if (OperatingSystem.IsWindows()) {
			// #24820 2013.1.15 yyagi
			this.iSystemWASAPIBufferSizeMs = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_WASAPIBUFFER"), 0, 99999, OpenTaiko.ConfigIni.nWASAPIBufferSizeMs,
				CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_WASAPIBUFFER_DESC"));
			this.listItemList.Add(this.iSystemWASAPIBufferSizeMs);

			// #24820 2013.1.17 yyagi
			this.iSystemASIODevice = new CItemList(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_ASIOPLAYBACK"), CItemList.EPanelType.Normal, OpenTaiko.ConfigIni.nASIODevice,
				CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_ASIOPLAYBACK_DESC"),
				CEnumerateAllAsioDevices.GetAllASIODevices());
			this.listItemList.Add(this.iSystemASIODevice);
		}

		// #33689 2014.6.17 yyagi
		this.iSystemSoundTimerType = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_OSTIMER"), OpenTaiko.ConfigIni.bUseOSTimer,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_OSTIMER_DESC"));
		this.listItemList.Add(this.iSystemSoundTimerType);

		ShowChara = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYCHARA"), OpenTaiko.ConfigIni.ShowChara,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYCHARA_DESC"));
		this.listItemList.Add(ShowChara);

		ShowDancer = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYDANCER"), OpenTaiko.ConfigIni.ShowDancer,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYDANCER_DESC"));
		this.listItemList.Add(ShowDancer);

		ShowMob = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYMOB"), OpenTaiko.ConfigIni.ShowMob,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYMOB_DESC"));
		this.listItemList.Add(ShowMob);

		ShowRunner = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYRUNNER"), OpenTaiko.ConfigIni.ShowRunner,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYRUNNER_DESC"));
		this.listItemList.Add(ShowRunner);

		ShowFooter = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYFOOTER"), OpenTaiko.ConfigIni.ShowFooter,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYFOOTER_DESC"));
		this.listItemList.Add(ShowFooter);

		FastRender = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_FASTRENDER"), OpenTaiko.ConfigIni.FastRender,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_FASTRENDER_DESC"));
		this.listItemList.Add(FastRender);

		ShowPuchiChara = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYPUCHI"), OpenTaiko.ConfigIni.ShowPuchiChara,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYPUCHI_DESC"));
		this.listItemList.Add(ShowPuchiChara);

		SimpleMode = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SIMPLEMODE"), OpenTaiko.ConfigIni.SimpleMode,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SIMPLEMODE_DESC"));
		this.listItemList.Add(SimpleMode);

		ASyncTextureLoad = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_TEXTUREASYNC"), OpenTaiko.ConfigIni.ASyncTextureLoad,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_TEXTUREASYNC_DESC"));
		this.listItemList.Add(ASyncTextureLoad);

		this.iSystemSkinSubfolder = new CItemList(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SKIN"), CItemBase.EPanelType.Normal, nSkinIndex,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SKIN_DESC"),
			skinNames);
		this.listItemList.Add(this.iSystemSkinSubfolder);

		// Render-resolution selector (under the skin selector). Values come from the current skin's "Resolutions";
		// each label is "<multiplier> (<width>x<height>)", e.g. "2/3 (1280x720)". Changing it reloads the skin (like a
		// skin change) so the whole game re-renders at skinResolution × multiplier, upscaled to the window.
		{
			var res = OpenTaiko.Skin.Resolutions;
			var resLabels = new string[res.Count];
			int resIndex = 0;
			for (int i = 0; i < res.Count; i++) {
				int rw = (int)Math.Round(OpenTaiko.Skin.Resolution[0] * res[i].val);
				int rh = (int)Math.Round(OpenTaiko.Skin.Resolution[1] * res[i].val);
				resLabels[i] = res[i].label + " (" + rw + "x" + rh + ")";
				if (Math.Abs(res[i].val - OpenTaiko.ConfigIni.fRenderScale) < 1e-4) resIndex = i;
			}
			this.iSystemResolution = new CItemList(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_RESOLUTION"), CItemBase.EPanelType.Normal, resIndex,
				CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_RESOLUTION_DESC"),
				resLabels);
			this.listItemList.Add(this.iSystemResolution);
		}

		this.iSystemGoToKeyAssign = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM"), CItemBase.EPanelType.Normal,
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_DESC"));
		this.listItemList.Add(this.iSystemGoToKeyAssign);

#if DEBUG
		this.debugImGui = new CItemToggle("[DEBUG ONLY] Show ImGui Debug Window", OpenTaiko.ConfigIni.DEBUG_bShowImgui);
		this.listItemList.Add(this.debugImGui);
#endif

		OnListMenuInitialize();
		if (refresh) {
			this.nCurrentSelectedItem = 0;
			this.eMenuType = EMenuType.System;
		}

	}
	#endregion


	// Gameplay options
	#region [ t項目リストの設定_Drums() ]

	public void tItemListSettings_Drums() {
		this.tConfigIniRecord();
		this.listItemList.Clear();

		// #27029 2012.1.5 from: 説明文は最大9行→13行に変更。

		this.iDrumsReturnToMenu = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_MENU_RETURN"), CItemBase.EPanelType.Other,
			CLangManager.LangInstance.GetString("SETTINGS_MENU_RETURN_DESC"));
		this.listItemList.Add(this.iDrumsReturnToMenu);

		this.iDrumsGoToCalibration = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_GAME_CALIBRATION"), CItemBase.EPanelType.Other,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_CALIBRATION_DESC"));
		this.listItemList.Add(this.iDrumsGoToCalibration);

		this.iRollsPerSec = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_GAME_AUTOROLL"), 0, 1000, OpenTaiko.ConfigIni.nRollsPerSec,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_AUTOROLL_DESC"));
		this.listItemList.Add(this.iRollsPerSec);

		this.iAILevel = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_GAME_AILEVEL"), 1, 10, OpenTaiko.ConfigIni.nDefaultAILevel,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_AILEVEL_DESC"));
		this.listItemList.Add(this.iAILevel);

		this.iSystemRisky = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_GAME_BADCOUNT"), 0, 10, OpenTaiko.ConfigIni.nRisky,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_BADCOUNT_DESC"));
		this.listItemList.Add(this.iSystemRisky);

		this.iTaikoNoInfo = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_GAME_NOINFO"), OpenTaiko.ConfigIni.bNoInfo,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_NOINFO_DESC"));
		this.listItemList.Add(this.iTaikoNoInfo);

		this.iDrumsTight = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_GAME_NOTELOCK"), OpenTaiko.ConfigIni.bTight,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_NOTELOCK_DESC"));
		this.listItemList.Add(this.iDrumsTight);

		this.iSystemMinComboDrums = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_GAME_COMBODISPLAY"), 1, 0x1869f, OpenTaiko.ConfigIni.nMinDisplayedCombo,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_COMBODISPLAY_DESC"));
		this.listItemList.Add(this.iSystemMinComboDrums);

		this.iGlobalOffsetMs = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_GAME_GLOBALOFFSET"), -9999, 9999, OpenTaiko.ConfigIni.nGlobalOffsetMs,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_GLOBALOFFSET_DESC"));
		this.listItemList.Add(this.iGlobalOffsetMs);

		this.iTaikoDefaultCourse = new CItemList(CLangManager.LangInstance.GetString("SETTINGS_GAME_DEFAULTDIFF"), CItemBase.EPanelType.Normal, OpenTaiko.ConfigIni.nDefaultCourse,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_DEFAULTDIFF_DESC"),
			new string[] {
				CLangManager.LangInstance.GetString("DIFF_EASY"),
				CLangManager.LangInstance.GetString("DIFF_NORMAL"),
				CLangManager.LangInstance.GetString("DIFF_HARD"),
				CLangManager.LangInstance.GetString("DIFF_EX"),
				CLangManager.LangInstance.GetString("DIFF_EXTRA"),
				CLangManager.LangInstance.GetString("DIFF_EXEXTRA") });
		this.listItemList.Add(this.iTaikoDefaultCourse);

		this.iTaikoScoreMode = new CItemList(CLangManager.LangInstance.GetString("SETTINGS_GAME_SCOREMODE"), CItemBase.EPanelType.Normal, OpenTaiko.ConfigIni.nScoreMode,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_SCOREMODE_DESC"),
			new string[] { "TYPE-A", "TYPE-B", "TYPE-C" });
		this.listItemList.Add(this.iTaikoScoreMode);

		this.ShinuchiMode = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_GAME_SHINUCHI"), OpenTaiko.ConfigIni.ShinuchiMode, CItemBase.EPanelType.Normal,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_SHINUCHI_DESC"));
		this.listItemList.Add(this.ShinuchiMode);

		this.iTaikoBranchGuide = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_GAME_BRANCHGUIDE"), OpenTaiko.ConfigIni.bBranchGuide,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_BRANCHGUIDE_DESC"));
		this.listItemList.Add(this.iTaikoBranchGuide);

		this.iTaikoBranchAnime = new CItemList(CLangManager.LangInstance.GetString("SETTINGS_GAME_BRANCHANIME"), CItemBase.EPanelType.Normal, OpenTaiko.ConfigIni.nBranchAnime,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_BRANCHANIME_DESC"),
			new string[] { "TYPE-A", "TYPE-B" });
		this.listItemList.Add(this.iTaikoBranchAnime);

		this.iTaikoGameMode = new CItemList(CLangManager.LangInstance.GetString("SETTINGS_GAME_SURVIVAL"), CItemBase.EPanelType.Normal, (int)OpenTaiko.ConfigIni.eGameMode,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_SURVIVAL_DESC"),
			new string[] { "OFF", "TYPE-A", "TYPE-B" });
		this.listItemList.Add(this.iTaikoGameMode);

		this.iTaikoBigNotesJudge = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_GAME_BIGNOTEJUDGE"), OpenTaiko.ConfigIni.bJudgeBigNotes,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_BIGNOTEJUDGE_DESC"));
		this.listItemList.Add(this.iTaikoBigNotesJudge);

		this.iTaikoForceNormalGauge = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_GAME_NORMALGAUGE"), OpenTaiko.ConfigIni.bForceNormalGauge,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_NORMALGAUGE_DESC"));
		this.listItemList.Add(this.iTaikoForceNormalGauge);

		this.iTaikoJudgeCountDisp = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_GAME_SCOREDISPLAY"), OpenTaiko.ConfigIni.bJudgeCountDisplay,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_SCOREDISPLAY_DESC"));
		this.listItemList.Add(this.iTaikoJudgeCountDisp);

		this.iShowExExtraAnime = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_GAME_EXEXTRAANIME"), OpenTaiko.ConfigIni.ShowExExtraAnime,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_EXEXTRAANIME_DESC"));
		this.listItemList.Add(this.iShowExExtraAnime);

		this.TokkunSkipCount = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_TRAINING_SKIPCOUNT"), 1, 99, OpenTaiko.ConfigIni.TokkunSkipMeasures,
			CLangManager.LangInstance.GetString("SETTINGS_TRAINING_SKIPCOUNT_DESC"));
		this.listItemList.Add(TokkunSkipCount);

		this.TokkunMashInterval = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_TRAINING_JUMPINTERVAL"), 1, 9999, OpenTaiko.ConfigIni.TokkunMashInterval,
			CLangManager.LangInstance.GetString("SETTINGS_TRAINING_JUMPINTERVAL_DESC"));
		this.listItemList.Add(TokkunMashInterval);

		this.iTaikoIgnoreSongUnlockables = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_GAME_IGNORESONGUNLOCKABLES"), OpenTaiko.ConfigIni.bIgnoreSongUnlockables,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_IGNORESONGUNLOCKABLES_DESC"));
		this.listItemList.Add(this.iTaikoIgnoreSongUnlockables);

		this.iControllerDeadzone = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_GAME_CONTROLLERDEADZONE"), 10, 90, OpenTaiko.ConfigIni.nControllerDeadzone,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_CONTROLLERDEADZONE_DESC"));
		this.listItemList.Add(this.iControllerDeadzone);

		this.iDrumsGoToKeyAssign = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME"), CItemBase.EPanelType.Normal,
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_DESC"));
		this.listItemList.Add(this.iDrumsGoToKeyAssign);

		this.iDrumsGoToTrainingKeyAssign = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING"), CItemBase.EPanelType.Normal,
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_DESC"));
		this.listItemList.Add(this.iDrumsGoToTrainingKeyAssign);

		OnListMenuInitialize();
		this.nCurrentSelectedItem = 0;
		this.eMenuType = EMenuType.Drums;
	}

	#endregion

	// Theme-specific settings
	#region [ t項目リストの設定_Theme() ]

	public void tItemListSettings_Theme() {
		this.tConfigIniRecord();
		this.listItemList.Clear();
		themeItems.Clear();
		themeStringInput = null;
		activeThemeStringId = null;

		this.iThemeReturnToMenu = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_MENU_RETURN"), CItemBase.EPanelType.Other,
			CLangManager.LangInstance.GetString("SETTINGS_MENU_RETURN_DESC"));
		this.listItemList.Add(this.iThemeReturnToMenu);

		var db = OpenTaiko.Databases?.DBThemeSettings;
		if (db != null) {
			foreach (var def in db.Definitions) {
				CItemBase item = CreateThemeItem(def, db);
				if (item != null) {
					themeItems[def.Id] = item;
					this.listItemList.Add(item);
				}
			}
		}

		OnListMenuInitialize();
		this.nCurrentSelectedItem = 0;
		this.eMenuType = EMenuType.Theme;
	}

	private CItemBase? CreateThemeItem(CThemeSettingDef def, DBThemeSettings db) {
		// For save-scoped settings, use the first loaded save's SaveId as representative.
		long repSaveId = OpenTaiko.SaveFileInstances[0]?.data?.SaveId ?? 0L;
		string stored = def.IsSaveScoped
			? db.GetSettingForSave(def.Id, repSaveId)
			: db.GetSetting(def.Id);

		string label = def.Label.GetString(def.Id);
		string desc = def.Description.GetString("");

		switch (def.Type.ToLowerInvariant()) {
			case "bool": {
					bool val = stored == "1" || string.Equals(stored, "true", StringComparison.OrdinalIgnoreCase);
					return new CItemToggle(label, val, desc);
				}
			case "int": {
					int val = int.TryParse(stored, out int v) ? v : def.DefaultInt;
					return new CItemInteger(label, (int)def.Min, (int)def.Max, val, desc);
				}
			case "double": {
					// Store/display as integer (implicit 2-decimal fixed-point).
					// E.g. Min=0.0, Max=5.0, step=0.01 → range 0–500 displayed as 0.00–5.00
					int scale = 100;
					int minI = (int)Math.Round(def.Min * scale);
					int maxI = (int)Math.Round(def.Max * scale);
					double dval = double.TryParse(stored,
						System.Globalization.NumberStyles.Any,
						System.Globalization.CultureInfo.InvariantCulture,
						out double dv) ? dv : def.DefaultDouble;
					return new CItemInteger(label, minI, maxI, (int)Math.Round(dval * scale), desc);
				}
			case "enum": {
					if (def.Options == null || def.Options.Length == 0) return null;
					int idx = Math.Max(0, Array.IndexOf(def.Options, stored));
					return new CItemList(label, CItemList.EPanelType.Normal, idx, desc, def.Options);
				}
			case "string": {
					return new CItemString(label, stored, desc);
				}
			default:
				return null;
		}
	}

	#endregion


	/// <summary>
	/// ESC押下時の右メニュー描画
	/// </summary>
	public void tEscPressed() {
		if (this.bElementValueFocus)       // #32059 2013.9.17 add yyagi
		{
			// Discard string text input: restore the value to what it was before editing.
			if (activeThemeStringId != null &&
				themeItems.TryGetValue(activeThemeStringId, out var si) && si is CItemString cis)
				cis.Value = _stringInputOriginalValue;
			themeStringInput = null;
			activeThemeStringId = null;
			this.bElementValueFocus = false;
		}

		switch (eMenuType) {
			case EMenuType.KeyAssignSystem:
				tItemListSettings_System();
				break;
			case EMenuType.KeyAssignDrums:
			case EMenuType.KeyAssignTraining:
				tItemListSettings_Drums();
				break;
		}

		// これ以外なら何もしない
	}
	public void tEnterPressed() {
		OpenTaiko.Skin.soundDecideSFX.tPlay();
		if (this.bElementValueFocus) {
			// The draw phase already committed the value via real-time update.
			// Just clean up in case ImGui did not fire (e.g. pad-only confirm with no ImGui capture).
			themeStringInput = null;
			activeThemeStringId = null;
			this.bElementValueFocus = false;
		} else if (this.listItemList[this.nCurrentSelectedItem] is CItemString strItem) {
			// If the draw phase committed this exact frame, consume the flag and do not reopen.
			if (_stringInputJustCommitted) {
				_stringInputJustCommitted = false;
			} else {
				// Open a CTextInput for string theme settings.
				activeThemeStringId = themeItems
					.FirstOrDefault(kv => kv.Value == strItem).Key;
				if (activeThemeStringId != null) {
					_stringInputOriginalValue = strItem.Value;
					themeStringInput = new CTextInput(strItem.Value, 256);
					this.bElementValueFocus = true;
				}
			}
		} else if (this.listItemList[this.nCurrentSelectedItem].eType == CItemBase.EType.Int) {
			this.bElementValueFocus = true;
		}
		#region [ 個々のキーアサイン ]
		//太鼓のキー設定。
		else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTaikoLRed) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.LRed);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTaikoRRed) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.RRed);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTaikoLBlue) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.LBlue);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTaikoRBlue) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.RBlue);
		}

		//太鼓のキー設定。2P
		else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTaikoLRed2P) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.LRed2P);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTaikoRRed2P) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.RRed2P);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTaikoLBlue2P) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.LBlue2P);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTaikoRBlue2P) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.RBlue2P);
		}

		//太鼓のキー設定。3P
		else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTaikoLRed3P) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.LRed3P);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTaikoRRed3P) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.RRed3P);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTaikoLBlue3P) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.LBlue3P);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTaikoRBlue3P) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.RBlue3P);
		}

		//太鼓のキー設定。4P
		else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTaikoLRed4P) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.LRed4P);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTaikoRRed4P) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.RRed4P);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTaikoLBlue4P) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.LBlue4P);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTaikoRBlue4P) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.RBlue4P);
		}

		//太鼓のキー設定。5P
		else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTaikoLRed5P) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.LRed5P);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTaikoRRed5P) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.RRed5P);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTaikoLBlue5P) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.LBlue5P);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTaikoRBlue5P) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.RBlue5P);
		}

		// Konga claps
		else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignKongaClap) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.Clap);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignKongaClap2P) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.Clap2P);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignKongaClap3P) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.Clap3P);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignKongaClap4P) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.Clap4P);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignKongaClap5P) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.Clap5P);
		}

		// Menu controls
		else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignDecide) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.Decide);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignCancel) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.Cancel);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignLeftChange) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.LeftChange);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignRightChange) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.RightChange);
		}

		// System controls
		else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignSystemCapture) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.System, EKeyConfigPad.Capture);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignSystemSongVolIncrease) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.System, EKeyConfigPad.SongVolumeIncrease);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignSystemSongVolDecrease) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.System, EKeyConfigPad.SongVolumeDecrease);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignSystemDisplayHit) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.System, EKeyConfigPad.DisplayHits);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignSystemDisplayDebug) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.System, EKeyConfigPad.DisplayDebug);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignSystemQuickConfig) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.System, EKeyConfigPad.QuickConfig);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignSystemSortSongs) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.System, EKeyConfigPad.SortSongs);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignSystemToggleAutoP1) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.System, EKeyConfigPad.ToggleAutoP1);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignSystemToggleAutoP2) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.System, EKeyConfigPad.ToggleAutoP2);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignSystemToggleTrainingMode) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.System, EKeyConfigPad.ToggleTrainingMode);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignSystemCycleVideoDisplayMode) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.System, EKeyConfigPad.CycleVideoDisplayMode);
		}

		// Training controls
		else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTrainingPause) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.TrainingPause);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTrainingToggleAuto) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.TrainingToggleAuto);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTrainingBookmark) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.TrainingBookmark);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTrainingIncreaseScrollSpeed) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.TrainingIncreaseScrollSpeed);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTrainingDecreaseScrollSpeed) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.TrainingDecreaseScrollSpeed);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTrainingIncreaseSongSpeed) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.TrainingIncreaseSongSpeed);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTrainingDecreaseSongSpeed) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.TrainingDecreaseSongSpeed);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTrainingBranchNormal) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.TrainingBranchNormal);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTrainingBranchExpert) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.TrainingBranchExpert);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTrainingBranchMaster) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.TrainingBranchMaster);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTrainingMoveForwardMeasure) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.TrainingMoveForwardMeasure);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTrainingMoveBackMeasure) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.TrainingMoveBackMeasure);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTrainingSkipForwardMeasure) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.TrainingSkipForwardMeasure);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTrainingSkipBackMeasure) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.TrainingSkipBackMeasure);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTrainingJumpToFirstMeasure) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.TrainingJumpToFirstMeasure);
		} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTrainingJumpToLastMeasure) {
			OpenTaiko.stageConfig.tPadSelectedNotify(EKeyConfigPart.Taiko, EKeyConfigPad.TrainingJumpToLastMeasure);
		}
		#endregion
		else {
			// #27029 2012.1.5 from
			this.listItemList[this.nCurrentSelectedItem].tEnterPressed();

			if (this.listItemList[this.nCurrentSelectedItem] == this.iSystemLanguage) {
				OpenTaiko.ConfigIni.sLang = CLangManager.intToLang(this.iSystemLanguage.nCurrentSelectedItemNumber);
				CLangManager.langAttach(OpenTaiko.ConfigIni.sLang);

				prvFont?.Dispose();
				OpenTaiko.stageConfig.ftFont?.Dispose();
				pfMenuTitle?.Dispose();
				pfBoxText?.Dispose();

				prvFont = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.Config_Font_Scale);
				OpenTaiko.stageConfig.ftFont = HPrivateFastFont.tInstantiateMainFont((int)OpenTaiko.Skin.Config_Font_Scale_Description, CFontRenderer.FontStyle.Bold);
				pfMenuTitle = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.Title_ModeSelect_Title_Scale[0]);
				pfBoxText = HPrivateFastFont.tInstantiateBoxFont(OpenTaiko.Skin.Title_ModeSelect_Title_Scale[1]);
				OpenTaiko.actEnumSongs.RefreshSkin(OpenTaiko.EnumSongs.IsEnumerating);

				tItemListSettings_System(refresh: false);
				OpenTaiko.stageConfig.ReloadMenus();
			}
			// Enter押下後の後処理

			if (this.listItemList[this.nCurrentSelectedItem] == this.iSystemWindowMode) {
				OpenTaiko.ConfigIni.nWindowMode = this.iSystemWindowMode.nCurrentSelectedItemNumber;
				OpenTaiko.app.bSwitchFullScreenAtNextFrame = true;
			} else if (this.listItemList[this.nCurrentSelectedItem] == this.iSystemVSyncWait) {
				OpenTaiko.ConfigIni.bEnableVSync = this.iSystemVSyncWait.bON;
				OpenTaiko.app.bSwitchVSyncAtTheNextFrame = true;
			}
			#region [ キーアサインへの遷移と脱出 ]
			else if (this.listItemList[this.nCurrentSelectedItem] == this.iSystemGoToKeyAssign)          // #24609 2011.4.12 yyagi
			{
				tItemListSettings_KeyAssignSystem();
			} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignSystemReturnToMenu)    // #24609 2011.4.12 yyagi
			{
				tConfigIniRecord();
				tItemListSettings_System();
			} else if (this.listItemList[this.nCurrentSelectedItem] == this.iDrumsGoToKeyAssign)               // #24525 2011.3.15 yyagi
			{
				tConfigIniRecord();
				tItemListSettings_KeyAssignDrums();
			} else if (this.listItemList[this.nCurrentSelectedItem] == this.iDrumsGoToTrainingKeyAssign) {
				tConfigIniRecord();
				tItemListSettings_KeyAssignTraining();
			} else if (this.listItemList[this.nCurrentSelectedItem] == this.iDrumsGoToCalibration) {
				OpenTaiko.stageConfig.actCalibrationMode.Start();
			} else if (this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignDrumsReturnToMenu ||
					   this.listItemList[this.nCurrentSelectedItem] == this.iKeyAssignTrainingReturnToMenu)     // #24525 2011.3.15 yyagi
			{
				tItemListSettings_Drums();
			}
			#endregion
			#region [ スキン項目でEnterを押下した場合に限り、スキンの縮小サンプルを生成する。]
			else if (this.listItemList[this.nCurrentSelectedItem] == this.iSystemSkinSubfolder)          // #28195 2012.5.2 yyagi
			{
				tGenerateSkinSample();
			}
			#endregion
			#region [ 曲データ一覧の再読み込み ]
			else if (this.listItemList[this.nCurrentSelectedItem] == this.iSystemReloadDTX)              // #32081 2013.10.21 yyagi
			{
				if (OpenTaiko.EnumSongs.IsEnumerating) {
					// Debug.WriteLine( "バックグラウンドでEnumeratingSongs中だったので、一旦中断します。" );
					OpenTaiko.EnumSongs.Abort();
					OpenTaiko.actEnumSongs.DeActivate();
				}

				OpenTaiko.EnumSongs.StartEnumFromDisk();
				OpenTaiko.EnumSongs.ChangeEnumeratePriority(ThreadPriority.Normal);
				OpenTaiko.actEnumSongs.bCommandSongDataGet = true;
				OpenTaiko.actEnumSongs.Activate();
			} else if (this.listItemList[this.nCurrentSelectedItem] == this.iSystemHardReloadDTX)              // #32081 2013.10.21 yyagi
			{
				if (OpenTaiko.EnumSongs.IsEnumerating) {
					OpenTaiko.EnumSongs.Abort();
					OpenTaiko.actEnumSongs.DeActivate();
				}

				OpenTaiko.EnumSongs.StartEnumFromDisk(true);
				OpenTaiko.EnumSongs.ChangeEnumeratePriority(ThreadPriority.Normal);
				OpenTaiko.actEnumSongs.bCommandSongDataGet = true;
				OpenTaiko.actEnumSongs.Activate();
			} else if (this.listItemList[this.nCurrentSelectedItem] == this.isSystemImportingScore) {
				// Running in a separate thread so the game doesn't freeze
				ScoreIniImportThread = new Thread(CScoreIni_Importer.ImportScoreInisToSavesDb3);
				ScoreIniImportThread.Start();
			}
			#endregion
		}
	}

	private void tGenerateSkinSample() {

		nSkinIndex = ((CItemList)this.listItemList[this.nCurrentSelectedItem]).nCurrentSelectedItemNumber;
		if (nSkinSampleIndex != nSkinIndex) {
			string path = skinSubFolders[nSkinIndex];
			path = System.IO.Path.Combine(path, @$"Graphics{Path.DirectorySeparatorChar}1_Title{Path.DirectorySeparatorChar}Background.png");
			SKBitmap bmSrc = SKBitmap.Decode(path);

			int _w = OpenTaiko.Skin.Resolution[0] / 4;
			int _h = OpenTaiko.Skin.Resolution[1] / 4;

			if (txSkinSample1 != null) {
				OpenTaiko.tDisposeSafely(ref txSkinSample1);
			}
			txSkinSample1 = OpenTaiko.tTextureCreate(bmSrc, false);

			txSkinSample1.vcScaleRatio = new Silk.NET.Maths.Vector3D<float>(_w / (float)txSkinSample1.szTextureSize.Width, _h / (float)txSkinSample1.szTextureSize.Height, 0);

			bmSrc.Dispose();
			nSkinSampleIndex = nSkinIndex;
		}
	}

	#region [ 項目リストの設定 ( Exit, KeyAssignSystem/Drums) ]
	public void tItemListSettings_Exit() {
		this.tConfigIniRecord();
		this.eMenuType = EMenuType.Unknown;
	}
	public void tItemListSettings_KeyAssignSystem() {
		this.listItemList.Clear();
		// #27029 2012.1.5 from: 説明文は最大9行→13行に変更。

		this.iKeyAssignSystemReturnToMenu = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_MENU_RETURN"), CItemBase.EPanelType.Other,
			CLangManager.LangInstance.GetString("SETTINGS_MENU_RETURN_DESC"));
		this.listItemList.Add(this.iKeyAssignSystemReturnToMenu);

		this.iKeyAssignSystemCapture = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_CAPTURE"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_CAPTURE_DESC"));
		this.listItemList.Add(this.iKeyAssignSystemCapture);
		this.iKeyAssignSystemSongVolIncrease = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_INCREASEVOL"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_INCREASEVOL_DESC"));
		this.listItemList.Add(this.iKeyAssignSystemSongVolIncrease);
		this.iKeyAssignSystemSongVolDecrease = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_DECREASEVOL"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_DECREASEVOL_DESC"));
		this.listItemList.Add(this.iKeyAssignSystemSongVolDecrease);
		this.iKeyAssignSystemDisplayHit = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_DISPLAYHITS"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_DISPLAYHITS_DESC"));
		this.listItemList.Add(this.iKeyAssignSystemDisplayHit);
		this.iKeyAssignSystemDisplayDebug = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_DISPLAYDEBUG"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_DISPLAYDEBUG_DESC"));
		this.listItemList.Add(this.iKeyAssignSystemDisplayDebug);
		this.iKeyAssignSystemQuickConfig = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_QUICKCONFIG"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_QUICKCONFIG_DESC"));
		this.listItemList.Add(this.iKeyAssignSystemQuickConfig);
		this.iKeyAssignSystemSortSongs = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_SONGSORT"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_SONGSORT_DESC"));
		this.listItemList.Add(this.iKeyAssignSystemSortSongs);
		this.iKeyAssignSystemToggleAutoP1 = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_AUTO1P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_AUTO1P_DESC"));
		this.listItemList.Add(this.iKeyAssignSystemToggleAutoP1);
		this.iKeyAssignSystemToggleAutoP2 = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_AUTO2P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_AUTO2P_DESC"));
		this.listItemList.Add(this.iKeyAssignSystemToggleAutoP2);
		this.iKeyAssignSystemToggleTrainingMode = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_TRAINING"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_TRAINING_DESC"));
		this.listItemList.Add(this.iKeyAssignSystemToggleTrainingMode);
		this.iKeyAssignSystemCycleVideoDisplayMode = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_BGMOVIEDISPLAY"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_BGMOVIEDISPLAY_DESC"));
		this.listItemList.Add(this.iKeyAssignSystemCycleVideoDisplayMode);

		OnListMenuInitialize();
		this.nCurrentSelectedItem = 0;
		this.eMenuType = EMenuType.KeyAssignSystem;
	}
	public void tItemListSettings_KeyAssignDrums() {
		this.listItemList.Clear();
		// #27029 2012.1.5 from: 説明文は最大9行→13行に変更。

		this.iKeyAssignDrumsReturnToMenu = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_MENU_RETURN"), CItemBase.EPanelType.Other,
			CLangManager.LangInstance.GetString("SETTINGS_MENU_RETURN_DESC"));
		this.listItemList.Add(this.iKeyAssignDrumsReturnToMenu);

		this.iKeyAssignTaikoLRed = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTRED"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTRED_DESC"));
		this.listItemList.Add(this.iKeyAssignTaikoLRed);
		this.iKeyAssignTaikoRRed = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTRED"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTRED_DESC"));
		this.listItemList.Add(this.iKeyAssignTaikoRRed);
		this.iKeyAssignTaikoLBlue = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTBLUE"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTBLUE_DESC"));
		this.listItemList.Add(this.iKeyAssignTaikoLBlue);
		this.iKeyAssignTaikoRBlue = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTBLUE"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTBLUE_DESC"));
		this.listItemList.Add(this.iKeyAssignTaikoRBlue);
		this.iKeyAssignKongaClap = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CLAP"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CLAP_DESC"));
		this.listItemList.Add(this.iKeyAssignKongaClap);

		this.iKeyAssignTaikoLRed2P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTRED2P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTRED2P_DESC"));
		this.listItemList.Add(this.iKeyAssignTaikoLRed2P);
		this.iKeyAssignTaikoRRed2P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTRED2P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTRED2P_DESC"));
		this.listItemList.Add(this.iKeyAssignTaikoRRed2P);
		this.iKeyAssignTaikoLBlue2P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTBLUE2P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTBLUE2P_DESC"));
		this.listItemList.Add(this.iKeyAssignTaikoLBlue2P);
		this.iKeyAssignTaikoRBlue2P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTBLUE2P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTBLUE2P_DESC"));
		this.listItemList.Add(this.iKeyAssignTaikoRBlue2P);
		this.iKeyAssignKongaClap2P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CLAP2P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CLAP2P_DESC"));
		this.listItemList.Add(this.iKeyAssignKongaClap2P);

		this.iKeyAssignTaikoLRed3P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTRED3P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTRED3P_DESC"));
		this.listItemList.Add(this.iKeyAssignTaikoLRed3P);
		this.iKeyAssignTaikoRRed3P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTRED3P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTRED3P_DESC"));
		this.listItemList.Add(this.iKeyAssignTaikoRRed3P);
		this.iKeyAssignTaikoLBlue3P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTBLUE3P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTBLUE3P_DESC"));
		this.listItemList.Add(this.iKeyAssignTaikoLBlue3P);
		this.iKeyAssignTaikoRBlue3P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTBLUE3P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTBLUE3P_DESC"));
		this.listItemList.Add(this.iKeyAssignTaikoRBlue3P);
		this.iKeyAssignKongaClap3P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CLAP3P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CLAP3P_DESC"));
		this.listItemList.Add(this.iKeyAssignKongaClap3P);

		this.iKeyAssignTaikoLRed4P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTRED4P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTRED4P_DESC"));
		this.listItemList.Add(this.iKeyAssignTaikoLRed4P);
		this.iKeyAssignTaikoRRed4P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTRED4P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTRED4P_DESC"));
		this.listItemList.Add(this.iKeyAssignTaikoRRed4P);
		this.iKeyAssignTaikoLBlue4P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTBLUE4P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTBLUE4P_DESC"));
		this.listItemList.Add(this.iKeyAssignTaikoLBlue4P);
		this.iKeyAssignTaikoRBlue4P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTBLUE4P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTBLUE4P_DESC"));
		this.listItemList.Add(this.iKeyAssignTaikoRBlue4P);
		this.iKeyAssignKongaClap4P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CLAP4P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CLAP4P_DESC"));
		this.listItemList.Add(this.iKeyAssignKongaClap4P);

		this.iKeyAssignTaikoLRed5P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTRED5P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTRED5P_DESC"));
		this.listItemList.Add(this.iKeyAssignTaikoLRed5P);
		this.iKeyAssignTaikoRRed5P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTRED5P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTRED5P_DESC"));
		this.listItemList.Add(this.iKeyAssignTaikoRRed5P);
		this.iKeyAssignTaikoLBlue5P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTBLUE5P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTBLUE5P_DESC"));
		this.listItemList.Add(this.iKeyAssignTaikoLBlue5P);
		this.iKeyAssignTaikoRBlue5P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTBLUE5P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTBLUE5P_DESC"));
		this.listItemList.Add(this.iKeyAssignTaikoRBlue5P);
		this.iKeyAssignKongaClap5P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CLAP5P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CLAP5P_DESC"));
		this.listItemList.Add(this.iKeyAssignKongaClap5P);

		this.iKeyAssignDecide = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_DECIDE"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_DECIDE_DESC"));
		this.listItemList.Add(this.iKeyAssignDecide);
		this.iKeyAssignCancel = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CANCEL"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CANCEL_DESC"));
		this.listItemList.Add(this.iKeyAssignCancel);

		this.iKeyAssignLeftChange = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTCHANGE"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTCHANGE_DESC"));
		this.listItemList.Add(this.iKeyAssignLeftChange);
		this.iKeyAssignRightChange = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTCHANGE"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTCHANGE_DESC"));
		this.listItemList.Add(this.iKeyAssignRightChange);

		OnListMenuInitialize();
		this.nCurrentSelectedItem = 0;
		this.eMenuType = EMenuType.KeyAssignDrums;
	}
	public void tItemListSettings_KeyAssignTraining() {
		this.listItemList.Clear();

		this.iKeyAssignTrainingReturnToMenu = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_MENU_RETURN"), CItemBase.EPanelType.Other,
			CLangManager.LangInstance.GetString("SETTINGS_MENU_RETURN_DESC"));
		this.listItemList.Add(this.iKeyAssignTrainingReturnToMenu);


		this.iKeyAssignTrainingPause = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_PAUSE"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_PAUSE_DESC"));
		this.listItemList.Add(this.iKeyAssignTrainingPause);

		this.iKeyAssignTrainingToggleAuto = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_AUTO"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_AUTO_DESC"));
		this.listItemList.Add(this.iKeyAssignTrainingToggleAuto);

		this.iKeyAssignTrainingBookmark = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_BOOKMARK"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_BOOKMARK_DESC"));
		this.listItemList.Add(this.iKeyAssignTrainingBookmark);

		this.iKeyAssignTrainingIncreaseScrollSpeed = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_INCREASESCROLL"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_INCREASESCROLL_DESC"));
		this.listItemList.Add(this.iKeyAssignTrainingIncreaseScrollSpeed);

		this.iKeyAssignTrainingDecreaseScrollSpeed = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_DECREASESCROLL"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_DECREASESCROLL_DESC"));
		this.listItemList.Add(this.iKeyAssignTrainingDecreaseScrollSpeed);

		this.iKeyAssignTrainingIncreaseSongSpeed = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_INCREASESPEED"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_INCREASESPEED_DESC"));
		this.listItemList.Add(this.iKeyAssignTrainingIncreaseSongSpeed);

		this.iKeyAssignTrainingDecreaseSongSpeed = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_DECREASESPEED"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_DECREASESPEED_DESC"));
		this.listItemList.Add(this.iKeyAssignTrainingDecreaseSongSpeed);

		this.iKeyAssignTrainingBranchNormal = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_BRANCHNORMAL"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_BRANCHNORMAL_DESC"));
		this.listItemList.Add(this.iKeyAssignTrainingBranchNormal);

		this.iKeyAssignTrainingBranchExpert = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_BRANCHEXPERT"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_BRANCHEXPERT_DESC"));
		this.listItemList.Add(this.iKeyAssignTrainingBranchExpert);

		this.iKeyAssignTrainingBranchMaster = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_BRANCHMASTER"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_BRANCHMASTER_DESC"));
		this.listItemList.Add(this.iKeyAssignTrainingBranchMaster);

		this.iKeyAssignTrainingMoveForwardMeasure = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_MOVEFORWARD"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_MOVEFORWARD_DESC"));
		this.listItemList.Add(this.iKeyAssignTrainingMoveForwardMeasure);

		this.iKeyAssignTrainingMoveBackMeasure = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_MOVEBACKWARD"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_MOVEBACKWARD_DESC"));
		this.listItemList.Add(this.iKeyAssignTrainingMoveBackMeasure);

		this.iKeyAssignTrainingSkipForwardMeasure = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_SKIPFORWARD"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_SKIPFORWARD_DESC"));
		this.listItemList.Add(this.iKeyAssignTrainingSkipForwardMeasure);

		this.iKeyAssignTrainingSkipBackMeasure = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_SKIPBACKWARD"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_SKIPBACKWARD_DESC"));
		this.listItemList.Add(this.iKeyAssignTrainingSkipBackMeasure);

		this.iKeyAssignTrainingJumpToFirstMeasure = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_JUMPTOFIRST"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_JUMPTOFIRST_DESC"));
		this.listItemList.Add(this.iKeyAssignTrainingJumpToFirstMeasure);

		this.iKeyAssignTrainingJumpToLastMeasure = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_JUMPTOLAST"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_JUMPTOLAST_DESC"));
		this.listItemList.Add(this.iKeyAssignTrainingJumpToLastMeasure);

		OnListMenuInitialize();
		this.nCurrentSelectedItem = 0;
		this.eMenuType = EMenuType.KeyAssignTraining;
	}
	#endregion
	public void tNextMove() {
		OpenTaiko.Skin.soundCursorMoveSound.tPlay();
		if (this.bElementValueFocus) {
			this.listItemList[this.nCurrentSelectedItem].tItemValuePrevMove();
		} else {
			this.nTargetScrollCounter += 100;
		}
	}
	public void tPrevMove() {
		OpenTaiko.Skin.soundCursorMoveSound.tPlay();
		if (this.bElementValueFocus) {
			this.listItemList[this.nCurrentSelectedItem].tItemValueNextMove();
		} else {
			this.nTargetScrollCounter -= 100;
		}
	}

	// CActivity 実装

	public override void Activate() {
		if (this.IsActivated)
			return;

		this.listItemList = new List<CItemBase>();
		this.eMenuType = EMenuType.Unknown;

		#region [ スキン選択肢と、現在選択中のスキン(index)の準備 #28195 2012.5.2 yyagi ]
		int ns = (OpenTaiko.Skin.strSystemSkinSubfolders == null) ? 0 : OpenTaiko.Skin.strSystemSkinSubfolders.Length;
		int nb = (OpenTaiko.Skin.strBoxDefSkinSubfolders == null) ? 0 : OpenTaiko.Skin.strBoxDefSkinSubfolders.Length;
		skinSubFolders = new string[ns + nb];
		for (int i = 0; i < ns; i++) {
			skinSubFolders[i] = OpenTaiko.Skin.strSystemSkinSubfolders[i];
		}
		for (int i = 0; i < nb; i++) {
			skinSubFolders[ns + i] = OpenTaiko.Skin.strBoxDefSkinSubfolders[i];
		}
		skinSubFolder_org = OpenTaiko.Skin.GetCurrentSkinSubfolderFullName(true);
		fRenderScale_org = OpenTaiko.ConfigIni.fRenderScale;
		Array.Sort(skinSubFolders);
		skinNames = CSkin.GetSkinName(skinSubFolders);
		nSkinIndex = Array.BinarySearch(skinSubFolders, skinSubFolder_org);
		if (nSkinIndex < 0) // 念のため
		{
			nSkinIndex = 0;
		}
		nSkinSampleIndex = -1;
		#endregion

		this.tItemListSettings_Drums();
		this.tItemListSettings_System();    // 順番として、最後にSystemを持ってくること。設定一覧の初期位置がSystemのため。
		this.bElementValueFocus = false;
		this.nTargetScrollCounter = 0;
		this.nCurrentScrollCounter = 0;
		this.nScrollTimerValue = -1;
		this.ctTriangleArrowAnime = new CCounter();

		this.iSystemBassBufferSizeMs_initial = this.iSystemBassBufferSizeMs.nCurrentValue;              // CONFIG脱出時にこの値から変更されているようなら
		if (OperatingSystem.IsWindows()) {
			this.iSystemSoundType_initial = this.iSystemSoundType.nCurrentSelectedItemNumber;   // CONFIGに入ったときの値を保持しておく
			this.iSystemWASAPIBufferSizeMs_initial = this.iSystemWASAPIBufferSizeMs.nCurrentValue;              // CONFIG脱出時にこの値から変更されているようなら
			this.iSystemASIODevice_initial = this.iSystemASIODevice.nCurrentSelectedItemNumber;
		}
		this.iSystemSoundTimerType_initial = this.iSystemSoundTimerType.GetIndex();
		base.Activate();
	}
	public override void DeActivate() {
		if (this.IsDeActivated)
			return;

		this.tConfigIniRecord();
		this.listItemList.Clear();
		this.ctTriangleArrowAnime = null;

		base.DeActivate();
		#region [ Skin変更 ]
		if (OpenTaiko.Skin.GetCurrentSkinSubfolderFullName(true) != this.skinSubFolder_org
			|| OpenTaiko.ConfigIni.fRenderScale != this.fRenderScale_org) {
			OpenTaiko.app.EnterRefreshSkinStage(isSavedBeforeUpdate: true);   // resolution change reloads the skin too
		}
		#endregion

		for (int i = 0; i < OpenTaiko.MAX_PLAYERS; i++) {
			int id = OpenTaiko.SaveFileInstances[i].data.TitleId;
			if (id > 0) {
				var title = OpenTaiko.Databases.DBNameplateUnlockables.data[id];
				OpenTaiko.SaveFileInstances[i].data.Title = title.nameplateInfo.cld.GetString("");
			}
			OpenTaiko.NamePlate.tNamePlateRefreshTitles(i);
		}
		// #24820 2013.1.22 yyagi CONFIGでWASAPI/ASIO/DirectSound関連の設定を変更した場合、サウンドデバイスを再構築する。
		// #33689 2014.6.17 yyagi CONFIGでSoundTimerTypeの設定を変更した場合も、サウンドデバイスを再構築する。
		#region [ サウンドデバイス変更 ]
		if (OperatingSystem.IsWindows()) {
			if (this.iSystemSoundType_initial != this.iSystemSoundType.nCurrentSelectedItemNumber ||
			this.iSystemBassBufferSizeMs_initial != this.iSystemBassBufferSizeMs.nCurrentValue ||
			this.iSystemWASAPIBufferSizeMs_initial != this.iSystemWASAPIBufferSizeMs.nCurrentValue ||
			this.iSystemASIODevice_initial != this.iSystemASIODevice.nCurrentSelectedItemNumber ||
			this.iSystemSoundTimerType_initial != this.iSystemSoundTimerType.GetIndex()) {
				ESoundDeviceType soundDeviceType;
				switch (this.iSystemSoundType.nCurrentSelectedItemNumber) {
					case 0:
						soundDeviceType = ESoundDeviceType.Bass;
						break;
					case 1:
						soundDeviceType = ESoundDeviceType.ASIO;
						break;
					case 2:
						soundDeviceType = ESoundDeviceType.ExclusiveWASAPI;
						break;
					case 3:
						soundDeviceType = ESoundDeviceType.SharedWASAPI;
						break;
					default:
						soundDeviceType = ESoundDeviceType.Unknown;
						break;
				}
				OpenTaiko.SoundManager.tInitialize(soundDeviceType,
					this.iSystemBassBufferSizeMs.nCurrentValue,
					this.iSystemWASAPIBufferSizeMs.nCurrentValue,
					0,
					this.iSystemASIODevice.nCurrentSelectedItemNumber,
					this.iSystemSoundTimerType.bON);
				OpenTaiko.app.ShowWindowTitle();
				OpenTaiko.Skin.ReloadSystemSounds();
				OpenTaiko.Skin.PreloadSystemSounds();
			}
		} else {
			if (this.iSystemBassBufferSizeMs_initial != this.iSystemBassBufferSizeMs.nCurrentValue ||
				this.iSystemSoundTimerType_initial != this.iSystemSoundTimerType.GetIndex()) {
				OpenTaiko.SoundManager.tInitialize(ESoundDeviceType.Bass,
					this.iSystemBassBufferSizeMs.nCurrentValue,
					0,
					0,
					0,
					this.iSystemSoundTimerType.bON);
			}
		}
		#endregion
		#region [ サウンドのタイムストレッチモード変更 ]
		FDK.SoundManager.bIsTimeStretch = this.iSystemTimeStretch.bON;
		#endregion
	}
	public override void CreateManagedResource() {
		this.prvFont = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.Config_Font_Scale);    // t項目リストの設定 の前に必要
		this.txSkinSample1 = null;      // スキン選択時に動的に設定するため、ここでは初期化しない
		base.CreateManagedResource();
	}
	public override void ReleaseManagedResource() {
		prvFont.Dispose();
		OpenTaiko.tTextureRelease(ref this.txSkinSample1);
		base.ReleaseManagedResource();
	}
	private void OnListMenuInitialize() {
		OnListMenuRelease();
		this.listMenu = new stMenuItemRight[this.listItemList.Count];
	}

	/// <summary>
	/// 事前にレンダリングしておいたテクスチャを解放する。
	/// </summary>
	private void OnListMenuRelease() {
		if (listMenu != null) {
			for (int i = 0; i < listMenu.Length; i++) {
				if (listMenu[i].txParam != null) {
					listMenu[i].txParam.Dispose();
				}
				if (listMenu[i].txMenuItemRight != null) {
					listMenu[i].txMenuItemRight.Dispose();
				}
			}
			this.listMenu = null;
		}
	}
	public override int Draw() {
		throw new InvalidOperationException("Draw(bool)のほうを使用してください。");
	}
	public int Draw(bool bItemListSideFocus) {
		if (this.IsDeActivated)
			return 0;

		// 進行

		#region [ 初めての進行描画 ]
		//-----------------
		if (base.IsFirstDraw) {
			this.nScrollTimerValue = OpenTaiko.Timer.NowTimeMs;
			this.ctTriangleArrowAnime.Start(0, 9, 50, OpenTaiko.Timer);
			base.IsFirstDraw = false;
		}
		//-----------------
		#endregion

		this.bItemListSideFocus = bItemListSideFocus;       // 記憶

		#region [ 項目スクロールの進行 ]
		//-----------------
		long nCurrentTime = OpenTaiko.Timer.NowTimeMs;
		if (nCurrentTime < this.nScrollTimerValue) this.nScrollTimerValue = nCurrentTime;

		const int INTERVAL = 2; // [ms]
		while ((nCurrentTime - this.nScrollTimerValue) >= INTERVAL) {
			int nScrollAmountToTargetItem = Math.Abs((int)(this.nTargetScrollCounter - this.nCurrentScrollCounter));
			int nAcceleration = 0;

			#region [ n加速度の決定；目標まで遠いほど加速する。]
			//-----------------
			if (nScrollAmountToTargetItem <= 100) {
				nAcceleration = 2;
			} else if (nScrollAmountToTargetItem <= 300) {
				nAcceleration = 3;
			} else if (nScrollAmountToTargetItem <= 500) {
				nAcceleration = 4;
			} else {
				nAcceleration = 8;
			}
			//-----------------
			#endregion
			#region [ this.n現在のスクロールカウンタに n加速度 を加減算。]
			//-----------------
			if (this.nCurrentScrollCounter < this.nTargetScrollCounter) {
				this.nCurrentScrollCounter += nAcceleration;
				if (this.nCurrentScrollCounter > this.nTargetScrollCounter) {
					// 目標を超えたら目標値で停止。
					this.nCurrentScrollCounter = this.nTargetScrollCounter;
				}
			} else if (this.nCurrentScrollCounter > this.nTargetScrollCounter) {
				this.nCurrentScrollCounter -= nAcceleration;
				if (this.nCurrentScrollCounter < this.nTargetScrollCounter) {
					// 目標を超えたら目標値で停止。
					this.nCurrentScrollCounter = this.nTargetScrollCounter;
				}
			}
			//-----------------
			#endregion
			#region [ 行超え処理、ならびに目標位置に到達したらスクロールを停止して項目変更通知を発行。]
			//-----------------
			if (this.nCurrentScrollCounter >= 100) {
				this.nCurrentSelectedItem = this.tNextItem(this.nCurrentSelectedItem);
				this.nCurrentScrollCounter -= 100;
				this.nTargetScrollCounter -= 100;
				if (this.nTargetScrollCounter == 0) {
					OpenTaiko.stageConfig.tItemChangeNotify();
				}
			} else if (this.nCurrentScrollCounter <= -100) {
				this.nCurrentSelectedItem = this.tPrevItem(this.nCurrentSelectedItem);
				this.nCurrentScrollCounter += 100;
				this.nTargetScrollCounter += 100;
				if (this.nTargetScrollCounter == 0) {
					OpenTaiko.stageConfig.tItemChangeNotify();
				}
			}
			//-----------------
			#endregion

			this.nScrollTimerValue += INTERVAL;
		}
		//-----------------
		#endregion

		#region [ ▲印アニメの進行 ]
		//-----------------
		if (this.bItemListSideFocus && (this.nTargetScrollCounter == 0))
			this.ctTriangleArrowAnime.TickLoop();
		//-----------------
		#endregion

		#region [ Theme string input update ]
		//-----------------
		if (themeStringInput != null && this.bElementValueFocus && activeThemeStringId != null) {
			// Update the CItemString value in real time so the panel reflects live typing.
			if (themeItems.TryGetValue(activeThemeStringId, out var liveItem) && liveItem is CItemString liveCis)
				liveCis.Value = themeStringInput.Text;

			if (themeStringInput.Update()) {
				// Enter confirmed — the value is already up-to-date from the live update above.
				themeStringInput = null;
				activeThemeStringId = null;
				this.bElementValueFocus = false;
				_stringInputJustCommitted = true;
			}
		}
		//-----------------
		#endregion

		// 描画

		#region [ 計11個の項目パネルを描画する。]
		//-----------------
		int nItem = this.nCurrentSelectedItem;
		for (int i = 0; i < (OpenTaiko.Skin.Config_ItemBox_Count / 2) - 1; i++)
			nItem = this.tPrevItem(nItem);

		for (int i = 0; i < OpenTaiko.Skin.Config_ItemBox_Count; i++)      // n行番号 == 0 がフォーカスされている項目パネル。
		{
			bool centerFlag = i == (OpenTaiko.Skin.Config_ItemBox_Count / 2) - 1;

			#region [ 今まさに画面外に飛びだそうとしている項目パネルは描画しない。]
			//-----------------
			if (((i == 0) && (this.nCurrentScrollCounter > 0)) ||       // 上に飛び出そうとしている
				((i == OpenTaiko.Skin.Config_ItemBox_Count - 1) && (this.nCurrentScrollCounter < 0)))      // 下に飛び出そうとしている
			{
				nItem = this.tNextItem(nItem);
				continue;
			}
			//-----------------
			#endregion

			int nMoveDestLineBasicPosition = (this.nCurrentScrollCounter <= 0) ? ((i + 1) % OpenTaiko.Skin.Config_ItemBox_Count) : (((i - 1) + OpenTaiko.Skin.Config_ItemBox_Count) % OpenTaiko.Skin.Config_ItemBox_Count);
			int x = OpenTaiko.Skin.Config_ItemBox_X[i] + ((int)((OpenTaiko.Skin.Config_ItemBox_X[nMoveDestLineBasicPosition] - OpenTaiko.Skin.Config_ItemBox_X[i]) * (((double)Math.Abs(this.nCurrentScrollCounter)) / 100.0)));
			int y = OpenTaiko.Skin.Config_ItemBox_Y[i] + ((int)((OpenTaiko.Skin.Config_ItemBox_Y[nMoveDestLineBasicPosition] - OpenTaiko.Skin.Config_ItemBox_Y[i]) * (((double)Math.Abs(this.nCurrentScrollCounter)) / 100.0)));

			#region [ 現在の行の項目パネル枠を描画。]
			//-----------------
			switch (this.listItemList[nItem].ePanelType) {
				case CItemBase.EPanelType.Normal:
				case CItemBase.EPanelType.Other:
					if (OpenTaiko.Tx.Config_ItemBox != null)
						OpenTaiko.Tx.Config_ItemBox.t2DDraw(x, y);
					break;
			}
			//-----------------
			#endregion
			#region [ 現在の行の項目名を描画。]
			//-----------------
			if (listMenu[nItem].txMenuItemRight != null)    // 自前のキャッシュに含まれているようなら、再レンダリングせずキャッシュを使用
			{
				listMenu[nItem].txMenuItemRight.t2DDraw(x + OpenTaiko.Skin.Config_ItemBox_Font_Offset[0], y + OpenTaiko.Skin.Config_ItemBox_Font_Offset[1]);
			} else {
				using (var bmpItem = prvFont.DrawText(this.listItemList[nItem].strItemName, Color.White, Color.Black, null, 30)) {
					listMenu[nItem].txMenuItemRight = OpenTaiko.tTextureCreate(bmpItem);
				}
			}
			//-----------------
			#endregion
			#region [ 現在の行の項目の要素を描画。]
			//-----------------
			string strParam = null;
			bool bEmphasis = false;
			switch (this.listItemList[nItem].eType) {
				case CItemBase.EType.ONorOFFToggle:
					#region [ *** ]
					//-----------------
					strParam = ((CItemToggle)this.listItemList[nItem]).bON ? "ON" : "OFF";
					break;
				//-----------------
				#endregion

				case CItemBase.EType.ONorOFForIndeterminateThreeState:
					#region [ *** ]
					//-----------------
					switch (((CItemThreeState)this.listItemList[nItem]).eCurrentState) {
						case CItemThreeState.EState.ON:
							strParam = "ON";
							break;

						case CItemThreeState.EState.Indeterminate:
							strParam = "- -";
							break;

						default:
							strParam = "OFF";
							break;
					}
					break;
				//-----------------
				#endregion

				case CItemBase.EType.Int:      // #24789 2011.4.8 yyagi: add PlaySpeed supports (copied them from OPTION)
					#region [ *** ]
					//-----------------
					if (this.listItemList[nItem] == this.iCommonPlaySpeed) {
						double d = ((double)((CItemInteger)this.listItemList[nItem]).nCurrentValue) / 20.0;
						strParam = d.ToString("0.000");
					} else {
						strParam = ((CItemInteger)this.listItemList[nItem]).nCurrentValue.ToString();
					}
					bEmphasis = centerFlag && this.bElementValueFocus;
					break;
				//-----------------
				#endregion

				case CItemBase.EType.List: // #28195 2012.5.2 yyagi: add Skin supports
					#region [ *** ]
				//-----------------
				{
						CItemList list = (CItemList)this.listItemList[nItem];
						strParam = list.listItemValue[list.nCurrentSelectedItemNumber];

						#region [ 必要な場合に、Skinのサンプルを生成・描画する。#28195 2012.5.2 yyagi ]
						if (this.listItemList[this.nCurrentSelectedItem] == this.iSystemSkinSubfolder) {
							tGenerateSkinSample();      // 最初にSkinの選択肢にきたとき(Enterを押す前)に限り、サンプル生成が発生する。
							if (txSkinSample1 != null) {
								txSkinSample1.t2DDraw(OpenTaiko.Skin.Config_SkinSample1[0], OpenTaiko.Skin.Config_SkinSample1[1]);
							}
						}
						#endregion
						break;
					}
					//-----------------
					#endregion
			}
			if (bEmphasis) {
				using (var bmpStr = prvFont.DrawText(strParam,
						   Color.Black,
						   Color.White,
						   null,
						   OpenTaiko.Skin.Config_Selected_Menu_Text_Grad_Color_1,
						   OpenTaiko.Skin.Config_Selected_Menu_Text_Grad_Color_2,
						   30)) {
					using (var txStr = OpenTaiko.tTextureCreate(bmpStr, false)) {
						txStr.t2DDraw(x + OpenTaiko.Skin.Config_ItemBox_ItemValue_Font_Offset[0], y + OpenTaiko.Skin.Config_ItemBox_ItemValue_Font_Offset[1]);
					}
				}
			} else {
				int nIndex = this.listItemList[nItem].GetIndex();
				if (listMenu[nItem].nParam != nIndex || listMenu[nItem].txParam == null) {
					stMenuItemRight stm = listMenu[nItem];
					stm.nParam = nIndex;
					object o = this.listItemList[nItem].objCurrentValue();
					stm.strParam = (o == null) ? "" : o.ToString();

					using (var bmpStr = prvFont.DrawText(stm.strParam, Color.White, Color.Black, null, 30)) {
						stm.txParam = OpenTaiko.tTextureCreate(bmpStr, false);
					}

					listMenu[nItem] = stm;
				}
				listMenu[nItem].txParam.t2DDraw(x + OpenTaiko.Skin.Config_ItemBox_ItemValue_Font_Offset[0], y + OpenTaiko.Skin.Config_ItemBox_ItemValue_Font_Offset[1]);
			}
			//-----------------
			#endregion

			nItem = this.tNextItem(nItem);
		}
		//-----------------
		#endregion

		#region [ 項目リストにフォーカスがあって、かつスクロールが停止しているなら、パネルの上下に▲印を描画する。]
		//-----------------
		if (this.bItemListSideFocus && (this.nTargetScrollCounter == 0)) {
			int x_upper;
			int x_lower;
			int y_upper;
			int y_lower;

			// 位置決定。

			if (this.bElementValueFocus) {
				x_upper = OpenTaiko.Skin.Config_Arrow_Focus_X[0];  // 要素値の上下あたり。
				x_lower = OpenTaiko.Skin.Config_Arrow_Focus_X[1];  // 要素値の上下あたり。
				y_upper = OpenTaiko.Skin.Config_Arrow_Focus_Y[0] - this.ctTriangleArrowAnime.CurrentValue;
				y_lower = OpenTaiko.Skin.Config_Arrow_Focus_Y[1] + this.ctTriangleArrowAnime.CurrentValue;
			} else {
				x_upper = OpenTaiko.Skin.Config_Arrow_X[0];  // 要素値の上下あたり。
				x_lower = OpenTaiko.Skin.Config_Arrow_X[1];  // 要素値の上下あたり。
				y_upper = OpenTaiko.Skin.Config_Arrow_Y[0] - this.ctTriangleArrowAnime.CurrentValue;
				y_lower = OpenTaiko.Skin.Config_Arrow_Y[1] + this.ctTriangleArrowAnime.CurrentValue;
			}

			// 描画。

			if (OpenTaiko.Tx.Config_Arrow != null) {
				OpenTaiko.Tx.Config_Arrow.t2DDraw(x_upper, y_upper, new Rectangle(0, 0, OpenTaiko.Tx.Config_Arrow.szImageSize.Width, OpenTaiko.Tx.Config_Arrow.szImageSize.Height / 2));
				OpenTaiko.Tx.Config_Arrow.t2DDraw(x_lower, y_lower, new Rectangle(0, OpenTaiko.Tx.Config_Arrow.szImageSize.Height / 2, OpenTaiko.Tx.Config_Arrow.szImageSize.Width, OpenTaiko.Tx.Config_Arrow.szImageSize.Height / 2));
			}
		}
		//-----------------
		#endregion
		return 0;
	}


	// その他

	#region [ private ]
	//-----------------
	private enum EMenuType {
		System,
		Drums,
		Theme,
		KeyAssignSystem,        // #24609 2011.4.12 yyagi: 画面キャプチャキーのアサイン
		KeyAssignDrums,
		KeyAssignTraining,
		Unknown

	}

	private bool bItemListSideFocus;
	private bool bElementValueFocus;
	private CCounter ctTriangleArrowAnime;
	private EMenuType eMenuType;
	#region [ Key Config ]

	private CItemBase iKeyAssignSystemReturnToMenu;     // #24609
	private CItemBase iKeyAssignDrumsReturnToMenu;
	private CItemBase iKeyAssignTrainingReturnToMenu;

	#region [System]
	private CItemBase iKeyAssignSystemCapture;          // #24609
	private CItemBase iKeyAssignSystemSongVolIncrease;
	private CItemBase iKeyAssignSystemSongVolDecrease;
	private CItemBase iKeyAssignSystemDisplayHit;
	private CItemBase iKeyAssignSystemDisplayDebug;
	private CItemBase iKeyAssignSystemQuickConfig;
	private CItemBase iKeyAssignSystemSortSongs;
	private CItemBase iKeyAssignSystemToggleAutoP1;
	private CItemBase iKeyAssignSystemToggleAutoP2;
	private CItemBase iKeyAssignSystemToggleTrainingMode;
	private CItemBase iKeyAssignSystemCycleVideoDisplayMode;
	#endregion

	#region [Drum]
	private CItemBase iKeyAssignTaikoLRed;
	private CItemBase iKeyAssignTaikoRRed;
	private CItemBase iKeyAssignTaikoLBlue;
	private CItemBase iKeyAssignTaikoRBlue;

	private CItemBase iKeyAssignTaikoLRed2P;
	private CItemBase iKeyAssignTaikoRRed2P;
	private CItemBase iKeyAssignTaikoLBlue2P;
	private CItemBase iKeyAssignTaikoRBlue2P;

	private CItemBase iKeyAssignTaikoLRed3P;
	private CItemBase iKeyAssignTaikoRRed3P;
	private CItemBase iKeyAssignTaikoLBlue3P;
	private CItemBase iKeyAssignTaikoRBlue3P;

	private CItemBase iKeyAssignTaikoLRed4P;
	private CItemBase iKeyAssignTaikoRRed4P;
	private CItemBase iKeyAssignTaikoLBlue4P;
	private CItemBase iKeyAssignTaikoRBlue4P;

	private CItemBase iKeyAssignTaikoLRed5P;
	private CItemBase iKeyAssignTaikoRRed5P;
	private CItemBase iKeyAssignTaikoLBlue5P;
	private CItemBase iKeyAssignTaikoRBlue5P;

	private CItemBase iKeyAssignKongaClap;
	private CItemBase iKeyAssignKongaClap2P;
	private CItemBase iKeyAssignKongaClap3P;
	private CItemBase iKeyAssignKongaClap4P;
	private CItemBase iKeyAssignKongaClap5P;

	private CItemBase iKeyAssignDecide;
	private CItemBase iKeyAssignCancel;
	private CItemBase iKeyAssignLeftChange;
	private CItemBase iKeyAssignRightChange;
	#endregion

	#region [Training]
	private CItemBase iKeyAssignTrainingIncreaseScrollSpeed;
	private CItemBase iKeyAssignTrainingDecreaseScrollSpeed;
	private CItemBase iKeyAssignTrainingToggleAuto;
	private CItemBase iKeyAssignTrainingBranchNormal;
	private CItemBase iKeyAssignTrainingBranchExpert;
	private CItemBase iKeyAssignTrainingBranchMaster;
	private CItemBase iKeyAssignTrainingPause;
	private CItemBase iKeyAssignTrainingBookmark;
	private CItemBase iKeyAssignTrainingMoveForwardMeasure;
	private CItemBase iKeyAssignTrainingMoveBackMeasure;
	private CItemBase iKeyAssignTrainingSkipForwardMeasure;
	private CItemBase iKeyAssignTrainingSkipBackMeasure;
	private CItemBase iKeyAssignTrainingIncreaseSongSpeed;
	private CItemBase iKeyAssignTrainingDecreaseSongSpeed;
	private CItemBase iKeyAssignTrainingJumpToFirstMeasure;
	private CItemBase iKeyAssignTrainingJumpToLastMeasure;
	#endregion

	#endregion
	private CItemToggle iLogOutputLog;
	private CItemToggle iSystemApplyLoudnessMetadata;
	private CItemInteger iSystemTargetLoudness;
	private CItemToggle iSystemApplySongVol;
	private CItemInteger iSystemMasterLevel;
	private CItemInteger iSystemSoundEffectLevel;
	private CItemInteger iSystemVoiceLevel;
	private CItemInteger iSystemSongPreviewLevel;
	private CItemInteger iSystemSongPlaybackLevel;
	private CItemInteger iSystemKeyboardSoundLevelIncrement;
	private CItemToggle iSystemAVI;
	private CItemList iSystemAVIDisplayMode;
	private CItemToggle iSystemBGA;
	private CItemInteger iSystemBGAlpha;
	private CItemToggle iSystemBGMSound;
	private CItemToggle iSystemDebugInfo;
	private CItemList iSystemGraphicsType;                 // #24820 2013.1.3 yyagi
	private CItemList iSystemWindowMode;                   // Windowed / Fullscreen / Borderless
	private CItemInteger iSystemMinComboDrums;
	private CItemInteger iSystemPreviewImageWait;
	private CItemInteger iSystemPreviewSoundWait;
	private CItemToggle iSystemRandomFromSubBox;
	private CItemBase iSystemReturnToMenu;
	private CItemToggle iSystemVSyncWait;
	private CItemToggle iSystemAutoResultCapture;       // #25399 2011.6.9 yyagi
	private CItemToggle SendDiscordPlayingInformation;
	private CItemInteger iSystemRisky;                  // #23559 2011.7.27 yyagi
	private CItemList iSystemSoundType;                 // #24820 2013.1.3 yyagi

	private CItemList iSystemLanguage;
	private CItemToggle iDanTowerHide;

	private CItemInteger iSystemBassBufferSizeMs;       // #24820 2013.1.15 yyagi
	private CItemInteger iSystemWASAPIBufferSizeMs;     // #24820 2013.1.15 yyagi
	private CItemList iSystemASIODevice;                // #24820 2013.1.17 yyagi

	private int iSystemSoundType_initial;
	private int iSystemBassBufferSizeMs_initial;
	private int iSystemWASAPIBufferSizeMs_initial;
	private int iSystemASIODevice_initial;
	private CItemToggle iSystemSoundTimerType;          // #33689 2014.6.17 yyagi
	private int iSystemSoundTimerType_initial;          // #33689 2014.6.17 yyagi

	private CItemToggle iSystemTimeStretch;             // #23664 2013.2.24 yyagi

	private List<CItemBase> listItemList;
	private long nScrollTimerValue;
	private int nCurrentScrollCounter;
	private int nTargetScrollCounter;

	private CCachedFontRenderer prvFont;
	private struct stMenuItemRight {
		public CTexture txMenuItemRight;
		public int nParam;
		public string strParam;
		public CTexture txParam;
	}
	private stMenuItemRight[] listMenu;

	private CTexture txSkinSample1;             // #28195 2012.5.2 yyagi
	private string[] skinSubFolders;
	private string[] skinNames;
	private string skinSubFolder_org;
	private float fRenderScale_org;   // render-scale at menu entry; a change triggers a skin reload on exit
	private int nSkinSampleIndex;
	private int nSkinIndex;

	private CItemBase iDrumsGoToCalibration;
	private CItemBase iDrumsGoToKeyAssign;
	private CItemBase iDrumsGoToTrainingKeyAssign;
	private CItemBase iSystemGoToKeyAssign;
	private CItemInteger iCommonPlaySpeed;

	private CItemInteger iLayoutType;

	private CItemBase iDrumsReturnToMenu;
	private CItemBase iThemeReturnToMenu;
	// Maps setting-id → CItemBase (CItemToggle / CItemInteger / CItemList / CItemString)
	private Dictionary<string, CItemBase> themeItems = new();
	private CTextInput? themeStringInput;
	private string? activeThemeStringId;
	// Set to true in the draw phase when the string input is committed via Enter (ImGui).
	// Consumed by tEnter押下() on the same frame to prevent immediately reopening the input.
	private bool _stringInputJustCommitted;
	// Value of the CItemString before the text input was opened — restored on Esc cancel.
	private string _stringInputOriginalValue = "";
	private CItemInteger iDrumsScrollSpeed;
	private CItemToggle iDrumsTight;
	private CItemToggle iTaikoAutoPlay;
	private CItemToggle iTaikoAutoPlay2P;
	private CItemToggle iTaikoAutoRoll;
	private CItemToggle iTaikoIgnoreSongUnlockables;
	private CItemInteger iControllerDeadzone;

	private CItemInteger iRollsPerSec;
	private CItemInteger iAILevel;

	private CItemToggle iTaikoBranchGuide;
	private CItemList iTaikoDefaultCourse; //2017.01.30 DD デフォルトでカーソルをあわせる難易度
	private CItemList iTaikoScoreMode;
	private CItemList iTaikoBranchAnime;
	private CItemToggle iTaikoNoInfo;
	private CItemList iTaikoRandom;
	private CItemList iTaikoStealth;
	private CItemList iTaikoGameMode;
	private CItemToggle iTaikoJust;
	private CItemToggle iTaikoJudgeCountDisp;
	private CItemToggle iTaikoBigNotesJudge;
	private CItemToggle iTaikoForceNormalGauge;
	private CItemInteger iTaikoPlayerCount;
	CItemToggle ShowChara;
	CItemToggle ShowDancer;
	CItemToggle ShowRunner;
	CItemToggle ShowMob;
	CItemToggle ShowFooter;
	CItemToggle ShowPuchiChara;
	CItemToggle SimpleMode;
	CItemToggle iShowExExtraAnime;
	CItemToggle ShinuchiMode;
	CItemToggle FastRender;
	CItemToggle ASyncTextureLoad;
	CItemInteger MusicPreTimeMs;
	CItemInteger TokkunSkipCount;
	CItemInteger TokkunMashInterval;

	private CItemInteger iInputAdjustTimeMs;
	public CItemInteger iGlobalOffsetMs;

	private CItemList iSystemSkinSubfolder;             // #28195 2012.5.2 yyagi
	private CItemList iSystemResolution;                // render-scale multiplier (under the skin selector)
	private CItemBase iSystemReloadDTX;                 // #32081 2013.10.21 yyagi
	private CItemBase iSystemHardReloadDTX;
	private CItemBase isSystemImportingScore;
	private CItemToggle iSystemGameEventBroadcasting;
	private CItemInteger iSystemGameEventBroadcastingPort;

	private CCachedFontRenderer pfMenuTitle;
	private CCachedFontRenderer pfBoxText;

	#region DBEUG
	private CItemToggle debugImGui;
	#endregion

	public Thread ScoreIniImportThread { get; private set; }
	public bool ScoreIniImportThreadIsActive {
		get {
			if (ScoreIniImportThread == null) return false;
			return ScoreIniImportThread.IsAlive;
		}
	}

	private int tPrevItem(int nItem) {
		if (--nItem < 0) {
			nItem = this.listItemList.Count - 1;
		}
		return nItem;
	}
	private int tNextItem(int nItem) {
		if (++nItem >= this.listItemList.Count) {
			nItem = 0;
		}
		return nItem;
	}
	private void tConfigIniRecord() {
		switch (this.eMenuType) {
			case EMenuType.System:
				this.tConfigIniRecord_System();
				return;

			case EMenuType.Drums:
				this.tConfigIniRecord_Drums();
				return;

			case EMenuType.Theme:
				this.tConfigIniRecord_Theme();
				return;
		}
	}
	private void tConfigIniRecord_System() {
		OpenTaiko.ConfigIni.nSongSpeed = this.iCommonPlaySpeed.nCurrentValue;

		OpenTaiko.ConfigIni.nGraphicsDeviceType = GraphicsDeviceFromString(AvailableGraphicsDevices[this.iSystemGraphicsType.nCurrentSelectedItemNumber]);
		OpenTaiko.ConfigIni.nWindowMode = this.iSystemWindowMode.nCurrentSelectedItemNumber;
		{
			var res = OpenTaiko.Skin.Resolutions;
			int ri = Math.Clamp(this.iSystemResolution.nCurrentSelectedItemNumber, 0, res.Count - 1);
			OpenTaiko.ConfigIni.fRenderScale = (float)res[ri].val;
		}
		OpenTaiko.ConfigIni.bIncludeSubfoldersOnRandomSelect = this.iSystemRandomFromSubBox.bON;

		OpenTaiko.ConfigIni.bEnableVSync = this.iSystemVSyncWait.bON;
		OpenTaiko.ConfigIni.bEnableAVI = this.iSystemAVI.bON;
		OpenTaiko.ConfigIni.eClipDispType = (EClipDispType)this.iSystemAVIDisplayMode.nCurrentSelectedItemNumber;
		OpenTaiko.ConfigIni.bEnableBGA = this.iSystemBGA.bON;
		OpenTaiko.ConfigIni.nMsWaitPreviewSoundFromSongSelected = this.iSystemPreviewSoundWait.nCurrentValue;
		OpenTaiko.ConfigIni.nMsWaitPreviewImageFromSongSelected = this.iSystemPreviewImageWait.nCurrentValue;
		OpenTaiko.ConfigIni.bDisplayDebugInfo = this.iSystemDebugInfo.bON;
		OpenTaiko.ConfigIni.nBackgroundTransparency = this.iSystemBGAlpha.nCurrentValue;
		OpenTaiko.ConfigIni.bBGMPlayVoiceSound = this.iSystemBGMSound.bON;
		OpenTaiko.ConfigIni.bDanTowerHide = this.iDanTowerHide.bON;

		OpenTaiko.ConfigIni.ApplySongVol = this.iSystemApplySongVol.bON;
		OpenTaiko.ConfigIni.MasterLevel = this.iSystemMasterLevel.nCurrentValue;
		OpenTaiko.ConfigIni.SoundEffectLevel = this.iSystemSoundEffectLevel.nCurrentValue;
		OpenTaiko.ConfigIni.VoiceLevel = this.iSystemVoiceLevel.nCurrentValue;
		OpenTaiko.ConfigIni.SongPreviewLevel = this.iSystemSongPreviewLevel.nCurrentValue;
		OpenTaiko.ConfigIni.SongPlaybackLevel = this.iSystemSongPlaybackLevel.nCurrentValue;
		OpenTaiko.ConfigIni.KeyboardSoundLevelIncrement = this.iSystemKeyboardSoundLevelIncrement.nCurrentValue;
		OpenTaiko.ConfigIni.MusicPreTimeMs = this.MusicPreTimeMs.nCurrentValue;

		OpenTaiko.ConfigIni.bOutputLogs = this.iLogOutputLog.bON;
		OpenTaiko.ConfigIni.bIsAutoResultCapture = this.iSystemAutoResultCapture.bON;                  // #25399 2011.6.9 yyagi
		OpenTaiko.ConfigIni.SendDiscordPlayingInformation = this.SendDiscordPlayingInformation.bON;

		bool bBroadcastingEnabledChanged = OpenTaiko.ConfigIni.bEnableGameEventBroadcasting != this.iSystemGameEventBroadcasting.bON;
		bool nBroadcastingPortChanged = OpenTaiko.ConfigIni.nGameEventBroadcastingPort != this.iSystemGameEventBroadcastingPort.nCurrentValue;

		OpenTaiko.ConfigIni.bEnableGameEventBroadcasting = this.iSystemGameEventBroadcasting.bON;
		OpenTaiko.ConfigIni.nGameEventBroadcastingPort = this.iSystemGameEventBroadcastingPort.nCurrentValue;

		if (bBroadcastingEnabledChanged || nBroadcastingPortChanged) {
			if (OpenTaiko.HttpEventReporter != null) {
				OpenTaiko.HttpEventReporter.StopListening();
			}
			OpenTaiko.HttpEventReporter = new HttpEventReporter("localhost", OpenTaiko.ConfigIni.nGameEventBroadcastingPort);
			if (OpenTaiko.ConfigIni.bEnableGameEventBroadcasting) {
				OpenTaiko.HttpEventReporter.StartListening();
			}
		}

		OpenTaiko.ConfigIni.nRisky = this.iSystemRisky.nCurrentValue;                                      // #23559 2011.7.27 yyagi

		OpenTaiko.ConfigIni.strSystemSkinSubfolderFullName = skinSubFolders[nSkinIndex];               // #28195 2012.5.2 yyagi
		OpenTaiko.Skin.SetCurrentSkinSubfolderFullName(OpenTaiko.ConfigIni.strSystemSkinSubfolderFullName, true);

		OpenTaiko.ConfigIni.nBassBufferSizeMs = this.iSystemBassBufferSizeMs.nCurrentValue;                // #24820 2013.1.15 yyagi
		if (OperatingSystem.IsWindows()) {
			OpenTaiko.ConfigIni.nSoundDeviceType = this.iSystemSoundType.nCurrentSelectedItemNumber;       // #24820 2013.1.3 yyagi
			OpenTaiko.ConfigIni.nWASAPIBufferSizeMs = this.iSystemWASAPIBufferSizeMs.nCurrentValue;                // #24820 2013.1.15 yyagi
			OpenTaiko.ConfigIni.nASIODevice = this.iSystemASIODevice.nCurrentSelectedItemNumber;           // #24820 2013.1.17 yyagi
		}
		OpenTaiko.ConfigIni.bUseOSTimer = this.iSystemSoundTimerType.bON;                              // #33689 2014.6.17 yyagi

		OpenTaiko.ConfigIni.bTimeStretch = this.iSystemTimeStretch.bON;                                    // #23664 2013.2.24 yyagi


		OpenTaiko.ConfigIni.sLang = CLangManager.intToLang(this.iSystemLanguage.nCurrentSelectedItemNumber);
		CLangManager.langAttach(OpenTaiko.ConfigIni.sLang);
		OpenTaiko.ConfigIni.ShowChara = this.ShowChara.bON;
		OpenTaiko.ConfigIni.ShowDancer = this.ShowDancer.bON;
		OpenTaiko.ConfigIni.ShowRunner = this.ShowRunner.bON;
		OpenTaiko.ConfigIni.ShowMob = this.ShowMob.bON;
		OpenTaiko.ConfigIni.ShowFooter = this.ShowFooter.bON;
		OpenTaiko.ConfigIni.ShowPuchiChara = this.ShowPuchiChara.bON;

		OpenTaiko.ConfigIni.nPlayerCount = this.iTaikoPlayerCount.nCurrentValue;
		OpenTaiko.ConfigIni.FastRender = this.FastRender.bON;
		OpenTaiko.ConfigIni.ASyncTextureLoad = this.ASyncTextureLoad.bON;
		OpenTaiko.ConfigIni.SimpleMode = this.SimpleMode.bON;

#if DEBUG
		OpenTaiko.ConfigIni.DEBUG_bShowImgui = this.debugImGui.bON;
#endif
	}
	private void tConfigIniRecord_Drums() {
		OpenTaiko.ConfigIni.nRollsPerSec = this.iRollsPerSec.nCurrentValue;

		OpenTaiko.ConfigIni.nDefaultAILevel = this.iAILevel.nCurrentValue;
		for (int i = 0; i < 2; i++)
			OpenTaiko.NamePlate.tNamePlateRefreshTitles(i);

		OpenTaiko.ConfigIni.bTight = this.iDrumsTight.bON;

		OpenTaiko.ConfigIni.nGlobalOffsetMs = this.iGlobalOffsetMs.nCurrentValue;
		OpenTaiko.ConfigIni.nControllerDeadzone = this.iControllerDeadzone.nCurrentValue;
		OpenTaiko.InputManager.Deadzone = OpenTaiko.ConfigIni.nControllerDeadzone / 100.0f;
		OpenTaiko.ConfigIni.bIgnoreSongUnlockables = this.iTaikoIgnoreSongUnlockables.bON;

		OpenTaiko.ConfigIni.nMinDisplayedCombo = this.iSystemMinComboDrums.nCurrentValue;
		OpenTaiko.ConfigIni.nRisky = this.iSystemRisky.nCurrentValue;                      // #23559 2911.7.27 yyagi
		OpenTaiko.ConfigIni.bBranchGuide = this.iTaikoBranchGuide.bON;
		OpenTaiko.ConfigIni.nDefaultCourse = this.iTaikoDefaultCourse.nCurrentSelectedItemNumber;
		OpenTaiko.ConfigIni.nScoreMode = this.iTaikoScoreMode.nCurrentSelectedItemNumber;
		OpenTaiko.ConfigIni.ShinuchiMode = this.ShinuchiMode.bON;
		OpenTaiko.ConfigIni.nBranchAnime = this.iTaikoBranchAnime.nCurrentSelectedItemNumber;
		OpenTaiko.ConfigIni.bNoInfo = this.iTaikoNoInfo.bON;

		OpenTaiko.ConfigIni.eGameMode = (EGame)this.iTaikoGameMode.nCurrentSelectedItemNumber;
		OpenTaiko.ConfigIni.bJudgeCountDisplay = this.iTaikoJudgeCountDisp.bON;
		OpenTaiko.ConfigIni.ShowExExtraAnime = this.iShowExExtraAnime.bON;
		OpenTaiko.ConfigIni.bJudgeBigNotes = this.iTaikoBigNotesJudge.bON;
		OpenTaiko.ConfigIni.bForceNormalGauge = this.iTaikoForceNormalGauge.bON;

		OpenTaiko.ConfigIni.TokkunSkipMeasures = this.TokkunSkipCount.nCurrentValue;
		OpenTaiko.ConfigIni.TokkunMashInterval = this.TokkunMashInterval.nCurrentValue;
	}
	private void tConfigIniRecord_Theme() {
		var db = OpenTaiko.Databases?.DBThemeSettings;
		if (db == null) return;

		foreach (var def in db.Definitions) {
			if (!themeItems.TryGetValue(def.Id, out var item)) continue;

			string valueStr = item switch {
				CItemToggle t => t.bON ? "1" : "0",
				CItemInteger i when string.Equals(def.Type, "double", StringComparison.OrdinalIgnoreCase)
					=> (i.nCurrentValue / 100.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
				CItemInteger i => i.nCurrentValue.ToString(),
				CItemList l => def.Options[l.nCurrentSelectedItemNumber],
				CItemString s => s.Value,
				_ => def.Default
			};

			if (def.IsSaveScoped) {
				// Save for all currently loaded saves.
				foreach (var sf in OpenTaiko.SaveFileInstances) {
					if (sf?.data != null)
						db.SetSettingForSave(def.Id, sf.data.SaveId, valueStr);
				}
			} else {
				db.SetSetting(def.Id, valueStr);
			}
		}
	}
	//-----------------
	#endregion
}
