<!-- modules/core.md -->

# Core

Kleine helpers zonder eigen scherm: easingcurves, kleurberekeningen, tabelhulpmiddelen, een JSON-encoder en -decoder, en de map van het huidige bestand. Ze raken bij het laden geen enginestatus aan, dus elke module, ook een alleen-lezen module, kan ze laden.

## Core.Easing

Easingcurves op een verhouding van 0 tot 1, voor animaties die je eigen klok aanstuurt (een tijdlijn, de leeftijd van een deeltje). Voor een tween op de klok van de engine is de global `COUNTER` met SetEasing de andere keuze. Elke curve begrenst zijn invoer tot 0..1 en beeldt 0 af op 0 en 1 op 1, behalve `hump`.

| Functie | Beschrijving |
| --- | --- |
| `Easing.linear(t)  -> number` | De begrensde verhouding zelf. |
| `Easing.inQuad(t)`, `outQuad`, `inOutQuad`, `inCubic`, `outCubic`, `inOutCubic`, `inSine`, `outSine`, `inOutSine`, `inExpo`, `outExpo`  `-> number` | De gebruikelijke curves, op naam. |
| `Easing.inBack(t, s?)`, `Easing.outBack(t, s?)  -> number` | Curves die doorschieten; `s` is de mate van doorschieten (standaard 1.7). |
| `Easing.inBounce(t)`, `Easing.outBounce(t)`, `Easing.outElastic(t)  -> number` | Stuiterende en elastische curves. |
| `Easing.hump(t)  -> number` | 0, in het midden omhoog naar 1, terug naar 0: voor indrukeffecten en flitsen. |
| `Easing.span(t, t0, dur)  -> number` | De voortgang van het venster dat bij `t0` begint en `dur` duurt, begrensd tot 0..1. |
| `Easing.lerp(a, b, k)  -> number` | `a + (b - a) * k`. |
| `Easing.clamp01(t)  -> number` | `t`, begrensd tot 0..1. |
| `Easing.get(kind, fn)  -> function` | Een curve via de easingnamen van de engine: `kind` is `"IN"`, `"OUT"` of `"INOUT"`, `fn` is `"LINEAR"`, `"SINE"`, `"QUAD"`, `"CUBIC"`, `"EXPO"`, `"BACK"`, `"BOUNCE"` of `"ELASTIC"`. Een paar zonder curve geeft de QUAD-curve. |

```lua
local Easing = require("Core.Easing")
local k = Easing.outBack(Easing.span(t, startTime, 0.4))
x = Easing.lerp(x0, x1, k)
```

## Core.Color

Kleurconversies en -aanpassingen. Elk kanaal is een float van 0 tot 1, ook de tint (0 tot 1 eenmaal rond het kleurenwiel); functies nemen en geven aparte waarden `r, g, b`.

| Functie | Beschrijving |
| --- | --- |
| `Color.rgbToHsv(r, g, b)  -> h, s, v` | RGB naar HSV. |
| `Color.hsvToRgb(h, s, v)  -> r, g, b` | HSV naar RGB. |
| `Color.rgbToHsl(r, g, b)  -> h, s, l` | RGB naar HSL. |
| `Color.hslToRgb(h, s, l)  -> r, g, b` | HSL naar RGB. |
| `Color.mix(r, g, b, r2, g2, b2, t)  -> r, g, b` | Lineaire menging: `t` = 0 houdt de eerste kleur, 1 geeft de tweede. |
| `Color.lighten(r, g, b, amount)  -> r, g, b` | Verschuift de HSL-lichtheid met `amount` van het resterende bereik richting wit (1 = wit); tint en verzadiging blijven. |
| `Color.darken(r, g, b, amount)  -> r, g, b` | Verschuift de HSL-lichtheid met `amount` richting zwart (1 = zwart). |
| `Color.saturate(r, g, b, factor, add?)  -> r, g, b` | Vermenigvuldigt de HSV-verzadiging met `factor`, telt `add` op en begrenst tot 1. |
| `Color.brighten(r, g, b, factor)  -> r, g, b` | Vermenigvuldigt de HSV-waarde met `factor`, begrensd tot 1. |
| `Color.ramp(from, to, n)  -> table` | `n` kleuren in stappen van `{r, g, b}` naar `{r, g, b}`, als lijst van `{r, g, b}`. |
| `Color.fromEngine(col)  -> r, g, b, a` | De kanalen van een `COLOR`-object, als floats. |
| `Color.toEngine(r, g, b, a?)  -> color` | Een `COLOR`-object uit floats (alfa standaard 1). |
| `Color.rampEngine(fromCol, toCol, n)  -> table` | Een verloop van `n` `COLOR`-objecten tussen twee `COLOR`-objecten. |
| `Color.clamp01(v)  -> number` | `v`, begrensd tot 0..1. |

## Core.Util

| Functie | Beschrijving |
| --- | --- |
| `Util.clamp(v, lo, hi)  -> number` | `v`, gehouden tussen `lo` en `hi`. |
| `Util.lerp(a, b, t)  -> number` | Lineaire interpolatie. |
| `Util.round(v)  -> number` | Rondt af op het dichtstbijzijnde gehele getal (helften naar boven). |
| `Util.deepcopy(o)  -> value` | Een diepe kopie van een Lua-tabel, inclusief metatabellen en cycli. |
| `Util.merge(base, over)  -> table` | Een kopie van `base` waarin `over` is samengevoegd: subtabellen worden recursief samengevoegd, andere waarden vervangen. |
| `Util.cloneTable(dict)  -> table` | Een .NET-dictionary (een geparseerd JSON-object, een verzameling SQL-rijen) recursief gekopieerd naar een Lua-tabel. |

## Core.Json

JSON-tekst uit Lua-waarden en terug, in puur Lua. Anders dan `JSONLOADER`, dat bestanden naar .NET-dictionaries parseert, werkt het op Lua-tabellen, dus het past bij data die je in een opslagstring (`sf:SetGlobalString`) of een databasewaarde bewaart.

| Functie | Beschrijving |
| --- | --- |
| `Json.encode(value)  -> string` | Een tabel met de sleutels 1..n wordt als array geschreven, elke andere tabel als object met gesorteerde sleutels, zodat dezelfde waarde altijd dezelfde tekst geeft. Functies en userdata worden uit objecten weggelaten; gehele getallen worden zonder decimaalteken geschreven. Een tabel die zichzelf bevat, werpt een fout op. |
| `Json.decode(text)  -> value` of `nil, message` | Parseert JSON-tekst. JSON-`null` wordt `Json.null`; bij een syntaxfout geeft het nil en een melding met de positie terug. |
| `Json.null` | De waarde die voor JSON-`null` staat; die wordt weer als `null` gecodeerd. |

```lua
local Json = require("Core.Json")
local sf = GetSaveFile(0)
local data = Json.decode(sf:GetGlobalString("my_feature")) or { v = 1, items = {} }
data.items[#data.items + 1] = "lamp"
sf:SetGlobalString("my_feature", Json.encode(data))
```

## Core.Here

Een functie die de map teruggeeft van het bestand dat haar aanroept, op basis van de argumenten die `require` aan dat bestand meegeeft. Zo vindt een Lib-bestand zijn eigen assets (zie [Assets worden opgelost ten opzichte van de aanroeper](README.md)).

| Functie | Beschrijving |
| --- | --- |
| `require("Core.Here")(...)  -> string` | De absolute map van het bestand, met slashes en een afsluitende `/`; nil wanneer het bestand niet via `require` is geladen. Roep haar aan op het hoogste niveau van het bestand, waar `...` de modulenaam en het bestandspad bevat. |
