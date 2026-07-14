using System.Diagnostics;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using DiscordRPC;
using FDK;
using Silk.NET.Maths;
using SkiaSharp;
using Rectangle = System.Drawing.Rectangle;

namespace OpenTaiko;

internal class OpenTaiko : Game {
	// Properties
	#region [ properties ]
	public static readonly string VERSION = Assembly.GetExecutingAssembly().GetName().Version.ToString();
	public static readonly string AppDisplayThreePartVersion = GetAppDisplayThreePartVersion();
	public static readonly string AppNumericThreePartVersion = GetAppNumericThreePartVersion();

	public static readonly int MAX_PLAYERS = 5;

	private static string GetAppDisplayThreePartVersion() {
		return $"v{GetAppNumericThreePartVersion()}";
	}

	private static string GetAppNumericThreePartVersion() {
		var version = Assembly.GetExecutingAssembly().GetName().Version;

		return $"{version.Major}.{version.Minor}.{version.Build}";
	}

	public static readonly string AppInformationalVersion =
		Assembly
			.GetExecutingAssembly()
			.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
			.Cast<AssemblyInformationalVersionAttribute>()
			.FirstOrDefault()
			?.InformationalVersion
		?? $"{GetAppDisplayThreePartVersion()} (unknown informational version)";

	public static CStage latestSongSelect {
		get;
		private set;
	}

	public static OpenTaiko app {
		get;
		private set;
	}
	public static CTextConsole actTextConsole {
		get;
		private set;
	}
	public static CConfigIni ConfigIni {
		get;
		private set;
	}

	public static CVisualLogManager VisualLogManager {
		get;
		private set;
	}

	/// <summary>
	/// When non-null, the song-loading stage will use this pre-built <see cref="CTja"/> instead of
	/// reading from <c>SongMount.rChosenScore.ファイル情報.ファイルの絶対パス</c>.
	/// Consumed (set back to null) immediately after the handoff so it is used only once.
	/// </summary>
	public static CTja? DanBuilderPrebuiltTja = null;

	#region [DTX instances]
	public static CTja? TJA { // only for P1
		get => tja[0];
		set => SetTJA(0, value);
	}

	public static CTja?[] TJAs
		=> tja.Select(x => x).ToArray();

	public static CTja? GetTJA(int player)
		=> tja.ElementAtOrDefault(player);
	public static void SetTJA(int player, CTja? value) {
		if (!(player >= 0 && player <= tja.Length)) {
			return;
		}
		if ((tja[player] != null) && (app != null)) {
			tja[player].DeActivate();
			tja[player].ReleaseManagedResource();
			tja[player].ReleaseUnmanagedResource();
			app.listTopLevelActivities.Remove(tja[player]);
		}
		tja[player] = value;
		if ((tja[player] != null) && (app != null)) {
			app.listTopLevelActivities.Add(tja[player]);
		}
	}

	#endregion

	public static CSongReplay[] ReplayInstances = new CSongReplay[5];

	// ── replay playback ──
	// note-shuffle seed used for the current play, per player; written into the saved replay and re-applied when watching
	public static int[] ReplaySeed = new int[5];
	// while true, that player's hits come from a recorded replay instead of live input (and the auto modicon is shown)
	public static bool[] bReplayMode = new bool[5];
	// the replay being played back for each player (set when bReplayMode is true)
	public static CSongReplay[] ReplayPlayback = new CSongReplay[5];
	// set by REPLAY:Watch from song select; the gameplay stage consumes it on activation to enter replay mode
	public static bool ReplayWatchArmed = false;
	public static CSongReplay PendingReplay = null;

	public static CFPS FPS {
		get;
		private set;
	}
	public static CFPS FPSInput {
		get;
		private set;
	}
	public static CInputManager InputManager {
		get;
		private set;
	}
	/// <summary>
	/// iOS: set external input devices (e.g. the iOS keyboard) before calling InitWithExternalContext().
	/// </summary>
	internal static List<IInputDevice> ExternalInputDevices { get; set; }

	public static CPad Pad {
		get;
		private set;
	}
	public static Random Random {
		get;
		private set;
	}
	public static CSkin Skin {
		get;
		private set;
	}
	public static CSongManager SongManager {
		get;
		set;    // 2012.1.26 yyagi private解除 CStage起動でのdesirialize読み込みのため
	}
	public static CEnumSongs EnumSongs {
		get;
		private set;
	}
	public static CActEnumSongs actEnumSongs {
		get;
		private set;
	}
	public static CActScanningLoudness actScanningLoudness {
		get;
		private set;
	}

	public static SoundManager SoundManager {
		get;
		private set;
	}

	public static SongGainController SongGainController {
		get;
		private set;
	}

	public static SoundGroupLevelController SoundGroupLevelController {
		get;
		private set;
	}

	public static CNamePlate NamePlate {
		get;
		private set;
	}

	public static NamePlateConfig NamePlateConfig {
		get;
		private set;
	}

	public static Favorites Favorites {
		get;
		private set;
	}

	public static RecentlyPlayedSongs RecentlyPlayedSongs {
		get;
		private set;
	}

	public static Databases Databases {
		get;
		private set;
	}

	public static CSystemError SystemError {
		get;
		private set;
	}
	public static CStageStartup stageStartup {
		get;
		private set;
	}
	public static CStageConfig stageConfig {
		get;
		private set;
	}
	public static CSongMount SongMount {
		get;
		private set;
	}

	public static CStageHeya stageHeya {
		get;
		private set;
	}

	public static CStageOnlineLounge stageOnlineLounge {
		get;
		private set;
	}

	public static CStageCutScene stageCutScene {
		get;
		private set;
	}

	public static CStageSongLoading stageSongLoading {
		get;
		private set;
	}
	public static CStagePlayDrumsScreen stageGameScreen {
		get;
		private set;
	}
	public static CStageResult stageResults {
		get;
		private set;
	}
	public static CStageChangeSkin stageChangeSkin {
		get;
		private set;
	}
	public static CStageTransition stageTransition {
		get;
		private set;
	}
	public static CStageShutdown stageExit {
		get;
		private set;
	}
	public static CStage rCurrentStage = null;
	public static CStage rPreviousStage = null;
	public static string strEXEFolder {
		get;
		private set;
	} = Environment.CurrentDirectory + Path.DirectorySeparatorChar;

	// Read assets straight from the app bundle to avoid copying all game data into Documents (keeps app size + first-launch time down).
	/// <summary>
	/// iOS only: read-only app bundle path. Null on other platforms.
	/// Used as a fallback for assets not copied to Documents (Global/, Lang/, etc.).
	/// </summary>
	public static string strBundleFolder {
		get;
		set;
	}

	/// <summary>
	/// Resolve a path for reading. On iOS, if the path doesn't exist under the
	/// Documents directory, falls back to the equivalent path under the app bundle.
	/// </summary>
	public static string ResolveAssetPath(string path) {
		if (strBundleFolder != null && !File.Exists(path) && !Directory.Exists(path)) {
			string relative;
			if (path.StartsWith(strEXEFolder)) {
				// Absolute path under Documents — try equivalent under bundle
				relative = path.Substring(strEXEFolder.Length);
			} else if (!Path.IsPathRooted(path)) {
				// Relative path (resolved against CWD=Documents) — try under bundle
				relative = path;
			} else {
				return path;
			}
			string bundlePath = strBundleFolder + relative;
			if (File.Exists(bundlePath) || Directory.Exists(bundlePath))
				return bundlePath;
		}
		return path;
	}

	// Writable mirror (under Documents) for files whose natural path is inside the read-only app bundle.
	private const string strBundleWritableMirror = "UserData";

	/// <summary>
	/// Resolve a path for WRITING. On iOS the app bundle is read-only, so a path inside it is remapped to a
	/// dedicated writable subtree of Documents (UserData). It is kept out of the skin's own relative path so a
	/// partial write does not shadow the complete bundle skin. Other platforms return the path unchanged.
	/// </summary>
	public static string ResolveWritePath(string path) {
		if (strBundleFolder != null && path.StartsWith(strBundleFolder)) {
			return System.IO.Path.Combine(strEXEFolder, strBundleWritableMirror, path.Substring(strBundleFolder.Length));
		}
		return path;
	}

	/// <summary>
	/// iOS dev/testing flag: when true, the iOS host pulses a Don (confirm) input every few frames so the
	/// app auto-walks through first-time setup / title / song-select without manual taps.
	/// MUST be false for release.
	/// </summary>
	public static bool iOSAutoAdvanceUI = false;

	/// <summary>
	/// On iOS, merge subdirectories from both Documents and bundle for a given path.
	/// Documents entries take priority (user overrides). On other platforms, just
	/// returns Directory.GetDirectories(path).
	/// </summary>
	public static string[] GetMergedDirectories(string path, string searchPattern = "*") {
		if (strBundleFolder == null)
			return Directory.Exists(path) ? Directory.GetDirectories(path, searchPattern) : Array.Empty<string>();

		string relative;
		if (path.StartsWith(strEXEFolder))
			relative = path.Substring(strEXEFolder.Length);
		else if (!Path.IsPathRooted(path))
			relative = path;
		else
			return Directory.Exists(path) ? Directory.GetDirectories(path, searchPattern) : Array.Empty<string>();

		string bundleDir = strBundleFolder + relative;

		// Collect subdirectory names from both locations, Documents wins on overlap
		var dirs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (Directory.Exists(bundleDir)) {
			foreach (string d in Directory.GetDirectories(bundleDir, searchPattern))
				dirs[Path.GetFileName(d)] = d;
		}
		if (Directory.Exists(path)) {
			foreach (string d in Directory.GetDirectories(path, searchPattern))
				dirs[Path.GetFileName(d)] = d; // overrides bundle entry
		}
		return dirs.Values.ToArray();
	}

