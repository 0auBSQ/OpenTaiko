﻿using System.Diagnostics;
using System.Drawing;
using System.Text;
using FDK;
using static OpenTaiko.CActSelect曲リスト;

namespace OpenTaiko {
	class CStageOnlineLounge : CStage {

		public CStageOnlineLounge() {
			base.eStageID = EStage.OnlineLounge;
			base.ePhaseID = CStage.EPhase.Common_NORMAL;

			// Load CActivity objects here
			// base.list子Activities.Add(this.act = new CAct());

			base.ChildActivities.Add(this.actFOtoTitle = new CActFIFOBlack());

		}

		private static string GetMenuName(int menu) {
			switch (menu) {
				case (int)ECurrentMenu.RETURN:
				default:
					return CLangManager.LangInstance.GetString("ONLINE_EXIT");
				case (int)ECurrentMenu.MAIN:
					return CLangManager.LangInstance.GetString("MENU_RETURN");
				case (int)ECurrentMenu.CDN_SELECT:
					return CLangManager.LangInstance.GetString("ONLINE_DOWNLOAD");
				case (int)ECurrentMenu.CDN_OPTION:
					return CLangManager.LangInstance.GetString("ONLINE_DOWNLOAD_CDN");
				case (int)ECurrentMenu.CDN_SONGS:
					return CLangManager.LangInstance.GetString("ONLINE_DOWNLOAD_SONG");
				case (int)ECurrentMenu.CDN_CHARACTERS:
					return CLangManager.LangInstance.GetString("ONLINE_DOWNLOAD_CHARA");
				case (int)ECurrentMenu.CDN_PUCHICHARAS:
					return CLangManager.LangInstance.GetString("ONLINE_DOWNLOAD_PUCHI");
				case (int)ECurrentMenu.MULTI_SELECT:
					return CLangManager.LangInstance.GetString("ONLINE_MULTIPLAYER");
			}
		}

		public override void Activate() {
			// On activation

			if (base.IsActivated)
				return;

			base.ePhaseID = CStage.EPhase.Common_NORMAL;
			this.eフェードアウト完了時の戻り値 = EReturnValue.Continuation;

			OpenTaiko.Skin.soundOnlineLoungeBGM?.tPlay();

			this.currentMenu = ECurrentMenu.MAIN;
			this.menuPointer = ECurrentMenu.CDN_SELECT;
			this.menus = new CMenuInfo[(int)ECurrentMenu.TOTAL];

			for (int i = 0; i < (int)ECurrentMenu.TOTAL; i++)
				this.menus[i] = new CMenuInfo(GetMenuName(i));



			dbCDN = OpenTaiko.Databases.DBCDN;
			dbCDNData = null;

			IsDownloading = false;

			#region [Main menu]

			this.ttkMainMenuOpt = new TitleTextureKey[3];

			this.ttkMainMenuOpt[0] = new TitleTextureKey(GetMenuName((int)ECurrentMenu.RETURN), this.pfOLFont, Color.White, Color.DarkRed, 1000);
			this.ttkMainMenuOpt[1] = new TitleTextureKey(GetMenuName((int)ECurrentMenu.CDN_SELECT), this.pfOLFont, Color.White, Color.DarkRed, 1000);
			this.ttkMainMenuOpt[2] = new TitleTextureKey(GetMenuName((int)ECurrentMenu.MULTI_SELECT), this.pfOLFont, Color.White, Color.DarkRed, 1000);

			this.mainMenu = new ECurrentMenu[] { ECurrentMenu.RETURN, ECurrentMenu.CDN_SELECT, ECurrentMenu.MULTI_SELECT };

			this.mainMenuIndex = 0;

			#endregion

			#region [CDN Select]

			int keyCount = dbCDN.data.Count;

			this.ttkCDNSelectOpt = new TitleTextureKey[keyCount + 1];

			this.ttkCDNSelectOpt[0] = new TitleTextureKey(GetMenuName((int)ECurrentMenu.MAIN), this.pfOLFont, Color.White, Color.DarkRed, 1000);

			for (int i = 0; i < keyCount; i++) {
				this.ttkCDNSelectOpt[i + 1] = new TitleTextureKey(dbCDN.data.ElementAt(i).Key, this.pfOLFont, Color.White, Color.DarkRed, 1000);
			}

			this.CDNSelectIndex = 0;

			#endregion

			#region [CDN Option]

			this.ttkCDNOptionOpt = new TitleTextureKey[4];

			this.ttkCDNOptionOpt[0] = new TitleTextureKey(GetMenuName((int)ECurrentMenu.MAIN), this.pfOLFont, Color.White, Color.DarkRed, 1000);
			this.ttkCDNOptionOpt[1] = new TitleTextureKey(GetMenuName((int)ECurrentMenu.CDN_SONGS), this.pfOLFont, Color.White, Color.DarkRed, 1000);
			this.ttkCDNOptionOpt[2] = new TitleTextureKey(GetMenuName((int)ECurrentMenu.CDN_CHARACTERS), this.pfOLFont, Color.White, Color.DarkRed, 1000);
			this.ttkCDNOptionOpt[3] = new TitleTextureKey(GetMenuName((int)ECurrentMenu.CDN_PUCHICHARAS), this.pfOLFont, Color.White, Color.DarkRed, 1000);

			this.cdnOptMenu = new ECurrentMenu[] { ECurrentMenu.CDN_SELECT, ECurrentMenu.CDN_SONGS, ECurrentMenu.CDN_CHARACTERS, ECurrentMenu.CDN_PUCHICHARAS };

			this.cdnOptMenuIndex = 0;

			#endregion

			Background = new ScriptBG(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.ONLINELOUNGE}Script.lua"));
			Background.Init();

			base.Activate();
		}

