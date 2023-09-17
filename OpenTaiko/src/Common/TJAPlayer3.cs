using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Net.NetworkInformation;
using System.Runtime.Serialization.Formatters.Binary;
using FDK;
using SampleFramework;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using Silk.NET.Maths;
using SkiaSharp;
using DiscordRPC;

using Rectangle = System.Drawing.Rectangle;
using Point = System.Drawing.Point;
using Color = System.Drawing.Color;
using System.Runtime.InteropServices;

namespace TJAPlayer3
{
	internal class TJAPlayer3 : Game
	{
		// プロパティ
		#region [ properties ]
		public static readonly string VERSION = Assembly.GetExecutingAssembly().GetName().Version.ToString();//.Substring(0, Assembly.GetExecutingAssembly().GetName().Version.ToString().Length - 2);
		public static readonly string AppDisplayThreePartVersion = GetAppDisplayThreePartVersion();
		public static readonly string AppNumericThreePartVersion = GetAppNumericThreePartVersion();

		private static string GetAppDisplayThreePartVersion()
		{
			return $"v{GetAppNumericThreePartVersion()}";
		}

		private static string GetAppNumericThreePartVersion()
		{
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
		public static readonly string D3DXDLL = "d3dx9_43.dll";		// June 2010
        //public static readonly string D3DXDLL = "d3dx9_42.dll";	// February 2010
        //public static readonly string D3DXDLL = "d3dx9_41.dll";	// March 2009

		public static CStage latestSongSelect
        {
			get;
			private set;
        }

		public static TJAPlayer3 app
		{
			get;
			private set;
		}
		public static C文字コンソール act文字コンソール
		{ 
			get;
			private set;
		}
		public static bool bコンパクトモード
		{
			get;
			private set;
		}
		public static CConfigIni ConfigIni
		{
			get; 
			private set;
		}

		public static CVisualLogManager VisualLogManager
        {
			get;
			private set;
        }

        #region [DTX instances]
        public static CDTX DTX
		{
			get
			{
				return dtx[ 0 ];
			}
			set
			{
				if( ( dtx[ 0 ] != null ) && ( app != null ) )
				{
					dtx[ 0 ].DeActivate();
					dtx[ 0 ].ReleaseManagedResource();
					dtx[ 0 ].ReleaseUnmanagedResource();
					app.listトップレベルActivities.Remove( dtx[ 0 ] );
				}
				dtx[ 0 ] = value;
				if( ( dtx[ 0 ] != null ) && ( app != null ) )
				{
					app.listトップレベルActivities.Add( dtx[ 0 ] );
				}
			}
		}
		public static CDTX DTX_2P
		{
			get
			{
				return dtx[ 1 ];
			}
			set
			{
				if( ( dtx[ 1 ] != null ) && ( app != null ) )
				{
					dtx[ 1 ].DeActivate();
					dtx[ 1 ].ReleaseManagedResource();
					dtx[ 1 ].ReleaseUnmanagedResource();
					app.listトップレベルActivities.Remove( dtx[ 1 ] );
				}
				dtx[ 1 ] = value;
				if( ( dtx[ 1 ] != null ) && ( app != null ) )
				{
					app.listトップレベルActivities.Add( dtx[ 1 ] );
				}
			}
		}
		public static CDTX DTX_3P
		{
			get
			{
				return dtx[2];
			}
			set
			{
				if ((dtx[2] != null) && (app != null))
				{
					dtx[2].DeActivate();
					dtx[2].ReleaseManagedResource();
					dtx[2].ReleaseUnmanagedResource();
					app.listトップレベルActivities.Remove(dtx[2]);
				}
				dtx[2] = value;
				if ((dtx[2] != null) && (app != null))
				{
					app.listトップレベルActivities.Add(dtx[2]);
				}
			}
		}
		public static CDTX DTX_4P
		{
			get
			{
				return dtx[3];
			}
			set
			{
				if ((dtx[3] != null) && (app != null))
				{
					dtx[3].DeActivate();
					dtx[3].ReleaseManagedResource();
					dtx[3].ReleaseUnmanagedResource();
					app.listトップレベルActivities.Remove(dtx[3]);
				}
				dtx[3] = value;
				if ((dtx[3] != null) && (app != null))
				{
					app.listトップレベルActivities.Add(dtx[3]);
				}
			}
		}
		public static CDTX DTX_5P
		{
			get
			{
				return dtx[4];
			}
			set
			{
				if ((dtx[4] != null) && (app != null))
				{
					dtx[4].DeActivate();
					dtx[4].ReleaseManagedResource();
					dtx[4].ReleaseUnmanagedResource();
					app.listトップレベルActivities.Remove(dtx[4]);
				}
				dtx[4] = value;
				if ((dtx[4] != null) && (app != null))
				{
					app.listトップレベルActivities.Add(dtx[4]);
				}
			}
		}

		public static CDTX GetDTX(int player)
        {
			switch (player)
            {
				case 0:
					return TJAPlayer3.DTX;
				case 1:
					return TJAPlayer3.DTX_2P;
				case 2:
					return TJAPlayer3.DTX_3P;
				case 3:
					return TJAPlayer3.DTX_4P;
				case 4:
					return TJAPlayer3.DTX_5P;
			}
			return null;
        }

		#endregion

		public static CSongReplay[] ReplayInstances = new CSongReplay[5];

		public static bool IsPerformingCalibration;

		public static CFPS FPS
		{ 
			get; 
			private set;
		}
		public static CInputManager Input管理 
		{
			get;
			private set;
		}
		#region [ 入力範囲ms ]
		public static int nPerfect範囲ms
		{
			get
			{
				if( stage選曲.r確定された曲 != null )
				{
					C曲リストノード c曲リストノード = stage選曲.r確定された曲.r親ノード;
					if( ( ( c曲リストノード != null ) && ( c曲リストノード.eノード種別 == C曲リストノード.Eノード種別.BOX ) ) && ( c曲リストノード.nPerfect範囲ms >= 0 ) )
					{
						return c曲リストノード.nPerfect範囲ms;
					}
				}
				return ConfigIni.nヒット範囲ms.Perfect;
			}
		}
		public static int nGreat範囲ms
		{
			get
			{
				if( stage選曲.r確定された曲 != null )
				{
					C曲リストノード c曲リストノード = stage選曲.r確定された曲.r親ノード;
					if( ( ( c曲リストノード != null ) && ( c曲リストノード.eノード種別 == C曲リストノード.Eノード種別.BOX ) ) && ( c曲リストノード.nGreat範囲ms >= 0 ) )
					{
						return c曲リストノード.nGreat範囲ms;
					}
				}
				return ConfigIni.nヒット範囲ms.Great;
			}
		}
		public static int nGood範囲ms
		{
			get
			{
				if( stage選曲.r確定された曲 != null )
				{
					C曲リストノード c曲リストノード = stage選曲.r確定された曲.r親ノード;
					if( ( ( c曲リストノード != null ) && ( c曲リストノード.eノード種別 == C曲リストノード.Eノード種別.BOX ) ) && ( c曲リストノード.nGood範囲ms >= 0 ) )
					{
						return c曲リストノード.nGood範囲ms;
					}
				}
				return ConfigIni.nヒット範囲ms.Good;
			}
		}
		public static int nPoor範囲ms
		{
			get
			{
				if( stage選曲.r確定された曲 != null )
				{
					C曲リストノード c曲リストノード = stage選曲.r確定された曲.r親ノード;
					if( ( ( c曲リストノード != null ) && ( c曲リストノード.eノード種別 == C曲リストノード.Eノード種別.BOX ) ) && ( c曲リストノード.nPoor範囲ms >= 0 ) )
					{
						return c曲リストノード.nPoor範囲ms;
					}
				}
				return ConfigIni.nヒット範囲ms.Poor;
			}
		}
		#endregion
		public static CPad Pad 
		{
			get;
			private set;
		}
		public static Random Random
		{
			get;
			private set;
		}
		public static CSkin Skin
		{
			get; 
			private set;
		}
		public static CSongs管理 Songs管理 
		{
			get;
			set;	// 2012.1.26 yyagi private解除 CStage起動でのdesirialize読み込みのため
		}
		public static CEnumSongs EnumSongs
		{
			get;
			private set;
		}
		public static CActEnumSongs actEnumSongs
		{
			get;
			private set;
		}
		public static CActScanningLoudness actScanningLoudness
		{
			get;
			private set;
		}

		public static SoundManager Sound管理
		{
			get;
			private set;
		}

	    public static SongGainController SongGainController
	    {
	        get;
	        private set;
	    }

	    public static SoundGroupLevelController SoundGroupLevelController
	    {
	        get;
	        private set;
	    }

		public static CNamePlate NamePlate 
		{
			get; 
			private set;
		}

		public static NamePlateConfig NamePlateConfig
		{
			get; 
			private set;
		}

		public static Favorites Favorites
        {
			get;
			private set;
        }

		public static RecentlyPlayedSongs RecentlyPlayedSongs
        {
			get;
			private set;
        }

		public static Databases Databases
		{
			get;
			private set;
		}

		public static CStage起動 stage起動 
		{
			get; 
			private set;
		}
		public static CStageタイトル stageタイトル
		{
			get;
			private set;
		}
//		public static CStageオプション stageオプション
//		{ 
//			get;
//			private set;
//		}
		public static CStageコンフィグ stageコンフィグ 
		{ 
			get; 
			private set;
		}
		public static CStage選曲 stage選曲
		{
			get;
			private set;
		}
		public static CStage段位選択 stage段位選択
		{
			get;
			private set;
		}
		public static CStageHeya stageHeya
		{
			get;
			private set;
		}

		public static CStageOnlineLounge stageOnlineLounge
		{
			get;
			private set;
		}

		public static CStageTowerSelect stageTowerSelect
		{
			get;
			private set;
		}

		public static COpenEncyclopedia stageOpenEncyclopedia
		{
			get;
			private set;
		}
		public static CStage曲読み込み stage曲読み込み
		{
			get;
			private set;
		}
		public static CStage演奏ドラム画面 stage演奏ドラム画面
		{
			get;
			private set;
		}
		public static CStage結果 stage結果
		{
			get;
			private set;
		}
		public static CStageChangeSkin stageChangeSkin
		{
			get;
			private set;
		}
		public static CStage終了 stage終了
		{
			get;
			private set;
		}
		public static CStage r現在のステージ = null;
		public static CStage r直前のステージ = null;
		public static string strEXEのあるフォルダ 
		{
			get;
			private set;
		}
		public static string strコンパクトモードファイル
		{ 
			get; 
			private set;
		}
		public static CTimer Timer
		{
			get;
			private set;
		}
		internal static IPluginActivity act現在入力を占有中のプラグイン = null;
		public bool b次のタイミングで垂直帰線同期切り替えを行う
		{
			get; 
			set;
		}
		public bool b次のタイミングで全画面_ウィンドウ切り替えを行う
		{
			get;
			set;
		}
		public CPluginHost PluginHost
		{
			get;
			private set;
		}
		public List<STPlugin> PluginList = new List<STPlugin>();
		public struct STPlugin
		{
			public IPluginActivity plugin;
			public string pluginDirectory;
			public string assemblyName;
			public Version Version;
		}
		private static Size currentClientSize		// #23510 2010.10.27 add yyagi to keep current window size
		{
			get;
			set;
		}
		public static CDTXVmode DTXVmode			// #28821 2014.1.23 yyagi
		{
			get;
			set;
		}
		public static DiscordRpcClient DiscordClient;

		// 0 : 1P, 1 : 2P
		public static int SaveFile = 0;

		public static SaveFile[] SaveFileInstances = new SaveFile[5];

		// 0 : Hidari, 1 : Migi (1P only)
		public static int PlayerSide = 0;

		public static int GetActualPlayer(int player)
        {
			if (SaveFile == 0 || player > 1)
				return player;
			if (player == 0)
				return 1;
			return 0;
        }

		public static bool P1IsBlue()
        {
			return (TJAPlayer3.PlayerSide == 1 && TJAPlayer3.ConfigIni.nPlayerCount == 1);
		}

        #endregion

        // コンストラクタ

        public TJAPlayer3()
		{
			TJAPlayer3.app = this;
		}

		public static string sEncType = "Shift_JIS";

		public static string LargeImageKey
		{
			get
			{
				return "opentaiko";
			}
		}
		
		public static string LargeImageText
		{
			get
			{
				return "Ver." + Assembly.GetExecutingAssembly().GetName().Version.ToString() + "(" + RuntimeInformation.RuntimeIdentifier + ")";
			}
		}

		public static CCounter BeatScaling;

		

		// メソッド


		#region [ #24609 リザルト画像をpngで保存する ]		// #24609 2011.3.14 yyagi; to save result screen in case BestRank or HiSkill.
		/// <summary>
		/// リザルト画像のキャプチャと保存。
		/// </summary>
		/// <param name="strFilename">保存するファイル名(フルパス)</param>
		public bool SaveResultScreen( string strFullPath )
		{
			string strSavePath = Path.GetDirectoryName( strFullPath );
			if ( !Directory.Exists( strSavePath ) )
			{
				try
				{
					Directory.CreateDirectory( strSavePath );
				}
				catch
				{
					Trace.TraceError(ToString());
					Trace.TraceError( "例外が発生しましたが処理を継続します。 (0bfe6bff-2a56-4df4-9333-2df26d9b765b)" );
					return false;
				}
			}

			using SKBitmap sKBitmap = GetScreenShot();
			using FileStream stream = File.OpenWrite(strFullPath);
			return sKBitmap.Encode(stream, SKEncodedImageFormat.Png, 80);
		}
		#endregion

		// Game 実装
		

        protected override void Configuration()
        {
			#region [ strEXEのあるフォルダを決定する ]
			//-----------------
            strEXEのあるフォルダ = Environment.CurrentDirectory + Path.DirectorySeparatorChar;
			// END #23629 2010.11.13 from
			//-----------------
			#endregion
			
			ConfigIni = new CConfigIni();
			
			string path = strEXEのあるフォルダ + "Config.ini";
			if( File.Exists( path ) )
			{
				try
				{
					// Load config info
					ConfigIni.tファイルから読み込み( path );
				}
				catch (Exception e)
				{
					//ConfigIni = new CConfigIni();	// 存在してなければ新規生成
					Trace.TraceError( e.ToString() );
					Trace.TraceError( "例外が発生しましたが処理を継続します。 (b8d93255-bbe4-4ca3-8264-7ee5175b19f3)" );
				}
			}
			

			GraphicsDeviceType_ = (GraphicsDeviceType)ConfigIni.nGraphicsDeviceType;
			WindowPosition = new Silk.NET.Maths.Vector2D<int>(ConfigIni.n初期ウィンドウ開始位置X, ConfigIni.n初期ウィンドウ開始位置Y);
			WindowSize = new Silk.NET.Maths.Vector2D<int>(ConfigIni.nウインドウwidth, ConfigIni.nウインドウheight);
			FullScreen = ConfigIni.b全画面モード;
			VSync = ConfigIni.b垂直帰線待ちを行う;
			Framerate = 0;
			
			base.Configuration();
        }

		protected override void Initialize()
		{

			this.t起動処理();

			/*
			if ( this.listトップレベルActivities != null )
			{
				foreach( CActivity activity in this.listトップレベルActivities )
					activity.OnManagedリソースの作成();
			}

			foreach( STPlugin st in this.listプラグイン )
			{
				Directory.SetCurrentDirectory( st.strプラグインフォルダ );
				st.plugin.OnManagedリソースの作成();
				Directory.SetCurrentDirectory( TJAPlayer3.strEXEのあるフォルダ );
			}
			*/
		}
		
		protected override void LoadContent()
		{
			if ( ConfigIni.bウィンドウモード )
			{
				if( !this.bマウスカーソル表示中 )
				{
					//Cursor.Show();
					this.bマウスカーソル表示中 = true;
				}
			}
			else if( this.bマウスカーソル表示中 )
			{
				//Cursor.Hide();
				this.bマウスカーソル表示中 = false;
			}

			if( this.listトップレベルActivities != null )
			{
				foreach( CActivity activity in this.listトップレベルActivities )
					activity.CreateUnmanagedResource();
			}

			foreach( STPlugin st in this.PluginList )
			{
				Directory.SetCurrentDirectory( st.pluginDirectory );
				st.plugin.OnUnmanagedリソースの作成();
				Directory.SetCurrentDirectory( TJAPlayer3.strEXEのあるフォルダ );
			}
		}
		protected override void UnloadContent()
		{
			if( this.listトップレベルActivities != null )
			{
				foreach( CActivity activity in this.listトップレベルActivities )
					activity.ReleaseUnmanagedResource();
			}

			foreach( STPlugin st in this.PluginList )
			{
				Directory.SetCurrentDirectory( st.pluginDirectory );
				st.plugin.OnUnmanagedリソースの解放();
				Directory.SetCurrentDirectory( TJAPlayer3.strEXEのあるフォルダ );
			}
		}
		protected override void OnExiting()
		{
			ConfigIni.n初期ウィンドウ開始位置X = WindowPosition.X;
			ConfigIni.n初期ウィンドウ開始位置Y = WindowPosition.Y;
			ConfigIni.nウインドウwidth = WindowSize.X;
			ConfigIni.nウインドウheight = WindowSize.Y;
			ConfigIni.b全画面モード = FullScreen;
			ConfigIni.b垂直帰線待ちを行う = VSync;
			Framerate = 0;
			
			this.t終了処理();
			base.OnExiting();
		}
		protected override void Update()
		{
		}
		protected override void Draw()
		{
			// Sound管理?.t再生中の処理をする();
            Timer?.Update();
            SoundManager.PlayTimer?.Update();
            Input管理?.Polling( TJAPlayer3.ConfigIni.bバッファ入力を行う );
            FPS?.Update();

			if (BeatScaling != null)
			{
				BeatScaling.Tick();
				float value = MathF.Sin((BeatScaling.CurrentValue / 1000.0f) * MathF.PI / 2.0f);
				float scale = 1.0f + ((1.0f - value) / 40.0f);
				Camera *= Matrix4X4.CreateScale(scale, scale, 1.0f);
				if (BeatScaling.CurrentValue == BeatScaling.EndValue) BeatScaling = null;
			}

			//CameraTest
			/*
            Camera *= Matrix4X4.CreateScale(1.0f / ScreenAspect, 1.0f, 1.0f) * 
            Matrix4X4.CreateRotationZ(MathF.PI / 4.0f) * 
            Matrix4X4.CreateScale(1.0f * ScreenAspect, 1.0f, 1.0f);
			*/

			// #xxxxx 2013.4.8 yyagi; sleepの挿入位置を、EndScnene～Present間から、BeginScene前に移動。描画遅延を小さくするため。

			#region [ DTXCreatorからの指示 ]
			/*
			if ( this.Window.IsReceivedMessage )	// ウインドウメッセージで、
			{
				string strMes = this.Window.strMessage;
				this.Window.IsReceivedMessage = false;

				if ( strMes != null )
				{
					DTXVmode.ParseArguments( strMes );

					if ( DTXVmode.Enabled )
					{
						bコンパクトモード = true;
						strコンパクトモードファイル = DTXVmode.filename;
						if ( DTXVmode.Command == CDTXVmode.ECommand.Preview )
						{
							// preview soundの再生
							string strPreviewFilename = DTXVmode.previewFilename;
//Trace.TraceInformation( "Preview Filename=" + DTXVmode.previewFilename );
							try
							{
								if ( this.previewSound != null )
								{
									this.previewSound.tサウンドを停止する();
									this.previewSound.Dispose();
									this.previewSound = null;
								}
								this.previewSound = TJAPlayer3.Sound管理.tサウンドを生成する( strPreviewFilename, ESoundGroup.SongPreview );

							    // 2018-08-23 twopointzero: DTXVmode previewVolume will always set
							    // Gain since in this mode it should override the application of
							    // SONGVOL or any other Gain source regardless of configuration.
								this.previewSound.SetGain(DTXVmode.previewVolume);

								this.previewSound.n位置 = DTXVmode.previewPan;
								this.previewSound.t再生を開始する();
								Trace.TraceInformation( "DTXCからの指示で、サウンドを生成しました。({0})", strPreviewFilename );
							}
							catch
							{
								Trace.TraceError(ToString());
								Trace.TraceError( "DTXCからの指示での、サウンドの生成に失敗しました。({0})", strPreviewFilename );
								if ( this.previewSound != null )
								{
									this.previewSound.Dispose();
								}
								this.previewSound = null;
							}
						}
					}
				}
			}
			*/
			#endregion

			if( r現在のステージ != null )
			{
				this.n進行描画の戻り値 = ( r現在のステージ != null ) ? r現在のステージ.Draw() : 0;

				#region [ プラグインの進行描画 ]
				//---------------------
				foreach( STPlugin sp in this.PluginList )
				{
					Directory.SetCurrentDirectory( sp.pluginDirectory );

					if( TJAPlayer3.act現在入力を占有中のプラグイン == null || TJAPlayer3.act現在入力を占有中のプラグイン == sp.plugin )
						sp.plugin.On進行描画(TJAPlayer3.Pad, TJAPlayer3.Input管理.Keyboard );
					else
						sp.plugin.On進行描画( null, null );

					Directory.SetCurrentDirectory( TJAPlayer3.strEXEのあるフォルダ );
				}
				//---------------------
				#endregion


				CScoreIni scoreIni = null;

				#region [ 曲検索スレッドの起動/終了 ]					// ここに"Enumerating Songs..."表示を集約
				if ( !TJAPlayer3.bコンパクトモード )
				{
					actEnumSongs.Draw();							// "Enumerating Songs..."アイコンの描画
				}
				switch ( r現在のステージ.eステージID )
				{
					case CStage.Eステージ.タイトル:
					case CStage.Eステージ.コンフィグ:
					case CStage.Eステージ.選曲:
					case CStage.Eステージ.曲読み込み:
						if ( EnumSongs != null )
						{
							#region [ (特定条件時) 曲検索スレッドの起動_開始 ]
							if ( r現在のステージ.eステージID == CStage.Eステージ.タイトル &&
								 r直前のステージ.eステージID == CStage.Eステージ.起動 &&
								 this.n進行描画の戻り値 == (int) CStageタイトル.E戻り値.継続 &&
								 !EnumSongs.IsSongListEnumStarted )
							{
								actEnumSongs.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									actEnumSongs.CreateManagedResource();
									actEnumSongs.CreateUnmanagedResource();
								}
								TJAPlayer3.stage選曲.bIsEnumeratingSongs = true;
								EnumSongs.Init();	// 取得した曲数を、新インスタンスにも与える
								EnumSongs.StartEnumFromDisk();		// 曲検索スレッドの起動_開始
							}
							#endregion
							
							#region [ 曲検索の中断と再開 ]
							if ( r現在のステージ.eステージID == CStage.Eステージ.選曲 && !EnumSongs.IsSongListEnumCompletelyDone )
							{
								switch ( this.n進行描画の戻り値 )
								{
									case 0:     // 何もない
										EnumSongs.Resume();
										EnumSongs.IsSlowdown = false;
										actEnumSongs.Activate();
										if (!ConfigIni.PreAssetsLoading) 
										{
											actEnumSongs.CreateManagedResource();
											actEnumSongs.CreateUnmanagedResource();
										}
										break;

									case 2:		// 曲決定
										EnumSongs.Suspend();						// #27060 バックグラウンドの曲検索を一時停止
										actEnumSongs.DeActivate();
										if (!ConfigIni.PreAssetsLoading) 
										{
											actEnumSongs.ReleaseManagedResource();
											actEnumSongs.ReleaseUnmanagedResource();
										}
										break;
								}
							}
							#endregion

							#region [ 曲探索中断待ち待機 ]
							if ( r現在のステージ.eステージID == CStage.Eステージ.曲読み込み && !EnumSongs.IsSongListEnumCompletelyDone &&
								EnumSongs.thDTXFileEnumerate != null )							// #28700 2012.6.12 yyagi; at Compact mode, enumerating thread does not exist.
							{
								EnumSongs.WaitUntilSuspended();									// 念のため、曲検索が一時中断されるまで待機
							}
							#endregion

							#region [ 曲検索が完了したら、実際の曲リストに反映する ]
							// CStage選曲.On活性化() に回した方がいいかな？
							if ( EnumSongs.IsSongListEnumerated )
							{
								actEnumSongs.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									actEnumSongs.ReleaseManagedResource();
									actEnumSongs.ReleaseUnmanagedResource();
								}
								TJAPlayer3.stage選曲.bIsEnumeratingSongs = false;

								bool bRemakeSongTitleBar = ( r現在のステージ.eステージID == CStage.Eステージ.選曲 ) ? true : false;
								TJAPlayer3.stage選曲.Refresh( EnumSongs.Songs管理, bRemakeSongTitleBar );
								EnumSongs.SongListEnumCompletelyDone();
							}
							#endregion
						}
						break;
				}
				#endregion

				switch ( r現在のステージ.eステージID )
				{
					case CStage.Eステージ.何もしない:
						break;

					case CStage.Eステージ.起動:
						#region [ *** ]
						//-----------------------------
						if( this.n進行描画の戻り値 != 0 )
						{
							if( !bコンパクトモード )
							{
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation( "----------------------" );
								Trace.TraceInformation( "■ タイトル" );
								stageタイトル.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									stageタイトル.CreateManagedResource();
									stageタイトル.CreateUnmanagedResource();
								}
								r直前のステージ = r現在のステージ;
								r現在のステージ = stageタイトル;
							}
							else
							{
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation( "----------------------" );
								Trace.TraceInformation( "■ 曲読み込み" );
								stage曲読み込み.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									stage曲読み込み.CreateManagedResource();
									stage曲読み込み.CreateUnmanagedResource();
								}
								r直前のステージ = r現在のステージ;
								r現在のステージ = stage曲読み込み;
							}
							foreach( STPlugin pg in this.PluginList )
							{
								Directory.SetCurrentDirectory( pg.pluginDirectory );
								pg.plugin.Onステージ変更();
								Directory.SetCurrentDirectory( TJAPlayer3.strEXEのあるフォルダ );
							}

							this.tガベージコレクションを実行する();
						}
						//-----------------------------
						#endregion
						break;

