using System.Drawing;
using FDK;

namespace OpenTaiko;

class CActNewHeya : CActivity {
	public bool IsOpend { get; private set; }
	private CCachedFontRenderer MenuFont;

	private TitleTextureKey[] MenuTitleKeys = new TitleTextureKey[5];
	private TitleTextureKey[] ttkPuchiCharaNames;
	private TitleTextureKey[] ttkPuchiCharaAuthors;
	private TitleTextureKey[] ttkCharacterNames;
	private TitleTextureKey[] ttkCharacterAuthors;
	private TitleTextureKey ttkInfoSection;
	private TitleTextureKey[] ttkDanTitles;
	private TitleTextureKey[] ttkTitles;
	private string[] titlesKeys;

	public CCounter InFade;

	public CCounter CharaBoxAnime;

	private int CurrentIndex;

	private int CurrentMaxIndex;

	private int CurrentPlayer;

	private enum SelectableInfo {
		PlayerSelect,
		ModeSelect,
		Select
	}

	private enum ModeType {
		None = -1,
		PuchiChara,
		Chara,
		DanTitle,
		SubTitle
	}

	private SelectableInfo CurrentState;

	private ModeType CurrentMode = ModeType.None;

	private void SetState(SelectableInfo selectableInfo) {
		CurrentState = selectableInfo;
		switch (selectableInfo) {
			case SelectableInfo.PlayerSelect:
				CurrentIndex = 1;
				CurrentMaxIndex = OpenTaiko.ConfigIni.nPlayerCount + 1;
				break;
			case SelectableInfo.ModeSelect:
				CurrentIndex = 1;
				CurrentMaxIndex = 5;
				break;
			case SelectableInfo.Select:
				CurrentMode = (ModeType)(CurrentIndex - 1);
				switch (CurrentMode) {
					case ModeType.Chara:
						CurrentMaxIndex = OpenTaiko.Skin.Characters_Ptn;
						break;
					case ModeType.PuchiChara:
						CurrentMaxIndex = OpenTaiko.Skin.Puchichara_Ptn;
						break;
					case ModeType.DanTitle: {
							int amount = 1;
							if (OpenTaiko.SaveFileInstances[CurrentPlayer].data.DanTitles != null)
								amount += OpenTaiko.SaveFileInstances[CurrentPlayer].data.DanTitles.Count;

							this.ttkDanTitles = new TitleTextureKey[amount];

							// Silver Shinjin (default rank) always avaliable by default
							this.ttkDanTitles[0] = new TitleTextureKey("新人", this.MenuFont, Color.White, Color.Black, 1000);

							int idx = 1;
							if (OpenTaiko.SaveFileInstances[CurrentPlayer].data.DanTitles != null) {
								foreach (var item in OpenTaiko.SaveFileInstances[CurrentPlayer].data.DanTitles) {
									if (item.Value.isGold == true)
										this.ttkDanTitles[idx] = new TitleTextureKey(item.Key, this.MenuFont, Color.Gold, Color.Black, 1000);
									else
										this.ttkDanTitles[idx] = new TitleTextureKey(item.Key, this.MenuFont, Color.White, Color.Black, 1000);
									idx++;
								}
							}

							CurrentMaxIndex = amount;
						}
						break;
					case ModeType.SubTitle: {
							int amount = 1;
							if (OpenTaiko.SaveFileInstances[CurrentPlayer].data.UnlockedNameplateIds != null)
								amount += OpenTaiko.SaveFileInstances[CurrentPlayer].data.UnlockedNameplateIds.Count;

							this.ttkTitles = new TitleTextureKey[amount];
							this.titlesKeys = new string[amount];

							// Wood shojinsha (default title) always avaliable by default
							this.ttkTitles[0] = new TitleTextureKey("初心者", this.MenuFont, Color.Black, Color.Transparent, 1000);
							this.titlesKeys[0] = "初心者";

							int idx = 1;
							if (OpenTaiko.SaveFileInstances[CurrentPlayer].data.UnlockedNameplateIds != null) {
								foreach (var item in OpenTaiko.SaveFileInstances[CurrentPlayer].data.UnlockedNameplateIds) {
									var name = OpenTaiko.Databases.DBNameplateUnlockables.data[item];
									this.ttkTitles[idx] = new TitleTextureKey(name.nameplateInfo.cld.GetString(""), this.MenuFont, Color.Black, Color.Transparent, 1000);
									this.titlesKeys[idx] = name.nameplateInfo.cld.GetString("");
									idx++;
								}
							}

							CurrentMaxIndex = amount;
						}
						break;
				}
				CurrentIndex = 0;
				break;
		}
	}

