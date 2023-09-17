﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using TJAPlayer3.C曲リストノードComparers;
using FDK;
using System.Drawing;

namespace TJAPlayer3
{
	[Serializable]
	internal class CSongs管理
	{
		// プロパティ

		/*public int nSongsDBから取得できたスコア数
		{
			get; 
			set; 
		}
		public int nSongsDBへ出力できたスコア数
		{
			get;
			set;
		}*/
		public int nスコアキャッシュから反映できたスコア数 
		{
			get;
			set; 
		}
		public int nファイルから反映できたスコア数
		{
			get;
			set;
		}
		public int n検索されたスコア数 
		{ 
			get;
			set;
		}
		public int n検索された曲ノード数
		{
			get; 
			set;
		}
		/*[NonSerialized]
		public List<Cスコア> listSongsDB;*/					// songs.dbから構築されるlist
		public List<C曲リストノード> list曲ルート;         // 起動時にフォルダ検索して構築されるlist
		public List<C曲リストノード> list曲ルート_Dan = new List<C曲リストノード>();          // 起動時にフォルダ検索して構築されるlist
		public List<C曲リストノード> list曲ルート_Tower = new List<C曲リストノード>();          // 起動時にフォルダ検索して構築されるlist
		public static List<FDK.CTexture> listCustomBGs = new List<FDK.CTexture>();
		public bool bIsSuspending							// 外部スレッドから、内部スレッドのsuspendを指示する時にtrueにする
		{													// 再開時は、これをfalseにしてから、次のautoReset.Set()を実行する
			get;
			set;
		}
		public bool bIsSlowdown								// #PREMOVIE再生時に曲検索を遅くする
		{
			get;
			set;
		}
		[NonSerialized]
		public AutoResetEvent AutoReset;
		/*public AutoResetEvent AutoReset
		{
			get
			{
				return autoReset;
			}
			private set
			{
				autoReset = value;
			}
		}*/

		private int searchCount;							// #PREMOVIE中は検索n回実行したら少しスリープする

		// コンストラクタ

		public CSongs管理()
		{
			//this.listSongsDB = new List<Cスコア>();
			this.list曲ルート = new List<C曲リストノード>();
			this.n検索された曲ノード数 = 0;
			this.n検索されたスコア数 = 0;
			this.bIsSuspending = false;						// #27060
			this.AutoReset = new AutoResetEvent( true );	// #27060
			this.searchCount = 0;
		}


		// メソッド

		#region [ Fetch song list ]
		//-----------------

