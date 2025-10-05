namespace OpenTaiko {
	public class LuaPlayStateFunc {

		// Tower
		public int LastRegisteredFloor => CFloorManagement.LastRegisteredFloor;
		public int MaxNumberOfLives => CFloorManagement.MaxNumberOfLives;
		public int CurrentNumberOfLives => CFloorManagement.CurrentNumberOfLives;
		public double InvincibilityDurationSpeedDependent => CFloorManagement.InvincibilityDurationSpeedDependent;
		public int InvincibilityDuration => CFloorManagement.InvincibilityDuration;

		// General
		public bool WasPlayEndedNormally() {
			return OpenTaiko.stageGameScreen.bPreviousPlayWasEndedNormally;
		}

		public bool WasPlayAborted() {
			return !OpenTaiko.stageGameScreen.bPreviousPlayWasEndedNormally;
		}

		public int GetGoodCount(int player) {
			return OpenTaiko.stageGameScreen.CChartScore[player].nGreat;
		}

		public int GetOkCount(int player) {
			return OpenTaiko.stageGameScreen.CChartScore[player].nGood;
		}

		public int GetBadCount(int player) {
			return OpenTaiko.stageGameScreen.CChartScore[player].nMiss;
		}

		public int GetRollCount(int player) {
			return OpenTaiko.stageGameScreen.CChartScore[player].nRoll;
		}

		public int GetADLibCount(int player) {
			return OpenTaiko.stageGameScreen.CChartScore[player].nADLIB;
		}

		public int GetMissedADLibCount(int player) {
			return OpenTaiko.stageGameScreen.CChartScore[player].nADLIBMiss;
		}

		public int GetBoomCount(int player) {
			return OpenTaiko.stageGameScreen.CChartScore[player].nMine;
		}

		public int GetAvoidedBoomCount(int player) {
			return OpenTaiko.stageGameScreen.CChartScore[player].nMineAvoid;
		}

		public int GetScore(int player) {
			return OpenTaiko.stageGameScreen.CChartScore[player].nScore;
		}

		public int GetCombo(int player) {
			return OpenTaiko.stageGameScreen.CChartScore[player].nCombo;
		}

		public int GetHighestCombo(int player) {
			return OpenTaiko.stageGameScreen.CChartScore[player].nHighestCombo;
		}

		// Normal play
		public bool IsClear(int player) {
			return HGaugeMethods.UNSAFE_FastNormaCheck(player);
		}

		public bool IsAssistedClear(int player) {
			if (!IsClear(player)) return false;
			return OpenTaiko.stageSongSelect.actPlayOption.tGetModMultiplier(CActPlayOption.EBalancingType.SCORE, false, player) < 1f;
		}

		public bool IsFullCombo(int player) {
			if (!IsClear(player) || IsAssistedClear(player)) return false;
			return GetBadCount(player) == 0 && GetBoomCount(player) == 0;
		}

		public bool IsPerfect(int player) {
			if (!IsFullCombo(player) || IsAssistedClear(player)) return false;
			return GetOkCount(player) == 0;
		}

		// Tower
		public bool IsAlive() {
			return CurrentNumberOfLives > 0;
		}

		// Dan
		private Exam.Status ExamStatus() {
			return OpenTaiko.stageGameScreen.actDan.GetResultExamStatus(OpenTaiko.stageResults.st演奏記録.Drums.Dan_C, OpenTaiko.stageSongSelect.rChoosenSong.DanSongs);
		}

		public bool IsPass() {
			return ExamStatus() != Exam.Status.Failure;
		}

		public bool IsRedPass() {
			return ExamStatus() == Exam.Status.Success;
		}

		public bool IsGoldPass() {
			return ExamStatus() == Exam.Status.Better_Success;
		}

		public bool IsDanClear() {
			return IsPass() && !IsAssistedClear(0);
		}

		public bool IsDanFullCombo() {
			return IsDanClear() && GetBadCount(0) == 0 && GetBoomCount(0) == 0;
		}

		public bool IsDanPerfect() {
			return IsDanFullCombo() && GetOkCount(0) == 0;
		}


		public LuaPlayStateFunc() {
		}
	}
}
