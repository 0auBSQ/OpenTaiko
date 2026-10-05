using System;
using System.IO;
using System.Reflection;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// closing the game runs the skin modules' onDestroy, as a skin change does, without disposing them
	[Collection("lua modules")]
	public class SkinExitTests : IDisposable {
		private const string Stage = "exit_probe";
		private readonly string _dir = Path.Combine(Path.GetTempPath(), "ot_skinexit_" + Guid.NewGuid().ToString("N"));
		private readonly FieldInfo _skinPath = typeof(CSkin).GetField("strSystemSkinSubfolderFullName", BindingFlags.NonPublic | BindingFlags.Static)!;
		private readonly PropertyInfo _stores = typeof(OpenTaiko.OpenTaiko).GetProperty(nameof(OpenTaiko.OpenTaiko.GlobalStores))!;
		private readonly object? _oldSkinPath;
		private readonly object? _oldStores;
		private readonly string _oldConfigSkin;

		public SkinExitTests() {
			string stageDir = Path.Combine(_dir, "Modules", "Stages", Stage);
			Directory.CreateDirectory(stageDir);
			File.WriteAllText(Path.Combine(stageDir, "Script.lua"),
				"destroyed = 0\n" +
				"function onDestroy()\n" +
				"    destroyed = destroyed + 1\n" +
				"    SHARED:SetSharedString(\"exit_probe\", tostring(destroyed))\n" +
				"end\n");

			// the module's Lua VM also reads the skin folder from the config (its setter is private)
			var config = typeof(OpenTaiko.OpenTaiko).GetProperty(nameof(OpenTaiko.OpenTaiko.ConfigIni))!;
			if (config.GetValue(null) == null) config.SetValue(null, new CConfigIni());
			_oldConfigSkin = OpenTaiko.OpenTaiko.ConfigIni.strSystemSkinSubfolderFullName;
			_oldSkinPath = _skinPath.GetValue(null);
			_oldStores = _stores.GetValue(null);
			string skin = _dir + Path.DirectorySeparatorChar;
			OpenTaiko.OpenTaiko.ConfigIni.strSystemSkinSubfolderFullName = skin;
			_skinPath.SetValue(null, skin);
			_stores.SetValue(null, new LuaGlobalStores());
		}

		public void Dispose() {
			if (LuaStageWrapper._allLuaStages.TryGetValue(Stage, out var stage)) {
				stage.DisposeStage();
				LuaStageWrapper._allLuaStages.Remove(Stage);
			}
			OpenTaiko.OpenTaiko.ConfigIni.strSystemSkinSubfolderFullName = _oldConfigSkin;
			_skinPath.SetValue(null, _oldSkinPath);
			_stores.SetValue(null, _oldStores);
			try { Directory.Delete(_dir, true); } catch { }
		}

		[Fact]
		public void DestroyModulesOnExit_RunsEachStagesOnDestroyOnce_AndKeepsTheStage() {
			var stage = new LuaStageWrapper(Stage);

			CSkin.DestroyModulesOnExit();

			Assert.Equal("1", OpenTaiko.OpenTaiko.GlobalStores.SharedStrings["exit_probe"]);
			Assert.Same(stage, LuaStageWrapper.GetLuaStage(Stage));
		}

		[Fact]
		public void DestroyModulesOnExit_WithNoModules_DoesNothing() {
			Assert.Null(LuaStageWrapper.GetLuaStage(Stage));
			CSkin.DestroyModulesOnExit();
			Assert.False(OpenTaiko.OpenTaiko.GlobalStores.SharedStrings.ContainsKey("exit_probe"));
		}
	}
}
