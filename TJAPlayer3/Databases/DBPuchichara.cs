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
                if (AllPurple == true) mult *= 1.1f;

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


            [JsonProperty("name")]
            public string Name;

            [JsonProperty("rarity")]
            public string Rarity;

            [JsonProperty("author")]
            public string Author;
        }

    }
}