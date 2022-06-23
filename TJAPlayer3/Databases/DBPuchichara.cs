using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace TJAPlayer3
{
    class DBPuchichara
    {
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