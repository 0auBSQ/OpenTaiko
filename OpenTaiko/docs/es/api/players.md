<!-- api/players.md -->

# Jugadores y perfiles

Archivos de guardado, placas de nombre, personajes, puchicharas, estado de la partida, temas y el idioma actual.

Los índices de jugador empiezan en 0 (0 a 4) en toda esta página excepto en THEME:GetThemeSettingForPlayer, que empieza en 1. Los módulos de solo lectura (ROActivities y fondos) reciben un handle de archivo de guardado cuyos métodos de escritura registran un error y no hacen nada; todo lo demás aquí se comporta igual en todos los tipos de módulo.

## Archivos de guardado

### GetSaveFile

Función global que devuelve el handle de archivo de guardado de una ranura de jugador.

<div class="callout warn">
Llámala como función simple (`GetSaveFile(0)`). Un índice fuera de rango registra un error y devuelve nil. Cada llamada crea un handle nuevo que lee datos en vivo, así que no hay nada que guardar en caché ni liberar.
</div>

| Método | Descripción |
| --- | --- |
| `GetSaveFile(player)  -> saveFile` | Devuelve el handle de archivo de guardado de la ranura de jugador (desde 0), o nil si el índice está fuera de rango. |

### Handle de archivo de guardado

El perfil de un jugador: nombre, monedas, objetos desbloqueados, triggers y contadores, estadísticas de clear, personaje equipado, puchichara, placa de nombre y título de dan.

<div class="callout warn">
Lee las propiedades con sintaxis de punto (sf.Name, sf.Coins). Los métodos de escritura persisten de inmediato. Los módulos de solo lectura bloquean los siguientes: SpendCoins, EarnCoins, UnlockNameplate, UnlockSong, asignar SelectedHitsounds, SetGlobalTrigger, SetGlobalCounter, ChangeCharacter (devuelve false), UnlockPuchichara, ChangePuchichara, UnlockCharacter, ChangeDan, ChangeName y ChangeNameplate.
</div>

| Método | Descripción |
| --- | --- |
| `sf.Name  -> string` | El nombre mostrado del jugador. |
| `sf.SaveId  -> integer` | El id numérico de base de datos de este guardado. |
| `sf.SaveUID  -> string` | El id de cadena único de este guardado. |
| `sf.NameplateInfo  -> nameplateInfo` | La placa de nombre equipada (consulta Handle de información de placa de nombre), o la placa de principiante por defecto si el id guardado es desconocido. |
| `sf.DanplateInfo  -> danplateInfo` | El título de dan actual (consulta Handle de información de placa de dan). |
| `sf.TotalPlaycount  -> integer` | Número total de partidas en este guardado. |
| `sf.AIBattlePlaycount  -> integer` | Número de partidas de batalla contra la IA. |
| `sf.AIBattleWins  -> integer` | Número de victorias en batalla contra la IA. |
| `sf.Coins  -> integer` | Saldo actual de monedas. |
| `sf.TotalEarnedCoins  -> integer` | Total de monedas ganadas durante la vida del guardado. |
| `sf:SpendCoins(price)  -> nil` | Descuenta monedas (el saldo nunca baja de 0) y persiste. |
| `sf:EarnCoins(amount)  -> nil` | Añade monedas al saldo y al total ganado, y persiste. |
| `sf:IsNameplateUnlocked(id)  -> bool` | Si la placa de nombre con este id está desbloqueada. |
| `sf:UnlockNameplate(id)  -> nil` | Desbloquea una placa de nombre y persiste (sin efecto si ya está desbloqueada). |
| `sf:IsSongUnlocked(uniqueId)  -> bool` | Si la canción con este id único está desbloqueada. |
| `sf:UnlockSong(uniqueId)  -> nil` | Desbloquea una canción y persiste (sin efecto si ya está desbloqueada). |
| `sf.SelectedHitsounds  -> string` | Nombre de carpeta del conjunto de hitsounds seleccionado. Asignar un nombre distinto lo persiste y recarga los hitsounds del jugador. |
| `sf:GetGlobalTrigger(name)  -> bool` | Lee un trigger booleano con nombre. |
| `sf:GetGlobalCounter(name)  -> number` | Lee un contador numérico con nombre. |
| `sf:SetGlobalTrigger(name, value)  -> nil` | Establece un trigger booleano con nombre. |
| `sf:SetGlobalCounter(name, value)  -> nil` | Establece un contador numérico con nombre. |
| `sf:GetClearStatusCount(difficulty, clearStatus)  -> integer` | Número de charts de una dificultad (0 Easy a 4 Extra Extreme) cuyo mejor estado de clear es exactamente clearStatus (0 ninguno, 1 asistido, 2 clear, 3 full combo, 4 perfecto). 0 para argumentos fuera de rango. |
| `sf:GetDanBestPlay(node)  -> danBestPlay` | La mejor partida sin mods de un nodo de canción dan (consulta Handle de mejor partida de dan); un handle con HasRecord false si no hay ninguna. |
| `sf:GetCharacter()  -> character` | El handle de personaje vinculado al jugador de esta ranura (consulta Handle de personaje). |
| `sf.CharacterName  -> string` | Nombre de carpeta del personaje equipado. |
| `sf:ChangeCharacter(folderName)  -> bool` | Equipa el personaje con este nombre de carpeta. Devuelve true cuando el personaje queda equipado o ya estaba activo, false si ningún personaje cargado tiene este nombre de carpeta. |
| `sf:GetPuchichara()  -> puchichara` | El puchichara equipado (consulta Handle de puchichara), o nil si no se puede resolver. |
| `sf:IsPuchicharaUnlocked(folderName)  -> bool` | Si el puchichara con este nombre de carpeta está desbloqueado. |
| `sf:UnlockPuchichara(folderName)  -> nil` | Desbloquea un puchichara y persiste (sin efecto si ya está desbloqueado). |
| `sf:ChangePuchichara(folderName)  -> nil` | Equipa el puchichara con este nombre de carpeta y persiste. El método no valida el nombre. |
| `sf:IsCharacterUnlocked(folderName)  -> bool` | Si el personaje está desbloqueado. El personaje equipado siempre cuenta como desbloqueado. |
| `sf:UnlockCharacter(folderName)  -> nil` | Desbloquea un personaje y persiste (sin efecto si ya está desbloqueado). |
| `sf.DanTitleCount  -> integer` | Número de títulos de dan disponibles, incluido el título por defecto (siempre al menos 1). |
| `sf:GetDanTitleByIndex(index)  -> danTitleEntry` | El título de dan en un índice desde 0 (consulta Handle de entrada de título de dan). El índice 0 es el título por defecto; nil si está fuera de rango. |
| `sf.SelectedDan  -> string` | Texto del título de dan activo. |
| `sf:ChangeDan(title)  -> nil` | Activa el título indicado, copiando sus indicadores de dorado y estado de clear si es uno que el jugador ganó, actualiza la placa de nombre y persiste. |
| `sf:ChangeName(name)  -> nil` | Cambia el nombre mostrado, actualiza la placa de nombre y persiste. El método ignora los nombres vacíos o sin cambios. |
| `sf:ChangeNameplate(id)  -> nil` | Equipa la placa de nombre con este id, actualiza la placa de nombre y persiste. Un id ausente de la base de datos borra el texto de título en caché. |

