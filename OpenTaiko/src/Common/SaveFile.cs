using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TJAPlayer3
{
    internal class SaveFile
    {

        public void tSaveFile(string filename)
        {
            path = @$"Saves{Path.DirectorySeparatorChar}" + filename + @".json";
            name = filename;

            if (!File.Exists(path))
            {
                this.data.Name = filename;
                tSaveFile();
            }
                
            tLoadFile();
        }

        #region [Medals]

        public void tEarnCoins(int amount)
        {
            data.Medals += amount;
            data.TotalEarnedMedals += amount;

            // Small trick here, each actual play (excluding Auto, AI, etc) are worth at least 5 coins for the player, whatever which mode it is (Dan, Tower, Taiko mode, etc)
            // Earn Coins is also called once per play, so we just add 1 here to the total playcount
            data.TotalPlaycount += 1;
            tSaveFile();
        }

        // Return false if the current amount of coins is to low
        public bool tSpendCoins(int amount)
        {
            if (data.Medals < amount)
                return false;

            data.Medals -= amount;

            tSaveFile();

            return true;
        }

        #endregion

        #region [Dan titles]

        public bool tUpdateDanTitle(string title, bool isGold, int clearStatus)
        {
            bool changed = false;

            bool iG = isGold;
            int cs = clearStatus;

            if (this.data.DanTitles == null)
                this.data.DanTitles = new Dictionary<string, CDanTitle>();

            if (this.data.DanTitles.ContainsKey(title))
            {
                if (this.data.DanTitles[title].clearStatus > cs)
                    cs = this.data.DanTitles[title].clearStatus;
                if (this.data.DanTitles[title].isGold)
                    iG = true;
            }

            // Automatically set the dan to nameplate if new
            // Add a function within the NamePlate.cs file to update the title texture 

            if (!this.data.DanTitles.ContainsKey(title) || cs != clearStatus || iG != isGold)
            {
                changed = true;
                /*
                TJAPlayer3.NamePlateConfig.data.Dan[player] = title;
                TJAPlayer3.NamePlateConfig.data.DanGold[player] = iG;
                TJAPlayer3.NamePlateConfig.data.DanType[player] = cs;
                */
            }


            CDanTitle danTitle = new CDanTitle(iG, cs);

            this.data.DanTitles[title] = danTitle;

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
                cld = new CLocalizationData();
            }

            [JsonProperty("iType")]
            public int iType;

            [JsonProperty("Localization")]
            public CLocalizationData cld;
        }

        public class CPassStatus
        {
            public CPassStatus()
            {
                d = new int[5] { -1, -1, -1, -1, -1 };
            }

            public int[] d;
        }

        #endregion

        #region [Heya]

        public void tReindexCharacter(string[] characterNamesList)
        {
            string character = this.data.CharacterName;

            if (characterNamesList.Contains(character))
                this.data.Character = characterNamesList.ToList().IndexOf(character);

        }

        public void tUpdateCharacterName(string newChara)
        {
            this.data.CharacterName = newChara;
        }

        public void tApplyHeyaChanges()
        {
            this.tSaveFile();
        }

        #endregion

        #region [Player stats]

        public void tUpdateSongClearStatus(CSongListNode node, int clearStatus, int difficulty)
        {
            if (difficulty > (int)Difficulty.Edit) return;

            string _id = node.uniqueId.data.id;
            var _sdp = data.standardPasses;

            if (!_sdp.ContainsKey(_id))
                _sdp[_id] = new CPassStatus();

            var cps = _sdp[_id];

            var _values = cps.d;
            if (clearStatus > _values[difficulty])
            {
                cps.d[difficulty] = clearStatus;
                tSaveFile();
            }
        }

        #endregion

        public class Data
        {
            [JsonProperty("name")]
            public string Name = "プレイヤー1";

            [JsonProperty("title")]
            public string Title = "初心者";

            [JsonProperty("dan")]
            public string Dan = "新人";

            [JsonProperty("danGold")]
            public bool DanGold = false;

            [JsonProperty("danType")]
            public int DanType = 0;

            [JsonProperty("titleType")]
            public int TitleType = 0;

            [JsonProperty("puchiChara")]
            public string PuchiChara = "0";

            [JsonProperty("medals")]
            public int Medals = 0;

            [JsonProperty("totalEarnedMedals")]
            public int TotalEarnedMedals = 0;

            [JsonProperty("totalPlaycount")]
            public int TotalPlaycount = 0;

            [JsonProperty("character")]
            public int Character = 0;

            [JsonProperty("characterName")]
            public string CharacterName = "0";

            [JsonProperty("danTitles")]
            public Dictionary<string, CDanTitle> DanTitles = new Dictionary<string, CDanTitle>();

            [JsonProperty("namePlateTitles")]
            public Dictionary<string, CNamePlateTitle> NamePlateTitles = new Dictionary<string, CNamePlateTitle>();

            [JsonProperty("unlockedCharacters")]
            public List<string> UnlockedCharacters = new List<string>();

            [JsonProperty("unlockedPuchicharas")]
            public List<string> UnlockedPuchicharas = new List<string>();

            [JsonProperty("activeTriggers")]
            public HashSet<string> ActiveTriggers = new HashSet<string>();

            [JsonProperty("standardPasses")]
            public Dictionary<string, CPassStatus> standardPasses = new Dictionary<string, CPassStatus>();

        }

        public Data data = new Data();
        public string path = "Save.json";
        public string name = "Save";

        #region [private]

        private void tSaveFile()
        {
            ConfigManager.SaveConfig(data, path);
        }

        private void tLoadFile()
        {
            data = ConfigManager.GetConfig<Data>(path);
        }

        #endregion
    }
}
