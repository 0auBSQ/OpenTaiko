<!-- guides/skins.md -->

# Skins en thema's toevoegen

Een skin is een map onder de map `System/` van het spel. Ze levert de graphics, geluiden, lettertypen, lay-outwaarden, locale-bestanden en de Lua-modules die elk scherm tekenen. Deze handleiding legt uit wat een map tot een skin maakt, de sleutels van `SkinConfig.ini`, de mapindeling, de Lua-moduleboom en zijn levenscyclus, en hoe je een skin installeert en selecteert. De API-referentie behandelt de Lua-API zelf (tekenen, geluid, invoer enzovoort).

Een skin bevat ook de Lua-modules die elk scherm draaien. Het spel laadt specifieke modules op naam en springt naar specifieke stages, dus de skinkiezer vermeldt een skin die van nul is opgebouwd wel, maar het spel kan hem niet draaien. Begin vanuit een kopie van de meegeleverde skin.

## Voordat je begint

- OpenTaiko 0.6.1 geïnstalleerd, met de meegeleverde skin `System/Open-World Memories/` aanwezig.
- Een teksteditor voor `SkinConfig.ini`, de ingesloten `*Config.ini`-bestanden en de Lua-modules.
- Basiskennis van Lua als je het schermgedrag wilt veranderen. Een pure hertexturering (PNG- en OGG-bestanden vervangen en `.ini`-waarden bewerken) vereist geen Lua.

## Stap 1: Begrijp wat een map tot een skin maakt

Bij het opstarten somt het spel de submappen van `System/` op. Een map telt alleen als skin als `Graphics/1_Title/Background.png` erin bestaat; het spel slaat elke andere map over. Als de geselecteerde skinmap ontbreekt, valt het spel terug op `System/Default/`, dan op de eerste geldige skin in alfabetische volgorde, dan op `System/` zelf.

Deze controle zorgt er alleen voor dat de map wordt vermeld. Stap 8 noemt de modules die ook moeten bestaan voordat de skin draait.

```
System/
  Open-World Memories/         <- de meegeleverde skin
  My New Skin/                 <- je skin
    Graphics/
      1_Title/
        Background.png          <- vereist om de map te laten vermelden
    SkinConfig.ini
```

## Stap 2: Kopieer de meegeleverde skin

Kopieer `System/Open-World Memories/` naar een nieuwe map ernaast, bijvoorbeeld `System/My New Skin/`. De kopie bevat alles wat het spel nodig heeft: `Graphics/`, `Sounds/`, `Fonts/`, `Locales/`, `Modules/`, `ThemeSettings.json`, `SkinConfig.ini` en de `*Config.ini`-bestanden die het insluit. De mapnaam is de identiteit van de skin (het spel registreert hem als geselecteerde skin en de skinkiezer toont hem), dus houd hem bestandssysteemveilig. Bewerk daarna `SkinConfig.ini` zodat de metadata je skin beschrijft.

## Stap 3: Bewerk SkinConfig.ini

`SkinConfig.ini` is een `Key=Value`-bestand, één instelling per regel. De parser verwijdert voorloopspaties en -tabs, behandelt regels die met `;` beginnen als commentaar, en leest een regel alleen wanneer die precies één `=` bevat. Sleutels worden exact vergeleken, en de parser negeert een onbekende sleutel zonder een fout te melden. De sleutels op skinniveau zijn:

- `Name=`: weergavenaam. Alleen metadata; de mapnaam selecteert de skin.
- `Version=`, `Creator=`: vrije strings (standaard `Unknown`). Het spel valideert ze niet.
- `DefaultLocale=`: locale-id dat het spel gebruikt wanneer de actieve speltaal geen bestand onder `Locales/` heeft (standaard `en`).
- `Resolution=W,H`: de resolutie waarvoor je de lay-outwaarden maakt (standaard `1280,720`). De meegeleverde skin gebruikt `1920,1080`.
- `Resolutions=`: selecteerbare renderschaalfactoren (Stap 4).
- `AIBattleCharacter=`: personagemap die voor de AI-tegenstander wordt gebruikt (Stap 5).
- `FontName<LANG>=` en `BoxFontName<LANG>=`: lettertypebestand per speltaal, waarbij `<LANG>` de taalcode in hoofdletters is (`EN`, `JA`, `FR`, `ES`, `NL`, `DE`, `RU`, `KO`, `ZH`). Het pad is relatief ten opzichte van de skinroot (een absoluut pad werkt ook) en het bestand moet bestaan, anders laat de parser de sleutel weg.

Elke andere sleutel (`Game_*`, `Result_*`, `Title_*` enzovoort) is een lay-outwaarde van een scherm. Dezelfde parser leest ze, en daarom kunnen ze in de ingesloten bestanden van Stap 6 staan.

```ini
;Skininformatie
Name=My New Skin
DefaultLocale=en
Version=1.0.0
Creator=Your Name
Resolution=1920,1080
;Selecteerbare renderschaalfactoren (<=1; decimaal of a/b-breuk, door komma's gescheiden). 1 is altijd beschikbaar en is de standaard.
Resolutions=1,2/3,1/3
;Personagemap die voor het AI-gevechtsslot wordt gebruikt.
AIBattleCharacter=10v2 - AItritus
FontNameEN=Fonts/MPLUSRounded1c-Medium.ttf
FontNameJA=Fonts/MPLUSRounded1c-Medium.ttf
BoxFontNameEN=Fonts/MPLUSRounded1c-Regular.ttf
BoxFontNameJA=Fonts/MPLUSRounded1c-Regular.ttf
```

## Stap 4: De optie Resolutions

`Resolutions=` is een door komma's gescheiden lijst van de renderschaalfactoren die het instellingenmenu aanbiedt. Het spel rendert op `Resolution` maal de gekozen factor en schaalt het resultaat op naar het venster; de venstergrootte verandert niet. Elk token is een decimaal getal (`0.5`) of een breuk (`2/3`). De parser laat tokens buiten het bereik 0 < waarde <= 1, niet-parseerbare tokens en duplicaten weg, voegt `1` toe als het ontbreekt, en sorteert de lijst met `1` vooraan. Het scheidingsteken moet een komma zijn, omdat een puntkomma een commentaarregel begint. Het instellingenmenu toont elk item met zijn pixelgrootte, bijvoorbeeld `2/3 (1280x720)` voor een skin van 1920x1080.

```ini
Resolution=1920,1080
Resolutions=1,2/3,1/3
; levert de opties op:
;   1     -> 1920x1080  (standaard)
;   2/3   -> 1280x720
;   1/3   -> 640x360
```

## Stap 5: De optie AIBattleCharacter

`AIBattleCharacter=` noemt de map onder `Global/Characters/` die het spel voor de AI-tegenstander in de AI-gevechtsmodus gebruikt. De standaard is `10v2 - AItritus`. De genoemde map moet bestaan.

```ini
;Personagemap die voor het AI-gevechtsslot wordt gebruikt.
AIBattleCharacter=10v2 - AItritus
```

## Stap 6: Splits de configuratie met #include

Wanneer de parser een regel van de vorm `#include SomeFile.ini` tegenkomt, leest hij dat bestand ter plekke in, recursief. Het pad is relatief ten opzichte van de skinroot. De meegeleverde `SkinConfig.ini` bevat alleen de metadata- en lettertypesleutels en sluit daarna één bestand per scherm in. Behoud deze regels wanneer je de skin kopieert, en bewerk de afzonderlijke `*Config.ini`-bestanden om een scherm bij te stellen.

