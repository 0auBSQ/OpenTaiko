<!-- getting-started.md -->

# Hoe modules werken

Met OpenTaiko 0.6.1 kan een skin schermen toevoegen en vervangen met Lua. De map Modules van een skin bevat één map per module, en elke module heeft een Script.lua die een vaste set globale callbackfuncties definieert. Het spel laadt elke Script.lua in een eigen gesandboxte Lua-state, registreert de engine-globals (TEXTURE, SOUND, INPUT, CONFIG en de andere die in de [API-referentie](api/README.md) zijn gedocumenteerd) en roept de callbacks op het juiste moment aan. [Modules en levenscyclus](api/activities.md) somt de exacte signaturen op.

## Voordat je begint

- OpenTaiko 0.6.1 en een skinmap met een map Modules. De meegeleverde skin is System/Open-World Memories.
- Een teksteditor en basiskennis van Lua (functies, tabellen, require).
- De meegeleverde stages onder System/Open-World Memories/Modules/Stages. De kleine, demo1 en demo3, tonen de vorm van de callbacks; de grotere laten zien hoe echte schermen zijn opgebouwd.

## Waar modules staan

Elke soort module heeft een eigen map onder Modules, en elke module is één map waarvan de naam het id van de module is:

```
Modules/
  Stages/        <name>/Script.lua   volledige schermen
  Activities/    <name>/Script.lua   subschermen aangestuurd door een stage
  ROActivities/  <name>/Script.lua   alleen-lezen subschermen en overlays
  Transitions/   <name>/Script.lua   fades tussen stages (als eerste geladen)
  Lib/           gedeelde .lua-bestanden bereikbaar via require; niet als modules gescand
```

Het instapbestand is altijd Script.lua. De assetpaden die je aan TEXTURE, SOUND, VIDEO en de andere loaders doorgeeft, zijn relatief ten opzichte van de modulemap; de meegeleverde modules bewaren ze bij conventie in de submappen Textures, Sounds, Videos en Databases, en zetten vertalingen in een map lang.

Twee soorten scripts staan elders:

- Achtergronden (schermachtergronden, gameplaylagen, mobs, clear-animaties, de kusudama) zijn Script.lua-bestanden onder de map Graphics van de skin, in de map van het scherm dat ze aankleden. Zie de sectie Achtergronden van [Modules en levenscyclus](api/activities.md).
- Personages zijn mappen onder Global/Characters. Een personagemap kan een eigen Script.lua bevatten; zonder die gebruikt het spel zijn ingebouwde personagescript. Zie [Personages toevoegen](guides/characters.md).

## Script.lua definieert globale functies

De Script.lua van een module definieert globale functies op het hoogste niveau met vaste namen, en het spel leest elke functie als een global. Het spel vindt een functie die je in een lokale tabel stopt en teruggeeft nooit, en roept een verkeerd gespelde naam (OnStart in plaats van onStart) nooit aan, omdat het een niet-gedefinieerde callback als no-op behandelt en niets meldt. Al het andere in het bestand kan lokaal zijn, en je kunt de module over meerdere bestanden verdelen die je met require laadt.

demo3 is de minimale vorm om te kopiëren:

```lua
-- Modules/Stages/mystage/Script.lua
local text = nil
local textTex = nil

function onStart()          -- eenmalig wanneer de skin laadt: laad hier assets
    text = TEXT:Create(16)
end

function activate()         -- elke keer dat de stage wordt betreden
    textTex = text:GetText("Hello")
end

function update(timestamp)  -- elk frame: invoer en statuswijzigingen
    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        return Exit("title", nil)
    end
end

function draw()             -- elk frame: alleen tekenen
    if textTex ~= nil then textTex:Draw(200, 200) end
end

function deactivate()       -- wanneer de stage wordt verlaten: stop geluiden, sluit databases
end

function onDestroy()        -- voordat de skin wordt ontladen: geef vrij wat je hebt aangemaakt
    if textTex ~= nil then textTex:Dispose() end
end
```

## De levenscyclus van een stage

