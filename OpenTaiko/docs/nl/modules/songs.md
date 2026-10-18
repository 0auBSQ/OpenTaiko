<!-- modules/songs.md -->

# Songs

## Songs.PlayStrings

Gedeelde strings die een stage instelt voor de ene speelsessie die hij start: de laadachtergrond die getoond moet worden, de stijl van een dan-plaatje, alles wat een achtergrond of een ROActivity van die speelsessie leest. De strings gelden alleen voor de chart waarmee ze zijn gestempeld, zodat waarden die zijn achtergebleven van een speelsessie waarvan de stage nooit terugkwam, geen andere speelsessie bereiken.

| Functie | Beschrijving |
| --- | --- |
| `PlayStrings.stamp()` | Markeert de nu gekozen chart (`SONGMOUNT`) als de chart waar de strings bij horen. Roep het direct na het mounten van de chart aan, vóór `Exit("play")`. |
| `PlayStrings.clear()` | Beëindigt de stempel; roep het aan wanneer de speelsessie is teruggekomen. |
| `PlayStrings.get(key)  -> string` | De gedeelde string `key` zolang de gekozen chart de gestempelde is, anders `""`. |
| `PlayStrings.forTitle(key, title)  -> string` | Dezelfde controle op charttitel, voor een lezer die alleen de titel kent (een dan-plaatje). |

De waarden zelf zijn gewone gedeelde strings, ingesteld met `SHARED:SetSharedString(key, value)`. De stempel staat in de gedeelde string `play_strings_chart`.

```lua
local PlayStrings = require("Songs.PlayStrings")

-- the stage, after mounting the chart
SHARED:SetSharedString("my_loading_art", "night")
PlayStrings.stamp()

-- a background script of that play
local art = PlayStrings.get("my_loading_art")   -- "night"; "" for any other chart
```
