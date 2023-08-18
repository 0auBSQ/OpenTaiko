using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = System.Drawing.Color;

namespace TJAPlayer3
{
    internal class HRarity
    {
        private static Dictionary<string, Color> RarityToColor = new Dictionary<string, Color>
        {
            ["Poor"] = Color.Gray,
            ["Common"] = Color.White,
            ["Uncommon"] = Color.Lime,
            ["Rare"] = Color.Blue,
            ["Epic"] = Color.Purple,
            ["Legendary"] = Color.Orange,
        };

        private static Dictionary<string, int> RarityToModalInt = new Dictionary<string, int>
        {
            ["Poor"] = 0,
            ["Common"] = 0,
            ["Uncommon"] = 1,
            ["Rare"] = 2,
            ["Epic"] = 3,
            ["Legendary"] = 4,
        };

        private static Dictionary<string, float> RarityToCoinMultiplier = new Dictionary<string, float>
        {
            ["Poor"] = 0.8f,
            ["Common"] = 1f,
            ["Uncommon"] = 1.1f,
            ["Rare"] = 1.2f,
            ["Epic"] = 1.3f,
            ["Legendary"] = 1.5f,
        };

        public static Color tRarityToColor(string rarity)
        {

            Color textColor = Color.White;

            if (RarityToColor.ContainsKey(rarity))
                textColor = RarityToColor[rarity];

            return textColor;

        }

        public static int tRarityToModalInt(string rarity)
        {
            int modalInt = 0;

            if (RarityToModalInt.ContainsKey(rarity))
                modalInt = RarityToModalInt[rarity];

            return modalInt;
        }

        public static float tRarityToRarityToCoinMultiplier(string rarity)
        {
            float coinMult = 1f;

            if (RarityToCoinMultiplier.ContainsKey(rarity))
                coinMult = RarityToCoinMultiplier[rarity];

            return coinMult;
        }


    }
}
