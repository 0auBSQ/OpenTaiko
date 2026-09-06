<!-- api/songs.md -->

# Canciones y charts

Solicitar la lista de canciones, recorrer nodos y charts, leer puntuaciones y construir cursos dan (exámenes).

Convenciones usadas en esta página:

- Los índices de dificultad empiezan en 0: 0 Easy, 1 Normal, 2 Hard, 3 Extreme (`Oni`), 4 Extra Extreme (`Edit`), 5 Tower, 6 Dan.
- Los índices de jugador y de archivo de guardado empiezan en 0 (0 es el jugador 1).
- Los miembros escritos con punto (`node.Title`) son propiedades; los miembros escritos con dos puntos (`node:GetChart(3)`) son métodos.
- Algunos miembros devuelven colecciones C#. Una lista tiene `.Count` y se indexa desde 0 (`list[0]`); un array tiene `.Length` y también se indexa desde 0. Cada entrada de abajo indica cuál devuelve.
- La lista de canciones está completa solo después de que termine la enumeración de canciones. Solicítala desde el callback `afterSongEnum()` (consulta [Módulos y ciclo de vida](activities.md)), o comprueba antes la global `IsSongsEnumDone()`; devuelve true una vez que la enumeración se ha completado.

## Solicitar la lista de canciones

### RequestSongList

Función global que construye una lista de canciones navegable a partir de un objeto de ajustes.

<div class="callout warn">
Disponible como función global simple. Pásale un objeto de ajustes creado por GenerateSongListSettings(). La llamada construye el árbol de canciones una vez, a partir de las canciones que el juego ha enumerado; el handle conserva el objeto de ajustes por referencia, así que puedes cambiar un campo y llamar al ReloadSongList() del handle para reconstruirlo.
</div>

| Método | Descripción |
| --- | --- |
| `RequestSongList(settings)  -> song list handle` | Construye y devuelve un handle de lista de canciones a partir de los ajustes de lista de canciones indicados. |

```lua
local settings = GenerateSongListSettings()
settings.AppendMainRandomBox = false
settings:SetExcludedGenreFolders({ "Dan", "Tower" })

local list = RequestSongList(settings)
local node = list:GetSelectedSongNode()
```

### GenerateSongListSettings

Función global que crea un objeto de ajustes de lista de canciones con valores por defecto.

| Método | Descripción |
| --- | --- |
| `GenerateSongListSettings()  -> song list settings` | Devuelve un nuevo objeto de ajustes de lista de canciones con los valores de campo por defecto. |

### Ajustes de lista de canciones

Objeto de configuración que controla qué nodos incluye una lista de canciones y cómo se comporta la navegación.

<div class="callout warn">
Todos los miembros de abajo son campos públicos que Lua lee y escribe directamente (settings.HideEmptyFolders = false), excepto los dos métodos setter, que reciben una tabla Lua. ExcludedGenreFolders y MandatoryDifficultyList son arrays C#; establécelos mediante sus métodos setter.
</div>

