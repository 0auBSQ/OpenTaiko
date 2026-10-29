<!-- guides/skins.md -->

# Añadir skins y temas

Un skin es una carpeta dentro del directorio `System/` del juego. Proporciona los gráficos, sonidos, fuentes, valores de disposición, archivos de configuración regional y los módulos Lua que dibujan cada pantalla. Esta guía explica qué convierte una carpeta en un skin, las claves de `SkinConfig.ini`, la estructura de carpetas, el árbol de módulos Lua y su ciclo de vida, y cómo instalas y seleccionas un skin. La referencia de la API cubre la API Lua en sí (dibujo, sonido, entrada, etc.).

Un skin también contiene los módulos Lua que ejecutan cada pantalla. El juego carga módulos concretos por nombre y salta a stages concretos, así que el selector de skins lista un skin construido desde cero pero el juego no puede ejecutarlo. Parte de una copia del skin incluido.

## Antes de empezar

- OpenTaiko 0.6.1 instalado, con el skin incluido `System/Open-World Memories/` presente.
- Un editor de texto plano para `SkinConfig.ini`, los archivos `*Config.ini` incluidos y los módulos Lua.
- Nociones básicas de Lua si pretendes cambiar el comportamiento de las pantallas. Un simple cambio de texturas (reemplazar archivos PNG y OGG y editar valores `.ini`) no necesita Lua.

## Paso 1: Entender qué convierte una carpeta en un skin

Al arrancar, el juego lista las subcarpetas de `System/`. Una carpeta cuenta como skin solo si existe `Graphics/1_Title/Background.png` dentro de ella; el juego omite cualquier otra carpeta. Si la carpeta del skin seleccionado no existe, el juego recurre a `System/Default/`, luego al primer skin válido en orden alfabético, y luego al propio `System/`.

Esta comprobación solo hace que la carpeta aparezca en la lista. El Paso 8 nombra los módulos que deben existir además para que el skin funcione.

```
System/
  Open-World Memories/         <- el skin incluido
  My New Skin/                 <- tu skin
    Graphics/
      1_Title/
        Background.png          <- obligatorio para que la carpeta aparezca en la lista
    SkinConfig.ini
```

## Paso 2: Copiar el skin incluido

Copia `System/Open-World Memories/` a una nueva carpeta hermana, por ejemplo `System/My New Skin/`. La copia contiene todo lo que el juego necesita: `Graphics/`, `Sounds/`, `Fonts/`, `Locales/`, `Modules/`, `ThemeSettings.json`, `SkinConfig.ini` y los archivos `*Config.ini` que incluye. El nombre de carpeta es la identidad del skin (el juego lo registra como skin seleccionado y el selector de skins lo muestra), así que mantenlo seguro para el sistema de archivos. Después edita `SkinConfig.ini` para que los metadatos describan tu skin.

## Paso 3: Editar SkinConfig.ini

`SkinConfig.ini` es un archivo `Key=Value`, un ajuste por línea. El analizador recorta los espacios y tabuladores iniciales, trata las líneas que empiezan por `;` como comentarios, y lee una línea solo cuando contiene exactamente un `=`. La comparación de claves es exacta, y el analizador ignora una clave desconocida sin informar de ningún error. Las claves a nivel de skin son:

