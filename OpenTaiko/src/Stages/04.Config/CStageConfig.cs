using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using FDK;
using SkiaSharp;

namespace OpenTaiko;

internal class CStageConfig : CStage {
	// Properties

	public CActDFPFont actFont { get; private set; }
	public CActCalibrationMode actCalibrationMode;


	// Constructor

	public CStageConfig() {
		CActDFPFont font;
		base.eStageID = CStage.EStage.Config;
		base.ePhaseID = CStage.EPhase.Common_NORMAL;
		this.actFont = font = new CActDFPFont();
		base.ChildActivities.Add(font);
		base.ChildActivities.Add(this.actFIFO = new CActFIFOWhite());
		// The legacy UI activities are kept as fields (other files reference them) but NOT auto-managed:
		// the config UI is now the Lua config_ui ROActivity. Not activating actList avoids it clobbering the
		// live config writes (its DeActivate would re-record stale item values).
		this.actList = new CActConfigList();
		this.actKeyAssign = new CActConfigKeyAssign();
		this.actOptionPanel = new CActOptionPanel();
		base.ChildActivities.Add(this.actCalibrationMode = new CActCalibrationMode());
		base.IsDeActivated = true;
	}


	// メソッド

	public void tAssignCompleteNotify()                                                         // CONFIGにのみ存在
	{                                                                                       //
		this.eItemPanelMode = EItemPanelMode.PadList;                               //
	}                                                                                       //
	public void tPadSelectedNotify(EKeyConfigPart part, EKeyConfigPad pad)                            //
	{                                                                                       //
		this.actKeyAssign.tStart(part, pad, this.actList.ibCurrentSelectedItem.strItemName);        //
		this.eItemPanelMode = EItemPanelMode.KeyCodeList;                         //
	}                                                                                       //
	public void tItemChangeNotify()                                                               // OPTIONと共通
	{                                                                                       //
		this.tDescriptionPanelCurrentSelectedItemDescriptionDraw();                     //
	}                                                                                       //


	// CStage 実装

	public override void Activate() {
		Trace.TraceInformation("コンフィグステージを活性化します。");
		Trace.Indent();
		try {
			OpenTaiko.Skin.bgmConfigScreen.tPlay();

			// snapshot the entry values whose heavy apply is deferred to exit (skin / render scale / sound device)
			_skinOrg = OpenTaiko.Skin.GetCurrentSkinSubfolderFullName(true);
			_renderOrg = OpenTaiko.ConfigIni.fRenderScale;
			_soundTypeOrg = OpenTaiko.ConfigIni.nSoundDeviceType;
			_bassBufOrg = OpenTaiko.ConfigIni.nBassBufferSizeMs;
			_wasapiBufOrg = OpenTaiko.ConfigIni.nWASAPIBufferSizeMs;
			_asioOrg = OpenTaiko.ConfigIni.nASIODevice;
			_osTimerOrg = OpenTaiko.ConfigIni.bUseOSTimer;

			RebuildModel();
			UI?.Activate(_model);   // hand the schema to the Lua config_ui ROActivity (it draws its own calm gradient bg)
		} finally {
			Trace.TraceInformation("コンフィグステージの活性化を完了しました。");
			Trace.Unindent();
		}
		base.Activate();        // 2011.3.14 yyagi: On活性化()をtryの中から外に移動
	}

	private static LuaROActivityWrapper? UI => LuaROActivityWrapper.GetROActivity("config_ui");
	private CLuaConfigModel _model;
	private CConfigOptionBuilder.Hooks _hooks;
	private string _skinOrg;
	private float _renderOrg;
	private int _soundTypeOrg, _bassBufOrg, _wasapiBufOrg, _asioOrg;
	private bool _osTimerOrg;

	private void RebuildModel() {
		if (_hooks == null) {
			_hooks = new CConfigOptionBuilder.Hooks {
				// language change → rebuild the localized model + push it to Lua
				Relocalize = () => { _model = CConfigOptionBuilder.Build(_hooks); UI?.Call("reload", _model); },
				Calibration = () => actCalibrationMode.Start(),
				ReloadSongs = () => tReloadSongs(false),
				HardReloadSongs = () => tReloadSongs(true),
				ImportScore = () => { try { new System.Threading.Thread(CScoreIni_Importer.ImportScoreInisToSavesDb3).Start(); } catch (Exception e) { Trace.TraceError(e.ToString()); } },
			};
		}
		_model = CConfigOptionBuilder.Build(_hooks);
	}

