using FDK;
using Silk.NET.Maths;


namespace OpenTaiko;

class CPuchichara {
	public CTexture? tx;
	public CSkin.CSystemSound welcome;
	public DBPuchichara.PuchicharaData metadata;
	public DBPuchichara.PuchicharaEffect effect;
	public CUnlockCondition? unlock;
	public string _path;

	public float GetEffectCoinMultiplier(bool allPurpleEnabled = true, bool autorollEnabled = true, bool showAdlibEnabled = true, bool splitLaneEnabled = true)
		=> effect.GetCoinMultiplier(HRarity.tRarityToRarityToCoinMultiplier(metadata.Rarity), allPurpleEnabled, autorollEnabled, showAdlibEnabled, splitLaneEnabled);

	public void tGetUnlockedItems(int _player, ModalQueue mq) {
		int player = _player;
		var _sf = OpenTaiko.SaveFileInstances[player].data.UnlockedPuchicharas;
		bool _edited = false;

		var _npvKey = Path.GetFileName(_path);

		if (!_sf.Contains(_npvKey)) {
			var _fulfilled = unlock?.tConditionMet(player, CUnlockCondition.EScreen.Internal).Item1 ?? false;

			if (_fulfilled) {
				_sf.Add(_npvKey);
				_edited = true;
				mq.tAddModal(
					new Modal(
						Modal.EModalType.Puchichara,
						HRarity.tRarityToModalInt(metadata.Rarity),
						this
					),
					_player);

				DBSaves.RegisterStringUnlockedAsset(OpenTaiko.SaveFileInstances[player].data.SaveId, "unlocked_puchicharas", _npvKey);
			}
		}

		if (_edited)
			OpenTaiko.SaveFileInstances[player].tApplyHeyaChanges();
	}

	public CPuchichara(string path) {
		_path = path;

		// Puchichara textures
		tx = OpenTaiko.Tx.TxCAbsolute($@"{path}{Path.DirectorySeparatorChar}Chara.png");
		if (tx != null) {
			tx.vcScaleRatio = new Vector3D<float>(OpenTaiko.Skin.Game_PuchiChara_Scale[0]);
		}

		// Puchichara welcome sfx
		welcome = new CSkin.CSystemSound($@"{path}{Path.DirectorySeparatorChar}Welcome.ogg", false, false, true, ESoundGroup.Voice);

		metadata = ConfigManager.ReadOr(Path.Combine(path, "Metadata.json"), ConfigManager.GetConfig<DBPuchichara.PuchicharaData>, new DBPuchichara.PuchicharaData());
		effect = ConfigManager.ReadOr(Path.Combine(path, "Effects.json"), ConfigManager.GetConfig<DBPuchichara.PuchicharaEffect>, new DBPuchichara.PuchicharaEffect());
		unlock = ConfigManager.ReadOr<CUnlockCondition?>(Path.Combine(path, "Unlock.json"), p => OpenTaiko.UnlockConditionFactory.GenerateUnlockObjectFromJsonPath(p), null);
	}
}
