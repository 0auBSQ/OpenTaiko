using System;
using System.IO;
using System.Runtime.CompilerServices;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// a skin folder counts as a skin only with its title background and its _boot and _title stages
	public class SkinValidityTests : IDisposable {
		private readonly string dir = Path.Combine(Path.GetTempPath(), "skin_valid_" + Guid.NewGuid().ToString("N"));

		public SkinValidityTests() => Directory.CreateDirectory(dir);

		public void Dispose() {
			try { Directory.Delete(dir, true); } catch (IOException) { }
		}

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
			Assert.True(skin.bIsValid(dir));

			File.Delete(Path.Combine(dir, "Graphics", "1_Title", "Background.png"));
			Assert.False(skin.bIsValid(dir));
		}
	}
}