	private void tReloadSongs(bool hard) {
		if (OpenTaiko.EnumSongs.IsEnumerating) {
			OpenTaiko.EnumSongs.Abort();
			OpenTaiko.actEnumSongs.DeActivate();
		}
		OpenTaiko.EnumSongs.StartEnumFromDisk(hard);
		OpenTaiko.EnumSongs.ChangeEnumeratePriority(System.Threading.ThreadPriority.Normal);
		OpenTaiko.actEnumSongs.bCommandSongDataGet = true;
		OpenTaiko.actEnumSongs.Activate();
	}

	private void tApplySoundDeviceIfChanged() {
		var cfg = OpenTaiko.ConfigIni;
		if (OperatingSystem.IsWindows()) {
			if (_soundTypeOrg != cfg.nSoundDeviceType || _bassBufOrg != cfg.nBassBufferSizeMs ||
				_wasapiBufOrg != cfg.nWASAPIBufferSizeMs || _asioOrg != cfg.nASIODevice || _osTimerOrg != cfg.bUseOSTimer) {
				ESoundDeviceType t = cfg.nSoundDeviceType switch {
					0 => ESoundDeviceType.Bass, 1 => ESoundDeviceType.ASIO,
					2 => ESoundDeviceType.ExclusiveWASAPI, 3 => ESoundDeviceType.SharedWASAPI, _ => ESoundDeviceType.Unknown,
				};
				OpenTaiko.SoundManager.tInitialize(t, cfg.nBassBufferSizeMs, cfg.nWASAPIBufferSizeMs, 0, cfg.nASIODevice, cfg.bUseOSTimer);
				OpenTaiko.app.ShowWindowTitle();
				OpenTaiko.Skin.ReloadSystemSounds();
				OpenTaiko.Skin.PreloadSystemSounds();
			}
		} else if (_bassBufOrg != cfg.nBassBufferSizeMs || _osTimerOrg != cfg.bUseOSTimer) {
			OpenTaiko.SoundManager.tInitialize(ESoundDeviceType.Bass, cfg.nBassBufferSizeMs, 0, 0, 0, cfg.bUseOSTimer);
		}
	}
	public override void DeActivate() {
		Trace.TraceInformation("コンフィグステージを非活性化します。");
		Trace.Indent();
		try {
			OpenTaiko.Skin.bgmConfigScreen.tStop();

			UI?.Deactivate();

			OpenTaiko.ConfigIni.tExport(OpenTaiko.strEXEFolder + "Config.ini");    // save on exit

			// deferred heavy apply (the live writes already landed in ConfigIni; here we reload what's expensive)
			if (OpenTaiko.Skin.GetCurrentSkinSubfolderFullName(true) != _skinOrg
				|| OpenTaiko.ConfigIni.fRenderScale != _renderOrg) {
				OpenTaiko.app.EnterRefreshSkinStage(isSavedBeforeUpdate: true);   // resolution change reloads the skin too
			}
			tApplySoundDeviceIfChanged();
			FDK.SoundManager.bIsTimeStretch = OpenTaiko.ConfigIni.bTimeStretch;

			for (int i = 0; i < OpenTaiko.MAX_PLAYERS; i++) {
				int id = OpenTaiko.SaveFileInstances[i].data.TitleId;
				if (id > 0) {
					var title = OpenTaiko.Databases.DBNameplateUnlockables.data[id];
					OpenTaiko.SaveFileInstances[i].data.Title = title.nameplateInfo.cld.GetString("");
				}
				OpenTaiko.NamePlate.tNamePlateRefreshTitles(i);
			}

			base.DeActivate();
		} catch (UnauthorizedAccessException e) {
			Trace.TraceError(e.ToString());
			Trace.TraceError("ファイルが読み取り専用になっていないか、管理者権限がないと書き込めなくなっていないか等を確認して下さい");
			Trace.TraceError("例外が発生しましたが処理を継続します。 (7a61f01b-1703-4aad-8d7d-08bd88ae8760)");
		} catch (Exception e) {
			Trace.TraceError(e.ToString());
			Trace.TraceError("例外が発生しましたが処理を継続します。 (83f0d93c-bb04-4a19-a596-bc32de39f496)");
		} finally {
			Trace.TraceInformation("コンフィグステージの非活性化を完了しました。");
			Trace.Unindent();
		}
	}

