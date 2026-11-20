using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests;

// Unit tests surrounding single TJA header or TJA command.
[Collection("tja")]
public class TjaArgumentTests : IClassFixture<TjaFixture> {
	public TjaArgumentTests(TjaFixture _)
		=> Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);   // #NEXTSONG's file lookup reads Shift_JIS, as the game registers at start

	private static TempDir TempDirWithLyrics(IEnumerable<int> indexes) {
		TempDir dir = new("ot_tja_");
		foreach (int i in indexes) {
			// Notice: .vtt still heavily relies on uses OpenTaiko.Skin and does not support skin-less test
			string pathLrc = Path.Combine(dir, $"{i}.lrc");
			File.WriteAllLines(pathLrc, [$"[00:00.00]{i}"]);
		}
		return dir;
	}

	// WAVE: is played if there is a chart section before the first #NEXTSONG
	// LYRICS:/LYRICFILE: is always played from the first #NEXTSONG except when there is no a single #NEXTSONG
	[Theory]
	[InlineData("dan_with_wav", "WAVE:preview.ogg", false)]
	[InlineData("dan_with_wav_chart_before_NEXTSONG", "WAVE:preview.ogg", true)]
	[InlineData("dan_empty_wav", "WAVE:", false)]
	[InlineData("dan_empty_wav_chart_before_NEXTSONG", "WAVE:", true)]
	[InlineData("dan_without_wav", "", false)]
	[InlineData("dan_without_wav_chart_before_NEXTSONG", "", true)]
	public void SongAudioLyricOrder_DanWith3NextSong(string title, string waveHeader, bool withPreNextSongChart) {
		using var dir = TempDirWithLyrics([1, 3, 4]);
		string pathTja = Path.Combine(dir, $"{title}.tja");
		File.WriteAllLines(pathTja, [
			$"TITLE:{title}",
			"BPM:120",
			waveHeader,
			"COURSE:Dan",
			"LYRICS:1.lrc,,3.lrc,4.lrc", // omit 2, extra 4
			"#START",
			withPreNextSongChart ? "0," : "",
			"#NEXTSONG song1,artist1,genre1,song1.ogg",
			"#NEXTSONG song2,artist2,genre2,song2.ogg",
			"#NEXTSONG song3,artist3,genre3,song3.ogg",
			"#END",
		]);
		var tja = TjaFixture.ParseTjaFile(pathTja);

		// get dan song audio chip and file list
		var chipWavs = tja.listChip.Where(c => c.nChannelNo == 0x01).ToList();
		var chipFirstWav = chipWavs[0];
		Assert.False(chipFirstWav.nSoundTimems == 0 && tja.listWAV.TryGetValue(chipFirstWav.nIntValue_InternalNumber, out var wav) && wav.strFileName.EndsWith("song1.ogg"),
			"first dan song audio should not be played right at chart start");
		var chipFileDanSong = chipWavs
			.Select(c => (chip: c, file: tja.listWAV.GetValueOrDefault(c.nIntValue_InternalNumber)?.strFileName))
			.Where(cf => !string.IsNullOrEmpty(cf.file) && Path.GetFileName(cf.file).StartsWith("song"))
			.ToList();

		// check dan song audio file list
		var filesWant = new[] { "song1.ogg", "song2.ogg", "song3.ogg" };
		Assert.True(filesWant.Length == chipFileDanSong.Count,
			$"{title}: expected {filesWant.Length} dan song files, got {chipFileDanSong.Count} [{string.Join(", ", chipFileDanSong)}]");
		for (int i = 0; i < filesWant.Length; ++i)
			Assert.True(filesWant[i] == Path.GetFileName(chipFileDanSong[i].file),
				$"{title}: dan song {i} expected to be '{filesWant[i]}', got '{chipFileDanSong[i].file}' ({chipFileDanSong[i]})");

		// check dan song lyrics
		var lyricsWant = new[] { "1", "3" };
		var lyricTimeChipWant = new[] { 0, 2 }.Select(i => chipFileDanSong[i].chip.nSoundTimems).ToList();
		Assert.True(lyricsWant.Length == tja.listLyric2.Count,
			$"{title}: expected {lyricsWant.Length} lines of lyrics, got {tja.listLyric2.Count} [{string.Join(", ", tja.listLyric2)}]");
		for (int i = 0; i < lyricsWant.Length; ++i) {
			Assert.True(lyricsWant[i] == tja.listLyric2[i].Text,
				$"{title}: lyric line {i + 1} ({tja.listLyric2[i]}) expected to be '{lyricsWant[i]}', got '{tja.listLyric2[i].Text}'");
			Assert.True(tja.listLyric2[i].Time == lyricTimeChipWant[i],
				$"{title}: lyric line {i + 1} ({tja.listLyric2[i]}) expected to be at '{lyricTimeChipWant[i]} ms', got {tja.listLyric2[i].Time} ms");
		}
	}

	[Theory]
	[InlineData("with_wav", "WAVE:song.ogg", false)]
	[InlineData("empty_wav", "WAVE:", false)]
	[InlineData("without_wav", "", false)]
	[InlineData("dan_with_wav", "WAVE:song.ogg", true)]
	[InlineData("dan_empty_wav", "WAVE:", true)]
	[InlineData("dan_without_wav", "", true)]
	public void SongAudioLyricOrder_NoNextSong(string title, string waveHeader, bool isDan) {
		using var dir = TempDirWithLyrics([1, 3, 4]);
		string pathTja = Path.Combine(dir, $"{title}.tja");
		File.WriteAllLines(pathTja, [
			$"TITLE:{title}",
			"BPM:120",
			waveHeader,
			isDan ? "COURSE:Dan" : "COURSE:Oni",
			"LYRICS:1.lrc,,3.lrc,4.lrc", // extra 3 & 4
			"#START",
			"#END",
		]);
		var tja = TjaFixture.ParseTjaFile(pathTja);

		var chipFirstWav = tja.listChip.First(c => c.nChannelNo == 0x01);
		Assert.True(chipFirstWav.nSoundTimems == 0, "song audio should be played right at chart start");

		// check song lyric
		const string lyricWant = "1";
		Assert.True(tja.listLyric2.Count == 1,
			$"{title}: expected 1 lines of lyrics, got {tja.listLyric2.Count} [{string.Join(", ", tja.listLyric2)}]");
		Assert.True(lyricWant == tja.listLyric2[0].Text,
			$"{title}: lyric line ({tja.listLyric2[0]}) expected to be '{lyricWant}', got '{tja.listLyric2[0].Text}'");
		Assert.True(tja.listLyric2[0].Time == 0,
			$"{title}: lyric line ({tja.listLyric2[0]}) expected to be at '{chipFirstWav.nSoundTimems} ms', got {tja.listLyric2[0].Time} ms");
	}

	[Theory]
	[InlineData("", Exam.Type.None)] // "keep current branch"
	[InlineData("p,50.0,75.0", Exam.Type.Accuracy, (int)CTja.EBranchCondBig.Both, CTExprRange.MoreOrEqual)] // normal
	[InlineData("p50.0,75.0,m", Exam.Type.Accuracy, (int)CTja.EBranchCondBig.Both, CTExprRange.MoreOrEqual)] // with range
	[InlineData("p50.0,75.0,m,", Exam.Type.Accuracy, (int)CTja.EBranchCondBig.Both, CTExprRange.MoreOrEqual)] // with range and trailing comma (,)
	[InlineData("p50.0,75.0", Exam.Type.Accuracy, (int)CTja.EBranchCondBig.Both, CTExprRange.MoreOrEqual)] // omit comma after letter-only condition
	[InlineData("p\t50.0\t,\t75.0\t,\tm", Exam.Type.Accuracy, (int)CTja.EBranchCondBig.Both, CTExprRange.MoreOrEqual)] // tab between arguments
	[InlineData("Pp,50.0,100.0", Exam.Type.PercentPerfect, (int)CTja.EBranchCondBig.BigOnly, CTExprRange.MoreOrEqual)] // non-standard casing (= PP)
	[InlineData("jB,2,1,l", Exam.Type.JudgeBad, (int)CTja.EBranchCondBig.Both, CTExprRange.Less)] // non-standard casing (= jb)
	[InlineData("lc:count,50.0,100.0", Exam.Type.LocalCounter, null, CTExprRange.MoreOrEqual)] // normal
	[InlineData("lc : count , 50.0 , 100.0", Exam.Type.LocalCounter, null, CTExprRange.MoreOrEqual)] // space around colon (:)
	[InlineData("lc:bad_count,10,5,l", Exam.Type.LocalCounter, null, CTExprRange.Less)] // with range
	[InlineData("lt,exp_key,mas_key", Exam.Type.LocalTrigger, null, CTExprRange.MoreOrEqual)] // normal
	[InlineData("lt , exp_key , mas_key", Exam.Type.LocalTrigger, null, CTExprRange.MoreOrEqual)] // space around trigger keys
	[InlineData("1,2", Exam.Type.None)] // missing condition without comma (,)
	[InlineData(",1,2", Exam.Type.None)] // missing condition
	[InlineData("BR,1,2", Exam.Type.None)] // unknown condition
	[InlineData("p", Exam.Type.None)] // missing values
	[InlineData("p,,90.0", Exam.Type.None)] // missing expert value
	[InlineData("p,abc,90.0", Exam.Type.None)] // invalid expert value
	[InlineData("p,50.0,", Exam.Type.None)] // missing master value
	[InlineData("p,50.0,abc", Exam.Type.None)] // invalid master value
	[InlineData("p,80.0,90.0,x", Exam.Type.None)] // unknown range
	[InlineData("lc,50.0,100.0", Exam.Type.None)] // missing colon (:) and counter key
	[InlineData("lc: ,50.0,100.0", Exam.Type.None)] // missing counter key
	[InlineData("lc:count,abc,100.0", Exam.Type.None)] // invalid expert value
	[InlineData("lc:count,50.0,abc", Exam.Type.None)] // invalid master value
	[InlineData("lc:count,50.0,100.0,x", Exam.Type.None)] // unknown range
	[InlineData("lt", Exam.Type.None)] // missing both trigger key
	[InlineData("lt, , mas_key", Exam.Type.None)] // missing expert trigger key
	[InlineData("lt, exp_key , ", Exam.Type.None)] // missing master trigger key
	public void BranchStartTypeBigRange(string args, Exam.Type type, int? big = null, CTExprRange? range = null) {
		var tja = TjaFixture.ParseTjaText("ot_tja_", "branch_args.tja", string.Join('\n', [
			$"TITLE:{args}",
			"BPM:120",
			"#START",
			$"#BRANCHSTART {args}",
			"#END",
		]));
		var chipBranchStart = tja.listChip.First(c => c.nChannelNo == 0xDE);
		Assert.Equal(type, chipBranchStart.eBranchCondition.type);
		if (big != null)
			Assert.Equal((CTja.EBranchCondBig)big, chipBranchStart.eBranchCondition.big);
		if (range != null)
			Assert.Equal(range, chipBranchStart.eBranchConditionRange);
	}
}
