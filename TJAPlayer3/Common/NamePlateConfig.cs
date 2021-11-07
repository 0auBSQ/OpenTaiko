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

        public class Data
        {
            public string[] Name = { "プレイヤー1", "プレイヤー2" };
            public string[] Title = { "初心者", "初心者" };
            public string[] Dan = { "素人", "素人" };

            public bool[] DanGold = { false, false };

            public int[] DanType = { 0, 0 };
            public int[] TitleType = { 1, 1 };

            public int[] PuchiChara = { 2, 11 };

            public int[] Medals = { 0, 0 };

            public int[] Character = { 0, 0 };
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
