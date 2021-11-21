using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FDK;

namespace TJAPlayer3
{
    internal class CLang_es : ILang
    {
        string ILang.GetString(int idx)
        {
            if (!dictionnary.ContainsKey(idx))
                return "[!] No se ha encontrado el índice en el diccionario.";

            return dictionnary[idx];
        }


        private static readonly Dictionary<int, string> dictionnary = new Dictionary<int, string>
        {
            [0] = "Cambia el idioma que se usa\nen el juego y menús.",
            [1] = "Idioma del sistema",
            [2] = "<< Volver al menú",
            [3] = "Volver al menú de la izquierda.",
            [4] = "Recargar datos de canciones",
            [5] = "Recarga y actualiza la lista de canciones.",
            [6] = "Número de jugadores",
            [7] = "Cambia el número de jugadores.\nAjustarlo a 2 permite jugar\ncanciones regulares a dos jugadores dividiendo \nla pantalla a la mitad.",
            [8] = "Modo de riesgo",
            [9] = "Modo de riesgo:\nDéjalo por sobre 1, en caso de que quieras especificar\nel número de Malo/Fallos para considerar el intento\nfallido.\nDejar en 0 para deshabilitar el modo de riesgo.",
            [10] = "Velocidad de la canción",
            [11] = "Cambia la velocidad de la canción.\n" +
                "Por ejemplo, puedes jugar a mitad de\n" +
                "velocidad ajustando el valor PlaySpeed = 0.500\n" +
                "para practicar.\n" +
                "\n" +
                "Nota: También cambia el tono de la canción.\n" +
                "Si Time Stretch está encendido, puede haber\n" +
                "lag de audio si se usa en menos de x0.900.",
            [16] = "Tipo de interfaz",
            [17] = "Puedes cambiar la interfaz de las canciones \nmostradas en la pantalla de selección.\n" +
                "0 : Regular (Diagonal de arriba hacia abajo)\n" +
                "1 : Vertical\n" +
                "2 : Diagonal de abajo hacia arriba\n" +
                "3 : Medio circulo hacia la derecha\n" +
                "4 : Medio circulo hacia la izquierda",
            [18] = "Como cambiar la velocidad del juego:\n" +
                "Encender para usar Time Stretch\n" +
                "y así cambiar la velocidad del juego." +
                "\n" +
                "Con Time Stretch activado, el juego usa\n" +
                "más capacidad del procesador. Y algo de \n" +
                "lag de audio ocurre por debajo de x0.900.",
            [19] = "Modo de ventana o pantalla completa.",
            [20] = "Desactivalo si no quieres que aparezca\nla pantalla de FIN DEL JUEGO.",
            [21] = "Activar el uso de subcarpetas en la\nSELECCIÓN ALEATORIA.",
            [23] = "Usar la reproducción AVI o no.",
            [24] = "Activar BGA (animaciones de fondo) o no.",
            [25] = "Tiempo de retraso(ms) para empezar a reproducir la\ndemo de la música en la pantalla\nSELECCIONAR CANCIÓN.\nPuedes especificar de 0ms a 10000ms",
            [26] = "Ajuste que proviene de DTXMania.\nEn OpenTaiko este no hace nada.",
            [27] = "Si está activado se mostrará información extra en\nla zona de BGA. (FPS, BPM, tiempo total, etc)\nPuedes activar o desactivar los indicadores\npresionando [Del] mientras juegas.",
            [28] = "Ajuste del grado de transparencia del\n juego y fondo.\n\n0=completamente transparente,\n255=sin transparencia",
            [29] = "Desactívalo si no quieres que\nse reproduzca música de fondo.",
            [30] = "Si quieres guardar tus records, actívalo.\nDesactívalo si tus canciones estan en\nun medio de solo lectura (CD-ROM, etc).\nNote that the score files also contain\n 'BGM Adjust' parameter. So if you\n want to keep adjusting parameter,\n you need to set SaveScore=ON.",
            [31] = "Originalmente una opción especial\npara el control de volumen.\nEste ajuste ya no sirve y deberia ser removido.",
            [32] = "Otro ajuste relacionado con BS1770GAIN\ny por lo mismo, inutilizable.",
            [33] = "Actívalo para usar el valor SONGVOL desde\nel .tja, Desactívalo si quieres usar los\ncontroles de volumen del juego.",
            [34] = $"The level adjustment for sound effects.\nYou can specify from {CSound.MinimumGroupLevel} to {CSound.MaximumGroupLevel}%.",
            [35] = $"The level adjustment for voices.\nYou can specify from {CSound.MinimumGroupLevel} to {CSound.MaximumGroupLevel}%.",
            [36] = $"The level adjustment for songs during gameplay.\nYou can specify from {CSound.MinimumGroupLevel} to {CSound.MaximumGroupLevel}%.",
            [37] = "The amount of sound level change for each press\nof a sound level control key.\nYou can specify from 1 to 20.",
            [38] = "Blank time before music source to play. (ms)\n",
            [39] = "AutoSaveResult:\nTurn ON to save your result screen\n image automatically when you get\n hiscore/hiskill.",
            [40] = "Comparte la información del .tja que estas\njugando en Discord.",
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
            [47] = "Mostrar imágenes del Personaje.\n",
            [48] = "Mostrar imágenes de Dancer.\n",
            [49] = "Mostrar imágenes de Mob.\n",
            [50] = "Mostrar imágenes de Runner.\n",
            [51] = "Mostrar imagen del Footer.\n",
            [52] = "Usar texturas pre-renderizadas.\n",
            [53] = "Mostrar imágenes del PuchiChara.\n",
            [54] = "Skin:\n" +
                "Cambiar Skin.",
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
            [80] = "DificultadDefecto",
            [81] = "Dificultad seleccionada por defecto.\n",
            [82] = "ModoPuntuación",
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

            [100] = "Modo Taiko",
            [101] = "Desafíos del Dojo",
            [102] = "Torres Taiko",
            [103] = "Tienda",
            [104] = "Aventura Taiko",
            [105] = "Mi Habitación",
            [106] = "Ajustes",
            [107] = "Salir",

            [150] = "¡Juega tus canciones\nfavoritas a tu propio gusto!",
            [151] = "¡Juega varias canciones seguidas de\npruebas desafiantes\npara obtener el rango Aprobado!",
            [152] = "¡Juega canciones largas con un\nnumero de vidas limitado y llega\na la punta de la torre!",
            [153] = "¡Compra nuevas canciones, petit-chara o personajes\nusando las medallas que ganaste jugando!",
            [154] = "¡Atraviesa varios obstáculos y\ndesbloquea nuevo contenido!",
            [155] = "¡Cambia la información de tu placa\n o el aspecto de tu personaje!",
            [156] = "¡Cambia tu estilo de juego\n o ajustes generales!",
            [157] = "Salir del juego.\n¡Hasta la próxima!",
            
            [200] = "Regresar",
            [201] = "Canciones jugadas recientemente",
            [202] = "¡Juega las canciones que jugaste recientemente!",

            [1000] = "Piso alcanzado",
            [1001] = "P",
            [1002] = "P",
            [1003] = "Puntuación",
            
            [1010] = "Indicador de almas",
            [1011] = "Cantidad de Perfectas",
            [1012] = "Cantidad de Buenas",
            [1013] = "Cantidad de Malas",
            [1014] = "Puntuación",
            [1015] = "Cantidad de redobles",
            [1016] = "Cantidad de golpes",
            [1017] = "Combo",
            [1018] = "Precisión",

            [1030] = "Regresar",
            [1031] = "Petit-Chara",
            [1032] = "Personaje",
            [1033] = "Título de Dan",
            [1034] = "Título de la Nameplate",
        };
    }
}
