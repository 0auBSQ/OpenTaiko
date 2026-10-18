<!-- modules/ui.md -->

# UI

Eine UI-Bibliothek für Menüs und Dialoge, gesteuert über Maus, Tastatur und Pad mit einer gemeinsamen Hervorhebung. `UI.SimpleUI` ist die minimale Bibliothek, die OpenTaiko mitliefert; ein Skin kann ihr mit `derive` ein eigenes Aussehen geben, wie es Open-World Memories mit PopUI tut. Globale Module beziehen ihre Bibliothek von `UI.Pick`, das die vom Skin gewählte liefert (oder eine, die die Stage nennt), sodass jeder Bildschirm eines Skins gleich aussieht.

## UI.Pick

`require("UI.Pick")` gibt eine Funktion zurück; ihr Aufruf gibt eine UI-Bibliothekstabelle zurück.

| Aufruf | Bibliothek |
| --- | --- |
| `Pick(name)` | Die Bibliothek mit diesem require-Namen (`"PopUI"`, `"UI.SimpleUI"`, ...), für eine Stage, die eine bestimmte will. |
| `Pick()` | Die Bibliothek, die der gemeinsame String `ui_library` nennt, den ein Skin einmal setzt (`_boot` von Open-World Memories setzt `"PopUI"`). |
| `Pick()` ohne `ui_library` | `UI.SimpleUI`. |

Ein Name, der nicht lädt oder etwas ohne Funktion `new` lädt, ergibt ebenfalls `UI.SimpleUI`, sodass ein Name, den ein anderer Skin im gemeinsamen String hinterlassen hat, nichts kostet. Die Wahl besteht nur aus Lua und gemeinsamen Strings: Die Engine weiß nichts davon.

```lua
-- in a skin's _boot onStart: the library every global module will use
SHARED:SetSharedString("ui_library", "MyUI")

-- in any module
local UI = require("UI.Pick")()
local ui = UI.new{ navPlayer = 1 }
```

## UI.SimpleUI

`UI.new(opts)` erzeugt einen Manager, der die Widgets besitzt, die Eingabe an sie verteilt und sie zeichnet. Rufen Sie `ui:update(ts)` und `ui:draw()` in jedem Frame auf.

| Feld von `opts` | Beschreibung |
| --- | --- |
| `theme` | Eine Theme-Tabelle, die über das Standardaussehen der Bibliothek gelegt wird (siehe Themes). |
| `sfx` | Sounds nach Namen, über die Standardwerte gelegt (siehe Sounds). |
| `bg` | true zeichnet zuerst die Farbe `bg` des Themes über den ganzen Bildschirm. |
| `navPlayer` | Der Spieler, dessen Trommel ebenfalls navigiert, in der Form, die `NavInput.getPn` annimmt; nil für alle Spieler. |
| `nav` | `"spatial"`: Die Pfeiltasten springen zum nächstgelegenen Widget in ihrer Richtung (ein Raster). Standardmäßig gehen Hoch und Runter die Widgets in der Reihenfolge ihrer Erstellung durch. |

| Methode | Beschreibung |
| --- | --- |
| `ui:update(ts)  -> "cancel" or nil` | Führt Eingabe und Animationen für einen Frame aus; gibt `"cancel"` zurück, wenn Cancel gedrückt wurde, damit die Stage darauf reagiert. |
| `ui:draw()` | Zeichnet jedes sichtbare Widget in der Reihenfolge seiner Erstellung. |
| `ui:button(o)`, `ui:label(o)`, ...  `-> widget` | Erzeugt ein Widget, fügt es hinzu und gibt es zurück (siehe Widgets). |
| `ui:add(w)`, `ui:remove(w)` | Fügt ein von Hand erzeugtes Widget hinzu oder entfernt es. |
| `ui:clear()` | Entfernt jedes Widget und gibt seine Canvases frei. |
| `ui:disposeWidgets()` | Gibt die Canvases der Widgets frei. Rufen Sie es auf, bevor Sie einen Manager verwerfen: Canvases werden nicht von selbst freigegeben. |
| `ui:focus(w)  -> bool`, `ui:focused()  -> widget` | Verschiebt den Tastaturfokus auf ein Widget; das Widget, das ihn hält. |
| `ui:focusNext()`, `ui:focusPrev()` | Verschiebt den Tastaturfokus um einen Schritt. |
| `ui:setTheme(t)`, `ui:setSfx(t)` | Ersetzt das Theme oder die Sounds des Managers; jedes Widget wird neu gestaltet. |
| `ui:isCapturing()  -> bool` | True, solange ein Textfeld die Tasten entgegennimmt: Überspringen Sie dann Ihre eigenen Tastenkürzel. |
| `ui:mouseActive()  -> bool` | True, solange die Maus das zuletzt verwendete Gerät ist. |
| `ui:drawText(size, str, x, y, color, opacity?, scale?, maxWidth?)` | Zeichnet Text mit der Glyphenschrift der UI, oben links verankert. |
| `ui:drawTextEx(size, str, x, y, fg, bg, opacity, scale, maxWidth, anchor)` | Dasselbe mit einer Umrissfarbe und einem der neun Ankernamen. |
| `ui:measureText(size, str)`, `ui:wrapLines(size, str, maxWidth)`, `ui:ellipsize(size, str, maxWidth)` | Messen, Umbrechen und Kürzen von Text mit derselben Schrift. |
| `ui:textNudge(size)  -> number` | Der Versatz, der eine Textzeile auf einer Mittellinie zentriert. |
| `UI.flushSharedFonts()` | Verwirft die zwischengespeicherten Schriften; rufen Sie es auf, wenn sich die Sprache (und damit die Schrift) ändert. |

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

