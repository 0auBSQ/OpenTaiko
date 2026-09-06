<!-- api/activities.md -->

# Modules en levenscyclus

De engine roept een vaste set callbacks aan in de Script.lua van een module. Deze pagina somt ze op, samen met de globals die activities, achtergronden en transities aansturen, en de counter-, camera- en diagnosehelpers die elke module ontvangt. Als je nog geen module hebt geschreven, lees dan eerst [Hoe modules werken](../getting-started.md).

## Levenscyclus-callbacks

De engine zoekt globale functies op het hoogste niveau op naam op in de Script.lua van elke module en roept ze op vaste momenten aan. De engine slaat een callback die je niet definieert over. De soort module bepaalt welke callbacks de engine aanroept.

<div class="callout warn">
Wanneer een skin laadt, maakt de engine de modules in deze volgorde aan en start ze: Transitions, Stages, Activities, ROActivities. Binnen elke soort voert de engine eerst de Script.lua van elke module uit (de code op het hoogste niveau), en roept daarna op elk ervan onStart aan. Activities en ROActivities zijn dus nog niet geladen terwijl de onStart van een stage draait, en een opzoeking daar geeft nil terug: zoek ze op in activate. Bij het wisselen van skin of bij het afsluiten draait onDestroy op Stages, daarna op ROActivities en Activities, en daarna op Transitions.
</div>

### Stages, Activities en ROActivities

| Methode | Beschrijving |
| --- | --- |
| `onStart()` | Eenmalig aangeroepen nadat de engine de module heeft aangemaakt, wat gebeurt bij het opstarten en opnieuw telkens wanneer de engine de skin laadt of herlaadt. Draait als een coroutine (zie LOADING hieronder); laad hier assets. |
| `activate(...)` | Stage: aangeroepen elke keer dat de engine de stage betreedt, als een coroutine. Activity/ROActivity: de host roept het aan via `handle:Activate(...)` met zijn eigen argumenten; de retourwaarden gaan terug naar de host. De engine vernieuwt de globals CHARACTERLIST en PUCHICHARALIST vlak voordat het draait. |
| `update(timestamp)` | Elk frame aangeroepen vóór draw; timestamp is de spelklok in milliseconden. Een stage ontvangt geen update meer zodra hij Exit heeft aangeroepen; draw blijft tijdens de fade-out draaien. Activity/ROActivity: de host roept het aan via `handle:Update()`. |
| `draw(...)` | Elk frame aangeroepen. Activity/ROActivity: de host roept het aan via `handle:Draw(...)` met zijn eigen argumenten; de retourwaarden gaan terug naar de host. |
| `deactivate(...)` | Stage: aangeroepen wanneer de engine de stage verlaat. Activity/ROActivity: de host roept het aan via `handle:Deactivate(...)`, of de module roept het op zichzelf aan via `DEACTIVATE()`; de retourwaarden gaan terug naar de host. |
| `afterSongEnum()` | Aangeroepen telkens wanneer de nummerenumeratie is afgerond, ook bij het opstarten en na een zachte of harde herlaadbeurt van de nummers, zelfs wanneer de module niet actief is. |
| `onDestroy()` | Aangeroepen voordat de engine de skin ontlaadt, zodat de module kan vrijgeven wat hij vasthoudt. |
| `reloadLanguage(lang)` | Aangeroepen op elke geladen module wanneer de speltaal verandert; lang is de nieuwe taalcode. |

### Achtergronden

Elk scherm host zijn eigen achtergronden. De sectie Achtergronden hieronder beschrijft het state-argument en de event-hooks.

| Methode | Beschrijving |
| --- | --- |
| `onStart()` | Eenmalig, synchroon aangeroepen, de eerste keer dat de host de achtergrond activeert. |
| `activate(state)` | Aangeroepen elke keer dat de host de achtergrond activeert; een heractivering draait onStart niet opnieuw. |
| `update(timestamp, state)` | Elk frame aangeroepen (niet terwijl de gameplay is gepauzeerd); timestamp is `state.timeStamp` in milliseconden. |
| `draw(state)` | Elk frame aangeroepen. |
| `reloadLanguage(lang)` | Aangeroepen wanneer de speltaal verandert. |

De engine roept afterSongEnum en onDestroy niet op achtergronden aan; wanneer de host een achtergrond vrijgeeft, geeft de engine de resources vrij die de achtergrond heeft aangemaakt.

