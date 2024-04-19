using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Drawing.Text;
using System.Threading.Tasks;
using Silk.NET.Maths;
using FDK;
using System.Linq;

using Rectangle = System.Drawing.Rectangle;
using Point = System.Drawing.Point;
using Color = System.Drawing.Color;
using RectangleF = System.Drawing.RectangleF;
using SkiaSharp;

namespace TJAPlayer3
{
	internal class CActSelect曲リスト : CActivity
	{
		// プロパティ

		public bool bIsEnumeratingSongs
		{
			get;
			set;
		}
		public bool bスクロール中
		{
			get
			{
				return ctScrollCounter.CurrentValue != ctScrollCounter.EndValue;
			}
		}
		public double fNowScrollAnime
        {
            get
            {
				double value = ctScrollCounter.CurrentValue / 1000.0;
				return Math.Sin(value * Math.PI / 2.0);
			}
        }
		public int n現在のアンカ難易度レベル 
		{
			get;
			private set;
		}
		public int n現在選択中の曲の現在の難易度レベル
		{
			get
			{
				return this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す( this.rCurrentlySelectedSong );
			}
		}
		public Cスコア r現在選択中のスコア
		{
			get
			{
				if( this.rCurrentlySelectedSong != null )
				{
					return this.rCurrentlySelectedSong.arスコア[ this.n現在選択中の曲の現在の難易度レベル ];
				}
				return null;
			}
		}
		public int nSelectSongIndex
        {
			get;
			private set;
		}
		public CSongListNode rPrevSelectedSong
		{
			get;
			private set;
		}
		private CSongListNode _rNowSelectedSong;
		public CSongListNode rCurrentlySelectedSong 
		{
            get
            {
				return _rNowSelectedSong;
			}
            set
            {
				rPrevSelectedSong = rCurrentlySelectedSong;
				_rNowSelectedSong = value;
			}
		}

		public void ResetSongIndex()
        {
			nSelectSongIndex = 0;
			this.rCurrentlySelectedSong = TJAPlayer3.Songs管理.list曲ルート[nSelectSongIndex];
		}

		public int nスクロールバー相対y座標
		{
			get;
			private set;
		}

		// t選択曲が変更された()内で使う、直前の選曲の保持
		// (前と同じ曲なら選択曲変更に掛かる再計算を省略して高速化するため)
		private CSongListNode song_last = null;
		
		// コンストラクタ

		public CActSelect曲リスト()
		{
			#region[ レベル数字 ]
			STレベル数字[] stレベル数字Ar = new STレベル数字[10];
			STレベル数字 st数字0 = new STレベル数字();
			STレベル数字 st数字1 = new STレベル数字();
			STレベル数字 st数字2 = new STレベル数字();
			STレベル数字 st数字3 = new STレベル数字();
			STレベル数字 st数字4 = new STレベル数字();
			STレベル数字 st数字5 = new STレベル数字();
			STレベル数字 st数字6 = new STレベル数字();
			STレベル数字 st数字7 = new STレベル数字();
			STレベル数字 st数字8 = new STレベル数字();
			STレベル数字 st数字9 = new STレベル数字();

			st数字0.ch = '0';
			st数字1.ch = '1';
			st数字2.ch = '2';
			st数字3.ch = '3';
			st数字4.ch = '4';
			st数字5.ch = '5';
			st数字6.ch = '6';
			st数字7.ch = '7';
			st数字8.ch = '8';
			st数字9.ch = '9';
			st数字0.ptX = 0;
			st数字1.ptX = 21;
			st数字2.ptX = 42;
			st数字3.ptX = 63;
			st数字4.ptX = 84;
			st数字5.ptX = 105;
			st数字6.ptX = 126;
			st数字7.ptX = 147;
			st数字8.ptX = 168;
			st数字9.ptX = 189;

			stレベル数字Ar[0] = st数字0;
			stレベル数字Ar[1] = st数字1;
			stレベル数字Ar[2] = st数字2;
			stレベル数字Ar[3] = st数字3;
			stレベル数字Ar[4] = st数字4;
			stレベル数字Ar[5] = st数字5;
			stレベル数字Ar[6] = st数字6;
			stレベル数字Ar[7] = st数字7;
			stレベル数字Ar[8] = st数字8;
			stレベル数字Ar[9] = st数字9;
			this.st小文字位置 = stレベル数字Ar;
			#endregion

			this.nSelectSongIndex = 0;
			this.rCurrentlySelectedSong = null;

			this.n現在のアンカ難易度レベル = Math.Min((int)Difficulty.Edit, TJAPlayer3.ConfigIni.nDefaultCourse);
			base.IsDeActivated = true;
			this.bIsEnumeratingSongs = false;
		}


		// メソッド

		// Closest level
		public int n現在のアンカ難易度レベルに最も近い難易度レベルを返す( CSongListNode song )
		{
			// 事前チェック。

			if( song == null )
				return this.n現在のアンカ難易度レベル;	// 曲がまったくないよ

			if( song.arスコア[ this.n現在のアンカ難易度レベル ] != null )
				return this.n現在のアンカ難易度レベル;	// 難易度ぴったりの曲があったよ

			if( ( song.eノード種別 == CSongListNode.ENodeType.BOX ) || ( song.eノード種別 == CSongListNode.ENodeType.BACKBOX ) )
				return 0;								// BOX と BACKBOX は関係無いよ


			// 現在のアンカレベルから、難易度上向きに検索開始。

			int n最も近いレベル = this.n現在のアンカ難易度レベル;

			for( int i = 0; i < (int)Difficulty.Total; i++ )
			{
				if( song.arスコア[ n最も近いレベル ] != null )
					break;	// 曲があった。

				n最も近いレベル = ( n最も近いレベル + 1 ) % (int)Difficulty.Total;	// 曲がなかったので次の難易度レベルへGo。（5以上になったら0に戻る。）
			}


			// 見つかった曲がアンカより下のレベルだった場合……
			// アンカから下向きに検索すれば、もっとアンカに近い曲があるんじゃね？

			if( n最も近いレベル < this.n現在のアンカ難易度レベル )
			{
				// 現在のアンカレベルから、難易度下向きに検索開始。

				n最も近いレベル = this.n現在のアンカ難易度レベル;

				for( int i = 0; i < (int)Difficulty.Total; i++ )
				{
					if( song.arスコア[ n最も近いレベル ] != null )
						break;	// 曲があった。

					n最も近いレベル = ( ( n最も近いレベル - 1 ) + (int)Difficulty.Total) % (int)Difficulty.Total;	// 曲がなかったので次の難易度レベルへGo。（0未満になったら4に戻る。）
				}
			}

			return n最も近いレベル;
		}
		public CSongListNode r指定された曲が存在するリストの先頭の曲( CSongListNode song )
		{
			List<CSongListNode> songList = GetSongListWithinMe( song );
			return ( songList == null ) ? null : songList[ 0 ];
		}
		public CSongListNode r指定された曲が存在するリストの末尾の曲( CSongListNode song )
		{
			List<CSongListNode> songList = GetSongListWithinMe( song );
			return ( songList == null ) ? null : songList[ songList.Count - 1 ];
		}

		private List<CSongListNode> GetSongListWithinMe( CSongListNode song )
		{
			if ( song.rParentNode == null )					// root階層のノートだったら
			{
				return TJAPlayer3.Songs管理.list曲ルート;	// rootのリストを返す
			}
			else
			{
				if ( ( song.rParentNode.list子リスト != null ) && ( song.rParentNode.list子リスト.Count > 0 ) )
				{
					return song.rParentNode.list子リスト;
				}
				else
				{
					return null;
				}
			}
		}


		public delegate void DGSortFunc( List<CSongListNode> songList, EInstrumentPad eInst, int order, params object[] p);
		/// <summary>
		/// 主にCSong管理.cs内にあるソート機能を、delegateで呼び出す。
		/// </summary>
		/// <param name="sf">ソート用に呼び出すメソッド</param>
		/// <param name="eInst">ソート基準とする楽器</param>
		/// <param name="order">-1=降順, 1=昇順</param>
		public void t曲リストのソート( DGSortFunc sf, EInstrumentPad eInst, int order, params object[] p )
		{
			List<CSongListNode> songList = GetSongListWithinMe( this.rCurrentlySelectedSong );
			if ( songList == null )
			{
				// 何もしない;
			}
			else
			{
//				CDTXMania.Songs管理.t曲リストのソート3_演奏回数の多い順( songList, eInst, order );
				sf( songList, eInst, order, p );
				//				this.r現在選択中の曲 = CDTXMania
				void addBackBox(List<CSongListNode> list, string parentName = "/")
				{
					foreach (CSongListNode node in list)
					{
						if (node.eノード種別 != CSongListNode.ENodeType.BOX) continue;
						string newPath = parentName + node.strタイトル + "/";
						CSongDict.tReinsertBackButtons(node, node.list子リスト, newPath, TJAPlayer3.Songs管理.listStrBoxDefSkinSubfolderFullName);

						addBackBox(node.list子リスト, newPath);
					}
				}
				addBackBox(TJAPlayer3.Songs管理.list曲ルート);
				this.t現在選択中の曲を元に曲バーを再構成する();
				tChangeSong(0);
				this.t選択曲が変更された(false);
				tUpdateCurSong();
				TJAPlayer3.stageSongSelect.tNotifySelectedSongChange();
			}
		}

		public void tResetTitleKey()
		{
			this.ttk選択している曲の曲名 = null;
			this.ttk選択している曲のサブタイトル = null;
			this.ttkSelectedSongMaker = null;
			this.ttkSelectedSongBPM = null;
		}

		public bool tBOXに入る()
		{
			//Trace.TraceInformation( "box enter" );
			//Trace.TraceInformation( "Skin現在Current : " + CDTXMania.Skin.GetCurrentSkinSubfolderFullName(false) );
			//Trace.TraceInformation( "Skin現在System  : " + CSkin.strSystemSkinSubfolderFullName );
			//Trace.TraceInformation( "Skin現在BoxDef  : " + CSkin.strBoxDefSkinSubfolderFullName );
			//Trace.TraceInformation( "Skin現在: " + CSkin.GetSkinName( CDTXMania.Skin.GetCurrentSkinSubfolderFullName(false) ) );
			//Trace.TraceInformation( "Skin現pt: " + CDTXMania.Skin.GetCurrentSkinSubfolderFullName(false) );
			//Trace.TraceInformation( "Skin指定: " + CSkin.GetSkinName( this.r現在選択中の曲.strSkinPath ) );
			//Trace.TraceInformation( "Skinpath: " + this.r現在選択中の曲.strSkinPath );
			bool ret = false;
			if (CSkin.GetSkinName(TJAPlayer3.Skin.GetCurrentSkinSubfolderFullName(false)) != CSkin.GetSkinName(this.rCurrentlySelectedSong.strSkinPath)
				&& CSkin.bUseBoxDefSkin)
			{
				ret = true;
				// BOXに入るときは、スキン変更発生時のみboxdefスキン設定の更新を行う
				TJAPlayer3.Skin.SetCurrentSkinSubfolderFullName(
					TJAPlayer3.Skin.GetSkinSubfolderFullNameFromSkinName(CSkin.GetSkinName(this.rCurrentlySelectedSong.strSkinPath)), false);
			}

			//Trace.TraceInformation( "Skin変更: " + CSkin.GetSkinName( CDTXMania.Skin.GetCurrentSkinSubfolderFullName(false) ) );
			//Trace.TraceInformation( "Skin変更Current : "+  CDTXMania.Skin.GetCurrentSkinSubfolderFullName(false) );
			//Trace.TraceInformation( "Skin変更System  : "+  CSkin.strSystemSkinSubfolderFullName );
			//Trace.TraceInformation( "Skin変更BoxDef  : "+  CSkin.strBoxDefSkinSubfolderFullName );

			tResetTitleTextureKey();


            if (rCurrentlySelectedSong.list子リスト.Count != 1)
			{
				if (TJAPlayer3.ConfigIni.TJAP3FolderMode)
				{
					this.rCurrentlySelectedSong = this.rCurrentlySelectedSong.list子リスト[0];
					nSelectSongIndex = 0;
					tChangeSong(this.rCurrentlySelectedSong.rParentNode.Openindex);
				}
				else
				{
					//実際には親フォルダを消さないように変更

					this.rCurrentlySelectedSong.bIsOpenFolder = true;

					// Previous index 
					int n回数 = this.rCurrentlySelectedSong.Openindex;
					if (this.rCurrentlySelectedSong.Openindex >= this.rCurrentlySelectedSong.list子リスト.Count())
						n回数 = 0;

					tChangeSong(n回数);
				}

				this.t現在選択中の曲を元に曲バーを再構成する();
				this.t選択曲が変更された(false);
				
				TJAPlayer3.stageSongSelect.tNotifySelectedSongChange();                          // #27648 項目数変更を反映させる
				this.b選択曲が変更された = true;
				// TJAPlayer3.Skin.bgm選曲画面.t停止する();
				CSongSelectSongManager.stopSong();
			}
			return ret;
		}

		public void tReturnToRootBox()
        {
			while (this.rCurrentlySelectedSong.rParentNode != null)
				tCloseBOX();
        }


		public bool tCloseBOX()
		{

			bool ret = false;
			if ( CSkin.GetSkinName( TJAPlayer3.Skin.GetCurrentSkinSubfolderFullName( false ) ) != CSkin.GetSkinName( this.rCurrentlySelectedSong.strSkinPath )
				&& CSkin.bUseBoxDefSkin )
			{
				ret = true;
			}
			// スキン変更が発生しなくても、boxdef圏外に出る場合は、boxdefスキン設定の更新が必要
			// (ユーザーがboxdefスキンをConfig指定している場合への対応のために必要)
			// tBoxに入る()とは処理が微妙に異なるので注意
			TJAPlayer3.Skin.SetCurrentSkinSubfolderFullName(
				( this.rCurrentlySelectedSong.strSkinPath == "" ) ? "" : TJAPlayer3.Skin.GetSkinSubfolderFullNameFromSkinName( CSkin.GetSkinName( this.rCurrentlySelectedSong.strSkinPath ) ), false );

            tResetTitleTextureKey();

            if (TJAPlayer3.ConfigIni.TJAP3FolderMode)
			{
				if (this.rCurrentlySelectedSong.rParentNode  != null)
				{
					this.rCurrentlySelectedSong = this.rCurrentlySelectedSong.rParentNode;
					this.rCurrentlySelectedSong.Openindex = nSelectSongIndex;
					tChangeSong(TJAPlayer3.Songs管理.list曲ルート.IndexOf(this.rCurrentlySelectedSong) - nSelectSongIndex);
				}
			}
			else
			{
				// Reindex the parent node
				List<CSongListNode> currentSongList = flattenList(TJAPlayer3.Songs管理.list曲ルート, true);
                this.rCurrentlySelectedSong.rParentNode.Openindex = currentSongList.IndexOf(this.rCurrentlySelectedSong) - currentSongList.IndexOf(this.rCurrentlySelectedSong.rParentNode.list子リスト[0]);
                this.rCurrentlySelectedSong.rParentNode.bIsOpenFolder = false;
                tChangeSong(-this.rCurrentlySelectedSong.rParentNode.Openindex);


            }

			this.t現在選択中の曲を元に曲バーを再構成する();
			this.t選択曲が変更された(false);                                 // #27648 項目数変更を反映させる
			this.b選択曲が変更された = true;

			return ret;
		}


		public List<CSongListNode> flattenList(List<CSongListNode> list, bool useOpenFlag = false)
        {
			List<CSongListNode> ret = new List<CSongListNode>();

			//foreach (var e in list)
			for(int i = 0; i < list.Count; i++)
            {
				var e = list[i];
				if (!useOpenFlag || !e.bIsOpenFolder) ret.Add(e);

				if (e.eノード種別 == CSongListNode.ENodeType.BOX && 
					(!useOpenFlag || e.bIsOpenFolder))
				{
					ret.AddRange(flattenList(e.list子リスト, useOpenFlag));
				}
            }

			return (ret);
        }

