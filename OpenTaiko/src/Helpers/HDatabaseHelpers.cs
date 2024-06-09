using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;

namespace TJAPlayer3
{
    public static class HDatabaseHelpers
    {
        private static string _savesDBPath = @$"{TJAPlayer3.strEXEのあるフォルダ}Saves.db3";
        private static SqliteConnection SavesDBConnection = new SqliteConnection(@$"Data Source={_savesDBPath}");

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

        public static SqliteConnection GetSavesDBConnection()
        {
            if (SavesDBConnection != null && SavesDBConnection.State == ConnectionState.Closed) SavesDBConnection.Open();
            return SavesDBConnection;
        }

        public static List<string> GetAvailableLanguage(SqliteConnection connection, string prefix)
        {
            List<string> _translations = new List<string>();
            foreach (string cd in CLangManager.Langcodes)
            {
                SqliteCommand chk = connection.CreateCommand();
                chk.CommandText =
                @$"
                        SELECT count(*) FROM sqlite_master WHERE type='table' AND name='{prefix}_{cd}';
                    ";
                SqliteDataReader chkreader = chk.ExecuteReader();
                while (chkreader.Read())
                {
                    if (chkreader.GetInt32(0) > 0)
                        _translations.Add(cd);
                }
            }
            return _translations;
        }

