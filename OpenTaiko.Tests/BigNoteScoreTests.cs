using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// ShinUchi off: every score mode doubles the score of the same notes, the big and joint notes (in Konga, the pink
	// and clap notes), and of nothing else
	public class BigNoteScoreTests {
		[Theory]
		[InlineData(0x13)]   // big Don, Konga pink
		[InlineData(0x14)]   // big Ka, Konga clap
		[InlineData(0x1A)]   // joint big Don
		[InlineData(0x1B)]   // joint big Ka
		public void BigAndJointNotes_AreDoubled(int channel) {
			Assert.True(CStagePlayScreenCommon.IsBigNoteForScore((NotesManager.ENoteType)channel));
		}

		[Theory]
		[InlineData(0x11)]   // Don
		[InlineData(0x12)]   // Ka
		[InlineData(0x101)]  // purple
		[InlineData(0x25)]   // not a note type
		public void OtherNotes_AreNotDoubled(int channel) {
			Assert.False(CStagePlayScreenCommon.IsBigNoteForScore((NotesManager.ENoteType)channel));
		}
	}
}
