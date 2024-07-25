using System.Drawing;
using FDK;

namespace TJAPlayer3 {
	internal class CActPlayOption : CActivity {
		public CActPlayOption() {
			base.IsDeActivated = true;
		}

		public override void Activate() {
			if (this.IsActivated)
				return;

			ctOpen = new CCounter();
			ctClose = new CCounter();

			for (int i = 0; i < OptionType.Length; i++)
				OptionType[i] = new CTexture();

			#region [ Speed ]

			txSpeed[0] = OptionTypeTx("0.5", Color.White, Color.Black);
			txSpeed[1] = OptionTypeTx("1.0", Color.White, Color.Black);
			txSpeed[2] = OptionTypeTx("1.1", Color.White, Color.Black);
			txSpeed[3] = OptionTypeTx("1.2", Color.White, Color.Black);
			txSpeed[4] = OptionTypeTx("1.3", Color.White, Color.Black);
			txSpeed[5] = OptionTypeTx("1.4", Color.White, Color.Black);
			txSpeed[6] = OptionTypeTx("1.5", Color.White, Color.Black);
			txSpeed[7] = OptionTypeTx("1.6", Color.White, Color.Black);
			txSpeed[8] = OptionTypeTx("1.7", Color.White, Color.Black);
			txSpeed[9] = OptionTypeTx("1.8", Color.White, Color.Black);
			txSpeed[10] = OptionTypeTx("1.9", Color.White, Color.Black);
			txSpeed[11] = OptionTypeTx("2.0", Color.White, Color.Black);
			txSpeed[12] = OptionTypeTx("2.5", Color.White, Color.Black);
			txSpeed[13] = OptionTypeTx("3.0", Color.White, Color.Black);
			txSpeed[14] = OptionTypeTx("3.5", Color.White, Color.Black);
			txSpeed[15] = OptionTypeTx("4.0", Color.White, Color.Black);

			#endregion

			for (int i = 0; i < txSongSpeed.Length; i++) {
				Color _c = Color.White;

				if (i < 5)
					_c = Color.LimeGreen;
				else if (i > 5)
					_c = Color.Red;

				txSongSpeed[i] = OptionTypeTx((0.5f + i * 0.1f).ToString("n1"), _c, Color.Black);
			}

			txSwitch[0] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_SWITCH_OFF"), Color.White, Color.Black);
			txSwitch[1] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_SWITCH_ON"), Color.White, Color.Black);

			txRandom[0] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_NONE"), Color.White, Color.Black);
			txRandom[1] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_RANDOM_SHUFFLE"), Color.White, Color.Black);
			txRandom[2] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_RANDOM_CHAOS"), Color.White, Color.Black);

			txStealth[0] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_SWITCH_OFF"), Color.White, Color.Black);
			txStealth[1] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_HIDE"), Color.White, Color.Black);
			txStealth[2] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_STEALTH"), Color.White, Color.Black);

			txJust[0] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_SWITCH_OFF"), Color.White, Color.Black);
			txJust[1] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_JUST"), Color.Red, Color.Black);
			txJust[2] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_SAFE"), Color.LimeGreen, Color.Black);

			txGameMode[0] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_NONE"), Color.White, Color.Black);
			txGameMode[1] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_GAMEMODE_TRAINING"), Color.White, Color.Black);

			txGameType[0] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_GAMETYPE_TAIKO"), Color.White, Color.Black);
			txGameType[1] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_GAMETYPE_KONGA"), Color.White, Color.Black);

			txFunMods[0] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_SWITCH_OFF"), Color.White, Color.Black);
			txFunMods[1] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_FUN_AVALANCHE"), Color.White, Color.Black);
			txFunMods[2] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_FUN_MINESWEEPER"), Color.White, Color.Black);

			txNone = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_BLANK"), Color.White, Color.Black);

			hsInfo = TJAPlayer3.Skin.hsHitSoundsInformations;

			txOtoiro = new CTexture[hsInfo.names.Length];

			if (txOtoiro.Length > 0) {
				for (int i = 0; i < txOtoiro.Length; i++) {
					txOtoiro[i] = OptionTypeTx(hsInfo.names[i], Color.White, Color.Black);
				}
			} else {
				txOtoiro = new CTexture[1];
				txOtoiro[0] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_BLANK"), Color.White, Color.Black);
			}

			OptionType[0] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_SPEED"), Color.White, Color.Black);
			OptionType[1] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_HIDE"), Color.White, Color.Black);
			OptionType[2] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_FLIP"), Color.White, Color.Black);
			OptionType[3] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_RANDOM"), Color.White, Color.Black);
			OptionType[4] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_TIMING"), Color.White, Color.Black);
			OptionType[5] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_JUSTICE"), Color.White, Color.Black);
			OptionType[6] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_GAMETYPE"), Color.White, Color.Black);
			OptionType[7] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_GAMEMODE"), Color.White, Color.Black);
			OptionType[8] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_AUTO"), Color.White, Color.Black);
			OptionType[9] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_SONGSPEED"), Color.White, Color.Black);
			OptionType[10] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_HITSOUND"), Color.White, Color.Black);
			OptionType[11] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_FUN"), Color.White, Color.Black);

			var _timingColors = new Color[] { Color.LimeGreen, Color.YellowGreen, Color.White, Color.Orange, Color.Red };
			for (int i = 0; i < 5; i++) {
				txTiming[i] = OptionTypeTx(CLangManager.LangInstance.GetString($"MOD_TIMING{i + 1}"), _timingColors[i], Color.Black);
			}

			for (int i = 0; i < OptionType.Length; i++)
				OptionType[i].vcScaleRatio.X = 0.96f;

			base.Activate();
		}

