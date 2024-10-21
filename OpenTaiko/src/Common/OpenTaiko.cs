using System.Diagnostics;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using DiscordRPC;
using FDK;
using SampleFramework;
using Silk.NET.Maths;
using SkiaSharp;
using Rectangle = System.Drawing.Rectangle;

namespace OpenTaiko {
	internal class OpenTaiko : Game {
		// Properties
		#region [ properties ]
		public static readonly string VERSION = Assembly.GetExecutingAssembly().GetName().Version.ToString();
		public static readonly string AppDisplayThreePartVersion = GetAppDisplayThreePartVersion();
		public static readonly string AppNumericThreePartVersion = GetAppNumericThreePartVersion();

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
		public static readonly string SLIMDXDLL = "c_net20x86_Jun2010";
		public static readonly string D3DXDLL = "d3dx9_43.dll";     // June 2010

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

		#region [DTX instances]
		public static CDTX DTX {
			get {
				return dtx[0];
			}
			set {
				if ((dtx[0] != null) && (app != null)) {
					dtx[0].DeActivate();
					dtx[0].ReleaseManagedResource();
					dtx[0].ReleaseUnmanagedResource();
					app.listトップレベルActivities.Remove(dtx[0]);
				}
				dtx[0] = value;
				if ((dtx[0] != null) && (app != null)) {
					app.listトップレベルActivities.Add(dtx[0]);
				}
			}
		}
		public static CDTX DTX_2P {
			get {
				return dtx[1];
			}
			set {
				if ((dtx[1] != null) && (app != null)) {
					dtx[1].DeActivate();
					dtx[1].ReleaseManagedResource();
					dtx[1].ReleaseUnmanagedResource();
					app.listトップレベルActivities.Remove(dtx[1]);
				}
				dtx[1] = value;
				if ((dtx[1] != null) && (app != null)) {
					app.listトップレベルActivities.Add(dtx[1]);
				}
			}
		}
		public static CDTX DTX_3P {
			get {
				return dtx[2];
			}
			set {
				if ((dtx[2] != null) && (app != null)) {
					dtx[2].DeActivate();
					dtx[2].ReleaseManagedResource();
					dtx[2].ReleaseUnmanagedResource();
					app.listトップレベルActivities.Remove(dtx[2]);
				}
				dtx[2] = value;
				if ((dtx[2] != null) && (app != null)) {
					app.listトップレベルActivities.Add(dtx[2]);
				}
			}
		}
		public static CDTX DTX_4P {
			get {
				return dtx[3];
			}
			set {
				if ((dtx[3] != null) && (app != null)) {
					dtx[3].DeActivate();
					dtx[3].ReleaseManagedResource();
					dtx[3].ReleaseUnmanagedResource();
					app.listトップレベルActivities.Remove(dtx[3]);
				}
				dtx[3] = value;
				if ((dtx[3] != null) && (app != null)) {
					app.listトップレベルActivities.Add(dtx[3]);
				}
			}
		}
		public static CDTX DTX_5P {
			get {
				return dtx[4];
			}
			set {
				if ((dtx[4] != null) && (app != null)) {
					dtx[4].DeActivate();
					dtx[4].ReleaseManagedResource();
					dtx[4].ReleaseUnmanagedResource();
					app.listトップレベルActivities.Remove(dtx[4]);
				}
				dtx[4] = value;
				if ((dtx[4] != null) && (app != null)) {
					app.listトップレベルActivities.Add(dtx[4]);
				}
			}
		}

		public static CDTX GetDTX(int player) {
			switch (player) {
				case 0:
					return OpenTaiko.DTX;
				case 1:
					return OpenTaiko.DTX_2P;
				case 2:
					return OpenTaiko.DTX_3P;
				case 3:
					return OpenTaiko.DTX_4P;
				case 4:
					return OpenTaiko.DTX_5P;
			}
			return null;
		}

		#endregion

		public static CSongReplay[] ReplayInstances = new CSongReplay[5];

		public static bool IsPerformingCalibration;

