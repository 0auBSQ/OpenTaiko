using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Drawing;
using System.IO;
using FDK;

namespace TJAPlayer3
{
	internal class CAct演奏PauseMenu : CActSelectPopupMenu
	{
		private readonly string QuickCfgTitle = "ポーズ";
		// コンストラクタ

		public CAct演奏PauseMenu()
		{
            CAct演奏PauseMenuMain();
		}

        private void CAct演奏PauseMenuMain()
		{
            this.bEsc有効 = false;
			lci = new List<List<List<CItemBase>>>();									// この画面に来る度に、メニューを作り直す。
			for ( int nConfSet = 0; nConfSet < (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan ? 3 : 2); nConfSet++ )
			{
				lci.Add( new List<List<CItemBase>>() );									// ConfSet用の3つ分の枠。
				for ( int nInst = 0; nInst < 3; nInst++ )
				{
					lci[ nConfSet ].Add( null );										// Drum/Guitar/Bassで3つ分、枠を作っておく
					lci[ nConfSet ][ nInst ] = MakeListCItemBase( nConfSet, nInst );
				}
			}
			base.Initialize( lci[ nCurrentConfigSet ][ 0 ], true, QuickCfgTitle, 0 );	// ConfSet=0, nInst=Drums
		}

		private List<CItemBase> MakeListCItemBase( int nConfigSet, int nInst )
		{
			List<CItemBase> l = new List<CItemBase>();

            #region [ 共通 SET切り替え/More/Return ]
            l.Add(new CSwitchItemList(CLangManager.LangInstance.GetString(900), CItemBase.Eパネル種別.通常, 0, "", "", new string[] { "" }));
            if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan) l.Add(new CSwitchItemList(CLangManager.LangInstance.GetString(901), CItemBase.Eパネル種別.通常, 0, "", "", new string[] { "" }));
            l.Add(new CSwitchItemList(CLangManager.LangInstance.GetString(902), CItemBase.Eパネル種別.通常, 0, "", "", new string[] { "", "" }));
			#endregion

			return l;
		}

		// メソッド
		public override void tActivatePopupMenu( E楽器パート einst )
		{
            this.CAct演奏PauseMenuMain();
			CActSelectPopupMenu.b選択した = false;
			this.bやり直しを選択した = false;
			base.tActivatePopupMenu( einst );
		}
		//public void tDeativatePopupMenu()
		//{
		//	base.tDeativatePopupMenu();
		//}

		public override void t進行描画sub()
		{
            if( this.bやり直しを選択した )
            {
                if( !sw.IsRunning )
                    this.sw = Stopwatch.StartNew();
                if( sw.ElapsedMilliseconds > 1500 )
                {
                    TJAPlayer3.stage演奏ドラム画面.bPAUSE = false;
                    TJAPlayer3.stage演奏ドラム画面.t演奏やりなおし();

	    		    this.tDeativatePopupMenu();
                    this.sw.Reset();
                }
            }
		}

		public override void tEnter押下Main( int nSortOrder )
		{
            switch ( n現在の選択行 )
            {
				case (int) EOrder.Continue:
                    TJAPlayer3.stage演奏ドラム画面.bPAUSE = false;

                    CSound管理.rc演奏用タイマ.t再開();
					TJAPlayer3.Timer.t再開();
					TJAPlayer3.DTX.t全チップの再生再開();
                    TJAPlayer3.stage演奏ドラム画面.actAVI.tPauseControl();
					CActSelectPopupMenu.b選択した = true;
					this.tDeativatePopupMenu();
					break;

				case (int) EOrder.Redoing:
					if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan)
					{
						this.bやり直しを選択した = true;
						CActSelectPopupMenu.b選択した = true;
					}
                    else
					{
						CSound管理.rc演奏用タイマ.t再開();
						TJAPlayer3.Timer.t再開();
						TJAPlayer3.stage演奏ドラム画面.t演奏中止();
						CActSelectPopupMenu.b選択した = true;
						this.tDeativatePopupMenu();
					}
					break;

				case (int) EOrder.Return:
                    CSound管理.rc演奏用タイマ.t再開();
					TJAPlayer3.Timer.t再開();
                    TJAPlayer3.stage演奏ドラム画面.t演奏中止();
					CActSelectPopupMenu.b選択した = true;
					this.tDeativatePopupMenu();
                    break;
                default:
                    break;
            }
		}

		public override void tCancel()
		{
		}

		// CActivity 実装

		public override void On活性化()
		{
			base.On活性化();
			this.bGotoDetailConfig = false;
            this.sw = new Stopwatch();
		}
		public override void On非活性化()
		{
			base.On非活性化();
		}
		public override void OnManagedリソースの作成()
		{
			if( !base.b活性化してない )
			{
				string pathパネル本体 = CSkin.Path( @"Graphics\ScreenSelect popup auto settings.png" );
				if ( File.Exists( pathパネル本体 ) )
				{
					this.txパネル本体 = TJAPlayer3.tテクスチャの生成( pathパネル本体, true );
				}

				base.OnManagedリソースの作成();
			}
		}
		public override void OnManagedリソースの解放()
		{
			if ( !base.b活性化してない )
			{
				TJAPlayer3.tテクスチャの解放( ref this.txパネル本体 );
				TJAPlayer3.tテクスチャの解放( ref this.tx文字列パネル );
				base.OnManagedリソースの解放();
			}
		}

		#region [ private ]
		//-----------------
		private int nCurrentTarget = 0;
		private int nCurrentConfigSet = 0;
		private List<List<List<CItemBase>>> lci;
		private enum EOrder : int
		{
			Continue,
			Redoing,
			Return, END,
			Default = 99
		};

		private bool b選択した;
		private CTexture txパネル本体;
		private CTexture tx文字列パネル;
        private Stopwatch sw;
        private bool bやり直しを選択した;
		//-----------------
		#endregion
	}


}