		public void UpdateDownloadBox()
		{

			C曲リストノード downloadBox = null;
			for (int i = 0; i < TJAPlayer3.Songs管理.list曲ルート.Count; i++)
			{
				if (TJAPlayer3.Songs管理.list曲ルート[i].strジャンル == "Download")
				{
					downloadBox = TJAPlayer3.Songs管理.list曲ルート[i];
					if (downloadBox.r親ノード != null) downloadBox = downloadBox.r親ノード;
				}

			}

			if (downloadBox != null && downloadBox.list子リスト != null)
            {

				var flatten = TJAPlayer3.stage選曲.act曲リスト.flattenList(downloadBox.list子リスト);
				
				// Works because flattenList creates a new List
				for (int i = 0; i < downloadBox.list子リスト.Count; i++)
				{
					CSongDict.tRemoveSongNode(downloadBox.list子リスト[i].uniqueId);
					downloadBox.list子リスト.Remove(downloadBox.list子リスト[i]);
					i--;
				}
				

				var path = downloadBox.arスコア[0].ファイル情報.フォルダの絶対パス;

				if (flatten.Count > 0)
				{
					int index = list曲ルート.IndexOf(flatten[0]);

					/*
					if (!list曲ルート.Contains(downloadBox))
					{
						for (int i = 0; i < flatten.Count; i++)
						{
							this.list曲ルート.Remove(flatten[i]);
						}
						list曲ルート.Insert(index, downloadBox);
					}
					*/

					if (!list曲ルート.Contains(downloadBox))
					{
						this.list曲ルート = this.list曲ルート.Except(flatten).ToList();
						list曲ルート.Insert(index, downloadBox);
					}

					t曲を検索してリストを作成する(path, true, downloadBox.list子リスト, downloadBox);
					this.t曲リストへ後処理を適用する(downloadBox.list子リスト, $"/{downloadBox.strタイトル}/");
					tSongsDBになかった曲をファイルから読み込んで反映する(downloadBox.list子リスト);
					downloadBox.list子リスト.Insert(0, CSongDict.tGenerateBackButton(downloadBox, $"/{downloadBox.strタイトル}/"));
				}
			}
			
		}
		public void t曲を検索してリストを作成する( string str基点フォルダ, bool b子BOXへ再帰する )
		{
			this.t曲を検索してリストを作成する( str基点フォルダ, b子BOXへ再帰する, this.list曲ルート, null );
		}
		private void t曲を検索してリストを作成する( string str基点フォルダ, bool b子BOXへ再帰する, List<C曲リストノード> listノードリスト, C曲リストノード node親 )
		{
			if( !str基点フォルダ.EndsWith( Path.DirectorySeparatorChar ) )
				str基点フォルダ = str基点フォルダ + Path.DirectorySeparatorChar;

			DirectoryInfo info = new DirectoryInfo( str基点フォルダ );

			if( TJAPlayer3.ConfigIni.bLog曲検索ログ出力 )
				Trace.TraceInformation( "基点フォルダ: " + str基点フォルダ );

			#region [ a.フォルダ内に set.def が存在する場合 → 1フォルダ内のtjaファイル無制限]
			//-----------------------------
			string path = str基点フォルダ + "set.def";
			if( File.Exists( path ) )
			{
				new FileInfo( path );
				if( TJAPlayer3.ConfigIni.bLog曲検索ログ出力 )
				{
					Trace.TraceInformation( "set.def検出 : {0}", path );
					Trace.Indent();
				}
				try
				{
                    foreach( FileInfo fileinfo in info.GetFiles() )
                    {
					    SlowOrSuspendSearchTask();
                        #region[ 拡張子を取得 ]
					    string strExt = fileinfo.Extension.ToLower();
                        #endregion
                        if( ( strExt.Equals( ".tja" ) || strExt.Equals( ".dtx" ) ) )
                        {
                            if( strExt.Equals( ".tja" ) )
                            {
                                //tja、dtxが両方存在していた場合、tjaを読み込まずにtjaと同名のdtxだけを使う。
                                string dtxscoreini = str基点フォルダ + ( fileinfo.Name.Replace( strExt, ".dtx" ) );
                                if( File.Exists( dtxscoreini ) )
                                {
                                    continue;
                                }
                            }

                            #region[ 新処理 ]

                            CDTX dtx = new CDTX( fileinfo.FullName, false, 1.0, 0, 1 );

                            C曲リストノード c曲リストノード = new C曲リストノード();
                            c曲リストノード.eノード種別 = C曲リストノード.Eノード種別.SCORE;

                            bool b = false;
                            for( int n = 0; n < (int)Difficulty.Total; n++ )
                            {
                                if( dtx.b譜面が存在する[ n ] )
                                {
                                    c曲リストノード.nスコア数++;
                                    c曲リストノード.r親ノード = node親;
                                    c曲リストノード.strBreadcrumbs = ( c曲リストノード.r親ノード == null ) ?
                                    str基点フォルダ + fileinfo.Name : c曲リストノード.r親ノード.strBreadcrumbs + " > " + str基点フォルダ + fileinfo.Name;

                                    c曲リストノード.strタイトル = dtx.TITLE;
                                    c曲リストノード.strサブタイトル = dtx.SUBTITLE;
                                    c曲リストノード.strジャンル = dtx.GENRE;
									c曲リストノード.strMaker = dtx.MAKER;
                                    c曲リストノード.nSide = dtx.SIDE;
                                    c曲リストノード.bExplicit = dtx.EXPLICIT;
                                    if (c曲リストノード.r親ノード != null && c曲リストノード.r親ノード.strジャンル != "")
                                    {
                                        c曲リストノード.strジャンル = c曲リストノード.r親ノード.strジャンル;
									}
									c曲リストノード.strSelectBGPath = $@"{fileinfo.FullName}{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}{dtx.SELECTBG}";
									if (!File.Exists(c曲リストノード.strSelectBGPath)) c曲リストノード.strSelectBGPath = null;
									c曲リストノード.nLevel = dtx.LEVELtaiko;
									c曲リストノード.nLevelIcon = dtx.LEVELtaikoIcon;

                                    // LIFE here
                                    c曲リストノード.nLife = dtx.LIFE;

									c曲リストノード.nTowerType = dtx.TOWERTYPE;

									c曲リストノード.nDanTick = dtx.DANTICK;
									c曲リストノード.cDanTickColor = dtx.DANTICKCOLOR;

									// Total count of floors for a tower chart
									c曲リストノード.nTotalFloor = 0;

									for (int i = 0; i < dtx.listChip.Count; i++)
									{
										CDTX.CChip pChip = dtx.listChip[i];

										if (pChip.n整数値_内部番号 > c曲リストノード.nTotalFloor && pChip.nチャンネル番号 == 0x50) c曲リストノード.nTotalFloor = pChip.n整数値_内部番号;

									}
									c曲リストノード.nTotalFloor++;

									/*
									switch (c曲リストノード.strジャンル) 
									{
										case "J-POP":
											c曲リストノード.strジャンル = "ポップス";
											break;
										case "ゲームミュージック":
											c曲リストノード.strジャンル = "ゲームバラエティ";
											break;
										case "どうよう":
											c曲リストノード.strジャンル = "キッズ";
											break;
									}
									*/

									c曲リストノード.str本当のジャンル = c曲リストノード.strジャンル;


									c曲リストノード.arスコア[ n ] = new Cスコア();
                                    c曲リストノード.arスコア[ n ].ファイル情報.ファイルの絶対パス = str基点フォルダ + fileinfo.Name;
                                    c曲リストノード.arスコア[ n ].ファイル情報.フォルダの絶対パス = str基点フォルダ;
                                    c曲リストノード.arスコア[ n ].ファイル情報.ファイルサイズ = fileinfo.Length;
                                    c曲リストノード.arスコア[ n ].ファイル情報.最終更新日時 = fileinfo.LastWriteTime;
                                    string strFileNameScoreIni = c曲リストノード.arスコア[ n ].ファイル情報.ファイルの絶対パス + ".score.ini";
                                    if( File.Exists( strFileNameScoreIni ) )
                                    {
                                        FileInfo infoScoreIni = new FileInfo( strFileNameScoreIni );
                                        c曲リストノード.arスコア[ n ].ScoreIni情報.ファイルサイズ = infoScoreIni.Length;
                                        c曲リストノード.arスコア[ n ].ScoreIni情報.最終更新日時 = infoScoreIni.LastWriteTime;
                                    }
                                    if( b == false )
                                    {
                                        this.n検索されたスコア数++;
                                        listノードリスト.Add( c曲リストノード );
                                        this.n検索された曲ノード数++;
                                        b = true;
                                    }
                                }
                            }
                            dtx = null;
                        }
                        #endregion
                    }
				}
				finally
				{
					if( TJAPlayer3.ConfigIni.bLog曲検索ログ出力 )
					{
						Trace.Unindent();
					}
				}
			}
			//-----------------------------
			#endregion

			#region [ b.フォルダ内に set.def が存在しない場合 → 個別ファイルからノード作成 ]
			//-----------------------------
            else
			{
				foreach( FileInfo fileinfo in info.GetFiles() )
				{
					SlowOrSuspendSearchTask();		// #27060 中断要求があったら、解除要求が来るまで待機, #PREMOVIE再生中は検索負荷を落とす
					string strExt = fileinfo.Extension.ToLower();

                    if( ( strExt.Equals( ".tja" ) || strExt.Equals( ".dtx" ) ) )
                    {
                        // 2017.06.02 kairera0467 廃止。
                        //if( strExt.Equals( ".tja" ) )
                        //{
                        //    //tja、dtxが両方存在していた場合、tjaを読み込まずにdtxだけ使う。
                        //    string[] dtxscoreini = Directory.GetFiles( str基点フォルダ, "*.dtx");
                        //    if(dtxscoreini.Length != 0 )
                        //    {
                        //        continue;
                        //    }
                        //}

                        #region[ 新処理 ]
                        CDTX dtx = new CDTX( str基点フォルダ + fileinfo.Name, false, 1.0, 0, 0 );
                        C曲リストノード c曲リストノード = new C曲リストノード();
                        c曲リストノード.eノード種別 = C曲リストノード.Eノード種別.SCORE;

                        bool b = false;
                        for( int n = 0; n < (int)Difficulty.Total; n++ )
                        {
                            if( dtx.b譜面が存在する[ n ] )
                            {
                                c曲リストノード.nスコア数++;
                                c曲リストノード.r親ノード = node親;
                                c曲リストノード.strBreadcrumbs = ( c曲リストノード.r親ノード == null ) ?
                                    str基点フォルダ + fileinfo.Name : c曲リストノード.r親ノード.strBreadcrumbs + " > " + str基点フォルダ + fileinfo.Name;

                                c曲リストノード.strタイトル = dtx.TITLE;
                                c曲リストノード.strサブタイトル = dtx.SUBTITLE;
								c曲リストノード.strMaker = dtx.MAKER;
                                c曲リストノード.nSide = dtx.SIDE;
                                c曲リストノード.bExplicit = dtx.EXPLICIT;

                                if (dtx.List_DanSongs != null)
									c曲リストノード.DanSongs = dtx.List_DanSongs;

								if (dtx.Dan_C != null)
									c曲リストノード.Dan_C = dtx.Dan_C;

								if (!string.IsNullOrEmpty(dtx.GENRE))
								{
									if(c曲リストノード.r親ノード != null)
									{
										c曲リストノード.strジャンル = c曲リストノード.r親ノード.strジャンル;
										c曲リストノード.str本当のジャンル = dtx.GENRE;
									}
                                    else
									{
										c曲リストノード.strジャンル = dtx.GENRE;
										c曲リストノード.str本当のジャンル = dtx.GENRE;
									}
								}
								else
								{
									c曲リストノード.strジャンル = c曲リストノード.r親ノード.strジャンル;
									c曲リストノード.str本当のジャンル = c曲リストノード.r親ノード.strジャンル;
								}

								if (c曲リストノード.strSelectBGPath == null || !File.Exists(str基点フォルダ + dtx.SELECTBG))
								{
									c曲リストノード.strSelectBGPath = c曲リストノード.r親ノード.strSelectBGPath;
								}
								else
								{
									c曲リストノード.strSelectBGPath = str基点フォルダ + dtx.SELECTBG;
								}
								if (!File.Exists(c曲リストノード.strSelectBGPath)) c曲リストノード.strSelectBGPath = null;

								if (c曲リストノード.r親ノード != null)
                                {
									c曲リストノード.strScenePreset = c曲リストノード.r親ノード.strScenePreset;
                                    if (c曲リストノード.r親ノード.IsChangedForeColor)
                                    {
                                        c曲リストノード.ForeColor = c曲リストノード.r親ノード.ForeColor;
                                        c曲リストノード.IsChangedForeColor = true;
                                    }
                                    if (c曲リストノード.r親ノード.IsChangedBackColor)
                                    {
                                        c曲リストノード.BackColor = c曲リストノード.r親ノード.BackColor;
                                        c曲リストノード.IsChangedBackColor = true;
                                    }
									if (c曲リストノード.r親ノード.isChangedBoxColor)
                                    {
										c曲リストノード.BoxColor = c曲リストノード.r親ノード.BoxColor;
										c曲リストノード.isChangedBoxColor = true;
                                    }
									if (c曲リストノード.r親ノード.isChangedBgColor)
									{
										c曲リストノード.BgColor = c曲リストノード.r親ノード.BgColor;
										c曲リストノード.isChangedBgColor = true;
									}
									if (c曲リストノード.r親ノード.isChangedBgType)
                                    {
										c曲リストノード.BgType = c曲リストノード.r親ノード.BgType;
										c曲リストノード.isChangedBgType = true;
									}
									if (c曲リストノード.r親ノード.isChangedBoxType)
                                    {
										c曲リストノード.BoxType = c曲リストノード.r親ノード.BoxType;
										c曲リストノード.isChangedBoxType = true;
									}
									if (c曲リストノード.r親ノード.isChangedBoxChara)
									{
										c曲リストノード.BoxChara = c曲リストノード.r親ノード.BoxChara;
										c曲リストノード.isChangedBoxChara = true;
									}
									
										
                                }


                                switch (CStrジャンルtoNum.ForAC15(c曲リストノード.strジャンル))
                                {
                                    case 0:
                                        c曲リストノード.ForeColor = TJAPlayer3.Skin.SongSelect_ForeColor_JPOP;
                                        c曲リストノード.BackColor = TJAPlayer3.Skin.SongSelect_BackColor_JPOP;
                                        break;
                                    case 1:
                                        c曲リストノード.ForeColor = TJAPlayer3.Skin.SongSelect_ForeColor_Anime;
                                        c曲リストノード.BackColor = TJAPlayer3.Skin.SongSelect_BackColor_Anime;
                                        break;
                                    case 2:
                                        c曲リストノード.ForeColor = TJAPlayer3.Skin.SongSelect_ForeColor_VOCALOID;
                                        c曲リストノード.BackColor = TJAPlayer3.Skin.SongSelect_BackColor_VOCALOID;
                                        break;
                                    case 3:
                                        c曲リストノード.ForeColor = TJAPlayer3.Skin.SongSelect_ForeColor_Children;
                                        c曲リストノード.BackColor = TJAPlayer3.Skin.SongSelect_BackColor_Children;
                                        break;
                                    case 4:
                                        c曲リストノード.ForeColor = TJAPlayer3.Skin.SongSelect_ForeColor_Variety;
                                        c曲リストノード.BackColor = TJAPlayer3.Skin.SongSelect_BackColor_Variety;
                                        break;
                                    case 5:
                                        c曲リストノード.ForeColor = TJAPlayer3.Skin.SongSelect_ForeColor_Classic;
                                        c曲リストノード.BackColor = TJAPlayer3.Skin.SongSelect_BackColor_Classic;
                                        break;
                                    case 6:
                                        c曲リストノード.ForeColor = TJAPlayer3.Skin.SongSelect_ForeColor_GameMusic;
                                        c曲リストノード.BackColor = TJAPlayer3.Skin.SongSelect_BackColor_GameMusic;
                                        break;
                                    case 7:
                                        c曲リストノード.ForeColor = TJAPlayer3.Skin.SongSelect_ForeColor_Namco;
                                        c曲リストノード.BackColor = TJAPlayer3.Skin.SongSelect_BackColor_Namco;
                                        break;
                                    default:
                                        break;
                                }


                                c曲リストノード.nLevel = dtx.LEVELtaiko;
                                c曲リストノード.nLevelIcon = dtx.LEVELtaikoIcon;
                                c曲リストノード.uniqueId = dtx.uniqueID;

								CSongDict.tAddSongNode(c曲リストノード.uniqueId, c曲リストノード);

								c曲リストノード.arスコア[ n ] = new Cスコア();
                                c曲リストノード.arスコア[ n ].ファイル情報.ファイルの絶対パス = str基点フォルダ + fileinfo.Name;
                                c曲リストノード.arスコア[ n ].ファイル情報.フォルダの絶対パス = str基点フォルダ;
                                c曲リストノード.arスコア[ n ].ファイル情報.ファイルサイズ = fileinfo.Length;
                                c曲リストノード.arスコア[ n ].ファイル情報.最終更新日時 = fileinfo.LastWriteTime;

                                if (c曲リストノード.r親ノード != null && String.IsNullOrEmpty(c曲リストノード.arスコア[n].譜面情報.Preimage))
                                {
                                    c曲リストノード.arスコア[n].譜面情報.Preimage = c曲リストノード.r親ノード.arスコア[0].譜面情報.Preimage;
                                }

                                string strFileNameScoreIni = c曲リストノード.arスコア[ n ].ファイル情報.ファイルの絶対パス + ".score.ini";
                                if( File.Exists( strFileNameScoreIni ) )
                                {
                                    FileInfo infoScoreIni = new FileInfo( strFileNameScoreIni );
                                    c曲リストノード.arスコア[ n ].ScoreIni情報.ファイルサイズ = infoScoreIni.Length;
                                    c曲リストノード.arスコア[ n ].ScoreIni情報.最終更新日時 = infoScoreIni.LastWriteTime;
                                }
                                if( b == false )
                                {
                                    this.n検索されたスコア数++;
                                    listノードリスト.Add( c曲リストノード );
                                    this.n検索された曲ノード数++;
                                    b = true;
                                }

                                if( TJAPlayer3.ConfigIni.bLog曲検索ログ出力 )
                                {
                                //    Trace.Indent();
                                //    try
                                //    {
                                //        StringBuilder sb = new StringBuilder( 0x100 );
                                //        sb.Append( string.Format( "nID#{0:D3}", c曲リストノード.nID ) );
                                //        if( c曲リストノード.r親ノード != null )
                                //        {
                                //            sb.Append( string.Format( "(in#{0:D3}):", c曲リストノード.r親ノード.nID ) );
                                //        }
                                //        else
                                //        {
                                //            sb.Append( "(onRoot):" );
                                //        }
                                //        sb.Append( " SONG, File=" + c曲リストノード.arスコア[ 0 ].ファイル情報.ファイルの絶対パス );
                                //        sb.Append( ", Size=" + c曲リストノード.arスコア[ 0 ].ファイル情報.ファイルサイズ );
                                //        sb.Append( ", LastUpdate=" + c曲リストノード.arスコア[ 0 ].ファイル情報.最終更新日時 );
                                //        Trace.TraceInformation( sb.ToString() );
                                //    }
                                //    finally
                                //    {
                                //        Trace.Unindent();
                                //    }
                                }
                            }
                        }
                        #endregion
                    }
				}
			}
			//-----------------------------
			#endregion

			foreach( DirectoryInfo infoDir in info.GetDirectories() )
			{
				SlowOrSuspendSearchTask();		// #27060 中断要求があったら、解除要求が来るまで待機, #PREMOVIE再生中は検索負荷を落とす

		
				#region [ a.box.def を含むフォルダの場合  ]
				//-----------------------------
				if( File.Exists( infoDir.FullName + @$"{Path.DirectorySeparatorChar}box.def" ) )
				{
					CBoxDef boxdef = new CBoxDef( infoDir.FullName + @$"{Path.DirectorySeparatorChar}box.def" );
					C曲リストノード c曲リストノード = new C曲リストノード();
					c曲リストノード.eノード種別 = C曲リストノード.Eノード種別.BOX;
					c曲リストノード.bDTXFilesで始まるフォルダ名のBOXである = false;
					c曲リストノード.strタイトル = boxdef.Title;
					c曲リストノード.strジャンル = boxdef.Genre;
                    c曲リストノード.strScenePreset = boxdef.ScenePreset;
                    c曲リストノード.strSelectBGPath = infoDir.FullName + Path.DirectorySeparatorChar + boxdef.SelectBG;
					if (!File.Exists(c曲リストノード.strSelectBGPath)) c曲リストノード.strSelectBGPath = null;

					if (boxdef.IsChangedForeColor)
                    {
                        c曲リストノード.ForeColor = boxdef.ForeColor;
                        c曲リストノード.IsChangedForeColor = true;
                    }
                    if (boxdef.IsChangedBackColor)
                    {
                        c曲リストノード.BackColor = boxdef.BackColor;
                        c曲リストノード.IsChangedBackColor = true;
                    }
					if (boxdef.IsChangedBoxColor)
                    {
						c曲リストノード.BoxColor = boxdef.BoxColor;
						c曲リストノード.isChangedBoxColor = true;
                    }
					if (boxdef.IsChangedBgColor)
					{
						c曲リストノード.BgColor = boxdef.BgColor;
						c曲リストノード.isChangedBgColor = true;
					}
					if (boxdef.IsChangedBgType)
					{
						c曲リストノード.BgType = boxdef.BgType;
						c曲リストノード.isChangedBgType = true;
					}
					if (boxdef.IsChangedBoxType)
					{
						c曲リストノード.BoxType = boxdef.BoxType;
						c曲リストノード.isChangedBoxType = true;
					}
					if (boxdef.IsChangedBoxChara)
					{
						c曲リストノード.BoxChara = boxdef.BoxChara;
						c曲リストノード.isChangedBoxChara = true;
					}
                    


                    for (int i = 0; i < 3; i++)
					{
						if ((boxdef.strBoxText[i] != null) && (boxdef.strBoxText[i].Length > 0))
						{
							c曲リストノード.strBoxText[i] = boxdef.strBoxText[i];
						}
					}
					switch (CStrジャンルtoNum.ForAC15(c曲リストノード.strジャンル))
                    {
                        case 0:
                            c曲リストノード.ForeColor = TJAPlayer3.Skin.SongSelect_ForeColor_JPOP;
                            c曲リストノード.BackColor = TJAPlayer3.Skin.SongSelect_BackColor_JPOP;
                            break;
                        case 1:
                            c曲リストノード.ForeColor = TJAPlayer3.Skin.SongSelect_ForeColor_Anime;
                            c曲リストノード.BackColor = TJAPlayer3.Skin.SongSelect_BackColor_Anime;
                            break;
                        case 2:
                            c曲リストノード.ForeColor = TJAPlayer3.Skin.SongSelect_ForeColor_VOCALOID;
                            c曲リストノード.BackColor = TJAPlayer3.Skin.SongSelect_BackColor_VOCALOID;
                            break;
                        case 3:
                            c曲リストノード.ForeColor = TJAPlayer3.Skin.SongSelect_ForeColor_Children;
                            c曲リストノード.BackColor = TJAPlayer3.Skin.SongSelect_BackColor_Children;
                            break;
                        case 4:
                            c曲リストノード.ForeColor = TJAPlayer3.Skin.SongSelect_ForeColor_Variety;
                            c曲リストノード.BackColor = TJAPlayer3.Skin.SongSelect_BackColor_Variety;
                            break;
                        case 5:
                            c曲リストノード.ForeColor = TJAPlayer3.Skin.SongSelect_ForeColor_Classic;
                            c曲リストノード.BackColor = TJAPlayer3.Skin.SongSelect_BackColor_Classic;
                            break;
                        case 6:
                            c曲リストノード.ForeColor = TJAPlayer3.Skin.SongSelect_ForeColor_GameMusic;
                            c曲リストノード.BackColor = TJAPlayer3.Skin.SongSelect_BackColor_GameMusic;
                            break;
                        case 7:
                            c曲リストノード.ForeColor = TJAPlayer3.Skin.SongSelect_ForeColor_Namco;
                            c曲リストノード.BackColor = TJAPlayer3.Skin.SongSelect_BackColor_Namco;
                            break;
                        default:
                            break;
                    }



                    c曲リストノード.nスコア数 = 1;
					c曲リストノード.arスコア[ 0 ] = new Cスコア();
					c曲リストノード.arスコア[ 0 ].ファイル情報.フォルダの絶対パス = infoDir.FullName + Path.DirectorySeparatorChar;
					c曲リストノード.arスコア[ 0 ].譜面情報.タイトル = boxdef.Title;
					c曲リストノード.arスコア[ 0 ].譜面情報.ジャンル = boxdef.Genre;
                    if (!String.IsNullOrEmpty(boxdef.DefaultPreimage))
                        c曲リストノード.arスコア[0].譜面情報.Preimage = boxdef.DefaultPreimage;
                    c曲リストノード.r親ノード = node親;
                    

                    c曲リストノード.strBreadcrumbs = ( c曲リストノード.r親ノード == null ) ?
						c曲リストノード.strタイトル : c曲リストノード.r親ノード.strBreadcrumbs + " > " + c曲リストノード.strタイトル;
	
					
					c曲リストノード.list子リスト = new List<C曲リストノード>();
					listノードリスト.Add( c曲リストノード );
					if( TJAPlayer3.ConfigIni.bLog曲検索ログ出力 )
					{
						Trace.TraceInformation( "box.def検出 : {0}", infoDir.FullName + @$"{Path.DirectorySeparatorChar}box.def" );
						Trace.Indent();
						try
						{
							StringBuilder sb = new StringBuilder( 0x400 );
							sb.Append( string.Format( "nID#{0:D3}", c曲リストノード.nID ) );
							if( c曲リストノード.r親ノード != null )
							{
								sb.Append( string.Format( "(in#{0:D3}):", c曲リストノード.r親ノード.nID ) );
							}
							else
							{
								sb.Append( "(onRoot):" );
							}
							sb.Append( "BOX, Title=" + c曲リストノード.strタイトル );
							if( ( c曲リストノード.strジャンル != null ) && ( c曲リストノード.strジャンル.Length > 0 ) )
							{
								sb.Append( ", Genre=" + c曲リストノード.strジャンル );
							}
                            if (c曲リストノード.IsChangedForeColor)
                            {
                                sb.Append(", ForeColor=" + c曲リストノード.ForeColor.ToString());
                            }
                            if (c曲リストノード.IsChangedBackColor)
                            {
                                sb.Append(", BackColor=" + c曲リストノード.BackColor.ToString());
                            }
							if (c曲リストノード.isChangedBoxColor)
                            {
								sb.Append(", BoxColor=" + c曲リストノード.BoxColor.ToString());
                            }
							if (c曲リストノード.isChangedBgColor)
							{
								sb.Append(", BgColor=" + c曲リストノード.BgColor.ToString());
							}
							if (c曲リストノード.isChangedBoxType)
							{
								sb.Append(", BoxType=" + c曲リストノード.BoxType.ToString());
							}
							if (c曲リストノード.isChangedBgType)
							{
								sb.Append(", BgType=" + c曲リストノード.BgType.ToString());
							}
							if (c曲リストノード.isChangedBoxChara)
							{
								sb.Append(", BoxChara=" + c曲リストノード.BoxChara.ToString());
							}
							Trace.TraceInformation( sb.ToString() );
						}
						finally
						{
							Trace.Unindent();
						}
					}
					if( b子BOXへ再帰する )
					{
						this.t曲を検索してリストを作成する( infoDir.FullName + Path.DirectorySeparatorChar, b子BOXへ再帰する, c曲リストノード.list子リスト, c曲リストノード );
					}
				}
				//-----------------------------
				#endregion

				#region [ c.通常フォルダの場合 ]
				//-----------------------------
				else
				{
					this.t曲を検索してリストを作成する( infoDir.FullName + Path.DirectorySeparatorChar, b子BOXへ再帰する, listノードリスト, node親 );
				}
				//-----------------------------
				#endregion
			}
		}
		//-----------------
		#endregion
		
