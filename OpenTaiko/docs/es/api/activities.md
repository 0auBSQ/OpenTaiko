<!-- api/activities.md -->

# Módulos y ciclo de vida

El motor llama a un conjunto fijo de callbacks en el Script.lua de un módulo. Esta página los lista, junto con las globales que dirigen activities, fondos y transiciones, y las utilidades de contador, cámara y diagnóstico que recibe cada módulo. Si nunca has escrito un módulo, lee primero [Cómo funcionan los módulos](../getting-started.md).

## Callbacks del ciclo de vida

El motor busca funciones globales de nivel superior por nombre en el Script.lua de cada módulo y las llama en puntos fijos. El motor omite un callback que no defines. El tipo de módulo decide qué callbacks llama el motor.

<div class="callout warn">
Cuando se carga un skin, el motor crea y arranca los módulos en este orden: Transitions, Stages, Activities, ROActivities. Dentro de cada tipo, el motor ejecuta primero el Script.lua de cada módulo (su código de nivel superior) y después llama a onStart en cada uno. Por tanto, las Activities y ROActivities todavía no están cargadas mientras se ejecuta el onStart de un stage, y una búsqueda ahí devuelve nil: búscalas en activate. Al cambiar de skin o salir, onDestroy se ejecuta en los Stages, luego en las ROActivities y Activities, y por último en las Transitions.
</div>

### Stages, Activities y ROActivities

| Método | Descripción |
| --- | --- |
| `onStart()` | Se llama una vez después de que el motor crea el módulo, lo que ocurre en el arranque y de nuevo cada vez que el motor carga o recarga el skin. Se ejecuta como corrutina (consulta LOADING más abajo); carga aquí los recursos. |
| `activate(...)` | Stage: se llama cada vez que el motor entra en el stage, como corrutina. Activity/ROActivity: el anfitrión lo llama a través de `handle:Activate(...)` con sus propios argumentos; los valores de retorno vuelven al anfitrión. El motor actualiza las globales CHARACTERLIST y PUCHICHARALIST justo antes de que se ejecute. |
| `update(timestamp)` | Se llama cada fotograma antes de draw; timestamp es el reloj del juego en milisegundos. Un stage deja de recibir update una vez que ha llamado a Exit; draw sigue ejecutándose durante el fundido de salida. Activity/ROActivity: el anfitrión lo llama a través de `handle:Update()`. |
| `draw(...)` | Se llama cada fotograma. Activity/ROActivity: el anfitrión lo llama a través de `handle:Draw(...)` con sus propios argumentos; los valores de retorno vuelven al anfitrión. |
| `deactivate(...)` | Stage: se llama cuando el motor abandona el stage. Activity/ROActivity: el anfitrión lo llama a través de `handle:Deactivate(...)`, o el propio módulo lo llama sobre sí mismo mediante `DEACTIVATE()`; los valores de retorno vuelven al anfitrión. |
| `afterSongEnum()` | Se llama cada vez que termina la enumeración de canciones, incluido el arranque y tras una recarga suave o completa de canciones, incluso cuando el módulo no está activo. |
| `onDestroy()` | Se llama antes de que el motor descargue el skin, para que el módulo pueda liberar lo que posee. |
| `reloadLanguage(lang)` | Se llama en cada módulo cargado cuando cambia el idioma del juego; lang es el nuevo código de idioma. |

### Fondos

Cada pantalla aloja sus propios fondos. La sección Fondos más abajo describe el argumento de estado y los ganchos de evento.

| Método | Descripción |
| --- | --- |
| `onStart()` | Se llama una vez, de forma síncrona, la primera vez que el anfitrión activa el fondo. |
| `activate(state)` | Se llama cada vez que el anfitrión activa el fondo; una reactivación no vuelve a ejecutar onStart. |
| `update(timestamp, state)` | Se llama cada fotograma (no mientras el juego está en pausa); timestamp es `state.timeStamp` en milisegundos. |
| `draw(state)` | Se llama cada fotograma. |
| `reloadLanguage(lang)` | Se llama cuando cambia el idioma del juego. |

