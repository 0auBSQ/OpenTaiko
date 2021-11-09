using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            [0] = "Cambia el idioma que se usa\nen el juego y menús.",
            [1] = "Idioma del sistema",
            [2] = "<< Volver al menú",
            [3] = "Volver al menú de la izquierda.",
            [4] = "Recargar datos de canciones",
            [5] = "Recarga y actualiza la lista de canciones.",
            [6] = "Numero de jugadores",
            [7] = "Change the ingame player countF\nSetting it to 2 makes able to play\nregular charts at 2 players by splitting \nthe screen in half.",
            [8] = "Risky",
            [9] = "Risky mode:\nSet it over 1, in case you'd like to specify\n the number of Poor/Miss times to be\n FAILED.\nSet 0 to disable Risky mode.",
            [10] = "Velocidad de la canción",
            [11] = "Cambia la velocidad de la canción.\n" +
                "For example, you can play in half\n" +
                " speed by setting PlaySpeed = 0.500\n" +
                " for your practice.\n" +
                "\n" +
                "Nota: Tambien cambia el tono de la canción.\n" +
                "In case TimeStretch=ON, some audio\n" +
                "lag occurs if slower than x0.900.",
            [16] = "Tipo de interfaz",
            [17] = "Puedes cambiar la interfaz de las canciones \nmostradas en la pantalla de selección.\n" +
                "0 : Regular (Diagonal de arriba hacia abajo)\n" +
                "1 : Vertical\n" +
                "2 : Diagonal de abajo hacia arriba\n" +
                "3 : Medio circulo hacia la derecha\n" +
                "4 : Medio circulo hacia la izquierda",

            [100] = "Modo Taiko",
            [101] = "Dan-i Dojo",
            [102] = "Torres Taiko",
            [103] = "Tienda",
            [104] = "Aventura Taiko",
            [105] = "My Room",
            [106] = "Ajustes",
            [107] = "Salir",

            [150] = "Juega tus canciones\nfavoritas a tu propio gusto !",
            [151] = "Play multiple charts in continuation\nfollowing challenging exams\nin order to get a PASS rank !",
            [152] = "Play long charts within a limited\ncount of lives and reach\nthe top of the tower !",
            [153] = "Compra nuevas canciones, petit-chara o personajes\nusando las medallas que ganaste jugando !",
            [154] = "Surpass various obstacles and\nunlock new content and horizons !",
            [155] = "Cambia la informacion de tu placa\n o el aspecto de tu personaje !",
            [156] = "Cambia tu estilo de juego\n o ajustes generales !",
            [157] = "Salir del juego.\nHasta la proxima !",

            [1000] = "Piso alcanzado",
            [1001] = "F",
            [1002] = "P",
            [1003] = "Puntuación",
        };
    }
}