		#region [ SongsDBになかった曲をファイルから読み込んで反映する ]
		//-----------------
		public void tSongsDBになかった曲をファイルから読み込んで反映する()
		{
			this.nファイルから反映できたスコア数 = 0;
			this.tSongsDBになかった曲をファイルから読み込んで反映する( this.list曲ルート );
		}
		private void tSongsDBになかった曲をファイルから読み込んで反映する( List<C曲リストノード> ノードリスト )
		{
			foreach( C曲リストノード c曲リストノード in ノードリスト )
			{
				SlowOrSuspendSearchTask();		// #27060 中断要求があったら、解除要求が来るまで待機, #PREMOVIE再生中は検索負荷を落とす

				if( c曲リストノード.eノード種別 == C曲リストノード.Eノード種別.BOX )
				{
					this.tSongsDBになかった曲をファイルから読み込んで反映する( c曲リストノード.list子リスト );
				}
				else if( ( c曲リストノード.eノード種別 == C曲リストノード.Eノード種別.SCORE )
					  || ( c曲リストノード.eノード種別 == C曲リストノード.Eノード種別.SCORE_MIDI ) )
				{
					for( int i = 0; i < (int)Difficulty.Total; i++ )
					{
						if( ( c曲リストノード.arスコア[ i ] != null ) && !c曲リストノード.arスコア[ i ].bSongDBにキャッシュがあった )
						{
							#region [ DTX ファイルのヘッダだけ読み込み、Cスコア.譜面情報 を設定する ]
							//-----------------
							string path = c曲リストノード.arスコア[ i ].ファイル情報.ファイルの絶対パス;
							if( File.Exists( path ) )
							{
								try
								{
									CDTX cdtx = new CDTX( c曲リストノード.arスコア[ i ].ファイル情報.ファイルの絶対パス, true, 0, 0, 0 );
                                    if( File.Exists( c曲リストノード.arスコア[ i ].ファイル情報.フォルダの絶対パス + "set.def" ) )
									    cdtx = new CDTX( c曲リストノード.arスコア[ i ].ファイル情報.ファイルの絶対パス, true, 0, 0, 1 );

									c曲リストノード.arスコア[ i ].譜面情報.タイトル = cdtx.TITLE;
                                    
									
                                    c曲リストノード.arスコア[ i ].譜面情報.アーティスト名 = cdtx.ARTIST;
									c曲リストノード.arスコア[ i ].譜面情報.コメント = cdtx.COMMENT;
									c曲リストノード.arスコア[ i ].譜面情報.ジャンル = cdtx.GENRE;
									if (!String.IsNullOrEmpty(cdtx.PREIMAGE))
										c曲リストノード.arスコア[ i ].譜面情報.Preimage = cdtx.PREIMAGE;
									c曲リストノード.arスコア[ i ].譜面情報.Presound = cdtx.PREVIEW;
									c曲リストノード.arスコア[ i ].譜面情報.Backgound = ( ( cdtx.BACKGROUND != null ) && ( cdtx.BACKGROUND.Length > 0 ) ) ? cdtx.BACKGROUND : cdtx.BACKGROUND_GR;
									c曲リストノード.arスコア[ i ].譜面情報.レベル.Drums = cdtx.LEVEL.Drums;
									c曲リストノード.arスコア[ i ].譜面情報.レベル.Guitar = cdtx.LEVEL.Guitar;
									c曲リストノード.arスコア[ i ].譜面情報.レベル.Bass = cdtx.LEVEL.Bass;
									c曲リストノード.arスコア[ i ].譜面情報.レベルを非表示にする = cdtx.HIDDENLEVEL;
									c曲リストノード.arスコア[i].譜面情報.Bpm = cdtx.BPM;
									c曲リストノード.arスコア[i].譜面情報.BaseBpm = cdtx.BASEBPM;
									c曲リストノード.arスコア[i].譜面情報.MinBpm = cdtx.MinBPM;
									c曲リストノード.arスコア[i].譜面情報.MaxBpm = cdtx.MaxBPM;
									c曲リストノード.arスコア[ i ].譜面情報.Duration = 0;	//  (cdtx.listChip == null)? 0 : cdtx.listChip[ cdtx.listChip.Count - 1 ].n発声時刻ms;
                                    c曲リストノード.arスコア[ i ].譜面情報.strBGMファイル名 = cdtx.strBGM_PATH;
                                    c曲リストノード.arスコア[ i ].譜面情報.SongVol = cdtx.SongVol;
                                    c曲リストノード.arスコア[ i ].譜面情報.SongLoudnessMetadata = cdtx.SongLoudnessMetadata;
								    c曲リストノード.arスコア[ i ].譜面情報.nデモBGMオフセット = cdtx.nデモBGMオフセット;
                                    c曲リストノード.arスコア[ i ].譜面情報.strサブタイトル = cdtx.SUBTITLE;
									for (int k = 0; k < (int)Difficulty.Total; k++)
									{
                                        c曲リストノード.arスコア[i].譜面情報.b譜面分岐[k] = cdtx.bHIDDENBRANCH ? false : cdtx.bHasBranch[k];
                                        c曲リストノード.arスコア[i].譜面情報.nレベル[k] = cdtx.LEVELtaiko[k];
                                        c曲リストノード.arスコア[i].譜面情報.nLevelIcon[k] = cdtx.LEVELtaikoIcon[k];
                                    }

                                    // Tower Lives
                                    c曲リストノード.arスコア[i].譜面情報.nLife = cdtx.LIFE;

									c曲リストノード.arスコア[i].譜面情報.nTowerType = cdtx.TOWERTYPE;

									c曲リストノード.arスコア[i].譜面情報.nDanTick = cdtx.DANTICK;
									c曲リストノード.arスコア[i].譜面情報.cDanTickColor = cdtx.DANTICKCOLOR;

									c曲リストノード.arスコア[i].譜面情報.nTotalFloor = 0;
									for (int k = 0; k < cdtx.listChip.Count; k++)
									{
										CDTX.CChip pChip = cdtx.listChip[k];

										if (pChip.n整数値_内部番号 > c曲リストノード.arスコア[i].譜面情報.nTotalFloor && pChip.nチャンネル番号 == 0x50)
											c曲リストノード.arスコア[i].譜面情報.nTotalFloor = pChip.n整数値_内部番号;
									}
									c曲リストノード.arスコア[i].譜面情報.nTotalFloor++;



									this.nファイルから反映できたスコア数++;
									cdtx.DeActivate();
//Debug.WriteLine( "★" + this.nファイルから反映できたスコア数 + " " + c曲リストノード.arスコア[ i ].譜面情報.タイトル );
									#region [ 曲検索ログ出力 ]
									//-----------------
									if( TJAPlayer3.ConfigIni.bLog曲検索ログ出力 )
									{
										StringBuilder sb = new StringBuilder( 0x400 );
										sb.Append( string.Format( "曲データファイルから譜面情報を転記しました。({0})", path ) );
										sb.Append( "(title=" + c曲リストノード.arスコア[ i ].譜面情報.タイトル );
										sb.Append( ", artist=" + c曲リストノード.arスコア[ i ].譜面情報.アーティスト名 );
										sb.Append( ", comment=" + c曲リストノード.arスコア[ i ].譜面情報.コメント );
										sb.Append( ", genre=" + c曲リストノード.arスコア[ i ].譜面情報.ジャンル );
										sb.Append( ", preimage=" + c曲リストノード.arスコア[ i ].譜面情報.Preimage );
										sb.Append( ", premovie=" + c曲リストノード.arスコア[ i ].譜面情報.Premovie );
										sb.Append( ", presound=" + c曲リストノード.arスコア[ i ].譜面情報.Presound );
										sb.Append( ", background=" + c曲リストノード.arスコア[ i ].譜面情報.Backgound );
										sb.Append( ", lvDr=" + c曲リストノード.arスコア[ i ].譜面情報.レベル.Drums );
										sb.Append( ", lvGt=" + c曲リストノード.arスコア[ i ].譜面情報.レベル.Guitar );
										sb.Append( ", lvBs=" + c曲リストノード.arスコア[ i ].譜面情報.レベル.Bass );
										sb.Append( ", lvHide=" + c曲リストノード.arスコア[ i ].譜面情報.レベルを非表示にする );
										sb.Append( ", type=" + c曲リストノード.arスコア[ i ].譜面情報.曲種別 );
										sb.Append(", bpm=" + c曲リストノード.arスコア[i].譜面情報.Bpm);
										sb.Append(", basebpm=" + c曲リストノード.arスコア[i].譜面情報.BaseBpm);
										sb.Append(", minbpm=" + c曲リストノード.arスコア[i].譜面情報.MinBpm);
										sb.Append(", maxbpm=" + c曲リストノード.arスコア[i].譜面情報.MaxBpm);
										//	sb.Append( ", duration=" + c曲リストノード.arスコア[ i ].譜面情報.Duration );
										Trace.TraceInformation( sb.ToString() );
									}
									//-----------------
									#endregion
								}
								catch( Exception exception )
								{
									Trace.TraceError( exception.ToString() );
									c曲リストノード.arスコア[ i ] = null;
									c曲リストノード.nスコア数--;
									this.n検索されたスコア数--;
									Trace.TraceError( "曲データファイルの読み込みに失敗しました。({0})", path );
								}
							}
							//-----------------
							#endregion

							#region [ 対応する .score.ini が存在していれば読み込み、Cスコア.譜面情報 に追加設定する ]
							//-----------------
                            try
                            {
								var scoreIniPath = c曲リストノード.arスコア[i].ファイル情報.ファイルの絶対パス;// + ".score.ini";

                                if( File.Exists( scoreIniPath ) )
                                {
									this.tScoreIniを読み込んで譜面情報を設定する(scoreIniPath, c曲リストノード.arスコア[i]);
								}
								// Legacy save files from DTX mania
								/*
                                else
                                {
                                    string[] dtxscoreini = Directory.GetFiles(c曲リストノード.arスコア[i].ファイル情報.フォルダの絶対パス, "*.dtx.score.ini");
                                    if (dtxscoreini.Length != 0 && File.Exists(dtxscoreini[0]))
                                    {
                                        this.tScoreIniを読み込んで譜面情報を設定する(dtxscoreini[0], c曲リストノード.arスコア[i]);
                                    }
                                }
								*/
                            }
                            catch (Exception e)
                            {
                                Trace.TraceError( e.ToString() );
                                Trace.TraceError( "例外が発生しましたが処理を継続します。 (c8b6538c-46a1-403e-8cc3-fc7e7ff914fb)" );
                            }

							//-----------------
							#endregion
						}
					}
				}
			}
		}
		//-----------------
		#endregion

