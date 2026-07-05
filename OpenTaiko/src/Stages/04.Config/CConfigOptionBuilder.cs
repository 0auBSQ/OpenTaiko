using System;
using System.Collections.Generic;
using System.IO;
using FDK;

namespace OpenTaiko;

/// <summary>
/// Builds the <see cref="CLuaConfigModel"/> (the option schema) for the Lua config_ui ROActivity, mirroring the
/// fields + side effects of the old <c>CActConfigList.tConfigIniRecord_*</c>. Each option's mutator writes the
/// <c>OpenTaiko.ConfigIni</c> field LIVE (plus immediate effects: language re-localize, deadzone, broadcasting,
/// window/vsync flags, time-stretch). Skin/render-resolution/sound-device changes are written live but their
/// heavy reload/rebuild is deferred to <see cref="CStageConfig.DeActivate"/> (snapshot compare). Saving to disk
/// happens on stage exit via the existing <c>tExport</c>.
/// </summary>
public static class CConfigOptionBuilder {
	// Settings strings come from the global game translations (Lang/*/lang.json). Anything NOT defined there
	// (e.g. settings strings authored per-skin) falls back to the active skin's theme translations
	// (System/<skin>/Locales/<lang>.json via CSkinLocaleManager), so a skin can localize its own settings.
	internal static string L(string k) {
		var lang = CLangManager.LangInstance;
		if (lang != null && lang.HasString(k)) return lang.GetString(k);
		var skin = OpenTaiko.Databases?.SkinLocaleManager;
		if (skin != null) {
			string s = skin.GetString(k);
			if (s != null && !s.StartsWith("[LOCALE NOT FOUND", StringComparison.Ordinal)) return s;
		}
		return lang != null ? lang.GetString(k) : k;
	}

	/// <summary>Like <see cref="L(string)"/> but, when neither the global Lang nor the active skin locale defines
	/// <paramref name="k"/>, returns the English <paramref name="fallback"/> (instead of a "KEY NOT FOUND" placeholder).
	/// Used for the menu chrome — section/group headers and value words — that lives in the SKIN locale, so a skin
	/// without those keys still shows readable English rather than a raw key.</summary>
	internal static string L(string k, string fallback) {
		var lang = CLangManager.LangInstance;
		if (lang != null && lang.HasString(k)) return lang.GetString(k);
		var skin = OpenTaiko.Databases?.SkinLocaleManager;
		if (skin != null) {
			string s = skin.GetString(k);
			if (s != null && !s.StartsWith("[LOCALE NOT FOUND", StringComparison.Ordinal)) return s;
		}
		return fallback;
	}

	public sealed class Hooks {
		public Action Relocalize = () => { };     // language changed → rebuild model + push reload to Lua
		public Action Calibration = () => { };    // launch the C# calibration tap-test
		public Action ReloadSongs = () => { };
		public Action HardReloadSongs = () => { };
		public Action ImportScore = () => { };
	}

	// the sorted skin subfolder list parallel to the skin chooser's choices (so index → folder)
	private static string[] _skinSubFolders = Array.Empty<string>();