	public void ReloadMenus() {
		string[] strMenuItem = {
			CLangManager.LangInstance.GetString("SETTINGS_SYSTEM"),
			CLangManager.LangInstance.GetString("SETTINGS_GAME"),
			CLangManager.LangInstance.GetString("SETTINGS_THEME"),
			CLangManager.LangInstance.GetString("SETTINGS_EXIT")
		};

		txMenuItemLeft = new CTexture[strMenuItem.Length, 2];

		using (var prvFont = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.Config_Font_Scale)) {
			for (int i = 0; i < strMenuItem.Length; i++) {
				using (var bmpStr = prvFont.DrawText(strMenuItem[i], Color.White, Color.Black, null, 30)) {
					txMenuItemLeft[i, 0]?.Dispose();
					txMenuItemLeft[i, 0] = OpenTaiko.tTextureCreate(bmpStr, false);
				}
				using (var bmpStr = prvFont.DrawText(strMenuItem[i],
						   Color.White,
						   Color.Black,
						   null,
						   OpenTaiko.Skin.Config_Selected_Menu_Text_Grad_Color_1,
						   OpenTaiko.Skin.Config_Selected_Menu_Text_Grad_Color_2,
						   30)) {
					txMenuItemLeft[i, 1]?.Dispose();
					txMenuItemLeft[i, 1] = OpenTaiko.tTextureCreate(bmpStr, false);
				}
			}
		}
	}

	public override void CreateManagedResource()                                            // OPTIONと画像以外共通
	{
		//if (HPrivateFastFont.FontExists(TJAPlayer3.Skin.FontName))
		//{
		//    this.ftフォント = new CCachedFontRenderer(TJAPlayer3.Skin.FontName, (int)TJAPlayer3.Skin.Config_Font_Scale_Description, CFontRenderer.FontStyle.Bold);
		//}
		//else
		//{
		//    this.ftフォント = new CCachedFontRenderer(CFontRenderer.DefaultFontName, (int)TJAPlayer3.Skin.Config_Font_Scale_Description, CFontRenderer.FontStyle.Bold);
		//}
		this.ftFont = HPrivateFastFont.tInstantiateMainFont((int)OpenTaiko.Skin.Config_Font_Scale_Description, CFontRenderer.FontStyle.Bold);


		OpenTaiko.Tx.Config_Cursor = OpenTaiko.tTextureCreate(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.CONFIG}Cursor.png"));

		//ctBackgroundAnime = new CCounter(0, TJAPlayer3.Tx.Config_Background.szテクスチャサイズ.Width, 20, TJAPlayer3.Timer);

		/*
		string[] strMenuItem = {
			CLangManager.LangInstance.GetString(10085),
			CLangManager.LangInstance.GetString(10086),
			CLangManager.LangInstance.GetString(10087)
		};

		txMenuItemLeft = new CTexture[strMenuItem.Length, 2];

		using (var prvFont = new CPrivateFastFont(new FontFamily(string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName) ? "MS UI Gothic" :  TJAPlayer3.ConfigIni.FontName), 20))
		{
			for (int i = 0; i < strMenuItem.Length; i++)
			{
				using (var bmpStr = prvFont.DrawPrivateFont(strMenuItem[i], Color.White, Color.Black))
				{
					txMenuItemLeft[i, 0] = TJAPlayer3.tテクスチャの生成(bmpStr, false);
				}
				using (var bmpStr = prvFont.DrawPrivateFont(strMenuItem[i], Color.White, Color.Black, Color.Yellow, Color.OrangeRed))
				{
					txMenuItemLeft[i, 1] = TJAPlayer3.tテクスチャの生成(bmpStr, false);
				}
			}
		}
		*/
		base.CreateManagedResource();
	}
	public override void ReleaseManagedResource()                                           // OPTIONと同じ(COnfig.iniの書き出しタイミングのみ異なるが、無視して良い)
	{
		if (this.ftFont != null) {
			this.ftFont.Dispose();
			this.ftFont = null;
		}
		//CDTXMania.tテクスチャの解放( ref this.tx背景 );
		//CDTXMania.tテクスチャの解放( ref this.tx上部パネル );
		//CDTXMania.tテクスチャの解放( ref this.tx下部パネル );
		//CDTXMania.tテクスチャの解放( ref this.txMenuカーソル );

		OpenTaiko.tTextureRelease(ref this.txDescriptionPanel);
		base.ReleaseManagedResource();
	}
	public override int Draw() {
		if (base.IsDeActivated)
			return 0;

		if (base.IsFirstDraw) {
			base.ePhaseID = CStage.EPhase.Common_FADEIN;
			this.actFIFO.tFadeInStart();
			base.IsFirstDraw = false;
		}

		if (actCalibrationMode.IsStarted) {
			// the calibration tap-test owns the screen + input until it finishes
			if (OpenTaiko.Skin.bgmConfigScreen.bIsPlaying)
				OpenTaiko.Skin.bgmConfigScreen.tStop();
			actCalibrationMode.Update();
			actCalibrationMode.Draw();
		} else {
			if (!OpenTaiko.Skin.bgmConfigScreen.bIsPlaying)
				OpenTaiko.Skin.bgmConfigScreen.tPlay();

			if (base.ePhaseID == CStage.EPhase.Common_NORMAL) {
				if (_model != null && _model.Keys.IsCapturing) {
					// C# owns input this frame: poll the device sweep for the key being bound
					_model.Keys.PollCaptureFrame();
				} else {
					var r = UI?.Update();   // Lua handles nav/edit/cancel; returns "exit" at the top level
					if (r != null && r.Length > 0 && (r[0] as string) == "exit") {
						OpenTaiko.Skin.soundDecideSFX.tPlay();
						this.actFIFO.tFadeOutStart();
						base.ePhaseID = CStage.EPhase.Common_FADEOUT;
					}
				}
			}
			UI?.Draw();
		}

		switch (base.ePhaseID) {
			case CStage.EPhase.Common_FADEIN:
				if (this.actFIFO.Draw() != 0)
					base.ePhaseID = CStage.EPhase.Common_NORMAL;
				break;

			case CStage.EPhase.Common_FADEOUT:
				if (this.actFIFO.Draw() == 0)
					break;
				return 1;
		}
		return 0;
	}


	// その他

	#region [ private ]
	//-----------------
	private enum EItemPanelMode {
		PadList,
		KeyCodeList
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct STKeyRepeatCounter {
		public CCounter Up;
		public CCounter Down;
		public CCounter R;
		public CCounter B;
		public CCounter this[int index] {
			get {
				switch (index) {
					case 0:
						return this.Up;

					case 1:
						return this.Down;

					case 2:
						return this.R;

					case 3:
						return this.B;
				}
				throw new IndexOutOfRangeException();
			}
			set {
				switch (index) {
					case 0:
						this.Up = value;
						return;

					case 1:
						this.Down = value;
						return;

					case 2:
						this.R = value;
						return;

					case 3:
						this.B = value;
						return;
				}
				throw new IndexOutOfRangeException();
			}
		}
	}

	//private CCounter ctBackgroundAnime;
	private CActFIFOWhite actFIFO;
	private CActConfigKeyAssign actKeyAssign;
	public CActConfigList actList;
	private CActOptionPanel actOptionPanel;
	private bool bMenuFocus;
	private STKeyRepeatCounter ctKeyRepeat;
	private const int DESC_H = 0x80;
	private const int DESC_W = 220;
	private EItemPanelMode eItemPanelMode;
	internal CCachedFontRenderer ftFont;
	private int nCurrentMenuNumber;
	//private CTexture txMenuカーソル;
	//private CTexture tx下部パネル;
	//private CTexture tx上部パネル;
	private CTexture txDescriptionPanel;
	//private CTexture tx背景;
	private CTexture[,] txMenuItemLeft;

	private void tCursorBottomMove() {
		if (!this.bMenuFocus) {
			switch (this.eItemPanelMode) {
				case EItemPanelMode.PadList:
					this.actList.tNextMove();
					return;

				case EItemPanelMode.KeyCodeList:
					this.actKeyAssign.tNextMove();
					return;
			}
		} else {
			OpenTaiko.Skin.soundCursorMoveSound.tPlay();
			this.nCurrentMenuNumber = (this.nCurrentMenuNumber + 1) % 4;
			switch (this.nCurrentMenuNumber) {
				case 0:
					this.actList.tItemListSettings_System();
					break;

				case 1:
					this.actList.tItemListSettings_Drums();
					break;

				case 2:
					this.actList.tItemListSettings_Theme();
					break;

				case 3:
					this.actList.tItemListSettings_Exit();
					break;
			}
			this.tDescriptionPanelCurrentSelectedMenuDescriptionDraw();
		}
	}
	private void tCursorTopMove() {
		if (!this.bMenuFocus) {
			switch (this.eItemPanelMode) {
				case EItemPanelMode.PadList:
					this.actList.tPrevMove();
					return;

				case EItemPanelMode.KeyCodeList:
					this.actKeyAssign.tPrevMove();
					return;
			}
		} else {
			OpenTaiko.Skin.soundCursorMoveSound.tPlay();
			this.nCurrentMenuNumber = ((this.nCurrentMenuNumber - 1) + 4) % 4;
			switch (this.nCurrentMenuNumber) {
				case 0:
					this.actList.tItemListSettings_System();
					break;

				case 1:
					this.actList.tItemListSettings_Drums();
					break;

				case 2:
					this.actList.tItemListSettings_Theme();
					break;

				case 3:
					this.actList.tItemListSettings_Exit();
					break;
			}
			this.tDescriptionPanelCurrentSelectedMenuDescriptionDraw();
		}
	}
	private void tDescriptionPanelCurrentSelectedMenuDescriptionDraw() {
		try {
			string text = "";
			switch (this.nCurrentMenuNumber) {
				case 0:
					text = CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_DESC");
					break;
				case 1:
					text = CLangManager.LangInstance.GetString("SETTINGS_GAME_DESC");
					break;
				case 2:
					text = CLangManager.LangInstance.GetString("SETTINGS_THEME_DESC");
					break;
				case 3:
					text = CLangManager.LangInstance.GetString("SETTINGS_EXIT_DESC");
					break;
			}
			SKBitmap image = ftFont.DrawText(text, Color.White, Color.Black, null, 30);
			if (this.txDescriptionPanel != null) {
				this.txDescriptionPanel.Dispose();
			}
			this.txDescriptionPanel = new CTexture(image);
			image.Dispose();
		} catch (CTextureCreateFailedException e) {
			Trace.TraceError(e.ToString());
			Trace.TraceError("説明文テクスチャの作成に失敗しました。");
			this.txDescriptionPanel = null;
		}
	}
	private void tDescriptionPanelCurrentSelectedItemDescriptionDraw() {
		try {
			var image = new SKBitmap(440, 288);     // 説明文領域サイズの縦横 2 倍。（描画時に 0.5 倍で表示する___のは中止。処理速度向上のため。）

			CItemBase item = this.actList.ibCurrentSelectedItem;
			if ((item.strDescription != null) && (item.strDescription.Length > 0)) {
				image.Dispose();
				image = ftFont.DrawText(item.strDescription, Color.White, Color.Black, null, 30);
			}
			if (this.txDescriptionPanel != null) {
				this.txDescriptionPanel.Dispose();
			}
			this.txDescriptionPanel = new CTexture(image);
			image.Dispose();
		} catch (CTextureCreateFailedException e) {
			Trace.TraceError(e.ToString());
			Trace.TraceError("説明文パネルテクスチャの作成に失敗しました。");
			this.txDescriptionPanel = null;
		}
	}
	//-----------------
	#endregion
}