El motor no llama a afterSongEnum ni a onDestroy en los fondos; cuando el anfitrión libera un fondo, el motor libera los recursos que este creó.

### Transiciones

| Método | Descripción |
| --- | --- |
| `onStart()` | Se llama una vez cuando se carga el skin, antes que los stages y las activities, como corrutina. |
| `fadeOut(t)` | Dibuja el fundido de salida sobre el stage saliente; t va de 0 a 1. |
| `loading(progress, elapsed)` | Dibuja la pantalla de carga; progress va de 0 a 1, elapsed es el tiempo en segundos desde que empezó la carga. |
| `fadeIn(t)` | Dibuja el fundido de entrada sobre el nuevo stage; t va de 0 a 1. |
| `onDestroy()` | Se llama antes de que el motor descargue el skin, después de los stages y las activities. |
| `reloadLanguage(lang)` | Se llama cuando cambia el idioma del juego. |

La sección Transiciones más abajo describe la temporización de las fases.

### Personajes

El Script.lua de un personaje define un conjunto distinto: loadAnimation, disposeAnimation, availableAnimation, setAnimationDuration, resetAnimationCounter, update, draw, getDrawSize, getHeyaRenderOffset, getAIBattlePosition, loadVoice, disposeVoice y playVoice. [Añadir personajes](../guides/characters.md) lo explica.

### Exit

Una función que el motor registra solo en los scripts de Stage; llamarla pide al motor abandonar el stage.

<div class="callout warn">
Disponible solo en los scripts de Modules/Stages; a las Activities y ROActivities las dirige su anfitrión y no la reciben. Acepta de 0 a 3 argumentos y tolera nil en cualquier posición. La propia llamada solicita la salida; los stages incluidos escriben `return Exit(...)` dentro de update para que nada más se ejecute en ese fotograma. target es "title", "play", "stage" o "legacy"; nil o cualquier otro valor significa "title". Cuando target es "stage", name es el módulo de Modules/Stages al que saltar; cuando target es "legacy", name es uno de "heya", "config", "exit" u "onlinelounge" (cualquier otro valor va al título). transition nombra un módulo de Modules/Transitions; si lo omites o el motor no lo encuentra, el motor usa el módulo llamado "default", y si el skin no tiene ninguna transición se reproduce un fundido a negro simple.
</div>

| Método | Descripción |
| --- | --- |
| `Exit(target?, name?, transition?)  -> number` | Solicita la salida del stage hacia el destino indicado, nombrando opcionalmente un módulo de destino y un módulo de transición; devuelve 0. |

```lua
function update(timestamp)
    if INPUT:KeyboardPressed("S") then
        return Exit("stage", "demo2")          -- salta a Modules/Stages/demo2
    end
    if INPUT:Pressed("Cancel") then
        return Exit("title", nil, "nokon_curtain")   -- vuelve al título con una transición concreta
    end
end
```

### LOADING

Utilidad de barra de carga para los callbacks que se ejecutan como corrutinas: el onStart de todos los tipos de módulo y el activate de un Stage.

<div class="callout warn">
Definida como la global LOADING en cada módulo. Estos callbacks se ejecutan en una corrutina propiedad del motor que el motor reanuda cada fotograma: el motor cede automáticamente cuando una reanudación ha agotado su presupuesto de tiempo, y también puedes ceder tú mismo con coroutine.yield(progress) o LOADING:Tick(sub). Los bloques que registras con LOADING:Add se ejecutan en orden después de que el cuerpo del callback retorne, y la barra avanza tras cada uno; el peso de un bloque es su proporción de la barra (1 por defecto). LOADING:Tick(sub) cede un fotograma desde dentro de un bloque e informa de una fracción de 0 a 1 dentro de ese bloque. Fuera de un callback en corrutina (el activate de una Activity o ROActivity, o cualquier update o draw), LOADING:Tick lanza un error de Lua porque no hay nada a lo que ceder, y los bloques encolados con LOADING:Add nunca se ejecutan.
</div>

