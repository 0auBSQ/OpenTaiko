using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// STAGES:SetNamespace / STAGES:ListNamespace: a stage declares a namespace (at the top of Script.lua or in a
	// callback) and another stage lists the namespace the way Exit("stage", name) would reach each name
	[Collection("lua modules")]
	public class StageNamespacesTests : IDisposable {
		private readonly string _id = Guid.NewGuid().ToString("N").Substring(0, 8);
		private string Ns => "CStage_" + _id;
		private string A => "ns_a_" + _id;
		private string B => "ns_b_" + _id;
		private string C => "ns_c_" + _id;
		private string D => "ns_d_" + _id;
		private string E => "ns_e_" + _id;
		private string Probe => "ns_probe_" + _id;
		private string Ro => "ns_ro_" + _id;

		private readonly string _skin = Path.Combine(Path.GetTempPath(), "ot_stagens_" + Guid.NewGuid().ToString("N"));
		private readonly List<string> _createdGlobal = new();
		private readonly FieldInfo _skinPath = typeof(CSkin).GetField("strSystemSkinSubfolderFullName", BindingFlags.NonPublic | BindingFlags.Static)!;
		private readonly PropertyInfo _stores = typeof(OpenTaiko.OpenTaiko).GetProperty(nameof(OpenTaiko.OpenTaiko.GlobalStores))!;
		private readonly object? _oldSkinPath;
		private readonly object? _oldStores;
		private readonly string _oldConfigSkin;
		private readonly List<LuaStageWrapper> _stages = new();

		// writes what the namespace lists into the shared string "<tag>": name|isGlobal|title|order|tags per entry
		private string ProbeScript(string tag) =>
			"function onStart()\n" +
			$"    local out = {{}}\n" +
			$"    for _, e in ipairs(STAGES:ListNamespace(\"{Ns}\")) do\n" +
			"        local t = e.info.title\n" +
			"        if type(t) == \"table\" then t = t.ja end\n" +
			"        local tags = e.info.tags and table.concat(e.info.tags, \"+\") or \"\"\n" +
			"        out[#out + 1] = e.name .. \"|\" .. tostring(e.isGlobal) .. \"|\" .. tostring(t) .. \"|\" .. tostring(e.info.order) .. \"|\" .. tags\n" +
			"    end\n" +
			$"    SHARED:SetSharedString(\"{tag}\", table.concat(out, \",\"))\n" +
			"end\n";

		public StageNamespacesTests() {
			WriteSkin("Stages", A, $"STAGES:SetNamespace(\"{Ns}\", {{ title = {{ en = \"A\", ja = \"エー\" }}, order = 2, tags = {{ \"x\", \"y\" }}, skip = function() end }})\n");
			WriteSkin("Stages", B, $"function onStart() SHARED:SetSharedString(\"{B}\", tostring(STAGES:SetNamespace(\"{Ns}\", {{ title = \"B\" }}))) end\n");
			WriteSkin("Stages", C, "STAGES:SetNamespace(\"Other\", { title = \"C\" })\n");
			WriteSkin("Stages", D, "-- no namespace on the skin side\n");
			WriteSkin("Stages", E, $"STAGES:SetNamespace(\"{Ns}\", {{ title = \"E\" }})\nSTAGES:SetNamespace(nil)\n");
			WriteSkin("Stages", Probe, ProbeScript("skin_list"));
			WriteSkin("ROActivities", Ro, $"SHARED:SetSharedString(\"{Ro}\", tostring(STAGES:SetNamespace(\"{Ns}\")))\n");

			WriteGlobal("Stages", Path.Combine(D, "Script.lua"), $"STAGES:SetNamespace(\"{Ns}\", {{ title = \"D\" }})\n");
			WriteGlobal("Stages", Path.Combine(Probe, "Script.lua"), ProbeScript("global_list"));
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

		private void WriteSkin(string type, string name, string text) => Write(Path.Combine(_skin, "Modules", type, name, "Script.lua"), text);

		private static void Write(string path, string text) {
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			File.WriteAllText(path, text);
		}

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

		// as the skin loads them: every Script.lua first (skin, then global), then every onStart
		private void LoadAll() {
			foreach (string n in new[] { A, B, C, D, E, Probe }) _stages.Add(new LuaStageWrapper(n, false));
			foreach (string n in new[] { D, Probe }) _stages.Add(new LuaStageWrapper(n, true));
			foreach (var s in _stages) {
				s.BeginOnStart();
				while (s.StepOnStart(out _)) { }
			}
		}

		private static string Shared(string key) => OpenTaiko.OpenTaiko.GlobalStores.SharedStrings[key];

		[Fact]
		public void Namespace_SetAtTheTopOrInOnStart_IsListed() {
			LoadAll();
			Assert.Equal("true", Shared(B));
			var names = LuaStageWrapper.GetStagesInNamespace(Ns).Select(s => s.customStageName).ToArray();
			Assert.Equal(new[] { A, B }, names);
		}

		[Fact]
		public void Listing_FollowsTheSideTheCallerReaches() {
			LoadAll();
			// a skin caller reaches the skin's D, which declared nothing; a global caller the global D
			Assert.Equal($"{A}|false|エー|2|x+y,{B}|false|B|nil|", Shared("skin_list"));
			Assert.Equal($"{A}|false|エー|2|x+y,{B}|false|B|nil|,{D}|true|D|nil|", Shared("global_list"));
			Assert.True(LuaStageWrapper.GetStagesInNamespace(Ns, true).Last().IsGlobal);
		}

		[Fact]
		public void Namespace_CanBeCleared_AndOtherNamespacesStayApart() {
			LoadAll();
			Assert.DoesNotContain(LuaStageWrapper.GetStagesInNamespace(Ns), s => s.customStageName == E);
			Assert.Equal(new[] { C }, LuaStageWrapper.GetStagesInNamespace("Other").Select(s => s.customStageName).ToArray());
			Assert.Empty(LuaStageWrapper.GetStagesInNamespace(""));
		}

		[Fact]
		public void SetNamespace_OutsideAStage_DoesNothing() {
			new LuaROActivityWrapper(Ro, false);
			Assert.Equal("false", Shared(Ro));
		}
	}
}
