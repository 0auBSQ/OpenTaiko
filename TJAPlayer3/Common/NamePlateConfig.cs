using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace TJAPlayer3
{
    class NamePlateConfig
    {
        public void tNamePlateConfig()
        {
            if (!File.Exists("NamePlate.json"))
                tSaveFile();

            tLoadFile();
        }

        #region [Medals]

        public void tEarnCoins(int[] amounts)
        {
            if (amounts.Length < 2)
                return;

            for (int i = 0; i < 2; i++)
            {
                int p = TJAPlayer3.GetActualPlayer(i);

                data.Medals[p] += amounts[i];
            }
            tSaveFile();
        }

        // Return false if the current amount of coins is to low
        public bool tSpendCoins(int amount, int player)
        {
            if (player > 1 || player < 0)
                return false;

            if (data.Medals[player] < amount)
                return false;

            data.Medals[player] -= amount;

            tSaveFile();

            return true;
        }

        #endregion

        #region [Dan titles]

        public bool tUpdateDanTitle(string title, bool isGold, int clearStatus, int player)
        {
            bool changed = false;

            bool iG = isGold;
            int cs = clearStatus;

            if (TJAPlayer3.NamePlateConfig.data.DanTitles[player] == null)
                TJAPlayer3.NamePlateConfig.data.DanTitles[player] = new Dictionary<string, CDanTitle>();

            if (TJAPlayer3.NamePlateConfig.data.DanTitles[player].ContainsKey(title))
            {
                if (TJAPlayer3.NamePlateConfig.data.DanTitles[player][title].clearStatus > cs)
                    cs = TJAPlayer3.NamePlateConfig.data.DanTitles[player][title].clearStatus;
                if (TJAPlayer3.NamePlateConfig.data.DanTitles[player][title].isGold)
                    iG = true;
            }

            // Automatically set the dan to nameplate if new
            // Add a function within the NamePlate.cs file to update the title texture 

            if (!TJAPlayer3.NamePlateConfig.data.DanTitles[player].ContainsKey(title) || cs != clearStatus || iG != isGold)
            {
                changed = true;
                /*
                TJAPlayer3.NamePlateConfig.data.Dan[player] = title;
                TJAPlayer3.NamePlateConfig.data.DanGold[player] = iG;
                TJAPlayer3.NamePlateConfig.data.DanType[player] = cs;
                */
            }


            CDanTitle danTitle = new CDanTitle(iG, cs);

            TJAPlayer3.NamePlateConfig.data.DanTitles[player][title] = danTitle;

            tSaveFile();

            return changed;
        }

        #endregion

        #region [Auxilliary classes]

        public class CDanTitle
        {
            public CDanTitle(bool iG, int cs)
            {
                isGold = iG;
                clearStatus = cs;
            }

            [JsonProperty("isGold")]
            public bool isGold;

            [JsonProperty("clearStatus")]
            public int clearStatus;
        }

        public class CNamePlateTitle
        {
            public CNamePlateTitle(int type)
            {
                iType = type;
            }

            [JsonProperty("iType")]
            public int iType;
        }

        #endregion

        #region [Heya]

        public void tApplyHeyaChanges()
        {
            this.tSaveFile();
        }

        #endregion

        public class Data
        {
            [JsonProperty("name")]
            public string[] Name = { "プレイヤー1", "プレイヤー2" };

            [JsonProperty("title")]
            public string[] Title = { "初心者", "初心者" };

            [JsonProperty("dan")]
            public string[] Dan = { "新人", "新人" };

            [JsonProperty("danGold")]
            public bool[] DanGold = { false, false };

            [JsonProperty("danType")]
            public int[] DanType = { 0, 0 };

            [JsonProperty("titleType")]
            public int[] TitleType = { 0, 0 };

            [JsonProperty("puchiChara")]
            public int[] PuchiChara = { 0, 0 };

            [JsonProperty("medals")]
            public int[] Medals = { 0, 0 };

            [JsonProperty("character")]
            public int[] Character = { 0, 0 };

            [JsonProperty("danTitles")]
            public Dictionary<string, CDanTitle>[] DanTitles = new Dictionary<string, CDanTitle>[2];

            [JsonProperty("namePlateTitles")]
            public Dictionary<string, CNamePlateTitle>[] NamePlateTitles = new Dictionary<string, CNamePlateTitle>[2];

            [JsonProperty("unlockedPuchicharas")]
            public List<string>[] UnlockedPuchicharas = new List<string>[2]
            {
                new List<string>(),
                new List<string>()
            };

        }

        public Data data = new Data();

        #region [private]

        private void tSaveFile()
        {
            ConfigManager.SaveConfig(data, "NamePlate.json");
        }

        private void tLoadFile()
        {
            data = ConfigManager.GetConfig<Data>(@"NamePlate.json");
        }

        #endregion
    }
}
