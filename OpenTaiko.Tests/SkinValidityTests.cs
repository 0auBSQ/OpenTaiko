using System;
using System.IO;
using System.Runtime.CompilerServices;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// a skin folder counts as a skin only with its title background, its _boot and _title stages and a
	// SkinConfig.ini version of at least CSkin.MinimumSkinVersion
	public class SkinValidityTests : IDisposable {
		private readonly TempDir dir = new("skin_valid_");

		public void Dispose() => dir.Dispose();

		private void Add(string relative) {
			string path = Path.Combine(dir, relative);
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			File.WriteAllText(path, "");
		}

		[Fact]
		public void NeedsTheBootAndTitleStages() {
			var skin = (CSkin)RuntimeHelpers.GetUninitializedObject(typeof(CSkin));
			Add(Path.Combine("Graphics", "1_Title", "Background.png"));
			Assert.False(skin.bIsValid(dir));

			Add(Path.Combine("Modules", "Stages", "_boot", "Script.lua"));
			Assert.False(skin.bIsValid(dir));

			Add(Path.Combine("Modules", "Stages", "_title", "Script.lua"));
			Assert.False(skin.bIsValid(dir));

			File.WriteAllText(Path.Combine(dir, "SkinConfig.ini"), "Name=Old\nVersion=0.6.0.24\n");
			Assert.False(skin.bIsValid(dir));

			File.WriteAllText(Path.Combine(dir, "SkinConfig.ini"), "Name=New\nVersion=0.7.0.0\n");
			Assert.True(skin.bIsValid(dir));

			File.Delete(Path.Combine(dir, "Graphics", "1_Title", "Background.png"));
			Assert.False(skin.bIsValid(dir));
		}
	}
}
