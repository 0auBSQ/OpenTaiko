﻿using System.Drawing;
using System.Runtime.InteropServices;
using FDK;


namespace OpenTaiko;

internal class CActSelectPopupMenu : CActivity {
	private static List<CActSelectPopupMenu> Child = new List<CActSelectPopupMenu>();

	public CActSelectPopupMenu() {
		Child.Add(this);
	}

	// Properties


	public int GetIndex(int pos) {
		return lciMenuItems[pos].cItem.GetIndex();
	}
	public object GetObj現在値(int pos) {
		return lciMenuItems[pos].cItem.obj現在値();
	}
	public bool bGotoDetailConfig {
		get;
		internal set;
	}

	/// <summary>
	/// ソートメニュー機能を使用中かどうか。外部からこれをtrueにすると、ソートメニューが出現する。falseにすると消える。
	/// </summary>
	public bool bIsActivePopupMenu {
		get;
		private set;
	}
	public virtual void tActivatePopupMenu(EInstrumentPad einst) {
		nItemSelecting = -1;        // #24757 2011.4.1 yyagi: Clear sorting status in each stating menu.
		this.bIsActivePopupMenu = true;
		this.bIsSelectingIntItem = false;
		this.bGotoDetailConfig = false;
	}
	public virtual void tDeativatePopupMenu() {
		this.bIsActivePopupMenu = false;
	}


	protected void Initialize(List<CItemBase> menulist, bool showAllItems, string title) {
		Initialize(menulist, showAllItems, title, 0);
	}

	protected void Initialize(List<CItemBase> menulist, bool showAllItems, string title, int defaultPos) {
		InitializePrvFont();

		b選択した = false;
		stqMenuTitle = new stQuickMenuItem();
		stqMenuTitle.cItem = new CItemBase();
		stqMenuTitle.cItem.str項目名 = title;
		using (var bitmap = prvFont.DrawText(title, Color.White, Color.Black, null, 30)) {
			stqMenuTitle.txName = OpenTaiko.tテクスチャの生成(bitmap, false);
		}
		lciMenuItems = new stQuickMenuItem[menulist.Count];
		for (int i = 0; i < menulist.Count; i++) {
			stQuickMenuItem stqm = new stQuickMenuItem();
			stqm.cItem = menulist[i];
			using (var bitmap = prvFont.DrawText(menulist[i].str項目名, Color.White, Color.Black, null, 30)) {
				stqm.txName = OpenTaiko.tテクスチャの生成(bitmap, false);
			}
			lciMenuItems[i] = stqm;
		}

		bShowAllItems = showAllItems;
		n現在の選択行 = defaultPos;
	}

