<!-- modules/vault.md -->

# Vault

La bóveda secreta guarda tres cofres, que se abren con llaves que el jugador gana en otros sitios (clears de la pagoda, la tienda de monedas). Los módulos `Vault` contienen las llaves, los conjuntos de recompensas de los cofres y las claves de guardado; no dibujan nada. Un skin dibuja su propia pantalla de la bóveda encima de ellos, y todos los skins comparten una misma bóveda, porque su estado vive en el archivo de guardado. El stage `secret_vault_rw` de Open-World Memories es uno de esos clientes.

## Los módulos

| Módulo | Qué contiene |
| --- | --- |
| `Vault.Keys` | Las tres llaves: sus cantidades y nombres, y si la bóveda está abierta. |
| `Vault.Chests` | Los tres cofres, sus conjuntos ponderados de recompensas y las ventanas emergentes de recompensa. |
| `Vault.Texts` | Las cadenas propias de los módulos (`lang/<code>/vault.json`). |

## Vault.Keys

Una llave se nombra por su número o por su id: 1 `"simple"`, 2 `"gold"`, 3 `"optk"`. Cada una abre el cofre del mismo número.

| Función | Descripción |
| --- | --- |
| `Keys.count(key)  -> number` | Cuántas llaves de ese tipo tiene el jugador 1. |
| `Keys.set(key, n)`, `Keys.add(key, n?)` | Establece la cantidad; suma `n` (1 por defecto). |
| `Keys.name(key)  -> string` | El nombre de la llave en el idioma del juego. |
| `Keys.vaultOpen()  -> bool` | Si la bóveda está abierta: el trigger `.vault_opened`, que activa la propia vía de acceso del skin a la bóveda. Mientras está cerrada, los demás módulos dan monedas en lugar de llaves. |
| `Keys.index(key)  -> number` | El número de la llave. |

## Vault.Chests

Los cofres son 1 de madera, 2 dorado y 3 de platino. Abrir uno gasta una de sus llaves y sortea una recompensa de su conjunto ponderado: nada (la llave se rompe), monedas, la llave del cofre siguiente, una placa de nombre que el jugador no tiene (ids 361 a 368, 369 a 375 y 376 a 380 según el cofre), un mueble de Mi Habitación (un mueble que ya se posee conserva una quinta parte de su probabilidad), o una canción aún no ganada de la carpeta `Secret Vault` de la banda sonora, repartida entre los cofres según el nivel de su chart más alto.

| Función | Descripción |
| --- | --- |
| `Chests.enter()  -> bool` | Una vez por visita, antes que nada: da la llave simple gratuita de la primera visita (y entonces devuelve true) y construye los conjuntos de canciones. |
| `Chests.roll(i)  -> reward` | Abre el cofre `i` con una de sus llaves (comprueba antes `keyCount`), guarda la recompensa de inmediato y la devuelve. |
| `Chests.showReward(reward)  -> bool` | Inicia la ventana emergente de la recompensa a través de la ROActivity `modal`; false para una rotura o una llave, que no tienen ventana. |
| `Chests.keyCount(i)  -> number` | Las llaves que se tienen para el cofre `i`. |
| `Chests.name(i)  -> string` | El nombre del cofre en el idioma del juego. |
| `Chests.stock(i)  -> left, total` | Lo que queda por ganar en el cofre (canciones, placas de nombre, muebles) y su total. |
| `Chests.chestPool(i)  -> table` | El conjunto ponderado del cofre tal como está en esta partida guardada: entradas `{ w, type, ... }`. |
| `Chests.songPool(i)`, `Chests.maxChartLevel(node)` | Las canciones del cofre; el nivel del chart más alto de una canción. |

Una recompensa es `{ type, openedChest, ... }`, donde `type` es `"snap"`, `"coins"` (`amount`), `"key"` (`target`, el número del cofre), `"nameplate"` (`id`, `info`), `"furniture"` (`id`, `rarity`) o `"song"` (`node`). La ventana emergente funciona como la ROActivity `modal`: llama a su `Update` y su `Draw` mientras `IsActive`, como hacen `Pagoda.Rewards.update` y `draw`. La ventana de un mueble muestra la textura compartida `myroom_thumb_<id>` que proporciona [Mi Habitación](my-room.md).

## Claves de guardado

Todo se guarda en la partida guardada del jugador 1 (`GetSaveFile(0)`):

| Clave | Tipo | Contenido |
| --- | --- | --- |
| `.vault_key_count_simple`, `.vault_key_count_gold`, `.vault_key_count_optk` | contadores | Las llaves que se tienen. |
| `.vault_opened` | trigger | La bóveda está abierta (lo activa el skin). |
| `.vault_first_key_given` | trigger | Se entregó la llave de la primera visita. |
| `.vault_song_unlocked_<unique id>` | trigger | Se ganó esa canción de `Secret Vault`. |
| `.myroom_<id>`, `.myroom_held_<id>` | contadores | Muebles ganados: entregados a Mi Habitación, y en posesión. |

Las placas de nombre y las monedas pasan por los métodos propios del archivo de guardado (`UnlockNameplate`, `EarnCoins`).

```lua
local Chests = require("Vault.Chests")

function activate()
    if Chests.enter() then showFreeKeyNotice = true end
end

local function open(i)
    if Chests.keyCount(i) < 1 then return end
    local reward = Chests.roll(i)
    if not Chests.showReward(reward) then playSnap(reward) end
end
```
