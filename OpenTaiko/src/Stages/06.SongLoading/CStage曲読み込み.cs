using System.Diagnostics;
using FDK;

namespace OpenTaiko;

internal class CStage曲読み込み : CStage {
	// コンストラクタ

	public CStage曲読み込み() {
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
			this.str曲タイトル = "";
			this.strSTAGEFILE = "";
			this.nBGM再生開始時刻 = -1;
			this.nBGMの総再生時間ms = 0;
			if (this.sd読み込み音 != null) {
				OpenTaiko.SoundManager.tDisposeSound(this.sd読み込み音);
				this.sd読み込み音 = null;
			}

			if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] >= 5 || OpenTaiko.ConfigIni.nPlayerCount != 1) {
				OpenTaiko.ConfigIni.bTokkunMode = false;
			}

			string strDTXファイルパス = OpenTaiko.SongMount.rChosenScore.ファイル情報.ファイルの絶対パス;

			var strフォルダ名 = Path.GetDirectoryName(strDTXファイルパス) + Path.DirectorySeparatorChar;

			var 譜面情報 = OpenTaiko.SongMount.rChosenScore.譜面情報;
			this.str曲タイトル = 譜面情報.タイトル;
			this.strサブタイトル = 譜面情報.strサブタイトル;

			this.strSTAGEFILE = CSkin.Path(@$"Graphics{Path.DirectorySeparatorChar}4_SongLoading{Path.DirectorySeparatorChar}Background.png");


			float wait = 600f;
			if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
				wait = 1000f;
			else if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower)
				wait = 1200f;

			this.ct待機 = new CCounter(0, wait, 5, OpenTaiko.Timer);
			this.ct曲名表示 = new CCounter(1, 30, 30, OpenTaiko.Timer);
			try {
				// When performing calibration, inform the player that
				// calibration is about to begin, rather than
				// displaying the song title and subtitle as usual.

				var タイトル = this.str曲タイトル;

				var サブタイトル = this.strサブタイトル;

				if (!string.IsNullOrEmpty(タイトル)) {
					//this.txタイトル = new CTexture( CDTXMania.app.Device, image, CDTXMania.TextureFormat );
					//this.txタイトル.vc拡大縮小倍率 = new Vector3( 0.5f, 0.5f, 1f );


					using (var bmpSongTitle = this.pfTITLE.DrawText(タイトル, OpenTaiko.Skin.SongLoading_Title_ForeColor, OpenTaiko.Skin.SongLoading_Title_BackColor, null, 30)) {
						this.txタイトル = new CTexture(bmpSongTitle);
						txタイトル.vcScaleRatio.X = OpenTaiko.GetSongNameXScaling(ref txタイトル, OpenTaiko.Skin.SongLoading_Title_MaxSize);
					}

					using (var bmpSongSubTitle = this.pfSUBTITLE.DrawText(サブタイトル, OpenTaiko.Skin.SongLoading_SubTitle_ForeColor, OpenTaiko.Skin.SongLoading_SubTitle_BackColor, null, 30)) {
						this.txサブタイトル = new CTexture(bmpSongSubTitle);
						txサブタイトル.vcScaleRatio.X = OpenTaiko.GetSongNameXScaling(ref txサブタイトル, OpenTaiko.Skin.SongLoading_SubTitle_MaxSize);
					}
				} else {
					this.txタイトル = null;
					this.txサブタイトル = null;
				}

				_activateTime = DateTime.Now;
			} catch (CTextureCreateFailedException e) {
				Trace.TraceError(e.ToString());
				Trace.TraceError("テクスチャの生成に失敗しました。({0})", new object[] { this.strSTAGEFILE });
				this.txタイトル = null;
				this.txサブタイトル = null;
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

			OpenTaiko.tテクスチャの解放(ref this.txタイトル);
			//CDTXMania.tテクスチャの解放( ref this.txSongnamePlate );
			OpenTaiko.tテクスチャの解放(ref this.txサブタイトル);
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
			CScore cスコア1 = OpenTaiko.SongMount.rChosenScore;
			if (this.sd読み込み音 != null) {
				if (OpenTaiko.Skin.sound曲読込開始音.bExclusive && (CSkin.CSystemSound.r最後に再生した排他システムサウンド != null)) {
					CSkin.CSystemSound.r最後に再生した排他システムサウンド.tStop();
				}
				this.sd読み込み音.PlayStart();
				this.nBGM再生開始時刻 = SoundManager.PlayTimer.NowTimeMs;
				this.nBGMの総再生時間ms = this.sd読み込み音.TotalPlayTime;
			} else {
				OpenTaiko.Skin.sound曲読込開始音.tPlay();
				this.nBGM再生開始時刻 = SoundManager.PlayTimer.NowTimeMs;
				this.nBGMの総再生時間ms = OpenTaiko.Skin.sound曲読込開始音.n長さ_現在のサウンド;
			}
			//this.actFI.tフェードイン開始();							// #27787 2012.3.10 yyagi 曲読み込み画面のフェードインの省略
			base.ePhaseID = CStage.EPhase.Common_FADEIN;
			base.IsFirstDraw = false;
		}
		//-----------------------------
		#endregion
		this.ct待機.Tick();

		#region [ Cancel loading with esc ]
		if (tキー入力()) {
			if (this.sd読み込み音 != null) {
				this.sd読み込み音.tStopSound();
				this.sd読み込み音.tDispose();
			}
			return (int)ESongLoadingScreenReturnValue.LoadCanceled;
		}
		#endregion

		bool isDan   = OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan;
		bool isAI    = OpenTaiko.ConfigIni.bAIBattleMode;

		void drawPlate() {
			if (OpenTaiko.Tx.SongLoading_Plate != null) {
				OpenTaiko.Tx.SongLoading_Plate.bスクリーン合成 = OpenTaiko.Skin.SongLoading_Plate_ScreenBlend;
				OpenTaiko.Tx.SongLoading_Plate.Opacity = 255;
				if (OpenTaiko.Skin.SongLoading_Plate_ReferencePoint == CSkin.ReferencePoint.Left) {
					OpenTaiko.Tx.SongLoading_Plate.t2D描画(OpenTaiko.Skin.SongLoading_Plate_X, OpenTaiko.Skin.SongLoading_Plate_Y - (OpenTaiko.Tx.SongLoading_Plate.sz画像サイズ.Height / 2));
				} else if (OpenTaiko.Skin.SongLoading_Plate_ReferencePoint == CSkin.ReferencePoint.Right) {
					OpenTaiko.Tx.SongLoading_Plate.t2D描画(OpenTaiko.Skin.SongLoading_Plate_X - OpenTaiko.Tx.SongLoading_Plate.sz画像サイズ.Width, OpenTaiko.Skin.SongLoading_Plate_Y - (OpenTaiko.Tx.SongLoading_Plate.sz画像サイズ.Height / 2));
				} else {
					OpenTaiko.Tx.SongLoading_Plate.t2D描画(OpenTaiko.Skin.SongLoading_Plate_X - (OpenTaiko.Tx.SongLoading_Plate.sz画像サイズ.Width / 2), OpenTaiko.Skin.SongLoading_Plate_Y - (OpenTaiko.Tx.SongLoading_Plate.sz画像サイズ.Height / 2));
				}
			}

			if (this.txタイトル != null) {
				int nサブタイトル補正 = string.IsNullOrEmpty(OpenTaiko.SongMount.rChosenScore.譜面情報.strサブタイトル) ? 15 : 0;
				this.txタイトル.Opacity = 255;
				if (OpenTaiko.Skin.SongLoading_Title_ReferencePoint == CSkin.ReferencePoint.Left) {
					this.txタイトル.t2D描画(OpenTaiko.Skin.SongLoading_Title_X, OpenTaiko.Skin.SongLoading_Title_Y - (this.txタイトル.sz画像サイズ.Height / 2) + nサブタイトル補正);
				} else if (OpenTaiko.Skin.SongLoading_Title_ReferencePoint == CSkin.ReferencePoint.Right) {
					this.txタイトル.t2D描画(OpenTaiko.Skin.SongLoading_Title_X - (this.txタイトル.sz画像サイズ.Width * txタイトル.vcScaleRatio.X), OpenTaiko.Skin.SongLoading_Title_Y - (this.txタイトル.sz画像サイズ.Height / 2) + nサブタイトル補正);
				} else {
					this.txタイトル.t2D描画((OpenTaiko.Skin.SongLoading_Title_X - ((this.txタイトル.sz画像サイズ.Width * txタイトル.vcScaleRatio.X) / 2)), OpenTaiko.Skin.SongLoading_Title_Y - (this.txタイトル.sz画像サイズ.Height / 2) + nサブタイトル補正);
				}
			}
			if (this.txサブタイトル != null) {
				this.txサブタイトル.Opacity = 255;
				if (OpenTaiko.Skin.SongLoading_SubTitle_ReferencePoint == CSkin.ReferencePoint.Left) {
					this.txサブタイトル.t2D描画(OpenTaiko.Skin.SongLoading_SubTitle_X, OpenTaiko.Skin.SongLoading_SubTitle_Y - (this.txサブタイトル.sz画像サイズ.Height / 2));
				} else if (OpenTaiko.Skin.SongLoading_Title_ReferencePoint == CSkin.ReferencePoint.Right) {
					this.txサブタイトル.t2D描画(OpenTaiko.Skin.SongLoading_SubTitle_X - (this.txサブタイトル.sz画像サイズ.Width * txタイトル.vcScaleRatio.X), OpenTaiko.Skin.SongLoading_SubTitle_Y - (this.txサブタイトル.sz画像サイズ.Height / 2));
				} else {
					this.txサブタイトル.t2D描画((OpenTaiko.Skin.SongLoading_SubTitle_X - ((this.txサブタイトル.sz画像サイズ.Width * txサブタイトル.vcScaleRatio.X) / 2)), OpenTaiko.Skin.SongLoading_SubTitle_Y - (this.txサブタイトル.sz画像サイズ.Height / 2));
				}
			}
		}

		void drawPlate_AI() {
			if (OpenTaiko.Tx.SongLoading_Plate_AI != null) {
				OpenTaiko.Tx.SongLoading_Plate_AI.bスクリーン合成 = OpenTaiko.Skin.SongLoading_Plate_ScreenBlend;
				OpenTaiko.Tx.SongLoading_Plate_AI.Opacity = 255;
				if (OpenTaiko.Skin.SongLoading_Plate_ReferencePoint == CSkin.ReferencePoint.Left) {
					OpenTaiko.Tx.SongLoading_Plate_AI.t2D描画(OpenTaiko.Skin.SongLoading_Plate_X_AI, OpenTaiko.Skin.SongLoading_Plate_Y_AI - (OpenTaiko.Tx.SongLoading_Plate_AI.sz画像サイズ.Height / 2));
				} else if (OpenTaiko.Skin.SongLoading_Plate_ReferencePoint == CSkin.ReferencePoint.Right) {
					OpenTaiko.Tx.SongLoading_Plate_AI.t2D描画(OpenTaiko.Skin.SongLoading_Plate_X_AI - OpenTaiko.Tx.SongLoading_Plate_AI.sz画像サイズ.Width, OpenTaiko.Skin.SongLoading_Plate_Y_AI - (OpenTaiko.Tx.SongLoading_Plate_AI.sz画像サイズ.Height / 2));
				} else {
					OpenTaiko.Tx.SongLoading_Plate_AI.t2D描画(OpenTaiko.Skin.SongLoading_Plate_X_AI - (OpenTaiko.Tx.SongLoading_Plate_AI.sz画像サイズ.Width / 2), OpenTaiko.Skin.SongLoading_Plate_Y_AI - (OpenTaiko.Tx.SongLoading_Plate_AI.sz画像サイズ.Height / 2));
				}
			}

			if (this.txタイトル != null) {
				int nサブタイトル補正 = string.IsNullOrEmpty(OpenTaiko.SongMount.rChosenScore.譜面情報.strサブタイトル) ? 15 : 0;
				this.txタイトル.Opacity = 255;
				if (OpenTaiko.Skin.SongLoading_Title_ReferencePoint == CSkin.ReferencePoint.Left) {
					this.txタイトル.t2D描画(OpenTaiko.Skin.SongLoading_Title_X_AI, OpenTaiko.Skin.SongLoading_Title_Y_AI - (this.txタイトル.sz画像サイズ.Height / 2) + nサブタイトル補正);
				} else if (OpenTaiko.Skin.SongLoading_Title_ReferencePoint == CSkin.ReferencePoint.Right) {
					this.txタイトル.t2D描画(OpenTaiko.Skin.SongLoading_Title_X_AI - (this.txタイトル.sz画像サイズ.Width * txタイトル.vcScaleRatio.X), OpenTaiko.Skin.SongLoading_Title_Y_AI - (this.txタイトル.sz画像サイズ.Height / 2) + nサブタイトル補正);
				} else {
					this.txタイトル.t2D描画((OpenTaiko.Skin.SongLoading_Title_X_AI - ((this.txタイトル.sz画像サイズ.Width * txタイトル.vcScaleRatio.X) / 2)), OpenTaiko.Skin.SongLoading_Title_Y_AI - (this.txタイトル.sz画像サイズ.Height / 2) + nサブタイトル補正);
				}
			}
			if (this.txサブタイトル != null) {
				this.txサブタイトル.Opacity = 255;
				if (OpenTaiko.Skin.SongLoading_SubTitle_ReferencePoint == CSkin.ReferencePoint.Left) {
					this.txサブタイトル.t2D描画(OpenTaiko.Skin.SongLoading_SubTitle_X_AI, OpenTaiko.Skin.SongLoading_SubTitle_Y_AI - (this.txサブタイトル.sz画像サイズ.Height / 2));
				} else if (OpenTaiko.Skin.SongLoading_Title_ReferencePoint == CSkin.ReferencePoint.Right) {
					this.txサブタイトル.t2D描画(OpenTaiko.Skin.SongLoading_SubTitle_X_AI - (this.txサブタイトル.sz画像サイズ.Width * txタイトル.vcScaleRatio.X), OpenTaiko.Skin.SongLoading_SubTitle_Y_AI - (this.txサブタイトル.sz画像サイズ.Height / 2));
				} else {
					this.txサブタイトル.t2D描画((OpenTaiko.Skin.SongLoading_SubTitle_X_AI - ((this.txサブタイトル.sz画像サイズ.Width * txサブタイトル.vcScaleRatio.X) / 2)), OpenTaiko.Skin.SongLoading_SubTitle_Y_AI - (this.txサブタイトル.sz画像サイズ.Height / 2));
				}
			}
		}

		#region [ Loading screen (except dan) ]
		//-----------------------------
		this.ct曲名表示.Tick();

		if (!isDan) {
			if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
				#region [Tower loading screen]

				if (OpenTaiko.Skin.Game_Tower_Ptn_Result > 0) {
					int xFactor = 0;
					float yFactor = 1f;

					int currentTowerType = Array.IndexOf(OpenTaiko.Skin.Game_Tower_Names, OpenTaiko.SongMount.rChoosenSong.score[5].譜面情報.nTowerType);

					if (currentTowerType < 0 || currentTowerType >= OpenTaiko.Skin.Game_Tower_Ptn)
						currentTowerType = 0;

					if (OpenTaiko.Tx.TowerResult_Background != null && currentTowerType < OpenTaiko.Tx.TowerResult_Tower.Length && OpenTaiko.Tx.TowerResult_Tower[currentTowerType] != null) {
						xFactor = (OpenTaiko.Tx.TowerResult_Background.szTextureSize.Width - OpenTaiko.Tx.TowerResult_Tower[currentTowerType].szTextureSize.Width) / 2;
						yFactor = OpenTaiko.Tx.TowerResult_Tower[currentTowerType].szTextureSize.Height / (float)OpenTaiko.Tx.TowerResult_Background.szTextureSize.Height;
					}

					float pos = (OpenTaiko.Tx.TowerResult_Background.szTextureSize.Height - OpenTaiko.Skin.Resolution[1]) -
								((ct待機.CurrentValue <= 1200 ? ct待機.CurrentValue / 10f : 120) / 120f * (OpenTaiko.Tx.TowerResult_Background.szTextureSize.Height - OpenTaiko.Skin.Resolution[1]));

					OpenTaiko.Tx.TowerResult_Background?.t2D描画(0, -1 * pos);

					if (currentTowerType < OpenTaiko.Tx.TowerResult_Tower.Length)
						OpenTaiko.Tx.TowerResult_Tower[currentTowerType]?.t2D描画(xFactor, -1 * yFactor * pos);
				}

				#endregion
			} else if (!isAI) {
				#region [Ensou loading screen]

				if (OpenTaiko.Tx.SongLoading_BgWait != null) OpenTaiko.Tx.SongLoading_BgWait.t2D描画(0, 0);
				if (OpenTaiko.Tx.SongLoading_Chara != null) OpenTaiko.Tx.SongLoading_Chara.t2D描画(0, 0);
				#endregion
			}

			//CDTXMania.act文字コンソール.tPrint( 0, 0, C文字コンソール.Eフォント種別.灰, this.ct曲名表示.n現在の値.ToString() );

			//-----------------------------
			#endregion
		} else {
			#region [ Dan Loading screen　]

			OpenTaiko.Tx.SongLoading_Bg_Dan.t2D描画(0, 0 - (ct待機.CurrentValue <= 600 ? ct待機.CurrentValue / 10f : 60));
			#endregion
		}

		if (isDan) {
			CTexture dp = (OpenTaiko.stageDanSongSelect.段位リスト.stバー情報 != null)
				? OpenTaiko.stageDanSongSelect.段位リスト.stバー情報[OpenTaiko.stageDanSongSelect.段位リスト.Cursor.IdxItem].txDanPlate
				: null;

			CActSelect段位リスト.tDisplayDanPlate(dp,
				null,
				OpenTaiko.Skin.SongLoading_DanPlate[0],
				OpenTaiko.Skin.SongLoading_DanPlate[1]);

			if (OpenTaiko.Tx.Tile_Black != null) {
				OpenTaiko.Tx.Tile_Black.Opacity = (int)(ct待機.CurrentValue <= 51 ? (255 - ct待機.CurrentValue / 0.2f) : (this.ct待機.CurrentValue - 949) / 0.2);
				for (int i = 0; i <= (GameWindowSize.Width / OpenTaiko.Tx.Tile_Black.szTextureSize.Width); i++) {
					for (int j = 0; j <= (GameWindowSize.Height / OpenTaiko.Tx.Tile_Black.szTextureSize.Height); j++) {
						OpenTaiko.Tx.Tile_Black.t2D描画(i * OpenTaiko.Tx.Tile_Black.szTextureSize.Width, j * OpenTaiko.Tx.Tile_Black.szTextureSize.Height);
					}
				}
			}
		} else if (isAI) {
			OpenTaiko.ConfigIni.tInitializeAILevel();
			OpenTaiko.Tx.SongLoading_Bg_AI_Wait.t2D描画(0, 0);
			drawPlate_AI();
		} else {
			drawPlate();
		}

		switch (base.ePhaseID) {
			case CStage.EPhase.Common_FADEIN:
				//if( this.actFI.On進行描画() != 0 )			    // #27787 2012.3.10 yyagi 曲読み込み画面のフェードインの省略
				// 必ず一度「CStaeg.Eフェーズ.共通_フェードイン」フェーズを経由させること。
				// さもないと、曲読み込みが完了するまで、曲読み込み画面が描画されない。
				base.ePhaseID = CStage.EPhase.SongLoading_LoadDTXFile;
				return (int)ESongLoadingScreenReturnValue.Continue;

			case CStage.EPhase.SongLoading_LoadDTXFile: {
					timeBeginLoad = DateTime.Now;
					str = OpenTaiko.SongMount.rChosenScore.ファイル情報.ファイルの絶対パス;

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
					_dtxLoadTask = Task.Run(() => {
						for (int i = 0; i < playerCount; i++) {
							cts.Token.ThrowIfCancellationRequested();
							captured[i] = new CTja(str, chosenDiffs[i], i, loadChart: true);
						}
					}, cts.Token);

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
									OpenTaiko.TJA.List_DanSongs[i].TitleTex = OpenTaiko.tテクスチャの生成(bmpSongTitle, false);
									OpenTaiko.TJA.List_DanSongs[i].TitleTex.vcScaleRatio.X = OpenTaiko.GetSongNameXScaling(ref OpenTaiko.TJA.List_DanSongs[i].TitleTex, OpenTaiko.Skin.Game_DanC_Title_MaxWidth);
								}
							}

							if (!string.IsNullOrEmpty(OpenTaiko.TJA.List_DanSongs[i].SubTitle)) {
								using (var bmpSongSubTitle = pfDanSubTitle.DrawText(OpenTaiko.TJA.List_DanSongs[i].SubTitle, subtitleForeColor, subtitleBackColor, null, 30)) {
									OpenTaiko.TJA.List_DanSongs[i].SubTitleTex = OpenTaiko.tテクスチャの生成(bmpSongSubTitle, false);
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
					if (this.ct待機.CurrentValue > 260) {
						// Start loading all WAVs on a background thread (BASS is thread-safe).
						var tja  = OpenTaiko.TJA;
						var wcts = _loadCts!;
						_wavLoadTask = Task.Run(() => {
							foreach (var cwav in tja.listWAV.Values) {
								if (wcts.Token.IsCancellationRequested) break;
								if (cwav.listこのWAVを使用するチャンネル番号の集合.Count > 0)
									tja.tWAVの読み込み(cwav);
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

					// Game screen activation (loads background scripts, character anims, etc.)
					// This still runs on the main thread as it involves GPU texture uploads.
					OpenTaiko.stageGameScreen.Activate();

					base.ePhaseID = CStage.EPhase.SongLoading_LoadBMPFile;
					return (int)ESongLoadingScreenReturnValue.Continue;
				}

			case CStage.EPhase.SongLoading_LoadBMPFile: {
					TimeSpan span;
					DateTime timeBeginLoadBMPAVI = DateTime.Now;

					if (OpenTaiko.ConfigIni.bEnableAVI)
						OpenTaiko.TJA.tAVIの読み込み();
					span = (TimeSpan)(DateTime.Now - timeBeginLoadBMPAVI);

					span = (TimeSpan)(DateTime.Now - timeBeginLoad);
					Trace.TraceInformation("総読込時間:                {0}", span.ToString());

					if (OpenTaiko.ConfigIni.FastRender) {
						var fastRender = new FastRender();
						fastRender.Render();
						fastRender = null;
					}


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
					if (nCurrentTime < this.nBGM再生開始時刻)
						this.nBGM再生開始時刻 = nCurrentTime;

					//						if ( ( nCurrentTime - this.nBGM再生開始時刻 ) > ( this.nBGMの総再生時間ms - 1000 ) )
					if ((nCurrentTime - this.nBGM再生開始時刻) >= (this.nBGMの総再生時間ms)    // #27787 2012.3.10 yyagi 1000ms == フェードイン分の時間
						&& (DateTime.Now - _activateTime).TotalSeconds >= 2.0)
					{
						base.ePhaseID = CStage.EPhase.Common_FADEOUT;
					}
					return (int)ESongLoadingScreenReturnValue.Continue;
				}

			case CStage.EPhase.Common_FADEOUT:
				if (this.ct待機.IsUnEnded)        // DTXVモード時は、フェードアウト省略
					return (int)ESongLoadingScreenReturnValue.Continue;

				if (this.sd読み込み音 != null) {
					this.sd読み込み音.tDispose();
				}
				return (int)ESongLoadingScreenReturnValue.LoadComplete;
		}
		return (int)ESongLoadingScreenReturnValue.Continue;
	}

	/// <summary>
	/// ESC押下時、trueを返す
	/// </summary>
	/// <returns></returns>
	protected bool tキー入力() {
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
	private long nBGMの総再生時間ms;
	private long nBGM再生開始時刻;
	private CSound sd読み込み音;
	private string strSTAGEFILE;
	private string str曲タイトル;
	private string strサブタイトル;
	private CTexture txタイトル;
	private CTexture txサブタイトル;
	//private CTexture txSongnamePlate;
	private DateTime timeBeginLoad;
	private DateTime timeBeginLoadWAV;
	private DateTime _activateTime;
	private CancellationTokenSource? _loadCts;
	private Task? _dtxLoadTask;
	private CTja[]? _loadedTjas;
	private Task? _wavLoadTask;
	private CCounter ct待機;
	private CCounter ct曲名表示;

	private CCachedFontRenderer pfTITLE;
	private CCachedFontRenderer pfSUBTITLE;

	private CCachedFontRenderer pfDanTitle = null;
	private CCachedFontRenderer pfDanSubTitle = null;
	//-----------------
	#endregion
}
