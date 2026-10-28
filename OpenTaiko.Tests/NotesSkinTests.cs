using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OpenTaiko;
using SkiaSharp;
using Xunit;

namespace OpenTaikoTests {
	// notes packs: the animation table of Definition.json, and OWM's packs matching their sheets
	public class NotesSkinTests {
		private static readonly string Packs = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
			"OpenTaiko", "System", "Open-World Memories", "Graphics", "5_Game"));

		private static SKSizeI Size(string path) {
			using var codec = SKCodec.Create(path);
			return new SKSizeI(codec.Info.Width, codec.Info.Height);
		}

		private static CSheetDef DefaultTable(int frames) => JsonConvert.DeserializeObject<CSheetDef>(@"{
			""Frames"": " + frames + @",
			""Animation"": [
				{ ""FromCombo"": 0, ""Rows"": [0] },
				{ ""FromCombo"": 50, ""Rows"": [1, 1, 0, 0], ""RowsOnEntry"": [0] },
				{ ""FromCombo"": 150, ""Rows"": [0, 1] },
				{ ""FromCombo"": 300, ""Rows"": [0, 2], ""RowsOnEntry"": [0, 1] }
			] }")!;

		// the rows the play screen used before the packs (its combo animation, rows 0-2)
		private static int FormerRow(int combo, int step, bool lagEnded) {
			if (combo >= 300) return step % 2 == 1 ? (lagEnded ? 2 : 1) : 0;
			if (combo >= 150) return step % 2 == 1 ? 1 : 0;
			if (combo >= 50) return step <= 1 ? (lagEnded ? 1 : 0) : 0;
			return 0;
		}

		[Fact]
		public void DefaultTableGivesTheFormerRows() {
			var sheet = DefaultTable(3);
			foreach (int combo in new[] { 0, 49, 50, 149, 150, 299, 300, 1000 })
				for (int step = 0; step < 4; step++)
					foreach (bool entry in new[] { false, true })
						foreach (var diff in new[] { Difficulty.Easy, Difficulty.Oni })
							Assert.Equal(FormerRow(combo, step, !entry), sheet.Row(new CNoteAnim(combo, step, entry, diff), "Don"));
		}

		[Fact]
		public void RowsStayInTheSheet() {
			var single = DefaultTable(1);
			Assert.Equal(0, single.Row(new CNoteAnim(500, 1, false, Difficulty.Oni), "Don"));
			Assert.Equal(0, new CSheetDef().Row(new CNoteAnim(500, 1, false, Difficulty.Oni), "Don"));
		}

		[Fact]
		public void TiersStartAtTheirDifficultysCombo() {
			var sheet = JsonConvert.DeserializeObject<CSheetDef>(@"{ ""Frames"": 3, ""Animation"": [
				{ ""FromCombo"": 0, ""Rows"": [0] },
				{ ""FromCombo"": 100, ""FromComboByDifficulty"": { ""Easy"": 5, ""hard"": 50 }, ""Rows"": [2] }
			] }")!;
			int Row(int combo, Difficulty diff) => sheet.Row(new CNoteAnim(combo, 0, false, diff), "Don");
			Assert.Equal(0, Row(4, Difficulty.Easy));
			Assert.Equal(2, Row(5, Difficulty.Easy));
			Assert.Equal(0, Row(49, Difficulty.Hard));
			Assert.Equal(2, Row(50, Difficulty.Hard));
			// unlisted difficulties use FromCombo
			Assert.Equal(0, Row(50, Difficulty.Normal));
			Assert.Equal(2, Row(100, Difficulty.Normal));
			Assert.Equal(2, Row(100, Difficulty.Edit));
		}

		[Fact]
		public void NotesCanHaveTheirOwnRows() {
			var sheet = JsonConvert.DeserializeObject<CSheetDef>(@"{ ""Frames"": 3, ""Animation"": [
				{ ""FromCombo"": 0, ""Rows"": [0, 2], ""RowsByNote"": { ""BigDon"": [1, 2] }, ""RowsOnEntry"": [0] }
			] }")!;
			int Row(string note, int step, bool entry = false) => sheet.Row(new CNoteAnim(10, step, entry, Difficulty.Oni), note);
			Assert.Equal(new[] { 0, 2 }, new[] { Row("Don", 0), Row("Don", 1) });
			Assert.Equal(new[] { 1, 2 }, new[] { Row("BigDon", 0), Row("BigDon", 1) });
			Assert.Equal(new[] { 1, 2 }, new[] { Row("bigdon", 0), Row("bigdon", 1) });
			Assert.Equal(new[] { 0, 0 }, new[] { Row("BigDon", 0, true), Row("BigDon", 1, true) });
		}

		// Sky Spirits: the tiers start at 5/10/30 combo in Easy and Normal, 10/30/50 in Hard and 10/50/100 in Extreme and
		// Extra Extreme; from the last tier on, the big notes (big Don, big Ka, KaDon) alternate their rows 1 and 2
		[Theory]
		[InlineData("Notes Skin (Taiko)/3")]
		[InlineData("Notes Skin (Taiko)/4")]
		public void SkySpiritsTiersFollowTheDifficulty(string pack) {
			var def = JsonConvert.DeserializeObject<CNotesSkinDef>(File.ReadAllText(Path.Combine(Packs, pack, "Definition.json")))!;
			var expected = new (Difficulty Diff, int[] From)[] {
				(Difficulty.Easy, [0, 5, 10, 30]), (Difficulty.Normal, [0, 5, 10, 30]), (Difficulty.Hard, [0, 10, 30, 50]),
				(Difficulty.Oni, [0, 10, 50, 100]), (Difficulty.Edit, [0, 10, 50, 100]),
			};
			foreach (var sheet in new[] { def.Notes, def.Rolls, def.Balloons })
				foreach (var (diff, from) in expected)
					Assert.Equal(from, sheet.Animation.Select(t => t.FromComboAt(diff)).ToArray());
			var top = new CNoteAnim(100, 0, false, Difficulty.Oni);
			foreach (string note in new[] { "Don", "Ka", "BigDon", "BigKa", "KaDon", "ADLib" }) {
				int[] rows = { def.Notes.Row(top, note), def.Notes.Row(top with { Step = 1 }, note) };
				Assert.Equal(note is "BigDon" or "BigKa" or "KaDon" ? new[] { 1, 2 } : new[] { 0, 2 }, rows);
			}
		}

		[Theory]
		[InlineData("Notes Skin (Taiko)/1", 6)]
		[InlineData("Notes Skin (Taiko)/3", 6)]
		[InlineData("Notes Skin (Taiko)/4", 6)]
		[InlineData("Notes Skin (Bongo)/1", 5)]
		public void OwmPacksMatchTheirSheets(string pack, int noteColumns) {
			string dir = Path.Combine(Packs, pack);
			var def = JsonConvert.DeserializeObject<CNotesSkinDef>(File.ReadAllText(Path.Combine(dir, "Definition.json")))!;
			Assert.False(string.IsNullOrEmpty(def.Name.GetString("")));
			var notes = Size(Path.Combine(dir, "Notes.png"));
			int cell = notes.Width / noteColumns;
			Assert.Equal(def.Notes.Frames, notes.Height / cell);
			Assert.Equal(def.Rolls.Frames, Size(Path.Combine(dir, "Rolls.png")).Height / cell);
			if (File.Exists(Path.Combine(dir, "Balloons.png")))
				Assert.Equal(def.Balloons.Frames, Size(Path.Combine(dir, "Balloons.png")).Height / cell);
			Assert.InRange(def.FlyingNotesFrame, 0, def.Notes.Frames - 1);
		}
	}
}