		#region [ 曲リストへ後処理を適用する ]
		//-----------------
		public void t曲リストへ後処理を適用する()
		{
			listStrBoxDefSkinSubfolderFullName = new List<string>();
			if ( TJAPlayer3.Skin.strBoxDefSkinSubfolders != null )
			{
				foreach ( string b in TJAPlayer3.Skin.strBoxDefSkinSubfolders )
				{
					listStrBoxDefSkinSubfolderFullName.Add( b );
				}
			}

			// Removed the pre-made recently played songs folder, so players will have total control on it's shape and visuals

			/*
			#region [ "最近遊んだ曲"BOXを生成する ]

			if(list曲ルート.Count > 0)
			{
				C曲リストノード crecentryplaysong = new C曲リストノード();
				crecentryplaysong.eノード種別 = C曲リストノード.Eノード種別.BOX;

				// 最近あそんだ曲
				crecentryplaysong.strタイトル = CLangManager.LangInstance.GetString(201);

				crecentryplaysong.strBoxText[0] = "";
				crecentryplaysong.strBoxText[1] = CLangManager.LangInstance.GetString(202);
				crecentryplaysong.strBoxText[2] = "";

				crecentryplaysong.strジャンル = "最近遊んだ曲";
				crecentryplaysong.nスコア数 = 1;
				crecentryplaysong.list子リスト = new List<C曲リストノード>();
				crecentryplaysong.BackColor = ColorTranslator.FromHtml("#164748");
				crecentryplaysong.BoxColor = Color.White;
				crecentryplaysong.BgColor = Color.White;

				crecentryplaysong.arスコア[0] = new Cスコア();
				crecentryplaysong.arスコア[0].ファイル情報.フォルダの絶対パス = "";
				crecentryplaysong.arスコア[0].譜面情報.タイトル = crecentryplaysong.strタイトル;
				crecentryplaysong.arスコア[0].譜面情報.コメント =
					(CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "ja") ?
					"最近遊んだ曲" :
					"Recentry play songs";

				list曲ルート.Add(crecentryplaysong);
			}

			#endregion
			*/

			this.t曲リストへ後処理を適用する(this.list曲ルート);

			foreach (C曲リストノード c曲リストノード in list曲ルート)
			{
				if (c曲リストノード.eノード種別 == C曲リストノード.Eノード種別.BOX)
				{
					if (c曲リストノード.strジャンル == "段位道場")
					{
						if (TJAPlayer3.ConfigIni.bDanTowerHide)
							list曲ルート.Remove(c曲リストノード);

						// Add to dojo
						list曲ルート_Dan = c曲リストノード.list子リスト;
						/*
						for (int i = 0; i < c曲リストノード.list子リスト.Count; i++)
						{
							if(c曲リストノード.list子リスト[i].eノード種別 == C曲リストノード.Eノード種別.SCORE)
							{
								list曲ルート_Dan.Add(c曲リストノード.list子リスト[i]);
								continue;
							}
						}
						*/
					}
					else if (c曲リストノード.strジャンル == "太鼓タワー")
					{
						if (TJAPlayer3.ConfigIni.bDanTowerHide)
							list曲ルート.Remove(c曲リストノード);

						list曲ルート_Tower = c曲リストノード.list子リスト;
					}
					else
					{
						for (int i = 0; i < c曲リストノード.list子リスト.Count; i++)
                        {
							if(c曲リストノード.list子リスト[i].arスコア[6] != null)
							{
								list曲ルート_Dan.Add(c曲リストノード.list子リスト[i]);

								if (TJAPlayer3.ConfigIni.bDanTowerHide)
									c曲リストノード.list子リスト.Remove(c曲リストノード.list子リスト[i]);
								
								continue;
							}
							if (c曲リストノード.list子リスト[i].arスコア[5] != null)
							{
								list曲ルート_Tower.Add(c曲リストノード.list子リスト[i]);

								if (TJAPlayer3.ConfigIni.bDanTowerHide)
									c曲リストノード.list子リスト.Remove(c曲リストノード.list子リスト[i]);

								continue;
							}
						}
					}
				}
                else
				{
					// ???????

					/*
					if (c曲リストノード.arスコア[5] != null)
					{
						c曲リストノード.list子リスト.Remove(c曲リストノード);
						list曲ルート_Dan.Add(c曲リストノード);
						continue;
					}
					*/
				}
			}

			#region [ skin名で比較して、systemスキンとboxdefスキンに重複があれば、boxdefスキン側を削除する ]
			string[] systemSkinNames = CSkin.GetSkinName( TJAPlayer3.Skin.strSystemSkinSubfolders );
			List<string> l = new List<string>( listStrBoxDefSkinSubfolderFullName );
			foreach ( string boxdefSkinSubfolderFullName in l )
			{
				if ( Array.BinarySearch( systemSkinNames,
					CSkin.GetSkinName( boxdefSkinSubfolderFullName ),
					StringComparer.InvariantCultureIgnoreCase ) >= 0 )
				{
					listStrBoxDefSkinSubfolderFullName.Remove( boxdefSkinSubfolderFullName );
				}
			}
			#endregion
			string[] ba = listStrBoxDefSkinSubfolderFullName.ToArray();
			Array.Sort( ba );
			TJAPlayer3.Skin.strBoxDefSkinSubfolders = ba;
		}


