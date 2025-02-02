using System.Drawing;
using FDK;
using SkiaSharp;

namespace OpenTaiko;

internal class CActConfigList : CActivity {
	// Properties

	public bool bIsKeyAssignSelected        // #24525 2011.3.15 yyagi
	{
		get {
			Eメニュー種別 e = this.eメニュー種別;
			if (e == Eメニュー種別.KeyAssignDrums || e == Eメニュー種別.KeyAssignSystem || e == Eメニュー種別.KeyAssignTraining) {
				return true;
			} else {
				return false;
			}
		}
	}
	public bool bIsFocusingParameter        // #32059 2013.9.17 yyagi
	{
		get {
			return b要素値にフォーカス中;
		}
	}
	public bool b現在選択されている項目はReturnToMenuである {
		get {
			CItemBase currentItem = this.list項目リスト[this.n現在の選択項目];
			if (currentItem == this.iSystemReturnToMenu || currentItem == this.iDrumsReturnToMenu) {
				return true;
			} else {
				return false;
			}
		}
	}
	public CItemBase ib現在の選択項目 {
		get {
			return this.list項目リスト[this.n現在の選択項目];
		}
	}
	public int n現在の選択項目;

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

	private static string[] AvailableGraphicsDevices { get {
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

	public void t項目リストの設定_System(bool refresh = true) {
		this.tConfigIniへ記録する();
		this.list項目リスト.Clear();

		// #27029 2012.1.5 from: 説明文は最大9行→13行に変更。

		this.iSystemReturnToMenu = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_MENU_RETURN"), CItemBase.EPanelType.Other,
			CLangManager.LangInstance.GetString("SETTINGS_MENU_RETURN_DESC"));
		this.list項目リスト.Add(this.iSystemReturnToMenu);

		this.iSystemReloadDTX = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_RELOADSONG"), CItemBase.EPanelType.Normal,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_RELOADSONG_DESC"));
		this.list項目リスト.Add(this.iSystemReloadDTX);

		this.iSystemHardReloadDTX = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_RELOADSONGCACHE"), CItemBase.EPanelType.Normal,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_RELOADSONGCACHE_DESC"));
		this.list項目リスト.Add(this.iSystemHardReloadDTX);

		this.isSystemImportingScore = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_IMPORTSCOREINI"), CItemBase.EPanelType.Normal,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_IMPORTSCOREINI_DESC"));
		this.list項目リスト.Add(this.isSystemImportingScore);

		this.iSystemLanguage = new CItemList(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_LANGUAGE"), CItemList.EPanelType.Normal, CLangManager.langToInt(OpenTaiko.ConfigIni.sLang),
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_LANGUAGE_DESC"),
			CLangManager.Languages);
		this.list項目リスト.Add(this.iSystemLanguage);

		this.iTaikoPlayerCount = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_PLAYERCOUNT"), 1, 5, OpenTaiko.ConfigIni.nPlayerCount,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_PLAYERCOUNT_DESC"));
		this.list項目リスト.Add(this.iTaikoPlayerCount);

		this.iDanTowerHide = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_HIDEDANTOWER"), OpenTaiko.ConfigIni.bDanTowerHide,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_HIDEDANTOWER_DESC"));
		this.list項目リスト.Add(this.iDanTowerHide);

		this.iCommonPlaySpeed = new CItemInteger(CLangManager.LangInstance.GetString("MOD_SONGSPEED"), 5, 400, OpenTaiko.ConfigIni.nSongSpeed,
			CLangManager.LangInstance.GetString("SETTINGS_MOD_SONGSPEED_DESC"));
		this.list項目リスト.Add(this.iCommonPlaySpeed);

		this.iSystemTimeStretch = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_TIMESTRETCH"), OpenTaiko.ConfigIni.bTimeStretch,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_TIMESTRETCH_DESC"));
		this.list項目リスト.Add(this.iSystemTimeStretch);

		this.iSystemGraphicsType = new CItemList(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_GRAPHICSAPI"), CItemList.EPanelType.Normal, GraphicsDeviceIntFromConfigInt(),
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_GRAPHICSAPI_DESC"),
			AvailableGraphicsDevices);
		this.list項目リスト.Add(this.iSystemGraphicsType);

		this.iSystemFullscreen = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_FULLSCREEN"), OpenTaiko.ConfigIni.bFullScreen,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_FULLSCREEN_DESC"));
		this.list項目リスト.Add(this.iSystemFullscreen);

		this.iSystemRandomFromSubBox = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_RANDOMSUBFOLDER"), OpenTaiko.ConfigIni.bIncludeSubfoldersOnRandomSelect,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_RANDOMSUBFOLDER_DESC"));
		this.list項目リスト.Add(this.iSystemRandomFromSubBox);

		this.iSystemVSyncWait = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_VSYNC"), OpenTaiko.ConfigIni.bEnableVSync,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_VSYNC_DESC"));
		this.list項目リスト.Add(this.iSystemVSyncWait);

		this.iSystemAVI = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BGMOVIE"), OpenTaiko.ConfigIni.bEnableAVI,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BGMOVIE_DESC"));
		this.list項目リスト.Add(this.iSystemAVI);

		this.iSystemAVIDisplayMode = new CItemList(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BGMOVIEDISPLAY"), CItemList.EPanelType.Normal, (int)OpenTaiko.ConfigIni.eClipDispType,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BGMOVIEDISPLAY_DESC"),
			new string[] {
				CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BGMOVIEDISPLAY_NONE"),
				CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BGMOVIEDISPLAY_FULL"),
				CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BGMOVIEDISPLAY_MINI"),
				CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BGMOVIEDISPLAY_BOTH")
			});
		this.list項目リスト.Add(this.iSystemAVIDisplayMode);

		this.iSystemBGA = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BGA"), OpenTaiko.ConfigIni.bEnableBGA,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BGA_DESC"));
		this.list項目リスト.Add(this.iSystemBGA);

		this.iSystemPreviewSoundWait = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SONGPREVIEWBUFFER"), 0, 0x2710, OpenTaiko.ConfigIni.nMsWaitPreviewSoundFromSongSelected,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SONGPREVIEWBUFFER_DESC"));
		this.list項目リスト.Add(this.iSystemPreviewSoundWait);

		this.iSystemPreviewImageWait = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_IMAGEPREVIEWBUFFER"), 0, 0x2710, OpenTaiko.ConfigIni.nMsWaitPreviewImageFromSongSelected,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_IMAGEPREVIEWBUFFER_DESC"));
		this.list項目リスト.Add(this.iSystemPreviewImageWait);

		this.iSystemDebugInfo = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DEBUGMODE"), OpenTaiko.ConfigIni.bDisplayDebugInfo,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DEBUGMODE_DESC"));
		this.list項目リスト.Add(this.iSystemDebugInfo);

		this.iSystemBGAlpha = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_LANEOPACITY"), 0, 0xff, OpenTaiko.ConfigIni.nBackgroundTransparency,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_LANEOPACITY_DESC"));
		this.list項目リスト.Add(this.iSystemBGAlpha);

		this.iSystemBGMSound = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SONGPLAYBACK"), OpenTaiko.ConfigIni.bBGMPlayVoiceSound,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SONGPLAYBACK_DESC"));
		this.list項目リスト.Add(this.iSystemBGMSound);

		this.iSystemApplySongVol = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_USESONGVOL"), OpenTaiko.ConfigIni.ApplySongVol,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_USESONGVOL_DESC"));
		this.list項目リスト.Add(this.iSystemApplySongVol);

		this.iSystemSoundEffectLevel = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SEVOL"), CSound.MinimumGroupLevel, CSound.MaximumGroupLevel, OpenTaiko.ConfigIni.SoundEffectLevel,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SEVOL_DESC"));
		this.list項目リスト.Add(this.iSystemSoundEffectLevel);

		this.iSystemVoiceLevel = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_VOICEVOL"), CSound.MinimumGroupLevel, CSound.MaximumGroupLevel, OpenTaiko.ConfigIni.VoiceLevel,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_VOICEVOL_DESC"));
		this.list項目リスト.Add(this.iSystemVoiceLevel);

		this.iSystemSongPreviewLevel = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SONGPREVIEWVOL"), CSound.MinimumGroupLevel, CSound.MaximumGroupLevel, OpenTaiko.ConfigIni.SongPreviewLevel,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SONGPREVIEWVOL_DESC"));
		this.list項目リスト.Add(this.iSystemSongPreviewLevel);

		this.iSystemSongPlaybackLevel = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SONGVOL"), CSound.MinimumGroupLevel, CSound.MaximumGroupLevel, OpenTaiko.ConfigIni.SongPlaybackLevel,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SONGVOL_DESC"));
		this.list項目リスト.Add(this.iSystemSongPlaybackLevel);

		this.iSystemKeyboardSoundLevelIncrement = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_VOLINCREMENT"), 1, 20, OpenTaiko.ConfigIni.KeyboardSoundLevelIncrement,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_VOLINCREMENT_DESC"));
		this.list項目リスト.Add(this.iSystemKeyboardSoundLevelIncrement);

		this.MusicPreTimeMs = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SONGPLAYBACKBUFFER"), 0, 10000, OpenTaiko.ConfigIni.MusicPreTimeMs,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SONGPLAYBACKBUFFER_DESC"));
		this.list項目リスト.Add(this.MusicPreTimeMs);

		this.iSystemAutoResultCapture = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_AUTOSCREENSHOT"), OpenTaiko.ConfigIni.bIsAutoResultCapture,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_AUTOSCREENSHOT_DESC"));
		this.list項目リスト.Add(this.iSystemAutoResultCapture);

		SendDiscordPlayingInformation = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISCORDRPC"),
			OpenTaiko.ConfigIni.SendDiscordPlayingInformation,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISCORDRPC_DESC"));
		list項目リスト.Add(SendDiscordPlayingInformation);

		this.iSystemBufferedInput = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BUFFEREDINPUT"), OpenTaiko.ConfigIni.bBufferedInputs,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BUFFEREDINPUT_DESC"));
		this.list項目リスト.Add(this.iSystemBufferedInput);
		this.iLogOutputLog = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_LOG"), OpenTaiko.ConfigIni.bOutputLogs,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_LOG_DESC"));
		this.list項目リスト.Add(this.iLogOutputLog);

		// #24820 2013.1.3 yyagi

		// Hide this option for non-Windows users since all other sound device options are Windows-exclusive.
		if (OperatingSystem.IsWindows()) {
			this.iSystemSoundType = new CItemList(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_AUDIOPLAYBACK"), CItemList.EPanelType.Normal, OpenTaiko.ConfigIni.nSoundDeviceType,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_AUDIOPLAYBACK_DESC"),
			new string[] { "Bass", "ASIO", "WASAPI Exclusive", "WASAPI Shared" });
			this.list項目リスト.Add(this.iSystemSoundType);
		}

		// #24820 2013.1.15 yyagi
		this.iSystemBassBufferSizeMs = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BASSBUFFER"), 0, 99999, OpenTaiko.ConfigIni.nBassBufferSizeMs,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_BASSBUFFER_DESC"));
		this.list項目リスト.Add(this.iSystemBassBufferSizeMs);

		if (OperatingSystem.IsWindows()) {
			// #24820 2013.1.15 yyagi
			this.iSystemWASAPIBufferSizeMs = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_WASAPIBUFFER"), 0, 99999, OpenTaiko.ConfigIni.nWASAPIBufferSizeMs,
				CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_WASAPIBUFFER_DESC"));
			this.list項目リスト.Add(this.iSystemWASAPIBufferSizeMs);

			// #24820 2013.1.17 yyagi
			this.iSystemASIODevice = new CItemList(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_ASIOPLAYBACK"), CItemList.EPanelType.Normal, OpenTaiko.ConfigIni.nASIODevice,
				CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_ASIOPLAYBACK_DESC"),
				CEnumerateAllAsioDevices.GetAllASIODevices());
			this.list項目リスト.Add(this.iSystemASIODevice);
		}

		// #33689 2014.6.17 yyagi
		this.iSystemSoundTimerType = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_OSTIMER"), OpenTaiko.ConfigIni.bUseOSTimer,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_OSTIMER_DESC"));
		this.list項目リスト.Add(this.iSystemSoundTimerType);

		ShowChara = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYCHARA"), OpenTaiko.ConfigIni.ShowChara,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYCHARA_DESC"));
		this.list項目リスト.Add(ShowChara);

		ShowDancer = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYDANCER"), OpenTaiko.ConfigIni.ShowDancer,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYDANCER_DESC"));
		this.list項目リスト.Add(ShowDancer);

		ShowMob = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYMOB"), OpenTaiko.ConfigIni.ShowMob,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYMOB_DESC"));
		this.list項目リスト.Add(ShowMob);

		ShowRunner = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYRUNNER"), OpenTaiko.ConfigIni.ShowRunner,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYRUNNER_DESC"));
		this.list項目リスト.Add(ShowRunner);

		ShowFooter = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYFOOTER"), OpenTaiko.ConfigIni.ShowFooter,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYFOOTER_DESC"));
		this.list項目リスト.Add(ShowFooter);

		FastRender = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_FASTRENDER"), OpenTaiko.ConfigIni.FastRender,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_FASTRENDER_DESC"));
		this.list項目リスト.Add(FastRender);

		ShowPuchiChara = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYPUCHI"), OpenTaiko.ConfigIni.ShowPuchiChara,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DISPLAYPUCHI_DESC"));
		this.list項目リスト.Add(ShowPuchiChara);

		SimpleMode = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SIMPLEMODE"), OpenTaiko.ConfigIni.SimpleMode,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SIMPLEMODE_DESC"));
		this.list項目リスト.Add(SimpleMode);

		ASyncTextureLoad = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_TEXTUREASYNC"), OpenTaiko.ConfigIni.ASyncTextureLoad,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_TEXTUREASYNC_DESC"));
		this.list項目リスト.Add(ASyncTextureLoad);

		this.iSystemSkinSubfolder = new CItemList(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SKIN"), CItemBase.EPanelType.Normal, nSkinIndex,
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_SKIN_DESC"),
			skinNames);
		this.list項目リスト.Add(this.iSystemSkinSubfolder);

		this.iSystemGoToKeyAssign = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM"), CItemBase.EPanelType.Normal,
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_DESC"));
		this.list項目リスト.Add(this.iSystemGoToKeyAssign);

