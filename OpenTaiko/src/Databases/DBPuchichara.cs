using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace TJAPlayer3
{
    class DBPuchichara
    {
        public class PuchicharaEffect
        {
            public PuchicharaEffect()
            {
                AllPurple = false;
                Autoroll = 0;
                ShowAdlib = false;
                SplitLane = false;
            }

            public float GetCoinMultiplier()
            {
                float mult = 1f;

                if (Autoroll > 0) mult *= 0f;
                if (ShowAdlib == true) mult *= 0.9f;
                //if (AllPurple == true) mult *= 1.1f;

                return mult;
            }

            [JsonProperty("allpurple")]
            public bool AllPurple;

            [JsonProperty("AutoRoll")]
            public int Autoroll;

            [JsonProperty("showadlib")]
            public bool ShowAdlib;

            [JsonProperty("splitlane")]
            public bool SplitLane;
        }

        public class PuchicharaData
        {
            public PuchicharaData()
            {
                Name = "(None)";
                Rarity = "Common";
                Author = "(None)";
            }

            public PuchicharaData(string pcn, string pcr, string pca)
            {
                Name = pcn;
                Rarity = pcr;
                Author = pca;
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
        }

    }
}