        public static void RegisterPlay(int player, int clearStatus, int scoreRank)
        {
            SqliteConnection connection = GetSavesDBConnection();
            SaveFile.Data saveData = TJAPlayer3.SaveFileInstances[TJAPlayer3.GetActualPlayer(player)].data;
            CBestPlayRecord currentPlay = new CBestPlayRecord();
            var choosenSong = TJAPlayer3.stageSongSelect.rChoosenSong;
            var choosenDifficulty = TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[player];
            var chartScore = TJAPlayer3.stage演奏ドラム画面.CChartScore[player];
            List<int>[] danResults = new List<int>[7] { new List<int>(), new List<int>(), new List<int>(), new List<int>(), new List<int>(), new List<int>(), new List<int>() };

            // 1st step: Init best play record class

            {
                currentPlay.ChartUniqueId = choosenSong.uniqueId.data.id;
                currentPlay.ChartGenre = choosenSong.strジャンル;
                currentPlay.Charter = choosenSong.strNotesDesigner[choosenDifficulty];
                currentPlay.Artist = choosenSong.strサブタイトル; // There is no direct Artist tag on the .tja format, so we directly use the subtitle as a guess
                currentPlay.PlayMods = ModIcons.tModsToPlayModsFlags(player);
                currentPlay.ChartDifficulty = choosenDifficulty;
                currentPlay.ChartLevel = choosenSong.arスコア[choosenDifficulty].譜面情報.nレベル[choosenDifficulty];
                currentPlay.ClearStatus = clearStatus;
                currentPlay.ScoreRank = scoreRank;
                currentPlay.HighScore = chartScore.nScore;
                if (choosenDifficulty == (int)Difficulty.Tower) currentPlay.TowerBestFloor = CFloorManagement.LastRegisteredFloor;
                if (choosenDifficulty == (int)Difficulty.Dan)
                {
                    for (int i = 0; i < TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs.Count; i++)
                    {
                        for (int j = 0; j < TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[i].Dan_C.Length; j++)
                        {
                            if (TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[i].Dan_C[j] != null)
                            {
                                int amount = TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[i].Dan_C[j].GetAmount();
                                danResults[j].Add(amount);                                
                            }
                        }
                    }
                }
                currentPlay.PlayCount = 1; // Will be directly added to the current instance if exists
                currentPlay.HighScoreGoodCount = chartScore.nGreat;
                currentPlay.HighScoreOkCount = chartScore.nGood;
                currentPlay.HighScoreBadCount = chartScore.nMiss;
                currentPlay.HighScoreMaxCombo = TJAPlayer3.stage演奏ドラム画面.actCombo.n現在のコンボ数.最高値[player];
                currentPlay.HighScoreRollCount = chartScore.nRoll;
                currentPlay.HighScoreADLibCount = chartScore.nADLIB;
                currentPlay.HighScoreBoomCount = chartScore.nMine;
            }

            // 2nd step: Overwrite the instance with best play results if exists 
            {
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText =
                @$"
                        SELECT * FROM best_plays WHERE ChartUniqueId='{currentPlay.ChartUniqueId}' AND PlayMods={currentPlay.PlayMods} and ChartDifficulty={currentPlay.ChartDifficulty};
                    ";
                SqliteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    // Overwrite multiple variables at once if the highscore is replaced
                    Int64 _highscore = (Int64)reader["HighScore"];
                    if (_highscore > currentPlay.HighScore)
                    {
                        currentPlay.HighScore = _highscore;
                        currentPlay.HighScoreGoodCount = (Int64)reader["HighScoreGoodCount"];
                        currentPlay.HighScoreOkCount = (Int64)reader["HighScoreOkCount"];
                        currentPlay.HighScoreBadCount = (Int64)reader["HighScoreBadCount"];
                        currentPlay.HighScoreMaxCombo = (Int64)reader["HighScoreMaxCombo"];
                        currentPlay.HighScoreRollCount = (Int64)reader["HighScoreRollCount"];
                        currentPlay.HighScoreADLibCount = (Int64)reader["HighScoreADLibCount"];
                        currentPlay.HighScoreBoomCount = (Int64)reader["HighScoreBoomCount"];
                    }
                    currentPlay.ClearStatus = Math.Max(currentPlay.ClearStatus, (Int64)reader["ClearStatus"]);
                    currentPlay.ScoreRank = Math.Max(currentPlay.ScoreRank, (Int64)reader["ScoreRank"]);
                    if (choosenDifficulty == (int)Difficulty.Tower) currentPlay.TowerBestFloor = Math.Max(currentPlay.TowerBestFloor, (Int64)reader["TowerBestFloor"]);
                    if (choosenDifficulty == (int)Difficulty.Dan)
                    {
                        List<int>[] oldDanResults = new List<int>[7]
                        {
                            JsonConvert.DeserializeObject<List<int>>((string)reader["DanExam1"]) ?? new List<int> { -1 },
                            JsonConvert.DeserializeObject<List<int>>((string)reader["DanExam2"]) ?? new List<int> { -1 },
                            JsonConvert.DeserializeObject<List<int>>((string)reader["DanExam3"]) ?? new List<int> { -1 },
                            JsonConvert.DeserializeObject<List<int>>((string)reader["DanExam4"]) ?? new List<int> { -1 },
                            JsonConvert.DeserializeObject<List<int>>((string)reader["DanExam5"]) ?? new List<int> { -1 },
                            JsonConvert.DeserializeObject<List<int>>((string)reader["DanExam6"]) ?? new List<int> { -1 },
                            JsonConvert.DeserializeObject<List<int>>((string)reader["DanExam7"]) ?? new List<int> { -1 }
                        };
                        for (int i = 0; i < TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs.Count; i++)
                        {
                            for (int j = 0; j < TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[i].Dan_C.Length; j++)
                            {
                                if (TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[i].Dan_C[j] != null)
                                {
                                    int amount = danResults[j][i];

                                    if (i < oldDanResults[j].Count)
                                    {
                                        int current = oldDanResults[j][i];
                                        if (current == -1)
                                        {
                                            danResults[j][i] = amount;
                                        }
                                        else if (TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[i].Dan_C[j].GetExamRange() == Exam.Range.More)
                                        {
                                            danResults[j][i] = Math.Max(amount, current);
                                        }
                                        else if (TJAPlayer3.stageSongSelect.rChoosenSong.DanSongs[i].Dan_C[j].GetExamRange() == Exam.Range.Less)
                                        {
                                            danResults[j][i] = Math.Min(amount, current);
                                        }
                                    }
                                    
                                }
                            }
                        }
                    }
                }
            }

