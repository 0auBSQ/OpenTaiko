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

            DBPuchichara.tDBPuchichara();
            DBUnlockables.tDBUnlockables();
        }

        public DBPuchichara DBPuchichara;
        public DBUnlockables DBUnlockables;
    }
}