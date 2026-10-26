using System;
using System.IO;
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
						Assert.Equal(FormerRow(combo, step, !entry), sheet.Row(new CNoteAnim(combo, step, entry)));
		}

		[Fact]
		public void RowsStayInTheSheet() {
			var single = DefaultTable(1);
			Assert.Equal(0, single.Row(new CNoteAnim(500, 1, false)));
			Assert.Equal(0, new CSheetDef().Row(new CNoteAnim(500, 1, false)));
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
