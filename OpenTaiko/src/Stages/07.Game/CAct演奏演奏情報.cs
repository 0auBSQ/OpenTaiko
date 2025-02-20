using FDK;

namespace OpenTaiko;

internal class CAct演奏演奏情報 : CActivity {
	// Properties

	public double[] dbBPM = new double[5];
	public readonly int[] NowMeasure = new int[5];
	public double dbSCROLL;
	public int[] _chipCounts = new int[2];

	// コンストラクタ

	public CAct演奏演奏情報() {
		base.IsDeActivated = true;
	}


	// CActivity 実装

	public override void Activate() {
		for (int i = 0; i < 5; i++) {
			NowMeasure[i] = 0;
			this.dbBPM[i] = OpenTaiko.TJA.BASEBPM;
		}
		this.dbSCROLL = 1.0;

		_chipCounts[0] = OpenTaiko.TJA.listChip.Where(num => NotesManager.IsMissableNote(num)).Count();
		_chipCounts[1] = OpenTaiko.TJA.listChip_Branch[2].Where(num => NotesManager.IsMissableNote(num)).Count();

		NotesTextN = string.Format("NoteN:         {0:####0}", OpenTaiko.TJA.nノーツ数_Branch[0]);
		NotesTextE = string.Format("NoteE:         {0:####0}", OpenTaiko.TJA.nノーツ数_Branch[1]);
		NotesTextM = string.Format("NoteM:         {0:####0}", OpenTaiko.TJA.nノーツ数_Branch[2]);
		NotesTextC = string.Format("NoteC:         {0:####0}", OpenTaiko.TJA.nノーツ数[3]);
		ScoreModeText = string.Format("SCOREMODE:     {0:####0}", OpenTaiko.TJA.nScoreModeTmp);
		ListChipText = string.Format("ListChip:      {0:####0}", _chipCounts[0]);
		ListChipMText = string.Format("ListChipM:     {0:####0}", _chipCounts[1]);

		base.Activate();
	}
	public override int Draw() {
		int dx = OpenTaiko.actTextConsole.fontWidth;
		int dy = OpenTaiko.actTextConsole.fontHeight;
		int x = OpenTaiko.Skin.Resolution[0] - 8 - 34 * dx;
		int y = 404 * OpenTaiko.Skin.Resolution[1] / 720;
		if (!base.IsDeActivated) {
			y += (13 - 1) * dy;
			OpenTaiko.actTextConsole.Print(x, y, CTextConsole.EFontType.White, string.Format("Song/G. Offset:{0:####0}/{1:####0} ms", OpenTaiko.TJA.nBGMAdjust, OpenTaiko.ConfigIni.nGlobalOffsetMs));
			y -= dy;
			int num = (OpenTaiko.TJA.listChip.Count > 0) ? OpenTaiko.TJA.listChip[OpenTaiko.TJA.listChip.Count - 1].n発声時刻ms : 0;
			string str = "Time:          " + (OpenTaiko.TJA.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs) / 1000.0).ToString("####0.00") + " / " + ((((double)num) / 1000.0)).ToString("####0.00");
			OpenTaiko.actTextConsole.Print(x, y, CTextConsole.EFontType.White, str);
			y -= dy;
			OpenTaiko.actTextConsole.Print(x, y, CTextConsole.EFontType.White, string.Format("Part:          {0:####0}/{1:####0}", NowMeasure[0], NowMeasure[1]));
			y -= dy;
			OpenTaiko.actTextConsole.Print(x, y, CTextConsole.EFontType.White, string.Format("BPM:           {0:####0.0000}", this.dbBPM[0]));
			y -= dy;
			OpenTaiko.actTextConsole.Print(x, y, CTextConsole.EFontType.White, string.Format("Frame:         {0:####0} fps", OpenTaiko.FPS.NowFPS));
			y -= dy;
			OpenTaiko.actTextConsole.Print(x, y, CTextConsole.EFontType.White, NotesTextN);
			y -= dy;
			OpenTaiko.actTextConsole.Print(x, y, CTextConsole.EFontType.White, NotesTextE);
			y -= dy;
			OpenTaiko.actTextConsole.Print(x, y, CTextConsole.EFontType.White, NotesTextM);
			y -= dy;
			OpenTaiko.actTextConsole.Print(x, y, CTextConsole.EFontType.White, NotesTextC);
			y -= dy;
			OpenTaiko.actTextConsole.Print(x, y, CTextConsole.EFontType.White, string.Format("SCROLL:        {0:####0.00}", this.dbSCROLL));
			y -= dy;
			OpenTaiko.actTextConsole.Print(x, y, CTextConsole.EFontType.White, ScoreModeText);
			y -= dy;
			OpenTaiko.actTextConsole.Print(x, y, CTextConsole.EFontType.White, ListChipText);
			y -= dy;
			OpenTaiko.actTextConsole.Print(x, y, CTextConsole.EFontType.White, ListChipMText);

			//CDTXMania.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "Sound CPU :    {0:####0.00}%", CDTXMania.Sound管理.GetCPUusage() ) );
			//y -= dy;
			//CDTXMania.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "Sound Mixing:  {0:####0}", CDTXMania.Sound管理.GetMixingStreams() ) );
			//y -= dy;
			//CDTXMania.act文字コンソール.tPrint( x, y, C文字コンソール.Eフォント種別.白, string.Format( "Sound Streams: {0:####0}", CDTXMania.Sound管理.GetStreams() ) );
			//y -= dy;
		}
		return 0;
	}

	private string NotesTextN;
	private string NotesTextE;
	private string NotesTextM;
	private string NotesTextC;
	private string ScoreModeText;
	private string ListChipText;
	private string ListChipMText;
}
