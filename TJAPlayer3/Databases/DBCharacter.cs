using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace TJAPlayer3
{
    class DBCharacter
    {
        public class CharacterData
        {
            public CharacterData()
            {
                Name = "(None)";
                Rarity = "Common";
                Author = "(None)";
            }

            public CharacterData(string pcn, string pcr, string pca)
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