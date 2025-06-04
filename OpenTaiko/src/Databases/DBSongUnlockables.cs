using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using static OpenTaiko.DBSongUnlockables;

namespace OpenTaiko;

internal class DBSongUnlockables : CSavableT<Dictionary<string, SongUnlockable>> {
	/* DISPLAYED : Song displayed in song select, only a lock appearing on the side, audio preview plays
	 * GRAYED : Box grayed, song preview does not play
	 * BLURED : Like grayed, but with a glitch effect on the song title and preimage making it unreadable
	 * HIDDEN : Song not appears on the song select list until being unlocked
	 */
	public enum EHiddenIndex {
		DISPLAYED = 0,
		GRAYED = 1,
		BLURED = 2,
		HIDDEN = 3
	}

	public DBSongUnlockables() {
		_fn = @$"{OpenTaiko.strEXEのあるフォルダ}Databases{Path.DirectorySeparatorChar}SongUnlockables.db3";
		using (var connection = new SqliteConnection(@$"Data Source={_fn}")) {
			connection.Open();

			// Get songs info
			var command = connection.CreateCommand();
			command.CommandText =
				@$"
                    SELECT *
                    FROM songs;
                ";

			var reader = command.ExecuteReader();
			while (reader.Read()) {
				SongUnlockable su = new SongUnlockable();
				su.hiddenIndex = (EHiddenIndex)(Int64)reader["HiddenIndex"];
				su.rarity = (string)reader["Rarity"];

				#region [ Build raw json ]

				CUnlockConditionFactory.UnlockConditionJsonRaw _raw = new CUnlockConditionFactory.UnlockConditionJsonRaw(
					(string)reader["UnlockCondition"],
					JsonConvert.DeserializeObject<int[]>((string)reader["UnlockValues"]) ?? new int[] { 0 },
					(string)reader["UnlockType"],
					JsonConvert.DeserializeObject<string[]>((string)reader["UnlockReferences"]) ?? new string[] { "" }
					);

				su.unlockConditions = OpenTaiko.UnlockConditionFactory.GenerateUnlockObjectFromJsonRaw(_raw);

				#endregion

				su.customUnlockText = JsonConvert.DeserializeObject<CLocalizationData>((string)reader["CustomUnlockText"]) ?? new CLocalizationData();

				data[((string)reader["SongUniqueId"])] = su;
			}
			reader.Close();
		}

	}
	public class SongUnlockable {
		[JsonProperty("HiddenIndex")]
		public EHiddenIndex hiddenIndex;

		[JsonProperty("Rarity")]
		public string rarity;

		[JsonProperty("UnlockCondition")]
		public CUnlockCondition unlockConditions;

		[JsonProperty("CustomUnlockText")]
		public CLocalizationData customUnlockText;

		public string GetUnlockMessage() {
			return customUnlockText.GetString(unlockConditions.tConditionMessage(CUnlockCondition.EScreen.SongSelect));
		}
	}

	public void tGetUnlockedItems(int _player, ModalQueue mq) {
		int player = OpenTaiko.GetActualPlayer(_player);
		var _sf = OpenTaiko.SaveFileInstances[player].data.UnlockedSongs;
		bool _edited = false;

		foreach (KeyValuePair<string, SongUnlockable> item in data) {
			string _npvKey = item.Key;
			CSongListNode? _node = CSongDict.tGetNodeFromID(_npvKey);

			if (!_sf.Contains(_npvKey)) {
				var _fulfilled = item.Value.unlockConditions.tConditionMet(player, CUnlockCondition.EScreen.Internal).Item1;

				if (_fulfilled) {
					_sf.Add(_npvKey);
					_edited = true;


					mq.tAddModal(
						new Modal(
							Modal.EModalType.Song,
							HRarity.tRarityToModalInt(item.Value.rarity),
							_node,
							OpenTaiko.stageSongSelect.actPreimageパネル.tGenerateAndGetPreimage
						),
						_player);

					DBSaves.RegisterStringUnlockedAsset(OpenTaiko.SaveFileInstances[player].data.SaveId, "unlocked_songs", _npvKey);

				}
			}
		}

		if (_edited)
			OpenTaiko.SaveFileInstances[player].tApplyHeyaChanges();
	}

	public bool tIsSongLocked(CSongListNode? song) {
		if (song == null || OpenTaiko.ConfigIni.bIgnoreSongUnlockables) return false;
		return !OpenTaiko.SaveFileInstances[OpenTaiko.SaveFile].data.UnlockedSongs.Contains(song.tGetUniqueId())
			   && data.ContainsKey(song.tGetUniqueId());
	}

	public EHiddenIndex tGetSongHiddenIndex(CSongListNode? song) {
		if (song == null || !tIsSongLocked(song)) return EHiddenIndex.DISPLAYED;
		return data[song.tGetUniqueId()].hiddenIndex;
	}

	public SongUnlockable? tGetUnlockableByUniqueId(CSongListNode? song) {
		if (song == null) return null;
		if (!data.ContainsKey(song.tGetUniqueId())) return null;
		return data[song.tGetUniqueId()];
	}
}