| Método | Descripción |
| --- | --- |
| `LOADING:Add(fn)  -> nil` | Registra un bloque de carga que se ejecuta cuando el callback retorna. |
| `LOADING:Add(label, fn)  -> nil` | Registra un bloque de carga con etiqueta. |
| `LOADING:Add(label, weight, fn)  -> nil` | Registra un bloque de carga con etiqueta y un peso explícito. |
| `LOADING:Tick(sub)  -> nil` | Cede un fotograma dentro de un bloque, informando de una fracción de subprogreso de 0 a 1 dentro del bloque actual. |

```lua
function onStart()
    LOADING:Add("textures", 3, function()
        for i, name in ipairs(names) do
            tx[name] = TEXTURE:CreateTexture(name)
            LOADING:Tick(i / #names)
        end
    end)
    LOADING:Add("sounds", 1, function()
        bgm = SOUND:CreateBGM("Sounds/BGM.ogg")
    end)
end
```

## Activities

### ACTIVITY

Global para buscar por nombre una Activity cargada.

<div class="callout warn">
Registrada como la global ACTIVITY en todos los módulos excepto las ROActivities y los fondos, donde es nil; esos módulos usan ROACTIVITY. El motor carga las Activities desde Modules/Activities/{name}. ACTIVITY también expone GetROActivity, que se comporta como ROACTIVITY:GetROActivity.
</div>

| Método | Descripción |
| --- | --- |
| `ACTIVITY:GetActivity(name)  -> activity handle` | Devuelve el handle de la Activity cargada con ese nombre de carpeta, o nil si no hay ninguna cargada. |
| `ACTIVITY:GetROActivity(name)  -> activity handle` | Igual que ROACTIVITY:GetROActivity. |

### ROACTIVITY

Global para buscar por nombre una Activity de solo lectura (ROActivity) cargada.

<div class="callout warn">
Registrada como la global ROACTIVITY en todos los módulos. El motor carga las ROActivities desde Modules/ROActivities/{name} y les da las globales CONFIG, DATABASE y GetSaveFile de solo lectura, así que sus scripts no pueden cambiar el estado del juego (consulta la sección de módulos de solo lectura de Cómo funcionan los módulos). Las Activities y ROActivities son singletons por nombre: una instancia por carpeta, que comparten todos los anfitriones.
</div>

| Método | Descripción |
| --- | --- |
| `ROACTIVITY:GetROActivity(name)  -> activity handle` | Devuelve el handle de la ROActivity cargada con ese nombre de carpeta, o nil si no hay ninguna cargada. |

### Handle de activity

El objeto que devuelven ACTIVITY:GetActivity y ROACTIVITY:GetROActivity. Un anfitrión lo usa para dirigir los callbacks del módulo.

<div class="callout warn">
Activate, Deactivate y Draw reenvían sus argumentos a los callbacks activate, deactivate y draw del módulo. Update llama a update con el tiempo actual del juego en milisegundos. Cada uno devuelve los valores que devolvió el callback como un array indexado desde 0, o nil cuando el callback no devolvió nada o no está definido; lee el primer valor con `result[0]`. Call invoca cualquier función global que defina el script del módulo.
</div>

| Método | Descripción |
| --- | --- |
| `handle.IsActive  -> boolean` | Verdadero después de que Activate se ejecute y hasta que se ejecute Deactivate (o el DEACTIVATE() del propio módulo). |
| `handle:Activate(...)  -> array` | Llama al callback activate del módulo con los argumentos indicados. |
| `handle:Deactivate(...)  -> array` | Llama al callback deactivate del módulo con los argumentos indicados. |
| `handle:Update()  -> array` | Llama al callback update del módulo con el tiempo actual del juego en milisegundos. |
| `handle:Draw(...)  -> array` | Llama al callback draw del módulo con los argumentos indicados. |
| `handle:Call(functionName, ...)  -> array` | Llama a la función global con ese nombre del script del módulo con los argumentos indicados. |

```lua
local act = nil

function activate()
    if act == nil then act = ACTIVITY:GetActivity("song_select_core") end
    act:Activate()
end

function update(timestamp)
    local result = act:Update()
    local signal = result ~= nil and result[0] or nil
    if signal == "play" then return Exit("play", nil) end
    if signal == "cancel" then return Exit("title", nil) end
end

function draw()
    act:Draw()
end

function deactivate()
    act:Deactivate()
end
```