| Método | Descripción |
| --- | --- |
| `settings.AppendMainRandomBox  (bool, default true)` | Cuando es true, la lista añade una casilla aleatoria a su raíz. |
| `settings.AppendSubRandomBoxes  (bool, default true)` | Cuando es true, la lista añade una casilla aleatoria al final de cada carpeta. |
| `settings.SubBackBoxFrequency  (int, default 7)` | Dentro de cada carpeta, la lista inserta una casilla de retorno al principio y después de cada N entradas; 0 desactiva las casillas de retorno generadas. |
| `settings.ExcludedGenreFolders  (string array)` | Nombres de carpetas de género que la lista deja fuera. Establécelo con SetExcludedGenreFolders. |
| `settings.RootGenreFolder  (string, default nil)` | Cuando está establecido, la raíz de la lista pasa a ser la primera carpeta (en profundidad) cuyo género coincide con este nombre; cuando es nil, la raíz es el nivel superior. |
| `settings.RootGenreFolderNode  (song node, default nil)` | Forma de nodo de RootGenreFolder. Cuando está establecido, tiene prioridad sobre la cadena, lo que desambigua carpetas que comparten nombre de género. |
| `settings.MandatoryDifficultyList  (Difficulty array, default nil)` | Dificultades que una canción debe tener para aparecer en la lista; nil significa sin requisito. Establécelo con SetMandatoryDifficultyList. |
| `settings.MandatoryDifficultyMatchAll  (bool, default true)` | true exige todas las dificultades listadas (AND); false exige al menos una (OR). |
| `settings.HideEmptyFolders  (bool, default true)` | Oculta las carpetas que no contienen ninguna canción visible, de forma recursiva. |
| `settings.FlattenOpenedFolders  (bool, default true)` | Cuando es true, la página actual es el árbol completo con las carpetas abiertas expandidas en su lugar (las carpetas cerradas cuentan como entradas únicas). Cuando es false, la página contiene solo los hermanos del nodo del cursor. |
| `settings.ModuloPagination  (bool, default true)` | Cuando es true, GetSongNodeAtOffset da la vuelta a la página; cuando es false devuelve nil más allá de cualquiera de los extremos. |
| `settings.ModuloMovement  (bool, default true)` | Cuando es true, Move da la vuelta a la página; cuando es false se detiene en cualquiera de los extremos. |
| `settings.ExcludeHiddenSongs  (bool, default true)` | Excluye las canciones cuyo HiddenIndex es 3 (oculta). |
| `settings.ExcludeLockedSongs  (bool, default false)` | Cuando es true, las páginas dejan fuera las canciones bloqueadas, así que la navegación nunca cae sobre ellas. |
| `settings.IgnoreUnlockables  (bool, default false)` | Cuando es true, la lista ignora ExcludeLockedSongs y GetRandomNodeInFolder puede devolver canciones bloqueadas. Las propiedades de nodo como IsLocked siguen reportando el estado real. |
| `settings:SetExcludedGenreFolders(table)  -> void` | Establece ExcludedGenreFolders a partir de una tabla Lua de cadenas de nombres de género. |
| `settings:SetMandatoryDifficultyList(table)  -> void` | Establece MandatoryDifficultyList a partir de una tabla Lua de índices de dificultad. |

### Handle de lista de canciones

Un árbol de canciones navegable devuelto por RequestSongList, con un cursor, navegación por carpetas y búsqueda.

<div class="callout warn">
Los métodos de búsqueda reciben una función Lua que recibe un nodo de canción y devuelve un booleano. Los métodos que devuelven varios nodos devuelven una lista C# (.Count, indexada desde 0).
</div>

| Método | Descripción |
| --- | --- |
| `list:ReloadSongList()  -> void` | Reconstruye todo el árbol a partir de las canciones y los ajustes actuales y mueve el cursor al primer nodo. |
| `list:GetRoot()  -> song node` | Devuelve el nodo raíz del árbol. |
| `list:GetSelectedSongNode()  -> song node` | Devuelve el nodo bajo el cursor, o nil cuando la lista está vacía. |
| `list:GetSongNodeAtOffset(offset)  -> song node` | Devuelve el nodo en el desplazamiento indicado respecto al cursor dentro de la página actual, dando la vuelta o devolviendo nil según ModuloPagination. |
| `list:Move(offset)  -> void` | Mueve el cursor el desplazamiento indicado dentro de la página actual, dando la vuelta o deteniéndose según ModuloMovement. |
| `list:OpenFolder()  -> bool` | Abre la carpeta bajo el cursor y mueve el cursor a su primer hijo; devuelve false si el cursor no está sobre una carpeta cerrada y no vacía. |
| `list:CloseFolder()  -> bool` | Cierra la carpeta que contiene el cursor y mueve el cursor a esa carpeta; devuelve false si no hay nada que cerrar. Salir de una carpeta virtual restaura el cursor guardado por OpenVirtualFolder. |
| `list:OpenVirtualFolder(baseFolder, songs, title)  -> bool` | Abre una carpeta temporal llamada `title` que contiene los nodos de canción de la tabla Lua `songs` (claves 1..n), con casillas de retorno generadas y una casilla aleatoria al final, y mueve el cursor dentro de ella. `baseFolder` pasa a ser el padre de la carpeta virtual. Devuelve false si la tabla no contiene nodos de canción. |
| `list:GetSongByUniqueId(id)  -> song node` | Devuelve la primera canción cuyo id único coincide, o nil. |
| `list:GetRandomNodeInFolder(node, recursive, predicate)  -> song node` | Elige una canción al azar entre los hermanos de `node` (la página que lo contiene). Con `recursive` (true por defecto), la elección también abarca las canciones dentro de las carpetas hermanas. Omite las canciones bloqueadas salvo que IgnoreUnlockables esté establecido. `predicate` es opcional. Devuelve nil cuando nada cumple los requisitos. |
| `list:SearchSongsByPredicate(predicate)  -> list of song nodes` | Devuelve todos los nodos de canción del árbol para los que el predicado devuelve true. |
| `list:SearchFirstSongByPredicate(predicate)  -> song node` | Devuelve el primer nodo de canción para el que el predicado devuelve true, o nil. |
| `list:SearchNodesByPredicate(predicate)  -> list of song nodes` | Como SearchSongsByPredicate, pero también evalúa las carpetas y los demás nodos que no son canciones. |