### Transities

| Methode | Beschrijving |
| --- | --- |
| `onStart()` | Eenmalig aangeroepen wanneer de skin laadt, vóór de stages en activities, als een coroutine. |
| `fadeOut(t)` | Tekent de fade-out over de vertrekkende stage; t loopt van 0 tot 1. |
| `loading(progress, elapsed)` | Tekent het laadscherm; progress loopt van 0 tot 1, elapsed is de tijd in seconden sinds het laden begon. |
| `fadeIn(t)` | Tekent de fade-in over de nieuwe stage; t loopt van 0 tot 1. |
| `onDestroy()` | Aangeroepen voordat de engine de skin ontlaadt, na de stages en activities. |
| `reloadLanguage(lang)` | Aangeroepen wanneer de speltaal verandert. |

De sectie Transities hieronder beschrijft de timing van de fasen.

### Personages

De Script.lua van een personage definieert een andere set: loadAnimation, disposeAnimation, availableAnimation, setAnimationDuration, resetAnimationCounter, update, draw, getDrawSize, getHeyaRenderOffset, getAIBattlePosition, loadVoice, disposeVoice en playVoice. [Personages toevoegen](../guides/characters.md) behandelt die.

### Exit

Een functie die de engine alleen in Stage-scripts registreert; het aanroepen ervan vraagt de engine de stage te verlaten.

<div class="callout warn">
Alleen beschikbaar in Modules/Stages-scripts; hun host stuurt Activities en ROActivities aan, en die krijgen hem niet. Accepteert 0 tot 3 argumenten en verdraagt nil op elke positie. De aanroep zelf vraagt de exit aan; de meegeleverde stages schrijven `return Exit(...)` binnen update, zodat er verder niets meer in dat frame draait. target is "title", "play", "stage" of "legacy"; nil of elke andere waarde betekent "title". Wanneer target "stage" is, is name de Modules/Stages-module waarnaar je springt; wanneer target "legacy" is, is name een van "heya", "config", "exit" of "onlinelounge" (elke andere waarde gaat naar het titelscherm). transition noemt een Modules/Transitions-module; als je die weglaat of de engine hem niet kan vinden, gebruikt de engine de module met de naam "default", en als de skin helemaal geen transities heeft, speelt de engine een gewone zwarte fade-out af.
</div>

| Methode | Beschrijving |
| --- | --- |
| `Exit(target?, name?, transition?)  -> number` | Vraagt de stage-exit richting de gegeven bestemming aan, met optioneel een doelmodule en een transitiemodule; geeft 0 terug. |

```lua
function update(timestamp)
    if INPUT:KeyboardPressed("S") then
        return Exit("stage", "demo2")          -- spring naar Modules/Stages/demo2
    end
    if INPUT:Pressed("Cancel") then
        return Exit("title", nil, "nokon_curtain")   -- terug naar het titelscherm via een benoemde transitie
    end
end
```

### LOADING

Laadbalkhelper voor de callbacks die als coroutine draaien: onStart van elke soort module, en activate van een Stage.

<div class="callout warn">
Gedefinieerd als de LOADING-global in elke module. Deze callbacks draaien op een coroutine die de engine beheert en elk frame hervat: de engine yieldt automatisch zodra een hervatting haar tijdbudget heeft opgebruikt, en je kunt ook zelf yielden met coroutine.yield(progress) of LOADING:Tick(sub). Blokken die je met LOADING:Add registreert, draaien in volgorde nadat de body van de callback is teruggekeerd, en de balk gaat na elk blok vooruit; het gewicht van een blok is zijn aandeel in de balk (standaard 1). LOADING:Tick(sub) yieldt één frame vanuit een blok en rapporteert een fractie van 0 tot 1 binnen dat blok. Buiten een coroutine-callback (de activate van een Activity of ROActivity, of welke update of draw dan ook) werpt LOADING:Tick een Lua-fout op omdat er niets is om naartoe te yielden, en blokken die met LOADING:Add in de wachtrij zijn gezet, draaien nooit.
</div>

