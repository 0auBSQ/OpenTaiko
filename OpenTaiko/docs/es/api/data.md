<!-- api/data.md -->

# Datos y persistencia

Guardar datos que sobreviven a un reinicio, consultar archivos SQLite, leer archivos, JSON e INI desde la carpeta del módulo, compartir recursos entre módulos, y leer o cambiar la configuración del juego.

Las rutas relativas que se pasan a DATABASE, SQL, STORAGE, JSONLOADER, INILOADER y SHARED se resuelven respecto al directorio del módulo en ejecución (la carpeta que contiene su `Script.lua`).

Algunos métodos de esta página devuelven colecciones .NET, que se comportan de forma distinta a las tablas Lua:

- Los arrays (`string[]`, `int[]`, `double[]`) empiezan en el índice 0 y exponen `.Length`.
- Los diccionarios (JSON analizado, filas SQL, mapas de idioma) se indexan con `d["key"]` (o `d[1]` para arrays analizados desde JSON) y se enumeran mediante `d:GetEnumerator()`; `pairs` y `#` no funcionan con ellos.

```lua
local files = STORAGE:GetFiles("maps", "*.json")
for i = 0, files.Length - 1 do
    print(files[i])
end

local e = dict:GetEnumerator()
while e:MoveNext() do
    print(e.Current.Key, e.Current.Value)
end
```

## Base de datos clave-valor

### DATABASE

Abre almacenes clave/valor LMDB que conservan valores de cadena entre reinicios, ya sea dentro de la carpeta del módulo o en la carpeta de datos global del juego.

<div class="callout warn">
Cada Read y Write abre y cierra su propio entorno LMDB, así que cada llamada es costosa; guarda los valores en caché en Lua y actualiza la caché cuando escribas. Dentro de los módulos de solo lectura (ROActivities y fondos) DATABASE devuelve almacenes cuyo Write registra un error y no hace nada.
</div>

| Método | Descripción |
| --- | --- |
| `DATABASE:OpenLocalDatabase(path)  -> database` | Abre (creándolo si es necesario) un almacén en una ruta relativa al directorio del módulo. |
| `DATABASE:OpenGlobalDatabase(path)  -> database` | Abre (creándolo si es necesario) un almacén dentro de `Global/ApplicationData/LMDB/` en la carpeta del juego, compartido por todos los módulos. |

### Handle de base de datos

Un almacén clave/valor devuelto por DATABASE, que asigna claves de cadena a valores de cadena.

<div class="callout warn">
Los valores son solo cadenas; convierte los números y booleanos tú mismo (por ejemplo con tostring y tonumber).
</div>

| Método | Descripción |
| --- | --- |
| `database:Write(key, value)  -> nil` | Guarda una cadena bajo la clave y la confirma. Registra un error y no hace nada en los módulos de solo lectura. |
| `database:Read(key)  -> string` | Devuelve la cadena guardada bajo la clave, o nil si la clave no existe o la lectura falla. |
| `database:Dispose()  -> nil` | No hace nada; el handle no mantiene recursos abiertos. |

```lua
local db

function activate()
    db = DATABASE:OpenGlobalDatabase("GameStatus")
    if db:Read("new_user") == nil then
        db:Write("new_user", "true")
    end
end
```

## Base de datos SQL

### SQL

Abre un archivo de base de datos SQLite dentro del directorio del módulo para ejecutar sentencias SQL.

| Método | Descripción |
| --- | --- |
| `SQL:OpenSQLDatabase(path)  -> sql` | Abre una base de datos SQLite en una ruta relativa al directorio del módulo. |

### Handle SQL

Una conexión SQLite devuelta por SQL:OpenSQLDatabase.

<div class="callout warn">
Query devuelve un diccionario con claves 1..n; cada fila es un diccionario con claves por nombre de columna (consulta la nota sobre colecciones .NET al principio de la página). Una sentencia fallida registra el error y devuelve un resultado vacío. Query pasa el texto de la sentencia a SQLite sin cambios, sin vinculación de parámetros, así que escapa cualquier valor que insertes en ella.
</div>