```lua
local results = list:SearchSongsByPredicate(function(node)
    return node:GetChart(3) ~= nil   -- tiene un chart Extreme
end)
for i = 0, results.Count - 1 do
    local node = results[i]
end
```

## Nodos, charts y puntuaciones

### Nodo de canción

Una única entrada de la lista de canciones: una canción, una carpeta, una casilla de retorno o una casilla aleatoria.

<div class="callout warn">
El handle de lista de canciones, SONGMOUNT:ChosenSongNode() y DANBUILDER:GetSong() devuelven nodos de canción. Las propiedades son de solo lectura. Las propiedades de metadatos devuelven nil en los nodos que no son canciones. Navega por la lista a través del handle de lista de canciones (Move, OpenFolder, CloseFolder y los métodos de búsqueda).
</div>

| Método | Descripción |
| --- | --- |
| `node.NotNull  (bool)` | Verdadero cuando el nodo envuelve una entrada real de la lista de canciones. |
| `node.IsFolder  (bool)` | Verdadero cuando el nodo es una carpeta. |
| `node.IsRandom  (bool)` | Verdadero cuando el nodo es una casilla aleatoria. |
| `node.IsReturn  (bool)` | Verdadero cuando el nodo es una casilla de retorno. |
| `node.IsSong  (bool)` | Verdadero cuando el nodo es una canción jugable. |
| `node.SongCount  (int)` | Número de canciones hijas directas. |
| `node.RecursiveSongCount  (int)` | Número de canciones bajo este nodo, incluidas las subcarpetas. |
| `node.VisibleSongCount  (int)` | Número de canciones hijas directas cuyo HiddenIndex no es 3. |
| `node.RecursiveVisibleSongCount  (int)` | Número de canciones visibles bajo este nodo, incluidas las subcarpetas. |
| `node.BoxType  (string)` | La cadena de estilo de casilla de la carpeta, o nil. |
| `node.BgType  (string)` | La cadena de estilo de fondo, o nil. |
| `node.BoxChara  (string)` | La cadena de personaje de la casilla, o nil. |
| `node.ForeColor  (color)` | El color de primer plano del nodo, o nil. |
| `node.BackColor  (color)` | El color de fondo del nodo, o nil. |
| `node.BoxColor  (color)` | El color de casilla del nodo, o nil. |
| `node.Title  (string)` | El título mostrado. Las casillas de retorno y aleatorias devuelven el texto localizado "Return" / "Random" construido a partir del título de la carpeta padre. |
| `node.Subtitle  (string)` | El subtítulo de la canción, o nil. |
| `node.Genre  (string)` | La cadena de género, o nil. |
| `node.UniqueId  (string)` | El id único de la canción, o nil. |
| `node.Maker  (string)` | El campo MAKER como una sola cadena, o nil. |
| `node.Charters  (string array)` | El campo MAKER dividido por comas. |
| `node.Side  (int)` | El valor SIDE: 0 normal, 1 ex, 2 ambos. |
| `node.Explicit  (bool)` | Verdadero cuando la canción está marcada como explícita; nil en los nodos que no son canciones. |
| `node.HasVideo  (bool)` | Verdadero cuando la canción tiene una película de fondo; nil en los nodos que no son canciones. |
| `node.DemoStart  (int)` | El desplazamiento del BGM de vista previa en milisegundos. |
| `node.AudioPath  (string)` | La ruta absoluta del archivo de BGM de la canción, o una cadena vacía. |
| `node.HasPreimage  (bool)` | Verdadero cuando la canción declara una preimagen. |
| `node.PreimagePath  (string)` | La ruta absoluta de la preimagen. Comprueba antes HasPreimage; sin preimagen esto es solo la carpeta de la canción. |
| `node:GetPreimage()  -> texture` | Carga la preimagen desde disco y devuelve una textura nueva, o nil si la canción no tiene ninguna. Libera la textura cuando termines con ella. |
| `node.ChartMd5  (string)` | MD5 del archivo de chart (hexadecimal en mayúsculas), o una cadena vacía. Se mantiene igual entre instalaciones; UniqueId no. |
| `node:GetChart(diff)  -> chart` | Devuelve el chart para el índice de dificultad indicado, o nil si la canción no tiene ese chart. |
| `node:GetCustomCommand(key)  -> string` | Devuelve el valor de un comando personalizado de ámbito global (una cabecera con prefijo de punto colocada antes del primer COURSE; la clave incluye el punto, por ejemplo ".VAULT_NAME"), o nil. |
| `node:GetCustomCommands()  -> dictionary` | Devuelve todos los comandos personalizados de ámbito global como un objeto diccionario C#. Prefiere GetCustomCommand para las búsquedas. |
| `node.UnlockCondition  (unlock condition)` | El objeto de condición de desbloqueo (consulta Condición de desbloqueo). |
| `node.UnlockText  (string)` | El texto de desbloqueo personalizado si la canción define uno; si no, el mensaje de condición generado. |
| `node.IsLocked  (bool)` | Verdadero cuando esta canción está bloqueada actualmente; siempre false en los nodos que no son canciones. |
| `node.HiddenIndex  (int)` | El estado de visualización del sistema de desbloqueo: 0 mostrada, 1 atenuada, 2 difuminada, 3 oculta (0 en los nodos que no son canciones). |
| `node.Rarity  (string)` | La etiqueta de rareza; "Common" para canciones sin entrada de desbloqueo, "-" para los nodos que no son canciones. |
| `node:Mount(p1diff, p2diff, p3diff, p4diff, p5diff)  -> bool` | Selecciona esta canción para jugar con el índice de dificultad indicado por jugador (0 por defecto). Comprueba solo los primeros CONFIG.PlayerCount índices y devuelve false si el nodo no es una canción o si falta o está fuera de rango la dificultad de un jugador activo. |
| `node:MountIfNotLocked(p1diff, p2diff, p3diff, p4diff, p5diff)  -> bool` | Igual que Mount, pero devuelve false sin montar cuando la canción está bloqueada. |

