using System.Collections.Generic;
using System.IO;

namespace TJAPlayer3
{
    class Databases
    {
        public void tDatabases()
        {
            DBPuchichara = new DBPuchichara();
            DBUnlockables = new DBUnlockables();
            DBCharacter = new DBCharacter();

            DBPuchichara.tDBPuchichara();
            DBUnlockables.tDBUnlockables();
            DBCharacter.tDBCharacter();
        }

        public DBPuchichara DBPuchichara;
        public DBUnlockables DBUnlockables;
        public DBCharacter DBCharacter;
    }
}