- `Name=`: nombre de visualización. Solo metadatos; el nombre de carpeta selecciona el skin.
- `Version=`, `Creator=`: cadenas libres (por defecto `Unknown`). El juego no las valida.
- `DefaultLocale=`: id de configuración regional que el juego usa cuando el idioma activo del juego no tiene archivo en `Locales/` (por defecto `en`).
- `Resolution=W,H`: la resolución para la que diseñas los valores de disposición (por defecto `1280,720`). El skin incluido usa `1920,1080`.
- `Resolutions=`: multiplicadores de escala de renderizado seleccionables (Paso 4).
- `AIBattleCharacter=`: carpeta de personaje usada para el oponente IA (Paso 5).
- `AIBattleNameplate=`: id de la placa de nombre que lleva el oponente IA (Paso 5).
- `FontName<LANG>=` y `BoxFontName<LANG>=`: archivo de fuente por idioma del juego, donde `<LANG>` es el código de idioma en mayúsculas (`EN`, `JA`, `FR`, `ES`, `NL`, `DE`, `RU`, `KO`, `ZH`). La ruta es relativa a la raíz del skin (una ruta absoluta también funciona) y el archivo debe existir; si no, el analizador descarta la clave.
- `FontKerning=`: `1` (por defecto) aplica el interletraje (kerning) de la fuente entre letras vecinas, de modo que pares como `AV` o `Ta` quedan más juntos; `0` lo desactiva. El texto vertical siempre se compone un carácter por celda, sin kerning.
- `FontPalt=`: `1` activa los anchos proporcionales de la fuente para el japonés (la característica OpenType `palt`): los kana, la puntuación y los corchetes ocupan su propio ancho en lugar de un cuadrado completo. `0` (por defecto) mantiene el espaciado de ancho completo. Solo cambian las fuentes que tienen esa característica.

Todas las demás claves (`Game_*`, `Result_*`, `Title_*`, etc.) son valores de disposición de pantalla. El mismo analizador las lee, y por eso pueden vivir en los archivos incluidos del Paso 6.

```ini
;Información del skin
Name=My New Skin
DefaultLocale=en
Version=1.0.0
Creator=Your Name
Resolution=1920,1080
;Multiplicadores de escala de renderizado seleccionables (<=1; decimal o fracción a/b, separados por comas). 1 siempre está disponible y es el valor por defecto.
Resolutions=1,2/3,1/3
;Carpeta de personaje usada para la ranura de batalla contra la IA.
AIBattleCharacter=10v2 - AItritus
;Placa de nombre que lleva el rival de la batalla contra la IA (un id de la lista de placas de nombre).
AIBattleNameplate=66
FontNameEN=Fonts/MPLUSRounded1c-Medium.ttf
FontNameJA=Fonts/MPLUSRounded1c-Medium.ttf
BoxFontNameEN=Fonts/MPLUSRounded1c-Regular.ttf
BoxFontNameJA=Fonts/MPLUSRounded1c-Regular.ttf
FontKerning=1
FontPalt=0
```

## Paso 4: La opción Resolutions

`Resolutions=` es una lista separada por comas de los multiplicadores de escala de renderizado que ofrece el menú de ajustes. El juego renderiza a `Resolution` por el multiplicador elegido y escala el resultado a la ventana; el tamaño de la ventana no cambia. Cada token es un decimal (`0.5`) o una fracción (`2/3`). El analizador descarta los tokens fuera del rango 0 < valor <= 1, los tokens no analizables y los duplicados, añade `1` si falta y ordena la lista con `1` en primer lugar. El separador debe ser una coma, porque un punto y coma inicia una línea de comentario. El menú de ajustes muestra cada entrada con su tamaño en píxeles, por ejemplo `2/3 (1280x720)` para un skin de 1920x1080.

```ini
Resolution=1920,1080
Resolutions=1,2/3,1/3
; produce las opciones:
;   1     -> 1920x1080  (por defecto)
;   2/3   -> 1280x720
;   1/3   -> 640x360
```

## Paso 5: Las opciones de la batalla contra la IA

`AIBattleCharacter=` nombra la carpeta dentro de `Global/Characters/` que el juego usa para el oponente IA en el modo de batalla contra la IA. El valor por defecto es `10v2 - AItritus`. La carpeta indicada debe existir.

`AIBattleNameplate=` indica por su id la placa de nombre que lleva el oponente IA (consulta [Añadir placas de nombre](nameplates.md)). El valor por defecto es `66`. Con un id que no figura en la lista de placas de nombre, el juego muestra la placa de la IA sin título.

```ini
;Carpeta de personaje usada para la ranura de batalla contra la IA.
AIBattleCharacter=10v2 - AItritus
;Placa de nombre que lleva el rival de la batalla contra la IA (un id de la lista de placas de nombre).
AIBattleNameplate=66
```

## Paso 6: Dividir la configuración con #include

