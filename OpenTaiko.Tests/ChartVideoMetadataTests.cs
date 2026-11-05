using System;
using System.IO;
using System.Collections.Generic;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// BGMOVIE and MOVIEOFFSET reach the song node (and its cache), so Lua can play a chart's video under its song.
	[Collection("tja")]
	public class ChartVideoMetadataTests : IClassFixture<TjaFixture> {
		public ChartVideoMetadataTests(TjaFixture _) { }

		private static CTja Parse(params string[] header) {
			using TempDir dir = new("ot_movie_");
			File.WriteAllBytes(Path.Combine(dir, "clip.mp4"), new byte[] { 0 });
			string p = Path.Combine(dir, "movie.tja");
			var lines = new List<string> { "TITLE:movie", "BPM:120", "WAVE:none.ogg" };
			lines.AddRange(header);
			lines.AddRange(new[] { "COURSE:Oni", "LEVEL:5", "#START", "1000,", "#END" });
			File.WriteAllText(p, string.Join("\n", lines));
			return TjaFixture.ParseTjaFile(p, loadChart: false); // metadata only
		}

		[Theory]
		[InlineData("-1.5", -1500)]
		[InlineData("2.25", 2250)]
		[InlineData("0", 0)]
		public void MovieOffset_IsSignedMilliseconds(string offset, int expected) {
			var tja = Parse("BGMOVIE:clip.mp4", "MOVIEOFFSET:" + offset);
			Assert.Equal("clip.mp4", tja.strBGVIDEO_PATH);
			Assert.Equal(expected, tja.msMovieOffset);
		}

		[Fact]
		public void NoMovie_LeavesPathEmptyAndOffsetZero() {
			var tja = Parse();
			Assert.True(string.IsNullOrEmpty(tja.strBGVIDEO_PATH));
			Assert.Equal(0, tja.msMovieOffset);
		}

		[Fact]
		public void SongListCache_KeepsMovieFields() {
			var node = new CSongListNode { strMovieFileName = "clip.mp4", msMovieOffset = -1500, bMovie = true };
			using var ms = new MemoryStream();
			CEnumSongs.WriteSongListCache(ms, new Dictionary<string, CSongListNode> { ["a"] = node });
			ms.Position = 0;
			var read = CEnumSongs.ReadSongListCache(ms);
			Assert.NotNull(read);
			Assert.Equal("clip.mp4", read!["a"].strMovieFileName);
			Assert.Equal(-1500, read["a"].msMovieOffset);
		}
	}
}