```lua
local save = GetSaveFile(0)
local entry = CHARACTERLIST:GetByName("Aoi")
if entry and not save:IsCharacterUnlocked(entry.FolderName) then
    local cond = entry.UnlockCondition
    if cond:IsUnlockable(0) and cond:GetCoinPrice() <= save.Coins then
        save:SpendCoins(cond:GetCoinPrice())
        save:UnlockCharacter(entry.FolderName)
    end
end
```

## Placas de nombre y títulos de dan

### NAMEPLATE

Dibuja placas de título, placas de dan y placas de nombre completas de jugador.

<div class="callout warn">
La ROActivity nameplate del skin (Modules/ROActivities/nameplate) se encarga del dibujo y define el arte y la disposición. La opacidad va de 0 a 255. Los parámetros de texto reciben una textura renderizada desde un objeto de texto (consulta Gráficos y texto); rarity es el índice 0 Poor, 1 Common, 2 Uncommon, 3 Rare, 4 Epic, 5 Legendary, 6 Mythical.
</div>

| Método | Descripción |
| --- | --- |
| `NAMEPLATE:DrawTitlePlate(x, y, opacity, type, text, rarity, nameplateId)  -> nil` | Dibuja una placa de título con el tipo de visualización, la textura de título prerrenderizada, el índice de rareza y el id de placa indicados. |
| `NAMEPLATE:DrawDanPlate(x, y, opacity, danGrade, text)  -> nil` | Dibuja una placa de dan para el grado indicado usando una textura de título prerrenderizada. |
| `NAMEPLATE:DrawPlayerNameplate(x, y, opacity, player)  -> nil` | Dibuja la placa de nombre completa de una ranura de jugador; el lado rojo o azul sigue el ajuste de lado 1P del juego. |
| `NAMEPLATE:DrawNameplateTitleById(id, x, y, opacity, font)  -> nil` | Renderiza el título localizado de la placa con este id usando un objeto de texto y lo dibuja como placa de título. El id debe existir en la base de datos de placas. |

### NAMEPLATESLIST

La base de datos de todas las placas de nombre que conoce el juego, con búsquedas por índice o id y filtrado.

<div class="callout warn">
Los métodos de consulta devuelven handles de información de placa de nombre. FindWhere llama a una función Lua una vez por placa y conserva las entradas para las que devuelve true.
</div>

| Método | Descripción |
| --- | --- |
| `NAMEPLATESLIST.Count  -> integer` | Número de placas de nombre en la base de datos. |
| `NAMEPLATESLIST:GetByIndex(index)  -> nameplateInfo` | La placa en una posición de base de datos desde 0, o nil si está fuera de rango. |
| `NAMEPLATESLIST:GetById(id)  -> nameplateInfo` | La placa con este id, o nil si no se encuentra. |
| `NAMEPLATESLIST:GetAll()  -> nameplateInfo[]` | Todas las placas como lista. |
| `NAMEPLATESLIST:FindWhere(predicate)  -> nameplateInfo[]` | Las placas para las que `predicate(info)` devuelve true. |

### Handle de información de placa de nombre

