using System.Linq;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// flying notes fly in the 1 and 2 player layouts only, and an online round follows the same rule as a local game
	[Collection("net")]   // sets LuaNetworking.Active, which the networking tests also use
	public class FlyingNotesTests {
		private static CConfigIni Config(int playerCount, bool aiBattle = false) {
			var cfg = new CConfigIni();
			cfg.nPlayerCount = playerCount;
			cfg.bAIBattleMode = aiBattle;
			return cfg;
		}

		[Theory]
		[InlineData(1, true)]
		[InlineData(2, true)]
		[InlineData(3, false)]
		[InlineData(4, false)]
		[InlineData(5, false)]
		public void FliesForOneOrTwoPlayers(int playerCount, bool flies) {
			Assert.Equal(flies, FlyingNotes.Flies(Config(playerCount), NotesManager.ENoteType.Don));
			Assert.Equal(flies, FlyingNotes.Flies(Config(playerCount), NotesManager.ENoteType.KaBig));
		}

		// an online round sets the player count to the room size with AI battle off, then starts the play sync
		[Theory]
		[InlineData(2, true)]
		[InlineData(3, false)]
		[InlineData(4, false)]
		[InlineData(5, false)]
		public void OnlineRoundFliesLikeALocalGame(int playerCount, bool flies) {
			var previous = LuaNetworking.Active;
			var net = new LuaNetworking();
			try {
				LuaNetworking.Active = net;
				net.SetPlaySpots("[" + string.Join(",", Enumerable.Range(1, playerCount)) + "]");
				net.BeginPlaySync("me");
				Assert.True(LuaNetworking.Active.PlaySyncActive);
				Assert.True(net.IsRemoteSpot(playerCount - 1));
				Assert.Equal(flies, FlyingNotes.Flies(Config(playerCount), NotesManager.ENoteType.Don));
			} finally {
				net.EndPlaySync();
				LuaNetworking.Active = previous;
			}
		}

		// AI battle counts as 2 players whatever player count was set
		[Fact]
		public void AIBattleFlies() {
			Assert.True(FlyingNotes.Flies(Config(4, aiBattle: true), NotesManager.ENoteType.Don));
		}

		[Fact]
		public void SimpleModeAndEmptyLanesDoNotFly() {
			var cfg = Config(1);
			Assert.False(FlyingNotes.Flies(cfg, NotesManager.ENoteType.Empty));
			Assert.False(FlyingNotes.Flies(cfg, NotesManager.ENoteType.Unknown));
			cfg.SimpleMode = true;
			Assert.False(FlyingNotes.Flies(cfg, NotesManager.ENoteType.Don));
		}
	}
}
