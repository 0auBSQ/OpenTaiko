using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Drawing;
using System.Threading;
using FDK;
using SkiaSharp;

namespace TJAPlayer3
{
	internal class CActConfigList : CActivity
	{
		// プロパティ

		public bool bIsKeyAssignSelected		// #24525 2011.3.15 yyagi
		{
			get
			{
				Eメニュー種別 e = this.eメニュー種別;
				if (e == Eメニュー種別.KeyAssignDrums || e == Eメニュー種別.KeyAssignSystem)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}
		public bool bIsFocusingParameter		// #32059 2013.9.17 yyagi
		{
			get
			{
				return b要素値にフォーカス中;
			}
		}
		public bool b現在選択されている項目はReturnToMenuである
		{
			get
			{
				CItemBase currentItem = this.list項目リスト[ this.n現在の選択項目 ];
				if (currentItem == this.iSystemReturnToMenu || currentItem == this.iDrumsReturnToMenu)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}
		public CItemBase ib現在の選択項目
		{
			get
			{
				return this.list項目リスト[ this.n現在の選択項目 ];
			}
		}
		public int n現在の選択項目;


		// General system options
		#region [ t項目リストの設定_System() ]

		public void t項目リストの設定_System(bool refresh = true)
		{
			this.tConfigIniへ記録する();
			this.list項目リスト.Clear();

			// #27029 2012.1.5 from: 説明文は最大9行→13行に変更。

			this.iSystemReturnToMenu = new CItemBase(CLangManager.LangInstance.GetString(2), CItemBase.Eパネル種別.その他,
				CLangManager.LangInstance.GetString(3));
			this.list項目リスト.Add( this.iSystemReturnToMenu );

			this.iSystemReloadDTX = new CItemBase(CLangManager.LangInstance.GetString(4), CItemBase.Eパネル種別.通常,
				CLangManager.LangInstance.GetString(5));
			this.list項目リスト.Add( this.iSystemReloadDTX );

			this.iSystemLanguage = new CItemList(CLangManager.LangInstance.GetString(1), CItemList.Eパネル種別.通常, CLangManager.langToInt(TJAPlayer3.ConfigIni.sLang),
				CLangManager.LangInstance.GetString(0),
				CLangManager.Languages);
			this.list項目リスト.Add(this.iSystemLanguage);

			this.iLayoutType = new CItemInteger(CLangManager.LangInstance.GetString(16), 0, (int)eLayoutType.TOTAL - 1, TJAPlayer3.ConfigIni.nLayoutType,
				CLangManager.LangInstance.GetString(17));
			this.list項目リスト.Add(this.iLayoutType);

			this.iTaikoPlayerCount = new CItemInteger(CLangManager.LangInstance.GetString(6), 1, 5, TJAPlayer3.ConfigIni.nPlayerCount,
				CLangManager.LangInstance.GetString(7));
            this.list項目リスト.Add( this.iTaikoPlayerCount );

			this.iDanTowerHide = new CItemToggle(CLangManager.LangInstance.GetString(10056), TJAPlayer3.ConfigIni.bDanTowerHide,
				CLangManager.LangInstance.GetString(10057));
			this.list項目リスト.Add(this.iDanTowerHide);

			/*
			this.iSystemRisky = new CItemInteger(CLangManager.LangInstance.GetString(8), 0, 10, TJAPlayer3.ConfigIni.nRisky,
				CLangManager.LangInstance.GetString(9));
			this.list項目リスト.Add( this.iSystemRisky );
			*/

			this.iCommonPlaySpeed = new CItemInteger(CLangManager.LangInstance.GetString(10), 5, 400, TJAPlayer3.ConfigIni.n演奏速度,
				CLangManager.LangInstance.GetString(11));
			this.list項目リスト.Add( this.iCommonPlaySpeed );

			this.iSystemTimeStretch = new CItemToggle(CLangManager.LangInstance.GetString(10018), TJAPlayer3.ConfigIni.bTimeStretch,
				CLangManager.LangInstance.GetString(18));
			this.list項目リスト.Add( this.iSystemTimeStretch );

			this.iSystemGraphicsType = new CItemList(CLangManager.LangInstance.GetString(9997), CItemList.Eパネル種別.通常, TJAPlayer3.ConfigIni.nGraphicsDeviceType,
				CLangManager.LangInstance.GetString(9996),
				//new string[] { "OpenGL", "DirectX9", "DirectX11", "Vulkan", "Metal" });
				new string[] { "OpenGL", "DirectX11", "Vulkan", "Metal" });
			this.list項目リスト.Add(this.iSystemGraphicsType);

			this.iSystemFullscreen = new CItemToggle(CLangManager.LangInstance.GetString(10019), TJAPlayer3.ConfigIni.b全画面モード,
				CLangManager.LangInstance.GetString(19));
			this.list項目リスト.Add( this.iSystemFullscreen );

			this.iSystemRandomFromSubBox = new CItemToggle(CLangManager.LangInstance.GetString(10021), TJAPlayer3.ConfigIni.bランダムセレクトで子BOXを検索対象とする,
				CLangManager.LangInstance.GetString(21));
			this.list項目リスト.Add( this.iSystemRandomFromSubBox );

			this.iSystemVSyncWait = new CItemToggle(CLangManager.LangInstance.GetString(10022), TJAPlayer3.ConfigIni.b垂直帰線待ちを行う,
				CLangManager.LangInstance.GetString(22));
			this.list項目リスト.Add( this.iSystemVSyncWait );

			this.iSystemAVI = new CItemToggle(CLangManager.LangInstance.GetString(10023), TJAPlayer3.ConfigIni.bAVI有効,
				CLangManager.LangInstance.GetString(23));
			this.list項目リスト.Add( this.iSystemAVI );

			this.iSystemBGA = new CItemToggle(CLangManager.LangInstance.GetString(10024), TJAPlayer3.ConfigIni.bBGA有効,
				CLangManager.LangInstance.GetString(24));
			this.list項目リスト.Add( this.iSystemBGA );

			this.iSystemPreviewSoundWait = new CItemInteger(CLangManager.LangInstance.GetString(10025), 0, 0x2710, TJAPlayer3.ConfigIni.n曲が選択されてからプレビュー音が鳴るまでのウェイトms,
				CLangManager.LangInstance.GetString(25));
			this.list項目リスト.Add( this.iSystemPreviewSoundWait );

			this.iSystemPreviewImageWait = new CItemInteger(CLangManager.LangInstance.GetString(10026), 0, 0x2710, TJAPlayer3.ConfigIni.n曲が選択されてからプレビュー画像が表示開始されるまでのウェイトms,
				CLangManager.LangInstance.GetString(26));
			this.list項目リスト.Add( this.iSystemPreviewImageWait );

			this.iSystemDebugInfo = new CItemToggle(CLangManager.LangInstance.GetString(10027), TJAPlayer3.ConfigIni.b演奏情報を表示する,
				CLangManager.LangInstance.GetString(27));
			this.list項目リスト.Add( this.iSystemDebugInfo );

			this.iSystemBGAlpha = new CItemInteger(CLangManager.LangInstance.GetString(10028), 0, 0xff, TJAPlayer3.ConfigIni.n背景の透過度,
				CLangManager.LangInstance.GetString(28));
			this.list項目リスト.Add( this.iSystemBGAlpha );

			this.iSystemBGMSound = new CItemToggle(CLangManager.LangInstance.GetString(10029), TJAPlayer3.ConfigIni.bBGM音を発声する,
				CLangManager.LangInstance.GetString(29));
			this.list項目リスト.Add( this.iSystemBGMSound );


			this.iSystemSaveScore = new CItemToggle(CLangManager.LangInstance.GetString(10030), TJAPlayer3.ConfigIni.bScoreIniを出力する,
				CLangManager.LangInstance.GetString(30));
			this.list項目リスト.Add( this.iSystemSaveScore );

		    this.iSystemApplySongVol = new CItemToggle(CLangManager.LangInstance.GetString(10033), TJAPlayer3.ConfigIni.ApplySongVol,
				CLangManager.LangInstance.GetString(33));
		    this.list項目リスト.Add( this.iSystemApplySongVol );

		    this.iSystemSoundEffectLevel = new CItemInteger(CLangManager.LangInstance.GetString(10034), CSound.MinimumGroupLevel, CSound.MaximumGroupLevel, TJAPlayer3.ConfigIni.SoundEffectLevel,
				CLangManager.LangInstance.GetString(34));
		    this.list項目リスト.Add( this.iSystemSoundEffectLevel );

		    this.iSystemVoiceLevel = new CItemInteger(CLangManager.LangInstance.GetString(10035), CSound.MinimumGroupLevel, CSound.MaximumGroupLevel, TJAPlayer3.ConfigIni.VoiceLevel,
				CLangManager.LangInstance.GetString(35));
		    this.list項目リスト.Add( this.iSystemVoiceLevel );

		    this.iSystemSongPreviewLevel = new CItemInteger(CLangManager.LangInstance.GetString(10058), CSound.MinimumGroupLevel, CSound.MaximumGroupLevel, TJAPlayer3.ConfigIni.SongPreviewLevel,
				CLangManager.LangInstance.GetString(10059));
		    this.list項目リスト.Add( this.iSystemSongPreviewLevel );
			
			this.iSystemSongPlaybackLevel = new CItemInteger(CLangManager.LangInstance.GetString(10036), CSound.MinimumGroupLevel, CSound.MaximumGroupLevel, TJAPlayer3.ConfigIni.SongPlaybackLevel,
				CLangManager.LangInstance.GetString(36));
		    this.list項目リスト.Add( this.iSystemSongPlaybackLevel );

		    this.iSystemKeyboardSoundLevelIncrement = new CItemInteger(CLangManager.LangInstance.GetString(10037), 1, 20, TJAPlayer3.ConfigIni.KeyboardSoundLevelIncrement,
				CLangManager.LangInstance.GetString(37));
		    this.list項目リスト.Add( this.iSystemKeyboardSoundLevelIncrement );

            this.MusicPreTimeMs = new CItemInteger(CLangManager.LangInstance.GetString(10038), 0, 10000, TJAPlayer3.ConfigIni.MusicPreTimeMs,
				CLangManager.LangInstance.GetString(38));
            this.list項目リスト.Add(this.MusicPreTimeMs);

            //this.iSystemStoicMode = new CItemToggle( "StoicMode", CDTXMania.ConfigIni.bストイックモード,
            //    "ストイック（禁欲）モード：\n以下をまとめて表示ON/OFFします。\n_プレビュー画像/動画\n_リザルト画像/動画\n_NowLoading画像\n_演奏画面の背景画像\n_BGA 画像 / AVI 動画\n_グラフ画像\n",
            //    "Turn ON to disable drawing\n * preview image / movie\n * result image / movie\n * nowloading image\n * wallpaper (in playing screen)\n * BGA / AVI (in playing screen)" );
            //this.list項目リスト.Add( this.iSystemStoicMode );

            this.iSystemAutoResultCapture = new CItemToggle(CLangManager.LangInstance.GetString(10039), TJAPlayer3.ConfigIni.bIsAutoResultCapture,
				CLangManager.LangInstance.GetString(39));
			this.list項目リスト.Add( this.iSystemAutoResultCapture );

            SendDiscordPlayingInformation = new CItemToggle(CLangManager.LangInstance.GetString(10040),
                TJAPlayer3.ConfigIni.SendDiscordPlayingInformation,
				CLangManager.LangInstance.GetString(40));
            list項目リスト.Add(SendDiscordPlayingInformation);

            this.iSystemBufferedInput = new CItemToggle(CLangManager.LangInstance.GetString(10041), TJAPlayer3.ConfigIni.bBufferedInputs,
				CLangManager.LangInstance.GetString(41));
			this.list項目リスト.Add( this.iSystemBufferedInput );
			this.iLogOutputLog = new CItemToggle(CLangManager.LangInstance.GetString(10042), TJAPlayer3.ConfigIni.bログ出力,
				CLangManager.LangInstance.GetString(42));
			this.list項目リスト.Add( this.iLogOutputLog );

			// #24820 2013.1.3 yyagi


			this.iSystemSoundType = new CItemList(CLangManager.LangInstance.GetString(10043), CItemList.Eパネル種別.通常, TJAPlayer3.ConfigIni.nSoundDeviceType,
				CLangManager.LangInstance.GetString(43),
				new string[] { "Bass", "ASIO", "WASAPI Exclusive", "WASAPI Shared" });
			this.list項目リスト.Add(this.iSystemSoundType);

			// #24820 2013.1.15 yyagi
			this.iSystemBassBufferSizeMs = new CItemInteger(CLangManager.LangInstance.GetString(9999), 0, 99999, TJAPlayer3.ConfigIni.nBassBufferSizeMs,
				CLangManager.LangInstance.GetString(9998));
			this.list項目リスト.Add( this.iSystemBassBufferSizeMs );

			// #24820 2013.1.15 yyagi
			this.iSystemWASAPIBufferSizeMs = new CItemInteger(CLangManager.LangInstance.GetString(10044), 0, 99999, TJAPlayer3.ConfigIni.nWASAPIBufferSizeMs,
				CLangManager.LangInstance.GetString(44));
			this.list項目リスト.Add( this.iSystemWASAPIBufferSizeMs );

			// #24820 2013.1.17 yyagi
			string[] asiodevs = CEnumerateAllAsioDevices.GetAllASIODevices();
			this.iSystemASIODevice = new CItemList(CLangManager.LangInstance.GetString(10045), CItemList.Eパネル種別.通常, TJAPlayer3.ConfigIni.nASIODevice,
				CLangManager.LangInstance.GetString(45),
				asiodevs );
			this.list項目リスト.Add( this.iSystemASIODevice );

			// #33689 2014.6.17 yyagi
			this.iSystemSoundTimerType = new CItemToggle(CLangManager.LangInstance.GetString(10046), TJAPlayer3.ConfigIni.bUseOSTimer,
				CLangManager.LangInstance.GetString(46));
			this.list項目リスト.Add( this.iSystemSoundTimerType );


            ShowChara = new CItemToggle(CLangManager.LangInstance.GetString(10047), TJAPlayer3.ConfigIni.ShowChara,
				CLangManager.LangInstance.GetString(47));
            this.list項目リスト.Add(ShowChara);

            ShowDancer = new CItemToggle(CLangManager.LangInstance.GetString(10048), TJAPlayer3.ConfigIni.ShowDancer,
				CLangManager.LangInstance.GetString(48));
            this.list項目リスト.Add(ShowDancer);

            ShowMob = new CItemToggle(CLangManager.LangInstance.GetString(10049), TJAPlayer3.ConfigIni.ShowMob,
				CLangManager.LangInstance.GetString(49));
            this.list項目リスト.Add(ShowMob);

            ShowRunner = new CItemToggle(CLangManager.LangInstance.GetString(10050), TJAPlayer3.ConfigIni.ShowRunner,
				CLangManager.LangInstance.GetString(50));
            this.list項目リスト.Add(ShowRunner);

            ShowFooter = new CItemToggle(CLangManager.LangInstance.GetString(10051), TJAPlayer3.ConfigIni.ShowFooter,
				CLangManager.LangInstance.GetString(51));
            this.list項目リスト.Add(ShowFooter);

            FastRender = new CItemToggle(CLangManager.LangInstance.GetString(10052), TJAPlayer3.ConfigIni.FastRender,
				CLangManager.LangInstance.GetString(52));
            this.list項目リスト.Add(FastRender);

            ASyncTextureLoad = new CItemToggle(CLangManager.LangInstance.GetString(9995), TJAPlayer3.ConfigIni.ASyncTextureLoad,
				CLangManager.LangInstance.GetString(9994));
            this.list項目リスト.Add(ASyncTextureLoad);

            ShowPuchiChara = new CItemToggle(CLangManager.LangInstance.GetString(10053), TJAPlayer3.ConfigIni.ShowPuchiChara,
				CLangManager.LangInstance.GetString(53));
            this.list項目リスト.Add(ShowPuchiChara);

            SimpleMode = new CItemToggle(CLangManager.LangInstance.GetString(9993), TJAPlayer3.ConfigIni.SimpleMode,
                CLangManager.LangInstance.GetString(9992));
            this.list項目リスト.Add(SimpleMode);



            this.iSystemSkinSubfolder = new CItemList(CLangManager.LangInstance.GetString(10054), CItemBase.Eパネル種別.通常, nSkinIndex,
				CLangManager.LangInstance.GetString(54),
                //"CONFIGURATIONを抜けると、設定した\n" +
                //"スキンに変更されます。",
                skinNames);
            this.list項目リスト.Add(this.iSystemSkinSubfolder);



            this.iSystemGoToKeyAssign = new CItemBase(CLangManager.LangInstance.GetString(10055), CItemBase.Eパネル種別.通常,
			CLangManager.LangInstance.GetString(55));
			this.list項目リスト.Add( this.iSystemGoToKeyAssign );

			OnListMenuの初期化();
			if (refresh)
            {
				this.n現在の選択項目 = 0;
				this.eメニュー種別 = Eメニュー種別.System;
			}
            
		}
		#endregion


		// Gameplay options 
		#region [ t項目リストの設定_Drums() ]

		public void t項目リストの設定_Drums()
		{
			this.tConfigIniへ記録する();
			this.list項目リスト.Clear();

			// #27029 2012.1.5 from: 説明文は最大9行→13行に変更。

			this.iDrumsReturnToMenu = new CItemBase(CLangManager.LangInstance.GetString(2), CItemBase.Eパネル種別.その他,
				CLangManager.LangInstance.GetString(3));
			this.list項目リスト.Add( this.iDrumsReturnToMenu );

			this.iRollsPerSec = new CItemInteger(CLangManager.LangInstance.GetString(60), 0, 1000, TJAPlayer3.ConfigIni.nRollsPerSec,
				CLangManager.LangInstance.GetString(61));
			this.list項目リスト.Add(this.iRollsPerSec);

			this.iAILevel = new CItemInteger(CLangManager.LangInstance.GetString(12), 1, 10, TJAPlayer3.ConfigIni.nAILevel,
				CLangManager.LangInstance.GetString(13));
			this.list項目リスト.Add(this.iAILevel);

			this.iSystemRisky = new CItemInteger(CLangManager.LangInstance.GetString(64), 0, 10, TJAPlayer3.ConfigIni.nRisky,
				CLangManager.LangInstance.GetString(65));
			this.list項目リスト.Add( this.iSystemRisky );

			this.iTaikoNoInfo = new CItemToggle(CLangManager.LangInstance.GetString(70), TJAPlayer3.ConfigIni.bNoInfo,
				CLangManager.LangInstance.GetString(71));
			this.list項目リスト.Add( this.iTaikoNoInfo );

			this.iDrumsTight = new CItemToggle(CLangManager.LangInstance.GetString(74), TJAPlayer3.ConfigIni.bTight,
				CLangManager.LangInstance.GetString(75));
			this.list項目リスト.Add( this.iDrumsTight );
            
			this.iSystemMinComboDrums = new CItemInteger(CLangManager.LangInstance.GetString(76), 1, 0x1869f, TJAPlayer3.ConfigIni.n表示可能な最小コンボ数.Drums,
				CLangManager.LangInstance.GetString(77));
			this.list項目リスト.Add( this.iSystemMinComboDrums );


			// #23580 2011.1.3 yyagi
			
			/*
			this.iInputAdjustTimeMs = new CItemInteger(CLangManager.LangInstance.GetString(78), -9999, 9999, TJAPlayer3.ConfigIni.nInputAdjustTimeMs,
				CLangManager.LangInstance.GetString(79));
			this.list項目リスト.Add( this.iInputAdjustTimeMs );
			*/

			this.iGlobalOffsetMs = new CItemInteger(CLangManager.LangInstance.GetString(14), -9999, 9999, TJAPlayer3.ConfigIni.nGlobalOffsetMs,
				CLangManager.LangInstance.GetString(15));
			this.list項目リスト.Add(this.iGlobalOffsetMs);


			this.iTaikoDefaultCourse = new CItemList(CLangManager.LangInstance.GetString(80), CItemBase.Eパネル種別.通常, TJAPlayer3.ConfigIni.nDefaultCourse,
				CLangManager.LangInstance.GetString(81),
                new string[] {
					CLangManager.LangInstance.GetString(1040),
					CLangManager.LangInstance.GetString(1041),
					CLangManager.LangInstance.GetString(1042),
					CLangManager.LangInstance.GetString(1043),
					CLangManager.LangInstance.GetString(1044),
					CLangManager.LangInstance.GetString(1045) });
            this.list項目リスト.Add(this.iTaikoDefaultCourse);

            this.iTaikoScoreMode = new CItemList(CLangManager.LangInstance.GetString(82), CItemBase.Eパネル種別.通常, TJAPlayer3.ConfigIni.nScoreMode,
				CLangManager.LangInstance.GetString(83),
                new string[] { "TYPE-A", "TYPE-B", "TYPE-C"});
            this.list項目リスト.Add(this.iTaikoScoreMode);

            this.ShinuchiMode = new CItemToggle(CLangManager.LangInstance.GetString(10084), TJAPlayer3.ConfigIni.ShinuchiMode, CItemBase.Eパネル種別.通常,
				CLangManager.LangInstance.GetString(84));
            this.list項目リスト.Add(this.ShinuchiMode);

            this.iTaikoBranchGuide = new CItemToggle(CLangManager.LangInstance.GetString(85), TJAPlayer3.ConfigIni.bBranchGuide,
				CLangManager.LangInstance.GetString(86));
            this.list項目リスト.Add(this.iTaikoBranchGuide);

            this.iTaikoBranchAnime = new CItemList(CLangManager.LangInstance.GetString(87), CItemBase.Eパネル種別.通常, TJAPlayer3.ConfigIni.nBranchAnime,
				CLangManager.LangInstance.GetString(88),
                new string[] { "TYPE-A", "TYPE-B" });
            this.list項目リスト.Add(this.iTaikoBranchAnime);

            this.iTaikoGameMode = new CItemList(CLangManager.LangInstance.GetString(89), CItemBase.Eパネル種別.通常, (int)TJAPlayer3.ConfigIni.eGameMode,
				CLangManager.LangInstance.GetString(90),
                new string[] { "OFF", "TYPE-A", "TYPE-B" });
            this.list項目リスト.Add( this.iTaikoGameMode );

            this.iTaikoBigNotesJudge = new CItemToggle(CLangManager.LangInstance.GetString(91), TJAPlayer3.ConfigIni.bJudgeBigNotes,
				CLangManager.LangInstance.GetString(92));
            this.list項目リスト.Add( this.iTaikoBigNotesJudge );

            this.iTaikoForceNormalGauge = new CItemToggle(CLangManager.LangInstance.GetString(10126), TJAPlayer3.ConfigIni.bForceNormalGauge,
				CLangManager.LangInstance.GetString(10127));
            this.list項目リスト.Add( this.iTaikoForceNormalGauge );

            this.iTaikoJudgeCountDisp = new CItemToggle(CLangManager.LangInstance.GetString(93), TJAPlayer3.ConfigIni.bJudgeCountDisplay,
				CLangManager.LangInstance.GetString(94));
            this.list項目リスト.Add( this.iTaikoJudgeCountDisp );

			this.iShowExExtraAnime = new CItemToggle(CLangManager.LangInstance.GetString(10124), TJAPlayer3.ConfigIni.ShowExExtraAnime,
				CLangManager.LangInstance.GetString(10125));
			this.list項目リスト.Add(this.iShowExExtraAnime);

			this.iDrumsGoToKeyAssign = new CItemBase(CLangManager.LangInstance.GetString(95), CItemBase.Eパネル種別.通常,
				CLangManager.LangInstance.GetString(96));
			this.list項目リスト.Add( this.iDrumsGoToKeyAssign );

            OnListMenuの初期化();
			this.n現在の選択項目 = 0;
			this.eメニュー種別 = Eメニュー種別.Drums;
		}

		#endregion








		/// <summary>Sud+Hidの初期値を返す</summary>
		/// <param name="eInst"></param>
		/// <returns>
		/// 0: None
		/// 1: Sudden
		/// 2: Hidden
		/// 3: Sud+Hid
		/// 4: Semi-Invisible
		/// 5: Full-Invisible
		/// </returns>
		private int getDefaultSudHidValue( E楽器パート eInst )
		{
			int defvar;
			int nInst = (int) eInst;
			if ( TJAPlayer3.ConfigIni.eInvisible[ nInst ] != EInvisible.OFF )
			{
				defvar = (int) TJAPlayer3.ConfigIni.eInvisible[ nInst ] + 3;
			}
			else
			{
				defvar = ( TJAPlayer3.ConfigIni.bSudden[ nInst ] ? 1 : 0 ) +
						 ( TJAPlayer3.ConfigIni.bHidden[ nInst ] ? 2 : 0 );
			}
			return defvar;
		}

		/// <summary>
		/// ESC押下時の右メニュー描画
		/// </summary>
		public void tEsc押下()
		{
			if ( this.b要素値にフォーカス中 )		// #32059 2013.9.17 add yyagi
			{
				this.b要素値にフォーカス中 = false;
			}

			if ( this.eメニュー種別 == Eメニュー種別.KeyAssignSystem )
			{
				t項目リストの設定_System();
			}
			else if ( this.eメニュー種別 == Eメニュー種別.KeyAssignDrums )
			{
				t項目リストの設定_Drums();
			}
			// これ以外なら何もしない
		}
		public void tEnter押下()
		{
			TJAPlayer3.Skin.sound決定音.t再生する();
			if( this.b要素値にフォーカス中 )
			{
				this.b要素値にフォーカス中 = false;
			}
			else if( this.list項目リスト[ this.n現在の選択項目 ].e種別 == CItemBase.E種別.整数 )
			{
				this.b要素値にフォーカス中 = true;
			}
			else if( this.b現在選択されている項目はReturnToMenuである )
			{
				//this.tConfigIniへ記録する();
				//CONFIG中にスキン変化が発生すると面倒なので、一旦マスクした。
			}
			#region [ 個々のキーアサイン ]
            //太鼓のキー設定。
			else if( this.list項目リスト[ this.n現在の選択項目 ] == this.iKeyAssignTaikoLRed )
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知( EKeyConfigPart.DRUMS, EKeyConfigPad.LRed );
			}
			else if( this.list項目リスト[ this.n現在の選択項目 ] == this.iKeyAssignTaikoRRed )
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知( EKeyConfigPart.DRUMS, EKeyConfigPad.RRed );
			}
			else if( this.list項目リスト[ this.n現在の選択項目 ] == this.iKeyAssignTaikoLBlue )
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知( EKeyConfigPart.DRUMS, EKeyConfigPad.LBlue );
			}
			else if ( this.list項目リスト[ this.n現在の選択項目 ] == this.iKeyAssignTaikoRBlue )
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知( EKeyConfigPart.DRUMS, EKeyConfigPad.RBlue );
			}