```
; staart van SkinConfig.ini (meegeleverde skin, in volgorde)
#include OtherConfig.ini
#include TitleConfig.ini
#include ConfigConfig.ini
#include SongSelectConfig.ini
#include HeyaConfig.ini
#include SongLoadingConfig.ini
#include GameConfig.ini
#include ModIconsConfig.ini
#include NameplateConfig.ini
#include AIResultConfig.ini
#include ResultConfig.ini
#include DaniSelectConfig.ini
#include DanResultConfig.ini
#include TowerResultConfig.ini
#include TowerSelectConfig.ini
#include OnlineLoungeConfig.ini
#include OpenEncyclopediaConfig.ini
#include ModalConfig.ini
#include Game4PConfig.ini
#include Result4PConfig.ini
#include Modal4PConfig.ini
```

## Stap 7: Leer de mapindeling van een skin kennen

Met de meegeleverde skin als referentie bevat de skinroot:

- `Graphics/`: afbeeldingen gegroepeerd in genummerde mappen per scherm (`0_Startup`, `1_Title`, `2_Config`, `3_DaniSelect`, `5_Game`, `6_Result`, `7_DanResult`, `7_Exit`, `8_TowerResult`, `10_Heya`, `12_OnlineLounge`, `13_TowerSelect`, `15_OpenEncyclopedia`) plus enkele gedeelde afbeeldingen bovenaan. Geanimeerde achtergronden zijn `Script.lua`-bestanden die naast de afbeeldingen van de map staan waartoe ze behoren (bijvoorbeeld `Graphics/0_Startup/Script.lua` en de mappen onder `Graphics/5_Game/5_Background/`).
- `Sounds/`: systeemgeluiden en BGM die het spel via vaste bestandsnamen laadt, bijvoorbeeld `Sounds/Move.ogg`, `Sounds/Decide.ogg`, `Sounds/Cancel.ogg`, `Sounds/BGM/Title.ogg`, `Sounds/BGM/SongSelect.ogg`, `Sounds/BGM/Result.ogg`. Als een bestand ontbreekt, speelt dat geluid niet af.
- `Fonts/`: de `.ttf`-bestanden waarnaar de `FontName`-sleutels verwijzen.
- `Locales/`: één JSON-bestand per taal (`en.json`, `ja.json`, ...) met de vorm `{ "Entries": { "KEY": "text" } }`. Deze strings labelen de eigen instellingen van de skin, en Lua leest ze via `THEME:GetSkinString(key)`. Wanneer een sleutel in de actieve taal ontbreekt, zoekt het spel hem op in het `DefaultLocale`-bestand.
- `Modules/`: de Lua-moduleboom (Stap 8).
- `ThemeSettings.json`: een array van instellingen die het optiescherm onder Thema-instellingen toont. Elk item heeft `id`, `type` (`bool`, `int`, `double`, `string` of `enum`), `scope` (`global`, de standaard, of `save` voor één waarde per opslagbestand), gelokaliseerde `label` en `description`, `default`, en `min`/`max` of `options` afhankelijk van het type.
- `SkinConfig.ini` en de ingesloten `*Config.ini`-bestanden.
- `README.txt`, `LICENSE.md`, `Licenses/`: naamsvermeldingsbestanden. Het spel leest ze niet.

```
My New Skin/
  SkinConfig.ini
  ThemeSettings.json
  Graphics/           afbeeldingen per scherm; sommige mappen bevatten een achtergrond-Script.lua
  Sounds/             .ogg-systeemgeluiden met vaste naam en BGM/
  Fonts/              .ttf-bestanden genoemd door de FontName-sleutels
  Locales/            en.json, ja.json, ... ({ "Entries": { ... } })
  Modules/            de Lua-moduleboom (Stap 8)
  <screen>Config.ini  lay-outbestanden ingesloten via #include
```

## Stap 8: De Modules-boom en de modules die het spel vereist

Wanneer de skin laadt, scant het spel vier submappen van `Modules/` en behandelt het elke directe submap daarin als één module waarvan het instapbestand `Script.lua` is:

