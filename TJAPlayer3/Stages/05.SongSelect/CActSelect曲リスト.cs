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
using SlimDX;
using FDK;
using System.Linq;

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
				if( this.n目標のスクロールカウンタ == 0 )
				{
					return ( this.n現在のスクロールカウンタ != 0 );
				}
				return true;
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
				return this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す( this.r現在選択中の曲 );
			}
		}
		public Cスコア r現在選択中のスコア
		{
			get
			{
				if( this.r現在選択中の曲 != null )
				{
					return this.r現在選択中の曲.arスコア[ this.n現在選択中の曲の現在の難易度レベル ];
				}
				return null;
			}
		}
		public C曲リストノード r現在選択中の曲 
		{
			get;
			private set;
		}

		public void ResetSongIndex()
        {
			this.r現在選択中の曲 = TJAPlayer3.Songs管理.list曲ルート[0];
		}

		public int nスクロールバー相対y座標
		{
			get;
			private set;
		}

		// t選択曲が変更された()内で使う、直前の選曲の保持
		// (前と同じ曲なら選択曲変更に掛かる再計算を省略して高速化するため)
		private C曲リストノード song_last = null;

		
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

			this.r現在選択中の曲 = null;
            this.n現在のアンカ難易度レベル = TJAPlayer3.ConfigIni.nDefaultCourse;
			base.b活性化してない = true;
			this.bIsEnumeratingSongs = false;
		}


		// メソッド

		// Closest level
		public int n現在のアンカ難易度レベルに最も近い難易度レベルを返す( C曲リストノード song )
		{
			// 事前チェック。

			if( song == null )
				return this.n現在のアンカ難易度レベル;	// 曲がまったくないよ

			if( song.arスコア[ this.n現在のアンカ難易度レベル ] != null )
				return this.n現在のアンカ難易度レベル;	// 難易度ぴったりの曲があったよ

			if( ( song.eノード種別 == C曲リストノード.Eノード種別.BOX ) || ( song.eノード種別 == C曲リストノード.Eノード種別.BACKBOX ) )
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
		public C曲リストノード r指定された曲が存在するリストの先頭の曲( C曲リストノード song )
		{
			List<C曲リストノード> songList = GetSongListWithinMe( song );
			return ( songList == null ) ? null : songList[ 0 ];
		}
		public C曲リストノード r指定された曲が存在するリストの末尾の曲( C曲リストノード song )
		{
			List<C曲リストノード> songList = GetSongListWithinMe( song );
			return ( songList == null ) ? null : songList[ songList.Count - 1 ];
		}

		private List<C曲リストノード> GetSongListWithinMe( C曲リストノード song )
		{
			if ( song.r親ノード == null )					// root階層のノートだったら
			{
				return TJAPlayer3.Songs管理.list曲ルート;	// rootのリストを返す
			}
			else
			{
				if ( ( song.r親ノード.list子リスト != null ) && ( song.r親ノード.list子リスト.Count > 0 ) )
				{
					return song.r親ノード.list子リスト;
				}
				else
				{
					return null;
				}
			}
		}


		public delegate void DGSortFunc( List<C曲リストノード> songList, E楽器パート eInst, int order, params object[] p);
		/// <summary>
		/// 主にCSong管理.cs内にあるソート機能を、delegateで呼び出す。
		/// </summary>
		/// <param name="sf">ソート用に呼び出すメソッド</param>
		/// <param name="eInst">ソート基準とする楽器</param>
		/// <param name="order">-1=降順, 1=昇順</param>
		public void t曲リストのソート( DGSortFunc sf, E楽器パート eInst, int order, params object[] p )
		{
			List<C曲リストノード> songList = GetSongListWithinMe( this.r現在選択中の曲 );
			if ( songList == null )
			{
				// 何もしない;
			}
			else
			{
//				CDTXMania.Songs管理.t曲リストのソート3_演奏回数の多い順( songList, eInst, order );
				sf( songList, eInst, order, p );
//				this.r現在選択中の曲 = CDTXMania
				this.t現在選択中の曲を元に曲バーを再構成する();
			}
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
			if (CSkin.GetSkinName(TJAPlayer3.Skin.GetCurrentSkinSubfolderFullName(false)) != CSkin.GetSkinName(this.r現在選択中の曲.strSkinPath)
				&& CSkin.bUseBoxDefSkin)
			{
				ret = true;
				// BOXに入るときは、スキン変更発生時のみboxdefスキン設定の更新を行う
				TJAPlayer3.Skin.SetCurrentSkinSubfolderFullName(
					TJAPlayer3.Skin.GetSkinSubfolderFullNameFromSkinName(CSkin.GetSkinName(this.r現在選択中の曲.strSkinPath)), false);
			}

			//Trace.TraceInformation( "Skin変更: " + CSkin.GetSkinName( CDTXMania.Skin.GetCurrentSkinSubfolderFullName(false) ) );
			//Trace.TraceInformation( "Skin変更Current : "+  CDTXMania.Skin.GetCurrentSkinSubfolderFullName(false) );
			//Trace.TraceInformation( "Skin変更System  : "+  CSkin.strSystemSkinSubfolderFullName );
			//Trace.TraceInformation( "Skin変更BoxDef  : "+  CSkin.strBoxDefSkinSubfolderFullName );
			if(r現在選択中の曲.list子リスト.Count != 1)
			{
				List<C曲リストノード> list = TJAPlayer3.Songs管理.list曲ルート;
				list.InsertRange(list.IndexOf(this.r現在選択中の曲) + 1, this.r現在選択中の曲.list子リスト);
				int n回数 = this.r現在選択中の曲.Openindex;
				for (int index = 0; index <= n回数; index++)
					this.r現在選択中の曲 = this.r次の曲(this.r現在選択中の曲);
				list.RemoveAt(list.IndexOf(this.r現在選択中の曲.r親ノード));
				this.t現在選択中の曲を元に曲バーを再構成する();
				this.t選択曲が変更された(false);
				TJAPlayer3.stage選曲.t選択曲変更通知();                          // #27648 項目数変更を反映させる
				this.b選択曲が変更された = true;
				// TJAPlayer3.Skin.bgm選曲画面.t停止する();
				CSongSelectSongManager.stopSong();
			}
			return ret;
		}
		public bool tBOXを出る()
		{
//Trace.TraceInformation( "box exit" );
//Trace.TraceInformation( "Skin現在Current : " + CDTXMania.Skin.GetCurrentSkinSubfolderFullName(false) );
//Trace.TraceInformation( "Skin現在System  : " + CSkin.strSystemSkinSubfolderFullName );
//Trace.TraceInformation( "Skin現在BoxDef  : " + CSkin.strBoxDefSkinSubfolderFullName );
//Trace.TraceInformation( "Skin現在: " + CSkin.GetSkinName( CDTXMania.Skin.GetCurrentSkinSubfolderFullName(false) ) );
//Trace.TraceInformation( "Skin現pt: " + CDTXMania.Skin.GetCurrentSkinSubfolderFullName(false) );
//Trace.TraceInformation( "Skin指定: " + CSkin.GetSkinName( this.r現在選択中の曲.strSkinPath ) );
//Trace.TraceInformation( "Skinpath: " + this.r現在選択中の曲.strSkinPath );
			bool ret = false;
			if ( CSkin.GetSkinName( TJAPlayer3.Skin.GetCurrentSkinSubfolderFullName( false ) ) != CSkin.GetSkinName( this.r現在選択中の曲.strSkinPath )
				&& CSkin.bUseBoxDefSkin )
			{
				ret = true;
			}
			// スキン変更が発生しなくても、boxdef圏外に出る場合は、boxdefスキン設定の更新が必要
			// (ユーザーがboxdefスキンをConfig指定している場合への対応のために必要)
			// tBoxに入る()とは処理が微妙に異なるので注意
			TJAPlayer3.Skin.SetCurrentSkinSubfolderFullName(
				( this.r現在選択中の曲.strSkinPath == "" ) ? "" : TJAPlayer3.Skin.GetSkinSubfolderFullNameFromSkinName( CSkin.GetSkinName( this.r現在選択中の曲.strSkinPath ) ), false );
			//Trace.TraceInformation( "SKIN変更: " + CSkin.GetSkinName( CDTXMania.Skin.GetCurrentSkinSubfolderFullName(false) ) );
			//Trace.TraceInformation( "SKIN変更Current : "+  CDTXMania.Skin.GetCurrentSkinSubfolderFullName(false) );
			//Trace.TraceInformation( "SKIN変更System  : "+  CSkin.strSystemSkinSubfolderFullName );
			//Trace.TraceInformation( "SKIN変更BoxDef  : "+  CSkin.strBoxDefSkinSubfolderFullName );

			List<C曲リストノード> list = TJAPlayer3.Songs管理.list曲ルート;
			list.Insert(list.IndexOf(this.r現在選択中の曲) + 1, this.r現在選択中の曲.r親ノード);
			this.r現在選択中の曲.r親ノード.Openindex = r現在選択中の曲.r親ノード.list子リスト.IndexOf(this.r現在選択中の曲);
			this.r現在選択中の曲 = this.r次の曲(r現在選択中の曲);
			for (int index = 0; index < list.Count; index++)
			{
				if (this.r現在選択中の曲.list子リスト.Contains(list[index]))
				{
					list.RemoveAt(index);
					index--;
				}
			}
			this.t現在選択中の曲を元に曲バーを再構成する();
			this.t選択曲が変更された(false);                                 // #27648 項目数変更を反映させる
			this.b選択曲が変更された = true;

			return ret;
		}
		public void t現在選択中の曲を元に曲バーを再構成する()
		{
			this.tバーの初期化();
		}
		public void t次に移動()
		{
			if( this.r現在選択中の曲 != null )
			{
				ctScoreFrameAnime.t停止();
				ctScoreFrameAnime.n現在の値 = 0;
				ctBarOpen.t停止();
				ctBarOpen.n現在の値 = 0;
				this.n目標のスクロールカウンタ += 100;
			}
			this.b選択曲が変更された = true;
		}
		public void t前に移動()
		{
			if( this.r現在選択中の曲 != null )
			{
				ctScoreFrameAnime.t停止();
				ctScoreFrameAnime.n現在の値 = 0;
				ctBarOpen.t停止();
				ctBarOpen.n現在の値 = 0;
				this.n目標のスクロールカウンタ -= 100;
			}
			this.b選択曲が変更された = true;
		}
		public void t難易度レベルをひとつ進める()
		{
			if( ( this.r現在選択中の曲 == null ) || ( this.r現在選択中の曲.nスコア数 <= 1 ) )
				return;		// 曲にスコアが０～１個しかないなら進める意味なし。
			

			// 難易度レベルを＋１し、現在選曲中のスコアを変更する。

			this.n現在のアンカ難易度レベル = this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す( this.r現在選択中の曲 );

			for( int i = 0; i < (int)Difficulty.Total; i++ )
			{
				this.n現在のアンカ難易度レベル = ( this.n現在のアンカ難易度レベル + 1 ) % (int)Difficulty.Total;	// ５以上になったら０に戻る。
				if( this.r現在選択中の曲.arスコア[ this.n現在のアンカ難易度レベル ] != null )	// 曲が存在してるならここで終了。存在してないなら次のレベルへGo。
					break;
			}


			// 曲毎に表示しているスキル値を、新しい難易度レベルに合わせて取得し直す。（表示されている13曲全部。）

			C曲リストノード song = this.r現在選択中の曲;
			for( int i = 0; i < 4; i++ )
				song = this.r前の曲( song );

			for( int i = this.n現在の選択行 - 4; i < ( ( this.n現在の選択行 - 4 ) + 9 ); i++ )
			{
				int index = ( i + 9 ) % 9;
				for( int m = 0; m < 3; m++ )
				{
					this.stバー情報[ index ].nスキル値[ m ] = (int) song.arスコア[ this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す( song ) ].譜面情報.最大スキル[ m ];
				}
				song = this.r次の曲( song );
			}


			// 選曲ステージに変更通知を発出し、関係Activityの対応を行ってもらう。

			TJAPlayer3.stage選曲.t選択曲変更通知();
		}
        /// <summary>
        /// 不便だったから作った。
        /// </summary>
		public void t難易度レベルをひとつ戻す()
		{
			if( ( this.r現在選択中の曲 == null ) || ( this.r現在選択中の曲.nスコア数 <= 1 ) )
				return;		// 曲にスコアが０～１個しかないなら進める意味なし。
			

			// 難易度レベルを＋１し、現在選曲中のスコアを変更する。

			this.n現在のアンカ難易度レベル = this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す( this.r現在選択中の曲 );

            this.n現在のアンカ難易度レベル--;
            if( this.n現在のアンカ難易度レベル < 0 ) // 0より下になったら4に戻す。
            {
                this.n現在のアンカ難易度レベル = 4;
            }

            //2016.08.13 kairera0467 かんたん譜面が無い譜面(ふつう、むずかしいのみ)で、難易度を最上位に戻せない不具合の修正。
            bool bLabel0NotFound = true;
            for( int i = this.n現在のアンカ難易度レベル; i >= 0; i-- )
            {
                if( this.r現在選択中の曲.arスコア[ i ] != null )
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
                    if( this.r現在選択中の曲.arスコア[ i ] != null )
                    {
                        this.n現在のアンカ難易度レベル = i;
                        break;
                    }
                }
            }

			// 曲毎に表示しているスキル値を、新しい難易度レベルに合わせて取得し直す。（表示されている13曲全部。）

			C曲リストノード song = this.r現在選択中の曲;
			for( int i = 0; i < 4; i++ )
				song = this.r前の曲( song );

			for( int i = this.n現在の選択行 - 4; i < ( ( this.n現在の選択行 - 4 ) + 9 ); i++ )
			{
				int index = ( i + 9 ) % 9;
				for( int m = 0; m < 3; m++ )
				{
					this.stバー情報[ index ].nスキル値[ m ] = (int) song.arスコア[ this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す( song ) ].譜面情報.最大スキル[ m ];
				}
				song = this.r次の曲( song );
			}


			// 選曲ステージに変更通知を発出し、関係Activityの対応を行ってもらう。

			TJAPlayer3.stage選曲.t選択曲変更通知();
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

				if ( this.r現在選択中の曲 != null )			// r現在選択中の曲==null とは、「最初songlist.dbが無かった or 検索したが1曲もない」
				{
					this.r現在選択中の曲 = searchCurrentBreadcrumbsPosition( TJAPlayer3.Songs管理.list曲ルート, this.r現在選択中の曲.strBreadcrumbs );
					if ( bRemakeSongTitleBar )					// 選曲画面以外に居るときには再構成しない (非活性化しているときに実行すると例外となる)
					{
						this.t現在選択中の曲を元に曲バーを再構成する();
					}
					return;
				}
			}
			if (this.b活性化してる)
			{
				this.On非活性化();
				this.r現在選択中の曲 = null;
				this.On活性化();
			}
		}


		/// <summary>
		/// 現在選曲している位置を検索する
		/// (曲一覧クラスを新しいものに入れ替える際に用いる)
		/// </summary>
		/// <param name="ln">検索対象のList</param>
		/// <param name="bc">検索するパンくずリスト(文字列)</param>
		/// <returns></returns>
		private C曲リストノード searchCurrentBreadcrumbsPosition( List<C曲リストノード> ln, string bc )
		{
			foreach (C曲リストノード n in ln)
			{
				if ( n.strBreadcrumbs == bc )
				{
					return n;
				}
				else if ( n.list子リスト != null && n.list子リスト.Count > 0 )	// 子リストが存在するなら、再帰で探す
				{
					C曲リストノード r = searchCurrentBreadcrumbsPosition( n.list子リスト, bc );
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
			C曲リストノード song = TJAPlayer3.stage選曲.r現在選択中の曲;
			if ( song == null )
				return;
			if ( song == song_last && bForce == false )
				return;
				
			song_last = song;
			List<C曲リストノード> list =TJAPlayer3.Songs管理.list曲ルート;
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
            TJAPlayer3.stage選曲.act演奏履歴パネル.tSongChange();
		}

		// CActivity 実装

		async public void tLoadPads()
        {
			while (bIsEnumeratingSongs)
            {
				await Task.Delay(100);
			}

			#region [Reset nodes]

			for (int pl = 0; pl < 2; pl++)
            {
				CScorePad[] SPArrRef = ScorePads;
				if (pl == 1)
					SPArrRef = ScorePads2;

				for (int s = 0; s <= (int)Difficulty.Edit + 1; s++)
				{
					CScorePad SPRef = SPArrRef[s];

					for (int i = 0; i < SPRef.ScoreRankCount.Length; i++)
						SPRef.ScoreRankCount[i] = 0;

					for (int i = 0; i < ScorePads[s].CrownCount.Length; i++)
						SPRef.CrownCount[i] = 0;
				}
			}

            #endregion

            #region [Load notes]

            foreach (var song in TJAPlayer3.Songs管理.list曲ルート)
			{
				for (int pl = 0; pl < 2; pl++)
                {
					CScorePad[] SPArrRef = ScorePads;
					if (pl == 1)
						SPArrRef = ScorePads2;
					
					// All score pads except UraOmote
					// this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(a)
					for (int s = 0; s <= (int)Difficulty.Edit; s++)
					{
						CScorePad SPRef = SPArrRef[s];

						if (song.eノード種別 == C曲リストノード.Eノード種別.BOX)
						{
							for (int i = 0; i < SPRef.ScoreRankCount.Length; i++)
							{
								SPRef.ScoreRankCount[i] += song.list子リスト.Where(a =>
									a.eノード種別 == C曲リストノード.Eノード種別.SCORE
									&& a.strジャンル != "最近遊んだ曲"
									&& a.arスコア[this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(a)] != null
									//&& a.arスコア[this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(a)].譜面情報.nスコアランク[s] == (i + 1)).Count();
									&& a.arスコア[this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(a)].GPInfo[pl].nScoreRank[s] == (i + 1)).Count();
							}
							for (int i = 0; i < SPRef.CrownCount.Length; i++)
							{
								SPRef.CrownCount[i] += song.list子リスト.Where(a =>
									a.eノード種別 == C曲リストノード.Eノード種別.SCORE
									&& a.strジャンル != "最近遊んだ曲"
									&& a.arスコア[this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(a)] != null
									&& a.arスコア[this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(a)].GPInfo[pl].nClear[s] == (i + 1)).Count();
							}
						}
						else
						{
							if (song.eノード種別 == C曲リストノード.Eノード種別.SCORE)
							{
								for (int i = 0; i < SPRef.ScoreRankCount.Length; i++)
								{
									SPRef.ScoreRankCount[i] += TJAPlayer3.Songs管理.list曲ルート.Where(a =>
										a.eノード種別 == C曲リストノード.Eノード種別.SCORE
										&& a.strジャンル != "最近遊んだ曲"
										&& a.arスコア[this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(a)] != null
										&& a.arスコア[this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(a)].GPInfo[pl].nScoreRank[s] == (i + 1)).Count();
								}
								for (int i = 0; i < SPRef.CrownCount.Length; i++)
								{
									SPRef.CrownCount[i] += TJAPlayer3.Songs管理.list曲ルート.Where(a =>
										a.eノード種別 == C曲リストノード.Eノード種別.SCORE
										&& a.strジャンル != "最近遊んだ曲"
										&& a.arスコア[this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(a)] != null
										&& a.arスコア[this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(a)].GPInfo[pl].nClear[s] == (i + 1)).Count();
								}
							}
						}
					}

				}

			#endregion

			#region [UraOmote pad]

			// UraOmote pad
			for (int i = 0; i < ScorePads[(int)Difficulty.Edit + 1].ScoreRankCount.Length; i++)
			{
				ScorePads[(int)Difficulty.Edit + 1].ScoreRankCount[i] = ScorePads[(int)Difficulty.Edit].ScoreRankCount[i] + ScorePads[(int)Difficulty.Oni].ScoreRankCount[i];
				ScorePads2[(int)Difficulty.Edit + 1].ScoreRankCount[i] = ScorePads2[(int)Difficulty.Edit].ScoreRankCount[i] + ScorePads2[(int)Difficulty.Oni].ScoreRankCount[i];
			}
			for (int i = 0; i < ScorePads[(int)Difficulty.Edit + 1].CrownCount.Length; i++)
			{
				ScorePads[(int)Difficulty.Edit + 1].CrownCount[i] = ScorePads[(int)Difficulty.Edit].CrownCount[i] + ScorePads[(int)Difficulty.Oni].CrownCount[i];
				ScorePads2[(int)Difficulty.Edit + 1].CrownCount[i] = ScorePads2[(int)Difficulty.Edit].CrownCount[i] + ScorePads2[(int)Difficulty.Oni].CrownCount[i];
			}

			#endregion

			}

		}

		public override void On活性化()
		{
			if( this.b活性化してる )
				return;

            if (!bFirstCrownLoad)
            {
				// tLoadPads();
				
				// Calculate Pads asynchonously
				new Task(tLoadPads).Start();
				
				bFirstCrownLoad = true;

			}

			TJAPlayer3.IsPerformingCalibration = false;

			TJAPlayer3.stage選曲.act難易度選択画面.bIsDifficltSelect = false;
			
            if (!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName))
            {
                this.pfBoxName = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 28);
                this.pfMusicName = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 22);
                this.pfSubtitle = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 13);
			}
            else
            {
                this.pfBoxName = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 28);
                this.pfMusicName = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 22);
                this.pfSubtitle = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 13);
			}

			if(!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.BoxFontName))
				this.pfBoxText = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.BoxFontName), 14);
			else
				this.pfBoxText = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 14);


			this.b登場アニメ全部完了 = false;
			this.n目標のスクロールカウンタ = 0;
			this.n現在のスクロールカウンタ = 0;
			this.nスクロールタイマ = -1;

			// フォント作成。
			// 曲リスト文字は２倍（面積４倍）でテクスチャに描画してから縮小表示するので、フォントサイズは２倍とする。

			FontStyle regular = FontStyle.Regular;
			this.ft曲リスト用フォント = new Font( TJAPlayer3.ConfigIni.FontName, 40f, regular, GraphicsUnit.Pixel );
			
			// 現在選択中の曲がない（＝はじめての活性化）なら、現在選択中の曲をルートの先頭ノードに設定する。

			if( ( this.r現在選択中の曲 == null ) && ( TJAPlayer3.Songs管理.list曲ルート.Count > 0 ) )
				this.r現在選択中の曲 = TJAPlayer3.Songs管理.list曲ルート[ 0 ];

			this.tバーの初期化();

			this.ctBarOpen = new CCounter();
			this.ctBoxOpen = new CCounter();
			this.ctDifficultyIn = new CCounter();

			this.ct三角矢印アニメ = new CCounter();

			this.ctBarFlash = new CCounter();

			this.ctScoreFrameAnime = new CCounter();

			// strboxText here
			if (this.r現在選択中の曲 != null)
			{
				for (int i = 0; i < 3; i++)
				{
					using (var texture = pfBoxText.DrawPrivateFont(this.r現在選択中の曲.strBoxText[i], r現在選択中の曲.ForeColor, r現在選択中の曲.BackColor, 26))
					{
						this.txBoxText[i] = TJAPlayer3.tテクスチャの生成(texture);
						this.strBoxText = this.r現在選択中の曲.strBoxText[0] + this.r現在選択中の曲.strBoxText[1] + this.r現在選択中の曲.strBoxText[2];
					}
				}
			}
            else
            {
				strBoxText = "null";
			}

			base.On活性化();

			this.t選択曲が変更された(true);		// #27648 2012.3.31 yyagi 選曲画面に入った直後の 現在位置/全アイテム数 の表示を正しく行うため
		}
		public override void On非活性化()
		{
			if( this.b活性化してない )
				return;

		    TJAPlayer3.t安全にDisposeする(ref pfBoxName);
		    TJAPlayer3.t安全にDisposeする(ref pfMusicName);
		    TJAPlayer3.t安全にDisposeする(ref pfSubtitle);

			TJAPlayer3.t安全にDisposeする( ref this.ft曲リスト用フォント );

			this.ttk選択している曲の曲名 = null;
			this.ttk選択している曲のサブタイトル = null;

			this.ct三角矢印アニメ = null;

			base.On非活性化();
		}
		public override void OnManagedリソースの作成()
		{
			if( this.b活性化してない )
				return;

			for( int i = 0; i < 9; i++ )
            {
                this.stバー情報[ i ].ttkタイトル = this.ttk曲名テクスチャを生成する( this.stバー情報[ i ].strタイトル文字列, this.stバー情報[i].ForeColor, this.stバー情報[i].BackColor, stバー情報[i].eバー種別 == Eバー種別.Box ? this.pfBoxName : this.pfMusicName);
            }

			int c = ( CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ja" ) ? 0 : 1;

			#region [ Songs not found画像 ]
			try
			{
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

					this.txSongNotFound = new CTexture( TJAPlayer3.app.Device, image, TJAPlayer3.TextureFormat );

					this.txSongNotFound.vc拡大縮小倍率 = new Vector3( 0.5f, 0.5f, 1f );	// 半分のサイズで表示する。
				}
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
				using ( Bitmap image = new Bitmap( 640, 96 ) )
				using ( Graphics graphics = Graphics.FromImage( image ) )
				{
					string[] s1 = { "曲データを検索しています。", "Now enumerating songs." };
					string[] s2 = { "そのまましばらくお待ち下さい。", "Please wait..." };
					graphics.DrawString( s1[c], this.ft曲リスト用フォント, Brushes.DarkGray, (float) 2f, (float) 2f );
					graphics.DrawString( s1[c], this.ft曲リスト用フォント, Brushes.White, (float) 0f, (float) 0f );
					graphics.DrawString( s2[c], this.ft曲リスト用フォント, Brushes.DarkGray, (float) 2f, (float) 44f );
					graphics.DrawString( s2[c], this.ft曲リスト用フォント, Brushes.White, (float) 0f, (float) 42f );

					this.txEnumeratingSongs = new CTexture( TJAPlayer3.app.Device, image, TJAPlayer3.TextureFormat );

					this.txEnumeratingSongs.vc拡大縮小倍率 = new Vector3( 0.5f, 0.5f, 1f );	// 半分のサイズで表示する。
				}
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

			base.OnManagedリソースの作成();
		}
		public override void OnManagedリソースの解放()
		{
			if( this.b活性化してない )
				return;

			for( int i = 0; i < 9; i++ )
            {
                TJAPlayer3.tテクスチャの解放( ref this.stバー情報[ i ].txタイトル名 );
                this.stバー情報[ i ].ttkタイトル = null;
            }

		    ClearTitleTextureCache();

            TJAPlayer3.tテクスチャの解放( ref this.txEnumeratingSongs );
            TJAPlayer3.tテクスチャの解放( ref this.txSongNotFound );

			base.OnManagedリソースの解放();
		}
		public override int On進行描画()
		{
			if (this.b活性化してない)
				return 0;

			#region [ 初めての進行描画 ]
			//-----------------
			if (this.b初めての進行描画)
			{
				this.nスクロールタイマ = (long)(CSound管理.rc演奏用タイマ.n現在時刻 * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0));
				TJAPlayer3.stage選曲.t選択曲変更通知();

				ctBarOpen.t開始(0, 260, 2, TJAPlayer3.Timer);
				this.ct三角矢印アニメ.t開始(0, 1000, 1, TJAPlayer3.Timer);
				base.b初めての進行描画 = false;
			}
			//-----------------
			#endregion

			ctBarFlash.t進行();
			ctBoxOpen.t進行();
			ctBarOpen.t進行();
			ctDifficultyIn.t進行();

			int BarAnimeCount = this.ctBarOpen.n現在の値 <= 200 ? 0 : (int)(Math.Sin(((this.ctBarOpen.n現在の値 - 200) * 1.5f) * (Math.PI / 180)) * 62.0f);

			if (BarAnimeCount == 62)
				ctScoreFrameAnime.t進行Loop();

			// まだ選択中の曲が決まってなければ、曲ツリールートの最初の曲にセットする。

			if ((this.r現在選択中の曲 == null) && (TJAPlayer3.Songs管理.list曲ルート.Count > 0))
				this.r現在選択中の曲 = TJAPlayer3.Songs管理.list曲ルート[0];

			// 描画。
			if (this.r現在選択中の曲 == null)
			{
				#region [ 曲が１つもないなら「Songs not found.」を表示してここで帰れ。]
				//-----------------
				if (bIsEnumeratingSongs)
				{
					if (this.txEnumeratingSongs != null)
					{
						this.txEnumeratingSongs.t2D描画(TJAPlayer3.app.Device, 320, 160);
					}
				}
				else
				{
					if (this.txSongNotFound != null)
						this.txSongNotFound.t2D描画(TJAPlayer3.app.Device, 320, 160);
				}
				//-----------------
				#endregion

				return 0;
			}

			// 本ステージは、(1)登場アニメフェーズ → (2)通常フェーズ　と二段階にわけて進む。

			if (strBoxText != r現在選択中の曲.strBoxText[0] + r現在選択中の曲.strBoxText[1] + r現在選択中の曲.strBoxText[2])
			{
				for (int i = 0; i < 3; i++)
				{
					using (var texture = pfBoxText.DrawPrivateFont(this.r現在選択中の曲.strBoxText[i], r現在選択中の曲.ForeColor, r現在選択中の曲.BackColor, 26))
					{
						this.txBoxText[i] = TJAPlayer3.tテクスチャの生成(texture);
						this.strBoxText = this.r現在選択中の曲.strBoxText[0] + this.r現在選択中の曲.strBoxText[1] + this.r現在選択中の曲.strBoxText[2];
					}
				}
			}


			// 進行。
			if (n現在のスクロールカウンタ == 0) ct三角矢印アニメ.t進行Loop();
			else ct三角矢印アニメ.n現在の値 = 0;

			{
				#region [ (2) 通常フェーズの進行。]
				//-----------------
				long n現在時刻 = CSound管理.rc演奏用タイマ.n現在時刻;

				if (n現在時刻 < this.nスクロールタイマ) // 念のため
					this.nスクロールタイマ = n現在時刻;

				const int nアニメ間隔 = 2;
				while ((n現在時刻 - this.nスクロールタイマ) >= nアニメ間隔)
				{
					int n加速度 = 1;
					int n残距離 = Math.Abs((int)(this.n目標のスクロールカウンタ - this.n現在のスクロールカウンタ));

					#region [ 残距離が遠いほどスクロールを速くする（＝n加速度を多くする）。]
					//-----------------
					if (n残距離 <= 10)
					{
						n加速度 = 1;
					}
					else if (n残距離 <= 100)
					{
						n加速度 = 2;
					}
					else if (n残距離 <= 300)
					{
						n加速度 = 3;
					}
					else if (n残距離 <= 500)
					{
						n加速度 = 4;
					}
					else
					{
						n加速度 = 650;
					}
					//-----------------
					#endregion

					#region [ 加速度を加算し、現在のスクロールカウンタを目標のスクロールカウンタまで近づける。 ]
					//-----------------
					if (this.n現在のスクロールカウンタ < this.n目標のスクロールカウンタ)        // (A) 正の方向に未達の場合：
					{
						this.n現在のスクロールカウンタ += n加速度;                             // カウンタを正方向に移動する。

						if (this.n現在のスクロールカウンタ > this.n目標のスクロールカウンタ)
							this.n現在のスクロールカウンタ = this.n目標のスクロールカウンタ;    // 到着！スクロール停止！
					}

					else if (this.n現在のスクロールカウンタ > this.n目標のスクロールカウンタ)   // (B) 負の方向に未達の場合：
					{
						this.n現在のスクロールカウンタ -= n加速度;                             // カウンタを負方向に移動する。

						if (this.n現在のスクロールカウンタ < this.n目標のスクロールカウンタ)    // 到着！スクロール停止！
							this.n現在のスクロールカウンタ = this.n目標のスクロールカウンタ;
					}
					//-----------------
					#endregion

					if (this.n現在のスクロールカウンタ >= 100)      // １行＝100カウント。
					{
						#region [ パネルを１行上にシフトする。]
						//-----------------

						// 選択曲と選択行を１つ下の行に移動。

						this.r現在選択中の曲 = this.r次の曲(this.r現在選択中の曲);
						this.n現在の選択行 = (this.n現在の選択行 + 1) % 9;

						// 選択曲から７つ下のパネル（＝新しく最下部に表示されるパネル。消えてしまう一番上のパネルを再利用する）に、新しい曲の情報を記載する。

						C曲リストノード song = this.r現在選択中の曲;
						for (int i = 0; i < 4; i++)
							song = this.r次の曲(song);

						int index = (this.n現在の選択行 + 4) % 9; // 新しく最下部に表示されるパネルのインデックス（0～12）。
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

						#endregion

						// stバー情報[] の内容を1行ずつずらす。

						C曲リストノード song2 = this.r現在選択中の曲;
						for (int i = 0; i < 4; i++)
							song2 = this.r前の曲(song2);

						for (int i = 0; i < 9; i++)
						{
							int n = (((this.n現在の選択行 - 4) + i) + 9) % 9;
							this.stバー情報[n].eバー種別 = this.e曲のバー種別を返す(song2);
							song2 = this.r次の曲(song2);
							this.stバー情報[n].ttkタイトル = this.ttk曲名テクスチャを生成する(this.stバー情報[n].strタイトル文字列, this.stバー情報[n].ForeColor, this.stバー情報[n].BackColor, stバー情報[n].eバー種別 == Eバー種別.Box ? this.pfBoxName : this.pfMusicName);
						}


						// 新しく最下部に表示されるパネル用のスキル値を取得。

						for (int i = 0; i < 3; i++)
							this.stバー情報[index].nスキル値[i] = (int)song.arスコア[this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(song)].譜面情報.最大スキル[i];


						// 1行(100カウント)移動完了。

						this.n現在のスクロールカウンタ -= 100;
						this.n目標のスクロールカウンタ -= 100;

						this.t選択曲が変更された(false);             // スクロールバー用に今何番目を選択しているかを更新



						if (this.n目標のスクロールカウンタ == 0)
						{
							TJAPlayer3.stage選曲.t選択曲変更通知();      // スクロール完了＝選択曲変更！
							ctBarOpen.t開始(0, 260, 2, TJAPlayer3.Timer);

							TJAPlayer3.stage選曲.NowGenre = this.r現在選択中の曲.strジャンル;

							TJAPlayer3.stage選曲.NowBg = this.r現在選択中の曲.BgType;

							TJAPlayer3.stage選曲.NowBgColor = this.r現在選択中の曲.BgColor;

							TJAPlayer3.stage選曲.NowUseGenre = !this.r現在選択中の曲.isChangedBgType;

							ctScoreFrameAnime.t開始(0, 6000, 1, TJAPlayer3.Timer);
						}

						//-----------------
						#endregion
					}
					else if (this.n現在のスクロールカウンタ <= -100)
					{
						#region [ パネルを１行下にシフトする。]
						//-----------------

						// 選択曲と選択行を１つ上の行に移動。

						this.r現在選択中の曲 = this.r前の曲(this.r現在選択中の曲);
						this.n現在の選択行 = ((this.n現在の選択行 - 1) + 9) % 9;


						// 選択曲から５つ上のパネル（＝新しく最上部に表示されるパネル。消えてしまう一番下のパネルを再利用する）に、新しい曲の情報を記載する。

						C曲リストノード song = this.r現在選択中の曲;
						for (int i = 0; i < 4; i++)
							song = this.r前の曲(song);

						int index = ((this.n現在の選択行 - 4) + 9) % 9;   // 新しく最上部に表示されるパネルのインデックス（0～12）。
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

						#endregion

						// stバー情報[] の内容を1行ずつずらす。

						C曲リストノード song2 = this.r現在選択中の曲;
						for (int i = 0; i < 4; i++)
							song2 = this.r前の曲(song2);

						for (int i = 0; i < 9; i++)
						{
							int n = (((this.n現在の選択行 - 4) + i) + 9) % 9;
							this.stバー情報[n].eバー種別 = this.e曲のバー種別を返す(song2);
							song2 = this.r次の曲(song2);
							this.stバー情報[n].ttkタイトル = this.ttk曲名テクスチャを生成する(this.stバー情報[n].strタイトル文字列, this.stバー情報[n].ForeColor, this.stバー情報[n].BackColor, stバー情報[n].eバー種別 == Eバー種別.Box ? this.pfBoxName : this.pfMusicName);
						}


						// 新しく最上部に表示されるパネル用のスキル値を取得。

						for (int i = 0; i < 3; i++)
							this.stバー情報[index].nスキル値[i] = (int)song.arスコア[this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(song)].譜面情報.最大スキル[i];


						// 1行(100カウント)移動完了。

						this.n現在のスクロールカウンタ += 100;
						this.n目標のスクロールカウンタ += 100;

						this.t選択曲が変更された(false);             // スクロールバー用に今何番目を選択しているかを更新

						this.ttk選択している曲の曲名 = null;
						this.ttk選択している曲のサブタイトル = null;

						if (this.n目標のスクロールカウンタ == 0)
						{
							TJAPlayer3.stage選曲.t選択曲変更通知();      // スクロール完了＝選択曲変更！
							ctBarOpen.t開始(0, 260, 2, TJAPlayer3.Timer);
							TJAPlayer3.stage選曲.NowGenre = this.r現在選択中の曲.strジャンル;
							TJAPlayer3.stage選曲.NowBg = this.r現在選択中の曲.BgType;
							TJAPlayer3.stage選曲.NowBgColor = this.r現在選択中の曲.BgColor;
							TJAPlayer3.stage選曲.NowUseGenre = !this.r現在選択中の曲.isChangedBgType;
							ctScoreFrameAnime.t開始(0, 6000, 1, TJAPlayer3.Timer);
						}
																//-----------------
						#endregion
					}

					if (this.b選択曲が変更された && n現在のスクロールカウンタ == 0)
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
					}
					this.nスクロールタイマ += nアニメ間隔;
				}
				//-----------------
				#endregion
			}

			int i選曲バーX座標 = 673; //選曲バーの座標用
			int i選択曲バーX座標 = 665; //選択曲バーの座標用

			#region [ (2) 通常フェーズの描画。]
			//-----------------
			for (int i = 0; i < 9; i++) // パネルは全13枚。
			{
				if ((i == 0 && this.n現在のスクロールカウンタ > 0) ||       // 最上行は、上に移動中なら表示しない。
					(i == 8 && this.n現在のスクロールカウンタ < 0))     // 最下行は、下に移動中なら表示しない。
					continue;

				int nパネル番号 = (((this.n現在の選択行 - 4) + i) + 9) % 9;
				int n見た目の行番号 = i;
				int n次のパネル番号 = (this.n現在のスクロールカウンタ <= 0) ? ((i + 1) % 9) : (((i - 1) + 9) % 9);
				int x = i選曲バーX座標;

                #region [Positions and layouts]

                int xZahyou = this.ptバーの座標[n見た目の行番号].X;
				int xNextZahyou = this.ptバーの座標[n次のパネル番号].X;
				int xCentralZahyou = this.ptバーの座標[4].X;

				int yZahyou = this.ptバーの座標[n見た目の行番号].Y;
				int yNextZahyou = this.ptバーの座標[n次のパネル番号].Y;
				int yCentralZahyou = this.ptバーの座標[4].Y;

				int Diff4 = Math.Abs(n見た目の行番号 - 4);
				int NextDiff4 = Math.Abs(n次のパネル番号 - 4);

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

				#endregion
				
				// x set here
				int xAnime = xZahyou + ((int)((xNextZahyou - xZahyou) 
					* (((double)Math.Abs(this.n現在のスクロールカウンタ)) / 100.0)));
				
				int y = yZahyou + ((int)((yNextZahyou - yZahyou) 
					* (((double)Math.Abs(this.n現在のスクロールカウンタ)) / 100.0)));

				// (B) スクロール中の選択曲バー、またはその他のバーの描画。

				float Box = 0;

				#region [ BoxOpenAnime ]

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

				#endregion


				#region [ バーテクスチャを描画。]
				//-----------------

				// int boxType = nStrジャンルtoNum(stバー情報[nパネル番号].strジャンル);
				int boxType = stバー情報[nパネル番号].BoxType;

				if (!stバー情報[nパネル番号].BoxTypeChanged)
					boxType = nStrジャンルtoNum(stバー情報[nパネル番号].strジャンル);

				TJAPlayer3.Tx.SongSelect_Bar_Genre[boxType].color4 = stバー情報[nパネル番号].BoxColor;

				TJAPlayer3.Tx.SongSelect_Bar_Genre[boxType].vc拡大縮小倍率.X = 1.0f;
				TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay.vc拡大縮小倍率.X = 1.0f;
				TJAPlayer3.Tx.SongSelect_Bar_Genre_Back.vc拡大縮小倍率.X = 1.0f;

				


				if (n現在のスクロールカウンタ != 0 || n見た目の行番号 != 4)
                {
					int songType = 0;
					if (this.stバー情報[nパネル番号].ar難易度[(int)Difficulty.Dan] >= 0)
						songType = 1;
					else if (this.stバー情報[nパネル番号].ar難易度[(int)Difficulty.Tower] >= 0)
						songType = 2;

					// TJAPlayer3.act文字コンソール.tPrint(x + -470, y + 30, C文字コンソール.Eフォント種別.白, (this.stバー情報[nパネル番号].ar難易度[(int)Difficulty.Tower]).ToString());

					this.tジャンル別選択されていない曲バーの描画(xAnime - (int)Box, y - ((int)Box * 3), this.stバー情報[nパネル番号].strジャンル, stバー情報[nパネル番号].eバー種別, stバー情報[nパネル番号].nクリア, stバー情報[nパネル番号].nスコアランク, boxType, songType);
				}

				/*
				 
				else if (n見た目の行番号 != 4)
					this.tジャンル別選択されていない曲バーの描画(xAnime - (int)Box, y - ((int)Box * 3), this.stバー情報[nパネル番号].strジャンル, stバー情報[nパネル番号].eバー種別, stバー情報[nパネル番号].nクリア, stバー情報[nパネル番号].nスコアランク, boxType);
				
				*/

				//-----------------
				#endregion

				#region [ タイトル名テクスチャを描画。]
				if (ctDifficultyIn.n現在の値 >= 1000 && TJAPlayer3.stage選曲.act難易度選択画面.bIsDifficltSelect)
					ResolveTitleTexture(this.stバー情報[nパネル番号].ttkタイトル).Opacity = (int)255.0f - (ctDifficultyIn.n現在の値 - 1000);
				else
					ResolveTitleTexture(this.stバー情報[nパネル番号].ttkタイトル).Opacity = 255;

				if (n現在のスクロールカウンタ != 0)
					ResolveTitleTexture(this.stバー情報[nパネル番号].ttkタイトル).t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, xAnime + 316 - Box + TJAPlayer3.Skin.SongSelect_Title_X, y + 59 - (Box * 3) + TJAPlayer3.Skin.SongSelect_Title_Y);
				else if (n見た目の行番号 != 4)
					ResolveTitleTexture(this.stバー情報[nパネル番号].ttkタイトル).t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, xAnime + 316 - Box + TJAPlayer3.Skin.SongSelect_Title_X, y + 62 - (Box * 3) + TJAPlayer3.Skin.SongSelect_Title_Y);
				#endregion

				//-----------------					
			}
			#endregion

			if (this.n現在のスクロールカウンタ == 0)
			{
				#region [ Draw BarCenter ]

				#region [ Bar_Select ]

				if (ctBoxOpen.n現在の値 >= 1300 && ctBoxOpen.n現在の値 <= 1940)
					TJAPlayer3.Tx.SongSelect_Bar_Select.vc拡大縮小倍率.X = 1.0f - (float)Math.Sin(((ctBoxOpen.n現在の値 - 1300) * 0.28125f) * (Math.PI / 180)) * 1.0f;
				else
					TJAPlayer3.Tx.SongSelect_Bar_Select.vc拡大縮小倍率.X = 1.0f;


				if (ctBarFlash.b終了値に達した && !TJAPlayer3.stage選曲.act難易度選択画面.bIsDifficltSelect)
					TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = (int)(BarAnimeCount * 4.25f);
				else
					TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = (int)(255 - (ctBarFlash.n現在の値 - 700) * 2.55f);

				TJAPlayer3.Tx.SongSelect_Bar_Select.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 322 - BarAnimeCount, new Rectangle(0, 0, 662, 40));

				TJAPlayer3.Tx.SongSelect_Bar_Select.vc拡大縮小倍率.Y = BarAnimeCount == 0 ? 0.2f : 0.2f + (float)(BarAnimeCount * 0.012f);
				TJAPlayer3.Tx.SongSelect_Bar_Select.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 360, new Rectangle(0, 40, 662, 172));
				TJAPlayer3.Tx.SongSelect_Bar_Select.vc拡大縮小倍率.Y = 1.0f;

				TJAPlayer3.Tx.SongSelect_Bar_Select.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 398 + BarAnimeCount, new Rectangle(0, 208, 662, 43));

				#region [ BarFlash ]

				if (ctBarFlash.n現在の値 <= 100)
					TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = (int)(ctBarFlash.n現在の値 * 2.55f);
				else if (ctBarFlash.n現在の値 <= 200)
					TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = (int)(255 - (ctBarFlash.n現在の値 - 100) * 2.55f);
				else if (ctBarFlash.n現在の値 <= 300)
					TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = (int)((ctBarFlash.n現在の値 - 200) * 2.55f);
				else if (ctBarFlash.n現在の値 <= 400)
					TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = (int)(255 - (ctBarFlash.n現在の値 - 300) * 2.55f);
				else if (ctBarFlash.n現在の値 <= 500)
					TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = (int)((ctBarFlash.n現在の値 - 400) * 2.55f);
				else if (ctBarFlash.n現在の値 <= 600)
					TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = (int)(255 - (ctBarFlash.n現在の値 - 500) * 2.55f);
				else if (ctBarFlash.n現在の値 <= 700)
					TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = (int)((ctBarFlash.n現在の値 - 600) * 2.55f);
				else if (ctBarFlash.n現在の値 <= 800)
					TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = (int)(255 - (ctBarFlash.n現在の値 - 700) * 2.55f);
				else
					TJAPlayer3.Tx.SongSelect_Bar_Select.Opacity = 0;

				TJAPlayer3.Tx.SongSelect_Bar_Select.t2D中心基準描画(TJAPlayer3.app.Device, 640, 360, new Rectangle(0, 251, 662, 251));

				#endregion

				#endregion

				if (r現在選択中の曲.eノード種別 == C曲リストノード.Eノード種別.SCORE)
                {
					#region [ Score ]

					#region [ Bar ]

					//int boxType = nStrジャンルtoNum(r現在選択中の曲.strジャンル);
					int boxType = r現在選択中の曲.BoxType;

					if (!r現在選択中の曲.isChangedBoxType)
						boxType = nStrジャンルtoNum(r現在選択中の曲.strジャンル);

					TJAPlayer3.Tx.SongSelect_Bar_Genre[boxType].color4 = r現在選択中の曲.BoxColor;

					if (ctBoxOpen.n現在の値 >= 1300 && ctBoxOpen.n現在の値 <= 1940)
					{
						TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay.vc拡大縮小倍率.X = 1.0f - (float)Math.Sin(((ctBoxOpen.n現在の値 - 1300) * 0.28125f) * (Math.PI / 180)) * 1.0f;
						TJAPlayer3.Tx.SongSelect_Bar_Genre[boxType].vc拡大縮小倍率.X = 1.0f - (float)Math.Sin(((ctBoxOpen.n現在の値 - 1300) * 0.28125f) * (Math.PI / 180)) * 1.0f;
					}
					else
					{
						TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay.vc拡大縮小倍率.X = 1.0f;
						TJAPlayer3.Tx.SongSelect_Bar_Genre[boxType].vc拡大縮小倍率.X = 1.0f;
					}

					TJAPlayer3.Tx.SongSelect_Bar_Genre[boxType].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 326 - BarAnimeCount, new Rectangle(0, 0, 632, 21));

					TJAPlayer3.Tx.SongSelect_Bar_Genre[boxType].vc拡大縮小倍率.Y = BarAnimeCount == 0 ? 1.0f : 1.0f + (float)(BarAnimeCount) /  23.6f;
					TJAPlayer3.Tx.SongSelect_Bar_Genre[boxType].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 360, new Rectangle(0, 21, 632, 48));
					TJAPlayer3.Tx.SongSelect_Bar_Genre[boxType].vc拡大縮小倍率.Y = 1.0f;

					TJAPlayer3.Tx.SongSelect_Bar_Genre[boxType].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 394 + BarAnimeCount, new Rectangle(0, 69, 632, 23));
					
					if (BarAnimeCount != 0)
					{
						TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 326 - BarAnimeCount, new Rectangle(0, 0, 632, 21));

						TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay.vc拡大縮小倍率.Y = BarAnimeCount == 0 ? 1.0f : 1.0f + (float)(BarAnimeCount) /  24.5f;
						TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 359, new Rectangle(0, 21, 632, 48));
						TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay.vc拡大縮小倍率.Y = 1.0f;

						TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 394 + BarAnimeCount, new Rectangle(0, 69, 632, 23));
					}
					else
					{
						TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 360, new Rectangle(0, 0, 632, 92));
					}
					#endregion

					#region [ Crown and ScoreRank ]

					// Mark

					if (this.r現在選択中の曲.arスコア[(int)Difficulty.Dan] != null)
                    {
						//int[] clear = this.r現在選択中の曲.arスコア[(int)Difficulty.Dan].譜面情報.nクリア;

						for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
						{
							int diff = 640 - 354;
							int shift = 640 + ((i == 1) ? diff : -diff);
							int ap = TJAPlayer3.GetActualPlayer(i);

							int[] clear = this.r現在選択中の曲.arスコア[(int)Difficulty.Dan].GPInfo[ap].nClear;

							int currentRank = Math.Min(clear[0], 6) - 1;
							displayDanStatus(shift, (int)(344 - BarAnimeCount / 1.1f), currentRank, 0.2f);
						}

					}
					else if (this.r現在選択中の曲.arスコア[(int)Difficulty.Tower] != null)
					{
						//int[] clear = this.r現在選択中の曲.arスコア[(int)Difficulty.Tower].譜面情報.nクリア;

						for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
						{
							int diff = 640 - 354;
							int shift = 640 + ((i == 1) ? diff : -diff);
							int ap = TJAPlayer3.GetActualPlayer(i);

							int[] clear = this.r現在選択中の曲.arスコア[(int)Difficulty.Tower].GPInfo[ap].nClear;

							int currentRank = Math.Min(clear[0], 7) - 1;
							displayTowerStatus(shift, (int)(344 - BarAnimeCount / 1.1f), currentRank, 0.3f);
						}
					}
					else if (this.r現在選択中の曲.arスコア[3] != null || this.r現在選択中の曲.arスコア[4] != null)
					{
						var sr = this.r現在選択中の曲.arスコア[n現在のアンカ難易度レベルに最も近い難易度レベルを返す(this.r現在選択中の曲)];

						for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                        {
							int diff = 640 - 354;
							int shift = 640 + ((i == 1) ? diff : -diff);
							int ap = TJAPlayer3.GetActualPlayer(i);

							int[] クリア = sr.GPInfo[ap].nClear;

							int[] スコアランク = sr.GPInfo[ap].nScoreRank;

							displayRegularCrowns(shift, (int)(344 - BarAnimeCount / 1.1f), クリア, スコアランク, 0.8f + BarAnimeCount / 620f);
						}
					}

					#endregion

					#endregion
				}
                if (r現在選択中の曲.eノード種別 == C曲リストノード.Eノード種別.BOX)
                {
					#region [ Box ]

					//int boxType = nStrジャンルtoNum(r現在選択中の曲.strジャンル);
					int boxType = r現在選択中の曲.BoxType;

					if (!r現在選択中の曲.isChangedBoxType)
						boxType = nStrジャンルtoNum(r現在選択中の曲.strジャンル);

					TJAPlayer3.Tx.SongSelect_Bar_Genre[boxType].color4 = r現在選択中の曲.BoxColor;

					if (ctBoxOpen.n現在の値 >= 1300 && ctBoxOpen.n現在の値 <= 1940)
                    {
						TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay.vc拡大縮小倍率.X = 1.0f - (float)Math.Sin(((ctBoxOpen.n現在の値 - 1300) * 0.28125f) * (Math.PI / 180)) * 1.0f;
						TJAPlayer3.Tx.SongSelect_Bar_Genre[boxType].vc拡大縮小倍率.X = 1.0f - (float)Math.Sin(((ctBoxOpen.n現在の値 - 1300) * 0.28125f) * (Math.PI / 180)) * 1.0f;
					}
                    else
					{
						TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay.vc拡大縮小倍率.X = 1.0f;
						TJAPlayer3.Tx.SongSelect_Bar_Genre[boxType].vc拡大縮小倍率.X = 1.0f;
					}

					TJAPlayer3.Tx.SongSelect_Bar_Genre[boxType].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 326 - BarAnimeCount, new Rectangle(0, 0, 632, 21));

					TJAPlayer3.Tx.SongSelect_Bar_Genre[boxType].vc拡大縮小倍率.Y = BarAnimeCount == 0 ? 1.0f : 1.0f + (float)(BarAnimeCount) /  23.6f;
					TJAPlayer3.Tx.SongSelect_Bar_Genre[boxType].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 360, new Rectangle(0, 21, 632, 48));
					TJAPlayer3.Tx.SongSelect_Bar_Genre[boxType].vc拡大縮小倍率.Y = 1.0f;

					TJAPlayer3.Tx.SongSelect_Bar_Genre[boxType].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 394 + BarAnimeCount, new Rectangle(0, 69, 632, 23));
					
					if (BarAnimeCount != 0)
					{
						TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 326, new Rectangle(0, 0, 632, 21));

						TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay.vc拡大縮小倍率.Y = BarAnimeCount == 0 ? 1.0f : 1.0f + (float)(BarAnimeCount) /  50;
						TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay.t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, 640, 336, new Rectangle(0, 21, 632, 48));
						TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay.vc拡大縮小倍率.Y = 1.0f;

						TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 394 + BarAnimeCount, new Rectangle(0, 69, 632, 23));
					}
					else
					{
						TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 360, new Rectangle(0, 0, 632, 92));
					}
					#endregion
				}
				if(r現在選択中の曲.eノード種別 == C曲リストノード.Eノード種別.BACKBOX)
                {
					#region [ BackBox ]

					if (ctBoxOpen.n現在の値 >= 1300 && ctBoxOpen.n現在の値 <= 1940)
						TJAPlayer3.Tx.SongSelect_Bar_Genre_Back.vc拡大縮小倍率.X = 1.0f - (float)Math.Sin(((ctBoxOpen.n現在の値 - 1300) * 0.28125f) * (Math.PI / 180)) * 1.0f;
					else
						TJAPlayer3.Tx.SongSelect_Bar_Genre_Back.vc拡大縮小倍率.X = 1.0f;

					TJAPlayer3.Tx.SongSelect_Bar_Genre_Back.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 326 - BarAnimeCount, new Rectangle(0, 0, 632, 21));

					TJAPlayer3.Tx.SongSelect_Bar_Genre_Back.vc拡大縮小倍率.Y = BarAnimeCount == 0 ? 1.0f : 1.0f + (float)(BarAnimeCount) /  23.6f;
					TJAPlayer3.Tx.SongSelect_Bar_Genre_Back.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 360, new Rectangle(0, 21, 632, 48));
					TJAPlayer3.Tx.SongSelect_Bar_Genre_Back.vc拡大縮小倍率.Y = 1.0f;

					TJAPlayer3.Tx.SongSelect_Bar_Genre_Back.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 394 + BarAnimeCount, new Rectangle(0, 69, 632, 23));
					
					#endregion
				}
				if (r現在選択中の曲.eノード種別 == C曲リストノード.Eノード種別.RANDOM)
                {
					#region [Random box]

					if (TJAPlayer3.Tx.SongSelect_Bar_Genre_Random != null)
                    {
						if (ctBoxOpen.n現在の値 >= 1300 && ctBoxOpen.n現在の値 <= 1940)
							TJAPlayer3.Tx.SongSelect_Bar_Genre_Random.vc拡大縮小倍率.X = 1.0f - (float)Math.Sin(((ctBoxOpen.n現在の値 - 1300) * 0.28125f) * (Math.PI / 180)) * 1.0f;
						else
							TJAPlayer3.Tx.SongSelect_Bar_Genre_Random.vc拡大縮小倍率.X = 1.0f;

						TJAPlayer3.Tx.SongSelect_Bar_Genre_Random.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 326 - BarAnimeCount, new Rectangle(0, 0, 632, 21));

						TJAPlayer3.Tx.SongSelect_Bar_Genre_Random.vc拡大縮小倍率.Y = BarAnimeCount == 0 ? 1.0f : 1.0f + (float)(BarAnimeCount) / 23.6f;
						TJAPlayer3.Tx.SongSelect_Bar_Genre_Random.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 360, new Rectangle(0, 21, 632, 48));
						TJAPlayer3.Tx.SongSelect_Bar_Genre_Random.vc拡大縮小倍率.Y = 1.0f;

						TJAPlayer3.Tx.SongSelect_Bar_Genre_Random.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 394 + BarAnimeCount, new Rectangle(0, 69, 632, 23));

					}

					#endregion
				}

                #endregion

                switch (r現在選択中の曲.eノード種別)
				{
					case C曲リストノード.Eノード種別.SCORE:
						{
							if (TJAPlayer3.Tx.SongSelect_Frame_Score != null)
							{
								// 難易度がTower、Danではない
								if (TJAPlayer3.stage選曲.n現在選択中の曲の難易度 != (int)Difficulty.Tower && TJAPlayer3.stage選曲.n現在選択中の曲の難易度 != (int)Difficulty.Dan)
								{
                                    #region [Display difficulty boxes]

                                    bool uraExists = TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[(int)Difficulty.Edit] >= 0;
									bool omoteExists = TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[(int)Difficulty.Oni] >= 0;

									for (int i = 0; i < (int)Difficulty.Edit + 1; i++)
									{
										if (ctBarOpen.n現在の値 >= 100)
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

											bool avaliable = TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[i] >= 0;

											if (avaliable)
												TJAPlayer3.Tx.SongSelect_Frame_Score[0].color4 = new Color4(1f, 1f, 1f);
											else
												TJAPlayer3.Tx.SongSelect_Frame_Score[0].color4 = new Color4(0.5f, 0.5f, 0.5f);

											#endregion

											// opacity : int (Box and scores opacity)
											#region [Opacity management]

											int opacity = 0;

											if (avaliable && BarAnimeCount == 62)
											{
												if (ctScoreFrameAnime.n現在の値 <= 3000)
													opacity = Math.Max(0, ctScoreFrameAnime.n現在の値 - 2745);
												else 
													opacity = Math.Min(255, 255 - (ctScoreFrameAnime.n現在の値 - 5745));
											}

											#endregion

											#region [Display box parameters]

											int difSelectOpacity = (i == (int)Difficulty.Edit && omoteExists) ? (BarAnimeCount < 62 ? 0 : opacity) : (int)(BarAnimeCount * 4.25f);
						
											if (!TJAPlayer3.stage選曲.act難易度選択画面.bIsDifficltSelect || ctDifficultyIn.n現在の値 < 1000)
                                            {
												TJAPlayer3.Tx.SongSelect_Frame_Score[0].Opacity = difSelectOpacity;
												TJAPlayer3.Tx.SongSelect_Level_Number.Opacity = difSelectOpacity;
											}
											else if (ctDifficultyIn.n現在の値 >= 1000)
											{
												int difInOpacity = (int)((float)((int)255.0f - (ctDifficultyIn.n現在の値 - 1000)) * ((i == (int)Difficulty.Edit && omoteExists) ? (float)difSelectOpacity / 255f : 1f));

												TJAPlayer3.Tx.SongSelect_Frame_Score[0].Opacity = difInOpacity;
												TJAPlayer3.Tx.SongSelect_Level_Number.Opacity = difInOpacity;
											}

											#endregion

											#region [Displayables]

											int displayingDiff = Math.Min(i, (int)Difficulty.Oni);
											int positionalOffset = displayingDiff * 122;

											TJAPlayer3.Tx.SongSelect_Frame_Score[0].t2D下中央基準描画(TJAPlayer3.app.Device, 494 
												+ positionalOffset - 31, TJAPlayer3.Skin.SongSelect_Overall_Y + 465, new Rectangle(122 * i, 0, 122, 360));

											if (avaliable)
												t小文字表示(TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[i] < 10 ? 497 
													+ positionalOffset - 5 : 492 + positionalOffset - 5, TJAPlayer3.Skin.SongSelect_Overall_Y + 277, 
													TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[i].ToString());

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
									if (TJAPlayer3.stage選曲.n現在選択中の曲の難易度 == (int)Difficulty.Dan)
										diff = 6;

									#endregion

									// avaliable : bool (Chart exists)
									#region [Gray box if stage isn't avaliable]

									bool avaliable = TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[diff] >= 0;

									if (avaliable)
										TJAPlayer3.Tx.SongSelect_Frame_Score[1].color4 = new Color4(1f, 1f, 1f);
									else
										TJAPlayer3.Tx.SongSelect_Frame_Score[1].color4 = new Color4(0.5f, 0.5f, 0.5f);

									#endregion

									// opacity : int (Box and scores opacity)
									#region [Opacity management]

									int opacity = 0;

									if (avaliable && BarAnimeCount == 62)
									{
										if (ctScoreFrameAnime.n現在の値 <= 3000)
											opacity = Math.Max(0, ctScoreFrameAnime.n現在の値 - 2745);
										else
											opacity = Math.Min(255, 255 - (ctScoreFrameAnime.n現在の値 - 5745));
									}

									#endregion

									#region [Display box parameters]

									int difSelectOpacity = (int)(BarAnimeCount * 4.25f);

									if (!TJAPlayer3.stage選曲.act難易度選択画面.bIsDifficltSelect || ctDifficultyIn.n現在の値 < 1000)
									{
										TJAPlayer3.Tx.SongSelect_Frame_Score[1].Opacity = difSelectOpacity;
										TJAPlayer3.Tx.SongSelect_Level_Number.Opacity = difSelectOpacity;
									}
									else if (ctDifficultyIn.n現在の値 >= 1000)
									{
										int difInOpacity = (int)255.0f - (ctDifficultyIn.n現在の値 - 1000);

										TJAPlayer3.Tx.SongSelect_Frame_Score[1].Opacity = difInOpacity;
										TJAPlayer3.Tx.SongSelect_Level_Number.Opacity = difInOpacity;
									}

									#endregion

									#region [Displayables]

									int xOffset = (diff - 5) * 244;

									TJAPlayer3.Tx.SongSelect_Frame_Score[1].t2D下中央基準描画(TJAPlayer3.app.Device, 494
										 - 31, TJAPlayer3.Skin.SongSelect_Overall_Y + 465, new Rectangle(xOffset, 0, 122, 360));

									if (avaliable)
										t小文字表示(TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[diff] < 10 ? 497
											- 5 : 492 - 5, TJAPlayer3.Skin.SongSelect_Overall_Y + 277,
											TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.nレベル[diff].ToString());

									#endregion

								}

							}
						}
						break;

					case C曲リストノード.Eノード種別.BOX:
						{
							for (int j = 0; j < 3; j++)
							{
								if (!ctBoxOpen.b終了値に達した && ctBoxOpen.n現在の値 != 0)
								{
									if (txBoxText[j] != null)
										this.txBoxText[j].Opacity = (int)(ctBoxOpen.n現在の値 >= 1200 && ctBoxOpen.n現在の値 <= 1620 ? 255 - (ctBoxOpen.n現在の値 - 1200) * 2.55f :
										ctBoxOpen.n現在の値 >= 2000 ? (ctBoxOpen.n現在の値 - 2000) * 2.55f : ctBoxOpen.n現在の値 <= 1200 ? 255 : 0);
								}
								else
									if (txBoxText[j] != null)
									this.txBoxText[j].Opacity = (int)(BarAnimeCount * 4.25f);

								if (this.txBoxText[j].szテクスチャサイズ.Width >= 510)
									this.txBoxText[j].vc拡大縮小倍率.X = 510f / this.txBoxText[j].szテクスチャサイズ.Width;

								this.txBoxText[j].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640 + TJAPlayer3.Skin.SongSelect_BoxExplanation_X, 360 + j * TJAPlayer3.Skin.SongSelect_BoxExplanation_Interval + TJAPlayer3.Skin.SongSelect_BoxExplanation_Y);
							}

							// Chara here

							int boxType = r現在選択中の曲.BoxChara;

							if (!r現在選択中の曲.isChangedBoxChara)
								boxType = this.nStrジャンルtoNumBox(r現在選択中の曲.strジャンル);

							// If BoxChara < 0, don't display any character
							if (boxType >= 0 && boxType < TJAPlayer3.Skin.SongSelect_Box_Chara_Count)
                            {
								if (!ctBoxOpen.b終了値に達した)
									TJAPlayer3.Tx.SongSelect_Box_Chara[boxType].Opacity = (int)(ctBoxOpen.n現在の値 >= 1200 && ctBoxOpen.n現在の値 <= 1620 ? 255 - (ctBoxOpen.n現在の値 - 1200) * 2.55f :
									ctBoxOpen.n現在の値 >= 2000 ? (ctBoxOpen.n現在の値 - 2000) * 2.55f : ctBoxOpen.n現在の値 <= 1200 ? 255 : 0);
								else
								{
									if (!TJAPlayer3.stage選曲.act難易度選択画面.bIsDifficltSelect)
										TJAPlayer3.Tx.SongSelect_Box_Chara[boxType].Opacity = (int)(BarAnimeCount * 4.25f);
									else if (ctDifficultyIn.n現在の値 >= 1000)
										TJAPlayer3.Tx.SongSelect_Box_Chara[boxType].Opacity = (int)255.0f - (ctDifficultyIn.n現在の値 - 1000);
								}

								float anime = 0;

								if (BarAnimeCount <= 45)
									anime = BarAnimeCount * 3.333333333f;
								else
									anime = 150 - (BarAnimeCount - 45) * 2.11764705f;

								TJAPlayer3.Tx.SongSelect_Box_Chara[boxType]?.t2D中心基準描画(TJAPlayer3.app.Device, 640 - TJAPlayer3.Tx.SongSelect_Box_Chara[boxType].szテクスチャサイズ.Width / 4 + 114 - anime, 360,
									new Rectangle(0, 0, TJAPlayer3.Tx.SongSelect_Box_Chara[boxType].szテクスチャサイズ.Width / 2,
									TJAPlayer3.Tx.SongSelect_Box_Chara[boxType].szテクスチャサイズ.Height));

								TJAPlayer3.Tx.SongSelect_Box_Chara[boxType]?.t2D中心基準描画(TJAPlayer3.app.Device, 640 + TJAPlayer3.Tx.SongSelect_Box_Chara[boxType].szテクスチャサイズ.Width / 4 - 114 + anime, 360,
									new Rectangle(TJAPlayer3.Tx.SongSelect_Box_Chara[boxType].szテクスチャサイズ.Width / 2, 0,
									TJAPlayer3.Tx.SongSelect_Box_Chara[boxType].szテクスチャサイズ.Width / 2, TJAPlayer3.Tx.SongSelect_Box_Chara[boxType].szテクスチャサイズ.Height));
							}
						}
						break;

					case C曲リストノード.Eノード種別.BACKBOX:
						if (TJAPlayer3.Tx.SongSelect_Frame_BackBox != null)
							TJAPlayer3.Tx.SongSelect_Frame_BackBox.t2D描画(TJAPlayer3.app.Device, 450, TJAPlayer3.Skin.SongSelect_Overall_Y);
						break;

					case C曲リストノード.Eノード種別.RANDOM:
						if (TJAPlayer3.Tx.SongSelect_Frame_Random != null)
							TJAPlayer3.Tx.SongSelect_Frame_Random.t2D描画(TJAPlayer3.app.Device, 450, TJAPlayer3.Skin.SongSelect_Overall_Y);
						break;
				}

				if (TJAPlayer3.Tx.SongSelect_Branch_Text != null 
					&& TJAPlayer3.stage選曲.r現在選択中のスコア.譜面情報.b譜面分岐[TJAPlayer3.stage選曲.n現在選択中の曲の難易度])
					TJAPlayer3.Tx.SongSelect_Branch_Text.t2D描画(TJAPlayer3.app.Device, 483, TJAPlayer3.Skin.SongSelect_Overall_Y + 21);

			}

			if (ctBoxOpen.n現在の値 >= 1620)
			{
				if (bBoxOpen)
				{
					this.tBOXに入る();
					bBoxOpen = false;
				}
				if (bBoxClose)
				{
					this.tBOXを出る();
					TJAPlayer3.stage選曲.bBGM再生済み = false;
					/*
					if (TJAPlayer3.ConfigIni.bBGM音を発声する || !TJAPlayer3.Skin.bgm選曲画面イン.b再生中)
						TJAPlayer3.Skin.bgm選曲画面イン.t再生する();
					TJAPlayer3.stage選曲.bBGMIn再生した = true;
					*/
					CSongSelectSongManager.playSongIfPossible();
					bBoxClose = false;
				}
			}

			if(ctDifficultyIn.n現在の値 >= ctDifficultyIn.n終了値)
            {
				ctDifficultyIn.t停止();
			}

			for (int i = 0; i < 9; i++)    // パネルは全13枚。
			{
				if ((i == 0 && this.n現在のスクロールカウンタ > 0) ||       // 最上行は、上に移動中なら表示しない。
					(i == 8 && this.n現在のスクロールカウンタ < 0))        // 最下行は、下に移動中なら表示しない。
					continue;

				int nパネル番号 = (((this.n現在の選択行 - 4) + i) + 9) % 9;
				int n見た目の行番号 = i;
				int n次のパネル番号 = (this.n現在のスクロールカウンタ <= 0) ? ((i + 1) % 9) : (((i - 1) + 9) % 9);
				//int x = this.ptバーの基本座標[ n見た目の行番号 ].X + ( (int) ( ( this.ptバーの基本座標[ n次のパネル番号 ].X - this.ptバーの基本座標[ n見た目の行番号 ].X ) * ( ( (double) Math.Abs( this.n現在のスクロールカウンタ ) ) / 100.0 ) ) );
				int x = i選曲バーX座標;
				int xAnime = this.ptバーの座標[n見た目の行番号].X + ((int)((this.ptバーの座標[n次のパネル番号].X - this.ptバーの座標[n見た目の行番号].X) * (((double)Math.Abs(this.n現在のスクロールカウンタ)) / 100.0)));

				int y = this.ptバーの座標[n見た目の行番号].Y + ((int)((this.ptバーの座標[n次のパネル番号].Y - this.ptバーの座標[n見た目の行番号].Y) * (((double)Math.Abs(this.n現在のスクロールカウンタ)) / 100.0)));
				
				if ((i == 4) && (this.n現在のスクロールカウンタ == 0))
				{
					CTexture tx選択している曲のサブタイトル = null;

					// (A) スクロールが停止しているときの選択曲バーの描画。

					#region [ タイトル名テクスチャを描画。]

					// Fonts here

					//-----------------
					if (this.stバー情報[nパネル番号].strタイトル文字列 != "" && this.ttk選択している曲の曲名 == null)
						this.ttk選択している曲の曲名 = this.ttk曲名テクスチャを生成する(this.stバー情報[nパネル番号].strタイトル文字列, this.stバー情報[nパネル番号].ForeColor, this.stバー情報[nパネル番号].BackColor, stバー情報[nパネル番号].eバー種別 == Eバー種別.Box ? this.pfBoxName : this.pfMusicName);
					if (this.stバー情報[nパネル番号].strサブタイトル != "" && this.ttk選択している曲のサブタイトル == null)
						this.ttk選択している曲のサブタイトル = this.ttkサブタイトルテクスチャを生成する(this.stバー情報[nパネル番号].strサブタイトル, this.stバー情報[nパネル番号].ForeColor, this.stバー情報[nパネル番号].BackColor);

					if (this.ttk選択している曲のサブタイトル != null)
						tx選択している曲のサブタイトル = ResolveTitleTexture(ttk選択している曲のサブタイトル);

					//サブタイトルがあったら700

					if (ttk選択している曲の曲名 != null)
					{
						if (!ctBoxOpen.b終了値に達した)
							ResolveTitleTexture(this.ttk選択している曲の曲名).Opacity = (int)(ctBoxOpen.n現在の値 >= 1200 && ctBoxOpen.n現在の値 <= 1620 ? 255 - (ctBoxOpen.n現在の値 - 1200) * 2.55f :
							ctBoxOpen.n現在の値 >= 2000 ? (ctBoxOpen.n現在の値 - 2000) * 2.55f : ctBoxOpen.n現在の値 <= 1200 ? 255 : 0);
						else
						{
							if (!TJAPlayer3.stage選曲.act難易度選択画面.bIsDifficltSelect)
								ResolveTitleTexture(this.ttk選択している曲の曲名).Opacity = 255;
							else if (ctDifficultyIn.n現在の値 >= 1000)
								ResolveTitleTexture(this.ttk選択している曲の曲名).Opacity = (int)255.0f - (ctDifficultyIn.n現在の値 - 1000);
						}
					}

					if (this.ttk選択している曲のサブタイトル != null)
					{
						if (!ctBoxOpen.b終了値に達した)
							tx選択している曲のサブタイトル.Opacity = (int)(ctBoxOpen.n現在の値 >= 1200 && ctBoxOpen.n現在の値 <= 1620 ? 255 - (ctBoxOpen.n現在の値 - 1200) * 2.55f :
							ctBoxOpen.n現在の値 >= 2000 ? (ctBoxOpen.n現在の値 - 2000) * 2.55f : ctBoxOpen.n現在の値 <= 1200 ? 255 : 0);
                        else
						{
							if (!TJAPlayer3.stage選曲.act難易度選択画面.bIsDifficltSelect)
								tx選択している曲のサブタイトル.Opacity = (int)(BarAnimeCount * 4.25f);
                            else if (ctDifficultyIn.n現在の値 >= 1000)
									tx選択している曲のサブタイトル.Opacity = (int)255.0f - (ctDifficultyIn.n現在の値 - 1000);
						} 

						tx選択している曲のサブタイトル.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640 + TJAPlayer3.Skin.SongSelect_Title_X, y + 90 - (this.stバー情報[nパネル番号].eバー種別 == Eバー種別.Box ? BarAnimeCount : BarAnimeCount / 1.1f) + TJAPlayer3.Skin.SongSelect_Title_Y);
						if (this.ttk選択している曲の曲名 != null)
						{
							ResolveTitleTexture(this.ttk選択している曲の曲名).t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640 + TJAPlayer3.Skin.SongSelect_Title_X, y + 61 - (r現在選択中の曲.eノード種別 != C曲リストノード.Eノード種別.BACKBOX ? (this.stバー情報[nパネル番号].eバー種別 == Eバー種別.Box ? BarAnimeCount : BarAnimeCount / 1.1f) : 0) + TJAPlayer3.Skin.SongSelect_Title_Y);
						}
					}
					else
					{
						if (this.ttk選択している曲の曲名 != null)
						{
							ResolveTitleTexture(this.ttk選択している曲の曲名).t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640 + TJAPlayer3.Skin.SongSelect_Title_X, y + 61 - (r現在選択中の曲.eノード種別 != C曲リストノード.Eノード種別.BACKBOX ? (this.stバー情報[nパネル番号].eバー種別 == Eバー種別.Box ? BarAnimeCount : BarAnimeCount / 1.1f) : 0) + TJAPlayer3.Skin.SongSelect_Title_Y);
						}
					}
					//-----------------
					#endregion
				}
			}
			//-----------------

			return 0;
		}
		

		// その他

		#region [ private ]
		//-----------------
		private enum Eバー種別 { Score, Box, Other, BackBox, Random }

		// Edit + 1 => UraOmote ScorePad, add 2P later
		public CScorePad[] ScorePads = new CScorePad[(int)Difficulty.Edit + 2] { new CScorePad(), new CScorePad(), new CScorePad(), new CScorePad(), new CScorePad(), new CScorePad() };
		public CScorePad[] ScorePads2 = new CScorePad[(int)Difficulty.Edit + 2] { new CScorePad(), new CScorePad(), new CScorePad(), new CScorePad(), new CScorePad(), new CScorePad() };

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
			public int BoxType;
			public int BgType;
			public int BoxChara;

			public bool BoxTypeChanged;
			public bool BgTypeChanged;
			public bool BoxCharaChanged;

			public int[] ar難易度;
            public bool[] b分岐;
            public string strジャンル;
            public string strサブタイトル;
            public TitleTextureKey ttkタイトル;

			public int[][] nクリア;
			public int[][] nスコアランク;
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
        private CPrivateFastFont pfMusicName;
        private CPrivateFastFont pfSubtitle;
        private CPrivateFastFont pfBoxName;

		private string strBoxText;
		private CPrivateFastFont pfBoxText;
		private CTexture[] txBoxText = new CTexture[3];

		private readonly Dictionary<TitleTextureKey, CTexture> _titledictionary
			= new Dictionary<TitleTextureKey, CTexture>();

		private Font ft曲リスト用フォント;
		private long nスクロールタイマ;
		public int n現在のスクロールカウンタ;
		private int n現在の選択行;
		private int n目標のスクロールカウンタ;
        private readonly Point[] ptバーの座標 = new Point[] {
		new Point(214, -127),new Point(239, -36), new Point(263, 55), new Point(291, 145),
		new Point(324, 314),
		new Point(358, 485), new Point(386, 574), new Point(411, 665), new Point(436, 756) };

        private STバー情報[] stバー情報 = new STバー情報[ 9 ];
		private CTexture txSongNotFound, txEnumeratingSongs;

        private TitleTextureKey ttk選択している曲の曲名;
        private TitleTextureKey ttk選択している曲のサブタイトル;

        private CTexture[] tx曲バー_難易度 = new CTexture[ 5 ];

		private int nCurrentPosition = 0;
		private int nNumOfItems = 0;

		private Eバー種別 e曲のバー種別を返す( C曲リストノード song )
		{
			if( song != null )
			{
				switch( song.eノード種別 )
				{
					case C曲リストノード.Eノード種別.SCORE:
					case C曲リストノード.Eノード種別.SCORE_MIDI:
						return Eバー種別.Score;

					case C曲リストノード.Eノード種別.BOX:
						return Eバー種別.Box;

					case C曲リストノード.Eノード種別.BACKBOX:
						return Eバー種別.BackBox;

					case C曲リストノード.Eノード種別.RANDOM:
						return Eバー種別.Random;
				}
			}
			return Eバー種別.Other;
		}
		public C曲リストノード r次の曲( C曲リストノード song )
		{
			if( song == null )
				return null;

			List<C曲リストノード> list = TJAPlayer3.Songs管理.list曲ルート;

			int index = list.IndexOf( song );

			if( index < 0 )
				return null;

			if( index == ( list.Count - 1 ) )
				return list[ 0 ];

			return list[ index + 1 ];
		}
		public C曲リストノード r前の曲( C曲リストノード song )
		{
			if( song == null )
				return null;

			List<C曲リストノード> list = TJAPlayer3.Songs管理.list曲ルート;

			int index = list.IndexOf( song );
	
			if( index < 0 )
				return null;

			if( index == 0 )
				return list[ list.Count - 1 ];

			return list[ index - 1 ];
		}

		private void tバーの初期化()
		{
			C曲リストノード song = this.r現在選択中の曲;
			
			if( song == null )
				return;

			for( int i = 0; i < 4; i++ )
				song = this.r前の曲( song );

			//

			for ( int i = 0; i < 9; i++ )
			{
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

			    for( int f = 0; f < (int)Difficulty.Total; f++ )
                {
                    if( song.arスコア[ f ] != null )
                        this.stバー情報[ i ].b分岐 = song.arスコア[ f ].譜面情報.b譜面分岐;
                }

				/*
				if (stバー情報[i].nクリア == null)
					this.stバー情報[i].nクリア = new int[5];
				if (stバー情報[i].nスコアランク == null)
					this.stバー情報[i].nスコアランク = new int[5];



				if(this.stバー情報[i].eバー種別 == Eバー種別.Score)
				{ 
					this.stバー情報[i].nクリア = song.arスコア[n現在のアンカ難易度レベルに最も近い難易度レベルを返す(song)].譜面情報.nクリア;
					this.stバー情報[i].nスコアランク = song.arスコア[n現在のアンカ難易度レベルに最も近い難易度レベルを返す(song)].譜面情報.nスコアランク;

				}
				*/

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

				#endregion



				for ( int j = 0; j < 3; j++ )
					this.stバー情報[ i ].nスキル値[ j ] = (int) song.arスコア[ this.n現在のアンカ難易度レベルに最も近い難易度レベルを返す( song ) ].譜面情報.最大スキル[ j ];

                this.stバー情報[ i ].ttkタイトル = this.ttk曲名テクスチャを生成する( this.stバー情報[ i ].strタイトル文字列, this.stバー情報[i].ForeColor, this.stバー情報[i].BackColor, stバー情報[i].eバー種別 == Eバー種別.Box ? this.pfBoxName : this.pfMusicName);

				song = this.r次の曲( song );
			}

			this.n現在の選択行 = 4;
		}

		// Song type : 0 - Ensou, 1 - Dan, 2 - Tower
		private void tジャンル別選択されていない曲バーの描画(int x, int y, string strジャンル, Eバー種別 eバー種別, int[][] クリア, int[][] スコアランク, int boxType, int _songType = 0)
		{
			if (x >= SampleFramework.GameWindowSize.Width || y >= SampleFramework.GameWindowSize.Height)
				return;

			var rc = new Rectangle(0, 48, 128, 48);

			int opct = 255;

			if (TJAPlayer3.stage選曲.act難易度選択画面.bIsDifficltSelect && ctDifficultyIn.n現在の値 >= 1000)
				opct = Math.Max((int)255.0f - (ctDifficultyIn.n現在の値 - 1000), 0);

			TJAPlayer3.Tx.SongSelect_Crown.Opacity = opct;
			TJAPlayer3.Tx.SongSelect_ScoreRank.Opacity = opct;
			for (int i = 0; i < TJAPlayer3.Skin.SongSelect_Bar_Genre_Count; i++)
				TJAPlayer3.Tx.SongSelect_Bar_Genre[i].Opacity = opct;
			TJAPlayer3.Tx.SongSelect_Bar_Genre_Back.Opacity = opct;
			TJAPlayer3.Tx.SongSelect_Bar_Genre_Random.Opacity = opct;
			TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay.Opacity = opct;

			if (eバー種別 == Eバー種別.Random)
            {
				TJAPlayer3.Tx.SongSelect_Bar_Genre_Random?.t2D描画(TJAPlayer3.app.Device, x, y);
			}
			else if (eバー種別 != Eバー種別.BackBox)
			{
				TJAPlayer3.Tx.SongSelect_Bar_Genre[boxType]?.t2D描画(TJAPlayer3.app.Device, x, y);

				if (TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay != null)
					TJAPlayer3.Tx.SongSelect_Bar_Genre_Overlay.t2D描画(TJAPlayer3.app.Device, x, y);
			}
			else
			{
				TJAPlayer3.Tx.SongSelect_Bar_Genre_Back?.t2D描画(TJAPlayer3.app.Device, x, y);
			}

			if (eバー種別 == Eバー種別.Score)
			{
				if (_songType == 1)
                {
					// displayDanStatus(x + 30, y + 30, Math.Min(クリア[0][0], 6) - 1, 0.2f);

					for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
					{
						int diff = 640 - 354;
						int shift = 640 + ((i == 1) ? diff : -diff) - 354;

						displayDanStatus(x + 30 + shift, y + 30, Math.Min(クリア[i][0], 6) - 1, 0.2f);
					}
				}	
				else if (_songType == 2)
                {
					// displayTowerStatus(x + 30, y + 30, Math.Min(クリア[0][0], 6) - 1, 0.2f);

					for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
					{
						int diff = 640 - 354;
						int shift = 640 + ((i == 1) ? diff : -diff) - 354;

						displayTowerStatus(x + 30 + shift, y + 30, Math.Min(クリア[i][0], 7) - 1, 0.3f);

						// TJAPlayer3.act文字コンソール.tPrint(x + 30 + shift - 50, y + 30, C文字コンソール.Eフォント種別.白, (Math.Min(クリア[i][0], 7) - 1).ToString());
					}
				}
				else
                {
					// var sr = this.r現在選択中の曲.arスコア[n現在のアンカ難易度レベルに最も近い難易度レベルを返す(this.r現在選択中の曲)];

					for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
					{
						int diff = 640 - 354;
						int shift = 640 + ((i == 1) ? diff : -diff) - 354;

						displayRegularCrowns(x + 30 + shift, y + 30, クリア[i], スコアランク[i], 0.8f);
					}
				}

			}
		}

		public void displayTowerStatus(int x, int y, int grade, float _resize)
        {
			if (grade >= 0 && TJAPlayer3.Tx.TowerResult_ScoreRankEffect != null)
			{
				TJAPlayer3.Tx.TowerResult_ScoreRankEffect.Opacity = 255;
				TJAPlayer3.Tx.TowerResult_ScoreRankEffect.vc拡大縮小倍率.X = _resize;
				TJAPlayer3.Tx.TowerResult_ScoreRankEffect.vc拡大縮小倍率.Y = _resize;
				TJAPlayer3.Tx.TowerResult_ScoreRankEffect.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x, y, new Rectangle(grade * 229, 0, 229, 194));
				TJAPlayer3.Tx.TowerResult_ScoreRankEffect.vc拡大縮小倍率.X = 1f;
				TJAPlayer3.Tx.TowerResult_ScoreRankEffect.vc拡大縮小倍率.Y = 1f;
			}
		}

		public void displayDanStatus(int x, int y, int grade, float _resize)
        {
			if (grade >= 0 && TJAPlayer3.Tx.DanResult_Rank != null)
			{
				TJAPlayer3.Tx.DanResult_Rank.Opacity = 255;
				TJAPlayer3.Tx.DanResult_Rank.vc拡大縮小倍率.X = _resize;
				TJAPlayer3.Tx.DanResult_Rank.vc拡大縮小倍率.Y = _resize;
				TJAPlayer3.Tx.DanResult_Rank.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x, y, new Rectangle(334 * (grade + 1), 0, 334, 334));
				TJAPlayer3.Tx.DanResult_Rank.vc拡大縮小倍率.X = 1f;
				TJAPlayer3.Tx.DanResult_Rank.vc拡大縮小倍率.Y = 1f;
			}
		}

		public void displayRegularCrowns(int x, int y, int[] クリア, int[] スコアランク, float _resize)
        {
			// To change to include all crowns/score ranks later

			TJAPlayer3.Tx.SongSelect_Crown.vc拡大縮小倍率.X = _resize;
			TJAPlayer3.Tx.SongSelect_Crown.vc拡大縮小倍率.Y = _resize;
			TJAPlayer3.Tx.SongSelect_ScoreRank.vc拡大縮小倍率.X = _resize;
			TJAPlayer3.Tx.SongSelect_ScoreRank.vc拡大縮小倍率.Y = _resize;

			// Other crowns
			if (クリア[3] > 0 && クリア[4] == 0)
				TJAPlayer3.Tx.SongSelect_Crown.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x, y, new RectangleF(9 * 43.2f + (クリア[3] - 1) * 43.2f, 0, 43.2f, 39));
			else if (クリア[4] > 0)
				TJAPlayer3.Tx.SongSelect_Crown.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x, y, new RectangleF(12 * 43.2f + (クリア[4] - 1) * 43.2f, 0, 43.2f, 39));

			if (スコアランク[3] > 0 && スコアランク[4] == 0)
				TJAPlayer3.Tx.SongSelect_ScoreRank.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x, y + 30, new RectangleF(0, (スコアランク[3] - 1) * 42.71f, 50, 42.71f));
			else if (スコアランク[4] > 0)
				TJAPlayer3.Tx.SongSelect_ScoreRank.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, x, y + 30, new RectangleF(0, (スコアランク[4] - 1) * 42.71f, 50, 42.71f));
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

		private TitleTextureKey ttk曲名テクスチャを生成する( string str文字, Color forecolor, Color backcolor, CPrivateFastFont pf)
        {
            return new TitleTextureKey(str文字, pf, forecolor, backcolor, 550);
        }

	    private TitleTextureKey ttkサブタイトルテクスチャを生成する( string str文字, Color forecolor, Color backcolor)
        {
            return new TitleTextureKey(str文字, pfSubtitle, forecolor, backcolor, 510);
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

		#region [Yes I do code bloating, what will you do ? =)]

		public CTexture ResolveTitleTextureTate(TitleTextureKey titleTextureKey)
		{
			if (!_titledictionary.TryGetValue(titleTextureKey, out var texture))
			{
				texture = GenerateTitleTextureTate(titleTextureKey);
				_titledictionary.Add(titleTextureKey, texture);
			}

			return texture;
		}

		private static CTexture GenerateTitleTextureTate(TitleTextureKey titleTextureKey)
		{
			using (var bmp = new Bitmap(titleTextureKey.cPrivateFastFont.DrawPrivateFont(
				titleTextureKey.str文字, titleTextureKey.forecolor, titleTextureKey.backcolor, true)))
			{
				CTexture tx文字テクスチャ = TJAPlayer3.tテクスチャの生成(bmp, false);
				if (tx文字テクスチャ.szテクスチャサイズ.Width > titleTextureKey.maxWidth)
				{
					tx文字テクスチャ.vc拡大縮小倍率.X = (float)(((double)titleTextureKey.maxWidth) / tx文字テクスチャ.szテクスチャサイズ.Width);
					tx文字テクスチャ.vc拡大縮小倍率.Y = (float)(((double)titleTextureKey.maxWidth) / tx文字テクスチャ.szテクスチャサイズ.Width);
				}

				return tx文字テクスチャ;
			}
		}

		#endregion

		private static CTexture GenerateTitleTexture(TitleTextureKey titleTextureKey)
	    {
			using (var bmp = new Bitmap(titleTextureKey.cPrivateFastFont.DrawPrivateFont(
	            titleTextureKey.str文字, titleTextureKey.forecolor, titleTextureKey.backcolor, titleTextureKey.secondEdge)))
	        {
	            CTexture tx文字テクスチャ = TJAPlayer3.tテクスチャの生成(bmp, false);
	            if (tx文字テクスチャ.szテクスチャサイズ.Width > titleTextureKey.maxWidth)
	            {
	                tx文字テクスチャ.vc拡大縮小倍率.X = (float) (((double) titleTextureKey.maxWidth) / tx文字テクスチャ.szテクスチャサイズ.Width);
	                tx文字テクスチャ.vc拡大縮小倍率.Y = (float) (((double) titleTextureKey.maxWidth) / tx文字テクスチャ.szテクスチャサイズ.Width);
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
	        public readonly CPrivateFastFont cPrivateFastFont;
	        public readonly Color forecolor;
	        public readonly Color backcolor;
	        public readonly int maxWidth;
			public readonly Color? secondEdge;

	        public TitleTextureKey(string str文字, CPrivateFastFont cPrivateFastFont, Color forecolor, Color backcolor, int maxHeight, Color? secondEdge = null)
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
		private void t小文字表示(int x, int y, string str)
		{
			foreach (char ch in str)
			{
				for (int i = 0; i < this.st小文字位置.Length; i++)
				{
					if (this.st小文字位置[i].ch == ch)
					{
						Rectangle rectangle = new Rectangle(this.st小文字位置[i].ptX, 0, 21, 25);
						if (TJAPlayer3.Tx.SongSelect_Level_Number != null)
						{
							if (str.Length > 1) TJAPlayer3.Tx.SongSelect_Level_Number.vc拡大縮小倍率.X = 0.8f;
							else TJAPlayer3.Tx.SongSelect_Level_Number.vc拡大縮小倍率.X = 1.0f;
							TJAPlayer3.Tx.SongSelect_Level_Number.t2D描画(TJAPlayer3.app.Device, x, y, rectangle);
						}
						break;
					}
				}
				x += 11;
			}
		}
		//-----------------
		#endregion
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