| Método | Descripción |
| --- | --- |
| `sql:Query(query)  -> rows` | Ejecuta el texto SQL y devuelve las filas del resultado. |

```lua
local db

function activate()
    db = SQL:OpenSQLDatabase("Databases/Items.db3")
    local rows = db:Query("SELECT * FROM itempool WHERE Slot = 'regular'")
    for i = 1, rows.Count do
        print(rows[i]["Code"])
    end
end
```

## Archivos, JSON e INI

### STORAGE

Acceso a archivos con raíz en el directorio del módulo, más utilidades para compartir códigos de sala en línea.

<div class="callout warn">
WriteText solo escribe dentro de la carpeta del módulo: rechaza rutas absolutas y rutas que se resuelvan fuera de ella. ReadText también acepta una ruta absoluta. Las utilidades de códigos de sala usan la carpeta compartida `Global/Lobbycodes/` junto al ejecutable. Los módulos de solo lectura pueden usar todos los métodos de STORAGE.
</div>

| Método | Descripción |
| --- | --- |
| `STORAGE:GetFiles(dir, pattern)  -> string[]` | Lista los archivos de un subdirectorio de la carpeta del módulo que coinciden con un patrón de búsqueda como `"*.png"`. Las entradas son rutas relativas a la carpeta del módulo, incluido el subdirectorio. El directorio debe existir. |
| `STORAGE:FileExists(path)  -> bool` | Verdadero si existe un archivo en la ruta relativa a la carpeta del módulo. |
| `STORAGE:DirectoryExists(path)  -> bool` | Verdadero si existe un directorio en la ruta relativa a la carpeta del módulo. |
| `STORAGE:WriteText(name, contents)  -> bool` | Escribe texto en un archivo dentro de la carpeta del módulo, creando subdirectorios. Devuelve false para rutas absolutas o que escapen de la carpeta, o en caso de fallo. |
| `STORAGE:ReadText(name)  -> string` | Devuelve el texto en bruto de un archivo (relativo a la carpeta del módulo, o absoluto), o nil si no existe o no se puede leer. |
| `STORAGE:GetFullPath(name)  -> string` | Devuelve la ruta absoluta de un archivo dentro de la carpeta del módulo, o nil para una entrada vacía o absoluta. |
| `STORAGE:WriteLobbyCode(name, contents)  -> bool` | Escribe un archivo en `Global/Lobbycodes/`. Devuelve false para nombres vacíos, absolutos o con `..`, o en caso de fallo. |
| `STORAGE:RevealLobbyCodes()  -> bool` | Abre el explorador de archivos del sistema en `Global/Lobbycodes/`. |
| `STORAGE:RevealInExplorer(name)  -> bool` | Abre el explorador de archivos del sistema con el archivo del módulo indicado seleccionado (Windows y macOS), o su carpeta en otros sistemas. |

### JSONLOADER

Analiza archivos y cadenas JSON desde el directorio del módulo.

<div class="callout warn">
Los valores analizados son diccionarios .NET: los objetos se indexan por nombre de miembro, los arrays con claves 1..n. Indexar directamente una clave ausente (`d["x"]`) lanza un error, así que usa JsonGet para búsquedas que puedan fallar. Los números se convierten en enteros o dobles; las cadenas, booleanos y null se asignan a sus equivalentes Lua. LoadJson devuelve un árbol JsonNode; indéxalo con `node["member"]` y convierte las hojas con ExtractNumber / ExtractText.
</div>

