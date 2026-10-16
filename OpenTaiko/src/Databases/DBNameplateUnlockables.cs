using System.Diagnostics;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using static OpenTaiko.DBNameplateUnlockables;

namespace OpenTaiko;

internal class DBNameplateUnlockables : CSavableT<Dictionary<string, NameplateUnlockable>> {
	public DBNameplateUnlockables() {
		tReload(null);
	}

	// the nameplate ids in load order: the shipped pack, the skin's pack, then Global/Nameplates/<folder> by name
	public readonly List<string> KeysInOrder = new();

	// a broken pack shows one notification per session, although the packs are read again with each skin load
	private readonly HashSet<string> _reportedPacks = new();

	/// <summary>
	/// Reads every nameplate pack again: the shipped one (<c>Databases/Nameplates</c>), the skin's
	/// (<c>&lt;skin&gt;/Nameplates</c>, when <paramref name="skinFolder"/> is given) and each <c>Global/Nameplates/&lt;folder&gt;</c>,
	/// then resolves the players' equipped nameplates again.
	/// </summary>
	public void tReload(string? skinFolder) {
		data.Clear();
		KeysInOrder.Clear();
		var packIds = new HashSet<string>();

		tLoadOrReport(OpenTaiko.ResolveAssetPath(@$"{OpenTaiko.strEXEFolder}Databases{Path.DirectorySeparatorChar}Nameplates"), true, packIds);
		if (skinFolder != null) {
			string skinPack = Path.Combine(skinFolder, "Nameplates");
			if (File.Exists(Path.Combine(skinPack, PackFile))) tLoadOrReport(skinPack, false, packIds);
		}
		string globalFolder = OpenTaiko.ResolveAssetPath(@$"{OpenTaiko.strEXEFolder}Global{Path.DirectorySeparatorChar}Nameplates");
		foreach (string folder in DirectoryUtils.SafeGetDirectories(globalFolder).OrderBy(f => f, StringComparer.Ordinal))
			if (File.Exists(Path.Combine(folder, PackFile))) tLoadOrReport(folder, false, packIds);

		// the save slots stay empty until the saves load, after this first read
		foreach (SaveFile? sf in OpenTaiko.SaveFileInstances)
			if (sf != null) tResolveNameplate(sf.data);
	}

	private void tLoadOrReport(string folder, bool isBase, HashSet<string> packIds) {
		string? error = tLoadPack(folder, isBase, packIds, OpenTaiko.UnlockConditionFactory, data, KeysInOrder);
		if (error == null) return;
		if (_reportedPacks.Add(folder)) LogNotification.PopError(error);
		else Trace.TraceError(error);
	}

	public class NameplateUnlockable {
		[JsonProperty("NameplateInfo")]
		public SaveFile.CNamePlateTitle nameplateInfo;

		[JsonProperty("Rarity")]
		public string rarity;

		// null = no unlock condition, the nameplate is owned by everyone
		[JsonProperty("UnlockCondition")]
		public CUnlockCondition? unlockConditions;

		// the id of the pack it comes from ("" = the shipped pack)
		[JsonProperty("Pack")]
		public string pack = "";

		// its pack's "name" (null = the pack has none, its id is shown)
		[JsonProperty("PackName")]
		public CLocalizationData? packName;
	}

	/// <summary>
	/// A nameplate id given by Lua or code: a string is used as it is, a whole number becomes its digits ("66").
	/// A negative number or nil means no nameplate ("").
	/// </summary>
	public static string tKey(object? id) {
		switch (id) {
			case string s:
				return s;
			case bool:
				return "";
			case IConvertible c:
				double d;
				try {
					d = c.ToDouble(CultureInfo.InvariantCulture);
				} catch (Exception) {
					return "";
				}
				if (d < 0 || d != Math.Floor(d) || d > long.MaxValue) return "";
				return ((long)d).ToString(CultureInfo.InvariantCulture);
			default:
				return "";
		}
	}

	/// <summary>
	/// Fills the equipped nameplate's title (in the current language), plate type and rarity from the catalogue.
	/// An empty or unknown id shows the plate without a title.
	/// </summary>
	public void tResolveNameplate(SaveFile.Data save) {
		if (data.TryGetValue(save.TitleId ?? "", out var nameplate)) {
			save.Title = nameplate.nameplateInfo.cld.GetString("");
			save.TitleType = nameplate.nameplateInfo.iType;
			save.TitleRarityInt = HRarity.tRarityToLangInt(nameplate.rarity);
		} else {
			save.Title = "";
			save.TitleType = 0;
			save.TitleRarityInt = 1;
		}
	}

	#region [ Pack files ]

	public const string PackFile = "Nameplates.json";
	public const string LangFolder = "Lang";
	public const string BasePackId = "opentaiko";

	internal class PackJson {
		// lowercase letters, digits and _; the ids of a pack's nameplates are "<pack>:<key>"
		[JsonProperty("pack")]
		public string? Pack;

		// the name the game shows for the pack, per language (optional; the pack id otherwise)
		[JsonProperty("name")]
		public CLocalizationData? Name;

		[JsonProperty("nameplates")]
		public NameplateJson?[]? Nameplates;
	}