		private void t曲リストへ後処理を適用する( List<C曲リストノード> ノードリスト, string parentName = "/", bool isGlobal = true )
		{
			
			if (isGlobal && ノードリスト.Count > 0)
            {
				var randomNode = CSongDict.tGenerateRandomButton(ノードリスト[0].r親ノード, parentName);
				ノードリスト.Add(randomNode);

			}


			// Don't sort songs if the folder isn't global
			// Call back reinsert back folders if sort called ?
			if (isGlobal)
			{
				#region [ Sort nodes ]
				//-----------------------------
				if (TJAPlayer3.ConfigIni.nDefaultSongSort == 0)
				{
					t曲リストのソート1_絶対パス順(ノードリスト);
				}
				else if (TJAPlayer3.ConfigIni.nDefaultSongSort == 1)
				{
					t曲リストのソート9_ジャンル順(ノードリスト, E楽器パート.TAIKO, 1, 0);
				}
				else if (TJAPlayer3.ConfigIni.nDefaultSongSort == 2)
				{
					t曲リストのソート9_ジャンル順(ノードリスト, E楽器パート.TAIKO, 2, 0);
				}
				//-----------------------------
				#endregion
			}

			// すべてのノードについて…
			foreach ( C曲リストノード c曲リストノード in ノードリスト )
			{
				SlowOrSuspendSearchTask();      // #27060 中断要求があったら、解除要求が来るまで待機, #PREMOVIE再生中は検索負荷を落とす

				#region [ Append "Back" buttons to the included folders ]
				//-----------------------------
				if ( c曲リストノード.eノード種別 == C曲リストノード.Eノード種別.BOX )
				{

					#region [ Sort child nodes ]
					//-----------------------------
					if (TJAPlayer3.ConfigIni.nDefaultSongSort == 0)
					{
						t曲リストのソート1_絶対パス順(c曲リストノード.list子リスト);
					}
					else if (TJAPlayer3.ConfigIni.nDefaultSongSort == 1)
					{
						t曲リストのソート9_ジャンル順(c曲リストノード.list子リスト, E楽器パート.TAIKO, 1, 0);
					}
					else if (TJAPlayer3.ConfigIni.nDefaultSongSort == 2)
					{
						t曲リストのソート9_ジャンル順(c曲リストノード.list子リスト, E楽器パート.TAIKO, 2, 0);
					}
					//-----------------------------
					#endregion


					string newPath = parentName + c曲リストノード.strタイトル + "/";

					CSongDict.tReinsertBackButtons(c曲リストノード, c曲リストノード.list子リスト, newPath, listStrBoxDefSkinSubfolderFullName);

					// Process subfolders recussively
					t曲リストへ後処理を適用する(c曲リストノード.list子リスト, newPath, false);

					continue;
				}

				//-----------------------------
				#endregion

				#region [ If no node title found, try to fetch it within the score objects ]
				//-----------------------------
				if ( string.IsNullOrEmpty( c曲リストノード.strタイトル ) )
				{
					for( int j = 0; j < (int)Difficulty.Total; j++ )
					{
						if( ( c曲リストノード.arスコア[ j ] != null ) && !string.IsNullOrEmpty( c曲リストノード.arスコア[ j ].譜面情報.タイトル ) )
						{
							c曲リストノード.strタイトル = c曲リストノード.arスコア[ j ].譜面情報.タイトル;

							if( TJAPlayer3.ConfigIni.bLog曲検索ログ出力 )
								Trace.TraceInformation( "タイトルを設定しました。(nID#{0:D3}, title={1})", c曲リストノード.nID, c曲リストノード.strタイトル );

							break;
						}
					}
				}
				//-----------------------------
				#endregion



			}

		}
		//-----------------
		#endregion