| Methode | Beschrijving |
| --- | --- |
| `LOADING:Add(fn)  -> nil` | Registreert een laadblok dat draait nadat de callback is teruggekeerd. |
| `LOADING:Add(label, fn)  -> nil` | Registreert een laadblok met label. |
| `LOADING:Add(label, weight, fn)  -> nil` | Registreert een laadblok met label en een expliciet gewicht. |
| `LOADING:Tick(sub)  -> nil` | Yieldt één frame binnen een blok en rapporteert een subvoortgangsfractie van 0 tot 1 binnen het huidige blok. |

```lua
function onStart()
    LOADING:Add("textures", 3, function()
        for i, name in ipairs(names) do
            tx[name] = TEXTURE:CreateTexture(name)
            LOADING:Tick(i / #names)
        end
    end)
    LOADING:Add("sounds", 1, function()
        bgm = SOUND:CreateBGM("Sounds/BGM.ogg")
    end)
end
```

## Activities

### ACTIVITY

Global om een geladen Activity op naam op te zoeken.

<div class="callout warn">
Geregistreerd als de ACTIVITY-global in elke module behalve ROActivities en achtergronden, waar hij nil is; die modules gebruiken ROACTIVITY. De engine laadt Activities uit Modules/Activities/{name}. ACTIVITY stelt ook GetROActivity beschikbaar, dat zich gedraagt als ROACTIVITY:GetROActivity.
</div>

| Methode | Beschrijving |
| --- | --- |
| `ACTIVITY:GetActivity(name)  -> activity handle` | Geeft de handle van de geladen Activity met de gegeven mapnaam terug, of nil als er geen is geladen. |
| `ACTIVITY:GetROActivity(name)  -> activity handle` | Hetzelfde als ROACTIVITY:GetROActivity. |

### ROACTIVITY

Global om een geladen alleen-lezen Activity (ROActivity) op naam op te zoeken.

<div class="callout warn">
Geregistreerd als de ROACTIVITY-global in elke module. De engine laadt ROActivities uit Modules/ROActivities/{name} en geeft ze de alleen-lezen globals CONFIG, DATABASE en GetSaveFile, zodat hun scripts de spelstatus niet kunnen wijzigen (zie de sectie over alleen-lezen modules van Hoe modules werken). Activities en ROActivities zijn singletons op naam: één instantie per map, die elke host deelt.
</div>

| Methode | Beschrijving |
| --- | --- |
| `ROACTIVITY:GetROActivity(name)  -> activity handle` | Geeft de handle van de geladen ROActivity met de gegeven mapnaam terug, of nil als er geen is geladen. |

### Activity-handle

Het object dat ACTIVITY:GetActivity en ROACTIVITY:GetROActivity teruggeven. Een host gebruikt het om de callbacks van de module aan te sturen.

<div class="callout warn">
Activate, Deactivate en Draw sturen hun argumenten door naar de callbacks activate, deactivate en draw van de module. Update roept update aan met de huidige speltijd in milliseconden. Elk ervan geeft de waarden terug die de callback teruggaf, als een array geïndexeerd vanaf 0, of nil wanneer de callback niets teruggaf of niet is gedefinieerd; lees de eerste waarde met `result[0]`. Call roept elke globale functie aan die het script van de module definieert.
</div>

| Methode | Beschrijving |
| --- | --- |
| `handle.IsActive  -> boolean` | True nadat Activate heeft gedraaid en totdat Deactivate (of de eigen DEACTIVATE() van de module) heeft gedraaid. |
| `handle:Activate(...)  -> array` | Roept de activate-callback van de module aan met de gegeven argumenten. |
| `handle:Deactivate(...)  -> array` | Roept de deactivate-callback van de module aan met de gegeven argumenten. |
| `handle:Update()  -> array` | Roept de update-callback van de module aan met de huidige speltijd in milliseconden. |
| `handle:Draw(...)  -> array` | Roept de draw-callback van de module aan met de gegeven argumenten. |
| `handle:Call(functionName, ...)  -> array` | Roept de benoemde globale functie van het modulescript aan met de gegeven argumenten. |

```lua
local act = nil

function activate()
    if act == nil then act = ACTIVITY:GetActivity("song_select_core") end
    act:Activate()
end

function update(timestamp)
    local result = act:Update()
    local signal = result ~= nil and result[0] or nil
    if signal == "play" then return Exit("play", nil) end
    if signal == "cancel" then return Exit("title", nil) end
end

function draw()
    act:Draw()
end

function deactivate()
    act:Deactivate()
end
```