### DEACTIVATE

Función que el motor registra dentro de los scripts de Activity y ROActivity; permite al módulo desactivarse a sí mismo.

<div class="callout warn">
Llamarla marca el módulo como inactivo (handle.IsActive pasa a ser false) y ejecuta el callback deactivate del propio módulo. Los diálogos incluidos la llaman cuando el jugador confirma o cancela, y el anfitrión observa IsActive para saber que el diálogo se cerró.
</div>

| Método | Descripción |
| --- | --- |
| `DEACTIVATE(...)` | Desactiva el módulo actual y llama a su callback deactivate con los argumentos indicados. |

### ROActivities alojadas por el motor

El motor busca algunas ROActivities por nombre de carpeta fijo y las dirige él mismo. Un skin reemplaza una de ellas incluyendo una carpeta de Modules/ROActivities con ese nombre; los puntos de llamada del motor listados abajo fijan los callbacks que debe definir. Cuando falta una, el motor no dibuja la función correspondiente.

| Nombre | Qué llama el motor |
| --- | --- |
| `nameplate` | `activate(player, name, title, dan, data)` cuando cambia la placa de un jugador; `update()` una vez por fotograma; `draw(mode, ...)` con mode 0 = placa completa `(x, y, opacity, player, side)`, 1 = placa de dan `(x, y, opacity, danGrade, textTexture)`, 2 = placa de título `(x, y, opacity, type, textTexture, rarity, nameplateId)`. |
| `modal` | `activate(player, rarity, modalType, ...)` por cada modal de desbloqueo en cola, luego `update()` y `draw()` cada fotograma. El script llama a DEACTIVATE() para cerrar el modal; el motor activa entonces el siguiente. |
| `modicons` | `activate()` una vez, luego `draw(x, y, player, layout, alpha)` con layout "menu" o "game". La global MODICONS envuelve esto. |
| `danplate` | `draw(x, y, opacity, danTick, r, g, b, titleText)` en la pantalla de resultados y en los cursos dan. |
| `popup_menu` | `activate(title, items, fontSize, ...)` donde items son las etiquetas unidas por saltos de línea, seguidas de las posiciones PopupMenu del skin; `draw(selected)` cada fotograma; `deactivate()` al cerrar. |
| `config_ui` | `activate(model)` con el modelo de ajustes; `update()` cada fotograma, devolviendo "exit" para abandonar la pantalla de ajustes; `draw()`; `reload(model)` mediante Call cuando el motor reconstruye el modelo; `deactivate()`. |
| `song_enum` | `activate()`, luego `draw(isCommandSongDataGet, done, total)` cada fotograma mientras se ejecuta el escaneo de canciones; `deactivate()`. |

## Fondos

### Módulo de fondo

Un Script.lua que dibuja un fondo de pantalla, una capa de juego, un mob, una animación de clear o un efecto de kusudama, alojado por las propias pantallas del motor.

<div class="callout warn">
Los fondos viven fuera de la carpeta Modules, dentro de la carpeta Graphics del skin, en el directorio de la pantalla que decoran, por ejemplo Graphics/0_Startup/Script.lua, Graphics/10_Heya/Script.lua, Graphics/6_Result/Script.lua, Graphics/5_Game/5_Background/Normal/Up/{variant}/Script.lua, Graphics/5_Game/5_Background/Normal/Down/{variant}/Script.lua, Graphics/5_Game/3_Mob/{variant}/Script.lua, Graphics/5_Game/9_End/{result}/Script.lua y Graphics/5_Game/11_Balloon/Kusudama/Script.lua. Cuando una carpeta contiene varias variantes, el motor elige una al azar (o según el preajuste de escena del chart) en cada partida. La pantalla anfitriona crea una instancia de fondo (los fondos de juego cada vez que el motor entra en la pantalla de juego) y la libera junto con la pantalla, así que durante el juego hay varias activas a la vez. Un script de fondo recibe las mismas globales que una ROActivity (CONFIG, DATABASE y GetSaveFile de solo lectura; sin ACTIVITY). Los ganchos de evento de abajo son opcionales, y el motor llama a cada uno una vez cuando ocurre su evento.
</div>

