<!-- guides/characters.md -->

# Een personage toevoegen

Een personage is een map onder `Global/Characters/` in de installatiemap van het spel. Elke submap die het spel daar vindt, wordt één selecteerbaar personage. Een map bevat een `Metadata.json` (naam, zeldzaamheid, auteur), een `CharaConfig.txt` (posities en animatietiming), de animatie-inhoud, en optioneel `Effects.json`, `Unlock.json`, `Palettes.json` en stemfragmenten. De animatie-inhoud bestaat ofwel uit mappen met genummerde PNG-frames, die het ingebouwde personagescript van het spel rendert, ofwel uit wat een `Script.lua` per personage zelf kiest te tekenen (het meegeleverde 3D-sjabloon tekent een glTF-model).

Compatibiliteit: OpenTaiko 0.6.1 laadt personages die voor 0.6.0 zijn gemaakt nog steeds zonder wijzigingen. Deze pagina beschrijft de huidige opbouw; gebruik die voor nieuwe personages.

## Voordat je begint

- OpenTaiko 0.6.1 geïnstalleerd. Het spel leest personages uit `Global/Characters/` naast het uitvoerbare bestand van het spel, en alle skins delen ze.
- Een teksteditor voor JSON- en INI-achtige bestanden.
- Voor een 2D-personage: de tekeningen geëxporteerd als genummerde PNG-frames (`0.png`, `1.png`, ...) met een transparante achtergrond, één map per animatiestatus.
- Voor een 3D-personage: een `model.glb` (binaire glTF) met de animatieclips, en een stilstaande `Render.png`.
- De meegeleverde mappen `01 - Template` (2D) en `01 - Template3D`. Kopieer er een als uitgangspunt.

## Stap 1: Begrijp detectie, volgorde en identiteit

Bij het opstarten somt het spel de submappen van `Global/Characters/` op en maakt het één personage per map aan, in de volgorde waarin het bestandssysteem ze teruggeeft. Het spel sorteert de lijst niet, dus de meegeleverde mappen dragen een numeriek voorvoegsel (`00 - None`, `01 - Template`, `02 - Student (A)`, ...) om de volgorde voorspelbaar te houden. Houd `00 - None` als eerste: index 0 is het lege slot en de terugval wanneer een opgeslagen personage ontbreekt.

Opslagbestanden slaan het gekozen personage op via de mapnaam (`characterName`) en lossen die bij elke opstart opnieuw op naar een index. Andere mappen toevoegen of verwijderen breekt nooit een opgeslagen selectie, maar een map hernoemen laat opslagbestanden die ernaar verwezen terugvallen op `00 - None`. Twee personages mogen dezelfde weergavenaam delen; de mapnaam moet uniek zijn.

Het spel enumereert personages eenmaal bij het opstarten en opnieuw wanneer de skin herlaadt; een map die je toevoegt terwijl het spel draait, verschijnt na de volgende opstart of herlaadbeurt van de skin.

## Stap 2: Maak de map en Metadata.json

Maak een map zoals `30 - MyChara` en voeg `Metadata.json` toe:

- `name`: weergavenaam. Ofwel een gewone string, ofwel een gelokaliseerd object `{ "strings": { "default": "...", "ja": "...", ... } }`. `default` is de terugval; de andere sleutels zijn speltaalcodes.
- `rarity`: een van `Poor`, `Common`, `Uncommon`, `Rare`, `Epic`, `Legendary`, `Mythical`. Zeldzaamheid bepaalt alleen de kleur en het niveau van de ontgrendelmelding; elke zeldzaamheid heeft een muntvermenigvuldiger van 1.
- `author`: gewone string of gelokaliseerd object.
- `description`: optioneel, gewone string of gelokaliseerd object.
- `speechtext`: optionele array van zes gelokaliseerde objecten die het resultatenscherm in de tekstballon van het personage toont. Het spel kiest het item per resultaat, in deze volgorde: mislukt met een lage gauge, mislukt met de gauge op 40% of hoger, gecleard, gecleard met een volle gauge, full combo, all perfect. Als je er minder dan zes opgeeft, herhaalt het spel de laatste.

Als `Metadata.json` ontbreekt, laadt het personage nog steeds, met naam `(None)`, zeldzaamheid `Common` en auteur `(None)`.