		public static CFPS FPS {
			get;
			private set;
		}
		public static CInputManager InputManager {
			get;
			private set;
		}
		#region [ 入力範囲ms ]
		public static int nPerfect範囲ms {
			get {
				if (stageSongSelect.rChoosenSong != null) {
					CSongListNode c曲リストノード = stageSongSelect.rChoosenSong.rParentNode;
					if (((c曲リストノード != null) && (c曲リストノード.eノード種別 == CSongListNode.ENodeType.BOX)) && (c曲リストノード.nPerfect範囲ms >= 0)) {
						return c曲リストノード.nPerfect範囲ms;
					}
				}
				return ConfigIni.nHitRangeMs.Perfect;
			}
		}
		public static int nGreat範囲ms {
			get {
				if (stageSongSelect.rChoosenSong != null) {
					CSongListNode c曲リストノード = stageSongSelect.rChoosenSong.rParentNode;
					if (((c曲リストノード != null) && (c曲リストノード.eノード種別 == CSongListNode.ENodeType.BOX)) && (c曲リストノード.nGreat範囲ms >= 0)) {
						return c曲リストノード.nGreat範囲ms;
					}
				}
				return ConfigIni.nHitRangeMs.Great;
			}
		}
		public static int nGood範囲ms {
			get {
				if (stageSongSelect.rChoosenSong != null) {
					CSongListNode c曲リストノード = stageSongSelect.rChoosenSong.rParentNode;
					if (((c曲リストノード != null) && (c曲リストノード.eノード種別 == CSongListNode.ENodeType.BOX)) && (c曲リストノード.nGood範囲ms >= 0)) {
						return c曲リストノード.nGood範囲ms;
					}
				}
				return ConfigIni.nHitRangeMs.Good;
			}
		}
		public static int nPoor範囲ms {
			get {
				if (stageSongSelect.rChoosenSong != null) {
					CSongListNode c曲リストノード = stageSongSelect.rChoosenSong.rParentNode;
					if (((c曲リストノード != null) && (c曲リストノード.eノード種別 == CSongListNode.ENodeType.BOX)) && (c曲リストノード.nPoor範囲ms >= 0)) {
						return c曲リストノード.nPoor範囲ms;
					}
				}
				return ConfigIni.nHitRangeMs.Poor;
			}
		}
		#endregion
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
		public static CSongs管理 Songs管理 {
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

		public static CStage起動 stage起動 {
			get;
			private set;
		}
		public static CStageタイトル stageタイトル {
			get;
			private set;
		}
		public static CStageコンフィグ stageコンフィグ {
			get;
			private set;
		}
		public static CStage選曲 stageSongSelect {
			get;
			private set;
		}
		public static CStage段位選択 stage段位選択 {
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

		public static CStageTowerSelect stageTowerSelect {
			get;
			private set;
		}

		public static CStage曲読み込み stage曲読み込み {
			get;
			private set;
		}
		public static CStage演奏ドラム画面 stage演奏ドラム画面 {
			get;
			private set;
		}
		public static CStage結果 stage結果 {
			get;
			private set;
		}
		public static CStageChangeSkin stageChangeSkin {
			get;
			private set;
		}
		public static CStage終了 stage終了 {
			get;
			private set;
		}
		public static CStage r現在のステージ = null;
		public static CStage r直前のステージ = null;
		public static string strEXEのあるフォルダ {
			get;
			private set;
		}
		public static CTimer Timer {
			get;
			private set;
		}
		public bool b次のタイミングで垂直帰線同期切り替えを行う {
			get;
			set;
		}
		public bool b次のタイミングで全画面_ウィンドウ切り替えを行う {
			get;
			set;
		}
		public static DiscordRpcClient DiscordClient;

		// 0 : 1P, 1 : 2P
		public static int SaveFile = 0;

		public static SaveFile[] SaveFileInstances = new SaveFile[5];

		// 0 : Hidari, 1 : Migi (1P only)
		public static int PlayerSide = 0;

		public static int GetActualPlayer(int player) {
			if (SaveFile == 0 || player > 1)
				return player;
			if (player == 0)
				return 1;
			return 0;
		}

		public static bool P1IsBlue() {
			return (OpenTaiko.PlayerSide == 1 && OpenTaiko.ConfigIni.nPlayerCount == 1);
		}

		#endregion

		// Constructor

		public OpenTaiko() : base("OpenTaiko.ico") {
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
			#region [ strEXEのあるフォルダを決定する ]
			//-----------------
			strEXEのあるフォルダ = Environment.CurrentDirectory + Path.DirectorySeparatorChar;
			// END #23629 2010.11.13 from
			//-----------------
			#endregion

			ConfigIni = new CConfigIni();

			string path = strEXEのあるフォルダ + "Config.ini";
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

			switch (ConfigIni.nGraphicsDeviceType) {
				case 0:
					GraphicsDeviceType_ = Silk.NET.GLFW.AnglePlatformType.OpenGL;
					break;
				case 1:
					GraphicsDeviceType_ = Silk.NET.GLFW.AnglePlatformType.D3D11;
					break;
				case 2:
					GraphicsDeviceType_ = Silk.NET.GLFW.AnglePlatformType.Vulkan;
					break;
				case 3:
					GraphicsDeviceType_ = Silk.NET.GLFW.AnglePlatformType.Metal;
					break;
			}

			WindowPosition = new Silk.NET.Maths.Vector2D<int>(ConfigIni.nWindowBaseXPosition, ConfigIni.nWindowBaseYPosition);
			WindowSize = new Silk.NET.Maths.Vector2D<int>(ConfigIni.nWindowWidth, ConfigIni.nWindowHeight);
			FullScreen = ConfigIni.bFullScreen;
			VSync = ConfigIni.bEnableVSync;
			Framerate = 0;

			base.Configuration();
		}

		protected override void Initialize() {
			this.t起動処理();
		}

		protected override void LoadContent() {
			if (ConfigIni.bWindowMode) {
				if (!this.bマウスカーソル表示中) {
					this.bマウスカーソル表示中 = true;
				}
			} else if (this.bマウスカーソル表示中) {
				this.bマウスカーソル表示中 = false;
			}

			if (this.listトップレベルActivities != null) {
				foreach (CActivity activity in this.listトップレベルActivities)
					activity.CreateUnmanagedResource();
			}
		}
		protected override void UnloadContent() {
			if (this.listトップレベルActivities != null) {
				foreach (CActivity activity in this.listトップレベルActivities)
					activity.ReleaseUnmanagedResource();
			}
		}
		protected override void OnExiting() {
			ConfigIni.nWindowBaseXPosition = WindowPosition.X;
			ConfigIni.nWindowBaseYPosition = WindowPosition.Y;
			ConfigIni.nWindowWidth = WindowSize.X;
			ConfigIni.nWindowHeight = WindowSize.Y;
			ConfigIni.bFullScreen = FullScreen;
			ConfigIni.bEnableVSync = VSync;
			Framerate = 0;

			this.t終了処理();
			base.OnExiting();
		}
		protected override void Update() {
			InputManager?.Polling(OpenTaiko.ConfigIni.bBufferedInputs);
		}
		protected override void Draw() {
#if !DEBUG
			try
#endif
			{
				Timer?.Update();
				SoundManager.PlayTimer?.Update();
				FPS?.Update();

				if (BeatScaling != null) {
					BeatScaling.Tick();
					float value = MathF.Sin((BeatScaling.CurrentValue / 1000.0f) * MathF.PI / 2.0f);
					float scale = 1.0f + ((1.0f - value) / 40.0f);
					Camera *= Matrix4X4.CreateScale(scale, scale, 1.0f);
					if (BeatScaling.CurrentValue == BeatScaling.EndValue) BeatScaling = null;
				}

				// #xxxxx 2013.4.8 yyagi; sleepの挿入位置を、EndScnene～Present間から、BeginScene前に移動。描画遅延を小さくするため。

				if (r現在のステージ != null) {
					OpenTaiko.NamePlate.lcNamePlate.Update();
					this.n進行描画の戻り値 = (r現在のステージ != null) ? r現在のステージ.Draw() : 0;

					CScoreIni scoreIni = null;

					#region [ 曲検索スレッドの起動/終了 ]
					// ここに"Enumerating Songs..."表示を集約
					actEnumSongs.Draw();                            // "Enumerating Songs..."アイコンの描画

					switch (r現在のステージ.eStageID) {
						case CStage.EStage.Title:
						case CStage.EStage.Config:
						case CStage.EStage.SongSelect:
						case CStage.EStage.SongLoading:
							if (EnumSongs != null) {
								#region [ (特定条件時) 曲検索スレッドの起動_開始 ]
								if (r現在のステージ.eStageID == CStage.EStage.Title &&
									 r直前のステージ.eStageID == CStage.EStage.StartUp &&
									 this.n進行描画の戻り値 == (int)CStageタイトル.E戻り値.継続 &&
									 !EnumSongs.IsSongListEnumStarted) {
									actEnumSongs.Activate();
									if (!ConfigIni.PreAssetsLoading) {
										actEnumSongs.CreateManagedResource();
										actEnumSongs.CreateUnmanagedResource();
									}
									OpenTaiko.stageSongSelect.bIsEnumeratingSongs = true;
									EnumSongs.Init();   // 取得した曲数を、新インスタンスにも与える
									EnumSongs.StartEnumFromDisk();      // 曲検索スレッドの起動_開始
								}
								#endregion

								#region [ 曲検索の中断と再開 ]
								if (r現在のステージ.eStageID == CStage.EStage.SongSelect && !EnumSongs.IsSongListEnumCompletelyDone) {
									switch (this.n進行描画の戻り値) {
										case 0:     // 何もない
											EnumSongs.Resume();
											EnumSongs.IsSlowdown = false;
											actEnumSongs.Activate();
											if (!ConfigIni.PreAssetsLoading) {
												actEnumSongs.CreateManagedResource();
												actEnumSongs.CreateUnmanagedResource();
											}
											break;

										case 2:     // 曲決定
											EnumSongs.Suspend();                        // #27060 バックグラウンドの曲検索を一時停止
											actEnumSongs.DeActivate();
											if (!ConfigIni.PreAssetsLoading) {
												actEnumSongs.ReleaseManagedResource();
												actEnumSongs.ReleaseUnmanagedResource();
											}
											break;
									}
								}
								#endregion

								#region [ 曲探索中断待ち待機 ]
								if (r現在のステージ.eStageID == CStage.EStage.SongLoading && !EnumSongs.IsSongListEnumCompletelyDone &&
									EnumSongs.thDTXFileEnumerate != null)                           // #28700 2012.6.12 yyagi; at Compact mode, enumerating thread does not exist.
								{
									EnumSongs.WaitUntilSuspended();                                 // 念のため、曲検索が一時中断されるまで待機
								}
								#endregion

								#region [ 曲検索が完了したら、実際の曲リストに反映する ]
								// CStage選曲.On活性化() に回した方がいいかな？
								if (EnumSongs.IsSongListEnumerated) {
									actEnumSongs.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										actEnumSongs.ReleaseManagedResource();
										actEnumSongs.ReleaseUnmanagedResource();
									}
									OpenTaiko.stageSongSelect.bIsEnumeratingSongs = false;

									bool bRemakeSongTitleBar = (r現在のステージ.eStageID == CStage.EStage.SongSelect) ? true : false;
									OpenTaiko.stageSongSelect.Refresh(EnumSongs.Songs管理, bRemakeSongTitleBar);
									EnumSongs.SongListEnumCompletelyDone();
								}
								#endregion
							}
							break;
					}
					#endregion

					switch (r現在のステージ.eStageID) {
						case CStage.EStage.None:
							break;

						case CStage.EStage.StartUp:
							#region [ *** ]
							//-----------------------------
							if (this.n進行描画の戻り値 != 0) {
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) {
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation("----------------------");
								Trace.TraceInformation("■ Title");
								stageタイトル.Activate();
								if (!ConfigIni.PreAssetsLoading) {
									stageタイトル.CreateManagedResource();
									stageタイトル.CreateUnmanagedResource();
								}
								r直前のステージ = r現在のステージ;
								r現在のステージ = stageタイトル;

								this.tガベージコレクションを実行する();
							}
							//-----------------------------
							#endregion
							break;

						case CStage.EStage.Title:
							#region [ *** ]
							//-----------------------------
							switch (this.n進行描画の戻り値) {
								case (int)CStageタイトル.E戻り値.GAMESTART:
									#region [ 選曲処理へ ]
									//-----------------------------
									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}
									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ Song Select");
									stageSongSelect.Activate();
									if (!ConfigIni.PreAssetsLoading) {
										stageSongSelect.CreateManagedResource();
										stageSongSelect.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;
									r現在のステージ = stageSongSelect;

									OpenTaiko.latestSongSelect = stageSongSelect;
									//-----------------------------
									#endregion
									break;

								case (int)CStageタイトル.E戻り値.DANGAMESTART:
									#region [ 段位選択処理へ ]
									//-----------------------------
									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}
									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ Dan-i Dojo");
									stage段位選択.Activate();
									if (!ConfigIni.PreAssetsLoading) {
										stage段位選択.CreateManagedResource();
										stage段位選択.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;
									r現在のステージ = stage段位選択;
									OpenTaiko.latestSongSelect = stage段位選択;
									//-----------------------------
									#endregion
									break;

								case (int)CStageタイトル.E戻り値.TAIKOTOWERSSTART:
									#region [Online Lounge]
									//-----------------------------
									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}
									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ Online Lounge");
									stageTowerSelect.Activate();
									if (!ConfigIni.PreAssetsLoading) {
										stageTowerSelect.CreateManagedResource();
										stageTowerSelect.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;
									r現在のステージ = stageTowerSelect;
									//-----------------------------
									#endregion
									break;

								case (int)CStageタイトル.E戻り値.HEYA:
									#region [Heya menu]
									//-----------------------------
									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}
									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ Taiko Heya");
									stageHeya.Activate();
									if (!ConfigIni.PreAssetsLoading) {
										stageHeya.CreateManagedResource();
										stageHeya.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;
									r現在のステージ = stageHeya;
									//-----------------------------
									#endregion
									break;

								case (int)CStageタイトル.E戻り値.ONLINELOUNGE:
									#region [Online Lounge]
									//-----------------------------
									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}
									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ Online Lounge");
									stageOnlineLounge.Activate();
									if (!ConfigIni.PreAssetsLoading) {
										stageOnlineLounge.CreateManagedResource();
										stageOnlineLounge.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;
									r現在のステージ = stageOnlineLounge;
									//-----------------------------
									#endregion
									break;

								case (int)CStageタイトル.E戻り値.CONFIG:
									#region [ *** ]
									//-----------------------------
									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}
									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ Config");
									stageコンフィグ.Activate();
									if (!ConfigIni.PreAssetsLoading) {
										stageコンフィグ.CreateManagedResource();
										stageコンフィグ.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;
									r現在のステージ = stageコンフィグ;
									//-----------------------------
									#endregion
									break;

								case (int)CStageタイトル.E戻り値.EXIT:
									#region [ *** ]
									//-----------------------------
									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}
									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ End");
									stage終了.Activate();
									if (!ConfigIni.PreAssetsLoading) {
										stage終了.CreateManagedResource();
										stage終了.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;
									r現在のステージ = stage終了;
									//-----------------------------
									#endregion
									break;

								case (int)CStageタイトル.E戻り値.AIBATTLEMODE:
									#region [ 選曲処理へ ]
									//-----------------------------
									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}
									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ Song Select");
									stageSongSelect.Activate();
									if (!ConfigIni.PreAssetsLoading) {
										stageSongSelect.CreateManagedResource();
										stageSongSelect.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;
									r現在のステージ = stageSongSelect;

									OpenTaiko.latestSongSelect = stageSongSelect;
									ConfigIni.nPreviousPlayerCount = ConfigIni.nPlayerCount;
									ConfigIni.nPlayerCount = 2;
									ConfigIni.bAIBattleMode = true;
									ConfigIni.tInitializeAILevel();
									//-----------------------------
									#endregion
									break;

							}

							//-----------------------------
							#endregion
							break;

						case CStage.EStage.Config:
							#region [ *** ]
							//-----------------------------
							if (this.n進行描画の戻り値 != 0) {
								switch (r直前のステージ.eStageID) {
									case CStage.EStage.Title:
										#region [ *** ]
										//-----------------------------
										r現在のステージ.DeActivate();
										if (!ConfigIni.PreAssetsLoading) {
											r現在のステージ.ReleaseManagedResource();
											r現在のステージ.ReleaseUnmanagedResource();
										}
										Trace.TraceInformation("----------------------");
										Trace.TraceInformation("■ Title");
										stageタイトル.Activate();
										if (!ConfigIni.PreAssetsLoading) {
											stageタイトル.CreateManagedResource();
											stageタイトル.CreateUnmanagedResource();
										}
										stageタイトル.tReloadMenus();
										r直前のステージ = r現在のステージ;
										r現在のステージ = stageタイトル;

										this.tガベージコレクションを実行する();
										break;
									//-----------------------------
									#endregion

									case CStage.EStage.SongSelect:
										#region [ *** ]
										//-----------------------------
										r現在のステージ.DeActivate();
										if (!ConfigIni.PreAssetsLoading) {
											r現在のステージ.ReleaseManagedResource();
											r現在のステージ.ReleaseUnmanagedResource();
										}
										Trace.TraceInformation("----------------------");
										Trace.TraceInformation("■ Song Select");
										stageSongSelect.Activate();
										if (!ConfigIni.PreAssetsLoading) {
											stageSongSelect.CreateManagedResource();
											stageSongSelect.CreateUnmanagedResource();
										}
										r直前のステージ = r現在のステージ;
										r現在のステージ = stageSongSelect;

										this.tガベージコレクションを実行する();
										break;
										//-----------------------------
										#endregion
								}
								return;
							}
							//-----------------------------
							#endregion
							break;

						case CStage.EStage.SongSelect:
							#region [ *** ]
							//-----------------------------
							switch (this.n進行描画の戻り値) {
								case (int)CStage選曲.E戻り値.タイトルに戻る:
									#region [ *** ]
									//-----------------------------
									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}
									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ Title");
									stageタイトル.Activate();
									if (!ConfigIni.PreAssetsLoading) {
										stageタイトル.CreateManagedResource();
										stageタイトル.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;
									r現在のステージ = stageタイトル;

									CSongSelectSongManager.stopSong();
									CSongSelectSongManager.enable();

									if (ConfigIni.bAIBattleMode == true) {
										ConfigIni.nPlayerCount = ConfigIni.nPreviousPlayerCount;
										ConfigIni.bAIBattleMode = false;
									}

									this.tガベージコレクションを実行する();
									break;
								//-----------------------------
								#endregion

								case (int)CStage選曲.E戻り値.選曲した:
									#region [ *** ]
									//-----------------------------
									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}
									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ Song Loading");
									stage曲読み込み.Activate();
									if (!ConfigIni.PreAssetsLoading) {
										stage曲読み込み.CreateManagedResource();
										stage曲読み込み.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;
									r現在のステージ = stage曲読み込み;

									/*
									Skin.bgm選曲画面イン.t停止する();
									Skin.bgm選曲画面.t停止する();
									*/
									CSongSelectSongManager.stopSong();
									CSongSelectSongManager.enable();

									this.tガベージコレクションを実行する();
									break;
								//-----------------------------
								#endregion

								//							case (int) CStage選曲.E戻り値.オプション呼び出し:
								#region [ *** ]
								//								//-----------------------------
								//								r現在のステージ.On非活性化();
								//								Trace.TraceInformation( "----------------------" );
								//								Trace.TraceInformation( "■ オプション" );
								//								stageオプション.On活性化();
								//								r直前のステージ = r現在のステージ;
								//								r現在のステージ = stageオプション;
								//
								//								foreach( STPlugin pg in this.listプラグイン )
								//								{
								//									Directory.SetCurrentDirectory( pg.strプラグインフォルダ );
								//									pg.plugin.Onステージ変更();
								//									Directory.SetCurrentDirectory( CDTXMania.strEXEのあるフォルダ );
								//								}
								//
								//								this.tガベージコレクションを実行する();
								//								break;
								//							//-----------------------------
								#endregion

								case (int)CStage選曲.E戻り値.コンフィグ呼び出し:
									#region [ *** ]
									//-----------------------------
									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}
									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ Config");
									stageコンフィグ.Activate();
									if (!ConfigIni.PreAssetsLoading) {
										stageコンフィグ.CreateManagedResource();
										stageコンフィグ.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;
									r現在のステージ = stageコンフィグ;

									CSongSelectSongManager.stopSong();
									CSongSelectSongManager.enable();

									this.tガベージコレクションを実行する();
									break;
								//-----------------------------
								#endregion

								case (int)CStage選曲.E戻り値.スキン変更:

									#region [ *** ]
									//-----------------------------
									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}
									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ Skin Change");
									stageChangeSkin.Activate();
									r直前のステージ = r現在のステージ;
									r現在のステージ = stageChangeSkin;
									break;
									//-----------------------------
									#endregion
							}
							//-----------------------------
							#endregion
							break;

						case CStage.EStage.DanDojoSelect:
							#region [ *** ]
							switch (this.n進行描画の戻り値) {
								case (int)CStage選曲.E戻り値.タイトルに戻る:
									#region [ *** ]
									//-----------------------------
									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}
									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ Title");
									stageタイトル.Activate();
									if (!ConfigIni.PreAssetsLoading) {
										stageタイトル.CreateManagedResource();
										stageタイトル.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;
									r現在のステージ = stageタイトル;

									/*
									Skin.bgm選曲画面イン.t停止する();
									Skin.bgm選曲画面.t停止する();
									*/
									CSongSelectSongManager.stopSong();
									CSongSelectSongManager.enable();

									this.tガベージコレクションを実行する();
									break;
								//-----------------------------
								#endregion

								case (int)CStage選曲.E戻り値.選曲した:
									#region [ *** ]
									//-----------------------------

									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}
									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ Song Loading");
									stage曲読み込み.Activate();
									if (!ConfigIni.PreAssetsLoading) {
										stage曲読み込み.CreateManagedResource();
										stage曲読み込み.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;
									r現在のステージ = stage曲読み込み;

									this.tガベージコレクションを実行する();
									break;
									//-----------------------------
									#endregion
							}
							#endregion
							break;

						case CStage.EStage.Heya:
							#region [ *** ]
							switch (this.n進行描画の戻り値) {
								case (int)CStage選曲.E戻り値.タイトルに戻る:
									#region [ *** ]
									//-----------------------------
									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}
									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ Title");
									stageタイトル.Activate();
									if (!ConfigIni.PreAssetsLoading) {
										stageタイトル.CreateManagedResource();
										stageタイトル.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;
									r現在のステージ = stageタイトル;

									CSongSelectSongManager.stopSong();
									CSongSelectSongManager.enable();

									this.tガベージコレクションを実行する();
									break;
									//-----------------------------
									#endregion
							}
							#endregion
							break;

						case CStage.EStage.SongLoading:
							#region [ *** ]
							//-----------------------------
							if (this.n進行描画の戻り値 != 0) {
								OpenTaiko.Pad.st検知したデバイス.Clear();  // 入力デバイスフラグクリア(2010.9.11)
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) {
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								#region [ ESC押下時は、曲の読み込みを中止して選曲画面に戻る ]
								if (this.n進行描画の戻り値 == (int)ESongLoadingScreenReturnValue.LoadCanceled) {
									//DTX.t全チップの再生停止();
									if (DTX != null) {
										DTX.DeActivate();
										DTX.ReleaseManagedResource();
										DTX.ReleaseUnmanagedResource();
									}

									// ???

									/*
									if (stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan)
									{
										Trace.TraceInformation("----------------------");
										Trace.TraceInformation("■ 段位選択");
										stage段位選択.On活性化();
										r直前のステージ = r現在のステージ;
										r現在のステージ = stage段位選択;
									}
									else
									{
										Trace.TraceInformation("----------------------");
										Trace.TraceInformation("■ 選曲");
										stage選曲.On活性化();
										r直前のステージ = r現在のステージ;
										r現在のステージ = stage選曲;
									}
									*/

									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ Return to song select menu");
									OpenTaiko.latestSongSelect.Activate();
									if (!ConfigIni.PreAssetsLoading) {
										OpenTaiko.latestSongSelect.CreateManagedResource();
										OpenTaiko.latestSongSelect.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;

									// Seek latest registered song select screen
									r現在のステージ = OpenTaiko.latestSongSelect;

									break;
								}
								#endregion

								Trace.TraceInformation("----------------------");
								Trace.TraceInformation("■ Gameplay (Drum Screen)");
#if false      // #23625 2011.1.11 Config.iniからダメージ/回復値の定数変更を行う場合はここを有効にする 087リリースに合わせ機能無効化
for (int i = 0; i < 5; i++)
{
	for (int j = 0; j < 2; j++)
	{
		stage演奏ドラム画面.fDamageGaugeDelta[i, j] = ConfigIni.fGaugeFactor[i, j];
	}
}
for (int i = 0; i < 3; i++) {
	stage演奏ドラム画面.fDamageLevelFactor[i] = ConfigIni.fDamageLevelFactor[i];
}
#endif
								r直前のステージ = r現在のステージ;
								r現在のステージ = stage演奏ドラム画面;

								this.tガベージコレクションを実行する();
							}
							//-----------------------------
							#endregion
							break;

						case CStage.EStage.Game:
							#region [ *** ]

							switch (this.n進行描画の戻り値) {
								case (int)EGameplayScreenReturnValue.ReloadAndReplay:
									#region [ DTXファイルを再読み込みして、再演奏 ]
									DTX.t全チップの再生停止();
									DTX.DeActivate();
									DTX.ReleaseManagedResource();
									DTX.ReleaseUnmanagedResource();
									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}
									stage曲読み込み.Activate();
									if (!ConfigIni.PreAssetsLoading) {
										stage曲読み込み.CreateManagedResource();
										stage曲読み込み.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;
									r現在のステージ = stage曲読み込み;
									this.tガベージコレクションを実行する();
									break;
								#endregion

								//case (int) E演奏画面の戻り値.再演奏:
								#region [ 再読み込み無しで、再演奏 ]
								#endregion
								//	break;

								case (int)EGameplayScreenReturnValue.Continue:
									break;

								case (int)EGameplayScreenReturnValue.PerformanceInterrupted:
									#region [ 演奏キャンセル ]
									//-----------------------------

									DTX.t全チップの再生停止();
									DTX.DeActivate();
									DTX.ReleaseManagedResource();
									DTX.ReleaseUnmanagedResource();
									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}

									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ Return to song select menu");
									OpenTaiko.latestSongSelect.Activate();
									if (!ConfigIni.PreAssetsLoading) {
										OpenTaiko.latestSongSelect.CreateManagedResource();
										OpenTaiko.latestSongSelect.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;

									// Seek latest registered song select screen
									r現在のステージ = OpenTaiko.latestSongSelect;

									this.tガベージコレクションを実行する();
									this.tガベージコレクションを実行する();
									break;
								//-----------------------------
								#endregion

								case (int)EGameplayScreenReturnValue.StageFailed:
									#region [ 演奏失敗(StageFailed) ]
									//-----------------------------

									DTX.t全チップの再生停止();
									DTX.DeActivate();
									DTX.ReleaseManagedResource();
									DTX.ReleaseUnmanagedResource();
									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}

									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ Song Select");
									stageSongSelect.Activate();
									if (!ConfigIni.PreAssetsLoading) {
										stageSongSelect.CreateManagedResource();
										stageSongSelect.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;
									r現在のステージ = stageSongSelect;

									this.tガベージコレクションを実行する();
									break;
								//-----------------------------
								#endregion

								case (int)EGameplayScreenReturnValue.StageCleared:
									#region [ 演奏クリア ]
									//-----------------------------

									// Fetch the results of the finished play
									CScoreIni.C演奏記録 c演奏記録_Drums;
									stage演奏ドラム画面.t演奏結果を格納する(out c演奏記録_Drums);

									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}
									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ Results");
									stage結果.st演奏記録.Drums = c演奏記録_Drums;
									stage結果.Activate();
									if (!ConfigIni.PreAssetsLoading) {
										stage結果.CreateManagedResource();
										stage結果.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;
									r現在のステージ = stage結果;

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
							if (this.n進行描画の戻り値 != 0) {
								//DTX.t全チップの再生一時停止();
								DTX.t全チップの再生停止とミキサーからの削除();
								DTX.DeActivate();
								DTX.ReleaseManagedResource();
								DTX.ReleaseUnmanagedResource();
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) {
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								this.tガベージコレクションを実行する();


								// After result screen

								/*
								if (stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan)
								{
									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ 段位選択");
									stage段位選択.On活性化();
									r直前のステージ = r現在のステージ;
									r現在のステージ = stage段位選択;
								}
								else
								{
									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ 選曲");
									stage選曲.On活性化();
									r直前のステージ = r現在のステージ;
									r現在のステージ = stage選曲;
								}
								*/

								Trace.TraceInformation("----------------------");
								Trace.TraceInformation("■ Return to song select menu");
								OpenTaiko.latestSongSelect.Activate();
								if (!ConfigIni.PreAssetsLoading) {
									OpenTaiko.latestSongSelect.CreateManagedResource();
									OpenTaiko.latestSongSelect.CreateUnmanagedResource();
								}
								r直前のステージ = r現在のステージ;

								// Seek latest registered song select screen
								r現在のステージ = OpenTaiko.latestSongSelect;

								stageSongSelect.NowSong++;

								this.tガベージコレクションを実行する();
							}
							//-----------------------------
							#endregion
							break;


						case CStage.EStage.TaikoTowers:
							#region [ *** ]
							switch (this.n進行描画の戻り値) {
								case (int)EReturnValue.ReturnToTitle:
									#region [ *** ]
									//-----------------------------
									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}
									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ Title");
									stageタイトル.Activate();
									r直前のステージ = r現在のステージ;
									r現在のステージ = stageタイトル;

									/*
									Skin.bgm選曲画面イン.t停止する();
									Skin.bgm選曲画面.t停止する();
									*/
									CSongSelectSongManager.stopSong();
									CSongSelectSongManager.enable();

									this.tガベージコレクションを実行する();
									break;
								//-----------------------------
								#endregion

								case (int)EReturnValue.SongChoosen:
									#region [ *** ]
									//-----------------------------
									latestSongSelect = stageTowerSelect;
									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}
									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ Song Loading");
									stage曲読み込み.Activate();
									if (!ConfigIni.PreAssetsLoading) {
										stage曲読み込み.CreateManagedResource();
										stage曲読み込み.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;
									r現在のステージ = stage曲読み込み;

									this.tガベージコレクションを実行する();
									break;
									//-----------------------------
									#endregion
							}
							#endregion
							break;

						case CStage.EStage.ChangeSkin:
							#region [ *** ]
							//-----------------------------
							if (this.n進行描画の戻り値 != 0) {
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) {
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation("----------------------");
								Trace.TraceInformation("■ Song Select");
								stageSongSelect.Activate();
								if (!ConfigIni.PreAssetsLoading) {
									stageSongSelect.CreateManagedResource();
									stageSongSelect.CreateUnmanagedResource();
								}
								r直前のステージ = r現在のステージ;
								r現在のステージ = stageSongSelect;
								this.tガベージコレクションを実行する();
							}
							//-----------------------------
							#endregion
							break;

						case CStage.EStage.End:
							#region [ *** ]
							//-----------------------------
							if (this.n進行描画の戻り値 != 0) {
								base.Exit();
								return;
							}
							//-----------------------------
							#endregion
							break;

						default:
							#region [ *** ]
							switch (this.n進行描画の戻り値) {
								case (int)CStage選曲.E戻り値.タイトルに戻る:
									#region [ *** ]
									//-----------------------------
									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) {
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}
									Trace.TraceInformation("----------------------");
									Trace.TraceInformation("■ Title");
									stageタイトル.Activate();
									if (!ConfigIni.PreAssetsLoading) {
										stageタイトル.CreateManagedResource();
										stageタイトル.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;
									r現在のステージ = stageタイトル;

									CSongSelectSongManager.stopSong();
									CSongSelectSongManager.enable();

									this.tガベージコレクションを実行する();
									break;
									//-----------------------------
									#endregion
							}
							#endregion
							break;
					}

					actScanningLoudness?.Draw();

					if (!ConfigIni.bTokkunMode) {
						float screen_ratiox = OpenTaiko.Skin.Resolution[0] / 1280.0f;
						float screen_ratioy = OpenTaiko.Skin.Resolution[1] / 720.0f;

						Camera *= Matrix4X4.CreateScale(fCamXScale, fCamYScale, 1f);

						Camera *= Matrix4X4.CreateScale(1.0f / ScreenAspect, 1.0f, 1.0f) *
						Matrix4X4.CreateRotationZ(CConversion.DegreeToRadian(fCamRotation)) *
						Matrix4X4.CreateScale(1.0f * ScreenAspect, 1.0f, 1.0f);

						Camera *= Matrix4X4.CreateTranslation(fCamXOffset / 1280, fCamYOffset / 720, 1f);

						if (OpenTaiko.DTX != null) {
							//object rendering
							foreach (KeyValuePair<string, CSongObject> pair in OpenTaiko.DTX.listObj) {
								pair.Value.tDraw();
							}
						}

						Camera = Matrix4X4<float>.Identity;
					}

					if (r現在のステージ != null && r現在のステージ.eStageID != CStage.EStage.StartUp && OpenTaiko.Tx.Network_Connection != null) {
						if (Math.Abs(SoundManager.PlayTimer.SystemTimeMs - this.前回のシステム時刻ms) > 10000) {
							this.前回のシステム時刻ms = SoundManager.PlayTimer.SystemTimeMs;
							Task.Factory.StartNew(() => {
								//IPv4 8.8.8.8にPingを送信する(timeout 5000ms)
								PingReply reply = new Ping().Send("8.8.8.8", 5000);
								this.bネットワークに接続中 = reply.Status == IPStatus.Success;
							});
						}
						OpenTaiko.Tx.Network_Connection.t2D描画(GameWindowSize.Width - (OpenTaiko.Tx.Network_Connection.szTextureSize.Width / 2), GameWindowSize.Height - OpenTaiko.Tx.Network_Connection.szTextureSize.Height, new Rectangle((OpenTaiko.Tx.Network_Connection.szTextureSize.Width / 2) * (this.bネットワークに接続中 ? 0 : 1), 0, OpenTaiko.Tx.Network_Connection.szTextureSize.Width / 2, OpenTaiko.Tx.Network_Connection.szTextureSize.Height));
					}
					// オーバレイを描画する(テクスチャの生成されていない起動ステージは例外

					// Display log cards
					VisualLogManager.Display();

					if (r現在のステージ != null && r現在のステージ.eStageID != CStage.EStage.StartUp && OpenTaiko.Tx.Overlay != null) {
						OpenTaiko.Tx.Overlay.t2D描画(0, 0);
					}
				}

				if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.System.Capture)) {
#if DEBUG
					if (OpenTaiko.InputManager.Keyboard.KeyPressing((int)SlimDXKeys.Key.LeftControl)) {
						if (r現在のステージ.eStageID != CStage.EStage.Game) {
							RefreshSkin();
							r現在のステージ.DeActivate();
							if (!ConfigIni.PreAssetsLoading) {
								r現在のステージ.ReleaseManagedResource();
								r現在のステージ.ReleaseUnmanagedResource();
							}
							r現在のステージ.Activate();
							if (!ConfigIni.PreAssetsLoading) {
								r現在のステージ.CreateManagedResource();
								r現在のステージ.CreateUnmanagedResource();
							}
						}
					} else {
						// Debug.WriteLine( "capture: " + string.Format( "{0:2x}", (int) e.KeyCode ) + " " + (int) e.KeyCode );
						string strFullPath =
						   Path.Combine(OpenTaiko.strEXEのあるフォルダ, "Capture_img");
						strFullPath = Path.Combine(strFullPath, DateTime.Now.ToString("yyyyMMddHHmmss") + ".png");
						SaveResultScreen(strFullPath);
					}
#else
					string strFullPath =
						Path.Combine(OpenTaiko.strEXEのあるフォルダ, "Capture_img");
					strFullPath = Path.Combine(strFullPath, DateTime.Now.ToString("yyyyMMddHHmmss") + ".png");
					SaveResultScreen(strFullPath);
#endif
				}

				#region [ 全画面_ウインドウ切り替え ]
				if (this.b次のタイミングで全画面_ウィンドウ切り替えを行う) {
					ConfigIni.bFullScreen = !ConfigIni.bFullScreen;
					app.ToggleWindowMode();
					this.b次のタイミングで全画面_ウィンドウ切り替えを行う = false;
				}
				#endregion
				#region [ 垂直基線同期切り替え ]
				if (this.b次のタイミングで垂直帰線同期切り替えを行う) {
					VSync = ConfigIni.bEnableVSync;
					this.b次のタイミングで垂直帰線同期切り替えを行う = false;
				}
				#endregion

#if DEBUG
				if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.F11))
					OpenTaiko.ConfigIni.DEBUG_bShowImgui = !OpenTaiko.ConfigIni.DEBUG_bShowImgui;
				if (OpenTaiko.ConfigIni.DEBUG_bShowImgui)
					ImGuiDebugWindow.Draw();
#endif
			}
