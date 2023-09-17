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
            [4] = "Reload Songs",
            [5] = "Reload the song folder.",
            [6] = "Player Count",
            [7] = "Select whether to use 2 player mode.",
            [8] = "Kanpeki Mode",
            [9] = "Choose how many BADs are allowed\nbefore a song is automatically failed.\nSet this to 0 to disable the mode.",
            [10] = "Song Playback Speed",
            [11] = "Change song playback speed.\nIf the Time Stretch option is enabled,\nsound issues may occur below 0.9x playback speed.\n" +
                "Note: It also changes the songs' pitch.",
            [12] = "AI Level",
            [13] = "Determines how precise the AI is.\n" +
                "If 0, AI is disabled.\n" +
                "If 1 or more, the 2P will play as AI.\n" +
                "Disabled if AUTO 2P is on.",
            [14] = "Global Offset",
            [15] = "Change the interpreted OFFSET\nvalue for all charts.\n" +
                "Can be set between -999 and 999ms.\n" +
                "To decrease input lag, set minus value.",
            [16] = "Layout type",
            [17] = "You can change the layout of the songs \ndisplayed on the song select screen.\n" +
                "0 : Regular (Up to down diagonal)\n" +
                "1 : Vertical\n" +
                "2 : Down to up diagonal\n" +
                "3 : Half-circle facing right\n" +
                "4 : Half-circle facing left",
            [18] = "Not sure what this option does.\nIt uses more CPU power,\nand might cause sound issues below 0.9x playback speed.",
            [19] = "Toggle between fullscreen and windowed mode.",
            [20] = "This is a redundant setting\nported from DTXMania.\nIt does nothing.",
            [21] = "Toggle whether subfolders are used\nduring random song selection.",
            [22] = "Toggle whether VSync is used.\nTurning it on will cap the FPS at 60,\nwhich will make the note scroll appear smoother\nbut increase input delay.\nTurning it off will uncap the fps,\nwhich will decrease input delay\nbut make the note scroll appear more unstable.",
            [23] = "Toggle whether background videos are used.\nIf this is enabled and a video is missing from a folder,\nthe background will appear blacked out.",
            [24] = "Toggle whether background animations appear.",
            [25] = "The time taken before a song preview is played.\nDecreasing this value may cause previews\nto begin while still scrolling.\nYou can specify from 0ms to 10000ms.",
            [26] = "This is a redundant setting\nported from DTXMania.\nIt does nothing.",
            [27] = "Toggle whether debug mode is enabled.\nThis will cause additional information\nto appear in the bottom right.\nThis will display your latency calibration\nfor hitsoundless play.",
            [28] = "This controls the opacity of background videos.\nIncrease the value if videos aren’t playing.\n\n0 = completely transparent,\n255 = no transparency",
            [29] = "Toggles whether music is played.",
            [30] = "Toggles whether score.ini files are saved in song folders.\nSong offset is saved here,\nso if hitsounds are disabled turn this on.",
            [31] = "This is a redundant setting that intended to use BSGain\nsound settings.\nSince BSGain support is broken,\nthis setting does nothing.",
            [32] = "This is a redundant setting that intended to use BSGain\nto normalise sound volume.\nSince BSGain support is broken,\nthis setting does nothing.",
            [33] = "This is a partially redundant setting\nthat toggles whether SONGVOL metadata is used.\nValues between 0 and 100 will lower song volume,\nbut any values over 100 do nothing.",
            [34] = "Adjust the volume of sounds related to don and ka.\nTo play without hitsounds, set this to 0.\nYou must restart the game after leaving config\nfor this setting to save.",
            [35] = "Adjust the volume of sounds related don-chan’s voice.\nYou must restart the game after leaving config\nfor this setting to save.",
            [36] = "Adjust the volume of song playback.\nYou must restart the game after leaving config\nfor this setting to save.",
            [37] = "Use the [ key to decrease volume\nand use the ] key to increase volume.\nThis setting changes how much they change the volume by.\nYou can specify from 1 to 20.",
            [38] = "The time taken before song playback during gameplay.\nDecreasing the value may cause songs to play too early.",
            [39] = "Toggle whether results screenshots are automatically taken.\nThis will only occur when a highscore is achieved,\nwhich may not correlate to the best play on that song.",
            [40] = "Toggle whether song information is shared with discord.",
            [41] = "When this is turned on, no inputs will be dropped\nbut the input poll rate will decrease.\nWhen this is turned off, inputs may be dropped\nbut they will be polled more often.",
            [42] = "Toggle whether a TJAPlayer3.log file is generated\nwhen the game is closed.\nThis tracks the performance of the game\nand identifies errors.",
            [43] = "ASIO:\n- Only works on sound devices that support asio playback\n- Has the least input delay\n\nWasapi:\n- Disables sound playback from any source except TJAP3\n- Has the second lowest input delay\n\nDirect Sound:\n- Allows exterior sound playback\n- Has the most input delay\n" +
                "Note: Exit CONFIGURATION to make\n" +
                "     the setting take effect.",
            [44] = "Change the sound buffer for wasapi sound playback mode.\nSet the number to be as low as possible\nwithout causing sound issues such as\nsong freezing and incorrect timing.\nSet it to 0 to use an estimated correct value,\nor use trial and error to find the correct value." +
                "\n" +
                "Note: Exit CONFIGURATION to make\n" +
                "     the setting take effect.",
            [45] = "Choose a valid device to enable asio playback mode with." +
                    "\n" +
                    "Note: Exit CONFIGURATION to make\n" +
                "     the setting take effect.",
            [46] = "Turning this on will create smoother note scroll,\nbut may introduce sound lag.\nTurning it off will create unstable note scroll,\nbut ensure no sound lag occurs.\n" +
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
            [52] = "Toggle whether images are rendered prior to songs loading.\n",
            [53] = "Show PuchiChara Images.\n",
            [54] = "Choose a skin to use from the system folder.",
            [55] = "A secondary menu for assigning system keys.",
            [56] = "Player 1 Auto Play",
            [57] = "Toggle whether player 1 plays automatically.\nPress F3 on song select to toggle this.",
            [58] = "Player 2 Auto Play",
            [59] = "Toggle whether player 2 plays automatically.\nPress F4 on song select to toggle this.",
            [60] = "Roll Speed",
            [61] = "When auto is enabled, rolls will be \nautomatically hit this many times per \nsecond. Has no effect on balloons. \n0 disables auto roll, and the \nmaximum value is one hit per frame.",
            [62] = "Scroll Speed",
            [63] = "Change the speed the notes travel at.\n" +
                "Can be set between x0.1 and x200.0.\n" +
                "(ScrollSpeed=x0.5 means half speed)",
            [64] = "Kanpeki Mode",
            [65] = "Choose how many BADs are allowed\nbefore a song is automatically failed.\nSet this to 0 to disable the mode.",
            [66] = "Note Modifiers",
            [67] = "Notes come randomly.\n\n Part: swapping lanes randomly for each\n  measures.\n Super: swapping chip randomly\n Hyper: swapping randomly\n  (number of lanes also changes)",
            [68] = "Hidden Notes",
            [69] = "DORON: Notes are hidden.\n" +
                "STEALTH: Notes and the text below them are hidden.",
            [70] = "No Information Mode",
            [71] = "Toggle whether song information is shown.\nTurning this on will disable song informaton.\nTurning this off will enable song information.\n",
            [72] = "Justice Mode",
            [73] = "Enabling this turns all OKs into BADs.",
            [74] = "Notelock Mode",
            [75] = "Toggle whether hitting in the space between notes\ncounts as a BAD.",
            [76] = "Minimum Combo Display",
            [77] = "Choose the initial number that combo is displayed at.\n" +
                "Can be specified between 1 and 99999.",
            [78] = "Hitcircle Adjustment",
            [79] = "Increasing the value will move the note\njudge area further right.\nDecreasing the value will move the note\njudge area further left.\n" +
                "Can be set between -99 and 99ms.\n" +
                "To decrease input lag, set minus value.",
            [80] = "Default Difficulty",
            [81] = "Choose the default difficulty to be chosen on song select.\nIf ura is not chosen, it will not be visible\nunless the right arrow key is pressed\non that song’s oni difficulty.",
            [82] = "Score Mode",
            [83] = "Chooses the formula used to determine scores.\n" +
                    "TYPE-A: Gen-1\n" +
                    "TYPE-B: Gen-2\n" +
                    "TYPE-C: Gen-3\n",
            [84] = "Makes every note worth\nthe same amount of points.\nUses the Gen-4 formula.",
            [85] = "Branch Guide",
            [86] = "Toggle whether a numerical guide is displayed\nto view which branch is going to be picked.\nIt doesn’t display on auto mode.",
            [87] = "Branch Animation Set",
            [88] = "Changes the animation set used when a chart branches.\n" +
                    "TYPE-A: Gen-2\n" +
                    "TYPE-B: Gen-3\n" +
                    " \n",
            [89] = "Survival Mode",
            [90] = "This mode is broken.\nIt implements a timer system similar to stepmania courses,\nbut some code is missing so the functionality is limited.",
            [91] = "Big Note Judgement",
            [92] = "Toggle whether big notes reward being hit with 2 keys.\nIf this is on, using 1 key will cause a visual delay\nbefore they disappear.\nHits with 2 keys will award double points.\nIf this is off, using 1 key will hit them like a regular note.\nDouble points will still be awarded\nAttempting to hit them with 2 keys may cause\nthe next note to be hit instead.",
            [93] = "Toggle Score Display",
            [94] = "Display the current good/ok/bad judgements\n in the bottom left.\n" +
                "(Single Player Only)",
            [95] = "Gameplay Key Config",
            [96] = "A secondary menu to adjust keys used during gameplay.",
            [97] = "Capture",
            [98] = "Capture key assign:\nTo assign key for screen capture.\n (You can use keyboard only. You can't\nuse pads to capture screenshot.",
            [99] = "LeftRed",

            [9996] = "Drawing Method:\n" +
                    "Select from either OpenGL,\n" +
                    "Vulkan, DirectX11, or DirectX12.\n" +
                    "OpenGL is slow, but compatible & stable.\n" +
                    "Vulkan works fastest on Linux.\n" +
                    "DirectX11 is fast and stable, but only\n" +
                    "works on Windows.\n" +
                    "DirectX12 is very fast but only works\n" +
                    "on Windows.\n" +
                    "\n" +
                    "This will take effect after game reboot.\n",
            [9997] = "Graphics API",

            [9998] = "Buffer size when using Bass:\n" +
                    "Size can be between 0～99999ms.\n" +
                    "A value of 0 will make the OS\n" +
                    "automatically decide the size.\n" +
                    "The smaller the value, the less\n" +
                    "audio lag there is, but may also\n" +
                    "cause abnormal/crackling audio.\n" +
                    "※ NOTE: Exit CONFIGURATION to make the\n" +
                    "　      settings take effect.",
            [9999] = "Bass Buffer Size",

            [10000] = "Drums key assign:\nTo assign key/pads for LeftRed\n button.",
            [10001] = "RightRed",
            [10002] = "Drums key assign:\nTo assign key/pads for RightRed\n button.",
            [10003] = "LeftBlue",
            [10004] = "Drums key assign:\nTo assign key/pads for LeftBlue\n button.",
            [10005] = "RightBlue",
            [10006] = "Drums key assign:\nTo assign key/pads for RightBlue\n button.",
            [10007] = "LeftRed2P",
            [10008] = "Drums key assign:\nTo assign key/pads for LeftRed2P\n button.",
            [10009] = "RightRed2P",
            [10010] = "Drums key assign:\nTo assign key/pads for RightRed2P\n button.",
            [10011] = "LeftBlue2P",
            [10012] = "Drums key assign:\nTo assign key/pads for LeftBlue2P\n button.",
            [10013] = "RightBlue2P",
            [10014] = "Drums key assign:\nTo assign key/pads for RightBlue2P\n button.",
            [10018] = "Time Stretch Mode",
            [10019] = "Fullscreen Mode",
            [10020] = "Game Over Mode",
            [10021] = "Use Subfolders in Random Selection",
            [10022] = "VSync Mode",
            [10023] = "Toggle Video Playback",
            [10024] = "Draw BGA",
            [10025] = "Song Preview Buffer",
            [10026] = "Image Preview Buffer",
            [10027] = "Debug Mode",
            [10028] = "Background Opacity",
            [10029] = "Toggle Song Playback",
            [10030] = "Save Scores",
            [10031] = "Apply Loudness Metadata",
            [10032] = "Target Loudness",
            [10033] = "Apply SONGVOL Metadata",
            [10034] = "Sound Effect Volume",
            [10035] = "Voice Volume",
            [10036] = "Song Playback Volume",
            [10037] = "Keyboard Volume Increment",
            [10038] = "Song Playback Buffer",
            [10039] = "Automatic Screenshots",
            [10040] = "Discord Rich Presence",
            [10041] = "Buffered Input Mode",
            [10042] = "Create Error Logs",
            [10043] = "Sound Playback Mode",
            [10044] = "Wasapi Buffer Size",
            [10045] = "Asio Playback Device",
            [10046] = "OS Timer Mode",
            [10047] = "Display Characters",
            [10048] = "Display Dancers",
            [10049] = "Display Mob",
            [10050] = "Display Runners",
            [10051] = "Display Footer",
            [10052] = "Fast Render",
            [10053] = "Draw PuchiChara",
            [10054] = "Current Skin",
            [10055] = "System Key Config",
            [10056] = "Hide Dan/Tower",
            [10057] = "Hide Dan and Tower charts\nin the Taiko Mode menu.\n" +
                    "Note: Reload songs to make\n" +
                "     the setting take effect.",
            [10058] = "Song Preview Volume",
            [10059] = "Adjust the volume of song preview.\nYou must restart the game after leaving config\nfor this setting to save.",
            [10060] = "Clap",
            [10061] = "Konga key assign:\nTo assign key/pads for Clap\n button.",
            [10062] = "Clap2P",
            [10063] = "Konga key assign:\nTo assign key/pads for Clap2P\n button.",

            [10064] = "Decide",
            [10065] = "Menu decide key.",
            [10066] = "Cancel",
            [10067] = "Menu cancel key.",
            [10068] = "LeftChange",
            [10069] = "Menu left change key.",
            [10070] = "RightChange",
            [10071] = "Menu right change key.",

            [10084] = "Shin'uchi Mode",
            [10085] = "System options",
            [10086] = "Gameplay options",
            [10087] = "Exit",
            [10091] = "Settings for an overall systems.",
            [10092] = "Settings to play the drums.",
            [10093] = "Save the settings and exit from CONFIGURATION menu.",

            [10094] = "LeftRed3P",
            [10095] = "Drums key assign:\nTo assign key/pads for LeftRed3P\n button.",
            [10096] = "RightRed3P",
            [10097] = "Drums key assign:\nTo assign key/pads for RightRed3P\n button.",
            [10098] = "LeftBlue3P",
            [10099] = "Drums key assign:\nTo assign key/pads for LeftBlue3P\n button.",
            [10100] = "RightBlue3P",
            [10101] = "Drums key assign:\nTo assign key/pads for RightBlue3P\n button.",

            [10102] = "LeftRed4P",
            [10103] = "Drums key assign:\nTo assign key/pads for LeftRed4P\n button.",
            [10104] = "RightRed4P",
            [10105] = "Drums key assign:\nTo assign key/pads for RightRed4P\n button.",
            [10106] = "LeftBlue4P",
            [10107] = "Drums key assign:\nTo assign key/pads for LeftBlue4P\n button.",
            [10108] = "RightBlue4P",
            [10109] = "Drums key assign:\nTo assign key/pads for RightBlue4P\n button.",

            [10110] = "LeftRed5P",
            [10111] = "Drums key assign:\nTo assign key/pads for LeftRed5P\n button.",
            [10112] = "RightRed5P",
            [10113] = "Drums key assign:\nTo assign key/pads for RightRed5P\n button.",
            [10114] = "LeftBlue5P",
            [10115] = "Drums key assign:\nTo assign key/pads for LeftBlue5P\n button.",
            [10116] = "RightBlue5P",
            [10117] = "Drums key assign:\nTo assign key/pads for RightBlue5P\n button.",

            [10118] = "Clap3P",
            [10119] = "Konga key assign:\nTo assign key/pads for Clap3P\n button.",
            [10120] = "Clap4P",
            [10121] = "Konga key assign:\nTo assign key/pads for Clap4P\n button.",
            [10122] = "Clap5P",
            [10123] = "Konga key assign:\nTo assign key/pads for Clap5P\n button.",

            [10124] = "Use Extreme/Extra Transitions",
            [10125] = "Play a skin-defined animation\nwhile switching between\nExtreme & Extra.",

            [100] = "Taiko Mode",
            [101] = "Dan-i Dojo",
            [102] = "Taiko Towers",
            [103] = "Shop",
            [104] = "Taiko Adventure",
            [105] = "My Room",
            [106] = "Settings",
            [107] = "Exit",
            [108] = "Online Lounge",
            [109] = "Open Encyclopedia",
            [110] = "AI Battle Mode",
            [111] = "Player Stats",
            [112] = "Chart Editor",
            [113] = "Open Toolbox",

            [150] = "Play your favorite\nsongs at your own pace !",
            [151] = "Play multiple charts in continuation\nfollowing challenging exams\nin order to get a PASS rank !",
            [152] = "Play long charts within a limited\ncount of lives and reach\nthe top of the tower !",
            [153] = "Buy new songs, petit-chara or characters\nusing the medals you earned in game !",
            [154] = "Surpass various obstacles and\nunlock new content and horizons !",
            [155] = "Change your nameplate info\n or your character visuals !",
            [156] = "Change your game style\n or general settings !",
            [157] = "Quit the game.\nSee you next time !",
            [158] = "Download new charts\nand content from\n the internet !",
            [159] = "Learn about OpenTaiko\nrelated features and\nhow to install new content !",
            [160] = "Fight a strong AI through\nmultiple sections and\naim for victory !",
            [161] = "Watch and track your\nprogression !",
            [162] = "Create your own .tja charts\nbased on your favorite songs !",
            [163] = "Use various tools to insert\nnew custom content !",

            [200] = "Return",
            [201] = "Recently played songs",
            [202] = "Play recently played songs !",
            [203] = "Random song",

            [300] = "Coins got !",
            [301] = "Character got !",
            [302] = "Puchichara got !",
            [303] = "Title got !",
            [304] = "Notice",
            [305] = "Confirm",
            [306] = "Coins",
            [307] = "Total",

            [400] = "Return to main menu",
            [401] = "Return",
            [402] = "Download content",
            [403] = "Select a CDN",
            [404] = "Download Songs",
            [405] = "Download Characters",
            [406] = "Download Puchicharas",
            [407] = "Online Multiplayer",

            [500] = "Timing",
            [501] = "Loose",
            [502] = "Lenient",
            [503] = "Normal",
            [504] = "Strict",
            [505] = "Rigorous",
            [510] = "Score Multiplier : ",
            [511] = "Coins Multiplier : ",
            [512] = "Game Type",
            [513] = "Taiko",
            [514] = "Konga",
            [515] = "Fun mods",
            [516] = "Avalanche",
            [517] = "Minesweeper",

            [1000] = "Reached floor",
            [1001] = "F",
            [1002] = "P",
            [1003] = "Score",

            [1010] = "Soul gauge",
            [1011] = "Good count",
            [1012] = "Ok count",
            [1013] = "Bad count",
            [1014] = "Score",
            [1015] = "Rolls count",
            [1016] = "Hit count",
            [1017] = "Combo",
            [1018] = "Accuracy",
            [1019] = "ADLIB count",
            [1020] = "Bombs hit",

            [1030] = "Return",
            [1031] = "Petit-Chara",
            [1032] = "Character",
            [1033] = "Dan Title",
            [1034] = "Nameplate Title",

            [1040] = "Easy",
            [1041] = "Normal",
            [1042] = "Hard",
            [1043] = "Extreme",
            [1044] = "Extra",
            [1045] = "Extreme / Extra",

            [90000] = "[ERROR] Invalid condition",
            [90001] = "Item only avaliable at the Shop.",
            [90002] = "Coin price : ",
            [90003] = "Item bought !",
            [90004] = "Not enough coins !",
            [90005] = "The following condition : ",

            [900] = "Resume",
            [901] = "Restart",
            [902] = "Quit",

            [910] = "AI",
            [911] = "Deus-Ex-Machina",

            [9000] = "Off",
            [9001] = "On",
            [9002] = "None",
            [9003] = "Shuffle",
            [9004] = "Chaos",
            [9006] = "Training Mode",
            [9007] = "-",
            [9008] = "Speed",
            [9009] = "Invisible",
            [9010] = "Flip Notes",
            [9011] = "Random",
            [9012] = "Game Mode",
            [9013] = "Auto",
            [9014] = "Voice",
            [9015] = "Sound Type",
            [9016] = "Stealth",
            [9017] = "Safe",
            [9018] = "Just",

            [9100] = "Search (Difficulty)",
            [9101] = "Difficulty",
            [9102] = "Level",

            [9200] = "Return",
            [9201] = "Path",
            [9202] = "Title",
            [9203] = "Subtitle",
            [9204] = "Displayed Level",
        };
    }
}