```json
{
  "name": {
    "strings": {
      "default": "My Character",
      "ja": "マイキャラ"
    }
  },
  "rarity": "Common",
  "author": {
    "strings": {
      "default": "Your Name"
    }
  }
}
```

## Stap 3 (2D-pad): Voeg de framemappen toe

Wanneer de map geen `Script.lua` heeft, rendert het spel het personage met zijn ingebouwde script (`CharaScript.lua` in de installatiemap van het spel). Dat script koppelt elke animatiestatus aan een submap en laadt daaruit `0.png`, `1.png`, `2.png`, ... Het laden stopt bij de eerste ontbrekende index, dus de nummering moet aaneengesloten zijn.

| Animatiestatus | Map |
|---|---|
| Game/Normal, Game/Clear, Game/Max | `Normal`, `Clear`, `Clear_Max` |
| Game/Gogo, Game/Gogo_Max | `GoGo`, `GoGo_Max` |
| Game/Miss, Game/Miss_Down | `Miss`, `MissDown` |
| Game/10combo, Game/10combo_Max | `10combo`, `10combo_Max` |
| Game/Cleared, Game/Failed | `Cleared`, `Failed` |
| Game/Clear_In, Game/Clear_Out | `Clearin`, `ClearOut` |
| Game/Max_In, Game/Max_Out | `Soulin`, `SoulOut` |
| Game/Miss_In, Game/Miss_Down_In, Game/Return | `MissIn`, `MissDownIn`, `Return` |
| Game/GoGoStart, Game/GoGoStart_Clear, Game/GoGoStart_Max | `GoGoStart`, `GoGoStart_Clear`, `GoGoStart_Max` |
| Game/Balloon_Breaking, Game/Balloon_Broke, Game/Balloon_Miss | `Balloon_Breaking`, `Balloon_Broke`, `Balloon_Miss` |
| Game/Kusudama_Breaking, Game/Kusudama_Broke, Game/Kusudama_Miss, Game/Kusudama_Idle | `Kusudama_Breaking`, `Kusudama_Broke`, `Kusudama_Miss`, `Kusudama_Idle` |
| Game/Tower/Standing, Climbing, Running, Clear, Fail (en de `_Tired`-varianten) | `Tower_Char/Standing`, `Tower_Char/Climbing`, `Tower_Char/Running`, `Tower_Char/Clear`, `Tower_Char/Fail` (plus `Tower_Char/Standing_Tired` enzovoort) |
| Menu/Wait, Menu/Start, Menu/Normal, Menu/Select | `Menu_Wait`, `Menu_Start`, `Menu_Loop`, `Menu_Select` |
| Entry/Normal, Entry/Jump | `Title_Normal`, `Title_Entry` |
| Result/Normal, Result/Clear, Result/Failed_In, Result/Failed | `Result_Normal`, `Result_Clear`, `Result_Failed_In`, `Result_Failed` |

Het ingebouwde script leest twee stilstaande afbeeldingen uit de hoofdmap: `Render.png` (het portret op volledige grootte, getekend waar het spel om het animatietype Render vraagt, bijvoorbeeld in de kamer) en `Preview.png` (de miniatuur; indien afwezig gebruikt het script `Normal/0.png`).

Ontbrekende statussen vallen terug op een andere status, zodat een personage een deelverzameling kan meeleveren. De terugvalketen is: Clear -> Normal, Max -> Clear, Miss -> Normal, Miss_Down -> Miss, Gogo -> Normal, Gogo_Max -> Gogo, 10combo_Max -> 10combo, GoGoStart_Clear -> GoGoStart, GoGoStart_Max -> GoGoStart_Clear, de Tower-`_Tired`-statussen -> hun normale status, Tower/Fail -> Tower/Standing_Tired, Kusudama_Idle -> Normal, Menu/Wait -> Gogo, Menu/Start en Menu/Select en Entry/Jump -> 10combo, Menu/Normal en Entry/Normal en Result/Normal -> Normal, Result/Clear -> Clear, Result/Failed_In -> Miss_In, Result/Failed -> Miss. Statussen zonder terugval (bijvoorbeeld Cleared, Failed, Return, de ballonstatussen) tekenen niets wanneer ze ontbreken. Het minimum voor een werkend personage is `Normal/0.png`.

```
30 - MyChara/
  Metadata.json
  CharaConfig.txt
  Render.png
  Normal/0.png 1.png 2.png ...
  Clear/0.png ...
  GoGo/0.png ...
  Miss/0.png ...
  Menu_Loop/0.png ...
  Result_Clear/0.png ...
  Sounds/                (optionele stemfragmenten, zie Stap 6)
```

