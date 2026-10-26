using System.Diagnostics;
using Newtonsoft.Json;

namespace OpenTaiko;

/// <summary>
/// The selectable notes and judges packs of the current skin, in numbered subfolders of Graphics/5_Game/:
/// "Notes Skin (Taiko)/n", "Notes Skin (Bongo)/n" and "Judges Skin/n", each with a Definition.json.
/// A file missing from the selected pack is taken from the category's first pack; a skin without the
/// category's folder keeps its former files (<see cref="CNotesSkinDef.Legacy"/>).
/// </summary>
internal static class CNotesSkins {
	public enum EKind { Taiko, Bongo, Judges }

	public sealed class Pack {
		public string Folder = "";      // the subfolder's name ("1", "2", ...)
		public string RelDir = "";      // relative to Graphics/, with a trailing separator
		public CNotesSkinDef Def = new();
		public string Name => this.Def.Name.GetString(this.Folder);
	}

	private static readonly string[] Dirs = { "Notes Skin (Taiko)", "Notes Skin (Bongo)", "Judges Skin" };
	private static readonly string[] SettingIds = { "@notes_skin_taiko", "@notes_skin_bongo", "@judges_skin" };
	private static readonly Pack[][] _packs = { [], [], [] };
	private static readonly int[] _selected = new int[3];

	public static IReadOnlyList<Pack> Packs(EKind kind) => _packs[(int)kind];
	public static int SelectedIndex(EKind kind) => _selected[(int)kind];
	public static bool HasPacks(EKind kind) => _packs[(int)kind].Length > 0;
	public static Pack? Selected(EKind kind) => HasPacks(kind) ? _packs[(int)kind][_selected[(int)kind]] : null;

	/// <summary>The definition drawing uses for a game type: the selected pack's, or the former layout's.</summary>
	public static CNotesSkinDef Def(EGameType gt) {
		var kind = gt is EGameType.Konga ? EKind.Bongo : EKind.Taiko;
		return Selected(kind)?.Def ?? (gt is EGameType.Konga ? _legacyKonga : _legacyTaiko);
	}
	private static CNotesSkinDef _legacyTaiko = CNotesSkinDef.LegacyKonga, _legacyKonga = CNotesSkinDef.LegacyKonga;

	/// <summary>Finds the packs of the current skin and restores the selection stored in its theme settings.</summary>
	public static void Scan() {
		_legacyTaiko = CNotesSkinDef.LegacyTaiko;
		_legacyKonga = CNotesSkinDef.LegacyKonga;
		string gameDir = CSkin.Path(TextureLoader.BASE + TextureLoader.GAME);
		for (int k = 0; k < 3; k++) {
			var list = new List<Pack>();
			string root = Path.Combine(gameDir, Dirs[k]);
			if (Directory.Exists(root)) {
				foreach (var dir in Directory.GetDirectories(root)) {
					string folder = Path.GetFileName(dir);
					if (!int.TryParse(folder, out _)) continue;
					var pack = new Pack {
						Folder = folder,
						RelDir = TextureLoader.GAME + Dirs[k] + Path.DirectorySeparatorChar + folder + Path.DirectorySeparatorChar,
					};
					string defPath = Path.Combine(dir, "Definition.json");
					if (File.Exists(defPath)) {
						try {
							pack.Def = JsonConvert.DeserializeObject<CNotesSkinDef>(File.ReadAllText(defPath)) ?? new();
						} catch (Exception ex) {
							Trace.TraceError($"[NotesSkins] {defPath}: {ex.Message}");
						}
					}
					list.Add(pack);
				}
			}
			list.Sort((a, b) => int.Parse(a.Folder).CompareTo(int.Parse(b.Folder)));
			_packs[k] = list.ToArray();
			string stored = OpenTaiko.Databases?.DBThemeSettings?.GetRaw(SettingIds[k]) ?? "";
			_selected[k] = Math.Max(0, Array.FindIndex(_packs[k], p => p.Folder == stored));
		}
	}

	public static void Select(EKind kind, int index) {
		int k = (int)kind;
		if (_packs[k].Length == 0) return;
		_selected[k] = Math.Clamp(index, 0, _packs[k].Length - 1);
		OpenTaiko.Databases?.DBThemeSettings?.SetRaw(SettingIds[k], _packs[k][_selected[k]].Folder);
	}

	/// <summary>A pack file relative to Graphics/: the selected pack's, else the first pack's; null when neither has it
	/// (or when <paramref name="selectedOnly"/> and the selected pack lacks it).</summary>
	public static string? File_(EKind kind, string rel, bool selectedOnly = false) {
		var packs = _packs[(int)kind];
		if (packs.Length == 0) return null;
		foreach (var pack in selectedOnly ? new[] { packs[_selected[(int)kind]] } : new[] { packs[_selected[(int)kind]], packs[0] }) {
			if (File.Exists(CSkin.Path(TextureLoader.BASE + pack.RelDir + rel))) return pack.RelDir + rel;
		}
		return null;
	}

