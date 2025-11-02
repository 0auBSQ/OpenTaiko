namespace OpenTaiko {
	internal class LuaSongDanExam {
		private Dan_C _dcInfo;

		public LuaSongDanExam(Dan_C _dc) {
			_dcInfo = _dc;
		}

		#region [Exam metadata]

		public int RedValue {
			get {
				return _dcInfo.GetValue()[0];
			}
		}

		public int GoldValue {
			get {
				return _dcInfo.GetValue()[1];
			}
		}

		public int TypeAsInt {
			get {
				return (int)_dcInfo.ExamType;
			}
		}

		public int RangeAsInt {
			get {
				return (int)_dcInfo.ExamRange;
			}
		}

		#endregion
	}
}