## Stap 4: Schrijf CharaConfig.txt

`CharaConfig.txt` is een `Key=Value`-tekstbestand; regels die met `;` beginnen zijn commentaar. Het ingebouwde script leest deze sleutels (het meegeleverde 3D-sjabloon leest de positiesleutels ook):

- `Chara_Resolution=W,H` (standaard `1280,720`): de resolutie waarvoor je de onderstaande coördinaten maakt. Het spel schaalt posities bij het tekenen van deze resolutie naar de skinresolutie.
- `Chara_LegacyMode` (standaard `1`): behoudt de verankering en offsetcorrecties van 0.6.0. Personages die van oudere versies zijn overgezet, rekenen hierop.
- `Game_Chara_X=...` / `Game_Chara_Y=...`: gameplaypositie; het script gebruikt de eerste waarde van elke lijst. `Game_Chara_Offset=X,Y` is een alternatieve vorm.
- `Game_Chara_X_AI=...` / `Game_Chara_Y_AI=...`: één waarde per speler voor het AI-gevecht. Wanneer beide sleutels aanwezig zijn, vervangen ze de AI-gevechtspositie van de skin voor dit personage.
- `Game_Chara_Balloon_X` / `Game_Chara_Balloon_Y`, `Game_Chara_Kusudama_X` / `Game_Chara_Kusudama_Y`: posities tijdens ballon- en kusudama-sequenties (eerste waarde gebruikt). `Game_Chara_Balloon_Offset`, `Game_Chara_Kusudama_Offset` en `Game_Chara_Tower_Offset` nemen een `X,Y`-paar.
- `Menu_Offset=X,Y`, `Menu_Chara_Scale`, `Result_Offset=X,Y`, `Heya_Chara_Render_Offset=X,Y`: offsets voor het menu, de resultaten en de kamerrender.
- `Game_Chara_Motion_<State>=0,1,2,...`: de volgorde waarin de frames van een status worden afgespeeld, als 0-gebaseerde frame-indices. Indien weggelaten worden de frames in bestandsvolgorde afgespeeld. Statusnamen volgen de mapnamen, bijvoorbeeld `Game_Chara_Motion_Normal`, `Game_Chara_Motion_GoGo`, `Game_Chara_Motion_Miss_Down`, `Game_Chara_Motion_Balloon_Broke`, `Game_Chara_Motion_Tower_Climbing`.
- `Game_Chara_Beat_<State>=N`: hoeveel beats één lus van de status beslaat, bijvoorbeeld `Game_Chara_Beat_Normal=1`, `Game_Chara_Beat_GoGo=2`.
- Menu-, titel- en resultaatstatussen gebruiken `Menu_Chara_Motion_Loop/Wait/Start/Select`, `Title_Chara_Motion_Normal/Entry`, `Result_Chara_Motion_Normal/Clear/Failed_In/Failed`, met bijpassende `_Beat_`-sleutels of vaste duren in milliseconden: `Chara_Menu_Loop_AnimationDuration`, `Chara_Menu_Wait_AnimationDuration`, `Chara_Menu_Start_AnimationDuration`, `Chara_Menu_Select_AnimationDuration`, `Chara_Normal_AnimationDuration`, `Chara_Entry_AnimationDuration`, `Chara_Result_Normal_AnimationDuration`, `Chara_Result_Clear_AnimationDuration`, `Chara_Result_Failed_In_AnimationDuration`, `Chara_Result_Failed_AnimationDuration`.

De volledige sleutellijst, met standaardwaarden, is de tabel `load_chara_config_defs` bovenaan het ingebouwde `CharaScript.lua`. Het script negeert sleutels die het niet kent, dus de meegeleverde `01 - Template/CharaConfig.txt` bevat ook enkele sleutels van de skinkant die in dit bestand geen effect hebben.

```ini
Chara_Version=0.6.1.0
Chara_Resolution=1920,1080

;X-positie van het personage (1P,2P)
Game_Chara_X=0,0
;Y-positie van het personage (1P,2P)
Game_Chara_Y=0,805

;Framevolgorde van de normale status en beats per lus
Game_Chara_Motion_Normal=0,1,2,3,4,5,6
Game_Chara_Beat_Normal=1

;Framevolgorde van GoGo en beats per lus
Game_Chara_Motion_GoGo=0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15
Game_Chara_Beat_GoGo=2
```