| Método | Descripción |
| --- | --- |
| `JSONLOADER:LoadJson(name)  -> JsonNode` | Analiza un archivo JSON relativo al directorio del módulo en un árbol JsonNode. Lanza un error si el archivo no existe. |
| `JSONLOADER:ExtractNumber(value)  -> number` | Convierte una hoja JsonNode a número; devuelve 0 para nil o valores no numéricos. |
| `JSONLOADER:ExtractText(value)  -> string` | Convierte una hoja JsonNode a cadena; devuelve nil para nil. |
| `JSONLOADER:JsonParseFile(name)  -> dict` | Analiza un archivo JSON cuya raíz es un objeto (diccionario vacío si el archivo está en blanco). Lanza un error si el archivo no existe o la raíz no es un objeto. |
| `JSONLOADER:JsonParseFileAny(name)  -> dict` | Analiza un archivo JSON cuya raíz es un objeto o un array (ruta relativa o absoluta). Devuelve nil si el archivo no existe o está en blanco. |
| `JSONLOADER:JsonParseString(json)  -> dict` | Analiza una cadena JSON cuya raíz es un objeto (diccionario vacío si está en blanco). Lanza un error si la raíz no es un objeto. |
| `JSONLOADER:JsonParseStringAny(json)  -> dict` | Analiza una cadena JSON cuya raíz es un objeto o un array. Devuelve nil para una entrada en blanco o inválida. |
| `JSONLOADER:JsonGet(dict, key)  -> value` | Busca una clave de cadena en un objeto o una clave entera en un array; devuelve nil cuando no existe o cuando dict no es un valor analizado. |
| `JSONLOADER:JsonCount(dict)  -> int` | Número de miembros de un objeto o array analizado, o 0 para cualquier otra cosa. |

```lua
local data = JSONLOADER:JsonParseFileAny("Config/layout.json")
local colors = data and JSONLOADER:JsonGet(data, "colors")
local n = JSONLOADER:JsonCount(colors)
for i = 1, n do
    print(JSONLOADER:JsonGet(colors, i))
end
```

### INILOADER

Carga un archivo plano `key=value` desde el directorio del módulo.

<div class="callout warn">
El cargador divide cada línea en el primer `=`, omite las líneas sin uno y deja que una clave repetida sobrescriba el valor anterior. No tiene secciones, comentarios ni comillas. Un archivo ausente produce un handle vacío.
</div>

| Método | Descripción |
| --- | --- |
| `INILOADER:LoadIni(name)  -> ini` | Lee un archivo `key=value` relativo al directorio del módulo. |

### Handle INI

Un archivo INI analizado devuelto por INILOADER:LoadIni con getters tipados.

<div class="callout warn">
Los getters devuelven el valor por defecto indicado cuando la clave no existe. Cuando la clave existe pero su valor no se puede analizar, los getters numéricos devuelven 0. Los getters de array dividen por comas y devuelven un array vacío cuando la clave no existe.
</div>

| Método | Descripción |
| --- | --- |
| `ini:GetBool(key, default)  -> bool` | Verdadero cuando el valor se analiza como el entero 1; el valor por defecto cuando la clave no existe. |
| `ini:GetInt(key, default)  -> int` | El valor como entero. |
| `ini:GetDouble(key, default)  -> number` | El valor como doble. |
| `ini:GetString(key, default)  -> string` | El valor de cadena en bruto. |
| `ini:GetStringArray(key)  -> string[]` | El valor dividido por comas. |
| `ini:GetIntArray(key)  -> int[]` | El valor dividido por comas, cada parte analizada como entero (0 si no se puede analizar). |
| `ini:GetDoubleArray(key)  -> double[]` | El valor dividido por comas, cada parte analizada como doble (0 si no se puede analizar). |

## Recursos compartidos y configuración

### SHARED

Un almacén global de texturas, sonidos y cadenas que sobrevive a los cambios de stage, de modo que cualquier módulo puede usar un recurso cargado una vez (por ejemplo en el arranque).