### Chart

Una dificultad de una canción: nivel, BPM, autores, datos de Torre y Dan, mejores puntuaciones y comandos personalizados.

<div class="callout warn">
El GetChart(diff) de un nodo de canción devuelve un chart. Las propiedades son de solo lectura. BPM, Life, TotalFloorCount, TowerType y DanTick devuelven nil cuando el chart no tiene información de chart. Difficulty y LevelIcon son objetos enum; compáralos mediante DifficultyAsInt, IsPlus e IsMinus.
</div>

| Método | Descripción |
| --- | --- |
| `chart.NotNull  (bool)` | Verdadero cuando el chart envuelve datos de chart reales. |
| `chart.Parent  (song node)` | El nodo de canción al que pertenece este chart. |
| `chart.Difficulty  (enum)` | La dificultad como objeto enum. |
| `chart.DifficultyAsInt  (int)` | El índice de dificultad. |
| `chart.Level  (int)` | El nivel de estrellas. |
| `chart.LevelDecimal  (number)` | El nivel con su parte fraccionaria (por ejemplo 12.888), o el nivel entero cuando no se definió ninguna. |
| `chart.LevelFirstDecimal  (int)` | El primer dígito decimal de LevelDecimal (0-9). |
| `chart.LevelIcon  (enum)` | El icono de nivel como objeto enum. |
| `chart.IsPlus  (bool)` | Verdadero cuando el icono de nivel es "plus". |
| `chart.IsMinus  (bool)` | Verdadero cuando el icono de nivel es "minus". |
| `chart.NotesDesigner  (string)` | El campo NOTESDESIGNER como una sola cadena. |
| `chart.Charters  (string array)` | El campo NOTESDESIGNER dividido por comas. |
| `chart.BPM  (number)` | El BPM principal, o nil. |
| `chart.BaseBPM  (number)` | El BPM base, o nil. |
| `chart.MinBPM  (number)` | El BPM mínimo, o nil. |
| `chart.MaxBPM  (number)` | El BPM máximo, o nil. |
| `chart.Life  (int)` | Número de vidas de la Torre, o nil. |
| `chart.TotalFloorCount  (int)` | Número de pisos de la Torre, o nil. |
| `chart.TowerType  (string)` | Cadena de tipo de Torre, o nil. |
| `chart.DanTick  (int)` | Valor de marca de la placa de dan, o nil. |
| `chart.DanTickColor  (color)` | Color de la marca de la placa de dan (blanco cuando el chart no tiene información de chart). |
| `chart.DanSongs  (array of dan songs)` | Las canciones que componen este chart Dan. |
| `chart.DanExams  (array of dan exams)` | Las condiciones de examen globales de este chart Dan. |
| `chart:GetSongExam(songIdx, examSlot)  -> dan exam` | Devuelve el examen por canción para el índice de canción (desde 1) y la ranura de examen (desde 1); el resultado tiene IsSet = false cuando no existe ninguno. |
| `chart:GetPlayerBestScore(save)  -> best score info` | Devuelve el resumen de la mejor partida del archivo de guardado indicado para este chart. |
| `chart:GetCustomCommand(key)  -> string` | Devuelve el valor de un comando personalizado de ámbito de chart (una cabecera con prefijo de punto dentro de este bloque COURSE; la clave incluye el punto), o nil. |
| `chart:GetCustomCommands()  -> dictionary` | Devuelve todos los comandos personalizados de ámbito de chart como un objeto diccionario C#. |
| `chart.SongFolder  (string)` | La carpeta absoluta que contiene el archivo de chart. |
| `chart.ChartPath  (string)` | La ruta absoluta del archivo de chart. |
| `chart.UniqueId  (string)` | El id único de la canción, o una cadena vacía. |
| `chart:Select(player)  -> bool` | Marca este chart como la dificultad elegida para el jugador indicado; el jugador 0 también establece la canción elegida. Devuelve false cuando el chart no es válido. |

