using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace TJAPlayer3
{
    class DBCharacter
    {

        public void tDBCharacter()
        {
            if (!File.Exists(@".\Databases\Character.json"))
                tSaveFile();

            tLoadFile();
        }

        #region [Auxiliary classes]

        public class CharacterData
        {
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

        #endregion

        public Dictionary<int, CharacterData> data = new Dictionary<int, CharacterData>();

        #region [private]

        private void tSaveFile()
        {
            ConfigManager.SaveConfig(data, @".\Databases\Character.json");
        }

        private void tLoadFile()
        {
            data = ConfigManager.GetConfig<Dictionary<int, CharacterData>>(@".\Databases\Character.json");
        }

        #endregion
    }
}