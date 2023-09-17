using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static SevenZip.Compression.LZMA.SevenZipHelper;

namespace TJAPlayer3
{
    class CSongReplay
    {
        /* Game version used for the replay
         * 521 = 0.5.2.1
         * 530 = 0.5.3
         * 531 = 0.5.3.1
         * 540 = 0.5.4
         * 600 = 0.6.0
         * 700 = 0.7.0
         * 1000 = 1.0.0
         */
        public int STORED_GAME_VERSION = 600;
        public string REPLAY_FOLDER_NAME = "Replay";

        /* Mod Flags
         * Bit Offsets (Values) :
         * - 0 (1) : Mirror
         * - 1 (2) : Random (Kimagure)
         * - 2 (4) : Super Random (Detarame)
         * - 3 (8) : Invisible (Doron) 
         * - 4 (16) : Perfect memory (Stealth)
         * - 5 (32) : Avalanche
         * - 6 (64) : Minesweeper
         * - 7 (128) : Just (Ok => Bad)
         * - 8 (256) : Safe (Bad => Ok)
        */
        [Flags]
        public enum EModFlag
        {
            None = 0,
            Mirror = 1 << 0,
            Random = 1 << 1,
            SuperRandom = 1 << 2,
            Invisible = 1 << 3,
            PerfectMemory = 1 << 4,
            Avalanche = 1 << 5,
            Minesweeper = 1 << 6,
            Just = 1 << 7,
            Safe = 1 << 8
        }

        public CSongReplay()
        {
            replayFolder = "";
            storedPlayer = 0;
        }