<div class="callout warn">
Los métodos Set* cargan en un hilo en segundo plano e intercambian el recurso en el hilo de renderizado; el callback opcional onCreate recibe el nuevo handle una vez colocado. Un Set* más reciente sobre la misma clave descarta cualquier carga todavía en curso. El intercambio libera el recurso anterior, lo que invalida cualquier handle obtenido antes de la recarga; vuelve a obtenerlo con Get*. Las variantes UsingAbsolutePath reciben una ruta completa. Consulta Gráficos y texto para los handles de textura y Audio para los handles de sonido.
</div>

| Método | Descripción |
| --- | --- |
| `SHARED:SetSharedString(key, value)  -> nil` | Guarda una cadena bajo una clave. |
| `SHARED:GetSharedString(key)  -> string` | Devuelve la cadena guardada bajo una clave, o una cadena vacía. |
| `SHARED:GetSharedTexture(key)  -> texture` | Devuelve la textura compartida de una clave, o una textura vacía si no se estableció ninguna. |
| `SHARED:GetSharedSound(key)  -> sound` | Devuelve el sonido compartido de una clave, o un sonido vacío si no se estableció ninguno. |
| `SHARED:ClearSharedTexture(key)  -> nil` | Libera la textura guardada bajo una clave y la reemplaza por una vacía. |
| `SHARED:ClearSharedSound(key)  -> nil` | Libera el sonido guardado bajo una clave y lo reemplaza por uno vacío. |
| `SHARED:SetSharedTexture(key, path, onCreate?)  -> nil` | Carga una textura en el almacén desde una ruta relativa al módulo. |
| `SHARED:SetSharedTexture(key, path, options, onCreate?)  -> nil` | Igual, con una tabla de opciones; `{ maxSize = N }` limita el lado más largo de la textura decodificada a N píxeles. |
| `SHARED:SetSharedTextureUsingAbsolutePath(key, path, onCreate?)  -> nil` | Carga una textura desde una ruta absoluta. |
| `SHARED:SetSharedTextureUsingAbsolutePath(key, path, options, onCreate?)  -> nil` | Carga una textura desde una ruta absoluta con una tabla de opciones. |
| `SHARED:SetSharedSFX(key, path, onCreate?)  -> nil` | Carga un efecto de sonido desde una ruta relativa al módulo. |
| `SHARED:SetSharedBGM(key, path, onCreate?)  -> nil` | Carga música de fondo (grupo de volumen de reproducción de canción) desde una ruta relativa al módulo. |
| `SHARED:SetSharedVoice(key, path, onCreate?)  -> nil` | Carga un clip de voz desde una ruta relativa al módulo. |
| `SHARED:SetSharedPreview(key, path, onCreate?)  -> nil` | Carga un clip de vista previa de canción desde una ruta relativa al módulo. |
| `SHARED:SetSharedSFXUsingAbsolutePath(key, path, onCreate?)  -> nil` | Carga un efecto de sonido desde una ruta absoluta. |
| `SHARED:SetSharedBGMUsingAbsolutePath(key, path, onCreate?)  -> nil` | Carga música de fondo desde una ruta absoluta. |
| `SHARED:SetSharedVoiceUsingAbsolutePath(key, path, onCreate?)  -> nil` | Carga un clip de voz desde una ruta absoluta. |
| `SHARED:SetSharedPreviewUsingAbsolutePath(key, path, onCreate?)  -> nil` | Carga un clip de vista previa de canción desde una ruta absoluta. |

```lua
-- en el stage de arranque
function onStart()
    SHARED:SetSharedSFX("Decide", "Sounds/Decide.ogg")
end

-- en cualquier módulo posterior
SHARED:GetSharedSound("Decide"):Play()
```

### CONFIG

Lee y cambia la configuración del juego: número de jugadores, modos, puntuación, mods de juego por jugador y niveles de volumen.

<div class="callout warn">
Las propiedades usan la sintaxis de punto (`CONFIG.PlayerCount`), los métodos la sintaxis de dos puntos. Los índices de jugador empiezan en 0 (0 a 4); los setters ignoran los índices fuera de rango y los getters devuelven un valor por defecto para ellos. Dentro de los módulos de solo lectura (ROActivities y fondos) todos los setters registran un error y no hacen nada. Los cambios se aplican de inmediato en memoria; el juego escribe Config.ini cuando se cierra normalmente.
</div>

