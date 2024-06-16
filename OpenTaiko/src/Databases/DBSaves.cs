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
    internal class DBSaves
    {
        private static string _savesDBFilename = $@"Saves.db3";
        private static string _savesDBPath = @$"{TJAPlayer3.strEXEのあるフォルダ}{_savesDBFilename}";
        private static SqliteConnection SavesDBConnection = new SqliteConnection(@$"Data Source={_savesDBPath}");

        private static string _DBNotFoundError = @$"The database {_savesDBFilename} was not found or the connection failed";

        public static SqliteConnection? GetSavesDBConnection()
        {
            try
            {
                if (SavesDBConnection != null && SavesDBConnection.State == ConnectionState.Closed) SavesDBConnection.Open();
                return SavesDBConnection;
            }
            catch
            {
                LogNotification.PopError(_DBNotFoundError);
                return null;
            }
        }

        public static Int64 GetPlayerSaveId(int player)
        {
            return TJAPlayer3.SaveFileInstances[TJAPlayer3.GetActualPlayer(player)].data.SaveId;
        }

        

        #region [best_plays Table]

        public static Dictionary<string, BestPlayRecords.CBestPlayRecord> GetBestPlaysAsDict(Int64 saveId)
        {
            Dictionary<string, BestPlayRecords.CBestPlayRecord> _bestPlays = new Dictionary<string, BestPlayRecords.CBestPlayRecord>();
            SqliteConnection? connection = GetSavesDBConnection();
            if (connection == null) return _bestPlays;

            var command = connection.CreateCommand();
            command.CommandText =
            @$"
                    SELECT *
                    FROM best_plays
                    WHERE SaveId={saveId};
                ";
            SqliteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                BestPlayRecords.CBestPlayRecord record = new BestPlayRecords.CBestPlayRecord();

                record.ChartUniqueId = (string)reader["ChartUniqueId"];
                record.ChartGenre = (string)reader["ChartGenre"];
                record.Charter = (string)reader["Charter"];
                record.Artist = (string)reader["Artist"];
                record.PlayMods = (Int64)reader["PlayMods"];
                record.ChartDifficulty = (Int64)reader["ChartDifficulty"]; ;
                record.ChartLevel = (Int64)reader["ChartLevel"];
                record.ClearStatus = (Int64)reader["ClearStatus"];
                record.ScoreRank = (Int64)reader["ScoreRank"];
                record.HighScore = (Int64)reader["HighScore"];
                record.TowerBestFloor = (Int64)reader["TowerBestFloor"];
                record.DanExam1 = JsonConvert.DeserializeObject<List<int>>((string)reader["DanExam1"] ?? "[]") ?? new List<int>();
                record.DanExam2 = JsonConvert.DeserializeObject<List<int>>((string)reader["DanExam2"] ?? "[]") ?? new List<int>();
                record.DanExam3 = JsonConvert.DeserializeObject<List<int>>((string)reader["DanExam3"] ?? "[]") ?? new List<int>();
                record.DanExam4 = JsonConvert.DeserializeObject<List<int>>((string)reader["DanExam4"] ?? "[]") ?? new List<int>();
                record.DanExam5 = JsonConvert.DeserializeObject<List<int>>((string)reader["DanExam5"] ?? "[]") ?? new List<int>();
                record.DanExam6 = JsonConvert.DeserializeObject<List<int>>((string)reader["DanExam6"] ?? "[]") ?? new List<int>();
                record.DanExam7 = JsonConvert.DeserializeObject<List<int>>((string)reader["DanExam7"] ?? "[]") ?? new List<int>();
                record.PlayCount = (Int64)reader["PlayCount"];
                record.HighScoreGoodCount = (Int64)reader["HighScoreGoodCount"];
                record.HighScoreOkCount = (Int64)reader["HighScoreOkCount"];
                record.HighScoreBadCount = (Int64)reader["HighScoreBadCount"];
                record.HighScoreMaxCombo = (Int64)reader["HighScoreMaxCombo"];
                record.HighScoreRollCount = (Int64)reader["HighScoreRollCount"];
                record.HighScoreADLibCount = (Int64)reader["HighScoreADLibCount"];
                record.HighScoreBoomCount = (Int64)reader["HighScoreBoomCount"];

                string key = record.ChartUniqueId + record.ChartDifficulty.ToString() + record.PlayMods.ToString();
                _bestPlays[key] = record;
            }
            reader.Close();

            return _bestPlays;
        }

        public static void RegisterPlay(int player, int clearStatus, int scoreRank)
        {
            SqliteConnection? connection = GetSavesDBConnection();
            if (connection == null) return;

            SaveFile.Data saveData = TJAPlayer3.SaveFileInstances[TJAPlayer3.GetActualPlayer(player)].data;
            BestPlayRecords.CBestPlayRecord currentPlay = new BestPlayRecords.CBestPlayRecord();
            var choosenSong = TJAPlayer3.stageSongSelect.rChoosenSong;
            var choosenDifficulty = TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[player];
            var chartScore = TJAPlayer3.stage演奏ドラム画面.CChartScore[player];
            List<int>[] danResults = new List<int>[7] { new List<int>(), new List<int>(), new List<int>(), new List<int>(), new List<int>(), new List<int>(), new List<int>() };

            // Do not register the play if Dan/Tower and any mod is ON
            if ((choosenDifficulty == (int)Difficulty.Tower || choosenDifficulty == (int)Difficulty.Dan) && !ModIcons.tPlayIsStock(player)) return;

            // 1st step: Init best play record class

            {
                currentPlay.ChartUniqueId = choosenSong.uniqueId.data.id.Replace(@"'", @"''");
                currentPlay.ChartGenre = choosenSong.strジャンル.Replace(@"'", @"''");
                currentPlay.Charter = choosenSong.strNotesDesigner[choosenDifficulty].Replace(@"'", @"''");
                currentPlay.Artist = choosenSong.strサブタイトル.Replace(@"'", @"''"); // There is no direct Artist tag on the .tja format, so we directly use the subtitle as a guess
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
                reader.Close();
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

            // Intermede: Update locally the play on the save file to reload it without requerying the database
            {
                string key = currentPlay.ChartUniqueId + currentPlay.ChartDifficulty.ToString() + currentPlay.PlayMods.ToString();
                saveData.bestPlays[key] = currentPlay;
                saveData.tFactorizeBestPlays();
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

        #endregion
    }
}
