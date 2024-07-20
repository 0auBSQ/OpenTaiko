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

            ["sc"] = 2,
            ["ce"] = 1,
            ["tp"] = 1,
            ["ap"] = 1,
            ["aw"] = 1,
            ["ig"] = 0,
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
             * (Note: Currently only me is relevant, the other types might be used in the future)
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

            public string GetRequiredClearStatus(int status, bool exact = false)
            {
                switch (status)
                {
                    case (int)EClearStatus.PERFECT:
                        return CLangManager.LangInstance.GetString(exact ? "UNLOCK_CONDITION_REQUIRE_PERFECT" : "UNLOCK_CONDITION_REQUIRE_PERFECT_MORE");
                    case (int)EClearStatus.FC:
                        return CLangManager.LangInstance.GetString(exact ? "UNLOCK_CONDITION_REQUIRE_FC" : "UNLOCK_CONDITION_REQUIRE_FC_MORE");
                    case (int)EClearStatus.CLEAR:
                        return CLangManager.LangInstance.GetString(exact ? "UNLOCK_CONDITION_REQUIRE_CLEAR" : "UNLOCK_CONDITION_REQUIRE_CLEAR_MORE");
                    case (int)EClearStatus.ASSISTED_CLEAR:
                        return CLangManager.LangInstance.GetString(exact ? "UNLOCK_CONDITION_REQUIRE_ASSIST" : "UNLOCK_CONDITION_REQUIRE_ASSIST_MORE");
                    case (int)EClearStatus.NONE:
                    default:
                        return CLangManager.LangInstance.GetString(exact ? "UNLOCK_CONDITION_REQUIRE_PLAY" : "UNLOCK_CONDITION_REQUIRE_PLAY_MORE");
                }
            }

            /*
             * == Condition avaliable ==
             * ch : "Coins here", coin requirement, payable within the heya menu, 1 value : [Coin price]
             * cs : "Coins shop", coin requirement, payable only within the Medal shop selection screen
             * cm : "Coins menu", coin requirement, payable only within the song select screen (used only for songs)
             * ce : "Coins earned", coins earned since the creation of the save file, 1 value : [Total earned coins]
             * dp : "Difficulty pass", count of difficulties pass, unlock check during the results screen, condition 3 values : [Difficulty int (0~4), Clear status (0~4), Number of performances], input 1 value [Plays fitting the condition]
             * lp : "Level pass", count of level pass, unlock check during the results screen, condition 3 values : [Star rating, Clear status (0~4), Number of performances], input 1 value [Plays fitting the condition]
             * sp : "Song performance", count of a specific song pass, unlock check during the results screen, condition 2 x n values for n songs  : [Difficulty int (0~4, if -1 : Any), Clear status (0~4), ...], input 1 value [Count of fullfiled songs], n references for n songs (Song ids)
             * sg : "Song genre (performance)", count of any unique song pass within a specific genre folder, unlock check during the results screen, condition 2 x n values for n songs : [Song count, Clear status (0~4), ...], input 1 value [Count of fullfiled genres], n references for n genres (Genre names)
             * sc : "Song charter (performance)", count of any chart pass by a specific charter, unlock check during the results screen, condition 2 x n values for n songs : [Song count, Clear status (0~4), ...], input 1 value [Count of fullfiled charters], n references for n charters (Charter names)
             * tp : "Total plays", 1 value : [Total playcount]
             * ap : "AI battle plays", 1 value : [AI battle playcount]
             * aw : "AI battle wins", 1 value : [AI battle wins count]
             * ig : "Impossible to Get", (not recommanded) used to be able to have content in database that is impossible to unlock, no values
             * 
            */
            public (bool, string?) tConditionMetWrapper(int player, EScreen screen = EScreen.MyRoom)
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
                            return (false, CLangManager.LangInstance.GetString("UNLOCK_CONDITION_ERROR", this.Condition, this.RequiredArgCount.ToString()));
                    case "ce":
                        if (this.Values.Length == 1)
                            return tConditionMet(new int[] { (int)TJAPlayer3.SaveFileInstances[player].data.TotalEarnedMedals }, screen);
                        else
                            return (false, CLangManager.LangInstance.GetString("UNLOCK_CONDITION_ERROR", this.Condition, this.RequiredArgCount.ToString()));
                    case "ap":
                        if (this.Values.Length == 1)
                            return tConditionMet(new int[] { (int)TJAPlayer3.SaveFileInstances[player].data.AIBattleModePlaycount }, screen);
                        else
                            return (false, CLangManager.LangInstance.GetString("UNLOCK_CONDITION_ERROR", this.Condition, this.RequiredArgCount.ToString()));
                    case "aw":
                        if (this.Values.Length == 1)
                            return tConditionMet(new int[] { (int)TJAPlayer3.SaveFileInstances[player].data.AIBattleModeWins }, screen);
                        else
                            return (false, CLangManager.LangInstance.GetString("UNLOCK_CONDITION_ERROR", this.Condition, this.RequiredArgCount.ToString()));
                    case "tp":
                        if (this.Values.Length == 1)
                            return tConditionMet(new int[] { (int)TJAPlayer3.SaveFileInstances[player].data.TotalPlaycount }, screen);
                        else
                            return (false, CLangManager.LangInstance.GetString("UNLOCK_CONDITION_ERROR", this.Condition, this.RequiredArgCount.ToString()));
                    case "dp":
                    case "lp":
                        if (this.Values.Length == 3)
                            return tConditionMet(new int[] { tGetCountChartsPassingCondition(player) }, screen);
                        else
                            return (false, CLangManager.LangInstance.GetString("UNLOCK_CONDITION_ERROR", this.Condition, this.RequiredArgCount.ToString()));
                    case "sp":
                    case "sg":
                    case "sc":
                        if (this.Values.Length % this.RequiredArgCount == 0
                            && this.Reference.Length == this.Values.Length / this.RequiredArgCount)
                            return tConditionMet(new int[] { tGetCountChartsPassingCondition(player) }, screen);
                        else
                            return (false, CLangManager.LangInstance.GetString("UNLOCK_CONDITION_ERROR2", this.Condition, this.RequiredArgCount.ToString()));
                    case "ig":
                        return (false, "");
                }

                return (false, CLangManager.LangInstance.GetString("UNLOCK_CONDITION_INVALID"));
            }


            public (bool, string?) tConditionMet(int[] inputValues, EScreen screen)
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
                            return (fulfiled, CLangManager.LangInstance.GetString(fulfiled ? "UNLOCK_COIN_BOUGHT" : "UNLOCK_COIN_MORE"));
                        default:
                            return (false, null); // Return the same text if my room
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
                            return (false, CLangManager.LangInstance.GetString("UNLOCK_CONDITION_INVALID"));
                        case "ce":
                        case "tp":
                        case "ap":
                        case "aw":
                            bool fulfiled = this.tValueRequirementMet(inputValues[0], this.Values[0]);
                            return (fulfiled, "");
                        case "dp":
                        case "lp":
                            fulfiled = this.tValueRequirementMet(inputValues[0], this.Values[2]);
                            return (fulfiled, "");
                        case "sp":
                        case "sg":
                        case "sc":
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
                            return (fulfiled, CLangManager.LangInstance.GetString(fulfiled ? "UNLOCK_COIN_BOUGHT" : "UNLOCK_COIN_MORE"));
                        default:
                            return (false, null);
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
                            return (fulfiled, CLangManager.LangInstance.GetString(fulfiled ? "UNLOCK_COIN_BOUGHT" : "UNLOCK_COIN_MORE"));
                    }
                }

                return (false, CLangManager.LangInstance.GetString("UNLOCK_CONDITION_INVALID"));
            }

            // My Room menu usage, to improve later
            public string tConditionMessage(EScreen screen = EScreen.MyRoom)
            {
                if (RequiredArgCount < 0 && RequiredArgs.ContainsKey(Condition))
                    RequiredArgCount = RequiredArgs[Condition];

                if (this.Values.Length < this.RequiredArgCount)
                    return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_ERROR", this.Condition, this.RequiredArgCount);

                // Only the player loaded as 1P can check unlockables in real time
                var SaveData = TJAPlayer3.SaveFileInstances[TJAPlayer3.SaveFile].data;
                var ChartStats = SaveData.bestPlaysStats;

                switch (this.Condition)
                {
                    case "ch":
                        {
                            if (screen == EScreen.MyRoom)
                                return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_COST", this.Values[0]);
                            return (CLangManager.LangInstance.GetString("UNLOCK_CONDITION_INVALID"));
                        }
                    case "cs":
                        {
                            if (screen == EScreen.Shop)
                                return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_COST", this.Values[0]);
                            return (CLangManager.LangInstance.GetString("UNLOCK_CONDITION_SHOP"));
                        }
                    case "cm":
                        {
                            if (screen == EScreen.SongSelect)
                                return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_COST", this.Values[0]);
                            return (CLangManager.LangInstance.GetString("UNLOCK_CONDITION_INVALID"));
                        }
                    case "ce":
                        return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_EARN", this.Values[0], SaveData.TotalEarnedMedals);
                    case "ap":
                        return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_AIPLAY", this.Values[0], SaveData.AIBattleModePlaycount);
                    case "aw":
                        return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_AIWIN", this.Values[0], SaveData.AIBattleModeWins);
                    case "tp":
                        return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_PLAY", this.Values[0], SaveData.TotalPlaycount);
                    case "dp":
                        {
                            var _aimedDifficulty = this.Values[0];
                            var _aimedStatus = this.Values[1];

                            if (_aimedStatus < (int)EClearStatus.NONE || _aimedStatus >= (int)EClearStatus.TOTAL) return (CLangManager.LangInstance.GetString("UNLOCK_CONDITION_INVALID"));
                            if (_aimedDifficulty < (int)Difficulty.Easy || _aimedDifficulty > (int)Difficulty.Edit) return (CLangManager.LangInstance.GetString("UNLOCK_CONDITION_INVALID"));

                            var _table = ChartStats.ClearStatuses[_aimedDifficulty];
                            var _ura = ChartStats.ClearStatuses[(int)Difficulty.Edit];
                            int _count = 0;
                            for (int i = _aimedStatus; i < (int)EClearStatus.TOTAL; i++)
                            {
                                _count += _table[i];
                                if (_aimedDifficulty == (int)Difficulty.Oni) _count += _ura[i];
                            }

                            var diffString = (_aimedDifficulty == (int)Difficulty.Oni) ? CLangManager.LangInstance.GetString("DIFF_EXEXTRA") : CLangManager.LangInstance.GetDifficulty(_aimedDifficulty);
                            var statusString = GetRequiredClearStatus(_aimedStatus);
                            return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_PLAYDIFF", statusString, this.Values[2], diffString, _count);
                        }
                    case "lp":
                        {
                            var _aimedDifficulty = this.Values[0];
                            var _aimedStatus = this.Values[1];

                            if (_aimedStatus < (int)EClearStatus.NONE || _aimedStatus >= (int)EClearStatus.TOTAL) return (CLangManager.LangInstance.GetString("UNLOCK_CONDITION_INVALID"));

                            int _count = 0;
                            if (_aimedStatus == (int)EClearStatus.NONE) _count = ChartStats.LevelPlays.TryGetValue(_aimedDifficulty, out var value) ? value : 0;
                            else if (_aimedStatus <= (int)EClearStatus.CLEAR) _count = ChartStats.LevelClears.TryGetValue(_aimedDifficulty, out var value) ? value : 0;
                            else if (_aimedStatus == (int)EClearStatus.FC) _count = ChartStats.LevelFCs.TryGetValue(_aimedDifficulty, out var value) ? value : 0;
                            else _count = ChartStats.LevelPerfects.TryGetValue(_aimedDifficulty, out var value) ? value : 0;

                            var statusString = GetRequiredClearStatus(_aimedStatus);
                            return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_PLAYLEVEL", statusString, this.Values[2], _aimedDifficulty, _count);
                        }
                    case "sp":
                        {
                            List<string> _rows = new List<string>();
                            var _challengeCount = this.Values.Length / this.RequiredArgCount;

                            var _count = 0;
                            for (int i = 0; i < _challengeCount; i++)
                            {
                                int _base = i * this.RequiredArgCount;
                                string _songId = this.Reference[i];
                                var _aimedDifficulty = this.Values[_base];
                                var _aimedStatus = this.Values[_base + 1];

                                var diffString = CLangManager.LangInstance.GetDifficulty(_aimedDifficulty);
                                var statusString = GetRequiredClearStatus(_aimedStatus);
                                var _songName = CSongDict.tGetNodeFromID(_songId)?.ldTitle.GetString("") ?? "[Not found]";

                                _rows.Add(CLangManager.LangInstance.GetString("UNLOCK_CONDITION_CHALLENGE_PLAYDIFF", statusString, _songName, diffString));


                                // Safisfied count
                                if (_aimedDifficulty >= (int)Difficulty.Easy && _aimedDifficulty <= (int)Difficulty.Edit)
                                {
                                    string key = _songId + _aimedDifficulty.ToString();
                                    var _cht = SaveData.bestPlaysDistinctCharts.TryGetValue(key, out var value) ? value : null;
                                    if (_cht != null && _cht.ClearStatus + 1 >= _aimedStatus) _count++;

                                }
                                else if (_aimedDifficulty < (int)Difficulty.Easy)
                                {
                                    for (int diff = (int)Difficulty.Easy; diff <= (int)Difficulty.Edit; diff++)
                                    {
                                        string key = _songId + diff.ToString();
                                        var _cht = SaveData.bestPlaysDistinctCharts.TryGetValue(key, out var value) ? value : null;
                                        if (_cht != null && _cht.ClearStatus + 1 >= _aimedStatus)
                                        {
                                            _count++;
                                            break;
                                        }
                                    }
                                }
                            }

                            // Push front
                            _rows.Insert(0, CLangManager.LangInstance.GetString("UNLOCK_CONDITION_CHALLENGE", _count, _challengeCount));
                            return String.Join("\n", _rows);
                        }
                    case "sg":
                        {
                            List<string> _rows = new List<string>();
                            var _challengeCount = this.Values.Length / this.RequiredArgCount;

                            var _count = 0;
                            for (int i = 0; i < _challengeCount; i++)
                            {
                                int _base = i * this.RequiredArgCount;
                                string _genreName = this.Reference[i];
                                int _songCount = this.Values[_base];
                                var _aimedStatus = this.Values[_base + 1];

                                int _satifsiedCount = 0;
                                if (_aimedStatus == (int)EClearStatus.NONE) _satifsiedCount = ChartStats.SongGenrePlays.TryGetValue(_genreName, out var value) ? value : 0;
                                else if (_aimedStatus <= (int)EClearStatus.CLEAR) _satifsiedCount = ChartStats.SongGenreClears.TryGetValue(_genreName, out var value) ? value : 0;
                                else if (_aimedStatus == (int)EClearStatus.FC) _satifsiedCount = ChartStats.SongGenreFCs.TryGetValue(_genreName, out var value) ? value : 0;
                                else _satifsiedCount = ChartStats.SongGenrePerfects.TryGetValue(_genreName, out var value) ? value : 0;

                                if (_satifsiedCount >= _songCount) _count++;


                                var statusString = GetRequiredClearStatus(_aimedStatus);
                                _rows.Add(CLangManager.LangInstance.GetString("UNLOCK_CONDITION_CHALLENGE_PLAYGENRE", statusString, _songCount, _genreName, _satifsiedCount));
                            }

                            _rows.Insert(0, CLangManager.LangInstance.GetString("UNLOCK_CONDITION_CHALLENGE", _count, _challengeCount));
                            return String.Join("\n", _rows);
                        }
                    case "sc":
                        {
                            List<string> _rows = new List<string>();
                            var _challengeCount = this.Values.Length / this.RequiredArgCount;

                            var _count = 0;
                            for (int i = 0; i < _challengeCount; i++)
                            {
                                int _base = i * this.RequiredArgCount;
                                string _charterName = this.Reference[i];
                                int _songCount = this.Values[_base];
                                var _aimedStatus = this.Values[_base + 1];

                                int _satifsiedCount = 0;
                                if (_aimedStatus == (int)EClearStatus.NONE) _satifsiedCount = ChartStats.CharterPlays.TryGetValue(_charterName, out var value) ? value : 0;
                                else if (_aimedStatus <= (int)EClearStatus.CLEAR) _satifsiedCount = ChartStats.CharterClears.TryGetValue(_charterName, out var value) ? value : 0;
                                else if (_aimedStatus == (int)EClearStatus.FC) _satifsiedCount = ChartStats.CharterFCs.TryGetValue(_charterName, out var value) ? value : 0;
                                else _satifsiedCount = ChartStats.CharterPerfects.TryGetValue(_charterName, out var value) ? value : 0;

                                if (_satifsiedCount >= _songCount) _count++;


                                var statusString = GetRequiredClearStatus(_aimedStatus);
                                _rows.Add(CLangManager.LangInstance.GetString("UNLOCK_CONDITION_CHALLENGE_PLAYCHARTER", statusString, _songCount, _charterName, _satifsiedCount));
                            }

                            _rows.Insert(0, CLangManager.LangInstance.GetString("UNLOCK_CONDITION_CHALLENGE", _count, _challengeCount));
                            return String.Join("\n", _rows);
                        }

                }
                // Includes cm or ig which are not supposed to be displayed in My Room
                return (CLangManager.LangInstance.GetString("UNLOCK_CONDITION_INVALID"));
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
                                if (_cht != null && _cht.ClearStatus + 1 >= _aimedStatus) _count++;

                            }
                            else if (_aimedDifficulty < (int)Difficulty.Easy)
                            {
                                for (int diff = (int)Difficulty.Easy; diff <= (int)Difficulty.Edit; diff++)
                                {
                                    string key = _songId + diff.ToString();
                                    var _cht = bpDistinctCharts.TryGetValue(key, out var value) ? value : null;
                                    if (_cht != null && _cht.ClearStatus + 1 >= _aimedStatus)
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
                    case "sc":
                        _count = 0;
                        for (int i = 0; i < this.Values.Length / this.RequiredArgCount; i++)
                        {
                            int _base = i * this.RequiredArgCount;
                            string _charterName = this.Reference[i];
                            int _songCount = this.Values[_base];
                            _aimedStatus = this.Values[_base + 1];

                            int _satifsiedCount = 0;
                            if (_aimedStatus == (int)EClearStatus.NONE) _satifsiedCount = chartStats.CharterPlays.TryGetValue(_charterName, out var value) ? value : 0;
                            else if (_aimedStatus <= (int)EClearStatus.CLEAR) _satifsiedCount = chartStats.CharterClears.TryGetValue(_charterName, out var value) ? value : 0;
                            else if (_aimedStatus == (int)EClearStatus.FC) _satifsiedCount = chartStats.CharterFCs.TryGetValue(_charterName, out var value) ? value : 0;
                            else _satifsiedCount = chartStats.CharterPerfects.TryGetValue(_charterName, out var value) ? value : 0;

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