| Método | Descripción |
| --- | --- |
| `CONFIG.ConfigIsNew  -> bool (read-only)` | Verdadero cuando el juego creó la configuración en este arranque. |
| `CONFIG.Language  -> string (read-only)` | El id de idioma guardado en Config.ini. |
| `CONFIG.PlayerCount  -> int` | Número de jugadores activos. El setter ignora los valores fuera de 1..5. |
| `CONFIG.IsAIBattleMode  -> bool` | Si el modo de batalla contra la IA está activado. |
| `CONFIG.AILevel  -> int` | Nivel de dificultad de la IA, limitado a 1..10 al escribir. |
| `CONFIG.IsTrainingMode  -> bool` | Si el modo de entrenamiento está activado. |
| `CONFIG.UseModernScoringMethod  -> bool` | Si el juego usa el método de puntuación moderno (shin-uchi). |
| `CONFIG.UsedLegacyScoringMethod  -> int` | Generación de puntuación legacy (consulta `CONFIG.LEGACY_SCORING`), limitada a 0..3 al escribir. |
| `CONFIG.AreSongUnlockablesDisabled  -> bool (read-only)` | Si el juego ignora las condiciones de desbloqueo de canciones. |
| `CONFIG.SongSpeed  -> int` | Velocidad de la canción en veinteavos del multiplicador: 20 es 1.0x. Limitada a 2..200 (0.1x a 10x) al escribir. |
| `CONFIG.MasterVolume  -> int` | Volumen maestro, limitado a 0..100 al escribir. |
| `CONFIG.SoundEffectVolume  -> int` | Volumen de efectos de sonido, limitado a 0..100 al escribir. |
| `CONFIG.VoiceVolume  -> int` | Volumen de voz, limitado a 0..100 al escribir. |
| `CONFIG.SongVolume  -> int` | Volumen de reproducción de canción, limitado a 0..100 al escribir. |
| `CONFIG.PreviewVolume  -> int` | Volumen de vista previa de canción, limitado a 0..100 al escribir. |
| `CONFIG:GetGameType(player)  -> int` | El tipo de juego del jugador (consulta `CONFIG.GAMETYPE`); Taiko para índices fuera de rango. |
| `CONFIG:SetGameType(player, gameType)  -> nil` | Establece el tipo de juego del jugador e ignora los valores no definidos. |
| `CONFIG:GetDefaultCourse(player)  -> int` | La dificultad por defecto (consulta `CONFIG.DEFAULT_COURSE`); Normal para índices fuera de rango. Un único ajuste global se aplica a todos los jugadores a pesar del argumento player. |
| `CONFIG:SetDefaultCourse(player, difficulty)  -> nil` | Establece la dificultad global por defecto, limitada desde Easy hasta una más allá de Extra Extreme (la visualización combinada Extra/Extra-Extra). |
| `CONFIG:GetScrollSpeed(player)  -> int` | El valor de velocidad de desplazamiento del jugador: 9 es 1.0x, cada paso es 0.1x (consulta `CONFIG.SCROLLSPEED`). 9 para índices fuera de rango. |
| `CONFIG:SetScrollSpeed(player, speed)  -> nil` | Establece el valor de velocidad de desplazamiento del jugador, limitado a 0..99. |
| `CONFIG:GetTimingZone(player)  -> int` | La ventana de juicio del jugador: 0 Loose, 1 Lenient, 2 Normal, 3 Strict, 4 Rigorous. 2 para índices fuera de rango. |
| `CONFIG:SetTimingZone(player, zone)  -> nil` | Establece la ventana de juicio del jugador, limitada a 0..4. |
| `CONFIG:GetAutoStatus(player)  -> bool` | Verdadero cuando el jugador está en auto-play o viendo una repetición. |
| `CONFIG:SetAutoStatus(player, isAuto)  -> nil` | Activa o desactiva el auto-play para el jugador. |
| `CONFIG:GetRandomMod(player)  -> int` | El mod aleatorio del jugador (consulta `CONFIG.RANDOM`); Off para índices fuera de rango. |
| `CONFIG:SetRandomMod(player, mode)  -> nil` | Establece el mod aleatorio del jugador e ignora los valores no definidos. |
| `CONFIG:GetFunMod(player)  -> int` | El mod fun del jugador (consulta `CONFIG.FUN`); None para índices fuera de rango. |
| `CONFIG:SetFunMod(player, mod)  -> nil` | Establece el mod fun del jugador e ignora los valores no definidos. |
| `CONFIG:GetStealthMod(player)  -> int` | El mod stealth del jugador (consulta `CONFIG.STEALTH`); Off para índices fuera de rango. |
| `CONFIG:SetStealthMod(player, mode)  -> nil` | Establece el mod stealth del jugador e ignora los valores no definidos. |
| `CONFIG:GetJusticeMod(player)  -> int` | El mod de juicio del jugador: 0 desactivado, 1 Just (Ok cuenta como Bad), 2 Safe (Bad cuenta como Ok). |
| `CONFIG:SetJusticeMod(player, mode)  -> nil` | Establece el mod de juicio del jugador, limitado a 0..2. |
| `CONFIG:GetModFlags(player)  -> integer` | Empaqueta la velocidad de desplazamiento, stealth, aleatorio, velocidad de canción, ventana de juicio, mod de juicio y mod fun en un único valor de 64 bits (un byte cada uno). |
| `CONFIG:SetModFlags(player, flags)  -> nil` | Aplica al jugador un valor producido por GetModFlags (la velocidad de la canción es global). |

