<!-- modules/ui.md -->

# UI

Een UI-bibliotheek voor menu's en dialogen, bediend met muis, toetsenbord en pad met één gedeelde markering. `UI.SimpleUI` is de minimale bibliotheek die OpenTaiko meelevert; een skin kan haar met `derive` een eigen uiterlijk geven, zoals Open-World Memories met PopUI doet. Globale modules halen hun bibliotheek uit `UI.Pick`, dat de bibliotheek geeft die de skin heeft gekozen (of een die de stage noemt), zodat elk scherm van een skin er hetzelfde uitziet.

## UI.Pick

`require("UI.Pick")` geeft een functie terug; die aanroepen geeft een UI-bibliotheektabel terug.

| Aanroep | Bibliotheek |
| --- | --- |
| `Pick(name)` | De bibliotheek met die require-naam (`"PopUI"`, `"UI.SimpleUI"`, ...), voor een stage die een specifieke wil. |
| `Pick()` | De bibliotheek die de gedeelde string `ui_library` noemt, die een skin één keer instelt (`_boot` van Open-World Memories stelt `"PopUI"` in). |
| `Pick()` zonder `ui_library` | `UI.SimpleUI`. |

Een naam die niet laadt, of die iets laadt zonder functie `new`, geeft ook `UI.SimpleUI`, dus een naam die een andere skin in de gedeelde string heeft achtergelaten, kost niets. De keuze is gewoon Lua en gedeelde strings: de engine weet er niets van.

```lua
-- in a skin's _boot onStart: the library every global module will use
SHARED:SetSharedString("ui_library", "MyUI")

-- in any module
local UI = require("UI.Pick")()
local ui = UI.new{ navPlayer = 1 }
```

## UI.SimpleUI

`UI.new(opts)` maakt een manager aan, die de widgets bezit, de invoer naar ze doorstuurt en ze tekent. Roep `ui:update(ts)` en `ui:draw()` elk frame aan.

