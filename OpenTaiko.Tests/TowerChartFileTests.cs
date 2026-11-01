using System;
using System.IO;
using Xunit;

namespace OpenTaiko.Tests;

// .optktct / .tct: one course of any song, by unique ID, played as a tower
public class TowerChartFileTests : IDisposable {
	private readonly string dir = Path.Combine(Path.GetTempPath(), "optk-tct-" + Guid.NewGuid().ToString("N"));

	public TowerChartFileTests() => Directory.CreateDirectory(dir);

	public void Dispose() => Directory.Delete(dir, true);

	private CTct Load(string json) {
		string path = Path.Combine(dir, "tower.optktct");
		File.WriteAllText(path, json);
		return new CTct(path);
	}

	[Fact]
	public void ReadsEveryField() {
		var tct = Load("""
			{
				"title": { "default": "Tower", "ja": "塔" },
				"subtitle": "sub",
				"id": "abc-123",
				"difficulty": "hard",
				"level": 7.5,
				"life": 3,
				"side": "normal",
				"towertype": "Day"
			}
			""");
		Assert.Equal("abc-123", tct.SourceId);
		Assert.Equal((int)Difficulty.Hard, tct.SourceDifficulty);
		Assert.Equal(7.5, tct.LEVEL);
		Assert.Equal(3, tct.LIFE);
		Assert.Equal(CTja.ESide.eNormal, tct.SIDE);
		Assert.Equal("Day", tct.TOWERTYPE);
		Assert.Equal("Tower", tct.TITLE!.GetString(""));
		Assert.Equal("sub", tct.SUBTITLE!.GetString(""));
	}

	[Fact]
	public void MissingFieldsTakeTheDefaults() {
		var tct = Load("""{ "id": "abc-123" }""");
		Assert.Equal((int)Difficulty.Oni, tct.SourceDifficulty);
		Assert.Null(tct.LEVEL);
		Assert.Equal(5, tct.LIFE);
		Assert.Null(tct.SIDE);
		Assert.Null(tct.TOWERTYPE);
		Assert.Null(tct.TITLE);
		Assert.Null(tct.SUBTITLE);
	}

	[Theory]
	[InlineData("\"hard\"")]
	[InlineData("\"2\"")]
	[InlineData("2")]
	public void DifficultyIsANameOrANumberAsInTcm(string value) {
		Assert.Equal((int)Difficulty.Hard, Load($$"""{ "id": "abc-123", "difficulty": {{value}} }""").SourceDifficulty);
	}

	[Fact]
	public void KeepsAtLeastOneLife() {
		Assert.Equal(1, Load("""{ "id": "abc-123", "life": 0 }""").LIFE);
	}

	[Fact]
	public void GetsAUniqueIdOfItsOwn() {
		var tct = Load("""{ "id": "abc-123" }""");
		Assert.True(File.Exists(Path.Combine(dir, "uniqueID.json")));
		Assert.NotEqual("abc-123", tct.UniqueID.data.id);
	}
}

// a .tct over a real .tja song: the tower node and the chart played as a tower
[Collection("tja")]
public class TowerChartFromSongTests : IClassFixture<OpenTaikoTests.TjaFixture>, IDisposable {
	private readonly string dir = Path.Combine(Path.GetTempPath(), "optk-tct-song-" + Guid.NewGuid().ToString("N"));
	private readonly CSongListNode song;

	public TowerChartFromSongTests(OpenTaikoTests.TjaFixture _) {
		string songDir = Path.Combine(dir, "song") + Path.DirectorySeparatorChar;
		Directory.CreateDirectory(songDir);
		string tjaPath = songDir + "song.tja";
		File.WriteAllText(tjaPath, "TITLE:Source\nBPM:120\nWAVE:song.ogg\nPREIMAGE:jacket.png\n\nCOURSE:Oni\nLEVEL:8\n\n#START\n1010,\n2020,\n1111,\n2222,\n#END\n");

		song = new CSongListNode { nodeType = CSongListNode.ENodeType.SCORE, uniqueId = new CSongUniqueID(songDir + "uniqueID.json") };
		song.ldTitle.SetString("default", "Source");
		song.songGenre = "Genre";
		song.nLevel[(int)Difficulty.Oni] = 8;
		var score = new CScore();
		score.FileInfo.FileAbsolutePath = tjaPath;
		score.FileInfo.FolderAbsolutePath = songDir;
		score.ChartInfo.Preimage = "jacket.png";
		score.ChartInfo.strBGMFileName = "song.ogg";
		song.score[(int)Difficulty.Oni] = score;
		CSongDict.tAddSongNode(song.uniqueId, song);

		Directory.CreateDirectory(Path.Combine(dir, "tower"));
	}