#if !DEBUG
			catch( Exception e )
			{
				Trace.WriteLine( "" );
				Trace.Write( e.ToString() );
				Trace.WriteLine( "" );
				Trace.WriteLine( "An error has occured." );
                AssemblyName asmApp = Assembly.GetExecutingAssembly().GetName();
				throw e;
			}
#endif
		}

		// その他

		#region [ 汎用ヘルパー ]
		//-----------------
		public static CTexture tテクスチャの生成(string fileName) {
			return tテクスチャの生成(fileName, false);
		}
		public static CTexture tテクスチャの生成(string fileName, bool b黒を透過する) {
			if (app == null) {
				return null;
			}
			try {
				return new CTexture(fileName, b黒を透過する);
			} catch (CTextureCreateFailedException e) {
				Trace.TraceError(e.ToString());
				Trace.TraceError("Texture generation has failed. ({0})", fileName);
				return null;
			} catch (FileNotFoundException) {
				Trace.TraceWarning("Could not find specified texture file. ({0})", fileName);
				return null;
			}
		}
		public static void tテクスチャの解放(ref CTexture tx) {
			OpenTaiko.tDisposeSafely(ref tx);
		}
		public static void tテクスチャの解放(ref CTextureAf tx) {
			OpenTaiko.tDisposeSafely(ref tx);
		}
		public static CTexture tテクスチャの生成(SKBitmap bitmap) {
			return tテクスチャの生成(bitmap, false);
		}
		public static CTexture tテクスチャの生成(SKBitmap bitmap, bool b黒を透過する) {
			if (app == null) {
				return null;
			}
			if (bitmap == null) {
				Trace.TraceError("Texture generation has failed. (bitmap==null)");
				return null;
			}
			try {
				return new CTexture(bitmap, b黒を透過する);
			} catch (CTextureCreateFailedException e) {
				Trace.TraceError(e.ToString());
				Trace.TraceError("Texture generation has failed. (txData)");
				return null;
			}
		}

		public static CTextureAf tテクスチャの生成Af(string fileName) {
			return tテクスチャの生成Af(fileName, false);
		}
		public static CTextureAf tテクスチャの生成Af(string fileName, bool b黒を透過する) {
			if (app == null) {
				return null;
			}
			try {
				return new CTextureAf(fileName, b黒を透過する);
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
		public static void tDisposeSafely<T>(ref T obj) {
			if (obj == null)
				return;

			var d = obj as IDisposable;

			if (d != null)
				d.Dispose();

			obj = default(T);
		}

		public static void t安全にDisposeする<T>(ref T[] array) where T : class, IDisposable //2020.08.01 Mr-Ojii twopointzero氏のソースコードをもとに追加
		{
			if (array == null) {
				return;
			}

			for (var i = 0; i < array.Length; i++) {
				array[i]?.Dispose();
				array[i] = null;
			}
		}

		/// <summary>
		/// そのフォルダの連番画像の最大値を返す。
		/// </summary>
		public static int t連番画像の枚数を数える(string ディレクトリ名, string プレフィックス = "", string 拡張子 = ".png") {
			int num = 0;
			while (File.Exists(ディレクトリ名 + プレフィックス + num + 拡張子)) {
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
		private bool bマウスカーソル表示中 = true;
		private bool b終了処理完了済み;
		public bool bネットワークに接続中 { get; private set; } = false;
		private long 前回のシステム時刻ms = long.MinValue;
		private static CDTX[] dtx = new CDTX[5];

		public static TextureLoader Tx = new TextureLoader();

		public List<CActivity> listトップレベルActivities;
		private int n進行描画の戻り値;
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

		private void t起動処理() {

			#region [ Read Config.ini and Database files ]
			//---------------------

			// Port <= 0.5.4 NamePlate.json to Pre 0.6.0 b1 Saves\
			NamePlateConfig = new NamePlateConfig();
			NamePlateConfig.tNamePlateConfig();

			Favorites = new Favorites();
			Favorites.tFavorites();

			RecentlyPlayedSongs = new RecentlyPlayedSongs();
			RecentlyPlayedSongs.tRecentlyPlayedSongs();

			Databases = new Databases();
			Databases.tDatabases();

			VisualLogManager = new CVisualLogManager();

			if (!File.Exists("Saves.db3")) {
				File.Copy(@$".init{Path.DirectorySeparatorChar}Saves.db3", "Saves.db3");
			}
			// Add a condition here (if old Saves\ format save files exist) to port them to database (?)
			SaveFileInstances = DBSaves.FetchSaveInstances();

			//---------------------
			#endregion

			#region [ ログ出力開始 ]
			//---------------------
			Trace.AutoFlush = true;
			if (ConfigIni.bOutputLogs) {
				try {
					Trace.Listeners.Add(new CTraceLogListener(new StreamWriter(System.IO.Path.Combine(strEXEのあるフォルダ, "OpenTaiko.log"), false, Encoding.GetEncoding(OpenTaiko.sEncType))));
				} catch (System.UnauthorizedAccessException)            // #24481 2011.2.20 yyagi
				  {
					int c = (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja") ? 0 : 1;
					string[] mes_writeErr = {
						"OpenTaiko.logへの書き込みができませんでした。書き込みできるようにしてから、再度起動してください。",
						"Failed to write OpenTaiko.log. Please set your device to READ/WRITE and try again."
					};
					Environment.Exit(1);
				}
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
			//---------------------
			#endregion

			DTX = null;

			#region [ Skin の初期化 ]
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
			catch (Exception e)
			{
				Trace.TraceInformation( "Skin failed to initialize." );
				throw;
			}
			finally
			{
				Trace.Unindent();
			}
#endif

			//---------------------
			#endregion
			//-----------
			#region [ Timer の初期化 ]
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

			#region [ FPS カウンタの初期化 ]
			//---------------------
			Trace.TraceInformation("Initializing FPS counter...");
			Trace.Indent();
			try {
				FPS = new CFPS();
				Trace.TraceInformation("FPS counter initialized.");
			} finally {
				Trace.Unindent();
			}
			//---------------------
			#endregion
			#region [ act文字コンソールの初期化 ]
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
			#region [ Input管理 の初期化 ]
			//---------------------
			Trace.TraceInformation("Initializing DirectInput and MIDI input...");
			Trace.Indent();
			try {
				bool bUseMIDIIn = true;
				InputManager = new CInputManager(Window_);
				foreach (IInputDevice device in InputManager.InputDevices) {
					if ((device.CurrentType == InputDeviceType.Joystick) && !ConfigIni.dicJoystick.ContainsValue(device.GUID)) {
						int key = 0;
						while (ConfigIni.dicJoystick.ContainsKey(key)) {
							key++;
						}
						ConfigIni.dicJoystick.Add(key, device.GUID);
					} else if ((device.CurrentType == InputDeviceType.Gamepad) && !ConfigIni.dicGamepad.ContainsValue(device.GUID)) {
						int key = 0;
						while (ConfigIni.dicGamepad.ContainsKey(key)) {
							key++;
						}
						ConfigIni.dicGamepad.Add(key, device.GUID);
					}
				}
				foreach (IInputDevice device2 in InputManager.InputDevices) {
					if (device2.CurrentType == InputDeviceType.Joystick) {
						foreach (KeyValuePair<int, string> pair in ConfigIni.dicJoystick) {
							if (device2.GUID.Equals(pair.Value)) {
								((CInputJoystick)device2).SetID(pair.Key);
								break;
							}
						}
						continue;
					} else if (device2.CurrentType == InputDeviceType.Gamepad) {
						foreach (KeyValuePair<int, string> pair in ConfigIni.dicGamepad) {
							if (device2.GUID.Equals(pair.Value)) {
								((CInputGamepad)device2).SetID(pair.Key);
								break;
							}
						}
						continue;
					}
				}
				Trace.TraceInformation("DirectInput has been initialized.");
			} catch (Exception exception2) {
				Trace.TraceError("DirectInput and MIDI input failed to initialize.");
				throw;
			} finally {
				Trace.Unindent();
			}
			//---------------------
			#endregion
			#region [ Pad の初期化 ]
			//---------------------
			Trace.TraceInformation("Initialize pad...");
			Trace.Indent();
			try {
				Pad = new CPad(ConfigIni, InputManager);
				Trace.TraceInformation("Pad has been initialized.");
			} catch (Exception exception3) {
				Trace.TraceError(exception3.ToString());
				Trace.TraceError("Pad failed to initialize.");
			} finally {
				Trace.Unindent();
			}
			//---------------------
			#endregion
			#region [ Sound管理 の初期化 ]
			//---------------------
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
					actScanningLoudness.Activate();
					if (!ConfigIni.PreAssetsLoading) {
						actScanningLoudness.CreateManagedResource();
						actScanningLoudness.CreateUnmanagedResource();
					}
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

				ShowWindowTitleWithSoundType();
				FDK.SoundManager.bIsTimeStretch = OpenTaiko.ConfigIni.bTimeStretch;
				SoundManager.nMasterVolume = OpenTaiko.ConfigIni.nMasterVolume;
				Trace.TraceInformation("サウンドデバイスの初期化を完了しました。");
			} catch (Exception e) {
				throw new NullReferenceException("No sound devices are enabled. Please check your audio settings.", e);
			} finally {
				Trace.Unindent();
			}
			//---------------------
			#endregion
			#region [ Songs管理 の初期化 ]
			//---------------------
			Trace.TraceInformation("Initializing song list...");
			Trace.Indent();
			try {
				Songs管理 = new CSongs管理();
				//				Songs管理_裏読 = new CSongs管理();
				EnumSongs = new CEnumSongs();
				actEnumSongs = new CActEnumSongs();
				Trace.TraceInformation("Song list initialized.");
			} catch (Exception e) {
				Trace.TraceError(e.ToString());
				Trace.TraceError("Song list failed to initialize.");
			} finally {
				Trace.Unindent();
			}
			//---------------------
			#endregion
			#region [ Random の初期化 ]
			//---------------------
			Random = new Random();
			//---------------------
			#endregion
			#region [ Stages initialisation ]
			//---------------------
			r現在のステージ = null;
			r直前のステージ = null;
			stage起動 = new CStage起動();
			stageタイトル = new CStageタイトル();
			stageコンフィグ = new CStageコンフィグ();
			stageSongSelect = new CStage選曲();
			stage段位選択 = new CStage段位選択();
			stageHeya = new CStageHeya();
			stageOnlineLounge = new CStageOnlineLounge();
			stageTowerSelect = new CStageTowerSelect();
			stage曲読み込み = new CStage曲読み込み();
			stage演奏ドラム画面 = new CStage演奏ドラム画面();
			stage結果 = new CStage結果();
			stage結果.RefreshSkin();
			stageChangeSkin = new CStageChangeSkin();
			stage終了 = new CStage終了();
			NamePlate = new CNamePlate();
			SaveFile = 0;
			this.listトップレベルActivities = new List<CActivity>();
			this.listトップレベルActivities.Add(actEnumSongs);
			this.listトップレベルActivities.Add(actTextConsole);
			this.listトップレベルActivities.Add(stage起動);
			this.listトップレベルActivities.Add(stageタイトル);
			this.listトップレベルActivities.Add(stageコンフィグ);
			this.listトップレベルActivities.Add(stageSongSelect);
			this.listトップレベルActivities.Add(stage段位選択);
			this.listトップレベルActivities.Add(stageHeya);
			this.listトップレベルActivities.Add(stageOnlineLounge);
			this.listトップレベルActivities.Add(stageTowerSelect);
			this.listトップレベルActivities.Add(stage曲読み込み);
			this.listトップレベルActivities.Add(stage演奏ドラム画面);
			this.listトップレベルActivities.Add(stage結果);
			this.listトップレベルActivities.Add(stageChangeSkin);
			this.listトップレベルActivities.Add(stage終了);
			//---------------------
			#endregion

			#region Discordの処理
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


			Trace.TraceInformation("Application successfully started.");


			#region [ 最初のステージの起動 ]
			//---------------------
			Trace.TraceInformation("----------------------");
			Trace.TraceInformation("■ Startup");

			r現在のステージ = stage起動;
			r現在のステージ.Activate();
			if (!ConfigIni.PreAssetsLoading) {
				r現在のステージ.CreateManagedResource();
				r現在のステージ.CreateUnmanagedResource();
			}

			//---------------------
			#endregion
		}

		public void ShowWindowTitleWithSoundType() {
			string delay = "";
			if (SoundManager.GetCurrentSoundDeviceType() != "DirectSound") {
				delay = "(" + SoundManager.GetSoundDelay() + "ms)";
			}
			AssemblyName asmApp = Assembly.GetExecutingAssembly().GetName();
			base.Text = asmApp.Name + " Ver." + VERSION + " (" + SoundManager.GetCurrentSoundDeviceType() + delay + ")";
		}

		private void t終了処理() {
			if (!this.b終了処理完了済み) {
				Trace.TraceInformation("----------------------");
				Trace.TraceInformation("■ Shutdown");
				#region [ 曲検索の終了処理 ]
				//---------------------

				if (actEnumSongs != null) {
					Trace.TraceInformation("Ending enumeration of songs...");
					Trace.Indent();
					try {
						actEnumSongs.DeActivate();
						actEnumSongs = null;
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
				if (OpenTaiko.r現在のステージ != null && OpenTaiko.r現在のステージ.IsActivated)     // #25398 2011.06.07 MODIFY FROM
				{
					Trace.TraceInformation("Exiting stage...");
					Trace.Indent();
					try {
						r現在のステージ.DeActivate();
						if (!ConfigIni.PreAssetsLoading) {
							r現在のステージ.ReleaseManagedResource();
							r現在のステージ.ReleaseUnmanagedResource();
						}
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
				if (Songs管理 != null) {
					Trace.TraceInformation("Ending song list...");
					Trace.Indent();
					try {
#pragma warning disable SYSLIB0011
						if (EnumSongs.IsSongListEnumCompletelyDone) {
							BinaryFormatter songlistdb_ = new BinaryFormatter();
							using Stream songlistdb = File.OpenWrite($"{OpenTaiko.strEXEのあるフォルダ}songlist.db");
							songlistdb_.Serialize(songlistdb, Songs管理.listSongsDB);
						}
#pragma warning restore SYSLIB0011

						Songs管理 = null;
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
					if (FPS != null) {
						FPS = null;
					}
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
				string str = strEXEのあるフォルダ + "Config.ini";
				Trace.Indent();
				try {
					ConfigIni.t書き出し(str);
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
					actScanningLoudness.DeActivate();
					if (!ConfigIni.PreAssetsLoading) {
						actScanningLoudness.ReleaseManagedResource();
						actScanningLoudness.ReleaseUnmanagedResource();
					}
					actScanningLoudness = null;
				} finally {
					Trace.Unindent();
					Trace.TraceInformation("Deinitialized loudness scanning, song gain control, and sound group level control.");
				}

				ConfigIni = null;

				//---------------------
				#endregion
				Trace.TraceInformation("OpenTaiko has closed down successfully.");
				this.b終了処理完了済み = true;
			}
		}

		private void tガベージコレクションを実行する() {
			GC.Collect(GC.MaxGeneration);
			GC.WaitForPendingFinalizers();
			GC.Collect(GC.MaxGeneration);
		}

		private void ChangeResolution(int nWidth, int nHeight) {
			GameWindowSize.Width = nWidth;
			GameWindowSize.Height = nHeight;

			//WindowSize = new Silk.NET.Maths.Vector2D<int>(nWidth, nHeight);
		}

		public void RefreshSkin() {
			Trace.TraceInformation("Skin Change:" + OpenTaiko.Skin.GetCurrentSkinSubfolderFullName(false));

			OpenTaiko.actTextConsole.DeActivate();
			actTextConsole.ReleaseManagedResource();
			actTextConsole.ReleaseUnmanagedResource();

			OpenTaiko.Skin.Dispose();
			OpenTaiko.Skin = null;
			OpenTaiko.Skin = new CSkin(OpenTaiko.ConfigIni.strSystemSkinSubfolderFullName, false);

			OpenTaiko.Tx.DisposeTexture();

			ChangeResolution(OpenTaiko.Skin.Resolution[0], OpenTaiko.Skin.Resolution[1]);

			OpenTaiko.Tx.LoadTexture();

			OpenTaiko.actTextConsole.Activate();
			actTextConsole.CreateManagedResource();
			actTextConsole.CreateUnmanagedResource();
			OpenTaiko.NamePlate.RefleshSkin();
			OpenTaiko.stage結果.RefreshSkin();
			CActSelectPopupMenu.RefleshSkin();
			CActSelect段位リスト.RefleshSkin();
		}
		#endregion

		#region [ EXTENDED VARIABLES ]
		public static float fCamXOffset;
		public static float fCamYOffset;

		public static float fCamZoomFactor = 1.0f;
		public static float fCamRotation;

		public static float fCamXScale = 1.0f;
		public static float fCamYScale = 1.0f;

		public static Color4 borderColor = new Color4(1f, 0f, 0f, 0f);
		#endregion
	}
}
