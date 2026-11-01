using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// one local player's Dynamic Beat is forced on every player for the play only: the players' own fun mods come
	// back after it, and a second call, which finds every player on Dynamic Beat, keeps the picks already saved
	public class SharedDynamicBeatTests {
		[Fact]
		public void ForcedDuringPlay_RestoredAfter_SecondCallKeepsPicks() {
			var cfg = new CConfigIni();
			cfg.nPlayerCount = 3;
			cfg.nFunMods[0] = EFunMods.Minesweeper;
			cfg.nFunMods[1] = EFunMods.DynamicBeat;
			cfg.nFunMods[3] = EFunMods.Avalanche;   // not playing

			CStagePlayScreenCommon.ForceSharedDynamicBeat(cfg);
			CStagePlayScreenCommon.ForceSharedDynamicBeat(cfg);
			Assert.Equal(new[] { EFunMods.DynamicBeat, EFunMods.DynamicBeat, EFunMods.DynamicBeat, EFunMods.Avalanche, EFunMods.None }, cfg.nFunMods);

			CStagePlayScreenCommon.RestoreSharedDynamicBeat(cfg);
			CStagePlayScreenCommon.RestoreSharedDynamicBeat(cfg);
			Assert.Equal(new[] { EFunMods.Minesweeper, EFunMods.DynamicBeat, EFunMods.None, EFunMods.Avalanche, EFunMods.None }, cfg.nFunMods);
		}
	}
}