	public static CTimer Timer {
		get;
		private set;
	}
	public bool bSwitchVSyncAtTheNextFrame {
		get;
		set;
	}
	public bool bSwitchFullScreenAtNextFrame {
		get;
		set;
	}
	public static DiscordRpcClient DiscordClient;

	public static SaveFile[] SaveFileInstances = new SaveFile[5];

	public static SaveFile PrimarySaveFile => SaveFileInstances[0];

	// 0 : Hidari, 1 : Migi (1P only)
	public static int PlayerSide = 0;

	// Modal manager
	public static CModalManager ModalManager {
		get;
		private set;
	}

	// Pause-menu popup manager (drives the popup_menu ROActivity; replaces the old CActSelectPopupMenu rendering)
	public static CPopupMenuManager PopupMenuManager {
		get;
		private set;
	}

	// Unlockables factory
	public static CUnlockConditionFactory UnlockConditionFactory {
		get;
		private set;
	}

	public static LuaGlobalStores GlobalStores {
		get;
		private set;
	}

	public static bool P1IsBlue() {
		return (OpenTaiko.PlayerSide == 1 && OpenTaiko.ConfigIni.nPlayerCount == 1);
	}

	#endregion

	public static HttpEventReporter? HttpEventReporter {
		get;
		set;
	}

	// Constructor

	public OpenTaiko(params string[] args) : base("OpenTaiko.ico", args) {
		OpenTaiko.app = this;
	}

	public static string sEncType = "Shift_JIS";

	public static string LargeImageKey {
		get {
			return "opentaiko";
		}
	}

	public static string LargeImageText {
		get {
			return "Ver." + Assembly.GetExecutingAssembly().GetName().Version.ToString() + "(" + RuntimeInformation.RuntimeIdentifier + ")";
		}
	}

	public static CCounter BeatScaling;

	/// <summary>
	/// Returns true for this session if the game fails to locate Config.ini.<br/>
	/// This could be treated as the player's first time launching the game.
	/// </summary>
	public static bool ConfigIsNew;


	// メソッド

	public void MountActivity(CActivity? Stage) {
		if (Stage == null)
			return;
		// Guarantee the stage's resources exist BEFORE Activate(), idempotently: activities use them inside
		// their own Activate() (e.g. fonts), so creating them afterward NREs.
		if (!Stage.bManagedResourceCreated)
			Stage.CreateManagedResource();
		if (!Stage.bUnmanagedResourceCreated)
			Stage.CreateUnmanagedResource();
		Stage.Activate();
	}

	public void UnmountActivity(CActivity? Stage) {
		if (Stage == null)
			return;
		Stage.DeActivate();
		if (!ConfigIni.PreAssetsLoading) {
			Stage.ReleaseManagedResource();
			Stage.ReleaseUnmanagedResource();
		}
	}

	public void ChangeStage(CStage Stage, string? traceMessage = null) {
		MountActivity(Stage);
		if (traceMessage != null) {
			Trace.TraceInformation("----------------------");
			Trace.TraceInformation($"■ {traceMessage}");
		}
		rPreviousStage = rCurrentStage;
		rCurrentStage = Stage;
	}

	public void UnmountAndChangeStage(CStage Stage, string? traceMessage = null) {
		// A Lua Exit(...) requested a transition: hand the switch to CStageTransition (it renders the still-
		// mounted outgoing stage during fade-out, activates the target behind a loading screen, then fades in).
		var pending = CStageTransition.ConsumePendingScript();
		if (rCurrentStage == pending?.stage && Stage != null) {
			stageTransition.Begin(rCurrentStage, Stage, CStageTransition.ActivateStep(Stage), default, pending.Value.script, traceMessage);
			rPreviousStage = rCurrentStage;
			rCurrentStage = stageTransition;   // outgoing stays mounted; the transition unmounts it after fade-out
			return;
		}

		UnmountActivity(rCurrentStage);
		this.ChangeStage(Stage, traceMessage);
	}

	// Enter the song load. With a transition module available, run it as ONE transition (fade the outgoing
	// stage out → drive CStageSongLoading [its screen + bar] → fade the loaded game screen in), so there's no
	// abrupt cut. Otherwise fall back to the legacy song-loading stage. `outgoing` is the still-mounted stage
	// being left (song select, or the cutscene). ESC during the load returns to the last song select.
	public void EnterSongLoad(CStage outgoing) {
		var slScript = LuaTransitionWrapper.Get("song_loading");
		if (slScript != null) {
			CStageTransition.ClearPendingScript();   // play path always uses the song_loading transition
			stageTransition.Begin(outgoing, stageGameScreen, new SongLoadStep(stageSongLoading),
				new TransitionOptions { NoAssetPhase = true, LoaderDrivesBar = true, RevealsGameplay = true,
				                        CancelTarget = latestSongSelect ?? outgoing },
				slScript, "Song Loading");
			rPreviousStage = rCurrentStage;
			rCurrentStage = stageTransition;
		} else {
			UnmountAndChangeStage(stageSongLoading, "Song Loading");
		}
	}

	public void UnmountAndChangeLuaStageOrError(string name, string? traceMessage = null, CSystemError.Errno errno = CSystemError.Errno.ENO_INVALIDSTAGENAME) {
		LuaStageWrapper.ForceSetNextRequestedStage(name);
		LuaStageWrapper? _stage = LuaStageWrapper.GetNextRequestedStage();
		if (_stage != null) {
			UnmountAndChangeStage(_stage, traceMessage ?? $"Lua Stage: {name}");
		} else {
			TriggerSystemError(errno);
		}
	}

	public void TriggerSystemError(CSystemError.Errno errno, Exception? exception = null, string? message = null) {
		CStageTransition.ClearPendingScript();   // don't carry a pending transition into the error stage
		if (exception != null)
			Trace.TraceError(exception.ToString());
		if (message != null)
			Trace.TraceError(message);
		SystemError.LoadError(errno, exception, message);
		UnmountAndChangeStage(SystemError);
	}

	public void EnterRefreshSkinStage(bool isSavedBeforeUpdate = false) {
		stageChangeSkin.SavePreviousStage(isSavedBeforeUpdate ? rCurrentStage : rPreviousStage);
	}



	#region [ #24609 リザルト画像をpngで保存する ]		// #24609 2011.3.14 yyagi; to save result screen in case BestRank or HiSkill.
	/// <summary>
	/// リザルト画像のキャプチャと保存。
	/// </summary>
	/// <param name="strFilename">保存するファイル名(フルパス)</param>
	public bool SaveResultScreen(string strFullPath) {
		bool success = true;

		void save(SKBitmap sKBitmap) {
			string strSavePath = Path.GetDirectoryName(strFullPath);
			if (!Directory.Exists(strSavePath)) {
				try {
					Directory.CreateDirectory(strSavePath);
				} catch {
					Trace.TraceError(ToString());
					Trace.TraceError("例外が発生しましたが処理を継続します。 (0bfe6bff-2a56-4df4-9333-2df26d9b765b)");
					success = false;
				}
			}
			if (!File.Exists(strFullPath)) {
				using FileStream stream = File.OpenWrite(strFullPath);
				sKBitmap.Encode(stream, SKEncodedImageFormat.Png, 80);
			}
		}

		GetScreenShotAsync(save);

		return success;
	}
	#endregion

	// Game 実装


	protected override void Configuration() {
		if (OperatingSystem.IsIOS()) {
			// iOS: Documents is writable; the bundle ships read-only defaults.
			// Writable files (Config.ini, databases, etc.) live in Documents.
			// Customizable trees (Global/, System/, Lang/) merge bundle defaults with Documents additions:
			// listings via GetMergedDirectories(), single assets via ResolveAssetPath() (Documents override, bundle fallback).
			strEXEFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + Path.DirectorySeparatorChar;
			// strBundleFolder is set by GameViewController before game launch
			// Set CWD so relative paths (Favorite.json, etc.) resolve to writable Documents dir
			Directory.SetCurrentDirectory(strEXEFolder);
		} else {
			strEXEFolder = Environment.CurrentDirectory + Path.DirectorySeparatorChar;
		}

		ConfigIni = new CConfigIni();

		string path = strEXEFolder + "Config.ini";
		if (File.Exists(path)) {
			try {
				// Load config info
				ConfigIni.LoadFromFile(path);
			} catch (Exception e) {
				Trace.TraceError(e.ToString());
				Trace.TraceError("例外が発生しましたが処理を継続します。 (b8d93255-bbe4-4ca3-8264-7ee5175b19f3)");
			}
		} else {
			ConfigIsNew = true;
		}

		if (ConfigIsNew) {
			GraphicsDeviceType_ = AnglePlatformType.OpenGL;

			if (OperatingSystem.IsWindows()) {
				GraphicsDeviceType_ = AnglePlatformType.OpenGL;
				ConfigIni.nGraphicsDeviceType = 0;
			}
			// While we aren't able to support MacOS, this check is included just in case this changes.
			else if (OperatingSystem.IsMacOS()) {
				GraphicsDeviceType_ = AnglePlatformType.Metal;
				ConfigIni.nGraphicsDeviceType = 3;
			} else if (OperatingSystem.IsLinux()) {
				GraphicsDeviceType_ = AnglePlatformType.Vulkan;
				ConfigIni.nGraphicsDeviceType = 2;
			}
		} else {
			switch (ConfigIni.nGraphicsDeviceType) {
				case 0:
					GraphicsDeviceType_ = AnglePlatformType.OpenGL;
					break;
				case 1:
					GraphicsDeviceType_ = AnglePlatformType.D3D11;
					break;
				case 2:
					GraphicsDeviceType_ = AnglePlatformType.Vulkan;
					break;
				case 3:
					GraphicsDeviceType_ = AnglePlatformType.Metal;
					break;
			}
		}

		if (VideoExporter.Active) VideoExporter.ApplyBootOverrides(this);

		if (!(OperatingSystem.IsIOS() || OperatingSystem.IsAndroid())) {
			WindowPosition = new Silk.NET.Maths.Vector2D<int>(ConfigIni.nWindowBaseXPosition, ConfigIni.nWindowBaseYPosition);
			WindowSize = new Silk.NET.Maths.Vector2D<int>(ConfigIni.nWindowWidth, ConfigIni.nWindowHeight);
			WindowMode = ConfigIni.nWindowMode;
		}
		VSync = ConfigIni.bEnableVSync;
		Framerate = 0;

		base.Configuration();
	}

