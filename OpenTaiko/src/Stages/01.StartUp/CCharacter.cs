using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;
using System.IO;

namespace TJAPlayer3
{
    class CCharacter
    {
        public DBCharacter.CharacterData metadata;
        public DBCharacter.CharacterEffect effect;
        public DBUnlockables.CUnlockConditions unlock;
        public string _path;

        public float GetEffectCoinMultiplier()
        {
            float mult = 1f;

            mult *= HRarity.tRarityToRarityToCoinMultiplier(metadata.Rarity);
            mult *= effect.GetCoinMultiplier();

            return mult;
        }

        public CCharacter(string path)
        {
            _path = path;

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