            //太鼓のキー設定。2P
			else if( this.list項目リスト[ this.n現在の選択項目 ] == this.iKeyAssignTaikoLRed2P )
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知( EKeyConfigPart.DRUMS, EKeyConfigPad.LRed2P );
			}
			else if( this.list項目リスト[ this.n現在の選択項目 ] == this.iKeyAssignTaikoRRed2P )
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知( EKeyConfigPart.DRUMS, EKeyConfigPad.RRed2P );
			}
			else if( this.list項目リスト[ this.n現在の選択項目 ] == this.iKeyAssignTaikoLBlue2P )
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知( EKeyConfigPart.DRUMS, EKeyConfigPad.LBlue2P );
			}
			else if ( this.list項目リスト[ this.n現在の選択項目 ] == this.iKeyAssignTaikoRBlue2P )
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知( EKeyConfigPart.DRUMS, EKeyConfigPad.RBlue2P );
			}

			//太鼓のキー設定。3P
			else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoLRed3P)
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知(EKeyConfigPart.DRUMS, EKeyConfigPad.LRed3P);
			}
			else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoRRed3P)
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知(EKeyConfigPart.DRUMS, EKeyConfigPad.RRed3P);
			}
			else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoLBlue3P)
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知(EKeyConfigPart.DRUMS, EKeyConfigPad.LBlue3P);
			}
			else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoRBlue3P)
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知(EKeyConfigPart.DRUMS, EKeyConfigPad.RBlue3P);
			}

			//太鼓のキー設定。4P
			else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoLRed4P)
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知(EKeyConfigPart.DRUMS, EKeyConfigPad.LRed4P);
			}
			else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoRRed4P)
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知(EKeyConfigPart.DRUMS, EKeyConfigPad.RRed4P);
			}
			else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoLBlue4P)
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知(EKeyConfigPart.DRUMS, EKeyConfigPad.LBlue4P);
			}
			else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoRBlue4P)
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知(EKeyConfigPart.DRUMS, EKeyConfigPad.RBlue4P);
			}

			//太鼓のキー設定。5P
			else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoLRed5P)
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知(EKeyConfigPart.DRUMS, EKeyConfigPad.LRed5P);
			}
			else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoRRed5P)
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知(EKeyConfigPart.DRUMS, EKeyConfigPad.RRed5P);
			}
			else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoLBlue5P)
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知(EKeyConfigPart.DRUMS, EKeyConfigPad.LBlue5P);
			}
			else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignTaikoRBlue5P)
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知(EKeyConfigPart.DRUMS, EKeyConfigPad.RBlue5P);
			}

			// Konga claps
			else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignKongaClap)
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知(EKeyConfigPart.DRUMS, EKeyConfigPad.Clap);
			}
			else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignKongaClap2P)
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知(EKeyConfigPart.DRUMS, EKeyConfigPad.Clap2P);
			}
			else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignKongaClap3P)
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知(EKeyConfigPart.DRUMS, EKeyConfigPad.Clap3P);
			}
			else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignKongaClap4P)
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知(EKeyConfigPart.DRUMS, EKeyConfigPad.Clap4P);
			}
			else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignKongaClap5P)
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知(EKeyConfigPart.DRUMS, EKeyConfigPad.Clap5P);
			}

			else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignDecide)
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知(EKeyConfigPart.DRUMS, EKeyConfigPad.Decide);
			}
			else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignCancel)
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知(EKeyConfigPart.DRUMS, EKeyConfigPad.Cancel);
			}
			else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignLeftChange)
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知(EKeyConfigPart.DRUMS, EKeyConfigPad.LeftChange);
			}
			else if (this.list項目リスト[this.n現在の選択項目] == this.iKeyAssignRightChange)
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知(EKeyConfigPart.DRUMS, EKeyConfigPad.RightChange);
			}

			else if ( this.list項目リスト[ this.n現在の選択項目 ] == this.iKeyAssignSystemCapture )
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知( EKeyConfigPart.SYSTEM, EKeyConfigPad.Capture);
			}
			else if ( this.list項目リスト[ this.n現在の選択項目 ] == this.iKeyAssignSystemSongVolIncrease )
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知( EKeyConfigPart.SYSTEM, EKeyConfigPad.SongVolumeIncrease);
			}
			else if ( this.list項目リスト[ this.n現在の選択項目 ] == this.iKeyAssignSystemSongVolDecrease )
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知( EKeyConfigPart.SYSTEM, EKeyConfigPad.SongVolumeDecrease);
			}
			else if ( this.list項目リスト[ this.n現在の選択項目 ] == this.iKeyAssignSystemDisplayHit )
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知( EKeyConfigPart.SYSTEM, EKeyConfigPad.DisplayHits);
			}
			else if ( this.list項目リスト[ this.n現在の選択項目 ] == this.iKeyAssignSystemDisplayDebug )
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知( EKeyConfigPart.SYSTEM, EKeyConfigPad.DisplayDebug);
			}
			else if ( this.list項目リスト[ this.n現在の選択項目 ] == this.iKeyAssignSystemQuickConfig )
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知( EKeyConfigPart.SYSTEM, EKeyConfigPad.QuickConfig);
			}
			else if ( this.list項目リスト[ this.n現在の選択項目 ] == this.iKeyAssignSystemNewHeya )
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知( EKeyConfigPart.SYSTEM, EKeyConfigPad.NewHeya);
			}
			else if ( this.list項目リスト[ this.n現在の選択項目 ] == this.iKeyAssignSystemSortSongs )
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知( EKeyConfigPart.SYSTEM, EKeyConfigPad.SortSongs);
			}
			else if ( this.list項目リスト[ this.n現在の選択項目 ] == this.iKeyAssignSystemToggleAutoP1 )
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知( EKeyConfigPart.SYSTEM, EKeyConfigPad.ToggleAutoP1);
			}
			else if ( this.list項目リスト[ this.n現在の選択項目 ] == this.iKeyAssignSystemToggleAutoP2 )
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知( EKeyConfigPart.SYSTEM, EKeyConfigPad.ToggleAutoP2);
			}
			else if ( this.list項目リスト[ this.n現在の選択項目 ] == this.iKeyAssignSystemToggleTrainingMode )
			{
				TJAPlayer3.stageコンフィグ.tパッド選択通知( EKeyConfigPart.SYSTEM, EKeyConfigPad.ToggleTrainingMode);
			}
			#endregion
			else
			{
		 		// #27029 2012.1.5 from
                //if( ( this.iSystemBDGroup.n現在選択されている項目番号 == (int) EBDGroup.どっちもBD ) &&
                //    ( ( this.list項目リスト[ this.n現在の選択項目 ] == this.iSystemHHGroup ) || ( this.list項目リスト[ this.n現在の選択項目 ] == this.iSystemHitSoundPriorityHH ) ) )
                //{
                //    // 変更禁止（何もしない）
                //}
                //else
                //{
                //    // 変更許可
                this.list項目リスト[ this.n現在の選択項目 ].tEnter押下();

				if (this.list項目リスト[this.n現在の選択項目] == this.iSystemLanguage)
                {
					TJAPlayer3.ConfigIni.sLang = CLangManager.intToLang(this.iSystemLanguage.n現在選択されている項目番号);
					CLangManager.langAttach(TJAPlayer3.ConfigIni.sLang);
					t項目リストの設定_System(refresh : false);
					TJAPlayer3.stageコンフィグ.ReloadMenus();
				}
                //}


				// Enter押下後の後処理

				if( this.list項目リスト[ this.n現在の選択項目 ] == this.iSystemFullscreen )
				{
					TJAPlayer3.app.b次のタイミングで全画面_ウィンドウ切り替えを行う = true;
				}
				else if( this.list項目リスト[ this.n現在の選択項目 ] == this.iSystemVSyncWait )
				{
					TJAPlayer3.ConfigIni.b垂直帰線待ちを行う = this.iSystemVSyncWait.bON;
					TJAPlayer3.app.b次のタイミングで垂直帰線同期切り替えを行う = true;
				}
				#region [ キーアサインへの遷移と脱出 ]
				else if ( this.list項目リスト[ this.n現在の選択項目 ] == this.iSystemGoToKeyAssign )			// #24609 2011.4.12 yyagi
				{
					t項目リストの設定_KeyAssignSystem();
				}
				else if ( this.list項目リスト[ this.n現在の選択項目 ] == this.iKeyAssignSystemReturnToMenu )	// #24609 2011.4.12 yyagi
				{
					t項目リストの設定_System();
				}
				else if ( this.list項目リスト[ this.n現在の選択項目 ] == this.iDrumsGoToKeyAssign )				// #24525 2011.3.15 yyagi
				{
					t項目リストの設定_KeyAssignDrums();
				}
				else if ( this.list項目リスト[ this.n現在の選択項目 ] == this.iKeyAssignDrumsReturnToMenu )		// #24525 2011.3.15 yyagi
				{
					t項目リストの設定_Drums();
				}
				#endregion
				#region [ スキン項目でEnterを押下した場合に限り、スキンの縮小サンプルを生成する。]
				else if ( this.list項目リスト[ this.n現在の選択項目 ] == this.iSystemSkinSubfolder )			// #28195 2012.5.2 yyagi
				{
					tGenerateSkinSample();
				}
				#endregion
				#region [ 曲データ一覧の再読み込み ]
				else if ( this.list項目リスト[ this.n現在の選択項目 ] == this.iSystemReloadDTX )				// #32081 2013.10.21 yyagi
				{
					if ( TJAPlayer3.EnumSongs.IsEnumerating )
					{
						// Debug.WriteLine( "バックグラウンドでEnumeratingSongs中だったので、一旦中断します。" );
						TJAPlayer3.EnumSongs.Abort();
						TJAPlayer3.actEnumSongs.DeActivate();
					}

					TJAPlayer3.EnumSongs.StartEnumFromDisk();
					TJAPlayer3.EnumSongs.ChangeEnumeratePriority( ThreadPriority.Normal );
					TJAPlayer3.actEnumSongs.bコマンドでの曲データ取得 = true;
					TJAPlayer3.actEnumSongs.Activate();
					// TJAPlayer3.stage選曲.Refresh(TJAPlayer3.EnumSongs.Songs管理, true);

					TJAPlayer3.stage選曲.act曲リスト.ResetSongIndex();
				}
				#endregion
			}
		}

		private void tGenerateSkinSample()
		{

			nSkinIndex = ( ( CItemList ) this.list項目リスト[ this.n現在の選択項目 ] ).n現在選択されている項目番号;
			if ( nSkinSampleIndex != nSkinIndex )
			{
				string path = skinSubFolders[ nSkinIndex ];
				path = System.IO.Path.Combine( path, @$"Graphics{Path.DirectorySeparatorChar}1_Title{Path.DirectorySeparatorChar}Background.png" );
				SKBitmap bmSrc = SKBitmap.Decode(path);

				int _w = TJAPlayer3.Skin.Resolution[0] / 4;// bmSrc.Width / 4;
				int _h = TJAPlayer3.Skin.Resolution[1] / 4;// bmSrc.Height / 4;

				if ( txSkinSample1 != null )
				{
					TJAPlayer3.t安全にDisposeする( ref txSkinSample1 );
				}
				txSkinSample1 = TJAPlayer3.tテクスチャの生成( bmSrc, false );

				txSkinSample1.vc拡大縮小倍率 = new Silk.NET.Maths.Vector3D<float>(_w / (float)txSkinSample1.szテクスチャサイズ.Width, _h / (float)txSkinSample1.szテクスチャサイズ.Height, 0);

				bmSrc.Dispose();
				nSkinSampleIndex = nSkinIndex;
			}
		}

		#region [ 項目リストの設定 ( Exit, KeyAssignSystem/Drums) ]
		public void t項目リストの設定_Exit()
		{
			this.tConfigIniへ記録する();
			this.eメニュー種別 = Eメニュー種別.Unknown;
		}
		public void t項目リストの設定_KeyAssignSystem()
		{
			//this.tConfigIniへ記録する();
			this.list項目リスト.Clear();

			// #27029 2012.1.5 from: 説明文は最大9行→13行に変更。

			this.iKeyAssignSystemReturnToMenu = new CItemBase(CLangManager.LangInstance.GetString(2), CItemBase.Eパネル種別.その他,
				CLangManager.LangInstance.GetString(3));
			this.list項目リスト.Add( this.iKeyAssignSystemReturnToMenu );

			this.iKeyAssignSystemCapture = new CItemBase(CLangManager.LangInstance.GetString(97),
				CLangManager.LangInstance.GetString(98));
			this.list項目リスト.Add( this.iKeyAssignSystemCapture );
			this.iKeyAssignSystemSongVolIncrease = new CItemBase(CLangManager.LangInstance.GetString(10128),
				CLangManager.LangInstance.GetString(10129));
			this.list項目リスト.Add( this.iKeyAssignSystemSongVolIncrease );
			this.iKeyAssignSystemSongVolDecrease = new CItemBase(CLangManager.LangInstance.GetString(10130),
				CLangManager.LangInstance.GetString(10131));
			this.list項目リスト.Add( this.iKeyAssignSystemSongVolDecrease );
			this.iKeyAssignSystemDisplayHit = new CItemBase(CLangManager.LangInstance.GetString(10132),
				CLangManager.LangInstance.GetString(10133));
			this.list項目リスト.Add( this.iKeyAssignSystemDisplayHit );
			this.iKeyAssignSystemDisplayDebug = new CItemBase(CLangManager.LangInstance.GetString(10134),
				CLangManager.LangInstance.GetString(10135));
			this.list項目リスト.Add( this.iKeyAssignSystemDisplayDebug );
			this.iKeyAssignSystemQuickConfig = new CItemBase(CLangManager.LangInstance.GetString(10136),
				CLangManager.LangInstance.GetString(10137));
			this.list項目リスト.Add( this.iKeyAssignSystemQuickConfig );
			this.iKeyAssignSystemNewHeya = new CItemBase(CLangManager.LangInstance.GetString(10138),
				CLangManager.LangInstance.GetString(10139));
			this.list項目リスト.Add( this.iKeyAssignSystemNewHeya );
			this.iKeyAssignSystemSortSongs = new CItemBase(CLangManager.LangInstance.GetString(10140),
				CLangManager.LangInstance.GetString(10141));
			this.list項目リスト.Add( this.iKeyAssignSystemSortSongs );
			this.iKeyAssignSystemToggleAutoP1 = new CItemBase(CLangManager.LangInstance.GetString(10142),
				CLangManager.LangInstance.GetString(10143));
			this.list項目リスト.Add( this.iKeyAssignSystemToggleAutoP1);
			this.iKeyAssignSystemToggleAutoP2 = new CItemBase(CLangManager.LangInstance.GetString(10144),
				CLangManager.LangInstance.GetString(10145));
			this.list項目リスト.Add( this.iKeyAssignSystemToggleAutoP2);
			this.iKeyAssignSystemToggleTrainingMode = new CItemBase(CLangManager.LangInstance.GetString(10146),
				CLangManager.LangInstance.GetString(10147));
			this.list項目リスト.Add( this.iKeyAssignSystemToggleTrainingMode);

            OnListMenuの初期化();
			this.n現在の選択項目 = 0;
			this.eメニュー種別 = Eメニュー種別.KeyAssignSystem;
		}
		public void t項目リストの設定_KeyAssignDrums()
		{
//			this.tConfigIniへ記録する();
			this.list項目リスト.Clear();

			// #27029 2012.1.5 from: 説明文は最大9行→13行に変更。

			this.iKeyAssignDrumsReturnToMenu = new CItemBase(CLangManager.LangInstance.GetString(2), CItemBase.Eパネル種別.その他,
				CLangManager.LangInstance.GetString(3));
			this.list項目リスト.Add(this.iKeyAssignDrumsReturnToMenu);

			this.iKeyAssignTaikoLRed = new CItemBase(CLangManager.LangInstance.GetString(99),
				CLangManager.LangInstance.GetString(10000));
			this.list項目リスト.Add(this.iKeyAssignTaikoLRed);
			this.iKeyAssignTaikoRRed = new CItemBase(CLangManager.LangInstance.GetString(10001),
				CLangManager.LangInstance.GetString(10002));
			this.list項目リスト.Add(this.iKeyAssignTaikoRRed);
			this.iKeyAssignTaikoLBlue = new CItemBase(CLangManager.LangInstance.GetString(10003),
				CLangManager.LangInstance.GetString(10004));
			this.list項目リスト.Add( this.iKeyAssignTaikoLBlue );
            this.iKeyAssignTaikoRBlue = new CItemBase(CLangManager.LangInstance.GetString(10005),
				CLangManager.LangInstance.GetString(10006));
			this.list項目リスト.Add( this.iKeyAssignTaikoRBlue );

			this.iKeyAssignTaikoLRed2P = new CItemBase(CLangManager.LangInstance.GetString(10007),
				CLangManager.LangInstance.GetString(10008));
			this.list項目リスト.Add( this.iKeyAssignTaikoLRed2P );
			this.iKeyAssignTaikoRRed2P = new CItemBase(CLangManager.LangInstance.GetString(10009),
				CLangManager.LangInstance.GetString(10010));
			this.list項目リスト.Add( this.iKeyAssignTaikoRRed2P );
			this.iKeyAssignTaikoLBlue2P = new CItemBase(CLangManager.LangInstance.GetString(10011),
				CLangManager.LangInstance.GetString(10012));
			this.list項目リスト.Add( this.iKeyAssignTaikoLBlue2P );
            this.iKeyAssignTaikoRBlue2P = new CItemBase(CLangManager.LangInstance.GetString(10013),
				CLangManager.LangInstance.GetString(10014));
			this.list項目リスト.Add( this.iKeyAssignTaikoRBlue2P );

			this.iKeyAssignTaikoLRed3P = new CItemBase(CLangManager.LangInstance.GetString(10094),
				CLangManager.LangInstance.GetString(10095));
			this.list項目リスト.Add(this.iKeyAssignTaikoLRed3P);
			this.iKeyAssignTaikoRRed3P = new CItemBase(CLangManager.LangInstance.GetString(10096),
				CLangManager.LangInstance.GetString(10097));
			this.list項目リスト.Add(this.iKeyAssignTaikoRRed3P);
			this.iKeyAssignTaikoLBlue3P = new CItemBase(CLangManager.LangInstance.GetString(10098),
				CLangManager.LangInstance.GetString(10099));
			this.list項目リスト.Add(this.iKeyAssignTaikoLBlue3P);
			this.iKeyAssignTaikoRBlue3P = new CItemBase(CLangManager.LangInstance.GetString(10100),
				CLangManager.LangInstance.GetString(10101));
			this.list項目リスト.Add(this.iKeyAssignTaikoRBlue3P);

			this.iKeyAssignTaikoLRed4P = new CItemBase(CLangManager.LangInstance.GetString(10102),
				CLangManager.LangInstance.GetString(10103));
			this.list項目リスト.Add(this.iKeyAssignTaikoLRed4P);
			this.iKeyAssignTaikoRRed4P = new CItemBase(CLangManager.LangInstance.GetString(10104),
				CLangManager.LangInstance.GetString(10105));
			this.list項目リスト.Add(this.iKeyAssignTaikoRRed4P);
			this.iKeyAssignTaikoLBlue4P = new CItemBase(CLangManager.LangInstance.GetString(10106),
				CLangManager.LangInstance.GetString(10107));
			this.list項目リスト.Add(this.iKeyAssignTaikoLBlue4P);
			this.iKeyAssignTaikoRBlue4P = new CItemBase(CLangManager.LangInstance.GetString(10108),
				CLangManager.LangInstance.GetString(10109));
			this.list項目リスト.Add(this.iKeyAssignTaikoRBlue4P);

			this.iKeyAssignTaikoLRed5P = new CItemBase(CLangManager.LangInstance.GetString(10110),
				CLangManager.LangInstance.GetString(10111));
			this.list項目リスト.Add(this.iKeyAssignTaikoLRed5P);
			this.iKeyAssignTaikoRRed5P = new CItemBase(CLangManager.LangInstance.GetString(10112),
				CLangManager.LangInstance.GetString(10113));
			this.list項目リスト.Add(this.iKeyAssignTaikoRRed5P);
			this.iKeyAssignTaikoLBlue5P = new CItemBase(CLangManager.LangInstance.GetString(10114),
				CLangManager.LangInstance.GetString(10115));
			this.list項目リスト.Add(this.iKeyAssignTaikoLBlue5P);
			this.iKeyAssignTaikoRBlue5P = new CItemBase(CLangManager.LangInstance.GetString(10116),
				CLangManager.LangInstance.GetString(10117));
			this.list項目リスト.Add(this.iKeyAssignTaikoRBlue5P);

			this.iKeyAssignKongaClap = new CItemBase(CLangManager.LangInstance.GetString(10060),
				CLangManager.LangInstance.GetString(10061));
			this.list項目リスト.Add(this.iKeyAssignKongaClap);

			this.iKeyAssignKongaClap2P = new CItemBase(CLangManager.LangInstance.GetString(10062),
				CLangManager.LangInstance.GetString(10063));
			this.list項目リスト.Add(this.iKeyAssignKongaClap2P);

			this.iKeyAssignKongaClap3P = new CItemBase(CLangManager.LangInstance.GetString(10118),
				CLangManager.LangInstance.GetString(10119));
			this.list項目リスト.Add(this.iKeyAssignKongaClap3P);

			this.iKeyAssignKongaClap4P = new CItemBase(CLangManager.LangInstance.GetString(10120),
				CLangManager.LangInstance.GetString(10121));
			this.list項目リスト.Add(this.iKeyAssignKongaClap4P);

			this.iKeyAssignKongaClap5P = new CItemBase(CLangManager.LangInstance.GetString(10122),
				CLangManager.LangInstance.GetString(10123));
			this.list項目リスト.Add(this.iKeyAssignKongaClap5P);

			this.iKeyAssignDecide = new CItemBase(CLangManager.LangInstance.GetString(10064),
				CLangManager.LangInstance.GetString(10065));
			this.list項目リスト.Add(this.iKeyAssignDecide);
			this.iKeyAssignCancel = new CItemBase(CLangManager.LangInstance.GetString(10066),
				CLangManager.LangInstance.GetString(10067));
			this.list項目リスト.Add(this.iKeyAssignCancel);

			this.iKeyAssignLeftChange = new CItemBase(CLangManager.LangInstance.GetString(10068),
				CLangManager.LangInstance.GetString(10069));
			this.list項目リスト.Add(this.iKeyAssignLeftChange);
			this.iKeyAssignRightChange = new CItemBase(CLangManager.LangInstance.GetString(10070),
				CLangManager.LangInstance.GetString(10071));
			this.list項目リスト.Add(this.iKeyAssignRightChange);

			OnListMenuの初期化();
			this.n現在の選択項目 = 0;
			this.eメニュー種別 = Eメニュー種別.KeyAssignDrums;
		}
		#endregion
		public void t次に移動()
		{
			TJAPlayer3.Skin.soundカーソル移動音.t再生する();
			if( this.b要素値にフォーカス中 )
			{
				this.list項目リスト[ this.n現在の選択項目 ].t項目値を前へ移動();
				t要素値を上下に変更中の処理();
			}
			else
			{
				this.n目標のスクロールカウンタ += 100;
			}
		}
		public void t前に移動()
		{
			TJAPlayer3.Skin.soundカーソル移動音.t再生する();
			if( this.b要素値にフォーカス中 )
			{
				this.list項目リスト[ this.n現在の選択項目 ].t項目値を次へ移動();
				t要素値を上下に変更中の処理();
			}
			else
			{
				this.n目標のスクロールカウンタ -= 100;
			}
		}
		private void t要素値を上下に変更中の処理()
		{
			//if ( this.list項目リスト[ this.n現在の選択項目 ] == this.iSystemMasterVolume )				// #33700 2014.4.26 yyagi
			//{
			//    CDTXMania.Sound管理.nMasterVolume = this.iSystemMasterVolume.n現在の値;
			//}
		}


		// CActivity 実装

		public override void Activate()
		{
			if( this.IsActivated )
				return;

			this.list項目リスト = new List<CItemBase>();
			this.eメニュー種別 = Eメニュー種別.Unknown;

			#region [ スキン選択肢と、現在選択中のスキン(index)の準備 #28195 2012.5.2 yyagi ]
			int ns = ( TJAPlayer3.Skin.strSystemSkinSubfolders == null ) ? 0 : TJAPlayer3.Skin.strSystemSkinSubfolders.Length;
			int nb = ( TJAPlayer3.Skin.strBoxDefSkinSubfolders == null ) ? 0 : TJAPlayer3.Skin.strBoxDefSkinSubfolders.Length;
			skinSubFolders = new string[ ns + nb ];
			for ( int i = 0; i < ns; i++ )
			{
				skinSubFolders[ i ] = TJAPlayer3.Skin.strSystemSkinSubfolders[ i ];
			}
			for ( int i = 0; i < nb; i++ )
			{
				skinSubFolders[ ns + i ] = TJAPlayer3.Skin.strBoxDefSkinSubfolders[ i ];
			}
			skinSubFolder_org = TJAPlayer3.Skin.GetCurrentSkinSubfolderFullName( true );
			Array.Sort( skinSubFolders );
			skinNames = CSkin.GetSkinName( skinSubFolders );
			nSkinIndex = Array.BinarySearch( skinSubFolders, skinSubFolder_org );
			if ( nSkinIndex < 0 )	// 念のため
			{
				nSkinIndex = 0;
			}
			nSkinSampleIndex = -1;
			#endregion

            //			this.listMenu = new List<stMenuItemRight>();

			this.t項目リストの設定_Drums();	// 
			this.t項目リストの設定_System();	// 順番として、最後にSystemを持ってくること。設定一覧の初期位置がSystemのため。
			this.b要素値にフォーカス中 = false;
			this.n目標のスクロールカウンタ = 0;
			this.n現在のスクロールカウンタ = 0;
			this.nスクロール用タイマ値 = -1;
			this.ct三角矢印アニメ = new CCounter();

			this.iSystemSoundType_initial			= this.iSystemSoundType.n現在選択されている項目番号;	// CONFIGに入ったときの値を保持しておく
			this.iSystemBassBufferSizeMs_initial	= this.iSystemBassBufferSizeMs.n現在の値;				// CONFIG脱出時にこの値から変更されているようなら
			this.iSystemWASAPIBufferSizeMs_initial	= this.iSystemWASAPIBufferSizeMs.n現在の値;				// CONFIG脱出時にこの値から変更されているようなら
			// this.iSystemASIOBufferSizeMs_initial	= this.iSystemASIOBufferSizeMs.n現在の値;				// サウンドデバイスを再構築する
			this.iSystemASIODevice_initial			= this.iSystemASIODevice.n現在選択されている項目番号;	//
			this.iSystemSoundTimerType_initial      = this.iSystemSoundTimerType.GetIndex();				//
			base.Activate();
		}
		public override void DeActivate()
		{
			if( this.IsDeActivated )
				return;

			this.tConfigIniへ記録する();
			this.list項目リスト.Clear();
			this.ct三角矢印アニメ = null;
            
			base.DeActivate();
			#region [ Skin変更 ]
			if ( TJAPlayer3.Skin.GetCurrentSkinSubfolderFullName( true ) != this.skinSubFolder_org )
			{
                TJAPlayer3.app.RefleshSkin();
            }
			#endregion

			// #24820 2013.1.22 yyagi CONFIGでWASAPI/ASIO/DirectSound関連の設定を変更した場合、サウンドデバイスを再構築する。
			// #33689 2014.6.17 yyagi CONFIGでSoundTimerTypeの設定を変更した場合も、サウンドデバイスを再構築する。
			#region [ サウンドデバイス変更 ]
			if ( this.iSystemSoundType_initial != this.iSystemSoundType.n現在選択されている項目番号 ||
				 this.iSystemBassBufferSizeMs_initial != this.iSystemBassBufferSizeMs.n現在の値 ||
				 this.iSystemWASAPIBufferSizeMs_initial != this.iSystemWASAPIBufferSizeMs.n現在の値 ||
				// this.iSystemASIOBufferSizeMs_initial != this.iSystemASIOBufferSizeMs.n現在の値 ||
				this.iSystemASIODevice_initial != this.iSystemASIODevice.n現在選択されている項目番号 ||
				this.iSystemSoundTimerType_initial != this.iSystemSoundTimerType.GetIndex() )
			{
				ESoundDeviceType soundDeviceType;
				switch (this.iSystemSoundType.n現在選択されている項目番号)
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
				TJAPlayer3.Sound管理.tInitialize( soundDeviceType,
										this.iSystemBassBufferSizeMs.n現在の値,
										this.iSystemWASAPIBufferSizeMs.n現在の値,
										0,
										// this.iSystemASIOBufferSizeMs.n現在の値,
										this.iSystemASIODevice.n現在選択されている項目番号,
										this.iSystemSoundTimerType.bON );
				TJAPlayer3.app.ShowWindowTitleWithSoundType();
				TJAPlayer3.Skin.ReloadSkin();// 音声の再読み込みをすることによって、音量の初期化を防ぐ
			}
			#endregion
			#region [ サウンドのタイムストレッチモード変更 ]
			FDK.SoundManager.bIsTimeStretch = this.iSystemTimeStretch.bON;
			#endregion
		}
		public override void CreateManagedResource()
		{
			this.prvFont = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.Config_Font_Scale);	// t項目リストの設定 の前に必要

			//this.tx通常項目行パネル = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\4_itembox.png" ), false );
			//this.txその他項目行パネル = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\4_itembox other.png" ), false );
			//this.tx三角矢印 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\4_triangle arrow.png" ), false );
			this.txSkinSample1 = null;		// スキン選択時に動的に設定するため、ここでは初期化しない
			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource()
		{
			prvFont.Dispose();
			
			TJAPlayer3.tテクスチャの解放( ref this.txSkinSample1 );
			//CDTXMania.tテクスチャの解放( ref this.tx通常項目行パネル );
			//CDTXMania.tテクスチャの解放( ref this.txその他項目行パネル );
			//CDTXMania.tテクスチャの解放( ref this.tx三角矢印 );
		
			base.ReleaseManagedResource();
		}
		private void OnListMenuの初期化()
		{
			OnListMenuの解放();
			this.listMenu = new stMenuItemRight[ this.list項目リスト.Count ];
		}

		/// <summary>
		/// 事前にレンダリングしておいたテクスチャを解放する。
		/// </summary>
		private void OnListMenuの解放()
		{
			if ( listMenu != null )
			{
				for ( int i = 0; i < listMenu.Length; i++ )
				{
					if ( listMenu[ i ].txParam != null )
					{
						listMenu[ i ].txParam.Dispose();
					}
					if ( listMenu[ i ].txMenuItemRight != null )
					{
						listMenu[ i ].txMenuItemRight.Dispose();
					}
				}
				this.listMenu = null;
			}
		}
		public override int Draw()
		{
			throw new InvalidOperationException( "t進行描画(bool)のほうを使用してください。" );
		}
		public int t進行描画( bool b項目リスト側にフォーカスがある )
		{
			if( this.IsDeActivated )
				return 0;

			// 進行

			#region [ 初めての進行描画 ]
			//-----------------
			if( base.IsFirstDraw )
			{
				this.nスクロール用タイマ値 = (long)(SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed);
				this.ct三角矢印アニメ.Start( 0, 9, 50, TJAPlayer3.Timer );
			
				base.IsFirstDraw = false;
			}
			//-----------------
			#endregion

			this.b項目リスト側にフォーカスがある = b項目リスト側にフォーカスがある;		// 記憶

			#region [ 項目スクロールの進行 ]
			//-----------------
			long n現在時刻 = TJAPlayer3.Timer.NowTime;
			if( n現在時刻 < this.nスクロール用タイマ値 ) this.nスクロール用タイマ値 = n現在時刻;

			const int INTERVAL = 2;	// [ms]
			while( ( n現在時刻 - this.nスクロール用タイマ値 ) >= INTERVAL )
			{
				int n目標項目までのスクロール量 = Math.Abs( (int) ( this.n目標のスクロールカウンタ - this.n現在のスクロールカウンタ ) );
				int n加速度 = 0;

				#region [ n加速度の決定；目標まで遠いほど加速する。]
				//-----------------
				if( n目標項目までのスクロール量 <= 100 )
				{
					n加速度 = 2;
				}
				else if( n目標項目までのスクロール量 <= 300 )
				{
					n加速度 = 3;
				}
				else if( n目標項目までのスクロール量 <= 500 )
				{
					n加速度 = 4;
				}
				else
				{
					n加速度 = 8;
				}
				//-----------------
				#endregion
				#region [ this.n現在のスクロールカウンタに n加速度 を加減算。]
				//-----------------
				if( this.n現在のスクロールカウンタ < this.n目標のスクロールカウンタ )
				{
					this.n現在のスクロールカウンタ += n加速度;
					if( this.n現在のスクロールカウンタ > this.n目標のスクロールカウンタ )
					{
						// 目標を超えたら目標値で停止。
						this.n現在のスクロールカウンタ = this.n目標のスクロールカウンタ;
					}
				}
				else if( this.n現在のスクロールカウンタ > this.n目標のスクロールカウンタ )
				{
					this.n現在のスクロールカウンタ -= n加速度;
					if( this.n現在のスクロールカウンタ < this.n目標のスクロールカウンタ )
					{
						// 目標を超えたら目標値で停止。
						this.n現在のスクロールカウンタ = this.n目標のスクロールカウンタ;
					}
				}
				//-----------------
				#endregion
				#region [ 行超え処理、ならびに目標位置に到達したらスクロールを停止して項目変更通知を発行。]
				//-----------------
				if( this.n現在のスクロールカウンタ >= 100 )
				{
					this.n現在の選択項目 = this.t次の項目( this.n現在の選択項目 );
					this.n現在のスクロールカウンタ -= 100;
					this.n目標のスクロールカウンタ -= 100;
					if( this.n目標のスクロールカウンタ == 0 )
					{
						TJAPlayer3.stageコンフィグ.t項目変更通知();
					}
				}
				else if( this.n現在のスクロールカウンタ <= -100 )
				{
					this.n現在の選択項目 = this.t前の項目( this.n現在の選択項目 );
					this.n現在のスクロールカウンタ += 100;
					this.n目標のスクロールカウンタ += 100;
					if( this.n目標のスクロールカウンタ == 0 )
					{
						TJAPlayer3.stageコンフィグ.t項目変更通知();
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
			if( this.b項目リスト側にフォーカスがある && ( this.n目標のスクロールカウンタ == 0 ) )
				this.ct三角矢印アニメ.TickLoop();
			//-----------------
			#endregion


			// 描画

			//this.ptパネルの基本座標[ 4 ].X = this.b項目リスト側にフォーカスがある ? 0x228 : 0x25a;		// メニューにフォーカスがあるなら、項目リストの中央は頭を出さない。

			#region [ 計11個の項目パネルを描画する。]
			//-----------------
			int nItem = this.n現在の選択項目;
			for( int i = 0; i < (TJAPlayer3.Skin.Config_ItemBox_Count / 2) - 1; i++ )
				nItem = this.t前の項目( nItem );

			for( int i = 0; i < TJAPlayer3.Skin.Config_ItemBox_Count; i++ )		// n行番号 == 0 がフォーカスされている項目パネル。
			{
				bool centerFlag = i == (TJAPlayer3.Skin.Config_ItemBox_Count / 2) - 1;

				#region [ 今まさに画面外に飛びだそうとしている項目パネルは描画しない。]
				//-----------------
				if ( ( ( i == 0 ) && ( this.n現在のスクロールカウンタ > 0 ) ) ||		// 上に飛び出そうとしている
					( ( i == TJAPlayer3.Skin.Config_ItemBox_Count - 1) && ( this.n現在のスクロールカウンタ < 0 ) ) )		// 下に飛び出そうとしている
				{
					nItem = this.t次の項目( nItem );
					continue;
				}
				//-----------------
				#endregion

				int n移動先の行の基本位置 = ( this.n現在のスクロールカウンタ <= 0 ) ? ( ( i + 1 ) % TJAPlayer3.Skin.Config_ItemBox_Count) : ( ( ( i - 1 ) + TJAPlayer3.Skin.Config_ItemBox_Count) % TJAPlayer3.Skin.Config_ItemBox_Count);
				int x = TJAPlayer3.Skin.Config_ItemBox_X[ i ] + ( (int) ( (TJAPlayer3.Skin.Config_ItemBox_X[ n移動先の行の基本位置 ] - TJAPlayer3.Skin.Config_ItemBox_X[ i ] ) * ( ( (double) Math.Abs( this.n現在のスクロールカウンタ ) ) / 100.0 ) ) );
				int y = TJAPlayer3.Skin.Config_ItemBox_Y[ i ] + ( (int) ( (TJAPlayer3.Skin.Config_ItemBox_Y[ n移動先の行の基本位置 ] - TJAPlayer3.Skin.Config_ItemBox_Y[ i ] ) * ( ( (double) Math.Abs( this.n現在のスクロールカウンタ ) ) / 100.0 ) ) );

				#region [ 現在の行の項目パネル枠を描画。]
				//-----------------
				switch( this.list項目リスト[ nItem ].eパネル種別 )
				{
					case CItemBase.Eパネル種別.通常:
                    case CItemBase.Eパネル種別.その他:
                        if ( TJAPlayer3.Tx.Config_ItemBox != null )
                            TJAPlayer3.Tx.Config_ItemBox.t2D描画( x, y );
						break;
				}
				//-----------------
				#endregion
				#region [ 現在の行の項目名を描画。]
				//-----------------
				if ( listMenu[ nItem ].txMenuItemRight != null )	// 自前のキャッシュに含まれているようなら、再レンダリングせずキャッシュを使用
				{
                    listMenu[nItem].txMenuItemRight.t2D描画(x + TJAPlayer3.Skin.Config_ItemBox_Font_Offset[0], y + TJAPlayer3.Skin.Config_ItemBox_Font_Offset[1]);
				}
				else
				{
					using (var bmpItem = prvFont.DrawText( this.list項目リスト[ nItem ].str項目名, Color.White, Color.Black, null, 30 ))
					{
					    listMenu[ nItem ].txMenuItemRight = TJAPlayer3.tテクスチャの生成( bmpItem );
					    // ctItem.t2D描画( CDTXMania.app.Device, ( x + 0x12 ) * Scale.X, ( y + 12 ) * Scale.Y - 20 );
					    // CDTXMania.tテクスチャの解放( ref ctItem );
					}
				}
				//CDTXMania.stageコンフィグ.actFont.t文字列描画( x + 0x12, y + 12, this.list項目リスト[ nItem ].str項目名 );
				//-----------------
				#endregion
				#region [ 現在の行の項目の要素を描画。]
				//-----------------
				string strParam = null;
				bool b強調 = false;
				switch ( this.list項目リスト[ nItem ].e種別 )
				{
					case CItemBase.E種別.ONorOFFトグル:
						#region [ *** ]
						//-----------------
						//CDTXMania.stageコンフィグ.actFont.t文字列描画( x + 210, y + 12, ( (CItemToggle) this.list項目リスト[ nItem ] ).bON ? "ON" : "OFF" );
						strParam = ( (CItemToggle) this.list項目リスト[ nItem ] ).bON ? "ON" : "OFF";
						break;
					//-----------------
						#endregion

					case CItemBase.E種別.ONorOFFor不定スリーステート:
						#region [ *** ]
						//-----------------
						switch ( ( (CItemThreeState) this.list項目リスト[ nItem ] ).e現在の状態 )
						{
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
						//CDTXMania.stageコンフィグ.actFont.t文字列描画( x + 210, y + 12, "ON" );
						break;
					//-----------------
						#endregion

					case CItemBase.E種別.整数:		// #24789 2011.4.8 yyagi: add PlaySpeed supports (copied them from OPTION)
						#region [ *** ]
						//-----------------
						if ( this.list項目リスト[ nItem ] == this.iCommonPlaySpeed )
						{
							double d = ( (double) ( (CItemInteger) this.list項目リスト[ nItem ] ).n現在の値 ) / 20.0;
							//CDTXMania.stageコンフィグ.actFont.t文字列描画( x + 210, y + 12, d.ToString( "0.000" ), ( n行番号 == 0 ) && this.b要素値にフォーカス中 );
							strParam = d.ToString( "0.000" );
						}
						/*else if ( this.list項目リスト[ nItem ] == this.iDrumsScrollSpeed)
						{
							float f = ( ( (CItemInteger) this.list項目リスト[ nItem ] ).n現在の値 + 1 ) / 10f;
							//CDTXMania.stageコンフィグ.actFont.t文字列描画( x + 210, y + 12, f.ToString( "x0.0" ), ( n行番号 == 0 ) && this.b要素値にフォーカス中 );
							strParam = f.ToString( "x0.0" );
						}*/
						else
						{
							//CDTXMania.stageコンフィグ.actFont.t文字列描画( x + 210, y + 12, ( (CItemInteger) this.list項目リスト[ nItem ] ).n現在の値.ToString(), ( n行番号 == 0 ) && this.b要素値にフォーカス中 );
							strParam = ( (CItemInteger) this.list項目リスト[ nItem ] ).n現在の値.ToString();
						}
						b強調 = centerFlag && this.b要素値にフォーカス中;
						break;
					//-----------------
						#endregion

					case CItemBase.E種別.リスト:	// #28195 2012.5.2 yyagi: add Skin supports
						#region [ *** ]
						//-----------------
						{
							CItemList list = (CItemList) this.list項目リスト[ nItem ];
							//CDTXMania.stageコンフィグ.actFont.t文字列描画( x + 210, y + 12, list.list項目値[ list.n現在選択されている項目番号 ] );
							strParam = list.list項目値[ list.n現在選択されている項目番号 ];

							#region [ 必要な場合に、Skinのサンプルを生成・描画する。#28195 2012.5.2 yyagi ]
							if ( this.list項目リスト[ this.n現在の選択項目 ] == this.iSystemSkinSubfolder )
							{
								tGenerateSkinSample();		// 最初にSkinの選択肢にきたとき(Enterを押す前)に限り、サンプル生成が発生する。
								if ( txSkinSample1 != null )
								{
									txSkinSample1.t2D描画( TJAPlayer3.Skin.Config_SkinSample1[0], TJAPlayer3.Skin.Config_SkinSample1[1]);
								}
							}
							#endregion
							break;
						}
					//-----------------
						#endregion
				}
				if ( b強調 )
				{
				    using (var bmpStr = prvFont.DrawText(strParam, 
						Color.Black, 
						Color.White, 
						null, 
						TJAPlayer3.Skin.Config_Selected_Menu_Text_Grad_Color_1, 
						TJAPlayer3.Skin.Config_Selected_Menu_Text_Grad_Color_2,
						30))
				    {
				        using (var txStr = TJAPlayer3.tテクスチャの生成( bmpStr, false ))
				        {
				            txStr.t2D描画( x + TJAPlayer3.Skin.Config_ItemBox_ItemValue_Font_Offset[0], y + TJAPlayer3.Skin.Config_ItemBox_ItemValue_Font_Offset[1]);
				        }
				    }
				}
				else
				{
					int nIndex = this.list項目リスト[ nItem ].GetIndex();
					if ( listMenu[ nItem ].nParam != nIndex || listMenu[ nItem ].txParam == null )
					{
						stMenuItemRight stm = listMenu[ nItem ];
						stm.nParam = nIndex;
						object o = this.list項目リスト[ nItem ].obj現在値();
						stm.strParam = ( o == null ) ? "" : o.ToString();

						using (var bmpStr = prvFont.DrawText( strParam, Color.White, Color.Black, null, 30 ))
						{
						    stm.txParam = TJAPlayer3.tテクスチャの生成( bmpStr, false );
						}

						listMenu[ nItem ] = stm;
					}
					listMenu[ nItem ].txParam.t2D描画( x + TJAPlayer3.Skin.Config_ItemBox_ItemValue_Font_Offset[0], y + TJAPlayer3.Skin.Config_ItemBox_ItemValue_Font_Offset[1]);
				}
				//-----------------
				#endregion
				
				nItem = this.t次の項目( nItem );
			}
			//-----------------
			#endregion
			
			#region [ 項目リストにフォーカスがあって、かつスクロールが停止しているなら、パネルの上下に▲印を描画する。]
			//-----------------
			if( this.b項目リスト側にフォーカスがある && ( this.n目標のスクロールカウンタ == 0 ) )
			{
				int x_upper;
				int x_lower;
				int y_upper;
				int y_lower;
			
				// 位置決定。

				if( this.b要素値にフォーカス中 )
				{
					x_upper = TJAPlayer3.Skin.Config_Arrow_Focus_X[0];  // 要素値の上下あたり。
					x_lower = TJAPlayer3.Skin.Config_Arrow_Focus_X[1];  // 要素値の上下あたり。
					y_upper = TJAPlayer3.Skin.Config_Arrow_Focus_Y[0] - this.ct三角矢印アニメ.CurrentValue;
					y_lower = TJAPlayer3.Skin.Config_Arrow_Focus_Y[1] + this.ct三角矢印アニメ.CurrentValue;
				}
				else
				{
					x_upper = TJAPlayer3.Skin.Config_Arrow_X[0];  // 要素値の上下あたり。
					x_lower = TJAPlayer3.Skin.Config_Arrow_X[1];  // 要素値の上下あたり。
					y_upper = TJAPlayer3.Skin.Config_Arrow_Y[0] - this.ct三角矢印アニメ.CurrentValue;
					y_lower = TJAPlayer3.Skin.Config_Arrow_Y[1] + this.ct三角矢印アニメ.CurrentValue;
				}

				// 描画。
				
				if( TJAPlayer3.Tx.Config_Arrow != null )
				{
                    TJAPlayer3.Tx.Config_Arrow.t2D描画( x_upper, y_upper, new Rectangle( 0, 0, TJAPlayer3.Tx.Config_Arrow.sz画像サイズ.Width, TJAPlayer3.Tx.Config_Arrow.sz画像サイズ.Height / 2) );
                    TJAPlayer3.Tx.Config_Arrow.t2D描画( x_lower, y_lower, new Rectangle( 0, TJAPlayer3.Tx.Config_Arrow.sz画像サイズ.Height / 2, TJAPlayer3.Tx.Config_Arrow.sz画像サイズ.Width, TJAPlayer3.Tx.Config_Arrow.sz画像サイズ.Height / 2) );
				}
			}
			//-----------------
			#endregion
			return 0;
		}
	

		// その他

		#region [ private ]
		//-----------------
		private enum Eメニュー種別
		{
			System,
			Drums,
			KeyAssignSystem,		// #24609 2011.4.12 yyagi: 画面キャプチャキーのアサイン
			KeyAssignDrums,
			Unknown

		}

		private bool b項目リスト側にフォーカスがある;
		private bool b要素値にフォーカス中;
		private CCounter ct三角矢印アニメ;
		private Eメニュー種別 eメニュー種別;
		#region [ キーコンフィグ ]
		private CItemBase iKeyAssignSystemCapture;			// #24609
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
		private CItemBase iKeyAssignSystemReturnToMenu;		// #24609
		private CItemBase iKeyAssignDrumsReturnToMenu;

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
		private CItemToggle iSystemSaveScore;
		private CItemToggle iSystemStageFailed;
		private CItemToggle iSystemVSyncWait;
		private CItemToggle iSystemAutoResultCapture;		// #25399 2011.6.9 yyagi
        private CItemToggle SendDiscordPlayingInformation;
        private CItemToggle iSystemBufferedInput;
		private CItemInteger iSystemRisky;					// #23559 2011.7.27 yyagi
		private CItemList iSystemSoundType;                 // #24820 2013.1.3 yyagi

		private CItemList iSystemLanguage;
		private CItemToggle iDanTowerHide;
		
		private CItemInteger iSystemBassBufferSizeMs;		// #24820 2013.1.15 yyagi
		private CItemInteger iSystemWASAPIBufferSizeMs;		// #24820 2013.1.15 yyagi
//		private CItemInteger iSystemASIOBufferSizeMs;		// #24820 2013.1.3 yyagi
		private CItemList	iSystemASIODevice;				// #24820 2013.1.17 yyagi

		private int iSystemSoundType_initial;
		private int iSystemBassBufferSizeMs_initial;
		private int iSystemWASAPIBufferSizeMs_initial;
//		private int iSystemASIOBufferSizeMs_initial;
		private int iSystemASIODevice_initial;
		private CItemToggle iSystemSoundTimerType;			// #33689 2014.6.17 yyagi
		private int iSystemSoundTimerType_initial;			// #33689 2014.6.17 yyagi

		private CItemToggle iSystemTimeStretch;				// #23664 2013.2.24 yyagi

		private List<CItemBase> list項目リスト;
		private long nスクロール用タイマ値;
		private int n現在のスクロールカウンタ;
		private int n目標のスクロールカウンタ;

		/*
        private Point[] ptパネルの基本座標 = new Point[] { 
			new Point(0x25a, 4), new Point(0x25a, 0x4f), new Point(0x25a, 0x9a), new Point(0x25a, 0xe5), 
			new Point(0x228, 0x130), 
			new Point(0x25a, 0x17b), new Point(0x25a, 0x1c6), new Point(0x25a, 0x211), new Point(0x25a, 0x25c), new Point(0x25a, 0x2a7) };
		*/

		//private CTexture txその他項目行パネル;
		//private CTexture tx三角矢印;
		//private CTexture tx通常項目行パネル;

		private CCachedFontRenderer prvFont;
		//private List<string> list項目リスト_str最終描画名;
		private struct stMenuItemRight
		{
			//	public string strMenuItem;
			public CTexture txMenuItemRight;
			public int nParam;
			public string strParam;
			public CTexture txParam;
		}
		private stMenuItemRight[] listMenu;

		private CTexture txSkinSample1;				// #28195 2012.5.2 yyagi
		private string[] skinSubFolders;			//
		private string[] skinNames;					//
		private string skinSubFolder_org;			//
		private int nSkinSampleIndex;				//
		private int nSkinIndex;						//

		private CItemBase iDrumsGoToKeyAssign;
		private CItemBase iSystemGoToKeyAssign;		// #24609
		private CItemInteger iCommonPlaySpeed;

		private CItemInteger iLayoutType;

		private CItemBase iDrumsReturnToMenu;
		private CItemInteger iDrumsScrollSpeed;
		private CItemToggle iDrumsTight;
        private CItemToggle iTaikoAutoPlay;
        private CItemToggle iTaikoAutoPlay2P;
        private CItemToggle iTaikoAutoRoll;

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

		private CItemInteger iInputAdjustTimeMs;
		private CItemInteger iGlobalOffsetMs;

		private CItemList iSystemSkinSubfolder;				// #28195 2012.5.2 yyagi
		private CItemBase iSystemReloadDTX;					// #32081 2013.10.21 yyagi
		//private CItemInteger iSystemMasterVolume;			// #33700 2014.4.26 yyagi

		private int t前の項目( int nItem )
		{
			if( --nItem < 0 )
			{
				nItem = this.list項目リスト.Count - 1;
			}
			return nItem;
		}
		private int t次の項目( int nItem )
		{
			if( ++nItem >= this.list項目リスト.Count )
			{
				nItem = 0;
			}
			return nItem;
		}
		private void tConfigIniへ記録する()
		{
			switch( this.eメニュー種別 )
			{
				case Eメニュー種別.System:
					this.tConfigIniへ記録する_System();
					return;

				case Eメニュー種別.Drums:
					this.tConfigIniへ記録する_Drums();
					return;
			}
		}
		private void tConfigIniへ記録する_System()
		{
            //CDTXMania.ConfigIni.eDark = (Eダークモード) this.iCommonDark.n現在選択されている項目番号;
			TJAPlayer3.ConfigIni.n演奏速度 = this.iCommonPlaySpeed.n現在の値;

			TJAPlayer3.ConfigIni.nGraphicsDeviceType = this.iSystemGraphicsType.n現在選択されている項目番号;
			TJAPlayer3.ConfigIni.b全画面モード = this.iSystemFullscreen.bON;
			// TJAPlayer3.ConfigIni.bSTAGEFAILED有効 = this.iSystemStageFailed.bON;
			TJAPlayer3.ConfigIni.bランダムセレクトで子BOXを検索対象とする = this.iSystemRandomFromSubBox.bON;

			//CDTXMania.ConfigIni.bWave再生位置自動調整機能有効 = this.iSystemAdjustWaves.bON;
			TJAPlayer3.ConfigIni.b垂直帰線待ちを行う = this.iSystemVSyncWait.bON;
			TJAPlayer3.ConfigIni.bBufferedInputs = this.iSystemBufferedInput.bON;
			TJAPlayer3.ConfigIni.bAVI有効 = this.iSystemAVI.bON;
			TJAPlayer3.ConfigIni.bBGA有効 = this.iSystemBGA.bON;
//			CDTXMania.ConfigIni.bGraph有効 = this.iSystemGraph.bON;#24074 2011.01.23 comment-out ikanick オプション(Drums)へ移行
			TJAPlayer3.ConfigIni.n曲が選択されてからプレビュー音が鳴るまでのウェイトms = this.iSystemPreviewSoundWait.n現在の値;
			TJAPlayer3.ConfigIni.n曲が選択されてからプレビュー画像が表示開始されるまでのウェイトms = this.iSystemPreviewImageWait.n現在の値;
			TJAPlayer3.ConfigIni.b演奏情報を表示する = this.iSystemDebugInfo.bON;
			TJAPlayer3.ConfigIni.n背景の透過度 = this.iSystemBGAlpha.n現在の値;
			TJAPlayer3.ConfigIni.bBGM音を発声する = this.iSystemBGMSound.bON;
			//CDTXMania.ConfigIni.b歓声を発声する = this.iSystemAudienceSound.bON;
			//CDTXMania.ConfigIni.eダメージレベル = (Eダメージレベル) this.iSystemDamageLevel.n現在選択されている項目番号;
			TJAPlayer3.ConfigIni.bScoreIniを出力する = this.iSystemSaveScore.bON;

			TJAPlayer3.ConfigIni.bDanTowerHide = this.iDanTowerHide.bON;

		    // TJAPlayer3.ConfigIni.ApplyLoudnessMetadata = this.iSystemApplyLoudnessMetadata.bON;
		    // TJAPlayer3.ConfigIni.TargetLoudness = this.iSystemTargetLoudness.n現在の値 / 10.0;
		    TJAPlayer3.ConfigIni.ApplySongVol = this.iSystemApplySongVol.bON;
		    TJAPlayer3.ConfigIni.SoundEffectLevel = this.iSystemSoundEffectLevel.n現在の値;
		    TJAPlayer3.ConfigIni.VoiceLevel = this.iSystemVoiceLevel.n現在の値;
		    TJAPlayer3.ConfigIni.SongPreviewLevel = this.iSystemSongPreviewLevel.n現在の値;
		    TJAPlayer3.ConfigIni.SongPlaybackLevel = this.iSystemSongPlaybackLevel.n現在の値;
		    TJAPlayer3.ConfigIni.KeyboardSoundLevelIncrement = this.iSystemKeyboardSoundLevelIncrement.n現在の値;
            TJAPlayer3.ConfigIni.MusicPreTimeMs = this.MusicPreTimeMs.n現在の値;

			TJAPlayer3.ConfigIni.bログ出力 = this.iLogOutputLog.bON;
			//CDTXMania.ConfigIni.bストイックモード = this.iSystemStoicMode.bON;

			//CDTXMania.ConfigIni.nShowLagType = this.iSystemShowLag.n現在選択されている項目番号;				// #25370 2011.6.3 yyagi
			TJAPlayer3.ConfigIni.bIsAutoResultCapture = this.iSystemAutoResultCapture.bON;					// #25399 2011.6.9 yyagi
            TJAPlayer3.ConfigIni.SendDiscordPlayingInformation = this.SendDiscordPlayingInformation.bON;

			TJAPlayer3.ConfigIni.nRisky = this.iSystemRisky.n現在の値;										// #23559 2011.7.27 yyagi

			TJAPlayer3.ConfigIni.strSystemSkinSubfolderFullName = skinSubFolders[ nSkinIndex ];				// #28195 2012.5.2 yyagi
			TJAPlayer3.Skin.SetCurrentSkinSubfolderFullName( TJAPlayer3.ConfigIni.strSystemSkinSubfolderFullName, true );

			TJAPlayer3.ConfigIni.nSoundDeviceType = this.iSystemSoundType.n現在選択されている項目番号;		// #24820 2013.1.3 yyagi
			TJAPlayer3.ConfigIni.nBassBufferSizeMs = this.iSystemBassBufferSizeMs.n現在の値;				// #24820 2013.1.15 yyagi
			TJAPlayer3.ConfigIni.nWASAPIBufferSizeMs = this.iSystemWASAPIBufferSizeMs.n現在の値;				// #24820 2013.1.15 yyagi
//			CDTXMania.ConfigIni.nASIOBufferSizeMs = this.iSystemASIOBufferSizeMs.n現在の値;					// #24820 2013.1.3 yyagi
			TJAPlayer3.ConfigIni.nASIODevice = this.iSystemASIODevice.n現在選択されている項目番号;			// #24820 2013.1.17 yyagi
			TJAPlayer3.ConfigIni.bUseOSTimer = this.iSystemSoundTimerType.bON;								// #33689 2014.6.17 yyagi

			TJAPlayer3.ConfigIni.bTimeStretch = this.iSystemTimeStretch.bON;									// #23664 2013.2.24 yyagi


			TJAPlayer3.ConfigIni.sLang = CLangManager.intToLang(this.iSystemLanguage.n現在選択されている項目番号);
			CLangManager.langAttach(TJAPlayer3.ConfigIni.sLang);


			//Trace.TraceInformation( "saved" );
			//Trace.TraceInformation( "Skin現在Current : " + CDTXMania.Skin.GetCurrentSkinSubfolderFullName(true) );
			//Trace.TraceInformation( "Skin現在System  : " + CSkin.strSystemSkinSubfolderFullName );
			//Trace.TraceInformation( "Skin現在BoxDef  : " + CSkin.strBoxDefSkinSubfolderFullName );
			//CDTXMania.ConfigIni.nMasterVolume = this.iSystemMasterVolume.n現在の値;							// #33700 2014.4.26 yyagi
			//CDTXMania.ConfigIni.e判定表示優先度 = (E判定表示優先度) this.iSystemJudgeDispPriority.n現在選択されている項目番号;
			TJAPlayer3.ConfigIni.ShowChara = this.ShowChara.bON;
            TJAPlayer3.ConfigIni.ShowDancer = this.ShowDancer.bON;
            TJAPlayer3.ConfigIni.ShowRunner = this.ShowRunner.bON;
            TJAPlayer3.ConfigIni.ShowMob = this.ShowMob.bON;
            TJAPlayer3.ConfigIni.ShowFooter = this.ShowFooter.bON;
            TJAPlayer3.ConfigIni.ShowPuchiChara = this.ShowPuchiChara.bON;

			TJAPlayer3.ConfigIni.nPlayerCount = this.iTaikoPlayerCount.n現在の値;

			TJAPlayer3.ConfigIni.nLayoutType = this.iLayoutType.n現在の値;
			TJAPlayer3.ConfigIni.FastRender = this.FastRender.bON;
            TJAPlayer3.ConfigIni.ASyncTextureLoad = this.ASyncTextureLoad.bON;
            TJAPlayer3.ConfigIni.SimpleMode = this.SimpleMode.bON;
        }
		private void tConfigIniへ記録する_Drums()
		{
            //TJAPlayer3.ConfigIni.b太鼓パートAutoPlay = this.iTaikoAutoPlay.bON;
            //TJAPlayer3.ConfigIni.b太鼓パートAutoPlay2P = this.iTaikoAutoPlay2P.bON;
			//TJAPlayer3.ConfigIni.bAuto先生の連打 = this.iTaikoAutoRoll.bON;
			TJAPlayer3.ConfigIni.nRollsPerSec = this.iRollsPerSec.n現在の値;

			TJAPlayer3.ConfigIni.nAILevel = this.iAILevel.n現在の値;
			for (int i = 0; i < 2; i++)
				TJAPlayer3.NamePlate.tNamePlateRefreshTitles(i);

			//TJAPlayer3.ConfigIni.nScrollSpeed[TJAPlayer3.SaveFile] = this.iDrumsScrollSpeed.n現在の値;
            
			TJAPlayer3.ConfigIni.bTight = this.iDrumsTight.bON;

		    //TJAPlayer3.ConfigIni.nInputAdjustTimeMs = this.iInputAdjustTimeMs.n現在の値;

			TJAPlayer3.ConfigIni.nGlobalOffsetMs = this.iGlobalOffsetMs.n現在の値;

			TJAPlayer3.ConfigIni.n表示可能な最小コンボ数.Drums = this.iSystemMinComboDrums.n現在の値;
			TJAPlayer3.ConfigIni.nRisky = this.iSystemRisky.n現在の値;						// #23559 2911.7.27 yyagi
			//CDTXMania.ConfigIni.e判定表示優先度.Drums = (E判定表示優先度) this.iDrumsJudgeDispPriority.n現在選択されている項目番号;

            TJAPlayer3.ConfigIni.bBranchGuide = this.iTaikoBranchGuide.bON;
            TJAPlayer3.ConfigIni.nDefaultCourse = this.iTaikoDefaultCourse.n現在選択されている項目番号;
            TJAPlayer3.ConfigIni.nScoreMode = this.iTaikoScoreMode.n現在選択されている項目番号;
            TJAPlayer3.ConfigIni.ShinuchiMode = this.ShinuchiMode.bON;
            TJAPlayer3.ConfigIni.nBranchAnime = this.iTaikoBranchAnime.n現在選択されている項目番号;
            //CDTXMania.ConfigIni.bHispeedRandom = this.iTaikoHispeedRandom.bON;
            TJAPlayer3.ConfigIni.bNoInfo = this.iTaikoNoInfo.bON;

            //TJAPlayer3.ConfigIni.eRandom.Taiko = (Eランダムモード)this.iTaikoRandom.n現在選択されている項目番号;
            //TJAPlayer3.ConfigIni.eSTEALTH = (Eステルスモード)this.iTaikoStealth.n現在選択されている項目番号;

            TJAPlayer3.ConfigIni.eGameMode = (EGame)this.iTaikoGameMode.n現在選択されている項目番号;
            //TJAPlayer3.ConfigIni.bJust = this.iTaikoJust.bON;
            TJAPlayer3.ConfigIni.bJudgeCountDisplay = this.iTaikoJudgeCountDisp.bON;
			TJAPlayer3.ConfigIni.ShowExExtraAnime = this.iShowExExtraAnime.bON;
			TJAPlayer3.ConfigIni.bJudgeBigNotes = this.iTaikoBigNotesJudge.bON;
			TJAPlayer3.ConfigIni.bForceNormalGauge = this.iTaikoForceNormalGauge.bON;

		}
		//-----------------
		#endregion
	}
}
