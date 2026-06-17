using System.Diagnostics;
using FDK;

namespace OpenTaiko;

internal class CStageSongLoading : CStage {
	// When driven by the song_loading transition, the transition draws the whole loading screen (bg, characters,
	// title/plate, bar) — so this stage runs the load only and skips its own visual. Set before Activate().
	internal bool TransitionDriven = false;

	// コンストラクタ

	public CStageSongLoading() {
		base.eStageID = CStage.EStage.SongLoading;
		base.ePhaseID = CStage.EPhase.Common_NORMAL;
		base.IsDeActivated = true;
		//base.list子Activities.Add( this.actFI = new CActFIFOBlack() );	// #27787 2012.3.10 yyagi 曲読み込み画面のフェードインの省略
		//base.list子Activities.Add( this.actFO = new CActFIFOBlack() );
	}


	// CStage 実装

	public override void Activate() {
		Trace.TraceInformation("曲読み込みステージを活性化します。");
		Trace.Indent();
		try {
			CLoadingProgress.Begin();   // bar tracks the streamed game-screen texture upload % (driven in Draw)

			this.strSongTitle = "";
			this.strSTAGEFILE = "";
			this.nBGMPlaybackStartTime = -1;
			this.nBGMTotalPlaybackTimems = 0;
			if (this.sdLoadSound != null) {
				OpenTaiko.SoundManager.tDisposeSound(this.sdLoadSound);
				this.sdLoadSound = null;
			}

			if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] >= 5 || OpenTaiko.ConfigIni.nPlayerCount != 1) {
				OpenTaiko.ConfigIni.bTokkunMode = false;
			}

			string strDTXFilePath = OpenTaiko.SongMount.rChosenScore.FileInfo.FileAbsolutePath;

			var strFolderName = Path.GetDirectoryName(strDTXFilePath) + Path.DirectorySeparatorChar;

			var ChartInfo = OpenTaiko.SongMount.rChosenScore.ChartInfo;
			this.strSongTitle = ChartInfo.Title;
			this.strSubtitle = ChartInfo.strSubtitle;

