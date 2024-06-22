using System.Collections.Generic;
using Newtonsoft.Json;
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
            _fn = @$"{TJAPlayer3.strEXEのあるフォルダ}Databases{Path.DirectorySeparatorChar}SongUnlockables.json";
            base.tDBInitSavable();
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
            var _sf = TJAPlayer3.SaveFileInstances[player].data.NamePlateTitles; // Placeholder
            bool _edited = false;

            foreach (KeyValuePair<string, SongUnlockable> item in data)
            {
                var _npvKey = item.Key;
                if (!_sf.ContainsKey(_npvKey))
                {
                    var _fulfilled = item.Value.unlockConditions.tConditionMetWrapper(player, DBUnlockables.CUnlockConditions.EScreen.Internal).Item1;

                    if (_fulfilled)
                    {
                        /*
                        _sf.Add(_npvKey, item.Value.nameplateInfo);
                        _edited = true;
                        mq.tAddModal(
                            new Modal(
                                Modal.EModalType.Title, 
                                HRarity.tRarityToModalInt(item.Value.rarity), 
                                item.Value.nameplateInfo.cld.GetString(item.Key)
                                ), 
                            _player);
                        */
                    } 
                }
            }

            if (_edited)
                TJAPlayer3.SaveFileInstances[player].tApplyHeyaChanges();
        }
    }
}
