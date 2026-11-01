<!-- modules/songs.md -->

# Songs

## Songs.PlayStrings

Des chaînes partagées qu'un stage définit pour la partie qu'il lance : l'arrière-plan de chargement à afficher, le style d'une plaque de dan, tout ce que lit un arrière-plan ou une ROActivity de cette partie. Les chaînes ne valent que pour la partition avec laquelle elles ont été estampillées, si bien que des valeurs laissées par une partie dont le stage n'est jamais revenu n'atteignent aucune autre partie.

| Fonction | Description |
| --- | --- |
| `PlayStrings.stamp()` | Marque la partition choisie à cet instant (`SONGMOUNT`) comme celle à laquelle appartiennent les chaînes. Appelez-la juste après avoir monté la partition, avant `Exit("play")`. |
| `PlayStrings.clear()` | Met fin à l'estampille ; appelez-la quand la partie est revenue. |
| `PlayStrings.get(key)  -> string` | La chaîne partagée `key` tant que la partition choisie est celle estampillée, sinon `""`. |
| `PlayStrings.forTitle(key, title)  -> string` | La même vérification par titre de partition, pour un lecteur qui ne connaît que le titre (une plaque de dan). |

Les valeurs elles-mêmes sont des chaînes partagées ordinaires, définies avec `SHARED:SetSharedString(key, value)`. L'estampille est conservée dans la chaîne partagée `play_strings_chart`.

```lua
local PlayStrings = require("Songs.PlayStrings")

-- the stage, after mounting the chart
SHARED:SetSharedString("my_loading_art", "night")
PlayStrings.stamp()

-- a background script of that play
local art = PlayStrings.get("my_loading_art")   -- "night"; "" for any other chart
```
