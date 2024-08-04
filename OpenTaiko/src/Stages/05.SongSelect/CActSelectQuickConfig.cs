using FDK;

namespace OpenTaiko {
	internal class CActSelectQuickConfig : CActSelectPopupMenu {
		// コンストラクタ

		public CActSelectQuickConfig() {
			CActSelectQuickConfigMain();
		}

		private void CActSelectQuickConfigMain() {
			/*
			•Target: Drums/Guitar/Bass 
			•Auto Mode: All ON/All OFF/CUSTOM 
			•Auto Lane: 
			•Scroll Speed: 
			•Play Speed: 
			•Risky: 
			•Hidden/Sudden: None/Hidden/Sudden/Both 
			•Conf SET: SET-1/SET-2/SET-3 
			•More... 
			•EXIT 
			*/
			lci = new List<List<List<CItemBase>>>();                                    // この画面に来る度に、メニューを作り直す。
			for (int nConfSet = 0; nConfSet < 3; nConfSet++) {
				lci.Add(new List<List<CItemBase>>());                                   // ConfSet用の3つ分の枠。
				for (int nInst = 0; nInst < 3; nInst++) {
					lci[nConfSet].Add(null);                                        // Drum/Guitar/Bassで3つ分、枠を作っておく
					lci[nConfSet][nInst] = MakeListCItemBase(nConfSet, nInst);
				}
			}
			base.Initialize(lci[nCurrentConfigSet][0], true, CLangManager.LangInstance.GetString("SONGSELECT_QUICKCONFIG"), 0); // ConfSet=0, nInst=Drums
		}