		// Songs DB here

		/*#region [ スコアキャッシュをSongsDBに出力する ]
		//-----------------
		public void tスコアキャッシュをSongsDBに出力する( string SongsDBファイル名 )
		{
			this.nSongsDBへ出力できたスコア数 = 0;
			try
			{
				BinaryWriter bw = new BinaryWriter( new FileStream( SongsDBファイル名, FileMode.Create, FileAccess.Write ) );
				bw.Write( SONGSDB_VERSION );
				this.tSongsDBにリストを１つ出力する( bw, this.list曲ルート );
				bw.Close();
			}
			catch (Exception e)
			{
				Trace.TraceError( "songs.dbの出力に失敗しました。" );
				Trace.TraceError( e.ToString() );
				Trace.TraceError( "例外が発生しましたが処理を継続します。 (ca70d133-f092-4351-8ebd-0906d8f1cffa)" );
			}
		}
		private void tSongsDBにノードを１つ出力する( BinaryWriter bw, C曲リストノード node )
		{
			for( int i = 0; i < (int)Difficulty.Total; i++ )
			{
				// ここではsuspendに応じないようにしておく(深い意味はない。ファイルの書き込みオープン状態を長時間維持したくないだけ)
				//if ( this.bIsSuspending )		// #27060 中断要求があったら、解除要求が来るまで待機
				//{
				//	autoReset.WaitOne();
				//}

				if( node.arスコア[ i ] != null )
				{
					bw.Write( node.arスコア[ i ].ファイル情報.ファイルの絶対パス );
					bw.Write( node.arスコア[ i ].ファイル情報.フォルダの絶対パス );
					bw.Write( node.arスコア[ i ].ファイル情報.最終更新日時.Ticks );
					bw.Write( node.arスコア[ i ].ファイル情報.ファイルサイズ );
					bw.Write( node.arスコア[ i ].ScoreIni情報.最終更新日時.Ticks );
					bw.Write( node.arスコア[ i ].ScoreIni情報.ファイルサイズ );
					bw.Write( node.arスコア[ i ].譜面情報.タイトル );
					bw.Write( node.arスコア[ i ].譜面情報.アーティスト名 );
					bw.Write( node.arスコア[ i ].譜面情報.コメント );
					bw.Write( node.arスコア[ i ].譜面情報.ジャンル );
					bw.Write( node.arスコア[ i ].譜面情報.Preimage );
					bw.Write( node.arスコア[ i ].譜面情報.Premovie );
					bw.Write( node.arスコア[ i ].譜面情報.Presound );
					bw.Write( node.arスコア[ i ].譜面情報.Backgound );
					bw.Write( node.arスコア[ i ].譜面情報.レベル.Drums );
					bw.Write( node.arスコア[ i ].譜面情報.レベル.Guitar );
					bw.Write( node.arスコア[ i ].譜面情報.レベル.Bass );
					bw.Write( node.arスコア[ i ].譜面情報.最大ランク.Drums );
					bw.Write( node.arスコア[ i ].譜面情報.最大ランク.Guitar );
					bw.Write( node.arスコア[ i ].譜面情報.最大ランク.Bass );
					bw.Write( node.arスコア[ i ].譜面情報.最大スキル.Drums );
					bw.Write( node.arスコア[ i ].譜面情報.最大スキル.Guitar );
					bw.Write( node.arスコア[ i ].譜面情報.最大スキル.Bass );
					bw.Write( node.arスコア[ i ].譜面情報.フルコンボ.Drums );
					bw.Write( node.arスコア[ i ].譜面情報.フルコンボ.Guitar );
					bw.Write( node.arスコア[ i ].譜面情報.フルコンボ.Bass );
					bw.Write( node.arスコア[ i ].譜面情報.演奏回数.Drums );
					bw.Write( node.arスコア[ i ].譜面情報.演奏回数.Guitar );
					bw.Write( node.arスコア[ i ].譜面情報.演奏回数.Bass );
					bw.Write( node.arスコア[ i ].譜面情報.演奏履歴.行1 );
					bw.Write( node.arスコア[ i ].譜面情報.演奏履歴.行2 );
					bw.Write( node.arスコア[ i ].譜面情報.演奏履歴.行3 );
					bw.Write( node.arスコア[ i ].譜面情報.演奏履歴.行4 );
					bw.Write( node.arスコア[ i ].譜面情報.演奏履歴.行5 );
                    bw.Write(node.arスコア[i].譜面情報.演奏履歴.行6);
                    bw.Write(node.arスコア[i].譜面情報.演奏履歴.行7);
                    bw.Write( node.arスコア[ i ].譜面情報.レベルを非表示にする );
					bw.Write( (int) node.arスコア[ i ].譜面情報.曲種別 );
					bw.Write( node.arスコア[ i ].譜面情報.Bpm );
					bw.Write( node.arスコア[ i ].譜面情報.Duration );
                    bw.Write( node.arスコア[ i ].譜面情報.strBGMファイル名 );
                    bw.Write( node.arスコア[ i ].譜面情報.SongVol );
				    var songLoudnessMetadata = node.arスコア[ i ].譜面情報.SongLoudnessMetadata;
				    bw.Write( songLoudnessMetadata.HasValue );
                    bw.Write( songLoudnessMetadata?.Integrated.ToDouble() ?? 0.0 );
                    bw.Write( songLoudnessMetadata?.TruePeak.HasValue ?? false );
                    bw.Write( songLoudnessMetadata?.TruePeak?.ToDouble() ?? 0.0 );
				    bw.Write( node.arスコア[ i ].譜面情報.nデモBGMオフセット );
                    bw.Write( node.arスコア[ i ].譜面情報.b譜面分岐[0] );
                    bw.Write( node.arスコア[ i ].譜面情報.b譜面分岐[1] );
                    bw.Write( node.arスコア[ i ].譜面情報.b譜面分岐[2] );
                    bw.Write( node.arスコア[ i ].譜面情報.b譜面分岐[3] );
                    bw.Write( node.arスコア[ i ].譜面情報.b譜面分岐[4] );
                    bw.Write(node.arスコア[i].譜面情報.b譜面分岐[5]);
                    bw.Write( node.arスコア[ i ].譜面情報.b譜面分岐[6] );
                    bw.Write( node.arスコア[ i ].譜面情報.ハイスコア );
                    bw.Write( node.arスコア[ i ].譜面情報.nハイスコア[0] );
                    bw.Write( node.arスコア[ i ].譜面情報.nハイスコア[1] );
                    bw.Write( node.arスコア[ i ].譜面情報.nハイスコア[2] );
                    bw.Write( node.arスコア[ i ].譜面情報.nハイスコア[3] );
                    bw.Write( node.arスコア[ i ].譜面情報.nハイスコア[4] );
                    bw.Write(node.arスコア[i].譜面情報.nハイスコア[5]);
                    bw.Write(node.arスコア[i].譜面情報.nハイスコア[6]);
                    bw.Write( node.arスコア[ i ].譜面情報.strサブタイトル );
                    bw.Write( node.arスコア[ i ].譜面情報.nレベル[0] );
                    bw.Write( node.arスコア[ i ].譜面情報.nレベル[1] );
                    bw.Write( node.arスコア[ i ].譜面情報.nレベル[2] );
                    bw.Write( node.arスコア[ i ].譜面情報.nレベル[3] );
                    bw.Write( node.arスコア[ i ].譜面情報.nレベル[4] );
                    bw.Write(node.arスコア[i].譜面情報.nレベル[5]);
                    bw.Write(node.arスコア[i].譜面情報.nレベル[6]);
					bw.Write(node.arスコア[i].譜面情報.nクリア[0]);
					bw.Write(node.arスコア[i].譜面情報.nクリア[1]);
					bw.Write(node.arスコア[i].譜面情報.nクリア[2]);
					bw.Write(node.arスコア[i].譜面情報.nクリア[3]);
					bw.Write(node.arスコア[i].譜面情報.nクリア[4]);
					bw.Write(node.arスコア[i].譜面情報.nスコアランク[0]);
					bw.Write(node.arスコア[i].譜面情報.nスコアランク[1]);
					bw.Write(node.arスコア[i].譜面情報.nスコアランク[2]);
					bw.Write(node.arスコア[i].譜面情報.nスコアランク[3]);
					bw.Write(node.arスコア[i].譜面情報.nスコアランク[4]);
                    this.nSongsDBへ出力できたスコア数++;
				}
			}
		}
		private void tSongsDBにリストを１つ出力する( BinaryWriter bw, List<C曲リストノード> list )
		{
			foreach( C曲リストノード c曲リストノード in list )
			{
				if(    ( c曲リストノード.eノード種別 == C曲リストノード.Eノード種別.SCORE )
					|| ( c曲リストノード.eノード種別 == C曲リストノード.Eノード種別.SCORE_MIDI ) )
				{
					this.tSongsDBにノードを１つ出力する( bw, c曲リストノード );
				}
				if( c曲リストノード.list子リスト != null )
				{
					this.tSongsDBにリストを１つ出力する( bw, c曲リストノード.list子リスト );
				}
			}
		}
		//-----------------
		#endregion*/
		
