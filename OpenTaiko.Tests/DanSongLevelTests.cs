using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace OpenTaiko.Tests;

// #NEXTSONG's level may have decimals, like a chart's LEVEL: the song keeps the whole level and the decimal one
[Collection("tja")]
public class DanSongLevelTests : IDisposable, IClassFixture<OpenTaikoTests.TjaFixture> {
	private readonly OpenTaikoTests.TempDir dir = new("optk-dan-");

	public DanSongLevelTests(OpenTaikoTests.TjaFixture _) {
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);   // #NEXTSONG's file lookup reads Shift_JIS, as the game registers at start
	}

	public void Dispose() => dir.Dispose();

	private CTja.DanSongs[] Load(params string[] levels) {
		string path = Path.Combine(dir, "dan.tja");
		var text = new StringBuilder("TITLE:Dan\nBPM:120\n\nCOURSE:Dan\nLEVEL:10\n\n#START\n");
		for (int i = 0; i < levels.Length; i++)
			text.Append($"#NEXTSONG Song{i},,,song{i}.ogg,300,120,{levels[i]},3\n1000,\n");
		text.Append("#END\n");
		File.WriteAllText(path, text.ToString());
		var tja = new CTja(path, CTja.ETjaCompat.OOS, Difficulty.Dan, 0, loadChart: true);
		var songs = tja.List_DanSongs.ToArray();
		tja.DeActivate();
		return songs;
	}

	[Fact]
	public void DecimalLevels_KeepTheWholeLevelAndTheDecimalOne() {
		var songs = Load("10.5", "9.2", "8", "");
		Assert.Equal(4, songs.Length);
		Assert.Equal(10, songs[0].Level);
		Assert.Equal(10.5, new LuaSongDanSong(songs[0]).LevelDecimal);
		Assert.Equal(9, songs[1].Level);
		Assert.Equal(9.2, new LuaSongDanSong(songs[1]).LevelDecimal);
		Assert.Equal(8, songs[2].Level);
		Assert.Equal(8.0, new LuaSongDanSong(songs[2]).LevelDecimal);
		Assert.Equal(10, songs[3].Level);   // no level: the default
		Assert.Equal(10.0, new LuaSongDanSong(songs[3]).LevelDecimal);
	}

	[Fact]
	public void SongListCache_KeepsTheDecimalLevel() {
		var node = new CSongListNode { DanSongs = new() { new CTja.DanSongs { Title = "a", SubTitle = "", Genre = "", FileName = "", Level = 10, LevelDecimal = 10.5 } } };
		using var ms = new MemoryStream();
		CEnumSongs.WriteSongListCache(ms, new Dictionary<string, CSongListNode> { ["a"] = node });
		ms.Position = 0;
		var read = CEnumSongs.ReadSongListCache(ms);
		Assert.NotNull(read);
		Assert.Equal(10.5, read!["a"].DanSongs![0].LevelDecimal);
	}
}
