using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// Global/Modules in the game folder: a skin module looks at the skin's modules and Lib first, a global module at the global ones
	[Collection("lua modules")]
	public class GlobalModulesTests : IDisposable {
		private readonly string _id = Guid.NewGuid().ToString("N").Substring(0, 8);
		private string Stage => "gm_stage_" + _id;
		private string Ro => "gm_ro_" + _id;
		private string LibBoth => "gm_both_" + _id;
		private string LibSkin => "gm_skin_" + _id;
		private string LibGlobal => "gm_global_" + _id;

		private readonly string _skin = Path.Combine(Path.GetTempPath(), "ot_globalmodules_" + Guid.NewGuid().ToString("N"));
		private readonly List<string> _createdGlobal = new();
		private readonly FieldInfo _skinPath = typeof(CSkin).GetField("strSystemSkinSubfolderFullName", BindingFlags.NonPublic | BindingFlags.Static)!;
		private readonly PropertyInfo _stores = typeof(OpenTaiko.OpenTaiko).GetProperty(nameof(OpenTaiko.OpenTaiko.GlobalStores))!;
		private readonly object? _oldSkinPath;
		private readonly object? _oldStores;
		private readonly string _oldConfigSkin;

		private string Probe(string tag) =>
			$"SHARED:SetSharedString(\"{tag}_both\", (require(\"{LibBoth}\")))\n" +
			$"SHARED:SetSharedString(\"{tag}_skinonly\", (require(\"{LibSkin}\")))\n" +
			$"SHARED:SetSharedString(\"{tag}_globalonly\", (require(\"{LibGlobal}\")))\n" +
			$"ROACTIVITY:GetROActivity(\"{Ro}\"):Activate()\n" +
			$"SHARED:SetSharedString(\"{tag}_ro\", SHARED:GetSharedString(\"{Ro}\"))\n";

		private string RoScript(string who) =>
			$"function activate() SHARED:SetSharedString(\"{Ro}\", \"{who}\") end\n";

		public GlobalModulesTests() {
			Write(Path.Combine(_skin, "Modules", "Lib", LibBoth + ".lua"), "return \"skin\"\n");
			Write(Path.Combine(_skin, "Modules", "Lib", LibSkin + ".lua"), "return \"skin only\"\n");
			Write(Path.Combine(_skin, "Modules", "ROActivities", Ro, "Script.lua"), RoScript("skin"));
			Write(Path.Combine(_skin, "Modules", "Stages", Stage, "Script.lua"), Probe("skin"));

			WriteGlobal("Lib", LibBoth + ".lua", "return \"global\"\n");
			WriteGlobal("Lib", LibGlobal + ".lua", "return \"global only\"\n");
			WriteGlobal("ROActivities", Path.Combine(Ro, "Script.lua"), RoScript("global"));
			WriteGlobal("Stages", Path.Combine(Stage, "Script.lua"), Probe("global"));
			// require checks files through a per-folder listing cache, and each test adds new files to the same Global/Modules/Lib
			FDK.CTexture.ClearFileListCache();

			var config = typeof(OpenTaiko.OpenTaiko).GetProperty(nameof(OpenTaiko.OpenTaiko.ConfigIni))!;
			if (config.GetValue(null) == null) config.SetValue(null, new CConfigIni());
			_oldConfigSkin = OpenTaiko.OpenTaiko.ConfigIni.strSystemSkinSubfolderFullName;
			_oldSkinPath = _skinPath.GetValue(null);
			_oldStores = _stores.GetValue(null);
			string skin = _skin + Path.DirectorySeparatorChar;
			OpenTaiko.OpenTaiko.ConfigIni.strSystemSkinSubfolderFullName = skin;
			_skinPath.SetValue(null, skin);
			_stores.SetValue(null, new LuaGlobalStores());
		}

		private static void Write(string path, string text) {
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			File.WriteAllText(path, text);
		}

		// the game folder's Global/Modules is shared with the other tests: only this test's files go there, and
		// the folders it had to make are removed afterwards
		private void WriteGlobal(string type, string relative, string text) {
			string path = Path.Combine(CSkin.GlobalModulesFolder(type), relative);
			for (string? dir = Path.GetDirectoryName(path); dir != null && !Directory.Exists(dir); dir = Path.GetDirectoryName(dir))
				_createdGlobal.Add(dir);
			Write(path, text);
			_createdGlobal.Add(path);
		}

		public void Dispose() {
			LuaStageWrapper.ResetLuaStagesDictionary();
			LuaROActivityWrapper.ResetROActivityDictionary();
			OpenTaiko.OpenTaiko.ConfigIni.strSystemSkinSubfolderFullName = _oldConfigSkin;
			_skinPath.SetValue(null, _oldSkinPath);
			_stores.SetValue(null, _oldStores);
			try { Directory.Delete(_skin, true); } catch { }
			foreach (string p in _createdGlobal) {
				try {
					if (File.Exists(p)) File.Delete(p);
					else if (Directory.Exists(p)) Directory.Delete(p, true);
				} catch { }
			}
		}

		private void LoadAll() {
			new LuaROActivityWrapper(Ro, false);
			new LuaROActivityWrapper(Ro, true);
			new LuaStageWrapper(Stage, false);
			new LuaStageWrapper(Stage, true);
		}

		private static string Shared(string key) => OpenTaiko.OpenTaiko.GlobalStores.SharedStrings[key];

		[Fact]
		public void Require_SkinModule_TakesTheSkinLibFirst() {
			LoadAll();
			Assert.Equal("skin", Shared("skin_both"));
			Assert.Equal("skin only", Shared("skin_skinonly"));
			Assert.Equal("global only", Shared("skin_globalonly"));
		}

		[Fact]
		public void Require_GlobalModule_TakesTheGlobalLibFirst() {
			LoadAll();
			Assert.Equal("global", Shared("global_both"));
			Assert.Equal("skin only", Shared("global_skinonly"));
			Assert.Equal("global only", Shared("global_globalonly"));
		}

		[Fact]
		public void RoActivityLookup_FollowsTheCallersFolder() {
			LoadAll();
			Assert.Equal("skin", Shared("skin_ro"));
			Assert.Equal("global", Shared("global_ro"));
			Assert.False(LuaROActivityWrapper.GetROActivity(Ro)!.IsGlobal);
			Assert.True(LuaROActivityWrapper.GetROActivity(Ro, true)!.IsGlobal);
		}

		[Fact]
		public void StageLookup_SkinFirst_UnlessAGlobalStageAsks() {
			LoadAll();
			Assert.False(LuaStageWrapper.GetLuaStage(Stage)!.IsGlobal);
			Assert.True(LuaStageWrapper.GetLuaStage(Stage, true)!.IsGlobal);

			LuaStageWrapper.ForceSetNextRequestedStage(Stage, true);
			Assert.True(LuaStageWrapper.GetNextRequestedStage()!.IsGlobal);
			LuaStageWrapper.ForceSetNextRequestedStage(Stage);
			Assert.False(LuaStageWrapper.GetNextRequestedStage()!.IsGlobal);
		}

		[Fact]
		public void GlobalModuleFolder_IsRecognised() {
			Assert.True(CSkin.IsGlobalModuleFolder(CSkin.GlobalModulePath("Stages", Stage)));
			Assert.False(CSkin.IsGlobalModuleFolder(Path.Combine(_skin, "Modules", "Stages", Stage)));
			Assert.False(CSkin.IsGlobalModuleFolder(Path.Combine(OpenTaiko.OpenTaiko.strEXEFolder, "Global", "Characters", "x")));
		}
	}
}
