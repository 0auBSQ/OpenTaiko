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
- `AIBattleNameplate=`: id van het naamplaatje dat de AI-tegenstander draagt (Stap 5).
- `FontName<LANG>=` en `BoxFontName<LANG>=`: lettertypebestand per speltaal, waarbij `<LANG>` de taalcode in hoofdletters is (`EN`, `JA`, `FR`, `ES`, `NL`, `DE`, `RU`, `KO`, `ZH`). Het pad is relatief ten opzichte van de skinroot (een absoluut pad werkt ook) en het bestand moet bestaan, anders laat de parser de sleutel weg.
- `FontKerning=`: `1` (standaard) past de kerning van het lettertype toe tussen naburige letters, zodat paren als `AV` of `Ta` dichter bij elkaar staan; `0` zet het uit. Verticale tekst wordt altijd één teken per vak gezet, zonder kerning.
- `FontPalt=`: `1` zet de proportionele breedtes van het lettertype voor Japans aan (de OpenType-functie `palt`): kana, leestekens en haakjes krijgen hun eigen breedte in plaats van een vol vierkant. `0` (standaard) houdt de volle breedte aan. Alleen lettertypen met deze functie veranderen.

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
;Naamplaatje dat de AI-tegenstander draagt (een id uit de lijst met naamplaatjes).
AIBattleNameplate=66
FontNameEN=Fonts/MPLUSRounded1c-Medium.ttf
FontNameJA=Fonts/MPLUSRounded1c-Medium.ttf
BoxFontNameEN=Fonts/MPLUSRounded1c-Regular.ttf
BoxFontNameJA=Fonts/MPLUSRounded1c-Regular.ttf
FontKerning=1
FontPalt=0
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

## Stap 5: De AI-gevechtsopties

`AIBattleCharacter=` noemt de map onder `Global/Characters/` die het spel voor de AI-tegenstander in de AI-gevechtsmodus gebruikt. De standaard is `10v2 - AItritus`. De genoemde map moet bestaan.

`AIBattleNameplate=` noemt via zijn id het naamplaatje dat de AI-tegenstander draagt (zie [Naamplaatjes toevoegen](nameplates.md)). De standaard is `66`. Bij een id dat niet in de naamplaatjeslijst staat, toont het spel het plaatje van de AI zonder titel.

```ini
;Personagemap die voor het AI-gevechtsslot wordt gebruikt.
AIBattleCharacter=10v2 - AItritus
;Naamplaatje dat de AI-tegenstander draagt (een id uit de lijst met naamplaatjes).
AIBattleNameplate=66
```

## Stap 6: Splits de configuratie met #include

Wanneer de parser een regel van de vorm `#include SomeFile.ini` tegenkomt, leest hij dat bestand ter plekke in, recursief. Het pad is relatief ten opzichte van de skinroot. De meegeleverde `SkinConfig.ini` bevat alleen de metadata- en lettertypesleutels en sluit daarna één bestand per scherm in. Behoud deze regels wanneer je de skin kopieert, en bewerk de afzonderlijke `*Config.ini`-bestanden om een scherm bij te stellen.

```
; staart van SkinConfig.ini (meegeleverde skin, in volgorde)
#include OtherConfig.ini
#include TitleConfig.ini
#include ConfigConfig.ini
#include SongSelectConfig.ini
#include SongLoadingConfig.ini
#include GameConfig.ini
#include ModIconsConfig.ini
#include NameplateConfig.ini
#include AIResultConfig.ini
#include ResultConfig.ini
#include DaniSelectConfig.ini
#include DanResultConfig.ini
#include TowerResultConfig.ini
#include OnlineLoungeConfig.ini
#include ModalConfig.ini
#include Game4PConfig.ini
#include Result4PConfig.ini
#include Modal4PConfig.ini
```

## Stap 7: Leer de mapindeling van een skin kennen

Met de meegeleverde skin als referentie bevat de skinroot:

- `Graphics/`: afbeeldingen gegroepeerd in genummerde mappen per scherm (`0_Startup`, `1_Title`, `2_Config`, `3_DaniSelect`, `5_Game`, `6_Result`, `7_DanResult`, `8_TowerResult`, `12_OnlineLounge`) plus enkele gedeelde afbeeldingen bovenaan. Geanimeerde achtergronden zijn `Script.lua`-bestanden die naast de afbeeldingen van de map staan waartoe ze behoren (bijvoorbeeld `Graphics/0_Startup/Script.lua` en de mappen onder `Graphics/5_Game/5_Background/`). Een torenlook is één map onder `Graphics/5_Game/5_Background/Tower/Down/` met de lucht, de verdiepingen (`Base/`), het dak (`Top.png`), een optionele decoratie (`Deco/`) en zijn indeling in `Config.json`; een chart kiest er een met `TOWERTYPE:`, en de torenselectie, het laadscherm en het resultaat tekenen dezelfde toren uit hun eigen kopie van de look, een map met dezelfde naam onder `Modules/Lib/TowerArt/` met de onderdelen op halve grootte en een eigen `Config.json` (`floor_height`, en `deco_dx`/`deco_dy`, de verschuiving van de decoratie vanaf het midden onderaan zijn verdieping, in pixels van die onderdelen); een nieuwe torenlook heeft dus beide mappen nodig.
- `Sounds/`: systeemgeluiden en BGM die het spel via vaste bestandsnamen laadt, bijvoorbeeld `Sounds/Move.ogg`, `Sounds/Decide.ogg`, `Sounds/Cancel.ogg`, `Sounds/BGM/Title.ogg`, `Sounds/BGM/SongSelect.ogg`, `Sounds/BGM/Result.ogg`. Als een bestand ontbreekt, speelt dat geluid niet af.
- `Fonts/`: de `.ttf`-bestanden waarnaar de `FontName`-sleutels verwijzen.
- `Locales/`: één map per taal (`en/`, `ja/`, ...) met zoveel JSON-bestanden als nodig, één per scherm of functie (`coin_shop.json`, `items.json`, ...), elk met de vorm `{ "Entries": { "KEY": "text" } }`; het spel leest elk bestand van de map (een enkel bestand `Locales/<lang>.json` wordt ook gelezen). Deze strings labelen de eigen schermen en instellingen van de skin, en Lua leest ze via `THEME:GetSkinString(key)`. Wanneer een sleutel in de actieve taal ontbreekt, zoekt het spel hem op in de `DefaultLocale`-taal.
- `Modules/`: de Lua-moduleboom (Stap 8).
- `ThemeSettings.json`: een array van instellingen die het optiescherm onder Thema-instellingen toont. Elk item heeft `id`, `type` (`bool`, `int`, `double`, `string`, `enum`, of `key` voor één toetsenbordtoets die het optiescherm vastlegt en als naam opslaat voor `INPUT:KeyboardPressed`), `scope` (`global`, de standaard, of `save` voor één waarde per opslagbestand), gelokaliseerde `label` en `description`, `default`, en `min`/`max` of `options` afhankelijk van het type. Het bestand mag in plaats daarvan een object zijn met `sections` (id naar gelokaliseerde kop, eenmaal gedeclareerd) en `settings` (de array); de optionele `section` van een item noemt een van die ids, of is een gewone koptekst, en groepeert het onder die tussenkop op de themapagina. `session: true` houdt de waarde alleen in het geheugen (bij elke start weer de standaard), `hidden: true` laat hem uit het instellingenmenu zodat de skin hem zelf leest en schrijft, en `step` bepaalt de stap van een int.
- `SkinConfig.ini` en de ingesloten `*Config.ini`-bestanden.
- `README.txt`, `LICENSE.md`, `Licenses/`: naamsvermeldingsbestanden. Het spel leest ze niet.

```
My New Skin/
  SkinConfig.ini
  ThemeSettings.json
  Graphics/           afbeeldingen per scherm; sommige mappen bevatten een achtergrond-Script.lua
  Sounds/             .ogg-systeemgeluiden met vaste naam en BGM/
  Fonts/              .ttf-bestanden genoemd door de FontName-sleutels
  Locales/            en/, ja/, ... one .json per screen ({ "Entries": { ... } })
  Modules/            de Lua-moduleboom (Stap 8)
  <screen>Config.ini  lay-outbestanden ingesloten via #include
```

## Stap 8: De Modules-boom en de modules die het spel vereist

Wanneer de skin laadt, scant het spel vier submappen van `Modules/` en behandelt het elke directe submap daarin als één module waarvan het instapbestand `Script.lua` is:

- `Modules/Transitions/`: transities die tussen stages afspelen. Het spel laadt ze als eerste, zodat ze klaar zijn voor de eerste stagewissel.
- `Modules/Stages/`: volledige schermen. Je betreedt een stage met `Exit("stage", "<folder name>")`.
- `Modules/Activities/`: herbruikbare subschermen die over een stage heen liggen (bijvoorbeeld `confirm_dialog`, `mod_select_dialog`, `song_select_core`).
- `Modules/ROActivities/`: alleen-lezen overlays die het spel rechtstreeks aanstuurt.

Het spel scant `Modules/Lib/` niet. Bestanden daar laad je met `require`: een module zoekt in zijn eigen map, daarna in `Modules/Lib/` en daarna in `Global/Modules/Lib/` (zie hieronder), dus `require("dialogue")` leidt naar `Modules/Lib/dialogue.lua`.