## Stap 5 (3D-pad): Lever model.glb en een Script.lua per personage mee

Wanneer `Script.lua` in de personagemap bestaat, vervangt het het ingebouwde script volledig. Het spel roept dan deze globale functies op naam aan:

- `loadAnimation(animationType)`, `disposeAnimation(animationType)`
- `availableAnimation(animationType)` dat een boolean teruggeeft. Het spel accepteert de oudere spelfout `avaialbeAnimation` nog steeds: het probeert eerst `availableAnimation` en valt terug op `avaialbeAnimation`. Het meegeleverde 3D-sjabloon gebruikt nog de oude naam.
- `setAnimationDuration(animationType, durationMs)`, `resetAnimationCounter(animationType)`
- `update(delta, animationType, looping)` dat `true` teruggeeft wanneer een niet-herhalende animatie is afgelopen
- `draw(animationType, x, y, scaleX, scaleY, opacity, color, contextType, anchor, clipW, clipH, clipX, clipY, rotation, blendMode, wrapMode, gradientMap)`
- `getDrawSize(animationType)` dat breedte en hoogte teruggeeft
- `getHeyaRenderOffset()` dat x en y teruggeeft; `getAIBattlePosition(player, charaScale)` dat x en y teruggeeft, of `nil` om de positie van de skin te gebruiken
- `loadVoice(voiceType)`, `disposeVoice(voiceType)`, `playVoice(voiceType)`

Animatietypes zijn de strings achter de constanten `CHARACTER.ANIM_*` (`"Game/Normal"`, `"Menu/Normal"`, ...), plus de twee speciale types `CHARACTER.ANIM_PREVIEW` (miniatuur) en `CHARACTER.ANIM_RENDER` (volledig portret). Stemtypes zijn de constanten `CHARACTER.VOICE_*`. De terugvalketen uit Stap 3 geldt ook voor gescripte personages: het spel vraagt `availableAnimation` en doorloopt de alternatieven tot er een beschikbaar is.

De meegeleverde map `01 - Template3D` bevat alleen `CharaConfig.txt`, `Effects.json`, `Metadata.json`, `model.glb`, `Render.png` en `Script.lua`. Zijn script laadt `model.glb` met `MODEL:Load`, rendert het in een scène die het met `SCENE3D:CreateScene` aanmaakt, leest de positiesleutels van `CharaConfig.txt`, en koppelt elk animatietype aan een clipindex en een beataantal in een `CLIP`-tabel. Om een 3D-personage te maken, kopieer je de map, vervang je `model.glb` en `Render.png`, en bewerk je `CLIP` zodat elk type naar de juiste clipindex van je model wijst.

```lua
-- fragment uit 01 - Template3D/Script.lua
local CLIP = {
  [CHARACTER.ANIM_GAME_NORMAL] = { clip = 0,  beat = 1 };
  [CHARACTER.ANIM_GAME_CLEAR]  = { clip = 1,  beat = 1 };
  [CHARACTER.ANIM_GAME_GOGO]   = { clip = 2,  beat = 4 };
  [CHARACTER.ANIM_MENU_NORMAL] = { clip = 14, beat = 2 };
  [CHARACTER.ANIM_RESULT_CLEAR]= { clip = 19, beat = 1 };
  -- één item per animatiestatus die het model ondersteunt
}

function loadAnimation(animationType)
  -- bouw de clip-/preview-/renderdata op en markeer die als beschikbaar
end

function availableAnimation(animationType)
  return animations[animationType] ~= nil
end
```

## Stap 6: Optionele bestanden: Effects.json, Unlock.json, Palettes.json, stemmen