			float wait = 600f;
			if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
				wait = 1000f;
			else if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower)
				wait = 1200f;

			this.ctWait = new CCounter(0, wait, 5, OpenTaiko.Timer);
			this.ctSongNameDisplay = new CCounter(1, 30, 30, OpenTaiko.Timer);
			try {
				// When performing calibration, inform the player that
				// calibration is about to begin, rather than
				// displaying the song title and subtitle as usual.

				var Title = this.strSongTitle;

				var Subtitle = this.strSubtitle;

				if (TransitionDriven) {
					this.txTitle = null;
					this.txSubtitle = null;
				} else if (!string.IsNullOrEmpty(Title)) {
					//this.txタイトル = new CTexture( CDTXMania.app.Device, image, CDTXMania.TextureFormat );
					//this.txタイトル.vc拡大縮小倍率 = new Vector3( 0.5f, 0.5f, 1f );


					using (var bmpSongTitle = this.pfTITLE.DrawText(Title, OpenTaiko.Skin.SongLoading_Title_ForeColor, OpenTaiko.Skin.SongLoading_Title_BackColor, null, 30)) {
						this.txTitle = new CTexture(bmpSongTitle);
						txTitle.vcScaleRatio.X = OpenTaiko.GetSongNameXScaling(ref txTitle, OpenTaiko.Skin.SongLoading_Title_MaxSize);
					}

					using (var bmpSongSubTitle = this.pfSUBTITLE.DrawText(Subtitle, OpenTaiko.Skin.SongLoading_SubTitle_ForeColor, OpenTaiko.Skin.SongLoading_SubTitle_BackColor, null, 30)) {
						this.txSubtitle = new CTexture(bmpSongSubTitle);
						txSubtitle.vcScaleRatio.X = OpenTaiko.GetSongNameXScaling(ref txSubtitle, OpenTaiko.Skin.SongLoading_SubTitle_MaxSize);
					}
				} else {
					this.txTitle = null;
					this.txSubtitle = null;
				}

				_activateTime = DateTime.Now;
			} catch (CTextureCreateFailedException e) {
				Trace.TraceError(e.ToString());
				Trace.TraceError("テクスチャの生成に失敗しました。({0})", new object[] { this.strSTAGEFILE });
				this.txTitle = null;
				this.txSubtitle = null;
			}

			_activateTime = DateTime.Now;
			base.Activate();
		} finally {
			Trace.TraceInformation("曲読み込みステージの活性化を完了しました。");
			Trace.Unindent();
		}
	}
	public override void DeActivate() {
		Trace.TraceInformation("曲読み込みステージを非活性化します。");
		Trace.Indent();
		try {
			// Cancel any in-flight background tasks so they don't race with state
			// that is about to be torn down (e.g. returning to song select via ESC).
			_loadCts?.Cancel();
			_loadCts?.Dispose();
			_loadCts = null;
			_dtxLoadTask = null;
			_wavLoadTask = null;
			_loadedTjas  = null;

			// On ESC mid-stream the game screen WAS activated (its textures are streaming in), so it must be
			// torn down — OpenTaiko's LoadCanceled handler doesn't do it. Cancel/clear the stream queues FIRST
			// (drops the stub references + disposes pending bitmaps), THEN DeActivate disposes the stub textures.
			if (_streamingActive) {
				CTexture.CancelStreaming();
				OpenTaiko.stageGameScreen.DeActivate();
				_streamingActive = false;
			}

			CLoadingProgress.End();   // clear the loading bar (covers normal completion + ESC cancel)

			OpenTaiko.tTextureRelease(ref this.txTitle);
			//CDTXMania.tテクスチャの解放( ref this.txSongnamePlate );
			OpenTaiko.tTextureRelease(ref this.txSubtitle);
			base.DeActivate();
		} finally {
			Trace.TraceInformation("曲読み込みステージの非活性化を完了しました。");
			Trace.Unindent();
		}
	}
	public override void CreateManagedResource() {
		this.pfTITLE = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.SongLoading_Title_FontSize);
		this.pfSUBTITLE = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.SongLoading_SubTitle_FontSize);
		pfDanTitle = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.Game_DanC_Title_Size);
		pfDanSubTitle = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.Game_DanC_SubTitle_Size);

		//this.txSongnamePlate = CDTXMania.tテクスチャの生成( CSkin.Path( @$"Graphics{Path.DirectorySeparatorChar}6_SongnamePlate.png" ) );
		base.CreateManagedResource();
	}
	public override void ReleaseManagedResource() {
		OpenTaiko.tDisposeSafely(ref this.pfTITLE);
		OpenTaiko.tDisposeSafely(ref this.pfSUBTITLE);

		pfDanTitle?.Dispose();
		pfDanSubTitle?.Dispose();

		base.ReleaseManagedResource();
	}
	public override int Draw() {
		string str;

		if (base.IsDeActivated)
			return 0;

		#region [ 初めての進行描画 ]
		//-----------------------------
		if (base.IsFirstDraw) {
			CScore cScore1 = OpenTaiko.SongMount.rChosenScore;
			if (this.sdLoadSound != null) {
				if (OpenTaiko.Skin.soundSongLoadStartSound.bExclusive && (CSkin.CSystemSound.rLastPlaybackExclusiveSystemSound != null)) {
					CSkin.CSystemSound.rLastPlaybackExclusiveSystemSound.tStop();
				}
				this.sdLoadSound.PlayStart();
				this.nBGMPlaybackStartTime = SoundManager.PlayTimer.NowTimeMs;
				this.nBGMTotalPlaybackTimems = this.sdLoadSound.TotalPlayTime;
			} else {
				OpenTaiko.Skin.soundSongLoadStartSound.tPlay();
				this.nBGMPlaybackStartTime = SoundManager.PlayTimer.NowTimeMs;
				this.nBGMTotalPlaybackTimems = (long)Math.Ceiling(OpenTaiko.Skin.soundSongLoadStartSound.nLength_CurrentSound);
			}
			//this.actFI.tフェードイン開始();							// #27787 2012.3.10 yyagi 曲読み込み画面のフェードインの省略
			base.ePhaseID = CStage.EPhase.Common_FADEIN;
			base.IsFirstDraw = false;
		}
		//-----------------------------
		#endregion
		this.ctWait.Tick();

		#region [ Cancel loading with esc ]
		if (tKeyInput()) {
			if (this.sdLoadSound != null) {
				this.sdLoadSound.tStopSound();
				this.sdLoadSound.tDispose();
			}
			OpenTaiko.stageGameScreen.bPreviousPlayWasEndedNormally = false;
			return (int)ESongLoadingScreenReturnValue.LoadCanceled;
		}
		#endregion

		bool isDan   = OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan;
		bool isAI    = OpenTaiko.ConfigIni.bAIBattleMode;

		void drawPlate() {
			if (this.txTitle != null) {
				int nSubtitleCorrection = string.IsNullOrEmpty(OpenTaiko.SongMount.rChosenScore.ChartInfo.strSubtitle) ? 15 : 0;
				this.txTitle.Opacity = 255;
				if (OpenTaiko.Skin.SongLoading_Title_ReferencePoint == CSkin.ReferencePoint.Left) {
					this.txTitle.t2DDraw(OpenTaiko.Skin.SongLoading_Title_X, OpenTaiko.Skin.SongLoading_Title_Y - (this.txTitle.szImageSize.Height / 2) + nSubtitleCorrection);
				} else if (OpenTaiko.Skin.SongLoading_Title_ReferencePoint == CSkin.ReferencePoint.Right) {
					this.txTitle.t2DDraw(OpenTaiko.Skin.SongLoading_Title_X - (this.txTitle.szImageSize.Width * txTitle.vcScaleRatio.X), OpenTaiko.Skin.SongLoading_Title_Y - (this.txTitle.szImageSize.Height / 2) + nSubtitleCorrection);
				} else {
					this.txTitle.t2DDraw((OpenTaiko.Skin.SongLoading_Title_X - ((this.txTitle.szImageSize.Width * txTitle.vcScaleRatio.X) / 2)), OpenTaiko.Skin.SongLoading_Title_Y - (this.txTitle.szImageSize.Height / 2) + nSubtitleCorrection);
				}
			}
			if (this.txSubtitle != null) {
				this.txSubtitle.Opacity = 255;
				if (OpenTaiko.Skin.SongLoading_SubTitle_ReferencePoint == CSkin.ReferencePoint.Left) {
					this.txSubtitle.t2DDraw(OpenTaiko.Skin.SongLoading_SubTitle_X, OpenTaiko.Skin.SongLoading_SubTitle_Y - (this.txSubtitle.szImageSize.Height / 2));
				} else if (OpenTaiko.Skin.SongLoading_Title_ReferencePoint == CSkin.ReferencePoint.Right) {
					this.txSubtitle.t2DDraw(OpenTaiko.Skin.SongLoading_SubTitle_X - (this.txSubtitle.szImageSize.Width * txTitle.vcScaleRatio.X), OpenTaiko.Skin.SongLoading_SubTitle_Y - (this.txSubtitle.szImageSize.Height / 2));
				} else {
					this.txSubtitle.t2DDraw((OpenTaiko.Skin.SongLoading_SubTitle_X - ((this.txSubtitle.szImageSize.Width * txSubtitle.vcScaleRatio.X) / 2)), OpenTaiko.Skin.SongLoading_SubTitle_Y - (this.txSubtitle.szImageSize.Height / 2));
				}
			}
		}

		void drawPlate_AI() {
			if (this.txTitle != null) {
				int nSubtitleCorrection = string.IsNullOrEmpty(OpenTaiko.SongMount.rChosenScore.ChartInfo.strSubtitle) ? 15 : 0;
				this.txTitle.Opacity = 255;
				if (OpenTaiko.Skin.SongLoading_Title_ReferencePoint == CSkin.ReferencePoint.Left) {
					this.txTitle.t2DDraw(OpenTaiko.Skin.SongLoading_Title_X_AI, OpenTaiko.Skin.SongLoading_Title_Y_AI - (this.txTitle.szImageSize.Height / 2) + nSubtitleCorrection);
				} else if (OpenTaiko.Skin.SongLoading_Title_ReferencePoint == CSkin.ReferencePoint.Right) {
					this.txTitle.t2DDraw(OpenTaiko.Skin.SongLoading_Title_X_AI - (this.txTitle.szImageSize.Width * txTitle.vcScaleRatio.X), OpenTaiko.Skin.SongLoading_Title_Y_AI - (this.txTitle.szImageSize.Height / 2) + nSubtitleCorrection);
				} else {
					this.txTitle.t2DDraw((OpenTaiko.Skin.SongLoading_Title_X_AI - ((this.txTitle.szImageSize.Width * txTitle.vcScaleRatio.X) / 2)), OpenTaiko.Skin.SongLoading_Title_Y_AI - (this.txTitle.szImageSize.Height / 2) + nSubtitleCorrection);
				}
			}
			if (this.txSubtitle != null) {
				this.txSubtitle.Opacity = 255;
				if (OpenTaiko.Skin.SongLoading_SubTitle_ReferencePoint == CSkin.ReferencePoint.Left) {
					this.txSubtitle.t2DDraw(OpenTaiko.Skin.SongLoading_SubTitle_X_AI, OpenTaiko.Skin.SongLoading_SubTitle_Y_AI - (this.txSubtitle.szImageSize.Height / 2));
				} else if (OpenTaiko.Skin.SongLoading_Title_ReferencePoint == CSkin.ReferencePoint.Right) {
					this.txSubtitle.t2DDraw(OpenTaiko.Skin.SongLoading_SubTitle_X_AI - (this.txSubtitle.szImageSize.Width * txTitle.vcScaleRatio.X), OpenTaiko.Skin.SongLoading_SubTitle_Y_AI - (this.txSubtitle.szImageSize.Height / 2));
				} else {
					this.txSubtitle.t2DDraw((OpenTaiko.Skin.SongLoading_SubTitle_X_AI - ((this.txSubtitle.szImageSize.Width * txSubtitle.vcScaleRatio.X) / 2)), OpenTaiko.Skin.SongLoading_SubTitle_Y_AI - (this.txSubtitle.szImageSize.Height / 2));
				}
			}
		}

		this.ctSongNameDisplay.Tick();

		if (TransitionDriven) {
				// The song_loading transition draws the whole loading screen — skip our own plate/title/tiles.
			} else if (isDan) {
			if (OpenTaiko.Tx.Tile_Black != null) {
				OpenTaiko.Tx.Tile_Black.Opacity = (int)(ctWait.CurrentValue <= 51 ? (255 - ctWait.CurrentValue / 0.2f) : (this.ctWait.CurrentValue - 949) / 0.2);
				for (int i = 0; i <= (GameWindowSize.Width / OpenTaiko.Tx.Tile_Black.szTextureSize.Width); i++) {
					for (int j = 0; j <= (GameWindowSize.Height / OpenTaiko.Tx.Tile_Black.szTextureSize.Height); j++) {
						OpenTaiko.Tx.Tile_Black.t2DDraw(i * OpenTaiko.Tx.Tile_Black.szTextureSize.Width, j * OpenTaiko.Tx.Tile_Black.szTextureSize.Height);
					}
				}
			}
		} else if (isAI) {
			drawPlate_AI();
		} else {
			drawPlate();
		}

		// Real-time bar: a small crawl during chart/WAV load, then the streamed game-screen texture upload %
		// (the per-song bulk). The render loop is free throughout, so it stays smooth + ESC-responsive.
		CLoadingProgress.Report(_streamingActive ? 0.15f + 0.80f * CTexture.StreamFraction : 0.12f);
		if (!TransitionDriven) CLoadingScreen.Draw();   // the song_loading transition draws the bar itself when driving

		switch (base.ePhaseID) {
			case CStage.EPhase.Common_FADEIN:
				//if( this.actFI.On進行描画() != 0 )			    // #27787 2012.3.10 yyagi 曲読み込み画面のフェードインの省略
				// 必ず一度「CStaeg.Eフェーズ.共通_フェードイン」フェーズを経由させること。
				// さもないと、曲読み込みが完了するまで、曲読み込み画面が描画されない。
				base.ePhaseID = CStage.EPhase.SongLoading_LoadDTXFile;
				return (int)ESongLoadingScreenReturnValue.Continue;

			case CStage.EPhase.SongLoading_LoadDTXFile: {
					timeBeginLoad = DateTime.Now;
					str = OpenTaiko.SongMount.rChosenScore.FileInfo.FileAbsolutePath;

					if ((OpenTaiko.TJA != null) && OpenTaiko.TJA.IsActivated)
						OpenTaiko.TJA.DeActivate();

					int playerCount = OpenTaiko.ConfigIni.nPlayerCount;
					int[] chosenDiffs = new int[playerCount];
					for (int i = 0; i < playerCount; i++)
						chosenDiffs[i] = OpenTaiko.SongMount.nChoosenSongDifficulty[i];

					_loadCts     = new CancellationTokenSource();
					_loadedTjas  = new CTja[playerCount];
					var cts      = _loadCts;
					var captured = _loadedTjas;

					// DanBuilder: if a pre-built CTja was supplied, use it directly (no file read).
					var prebuilt = OpenTaiko.DanBuilderPrebuiltTja;
					string strExt = Path.GetExtension(str).ToLowerInvariant();
					if (prebuilt != null) {
						OpenTaiko.DanBuilderPrebuiltTja = null;
						_dtxLoadTask = Task.Run(() => {
							for (int i = 0; i < playerCount; i++)
								captured[i] = prebuilt;
						}, cts.Token);
					} else if (strExt is ".optktcm" or ".tcm") {
						// TCM dan course: build merged CTja from referenced songs.
						var capturedStr = str;
						_dtxLoadTask = Task.Run(() => {
							var tcm = new CTcm(capturedStr);
							var built = tcm.BuildDanCtja();
							for (int i = 0; i < playerCount; i++)
								captured[i] = built;
						}, cts.Token);
					} else if (strExt is ".optktci" or ".tci") {
						// TCI individual song: build CTja from osu course.
						var capturedStr = str;
						var capturedDiffs = chosenDiffs;
						_dtxLoadTask = Task.Run(() => {
							var tci = new CTci(capturedStr);
							for (int i = 0; i < playerCount; i++) {
								cts.Token.ThrowIfCancellationRequested();
								captured[i] = tci.BuildCtja(capturedDiffs[i]);
							}
						}, cts.Token);
					} else {
						_dtxLoadTask = Task.Run(() => {
							for (int i = 0; i < playerCount; i++) {
								cts.Token.ThrowIfCancellationRequested();
								captured[i] = new CTja(str, chosenDiffs[i], i, loadChart: true);
							}
						}, cts.Token);
					}

					base.ePhaseID = CStage.EPhase.SongLoading_WaitDTXLoaded;
					return (int)ESongLoadingScreenReturnValue.Continue;
				}

			case CStage.EPhase.SongLoading_WaitDTXLoaded: {
					if (!_dtxLoadTask!.IsCompleted)
						return (int)ESongLoadingScreenReturnValue.Continue;

					if (_dtxLoadTask.IsFaulted) {
						Trace.TraceError("Chart loading failed: {0}", _dtxLoadTask.Exception);
						_dtxLoadTask = null;
						_loadedTjas = null;
						return (int)ESongLoadingScreenReturnValue.LoadCanceled;
					}

					for (int i = 0; i < _loadedTjas!.Length; i++)
						OpenTaiko.SetTJA(i, _loadedTjas[i]);

					_dtxLoadTask = null;
					_loadedTjas = null;

					TimeSpan span = (TimeSpan)(DateTime.Now - timeBeginLoad);
					Trace.TraceInformation("---- Song information -----------------");
					Trace.TraceInformation("TITLE: {0}", OpenTaiko.TJA.TITLE.GetString(""));
					Trace.TraceInformation("FILE: {0}", OpenTaiko.TJA.strFullPath);
					Trace.TraceInformation("---------------------------");
					Trace.TraceInformation("Chart loading time:           {0}", span.ToString());

					// 段位認定モード用。
					#region [dan setup]
					if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan && OpenTaiko.TJA.List_DanSongs != null) {
						var titleForeColor = OpenTaiko.Skin.Game_DanC_Title_ForeColor;
						var titleBackColor = OpenTaiko.Skin.Game_DanC_Title_BackColor;
						var subtitleForeColor = OpenTaiko.Skin.Game_DanC_SubTitle_ForeColor;
						var subtitleBackColor = OpenTaiko.Skin.Game_DanC_SubTitle_BackColor;

						for (int i = 0; i < OpenTaiko.TJA.List_DanSongs.Count; i++) {
							if (!string.IsNullOrEmpty(OpenTaiko.TJA.List_DanSongs[i].Title)) {
								using (var bmpSongTitle = pfDanTitle.DrawText(OpenTaiko.TJA.List_DanSongs[i].Title, titleForeColor, titleBackColor, null, 30)) {
									OpenTaiko.TJA.List_DanSongs[i].TitleTex = OpenTaiko.tTextureCreate(bmpSongTitle, false);
									OpenTaiko.TJA.List_DanSongs[i].TitleTex.vcScaleRatio.X = OpenTaiko.GetSongNameXScaling(ref OpenTaiko.TJA.List_DanSongs[i].TitleTex, OpenTaiko.Skin.Game_DanC_Title_MaxWidth);
								}
							}

							if (!string.IsNullOrEmpty(OpenTaiko.TJA.List_DanSongs[i].SubTitle)) {
								using (var bmpSongSubTitle = pfDanSubTitle.DrawText(OpenTaiko.TJA.List_DanSongs[i].SubTitle, subtitleForeColor, subtitleBackColor, null, 30)) {
									OpenTaiko.TJA.List_DanSongs[i].SubTitleTex = OpenTaiko.tTextureCreate(bmpSongSubTitle, false);
									OpenTaiko.TJA.List_DanSongs[i].SubTitleTex.vcScaleRatio.X = OpenTaiko.GetSongNameXScaling(ref OpenTaiko.TJA.List_DanSongs[i].SubTitleTex, OpenTaiko.Skin.Game_DanC_SubTitle_MaxWidth);
								}
							}
						}
					}
					#endregion

					timeBeginLoadWAV = DateTime.Now;
					base.ePhaseID = CStage.EPhase.SongLoading_WaitToLoadWAVFile;
					return (int)ESongLoadingScreenReturnValue.Continue;
				}

			case CStage.EPhase.SongLoading_WaitToLoadWAVFile: {
					if (this.ctWait.CurrentValue > 260) {
						// Start loading all WAVs on a background thread (BASS is thread-safe).
						var tja  = OpenTaiko.TJA;
						var wcts = _loadCts!;
						_wavLoadTask = Task.Run(() => {
							foreach (var cwav in tja.listWAV.Values) {
								if (wcts.Token.IsCancellationRequested) break;
								if (cwav.listThisWAVUseChannelNumberSet.Count > 0)
									tja.tWAVLoad(cwav);
							}
						}, wcts.Token);
						base.ePhaseID = CStage.EPhase.SongLoading_LoadWAVFile;
					}
					return (int)ESongLoadingScreenReturnValue.Continue;
				}

			case CStage.EPhase.SongLoading_LoadWAVFile: {
					// Poll the background WAV loading task each frame so the animation keeps running.
					if (!_wavLoadTask!.IsCompleted)
						return (int)ESongLoadingScreenReturnValue.Continue;

					if (_wavLoadTask.IsFaulted) {
						Trace.TraceError("WAV loading failed: {0}", _wavLoadTask.Exception);
						_wavLoadTask = null;
						return (int)ESongLoadingScreenReturnValue.LoadCanceled;
					}

					_wavLoadTask = null;

					TimeSpan span = (TimeSpan)(DateTime.Now - timeBeginLoadWAV);
					Trace.TraceInformation("Song loading time({0,4}):     {1}", OpenTaiko.TJA.listWAV.Count, span.ToString());
					timeBeginLoadWAV = DateTime.Now;

					if (OpenTaiko.ConfigIni.bDynamicBassMixerManagement) {
						OpenTaiko.TJA.PlanToAddMixerChannel();
					}

					for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
						var _dtx = OpenTaiko.GetTJA(i);
						_dtx?.tInitLocalStores(i);
						_dtx?.tRandomizeTaikoChips(i);
						_dtx?.tApplyFunMods(i);
						OpenTaiko.ReplayInstances[i] = new CSongReplay(_dtx.strFullPath, i);
					}

					// Chart + WAV are loaded; the game-screen activation (streamed) runs in the next phase.
					base.ePhaseID = CStage.EPhase.SongLoading_LoadBMPFile;
					return (int)ESongLoadingScreenReturnValue.Continue;
				}

			case CStage.EPhase.SongLoading_LoadBMPFile: {
					// Activate the game screen in STREAMING mode: it runs synchronously on the render thread (Lua
					// VMs are thread-affine — running them off-thread corrupts the interpreter and AccessViolation
					// crashes), but each MakeTexture(path) only reads the image header (size) and QUEUES the pixel
					// work instead of decoding inline. So Activate returns in a few ms rather than freezing for
					// seconds; the decode + GL upload then stream in over the next frames (StreamTextures below)
					// with the render loop free → smooth bar + live ESC.
					// Batch trace writes during activation: Trace.AutoFlush is on (OpenTaiko.cs), so the per-texture
					// [ALLOC_TEX] debug log otherwise flushes to disk every call — slow on an AV-scanned folder and
					// a big part of the activation cost. Buffer it, then flush once.
					bool prevAutoFlush = System.Diagnostics.Trace.AutoFlush;
					System.Diagnostics.Trace.AutoFlush = false;
					try {
						CTexture.BeginStreaming();
						OpenTaiko.stageGameScreen.Activate();
						CTexture.StreamingLoad = false;     // activation done queueing; later loads go synchronous
						CTexture.StartStreamDecode();       // decode everything queued, off-thread
						_streamingActive = true;
					} finally {
						System.Diagnostics.Trace.AutoFlush = prevAutoFlush;
						System.Diagnostics.Trace.Flush();
					}
					base.ePhaseID = CStage.EPhase.SongLoading_StreamTextures;
					return (int)ESongLoadingScreenReturnValue.Continue;
				}

			case CStage.EPhase.SongLoading_StreamTextures: {
					// Drain decoded bitmaps to the GPU within a per-frame time budget; keep rendering (smooth bar
					// + responsive ESC) until every queued texture is uploaded. AVI + FastRender below both touch
					// the freshly-loaded textures, so they MUST run only after the stream completes.
					CTexture.PumpUploads(8.0);
					if (!CTexture.StreamComplete)
						return (int)ESongLoadingScreenReturnValue.Continue;

					CTexture.EndStreaming();
					_streamingActive = false;
					CLoadingProgress.Report(0.96f);

					if (OpenTaiko.ConfigIni.bEnableAVI)
						OpenTaiko.TJA.tAVILoad();

					TimeSpan span = (TimeSpan)(DateTime.Now - timeBeginLoad);
					Trace.TraceInformation("総読込時間:                {0}", span.ToString());

					if (OpenTaiko.ConfigIni.FastRender) {
						var fastRender = new FastRender();
						fastRender.Render();
						fastRender = null;
					}

					CLoadingProgress.End();   // everything loaded → 100%

					// Loading is complete — release the cancellation token source.
					_loadCts?.Dispose();
					_loadCts = null;

					OpenTaiko.Timer.Update();
					//CSound管理.rc演奏用タイマ.t更新();
					base.ePhaseID = CStage.EPhase.SongLoading_WaitForSoundSystemBGM;
					return (int)ESongLoadingScreenReturnValue.Continue;
				}

			case CStage.EPhase.SongLoading_WaitForSoundSystemBGM: {
					long nCurrentTime = OpenTaiko.Timer.NowTimeMs;
					if (nCurrentTime < this.nBGMPlaybackStartTime)
						this.nBGMPlaybackStartTime = nCurrentTime;

					//						if ( ( nCurrentTime - this.nBGM再生開始時刻 ) > ( this.nBGMの総再生時間ms - 1000 ) )
					if ((nCurrentTime - this.nBGMPlaybackStartTime) >= (this.nBGMTotalPlaybackTimems)    // #27787 2012.3.10 yyagi 1000ms == フェードイン分の時間
						&& (DateTime.Now - _activateTime).TotalSeconds >= 2.0)
					{
						base.ePhaseID = CStage.EPhase.Common_FADEOUT;
					}
					return (int)ESongLoadingScreenReturnValue.Continue;
				}

			case CStage.EPhase.Common_FADEOUT:
				if (this.ctWait.IsUnEnded)        // DTXVモード時は、フェードアウト省略
					return (int)ESongLoadingScreenReturnValue.Continue;

				if (this.sdLoadSound != null) {
					this.sdLoadSound.tDispose();
				}
				return (int)ESongLoadingScreenReturnValue.LoadComplete;
		}
		return (int)ESongLoadingScreenReturnValue.Continue;
	}

	/// <summary>
	/// ESC押下時、trueを返す
	/// </summary>
	/// <returns></returns>
	protected bool tKeyInput() {
		IInputDevice keyboard = OpenTaiko.InputManager.Keyboard;
		if (keyboard.KeyPressed((int)SlimDXKeys.Key.Escape))        // escape (exit)
		{
			return true;
		}
		return false;
	}

	// その他

	#region [ private ]
	//-----------------
	//private CActFIFOBlack actFI;
	//private CActFIFOBlack actFO;
	private long nBGMTotalPlaybackTimems;
	private long nBGMPlaybackStartTime;
	private CSound sdLoadSound;
	private string strSTAGEFILE;
	private string strSongTitle;
	private string strSubtitle;
	private CTexture txTitle;
	private CTexture txSubtitle;
	//private CTexture txSongnamePlate;
	private DateTime timeBeginLoad;
	private DateTime timeBeginLoadWAV;
	private DateTime _activateTime;
	private CancellationTokenSource? _loadCts;
	private Task? _dtxLoadTask;
	private CTja[]? _loadedTjas;
	private Task? _wavLoadTask;
	private bool _streamingActive;             // game screen activated + textures streaming in (see LoadBMPFile)
	private CCounter ctWait;
	private CCounter ctSongNameDisplay;

	private CCachedFontRenderer pfTITLE;
	private CCachedFontRenderer pfSUBTITLE;

	private CCachedFontRenderer pfDanTitle = null;
	private CCachedFontRenderer pfDanSubTitle = null;
	//-----------------
	#endregion
}