					case CStage.Eステージ.タイトル:
						#region [ *** ]
						//-----------------------------
						switch( this.n進行描画の戻り値 )
						{
							case (int)CStageタイトル.E戻り値.GAMESTART:
								#region [ 選曲処理へ ]
								//-----------------------------
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation( "----------------------" );
								Trace.TraceInformation( "■ 選曲" );
								stage選曲.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									stage選曲.CreateManagedResource();
									stage選曲.CreateUnmanagedResource();
								}
								r直前のステージ = r現在のステージ;
								r現在のステージ = stage選曲;

								TJAPlayer3.latestSongSelect = stage選曲;
								//-----------------------------
								#endregion
								break;

							case (int)CStageタイトル.E戻り値.DANGAMESTART:
								#region [ 段位選択処理へ ]
								//-----------------------------
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation( "----------------------" );
								Trace.TraceInformation( "■ 段位選択" );
								stage段位選択.Activate();	
								if (!ConfigIni.PreAssetsLoading) 
								{
									stage段位選択.CreateManagedResource();
									stage段位選択.CreateUnmanagedResource();
								}							
								r直前のステージ = r現在のステージ;
								r現在のステージ = stage段位選択;
								TJAPlayer3.latestSongSelect = stage段位選択;
								//-----------------------------
								#endregion
								break;

