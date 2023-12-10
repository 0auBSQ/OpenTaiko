using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace TJAPlayer3
{
    class DBCharacter
    {
        public class CharacterEffect
        {
            public CharacterEffect()
            {
                Gauge = "Normal";
                BombFactor = 20;
                FuseRollFactor = 0;
            }

            public float GetCoinMultiplier()
            {
                float mult = 1f;

                if (Gauge == "Hard") mult *= 1.5f;
                if (Gauge == "Extreme") mult *= 1.8f;

                return mult;
            }

            public string tGetGaugeType()
            {
                return TJAPlayer3.ConfigIni.bForceNormalGauge || TJAPlayer3.stage選曲.n確定された曲の難易度[0] >= 5 ? "Normal" : Gauge;
            }


            [JsonProperty("gauge")]
            public string Gauge;

            [JsonProperty("bombFactor")]
            public int BombFactor;

            [JsonProperty("fuseRollFactor")]
            public int FuseRollFactor;
        }

        public class CharacterData
        {
            public CharacterData()
            {
                Name = "(None)";
                Rarity = "Common";
                Author = "(None)";
                SpeechText = new CLocalizationData[6] { new CLocalizationData(), new CLocalizationData(), new CLocalizationData(), new CLocalizationData(), new CLocalizationData(), new CLocalizationData() };
            }

            public CharacterData(string pcn, string pcr, string pca, CLocalizationData[] pcst)
            {
                Name = pcn;
                Rarity = pcr;
                Author = pca;
                SpeechText = pcst;
            }


            [JsonProperty("name")]
            public string Name;

            [JsonProperty("rarity")]
            public string Rarity;

            [JsonProperty("author")]
            public string Author;

            [JsonProperty("speechtext")]
            public CLocalizationData[] SpeechText;
        }

    }
}