### Información de mejor puntuación

El resumen de la mejor partida de un archivo de guardado para un chart.

<div class="callout warn">
chart:GetPlayerBestScore(save) devuelve este objeto. Todos los miembros son de solo lectura. Un índice de guardado inválido produce un registro vacío.
</div>

| Método | Descripción |
| --- | --- |
| `info.ScoreRank  (int)` | El mejor rango de puntuación alcanzado. |
| `info.ClearStatus  (int)` | El mejor estado de clear alcanzado. |
| `info.HighScore  (int)` | La puntuación máxima. |
| `info.HasBeenPlayed  (bool)` | Verdadero cuando el chart tiene al menos una partida registrada, sea cual sea el resultado. |
| `info.PlayCount  (int)` | Número total de partidas en este chart, todas las variantes de mods combinadas. |

## Exámenes dan

### Canción de dan

Una entrada de canción dentro de un curso Dan.

<div class="callout warn">
Elementos de chart.DanSongs. Todos los miembros son de solo lectura.
</div>

| Método | Descripción |
| --- | --- |
| `dansong.Title  (string)` | El título de la canción. |
| `dansong.SubTitle  (string)` | El subtítulo de la canción. |
| `dansong.Genre  (string)` | El género de la canción. |
| `dansong.Level  (int)` | El nivel de estrellas de la canción. |
| `dansong.Difficulty  (enum)` | La dificultad como objeto enum. |
| `dansong.DifficultyAsInt  (int)` | El índice de dificultad. |

### Examen dan

Una condición de aprobado/suspenso de un curso Dan.

<div class="callout warn">
Elementos de chart.DanExams, o devueltos por chart:GetSongExam(). Todos los miembros son de solo lectura. Valores de TypeAsInt: 0 medidor, 1 juicios perfectos, 2 juicios buenos, 3 juicios malos, 4 puntuación, 5 redobles, 6 golpes, 7 combo, 8 precisión, 9 juicios ad-lib, 10 juicios de mina. Valores de RangeAsInt: 0 "al menos", 1 "menos de".
</div>

| Método | Descripción |
| --- | --- |
| `danexam.IsSet  (bool)` | Verdadero cuando esta ranura de examen está activada. |
| `danexam.RedValue  (int)` | El umbral rojo (aprobado). |
| `danexam.GoldValue  (int)` | El umbral dorado. |
| `danexam.TypeAsInt  (int)` | El tipo de examen. |
| `danexam.RangeAsInt  (int)` | La dirección de la comparación. |

### DANBUILDER

Global para ensamblar en memoria un curso Dan a partir de nodos de canción, dificultades y condiciones de examen, y luego montarlo para jugar.

<div class="callout warn">
Disponible como la global DANBUILDER. Los índices de canción y de ranura empiezan en 1, excepto la dificultad que se pasa a AddSong, que es un índice de dificultad desde 0. Las ranuras de examen van de 1 a 7. Cadenas de tipo de examen (sin distinguir mayúsculas, forma corta entre paréntesis): "judgeperfect" (jp), "judgegood" (jg), "judgebad" (jb), "score" (s), "roll" (r), "hit" (h), "combo" (c), "accuracy" (a), "judgeadlib" (ja), "judgemine" (jm); cualquier otra cadena significa medidor. lessThan = true convierte el examen en una comprobación de "menos de", false en una de "al menos". El constructor conserva su estado entre llamadas; llama a Clear() antes de construir un curso nuevo.
</div>

