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

                if (Gauge == "Hard" && !TJAPlayer3.ConfigIni.bForceNormalGauge) mult *= 1.5f;
                if (Gauge == "Extreme" && !TJAPlayer3.ConfigIni.bForceNormalGauge) mult *= 1.8f;

                return mult;
            }

            public string tGetGaugeType()
            {
                return TJAPlayer3.ConfigIni.bForceNormalGauge || TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] >= 5 ? "Normal" : Gauge;
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

            public string tGetName()
            {
                if (Name is string) return (string)Name;
                else if (Name is CLocalizationData) return ((CLocalizationData)Name).GetString("");
                return "";
            }

            public string tGetAuthor()
            {
                if (Author is string) return (string)Author;
                else if (Author is CLocalizationData) return ((CLocalizationData)Author).GetString("");
                return "";
            }

            public string tGetDescription()
            {
                if (Description is string) return (string)Description;
                else if (Description is CLocalizationData) return ((CLocalizationData)Description).GetString("");
                return "";
            }

            // String or CLocalizationData
            [JsonProperty("name")]
            [JsonConverter(typeof(LocalizedStringConverter<CLocalizationData>))]
            public object Name;

            [JsonProperty("rarity")]
            public string Rarity;

            // String or CLocalizationData
            [JsonProperty("author")]
            [JsonConverter(typeof(LocalizedStringConverter<CLocalizationData>))]
            public object Author;

            // String or CLocalizationData
            [JsonProperty("description")]
            [JsonConverter(typeof(LocalizedStringConverter<CLocalizationData>))]
            public object Description;

            [JsonProperty("speechtext")]
            public CLocalizationData[] SpeechText;
        }

    }
}