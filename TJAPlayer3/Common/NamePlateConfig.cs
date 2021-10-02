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
                ConfigManager.SaveConfig(data, "NamePlate.json");

            data = ConfigManager.GetConfig<Data>(@"NamePlate.json");
        }

        public class Data
        {
            public string[] Name = { "どんちゃん", "かっちゃん" };
            public string[] Title = { "どんちゃんですよ！", "かっちゃんですよ！" };
            public string[] Dan = { "達人", "達人" };

            public bool[] DanGold = { false, false };

            public int[] DanType = { 1, 2 };
            public int[] TitleType = { 1, 2 };

            public int[] PuchiChara = { 2, 11 };
        }

        public Data data = new Data();
    }
}