							case (int)CStageタイトル.E戻り値.TAIKOTOWERSSTART:
								#region [Online Lounge]
								//-----------------------------
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation("----------------------");
								Trace.TraceInformation("■ Online Lounge");
								stageTowerSelect.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
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
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation("----------------------");
								Trace.TraceInformation("■ Taiko Heya");
								stageHeya.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
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
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation("----------------------");
								Trace.TraceInformation("■ Online Lounge");
								stageOnlineLounge.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									stageOnlineLounge.CreateManagedResource();
									stageOnlineLounge.CreateUnmanagedResource();
								}		
								r直前のステージ = r現在のステージ;
								r現在のステージ = stageOnlineLounge;
								//-----------------------------
								#endregion
								break;

							case (int)CStageタイトル.E戻り値.ENCYCLOPEDIA:
								#region [Online Lounge]
								//-----------------------------
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation("----------------------");
								Trace.TraceInformation("■ Open Encyclopedia");
								stageOpenEncyclopedia.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									stageOpenEncyclopedia.CreateManagedResource();
									stageOpenEncyclopedia.CreateUnmanagedResource();
								}		
								r直前のステージ = r現在のステージ;
								r現在のステージ = stageOpenEncyclopedia;
								//-----------------------------
								#endregion
								break;

							case (int)CStageタイトル.E戻り値.CONFIG:
								#region [ *** ]
								//-----------------------------
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation( "----------------------" );
								Trace.TraceInformation( "■ コンフィグ" );
								stageコンフィグ.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
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
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation( "----------------------" );
								Trace.TraceInformation( "■ 終了" );
								stage終了.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
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
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation("----------------------");
								Trace.TraceInformation("■ 選曲");
								stage選曲.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									stage選曲.CreateManagedResource();
									stage選曲.CreateUnmanagedResource();
								}		
								r直前のステージ = r現在のステージ;
								r現在のステージ = stage選曲;

								TJAPlayer3.latestSongSelect = stage選曲;
								ConfigIni.nPreviousPlayerCount = ConfigIni.nPlayerCount;
                                ConfigIni.nPlayerCount = 2;
								ConfigIni.bAIBattleMode = true;
								//-----------------------------
								#endregion
								break;

						}

						foreach ( STPlugin pg in this.PluginList )
						{
							Directory.SetCurrentDirectory( pg.pluginDirectory );
							pg.plugin.Onステージ変更();
							Directory.SetCurrentDirectory( TJAPlayer3.strEXEのあるフォルダ );
						}

						//this.tガベージコレクションを実行する();		// #31980 2013.9.3 yyagi タイトル画面でだけ、毎フレームGCを実行して重くなっていた問題の修正
						//-----------------------------
						#endregion
						break;

					case CStage.Eステージ.コンフィグ:
						#region [ *** ]
						//-----------------------------
						if( this.n進行描画の戻り値 != 0 )
						{
							switch( r直前のステージ.eステージID )
							{
								case CStage.Eステージ.タイトル:
									#region [ *** ]
									//-----------------------------
									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) 
									{
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}
									Trace.TraceInformation( "----------------------" );
									Trace.TraceInformation( "■ タイトル" );
									stageタイトル.Activate();
									if (!ConfigIni.PreAssetsLoading) 
									{
										stageタイトル.CreateManagedResource();
										stageタイトル.CreateUnmanagedResource();
									}
									stageタイトル.tReloadMenus();
									r直前のステージ = r現在のステージ;
									r現在のステージ = stageタイトル;

									foreach( STPlugin pg in this.PluginList )
									{
										Directory.SetCurrentDirectory( pg.pluginDirectory );
										pg.plugin.Onステージ変更();
										Directory.SetCurrentDirectory( TJAPlayer3.strEXEのあるフォルダ );
									}

									this.tガベージコレクションを実行する();
									break;
								//-----------------------------
									#endregion

								case CStage.Eステージ.選曲:
									#region [ *** ]
									//-----------------------------
									r現在のステージ.DeActivate();
									if (!ConfigIni.PreAssetsLoading) 
									{
										r現在のステージ.ReleaseManagedResource();
										r現在のステージ.ReleaseUnmanagedResource();
									}
									Trace.TraceInformation( "----------------------" );
									Trace.TraceInformation( "■ 選曲" );
									stage選曲.Activate();
									if (!ConfigIni.PreAssetsLoading) 
									{
										stage選曲.CreateManagedResource();
										stage選曲.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;
									r現在のステージ = stage選曲;

									foreach( STPlugin pg in this.PluginList )
									{
										Directory.SetCurrentDirectory( pg.pluginDirectory );
										pg.plugin.Onステージ変更();
										Directory.SetCurrentDirectory( TJAPlayer3.strEXEのあるフォルダ );
									}

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

					case CStage.Eステージ.選曲:
						#region [ *** ]
						//-----------------------------
						switch( this.n進行描画の戻り値 )
						{
							case (int) CStage選曲.E戻り値.タイトルに戻る:
								#region [ *** ]
								//-----------------------------
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation( "----------------------" );
								Trace.TraceInformation( "■ タイトル" );
								stageタイトル.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									stageタイトル.CreateManagedResource();
									stageタイトル.CreateUnmanagedResource();
								}
								r直前のステージ = r現在のステージ;
								r現在のステージ = stageタイトル;

								CSongSelectSongManager.stopSong();
								CSongSelectSongManager.enable();

								if (ConfigIni.bAIBattleMode == true)
								{
									ConfigIni.nPlayerCount = ConfigIni.nPreviousPlayerCount;
                                    ConfigIni.bAIBattleMode = false;
                                }
                                /*
								Skin.bgm選曲画面イン.t停止する();
								Skin.bgm選曲画面.t停止する();
								*/

								foreach ( STPlugin pg in this.PluginList )
								{
									Directory.SetCurrentDirectory( pg.pluginDirectory );
									pg.plugin.Onステージ変更();
									Directory.SetCurrentDirectory( TJAPlayer3.strEXEのあるフォルダ );
								}

								this.tガベージコレクションを実行する();
								break;
							//-----------------------------
								#endregion

							case (int) CStage選曲.E戻り値.選曲した:
								#region [ *** ]
								//-----------------------------
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation( "----------------------" );
								Trace.TraceInformation( "■ 曲読み込み" );
								stage曲読み込み.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
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

								foreach ( STPlugin pg in this.PluginList )
								{
									Directory.SetCurrentDirectory( pg.pluginDirectory );
									pg.plugin.Onステージ変更();
									Directory.SetCurrentDirectory( TJAPlayer3.strEXEのあるフォルダ );
								}

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

							case (int) CStage選曲.E戻り値.コンフィグ呼び出し:
								#region [ *** ]
								//-----------------------------
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation( "----------------------" );
								Trace.TraceInformation( "■ コンフィグ" );
								stageコンフィグ.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									stageコンフィグ.CreateManagedResource();
									stageコンフィグ.CreateUnmanagedResource();
								}
								r直前のステージ = r現在のステージ;
								r現在のステージ = stageコンフィグ;

								CSongSelectSongManager.stopSong();
								CSongSelectSongManager.enable();

								foreach ( STPlugin pg in this.PluginList )
								{
									Directory.SetCurrentDirectory( pg.pluginDirectory );
									pg.plugin.Onステージ変更();
									Directory.SetCurrentDirectory( TJAPlayer3.strEXEのあるフォルダ );
								}

								this.tガベージコレクションを実行する();
								break;
							//-----------------------------
								#endregion

							case (int) CStage選曲.E戻り値.スキン変更:

								#region [ *** ]
								//-----------------------------
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation( "----------------------" );
								Trace.TraceInformation( "■ スキン切り替え" );
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

					case CStage.Eステージ.段位選択:
						#region [ *** ]
						switch (this.n進行描画の戻り値)
						{
							case (int)CStage選曲.E戻り値.タイトルに戻る:
								#region [ *** ]
								//-----------------------------
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation("----------------------");
								Trace.TraceInformation("■ タイトル");
								stageタイトル.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
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

								foreach (STPlugin pg in this.PluginList)
								{
									Directory.SetCurrentDirectory(pg.pluginDirectory);
									pg.plugin.Onステージ変更();
									Directory.SetCurrentDirectory(TJAPlayer3.strEXEのあるフォルダ);
								}

								this.tガベージコレクションを実行する();
								break;
							//-----------------------------
							#endregion

							case (int)CStage選曲.E戻り値.選曲した:
								#region [ *** ]
								//-----------------------------

								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation("----------------------");
								Trace.TraceInformation("■ 曲読み込み");
								stage曲読み込み.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									stage曲読み込み.CreateManagedResource();
									stage曲読み込み.CreateUnmanagedResource();
								}
								r直前のステージ = r現在のステージ;
								r現在のステージ = stage曲読み込み;

								foreach (STPlugin pg in this.PluginList)
								{
									Directory.SetCurrentDirectory(pg.pluginDirectory);
									pg.plugin.Onステージ変更();
									Directory.SetCurrentDirectory(TJAPlayer3.strEXEのあるフォルダ);
								}

								this.tガベージコレクションを実行する();
								break;
							//-----------------------------
							#endregion
						}
						#endregion
						break;

					case CStage.Eステージ.Heya:
						#region [ *** ]
						switch (this.n進行描画の戻り値)
						{
							case (int)CStage選曲.E戻り値.タイトルに戻る:
								#region [ *** ]
								//-----------------------------
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation("----------------------");
								Trace.TraceInformation("■ タイトル");
								stageタイトル.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									stageタイトル.CreateManagedResource();
									stageタイトル.CreateUnmanagedResource();
								}
								r直前のステージ = r現在のステージ;
								r現在のステージ = stageタイトル;

								CSongSelectSongManager.stopSong();
								CSongSelectSongManager.enable();

								foreach (STPlugin pg in this.PluginList)
								{
									Directory.SetCurrentDirectory(pg.pluginDirectory);
									pg.plugin.Onステージ変更();
									Directory.SetCurrentDirectory(TJAPlayer3.strEXEのあるフォルダ);
								}

								this.tガベージコレクションを実行する();
								break;
								//-----------------------------
								#endregion
						}
						#endregion
						break;

					case CStage.Eステージ.曲読み込み:
						#region [ *** ]
						//-----------------------------
						DTXVmode.Refreshed = false;		// 曲のリロード中に発生した再リロードは、無視する。
						if( this.n進行描画の戻り値 != 0 )
						{
							TJAPlayer3.Pad.st検知したデバイス.Clear();	// 入力デバイスフラグクリア(2010.9.11)
							r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
							#region [ ESC押下時は、曲の読み込みを中止して選曲画面に戻る ]
							if ( this.n進行描画の戻り値 == (int) E曲読込画面の戻り値.読込中止 )
							{
								//DTX.t全チップの再生停止();
								if( DTX != null )
                                {
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
								TJAPlayer3.latestSongSelect.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									TJAPlayer3.latestSongSelect.CreateManagedResource();
									TJAPlayer3.latestSongSelect.CreateUnmanagedResource();
								}
								r直前のステージ = r現在のステージ;

								// Seek latest registered song select screen
								r現在のステージ = TJAPlayer3.latestSongSelect;

								foreach ( STPlugin pg in this.PluginList )
								{
									Directory.SetCurrentDirectory( pg.pluginDirectory );
									pg.plugin.Onステージ変更();
									Directory.SetCurrentDirectory( TJAPlayer3.strEXEのあるフォルダ );
								}

								break;
							}
							#endregion

							Trace.TraceInformation( "----------------------" );
							Trace.TraceInformation( "■ 演奏（ドラム画面）" );
#if false		// #23625 2011.1.11 Config.iniからダメージ/回復値の定数変更を行う場合はここを有効にする 087リリースに合わせ機能無効化
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
							foreach( STPlugin pg in this.PluginList )
							{
								Directory.SetCurrentDirectory( pg.pluginDirectory );
								pg.plugin.Onステージ変更();
								Directory.SetCurrentDirectory( TJAPlayer3.strEXEのあるフォルダ );
							}

							this.tガベージコレクションを実行する();
						}
						//-----------------------------
						#endregion
						break;

					case CStage.Eステージ.演奏:
						#region [ *** ]

						#region [ DTXVモード中にDTXCreatorから指示を受けた場合の処理 ]
						if ( DTXVmode.Enabled && DTXVmode.Refreshed )
						{
							DTXVmode.Refreshed = false;

							if ( DTXVmode.Command == CDTXVmode.ECommand.Stop )
							{
								TJAPlayer3.stage演奏ドラム画面.t停止();
								if ( previewSound != null )
								{
									this.previewSound.tStopSound();
									this.previewSound.Dispose();
									this.previewSound = null;
								}
								//{
								//    int lastd = 0;
								//    int f = 0;
								//    for ( int i = 0; i < swlist1.Count; i++ )
								//    {
								//        int d1 = swlist1[ i ];
								//        int d2 = swlist2[ i ];
								//        int d3 = swlist3[ i ];
								//        int d4 = swlist4[ i ];
								//        int d5 = swlist5[ i ];

								//        int dif = d1 - lastd;
								//        string s = "";
								//        if ( 16 <= dif && dif <= 17 )
								//        {
								//        }
								//        else
								//        {
								//            s = "★";
								//        }
								//        Trace.TraceInformation( "frame {0:D4}: {1:D3} ( {2:D3}, {3:D3} - {7:D3}, {4:D3} ) {5}, n現在時刻={6}", f, dif, d1, d2, d3, s, d4, d5 );
								//        lastd = d1;
								//        f++;
								//    }
								//    swlist1.Clear();
								//    swlist2.Clear();
								//    swlist3.Clear();
								//    swlist4.Clear();
								//    swlist5.Clear();

								//}
							}
							else if ( DTXVmode.Command == CDTXVmode.ECommand.Play )
							{
								if ( DTXVmode.NeedReload )
								{
									TJAPlayer3.stage演奏ドラム画面.t再読込();

									TJAPlayer3.ConfigIni.bTimeStretch = DTXVmode.TimeStretch;
									SoundManager.bIsTimeStretch = DTXVmode.TimeStretch;
									if ( TJAPlayer3.ConfigIni.b垂直帰線待ちを行う != DTXVmode.VSyncWait )
									{
										TJAPlayer3.ConfigIni.b垂直帰線待ちを行う = DTXVmode.VSyncWait;
										TJAPlayer3.app.b次のタイミングで垂直帰線同期切り替えを行う = true;
									}
								}
								else
								{
									TJAPlayer3.stage演奏ドラム画面.t演奏位置の変更( TJAPlayer3.DTXVmode.nStartBar, 0 );
								}
							}
						}
						#endregion

						switch( this.n進行描画の戻り値 )
						{
							case (int) E演奏画面の戻り値.再読込_再演奏:
								#region [ DTXファイルを再読み込みして、再演奏 ]
								DTX.t全チップの再生停止();
								DTX.DeActivate();
								DTX.ReleaseManagedResource();
								DTX.ReleaseUnmanagedResource();
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								stage曲読み込み.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
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

							case (int) E演奏画面の戻り値.継続:
								break;

							case (int) E演奏画面の戻り値.演奏中断:
								#region [ 演奏キャンセル ]
								//-----------------------------
								scoreIni = this.tScoreIniへBGMAdjustとHistoryとPlayCountを更新( "Play canceled" );

								#region [ プラグイン On演奏キャンセル() の呼び出し ]
								//---------------------
								foreach( STPlugin pg in this.PluginList )
								{
									Directory.SetCurrentDirectory( pg.pluginDirectory );
									pg.plugin.On演奏キャンセル( scoreIni );
									Directory.SetCurrentDirectory( TJAPlayer3.strEXEのあるフォルダ );
								}
								//---------------------
								#endregion

								DTX.t全チップの再生停止();
								DTX.DeActivate();
								DTX.ReleaseManagedResource();
								DTX.ReleaseUnmanagedResource();
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}

									// Play cancelled return screen

									/*
									if(stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan)
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
									TJAPlayer3.latestSongSelect.Activate();
									if (!ConfigIni.PreAssetsLoading) 
									{
										TJAPlayer3.latestSongSelect.CreateManagedResource();
										TJAPlayer3.latestSongSelect.CreateUnmanagedResource();
									}
									r直前のステージ = r現在のステージ;

									// Seek latest registered song select screen
									r現在のステージ = TJAPlayer3.latestSongSelect;

									#region [ プラグイン Onステージ変更() の呼び出し ]
									//---------------------
									foreach ( STPlugin pg in this.PluginList )
									{
										Directory.SetCurrentDirectory( pg.pluginDirectory );
										pg.plugin.Onステージ変更();
										Directory.SetCurrentDirectory( TJAPlayer3.strEXEのあるフォルダ );
									}
									//---------------------
									#endregion

									this.tガベージコレクションを実行する();
                                this.tガベージコレクションを実行する();
                                break;
								//-----------------------------
								#endregion

							case (int) E演奏画面の戻り値.ステージ失敗:
								#region [ 演奏失敗(StageFailed) ]
								//-----------------------------
								scoreIni = this.tScoreIniへBGMAdjustとHistoryとPlayCountを更新( "Stage failed" );

								#region [ プラグイン On演奏失敗() の呼び出し ]
								//---------------------
								foreach( STPlugin pg in this.PluginList )
								{
									Directory.SetCurrentDirectory( pg.pluginDirectory );
									pg.plugin.On演奏失敗( scoreIni );
									Directory.SetCurrentDirectory( TJAPlayer3.strEXEのあるフォルダ );
								}
								//---------------------
								#endregion

								DTX.t全チップの再生停止();
								DTX.DeActivate();
								DTX.ReleaseManagedResource();
								DTX.ReleaseUnmanagedResource();
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}

								Trace.TraceInformation("----------------------");
								Trace.TraceInformation("■ 選曲");
								stage選曲.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									stage選曲.CreateManagedResource();
									stage選曲.CreateUnmanagedResource();
								}
								r直前のステージ = r現在のステージ;
								r現在のステージ = stage選曲;

								#region [ プラグイン Onステージ変更() の呼び出し ]
								//---------------------
								foreach (STPlugin pg in this.PluginList)
								{
									Directory.SetCurrentDirectory(pg.pluginDirectory);
									pg.plugin.Onステージ変更();
									Directory.SetCurrentDirectory(TJAPlayer3.strEXEのあるフォルダ);
								}
								//---------------------
								#endregion

								this.tガベージコレクションを実行する();
								break;
								//-----------------------------
								#endregion

							case (int) E演奏画面の戻り値.ステージクリア:
								#region [ 演奏クリア ]
								//-----------------------------
								CScoreIni.C演奏記録 c演奏記録_Drums;
								stage演奏ドラム画面.t演奏結果を格納する( out c演奏記録_Drums );

                                double ps = 0.0, gs = 0.0;
								if ( !c演奏記録_Drums.b全AUTOである && c演奏記録_Drums.n全チップ数 > 0) {
									ps = c演奏記録_Drums.db演奏型スキル値;
									gs = c演奏記録_Drums.dbゲーム型スキル値;
								}
								string str = "Cleared";
								switch( CScoreIni.t総合ランク値を計算して返す( c演奏記録_Drums, null, null ) )
								{
									case (int)CScoreIni.ERANK.SS:
										str = string.Format( "Cleared (SS: {0:F2})", ps );
										break;

									case (int) CScoreIni.ERANK.S:
										str = string.Format( "Cleared (S: {0:F2})", ps );
										break;

									case (int) CScoreIni.ERANK.A:
										str = string.Format( "Cleared (A: {0:F2})", ps );
										break;

									case (int) CScoreIni.ERANK.B:
										str = string.Format( "Cleared (B: {0:F2})", ps );
										break;

									case (int) CScoreIni.ERANK.C:
										str = string.Format( "Cleared (C: {0:F2})", ps );
										break;

									case (int) CScoreIni.ERANK.D:
										str = string.Format( "Cleared (D: {0:F2})", ps );
										break;

									case (int) CScoreIni.ERANK.E:
										str = string.Format( "Cleared (E: {0:F2})", ps );
										break;

									case (int)CScoreIni.ERANK.UNKNOWN:	// #23534 2010.10.28 yyagi add: 演奏チップが0個のとき
										str = "Cleared (No chips)";
										break;
								}

								scoreIni = this.tScoreIniへBGMAdjustとHistoryとPlayCountを更新( str );

								#region [ プラグイン On演奏クリア() の呼び出し ]
								//---------------------
								foreach( STPlugin pg in this.PluginList )
								{
									Directory.SetCurrentDirectory( pg.pluginDirectory );
									pg.plugin.On演奏クリア( scoreIni );
									Directory.SetCurrentDirectory( TJAPlayer3.strEXEのあるフォルダ );
								}
								//---------------------
								#endregion

								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation( "----------------------" );
								Trace.TraceInformation( "■ 結果" );
								stage結果.st演奏記録.Drums = c演奏記録_Drums;
								stage結果.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									stage結果.CreateManagedResource();
									stage結果.CreateUnmanagedResource();
								}
								r直前のステージ = r現在のステージ;
								r現在のステージ = stage結果;

								#region [ プラグイン Onステージ変更() の呼び出し ]
								//---------------------
								foreach( STPlugin pg in this.PluginList )
								{
									Directory.SetCurrentDirectory( pg.pluginDirectory );
									pg.plugin.Onステージ変更();
									Directory.SetCurrentDirectory( TJAPlayer3.strEXEのあるフォルダ );
								}
								//---------------------
								#endregion

								break;
								//-----------------------------
								#endregion
						}
						//-----------------------------
						#endregion
						break;

					case CStage.Eステージ.結果:
						#region [ *** ]
						//-----------------------------
						if( this.n進行描画の戻り値 != 0 )
						{
							//DTX.t全チップの再生一時停止();
                            DTX.t全チップの再生停止とミキサーからの削除();
                            DTX.DeActivate();
							DTX.ReleaseManagedResource();
							DTX.ReleaseUnmanagedResource();
							r現在のステージ.DeActivate();
							if (!ConfigIni.PreAssetsLoading) 
							{
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
							TJAPlayer3.latestSongSelect.Activate();
							if (!ConfigIni.PreAssetsLoading) 
							{
								TJAPlayer3.latestSongSelect.CreateManagedResource();
								TJAPlayer3.latestSongSelect.CreateUnmanagedResource();
							}
							r直前のステージ = r現在のステージ;

							// Seek latest registered song select screen
							r現在のステージ = TJAPlayer3.latestSongSelect;

							stage選曲.NowSong++;

							foreach (STPlugin pg in this.PluginList)
							{
								Directory.SetCurrentDirectory(pg.pluginDirectory);
								pg.plugin.Onステージ変更();
								Directory.SetCurrentDirectory(TJAPlayer3.strEXEのあるフォルダ);
							}

							this.tガベージコレクションを実行する();
						}
						//-----------------------------
						#endregion
						break;


					case CStage.Eステージ.TaikoTowers:
						#region [ *** ]
						switch (this.n進行描画の戻り値)
						{
							case (int)EReturnValue.ReturnToTitle:
								#region [ *** ]
								//-----------------------------
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation("----------------------");
								Trace.TraceInformation("■ タイトル");
								stageタイトル.Activate();
								r直前のステージ = r現在のステージ;
								r現在のステージ = stageタイトル;

								/*
								Skin.bgm選曲画面イン.t停止する();
								Skin.bgm選曲画面.t停止する();
								*/
								CSongSelectSongManager.stopSong();
								CSongSelectSongManager.enable();

								foreach (STPlugin pg in this.PluginList)
								{
									Directory.SetCurrentDirectory(pg.pluginDirectory);
									pg.plugin.Onステージ変更();
									Directory.SetCurrentDirectory(TJAPlayer3.strEXEのあるフォルダ);
								}

								this.tガベージコレクションを実行する();
								break;
							//-----------------------------
							#endregion

							case (int)EReturnValue.SongChoosen:
								#region [ *** ]
								//-----------------------------
								latestSongSelect = stageTowerSelect;
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation("----------------------");
								Trace.TraceInformation("■ 曲読み込み");
								stage曲読み込み.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									stage曲読み込み.CreateManagedResource();
									stage曲読み込み.CreateUnmanagedResource();
								}
								r直前のステージ = r現在のステージ;
								r現在のステージ = stage曲読み込み;

								foreach (STPlugin pg in this.PluginList)
								{
									Directory.SetCurrentDirectory(pg.pluginDirectory);
									pg.plugin.Onステージ変更();
									Directory.SetCurrentDirectory(TJAPlayer3.strEXEのあるフォルダ);
								}

								this.tガベージコレクションを実行する();
								break;
								//-----------------------------
								#endregion
						}
						#endregion
						break;

					case CStage.Eステージ.ChangeSkin:
						#region [ *** ]
						//-----------------------------
						if ( this.n進行描画の戻り値 != 0 )
						{
							r現在のステージ.DeActivate();
							if (!ConfigIni.PreAssetsLoading) 
							{
								r現在のステージ.ReleaseManagedResource();
								r現在のステージ.ReleaseUnmanagedResource();
							}
							Trace.TraceInformation( "----------------------" );
							Trace.TraceInformation( "■ 選曲" );
							stage選曲.Activate();
							if (!ConfigIni.PreAssetsLoading) 
							{
								stage選曲.CreateManagedResource();
								stage選曲.CreateUnmanagedResource();
							}
							r直前のステージ = r現在のステージ;
							r現在のステージ = stage選曲;
							this.tガベージコレクションを実行する();
						}
						//-----------------------------
						#endregion
						break;

					case CStage.Eステージ.終了:
						#region [ *** ]
						//-----------------------------
						if( this.n進行描画の戻り値 != 0 )
						{
							base.Exit();
							return;
						}
						//-----------------------------
						#endregion
						break;

					default:
						#region [ *** ]
						switch (this.n進行描画の戻り値)
						{
							case (int)CStage選曲.E戻り値.タイトルに戻る:
								#region [ *** ]
								//-----------------------------
								r現在のステージ.DeActivate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									r現在のステージ.ReleaseManagedResource();
									r現在のステージ.ReleaseUnmanagedResource();
								}
								Trace.TraceInformation("----------------------");
								Trace.TraceInformation("■ タイトル");
								stageタイトル.Activate();
								if (!ConfigIni.PreAssetsLoading) 
								{
									stageタイトル.CreateManagedResource();
									stageタイトル.CreateUnmanagedResource();
								}
								r直前のステージ = r現在のステージ;
								r現在のステージ = stageタイトル;

								CSongSelectSongManager.stopSong();
								CSongSelectSongManager.enable();

								foreach (STPlugin pg in this.PluginList)
								{
									Directory.SetCurrentDirectory(pg.pluginDirectory);
									pg.plugin.Onステージ変更();
									Directory.SetCurrentDirectory(TJAPlayer3.strEXEのあるフォルダ);
								}

								this.tガベージコレクションを実行する();
								break;
								//-----------------------------
								#endregion
						}
						#endregion
						break;
				}

			    actScanningLoudness?.Draw();

				if (!ConfigIni.bTokkunMode)
				{
					float screen_ratiox = TJAPlayer3.Skin.Resolution[0] / 1280.0f;
					float screen_ratioy = TJAPlayer3.Skin.Resolution[1] / 720.0f;
					/*
					var mat = Matrix.LookAtLH(new Vector3(-fCamXOffset * screen_ratiox, fCamYOffset * screen_ratioy, (float)(-SampleFramework.GameWindowSize.Height / (fCamZoomFactor * 2) * Math.Sqrt(3.0))), new Vector3(-fCamXOffset * screen_ratiox, fCamYOffset * screen_ratioy, 0f), new Vector3(0f, 1f, 0f));
					mat *= Matrix.RotationYawPitchRoll(0, 0, C変換.DegreeToRadian(fCamRotation));
					mat *= Matrix.Scaling(fCamXScale, fCamYScale, 1f);
					this.Device.SetTransform(TransformState.View, mat);
					*/

					Camera *= Matrix4X4.CreateScale(fCamXScale, fCamYScale, 1f);
					
					Camera *= Matrix4X4.CreateScale(1.0f / ScreenAspect, 1.0f, 1.0f) * 
					Matrix4X4.CreateRotationZ(CConversion.DegreeToRadian(fCamRotation)) * 
					Matrix4X4.CreateScale(1.0f * ScreenAspect, 1.0f, 1.0f);

					Camera *= Matrix4X4.CreateTranslation(fCamXOffset / 1280, fCamYOffset / 720, 1f);

					if (TJAPlayer3.DTX != null)
					{
						//object rendering
						foreach (KeyValuePair<string, CSongObject> pair in TJAPlayer3.DTX.listObj)
						{
							pair.Value.tDraw();
						}
					}

					Camera = Matrix4X4<float>.Identity;
				}

				if (r現在のステージ != null && r現在のステージ.eステージID != CStage.Eステージ.起動 && TJAPlayer3.Tx.Network_Connection != null)
				{
					if (Math.Abs(SoundManager.PlayTimer.SystemTimeMs - this.前回のシステム時刻ms) > 10000)
					{
						this.前回のシステム時刻ms = SoundManager.PlayTimer.SystemTimeMs;
						Task.Factory.StartNew(() =>
						{
							//IPv4 8.8.8.8にPingを送信する(timeout 5000ms)
							PingReply reply = new Ping().Send("8.8.8.8", 5000);
							this.bネットワークに接続中 = reply.Status == IPStatus.Success;
						});
					}
					TJAPlayer3.Tx.Network_Connection.t2D描画(GameWindowSize.Width - (TJAPlayer3.Tx.Network_Connection.szテクスチャサイズ.Width / 2), GameWindowSize.Height - TJAPlayer3.Tx.Network_Connection.szテクスチャサイズ.Height, new Rectangle((TJAPlayer3.Tx.Network_Connection.szテクスチャサイズ.Width / 2) * (this.bネットワークに接続中 ? 0 : 1), 0, TJAPlayer3.Tx.Network_Connection.szテクスチャサイズ.Width / 2, TJAPlayer3.Tx.Network_Connection.szテクスチャサイズ.Height));
				}
				// オーバレイを描画する(テクスチャの生成されていない起動ステージは例外

				// Display log cards
				VisualLogManager.Display();

				if (r現在のステージ != null && r現在のステージ.eステージID != CStage.Eステージ.起動 && TJAPlayer3.Tx.Overlay != null)
				{
					TJAPlayer3.Tx.Overlay.t2D描画(0, 0);
				}
			}

			foreach(var capture in ConfigIni.KeyAssign.System.Capture)
			{
				if (TJAPlayer3.Input管理.Keyboard.KeyPressed(capture.コード) && capture.コード != 0)
				{
					if (TJAPlayer3.Input管理.Keyboard.KeyPressing((int)SlimDXKeys.Key.LeftControl))
					{
						if (r現在のステージ.eステージID != CStage.Eステージ.演奏)
						{
							RefleshSkin();
							r現在のステージ.DeActivate();
							if (!ConfigIni.PreAssetsLoading) 
							{
								r現在のステージ.ReleaseManagedResource();
								r現在のステージ.ReleaseUnmanagedResource();
							}
							r現在のステージ.Activate();
							if (!ConfigIni.PreAssetsLoading) 
							{
								r現在のステージ.CreateManagedResource();
								r現在のステージ.CreateUnmanagedResource();
							}
						}
					}
					else
                    {
						// Debug.WriteLine( "capture: " + string.Format( "{0:2x}", (int) e.KeyCode ) + " " + (int) e.KeyCode );
						string strFullPath =
						   Path.Combine(TJAPlayer3.strEXEのあるフォルダ, "Capture_img");
						strFullPath = Path.Combine(strFullPath, DateTime.Now.ToString("yyyyMMddHHmmss") + ".png");
						SaveResultScreen(strFullPath);
					}
				}
			}

			/*
			if ( Sound管理?.GetCurrentSoundDeviceType() != "DirectSound" )
			{
				Sound管理?.t再生中の処理をする();	// サウンドバッファの更新; 画面描画と同期させることで、スクロールをスムーズにする
			}
			*/

			#region [ 全画面_ウインドウ切り替え ]
			if ( this.b次のタイミングで全画面_ウィンドウ切り替えを行う )
			{
				ConfigIni.b全画面モード = !ConfigIni.b全画面モード;
				app.ToggleWindowMode();
				this.b次のタイミングで全画面_ウィンドウ切り替えを行う = false;
			}
			#endregion
			#region [ 垂直基線同期切り替え ]
			if ( this.b次のタイミングで垂直帰線同期切り替えを行う )
			{
				VSync = ConfigIni.b垂直帰線待ちを行う;
				this.b次のタイミングで垂直帰線同期切り替えを行う = false;
			}
			#endregion
		}

		// その他

		#region [ 汎用ヘルパー ]
		//-----------------
		public static CTexture tテクスチャの生成( string fileName )
		{
			return tテクスチャの生成( fileName, false );
		}
		public static CTexture tテクスチャの生成( string fileName, bool b黒を透過する )
		{
			if ( app == null )
			{
				return null;
			}
			try
			{
				return new CTexture( fileName, b黒を透過する );
			}
			catch ( CTextureCreateFailedException e )
			{
				Trace.TraceError( e.ToString() );
				Trace.TraceError( "テクスチャの生成に失敗しました。({0})", fileName );
				return null;
			}
			catch ( FileNotFoundException )
			{
				Trace.TraceWarning( "テクスチャファイルが見つかりませんでした。({0})", fileName );
				return null;
			}
		}
		public static void tテクスチャの解放(ref CTexture tx )
		{
			TJAPlayer3.t安全にDisposeする( ref tx );
		}
        public static void tテクスチャの解放( ref CTextureAf tx )
		{
			TJAPlayer3.t安全にDisposeする( ref tx );
		}
		public static CTexture tテクスチャの生成( SKBitmap bitmap )
		{
			return tテクスチャの生成( bitmap, false );
		}
		public static CTexture tテクスチャの生成( SKBitmap bitmap, bool b黒を透過する )
		{
			if ( app == null )
			{
				return null;
			}
            if (bitmap == null)
            {
                Trace.TraceError("テクスチャの生成に失敗しました。(bitmap==null)");
                return null;
            }
            try
			{
				return new CTexture( bitmap, b黒を透過する );
			}
			catch ( CTextureCreateFailedException e )
			{
				Trace.TraceError( e.ToString() );
				Trace.TraceError( "テクスチャの生成に失敗しました。(txData)" );
				return null;
			}
		}

        public static CTextureAf tテクスチャの生成Af( string fileName )
		{
			return tテクスチャの生成Af( fileName, false );
		}
		public static CTextureAf tテクスチャの生成Af( string fileName, bool b黒を透過する )
		{
			if ( app == null )
			{
				return null;
			}
			try
			{
				return new CTextureAf( fileName, b黒を透過する );
			}
			catch ( CTextureCreateFailedException e )
			{
				Trace.TraceError( e.ToString() );
				Trace.TraceError( "テクスチャの生成に失敗しました。({0})", fileName );
				return null;
			}
			catch ( FileNotFoundException e )
			{
				Trace.TraceError( e.ToString() );
				Trace.TraceError( "テクスチャファイルが見つかりませんでした。({0})", fileName );
				return null;
			}
		}

        /// <summary>プロパティ、インデクサには ref は使用できないので注意。</summary>
        public static void t安全にDisposeする<T>(ref T obj)
        {
            if (obj == null)
                return;

            var d = obj as IDisposable;

            if (d != null)
                d.Dispose();

            obj = default(T);
        }

		public static void t安全にDisposeする<T>(ref T[] array) where T : class, IDisposable //2020.08.01 Mr-Ojii twopointzero氏のソースコードをもとに追加
		{
			if (array == null)
			{
				return;
			}

			for (var i = 0; i < array.Length; i++)
			{
				array[i]?.Dispose();
				array[i] = null;
			}
		}

		/// <summary>
		/// そのフォルダの連番画像の最大値を返す。
		/// </summary>
		public static int t連番画像の枚数を数える(string ディレクトリ名, string プレフィックス = "", string 拡張子 = ".png")
        {
            int num = 0;
            while(File.Exists(ディレクトリ名 + プレフィックス + num + 拡張子))
            {
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
		public static float GetSongNameXScaling(ref CTexture cTexture, int samePixel = 660)
		{
            if (cTexture == null) return 1f;
            float scalingRate = (float)samePixel / (float)cTexture.szテクスチャサイズ.Width;
            if (cTexture.szテクスチャサイズ.Width <= samePixel)
                scalingRate = 1.0f;
            return scalingRate;
        }

        /// <summary>
        /// 難易度を表す数字を列挙体に変換します。
        /// </summary>
        /// <param name="number">難易度を表す数字。</param>
        /// <returns>Difficulty 列挙体</returns>
        public static Difficulty DifficultyNumberToEnum(int number)
        {
            switch (number)
            {
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
		private bool bネットワークに接続中 = false;
		private long 前回のシステム時刻ms = long.MinValue;
		private static CDTX[] dtx = new CDTX[ 5 ];

        public static TextureLoader Tx = new TextureLoader();

		public List<CActivity> listトップレベルActivities;
		private int n進行描画の戻り値;
		private string strWindowTitle
		{
			get
			{
				if ( DTXVmode.Enabled )
				{
					return "DTXViewer release " + VERSION;
				}
				else
				{
					return "TJAPlayer3 feat.DTXMania";
				}
			}
		}
		private CSound previewSound;
        public static DateTime StartupTime
        {
            get;
            private set;
        }

        private void t起動処理()
		{

			#region [ Read Config.ini and Database files ]
			//---------------------
			NamePlateConfig = new NamePlateConfig();
			NamePlateConfig.tNamePlateConfig();

			Favorites = new Favorites();
			Favorites.tFavorites();

			RecentlyPlayedSongs = new RecentlyPlayedSongs();
			RecentlyPlayedSongs.tRecentlyPlayedSongs();

			Databases = new Databases();
			Databases.tDatabases();

			VisualLogManager = new CVisualLogManager();


            for (int i = 0; i < 5; i++)
            {
                SaveFileInstances[i] = new SaveFile();
                SaveFileInstances[i].tSaveFile(TJAPlayer3.ConfigIni.sSaveFile[i]);
            }
			// 2012.8.22 Config.iniが無いときに初期値が適用されるよう、この設定行をifブロック外に移動

			//---------------------
			#endregion

			#region [ ログ出力開始 ]
			//---------------------
			Trace.AutoFlush = true;
			if( ConfigIni.bログ出力 )
			{
				try
				{
					Trace.Listeners.Add( new CTraceLogListener( new StreamWriter( System.IO.Path.Combine( strEXEのあるフォルダ, "TJAPlayer3.log" ), false, Encoding.GetEncoding(TJAPlayer3.sEncType) ) ) );
				}
				catch ( System.UnauthorizedAccessException )			// #24481 2011.2.20 yyagi
				{
					int c = (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja")? 0 : 1;
					string[] mes_writeErr = {
						"DTXManiaLog.txtへの書き込みができませんでした。書き込みできるようにしてから、再度起動してください。",
						"Failed to write DTXManiaLog.txt. Please set it writable and try again."
					};
					//MessageBox.Show( mes_writeErr[c], "DTXMania boot error", MessageBoxButtons.OK, MessageBoxIcon.Error );
					Environment.Exit(1);
				}
			}
			Trace.WriteLine("");
			Trace.WriteLine( "DTXMania powered by YAMAHA Silent Session Drums" );
			Trace.WriteLine( string.Format( "Release: {0}", VERSION ) );
			Trace.WriteLine( "" );
			Trace.TraceInformation( "----------------------" );
			Trace.TraceInformation( "■ アプリケーションの初期化" );
			Trace.TraceInformation( "OS Version: " + Environment.OSVersion );
			Trace.TraceInformation( "ProcessorCount: " + Environment.ProcessorCount.ToString() );
			Trace.TraceInformation( "CLR Version: " + Environment.Version.ToString() );
			//---------------------
			#endregion
			
			#region [ DTXVmodeクラス の初期化 ]
			//---------------------
			//Trace.TraceInformation( "DTXVモードの初期化を行います。" );
			//Trace.Indent();
			try
			{
				DTXVmode = new CDTXVmode();
				DTXVmode.Enabled = false;
				//Trace.TraceInformation( "DTXVモードの初期化を完了しました。" );
			}
			finally
			{
				//Trace.Unindent();
			}
			//---------------------
			#endregion


			DTX = null;

			#region [ Skin の初期化 ]
			//---------------------
			Trace.TraceInformation( "スキンの初期化を行います。" );
			Trace.Indent();
#if !DEBUG
			try
#endif
			{
				Skin = new CSkin( TJAPlayer3.ConfigIni.strSystemSkinSubfolderFullName, false);
				TJAPlayer3.ConfigIni.strSystemSkinSubfolderFullName = TJAPlayer3.Skin.GetCurrentSkinSubfolderFullName( true );  // 旧指定のSkinフォルダが消滅していた場合に備える

				ChangeResolution(TJAPlayer3.Skin.Resolution[0], TJAPlayer3.Skin.Resolution[1]);

				Trace.TraceInformation( "スキンの初期化を完了しました。" );
			}
#if !DEBUG
			catch (Exception e)
			{
				Trace.TraceInformation( "スキンの初期化に失敗しました。" );
				throw;
			}
			finally
			{
				Trace.Unindent();
			}
#endif

			// Init Modal fonts once config.ini parsing is done
			// Moved here to reference Skin values.
			Modal.tInitModalFonts();
			//---------------------
			#endregion
			//-----------
			#region [ Timer の初期化 ]
			//---------------------
			Trace.TraceInformation( "タイマの初期化を行います。" );
			Trace.Indent();
			try
			{
				Timer = new CTimer( CTimer.TimerType.MultiMedia );
				Trace.TraceInformation( "タイマの初期化を完了しました。" );
			}
			finally
			{
				Trace.Unindent();
			}
			//---------------------
			#endregion
			//-----------

			#region [ FPS カウンタの初期化 ]
			//---------------------
			Trace.TraceInformation( "FPSカウンタの初期化を行います。" );
			Trace.Indent();
			try
			{
				FPS = new CFPS();
				Trace.TraceInformation( "FPSカウンタを生成しました。" );
			}
			finally
			{
				Trace.Unindent();
			}
			//---------------------
			#endregion
			#region [ act文字コンソールの初期化 ]
			//---------------------
			Trace.TraceInformation( "文字コンソールの初期化を行います。" );
			Trace.Indent();
			try
			{
				act文字コンソール = new C文字コンソール();
				Trace.TraceInformation( "文字コンソールを生成しました。" );
				act文字コンソール.Activate();
				//if (!ConfigIni.PreAssetsLoading) 
				{
					act文字コンソール.CreateManagedResource();
					act文字コンソール.CreateUnmanagedResource();
				}
				Trace.TraceInformation( "文字コンソールを活性化しました。" );
				Trace.TraceInformation( "文字コンソールの初期化を完了しました。" );
			}
			catch( Exception exception )
			{
				Trace.TraceError( exception.ToString() );
				Trace.TraceError( "文字コンソールの初期化に失敗しました。" );
			}
			finally
			{
				Trace.Unindent();
			}
			//---------------------
			#endregion
			#region [ Input管理 の初期化 ]
			//---------------------
			Trace.TraceInformation( "DirectInput, MIDI入力の初期化を行います。" );
			Trace.Indent();
			try
			{
				bool bUseMIDIIn = !DTXVmode.Enabled;
				Input管理 = new CInputManager(Window_);
				foreach( IInputDevice device in Input管理.InputDevices )
				{
					if( ( device.CurrentType == InputDeviceType.Joystick ) && !ConfigIni.dicJoystick.ContainsValue( device.GUID ) )
					{
						int key = 0;
						while( ConfigIni.dicJoystick.ContainsKey( key ) )
						{
							key++;
						}
						ConfigIni.dicJoystick.Add( key, device.GUID );
					}
				}
				foreach( IInputDevice device2 in Input管理.InputDevices )
				{
					if( device2.CurrentType == InputDeviceType.Joystick )
					{
						foreach( KeyValuePair<int, string> pair in ConfigIni.dicJoystick )
						{
							if( device2.GUID.Equals( pair.Value ) )
							{
								( (CInputJoystick) device2 ).SetID( pair.Key );
								break;
							}
						}
						continue;
					}
				}
				Trace.TraceInformation( "DirectInput の初期化を完了しました。" );
			}
			catch( Exception exception2 )
			{
				Trace.TraceError( "DirectInput, MIDI入力の初期化に失敗しました。" );
				throw;
			}
			finally
			{
				Trace.Unindent();
			}
			//---------------------
			#endregion
			#region [ Pad の初期化 ]
			//---------------------
			Trace.TraceInformation( "パッドの初期化を行います。" );
			Trace.Indent();
			try
			{
				Pad = new CPad( ConfigIni, Input管理 );
				Trace.TraceInformation( "パッドの初期化を完了しました。" );
			}
			catch( Exception exception3 )
			{
				Trace.TraceError( exception3.ToString() );
				Trace.TraceError( "パッドの初期化に失敗しました。" );
			}
			finally
			{
				Trace.Unindent();
			}
			//---------------------
			#endregion
			#region [ Sound管理 の初期化 ]
			//---------------------
			Trace.TraceInformation( "サウンドデバイスの初期化を行います。" );
			Trace.Indent();
			try
			{
				ESoundDeviceType soundDeviceType;
				switch (TJAPlayer3.ConfigIni.nSoundDeviceType)
				{
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
				Sound管理 = new SoundManager(Window_,
											soundDeviceType,
											TJAPlayer3.ConfigIni.nBassBufferSizeMs,
											TJAPlayer3.ConfigIni.nWASAPIBufferSizeMs,
					// CDTXMania.ConfigIni.nASIOBufferSizeMs,
											0,
											TJAPlayer3.ConfigIni.nASIODevice,
											TJAPlayer3.ConfigIni.bUseOSTimer
				);
				//Sound管理 = FDK.CSound管理.Instance;
				//Sound管理.t初期化( soundDeviceType, 0, 0, CDTXMania.ConfigIni.nASIODevice, base.Window.Handle );


				Trace.TraceInformation("Initializing loudness scanning, song gain control, and sound group level control...");
				Trace.Indent();
				try
				{
				    actScanningLoudness = new CActScanningLoudness();
				    actScanningLoudness.Activate();
					if (!ConfigIni.PreAssetsLoading) 
					{
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
				}
				finally
				{
					Trace.Unindent();
					Trace.TraceInformation("Initialized loudness scanning, song gain control, and sound group level control.");
				}

				ShowWindowTitleWithSoundType();
				FDK.SoundManager.bIsTimeStretch = TJAPlayer3.ConfigIni.bTimeStretch;
				Sound管理.nMasterVolume = TJAPlayer3.ConfigIni.nMasterVolume;
				//FDK.CSound管理.bIsMP3DecodeByWindowsCodec = CDTXMania.ConfigIni.bNoMP3Streaming;
				Trace.TraceInformation( "サウンドデバイスの初期化を完了しました。" );
			}
			catch (Exception e)
			{
                throw new NullReferenceException("サウンドデバイスがひとつも有効になっていないため、サウンドデバイスの初期化ができませんでした。", e);
			}
			finally
			{
				Trace.Unindent();
			}
			//---------------------
			#endregion
			#region [ Songs管理 の初期化 ]
			//---------------------
			Trace.TraceInformation( "曲リストの初期化を行います。" );
			Trace.Indent();
			try
			{
				Songs管理 = new CSongs管理();
//				Songs管理_裏読 = new CSongs管理();
				EnumSongs = new CEnumSongs();
				actEnumSongs = new CActEnumSongs();
				Trace.TraceInformation( "曲リストの初期化を完了しました。" );
			}
			catch( Exception e )
			{
				Trace.TraceError( e.ToString() );
				Trace.TraceError( "曲リストの初期化に失敗しました。" );
			}
			finally
			{
				Trace.Unindent();
			}
			//---------------------
			#endregion
			#region [ Random の初期化 ]
			//---------------------
			Random = new Random( (int) Timer.SystemTime );
			//---------------------
			#endregion
			#region [ Stages initialisation ]
			//---------------------
			r現在のステージ = null;
			r直前のステージ = null;
			stage起動 = new CStage起動();
			stageタイトル = new CStageタイトル();
//			stageオプション = new CStageオプション();
			stageコンフィグ = new CStageコンフィグ();
			stage選曲 = new CStage選曲();
			stage段位選択 = new CStage段位選択();
			stageHeya = new CStageHeya();
			stageOnlineLounge = new CStageOnlineLounge();
			stageTowerSelect = new CStageTowerSelect();
			stageOpenEncyclopedia = new COpenEncyclopedia();
			stage曲読み込み = new CStage曲読み込み();
			stage演奏ドラム画面 = new CStage演奏ドラム画面();
			stage結果 = new CStage結果();
			stageChangeSkin = new CStageChangeSkin();
			stage終了 = new CStage終了();
			NamePlate = new CNamePlate();
			SaveFile = 0;
			this.listトップレベルActivities = new List<CActivity>();
			this.listトップレベルActivities.Add( actEnumSongs );
			this.listトップレベルActivities.Add( act文字コンソール );
			this.listトップレベルActivities.Add( stage起動 );
			this.listトップレベルActivities.Add( stageタイトル );
//			this.listトップレベルActivities.Add( stageオプション );
			this.listトップレベルActivities.Add( stageコンフィグ );
			this.listトップレベルActivities.Add( stage選曲 );
			this.listトップレベルActivities.Add( stage段位選択 );
			this.listトップレベルActivities.Add( stageHeya );
			this.listトップレベルActivities.Add(stageOnlineLounge);
			this.listトップレベルActivities.Add(stageTowerSelect);
			this.listトップレベルActivities.Add( stageOpenEncyclopedia );
			this.listトップレベルActivities.Add( stage曲読み込み );
			this.listトップレベルActivities.Add( stage演奏ドラム画面 );
			this.listトップレベルActivities.Add( stage結果 );
			this.listトップレベルActivities.Add( stageChangeSkin );
			this.listトップレベルActivities.Add( stage終了 );
			//---------------------
			#endregion
			#region [ プラグインの検索と生成 ]
			//---------------------
			PluginHost = new CPluginHost();

			Trace.TraceInformation( "プラグインの検索と生成を行います。" );
			Trace.Indent();
			try
			{
				this.tプラグイン検索と生成();
				Trace.TraceInformation( "プラグインの検索と生成を完了しました。" );
			}
			finally
			{
				Trace.Unindent();
			}
			//---------------------
			#endregion
			#region [ プラグインの初期化 ]
			//---------------------
			if( this.PluginList != null && this.PluginList.Count > 0 )
			{
				Trace.TraceInformation( "プラグインの初期化を行います。" );
				Trace.Indent();
				try
				{
					foreach( STPlugin st in this.PluginList )
					{
						Directory.SetCurrentDirectory( st.pluginDirectory );
						st.plugin.On初期化( this.PluginHost );
						st.plugin.OnManagedリソースの作成();
						st.plugin.OnUnmanagedリソースの作成();
						Directory.SetCurrentDirectory( TJAPlayer3.strEXEのあるフォルダ );
					}
					Trace.TraceInformation( "すべてのプラグインの初期化を完了しました。" );
				}
				catch
				{
					Trace.TraceError( "プラグインのどれかの初期化に失敗しました。" );
					throw;
				}
				finally
				{
					Trace.Unindent();
				}
			}

            //---------------------
            #endregion

            #region Discordの処理
			DiscordClient = new DiscordRpcClient("939341030141096007");
			DiscordClient?.Initialize();
			StartupTime = DateTime.UtcNow;
			DiscordClient?.SetPresence(new RichPresence()
			{
				Details = "",
				State = "Startup",
				Timestamps = new Timestamps(TJAPlayer3.StartupTime),
				Assets = new Assets()
				{
					LargeImageKey = TJAPlayer3.LargeImageKey,
					LargeImageText = TJAPlayer3.LargeImageText,
				}
			});
            #endregion


            Trace.TraceInformation( "アプリケーションの初期化を完了しました。" );


            #region [ 最初のステージの起動 ]
            //---------------------
            Trace.TraceInformation( "----------------------" );
			Trace.TraceInformation( "■ 起動" );

			if ( TJAPlayer3.bコンパクトモード )
			{
				r現在のステージ = stage曲読み込み;
			}
			else
			{
				r現在のステージ = stage起動;
			}
			r現在のステージ.Activate();
			if (!ConfigIni.PreAssetsLoading) 
			{
				r現在のステージ.CreateManagedResource();
				r現在のステージ.CreateUnmanagedResource();
			}

			//---------------------
			#endregion
		}

		public void ShowWindowTitleWithSoundType()
		{
			string delay = "";
			if ( Sound管理.GetCurrentSoundDeviceType() != "DirectSound" )
			{
				delay = "(" + Sound管理.GetSoundDelay() + "ms)";
			}
            AssemblyName asmApp = Assembly.GetExecutingAssembly().GetName();
            base.Text = asmApp.Name + " Ver." + VERSION + " (" + Sound管理.GetCurrentSoundDeviceType() + delay + ")";
		}

		private void t終了処理()
		{
			if( !this.b終了処理完了済み )
			{
				Trace.TraceInformation( "----------------------" );
				Trace.TraceInformation( "■ アプリケーションの終了" );
				#region [ 曲検索の終了処理 ]
				//---------------------
				if ( actEnumSongs != null )
				{
					Trace.TraceInformation( "曲検索actの終了処理を行います。" );
					Trace.Indent();
					try
					{
						actEnumSongs.DeActivate();
						actEnumSongs= null;
						Trace.TraceInformation( "曲検索actの終了処理を完了しました。" );
					}
					catch ( Exception e )
					{
						Trace.TraceError( e.ToString() );
						Trace.TraceError( "曲検索actの終了処理に失敗しました。" );
					}
					finally
					{
						Trace.Unindent();
					}
				}
				//---------------------
				#endregion
				#region [ 現在のステージの終了処理 ]
				//---------------------
				if( TJAPlayer3.r現在のステージ != null && TJAPlayer3.r現在のステージ.IsActivated )		// #25398 2011.06.07 MODIFY FROM
				{
					Trace.TraceInformation( "現在のステージを終了します。" );
					Trace.Indent();
					try
					{
						r現在のステージ.DeActivate();
						if (!ConfigIni.PreAssetsLoading) 
						{
							r現在のステージ.ReleaseManagedResource();
							r現在のステージ.ReleaseUnmanagedResource();
						}
						Trace.TraceInformation( "現在のステージの終了処理を完了しました。" );
					}
					finally
					{
						Trace.Unindent();
					}
				}
				//---------------------
				#endregion
				#region [ プラグインの終了処理 ]
				//---------------------
				if (this.PluginList != null && this.PluginList.Count > 0)
				{
					Trace.TraceInformation( "すべてのプラグインを終了します。" );
					Trace.Indent();
					try
					{
						foreach( STPlugin st in this.PluginList )
						{
							Directory.SetCurrentDirectory( st.pluginDirectory );
							st.plugin.OnUnmanagedリソースの解放();
							st.plugin.OnManagedリソースの解放();
							st.plugin.On終了();
							Directory.SetCurrentDirectory( TJAPlayer3.strEXEのあるフォルダ );
						}
						PluginHost = null;
						Trace.TraceInformation( "すべてのプラグインの終了処理を完了しました。" );
					}
					finally
					{
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
                if (Songs管理 != null)
				{
					Trace.TraceInformation( "曲リストの終了処理を行います。" );
					Trace.Indent();
					try
					{
						Songs管理 = null;
						Trace.TraceInformation( "曲リストの終了処理を完了しました。" );
					}
					catch( Exception exception )
					{
						Trace.TraceError( exception.ToString() );
						Trace.TraceError( "曲リストの終了処理に失敗しました。" );
					}
					finally
					{
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
                if (Skin != null)
				{
					Trace.TraceInformation( "スキンの終了処理を行います。" );
					Trace.Indent();
					try
					{
						Skin.Dispose();
						Skin = null;
						Trace.TraceInformation( "スキンの終了処理を完了しました。" );
					}
					catch( Exception exception2 )
					{
						Trace.TraceError( exception2.ToString() );
						Trace.TraceError( "スキンの終了処理に失敗しました。" );
					}
					finally
					{
						Trace.Unindent();
					}
				}
				//---------------------
				#endregion
				#region [ DirectSoundの終了処理 ]
				//---------------------
				if (Sound管理 != null)
				{
					Trace.TraceInformation( "DirectSound の終了処理を行います。" );
					Trace.Indent();
					try
					{
						Sound管理.Dispose();
						Sound管理 = null;
						Trace.TraceInformation( "DirectSound の終了処理を完了しました。" );
					}
					catch( Exception exception3 )
					{
						Trace.TraceError( exception3.ToString() );
						Trace.TraceError( "DirectSound の終了処理に失敗しました。" );
					}
					finally
					{
						Trace.Unindent();
					}
				}
				//---------------------
				#endregion
				#region [ パッドの終了処理 ]
				//---------------------
				if (Pad != null)
				{
					Trace.TraceInformation( "パッドの終了処理を行います。" );
					Trace.Indent();
					try
					{
						Pad = null;
						Trace.TraceInformation( "パッドの終了処理を完了しました。" );
					}
					catch( Exception exception4 )
					{
						Trace.TraceError( exception4.ToString() );
						Trace.TraceError( "パッドの終了処理に失敗しました。" );
					}
					finally
					{
						Trace.Unindent();
					}
				}
				//---------------------
				#endregion
				#region [ DirectInput, MIDI入力の終了処理 ]
				//---------------------
				if (Input管理 != null)
				{
					Trace.TraceInformation( "DirectInput, MIDI入力の終了処理を行います。" );
					Trace.Indent();
					try
					{
						Input管理.Dispose();
						Input管理 = null;
						Trace.TraceInformation( "DirectInput, MIDI入力の終了処理を完了しました。" );
					}
					catch( Exception exception5 )
					{
						Trace.TraceError( exception5.ToString() );
						Trace.TraceError( "DirectInput, MIDI入力の終了処理に失敗しました。" );
					}
					finally
					{
						Trace.Unindent();
					}
				}
				//---------------------
				#endregion
				#region [ 文字コンソールの終了処理 ]
				//---------------------
				if (act文字コンソール != null)
				{
					Trace.TraceInformation( "文字コンソールの終了処理を行います。" );
					Trace.Indent();
					try
					{
						act文字コンソール.DeActivate();
						//if (!ConfigIni.PreAssetsLoading) 
						{
							act文字コンソール.ReleaseManagedResource();
							act文字コンソール.ReleaseUnmanagedResource();
						}
						act文字コンソール = null;
						Trace.TraceInformation( "文字コンソールの終了処理を完了しました。" );
					}
					catch( Exception exception6 )
					{
						Trace.TraceError( exception6.ToString() );
						Trace.TraceError( "文字コンソールの終了処理に失敗しました。" );
					}
					finally
					{
						Trace.Unindent();
					}
				}
				//---------------------
				#endregion
				#region [ FPSカウンタの終了処理 ]
				//---------------------
				Trace.TraceInformation("FPSカウンタの終了処理を行います。");
				Trace.Indent();
				try
				{
					if( FPS != null )
					{
						FPS = null;
					}
					Trace.TraceInformation( "FPSカウンタの終了処理を完了しました。" );
				}
				finally
				{
					Trace.Unindent();
				}
				//---------------------
				#endregion
				#region [ タイマの終了処理 ]
				//---------------------
				Trace.TraceInformation("タイマの終了処理を行います。");
				Trace.Indent();
				try
				{
					if( Timer != null )
					{
						Timer.Dispose();
						Timer = null;
						Trace.TraceInformation( "タイマの終了処理を完了しました。" );
					}
					else
					{
						Trace.TraceInformation( "タイマは使用されていません。" );
					}
				}
				finally
				{
					Trace.Unindent();
				}
				//---------------------
				#endregion
				#region [ Config.iniの出力 ]
				//---------------------
				Trace.TraceInformation("Config.ini を出力します。");
//				if ( ConfigIni.bIsSwappedGuitarBass )			// #24063 2011.1.16 yyagi ギターベースがスワップしているときは元に戻す
				string str = strEXEのあるフォルダ + "Config.ini";
				Trace.Indent();
				try
				{
					if ( DTXVmode.Enabled )
					{
						DTXVmode.tUpdateConfigIni();
						Trace.TraceInformation( "DTXVモードの設定情報を、Config.iniに保存しました。" );
					}
					else
					{
						ConfigIni.t書き出し( str );
						Trace.TraceInformation( "保存しました。({0})", str );
					}
				}
				catch( Exception e )
				{
					Trace.TraceError( e.ToString() );
					Trace.TraceError( "Config.ini の出力に失敗しました。({0})", str );
				}
				finally
				{
					Trace.Unindent();
				}

			    Trace.TraceInformation("Deinitializing loudness scanning, song gain control, and sound group level control...");
			    Trace.Indent();
			    try
			    {
			        SoundGroupLevelController = null;
			        SongGainController = null;
			        LoudnessMetadataScanner.StopBackgroundScanning(joinImmediately: true);
                    actScanningLoudness.DeActivate();
					if (!ConfigIni.PreAssetsLoading) 
					{
						actScanningLoudness.ReleaseManagedResource();
						actScanningLoudness.ReleaseUnmanagedResource();
					}
			        actScanningLoudness = null;
			    }
			    finally
			    {
			        Trace.Unindent();
			        Trace.TraceInformation("Deinitialized loudness scanning, song gain control, and sound group level control.");
			    }

			    ConfigIni = null;

				//---------------------
				#endregion
				#region [ DTXVmodeの終了処理 ]
				//---------------------
				//Trace.TraceInformation( "DTXVモードの終了処理を行います。" );
				//Trace.Indent();
				try
				{
					if ( DTXVmode != null )
					{
						DTXVmode = null;
						//Trace.TraceInformation( "DTXVモードの終了処理を完了しました。" );
					}
					else
					{
						//Trace.TraceInformation( "DTXVモードは使用されていません。" );
					}
				}
				finally
				{
					//Trace.Unindent();
				}
				//---------------------
				#endregion
                Trace.TraceInformation( "アプリケーションの終了処理を完了しました。" );


				this.b終了処理完了済み = true;
			}
		}
		private CScoreIni tScoreIniへBGMAdjustとHistoryとPlayCountを更新(string str新ヒストリ行)
		{
			bool bIsUpdatedDrums, bIsUpdatedGuitar, bIsUpdatedBass;
			string strFilename = DTX.strファイル名の絶対パス + ".score.ini";
			CScoreIni ini = new CScoreIni( strFilename );
			if( !File.Exists( strFilename ) )
			{
				ini.stファイル.Title = DTX.TITLE;
				ini.stファイル.Name = DTX.strファイル名;
				ini.stファイル.Hash = CScoreIni.tファイルのMD5を求めて返す( DTX.strファイル名の絶対パス );
				for( int i = 0; i < 6; i++ )
				{
					ini.stセクション[ i ].nPerfectになる範囲ms = nPerfect範囲ms;
					ini.stセクション[ i ].nGreatになる範囲ms = nGreat範囲ms;
					ini.stセクション[ i ].nGoodになる範囲ms = nGood範囲ms;
					ini.stセクション[ i ].nPoorになる範囲ms = nPoor範囲ms;
				}
			}
			ini.stファイル.BGMAdjust = DTX.nBGMAdjust;
			CScoreIni.t更新条件を取得する( out bIsUpdatedDrums, out bIsUpdatedGuitar, out bIsUpdatedBass );
			if( bIsUpdatedDrums || bIsUpdatedGuitar || bIsUpdatedBass )
			{
				if( bIsUpdatedDrums )
				{
					ini.stファイル.PlayCountDrums++;
				}
				if( bIsUpdatedGuitar )
				{
					ini.stファイル.PlayCountGuitar++;
				}
				if( bIsUpdatedBass )
				{
					ini.stファイル.PlayCountBass++;
				}
				ini.tヒストリを追加する( str新ヒストリ行 );
				if( !bコンパクトモード )
				{
					stage選曲.r確定されたスコア.譜面情報.演奏回数.Drums = ini.stファイル.PlayCountDrums;
					stage選曲.r確定されたスコア.譜面情報.演奏回数.Guitar = ini.stファイル.PlayCountGuitar;
					stage選曲.r確定されたスコア.譜面情報.演奏回数.Bass = ini.stファイル.PlayCountBass;
					for( int j = 0; j < ini.stファイル.History.Length; j++ )
					{
						stage選曲.r確定されたスコア.譜面情報.演奏履歴[ j ] = ini.stファイル.History[ j ];
					}
				}
			}
			if( ConfigIni.bScoreIniを出力する )
			{
				ini.t書き出し( strFilename );
			}

			return ini;
		}
		private void tガベージコレクションを実行する()
		{
			GC.Collect(GC.MaxGeneration);
			GC.WaitForPendingFinalizers();
			GC.Collect(GC.MaxGeneration);
		}
		private void tプラグイン検索と生成()
		{
			this.PluginList = new List<STPlugin>();

			string PluginActivityName = typeof( IPluginActivity ).FullName;
			string PluginFolderPath = strEXEのあるフォルダ + "Plugins" + Path.DirectorySeparatorChar;

			this.SearchAndGeneratePluginsInFolder( PluginFolderPath, PluginActivityName );

			if( this.PluginList.Count > 0 )
				Trace.TraceInformation( this.PluginList.Count + " 個のプラグインを読み込みました。" );
		}

		private void ChangeResolution(int nWidth, int nHeight)
		{
			GameWindowSize.Width = nWidth;
			GameWindowSize.Height = nHeight;
			
			WindowSize = new Silk.NET.Maths.Vector2D<int>(nWidth, nHeight);
		}

		public void RefleshSkin()
        {
            Trace.TraceInformation("スキン変更:" + TJAPlayer3.Skin.GetCurrentSkinSubfolderFullName(false));

            TJAPlayer3.act文字コンソール.DeActivate();
			act文字コンソール.ReleaseManagedResource();
			act文字コンソール.ReleaseUnmanagedResource();

            TJAPlayer3.Skin.Dispose();
            TJAPlayer3.Skin = null;
            TJAPlayer3.Skin = new CSkin(TJAPlayer3.ConfigIni.strSystemSkinSubfolderFullName, false);

			TJAPlayer3.Tx.DisposeTexture();

			ChangeResolution(TJAPlayer3.Skin.Resolution[0], TJAPlayer3.Skin.Resolution[1]);

			TJAPlayer3.Tx.LoadTexture();

            TJAPlayer3.act文字コンソール.Activate();
			act文字コンソール.CreateManagedResource();
			act文字コンソール.CreateUnmanagedResource();
			TJAPlayer3.NamePlate.RefleshSkin();
			CActSelectPopupMenu.RefleshSkin();
			CActSelect段位リスト.RefleshSkin();
		}
		#region [ Windowイベント処理 ]
		private void SearchAndGeneratePluginsInFolder( string PluginFolderPath, string PluginTypeName )
		{
			// 指定されたパスが存在しないとエラー
			if( !Directory.Exists( PluginFolderPath ) )
			{
				Trace.TraceWarning( "The plugin folder does not exist. (" + PluginFolderPath + ")" );
				return;
			}

			// (1) すべての *.dll について…
			string[] strDLLs = System.IO.Directory.GetFiles( PluginFolderPath, "*.dll" );
			foreach( string dllName in strDLLs )
			{
				try
				{
					// (1-1) dll をアセンブリとして読み込む。
					System.Reflection.Assembly asm = System.Reflection.Assembly.LoadFrom( dllName );

					// (1-2) アセンブリ内のすべての型について、プラグインとして有効か調べる
					foreach( Type t in asm.GetTypes() )
					{
						//  (1-3) ↓クラスであり↓Publicであり↓抽象クラスでなく↓IPlugin型のインスタンスが作れる　型を持っていれば有効
						if( t.IsClass && t.IsPublic && !t.IsAbstract && t.GetInterface( PluginTypeName ) != null )
						{
							// (1-4) クラス名からインスタンスを作成する
							var st = new STPlugin() {
								plugin = (IPluginActivity) asm.CreateInstance( t.FullName ),
								pluginDirectory = Path.GetDirectoryName( dllName ),
								assemblyName = asm.GetName().Name,
								Version = asm.GetName().Version,
							};

							// (1-5) プラグインリストへ登録
							this.PluginList.Add( st );
							Trace.TraceInformation( "Plugin {0} ({1}, {2}, {3}) has been loaded.", t.FullName, Path.GetFileName( dllName ), st.assemblyName, st.Version.ToString() );
						}
					}
				}
				catch (Exception e)
				{
					Trace.TraceError( e.ToString() );
					Trace.TraceInformation( dllName + "could not be used to generate a plugin. Skipping plugin." );
				}
			}

			// (2) サブフォルダがあれば再帰する
			string[] strDirs = Directory.GetDirectories( PluginFolderPath, "*" );
			foreach( string dir in strDirs )
				this.SearchAndGeneratePluginsInFolder( dir + Path.DirectorySeparatorChar, PluginTypeName );
		}
		//-----------------
		/*
		private void Window_ResizeEnd(object sender, EventArgs e)				// #23510 2010.11.20 yyagi: to get resized window size
		{
			if ( ConfigIni.bウィンドウモード )
			{
				ConfigIni.n初期ウィンドウ開始位置X = base.Window.Location.X;	// #30675 2013.02.04 ikanick add
				ConfigIni.n初期ウィンドウ開始位置Y = base.Window.Location.Y;	//
			}

			ConfigIni.nウインドウwidth = (ConfigIni.bウィンドウモード) ? base.Window.ClientSize.Width : currentClientSize.Width;	// #23510 2010.10.31 yyagi add
			ConfigIni.nウインドウheight = (ConfigIni.bウィンドウモード) ? base.Window.ClientSize.Height : currentClientSize.Height;
		}
		*/
		#endregion
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
