namespace OpenTaiko {
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

			object _ps = null;

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
				default:
					break;
			};

			bool sectionIsValid = _ps != null ? ((Dictionary<string, DBSkinPreset.SkinScene>)_ps).Count > 0 : false;

			var preset = (sectionIsValid
					&& OpenTaiko.stageSongSelect.rChoosenSong.strScenePreset != null
					&& ((Dictionary<string, DBSkinPreset.SkinScene>)_ps).ContainsKey(OpenTaiko.stageSongSelect.rChoosenSong.strScenePreset))
				? ((Dictionary<string, DBSkinPreset.SkinScene>)_ps)[OpenTaiko.stageSongSelect.rChoosenSong.strScenePreset]
				: null;

			if (sectionIsValid
					&& OpenTaiko.DTX.scenePreset != null
					&& ((Dictionary<string, DBSkinPreset.SkinScene>)_ps).ContainsKey(OpenTaiko.DTX.scenePreset)) // If currently selected song has valid SCENEPRESET metadata within TJA
			{
				preset = ((Dictionary<string, DBSkinPreset.SkinScene>)_ps)[OpenTaiko.DTX.scenePreset];
			} else if (sectionIsValid
					  && OpenTaiko.stageSongSelect.rChoosenSong.strScenePreset != null
					  && ((Dictionary<string, DBSkinPreset.SkinScene>)_ps).ContainsKey(OpenTaiko.stageSongSelect.rChoosenSong.strScenePreset)) {
				preset = ((Dictionary<string, DBSkinPreset.SkinScene>)_ps)[OpenTaiko.stageSongSelect.rChoosenSong.strScenePreset];
			} else if (sectionIsValid
					  && ((Dictionary<string, DBSkinPreset.SkinScene>)_ps).ContainsKey("")) {
				preset = ((Dictionary<string, DBSkinPreset.SkinScene>)_ps)[""];
			} else if (sectionIsValid) {
				var cstps = (Dictionary<string, DBSkinPreset.SkinScene>)_ps;
				Random rand = new Random();
				preset = cstps.ElementAt(rand.Next(0, cstps.Count)).Value;
			}

			return preset;
		}
	}
}
