using System.Collections.Generic;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// GetEarlierSongsExamStatus: every song before the last one counts towards a dan's result for a per-song exam, so a
	// dan that ended on a failed song, or with songs never played, is never saved as a pass
	public class DanResultTests {
		// a "less than" bad-count exam: under 3 passes, under 1 is gold; null amount = the song was never played
		private static Dan_C MakeExam(int? bads) {
			var exam = new Dan_C(Exam.Type.JudgeBad, new[] { 3, 1 }, Exam.Range.Less);
			if (bads is int amount) exam.Update(amount);
			return exam;
		}

		private static List<CTja.DanSongs> MakeSongs(params Dan_C?[] exams) {
			var songs = new List<CTja.DanSongs>();
			foreach (var exam in exams) {
				var song = new CTja.DanSongs();
				song.Dan_C[1] = exam;
				songs.Add(song);
			}
			return songs;
		}

		private static Exam.Status Earlier(Exam.Status from, params Dan_C?[] exams)
			=> Dan_Cert.GetEarlierSongsExamStatus(from, MakeSongs(exams), 1);

		[Fact]
		public void ExamStatuses_AreWhatTheTestsAssume() {
			Assert.Equal(Exam.Status.Better_Success, MakeExam(0).GetExamStatus());
			Assert.Equal(Exam.Status.Success, MakeExam(2).GetExamStatus());
			Assert.Equal(Exam.Status.Failure, MakeExam(3).GetExamStatus());
			Assert.Equal(Exam.Status.Failure, MakeExam(null).GetExamStatus());   // never played
		}

		[Fact]
		public void LastSong_IsLeftToTheExamsInPlay() {
			Assert.Equal(Exam.Status.Better_Success, Earlier(Exam.Status.Better_Success, MakeExam(0), MakeExam(0), MakeExam(9)));
		}

		[Fact]
		public void TakesTheLowestEarlierSong() {
			Assert.Equal(Exam.Status.Success, Earlier(Exam.Status.Better_Success, MakeExam(0), MakeExam(2), MakeExam(0)));
			Assert.Equal(Exam.Status.Failure, Earlier(Exam.Status.Better_Success, MakeExam(2), MakeExam(3), MakeExam(0)));
		}

		[Fact]
		public void AFailureAfterALowerSong_StillCounts() {
			// a failed song after a lower one still fails the exam
			Assert.Equal(Exam.Status.Failure, Earlier(Exam.Status.Better_Success, MakeExam(2), MakeExam(9), MakeExam(0)));
		}

		[Fact]
		public void ALowerStart_IsStillScanned() {
			// the earlier songs are scanned even when the status starts below Better_Success
			Assert.Equal(Exam.Status.Failure, Earlier(Exam.Status.Success, MakeExam(0), MakeExam(9), MakeExam(0)));
		}

		[Fact]
		public void SongsNeverPlayed_ReadAsFailed() {
			// a dan that ended on song 1 never reaches song 2
			Assert.Equal(Exam.Status.Failure, Earlier(Exam.Status.Better_Success, MakeExam(0), MakeExam(null), MakeExam(null)));
		}

		[Fact]
		public void SongsWithoutThisExam_AreSkipped() {
			Assert.Equal(Exam.Status.Success, Earlier(Exam.Status.Success, null, null, MakeExam(9)));
			Assert.Equal(Exam.Status.Better_Success, Earlier(Exam.Status.Better_Success, MakeExam(0)));   // single song
		}
	}
}