	public static CLuaConfigModel Build(Hooks hooks) {
		var m = new CLuaConfigModel {
			Keys = new CLuaKeyConfigService(),
			Categories = new List<string> { "System", "Game", "Theme" },
			CategoryLabels = new List<string> { L("SETTINGS_SYSTEM"), L("SETTINGS_GAME"), L("SETTINGS_THEME") },
			CategoryDescs = new List<string> { L("SETTINGS_SYSTEM_DESC"), L("SETTINGS_GAME_DESC"), L("SETTINGS_THEME_DESC") },
		};
		var cfg = OpenTaiko.ConfigIni;
		var O = m.Options;

		// Localized SECTION headers. The Section string is BOTH the on-screen group header AND the Lua grouping key,
		// so we resolve it once here (skin locale via L(key, englishFallback)); options keep landing in the right
		// group because the resolved label is identical for every option in that section.
		string secSongs   = L("SETTINGS_SECTION_SONGS", "Songs & Maintenance");
		string secLang    = L("SETTINGS_SECTION_LANGUAGE", "Language");
		string secDisplay = L("SETTINGS_SECTION_DISPLAY", "Display & Window");
		string secBg      = L("SETTINGS_SECTION_BACKGROUND", "Background & Video");
		string secAudio   = L("SETTINGS_SECTION_AUDIO", "Audio");
		string secVolume  = L("SETTINGS_SECTION_VOLUME", "Volume");
		string secChars   = L("SETTINGS_SECTION_CHARACTERS", "Characters & HUD");
		string secIntegr  = L("SETTINGS_SECTION_INTEGRATIONS", "Integrations & Advanced");
		string secInput   = L("SETTINGS_SECTION_INPUT", "Input");
		string secGameplay = L("SETTINGS_SECTION_GAMEPLAY", "Gameplay");
		string secFeedback = L("SETTINGS_SECTION_FEEDBACK", "Display & Feedback");
		string secTiming  = L("SETTINGS_SECTION_TIMING", "Timing");
		string secAutoAi  = L("SETTINGS_SECTION_AUTOAI", "Auto & AI");
		string secUnlock  = L("SETTINGS_SECTION_UNLOCKABLES", "Unlockables");
		string secTraining = L("SETTINGS_SECTION_TRAINING", "Training");

		// ── SYSTEM ──────────────────────────────────────────────────────────────────
		const string SYS = "System";
		// Appearance: the skin/theme selector goes first (live-set, heavy reload deferred to exit).
		BuildSkinChooser(O, cfg);

		// Songs & Maintenance
		O.Add(CLuaConfigOption.Action_(SYS, secSongs,L("SETTINGS_SYSTEM_RELOADSONG"), L("SETTINGS_SYSTEM_RELOADSONG_DESC"), hooks.ReloadSongs));
		O.Add(CLuaConfigOption.Action_(SYS, secSongs,L("SETTINGS_SYSTEM_RELOADSONGCACHE"), L("SETTINGS_SYSTEM_RELOADSONGCACHE_DESC"), hooks.HardReloadSongs));
		O.Add(CLuaConfigOption.Action_(SYS, secSongs,L("SETTINGS_SYSTEM_IMPORTSCOREINI"), L("SETTINGS_SYSTEM_IMPORTSCOREINI_DESC"), hooks.ImportScore));

		// Language (player count moved to Game; random-subfolder + dan/tower-hide are handled by the Lua song list now)
		O.Add(CLuaConfigOption.Choice_(SYS, secLang,L("SETTINGS_SYSTEM_LANGUAGE"), L("SETTINGS_SYSTEM_LANGUAGE_DESC"),
			CLangManager.Languages, CLangManager.langToInt(cfg.sLang), idx => {
				cfg.sLang = CLangManager.intToLang(idx);
				CLangManager.langAttach(cfg.sLang);
				hooks.Relocalize();
			}));

		// Display & Window
		string[] graphics = AvailableGraphicsDevices();
		O.Add(CLuaConfigOption.Choice_(SYS, secDisplay,L("SETTINGS_SYSTEM_GRAPHICSAPI"), L("SETTINGS_SYSTEM_GRAPHICSAPI_DESC"),
			graphics, Math.Max(0, Array.IndexOf(graphics, GraphicsName(cfg.nGraphicsDeviceType))),
			idx => cfg.nGraphicsDeviceType = GraphicsInt(graphics[idx])));
		O.Add(CLuaConfigOption.Choice_(SYS, secDisplay,L("SETTINGS_SYSTEM_WINDOWMODE"), L("SETTINGS_SYSTEM_WINDOWMODE_DESC"),
			new[] { L("SETTINGS_SYSTEM_WINDOWMODE_WINDOWED"), L("SETTINGS_SYSTEM_WINDOWMODE_FULLSCREEN"), L("SETTINGS_SYSTEM_WINDOWMODE_BORDERLESS") },
			cfg.nWindowMode, idx => { cfg.nWindowMode = idx; OpenTaiko.app.bSwitchFullScreenAtNextFrame = true; }));
		// Render resolution (from the current skin's Resolutions)
		{
			var res = OpenTaiko.Skin.Resolutions;
			var labels = new string[res.Count];
			int cur = 0;
			for (int i = 0; i < res.Count; i++) {
				int rw = (int)Math.Round(OpenTaiko.Skin.Resolution[0] * res[i].val);
				int rh = (int)Math.Round(OpenTaiko.Skin.Resolution[1] * res[i].val);
				labels[i] = res[i].label + " (" + rw + "x" + rh + ")";
				if (Math.Abs(res[i].val - cfg.fRenderScale) < 1e-4) cur = i;
			}
			O.Add(CLuaConfigOption.Choice_(SYS, secDisplay,L("SETTINGS_SYSTEM_RESOLUTION"), L("SETTINGS_SYSTEM_RESOLUTION_DESC"),
				labels, cur, idx => { idx = Math.Clamp(idx, 0, res.Count - 1); cfg.fRenderScale = (float)res[idx].val; }));
		}
		// One framerate control per platform: iOS caps FPS (60 vs the display's max), desktop toggles VSync.
		if (OperatingSystem.IsIOS())
			O.Add(CLuaConfigOption.Choice_(SYS, secDisplay,L("SETTINGS_SYSTEM_FRAMERATE"), L("SETTINGS_SYSTEM_FRAMERATE_DESC"),
				new[] { "60 FPS", "Unlimited" }, cfg.biOSUnlimitedFrameRate ? 1 : 0,
				idx => cfg.biOSUnlimitedFrameRate = idx == 1));
		else
			O.Add(CLuaConfigOption.Toggle_(SYS, secDisplay,L("SETTINGS_SYSTEM_VSYNC"), L("SETTINGS_SYSTEM_VSYNC_DESC"),
				cfg.bEnableVSync, v => { cfg.bEnableVSync = v; OpenTaiko.app.bSwitchVSyncAtTheNextFrame = true; }));
		// iOS only: resize the on-screen Don drum circle (radius as % of screen width).
		if (OperatingSystem.IsIOS())
			O.Add(CLuaConfigOption.Int_(SYS, secDisplay,"Touch Drum Size", "Radius of the Don drum circle as % of screen width.",
				cfg.nTouchDrumVisual, 10, 50, 1, v => cfg.nTouchDrumVisual = v));
		O.Add(CLuaConfigOption.Int_(SYS, secDisplay,L("SETTINGS_SYSTEM_LANEOPACITY"), L("SETTINGS_SYSTEM_LANEOPACITY_DESC"),
			cfg.nBackgroundTransparency, 0, 255, 5, v => cfg.nBackgroundTransparency = v));
		O.Add(CLuaConfigOption.Toggle_(SYS, secDisplay,L("SETTINGS_SYSTEM_FASTRENDER"), L("SETTINGS_SYSTEM_FASTRENDER_DESC"),
			cfg.FastRender, v => cfg.FastRender = v));
		O.Add(CLuaConfigOption.Toggle_(SYS, secDisplay,L("SETTINGS_SYSTEM_SIMPLEMODE"), L("SETTINGS_SYSTEM_SIMPLEMODE_DESC"),
			cfg.SimpleMode, v => cfg.SimpleMode = v));

		// Background & Video
		O.Add(CLuaConfigOption.Toggle_(SYS, secBg,L("SETTINGS_SYSTEM_BGMOVIE"), L("SETTINGS_SYSTEM_BGMOVIE_DESC"),
			cfg.bEnableAVI, v => cfg.bEnableAVI = v));
		O.Add(CLuaConfigOption.Choice_(SYS, secBg,L("SETTINGS_SYSTEM_BGMOVIEDISPLAY"), L("SETTINGS_SYSTEM_BGMOVIEDISPLAY_DESC"),
			new[] { L("SETTINGS_SYSTEM_BGMOVIEDISPLAY_NONE"), L("SETTINGS_SYSTEM_BGMOVIEDISPLAY_FULL"), L("SETTINGS_SYSTEM_BGMOVIEDISPLAY_MINI"), L("SETTINGS_SYSTEM_BGMOVIEDISPLAY_BOTH") },
			(int)cfg.eClipDispType, idx => cfg.eClipDispType = (EClipDispType)idx));
		O.Add(CLuaConfigOption.Toggle_(SYS, secBg,L("SETTINGS_SYSTEM_BGA"), L("SETTINGS_SYSTEM_BGA_DESC"),
			cfg.bEnableBGA, v => cfg.bEnableBGA = v));

		// Audio
		if (OperatingSystem.IsWindows())
			O.Add(CLuaConfigOption.Choice_(SYS, secAudio,L("SETTINGS_SYSTEM_AUDIOPLAYBACK"), L("SETTINGS_SYSTEM_AUDIOPLAYBACK_DESC"),
				new[] { "Bass", "ASIO", "WASAPI Exclusive", "WASAPI Shared" }, cfg.nSoundDeviceType, idx => cfg.nSoundDeviceType = idx));
		O.Add(CLuaConfigOption.Int_(SYS, secAudio,L("SETTINGS_SYSTEM_BASSBUFFER"), L("SETTINGS_SYSTEM_BASSBUFFER_DESC"),
			cfg.nBassBufferSizeMs, 0, 99999, 1, v => cfg.nBassBufferSizeMs = v));
		if (OperatingSystem.IsWindows()) {
			O.Add(CLuaConfigOption.Int_(SYS, secAudio,L("SETTINGS_SYSTEM_WASAPIBUFFER"), L("SETTINGS_SYSTEM_WASAPIBUFFER_DESC"),
				cfg.nWASAPIBufferSizeMs, 0, 99999, 1, v => cfg.nWASAPIBufferSizeMs = v));
			var asio = CEnumerateAllAsioDevices.GetAllASIODevices();
			if (asio != null && asio.Length > 0)
				O.Add(CLuaConfigOption.Choice_(SYS, secAudio,L("SETTINGS_SYSTEM_ASIOPLAYBACK"), L("SETTINGS_SYSTEM_ASIOPLAYBACK_DESC"),
					asio, Math.Clamp(cfg.nASIODevice, 0, asio.Length - 1), idx => cfg.nASIODevice = idx));
		}
		O.Add(CLuaConfigOption.Toggle_(SYS, secAudio,L("SETTINGS_SYSTEM_OSTIMER"), L("SETTINGS_SYSTEM_OSTIMER_DESC"),
			cfg.bUseOSTimer, v => cfg.bUseOSTimer = v));
		O.Add(CLuaConfigOption.Toggle_(SYS, secAudio,L("SETTINGS_SYSTEM_TIMESTRETCH"), L("SETTINGS_SYSTEM_TIMESTRETCH_DESC"),
			cfg.bTimeStretch, v => { cfg.bTimeStretch = v; SoundManager.bIsTimeStretch = v; }));
		O.Add(CLuaConfigOption.Toggle_(SYS, secAudio,L("SETTINGS_SYSTEM_SONGPLAYBACK"), L("SETTINGS_SYSTEM_SONGPLAYBACK_DESC"),
			cfg.bBGMPlayVoiceSound, v => cfg.bBGMPlayVoiceSound = v));
		O.Add(CLuaConfigOption.Toggle_(SYS, secAudio,L("SETTINGS_SYSTEM_USESONGVOL"), L("SETTINGS_SYSTEM_USESONGVOL_DESC"),
			cfg.ApplySongVol, v => cfg.ApplySongVol = v));

		// Volume
		O.Add(CLuaConfigOption.Int_(SYS, secVolume,L("SETTINGS_SYSTEM_MASTERVOL"), L("SETTINGS_SYSTEM_MASTERVOL_DESC"),
			cfg.MasterLevel, CSound.MinimumGroupLevel, CSound.MaximumGroupLevel, 1, v => cfg.MasterLevel = v));
		O.Add(CLuaConfigOption.Int_(SYS, secVolume,L("SETTINGS_SYSTEM_SEVOL"), L("SETTINGS_SYSTEM_SEVOL_DESC"),
			cfg.SoundEffectLevel, CSound.MinimumGroupLevel, CSound.MaximumGroupLevel, 1, v => cfg.SoundEffectLevel = v));
		O.Add(CLuaConfigOption.Int_(SYS, secVolume,L("SETTINGS_SYSTEM_VOICEVOL"), L("SETTINGS_SYSTEM_VOICEVOL_DESC"),
			cfg.VoiceLevel, CSound.MinimumGroupLevel, CSound.MaximumGroupLevel, 1, v => cfg.VoiceLevel = v));
		O.Add(CLuaConfigOption.Int_(SYS, secVolume,L("SETTINGS_SYSTEM_SONGPREVIEWVOL"), L("SETTINGS_SYSTEM_SONGPREVIEWVOL_DESC"),
			cfg.SongPreviewLevel, CSound.MinimumGroupLevel, CSound.MaximumGroupLevel, 1, v => cfg.SongPreviewLevel = v));
		O.Add(CLuaConfigOption.Int_(SYS, secVolume,L("SETTINGS_SYSTEM_SONGVOL"), L("SETTINGS_SYSTEM_SONGVOL_DESC"),
			cfg.SongPlaybackLevel, CSound.MinimumGroupLevel, CSound.MaximumGroupLevel, 1, v => cfg.SongPlaybackLevel = v));
		O.Add(CLuaConfigOption.Int_(SYS, secVolume,L("SETTINGS_SYSTEM_VOLINCREMENT"), L("SETTINGS_SYSTEM_VOLINCREMENT_DESC"),
			cfg.KeyboardSoundLevelIncrement, 1, 20, 1, v => cfg.KeyboardSoundLevelIncrement = v));


		// Characters & HUD
		O.Add(CLuaConfigOption.Toggle_(SYS, secChars,L("SETTINGS_SYSTEM_DISPLAYCHARA"), L("SETTINGS_SYSTEM_DISPLAYCHARA_DESC"), cfg.ShowChara, v => cfg.ShowChara = v));
		O.Add(CLuaConfigOption.Toggle_(SYS, secChars,L("SETTINGS_SYSTEM_DISPLAYDANCER"), L("SETTINGS_SYSTEM_DISPLAYDANCER_DESC"), cfg.ShowDancer, v => cfg.ShowDancer = v));
		O.Add(CLuaConfigOption.Toggle_(SYS, secChars,L("SETTINGS_SYSTEM_DISPLAYMOB"), L("SETTINGS_SYSTEM_DISPLAYMOB_DESC"), cfg.ShowMob, v => cfg.ShowMob = v));
		O.Add(CLuaConfigOption.Toggle_(SYS, secChars,L("SETTINGS_SYSTEM_DISPLAYRUNNER"), L("SETTINGS_SYSTEM_DISPLAYRUNNER_DESC"), cfg.ShowRunner, v => cfg.ShowRunner = v));
		O.Add(CLuaConfigOption.Toggle_(SYS, secChars,L("SETTINGS_SYSTEM_DISPLAYFOOTER"), L("SETTINGS_SYSTEM_DISPLAYFOOTER_DESC"), cfg.ShowFooter, v => cfg.ShowFooter = v));
		O.Add(CLuaConfigOption.Toggle_(SYS, secChars,L("SETTINGS_SYSTEM_DISPLAYPUCHI"), L("SETTINGS_SYSTEM_DISPLAYPUCHI_DESC"), cfg.ShowPuchiChara, v => cfg.ShowPuchiChara = v));

		// Integrations & Advanced
		O.Add(CLuaConfigOption.Toggle_(SYS, secIntegr,L("SETTINGS_SYSTEM_DISCORDRPC"), L("SETTINGS_SYSTEM_DISCORDRPC_DESC"), cfg.SendDiscordPlayingInformation, v => cfg.SendDiscordPlayingInformation = v));
		O.Add(CLuaConfigOption.Toggle_(SYS, secIntegr,L("SETTINGS_SYSTEM_LUANETWORKING"), L("SETTINGS_SYSTEM_LUANETWORKING_DESC"), cfg.bAllowLuaNetworkingConnections, v => cfg.bAllowLuaNetworkingConnections = v));
		O.Add(CLuaConfigOption.Toggle_(SYS, secIntegr,L("SETTINGS_SYSTEM_GAMEEVENTBROADCASTING"), L("SETTINGS_SYSTEM_GAMEEVENTBROADCASTING_DESC"), cfg.bEnableGameEventBroadcasting, v => { cfg.bEnableGameEventBroadcasting = v; RefreshBroadcasting(); }));
		O.Add(CLuaConfigOption.IntInput_(SYS, secIntegr, L("SETTINGS_SYSTEM_BROADCASTINGPORT"), L("SETTINGS_SYSTEM_BROADCASTINGPORT_DESC"), cfg.nGameEventBroadcastingPort, 0, 65535, v => { cfg.nGameEventBroadcastingPort = v; RefreshBroadcasting(); }));
		O.Add(CLuaConfigOption.Toggle_(SYS, secIntegr,L("SETTINGS_SYSTEM_AUTOSCREENSHOT"), L("SETTINGS_SYSTEM_AUTOSCREENSHOT_DESC"), cfg.bIsAutoResultCapture, v => cfg.bIsAutoResultCapture = v));
		O.Add(CLuaConfigOption.Toggle_(SYS, secIntegr,L("SETTINGS_SYSTEM_DEBUGMODE"), L("SETTINGS_SYSTEM_DEBUGMODE_DESC"), cfg.bDisplayDebugInfo, v => cfg.bDisplayDebugInfo = v));
		O.Add(CLuaConfigOption.Toggle_(SYS, secIntegr,L("SETTINGS_SYSTEM_LOG"), L("SETTINGS_SYSTEM_LOG_DESC"), cfg.bOutputLogs, v => cfg.bOutputLogs = v));

		// Input
		O.Add(CLuaConfigOption.KeyConfig_(SYS, secInput,L("SETTINGS_KEYASSIGN_SYSTEM"), L("SETTINGS_KEYASSIGN_SYSTEM_DESC"), "System", "system", () => { }));

		// ── GAME ────────────────────────────────────────────────────────────────────
		const string GAME = "Game";
		// Gameplay
		O.Add(CLuaConfigOption.Int_(GAME, secGameplay,L("SETTINGS_SYSTEM_PLAYERCOUNT"), L("SETTINGS_SYSTEM_PLAYERCOUNT_DESC"),
			cfg.nPlayerCount, 1, 5, 1, v => cfg.nPlayerCount = v));
		O.Add(CLuaConfigOption.Choice_(GAME, secGameplay,L("SETTINGS_GAME_DEFAULTDIFF"), L("SETTINGS_GAME_DEFAULTDIFF_DESC"),
			new[] { L("DIFF_EASY"), L("DIFF_NORMAL"), L("DIFF_HARD"), L("DIFF_EX"), L("DIFF_EXTRA"), L("DIFF_EXEXTRA") }, cfg.nDefaultCourse, idx => cfg.nDefaultCourse = idx));
		O.Add(CLuaConfigOption.Choice_(GAME, secGameplay,L("SETTINGS_GAME_SCOREMODE"), L("SETTINGS_GAME_SCOREMODE_DESC"),
			new[] { "TYPE-A", "TYPE-B", "TYPE-C" }, cfg.nScoreMode, idx => cfg.nScoreMode = idx));
		O.Add(CLuaConfigOption.Toggle_(GAME, secGameplay,L("SETTINGS_GAME_SHINUCHI"), L("SETTINGS_GAME_SHINUCHI_DESC"), cfg.ShinuchiMode, v => cfg.ShinuchiMode = v));
		O.Add(CLuaConfigOption.Choice_(GAME, secGameplay,L("SETTINGS_GAME_SURVIVAL"), L("SETTINGS_GAME_SURVIVAL_DESC"),
			new[] { "OFF", "TYPE-A", "TYPE-B" }, (int)cfg.eGameMode, idx => cfg.eGameMode = (EGame)idx));
		O.Add(CLuaConfigOption.Toggle_(GAME, secGameplay,L("SETTINGS_GAME_NORMALGAUGE"), L("SETTINGS_GAME_NORMALGAUGE_DESC"), cfg.bForceNormalGauge, v => cfg.bForceNormalGauge = v));
		O.Add(CLuaConfigOption.Toggle_(GAME, secGameplay,L("SETTINGS_GAME_BIGNOTEJUDGE"), L("SETTINGS_GAME_BIGNOTEJUDGE_DESC"), cfg.bJudgeBigNotes, v => cfg.bJudgeBigNotes = v));
		O.Add(CLuaConfigOption.Toggle_(GAME, secGameplay,L("SETTINGS_GAME_NOTELOCK"), L("SETTINGS_GAME_NOTELOCK_DESC"), cfg.bTight, v => cfg.bTight = v));
		O.Add(CLuaConfigOption.Int_(GAME, secGameplay,L("SETTINGS_GAME_BADCOUNT"), L("SETTINGS_GAME_BADCOUNT_DESC"), cfg.nRisky, 0, 10, 1, v => cfg.nRisky = v));

		// Display & Feedback
		O.Add(CLuaConfigOption.Int_(GAME, secFeedback,L("SETTINGS_GAME_COMBODISPLAY"), L("SETTINGS_GAME_COMBODISPLAY_DESC"), cfg.nMinDisplayedCombo, 1, 99999, 1, v => cfg.nMinDisplayedCombo = v));
		O.Add(CLuaConfigOption.Toggle_(GAME, secFeedback,L("SETTINGS_GAME_SCOREDISPLAY"), L("SETTINGS_GAME_SCOREDISPLAY_DESC"), cfg.bJudgeCountDisplay, v => cfg.bJudgeCountDisplay = v));
		O.Add(CLuaConfigOption.Toggle_(GAME, secFeedback,L("SETTINGS_GAME_BRANCHGUIDE"), L("SETTINGS_GAME_BRANCHGUIDE_DESC"), cfg.bBranchGuide, v => cfg.bBranchGuide = v));
		O.Add(CLuaConfigOption.Choice_(GAME, secFeedback,L("SETTINGS_GAME_BRANCHANIME"), L("SETTINGS_GAME_BRANCHANIME_DESC"),
			new[] { "TYPE-A", "TYPE-B" }, cfg.nBranchAnime, idx => cfg.nBranchAnime = idx));

		// Timing & Input
		O.Add(CLuaConfigOption.Int_(GAME, secTiming,L("SETTINGS_GAME_GLOBALOFFSET"), L("SETTINGS_GAME_GLOBALOFFSET_DESC"), cfg.nGlobalOffsetMs, -9999, 9999, 1, v => cfg.nGlobalOffsetMs = v));
		O.Add(CLuaConfigOption.Action_(GAME, secTiming,L("SETTINGS_GAME_CALIBRATION"), L("SETTINGS_GAME_CALIBRATION_DESC"), hooks.Calibration));
		O.Add(CLuaConfigOption.Int_(GAME, secTiming,L("SETTINGS_GAME_CONTROLLERDEADZONE"), L("SETTINGS_GAME_CONTROLLERDEADZONE_DESC"), cfg.nControllerDeadzone, 10, 90, 1,
			v => { cfg.nControllerDeadzone = v; OpenTaiko.InputManager.Deadzone = v / 100.0f; }));

		// Auto & AI
		O.Add(CLuaConfigOption.Int_(GAME, secAutoAi,L("SETTINGS_GAME_AUTOROLL"), L("SETTINGS_GAME_AUTOROLL_DESC"), cfg.nRollsPerSec, 0, 1000, 1, v => cfg.nRollsPerSec = v));
		O.Add(CLuaConfigOption.Int_(GAME, secAutoAi,L("SETTINGS_GAME_AILEVEL"), L("SETTINGS_GAME_AILEVEL_DESC"), cfg.nDefaultAILevel, 1, 10, 1,
			v => { cfg.nDefaultAILevel = v; for (int i = 0; i < 2; i++) OpenTaiko.NamePlate.tNamePlateRefreshTitles(i); }));

		// Unlockables
		O.Add(CLuaConfigOption.Toggle_(GAME, secUnlock,L("SETTINGS_GAME_IGNORESONGUNLOCKABLES"), L("SETTINGS_GAME_IGNORESONGUNLOCKABLES_DESC"), cfg.bIgnoreSongUnlockables, v => cfg.bIgnoreSongUnlockables = v));

		// Training
		O.Add(CLuaConfigOption.Int_(GAME, secTraining,L("SETTINGS_TRAINING_SKIPCOUNT"), L("SETTINGS_TRAINING_SKIPCOUNT_DESC"), cfg.TokkunSkipMeasures, 1, 99, 1, v => cfg.TokkunSkipMeasures = v));
		O.Add(CLuaConfigOption.Int_(GAME, secTraining,L("SETTINGS_TRAINING_JUMPINTERVAL"), L("SETTINGS_TRAINING_JUMPINTERVAL_DESC"), cfg.TokkunMashInterval, 1, 9999, 1, v => cfg.TokkunMashInterval = v));

		// Input
		O.Add(CLuaConfigOption.KeyConfig_(GAME, secInput,L("SETTINGS_KEYASSIGN_GAME"), L("SETTINGS_KEYASSIGN_GAME_DESC"), "Taiko", "drums", () => { }));
		O.Add(CLuaConfigOption.KeyConfig_(GAME, secInput,L("SETTINGS_KEYASSIGN_TRAINING"), L("SETTINGS_KEYASSIGN_TRAINING_DESC"), "Taiko", "training", () => { }));

		// ── THEME (dynamic, per-skin) ────────────────────────────────────────────────
		BuildThemeOptions(O);

		return m;
	}

