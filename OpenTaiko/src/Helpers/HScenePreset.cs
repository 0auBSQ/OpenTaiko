namespace OpenTaiko;

class HScenePreset {
	public static DBSkinPreset.SkinScene GetBGPreset() {
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
		if (sectionIsValid && _ps.TryGetValue(OpenTaiko.TJA?.scenePreset ?? OpenTaiko.stageSongSelect.rChoosenSong.strScenePreset ?? "", out var result_song)) {
			return result_song;
		}
		else if (sectionIsValid && _ps.TryGetValue(OpenTaiko.stageSongSelect.rChoosenSong.strScenePreset ?? "", out var result_box)) {
			return result_box;
		}
		else if (sectionIsValid && _ps.TryGetValue("", out var result_fallback)) {
			return result_fallback;
		}
		else if (sectionIsValid) {
			Random rand = new Random();
			return _ps.ElementAt(rand.Next(0, _ps.Count)).Value;
		}

		return null;
	}
}