- `Effects.json`: `gauge` (`Normal`, `Hard` of `Extreme`; standaard `Normal`) kiest het type zielgauge. `Hard` vermenigvuldigt de muntwinst met 1,5 en `Extreme` met 1,8, tenzij het spel de normale gauge afdwingt. Wanneer de fun-mod Minesweeper actief is, is `bombFactor` (1-100, standaard 20) het percentage noten dat de mod in bommen verandert en `fuseRollFactor` (0-100, standaard 0) het percentage ballonnen dat hij in lontroffels verandert.
- `Unlock.json`: indien aanwezig blijft het personage vergrendeld tot de speler aan de voorwaarde voldoet. Het formaat en de voorwaarde-id's komen overeen met die voor nummers; zie de handleiding over ontgrendelvoorwaarden. De speler koopt muntvoorwaarden in het kamerscherm; het spel controleert de andere voorwaarden automatisch op het resultatenscherm. Meegeleverde voorbeelden: Kuro gebruikt `{ "condition": "dp", "type": "me", "values": [3, 3, 10] }` (tien clears van Extreme-charts met full combo of beter) en Aoi gebruikt `{ "condition": "ch", "type": "me", "values": [200] }` (200 munten).
- `Palettes.json`: een array van kleurpaletten die de speler op het personage kan toepassen. Elk item heeft `name`, `blend` (0-1), `stops` (een array van gradiëntstops `[position, R, G, B]` of `[position, R, G, B, A]`; geef er minstens twee op) en `plays`, het aantal spelbeurten met dit personage dat het palet ontgrendelt (0 of afwezig betekent meteen beschikbaar). Een item met `"stops": null` is de ongetinte standaard.
- Stemmen: het ingebouwde script laadt `.ogg`-bestanden van vaste paden binnen de personagemap, bijvoorbeeld `Sounds/Clear/Clear.ogg`, `Sounds/Clear/Failed.ogg`, `Sounds/Clear/FullCombo.ogg`, `Sounds/Clear/AllPerfect.ogg`, `Sounds/Menu/SongSelect.ogg`, `Sounds/Menu/SongDecide.ogg`, `Sounds/Menu/DiffSelect.ogg`, `Sounds/Title/Sanka.ogg`, `Sounds/Result/BestScore.ogg`, `Sounds/Result/ClearSuccess.ogg`, `Sounds/Result/ClearFailed.ogg`. De volledige lijst is de tabel `voice_files` bovenaan het ingebouwde `CharaScript.lua`. Het script slaat ontbrekende bestanden over.

```json
{
  "gauge": "Normal",
  "bombFactor": 20,
  "fuseRollFactor": 0
}
```

```json
{
  "condition": "ch",
  "type": "me",
  "values": [ 200 ]
}
```

```json
[
  { "name": "Default", "stops": null },
  { "name": "Green", "blend": 1.0, "stops": [ [0, 0, 0, 0], [0.25, 0, 255, 0] ], "plays": 10 }
]
```

## Stap 7: Herstart en selecteer het personage

Herstart het spel (of herlaad de skin vanuit de instellingen). Het personage verschijnt in de personagelijst van het kamerscherm, waar vergrendelde personages hun ontgrendelvoorwaarde tonen. Lua-stages kunnen de lijst ook lezen via de global `CHARACTERLIST`, die van elk item de mapnaam, weergavenaam, zeldzaamheid en ontgrendelvoorwaarde beschikbaar stelt.

## Probleemoplossing en opmerkingen

- Het personage verschijnt niet: controleer of de map rechtstreeks onder `Global/Characters/` staat en herstart het spel. Het spel bouwt de lijst eenmaal bij het opstarten op.
- Het personage tekent niets: `Normal/0.png` ontbreekt, of de mapnamen komen niet overeen met de tabel in Stap 3. Frames moeten `0.png`, `1.png`, ... heten zonder gaten; een gat beëindigt de animatie bij die index zonder foutmelding.
- Het personage staat buiten beeld of heeft de verkeerde grootte: `Chara_Resolution` moet overeenkomen met de resolutie waarvoor je de positiewaarden hebt gemaakt. Wanneer de sleutel afwezig is, gaat het spel uit van `1280,720`.
- Slechts een deel van de animatieset speelt: statussen zonder terugval (Cleared, Failed, Return, de ballon- en kusudama-statussen) hebben een eigen map nodig.
- Een 3D-personage toont elke animatie als niet beschikbaar: `Script.lua` moet `availableAnimation` (of `avaialbeAnimation`) definiëren en `true` teruggeven voor de geladen types.
- Een aanwezige `Script.lua` vervangt het ingebouwde script volledig. Een gescript personage kan nog steeds mappen met genummerde PNG's laden, maar alleen als het script ze zelf laadt.
- Opslagbestanden verwijzen naar de mapnaam, dus een map hernoemen die spelers al hebben geselecteerd, zet hun selectie terug naar het lege slot.
- De meegeleverde JSON-bestanden bevatten afsluitende komma's. De JSON-parser van het spel accepteert die; strikte validators weigeren ze.