Las tablas de constantes de CONFIG dan nombre a los valores enteros anteriores. `SONGSPEED` y `SCROLLSPEED` también convierten entre valores guardados y multiplicadores.

| Miembro | Descripción |
| --- | --- |
| `CONFIG.SONGSPEED.Normal  -> int` | 20, el valor guardado para 1.0x. |
| `CONFIG.SONGSPEED:ToActual(value)  -> number` | Convierte una velocidad de canción guardada a su multiplicador (value / 20). |
| `CONFIG.SONGSPEED:FromActual(multiplier)  -> int` | Convierte un multiplicador a la velocidad de canción guardada más cercana. |
| `CONFIG.SCROLLSPEED.Normal  -> int` | 9, el valor guardado para 1.0x. |
| `CONFIG.SCROLLSPEED:ToActual(value)  -> number` | Convierte una velocidad de desplazamiento guardada a su multiplicador ((value + 1) / 10). |
| `CONFIG.SCROLLSPEED:FromActual(multiplier)  -> int` | Convierte un multiplicador a la velocidad de desplazamiento guardada más cercana. |
| `CONFIG.GAMETYPE` | `Taiko`, `Konga`. |
| `CONFIG.DEFAULT_COURSE` | `Easy`, `Normal`, `Hard`, `Oni`, `Edit`. |
| `CONFIG.LEGACY_SCORING` | `Gen1Oni`, `Gen1_2`, `Gen2`, `Gen3`. |
| `CONFIG.RANDOM` | `Off`, `Random`, `Mirror`, `SuperRandom`, `MirrorRandom`. |
| `CONFIG.STEALTH` | `Off`, `Doron`, `Stealth`. |
| `CONFIG.FUN` | `None`, `Avalanche`, `Minesweeper`, `DynamicBeat`, `Total`. |
| `CONFIG.JUSTICE` | `None`, `Just`, `Safe`. |

```lua
local multiplier = CONFIG.SONGSPEED:ToActual(CONFIG.SongSpeed)
if CONFIG:GetRandomMod(0) == CONFIG.RANDOM.Mirror then
    -- chart en espejo
end
```
