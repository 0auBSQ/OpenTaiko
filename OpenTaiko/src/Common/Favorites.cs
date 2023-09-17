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

        #region [Auxiliary methods]

        public void tToggleFavorite(string chartID)
        {
            if (tIsFavorite(chartID))
                data.favorites[TJAPlayer3.SaveFile].Remove(chartID);
            else
                data.favorites[TJAPlayer3.SaveFile].Add(chartID);

            tSaveFile();
        }

        public bool tIsFavorite(string chartID)
        {
            return (data.favorites[TJAPlayer3.SaveFile].Contains(chartID));
        }
        

        #endregion

        public class Data
        {
            public HashSet<string>[] favorites = new HashSet<string>[2] { new HashSet<string>(), new HashSet<string>() };
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