Un título de placa de nombre: texto localizado, tipo de visualización, id, rareza y condición de desbloqueo.

<div class="callout warn">
sf.NameplateInfo y NAMEPLATESLIST devuelven estos handles. La placa de principiante por defecto tiene id -1, rareza "Common" y ninguna condición de desbloqueo.
</div>

| Método | Descripción |
| --- | --- |
| `info.Title  -> string` | Texto del título en el idioma actual. |
| `info.Type  -> integer` | Código de tipo de visualización que se pasa a NAMEPLATE:DrawTitlePlate. |
| `info.Id  -> integer` | Id de la placa (-1 para la placa de principiante por defecto). |
| `info.Rarity  -> string` | Nombre de rareza: "Poor", "Common", "Uncommon", "Rare", "Epic", "Legendary" o "Mythical". |
| `info.UnlockCondition  -> unlockCondition` | La condición de desbloqueo (consulta Handle de condición de desbloqueo). |

### Handle de información de placa de dan

El título de dan activo del jugador tal como se muestra en la placa de nombre.

<div class="callout warn">
sf.DanplateInfo devuelve este handle. Los valores reflejan el archivo de guardado en el momento de la lectura.
</div>

| Método | Descripción |
| --- | --- |
| `info.Title  -> string` | Texto del título de dan activo. |
| `info.Gold  -> bool` | Si el jugador ganó el título activo con un aprobado dorado. |
| `info.ClearStatus  -> integer` | Código de estado de clear del título activo. |

### Handle de entrada de título de dan

Un título de dan que el jugador puede seleccionar.

<div class="callout warn">
sf:GetDanTitleByIndex devuelve estas entradas. El índice 0 es el título por defecto (no dorado, estado de clear 0); los índices posteriores son títulos que el jugador ganó.
</div>

| Método | Descripción |
| --- | --- |
| `entry.Title  -> string` | Texto del título. |
| `entry.IsGold  -> bool` | Si el jugador ganó el título con un aprobado dorado. |
| `entry.ClearStatus  -> integer` | Mejor estado de clear registrado para el título. |

### Handle de mejor partida de dan

Los mejores resultados de examen de un registro de dan.

<div class="callout warn">
sf:GetDanBestPlay devuelve este handle. Comprueba HasRecord antes de leer los exámenes. GetExam devuelve un array .NET: indexa desde 0 y lee `.Length`.
</div>

| Método | Descripción |
| --- | --- |
| `play.HasRecord  -> bool` | Si existe un registro para la canción. |
| `play:GetExam(slot)  -> int[]` | Mejores puntuaciones de la ranura de examen 1 a 7: un valor para un examen de todo el curso, uno por canción para los exámenes por canción. Vacío para un registro ausente o una ranura inválida. |

## Personajes y puchicharas

### CHARACTER

Crea handles de personaje y expone los nombres de las ranuras estándar de animación y voz.

<div class="callout warn">
CreateCharacter devuelve un handle que posee sus recursos; comprueba IsValid y llama a Dispose al terminar. GetPlayerCharacter devuelve un handle que sigue al personaje equipado por el jugador y no necesita liberarse. GetPlayerGradientMap devuelve un mapa de gradiente (consulta Gráficos y texto). Los miembros ANIM_* y VOICE_* son cadenas de solo lectura; pásalas a los métodos de animación y voz del handle de personaje.
</div>