		public override void DeActivate() {
			// On de-activation

			OpenTaiko.tDisposeSafely(ref Background);

			OpenTaiko.Songs管理.UpdateDownloadBox();

			base.DeActivate();
		}

		public override void CreateManagedResource() {
			// Ressource allocation

			this.pfOLFont = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.OnlineLounge_Font_OLFont);
			this.pfOLFontLarge = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.OnlineLounge_Font_OLFontLarge);

			base.CreateManagedResource();
		}

		public override void ReleaseManagedResource() {
			// Ressource freeing
			this.pfOLFont?.Dispose();
			this.pfOLFontLarge?.Dispose();

			base.ReleaseManagedResource();
		}

		public override int Draw() {
			Background.Update();
			Background.Draw();

			//OnlineLounge_Background.t2D描画(0, 0);

			#region [Menus]


			#region [Base Menus]

			TitleTextureKey[] _ref = this.ttkMainMenuOpt;
			int _selector = mainMenuIndex;

			if (currentMenu == ECurrentMenu.CDN_SELECT) {
				_ref = this.ttkCDNSelectOpt;
				_selector = CDNSelectIndex;
			} else if (currentMenu == ECurrentMenu.CDN_OPTION) {
				_ref = this.ttkCDNOptionOpt;
				_selector = cdnOptMenuIndex;
			}


			if (currentMenu == ECurrentMenu.MAIN
				|| currentMenu == ECurrentMenu.CDN_SELECT
				|| currentMenu == ECurrentMenu.CDN_OPTION) {
				int baseX = OpenTaiko.Skin.OnlineLounge_Side_Menu[0] - _ref.Length * (OpenTaiko.Skin.OnlineLounge_Side_Menu_Move[0] / 2);
				int baseY = OpenTaiko.Skin.OnlineLounge_Side_Menu[1] - _ref.Length * (OpenTaiko.Skin.OnlineLounge_Side_Menu_Move[1] / 2);

				for (int i = 0; i < _ref.Length; i++) {
					CTexture tmpTex = TitleTextureKey.ResolveTitleTexture(_ref[i]);

					if (_selector != i) {
						tmpTex.color4 = CConversion.ColorToColor4(Color.DarkGray);
						OpenTaiko.Tx.OnlineLounge_Side_Menu?.tUpdateColor4(CConversion.ColorToColor4(Color.DarkGray));
					} else {
						tmpTex.color4 = CConversion.ColorToColor4(Color.White);
						OpenTaiko.Tx.OnlineLounge_Side_Menu?.tUpdateColor4(CConversion.ColorToColor4(Color.White));
					}

					OpenTaiko.Tx.OnlineLounge_Side_Menu?.t2D拡大率考慮上中央基準描画(baseX + OpenTaiko.Skin.OnlineLounge_Side_Menu_Move[0] * i,
						baseY + OpenTaiko.Skin.OnlineLounge_Side_Menu_Move[1] * i);
					tmpTex.t2D拡大率考慮上中央基準描画(
						baseX + OpenTaiko.Skin.OnlineLounge_Side_Menu_Text_Offset[0] + OpenTaiko.Skin.OnlineLounge_Side_Menu_Move[0] * i,
						baseY + OpenTaiko.Skin.OnlineLounge_Side_Menu_Text_Offset[1] + OpenTaiko.Skin.OnlineLounge_Side_Menu_Move[1] * i);
				}
			}

			#endregion

			#region [Song list menu]

			if (currentMenu == ECurrentMenu.CDN_SONGS) {
				_ref = this.ttkCDNSongList;
				_selector = cdnSongListIndex;

				int baseX = OpenTaiko.Skin.OnlineLounge_Song[0];
				int baseY = OpenTaiko.Skin.OnlineLounge_Song[1];

				for (int i = -4; i < 4; i++) {
					int pos = (_ref.Length * 5 + _selector + i) % _ref.Length;

					CTexture tmpTex = TitleTextureKey.ResolveTitleTexture(_ref[pos]);
					CTexture tmpSubtitle = TitleTextureKey.ResolveTitleTexture(ttkCDNSongSubtitles[pos]);

					var _color = CConversion.ColorToColor4(Color.DarkGray);

					if (i == 0)
						_color = CConversion.ColorToColor4(Color.White);
					if (pos > 0 && i != 0) {
						var song = apiMethods.FetchedSongsList[pos - 1];
						var downloadLink = GetDownloadLink(song);

						if (CSongDict.tContainsSongUrl(downloadLink))
							_color = CConversion.ColorToColor4(Color.DimGray);
					}


					tmpTex.color4 = _color;
					tmpSubtitle.color4 = _color;

					int x = baseX + OpenTaiko.Skin.OnlineLounge_Song_Move[0] * i;
					int y = baseY + OpenTaiko.Skin.OnlineLounge_Song_Move[1] * i;

					if (pos == 0) {
						OpenTaiko.Tx.OnlineLounge_Return_Box?.tUpdateColor4(_color);
						OpenTaiko.Tx.OnlineLounge_Return_Box?.t2D拡大率考慮上中央基準描画(x, y);
					} else {
						OpenTaiko.Tx.OnlineLounge_Song_Box?.tUpdateColor4(_color);
						OpenTaiko.Tx.OnlineLounge_Song_Box?.t2D拡大率考慮上中央基準描画(x, y);
					}


					tmpTex.t2D拡大率考慮上中央基準描画(x + OpenTaiko.Skin.OnlineLounge_Song_Title_Offset[0], y + OpenTaiko.Skin.OnlineLounge_Song_Title_Offset[1]);
					tmpSubtitle.t2D拡大率考慮上中央基準描画(x + OpenTaiko.Skin.OnlineLounge_Song_SubTitle_Offset[0], y + OpenTaiko.Skin.OnlineLounge_Song_SubTitle_Offset[1]);

					if (pos != 0 && i == 0) {
						OpenTaiko.Tx.OnlineLounge_Context.t2D描画(0, 0);

						var song_ = apiMethods.FetchedSongsList[pos - 1];

						int[] diffs = new int[]
						{
							song_.D0,
							song_.D1,
							song_.D2,
							song_.D3,
							song_.D4,
							song_.D5,
							song_.D6,
						};

						#region [Charter Name]

						if (song_.charter != null && song_.charter.charter_name != null && song_.charter.charter_name != "") {
							var charter_ = TitleTextureKey.ResolveTitleTexture(
									new TitleTextureKey("Charter : " + song_.charter.charter_name, this.pfOLFontLarge, Color.White, Color.Black, 1000));
							charter_?.t2D中心基準描画(OpenTaiko.Skin.OnlineLounge_Context_Charter[0], OpenTaiko.Skin.OnlineLounge_Context_Charter[1]);
						}

						#endregion

						#region [Song Genre]

						if (song_.Genre != null && song_.Genre.genre != null && song_.Genre.genre != "") {
							var genre_ = TitleTextureKey.ResolveTitleTexture(
									new TitleTextureKey(song_.Genre.genre, this.pfOLFontLarge, Color.White, Color.Black, 1000));
							genre_?.t2D中心基準描画(OpenTaiko.Skin.OnlineLounge_Context_Genre[0], OpenTaiko.Skin.OnlineLounge_Context_Genre[1]);
						}

						#endregion

						#region [Difficulties]

						for (int k = 0; k < (int)Difficulty.Total; k++) {
							int diff = diffs[k];

							int column = (k >= 3) ? OpenTaiko.Skin.OnlineLounge_Context_Couse_Move[0] : 0;
							int row = OpenTaiko.Skin.OnlineLounge_Context_Couse_Move[1] * (k % 3);

							if (diff > 0) {
								OpenTaiko.Tx.Couse_Symbol[k]?.t2D中心基準描画(
									OpenTaiko.Skin.OnlineLounge_Context_Couse_Symbol[0] + column,
									OpenTaiko.Skin.OnlineLounge_Context_Couse_Symbol[1] + row);

								var difnb_ = TitleTextureKey.ResolveTitleTexture(
									new TitleTextureKey(diff.ToString(), this.pfOLFontLarge, (diff > 10) ? Color.Red : Color.White, Color.Black, 1000));
								difnb_?.t2D中心基準描画(OpenTaiko.Skin.OnlineLounge_Context_Level[0] + column, OpenTaiko.Skin.OnlineLounge_Context_Level[1] + row);
							}

						}

						#endregion


					}

				}
			}

			#endregion

			if (IsDownloading) {
				OpenTaiko.Tx.OnlineLounge_Box.t2D描画(0, 0);

				var text = TitleTextureKey.ResolveTitleTexture(
									new TitleTextureKey("Downloading...", this.pfOLFontLarge, Color.White, Color.Black, 1000));
				text.t2D中心基準描画(OpenTaiko.Skin.OnlineLounge_Downloading[0], OpenTaiko.Skin.OnlineLounge_Downloading[1]);
			}

			#endregion



			#region [Input]

			//if (!IsDownloading)
			{
				if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.RightArrow) ||
					OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RightChange)) {
					if (this.tMove(1)) {
						OpenTaiko.Skin.soundChangeSFX.tPlay();
					}
				} else if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.LeftArrow) ||
					  OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LeftChange)) {
					if (this.tMove(-1)) {
						OpenTaiko.Skin.soundChangeSFX.tPlay();
					}
				} else if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape) ||
				  OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.Cancel)) {

					#region [Fast return (Escape)]

					OpenTaiko.Skin.soundCancelSFX.tPlay();

					if (currentMenu == ECurrentMenu.MAIN) {
						// Return to title screen
						OpenTaiko.Skin.soundOnlineLoungeBGM?.tStop();
						this.eフェードアウト完了時の戻り値 = EReturnValue.ReturnToTitle;
						this.actFOtoTitle.tフェードアウト開始();
						base.ePhaseID = CStage.EPhase.Common_FADEOUT;
					} else if (currentMenu == ECurrentMenu.CDN_SELECT || currentMenu == ECurrentMenu.MULTI_SELECT) {
						// Return to base menu
						currentMenu = ECurrentMenu.MAIN;
					} else if (currentMenu == ECurrentMenu.CDN_OPTION) {
						// Return to CDN select menu
						currentMenu = ECurrentMenu.CDN_SELECT;
					} else if (currentMenu == ECurrentMenu.CDN_SONGS || currentMenu == ECurrentMenu.CDN_CHARACTERS || currentMenu == ECurrentMenu.CDN_PUCHICHARAS) {
						// Return to CDN select option
						currentMenu = ECurrentMenu.CDN_OPTION;
					}

					return 0;

					#endregion
				} else if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return) ||
					  OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.Decide)) {

					#region [Decide]

					if (currentMenu == ECurrentMenu.MAIN) {
						if (mainMenu[mainMenuIndex] == ECurrentMenu.CDN_SELECT || !IsDownloading) {
							// Base menu
							currentMenu = mainMenu[mainMenuIndex];
							if (currentMenu == ECurrentMenu.RETURN) {
								// Quit
								OpenTaiko.Skin.soundCancelSFX.tPlay();
								OpenTaiko.Skin.soundOnlineLoungeBGM?.tStop();
								this.eフェードアウト完了時の戻り値 = EReturnValue.ReturnToTitle;
								this.actFOtoTitle.tフェードアウト開始();
								base.ePhaseID = CStage.EPhase.Common_FADEOUT;
							} else {
								OpenTaiko.Skin.soundDecideSFX.tPlay();
							}
						} else {
							OpenTaiko.Skin.soundError.tPlay();
						}
					} else if (currentMenu == ECurrentMenu.CDN_SELECT) {
						// CDN Select Menu
						if (CDNSelectIndex > 0) {
							currentMenu = ECurrentMenu.CDN_OPTION;
							dbCDNData = dbCDN.data.ElementAt(CDNSelectIndex - 1).Value;
							OpenTaiko.Skin.soundDecideSFX.tPlay();
						} else {
							currentMenu = ECurrentMenu.MAIN;
							OpenTaiko.Skin.soundCancelSFX.tPlay();
						}
					} else if (currentMenu == ECurrentMenu.CDN_OPTION) {
						// CDN Option Menu
						currentMenu = cdnOptMenu[cdnOptMenuIndex];
						if (currentMenu == ECurrentMenu.CDN_SELECT)
							OpenTaiko.Skin.soundCancelSFX.tPlay();
						else {
							if (currentMenu == ECurrentMenu.CDN_SONGS) {
								apiMethods = new API(dbCDNData);
								apiMethods.tLoadSongsFromInternalCDN();

								#region [Generate song list values]

								int songCountPlusOne = apiMethods.FetchedSongsList.Length + 1;

								this.ttkCDNSongList = new TitleTextureKey[songCountPlusOne];
								this.ttkCDNSongSubtitles = new TitleTextureKey[songCountPlusOne];

								this.ttkCDNSongList[0] = new TitleTextureKey(GetMenuName((int)ECurrentMenu.MAIN), this.pfOLFont, Color.White, Color.DarkRed, 1000);
								this.ttkCDNSongSubtitles[0] = new TitleTextureKey("", this.pfOLFont, Color.White, Color.DarkRed, 1000);

								for (int i = 0; i < apiMethods.FetchedSongsList.Length; i++) {
									this.ttkCDNSongList[i + 1] = new TitleTextureKey(apiMethods.FetchedSongsList[i].SongTitle, this.pfOLFont, Color.White, Color.DarkRed, 1000);

									string subtitle_ = apiMethods.FetchedSongsList[i].SongSubtitle;
									if (subtitle_.Length >= 2)
										subtitle_ = subtitle_.Substring(2);
									this.ttkCDNSongSubtitles[i + 1] = new TitleTextureKey(subtitle_, this.pfOLFont, Color.White, Color.DarkRed, 1000);
								}

								this.cdnSongListIndex = 0;

								#endregion
							}
							OpenTaiko.Skin.soundDecideSFX.tPlay();
						}

					} else if (currentMenu == ECurrentMenu.CDN_SONGS) {
						if (this.cdnSongListIndex == 0) {
							OpenTaiko.Skin.soundCancelSFX.tPlay();
							currentMenu = ECurrentMenu.CDN_OPTION;
						} else {
							if (this.cdnSongListIndex < apiMethods.FetchedSongsList.Length) {
								var song = apiMethods.FetchedSongsList[this.cdnSongListIndex - 1];
								//var zipPath = $@"Cache{Path.DirectorySeparatorChar}{song.Md5}.zip";
								var downloadLink = GetDownloadLink(song);

								if (CSongDict.tContainsSongUrl(downloadLink) || song.DownloadNow) {
									OpenTaiko.Skin.soundError.tPlay();
								} else {
									OpenTaiko.Skin.soundDecideSFX.tPlay();
									System.Threading.Thread download =
										new System.Threading.Thread(new System.Threading.ThreadStart(DownloadSong));
									download.Start();
								}
							}
						}
					}


					#endregion
				}
			}
			#endregion

			// Menu exit fade out transition
			#region [FadeOut]

			switch (base.ePhaseID) {
				case CStage.EPhase.Common_FADEOUT:
					if (this.actFOtoTitle.Draw() == 0) {
						break;
					}
					return (int)this.eフェードアウト完了時の戻り値;

			}

			#endregion

			return 0;
		}

		public bool tMove(int val) {
			if (currentMenu == ECurrentMenu.MAIN) {
				if (mainMenuIndex + val < 0 || mainMenuIndex + val >= mainMenu.Length)
					return false;

				mainMenuIndex += val;
			} else if (currentMenu == ECurrentMenu.CDN_SELECT) {
				if (CDNSelectIndex + val < 0 || CDNSelectIndex + val >= ttkCDNSelectOpt.Length)
					return false;

				CDNSelectIndex += val;
			} else if (currentMenu == ECurrentMenu.CDN_OPTION) {
				if (cdnOptMenuIndex + val < 0 || cdnOptMenuIndex + val >= cdnOptMenu.Length)
					return false;

				cdnOptMenuIndex += val;
			} else if (currentMenu == ECurrentMenu.CDN_SONGS) {
				cdnSongListIndex = (ttkCDNSongList.Length + cdnSongListIndex + val) % ttkCDNSongList.Length;
			}

			return true;
		}

		#region [Song Downloading]

		public string ReplaceInvalidChars(string filename, string substitute = "_") {
			return string.Join(substitute, filename.Split(Path.GetInvalidFileNameChars()));
		}

		public string TruncateString(string s, int length) {
			return s.Substring(0, Math.Min(length, s.Length));
		}

		private string GetAssignedLanguageValue(Dictionary<string, string> ens) {
			if (ens.ContainsKey(OpenTaiko.ConfigIni.sLang))
				return ens[OpenTaiko.ConfigIni.sLang];
			return ens["default"];
		}

		private string GetDownloadLink(API.APISongData song) {
			return $"{dbCDNData.BaseUrl}{GetAssignedLanguageValue(dbCDNData.Download)}{song.Id}";
		}

		private void DownloadSong() {
			IsDownloading = true;

			// Create Cache folder if does not exist
			Directory.CreateDirectory($@"Cache{Path.DirectorySeparatorChar}");


			var song = apiMethods.FetchedSongsList[this.cdnSongListIndex - 1];
			song.DownloadNow = true;
			var zipName = ReplaceInvalidChars($@"{TruncateString(song.SongTitle, 16)}-{TruncateString(song.Md5, 10)}");
			var zipPath = $@"Cache{Path.DirectorySeparatorChar}{zipName}.zip";
			var downloadLink = GetDownloadLink(song);

			try {
				// Download zip from cdn

				if (!File.Exists(zipPath)) {
					System.Net.WebClient wc = new System.Net.WebClient();

					wc.DownloadFile(downloadLink, zipPath);
					wc.Dispose();
				}

				// Fetch closest Download folder node
				CSongListNode downloadBox = null;
				for (int i = 0; i < OpenTaiko.Songs管理.list曲ルート.Count; i++) {
					if (OpenTaiko.Songs管理.list曲ルート[i].strジャンル == "Download") {
						downloadBox = OpenTaiko.Songs管理.list曲ルート[i];
						if (downloadBox.rParentNode != null) downloadBox = downloadBox.rParentNode;
						break;
					}
				}

				// If there is at least one download folder, transfer the zip contents in it
				if (downloadBox != null) {
					var path = downloadBox.arスコア[0].ファイル情報.フォルダの絶対パス;
					var genredPath = $@"{path}{Path.DirectorySeparatorChar}{song.Genre.genre}{Path.DirectorySeparatorChar}";

					if (!Directory.Exists(genredPath)) {
						// Create Genre sub-folder if does not exist
						Directory.CreateDirectory(genredPath);

						// Search a corresponding box-def if exists
						CSongListNode correspondingBox = null;
						for (int i = 0; i < OpenTaiko.Songs管理.list曲ルート.Count; i++) {
							if (OpenTaiko.Songs管理.list曲ルート[i].strジャンル == song.Genre.genre
								&& OpenTaiko.Songs管理.list曲ルート[i].eノード種別 == CSongListNode.ENodeType.BOX)
								correspondingBox = OpenTaiko.Songs管理.list曲ルート[i];
						}

						var newBoxDef = $@"{genredPath}{Path.DirectorySeparatorChar}box.def";

						if (correspondingBox == null) {
							// Generate box.def if none available

							//File.Create(newBoxDef);

							StreamWriter sw = new StreamWriter(newBoxDef, false, Encoding.GetEncoding(OpenTaiko.sEncType));

							sw.WriteLine($@"#TITLE:{song.Genre.genre}");
							sw.WriteLine($@"#GENRE:{song.Genre.genre}");
							sw.WriteLine($@"#BOXEXPLANATION1:");
							sw.WriteLine($@"#BOXEXPLANATION2:");
							sw.WriteLine($@"#BOXEXPLANATION3:");
							sw.WriteLine($@"#BGCOLOR:#ff00a2");
							sw.WriteLine($@"#BOXCOLOR:#ff00a2");
							sw.WriteLine($@"#BOXTYPE:0");
							sw.WriteLine($@"#BGTYPE:1");
							sw.WriteLine($@"#BOXCHARA:0");
							sw.Close();
						} else {
							// Copy the existing box.def if available
							var corPath = correspondingBox.arスコア[0].ファイル情報.フォルダの絶対パス;

							File.Copy($@"{corPath}{Path.DirectorySeparatorChar}box.def", newBoxDef);
						}


					}


					var songPath = $@"{genredPath}{zipName}";

					System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, songPath);

					// Generate Unique ID with URL
					var idPath = songPath;
					while (1 == 1) {
						var directories = Directory.GetDirectories(idPath);
						if (directories.Length < 1)
							break;

						idPath = directories[0];
					}

					var uid = new CSongUniqueID(idPath + @$"{Path.DirectorySeparatorChar}uniqueID.json");
					uid.tAttachOnlineAddress(downloadLink);
					CSongDict.tAddSongUrl(uid);

				}

				//System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, $@"Songs{Path.DirectorySeparatorChar}S3 Download{Path.DirectorySeparatorChar}{song.Md5}");
			} catch (Exception e) {
				Trace.TraceInformation(e.ToString());
				OpenTaiko.Skin.soundError.tPlay();
			}


			song.DownloadNow = false;
			IsDownloading = false;
		}

		#endregion

		#region [Enums]

		public enum ECurrentMenu : int {
			RETURN,         // Return button
			MAIN,           // Choice between select CDN and Online multiplayer
			CDN_SELECT,     // Select a registered CDN
			CDN_OPTION,     // Select between Download songs, Download characters and Download puchicharas
			CDN_SONGS,      // List songs
			CDN_CHARACTERS, // List characters
			CDN_PUCHICHARAS,// List puchicharas
			MULTI_SELECT,   // Main online multiplayer menu
			TOTAL,          // Submenus count
		}

		#endregion

		#region [Private]

		private ScriptBG Background;

		private ECurrentMenu currentMenu;
		private ECurrentMenu menuPointer;
		private CMenuInfo[] menus;
		public EReturnValue eフェードアウト完了時の戻り値;
		public CActFIFOBlack actFOtoTitle;


		private CCachedFontRenderer pfOLFont;
		private CCachedFontRenderer pfOLFontLarge;

		private DBCDN dbCDN;
		private DBCDN.CDNData dbCDNData;
		private API apiMethods;

		// Main Menu
		private TitleTextureKey[] ttkMainMenuOpt;
		private ECurrentMenu[] mainMenu;
		private int mainMenuIndex;

		// CDN Select
		private TitleTextureKey[] ttkCDNSelectOpt;
		private int CDNSelectIndex;

		// CDN Option
		private TitleTextureKey[] ttkCDNOptionOpt;
		private ECurrentMenu[] cdnOptMenu;
		private int cdnOptMenuIndex;

		// CDN List songs option
		private TitleTextureKey[] ttkCDNSongList;
		private TitleTextureKey[] ttkCDNSongSubtitles;
		private int cdnSongListIndex;

		private bool IsDownloading;

		private class CMenuInfo {
			public CMenuInfo(string ttl) {
				title = ttl;
			}

			public string title;
		}

		#endregion

	}
}