| Veld van `opts` | Beschrijving |
| --- | --- |
| `theme` | Een thematabel die over het standaarduiterlijk van de bibliotheek wordt samengevoegd (zie Thema's). |
| `sfx` | Geluiden op naam, samengevoegd over de standaardwaarden (zie Geluiden). |
| `bg` | true tekent eerst de kleur `bg` van het thema over het hele scherm. |
| `navPlayer` | De speler wiens drum ook navigeert, in de vorm die `NavInput.getPn` aanneemt; nil voor alle spelers. |
| `nav` | `"spatial"`: de pijltjestoetsen gaan naar de dichtstbijzijnde widget in hun richting (een raster). Standaard lopen Omhoog en Omlaag de widgets langs in de volgorde waarin ze zijn gemaakt. |

| Methode | Beschrijving |
| --- | --- |
| `ui:update(ts)  -> "cancel" or nil` | Verwerkt de invoer en de animaties voor één frame; geeft `"cancel"` terug wanneer Cancel is ingedrukt, zodat de stage erop kan reageren. |
| `ui:draw()` | Tekent elke zichtbare widget, in de volgorde waarin ze zijn gemaakt. |
| `ui:button(o)`, `ui:label(o)`, ...  `-> widget` | Maakt een widget, voegt hem toe en geeft hem terug (zie Widgets). |
| `ui:add(w)`, `ui:remove(w)` | Voegt een met de hand gemaakte widget toe of verwijdert hem. |
| `ui:clear()` | Verwijdert elke widget en geeft zijn canvassen vrij. |
| `ui:disposeWidgets()` | Geeft de canvassen van de widgets vrij. Roep het aan voordat je een manager laat vallen: canvassen worden niet vanzelf vrijgegeven. |
| `ui:focus(w)  -> bool`, `ui:focused()  -> widget` | Verplaatst de toetsenbordfocus naar een widget; de widget die hem heeft. |
| `ui:focusNext()`, `ui:focusPrev()` | Verplaatst de toetsenbordfocus één stap. |
| `ui:setTheme(t)`, `ui:setSfx(t)` | Vervangt het thema of de geluiden van de manager; elke widget krijgt de nieuwe stijl. |
| `ui:isCapturing()  -> bool` | True zolang een tekstvak de toetsen opvangt: sla dan je eigen sneltoetsen over. |
| `ui:mouseActive()  -> bool` | True zolang de muis het laatst gebruikte apparaat is. |
| `ui:drawText(size, str, x, y, color, opacity?, scale?, maxWidth?)` | Tekent tekst met het glyphlettertype van de UI, linksboven verankerd. |
| `ui:drawTextEx(size, str, x, y, fg, bg, opacity, scale, maxWidth, anchor)` | Hetzelfde met een omlijningskleur en een van de negen ankernamen. |
| `ui:measureText(size, str)`, `ui:wrapLines(size, str, maxWidth)`, `ui:ellipsize(size, str, maxWidth)` | Tekst meten, laten teruglopen en inkorten met hetzelfde lettertype. |
| `ui:textNudge(size)  -> number` | De verschuiving die een tekstregel op een middellijn centreert. |
| `UI.flushSharedFonts()` | Gooit de gecachte lettertypen weg; roep het aan wanneer de taal (en dus het lettertype) verandert. |

```lua
local UI = require("UI.Pick")()
local ui

function activate()
    ui = UI.new{ navPlayer = 1 }
    ui:panel{ x = 560, y = 240, w = 800, h = 600, title = "Options" }
    ui:toggle{ text = "Hints", x = 620, y = 360, value = true, onChange = function(v) hints = v end }
    ui:button{ text = "Back", x = 860, y = 720, onClick = function() leaving = true end }
end

function update(ts)
    if ui:update(ts) == "cancel" or leaving then return Exit("stage", "_title") end
end

function draw() ui:draw() end

function deactivate() ui:disposeWidgets() end
```

## Widgets

Elke widget neemt `x`, `y`, `w` en `h`, plus `enabled`, `visible`, `focusable`, `mouseOnly` (reageert op aanwijzen en klikken, maar de toetsen slaan hem over), `style` of `theme` (themavelden voor alleen deze widget), `sfx` en `disabledLook = "fade"`. De opties worden de velden van de widget; wijzig ze met de methoden (`setEnabled(b)`, `setVisible(b)`, `setText(t)`, ...), die de widget opnieuw opmaken.

| Fabriek | Opties en methoden |
| --- | --- |
| `ui:button(o)` | `text` of `icon` (`"play"`, `"pause"`, `"stop"`, `"back"`, `"close"`, `"lock"`, `"repeat"`, ...), `accent`, `onClick(b)`. Zijn grootte past zich aan het label aan als `w`/`h` ontbreken. `setText`, `setIcon`. |
| `ui:label(o)` | `text`, `size` (`"title"`, `"label"`, `"small"` of een getal), `color`, `align`, `chip` (een afgeronde achtergrond). Niet focusbaar. `setText`. |
| `ui:toggle(o)`, `ui:checkbox(o)` | `text`, `value`, `onChange(v)`. `setValue(v, silent)`. |
| `ui:slider(o)` | `min`, `max`, `step`, `value`, `showValue`, `valueW`, `onChange(v)`. Slepen, of Links/Rechts met focus. `setValue(v, silent)`. |
| `ui:textbox(o)` | `value`, `placeholder`, `maxLen`, `onChange`, `onSubmit`. Klik of Decide om te typen. |
| `ui:panel(o)` | `title`. Een container die achter de widgets wordt getekend die na hem zijn gemaakt; `panel:content()` geeft zijn binnenrechthoek. Niet focusbaar. |
| `ui:bubble(o)` | `text`, `tail` (`"bottom"`, `"top"`, `"left"`, `"right"`), `tailPos`, `typewriter`, `onDone`. `setText`, `setTail`, `setSize`. |
| `ui:menu(o)` | `items` (strings, of `{ text, value, locked, mark }`), `selected`, `rowHeight`, `onChange(i, item)`, `onSelect(i, item)`, `onLockedSelect`, `drawLocked`, `hoverSelects`, `scrollbar`. Een scrollende lijst. `setSelected(i)`. |
| `ui:chooser(o)` | `options`, `index`, `wrap`, `onChange(i, option)`. Een optiekiezer (Links/Rechts). `value()`, `setOptions`, `setIndex`. |
| `ui:settingsList(o)` | `rows` (`{ header = "..." }` of `{ opt = option }`), `onFocusRow`, `onActivateRow`, `scrollbar`. De lijst met optierijen van het instellingenscherm. `setRows`. |
| `ui:coins(o)` | Een muntenteller (zie hieronder). Niet focusbaar. |

De widgetklassen zijn velden van de bibliotheek (`UI.Button`, `UI.Menu`, ...) om subklassen van te maken, samen met `UI.Widget` (de basisklasse), `UI.Shape` (de afgeronde vormen, schaduwen en pictogrammen die de widgets voorbakken), `UI.Theme`, `UI.Sfx` en `UI.Cache` (gepoolde canvassen).

## De munten-widget

`ui:coins{ amount, plain, size, iconSize, color, edge }` toont een bedrag naast het muntpictogram, op een pil (`plain = true`: alleen het pictogram en het getal, rechts uitgelijnd). Elke bibliotheek tekent hem op haar eigen manier: het pictogram van SimpleUI is de `Coin.png` ernaast, PopUI houdt de muntenbox van de meegeleverde skin.

| Methode | Beschrijving |
| --- | --- |
| `c:setAmount(n)` | Toont meteen `n`. |
| `c:add(n)` | Laat het getal met `n` oplopen. |
| `c:gain(n)`, `c:pay(n)` | Hetzelfde, met een `+n` of `-n` erboven en het geluid `coin` of `pay`. |
| `c:amount()  -> number` | Het bedrag waar hij naartoe loopt. |
| `c:setPrice(n)`, `c:setBonus(n)` | Een prijs, of een bonus `+n`, erboven getoond; nil haalt hem weg. |
| `c:drawCoin(x, y, sx, sy, opacity)` | Tekent alleen het muntpictogram, gecentreerd op (x, y). |

## Thema's

Een thema is één tabel. De manager bepaalt het uiterlijk van elke widget uit de standaard van de bibliotheek, dan het `theme` van de manager, dan de `style` van de widget; tabellen worden veld voor veld samengevoegd, dus een thema noemt alleen wat het verandert.

| Veld | Beschrijving |
| --- | --- |
| `colors` | `{r, g, b, a}`-kleuren van 0 tot 255: `bg`, `surface`/`surface2` (het vlak, boven en onder), `primary`/`primary2`, `accent`/`accent2`, `outline`, `text`, `textOnAccent`, `textDisabled`, `shadow`, `gloss`, `focusRing`, `track`, en `gain`/`loss` voor de munten. |
| `radius`, `radiusSmall` | Hoekradii voor grote en kleine elementen. |
| `outlineWidth` | De dikte van de rand. |
| `gloss`, `shadow`, `textOutline` | De glans bovenaan (bool), de slagschaduw (`{ dx, dy, layers, grow }`), omlijnde letters (bool). |
| `font` | Tekstgroottes: `small`, `label`, `button`, `title`. |
| `anim` | Schalen en tijden voor aanwijzen en indrukken, hun easings (`{ "OUT", "QUAD" }`), `typewriterCps`. |

```lua
local ui = UI.new{ theme = { colors = { accent = { 220, 90, 60, 255 } }, radius = 14 } }
ui:button{ text = "Delete", x = 100, y = 100, style = { colors = { primary = { 180, 40, 40, 255 } } } }
```

## Geluiden

`sfx`-tabellen koppelen een naam aan een geluidshandle, een functie of de naam van een ander item: `move`, `hover`, `click`, `toggle`, `decide`, `cancel`, `skip`, `error`, plus `coin` en `pay` voor de munten-widget. Standaard spelen ze de gedeelde geluiden `Move`, `Decide`, `Cancel`, `Skip` en `Error`, dus een skin die die met `SHARED:SetSharedSFX` registreert, geeft elk globaal scherm zijn geluiden; een ontbrekend geluid blijft stil.

## derive: een bibliotheek met jouw uiterlijk

`UI.SimpleUI.derive(spec)` geeft een nieuwe bibliotheek terug met de widgets, de manager, de focus en de invoer van SimpleUI, en een eigen uiterlijk:

| Veld van `spec` | Beschrijving |
| --- | --- |
| `Theme` | Een themamodule: een tabel waarvan `DEFAULT` het volledige standaardthema is (kopieer `UI.SimpleUI.theme` van SimpleUI en pas het aan). |
| `Coins` | Een optionele klasse voor de munten-widget, voor een muntenteller die op jouw manier wordt getekend. |
| `name` | De naam van de bibliotheek. |

```lua
-- System/My Skin/Modules/Lib/MyUI.lua
local SimpleUI = require("UI.SimpleUI")
return SimpleUI.derive{
    name  = "MyUI",
    Theme = require("MyUI.theme"),
}
```

Noem haar in `ui_library` en elke globale module bouwt er zijn schermen mee. `Modules/Lib/PopUI.lua` van Open-World Memories volgt precies dit patroon: SimpleUI met zijn Bubblegum-thema en zijn eigen munten-widget.