	private void ChangeIndex(int change) {
		CurrentIndex += change;

		if (CurrentIndex < 0) CurrentIndex = CurrentMaxIndex - 1;
		else if (CurrentIndex >= CurrentMaxIndex) CurrentIndex = 0;
		if (CurrentState == SelectableInfo.Select) {
			switch (CurrentMode) {
				case ModeType.PuchiChara:
					tUpdateUnlockableTextPuchi();
					break;
				case ModeType.Chara:
					tUpdateUnlockableTextChara();
					break;
				case ModeType.DanTitle:
					break;
				case ModeType.SubTitle:
					break;
			}
		}
	}

	public void Open() {
		InFade = new CCounter(0, 255, 1.0, OpenTaiko.Timer);
		IsOpend = true;
		CurrentMode = ModeType.None;

		SetState(SelectableInfo.PlayerSelect);
	}

	public void Close() {
		IsOpend = false;
	}

	public override void Activate() {
		InFade = new CCounter();
		CharaBoxAnime = new CCounter();

		MenuTitleKeys[0] = new TitleTextureKey(CLangManager.LangInstance.GetString("MENU_RETURN"), MenuFont, Color.White, Color.Black, 9999);
		MenuTitleKeys[1] = new TitleTextureKey(CLangManager.LangInstance.GetString("HEYA_PUCHI"), MenuFont, Color.White, Color.Black, 9999);
		MenuTitleKeys[2] = new TitleTextureKey(CLangManager.LangInstance.GetString("HEYA_CHARA"), MenuFont, Color.White, Color.Black, 9999);
		MenuTitleKeys[3] = new TitleTextureKey(CLangManager.LangInstance.GetString("HEYA_DAN"), MenuFont, Color.White, Color.Black, 9999);
		MenuTitleKeys[4] = new TitleTextureKey(CLangManager.LangInstance.GetString("HEYA_NAMEPLATE"), MenuFont, Color.White, Color.Black, 9999);

		ttkPuchiCharaNames = new TitleTextureKey[OpenTaiko.Skin.Puchichara_Ptn];
		ttkPuchiCharaAuthors = new TitleTextureKey[OpenTaiko.Skin.Puchichara_Ptn];

		for (int i = 0; i < OpenTaiko.Skin.Puchichara_Ptn; i++) {
			var textColor = HRarity.tRarityToColor(OpenTaiko.Tx.Puchichara[i].metadata.Rarity);
			ttkPuchiCharaNames[i] = new TitleTextureKey(OpenTaiko.Tx.Puchichara[i].metadata.tGetName(), this.MenuFont, textColor, Color.Black, 1000);
			ttkPuchiCharaAuthors[i] = new TitleTextureKey(OpenTaiko.Tx.Puchichara[i].metadata.tGetAuthor(), this.MenuFont, Color.White, Color.Black, 1000);
		}


		ttkCharacterAuthors = new TitleTextureKey[OpenTaiko.Skin.Characters_Ptn];
		ttkCharacterNames = new TitleTextureKey[OpenTaiko.Skin.Characters_Ptn];

		for (int i = 0; i < OpenTaiko.Skin.Characters_Ptn; i++) {
			var textColor = HRarity.tRarityToColor(OpenTaiko.Tx.Characters[i].metadata.Rarity);
			ttkCharacterNames[i] = new TitleTextureKey(OpenTaiko.Tx.Characters[i].metadata.tGetName(), this.MenuFont, textColor, Color.Black, 1000);
			ttkCharacterAuthors[i] = new TitleTextureKey(OpenTaiko.Tx.Characters[i].metadata.tGetAuthor(), this.MenuFont, Color.White, Color.Black, 1000);
		}


		base.Activate();
	}

	public override void DeActivate() {

		base.DeActivate();
	}

