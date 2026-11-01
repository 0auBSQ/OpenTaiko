<!-- modules/core.md -->

# Core

Kleine Helfer ohne eigenen Bildschirm: Easing-Kurven, Farbberechnungen, Tabellenwerkzeuge, ein JSON-Encoder und -Decoder sowie der Ordner der aktuellen Datei. Sie berühren beim Laden keinen Engine-Zustand, sodass jedes Modul, auch ein schreibgeschütztes, sie per require laden kann.

## Core.Easing

Easing-Kurven auf einem Verhältnis von 0 bis 1, für Animationen, die Ihre eigene Uhr antreibt (eine Zeitleiste, das Alter eines Partikels). Für ein von der Engine getaktetes Tweening ist das globale Objekt `COUNTER` mit SetEasing die andere Wahl. Jede Kurve begrenzt ihre Eingabe auf 0..1 und bildet 0 auf 0 und 1 auf 1 ab, außer `hump`.

| Funktion | Beschreibung |
| --- | --- |
| `Easing.linear(t)  -> number` | Das begrenzte Verhältnis selbst. |
| `Easing.inQuad(t)`, `outQuad`, `inOutQuad`, `inCubic`, `outCubic`, `inOutCubic`, `inSine`, `outSine`, `inOutSine`, `inExpo`, `outExpo`  `-> number` | Die üblichen Kurven, nach Namen. |
| `Easing.inBack(t, s?)`, `Easing.outBack(t, s?)  -> number` | Kurven, die überschwingen; `s` ist das Maß des Überschwingens (standardmäßig 1.7). |
| `Easing.inBounce(t)`, `Easing.outBounce(t)`, `Easing.outElastic(t)  -> number` | Springende und elastische Kurven. |
| `Easing.hump(t)  -> number` | 0, in der Mitte hinauf auf 1, zurück auf 0: für Stauchungen und Aufblitzen. |
| `Easing.span(t, t0, dur)  -> number` | Der Fortschritt des Fensters, das bei `t0` beginnt und `dur` dauert, begrenzt auf 0..1. |
| `Easing.lerp(a, b, k)  -> number` | `a + (b - a) * k`. |
| `Easing.clamp01(t)  -> number` | `t`, begrenzt auf 0..1. |
| `Easing.get(kind, fn)  -> function` | Eine Kurve nach den Easing-Namen der Engine: `kind` ist `"IN"`, `"OUT"` oder `"INOUT"`, `fn` ist `"LINEAR"`, `"SINE"`, `"QUAD"`, `"CUBIC"`, `"EXPO"`, `"BACK"`, `"BOUNCE"` oder `"ELASTIC"`. Ein Paar ohne eigene Kurve ergibt die QUAD-Kurve. |

```lua
local Easing = require("Core.Easing")
local k = Easing.outBack(Easing.span(t, startTime, 0.4))
x = Easing.lerp(x0, x1, k)
```

## Core.Color

Farbumrechnungen und -anpassungen. Jeder Kanal ist eine Gleitkommazahl von 0 bis 1, auch der Farbton (0 bis 1 einmal um den Farbkreis); die Funktionen nehmen und liefern getrennte Werte `r, g, b`.