### DEACTIVATE

Functie die de engine binnen Activity- en ROActivity-scripts registreert; ze laat de module zichzelf deactiveren.

<div class="callout warn">
Het aanroepen markeert de module als inactief (handle.IsActive wordt false) en draait de eigen deactivate-callback van de module. De meegeleverde dialoogvensters roepen hem aan wanneer de speler bevestigt of annuleert, en de host let op IsActive om te weten dat het dialoogvenster is gesloten.
</div>

| Methode | Beschrijving |
| --- | --- |
| `DEACTIVATE(...)` | Deactiveert de huidige module en roept haar deactivate-callback aan met de gegeven argumenten. |

### ROActivities gehost door de engine

De engine zoekt sommige ROActivities op vaste mapnaam op en stuurt ze zelf aan. Een skin vervangt er een door een Modules/ROActivities-map met die naam mee te leveren; de hieronder vermelde aanroeppunten van de engine leggen vast welke callbacks hij moet definiëren. Wanneer er een ontbreekt, tekent de engine de bijbehorende functie niet.

| Naam | Wat de engine aanroept |
| --- | --- |
| `nameplate` | `activate(player, name, title, dan, data)` wanneer het plaatje van een speler verandert; `update()` eenmaal per frame; `draw(mode, ...)` met mode 0 = volledig plaatje `(x, y, opacity, player, side)`, 1 = dan-plaatje `(x, y, opacity, danGrade, textTexture)`, 2 = titelplaatje `(x, y, opacity, type, textTexture, rarity, nameplateId)`. |
| `modal` | `activate(player, rarity, modalType, ...)` voor elke ontgrendelmodal in de wachtrij, daarna `update()` en `draw()` elk frame. Het script roept DEACTIVATE() aan om de modal te sluiten; de engine activeert dan de volgende. |
| `modicons` | `activate()` eenmalig, daarna `draw(x, y, player, layout, alpha)` met layout "menu" of "game". De MODICONS-global omhult dit. |
| `danplate` | `draw(x, y, opacity, danTick, r, g, b, titleText)` op het resultatenscherm en in dan-cursussen. |
| `popup_menu` | `activate(title, items, fontSize, ...)` waarbij items de labels zijn, samengevoegd met regeleinden, gevolgd door de PopupMenu-skinposities; `draw(selected)` elk frame; `deactivate()` bij sluiten. |
| `config_ui` | `activate(model)` met het instellingenmodel; `update()` elk frame, dat "exit" teruggeeft om het instellingenscherm te verlaten; `draw()`; `reload(model)` via Call wanneer de engine het model opnieuw opbouwt; `deactivate()`. |
| `song_enum` | `activate()`, daarna `draw(isCommandSongDataGet, done, total)` elk frame terwijl de nummerscan draait; `deactivate()`. |

## Achtergronden

### Achtergrondmodule

Een Script.lua die één schermachtergrond, gameplaylaag, mob, clear-animatie of kusudama-effect tekent, gehost door de eigen schermen van de engine.

<div class="callout warn">
Achtergronden staan buiten de map Modules, onder de map Graphics van de skin, in de map van het scherm dat ze aankleden, bijvoorbeeld Graphics/0_Startup/Script.lua, Graphics/10_Heya/Script.lua, Graphics/6_Result/Script.lua, Graphics/5_Game/5_Background/Normal/Up/{variant}/Script.lua, Graphics/5_Game/5_Background/Normal/Down/{variant}/Script.lua, Graphics/5_Game/3_Mob/{variant}/Script.lua, Graphics/5_Game/9_End/{result}/Script.lua en Graphics/5_Game/11_Balloon/Kusudama/Script.lua. Waar een map meerdere varianten bevat, kiest de engine er bij elke spelbeurt een willekeurig (of uit de scènepreset van de chart). Het hostscherm maakt een achtergrondinstantie aan (de gameplay-achtergronden elke keer dat de engine het spelscherm betreedt) en geeft haar samen met het scherm vrij, dus tijdens de gameplay zijn er meerdere tegelijk actief. Een achtergrondscript ontvangt dezelfde globals als een ROActivity (alleen-lezen CONFIG, DATABASE en GetSaveFile; geen ACTIVITY). De onderstaande event-hooks zijn optioneel, en de engine roept elk ervan eenmaal aan wanneer zijn gebeurtenis plaatsvindt.
</div>

