<!-- guides/characters.md -->

# Añadir un personaje

Un personaje es una carpeta dentro de `Global/Characters/` en la carpeta de instalación del juego. Cada subcarpeta que el juego encuentra ahí se convierte en un personaje seleccionable. Una carpeta contiene un `Metadata.json` (nombre, rareza, autor), un `CharaConfig.txt` (posiciones y temporización de animaciones), el contenido de animación y, opcionalmente, `Effects.json`, `Unlock.json`, `Palettes.json` y clips de voz. El contenido de animación es o bien carpetas de fotogramas PNG numerados, que renderiza el script de personaje integrado del juego, o bien cualquier cosa que un `Script.lua` por personaje decida dibujar (la plantilla 3D incluida dibuja un modelo glTF).

Compatibilidad: OpenTaiko 0.6.1 sigue cargando sin cambios los personajes creados para 0.6.0. Esta página describe la estructura actual; úsala para los personajes nuevos.

## Antes de empezar

- OpenTaiko 0.6.1 instalado. El juego lee los personajes de `Global/Characters/` junto al ejecutable del juego, y todos los skins los comparten.
- Un editor de texto para archivos JSON y de estilo INI.
- Para un personaje 2D: el arte exportado como fotogramas PNG numerados (`0.png`, `1.png`, ...) con fondo transparente, una carpeta por estado de animación.
- Para un personaje 3D: un `model.glb` (glTF binario) que contenga los clips de animación, y una imagen fija `Render.png`.
- Las carpetas incluidas `01 - Template` (2D) y `01 - Template3D`. Copia una de ellas como punto de partida.

## Paso 1: Entender el descubrimiento, el orden y la identidad

En el arranque el juego lista las subcarpetas de `Global/Characters/` y crea un personaje por carpeta, en el orden en que las devuelve el sistema de archivos. El juego no ordena la lista, así que las carpetas incluidas llevan un prefijo numérico (`00 - None`, `01 - Template`, `02 - Student (A)`, ...) para que el orden sea predecible. Mantén `00 - None` en primer lugar: el índice 0 es la ranura vacía y la alternativa cuando falta un personaje guardado.

Los archivos de guardado almacenan el personaje elegido por nombre de carpeta (`characterName`) y lo vuelven a resolver a un índice en cada arranque. Añadir o eliminar otras carpetas nunca rompe una selección guardada, pero renombrar una carpeta hace que los guardados que la referenciaban recurran a `00 - None`. Dos personajes pueden compartir nombre de visualización; el nombre de carpeta debe ser único.

El juego enumera los personajes una vez en el arranque y de nuevo al recargar el skin; una carpeta añadida mientras el juego está en ejecución aparece tras el siguiente arranque o recarga del skin.

## Paso 2: Crear la carpeta y Metadata.json

Crea una carpeta como `30 - MyChara` y añade `Metadata.json`:

- `name`: nombre de visualización. Una cadena simple o un objeto localizado `{ "strings": { "default": "...", "ja": "...", ... } }`. `default` es la alternativa; las demás claves son códigos de idioma del juego.
- `rarity`: uno de `Poor`, `Common`, `Uncommon`, `Rare`, `Epic`, `Legendary`, `Mythical`. La rareza solo controla el color y el nivel de la notificación de desbloqueo; todas las rarezas tienen un multiplicador de monedas de 1.
- `author`: cadena simple u objeto localizado.
- `description`: opcional, cadena simple u objeto localizado.
- `speechtext`: opcional, un array de seis objetos localizados que la pantalla de resultados muestra en el bocadillo del personaje. El juego elige la entrada según el resultado, en este orden: fallida con el medidor bajo, fallida con el medidor al 40 % o más, superada, superada con el medidor lleno, full combo, todo perfecto. Si das menos de seis, el juego repite la última.

Si falta `Metadata.json`, el personaje se carga igualmente con el nombre `(None)`, la rareza `Common` y el autor `(None)`.

```json
{
  "name": {
    "strings": {
      "default": "My Character",
      "ja": "マイキャラ"
    }
  },
  "rarity": "Common",
  "author": {
    "strings": {
      "default": "Your Name"
    }
  }
}
```

## Paso 3 (ruta 2D): Añadir las carpetas de fotogramas

