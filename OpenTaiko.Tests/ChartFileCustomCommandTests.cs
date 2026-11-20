using System;
using System.IO;
using System.Linq;
using Xunit;

namespace OpenTaiko.Tests;

// .optktci / .tci: dot keys at the top of the file are the song's custom commands, those of a course its own,
// as a .tja's before its first COURSE and inside one
[Collection("tja")]
public class TciCustomCommandTests : IClassFixture<OpenTaikoTests.TjaFixture>, IDisposable {
	private readonly OpenTaikoTests.TempDir dir = new("optk-tci-cmd-");

	public TciCustomCommandTests(OpenTaikoTests.TjaFixture _) { }

	public void Dispose() => dir.Dispose();

	[Fact]
	public void FileAndCourseScopes() {
		string path = Path.Combine(dir, "chart.optktci");
		File.WriteAllText(path, """
			{
				"title": "Song", "bpm": 120, ".INTRO": "intro.mp4",
				"courses": [
					{ "difficulty": "oni", "level": 9, "single": "o.osu", ".VAULT_NAME": "Old Hard" },
					{ "difficulty": "hard", "level": 6, "single": "h.osu" }
				]
			}
			""");
		var tci = new CTci(path);
		Assert.Equal("intro.mp4", tci.CUSTOM_COMMANDS[".INTRO"]);
		Assert.Equal("Old Hard", tci.Courses.First(c => c.DifficultyIndex == (int)Difficulty.Oni).CustomCommands[".VAULT_NAME"]);

		var node = tci.BuildSongListNode();
		Assert.Equal("intro.mp4", node.customMetadataGScope[".INTRO"]);
		var oni = new LuaSongChart(null, node, (int)Difficulty.Oni);
		Assert.Equal("Old Hard", oni.GetCustomCommand(".VAULT_NAME"));
		// a course's commands are its own, and the file's are not repeated in it
		Assert.Null(oni.GetCustomCommand(".INTRO"));
		Assert.Null(new LuaSongChart(null, node, (int)Difficulty.Hard).GetCustomCommand(".VAULT_NAME"));
	}
}