| Método | Descripción |
| --- | --- |
| `DANBUILDER.SongCount  (int)` | Número de canciones añadidas hasta ahora. |
| `DANBUILDER:AddSong(node, diff)  -> void` | Añade un nodo de canción con el índice de dificultad indicado (desde 0). |
| `DANBUILDER:GetSong(i)  -> song node` | Devuelve el nodo de canción en el índice i (desde 1), o nil. |
| `DANBUILDER:GetSongDiff(i)  -> int` | Devuelve el índice de dificultad guardado para la canción en el índice i (desde 1), o -1. |
| `DANBUILDER:SetTitle(title)  -> void` | Establece el título del curso ("Dynamic Dan" por defecto). |
| `DANBUILDER:SetSubtitle(subtitle)  -> void` | Establece el subtítulo del curso. |
| `DANBUILDER:SetDanTick(tick)  -> void` | Establece el valor de marca de la placa de Dan (2 por defecto). |
| `DANBUILDER:SetDanTickColor(r, g, b)  -> void` | Establece el color de la marca de la placa de Dan a partir de componentes 0-255 (blanco por defecto). |
| `DANBUILDER:SetGlobalExam(slot, type, red, gold, lessThan)  -> void` | Establece un examen de todo el curso en la ranura indicada. |
| `DANBUILDER:SetPerSongExam(songIndex, slot, type, red, gold, lessThan)  -> void` | Establece un examen que se aplica a una canción, por índice de canción (desde 1) y ranura. |
| `DANBUILDER:Clear()  -> void` | Elimina todas las canciones y exámenes y reinicia los metadatos a sus valores por defecto. |
| `DANBUILDER:Mount()  -> bool` | Construye el chart del curso en memoria y lo selecciona para jugar en la dificultad Dan para el jugador 1; devuelve false si el constructor no contiene ninguna canción o la construcción falló. |

```lua
DANBUILDER:Clear()
DANBUILDER:SetTitle("Custom course")
DANBUILDER:AddSong(list:GetSongByUniqueId(id1), 3)
DANBUILDER:AddSong(list:GetSongByUniqueId(id2), 3)
DANBUILDER:SetGlobalExam(1, "gauge", 90, 100, false)
DANBUILDER:SetPerSongExam(2, 2, "judgebad", 10, 5, true)
if DANBUILDER:Mount() then
    return Exit("play")
end
```

## Desbloqueos, ranuras virtuales e iconos de mods

### Condición de desbloqueo

Describe qué desbloquea una canción y si un jugador cumple actualmente la condición.

<div class="callout warn">
node.UnlockCondition devuelve este objeto. HasCondition es una propiedad; el resto son métodos. Las canciones sin entrada de desbloqueo reportan HasCondition = false e IsUnlockable = true.
</div>

| Método | Descripción |
| --- | --- |
| `cond.HasCondition  (bool)` | Verdadero cuando la canción tiene una condición de desbloqueo explícita. |
| `cond:GetConditionMessage()  -> string` | Devuelve la descripción legible de la condición, o una cadena vacía. |
| `cond:GetConditionType()  -> string` | Devuelve el id del tipo de condición (por ejemplo "ch", "cs", "gt", "gc", "ig"), o una cadena vacía. |
| `cond:GetCoinPrice()  -> int` | Devuelve el coste en monedas, o 0. |
| `cond:IsUnlockable(player)  -> bool` | Devuelve true cuando el jugador indicado cumple la condición. |
| `cond:GetBlockedMessage(player)  -> string` | Devuelve por qué el jugador no cumple la condición, o una cadena vacía cuando la cumple. |

### VIRTUALSLOTS

Global para leer y escribir las cinco ranuras de personaje virtuales (V1-V5) y para redirigir un puesto de jugador de modo que muestre los visuales de una ranura.

<div class="callout warn">
Disponible como la global VIRTUALSLOTS. Los índices de ranura van de 1 a 5; los setters ignoran los índices fuera de rango y los getters devuelven los valores por defecto para ellos. Estos métodos no escriben nada en disco. El motor gestiona la ranura de la IA, que esta global no puede editar.
</div>