	private static void BuildSkinChooser(List<CLuaConfigOption> O, CConfigIni cfg) {
		int ns = OpenTaiko.Skin.strSystemSkinSubfolders?.Length ?? 0;
		int nb = OpenTaiko.Skin.strBoxDefSkinSubfolders?.Length ?? 0;
		var folders = new string[ns + nb];
		for (int i = 0; i < ns; i++) folders[i] = OpenTaiko.Skin.strSystemSkinSubfolders[i];
		for (int i = 0; i < nb; i++) folders[ns + i] = OpenTaiko.Skin.strBoxDefSkinSubfolders[i];
		Array.Sort(folders);
		_skinSubFolders = folders;
		string[] names = CSkin.GetSkinName(folders);
		// Per-skin preview thumbnail (parallel to names/folders, so it lines up with the chooser index). Folders are
		// absolute and already end with a directory separator. We do NOT File.Exists-check here: the Lua preview pane
		// gates drawing on the texture's width, so a skin missing this file simply shows no thumbnail.
		var thumbs = new string[folders.Length];
		for (int i = 0; i < folders.Length; i++)
			thumbs[i] = folders[i] + "Graphics" + Path.DirectorySeparatorChar + "1_Title" + Path.DirectorySeparatorChar + "Background.png";
		int cur = Array.BinarySearch(folders, OpenTaiko.Skin.GetCurrentSkinSubfolderFullName(true));
		if (cur < 0) cur = 0;
		O.Add(CLuaConfigOption.Choice_("System", L("SETTINGS_SECTION_APPEARANCE", "Appearance"), L("SETTINGS_SYSTEM_SKIN"), L("SETTINGS_SYSTEM_SKIN_DESC"),
			names, cur, idx => {
				idx = Math.Clamp(idx, 0, _skinSubFolders.Length - 1);
				cfg.strSystemSkinSubfolderFullName = _skinSubFolders[idx];
				OpenTaiko.Skin.SetCurrentSkinSubfolderFullName(cfg.strSystemSkinSubfolderFullName, true);
			}, thumbs));
	}