- `Modules/Transitions/`: transities die tussen stages afspelen. Het spel laadt ze als eerste, zodat ze klaar zijn voor de eerste stagewissel.
- `Modules/Stages/`: volledige schermen. Je betreedt een stage met `Exit("stage", "<folder name>")`.
- `Modules/Activities/`: herbruikbare subschermen die over een stage heen liggen (bijvoorbeeld `confirm_dialog`, `mod_select_dialog`, `song_select_core`).
- `Modules/ROActivities/`: alleen-lezen overlays die het spel rechtstreeks aanstuurt.

Het spel scant `Modules/Lib/` niet. Bestanden daar laad je met `require`: het zoekpad van een module is zijn eigen map gevolgd door `Modules/Lib/`, dus `require("dialogue")` leidt naar `Modules/Lib/dialogue.lua`. Je kunt stages en activities ook onder `Global/Stages/` en `Global/Activities/` in de installatiemap van het spel plaatsen; het spel laadt die voor elke skin.

Binnen elke categorie maakt het spel eerst elke module aan en draait daarna `onStart` op elk ervan, in de volgorde Transitions, Stages, Activities, ROActivities.

Het spel zoekt deze modules op naam op, en de meegeleverde skin levert ze allemaal:

- Stages `_boot` en `_title`. Het spel stopt met een fout als een van beide ontbreekt.
- ROActivities `modal`, `config_ui`, `nameplate`, `popup_menu`, `modicons`, `song_enum` en `danplate`.
- Transitions `default` en `song_loading`. `song_loading` speelt terwijl het spel een nummer laadt; het spel gebruikt `default` wanneer `Exit` geen transitie noemt of een noemt die niet bestaat. Een skin zonder enige transitiemodule valt terug op een gewone zwarte fade.

Houd al deze modules op hun plaats bij het bouwen van een skin; voeg je eigen modules ernaast toe.

```
Modules/
  Transitions/   <name>/Script.lua   (als eerste geladen; "default" en "song_loading" door het spel gebruikt)
  Stages/        <name>/Script.lua   ("_boot" en "_title" vereist)
  Activities/    <name>/Script.lua
  ROActivities/  <name>/Script.lua   (modal, config_ui, nameplate, popup_menu, modicons, song_enum, danplate vereist)
  Lib/           gedeelde .lua-bestanden bereikt met require, niet gescand
```

## Stap 9: De Script.lua van een stage en zijn levenscyclus

`Script.lua` draait eenmaal wanneer het spel de module aanmaakt, met de engine-globals (`TEXTURE`, `SOUND`, `INPUT`, `CONFIG`, `THEME` en de rest) al gedefinieerd. Het spel zoekt daarna globale functies op naam op en roept ze aan. Voor een stage:

- `onStart()`: eenmalig, wanneer de skin laadt. Draait als een coroutine, dus zwaar laadwerk kan `coroutine.yield()` of de `LOADING`-helpers aanroepen om het werk achter de laadbalk over meerdere frames te verdelen.
- `activate()`: elke keer dat het spel de stage betreedt. Ook een coroutine. Het spel vernieuwt de globals `CHARACTERLIST` en `PUCHICHARALIST` vlak voordat het draait, dus bouw alles wat ervan afhangt hier op; in `onStart` zijn ze nog leeg.
- `update(timestamp)`: elk frame. Geef `Exit(target, name, transition)` terug om de stage te verlaten. `target` is `"title"`, `"play"`, `"stage"` (met `name` = een stagemap) of `"legacy"` (met `name` = `heya`, `config`, `exit` of `onlinelounge`); `transition` is een map onder `Modules/Transitions/` en is standaard `default`.
- `draw()`: elk frame.
- `deactivate()`: wanneer het spel de stage verlaat.
- `afterSongEnum()`: wanneer de nummerlijst klaar is met enumereren.
- `onDestroy()`: wanneer het spel de skin afbreekt.

