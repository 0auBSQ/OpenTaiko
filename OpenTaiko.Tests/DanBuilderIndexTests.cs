using System.Collections.Generic;
using System.Reflection;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// DANBUILDER: song indices are 0-based, exam slots 1-based (1 to 7)
	public class DanBuilderIndexTests {
		private static List<(string, int, int, bool)?[]> PerSongExams(LuaDanBuildFunc builder) =>
			(List<(string, int, int, bool)?[]>)typeof(LuaDanBuildFunc)
				.GetField("_perSongExams", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(builder)!;

		[Fact]
		public void Songs_AreReadFromIndexZero() {
			var builder = new LuaDanBuildFunc();
			var first = new LuaSongNode();
			var second = new LuaSongNode();
			builder.AddSong(first, 3);
			builder.AddSong(second, 4);

			Assert.Same(first, builder.GetSong(0));
			Assert.Same(second, builder.GetSong(1));
			Assert.Null(builder.GetSong(2));
			Assert.Null(builder.GetSong(-1));
			Assert.Equal(3, builder.GetSongDiff(0));
			Assert.Equal(4, builder.GetSongDiff(1));
			Assert.Equal(-1, builder.GetSongDiff(2));
		}

		[Fact]
		public void PerSongExams_TakeASongFromZero_AndASlotFromOne() {
			var builder = new LuaDanBuildFunc();
			builder.AddSong(new LuaSongNode(), 3);
			builder.AddSong(new LuaSongNode(), 3);

			builder.SetPerSongExam(0, 1, "jb", 50, 20, true);
			builder.SetPerSongExam(1, 7, "g", 90, 95, false);
			builder.SetPerSongExam(2, 1, "g", 1, 2, false);
			builder.SetPerSongExam(0, 0, "g", 1, 2, false);
			builder.SetPerSongExam(0, 8, "g", 1, 2, false);

			var exams = PerSongExams(builder);
			Assert.Equal(("jb", 50, 20, true), exams[0][0]);
			Assert.Equal(("g", 90, 95, false), exams[1][6]);
			Assert.Equal(1, CountSet(exams[0]));
			Assert.Equal(1, CountSet(exams[1]));
		}

		private static int CountSet((string, int, int, bool)?[] slots) {
			int n = 0;
			foreach (var slot in slots) if (slot != null) n++;
			return n;
		}
	}
}
