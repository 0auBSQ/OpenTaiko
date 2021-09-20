using System;
using System.Collections.Generic;
using System.Text;
using FDK;
using System.Diagnostics;

namespace TJAPlayer3
{
    internal class CAct演奏演奏情報 : CActivity
    {
        // プロパティ

        public double dbBPM;
        public readonly int[] NowMeasure = new int[2];
        public double dbSCROLL;

		// コンストラクタ

		public CAct演奏演奏情報()
		{
			base.b活性化してない = true;
		}

				
		// CActivity 実装

		public override void On活性化()
		{
            for (int i = 0; i < 2; i++)
            {
                NowMeasure[i] = 0;
            }
			this.dbBPM = TJAPlayer3.DTX.BASEBPM;
            this.dbSCROLL = 1.0;
			base.On活性化();
		}
		public override int On進行描画()
		{
			throw new InvalidOperationException( "t進行描画(int x, int y) のほうを使用してください。" );
		}
		public void t進行描画( int x, int y )
		{
			if ( !base.b活性化してない )
			{
				y += 0x153;
				TJAPlayer3.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "BGM/Taiko Adj: {0:####0}/{1:####0} ms", TJAPlayer3.DTX.nBGMAdjust, TJAPlayer3.ConfigIni.nInputAdjustTimeMs ) );
				y -= 0x10;
				int num = ( TJAPlayer3.DTX.listChip.Count > 0 ) ? TJAPlayer3.DTX.listChip[ TJAPlayer3.DTX.listChip.Count - 1 ].n発声時刻ms : 0;
				string str = "Time:          " + ((((double)(CSound管理.rc演奏用タイマ.n現在時刻 * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0))) / 1000.0)).ToString("####0.00") + " / " + ((((double)num) / 1000.0)).ToString("####0.00");
				TJAPlayer3.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, str );
				y -= 0x10;
				TJAPlayer3.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "Part:          {0:####0}/{1:####0}", NowMeasure[0], NowMeasure[1] ) );
				y -= 0x10;
				TJAPlayer3.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "BPM:           {0:####0.0000}", this.dbBPM ) );
				y -= 0x10;
				TJAPlayer3.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "Frame:         {0:####0} fps", TJAPlayer3.FPS.n現在のFPS ) );
				y -= 0x10;
				TJAPlayer3.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "NoteN:         {0:####0}", TJAPlayer3.DTX.nノーツ数[0] ) );
				y -= 0x10;
				TJAPlayer3.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "NoteE:         {0:####0}", TJAPlayer3.DTX.nノーツ数[1] ) );
				y -= 0x10;
				TJAPlayer3.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "NoteM:         {0:####0}", TJAPlayer3.DTX.nノーツ数[2] ) );
				y -= 0x10;
				TJAPlayer3.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "NoteC:         {0:####0}", TJAPlayer3.DTX.nノーツ数[3] ) );
				y -= 0x10;
				TJAPlayer3.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "SCROLL:        {0:####0.00}", this.dbSCROLL ) );
                y -= 0x10;
                TJAPlayer3.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "SCOREMODE:     {0:####0}", TJAPlayer3.DTX.nScoreModeTmp ) );
                y -= 0x10;
                TJAPlayer3.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "SCROLLMODE:    {0:####0}", Enum.GetName(typeof(EScrollMode), TJAPlayer3.ConfigIni.eScrollMode ) ) );


				//CDTXMania.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "Sound CPU :    {0:####0.00}%", CDTXMania.Sound管理.GetCPUusage() ) );
				//y -= 0x10;
				//CDTXMania.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "Sound Mixing:  {0:####0}", CDTXMania.Sound管理.GetMixingStreams() ) );
				//y -= 0x10;
				//CDTXMania.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "Sound Streams: {0:####0}", CDTXMania.Sound管理.GetStreams() ) );
				//y -= 0x10;
			}
		}
	}
}