| Methode | Beschrijving |
| --- | --- |
| `clearIn(player)` | Gameplay-achtergronden Up en Down: de gauge van de speler heeft de clear-zone bereikt. |
| `clearOut(player)` | Gameplay-achtergronden Up en Down: de gauge van de speler is uit de clear-zone gezakt. |
| `playEndAnime(player)` | Clear-animaties (Graphics/5_Game/9_End): de eindanimatie start voor de speler. |
| `kusuIn()` / `kusuBroke()` / `kusuMiss()` | Kusudama: de ballon verschijnt, de speler breekt hem, of de speler mist hem. |
| `skipAnime()` | Resultaatachtergrond: de speler heeft de resultaatanimatie overgeslagen. |

### Achtergrondstatus

Het object dat de host aan activate, update en draw van een achtergrond doorgeeft.

<div class="callout warn">
Eén instantie per host, die de host elk frame ter plekke bijwerkt. De arrayvelden delen de per-speler-arrays van de engine en zijn geïndexeerd vanaf 0 (`state.gauge[0]` is speler 1). Alleen gameplayhosts vernieuwen de gameplayvelden; andere hosts laten ze op hun standaardwaarden, en timeStamp blijft -1 buiten de gameplay. De state bevat geen frametiming: lees de fps-global.
</div>

| Methode | Beschrijving |
| --- | --- |
| `state.playerCount  -> number` | Aantal spelers. |
| `state.p1IsBlue  -> boolean` | True wanneer speler 1 de blauwe kant gebruikt. |
| `state.lang  -> string` | Huidige taalcode. |
| `state.simplemode  -> boolean` | True wanneer Simple Mode aan staat. |
| `state.puchicharaRarities  -> string[]` | Zeldzaamheid van de puchichara van elke speler. |
| `state.characterRarities  -> string[]` | Zeldzaamheid van het personage van elke speler. |
| `state.isClear  -> boolean[]` | Of elke speler zich momenteel in de clear-zone bevindt. |
| `state.gauge  -> number[]` | De gaugewaarde van elke speler. |
| `state.bpm  -> number[]` | De huidige BPM van elke speler. |
| `state.gogo  -> boolean[]` | Of elke speler zich in go-go-time bevindt. |
| `state.towerNightNum  -> number` | Dag-naar-nachtfactor van de toren, van 0 tot 1. |
| `state.battleState  -> number` | Statuscode van het AI-gevecht. |
| `state.battleWin  -> boolean` | True wanneer de speler het AI-gevecht aan het winnen is. |
| `state.timeStamp  -> number` | Met de chart gesynchroniseerde tijd in seconden; -1 buiten de gameplay. |
| `state.paused  -> boolean` | True terwijl de gameplay is gepauzeerd. |
| `state.player  -> number` | De speler waarvoor een per-speler-host (clear-animaties) tekent. |

## Transities

### Transitiemodule

Een Modules/Transitions/{name}/Script.lua die de fasen fade-out, laden en fade-in tussen twee stages tekent.

<div class="callout warn">
Het derde argument van Exit kiest de transitie; de engine gebruikt "default" wanneer de aanroep er geen noemt of de engine de naam niet kan vinden. Het laden naar de gameplay na Exit("play") gebruikt altijd de transitie met de naam "song_loading", of "default" wanneer de skin die niet heeft. De engine doorloopt de fasen in volgorde: hij roept fadeOut(t) elk frame over de vertrekkende stage aan tot t 1 bereikt, ontkoppelt daarna de vertrekkende stage en laadt de nieuwe terwijl hij elk frame loading(progress, elapsed) aanroept, en roept daarna fadeIn(t) over de nieuwe stage aan tot t 1 bereikt. Elke fade duurt 0,5 seconde tenzij het script FADE_OUT_SECONDS of FADE_IN_SECONDS instelt; de engine negeert een waarde die geen positief getal is. Bij een stagewissel roept de engine loading pas aan zodra het laden langer dan 0,5 seconde heeft geduurd; daarvóór roept hij fadeOut(1) aan, zodat korte laadbeurten geen laadscherm laten flitsen. Het pad voor het laden van een nummer toont de laadfase meteen.
</div>

