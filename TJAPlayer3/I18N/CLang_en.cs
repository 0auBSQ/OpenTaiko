using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FDK;

namespace TJAPlayer3
{
    internal class CLang_en : ILang
    {
        string ILang.GetString(int idx)
        {
            if (!dictionnary.ContainsKey(idx))
                return "[!] Index not found in dictionary";

            return dictionnary[idx];
        }


        private static readonly Dictionary<int, string> dictionnary = new Dictionary<int, string>
        {
            [0] = "Change the displayed language\ningame and within the menus.",
            [1] = "System language",
            [2] = "<< Return to Menu",
            [3] = "Return to left menu.",
            [4] = "Reload song data",
            [5] = "Retrieve and update the song list.",
            [6] = "Player count",
            [7] = "Change the ingame player countF\nSetting it to 2 makes able to play\nregular charts at 2 players by splitting \nthe screen in half.",
            [8] = "Risky",
            [9] = "Risky mode:\nSet it over 1, in case you'd like to specify\n the number of Poor/Miss times to be\n FAILED.\nSet 0 to disable Risky mode.",
            [10] = "Song speed",
            [11] = "It changes the song speed.\n" +
                "For example, you can play in half\n" +
                " speed by setting PlaySpeed = 0.500\n" +
                " for your practice.\n" +
                "\n" +
                "Note: It also changes the songs' pitch.\n" +
                "In case TimeStretch=ON, some audio\n" +
                "lag occurs if slower than x0.900.",
            [16] = "Layout type",
            [17] = "You can change the layout of the songs \ndisplayed on the song select screen.\n" +
                "0 : Regular (Up to down diagonal)\n" +
                "1 : Vertical\n" +
                "2 : Down to up diagonal\n" +
                "3 : Half-circle facing right\n" +
                "4 : Half-circle facing left",
            [18] = "How to change the playing speed:\n" +
                "Turn ON to use time stretch\n" +
                "to change the play speed." +
                "\n" +
                "If you set TimeStretch=ON, it usese\n" +
                "more CPU power. And some sound\n" +
                "lag occurs slower than x0.900.",
            [19] = "Fullscreen mode or window mode.",
            [20] = "Turn OFF if you don't want to encount\n GAME OVER.",
            [21] = "Turn ON to use child BOX (subfolders)\n at RANDOM SELECT.",
            [22] = "Turn ON to wait VSync (Vertical\n Synchronizing signal) at every\n drawings. (so FPS becomes 60)\nIf you have enough CPU/GPU power,\n the scroll would become smooth.",
            [23] = "To use AVI playback or not.",
            [24] = "To draw BGA (back ground animations)\n or not.",
            [25] = "Delay time(ms) to start playing preview\n sound in SELECT MUSIC screen.\nYou can specify from 0ms to 10000ms.",
            [26] = "Delay time(ms) to show preview image\n in SELECT MUSIC screen.\nYou can specify from 0ms to 10000ms.",
            [27] = "To show song informations on playing\n BGA area. (FPS, BPM, total time etc)\nYou can ON/OFF the indications\n by pushing [Del] while playing drums",
            [28] = "The degree for transparing playing\n screen and wallpaper.\n\n0=completely transparent,\n255=no transparency",
            [29] = "Turn OFF if you don't want to play\n BGM.",
            [30] = "To save high-scores/skills, turn it ON.\nTurn OFF in case your song data are\n in read-only media (CD-ROM etc).\nNote that the score files also contain\n 'BGM Adjust' parameter. So if you\n want to keep adjusting parameter,\n you need to set SaveScore=ON.",
            [31] = "To apply BS1770GAIN loudness\nmetadata when playing songs, turn it ON.\nTurn OFF if you prefer to use only\nthe main song level controls.\nIt needs BS1770GAIN.",
            [32] = "When applying BS1770GAIN loudness\nmetadata while playing songs, song levels\nwill be adjusted to target this loudness,\nmeasured in cB (centibels) relative to full scale.\n",
            [33] = "To apply .tja SONGVOL properties when playing\nsongs, turn it ON. Turn OFF if you prefer to\nuse only the main song level controls.",
            [34] = $"The level adjustment for sound effects.\nYou can specify from {CSound.MinimumGroupLevel} to {CSound.MaximumGroupLevel}%.",
            [35] = $"The level adjustment for voices.\nYou can specify from {CSound.MinimumGroupLevel} to {CSound.MaximumGroupLevel}%.",
            [36] = $"The level adjustment for songs during gameplay.\nYou can specify from {CSound.MinimumGroupLevel} to {CSound.MaximumGroupLevel}%.",
            [37] = "The amount of sound level change for each press\nof a sound level control key.\nYou can specify from 1 to 20.",
            [38] = "Blank time before music source to play. (ms)\n",
            [39] = "AutoSaveResult:\nTurn ON to save your result screen\n image automatically when you get\n hiscore/hiskill.",
            [40] = "Share Playing .tja file infomation on Discord.",
            [41] = "To select joystick input method.\n\nON to use buffer input. No lost/lags.\nOFF to use realtime input. It may\n causes lost/lags for input.\n Moreover, input frequency is\n synchronized with FPS.",
            [42] = "Turn ON to put debug log to\n DTXManiaLog.txt\nTo take it effective, you need to\n re-open DTXMania.",
            [43] = "Sound output type:\n" +
                "You can choose WASAPI, ASIO or\n" +
                "DShow(DirectShow).\n" +
                "WASAPI can use only after Vista.\n" +
                "ASIO can use on the\n" +
                "\"ASIO-supported\" sound device.\n" +
                "You should use WASAPI or ASIO\n" +
                "to decrease the sound lag.\n" +
                "\n" +
                "Note: Exit CONFIGURATION to make\n" +
                "     the setting take effect.",
            [44] = "Sound buffer size for WASAPI:\n" +
                "You can set from 0 to 99999ms.\n" +
                "Set 0 to use a default sysytem\n" +
                "buffer size.\n" +
                "Smaller value makes smaller lag,\n" +
                "but it may cause sound troubles.\n" +
                "\n" +
                "Note: Exit CONFIGURATION to make\n" +
                "     the setting take effect.",
            [45] = "ASIO device:\n" +
                    "You can choose the sound device\n" +
                    "used with ASIO.\n" +
                    "\n" +
                    "Note: Exit CONFIGURATION to make\n" +
                "     the setting take effect.",
            [46] = "Use OS Timer or not:\n" +
                "If this settings is ON, DTXMania uses\n" +
                "OS Standard timer. It brings smooth\n" +
                "scroll, but may cause some sound lag.\n" +
                "(so AdjustWaves is also avilable)\n" +
                "\n" +
                "If OFF, DTXMania uses its original\n" +
                "timer and the effect is vice versa.\n" +
                "\n" +
                "This settings is avilable only when\n" +
                "you uses WASAPI/ASIO.\n",
            [47] = "Show Character Images.\n",
            [48] = "Show Dancer Images.\n",
            [49] = "Show Mob Images.\n",
            [50] = "Show Runner Images.\n",
            [51] = "Show Footer Image.\n",
            [52] = "Use pre-textures render.\n",
            [53] = "Show PuchiChara Images.\n",
            [54] = "Skin:\n" +
                "Change skin.",
            [55] = "Settings for the system key/pad inputs.",
            [56] = "AUTO PLAY",
            [57] = "To play P1 Taiko\n" +
                " automatically.",
            [58] = "AUTO PLAY 2P",
            [59] = "To play P2 Taiko\n" +
                " automatically.",
            [60] = "AUTO Roll",
            [61] = "If OFF the drumrolls\n" +
                    "aren't played by auto.",
            [62] = "ScrollSpeed",
            [63] = "To change the scroll speed for the\n" +
                "drums lanes.\n" +
                "You can set it from x0.1 to x200.0.\n" +
                "(ScrollSpeed=x0.5 means half speed)",
            [64] = "Risky",
            [65] = "Risky mode:\n" +
                "Set over 1, in case you'd like to specify\n" +
                " the number of Poor/Miss times to be\n" +
                " FAILED.\n" +
                "Set 0 to disable Risky mode.",
            [66] = "Random",
            [67] = "Notes come randomly.\n\n Part: swapping lanes randomly for each\n  measures.\n Super: swapping chip randomly\n Hyper: swapping randomly\n  (number of lanes also changes)",
            [68] = "Stealth",
            [69] = "DORON:Hidden for NoteImage.\n" +
                "STEALTH:Hidden for NoteImage and SeNotes",
            [70] = "NoInfo",
            [71] = "Hide the song informations.\n",
            [72] = "JUST",
            [73] = "Allow only GOODs, making OKs becoming\n" +
                    "BADs.",
            [74] = "Tight",
            [75] = "It becomes MISS to hit pad without\n" +
                " chip.",
            [76] = "D-MinCombo",
            [77] = "Initial number to show the combo\n" +
                " for the drums.\n" +
                "You can specify from 1 to 99999.",
            [78] = "InputAdjust",
            [79] = "To adjust the input timing.\n" +
                "You can set from -99 to 99ms.\n" +
                "To decrease input lag, set minus value.",
            [80] = "DefaultCourse",
            [81] = "Difficulty selected by default\n",
            [82] = "ScoreMode",
            [83] = "Score calculation method\n" +
                    "TYPE-A: Old allotment\n" +
                    "TYPE-B: Old case allotment\n" +
                    "TYPE-C: New allotment\n",
            [84] = "Turn on fixed score mode.",
            [85] = "BranchGuide",
            [86] = "Display the referenced value for branches.\n" +
                    "Not effective with auto.",
            [87] = "BranchAnime",
            [88] = "Branch animation type\n" +
                    "TYPE-A: Taiko 7-14\n" +
                    "TYPE-B: Taiko 15+\n" +
                    " \n",
            [89] = "GameMode",
            [90] = "Game mode:\n" +
                    "(Not avaliable for 2P mode)\n" +
                    "TYPE-A: 完走!叩ききりまショー!\n" +
                    "TYPE-B: 完走!叩ききりまショー!(激辛)\n" +
                    " \n",
            [91] = "BigNotesJudge",
            [92] = "Require to hit both side for big notes.",
            [93] = "JudgeCountDisp",
            [94] = "Show the JudgeCount\n" +
                "(SinglePlay Only)",
            [95] = "KEY CONFIG",
            [96] = "Settings for the drums key/pad inputs.",
            [97] = "Capture",
            [98] = "Capture key assign:\nTo assign key for screen capture.\n (You can use keyboard only. You can't\nuse pads to capture screenshot.",
            [99] = "LeftRed",
            [10000] = "Drums key assign:\nTo assign key/pads for LeftRed\n button.",
            [10001] = "RightRed",
            [10002] = "Drums key assign:\nTo assign key/pads for RightRed\n button.",
            [10003] = "LeftBlue",
            [10004] = "Drums key assign:\nTo assign key/pads for LeftBlue\n button.",
            [10005] = "RightBlue",
            [10006] = "Drums key assign:\nTo assign key/pads for RightBlue\n button.",
            [10007] = "LeftRed2P",
            [10008] = "Drums key assign:\nTo assign key/pads for RightCymbal\n button.",
            [10009] = "RightRed2P",
            [10010] = "Drums key assign:\nTo assign key/pads for RightRed2P\n button.",
            [10011] = "LeftBlue2P",
            [10012] = "Drums key assign:\nTo assign key/pads for LeftBlue2P\n button.",
            [10013] = "RightBlue2P",
            [10014] = "Drums key assign:\nTo assign key/pads for RightBlue2P\n button.",
            [10018] = "TimeStretch",
            [10019] = "Fullscreen",
            [10020] = "StageFailed",
            [10021] = "RandSubBox",
            [10022] = "VSyncWait",
            [10023] = "AVI",
            [10024] = "BGA",
            [10025] = "PreSoundWait",
            [10026] = "PreImageWait",
            [10027] = "Debug Info",
            [10028] = "BG Alpha",
            [10029] = "BGM Sound",
            [10030] = "SaveScore",
            [10031] = "Apply Loudness Metadata",
            [10032] = "Target Loudness",
            [10033] = "Apply SONGVOL",
            [10034] = "Sound Effect Level",
            [10035] = "Voice Level",
            [10036] = "Song Playback Level",
            [10037] = "Keyboard Level Increment",
            [10038] = "MusicPreTimeMs",
            [10039] = "Autosaveresult",
            [10040] = "SendDiscordPlayingInformation",
            [10041] = "BufferedInput",
            [10042] = "TraceLog",
            [10043] = "SoundType",
            [10044] = "WASAPIBufSize",
            [10045] = "ASIO device",
            [10046] = "UseOSTimer",
            [10047] = "ShowChara",
            [10048] = "ShowDancer",
            [10049] = "ShowMob",
            [10050] = "ShowRunner",
            [10051] = "ShowFooter",
            [10052] = "FastRender",
            [10053] = "ShowPuchiChara",
            [10054] = "Skin (Full)",
            [10055] = "System Keys",
            [10084] = "ShinuchiMode",

            [100] = "Taiko Mode",
            [101] = "Dan-i Dojo",
            [102] = "Taiko Towers",
            [103] = "Shop",
            [104] = "Taiko Adventure",
            [105] = "My Room",
            [106] = "Settings",
            [107] = "Exit",

            [150] = "Play your favorite\nsongs at your own pace !",
            [151] = "Play multiple charts in continuation\nfollowing challenging exams\nin order to get a PASS rank !",
            [152] = "Play long charts within a limited\ncount of lives and reach\nthe top of the tower !",
            [153] = "Buy new songs, petit-chara or characters\nusing the medals you earned in game !",
            [154] = "Surpass various obstacles and\nunlock new content and horizons !",
            [155] = "Change your nameplate info\n or your character visuals !",
            [156] = "Change your game style\n or general settings !",
            [157] = "Quit the game.\nSee you next time !",

            [200] = "Return",
            [201] = "Recently played songs",
            [202] = "Play recently played songs !",

            [1000] = "Reached floor",
            [1001] = "F",
            [1002] = "P",
            [1003] = "Score",

            [1010] = "Soul gauge",
            [1011] = "Perfect count",
            [1012] = "Good count",
            [1013] = "Bad count",
            [1014] = "Score",
            [1015] = "Rolls count",
            [1016] = "Hit count",
            [1017] = "Combo",
            [1018] = "Accuracy",

            [1030] = "Return",
            [1031] = "Petit-Chara",
            [1032] = "Character",
            [1033] = "Dan Title",
            [1034] = "Nameplate Title",
        };
    }
}
