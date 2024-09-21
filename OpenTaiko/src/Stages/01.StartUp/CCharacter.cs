namespace OpenTaiko {
	class CCharacter {
		public DBCharacter.CharacterData metadata;
		public DBCharacter.CharacterEffect effect;
		public DBUnlockables.CUnlockConditions unlock;
		public string _path;
		public int _idx;

		public float GetEffectCoinMultiplier() {
			float mult = 1f;

			mult *= HRarity.tRarityToRarityToCoinMultiplier(metadata.Rarity);
			mult *= effect.GetCoinMultiplier();

			return mult;
		}

		public void tGetUnlockedItems(int _player, ModalQueue mq) {
			int player = OpenTaiko.GetActualPlayer(_player);
			var _sf = OpenTaiko.SaveFileInstances[player].data.UnlockedCharacters;
			bool _edited = false;

			var _npvKey = Path.GetFileName(_path);

			if (!_sf.Contains(_npvKey)) {
				var _fulfilled = unlock?.tConditionMetWrapper(player, DBUnlockables.CUnlockConditions.EScreen.Internal).Item1 ?? false;

				if (_fulfilled) {
					_sf.Add(_npvKey);
					_edited = true;
					mq.tAddModal(
						new Modal(
							Modal.EModalType.Character,
							HRarity.tRarityToModalInt(metadata.Rarity),
							this,
							OpenTaiko.Tx.Characters_Heya_Render[_idx]
							),
						_player);

					DBSaves.RegisterStringUnlockedAsset(OpenTaiko.SaveFileInstances[player].data.SaveId, "unlocked_characters", _npvKey);
				}
			}

			if (_edited)
				OpenTaiko.SaveFileInstances[player].tApplyHeyaChanges();
		}

		public CCharacter(string path, int i) {
			_path = path;
			_idx = i;

			// Character metadata
			if (File.Exists($@"{path}{Path.DirectorySeparatorChar}Metadata.json"))
				metadata = ConfigManager.GetConfig<DBCharacter.CharacterData>($@"{path}{Path.DirectorySeparatorChar}Metadata.json");
			else
				metadata = new DBCharacter.CharacterData();

			// Character metadata
			if (File.Exists($@"{path}{Path.DirectorySeparatorChar}Effects.json"))
				effect = ConfigManager.GetConfig<DBCharacter.CharacterEffect>($@"{path}{Path.DirectorySeparatorChar}Effects.json");
			else
				effect = new DBCharacter.CharacterEffect();

			// Character unlockables
			if (File.Exists($@"{path}{Path.DirectorySeparatorChar}Unlock.json"))
				unlock = ConfigManager.GetConfig<DBUnlockables.CUnlockConditions>($@"{path}{Path.DirectorySeparatorChar}Unlock.json");
			else
				unlock = null;
		}
	}
}
