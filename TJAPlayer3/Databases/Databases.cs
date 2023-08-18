using System.Collections.Generic;
using System.IO;

namespace TJAPlayer3
{
    class Databases
    {
        public void tDatabases()
        {
            DBCDN = new DBCDN();
            DBEncyclopediaMenus = new DBEncyclopediaMenus();
            DBNameplateUnlockables = new DBNameplateUnlockables();
            DBSongUnlockables = new DBSongUnlockables();
        }

        public DBCDN DBCDN;
        public DBEncyclopediaMenus DBEncyclopediaMenus;
        public DBNameplateUnlockables DBNameplateUnlockables;
        public DBSongUnlockables DBSongUnlockables;
    }
}