		public void t現在選択中の曲を元に曲バーを再構成する()
		{
			this.tバーの初期化();
		}
		public void t次に移動()
		{
			if( this.rCurrentlySelectedSong != null )
			{
				nNowChange = 1;
				ctScoreFrameAnime.Stop();
				ctScoreFrameAnime.CurrentValue = 0;
				ctBarOpen.Stop();
				ctBarOpen.CurrentValue = 0;
				this.ctScrollCounter = new CCounter(0, 1000, TJAPlayer3.Skin.SongSelect_Scroll_Interval, TJAPlayer3.Timer);


				#region [ パネルを１行上にシフトする。]
				//-----------------

				int barCenterNum = (TJAPlayer3.Skin.SongSelect_Bar_Count - 1) / 2;

				// 選択曲と選択行を１つ下の行に移動。

				tChangeSong(1);
				this.n現在の選択行 = (this.n現在の選択行 + 1) % TJAPlayer3.Skin.SongSelect_Bar_Count;

				// 選択曲から７つ下のパネル（＝新しく最下部に表示されるパネル。消えてしまう一番上のパネルを再利用する）に、新しい曲の情報を記載する。

				var song = this.rGetSideSong(barCenterNum);

				int index = (this.n現在の選択行 + barCenterNum) % TJAPlayer3.Skin.SongSelect_Bar_Count; // 新しく最下部に表示されるパネルのインデックス（0～12）。
				this.stバー情報[index].strタイトル文字列 = song.strタイトル;
				this.stバー情報[index].ForeColor = song.ForeColor;
				this.stバー情報[index].BackColor = song.BackColor;
				this.stバー情報[index].BoxColor = song.BoxColor;
				this.stバー情報[index].BgColor = song.BgColor;

				// Set default if unchanged here
				this.stバー情報[index].BoxType = song.BoxType;
				this.stバー情報[index].BgType = song.BgType;

				this.stバー情報[index].BgTypeChanged = song.isChangedBgType;
				this.stバー情報[index].BoxTypeChanged = song.isChangedBoxType;

				this.stバー情報[index].BoxChara = song.BoxChara;
				this.stバー情報[index].BoxCharaChanged = song.isChangedBoxChara;

				this.stバー情報[index].strジャンル = song.strジャンル;
				this.stバー情報[index].strサブタイトル = song.strサブタイトル;
				this.stバー情報[index].ar難易度 = song.nLevel;
				this.stバー情報[index].nLevelIcon = song.nLevelIcon;

                for (int f = 0; f < (int)Difficulty.Total; f++)
				{
					if (song.arスコア[f] != null)
						this.stバー情報[index].b分岐 = song.arスコア[f].譜面情報.b譜面分岐;
				}

				#region [Reroll cases]

				if (stバー情報[index].nクリア == null)
					this.stバー情報[index].nクリア = new int[2][];
				if (stバー情報[index].nスコアランク == null)
					this.stバー情報[index].nスコアランク = new int[2][];

				for (int i = 0; i < 2; i++)
				{
					this.stバー情報[index].nクリア[i] = new int[5];
					this.stバー情報[index].nスコアランク[i] = new int[5];

					int ap = TJAPlayer3.GetActualPlayer(i);
					var sr = song.arスコア[n現在のアンカ難易度レベルに最も近い難易度レベルを返す(song)];

					this.stバー情報[index].nクリア[i] = sr.GPInfo[ap].nClear;
					this.stバー情報[index].nスコアランク[i] = sr.GPInfo[ap].nScoreRank;
				}

				this.stバー情報[index].csu = song.uniqueId;
				this.stバー情報[index].reference = song;

				#endregion

				// stバー情報[] の内容を1行ずつずらす。


				for (int i = 0; i < TJAPlayer3.Skin.SongSelect_Bar_Count; i++)
				{
					CSongListNode song2 = this.rGetSideSong(i - barCenterNum);
					int n = (((this.n現在の選択行 - barCenterNum) + i) + TJAPlayer3.Skin.SongSelect_Bar_Count) % TJAPlayer3.Skin.SongSelect_Bar_Count;
					this.stバー情報[n].eバー種別 = this.e曲のバー種別を返す(song2);
					this.stバー情報[n].ttkタイトル = this.ttk曲名テクスチャを生成する(this.stバー情報[n].strタイトル文字列, this.stバー情報[n].ForeColor, this.stバー情報[n].BackColor, stバー情報[n].eバー種別 == Eバー種別.Box ? this.pfBoxName : this.pfMusicName);
				}


				// 新しく最下部に表示されるパネル用のスキル値を取得。

				for (int i = 0; i < 3; i++)
					this.stバー情報[index].nスキル値[i] = (int)song.arスコア[this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(song)].譜面情報.最大スキル[i];


				// 1行(100カウント)移動完了。

				this.t選択曲が変更された(false);             // スクロールバー用に今何番目を選択しているかを更新



				{
					TJAPlayer3.stageSongSelect.tNotifySelectedSongChange();      // スクロール完了＝選択曲変更！
					ctBarOpen.Start(0, 260, 2, TJAPlayer3.Timer);

					TJAPlayer3.stageSongSelect.NowGenre = this.rCurrentlySelectedSong.strジャンル;

					TJAPlayer3.stageSongSelect.NowBg = this.rCurrentlySelectedSong.BgType;

					TJAPlayer3.stageSongSelect.NowBgColor = this.rCurrentlySelectedSong.BgColor;

					TJAPlayer3.stageSongSelect.NowUseGenre = !this.rCurrentlySelectedSong.isChangedBgType;

					ctScoreFrameAnime.Start(0, 6000, 1, TJAPlayer3.Timer);
				}

                //-----------------
                #endregion


                tResetTitleTextureKey();
            }
			this.b選択曲が変更された = true;
		}
		public void t前に移動()
		{
			if( this.rCurrentlySelectedSong != null )
			{
				nNowChange = -1;
				ctScoreFrameAnime.Stop();
				ctScoreFrameAnime.CurrentValue = 0;
				ctBarOpen.Stop();
				ctBarOpen.CurrentValue = 0;
				this.ctScrollCounter = new CCounter(0, 1000, TJAPlayer3.Skin.SongSelect_Scroll_Interval, TJAPlayer3.Timer);


				#region [ パネルを１行下にシフトする。]
				//-----------------

				int barCenterNum = (TJAPlayer3.Skin.SongSelect_Bar_Count - 1) / 2;

				// 選択曲と選択行を１つ上の行に移動。

				tChangeSong(-1);
				this.n現在の選択行 = ((this.n現在の選択行 - 1) + TJAPlayer3.Skin.SongSelect_Bar_Count) % TJAPlayer3.Skin.SongSelect_Bar_Count;


				// 選択曲から５つ上のパネル（＝新しく最上部に表示されるパネル。消えてしまう一番下のパネルを再利用する）に、新しい曲の情報を記載する。

				var song = this.rGetSideSong(-barCenterNum);

				int index = ((this.n現在の選択行 - barCenterNum) + TJAPlayer3.Skin.SongSelect_Bar_Count) % TJAPlayer3.Skin.SongSelect_Bar_Count;   // 新しく最上部に表示されるパネルのインデックス（0～12）。
				this.stバー情報[index].strタイトル文字列 = song.strタイトル;
				this.stバー情報[index].ForeColor = song.ForeColor;
				this.stバー情報[index].BackColor = song.BackColor;
				this.stバー情報[index].BoxColor = song.BoxColor;
				this.stバー情報[index].BgColor = song.BgColor;

				// Set default if unchanged here
				this.stバー情報[index].BoxType = song.BoxType;
				this.stバー情報[index].BgType = song.BgType;

				this.stバー情報[index].BgTypeChanged = song.isChangedBgType;
				this.stバー情報[index].BoxTypeChanged = song.isChangedBoxType;

				this.stバー情報[index].BoxChara = song.BoxChara;
				this.stバー情報[index].BoxCharaChanged = song.isChangedBoxChara;

				this.stバー情報[index].strサブタイトル = song.strサブタイトル;
				this.stバー情報[index].strジャンル = song.strジャンル;
				this.stバー情報[index].ar難易度 = song.nLevel;
                this.stバー情報[index].nLevelIcon = song.nLevelIcon;
                for (int f = 0; f < (int)Difficulty.Total; f++)
				{
					if (song.arスコア[f] != null)
						this.stバー情報[index].b分岐 = song.arスコア[f].譜面情報.b譜面分岐;
				}

				/*
				if (stバー情報[index].nクリア == null)
					this.stバー情報[index].nクリア = new int[5];
				if (stバー情報[index].nスコアランク == null)
					this.stバー情報[index].nスコアランク = new int[5];

				for (int i = 0; i <= (int)Difficulty.Edit; i++)
				{
					if (song.arスコア[i] != null)
					{
						this.stバー情報[index].nクリア = song.arスコア[i].譜面情報.nクリア;
						this.stバー情報[index].nスコアランク = song.arスコア[i].譜面情報.nスコアランク;
					}
				}
				*/

				#region [Reroll cases]

				if (stバー情報[index].nクリア == null)
					this.stバー情報[index].nクリア = new int[2][];
				if (stバー情報[index].nスコアランク == null)
					this.stバー情報[index].nスコアランク = new int[2][];

				for (int i = 0; i < 2; i++)
				{
					this.stバー情報[index].nクリア[i] = new int[5];
					this.stバー情報[index].nスコアランク[i] = new int[5];

					int ap = TJAPlayer3.GetActualPlayer(i);
					var sr = song.arスコア[n現在のアンカ難易度レベルに最も近い難易度レベルを返す(song)];

					this.stバー情報[index].nクリア[i] = sr.GPInfo[ap].nClear;
					this.stバー情報[index].nスコアランク[i] = sr.GPInfo[ap].nScoreRank;
				}

				this.stバー情報[index].csu = song.uniqueId;
				this.stバー情報[index].reference = song;

				#endregion

				// stバー情報[] の内容を1行ずつずらす。

				for (int i = 0; i < TJAPlayer3.Skin.SongSelect_Bar_Count; i++)
				{
					int n = (((this.n現在の選択行 - barCenterNum) + i) + TJAPlayer3.Skin.SongSelect_Bar_Count) % TJAPlayer3.Skin.SongSelect_Bar_Count;
					var song2 = this.rGetSideSong(i - barCenterNum);
					this.stバー情報[n].eバー種別 = this.e曲のバー種別を返す(song2);
					this.stバー情報[n].ttkタイトル = this.ttk曲名テクスチャを生成する(this.stバー情報[n].strタイトル文字列, this.stバー情報[n].ForeColor, this.stバー情報[n].BackColor, stバー情報[n].eバー種別 == Eバー種別.Box ? this.pfBoxName : this.pfMusicName);
				}


				// 新しく最上部に表示されるパネル用のスキル値を取得。

				for (int i = 0; i < 3; i++)
					this.stバー情報[index].nスキル値[i] = (int)song.arスコア[this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(song)].譜面情報.最大スキル[i];


				this.t選択曲が変更された(false);             // スクロールバー用に今何番目を選択しているかを更新

				this.ttk選択している曲の曲名 = null;
				this.ttk選択している曲のサブタイトル = null;

				TJAPlayer3.stageSongSelect.tNotifySelectedSongChange();      // スクロール完了＝選択曲変更！
				ctBarOpen.Start(0, 260, 2, TJAPlayer3.Timer);
				TJAPlayer3.stageSongSelect.NowGenre = this.rCurrentlySelectedSong.strジャンル;
				TJAPlayer3.stageSongSelect.NowBg = this.rCurrentlySelectedSong.BgType;
				TJAPlayer3.stageSongSelect.NowBgColor = this.rCurrentlySelectedSong.BgColor;
				TJAPlayer3.stageSongSelect.NowUseGenre = !this.rCurrentlySelectedSong.isChangedBgType;
				ctScoreFrameAnime.Start(0, 6000, 1, TJAPlayer3.Timer);
                //-----------------
                #endregion


                tResetTitleTextureKey();
            }
			this.b選択曲が変更された = true;
		}
		public void tUpdateCurSong()
		{
			if ((this.rGetSideSong(0).eノード種別 == CSongListNode.ENodeType.SCORE) || this.rGetSideSong(0).eノード種別 == CSongListNode.ENodeType.BACKBOX)
			{
				TJAPlayer3.stageSongSelect.bBGMIn再生した = false;

				CSongSelectSongManager.disable();
			}
			else
			{
				CSongSelectSongManager.enable();
				CSongSelectSongManager.playSongIfPossible();
			}

			TJAPlayer3.stageSongSelect.ctBackgroundFade.Start(0, 600, 1, TJAPlayer3.Timer);
			if (this.ctBarOpen.CurrentValue >= 200 || TJAPlayer3.stageSongSelect.ctBackgroundFade.CurrentValue >= 600 - 255)
			{
				TJAPlayer3.stageSongSelect.OldGenre = this.rCurrentlySelectedSong.strジャンル;
				TJAPlayer3.stageSongSelect.OldUseGenre = !this.rCurrentlySelectedSong.isChangedBgType;
				TJAPlayer3.stageSongSelect.OldBg = this.rCurrentlySelectedSong.BgType;
				TJAPlayer3.stageSongSelect.OldBgColor = this.rCurrentlySelectedSong.BgColor;
			}

			if (this.rCurrentlySelectedSong != null)
			{
				ctScoreFrameAnime.Stop();
				ctScoreFrameAnime.CurrentValue = 0;
				ctBarOpen.Stop();
				ctBarOpen.CurrentValue = 0;
			}
			this.b選択曲が変更された = true;
		}
		public void t難易度レベルをひとつ進める()
		{
			if( ( this.rCurrentlySelectedSong == null ) || ( this.rCurrentlySelectedSong.nスコア数 <= 1 ) )
				return;		// 曲にスコアが０～１個しかないなら進める意味なし。
			

			// 難易度レベルを＋１し、現在選曲中のスコアを変更する。

			this.n現在のアンカ難易度レベル = this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す( this.rCurrentlySelectedSong );

			for( int i = 0; i < (int)Difficulty.Total; i++ )
			{
				this.n現在のアンカ難易度レベル = ( this.n現在のアンカ難易度レベル + 1 ) % (int)Difficulty.Total;	// ５以上になったら０に戻る。
				if( this.rCurrentlySelectedSong.arスコア[ this.n現在のアンカ難易度レベル ] != null )	// 曲が存在してるならここで終了。存在してないなら次のレベルへGo。
					break;
			}


            // 曲毎に表示しているスキル値を、新しい難易度レベルに合わせて取得し直す。（表示されている13曲全部。）

            int _center = (TJAPlayer3.Skin.SongSelect_Bar_Count - 1) / 2;
            for ( int i = this.n現在の選択行 - _center; i < ( ( this.n現在の選択行 - _center) + TJAPlayer3.Skin.SongSelect_Bar_Count); i++ )
			{
				var song = this.rGetSideSong(i);
				int index = ( i + TJAPlayer3.Skin.SongSelect_Bar_Count) % TJAPlayer3.Skin.SongSelect_Bar_Count;
				for( int m = 0; m < 3; m++ )
				{
					this.stバー情報[ index ].nスキル値[ m ] = (int) song.arスコア[ this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す( song ) ].譜面情報.最大スキル[ m ];
				}
			}


			// 選曲ステージに変更通知を発出し、関係Activityの対応を行ってもらう。

			TJAPlayer3.stageSongSelect.tNotifySelectedSongChange();
		}
        /// <summary>
        /// 不便だったから作った。
        /// </summary>
		public void t難易度レベルをひとつ戻す()
		{
			if( ( this.rCurrentlySelectedSong == null ) || ( this.rCurrentlySelectedSong.nスコア数 <= 1 ) )
				return;     // 曲にスコアが０～１個しかないなら進める意味なし。

            

            // 難易度レベルを＋１し、現在選曲中のスコアを変更する。

            this.n現在のアンカ難易度レベル = this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す( this.rCurrentlySelectedSong );

            this.n現在のアンカ難易度レベル--;
            if( this.n現在のアンカ難易度レベル < 0 ) // 0より下になったら4に戻す。
            {
                this.n現在のアンカ難易度レベル = 4;
            }

            //2016.08.13 kairera0467 かんたん譜面が無い譜面(ふつう、むずかしいのみ)で、難易度を最上位に戻せない不具合の修正。
            bool bLabel0NotFound = true;
            for( int i = this.n現在のアンカ難易度レベル; i >= 0; i-- )
            {
                if( this.rCurrentlySelectedSong.arスコア[ i ] != null )
                {
                    this.n現在のアンカ難易度レベル = i;
                    bLabel0NotFound = false;
                    break;
                }
            }
            if( bLabel0NotFound )
            {
                for( int i = 4; i >= 0; i-- )
                {
                    if( this.rCurrentlySelectedSong.arスコア[ i ] != null )
                    {
                        this.n現在のアンカ難易度レベル = i;
                        break;
                    }
                }
            }

            // 曲毎に表示しているスキル値を、新しい難易度レベルに合わせて取得し直す。（表示されている13曲全部。）

            int _center = (TJAPlayer3.Skin.SongSelect_Bar_Count - 1) / 2;
            for ( int i = this.n現在の選択行 - _center; i < ( ( this.n現在の選択行 - _center ) + TJAPlayer3.Skin.SongSelect_Bar_Count); i++ )
			{
				CSongListNode song = this.rGetSideSong(i);
				int index = ( i + TJAPlayer3.Skin.SongSelect_Bar_Count) % TJAPlayer3.Skin.SongSelect_Bar_Count;
				for( int m = 0; m < 3; m++ )
				{
					this.stバー情報[ index ].nスキル値[ m ] = (int) song.arスコア[ this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す( song ) ].譜面情報.最大スキル[ m ];
				}
			}


			// 選曲ステージに変更通知を発出し、関係Activityの対応を行ってもらう。

			TJAPlayer3.stageSongSelect.tNotifySelectedSongChange();
		}


		/// <summary>
		/// 曲リストをリセットする
		/// </summary>
		/// <param name="cs"></param>
		public void Refresh(CSongs管理 cs, bool bRemakeSongTitleBar )		// #26070 2012.2.28 yyagi
		{
//			this.On非活性化();

			if ( cs != null && cs.list曲ルート.Count > 0 )	// 新しい曲リストを検索して、1曲以上あった
			{
				TJAPlayer3.Songs管理 = cs;

				if ( this.rCurrentlySelectedSong != null )			// r現在選択中の曲==null とは、「最初songlist.dbが無かった or 検索したが1曲もない」
				{
					this.rCurrentlySelectedSong = searchCurrentBreadcrumbsPosition( TJAPlayer3.Songs管理.list曲ルート, this.rCurrentlySelectedSong.strBreadcrumbs );
					if ( bRemakeSongTitleBar )					// 選曲画面以外に居るときには再構成しない (非活性化しているときに実行すると例外となる)
					{
						this.t現在選択中の曲を元に曲バーを再構成する();
					}
					return;
				}
			}
			if (this.IsActivated)
			{
				this.DeActivate();
				this.rCurrentlySelectedSong = null;
				this.nSelectSongIndex = 0;
				this.Activate();
			}
		}


		/// <summary>
		/// 現在選曲している位置を検索する
		/// (曲一覧クラスを新しいものに入れ替える際に用いる)
		/// </summary>
		/// <param name="ln">検索対象のList</param>
		/// <param name="bc">検索するパンくずリスト(文字列)</param>
		/// <returns></returns>
		private CSongListNode searchCurrentBreadcrumbsPosition( List<CSongListNode> ln, string bc )
		{
			foreach (CSongListNode n in ln)
			{
				if ( n.strBreadcrumbs == bc )
				{
					return n;
				}
				else if ( n.list子リスト != null && n.list子リスト.Count > 0 )	// 子リストが存在するなら、再帰で探す
				{
					CSongListNode r = searchCurrentBreadcrumbsPosition( n.list子リスト, bc );
					if ( r != null ) return r;
				}
			}
			return null;
		}

		/// <summary>
		/// BOXのアイテム数と、今何番目を選択しているかをセットする
		/// </summary>
		public void t選択曲が変更された( bool bForce )	// #27648
		{
			CSongListNode song = TJAPlayer3.stageSongSelect.rNowSelectedSong;
			if ( song == null )
				return;
			if ( song == song_last && bForce == false )
				return;
				
			song_last = song;
			List<CSongListNode> list =TJAPlayer3.Songs管理.list曲ルート;
			int index = list.IndexOf( song ) + 1;
			if ( index <= 0 )
			{
				nCurrentPosition = nNumOfItems = 0;
			}
			else
			{
				nCurrentPosition = index;
				nNumOfItems = list.Count;
			}
            TJAPlayer3.stageSongSelect.act演奏履歴パネル.tSongChange();
		}

		// CActivity 実装

		async public void tLoadPads()
        {
			while (bIsEnumeratingSongs)
            {
				await Task.Delay(100);
			}

			CSongDict.tRefreshScoreTables();

		}

		public override void Activate()
		{
			if( this.IsActivated )
				return;

            if (!bFirstCrownLoad)
            {
				// tLoadPads();
				
				// Calculate Pads asynchonously
				new Task(tLoadPads).Start();
				
				bFirstCrownLoad = true;

			}

			TJAPlayer3.IsPerformingCalibration = false;

			TJAPlayer3.stageSongSelect.actDifficultySelectionScreen.bIsDifficltSelect = false;

            this.pfBoxName = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.SongSelect_BoxName_Scale);
            this.pfMusicName = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.SongSelect_MusicName_Scale);
            this.pfSubtitle = HPrivateFastFont.tInstantiateMainFont(TJAPlayer3.Skin.SongSelect_Subtitle_Scale);
            this.pfMaker = HPrivateFastFont.tInstantiateBoxFont(TJAPlayer3.Skin.SongSelect_Maker_Size);
            this.pfBoxText = HPrivateFastFont.tInstantiateBoxFont(TJAPlayer3.Skin.SongSelect_BoxText_Scale);
            this.pfBPM = HPrivateFastFont.tInstantiateBoxFont(TJAPlayer3.Skin.SongSelect_BPM_Text_Size);


            this.b登場アニメ全部完了 = false;
			this.ctScrollCounter = new CCounter(0, 1000, TJAPlayer3.Skin.SongSelect_Scroll_Interval, TJAPlayer3.Timer);

			// フォント作成。
			// 曲リスト文字は２倍（面積４倍）でテクスチャに描画してから縮小表示するので、フォントサイズは２倍とする。
			
			// 現在選択中の曲がない（＝はじめての活性化）なら、現在選択中の曲をルートの先頭ノードに設定する。

			if( ( this.rCurrentlySelectedSong == null ) && ( TJAPlayer3.Songs管理.list曲ルート.Count > 0 ) )
				this.rCurrentlySelectedSong = TJAPlayer3.Songs管理.list曲ルート[ 0 ];

			this.tバーの初期化();

			this.ctBarOpen = new CCounter();
			this.ctBoxOpen = new CCounter();
			this.ctDifficultyIn = new CCounter();

			this.ct三角矢印アニメ = new CCounter();

			this.ctBarFlash = new CCounter();

			this.ctScoreFrameAnime = new CCounter();

			// strboxText here
			if (this.rCurrentlySelectedSong != null)
			{
				for (int i = 0; i < 3; i++)
				{
					using (var texture = pfBoxText.DrawText(this.rCurrentlySelectedSong.strBoxText[i], rCurrentlySelectedSong.ForeColor, rCurrentlySelectedSong.BackColor, null, 26))
					{
						this.txBoxText[i] = TJAPlayer3.tテクスチャの生成(texture);
						this.strBoxText = this.rCurrentlySelectedSong.strBoxText[0] + this.rCurrentlySelectedSong.strBoxText[1] + this.rCurrentlySelectedSong.strBoxText[2];
					}
				}
			}
            else
            {
				strBoxText = "null";
			}

			for( int i = 0; i < TJAPlayer3.Skin.SongSelect_Bar_Count; i++ )
            {
                this.stバー情報[ i ].ttkタイトル = this.ttk曲名テクスチャを生成する( this.stバー情報[ i ].strタイトル文字列, this.stバー情報[i].ForeColor, this.stバー情報[i].BackColor, stバー情報[i].eバー種別 == Eバー種別.Box ? this.pfBoxName : this.pfMusicName);
            }

			base.Activate();

