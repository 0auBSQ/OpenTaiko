using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// nameplate packs: the shipped files, pack ids and prefixed ids, translations by key, broken packs
	public class NameplatePackTests : IDisposable {
		private readonly string dir = Path.Combine(Path.GetTempPath(), "np_packs_" + Guid.NewGuid().ToString("N"));
		private readonly CUnlockConditionFactory factory = new();

		public NameplatePackTests() {
			Directory.CreateDirectory(dir);
		}

		public void Dispose() {
			try { Directory.Delete(dir, true); } catch (IOException) { }
		}

		private string Pack(string name, string json, Dictionary<string, string>? lang = null) {
			string folder = Path.Combine(dir, name);
			Directory.CreateDirectory(Path.Combine(folder, "Lang"));
			File.WriteAllText(Path.Combine(folder, "Nameplates.json"), json);
			foreach (var (code, text) in lang ?? new())
				File.WriteAllText(Path.Combine(folder, "Lang", code + ".json"), text);
			return folder;
		}

		private sealed class Collect : TraceListener {
			public readonly List<string> Lines = new();
			public override void Write(string? message) { }
			public override void WriteLine(string? message) { lock (Lines) Lines.Add(message ?? ""); }
		}

		[Fact]
		public void ShippedNameplates_LoadWithoutWarnings() {
			string folder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "OpenTaiko", "Databases", "Nameplates"));
			var data = new Dictionary<string, DBNameplateUnlockables.NameplateUnlockable>();
			var order = new List<string>();
			var listener = new Collect();
			Trace.Listeners.Add(listener);
			try {
				Assert.Null(DBNameplateUnlockables.tLoadPack(folder, true, new HashSet<string>(), factory, data, order));
			} finally {
				Trace.Listeners.Remove(listener);
			}
			Assert.DoesNotContain(listener.Lines, l => l.Contains(folder));
			Assert.True(order.Count > 380);
			Assert.Equal("Beginner", data["0"].nameplateInfo.cld.Strings["default"]);
			Assert.Null(data["0"].unlockConditions);
			Assert.Equal(("", "OpenTaiko"), (data["66"].pack, data["66"].packName!.Strings["default"]));
		}

		[Fact]
		public void PackIds_PrefixTheKeys_AndTranslationsUseTheKeys() {
			string folder = Pack("Example Pack", """
				{
					"pack": "example_pack",
					"name": { "strings": { "default": "Example Pack", "fr": "Pack d'exemple" } },
					"nameplates": [
						{ "key": "night_sky", "title": "Night Sky Walker", "type": 8, "rarity": "Rare",
						  "unlock": { "condition": "tp", "values": [50] } },
						{ "key": "hello", "title": "Hello There" }
					]
				}
				""", new() { ["fr"] = "{ \"night_sky\": \"Promeneur du ciel nocturne\" }" });

			var data = new Dictionary<string, DBNameplateUnlockables.NameplateUnlockable>();
			var order = new List<string>();
			Assert.Null(DBNameplateUnlockables.tLoadPack(folder, false, new HashSet<string>(), factory, data, order));

			Assert.Equal(new[] { "example_pack:night_sky", "example_pack:hello" }, order);
			var np = data["example_pack:night_sky"];
			Assert.Equal("Night Sky Walker", np.nameplateInfo.cld.Strings["default"]);
			Assert.Equal("Promeneur du ciel nocturne", np.nameplateInfo.cld.Strings["fr"]);
			Assert.Equal((8, "Rare", "example_pack"), (np.nameplateInfo.iType, np.rarity, np.pack));
			Assert.Equal(("Example Pack", "Pack d'exemple"), (np.packName!.Strings["default"], np.packName.Strings["fr"]));
			Assert.Equal("CUnlockTP", np.unlockConditions!.GetType().Name);
			Assert.Null(data["example_pack:hello"].unlockConditions);
			Assert.Equal("Common", data["example_pack:hello"].rarity);
		}

		[Fact]
		public void BadPackIds_AndBrokenFiles_SkipThePack() {
			const string entry = "\"nameplates\": [ { \"key\": \"a\", \"title\": \"A\" } ]";
			var packIds = new HashSet<string>();
			var data = new Dictionary<string, DBNameplateUnlockables.NameplateUnlockable>();
			var order = new List<string>();
			string? Load(string folder) => DBNameplateUnlockables.tLoadPack(folder, false, packIds, factory, data, order);

			Assert.NotNull(Load(Pack("no_id", "{ " + entry + " }")));
			Assert.NotNull(Load(Pack("reserved", "{ \"pack\": \"opentaiko\", " + entry + " }")));
			Assert.NotNull(Load(Pack("upper", "{ \"pack\": \"Mine\", " + entry + " }")));
			Assert.NotNull(Load(Pack("broken", "{ \"pack\": \"broken\", \"nameplates\": [ { \"key\": ")));
			Assert.Null(Load(Pack("first", "{ \"pack\": \"mine\", " + entry + " }")));
			Assert.NotNull(Load(Pack("second", "{ \"pack\": \"mine\", " + entry + " }")));
			Assert.Equal(new[] { "mine:a" }, order);
			Assert.Null(data["mine:a"].packName);   // no "name": the strip shows the pack id
		}
	}
}