Cuando el analizador encuentra una línea de la forma `#include SomeFile.ini`, lee ese archivo en su lugar, de forma recursiva. La ruta es relativa a la raíz del skin. El `SkinConfig.ini` incluido contiene solo los metadatos y las claves de fuente, y luego incluye un archivo por pantalla. Conserva estas líneas cuando copies el skin, y edita los archivos `*Config.ini` individuales para reajustar una pantalla.

```
; final de SkinConfig.ini (skin incluido, en orden)
#include OtherConfig.ini
#include TitleConfig.ini
#include ConfigConfig.ini
#include SongSelectConfig.ini
#include SongLoadingConfig.ini
#include GameConfig.ini
#include ModIconsConfig.ini
#include NameplateConfig.ini
#include AIResultConfig.ini
#include ResultConfig.ini
#include DaniSelectConfig.ini
#include DanResultConfig.ini
#include OnlineLoungeConfig.ini
#include ModalConfig.ini
#include Game4PConfig.ini
#include Result4PConfig.ini
#include Modal4PConfig.ini
```

## Paso 7: Conocer la estructura de carpetas del skin

Con el skin incluido como referencia, la raíz del skin contiene:

- `Graphics/`: imágenes agrupadas en carpetas numeradas por pantalla (`0_Startup`, `1_Title`, `2_Config`, `3_DaniSelect`, `5_Game`, `6_Result`, `7_DanResult`, `8_TowerResult`, `12_OnlineLounge`) más algunas imágenes compartidas en el nivel superior. Los fondos animados son archivos `Script.lua` que están junto a las imágenes de la carpeta a la que pertenecen (por ejemplo `Graphics/0_Startup/Script.lua` y las carpetas dentro de `Graphics/5_Game/5_Background/`). Un aspecto de torre es una carpeta bajo `Graphics/5_Game/5_Background/Tower/Down/` que contiene el cielo, los pisos (`Base/`), el tejado (`Top.png`), una decoración opcional (`Deco/`) y su disposición en `Config.json`; un chart lo elige con `TOWERTYPE:`, y la selección de torre, la pantalla de carga y el resultado dibujan la misma torre a partir de su propia copia del aspecto, una carpeta con el mismo nombre en `Modules/Lib/TowerArt/` con las piezas a la mitad de tamaño y un `Config.json` propio (`floor_height`, y `deco_dx`/`deco_dy`, el desplazamiento de la decoración desde el centro inferior de su piso, en píxeles de esas piezas), así que un aspecto nuevo necesita ambas carpetas. Las notas, los redobles, los globos, los efectos de golpe y los textos de juicio están en paquetes dentro de `Graphics/5_Game/` entre los que elige el jugador (consulta [Añadir paquetes de notas y juicios](notes-skins.md)).
- `Sounds/`: sonidos del sistema y BGM que el juego carga por nombre de archivo fijo, por ejemplo `Sounds/Move.ogg`, `Sounds/Decide.ogg`, `Sounds/Cancel.ogg`, `Sounds/BGM/Title.ogg`, `Sounds/BGM/SongSelect.ogg`, `Sounds/BGM/Result.ogg`. Si falta un archivo, ese sonido no se reproduce.
- `Fonts/`: los archivos `.ttf` referenciados por las claves `FontName`.
- `Locales/`: una carpeta por idioma (`en/`, `ja/`, ...) con cualquier número de archivos JSON, uno por pantalla o función (`coin_shop.json`, `items.json`, ...), cada uno con la forma `{ "Entries": { "KEY": "text" } }`; el juego lee todos los archivos de la carpeta (también lee un único archivo `Locales/<lang>.json`). Estas cadenas etiquetan las pantallas y los ajustes propios del skin, y Lua las lee a través de `THEME:GetSkinString(key)`. Cuando una clave falta en el idioma activo, el juego la busca en el idioma de `DefaultLocale`.
- `Modules/`: el árbol de módulos Lua (Paso 8).
- `ThemeSettings.json`: un array de ajustes que la pantalla de opciones muestra bajo Ajustes de tema. Cada entrada tiene `id`, `type` (`bool`, `int`, `double`, `string`, `enum`, o `key` para una tecla del teclado que la pantalla de opciones captura y guarda por su nombre para `INPUT:KeyboardPressed`), `scope` (`global`, el valor por defecto, o `save` para un valor por archivo de guardado), `label` y `description` localizados, `default`, y `min`/`max` u `options` según el tipo. El archivo puede ser en su lugar un objeto con `sections` (id a título localizado, declarado una vez) y `settings` (el array); el `section` opcional de una entrada nombra uno de esos ids, o es un texto de título simple, y la agrupa bajo ese subtítulo en la página del tema. `session: true` mantiene el valor solo en memoria (vuelve al valor por defecto en cada inicio), `hidden: true` lo deja fuera del menú de ajustes para que el skin lo lea y escriba por su cuenta, y `step` fija el incremento de un int.
- `SkinConfig.ini` y los archivos `*Config.ini` incluidos.
- `README.txt`, `LICENSE.md`, `Licenses/`: archivos de atribución. El juego no los lee.