| Methode | Beschrijving |
| --- | --- |
| `FADE_OUT_SECONDS  -> number` | Optionele global op het hoogste niveau: duur van de fade-out in seconden. |
| `FADE_IN_SECONDS  -> number` | Optionele global op het hoogste niveau: duur van de fade-in in seconden. |

```lua
FADE_OUT_SECONDS = 0.3
FADE_IN_SECONDS = 0.3

local pixel = nil

local function cover(alpha)
    pixel:SetColor(0, 0, 0)
    pixel:SetOpacity(alpha)
    pixel:SetScale(8000, 8000)
    pixel:Draw(0, 0)
end

function onStart()
    pixel = TEXTURE:CreateTexture("pixel.png")
end

function fadeOut(t) cover(t) end
function fadeIn(t) cover(1.0 - t) end
function loading(progress, elapsed) cover(1.0) end

function onDestroy()
    if pixel ~= nil then pixel:Dispose() end
end
```

## Timing en camera

### COUNTER

Factory voor animatiecounters die een waarde in de tijd van een begin- naar een eindwaarde bewegen.

<div class="callout warn">
Geregistreerd als de COUNTER-global. Een counter gaat alleen vooruit wanneer je Tick aanroept, met de framedelta gedeeld door interval, waarbij interval het aantal seconden per eenheid van waarde is. Het teken van interval moet bij de richting passen: positief wanneer end groter is dan begin, negatief wanneer het kleiner is. Als de tekens niet overeenkomen, verwisselt de counter de uiteinden en eindigt hij bij zijn eerste tick. CreateCounterDuration neemt de totale duur en kiest het teken zelf. Een interval van nul of gelijke begin- en eindwaarden beëindigt de counter bij zijn eerste tick. De counter roept de optionele functie ended aan wanneer de waarde het einde bereikt, en eenmaal per voltooide cyclus bij herhalen of stuiteren.
</div>

| Methode | Beschrijving |
| --- | --- |
| `COUNTER:CreateCounter(begin, end, interval, ended?)  -> counter` | Maakt een counter die van begin naar end beweegt met interval seconden per eenheid en bij voltooiing ended aanroept. |
| `COUNTER:CreateCounterDuration(begin, end, seconds, ended?)  -> counter` | Maakt een counter die in het gegeven aantal seconden van begin naar end beweegt; geeft een lege counter terug als seconds niet positief is of begin gelijk is aan end. |
| `COUNTER:EmptyCounter()  -> counter` | Maakt een inerte counter waarvan de waarde op 0 blijft, bruikbaar als placeholder. |

### Counter-handle

Een counter gemaakt door COUNTER.

<div class="callout warn">
Lees elk frame Value en roep elk frame Tick aan om hem te laten vorderen; een counter die je niet hebt gestart, of die is gestopt, negeert Tick. Begin, End en Interval zijn leesbare en schrijfbare velden. SetLoop en SetBounce sluiten elkaar uit. SetEasing vormt alleen de gerapporteerde Value; de counter blijft eronder lineair vorderen. Listeners ontvangen de huidige waarde bij elke tick, inclusief de laatste.
</div>

| Methode | Beschrijving |
| --- | --- |
| `counter.Value  -> number` | De huidige waarde, met easing toegepast als die is ingesteld; een toewijzing laat de counter springen. |
| `counter.Begin  -> number` | De beginwaarde (leesbaar en schrijfbaar). |
| `counter.End  -> number` | De eindwaarde (leesbaar en schrijfbaar). |
| `counter.Interval  -> number` | Seconden per eenheid van waarde (leesbaar en schrijfbaar). |
| `counter:Start()  -> nil` | Zet de waarde terug op Begin en begint te ticken. |
| `counter:Resume()  -> nil` | Begint te ticken zonder de waarde te resetten. |
| `counter:Stop()  -> nil` | Stopt met ticken. |
| `counter:Pause()  -> nil` | Hetzelfde als Stop. |
| `counter:Reset()  -> nil` | Zet de waarde terug op Begin zonder te veranderen of hij tickt. |
| `counter:Tick()  -> nil` | Laat de waarde één frame vorderen en roept waar van toepassing listeners en de functie ended aan. |
| `counter:SetLoop(loop)  -> nil` | Springt terug naar Begin wanneer de waarde het einde bereikt; schakelt stuiteren uit. |
| `counter:SetBounce(bounce)  -> nil` | Keert de richting om wanneer de waarde een van beide uiteinden bereikt; schakelt herhalen uit. |
| `counter:GetLoop()  -> boolean` | Of herhalen aan staat. |
| `counter:GetBounce()  -> boolean` | Of stuiteren aan staat. |
| `counter:SetEasing(type, function)  -> nil` | Past een easingcurve toe op de gerapporteerde waarde; type is IN, OUT, INOUT of OUTIN en function is LINEAR, SINE, QUAD, CUBIC, QUART, QUINT, EXPO, CIRC, ELASTIC, BACK of BOUNCE (niet hoofdlettergevoelig; de counter negeert een onbekende naam). |
| `counter:ClearEasing()  -> nil` | Verwijdert de easing. |
| `counter:Listen(listener)  -> nil` | Registreert een functie die bij elke tick met de huidige waarde wordt aangeroepen. |
| `counter:ClearListeners()  -> nil` | Verwijdert alle listeners. |

