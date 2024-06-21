using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using static TJAPlayer3.BestPlayRecords;

namespace TJAPlayer3
{
    class DBUnlockables
    {
        public static Dictionary<string, int> RequiredArgs = new Dictionary<string, int>()
        {
            ["ch"] = 1,
            ["cs"] = 1,
            ["dp"] = 3,
            ["lp"] = 3,
            ["sp"] = 2,
            ["sg"] = 2,
        };

        public class CUnlockConditions
        {
            public CUnlockConditions()
            {
                Condition = "";
                Values = new int[]{ 0 };
                Type = "me";
                Reference = new string[] { "" };
            }
            public CUnlockConditions(string cd, int[] vl, string tp, string[] rf)
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

            // Referenced charts
            [JsonProperty("references")]
            public string[] Reference;

            [JsonIgnore]
            private int RequiredArgCount = -1;

            public bool tHasCondition()
            {
                return Condition != "";
            }

            /*
             * == Types of conditions ==
             * l : "Less than"
             * le : "Less or equal"
             * e : "Equal"
             * me : "More or equal"  (Default)
             * m : "More than"
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
                        return (val1 >= val2);
                }
            }

            /*
             * == Condition avaliable ==
             * ch : "Coins here", coin requirement, payable within the heya menu, 1 value : [Coin price]
             * cs : "Coins shop", coin requirement, payable only within the Medal shop selection screen
             * cm : "Coins menu", coin requirement, payable only within the song select screen (used only for songs)
             * dp : "Difficulty pass", count of difficulties pass, unlock check during the results screen, condition 3 values : [Difficulty int (0~4), Clear status (0~4), Number of performances], input 1 value [Plays fitting the condition]
             * lp : "Level pass", count of level pass, unlock check during the results screen, condition 3 values : [Star rating, Clear status (0~4), Number of performances], input 1 value [Plays fitting the condition]
             * sp : "Song performance", count of a specific song pass, unlock check during the results screen, condition 2 x n values for n songs  : [Difficulty int (0~4, if -1 : Any), Clear status (0~4), ...], input 1 value [Count of fullfiled songs], n references for n songs (Song ids)
             * sg : "Song genre (performance)", count of any unique song pass within a specific genre folder, unlock check during the results screen, condition 2 x n values for n songs : [Song count, Clear status (0~4), ...], input 1 value [Count of fullfiled genres], n references for n genres (Genre names)
             * 
             * 
            */
            public (bool, string) tConditionMetWrapper(int player, EScreen screen = EScreen.MyRoom)
            {
                if (RequiredArgCount < 0 && RequiredArgs.ContainsKey(Condition))
                    RequiredArgCount = RequiredArgs[Condition];

                switch (this.Condition)
                {
                    case "ch":
                    case "cs":
                    case "cm":
                        if (this.Values.Length == 1)
                            return tConditionMet(new int[] { (int)TJAPlayer3.SaveFileInstances[player].data.Medals }, screen);
                        else
                            return (false, CLangManager.LangInstance.GetString(90005) + this.Condition + " requires " + this.RequiredArgCount.ToString() + " values.");
                    case "dp":
                    case "lp":
                        if (this.Values.Length == 3)
                            return tConditionMet(new int[] { tGetCountChartsPassingCondition(player) }, screen);
                        else
                            return (false, CLangManager.LangInstance.GetString(90005) + this.Condition + " requires " + this.RequiredArgCount.ToString() + " values.");
                    case "sp":
                    case "sg":
                        if (this.Values.Length % this.RequiredArgCount == 0
                            && this.Reference.Length == this.Values.Length / this.RequiredArgCount)
                            return tConditionMet(new int[] { tGetCountChartsPassingCondition(player) }, screen);
                        else
                            return (false, CLangManager.LangInstance.GetString(90005) + this.Condition + " requires (" + this.RequiredArgCount.ToString() + " * n) values and n references.");

                }

                return (false, CLangManager.LangInstance.GetString(90000));
            }


            public (bool, string) tConditionMet(int[] inputValues, EScreen screen)
            {
                // Trying to unlock an item from the My Room menu (If my room buy => check if enough coints, else => Display a hint to how to get the item)
                if (screen == EScreen.MyRoom)
                {
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
                }
                // Unlockables from result screen or specific events (If any buy event => Invalid command, else check)
                else if (screen == EScreen.Internal)
                {
                    switch (this.Condition)
                    {

                        case "ch":
                        case "cs":
                        case "cm":
                            return (false, CLangManager.LangInstance.GetString(90000));
                        case "dp":
                        case "lp":
                            bool fulfiled = this.tValueRequirementMet(inputValues[0], this.Values[2]);
                            return (fulfiled, "");
                        case "sp":
                        case "sg":
                            fulfiled = this.tValueRequirementMet(inputValues[0], this.Reference.Length);
                            return (fulfiled, "");
                    }
                }
                // Trying to unlock an item from the Shop menu (If shop => check if enough coins, else => Invalid command)
                else if (screen == EScreen.Shop)
                {
                    switch (this.Condition)
                    {
                        case "cs":
                            // Coins are strictly more or equal
                            this.Type = "me";
                            bool fulfiled = this.tValueRequirementMet(inputValues[0], this.Values[0]);
                            return (fulfiled, CLangManager.LangInstance.GetString(90003 + ((fulfiled == false) ? 1 : 0)));
                    }
                }
                // Trying to unlock an item from the Song Select screen (If song select => check if enough coins, else => Invalid command)
                else if (screen == EScreen.SongSelect)
                {
                    switch (this.Condition)
                    {
                        case "cm":
                            // Coins are strictly more or equal
                            this.Type = "me";
                            bool fulfiled = this.tValueRequirementMet(inputValues[0], this.Values[0]);
                            return (fulfiled, CLangManager.LangInstance.GetString(90003 + ((fulfiled == false) ? 1 : 0)));
                    }
                }

                return (false, CLangManager.LangInstance.GetString(90000));
            }