```
My New Skin/
  SkinConfig.ini
  ThemeSettings.json
  Graphics/           imágenes por pantalla; algunas carpetas llevan un Script.lua de fondo
  Sounds/             sonidos del sistema .ogg con nombre fijo y BGM/
  Fonts/              archivos .ttf nombrados por las claves FontName
  Locales/            en/, ja/, ... one .json per screen ({ "Entries": { ... } })
  Modules/            el árbol de módulos Lua (Paso 8)
  <screen>Config.ini  archivos de disposición incorporados mediante #include
```

## Paso 8: El árbol Modules y los módulos que el juego requiere

Cuando se carga el skin, el juego escanea cuatro subcarpetas de `Modules/` y trata cada subcarpeta directa dentro de ellas como un módulo cuyo archivo de entrada es `Script.lua`:

- `Modules/Transitions/`: transiciones que se reproducen entre stages. El juego las carga primero, para que estén listas para el primer cambio de stage.
- `Modules/Stages/`: pantallas completas. Entras en un stage con `Exit("stage", "<folder name>")`.
- `Modules/Activities/`: subpantallas reutilizables superpuestas a un stage (por ejemplo `confirm_dialog`, `mod_select_dialog`, `song_select_core`).
- `Modules/ROActivities/`: superposiciones de solo lectura que el juego dirige directamente.

El juego no escanea `Modules/Lib/`. Los archivos de ahí los cargas con `require`: un módulo busca en su propia carpeta, luego en `Modules/Lib/` y luego en `Global/Modules/Lib/` (ver más abajo), así que `require("dialogue")` se resuelve a `Modules/Lib/dialogue.lua`.

La carpeta del juego también puede contener módulos que cargan todos los skins. `Global/Modules/` en la carpeta de instalación del juego tiene las mismas subcarpetas `Transitions/`, `Stages/`, `Activities/`, `ROActivities/` y `Lib/` que el `Modules/` de un skin, y el juego carga cada categoría global justo después de la del skin. Un nombre puede existir en los dos sitios. Cuando un módulo del skin pide un stage, una activity, una ROActivity o una transition por su nombre, obtiene el módulo del skin, y un módulo global obtiene el global; cada uno recurre al otro sitio cuando el suyo no tiene un módulo con ese nombre. `require` sigue la misma regla: un módulo global busca en `Global/Modules/Lib/` antes que en el `Modules/Lib/` del skin. Los módulos que el propio juego busca por nombre (en la lista de abajo) vienen primero del skin. `_boot` y `_title` deben quedarse en el skin: cuando `Global/Modules/Stages/` contiene alguno de los dos, el juego se detiene con un error al arrancar.

Los archivos de Lib que el juego incluye en `Global/Modules/Lib/` están documentados en [Módulos proporcionados](../modules/README.md): una biblioteca de interfaz, un cuadro de diálogo, utilidades de entrada y de localización, el motor de mundos 3D, y las reglas de la pagoda, la bóveda y la tienda de monedas, con nombres organizados por espacios de nombres como `require("UI.Pick")` o `require("Core.Easing")`. Del lugar donde viven los archivos globales se derivan algunas reglas:

- Un archivo de Lib se ejecuta en el estado Lua del módulo que lo carga con require. Sus propios require siguen el orden de búsqueda de ese módulo, y las rutas relativas que pasa a `TEXTURE`, `SOUND`, `JSONLOADER` y los demás cargadores se resuelven respecto a la carpeta de ese módulo. Un archivo de Lib que incluye recursos llega a ellos con `require("Core.Here")(...)` y los cargadores de ruta absoluta.
- Los módulos globales no tienen `Locales/` ni `ThemeSettings.json`: `THEME:GetSkinString` y los ajustes del tema leen los del skin activo. Un módulo global guarda sus cadenas en su propia carpeta `lang/` (Paso 10).
- Las texturas, sonidos y cadenas de `SHARED` son un único espacio de nombres para el skin y todos los módulos globales: pon un prefijo a tus claves.
- Una actualización añade y reemplaza archivos pero nunca borra los antiguos, y los módulos del skin buscan en el `Modules/Lib/` del skin antes que en `Global/Modules/Lib/`, así que un archivo obsoleto del skin con el mismo nombre ganaría. Por eso los módulos proporcionados viven en carpetas de espacio de nombres que ningún archivo del skin comparte, y por eso el skin incluido conserva sus antiguos nombres de Lib como reenvíos de dos líneas hacia ellos en lugar de borrar los archivos.

Dentro de cada categoría el juego crea primero todos los módulos y después ejecuta `onStart` en cada uno, en el orden Transitions, Stages, Activities, ROActivities.

El juego busca estos módulos por nombre, y el skin incluido los proporciona todos:

- Stages `_boot` y `_title`. El juego se detiene con un error si falta cualquiera de los dos.
- ROActivities `modal`, `config_ui`, `nameplate`, `popup_menu`, `modicons`, `song_enum`, `cutscene`, `results_tower`, `tower_hud` y `danplate`.
- Transitions `default` y `song_loading`. `song_loading` se reproduce mientras el juego carga una canción; el juego usa `default` cuando `Exit` no nombra ninguna transición o nombra una que no existe. Un skin sin ningún módulo de transición recurre a un fundido a negro simple.

Mantén todos estos en su sitio al construir un skin; añade tus propios módulos junto a ellos.

```
Modules/
  Transitions/   <name>/Script.lua   (se cargan primero; el juego usa "default" y "song_loading")
  Stages/        <name>/Script.lua   ("_boot" y "_title" obligatorios)
  Activities/    <name>/Script.lua
  ROActivities/  <name>/Script.lua   (modal, config_ui, nameplate, popup_menu, modicons, song_enum, cutscene, danplate obligatorios)
  Lib/           archivos .lua compartidos accesibles con require, no se escanean
```

## Paso 9: El Script.lua de un stage y su ciclo de vida

`Script.lua` se ejecuta una vez cuando el juego crea el módulo, con las globales del motor (`TEXTURE`, `SOUND`, `INPUT`, `CONFIG`, `THEME` y el resto) ya definidas. El juego busca entonces funciones globales por nombre y las llama. Para un stage:

- `onStart()`: una vez, cuando se carga el skin. Se ejecuta como corrutina, así que una carga pesada puede llamar a `coroutine.yield()` o a las utilidades `LOADING` para repartir el trabajo entre fotogramas detrás de la barra de carga.
- `activate()`: cada vez que el juego entra en el stage. También es una corrutina. El juego actualiza las globales `CHARACTERLIST` y `PUCHICHARALIST` justo antes de que se ejecute, así que construye aquí todo lo que dependa de ellas; en `onStart` todavía están vacías.
- `update(timestamp)`: cada fotograma. Devuelve `Exit(target, name, transition)` para abandonar el stage. `target` es `"title"`, `"play"`, `"stage"` (con `name` = una carpeta de stage) o `"legacy"` (con `name` = `heya`, `config`, `exit` u `onlinelounge`); `transition` es una carpeta dentro de `Modules/Transitions/` y por defecto es `default`.
- `draw()`: cada fotograma.
- `deactivate()`: cuando el juego abandona el stage.
- `afterSongEnum()`: cuando la lista de canciones ha terminado de enumerarse.
- `onDestroy()`: cuando el juego desmonta el skin.