```lua
local fade = nil

function activate()
    fade = COUNTER:CreateCounterDuration(0, 1, 0.5, function() debugLog("fade done") end)
    fade:SetEasing("OUT", "QUAD")
    fade:Start()
end

function update(timestamp)
    fade:Tick()
end

function draw()
    background:SetOpacity(fade.Value)
    background:Draw(0, 0)
end
```

### GLOBALCAMERA

De 2D-camera voor het hele scherm: hij pant, zoomt en roteert het volledige gerenderde frame en voegt een uitdovend schermschudden toe.

<div class="callout warn">
Geregistreerd als de GLOBALCAMERA-global. Hij stuurt dezelfde schermtransformatie aan als de TJA-commando's #CAMERA en beïnvloedt alles wat wordt getekend, inclusief geblitte 3D-scènes. Offsets zijn in referentiepixels van 1280x720, rotatie is in graden en een zoom van 1 betekent geen schaling. De basistransformatie blijft behouden tot je haar wijzigt: roep elk frame Update(dt) aan om haar toe te passen en het schudden te laten vorderen, en Reset() wanneer je de stage verlaat, zodat ze niet naar de volgende stage wordt meegenomen. De eigen camera van een 3D-scène stel je in op het scèneobject.
</div>

| Methode | Beschrijving |
| --- | --- |
| `GLOBALCAMERA:SetOffset(x, y)  -> nil` | Pant het scherm met (x, y) pixels. |
| `GLOBALCAMERA:SetZoom(sx, sy)  -> nil` | Zoomt het scherm met afzonderlijke X- en Y-factoren. |
| `GLOBALCAMERA:SetUniformZoom(s)  -> nil` | Zoomt het scherm uniform. |
| `GLOBALCAMERA:SetRotation(deg)  -> nil` | Roteert het scherm om zijn middelpunt. |
| `GLOBALCAMERA:GetOffsetX()  -> number` | De X-basisoffset. |
| `GLOBALCAMERA:GetOffsetY()  -> number` | De Y-basisoffset. |
| `GLOBALCAMERA:GetZoomX()  -> number` | De X-zoomfactor. |
| `GLOBALCAMERA:GetZoomY()  -> number` | De Y-zoomfactor. |
| `GLOBALCAMERA:GetRotation()  -> number` | De basisrotatie in graden. |
| `GLOBALCAMERA:Shake(amplitudePx, seconds, rotAmpDeg?)  -> nil` | Start een schudbeweging die lineair uitdooft vanaf de gegeven pixelamplitude over het gegeven aantal seconden, met een optionele rotatiewiebel in graden. De camera negeert een aanroep met seconds van 0 of minder; een nieuwe schudbeweging vervangt de huidige alleen wanneer haar amplitude gelijk of groter is. |
| `GLOBALCAMERA.IsShaking  -> boolean` | True terwijl een schudbeweging nog uitdooft. |
| `GLOBALCAMERA:Update(dt)  -> nil` | Laat het schudden dt seconden vorderen (begrensd op 0,25) en past de basistransformatie plus het schudden op het scherm toe. |
| `GLOBALCAMERA:Reset()  -> nil` | Centreert de camera opnieuw, reset zoom en rotatie en stopt elke schudbeweging. |