| Método | Descripción |
| --- | --- |
| `CHARACTER:CreateCharacter(folderName)  -> character` | Carga un personaje independiente desde Global/Characters/{folderName}. IsValid es false si la carpeta no existe. |
| `CHARACTER:GetPlayerCharacter(player)  -> character` | Un handle vinculado a una ranura de jugador que resuelve el personaje equipado en cada llamada. |
| `CHARACTER:GetPlayerGradientMap(player)  -> gradientMap` | El gradiente de paleta activo para una ranura de jugador, o nil si no hay ninguno establecido. |
| `CHARACTER.ANIM_PREVIEW  -> string` | Pose de vista previa (menús y tiendas). |
| `CHARACTER.ANIM_RENDER  -> string` | Pose de render completo. |
| `CHARACTER.ANIM_GAME_NORMAL  -> string` | Juego, estado normal. |
| `CHARACTER.ANIM_GAME_CLEAR  -> string` | Juego, medidor en la zona de clear. |
| `CHARACTER.ANIM_GAME_MAX  -> string` | Juego, medidor lleno. |
| `CHARACTER.ANIM_GAME_GOGO  -> string` | Juego, go-go time. |
| `CHARACTER.ANIM_GAME_GOGO_MAX  -> string` | Juego, go-go time con el medidor lleno. |
| `CHARACTER.ANIM_GAME_MISS  -> string` | Juego, fallo. |
| `CHARACTER.ANIM_GAME_MISS_DOWN  -> string` | Juego, fallo con el medidor bajo. |
| `CHARACTER.ANIM_GAME_10COMBO  -> string` | Juego, hito de 10 combos. |
| `CHARACTER.ANIM_GAME_10COMBO_MAX  -> string` | Juego, hito de 10 combos con el medidor lleno. |
| `CHARACTER.ANIM_GAME_CLEARED  -> string` | Juego, canción superada. |
| `CHARACTER.ANIM_GAME_FAILED  -> string` | Juego, canción fallida. |
| `CHARACTER.ANIM_GAME_CLEAR_OUT  -> string` | Transición de salida del estado de clear. |
| `CHARACTER.ANIM_GAME_CLEAR_IN  -> string` | Transición de entrada al estado de clear. |
| `CHARACTER.ANIM_GAME_MAX_OUT  -> string` | Transición de salida del estado de medidor lleno. |
| `CHARACTER.ANIM_GAME_MAX_IN  -> string` | Transición de entrada al estado de medidor lleno. |
| `CHARACTER.ANIM_GAME_MISS_IN  -> string` | Transición de entrada a un fallo. |
| `CHARACTER.ANIM_GAME_MISS_DOWN_IN  -> string` | Transición de entrada a un fallo con el medidor bajo. |
| `CHARACTER.ANIM_GAME_RETURN  -> string` | Vuelta al estado normal. |
| `CHARACTER.ANIM_GAME_GOGOSTART  -> string` | Estallido de inicio de go-go. |
| `CHARACTER.ANIM_GAME_GOGOSTART_CLEAR  -> string` | Estallido de inicio de go-go en el estado de clear. |
| `CHARACTER.ANIM_GAME_GOGOSTART_MAX  -> string` | Estallido de inicio de go-go con el medidor lleno. |
| `CHARACTER.ANIM_GAME_BALLOON_BREAKING  -> string` | Globo siendo golpeado. |
| `CHARACTER.ANIM_GAME_BALLOON_BROKE  -> string` | Globo reventado. |
| `CHARACTER.ANIM_GAME_BALLOON_MISS  -> string` | Globo fallado. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_BREAKING  -> string` | Kusudama siendo golpeado. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_BROKE  -> string` | Kusudama roto. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_MISS  -> string` | Kusudama fallado. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_IDLE  -> string` | Kusudama en reposo. |
| `CHARACTER.ANIM_GAME_TOWER_STANDING  -> string` | Modo Torre, de pie. |
| `CHARACTER.ANIM_GAME_TOWER_STANDING_TIRED  -> string` | Modo Torre, de pie cansado. |
| `CHARACTER.ANIM_GAME_TOWER_CLIMBING  -> string` | Modo Torre, escalando. |
| `CHARACTER.ANIM_GAME_TOWER_CLIMBING_TIRED  -> string` | Modo Torre, escalando cansado. |
| `CHARACTER.ANIM_GAME_TOWER_RUNNING  -> string` | Modo Torre, corriendo. |
| `CHARACTER.ANIM_GAME_TOWER_RUNNING_TIRED  -> string` | Modo Torre, corriendo cansado. |
| `CHARACTER.ANIM_GAME_TOWER_CLEAR  -> string` | Modo Torre, clear. |
| `CHARACTER.ANIM_GAME_TOWER_CLEAR_TIRED  -> string` | Modo Torre, clear cansado. |
| `CHARACTER.ANIM_GAME_TOWER_FAIL  -> string` | Modo Torre, fallo. |
| `CHARACTER.ANIM_MENU_WAIT  -> string` | Menú, en espera. |
| `CHARACTER.ANIM_MENU_START  -> string` | Menú, inicio. |
| `CHARACTER.ANIM_MENU_NORMAL  -> string` | Menú, normal. |
| `CHARACTER.ANIM_MENU_SELECT  -> string` | Menú, selección. |
| `CHARACTER.ANIM_ENTRY_NORMAL  -> string` | Pantalla de entrada, normal. |
| `CHARACTER.ANIM_ENTRY_JUMP  -> string` | Pantalla de entrada, salto. |
| `CHARACTER.ANIM_RESULT_NORMAL  -> string` | Resultados, normal. |
| `CHARACTER.ANIM_RESULT_CLEAR  -> string` | Resultados, clear. |
| `CHARACTER.ANIM_RESULT_FAILED_IN  -> string` | Resultados, entrada al estado de fallo. |
| `CHARACTER.ANIM_RESULT_FAILED  -> string` | Resultados, fallo. |
| `CHARACTER.VOICE_END_FAILED  -> string` | Fin de canción, fallida. |
| `CHARACTER.VOICE_END_CLEAR  -> string` | Fin de canción, superada. |
| `CHARACTER.VOICE_END_FULLCOMBO  -> string` | Fin de canción, full combo. |
| `CHARACTER.VOICE_END_ALLPERFECT  -> string` | Fin de canción, todo perfecto. |
| `CHARACTER.VOICE_END_AIBATTLE_WIN  -> string` | Fin de canción, batalla contra la IA ganada. |
| `CHARACTER.VOICE_END_AIBATTLE_LOSE  -> string` | Fin de canción, batalla contra la IA perdida. |
| `CHARACTER.VOICE_MENU_SONGSELECT  -> string` | Entrada en la selección de canciones. |
| `CHARACTER.VOICE_MENU_SONGDECIDE  -> string` | Canción confirmada. |
| `CHARACTER.VOICE_MENU_SONGDECIDE_AI  -> string` | Canción confirmada en batalla contra la IA. |
| `CHARACTER.VOICE_MENU_DIFFSELECT  -> string` | Selección de dificultad. |
| `CHARACTER.VOICE_MENU_DANSELECTSTART  -> string` | Entrada en la selección de dan. |
| `CHARACTER.VOICE_MENU_DANSELECTPROMPT  -> string` | Aviso de selección de dan. |
| `CHARACTER.VOICE_MENU_DANSELECTCONFIRM  -> string` | Curso dan confirmado. |
| `CHARACTER.VOICE_TITLE_SANKA  -> string` | Entrada en la pantalla de título. |
| `CHARACTER.VOICE_TOWER_MISS  -> string` | Fallo en modo Torre. |
| `CHARACTER.VOICE_RESULT_BESTSCORE  -> string` | Resultados, nueva mejor puntuación. |
| `CHARACTER.VOICE_RESULT_CLEARFAILED  -> string` | Resultados, fallida. |
| `CHARACTER.VOICE_RESULT_CLEARSUCCESS  -> string` | Resultados, superada. |
| `CHARACTER.VOICE_RESULT_DANFAILED  -> string` | Resultados, dan suspendido. |
| `CHARACTER.VOICE_RESULT_DANREDPASS  -> string` | Resultados, dan aprobado. |
| `CHARACTER.VOICE_RESULT_DANGOLDPASS  -> string` | Resultados, dan aprobado con dorado. |