	private static bool tIsPackId(string id) =>
		id.Length > 0 && id.All(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_');

	internal class NameplateJson {
		[JsonProperty("key")]
		public string? Key;

		// the title for every language without one in Lang/<code>.json
		[JsonProperty("title")]
		public string? Title;

		[JsonProperty("type")]
		public int Type = 0;

		[JsonProperty("rarity")]
		public string? Rarity;

		[JsonProperty("unlock")]
		public CUnlockConditionFactory.UnlockConditionJsonRaw? Unlock;
	}

	/// <summary>
	/// Reads a nameplate pack folder: <c>Nameplates.json</c>, then the optional <c>Lang/&lt;code&gt;.json</c> files
	/// (key -> title in that language). A language without a title there shows the one in <c>Nameplates.json</c>.
	/// The shipped pack's ids are its keys; any other pack's ids are "&lt;pack&gt;:&lt;key&gt;", and its pack id must be
	/// new in <paramref name="packIds"/>. The ids are appended to <paramref name="order"/> in file order.
	/// Broken entries are skipped with a warning. Returns null when the pack was read, else why it was skipped.
	/// </summary>
	internal static string? tLoadPack(string folder, bool isBase, HashSet<string> packIds, CUnlockConditionFactory factory,
		Dictionary<string, NameplateUnlockable> into, List<string> order) {
		string packPath = Path.Combine(folder, PackFile);
		PackJson? pack;
		try {
			pack = ConfigManager.JsonParse<PackJson>(File.ReadAllText(packPath, Encoding.UTF8));
		} catch (Exception ex) {
			return $"Nameplate pack {packPath} could not be read: {ex.Message}";
		}
		if (pack?.Nameplates == null)
			return $"Nameplate pack {packPath} has no \"nameplates\" list.";

		string packId = isBase ? BasePackId : (pack.Pack ?? "");
		if (!isBase && (!tIsPackId(packId) || packId == BasePackId))
			return $"Nameplate pack {packPath} needs a \"pack\" id made of lowercase letters, digits and _ (other than \"{BasePackId}\").";
		if (!packIds.Add(packId))
			return $"Nameplate pack {packPath} was skipped: another pack already has the id \"{packId}\".";
		string prefix = isBase ? "" : packId + ":";

		var added = new Dictionary<string, NameplateUnlockable>();
		foreach (NameplateJson? np in pack.Nameplates) {
			if (np == null) continue;
			if (string.IsNullOrEmpty(np.Key)) {
				Trace.TraceWarning($"Nameplates: {packPath}: skipped an entry with no key.");
				continue;
			}
			string id = prefix + np.Key;
			if (into.ContainsKey(id)) {
				Trace.TraceWarning($"Nameplates: {packPath}: skipped a second entry with key \"{np.Key}\".");
				continue;
			}
			if (np.Title == null) {
				Trace.TraceWarning($"Nameplates: {packPath}: skipped \"{np.Key}\", it has no title.");
				continue;
			}

			var nu = new NameplateUnlockable();
			nu.rarity = np.Rarity ?? "Common";
			if (!HRarity.tIsRarity(nu.rarity)) {
				Trace.TraceWarning($"Nameplates: {packPath}: \"{np.Key}\" has an unknown rarity \"{np.Rarity}\", Common is used.");
				nu.rarity = "Common";
			}
			nu.pack = isBase ? "" : packId;
			nu.packName = pack.Name;

			nu.nameplateInfo = new SaveFile.CNamePlateTitle(np.Type);
			nu.nameplateInfo.cld.SetString("default", np.Title);

			if (np.Unlock != null) {
				nu.unlockConditions = factory.GenerateUnlockObjectFromJsonRaw(np.Unlock);
				if (nu.unlockConditions is CUnlockError)
					Trace.TraceWarning($"Nameplates: {packPath}: \"{np.Key}\" has an unknown unlock condition \"{np.Unlock.Condition}\", it can never be unlocked.");
			}

			into[id] = nu;
			order.Add(id);
			added[np.Key] = nu;
		}

		string langFolder = Path.Combine(folder, LangFolder);
		if (Directory.Exists(langFolder)) {
			foreach (string langPath in Directory.GetFiles(langFolder, "*.json").OrderBy(p => p, StringComparer.Ordinal)) {
				string code = Path.GetFileNameWithoutExtension(langPath);
				Dictionary<string, string>? titles;
				try {
					titles = ConfigManager.JsonParse<Dictionary<string, string>>(File.ReadAllText(langPath, Encoding.UTF8));
				} catch (Exception ex) {
					Trace.TraceWarning($"Nameplates: {langPath} could not be read: {ex.Message}");
					continue;
				}
				if (titles == null) continue;

				foreach (var title in titles) {
					if (title.Value == null) continue;
					if (added.TryGetValue(title.Key, out var nu))
						nu.nameplateInfo.cld.SetString(code, title.Value);
					else
						Trace.TraceWarning($"Nameplates: {langPath}: no nameplate has the key \"{title.Key}\".");
				}
			}
		}

		if (!isBase) Trace.TraceInformation($"Nameplates: pack \"{packId}\" ({folder}): {added.Count} nameplates.");
		return null;
	}

	#endregion

	public void tGetUnlockedItems(int _player, ModalQueue mq) {
		int player = _player;
		var _sf = OpenTaiko.SaveFileInstances[player].data.UnlockedNameplateIds;
		bool _edited = false;

		foreach (string key in KeysInOrder) {
			NameplateUnlockable item = data[key];
			if (!_sf.Contains(key)) {
				var _fulfilled = item.unlockConditions?.tConditionMet(player, CUnlockCondition.EScreen.Internal).Item1 ?? false;

				if (_fulfilled) {
					_sf.Add(key);
					_edited = true;
					mq.tAddModal(
						new Modal(
							Modal.EModalType.Title,
							HRarity.tRarityToModalInt(item.rarity),
							new LuaNameplateInfo(item, key)
						),
						_player);

					DBSaves.RegisterUnlockedNameplate(OpenTaiko.SaveFileInstances[player].data.SaveId, key);
				}
			}
		}

		if (_edited)
			OpenTaiko.SaveFileInstances[player].tApplyHeyaChanges();
	}
}
