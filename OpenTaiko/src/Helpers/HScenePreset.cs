namespace OpenTaiko;

class HScenePreset {
	public static DBSkinPreset.SkinScene? GetBGPreset() {
		string presetSection = "";
		if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
			presetSection = "Tower";
		} else if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
			presetSection = "Dan";
		} else if (OpenTaiko.ConfigIni.bAIBattleMode) {
			presetSection = "AI";
		} else {
			presetSection = "Regular";
		}

		Dictionary<string, DBSkinPreset.SkinScene> _ps = [];

		switch (presetSection) {
			case "Regular":
				_ps = OpenTaiko.Skin.Game_SkinScenes.Regular;
				break;
			case "Dan":
				_ps = OpenTaiko.Skin.Game_SkinScenes.Dan;
				break;
			case "Tower":
				_ps = OpenTaiko.Skin.Game_SkinScenes.Tower;
				break;
			case "AI":
				_ps = OpenTaiko.Skin.Game_SkinScenes.AI;
				break;
		};

		bool sectionIsValid = _ps.Count > 0;

#if DEBUG
		if (!string.IsNullOrWhiteSpace(ImGuiDebugWindow.OverrideBGPreset))
			return _ps.TryGetValue(ImGuiDebugWindow.OverrideBGPreset, out var value) ? value : null;
#endif
		if (!sectionIsValid)
			return null;

		Random? rand = null;
		foreach (var list in new string[][]{
			OpenTaiko.TJA?.scenePresets ?? [], // song tja
			CTja.SplitComma(OpenTaiko.stageSongSelect.rChoosenSong.strScenePresets ?? ""), // box.def
			[""], // fallback
			}) {
			var options = list.Select(k => _ps.GetValueOrDefault(k)).Where(k => k != null).ToArray();
			if (options.Length <= 0)
				continue;

			// random among specified scenes
			rand = new Random();
			return options.ElementAt(rand.Next(0, options.Length));
		}

		// random among all scenes
		rand ??= new Random();
		return _ps.ElementAt(rand.Next(0, _ps.Count)).Value;
	}
}