### Handle de personaje

Un personaje dibujable: reproduce animaciones y voces con nombre y lleva un estado de dibujo por handle (opacidad, escala, tinte, rotación, modo de mezcla y de envoltura, gradiente de paleta).

<div class="callout warn">
CHARACTER:GetPlayerCharacter, CHARACTER:CreateCharacter, sf:GetCharacter y la propiedad Character de una entrada de lista de personajes devuelven handles de personaje. Solo los handles de CreateCharacter poseen sus recursos y necesitan Dispose. El handle guarda los valores Set* y los aplica en cada dibujo siguiente; los argumentos de escala y opacidad de los métodos de dibujo se multiplican con los valores guardados. La opacidad guardada va de 0.0 a 1.0, la opacidad por dibujo de 0 a 255. Los nombres de animación y voz son las constantes de CHARACTER.
</div>

| Método | Descripción |
| --- | --- |
| `char.IsValid  -> bool` | Si el handle se resuelve a un personaje cargado. |
| `char.FolderName  -> string` | Nombre de carpeta, o una cadena vacía si es inválido. |
| `char.FullPath  -> string` | Ruta absoluta de la carpeta, o una cadena vacía si es inválido. |
| `char.DisplayName  -> string` | Nombre de visualización localizado, con el nombre de carpeta como alternativa. |
| `char:SetPaletteGradient(stops, blend?)  -> nil` | Aplica un gradiente de paleta construido a partir de una tabla de al menos dos paradas de color, con una cantidad de mezcla opcional (1.0 por defecto). Los handles vinculados a jugador también guardan el gradiente en la ranura del jugador. Pasar nil lo elimina. |
| `char:ClearPaletteGradient()  -> nil` | Elimina el gradiente de paleta (y el gradiente de la ranura del jugador para los handles vinculados a jugador). |
| `char:SetOpacity(opacity)  -> nil` | Opacidad guardada, de 0.0 transparente a 1.0 opaco. |
| `char:SetScale(scaleX, scaleY)  -> nil` | Escala guardada; una X negativa refleja horizontalmente. |
| `char:SetColor(color)  -> nil` | Tinte guardado a partir de un valor de color. |
| `char:SetColor(r, g, b)  -> nil` | Tinte guardado a partir de tres canales de 0.0 a 1.0. |
| `char:SetRotation(degrees)  -> nil` | Rotación guardada en grados. |
| `char:SetBlendMode(mode)  -> nil` | Modo de mezcla guardado: "normal", "add", "multi", "sub" o "screen". |
| `char:SetWrapMode(mode)  -> nil` | Modo de envoltura de textura guardado: "edge", "border", "repeat" o "mirror". |
| `char:GetScale()  -> vector2` | Escala guardada. |
| `char:GetColor()  -> tuple` | Tinte guardado como una tupla .NET con los campos Item1, Item2 e Item3 (rojo, verde, azul). |
| `char:GetRotation()  -> number` | Rotación guardada en grados. |
| `char:GetBlendMode()  -> string` | Modo de mezcla guardado. |
| `char:GetWrapMode()  -> string` | Modo de envoltura guardado. |
| `char:Draw(x, y, animation, scaleX?, scaleY?, opacity?)  -> nil` | Dibuja la animación en x, y. Valores por defecto: escala 1, opacidad 255. |
| `char:DrawAtAnchor(x, y, animation, anchor?, scaleX?, scaleY?, opacity?)  -> nil` | Dibuja la animación con el punto de ancla indicado ("bottom" por defecto) colocado en x, y. |
| `char:DrawRect(x, y, w, h, animation, scaleX?, scaleY?, opacity?)  -> nil` | Dibuja la animación en la esquina superior izquierda del rectángulo. El método acepta w y h para el código de disposición, pero no afectan al dibujo. |
| `char:DrawRectAtAnchor(x, y, clipW, clipH, animation, opacity?, clipX?, clipY?)  -> nil` | Dibuja la animación con su esquina superior izquierda en x, y, recortada a un rectángulo de clipW por clipH desplazado clipX, clipY. La escala, el tinte y la rotación provienen solo del estado guardado. |
| `char:Update(animation, looping?)  -> bool` | Avanza la animación (en bucle por defecto) y devuelve si sigue reproduciéndose. |
| `char:LoadAnimation(animation)  -> nil` | Carga los fotogramas de la animación. |
| `char:DisposeAnimation(animation)  -> nil` | Libera los fotogramas de la animación. |
| `char:AvailableAnimation(animation)  -> bool` | Si el personaje proporciona la animación. |
| `char:SetAnimationDuration(animation, duration)  -> nil` | Establece la duración de reproducción de la animación. |
| `char:SetAnimationCyclesFromBPM(animation, bpm)  -> nil` | Establece la longitud del ciclo de la animación a partir de un BPM. |
| `char:ResetAnimationCounter(animation)  -> nil` | Reinicia la animación desde su primer fotograma. |
| `char:GetAnimationSize(animation)  -> vector2` | Tamaño dibujado del fotograma actual de la animación a la resolución del skin, o (0, 0) si no está disponible. |
| `char:LoadVoice(voice)  -> nil` | Carga un clip de voz. |
| `char:DisposeVoice(voice)  -> nil` | Libera un clip de voz. |
| `char:PlayVoice(voice)  -> nil` | Reproduce un clip de voz. |
| `char:Dispose()  -> nil` | Libera los recursos del personaje (solo handles de CreateCharacter). |