		#region [ 曲リストソート ]
		//-----------------

	    public static void t曲リストのソート1_絶対パス順( List<C曲リストノード> ノードリスト )
	    {
	        t曲リストのソート1_絶対パス順(ノードリスト, E楽器パート.TAIKO, 1, 0);

	        foreach( C曲リストノード c曲リストノード in ノードリスト )
	        {
	            if( ( c曲リストノード.list子リスト != null ) && ( c曲リストノード.list子リスト.Count > 1 ) )
	            {
	                t曲リストのソート1_絶対パス順( c曲リストノード.list子リスト );
	            }
	        }
	    }

	    public static void t曲リストのソート1_絶対パス順( List<C曲リストノード> ノードリスト, E楽器パート part, int order, params object[] p )
	    {
            var comparer = new ComparerChain<C曲リストノード>(
                new C曲リストノードComparerノード種別(),
                new C曲リストノードComparer絶対パス(order),
                new C曲リストノードComparerタイトル(order),
                new C曲リストノードComparerSubtitle(order));

	        ノードリスト.Sort( comparer );
	    }

	    public static void t曲リストのソート2_タイトル順( List<C曲リストノード> ノードリスト, E楽器パート part, int order, params object[] p )
	    {
	        var comparer = new ComparerChain<C曲リストノード>(
	            new C曲リストノードComparerノード種別(),
	            new C曲リストノードComparerタイトル(order),
                new C曲リストノードComparerSubtitle(order),
                new C曲リストノードComparer絶対パス(order));

	        ノードリスト.Sort( comparer );
	    }

        public static void tSongListSortBySubtitle(List<C曲リストノード> ノードリスト, E楽器パート part, int order, params object[] p)
        {
            var comparer = new ComparerChain<C曲リストノード>(
                new C曲リストノードComparerノード種別(),
                new C曲リストノードComparerSubtitle(order),
                new C曲リストノードComparerタイトル(order),
                new C曲リストノードComparer絶対パス(order));

            ノードリスト.Sort(comparer);
        }

        public static void tSongListSortByLevel(List<C曲リストノード> ノードリスト, E楽器パート part, int order, params object[] p)
        {
            var comparer = new ComparerChain<C曲リストノード>(
                new C曲リストノードComparerノード種別(),
                new C曲リストノードComparerLevel(order),
                new C曲リストノードComparerLevelIcon(order),
                new C曲リストノードComparerタイトル(order),
                new C曲リストノードComparerSubtitle(order),
                new C曲リストノードComparer絶対パス(order));

            ノードリスト.Sort(comparer);
        }