            // Intermede: Dan results to Dan exams
            {
                if (choosenDifficulty == (int)Difficulty.Dan)
                {
                    currentPlay.DanExam1 = danResults[0];
                    currentPlay.DanExam2 = danResults[1];
                    currentPlay.DanExam3 = danResults[2];
                    currentPlay.DanExam4 = danResults[3];
                    currentPlay.DanExam5 = danResults[4];
                    currentPlay.DanExam6 = danResults[5];
                    currentPlay.DanExam7 = danResults[6];
                }
            }

            // 3rd step: Insert/Update to database
            {
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = $@"
                    INSERT INTO best_plays(ChartUniqueId,ChartGenre,Charter,Artist,PlayMods,ChartDifficulty,ChartLevel,ClearStatus,ScoreRank,HighScore,SaveId,TowerBestFloor,DanExam1,DanExam2,DanExam3,DanExam4,DanExam5,DanExam6,DanExam7,PlayCount,HighScoreGoodCount,HighScoreOkCount,HighScoreBadCount,HighScoreMaxCombo,HighScoreRollCount,HighScoreADLibCount,HighScoreBoomCount)
                       VALUES(
                            '{currentPlay.ChartUniqueId}',
                            '{currentPlay.ChartGenre}',
                            '{currentPlay.Charter}',
                            '{currentPlay.Artist}',
                            {currentPlay.PlayMods},
                            {currentPlay.ChartDifficulty},
                            {currentPlay.ChartLevel},
                            {currentPlay.ClearStatus},
                            {currentPlay.ScoreRank},
                            {currentPlay.HighScore},
                            {saveData.SaveId},
                            {currentPlay.TowerBestFloor},
                            '{@$"[{String.Join(",", currentPlay.DanExam1.ToArray())}]"}',
                            '{@$"[{String.Join(",", currentPlay.DanExam2.ToArray())}]"}',
                            '{@$"[{String.Join(",", currentPlay.DanExam3.ToArray())}]"}',
                            '{@$"[{String.Join(",", currentPlay.DanExam4.ToArray())}]"}',
                            '{@$"[{String.Join(",", currentPlay.DanExam5.ToArray())}]"}',
                            '{@$"[{String.Join(",", currentPlay.DanExam6.ToArray())}]"}',
                            '{@$"[{String.Join(",", currentPlay.DanExam7.ToArray())}]"}',
                            {currentPlay.PlayCount},
                            {currentPlay.HighScoreGoodCount},
                            {currentPlay.HighScoreOkCount},
                            {currentPlay.HighScoreBadCount},
                            {currentPlay.HighScoreMaxCombo},
                            {currentPlay.HighScoreRollCount},
                            {currentPlay.HighScoreADLibCount},
                            {currentPlay.HighScoreBoomCount}
                        )
                       ON CONFLICT(ChartUniqueId,ChartDifficulty,PlayMods) DO UPDATE SET
                            PlayCount=best_plays.PlayCount+1,
                            ClearStatus=EXCLUDED.ClearStatus,
                            ScoreRank=EXCLUDED.ScoreRank,
                            HighScore=EXCLUDED.HighScore,
                            HighScoreGoodCount=EXCLUDED.HighScoreGoodCount,
                            HighScoreOkCount=EXCLUDED.HighScoreOkCount,
                            HighScoreBadCount=EXCLUDED.HighScoreBadCount,
                            HighScoreMaxCombo=EXCLUDED.HighScoreMaxCombo,
                            HighScoreRollCount=EXCLUDED.HighScoreRollCount,
                            HighScoreADLibCount=EXCLUDED.HighScoreADLibCount,
                            HighScoreBoomCount=EXCLUDED.HighScoreBoomCount,
                            TowerBestFloor=EXCLUDED.TowerBestFloor,
                            DanExam1=EXCLUDED.DanExam1,
                            DanExam2=EXCLUDED.DanExam2,
                            DanExam3=EXCLUDED.DanExam3,
                            DanExam4=EXCLUDED.DanExam4,
                            DanExam5=EXCLUDED.DanExam5,
                            DanExam6=EXCLUDED.DanExam6,
                            DanExam7=EXCLUDED.DanExam7
                ";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