```lua
local chara = CHARACTER:GetPlayerCharacter(0)
chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL)

function update()
    chara:Update(CHARACTER.ANIM_MENU_NORMAL)
end

function draw()
    chara:DrawAtAnchor(960, 1000, CHARACTER.ANIM_MENU_NORMAL, "bottom")
end
```

### CHARACTERLIST

La lista de todos los personajes cargados.

<div class="callout warn">
El skin reconstruye la lista cuando carga sus personajes y la libera al recargar el skin, así que la global puede ser nil mientras no hay personajes cargados. Los métodos de consulta devuelven entradas de lista de personajes.
</div>

| Método | Descripción |
| --- | --- |
| `CHARACTERLIST.Count  -> integer` | Número de personajes cargados. |
| `CHARACTERLIST:GetAll()  -> characterEntry[]` | Todos los personajes como lista. |
| `CHARACTERLIST:GetByIndex(index)  -> characterEntry` | La entrada en un índice desde 0, o nil si está fuera de rango. |
| `CHARACTERLIST:GetByName(folderName)  -> characterEntry` | La entrada con este nombre de carpeta, o nil si no se encuentra. |

### Entrada de lista de personajes

Una entrada de CHARACTERLIST: nombre de carpeta, nombre de visualización, rareza, un handle de personaje y la condición de desbloqueo.

<div class="callout warn">
La lista es propietaria del handle compartido de la propiedad Character; no lo liberes. Carga las animaciones en él antes de dibujar.
</div>

| Método | Descripción |
| --- | --- |
| `entry.FolderName  -> string` | Nombre de carpeta; los archivos de guardado lo usan como clave. |
| `entry.DisplayName  -> string` | Nombre de visualización localizado. |
| `entry.Rarity  -> string` | Nombre de rareza (consulta la lista en Handle de información de placa de nombre). |
| `entry.Character  -> character` | Handle de personaje de esta entrada. |
| `entry.UnlockCondition  -> unlockCondition` | La condición de desbloqueo (consulta Handle de condición de desbloqueo). |

### PUCHICHARALIST

La lista de todos los puchicharas cargados, más la selección actual de cada jugador.

<div class="callout warn">
El skin reconstruye la lista cuando carga sus texturas de puchichara y la libera al recargar el skin, así que la global puede ser nil mientras no están cargadas. Los métodos de consulta devuelven handles de puchichara.
</div>

| Método | Descripción |
| --- | --- |
| `PUCHICHARALIST.Count  -> integer` | Número de puchicharas cargados. |
| `PUCHICHARALIST:GetAll()  -> puchichara[]` | Todos los puchicharas como lista. |
| `PUCHICHARALIST:GetByIndex(index)  -> puchichara` | El puchichara en un índice desde 0, o nil si está fuera de rango. |
| `PUCHICHARALIST:GetByName(folderName)  -> puchichara` | El puchichara con este nombre de carpeta, o nil si no se encuentra. |
| `PUCHICHARALIST:GetPlayerPuchichara(player)  -> puchichara` | El puchichara equipado por una ranura de jugador, o nil si no se puede resolver. |

### Handle de puchichara

Un puchichara: sus texturas, nombre y autor localizados, rareza, nombre de carpeta y condición de desbloqueo.

<div class="callout warn">
PUCHICHARALIST y sf:GetPuchichara devuelven estos handles. La lista es propietaria de las texturas; no las liberes. Una imagen ausente da una textura vacía.
</div>

