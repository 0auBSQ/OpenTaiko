using System.Collections.Generic;
using System.IO;

namespace TJAPlayer3
{
    class Databases
    {
        public void tDatabases()
        {
            //DBPuchichara = new DBPuchichara();
            DBUnlockables = new DBUnlockables();
            //DBCharacter = new DBCharacter();
            DBCDN = new DBCDN();

            //DBPuchichara.tDBPuchichara();
            DBUnlockables.tDBUnlockables();
            //DBCharacter.tDBCharacter();
            DBCDN.tDBCDN();
        }

        //public DBPuchichara DBPuchichara;
        public DBUnlockables DBUnlockables;
        //public DBCharacter DBCharacter;
        public DBCDN DBCDN;
    }
}