Cuando la carpeta no tiene `Script.lua`, el juego renderiza el personaje con su script integrado (`CharaScript.lua` en la carpeta de instalación del juego). Ese script asigna cada estado de animación a una subcarpeta y carga `0.png`, `1.png`, `2.png`, ... desde ella. La carga se detiene en el primer índice ausente, así que la numeración debe ser contigua.

| Estado de animación | Carpeta |
|---|---|
| Game/Normal, Game/Clear, Game/Max | `Normal`, `Clear`, `Clear_Max` |
| Game/Gogo, Game/Gogo_Max | `GoGo`, `GoGo_Max` |
| Game/Miss, Game/Miss_Down | `Miss`, `MissDown` |
| Game/10combo, Game/10combo_Max | `10combo`, `10combo_Max` |
| Game/Cleared, Game/Failed | `Cleared`, `Failed` |
| Game/Clear_In, Game/Clear_Out | `Clearin`, `ClearOut` |
| Game/Max_In, Game/Max_Out | `Soulin`, `SoulOut` |
| Game/Miss_In, Game/Miss_Down_In, Game/Return | `MissIn`, `MissDownIn`, `Return` |
| Game/GoGoStart, Game/GoGoStart_Clear, Game/GoGoStart_Max | `GoGoStart`, `GoGoStart_Clear`, `GoGoStart_Max` |
| Game/Balloon_Breaking, Game/Balloon_Broke, Game/Balloon_Miss | `Balloon_Breaking`, `Balloon_Broke`, `Balloon_Miss` |
| Game/Kusudama_Breaking, Game/Kusudama_Broke, Game/Kusudama_Miss, Game/Kusudama_Idle | `Kusudama_Breaking`, `Kusudama_Broke`, `Kusudama_Miss`, `Kusudama_Idle` |
| Game/Tower/Standing, Climbing, Running, Clear, Fail (y las variantes `_Tired`) | `Tower_Char/Standing`, `Tower_Char/Climbing`, `Tower_Char/Running`, `Tower_Char/Clear`, `Tower_Char/Fail` (más `Tower_Char/Standing_Tired` y así sucesivamente) |
| Menu/Wait, Menu/Start, Menu/Normal, Menu/Select | `Menu_Wait`, `Menu_Start`, `Menu_Loop`, `Menu_Select` |
| Entry/Normal, Entry/Jump | `Title_Normal`, `Title_Entry` |
| Result/Normal, Result/Clear, Result/Failed_In, Result/Failed | `Result_Normal`, `Result_Clear`, `Result_Failed_In`, `Result_Failed` |

El script integrado lee dos imágenes fijas de la raíz de la carpeta: `Render.png` (el retrato a tamaño completo, dibujado siempre que el juego pide el tipo de animación Render, por ejemplo en la habitación) y `Preview.png` (la miniatura; cuando falta, el script usa `Normal/0.png`).

Los estados ausentes recurren a otro estado, así que un personaje puede incluir solo un subconjunto. La cadena de alternativas es: Clear -> Normal, Max -> Clear, Miss -> Normal, Miss_Down -> Miss, Gogo -> Normal, Gogo_Max -> Gogo, 10combo_Max -> 10combo, GoGoStart_Clear -> GoGoStart, GoGoStart_Max -> GoGoStart_Clear, los estados `_Tired` de Torre -> su estado normal, Tower/Fail -> Tower/Standing_Tired, Kusudama_Idle -> Normal, Menu/Wait -> Gogo, Menu/Start y Menu/Select y Entry/Jump -> 10combo, Menu/Normal y Entry/Normal y Result/Normal -> Normal, Result/Clear -> Clear, Result/Failed_In -> Miss_In, Result/Failed -> Miss. Los estados sin alternativa (por ejemplo Cleared, Failed, Return, los estados de globo) no dibujan nada cuando faltan. El mínimo para un personaje funcional es `Normal/0.png`.

```
30 - MyChara/
  Metadata.json
  CharaConfig.txt
  Render.png
  Normal/0.png 1.png 2.png ...
  Clear/0.png ...
  GoGo/0.png ...
  Miss/0.png ...
  Menu_Loop/0.png ...
  Result_Clear/0.png ...
  Sounds/                (clips de voz opcionales, ver Paso 6)
```

