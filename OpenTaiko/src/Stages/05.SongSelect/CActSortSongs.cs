using System.Collections.Generic;

namespace TJAPlayer3
{
	internal class CActSortSongs : CActSelectPopupMenu
	{

		// コンストラクタ

		public CActSortSongs()
		{
			List<CItemBase> lci = new List<CItemBase>();
			lci.Add( new CItemList( CLangManager.LangInstance.GetString(9201),		CItemBase.Eパネル種別.通常, 0, "", "", new string[] { "Z,Y,X,...",		"A,B,C,..." } ) );
			lci.Add( new CItemList(CLangManager.LangInstance.GetString(9202),		CItemBase.Eパネル種別.通常, 0, "", "", new string[] { "Z,Y,X,...",		"A,B,C,..." } ) );
            lci.Add(new CItemList(CLangManager.LangInstance.GetString(9203), CItemBase.Eパネル種別.通常, 0, "", "", new string[] { "Z,Y,X,...", "A,B,C,..." }));
            lci.Add( new CItemList(CLangManager.LangInstance.GetString(9204),		CItemBase.Eパネル種別.通常, 0, "", "", new string[] { "13,12,11,...",	"1,2,3,..." } ) );
            //lci.Add( new CItemList( "Best Rank",	CItemBase.Eパネル種別.通常, 0, "", "", new string[] { "E,D,C,...",		"SS,S,A,..." } ) );
            //lci.Add( new CItemList( "PlayCount",	CItemBase.Eパネル種別.通常, 0, "", "", new string[] { "10,9,8,...",		"1,2,3,..." } ) );
            
            //lci.Add( new CItemList( "SkillPoint",	CItemBase.Eパネル種別.通常, 0, "", "", new string[] { "100,99,98,...",	"1,2,3,..." } ) );
#if TEST_SORTBGM
			lci.Add( new CItemList( "BPM",			CItemBase.Eパネル種別.通常, 0, "", "", new string[] { "300,200,...",	"70,80,90,..." } ) );
#endif
			//lci.Add( new CItemList( "ジャンル",			CItemBase.Eパネル種別.通常, 0, "", "", new string[] { "AC15",	"AC8-14" } ) );
			lci.Add( new CItemList(CLangManager.LangInstance.GetString(9200),		CItemBase.Eパネル種別.通常, 0, "", "", new string[] { "", 				"" } ) );
			
			base.Initialize( lci, false, "SORT MENU" );
		}


		// メソッド
		public void tActivatePopupMenu( EInstrumentPad einst, ref CActSelect曲リスト ca )
		{
		    this.act曲リスト = ca;
			base.tActivatePopupMenu( einst );
		}
		//public void tDeativatePopupMenu()
		//{
		//	base.tDeativatePopupMenu();
		//}


		public override void tEnter押下Main( int nSortOrder )
		{
			nSortOrder *= 2;	// 0,1  => -1, 1
			nSortOrder -= 1;
			switch ( (EOrder)n現在の選択行 )
			{
				case EOrder.Path:
					this.act曲リスト.t曲リストのソート(
					    CSongs管理.t曲リストのソート1_絶対パス順, eInst, nSortOrder
					);
					this.act曲リスト.t選択曲が変更された(true);
					break;
				case EOrder.Title:
					this.act曲リスト.t曲リストのソート(
					    CSongs管理.t曲リストのソート2_タイトル順, eInst, nSortOrder
					);
					this.act曲リスト.t選択曲が変更された(true);
					break;
                case EOrder.Subtitle:
                    this.act曲リスト.t曲リストのソート(
                        CSongs管理.tSongListSortBySubtitle, eInst, nSortOrder
                    );
                    this.act曲リスト.t選択曲が変更された( true );
                    break;
                case EOrder.Level:
                    this.act曲リスト.t曲リストのソート(
                        CSongs管理.tSongListSortByLevel, eInst, nSortOrder
                    );
                    this.act曲リスト.t選択曲が変更された(true);
                    break;
#if TEST_SORTBGM
						case (int) ESortItem.BPM:
						this.act曲リスト.t曲リストのソート(
							CSongs管理.t曲リストのソート9_BPM順, eInst, nSortOrder,
							this.act曲リスト.n現在のアンカ難易度レベル
						);
					this.act曲リスト.t選択曲が変更された(true);
						break;
#endif
                case EOrder.Return:
					this.tDeativatePopupMenu();
					break;
				default:
					break;
			}
		}
		
		// CActivity 実装

		public override void Activate()
		{
            //this.e現在のソート = EOrder.Title;
			base.Activate();
		}
		public override void DeActivate()
		{
			if( !base.IsDeActivated )
			{
				base.DeActivate();
			}
		}
		public override void CreateManagedResource()
		{
			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource()
		{
			base.ReleaseManagedResource();
		}

		#region [ private ]
		//-----------------

		private CActSelect曲リスト act曲リスト;

		private enum EOrder : int
		{
            Path = 0,
			Title = 1,
			Subtitle = 2,
			Level = 3,
            Return = 4
		}

		//-----------------
		#endregion
	}


}