- onStart(): het spel roept het eenmalig aan nadat de skin is geladen, en opnieuw na elke herlaadbeurt van de skin, of de stage nu op het scherm staat of niet. Laad hier texturen, geluiden en video's. Het draait als een coroutine, dus een lange laadbeurt kan zich met de LOADING-helper over meerdere frames achter de laadbalk uitspreiden.
- activate(): het spel roept het aan bij elke binnenkomst in de stage. Reset hier de status per bezoek, start muziek en open databases. Het draait eveneens als een coroutine en kan LOADING gebruiken. Het spel vernieuwt de personage- en puchichara-lijsten (CHARACTERLIST, PUCHICHARALIST) vlak voordat activate draait, dus lees ze hier; onStart draait vóór die vernieuwing.
- update(timestamp): het spel roept het elk frame aan vóór draw en geeft de spelklok in milliseconden door. Verwerk hier invoer en wijzig de status. Zodra de stage Exit heeft aangeroepen, stopt het spel met update aanroepen en blijft het draw aanroepen tijdens de fade-out.
- draw(): het spel roept het elk frame aan. Alleen tekenen, en houd allocaties per frame laag.
- deactivate(): het spel roept het aan bij het verlaten van de stage. demo3 geeft hier zijn databases vrij; demo1 stopt zijn muziek en video.
- afterSongEnum(): het spel roept het aan telkens wanneer de nummerenumeratie is afgerond, bij het opstarten en na een zachte of harde herlaadbeurt, ook wanneer de stage niet actief is. Gebruik het wanneer de module van de nummerlijst afhangt.
- onDestroy(): het spel roept het aan voordat het de skin ontlaadt. demo1 geeft hier zijn textuur, video, teksttextuur en geluiden vrij.
- reloadLanguage(lang): het spel roept het aan wanneer de taal verandert (zie de sectie over lokalisatie hieronder).

Wanneer een skin laadt, maakt het spel de modules soort per soort aan: eerst Transitions, dan Stages, Activities en ROActivities. Binnen een soort draait het elke Script.lua voordat het een onStart aanroept. Activities en ROActivities bestaan dus nog niet terwijl de onStart van een stage draait; zoek ze op in activate.

De andere soorten gebruiken varianten van deze set. Activities en ROActivities hebben dezelfde callbacks, maar de hostende stage roept activate, deactivate, draw en update aan en ontvangt hun retourwaarden. Achtergronden ontvangen een state-object in activate(state), update(timestamp, state) en draw(state) en kunnen event-hooks zoals clearIn, playEndAnime en kusuBroke definiëren. Transities definiëren fadeOut(t), loading(progress, elapsed) en fadeIn(t). Personages definiëren een eigen set animatie- en stemfuncties. [Modules en levenscyclus](api/activities.md) somt ze allemaal op.

## Een soort module kiezen

- Stage (Modules/Stages): een volledig scherm waarnaar het spel overschakelt. Hij bezit het frame, verwerkt invoer en vertrekt door Exit aan te roepen. Gebruik het voor alles wat een scherm op zichzelf is.
- Activity (Modules/Activities): een subscherm dat een stage van binnenuit gebruikt, zoals een dialoogvenster. Het is een singleton die je opzoekt met ACTIVITY:GetActivity(name); de hostende stage roept zijn Activate, Update, Draw en Deactivate aan. Gebruik het voor gedeelde onderdelen die spelstatus mogen schrijven.
- ROActivity (Modules/ROActivities): de alleen-lezen vorm van een Activity, die je opzoekt met ROACTIVITY:GetROActivity(name). Hij krijgt een alleen-lezen CONFIG, DATABASE en GetSaveFile en heeft geen ACTIVITY-global. Gebruik het voor onderdelen die alleen status lezen, wat het merendeel van herbruikbare UI dekt. De engine host meerdere eigen overlays als ROActivities met vaste namen (nameplate, modal, modicons, danplate, popup_menu, config_ui, song_enum); een skin vervangt er een door een map met die naam mee te leveren en de callbacks die de engine aanroept te behouden.
- Achtergrond: een Script.lua onder Graphics die achter of boven op een van de engine-schermen tekent. Achtergronden krijgen dezelfde alleen-lezen globals als ROActivities.
- Transitie (Modules/Transitions): de fade-out, het laden en de fade-in die het spel tussen stages afspeelt. Een stage kiest er een op naam in het derde argument van Exit; het spel valt terug op de transitie met de naam default wanneer de stage er geen noemt of de naam niet bestaat, en speelt de transitie met de naam song_loading af om de gameplay te betreden.
- Personage: zie [Personages toevoegen](guides/characters.md).

## Een stage verlaten met Exit

Alleen stages hebben de Exit-global. Hij neemt maximaal drie argumenten en accepteert nil op elke positie: het doel ("title", "play", "stage" of "legacy"; nil betekent "title"), de naam van de bestemmingsstage wanneer het doel "stage" is (of een legacy-sleutel wanneer het "legacy" is), en de naam van een transitiemodule. De meegeleverde stages schrijven `return Exit(...)` binnen update, zodat er verder niets meer in dat frame draait.

```lua
-- uit demo1/Script.lua, binnen update()
if INPUT:KeyboardPressed("S") == true then
    sounds.Skip:Play()
    return Exit("stage", "demo2")   -- spring naar Modules/Stages/demo2
end
-- ...
return Exit("title", nil)           -- terug naar het titelscherm
```

## De sandbox

Elke Script.lua draait in een beperkte Lua-state:

