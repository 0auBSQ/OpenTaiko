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
        public CTexture render;
        public CSkin.Cシステムサウンド welcome;
        public DBPuchichara.PuchicharaData metadata;
        public DBPuchichara.PuchicharaEffect effect;
        public DBUnlockables.CUnlockConditions unlock;
        public string _path;

        public float GetEffectCoinMultiplier()
        {
            float mult = 1f;

            mult *= HRarity.tRarityToRarityToCoinMultiplier(metadata.Rarity);
            mult *= effect.GetCoinMultiplier();

            return mult;
        }

        public CPuchichara(string path)
        {
            _path = path;

            // Puchichara textures
            tx = TJAPlayer3.Tx.TxCAbsolute($@"{path}\Chara.png");
            if (tx != null)
            {
                tx.vc拡大縮小倍率 = new SharpDX.Vector3(TJAPlayer3.Skin.Game_PuchiChara_Scale[0]);
            }

            // Heya render
            render = TJAPlayer3.Tx.TxCAbsolute($@"{path}\Render.png");

            // Puchichara welcome sfx
            welcome = new CSkin.Cシステムサウンド($@"{path}\Welcome.ogg", false, false, true, ESoundGroup.Voice);

            // Puchichara metadata
            if (File.Exists($@"{path}\Metadata.json"))
                metadata = ConfigManager.GetConfig<DBPuchichara.PuchicharaData>($@"{path}\Metadata.json");
            else
                metadata = new DBPuchichara.PuchicharaData();

            // Puchichara metadata
            if (File.Exists($@"{path}\Effects.json"))
                effect = ConfigManager.GetConfig<DBPuchichara.PuchicharaEffect>($@"{path}\Effects.json");
            else
                effect = new DBPuchichara.PuchicharaEffect();

            // Puchichara unlockables
            if (File.Exists($@"{path}\Unlock.json"))
                unlock = ConfigManager.GetConfig<DBUnlockables.CUnlockConditions>($@"{path}\Unlock.json");
            else
                unlock = null;
        }
    }
}