```lua
function update(timestamp)
    if INPUT:Pressed("LRed") or INPUT:Pressed("RRed") then GLOBALCAMERA:Shake(18, 0.35) end
    GLOBALCAMERA:Update(fps.deltaTime)
end

function deactivate()
    GLOBALCAMERA:Reset()
end
```

## Nummerenumeratie

Twee globale functies rapporteren de status van de nummerscan. Gebruik ze samen met de callback afterSongEnum.

| Methode | Beschrijving |
| --- | --- |
| `IsSongsEnumerating()  -> boolean` | True terwijl een nummerenumeratie bezig is. |
| `IsSongsEnumDone()  -> boolean` | True zodra de nummerscan is afgerond. Het is false voordat de scan start en terwijl hij draait; IsSongsEnumerating is false in zowel de niet-gestarte als de afgeronde toestand, dus controleer deze functie om te weten dat de lijst klaar is. |

## Diagnose

### info

Alleen-lezen object met basale spelstatus en de eigen map van de module.

<div class="callout warn">
Geregistreerd als de info-global en per module aangemaakt. Elk veld berekent zijn waarde bij toegang; online bevraagt het besturingssysteem telkens wanneer je het leest.
</div>

| Methode | Beschrijving |
| --- | --- |
| `info.playerCount  -> number` | Het ingestelde aantal spelers. |
| `info.lang  -> string` | De huidige taalcode. |
| `info.simplemode  -> boolean` | True wanneer Simple Mode aan staat. |
| `info.p1IsBlue  -> boolean` | True wanneer speler 1 de blauwe kant gebruikt. |
| `info.online  -> boolean` | True wanneer een bruikbare netwerkinterface beschikbaar is. |
| `info.dir  -> string` | De map van deze module. |

### fps

Alleen-lezen object met de frametiming en een klok met hoge resolutie.

| Methode | Beschrijving |
| --- | --- |
| `fps.deltaTime  -> number` | Seconden verstreken sinds het vorige frame. |
| `fps.fps  -> number` | De huidige gemeten frames per seconde. |
| `fps.ms  -> number` | Een monotone klok in milliseconden, om delen van Lua te timen door verschillen te nemen. |

### debugLog

| Methode | Beschrijving |
| --- | --- |
| `debugLog(message)  -> nil` | Schrijft de string naar het tracelog van de engine met een voorvoegsel dat hem als Lua-log markeert. |

## Andere globals

De engine registreert deze globals in elke module; hun eigen pagina's documenteren ze.

| Global | Pagina |
| --- | --- |
| `GetSaveFile(player)` | [Spelers en profielen](players.md). Geeft binnen ROActivities en achtergronden een alleen-lezen handle terug. |
| `RequestSongList(settings)`, `GenerateSongListSettings()` | [Nummers en charts](songs.md). |
| `MODICONS` | [Nummers en charts](songs.md). |
| `CONFIG`, `DATABASE`, `SHARED`, `STORAGE`, `JSONLOADER`, `INILOADER`, `SQL` | [Data en persistentie](data.md). |
| `TEXTURE`, `CANVAS`, `GRAPHICS`, `TEXT`, `VIDEO`, `COLOR`, `GRADIENT`, `SIZE` | [Graphics en tekst](graphics.md). |
| `SOUND`, `HITSOUNDSLIST` | [Audio](audio.md). |
| `INPUT` | [Invoer](input.md). |
| `NAMEPLATE`, `NAMEPLATESLIST`, `CHARACTER`, `CHARACTERLIST`, `PUCHICHARALIST`, `PLAYSTATE`, `THEME`, `LANG` | [Spelers en profielen](players.md). |
| `VECTOR`, `VECTOR2`, `VECTOR3`, `VECTOR4`, `MATRIX`, `MATRIX2`, `MATRIX3`, `MATRIX4`, `QUATERNION` | [Wiskunde](math.md). |
| `SONGMOUNT`, `REPLAY`, `DANBUILDER`, `VIRTUALSLOTS` | [Nummers en charts](songs.md). |
| `NET` | [Online netwerken](networking.md). |
| `SCENE3D`, `MODEL`, `PHYSICS`, `COLLIDERS`, `PATHFIND`, `HEIGHTMAP` | [3D-engine: rasterizerwereld](3d.md), [3D-engine: raytracerwereld](3d-raytrace.md), [3D-engine: fysica](3d-physics.md) <span class="badge-exp">Experimenteel</span>. |
