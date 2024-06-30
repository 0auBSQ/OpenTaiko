using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;
using static TJAPlayer3.CActSelect曲リスト;
using Color = System.Drawing.Color;

namespace TJAPlayer3
{
    class CHeyaDisplayAssetInformations
    {
        private static TitleTextureKey? ttkDescription = null;

        private static String ToHex(System.Drawing.Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        private static int XOrigin 
        {
            get 
            {
                return TJAPlayer3.Skin.Heya_DescriptionTextOrigin[0];
            }
        }

        private static int YOrigin
        {
            get
            {
                return TJAPlayer3.Skin.Heya_DescriptionTextOrigin[1];
            }
        }

        public static void DisplayCharacterInfo(CCachedFontRenderer pf, CCharacter character)
        {
            string description = "";
            description += ("Name: " + character.metadata.tGetName() + "\n");
            description += ("Rarity: " + "<c." + ToHex(HRarity.tRarityToColor(character.metadata.Rarity)) + ">" + character.metadata.Rarity + "</c>" + "\n");
            if (character.metadata.tGetDescription() != "") description += character.metadata.tGetDescription() + "\n";
            description += ("Author: " + character.metadata.tGetAuthor() + "\n\n");

            var gaugeType = character.effect.Gauge;
            if (gaugeType == "Normal")
            {
                description += "Gauge Type: Normal\n";
                description += "Finish the play within the clear zone to pass the song!\n";
            }
            else if (gaugeType == "Hard")
            {
                description += "Gauge Type: <c.#ff4444>Hard</c>\n";
                description += "The gauge starts full and sharply depletes at each miss!\nBe careful, if the gauge value reachs 0, the play is automatically failed!\n";
            }
            else if (gaugeType == "Extreme")
            {
                description += "Gauge Type: <c.#360404>Extreme</c>\n";
                description += "The gauge starts full and sharply depletes at each miss!\nA strange power seems to reduce the margin of error progressively through the song...\n";
            }

            var bombFactor = character.effect.BombFactor;
            if (bombFactor < 10) description += $"Bomb Factor: {bombFactor}% of notes converted to mines in Minesweeper\n";
            else if (bombFactor < 25) description += $"Bomb Factor: <c.#b0b0b0>{bombFactor}</c>% of notes converted to mines in Minesweeper\n";
            else description += $"Bomb Factor: <c.#6b6b6b>{bombFactor}</c>% of notes converted to mines in Minesweeper\n";

            var fuseFactor = character.effect.FuseRollFactor;
            if (fuseFactor < 10) description += $"Fuse Factor: {fuseFactor}% of balloons converted to fuse rolls in Minesweeper\n";
            else if (fuseFactor < 25) description += $"Fuse Factor: <c.#b474c4>{fuseFactor}</c>% of balloons converted to fuse rolls in Minesweeper\n";
            else description += $"Fuse Factor: <c.#7c009c>{fuseFactor}</c>% of balloons converted to fuse rolls in Minesweeper\n";
            description += $"Coin multiplier: x{character.effect.GetCoinMultiplier()}";


            if (ttkDescription is null || ttkDescription.str文字 != description)
            {
                ttkDescription = new TitleTextureKey(description, pf, Color.White, Color.Black, 1000);
            }

            TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(ttkDescription).t2D描画(XOrigin, YOrigin);

        }

        public static void DisplayPuchicharaInfo(CCachedFontRenderer pf, CPuchichara puchi)
        {
            string description = "";
            description += ("Name: " + puchi.metadata.tGetName() + "\n");
            description += ("Rarity: " + "<c." + ToHex(HRarity.tRarityToColor(puchi.metadata.Rarity)) + ">" + puchi.metadata.Rarity + "</c>" + "\n");
            if (puchi.metadata.tGetDescription() != "") description += puchi.metadata.tGetDescription() + "\n";
            description += ("Author: " + puchi.metadata.tGetAuthor() + "\n\n");

            if (puchi.effect.AllPurple) description += "All big notes become <c.#c800ff>Swap</c> notes\n";
            if (puchi.effect.ShowAdlib) description += "<c.#c4ffe2>ADLib</c> notes become visible\n";
            if (puchi.effect.Autoroll > 0) description += $"Automatic <c.#ffff00>Rolls</c> at {puchi.effect.Autoroll} hits/s\n";
            if (puchi.effect.SplitLane) description += "<c.#ff4040>Split</c> <c.#4053ff>Lanes</c>\n";
            description += $"Coin multiplier: x{puchi.effect.GetCoinMultiplier()}";

            if (ttkDescription is null || ttkDescription.str文字 != description)
            {
                ttkDescription = new TitleTextureKey(description, pf, Color.White, Color.Black, 1000);
            }

            TJAPlayer3.stageSongSelect.actSongList.ResolveTitleTexture(ttkDescription).t2D描画(XOrigin, YOrigin);
        }

        public static void DisplayNameplateTitleInfo(CCachedFontRenderer pf)
        {

        }

        public static void DisplayDanplateInfo(CCachedFontRenderer pf)
        {

        }
    }
}