| Método | Descripción |
| --- | --- |
| `VIRTUALSLOTS:GetCharacter(slot)  -> string` | Devuelve el nombre de carpeta del personaje de la ranura, o "None". |
| `VIRTUALSLOTS:SetCharacter(slot, folderName)  -> void` | Establece el nombre de carpeta del personaje de la ranura. |
| `VIRTUALSLOTS:GetPuchichara(slot)  -> string` | Devuelve el nombre de carpeta del puchichara de la ranura, o "None". |
| `VIRTUALSLOTS:SetPuchichara(slot, folderName)  -> void` | Establece el nombre de carpeta del puchichara de la ranura. |
| `VIRTUALSLOTS:GetNameplateName(slot)  -> string` | Devuelve el nombre de jugador de la placa de nombre de la ranura, o "VSlot". |
| `VIRTUALSLOTS:SetNameplateName(slot, name)  -> void` | Establece el nombre de jugador de la placa de nombre de la ranura. |
| `VIRTUALSLOTS:GetNameplateTitle(slot)  -> string` | Devuelve el texto del título de la placa de nombre de la ranura. |
| `VIRTUALSLOTS:SetNameplateTitle(slot, title)  -> void` | Establece el texto del título de la placa de nombre de la ranura. |
| `VIRTUALSLOTS:GetNameplateDan(slot)  -> string` | Devuelve el texto de dan de la placa de nombre de la ranura. |
| `VIRTUALSLOTS:SetNameplateDan(slot, dan)  -> void` | Establece el texto de dan de la placa de nombre de la ranura. |
| `VIRTUALSLOTS:SetNameplateById(slot, nameplateId)  -> void` | Aplica una placa de nombre de la base de datos de placas por id: establece el texto del título, el tipo y la rareza. Un id desconocido solo registra el id. |
| `VIRTUALSLOTS:SetNameplateType(slot, type)  -> void` | Establece directamente el tipo de título de la placa de nombre (índice de estilo). |
| `VIRTUALSLOTS:SetNameplateRarity(slot, rarity)  -> void` | Establece directamente el índice de rareza del título de la placa de nombre. |
| `VIRTUALSLOTS:SetNameplateDanType(slot, danType)  -> void` | Establece el tipo de la placa de dan. |
| `VIRTUALSLOTS:SetNameplateDanGold(slot, gold)  -> void` | Establece si la placa de dan aparece en dorado. |
| `VIRTUALSLOTS:MountSlot(playerSpot, slotInfo)  -> void` | Hace que el puesto de jugador 1-5 muestre los visuales de `slotInfo`: "1P"-"5P" (el archivo de guardado de un jugador), "AI", o "V1"-"V5". La sustitución dura hasta la siguiente llamada a MountSlot para ese puesto. |

### MODICONS

Global para dibujar los iconos de mods activos de un jugador en una posición de pantalla.

<div class="callout warn">
Disponible como la global MODICONS. La ROActivity modicons se encarga del dibujo; la primera llamada a Draw la activa.
</div>

| Método | Descripción |
| --- | --- |
| `MODICONS:Draw(player, x, y, alpha)  -> void` | Dibuja los iconos de mods del jugador indicado en (x, y) usando la disposición de menú; alpha es opcional (255 por defecto). |

## Repeticiones y la canción seleccionada

### REPLAY

Global para listar las repeticiones guardadas de un chart e iniciar la reproducción de una.

<div class="callout warn">
Disponible como la global REPLAY. ListReplays devuelve un array C# de cabeceras de repetición (.Length, desde 0). Watch carga una repetición y prepara la reproducción solo para la siguiente partida; el juego aplica los mods de la repetición en memoria y restaura después los mods anteriores. songFolder y chartPath provienen del SongFolder y ChartPath de un chart.
</div>

| Método | Descripción |
| --- | --- |
| `REPLAY:ListReplays(songFolder, uniqueId, difficulty, topN, chartPath)  -> array of replay headers` | Devuelve hasta topN repeticiones del chart y la dificultad, ordenadas por puntuación. chartPath permite al listado calcular ChecksumMismatch. |
| `REPLAY:ListReplays(songFolder, uniqueId, difficulty, topN)  -> array of replay headers` | Igual sin ruta de chart (el listado omite ChecksumMismatch). |
| `REPLAY:ListReplaysAsync(songFolder, uniqueId, difficulty, topN, chartPath)  -> replay list handle` | Ejecuta el mismo listado en un hilo en segundo plano y devuelve un handle para consultar. |
| `REPLAY:Watch(filepath, chartPath)  -> bool` | Carga el archivo de repetición y prepara la reproducción para la siguiente partida; devuelve false cuando el archivo no consigue cargarse o la repetición no se puede ver. chartPath activa los avisos de "repetición inválida" en el juego. |
| `REPLAY:Watch(filepath)  -> bool` | Igual sin ruta de chart. |
| `REPLAY.MODFLAG  (object)` | Valores de bit para ModFlags: None (0), Mirror (1), Random (2), SuperRandom (4), Invisible (8), PerfectMemory (16), Avalanche (32), Minesweeper (64), Just (128), Safe (256), DynamicBeat (512). |