		public void tFetchMults(int player) {
			var scoreMult = tGetModMultiplier(EBalancingType.SCORE, true, player);
			var coinMult = tGetModMultiplier(EBalancingType.COINS, true, player);
			txModMults[0] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_SCOREMULTIPLY", scoreMult.ToString("n2")), Color.White, Color.Black);
			txModMults[1] = OptionTypeTx(CLangManager.LangInstance.GetString("MOD_COINMULTIPLY", coinMult.ToString("n2")), Color.White, Color.Black);
		}

		public override void DeActivate() {
			base.DeActivate();
		}
		public override void CreateManagedResource() {
			OptionFont = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.SongSelect_Option_Font_Scale);

			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource() {
			OptionFont.Dispose();


			base.ReleaseManagedResource();
		}



		public int On進行描画(int player) {
			if (this.IsDeActivated)
				return 0;

			if (ctOpen.CurrentValue == 0)
				Init(player);

			ctOpen.Tick();
			ctClose.Tick();

			if (!ctOpen.IsTicked) ctOpen.Start(0, 50, 6, TJAPlayer3.Timer);

			var act難易度 = TJAPlayer3.stageSongSelect.actDifficultySelectionScreen;
			var danAct = TJAPlayer3.stage段位選択.段位挑戦選択画面;

			#region [ Open & Close ]

			float oy1 = ctOpen.CurrentValue * 18;
			float oy2 = (ctOpen.CurrentValue - 30) * 4;
			float oy3 = ctOpen.CurrentValue < 30 ? 410 - oy1 : -80 + oy2;

			float cy1 = ctClose.CurrentValue * 3;
			float cy2 = (ctClose.CurrentValue - 20) * 16;
			float cy3 = ctClose.CurrentValue < 20 ? 0 - cy1 : 20 + cy2;

			float y = oy3 + cy3;

			#endregion




			var _textures = new CTexture[]
			{
				txSpeed[nSpeedCount],
				txStealth[nStealth],
				txSwitch[nAbekobe],
				txRandom[nRandom],
				txTiming[nTiming],
				txJust[nJust],
				txGameType[nGameType],
				txGameMode[nGameMode],
				txSwitch[nAutoMode],
				txSongSpeed[nSongSpeed],
				txOtoiro[nOtoiro],
				txFunMods[nFunMods],
			};

			var pos = player % 2;
			var _shift = pos == 1 ? (TJAPlayer3.Tx.Difficulty_Option.szTextureSize.Width / 2) : 0;
			var _rect = new Rectangle(_shift, 0, TJAPlayer3.Tx.Difficulty_Option.szTextureSize.Width / 2, TJAPlayer3.Tx.Difficulty_Option.szTextureSize.Height);

			TJAPlayer3.Tx.Difficulty_Option.t2D描画(_shift, y, _rect);
			TJAPlayer3.Tx.Difficulty_Option_Select.t2D描画(_shift + TJAPlayer3.Skin.SongSelect_Option_Select_Offset[0] + NowCount * TJAPlayer3.Skin.SongSelect_Option_Interval[0],
				TJAPlayer3.Skin.SongSelect_Option_Select_Offset[1] + y + NowCount * TJAPlayer3.Skin.SongSelect_Option_Interval[1], _rect);

			for (int i = 0; i < OptionType.Length; i++) {
				OptionType[i].t2D描画(TJAPlayer3.Skin.SongSelect_Option_OptionType_X[pos] + i * TJAPlayer3.Skin.SongSelect_Option_Interval[0],
					TJAPlayer3.Skin.SongSelect_Option_OptionType_Y[pos] + y + i * TJAPlayer3.Skin.SongSelect_Option_Interval[1]);
			}

			txModMults[0]?.t2D拡大率考慮描画(CTexture.RefPnt.Up, TJAPlayer3.Skin.SongSelect_Option_ModMults1_X[pos], TJAPlayer3.Skin.SongSelect_Option_ModMults1_Y[pos] + y);
			txModMults[1]?.t2D拡大率考慮描画(CTexture.RefPnt.Up, TJAPlayer3.Skin.SongSelect_Option_ModMults2_X[pos], TJAPlayer3.Skin.SongSelect_Option_ModMults2_Y[pos] + y);

			for (int i = 0; i < _textures.Length; i++) {
				_textures[i]?.t2D拡大率考慮描画(CTexture.RefPnt.Up, TJAPlayer3.Skin.SongSelect_Option_Value_X[pos] + i * TJAPlayer3.Skin.SongSelect_Option_Interval[0],
					TJAPlayer3.Skin.SongSelect_Option_Value_Y[pos] + y + i * TJAPlayer3.Skin.SongSelect_Option_Interval[1]);
			}

			if (ctClose.CurrentValue >= 50) {
				Decision(player);
				NowCount = 0;
				ctOpen.Stop();
				ctOpen.CurrentValue = 0;
				ctClose.Stop();
				ctClose.CurrentValue = 0;
				bEnd = false;
				act難易度.bOption[player] = false;
				danAct.bOption = false;
			}

			#region [ Inputs ]

			if (!ctClose.IsTicked) {
				bool _leftDrum = false;

				bool _rightDrum = false;

				bool _centerDrum = false;

				bool _cancel = false;

				switch (player) {
					case 0:
						_rightDrum = (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RightChange) || TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.RightArrow));
						_leftDrum = (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LeftChange) || TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.LeftArrow));
						_centerDrum = (TJAPlayer3.Pad.bPressedDGB(EPad.Decide) ||
							(TJAPlayer3.ConfigIni.bEnterがキー割り当てのどこにも使用されていない && TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return)));
						_cancel = (TJAPlayer3.Pad.bPressedDGB(EPad.Cancel) || TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape));
						break;
					case 1:
						_rightDrum = (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RBlue2P));
						_leftDrum = (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LBlue2P));
						_centerDrum = (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LRed2P) || TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RRed2P));
						break;
					case 2:
						_rightDrum = (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RBlue3P));
						_leftDrum = (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LBlue3P));
						_centerDrum = (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LRed3P) || TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RRed3P));
						break;
					case 3:
						_rightDrum = (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RBlue4P));
						_leftDrum = (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LBlue4P));
						_centerDrum = (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LRed4P) || TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RRed4P));
						break;
					case 4:
						_rightDrum = (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RBlue5P));
						_leftDrum = (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LBlue5P));
						_centerDrum = (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LRed5P) || TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RRed5P));
						break;
				}


				if (_leftDrum || TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.LeftArrow)) {
					OptionSelect(true);
					tFetchMults(player);
					TJAPlayer3.Skin.soundChangeSFX.tPlay();
				}

				if (_rightDrum || TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.RightArrow)) {
					OptionSelect(false);
					tFetchMults(player);
					TJAPlayer3.Skin.soundChangeSFX.tPlay();
				}

				if (_centerDrum && ctOpen.CurrentValue >= ctOpen.EndValue) {
					TJAPlayer3.Skin.soundDecideSFX.tPlay();
					if (NowCount < nOptionCount) {
						NowCount++;
					} else if (NowCount >= nOptionCount && !bEnd) {
						bEnd = true;
						ctClose.Start(0, 50, 6, TJAPlayer3.Timer);
					}
				}

				int cp1 = nOptionCount + 1;

				if (TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.UpArrow)) {
					TJAPlayer3.Skin.soundChangeSFX.tPlay();
					NowCount = (NowCount + cp1 - 1) % cp1;
				}

				if (TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.DownArrow)) {
					TJAPlayer3.Skin.soundChangeSFX.tPlay();
					NowCount = (NowCount + 1) % cp1;
				}

				if (TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape)) {
					TJAPlayer3.Skin.soundDecideSFX.tPlay();
					bEnd = true;
					ctClose.Start(0, 50, 6, TJAPlayer3.Timer);
				}
			}


			#endregion

			return 0;
		}
		private CCachedFontRenderer OptionFont;

		public int nOptionCount = 11;

		public CCounter ctOpen;
		public CCounter ctClose;
		public CTexture[] OptionType = new CTexture[12];

		public int NowCount;
		public int[] NowCountType = new int[8];

		public bool bEnd;

		public CTexture[] txSpeed = new CTexture[16];
		public int nSpeedCount = 1;

		public CTexture[] txStealth = new CTexture[3];
		public int nStealth = 0;
		public int nAbekobe = 0;

		public CTexture[] txRandom = new CTexture[3];
		public int nRandom = 0;

		public CTexture[] txGameMode = new CTexture[2];
		public int nGameMode;

		public CTexture[] txAutoMode = new CTexture[2];
		public int nAutoMode = 0;
		public CTexture txNone = new CTexture();

		public CTexture[] txSwitch = new CTexture[2];

		public CTexture[] txTiming = new CTexture[5];
		public int nTiming = 2;

		public CTexture[] txJust = new CTexture[3];
		public int nJust = 0;

		public CTexture[] txOtoiro;
		public CHitSounds hsInfo;
		public int nOtoiro = 0;

		public CTexture[] txSongSpeed = new CTexture[16];
		public int nSongSpeed = 5;

		public CTexture[] txGameType = new CTexture[2];
		public int nGameType = 0;

		public CTexture[] txFunMods = new CTexture[3];
		public int nFunMods = 0;

		public CTexture[] txModMults = new CTexture[2];

		public CTexture OptionTypeTx(string str文字, Color forecolor, Color backcolor) {
			using (var bmp = OptionFont.DrawText(str文字, forecolor, backcolor, null, 30)) {
				return TJAPlayer3.tテクスチャの生成(bmp);
			}
		}

		private void ShiftVal(bool left, ref int value, int capUp, int capDown) {
			if (left) {
				if (value > capDown) value--;
				else value = capUp;
			} else {
				if (value < capUp) value++;
				else value = capDown;
			}
		}

		public void OptionSelect(bool left) {
			switch (NowCount) {
				case 0:
					ShiftVal(left, ref nSpeedCount, 15, 0);
					break;
				case 1:
					ShiftVal(left, ref nStealth, 2, 0);
					break;
				case 2:
					if (nAbekobe == 0) nAbekobe = 1;
					else nAbekobe = 0;
					break;
				case 3:
					ShiftVal(left, ref nRandom, 2, 0);
					break;
				case 4:
					ShiftVal(left, ref nTiming, 4, 0);
					break;
				case 5:
					ShiftVal(left, ref nJust, 2, 0);
					break;
				case 6:
					ShiftVal(left, ref nGameType, 1, 0);
					break;
				case 7:
					if (nGameMode == 0) nGameMode = 1;
					else nGameMode = 0;
					break;
				case 8:
					if (nAutoMode == 0) nAutoMode = 1;
					else nAutoMode = 0;
					break;
				case 9:
					ShiftVal(left, ref nSongSpeed, txSongSpeed.Length - 1, 0);
					break;
				case 10:
					ShiftVal(left, ref nOtoiro, txOtoiro.Length - 1, 0);
					break;
				case 11:
					ShiftVal(left, ref nFunMods, txFunMods.Length - 1, 0);
					break;
			}
		}

		public void Init(int player) {
			int actual = TJAPlayer3.GetActualPlayer(player);

			#region [ Speed ]

			int speed = TJAPlayer3.ConfigIni.nScrollSpeed[actual];

			if (speed <= 8)
				nSpeedCount = 0;
			else if (speed <= 19)
				nSpeedCount = speed - 8;
			else if (speed <= 24)
				nSpeedCount = 12;
			else if (speed <= 29)
				nSpeedCount = 13;
			else if (speed <= 34)
				nSpeedCount = 14;
			else
				nSpeedCount = 15;

			#endregion

			#region [ Doron ]

			nStealth = (int)TJAPlayer3.ConfigIni.eSTEALTH[actual];

			#endregion

			#region [ Random ]

			var rand_ = TJAPlayer3.ConfigIni.eRandom[actual];

			if (rand_ == ERandomMode.MIRRORRANDOM) {
				nRandom = 2;
				nAbekobe = 1;
			} else if (rand_ == ERandomMode.SUPERRANDOM) {
				nRandom = 2;
				nAbekobe = 0;
			} else if (rand_ == ERandomMode.RANDOM) {
				nRandom = 1;
				nAbekobe = 0;
			} else if (rand_ == ERandomMode.MIRROR) {
				nRandom = 0;
				nAbekobe = 1;
			} else if (rand_ == ERandomMode.OFF) {
				nRandom = 0;
				nAbekobe = 0;
			}

			#endregion

			#region [ Timing ]

			nTiming = TJAPlayer3.ConfigIni.nTimingZones[actual];

			#endregion

			#region [Just]

			nJust = TJAPlayer3.ConfigIni.bJust[actual];

			#endregion

			#region [GameType]

			nGameType = (int)TJAPlayer3.ConfigIni.nGameType[actual];

			#endregion

			#region [Fun mods]

			nFunMods = (int)TJAPlayer3.ConfigIni.nFunMods[actual];

			#endregion

			#region [ GameMode ]

			if (TJAPlayer3.ConfigIni.bTokkunMode == true)
				nGameMode = 1;
			else
				nGameMode = 0;

			#endregion

			#region [ AutoMode ]

			bool _auto = TJAPlayer3.ConfigIni.bAutoPlay[player];

			if (_auto == true)
				nAutoMode = 1;
			else
				nAutoMode = 0;

			#endregion

			#region [ Hitsounds ]

			nOtoiro = Math.Min(txOtoiro.Length - 1, TJAPlayer3.ConfigIni.nHitSounds[actual]);

			#endregion

			#region [ Song speed ]

			nSongSpeed = Math.Max(0, Math.Min(txSongSpeed.Length - 1, (TJAPlayer3.ConfigIni.nSongSpeed / 2) - 5));

			#endregion

			tFetchMults(player);

		}

		public void Decision(int player) {
			int actual = TJAPlayer3.GetActualPlayer(player);

			#region [ Speed ]

			if (nSpeedCount == 0) {
				TJAPlayer3.ConfigIni.nScrollSpeed[actual] = 4;
			} else if (nSpeedCount > 0 && nSpeedCount <= 11) {
				TJAPlayer3.ConfigIni.nScrollSpeed[actual] = nSpeedCount + 8;
			} else if (nSpeedCount == 12) {
				TJAPlayer3.ConfigIni.nScrollSpeed[actual] = 24;
			} else if (nSpeedCount == 13) {
				TJAPlayer3.ConfigIni.nScrollSpeed[actual] = 29;
			} else if (nSpeedCount == 14) {
				TJAPlayer3.ConfigIni.nScrollSpeed[actual] = 34;
			} else if (nSpeedCount == 15) {
				TJAPlayer3.ConfigIni.nScrollSpeed[actual] = 39;
			}

			#endregion

			#region [ Doron ]

			TJAPlayer3.ConfigIni.eSTEALTH[actual] = (EStealthMode)nStealth;

			#endregion

			#region [ Random ]

			if (nRandom == 2 && nAbekobe == 1) {
				TJAPlayer3.ConfigIni.eRandom[actual] = ERandomMode.MIRRORRANDOM;
			} else if (nRandom == 2 && nAbekobe == 0) {
				TJAPlayer3.ConfigIni.eRandom[actual] = ERandomMode.SUPERRANDOM;
			} else if (nRandom == 1 && nAbekobe == 1) {
				TJAPlayer3.ConfigIni.eRandom[actual] = ERandomMode.RANDOM;
			} else if (nRandom == 1 && nAbekobe == 0) {
				TJAPlayer3.ConfigIni.eRandom[actual] = ERandomMode.RANDOM;
			} else if (nRandom == 0 && nAbekobe == 1) {
				TJAPlayer3.ConfigIni.eRandom[actual] = ERandomMode.MIRROR;
			} else if (nRandom == 0 && nAbekobe == 0) {
				TJAPlayer3.ConfigIni.eRandom[actual] = ERandomMode.OFF;
			}

			#endregion

			#region [ Timing ]

			TJAPlayer3.ConfigIni.nTimingZones[actual] = nTiming;

			#endregion

			#region [Just]

			TJAPlayer3.ConfigIni.bJust[actual] = nJust;

			#endregion

			#region [GameType]

			TJAPlayer3.ConfigIni.nGameType[actual] = (EGameType)nGameType;

			#endregion

			#region [Fun mods]

			TJAPlayer3.ConfigIni.nFunMods[actual] = (EFunMods)nFunMods;

			#endregion

			#region [ GameMode ]

			if (nGameMode == 0) {
				TJAPlayer3.ConfigIni.bTokkunMode = false;
			} else {
				TJAPlayer3.ConfigIni.bTokkunMode = true;
			}

			#endregion

			#region [ AutoMode ]

			if (nAutoMode == 1) {
				TJAPlayer3.ConfigIni.bAutoPlay[player] = true;
			} else {
				TJAPlayer3.ConfigIni.bAutoPlay[player] = false;
			}

			#endregion

			#region [ Hitsounds ]

			TJAPlayer3.ConfigIni.nHitSounds[actual] = nOtoiro;
			hsInfo.tReloadHitSounds(nOtoiro, actual);

			#endregion

			#region [ Song speed ]

			TJAPlayer3.ConfigIni.nSongSpeed = (nSongSpeed + 5) * 2;

			#endregion
		}

		#region [ Balancing functions ]

		public float tGetScrollSpeedFactor(EBalancingType ebt = EBalancingType.SCORE, bool isMenu = false, int actual = 0) {
			var _compare = (isMenu) ? nSpeedCount != 1 : TJAPlayer3.ConfigIni.nScrollSpeed[actual] != 9;

			if (ebt == EBalancingType.SCORE)
				return (_compare) ? 0.9f : 1f;
			return 1f;
		}

		public float tGetSongSpeedFactor(EBalancingType ebt = EBalancingType.SCORE, bool isMenu = false, int actual = 0) {
			var _compare = ((isMenu) ? (nSongSpeed + 5) * 2 : TJAPlayer3.ConfigIni.nSongSpeed) / 20f;

			if (ebt == EBalancingType.SCORE || _compare <= 1f)
				return Math.Min(1f, (float)Math.Pow(_compare, 1.3));
			return Math.Max(1f, (float)Math.Pow(_compare, 0.7));
		}

		public float tGetJustFactor(EBalancingType ebt = EBalancingType.SCORE, bool isMenu = false, int actual = 0) {
			var _compare = (isMenu) ? nJust : TJAPlayer3.ConfigIni.bJust[actual];

			if (ebt == EBalancingType.SCORE)
				return (_compare == 2) ? 0.6f : 1f;

			return (_compare > 0) ? ((_compare > 1) ? 0.5f : 1.3f) : 1f;
		}

		public float tGetTimingFactor(EBalancingType ebt = EBalancingType.SCORE, bool isMenu = false, int actual = 0) {
			var _compare = (isMenu) ? nTiming - 2 : TJAPlayer3.ConfigIni.nTimingZones[actual] - 2;

			if (ebt == EBalancingType.SCORE)
				return (_compare < 0) ? (1f + 0.2f * _compare) : 1f;

			return 1f + 0.2f * _compare;
		}

		public float tGetDoronFactor(EBalancingType ebt = EBalancingType.SCORE, bool isMenu = false, int actual = 0) {
			var _compare = (isMenu) ? nStealth : (int)TJAPlayer3.ConfigIni.eSTEALTH[actual];

			if (ebt == EBalancingType.SCORE || _compare == 0)
				return 1f;

			// Doron : x1.1 coins, Stealth : x1.4 coins
			return 1f + 0.1f * (float)Math.Pow(_compare, 2);
		}

		public float tGetModMultiplier(EBalancingType ebt = EBalancingType.SCORE, bool isMenu = false, int player = 0) {
			float factor = 1f;
			int actual = TJAPlayer3.GetActualPlayer(player);

			//factor *= tGetScrollSpeedFactor(ebt, isMenu, actual);
			factor *= tGetSongSpeedFactor(ebt, isMenu, actual);
			factor *= tGetJustFactor(ebt, isMenu, actual);
			factor *= tGetTimingFactor(ebt, isMenu, actual);
			factor *= tGetDoronFactor(ebt, isMenu, actual);

			return ebt == EBalancingType.SCORE ? Math.Min(factor, 1f) : factor;
		}

		public enum EBalancingType {
			SCORE,
			COINS
		}

		#endregion

	}
}