| Método | Descripción |
| --- | --- |
| `clearIn(player)` | Fondos de juego Up y Down: el medidor del jugador alcanzó la zona de clear. |
| `clearOut(player)` | Fondos de juego Up y Down: el medidor del jugador salió de la zona de clear. |
| `playEndAnime(player)` | Animaciones de clear (Graphics/5_Game/9_End): empieza la animación final para el jugador. |
| `kusuIn()` / `kusuBroke()` / `kusuMiss()` | Kusudama: el globo aparece, el jugador lo rompe o el jugador lo falla. |
| `skipAnime()` | Fondo de resultados: el jugador omitió la animación de resultados. |

### Estado de fondo

El objeto que el anfitrión pasa a activate, update y draw de un fondo.

<div class="callout warn">
Una instancia por anfitrión, que el anfitrión actualiza en el sitio cada fotograma. Los campos de array comparten los arrays por jugador del motor y se indexan desde 0 (`state.gauge[0]` es el jugador 1). Solo los anfitriones de juego actualizan los campos de juego; los demás anfitriones los dejan en sus valores por defecto, y timeStamp se queda en -1 fuera del juego. El estado no contiene la temporización de fotogramas: lee la global fps.
</div>

| Método | Descripción |
| --- | --- |
| `state.playerCount  -> number` | Número de jugadores. |
| `state.p1IsBlue  -> boolean` | Verdadero cuando el jugador 1 usa el lado azul. |
| `state.lang  -> string` | Código del idioma actual. |
| `state.simplemode  -> boolean` | Verdadero cuando el Modo Simple está activado. |
| `state.puchicharaRarities  -> string[]` | Rareza del puchichara de cada jugador. |
| `state.characterRarities  -> string[]` | Rareza del personaje de cada jugador. |
| `state.isClear  -> boolean[]` | Si cada jugador está actualmente en la zona de clear. |
| `state.gauge  -> number[]` | Valor del medidor de cada jugador. |
| `state.bpm  -> number[]` | BPM actual de cada jugador. |
| `state.gogo  -> boolean[]` | Si cada jugador está en go-go time. |
| `state.towerNightNum  -> number` | Factor de día a noche de la Torre, de 0 a 1. |
| `state.battleState  -> number` | Código de estado de la batalla contra la IA. |
| `state.battleWin  -> boolean` | Verdadero cuando el jugador va ganando la batalla contra la IA. |
| `state.timeStamp  -> number` | Tiempo sincronizado con el chart en segundos; -1 fuera del juego. |
| `state.paused  -> boolean` | Verdadero mientras el juego está en pausa. |
| `state.player  -> number` | El jugador para el que dibuja un anfitrión por jugador (animaciones de clear). |

## Transiciones

### Módulo de transición

Un Modules/Transitions/{name}/Script.lua que dibuja las fases de fundido de salida, carga y fundido de entrada entre dos stages.

<div class="callout warn">
El tercer argumento de Exit selecciona la transición; el motor usa "default" cuando la llamada no indica ninguna o el motor no encuentra el nombre. La carga hacia el juego tras Exit("play") usa siempre la transición llamada "song_loading", o "default" cuando el skin no la tiene. El motor dirige las fases en orden: llama a fadeOut(t) cada fotograma sobre el stage saliente hasta que t llega a 1, luego desmonta el stage saliente y carga el nuevo mientras llama a loading(progress, elapsed) cada fotograma, y después llama a fadeIn(t) sobre el nuevo stage hasta que t llega a 1. Cada fundido dura 0.5 segundos a menos que el script establezca FADE_OUT_SECONDS o FADE_IN_SECONDS; el motor ignora un valor que no sea un número positivo. En un cambio de stage, el motor solo llama a loading cuando la carga ha tardado más de 0.5 segundos; antes de eso llama a fadeOut(1) para que las cargas cortas no muestren una pantalla de carga fugaz. La ruta de carga de canción muestra la fase de carga de inmediato.
</div>