- os behoudt alleen time, date en difftime. De sandbox verwijdert io, debug, loadfile en dofile, en import doet niets.
- package krimpt tot een aangepaste loader: package.path en package.cpath zijn leeg en de sandbox vervangt de standaard searchers, zodat alleen de onderstaande paden doorzoekbaar zijn.
- require kijkt eerst in de eigen map van de module, daarna in de map Modules/Lib van de skin, en laadt het eerste bestand dat het vindt. Een modulebestand en een Lib-bestand met dezelfde naam leiden naar het modulebestand. Punten in de naam worden padscheidingstekens, dus require("DBControllers.dbScores") en require("DBControllers/dbScores") laden beide DBControllers/dbScores.lua. Niet-ASCII-paden werken.

```lua
-- uit intro_nokon/Script.lua
local DBScores  = require("DBControllers/dbScores")  -- de eigen submap van de module
local I18N      = require("i18n")                     -- Modules/Lib/i18n.lua
local Opening   = require("opening")                  -- de modulemap
local Dialogue  = require("nokon_dialogue")           -- de modulemap
```

## Alleen-lezen modules

Het spel maakt ROActivities en achtergronden met beperkte globals aan voordat hun Script.lua draait: CONFIG is een alleen-lezen weergave, GetSaveFile(player) geeft een alleen-lezen opslagbestand terug, DATABASE opent alleen-lezen stores, en ACTIVITY is nil (gebruik ROACTIVITY). Een schrijfactie via een van deze logt een foutmelding, doet niets en werpt geen Lua-fout op. Een module die instellingen, opslagdata of een database moet wijzigen, moet een Activity of een Stage zijn.

## Lokalisatie met lang/

De meegeleverde skin vertaalt de eigen strings van elke module met de gedeelde bibliotheek Modules/Lib/i18n.lua. De Engelse string in de code is de sleutel: de module levert een lang/ja.lua mee die een tabel teruggeeft die elke Engelse string aan zijn Japanse vertaling koppelt, en de bibliotheek zoekt strings op in die tabel.

De bibliotheek heeft drie functies:

- detect() leest de huidige speltaal via de global LANG en laadt het woordenboek daarvoor. Als de taal Japans is, doet het een require van lang/ja, dat binnen de modulemap wordt opgelost, zodat elke module zijn eigen woordenboek heeft; voor elke andere taal laadt het niets. Tot je het aanroept is er geen woordenboek geladen en blijft elke string Engels.
- tr(s) geeft de vertaling van s uit het geladen woordenboek terug, of s zelf wanneer het woordenboek er geen ingang voor heeft of er geen woordenboek is geladen.
- trf(fmt, ...) vertaalt de formatstring fmt op dezelfde manier en formatteert hem daarna met string.format.

Roep detect() aan in activate en bouw daarna je tekst op met tr en trf. activate draait bij elke binnenkomst in de module, dus een taal die de speler in de instellingen heeft gewijzigd wordt bij het volgende bezoek van kracht, en de module heeft geen andere hook nodig. Sleutels moeten exact overeenkomen met de Engelse bron, inclusief leestekens, spaties en regeleinden, en de vertaling moet placeholders zoals %s of {Player 1 name} letterlijk behouden.

```lua
-- Modules/Stages/mystage/lang/ja.lua
local T = {}
T["Nokon"] = "ノコン"
T["Alright, quiz time!"] = "さあ、クイズの時間である！"
return T
```

```lua
-- Modules/Stages/mystage/Script.lua
local I18N = require("i18n")
local title

function activate()
    I18N.detect()
    title = I18N.tr("Alright, quiz time!")
end
```

Het spel roept bij een taalwijziging ook een globale reloadLanguage(lang) aan op elke geladen module. Alleen een module die op het scherm blijft terwijl de taal verandert heeft die nodig, zoals een scherm met een taalkeuze; roep daar detect() opnieuw aan en bouw de vooraf gerenderde tekst opnieuw op.

## Waar je op moet letten

- Het spel geeft de texturen, geluiden, video's en tekstobjecten die een module heeft aangemaakt vrij wanneer het de module vrijgeeft, zodat een herlaadbeurt van de skin ze niet lekt. Geef resources die je per bezoek opent, zoals databases, vrij in deactivate, zoals demo3 doet, en geef in onDestroy vrij wat je hebt aangemaakt, zoals demo1 doet.
- GetText cachet één textuur per afzonderlijke string op zijn tekstobject. Een string die elk frame verandert, voegt elk frame een textuur toe en het spel wordt geleidelijk trager. Teken veranderende waarden met de glyph-renderer (TEXT:CreateGlyphCached), of bewaar één textuur tot de waarde verandert.
- LOADING werkt alleen in callbacks die als coroutine draaien: onStart van elke module, en activate van een stage. LOADING:Tick aanroepen vanuit de activate van een Activity, of vanuit update of draw, werpt een Lua-fout op.
- onStart en afterSongEnum draaien terwijl de module niet op het scherm staat. Schrijf ze zo dat ze werken zonder dat de stage zichtbaar is.