        public CSongReplay(string ChartPath, int player)
        {
            string _chartFolder = Path.GetDirectoryName(ChartPath);
            replayFolder = Path.Combine(_chartFolder, REPLAY_FOLDER_NAME);

            try
            {
                Directory.CreateDirectory(replayFolder);

                Console.WriteLine("Folder Path: " + replayFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }

            storedPlayer = player;
        }

        public void tRegisterInput(double timestamp, byte keypress)
        {
            allInputs.Add(Tuple.Create(timestamp, keypress));
        }

        #region [Dan methods]
        
        public void tDanRegisterSongCount(int songCount)
        {
            DanSongCount = songCount;
            IndividualGoodCount = new int[songCount];
            IndividualOkCount = new int[songCount];
            IndividualBadCount = new int[songCount];
            IndividualRollCount = new int[songCount];
            IndividualMaxCombo = new int[songCount];
            IndividualBoomCount = new int[songCount];
            IndividualADLibCount = new int[songCount];
            IndividualScore = new int[songCount];
        }

        public void tDanInputSongResults(int songNo)
        {
            if (songNo >= DanSongCount) return;
            if (songNo < 0) return;
            IndividualGoodCount[songNo] = TJAPlayer3.stage演奏ドラム画面.n良[songNo];
            IndividualOkCount[songNo] = TJAPlayer3.stage演奏ドラム画面.n可[songNo];
            IndividualBadCount[songNo] = TJAPlayer3.stage演奏ドラム画面.n不可[songNo];
            IndividualRollCount[songNo] = TJAPlayer3.stage演奏ドラム画面.n連打[songNo];
            IndividualMaxCombo[songNo] = TJAPlayer3.stage演奏ドラム画面.nHighestCombo[songNo];
            IndividualBoomCount[songNo] = TJAPlayer3.stage演奏ドラム画面.nMine[songNo];
            IndividualADLibCount[songNo] = TJAPlayer3.stage演奏ドラム画面.nADLIB[songNo];
            danAccumulatedScore = 0;
            for (int acc = 0; acc < songNo; acc++) danAccumulatedScore += IndividualScore[acc];
            IndividualScore[songNo] = (int)TJAPlayer3.stage演奏ドラム画面.actScore.GetScore(0) - danAccumulatedScore;
        }

        #endregion

        #region [Load methods]

        private List<Tuple<double, byte>> ConvertByteArrayToTupleList(byte[] byteArray)
        {
            List<Tuple<double, byte>> tupleList = new List<Tuple<double, byte>>();

            for (int i = 0; i < byteArray.Length; i += sizeof(double) + sizeof(byte))
            {
                double doubleValue = BitConverter.ToDouble(byteArray, i);
                byte byteValue = byteArray[i + sizeof(double)];
                tupleList.Add(Tuple.Create(doubleValue, byteValue));
            }

            return tupleList;
        }

        public void tLoadReplayFile(string optkrFilePath)
        {
            try
            {
                using (FileStream fileStream = new FileStream(optkrFilePath, FileMode.Open))
                {
                    using (BinaryReader reader = new BinaryReader(fileStream))
                    {
                        GameMode = reader.ReadByte();
                        GameVersion = reader.ReadInt32();
                        ChartChecksum = reader.ReadString();
                        PlayerName = reader.ReadString();
                        GoodCount = reader.ReadInt32();
                        OkCount = reader.ReadInt32();
                        BadCount = reader.ReadInt32();
                        RollCount = reader.ReadInt32();
                        MaxCombo = reader.ReadInt32();
                        BoomCount = reader.ReadInt32();
                        ADLibCount = reader.ReadInt32();
                        Score = reader.ReadInt32();
                        CoinValue = reader.ReadInt16();
                        ReachedFloor = reader.ReadInt32();
                        RemainingLives = reader.ReadInt32();
                        DanSongCount = reader.ReadInt32();
                        for (int i = 0; i < DanSongCount; i++)
                        {
                            IndividualGoodCount[i] = reader.ReadInt32();
                            IndividualOkCount[i] = reader.ReadInt32();
                            IndividualBadCount[i] = reader.ReadInt32();
                            IndividualRollCount[i] = reader.ReadInt32();
                            IndividualMaxCombo[i] = reader.ReadInt32();
                            IndividualBoomCount[i] = reader.ReadInt32();
                            IndividualADLibCount[i] = reader.ReadInt32();
                            IndividualScore[i] = reader.ReadInt32();
                        }
                        ClearStatus = reader.ReadByte();
                        ScoreRank = reader.ReadByte();
                        ScrollSpeedValue = reader.ReadInt32();
                        SongSpeedValue = reader.ReadInt32();
                        JudgeStrictnessAdjust = reader.ReadInt32();
                        ModFlags = reader.ReadInt32();
                        GaugeType = reader.ReadByte();
                        GaugeFill = reader.ReadSingle();
                        Timestamp = reader.ReadInt64();
                        CompressedInputsSize = reader.ReadInt32();
                        CompressedInputs = reader.ReadBytes(CompressedInputsSize);
                        var uncomp = SevenZip.Compression.LZMA.SevenZipHelper.Decompress(CompressedInputs);
                        allInputs = ConvertByteArrayToTupleList(uncomp);
                        ChartUniqueID = reader.ReadString();
                        ChartDifficulty = reader.ReadByte();
                        ChartLevel = reader.ReadByte();
                        OnlineScoreID = reader.ReadInt64();
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        #endregion

        #region [Save methods]

        private byte[] ConvertTupleListToByteArray(List<Tuple<double, byte>> tupleList)
        {
            List<byte> byteArray = new List<byte>();

            foreach (var tuple in tupleList)
            {
                byte[] doubleBytes = BitConverter.GetBytes(tuple.Item1);
                byteArray.AddRange(doubleBytes);
                byteArray.Add(tuple.Item2);
            }

            return byteArray.ToArray();
        }

        public void tSaveReplayFile()
        {
            string _path = replayFolder + @"/Replay_" + ChartUniqueID + @"_" + PlayerName + @"_" + Timestamp.ToString() + @".optkr";

            try
            {
                using (FileStream fileStream = new FileStream(_path, FileMode.Create))
                {
                    using (BinaryWriter writer = new BinaryWriter(fileStream))
                    {
                        writer.Write(GameMode);
                        writer.Write(GameVersion);
                        writer.Write(ChartChecksum);
                        writer.Write(PlayerName);
                        writer.Write(GoodCount);
                        writer.Write(OkCount);
                        writer.Write(BadCount);
                        writer.Write(RollCount);
                        writer.Write(MaxCombo);
                        writer.Write(BoomCount);
                        writer.Write(ADLibCount);
                        writer.Write(Score);
                        writer.Write(CoinValue);
                        writer.Write(ReachedFloor);
                        writer.Write(RemainingLives);
                        writer.Write(DanSongCount);
                        for (int i = 0; i < DanSongCount; i++)
                        {
                            writer.Write(IndividualGoodCount[i]);
                            writer.Write(IndividualOkCount[i]);
                            writer.Write(IndividualBadCount[i]);
                            writer.Write(IndividualRollCount[i]);
                            writer.Write(IndividualMaxCombo[i]);
                            writer.Write(IndividualBoomCount[i]);
                            writer.Write(IndividualADLibCount[i]);
                            writer.Write(IndividualScore[i]);
                        }
                        writer.Write(ClearStatus);
                        writer.Write(ScoreRank);
                        writer.Write(ScrollSpeedValue);
                        writer.Write(SongSpeedValue);
                        writer.Write(JudgeStrictnessAdjust);
                        writer.Write(ModFlags);
                        writer.Write(GaugeType);
                        writer.Write(GaugeFill);
                        writer.Write(Timestamp);
                        writer.Write(CompressedInputsSize);
                        writer.Write(CompressedInputs);
                        writer.Write(ChartUniqueID);
                        writer.Write(ChartDifficulty);
                        writer.Write(ChartLevel);
                        writer.Write(OnlineScoreID);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        public void tResultsRegisterReplayInformations(int Coins, int Clear, int SRank)
        {
            // Actual player (Used for saved informations)
            int actualPlayer = TJAPlayer3.GetActualPlayer(storedPlayer);

            // Game mode
            switch (TJAPlayer3.stage選曲.n確定された曲の難易度[0])
            {
                case (int)Difficulty.Dan:
                    GameMode = 1;
                    break;
                case (int)Difficulty.Tower:
                    GameMode = 2;
                    break;
                default:
                    GameMode = 0;
                    break;
            }
            // Game version
            GameVersion = STORED_GAME_VERSION;
            // Chart Checksum (temporary)
            ChartChecksum = "";
            // Player Name
            PlayerName = TJAPlayer3.SaveFileInstances[actualPlayer].data.Name;
            // Performance informations
            GoodCount = TJAPlayer3.stage演奏ドラム画面.CChartScore[storedPlayer].nGreat;
            OkCount = TJAPlayer3.stage演奏ドラム画面.CChartScore[storedPlayer].nGood;
            BadCount = TJAPlayer3.stage演奏ドラム画面.CChartScore[storedPlayer].nMiss;
            RollCount = TJAPlayer3.stage演奏ドラム画面.GetRoll(storedPlayer);
            MaxCombo = TJAPlayer3.stage演奏ドラム画面.actCombo.n現在のコンボ数.最高値[storedPlayer];
            BoomCount = TJAPlayer3.stage演奏ドラム画面.CChartScore[storedPlayer].nMine;
            ADLibCount = TJAPlayer3.stage演奏ドラム画面.CChartScore[storedPlayer].nADLIB;
            Score = TJAPlayer3.stage演奏ドラム画面.CChartScore[storedPlayer].nScore;
            CoinValue = (short)Coins;
            // Tower parameters
            if (GameMode == 2)
            {
                ReachedFloor = CFloorManagement.LastRegisteredFloor;
                RemainingLives = CFloorManagement.CurrentNumberOfLives;
            }
            // Clear status
            ClearStatus = (byte)Clear;
            // Score rank
            ScoreRank = (byte)SRank;
            // Scroll speed value (as on ConfigIni, 9 is x1)
            ScrollSpeedValue = TJAPlayer3.ConfigIni.nScrollSpeed[actualPlayer];
            // Song speed value (as on ConfigIni, 20 is x1)
            SongSpeedValue = TJAPlayer3.ConfigIni.n演奏速度;
            // Just strictess adjust mod value (as on ConfigIni, between -2 for lenient and 2 for rigorous)
            JudgeStrictnessAdjust = TJAPlayer3.ConfigIni.nTimingZones[actualPlayer];

            /* Mod Flags
             * Bit Offsets (Values) :
             * - 0 (1) : Mirror
             * - 1 (2) : Random (Kimagure)
             * - 2 (4) : Super Random (Detarame)
             * - 3 (8) : Invisible (Doron) 
             * - 4 (16) : Perfect memory (Stealth)
             * - 5 (32) : Avalanche
             * - 6 (64) : Minesweeper
             * - 7 (128) : Just (Ok => Bad)
             * - 8 (256) : Safe (Bad => Ok)
             */
            ModFlags = (int)EModFlag.None;
            if (TJAPlayer3.ConfigIni.eRandom[actualPlayer] == Eランダムモード.MIRROR) ModFlags |= (int)EModFlag.Mirror;
            if (TJAPlayer3.ConfigIni.eRandom[actualPlayer] == Eランダムモード.RANDOM) ModFlags |= (int)EModFlag.Random;
            if (TJAPlayer3.ConfigIni.eRandom[actualPlayer] == Eランダムモード.SUPERRANDOM) ModFlags |= (int)EModFlag.SuperRandom;
            if (TJAPlayer3.ConfigIni.eRandom[actualPlayer] == Eランダムモード.HYPERRANDOM) ModFlags |= ((int)EModFlag.Random | (int)EModFlag.Mirror);
            if (TJAPlayer3.ConfigIni.eSTEALTH[actualPlayer] == Eステルスモード.DORON) ModFlags |= (int)EModFlag.Invisible;
            if (TJAPlayer3.ConfigIni.eSTEALTH[actualPlayer] == Eステルスモード.STEALTH) ModFlags |= (int)EModFlag.PerfectMemory;
            if (TJAPlayer3.ConfigIni.nFunMods[actualPlayer] == EFunMods.AVALANCHE) ModFlags |= (int)EModFlag.Avalanche;
            if (TJAPlayer3.ConfigIni.nFunMods[actualPlayer] == EFunMods.MINESWEEPER) ModFlags |= (int)EModFlag.Minesweeper;
            if (TJAPlayer3.ConfigIni.bJust[actualPlayer] == 1) ModFlags |= (int)EModFlag.Just;
            if (TJAPlayer3.ConfigIni.bJust[actualPlayer] == 2) ModFlags |= (int)EModFlag.Safe;
            /* Gauge type
             * - 0 : Normal
             * - 1 : Hard
             * - 2 : Extreme
             */
            var chara = TJAPlayer3.Tx.Characters[TJAPlayer3.SaveFileInstances[actualPlayer].data.Character];
            GaugeType = (byte)HGaugeMethods.tGetGaugeTypeEnum(chara.effect.Gauge);
            // Gauge fill value
            GaugeFill = (float)TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[storedPlayer];
            // Generation timestamp (in ticks)
            Timestamp = DateTime.Now.Ticks;
            // Compressed inputs and size
            byte[] barr = ConvertTupleListToByteArray(allInputs);
            CompressedInputs = SevenZip.Compression.LZMA.SevenZipHelper.Compress(barr);
            CompressedInputsSize = CompressedInputs.Length;
            // Chart metadata
            ChartUniqueID = TJAPlayer3.stage選曲.r確定された曲.uniqueId.data.id;
            ChartDifficulty = (byte)TJAPlayer3.stage選曲.n確定された曲の難易度[storedPlayer];
            ChartLevel = (byte)Math.Min(255, TJAPlayer3.stage選曲.r確定された曲.arスコア[ChartDifficulty].譜面情報.nレベル[ChartDifficulty]);
            // Online score ID used for online leaderboards linking, given by the server (Defaulted to 0 for now)
            OnlineScoreID = 0;
            // Replay Checksum (Calculate at the end)
            ReplayChecksum = "";
        }

        #endregion

        #region [Helper variables]

        private string chartPath;
        private string replayFolder;
        private int storedPlayer;
        private int danAccumulatedScore = 0;

        private List<Tuple<double, byte>> allInputs = new List<Tuple<double, byte>>();

        #endregion

        #region [Replay file variables]

        /* Game mode of the replay
         * 0 = Regular
         * 1 = Dan
         * 2 = Tower
         */
        public byte GameMode = 0;
        // Game version used for the replay
        public int GameVersion;
        // MD5 checksum of the chart
        public string ChartChecksum;
        // Player name
        public string PlayerName;
        // Replay hash
        public string ReplayChecksum;
        /* Performance informations
         * - Good count (Int)
         * - Ok count (Int)
         * - Bad count (Int)
         * - Roll count (Int)
         * - Max combo (Int)
         * - Boom count (Int)
         * - ADLib count (Int)
         * - Score (Int)
         * - Coin value of the play (Short)
         */
        public int GoodCount;
        public int OkCount;
        public int BadCount;
        public int RollCount;
        public int MaxCombo;
        public int BoomCount;
        public int ADLibCount;
        public int Score;
        public short CoinValue;
        /* Performance informations (Tower only)
         * - Reached floor (Int)
         * - Remaining lives (Int)
         */
        public int ReachedFloor = 0;
        public int RemainingLives = 0;
        // Individual performance informations (Dan only)
        public int DanSongCount = 0;
        public int[] IndividualGoodCount;
        public int[] IndividualOkCount;
        public int[] IndividualBadCount;
        public int[] IndividualRollCount;
        public int[] IndividualMaxCombo;
        public int[] IndividualBoomCount;
        public int[] IndividualADLibCount;
        public int[] IndividualScore;
        /* Clear status
         * - Regular :
         *  > 0 : Failed (None)
         *  > 1 : Assisted clear (Bronze) 
         *  > 2 : Clear (Silver)
         *  > 3 : Full combo (Gold)
         *  > 4 : Perfect (Platinum / Rainbow)
         * - Tower :
         *  > 0 : None
         *  > 1 : 10% Mark (初)
         *  > 2 : 25% Mark (低)
         *  > 3 : 50% Mark (中)
         *  > 4 : 75% Mark (高)
         *  > 5 : Assisted clear (Bronze 可)
         *  > 6 : Clear (Silver 良)
         *  > 7 : Full combo (Gold 優)
         *  > 8 : Perfect (Platinum / Rainbow 秀)
         *  - Dan :
         *  > 0 : Failed - No dan title
         *  > 1 : Assisted Red clear - No dan title
         *  > 2 : Assisted Gold clear - No dan title
         *  > 3 : Red clear - Dan title
         *  > 4 : Gold clear - Dan title
         *  > 5 : Red full combo - Dan title
         *  > 6 : Gold full combo - Dan title
         *  > 7 : Red perfect - Dan title
         *  > 8 : Gold perfect - Dan title
         */
        public byte ClearStatus;
        /* Score Rank (Regular only)
         * - 0 : F (Under 500k, Press F for respects)
         * - 1 : E (500k ~ Under 600k, Ew...)
         * - 2 : D (600k ~ Under 700k, Disappointing)
         * - 3 : C (700k ~ Under 800k, Correct)
         * - 4 : B (800k ~ Under 900k, Brillant!)
         * - 5 : A (900k ~ Under 950k, Amazing!)
         * - 6 : S (950k and more, Splendiferous!!)
         * - 7 : Ω ((Around) 1M and more, Ωut-of-this-world!!!)
         */
        public byte ScoreRank;
        // Scroll speed value (as on ConfigIni, 9 is x1)
        public int ScrollSpeedValue;
        // Song speed value (as on ConfigIni, 20 is x1)
        public int SongSpeedValue;
        // Just strictess adjust mod value (as on ConfigIni, between -2 for lenient and 2 for rigorous)
        public int JudgeStrictnessAdjust;
        /* Mod Flags
         * Bit Offsets (Values) :
         * - 0 (1) : Mirror
         * - 1 (2) : Random (Kimagure)
         * - 2 (4) : Super Random (Detarame)
         * - 3 (8) : Invisible (Doron) 
         * - 4 (16) : Perfect memory (Stealth)
         * - 5 (32) : Avalanche
         * - 6 (64) : Minesweeper
         * - 7 (128) : Just (Ok => Bad)
         * - 8 (256) : Safe (Bad => Ok)
         */
        public int ModFlags;
        /* Gauge type
         * - 0 : Normal
         * - 1 : Hard
         * - 2 : Extreme
         */
        public byte GaugeType;
        // Gauge fill value
        public float GaugeFill;
        // Generation timestamp (in ticks)
        public long Timestamp;
        // Size in bytes of the compressed inputs (replay data) array
        public int CompressedInputsSize;
        // Compressed inputs (replay data)
        public byte[] CompressedInputs;
        /* Chart metadata
         * - Chart unique ID : String
         * - Chart difficulty : Byte (Between 0 and 6)
         * - Chart level : Byte (Rounded to 255, usually between 0 and 13)
         */
        public string ChartUniqueID;
        public byte ChartDifficulty;
        public byte ChartLevel;
        // Online score ID used for online leaderboards linking, given by the server
        public long OnlineScoreID;

        #endregion
    }
}
