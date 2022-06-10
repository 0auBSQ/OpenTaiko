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
        public DBUnlockables.CUnlockConditions unlock;
        public string _path;

        public CCharacter(string path)
        {
            _path = path;

            // Character metadata
            if (File.Exists($@"{path}\Metadata.json"))
                metadata = ConfigManager.GetConfig<DBCharacter.CharacterData>($@"{path}\Metadata.json");
            else
                metadata = new DBCharacter.CharacterData();

            // Character unlockables
            if (File.Exists($@"{path}\Unlock.json"))
                unlock = ConfigManager.GetConfig<DBUnlockables.CUnlockConditions>($@"{path}\Unlock.json");
            else
                unlock = null;
        }
    }
}