	public void Dispose() {
		CSongDict.tRemoveSongNode(song.uniqueId);
		Directory.Delete(dir, true);
	}

	private CTct Tower(string extra = "") {
		string path = Path.Combine(dir, "tower", "tower.optktct");
		File.WriteAllText(path, $$"""{ "id": "{{song.uniqueId.data.id}}", "difficulty": "oni"{{extra}} }""");
		return new CTct(path);
	}

	[Fact]
	public void BuildsATowerNode() {
		var node = Tower(""", "life": 3, "towertype": "Day" """).BuildSongListNode();
		Assert.NotNull(node);
		var tower = node!.score[(int)Difficulty.Tower];
		Assert.NotNull(tower);
		Assert.Null(node.score[(int)Difficulty.Oni]);
		// 4 measures and the closing bar line, counted as for a tower course in a .tja
		Assert.Equal(5, tower!.ChartInfo.nTotalFloor);
		Assert.Equal(3, tower.ChartInfo.nLife);
		Assert.Equal("Day", tower.ChartInfo.nTowerType);
		Assert.Equal(8, node.nLevel[(int)Difficulty.Tower]);
		Assert.Equal(-1, node.nLevel[(int)Difficulty.Oni]);
		Assert.Equal("Source", node.ldTitle.GetString(""));
		Assert.Equal(CTja.ESide.eEx, node.nSide);
		// the song's assets stay reachable from the tower
		Assert.Equal(song.score[(int)Difficulty.Oni]!.FileInfo.FolderAbsolutePath, tower.FileInfo.FolderAbsolutePath);
		Assert.Equal("jacket.png", tower.ChartInfo.Preimage);
		Assert.NotEqual(song.uniqueId.data.id, node.uniqueId.data.id);
	}

	[Fact]
	public void FloorsMatchTheSameChartAsATowerCourse() {
		string path = Path.Combine(dir, "tower", "native.tja");
		File.WriteAllText(path, "TITLE:Native\nBPM:120\n\nCOURSE:Tower\nLEVEL:8\nLIFE:5\n\n#START\n1010,\n2020,\n1111,\n2222,\n#END\n");
		var native = new CTja(path, CTja.ETjaCompat.OOS, Difficulty.Tower, 0, loadChart: true);
		int max = 0;
		foreach (var chip in native.listChip)
			if (chip.nChannelNo == 0x50 && chip.nIntValue_InternalNumber > max) max = chip.nIntValue_InternalNumber;
		native.DeActivate();
		Assert.Equal(max + 1, Tower().BuildSongListNode()!.score[(int)Difficulty.Tower]!.ChartInfo.nTotalFloor);
	}

	[Fact]
	public void TheChartPlaysAsATower() {
		var tja = Tower(""", "life": 2, "side": "normal", "level": 10 """).BuildCtja(0);
		Assert.Equal(2, tja.LIFE);
		Assert.Equal(CTja.ESide.eNormal, tja.SIDE);
		Assert.Contains(tja.listChip, c => c.nChannelNo == 0x11);
		tja.DeActivate();
	}

	[Fact]
	public void ASetTitleTakesPriority() {
		var tct = Tower(""", "title": "Own title", "level": 10 """);
		var node = tct.BuildSongListNode()!;
		Assert.Equal("Own title", node.ldTitle.GetString(""));
		Assert.Equal("Own title", node.score[(int)Difficulty.Tower]!.ChartInfo.Title);
		Assert.Equal(10, node.nLevel[(int)Difficulty.Tower]);
		var tja = tct.BuildCtja(0);
		Assert.Equal("Own title", tja.TITLE.GetString(""));
		tja.DeActivate();
	}

	[Fact]
	public void AMissingSongGivesNoNode() {
		string path = Path.Combine(dir, "tower", "missing.optktct");
		File.WriteAllText(path, """{ "id": "no-such-song" }""");
		Assert.Null(new CTct(path).BuildSongListNode());
	}
}