## Paso 4: Escribir CharaConfig.txt

`CharaConfig.txt` es un archivo de texto `Key=Value`; las líneas que empiezan por `;` son comentarios. El script integrado lee estas claves (la plantilla 3D incluida también lee las claves de posición):

- `Chara_Resolution=W,H` (por defecto `1280,720`): la resolución para la que diseñas las coordenadas de abajo. El juego escala las posiciones de esta resolución a la resolución del skin al dibujar.
- `Chara_LegacyMode` (por defecto `1`): conserva el anclaje y las correcciones de desplazamiento de 0.6.0. Los personajes portados de versiones anteriores dependen de él.
- `Game_Chara_X=...` / `Game_Chara_Y=...`: posición durante el juego; el script usa el primer valor de cada lista. `Game_Chara_Offset=X,Y` es una forma alternativa.
- `Game_Chara_X_AI=...` / `Game_Chara_Y_AI=...`: un valor por jugador para la batalla contra la IA. Cuando ambas claves están presentes reemplazan la posición de batalla contra la IA del skin para este personaje.
- `Game_Chara_Balloon_X` / `Game_Chara_Balloon_Y`, `Game_Chara_Kusudama_X` / `Game_Chara_Kusudama_Y`: posiciones durante las secuencias de globo y kusudama (se usa el primer valor). `Game_Chara_Balloon_Offset`, `Game_Chara_Kusudama_Offset` y `Game_Chara_Tower_Offset` reciben un par `X,Y`.
- `Menu_Offset=X,Y`, `Menu_Chara_Scale`, `Result_Offset=X,Y`, `Heya_Chara_Render_Offset=X,Y`: desplazamientos para el menú, los resultados y el render de la habitación.
- `Game_Chara_Motion_<State>=0,1,2,...`: el orden en que se reproducen los fotogramas de un estado, como índices de fotograma desde 0. Cuando se omite, los fotogramas se reproducen en el orden de los archivos. Los nombres de estado siguen los nombres de carpeta, por ejemplo `Game_Chara_Motion_Normal`, `Game_Chara_Motion_GoGo`, `Game_Chara_Motion_Miss_Down`, `Game_Chara_Motion_Balloon_Broke`, `Game_Chara_Motion_Tower_Climbing`.
- `Game_Chara_Beat_<State>=N`: cuántos pulsos abarca un bucle del estado, por ejemplo `Game_Chara_Beat_Normal=1`, `Game_Chara_Beat_GoGo=2`.
- Los estados de menú, título y resultados usan `Menu_Chara_Motion_Loop/Wait/Start/Select`, `Title_Chara_Motion_Normal/Entry`, `Result_Chara_Motion_Normal/Clear/Failed_In/Failed`, con claves `_Beat_` correspondientes o duraciones fijas en milisegundos: `Chara_Menu_Loop_AnimationDuration`, `Chara_Menu_Wait_AnimationDuration`, `Chara_Menu_Start_AnimationDuration`, `Chara_Menu_Select_AnimationDuration`, `Chara_Normal_AnimationDuration`, `Chara_Entry_AnimationDuration`, `Chara_Result_Normal_AnimationDuration`, `Chara_Result_Clear_AnimationDuration`, `Chara_Result_Failed_In_AnimationDuration`, `Chara_Result_Failed_AnimationDuration`.

La lista completa de claves, con sus valores por defecto, es la tabla `load_chara_config_defs` al principio del `CharaScript.lua` integrado. El script ignora las claves que no conoce, así que el `01 - Template/CharaConfig.txt` incluido también contiene algunas claves del lado del skin que no tienen efecto en este archivo.

```ini
Chara_Version=0.6.1.0
Chara_Resolution=1920,1080

;Posición X del personaje (1P,2P)
Game_Chara_X=0,0
;Posición Y del personaje (1P,2P)
Game_Chara_Y=0,805

;Orden de fotogramas del estado normal y pulsos por bucle
Game_Chara_Motion_Normal=0,1,2,3,4,5,6
Game_Chara_Beat_Normal=1

;Orden de fotogramas de GoGo y pulsos por bucle
Game_Chara_Motion_GoGo=0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15
Game_Chara_Beat_GoGo=2
```