	    public static void t曲リストのソート9_ジャンル順(List<C曲リストノード> ノードリスト, E楽器パート part, int order, params object[] p)
	    {
	        try
	        {
	            var acGenreComparer = order == 1
	                ? (IComparer<C曲リストノード>) new C曲リストノードComparerAC8_14()
	                : new C曲リストノードComparerAC15();

	            var comparer = new ComparerChain<C曲リストノード>(
	                new C曲リストノードComparerノード種別(),
	                acGenreComparer,
	                new C曲リストノードComparer絶対パス(1),
	                new C曲リストノードComparerタイトル(1));

	            ノードリスト.Sort( comparer );
	        }
	        catch (Exception ex)
	        {
	            Trace.TraceError(ex.ToString());
	            Trace.TraceError("例外が発生しましたが処理を継続します。 (bca6dda7-76ad-42fc-a415-250f52c0b17d)");
	        }
	    }

#if TEST_SORTBGM
		public static void t曲リストのソート9_BPM順( List<C曲リストノード> ノードリスト, E楽器パート part, int order, params object[] p )
		{
			order = -order;
			int nL12345 = (int) p[ 0 ];
			if ( part != E楽器パート.UNKNOWN )
			{
				ノードリスト.Sort( delegate( C曲リストノード n1, C曲リストノード n2 )
				{
        #region [ 共通処理 ]
					if ( n1 == n2 )
					{
						return 0;
					}
					int num = this.t比較0_共通( n1, n2 );
					if ( num != 0 )
					{
						return num;
					}
					if ( ( n1.eノード種別 == C曲リストノード.Eノード種別.BOX ) && ( n2.eノード種別 == C曲リストノード.Eノード種別.BOX ) )
					{
						return order * n1.arスコア[ 0 ].ファイル情報.フォルダの絶対パス.CompareTo( n2.arスコア[ 0 ].ファイル情報.フォルダの絶対パス );
					}
        #endregion
					double dBPMn1 = 0.0, dBPMn2 = 0.0;
					if ( n1.arスコア[ nL12345 ] != null )
					{
						dBPMn1 = n1.arスコア[ nL12345 ].譜面情報.bpm;
					}
					if ( n2.arスコア[ nL12345 ] != null )
					{
						dBPMn2 = n2.arスコア[ nL12345 ].譜面情報.bpm;
					}
					double d = dBPMn1- dBPMn2;
					if ( d != 0 )
					{
						return order * System.Math.Sign( d );
					}
					return order * n1.strタイトル.CompareTo( n2.strタイトル );
				} );
				foreach ( C曲リストノード c曲リストノード in ノードリスト )
				{
					double dBPM = 0;
					if ( c曲リストノード.arスコア[ nL12345 ] != null )
					{
						dBPM = c曲リストノード.arスコア[ nL12345 ].譜面情報.bpm;
					}
Debug.WriteLine( dBPM + ":" + c曲リストノード.strタイトル );
				}
			}
		}
#endif
        //-----------------
        #endregion

        #region [ .score.ini を読み込んで Cスコア.譜面情報に設定する ]
        //-----------------
        public void tScoreIniを読み込んで譜面情報を設定する( string strScoreIniファイルパス, Cスコア score )
		{
			// New format
			string[] fp =
			{
				//strScoreIniファイルパス + "1P.score.ini",
				//strScoreIniファイルパス + "2P.score.ini",
                strScoreIniファイルパス + TJAPlayer3.SaveFileInstances[0].name + @".score.ini",
                strScoreIniファイルパス + TJAPlayer3.SaveFileInstances[1].name + @".score.ini",
                strScoreIniファイルパス + TJAPlayer3.SaveFileInstances[2].name + @".score.ini",
                strScoreIniファイルパス + TJAPlayer3.SaveFileInstances[3].name + @".score.ini",
                strScoreIniファイルパス + TJAPlayer3.SaveFileInstances[4].name + @".score.ini"
            };

			// Load legacy format if new doesn't exist yet
			if (!File.Exists(fp[0]))
				fp[0] = strScoreIniファイルパス + ".score.ini";


			/*
			if ( !File.Exists( strScoreIniファイルパス ) )
				return;
			*/

			// Select the main file for the common informations
			/*
			int mainFile = 0;
			if (!File.Exists(fp[0]))
				mainFile = 1;
			if (!File.Exists(fp[1]) && mainFile == 1)
				return;
			*/

			// Only the necessary scores are read from the auxilliary score file
			//int auxFile = mainFile ^ 1;

			try
			{
				//var ini = new CScoreIni( strScoreIniファイルパス );

				CScoreIni[] csi =
				{
					//new CScoreIni(fp[mainFile]),
					//File.Exists(fp[auxFile]) ? new CScoreIni(fp[auxFile]) : null,
                    new CScoreIni(fp[0]),
                    new CScoreIni(fp[1]),
                    new CScoreIni(fp[2]),
                    new CScoreIni(fp[3]),
                    new CScoreIni(fp[4])
                };

				var ini = csi[0];

				ini.t全演奏記録セクションの整合性をチェックし不整合があればリセットする();
				csi[1]?.t全演奏記録セクションの整合性をチェックし不整合があればリセットする();

				for ( int n楽器番号 = 0; n楽器番号 < 3; n楽器番号++ )
				{
					int n = ( n楽器番号 * 2 ) + 1;	// n = 0～5

					#region score.譜面情報.最大ランク[ n楽器番号 ] = ... 
					//-----------------
					if( ini.stセクション[ n ].b演奏にMIDI入力を使用した ||
						ini.stセクション[ n ].b演奏にキーボードを使用した ||
						ini.stセクション[ n ].b演奏にジョイパッドを使用した ||
						ini.stセクション[ n ].b演奏にマウスを使用した )
					{
						// (A) 全オートじゃないようなので、演奏結果情報を有効としてランクを算出する。

						score.譜面情報.最大ランク[ n楽器番号 ] =
							CScoreIni.tランク値を計算して返す( 
								ini.stセクション[ n ].n全チップ数,
								ini.stセクション[ n ].nPerfect数, 
								ini.stセクション[ n ].nGreat数,
								ini.stセクション[ n ].nGood数, 
								ini.stセクション[ n ].nPoor数,
								ini.stセクション[ n ].nMiss数 );
					}
					else
					{
						// (B) 全オートらしいので、ランクは無効とする。

						score.譜面情報.最大ランク[ n楽器番号 ] = (int) CScoreIni.ERANK.UNKNOWN;
					}
					//-----------------
					#endregion

					score.譜面情報.最大スキル[ n楽器番号 ] = ini.stセクション[ n ].db演奏型スキル値;
					score.譜面情報.フルコンボ[ n楽器番号 ] = ini.stセクション[ n ].bフルコンボである;
				}

				// Legacy
				score.譜面情報.ハイスコア = (int)ini.stセクション.HiScoreDrums.nスコア;
				score.譜面情報.nクリア = ini.stセクション.HiScoreDrums.nクリア;
				score.譜面情報.nスコアランク = ini.stセクション.HiScoreDrums.nスコアランク;

				for (int i = 0; i < (int)Difficulty.Total; i++)
				{
					score.譜面情報.nハイスコア[i] = (int)ini.stセクション.HiScoreDrums.nハイスコア[i];
				}

				// Load GPInfo for each save file
				for (int i = 0; i < 5; i++)
                {
					if (csi[i] == null)
						continue;

					score.GPInfo[i].nClear = csi[i].stセクション.HiScoreDrums.nクリア;
					score.GPInfo[i].nScoreRank = csi[i].stセクション.HiScoreDrums.nスコアランク;

					for (int j = 0; j < (int)Difficulty.Total; j++)
					{
						score.GPInfo[i].nHighScore[j] = (int)csi[i].stセクション.HiScoreDrums.nハイスコア[j];
					}
                }

				score.譜面情報.演奏回数.Drums = ini.stファイル.PlayCountDrums;
				score.譜面情報.演奏回数.Guitar = ini.stファイル.PlayCountGuitar;
				score.譜面情報.演奏回数.Bass = ini.stファイル.PlayCountBass;
				for( int i = 0; i < (int)Difficulty.Total; i++ )
					score.譜面情報.演奏履歴[ i ] = ini.stファイル.History[ i ];
			}
			catch (Exception e)
			{
				Trace.TraceError( "演奏記録ファイルの読み込みに失敗しました。[{0}]", strScoreIniファイルパス );
				Trace.TraceError( e.ToString() );
				Trace.TraceError( "例外が発生しましたが処理を継続します。 (801f823d-a952-4809-a1bb-cf6a56194f5c)" );
			}
		}
		//-----------------
		#endregion

		// その他


		#region [ private ]
		//-----------------
		//private const string SONGSDB_VERSION = "SongsDB5";
		public List<string> listStrBoxDefSkinSubfolderFullName
        {
			get;
			private set;
        }

		/// <summary>
		/// 検索を中断_スローダウンする
		/// </summary>
		private void SlowOrSuspendSearchTask()
		{
			if ( this.bIsSuspending )		// #27060 中断要求があったら、解除要求が来るまで待機
			{
				AutoReset.WaitOne();
			}
			if ( this.bIsSlowdown && ++this.searchCount > 10 )			// #27060 #PREMOVIE再生中は検索負荷を下げる
			{
				Thread.Sleep( 100 );
				this.searchCount = 0;
			}
		}

		//-----------------
		#endregion
	}
}
　