| Método | Descripción |
| --- | --- |
| `FADE_OUT_SECONDS  -> number` | Global opcional de nivel superior: duración del fundido de salida en segundos. |
| `FADE_IN_SECONDS  -> number` | Global opcional de nivel superior: duración del fundido de entrada en segundos. |

```lua
FADE_OUT_SECONDS = 0.3
FADE_IN_SECONDS = 0.3

local pixel = nil

local function cover(alpha)
    pixel:SetColor(0, 0, 0)
    pixel:SetOpacity(alpha)
    pixel:SetScale(8000, 8000)
    pixel:Draw(0, 0)
end

function onStart()
    pixel = TEXTURE:CreateTexture("pixel.png")
end

function fadeOut(t) cover(t) end
function fadeIn(t) cover(1.0 - t) end
function loading(progress, elapsed) cover(1.0) end

function onDestroy()
    if pixel ~= nil then pixel:Dispose() end
end
```

## Temporización y cámara

### COUNTER

Fábrica de contadores de animación que mueven un valor desde un valor inicial hasta uno final a lo largo del tiempo.

<div class="callout warn">
Registrada como la global COUNTER. Un contador solo avanza cuando llamas a Tick, en el delta del fotograma dividido por interval, donde interval es el número de segundos por unidad de valor. El signo de interval debe coincidir con la dirección: positivo cuando end es mayor que begin, negativo cuando es menor. Si los signos no coinciden, el contador intercambia los extremos y termina en su primer tick. CreateCounterDuration recibe la duración total y elige el signo por sí mismo. Un interval de cero o unos valores begin y end iguales terminan el contador en su primer tick. El contador llama a la función opcional ended cuando el valor alcanza el final, y una vez por ciclo completado cuando se repite o rebota.
</div>

| Método | Descripción |
| --- | --- |
| `COUNTER:CreateCounter(begin, end, interval, ended?)  -> counter` | Crea un contador que va de begin a end a interval segundos por unidad, llamando a ended al terminar. |
| `COUNTER:CreateCounterDuration(begin, end, seconds, ended?)  -> counter` | Crea un contador que va de begin a end en el número de segundos indicado; devuelve un contador vacío si seconds no es positivo o begin es igual a end. |
| `COUNTER:EmptyCounter()  -> counter` | Crea un contador inerte cuyo valor se queda en 0, útil como marcador de posición. |

### Handle de contador

Un contador creado por COUNTER.

<div class="callout warn">
Lee Value cada fotograma y llama a Tick cada fotograma para avanzarlo; un contador que no has iniciado, o que se ha detenido, ignora Tick. Begin, End e Interval son campos de lectura y escritura. SetLoop y SetBounce se excluyen mutuamente. SetEasing solo da forma al Value que se reporta; por debajo, el contador sigue avanzando linealmente. Los listeners reciben el valor actual en cada tick, incluido el último.
</div>

| Método | Descripción |
| --- | --- |
| `counter.Value  -> number` | El valor actual, con el easing aplicado si se estableció; asignarlo hace saltar el contador. |
| `counter.Begin  -> number` | El valor inicial (lectura y escritura). |
| `counter.End  -> number` | El valor final (lectura y escritura). |
| `counter.Interval  -> number` | Segundos por unidad de valor (lectura y escritura). |
| `counter:Start()  -> nil` | Reinicia el valor a Begin y empieza a avanzar. |
| `counter:Resume()  -> nil` | Empieza a avanzar sin reiniciar el valor. |
| `counter:Stop()  -> nil` | Deja de avanzar. |
| `counter:Pause()  -> nil` | Igual que Stop. |
| `counter:Reset()  -> nil` | Devuelve el valor a Begin sin cambiar si avanza o no. |
| `counter:Tick()  -> nil` | Avanza el valor un fotograma, llamando a los listeners y a la función ended según corresponda. |
| `counter:SetLoop(loop)  -> nil` | Vuelve a Begin cuando el valor alcanza el final; desactiva el rebote. |
| `counter:SetBounce(bounce)  -> nil` | Invierte la dirección cuando el valor alcanza cualquiera de los extremos; desactiva la repetición. |
| `counter:GetLoop()  -> boolean` | Si la repetición está activada. |
| `counter:GetBounce()  -> boolean` | Si el rebote está activado. |
| `counter:SetEasing(type, function)  -> nil` | Aplica una curva de easing al valor reportado; type es IN, OUT, INOUT u OUTIN y function es LINEAR, SINE, QUAD, CUBIC, QUART, QUINT, EXPO, CIRC, ELASTIC, BACK o BOUNCE (sin distinguir mayúsculas; el contador ignora un nombre desconocido). |
| `counter:ClearEasing()  -> nil` | Elimina el easing. |
| `counter:Listen(listener)  -> nil` | Registra una función que se llama con el valor actual en cada tick. |
| `counter:ClearListeners()  -> nil` | Elimina todos los listeners. |

