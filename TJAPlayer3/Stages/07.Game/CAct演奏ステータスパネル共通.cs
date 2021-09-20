using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics;
using FDK;

namespace TJAPlayer3
{
	internal class CAct演奏ステータスパネル共通 : CActivity
	{
		// コンストラクタ
		public CAct演奏ステータスパネル共通()
		{
			this.stパネルマップ = new STATUSPANEL[ 12 ];		// yyagi: 以下、手抜きの初期化でスマン
																// { "DTXMANIA", 0 }, { "EXTREME", 1 }, ... みたいに書きたいが___
			string[] labels = new string[ 12 ] {
			    "DTXMANIA", "EXTREME", "ADVANCED", "ADVANCE", "BASIC", "RAW",
			    "REAL", "EASY", "EX-REAL", "ExREAL", "ExpertReal", "NORMAL"
			};
			int[] status = new int[ 12 ] {
			    0, 1, 2, 2, 3, 4, 5, 6, 7, 7, 7, 8
			};

			for ( int i = 0; i < 12; i++ )
			{
				this.stパネルマップ[ i ] = new STATUSPANEL();
				this.stパネルマップ[ i ].status = status[ i ];
				this.stパネルマップ[ i ].label = labels[ i ];
			}

			#region [ 旧初期化処理(注釈化) ]
			//STATUSPANEL[] statuspanelArray = new STATUSPANEL[ 12 ];
			//STATUSPANEL statuspanel = new STATUSPANEL();
			//statuspanel.status = 0;
			//statuspanel.label = "DTXMANIA";
			//statuspanelArray[ 0 ] = statuspanel;
			//STATUSPANEL statuspanel2 = new STATUSPANEL();
			//statuspanel2.status = 1;
			//statuspanel2.label = "EXTREME";
			//statuspanelArray[ 1 ] = statuspanel2;
			//STATUSPANEL statuspanel3 = new STATUSPANEL();
			//statuspanel3.status = 2;
			//statuspanel3.label = "ADVANCED";
			//statuspanelArray[ 2 ] = statuspanel3;
			//STATUSPANEL statuspanel4 = new STATUSPANEL();
			//statuspanel4.status = 2;
			//statuspanel4.label = "ADVANCE";
			//statuspanelArray[ 3 ] = statuspanel4;
			//STATUSPANEL statuspanel5 = new STATUSPANEL();
			//statuspanel5.status = 3;
			//statuspanel5.label = "BASIC";
			//statuspanelArray[ 4 ] = statuspanel5;
			//STATUSPANEL statuspanel6 = new STATUSPANEL();
			//statuspanel6.status = 4;
			//statuspanel6.label = "RAW";
			//statuspanelArray[ 5 ] = statuspanel6;
			//STATUSPANEL statuspanel7 = new STATUSPANEL();
			//statuspanel7.status = 5;
			//statuspanel7.label = "REAL";
			//statuspanelArray[ 6 ] = statuspanel7;
			//STATUSPANEL statuspanel8 = new STATUSPANEL();
			//statuspanel8.status = 6;
			//statuspanel8.label = "EASY";
			//statuspanelArray[ 7 ] = statuspanel8;
			//STATUSPANEL statuspanel9 = new STATUSPANEL();
			//statuspanel9.status = 7;
			//statuspanel9.label = "EX-REAL";
			//statuspanelArray[ 8 ] = statuspanel9;
			//STATUSPANEL statuspanel10 = new STATUSPANEL();
			//statuspanel10.status = 7;
			//statuspanel10.label = "ExREAL";
			//statuspanelArray[ 9 ] = statuspanel10;
			//STATUSPANEL statuspanel11 = new STATUSPANEL();
			//statuspanel11.status = 7;
			//statuspanel11.label = "ExpertReal";
			//statuspanelArray[ 10 ] = statuspanel11;
			//STATUSPANEL statuspanel12 = new STATUSPANEL();
			//statuspanel12.status = 8;
			//statuspanel12.label = "NORMAL";
			//statuspanelArray[ 11 ] = statuspanel12;
			//this.stパネルマップ = statuspanelArray;
			#endregion
			base.b活性化してない = true;
		}


		// メソッド

		public void tラベル名からステータスパネルを決定する( string strラベル名 )
		{
			if ( string.IsNullOrEmpty( strラベル名 ) )
			{
				this.nStatus = 0;
			}
			else
			{
				foreach ( STATUSPANEL statuspanel in this.stパネルマップ )
				{
					if ( strラベル名.Equals( statuspanel.label, StringComparison.CurrentCultureIgnoreCase ) )	// #24482 2011.2.17 yyagi ignore case
					{
						this.nStatus = statuspanel.status;
						return;
					}
				}
				this.nStatus = 0;
			}
		}

		// CActivity 実装

		public override void On活性化()
		{
			this.nStatus = 0;
			base.On活性化();
		}


		#region [ protected ]
		//-----------------
		[StructLayout( LayoutKind.Sequential )]
		protected struct STATUSPANEL
		{
			public string label;
			public int status;
		}

		protected int nStatus;
		protected STATUSPANEL[] stパネルマップ = null;
		//-----------------
		#endregion
	}
}
