using System;
using System.Collections.Generic;
using System.Text;
using FDK;
using System.Diagnostics;
using System.Linq;

namespace TJAPlayer3
{
    internal class CAct演奏演奏情報 : CActivity
    {
        // プロパティ

        public double[] dbBPM = new double[5];
		public readonly int[] NowMeasure = new int[5];
        public double dbSCROLL;
		public int[] _chipCounts = new int[2];

		// コンストラクタ

		public CAct演奏演奏情報()
		{
			base.IsDeActivated = true;
		}

				
		// CActivity 実装

		public override void Activate()
		{
            for (int i = 0; i < 5; i++)
            {
                NowMeasure[i] = 0;
				this.dbBPM[i] = TJAPlayer3.DTX.BASEBPM;
			}
            this.dbSCROLL = 1.0;

			_chipCounts[0] = TJAPlayer3.DTX.listChip.Where(num => NotesManager.IsMissableNote(num)).Count();
			_chipCounts[1] = TJAPlayer3.DTX.listChip_Branch[2].Where(num => NotesManager.IsMissableNote(num)).Count();

			NotesTextN = string.Format( "NoteN:         {0:####0}", TJAPlayer3.DTX.nノーツ数_Branch[0]);
			NotesTextE = string.Format( "NoteE:         {0:####0}", TJAPlayer3.DTX.nノーツ数_Branch[1]);
			NotesTextM = string.Format( "NoteM:         {0:####0}", TJAPlayer3.DTX.nノーツ数_Branch[2]);
			NotesTextC = string.Format( "NoteC:         {0:####0}", TJAPlayer3.DTX.nノーツ数[3] );
			ScoreModeText = string.Format( "SCOREMODE:     {0:####0}", TJAPlayer3.DTX.nScoreModeTmp );
			ListChipText = string.Format(  "ListChip:      {0:####0}", _chipCounts[0]);
			ListChipMText = string.Format(  "ListChipM:     {0:####0}", _chipCounts[1]);

			base.Activate();
		}
		public override int Draw()
		{
			throw new InvalidOperationException( "t進行描画(int x, int y) のほうを使用してください。" );
		}
		public void t進行描画( int x, int y )
		{
			if ( !base.IsDeActivated )
			{
				y += 0x153;
				TJAPlayer3.actTextConsole.tPrint( x, y, CTextConsole.EFontType.White, string.Format( "Song/G. Offset:{0:####0}/{1:####0} ms", TJAPlayer3.DTX.nBGMAdjust, TJAPlayer3.ConfigIni.nGlobalOffsetMs ) );
				y -= 0x10;
				int num = ( TJAPlayer3.DTX.listChip.Count > 0 ) ? TJAPlayer3.DTX.listChip[ TJAPlayer3.DTX.listChip.Count - 1 ].n発声時刻ms : 0;
				string str = "Time:          " + ((((double)(SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed)) / 1000.0)).ToString("####0.00") + " / " + ((((double)num) / 1000.0)).ToString("####0.00");
				TJAPlayer3.actTextConsole.tPrint( x, y, CTextConsole.EFontType.White, str );
				y -= 0x10;
				TJAPlayer3.actTextConsole.tPrint( x, y, CTextConsole.EFontType.White, string.Format( "Part:          {0:####0}/{1:####0}", NowMeasure[0], NowMeasure[1] ) );
				y -= 0x10;
				TJAPlayer3.actTextConsole.tPrint( x, y, CTextConsole.EFontType.White, string.Format( "BPM:           {0:####0.0000}", this.dbBPM[0] ) );
				y -= 0x10;
				TJAPlayer3.actTextConsole.tPrint( x, y, CTextConsole.EFontType.White, string.Format( "Frame:         {0:####0} fps", TJAPlayer3.FPS.NowFPS ) );
				y -= 0x10;
				TJAPlayer3.actTextConsole.tPrint( x, y, CTextConsole.EFontType.White, NotesTextN);
				y -= 0x10;
				TJAPlayer3.actTextConsole.tPrint( x, y, CTextConsole.EFontType.White, NotesTextE );
				y -= 0x10;
				TJAPlayer3.actTextConsole.tPrint( x, y, CTextConsole.EFontType.White, NotesTextM );
				y -= 0x10;
				TJAPlayer3.actTextConsole.tPrint( x, y, CTextConsole.EFontType.White, NotesTextC );
				y -= 0x10;
				TJAPlayer3.actTextConsole.tPrint( x, y, CTextConsole.EFontType.White, string.Format( "SCROLL:        {0:####0.00}", this.dbSCROLL ) );
                y -= 0x10;
                TJAPlayer3.actTextConsole.tPrint( x, y, CTextConsole.EFontType.White, ScoreModeText );
                y -= 0x10;
				TJAPlayer3.actTextConsole.tPrint(x, y, CTextConsole.EFontType.White, ListChipText);
				y -= 0x10;
				TJAPlayer3.actTextConsole.tPrint(x, y, CTextConsole.EFontType.White, ListChipMText);

				//CDTXMania.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "Sound CPU :    {0:####0.00}%", CDTXMania.Sound管理.GetCPUusage() ) );
				//y -= 0x10;
				//CDTXMania.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "Sound Mixing:  {0:####0}", CDTXMania.Sound管理.GetMixingStreams() ) );
				//y -= 0x10;
				//CDTXMania.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "Sound Streams: {0:####0}", CDTXMania.Sound管理.GetStreams() ) );
				//y -= 0x10;
			}
		}

		private string NotesTextN;
		private string NotesTextE;
		private string NotesTextM;
		private string NotesTextC;
		private string ScoreModeText;
		private string ListChipText;
		private string ListChipMText;
	}
}