## Paso 5 (ruta 3D): Incluir model.glb y un Script.lua por personaje

Cuando existe `Script.lua` en la carpeta del personaje, reemplaza por completo el script integrado. El juego llama entonces a estas funciones globales por nombre:

- `loadAnimation(animationType)`, `disposeAnimation(animationType)`
- `availableAnimation(animationType)` que devuelve un booleano. El juego sigue aceptando la antigua forma mal escrita `avaialbeAnimation`: prueba primero `availableAnimation` y recurre a `avaialbeAnimation`. La plantilla 3D incluida todavía usa el nombre antiguo.
- `setAnimationDuration(animationType, durationMs)`, `resetAnimationCounter(animationType)`
- `update(delta, animationType, looping)` que devuelve `true` cuando una animación sin bucle ha terminado
- `draw(animationType, x, y, scaleX, scaleY, opacity, color, contextType, anchor, clipW, clipH, clipX, clipY, rotation, blendMode, wrapMode, gradientMap)`
- `getDrawSize(animationType)` que devuelve anchura y altura
- `getHeyaRenderOffset()` que devuelve x e y; `getAIBattlePosition(player, charaScale)` que devuelve x e y, o `nil` para usar la posición del skin
- `loadVoice(voiceType)`, `disposeVoice(voiceType)`, `playVoice(voiceType)`

Los tipos de animación son las cadenas que hay detrás de las constantes `CHARACTER.ANIM_*` (`"Game/Normal"`, `"Menu/Normal"`, ...), más los dos tipos especiales `CHARACTER.ANIM_PREVIEW` (miniatura) y `CHARACTER.ANIM_RENDER` (retrato completo). Los tipos de voz son las constantes `CHARACTER.VOICE_*`. La cadena de alternativas del Paso 3 también se aplica a los personajes con script: el juego consulta `availableAnimation` y recorre las alternativas hasta que una está disponible.

La carpeta `01 - Template3D` incluida contiene solo `CharaConfig.txt`, `Effects.json`, `Metadata.json`, `model.glb`, `Render.png` y `Script.lua`. Su script carga `model.glb` con `MODEL:Load`, lo renderiza en una escena que crea con `SCENE3D:CreateScene`, lee las claves de posición de `CharaConfig.txt` y asigna cada tipo de animación a un índice de clip y a un número de pulsos en una tabla `CLIP`. Para hacer un personaje 3D, copia la carpeta, reemplaza `model.glb` y `Render.png`, y edita `CLIP` para que cada tipo apunte al índice de clip correcto de tu modelo.

```lua
-- extracto de 01 - Template3D/Script.lua
local CLIP = {
  [CHARACTER.ANIM_GAME_NORMAL] = { clip = 0,  beat = 1 };
  [CHARACTER.ANIM_GAME_CLEAR]  = { clip = 1,  beat = 1 };
  [CHARACTER.ANIM_GAME_GOGO]   = { clip = 2,  beat = 4 };
  [CHARACTER.ANIM_MENU_NORMAL] = { clip = 14, beat = 2 };
  [CHARACTER.ANIM_RESULT_CLEAR]= { clip = 19, beat = 1 };
  -- una entrada por cada estado de animación que admite el modelo
}

function loadAnimation(animationType)
  -- construye los datos de clip / vista previa / render y márcalos como disponibles
end

function availableAnimation(animationType)
  return animations[animationType] ~= nil
end
```

## Paso 6: Archivos opcionales: Effects.json, Unlock.json, Palettes.json, voces

