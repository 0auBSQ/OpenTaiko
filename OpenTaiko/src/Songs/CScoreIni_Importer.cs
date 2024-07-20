using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    static class CScoreIni_Importer
    {
        public static string Status { get; private set; } = "";
        private static readonly string langSUBTITLE = "SUBTITLE" + CLangManager.LangInstance.Id.ToUpper();
        public static void ImportScoreInisToSavesDb3()
        {
            Trace.TraceInformation("Importing score.ini files to Saves.db3 database!");

            Status = "Establishing connection to database...";
            SqliteConnection? connection = DBSaves.GetSavesDBConnection();
            if (connection == null)
            {
                Trace.TraceError("Could not establish a connection to Saves.db3 database. Aborting score import.");
                return;
            }

            Status = "Searching for scores...";
            string[] _scoreFiles = Directory.GetFiles(TJAPlayer3.ConfigIni.strSongsPath, "*.score.ini", SearchOption.AllDirectories);
            Trace.TraceInformation($"{_scoreFiles.Length} score.ini files have been found. Beginning import.");

            int importcount = 0;
            Status = "Importing scores...";
            foreach (string _score in _scoreFiles)
            {
                try
                {
                    string directory = Path.GetDirectoryName(_score);
                    DirectoryInfo dir_parent = Directory.GetParent(directory);

                    string[] TJAData = Array.Empty<string>();
                    if (File.Exists(GetTJAFile(_score)))
                        TJAData = File.ReadAllLines(GetTJAFile(_score));
                    string[] Difficulties = TJAData.Where(str => str.StartsWith("COURSE:")).ToArray();
                    string[] Levels = TJAData.Where(str => str.StartsWith("LEVEL:")).ToArray();

                    string[] ScoreData = File.ReadAllLines(_score);

                    CSongUniqueID UniqueId = new CSongUniqueID(Path.Combine(directory, "uniqueID.json"));
                    CBoxDef boxdef = new CBoxDef();
                    if (File.Exists(Path.Combine(dir_parent.FullName, "box.def")))
                        boxdef = new CBoxDef(Path.Combine(dir_parent.FullName, "box.def"));
                    //string Charter = "";
                    string[] Charters = new string[8] { "", "", "", "", "", "", "", "" };
                    string Artist = "";

                    int[] Level = { -1, -1, -1, -1, -1, -1, -1 };
                    int[] Clear = { -1, -1, -1, -1, -1, -1, -1 };
                    int[] Rank = { -1, -1, -1, -1, -1, -1, -1 };
                    int[] HighScore = { 0, 0, 0, 0, 0, 0, 0 };

                    foreach (string data in TJAData)
                    {
                        string[] split = data.Split(":", 2);
                        switch (split[0])
                        {
                            case "SUBTITLE":
                                Artist = split[1].StartsWith("--") || split[1].StartsWith("++") ? split[1].Substring(2) : split[1];
                                break;
                            case "MAKER":
                                Charters[0] = split[1];
                                break;
                            case "NOTESDESIGNER0":
                                Charters[1] = split[1];
                                break;
                            case "NOTESDESIGNER1":
                                Charters[2] = split[1];
                                break;
                            case "NOTESDESIGNER2":
                                Charters[3] = split[1];
                                break;
                            case "NOTESDESIGNER3":
                                Charters[4] = split[1];
                                break;
                            case "NOTESDESIGNER4":
                                Charters[5] = split[1];
                                break;
                            case "NOTESDESIGNER5":
                                Charters[6] = split[1];
                                break;
                            case "NOTESDESIGNER6":
                                Charters[7] = split[1];
                                break;
                            default:
                                if (split[0] == langSUBTITLE)
                                    Artist = split[1];
                                break;
                        }
                    }

                    // Tower/Dan score data is saved in index 0 (Easy).
                    for (int i = 0; i < Difficulties.Length; i++)
                    {
                        int diff_index = Difficulties[i].IndexOf(':') + 1;
                        int lvl_index = Levels[i].IndexOf(":") + 1;

                        int level = int.TryParse(Levels[i].Substring(lvl_index), out int result) ? result : -1;
                        switch (Difficulties[i].Substring(diff_index).ToLower())
                        {
                            case "0":
                            case "easy":
                                Level[0] = level;
                                break;
                            case "1":
                            case "normal":
                                Level[1] = level;
                                break;
                            case "2":
                            case "hard":
                                Level[2] = level;
                                break;
                            case "3":
                            case "oni":
                                Level[3] = level;
                                break;
                            case "4":
                            case "edit":
                                Level[4] = level;
                                break;
                            case "5":
                            case "tower":
                                Level[5] = level;
                                break;
                            case "6":
                            case "dan":
                                Level[6] = level;
                                break;
                        }
                    }

                    foreach (string data in ScoreData)
                    {
                        string[] split = data.Split('=', 2);
                        int num = 0;
                        if (split.Length == 2) num = int.TryParse(split[1], out int result) ? result : 0;
                        switch (split[0])
                        {
                            case "HiScore1":
                                HighScore[0] = num;
                                HighScore[5] = num;
                                HighScore[6] = num;
                                break;
                            case "HiScore2":
                                HighScore[1] = num;
                                break;
                            case "HiScore3":
                                HighScore[2] = num;
                                break;
                            case "HiScore4":
                                HighScore[3] = num;
                                break;
                            case "HiScore5":
                                HighScore[4] = num;
                                break;

                            case "Clear0":
                                Clear[0] = num == 0 ? -1 : num;
                                Clear[5] = num == 0 ? -1 : num;
                                Clear[6] = num == 0 ? -1 : num;
                                break;
                            case "Clear1":
                                Clear[1] = num == 0 ? -1 : num;
                                break;
                            case "Clear2":
                                Clear[2] = num == 0 ? -1 : num;
                                break;
                            case "Clear3":
                                Clear[3] = num == 0 ? -1 : num;
                                break;
                            case "Clear4":
                                Clear[4] = num == 0 ? -1 : num;
                                break;

                            case "ScoreRank0":
                                Rank[0] = num - 1;
                                Rank[5] = num - 1;
                                Rank[6] = num - 1;
                                break;
                            case "ScoreRank1":
                                Rank[1] = num - 1;
                                break;
                            case "ScoreRank2":
                                Rank[2] = num - 1;
                                break;
                            case "ScoreRank3":
                                Rank[3] = num - 1;
                                break;
                            case "ScoreRank4":
                                Rank[4] = num - 1;
                                break;
                        }
                    }

                    for (int i = 0; i < Level.Length; i++)
                    {
                        int score_index = i < 5 ? i : 0;
                        if (Level[i] != -1 && HighScore[score_index] > 0)
                        {
                            SqliteCommand cmd = connection.CreateCommand();
                            cmd.CommandText = $@"
                    INSERT INTO best_plays(ChartUniqueId,ChartGenre,Charter,Artist,PlayMods,ChartDifficulty,ChartLevel,ClearStatus,ScoreRank,HighScore,SaveId,TowerBestFloor,DanExam1,DanExam2,DanExam3,DanExam4,DanExam5,DanExam6,DanExam7,PlayCount,HighScoreGoodCount,HighScoreOkCount,HighScoreBadCount,HighScoreMaxCombo,HighScoreRollCount,HighScoreADLibCount,HighScoreBoomCount)
                       VALUES(
                            '{UniqueId.data.id.Replace(@"'", @"''")}',
                            '{boxdef.Genre}',
                            '{(!string.IsNullOrEmpty(Charters[i + 1]) ? Charters[i + 1] : Charters[0]).Replace(@"'", @"''")}',
                            '{Artist.Replace(@"'", @"''")}',
                            8925478921,
                            {i},
                            {Level[i]},
                            {Clear[i] + (i == 6 && Clear[i] > -1 ? 1 : 0)},
                            {(i != 5 ? Rank[i] : -1)},
                            {HighScore[score_index]},
                            {GetPlayerId(_score)},
                            {(i == 5 ? Rank[i]+1 : 0)},
                            '[-1]',
                            '[-1]',
                            '[-1]',
                            '[-1]',
                            '[-1]',
                            '[-1]',
                            '[-1]',
                            1,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0
                        )
                       ON CONFLICT(ChartUniqueId,ChartDifficulty,PlayMods) DO NOTHING
                ";
                            if (cmd.ExecuteNonQuery() > 0)
                                importcount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning($"Failed to import {_score} into new database. More details:\n{ex}");
                }
            }
            Trace.TraceInformation($"Imported {importcount} of {_scoreFiles.Length} scores from score.ini files.");
            Status = "";
        }

        private static string GetTJAFile(string path)
        {
            FileInfo info = new FileInfo(path);
            
            if (info.FullName.EndsWith("1P.score.ini")) return info.FullName.Replace("1P.score.ini", "");
            if (info.FullName.EndsWith("2P.score.ini")) return info.FullName.Replace("2P.score.ini", "");
            if (info.FullName.EndsWith("3P.score.ini")) return info.FullName.Replace("3P.score.ini", "");
            if (info.FullName.EndsWith("4P.score.ini")) return info.FullName.Replace("4P.score.ini", "");
            if (info.FullName.EndsWith("5P.score.ini")) return info.FullName.Replace("5P.score.ini", "");
            return info.FullName.Replace(".score.ini", "");
        }
        private static int GetPlayerId(string path)
        {
            FileInfo info = new FileInfo(path);

            if (info.Name.EndsWith("1P.score.ini")) return 0;
            if (info.Name.EndsWith("2P.score.ini")) return 1;
            if (info.Name.EndsWith("3P.score.ini")) return 2;
            if (info.Name.EndsWith("4P.score.ini")) return 3;
            if (info.Name.EndsWith("5P.score.ini")) return 4;
            return 0;
        }
    }
}