			this.t選択曲が変更された(true);		// #27648 2012.3.31 yyagi 選曲画面に入った直後の 現在位置/全アイテム数 の表示を正しく行うため
		}
		public override void DeActivate()
		{
			if( this.IsDeActivated )
				return;

		    TJAPlayer3.tDisposeSafely(ref pfBoxName);
		    TJAPlayer3.tDisposeSafely(ref pfMusicName);
		    TJAPlayer3.tDisposeSafely(ref pfSubtitle);
			TJAPlayer3.tDisposeSafely(ref pfMaker);
            TJAPlayer3.tDisposeSafely(ref pfBPM);

			tResetTitleKey();
		    ClearTitleTextureCache();

			this.ct三角矢印アニメ = null;

			base.DeActivate();
		}
		public override void CreateManagedResource()
		{
			this.ft曲リスト用フォント = HPrivateFastFont.tInstantiateMainFont(40);

			int c = ( CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ja" ) ? 0 : 1;

			#region [ Songs not found画像 ]
			try
			{
				this.txSongNotFound = new CTexture(ft曲リスト用フォント.DrawText("Songs not found.\nYou need to install songs.", Color.White));
				this.txSongNotFound.vcScaleRatio = new Vector3D<float>( 0.5f, 0.5f, 1f );	// 半分のサイズで表示する。

				/*
				using( Bitmap image = new Bitmap( 640, 128 ) )
				using( Graphics graphics = Graphics.FromImage( image ) )
				{
					string[] s1 = { "曲データが見つかりません。", "Songs not found." };
					string[] s2 = { "曲データをDTXManiaGR.exe以下の", "You need to install songs." };
					string[] s3 = { "フォルダにインストールして下さい。", "" };
					graphics.DrawString( s1[c], this.ft曲リスト用フォント, Brushes.DarkGray, (float) 2f, (float) 2f );
					graphics.DrawString( s1[c], this.ft曲リスト用フォント, Brushes.White, (float) 0f, (float) 0f );
					graphics.DrawString( s2[c], this.ft曲リスト用フォント, Brushes.DarkGray, (float) 2f, (float) 44f );
					graphics.DrawString( s2[c], this.ft曲リスト用フォント, Brushes.White, (float) 0f, (float) 42f );
					graphics.DrawString( s3[c], this.ft曲リスト用フォント, Brushes.DarkGray, (float) 2f, (float) 86f );
					graphics.DrawString( s3[c], this.ft曲リスト用フォント, Brushes.White, (float) 0f, (float) 84f );

					this.txSongNotFound = new CTexture( image );

				}
				*/
			}
			catch( CTextureCreateFailedException e )
			{
				Trace.TraceError( e.ToString() );
				Trace.TraceError( "SoungNotFoundテクスチャの作成に失敗しました。" );
				this.txSongNotFound = null;
			}
			#endregion
			#region [ "曲データを検索しています"画像 ]
			try
			{
				this.txEnumeratingSongs = new CTexture(ft曲リスト用フォント.DrawText("Now enumerating songs.\nPlease wait...", Color.White));
				this.txEnumeratingSongs.vcScaleRatio = new Vector3D<float>( 0.5f, 0.5f, 1f );	// 半分のサイズで表示する。

				/*
				using ( Bitmap image = new Bitmap( 640, 96 ) )
				using ( Graphics graphics = Graphics.FromImage( image ) )
				{
					string[] s1 = { "曲データを検索しています。", "Now enumerating songs." };
					string[] s2 = { "そのまましばらくお待ち下さい。", "Please wait..." };
					graphics.DrawString( s1[c], this.ft曲リスト用フォント, Brushes.DarkGray, (float) 2f, (float) 2f );
					graphics.DrawString( s1[c], this.ft曲リスト用フォント, Brushes.White, (float) 0f, (float) 0f );
					graphics.DrawString( s2[c], this.ft曲リスト用フォント, Brushes.DarkGray, (float) 2f, (float) 44f );
					graphics.DrawString( s2[c], this.ft曲リスト用フォント, Brushes.White, (float) 0f, (float) 42f );

					this.txEnumeratingSongs = new CTexture( image );

					this.txEnumeratingSongs.vc拡大縮小倍率 = new Vector3D<float>( 0.5f, 0.5f, 1f );	// 半分のサイズで表示する。
				}
				*/
			}
			catch ( CTextureCreateFailedException e )
			{
				Trace.TraceError( e.ToString() );
				Trace.TraceError( "txEnumeratingSongsテクスチャの作成に失敗しました。" );
				this.txEnumeratingSongs = null;
			}
			#endregion
			#region [ 曲数表示 ]
			//this.txアイテム数数字 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\ScreenSelect skill number on gauge etc.png" ), false );
			#endregion

			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource()
		{
            TJAPlayer3.tDisposeSafely( ref this.ft曲リスト用フォント );

			for( int i = 0; i < TJAPlayer3.Skin.SongSelect_Bar_Count; i++ )
            {
                TJAPlayer3.tテクスチャの解放( ref this.stバー情報[ i ].txタイトル名 );
                this.stバー情報[ i ].ttkタイトル = null;
            }

            TJAPlayer3.tテクスチャの解放( ref this.txEnumeratingSongs );
            TJAPlayer3.tテクスチャの解放( ref this.txSongNotFound );

			base.ReleaseManagedResource();
		}
		public override int Draw()
		{
			if (this.IsDeActivated)
				return 0;

			#region [ 初めての進行描画 ]
			//-----------------
			if (this.IsFirstDraw)
			{
				TJAPlayer3.stageSongSelect.tNotifySelectedSongChange();

				ctBarOpen.Start(0, 260, 2, TJAPlayer3.Timer);
				this.ct三角矢印アニメ.Start(0, 1000, 1, TJAPlayer3.Timer);
				base.IsFirstDraw = false;
			}
			//-----------------
			#endregion

			ctBarFlash.Tick();
			ctBoxOpen.Tick();
			ctBarOpen.Tick();
			ctDifficultyIn.Tick();

			float BarAnimeCount = this.ctBarOpen.CurrentValue <= 200 ? 0 : (float)Math.Sin(((this.ctBarOpen.CurrentValue - 200) * 1.5f) * (Math.PI / 180));
			int centerMove = (int)(BarAnimeCount * TJAPlayer3.Skin.SongSelect_Bar_Center_Move);
			int centerMoveX = (int)(BarAnimeCount * TJAPlayer3.Skin.SongSelect_Bar_Center_Move_X);

			if (BarAnimeCount == 1.0)
				ctScoreFrameAnime.TickLoop();

			// まだ選択中の曲が決まってなければ、曲ツリールートの最初の曲にセットする。

			if ((this.rCurrentlySelectedSong == null) && (TJAPlayer3.Songs管理.list曲ルート.Count > 0))
            {
				nSelectSongIndex = 0;
				this.rCurrentlySelectedSong = TJAPlayer3.Songs管理.list曲ルート[nSelectSongIndex];
			}

			// 描画。
			if (this.rCurrentlySelectedSong == null)
			{
				#region [ 曲が１つもないなら「Songs not found.」を表示してここで帰れ。]
				//-----------------
				if (bIsEnumeratingSongs)
				{
					if (this.txEnumeratingSongs != null)
					{
						this.txEnumeratingSongs.t2D描画(320, 160);
					}
				}
				else
				{
					if (this.txSongNotFound != null)
						this.txSongNotFound.t2D描画(320, 160);
				}
				//-----------------
				#endregion

				return 0;
			}

			// 本ステージは、(1)登場アニメフェーズ → (2)通常フェーズ　と二段階にわけて進む。

			if (strBoxText != rCurrentlySelectedSong.strBoxText[0] + rCurrentlySelectedSong.strBoxText[1] + rCurrentlySelectedSong.strBoxText[2])
			{
				for (int i = 0; i < 3; i++)
				{
					using (var texture = pfBoxText.DrawText(this.rCurrentlySelectedSong.strBoxText[i], rCurrentlySelectedSong.ForeColor, rCurrentlySelectedSong.BackColor, null, 26))
					{
						this.txBoxText[i] = TJAPlayer3.tテクスチャの生成(texture);
						this.strBoxText = this.rCurrentlySelectedSong.strBoxText[0] + this.rCurrentlySelectedSong.strBoxText[1] + this.rCurrentlySelectedSong.strBoxText[2];
					}
				}
			}

			this.ctScrollCounter.Tick();

			// 進行。
			if (this.ctScrollCounter.CurrentValue == this.ctScrollCounter.EndValue) ct三角矢印アニメ.TickLoop();
			else ct三角矢印アニメ.CurrentValue = 0;


			int i選曲バーX座標 = 673; //選曲バーの座標用
			int i選択曲バーX座標 = 665; //選択曲バーの座標用

			int barCenterNum = (TJAPlayer3.Skin.SongSelect_Bar_Count - 1) / 2;

			if (this.ctScrollCounter.CurrentValue == this.ctScrollCounter.EndValue)
            {
				nNowChange = 0;
			}

			#region [ (2) 通常フェーズの描画。]
			//-----------------
			for (int i = 0; i < TJAPlayer3.Skin.SongSelect_Bar_Count; i++) // パネルは全13枚。
			{
				int index = i + nNowChange;
				if (((index < 0 || index >= TJAPlayer3.Skin.SongSelect_Bar_Count) && this.ctScrollCounter.CurrentValue != this.ctScrollCounter.EndValue))
					continue;

				int nパネル番号 = (((this.n現在の選択行 - barCenterNum) + i) + TJAPlayer3.Skin.SongSelect_Bar_Count) % TJAPlayer3.Skin.SongSelect_Bar_Count;
				int n見た目の行番号 = i;
				int n次のパネル番号 = (index % TJAPlayer3.Skin.SongSelect_Bar_Count);
				int x = i選曲バーX座標;

				#region [Positions and layouts]

				int xZahyou = TJAPlayer3.Skin.SongSelect_Bar_X[n見た目の行番号];
				int xNextZahyou = TJAPlayer3.Skin.SongSelect_Bar_X[n次のパネル番号];
				int xCentralZahyou = TJAPlayer3.Skin.SongSelect_Bar_X[barCenterNum];

				int yZahyou = TJAPlayer3.Skin.SongSelect_Bar_Y[n見た目の行番号];
				int yNextZahyou = TJAPlayer3.Skin.SongSelect_Bar_Y[n次のパネル番号];
				int yCentralZahyou = TJAPlayer3.Skin.SongSelect_Bar_Y[barCenterNum];

				int Diff4 = Math.Abs(n見た目の行番号 - barCenterNum);
				int NextDiff4 = Math.Abs(n次のパネル番号 - barCenterNum);

				/*
				eLayoutType eLayout = (eLayoutType)TJAPlayer3.ConfigIni.nLayoutType;

				switch (eLayout)
                {
					case eLayoutType.DiagonalDownUp:
						xZahyou = xCentralZahyou - 2 * (xZahyou - xCentralZahyou);
						xNextZahyou = xCentralZahyou - 2 * (xNextZahyou - xCentralZahyou);
						break;
					case eLayoutType.Vertical:
						xZahyou = xCentralZahyou;
						xNextZahyou = xCentralZahyou;
						break;
					case eLayoutType.HalfCircleRight:
						xZahyou = xCentralZahyou - 25 * (int)Math.Pow(Diff4, 2);
						xNextZahyou = xCentralZahyou - 25 * (int)Math.Pow(NextDiff4, 2);
						break;
					case eLayoutType.HalfCircleLeft:
						xZahyou = xCentralZahyou + 25 * (int)Math.Pow(Diff4, 2);
						xNextZahyou = xCentralZahyou + 25 * (int)Math.Pow(NextDiff4, 2);
						break;
					default:
						break;
				}
				*/

				#endregion
				
				// x set here
				int xAnime = xZahyou + (int)((xNextZahyou - xZahyou) 
					* (1.0 - fNowScrollAnime));
				
				int y = yZahyou + (int)((yNextZahyou - yZahyou)
					* (1.0 - fNowScrollAnime));

				// (B) スクロール中の選択曲バー、またはその他のバーの描画。

				float Box = 0;
				float Box_X = 0;
				float Box_Y = 0;
                int _center = (TJAPlayer3.Skin.SongSelect_Bar_Count - 1) / 2;
				float[] _boxAim = new float[2]
				{
                    TJAPlayer3.Skin.SongSelect_Bar_Anim_X[i],
                    TJAPlayer3.Skin.SongSelect_Bar_Anim_Y[i]
                };

				int _movfs = i - 1;
				int _maxfs = _center - 1;
				int _gap = Math.Max(1, TJAPlayer3.Skin.SongSelect_Bar_Count - 3);

                #region [ BoxOpenAnime ]

                if (i != _center && i != 0 && i < TJAPlayer3.Skin.SongSelect_Bar_Count - 1)
				{
                    if (ctBoxOpen.CurrentValue >= 1000 && ctBoxOpen.CurrentValue <= 1560)
                    {
						float _curve = 1000 + (150 / _gap) * (_maxfs - _movfs);//Math.Abs(150 / (_center - i));
                        Box_X = _boxAim[0] - (float)Math.Sin(((ctBoxOpen.CurrentValue - _curve) / 4 + 90) * (Math.PI / 180)) * _boxAim[0];
						Box_Y = _boxAim[1] - (float)Math.Sin(((ctBoxOpen.CurrentValue - _curve) / 4 + 90) * (Math.PI / 180)) * _boxAim[1];
                    }
                    if (ctBoxOpen.CurrentValue > 1300 && ctBoxOpen.CurrentValue < 1940)
                    {
                        ctBoxOpen.ChangeInterval(0.65);
						Box_X = _boxAim[0];
						Box_Y = _boxAim[1];
                    }

                    if (ctBoxOpen.CurrentValue >= 1840 && ctBoxOpen.CurrentValue <= 2300)
                    {
                        ctBoxOpen.ChangeInterval(1.3);
						float _curve = 1940 - (100 / _gap) * (_maxfs - _movfs);// Math.Abs(100 / (_center - i));
                        Box_X = _boxAim[0] - (float)Math.Sin(((ctBoxOpen.CurrentValue - _curve) / 4) * (Math.PI / 180)) * _boxAim[0];
                        Box_Y = _boxAim[1] - (float)Math.Sin(((ctBoxOpen.CurrentValue - _curve) / 4) * (Math.PI / 180)) * _boxAim[1];
                    }

                }

				#region [old]

				/*
                if (ctBoxOpen.n現在の値 <= 560 + 1000)
				{
					if (i == 1)
					{
						if (ctBoxOpen.n現在の値 >= 1000 && ctBoxOpen.n現在の値 <= 360 + 1000)
							Box = 400.0f - (float)Math.Sin(((ctBoxOpen.n現在の値 - 1000) / 4 + 90) * (Math.PI / 180)) * 400.0f;
						if (ctBoxOpen.n現在の値 >= 360 + 1000)
							Box = 400.0f;
					}
					if (i == 2)
					{
						if (ctBoxOpen.n現在の値 >= 75 + 1000 && ctBoxOpen.n現在の値 <= 435 + 1000)
							Box = 500.0f - (float)Math.Sin(((ctBoxOpen.n現在の値 - 1075) / 4 + 90) * (Math.PI / 180)) * 500.0f;
						if (ctBoxOpen.n現在の値 >= 435 + 1000)
							Box = 500.0f;
					}
					if (i == 3)
					{
						if (ctBoxOpen.n現在の値 >= 150 + 1000 && ctBoxOpen.n現在の値 <= 510 + 1000)
							Box = 600.0f - (float)Math.Sin(((ctBoxOpen.n現在の値 - 1150) / 4 + 90) * (Math.PI / 180)) * 600.0f;
						if (ctBoxOpen.n現在の値 >= 510 + 1000)
							Box = 600.0f;
					}
					if (i == 5)
					{
						if (ctBoxOpen.n現在の値 >= 150 + 1000 && ctBoxOpen.n現在の値 <= 510 + 1000)
							Box = -600.0f + (float)Math.Sin(((ctBoxOpen.n現在の値 - 1150) / 4 + 90) * (Math.PI / 180)) * 600.0f;
						if (ctBoxOpen.n現在の値 >= 510 + 1000)
							Box = 600.0f;
					}
					if (i == 6)
					{
						if (ctBoxOpen.n現在の値 >= 75 + 1000 && ctBoxOpen.n現在の値 <= 435 + 1000)
							Box = -500.0f + (float)Math.Sin(((ctBoxOpen.n現在の値 - 1075) / 4 + 90) * (Math.PI / 180)) * 500.0f;
						if (ctBoxOpen.n現在の値 >= 435 + 1000)
							Box = 500.0f;
					}
					if (i == 7)
					{
						if (ctBoxOpen.n現在の値 >= 1000 && ctBoxOpen.n現在の値 <= 360 + 1000)
							Box = -400.0f + (float)Math.Sin(((ctBoxOpen.n現在の値 - 1000) / 4 + 90) * (Math.PI / 180)) * 400.0f;
						if (ctBoxOpen.n現在の値 >= 360 + 1000)
							Box = 400.0f;
					}
				}

				if (ctBoxOpen.n現在の値 > 1300 && ctBoxOpen.n現在の値 < 1940)
				{
					ctBoxOpen.t間隔値変更(0.65);
					if (i == 1)
						Box = 600.0f;
					if (i == 2)
						Box = 600.0f;
					if (i == 3)
						Box = 600.0f;
					if (i == 5)
						Box = -600.0f;
					if (i == 6)
						Box = -600.0f;
					if (i == 7)
						Box = -600.0f;
				}

				if (ctBoxOpen.n現在の値 >= 1840 && ctBoxOpen.n現在の値 <= 560 + 1840)
				{
					ctBoxOpen.t間隔値変更(1.3);
					if (i == 1)
					{
						if (ctBoxOpen.n現在の値 >= 100 + 1840 && ctBoxOpen.n現在の値 <= 460 + 1840)
							Box = 600.0f - (float)Math.Sin(((ctBoxOpen.n現在の値 - 1940) / 4) * (Math.PI / 180)) * 600.0f;
						if (ctBoxOpen.n現在の値 < 100 + 1840)
							Box = 600.0f;
					}
					if (i == 2)
					{
						if (ctBoxOpen.n現在の値 >= 50 + 1840 && ctBoxOpen.n現在の値 <= 410 + 1840)
							Box = 500.0f - (float)Math.Sin(((ctBoxOpen.n現在の値 - 1890) / 4) * (Math.PI / 180)) * 500.0f;
						if (ctBoxOpen.n現在の値 < 50 + 1840)
							Box = 600.0f;
					}
					if (i == 3)
					{
						if (ctBoxOpen.n現在の値 >= 1840 && ctBoxOpen.n現在の値 <= 360 + 1840)
							Box = 400.0f - (float)Math.Sin(((ctBoxOpen.n現在の値 - 1840) / 4) * (Math.PI / 180)) * 400.0f;
						if (ctBoxOpen.n現在の値 < 1840)
							Box = 600.0f;
					}
					if (i == 5)
					{
						if (ctBoxOpen.n現在の値 >= 1840 && ctBoxOpen.n現在の値 <= 360 + 1840)
							Box = -400.0f + (float)Math.Sin(((ctBoxOpen.n現在の値 - 1840) / 4) * (Math.PI / 180)) * 400.0f;
						if (ctBoxOpen.n現在の値 < 1840)
							Box = -600.0f;
					}
					if (i == 6)
					{
						if (ctBoxOpen.n現在の値 >= 50 + 1840 && ctBoxOpen.n現在の値 <= 410 + 1840)
							Box = -500.0f + (float)Math.Sin(((ctBoxOpen.n現在の値 - 1890) / 4) * (Math.PI / 180)) * 500.0f;
						if (ctBoxOpen.n現在の値 < 50 + 1840)
							Box = -600.0f;
					}
					if (i == 7)
					{
						if (ctBoxOpen.n現在の値 >= 100 + 1840 && ctBoxOpen.n現在の値 <= 460 + 1840)
							Box = -600.0f + (float)Math.Sin(((ctBoxOpen.n現在の値 - 1940) / 4) * (Math.PI / 180)) * 600.0f;
						if (ctBoxOpen.n現在の値 < 100 + 1840)
							Box = -600.0f;
					}
				}
				*/

				#endregion

				#endregion


				#region [ バーテクスチャを描画。]
				//-----------------

				// int boxType = nStrジャンルtoNum(stバー情報[nパネル番号].strジャンル);
				string boxType = stバー情報[nパネル番号].BoxType;
				var bar_genre = HGenreBar.tGetGenreBar(boxType, TJAPlayer3.Tx.SongSelect_Bar_Genre);
				var bar_genre_overlap = HGenreBar.tGetGenreBar(boxType, TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlap);

				bar_genre.color4 = CConversion.ColorToColor4(stバー情報[nパネル番号].BoxColor);

				bar_genre.vcScaleRatio.X = 1.0f;
				if (bar_genre_overlap != null)
                    bar_genre_overlap.vcScaleRatio.X = 1.0f;

                TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay.vcScaleRatio.X = 1.0f;
				TJAPlayer3.Tx.SongSelect_Bar_Genre_Back.vcScaleRatio.X = 1.0f;

				


				if (ctScrollCounter.CurrentValue != ctScrollCounter.EndValue || n見た目の行番号 != barCenterNum)
                {
					int songType = 0;
					if (this.stバー情報[nパネル番号].ar難易度[(int)Difficulty.Dan] >= 0)
						songType = 1;
					else if (this.stバー情報[nパネル番号].ar難易度[(int)Difficulty.Tower] >= 0)
						songType = 2;

					// TJAPlayer3.act文字コンソール.tPrint(x + -470, y + 30, C文字コンソール.Eフォント種別.白, (this.stバー情報[nパネル番号].ar難易度[(int)Difficulty.Tower]).ToString());


					// Very disgusting, to improve and unbloat asap
					this.tジャンル別選択されていない曲バーの描画(
						xAnime - (int)Box_X,//Box, 
						y - ((int)Box_Y),//Box * 3), 
						this.stバー情報[nパネル番号].strジャンル, 
						stバー情報[nパネル番号].eバー種別, 
						stバー情報[nパネル番号].nクリア, 
						stバー情報[nパネル番号].nスコアランク, 
						boxType, 
						songType,
						stバー情報[nパネル番号].csu,
                        stバー情報[nパネル番号].reference);
				}

				/*
				 
				else if (n見た目の行番号 != 4)
					this.tジャンル別選択されていない曲バーの描画(xAnime - (int)Box, y - ((int)Box * 3), this.stバー情報[nパネル番号].strジャンル, stバー情報[nパネル番号].eバー種別, stバー情報[nパネル番号].nクリア, stバー情報[nパネル番号].nスコアランク, boxType);
				
				*/

				//-----------------
				#endregion

				#region [ タイトル名テクスチャを描画。]
				if (ctDifficultyIn.CurrentValue >= 1000 && TJAPlayer3.stageSongSelect.actDifficultySelectionScreen.bIsDifficltSelect)
					ResolveTitleTexture(this.stバー情報[nパネル番号].ttkタイトル, TJAPlayer3.Skin.SongSelect_VerticalText).Opacity = (int)255.0f - (ctDifficultyIn.CurrentValue - 1000);
				else
					ResolveTitleTexture(this.stバー情報[nパネル番号].ttkタイトル, TJAPlayer3.Skin.SongSelect_VerticalText).Opacity = 255;

				if (ctScrollCounter.CurrentValue != ctScrollCounter.EndValue)
					ResolveTitleTexture(this.stバー情報[nパネル番号].ttkタイトル, TJAPlayer3.Skin.SongSelect_VerticalText).t2D拡大率考慮中央基準描画(
						xAnime - Box_X + GetTitleOffsetX(this.stバー情報[nパネル番号].eバー種別), y - Box_Y + GetTitleOffsetY(this.stバー情報[nパネル番号].eバー種別));
				else if (n見た目の行番号 != barCenterNum)
					ResolveTitleTexture(this.stバー情報[nパネル番号].ttkタイトル, TJAPlayer3.Skin.SongSelect_VerticalText).t2D拡大率考慮中央基準描画(
						xAnime - Box_X + GetTitleOffsetX(this.stバー情報[nパネル番号].eバー種別), y - Box_Y + GetTitleOffsetY(this.stバー情報[nパネル番号].eバー種別));
				#endregion

				//-----------------					
			}
			#endregion

			
			if (TJAPlayer3.Skin.SongSelect_Bar_Select_Skip_Fade ||
				this.ctScrollCounter.CurrentValue == this.ctScrollCounter.EndValue)
			{
                #region [ Bar_Select ]

                int barSelect_width = TJAPlayer3.Tx.SongSelect_Bar_Select.sz画像サイズ.Width;
                int barSelect_height = TJAPlayer3.Tx.SongSelect_Bar_Select.sz画像サイズ.Height / 2;

                if (ctBarFlash.IsEnded && !TJAPlayer3.stageSongSelect.actDifficultySelectionScreen.bIsDifficltSelect)
                {
                    TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = (int)(BarAnimeCount * 255.0f);
                    if (TJAPlayer3.Skin.SongSelect_Bar_Select_Skip_Fade)
                        TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = 255;

                }
                else
                    TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = (int)(255 - (ctBarFlash.CurrentValue - 700) * 2.55f);

                TJAPlayer3.Tx.SongSelect_Bar_Select.t2D描画(TJAPlayer3.Skin.SongSelect_Bar_Select[0], TJAPlayer3.Skin.SongSelect_Bar_Select[1], new Rectangle(0, 0, barSelect_width, barSelect_height));

                #region [ BarFlash ]

                if (ctBarFlash.CurrentValue <= 100)
                    TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = (int)(ctBarFlash.CurrentValue * 2.55f);
                else if (ctBarFlash.CurrentValue <= 200)
                    TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = (int)(255 - (ctBarFlash.CurrentValue - 100) * 2.55f);
                else if (ctBarFlash.CurrentValue <= 300)
                    TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = (int)((ctBarFlash.CurrentValue - 200) * 2.55f);
                else if (ctBarFlash.CurrentValue <= 400)
                    TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = (int)(255 - (ctBarFlash.CurrentValue - 300) * 2.55f);
                else if (ctBarFlash.CurrentValue <= 500)
                    TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = (int)((ctBarFlash.CurrentValue - 400) * 2.55f);
                else if (ctBarFlash.CurrentValue <= 600)
                    TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = (int)(255 - (ctBarFlash.CurrentValue - 500) * 2.55f);
                else if (ctBarFlash.CurrentValue <= 700)
                    TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = (int)((ctBarFlash.CurrentValue - 600) * 2.55f);
                else if (ctBarFlash.CurrentValue <= 800)
                    TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = (int)(255 - (ctBarFlash.CurrentValue - 700) * 2.55f);
                else
                    TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = 0;

                TJAPlayer3.Tx.SongSelect_Bar_Select.t2D描画(TJAPlayer3.Skin.SongSelect_Bar_Select[0], TJAPlayer3.Skin.SongSelect_Bar_Select[1], new Rectangle(0, barSelect_height, barSelect_width, barSelect_height));

                #endregion

                #endregion
            }


            if (this.ctScrollCounter.CurrentValue == this.ctScrollCounter.EndValue)
			{
                #region [ Draw BarCenter ]

                

                if (rCurrentlySelectedSong.eノード種別 == CSongListNode.ENodeType.SCORE)
                {
					#region [ Score ]

					#region [ Bar ]

					//int boxType = nStrジャンルtoNum(r現在選択中の曲.strジャンル);
					var bar_genre = HGenreBar.tGetGenreBar(rCurrentlySelectedSong.BoxType, TJAPlayer3.Tx.SongSelect_Bar_Genre);
					var bar_genre_overlap = HGenreBar.tGetGenreBar(rCurrentlySelectedSong.BoxType, TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlap);

					DrawBarCenter(bar_genre, TJAPlayer3.Skin.SongSelect_Bar_X[barCenterNum], TJAPlayer3.Skin.SongSelect_Bar_Y[barCenterNum], centerMoveX, centerMove, true, false, false);
                    DrawBarCenter(bar_genre_overlap, TJAPlayer3.Skin.SongSelect_Bar_X[barCenterNum], TJAPlayer3.Skin.SongSelect_Bar_Y[barCenterNum], centerMoveX, centerMove, false, true, true);

                    #endregion

                    #region [ Crown and ScoreRank ]

                    // Mark

                    if (this.rCurrentlySelectedSong.arスコア[(int)Difficulty.Dan] != null)
                    {
						//int[] clear = this.r現在選択中の曲.arスコア[(int)Difficulty.Dan].譜面情報.nクリア;

						for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
						{
							if (i >= 2) continue;

							int ap = TJAPlayer3.GetActualPlayer(i);

							int[] clear = this.rCurrentlySelectedSong.arスコア[(int)Difficulty.Dan].GPInfo[ap].nClear;

							int currentRank = Math.Min(clear[0], 6) - 1;

							int x = TJAPlayer3.Skin.SongSelect_Bar_X[barCenterNum] + TJAPlayer3.Skin.SongSelect_DanStatus_Offset_X[i];
							int y = TJAPlayer3.Skin.SongSelect_Bar_Y[barCenterNum] + TJAPlayer3.Skin.SongSelect_DanStatus_Offset_Y[i];

							displayDanStatus((int)(x - centerMoveX / 1.1f), (int)(y - centerMove / 1.1f), currentRank, 0.2f);
						}

					}
					else if (this.rCurrentlySelectedSong.arスコア[(int)Difficulty.Tower] != null)
					{
						//int[] clear = this.r現在選択中の曲.arスコア[(int)Difficulty.Tower].譜面情報.nクリア;

						for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
						{
							if (i >= 2) continue;

							int ap = TJAPlayer3.GetActualPlayer(i);

							int[] clear = this.rCurrentlySelectedSong.arスコア[(int)Difficulty.Tower].GPInfo[ap].nClear;

							int currentRank = Math.Min(clear[0], 7) - 1;

							int x = TJAPlayer3.Skin.SongSelect_Bar_X[barCenterNum] + TJAPlayer3.Skin.SongSelect_TowerStatus_Offset_X[i];
							int y = TJAPlayer3.Skin.SongSelect_Bar_Y[barCenterNum] + TJAPlayer3.Skin.SongSelect_TowerStatus_Offset_Y[i];

							displayTowerStatus((int)(x - centerMoveX / 1.1f), (int)(y - centerMove / 1.1f), currentRank, 0.3f);
						}
					}
					else if (this.rCurrentlySelectedSong.arスコア[3] != null || this.rCurrentlySelectedSong.arスコア[4] != null)
					{
						var sr = this.rCurrentlySelectedSong.arスコア[n現在のアンカ難易度レベルに最も近い難易度レベルを返す(this.rCurrentlySelectedSong)];

						for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
						{
							if (i >= 2) continue;

							int ap = TJAPlayer3.GetActualPlayer(i);

							int[] クリア = sr.GPInfo[ap].nClear;

							int[] スコアランク = sr.GPInfo[ap].nScoreRank;

							int x = TJAPlayer3.Skin.SongSelect_Bar_X[barCenterNum] + TJAPlayer3.Skin.SongSelect_RegularCrowns_Offset_X[i];
							int y = TJAPlayer3.Skin.SongSelect_Bar_Y[barCenterNum] + TJAPlayer3.Skin.SongSelect_RegularCrowns_Offset_Y[i];

							displayRegularCrowns((int)(x - centerMoveX / 1.1f), (int)(y - centerMove / 1.1f), クリア, スコアランク, 0.8f + BarAnimeCount / 620f);
							
						}
					}


					#endregion

					#region [Favorite status]

					displayFavoriteStatus(
						(int)(TJAPlayer3.Skin.SongSelect_Bar_X[barCenterNum] + TJAPlayer3.Skin.SongSelect_FavoriteStatus_Offset[0] - centerMoveX / 1.1f),
						(int)(TJAPlayer3.Skin.SongSelect_Bar_Y[barCenterNum] + TJAPlayer3.Skin.SongSelect_FavoriteStatus_Offset[1] - centerMove / 1.1f), this.rCurrentlySelectedSong.uniqueId, 1f + BarAnimeCount / 620f);

                    #endregion

                    #region [Level number big]

                    tPrintLevelNumberBig(
						(int)(TJAPlayer3.Skin.SongSelect_Bar_X[barCenterNum] + TJAPlayer3.Skin.SongSelect_Level_Offset[0] - centerMoveX / 1.1f),
						(int)(TJAPlayer3.Skin.SongSelect_Bar_Y[barCenterNum] + TJAPlayer3.Skin.SongSelect_Level_Offset[1] - centerMove / 1.1f),
                        this.rCurrentlySelectedSong
                        );

					#endregion

					#endregion
				}
                if (rCurrentlySelectedSong.eノード種別 == CSongListNode.ENodeType.BOX)
                {
					#region [ Box ]

					//int boxType = nStrジャンルtoNum(r現在選択中の曲.strジャンル);
					var bar_genre = HGenreBar.tGetGenreBar(rCurrentlySelectedSong.BoxType, TJAPlayer3.Tx.SongSelect_Bar_Genre);
					var bar_genre_overlap = HGenreBar.tGetGenreBar(rCurrentlySelectedSong.BoxType, TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlap);

					//DrawBarCenter(TJAPlayer3.Tx.SongSelect_Bar_Genre[boxType], TJAPlayer3.Skin.SongSelect_Bar_X[barCenterNum], TJAPlayer3.Skin.SongSelect_Bar_Y[barCenterNum], centerMoveX, centerMove, true, true, false);

                    DrawBarCenter(bar_genre, TJAPlayer3.Skin.SongSelect_Bar_X[barCenterNum], TJAPlayer3.Skin.SongSelect_Bar_Y[barCenterNum], centerMoveX, centerMove, true, false, false);
                    DrawBarCenter(bar_genre_overlap, TJAPlayer3.Skin.SongSelect_Bar_X[barCenterNum], TJAPlayer3.Skin.SongSelect_Bar_Y[barCenterNum], centerMoveX, centerMove, false, true, false);


                    #endregion
                }
                if (rCurrentlySelectedSong.eノード種別 == CSongListNode.ENodeType.BACKBOX)
                {
					#region [ BackBox ]

					DrawBarCenter(TJAPlayer3.Tx.SongSelect_Bar_Genre_Back, TJAPlayer3.Skin.SongSelect_Bar_X[barCenterNum], TJAPlayer3.Skin.SongSelect_Bar_Y[barCenterNum], centerMoveX, centerMove, false, false, false);
					
					#endregion
				}
				if (rCurrentlySelectedSong.eノード種別 == CSongListNode.ENodeType.RANDOM)
                {
					#region [Random box]

					DrawBarCenter(TJAPlayer3.Tx.SongSelect_Bar_Genre_Random, TJAPlayer3.Skin.SongSelect_Bar_X[barCenterNum], TJAPlayer3.Skin.SongSelect_Bar_Y[barCenterNum], centerMoveX, centerMove, false, false, false);

					#endregion
				}

                #endregion

                switch (rCurrentlySelectedSong.eノード種別)
				{
					case CSongListNode.ENodeType.SCORE:
						{
							if (TJAPlayer3.Tx.SongSelect_Frame_Score != null)
							{
								// 難易度がTower、Danではない
								if (TJAPlayer3.stageSongSelect.n現在選択中の曲の難易度 != (int)Difficulty.Tower && TJAPlayer3.stageSongSelect.n現在選択中の曲の難易度 != (int)Difficulty.Dan)
								{
                                    #region [Display difficulty boxes]

                                    bool uraExists = TJAPlayer3.stageSongSelect.r現在選択中のスコア.譜面情報.nレベル[(int)Difficulty.Edit] >= 0;
									bool omoteExists = TJAPlayer3.stageSongSelect.r現在選択中のスコア.譜面情報.nレベル[(int)Difficulty.Oni] >= 0;

									for (int i = 0; i < (int)Difficulty.Edit + 1; i++)
									{
										if (ctBarOpen.CurrentValue >= 100 || TJAPlayer3.Skin.SongSelect_Shorten_Frame_Fade)
										{
                                            #region [Skip conditions]

                                            // Don't even bother process the Ura box if there isn't one
                                            if (!uraExists && i == (int)Difficulty.Edit)
												break;

											// Don't process oni box if ura exists but not oni
											else if (uraExists && !omoteExists && i == (int)Difficulty.Oni)
												continue;

											#endregion

											// avaliable : bool (Chart exists)
											#region [Gray box if stage isn't avaliable]

											bool avaliable = TJAPlayer3.stageSongSelect.r現在選択中のスコア.譜面情報.nレベル[i] >= 0;

											if (avaliable)
												TJAPlayer3.Tx.SongSelect_Frame_Score[0].color4 = new Color4(1f, 1f, 1f, 1f);
											else
												TJAPlayer3.Tx.SongSelect_Frame_Score[0].color4 = new Color4(0.5f, 0.5f, 0.5f, 1f);

											#endregion

											// opacity : int (Box and scores opacity)
											#region [Opacity management]

											int opacity = 0;

											if (avaliable && BarAnimeCount == 1.0)
											{
												if (ctScoreFrameAnime.CurrentValue <= 3000)
													opacity = Math.Max(0, ctScoreFrameAnime.CurrentValue - 2745);
												else 
													opacity = Math.Min(255, 255 - (ctScoreFrameAnime.CurrentValue - 5745));
											}

											#endregion

											#region [Display box parameters]

											bool _switchingUra = i == (int)Difficulty.Edit && omoteExists;

                                            int difSelectOpacity = (_switchingUra) ? (BarAnimeCount < 1.0 ? 0 : opacity) : (int)(BarAnimeCount * 255.0f);

                                            if (TJAPlayer3.Skin.SongSelect_Shorten_Frame_Fade && !_switchingUra)
											{
                                                difSelectOpacity = 255;
                                            }


                                            if (!TJAPlayer3.stageSongSelect.actDifficultySelectionScreen.bIsDifficltSelect || ctDifficultyIn.CurrentValue < 1000)
                                            {
												TJAPlayer3.Tx.SongSelect_Frame_Score[0].Opacity = difSelectOpacity;
												TJAPlayer3.Tx.SongSelect_Level_Number.Opacity = difSelectOpacity;
                                                TJAPlayer3.Tx.SongSelect_Level_Number_Icon?.tUpdateOpacity(difSelectOpacity);
                                                TJAPlayer3.Tx.SongSelect_Level_Number_Colored?.tUpdateOpacity(difSelectOpacity);
                                                if (TJAPlayer3.Tx.SongSelect_Level != null) TJAPlayer3.Tx.SongSelect_Level.Opacity = difSelectOpacity;
											}
											else if (ctDifficultyIn.CurrentValue >= 1000)
											{
												int difInOpacity = (int)((float)((int)255.0f - (ctDifficultyIn.CurrentValue - 1000)) * ((i == (int)Difficulty.Edit && omoteExists) ? (float)difSelectOpacity / 255f : 1f));

												TJAPlayer3.Tx.SongSelect_Frame_Score[0].Opacity = difInOpacity;
												TJAPlayer3.Tx.SongSelect_Level_Number.Opacity = difInOpacity;
                                                TJAPlayer3.Tx.SongSelect_Level_Number_Icon?.tUpdateOpacity(difInOpacity);
                                                TJAPlayer3.Tx.SongSelect_Level_Number_Colored?.tUpdateOpacity(difInOpacity);
                                                if (TJAPlayer3.Tx.SongSelect_Level != null) TJAPlayer3.Tx.SongSelect_Level.Opacity = difInOpacity;
											}

											#endregion

											#region [Displayables]

											int displayingDiff = Math.Min(i, (int)Difficulty.Oni);
											int positionalOffset = displayingDiff * 122;

											int width = TJAPlayer3.Tx.SongSelect_Frame_Score[0].sz画像サイズ.Width / 5;
											int height = TJAPlayer3.Tx.SongSelect_Frame_Score[0].sz画像サイズ.Height;

											TJAPlayer3.Tx.SongSelect_Frame_Score[0].t2D描画(TJAPlayer3.Skin.SongSelect_Frame_Score_X[displayingDiff], TJAPlayer3.Skin.SongSelect_Frame_Score_Y[displayingDiff], new Rectangle(width * i, 0, width, height));

											if (avaliable)
                                            {
												t小文字表示(TJAPlayer3.Skin.SongSelect_Level_Number_X[displayingDiff], TJAPlayer3.Skin.SongSelect_Level_Number_Y[displayingDiff],
													TJAPlayer3.stageSongSelect.r現在選択中のスコア.譜面情報.nレベル[i],
													i,
                                                    TJAPlayer3.stageSongSelect.r現在選択中のスコア.譜面情報.nLevelIcon[i]
                                                    );

												if (TJAPlayer3.Tx.SongSelect_Level != null)
												{
													int level_width = TJAPlayer3.Tx.SongSelect_Level.szTextureSize.Width / 7;
													int level_height = TJAPlayer3.Tx.SongSelect_Level.szTextureSize.Height;

													for (int i2 = 0; i2 < TJAPlayer3.stageSongSelect.r現在選択中のスコア.譜面情報.nレベル[i]; i2++)
													{
														TJAPlayer3.Tx.SongSelect_Level.t2D描画(
															TJAPlayer3.Skin.SongSelect_Level_X[displayingDiff] + (TJAPlayer3.Skin.SongSelect_Level_Move[0] * i2),
															TJAPlayer3.Skin.SongSelect_Level_Y[displayingDiff] + (TJAPlayer3.Skin.SongSelect_Level_Move[1] * i2),
															new RectangleF(level_width * i, 0, level_width, level_height));
													}
												}

												if (TJAPlayer3.stageSongSelect.r現在選択中のスコア.譜面情報.b譜面分岐[i])
												{
                                                    TJAPlayer3.Tx.SongSelect_Branch?.tUpdateOpacity(TJAPlayer3.Tx.SongSelect_Frame_Score[0].Opacity);
                                                    TJAPlayer3.Tx.SongSelect_Branch?.t2D描画(
                                                        
                                                        TJAPlayer3.Skin.SongSelect_Frame_Score_X[displayingDiff] + TJAPlayer3.Skin.SongSelect_Branch_Offset[0],
                                                        TJAPlayer3.Skin.SongSelect_Frame_Score_Y[displayingDiff] + TJAPlayer3.Skin.SongSelect_Branch_Offset[1]
                                                    );
                                                }
													
                                            }

											#endregion

										}
									}

                                    #endregion

                                }
								else
                                {
									// diff : int (5 : Tower, 6 : Dan)
                                    #region [Check if Dan or Tower]

                                    int diff = 5;
									if (TJAPlayer3.stageSongSelect.n現在選択中の曲の難易度 == (int)Difficulty.Dan)
										diff = 6;

									#endregion

									// avaliable : bool (Chart exists)
									#region [Gray box if stage isn't avaliable]

									bool avaliable = TJAPlayer3.stageSongSelect.r現在選択中のスコア.譜面情報.nレベル[diff] >= 0;

									if (avaliable)
										TJAPlayer3.Tx.SongSelect_Frame_Score[1].color4 = new Color4(1f, 1f, 1f, 1f);
									else
										TJAPlayer3.Tx.SongSelect_Frame_Score[1].color4 = new Color4(0.5f, 0.5f, 0.5f, 1f);

									#endregion

									#region [Display box parameters]

									int difSelectOpacity = (int)(BarAnimeCount * 255);

									if (TJAPlayer3.Skin.SongSelect_Shorten_Frame_Fade)
										difSelectOpacity = 255;

									if (!TJAPlayer3.stageSongSelect.actDifficultySelectionScreen.bIsDifficltSelect || ctDifficultyIn.CurrentValue < 1000)
									{
										TJAPlayer3.Tx.SongSelect_Frame_Score[1].Opacity = difSelectOpacity;
										TJAPlayer3.Tx.SongSelect_Level_Number.Opacity = difSelectOpacity;
                                        TJAPlayer3.Tx.SongSelect_Level_Number_Icon?.tUpdateOpacity(difSelectOpacity);
                                        TJAPlayer3.Tx.SongSelect_Level_Number_Colored?.tUpdateOpacity(difSelectOpacity);
                                        if (TJAPlayer3.Tx.SongSelect_Level != null) TJAPlayer3.Tx.SongSelect_Level.Opacity = difSelectOpacity;
									}
									else if (ctDifficultyIn.CurrentValue >= 1000)
									{
										int difInOpacity = (int)255.0f - (ctDifficultyIn.CurrentValue - 1000);

										TJAPlayer3.Tx.SongSelect_Frame_Score[1].Opacity = difInOpacity;
										TJAPlayer3.Tx.SongSelect_Level_Number.Opacity = difInOpacity;
                                        TJAPlayer3.Tx.SongSelect_Level_Number_Icon?.tUpdateOpacity(difInOpacity);
                                        TJAPlayer3.Tx.SongSelect_Level_Number_Colored?.tUpdateOpacity(difInOpacity);
                                        if (TJAPlayer3.Tx.SongSelect_Level != null) TJAPlayer3.Tx.SongSelect_Level.Opacity = difInOpacity;
									}

									#endregion

									#region [Displayables]

									int displayingDiff = diff == 5 ? 0 : 2;
									int width = TJAPlayer3.Tx.SongSelect_Frame_Score[0].sz画像サイズ.Width / 5;
									int height = TJAPlayer3.Tx.SongSelect_Frame_Score[0].sz画像サイズ.Height;

									TJAPlayer3.Tx.SongSelect_Frame_Score[1].t2D描画(TJAPlayer3.Skin.SongSelect_Frame_Score_X[displayingDiff], TJAPlayer3.Skin.SongSelect_Frame_Score_Y[displayingDiff], new Rectangle(width * displayingDiff, 0, width, height));

									var _level_number = (diff == 5) ? TJAPlayer3.Skin.SongSelect_Level_Number_Tower : TJAPlayer3.Skin.SongSelect_Level_Number_Tower;


                                    if (avaliable)
									{
										t小文字表示(TJAPlayer3.Skin.SongSelect_Level_Number_X[displayingDiff], TJAPlayer3.Skin.SongSelect_Level_Number_Y[displayingDiff],
											TJAPlayer3.stageSongSelect.r現在選択中のスコア.譜面情報.nレベル[diff],
											diff,
                                            TJAPlayer3.stageSongSelect.r現在選択中のスコア.譜面情報.nLevelIcon[diff]
                                            );

										if (diff == 5)
										{
											var _sidet = TJAPlayer3.Tx.SongSelect_Tower_Side;
											if (_sidet != null)
											{
                                                var _side = (TJAPlayer3.stageSongSelect.rNowSelectedSong.nSide == CDTX.ESide.eNormal) ? 0 : 1;
												var _sc = _sidet.sz画像サイズ.Width / 2;
                                                _sidet.t2D描画(
												TJAPlayer3.Skin.SongSelect_Tower_Side[0],
                                                TJAPlayer3.Skin.SongSelect_Tower_Side[1],
                                                new Rectangle(_side * _sc, 0, _sc, _sidet.sz画像サイズ.Height));
                                            }

                                        }

										if (TJAPlayer3.Tx.SongSelect_Level != null)
										{
											int level_width = TJAPlayer3.Tx.SongSelect_Level.szTextureSize.Width / 7;
											int level_height = TJAPlayer3.Tx.SongSelect_Level.szTextureSize.Height;

											for (int i2 = 0; i2 < TJAPlayer3.stageSongSelect.r現在選択中のスコア.譜面情報.nレベル[diff]; i2++)
											{
												TJAPlayer3.Tx.SongSelect_Level?.t2D描画(
													TJAPlayer3.Skin.SongSelect_Level_X[displayingDiff] + (TJAPlayer3.Skin.SongSelect_Level_Move[0] * i2),
													TJAPlayer3.Skin.SongSelect_Level_Y[displayingDiff] + (TJAPlayer3.Skin.SongSelect_Level_Move[1] * i2),
													new RectangleF(level_width * diff, 0, level_width, level_height));

											}
										}
									}

									#endregion

								}

							}
						}
						break;

					case CSongListNode.ENodeType.BOX:
						{
							for (int j = 0; j < 3; j++)
							{
								if (!ctBoxOpen.IsEnded && ctBoxOpen.CurrentValue != 0)
								{
									if (txBoxText[j] != null)
										this.txBoxText[j].Opacity = (int)(ctBoxOpen.CurrentValue >= 1200 && ctBoxOpen.CurrentValue <= 1620 ? 255 - (ctBoxOpen.CurrentValue - 1200) * 2.55f :
										ctBoxOpen.CurrentValue >= 2000 ? (ctBoxOpen.CurrentValue - 2000) * 2.55f : ctBoxOpen.CurrentValue <= 1200 ? 255 : 0);
								}
								else
									if (txBoxText[j] != null)
									this.txBoxText[j].Opacity = (int)(BarAnimeCount * 255.0f);

								if (this.txBoxText[j].szTextureSize.Width >= 510)
									this.txBoxText[j].vcScaleRatio.X = 510f / this.txBoxText[j].szTextureSize.Width;

								this.txBoxText[j].t2D拡大率考慮中央基準描画(TJAPlayer3.Skin.SongSelect_BoxExplanation_X, TJAPlayer3.Skin.SongSelect_BoxExplanation_Y + j * TJAPlayer3.Skin.SongSelect_BoxExplanation_Interval);
							}

							// Chara here

							var box_chara = HGenreBar.tGetGenreBar(rCurrentlySelectedSong.BoxChara, TJAPlayer3.Tx.SongSelect_Box_Chara);

							// If BoxChara < 0, don't display any character
                            {
								if (!ctBoxOpen.IsEnded)
									box_chara.Opacity = (int)(ctBoxOpen.CurrentValue >= 1200 && ctBoxOpen.CurrentValue <= 1620 ? 255 - (ctBoxOpen.CurrentValue - 1200) * 2.55f :
									ctBoxOpen.CurrentValue >= 2000 ? (ctBoxOpen.CurrentValue - 2000) * 2.55f : ctBoxOpen.CurrentValue <= 1200 ? 255 : 0);
								else
								{
									if (!TJAPlayer3.stageSongSelect.actDifficultySelectionScreen.bIsDifficltSelect)
										box_chara.Opacity = (int)(BarAnimeCount * 255.0f);
									else if (ctDifficultyIn.CurrentValue >= 1000)
										box_chara.Opacity = (int)255.0f - (ctDifficultyIn.CurrentValue - 1000);
								}

								box_chara?.t2D中心基準描画(TJAPlayer3.Skin.SongSelect_Box_Chara_X[0] - (BarAnimeCount * TJAPlayer3.Skin.SongSelect_Box_Chara_Move), TJAPlayer3.Skin.SongSelect_Box_Chara_Y[0],
									new Rectangle(0, 0, box_chara.szTextureSize.Width / 2,
									box_chara.szTextureSize.Height));

								box_chara?.t2D中心基準描画(TJAPlayer3.Skin.SongSelect_Box_Chara_X[1] + (BarAnimeCount * TJAPlayer3.Skin.SongSelect_Box_Chara_Move), TJAPlayer3.Skin.SongSelect_Box_Chara_Y[1],
									new Rectangle(box_chara.szTextureSize.Width / 2, 0,
									box_chara.szTextureSize.Width / 2, box_chara.szTextureSize.Height));
							}
						}
						break;

					case CSongListNode.ENodeType.BACKBOX:
						//if (TJAPlayer3.Tx.SongSelect_Frame_BackBox != null)
						//	TJAPlayer3.Tx.SongSelect_Frame_BackBox.t2D描画(450, TJAPlayer3.Skin.SongSelect_Overall_Y);
						break;

					case CSongListNode.ENodeType.RANDOM:
						//if (TJAPlayer3.Tx.SongSelect_Frame_Random != null)
						//	TJAPlayer3.Tx.SongSelect_Frame_Random.t2D描画(450, TJAPlayer3.Skin.SongSelect_Overall_Y);
						break;
				}

				/*if (TJAPlayer3.Tx.SongSelect_Branch_Text != null 
					&& TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.b譜面分岐[TJAPlayer3.stage選曲.n現在選択中の曲の難易度])
					TJAPlayer3.Tx.SongSelect_Branch_Text.t2D描画(483, TJAPlayer3.Skin.SongSelect_Overall_Y + 21);
				*/
			}

			if (ctBoxOpen.CurrentValue >= 1620)
			{
				if (bBoxOpen)
				{
					this.tBOXに入る();
					bBoxOpen = false;
				}
				if (bBoxClose)
				{
					this.tCloseBOX();
					TJAPlayer3.stageSongSelect.bBGM再生済み = false;
					/*
					if (TJAPlayer3.ConfigIni.bBGM音を発声する || !TJAPlayer3.Skin.bgm選曲画面イン.b再生中)
						TJAPlayer3.Skin.bgm選曲画面イン.t再生する();
					TJAPlayer3.stage選曲.bBGMIn再生した = true;
					*/
					CSongSelectSongManager.playSongIfPossible();
					bBoxClose = false;
				}
			}

			if(ctDifficultyIn.CurrentValue >= ctDifficultyIn.EndValue)
            {
				ctDifficultyIn.Stop();
			}

			for (int i = 0; i < TJAPlayer3.Skin.SongSelect_Bar_Count; i++)    // パネルは全13枚。
			{
				int index = i + nNowChange;
				if (((index < 0 || index >= TJAPlayer3.Skin.SongSelect_Bar_Count) && this.ctScrollCounter.CurrentValue != this.ctScrollCounter.EndValue))
					continue;

				int nパネル番号 = (((this.n現在の選択行 - barCenterNum) + i) + TJAPlayer3.Skin.SongSelect_Bar_Count) % TJAPlayer3.Skin.SongSelect_Bar_Count;
				int n見た目の行番号 = i;
				int n次のパネル番号 = ((i + nNowChange) % TJAPlayer3.Skin.SongSelect_Bar_Count);
				//int x = this.ptバーの基本座標[ n見た目の行番号 ].X + ( (int) ( ( this.ptバーの基本座標[ n次のパネル番号 ].X - this.ptバーの基本座標[ n見た目の行番号 ].X ) * ( ( (double) Math.Abs( this.n現在のスクロールカウンタ ) ) / 100.0 ) ) );
				int x = i選曲バーX座標;
				int xAnime = TJAPlayer3.Skin.SongSelect_Bar_X[n見た目の行番号] + ((int)((TJAPlayer3.Skin.SongSelect_Bar_X[n次のパネル番号] - TJAPlayer3.Skin.SongSelect_Bar_X[n見た目の行番号]) *
					fNowScrollAnime));

				int y = TJAPlayer3.Skin.SongSelect_Bar_Y[n見た目の行番号] + ((int)((TJAPlayer3.Skin.SongSelect_Bar_Y[n次のパネル番号] - TJAPlayer3.Skin.SongSelect_Bar_Y[n見た目の行番号]) *
					fNowScrollAnime));
				
				if ((i == barCenterNum) && ctScrollCounter.CurrentValue == ctScrollCounter.EndValue)
				{
					CTexture tx選択している曲のサブタイトル = null;

					// (A) スクロールが停止しているときの選択曲バーの描画。

					#region [ タイトル名テクスチャを描画。]

					// Fonts here

					//-----------------
					if (rCurrentlySelectedSong.strタイトル != "" && this.ttk選択している曲の曲名 == null)
						this.ttk選択している曲の曲名 = this.ttk曲名テクスチャを生成する(rCurrentlySelectedSong.strタイトル, rCurrentlySelectedSong.ForeColor, rCurrentlySelectedSong.BackColor, rCurrentlySelectedSong.eノード種別 == CSongListNode.ENodeType.BOX ? this.pfBoxName : this.pfMusicName);
					if (rCurrentlySelectedSong.strサブタイトル != "" && this.ttk選択している曲のサブタイトル == null)
						this.ttk選択している曲のサブタイトル = this.ttkサブタイトルテクスチャを生成する(rCurrentlySelectedSong.strサブタイトル, rCurrentlySelectedSong.ForeColor, rCurrentlySelectedSong.BackColor);
					if (rCurrentlySelectedSong.strMaker != "" && this.ttkSelectedSongMaker == null)
						this.ttkSelectedSongMaker = this.ttkGenerateMakerTexture(rCurrentlySelectedSong.strMaker, rCurrentlySelectedSong.ForeColor, rCurrentlySelectedSong.BackColor);
					if (this.ttkSelectedSongBPM == null)
						this.ttkSelectedSongBPM = this.ttkGenerateBPMTexture(rCurrentlySelectedSong, rCurrentlySelectedSong.ForeColor, rCurrentlySelectedSong.BackColor); ;


					if (this.ttk選択している曲のサブタイトル != null)
						tx選択している曲のサブタイトル = ResolveTitleTexture(ttk選択している曲のサブタイトル, TJAPlayer3.Skin.SongSelect_VerticalText);

					//サブタイトルがあったら700

					if (ttk選択している曲の曲名 != null)
					{
						if (!ctBoxOpen.IsEnded)
							ResolveTitleTexture(this.ttk選択している曲の曲名, TJAPlayer3.Skin.SongSelect_VerticalText).Opacity = (int)(ctBoxOpen.CurrentValue >= 1200 && ctBoxOpen.CurrentValue <= 1620 ? 255 - (ctBoxOpen.CurrentValue - 1200) * 2.55f :
							ctBoxOpen.CurrentValue >= 2000 ? (ctBoxOpen.CurrentValue - 2000) * 2.55f : ctBoxOpen.CurrentValue <= 1200 ? 255 : 0);
						else
						{
							if (!TJAPlayer3.stageSongSelect.actDifficultySelectionScreen.bIsDifficltSelect)
								ResolveTitleTexture(this.ttk選択している曲の曲名, TJAPlayer3.Skin.SongSelect_VerticalText).Opacity = 255;
							else if (ctDifficultyIn.CurrentValue >= 1000)
								ResolveTitleTexture(this.ttk選択している曲の曲名, TJAPlayer3.Skin.SongSelect_VerticalText).Opacity = (int)255.0f - (ctDifficultyIn.CurrentValue - 1000);
						}
					}

					if (this.ttk選択している曲のサブタイトル != null)
					{
						if (!ctBoxOpen.IsEnded)
							tx選択している曲のサブタイトル.Opacity = (int)(ctBoxOpen.CurrentValue >= 1200 && ctBoxOpen.CurrentValue <= 1620 ? 255 - (ctBoxOpen.CurrentValue - 1200) * 2.55f :
							ctBoxOpen.CurrentValue >= 2000 ? (ctBoxOpen.CurrentValue - 2000) * 2.55f : ctBoxOpen.CurrentValue <= 1200 ? 255 : 0);
                        else
						{
							if (!TJAPlayer3.stageSongSelect.actDifficultySelectionScreen.bIsDifficltSelect)
								tx選択している曲のサブタイトル.Opacity = (int)(BarAnimeCount * 255.0f);
                            else if (ctDifficultyIn.CurrentValue >= 1000)
									tx選択している曲のサブタイトル.Opacity = (int)255.0f - (ctDifficultyIn.CurrentValue - 1000);
						} 

						tx選択している曲のサブタイトル.t2D拡大率考慮中央基準描画(
							xAnime + TJAPlayer3.Skin.SongSelect_Bar_SubTitle_Offset[0] + (rCurrentlySelectedSong.eノード種別 == CSongListNode.ENodeType.BOX ? centerMoveX : centerMoveX / 1.1f), 
							y + TJAPlayer3.Skin.SongSelect_Bar_SubTitle_Offset[1] - (rCurrentlySelectedSong.eノード種別 == CSongListNode.ENodeType.BOX ? centerMove : centerMove / 1.1f));
						
						if (this.ttk選択している曲の曲名 != null)
						{
							ResolveTitleTexture(this.ttk選択している曲の曲名, TJAPlayer3.Skin.SongSelect_VerticalText).t2D拡大率考慮中央基準描画(
								xAnime + GetTitleOffsetX(rCurrentlySelectedSong.eノード種別) +
								(rCurrentlySelectedSong.eノード種別 != CSongListNode.ENodeType.BACKBOX ? (rCurrentlySelectedSong.eノード種別 == CSongListNode.ENodeType.BOX ? centerMoveX : centerMoveX / 1.1f) : 0),

								y + GetTitleOffsetY(rCurrentlySelectedSong.eノード種別) - 
								(rCurrentlySelectedSong.eノード種別 != CSongListNode.ENodeType.BACKBOX ? (rCurrentlySelectedSong.eノード種別 == CSongListNode.ENodeType.BOX ? centerMove : centerMove / 1.1f) : 0));
						}
					}
					else
					{
						if (this.ttk選択している曲の曲名 != null)
						{
							ResolveTitleTexture(this.ttk選択している曲の曲名, TJAPlayer3.Skin.SongSelect_VerticalText).t2D拡大率考慮中央基準描画(
								xAnime + GetTitleOffsetX(this.stバー情報[nパネル番号].eバー種別) +
								(rCurrentlySelectedSong.eノード種別 != CSongListNode.ENodeType.BACKBOX ? (rCurrentlySelectedSong.eノード種別 == CSongListNode.ENodeType.BOX ? centerMoveX : centerMoveX / 1.1f) : 0), 

								y + GetTitleOffsetY(this.stバー情報[nパネル番号].eバー種別) - 
								(rCurrentlySelectedSong.eノード種別 != CSongListNode.ENodeType.BACKBOX ? (rCurrentlySelectedSong.eノード種別 == CSongListNode.ENodeType.BOX ? centerMove : centerMove / 1.1f) : 0));
						}
					}
					//-----------------
					#endregion
				}
			}
			//-----------------

			//TJAPlayer3.act文字コンソール.tPrint(0, 12, C文字コンソール.Eフォント種別.白, CSongDict.tGetNodesCount().ToString());

			return 0;
		}

		public void tMenuContextView(eMenuContext emc)
        {
			// Context vars :
			// 0 - Selected difficulty
			// 1 - Selected star rating
			// 2 - Current menu (0 : select difficulty, 1 : select star rating)
			if (emc == eMenuContext.SearchByDifficulty)
            {
				TJAPlayer3.Tx.SongSelect_Search_Window?.t2D描画(0, 0);

				int tileSize = 0;
				if (TJAPlayer3.Tx.Dani_Difficulty_Cymbol != null)
                {
					tileSize = TJAPlayer3.Tx.Dani_Difficulty_Cymbol.szTextureSize.Height;
					TJAPlayer3.Tx.Dani_Difficulty_Cymbol.t2D中心基準描画(TJAPlayer3.Skin.SongSelect_Search_Bar_X[0], TJAPlayer3.Skin.SongSelect_Search_Bar_Y[0],
						new Rectangle(tileSize * _contextVars[0],
						0,
						(_contextVars[0] == (int)Difficulty.Oni ? 2 : 1) * tileSize,
						tileSize));
				}
				

				if (_contextVars[2] == 0)
                {
					TJAPlayer3.Tx.SongSelect_Search_Arrow_Glow?.t2D中心基準描画(TJAPlayer3.Skin.SongSelect_Search_Bar_X[0], TJAPlayer3.Skin.SongSelect_Search_Bar_Y[0]);
				}
				else if (_contextVars[2] == 1)
                {
					TJAPlayer3.Tx.SongSelect_Search_Arrow?.t2D中心基準描画(TJAPlayer3.Skin.SongSelect_Search_Bar_X[0], TJAPlayer3.Skin.SongSelect_Search_Bar_Y[0]);
					TJAPlayer3.Tx.SongSelect_Search_Arrow_Glow?.t2D中心基準描画(TJAPlayer3.Skin.SongSelect_Search_Bar_X[1], TJAPlayer3.Skin.SongSelect_Search_Bar_Y[1]);

					if (TJAPlayer3.Tx.SongSelect_Level_Icons != null)
                    {
						tileSize = TJAPlayer3.Tx.SongSelect_Level_Icons.szTextureSize.Height;
						TJAPlayer3.Tx.SongSelect_Level_Icons.t2D中心基準描画(TJAPlayer3.Skin.SongSelect_Search_Bar_X[1], TJAPlayer3.Skin.SongSelect_Search_Bar_Y[1],
							new Rectangle(tileSize * _contextVars[1],
							0,
							tileSize,
							tileSize));
					}
					
				}
            }
			// Context vars :
			// 0~4 - Selected difficulty (1~5P)
			// 5 - Current menu (0~4 for each player)
			else if (emc == eMenuContext.Random)
            {
				// To change with a new texture
				TJAPlayer3.Tx.SongSelect_Search_Window?.t2D描画(0, 0);

				for (int i = 0; i <= _contextVars[5]; i++)
                {
					if (TJAPlayer3.Tx.Dani_Difficulty_Cymbol != null)
					{
						var tileSize = TJAPlayer3.Tx.Dani_Difficulty_Cymbol.szTextureSize.Height;
						TJAPlayer3.Tx.Dani_Difficulty_Cymbol.t2D中心基準描画(TJAPlayer3.Skin.SongSelect_Search_Bar_X[i], TJAPlayer3.Skin.SongSelect_Search_Bar_Y[i],
							new Rectangle(tileSize * _contextVars[i],
							0,
							(_contextVars[i] == (int)Difficulty.Oni ? 2 : 1) * tileSize,
							tileSize));

					}

					if (i < _contextVars[5])
						TJAPlayer3.Tx.SongSelect_Search_Arrow?.t2D中心基準描画(TJAPlayer3.Skin.SongSelect_Search_Bar_X[i], TJAPlayer3.Skin.SongSelect_Search_Bar_Y[i]);
					else if (i == _contextVars[5])
						TJAPlayer3.Tx.SongSelect_Search_Arrow_Glow?.t2D中心基準描画(TJAPlayer3.Skin.SongSelect_Search_Bar_X[i], TJAPlayer3.Skin.SongSelect_Search_Bar_Y[i]);
				}
            }
        }

		public bool tMenuContextController(eMenuContext emc)
        {
			tMenuContextView(emc);

			#region [Inputs]

			#region [Decide]

			if ((TJAPlayer3.Pad.bPressedDGB(EPad.Decide)) ||
			((TJAPlayer3.ConfigIni.bEnterがキー割り当てのどこにも使用されていない && TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return))))
			{
				if (emc == eMenuContext.SearchByDifficulty)
				{
					TJAPlayer3.Skin.soundDecideSFX.tPlay();

					_contextVars[2]++;
					if (_contextVars[2] >= 2)
					{
						//tMenuContextDisable();
						return true;
					}
					else if (_contextVars[2] == 1)
					{
						// Set default level for each difficulty
						switch (_contextVars[0])
						{
							case (int)Difficulty.Easy:
								_contextVars[1] = 1;
								break;
							case (int)Difficulty.Normal:
								_contextVars[1] = 3;
								break;
							case (int)Difficulty.Hard:
								_contextVars[1] = 6;
								break;
							default:
								_contextVars[1] = 8;
								break;
						}
					}

				}
				else if (emc == eMenuContext.Random)
				{
					TJAPlayer3.Skin.soundDecideSFX.tPlay();

					_contextVars[5]++;
					if (_contextVars[5] >= TJAPlayer3.ConfigIni.nPlayerCount)
						return true;
					if (_contextVars[5] >= 1 && TJAPlayer3.ConfigIni.bAIBattleMode)
					{
						_contextVars[1] = _contextVars[0];
                        return true;
					}
					_contextVars[_contextVars[5]] = Math.Min((int)Difficulty.Oni, TJAPlayer3.ConfigIni.nDefaultCourse);
                }

			}

            #endregion

            #region [Left]

            else if (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LeftChange)
				|| TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.LeftArrow))
            {
				if (emc == eMenuContext.SearchByDifficulty)
				{
					TJAPlayer3.Skin.soundChangeSFX.tPlay();

					_contextVars[_contextVars[2]]--;
					// Clamp values
					_contextVars[0] = Math.Max(0, Math.Min((int)Difficulty.Oni, _contextVars[0]));
					_contextVars[1] = Math.Max(1, Math.Min(13, _contextVars[1]));
				}
				else if (emc == eMenuContext.Random)
				{
					TJAPlayer3.Skin.soundChangeSFX.tPlay();

					_contextVars[_contextVars[5]]--;
					// Clamp values
					_contextVars[_contextVars[5]] = Math.Max(0, Math.Min((int)Difficulty.Oni, _contextVars[_contextVars[5]]));
				}
			}

			#endregion

			#region [Right]

			else if (TJAPlayer3.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RightChange)
				|| TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.RightArrow))
			{
				if (emc == eMenuContext.SearchByDifficulty)
				{
					TJAPlayer3.Skin.soundChangeSFX.tPlay();

					_contextVars[_contextVars[2]]++;
					// Clamp values
					_contextVars[0] = Math.Max(0, Math.Min((int)Difficulty.Oni, _contextVars[0]));
					_contextVars[1] = Math.Max(1, Math.Min(13, _contextVars[1]));
				}
				else if (emc == eMenuContext.Random)
				{
					TJAPlayer3.Skin.soundChangeSFX.tPlay();

					_contextVars[_contextVars[5]]++;
					// Clamp values
					_contextVars[_contextVars[5]] = Math.Max(0, Math.Min((int)Difficulty.Oni, _contextVars[_contextVars[5]]));
				}
			}

			#endregion

			#endregion

			return false;
		}

        public void tMenuContextTrigger(eMenuContext emc)
        {
			_contextVars = new int[10];
			isContextBoxOpened = true;
			latestContext = emc;
        }

		public void tMenuContextDisable()
        {
			isContextBoxOpened = false;
			latestContext = eMenuContext.NONE;
        }

		public int tMenuContextGetVar(int i)
        {
			if (i < 0 || i >= 10)
				return -1;
			return _contextVars[i];
        }

		public bool isContextBoxOpened = false;
		public eMenuContext latestContext = eMenuContext.NONE;
		private int[] _contextVars = new int[10];


        // その他

        #region [ private ]
        //-----------------
        private enum Eバー種別 { Score, Box, Other, BackBox, Random }

		// Edit + 1 => UraOmote ScorePad
		// public CScorePad[] ScorePads = new CScorePad[(int)Difficulty.Edit + 2] { new CScorePad(), new CScorePad(), new CScorePad(), new CScorePad(), new CScorePad(), new CScorePad() };
		// public CScorePad[] ScorePads2 = new CScorePad[(int)Difficulty.Edit + 2] { new CScorePad(), new CScorePad(), new CScorePad(), new CScorePad(), new CScorePad(), new CScorePad() };

		public class CScorePad
        {
			public int[] ScoreRankCount = new int[7];
			public int[] CrownCount = new int[3];
        }

		private struct STバー
		{
			public CTexture Score;
			public CTexture Box;
			public CTexture Other;
			public CTexture this[ int index ]
			{
				get
				{
					switch( index )
					{
						case 0:
							return this.Score;

						case 1:
							return this.Box;

						case 2:
							return this.Other;
					}
					throw new IndexOutOfRangeException();
				}
				set
				{
					switch( index )
					{
						case 0:
							this.Score = value;
							return;

						case 1:
							this.Box = value;
							return;

						case 2:
							this.Other = value;
							return;
					}
					throw new IndexOutOfRangeException();
				}
			}
		}

		private struct STバー情報
		{
			public CActSelect曲リスト.Eバー種別 eバー種別;
			public string strタイトル文字列;
			public CTexture txタイトル名;
			public STDGBVALUE<int> nスキル値;
			public Color col文字色;
            public Color ForeColor;
            public Color BackColor;
			public Color BoxColor;

			public Color BgColor;
			public string BoxType;
			public string BgType;
			public string BoxChara;

			public bool BoxTypeChanged;
			public bool BgTypeChanged;
			public bool BoxCharaChanged;

			public int[] ar難易度;
			public CDTX.ELevelIcon[] nLevelIcon;
            public bool[] b分岐;
            public string strジャンル;
            public string strサブタイトル;
            public TitleTextureKey ttkタイトル;

			public int[][] nクリア;
			public int[][] nスコアランク;

			public CSongUniqueID csu;
			public CSongListNode reference;
        }

		public bool bFirstCrownLoad;

		public CCounter ctBarFlash;
		public CCounter ctDifficultyIn;

		private CCounter ctScoreFrameAnime;

		public CCounter ctBarOpen;
		public CCounter ctBoxOpen;
		public bool bBoxOpen;
		public bool bBoxClose;

		public bool b選択曲が変更された = true;
		private bool b登場アニメ全部完了;
		private CCounter[] ct登場アニメ用 = new CCounter[ 13 ];
        private CCounter ct三角矢印アニメ;
        private CCachedFontRenderer pfMusicName;
        private CCachedFontRenderer pfSubtitle;
		private CCachedFontRenderer pfMaker;
        private CCachedFontRenderer pfBPM;
        private CCachedFontRenderer pfBoxName;

		private string strBoxText;
		private CCachedFontRenderer pfBoxText;
		private CTexture[] txBoxText = new CTexture[3];
		






		private readonly Dictionary<TitleTextureKey, CTexture> _titledictionary
			= new Dictionary<TitleTextureKey, CTexture>();

		private CCachedFontRenderer ft曲リスト用フォント;
		//public int n現在のスクロールカウンタ;
		private int n現在の選択行;
		//private int n目標のスクロールカウンタ;
		private CCounter ctScrollCounter;

		/*
        private readonly Point[] ptバーの座標 = new Point[] {
		new Point(214, -127),new Point(239, -36), new Point(263, 55), new Point(291, 145),
		new Point(324, 314),
		new Point(358, 485), new Point(386, 574), new Point(411, 665), new Point(436, 756) };
		*/

        private STバー情報[] stバー情報 = new STバー情報[TJAPlayer3.Skin.SongSelect_Bar_Count];
		private CTexture txSongNotFound, txEnumeratingSongs;

        private TitleTextureKey ttk選択している曲の曲名;
        private TitleTextureKey ttk選択している曲のサブタイトル;
		public TitleTextureKey ttkSelectedSongBPM;
		public TitleTextureKey ttkSelectedSongMaker;

		private CTexture[] tx曲バー_難易度 = new CTexture[ 5 ];

		private int nCurrentPosition = 0;
		private int nNumOfItems = 0;

		private int nNowChange;

		private int GetTitleOffsetX(Eバー種別 bar)
		{
			switch (bar)
			{
				case Eバー種別.Score:
					return TJAPlayer3.Skin.SongSelect_Bar_Title_Offset[0];
				case Eバー種別.Box:
					return TJAPlayer3.Skin.SongSelect_Bar_Box_Offset[0];
				case Eバー種別.BackBox:
					return TJAPlayer3.Skin.SongSelect_Bar_BackBox_Offset[0];
				case Eバー種別.Random:
					return TJAPlayer3.Skin.SongSelect_Bar_Random_Offset[0];
				default:
					return TJAPlayer3.Skin.SongSelect_Bar_Title_Offset[0];
			}
		}

		private int GetTitleOffsetX(CSongListNode.ENodeType node)
		{
			switch (node)
			{
				case CSongListNode.ENodeType.SCORE:
					return TJAPlayer3.Skin.SongSelect_Bar_Title_Offset[0];
				case CSongListNode.ENodeType.BOX:
					return TJAPlayer3.Skin.SongSelect_Bar_Box_Offset[0];
				case CSongListNode.ENodeType.BACKBOX:
					return TJAPlayer3.Skin.SongSelect_Bar_BackBox_Offset[0];
				case CSongListNode.ENodeType.RANDOM:
					return TJAPlayer3.Skin.SongSelect_Bar_Random_Offset[0];
				default:
					return TJAPlayer3.Skin.SongSelect_Bar_Title_Offset[0];
			}
		}

		private int GetTitleOffsetY(Eバー種別 bar)
		{
			switch (bar)
			{
				case Eバー種別.Score:
					return TJAPlayer3.Skin.SongSelect_Bar_Title_Offset[1];
				case Eバー種別.Box:
					return TJAPlayer3.Skin.SongSelect_Bar_Box_Offset[1];
				case Eバー種別.BackBox:
					return TJAPlayer3.Skin.SongSelect_Bar_BackBox_Offset[1];
				case Eバー種別.Random:
					return TJAPlayer3.Skin.SongSelect_Bar_Random_Offset[1];
				default:
					return TJAPlayer3.Skin.SongSelect_Bar_Title_Offset[1];
			}
		}

		private int GetTitleOffsetY(CSongListNode.ENodeType node)
		{
			switch (node)
			{
				case CSongListNode.ENodeType.SCORE:
					return TJAPlayer3.Skin.SongSelect_Bar_Title_Offset[1];
				case CSongListNode.ENodeType.BOX:
					return TJAPlayer3.Skin.SongSelect_Bar_Box_Offset[1];
				case CSongListNode.ENodeType.BACKBOX:
					return TJAPlayer3.Skin.SongSelect_Bar_BackBox_Offset[1];
				case CSongListNode.ENodeType.RANDOM:
					return TJAPlayer3.Skin.SongSelect_Bar_Random_Offset[1];
				default:
					return TJAPlayer3.Skin.SongSelect_Bar_Title_Offset[1];
			}
		}

		private void DrawBarCenter(CTexture texture, int x, int y, int moveX, int move, bool changeColor, bool drawOverlay, bool fullScaleOverlay)
		{
            CTexture overlay = TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay;

            float openAnime = 1;

            if (ctBoxOpen.CurrentValue >= 1300 && ctBoxOpen.CurrentValue <= 1940)
            {
                openAnime -= (float)Math.Sin(((ctBoxOpen.CurrentValue - 1300) * 0.28125f) * (Math.PI / 180)) * 1.0f;
            }

            float overlay_xoffset = ((overlay.szTextureSize.Width / 3) * (1.0f - openAnime));
            float moveX_xoffset = (TJAPlayer3.Skin.SongSelect_Bar_Center_Move_X * (1.0f - openAnime));

            int width = overlay.sz画像サイズ.Width / 3;
            int height = overlay.sz画像サイズ.Height / 3;

            if (texture != null)
			{
                if (changeColor)
                {
                    texture.color4 = CConversion.ColorToColor4(rCurrentlySelectedSong.BoxColor);
                }

                float texture_xoffset = ((texture.szTextureSize.Width / 3) * (1.0f - openAnime));

                texture.vcScaleRatio.X = 1.0f * openAnime;
                texture.vcScaleRatio.Y = 1.0f;
                texture.t2D描画(x + (texture_xoffset * 1.5f) + moveX_xoffset - moveX, y - move, new Rectangle(0, 0, width, height));

                texture.vcScaleRatio.X = 1.0f * openAnime;
                texture.vcScaleRatio.Y = 1.0f + ((move / (float)height) * 2.0f);
                texture.t2D描画(x + (texture_xoffset * 1.5f) + moveX_xoffset - moveX, y + height - move, new Rectangle(0, height, width, height));

                texture.vcScaleRatio.X = 1.0f * openAnime;
                texture.vcScaleRatio.Y = 1.0f;
                texture.t2D描画(x + (texture_xoffset * 1.5f) + moveX_xoffset - moveX, y + (height * 2) + move, new Rectangle(0, height * 2, width, height));


                texture.vcScaleRatio.X = (1.0f + ((moveX / (float)width) * 2.0f)) * openAnime;
                texture.vcScaleRatio.Y = 1.0f;
                texture.t2D描画(x + (texture_xoffset / 2) + moveX_xoffset - moveX + width, y - move, new Rectangle(width, 0, width, height));

                texture.vcScaleRatio.X = (1.0f + ((moveX / (float)width) * 2.0f)) * openAnime;
                texture.vcScaleRatio.Y = 1.0f + ((move / (float)height) * 2.0f);
                texture.t2D描画(x + (texture_xoffset / 2) + moveX_xoffset - moveX + width, y + height - move, new Rectangle(width, height, width, height));

                texture.vcScaleRatio.X = (1.0f + ((moveX / (float)width) * 2.0f)) * openAnime;
                texture.vcScaleRatio.Y = 1.0f;
                texture.t2D描画(x + (texture_xoffset / 2) + moveX_xoffset - moveX + width, y + (height * 2) + move, new Rectangle(width, height * 2, width, height));


                texture.vcScaleRatio.X = 1.0f * openAnime;
                texture.vcScaleRatio.Y = 1.0f;
                texture.t2D描画(x - (texture_xoffset / 2) - moveX_xoffset + moveX + (width * 2), y - move, new Rectangle(width * 2, 0, width, height));

                texture.vcScaleRatio.X = 1.0f * openAnime;
                texture.vcScaleRatio.Y = 1.0f + ((move / (float)height) * 2.0f);
                texture.t2D描画(x - (texture_xoffset / 2) - moveX_xoffset + moveX + (width * 2), y + height - move, new Rectangle(width * 2, height, width, height));

                texture.vcScaleRatio.X = 1.0f * openAnime;
                texture.vcScaleRatio.Y = 1.0f;
                texture.t2D描画(x - (texture_xoffset / 2) - moveX_xoffset + moveX + (width * 2), y + (height * 2) + move, new Rectangle(width * 2, height * 2, width, height));

            }

            if (drawOverlay)
			{
				if (fullScaleOverlay)
				{
					overlay.vcScaleRatio.X = 1.0f * openAnime;
					overlay.vcScaleRatio.Y = 1.0f;
					overlay.t2D描画(x + (overlay_xoffset * 1.5f) + moveX_xoffset - moveX, y - move, new Rectangle(0, 0, width, height));

					overlay.vcScaleRatio.X = 1.0f * openAnime;
					overlay.vcScaleRatio.Y = 1.0f + ((move / (float)height) * 2.0f);
					overlay.t2D描画(x + (overlay_xoffset * 1.5f) + moveX_xoffset - moveX, y + height - move, new Rectangle(0, height, width, height));
				}
				else
				{
					overlay.vcScaleRatio.X = 1.0f * openAnime;
					overlay.vcScaleRatio.Y = 1.0f;
					overlay.t2D描画(x + (overlay_xoffset * 1.5f) + moveX_xoffset - moveX, y, new Rectangle(0, 0, width, height));

					overlay.vcScaleRatio.X = 1.0f * openAnime;
					overlay.vcScaleRatio.Y = 1.0f + ((move / (float)height) * 1.0f);
					overlay.t2D描画(x + (overlay_xoffset * 1.5f) + moveX_xoffset - moveX, y + height, new Rectangle(0, height, width, height));
				}
				overlay.vcScaleRatio.X = 1.0f * openAnime;
				overlay.vcScaleRatio.Y = 1.0f;
				overlay.t2D描画(x + (overlay_xoffset * 1.5f) + moveX_xoffset - moveX, y + (height * 2) + move, new Rectangle(0, height * 2, width, height));


				if (fullScaleOverlay)
				{
					overlay.vcScaleRatio.X = (1.0f + ((moveX / (float)width) * 2.0f)) * openAnime;
					overlay.vcScaleRatio.Y = 1.0f;
					overlay.t2D描画(x + (overlay_xoffset / 2) + moveX_xoffset - moveX + width, y - move, new Rectangle(width, 0, width, height));

					overlay.vcScaleRatio.X = (1.0f + ((moveX / (float)width) * 2.0f)) * openAnime;
					overlay.vcScaleRatio.Y = 1.0f + ((move / (float)height) * 2.0f);
					overlay.t2D描画(x + (overlay_xoffset / 2) + moveX_xoffset - moveX + width, y + height - move, new Rectangle(width, height, width, height));
				}
				else
				{
					overlay.vcScaleRatio.X = (1.0f + ((moveX / (float)width) * 2.0f)) * openAnime;
					overlay.vcScaleRatio.Y = 1.0f;
					overlay.t2D描画(x + (overlay_xoffset / 2) + moveX_xoffset - moveX + width, y, new Rectangle(width, 0, width, height));

					overlay.vcScaleRatio.X = (1.0f + ((moveX / (float)width) * 2.0f)) * openAnime;
					overlay.vcScaleRatio.Y = 1.0f + ((move / (float)height) * 1.0f);
					overlay.t2D描画(x + (overlay_xoffset / 2) + moveX_xoffset - moveX + width, y + height, new Rectangle(width, height, width, height));
				}
				overlay.vcScaleRatio.X = (1.0f + ((moveX / (float)width) * 2.0f)) * openAnime;
				overlay.vcScaleRatio.Y = 1.0f;
				overlay.t2D描画(x + (overlay_xoffset / 2) + moveX_xoffset - moveX + width, y + (height * 2) + move, new Rectangle(width, height * 2, width, height));


				if (fullScaleOverlay)
				{
					overlay.vcScaleRatio.X = 1.0f * openAnime;
					overlay.vcScaleRatio.Y = 1.0f;
					overlay.t2D描画(x - (overlay_xoffset / 2) - moveX_xoffset + moveX + (width * 2), y - move, new Rectangle(width * 2, 0, width, height));

					overlay.vcScaleRatio.X = 1.0f * openAnime;
					overlay.vcScaleRatio.Y = 1.0f + ((move / (float)height) * 2.0f);
					overlay.t2D描画(x - (overlay_xoffset / 2) - moveX_xoffset + moveX + (width * 2), y + height - move, new Rectangle(width * 2, height, width, height));
				}
				else
				{
					overlay.vcScaleRatio.X = 1.0f * openAnime;
					overlay.vcScaleRatio.Y = 1.0f;
					overlay.t2D描画(x - (overlay_xoffset / 2) - moveX_xoffset + moveX + (width * 2), y, new Rectangle(width * 2, 0, width, height));

					overlay.vcScaleRatio.X = 1.0f * openAnime;
					overlay.vcScaleRatio.Y = 1.0f + ((move / (float)height) * 1.0f);
					overlay.t2D描画(x - (overlay_xoffset / 2) - moveX_xoffset + moveX + (width * 2), y + height, new Rectangle(width * 2, height, width, height));
				}
				overlay.vcScaleRatio.X = 1.0f * openAnime;
				overlay.vcScaleRatio.Y = 1.0f;
				overlay.t2D描画(x - (overlay_xoffset / 2) - moveX_xoffset + moveX + (width * 2), y + (height * 2) + move, new Rectangle(width * 2, height * 2, width, height));

			}

		}

		private Eバー種別 e曲のバー種別を返す( CSongListNode song )
		{
			if( song != null )
			{
				switch( song.eノード種別 )
				{
					case CSongListNode.ENodeType.SCORE:
					case CSongListNode.ENodeType.SCORE_MIDI:
						return Eバー種別.Score;

					case CSongListNode.ENodeType.BOX:
						return Eバー種別.Box;

					case CSongListNode.ENodeType.BACKBOX:
						return Eバー種別.BackBox;

					case CSongListNode.ENodeType.RANDOM:
						return Eバー種別.Random;
				}
			}
			return Eバー種別.Other;
		}
		private void tChangeSong(int change)
		{
			List<CSongListNode> list = (TJAPlayer3.ConfigIni.TJAP3FolderMode && rCurrentlySelectedSong.rParentNode != null) ? rCurrentlySelectedSong.rParentNode.list子リスト : flattenList(TJAPlayer3.Songs管理.list曲ルート, true);

			int index = nSelectSongIndex + change;

			while (index >= list.Count)
			{
				index -= list.Count;
			}
			while (index < 0)
			{
				index += list.Count;
			}
			nSelectSongIndex = index;
			rCurrentlySelectedSong = list[index];
		}
		public CSongListNode rGetSideSong(int change)
		{
			if (rCurrentlySelectedSong == null) return null; 

			List<CSongListNode> list = (TJAPlayer3.ConfigIni.TJAP3FolderMode && rCurrentlySelectedSong.rParentNode != null) ? rCurrentlySelectedSong.rParentNode.list子リスト : flattenList(TJAPlayer3.Songs管理.list曲ルート, true);

			if (list.Count <= 0) return null;

			int index = nSelectSongIndex + change;

			while (index >= list.Count)
            {
				index -= list.Count;
			}
			while (index < 0)
			{
				index += list.Count;
			}

			return list[index];
		}

		public void tバーの初期化()
		{
            stバー情報 = new STバー情報[TJAPlayer3.Skin.SongSelect_Bar_Count];

            int barCenterNum = (TJAPlayer3.Skin.SongSelect_Bar_Count - 1) / 2;
            for ( int i = 0; i < TJAPlayer3.Skin.SongSelect_Bar_Count; i++ )
			{
				CSongListNode song = this.rGetSideSong(i - barCenterNum);
				if (song == null) continue;
				this.stバー情報[ i ].strタイトル文字列 = song.strタイトル;
                this.stバー情報[ i ].strジャンル = song.strジャンル;
				this.stバー情報[ i ].col文字色 = song.col文字色;
                this.stバー情報[i].ForeColor = song.ForeColor;
                this.stバー情報[i].BackColor = song.BackColor;

				this.stバー情報[i].BoxColor = song.BoxColor;
				this.stバー情報[i].BgColor = song.BgColor;
				
				this.stバー情報[i].BoxType = song.BoxType;
				this.stバー情報[i].BgType = song.BgType;

				this.stバー情報[i].BgTypeChanged = song.isChangedBgType;
				this.stバー情報[i].BoxTypeChanged = song.isChangedBoxType;

				this.stバー情報[i].BoxChara = song.BoxChara;
				this.stバー情報[i].BoxCharaChanged = song.isChangedBoxChara;

				this.stバー情報[ i ].eバー種別 = this.e曲のバー種別を返す( song );
                this.stバー情報[ i ].strサブタイトル = song.strサブタイトル;
                this.stバー情報[ i ].ar難易度 = song.nLevel;
                this.stバー情報[i].nLevelIcon = song.nLevelIcon;

                for ( int f = 0; f < (int)Difficulty.Total; f++ )
                {
                    if( song.arスコア[ f ] != null )
                        this.stバー情報[ i ].b分岐 = song.arスコア[ f ].譜面情報.b譜面分岐;
                }

				#region [Reroll cases]

				if (stバー情報[i].nクリア == null)
					this.stバー情報[i].nクリア = new int[2][];
				if (stバー情報[i].nスコアランク == null)
					this.stバー情報[i].nスコアランク = new int[2][];

				for (int d = 0; d < 2; d++)
				{
					this.stバー情報[i].nクリア[d] = new int[5];
					this.stバー情報[i].nスコアランク[d] = new int[5];

					if (this.stバー情報[i].eバー種別 == Eバー種別.Score)
					{
						int ap = TJAPlayer3.GetActualPlayer(d);
						var sr = song.arスコア[n現在のアンカ難易度レベルに最も近い難易度レベルを返す(song)];

						this.stバー情報[i].nクリア[d] = sr.GPInfo[ap].nClear;
						this.stバー情報[i].nスコアランク[d] = sr.GPInfo[ap].nScoreRank;
					}
				}

				this.stバー情報[i].csu = song.uniqueId;
				this.stバー情報[i].reference = song;

                #endregion



                for ( int j = 0; j < 3; j++ )
					this.stバー情報[ i ].nスキル値[ j ] = (int) song.arスコア[ this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す( song ) ].譜面情報.最大スキル[ j ];

                this.stバー情報[ i ].ttkタイトル = this.ttk曲名テクスチャを生成する( this.stバー情報[ i ].strタイトル文字列, this.stバー情報[i].ForeColor, this.stバー情報[i].BackColor, stバー情報[i].eバー種別 == Eバー種別.Box ? this.pfBoxName : this.pfMusicName);
			}

            int _center = (TJAPlayer3.Skin.SongSelect_Bar_Count - 1) / 2;
            this.n現在の選択行 = _center;
		}

		// Song type : 0 - Ensou, 1 - Dan, 2 - Tower
		private void tジャンル別選択されていない曲バーの描画(
			int x, 
			int y, 
			string strジャンル, 
			Eバー種別 eバー種別, 
			int[][] クリア, 
			int[][] スコアランク, 
			string boxType, 
			int _songType = 0, 
			CSongUniqueID csu = null,
            CSongListNode reference = null
            )
		{
			if (x >= SampleFramework.GameWindowSize.Width || y >= SampleFramework.GameWindowSize.Height)
				return;

			var rc = new Rectangle(0, 48, 128, 48);

			int opct = 255;

			if (TJAPlayer3.stageSongSelect.actDifficultySelectionScreen.bIsDifficltSelect && ctDifficultyIn.CurrentValue >= 1000)
				opct = Math.Max((int)255.0f - (ctDifficultyIn.CurrentValue - 1000), 0);

			TJAPlayer3.Tx.SongSelect_Crown.Opacity = opct;
			TJAPlayer3.Tx.SongSelect_ScoreRank.Opacity = opct;

			foreach(var tex in TJAPlayer3.Tx.SongSelect_Bar_Genre)
			{
                tex.Value.Opacity = opct;
			}
			foreach(var tex in TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlap)
			{
                tex.Value.Opacity = opct;
			}
				
			TJAPlayer3.Tx.SongSelect_Bar_Genre_Back.Opacity = opct;
			TJAPlayer3.Tx.SongSelect_Bar_Genre_Random.Opacity = opct;
			TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay.Opacity = opct;
			TJAPlayer3.Tx.SongSelect_Favorite.Opacity = opct; 
			TJAPlayer3.Tx.TowerResult_ScoreRankEffect.Opacity = opct;
			TJAPlayer3.Tx.DanResult_Rank.Opacity = opct;
			TJAPlayer3.Tx.SongSelect_Level_Number_Big?.tUpdateOpacity(opct);
            TJAPlayer3.Tx.SongSelect_Level_Number_Big_Colored?.tUpdateOpacity(opct);
            TJAPlayer3.Tx.SongSelect_Level_Number_Big_Icon?.tUpdateOpacity(opct);
            for (int i = 0; i < TJAPlayer3.Tx.SongSelect_Song_Panel.Length; i++)
			{
				TJAPlayer3.Tx.SongSelect_Song_Panel[i]?.tUpdateOpacity(opct);
			}
			TJAPlayer3.Tx.SongSelect_Bpm_Number?.tUpdateOpacity(opct);
			if (ttkSelectedSongMaker != null && TJAPlayer3.Skin.SongSelect_Maker_Show)
			{
				ResolveTitleTexture(ttkSelectedSongMaker)?.tUpdateOpacity(opct);
			}
            if (ttkSelectedSongBPM != null && TJAPlayer3.Skin.SongSelect_BPM_Text_Show)
            {
                ResolveTitleTexture(ttkSelectedSongBPM)?.tUpdateOpacity(opct);
            }
			TJAPlayer3.Tx.SongSelect_Explicit?.tUpdateOpacity(opct);
			TJAPlayer3.Tx.SongSelect_Movie?.tUpdateOpacity(opct);


            if (eバー種別 == Eバー種別.Random)
            {
				TJAPlayer3.Tx.SongSelect_Bar_Genre_Random?.t2D描画(x, y);
			}
			else if (eバー種別 != Eバー種別.BackBox)
			{
				HGenreBar.tGetGenreBar(boxType, TJAPlayer3.Tx.SongSelect_Bar_Genre)?.t2D描画(x, y);
				HGenreBar.tGetGenreBar(boxType, TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlap)?.t2D描画(x, y);

                if (TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay != null)
					TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay.t2D描画(x, y);
			}
			else
			{
				TJAPlayer3.Tx.SongSelect_Bar_Genre_Back?.t2D描画(x, y);
			}

			if (eバー種別 == Eバー種別.Score)
			{
				if (_songType == 1)
                {
					// displayDanStatus(x + 30, y + 30, Math.Min(クリア[0][0], 6) - 1, 0.2f);

					for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
					{
						if (i >= 2) continue;

						displayDanStatus(x + TJAPlayer3.Skin.SongSelect_DanStatus_Offset_X[i], y + TJAPlayer3.Skin.SongSelect_DanStatus_Offset_Y[i], Math.Min(クリア[i][0], 6) - 1, 0.2f);
					}
				}	
				else if (_songType == 2)
                {
					// displayTowerStatus(x + 30, y + 30, Math.Min(クリア[0][0], 6) - 1, 0.2f);

					for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
					{
						if (i >= 2) continue;

						displayTowerStatus(x + TJAPlayer3.Skin.SongSelect_TowerStatus_Offset_X[i], y + TJAPlayer3.Skin.SongSelect_TowerStatus_Offset_Y[i], Math.Min(クリア[i][0], 7) - 1, 0.3f);
					}
				}
				else
                {
					// var sr = this.r現在選択中の曲.arスコア[n現在のアンカ難易度レベルに最も近い難易度レベルを返す(this.r現在選択中の曲)];

					for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
					{
						if (i >= 2) continue;

						displayRegularCrowns(x + TJAPlayer3.Skin.SongSelect_RegularCrowns_Offset_X[i], y + TJAPlayer3.Skin.SongSelect_RegularCrowns_Offset_Y[i], クリア[i], スコアランク[i], 0.8f);
					}
				}

				displayFavoriteStatus(x + TJAPlayer3.Skin.SongSelect_FavoriteStatus_Offset[0], y + TJAPlayer3.Skin.SongSelect_FavoriteStatus_Offset[1], csu, 1f);
                tPrintLevelNumberBig(
                        x + TJAPlayer3.Skin.SongSelect_Level_Offset[0],
                        y + TJAPlayer3.Skin.SongSelect_Level_Offset[1],
                        reference
                        );
            }
		}

		public void displayTowerStatus(int x, int y, int grade, float _resize)
        {
			if (grade >= 0 && TJAPlayer3.Tx.TowerResult_ScoreRankEffect != null)
			{
				int scoreRankEffect_width = TJAPlayer3.Tx.TowerResult_ScoreRankEffect.szTextureSize.Width / 7;
				int scoreRankEffect_height = TJAPlayer3.Tx.TowerResult_ScoreRankEffect.szTextureSize.Height;

				TJAPlayer3.Tx.TowerResult_ScoreRankEffect.Opacity = 255;
				TJAPlayer3.Tx.TowerResult_ScoreRankEffect.vcScaleRatio.X = _resize;
				TJAPlayer3.Tx.TowerResult_ScoreRankEffect.vcScaleRatio.Y = _resize;
				TJAPlayer3.Tx.TowerResult_ScoreRankEffect.t2D拡大率考慮中央基準描画(x, y, new Rectangle(grade * scoreRankEffect_width, 0, scoreRankEffect_width, scoreRankEffect_height));
				TJAPlayer3.Tx.TowerResult_ScoreRankEffect.vcScaleRatio.X = 1f;
				TJAPlayer3.Tx.TowerResult_ScoreRankEffect.vcScaleRatio.Y = 1f;
			}
		}

		public void displayDanStatus(int x, int y, int grade, float _resize)
        {
			if (grade >= 0 && TJAPlayer3.Tx.DanResult_Rank != null)
			{
				int danResult_rank_width = TJAPlayer3.Tx.DanResult_Rank.szTextureSize.Width / 7;
				int danResult_rank_height = TJAPlayer3.Tx.DanResult_Rank.szTextureSize.Height;

				TJAPlayer3.Tx.DanResult_Rank.Opacity = 255;
				TJAPlayer3.Tx.DanResult_Rank.vcScaleRatio.X = _resize;
				TJAPlayer3.Tx.DanResult_Rank.vcScaleRatio.Y = _resize;
				TJAPlayer3.Tx.DanResult_Rank.t2D拡大率考慮中央基準描画(x, y, new Rectangle(danResult_rank_width * (grade + 1), 0, danResult_rank_width, danResult_rank_height));
				TJAPlayer3.Tx.DanResult_Rank.vcScaleRatio.X = 1f;
				TJAPlayer3.Tx.DanResult_Rank.vcScaleRatio.Y = 1f;
			}
		}

		public void displayRegularCrowns(int x, int y, int[] クリア, int[] スコアランク, float _resize)
        {
			// Don't display if one of the 2 textures is missing (to avoid crashes)
			if (TJAPlayer3.Tx.SongSelect_Crown == null || TJAPlayer3.Tx.SongSelect_ScoreRank == null)
				return;

			// To change to include all crowns/score ranks later

			TJAPlayer3.Tx.SongSelect_Crown.vcScaleRatio.X = _resize;
			TJAPlayer3.Tx.SongSelect_Crown.vcScaleRatio.Y = _resize;
			TJAPlayer3.Tx.SongSelect_ScoreRank.vcScaleRatio.X = _resize;
			TJAPlayer3.Tx.SongSelect_ScoreRank.vcScaleRatio.Y = _resize;

			int bestCrown = -1;
			int bestScoreRank = -1;

			for (int i = 0; i <= (int)Difficulty.Edit; i++)
            {
				if (クリア[i] > 0)
					bestCrown = i;
				if (スコアランク[i] > 0)
					bestScoreRank = i;
            }

			if (bestCrown >= 0)
			{
				float width = TJAPlayer3.Tx.SongSelect_Crown.szTextureSize.Width / 15.0f;
				int height = TJAPlayer3.Tx.SongSelect_Crown.szTextureSize.Height;
				TJAPlayer3.Tx.SongSelect_Crown?.t2D拡大率考慮中央基準描画(x + TJAPlayer3.Skin.SongSelect_RegularCrowns_ScoreRank_Offset_X[0], y + TJAPlayer3.Skin.SongSelect_RegularCrowns_ScoreRank_Offset_Y[0], 
					new RectangleF(12 * width + (クリア[bestCrown] - 1) * width, 0, width, height));
			}

			if (bestScoreRank >= 0)
			{
				int width = TJAPlayer3.Tx.SongSelect_ScoreRank.szTextureSize.Width;
				float height = TJAPlayer3.Tx.SongSelect_ScoreRank.szTextureSize.Height / 7.0f;
				TJAPlayer3.Tx.SongSelect_ScoreRank?.t2D拡大率考慮中央基準描画(x + TJAPlayer3.Skin.SongSelect_RegularCrowns_ScoreRank_Offset_X[1], y + TJAPlayer3.Skin.SongSelect_RegularCrowns_ScoreRank_Offset_Y[1],
					new RectangleF(0, (スコアランク[bestScoreRank] - 1) * height, width, height));
			}

			if (TJAPlayer3.Tx.Dani_Difficulty_Cymbol != null)
			{
				int dani_difficulty_cymbol_width = TJAPlayer3.Tx.Dani_Difficulty_Cymbol.szTextureSize.Width / 5;
				int dani_difficulty_cymbol_height = TJAPlayer3.Tx.Dani_Difficulty_Cymbol.szTextureSize.Height;

				TJAPlayer3.Tx.Dani_Difficulty_Cymbol.Opacity = TJAPlayer3.Tx.SongSelect_Favorite.Opacity;
				TJAPlayer3.Tx.Dani_Difficulty_Cymbol.vcScaleRatio.X = 0.5f;
				TJAPlayer3.Tx.Dani_Difficulty_Cymbol.vcScaleRatio.Y = 0.5f;

				if (bestCrown >= 0)
				{
					TJAPlayer3.Tx.Dani_Difficulty_Cymbol.t2D中心基準描画(
										x + TJAPlayer3.Skin.SongSelect_RegularCrowns_Difficulty_Cymbol_Offset_X[0],
										y + TJAPlayer3.Skin.SongSelect_RegularCrowns_Difficulty_Cymbol_Offset_Y[0],
										new Rectangle(bestCrown * dani_difficulty_cymbol_width, 0, dani_difficulty_cymbol_width, dani_difficulty_cymbol_height));
				}

				if (bestScoreRank >= 0)
				{
					TJAPlayer3.Tx.Dani_Difficulty_Cymbol.t2D中心基準描画(
										x + TJAPlayer3.Skin.SongSelect_RegularCrowns_Difficulty_Cymbol_Offset_X[1],
										y + TJAPlayer3.Skin.SongSelect_RegularCrowns_Difficulty_Cymbol_Offset_Y[1],
										new Rectangle(bestScoreRank * dani_difficulty_cymbol_width, 0, dani_difficulty_cymbol_width, dani_difficulty_cymbol_height));
				}

				TJAPlayer3.Tx.Dani_Difficulty_Cymbol.Opacity = 255;
				TJAPlayer3.Tx.Dani_Difficulty_Cymbol.vcScaleRatio.X = 1f;
				TJAPlayer3.Tx.Dani_Difficulty_Cymbol.vcScaleRatio.Y = 1f;
			}


		}

		public void displayFavoriteStatus(int x, int y, CSongUniqueID csu, float _resize)
        {
			if (csu != null
				&& TJAPlayer3.Tx.SongSelect_Favorite != null
				&& TJAPlayer3.Favorites.tIsFavorite(csu.data.id))
            {
				TJAPlayer3.Tx.SongSelect_Favorite.vcScaleRatio.X = _resize;
				TJAPlayer3.Tx.SongSelect_Favorite.vcScaleRatio.Y = _resize;
				TJAPlayer3.Tx.SongSelect_Favorite.t2D拡大率考慮中央基準描画(x, y);
			}
        }

		public int nStrジャンルtoNum(string strジャンル)
		{
			return this.nStrジャンルtoNumBox(strジャンル);
		}
		
		public int nStrジャンルtoNumBox(string strジャンル)
		{
			switch (strジャンル)
			{
				case "ポップス":
				case "J-POP":
				case "POPS":
				case "JPOP":
					return 1;
				case "アニメ":
					return 2;
				case "ボーカロイド":
				case "VOCALOID":
					return 8;
				case "キッズ":
				case "どうよう":
					return 7;
				case "バラエティ":
					return 6;
				case "クラシック":
					return 5;
				case "ゲームバラエティ":
				case "ゲームミュージック":
					return 3;
				case "ナムコオリジナル":
					return 4;
				case "最近遊んだ曲":
					return 9;
				default:
					return 0;
			}
		}

		private TitleTextureKey ttk曲名テクスチャを生成する( string str文字, Color forecolor, Color backcolor, CCachedFontRenderer pf)
        {
            return new TitleTextureKey(str文字, pf, forecolor, backcolor, TJAPlayer3.Skin.SongSelect_Title_MaxSize);
        }

	    private TitleTextureKey ttkサブタイトルテクスチャを生成する( string str文字, Color forecolor, Color backcolor)
        {
            return new TitleTextureKey(str文字, pfSubtitle, forecolor, backcolor, TJAPlayer3.Skin.SongSelect_SubTitle_MaxSize);
		}

		private TitleTextureKey ttkGenerateMakerTexture(string str文字, Color forecolor, Color backcolor)
		{
			return new TitleTextureKey("Maker: " + str文字, pfMaker, forecolor, backcolor, TJAPlayer3.Skin.SongSelect_Maker_MaxSize);
		}

        private TitleTextureKey ttkGenerateBPMTexture(CSongListNode node, Color forecolor, Color backcolor)
        {
			var _score = node.arスコア[tFetchDifficulty(node)].譜面情報;
			var _speed = TJAPlayer3.ConfigIni.SongPlaybackSpeed;

            double[] bpms = new double[3] {
				_score.BaseBpm * _speed,
				_score.MinBpm * _speed,
				_score.MaxBpm * _speed
            };

			string bpm_str = "BPM: " + bpms[0].ToString();
			if (bpms[1] != bpms[0] || bpms[2] != bpms[0])
				bpm_str += " (" + bpms[1].ToString() + "-" + bpms[2].ToString() + ")";

			var _color = forecolor;
			if (_speed > 1)
				_color = Color.Red;
			else if (_speed < 1)
				_color = Color.LightBlue;

            return new TitleTextureKey(bpm_str, pfBPM, _color, backcolor, TJAPlayer3.Skin.SongSelect_BPM_Text_MaxSize);
        }

        public CTexture ResolveTitleTexture(TitleTextureKey titleTextureKey)
	    {
			if (!_titledictionary.TryGetValue(titleTextureKey, out var texture))
			{
				texture = GenerateTitleTexture(titleTextureKey);
				_titledictionary.Add(titleTextureKey, texture);
			}

			return texture;
		}

		public CTexture ResolveTitleTexture(TitleTextureKey titleTextureKey, bool bVertical, bool keepCenter = false)
		{
			if (!_titledictionary.TryGetValue(titleTextureKey, out var texture))
			{
				if (bVertical)
					texture = GenerateTitleTextureTate(titleTextureKey, keepCenter);
				else
					texture = GenerateTitleTexture(titleTextureKey, keepCenter);
				_titledictionary.Add(titleTextureKey, texture);
			}

			return texture;
		}

		public CTexture ResolveTitleTextureTate(TitleTextureKey titleTextureKey)
		{
			if (!_titledictionary.TryGetValue(titleTextureKey, out var texture))
			{
				texture = GenerateTitleTextureTate(titleTextureKey);
				_titledictionary.Add(titleTextureKey, texture);
			}

			return texture;
		}

		private static CTexture GenerateTitleTextureTate(TitleTextureKey titleTextureKey, bool keepCenter = false)
		{
			using (var bmp = titleTextureKey.cPrivateFastFont.DrawText_V(
				titleTextureKey.str文字, titleTextureKey.forecolor, titleTextureKey.backcolor, titleTextureKey.secondEdge, 30, keepCenter))
			{
				CTexture tx文字テクスチャ = TJAPlayer3.tテクスチャの生成(bmp, false);
				if (tx文字テクスチャ.szTextureSize.Height > titleTextureKey.maxWidth)
				{
					//tx文字テクスチャ.vc拡大縮小倍率.X = (float)(((double)titleTextureKey.maxWidth) / tx文字テクスチャ.szテクスチャサイズ.Height);
					tx文字テクスチャ.vcScaleRatio.X = 1.0f;
					tx文字テクスチャ.vcScaleRatio.Y = (float)(((double)titleTextureKey.maxWidth) / tx文字テクスチャ.szTextureSize.Height);
				}

				return tx文字テクスチャ;
			}
		}


		private static CTexture GenerateTitleTexture(TitleTextureKey titleTextureKey, bool keepCenter = false)
	    {
			using (var bmp = titleTextureKey.cPrivateFastFont.DrawText(
	            titleTextureKey.str文字, titleTextureKey.forecolor, titleTextureKey.backcolor, titleTextureKey.secondEdge, 30, keepCenter))
	        {
	            CTexture tx文字テクスチャ = TJAPlayer3.tテクスチャの生成(bmp, false);
	            if (tx文字テクスチャ.szTextureSize.Width > titleTextureKey.maxWidth)
	            {
	                tx文字テクスチャ.vcScaleRatio.X = (float) (((double) titleTextureKey.maxWidth) / tx文字テクスチャ.szTextureSize.Width);
					tx文字テクスチャ.vcScaleRatio.Y = 1.0f;// (float) (((double) titleTextureKey.maxWidth) / tx文字テクスチャ.szテクスチャサイズ.Width);

	            }

	            return tx文字テクスチャ;
	        }
	    }

	    private void ClearTitleTextureCache()
	    {
			foreach (var titleTexture in _titledictionary.Values)
			{
				titleTexture.Dispose();
	        }

			_titledictionary.Clear();
		}

		public sealed class TitleTextureKey
	    {
	        public readonly string str文字;
	        public readonly CCachedFontRenderer cPrivateFastFont;
	        public readonly Color forecolor;
	        public readonly Color backcolor;
	        public readonly int maxWidth;
			public readonly Color? secondEdge;

	        public TitleTextureKey(string str文字, CCachedFontRenderer cPrivateFastFont, Color forecolor, Color backcolor, int maxHeight, Color? secondEdge = null)
	        {
	            this.str文字 = str文字;
	            this.cPrivateFastFont = cPrivateFastFont;
	            this.forecolor = forecolor;
	            this.backcolor = backcolor;
	            this.maxWidth = maxHeight;
				this.secondEdge = secondEdge;
	        }

	        private bool Equals(TitleTextureKey other)
	        {
	            return string.Equals(str文字, other.str文字) &&
	                   cPrivateFastFont.Equals(other.cPrivateFastFont) &&
	                   forecolor.Equals(other.forecolor) &&
	                   backcolor.Equals(other.backcolor) &&
					   secondEdge.Equals(other.secondEdge) &&
	                   maxWidth == other.maxWidth;
	        }

	        public override bool Equals(object obj)
	        {
	            if (ReferenceEquals(null, obj)) return false;
	            if (ReferenceEquals(this, obj)) return true;
	            return obj is TitleTextureKey other && Equals(other);
	        }

	        public override int GetHashCode()
	        {
	            unchecked
	            {
	                var hashCode = str文字.GetHashCode();
	                hashCode = (hashCode * 397) ^ cPrivateFastFont.GetHashCode();
	                hashCode = (hashCode * 397) ^ forecolor.GetHashCode();
	                hashCode = (hashCode * 397) ^ backcolor.GetHashCode();
	                hashCode = (hashCode * 397) ^ maxWidth;
					if (secondEdge != null)
						hashCode = (hashCode * 397) ^ secondEdge.GetHashCode();
	                return hashCode;
	            }
	        }

	        public static bool operator ==(TitleTextureKey left, TitleTextureKey right)
	        {
	            return Equals(left, right);
	        }

	        public static bool operator !=(TitleTextureKey left, TitleTextureKey right)
	        {
	            return !Equals(left, right);
	        }
	    }

		private void tアイテム数の描画()
		{
			string s = nCurrentPosition.ToString() + "/" + nNumOfItems.ToString();
			int x = 639 - 8 - 12;
			int y = 362;

			for ( int p = s.Length - 1; p >= 0; p-- )
			{
				tアイテム数の描画_１桁描画( x, y, s[ p ] );
				x -= 8;
			}
		}
		private void tアイテム数の描画_１桁描画( int x, int y, char s数値 )
		{
			int dx, dy;
			if ( s数値 == '/' )
			{
				dx = 48;
				dy = 0;
			}
			else
			{
				int n = (int) s数値 - (int) '0';
				dx = ( n % 6 ) * 8;
				dy = ( n / 6 ) * 12;
			}
			//if ( this.txアイテム数数字 != null )
			//{
			//	this.txアイテム数数字.t2D描画( CDTXMania.app.Device, x, y, new Rectangle( dx, dy, 8, 12 ) );
			//}
		}


        //数字フォント
        private CTexture txレベル数字フォント;
        [StructLayout( LayoutKind.Sequential )]
        private struct STレベル数字
        {
            public char ch;
            public int ptX;
        }
        private STレベル数字[] st小文字位置 = new STレベル数字[ 10 ];

		private void tResetTitleTextureKey()
		{
            if (this.ttk選択している曲の曲名 != null)
            {
                this.ttk選択している曲の曲名 = null;
                this.b選択曲が変更された = false;
            }
            if (this.ttk選択している曲のサブタイトル != null)
            {
                this.ttk選択している曲のサブタイトル = null;
                this.b選択曲が変更された = false;
            }
            if (this.ttkSelectedSongMaker != null)
            {
                this.ttkSelectedSongMaker = null;
                this.b選択曲が変更された = false;
            }
            if (this.ttkSelectedSongBPM != null)
            {
                this.ttkSelectedSongBPM = null;
                this.b選択曲が変更された = false;
            }
        }

		public void tDisplayLevelIcon(int x, int y, CDTX.ELevelIcon icon, CTexture iconTex = null)
		{
			var _tex = (iconTex != null) ? iconTex : TJAPlayer3.Tx.SongSelect_Level_Number_Big_Icon;
            if (icon != CDTX.ELevelIcon.eNone &&
                _tex != null)
            {
                var __width = _tex.sz画像サイズ.Width / 3;
                var __height = _tex.sz画像サイズ.Height;
                _tex.t2D_DisplayImage_AnchorUpRight(
                                
                                x,
                                y,
                                new Rectangle(__width * (int)icon, 0, __width, __height)
                                );
            }
        }

		private void t小文字表示(int x, int y, int num, int diff, CDTX.ELevelIcon icon)
		{
			int[] nums = CConversion.SeparateDigits(num);
            float[] icon_coords = new float[2] { -999, -999 };
            for (int j = 0; j < nums.Length; j++)
			{
				float offset = j - (nums.Length / 2.0f);
				float _x = x - (TJAPlayer3.Skin.SongSelect_Level_Number_Interval[0] * offset);
				float _y = y - (TJAPlayer3.Skin.SongSelect_Level_Number_Interval[1] * offset);

				float width = TJAPlayer3.Tx.SongSelect_Level_Number.sz画像サイズ.Width / 10.0f;
				float height = TJAPlayer3.Tx.SongSelect_Level_Number.sz画像サイズ.Height;

                var _expand_ratio = 1.0f / (1.0f + (0.25f * (nums.Length - 1)));
                TJAPlayer3.Tx.SongSelect_Level_Number.vcScaleRatio.X = _expand_ratio;

                icon_coords[0] = Math.Max(icon_coords[0], _x + width * _expand_ratio);
                icon_coords[1] = _y;

                TJAPlayer3.Tx.SongSelect_Level_Number.t2D描画(_x, _y, new RectangleF(width * nums[j], 0, width, height));

                if (TJAPlayer3.Tx.SongSelect_Level_Number_Colored != null)
                {
                    TJAPlayer3.Tx.SongSelect_Level_Number_Colored.vcScaleRatio.X = _expand_ratio;
                    TJAPlayer3.Tx.SongSelect_Level_Number_Colored.color4 = CConversion.ColorToColor4(TJAPlayer3.Skin.SongSelect_Difficulty_Colors[diff]);
                    TJAPlayer3.Tx.SongSelect_Level_Number_Colored.t2D描画(_x, _y, new RectangleF(width * nums[j], 0, width, height));
                }
            }
			tDisplayLevelIcon((int)icon_coords[0], (int)icon_coords[1], icon, TJAPlayer3.Tx.SongSelect_Level_Number_Icon);
        }

        public void tPrintLevelNumberBig(int x, int y, CSongListNode song)
        {
			if (song == null) return;
			int difficulty = tFetchDifficulty(song);
			int num = song.nLevel[difficulty];
			var icon = song.nLevelIcon[difficulty];

			if (TJAPlayer3.Tx.SongSelect_Level_Number_Big == null || num < 0) return;
            int[] nums = CConversion.SeparateDigits(num);
			float _ratio = 1f;
			float[] icon_coords = new float[2] { -999, -999 };
            if (TJAPlayer3.Tx.SongSelect_Level_Number != null)
			{
				_ratio = TJAPlayer3.Tx.SongSelect_Level_Number_Big.szTextureSize.Width / TJAPlayer3.Tx.SongSelect_Level_Number.szTextureSize.Width;
            }
            for (int j = 0; j < nums.Length; j++)
            {
                float offset = j - (nums.Length / 2.0f);
                float _x = x - (TJAPlayer3.Skin.SongSelect_Level_Number_Interval[0] * offset * _ratio);
                float _y = y - (TJAPlayer3.Skin.SongSelect_Level_Number_Interval[1] * offset);

                float width = TJAPlayer3.Tx.SongSelect_Level_Number_Big.sz画像サイズ.Width / 10.0f;
                float height = TJAPlayer3.Tx.SongSelect_Level_Number_Big.sz画像サイズ.Height;

				var _expand_ratio = 1.0f / (1.0f + (0.25f * (nums.Length - 1)));
				TJAPlayer3.Tx.SongSelect_Level_Number_Big.vcScaleRatio.X = _expand_ratio;
                TJAPlayer3.Tx.SongSelect_Level_Number_Big.t2D描画(_x, _y, new RectangleF(width * nums[j], 0, width, height));

                icon_coords[0] = Math.Max(icon_coords[0], _x + width * _expand_ratio);
                icon_coords[1] = _y;

                if (TJAPlayer3.Tx.SongSelect_Level_Number_Big_Colored != null)
				{
                    TJAPlayer3.Tx.SongSelect_Level_Number_Big_Colored.vcScaleRatio.X = _expand_ratio;
                    TJAPlayer3.Tx.SongSelect_Level_Number_Big_Colored.color4 = CConversion.ColorToColor4(TJAPlayer3.Skin.SongSelect_Difficulty_Colors[difficulty]);
                    TJAPlayer3.Tx.SongSelect_Level_Number_Big_Colored.t2D描画(_x, _y, new RectangleF(width * nums[j], 0, width, height));
                }
                
            }
            tDisplayLevelIcon((int)icon_coords[0], (int)icon_coords[1], icon);

        }

		public int tFetchDifficulty(CSongListNode song)
		{
            var closest = this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(song);
            int defaultTable = Math.Max(0, Math.Min((int)Difficulty.Edit + 1, TJAPlayer3.ConfigIni.nDefaultCourse));

			if (song.arスコア[defaultTable] == null)
				return closest;
			return defaultTable;
        }

        //-----------------
        #endregion
    }

	public enum eMenuContext
    {
		NONE,
		SearchByDifficulty,
		Random,
    }

	public enum eLayoutType
    {
		DiagonalUpDown,
		Vertical,
		DiagonalDownUp,
		HalfCircleRight,
		HalfCircleLeft,
		TOTAL
    }
}
