using System.Linq;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// skin settings file name: SkinSettings_[skin folder name].db3
	public class SkinSettingsTests {
		private static string Name(string folder) => DBThemeSettings.SettingsFileName(folder);

		[Fact]
		public void APlainNameIsKept() {
			Assert.Equal("SkinSettings_Open-World Memories.db3", Name("System/Open-World Memories/"));
			Assert.Equal("SkinSettings_SimpleStyle (1080p).db3", Name(@"System\SimpleStyle (1080p)\"));
		}

		[Theory]
		[InlineData("A:B", "A%3AB")]
		[InlineData("a*b?c", "a%2Ab%3Fc")]
		[InlineData("<x>|\"y\"", "%3Cx%3E%7C%22y%22")]
		[InlineData("100%", "100%25")]
		[InlineData("Skin.", "Skin%2E")]
		[InlineData("Skin ", "Skin%20")]
		[InlineData("tab\tname", "tab%09name")]
		public void ForbiddenCharactersAreEscaped(string folder, string escaped) {
			Assert.Equal("SkinSettings_" + escaped + ".db3", Name("System/" + folder));
		}

		[Fact]
		public void ALongNameIsCutWithAHash() {
			string a = new string('x', 200) + "A", b = new string('x', 200) + "B";
			string na = Name("System/" + a), nb = Name("System/" + b);
			Assert.Equal("SkinSettings_".Length + 64 + ".db3".Length, na.Length);
			Assert.Matches("^SkinSettings_x{55}_[0-9A-F]{8}\\.db3$", na);
			Assert.NotEqual(na, nb);
			Assert.Equal(na, Name("System/" + a));
		}

		[Fact]
		public void TheCutNeverSplitsACharacter() {
			string n = Name("System/" + string.Concat(Enumerable.Repeat("😀", 60)));
			for (int i = 0; i < n.Length; i++) {
				if (char.IsHighSurrogate(n[i])) Assert.True(i + 1 < n.Length && char.IsLowSurrogate(n[i + 1]));
				if (char.IsLowSurrogate(n[i])) Assert.True(i > 0 && char.IsHighSurrogate(n[i - 1]));
			}
		}

		[Theory]
		[InlineData("A:B*C?")]
		[InlineData("..")]
		[InlineData("Skin.")]
		public void TheNameIsAValidFileName(string folder) {
			string n = Name("System/" + folder);
			Assert.DoesNotContain(n, c => "<>:\"/\\|?*".IndexOf(c) >= 0 || c < 0x20);
			Assert.False(n.EndsWith('.') || n.EndsWith(' '));
		}
	}
}