	private void InitializePrvFont() {
		prvFont?.Dispose();
		prvFont = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.PopupMenu_Font_Size);
	}

	public static void RefleshSkin() {
		for (int i = 0; i < Child.Count; i++) {
			Child[i]._RefleshSkin();
		}
	}

	public void _RefleshSkin() {
		OpenTaiko.tDisposeSafely(ref prvFont);
		InitializePrvFont();

		using (var bitmap = prvFont.DrawText(stqMenuTitle.cItem?.str項目名 ?? "", Color.White, Color.Black, null, 30)) {
			OpenTaiko.tDisposeSafely(ref stqMenuTitle.txName);
			stqMenuTitle.txName = OpenTaiko.tテクスチャの生成(bitmap, false);
		}
		if (lciMenuItems != null) {
			for (int i = 0; i < lciMenuItems.Length; i++) {
				using (var bitmap = prvFont.DrawText(lciMenuItems[i].cItem?.str項目名 ?? "", Color.White, Color.Black, null, 30)) {
					OpenTaiko.tDisposeSafely(ref lciMenuItems[i].txName);
					lciMenuItems[i].txName = OpenTaiko.tテクスチャの生成(bitmap, false);
				}
			}
		}
	}

	public void tEnter押下() {
		if (this.bキー入力待ち) {
			OpenTaiko.Skin.soundDecideSFX.tPlay();

			if (this.n現在の選択行 != lciMenuItems.Length - 1) {
				if (lciMenuItems[n現在の選択行].cItem.e種別 == CItemBase.E種別.リスト ||
					lciMenuItems[n現在の選択行].cItem.e種別 == CItemBase.E種別.ONorOFFトグル ||
					lciMenuItems[n現在の選択行].cItem.e種別 == CItemBase.E種別.ONorOFFor不定スリーステート) {
					lciMenuItems[n現在の選択行].cItem.t項目値を次へ移動();
				} else if (lciMenuItems[n現在の選択行].cItem.e種別 == CItemBase.E種別.整数) {
					bIsSelectingIntItem = !bIsSelectingIntItem;     // 選択状態/選択解除状態を反転する
				} else if (lciMenuItems[n現在の選択行].cItem.e種別 == CItemBase.E種別.切替リスト) {
					// 特に何もしない
				} else {
					throw new ArgumentException();
				}
				nItemSelecting = n現在の選択行;
			}
			tEnter押下Main((int)lciMenuItems[n現在の選択行].cItem.GetIndex());

			this.bキー入力待ち = true;
		}
	}

	/// <summary>
	/// Decide押下時の処理を、継承先で記述する。
	/// </summary>
	/// <param name="val">CItemBaseの現在の設定値のindex</param>
	public virtual void tEnter押下Main(int val) {
	}
	/// <summary>
	/// Cancel押下時の追加処理があれば、継承先で記述する。
	/// </summary>
	public virtual void tCancel() {
	}
	/// <summary>
	/// 追加の描画処理。必要に応じて、継承先で記述する。
	/// </summary>
	public virtual void t進行描画sub() {
	}

	public void t次に移動() {
		if (this.bキー入力待ち) {
			OpenTaiko.Skin.soundカーソル移動音.tPlay();
			if (bIsSelectingIntItem) {
				lciMenuItems[n現在の選択行].cItem.t項目値を前へ移動();        // 項目移動と数値上下は方向が逆になるので注意
			} else {
				if (++this.n現在の選択行 >= this.lciMenuItems.Length) {
					this.n現在の選択行 = 0;
				}
			}
		}
	}
	public void t前に移動() {
		if (this.bキー入力待ち) {
			OpenTaiko.Skin.soundカーソル移動音.tPlay();
			if (bIsSelectingIntItem) {
				lciMenuItems[n現在の選択行].cItem.t項目値を次へ移動();        // 項目移動と数値上下は方向が逆になるので注意
			} else {
				if (--this.n現在の選択行 < 0) {
					this.n現在の選択行 = this.lciMenuItems.Length - 1;
				}
			}
		}
	}

	// CActivity 実装

	public override void Activate() {
		//		this.n現在の選択行 = 0;
		this.bキー入力待ち = true;
		for (int i = 0; i < 4; i++) {
			this.ctキー反復用[i] = new CCounter(0, 0, 0, OpenTaiko.Timer);
		}
		base.IsDeActivated = true;
		b選択した = false;
		this.bIsActivePopupMenu = false;
		this.font = new CActDFPFont();
		base.ChildActivities.Add(this.font);
		nItemSelecting = -1;

		base.Activate();
	}
	public override void DeActivate() {
		if (!base.IsDeActivated) {
			base.ChildActivities.Remove(this.font);
			this.font.DeActivate();
			this.font = null;

			//CDTXMania.tテクスチャの解放( ref this.txCursor );
			//CDTXMania.tテクスチャの解放( ref this.txPopupMenuBackground );
			for (int i = 0; i < 4; i++) {
				this.ctキー反復用[i] = null;
			}
			base.DeActivate();
		}
	}

	public override void CreateManagedResource() {
		base.CreateManagedResource();

		InitializePrvFont();
	}

	public override void ReleaseManagedResource() {
		//CDTXMania.tテクスチャの解放( ref this.txPopupMenuBackground );
		//CDTXMania.tテクスチャの解放( ref this.txCursor );
		OpenTaiko.tDisposeSafely(ref this.prvFont);
		base.ReleaseManagedResource();
	}

	public override int Draw() {
		if (!base.IsDeActivated && this.bIsActivePopupMenu) {
			if (this.bキー入力待ち) {
				#region [ Shift-F1: CONFIG画面 ]
				if ((OpenTaiko.InputManager.Keyboard.KeyPressing((int)SlimDXKeys.Key.RightShift) || OpenTaiko.InputManager.Keyboard.KeyPressing((int)SlimDXKeys.Key.LeftShift)) &&
					OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.F1)) {  // [SHIFT] + [F1] CONFIG
					OpenTaiko.Skin.soundCancelSFX.tPlay();
					tCancel();
					this.bGotoDetailConfig = true;
				}
				#endregion
				#region [ キー入力: キャンセル ]
				else if ((OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape)
						  || OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, EPad.FT)
						  || OpenTaiko.Pad.bPressedGB(EPad.Cancel))
						 && this.bEsc有効) {   // キャンセル
					OpenTaiko.Skin.soundCancelSFX.tPlay();
					tCancel();
					this.bIsActivePopupMenu = false;
				}
				#endregion

				if (!b選択した) {
					#region [ キー入力: 決定 ]
					// E楽器パート eInst = E楽器パート.UNKNOWN;
					ESortAction eAction = ESortAction.END;
					if (
						OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, EPad.Decide)
						|| OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, EPad.RD)
						|| OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, EPad.LC)
						|| OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, EPad.LRed)
						|| OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, EPad.RRed)
						|| (OpenTaiko.ConfigIni.bEnterIsNotUsedInKeyAssignments && OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return))) {
						eAction = ESortAction.Decide;
					}
					if (eAction == ESortAction.Decide)  // 決定
					{
						this.tEnter押下();
					}
					#endregion
					#region [ キー入力: 前に移動 ]
					this.ctキー反復用.Up.KeyIntervalFunc(OpenTaiko.InputManager.Keyboard.KeyPressing((int)SlimDXKeys.Key.UpArrow), new CCounter.KeyProcess(this.t前に移動));
					this.ctキー反復用.R.KeyIntervalFunc(OpenTaiko.Pad.IsPressingGB(EPad.R), new CCounter.KeyProcess(this.t前に移動));
					if (OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, EPad.SD) || OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, EPad.LBlue)) {
						this.t前に移動();
					}
					#endregion
					#region [ キー入力: 次に移動 ]
					this.ctキー反復用.Down.KeyIntervalFunc(OpenTaiko.InputManager.Keyboard.KeyPressing((int)SlimDXKeys.Key.DownArrow), new CCounter.KeyProcess(this.t次に移動));
					this.ctキー反復用.B.KeyIntervalFunc(OpenTaiko.Pad.IsPressingGB(EPad.B), new CCounter.KeyProcess(this.t次に移動));
					if (OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, EPad.LT) || OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, EPad.RBlue)) {
						this.t次に移動();
					}
					#endregion
				}
			}
			#region [ ポップアップメニュー 背景描画 ]
			if (OpenTaiko.Tx.Menu_Title != null) {
				OpenTaiko.Tx.Menu_Title.t2D描画(OpenTaiko.Skin.PopupMenu_Menu_Title[0], OpenTaiko.Skin.PopupMenu_Menu_Title[1]);
			}
			#endregion
			#region [ ソートメニュータイトル描画 ]
			stqMenuTitle.txName.t2D描画(OpenTaiko.Skin.PopupMenu_Title[0], OpenTaiko.Skin.PopupMenu_Title[1]);
			#endregion
			#region [ カーソル描画 ]
			if (OpenTaiko.Tx.Menu_Highlight != null) {
				int curX = OpenTaiko.Skin.PopupMenu_Menu_Highlight[0] + (OpenTaiko.Skin.PopupMenu_Move[0] * (this.n現在の選択行 + 1));
				int curY = OpenTaiko.Skin.PopupMenu_Menu_Highlight[1] + (OpenTaiko.Skin.PopupMenu_Move[1] * (this.n現在の選択行 + 1));

				int width = OpenTaiko.Tx.Menu_Highlight.szTextureSize.Width / 2;
				int height = OpenTaiko.Tx.Menu_Highlight.szTextureSize.Height;

				OpenTaiko.Tx.Menu_Highlight.t2D描画(curX, curY, new Rectangle(0, 0, width, height));
				curX += width;
				Rectangle rectangle = new Rectangle(width / 2, 0, width, height);
				for (int j = 0; j < 16; j++) {
					OpenTaiko.Tx.Menu_Highlight.t2D描画(curX, curY, rectangle);
					curX += width;
				}
				OpenTaiko.Tx.Menu_Highlight.t2D描画(curX, curY, new Rectangle(width, 0, width, height));
			}
			#endregion
			#region [ ソート候補文字列描画 ]
			for (int i = 0; i < lciMenuItems.Length; i++) {
				bool bItemBold = (i == nItemSelecting && !bShowAllItems) ? true : false;
				//font.t文字列描画( 190, 80 + i * 32, lciMenuItems[ i ].cItem.str項目名, bItemBold, 1.0f );
				if (lciMenuItems[i].txName != null) {
					lciMenuItems[i].txName.t2D描画(OpenTaiko.Skin.PopupMenu_MenuItem_Name[0] + i * OpenTaiko.Skin.PopupMenu_Move[0],
						OpenTaiko.Skin.PopupMenu_MenuItem_Name[1] + i * OpenTaiko.Skin.PopupMenu_Move[1]);
				}

				bool bValueBold = (bItemBold || (i == nItemSelecting && bIsSelectingIntItem)) ? true : false;
				if (bItemBold || bShowAllItems) {
					string s;
					if (lciMenuItems[i].cItem.str項目名 == CLangManager.LangInstance.GetString("MOD_SONGSPEED")) {
						double d = (double)((int)lciMenuItems[i].cItem.obj現在値() / 20.0);
						s = "x" + d.ToString("0.00");
					} else if (lciMenuItems[i].cItem.str項目名 == CLangManager.LangInstance.GetString("MOD_SPEED")) {
						double d = (double)((((int)lciMenuItems[i].cItem.obj現在値()) + 1) / 10.0);
						s = "x" + d.ToString("0.0");
					} else {
						s = lciMenuItems[i].cItem.obj現在値().ToString();
					}
					//               switch (lciMenuItems[i].cItem.str項目名)
					//               {
					//                   case "演奏速度":
					//                       {
					//                           double d = (double)((int)lciMenuItems[i].cItem.obj現在値() / 20.0);
					//                           s = "x" + d.ToString("0.000");
					//                       }
					//                       break;
					//                   case "ばいそく":
					//                       {
					//double d = (double)((((int)lciMenuItems[i].cItem.obj現在値()) + 1) / 10.0);
					//s = "x" + d.ToString("0.0");
					//                       }
					//                       break;

					//                   default:
					//                       s = lciMenuItems[i].cItem.obj現在値().ToString();
					//                       break;
					//               }
					//font.t文字列描画( (int)(340 * Scale.X), (int)(80 + i * 32), s, bValueBold, 1.0f * Scale.Y);
					using (var bmpStr = bValueBold ?
							   prvFont.DrawText(s, Color.White, Color.OrangeRed, null, Color.Yellow, Color.OrangeRed, 30) :
							   prvFont.DrawText(s, Color.White, Color.Black, null, 30)) {
						using (var ctStr = OpenTaiko.tテクスチャの生成(bmpStr, false)) {
							ctStr.t2D描画(OpenTaiko.Skin.PopupMenu_MenuItem_Value[0] + i * OpenTaiko.Skin.PopupMenu_Move[0],
								OpenTaiko.Skin.PopupMenu_MenuItem_Value[1] + i * OpenTaiko.Skin.PopupMenu_Move[1]);
						}
					}
				}
			}
			#endregion
			t進行描画sub();
		}
		return 0;
	}


	// その他

	#region [ private ]
	//-----------------
	private bool bキー入力待ち;
	protected bool bEsc有効;

	internal int n現在の選択行;

	//private CTexture txPopupMenuBackground;
	//private CTexture txCursor;
	private CActDFPFont font;
	CCachedFontRenderer prvFont;

	internal struct stQuickMenuItem {
		internal CItemBase cItem;
		internal CTexture txName;
	}
	private stQuickMenuItem[] lciMenuItems;
	private stQuickMenuItem stqMenuTitle;
	private string strMenuTitle;
	private bool bShowAllItems;
	private bool bIsSelectingIntItem;
	public static bool b選択した;
	[StructLayout(LayoutKind.Sequential)]
	private struct STキー反復用カウンタ {
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
	private STキー反復用カウンタ ctキー反復用;

	private enum ESortAction : int {
		Cancel, Decide, Previous, Next, END
	}
	private int nItemSelecting;     // 「n現在の選択行」とは別に設ける。sortでメニュー表示直後にアイテムの中身を表示しないようにするため
									//-----------------
	#endregion
}