Jedes Widget nimmt `x`, `y`, `w` und `h` sowie `enabled`, `visible`, `focusable`, `mouseOnly` (reagiert auf Überfahren und Klicken, aber die Tasten überspringen es), `style` oder `theme` (Theme-Felder nur für dieses Widget), `sfx` und `disabledLook = "fade"`. Die Optionen werden zu Feldern des Widgets; ändern Sie sie mit den Methoden (`setEnabled(b)`, `setVisible(b)`, `setText(t)`, ...), die das Widget neu gestalten.

| Fabrik | Optionen und Methoden |
| --- | --- |
| `ui:button(o)` | `text` oder `icon` (`"play"`, `"pause"`, `"stop"`, `"back"`, `"close"`, `"lock"`, `"repeat"`, ...), `accent`, `onClick(b)`. Seine Größe passt sich der Beschriftung an, wenn `w`/`h` fehlen. `setText`, `setIcon`. |
| `ui:label(o)` | `text`, `size` (`"title"`, `"label"`, `"small"` oder eine Zahl), `color`, `align`, `chip` (ein abgerundeter Hintergrund). Nicht fokussierbar. `setText`. |
| `ui:toggle(o)`, `ui:checkbox(o)` | `text`, `value`, `onChange(v)`. `setValue(v, silent)`. |
| `ui:slider(o)` | `min`, `max`, `step`, `value`, `showValue`, `valueW`, `onChange(v)`. Ziehen, oder Links/Rechts bei Fokus. `setValue(v, silent)`. |
| `ui:textbox(o)` | `value`, `placeholder`, `maxLen`, `onChange`, `onSubmit`. Klicken oder Decide, um zu tippen. |
| `ui:panel(o)` | `title`. Ein Container, der hinter den nach ihm erzeugten Widgets gezeichnet wird; `panel:content()` liefert sein inneres Rechteck. Nicht fokussierbar. |
| `ui:bubble(o)` | `text`, `tail` (`"bottom"`, `"top"`, `"left"`, `"right"`), `tailPos`, `typewriter`, `onDone`. `setText`, `setTail`, `setSize`. |
| `ui:menu(o)` | `items` (Strings oder `{ text, value, locked, mark }`), `selected`, `rowHeight`, `onChange(i, item)`, `onSelect(i, item)`, `onLockedSelect`, `drawLocked`, `hoverSelects`, `scrollbar`. Eine scrollende Liste. `setSelected(i)`. |
| `ui:chooser(o)` | `options`, `index`, `wrap`, `onChange(i, option)`. Ein Optionswähler (Links/Rechts). `value()`, `setOptions`, `setIndex`. |
| `ui:settingsList(o)` | `rows` (`{ header = "..." }` oder `{ opt = option }`), `onFocusRow`, `onActivateRow`, `scrollbar`. Die Liste der Optionszeilen des Einstellungsbildschirms. `setRows`. |
| `ui:coins(o)` | Ein Münzzähler (siehe unten). Nicht fokussierbar. |

Die Widget-Klassen sind Felder der Bibliothek (`UI.Button`, `UI.Menu`, ...) zum Ableiten von Unterklassen, zusammen mit `UI.Widget` (der Basisklasse), `UI.Shape` (den abgerundeten Formen, Schatten und Symbolen, die die Widgets vorrendern), `UI.Theme`, `UI.Sfx` und `UI.Cache` (gepoolte Canvases).

## Das Münz-Widget

