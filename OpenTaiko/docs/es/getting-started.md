<!-- getting-started.md -->

# Cómo funcionan los módulos

OpenTaiko 0.6.1 permite que un skin añada y reemplace pantallas con Lua. La carpeta Modules de un skin contiene una carpeta por módulo, y cada módulo tiene un Script.lua que define un conjunto fijo de funciones de callback globales. El juego carga cada Script.lua en su propio estado Lua aislado (sandbox), registra las globales del motor (TEXTURE, SOUND, INPUT, CONFIG y las demás documentadas en la [referencia de la API](api/README.md)) y llama a los callbacks en el momento adecuado. [Módulos y ciclo de vida](api/activities.md) recoge las firmas exactas.

## Antes de empezar

- OpenTaiko 0.6.1 y una carpeta de skin con un directorio Modules. El skin incluido es System/Open-World Memories.
- Un editor de texto y nociones básicas de Lua (funciones, tablas, require).
- Los stages incluidos en System/Open-World Memories/Modules/Stages. Los pequeños, demo1 y demo3, muestran la forma de los callbacks; los grandes muestran cómo se organizan pantallas reales.

## Dónde viven los módulos

Cada tipo de módulo tiene su propia carpeta dentro de Modules, y cada módulo es una carpeta cuyo nombre es el id del módulo:

```
Modules/
  Stages/        <name>/Script.lua   pantallas completas
  Activities/    <name>/Script.lua   subpantallas dirigidas por un stage
  ROActivities/  <name>/Script.lua   subpantallas y superposiciones de solo lectura
  Transitions/   <name>/Script.lua   fundidos entre stages (se cargan primero)
  Lib/           archivos .lua compartidos accesibles con require; no se escanean como módulos
```

El archivo de entrada es siempre Script.lua. Las rutas de recursos que pasas a TEXTURE, SOUND, VIDEO y los demás cargadores son relativas a la carpeta del módulo; por convención, los módulos incluidos los guardan en las subcarpetas Textures, Sounds, Videos y Databases, y colocan las traducciones en una carpeta lang.

Dos tipos de script viven en otro lugar:

- Los fondos (fondos de pantalla, capas de juego, mobs, animaciones de clear, el kusudama) son archivos Script.lua dentro de la carpeta Graphics del skin, en el directorio de la pantalla que decoran. Consulta la sección Fondos de [Módulos y ciclo de vida](api/activities.md).
- Los personajes son carpetas dentro de Global/Characters. Una carpeta de personaje puede llevar su propio Script.lua; sin él, el juego usa su script de personaje integrado. Consulta [Añadir personajes](guides/characters.md).

## Script.lua define funciones globales

El Script.lua de un módulo define funciones globales de nivel superior con nombres fijos, y el juego lee cada una como una global. El juego nunca encuentra una función envuelta en una tabla local y devuelta, y nunca llama a un nombre mal escrito (OnStart en lugar de onStart), porque trata un callback no definido como una operación nula y no informa de nada. Todo lo demás en el archivo puede ser local, y puedes repartir el módulo en varios archivos cargados con require.

demo3 es la forma mínima que conviene copiar:

```lua
-- Modules/Stages/mystage/Script.lua
local text = nil
local textTex = nil

function onStart()          -- una vez cuando se carga el skin: carga aquí los recursos
    text = TEXT:Create(16)
end

function activate()         -- cada vez que se entra en el stage
    textTex = text:GetText("Hello")
end

function update(timestamp)  -- cada fotograma: entrada y cambios de estado
    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        return Exit("title", nil)
    end
end

function draw()             -- cada fotograma: solo dibujo
    if textTex ~= nil then textTex:Draw(200, 200) end
end

function deactivate()       -- cuando se abandona el stage: detener sonidos, cerrar bases de datos
end

function onDestroy()        -- antes de descargar el skin: liberar lo que creaste
    if textTex ~= nil then textTex:Dispose() end
end
```

## El ciclo de vida de un stage