Ze zijn allemaal optioneel; het spel slaat een functie die je niet definieert over. Activities, ROActivities en Transitions volgen hetzelfde patroon met hun eigen hooksets.

```lua
-- Modules/Stages/my_stage/Script.lua
function onStart()
  -- eenmalige setup; mag coroutine.yield() aanroepen tijdens zwaar laadwerk
end

function activate()
  -- draait elke keer dat de stage wordt betreden
end

function update(ts)
  if INPUT:Pressed("Cancel") then
    return Exit("stage", "_title")   -- verlaat deze stage
  end
  return nil
end

function draw()
  -- rendering per frame
end

function deactivate() end
function afterSongEnum() end
function onDestroy() end
```

## Stap 10: Lokaliseer een module met lang/

Een module kan zijn eigen vertalingen bewaren in een submap `lang/` naast `Script.lua`. Omdat de eigen map van de module op zijn `require`-pad staat, leidt `require("lang.ja")` naar `lang/ja.lua`. De meegeleverde skin doet dit voor zijn grotere stages (bijvoorbeeld `Modules/Stages/myroom/lang/ja.lua` en `Modules/Stages/intro_nokon/lang/ja.lua`) via de helper `Modules/Lib/i18n.lua`. Dit staat los van de skinbrede map `Locales/` uit Stap 7.

```
Modules/Stages/my_stage/
  Script.lua
  lang/
    ja.lua        -- require("lang.ja")
```

## Stap 11: Installeer en selecteer de skin

Plaats de map onder `System/`. Open de instellingen, ga naar de sectie Uiterlijk en kies de skin bij de optie Skin; de kiezer vermeldt elke geldige skin op mapnaam en toont zijn `Graphics/1_Title/Background.png` als miniatuur. Wanneer je van skin wisselt, breekt het spel de huidige af, laadt het de nieuwe en herlaadt het al zijn Lua-modules achter een laadbalk.

Het spel schrijft de keuze naar `Config.ini` als `SkinPath=`, relatief ten opzichte van `System/`. Het schrijft de kale mapnaam (bijvoorbeeld `SkinPath=My New Skin\` op Windows) en accepteert ook de vorm `./My New Skin/` die in het commentaar van het bestand wordt getoond.

```ini
; In Config.ini (geschreven wanneer een skin in het spel wordt gekozen):
; Pad van de skinmap, relatief ten opzichte van System/
SkinPath=My New Skin\
```

## Probleemoplossing en opmerkingen

- De kiezer vermeldt de skin niet: `Graphics/1_Title/Background.png` ontbreekt, of de map staat niet rechtstreeks onder `System/`.
- Het spel geeft meteen nadat je de skin selecteert een fout: een vereiste module ontbreekt (Stap 8) of een ervan heeft een Lua-fout opgeworpen. Test een skin door ernaar te wisselen.
- Een sleutel in `SkinConfig.ini` heeft geen effect: de sleutel is verkeerd gespeld, de regel bevat meer dan één `=`, of de waarde kon niet worden geparseerd. De parser negeert onbekende sleutels zonder ze te melden.
- `Resolutions=` toont alleen `1`: de lijst gebruikte puntkomma's (een commentaarteken) of elke waarde lag buiten 0 < waarde <= 1.
- Een lettertypesleutel heeft geen effect: het bestandspad bestaat niet relatief ten opzichte van de skinroot.
- `CHARACTERLIST` of `PUCHICHARALIST` is leeg in `onStart`: het spel vult ze nadat het de modules heeft aangemaakt. Gebruik ze vanuit `activate`.
- De skinmap hernoemen verandert zijn identiteit; `SkinPath` in `Config.ini` moet naar de nieuwe naam wijzen.
- `Name=`, `Version=` en `Creator=` zijn uitsluitend informatief. Het spel voert er geen compatibiliteitscontrole op uit.
