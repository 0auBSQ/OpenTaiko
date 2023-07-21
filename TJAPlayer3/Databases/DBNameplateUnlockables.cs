using System.Collections.Generic;
using Newtonsoft.Json;
using static TJAPlayer3.DBNameplateUnlockables;

namespace TJAPlayer3
{
    internal class DBNameplateUnlockables : CSavableT<Dictionary<string, NameplateUnlockable>>
    {
        public DBNameplateUnlockables()
        {
            _fn = @".\Databases\NameplateUnlockables.json";
            base.tDBInitSavable();
        }
        public class NameplateUnlockable
        {
            [JsonProperty("NameplateInfo")]
            public SaveFile.CNamePlateTitle nameplateInfo;

            [JsonProperty("Rarity")]
            public string rarity;

            [JsonProperty("UnlockCondition")]
            public DBUnlockables.CUnlockConditions unlockConditions;
        }

        public void tGetUnlockedItems(int _player, ModalQueue mq)
        {
            int player = TJAPlayer3.GetActualPlayer(_player);
            var _sf = TJAPlayer3.SaveFileInstances[player].data.NamePlateTitles;
            bool _edited = false;

            foreach (KeyValuePair<string, NameplateUnlockable> item in data)
            {
                var _npvKey = item.Key;
                if (!_sf.ContainsKey(_npvKey))
                {
                    var _fulfilled = item.Value.unlockConditions.tConditionMetWrapper(player, DBUnlockables.CUnlockConditions.EScreen.Internal).Item1;

                    if (_fulfilled)
                    {
                        _sf.Add(_npvKey, item.Value.nameplateInfo);
                        _edited = true;
                        mq.tAddModal(
                            new Modal(
                                Modal.EModalType.Title, 
                                HRarity.tRarityToModalInt(item.Value.rarity), 
                                item.Value.nameplateInfo.cld.GetString(item.Key)
                                ), 
                            _player);
                    } 
                }
            }

            if (_edited)
                TJAPlayer3.SaveFileInstances[player].tApplyHeyaChanges();
        }
    }
}
