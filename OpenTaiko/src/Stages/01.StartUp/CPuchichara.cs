using FDK;
using Silk.NET.Maths;


namespace TJAPlayer3 {
	class CPuchichara {
		public CTexture tx;
		public CTexture render;
		public CSkin.CSystemSound welcome;
		public DBPuchichara.PuchicharaData metadata;
		public DBPuchichara.PuchicharaEffect effect;
		public DBUnlockables.CUnlockConditions unlock;
		public string _path;

		public float GetEffectCoinMultiplier() {
			float mult = 1f;

			mult *= HRarity.tRarityToRarityToCoinMultiplier(metadata.Rarity);
			mult *= effect.GetCoinMultiplier();

			return mult;
		}

		public void tGetUnlockedItems(int _player, ModalQueue mq) {
			int player = TJAPlayer3.GetActualPlayer(_player);
			var _sf = TJAPlayer3.SaveFileInstances[player].data.UnlockedPuchicharas;
			bool _edited = false;

			var _npvKey = Path.GetFileName(_path);

			if (!_sf.Contains(_npvKey)) {
				var _fulfilled = unlock?.tConditionMetWrapper(player, DBUnlockables.CUnlockConditions.EScreen.Internal).Item1 ?? false;

				if (_fulfilled) {
					_sf.Add(_npvKey);
					_edited = true;
					mq.tAddModal(
						new Modal(
							Modal.EModalType.Puchichara,
							HRarity.tRarityToModalInt(metadata.Rarity),
							metadata.tGetName()
							),
						_player);

					DBSaves.RegisterStringUnlockedAsset(TJAPlayer3.SaveFileInstances[player].data.SaveId, "unlocked_puchicharas", _npvKey);
				}
			}

			if (_edited)
				TJAPlayer3.SaveFileInstances[player].tApplyHeyaChanges();
		}

		public CPuchichara(string path) {
			_path = path;

			// Puchichara textures
			tx = TJAPlayer3.Tx.TxCAbsolute($@"{path}{Path.DirectorySeparatorChar}Chara.png");
			if (tx != null) {
				tx.vcScaleRatio = new Vector3D<float>(TJAPlayer3.Skin.Game_PuchiChara_Scale[0]);
			}

			// Heya render
			render = TJAPlayer3.Tx.TxCAbsolute($@"{path}{Path.DirectorySeparatorChar}Render.png");

			// Puchichara welcome sfx
			welcome = new CSkin.CSystemSound($@"{path}{Path.DirectorySeparatorChar}Welcome.ogg", false, false, true, ESoundGroup.Voice);

			// Puchichara metadata
			if (File.Exists($@"{path}{Path.DirectorySeparatorChar}Metadata.json"))
				metadata = ConfigManager.GetConfig<DBPuchichara.PuchicharaData>($@"{path}{Path.DirectorySeparatorChar}Metadata.json");
			else
				metadata = new DBPuchichara.PuchicharaData();

			// Puchichara metadata
			if (File.Exists($@"{path}{Path.DirectorySeparatorChar}Effects.json"))
				effect = ConfigManager.GetConfig<DBPuchichara.PuchicharaEffect>($@"{path}{Path.DirectorySeparatorChar}Effects.json");
			else
				effect = new DBPuchichara.PuchicharaEffect();

			// Puchichara unlockables
			if (File.Exists($@"{path}{Path.DirectorySeparatorChar}Unlock.json"))
				unlock = ConfigManager.GetConfig<DBUnlockables.CUnlockConditions>($@"{path}{Path.DirectorySeparatorChar}Unlock.json");
			else
				unlock = null;
		}
	}
}