	protected override void InitializeLog() {
		base.InitializeLog();
		tStartupLog();
	}

	protected override void Initialize() {
		this.tStartupProcess();
	}

	protected override void LoadContent() {
		if (ConfigIni.bWindowMode) {
			if (!this.bMouseCursorDisplaying) {
				this.bMouseCursorDisplaying = true;
			}
		} else if (this.bMouseCursorDisplaying) {
			this.bMouseCursorDisplaying = false;
		}

		if (this.listTopLevelActivities != null) {
			foreach (CActivity activity in this.listTopLevelActivities)
				activity.CreateUnmanagedResource();
		}
	}
	protected override void UnloadContent() {
		if (this.listTopLevelActivities != null) {
			foreach (CActivity activity in this.listTopLevelActivities)
				activity.ReleaseUnmanagedResource();
		}
	}
	protected override void OnExiting() {
		this.tExitProcess();
		base.OnExiting();
	}
	protected override void Events() {
		base.Events();
		FPSInput?.Update();
	}
	protected override void Update() {
		InputManager?.Polling();
		FPSInput?.Update(); // events polled before Update() is called
	}

	protected override void Draw() {
#if !DEBUG
		try
#endif
		{
			Timer?.Update();
			SoundManager.PlayTimer?.Update();
			FPS?.Update();

			// #xxxxx 2013.4.8 yyagi; sleepの挿入位置を、EndScnene～Present間から、BeginScene前に移動。描画遅延を小さくするため。

			if (rCurrentStage != null) {
				// set up camera before drawing main UI
				if (!ConfigIni.bTokkunMode && rCurrentStage?.eStageID != CStage.EStage.CRASH) {
					float screen_ratiox = OpenTaiko.Skin != null ? OpenTaiko.Skin.Resolution[0] / 1280.0f : 1.0f;
					float screen_ratioy = OpenTaiko.Skin != null ? OpenTaiko.Skin.Resolution[1] / 720.0f : 1.0f;

					Camera = Matrix4X4<float>.Identity;

					Camera *= Matrix4X4.CreateScale(fCamXScale, fCamYScale, 1f);

					Camera *= Matrix4X4.CreateScale(1.0f * ScreenAspect, 1.0f, 1.0f) *
							  Matrix4X4.CreateRotationZ(CConversion.DegreeToRadian(fCamRotation)) *
							  Matrix4X4.CreateScale(1.0f / ScreenAspect, 1.0f, 1.0f);

					Camera *= Matrix4X4.CreateTranslation(fCamXOffset / 1280, fCamYOffset / 720, 1f);

					if (BeatScaling != null) {
						BeatScaling.Tick();
						float value = MathF.Sin((BeatScaling.CurrentValue / 1000.0f) * MathF.PI / 2.0f);
						float scale = 1.0f + ((1.0f - value) / 40.0f);
						Camera *= Matrix4X4.CreateScale(scale, scale, 1.0f);
						if (BeatScaling.CurrentValue == BeatScaling.EndValue) BeatScaling = null;
					}
				}

				if (OperatingSystem.IsIOS() && _iosStageDebugCounter++ % 300 == 0) {
					Console.WriteLine($"[OpenTaiko] Stage: {rCurrentStage.eStageID}{(string.IsNullOrEmpty(rCurrentStage.customStageName) ? "" : $" ({rCurrentStage.customStageName})")}, Phase: {rCurrentStage.ePhaseID}");
				}
				OpenTaiko.NamePlate?.Update();
				this.nDrawLoopReturnValue = (rCurrentStage != null) ? rCurrentStage.Draw() : 0;

				if (VideoExporter.Active)
					this.nDrawLoopReturnValue = VideoExporter.Tick(this, this.nDrawLoopReturnValue);

				// Chart-object overlay. During normal gameplay the play screen draws these itself (correctly
				// layered UNDER its fade-out), so we only draw them here for:
				//  (a) the song-loading → gameplay reveal, where the play screen isn't the current stage yet but
				//      the objects should show through the fading loading screen, and
				//  (b) Tokkun/training mode, where the play screen skips its own object draw.
				// This keeps the objects from lingering over the clear/result screens after the play ends, and
				// lets the play's fade cover them instead of them punching through on top.
				if (OpenTaiko.TJA != null &&
					((rCurrentStage?.eStageID == CStage.EStage.Transition && stageTransition.RevealingGameplay)
					 || (rCurrentStage?.eStageID == CStage.EStage.Game && OpenTaiko.ConfigIni.bTokkunMode))) {
					//object rendering
					foreach (KeyValuePair<string, CSongObject> pair in OpenTaiko.TJA.listObj) {
						pair.Value.tDraw();
					}
				}

				// draw the remaining elements normally
				Camera = Matrix4X4<float>.Identity;

				CScoreIni scoreIni = null;

				#region [ Enumerate Songs thread ]

				actEnumSongs?.Draw();                            // "Enumerating Songs..." icon


				switch (rCurrentStage.eStageID) {
					case CStage.EStage.Config:
					case CStage.EStage.SongSelect:
					case CStage.EStage.SongLoading:
					case CStage.EStage.Heya:
					case CStage.EStage.CUSTOM:
						if (EnumSongs != null) {
							#region [ Start the thread for song enumaration ]
							if (rCurrentStage.eStageID == CStage.EStage.CUSTOM &&
								this.nDrawLoopReturnValue == (int)EReturnValue.Continuation &&
								!EnumSongs.IsSongListEnumStarted) {
								MountActivity(actEnumSongs);
								EnumSongs.Init();   // 取得した曲数を、新インスタンスにも与える
								EnumSongs.StartEnumFromDisk();      // 曲検索スレッドの起動_開始
							}
							#endregion

							#region [ 曲検索の中断と再開 ]
							if (rCurrentStage.eStageID == CStage.EStage.SongSelect && !EnumSongs.IsSongListEnumCompletelyDone) {
								switch (this.nDrawLoopReturnValue) {
									case 0:     // 何もない
										EnumSongs.Resume();
										EnumSongs.IsSlowdown = false;
										MountActivity(actEnumSongs);
										break;

									case 2:     // 曲決定
										EnumSongs.Suspend();                        // #27060 バックグラウンドの曲検索を一時停止
										UnmountActivity(actEnumSongs);
										break;
								}
							}
							#endregion

							#region [ 曲探索中断待ち待機 ]
							if (rCurrentStage.eStageID == CStage.EStage.SongLoading && !EnumSongs.IsSongListEnumCompletelyDone &&
								EnumSongs.thDTXFileEnumerate != null)                           // #28700 2012.6.12 yyagi; at Compact mode, enumerating thread does not exist.
							{
								EnumSongs.WaitUntilSuspended();                                 // 念のため、曲検索が一時中断されるまで待機
							}
							#endregion

							#region [ 曲検索が完了したら、実際の曲リストに反映する ]
							// CStage選曲.On活性化() に回した方がいいかな？
							if (EnumSongs.state is CEnumSongs.DTXEnumState.Enumeratad or CEnumSongs.DTXEnumState.Canceled) {
								UnmountActivity(actEnumSongs);

								if (EnumSongs.IsSongListEnumerated) {
								OpenTaiko.SongManager = EnumSongs.SongManager;
								EnumSongs.SongListEnumCompletelyDone();

								// Propagate AfterSongEnum events to all lua stages
								LuaStageWrapper.PropagateAfterSongEnumEvent();
								LuaActivityWrapper.PropagateAfterSongEnumEvent();
							}
							}
							#endregion
						}
						break;
				}
				#endregion

			handleDrawLoopReturnValue:
				switch (rCurrentStage.eStageID) {
					case CStage.EStage.None:
						break;

					case CStage.EStage.CRASH:
						break;

					case CStage.EStage.StartUp:
						if (this.nDrawLoopReturnValue != 0) {
							UnmountAndChangeLuaStageOrError("_boot", "Boot", CSystemError.Errno.ENO_BOOTNOTFOUND);
							this.tExecuteGarbageCollection();
						}
						break;

					case CStage.EStage.Transition:
						// Transition finished. Normal: take over the target it already mounted (rPreviousStage
						// stays the stage we came from). Cancelled song load (ESC): tear down the chart + go back
						// to song select.
						if (this.nDrawLoopReturnValue != 0) {
							if (stageTransition.Canceled) {
								CStage? cancelTo = stageTransition.CancelTarget;
								stageTransition.Finish();
								OpenTaiko.Pad.detectedDevice.Clear();
								if (TJA != null) {
									TJA.DeActivate();
									TJA.ReleaseManagedResource();
									TJA.ReleaseUnmanagedResource();
								}
								SongMount.bIsAfterSongJump = false;
								UnmountAndChangeStage(cancelTo ?? latestSongSelect, "Return to song select menu");
								this.tExecuteGarbageCollection();
							} else {
								CStage? _target = stageTransition.Target;
								stageTransition.Finish();
								rCurrentStage = _target ?? rCurrentStage;
								// forward target's return value
								if (stageTransition.TargetDrawLoopReturnValue != null && rCurrentStage != stageTransition) {
									this.nDrawLoopReturnValue = stageTransition.TargetDrawLoopReturnValue.Value;
									goto handleDrawLoopReturnValue;
								}
							}
						}
						break;

					case CStage.EStage.Config:
						#region [ *** ]
						//-----------------------------
						if (this.nDrawLoopReturnValue != 0) {
							// update target stage
							switch (rPreviousStage?.eStageID) {
								default:
								case CStage.EStage.CUSTOM:
									UnmountAndChangeLuaStageOrError("_title", "Title", CSystemError.Errno.ENO_TITLENOTFOUND);
									this.tExecuteGarbageCollection();
									break;

							}
							if (stageChangeSkin.IsPreviousStageSaved) { // change skin
								UnmountAndChangeStage(stageChangeSkin);
							}
							return;
						}
						//-----------------------------
						#endregion
						break;

					case CStage.EStage.Heya:
						#region [ *** ]
						switch (this.nDrawLoopReturnValue) {
							case (int)EReturnValue.BackToTitle:
								#region [ *** ]
								//-----------------------------
								UnmountAndChangeLuaStageOrError("_title", "Title", CSystemError.Errno.ENO_TITLENOTFOUND);

								this.tExecuteGarbageCollection();
								break;
								//-----------------------------
								#endregion
						}
						#endregion
						break;

					case CStage.EStage.CutScene:
						#region [ *** ]
						switch (this.nDrawLoopReturnValue) {
							case (int)CStageCutScene.EReturnValue.IntroFinished:
								EnterSongLoad(rCurrentStage);   // song_loading transition, or legacy stage fallback
								this.tExecuteGarbageCollection();
								break;

							case (int)CStageCutScene.EReturnValue.OutroFinishedFadeOut:
								UnmountActivity(rCurrentStage);
								this.NextSongSelectStage(OpenTaiko.stageResults);

								this.tExecuteGarbageCollection();
								break;
						}
						#endregion
						break;

					case CStage.EStage.SongLoading:
						#region [ *** ]
						//-----------------------------
						if (this.nDrawLoopReturnValue != 0) {
							OpenTaiko.Pad.detectedDevice.Clear();
							#region [ If ESC is pressed, cancel the loading and go back to song select ]
							if (this.nDrawLoopReturnValue == (int)ESongLoadingScreenReturnValue.LoadCanceled) {
								UnmountActivity(rCurrentStage);

								if (TJA != null) {
									TJA.DeActivate();
									TJA.ReleaseManagedResource();
									TJA.ReleaseUnmanagedResource();
								}

								SongMount.bIsAfterSongJump = false;
								UnmountAndChangeStage(OpenTaiko.latestSongSelect, "Return to song select menu");
								break;
							}
							#endregion

							Trace.TraceInformation("----------------------");
							Trace.TraceInformation("■ Gameplay (Drum Screen)");
							this.tExecuteGarbageCollection();

							// Gameplay was already activated during the song load. Hand off via a reveal transition
							// (fade the loading screen out + the game screen in, both clock-driven so audio/notes stay
							// in sync) instead of the abrupt cut. The chart-object overlay is held during the fade
							// (see the EStage.Transition guard above). No transition module ⇒ legacy direct swap.
							var revealScript = LuaTransitionWrapper.Get(null);
							if (revealScript != null) {
								stageTransition.Begin(rCurrentStage, stageGameScreen, null, default, revealScript, "Gameplay (Drum Screen)");
								rPreviousStage = rCurrentStage;
								rCurrentStage = stageTransition;
							} else {
								UnmountActivity(rCurrentStage);
								rPreviousStage = rCurrentStage;
								rCurrentStage = stageGameScreen;
							}
						}
						//-----------------------------
						#endregion
						break;

					case CStage.EStage.Game:
						#region [ *** ]

						switch (this.nDrawLoopReturnValue) {
							case (int)EGameplayScreenReturnValue.ReloadAndReplay:
								#region [ Restart play ]
								TJA.tStopAllChips();
								TJA.DeActivate();
								TJA.ReleaseManagedResource();
								TJA.ReleaseUnmanagedResource();
								UnmountAndChangeStage(stageSongLoading);
								this.tExecuteGarbageCollection();
								break;
							#endregion


							case (int)EGameplayScreenReturnValue.Continue:
								break;

							case (int)EGameplayScreenReturnValue.PerformanceInterrupted:
								#region [ Play cancelled ]
								//-----------------------------

								TJA.tStopAllChips();
								TJA.DeActivate();
								TJA.ReleaseManagedResource();
								TJA.ReleaseUnmanagedResource();
								SongMount.bIsAfterSongJump = false;
								UnmountAndChangeStage(OpenTaiko.latestSongSelect, "Return to song select menu");

								this.tExecuteGarbageCollection();
								break;
							//-----------------------------
							#endregion

							case (int)EGameplayScreenReturnValue.StageFailed:
								#region [ Stage failed (skip results) ]
								//-----------------------------

								TJA.tStopAllChips();
								TJA.DeActivate();
								TJA.ReleaseManagedResource();
								TJA.ReleaseUnmanagedResource();
								SongMount.bIsAfterSongJump = false;
								UnmountAndChangeStage(OpenTaiko.latestSongSelect, "Return to song select menu");
								this.tExecuteGarbageCollection();
								break;
							//-----------------------------
							#endregion

							case (int)EGameplayScreenReturnValue.StageCleared:
								#region [ Stage completed (go to results) ]
								//-----------------------------

								// Fetch the results of the finished play
								stageGameScreen.bPreviousPlayWasEndedNormally = true;
								CScoreIni.CPlayRecord cPlayRecord_Drums;
								stageGameScreen.tPlayResultStore(out cPlayRecord_Drums);
								stageResults.stPlayRecord = cPlayRecord_Drums;
								UnmountAndChangeStage(stageResults, "Results");
								break;
								//-----------------------------
								#endregion

							case (int)EGameplayScreenReturnValue.SongJump:
								#region [ Song jump (skip results, load new song) ]
								//-----------------------------
								SongMount.bSongJumpPending = false;
								TJA?.tStopAllChipsAndRemoveFromMixer();
								TJA?.DeActivate();
								TJA?.ReleaseManagedResource();
								TJA?.ReleaseUnmanagedResource();
								UnmountAndChangeStage(stageSongLoading, "Song Loading");
								this.tExecuteGarbageCollection();
								break;
								//-----------------------------
								#endregion
						}
						//-----------------------------
						#endregion
						break;

					case CStage.EStage.Results:
						#region [ *** ]
						//-----------------------------
						if (this.nDrawLoopReturnValue != 0) {
							//DTX.t全チップの再生一時停止();
							TJA.tStopAllChipsAndRemoveFromMixer();
							TJA.DeActivate();
							TJA.ReleaseManagedResource();
							TJA.ReleaseUnmanagedResource();
							UnmountActivity(rCurrentStage);
							this.tExecuteGarbageCollection();

							// Online VS: no outro cutscene — every player would otherwise sit through it out of sync.
							bool _onlineNoCut = LuaNetworking.Active?.PlaySyncActive == true;
							if (!_onlineNoCut && stageCutScene.LoadCutScenes(rCurrentStage)) {
								//-----------------------------
								this.ChangeStage(stageCutScene, "Cut Scene");
							} else {
								SongMount.bIsAfterSongJump = false;
								this.NextSongSelectStage(rCurrentStage);
							}
						}
						//-----------------------------
						#endregion
						break;


					case CStage.EStage.ChangeSkin:
						#region [ *** ]
						//-----------------------------
						if (this.nDrawLoopReturnValue != 0) {
							// After a skin (re)load, enter the new skin's _boot stage (same entry point as the
							// initial startup) instead of silently reverting to the previous screen.
							UnmountAndChangeLuaStageOrError("_boot", "Boot", CSystemError.Errno.ENO_BOOTNOTFOUND);
							this.tExecuteGarbageCollection();
						}
						//-----------------------------
						#endregion
						break;

					case CStage.EStage.CUSTOM:
						#region [ Lua Stages ]
						switch (this.nDrawLoopReturnValue) {
							case (int)EReturnValue.BackToTitle:
								#region [ Back to title screen ]
								//-----------------------------
								UnmountAndChangeLuaStageOrError("_title", "Title", CSystemError.Errno.ENO_TITLENOTFOUND);

								this.tExecuteGarbageCollection();
								break;
							//-----------------------------
							#endregion

							case (int)EReturnValue.SongSelected:
								#region [ Song selected ]
								//-----------------------------
								// Online VS: skip intro cutscenes — the lobby already bracketed the play round
								// (PlaySyncActive), and a cutscene would desync every player's start.
								// Watching a replay: skip the intro cutscene entirely — don't play it and don't run
								// LoadCutScenes (which would trip the cutscene's "first met" conditions).
								bool playCutScenes = LuaNetworking.Active?.PlaySyncActive != true
									&& !OpenTaiko.ReplayWatchArmed
									&& stageCutScene.LoadCutScenes(rCurrentStage, true);
								latestSongSelect = rCurrentStage;
								if (playCutScenes) {
									UnmountAndChangeStage(stageCutScene, "Cut Scene");
								} else {
									EnterSongLoad(rCurrentStage);   // song_loading transition, or legacy stage fallback
								}
								this.tExecuteGarbageCollection();
								break;
							//-----------------------------
							#endregion

							case (int)EReturnValue.JumpToLuaStage: // Transition to another Lua Stage
								UnmountAndChangeLuaStageOrError(LuaStageWrapper.GetNextRequestedStageName());
								this.tExecuteGarbageCollection();
								break;

							case (int)EReturnValue.HEYA:
								UnmountAndChangeStage(stageHeya, "Taiko Heya");
								break;

							case (int)EReturnValue.ONLINELOUNGE:
								UnmountAndChangeStage(stageOnlineLounge, "Online Lounge");
								break;

							case (int)EReturnValue.CONFIG:
								UnmountAndChangeStage(stageConfig, "Config");
								break;

							case (int)EReturnValue.EXIT:
								UnmountAndChangeStage(stageExit, "End");
								break;
						}
						#endregion
						break;

					case CStage.EStage.End:
						#region [ *** ]
						//-----------------------------
						if (this.nDrawLoopReturnValue != 0) {
							base.Exit();
							return;
						}
						//-----------------------------
						#endregion
						break;

					default:
						#region [ *** ]
						switch (this.nDrawLoopReturnValue) {
							case (int)EReturnValue.BackToTitle:
								#region [ *** ]
								//-----------------------------
								UnmountAndChangeLuaStageOrError("_title", "Title", CSystemError.Errno.ENO_TITLENOTFOUND);

								this.tExecuteGarbageCollection();
								break;
								//-----------------------------
								#endregion
						}
						#endregion
						break;
				}

				actScanningLoudness?.Draw();

				// オーバレイを描画する(テクスチャの生成されていない起動ステージは例外

				// Display log cards
				VisualLogManager?.Display();

				if (rCurrentStage != null
					&& rCurrentStage.eStageID != CStage.EStage.StartUp
					&& rCurrentStage.eStageID != CStage.EStage.CRASH
					&& OpenTaiko.Tx.Overlay != null
					&& !(OperatingSystem.IsIOS() || OperatingSystem.IsAndroid())) {
					OpenTaiko.Tx.Overlay.t2DDraw(0, 0);
				}
			}

			if (OpenTaiko.ConfigIni.KeyAssign.System.Capture.IsPressed()) {
#if DEBUG
				if (OpenTaiko.InputManager.Keyboard.KeyPressing((int)SlimDXKeys.Key.LeftControl)) {
					if (rCurrentStage.eStageID is not (CStage.EStage.StartUp or CStage.EStage.Game or CStage.EStage.ChangeSkin)) {
						this.EnterRefreshSkinStage();
						this.UnmountAndChangeStage(stageChangeSkin);
					}
				} else {
					// Debug.WriteLine( "capture: " + string.Format( "{0:2x}", (int) e.KeyCode ) + " " + (int) e.KeyCode );
					string strFullPath =
						Path.Combine(OpenTaiko.strEXEFolder, "Capture_img");
					strFullPath = Path.Combine(strFullPath, DateTime.Now.ToString("yyyyMMddHHmmss") + ".png");
					SaveResultScreen(strFullPath);
				}
#else
				string strFullPath =
					Path.Combine(OpenTaiko.strEXEFolder, "Capture_img");
				strFullPath = Path.Combine(strFullPath, DateTime.Now.ToString("yyyyMMddHHmmss") + ".png");
				SaveResultScreen(strFullPath);
#endif
			}

			#region [ Fullscreen Toggle ]
			if (this.bSwitchFullScreenAtNextFrame) {
				app.WindowMode = ConfigIni.nWindowMode;   // menu already set the chosen mode; apply it to the window
				this.bSwitchFullScreenAtNextFrame = false;
			}
			#endregion
			#region [ VSync Toggle ]
			if (this.bSwitchVSyncAtTheNextFrame) {
				VSync = ConfigIni.bEnableVSync;
				this.bSwitchVSyncAtTheNextFrame = false;
			}
			#endregion

#if DEBUG
			if (OpenTaiko.InputManager != null && OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.F11))
				OpenTaiko.ConfigIni.DEBUG_bShowImgui = !OpenTaiko.ConfigIni.DEBUG_bShowImgui;
			if (OpenTaiko.ConfigIni.DEBUG_bShowImgui)
				ImGuiDebugWindow.Draw();
#endif
		}
#if !DEBUG
		catch (Exception e) {
			Trace.WriteLine("");
			Trace.Write(e.ToString());
			Trace.WriteLine("");
			Trace.WriteLine("An error has occured.");
			AssemblyName asmApp = Assembly.GetExecutingAssembly().GetName();
			throw;
		}
#endif
	}

	private void NextSongSelectStage(CStage fromStage) {
		ChangeStage(OpenTaiko.latestSongSelect, "Return to song select menu");
		rPreviousStage = fromStage;
		this.tExecuteGarbageCollection();
	}

	// その他

	#region [ 汎用ヘルパー ]
	//-----------------
	public static CTexture tTextureCreate(string fileName) {
		return tTextureCreate(fileName, false);
	}
	public static CTexture tTextureCreate(string fileName, bool bBlackTransparent) => tTextureCreate(fileName, bBlackTransparent, 0);
	public static CTexture tTextureCreate(string fileName, bool bBlackTransparent, int maxDimension) {
		if (app == null) {
			return null;
		}
#if DEBUG
		Trace.TraceInformation($"[ALLOC_TEX] {fileName}");
#endif
		// Fast-skip missing files: returning null avoids a FileNotFoundException per missing texture, which is
		// very slow under a debugger (first-chance handling) — a malformed asset (e.g. a dancer whose
		// DancerConfig count exceeds its frame folders) otherwise throws dozens of them and freezes the load.
		// FileExistsCached uses a per-directory listing (no slow per-file metadata hit on AV-scanned folders).
		if (!CTexture.FileExistsCached(fileName)) {
			Trace.TraceWarning("Could not find specified texture file. ({0})", fileName);
			return null;
		}
		try {
			return new CTexture(fileName, bBlackTransparent, maxDimension);
		} catch (CTextureCreateFailedException e) {
			Trace.TraceError(e.ToString());
			Trace.TraceError("Texture generation has failed. ({0})", fileName);
			return null;
		} catch (FileNotFoundException) {
			Trace.TraceWarning("Could not find specified texture file. ({0})", fileName);
			return null;
		}
	}
	public static void tTextureRelease(ref CTexture tx) {
		OpenTaiko.tDisposeSafely(ref tx);
	}
	public static void tTextureRelease(ref CTextureAf tx) {
		OpenTaiko.tDisposeSafely(ref tx);
	}
	public static CTexture tTextureCreate(SKBitmap bitmap) {
		return tTextureCreate(bitmap, false);
	}
	public static CTexture tTextureCreate(SKBitmap bitmap, bool bBlackTransparent) {
		if (app == null) {
			return null;
		}
		if (bitmap == null) {
			Trace.TraceError("Texture generation has failed. (bitmap==null)");
			return null;
		}
		try {
			return new CTexture(bitmap, bBlackTransparent);
		} catch (CTextureCreateFailedException e) {
			Trace.TraceError(e.ToString());
			Trace.TraceError("Texture generation has failed. (txData)");
			return null;
		}
	}

	public static CTextureAf tTextureCreateAf(string fileName) {
		return tTextureCreateAf(fileName, false);
	}
	public static CTextureAf tTextureCreateAf(string fileName, bool bBlackTransparent) {
		if (app == null) {
			return null;
		}
		try {
			return new CTextureAf(fileName, bBlackTransparent);
		} catch (CTextureCreateFailedException e) {
			Trace.TraceError(e.ToString());
			Trace.TraceError("Texture generation has failed. ({0})", fileName);
			return null;
		} catch (FileNotFoundException e) {
			Trace.TraceError(e.ToString());
			Trace.TraceError("Texture generation has failed. ({0})", fileName);
			return null;
		}
	}

	/// <summary>プロパティ、インデクサには ref は使用できないので注意。</summary>
	public static void tDisposeSafely(ref CSound? obj) {
		obj?.tDispose();
		obj = null;
	}

	public static void tDisposeSafely<T>(ref T? obj) {
		(obj as IDisposable)?.Dispose();
		obj = default(T);
	}

	public static void tDisposeSafely<T>(ref T?[] array) where T : class, IDisposable //2020.08.01 Mr-Ojii twopointzero氏のソースコードをもとに追加
	{
		if (array == null) {
			return;
		}

		for (var i = 0; i < array.Length; i++) {
			tDisposeSafely(ref array[i]);
		}
	}

	/// <summary>
	/// そのフォルダの連番画像の最大値を返す。
	/// </summary>
	public static int tSequenceImageSheetCountCount(string DirectoryName, string Prefix = "", string Extension = ".png") {
		int num = 0;
		while (File.Exists(DirectoryName + Prefix + num + Extension)) {
			num++;
		}
		return num;
	}

	/// <summary>
	/// 曲名テクスチャの縮小倍率を返す。
	/// </summary>
	/// <param name="cTexture">曲名テクスチャ。</param>
	/// <param name="samePixel">等倍で表示するピクセル数の最大値(デフォルト値:645)</param>
	/// <returns>曲名テクスチャの縮小倍率。そのテクスチャがnullならば一倍(1f)を返す。</returns>
	public static float GetSongNameXScaling(ref CTexture cTexture, int samePixel = 660) {
		if (cTexture == null) return 1f;
		float scalingRate = (float)samePixel / (float)cTexture.szTextureSize.Width;
		if (cTexture.szTextureSize.Width <= samePixel)
			scalingRate = 1.0f;
		return scalingRate;
	}

	/// <summary>
	/// 難易度を表す数字を列挙体に変換します。
	/// </summary>
	/// <param name="number">難易度を表す数字。</param>
	/// <returns>Difficulty 列挙体</returns>
	public static Difficulty DifficultyNumberToEnum(int number) {
		switch (number) {
			case 0:
				return Difficulty.Easy;
			case 1:
				return Difficulty.Normal;
			case 2:
				return Difficulty.Hard;
			case 3:
				return Difficulty.Oni;
			case 4:
				return Difficulty.Edit;
			case 5:
				return Difficulty.Tower;
			case 6:
				return Difficulty.Dan;
			default:
				throw new IndexOutOfRangeException();
		}
	}

	//-----------------
	#endregion

	#region [ private ]
	//-----------------
	private bool bMouseCursorDisplaying = true;
	private bool bEndProcessCompleteDone;
	private static CTja[] tja = new CTja[MAX_PLAYERS];

	public static TextureLoader Tx = new TextureLoader();

	public List<CActivity> listTopLevelActivities;
	private int nDrawLoopReturnValue;
	private int _iosStageDebugCounter;
	private string strWindowTitle
	// ayo komi isn't this useless code? - tfd500
	{
		get {
			return "OpenTaiko";
		}
	}
	private CSound previewSound;
	public static DateTime StartupTime {
		get;
		private set;
	}

	private static CTraceLogListener? FileLogListener = null;

	private static void tStartupLog() {
		Trace.AutoFlush = true;
		try {
			Trace.Listeners.Add(FileLogListener = new CTraceLogListener(new StreamWriter(System.IO.Path.Combine(strEXEFolder, "OpenTaiko.log"), false, Encoding.UTF8)));
		} catch (System.UnauthorizedAccessException e)            // #24481 2011.2.20 yyagi
		{
			// still has console logging
			Trace.TraceError(e.ToString());
			int c = (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja") ? 0 : 1;
			string[] mes_writeErr = {
				"OpenTaiko.logへの書き込みができませんでした。書き込みできるようにしてから、再度起動してください。",
				"Failed to write OpenTaiko.log. Please set your device to READ/WRITE and try again."
			};
			Trace.WriteLine(mes_writeErr);
		}
	}

	private void tStartupProcess() {
		#region [ Error message interface initialisation ]
		try {
			// Load System error beforehand
			this.listTopLevelActivities = new List<CActivity>();
			SystemError = new CSystemError();
			this.listTopLevelActivities.Add(SystemError);

			VisualLogManager = new CVisualLogManager();

			#region [ Lua global stores initialisation ]

			GlobalStores = new LuaGlobalStores();

			#endregion
		} catch (Exception ex) {
			Trace.TraceError(ex.ToString());
			Trace.TraceError("Error message interface initialization falied.");
		}
		#endregion

		#region [ Read Config.ini and Database files ]
		//---------------------
		try {
			UnlockConditionFactory = new CUnlockConditionFactory();

			// Port <= 0.5.4 NamePlate.json to Pre 0.6.0 b1 Saves\
			NamePlateConfig = new NamePlateConfig();
			NamePlateConfig.tNamePlateConfig();

			Favorites = new Favorites();
			Favorites.tFavorites();

			RecentlyPlayedSongs = new RecentlyPlayedSongs();
			RecentlyPlayedSongs.tRecentlyPlayedSongs();

			Databases = new Databases();
			Databases.tDatabases();

			if (!File.Exists("Saves.db3")) {
				File.Copy(@$".init{Path.DirectorySeparatorChar}Saves.db3", "Saves.db3");
			}
			// Add a condition here (if old Saves\ format save files exist) to port them to database (?)
			SaveFileInstances = DBSaves.FetchSaveInstances();
		} catch (Exception ex) {
			Trace.TraceError(ex.ToString());
			Trace.TraceError("Config.ini and databases loading falied.");
		}

		//---------------------
		#endregion

		#region [ Log output config initialisation ]
		//---------------------
		if (!ConfigIni.bOutputLogs) {
			Trace.Listeners.Remove(FileLogListener);
		}
		Trace.WriteLine("");
		Trace.WriteLine("Welcome to OpenTaiko! Starting log...");
		Trace.WriteLine(string.Format("Version: {0}", VERSION));
		Trace.WriteLine("");
		Trace.TraceInformation("----------------------");
		Trace.TraceInformation("■ Application Info:");
		Trace.TraceInformation("OS Version: " + Environment.OSVersion);
		Trace.TraceInformation("Processors: " + Environment.ProcessorCount.ToString());
		Trace.TraceInformation("CLR Version: " + Environment.Version.ToString());

		if (ConfigIsNew) {
			Trace.TraceInformation("----------------------");
			Trace.TraceInformation("No Config.ini file was found. This usually means you've launched the game for the first time. A Config.ini file will be generated after safely closing the game.");
			Trace.TraceInformation("Thanks for joining us! (≧∇≦)ﾉ");
			Trace.TraceInformation($"{GraphicsDeviceType_} was selected as the recommended Graphics Device for your OS.");
			Trace.TraceInformation($"{(CConfigIni.ESoundDeviceTypeForConfig)ConfigIni.nSoundDeviceType} was selected as the recommended Sound Device for your OS.");
		}
		//---------------------
		#endregion

		TJA = null;


		#region [ Skin initialisation ]
		//---------------------
		Trace.TraceInformation("Initializing skin...");
		Trace.Indent();
#if !DEBUG
		try
#endif
		{
			Skin = new CSkin(OpenTaiko.ConfigIni.strSystemSkinSubfolderFullName, false);
			OpenTaiko.ConfigIni.strSystemSkinSubfolderFullName = OpenTaiko.Skin.GetCurrentSkinSubfolderFullName(true);  // 旧指定のSkinフォルダが消滅していた場合に備える

			ChangeResolution(OpenTaiko.Skin.Resolution[0], OpenTaiko.Skin.Resolution[1]);

			Trace.TraceInformation("Skin successfully initialized.");
		}
#if !DEBUG
		catch (Exception e) {
			Trace.TraceInformation("Skin failed to initialize.");
			TriggerSystemError(CSystemError.Errno.ENO_SKINNOTFOUND);
			return;
			//throw;
		} finally {
			Trace.Unindent();
		}
#endif

		//---------------------
		#endregion
		//-----------
		#region [ Timer initialisation ]
		//---------------------
		Trace.TraceInformation("Initializing timer...");
		Trace.Indent();
		try {
			Timer = new CTimer(CTimer.TimerType.MultiMedia);
			Trace.TraceInformation("Timer successfully initialized.");
		} finally {
			Trace.Unindent();
		}
		//---------------------
		#endregion
		//-----------

		#region [ FPS counter initialisation ]
		//---------------------
		Trace.TraceInformation("Initializing FPS counter...");
		Trace.Indent();
		try {
			FPS = new CFPS();
			FPSInput = new CFPS();
			Trace.TraceInformation("FPS counter initialized.");
		} finally {
			Trace.Unindent();
		}
		//---------------------
		#endregion

		#region [ Text console initialisation ]
		//---------------------
		Trace.TraceInformation("Initializing console...");
		Trace.Indent();
		try {
			actTextConsole = new CTextConsole();
			Trace.TraceInformation("Console initialized.");
			actTextConsole.Activate();
			actTextConsole.CreateManagedResource();
			actTextConsole.CreateUnmanagedResource();
			Trace.TraceInformation("Console has been activated.");
			Trace.TraceInformation("Console has finished being initialized.");
		} catch (Exception exception) {
			Trace.TraceError(exception.ToString());
			Trace.TraceError("Console failed to initialize.");
		} finally {
			Trace.Unindent();
		}
		//---------------------
		#endregion

		#region [ Input management initialisation ]
		//---------------------
		Trace.TraceInformation("Initializing DirectInput and MIDI input...");
		Trace.Indent();
		try {
			if ((OperatingSystem.IsIOS() || OperatingSystem.IsAndroid()) && ExternalInputDevices != null) {
				// Mobile: use externally-provided input devices (touch) instead of Silk.NET
				InputManager = new CInputManager(ExternalInputDevices);
			} else {
				InputManager = new CInputManager(Window_, OpenTaiko.ConfigIni.bBufferedInputs, true, OpenTaiko.ConfigIni.nControllerDeadzone / 100.0f);
			}
			InputManager.SetID(ConfigIni.StableIdToGuid);
			Trace.TraceInformation("DirectInput has been initialized.");
		} catch (Exception ex) {
			Trace.TraceError(ex.ToString());
			Trace.TraceError("DirectInput and MIDI input failed to initialize.");
			TriggerSystemError(CSystemError.Errno.ENO_INPUTINITFAILED, ex);
			return;
			//throw;
		} finally {
			Trace.Unindent();
		}
		//---------------------
		#endregion

		#region [ Pad initialisation ]
		//---------------------
		Trace.TraceInformation("Initialize pad...");
		Trace.Indent();
		try {
			Pad = new CPad(ConfigIni, InputManager);
			Trace.TraceInformation("Pad has been initialized.");
		} catch (Exception ex) {
			Trace.TraceError(ex.ToString());
			Trace.TraceError("Pad failed to initialize.");
			TriggerSystemError(CSystemError.Errno.ENO_PADINITFAILED, ex);
			return;
		} finally {
			Trace.Unindent();
		}
		//---------------------
		#endregion

		#region [ Sound Device initialization ]
		//---------------------
		if (OperatingSystem.IsIOS() || OperatingSystem.IsAndroid()) {
			Trace.TraceInformation("Mobile: initializing BASS sound device.");
			SoundManager = new SoundManager(Window_,
				ESoundDeviceType.Bass,
				OpenTaiko.ConfigIni.nBassBufferSizeMs,
				OpenTaiko.ConfigIni.nWASAPIBufferSizeMs,
				0,
				OpenTaiko.ConfigIni.nASIODevice,
				OpenTaiko.ConfigIni.bUseOSTimer);
			// iOS: the desktop branch below also creates these; without SongGainController the first note NREs (CTja.tチップの再生).
			SongGainController = new SongGainController();
			ConfigIniToSongGainControllerBinder.Bind(ConfigIni, SongGainController);
			SoundGroupLevelController = new SoundGroupLevelController(CSound.SoundInstances);
			ConfigIniToSoundGroupLevelControllerBinder.Bind(ConfigIni, SoundGroupLevelController);
		} else {
			Trace.TraceInformation("Initializing sound device...");
			Trace.Indent();
			try {
				ESoundDeviceType soundDeviceType;
				switch (OpenTaiko.ConfigIni.nSoundDeviceType) {
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
				SoundManager = new SoundManager(Window_,
					soundDeviceType,
					OpenTaiko.ConfigIni.nBassBufferSizeMs,
					OpenTaiko.ConfigIni.nWASAPIBufferSizeMs,
					// CDTXMania.ConfigIni.nASIOBufferSizeMs,
					0,
					OpenTaiko.ConfigIni.nASIODevice,
					OpenTaiko.ConfigIni.bUseOSTimer
				);
				//Sound管理 = FDK.CSound管理.Instance;
				//Sound管理.t初期化( soundDeviceType, 0, 0, CDTXMania.ConfigIni.nASIODevice, base.Window.Handle );


				Trace.TraceInformation("Initializing loudness scanning, song gain control, and sound group level control...");
				Trace.Indent();
				try {
					actScanningLoudness = new CActScanningLoudness();
					MountActivity(actScanningLoudness);
					LoudnessMetadataScanner.ScanningStateChanged +=
						(_, args) => actScanningLoudness.bIsActivelyScanning = args.IsActivelyScanning;
					LoudnessMetadataScanner.StartBackgroundScanning();

					SongGainController = new SongGainController();
					ConfigIniToSongGainControllerBinder.Bind(ConfigIni, SongGainController);

					SoundGroupLevelController = new SoundGroupLevelController(CSound.SoundInstances);
					ConfigIniToSoundGroupLevelControllerBinder.Bind(ConfigIni, SoundGroupLevelController);
				} finally {
					Trace.Unindent();
					Trace.TraceInformation("Initialized loudness scanning, song gain control, and sound group level control.");
				}

				ShowWindowTitle();
				FDK.SoundManager.bIsTimeStretch = OpenTaiko.ConfigIni.bTimeStretch;
				SoundManager.nMasterVolume = OpenTaiko.ConfigIni.nMasterVolume;
				Trace.TraceInformation("サウンドデバイスの初期化を完了しました。");
			} catch (Exception e) {
				Trace.TraceError(e.ToString());
				TriggerSystemError(CSystemError.Errno.ENO_NOAUDIODEVICE, e);
				return;
				// throw new NullReferenceException("No sound devices are enabled. Please check your audio settings.", e);
			} finally {
				Trace.Unindent();
			}
		}
		//---------------------
		#endregion

		#region [ Songs management initialization ]
		//---------------------
		Trace.TraceInformation("Initializing song list...");
		Trace.Indent();
		try {
			SongManager = new CSongManager();
			//				Songs管理_裏読 = new CSongs管理();
			EnumSongs = new CEnumSongs();
			actEnumSongs = new CActEnumSongs();
			Trace.TraceInformation("Song list initialized.");
		} catch (Exception e) {
			Trace.TraceError(e.ToString());
			Trace.TraceError("Song list failed to initialize.");
			TriggerSystemError(CSystemError.Errno.ENO_SONGLISTINITFAILED, e);
			return;
		} finally {
			Trace.Unindent();
		}
		//---------------------
		#endregion

		#region [ Random initialization ]
		//---------------------
		Random = new Random();
		//---------------------
		#endregion

		#region [ Modal queue initialisation ]

		ModalManager = new CModalManager();
		ModalManager.RefreshSkin();

		PopupMenuManager = new CPopupMenuManager();

		#endregion

		#region [ Stages initialisation ]
		//---------------------
		rCurrentStage = null;
		rPreviousStage = null;
		stageStartup = new CStageStartup();
		stageConfig = new CStageConfig();
		SongMount = new CSongMount();
		stageHeya = new CStageHeya();
		stageOnlineLounge = new CStageOnlineLounge();
		stageCutScene = new CStageCutScene();
		stageSongLoading = new CStageSongLoading();
		stageGameScreen = new CStagePlayDrumsScreen();
		stageResults = new CStageResult();
		stageChangeSkin = new CStageChangeSkin();
		stageTransition = new CStageTransition();
		stageExit = new CStageShutdown();
		NamePlate = new CNamePlate();

		this.listTopLevelActivities.Add(actEnumSongs);
		this.listTopLevelActivities.Add(actTextConsole);
		this.listTopLevelActivities.Add(stageStartup);
		this.listTopLevelActivities.Add(stageConfig);
		this.listTopLevelActivities.Add(stageHeya);
		this.listTopLevelActivities.Add(stageOnlineLounge);
		this.listTopLevelActivities.Add(stageSongLoading);
		this.listTopLevelActivities.Add(stageGameScreen);
		this.listTopLevelActivities.Add(stageResults);
		this.listTopLevelActivities.Add(stageChangeSkin);
		this.listTopLevelActivities.Add(stageTransition);
		this.listTopLevelActivities.Add(stageExit);
		//---------------------
		#endregion

		#region [ Discord Rpc initialisation]
		DiscordClient = new DiscordRpcClient("939341030141096007");
		DiscordClient?.Initialize();
		StartupTime = DateTime.UtcNow;
		DiscordClient?.SetPresence(new RichPresence() {
			Details = "",
			State = "Startup",
			Timestamps = new Timestamps(OpenTaiko.StartupTime),
			Assets = new Assets() {
				LargeImageKey = OpenTaiko.LargeImageKey,
				LargeImageText = OpenTaiko.LargeImageText,
			}
		});
		#endregion

		// Set up the HTTP server.
		OpenTaiko.HttpEventReporter = new HttpEventReporter("localhost", OpenTaiko.ConfigIni.nGameEventBroadcastingPort);
		if (OpenTaiko.ConfigIni.bEnableGameEventBroadcasting)
			OpenTaiko.HttpEventReporter.StartListening();

		Trace.TraceInformation("Application successfully started.");

		#region [ Move to Startup Stage ]
		//---------------------
		// The skin's Lua modules are NOT loaded here anymore. The boot stage loads them incrementally in its
		// own Draw loop (so the loading bar animates via the normal render loop, no manual presenting), and
		// once done it runs NamePlate/Modal RefreshSkin + starts the song-list enum. This keeps OnLoad fast
		// so the render loop (and the loading screen) starts immediately.
		CLoadingProgress.Begin();
		ChangeStage(stageStartup, "Startup");
		Trace.TraceInformation("----------------------");
		Trace.TraceInformation("■ Startup");
		//---------------------
		#endregion
	}

	public void ShowWindowTitle() {
		string delay = "";
		if (SoundManager.GetCurrentSoundDeviceType() != "DirectSound") {
			delay = "(" + SoundManager.GetSoundDelay() + "ms)";
		}
		AssemblyName asmApp = Assembly.GetExecutingAssembly().GetName();
		base.Text = asmApp.Name + " Ver." + VERSION + " - (" + GraphicsDeviceType_ + ") - (" + SoundManager.GetCurrentSoundDeviceType() + delay + ")";
	}

	private void tExitProcess() {
		if (!this.bEndProcessCompleteDone) {
			Trace.TraceInformation("----------------------");
			Trace.TraceInformation("■ Shutdown");

			ConfigIni.nWindowMode = WindowMode;
			// Only persist window geometry from a WINDOWED session — in fullscreen/borderless the window size is the
			// monitor size (or GLFW's fullscreen size), which must not overwrite the user's saved windowed dimensions.
			if (WindowMode == 0) {
				ConfigIni.nWindowBaseXPosition = WindowPosition.X;
				ConfigIni.nWindowBaseYPosition = WindowPosition.Y;
				ConfigIni.nWindowWidth = WindowSize.X;
				ConfigIni.nWindowHeight = WindowSize.Y;
			}
			ConfigIni.bEnableVSync = VSync;
			Framerate = 0;

			#region [ 曲検索の終了処理 ]
			//---------------------

			if (actEnumSongs != null) {
				Trace.TraceInformation("Ending enumeration of songs...");
				Trace.Indent();
				try {
					actEnumSongs.DeActivate();
					actEnumSongs = null;
					EnumSongs.Suspend(); // stop thread to prevent using disposed resources
					EnumSongs.WaitUntilSuspended();
					Trace.TraceInformation("Enumeration of songs closed down successfully.");
				} catch (Exception e) {
					Trace.TraceError(e.ToString());
					Trace.TraceError("Song enumeration could not close.");
				} finally {
					Trace.Unindent();
				}
			}
			//---------------------
			#endregion
			#region [ 現在のステージの終了処理 ]
			//---------------------
			if (OpenTaiko.rCurrentStage != null && OpenTaiko.rCurrentStage.IsActivated)     // #25398 2011.06.07 MODIFY FROM
			{
				Trace.TraceInformation("Exiting stage...");
				Trace.Indent();
				try {
					UnmountActivity(rCurrentStage);
					Trace.TraceInformation("Stage exited.");
				} finally {
					Trace.Unindent();
				}
			}
			//---------------------
			#endregion

			#region Discordの処理
			DiscordClient?.Dispose();
			#endregion
			#region [ 曲リストの終了処理 ]
			//---------------------
			if (SongManager != null) {
				Trace.TraceInformation("Ending song list...");
				Trace.Indent();
				try {
#pragma warning disable SYSLIB0011
					if (EnumSongs.IsSongListEnumCompletelyDone
						&& !OperatingSystem.IsIOS() && !OperatingSystem.IsAndroid()) {   // BinaryFormatter is unsupported on mobile
						using Stream songlistdb = File.Create($"{OpenTaiko.strEXEFolder}songlist.db");
						CEnumSongs.WriteSongListCache(songlistdb, SongManager.listSongsDB);
					}
#pragma warning restore SYSLIB0011

					SongManager = null;
					Trace.TraceInformation("Song list terminated.");
				} catch (Exception exception) {
					Trace.TraceError(exception.ToString());
					Trace.TraceError("Song list failed to terminate.");
				} finally {
					Trace.Unindent();
				}
			}
			//---------------------
			#endregion
			#region TextureLoaderの処理
			Tx.DisposeTexture();
			#endregion
			#region [ スキンの終了処理 ]
			//---------------------
			if (Skin != null) {
				Trace.TraceInformation("Terminating skin...");
				Trace.Indent();
				try {
					Skin.Dispose();
					Skin = null;
					Trace.TraceInformation("Skin has been terminated.");
				} catch (Exception exception2) {
					Trace.TraceError(exception2.ToString());
					Trace.TraceError("Skin failed to terminate.");
				} finally {
					Trace.Unindent();
				}
			}
			//---------------------
			#endregion
			#region [ DirectSoundの終了処理 ]
			//---------------------
			if (SoundManager != null) {
				Trace.TraceInformation("Ending DirectSound devices...");
				Trace.Indent();
				try {
					SoundManager.Dispose();
					SoundManager = null;
					Trace.TraceInformation("DirectSound devices have been terminated.");
				} catch (Exception exception3) {
					Trace.TraceError(exception3.ToString());
					Trace.TraceError("DirectSound devices failed to terminate.");
				} finally {
					Trace.Unindent();
				}
			}
			//---------------------
			#endregion
			#region [ パッドの終了処理 ]
			//---------------------
			if (Pad != null) {
				Trace.TraceInformation("Ending pads...");
				Trace.Indent();
				try {
					Pad = null;
					Trace.TraceInformation("Pads have been terminated.");
				} catch (Exception exception4) {
					Trace.TraceError(exception4.ToString());
					Trace.TraceError("Pads failed to terminate。");
				} finally {
					Trace.Unindent();
				}
			}
			//---------------------
			#endregion
			#region [ DirectInput, MIDI入力の終了処理 ]
			//---------------------
			if (InputManager != null) {
				Trace.TraceInformation("Ending DirectInput and MIDI devices...");
				Trace.Indent();
				try {
					InputManager.Dispose();
					InputManager = null;
					Trace.TraceInformation("DirectInput and MIDI devices terminated.");
				} catch (Exception exception5) {
					Trace.TraceError(exception5.ToString());
					Trace.TraceError("DirectInput and MIDI devices failed to terminate.");
				} finally {
					Trace.Unindent();
				}
			}
			//---------------------
			#endregion
			#region [ 文字コンソールの終了処理 ]
			//---------------------
			if (actTextConsole != null) {
				Trace.TraceInformation("Ending console...");
				Trace.Indent();
				try {
					actTextConsole.DeActivate();
					actTextConsole.ReleaseManagedResource();
					actTextConsole.ReleaseUnmanagedResource();
					actTextConsole = null;
					Trace.TraceInformation("Console terminated.");
				} catch (Exception exception6) {
					Trace.TraceError(exception6.ToString());
					Trace.TraceError("Console failed to terminate.");
				} finally {
					Trace.Unindent();
				}
			}
			//---------------------
			#endregion
			#region [ FPSカウンタの終了処理 ]
			//---------------------
			Trace.TraceInformation("Ending FPS counter...");
			Trace.Indent();
			try {
				FPSInput = FPS = null;
				Trace.TraceInformation("FPS counter terminated.");
			} finally {
				Trace.Unindent();
			}
			//---------------------
			#endregion
			#region [ タイマの終了処理 ]
			//---------------------
			Trace.TraceInformation("Ending timer...");
			Trace.Indent();
			try {
				if (Timer != null) {
					Timer.Dispose();
					Timer = null;
					Trace.TraceInformation("Timer terminated.");
				} else {
					Trace.TraceInformation("There are no existing timers.");
				}
			} finally {
				Trace.Unindent();
			}
			//---------------------
			#endregion
			#region [ Config.iniの出力 ]
			//---------------------
			Trace.TraceInformation("Outputting Config.ini...");
			Trace.TraceInformation("This only needs to be done once, unless you have deleted the file!");
			string str = strEXEFolder + "Config.ini";
			Trace.Indent();
			try {
				// quitting the game while a replay is armed/playing: put the player's real mods back before the
				// export below persists the config (the replay's virtual mods must never reach Config.ini)
				CSongReplay.tRestoreVirtualMods();
				// the exporter mutates the config (hidden window, auto, player count) — never persist that
				if (!VideoExporter.Active) ConfigIni.tExport(str);
				Trace.TraceInformation("Saved succesfully. ({0})", str);
			} catch (Exception e) {
				Trace.TraceError(e.ToString());
				Trace.TraceError("Config.ini failed to create. ({0})", str);
			} finally {
				Trace.Unindent();
			}

			Trace.TraceInformation("Deinitializing loudness scanning, song gain control, and sound group level control...");
			Trace.Indent();
			try {
				SoundGroupLevelController = null;
				SongGainController = null;
				LoudnessMetadataScanner.StopBackgroundScanning(joinImmediately: true);
				UnmountActivity(actScanningLoudness);
				actScanningLoudness = null;
			} finally {
				Trace.Unindent();
				Trace.TraceInformation("Deinitialized loudness scanning, song gain control, and sound group level control.");
			}

			ConfigIni = null;

			//---------------------
			#endregion
			Trace.TraceInformation("OpenTaiko has closed down successfully.");
			this.bEndProcessCompleteDone = true;
		}
	}

	private void tExecuteGarbageCollection() {
		GC.Collect(GC.MaxGeneration);
		GC.WaitForPendingFinalizers();
		GC.Collect(GC.MaxGeneration);
	}

	private void ChangeResolution(int nWidth, int nHeight) {
		GameWindowSize.Width = nWidth;
		GameWindowSize.Height = nHeight;

		// Apply the skin render-scale: snap the saved multiplier to one this skin actually offers (the list always
		// contains 1.0) and push it to the engine. The global render-scale FBO rebuilds itself lazily when this (or
		// GameWindowSize) changes, so 2D/text/3D/textures all start rendering at the new internal resolution.
		double cur = OpenTaiko.ConfigIni.fRenderScale;
		double snapped = 1.0;
		if (OpenTaiko.Skin != null) {
			foreach (var r in OpenTaiko.Skin.Resolutions) {
				if (Math.Abs(r.val - cur) < 1e-4) { snapped = r.val; break; }
			}
		}
		OpenTaiko.ConfigIni.fRenderScale = (float)snapped;
		Game.RenderScale = (float)snapped;

		//WindowSize = new Silk.NET.Maths.Vector2D<int>(nWidth, nHeight);
	}

	public void RefreshSkin() {
		this.ChangeSkin();
		this.LoadSkin();
	}

	public void ChangeSkin() {
		Trace.TraceInformation("Skin Change:" + OpenTaiko.Skin.GetCurrentSkinSubfolderFullName(false));

		OpenTaiko.actTextConsole.DeActivate();
		actTextConsole.ReleaseManagedResource();
		actTextConsole.ReleaseUnmanagedResource();

		EnumSongs.Suspend(); // stop thread to prevent using disposed resources
		EnumSongs.WaitUntilSuspended();

		stageGameScreen.actEnd.ReleaseManagedResource(); // force release due to lazy release

		OpenTaiko.Skin.Dispose();
		OpenTaiko.Skin = new CSkin(OpenTaiko.ConfigIni.strSystemSkinSubfolderFullName, false);

		ChangeResolution(OpenTaiko.Skin.Resolution[0], OpenTaiko.Skin.Resolution[1]);

		OpenTaiko.actTextConsole.Activate();
		actTextConsole.CreateManagedResource();
		actTextConsole.CreateUnmanagedResource();
	}

	public void LoadSkin() {
		// Synchronous full (re)load — kept for any caller that doesn't drive the incremental path.
		var loader = LoadSkinBegin();
		while (loader.MoveNext()) { }
		LoadSkinFinish();
	}

	// Incremental skin (re)load, so CStageChangeSkin can drive it across frames with a loading bar (the same
	// pattern boot uses). LoadSkinBegin returns the module loader to MoveNext each frame; LoadSkinFinish runs
	// once it (and the streamed onStart textures) are done.
	public System.Collections.Generic.IEnumerator<float> LoadSkinBegin() {
		OpenTaiko.Skin.PreloadSystemSounds();
		return OpenTaiko.Skin.LoadModulesIncrementally();
	}

	public void LoadSkinFinish() {
		OpenTaiko.Tx.DisposeTexture();
		OpenTaiko.Tx.LoadTexture();

		// Re-propagate AfterSongEnum events to all lua stages
		if (EnumSongs.IsSongListEnumCompletelyDone) {
			LuaStageWrapper.PropagateAfterSongEnumEvent();
			LuaActivityWrapper.PropagateAfterSongEnumEvent();
		}
		EnumSongs.Resume();

		actEnumSongs.RefreshSkin(EnumSongs.IsEnumerating);
		OpenTaiko.NamePlate.RefreshSkin();
		OpenTaiko.ModalManager.RefreshSkin();
		OpenTaiko.PopupMenuManager.RefreshSkin();
		CVirtualSlotManager.RefreshAICharacter();   // AI battle slot character now comes from the (reloaded) skin
	}
	#endregion

	#region [ EXTENDED VARIABLES ]
	public static float fCamXOffset;
	public static float fCamYOffset;

	public static float fCamZoomFactor = 1.0f;
	public static float fCamRotation;

	public static float fCamXScale = 1.0f;
	public static float fCamYScale = 1.0f;

	public static Color4 borderColor { get => Game.BorderColor; set => Game.BorderColor = value; }

	public static void ResetCameraStates() {
		fCamXOffset = 0;
		fCamYOffset = 0;
		fCamZoomFactor = 1.0f;
		fCamRotation = 0;
		fCamXScale = 1.0f;
		fCamYScale = 1.0f;
		borderColor = new Color4(0f, 0f, 0f, 1f);
	}
	#endregion
}
