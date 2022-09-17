using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TJAPlayer3
{
    internal class RecentlyPlayedSongs
    {
        public void tRecentlyPlayedSongs() {
            if (!File.Exists("RecentlyPlayedSongs.json"))
                tSaveFile();

            tLoadFile();
        }

        #region [Auxiliary methods]

        public void tAddChart(string chartID)
        {
            if (!data.recentlyplayedsongs[TJAPlayer3.SaveFile].Contains(chartID))
                data.recentlyplayedsongs[TJAPlayer3.SaveFile].Enqueue(chartID);

            while (data.recentlyplayedsongs[TJAPlayer3.SaveFile].Count > TJAPlayer3.ConfigIni.nRecentlyPlayedMax)
                data.recentlyplayedsongs[TJAPlayer3.SaveFile].Dequeue();

            tSaveFile();
        }

        #endregion

        public class Data
        {
            public Queue<string>[] recentlyplayedsongs = new Queue<string>[2] { new Queue<string>(), new Queue<string>() };
        }

        public Data data = new Data();

        #region [private]

        private void tSaveFile()
        {
            ConfigManager.SaveConfig(data, "RecentlyPlayedSongs.json");
        }

        private void tLoadFile()
        {
            data = ConfigManager.GetConfig<Data>(@"RecentlyPlayedSongs.json");
        }

        #endregion
    }

}