| Método | Descripción |
| --- | --- |
| `puchi.tx  -> texture` | Hoja de sprites cargada desde Chara.png. |
| `puchi.render  -> texture` | Render completo cargado desde Render.png. |
| `puchi.Name  -> string` | Nombre de visualización localizado. |
| `puchi.Author  -> string` | Nombre de autor localizado. |
| `puchi.Rarity  -> string` | Nombre de rareza (consulta la lista en Handle de información de placa de nombre). |
| `puchi.FolderName  -> string` | Nombre de carpeta; los archivos de guardado lo usan como clave. |
| `puchi.UnlockCondition  -> unlockCondition` | La condición de desbloqueo (consulta Handle de condición de desbloqueo). |
| `puchi:GetUnlockMessage()  -> string` | Atajo para `puchi.UnlockCondition:GetConditionMessage()`. |

## Estado de la partida y desbloqueos

### PLAYSTATE

Resultados en vivo de la partida actual o más reciente: recuentos de juicios, puntuación, combo, comprobaciones de clear y estado de torre y dan.

<div class="callout warn">
Los valores provienen de la pantalla de juego, así que tienen sentido durante una partida y en las pantallas que la siguen. Los índices de jugador empiezan en 0; los métodos no comprueban su rango. Las comprobaciones de dan siempre evalúan al jugador 0.
</div>

| Método | Descripción |
| --- | --- |
| `PLAYSTATE.LastRegisteredFloor  -> integer` | Modo Torre: el último piso alcanzado. |
| `PLAYSTATE.MaxNumberOfLives  -> integer` | Modo Torre: el número máximo de vidas. |
| `PLAYSTATE.CurrentNumberOfLives  -> integer` | Modo Torre: el número actual de vidas. |
| `PLAYSTATE.InvincibilityDurationSpeedDependent  -> number` | Modo Torre: la duración de invencibilidad ajustada a la velocidad de la canción. |
| `PLAYSTATE.InvincibilityDuration  -> integer` | Modo Torre: la duración base de invencibilidad. |
| `PLAYSTATE:WasPlayEndedNormally()  -> bool` | Si la partida anterior llegó hasta el final. |
| `PLAYSTATE:WasPlayAborted()  -> bool` | Si el jugador abandonó la partida anterior antes de tiempo. |
| `PLAYSTATE:GetGoodCount(player)  -> integer` | Número de juicios Good. |
| `PLAYSTATE:GetOkCount(player)  -> integer` | Número de juicios Ok. |
| `PLAYSTATE:GetBadCount(player)  -> integer` | Número de juicios Bad. |
| `PLAYSTATE:GetRollCount(player)  -> integer` | Número de golpes de redoble. |
| `PLAYSTATE:GetADLibCount(player)  -> integer` | Número de notas ADLib acertadas. |
| `PLAYSTATE:GetMissedADLibCount(player)  -> integer` | Número de notas ADLib falladas. |
| `PLAYSTATE:GetBoomCount(player)  -> integer` | Número de notas de mina golpeadas. |
| `PLAYSTATE:GetAvoidedBoomCount(player)  -> integer` | Número de notas de mina evitadas. |
| `PLAYSTATE:GetScore(player)  -> integer` | Puntuación actual. |
| `PLAYSTATE:GetCombo(player)  -> integer` | Combo actual. |
| `PLAYSTATE:GetHighestCombo(player)  -> integer` | Combo más alto alcanzado. |
| `PLAYSTATE:IsClear(player)  -> bool` | Si el medidor alcanza la línea de clear. |
| `PLAYSTATE:IsAssistedClear(player)  -> bool` | Si la partida es un clear mientras hay activo un mod que reduce la puntuación. |
| `PLAYSTATE:IsFullCombo(player)  -> bool` | Clear, no asistido, sin juicios Bad y sin minas golpeadas. |
| `PLAYSTATE:IsPerfect(player)  -> bool` | Full combo sin juicios Ok. |
| `PLAYSTATE:IsAlive()  -> bool` | Modo Torre: si quedan vidas. |
| `PLAYSTATE:IsPass()  -> bool` | Modo Dan: si el estado del examen no es un suspenso. |
| `PLAYSTATE:IsRedPass()  -> bool` | Modo Dan: si el estado del examen es un aprobado estándar. |
| `PLAYSTATE:IsGoldPass()  -> bool` | Modo Dan: si el estado del examen es un aprobado dorado. |
| `PLAYSTATE:IsDanClear()  -> bool` | Modo Dan: aprobado y no asistido. |
| `PLAYSTATE:IsDanFullCombo()  -> bool` | Modo Dan: clear de dan sin juicios Bad y sin minas golpeadas. |
| `PLAYSTATE:IsDanPerfect()  -> bool` | Modo Dan: full combo de dan sin juicios Ok. |

### Handle de condición de desbloqueo

El requisito de desbloqueo de una placa de nombre, un personaje o un puchichara.

<div class="callout warn">
La propiedad UnlockCondition de los handles de información de placa de nombre, las entradas de lista de personajes y los handles de puchichara devuelve este handle. Un objeto sin condición (HasCondition false) está disponible por defecto: IsUnlockable devuelve true y los mensajes están vacíos. El vocabulario de condiciones coincide con el de Unlock.json y los desbloqueables de charts; consulta la guía <a href="../guides/unlockables.md">Desbloqueables de charts</a>.
</div>

