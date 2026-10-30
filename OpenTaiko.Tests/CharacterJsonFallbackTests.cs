using System;
using System.IO;
using Xunit;

namespace OpenTaiko.Tests;

// ConfigManager.ReadOr and character folders: a broken json gives the defaults instead of throwing, so one bad
// character cannot stop the boot before CHARACTERLIST exists
public class CharacterJsonFallbackTests : IDisposable {
	private readonly string dir = Path.Combine(Path.GetTempPath(), "optk-chara-" + Guid.NewGuid().ToString("N"));

	public CharacterJsonFallbackTests() => Directory.CreateDirectory(dir);
	public void Dispose() => Directory.Delete(dir, true);

	[Fact]
	public void BrokenJsonGivesTheFallback() {
		string file = Path.Combine(dir, "Metadata.json");
		File.WriteAllText(file, "{ \"name\": \"Broken\", ");
		var fallback = new DBCharacter.CharacterData();
		Assert.Same(fallback, ConfigManager.ReadOr(file, ConfigManager.GetConfig<DBCharacter.CharacterData>, fallback));
	}

	[Fact]
	public void MissingFileGivesTheFallbackWithoutCreatingIt() {
		string file = Path.Combine(dir, "Effects.json");
		var fallback = new DBCharacter.CharacterEffect();
		Assert.Same(fallback, ConfigManager.ReadOr(file, ConfigManager.GetConfig<DBCharacter.CharacterEffect>, fallback));
		Assert.False(File.Exists(file));
	}

	[Fact]
	public void ValidJsonIsRead() {
		string file = Path.Combine(dir, "Metadata.json");
		File.WriteAllText(file, "{ \"rarity\": \"Rare\" }");
		var data = ConfigManager.ReadOr(file, ConfigManager.GetConfig<DBCharacter.CharacterData>, new DBCharacter.CharacterData());
		Assert.Equal("Rare", data.Rarity);
	}

	[Fact]
	public void ACharacterWithBrokenJsonStillLoads() {
		File.WriteAllText(Path.Combine(dir, "Metadata.json"), "not json");
		File.WriteAllText(Path.Combine(dir, "Effects.json"), "{ \"gauge\": ");
		var info = new CCharacter.Info(dir, 3);
		Assert.Equal("Common", info.metadata.Rarity);
		Assert.Equal("Normal", info.effect.Gauge);
		Assert.Null(info.unlock);
		Assert.Equal(3, info._idx);
	}
}