- `Effects.json`: `gauge` (`Normal`, `Hard` o `Extreme`; por defecto `Normal`) selecciona el tipo de medidor de alma. `Hard` multiplica las ganancias de monedas por 1.5 y `Extreme` por 1.8 salvo que el juego fuerce el medidor normal. Cuando el mod fun Minesweeper está activo, `bombFactor` (1-100, por defecto 20) es el porcentaje de notas que el mod convierte en bombas y `fuseRollFactor` (0-100, por defecto 0) el porcentaje de globos que convierte en redobles de mecha.
- `Unlock.json`: cuando está presente, el personaje permanece bloqueado hasta que el jugador cumple la condición. El formato y los ids de condición coinciden con los de las canciones; consulta la guía de desbloqueables. El jugador compra las condiciones de monedas en la pantalla de la habitación; el juego comprueba las demás condiciones automáticamente en la pantalla de resultados. Ejemplos incluidos: Kuro usa `{ "condition": "dp", "type": "me", "values": [3, 3, 10] }` (diez clears de charts Extreme con full combo o mejor) y Aoi usa `{ "condition": "ch", "type": "me", "values": [200] }` (200 monedas).
- `Palettes.json`: un array de paletas de color que el jugador puede aplicar al personaje. Cada entrada tiene `name`, `blend` (0-1), `stops` (un array de paradas de gradiente `[position, R, G, B]` o `[position, R, G, B, A]`; indica al menos dos) y `plays`, el número de partidas con este personaje que desbloquea la paleta (0 o ausente significa disponible de inmediato). Una entrada con `"stops": null` es el valor por defecto sin tinte.
- Voces: el script integrado carga archivos `.ogg` desde rutas fijas dentro de la carpeta del personaje, por ejemplo `Sounds/Clear/Clear.ogg`, `Sounds/Clear/Failed.ogg`, `Sounds/Clear/FullCombo.ogg`, `Sounds/Clear/AllPerfect.ogg`, `Sounds/Menu/SongSelect.ogg`, `Sounds/Menu/SongDecide.ogg`, `Sounds/Menu/DiffSelect.ogg`, `Sounds/Title/Sanka.ogg`, `Sounds/Result/BestScore.ogg`, `Sounds/Result/ClearSuccess.ogg`, `Sounds/Result/ClearFailed.ogg`. La lista completa es la tabla `voice_files` al principio del `CharaScript.lua` integrado. El script omite los archivos ausentes.

```json
{
  "gauge": "Normal",
  "bombFactor": 20,
  "fuseRollFactor": 0
}
```

```json
{
  "condition": "ch",
  "type": "me",
  "values": [ 200 ]
}
```

```json
[
  { "name": "Default", "stops": null },
  { "name": "Green", "blend": 1.0, "stops": [ [0, 0, 0, 0], [0.25, 0, 255, 0] ], "plays": 10 }
]
```

## Paso 7: Reiniciar y seleccionar el personaje

Reinicia el juego (o recarga el skin desde los ajustes). El personaje aparece en la lista de personajes de la pantalla de la habitación, donde los personajes bloqueados muestran su condición de desbloqueo. Los stages Lua también pueden leer la lista a través de la global `CHARACTERLIST`, que expone el nombre de carpeta, el nombre de visualización, la rareza y la condición de desbloqueo de cada entrada.

## Resolución de problemas y notas

- El personaje no aparece: comprueba que la carpeta está directamente dentro de `Global/Characters/` y reinicia el juego. El juego construye la lista una vez en el arranque.
- El personaje no dibuja nada: falta `Normal/0.png`, o los nombres de carpeta no coinciden con la tabla del Paso 3. Los fotogramas deben llamarse `0.png`, `1.png`, ... sin huecos; un hueco termina la animación en ese índice sin ningún error.
- El personaje está fuera de pantalla o tiene un tamaño incorrecto: `Chara_Resolution` debe coincidir con la resolución para la que diseñaste los valores de posición. Cuando la clave está ausente el juego asume `1280,720`.
- Solo se reproduce parte del conjunto de animaciones: los estados sin alternativa (Cleared, Failed, Return, los estados de globo y kusudama) necesitan su propia carpeta.
- Un personaje 3D muestra todas las animaciones como no disponibles: `Script.lua` debe definir `availableAnimation` (o `avaialbeAnimation`) y devolver `true` para los tipos cargados.
- Un `Script.lua` presente reemplaza por completo el script integrado. Un personaje con script puede seguir cargando carpetas de PNG numerados, pero solo si el propio script las carga.
- Los guardados referencian el nombre de carpeta, así que renombrar una carpeta que los jugadores ya tienen seleccionada restablece su selección a la ranura vacía.
- Los archivos JSON incluidos contienen comas finales. El analizador JSON del juego las acepta; los validadores estrictos las rechazan.
