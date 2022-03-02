using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TJAPlayer3
{
    internal class Favorites
    {
        public void tFavorites() {
            if (!File.Exists("Favorite.json"))
                tSaveFile();

            tLoadFile();
        }

        public class Data
        {

        }

        public Data data = new Data();

        #region [private]

        private void tSaveFile()
        {
            ConfigManager.SaveConfig(data, "Favorite.json");
        }

        private void tLoadFile()
        {
            data = ConfigManager.GetConfig<Data>(@"Favorite.json");
        }

        #endregion
    }

}