	public override void CreateManagedResource() {
		this.MenuFont = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.Heya_Font_Scale);
		base.CreateManagedResource();
	}

	public override void ReleaseManagedResource() {
		MenuFont.Dispose();

		base.ReleaseManagedResource();
	}

	public override int Draw() {
		if ((OpenTaiko.Pad.bPressedDGB(EPad.Decide)) || ((OpenTaiko.ConfigIni.bEnterIsNotUsedInKeyAssignments && OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return)))) {
			switch (CurrentState) {
				case SelectableInfo.PlayerSelect: {
						switch (CurrentIndex) {
							case 0:
								Close();
								OpenTaiko.Skin.soundCancelSFX.tPlay();
								break;
							default: {
									CurrentPlayer = OpenTaiko.GetActualPlayer(CurrentIndex - 1);
									SetState(SelectableInfo.ModeSelect);
									OpenTaiko.Skin.soundDecideSFX.tPlay();
								}
								break;
						}
					}
					break;
				case SelectableInfo.ModeSelect: {
						switch (CurrentIndex) {
							case 0:
								SetState(SelectableInfo.PlayerSelect);
								OpenTaiko.Skin.soundCancelSFX.tPlay();
								break;
							default: {
									SetState(SelectableInfo.Select);
									OpenTaiko.Skin.soundDecideSFX.tPlay();
								}
								break;
						}
					}
					break;
				case SelectableInfo.Select: {
						switch (CurrentMode) {
							case ModeType.PuchiChara: {
									var ess = this.tSelectPuchi();

									if (ess == ESelectStatus.SELECTED) {
										//PuchiChara.tGetPuchiCharaIndexByName(p);
										//TJAPlayer3.NamePlateConfig.data.PuchiChara[iPlayer] = TJAPlayer3.Skin.Puchicharas_Name[iPuchiCharaCurrent];// iPuchiCharaCurrent;
										//TJAPlayer3.NamePlateConfig.tApplyHeyaChanges();
										OpenTaiko.SaveFileInstances[CurrentPlayer].data.PuchiChara = OpenTaiko.Skin.Puchicharas_Name[CurrentIndex];// iPuchiCharaCurrent;
										OpenTaiko.SaveFileInstances[CurrentPlayer].tApplyHeyaChanges();
										OpenTaiko.Skin.soundDecideSFX.tPlay();
										OpenTaiko.Tx.Puchichara[CurrentIndex].welcome.tPlay();

										SetState(SelectableInfo.PlayerSelect);
									} else if (ess == ESelectStatus.SUCCESS) {
										//TJAPlayer3.NamePlateConfig.data.UnlockedPuchicharas[iPlayer].Add(TJAPlayer3.Skin.Puchicharas_Name[iPuchiCharaCurrent]);
										//TJAPlayer3.NamePlateConfig.tSpendCoins(TJAPlayer3.Tx.Puchichara[iPuchiCharaCurrent].unlock.Values[0], iPlayer);
										OpenTaiko.SaveFileInstances[CurrentPlayer].data.UnlockedPuchicharas.Add(OpenTaiko.Skin.Puchicharas_Name[CurrentIndex]);
										if (OpenTaiko.Tx.Puchichara[CurrentIndex].unlock is CUnlockCH)
											OpenTaiko.SaveFileInstances[CurrentPlayer].tSpendCoins(OpenTaiko.Tx.Puchichara[CurrentIndex].unlock.Values[0]);
										else if (OpenTaiko.Tx.Puchichara[CurrentIndex].unlock is CUnlockAndComb || OpenTaiko.Tx.Puchichara[CurrentIndex].unlock is CUnlockOrComb)
											OpenTaiko.SaveFileInstances[CurrentPlayer].tSpendCoins(OpenTaiko.Tx.Puchichara[CurrentIndex].unlock.CoinStack);
										OpenTaiko.Skin.soundDecideSFX.tPlay();
									} else {
										OpenTaiko.Skin.soundError.tPlay();
									}
								}
								break;
							case ModeType.Chara: {
									var ess = this.tSelectChara();

									if (ess == ESelectStatus.SELECTED) {
										//TJAPlayer3.Tx.Loading?.t2D描画(18, 7);

										// Reload character, a bit time expensive but with a O(N) memory complexity instead of O(N * M)
										OpenTaiko.Tx.ReloadCharacter(OpenTaiko.SaveFileInstances[CurrentPlayer].data.Character, CurrentIndex, CurrentPlayer);
										OpenTaiko.SaveFileInstances[CurrentPlayer].data.Character = CurrentIndex;

										// Update the character
										OpenTaiko.SaveFileInstances[CurrentPlayer].tUpdateCharacterName(OpenTaiko.Skin.Characters_DirName[CurrentIndex]);

										// Welcome voice using Sanka
										OpenTaiko.Skin.soundDecideSFX.tPlay();
										OpenTaiko.Skin.voiceTitleSanka[CurrentPlayer]?.tPlay();

										CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.NORMAL);

										OpenTaiko.SaveFileInstances[CurrentPlayer].tApplyHeyaChanges();

										SetState(SelectableInfo.PlayerSelect);
										CurrentMode = ModeType.None;
									} else if (ess == ESelectStatus.SUCCESS) {
										OpenTaiko.SaveFileInstances[CurrentPlayer].data.UnlockedCharacters.Add(OpenTaiko.Skin.Characters_DirName[CurrentIndex]);
										if (OpenTaiko.Tx.Characters[CurrentIndex].unlock is CUnlockCH)
											OpenTaiko.SaveFileInstances[CurrentPlayer].tSpendCoins(OpenTaiko.Tx.Characters[CurrentIndex].unlock.Values[0]);
										else if (OpenTaiko.Tx.Characters[CurrentIndex].unlock is CUnlockAndComb || OpenTaiko.Tx.Characters[CurrentIndex].unlock is CUnlockOrComb)
											OpenTaiko.SaveFileInstances[CurrentPlayer].tSpendCoins(OpenTaiko.Tx.Characters[CurrentIndex].unlock.CoinStack);
										OpenTaiko.Skin.soundDecideSFX.tPlay();
									} else {
										OpenTaiko.Skin.soundError.tPlay();
									}
								}
								break;
							case ModeType.DanTitle: {
									bool iG = false;
									int cs = 0;

									if (CurrentIndex > 0) {
										iG = OpenTaiko.SaveFileInstances[CurrentPlayer].data.DanTitles[this.ttkDanTitles[CurrentIndex].str].isGold;
										cs = OpenTaiko.SaveFileInstances[CurrentPlayer].data.DanTitles[this.ttkDanTitles[CurrentIndex].str].clearStatus;
									}

									OpenTaiko.SaveFileInstances[CurrentPlayer].data.Dan = this.ttkDanTitles[CurrentIndex].str;
									OpenTaiko.SaveFileInstances[CurrentPlayer].data.DanGold = iG;
									OpenTaiko.SaveFileInstances[CurrentPlayer].data.DanType = cs;

									OpenTaiko.NamePlate.tNamePlateRefreshTitles(CurrentPlayer);

									OpenTaiko.SaveFileInstances[CurrentPlayer].tApplyHeyaChanges();

									OpenTaiko.Skin.soundDecideSFX.tPlay();
									SetState(SelectableInfo.PlayerSelect);
								}
								break;
							case ModeType.SubTitle: {

									if (CurrentIndex == 0) {
										OpenTaiko.SaveFileInstances[CurrentPlayer].data.TitleType = 0;
										OpenTaiko.SaveFileInstances[CurrentPlayer].data.TitleId = -1;
										OpenTaiko.SaveFileInstances[CurrentPlayer].data.TitleRarityInt = 1;
										OpenTaiko.SaveFileInstances[CurrentPlayer].data.Title = "初心者";
									} else if (OpenTaiko.SaveFileInstances[CurrentPlayer].data.UnlockedNameplateIds != null &&
											   OpenTaiko.Databases.DBNameplateUnlockables.data.ContainsKey(OpenTaiko.SaveFileInstances[CurrentPlayer].data.UnlockedNameplateIds[CurrentIndex - 1])) {
										var id = OpenTaiko.SaveFileInstances[CurrentPlayer].data.UnlockedNameplateIds[CurrentIndex - 1];
										var nameplate = OpenTaiko.Databases.DBNameplateUnlockables.data[id];

										OpenTaiko.SaveFileInstances[CurrentPlayer].data.TitleId = id;
										OpenTaiko.SaveFileInstances[CurrentPlayer].data.Title = nameplate.nameplateInfo.cld.GetString("");
										OpenTaiko.SaveFileInstances[CurrentPlayer].data.TitleRarityInt = HRarity.tRarityToLangInt(nameplate.rarity);
										OpenTaiko.SaveFileInstances[CurrentPlayer].data.TitleType = nameplate.nameplateInfo.iType;
									} else {
										OpenTaiko.SaveFileInstances[CurrentPlayer].data.TitleType = -1;
										OpenTaiko.SaveFileInstances[CurrentPlayer].data.TitleId = -1;
										OpenTaiko.SaveFileInstances[CurrentPlayer].data.TitleRarityInt = 1;
										OpenTaiko.SaveFileInstances[CurrentPlayer].data.Title = "";
									}

									OpenTaiko.NamePlate.tNamePlateRefreshTitles(CurrentPlayer);

									OpenTaiko.SaveFileInstances[CurrentPlayer].tApplyHeyaChanges();

									OpenTaiko.Skin.soundDecideSFX.tPlay();
									SetState(SelectableInfo.PlayerSelect);
								}
								break;
						}
					}
					break;
			}
		} else if ((OpenTaiko.Pad.bPressedDGB(EPad.Cancel) || OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape))) {
			Close();
			OpenTaiko.Skin.soundCancelSFX.tPlay();
		} else if (OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, EPad.LeftChange)
				   || OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.LeftArrow)) {
			ChangeIndex(-1);
			OpenTaiko.Skin.soundChangeSFX.tPlay();
		} else if (OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, EPad.RightChange)
				   || OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.RightArrow)) {
			ChangeIndex(1);
			OpenTaiko.Skin.soundChangeSFX.tPlay();
		}

		InFade.Tick();

		if (OpenTaiko.Tx.Tile_Black != null) {
			OpenTaiko.Tx.Tile_Black.Opacity = InFade.CurrentValue / 2;
			for (int i = 0; i <= (GameWindowSize.Width / OpenTaiko.Tx.Tile_Black.szTextureSize.Width); i++)        // #23510 2010.10.31 yyagi: change "clientSize.Width" to "640" to fix FIFO drawing size
			{
				for (int j = 0; j <= (GameWindowSize.Height / OpenTaiko.Tx.Tile_Black.szTextureSize.Height); j++)  // #23510 2010.10.31 yyagi: change "clientSize.Height" to "480" to fix FIFO drawing size
				{
					OpenTaiko.Tx.Tile_Black.t2D描画(i * OpenTaiko.Tx.Tile_Black.szTextureSize.Width, j * OpenTaiko.Tx.Tile_Black.szTextureSize.Height);
				}
			}
		}


		switch (CurrentState) {
			case SelectableInfo.PlayerSelect:
				if (CurrentIndex == 0) {
					OpenTaiko.Tx.NewHeya_Close_Select.t2D描画(OpenTaiko.Skin.SongSelect_NewHeya_Close_Select[0], OpenTaiko.Skin.SongSelect_NewHeya_Close_Select[1]);
				} else {
					OpenTaiko.Tx.NewHeya_PlayerPlate_Select.t2D描画(OpenTaiko.Skin.SongSelect_NewHeya_PlayerPlate_X[CurrentIndex - 1], OpenTaiko.Skin.SongSelect_NewHeya_PlayerPlate_Y[CurrentIndex - 1]);
				}
				break;
			case SelectableInfo.ModeSelect: {
					OpenTaiko.Tx.NewHeya_ModeBar_Select.t2D描画(OpenTaiko.Skin.SongSelect_NewHeya_ModeBar_X[CurrentIndex], OpenTaiko.Skin.SongSelect_NewHeya_ModeBar_Y[CurrentIndex]);
				}
				break;
			case SelectableInfo.Select: {
					switch (CurrentMode) {
						case ModeType.Chara:
							for (int i = 1; i < OpenTaiko.Skin.SongSelect_NewHeya_Box_Count - 1; i++) {
								int x = OpenTaiko.Skin.SongSelect_NewHeya_Box_X[i];
								int y = OpenTaiko.Skin.SongSelect_NewHeya_Box_Y[i];
								int index = i - (OpenTaiko.Skin.SongSelect_NewHeya_Box_Count / 2) + CurrentIndex;
								while (index < 0) {
									index += CurrentMaxIndex;
								}
								while (index >= CurrentMaxIndex) {
									index -= CurrentMaxIndex;
								}
								OpenTaiko.Tx.NewHeya_Box.t2D描画(x, y);


								float charaRatioX = 1.0f;
								float charaRatioY = 1.0f;
								if (OpenTaiko.Skin.Characters_Resolution[index] != null) {
									charaRatioX = OpenTaiko.Skin.Resolution[0] / (float)OpenTaiko.Skin.Characters_Resolution[index][0];
									charaRatioY = OpenTaiko.Skin.Resolution[1] / (float)OpenTaiko.Skin.Characters_Resolution[index][1];
								}

								if (OpenTaiko.Tx.Characters_Heya_Preview[index] != null) {
									OpenTaiko.Tx.Characters_Heya_Preview[index].vcScaleRatio.X = charaRatioX;
									OpenTaiko.Tx.Characters_Heya_Preview[index].vcScaleRatio.Y = charaRatioY;
								}

								OpenTaiko.Tx.Characters_Heya_Preview[index]?.t2D拡大率考慮中央基準描画(x + OpenTaiko.Skin.SongSelect_NewHeya_Box_Chara_Offset[0], y + OpenTaiko.Skin.SongSelect_NewHeya_Box_Chara_Offset[1]);
								OpenTaiko.Tx.Characters_Heya_Preview[index]?.tUpdateColor4(CConversion.ColorToColor4(Color.White));

								if (ttkCharacterNames[index] != null) {
									CTexture tmpTex = TitleTextureKey.ResolveTitleTexture(ttkCharacterNames[index]);

									tmpTex.t2D拡大率考慮上中央基準描画(x + OpenTaiko.Skin.SongSelect_NewHeya_Box_Name_Offset[0], y + OpenTaiko.Skin.SongSelect_NewHeya_Box_Name_Offset[1]);
								}

								if (ttkCharacterAuthors[index] != null) {
									CTexture tmpTex = TitleTextureKey.ResolveTitleTexture(ttkCharacterAuthors[index]);

									tmpTex.t2D拡大率考慮上中央基準描画(x + OpenTaiko.Skin.SongSelect_NewHeya_Box_Author_Offset[0], y + OpenTaiko.Skin.SongSelect_NewHeya_Box_Author_Offset[1]);
								}

								if (OpenTaiko.Tx.Characters[index].unlock != null
									&& !OpenTaiko.SaveFileInstances[CurrentPlayer].data.UnlockedCharacters.Contains(OpenTaiko.Skin.Characters_DirName[index])) {
									OpenTaiko.Tx.NewHeya_Lock?.t2D描画(x + OpenTaiko.Skin.SongSelect_NewHeya_Lock_Offset[0], y + OpenTaiko.Skin.SongSelect_NewHeya_Lock_Offset[1]);

									if (this.ttkInfoSection != null)
										TitleTextureKey.ResolveTitleTexture(this.ttkInfoSection)
											.t2D拡大率考慮上中央基準描画(x + OpenTaiko.Skin.SongSelect_NewHeya_InfoSection_Offset[0], y + OpenTaiko.Skin.SongSelect_NewHeya_InfoSection_Offset[1]);
								}
							}
							break;
						case ModeType.PuchiChara:
							for (int i = 1; i < OpenTaiko.Skin.SongSelect_NewHeya_Box_Count - 1; i++) {
								int x = OpenTaiko.Skin.SongSelect_NewHeya_Box_X[i];
								int y = OpenTaiko.Skin.SongSelect_NewHeya_Box_Y[i];
								int index = i - (OpenTaiko.Skin.SongSelect_NewHeya_Box_Count / 2) + CurrentIndex;
								while (index < 0) {
									index += CurrentMaxIndex;
								}
								while (index >= CurrentMaxIndex) {
									index -= CurrentMaxIndex;
								}
								OpenTaiko.Tx.NewHeya_Box.t2D描画(x, y);

								OpenTaiko.stageSongSelect.PuchiChara.DrawPuchichara(index,
									x + OpenTaiko.Skin.SongSelect_NewHeya_Box_Chara_Offset[0], y + OpenTaiko.Skin.SongSelect_NewHeya_Box_Chara_Offset[1],
									OpenTaiko.Skin.Resolution[1] / 1080.0f, 255, true);

								OpenTaiko.Tx.Puchichara[index].tx?.tUpdateColor4(CConversion.ColorToColor4(Color.White));


								if (ttkPuchiCharaNames[index] != null) {
									CTexture tmpTex = TitleTextureKey.ResolveTitleTexture(ttkPuchiCharaNames[index]);

									tmpTex.t2D拡大率考慮上中央基準描画(x + OpenTaiko.Skin.SongSelect_NewHeya_Box_Name_Offset[0], y + OpenTaiko.Skin.SongSelect_NewHeya_Box_Name_Offset[1]);
								}

								if (ttkPuchiCharaAuthors[index] != null) {
									CTexture tmpTex = TitleTextureKey.ResolveTitleTexture(ttkPuchiCharaAuthors[index]);

									tmpTex.t2D拡大率考慮上中央基準描画(x + OpenTaiko.Skin.SongSelect_NewHeya_Box_Author_Offset[0], y + OpenTaiko.Skin.SongSelect_NewHeya_Box_Author_Offset[1]);
								}

								if (OpenTaiko.Tx.Puchichara[index].unlock != null
									&& !OpenTaiko.SaveFileInstances[CurrentPlayer].data.UnlockedPuchicharas.Contains(OpenTaiko.Skin.Puchicharas_Name[index])) {
									OpenTaiko.Tx.NewHeya_Lock?.t2D描画(x + OpenTaiko.Skin.SongSelect_NewHeya_Lock_Offset[0], y + OpenTaiko.Skin.SongSelect_NewHeya_Lock_Offset[1]);

									if (this.ttkInfoSection != null)
										TitleTextureKey.ResolveTitleTexture(this.ttkInfoSection)
											.t2D拡大率考慮上中央基準描画(x + OpenTaiko.Skin.SongSelect_NewHeya_InfoSection_Offset[0], y + OpenTaiko.Skin.SongSelect_NewHeya_InfoSection_Offset[1]);
								}
							}
							break;
						case ModeType.SubTitle:
							for (int i = 1; i < OpenTaiko.Skin.SongSelect_NewHeya_Box_Count - 1; i++) {
								int x = OpenTaiko.Skin.SongSelect_NewHeya_Box_X[i];
								int y = OpenTaiko.Skin.SongSelect_NewHeya_Box_Y[i];
								int index = i - (OpenTaiko.Skin.SongSelect_NewHeya_Box_Count / 2) + CurrentIndex;
								while (index < 0) {
									index += CurrentMaxIndex;
								}
								while (index >= CurrentMaxIndex) {
									index -= CurrentMaxIndex;
								}
								CTexture tmpTex = TitleTextureKey.ResolveTitleTexture(this.ttkTitles[index]);

								if (i != 0) {
									tmpTex.color4 = CConversion.ColorToColor4(Color.DarkGray);
								} else {
									tmpTex.color4 = CConversion.ColorToColor4(Color.White);
								}

								OpenTaiko.Tx.NewHeya_Box.t2D描画(x, y);

								x += OpenTaiko.Skin.SongSelect_NewHeya_Box_Chara_Offset[0];
								y += OpenTaiko.Skin.SongSelect_NewHeya_Box_Chara_Offset[1];

								int iType = -1;
								int rarity = 1;
								int id = -1;

								if (index == 0) {
									iType = 0;
								} else if (OpenTaiko.SaveFileInstances[CurrentPlayer].data.UnlockedNameplateIds != null &&
										   OpenTaiko.Databases.DBNameplateUnlockables.data.ContainsKey(OpenTaiko.SaveFileInstances[CurrentPlayer].data.UnlockedNameplateIds[index - 1])) {
									id = OpenTaiko.SaveFileInstances[CurrentPlayer].data.UnlockedNameplateIds[index - 1];
									var nameplate = OpenTaiko.Databases.DBNameplateUnlockables.data[id];
									iType = nameplate.nameplateInfo.iType;
									rarity = HRarity.tRarityToLangInt(nameplate.rarity);
								}



								tmpTex.t2D拡大率考慮上中央基準描画(x + OpenTaiko.Skin.Heya_Side_Menu_Font_Offset[0], y + OpenTaiko.Skin.Heya_Side_Menu_Font_Offset[1]);

								OpenTaiko.NamePlate.lcNamePlate.DrawTitlePlate(x, y, 255, iType, tmpTex, rarity, id);

							}
							break;
						case ModeType.DanTitle:
							for (int i = 1; i < OpenTaiko.Skin.SongSelect_NewHeya_Box_Count - 1; i++) {
								int x = OpenTaiko.Skin.SongSelect_NewHeya_Box_X[i];
								int y = OpenTaiko.Skin.SongSelect_NewHeya_Box_Y[i];
								int index = i - (OpenTaiko.Skin.SongSelect_NewHeya_Box_Count / 2) + CurrentIndex;
								while (index < 0) {
									index += CurrentMaxIndex;
								}
								while (index >= CurrentMaxIndex) {
									index -= CurrentMaxIndex;
								}
								CTexture tmpTex = TitleTextureKey.ResolveTitleTexture(this.ttkDanTitles[index]);

								if (i != 0) {
									tmpTex.color4 = CConversion.ColorToColor4(Color.DarkGray);
								} else {
									tmpTex.color4 = CConversion.ColorToColor4(Color.White);
								}

								OpenTaiko.Tx.NewHeya_Box.t2D描画(x, y);

								x += OpenTaiko.Skin.SongSelect_NewHeya_Box_Chara_Offset[0];
								y += OpenTaiko.Skin.SongSelect_NewHeya_Box_Chara_Offset[1];

								int danGrade = 0;
								if (index > 0) {
									danGrade = OpenTaiko.SaveFileInstances[CurrentPlayer].data.DanTitles[this.ttkDanTitles[index].str].clearStatus;
								}

								OpenTaiko.NamePlate.lcNamePlate.DrawDan(x, y, 255, danGrade, tmpTex);

								/*
								TJAPlayer3.NamePlate.tNamePlateDisplayNamePlateBase(
									x - TJAPlayer3.Tx.NamePlateBase.szTextureSize.Width / 2,
									y - TJAPlayer3.Tx.NamePlateBase.szTextureSize.Height / 24,
									(8 + danGrade));
								TJAPlayer3.Tx.NamePlateBase.color4 = CConversion.ColorToColor4(Color.White);

								tmpTex.t2D拡大率考慮上中央基準描画(x + TJAPlayer3.Skin.Heya_Side_Menu_Font_Offset[0], y + TJAPlayer3.Skin.Heya_Side_Menu_Font_Offset[1]);
								*/
							}
							break;
					}
				}
				break;
		}

		OpenTaiko.Tx.NewHeya_Close.t2D描画(0, 0);

		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			OpenTaiko.Tx.NewHeya_PlayerPlate[OpenTaiko.GetActualPlayer(i)].t2D描画(OpenTaiko.Skin.SongSelect_NewHeya_PlayerPlate_X[i], OpenTaiko.Skin.SongSelect_NewHeya_PlayerPlate_Y[i]);
		}

		for (int i = 0; i < 5; i++) {
			OpenTaiko.Tx.NewHeya_ModeBar.t2D描画(OpenTaiko.Skin.SongSelect_NewHeya_ModeBar_X[i], OpenTaiko.Skin.SongSelect_NewHeya_ModeBar_Y[i]);
			int title_x = OpenTaiko.Skin.SongSelect_NewHeya_ModeBar_X[i] + OpenTaiko.Skin.SongSelect_NewHeya_ModeBar_Font_Offset[0];
			int title_y = OpenTaiko.Skin.SongSelect_NewHeya_ModeBar_Y[i] + OpenTaiko.Skin.SongSelect_NewHeya_ModeBar_Font_Offset[1];
			TitleTextureKey.ResolveTitleTexture(MenuTitleKeys[i], false).t2D拡大率考慮中央基準描画(title_x, title_y);
		}

		return base.Draw();
	}

	/*
	 *  FAILED : Selection/Purchase failed (failed condition)
	 *  SUCCESS : Purchase succeed (without selection)
	 *  SELECTED : Selection succeed
	 */
	private enum ESelectStatus {
		FAILED,
		SUCCESS,
		SELECTED
	};

	private ESelectStatus tSelectPuchi() {
		// Add "If unlocked" to select directly

		if (OpenTaiko.Tx.Puchichara[CurrentIndex].unlock != null
			&& !OpenTaiko.SaveFileInstances[CurrentPlayer].data.UnlockedPuchicharas.Contains(OpenTaiko.Skin.Puchicharas_Name[CurrentIndex])) {
			(bool, string?) response = OpenTaiko.Tx.Puchichara[CurrentIndex].unlock.tConditionMet(CurrentPlayer);
			//tConditionMet(
			//new int[] { TJAPlayer3.SaveFileInstances[TJAPlayer3.SaveFile].data.Medals });

			Color responseColor = (response.Item1) ? Color.Lime : Color.Red;

			// Send coins here for the unlock, considering that only coin-paid puchicharas can be unlocked directly from the Heya menu

			this.ttkInfoSection = new TitleTextureKey(response.Item2 ?? this.ttkInfoSection.str, this.MenuFont, responseColor, Color.Black, 1000);

			return (response.Item1) ? ESelectStatus.SUCCESS : ESelectStatus.FAILED;
		}

		this.ttkInfoSection = null;
		return ESelectStatus.SELECTED;
	}

	private void tUpdateUnlockableTextPuchi() {
		#region [Check unlockable]

		if (OpenTaiko.Tx.Puchichara[CurrentIndex].unlock != null
			&& !OpenTaiko.SaveFileInstances[CurrentPlayer].data.UnlockedPuchicharas.Contains(OpenTaiko.Skin.Puchicharas_Name[CurrentIndex])) {
			this.ttkInfoSection = new TitleTextureKey(OpenTaiko.Tx.Puchichara[CurrentIndex].unlock.tConditionMessage()
				, this.MenuFont, Color.White, Color.Black, 1000);
		} else
			this.ttkInfoSection = null;

		#endregion
	}
	private void tUpdateUnlockableTextChara() {
		#region [Check unlockable]

		if (OpenTaiko.Tx.Characters[CurrentIndex].unlock != null
			&& !OpenTaiko.SaveFileInstances[CurrentPlayer].data.UnlockedCharacters.Contains(OpenTaiko.Skin.Characters_DirName[CurrentIndex])) {
			this.ttkInfoSection = new TitleTextureKey(OpenTaiko.Tx.Characters[CurrentIndex].unlock.tConditionMessage()
				, this.MenuFont, Color.White, Color.Black, 1000);
		} else
			this.ttkInfoSection = null;

		#endregion
	}

	private ESelectStatus tSelectChara() {
		// Add "If unlocked" to select directly

		if (OpenTaiko.Tx.Characters[CurrentIndex].unlock != null
			&& !OpenTaiko.SaveFileInstances[CurrentPlayer].data.UnlockedCharacters.Contains(OpenTaiko.Skin.Characters_DirName[CurrentIndex])) {
			(bool, string?) response = OpenTaiko.Tx.Characters[CurrentIndex].unlock.tConditionMet(CurrentPlayer);
			//TJAPlayer3.Tx.Characters[iCharacterCurrent].unlock.tConditionMet(
			//new int[] { TJAPlayer3.SaveFileInstances[TJAPlayer3.SaveFile].data.Medals });

			Color responseColor = (response.Item1) ? Color.Lime : Color.Red;

			// Send coins here for the unlock, considering that only coin-paid puchicharas can be unlocked directly from the Heya menu

			this.ttkInfoSection = new TitleTextureKey(response.Item2 ?? this.ttkInfoSection.str, this.MenuFont, responseColor, Color.Black, 1000);

			return (response.Item1) ? ESelectStatus.SUCCESS : ESelectStatus.FAILED;
		}

		this.ttkInfoSection = null;
		return ESelectStatus.SELECTED;
	}
}