| Método | Descripción |
| --- | --- |
| `cond.HasCondition  -> bool` | Si el objeto tiene una condición de desbloqueo. |
| `cond:GetConditionType()  -> string` | El id del tipo de condición (por ejemplo "ch", "cs", "gt", "gc" o "ig"), o una cadena vacía. |
| `cond:GetCoinPrice()  -> integer` | Precio en monedas de la condición, o 0. |
| `cond:GetConditionMessage()  -> string` | Descripción localizada de la condición. |
| `cond:IsUnlockable(player)  -> bool` | Si el jugador cumple actualmente la condición. |
| `cond:GetBlockedMessage(player)  -> string` | Por qué el jugador no cumple la condición, o una cadena vacía cuando la cumple. |

## Tema e idioma

### THEME

La resolución del skin, los ajustes de tema, las cadenas localizadas de ámbito de skin y las definiciones de ajustes de tema.

<div class="callout warn">
El skin declara los ajustes de tema en ThemeSettings.json y guarda sus valores en el ThemeSettings.db3 contiguo. Los getters devuelven siempre los valores de los ajustes como cadenas; un ajuste ausente devuelve su valor por defecto declarado, o una cadena vacía si no existe ninguna declaración. GetThemeSettingForPlayer recibe un número de jugador desde 1. Los índices de definición empiezan en 0.
</div>

| Método | Descripción |
| --- | --- |
| `THEME:GetResolution()  -> vector2` | La resolución del skin. |
| `THEME:GetThemeSetting(settingId)  -> string` | Valor de un ajuste de ámbito global. |
| `THEME:GetThemeSettingForPlayer(settingId, player)  -> string` | Valor de un ajuste de ámbito de guardado para el jugador (desde 1), o su valor por defecto si el guardado no tiene valor. |
| `THEME:GetSkinString(key)  -> string` | Cadena localizada de la carpeta Locales del skin: primero el idioma actual, luego la configuración regional por defecto del skin, luego `[LOCALE NOT FOUND: key]`. |
| `THEME:GetDefinitionCount()  -> integer` | Número de definiciones de ajustes en ThemeSettings.json. |
| `THEME:GetDefinitionId(index)  -> string` | Id de la definición en un índice desde 0, o una cadena vacía. |
| `THEME:GetDefinitionScope(index)  -> string` | Ámbito de la definición: "global" o "save". |
| `THEME:GetDefinitionType(index)  -> string` | Tipo de la definición: "bool", "int", "double", "string" o "enum". |

### LANG

Cadenas localizadas del juego, cambio de idioma y valores de texto multilingües.

<div class="callout warn">
GetString formatea la entrada con los argumentos adicionales. GetLanguageIds y GetLanguageNames devuelven arrays .NET (desde 0, `.Length`); GetAvailableLanguages devuelve un diccionario que se enumera con `:GetEnumerator()` (consulta Datos y persistencia). FromDict recibe un objeto JSON analizado por JSONLOADER (no acepta una tabla Lua); AsLocalizationData recibe un JsonNode de JSONLOADER:LoadJson.
</div>

| Método | Descripción |
| --- | --- |
| `LANG:GetString(key, ...)  -> string` | La cadena localizada de una clave, con los marcadores de formato rellenados con los argumentos adicionales. |
| `LANG:ChangeLanguage(id)  -> bool` | Cambia el idioma activo si el id existe y difiere del actual, y luego llama a `reloadLanguage` en todos los scripts cargados; devuelve si cambió. Deja CONFIG.Language sin cambios. |
| `LANG:GetLanguageIds()  -> string[]` | Ids de los idiomas disponibles. |
| `LANG:GetLanguageNames()  -> string[]` | Nombres de visualización de los idiomas disponibles, en el mismo orden. |
| `LANG:GetAvailableLanguages()  -> dict` | Id de idioma a nombre de visualización. |
| `LANG:GetExamName(type)  -> string` | Nombre localizado de un tipo de examen dan. |
| `LANG:AsLocalizationData(node)  -> localizationData` | Construye un valor de localización a partir de un JsonNode con la forma `{ "strings": { "<lang>": "text" } }`. |
| `LANG:FromDict(dict)  -> localizationData` | Construye un valor de localización a partir de un objeto JSON analizado que asigna ids de idioma a texto. |
| `LANG:FromString(json)  -> localizationData` | Construye un valor de localización a partir de una cadena de objeto JSON que asigna ids de idioma a texto; un valor vacío si la cadena no se puede analizar. |

```lua
local langs = LANG:GetAvailableLanguages()
local e = langs:GetEnumerator()
while e:MoveNext() do
    print(e.Current.Key, e.Current.Value)
end

local name = LANG:FromString('{"ja":"太鼓","default":"Taiko"}'):GetString("")
```

### Handle de datos de localización

Un conjunto de cadenas indexadas por id de idioma que se resuelve al idioma actual.

<div class="callout warn">
LANG:AsLocalizationData, LANG:FromDict y LANG:FromString devuelven este handle. Orden de resolución: el id del idioma actual, luego la clave "default", luego la alternativa que se pasa a GetString.
</div>

| Método | Descripción |
| --- | --- |
| `loc:GetString(fallback)  -> string` | El texto para el idioma actual, o "default", o la alternativa. |
| `loc:SetString(langId, text)  -> nil` | Establece el texto para un id de idioma. |
| `loc:GetAllStrings()  -> string[]` | Todos los textos guardados, sin orden particular. |
