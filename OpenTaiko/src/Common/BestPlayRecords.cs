using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    internal class BestPlayRecords
    {

        public enum EClearStatus
        {
            NONE = 0,
            ASSISTED_CLEAR = 1,
            CLEAR = 2,
            FC = 3,
            PERFECT = 4,
            TOTAL = 5
        }

        public enum EDanClearStatus
        {
            NONE = 0,
            ASSISTED_CLEAR_RED = 1,
            ASSISTED_CLEAR_GOLD = 2,
            CLEAR_RED = 3,
            CLEAR_GOLD = 4,
            FC_RED = 5,
            FC_GOLD = 6,
            PERFECT_RED = 7,
            PERFECT_GOLD = 8,
            TOTAL = 9
        }

        public enum ETowerClearStatus
        {
            NONE = 0,
            ASSISTED_CLEAR = 1,
            PROGRESS_10 = 2,
            PROGRESS_25 = 3,
            PROGRESS_50 = 4,
            PROGRESS_75 = 5,
            CLEAR = 6,
            FC = 7,
            PERFECT = 8,
            TOTAL = 9
        }

        public class CSongSelectTableEntry
        {
            public int ScoreRankDifficulty = 0;
            public int ScoreRank = -1;
            public int ClearStatusDifficulty = 0;
            public int ClearStatus = -1;
            public int TowerReachedFloor = 0;
            public int[] ClearStatuses = new int[(int)Difficulty.Total] { 0, 0, 0, 0, 0, 0, 0 };
            public int[] ScoreRanks = new int[(int)Difficulty.Total] { 0, 0, 0, 0, 0, 0, 0 };
            public int[] HighScore = new int[(int)Difficulty.Total] { 0, 0, 0, 0, 0, 0, 0 };
        }

        public class CBestPlayRecord
        {
            public string ChartUniqueId = "none";
            public string ChartGenre = "none";
            public string Charter = "none";
            public string Artist = "none";
            public Int64 PlayMods = 0;
            public Int64 ChartDifficulty = 3;
            public Int64 ChartLevel = 8;
            public Int64 ClearStatus = -1;
            public Int64 ScoreRank = -1;
            public Int64 HighScore = 0;
            public Int64 TowerBestFloor = 0;
            public List<int> DanExam1 = new List<int> { -1 };
            public List<int> DanExam2 = new List<int> { -1 };
            public List<int> DanExam3 = new List<int> { -1 };
            public List<int> DanExam4 = new List<int> { -1 };
            public List<int> DanExam5 = new List<int> { -1 };
            public List<int> DanExam6 = new List<int> { -1 };
            public List<int> DanExam7 = new List<int> { -1 };
            public Int64 PlayCount = 1;
            public Int64 HighScoreGoodCount = 0;
            public Int64 HighScoreOkCount = 0;
            public Int64 HighScoreBadCount = 0;
            public Int64 HighScoreMaxCombo = 0;
            public Int64 HighScoreRollCount = 0;
            public Int64 HighScoreADLibCount = 0;
            public Int64 HighScoreBoomCount = 0;
        }

        public class CBestPlayStats
        {
            public int DistinctPlays = 0;
            public int DistinctClears = 0;
            public int DistinctFCs = 0;
            public int DistinctPerfects = 0;
            public int SongDistinctPlays = 0;
            public int SongDistinctClears = 0;
            public int SongDistinctFCs = 0;
            public int SongDistinctPerfects = 0;
            public int[][] ClearStatuses = new int[(int)Difficulty.Total][];
            public int[][] ScoreRanks = new int[(int)Difficulty.Total][];
            public Dictionary<int, int> LevelPlays = new Dictionary<int, int>();
            public Dictionary<int, int> LevelClears = new Dictionary<int, int>();
            public Dictionary<int, int> LevelFCs = new Dictionary<int, int>();
            public Dictionary<int, int> LevelPerfects = new Dictionary<int, int>();
            public Dictionary<string, int> GenrePlays = new Dictionary<string, int>();
            public Dictionary<string, int> GenreClears = new Dictionary<string, int>();
            public Dictionary<string, int> GenreFCs = new Dictionary<string, int>();
            public Dictionary<string, int> GenrePerfects = new Dictionary<string, int>();
            public Dictionary<string, int> SongGenrePlays = new Dictionary<string, int>();
            public Dictionary<string, int> SongGenreClears = new Dictionary<string, int>();
            public Dictionary<string, int> SongGenreFCs = new Dictionary<string, int>();
            public Dictionary<string, int> SongGenrePerfects = new Dictionary<string, int>();
            public Dictionary<string, int> CharterPlays = new Dictionary<string, int>();
            public Dictionary<string, int> CharterClears = new Dictionary<string, int>();
            public Dictionary<string, int> CharterFCs = new Dictionary<string, int>();
            public Dictionary<string, int> CharterPerfects = new Dictionary<string, int>();

            public CBestPlayStats()
            {
                // 0 : Not clear, 1 : Assisted clear, 2 : Clear, 3 : FC, 4 : Perfect
                ClearStatuses[0] = new int[(int)EClearStatus.TOTAL] { 0, 0, 0, 0, 0 };
                ClearStatuses[1] = new int[(int)EClearStatus.TOTAL] { 0, 0, 0, 0, 0 };
                ClearStatuses[2] = new int[(int)EClearStatus.TOTAL] { 0, 0, 0, 0, 0 };
                ClearStatuses[3] = new int[(int)EClearStatus.TOTAL] { 0, 0, 0, 0, 0 };
                ClearStatuses[4] = new int[(int)EClearStatus.TOTAL] { 0, 0, 0, 0, 0 };
                ClearStatuses[5] = new int[(int)ETowerClearStatus.TOTAL] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                ClearStatuses[6] = new int[(int)EDanClearStatus.TOTAL] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

                // 0 : None, 1 : E, 2 : D, 3 : C, 4 : B, 5 : A, 6 : S, 7 : Omega
                ScoreRanks[0] = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                ScoreRanks[1] = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                ScoreRanks[2] = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                ScoreRanks[3] = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                ScoreRanks[4] = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
            }
        }

        static private void InitOrAddDict<T>(Dictionary<T, int> dict, T entry) where T : notnull
        {
            if (!dict.ContainsKey(entry))
                dict[entry] = 0;
            dict[entry]++;
        }

        static public CBestPlayStats tGenerateBestPlayStats(
            Dictionary<string, CBestPlayRecord>.ValueCollection uniqueChartBestPlays,
            Dictionary<string, CBestPlayRecord>.ValueCollection uniqueSongBestPlays
        )
        {
            CBestPlayStats stats = new CBestPlayStats();

            // Individual charts
            foreach (CBestPlayRecord record in uniqueChartBestPlays)
            {
                Int64 roundedDifficulty = Math.Max((int)Difficulty.Easy, Math.Min((int)Difficulty.Total - 1, record.ChartDifficulty));
                if (roundedDifficulty <= (int)Difficulty.Edit)
                {
                    string[] ChartersArr = record.Charter.SplitByCommas();
                    Int64 roundedScoreRank = Math.Max(0, Math.Min(7, record.ScoreRank + 1));
                    Int64 roundedClearStatus = Math.Max((int)EClearStatus.NONE, Math.Min((int)EClearStatus.PERFECT, record.ClearStatus + 1));

                    stats.ScoreRanks[roundedDifficulty][roundedScoreRank]++;
                    stats.ClearStatuses[roundedDifficulty][roundedClearStatus]++;
                    foreach (string Charter in ChartersArr)
                    {
                        InitOrAddDict(stats.CharterPlays, Charter);
                        if (roundedClearStatus >= (int)EClearStatus.CLEAR) InitOrAddDict(stats.CharterClears, Charter);
                        if (roundedClearStatus >= (int)EClearStatus.FC) InitOrAddDict(stats.CharterFCs, Charter);
                        if (roundedClearStatus == (int)EClearStatus.PERFECT) InitOrAddDict(stats.CharterPerfects, Charter);
                    }
                    InitOrAddDict(stats.GenrePlays, record.ChartGenre);
                    if (roundedClearStatus >= (int)EClearStatus.CLEAR) InitOrAddDict(stats.GenreClears, record.ChartGenre);
                    if (roundedClearStatus >= (int)EClearStatus.FC) InitOrAddDict(stats.GenreFCs, record.ChartGenre);
                    if (roundedClearStatus == (int)EClearStatus.PERFECT) InitOrAddDict(stats.GenrePerfects, record.ChartGenre);
                    InitOrAddDict(stats.LevelPlays, (int)record.ChartLevel);
                    if (roundedClearStatus >= (int)EClearStatus.CLEAR) InitOrAddDict(stats.LevelClears, (int)record.ChartLevel);
                    if (roundedClearStatus >= (int)EClearStatus.FC) InitOrAddDict(stats.LevelFCs, (int)record.ChartLevel);
                    if (roundedClearStatus == (int)EClearStatus.PERFECT) InitOrAddDict(stats.LevelPerfects, (int)record.ChartLevel);
                    stats.DistinctPlays++;
                    if (roundedClearStatus >= (int)EClearStatus.CLEAR) stats.DistinctClears++;
                    if (roundedClearStatus >= (int)EClearStatus.FC) stats.DistinctFCs++;
                    if (roundedClearStatus == (int)EClearStatus.PERFECT) stats.DistinctPerfects++;
                }
                else if (roundedDifficulty == (int)Difficulty.Tower)
                {
                    Int64 roundedClearStatus = Math.Max((int)ETowerClearStatus.NONE, Math.Min((int)ETowerClearStatus.TOTAL, record.ClearStatus + 1));
                    stats.ClearStatuses[roundedDifficulty][roundedClearStatus]++;

                }
                else if (roundedDifficulty == (int)Difficulty.Dan)
                {
                    Int64 roundedClearStatus = Math.Max((int)EDanClearStatus.NONE, Math.Min((int)EDanClearStatus.TOTAL, record.ClearStatus + 1));
                    stats.ClearStatuses[roundedDifficulty][roundedClearStatus]++;

                }
            }

            // Individual songs
            foreach (CBestPlayRecord record in uniqueSongBestPlays)
            {
                Int64 roundedDifficulty = Math.Max((int)Difficulty.Easy, Math.Min((int)Difficulty.Total - 1, record.ChartDifficulty));
                
                if (roundedDifficulty <= (int)Difficulty.Edit)
                {
                    Int64 roundedClearStatus = Math.Max((int)EClearStatus.NONE, Math.Min((int)EClearStatus.PERFECT, record.ClearStatus + 1));

                    InitOrAddDict(stats.SongGenrePlays, record.ChartGenre);
                    if (roundedClearStatus >= (int)EClearStatus.CLEAR) InitOrAddDict(stats.SongGenreClears, record.ChartGenre);
                    if (roundedClearStatus >= (int)EClearStatus.FC) InitOrAddDict(stats.SongGenreFCs, record.ChartGenre);
                    if (roundedClearStatus == (int)EClearStatus.PERFECT) InitOrAddDict(stats.SongGenrePerfects, record.ChartGenre);
                    stats.SongDistinctPlays++;
                    if (roundedClearStatus >= (int)EClearStatus.CLEAR) stats.SongDistinctClears++;
                    if (roundedClearStatus >= (int)EClearStatus.FC) stats.SongDistinctFCs++;
                    if (roundedClearStatus == (int)EClearStatus.PERFECT) stats.SongDistinctPerfects++;
                }
            }

            return stats;
        }
    }
}