```lua
local fade = nil

function activate()
    fade = COUNTER:CreateCounterDuration(0, 1, 0.5, function() debugLog("fade done") end)
    fade:SetEasing("OUT", "QUAD")
    fade:Start()
end

function update(timestamp)
    fade:Tick()
end

function draw()
    background:SetOpacity(fade.Value)
    background:Draw(0, 0)
end
```

### GLOBALCAMERA

La cámara 2D de pantalla completa: desplaza, amplía y rota todo el fotograma renderizado y añade una sacudida de pantalla que se atenúa.

<div class="callout warn">
Registrada como la global GLOBALCAMERA. Dirige la misma transformación de pantalla que los comandos #CAMERA de TJA y afecta a todo lo que se dibuja, incluidas las escenas 3D volcadas en pantalla. Los desplazamientos están en píxeles de referencia de 1280x720, la rotación en grados y un zoom de 1 significa sin escalado. La transformación base persiste hasta que la cambias: llama a Update(dt) cada fotograma para aplicarla y avanzar la sacudida, y a Reset() cuando abandonas el stage para que no se arrastre al siguiente. La cámara propia de una escena 3D la estableces en el objeto de escena.
</div>

| Método | Descripción |
| --- | --- |
| `GLOBALCAMERA:SetOffset(x, y)  -> nil` | Desplaza la pantalla (x, y) píxeles. |
| `GLOBALCAMERA:SetZoom(sx, sy)  -> nil` | Amplía la pantalla con factores X e Y independientes. |
| `GLOBALCAMERA:SetUniformZoom(s)  -> nil` | Amplía la pantalla de forma uniforme. |
| `GLOBALCAMERA:SetRotation(deg)  -> nil` | Rota la pantalla alrededor de su centro. |
| `GLOBALCAMERA:GetOffsetX()  -> number` | El desplazamiento X base. |
| `GLOBALCAMERA:GetOffsetY()  -> number` | El desplazamiento Y base. |
| `GLOBALCAMERA:GetZoomX()  -> number` | El factor de zoom X. |
| `GLOBALCAMERA:GetZoomY()  -> number` | El factor de zoom Y. |
| `GLOBALCAMERA:GetRotation()  -> number` | La rotación base en grados. |
| `GLOBALCAMERA:Shake(amplitudePx, seconds, rotAmpDeg?)  -> nil` | Inicia una sacudida que se atenúa linealmente desde la amplitud en píxeles indicada durante los segundos indicados, con un bamboleo rotacional opcional en grados. La cámara ignora una llamada con seconds de 0 o menos; una nueva sacudida reemplaza a la actual solo cuando su amplitud es igual o mayor. |
| `GLOBALCAMERA.IsShaking  -> boolean` | Verdadero mientras una sacudida todavía se está atenuando. |
| `GLOBALCAMERA:Update(dt)  -> nil` | Avanza la sacudida dt segundos (limitado a 0.25) y aplica a la pantalla la transformación base más la sacudida. |
| `GLOBALCAMERA:Reset()  -> nil` | Recentra la cámara, reinicia el zoom y la rotación y detiene cualquier sacudida. |

```lua
function update(timestamp)
    if INPUT:Pressed("LRed") or INPUT:Pressed("RRed") then GLOBALCAMERA:Shake(18, 0.35) end
    GLOBALCAMERA:Update(fps.deltaTime)
end

function deactivate()
    GLOBALCAMERA:Reset()
end
```

## Enumeración de canciones

