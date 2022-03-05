﻿using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace TJAPlayer3
{
    class DBPuchichara
    {

        public void tDBPuchichara()
        {
            if (!File.Exists(@".\Databases\Puchichara.json"))
                tSaveFile();

            tLoadFile();
        }

        #region [Auxiliary classes]

        public class PuchicharaData
        {
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

        #endregion

        public Dictionary<int, PuchicharaData> data = new Dictionary<int, PuchicharaData>();

        #region [private]

        private void tSaveFile()
        {
            ConfigManager.SaveConfig(data, @".\Databases\Puchichara.json");
        }

        private void tLoadFile()
        {
            data = ConfigManager.GetConfig<Dictionary<int, PuchicharaData>>(@".\Databases\Puchichara.json");
        }

        #endregion
    }
}