De spelmap kan ook modules bevatten die elke skin laadt. `Global/Modules/` in de installatiemap van het spel heeft dezelfde submappen `Transitions/`, `Stages/`, `Activities/`, `ROActivities/` en `Lib/` als de `Modules/` van een skin, en het spel laadt elke globale categorie direct na die van de skin. Een naam kan op beide plaatsen bestaan. Vraagt een module van de skin een stage, activity, ROActivity of transition op naam op, dan krijgt die de module van de skin, en een globale module krijgt de globale; elk valt terug op de andere plaats als de eigen plaats geen module met die naam heeft. `require` volgt dezelfde regel: een globale module zoekt in `Global/Modules/Lib/` vóór de `Modules/Lib/` van de skin. De modules die het spel zelf op naam zoekt (hieronder vermeld) komen eerst uit de skin. `_boot` en `_title` moeten in de skin blijven: als `Global/Modules/Stages/` een van beide bevat, stopt het spel bij het opstarten met een fout.

De Lib-bestanden die het spel in `Global/Modules/Lib/` meelevert, staan beschreven in [Meegeleverde modules](../modules/README.md): een UI-bibliotheek, een dialoogvenster, helpers voor invoer en lokalisatie, de 3D-wereldengine, en de regels van de pagode, de kluis en de muntwinkel, onder namen met een naamruimte zoals `require("UI.Pick")` of `require("Core.Easing")`. Uit de plek waar globale bestanden staan, volgen een paar regels:

- Een Lib-bestand draait in de Lua-state van de module die het met require laadt. Zijn eigen requires volgen de zoekvolgorde van die module, en de relatieve paden die het aan `TEXTURE`, `SOUND`, `JSONLOADER` en de andere loaders geeft, worden opgelost ten opzichte van de map van die module. Een Lib-bestand dat assets meelevert, bereikt ze via `require("Core.Here")(...)` en de loaders voor absolute paden.
- Globale modules hebben geen `Locales/` en geen `ThemeSettings.json`: `THEME:GetSkinString` en de thema-instellingen lezen die van de actieve skin. Een globale module bewaart zijn strings in zijn eigen map `lang/` (Stap 10).
- De texturen, geluiden en strings van `SHARED` vormen één naamruimte voor de skin en elke globale module: geef je sleutels een prefix.
- Een update voegt bestanden toe en vervangt ze, maar verwijdert nooit oude, en modules van de skin zoeken in de `Modules/Lib/` van de skin vóór `Global/Modules/Lib/`, dus een verouderd skinbestand met dezelfde naam zou winnen. Daarom staan de meegeleverde modules in naamruimtemappen die geen enkel skinbestand deelt, en daarom houdt de meegeleverde skin zijn oude Lib-namen als doorverwijzingen van twee regels naar die modules in plaats van de bestanden te verwijderen.

Binnen elke categorie maakt het spel eerst elke module aan en draait daarna `onStart` op elk ervan, in de volgorde Transitions, Stages, Activities, ROActivities.

Het spel zoekt deze modules op naam op, en de meegeleverde skin levert ze allemaal:

- Stages `_boot` en `_title`. Het spel stopt met een fout als een van beide ontbreekt.
- ROActivities `modal`, `config_ui`, `nameplate`, `popup_menu`, `modicons`, `song_enum`, `cutscene`, `tower_view`, `tower_hud` en `danplate`.
- Transitions `default` en `song_loading`. `song_loading` speelt terwijl het spel een nummer laadt; het spel gebruikt `default` wanneer `Exit` geen transitie noemt of een noemt die niet bestaat. Een skin zonder enige transitiemodule valt terug op een gewone zwarte fade.

Houd al deze modules op hun plaats bij het bouwen van een skin; voeg je eigen modules ernaast toe.

```
Modules/
  Transitions/   <name>/Script.lua   (als eerste geladen; "default" en "song_loading" door het spel gebruikt)
  Stages/        <name>/Script.lua   ("_boot" en "_title" vereist)
  Activities/    <name>/Script.lua
  ROActivities/  <name>/Script.lua   (modal, config_ui, nameplate, popup_menu, modicons, song_enum, cutscene, danplate vereist)
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

Een module kan zijn eigen vertalingen bewaren in een submap `lang/` naast `Script.lua`, gelezen via de meegeleverde module [Locale.i18n](../modules/locale.md): één bestand `lang/<code>/<name>.json` per onderdeel en taal, met stabiele id's als sleutels en het Engels in `lang/en/` als terugval. Dit staat los van de skinbrede map `Locales/` uit Stap 7, en het is de enige manier waarop een globale module zijn tekst kan vertalen. Enkele oudere stages van de meegeleverde skin (bijvoorbeeld `Modules/Stages/intro_nokon/lang/ja.lua`) gebruiken de eerste vorm van de bibliotheek, een Lua-tabel die elke Engelse string aan zijn vertaling koppelt.

```
Modules/Stages/my_stage/
  Script.lua
  lang/
    en/my_stage.json   -- require("Locale.i18n").texts("my_stage")
    ja/my_stage.json
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
