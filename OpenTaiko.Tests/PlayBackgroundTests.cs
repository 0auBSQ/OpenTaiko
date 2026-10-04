using System;
using System.IO;
using NLua;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// a Lua script forces the next play's gameplay backgrounds by folder, and the gameplay backgrounds read the latest
	// note hits from their state
	public class PlayBackgroundTests : IDisposable {
		private readonly string _dir = Path.Combine(Path.GetTempPath(), "ot_playbg_" + Guid.NewGuid().ToString("N"));

		public PlayBackgroundTests() {
			Directory.CreateDirectory(Path.Combine(_dir, "bg", "up"));
			Directory.CreateDirectory(Path.Combine(_dir, "bg", "down"));
			Directory.CreateDirectory(Path.Combine(_dir, "bg", "empty"));
			File.WriteAllText(Path.Combine(_dir, "bg", "up", "Script.lua"), "");
			File.WriteAllText(Path.Combine(_dir, "bg", "down", "Script.lua"), "");
		}

		public void Dispose() {
			CActImplBackground.TakeForcedBackgrounds();
			try { Directory.Delete(_dir, true); } catch { }
		}

		[Fact]
		public void ForcedFolders_AreResolvedFromTheScriptFolder_AndTakenOnce() {
			var mount = new LuaSongMountFunc(Path.Combine(_dir, "stage"));
			Assert.True(mount.SetBackgrounds("../bg/up", "../bg/down"));

			var (up, down) = CActImplBackground.TakeForcedBackgrounds();
			Assert.Equal(Path.GetFullPath(Path.Combine(_dir, "bg", "up")), up);
			Assert.Equal(Path.GetFullPath(Path.Combine(_dir, "bg", "down")), down);
			Assert.Equal((null, null), CActImplBackground.TakeForcedBackgrounds());
		}

		[Fact]
		public void FolderWithoutScript_KeepsTheUsualPick_AndNilClears() {
			var mount = new LuaSongMountFunc(_dir);
			Assert.False(mount.SetBackgrounds("bg/empty", "bg/down"));
			Assert.Null(CActImplBackground.NextUpDir);
			Assert.NotNull(CActImplBackground.NextDownDir);

			Assert.True(mount.SetBackgrounds(Path.Combine(_dir, "bg", "up")));
			Assert.NotNull(CActImplBackground.NextUpDir);
			Assert.Null(CActImplBackground.NextDownDir);

			Assert.True(mount.SetBackgrounds());
			Assert.Equal((null, null), CActImplBackground.TakeForcedBackgrounds());
		}

		[Fact]
		public void Hits_FillARingPerPlayer() {
			var state = new LuaBackgroundState();
			int total = LuaBackgroundState.HitRing + 5;
			for (int i = 0; i < total; i++)
				state.AddHit(1, i % 2 == 0 ? NotesManager.ENoteType.DonBig : NotesManager.ENoteType.Ka, LuaBackgroundState.JudgeName(ENoteJudge.Good));
			state.AddHit(0, NotesManager.ENoteType.Kadon, "roll");
			state.AddHit(7, NotesManager.ENoteType.Don, "miss");   // no such player: ignored

			Assert.Equal(new[] { 1, total, 0, 0, 0 }, state.hitCount);
			int last = (total - 1) % state.hitRing;
			Assert.Equal("don", state.hitNote[1][last]);
			Assert.True(state.hitBig[1][last]);
			Assert.Equal("good", state.hitJudge[1][last]);
			Assert.Equal(("kadon", false), (state.hitNote[0][0], state.hitBig[0][0]));
			Assert.Equal("roll", state.hitJudge[0][0]);

			state.ResetHits();
			Assert.Equal(new int[5], state.hitCount);
			Assert.Equal("", state.hitNote[1][last]);
		}

		[Fact]
		public void FlyTarget_IsSetPerPlayer_AndClearedWithTheHits() {
			var state = new LuaBackgroundState();
			Assert.False(state.TryGetFlyTarget(0, out _, out _));

			state.SetFlyTarget(1, 1700, -40);
			state.SetFlyTarget(9, 1, 1);   // no such player: ignored
			Assert.False(state.TryGetFlyTarget(0, out _, out _));
			Assert.True(state.TryGetFlyTarget(1, out double x, out double y));
			Assert.Equal((1700.0, -40.0), (x, y));

			state.ClearFlyTarget(1);
			Assert.False(state.TryGetFlyTarget(1, out _, out _));

			state.SetFlyTarget(0, 5, 6);
			state.landCount[0] = 3;
			state.ResetHits();
			Assert.False(state.TryGetFlyTarget(0, out _, out _));
			Assert.Equal(new int[5], state.landCount);
		}

		// note types by channel number: 0x11 don, 0x12 ka, 0x13 big don, 0x14 big ka, 0x1A/0x1B joined big notes,
		// 0x101 kadon, 0x1C bomb
		[Theory]
		[InlineData(0x11, "don", false)]
		[InlineData(0x13, "don", true)]
		[InlineData(0x1A, "don", true)]
		[InlineData(0x12, "ka", false)]
		[InlineData(0x14, "ka", true)]
		[InlineData(0x101, "kadon", false)]
		[InlineData(0x1C, "", false)]
		public void NoteNames(int note, string name, bool big) {
			Assert.Equal((name, big), LuaBackgroundState.NoteName((NotesManager.ENoteType)note));
		}

		[Fact]
		public void JudgeNames() {
			Assert.Equal("perfect", LuaBackgroundState.JudgeName(ENoteJudge.Perfect));
			Assert.Equal("great", LuaBackgroundState.JudgeName(ENoteJudge.Great));
			Assert.Equal("good", LuaBackgroundState.JudgeName(ENoteJudge.Good));
			Assert.Equal("poor", LuaBackgroundState.JudgeName(ENoteJudge.Poor));
			Assert.Equal("miss", LuaBackgroundState.JudgeName(ENoteJudge.Miss));
		}

		// what a background script and a stage see through NLua: 0-based nested arrays and the optional arguments
		[Fact]
		public void LuaReadsHitsAndForcesBackgrounds() {
			var state = new LuaBackgroundState();
			state.AddHit(0, NotesManager.ENoteType.Ka, "perfect");
			state.AddHit(0, NotesManager.ENoteType.DonBig, "miss");
			state.landCount[0] = 1;

			using var lua = new Lua();
			lua["state"] = state;
			lua["SONGMOUNT"] = new LuaSongMountFunc(_dir);
			var r = lua.DoString(@"
				local n = state.hitCount[0]
				local slot = (n - 1) % state.hitRing
				state:SetFlyTarget(0, 1700, 138)
				return n, state.hitNote[0][slot], state.hitBig[0][slot], state.hitJudge[0][slot],
					state.hitNote[0][0], state.landCount[0],
					SONGMOUNT:SetBackgrounds('bg/up'), SONGMOUNT:SetBackgrounds(nil, 'bg/empty')");

			Assert.Equal(2L, Convert.ToInt64(r[0]));
			Assert.Equal("don", r[1]);
			Assert.Equal(true, r[2]);
			Assert.Equal("miss", r[3]);
			Assert.Equal("ka", r[4]);
			Assert.Equal(1L, Convert.ToInt64(r[5]));
			Assert.Equal(true, r[6]);
			Assert.Equal(false, r[7]);
			Assert.Equal((null, null), CActImplBackground.TakeForcedBackgrounds());
			Assert.True(state.TryGetFlyTarget(0, out double x, out double y));
			Assert.Equal((1700.0, 138.0), (x, y));
		}

		// the play screen hands the target to the flying notes only on frames it drew the background
		[Fact]
		public void FlyTarget_AppliesWhileTheBackgroundIsShown() {
			var bg = new CActImplBackground();
			var state = (LuaBackgroundState)typeof(CActImplBackground)
				.GetField("_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(bg)!;
			state.SetFlyTarget(0, 10, 20);

			Assert.False(bg.FlyTarget(0, out _, out _));
			bg.Shown = true;
			Assert.True(bg.FlyTarget(0, out double x, out double y));
			Assert.Equal((10.0, 20.0), (x, y));

			bg.NoteLanded(0);
			bg.NoteLanded(5);   // no such player: ignored
			Assert.Equal(new[] { 1, 0, 0, 0, 0 }, state.landCount);
		}
	}
}