#if DEBUG
		this.debugImGui = new CItemToggle("[DEBUG ONLY] Show ImGui Debug Window", OpenTaiko.ConfigIni.DEBUG_bShowImgui);
		this.list項目リスト.Add(this.debugImGui);
#endif

		OnListMenuの初期化();
		if (refresh) {
			this.n現在の選択項目 = 0;
			this.eメニュー種別 = Eメニュー種別.System;
		}

	}
	#endregion


	// Gameplay options
	#region [ t項目リストの設定_Drums() ]

	public void t項目リストの設定_Drums() {
		this.tConfigIniへ記録する();
		this.list項目リスト.Clear();

		// #27029 2012.1.5 from: 説明文は最大9行→13行に変更。

		this.iDrumsReturnToMenu = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_MENU_RETURN"), CItemBase.EPanelType.Other,
			CLangManager.LangInstance.GetString("SETTINGS_MENU_RETURN_DESC"));
		this.list項目リスト.Add(this.iDrumsReturnToMenu);

		this.iDrumsGoToCalibration = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_GAME_CALIBRATION"), CItemBase.EPanelType.Other,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_CALIBRATION_DESC"));
		this.list項目リスト.Add(this.iDrumsGoToCalibration);

		this.iRollsPerSec = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_GAME_AUTOROLL"), 0, 1000, OpenTaiko.ConfigIni.nRollsPerSec,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_AUTOROLL_DESC"));
		this.list項目リスト.Add(this.iRollsPerSec);

		this.iAILevel = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_GAME_AILEVEL"), 1, 10, OpenTaiko.ConfigIni.nDefaultAILevel,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_AILEVEL_DESC"));
		this.list項目リスト.Add(this.iAILevel);

		this.iSystemRisky = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_GAME_BADCOUNT"), 0, 10, OpenTaiko.ConfigIni.nRisky,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_BADCOUNT_DESC"));
		this.list項目リスト.Add(this.iSystemRisky);

		this.iTaikoNoInfo = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_GAME_NOINFO"), OpenTaiko.ConfigIni.bNoInfo,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_NOINFO_DESC"));
		this.list項目リスト.Add(this.iTaikoNoInfo);

		this.iDrumsTight = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_GAME_NOTELOCK"), OpenTaiko.ConfigIni.bTight,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_NOTELOCK_DESC"));
		this.list項目リスト.Add(this.iDrumsTight);

		this.iSystemMinComboDrums = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_GAME_COMBODISPLAY"), 1, 0x1869f, OpenTaiko.ConfigIni.nMinDisplayedCombo.Drums,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_COMBODISPLAY_DESC"));
		this.list項目リスト.Add(this.iSystemMinComboDrums);

		this.iGlobalOffsetMs = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_GAME_GLOBALOFFSET"), -9999, 9999, OpenTaiko.ConfigIni.nGlobalOffsetMs,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_GLOBALOFFSET_DESC"));
		this.list項目リスト.Add(this.iGlobalOffsetMs);

		this.iTaikoDefaultCourse = new CItemList(CLangManager.LangInstance.GetString("SETTINGS_GAME_DEFAULTDIFF"), CItemBase.EPanelType.Normal, OpenTaiko.ConfigIni.nDefaultCourse,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_DEFAULTDIFF_DESC"),
			new string[] {
				CLangManager.LangInstance.GetString("DIFF_EASY"),
				CLangManager.LangInstance.GetString("DIFF_NORMAL"),
				CLangManager.LangInstance.GetString("DIFF_HARD"),
				CLangManager.LangInstance.GetString("DIFF_EX"),
				CLangManager.LangInstance.GetString("DIFF_EXTRA"),
				CLangManager.LangInstance.GetString("DIFF_EXEXTRA") });
		this.list項目リスト.Add(this.iTaikoDefaultCourse);

		this.iTaikoScoreMode = new CItemList(CLangManager.LangInstance.GetString("SETTINGS_GAME_SCOREMODE"), CItemBase.EPanelType.Normal, OpenTaiko.ConfigIni.nScoreMode,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_SCOREMODE_DESC"),
			new string[] { "TYPE-A", "TYPE-B", "TYPE-C" });
		this.list項目リスト.Add(this.iTaikoScoreMode);

		this.ShinuchiMode = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_GAME_SHINUCHI"), OpenTaiko.ConfigIni.ShinuchiMode, CItemBase.EPanelType.Normal,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_SHINUCHI_DESC"));
		this.list項目リスト.Add(this.ShinuchiMode);

		// FIXME: This does nothing vvv
		this.iTaikoBranchGuide = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_GAME_BRANCHGUIDE"), OpenTaiko.ConfigIni.bBranchGuide,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_BRANCHGUIDE_DESC"));
		this.list項目リスト.Add(this.iTaikoBranchGuide);

		this.iTaikoBranchAnime = new CItemList(CLangManager.LangInstance.GetString("SETTINGS_GAME_BRANCHANIME"), CItemBase.EPanelType.Normal, OpenTaiko.ConfigIni.nBranchAnime,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_BRANCHANIME_DESC"),
			new string[] { "TYPE-A", "TYPE-B" });
		this.list項目リスト.Add(this.iTaikoBranchAnime);

		this.iTaikoGameMode = new CItemList(CLangManager.LangInstance.GetString("SETTINGS_GAME_SURVIVAL"), CItemBase.EPanelType.Normal, (int)OpenTaiko.ConfigIni.eGameMode,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_SURVIVAL_DESC"),
			new string[] { "OFF", "TYPE-A", "TYPE-B" });
		this.list項目リスト.Add(this.iTaikoGameMode);

		this.iTaikoBigNotesJudge = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_GAME_BIGNOTEJUDGE"), OpenTaiko.ConfigIni.bJudgeBigNotes,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_BIGNOTEJUDGE_DESC"));
		this.list項目リスト.Add(this.iTaikoBigNotesJudge);

		this.iTaikoForceNormalGauge = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_GAME_NORMALGAUGE"), OpenTaiko.ConfigIni.bForceNormalGauge,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_NORMALGAUGE_DESC"));
		this.list項目リスト.Add(this.iTaikoForceNormalGauge);

		this.iTaikoJudgeCountDisp = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_GAME_SCOREDISPLAY"), OpenTaiko.ConfigIni.bJudgeCountDisplay,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_SCOREDISPLAY_DESC"));
		this.list項目リスト.Add(this.iTaikoJudgeCountDisp);

		this.iShowExExtraAnime = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_GAME_EXEXTRAANIME"), OpenTaiko.ConfigIni.ShowExExtraAnime,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_EXEXTRAANIME_DESC"));
		this.list項目リスト.Add(this.iShowExExtraAnime);

		this.TokkunSkipCount = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_TRAINING_SKIPCOUNT"), 1, 99, OpenTaiko.ConfigIni.TokkunSkipMeasures,
			CLangManager.LangInstance.GetString("SETTINGS_TRAINING_SKIPCOUNT_DESC"));
		this.list項目リスト.Add(TokkunSkipCount);

		this.TokkunMashInterval = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_TRAINING_JUMPINTERVAL"), 1, 9999, OpenTaiko.ConfigIni.TokkunMashInterval,
			CLangManager.LangInstance.GetString("SETTINGS_TRAINING_JUMPINTERVAL_DESC"));
		this.list項目リスト.Add(TokkunMashInterval);

		this.iTaikoIgnoreSongUnlockables = new CItemToggle(CLangManager.LangInstance.GetString("SETTINGS_GAME_IGNORESONGUNLOCKABLES"), OpenTaiko.ConfigIni.bIgnoreSongUnlockables,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_IGNORESONGUNLOCKABLES_DESC"));
		this.list項目リスト.Add(this.iTaikoIgnoreSongUnlockables);

		this.iControllerDeadzone = new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_GAME_CONTROLLERDEADZONE"), 10, 90, OpenTaiko.ConfigIni.nControllerDeadzone,
			CLangManager.LangInstance.GetString("SETTINGS_GAME_CONTROLLERDEADZONE_DESC"));
		this.list項目リスト.Add(this.iControllerDeadzone);

		this.iDrumsGoToKeyAssign = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME"), CItemBase.EPanelType.Normal,
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_DESC"));
		this.list項目リスト.Add(this.iDrumsGoToKeyAssign);

		this.iDrumsGoToTrainingKeyAssign = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING"), CItemBase.EPanelType.Normal,
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_DESC"));
		this.list項目リスト.Add(this.iDrumsGoToTrainingKeyAssign);

		OnListMenuの初期化();
		this.n現在の選択項目 = 0;
		this.eメニュー種別 = Eメニュー種別.Drums;
	}

	#endregion


	/// <summary>
	/// ESC押下時の右メニュー描画
	/// </summary>
	public void tEsc押下() {
		if (this.b要素値にフォーカス中)       // #32059 2013.9.17 add yyagi
		{
			this.b要素値にフォーカス中 = false;
		}

		switch (eメニュー種別) {
			case Eメニュー種別.KeyAssignSystem:
				t項目リストの設定_System();
				break;
			case Eメニュー種別.KeyAssignDrums:
			case Eメニュー種別.KeyAssignTraining:
				t項目リストの設定_Drums();
				break;
		}

		// これ以外なら何もしない
	}
	public void tEnter押下() {
		OpenTaiko.Skin.soundDecideSFX.tPlay();
		if (this.b要素値にフォーカス中) {
			this.b要素値にフォーカス中 = false;
		} else if (this.list項目リスト[this.n現在の選択項目].e種別 == CItemBase.E種別.整数) {
			this.b要素値にフォーカス中 = true;
		}
		#region [ 個々のキーアサイン ]
		//太鼓のキー設定。
		else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoLRed) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.LRed);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoRRed) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.RRed);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoLBlue) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.LBlue);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoRBlue) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.RBlue);
		}

		//太鼓のキー設定。2P
		else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoLRed2P) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.LRed2P);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoRRed2P) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.RRed2P);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoLBlue2P) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.LBlue2P);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoRBlue2P) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.RBlue2P);
		}

		//太鼓のキー設定。3P
		else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoLRed3P) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.LRed3P);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoRRed3P) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.RRed3P);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoLBlue3P) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.LBlue3P);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoRBlue3P) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.RBlue3P);
		}

		//太鼓のキー設定。4P
		else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoLRed4P) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.LRed4P);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoRRed4P) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.RRed4P);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoLBlue4P) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.LBlue4P);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoRBlue4P) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.RBlue4P);
		}

		//太鼓のキー設定。5P
		else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoLRed5P) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.LRed5P);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoRRed5P) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.RRed5P);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoLBlue5P) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.LBlue5P);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoRBlue5P) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.RBlue5P);
		}

		// Konga claps
		else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignKongaClap) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.Clap);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignKongaClap2P) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.Clap2P);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignKongaClap3P) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.Clap3P);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignKongaClap4P) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.Clap4P);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignKongaClap5P) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.Clap5P);
		}

		// Menu controls
		else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignDecide) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.Decide);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignCancel) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.Cancel);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignLeftChange) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.LeftChange);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignRightChange) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.RightChange);
		}

		// System controls
		else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignSystemCapture) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.System, EKeyConfigPad.Capture);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignSystemSongVolIncrease) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.System, EKeyConfigPad.SongVolumeIncrease);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignSystemSongVolDecrease) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.System, EKeyConfigPad.SongVolumeDecrease);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignSystemDisplayHit) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.System, EKeyConfigPad.DisplayHits);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignSystemDisplayDebug) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.System, EKeyConfigPad.DisplayDebug);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignSystemQuickConfig) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.System, EKeyConfigPad.QuickConfig);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignSystemNewHeya) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.System, EKeyConfigPad.NewHeya);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignSystemSortSongs) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.System, EKeyConfigPad.SortSongs);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignSystemToggleAutoP1) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.System, EKeyConfigPad.ToggleAutoP1);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignSystemToggleAutoP2) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.System, EKeyConfigPad.ToggleAutoP2);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignSystemToggleTrainingMode) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.System, EKeyConfigPad.ToggleTrainingMode);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignSystemCycleVideoDisplayMode) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.System, EKeyConfigPad.CycleVideoDisplayMode);
		}

		// Training controls
		else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTrainingPause) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.TrainingPause);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTrainingToggleAuto) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.TrainingToggleAuto);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTrainingBookmark) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.TrainingBookmark);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTrainingIncreaseScrollSpeed) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.TrainingIncreaseScrollSpeed);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTrainingDecreaseScrollSpeed) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.TrainingDecreaseScrollSpeed);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTrainingIncreaseSongSpeed) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.TrainingIncreaseSongSpeed);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTrainingDecreaseSongSpeed) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.TrainingDecreaseSongSpeed);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTrainingBranchNormal) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.TrainingBranchNormal);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTrainingBranchExpert) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.TrainingBranchExpert);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTrainingBranchMaster) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.TrainingBranchMaster);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTrainingMoveForwardMeasure) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.TrainingMoveForwardMeasure);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTrainingMoveBackMeasure) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.TrainingMoveBackMeasure);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTrainingSkipForwardMeasure) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.TrainingSkipForwardMeasure);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTrainingSkipBackMeasure) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.TrainingSkipBackMeasure);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTrainingJumpToFirstMeasure) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.TrainingJumpToFirstMeasure);
		} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTrainingJumpToLastMeasure) {
			OpenTaiko.stageConfig.tパッド選択通知(EKeyConfigPart.Drums, EKeyConfigPad.TrainingJumpToLastMeasure);
		}
		#endregion
		else {
			// #27029 2012.1.5 from
			this.list項目リスト[this.n現在の選択項目].tEnter押下();

			if (this.list項目リスト[this.n現在の選択項目] == this.iSystemLanguage) {
				OpenTaiko.ConfigIni.sLang = CLangManager.intToLang(this.iSystemLanguage.n現在選択されている項目番号);
				CLangManager.langAttach(OpenTaiko.ConfigIni.sLang);

				prvFont?.Dispose();
				OpenTaiko.stageConfig.ftフォント?.Dispose();
				OpenTaiko.stageTitle.pfMenuTitle?.Dispose();
				OpenTaiko.stageTitle.pfBoxText?.Dispose();

				prvFont = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.Config_Font_Scale);
				OpenTaiko.stageConfig.ftフォント = HPrivateFastFont.tInstantiateMainFont((int)OpenTaiko.Skin.Config_Font_Scale_Description, CFontRenderer.FontStyle.Bold);
				OpenTaiko.stageTitle.pfMenuTitle = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.Title_ModeSelect_Title_Scale[0]);
				OpenTaiko.stageTitle.pfBoxText = HPrivateFastFont.tInstantiateBoxFont(OpenTaiko.Skin.Title_ModeSelect_Title_Scale[1]);

				t項目リストの設定_System(refresh: false);
				OpenTaiko.stageConfig.ReloadMenus();
			}
			// Enter押下後の後処理

			if (this.list項目リスト[this.n現在の選択項目] == this.iSystemFullscreen) {
				OpenTaiko.app.bSwitchFullScreenAtNextFrame = true;
			} else if (this.list項目リスト[this.n現在の選択項目] == this.iSystemVSyncWait) {
				OpenTaiko.ConfigIni.bEnableVSync = this.iSystemVSyncWait.bON;
				OpenTaiko.app.bSwitchVSyncAtTheNextFrame = true;
			}
			#region [ キーアサインへの遷移と脱出 ]
			else if (this.list項目リスト[this.n現在の選択項目] == this.iSystemGoToKeyAssign)          // #24609 2011.4.12 yyagi
			{
				t項目リストの設定_KeyAssignSystem();
			} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignSystemReturnToMenu)    // #24609 2011.4.12 yyagi
			{
				tConfigIniへ記録する();
				t項目リストの設定_System();
			} else if (this.list項目リスト[this.n現在の選択項目] == this.iDrumsGoToKeyAssign)               // #24525 2011.3.15 yyagi
			{
				tConfigIniへ記録する();
				t項目リストの設定_KeyAssignDrums();
			} else if (this.list項目リスト[this.n現在の選択項目] == this.iDrumsGoToTrainingKeyAssign) {
				tConfigIniへ記録する();
				t項目リストの設定_KeyAssignTraining();
			} else if (this.list項目リスト[this.n現在の選択項目] == this.iDrumsGoToCalibration) {
				OpenTaiko.stageConfig.actCalibrationMode.Start();
			} else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignDrumsReturnToMenu ||
					   this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTrainingReturnToMenu)     // #24525 2011.3.15 yyagi
			{
				t項目リストの設定_Drums();
			}
			#endregion
			#region [ スキン項目でEnterを押下した場合に限り、スキンの縮小サンプルを生成する。]
			else if (this.list項目リスト[this.n現在の選択項目] == this.iSystemSkinSubfolder)          // #28195 2012.5.2 yyagi
			{
				tGenerateSkinSample();
			}
			#endregion
			#region [ 曲データ一覧の再読み込み ]
			else if (this.list項目リスト[this.n現在の選択項目] == this.iSystemReloadDTX)              // #32081 2013.10.21 yyagi
			{
				if (OpenTaiko.EnumSongs.IsEnumerating) {
					// Debug.WriteLine( "バックグラウンドでEnumeratingSongs中だったので、一旦中断します。" );
					OpenTaiko.EnumSongs.Abort();
					OpenTaiko.actEnumSongs.DeActivate();
				}

				OpenTaiko.EnumSongs.StartEnumFromDisk();
				OpenTaiko.EnumSongs.ChangeEnumeratePriority(ThreadPriority.Normal);
				OpenTaiko.actEnumSongs.bコマンドでの曲データ取得 = true;
				OpenTaiko.actEnumSongs.Activate();
				OpenTaiko.stageSongSelect.actSongList.ResetSongIndex();
			} else if (this.list項目リスト[this.n現在の選択項目] == this.iSystemHardReloadDTX)              // #32081 2013.10.21 yyagi
			{
				if (OpenTaiko.EnumSongs.IsEnumerating) {
					OpenTaiko.EnumSongs.Abort();
					OpenTaiko.actEnumSongs.DeActivate();
				}

				OpenTaiko.EnumSongs.StartEnumFromDisk(true);
				OpenTaiko.EnumSongs.ChangeEnumeratePriority(ThreadPriority.Normal);
				OpenTaiko.actEnumSongs.bコマンドでの曲データ取得 = true;
				OpenTaiko.actEnumSongs.Activate();
				OpenTaiko.stageSongSelect.actSongList.ResetSongIndex();
			} else if (this.list項目リスト[this.n現在の選択項目] == this.isSystemImportingScore) {
				// Running in a separate thread so the game doesn't freeze
				ScoreIniImportThread = new Thread(CScoreIni_Importer.ImportScoreInisToSavesDb3);
				ScoreIniImportThread.Start();
			}
			#endregion
		}
	}

	private void tGenerateSkinSample() {

		nSkinIndex = ((CItemList)this.list項目リスト[this.n現在の選択項目]).n現在選択されている項目番号;
		if (nSkinSampleIndex != nSkinIndex) {
			string path = skinSubFolders[nSkinIndex];
			path = System.IO.Path.Combine(path, @$"Graphics{Path.DirectorySeparatorChar}1_Title{Path.DirectorySeparatorChar}Background.png");
			SKBitmap bmSrc = SKBitmap.Decode(path);

			int _w = OpenTaiko.Skin.Resolution[0] / 4;
			int _h = OpenTaiko.Skin.Resolution[1] / 4;

			if (txSkinSample1 != null) {
				OpenTaiko.tDisposeSafely(ref txSkinSample1);
			}
			txSkinSample1 = OpenTaiko.tテクスチャの生成(bmSrc, false);

			txSkinSample1.vcScaleRatio = new Silk.NET.Maths.Vector3D<float>(_w / (float)txSkinSample1.szTextureSize.Width, _h / (float)txSkinSample1.szTextureSize.Height, 0);

			bmSrc.Dispose();
			nSkinSampleIndex = nSkinIndex;
		}
	}

	#region [ 項目リストの設定 ( Exit, KeyAssignSystem/Drums) ]
	public void t項目リストの設定_Exit() {
		this.tConfigIniへ記録する();
		this.eメニュー種別 = Eメニュー種別.Unknown;
	}
	public void t項目リストの設定_KeyAssignSystem() {
		this.list項目リスト.Clear();
		// #27029 2012.1.5 from: 説明文は最大9行→13行に変更。

		this.iKeyAssignSystemReturnToMenu = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_MENU_RETURN"), CItemBase.EPanelType.Other,
			CLangManager.LangInstance.GetString("SETTINGS_MENU_RETURN_DESC"));
		this.list項目リスト.Add(this.iKeyAssignSystemReturnToMenu);

		this.iKeyAssignSystemCapture = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_CAPTURE"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_CAPTURE_DESC"));
		this.list項目リスト.Add(this.iKeyAssignSystemCapture);
		this.iKeyAssignSystemSongVolIncrease = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_INCREASEVOL"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_INCREASEVOL_DESC"));
		this.list項目リスト.Add(this.iKeyAssignSystemSongVolIncrease);
		this.iKeyAssignSystemSongVolDecrease = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_DECREASEVOL"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_DECREASEVOL_DESC"));
		this.list項目リスト.Add(this.iKeyAssignSystemSongVolDecrease);
		this.iKeyAssignSystemDisplayHit = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_DISPLAYHITS"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_DISPLAYHITS_DESC"));
		this.list項目リスト.Add(this.iKeyAssignSystemDisplayHit);
		this.iKeyAssignSystemDisplayDebug = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_DISPLAYDEBUG"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_DISPLAYDEBUG_DESC"));
		this.list項目リスト.Add(this.iKeyAssignSystemDisplayDebug);
		this.iKeyAssignSystemQuickConfig = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_QUICKCONFIG"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_QUICKCONFIG_DESC"));
		this.list項目リスト.Add(this.iKeyAssignSystemQuickConfig);
		this.iKeyAssignSystemNewHeya = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_QUICKHEYA"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_QUICKHEYA_DESC"));
		this.list項目リスト.Add(this.iKeyAssignSystemNewHeya);
		this.iKeyAssignSystemSortSongs = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_SONGSORT"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_SONGSORT_DESC"));
		this.list項目リスト.Add(this.iKeyAssignSystemSortSongs);
		this.iKeyAssignSystemToggleAutoP1 = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_AUTO1P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_AUTO1P_DESC"));
		this.list項目リスト.Add(this.iKeyAssignSystemToggleAutoP1);
		this.iKeyAssignSystemToggleAutoP2 = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_AUTO2P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_AUTO2P_DESC"));
		this.list項目リスト.Add(this.iKeyAssignSystemToggleAutoP2);
		this.iKeyAssignSystemToggleTrainingMode = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_TRAINING"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_TRAINING_DESC"));
		this.list項目リスト.Add(this.iKeyAssignSystemToggleTrainingMode);
		this.iKeyAssignSystemCycleVideoDisplayMode = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_BGMOVIEDISPLAY"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_SYSTEM_BGMOVIEDISPLAY_DESC"));
		this.list項目リスト.Add(this.iKeyAssignSystemCycleVideoDisplayMode);

		OnListMenuの初期化();
		this.n現在の選択項目 = 0;
		this.eメニュー種別 = Eメニュー種別.KeyAssignSystem;
	}
	public void t項目リストの設定_KeyAssignDrums() {
		this.list項目リスト.Clear();
		// #27029 2012.1.5 from: 説明文は最大9行→13行に変更。

		this.iKeyAssignDrumsReturnToMenu = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_MENU_RETURN"), CItemBase.EPanelType.Other,
			CLangManager.LangInstance.GetString("SETTINGS_MENU_RETURN_DESC"));
		this.list項目リスト.Add(this.iKeyAssignDrumsReturnToMenu);

		this.iKeyAssignTaikoLRed = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTRED"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTRED_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTaikoLRed);
		this.iKeyAssignTaikoRRed = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTRED"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTRED_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTaikoRRed);
		this.iKeyAssignTaikoLBlue = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTBLUE"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTBLUE_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTaikoLBlue);
		this.iKeyAssignTaikoRBlue = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTBLUE"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTBLUE_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTaikoRBlue);
		this.iKeyAssignKongaClap = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CLAP"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CLAP_DESC"));
		this.list項目リスト.Add(this.iKeyAssignKongaClap);

		this.iKeyAssignTaikoLRed2P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTRED2P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTRED2P_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTaikoLRed2P);
		this.iKeyAssignTaikoRRed2P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTRED2P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTRED2P_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTaikoRRed2P);
		this.iKeyAssignTaikoLBlue2P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTBLUE2P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTBLUE2P_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTaikoLBlue2P);
		this.iKeyAssignTaikoRBlue2P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTBLUE2P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTBLUE2P_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTaikoRBlue2P);
		this.iKeyAssignKongaClap2P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CLAP2P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CLAP2P_DESC"));
		this.list項目リスト.Add(this.iKeyAssignKongaClap2P);

		this.iKeyAssignTaikoLRed3P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTRED3P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTRED3P_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTaikoLRed3P);
		this.iKeyAssignTaikoRRed3P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTRED3P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTRED3P_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTaikoRRed3P);
		this.iKeyAssignTaikoLBlue3P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTBLUE3P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTBLUE3P_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTaikoLBlue3P);
		this.iKeyAssignTaikoRBlue3P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTBLUE3P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTBLUE3P_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTaikoRBlue3P);
		this.iKeyAssignKongaClap3P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CLAP3P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CLAP3P_DESC"));
		this.list項目リスト.Add(this.iKeyAssignKongaClap3P);

		this.iKeyAssignTaikoLRed4P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTRED4P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTRED4P_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTaikoLRed4P);
		this.iKeyAssignTaikoRRed4P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTRED4P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTRED4P_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTaikoRRed4P);
		this.iKeyAssignTaikoLBlue4P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTBLUE4P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTBLUE4P_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTaikoLBlue4P);
		this.iKeyAssignTaikoRBlue4P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTBLUE4P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTBLUE4P_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTaikoRBlue4P);
		this.iKeyAssignKongaClap4P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CLAP4P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CLAP4P_DESC"));
		this.list項目リスト.Add(this.iKeyAssignKongaClap4P);

		this.iKeyAssignTaikoLRed5P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTRED5P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTRED5P_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTaikoLRed5P);
		this.iKeyAssignTaikoRRed5P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTRED5P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTRED5P_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTaikoRRed5P);
		this.iKeyAssignTaikoLBlue5P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTBLUE5P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTBLUE5P_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTaikoLBlue5P);
		this.iKeyAssignTaikoRBlue5P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTBLUE5P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTBLUE5P_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTaikoRBlue5P);
		this.iKeyAssignKongaClap5P = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CLAP5P"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CLAP5P_DESC"));
		this.list項目リスト.Add(this.iKeyAssignKongaClap5P);

		this.iKeyAssignDecide = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_DECIDE"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_DECIDE_DESC"));
		this.list項目リスト.Add(this.iKeyAssignDecide);
		this.iKeyAssignCancel = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CANCEL"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_CANCEL_DESC"));
		this.list項目リスト.Add(this.iKeyAssignCancel);

		this.iKeyAssignLeftChange = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTCHANGE"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_LEFTCHANGE_DESC"));
		this.list項目リスト.Add(this.iKeyAssignLeftChange);
		this.iKeyAssignRightChange = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTCHANGE"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_GAME_RIGHTCHANGE_DESC"));
		this.list項目リスト.Add(this.iKeyAssignRightChange);

		OnListMenuの初期化();
		this.n現在の選択項目 = 0;
		this.eメニュー種別 = Eメニュー種別.KeyAssignDrums;
	}
	public void t項目リストの設定_KeyAssignTraining() {
		this.list項目リスト.Clear();

		this.iKeyAssignTrainingReturnToMenu = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_MENU_RETURN"), CItemBase.EPanelType.Other,
			CLangManager.LangInstance.GetString("SETTINGS_MENU_RETURN_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTrainingReturnToMenu);


		this.iKeyAssignTrainingPause = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_PAUSE"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_PAUSE_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTrainingPause);

		this.iKeyAssignTrainingToggleAuto = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_AUTO"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_AUTO_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTrainingToggleAuto);

		this.iKeyAssignTrainingBookmark = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_BOOKMARK"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_BOOKMARK_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTrainingBookmark);

		this.iKeyAssignTrainingIncreaseScrollSpeed = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_INCREASESCROLL"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_INCREASESCROLL_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTrainingIncreaseScrollSpeed);

		this.iKeyAssignTrainingDecreaseScrollSpeed = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_DECREASESCROLL"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_DECREASESCROLL_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTrainingDecreaseScrollSpeed);

		this.iKeyAssignTrainingIncreaseSongSpeed = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_INCREASESPEED"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_INCREASESPEED_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTrainingIncreaseSongSpeed);

		this.iKeyAssignTrainingDecreaseSongSpeed = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_DECREASESPEED"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_DECREASESPEED_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTrainingDecreaseSongSpeed);

		this.iKeyAssignTrainingBranchNormal = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_BRANCHNORMAL"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_BRANCHNORMAL_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTrainingBranchNormal);

		this.iKeyAssignTrainingBranchExpert = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_BRANCHEXPERT"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_BRANCHEXPERT_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTrainingBranchExpert);

		this.iKeyAssignTrainingBranchMaster = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_BRANCHMASTER"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_BRANCHMASTER_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTrainingBranchMaster);

		this.iKeyAssignTrainingMoveForwardMeasure = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_MOVEFORWARD"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_MOVEFORWARD_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTrainingMoveForwardMeasure);

		this.iKeyAssignTrainingMoveBackMeasure = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_MOVEBACKWARD"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_MOVEBACKWARD_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTrainingMoveBackMeasure);

		this.iKeyAssignTrainingSkipForwardMeasure = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_SKIPFORWARD"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_SKIPFORWARD_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTrainingSkipForwardMeasure);

		this.iKeyAssignTrainingSkipBackMeasure = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_SKIPBACKWARD"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_SKIPBACKWARD_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTrainingSkipBackMeasure);

		this.iKeyAssignTrainingJumpToFirstMeasure = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_JUMPTOFIRST"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_JUMPTOFIRST_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTrainingJumpToFirstMeasure);

		this.iKeyAssignTrainingJumpToLastMeasure = new CItemBase(CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_JUMPTOLAST"),
			CLangManager.LangInstance.GetString("SETTINGS_KEYASSIGN_TRAINING_JUMPTOLAST_DESC"));
		this.list項目リスト.Add(this.iKeyAssignTrainingJumpToLastMeasure);

		OnListMenuの初期化();
		this.n現在の選択項目 = 0;
		this.eメニュー種別 = Eメニュー種別.KeyAssignTraining;
	}
	#endregion
	public void t次に移動() {
		OpenTaiko.Skin.soundカーソル移動音.tPlay();
		if (this.b要素値にフォーカス中) {
			this.list項目リスト[this.n現在の選択項目].t項目値を前へ移動();
		} else {
			this.n目標のスクロールカウンタ += 100;
		}
	}
	public void t前に移動() {
		OpenTaiko.Skin.soundカーソル移動音.tPlay();
		if (this.b要素値にフォーカス中) {
			this.list項目リスト[this.n現在の選択項目].t項目値を次へ移動();
		} else {
			this.n目標のスクロールカウンタ -= 100;
		}
	}

	// CActivity 実装

	public override void Activate() {
		if (this.IsActivated)
			return;

		this.list項目リスト = new List<CItemBase>();
		this.eメニュー種別 = Eメニュー種別.Unknown;

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
		Array.Sort(skinSubFolders);
		skinNames = CSkin.GetSkinName(skinSubFolders);
		nSkinIndex = Array.BinarySearch(skinSubFolders, skinSubFolder_org);
		if (nSkinIndex < 0) // 念のため
		{
			nSkinIndex = 0;
		}
		nSkinSampleIndex = -1;
		#endregion

		this.t項目リストの設定_Drums();
		this.t項目リストの設定_System();    // 順番として、最後にSystemを持ってくること。設定一覧の初期位置がSystemのため。
		this.b要素値にフォーカス中 = false;
		this.n目標のスクロールカウンタ = 0;
		this.n現在のスクロールカウンタ = 0;
		this.nスクロール用タイマ値 = -1;
		this.ct三角矢印アニメ = new CCounter();

		this.iSystemBassBufferSizeMs_initial = this.iSystemBassBufferSizeMs.n現在の値;              // CONFIG脱出時にこの値から変更されているようなら
		if (OperatingSystem.IsWindows()) {
			this.iSystemSoundType_initial = this.iSystemSoundType.n現在選択されている項目番号;   // CONFIGに入ったときの値を保持しておく
			this.iSystemWASAPIBufferSizeMs_initial = this.iSystemWASAPIBufferSizeMs.n現在の値;              // CONFIG脱出時にこの値から変更されているようなら
			this.iSystemASIODevice_initial = this.iSystemASIODevice.n現在選択されている項目番号;
		}
		this.iSystemSoundTimerType_initial = this.iSystemSoundTimerType.GetIndex();
		base.Activate();
	}
	public override void DeActivate() {
		if (this.IsDeActivated)
			return;

		this.tConfigIniへ記録する();
		this.list項目リスト.Clear();
		this.ct三角矢印アニメ = null;

		base.DeActivate();
		#region [ Skin変更 ]
		if (OpenTaiko.Skin.GetCurrentSkinSubfolderFullName(true) != this.skinSubFolder_org) {
			OpenTaiko.app.RefreshSkin();
		}
		#endregion

		for (int i = 0; i < OpenTaiko.MAX_PLAYERS; i++) {
			int id = OpenTaiko.SaveFileInstances[OpenTaiko.GetActualPlayer(i)].data.TitleId;
			if (id > 0) {
				var title = OpenTaiko.Databases.DBNameplateUnlockables.data[id];
				OpenTaiko.SaveFileInstances[OpenTaiko.GetActualPlayer(i)].data.Title = title.nameplateInfo.cld.GetString("");
			}
			OpenTaiko.NamePlate.tNamePlateRefreshTitles(i);
		}
		// #24820 2013.1.22 yyagi CONFIGでWASAPI/ASIO/DirectSound関連の設定を変更した場合、サウンドデバイスを再構築する。
		// #33689 2014.6.17 yyagi CONFIGでSoundTimerTypeの設定を変更した場合も、サウンドデバイスを再構築する。
		#region [ サウンドデバイス変更 ]
		if (OperatingSystem.IsWindows()) {
			if (this.iSystemSoundType_initial != this.iSystemSoundType.n現在選択されている項目番号 ||
			this.iSystemBassBufferSizeMs_initial != this.iSystemBassBufferSizeMs.n現在の値 ||
			this.iSystemWASAPIBufferSizeMs_initial != this.iSystemWASAPIBufferSizeMs.n現在の値 ||
			this.iSystemASIODevice_initial != this.iSystemASIODevice.n現在選択されている項目番号 ||
			this.iSystemSoundTimerType_initial != this.iSystemSoundTimerType.GetIndex()) {
				ESoundDeviceType soundDeviceType;
				switch (this.iSystemSoundType.n現在選択されている項目番号) {
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
					this.iSystemBassBufferSizeMs.n現在の値,
					this.iSystemWASAPIBufferSizeMs.n現在の値,
					0,
					this.iSystemASIODevice.n現在選択されている項目番号,
					this.iSystemSoundTimerType.bON);
				OpenTaiko.app.ShowWindowTitle();
				OpenTaiko.Skin.ReloadSkin();// 音声の再読み込みをすることによって、音量の初期化を防ぐ
			}
		}
		else {
			if (this.iSystemBassBufferSizeMs_initial != this.iSystemBassBufferSizeMs.n現在の値 ||
				this.iSystemSoundTimerType_initial != this.iSystemSoundTimerType.GetIndex()) {
				OpenTaiko.SoundManager.tInitialize(ESoundDeviceType.Bass,
					this.iSystemBassBufferSizeMs.n現在の値,
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
		OpenTaiko.tテクスチャの解放(ref this.txSkinSample1);
		base.ReleaseManagedResource();
	}
	private void OnListMenuの初期化() {
		OnListMenuの解放();
		this.listMenu = new stMenuItemRight[this.list項目リスト.Count];
	}

	/// <summary>
	/// 事前にレンダリングしておいたテクスチャを解放する。
	/// </summary>
	private void OnListMenuの解放() {
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
	public int Draw(bool b項目リスト側にフォーカスがある) {
		if (this.IsDeActivated)
			return 0;

		// 進行

		#region [ 初めての進行描画 ]
		//-----------------
		if (base.IsFirstDraw) {
			this.nスクロール用タイマ値 = OpenTaiko.Timer.NowTimeMs;
			this.ct三角矢印アニメ.Start(0, 9, 50, OpenTaiko.Timer);
			base.IsFirstDraw = false;
		}
		//-----------------
		#endregion

		this.b項目リスト側にフォーカスがある = b項目リスト側にフォーカスがある;       // 記憶

		#region [ 項目スクロールの進行 ]
		//-----------------
		long n現在時刻 = OpenTaiko.Timer.NowTimeMs;
		if (n現在時刻 < this.nスクロール用タイマ値) this.nスクロール用タイマ値 = n現在時刻;

		const int INTERVAL = 2; // [ms]
		while ((n現在時刻 - this.nスクロール用タイマ値) >= INTERVAL) {
			int n目標項目までのスクロール量 = Math.Abs((int)(this.n目標のスクロールカウンタ - this.n現在のスクロールカウンタ));
			int n加速度 = 0;

			#region [ n加速度の決定；目標まで遠いほど加速する。]
			//-----------------
			if (n目標項目までのスクロール量 <= 100) {
				n加速度 = 2;
			} else if (n目標項目までのスクロール量 <= 300) {
				n加速度 = 3;
			} else if (n目標項目までのスクロール量 <= 500) {
				n加速度 = 4;
			} else {
				n加速度 = 8;
			}
			//-----------------
			#endregion
			#region [ this.n現在のスクロールカウンタに n加速度 を加減算。]
			//-----------------
			if (this.n現在のスクロールカウンタ < this.n目標のスクロールカウンタ) {
				this.n現在のスクロールカウンタ += n加速度;
				if (this.n現在のスクロールカウンタ > this.n目標のスクロールカウンタ) {
					// 目標を超えたら目標値で停止。
					this.n現在のスクロールカウンタ = this.n目標のスクロールカウンタ;
				}
			} else if (this.n現在のスクロールカウンタ > this.n目標のスクロールカウンタ) {
				this.n現在のスクロールカウンタ -= n加速度;
				if (this.n現在のスクロールカウンタ < this.n目標のスクロールカウンタ) {
					// 目標を超えたら目標値で停止。
					this.n現在のスクロールカウンタ = this.n目標のスクロールカウンタ;
				}
			}
			//-----------------
			#endregion
			#region [ 行超え処理、ならびに目標位置に到達したらスクロールを停止して項目変更通知を発行。]
			//-----------------
			if (this.n現在のスクロールカウンタ >= 100) {
				this.n現在の選択項目 = this.t次の項目(this.n現在の選択項目);
				this.n現在のスクロールカウンタ -= 100;
				this.n目標のスクロールカウンタ -= 100;
				if (this.n目標のスクロールカウンタ == 0) {
					OpenTaiko.stageConfig.t項目変更通知();
				}
			} else if (this.n現在のスクロールカウンタ <= -100) {
				this.n現在の選択項目 = this.t前の項目(this.n現在の選択項目);
				this.n現在のスクロールカウンタ += 100;
				this.n目標のスクロールカウンタ += 100;
				if (this.n目標のスクロールカウンタ == 0) {
					OpenTaiko.stageConfig.t項目変更通知();
				}
			}
			//-----------------
			#endregion

			this.nスクロール用タイマ値 += INTERVAL;
		}
		//-----------------
		#endregion

		#region [ ▲印アニメの進行 ]
		//-----------------
		if (this.b項目リスト側にフォーカスがある && (this.n目標のスクロールカウンタ == 0))
			this.ct三角矢印アニメ.TickLoop();
		//-----------------
		#endregion
		// 描画

		#region [ 計11個の項目パネルを描画する。]
		//-----------------
		int nItem = this.n現在の選択項目;
		for (int i = 0; i < (OpenTaiko.Skin.Config_ItemBox_Count / 2) - 1; i++)
			nItem = this.t前の項目(nItem);

		for (int i = 0; i < OpenTaiko.Skin.Config_ItemBox_Count; i++)      // n行番号 == 0 がフォーカスされている項目パネル。
		{
			bool centerFlag = i == (OpenTaiko.Skin.Config_ItemBox_Count / 2) - 1;

			#region [ 今まさに画面外に飛びだそうとしている項目パネルは描画しない。]
			//-----------------
			if (((i == 0) && (this.n現在のスクロールカウンタ > 0)) ||       // 上に飛び出そうとしている
				((i == OpenTaiko.Skin.Config_ItemBox_Count - 1) && (this.n現在のスクロールカウンタ < 0)))      // 下に飛び出そうとしている
			{
				nItem = this.t次の項目(nItem);
				continue;
			}
			//-----------------
			#endregion

			int n移動先の行の基本位置 = (this.n現在のスクロールカウンタ <= 0) ? ((i + 1) % OpenTaiko.Skin.Config_ItemBox_Count) : (((i - 1) + OpenTaiko.Skin.Config_ItemBox_Count) % OpenTaiko.Skin.Config_ItemBox_Count);
			int x = OpenTaiko.Skin.Config_ItemBox_X[i] + ((int)((OpenTaiko.Skin.Config_ItemBox_X[n移動先の行の基本位置] - OpenTaiko.Skin.Config_ItemBox_X[i]) * (((double)Math.Abs(this.n現在のスクロールカウンタ)) / 100.0)));
			int y = OpenTaiko.Skin.Config_ItemBox_Y[i] + ((int)((OpenTaiko.Skin.Config_ItemBox_Y[n移動先の行の基本位置] - OpenTaiko.Skin.Config_ItemBox_Y[i]) * (((double)Math.Abs(this.n現在のスクロールカウンタ)) / 100.0)));

			#region [ 現在の行の項目パネル枠を描画。]
			//-----------------
			switch (this.list項目リスト[nItem].eパネル種別) {
				case CItemBase.EPanelType.Normal:
				case CItemBase.EPanelType.Other:
					if (OpenTaiko.Tx.Config_ItemBox != null)
						OpenTaiko.Tx.Config_ItemBox.t2D描画(x, y);
					break;
			}
			//-----------------
			#endregion
			#region [ 現在の行の項目名を描画。]
			//-----------------
			if (listMenu[nItem].txMenuItemRight != null)    // 自前のキャッシュに含まれているようなら、再レンダリングせずキャッシュを使用
			{
				listMenu[nItem].txMenuItemRight.t2D描画(x + OpenTaiko.Skin.Config_ItemBox_Font_Offset[0], y + OpenTaiko.Skin.Config_ItemBox_Font_Offset[1]);
			} else {
				using (var bmpItem = prvFont.DrawText(this.list項目リスト[nItem].str項目名, Color.White, Color.Black, null, 30)) {
					listMenu[nItem].txMenuItemRight = OpenTaiko.tテクスチャの生成(bmpItem);
				}
			}
			//-----------------
			#endregion
			#region [ 現在の行の項目の要素を描画。]
			//-----------------
			string strParam = null;
			bool b強調 = false;
			switch (this.list項目リスト[nItem].e種別) {
				case CItemBase.E種別.ONorOFFトグル:
					#region [ *** ]
					//-----------------
					strParam = ((CItemToggle)this.list項目リスト[nItem]).bON ? "ON" : "OFF";
					break;
				//-----------------
				#endregion

				case CItemBase.E種別.ONorOFFor不定スリーステート:
					#region [ *** ]
					//-----------------
					switch (((CItemThreeState)this.list項目リスト[nItem]).e現在の状態) {
						case CItemThreeState.E状態.ON:
							strParam = "ON";
							break;

						case CItemThreeState.E状態.不定:
							strParam = "- -";
							break;

						default:
							strParam = "OFF";
							break;
					}
					break;
				//-----------------
				#endregion

				case CItemBase.E種別.整数:      // #24789 2011.4.8 yyagi: add PlaySpeed supports (copied them from OPTION)
					#region [ *** ]
					//-----------------
					if (this.list項目リスト[nItem] == this.iCommonPlaySpeed) {
						double d = ((double)((CItemInteger)this.list項目リスト[nItem]).n現在の値) / 20.0;
						strParam = d.ToString("0.000");
					} else {
						strParam = ((CItemInteger)this.list項目リスト[nItem]).n現在の値.ToString();
					}
					b強調 = centerFlag && this.b要素値にフォーカス中;
					break;
				//-----------------
				#endregion

				case CItemBase.E種別.リスト: // #28195 2012.5.2 yyagi: add Skin supports
					#region [ *** ]
				//-----------------
				{
						CItemList list = (CItemList)this.list項目リスト[nItem];
						strParam = list.list項目値[list.n現在選択されている項目番号];

						#region [ 必要な場合に、Skinのサンプルを生成・描画する。#28195 2012.5.2 yyagi ]
						if (this.list項目リスト[this.n現在の選択項目] == this.iSystemSkinSubfolder) {
							tGenerateSkinSample();      // 最初にSkinの選択肢にきたとき(Enterを押す前)に限り、サンプル生成が発生する。
							if (txSkinSample1 != null) {
								txSkinSample1.t2D描画(OpenTaiko.Skin.Config_SkinSample1[0], OpenTaiko.Skin.Config_SkinSample1[1]);
							}
						}
						#endregion
						break;
					}
					//-----------------
					#endregion
			}
			if (b強調) {
				using (var bmpStr = prvFont.DrawText(strParam,
						   Color.Black,
						   Color.White,
						   null,
						   OpenTaiko.Skin.Config_Selected_Menu_Text_Grad_Color_1,
						   OpenTaiko.Skin.Config_Selected_Menu_Text_Grad_Color_2,
						   30)) {
					using (var txStr = OpenTaiko.tテクスチャの生成(bmpStr, false)) {
						txStr.t2D描画(x + OpenTaiko.Skin.Config_ItemBox_ItemValue_Font_Offset[0], y + OpenTaiko.Skin.Config_ItemBox_ItemValue_Font_Offset[1]);
					}
				}
			} else {
				int nIndex = this.list項目リスト[nItem].GetIndex();
				if (listMenu[nItem].nParam != nIndex || listMenu[nItem].txParam == null) {
					stMenuItemRight stm = listMenu[nItem];
					stm.nParam = nIndex;
					object o = this.list項目リスト[nItem].obj現在値();
					stm.strParam = (o == null) ? "" : o.ToString();

					using (var bmpStr = prvFont.DrawText(strParam, Color.White, Color.Black, null, 30)) {
						stm.txParam = OpenTaiko.tテクスチャの生成(bmpStr, false);
					}

					listMenu[nItem] = stm;
				}
				listMenu[nItem].txParam.t2D描画(x + OpenTaiko.Skin.Config_ItemBox_ItemValue_Font_Offset[0], y + OpenTaiko.Skin.Config_ItemBox_ItemValue_Font_Offset[1]);
			}
			//-----------------
			#endregion

			nItem = this.t次の項目(nItem);
		}
		//-----------------
		#endregion

		#region [ 項目リストにフォーカスがあって、かつスクロールが停止しているなら、パネルの上下に▲印を描画する。]
		//-----------------
		if (this.b項目リスト側にフォーカスがある && (this.n目標のスクロールカウンタ == 0)) {
			int x_upper;
			int x_lower;
			int y_upper;
			int y_lower;

			// 位置決定。

			if (this.b要素値にフォーカス中) {
				x_upper = OpenTaiko.Skin.Config_Arrow_Focus_X[0];  // 要素値の上下あたり。
				x_lower = OpenTaiko.Skin.Config_Arrow_Focus_X[1];  // 要素値の上下あたり。
				y_upper = OpenTaiko.Skin.Config_Arrow_Focus_Y[0] - this.ct三角矢印アニメ.CurrentValue;
				y_lower = OpenTaiko.Skin.Config_Arrow_Focus_Y[1] + this.ct三角矢印アニメ.CurrentValue;
			} else {
				x_upper = OpenTaiko.Skin.Config_Arrow_X[0];  // 要素値の上下あたり。
				x_lower = OpenTaiko.Skin.Config_Arrow_X[1];  // 要素値の上下あたり。
				y_upper = OpenTaiko.Skin.Config_Arrow_Y[0] - this.ct三角矢印アニメ.CurrentValue;
				y_lower = OpenTaiko.Skin.Config_Arrow_Y[1] + this.ct三角矢印アニメ.CurrentValue;
			}

			// 描画。

			if (OpenTaiko.Tx.Config_Arrow != null) {
				OpenTaiko.Tx.Config_Arrow.t2D描画(x_upper, y_upper, new Rectangle(0, 0, OpenTaiko.Tx.Config_Arrow.sz画像サイズ.Width, OpenTaiko.Tx.Config_Arrow.sz画像サイズ.Height / 2));
				OpenTaiko.Tx.Config_Arrow.t2D描画(x_lower, y_lower, new Rectangle(0, OpenTaiko.Tx.Config_Arrow.sz画像サイズ.Height / 2, OpenTaiko.Tx.Config_Arrow.sz画像サイズ.Width, OpenTaiko.Tx.Config_Arrow.sz画像サイズ.Height / 2));
			}
		}
		//-----------------
		#endregion
		return 0;
	}


	// その他

	#region [ private ]
	//-----------------
	private enum Eメニュー種別 {
		System,
		Drums,
		KeyAssignSystem,        // #24609 2011.4.12 yyagi: 画面キャプチャキーのアサイン
		KeyAssignDrums,
		KeyAssignTraining,
		Unknown

	}

	private bool b項目リスト側にフォーカスがある;
	private bool b要素値にフォーカス中;
	private CCounter ct三角矢印アニメ;
	private Eメニュー種別 eメニュー種別;
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
	private CItemBase iKeyAssignSystemNewHeya;
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
	private CItemToggle iSystemFullscreen;
	private CItemInteger iSystemMinComboDrums;
	private CItemInteger iSystemPreviewImageWait;
	private CItemInteger iSystemPreviewSoundWait;
	private CItemToggle iSystemRandomFromSubBox;
	private CItemBase iSystemReturnToMenu;
	private CItemToggle iSystemVSyncWait;
	private CItemToggle iSystemAutoResultCapture;       // #25399 2011.6.9 yyagi
	private CItemToggle SendDiscordPlayingInformation;
	private CItemToggle iSystemBufferedInput;
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

	private List<CItemBase> list項目リスト;
	private long nスクロール用タイマ値;
	private int n現在のスクロールカウンタ;
	private int n目標のスクロールカウンタ;

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
	private int nSkinSampleIndex;
	private int nSkinIndex;

	private CItemBase iDrumsGoToCalibration;
	private CItemBase iDrumsGoToKeyAssign;
	private CItemBase iDrumsGoToTrainingKeyAssign;
	private CItemBase iSystemGoToKeyAssign;
	private CItemInteger iCommonPlaySpeed;

	private CItemInteger iLayoutType;

	private CItemBase iDrumsReturnToMenu;
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
	private CItemBase iSystemReloadDTX;                 // #32081 2013.10.21 yyagi
	private CItemBase iSystemHardReloadDTX;
	private CItemBase isSystemImportingScore;

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

	private int t前の項目(int nItem) {
		if (--nItem < 0) {
			nItem = this.list項目リスト.Count - 1;
		}
		return nItem;
	}
	private int t次の項目(int nItem) {
		if (++nItem >= this.list項目リスト.Count) {
			nItem = 0;
		}
		return nItem;
	}
	private void tConfigIniへ記録する() {
		switch (this.eメニュー種別) {
			case Eメニュー種別.System:
				this.tConfigIniへ記録する_System();
				return;

			case Eメニュー種別.Drums:
				this.tConfigIniへ記録する_Drums();
				return;
		}
	}
	private void tConfigIniへ記録する_System() {
		OpenTaiko.ConfigIni.nSongSpeed = this.iCommonPlaySpeed.n現在の値;

		OpenTaiko.ConfigIni.nGraphicsDeviceType = GraphicsDeviceFromString(AvailableGraphicsDevices[this.iSystemGraphicsType.n現在選択されている項目番号]);
		OpenTaiko.ConfigIni.bFullScreen = this.iSystemFullscreen.bON;
		OpenTaiko.ConfigIni.bIncludeSubfoldersOnRandomSelect = this.iSystemRandomFromSubBox.bON;

		OpenTaiko.ConfigIni.bEnableVSync = this.iSystemVSyncWait.bON;
		OpenTaiko.ConfigIni.bBufferedInputs = this.iSystemBufferedInput.bON;
		OpenTaiko.InputManager?.SetUseBufferInput(OpenTaiko.ConfigIni.bBufferedInputs);
		OpenTaiko.ConfigIni.bEnableAVI = this.iSystemAVI.bON;
		OpenTaiko.ConfigIni.eClipDispType = (EClipDispType)this.iSystemAVIDisplayMode.n現在選択されている項目番号;
		OpenTaiko.ConfigIni.bEnableBGA = this.iSystemBGA.bON;
		OpenTaiko.ConfigIni.nMsWaitPreviewSoundFromSongSelected = this.iSystemPreviewSoundWait.n現在の値;
		OpenTaiko.ConfigIni.nMsWaitPreviewImageFromSongSelected = this.iSystemPreviewImageWait.n現在の値;
		OpenTaiko.ConfigIni.bDisplayDebugInfo = this.iSystemDebugInfo.bON;
		OpenTaiko.ConfigIni.nBackgroundTransparency = this.iSystemBGAlpha.n現在の値;
		OpenTaiko.ConfigIni.bBGMPlayVoiceSound = this.iSystemBGMSound.bON;
		OpenTaiko.ConfigIni.bDanTowerHide = this.iDanTowerHide.bON;

		OpenTaiko.ConfigIni.ApplySongVol = this.iSystemApplySongVol.bON;
		OpenTaiko.ConfigIni.SoundEffectLevel = this.iSystemSoundEffectLevel.n現在の値;
		OpenTaiko.ConfigIni.VoiceLevel = this.iSystemVoiceLevel.n現在の値;
		OpenTaiko.ConfigIni.SongPreviewLevel = this.iSystemSongPreviewLevel.n現在の値;
		OpenTaiko.ConfigIni.SongPlaybackLevel = this.iSystemSongPlaybackLevel.n現在の値;
		OpenTaiko.ConfigIni.KeyboardSoundLevelIncrement = this.iSystemKeyboardSoundLevelIncrement.n現在の値;
		OpenTaiko.ConfigIni.MusicPreTimeMs = this.MusicPreTimeMs.n現在の値;

		OpenTaiko.ConfigIni.bOutputLogs = this.iLogOutputLog.bON;
		OpenTaiko.ConfigIni.bIsAutoResultCapture = this.iSystemAutoResultCapture.bON;                  // #25399 2011.6.9 yyagi
		OpenTaiko.ConfigIni.SendDiscordPlayingInformation = this.SendDiscordPlayingInformation.bON;

		OpenTaiko.ConfigIni.nRisky = this.iSystemRisky.n現在の値;                                      // #23559 2011.7.27 yyagi

		OpenTaiko.ConfigIni.strSystemSkinSubfolderFullName = skinSubFolders[nSkinIndex];               // #28195 2012.5.2 yyagi
		OpenTaiko.Skin.SetCurrentSkinSubfolderFullName(OpenTaiko.ConfigIni.strSystemSkinSubfolderFullName, true);

		OpenTaiko.ConfigIni.nBassBufferSizeMs = this.iSystemBassBufferSizeMs.n現在の値;                // #24820 2013.1.15 yyagi
		if (OperatingSystem.IsWindows()) {
			OpenTaiko.ConfigIni.nSoundDeviceType = this.iSystemSoundType.n現在選択されている項目番号;       // #24820 2013.1.3 yyagi
			OpenTaiko.ConfigIni.nWASAPIBufferSizeMs = this.iSystemWASAPIBufferSizeMs.n現在の値;                // #24820 2013.1.15 yyagi
			OpenTaiko.ConfigIni.nASIODevice = this.iSystemASIODevice.n現在選択されている項目番号;           // #24820 2013.1.17 yyagi
		}
		OpenTaiko.ConfigIni.bUseOSTimer = this.iSystemSoundTimerType.bON;                              // #33689 2014.6.17 yyagi

		OpenTaiko.ConfigIni.bTimeStretch = this.iSystemTimeStretch.bON;                                    // #23664 2013.2.24 yyagi


		OpenTaiko.ConfigIni.sLang = CLangManager.intToLang(this.iSystemLanguage.n現在選択されている項目番号);
		CLangManager.langAttach(OpenTaiko.ConfigIni.sLang);
		OpenTaiko.ConfigIni.ShowChara = this.ShowChara.bON;
		OpenTaiko.ConfigIni.ShowDancer = this.ShowDancer.bON;
		OpenTaiko.ConfigIni.ShowRunner = this.ShowRunner.bON;
		OpenTaiko.ConfigIni.ShowMob = this.ShowMob.bON;
		OpenTaiko.ConfigIni.ShowFooter = this.ShowFooter.bON;
		OpenTaiko.ConfigIni.ShowPuchiChara = this.ShowPuchiChara.bON;

		OpenTaiko.ConfigIni.nPlayerCount = this.iTaikoPlayerCount.n現在の値;
		OpenTaiko.ConfigIni.FastRender = this.FastRender.bON;
		OpenTaiko.ConfigIni.ASyncTextureLoad = this.ASyncTextureLoad.bON;
		OpenTaiko.ConfigIni.SimpleMode = this.SimpleMode.bON;

#if DEBUG
		OpenTaiko.ConfigIni.DEBUG_bShowImgui = this.debugImGui.bON;
#endif
	}
	private void tConfigIniへ記録する_Drums() {
		OpenTaiko.ConfigIni.nRollsPerSec = this.iRollsPerSec.n現在の値;

		OpenTaiko.ConfigIni.nDefaultAILevel = this.iAILevel.n現在の値;
		for (int i = 0; i < 2; i++)
			OpenTaiko.NamePlate.tNamePlateRefreshTitles(i);

		OpenTaiko.ConfigIni.bTight = this.iDrumsTight.bON;

		OpenTaiko.ConfigIni.nGlobalOffsetMs = this.iGlobalOffsetMs.n現在の値;
		OpenTaiko.ConfigIni.nControllerDeadzone = this.iControllerDeadzone.n現在の値;
		OpenTaiko.InputManager.Deadzone = OpenTaiko.ConfigIni.nControllerDeadzone / 100.0f;
		OpenTaiko.ConfigIni.bIgnoreSongUnlockables = this.iTaikoIgnoreSongUnlockables.bON;

		OpenTaiko.ConfigIni.nMinDisplayedCombo.Drums = this.iSystemMinComboDrums.n現在の値;
		OpenTaiko.ConfigIni.nRisky = this.iSystemRisky.n現在の値;                      // #23559 2911.7.27 yyagi
		OpenTaiko.ConfigIni.bBranchGuide = this.iTaikoBranchGuide.bON;
		OpenTaiko.ConfigIni.nDefaultCourse = this.iTaikoDefaultCourse.n現在選択されている項目番号;
		OpenTaiko.ConfigIni.nScoreMode = this.iTaikoScoreMode.n現在選択されている項目番号;
		OpenTaiko.ConfigIni.ShinuchiMode = this.ShinuchiMode.bON;
		OpenTaiko.ConfigIni.nBranchAnime = this.iTaikoBranchAnime.n現在選択されている項目番号;
		OpenTaiko.ConfigIni.bNoInfo = this.iTaikoNoInfo.bON;

		OpenTaiko.ConfigIni.eGameMode = (EGame)this.iTaikoGameMode.n現在選択されている項目番号;
		OpenTaiko.ConfigIni.bJudgeCountDisplay = this.iTaikoJudgeCountDisp.bON;
		OpenTaiko.ConfigIni.ShowExExtraAnime = this.iShowExExtraAnime.bON;
		OpenTaiko.ConfigIni.bJudgeBigNotes = this.iTaikoBigNotesJudge.bON;
		OpenTaiko.ConfigIni.bForceNormalGauge = this.iTaikoForceNormalGauge.bON;

		OpenTaiko.ConfigIni.TokkunSkipMeasures = this.TokkunSkipCount.n現在の値;
		OpenTaiko.ConfigIni.TokkunMashInterval = this.TokkunMashInterval.n現在の値;
	}
	//-----------------
	#endregion
}
