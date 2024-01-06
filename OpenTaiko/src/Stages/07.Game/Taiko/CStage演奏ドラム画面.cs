using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Diagnostics;
using System.Threading;
using FDK;
using DiscordRPC;

using Rectangle = System.Drawing.Rectangle;
using Point = System.Drawing.Point;
using Color = System.Drawing.Color;
namespace TJAPlayer3
{
    internal class CStage演奏ドラム画面 : CStage演奏画面共通
    {
        // コンストラクタ

		public CStage演奏ドラム画面()
		{
			base.eステージID = CStage.Eステージ.演奏;
			base.eフェーズID = CStage.Eフェーズ.共通_通常状態;
			base.IsDeActivated = true;
			base.ChildActivities.Add( this.actPad = new CAct演奏Drumsパッド() );
			base.ChildActivities.Add( this.actCombo = new CAct演奏DrumsコンボDGB() );
			base.ChildActivities.Add( this.actDANGER = new CAct演奏DrumsDanger() );
			base.ChildActivities.Add( this.actChipFireD = new CAct演奏DrumsチップファイアD() );
			base.ChildActivities.Add( this.Rainbow = new Rainbow() );
            base.ChildActivities.Add( this.actGauge = new CAct演奏Drumsゲージ() );
            base.ChildActivities.Add( this.actGraph = new CAct演奏Drumsグラフ() ); // #24074 2011.01.23 add ikanick
			base.ChildActivities.Add( this.actJudgeString = new CAct演奏Drums判定文字列() );
			base.ChildActivities.Add( this.actTaikoLaneFlash = new TaikoLaneFlash() );
			base.ChildActivities.Add( this.actLaneFlushGB = new CAct演奏DrumsレーンフラッシュGB() );
			base.ChildActivities.Add( this.actScore = new CAct演奏Drumsスコア() );
			base.ChildActivities.Add( this.actStatusPanels = new CAct演奏Drumsステータスパネル() );
			base.ChildActivities.Add( this.act譜面スクロール速度 = new CAct演奏スクロール速度() );
			base.ChildActivities.Add( this.actAVI = new CAct演奏AVI() );
			base.ChildActivities.Add( this.actPanel = new CAct演奏パネル文字列() );
			base.ChildActivities.Add( this.actStageFailed = new CAct演奏ステージ失敗() );
			base.ChildActivities.Add( this.actPlayInfo = new CAct演奏演奏情報() );
			//base.list子Activities.Add( this.actFI = new CActFIFOBlack() );
            base.ChildActivities.Add( this.actFI = new CActFIFOStart() );
			base.ChildActivities.Add( this.actFO = new CActFIFOBlack() );
			base.ChildActivities.Add( this.actFOClear = new CActFIFOResult() );
            base.ChildActivities.Add( this.actLane = new CAct演奏Drumsレーン() );
            base.ChildActivities.Add( this.actEnd = new CAct演奏Drums演奏終了演出() );
            base.ChildActivities.Add( this.actDancer = new CAct演奏DrumsDancer() );
            base.ChildActivities.Add( this.actMtaiko = new CAct演奏DrumsMtaiko() );
            base.ChildActivities.Add( this.actLaneTaiko = new CAct演奏Drumsレーン太鼓() );
            base.ChildActivities.Add( this.actRoll = new CAct演奏Drums連打() );
            base.ChildActivities.Add( this.actBalloon = new CAct演奏Drums風船() );
            base.ChildActivities.Add( this.actChara = new CAct演奏Drumsキャラクター() );
            base.ChildActivities.Add( this.actGame = new CAct演奏Drumsゲームモード() );
            base.ChildActivities.Add( this.actBackground = new CAct演奏Drums背景() );
            base.ChildActivities.Add( this.actRollChara = new CAct演奏Drums連打キャラ() );
            base.ChildActivities.Add( this.actComboBalloon = new CAct演奏Drumsコンボ吹き出し() );
            base.ChildActivities.Add( this.actComboVoice = new CAct演奏Combo音声() );
            base.ChildActivities.Add( this.actPauseMenu = new CAct演奏PauseMenu() );
            base.ChildActivities.Add(this.actChipEffects = new CAct演奏Drumsチップエフェクト());
            base.ChildActivities.Add(this.actFooter = new CAct演奏DrumsFooter());
            base.ChildActivities.Add(this.actRunner = new CAct演奏DrumsRunner());
            base.ChildActivities.Add(this.actMob = new CAct演奏DrumsMob());
            base.ChildActivities.Add(this.GoGoSplash = new GoGoSplash());
            base.ChildActivities.Add(this.FlyingNotes = new FlyingNotes());
            base.ChildActivities.Add(this.FireWorks = new FireWorks());
            base.ChildActivities.Add(this.PuchiChara = new PuchiChara());
            base.ChildActivities.Add(this.ScoreRank = new CAct演奏Drumsスコアランク());

            base.ChildActivities.Add(this.actDan = new Dan_Cert());
            base.ChildActivities.Add(this.actTokkun = new CAct演奏Drums特訓モード());
            base.ChildActivities.Add(this.actAIBattle = new AIBattle());
            #region[ 文字初期化 ]
            ST文字位置[] st文字位置Array = new ST文字位置[ 12 ];
			ST文字位置 st文字位置 = new ST文字位置();
			st文字位置.ch = '0';
			st文字位置.pt = new Point( 0, 0 );
			st文字位置Array[ 0 ] = st文字位置;
			ST文字位置 st文字位置2 = new ST文字位置();
			st文字位置2.ch = '1';
			st文字位置2.pt = new Point( 32, 0 );
			st文字位置Array[ 1 ] = st文字位置2;
			ST文字位置 st文字位置3 = new ST文字位置();
			st文字位置3.ch = '2';
			st文字位置3.pt = new Point( 64, 0 );
			st文字位置Array[ 2 ] = st文字位置3;
			ST文字位置 st文字位置4 = new ST文字位置();
			st文字位置4.ch = '3';
			st文字位置4.pt = new Point( 96, 0 );
			st文字位置Array[ 3 ] = st文字位置4;
			ST文字位置 st文字位置5 = new ST文字位置();
			st文字位置5.ch = '4';
			st文字位置5.pt = new Point( 128, 0 );
			st文字位置Array[ 4 ] = st文字位置5;
			ST文字位置 st文字位置6 = new ST文字位置();
			st文字位置6.ch = '5';
			st文字位置6.pt = new Point( 160, 0 );
			st文字位置Array[ 5 ] = st文字位置6;
			ST文字位置 st文字位置7 = new ST文字位置();
			st文字位置7.ch = '6';
			st文字位置7.pt = new Point( 192, 0 );
			st文字位置Array[ 6 ] = st文字位置7;
			ST文字位置 st文字位置8 = new ST文字位置();
			st文字位置8.ch = '7';
			st文字位置8.pt = new Point( 224, 0 );
			st文字位置Array[ 7 ] = st文字位置8;
			ST文字位置 st文字位置9 = new ST文字位置();
			st文字位置9.ch = '8';
			st文字位置9.pt = new Point( 256, 0 );
			st文字位置Array[ 8 ] = st文字位置9;
			ST文字位置 st文字位置10 = new ST文字位置();
			st文字位置10.ch = '9';
			st文字位置10.pt = new Point( 288, 0 );
			st文字位置Array[ 9 ] = st文字位置10;
			ST文字位置 st文字位置11 = new ST文字位置();
			st文字位置11.ch = '%';
			st文字位置11.pt = new Point( 320, 0 );
			st文字位置Array[ 10 ] = st文字位置11;
			ST文字位置 st文字位置12 = new ST文字位置();
			st文字位置12.ch = ' ';
			st文字位置12.pt = new Point( 0, 0 );
			st文字位置Array[ 11 ] = st文字位置12;
			this.st小文字位置 = st文字位置Array;

			st文字位置Array = new ST文字位置[ 12 ];
		    st文字位置 = new ST文字位置();
			st文字位置.ch = '0';
			st文字位置.pt = new Point( 0, 0 );
			st文字位置Array[ 0 ] = st文字位置;
			st文字位置2 = new ST文字位置();
			st文字位置2.ch = '1';
			st文字位置2.pt = new Point( 32, 0 );
			st文字位置Array[ 1 ] = st文字位置2;
			st文字位置3 = new ST文字位置();
			st文字位置3.ch = '2';
			st文字位置3.pt = new Point( 64, 0 );
			st文字位置Array[ 2 ] = st文字位置3;
			st文字位置4 = new ST文字位置();
			st文字位置4.ch = '3';
			st文字位置4.pt = new Point( 96, 0 );
			st文字位置Array[ 3 ] = st文字位置4;
			st文字位置5 = new ST文字位置();
			st文字位置5.ch = '4';
			st文字位置5.pt = new Point( 128, 0 );
			st文字位置Array[ 4 ] = st文字位置5;
			st文字位置6 = new ST文字位置();
			st文字位置6.ch = '5';
			st文字位置6.pt = new Point( 160, 0 );
			st文字位置Array[ 5 ] = st文字位置6;
			st文字位置7 = new ST文字位置();
			st文字位置7.ch = '6';
			st文字位置7.pt = new Point( 192, 0 );
			st文字位置Array[ 6 ] = st文字位置7;
			st文字位置8 = new ST文字位置();
			st文字位置8.ch = '7';
			st文字位置8.pt = new Point( 224, 0 );
			st文字位置Array[ 7 ] = st文字位置8;
			st文字位置9 = new ST文字位置();
			st文字位置9.ch = '8';
			st文字位置9.pt = new Point( 256, 0 );
			st文字位置Array[ 8 ] = st文字位置9;
			st文字位置10 = new ST文字位置();
			st文字位置10.ch = '9';
			st文字位置10.pt = new Point( 288, 0 );
			st文字位置Array[ 9 ] = st文字位置10;
			st文字位置11 = new ST文字位置();
			st文字位置11.ch = '%';
			st文字位置11.pt = new Point( 320, 0 );
			st文字位置Array[ 10 ] = st文字位置11;
			st文字位置12 = new ST文字位置();
			st文字位置12.ch = ' ';
			st文字位置12.pt = new Point( 0, 0 );
			st文字位置Array[ 11 ] = st文字位置12;
			this.st小文字位置 = st文字位置Array;
            #endregion
        }


		// メソッド

		public void t演奏結果を格納する( out CScoreIni.C演奏記録 Drums )
		{
			base.t演奏結果を格納する_ドラム( out Drums );
		}


		// CStage 実装

		public override void Activate()
		{
            LoudnessMetadataScanner.StopBackgroundScanning(joinImmediately: false);

			this.bフィルイン中 = false;
            this.n待機中の大音符の座標 = 0;
            this.actGame.t叩ききりまショー_初期化();
            base.ReSetScore(TJAPlayer3.DTX.nScoreInit[0, TJAPlayer3.stage選曲.n確定された曲の難易度[0]], TJAPlayer3.DTX.nScoreDiff[TJAPlayer3.stage選曲.n確定された曲の難易度[0]]);
            
            #region [ branch ]
            for (int i = 0; i < 5; i++)
            {
                this.n分岐した回数[i] = 0;
                this.bLEVELHOLD[i] = false;
            }
            this.nBranch条件数値A = 0;
            this.nBranch条件数値B = 0;
            #endregion
            
            if ((TJAPlayer3.DTX.listVD.TryGetValue(1, out CVideoDecoder vd2)))
            {
                ShowVideo = true;
            }
            else 
            {
                ShowVideo = false;
            }

            base.Activate();
            base.eフェーズID = CStage.Eフェーズ.共通_通常状態;//初期化すれば、リザルト変遷は止まる。

            for (int i = 0; i < 5; i++) {
                ifp[i] = false;
                isDeniedPlaying[i] = false;

                if (bIsAlreadyCleared[i])
                {
                    actBackground.ClearIn(i);
                }
            }
            
            this.nStoredHit = new int[TJAPlayer3.ConfigIni.nPlayerCount];

            // MODIFY_BEGIN #25398 2011.06.07 FROM
            if ( TJAPlayer3.bコンパクトモード )
			{
				var score = new Cスコア();
                TJAPlayer3.Songs管理.tScoreIniを読み込んで譜面情報を設定する(TJAPlayer3.strコンパクトモードファイル, score);// + ".score.ini", score );
			}
			else
			{
			}
			// MODIFY_END #25398
			dtLastQueueOperation = DateTime.MinValue;

            PuchiChara.ChangeBPM(60.0 / TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM[0]);

            //dbUnit = Math.Ceiling( dbUnit * 1000.0 );
            //dbUnit = dbUnit / 1000.0;

            //if (this.actChara.ctキャラクターアクションタイマ != null) this.actChara.ctキャラクターアクションタイマ = new CCounter();

            //this.actDancer.ct通常モーション = new CCounter( 0, this.actDancer.arモーション番号_通常.Length - 1, ( dbUnit * 4.0) / this.actDancer.arモーション番号_通常.Length, CSound管理.rc演奏用タイマ );
            //this.actDancer.ctモブ = new CCounter( 1.0, 16.0, ((60.0 / CDTXMania.stage演奏ドラム画面.actPlayInfo.dbBPM / 16.0 )), CSound管理.rc演奏用タイマ );


            this.ct手つなぎ = new CCounter( 0, 60, 20, TJAPlayer3.Timer );
            this.ShownLyric2 = 0;


			string diffToString(int diff)
			{
				string[] diffArr =
				{
					" Easy ",
					" Normal ",
					" Hard ",
					" Extreme ",
					" Extra ",
					" Tower ",
					" Dan "
				};
				string[] diffArrIcon =
				{
					"-",
					"",
					"+"
				};

				int level = TJAPlayer3.stage選曲.r確定された曲.nLevel[diff];
				CDTX.ELevelIcon levelIcon = TJAPlayer3.stage選曲.r確定された曲.nLevelIcon[diff];

				return (diffArr[Math.Min(diff, 6)] + "Lv." + level + diffArrIcon[(int)levelIcon]);
			}

            // Discord Presence の更新
			string Details = TJAPlayer3.ConfigIni.SendDiscordPlayingInformation ? TJAPlayer3.stage選曲.r確定された曲.strタイトル
				+ diffToString(TJAPlayer3.stage選曲.n確定された曲の難易度[0]) : "";

            var difficultyName = TJAPlayer3.DifficultyNumberToEnum(TJAPlayer3.stage選曲.n確定された曲の難易度[0]).ToString();

			TJAPlayer3.DiscordClient?.SetPresence(new RichPresence()
			{
				Details = Details.Substring(0, Math.Min(127, Details.Length)),
				State = "Playing" + (TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[0] == true ? " (Auto)" : ""),
				Timestamps = new Timestamps(DateTime.UtcNow, DateTime.UtcNow.AddMilliseconds(TJAPlayer3.DTX.listChip[TJAPlayer3.DTX.listChip.Count - 1].n発声時刻ms / TJAPlayer3.ConfigIni.SongPlaybackSpeed)),
				Assets = new Assets()
				{
					SmallImageKey = TJAPlayer3.ConfigIni.SendDiscordPlayingInformation ? difficultyName.ToLower() : "",
					SmallImageText = TJAPlayer3.ConfigIni.SendDiscordPlayingInformation ? String.Format("COURSE:{0} ({1})", difficultyName, TJAPlayer3.stage選曲.n確定された曲の難易度[0]) : "",
					LargeImageKey = TJAPlayer3.LargeImageKey,
					LargeImageText = TJAPlayer3.LargeImageText,
				}
			});

            

			// When performing calibration, reduce audio distraction from user input.
			// For users who play primarily by listening to the music,
			// you might think that we want them to hear drum sound effects during
			// calibration, but we do not. Humans are remarkably good at adjusting
			// the timing of their own physical movement, even without realizing it.
			// We are calibrating their input timing for the purposes of judgment.
			// We do not want them subconsciously playing early so as to line up
			// their drum sound effects with the sounds of the input calibration file.
			// Instead, we want them focused on the sounds of their keyboard, tatacon,
			// other controller, etc. and the sounds of the input calibration audio file.
			if (!TJAPlayer3.IsPerformingCalibration)
            {
                for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                {
                    int actual = TJAPlayer3.GetActualPlayer(i);

                    var hs = TJAPlayer3.Skin.hsHitSoundsInformations;

                    //this.soundRed[i] = TJAPlayer3.Sound管理.tサウンドを生成する(CSkin.Path(hs.don[actual]), ESoundGroup.SoundEffect);
                    //this.soundBlue[i] = TJAPlayer3.Sound管理.tサウンドを生成する(CSkin.Path(hs.ka[actual]), ESoundGroup.SoundEffect);
                    //this.soundAdlib[i] = TJAPlayer3.Sound管理.tサウンドを生成する(CSkin.Path(hs.adlib[actual]), ESoundGroup.SoundEffect);
                    //this.soundClap[i] = TJAPlayer3.Sound管理.tサウンドを生成する(CSkin.Path(hs.clap[actual]), ESoundGroup.SoundEffect);

                    this.soundRed[i] = TJAPlayer3.Sound管理.tCreateSound((@$"Global{Path.DirectorySeparatorChar}HitSounds{Path.DirectorySeparatorChar}" + hs.don[actual]), ESoundGroup.SoundEffect);
                    this.soundBlue[i] = TJAPlayer3.Sound管理.tCreateSound((@$"Global{Path.DirectorySeparatorChar}HitSounds{Path.DirectorySeparatorChar}" + hs.ka[actual]), ESoundGroup.SoundEffect);
                    this.soundAdlib[i] = TJAPlayer3.Sound管理.tCreateSound((@$"Global{Path.DirectorySeparatorChar}HitSounds{Path.DirectorySeparatorChar}" + hs.adlib[actual]), ESoundGroup.SoundEffect);
                    this.soundClap[i] = TJAPlayer3.Sound管理.tCreateSound((@$"Global{Path.DirectorySeparatorChar}HitSounds{Path.DirectorySeparatorChar}" + hs.clap[actual]), ESoundGroup.SoundEffect);

                    int _panning = TJAPlayer3.ConfigIni.nPanning[TJAPlayer3.ConfigIni.nPlayerCount - 1][i];
                    if (this.soundRed[i] != null) this.soundRed[i].SoundPosition = _panning;
                    if (this.soundBlue[i] != null) this.soundBlue[i].SoundPosition = _panning;
                    if (this.soundAdlib[i] != null) this.soundAdlib[i].SoundPosition = _panning;
                    if (this.soundClap[i] != null) this.soundClap[i].SoundPosition = _panning;
                }
                /*
                this.soundRed = TJAPlayer3.Sound管理.tサウンドを生成する( CSkin.Path( @"Sounds\Taiko\dong.ogg" ), ESoundGroup.SoundEffect );
			    this.soundBlue = TJAPlayer3.Sound管理.tサウンドを生成する( CSkin.Path( @"Sounds\Taiko\ka.ogg" ), ESoundGroup.SoundEffect );
			    this.soundAdlib = TJAPlayer3.Sound管理.tサウンドを生成する( CSkin.Path(@"Sounds\Taiko\Adlib.ogg"), ESoundGroup.SoundEffect );

                this.soundRed2 = TJAPlayer3.Sound管理.tサウンドを生成する(CSkin.Path(@"Sounds\Taiko\dong.ogg"), ESoundGroup.SoundEffect);
                this.soundBlue2 = TJAPlayer3.Sound管理.tサウンドを生成する(CSkin.Path(@"Sounds\Taiko\ka.ogg"), ESoundGroup.SoundEffect);
                this.soundAdlib2 = TJAPlayer3.Sound管理.tサウンドを生成する(CSkin.Path(@"Sounds\Taiko\Adlib.ogg"), ESoundGroup.SoundEffect);
                */

                /*
                if (TJAPlayer3.ConfigIni.nPlayerCount == 2)//2020.05.06 Mr-Ojii左右に出したかったから、追加。
                {
                    this.soundRed[0].n位置 = -100;
                    this.soundBlue[0].n位置 = -100;
                    this.soundAdlib[0].n位置 = -100;
                    this.soundRed[1].n位置 = 100;
                    this.soundBlue[1].n位置 = 100;
                    this.soundAdlib[1].n位置 = 100;
                }
                */
            }
        }
		public override void DeActivate()
		{
            this.ct手つなぎ = null;

            for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                if (this.soundRed[i] != null)
                    this.soundRed[i].tDispose();
                if (this.soundBlue[i] != null)
                    this.soundBlue[i].tDispose();
                if (this.soundAdlib[i] != null)
                    this.soundAdlib[i].tDispose();
                if (this.soundClap[i] != null)
                    this.soundClap[i].tDispose();
            }

