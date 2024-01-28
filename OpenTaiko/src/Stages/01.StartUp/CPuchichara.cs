using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Silk.NET.Maths;
using FDK;


namespace TJAPlayer3
{
    class CPuchichara
    {
        public CTexture tx;
        public CTexture render;
        public CSkin.CSystemSound welcome;
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
            tx = TJAPlayer3.Tx.TxCAbsolute($@"{path}{Path.DirectorySeparatorChar}Chara.png");
            if (tx != null)
            {
                tx.vcScaleRatio = new Vector3D<float>(TJAPlayer3.Skin.Game_PuchiChara_Scale[0]);
            }

            // Heya render
            render = TJAPlayer3.Tx.TxCAbsolute($@"{path}{Path.DirectorySeparatorChar}Render.png");

            // Puchichara welcome sfx
            welcome = new CSkin.CSystemSound($@"{path}{Path.DirectorySeparatorChar}Welcome.ogg", false, false, true, ESoundGroup.Voice);

            // Puchichara metadata
            if (File.Exists($@"{path}{Path.DirectorySeparatorChar}Metadata.json"))
                metadata = ConfigManager.GetConfig<DBPuchichara.PuchicharaData>($@"{path}{Path.DirectorySeparatorChar}Metadata.json");
            else
                metadata = new DBPuchichara.PuchicharaData();

            // Puchichara metadata
            if (File.Exists($@"{path}{Path.DirectorySeparatorChar}Effects.json"))
                effect = ConfigManager.GetConfig<DBPuchichara.PuchicharaEffect>($@"{path}{Path.DirectorySeparatorChar}Effects.json");
            else
                effect = new DBPuchichara.PuchicharaEffect();

            // Puchichara unlockables
            if (File.Exists($@"{path}{Path.DirectorySeparatorChar}Unlock.json"))
                unlock = ConfigManager.GetConfig<DBUnlockables.CUnlockConditions>($@"{path}{Path.DirectorySeparatorChar}Unlock.json");
            else
                unlock = null;
        }
    }
}
