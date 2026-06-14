namespace OpenTaiko;

[Serializable]
public class CScoreIni {
	// Properties

	[Serializable]
	public class CPlayRecord {
		public int nOkCount;
		public int nBadCount;
		public int nGoodCount;
		public Dan_C[] Dan_C;

		public CPlayRecord() {
			Dan_C = new Dan_C[CExamInfo.cMaxExam];
		}

	}

}