- onStart(): el juego lo llama una vez después de cargar el skin, y de nuevo tras cada recarga del skin, esté o no el stage en pantalla. Carga aquí texturas, sonidos y vídeos. Se ejecuta como corrutina, así que una carga larga puede repartirse entre fotogramas detrás de la barra de carga con la utilidad LOADING.
- activate(): el juego lo llama cada vez que se entra en el stage. Reinicia aquí el estado por visita, arranca la música y abre las bases de datos. También se ejecuta como corrutina y puede usar LOADING. El juego actualiza las listas de personajes y puchicharas (CHARACTERLIST, PUCHICHARALIST) justo antes de que se ejecute activate, así que léelas aquí; onStart se ejecuta antes de esa actualización.
- update(timestamp): el juego lo llama cada fotograma antes de draw y le pasa el reloj del juego en milisegundos. Gestiona aquí la entrada y cambia el estado. Una vez que el stage ha llamado a Exit, el juego deja de llamar a update y sigue llamando a draw durante el fundido de salida.
- draw(): el juego lo llama cada fotograma. Solo dibuja, y mantén bajas las asignaciones por fotograma.
- deactivate(): el juego lo llama al abandonar el stage. demo3 libera aquí sus bases de datos; demo1 detiene su música y su vídeo.
- afterSongEnum(): el juego lo llama cada vez que termina la enumeración de canciones, en el arranque y tras una recarga suave o completa, incluso cuando el stage no está activo. Úsalo cuando el módulo dependa de la lista de canciones.
- onDestroy(): el juego lo llama antes de descargar el skin. demo1 libera aquí su textura, su vídeo, su textura de texto y sus sonidos.
- reloadLanguage(lang): el juego lo llama cuando cambia el idioma (consulta la sección de localización más abajo).

Cuando se carga un skin, el juego crea los módulos tipo por tipo: primero Transitions, luego Stages, Activities y ROActivities. Dentro de cada tipo ejecuta todos los Script.lua antes de llamar a ningún onStart. Por tanto, las Activities y ROActivities aún no existen mientras se ejecuta el onStart de un stage; búscalas en activate.

Los demás tipos usan variaciones de este conjunto. Las Activities y ROActivities tienen los mismos callbacks, pero el stage que las aloja invoca activate, deactivate, draw y update y recibe sus valores de retorno. Los fondos reciben un objeto de estado en activate(state), update(timestamp, state) y draw(state), y pueden definir ganchos de evento como clearIn, playEndAnime y kusuBroke. Las transiciones definen fadeOut(t), loading(progress, elapsed) y fadeIn(t). Los personajes definen su propio conjunto de animaciones y voces. [Módulos y ciclo de vida](api/activities.md) los lista todos.

## Elegir un tipo de módulo

- Stage (Modules/Stages): una pantalla completa a la que el juego cambia. Es dueño del fotograma, gestiona la entrada y sale llamando a Exit. Úsalo para cualquier cosa que sea una pantalla por sí misma.
- Activity (Modules/Activities): una subpantalla que un stage usa desde dentro, como un diálogo. Es un singleton que obtienes con ACTIVITY:GetActivity(name); el stage anfitrión llama a su Activate, Update, Draw y Deactivate. Úsala para piezas compartidas que pueden escribir el estado del juego.
- ROActivity (Modules/ROActivities): la forma de solo lectura de una Activity, que obtienes con ROACTIVITY:GetROActivity(name). Recibe CONFIG, DATABASE y GetSaveFile de solo lectura y no tiene la global ACTIVITY. Úsala para piezas que solo leen estado, lo que cubre la mayor parte de la interfaz reutilizable. El motor aloja varias de sus propias superposiciones como ROActivities con nombres fijos (nameplate, modal, modicons, danplate, popup_menu, config_ui, song_enum); un skin reemplaza una de ellas incluyendo una carpeta con ese nombre y conservando los callbacks que el motor llama.
- Fondo (Background): un Script.lua dentro de Graphics que dibuja detrás o encima de una de las pantallas del motor. Los fondos reciben las mismas globales de solo lectura que las ROActivities.
- Transición (Modules/Transitions): el fundido de salida, la pantalla de carga y el fundido de entrada que el juego reproduce entre stages. Un stage elige una por nombre en el tercer argumento de Exit; el juego recurre a la llamada default cuando el stage no indica ninguna o el nombre no existe, y reproduce la llamada song_loading para entrar en el juego.
- Personaje: consulta [Añadir personajes](guides/characters.md).

## Salir de un stage con Exit

Solo los stages tienen la global Exit. Acepta hasta tres argumentos y admite nil en cualquier posición: el destino ("title", "play", "stage" o "legacy"; nil significa "title"), el nombre del stage de destino cuando el destino es "stage" (o una clave legacy cuando es "legacy"), y el nombre de un módulo de transición. Los stages incluidos escriben `return Exit(...)` dentro de update para que nada más se ejecute en ese fotograma.

```lua
-- de demo1/Script.lua, dentro de update()
if INPUT:KeyboardPressed("S") == true then
    sounds.Skip:Play()
    return Exit("stage", "demo2")   -- salta a Modules/Stages/demo2
end
-- ...
return Exit("title", nil)           -- vuelve a la pantalla de título
```

## El sandbox

Cada Script.lua se ejecuta en un estado Lua restringido:

- os conserva solo time, date y difftime. El sandbox elimina io, debug, loadfile y dofile, e import no hace nada.
- package se reduce a un cargador personalizado: package.path y package.cpath están vacíos y el sandbox reemplaza los buscadores estándar, así que solo se pueden buscar las rutas indicadas más abajo.
- require busca primero en la propia carpeta del módulo, luego en la carpeta Modules/Lib del skin, y carga el primer archivo que encuentra. Un archivo del módulo y un archivo de Lib con el mismo nombre se resuelven al archivo del módulo. Los puntos del nombre se convierten en separadores de ruta, así que require("DBControllers.dbScores") y require("DBControllers/dbScores") cargan ambos DBControllers/dbScores.lua. Las rutas no ASCII funcionan.

```lua
-- de intro_nokon/Script.lua
local DBScores  = require("DBControllers/dbScores")  -- la subcarpeta propia del módulo
local I18N      = require("i18n")                     -- Modules/Lib/i18n.lua
local Opening   = require("opening")                  -- la carpeta del módulo
local Dialogue  = require("nokon_dialogue")           -- la carpeta del módulo
```

## Módulos de solo lectura

El juego crea las ROActivities y los fondos con globales restringidas antes de que se ejecute su Script.lua: CONFIG es una vista de solo lectura, GetSaveFile(player) devuelve un archivo de guardado de solo lectura, DATABASE abre almacenes de solo lectura y ACTIVITY es nil (usa ROACTIVITY). Una escritura a través de cualquiera de ellos registra una notificación de error, no hace nada y no lanza ningún error de Lua. Un módulo que necesite cambiar ajustes, datos de guardado o una base de datos debe ser una Activity o un Stage.

## Localización con lang/

El skin incluido traduce las cadenas propias de cada módulo con la biblioteca compartida Modules/Lib/i18n.lua. La cadena en inglés del código es la clave: el módulo incluye un lang/ja.lua que devuelve una tabla que asocia cada cadena en inglés con su traducción al japonés, y la biblioteca busca las cadenas en esa tabla.

La biblioteca tiene tres funciones:

- detect() lee el idioma actual del juego a través de la global LANG y carga el diccionario correspondiente. Cuando el idioma es japonés, hace require de lang/ja, que se resuelve dentro de la carpeta del módulo, así que cada módulo tiene su propio diccionario; para cualquier otro idioma no carga nada. Hasta que la llames, no hay ningún diccionario cargado y todas las cadenas siguen en inglés.
- tr(s) devuelve la traducción de s del diccionario cargado, o la propia s cuando el diccionario no tiene entrada para ella o no hay ningún diccionario cargado.
- trf(fmt, ...) traduce la cadena de formato fmt de la misma manera y luego la formatea con string.format.

Llama a detect() en activate y luego construye tu texto con tr y trf. activate se ejecuta en cada entrada al módulo, así que un idioma que el jugador cambió en los ajustes surte efecto en la siguiente visita y el módulo no necesita ningún otro gancho. Las claves deben coincidir exactamente con el texto original en inglés, incluidos puntuación, espacios y saltos de línea, y la traducción debe conservar tal cual los marcadores como %s o {Player 1 name}.

```lua
-- Modules/Stages/mystage/lang/ja.lua
local T = {}
T["Nokon"] = "ノコン"
T["Alright, quiz time!"] = "さあ、クイズの時間である！"
return T
```

```lua
-- Modules/Stages/mystage/Script.lua
local I18N = require("i18n")
local title

function activate()
    I18N.detect()
    title = I18N.tr("Alright, quiz time!")
end
```

El juego también llama a una global reloadLanguage(lang) en cada módulo cargado cuando cambia el idioma. Solo la necesita un módulo que permanece en pantalla mientras cambia el idioma, como una pantalla con un selector de idioma; ahí, vuelve a llamar a detect() y reconstruye el texto prerrenderizado.

## Cosas a tener en cuenta

- El juego libera las texturas, sonidos, vídeos y objetos de texto que creó un módulo cuando libera el propio módulo, así que una recarga del skin no los filtra. Libera en deactivate los recursos que abres por visita, como las bases de datos, como hace demo3, y libera en onDestroy lo que creaste, como hace demo1.
- GetText guarda en caché una textura por cada cadena distinta en su objeto de texto. Una cadena que cambia cada fotograma añade una textura cada fotograma y el juego se ralentiza progresivamente. Dibuja los valores cambiantes con el renderizador de glifos (TEXT:CreateGlyphCached) o conserva una textura hasta que el valor cambie.
- LOADING solo funciona en callbacks que se ejecutan como corrutinas: el onStart de cualquier módulo y el activate de un stage. Llamar a LOADING:Tick desde el activate de una Activity, o desde update o draw, lanza un error de Lua.
- onStart y afterSongEnum se ejecutan mientras el módulo está fuera de pantalla. Escríbelos de forma que funcionen sin que el stage esté visible.
