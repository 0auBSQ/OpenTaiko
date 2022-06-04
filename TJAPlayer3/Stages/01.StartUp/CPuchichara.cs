using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FDK;


namespace TJAPlayer3
{
    class CPuchichara
    {
        public CTexture tx;
        public CSkin.Cシステムサウンド welcome;
        public DBPuchichara.PuchicharaData metadata;
        public DBUnlockables.CUnlockConditions unlock;
        public string _path;

        public CPuchichara(string path)
        {
            _path = path;

            // Puchichara textures
            tx = TJAPlayer3.Tx.TxCAbsolute($@"{path}\Chara.png");
            if (tx != null)
            {
                tx.vc拡大縮小倍率 = new SharpDX.Vector3(TJAPlayer3.Skin.Game_PuchiChara_Scale[0]);
            }

            // Puchichara welcome sfx
            welcome = new CSkin.Cシステムサウンド($@"{path}\Welcome.ogg", false, false, true, ESoundGroup.Voice);

            // Puchichara metadata
            if (File.Exists($@"{path}\Metadata.json"))
                metadata = ConfigManager.GetConfig<DBPuchichara.PuchicharaData>($@"{path}\Metadata.json");
            else
                metadata = new DBPuchichara.PuchicharaData();

            // Puchichara unlockables
            if (File.Exists($@"{path}\Unlock.json"))
                unlock = ConfigManager.GetConfig<DBUnlockables.CUnlockConditions>($@"{path}\Unlock.json");
            else
                unlock = null;
        }
    }
}