	private static void BuildThemeOptions(List<CLuaConfigOption> O) {
		var db = OpenTaiko.Databases?.DBThemeSettings;
		if (db == null) return;
		string secThemeSettings = L("SETTINGS_SECTION_THEME", "Theme Settings");
		long repSaveId = OpenTaiko.SaveFileInstances[0]?.data?.SaveId ?? 0L;

		void Persist(CThemeSettingDef def, string valueStr) {
			if (def.IsSaveScoped) {
				foreach (var sf in OpenTaiko.SaveFileInstances)
					if (sf?.data != null) db.SetSettingForSave(def.Id, sf.data.SaveId, valueStr);
			} else db.SetSetting(def.Id, valueStr);
		}

		foreach (var def in db.Definitions) {
			string label = def.Label.GetString(def.Id);
			string desc = def.Description.GetString("");
			string stored = def.IsSaveScoped ? db.GetSettingForSave(def.Id, repSaveId) : db.GetSetting(def.Id);
			switch (def.Type.ToLowerInvariant()) {
				case "bool":
					O.Add(CLuaConfigOption.Toggle_("Theme", secThemeSettings,label, desc,
						stored == "1" || string.Equals(stored, "true", StringComparison.OrdinalIgnoreCase),
						v => Persist(def, v ? "1" : "0")));
					break;
				case "int":
					O.Add(CLuaConfigOption.Int_("Theme", secThemeSettings,label, desc,
						int.TryParse(stored, out int iv) ? iv : def.DefaultInt, (int)def.Min, (int)def.Max, 1,
						v => Persist(def, v.ToString())));
					break;
				case "double": {
						int scale = 100;
						double dv = double.TryParse(stored, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double d) ? d : def.DefaultDouble;
						O.Add(CLuaConfigOption.Int_("Theme", secThemeSettings,label, desc,
							(int)Math.Round(dv * scale), (int)Math.Round(def.Min * scale), (int)Math.Round(def.Max * scale), 1,
							v => Persist(def, (v / 100.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)),
							o => (o.Value / 100.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
						break;
					}
				case "enum":
					if (def.Options != null && def.Options.Length > 0)
						O.Add(CLuaConfigOption.Choice_("Theme", secThemeSettings,label, desc, def.Options,
							Math.Max(0, Array.IndexOf(def.Options, stored)), idx => Persist(def, def.Options[idx])));
					break;
				// "string" theme settings omitted from the inline UI for now (rare; need a text editor).
			}
		}
	}

	// ── helpers mirrored from CActConfigList ──
	private static string[] AvailableGraphicsDevices() {
		if (OperatingSystem.IsIOS()) return new[] { "Metal (iOS)" };
		if (OperatingSystem.IsWindows()) return new[] { "OpenGL", "DirectX11", "Vulkan" };
		if (OperatingSystem.IsMacOS()) return new[] { "OpenGL", "Metal" };
		if (OperatingSystem.IsLinux()) return new[] { "OpenGL", "Vulkan" };
		return new[] { "OpenGL", "DirectX11", "Vulkan", "Metal" };
	}
	private static string GraphicsName(int i) => OperatingSystem.IsIOS() ? "Metal (iOS)" : (i switch { 0 => "OpenGL", 1 => "DirectX11", 2 => "Vulkan", 3 => "Metal", _ => "OpenGL" });
	private static int GraphicsInt(string s) => s switch { "OpenGL" => 0, "DirectX11" => 1, "Vulkan" => 2, "Metal" => 3, "Metal (iOS)" => 3, _ => 0 };

	private static void RefreshBroadcasting() {
		var cfg = OpenTaiko.ConfigIni;
		OpenTaiko.HttpEventReporter?.StopListening();
		OpenTaiko.HttpEventReporter = new HttpEventReporter("localhost", cfg.nGameEventBroadcastingPort);
		if (cfg.bEnableGameEventBroadcasting) OpenTaiko.HttpEventReporter.StartListening();
	}
}