| Funktion | Beschreibung |
| --- | --- |
| `Color.rgbToHsv(r, g, b)  -> h, s, v` | RGB nach HSV. |
| `Color.hsvToRgb(h, s, v)  -> r, g, b` | HSV nach RGB. |
| `Color.rgbToHsl(r, g, b)  -> h, s, l` | RGB nach HSL. |
| `Color.hslToRgb(h, s, l)  -> r, g, b` | HSL nach RGB. |
| `Color.mix(r, g, b, r2, g2, b2, t)  -> r, g, b` | Lineare Mischung: `t` = 0 behält die erste Farbe, 1 ergibt die zweite. |
| `Color.lighten(r, g, b, amount)  -> r, g, b` | Verschiebt die HSL-Helligkeit um `amount` des verbleibenden Bereichs in Richtung Weiß (1 = Weiß); Farbton und Sättigung bleiben. |
| `Color.darken(r, g, b, amount)  -> r, g, b` | Verschiebt die HSL-Helligkeit um `amount` in Richtung Schwarz (1 = Schwarz). |
| `Color.saturate(r, g, b, factor, add?)  -> r, g, b` | Multipliziert die HSV-Sättigung mit `factor`, addiert `add` und begrenzt auf 1. |
| `Color.brighten(r, g, b, factor)  -> r, g, b` | Multipliziert den HSV-Hellwert mit `factor`, begrenzt auf 1. |
| `Color.ramp(from, to, n)  -> table` | `n` Farben in Stufen von `{r, g, b}` bis `{r, g, b}`, als Liste von `{r, g, b}`. |
| `Color.fromEngine(col)  -> r, g, b, a` | Die Kanäle eines `COLOR`-Objekts als Gleitkommazahlen. |
| `Color.toEngine(r, g, b, a?)  -> color` | Ein `COLOR`-Objekt aus Gleitkommazahlen (Alpha standardmäßig 1). |
| `Color.rampEngine(fromCol, toCol, n)  -> table` | Ein Verlauf aus `n` `COLOR`-Objekten zwischen zwei `COLOR`-Objekten. |
| `Color.clamp01(v)  -> number` | `v`, begrenzt auf 0..1. |

## Core.Util

| Funktion | Beschreibung |
| --- | --- |
| `Util.clamp(v, lo, hi)  -> number` | `v`, gehalten zwischen `lo` und `hi`. |
| `Util.lerp(a, b, t)  -> number` | Lineare Interpolation. |
| `Util.round(v)  -> number` | Rundet auf die nächste ganze Zahl (Hälften aufwärts). |
| `Util.deepcopy(o)  -> value` | Eine tiefe Kopie einer Lua-Tabelle, einschließlich Metatabellen und Zyklen. |
| `Util.merge(base, over)  -> table` | Eine Kopie von `base`, in die `over` eingemischt ist: Untertabellen werden rekursiv zusammengeführt, andere Werte ersetzt. |
| `Util.cloneTable(dict)  -> table` | Ein .NET-Dictionary (ein geparstes JSON-Objekt, eine SQL-Zeilenmenge), rekursiv in eine Lua-Tabelle kopiert. |

## Core.Json

JSON-Text aus Lua-Werten und zurück, in reinem Lua. Anders als `JSONLOADER`, das Dateien in .NET-Dictionaries parst, arbeitet es mit Lua-Tabellen und eignet sich daher für Daten, die Sie in einem Spielstand-String (`sf:SetGlobalString`) oder einem Datenbankwert ablegen.

| Funktion | Beschreibung |
| --- | --- |
| `Json.encode(value)  -> string` | Eine Tabelle mit den Schlüsseln 1..n wird als Array geschrieben, jede andere Tabelle als Objekt mit sortierten Schlüsseln, sodass derselbe Wert immer denselben Text ergibt. Funktionen und Userdata werden in Objekten ausgelassen; ganze Zahlen werden ohne Dezimalpunkt geschrieben. Eine Tabelle, die sich selbst enthält, löst einen Fehler aus. |
| `Json.decode(text)  -> value` oder `nil, message` | Parst JSON-Text. JSON-`null` wird zu `Json.null`; bei einem Syntaxfehler gibt es nil und eine Meldung mit der Position zurück. |
| `Json.null` | Der Wert, der für JSON-`null` steht; er wird wieder als `null` kodiert. |

```lua
local Json = require("Core.Json")
local sf = GetSaveFile(0)
local data = Json.decode(sf:GetGlobalString("my_feature")) or { v = 1, items = {} }
data.items[#data.items + 1] = "lamp"
sf:SetGlobalString("my_feature", Json.encode(data))
```

## Core.Here

Eine Funktion, die den Ordner der aufrufenden Datei zurückgibt, ermittelt aus den Argumenten, die `require` an diese Datei übergibt. So findet eine Lib-Datei ihre eigenen Assets (siehe [Assets werden gegen den Aufrufer aufgelöst](README.md)).

| Funktion | Beschreibung |
| --- | --- |
| `require("Core.Here")(...)  -> string` | Der absolute Ordner der Datei, mit Schrägstrichen und abschließendem `/`; nil, wenn die Datei nicht über `require` geladen wurde. Rufen Sie sie auf der obersten Ebene der Datei auf, wo `...` den Modulnamen und den Dateipfad enthält. |
