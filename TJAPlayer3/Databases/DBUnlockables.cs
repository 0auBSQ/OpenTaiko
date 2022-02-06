using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace TJAPlayer3
{
    class DBUnlockables
    {
        public void tDBUnlockables()
        {
            if (!File.Exists(@".\Databases\Unlockables.json"))
                tSaveFile();

            tLoadFile();
        }

        #region [Auxilliary classes]

        public Dictionary<string, int> RequiredArgs = new Dictionary<string, int>()
        {
            ["ch"] = 1,
            ["cs"] = 1,
        };

        public class CUnlockConditions
        {
            public CUnlockConditions(string cd, int[] vl, string tp, string rf)
            {
                Condition = cd;
                Values = vl;
                Type = tp;
                Reference = rf;
            }

            // Condition type
            [JsonProperty("condition")]
            public string Condition;

            // Condition values
            [JsonProperty("values")]
            public int[] Values;

            // Condition type
            [JsonProperty("type")]
            public string Type;

            // Referenced chart
            [JsonProperty("reference")]
            public string Reference;

            [JsonIgnore]
            private int RequiredArgCount = -1;

            /*
             * == Types of conditions ==
             * l : "Less than"
             * le : "Less or equal"
             * e : "Equal"
             * me : "More or equal"
             * m : "More than" (Default)
             * d : "Different"
            */
            public bool tValueRequirementMet(int val1, int val2)
            {
                switch (this.Type)
                {
                    case "l":
                        return (val1 < val2);
                    case "le":
                        return (val1 <= val2);
                    case "e":
                        return (val1 == val2);
                    case "me":
                        return (val1 >= val2);
                    case "m":
                        return (val1 > val2);
                    case "d":
                        return (val1 != val2);
                    default:
                        return (val1 > val2);
                }
            }

            /*
             * == Condition avaliable ==
             * ch : "Coins here", coin requirement, payable within the heya menu, 1 value : [Coin price]
             * cs : "Coins shop", coin requirement, payable only within the Medal shop selection screen
             *
             *
             *
             * 
            */
            public (bool, string) tConditionMet(int[] inputValues)
            {
                if (RequiredArgCount < 0 && TJAPlayer3.Databases.DBUnlockables.RequiredArgs.ContainsKey(Condition))
                    RequiredArgCount = TJAPlayer3.Databases.DBUnlockables.RequiredArgs[Condition];

                if (this.Values.Length < this.RequiredArgCount)
                    return (false, CLangManager.LangInstance.GetString(90005) + this.Condition + " requires " + this.RequiredArgCount.ToString() + " values.");

                switch (this.Condition)
                {
                    case "ch":
                        // Coins are strictly more or equal
                        this.Type = "me";
                        bool fulfiled = this.tValueRequirementMet(inputValues[0], this.Values[0]);
                        return (fulfiled, CLangManager.LangInstance.GetString(90003 + ((fulfiled == false) ? 1 : 0)));
                    case "cs":
                        return (false, CLangManager.LangInstance.GetString(90001)); // Will be buyable later from the randomized shop
                }

                return (false, CLangManager.LangInstance.GetString(90000));
            }

            public string tConditionMessage()
            {
                if (RequiredArgCount < 0 && TJAPlayer3.Databases.DBUnlockables.RequiredArgs.ContainsKey(Condition))
                    RequiredArgCount = TJAPlayer3.Databases.DBUnlockables.RequiredArgs[Condition];

                if (this.Values.Length < this.RequiredArgCount)
                    return (CLangManager.LangInstance.GetString(90005) + this.Condition + " requires " + this.RequiredArgCount.ToString() + " values.");

                switch (this.Condition)
                {
                    case "ch":
                        return (CLangManager.LangInstance.GetString(90002) + this.Values[0]);
                    case "cs":
                        return (CLangManager.LangInstance.GetString(90001)); // Will be buyable later from the randomized shop
                }
                return (CLangManager.LangInstance.GetString(90000));
            }

        }

        #endregion


        public class CUnlockables
        {
            [JsonProperty("puchichara")]
            public Dictionary<int, CUnlockConditions> Puchichara = new Dictionary<int, CUnlockConditions>();
        }

        public CUnlockables data = new CUnlockables();

        #region [private]

        private void tSaveFile()
        {
            ConfigManager.SaveConfig(data, @".\Databases\Unlockables.json");
        }

        private void tLoadFile()
        {
            data = ConfigManager.GetConfig<CUnlockables>(@".\Databases\Unlockables.json");
        }

        #endregion
    }



}