	/// <summary>A pack folder relative to Graphics/, chosen like <see cref="File_"/>.</summary>
	public static string? Dir(EKind kind, string rel) {
		var packs = _packs[(int)kind];
		if (packs.Length == 0) return null;
		foreach (var pack in new[] { packs[_selected[(int)kind]], packs[0] }) {
			if (Directory.Exists(CSkin.Path(TextureLoader.BASE + pack.RelDir + rel))) return pack.RelDir + rel;
		}
		return null;
	}

	/// <summary>The settings menu's preview of a pack: its Preview.png, else its main sheet (absolute paths).</summary>
	public static string PreviewPath(EKind kind, Pack pack) {
		string dir = CSkin.Path(TextureLoader.BASE + pack.RelDir);
		string preview = Path.Combine(dir, "Preview.png");
		return File.Exists(preview) ? preview : Path.Combine(dir, kind is EKind.Judges ? "Judge.png" : "Notes.png");
	}

	// whether reaching this combo starts a sheet's entry rows somewhere
	public static bool IsEntryCombo(int combo) {
		foreach (var def in new[] { Def(EGameType.Taiko), Def(EGameType.Konga) })
			foreach (var sheet in new[] { def.Notes, def.Rolls, def.Balloons })
				if (sheet.Animation.Any(t => t.FromCombo == combo && t.RowsOnEntry != null)) return true;
		return false;
	}
}

/// <summary>The animation state of a player's notes: the combo, the step of the beat (0-3, 4 per beat) and whether
/// the combo has just reached a tier with entry rows.</summary>
internal readonly record struct CNoteAnim(int Combo, int Step, bool Entry) {
	public static readonly CNoteAnim Still = new(0, 0, false);
}

/// <summary>Definition.json of a notes pack.</summary>
internal class CNotesSkinDef {
	[JsonProperty("Name")]
	public CLocalizationData Name { get; set; } = new();

	[JsonProperty("Notes")]
	public CSheetDef Notes { get; set; } = new();

	[JsonProperty("Rolls")]
	public CSheetDef Rolls { get; set; } = new();

	[JsonProperty("Balloons")]
	public CSheetDef Balloons { get; set; } = new();

	/// <summary>The Notes.png row flying notes are drawn with.</summary>
	[JsonProperty("FlyingNotesFrame")]
	public int FlyingNotesFrame { get; set; } = 0;

	/// <summary>The scale of a partner note when the pack has no Notes_Partner.png.</summary>
	[JsonProperty("PartnerNotesScale")]
	public float PartnerNotesScale { get; set; } = MathF.Sqrt(2f);

	/// <summary>The scale of a giant note when the pack has no Notes_Giant.png.</summary>
	[JsonProperty("GiantNotesScale")]
	public float GiantNotesScale { get; set; } = 2f;

	/// <summary>True for the former layout (a skin without packs): Notes.png with the hit circle, rolls and balloon,
	/// Swap.png, Adlib.png, Kusu.png and FuseRoll.png.</summary>
	[JsonIgnore]
	public bool Legacy { get; private init; }

	private static List<CAnimTier> LegacyAnimation() => OpenTaiko.Skin.Game_Notes_Anime ? [
		new() { FromCombo = 0, Rows = [0] },
		new() { FromCombo = 50, Rows = [1, 1, 0, 0], RowsOnEntry = [0] },
		new() { FromCombo = 150, Rows = [0, 1] },
		new() { FromCombo = 300, Rows = [0, 2], RowsOnEntry = [0, 1] },
	] : [];

	public static CNotesSkinDef LegacyTaiko => new() {
		Legacy = true,
		Notes = new() { Frames = 4, Animation = LegacyAnimation() },
		Rolls = new() { Frames = 4, Animation = LegacyAnimation() },
		Balloons = new() { Frames = 4, Animation = LegacyAnimation() },
		FlyingNotesFrame = 3,
	};

	public static CNotesSkinDef LegacyKonga => new() { Legacy = true };
}

internal class CSheetDef {
	/// <summary>The sheet's row count: one row per animation frame.</summary>
	[JsonProperty("Frames")]
	public int Frames { get; set; } = 1;

	/// <summary>The rows shown by combo; without tiers the sheet shows its first row.</summary>
	[JsonProperty("Animation")]
	public List<CAnimTier> Animation { get; set; } = [];

	/// <summary>The row to draw for an animation state.</summary>
	public int Row(CNoteAnim anim) {
		CAnimTier? tier = null;
		foreach (var t in this.Animation)
			if (anim.Combo >= t.FromCombo && (tier == null || t.FromCombo >= tier.FromCombo)) tier = t;
		if (tier == null) return 0;
		int[] rows = (anim.Entry && tier.RowsOnEntry is { Length: > 0 } entry) ? entry : tier.Rows;
		if (rows.Length == 0) return 0;
		return Math.Clamp(rows[anim.Step % rows.Length], 0, Math.Max(0, this.Frames - 1));
	}
}

/// <summary>From a combo on: the row of each step of the beat (4 steps per beat, the list repeats when shorter),
/// and optionally the rows used just after the combo reaches it.</summary>
internal class CAnimTier {
	[JsonProperty("FromCombo")]
	public int FromCombo { get; set; }

	[JsonProperty("Rows")]
	public int[] Rows { get; set; } = [0];

	[JsonProperty("RowsOnEntry")]
	public int[]? RowsOnEntry { get; set; }
}
