using System;
using System.IO;
using Xunit;

namespace OpenTaiko.Tests;

// .optktcm / .tcm: scene_preset and level are read from .optktcm only, the dot keys (custom commands) from both
public class DanCourseFileTests : IDisposable {
	private readonly string dir = Path.Combine(Path.GetTempPath(), "optk-tcm-" + Guid.NewGuid().ToString("N"));

	public DanCourseFileTests() => Directory.CreateDirectory(dir);

	public void Dispose() => Directory.Delete(dir, true);

	private CTcm Load(string fileName, string json) {
		string path = Path.Combine(dir, fileName);
		File.WriteAllText(path, json);
		return new CTcm(path);
	}

	[Fact]
	public void OptkTcmReadsTheScenePresets() {
		var tcm = Load("dan.optktcm", """{ "title": "Dan", "scene_preset": "Fall,Winter", "charts": [] }""");
		Assert.Equal(new[] { "Fall", "Winter" }, tcm.SCENE_PRESETS);
	}

	[Fact]
	public void PlainTcmIgnoresTheScenePresets() {
		var tcm = Load("dan.tcm", """{ "title": "Dan", "scene_preset": "Fall", "charts": [] }""");
		Assert.Empty(tcm.SCENE_PRESETS);
	}

	[Fact]
	public void NoScenePresetLeavesItEmpty() {
		Assert.Empty(Load("dan.optktcm", """{ "title": "Dan", "charts": [] }""").SCENE_PRESETS);
	}

	[Theory]
	[InlineData("dan.optktcm", """, "level": 10.6""", 10.6)]
	[InlineData("dan.optktcm", """, "level": 7""", 7.0)]
	[InlineData("dan.optktcm", "", 10.0)]
	[InlineData("dan.tcm", """, "level": 7""", 10.0)]
	public void LevelIsReadFromOptkTcmOnly(string fileName, string extra, double expected) {
		Assert.Equal(expected, Load(fileName, $$"""{ "title": "Dan"{{extra}}, "charts": [] }""").LEVEL);
	}

	[Theory]
	[InlineData("dan.optktcm")]
	[InlineData("dan.tcm")]
	public void DotKeysAreCustomCommandsOfTheDanAndItsCourse(string fileName) {
		var song = new CSongListNode { nodeType = CSongListNode.ENodeType.SCORE, uniqueId = new CSongUniqueID(Path.Combine(dir, "song-uniqueID.json")) };
		var score = new CScore();
		score.FileInfo.FileAbsolutePath = Path.Combine(dir, "song.tja");
		song.score[(int)Difficulty.Oni] = score;
		CSongDict.tAddSongNode(song.uniqueId, song);
		try {
			var tcm = Load(fileName, $$"""{ "title": "Dan", ".BADGE": "gold", ".STARS": 4, "charts": [ { "id": "{{song.uniqueId.data.id}}", "difficulty": "oni" } ] }""");
			Assert.Equal("gold", tcm.CUSTOM_COMMANDS[".BADGE"]);
			var node = tcm.BuildSongListNode()!;
			Assert.Equal("gold", node.customMetadataGScope[".BADGE"]);
			Assert.Equal("4", new LuaSongChart(null, node, (int)Difficulty.Dan).GetCustomCommand(".STARS"));
		} finally {
			CSongDict.tRemoveSongNode(song.uniqueId);
		}
	}

	[Fact]
	public void TheSongListShowsTheDanLevel() {
		var song = new CSongListNode { nodeType = CSongListNode.ENodeType.SCORE, uniqueId = new CSongUniqueID(Path.Combine(dir, "song-uniqueID.json")) };
		var score = new CScore();
		score.FileInfo.FileAbsolutePath = Path.Combine(dir, "song.tja");
		song.score[(int)Difficulty.Oni] = score;
		CSongDict.tAddSongNode(song.uniqueId, song);
		try {
			var node = Load("dan.optktcm", $$"""{ "title": "Dan", "level": 10.6, "charts": [ { "id": "{{song.uniqueId.data.id}}", "difficulty": "oni" } ] }""").BuildSongListNode()!;
			int dan = (int)Difficulty.Dan;
			Assert.Equal(10, node.nLevel[dan]);
			Assert.Equal(10.6, node.dLevel[dan]);
			Assert.Equal(CTja.ELevelIcon.ePlus, node.nLevelIcon[dan]);
			Assert.Equal(10, node.score[dan]!.ChartInfo.nLevel[dan]);
		} finally {
			CSongDict.tRemoveSongNode(song.uniqueId);
		}
	}
}
