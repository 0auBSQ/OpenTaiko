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
            [0] = "Cambia el idioma que se usa\n en el juego y menús.",
            [1] = "Idioma del sistema",
            [2] = "<< Volver al menú",
            [3] = "Volver al menú de la izquierda.",
            [4] = "Recargar lista de canciones",
            [5] = "Recarga y actualiza la lista de canciones.",
            // ----------------------------------
            [10148] = "Reload Songs (Hard Reload)",
            [10149] = "Clean the existing database and\n" + 
            "reload the song folder from scratch.",
            // Please translate the text above!
            [6] = "Número de jugadores",
            // ----------------------------------
            [7] = "Cambia el número de jugadores.\nAjustarlo a 2 permite jugar\n canciones regulares a dos jugadores dividiendo\n la pantalla a la mitad.",
            // Please update the translation above. Up to 5 players can be active at a time.
            [8] = "Modo Kanpeki",
            [9] = "Modo Kanpeki:\nElige el número de fallos antes de\n que se considere un intento fallido.\nDejar en 0 para deshabilitar el modo Kanpeki.",
            [10] = "Velocidad de la canción",
            [11] = "Cambia la velocidad de la canción.\n" +
                "Por ejemplo, puedes jugar a mitad de\n" +
                " velocidad ajustando el valor PlaySpeed = 0.500\n" +
                " para practicar.\n" +
                "\n" +
                "Nota: También cambia el tono de la canción.\n" +
                "Si Time Stretch está encendido, puede haber\n" +
                " lag de audio si se usa en menos de x0.9.",
            [12] = "Nivel de la IA",
            [13] = "Determina que tan precisa es la IA.\n" +
                "Si se deja en 0, se desactiva.\n" +
                "Si se deja en 1 o más,\n el J2 se convierte en IA.\n" +
                "No se usa si Juego Automático J2\n se encuentra activado.",
            [14] = "Compensación global de sonido",
            [15] = "Cambia el retardo de la canción\npara todos los charts.\n" +
                "Puedes elegir entre -999 a 999ms.\n" +
                "Para disminuir el retraso de la entrada,\n disminuye este valor.",
            [16] = "Tipo de interfaz",
            [17] = "Puedes cambiar la interfaz de las canciones\n mostradas en la pantalla de selección.\n" +
                "0 : Regular (Diagonal de arriba hacia abajo)\n" +
                "1 : Vertical\n" +
                "2 : Diagonal de abajo hacia arriba\n" +
                "3 : Medio circulo hacia la derecha\n" +
                "4 : Medio circulo hacia la izquierda",
            [18] = "No se está seguro de que hace esto.\nUsa más capacidad de la CPU,\n y causa lag si la velocidad de juego está\nen menos de x0.9.",
            [19] = "Modo de ventana o pantalla completa.",
            [20] = "Ajuste que proviene de DTXMania.\nEn OpenTaiko este no hace nada.",
            [21] = "Activar el uso de subcarpetas en la\n SELECCIÓN ALEATORIA.",
            [22] = "Activa la sincronización vertical.\nActivarlo limitará los FPS a 60, aumentará\nel retraso de entrada y suavizará el desplazamiento.\nDesactivarlo no limitará los FPS,\ndisminuirá el retraso de la entrada pero\nel desplazamiento se verá afectado.",
            [23] = "Usar la reproducción AVI o no.",
            [24] = "Activar BGA (animaciones de fondo) o no.",
            [25] = "Tiempo de retraso(ms) para empezar a reproducir la\ndemo de la música en la pantalla\nSELECCIONAR CANCIÓN.\nPuedes especificar de 0ms a 10000ms",
            [26] = "Ajuste que proviene de DTXMania.\nEn OpenTaiko este no hace nada.",
            [27] = "Si está activado se mostrará información extra en\nla zona de BGA. (FPS, BPM, tiempo total, etc)\nPuedes activar o desactivar los indicadores\npresionando [Del] mientras juegas.",
            // ----------------------------------
            [28] = "Ajuste del grado de transparencia del fondo.\n\n0=completamente transparente,\n255=sin transparencia",
            // Please update the translation above. The lane background (the bar drawn behind the notes) opacity is what's being adjusted, and will only take effect when videos are playing.
            [29] = "Desactívalo si no quieres que\nse reproduzca música de fondo.",
            [30] = "Guarda tus récords en el juego.\nDesactívalo si prefieres que tus puntajes no\nse guarden automáticamente.\n",
            [31] = "Ajuste relacionado con BS1770GAIN\n y por lo mismo, inutilizable.",
            [32] = "Ajuste relacionado con BS1770GAIN\n y por lo mismo, inutilizable.",
            [33] = "Actívalo para usar el valor SONGVOL desde\n el .tja, Desactívalo si quieres usar los\ncontroles de volumen del juego.",
            [34] = "Ajusta el volumen de los efectos de sonido.\nDebes reiniciar el juego después de salir\nde la configuración para aplicar los cambios",
            [35] = "Ajusta el volumen de las voces de Don-Chan.\nDebes reiniciar el juego después de salir\nde la configuración para aplicar los cambios",
            [36] = "Ajusta el volumen de la música.\nDebes reiniciar el juego después de salir\nde la configuración para aplicar los cambios",
            [37] = "La cantidad de volumen que cambia\nal presionar las teclas de control de volumen.\nPuedes especificar desde 1 a 20.",
            [38] = "Tiempo antes de que la música comience. (ms)\n",
            [39] = "Si activas esto, se tomará una captura\n cuando obtengas un nuevo récord.",
            [40] = "Comparte la información del .tja que estas\n jugando en Discord.",
            [41] = "Cuando se activa, la entrada no tendrá pérdidas,\n pero la tasa de actualización de la entrada será menor.\nCuando se desactiva, pueden haber entradas perdidas\n pero se actualizarán con más frecuencia.",
            [42] = "Actívalo para guardar info. de depuración\n en TJAPlayer3.log cuando cierres el juego.\nAquí se guarda información del rendimiento y\n eventuales errores del simulador.",
            // ----------------------------------
            [43] = "ASIO:\nSolo funciona en dispositivos compatibles con ASIO.\nTiene la menor latencia de entrada.\n\nWasapi:\nDesactiva cualquier otra fuente de sonido.\nTiene la segunda menor latencia de entrada.\n\nDirect Sound:\nPermite sonido desde otras fuentes.\nTiene la mayor latencia de entrada.\n" +
                 "\n" +
                 "Nota: Sal de la configuración\n" +
                 "     para aplicar los cambios.",
            // Please update the translation above; DirectSound is no longer used, and has been replaced with BASS. BASS is compatible with all platforms.
            [44] = "Cambia el buffer de sonido para Wasapi.\nDeja el número más bajo posible\n evitando problemas como congelamiento de la canción y\n timing incorrecto. Déjalo en 0 para usar un valor\n estimado, o encuentra el valor correcto para ti a base de\nprueba y error." +
                "\n" +
                "Nota: Sal de la configuración\n" +
                "     para aplicar los cambios.",
            [45] = "Dispositivo ASIO:\n" +
                    "Elige el dispositivo de audio usado con ASIO\n" +
                    "\n" +
                    "Note: Sal de la configuración\n" +
                "     para aplicar los cambios.",
            [46] = "Usar esto puede hacer que las notas se vean\n más suaves, pero puede haber lag de sonido.\nNo usarlo va a hacer que las notas se vean inestables,\n pero sin ningún tipo de lag.\n" +
                "\n" +
                "Este ajuste solo está disponible\n" +
                " usando WASAPI o ASIO.\n",
            [47] = "Mostrar imágenes del Personaje.\n",
            [48] = "Mostrar imágenes de Dancer.\n",
            [49] = "Mostrar imágenes de Mob.\n",
            [50] = "Mostrar imágenes de Runner.\n",
            [51] = "Mostrar imagen del Footer.\n",
            [52] = "Usar texturas pre-renderizadas.\n",
            [53] = "Mostrar imágenes del Puchi-Chara.\n",
            [54] = "Elige una skin desde la carpeta de skins.",
            [55] = "Menú secundario para cambiar las teclas que\nusa el juego.",
            [56] = "Juego automático",
            [57] = "Al activarlo, el carril de J1\n" +
                " se jugará automáticamente.",
            [58] = "Juego Automático J2",
            [59] = "Al activarlo, el carril de J2\n" +
                " se jugará automáticamente.",
            [60] = "Velocidad del redoble",
            [61] = "Redobles por segundo cuando se usa el\nmodo automático.\nNo tiene efecto en los globos.\nDesactivado si está en 0,\nbloqueado a un redoble por frame.",
            [62] = "VelDesplazamiento",
            [63] = "Cambiar la velocidad de desplazamiento\n" +
                " en el carril de las notas\n" +
                "Puedes ajustarlo desde x0.1 a x200.0.\n" +
                "(ScrollSpeed=x0.5 sería la mitad de velocidad)",
            [64] = "Modo Kanpeki",
            [65] = "Modo Kanpeki:\nElige el número de fallos antes de\n que se considere un intento fallido.\nDejar en 0 para deshabilitar el modo Kanpeki.",
            [66] = "Notas aleatorias",
            [67] = "Las notas Don y Ka se aleatorizan.\nCon las opciones que hay puedes cambiar la tasa\n de aleatorización.",
            [68] = "Notas ocultas",
            [69] = "DORON: Las notas están ocultas.\n" +
                "STEALTH: Las notas y el texto debajo están ocultos.",
            [70] = "Sin información",
            [71] = "Oculta la información de la canción.\n",
            [72] = "Modo estricto",
            [73] = "Solo permite las notas buenas, convirtiendo\nlas OK en fallos.",
            [74] = "Bloqueo de notas",
            [75] = "Activa si golpear en espacios vacíos\ncuenta como una falla.",
            [76] = "Combo mínimo",
            [77] = "Número mínimo para mostrar el combo\n" +
                "en el tambor.\n" +
                "Puedes elegir desde 1 a 99999.",
            [78] = "Ajuste del área de juicio",
            [79] = "Para cambiar la área del circulo de juicio de las notas.\nAumentarlo moverá la área a la derecha, y disminuirlo\n la moverá a la izquierda.\n" +
                "Puedes dejarlo desde -99 a 99ms.\n" +
                "Para disminuir el lag de la entrada,\n deja un valor negativo.",
            [80] = "Dificultad por defecto",
            [81] = "Dificultad seleccionada por defecto.\n",
            [82] = "Modo de puntuación",
            [83] = "Elige el método para calcular la puntuación\n" +
                    "TYPE-A: Puntuación de Gen-1\n" +
                    "TYPE-B: Puntuación de Gen-2\n" +
                    "TYPE-C: Puntuación de Gen-3\n",
            [84] = "Hace que todas las notas\n valgan los mismos puntos.\nUsa la fórmula de Gen-4.",
            [85] = "Guía de divisiones",
            [86] = "Activa una guía numérica para ver\n que división se elegirá.\nNo se muestra en modo automático.",
            [87] = "Animación de división",
            [88] = "Tipo de animación para las divisiones\n" +
                    "TYPE-A: Animación de Gen-2\n" +
                    "TYPE-B: Animación de Gen-3\n" +
                    " \n",
            [89] = "Modo de supervivencia",
            [90] = "Esta opción no funciona.\nImplementa un contador parecido al de Stepmania,\n pero el código está incompleto así que su\n funcionamiento es limitado.",
            [91] = "Considerar notas grandes",
            [92] = "Requerir usar los dos lados para golpear las\n notas grandes.",
            [93] = "Mostrar conteo de notas",
            [94] = "Mostrar el conteo de las notas\n" +
                "(Solo en modo de un jugador)",
            [95] = "Ajustes de controles",
            [96] = "Ajustes de los botones/pads que se usarán.",

            // ----------------------------------
            [10124] = "Use Extreme/Extra Transitions",
            [10125] = "Play a skin-defined animation\nwhile switching between\nExtreme & Extra.",

            [10126] = "Always use normal gauge",
            [10127] = "Always use normal gauge",

            [10150] = "Video Playback Display Mode",
            [10151] = "Change how videos are displayed\nin the background.",
            // Please translate the text above!

            [97] = "Captura",
            [98] = "Botón para capturar:\nPara asignar una tecla a la captura de pantalla.\n (Solo puedes usar el teclado. No puedes\nusar un pad del tambor para tomar capturas.",
            // ----------------------------------
            [10128] = "Increase Volume",
            [10129] = "System key assign:\nAssign any key for increasing music volume.\n(You can only use keyboard. You can't\nuse gamepads.)",
            [10130] = "Decrease Volume",
            [10131] = "System key assign:\nAssign any key for decreasing music volume.\n(You can only use keyboard. You can't\nuse gamepads.)",
            [10132] = "Display Hit Values",
            [10133] = "System key assign:\nAssign any key for displaying hit values.\n(You can only use keyboard. You can't\nuse gamepads.)",
            [10134] = "Display Debug Menu",
            [10135] = "System key assign:\nAssign any key for displaying debug menu.\n(You can only use keyboard. You can't\nuse gamepads.)",
            [10136] = "Quick Config",
            [10137] = "System key assign:\nAssign any key for accessing the quick config.\n(You can only use keyboard. You can't\nuse gamepads.)",
            [10138] = "Player Customization",
            [10139] = "System key assign:\nAssign any key for player customization.\n(You can only use keyboard. You can't\nuse gamepads.)",
            [10140] = "Change Song Sort",
            [10141] = "System key assign:\nAssign any key for resorting songs.\n(You can only use keyboard. You can't\nuse gamepads.)",
            [10142] = "Toggle Auto (P1)",
            [10143] = "System key assign:\nAssign any key for toggling auto (P1).\n(You can only use keyboard. You can't\nuse gamepads.)",
            [10144] = "Toggle Auto (P2)",
            [10145] = "System key assign:\nAssign any key for toggling auto (P2).\n(You can only use keyboard. You can't\nuse gamepads.)",
            [10146] = "Toggle Training Mode",
            [10147] = "System key assign:\nAssign any key for toggling training mode.\n(You can only use keyboard. You can't\nuse gamepads.)",
            [10152] = "Cycle Video Playback Display",
            [10153] = "System key assign:\nAssign any key for cycling video playback display modes.\n(You can only use keyboard. You can't\nuse gamepads.)",
            // Please translate the text above!

            [99] = "Rojo izquierdo",
            [10000] = "Asigna un botón o un pad para\n el Don (rojo) izquierdo.",
            [10001] = "Rojo derecho",
            [10002] = "Asigna un botón o un pad para\nel Don (rojo) derecho.",
            [10003] = "Azul izquierdo",
            [10004] = "Asigna un botón o un pad para\n el Ka (azul) izquierdo.",
            [10005] = "Azul derecho",
            [10006] = "Asigna un botón o un pad para\n el Ka (azul) derecho.",
            [10007] = "Rojo izquierdo J2",
            [10008] = "Asigna un botón o un pad para\n el Don (rojo) izquierdo del J2.",
            [10009] = "Rojo derecho J2",
            [10010] = "Asigna un botón o un pad para\n el Don (rojo) derecho del J2.",
            [10011] = "Azul izquierdo J2",
            [10012] = "Asigna un botón o un pad para\n el Ka (azul) izquierdo del J2.",
            [10013] = "Azul derecho J2",
            [10014] = "Asigna un botón o un pad para\n el Ka (azul) derecho del J2.",
            [10018] = "Time Stretch",
            [10019] = "Pantalla completa",
            [10020] = "Fin del juego",
            [10021] = "Usar subcarpetas en random",
            [10022] = "Sincronización vertical",
            [10023] = "Reproducción de video",
            [10024] = "BGA",
            [10025] = "Delay de la demo",
            [10026] = "Delay de la imagen",
            [10027] = "Informacion Debug",
            // ----------------------------------
            [10028] = "Transparencia del fondo",
            // Please update the translation above. The actual title should read "Lane Background Opacity". Check comment on [28] for further details.
            [10029] = "Volumen de música de fondo",
            [10030] = "Guardar Puntuación",
            [10031] = "Apply Loudness Metadata",
            [10032] = "Target Loudness",
            [10033] = "Usar SONGVOL",
            [10034] = "Volumen de efectos",
            [10035] = "Volumen de voces",
            [10036] = "Volumen de la música",
            [10037] = "Aumento de volumen",
            [10038] = "Retardo de la música",
            [10039] = "Autoguardar Resultado",
            [10040] = "Enviar info. a Discord",
            [10041] = "Entrada con buffer",
            [10042] = "Guardar registros",
            [10043] = "Sistema de sonido",
            [10044] = "Tamaño del buffer WASAPI",
            [10045] = "Dispositivo ASIO",
            [10046] = "Usar timer del sistema",
            [10047] = "Mostrar Personaje",
            [10048] = "Mostrar Dancer",
            [10049] = "Mostrar Mob",
            [10050] = "Mostrar Runner",
            [10051] = "Mostrar Footer",
            [10052] = "Renderizado Rápido",
            [10053] = "Mostrar Puchi-Chara",
            [10054] = "Skin (Full)",
            [10055] = "Teclas del sistema",
            [10056] = "Ocultar Dans/Torres",
            [10057] = "Ocultar charts de Dans y torres\nen el menú de Modo Taiko.\n" +
                    "Nota: Recarga las canciones para\n" +
                "     aplicar los cambios.",
            [10058] = "Volumen de la demo",
            [10059] = "Ajusta el volumen de la demo de la música.\nDebes reiniciar el juego después de salir\nde la configuración para aplicar los cambios",
            [10060] = "Aplauso",
            [10061] = "Asignación para teclas Konga:\nAsignar teclas/pads para el\n Aplauso.",
            [10062] = "AplausoP2",
            [10063] = "Asignación para teclas Konga:\nAsignar teclas/pads para el\n Aplauso J2.",
            
            [10064] = "Confirmar",
            [10065] = "Tecla de confirmacion en los menus.",
            [10066] = "Cancelar",
            [10067] = "Tecla para cancelar en los menus.",
            [10068] = "MenuIzquierda",
            [10069] = "Tecla para cambiar a la izquierda en los menus.",
            [10070] = "MenuDerecha",
            [10071] = "Tecla para cambiar a la derecha en los menus.",
            
            [10084] = "Modo Shinuchi",
            [10085] = "Opciones principales",
            [10086] = "Opciones del juego",
            [10087] = "Salir",
            [10091] = "Ajustes generales del simulador.",
            [10092] = "Ajustes para los controles.",
            [10093] = "Guarda los cambios y sal del menú de configuración.",

            [10094] = "Rojo izquierdo J3",
            [10095] = "Asigna un botón o un pad para\n el Don (rojo) izquierdo del J3.",
            [10096] = "Rojo derecho J3",
            [10097] = "Asigna un botón o un pad para\n el Don (rojo) derecho del J3.",
            [10098] = "Azul izquierdo J3",
            [10099] = "Asigna un botón o un pad para\n el Ka (azul) izquierdo del J3.",
            [10100] = "Azul derecho J3",
            [10101] = "Asigna un botón o un pad para\n el Ka (azul) derecho del J3.",

            [10102] = "Rojo izquierdo J4",
            [10103] = "Asigna un botón o un pad para\n el Don (rojo) izquierdo del J4.",
            [10104] = "Rojo derecho J4",
            [10105] = "Asigna un botón o un pad para\n el Don (rojo) derecho del J4.",
            [10106] = "Azul izquierdo J4",
            [10107] = "Asigna un botón o un pad para\n el Ka (azul) izquierdo del J4.",
            [10108] = "Azul derecho J4",
            [10109] = "Asigna un botón o un pad para\n el Ka (azul) derecho del J4.",

            [10110] = "Rojo izquierdo J5",
            [10111] = "Asigna un botón o un pad para\n el Don (rojo) izquierdo del J5.",
            [10112] = "Rojo derecho J5",
            [10113] = "Asigna un botón o un pad para\n el Don (rojo) derecho del J5.",
            [10114] = "Azul izquierdo J5",
            [10115] = "Asigna un botón o un pad para\n el Ka (azul) izquierdo del J5.",
            [10116] = "Azul derecho J5",
            [10117] = "Asigna un botón o un pad para\n el Ka (azul) derecho del J5.",

            [10118] = "AplausoJ3",
            [10119] = "Asignación para teclas Konga:\nAsignar teclas/pads para el\n Aplauso J3.",
            [10120] = "AplausoJ4",
            [10121] = "Asignación para teclas Konga:\nAsignar teclas/pads para el\n Aplauso J4.",
            [10122] = "AplausoJ5",
            [10123] = "Asignación para teclas Konga:\nAsignar teclas/pads para el\n Aplauso J5.",

            [100] = "Modo Taiko",
            [101] = "Dan-i Dojo",
            [102] = "Torres Taiko",
            [103] = "Tienda",
            [104] = "Aventura Taiko",
            [105] = "Mi Habitación",
            [106] = "Ajustes",
            [107] = "Salir",
            [108] = "Sala en línea",
            [109] = "Abrir enciclopedia",
            [110] = "Modo de batalla IA",
            [111] = "Estadisticas del jugador",
            [112] = "Editor de charts",
            [113] = "Abrir herramientas",

            [150] = "¡Juega tus canciones\nfavoritas a tu propio gusto!",
            [151] = "¡Juega varias canciones mientras cumples\nretos que te pondrán a prueba\npara completar el desafío!",
            [152] = "¡Juega canciones largas con un\nnúmero de vidas limitado y llega\na la punta de la torre!",
            [153] = "¡Compra nuevas canciones, PuchiCharas o personajes\nusando las medallas que ganaste jugando!",
            [154] = "¡Atraviesa varios obstáculos y\ndesbloquea nuevo contenido!",
            [155] = "¡Cambia la información de tu placa\n o el aspecto de tu personaje!",
            [156] = "¡Cambia tu estilo de juego\no ajustes generales!",
            [157] = "Salir del juego.\n¡Hasta la próxima!",
            [158] = "¡Descarga nuevos charts\ny contenido desde\n internet!",
            [159] = "¡Aprende sobre las funciones relacionadas\na OpenTaiko y como instalar\nnuevo contenido!",
            [160] = "¡Lucha contra una IA en\nmultiples secciones y\nhazte con la victoria!",
            [161] = "¡Revisa tu progreso!",
            [162] = "¡Crea tus propios charts .tja\ncon tus canciones favoritas!",
            [163] = "¡Usa varias herramientas para insertar\nnuevo contenido personalizado!",

            [200] = "Regresar",
            [201] = "Canciones jugadas recientemente",
            [202] = "¡Juega las canciones que jugaste recientemente!",
            [203] = "Canción aleatoria",

            [300] = "Monedas conseguidas !",
            [301] = "Personaje conseguido !",
            [302] = "Puchichara conseguido !",
            [303] = "Titulo conseguido !",
            [304] = "Aviso",
            [305] = "Confirmación",
            [306] = "Monedas",
            [307] = "Total",

            [400] = "Volver al menú principal",
            [401] = "Atras",
            [402] = "Descargar contenido",
            [403] = "Selecciona un CDN",
            [404] = "Descargar canciones",
            [405] = "Descargar personajes",
            [406] = "Descargar Puchicharas",
            [407] = "Multijugador en línea",

            [500] = "Timing",
            [501] = "Relajado",
            [502] = "Leve",
            [503] = "Normal",
            [504] = "Estricto",
            [505] = "Rigoroso",
            [510] = "Multiplicador de puntuación: ",
            [511] = "Multiplicador de monedas: ",
            [512] = "Tipo de juego",
            [513] = "Taiko",
            [514] = "Konga",
            [515] = "Fun mods",
            [516] = "Avalancha",
            [517] = "Buscaminas",

            [1000] = "Piso alcanzado",
            [1001] = "P",
            [1002] = "P",
            [1003] = "Puntuación",

            [1010] = "Indicador de almas",
            [1011] = "Cantidad de Perfectas",
            [1012] = "Cantidad de OK",
            [1013] = "Cantidad de Malas",
            [1014] = "Puntuación",
            [1015] = "Cantidad de redobles",
            [1016] = "Cantidad de golpes",
            [1017] = "Combo",
            [1018] = "Precisión",
            [1019] = "Cantidad de ADLIBs",
            [1020] = "Bombas golpeadas",

            [1030] = "Regresar",
            [1031] = "Puchi-Chara",
            [1032] = "Personaje",
            [1033] = "Título de Dan",
            [1034] = "Título de la placa",

            [1040] = "Fácil",
            [1041] = "Normal",
            [1042] = "Difícil",
            [1043] = "Extremo",
            [1044] = "Extra",
            [1045] = "Extremo / Extra",

            [90000] = "[ERROR] Condicion no valida",
            [90001] = "Item solamente disponible en la tienda.",
            [90002] = "Precio: ",
            [90003] = "Item comprado!",
            [90004] = "Monedas insuficientes!",
            [90005] = "Esta condición: ",

            [900] = "Reanudar",
            [901] = "Reiniciar",
            [902] = "Salir",

            [910] = "AI",
            [911] = "Deus-Ex-Machina",

            [9000] = "No",
            [9001] = "Sí",
            [9002] = "Nada",
            [9003] = "Caprichoso",
            [9004] = "Caótico",
            [9006] = "Modo Entrenamiento",
            [9007] = "-",
            [9008] = "Velocidad",
            [9009] = "Notas invisibles",
            [9010] = "Notas opuestas",
            [9011] = "Aleatorio",
            [9012] = "Modo de juego",
            [9013] = "Auto",
            [9014] = "Voz",
            [9015] = "Instrumento",
            [9016] = "Cauteloso",
            [9017] = "Seguro",
            [9018] = "Exacto",

            [9100] = "Buscar (Dificultad)",
            [9101] = "Dificultad",
            [9102] = "Nivel",

            [9200] = "Regresar",
            [9201] = "Ruta",
            [9202] = "Titulo",
            [9203] = "Subtitulo",
            [9204] = "Dificultad mostrada",
        };
    }
}
