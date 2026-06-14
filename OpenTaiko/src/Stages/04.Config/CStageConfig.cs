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
		base.ChildActivities.Add(this.actList = new CActConfigList());
		base.ChildActivities.Add(this.actKeyAssign = new CActConfigKeyAssign());
		base.ChildActivities.Add(this.actOptionPanel = new CActOptionPanel());
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

			this.nCurrentMenuNumber = 0;                                                    //
			for (int i = 0; i < 4; i++)                                                 //
			{                                                                               //
				this.ctKeyRepeat[i] = new CCounter(0, 0, 0, OpenTaiko.Timer);          //
			}                                                                               //
			this.bMenuFocus = true;                                           // ここまでOPTIONと共通
			this.eItemPanelMode = EItemPanelMode.PadList;

			ReloadMenus();

			Background = new ScriptBG(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.CONFIG}Script.lua"));
			Background.Init();


			if (this.bMenuFocus) {
				this.tDescriptionPanelCurrentSelectedMenuDescriptionDraw();
			} else {
				this.tDescriptionPanelCurrentSelectedItemDescriptionDraw();
			}
		} finally {
			Trace.TraceInformation("コンフィグステージの活性化を完了しました。");
			Trace.Unindent();
		}
		base.Activate();        // 2011.3.14 yyagi: On活性化()をtryの中から外に移動
	}
	public override void DeActivate() {
		Trace.TraceInformation("コンフィグステージを非活性化します。");
		Trace.Indent();
		try {
			OpenTaiko.Skin.bgmConfigScreen.tStop();

			OpenTaiko.ConfigIni.tExport(OpenTaiko.strEXEFolder + "Config.ini");    // CONFIGだけ
			for (int i = 0; i < 4; i++) {
				this.ctKeyRepeat[i] = null;
			}

			for (int i = 0; i < txMenuItemLeft.GetLength(0); i++) {
				txMenuItemLeft[i, 0].Dispose();
				txMenuItemLeft[i, 0] = null;
				txMenuItemLeft[i, 1].Dispose();
				txMenuItemLeft[i, 1] = null;
			}
			txMenuItemLeft = null;

			OpenTaiko.tDisposeSafely(ref Background);

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

		//ctBackgroundAnime.t進行Loop();

		// 描画

		#region [ Background ]

		//---------------------
		/*
		for(int i = 0; i < 2; i++)
			if (TJAPlayer3.Tx.Config_Background != null )
				TJAPlayer3.Tx.Config_Background.t2D描画( 0 + -(TJAPlayer3.Tx.Config_Background.szテクスチャサイズ.Width * i) + ctBackgroundAnime.n現在の値, 0 );
		if(TJAPlayer3.Tx.Config_Header != null )
            TJAPlayer3.Tx.Config_Header.t2D描画( 0, 0 );
		*/
		Background.Update();
		Background.Draw();
		//---------------------

		#endregion

		#region [ Menu Cursor ]
		//---------------------
		if (OpenTaiko.Tx.Config_Cursor != null) {
			#region Old
			/*
			Rectangle rectangle;
            TJAPlayer3.Tx.TJAPlayer3.Tx.Config_Cursor.Opacity = this.bメニューにフォーカス中 ? 255 : 128;
			int x = 110;
			int y = (int)( 145.5 + ( this.n現在のメニュー番号 * 37.5 ) );
			int num3 = 340;
            TJAPlayer3.Tx.TJAPlayer3.Tx.Config_Cursor.t2D描画( x, y, new Rectangle( 0, 0, 32, 48 ) );
            TJAPlayer3.Tx.TJAPlayer3.Tx.Config_Cursor.t2D描画( ( x + num3 ) - 32, y, new Rectangle( 20, 0, 32, 48 ) );
			x += 32;
			for( num3 -= 64; num3 > 0; num3 -= rectangle.Width )
			{
				rectangle = new Rectangle( 16, 0, 32, 48 );
				if( num3 < 32 )
				{
					rectangle.Width -= 32 - num3;
				}
                TJAPlayer3.Tx.TJAPlayer3.Tx.Config_Cursor.t2D描画( x, y, rectangle );
				x += rectangle.Width;
			}
			*/
			#endregion


			int x = OpenTaiko.Skin.Config_Item_X[this.nCurrentMenuNumber];
			int y = OpenTaiko.Skin.Config_Item_Y[this.nCurrentMenuNumber];

			int width = OpenTaiko.Tx.Config_Cursor.szImageSize.Width / 3;
			int height = OpenTaiko.Tx.Config_Cursor.szImageSize.Height;

			int move = OpenTaiko.Skin.Config_Item_Width;

			//Left
			OpenTaiko.Tx.Config_Cursor.t2DCenterBasedDraw(x - (width / 2) - move, y,
				new Rectangle(0, 0, width, height));

			//Right
			OpenTaiko.Tx.Config_Cursor.t2DCenterBasedDraw(x + (width / 2) + move, y,
				new Rectangle(width * 2, 0, width, height));

			//Center
			OpenTaiko.Tx.Config_Cursor.vcScaleRatio.X = (move / (float)width) * 2.0f;
			OpenTaiko.Tx.Config_Cursor.t2DScaledCenterBasedDraw(x, y,
				new Rectangle(width, 0, width, height));

			OpenTaiko.Tx.Config_Cursor.vcScaleRatio.X = 1.0f;
		}
		//---------------------
		#endregion

		#region [ Menu ]
		//---------------------
		//int menuY = 162 - 22 + 13;
		//int stepY = 39;
		for (int i = 0; i < txMenuItemLeft.GetLength(0); i++) {
			//Bitmap bmpStr = (this.n現在のメニュー番号 == i) ?
			//      prvFont.DrawPrivateFont( strMenuItem[ i ], Color.White, Color.Black, Color.Yellow, Color.OrangeRed ) :
			//      prvFont.DrawPrivateFont( strMenuItem[ i ], Color.White, Color.Black );
			//txMenuItemLeft = CDTXMania.tテクスチャの生成( bmpStr, false );

			int flag = (this.nCurrentMenuNumber == i) ? 1 : 0;
			txMenuItemLeft[i, flag].t2DCenterBasedDraw(OpenTaiko.Skin.Config_Item_X[i] + OpenTaiko.Skin.Config_Item_Font_Offset[0], OpenTaiko.Skin.Config_Item_Y[i] + OpenTaiko.Skin.Config_Item_Font_Offset[1]); //55
																																																		 //txMenuItem.Dispose();
																																																		 //menuY += stepY;
		}
		//---------------------
		#endregion

		#region [ Explanation Panel ]
		//---------------------
		if (this.txDescriptionPanel != null)
			this.txDescriptionPanel.t2DDraw(OpenTaiko.Skin.Config_ExplanationPanel[0], OpenTaiko.Skin.Config_ExplanationPanel[1]);
		//---------------------
		#endregion

		#region [ Item ]
		//---------------------
		switch (this.eItemPanelMode) {
			case EItemPanelMode.PadList:
				this.actList.Draw(!this.bMenuFocus);
				break;

			case EItemPanelMode.KeyCodeList:
				this.actKeyAssign.Draw();
				break;
		}
		//---------------------
		#endregion

		//#region [ 上部パネル ]
		////---------------------
		//if( this.tx上部パネル != null )
		//	this.tx上部パネル.t2D描画( CDTXMania.app.Device, 0, 0 );
		////---------------------
		//#endregion
		//#region [ 下部パネル ]
		////---------------------
		//if( this.tx下部パネル != null )
		//	this.tx下部パネル.t2D描画( CDTXMania.app.Device, 0, 720 - this.tx下部パネル.szテクスチャサイズ.Height );
		////---------------------
		//#endregion

		#region [ Option Panel ]
		//---------------------
		//this.actオプションパネル.On進行描画();
		//---------------------
		#endregion

		#region [ FadeOut ]
		//---------------------
		switch (base.ePhaseID) {
			case CStage.EPhase.Common_FADEIN:
				if (this.actFIFO.Draw() != 0) {
					base.ePhaseID = CStage.EPhase.Common_NORMAL;
				}
				break;

			case CStage.EPhase.Common_FADEOUT:
				if (this.actFIFO.Draw() == 0) {
					break;
				}
				return 1;
		}
		//---------------------
		#endregion

		#region [ Enumerating Songs ]
		// CActEnumSongs側で表示する
		#endregion

		// キー入力

		if ((base.ePhaseID != CStage.EPhase.Common_NORMAL)
			|| this.actKeyAssign.bKeyInputWaitMiddle)
			return 0;

		if (actCalibrationMode.IsStarted) {
			if (OpenTaiko.Skin.bgmConfigScreen.bIsPlaying)
				OpenTaiko.Skin.bgmConfigScreen.tStop();

			actCalibrationMode.Update();
			actCalibrationMode.Draw();
		} else if (actList.ScoreIniImportThreadIsActive) {
			HBlackBackdrop.Draw(191);

			using (var prvFont = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.Config_Font_Scale)) {
				using (var status_text = new CTexture(prvFont.DrawText(
						   CScoreIni_Importer.Status,
						   Color.White,
						   Color.Black,
						   null,
						   30,
						   true))) {
					status_text.t2D_DisplayImage_AnchorCenter(GameWindowSize.Width / 2, GameWindowSize.Height / 2);
				}
			}
		}
		// 曲データの一覧取得中は、キー入力を無効化する
		else if (!OpenTaiko.EnumSongs.IsEnumerating || OpenTaiko.actEnumSongs.bCommandSongDataGet != true) {
			if (!OpenTaiko.Skin.bgmConfigScreen.bIsPlaying)
				OpenTaiko.Skin.bgmConfigScreen.tPlay();

			if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape) || OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.FT)) {
				OpenTaiko.Skin.soundCancelSFX.tPlay();
				if (!this.bMenuFocus) {
					if (this.eItemPanelMode == EItemPanelMode.KeyCodeList) {
						OpenTaiko.stageConfig.tAssignCompleteNotify();
						return 0;
					}
					bool wasEditingStringInput = this.actList.bIsFocusingParameter;
					if (!this.actList.bIsKeyAssignSelected && !wasEditingStringInput)   // #24525 2011.3.15 yyagi, #32059 2013.9.17 yyagi
					{
						this.bMenuFocus = true;
					}
					// Only reset the description panel when not simply cancelling a string text input.
					if (!wasEditingStringInput)
						this.tDescriptionPanelCurrentSelectedMenuDescriptionDraw();
					this.actList.tEscPressed();                              // #24525 2011.3.15 yyagi ESC押下時の右メニュー描画用
				} else {
					this.actFIFO.tFadeOutStart();
					base.ePhaseID = CStage.EPhase.Common_FADEOUT;
				}
			} else if ((OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.CY) || OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.RD)) || (OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.LC) || (OpenTaiko.ConfigIni.bEnterIsNotUsedInKeyAssignments && OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return)))) {
				if (this.nCurrentMenuNumber == 3) {
					// Exit
					OpenTaiko.Skin.soundDecideSFX.tPlay();
					this.actFIFO.tFadeOutStart();
					base.ePhaseID = CStage.EPhase.Common_FADEOUT;
				} else if (this.bMenuFocus) {
					OpenTaiko.Skin.soundDecideSFX.tPlay();
					this.bMenuFocus = false;
					this.tDescriptionPanelCurrentSelectedItemDescriptionDraw();
				} else {
					switch (this.eItemPanelMode) {
						case EItemPanelMode.PadList:
							bool bIsKeyAssignSelectedBeforeHitEnter = this.actList.bIsKeyAssignSelected;    // #24525 2011.3.15 yyagi
							this.actList.tEnterPressed();

							this.tDescriptionPanelCurrentSelectedItemDescriptionDraw();

							if (this.actList.bCurrentSelectedItemReturnToMenu) {
								this.tDescriptionPanelCurrentSelectedMenuDescriptionDraw();
								if (bIsKeyAssignSelectedBeforeHitEnter == false)                            // #24525 2011.3.15 yyagi
								{
									this.bMenuFocus = true;
								}
							}
							break;

						case EItemPanelMode.KeyCodeList:
							this.actKeyAssign.tEnterPressed();
							break;
					}
				}
			}
			this.ctKeyRepeat.Up.KeyIntervalFunc(OpenTaiko.InputManager.Keyboard.KeyPressing((int)SlimDXKeys.Key.UpArrow), new CCounter.KeyProcess(this.tCursorTopMove));
			if (OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.SD)) {
				this.tCursorTopMove();
			}
			this.ctKeyRepeat.Down.KeyIntervalFunc(OpenTaiko.InputManager.Keyboard.KeyPressing((int)SlimDXKeys.Key.DownArrow), new CCounter.KeyProcess(this.tCursorBottomMove));
			if (OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.LT)) {
				this.tCursorBottomMove();
			}
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

	private ScriptBG Background;

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
