namespace OpenTaiko {
	internal class LuaSongDanExam {
		private Dan_C? _dcInfo;

		public LuaSongDanExam(Dan_C? _dc) {
			_dcInfo = _dc;
		}

		#region [Exam metadata]

		public bool IsSet => _dcInfo != null && _dcInfo.ExamIsEnable;

		public int RedValue  => _dcInfo?.GetValue()[0] ?? 0;
		public int GoldValue => _dcInfo?.GetValue()[1] ?? 0;
		public int TypeAsInt => _dcInfo != null ? (int)_dcInfo.ExamType  : 0;
		public int RangeAsInt => _dcInfo != null ? (int)_dcInfo.ExamRange : 0;

		#endregion
	}
}