`ui:coins{ amount, plain, size, iconSize, color, edge }` zeigt einen Betrag neben dem Münzsymbol auf einer Pille (`plain = true`: nur das Symbol und die Zahl, rechtsbündig). Jede Bibliothek zeichnet es auf ihre eigene Weise: Das Symbol von SimpleUI ist die `Coin.png` daneben, PopUI behält die Münzbox des mitgelieferten Skins.

| Methode | Beschreibung |
| --- | --- |
| `c:setAmount(n)` | Zeigt sofort `n`. |
| `c:add(n)` | Lässt die Zahl um `n` weiterlaufen. |
| `c:gain(n)`, `c:pay(n)` | Dasselbe, mit einem `+n` oder `-n` darüber und dem Sound `coin` oder `pay`. |
| `c:amount()  -> number` | Der Betrag, auf den es zuläuft. |
| `c:setPrice(n)`, `c:setBonus(n)` | Ein Preis oder ein Bonus `+n`, darüber angezeigt; nil entfernt ihn. |
| `c:drawCoin(x, y, sx, sy, opacity)` | Zeichnet nur das Münzsymbol, zentriert auf (x, y). |

## Themes

Ein Theme ist eine einzige Tabelle. Der Manager bestimmt das Aussehen jedes Widgets aus dem Standard der Bibliothek, dann dem `theme` des Managers, dann dem `style` des Widgets; Tabellen werden Feld für Feld zusammengeführt, sodass ein Theme nur nennt, was es ändert.

| Feld | Beschreibung |
| --- | --- |
| `colors` | `{r, g, b, a}`-Farben von 0 bis 255: `bg`, `surface`/`surface2` (Fläche, oben und unten), `primary`/`primary2`, `accent`/`accent2`, `outline`, `text`, `textOnAccent`, `textDisabled`, `shadow`, `gloss`, `focusRing`, `track` sowie `gain`/`loss` für die Münzen. |
| `radius`, `radiusSmall` | Eckenradien für große und kleine Elemente. |
| `outlineWidth` | Stärke des Rahmens. |
| `gloss`, `shadow`, `textOutline` | Der Glanz oben (bool), der Schlagschatten (`{ dx, dy, layers, grow }`), umrandete Buchstaben (bool). |
| `font` | Textgrößen: `small`, `label`, `button`, `title`. |
| `anim` | Skalierungen und Zeiten für Hover und Drücken, ihre Easings (`{ "OUT", "QUAD" }`), `typewriterCps`. |

```lua
local ui = UI.new{ theme = { colors = { accent = { 220, 90, 60, 255 } }, radius = 14 } }
ui:button{ text = "Delete", x = 100, y = 100, style = { colors = { primary = { 180, 40, 40, 255 } } } }
```

## Sounds

`sfx`-Tabellen ordnen einem Namen ein Sound-Handle, eine Funktion oder den Namen eines anderen Eintrags zu: `move`, `hover`, `click`, `toggle`, `decide`, `cancel`, `skip`, `error` sowie `coin` und `pay` für das Münz-Widget. Standardmäßig spielen sie die gemeinsamen Sounds `Move`, `Decide`, `Cancel`, `Skip` und `Error`, sodass ein Skin, der diese mit `SHARED:SetSharedSFX` registriert, jedem globalen Bildschirm seine Sounds gibt; ein fehlender bleibt stumm.

## derive: eine Bibliothek in Ihrem Aussehen

`UI.SimpleUI.derive(spec)` gibt eine neue Bibliothek mit den Widgets, dem Manager, dem Fokus und der Eingabe von SimpleUI zurück, aber mit eigenem Aussehen:

| Feld von `spec` | Beschreibung |
| --- | --- |
| `Theme` | Ein Theme-Modul: eine Tabelle, deren `DEFAULT` das vollständige Standard-Theme ist (kopieren Sie `UI.SimpleUI.theme` von SimpleUI und ändern Sie es). |
| `Coins` | Eine optionale Klasse für das Münz-Widget, für einen auf Ihre Weise gezeichneten Münzzähler. |
| `name` | Der Name der Bibliothek. |

```lua
-- System/My Skin/Modules/Lib/MyUI.lua
local SimpleUI = require("UI.SimpleUI")
return SimpleUI.derive{
    name  = "MyUI",
    Theme = require("MyUI.theme"),
}
```

Nennen Sie sie in `ui_library`, und jedes globale Modul baut seine Bildschirme damit. `Modules/Lib/PopUI.lua` von Open-World Memories folgt genau diesem Muster: SimpleUI mit seinem Bubblegum-Theme und seinem eigenen Münz-Widget.
