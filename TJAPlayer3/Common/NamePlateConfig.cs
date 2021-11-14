using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
                data.Medals[i] += amounts[i];

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

        public void tUpdateDanTitle(string title, bool isGold, int clearStatus, int player)
        {
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
            /*
            if (!TJAPlayer3.NamePlateConfig.data.DanTitles[player].ContainsKey(title) || cs != clearStatus || iG != isGold)
            {
                TJAPlayer3.NamePlateConfig.data.Dan[player] = title;
                TJAPlayer3.NamePlateConfig.data.DanGold[player] = iG;
                TJAPlayer3.NamePlateConfig.data.DanType[player] = cs;
            }
            */

            CDanTitle danTitle = new CDanTitle(iG, cs);

            TJAPlayer3.NamePlateConfig.data.DanTitles[player][title] = danTitle;

            tSaveFile();
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

            public bool isGold;
            public int clearStatus;
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
            public string[] Name = { "プレイヤー1", "プレイヤー2" };
            public string[] Title = { "初心者", "初心者" };
            public string[] Dan = { "新人", "新人" };

            public bool[] DanGold = { false, false };

            public int[] DanType = { 0, 0 };
            public int[] TitleType = { 1, 1 };

            public int[] PuchiChara = { 2, 11 };

            public int[] Medals = { 0, 0 };

            public int[] Character = { 0, 0 };

            public Dictionary<string, CDanTitle>[] DanTitles = new Dictionary<string, CDanTitle>[2];
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