Todas son opcionales; el juego omite una función que no defines. Las Activities, ROActivities y Transitions siguen el mismo patrón con sus propios conjuntos de ganchos.

```lua
-- Modules/Stages/my_stage/Script.lua
function onStart()
  -- configuración única; puede hacer coroutine.yield() durante cargas pesadas
end

function activate()
  -- se ejecuta cada vez que se entra en el stage
end

function update(ts)
  if INPUT:Pressed("Cancel") then
    return Exit("stage", "_title")   -- abandona este stage
  end
  return nil
end

function draw()
  -- renderizado por fotograma
end

function deactivate() end
function afterSongEnum() end
function onDestroy() end
```

## Paso 10: Localizar un módulo con lang/

Un módulo puede guardar sus propias traducciones en una subcarpeta `lang/` junto a `Script.lua`, leídas con el módulo proporcionado [Locale.i18n](../modules/locale.md): un archivo `lang/<code>/<name>.json` por funcionalidad y por idioma, con ids estables como claves, y el inglés de `lang/en/` como reserva. Esto es independiente de la carpeta `Locales/` de todo el skin del Paso 7, y es la única forma que tiene un módulo global de traducir su texto. Algunos stages más antiguos del skin incluido (por ejemplo `Modules/Stages/intro_nokon/lang/ja.lua`) usan la primera forma de la biblioteca, una tabla Lua que asocia cada cadena en inglés con su traducción.

```
Modules/Stages/my_stage/
  Script.lua
  lang/
    en/my_stage.json   -- require("Locale.i18n").texts("my_stage")
    ja/my_stage.json
```

## Paso 11: Instalar y seleccionar el skin

Coloca la carpeta dentro de `System/`. Abre los ajustes, ve a la sección Apariencia y elige el skin en la opción Skin; el selector lista todos los skins válidos por nombre de carpeta y muestra su `Graphics/1_Title/Background.png` como miniatura. Cuando cambias de skin, el juego desmonta el actual, carga el nuevo y recarga todos sus módulos Lua detrás de una barra de carga.

El juego escribe la elección en `Config.ini` como `SkinPath=`, relativa a `System/`. Escribe el nombre de carpeta a secas (por ejemplo `SkinPath=My New Skin\` en Windows) y también acepta la forma `./My New Skin/` que se muestra en el comentario del archivo.

```ini
; En Config.ini (se escribe cuando se elige un skin en el juego):
; Ruta de la carpeta del skin, relativa a System/
SkinPath=My New Skin\
```

## Resolución de problemas y notas

- El selector no lista el skin: falta `Graphics/1_Title/Background.png`, o la carpeta no está directamente dentro de `System/`.
- El juego da error justo después de que selecciones el skin: falta un módulo obligatorio (Paso 8) o uno de ellos lanzó un error de Lua. Prueba un skin cambiando a él.
- Una clave de `SkinConfig.ini` no tiene efecto: la clave está mal escrita, la línea contiene más de un `=`, o el valor no se pudo analizar. El analizador ignora las claves desconocidas sin informar de ellas.
- `Resolutions=` solo muestra `1`: la lista usaba puntos y coma (un marcador de comentario) o todos los valores estaban fuera de 0 < valor <= 1.
- Una clave de fuente no tiene efecto: la ruta del archivo no existe respecto a la raíz del skin.
- `CHARACTERLIST` o `PUCHICHARALIST` están vacías en `onStart`: el juego las rellena después de crear los módulos. Úsalas desde `activate`.
- Renombrar la carpeta del skin cambia su identidad; `SkinPath` en `Config.ini` debe apuntar al nuevo nombre.
- `Name=`, `Version=` y `Creator=` son solo informativos. El juego no hace ninguna comprobación de compatibilidad con ellos.
