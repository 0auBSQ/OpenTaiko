using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using static TJAPlayer3.DBNameplateUnlockables;
using static TJAPlayer3.DBSongUnlockables;

namespace TJAPlayer3
{
    internal class DBSongUnlockables : CSavableT<Dictionary<string, SongUnlockable>>
    {
        /* DISPLAYED : Song displayed in song select, only a lock appearing on the side, audio preview plays
         * GRAYED : Box grayed, song preview does not play
         * HIDDEN : Song not appears on the song select list until being unlocked
         */
        public enum EHiddenIndex
        {
            DISPLAYED = 0,
            GRAYED = 1,
            HIDDEN = 2
        }

        public DBSongUnlockables()
        {
            //_fn = @$"{TJAPlayer3.strEXEのあるフォルダ}Databases{Path.DirectorySeparatorChar}SongUnlockables.json";
            //base.tDBInitSavable();

            _fn = @$"{TJAPlayer3.strEXEのあるフォルダ}Databases{Path.DirectorySeparatorChar}SongUnlockables.db3";

            
            using (var connection = new SqliteConnection(@$"Data Source={_fn}"))
            {
                connection.Open();


                // Get songs info
                var command = connection.CreateCommand();
                command.CommandText =
                @$"
                    SELECT *
                    FROM songs;
                ";

                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    SongUnlockable su = new SongUnlockable();
                    su.hiddenIndex = (EHiddenIndex)(Int64)reader["HiddenIndex"];
                    su.rarity = (string)reader["Rarity"];
                    su.unlockConditions = new DBUnlockables.CUnlockConditions();
                    su.unlockConditions.Condition = (string)reader["UnlockCondition"];
                    su.unlockConditions.Values = JsonConvert.DeserializeObject<int[]>((string)reader["UnlockValues"]) ?? new int[] { 0 };
                    su.unlockConditions.Type = (string)reader["UnlockType"];
                    su.unlockConditions.Reference = JsonConvert.DeserializeObject<string[]>((string)reader["UnlockReferences"]) ?? new string[] { "" };
 
                    data[((string)reader["SongUniqueId"])] = su;
                }
                reader.Close();
            }
            
        }
        public class SongUnlockable
        {
            [JsonProperty("HiddenIndex")]
            public EHiddenIndex hiddenIndex;

            [JsonProperty("Rarity")]
            public string rarity;

            [JsonProperty("UnlockCondition")]
            public DBUnlockables.CUnlockConditions unlockConditions;
        }

        public void tGetUnlockedItems(int _player, ModalQueue mq)
        {
            int player = TJAPlayer3.GetActualPlayer(_player);
            var _sf = TJAPlayer3.SaveFileInstances[player].data.UnlockedSongs;
            bool _edited = false;

            foreach (KeyValuePair<string, SongUnlockable> item in data)
            {
                string _npvKey = item.Key;
                string? _songName = CSongDict.tGetNodeFromID(_npvKey)?.ldTitle.GetString("");
                string _songSubtitle = CSongDict.tGetNodeFromID(_npvKey)?.ldSubtitle.GetString("") ?? "";

                if (!_sf.Contains(_npvKey) && _songName != null)
                {
                    var _fulfilled = item.Value.unlockConditions.tConditionMetWrapper(player, DBUnlockables.CUnlockConditions.EScreen.Internal).Item1;

                    if (_fulfilled)
                    {
                        //_sf.Add(_npvKey, item.Value.nameplateInfo);
                        _sf.Add(_npvKey);
                        _edited = true;


                        mq.tAddModal(
                            new Modal(
                                Modal.EModalType.Song,
                                HRarity.tRarityToModalInt(item.Value.rarity),
                                _songName,
                                _songSubtitle
                                ),
                            _player);

                        DBSaves.RegisterStringUnlockedAsset(TJAPlayer3.SaveFileInstances[player].data.SaveId, "unlocked_songs", _npvKey);
                       
                    }
                }
            }

            if (_edited)
                TJAPlayer3.SaveFileInstances[player].tApplyHeyaChanges();
        }

        public bool tIsSongLocked(CSongListNode? song)
        {
            if (song == null) return false;
            return !TJAPlayer3.SaveFileInstances[TJAPlayer3.SaveFile].data.UnlockedSongs.Contains(song.tGetUniqueId())
                && data.ContainsKey(song.tGetUniqueId());
        }

        public EHiddenIndex tGetSongHiddenIndex(CSongListNode? song)
        {
            if (song == null || !tIsSongLocked(song)) return EHiddenIndex.DISPLAYED;
            return data[song.tGetUniqueId()].hiddenIndex;
        }

        public SongUnlockable? tGetUnlockableByUniqueId(CSongListNode? song)
        {
            if (song == null) return null;
            if (!data.ContainsKey(song.tGetUniqueId())) return null;
            return data[song.tGetUniqueId()];
        }
    }
}