		private List<CItemBase> MakeListCItemBase(int nConfigSet, int nInst) {
			List<CItemBase> l = new List<CItemBase>();

			#region [ 共通 Target/AutoMode/AutoLane ]
			#endregion
			#region [ 個別 ScrollSpeed ]
			l.Add(new CItemInteger(CLangManager.LangInstance.GetString("MOD_SPEED"), 0, 1999, OpenTaiko.ConfigIni.nScrollSpeed[OpenTaiko.SaveFile],
				""));
			#endregion
			#region [ 共通 Dark/Risky/PlaySpeed ]
			l.Add(new CItemInteger(CLangManager.LangInstance.GetString("MOD_SONGSPEED"), 5, 400, OpenTaiko.ConfigIni.nSongSpeed,
				""));
			#endregion
			#region [ 個別 Sud/Hid ]
			l.Add(new CItemList(CLangManager.LangInstance.GetString("MOD_RANDOM"), CItemBase.EPanelType.Normal, (int)OpenTaiko.ConfigIni.eRandom[OpenTaiko.SaveFile],
				"",
				new string[] { CLangManager.LangInstance.GetString("MOD_SWITCH_OFF"), CLangManager.LangInstance.GetString("MOD_RANDOM"), CLangManager.LangInstance.GetString("MOD_FLIP"), "SUPER", "HYPER" }));
			l.Add(new CItemList(CLangManager.LangInstance.GetString("MOD_HIDE"), CItemBase.EPanelType.Normal, (int)OpenTaiko.ConfigIni.eSTEALTH[OpenTaiko.SaveFile],
				"",
				new string[] { CLangManager.LangInstance.GetString("MOD_SWITCH_OFF"), CLangManager.LangInstance.GetString("MOD_HIDE"), CLangManager.LangInstance.GetString("MOD_STEALTH") }));
			l.Add(new CItemList(CLangManager.LangInstance.GetString("SETTINGS_GAME_SURVIVAL"), CItemBase.EPanelType.Normal, (int)OpenTaiko.ConfigIni.eGameMode,
				"",
				new string[] { CLangManager.LangInstance.GetString("MOD_SWITCH_OFF"), "TYPE-A", "TYPE-B" }));

			l.Add(new CItemList(CLangManager.LangInstance.GetString("SETTINGS_GAME_SHINUCHI"), CItemBase.EPanelType.Normal, OpenTaiko.ConfigIni.ShinuchiMode ? 1 : 0, "", "", new string[] { CLangManager.LangInstance.GetString("MOD_SWITCH_OFF"), CLangManager.LangInstance.GetString("MOD_SWITCH_ON") }));

			#endregion
			#region [ 共通 SET切り替え/More/Return ]
			l.Add(new CItemInteger(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_PLAYERCOUNT"), 1, 5, OpenTaiko.ConfigIni.nPlayerCount, ""));
			l.Add(new CSwitchItemList(CLangManager.LangInstance.GetString("SONGSELECT_QUICKCONFIG_MORE"), CItemBase.EPanelType.Normal, 0, "", "", new string[] { "" }));
			l.Add(new CSwitchItemList(CLangManager.LangInstance.GetString("MENU_RETURN"), CItemBase.EPanelType.Normal, 0, "", "", new string[] { "", "" }));
			#endregion

			return l;
		}

		// メソッド
		public override void tActivatePopupMenu(EInstrumentPad einst) {
			this.CActSelectQuickConfigMain();
			base.tActivatePopupMenu(einst);
		}
		//public void tDeativatePopupMenu()
		//{
		//	base.tDeativatePopupMenu();
		//}

		public override void t進行描画sub() {

		}

		public override void tEnter押下Main(int nSortOrder) {
			switch (n現在の選択行) {
				case (int)EOrder.ScrollSpeed:
					OpenTaiko.ConfigIni.nScrollSpeed[OpenTaiko.SaveFile] = (int)GetObj現在値((int)EOrder.ScrollSpeed);
					break;

				case (int)EOrder.PlaySpeed:
					OpenTaiko.ConfigIni.nSongSpeed = (int)GetObj現在値((int)EOrder.PlaySpeed);
					break;
				case (int)EOrder.Random:
					OpenTaiko.ConfigIni.eRandom[OpenTaiko.SaveFile] = (ERandomMode)GetIndex((int)EOrder.Random);
					break;
				case (int)EOrder.Stealth:
					OpenTaiko.ConfigIni.eSTEALTH[OpenTaiko.SaveFile] = (EStealthMode)GetIndex((int)EOrder.Stealth);
					break;
				case (int)EOrder.GameMode:
					EGame game = EGame.OFF;
					switch ((int)GetIndex((int)EOrder.GameMode)) {
						case 0: game = EGame.OFF; break;
						case 1: game = EGame.完走叩ききりまショー; break;
						case 2: game = EGame.完走叩ききりまショー激辛; break;
					}
					OpenTaiko.ConfigIni.eGameMode = game;
					break;
				case (int)EOrder.ShinuchiMode:
					OpenTaiko.ConfigIni.ShinuchiMode = !OpenTaiko.ConfigIni.ShinuchiMode;
					break;
				case (int)EOrder.PlayerCount:
					OpenTaiko.ConfigIni.nPlayerCount = (int)GetObj現在値((int)EOrder.PlayerCount);
					break;
				case (int)EOrder.More:
					SetAutoParameters();            // 簡易CONFIGメニュー脱出に伴い、簡易CONFIG内のAUTOの設定をConfigIniクラスに反映する
					this.bGotoDetailConfig = true;
					this.tDeativatePopupMenu();
					break;

				case (int)EOrder.Return:
					SetAutoParameters();            // 簡易CONFIGメニュー脱出に伴い、簡易CONFIG内のAUTOの設定をConfigIniクラスに反映する
					this.tDeativatePopupMenu();
					break;
				default:
					break;
			}
		}

		public override void tCancel() {
			SetAutoParameters();
			// Autoの設定値保持のロジックを書くこと！
			// (Autoのパラメータ切り替え時は実際に値設定していないため、キャンセルまたはRetern, More...時に値設定する必要有り)
		}


		/// <summary>
		/// ConfigIni.bAutoPlayに簡易CONFIGの状態を反映する
		/// </summary>
		private void SetAutoParameters() {

		}

		// CActivity 実装

		public override void Activate() {
			base.Activate();
			this.bGotoDetailConfig = false;
		}
		public override void DeActivate() {
			base.DeActivate();
		}
		public override void CreateManagedResource() {
			this.ft表示用フォント = new CCachedFontRenderer("Arial", 26, CFontRenderer.FontStyle.Bold);
			//string pathパネル本体 = CSkin.Path( @"Graphics\ScreenSelect popup auto settings.png" );
			//if ( File.Exists( pathパネル本体 ) )
			//{
			//	this.txパネル本体 = CDTXMania.tテクスチャの生成( pathパネル本体, true );
			//}

			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource() {
			if (this.ft表示用フォント != null) {
				this.ft表示用フォント.Dispose();
				this.ft表示用フォント = null;
			}
			//CDTXMania.tテクスチャの解放( ref this.txパネル本体 );
			OpenTaiko.tテクスチャの解放(ref this.tx文字列パネル);
			base.ReleaseManagedResource();
		}

		#region [ private ]
		//-----------------
		private int nCurrentTarget = 0;
		private int nCurrentConfigSet = 0;
		private List<List<List<CItemBase>>> lci;        // DrGtBs, ConfSet, 選択肢一覧。都合、3次のListとなる。
		private enum EOrder : int {
			ScrollSpeed = 0,
			PlaySpeed,
			Random,
			Stealth,
			GameMode,
			ShinuchiMode,
			PlayerCount,
			More,
			Return,
			END,
			Default = 99
		};

		private CCachedFontRenderer ft表示用フォント;
		//private CTexture txパネル本体;
		private CTexture tx文字列パネル;
		private CTexture tx説明文1;
		//-----------------
		#endregion
	}


}