            // My Room menu usage, to improve later
            public string tConditionMessage()
            {
                if (RequiredArgCount < 0 && RequiredArgs.ContainsKey(Condition))
                    RequiredArgCount = RequiredArgs[Condition];

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

            public enum EScreen
            {
                MyRoom,
                Shop,
                SongSelect,
                Internal
            }

            #region [private calls]

            private int tGetCountChartsPassingCondition(int player)
            {
                int _aimedDifficulty = 0;
                int _aimedStatus = 0;

                if (this.Condition == "dp" || this.Condition == "lp")
                {
                    _aimedDifficulty = this.Values[0]; // Difficulty if dp, Level if lp
                    _aimedStatus = this.Values[1];

                    // dp and lp only work for regular (Dan and Tower excluded) charts
                    if (_aimedStatus < (int)EClearStatus.NONE || _aimedStatus >= (int)EClearStatus.TOTAL) return 0;
                    if (this.Condition == "dp" && (_aimedDifficulty < (int)Difficulty.Easy || _aimedDifficulty > (int)Difficulty.Edit)) return 0;  
                }

                var bpDistinctCharts = TJAPlayer3.SaveFileInstances[player].data.bestPlaysDistinctCharts;
                var chartStats = TJAPlayer3.SaveFileInstances[player].data.bestPlaysStats;

                switch (this.Condition)
                {
                    case "dp":
                        var _table = chartStats.ClearStatuses[_aimedDifficulty];
                        var _ura = chartStats.ClearStatuses[(int)Difficulty.Edit];
                        int _count = 0;
                        for (int i = _aimedStatus; i < (int)EClearStatus.TOTAL; i++)
                        {
                            _count += _table[i];
                            if (_aimedDifficulty == (int)Difficulty.Oni) _count += _ura[i];
                        }
                        return _count;
                    case "lp":
                        if (_aimedStatus == (int)EClearStatus.NONE) return chartStats.LevelPlays.TryGetValue(_aimedDifficulty, out var value) ? value : 0;
                        else if (_aimedStatus <= (int)EClearStatus.CLEAR) return chartStats.LevelClears.TryGetValue(_aimedDifficulty, out var value) ? value : 0;
                        else if (_aimedStatus == (int)EClearStatus.FC) return chartStats.LevelFCs.TryGetValue(_aimedDifficulty, out var value) ? value : 0;
                        else return chartStats.LevelPerfects.TryGetValue(_aimedDifficulty, out var value) ? value : 0;
                    case "sp":
                        _count = 0;
                        for (int i = 0; i < this.Values.Length / this.RequiredArgCount; i++)
                        {
                            int _base = i * this.RequiredArgCount;
                            string _songId = this.Reference[i];
                            _aimedDifficulty = this.Values[_base];
                            _aimedStatus = this.Values[_base + 1];

                            if (_aimedDifficulty >= (int)Difficulty.Easy && _aimedDifficulty <= (int)Difficulty.Edit)
                            {
                                string key = _songId + _aimedDifficulty.ToString();
                                var _cht = bpDistinctCharts.TryGetValue(key, out var value) ? value : null;
                                if (_cht != null && _cht.ClearStatus >= _aimedStatus) _count++;

                            }
                            else if (_aimedDifficulty < (int)Difficulty.Easy)
                            {
                                for (int diff = (int)Difficulty.Easy; diff <= (int)Difficulty.Edit; diff++)
                                {
                                    string key = _songId + diff.ToString();
                                    var _cht = bpDistinctCharts.TryGetValue(key, out var value) ? value : null;
                                    if (_cht != null && _cht.ClearStatus >= _aimedStatus)
                                    {
                                        _count++;
                                        break;
                                    }
                                }
                            }
                        }
                        return _count;
                    case "sg":
                        _count = 0;
                        for (int i = 0; i < this.Values.Length / this.RequiredArgCount; i++)
                        {
                            int _base = i * this.RequiredArgCount;
                            string _genreName = this.Reference[i];
                            int _songCount = this.Values[_base];
                            _aimedStatus = this.Values[_base + 1];

                            int _satifsiedCount = 0;
                            if (_aimedStatus == (int)EClearStatus.NONE) _satifsiedCount = chartStats.SongGenrePlays.TryGetValue(_genreName, out var value) ? value : 0;
                            else if (_aimedStatus <= (int)EClearStatus.CLEAR) _satifsiedCount = chartStats.SongGenreClears.TryGetValue(_genreName, out var value) ? value : 0;
                            else if (_aimedStatus == (int)EClearStatus.FC) _satifsiedCount = chartStats.SongGenreFCs.TryGetValue(_genreName, out var value) ? value : 0;
                            else _satifsiedCount = chartStats.SongGenrePerfects.TryGetValue(_genreName, out var value) ? value : 0;

                            if (_satifsiedCount >= _songCount) _count++;
                        }
                        return _count;
                }
                return -1;
            }

            #endregion

        }

    }



}