```lua
local flags = header.ModFlags
local mirrored = (flags & REPLAY.MODFLAG.Mirror) ~= 0
```

### Handle de lista de repeticiones

Handle devuelto por REPLAY:ListReplaysAsync.

<div class="callout warn">
Consulta IsDone cada fotograma; una vez que es true, lee Result.
</div>

| Método | Descripción |
| --- | --- |
| `handle.IsDone  (bool)` | Verdadero una vez que el listado en segundo plano ha terminado. |
| `handle.Result  (array of replay headers)` | Las repeticiones listadas (vacío hasta que IsDone es true). |

### Cabecera de repetición

Metadatos de una repetición guardada.

<div class="callout warn">
Elementos del array devuelto por REPLAY:ListReplays o del Result de un handle de lista de repeticiones. Todos los miembros son de solo lectura.
</div>

| Método | Descripción |
| --- | --- |
| `rep.FilePath  (string)` | La ruta absoluta del archivo de repetición; pásala a REPLAY:Watch. |
| `rep.PlayerName  (string)` | El nombre del jugador que grabó la repetición. |
| `rep.Score  (int)` | La puntuación final. |
| `rep.ClearStatus  (int)` | El estado de clear de la partida. |
| `rep.ScoreRank  (int)` | El rango de puntuación de la partida. |
| `rep.Good  (int)` | Número de juicios Good (perfecto). |
| `rep.Ok  (int)` | Número de juicios Ok. |
| `rep.Bad  (int)` | Número de juicios Bad (fallo). |
| `rep.Roll  (int)` | Número de golpes de redoble. |
| `rep.MaxCombo  (int)` | Combo máximo. |
| `rep.Boom  (int)` | Número de minas golpeadas. |
| `rep.ADLib  (int)` | Número de golpes ad-lib. |
| `rep.ModFlags  (int)` | Máscara de bits de los mods usados (consulta REPLAY.MODFLAG). |
| `rep.ScrollSpeed  (int)` | El ajuste de velocidad de desplazamiento de la partida. |
| `rep.SongSpeed  (int)` | El ajuste de velocidad de canción de la partida. |
| `rep.JudgeStrictness  (int)` | El ajuste de ventana de juicio de la partida. |
| `rep.Date  (string)` | La fecha de la partida con formato "yyyy-MM-dd HH:mm". |
| `rep.Timestamp  (int)` | La fecha de la partida como ticks en bruto. |
| `rep.ChartUniqueID  (string)` | El id único del chart. |
| `rep.ChartDifficulty  (int)` | El índice de dificultad de la partida grabada. |
| `rep.ChartChecksum  (string)` | El MD5 del chart guardado con la repetición. |
| `rep.RandomSeed  (int)` | La semilla de mezcla de notas, o -1 cuando el archivo no guarda ninguna. |
| `rep.GameMode  (int)` | El modo de juego de la partida grabada. |
| `rep.GameVersion  (int)` | La versión del juego que grabó la repetición. |
| `rep.Watchable  (bool)` | Verdadero cuando el juego puede reproducir la repetición fielmente. |
| `rep.UnwatchableReason  (string)` | Por qué la repetición no se puede ver, cuando Watchable es false. |
| `rep.OldVersion  (bool)` | Verdadero cuando una versión anterior del juego grabó la repetición. |
| `rep.ChecksumMismatch  (bool)` | Verdadero cuando el archivo de chart ya no coincide con la grabación (se calcula solo cuando pasas una ruta de chart). |

### SONGMOUNT

Global de solo lectura para la canción seleccionada actualmente para jugar.

<div class="callout warn">
Disponible como la global SONGMOUNT. Refleja el estado establecido por el Mount() de un nodo de canción, el Select() de un chart o DANBUILDER:Mount().
</div>

| Método | Descripción |
| --- | --- |
| `SONGMOUNT:ChosenUniqueId()  -> string` | Devuelve el id único de la canción seleccionada, o una cadena vacía. |
| `SONGMOUNT:ChosenDifficulty()  -> int` | Devuelve el índice de dificultad seleccionado para el jugador 1. |
| `SONGMOUNT:ChosenSongNode()  -> song node` | Devuelve la canción seleccionada como nodo de canción (sin hijos), o nil cuando no hay nada seleccionado. |