			base.DeActivate();

            LoudnessMetadataScanner.StartBackgroundScanning();
		}
		public override void CreateManagedResource()
		{
				//this.t背景テクスチャの生成();
				//this.tx太鼓ノーツ = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\7_taiko_notes.png" ) );
				//this.txHand = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\7_taiko_notes_arm.png" ) );
				//this.txSenotes = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\7_senotes.png" ) );
				//this.tx小節線 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\7_bar_line.png" ) );
				//this.tx小節線_branch = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\7_bar_line_branch.png" ) );
    //            this.tx判定数小文字 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\8_Result_number_s.png" ) );
    //            this.txNamePlate = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\7_NamePlate.png" ) );
    //            if (CDTXMania.stage演奏ドラム画面.bDoublePlay)
    //                this.txNamePlate2P = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\7_NamePlate2P.png" ) );
    //            this.txPlayerNumber = CDTXMania.tテクスチャの生成(CSkin.Path(@"Graphics\7_PlayerNumber.png"));

    //            this.tx判定数表示パネル = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\7_Paramater Panel.png" ) );

			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource()
		{
            base.ReleaseManagedResource();
		}
		public override int Draw()
		{
			base.sw.Start();
			if( !base.IsDeActivated )
			{
				bool bIsFinishedPlaying = false;
                bool bIsFinishedEndAnime = false;
				bool bIsFinishedFadeout = false;
				#region [ 初めての進行描画 ]
				if ( base.IsFirstDraw )
				{
                    SoundManager.PlayTimer.Reset();
					TJAPlayer3.Timer.Reset();
					this.ctチップ模様アニメ.Drums = new CCounter( 0, 1, 500, TJAPlayer3.Timer );
					this.ctチップ模様アニメ.Guitar = new CCounter( 0, 0x17, 20, TJAPlayer3.Timer );
					this.ctチップ模様アニメ.Bass = new CCounter( 0, 0x17, 20, TJAPlayer3.Timer );
					this.ctチップ模様アニメ.Taiko = new CCounter( 0, 1, 500, TJAPlayer3.Timer );

					// this.actChipFireD.Start( Eレーン.HH );	// #31554 2013.6.12 yyagi
					// 初チップヒット時のもたつき回避。最初にactChipFireD.Start()するときにJITが掛かって？
					// ものすごく待たされる(2回目以降と比べると2,3桁tick違う)。そこで最初の画面フェードインの間に
					// 一発Start()を掛けてJITの結果を生成させておく。

					base.eフェーズID = CStage.Eフェーズ.共通_フェードイン;

                    this.actFI.tフェードイン開始();

                    if ( TJAPlayer3.DTXVmode.Enabled )			// DTXVモードなら
					{
						#region [ DTXV用の再生設定にする(全AUTOなど) ]
						tDTXV用の設定();
						#endregion
						t演奏位置の変更( TJAPlayer3.DTXVmode.nStartBar, 0 );
					}

					// TJAPlayer3.Sound管理.tDisableUpdateBufferAutomatically();
					base.IsFirstDraw = false;
				}
				#endregion
				if ( ( ( TJAPlayer3.ConfigIni.nRisky != 0 && this.actGauge.IsFailed( E楽器パート.TAIKO ) ) 
                    || this.actGame.st叩ききりまショー.ct残り時間.IsEnded 
                    || (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower && CFloorManagement.CurrentNumberOfLives <= 0)) 
                    && ( base.eフェーズID == CStage.Eフェーズ.共通_通常状態 ))
				{
					this.actStageFailed.Start();
					TJAPlayer3.DTX.t全チップの再生停止();
					base.eフェーズID = CStage.Eフェーズ.演奏_STAGE_FAILED;
				}

                bool BGA_Hidden = TJAPlayer3.ConfigIni.bAVI有効 && TJAPlayer3.DTX.listVD.Count > 0 && ShowVideo;

                // (????)
                if ( !String.IsNullOrEmpty( TJAPlayer3.DTX.strBGIMAGE_PATH ) || ( TJAPlayer3.DTX.listVD.Count == 0 ) || !ShowVideo || !TJAPlayer3.ConfigIni.bAVI有効 ) //背景動画があったら背景画像を描画しない。
                {
				    this.t進行描画_背景();
                }

                if (TJAPlayer3.ConfigIni.bAVI有効 && TJAPlayer3.DTX.listVD.Count > 0 && ShowVideo && !TJAPlayer3.ConfigIni.bTokkunMode)
                {
                    this.t進行描画_AVI();
                }
                else if (TJAPlayer3.ConfigIni.bBGA有効)
                {
                    if (TJAPlayer3.ConfigIni.bTokkunMode) actTokkun.On進行描画_背景();
                    else actBackground.Draw();
                }

                if (!BGA_Hidden && !TJAPlayer3.ConfigIni.bTokkunMode)
                {
                    actRollChara.Draw();
                }

                if (!BGA_Hidden && !bDoublePlay && TJAPlayer3.ConfigIni.ShowDancer && !TJAPlayer3.ConfigIni.bTokkunMode)
                {
                    actDancer.Draw();
                }

                if(!BGA_Hidden && !bDoublePlay && TJAPlayer3.ConfigIni.ShowFooter && !TJAPlayer3.ConfigIni.bTokkunMode)
                    this.actFooter.Draw();

                //this.t進行描画_グラフ();   // #24074 2011.01.23 add ikanick


                //this.t進行描画_DANGER();
                //this.t進行描画_判定ライン();

                if( TJAPlayer3.ConfigIni.ShowChara && TJAPlayer3.ConfigIni.nPlayerCount <= 2)
                {
                    this.actChara.Draw();
                }

                if(!BGA_Hidden && TJAPlayer3.ConfigIni.ShowMob && !TJAPlayer3.ConfigIni.bTokkunMode)
                    this.actMob.Draw();

                if ( TJAPlayer3.ConfigIni.eGameMode != EGame.OFF )
                    this.actGame.Draw();

				this.t進行描画_譜面スクロール速度();
				this.t進行描画_チップアニメ();

                this.actLaneTaiko.Draw();

                if (TJAPlayer3.ConfigIni.ShowRunner && !TJAPlayer3.ConfigIni.bAIBattleMode && TJAPlayer3.ConfigIni.nPlayerCount <= 2)
                    this.actRunner.Draw();

                //this.t進行描画_レーン();
                //this.t進行描画_レーンフラッシュD();

                if ( ( TJAPlayer3.ConfigIni.eClipDispType == EClipDispType.ウィンドウのみ || TJAPlayer3.ConfigIni.eClipDispType == EClipDispType.両方 ) && TJAPlayer3.ConfigIni.nPlayerCount == 1 )
                    this.actAVI.t窓表示();

				if( !TJAPlayer3.ConfigIni.bNoInfo && !TJAPlayer3.ConfigIni.bTokkunMode)
                    this.t進行描画_ゲージ();

                this.actLaneTaiko.ゴーゴー炎();

                // bIsFinishedPlaying was dependent on 2P in this case

                this.actDan.Draw();

                for ( int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++ )
                {
                    // bIsFinishedPlaying = this.t進行描画_チップ(E楽器パート.DRUMS, i);
                    bool btmp = this.t進行描画_チップ(E楽器パート.DRUMS, i);
                    if (btmp == true)
                        ifp[i] = true;

#if DEBUG
                    if (TJAPlayer3.Input管理.Keyboard.KeyPressed((int)SlimDXKeys.Key.D0))
                    {
                        ifp[i] = true;
                    }
#endif

                    this.t進行描画_チップ_連打( E楽器パート.DRUMS, i );
                }

                this.actMtaiko.Draw();

                if (TJAPlayer3.ConfigIni.bAIBattleMode)
                {
                    this.actAIBattle.Draw();
                }

                this.GoGoSplash.Draw();
                this.t進行描画_リアルタイム判定数表示();
                if (TJAPlayer3.ConfigIni.bTokkunMode)
                    this.actTokkun.On進行描画_小節_速度();

                if ( !TJAPlayer3.ConfigIni.bNoInfo )
			        this.t進行描画_コンボ();
                if( !TJAPlayer3.ConfigIni.bNoInfo && !TJAPlayer3.ConfigIni.bTokkunMode)
				    this.t進行描画_スコア();

                if (TJAPlayer3.ConfigIni.ShowChara && TJAPlayer3.ConfigIni.nPlayerCount > 2)
                {
                    this.actChara.Draw();
                }

                this.Rainbow.Draw();
                this.FireWorks.Draw();
                this.actChipEffects.Draw();
                this.FlyingNotes.Draw();
                this.t進行描画_チップファイアD();

                if (!TJAPlayer3.ConfigIni.bNoInfo)
                    this.t進行描画_パネル文字列();

                this.actComboBalloon.Draw();

                for ( int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++ )
                {
                    this.actRoll.On進行描画( this.n現在の連打数[ i ], i );
                }


                if( !TJAPlayer3.ConfigIni.bNoInfo )
                    this.t進行描画_判定文字列1_通常位置指定の場合();

                this.t進行描画_演奏情報();

                if (TJAPlayer3.DTX.listLyric2.Count > ShownLyric2 && TJAPlayer3.DTX.listLyric2[ShownLyric2].Time < (long)(SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed))
                {
                    this.actPanel.t歌詞テクスチャを生成する(TJAPlayer3.DTX.listLyric2[ShownLyric2++].TextTex);
                }

                this.actPanel.t歌詞テクスチャを描画する();

                actChara.OnDraw_Balloon();

                // Floor voice
                if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
                    this.actComboVoice.tPlayFloorSound();

                this.t全体制御メソッド();
                
                this.actPauseMenu.t進行描画();
                //this.actEnd.On進行描画();
				this.t進行描画_STAGEFAILED();

                this.ScoreRank.Draw();

                if (TJAPlayer3.ConfigIni.bTokkunMode)
                {
                    actTokkun.Draw();
                }


                bIsFinishedEndAnime = this.actEnd.Draw() == 1 ? true : false;
				bIsFinishedFadeout = this.t進行描画_フェードイン_アウト();

                bIsFinishedPlaying = true;
                for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                {
                    if (!ifp[i]) bIsFinishedPlaying = false;
                }

                //演奏終了→演出表示→フェードアウト
                if ( bIsFinishedPlaying && base.eフェーズID == CStage.Eフェーズ.共通_通常状態 )
                {
                    if (TJAPlayer3.ConfigIni.bTokkunMode)
                    {
                        bIsFinishedPlaying = false;
                        TJAPlayer3.Skin.sound特訓停止音.t再生する();
                        actTokkun.t演奏を停止する();

                        actTokkun.t譜面の表示位置を合わせる(true);
                    }
                    else
                    {
                        for(int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                        {
                            base.eフェーズID = CStage.Eフェーズ.演奏_演奏終了演出;

                            this.actEnd.Start();

                            int Character = this.actChara.iCurrentCharacter[i];

                            if (HGaugeMethods.UNSAFE_IsRainbow(i))
                            {
                                if (TJAPlayer3.Skin.Characters_10Combo_Maxed_Ptn[Character] != 0)
                                {
                                    if (HGaugeMethods.UNSAFE_IsRainbow(i))
                                    {
                                        double dbUnit = (((60.0 / (TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM[i]))));
                                        this.actChara.ChangeAnime(i, CAct演奏Drumsキャラクター.Anime.Combo10_Max, true);
                                    }
                                }
                            }
                            else if (HGaugeMethods.UNSAFE_FastNormaCheck(i))
                            {
                                if (TJAPlayer3.Skin.Characters_Become_Cleared_Ptn[Character] != 0)
                                {
                                    this.actChara.ChangeAnime(i, CAct演奏Drumsキャラクター.Anime.Cleared, true);;
                                }
                            }
                            else
                            {
                                if (TJAPlayer3.Skin.Characters_ClearOut_Ptn[Character] != 0)
                                {
                                    this.actChara.ChangeAnime(i, CAct演奏Drumsキャラクター.Anime.ClearOut, true);
                                }
                            }
                        }
                    }
                }
                else if( bIsFinishedEndAnime && base.eフェーズID == Eフェーズ.演奏_演奏終了演出 )
                {
                    this.eフェードアウト完了時の戻り値 = E演奏画面の戻り値.ステージクリア;
                    base.eフェーズID = CStage.Eフェーズ.演奏_STAGE_CLEAR_フェードアウト;
                    this.actFOClear.tフェードアウト開始();
                }

				if( bIsFinishedFadeout )
				{
					Debug.WriteLine( "Total On進行描画=" + sw.ElapsedMilliseconds + "ms" );
					return (int) this.eフェードアウト完了時の戻り値;
				}

				ManageMixerQueue();

				// キー入力

				if( TJAPlayer3.act現在入力を占有中のプラグイン == null )
					this.tキー入力();


            }
            base.sw.Stop();
			return 0;
		}

		// その他

		#region [ private ]
		//-----------------
		[StructLayout( LayoutKind.Sequential )]
		private struct ST文字位置
		{
			public char ch;
			public Point pt;
		}
		public CAct演奏DrumsチップファイアD actChipFireD;

        private CAct演奏Drumsグラフ actGraph;   // #24074 2011.01.23 add ikanick
		private CAct演奏Drumsパッド actPad;
        public CAct演奏Drumsレーン actLane;
        public CAct演奏DrumsMtaiko actMtaiko;
        public CAct演奏Drumsレーン太鼓 actLaneTaiko;
        public CAct演奏Drums演奏終了演出 actEnd;
        private CAct演奏Drumsゲームモード actGame;
        public CAct演奏Drums特訓モード actTokkun;
        public CAct演奏Drums背景 actBackground;
        public GoGoSplash GoGoSplash;
        public FlyingNotes FlyingNotes;
        public FireWorks FireWorks;
        public PuchiChara PuchiChara;
        public CAct演奏Drumsスコアランク ScoreRank;
        private bool bフィルイン中;
		private readonly Eパッド[] eチャンネルtoパッド = new Eパッド[]
		{
			Eパッド.HH, Eパッド.SD, Eパッド.BD, Eパッド.HT,
			Eパッド.LT, Eパッド.CY, Eパッド.FT, Eパッド.HHO,
			Eパッド.RD, Eパッド.UNKNOWN, Eパッド.UNKNOWN, Eパッド.LC,
            Eパッド.LP, Eパッド.LBD
		};
        private int[] nチャンネルtoX座標 = new int[] { 370, 470, 582, 527, 645, 748, 694, 373, 815, 298, 419, 419 };
        private CCounter ct手つなぎ;
        private CTexture txヒットバーGB;
		private CTexture txレーンフレームGB;
        //private CTexture tx太鼓ノーツ;
        //private CTexture txHand;
        //private CTexture txSenotes;
        //private CTexture tx小節線;
        //private CTexture tx小節線_branch;

        private CTexture tx判定数表示パネル;
        private CTexture tx判定数小文字;
        //private CTexture txNamePlate; //ちょっと描画順で都合が悪くなるので移動。
        //private CTexture txNamePlate2P; //ちょっと描画順で都合が悪くなるので移動。
        //private CTexture txPlayerNumber;
        private CTexture txMovie; //2016.08.30 kairera0467 ウィンドウ表示

        public float nGauge = 0.0f; 
        private int ShownLyric2 = 0;

        private StreamWriter stream;

        private int n待機中の大音符の座標;
        private readonly ST文字位置[] st小文字位置;
        private readonly ST文字位置[] st大文字位置;
		//-----------------

		protected override E判定 tチップのヒット処理( long nHitTime, CDTX.CChip pChip, bool bCorrectLane )
		{
			E判定 eJudgeResult = tチップのヒット処理( nHitTime, pChip, E楽器パート.DRUMS, bCorrectLane, 0 );
			// #24074 2011.01.23 add ikanick
            if( pChip.nコース == this.n現在のコース[ 0 ] && NotesManager.IsMissableNote(pChip) && pChip.bShow == true && eJudgeResult != E判定.Auto )
                this.actGame.t叩ききりまショー_判定から各数値を増加させる( eJudgeResult, (int)( nHitTime - pChip.n発声時刻ms ) );
			return eJudgeResult;
		}

        protected override void tチップのヒット処理_BadならびにTight時のMiss(CDTX.ECourse eCourse, E楽器パート part)
        {
            this.tチップのヒット処理_BadならびにTight時のMiss(eCourse, part, 0, E楽器パート.DRUMS);
        }
        protected override void tチップのヒット処理_BadならびにTight時のMiss(CDTX.ECourse eCourse, E楽器パート part, int nLane)
        {
            this.tチップのヒット処理_BadならびにTight時のMiss(eCourse, part, nLane, E楽器パート.DRUMS);
        }

        private int ChannelNumToFlyNoteNum(CDTX.CChip pChip, int nPlayer, bool b両手入力 = false, int nInput = 0)
        {
            var _gt = TJAPlayer3.ConfigIni.nGameType[TJAPlayer3.GetActualPlayer(nPlayer)];

            int nFly = 0;
            switch (pChip.nチャンネル番号)
            {
                case 0x11:
                    nFly = 1;
                    break;
                case 0x12:
                    nFly = 2;
                    break;
                case 0x13:
                case 0x1A:
                    nFly = b両手入力 ? 3 : 1;
                    break;
                case 0x14:
                case 0x1B:
                    nFly = (b両手入力 || _gt == EGameType.KONGA) ? 4 : 2;
                    break;
                case 0x1F:
                    nFly = nInput == 0 ? 1 : 2;
                    break;
                case 0x101:
                    nFly = 5;
                    break;
                default:
                    nFly = 1;
                    break;
            }
            return nFly;
        }

        private bool tドラムヒット処理( long nHitTime, Eパッド type, CDTX.CChip pChip, bool b両手入力, int nPlayer )
		{
            int nInput = 0;

            switch( type )
            {
                case Eパッド.LRed:
                case Eパッド.RRed:
                case Eパッド.LRed2P:
                case Eパッド.RRed2P:
                case Eパッド.LRed3P:
                case Eパッド.RRed3P:
                case Eパッド.LRed4P:
                case Eパッド.RRed4P:
                case Eパッド.LRed5P:
                case Eパッド.RRed5P:
                    nInput = 0;
                    if( b両手入力 )
                        nInput = 2;
                    break;
                case Eパッド.LBlue:
                case Eパッド.RBlue:
                case Eパッド.LBlue2P:
                case Eパッド.RBlue2P:
                case Eパッド.LBlue3P:
                case Eパッド.RBlue3P:
                case Eパッド.LBlue4P:
                case Eパッド.RBlue4P:
                case Eパッド.LBlue5P:
                case Eパッド.RBlue5P:
                    nInput = 1;
                    if( b両手入力 )
                        nInput = 3;
                    break;
                case Eパッド.CLAP:
                case Eパッド.CLAP2P:
                case Eパッド.CLAP3P:
                case Eパッド.CLAP4P:
                case Eパッド.CLAP5P:
                    nInput = 4;
                    break;
            }


		    if( pChip == null )
			{
				return false;
			}
            
            if (NotesManager.IsGenericRoll(pChip) && !NotesManager.IsRollEnd(pChip))
            {
			    this.tチップのヒット処理( nHitTime, pChip, E楽器パート.TAIKO, true, nInput, nPlayer );
                return true;
            }
            
            else if (!NotesManager.IsHittableNote(pChip))
            {
                return false;
            }

			E判定 e判定 = this.e指定時刻からChipのJUDGEを返す( nHitTime, pChip, nPlayer );

            e判定 = AlterJudgement(nPlayer, e判定, false);

            this.actGame.t叩ききりまショー_判定から各数値を増加させる( e判定, (int)( nHitTime - pChip.n発声時刻ms ) );

			if( e判定 == E判定.Miss )
			{
				return false;
			}

			this.tチップのヒット処理( nHitTime, pChip, E楽器パート.TAIKO, true, nInput, nPlayer );
			
            if( ( e判定 != E判定.Poor ) && ( e判定 != E判定.Miss ) )
			{
                TJAPlayer3.stage演奏ドラム画面.actLaneTaiko.Start( pChip.nチャンネル番号, e判定, b両手入力, nPlayer );

                int nFly = ChannelNumToFlyNoteNum(pChip, nPlayer, b両手入力, nInput);

                this.actTaikoLaneFlash.PlayerLane[nPlayer].Start(PlayerLane.FlashType.Hit);
                this.FlyingNotes.Start(nFly, nPlayer);
			}

			return true;
		}

		protected override void ドラムスクロール速度アップ()
		{
			TJAPlayer3.ConfigIni.nScrollSpeed[TJAPlayer3.SaveFile] = Math.Min( TJAPlayer3.ConfigIni.nScrollSpeed[TJAPlayer3.SaveFile] + 1, 1999 );
		}
		protected override void ドラムスクロール速度ダウン()
		{
			TJAPlayer3.ConfigIni.nScrollSpeed[TJAPlayer3.SaveFile] = Math.Max( TJAPlayer3.ConfigIni.nScrollSpeed[TJAPlayer3.SaveFile] - 1, 0 );
		}

	
		protected override void t進行描画_AVI()
		{
			base.t進行描画_AVI( 0, 0 );
		}
		protected override void t進行描画_DANGER()
		{
			this.actDANGER.t進行描画( this.actGauge.IsDanger(E楽器パート.DRUMS), false, false );
		}

		private void t進行描画_グラフ()        
        {
			if( TJAPlayer3.ConfigIni.bGraph.Drums )
			{
                this.actGraph.Draw();
            }
        }

		private void t進行描画_チップファイアD()
		{
			this.actChipFireD.Draw();
		}


		private void t進行描画_ドラムパッド()
		{
			if( TJAPlayer3.ConfigIni.eDark != Eダークモード.FULL )
			{
				this.actPad.Draw();
			}
		}
		protected override void t進行描画_パネル文字列()
		{
			base.t進行描画_パネル文字列( 336, 427 );
		}

		protected override void t進行描画_演奏情報()
		{
			base.t進行描画_演奏情報( 1000, 257 );
		}

        protected override void t紙吹雪_開始()
        {
            //if( this.actCombo.n現在のコンボ数.Drums % 10 == 0 && this.actCombo.n現在のコンボ数.Drums > 0 )
            {
                //this.actChipFireD.Start紙吹雪();
            }
        }

		protected override void t入力処理_ドラム()
		{
            // Input adjust deprecated
            var nInputAdjustTimeMs = 0; // TJAPlayer3.ConfigIni.nInputAdjustTimeMs;

			for( int nPad = 0; nPad < (int) Eパッド.MAX; nPad++ )		// #27029 2012.1.4 from: <10 to <=10; Eパッドの要素が１つ（HP）増えたため。
																		//		  2012.1.5 yyagi: (int)Eパッド.MAX に変更。Eパッドの要素数への依存を無くすため。
			{
				List<STInputEvent> listInputEvent = TJAPlayer3.Pad.GetEvents( E楽器パート.DRUMS, (Eパッド) nPad );

				if( ( listInputEvent == null ) || ( listInputEvent.Count == 0 ) )
					continue;

				this.t入力メソッド記憶( E楽器パート.DRUMS );

				foreach( STInputEvent inputEvent in listInputEvent )
				{
					if( !inputEvent.Pressed )
						continue;

                    long nTime = (long)(((SoundManager.PlayTimer.NowTimeMs + nInputAdjustTimeMs) * TJAPlayer3.ConfigIni.SongPlaybackSpeed));
                    //int nPad09 = ( nPad == (int) Eパッド.HP ) ? (int) Eパッド.BD : nPad;		// #27029 2012.1.5 yyagi

                    bool bHitted = false;

                    int nLane = 0;
                    int nHand = 0;
                    int nChannel = 0;

                    //連打チップを検索してから通常音符検索
                    //連打チップの検索は、
                    //一番近くの連打音符を探す→時刻チェック
                    //発声 < 現在時刻 && 終わり > 現在時刻

                    //2015.03.19 kairera0467 Chipを1つにまとめて1つのレーン扱いにする。

                    bool isPad1P = (nPad >= 12 && nPad <= 15) || nPad == 32;
                    bool isPad2P = (nPad >= 16 && nPad <= 19) || nPad == 33;
                    bool isPad3P = (nPad >= 20 && nPad <= 23) || nPad == 34;
                    bool isPad4P = (nPad >= 24 && nPad <= 27) || nPad == 35;
                    bool isPad5P = (nPad >= 28 && nPad <= 31) || nPad == 36;

                    int nUsePlayer = 0;
                    if (isPad1P)
                    {
                        nUsePlayer = 0;
                    }
                    else if (isPad2P)
                    {
                        nUsePlayer = 1;
                        if (TJAPlayer3.ConfigIni.nPlayerCount < 2) //プレイ人数が2人以上でなければ入力をキャンセル
                            break;
                    }
                    else if (isPad3P)
                    {
                        nUsePlayer = 2;
                        if (TJAPlayer3.ConfigIni.nPlayerCount < 3) //プレイ人数が3人以上でなければ入力をキャンセル
                            break;
                    }
                    else if (isPad4P)
                    {
                        nUsePlayer = 3;
                        if (TJAPlayer3.ConfigIni.nPlayerCount < 4) //プレイ人数が4人以上でなければ入力をキャンセル
                            break;
                    }
                    else if (isPad5P)
                    {
                        nUsePlayer = 4;
                        if (TJAPlayer3.ConfigIni.nPlayerCount < 5) //プレイ人数が5人以上でなければ入力をキャンセル
                            break;
                    }

                    if (TJAPlayer3.stage演奏ドラム画面.isDeniedPlaying[nUsePlayer]) break;

                    if (!TJAPlayer3.ConfigIni.bTokkunMode && TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[0] && isPad1P)//2020.05.18 Mr-Ojii オート時の入力キャンセル
                        break;
                    else if ((TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[1] || TJAPlayer3.ConfigIni.bAIBattleMode) && isPad2P)
                        break;
                    else if (TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[2] && isPad3P)
                        break;
                    else if (TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[3] && isPad4P)
                        break;
                    else if (TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[4] && isPad5P)
                        break;
                    //var padTo = nUsePlayer == 0 ? nPad - 12 : nPad - 12 - 4;
                    var padTo = nPad - 12;
                    padTo -= 4 * nUsePlayer;

                    var isDon = padTo < 2 ? true : false;

                    CDTX.CChip chipNoHit = r指定時刻に一番近い未ヒットChipを過去方向優先で検索する(nTime, nUsePlayer);
                    E判定 e判定 = (chipNoHit != null) ? this.e指定時刻からChipのJUDGEを返す(nTime, chipNoHit, nUsePlayer) : E判定.Miss;

                    e判定 = AlterJudgement(nUsePlayer, e判定, false);

                    #region [ADLIB]

                    bool b太鼓音再生フラグ = true;
                    if (chipNoHit != null)
                    {
                        if (NotesManager.IsADLIB(chipNoHit) && (e判定 == E判定.Perfect || e判定 == E判定.Good))
                            b太鼓音再生フラグ = false;
                        if (NotesManager.IsADLIB(chipNoHit) && (e判定 != E判定.Miss && e判定 != E判定.Poor))
                            this.soundAdlib[chipNoHit.nPlayerSide]?.PlayStart();
                    }

                    #endregion

                    #region [Visual effects]

                    switch (nPad)
                    {
                        case 12:
                            nLane = 0;
                            nHand = 0;
                            nChannel = 0x11;
                            if (b太鼓音再生フラグ)
                            {
                                this.soundRed[0]?.PlayStart();
                            }
                            break;
                        case 13:
                            nLane = 0;
                            nHand = 1;
                            nChannel = 0x11;
                            if (b太鼓音再生フラグ)
                            {
                                this.soundRed[0]?.PlayStart();
                            }
                            break;
                        case 14:
                            nLane = 1;
                            nHand = 0;
                            nChannel = 0x12;
                            if (b太鼓音再生フラグ)
                                this.soundBlue[0]?.PlayStart();
                            break;
                        case 15:
                            nLane = 1;
                            nHand = 1;
                            nChannel = 0x12;
                            if (b太鼓音再生フラグ)
                                this.soundBlue[0]?.PlayStart();
                            break;
                        //以下2P
                        case 16:
                            nLane = 0;
                            nHand = 0;
                            nChannel = 0x11;
                            if (b太鼓音再生フラグ)
                            {
                                this.soundRed[1]?.PlayStart();
                            }
                            break;
                        case 17:
                            nLane = 0;
                            nHand = 1;
                            nChannel = 0x11;
                            if (b太鼓音再生フラグ)
                            {
                                this.soundRed[1]?.PlayStart();
                            }
                            break;
                        case 18:
                            nLane = 1;
                            nHand = 0;
                            nChannel = 0x12;
                            if (b太鼓音再生フラグ)
                                this.soundBlue[1]?.PlayStart();
                            break;
                        case 19:
                            nLane = 1;
                            nHand = 1;
                            nChannel = 0x12;
                            if (b太鼓音再生フラグ)
                                this.soundBlue[1]?.PlayStart();
                            break;
                        //以下3P
                        case 20:
                            nLane = 0;
                            nHand = 0;
                            nChannel = 0x11;
                            if (b太鼓音再生フラグ)
                            {
                                this.soundRed[2]?.PlayStart();
                            }
                            break;
                        case 21:
                            nLane = 0;
                            nHand = 1;
                            nChannel = 0x11;
                            if (b太鼓音再生フラグ)
                            {
                                this.soundRed[2]?.PlayStart();
                            }
                            break;
                        case 22:
                            nLane = 1;
                            nHand = 0;
                            nChannel = 0x12;
                            if (b太鼓音再生フラグ)
                                this.soundBlue[2]?.PlayStart();
                            break;
                        case 23:
                            nLane = 1;
                            nHand = 1;
                            nChannel = 0x12;
                            if (b太鼓音再生フラグ)
                                this.soundBlue[2]?.PlayStart();
                            break;
                        //以下4P
                        case 24:
                            nLane = 0;
                            nHand = 0;
                            nChannel = 0x11;
                            if (b太鼓音再生フラグ)
                            {
                                this.soundRed[3]?.PlayStart();
                            }
                            break;
                        case 25:
                            nLane = 0;
                            nHand = 1;
                            nChannel = 0x11;
                            if (b太鼓音再生フラグ)
                            {
                                this.soundRed[3]?.PlayStart();
                            }
                            break;
                        case 26:
                            nLane = 1;
                            nHand = 0;
                            nChannel = 0x12;
                            if (b太鼓音再生フラグ)
                                this.soundBlue[3]?.PlayStart();
                            break;
                        case 27:
                            nLane = 1;
                            nHand = 1;
                            nChannel = 0x12;
                            if (b太鼓音再生フラグ)
                                this.soundBlue[3]?.PlayStart();
                            break;
                        //以下5P
                        case 28:
                            nLane = 0;
                            nHand = 0;
                            nChannel = 0x11;
                            if (b太鼓音再生フラグ)
                            {
                                this.soundRed[4]?.PlayStart();
                            }
                            break;
                        case 29:
                            nLane = 0;
                            nHand = 1;
                            nChannel = 0x11;
                            if (b太鼓音再生フラグ)
                            {
                                this.soundRed[4]?.PlayStart();
                            }
                            break;
                        case 30:
                            nLane = 1;
                            nHand = 0;
                            nChannel = 0x12;
                            if (b太鼓音再生フラグ)
                                this.soundBlue[4]?.PlayStart();
                            break;
                        case 31:
                            nLane = 1;
                            nHand = 1;
                            nChannel = 0x12;
                            if (b太鼓音再生フラグ)
                                this.soundBlue[4]?.PlayStart();
                            break;
                        // Clap
                        case (int)Eパッド.CLAP:
                            if (TJAPlayer3.ConfigIni.nGameType[TJAPlayer3.GetActualPlayer(0)] == EGameType.KONGA)
                            {
                                nLane = (int)PlayerLane.FlashType.Clap;
                                nHand = 0;
                                nChannel = 0x14;
                                if (b太鼓音再生フラグ)
                                {
                                    this.soundClap[0]?.PlayStart();
                                }
                            }
                            else
                            {
                                nLane = (int)PlayerLane.FlashType.Total;
                            }
                            break;
                        case (int)Eパッド.CLAP2P:
                            if (TJAPlayer3.ConfigIni.nGameType[TJAPlayer3.GetActualPlayer(1)] == EGameType.KONGA)
                            {
                                nLane = (int)PlayerLane.FlashType.Clap;
                                nHand = 0;
                                nChannel = 0x14;
                                if (b太鼓音再生フラグ)
                                {
                                    this.soundClap[1]?.PlayStart();
                                }
                            }
                            else
                            {
                                nLane = (int)PlayerLane.FlashType.Total;
                            }
                            break;
                        case (int)Eパッド.CLAP3P:
                            if (TJAPlayer3.ConfigIni.nGameType[TJAPlayer3.GetActualPlayer(1)] == EGameType.KONGA)
                            {
                                nLane = (int)PlayerLane.FlashType.Clap;
                                nHand = 0;
                                nChannel = 0x14;
                                if (b太鼓音再生フラグ)
                                {
                                    this.soundClap[2]?.PlayStart();
                                }
                            }
                            else
                            {
                                nLane = (int)PlayerLane.FlashType.Total;
                            }
                            break;
                        case (int)Eパッド.CLAP4P:
                            if (TJAPlayer3.ConfigIni.nGameType[TJAPlayer3.GetActualPlayer(1)] == EGameType.KONGA)
                            {
                                nLane = (int)PlayerLane.FlashType.Clap;
                                nHand = 0;
                                nChannel = 0x14;
                                if (b太鼓音再生フラグ)
                                {
                                    this.soundClap[3]?.PlayStart();
                                }
                            }
                            else
                            {
                                nLane = (int)PlayerLane.FlashType.Total;
                            }
                            break;
                        case (int)Eパッド.CLAP5P:
                            if (TJAPlayer3.ConfigIni.nGameType[TJAPlayer3.GetActualPlayer(1)] == EGameType.KONGA)
                            {
                                nLane = (int)PlayerLane.FlashType.Clap;
                                nHand = 0;
                                nChannel = 0x14;
                                if (b太鼓音再生フラグ)
                                {
                                    this.soundClap[4]?.PlayStart();
                                }
                            }
                            else
                            {
                                nLane = (int)PlayerLane.FlashType.Total;
                            }
                            break;
                        default:
                            {
                                continue;
                            }
                            break;
                    }

                    TJAPlayer3.stage演奏ドラム画面.actTaikoLaneFlash.PlayerLane[nUsePlayer].Start((PlayerLane.FlashType)nLane);
                    TJAPlayer3.stage演奏ドラム画面.actMtaiko.tMtaikoEvent(nChannel, nHand, nUsePlayer);

                    #endregion

                    // Chip bools
                    EGameType _gt = TJAPlayer3.ConfigIni.nGameType[TJAPlayer3.GetActualPlayer(nUsePlayer)];
                    bool _isBigKaTaiko = NotesManager.IsBigKaTaiko(chipNoHit, _gt);
                    bool _isBigDonTaiko = NotesManager.IsBigDonTaiko(chipNoHit, _gt);
                    bool _isClapKonga = NotesManager.IsClapKonga(chipNoHit, _gt);
                    bool _isPinkKonga = NotesManager.IsSwapNote(chipNoHit, _gt);
                    

                    if (this.b連打中[nUsePlayer])
                    {
                        chipNoHit = this.chip現在処理中の連打チップ[nUsePlayer];
                        e判定 = E判定.Perfect;
                    }

                    if (chipNoHit == null)
                    {
                        break;
                    }

                    switch (((Eパッド)nPad))
                    {
                        case Eパッド.LRed:
                        case Eパッド.LRed2P:
                        case Eパッド.LRed3P:
                        case Eパッド.LRed4P:
                        case Eパッド.LRed5P:
                        case Eパッド.RRed:
                        case Eパッド.RRed2P:
                        case Eパッド.RRed3P:
                        case Eパッド.RRed4P:
                        case Eパッド.RRed5P:
                        case Eパッド.LBlue:
                        case Eパッド.LBlue2P:
                        case Eパッド.LBlue3P:
                        case Eパッド.LBlue4P:
                        case Eパッド.LBlue5P:
                        case Eパッド.RBlue:
                        case Eパッド.RBlue2P:
                        case Eパッド.RBlue3P:
                        case Eパッド.RBlue4P:
                        case Eパッド.RBlue5P:
                            {

                                // Regular notes

                                #region [Fetch values]

                                // Flatten pads from 8 to 4
                                var _pad = (Eパッド)nPad;
                                if ((Eパッド)nPad == Eパッド.LRed2P) _pad = Eパッド.LRed;
                                if ((Eパッド)nPad == Eパッド.RRed2P) _pad = Eパッド.RRed;
                                if ((Eパッド)nPad == Eパッド.LBlue2P) _pad = Eパッド.LBlue;
                                if ((Eパッド)nPad == Eパッド.RBlue2P) _pad = Eパッド.RBlue;

                                if ((Eパッド)nPad == Eパッド.LRed3P) _pad = Eパッド.LRed;
                                if ((Eパッド)nPad == Eパッド.RRed3P) _pad = Eパッド.RRed;
                                if ((Eパッド)nPad == Eパッド.LBlue3P) _pad = Eパッド.LBlue;
                                if ((Eパッド)nPad == Eパッド.RBlue3P) _pad = Eパッド.RBlue;

                                if ((Eパッド)nPad == Eパッド.LRed4P) _pad = Eパッド.LRed;
                                if ((Eパッド)nPad == Eパッド.RRed4P) _pad = Eパッド.RRed;
                                if ((Eパッド)nPad == Eパッド.LBlue4P) _pad = Eパッド.LBlue;
                                if ((Eパッド)nPad == Eパッド.RBlue4P) _pad = Eパッド.RBlue;

                                if ((Eパッド)nPad == Eパッド.LRed5P) _pad = Eパッド.LRed;
                                if ((Eパッド)nPad == Eパッド.RRed5P) _pad = Eパッド.RRed;
                                if ((Eパッド)nPad == Eパッド.LBlue5P) _pad = Eパッド.LBlue;
                                if ((Eパッド)nPad == Eパッド.RBlue5P) _pad = Eパッド.RBlue;

                                bool _isLeftPad = _pad == Eパッド.LRed || _pad == Eパッド.LBlue;
                                bool _isBlue = _pad == Eパッド.RBlue || _pad == Eパッド.LBlue;

                                int waitInstr = _isLeftPad ? 2 : 1;
                                int waitRec = waitInstr == 2 ? 1 : 2;

                                bool _isBigNoteTaiko = _isBlue ? _isBigKaTaiko : _isBigDonTaiko;
                                bool _isSmallNote = NotesManager.IsSmallNote(chipNoHit, _isBlue);

                                #endregion

                                // Register to replay file
                                TJAPlayer3.ReplayInstances[nUsePlayer]?.tRegisterInput(nTime, (byte)_pad);

                                // Process small note
                                if (e判定 != E判定.Miss && _isSmallNote)
                                {
                                    this.tドラムヒット処理(nTime, _pad, chipNoHit, false, nUsePlayer);
                                    bHitted = true;
                                }

                                // Process big notes (judge big notes off)
                                if (e判定 != E判定.Miss && _isBigNoteTaiko && !TJAPlayer3.ConfigIni.b大音符判定)
                                {
                                    this.tドラムヒット処理(nTime, _pad, chipNoHit, true, nUsePlayer);
                                    bHitted = true;
                                    //this.nWaitButton = 0;
                                    this.nStoredHit[nUsePlayer] = 0;
                                    break;
                                }

                                // Process big notes (judge big notes on)
                                if (e判定 != E判定.Miss && ((_isBigNoteTaiko && TJAPlayer3.ConfigIni.b大音符判定) || _isPinkKonga))
                                {
                                    double divided_songspeed = TJAPlayer3.ConfigIni.SongPlaybackSpeed;
                                    float time = chipNoHit.n発声時刻ms - (float)(SoundManager.PlayTimer.NowTimeMs * divided_songspeed);
                                    int nWaitTime = TJAPlayer3.ConfigIni.n両手判定の待ち時間;

                                    bool _timeB110 = time <= 110;

                                    if (chipNoHit.eNoteState == ENoteState.none)
                                    {
                                        if (_timeB110)
                                        {
                                            chipNoHit.nProcessTime = (int)(SoundManager.PlayTimer.NowTimeMs * divided_songspeed);
                                            chipNoHit.eNoteState = ENoteState.wait;
                                            //this.nWaitButton = waitInstr;
                                            this.nStoredHit[nUsePlayer] = (int)_pad;
                                        }
                                    }
                                    else if (chipNoHit.eNoteState == ENoteState.wait)
                                    {

                                        bool _isExpected = NotesManager.IsExpectedPad(this.nStoredHit[nUsePlayer], (int)_pad, chipNoHit, _gt);

                                        // Double tap success
                                        // (this.nWaitButton == waitRec && _timeB110 && chipNoHit.nProcessTime 
                                        //   + nWaitTime > (int)(CSound管理.rc演奏用タイマ.n現在時刻ms * divided_songspeed))

                                        if (_isExpected && _timeB110 && chipNoHit.nProcessTime
                                           + nWaitTime > (int)(SoundManager.PlayTimer.NowTimeMs * divided_songspeed))
                                        {
                                            this.tドラムヒット処理(nTime, _pad, chipNoHit, true, nUsePlayer);
                                            bHitted = true;
                                            //this.nWaitButton = 0;
                                            this.nStoredHit[nUsePlayer] = 0;
                                        }

                                        // Double tap failure
                                        // else if (this.nWaitButton == waitInstr && _timeB110 && chipNoHit.nProcessTime 
                                        //    + nWaitTime < (int)(CSound管理.rc演奏用タイマ.n現在時刻ms * divided_songspeed))
                                        else if (!_isExpected || (_timeB110 && chipNoHit.nProcessTime
                                            + nWaitTime < (int)(SoundManager.PlayTimer.NowTimeMs * divided_songspeed)))
                                        {
                                            if (!_isPinkKonga)
                                            {
                                                this.tドラムヒット処理(nTime, _pad, chipNoHit, false, nUsePlayer);
                                                bHitted = true;
                                            }
                                            
                                            //this.nWaitButton = 0;
                                            this.nStoredHit[nUsePlayer] = 0;
                                        }
                                    }
                                }

                                // Judge rolls
                                if (e判定 != E判定.Miss 
                                    && NotesManager.IsGenericRoll(chipNoHit) 
                                    && !NotesManager.IsRollEnd(chipNoHit))
                                {
                                    bool _isBalloon = NotesManager.IsGenericBalloon(chipNoHit);
                                    bool _isKusudama = NotesManager.IsKusudama(chipNoHit);
                                    bool _isKongaRedRoll = (NotesManager.IsSmallRoll(chipNoHit) || NotesManager.IsBigRoll(chipNoHit)) || _gt == EGameType.TAIKO;

                                    bool _isRedOnly = _isBalloon || _isKongaRedRoll || _isKusudama;

                                    // To be added later
                                    bool _isKongaPinkRoll = NotesManager.IsBigRoll(chipNoHit) && _gt == EGameType.KONGA;

                                    // To improve (array of functions ?)
                                    bool _isBlueOnly = ((NotesManager.IsYellowRoll(chipNoHit) || NotesManager.IsBigRoll(chipNoHit)) || _gt == EGameType.TAIKO)
                                        && !_isBalloon && !_isKusudama;

                                    if ((_isRedOnly && !_isBlue) || (_isBlueOnly && _isBlue))
                                        this.tドラムヒット処理(nTime, _pad, chipNoHit, false, nUsePlayer);
                                }

                                if (!bHitted)
                                    break;
                                continue;

                            }

                        case Eパッド.CLAP:
                        case Eパッド.CLAP2P:
                        case Eパッド.CLAP3P:
                        case Eパッド.CLAP4P:
                        case Eパッド.CLAP5P:
                            {
                                var _pad = (Eパッド)nPad;

                                // Process konga clap
                                if (e判定 != E判定.Miss && _isClapKonga)
                                {
                                    this.tドラムヒット処理(nTime, _pad, chipNoHit, false, nUsePlayer);
                                    bHitted = true;
                                }

                                // Judge rolls
                                if (e判定 != E判定.Miss
                                    && NotesManager.IsGenericRoll(chipNoHit)
                                    && !NotesManager.IsRollEnd(chipNoHit))
                                {
                                    bool _isKongaClapRoll = NotesManager.IsClapRoll(chipNoHit) && _gt == EGameType.KONGA;

                                    if (_isKongaClapRoll)
                                        this.tドラムヒット処理(nTime, _pad, chipNoHit, false, nUsePlayer);
                                }


                                if (!bHitted)
                                    break;
                                continue;
                            }

                    }


                    if (e判定 != E判定.Miss && NotesManager.IsADLIB(chipNoHit))
                    {
                        this.tドラムヒット処理(nTime, (Eパッド)nPad, chipNoHit, false, nUsePlayer);
                        bHitted = true;
                    }

                    if (e判定 != E判定.Miss && NotesManager.IsMine(chipNoHit))
                    {
                        this.tドラムヒット処理(nTime, (Eパッド)nPad, chipNoHit, false, nUsePlayer);
                        bHitted = true;
                    }



                    #region [ (A) ヒットしていればヒット処理して次の inputEvent へ ]
                    //-----------------------------

                    #region [Legacy]

                    /*
                    switch (((Eパッド)nPad))
                    {
                        case Eパッド.LRed:
                        case Eパッド.LRed2P:
                            #region[ 面のヒット処理 ]
                            //-----------------------------
                            {
                            if (e判定 != E判定.Miss && chipNoHit.nチャンネル番号 == 0x11)
                            {
                                    this.tドラムヒット処理(nTime, Eパッド.LRed, chipNoHit, false, nUsePlayer);
                                    bHitted = true;
                                }
                                if (e判定 != E判定.Miss && (_isBigDonTaiko || chipNoHit.nチャンネル番号 == 0x1A) && !TJAPlayer3.ConfigIni.b大音符判定)
                                {
                                    this.tドラムヒット処理(nTime, Eパッド.LRed, chipNoHit, true, nUsePlayer);
                                    bHitted = true;
                                    this.nWaitButton = 0;
                                    break;
                                }
                                if (e判定 != E判定.Miss && (_isBigDonTaiko || chipNoHit.nチャンネル番号 == 0x1A) && TJAPlayer3.ConfigIni.b大音符判定)
                                {
                                    if (chipNoHit.eNoteState == ENoteState.none)
                                    {
                                        float time = chipNoHit.n発声時刻ms - (float)(CSound管理.rc演奏用タイマ.n現在時刻ms * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0));
                                        if (time <= 110)
                                        {
                                            chipNoHit.nProcessTime = (int)(CSound管理.rc演奏用タイマ.n現在時刻 * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0));
                                            chipNoHit.eNoteState = ENoteState.wait;
                                            this.nWaitButton = 2;
                                        }
                                    }
                                    else if (chipNoHit.eNoteState == ENoteState.wait)
                                    {
                                        float time = chipNoHit.n発声時刻ms - (float)(CSound管理.rc演奏用タイマ.n現在時刻ms * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0));
                                        int nWaitTime = TJAPlayer3.ConfigIni.n両手判定の待ち時間;
                                        if (this.nWaitButton == 1 && time <= 110 && chipNoHit.nProcessTime + nWaitTime > (int)(CSound管理.rc演奏用タイマ.n現在時刻 * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0)))
                                        {
                                            this.tドラムヒット処理(nTime, Eパッド.LRed, chipNoHit, true, nUsePlayer);
                                            bHitted = true;
                                            this.nWaitButton = 0;
                                        }
                                        else if (this.nWaitButton == 2 && time <= 110 && chipNoHit.nProcessTime + nWaitTime < (int)(CSound管理.rc演奏用タイマ.n現在時刻 * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0)))
                                        {
                                            this.tドラムヒット処理(nTime, Eパッド.LRed, chipNoHit, false, nUsePlayer);
                                            bHitted = true;
                                            this.nWaitButton = 0;
                                        }
                                }
                            }
                                if (e判定 != E判定.Miss && (chipNoHit.nチャンネル番号 == 0x15 || chipNoHit.nチャンネル番号 == 0x16 || chipNoHit.nチャンネル番号 == 0x17))
                                {
                                    this.tドラムヒット処理(nTime, Eパッド.LRed, chipNoHit, false, nUsePlayer);
                                }

                                if (!bHitted)
                                    break;
                                continue;
                            }
                        //-----------------------------
                        #endregion
                        case Eパッド.RRed:
                        case Eパッド.RRed2P:
                            #region[ 面のヒット処理 ]
                            //-----------------------------
                            {
                            if (e判定 != E判定.Miss && chipNoHit.nチャンネル番号 == 0x11)
                            {
                                    this.tドラムヒット処理(nTime, Eパッド.RRed, chipNoHit, false, nUsePlayer);
                                    bHitted = true;
                                }
                                if (e判定 != E判定.Miss && (_isBigDonTaiko || chipNoHit.nチャンネル番号 == 0x1A) && !TJAPlayer3.ConfigIni.b大音符判定)
                                {
                                    this.tドラムヒット処理(nTime, Eパッド.RRed, chipNoHit, true, nUsePlayer);
                                    bHitted = true;
                                    this.nWaitButton = 0;
                                    break;
                                }
                                if (e判定 != E判定.Miss && (_isBigDonTaiko || chipNoHit.nチャンネル番号 == 0x1A) && TJAPlayer3.ConfigIni.b大音符判定)
                                {
                                    if (chipNoHit.eNoteState == ENoteState.none)
                                    {
                                        float time = chipNoHit.n発声時刻ms - (float)(CSound管理.rc演奏用タイマ.n現在時刻ms * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0));
                                        if (time <= 110)
                                        {
                                            chipNoHit.nProcessTime = (int)CSound管理.rc演奏用タイマ.n現在時刻ms;
                                            this.n待機中の大音符の座標 = chipNoHit.nバーからの距離dot.Taiko;
                                            chipNoHit.eNoteState = ENoteState.wait;
                                            this.nWaitButton = 1;
                                        }
                                    }
                                    else if (chipNoHit.eNoteState == ENoteState.wait)
                                    {
                                        float time = chipNoHit.n発声時刻ms - (float)(CSound管理.rc演奏用タイマ.n現在時刻ms * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0));
                                        int nWaitTime = TJAPlayer3.ConfigIni.n両手判定の待ち時間;
                                        if (this.nWaitButton == 2 && time <= 110 && chipNoHit.nProcessTime + nWaitTime > (int)(CSound管理.rc演奏用タイマ.n現在時刻 * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0)))
                                        {
                                            this.tドラムヒット処理(nTime, Eパッド.RRed, chipNoHit, true, nUsePlayer);
                                            bHitted = true;
                                            this.nWaitButton = 0;
                                            break;
                                        }
                                        else if (this.nWaitButton == 2 && time <= 110 && chipNoHit.nProcessTime + nWaitTime < (int)(CSound管理.rc演奏用タイマ.n現在時刻 * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0)))
                                        {
                                            this.tドラムヒット処理(nTime, Eパッド.RRed, chipNoHit, false, nUsePlayer);
                                            bHitted = true;
                                            this.nWaitButton = 0;
                                        }
                                    }
                                }
                                if (e判定 != E判定.Miss && (chipNoHit.nチャンネル番号 == 0x15 || chipNoHit.nチャンネル番号 == 0x16 || chipNoHit.nチャンネル番号 == 0x17))
                                {
                                    this.tドラムヒット処理(nTime, Eパッド.RRed, chipNoHit, false, nUsePlayer);
                                }

                                if (!bHitted)
                                    break;

                                continue;
                            }
                        //-----------------------------
                        #endregion

                        case Eパッド.LBlue:
                        case Eパッド.LBlue2P:
                            #region[ ふちのヒット処理 ]
                            //-----------------------------
                            {

                                

                            if (e判定 != E判定.Miss && chipNoHit.nチャンネル番号 == 0x12)
                            {
                                    this.tドラムヒット処理(nTime, Eパッド.LBlue, chipNoHit, false, nUsePlayer);
                                    bHitted = true;
                                }
                                if (e判定 != E判定.Miss && (chipNoHit.nチャンネル番号 == 0x14 || chipNoHit.nチャンネル番号 == 0x1B) && !TJAPlayer3.ConfigIni.b大音符判定)
                                {
                                    this.tドラムヒット処理(nTime, Eパッド.LBlue, chipNoHit, true, nUsePlayer);
                                    bHitted = true;
                                    this.nWaitButton = 0;
                                    break;
                                }
                                if (e判定 != E判定.Miss && (chipNoHit.nチャンネル番号 == 0x14 || chipNoHit.nチャンネル番号 == 0x1B) && TJAPlayer3.ConfigIni.b大音符判定)
                                {
                                    if (chipNoHit.eNoteState == ENoteState.none)
                                    {
                                        float time = chipNoHit.n発声時刻ms - (float)(CSound管理.rc演奏用タイマ.n現在時刻ms * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0));
                                        if (time <= 110)
                                        {
                                            chipNoHit.nProcessTime = (int)CSound管理.rc演奏用タイマ.n現在時刻ms;
                                            chipNoHit.eNoteState = ENoteState.wait;
                                            this.nWaitButton = 2;
                                        }
                                    }
                                    else if (chipNoHit.eNoteState == ENoteState.wait)
                                    {
                                        float time = chipNoHit.n発声時刻ms - (float)(CSound管理.rc演奏用タイマ.n現在時刻ms * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0));
                                        int nWaitTime = TJAPlayer3.ConfigIni.n両手判定の待ち時間;
                                        if (this.nWaitButton == 1 && time <= 110 && chipNoHit.nProcessTime + nWaitTime > (int)(CSound管理.rc演奏用タイマ.n現在時刻 * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0)))
                                        {
                                            this.tドラムヒット処理(nTime, Eパッド.LBlue, chipNoHit, true, nUsePlayer);
                                            bHitted = true;
                                            this.nWaitButton = 0;
                                        }
                                        else if (this.nWaitButton == 2 && time <= 110 && chipNoHit.nProcessTime + nWaitTime < (int)(CSound管理.rc演奏用タイマ.n現在時刻 * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0)))
                                        {
                                            this.tドラムヒット処理(nTime, Eパッド.LBlue, chipNoHit, false, nUsePlayer);
                                            bHitted = true;
                                            this.nWaitButton = 0;
                                        }
                                    }
                            }
                                if (e判定 != E判定.Miss && (chipNoHit.nチャンネル番号 == 0x15 || chipNoHit.nチャンネル番号 == 0x16))
                                {
                                    this.tドラムヒット処理(nTime, Eパッド.LBlue, chipNoHit, false, nUsePlayer);
                                }

                                if (!bHitted)
                                    break;
                                continue;
                            }
                        //-----------------------------
                        #endregion

                        case Eパッド.RBlue:
                        case Eパッド.RBlue2P:
                            #region[ ふちのヒット処理 ]
                            //-----------------------------
                            {
                                if (e判定 != E判定.Miss && chipNoHit.nチャンネル番号 == 0x12)
                                {
                                    this.tドラムヒット処理(nTime, Eパッド.RBlue, chipNoHit, false, nUsePlayer);
                                    bHitted = true;
                                }
                                if (e判定 != E判定.Miss && (chipNoHit.nチャンネル番号 == 0x14 || chipNoHit.nチャンネル番号 == 0x1B) && !TJAPlayer3.ConfigIni.b大音符判定)
                                {
                                    this.tドラムヒット処理(nTime, Eパッド.RBlue, chipNoHit, true, nUsePlayer);
                                    bHitted = true;
                                    this.nWaitButton = 0;
                                    break;
                                }
                                if (e判定 != E判定.Miss && (chipNoHit.nチャンネル番号 == 0x14 || chipNoHit.nチャンネル番号 == 0x1B) && TJAPlayer3.ConfigIni.b大音符判定)
                                {
                                    if (chipNoHit.eNoteState == ENoteState.none)
                                    {
                                        float time = chipNoHit.n発声時刻ms - (float)(CSound管理.rc演奏用タイマ.n現在時刻ms * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0));
                                        if (time <= 110)
                                        {
                                            chipNoHit.nProcessTime = (int)CSound管理.rc演奏用タイマ.n現在時刻ms;
                                            this.n待機中の大音符の座標 = chipNoHit.nバーからの距離dot.Taiko;
                                            chipNoHit.eNoteState = ENoteState.wait;
                                            this.nWaitButton = 1;
                                        }
                                    }
                                    else if (chipNoHit.eNoteState == ENoteState.wait)
                                    {
                                        float time = chipNoHit.n発声時刻ms - (float)(CSound管理.rc演奏用タイマ.n現在時刻ms * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0));
                                        int nWaitTime = TJAPlayer3.ConfigIni.n両手判定の待ち時間;
                                        if (this.nWaitButton == 2 && time <= 110 && chipNoHit.nProcessTime + nWaitTime > (int)(CSound管理.rc演奏用タイマ.n現在時刻 * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0)))
                                        {
                                            this.tドラムヒット処理(nTime, Eパッド.RBlue, chipNoHit, true, nUsePlayer);
                                            bHitted = true;
                                            this.nWaitButton = 0;
                                            break;
                                        }
                                        else if (this.nWaitButton == 2 && time <= 110 && chipNoHit.nProcessTime + nWaitTime < (int)(CSound管理.rc演奏用タイマ.n現在時刻 * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0)))
                                        {
                                            this.tドラムヒット処理(nTime, Eパッド.RBlue, chipNoHit, false, nUsePlayer);
                                            bHitted = true;
                                            this.nWaitButton = 0;
                                        }
                                }
                             }
                                if (e判定 != E判定.Miss && (chipNoHit.nチャンネル番号 == 0x15 || chipNoHit.nチャンネル番号 == 0x16))
                                {
                                    this.tドラムヒット処理(nTime, Eパッド.RBlue, chipNoHit, false, nUsePlayer);
                                }

                                if (!bHitted)
                                    break;
                                continue;
                            }
                            //-----------------------------
                            #endregion
                    }
                    */

                    #endregion

                    //2016.07.14 kairera0467 Adlibの場合、一括して処理を行う。


                    //-----------------------------
                    #endregion


                    #region [ (B) ヒットしてなかった場合は、レーンフラッシュ、パッドアニメ、空打ち音再生を実行 ]
                    //-----------------------------
                    int pad = nPad; // 以下、nPad の代わりに pad を用いる。（成りすまし用）
                                    // BAD or TIGHT 時の処理。
                    if (TJAPlayer3.ConfigIni.bTight && !b連打中[nUsePlayer]) // 18/8/13 - 連打時にこれが発動すると困る!!! (AioiLight)
                        this.tチップのヒット処理_BadならびにTight時のMiss(chipNoHit.nコース, E楽器パート.DRUMS, 0, E楽器パート.TAIKO);
                    //-----------------------------
                    #endregion
                }
            }
		}

		// t入力処理_ドラム()からメソッドを抽出したもの。
		/// <summary>
		/// chipArrayの中を, n発生位置の小さい順に並べる + nullを大きい方に退かす。セットでe判定Arrayも並べ直す。
		/// </summary>
		/// <param name="chipArray">ソート対象chip群</param>
		/// <param name="e判定Array">ソート対象e判定群</param>
		/// <param name="NumOfChips">チップ数</param>
		private static void SortChipsByNTime( CDTX.CChip[] chipArray, E判定[] e判定Array, int NumOfChips )
		{
			for ( int i = 0; i < NumOfChips - 1; i++ )
			{
				//num9 = 2;
				//while( num9 > num8 )
				for ( int j = NumOfChips - 1; j > i; j-- )
				{
					if ( ( chipArray[ j - 1 ] == null ) || ( ( chipArray[ j ] != null ) && ( chipArray[ j - 1 ].n発声位置 > chipArray[ j ].n発声位置 ) ) )
					{
						// swap
						CDTX.CChip chipTemp = chipArray[ j - 1 ];
						chipArray[ j - 1 ] = chipArray[ j ];
						chipArray[ j ] = chipTemp;
						E判定 e判定Temp = e判定Array[ j - 1 ];
						e判定Array[ j - 1 ] = e判定Array[ j ];
						e判定Array[ j ] = e判定Temp;
					}
					//num9--;
				}
				//num8++;
			}
		}

		protected override void t背景テクスチャの生成()
		{
			Rectangle bgrect = new Rectangle( 0, 0, 1280, 720 );
			string DefaultBgFilename = @$"Graphics{Path.DirectorySeparatorChar}5_Game{Path.DirectorySeparatorChar}5_Background{Path.DirectorySeparatorChar}0{Path.DirectorySeparatorChar}Background.png";
			string BgFilename = "";
            if( !String.IsNullOrEmpty( TJAPlayer3.DTX.strBGIMAGE_PATH ) )
                BgFilename = TJAPlayer3.DTX.strBGIMAGE_PATH;
			base.t背景テクスチャの生成( DefaultBgFilename, bgrect, BgFilename );
		}
		protected override void t進行描画_チップ_Taiko( CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip, int nPlayer )
        {
            int nLane = (int)PlayerLane.FlashType.Red;
            EGameType _gt = TJAPlayer3.ConfigIni.nGameType[TJAPlayer3.GetActualPlayer(nPlayer)];


            #region[ 作り直したもの ]

            if (pChip.b可視)
            {
                if (!pChip.bHit)
                {
                    long nPlayTime = (long)(SoundManager.PlayTimer.NowTimeMs * TJAPlayer3.ConfigIni.SongPlaybackSpeed);
                    if ((!pChip.bHit) && (pChip.n発声時刻ms <= nPlayTime))
                    {
                        bool bAutoPlay = TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[nPlayer];
                        switch (nPlayer)
                        {
                            case 1:
                                bAutoPlay = TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[nPlayer] || TJAPlayer3.ConfigIni.bAIBattleMode;
                                break;
                        }

                        if (bAutoPlay && !this.bPAUSE)
                        {
                            pChip.bHit = true;
                            if (!NotesManager.IsADLIB(pChip)) // Provisional, to avoid crash on 0x101
                                this.FlyingNotes.Start(ChannelNumToFlyNoteNum(pChip, nPlayer), nPlayer);

                            //this.actChipFireTaiko.Start(pChip.nチャンネル番号 < 0x1A ? (pChip.nチャンネル番号 - 0x10) : (pChip.nチャンネル番号 - 0x17), nPlayer);
                            if (pChip.nチャンネル番号 == 0x12 || pChip.nチャンネル番号 == 0x14 || pChip.nチャンネル番号 == 0x1B) nLane = (int)PlayerLane.FlashType.Blue;

                            if (pChip.nチャンネル番号 == 0x14 && _gt == EGameType.KONGA) nLane = (int)PlayerLane.FlashType.Clap;

                            TJAPlayer3.stage演奏ドラム画面.actTaikoLaneFlash.PlayerLane[nPlayer].Start((PlayerLane.FlashType)nLane);
                            TJAPlayer3.stage演奏ドラム画面.actTaikoLaneFlash.PlayerLane[nPlayer].Start(PlayerLane.FlashType.Hit);

                            this.actMtaiko.tMtaikoEvent(pChip.nチャンネル番号, this.nHand[nPlayer], nPlayer);

                            int n大音符 = (pChip.nチャンネル番号 == 0x11 || pChip.nチャンネル番号 == 0x12 ? 2 : 0);

                            this.tチップのヒット処理(pChip.n発声時刻ms, pChip, E楽器パート.TAIKO, true, nLane + n大音符, nPlayer, false);
                            this.tサウンド再生(pChip, nPlayer);
                            return;
                        }
                    }


                    if ( pChip.nノーツ出現時刻ms != 0 && ( nPlayTime < pChip.n発声時刻ms - pChip.nノーツ出現時刻ms ) )
                        pChip.bShow = false;
                    else
                        pChip.bShow = true;


                    switch (nPlayer)
                    {
                        case 0:
                            break;
                        case 1:
                            break;
                    }
                    switch( pChip.nPlayerSide )
                    {
                        case 1:
                            break;
                    }

                    int x = pChip.nバーからの距離dot.Taiko;
                    int y = NoteOriginY[nPlayer];// + ((int)(pChip.nコース) * 100)

                    int xTemp = 0;
                    int yTemp = 0;

                    #region[ スクロール方向変更 ]
                    if( pChip.nスクロール方向 != 0 )
                    {
                        xTemp = x;
                        yTemp = y;
                    }
                    switch ( pChip.nスクロール方向 )
                    {
                        case 0:
                            x += ( NoteOriginX[ nPlayer ] );
                            break;
                        case 1:
                            x = (NoteOriginX[ nPlayer ] );
                            y = NoteOriginY[ nPlayer ] - xTemp;
                            break;
                        case 2:
                            x = (NoteOriginX[ nPlayer ] + 3 );
                            y = NoteOriginY[ nPlayer ] + xTemp;
                            break;
                        case 3:
                            x += (NoteOriginX[ nPlayer ] );
                            y = NoteOriginY[ nPlayer ] - xTemp;
                            break;
                        case 4:
                            x += (NoteOriginX[ nPlayer ] );
                            y = NoteOriginY[ nPlayer ] + xTemp;
                            break;
                        case 5:
                            x = (NoteOriginX[ nPlayer ] + 10 ) - xTemp;
                            break;
                        case 6:
                            x = (NoteOriginX[ nPlayer ] ) - xTemp;
                            y = NoteOriginY[ nPlayer ] - xTemp;
                            break;
                        case 7:
                            x = (NoteOriginX[ nPlayer ] ) - xTemp;
                            y = NoteOriginY[ nPlayer ] + xTemp;
                            break;
                    }
                    #endregion

                    #region[ 両手待ち時 ]
                    if( pChip.eNoteState == ENoteState.wait )
                    {
                        x = (NoteOriginX[nPlayer]);
                    }
                    #endregion

                    #region[ HIDSUD & STEALTH ]
                    if( TJAPlayer3.ConfigIni.eSTEALTH[TJAPlayer3.GetActualPlayer(nPlayer)] == Eステルスモード.STEALTH || TJAPlayer3.stage演奏ドラム画面.bCustomDoron)
                    {
                        pChip.bShow = false;
                    }
                    #endregion

                    long __dbt = (long)(SoundManager.PlayTimer.NowTimeMs * TJAPlayer3.ConfigIni.SongPlaybackSpeed);
                    long time = pChip.n発声時刻ms - __dbt;
                    
                    if( pChip.dbSCROLL_Y != 0.0 )
                    {
                        var dbSCROLL = pChip.eScrollMode == EScrollMode.BMSCROLL ? 1.0 : pChip.dbSCROLL;

                        y = NoteOriginY[nPlayer];


                        double _scrollSpeed = pChip.dbSCROLL_Y * (this.act譜面スクロール速度.db現在の譜面スクロール速度[nPlayer] + 1.0) / 10.0;
                        float play_bpm_time = this.GetNowPBMTime(dTX, 0);

                        y += NotesManager.GetNoteY(pChip, time * pChip.dbBPM, _scrollSpeed, TJAPlayer3.Skin.Game_Notes_Interval, play_bpm_time, pChip.eScrollMode, false);
                    }

                    if (bSplitLane[nPlayer] || TJAPlayer3.Tx.Puchichara[PuchiChara.tGetPuchiCharaIndexByName(TJAPlayer3.GetActualPlayer(nPlayer))].effect.SplitLane)
                    {
                        if (NotesManager.IsDonNote(pChip))
                        {
                            y -= TJAPlayer3.Skin.Game_Notes_Size[1] / 3;
                        }
                        else if (NotesManager.IsKaNote(pChip))
                        {
                            y += TJAPlayer3.Skin.Game_Notes_Size[1] / 3;
                        }
                    }

                    if ( time < 0 )
                    {
                        this.actGame.st叩ききりまショー.b最初のチップが叩かれた = true;
                    }

                    if(x > 0 - TJAPlayer3.Skin.Game_Notes_Size[0] && x < TJAPlayer3.Skin.Resolution[0])
                    {
                        if( TJAPlayer3.Tx.Notes[(int)_gt] != null )
                        {
                            //int num9 = this.actCombo.n現在のコンボ数.Drums >= 50 ? this.ctチップ模様アニメ.Drums.n現在の値 * 130 : 0;
                            int num9 = 0;
                            if (TJAPlayer3.Skin.Game_Notes_Anime)
                            {
                                if (this.actCombo.n現在のコンボ数[nPlayer] >= 300 && ctChipAnimeLag[nPlayer].IsEnded)
                                {
                                    //num9 = ctChipAnime[nPlayer].n現在の値 != 0 ? 260 : 0;
                                    if ((int)ctChipAnime[nPlayer].CurrentValue == 1 || (int)ctChipAnime[nPlayer].CurrentValue == 3)
                                    {
                                        num9 = TJAPlayer3.Skin.Game_Notes_Size[1] * 2;
                                    }
                                    else
                                    {
                                        num9 = 0;
                                    }
                                }
                                else if (this.actCombo.n現在のコンボ数[nPlayer] >= 300 && !ctChipAnimeLag[nPlayer].IsEnded)
                                {
                                    //num9 = base.n現在の音符の顔番号 != 0 ? base.n現在の音符の顔番号 * 130 : 0;
                                    if ((int)ctChipAnime[nPlayer].CurrentValue == 1 || (int)ctChipAnime[nPlayer].CurrentValue == 3)
                                    {
                                        num9 = TJAPlayer3.Skin.Game_Notes_Size[1];
                                    }
                                    else
                                    {
                                        num9 = 0;
                                    }
                                }
                                else if (this.actCombo.n現在のコンボ数[nPlayer] >= 150)
                                {
                                    //num9 = base.n現在の音符の顔番号 != 0 ? base.n現在の音符の顔番号 * 130 : 0;
                                    if ((int)ctChipAnime[nPlayer].CurrentValue == 1 || (int)ctChipAnime[nPlayer].CurrentValue == 3)
                                    {
                                        num9 = TJAPlayer3.Skin.Game_Notes_Size[1];
                                    }
                                    else
                                    {
                                        num9 = 0;
                                    }
                                }
                                else if (this.actCombo.n現在のコンボ数[nPlayer] >= 50 && ctChipAnimeLag[nPlayer].IsEnded)
                                {
                                    //num9 = base.n現在の音符の顔番号 != 0 ? base.n現在の音符の顔番号 * 130 : 0;
                                    if ((int)ctChipAnime[nPlayer].CurrentValue <= 1)
                                    {
                                        num9 = TJAPlayer3.Skin.Game_Notes_Size[1];
                                    }
                                    else
                                    {
                                        num9 = 0;
                                    }
                                }
                                else if (this.actCombo.n現在のコンボ数[nPlayer] >= 50 && !ctChipAnimeLag[nPlayer].IsEnded)
                                {
                                    //num9 = base.n現在の音符の顔番号 != 0 ? base.n現在の音符の顔番号 * 130 : 0;
                                    num9 = 0;
                                }
                                else
                                {
                                    num9 = 0;
                                }
                            }



                            int nSenotesX = 0;
                            int nSenotesY = 0;

                            switch (TJAPlayer3.ConfigIni.nPlayerCount)
                            {
                                case 1:
                                case 2:
                                    nSenotesX = TJAPlayer3.Skin.nSENotesX[nPlayer];
                                    nSenotesY = TJAPlayer3.Skin.nSENotesY[nPlayer];
                                    break;
                                case 3:
                                case 4:
                                    nSenotesX = TJAPlayer3.Skin.nSENotes_4P[0];
                                    nSenotesY = TJAPlayer3.Skin.nSENotes_4P[1];
                                    break;
                                case 5:
                                    nSenotesX = TJAPlayer3.Skin.nSENotes_5P[0];
                                    nSenotesY = TJAPlayer3.Skin.nSENotes_5P[1];
                                    break;
                            }

                            this.ct手つなぎ.TickLoop();
                            float fHand = (this.ct手つなぎ.CurrentValue < 30 ? this.ct手つなぎ.CurrentValue : 60 - this.ct手つなぎ.CurrentValue) / 30.0f;


                            //x = ( x ) - ( ( int ) ( (TJAPlayer3.Skin.Game_Note_Size[0] * pChip.dbチップサイズ倍率 ) / 2.0 ) );

                            //TJAPlayer3.Tx.Notes[(int)_gt].b加算合成 = false;
                            //TJAPlayer3.Tx.SENotes.b加算合成 = false;
                            
                            switch (pChip.nチャンネル番号)
                            {
                                case 0x11:
                                case 0x12:
                                case 0x13:
                                case 0x14:
                                case 0x1C:
                                case 0x101:
                                    {
                                        NotesManager.DisplayNote(nPlayer, x, y, pChip, num9);
                                        NotesManager.DisplaySENotes(nPlayer, x + nSenotesX, y + nSenotesY, pChip);
                                        
                                        //TJAPlayer3.Tx.SENotes[(int)_gt]?.t2D描画(device, x - 2, y + nSenotesY, new Rectangle(0, 30 * pChip.nSenote, 136, 30));
                                        break;
                                    }

                                case 0x1A:
                                case 0x1B:
                                    {
                                        int moveX = (int)(fHand * TJAPlayer3.Skin.Game_Notes_Arm_Move[0]);
                                        int moveY = (int)(fHand * TJAPlayer3.Skin.Game_Notes_Arm_Move[1]);
                                        if (TJAPlayer3.ConfigIni.eSTEALTH[TJAPlayer3.GetActualPlayer(nPlayer)] == Eステルスモード.OFF && pChip.bShow)
                                        {
                                            if (nPlayer != TJAPlayer3.ConfigIni.nPlayerCount - 1)
                                            {
                                                //上から下
                                                TJAPlayer3.Tx.Notes_Arm?.t2D上下反転描画(
                                                    x + TJAPlayer3.Skin.Game_Notes_Arm_Offset_Left_X[0] + moveX, 
                                                    y + TJAPlayer3.Skin.Game_Notes_Arm_Offset_Left_Y[0] + moveY);
                                                TJAPlayer3.Tx.Notes_Arm?.t2D上下反転描画(
                                                    x + TJAPlayer3.Skin.Game_Notes_Arm_Offset_Right_X[0] - moveX, 
                                                    y + TJAPlayer3.Skin.Game_Notes_Arm_Offset_Right_Y[0] - moveY);
                                            }
                                            if (nPlayer != 0)
                                            {
                                                //下から上
                                                TJAPlayer3.Tx.Notes_Arm?.t2D描画( 
                                                    x + TJAPlayer3.Skin.Game_Notes_Arm_Offset_Left_X[1] + moveX, 
                                                    y + TJAPlayer3.Skin.Game_Notes_Arm_Offset_Left_Y[1] + moveY);
                                                TJAPlayer3.Tx.Notes_Arm?.t2D描画(
                                                    x + TJAPlayer3.Skin.Game_Notes_Arm_Offset_Right_X[1] - moveX, 
                                                    y + TJAPlayer3.Skin.Game_Notes_Arm_Offset_Right_Y[1] - moveY);
                                            }
                                            NotesManager.DisplayNote(nPlayer, x, y, pChip, num9);
                                            NotesManager.DisplaySENotes(nPlayer, x + nSenotesX, y + nSenotesY, pChip);
                                        }
                                        break;
                                    }

                                case 0x1F:
                                    {
                                        NotesManager.DisplayNote(nPlayer, x, y, pChip, num9);
                                    }
                                    break;
                                default:
                                    { 
                                    }
                                    break;

                            }
                            //CDTXMania.act文字コンソール.tPrint( x + 60, y + 160, C文字コンソール.Eフォント種別.白, pChip.nPlayerSide.ToString() );
                        }
                    }
                }
            }
            else
            {
                return;
            }
            #endregion
        }
		protected override void t進行描画_チップ_Taiko連打( CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip, int nPlayer )
        {
            int nSenotesX = 0;
            int nSenotesY = 0;
            long nowTime = (long)(SoundManager.PlayTimer.NowTimeMs * TJAPlayer3.ConfigIni.SongPlaybackSpeed);

            switch (TJAPlayer3.ConfigIni.nPlayerCount)
            {
                case 1:
                case 2:
                    nSenotesX = TJAPlayer3.Skin.nSENotesX[nPlayer];
                    nSenotesY = TJAPlayer3.Skin.nSENotesY[nPlayer];
                    break;
                case 3:
                case 4:
                    nSenotesX = TJAPlayer3.Skin.nSENotes_4P[0];
                    nSenotesY = TJAPlayer3.Skin.nSENotes_4P[1];
                    break;
                case 5:
                    nSenotesX = TJAPlayer3.Skin.nSENotes_5P[0];
                    nSenotesY = TJAPlayer3.Skin.nSENotes_5P[1];
                    break;
            }

            int nノート座標 = pChip.nバーからの距離dot.Taiko;
            int nノート末端座標 = pChip.nバーからのノーツ末端距離dot;
            int nノート末端座標_Y = pChip.nバーからのノーツ末端距離dot_Y;
            int n先頭発声位置 = 0;

            EGameType _gt = TJAPlayer3.ConfigIni.nGameType[TJAPlayer3.GetActualPlayer(nPlayer)];

            // 2016.11.2 kairera0467
            // 黄連打音符を赤くするやつの実装方法メモ
            //前面を黄色、背面を変色後にしたものを重ねて、打数に応じて前面の透明度を操作すれば、色を操作できるはず。
            //ただしテクスチャのαチャンネル部分が太くなるなどのデメリットが出る。備えよう。

            #region[ 作り直したもの ]
            if (pChip.b可視)
            {
                if (NotesManager.IsGenericRoll(pChip))
                {
                    if (pChip.nノーツ出現時刻ms != 0 && (nowTime < pChip.n発声時刻ms - pChip.nノーツ出現時刻ms))
                        pChip.bShow = false;
                    else if (pChip.nノーツ出現時刻ms != 0 && pChip.nノーツ移動開始時刻ms != 0)
                        pChip.bShow = true;
                }
                if (NotesManager.IsRollEnd(pChip))
                {
                    if (pChip.nノーツ出現時刻ms != 0 && (nowTime < n先頭発声位置 - pChip.nノーツ出現時刻ms))
                        pChip.bShow = false;
                    else
                        pChip.bShow = true;

                    CDTX.CChip cChip = null;
                    if (pChip.nノーツ移動開始時刻ms != 0) // n先頭発声位置 value is only used when this condition is met
                    {
                        cChip = TJAPlayer3.stage演奏ドラム画面.r指定時刻に一番近い連打Chip_ヒット未済問わず不可視考慮(pChip.n発声時刻ms, 0x10 + pChip.n連打音符State, 0, nPlayer);
                        if (cChip != null)
                        {
                            n先頭発声位置 = cChip.n発声時刻ms;
                        }
                    }
                }

                int x = NoteOriginX[nPlayer] + nノート座標;
                int x末端 = NoteOriginX[nPlayer] + nノート末端座標;
                int y末端 = NoteOriginY[nPlayer] + nノート末端座標_Y;
                int y = NoteOriginY[nPlayer];

                if (pChip.dbSCROLL_Y != 0.0)
                {
                    double _scrollSpeed = pChip.dbSCROLL_Y * (this.act譜面スクロール速度.db現在の譜面スクロール速度[nPlayer] + 1.0) / 10.0;
                    long __dbt = nowTime;
                    long time = pChip.n発声時刻ms - __dbt;
                    float play_bpm_time = this.GetNowPBMTime(dTX, 0);
                    y += NotesManager.GetNoteY(pChip, time * pChip.dbBPM, _scrollSpeed, TJAPlayer3.Skin.Game_Notes_Interval, play_bpm_time, pChip.eScrollMode, false);
                }

                if (bSplitLane[nPlayer] || TJAPlayer3.Tx.Puchichara[PuchiChara.tGetPuchiCharaIndexByName(TJAPlayer3.GetActualPlayer(nPlayer))].effect.SplitLane)
                {
                    if (TJAPlayer3.ConfigIni.nGameType[nPlayer] == EGameType.KONGA)
                    {
                        if (NotesManager.IsClapRoll(pChip))
                        {
                        }
                        else if (NotesManager.IsYellowRoll(pChip))
                        {
                            y += TJAPlayer3.Skin.Game_Notes_Size[1] / 2;
                            y末端 += TJAPlayer3.Skin.Game_Notes_Size[1] / 2;
                        }
                        else if (NotesManager.IsRoll(pChip))
                        {
                            y -= TJAPlayer3.Skin.Game_Notes_Size[1] / 2;
                            y末端 -= TJAPlayer3.Skin.Game_Notes_Size[1] / 2;
                        }
                    }
                }

                #region[ HIDSUD & STEALTH ]

                if (TJAPlayer3.ConfigIni.eSTEALTH[TJAPlayer3.GetActualPlayer(nPlayer)] == Eステルスモード.STEALTH || TJAPlayer3.stage演奏ドラム画面.bCustomDoron)
                {
                    pChip.bShow = false;
                }

                #endregion

                //if( CDTXMania.ConfigIni.eScrollMode != EScrollMode.Normal )
                //x -= 10;

                //if(x末端 > 0 - TJAPlayer3.Skin.Game_Notes_Size[0] && x < TJAPlayer3.Skin.Resolution[0])
                if((Math.Min(x, x末端) < TJAPlayer3.Skin.Resolution[0] && Math.Max(x, x末端) > 0 - TJAPlayer3.Skin.Game_Notes_Size[0]))
                {
                    if (TJAPlayer3.Tx.Notes[(int)_gt] != null)
                    {
                        //int num9 = this.actCombo.n現在のコンボ数.Drums >= 50 ? this.ctチップ模様アニメ.Drums.n現在の値 * 130 : 0;
                        //int num9 = this.actCombo.n現在のコンボ数.Drums >= 50 ? base.n現在の音符の顔番号 * 130 : 0;
                        int num9 = 0;
                        //if( this.actCombo.n現在のコンボ数[ nPlayer ] >= 300 )
                        //{
                        //    num9 = base.n現在の音符の顔番号 != 0 ? 260 : 0;
                        //}
                        //else if( this.actCombo.n現在のコンボ数[ nPlayer ] >= 50 )
                        //{
                        //    num9 = base.n現在の音符の顔番号 != 0 ? base.n現在の音符の顔番号 * 130 : 0;
                        //}
                        if (TJAPlayer3.Skin.Game_Notes_Anime)
                        {
                            if (this.actCombo.n現在のコンボ数[nPlayer] >= 300 && ctChipAnimeLag[nPlayer].IsEnded)
                            {
                                //num9 = ctChipAnime[nPlayer].db現在の値 != 0 ? 260 : 0;
                                if ((int)ctChipAnime[nPlayer].CurrentValue == 1 || (int)ctChipAnime[nPlayer].CurrentValue == 3)
                                {
                                    num9 = TJAPlayer3.Skin.Game_Notes_Size[1] * 2;
                                }
                                else
                                {
                                    num9 = 0;
                                }
                            }
                            else if (this.actCombo.n現在のコンボ数[nPlayer] >= 300 && !ctChipAnimeLag[nPlayer].IsEnded)
                            {
                                //num9 = base.n現在の音符の顔番号 != 0 ? base.n現在の音符の顔番号 * 130 : 0;
                                if ((int)ctChipAnime[nPlayer].CurrentValue == 1 || (int)ctChipAnime[nPlayer].CurrentValue == 3)
                                {
                                    num9 = TJAPlayer3.Skin.Game_Notes_Size[1];
                                }
                                else
                                {
                                    num9 = 0;
                                }
                            }
                            else if (this.actCombo.n現在のコンボ数[nPlayer] >= 150)
                            {
                                //num9 = base.n現在の音符の顔番号 != 0 ? base.n現在の音符の顔番号 * 130 : 0;
                                if ((int)ctChipAnime[nPlayer].CurrentValue == 1 || (int)ctChipAnime[nPlayer].CurrentValue == 3)
                                {
                                    num9 = TJAPlayer3.Skin.Game_Notes_Size[1];
                                }
                                else
                                {
                                    num9 = 0;
                                }
                            }
                            else if (this.actCombo.n現在のコンボ数[nPlayer] >= 50 && ctChipAnimeLag[nPlayer].IsEnded)
                            {
                                //num9 = base.n現在の音符の顔番号 != 0 ? base.n現在の音符の顔番号 * 130 : 0;
                                if ((int)ctChipAnime[nPlayer].CurrentValue <= 1)
                                {
                                    num9 = TJAPlayer3.Skin.Game_Notes_Size[1];
                                }
                                else
                                {
                                    num9 = 0;
                                }
                            }
                            else if (this.actCombo.n現在のコンボ数[nPlayer] >= 50 && !ctChipAnimeLag[nPlayer].IsEnded)
                            {
                                //num9 = base.n現在の音符の顔番号 != 0 ? base.n現在の音符の顔番号 * 130 : 0;
                                num9 = 0;
                            }
                            else
                            {
                                num9 = 0;
                            }
                        }

                        //kairera0467氏 の TJAPlayer2forPC のコードを参考にし、打数に応じて色を変える(打数の変更以外はほとんどそのまんま) ろみゅ～？ 2018/8/20
                        pChip.RollInputTime?.Tick();
                        pChip.RollDelay?.Tick();

                        if (pChip.RollInputTime != null && pChip.RollInputTime.IsEnded)
                        {
                            pChip.RollInputTime.Stop();
                            pChip.RollInputTime.CurrentValue = 0;
                            pChip.RollDelay = new CCounter(0, 1, 1, TJAPlayer3.Timer);
                        }

                        if (pChip.RollDelay != null && pChip.RollDelay.IsEnded && pChip.RollEffectLevel > 0)
                        {
                            pChip.RollEffectLevel--;
                            pChip.RollDelay = new CCounter(0, 1, 1, TJAPlayer3.Timer);
                            pChip.RollDelay.CurrentValue = 0;
                        }

                        float f減少するカラー = 1.0f - ((0.95f / 100) * pChip.RollEffectLevel);
                        var effectedColor = new Color4(1.0f, f減少するカラー, f減少するカラー, 1f);
                        var normalColor = new Color4(1.0f, 1.0f, 1.0f, 1f);
                        float f末端ノーツのテクスチャ位置調整 = 65f;

                        //136, 30
                        var _size = TJAPlayer3.Skin.Game_SENote_Size;
                        int _60_cut = 60 * _size[0] / 136;
                        int _58_cut = 58 * _size[0] / 136;
                        int _78_cut = 78 * _size[0] / 136;

                        if (NotesManager.IsRoll(pChip) || NotesManager.IsFuzeRoll(pChip))
                        {
                            if (NotesManager.IsFuzeRoll(pChip)
                                && nowTime >= pChip.n発声時刻ms && nowTime < pChip.nノーツ終了時刻ms)
                            {
                                x = NoteOriginX[nPlayer];
                                y = NoteOriginY[nPlayer];
                            }
                            

                            NotesManager.DisplayRoll(nPlayer, x, y, pChip, num9, normalColor, effectedColor, x末端, y末端);

                            if (TJAPlayer3.Tx.SENotes[(int)_gt] != null)
                            {
                                int _shift = NotesManager.IsBigRoll(pChip) ? 26 : 0;

                                if (!NotesManager.IsFuzeRoll(pChip))
                                {
                                    TJAPlayer3.Tx.SENotes[(int)_gt].vc拡大縮小倍率.X = x末端 - x - 44 - _shift;
                                    TJAPlayer3.Tx.SENotes[(int)_gt].t2D描画(x + 90 + _shift, y + nSenotesY, new Rectangle(_60_cut, 8 * _size[1], 1, _size[1]));
                                    TJAPlayer3.Tx.SENotes[(int)_gt].vc拡大縮小倍率.X = 1.0f;
                                    TJAPlayer3.Tx.SENotes[(int)_gt].t2D描画(x + 30 + _shift, y + nSenotesY, new Rectangle(0, 8 * _size[1], _60_cut, _size[1]));
                                    TJAPlayer3.Tx.SENotes[(int)_gt].t2D描画(x - (_shift / 13), y + nSenotesY, new Rectangle(0, _size[1] * pChip.nSenote, _size[0], _size[1]));
                                }
                                else
                                {
                                    NotesManager.DisplaySENotes(nPlayer, x + nSenotesX, y + nSenotesY, pChip);
                                }
                                
                            }
                            
                        }

                        if (NotesManager.IsBalloon(pChip) || NotesManager.IsKusudama(pChip))
                        {
                            if (pChip.bShow)
                            {
                                if (nowTime >= pChip.n発声時刻ms && nowTime < pChip.nノーツ終了時刻ms)
                                    x = NoteOriginX[nPlayer];
                                else if (nowTime >= pChip.nノーツ終了時刻ms)
                                    x = (NoteOriginX[nPlayer] + pChip.nバーからのノーツ末端距離dot);

                                NotesManager.DisplayNote(nPlayer, x, y, pChip, num9, TJAPlayer3.Skin.Game_Notes_Size[0] * 2);
                                NotesManager.DisplaySENotes(nPlayer, x + nSenotesX, y + nSenotesY, pChip);

                                /*
                                if (TJAPlayer3.ConfigIni.eSTEALTH != Eステルスモード.DORON)
                                    TJAPlayer3.Tx.Notes[(int)_gt].t2D描画(x, y, new Rectangle(1430, num9, 260, 130));
                                */

                                //TJAPlayer3.Tx.SENotes.t2D描画(x - 2, y + nSenotesY, new Rectangle(0, 30 * pChip.nSenote, 136, 30));
                            }
                        }
                        if (NotesManager.IsRollEnd(pChip))
                        {
                            //大きい連打か小さい連打かの区別方法を考えてなかったよちくしょう
                            if (TJAPlayer3.Tx.Notes[(int)_gt] != null)
                                TJAPlayer3.Tx.Notes[(int)_gt].vc拡大縮小倍率.X = 1.0f;
                            int n = 0;
                            switch (pChip.n連打音符State)
                            {
                                case 5:
                                    n = 910;
                                    break;
                                case 6:
                                    n = 1300;
                                    break;
                                default:
                                    n = 910;
                                    break;
                            }
                            if (pChip.n連打音符State != 7 && pChip.n連打音符State != 9 && pChip.n連打音符State != 13)
                            {
                                //if( CDTXMania.ConfigIni.eSTEALTH != Eステルスモード.DORON )
                                //    CDTXMania.Tx.Notes.t2D描画( CDTXMania.app.Device, x, y, new Rectangle( n, num9, 130, 130 ) );//大音符:1170
                                TJAPlayer3.Tx.SENotes[(int)_gt]?.t2D描画(x + 56, y + nSenotesY, new Rectangle(_58_cut, 9 * _size[1], _78_cut, _size[1]));
                            }

                        }
                    }
                }

                if (pChip.n発声時刻ms < nowTime && pChip.nノーツ終了時刻ms > nowTime)
                {
                    var puchichara = TJAPlayer3.Tx.Puchichara[PuchiChara.tGetPuchiCharaIndexByName(TJAPlayer3.GetActualPlayer(nPlayer))];

                    //時間内でかつ0x9Aじゃないならならヒット処理
                    if (!NotesManager.IsRollEnd(pChip) &&
                        ((nPlayer != 1 ? TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[nPlayer] : 
                        (TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[nPlayer] || TJAPlayer3.ConfigIni.bAIBattleMode)) || 
                        puchichara.effect.Autoroll > 0))
                        this.tチップのヒット処理(pChip.n発声時刻ms, pChip, E楽器パート.TAIKO, false, 0, nPlayer, puchichara.effect.Autoroll > 0);
                }
            }
            #endregion
        }

		protected override void t進行描画_チップ_ドラムス( CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip )
		{
		}
        protected override void t進行描画_チップ本体_ドラムス( CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip )
		{
		}
		protected override void t進行描画_チップ_フィルイン( CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip )
		{

		}
		protected override void t進行描画_チップ_小節線( CConfigIni configIni, ref CDTX dTX, ref CDTX.CChip pChip, int nPlayer )
		{
            if( pChip.nコース != this.n現在のコース[ nPlayer ] )
                return;

			//int n小節番号plus1 = pChip.n発声位置 / 384;
            //int n小節番号plus1 = this.actPlayInfo.NowMeasure[nPlayer];
            int x = NoteOriginX[ nPlayer ] + pChip.nバーからの距離dot.Taiko;
            int y = NoteOriginY[ nPlayer ];

            if( pChip.dbSCROLL_Y != 0.0 )
            {
                double _scrollSpeed = pChip.dbSCROLL_Y * (this.act譜面スクロール速度.db現在の譜面スクロール速度[nPlayer] + 1.0) / 10.0;
                long __dbt = (long)(SoundManager.PlayTimer.NowTimeMs * TJAPlayer3.ConfigIni.SongPlaybackSpeed);
                long time = pChip.n発声時刻ms - __dbt;
                float play_bpm_time = this.GetNowPBMTime(dTX, 0);
                y += NotesManager.GetNoteY(pChip, time * pChip.dbBPM, _scrollSpeed, TJAPlayer3.Skin.Game_Notes_Interval, play_bpm_time, pChip.eScrollMode, false);

                //y += (int)(((pChip.n発声時刻ms - (CSound管理.rc演奏用タイマ.n現在時刻 * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0))) * pChip.dbBPM * pChip.dbSCROLL_Y * (this.act譜面スクロール速度.db現在の譜面スクロール速度[nPlayer] + 1.5)) / 628.7);
            }

            if ( !pChip.bHit && pChip.n発声時刻ms > SoundManager.PlayTimer.NowTime )
			{
				//pChip.bHit = true;
				//this.actPlayInfo.n小節番号 = n小節番号plus1 - 1;
                //this.actPlayInfo.n小節番号++;
				if ( configIni.bWave再生位置自動調整機能有効 && ( bIsDirectSound || bUseOSTimer ) )
				{
					dTX.tWave再生位置自動補正();
				}
			}
            /*
            if (configIni.b演奏情報を表示する || TJAPlayer3.ConfigIni.bTokkunMode)
            {
                var nowMeasure = pChip.n整数値_内部番号;
                if ( x >= 310 )
                {
				    TJAPlayer3.act文字コンソール.tPrint(x + 8, y - 26, C文字コンソール.Eフォント種別.白, nowMeasure.ToString());
                }
			}
            */
			if ( (pChip.b可視 && !pChip.bHideBarLine) && (TJAPlayer3.Tx.Bar != null ) )
			{
                if( x >= 0 && x <= SampleFramework.GameWindowSize.Width )
                {
                    if( pChip.bBranch )
                    {
                        //this.tx小節線_branch.t2D描画( CDTXMania.app.Device, x - 3, y, new Rectangle( 0, 0, 3, 130 ) );
                        TJAPlayer3.Tx.Bar_Branch?.t2D描画( x + ((TJAPlayer3.Skin.Game_Notes_Size[0] - TJAPlayer3.Tx.Bar_Branch.szテクスチャサイズ.Width) / 2), y, new Rectangle( 0, 0, TJAPlayer3.Tx.Bar_Branch.szテクスチャサイズ.Width, TJAPlayer3.Skin.Game_Notes_Size[1]) );
                    }
                    else
                    {
                        //this.tx小節線.t2D描画( CDTXMania.app.Device, x - 3, y, new Rectangle( 0, 0, 3, 130 ) );
                        TJAPlayer3.Tx.Bar?.t2D描画( x + ((TJAPlayer3.Skin.Game_Notes_Size[0] - TJAPlayer3.Tx.Bar.szテクスチャサイズ.Width) / 2), y, new Rectangle( 0, 0, TJAPlayer3.Tx.Bar.szテクスチャサイズ.Width, TJAPlayer3.Skin.Game_Notes_Size[1]) );
                    }
                }
			}
		}

        protected void t進行描画_レーン()
        {
            this.actLane.Draw();
        }

        /// <summary>
        /// 全体にわたる制御をする。
        /// </summary>
        public void t全体制御メソッド()
        {
            int t = (int)SoundManager.PlayTimer.NowTimeMs;
            //CDTXMania.act文字コンソール.tPrint( 0, 16, C文字コンソール.Eフォント種別.白, t.ToString() );

            this.actBalloon.tDrawKusudama();

            for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                var chkChip = this.chip現在処理中の連打チップ[i];
                if (chkChip != null)
                {
                    long nowTime = (long)(SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed);
                    //int n = this.chip現在処理中の連打チップ[i].nチャンネル番号;
                    if ((NotesManager.IsGenericBalloon(chkChip) || NotesManager.IsKusudama(chkChip)) && (this.b連打中[i] == true))
                    {
                        //if (this.chip現在処理中の連打チップ.n発声時刻ms <= (int)CSound管理.rc演奏用タイマ.n現在時刻ms && this.chip現在処理中の連打チップ.nノーツ終了時刻ms >= (int)CSound管理.rc演奏用タイマ.n現在時刻ms)
                        if (chkChip.n発声時刻ms <= (int)nowTime
                            && chkChip.nノーツ終了時刻ms + 500 >= (int)nowTime)
                        {
                            var balloon = NotesManager.IsKusudama(chkChip) ? nCurrentKusudamaCount : chkChip.nBalloon;
                            if (!NotesManager.IsFuzeRoll(chkChip)) chkChip.bShow = false;
                            this.actBalloon.On進行描画(
                                balloon, 
                                this.n風船残り[i], 
                                i,
                                NotesManager.IsFuzeRoll(chkChip)
                                 ? CAct演奏Drums風船.EBalloonType.FUSEROLL
                                 : NotesManager.IsKusudama(chkChip)
                                    ? CAct演奏Drums風船.EBalloonType.KUSUDAMA
                                    : CAct演奏Drums風船.EBalloonType.BALLOON
                                );
                        }
                        else
                        {
                            this.n現在の連打数[i] = 0;
                        }

                    }
                }
            }
            #region [ Treat big notes hit with a single hand ]
            //常時イベントが発生しているメソッドのほうがいいんじゃないかという予想。
            //CDTX.CChip chipNoHit = this.r指定時刻に一番近い未ヒットChip((int)CSound管理.rc演奏用タイマ.n現在時刻ms, 0);
            for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
            {
                CDTX.CChip chipNoHit = r指定時刻に一番近い未ヒットChipを過去方向優先で検索する((long)(SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed), i);

                EGameType _gt = TJAPlayer3.ConfigIni.nGameType[TJAPlayer3.GetActualPlayer(i)];
                bool _isBigKaTaiko = NotesManager.IsBigKaTaiko(chipNoHit, _gt);
                bool _isBigDonTaiko = NotesManager.IsBigDonTaiko(chipNoHit, _gt);
                bool _isSwapNote = NotesManager.IsSwapNote(chipNoHit, _gt);

                if (chipNoHit != null && (_isBigDonTaiko || _isBigKaTaiko))
                {
                    float timeC = chipNoHit.n発声時刻ms - (float)(SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed);
                    int nWaitTime = TJAPlayer3.ConfigIni.n両手判定の待ち時間;
                    if (chipNoHit.eNoteState == ENoteState.wait && timeC <= 110 
                        && chipNoHit.nProcessTime + nWaitTime <= (int)(SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed))
                    {
                        if (!_isSwapNote)
                        {
                            this.tドラムヒット処理(chipNoHit.nProcessTime, Eパッド.RRed, chipNoHit, false, i);
                            //this.nWaitButton = 0;
                            this.nStoredHit[i] = 0;
                            chipNoHit.bHit = true;
                            chipNoHit.IsHitted = true;
                        }
                        

                        chipNoHit.eNoteState = ENoteState.none;
                    }
                }
            }

            #endregion

            //string strNull = "Found";

            if (TJAPlayer3.Input管理.Keyboard.KeyPressed((int)SlimDXKeys.Key.F1))
            {
                if (!this.actPauseMenu.bIsActivePopupMenu && this.bPAUSE == false)
                {
                    TJAPlayer3.Skin.sound変更音.t再生する();

                    SoundManager.PlayTimer.Pause();
                    TJAPlayer3.Timer.Pause();
                    TJAPlayer3.DTX.t全チップの再生一時停止();
                    this.actAVI.tPauseControl();

                    this.bPAUSE = true;
                    this.actPauseMenu.tActivatePopupMenu(0);
                }

            }

        }

        private void t進行描画_リアルタイム判定数表示()
        {
            var showJudgeInfo = false;

            if (TJAPlayer3.ConfigIni.nPlayerCount == 1 ? (TJAPlayer3.ConfigIni.bJudgeCountDisplay && !TJAPlayer3.ConfigIni.b太鼓パートAutoPlay[0]) : false) showJudgeInfo = true;
            if (TJAPlayer3.ConfigIni.bTokkunMode) showJudgeInfo = true;

            if (showJudgeInfo)
            {
                //ボードの横幅は333px
                //数字フォントの小さいほうはリザルトのものと同じ。
                if( TJAPlayer3.Tx.Judge_Meter != null )
                    TJAPlayer3.Tx.Judge_Meter.t2D描画( TJAPlayer3.Skin.Game_Judge_Meter[0], TJAPlayer3.Skin.Game_Judge_Meter[1]);

                this.t小文字表示(TJAPlayer3.Skin.Game_Judge_Meter_Perfect[0], TJAPlayer3.Skin.Game_Judge_Meter_Perfect[1], this.nヒット数_Auto含まない.Drums.Perfect, false, false);
                this.t小文字表示(TJAPlayer3.Skin.Game_Judge_Meter_Good[0], TJAPlayer3.Skin.Game_Judge_Meter_Good[1], this.nヒット数_Auto含まない.Drums.Great, false, false);
                this.t小文字表示(TJAPlayer3.Skin.Game_Judge_Meter_Miss[0], TJAPlayer3.Skin.Game_Judge_Meter_Miss[1], this.nヒット数_Auto含まない.Drums.Miss, false, false);
                this.t小文字表示(TJAPlayer3.Skin.Game_Judge_Meter_Roll[0], TJAPlayer3.Skin.Game_Judge_Meter_Roll[1], GetRoll(0), false, false);

                int nNowTotal = this.nヒット数_Auto含まない.Drums.Perfect + this.nヒット数_Auto含まない.Drums.Great + this.nヒット数_Auto含まない.Drums.Miss;
                double dbたたけた率 = Math.Round((100.0 * ( TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含まない.Drums.Perfect + TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含まない.Drums.Great)) / (double)nNowTotal);
                double dbPERFECT率 = Math.Round((100.0 * TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含まない.Drums.Perfect) / (double)nNowTotal);
                double dbGREAT率 = Math.Round((100.0 * TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含まない.Drums.Great / (double)nNowTotal));
                double dbMISS率 = Math.Round((100.0 * TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含まない.Drums.Miss / (double)nNowTotal));

                if (double.IsNaN(dbたたけた率))
                    dbたたけた率 = 0;
                if (double.IsNaN(dbPERFECT率))
                    dbPERFECT率 = 0;
                if (double.IsNaN(dbGREAT率))
                    dbGREAT率 = 0;
                if (double.IsNaN(dbMISS率))
                    dbMISS率 = 0;

                this.t大文字表示(TJAPlayer3.Skin.Game_Judge_Meter_HitRate[0], TJAPlayer3.Skin.Game_Judge_Meter_HitRate[1], (int)dbたたけた率);
                this.t小文字表示(TJAPlayer3.Skin.Game_Judge_Meter_PerfectRate[0], TJAPlayer3.Skin.Game_Judge_Meter_PerfectRate[1], (int)dbPERFECT率, false, true);
                this.t小文字表示(TJAPlayer3.Skin.Game_Judge_Meter_GoodRate[0], TJAPlayer3.Skin.Game_Judge_Meter_GoodRate[1], (int)dbGREAT率, false, true);
                this.t小文字表示(TJAPlayer3.Skin.Game_Judge_Meter_MissRate[0], TJAPlayer3.Skin.Game_Judge_Meter_MissRate[1], (int)dbMISS率, false, true);
            }
        }

        private void t小文字表示( int x, int y, int num, bool bOrange, bool drawPercent )
        {
            float width = TJAPlayer3.Tx.Result_Number.sz画像サイズ.Width / 11.0f;
            float height = TJAPlayer3.Tx.Result_Number.sz画像サイズ.Height / 2.0f;

            int[] nums = CConversion.SeparateDigits(num);

            if (drawPercent)
            {
                TJAPlayer3.Tx.Result_Number.t2D拡大率考慮中央基準描画(x + (TJAPlayer3.Skin.Result_Number_Interval[0] * 3.0f) + (width / 2),
                    y + (TJAPlayer3.Skin.Result_Number_Interval[1] * 3.0f) + (height / 2),
                    new System.Drawing.RectangleF(width * 10, 0, width, height));
            }

            for (int j = 0; j < nums.Length; j++)
            {
                float offset = j - 1.5f;
                float _x = x - (TJAPlayer3.Skin.Result_Number_Interval[0] * offset);
                float _y = y - (TJAPlayer3.Skin.Result_Number_Interval[1] * offset);

                TJAPlayer3.Tx.Result_Number.t2D拡大率考慮中央基準描画(_x + (width / 2), _y + (height / 2),
                    new System.Drawing.RectangleF(width * nums[j], 0, width, height));
            }
        }

        private void t大文字表示( int x, int y, int num)
        {
            int[] nums = CConversion.SeparateDigits(num);
            for (int j = 0; j < nums.Length; j++)
            {
                float offset = j - 1.5f;
                float _x = x - ((TJAPlayer3.Skin.Result_Number_Interval[0] * 1.27f) * offset);
                float _y = y - ((TJAPlayer3.Skin.Result_Number_Interval[1] * 1.27f) * offset);

                float width = TJAPlayer3.Tx.Result_Number.sz画像サイズ.Width / 11.0f;
                float height = TJAPlayer3.Tx.Result_Number.sz画像サイズ.Height / 2.0f;

                TJAPlayer3.Tx.Result_Number.t2D拡大率考慮中央基準描画(_x + (width / 2), _y + (height / 2),
                    new System.Drawing.RectangleF(width * nums[j], height, width, height));
            }
        }
		#endregion
	}
}