Dos funciones globales informan del estado del escaneo de canciones. Úsalas junto con el callback afterSongEnum.

| Método | Descripción |
| --- | --- |
| `IsSongsEnumerating()  -> boolean` | Verdadero mientras se está ejecutando una pasada de enumeración de canciones. |
| `IsSongsEnumDone()  -> boolean` | Verdadero una vez que el escaneo de canciones ha terminado. Es falso antes de que empiece el escaneo y mientras se ejecuta; IsSongsEnumerating es falso tanto en el estado sin iniciar como en el terminado, así que consulta esta función para saber que la lista está lista. |

## Diagnóstico

### info

Objeto de solo lectura con el estado básico del juego y el directorio del propio módulo.

<div class="callout warn">
Registrado como la global info y creado por módulo. Cada campo calcula su valor en el momento del acceso; online consulta al sistema operativo cada vez que lo lees.
</div>

| Método | Descripción |
| --- | --- |
| `info.playerCount  -> number` | El número de jugadores configurado. |
| `info.lang  -> string` | El código del idioma actual. |
| `info.simplemode  -> boolean` | Verdadero cuando el Modo Simple está activado. |
| `info.p1IsBlue  -> boolean` | Verdadero cuando el jugador 1 usa el lado azul. |
| `info.online  -> boolean` | Verdadero cuando hay disponible una interfaz de red utilizable. |
| `info.dir  -> string` | El directorio de este módulo. |

### fps

Objeto de solo lectura con la temporización de fotogramas y un reloj de alta resolución.

| Método | Descripción |
| --- | --- |
| `fps.deltaTime  -> number` | Segundos transcurridos desde el fotograma anterior. |
| `fps.fps  -> number` | Los fotogramas por segundo medidos actualmente. |
| `fps.ms  -> number` | Un reloj monótono en milisegundos, para cronometrar secciones de Lua tomando diferencias. |

### debugLog

| Método | Descripción |
| --- | --- |
| `debugLog(message)  -> nil` | Escribe la cadena en el registro de traza del motor con un prefijo que la marca como registro de Lua. |

## Otras globales

El motor registra estas globales en todos los módulos; sus propias páginas las documentan.

| Global | Página |
| --- | --- |
| `GetSaveFile(player)` | [Jugadores y perfiles](players.md). Devuelve un handle de solo lectura dentro de las ROActivities y los fondos. |
| `RequestSongList(settings)`, `GenerateSongListSettings()` | [Canciones y charts](songs.md). |
| `MODICONS` | [Canciones y charts](songs.md). |
| `CONFIG`, `DATABASE`, `SHARED`, `STORAGE`, `JSONLOADER`, `INILOADER`, `SQL` | [Datos y persistencia](data.md). |
| `TEXTURE`, `CANVAS`, `GRAPHICS`, `TEXT`, `VIDEO`, `COLOR`, `GRADIENT`, `SIZE` | [Gráficos y texto](graphics.md). |
| `SOUND`, `HITSOUNDSLIST` | [Audio](audio.md). |
| `INPUT` | [Entrada](input.md). |
| `NAMEPLATE`, `NAMEPLATESLIST`, `CHARACTER`, `CHARACTERLIST`, `PUCHICHARALIST`, `PLAYSTATE`, `THEME`, `LANG` | [Jugadores y perfiles](players.md). |
| `VECTOR`, `VECTOR2`, `VECTOR3`, `VECTOR4`, `MATRIX`, `MATRIX2`, `MATRIX3`, `MATRIX4`, `QUATERNION` | [Matemáticas](math.md). |
| `SONGMOUNT`, `REPLAY`, `DANBUILDER`, `VIRTUALSLOTS` | [Canciones y charts](songs.md). |
| `NET` | [Red en línea](networking.md). |
| `SCENE3D`, `MODEL`, `PHYSICS`, `COLLIDERS`, `PATHFIND`, `HEIGHTMAP` | [Motor 3D: mundo del rasterizador](3d.md), [Motor 3D: mundo del trazador de rutas](3d-raytrace.md), [Motor 3D: físicas](3d-physics.md) <span class="badge-exp">Experimental</span>. |
