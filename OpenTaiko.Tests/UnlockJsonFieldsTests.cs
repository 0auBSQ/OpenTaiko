using System;
using System.IO;
using Xunit;

namespace OpenTaiko.Tests;

// A character's or puchichara's Unlock.json reads the song fields too: custom_unlock_text, hidden_index and rarity
// (which replaces the Metadata.json rarity); a file without them reads as before
public class UnlockJsonFieldsTests : IDisposable {
	private readonly string dir = Path.Combine(Path.GetTempPath(), "optk-unlock-" + Guid.NewGuid().ToString("N"));

	public UnlockJsonFieldsTests() => Directory.CreateDirectory(dir);
	public void Dispose() => Directory.Delete(dir, true);

	private CUnlockCondition Read(string json) {
		string file = Path.Combine(dir, "Unlock.json");
		File.WriteAllText(file, json);
		return new CUnlockConditionFactory().GenerateUnlockObjectFromJsonPath(file);
	}

	[Fact]
	public void SongFieldsAreRead() {
		var uc = Read("""
			{
				"hidden_index": 2,
				"rarity": "Rare",
				"condition": "gc",
				"type": "me",
				"values": [5],
				"references": ["pagoda_plays"],
				"custom_unlock_text": { "strings": { "default": "Climb", "fr": "Gravis" } }
			}
			""");
		Assert.Equal("gc", uc.Id);
		Assert.Equal(2, uc.HiddenIndex);
		Assert.Equal("Rare", uc.Rarity);
		Assert.NotNull(uc.CustomText);
		Assert.Equal("Climb", uc.CustomText!.Strings["default"]);
		Assert.Equal("Gravis", uc.CustomText.Strings["fr"]);

		var lua = new LuaUnlockCondition(uc);
		Assert.True(lua.HasCustomMessage);
		Assert.Equal(2, lua.GetHiddenIndex());
		Assert.Equal("Rare", lua.GetRarity());
	}

	[Fact]
	public void AFileWithoutThemReadsAsBefore() {
		var uc = Read("""{ "condition": "ch", "type": "me", "values": [200] }""");
		Assert.Equal("ch", uc.Id);
		Assert.Equal(0, uc.HiddenIndex);
		Assert.Null(uc.Rarity);
		Assert.Null(uc.CustomText);

		var lua = new LuaUnlockCondition(uc);
		Assert.False(lua.HasCustomMessage);
		Assert.Equal(0, lua.GetHiddenIndex());
		Assert.Equal("", lua.GetRarity());
		Assert.Equal("Uncommon", CUnlockCondition.tRarityOverride(uc, "Uncommon", dir));
	}

	[Fact]
	public void HiddenIndexIsClampedLikeSongs() {
		Assert.Equal(3, Read("""{ "hidden_index": 9, "condition": "ch", "values": [1] }""").HiddenIndex);
		Assert.Equal(0, Read("""{ "hidden_index": -4, "condition": "ch", "values": [1] }""").HiddenIndex);
	}

	[Fact]
	public void RarityReplacesTheMetadataOneOnlyWhenKnown() {
		Assert.Equal("Epic", CUnlockCondition.tRarityOverride(Read("""{ "rarity": "Epic", "condition": "ch", "values": [1] }"""), "Common", dir));
		Assert.Equal("Common", CUnlockCondition.tRarityOverride(Read("""{ "rarity": "Shiny", "condition": "ch", "values": [1] }"""), "Common", dir));
		Assert.Equal("Common", CUnlockCondition.tRarityOverride(Read("""{ "rarity": "  ", "condition": "ch", "values": [1] }"""), "Common", dir));
		Assert.Equal("Rare", CUnlockCondition.tRarityOverride(null, "Rare", dir));
	}

	[Fact]
	public void NoConditionMeansNoFields() {
		var lua = new LuaUnlockCondition(null);
		Assert.False(lua.HasCustomMessage);
		Assert.Equal(0, lua.GetHiddenIndex());
		Assert.Equal("", lua.GetRarity());
		Assert.Equal("", lua.GetConditionMessage());
	}
}
