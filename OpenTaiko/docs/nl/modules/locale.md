<!-- modules/locale.md -->

# Locale

## Locale.i18n

De eigen vertalingen van een module, die naast de module staan in plaats van in de `Locales/` van de skin. Teksten staan in `lang/<code>/<name>.json`, één bestand per onderdeel, met stabiele id's als sleutels; `lang/en/` bevat het Engels en is de terugval voor elke andere taal. De codes zijn de taal-id's van het spel: `en`, `de`, `es`, `fr`, `ja`, `ko`, `nl`, `ru`, `zh`.

```
Global/Modules/Stages/my_stage/
  Script.lua
  lang/
    en/menu.json      { "title": "Settings", "count": "%d items", "tips": ["One", "Two"] }
    ja/menu.json      { "title": "設定", "count": "%d 件" }
```

| Functie of veld | Beschrijving |
| --- | --- |
| `I18N.texts(name, dir?)  -> texts` | De teksten van `lang/<code>/<name>.json`. `dir` is de map die `lang/` bevat: laat het in een stage of activity weg (of geef `""` door) voor de eigen map, en geef in een Lib-bestand de map van `Core.Here` door. De bestanden worden bij het eerste gebruik gelezen en bewaard. |
| `I18N.detect()` | Leest de huidige taal van het spel en maakt die tot de taal die elke opzoeking gebruikt. Roep het aan in `activate`, dat bij elke binnenkomst draait, zodat een in de instellingen gewijzigde taal bij het volgende bezoek geldt. |
| `I18N.load(code)` | Gebruikt de taal `code` (voor een testopstelling of een taalvoorbeeld). |
| `I18N.currentCode()  -> string` | Het taal-id van de huidige taal van het spel, `"en"` als de engine er geen geeft. |
| `I18N.code` | De taal die de opzoekingen nu gebruiken. |
| `I18N.localizedPath(path, code?)  -> string` | `<name>_<code>.<ext>` als er naast `path` een textuur met die naam bestaat, anders `path`: voor afbeeldingen met ingebakken tekst. |

| Methode | Beschrijving |
| --- | --- |
| `texts:tr(id)  -> string` | De tekst van `id`; het id zelf als geen enkele taal het heeft, waardoor het gat op het scherm zichtbaar wordt. |
| `texts:trf(id, ...)  -> string` | De tekst van `id`, gebruikt als `string.format`-patroon met de argumenten. |
| `texts:get(...)  -> string` | De string op een sleutelpad (`texts:get("menu", "title")` voor geneste objecten), nil als die ontbreekt of leeg is. |
| `texts:list(...)  -> table` | De array van strings op een sleutelpad; elk element valt afzonderlijk terug op Engels. |
| `texts:loc(...)  -> handle` | Een handle waarvan `GetString(fallback)` het sleutelpad leest op het moment van aanroepen, zodat een bij het laden gecachte naam een latere taalwijziging volgt. |
| `texts:node(...)  -> value` | De ruwe JSON-knoop op een sleutelpad (huidige taal, anders Engels). |

```lua
local I18N = require("Locale.i18n")
local T = I18N.texts("menu")
local title, count

function activate()
    I18N.detect()
    title = T:tr("title")
    count = T:trf("count", #items)
end
```

<div class="callout warn">
De opzoekingen volgen `I18N.code`, dat alleen verandert wanneer `detect` of `load` draait. Een module die op het scherm blijft terwijl de speler de taal wijzigt, kan de global `reloadLanguage(lang)` definiëren en daarin `detect` aanroepen. De tekstmodules van Pagoda, Vault en Shop controleren de taal zelf bij elke opzoeking.
</div>

Oudere modules gebruiken in plaats daarvan een woordenboek met exacte Engelse strings: `lang/<code>.lua` geeft een tabel terug die elke Engelse string aan zijn vertaling koppelt, `detect` laadt die voor de huidige taal, en de functies `I18N.tr(s)` en `I18N.trf(fmt, ...)` zoeken strings daarin op. Die vorm werkt nog steeds; nieuwe modules horen `texts` te gebruiken.
