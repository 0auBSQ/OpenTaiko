using System;
using System.Text;
using DiscordRPC;
using Xunit;

namespace OpenTaikoTests {
	// TruncateUtf8 cuts the Discord presence details to 128 UTF-8 bytes; a cut inside a character decoded to U+FFFD,
	// re-encoded to more than 128 bytes and made the Details setter throw when a song with a long title started.
	public class Utf8TruncateTests {
		[Theory]
		[InlineData("あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをん Extreme Lv.10")]
		[InlineData("あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをんあいうえおかきくけこさしす Extra Lv.10")]
		[InlineData("X🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁🥁 Extreme Lv.10")]
		[InlineData("Placeholder title テストテストテストテストテストテストテストテストテストテストテストテストテスト Extreme Lv.10")]
		public void LongTitles_FitDiscordDetails(string details) {
			byte[] full = Encoding.UTF8.GetBytes(details);
			Assert.True(full.Length > 128 && (full[128] & 0xC0) == 0x80);   // a plain 128-byte cut would split a character
			string cut = details.TruncateUtf8(128);
			int bytes = Encoding.UTF8.GetByteCount(cut);
			Assert.InRange(bytes, 125, 128);        // at most one character (up to 4 bytes) dropped at the cut
			Assert.DoesNotContain('\uFFFD', cut);
			Assert.StartsWith(cut, details);
			var presence = new RichPresence { Details = cut };   // throws past 128 bytes
			Assert.Equal(cut, presence.Details);
		}

		[Fact]
		public void ShortText_IsUnchanged() {
			Assert.Equal("Placeholder title Extreme Lv.8", "Placeholder title Extreme Lv.8".TruncateUtf8(128));
			Assert.Equal("", "".TruncateUtf8(128));
		}
	}
}
