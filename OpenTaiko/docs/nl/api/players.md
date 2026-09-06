<!-- api/players.md -->

# Spelers en profielen

Opslagbestanden, naamplaatjes, personages, puchichara's, spelstatus, thema's en de huidige taal.

Spelerindices beginnen overal op deze pagina bij 0 (0 tot 4), behalve bij THEME:GetThemeSettingForPlayer, dat bij 1 begint. Alleen-lezen modules (ROActivities en achtergronden) ontvangen een opslagbestand-handle waarvan de schrijfmethoden een fout loggen en niets doen; al het andere hier gedraagt zich in elk moduletype hetzelfde.

## Opslagbestanden

### GetSaveFile

Globale functie die de opslagbestand-handle van een spelerslot teruggeeft.

<div class="callout warn">
Roep het aan als gewone functie (`GetSaveFile(0)`). Een index buiten bereik logt een fout en geeft nil terug. Elke aanroep maakt een nieuwe handle aan die live data leest, dus er valt niets te cachen of vrij te geven.
</div>

| Methode | Beschrijving |
| --- | --- |
| `GetSaveFile(player)  -> saveFile` | Geeft de opslagbestand-handle voor het 0-gebaseerde spelerslot terug, of nil als de index buiten bereik is. |

### Opslagbestand-handle

Het profiel van één speler: naam, munten, ontgrendelde items, triggers en tellers, clearstatistieken, uitgerust personage, puchichara, naamplaatje en dan-titel.

<div class="callout warn">
Lees eigenschappen met puntsyntaxis (sf.Name, sf.Coins). Schrijfmethoden slaan onmiddellijk op. Alleen-lezen modules blokkeren de volgende: SpendCoins, EarnCoins, UnlockNameplate, UnlockSong, het toewijzen van SelectedHitsounds, SetGlobalTrigger, SetGlobalCounter, ChangeCharacter (geeft false terug), UnlockPuchichara, ChangePuchichara, UnlockCharacter, ChangeDan, ChangeName en ChangeNameplate.
</div>

| Methode | Beschrijving |
| --- | --- |
| `sf.Name  -> string` | De weergegeven naam van de speler. |
| `sf.SaveId  -> integer` | Het numerieke database-id van dit opslagbestand. |
| `sf.SaveUID  -> string` | Het unieke string-id van dit opslagbestand. |
| `sf.NameplateInfo  -> nameplateInfo` | Het uitgeruste naamplaatje (zie Naamplaatje-info-handle), of het standaard beginnersnaamplaatje als het opgeslagen id onbekend is. |
| `sf.DanplateInfo  -> danplateInfo` | De huidige dan-titel (zie Dan-plaatje-info-handle). |
| `sf.TotalPlaycount  -> integer` | Totaal aantal spelbeurten op dit opslagbestand. |
| `sf.AIBattlePlaycount  -> integer` | Aantal AI-gevechten. |
| `sf.AIBattleWins  -> integer` | Aantal gewonnen AI-gevechten. |
| `sf.Coins  -> integer` | Huidig muntsaldo. |
| `sf.TotalEarnedCoins  -> integer` | Totaal aantal munten verdiend over de levensduur van het opslagbestand. |
| `sf:SpendCoins(price)  -> nil` | Trekt munten af (het saldo komt nooit onder 0) en slaat op. |
| `sf:EarnCoins(amount)  -> nil` | Voegt munten toe aan het saldo en aan het totaal verdiende, en slaat op. |
| `sf:IsNameplateUnlocked(id)  -> bool` | Of het naamplaatje met dit id is ontgrendeld. |
| `sf:UnlockNameplate(id)  -> nil` | Ontgrendelt een naamplaatje en slaat op (no-op als het al ontgrendeld is). |
| `sf:IsSongUnlocked(uniqueId)  -> bool` | Of het nummer met dit unieke id is ontgrendeld. |
| `sf:UnlockSong(uniqueId)  -> nil` | Ontgrendelt een nummer en slaat op (no-op als het al ontgrendeld is). |
| `sf.SelectedHitsounds  -> string` | Mapnaam van de geselecteerde hitsound-set. Het toewijzen van een andere naam slaat die op en herlaadt de hitsounds van de speler. |
| `sf:GetGlobalTrigger(name)  -> bool` | Leest een benoemde booleaanse trigger. |
| `sf:GetGlobalCounter(name)  -> number` | Leest een benoemde numerieke teller. |
| `sf:SetGlobalTrigger(name, value)  -> nil` | Zet een benoemde booleaanse trigger. |
| `sf:SetGlobalCounter(name, value)  -> nil` | Zet een benoemde numerieke teller. |
| `sf:GetClearStatusCount(difficulty, clearStatus)  -> integer` | Aantal charts van een moeilijkheid (0 Easy tot 4 Extra Extreme) waarvan de beste clearstatus exact clearStatus is (0 geen, 1 assisted, 2 clear, 3 full combo, 4 perfect). 0 voor argumenten buiten bereik. |
| `sf:GetDanBestPlay(node)  -> danBestPlay` | De beste spelbeurt zonder mods voor een dan-nummerknoop (zie Beste-dan-spelbeurt-handle); een handle met HasRecord false als er geen is. |
| `sf:GetCharacter()  -> character` | De aan de speler gebonden personage-handle voor dit slot (zie Personage-handle). |
| `sf.CharacterName  -> string` | Mapnaam van het uitgeruste personage. |
| `sf:ChangeCharacter(folderName)  -> bool` | Rust het personage met deze mapnaam uit. Geeft true terug wanneer het personage nu is uitgerust of al actief was, false als geen geladen personage deze mapnaam heeft. |
| `sf:GetPuchichara()  -> puchichara` | De uitgeruste puchichara (zie Puchichara-handle), of nil als die niet kan worden opgelost. |
| `sf:IsPuchicharaUnlocked(folderName)  -> bool` | Of de puchichara met deze mapnaam is ontgrendeld. |
| `sf:UnlockPuchichara(folderName)  -> nil` | Ontgrendelt een puchichara en slaat op (no-op als die al ontgrendeld is). |
| `sf:ChangePuchichara(folderName)  -> nil` | Rust de puchichara met deze mapnaam uit en slaat op. De methode valideert de naam niet. |
| `sf:IsCharacterUnlocked(folderName)  -> bool` | Of het personage is ontgrendeld. Het uitgeruste personage telt altijd als ontgrendeld. |
| `sf:UnlockCharacter(folderName)  -> nil` | Ontgrendelt een personage en slaat op (no-op als het al ontgrendeld is). |
| `sf.DanTitleCount  -> integer` | Aantal beschikbare dan-titels, inclusief de standaardtitel (altijd minstens 1). |
| `sf:GetDanTitleByIndex(index)  -> danTitleEntry` | De dan-titel op een 0-gebaseerde index (zie Dan-titel-item-handle). Index 0 is de standaardtitel; nil indien buiten bereik. |
| `sf.SelectedDan  -> string` | Tekst van de actieve dan-titel. |
| `sf:ChangeDan(title)  -> nil` | Maakt de gegeven titel actief, kopieert zijn goud- en clearstatusvlaggen als het er een is die de speler heeft verdiend, vernieuwt het naamplaatje en slaat op. |
| `sf:ChangeName(name)  -> nil` | Wijzigt de weergegeven naam, vernieuwt het naamplaatje en slaat op. De methode negeert lege of ongewijzigde namen. |
| `sf:ChangeNameplate(id)  -> nil` | Rust het naamplaatje met dit id uit, vernieuwt het naamplaatje en slaat op. Een id dat in de database ontbreekt, wist de gecachte titeltekst. |

```lua
local save = GetSaveFile(0)
local entry = CHARACTERLIST:GetByName("Aoi")
if entry and not save:IsCharacterUnlocked(entry.FolderName) then
    local cond = entry.UnlockCondition
    if cond:IsUnlockable(0) and cond:GetCoinPrice() <= save.Coins then
        save:SpendCoins(cond:GetCoinPrice())
        save:UnlockCharacter(entry.FolderName)
    end
end
```

## Naamplaatjes en dan-titels

### NAMEPLATE

Tekent titelplaatjes, dan-plaatjes en complete spelernaamplaatjes.

<div class="callout warn">
De ROActivity nameplate van de skin (Modules/ROActivities/nameplate) doet het tekenen en bepaalt de vormgeving en lay-out. Dekking loopt van 0 tot 255. Tekstparameters nemen een textuur die door een tekstobject is gerenderd (zie Graphics en tekst); rarity is de index 0 Poor, 1 Common, 2 Uncommon, 3 Rare, 4 Epic, 5 Legendary, 6 Mythical.
</div>

| Methode | Beschrijving |
| --- | --- |
| `NAMEPLATE:DrawTitlePlate(x, y, opacity, type, text, rarity, nameplateId)  -> nil` | Tekent een titelplaatje met het gegeven weergavetype, de vooraf gerenderde titeltextuur, de zeldzaamheidsindex en het naamplaatje-id. |
| `NAMEPLATE:DrawDanPlate(x, y, opacity, danGrade, text)  -> nil` | Tekent een dan-plaatje voor de gegeven graad met een vooraf gerenderde titeltextuur. |
| `NAMEPLATE:DrawPlayerNameplate(x, y, opacity, player)  -> nil` | Tekent het complete naamplaatje van een spelerslot; de rode of blauwe kant volgt de 1P-kantinstelling van het spel. |
| `NAMEPLATE:DrawNameplateTitleById(id, x, y, opacity, font)  -> nil` | Rendert de gelokaliseerde titel van het naamplaatje met dit id met een tekstobject en tekent die als titelplaatje. Het id moet in de naamplaatjesdatabase bestaan. |

### NAMEPLATESLIST

De database van elk naamplaatje dat het spel kent, met opzoeken op index of id en filteren.

<div class="callout warn">
Zoekmethoden geven naamplaatje-info-handles terug. FindWhere roept een Lua-functie eenmaal per naamplaatje aan en behoudt de items waarvoor die true teruggeeft.
</div>

| Methode | Beschrijving |
| --- | --- |
| `NAMEPLATESLIST.Count  -> integer` | Aantal naamplaatjes in de database. |
| `NAMEPLATESLIST:GetByIndex(index)  -> nameplateInfo` | Het naamplaatje op een 0-gebaseerde databasepositie, of nil indien buiten bereik. |
| `NAMEPLATESLIST:GetById(id)  -> nameplateInfo` | Het naamplaatje met dit id, of nil indien niet gevonden. |
| `NAMEPLATESLIST:GetAll()  -> nameplateInfo[]` | Elk naamplaatje als lijst. |
| `NAMEPLATESLIST:FindWhere(predicate)  -> nameplateInfo[]` | De naamplaatjes waarvoor `predicate(info)` true teruggeeft. |

### Naamplaatje-info-handle

Eén naamplaatjestitel: gelokaliseerde tekst, weergavetype, id, zeldzaamheid en ontgrendelvoorwaarde.

<div class="callout warn">
sf.NameplateInfo en NAMEPLATESLIST geven deze handles terug. Het standaard beginnersnaamplaatje heeft id -1, zeldzaamheid "Common" en geen ontgrendelvoorwaarde.
</div>

| Methode | Beschrijving |
| --- | --- |
| `info.Title  -> string` | Titeltekst in de huidige taal. |
| `info.Type  -> integer` | Weergavetypecode die aan NAMEPLATE:DrawTitlePlate wordt doorgegeven. |
| `info.Id  -> integer` | Naamplaatje-id (-1 voor het standaard beginnersnaamplaatje). |
| `info.Rarity  -> string` | Zeldzaamheidsnaam: "Poor", "Common", "Uncommon", "Rare", "Epic", "Legendary" of "Mythical". |
| `info.UnlockCondition  -> unlockCondition` | De ontgrendelvoorwaarde (zie Ontgrendelvoorwaarde-handle). |

### Dan-plaatje-info-handle

De actieve dan-titel van de speler zoals getoond op het naamplaatje.

<div class="callout warn">
sf.DanplateInfo geeft deze handle terug. De waarden weerspiegelen het opslagbestand op het moment van lezen.
</div>

| Methode | Beschrijving |
| --- | --- |
| `info.Title  -> string` | Tekst van de actieve dan-titel. |
| `info.Gold  -> bool` | Of de speler de actieve titel met een gouden pass heeft verdiend. |
| `info.ClearStatus  -> integer` | Clearstatuscode van de actieve titel. |

### Dan-titel-item-handle

Eén dan-titel die de speler kan selecteren.

<div class="callout warn">
sf:GetDanTitleByIndex geeft deze items terug. Index 0 is de standaardtitel (niet goud, clearstatus 0); latere indices zijn titels die de speler heeft verdiend.
</div>

| Methode | Beschrijving |
| --- | --- |
| `entry.Title  -> string` | Titeltekst. |
| `entry.IsGold  -> bool` | Of de speler de titel met een gouden pass heeft verdiend. |
| `entry.ClearStatus  -> integer` | Beste clearstatus die voor de titel is geregistreerd. |

### Beste-dan-spelbeurt-handle

De beste examenresultaten van één dan-record.

<div class="callout warn">
sf:GetDanBestPlay geeft deze handle terug. Controleer HasRecord voordat je examens leest. GetExam geeft een .NET-array terug: indexeer vanaf 0 en lees `.Length`.
</div>

| Methode | Beschrijving |
| --- | --- |
| `play.HasRecord  -> bool` | Of er een record voor het nummer bestaat. |
| `play:GetExam(slot)  -> int[]` | Beste scores voor examenslot 1 tot 7: één waarde voor een cursusbreed examen, één per nummer voor examens per nummer. Leeg voor een ontbrekend record of een ongeldig slot. |

## Personages en puchichara's

### CHARACTER

Maakt personage-handles en stelt de namen van de standaard animatie- en stemslots beschikbaar.

<div class="callout warn">
CreateCharacter geeft een handle terug die zijn eigen resources bezit; controleer IsValid en roep Dispose aan wanneer je klaar bent. GetPlayerCharacter geeft een handle terug die het uitgeruste personage van de speler volgt en geen vrijgave nodig heeft. GetPlayerGradientMap geeft een gradiëntmap terug (zie Graphics en tekst). De leden ANIM_* en VOICE_* zijn alleen-lezen strings; geef ze door aan de animatie- en stemmethoden van de personage-handle.
</div>

| Methode | Beschrijving |
| --- | --- |
| `CHARACTER:CreateCharacter(folderName)  -> character` | Laadt een op zichzelf staand personage uit Global/Characters/{folderName}. IsValid is false als de map niet bestaat. |
| `CHARACTER:GetPlayerCharacter(player)  -> character` | Een handle gebonden aan een spelerslot die bij elke aanroep het uitgeruste personage oplost. |
| `CHARACTER:GetPlayerGradientMap(player)  -> gradientMap` | De paletgradiënt die actief is voor een spelerslot, of nil als er geen is gezet. |
| `CHARACTER.ANIM_PREVIEW  -> string` | Preview-pose (menu's en winkels). |
| `CHARACTER.ANIM_RENDER  -> string` | Volledige renderpose. |
| `CHARACTER.ANIM_GAME_NORMAL  -> string` | Gameplay, normale status. |
| `CHARACTER.ANIM_GAME_CLEAR  -> string` | Gameplay, gauge in de clear-zone. |
| `CHARACTER.ANIM_GAME_MAX  -> string` | Gameplay, gauge vol. |
| `CHARACTER.ANIM_GAME_GOGO  -> string` | Gameplay, go-go-time. |
| `CHARACTER.ANIM_GAME_GOGO_MAX  -> string` | Gameplay, go-go-time met een volle gauge. |
| `CHARACTER.ANIM_GAME_MISS  -> string` | Gameplay, mis. |
| `CHARACTER.ANIM_GAME_MISS_DOWN  -> string` | Gameplay, mis met een lage gauge. |
| `CHARACTER.ANIM_GAME_10COMBO  -> string` | Gameplay, mijlpaal van 10 combo. |
| `CHARACTER.ANIM_GAME_10COMBO_MAX  -> string` | Gameplay, mijlpaal van 10 combo met een volle gauge. |
| `CHARACTER.ANIM_GAME_CLEARED  -> string` | Gameplay, nummer gecleard. |
| `CHARACTER.ANIM_GAME_FAILED  -> string` | Gameplay, nummer mislukt. |
| `CHARACTER.ANIM_GAME_CLEAR_OUT  -> string` | Overgang uit de clear-status. |
| `CHARACTER.ANIM_GAME_CLEAR_IN  -> string` | Overgang naar de clear-status. |
| `CHARACTER.ANIM_GAME_MAX_OUT  -> string` | Overgang uit de volle-gaugestatus. |
| `CHARACTER.ANIM_GAME_MAX_IN  -> string` | Overgang naar de volle-gaugestatus. |
| `CHARACTER.ANIM_GAME_MISS_IN  -> string` | Overgang naar een mis. |
| `CHARACTER.ANIM_GAME_MISS_DOWN_IN  -> string` | Overgang naar een mis met lage gauge. |
| `CHARACTER.ANIM_GAME_RETURN  -> string` | Terugkeer naar de normale status. |
| `CHARACTER.ANIM_GAME_GOGOSTART  -> string` | Go-go-startuitbarsting. |
| `CHARACTER.ANIM_GAME_GOGOSTART_CLEAR  -> string` | Go-go-startuitbarsting in de clear-status. |
| `CHARACTER.ANIM_GAME_GOGOSTART_MAX  -> string` | Go-go-startuitbarsting met een volle gauge. |
| `CHARACTER.ANIM_GAME_BALLOON_BREAKING  -> string` | Ballon wordt geraakt. |
| `CHARACTER.ANIM_GAME_BALLOON_BROKE  -> string` | Ballon geknapt. |
| `CHARACTER.ANIM_GAME_BALLOON_MISS  -> string` | Ballon gemist. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_BREAKING  -> string` | Kusudama wordt geraakt. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_BROKE  -> string` | Kusudama gebroken. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_MISS  -> string` | Kusudama gemist. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_IDLE  -> string` | Kusudama inactief. |
| `CHARACTER.ANIM_GAME_TOWER_STANDING  -> string` | Tower-modus, staand. |
| `CHARACTER.ANIM_GAME_TOWER_STANDING_TIRED  -> string` | Tower-modus, staand terwijl moe. |
| `CHARACTER.ANIM_GAME_TOWER_CLIMBING  -> string` | Tower-modus, klimmend. |
| `CHARACTER.ANIM_GAME_TOWER_CLIMBING_TIRED  -> string` | Tower-modus, klimmend terwijl moe. |
| `CHARACTER.ANIM_GAME_TOWER_RUNNING  -> string` | Tower-modus, rennend. |
| `CHARACTER.ANIM_GAME_TOWER_RUNNING_TIRED  -> string` | Tower-modus, rennend terwijl moe. |
| `CHARACTER.ANIM_GAME_TOWER_CLEAR  -> string` | Tower-modus, clear. |
| `CHARACTER.ANIM_GAME_TOWER_CLEAR_TIRED  -> string` | Tower-modus, clear terwijl moe. |
| `CHARACTER.ANIM_GAME_TOWER_FAIL  -> string` | Tower-modus, mislukt. |
| `CHARACTER.ANIM_MENU_WAIT  -> string` | Menu, wachtend. |
| `CHARACTER.ANIM_MENU_START  -> string` | Menu, start. |
| `CHARACTER.ANIM_MENU_NORMAL  -> string` | Menu, normaal. |
| `CHARACTER.ANIM_MENU_SELECT  -> string` | Menu, selectie. |
| `CHARACTER.ANIM_ENTRY_NORMAL  -> string` | Entryscherm, normaal. |
| `CHARACTER.ANIM_ENTRY_JUMP  -> string` | Entryscherm, sprong. |
| `CHARACTER.ANIM_RESULT_NORMAL  -> string` | Resultaten, normaal. |
| `CHARACTER.ANIM_RESULT_CLEAR  -> string` | Resultaten, clear. |
| `CHARACTER.ANIM_RESULT_FAILED_IN  -> string` | Resultaten, overgang naar de mislukt-status. |
| `CHARACTER.ANIM_RESULT_FAILED  -> string` | Resultaten, mislukt. |
| `CHARACTER.VOICE_END_FAILED  -> string` | Einde nummer, mislukt. |
| `CHARACTER.VOICE_END_CLEAR  -> string` | Einde nummer, gecleard. |
| `CHARACTER.VOICE_END_FULLCOMBO  -> string` | Einde nummer, full combo. |
| `CHARACTER.VOICE_END_ALLPERFECT  -> string` | Einde nummer, all perfect. |
| `CHARACTER.VOICE_END_AIBATTLE_WIN  -> string` | Einde nummer, AI-gevecht gewonnen. |
| `CHARACTER.VOICE_END_AIBATTLE_LOSE  -> string` | Einde nummer, AI-gevecht verloren. |
| `CHARACTER.VOICE_MENU_SONGSELECT  -> string` | Nummerselectie betreden. |
| `CHARACTER.VOICE_MENU_SONGDECIDE  -> string` | Nummer bevestigd. |
| `CHARACTER.VOICE_MENU_SONGDECIDE_AI  -> string` | Nummer bevestigd in AI-gevecht. |
| `CHARACTER.VOICE_MENU_DIFFSELECT  -> string` | Moeilijkheidsselectie. |
| `CHARACTER.VOICE_MENU_DANSELECTSTART  -> string` | Dan-selectie betreden. |
| `CHARACTER.VOICE_MENU_DANSELECTPROMPT  -> string` | Dan-selectieprompt. |
| `CHARACTER.VOICE_MENU_DANSELECTCONFIRM  -> string` | Dan-cursus bevestigd. |
| `CHARACTER.VOICE_TITLE_SANKA  -> string` | Titelscherm betreden. |
| `CHARACTER.VOICE_TOWER_MISS  -> string` | Tower-modus mis. |
| `CHARACTER.VOICE_RESULT_BESTSCORE  -> string` | Resultaten, nieuwe beste score. |
| `CHARACTER.VOICE_RESULT_CLEARFAILED  -> string` | Resultaten, mislukt. |
| `CHARACTER.VOICE_RESULT_CLEARSUCCESS  -> string` | Resultaten, gecleard. |
| `CHARACTER.VOICE_RESULT_DANFAILED  -> string` | Resultaten, dan mislukt. |
| `CHARACTER.VOICE_RESULT_DANREDPASS  -> string` | Resultaten, dan geslaagd. |
| `CHARACTER.VOICE_RESULT_DANGOLDPASS  -> string` | Resultaten, dan geslaagd met goud. |

### Personage-handle

Een tekenbaar personage: speelt benoemde animaties en stemmen af en draagt tekenstatus per handle (dekking, schaal, tint, rotatie, blend- en wrapmodus, paletgradiënt).

<div class="callout warn">
CHARACTER:GetPlayerCharacter, CHARACTER:CreateCharacter, sf:GetCharacter en de eigenschap Character van een personagelijstitem geven personage-handles terug. Alleen handles van CreateCharacter bezitten hun resources en hebben Dispose nodig. De handle slaat Set*-waarden op en past ze bij elke volgende tekenaanroep toe; de schaal- en dekkingsargumenten van de tekenmethoden worden met de opgeslagen waarden vermenigvuldigd. Opgeslagen dekking loopt van 0.0 tot 1.0, dekking per tekenaanroep van 0 tot 255. Animatie- en stemnamen zijn de CHARACTER-constanten.
</div>

| Methode | Beschrijving |
| --- | --- |
| `char.IsValid  -> bool` | Of de handle naar een geladen personage verwijst. |
| `char.FolderName  -> string` | Mapnaam, of een lege string indien ongeldig. |
| `char.FullPath  -> string` | Absoluut mappad, of een lege string indien ongeldig. |
| `char.DisplayName  -> string` | Gelokaliseerde weergavenaam, met de mapnaam als terugval. |
| `char:SetPaletteGradient(stops, blend?)  -> nil` | Past een paletgradiënt toe die is opgebouwd uit een tabel van minstens twee kleurstops, met een optionele blendhoeveelheid (standaard 1.0). Aan spelers gebonden handles slaan de gradiënt ook op het spelerslot op. nil doorgeven wist hem. |
| `char:ClearPaletteGradient()  -> nil` | Verwijdert de paletgradiënt (en de gradiënt van het spelerslot voor aan spelers gebonden handles). |
| `char:SetOpacity(opacity)  -> nil` | Opgeslagen dekking, 0.0 transparant tot 1.0 ondoorzichtig. |
| `char:SetScale(scaleX, scaleY)  -> nil` | Opgeslagen schaal; een negatieve X spiegelt horizontaal. |
| `char:SetColor(color)  -> nil` | Opgeslagen tint vanuit een kleurwaarde. |
| `char:SetColor(r, g, b)  -> nil` | Opgeslagen tint vanuit drie kanalen 0.0 tot 1.0. |
| `char:SetRotation(degrees)  -> nil` | Opgeslagen rotatie in graden. |
| `char:SetBlendMode(mode)  -> nil` | Opgeslagen blendmodus: "normal", "add", "multi", "sub" of "screen". |
| `char:SetWrapMode(mode)  -> nil` | Opgeslagen textuurwrapmodus: "edge", "border", "repeat" of "mirror". |
| `char:GetScale()  -> vector2` | Opgeslagen schaal. |
| `char:GetColor()  -> tuple` | Opgeslagen tint als .NET-tuple met de velden Item1, Item2 en Item3 (rood, groen, blauw). |
| `char:GetRotation()  -> number` | Opgeslagen rotatie in graden. |
| `char:GetBlendMode()  -> string` | Opgeslagen blendmodus. |
| `char:GetWrapMode()  -> string` | Opgeslagen wrapmodus. |
| `char:Draw(x, y, animation, scaleX?, scaleY?, opacity?)  -> nil` | Tekent de animatie op x, y. Standaardwaarden: schaal 1, dekking 255. |
| `char:DrawAtAnchor(x, y, animation, anchor?, scaleX?, scaleY?, opacity?)  -> nil` | Tekent de animatie met het benoemde ankerpunt (standaard "bottom") op x, y geplaatst. |
| `char:DrawRect(x, y, w, h, animation, scaleX?, scaleY?, opacity?)  -> nil` | Tekent de animatie op de linkerbovenhoek van de rechthoek. De methode accepteert w en h voor lay-outcode, maar ze beïnvloeden het tekenen niet. |
| `char:DrawRectAtAnchor(x, y, clipW, clipH, animation, opacity?, clipX?, clipY?)  -> nil` | Tekent de animatie met de linkerbovenhoek op x, y, geclipt op een rechthoek van clipW bij clipH, verschoven met clipX, clipY. Schaal, tint en rotatie komen uitsluitend uit de opgeslagen status. |
| `char:Update(animation, looping?)  -> bool` | Laat de animatie vorderen (standaard herhalend) en geeft terug of ze nog speelt. |
| `char:LoadAnimation(animation)  -> nil` | Laadt de frames van de animatie. |
| `char:DisposeAnimation(animation)  -> nil` | Geeft de frames van de animatie vrij. |
| `char:AvailableAnimation(animation)  -> bool` | Of het personage de animatie aanbiedt. |
| `char:SetAnimationDuration(animation, duration)  -> nil` | Stelt de afspeelduur van de animatie in. |
| `char:SetAnimationCyclesFromBPM(animation, bpm)  -> nil` | Stelt de cycluslengte van de animatie in op basis van een BPM. |
| `char:ResetAnimationCounter(animation)  -> nil` | Herstart de animatie vanaf het eerste frame. |
| `char:GetAnimationSize(animation)  -> vector2` | Getekende afmeting van het huidige frame van de animatie op skinresolutie, of (0, 0) indien niet beschikbaar. |
| `char:LoadVoice(voice)  -> nil` | Laadt een stemfragment. |
| `char:DisposeVoice(voice)  -> nil` | Geeft een stemfragment vrij. |
| `char:PlayVoice(voice)  -> nil` | Speelt een stemfragment af. |
| `char:Dispose()  -> nil` | Geeft de resources van het personage vrij (alleen handles van CreateCharacter). |

```lua
local chara = CHARACTER:GetPlayerCharacter(0)
chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL)

function update()
    chara:Update(CHARACTER.ANIM_MENU_NORMAL)
end

function draw()
    chara:DrawAtAnchor(960, 1000, CHARACTER.ANIM_MENU_NORMAL, "bottom")
end
```

### CHARACTERLIST

De lijst van elk geladen personage.

<div class="callout warn">
De skin bouwt de lijst opnieuw op wanneer hij zijn personages laadt en geeft hem vrij bij het herladen van de skin, dus de global kan nil zijn terwijl er geen personages zijn geladen. Zoekmethoden geven personagelijstitems terug.
</div>

| Methode | Beschrijving |
| --- | --- |
| `CHARACTERLIST.Count  -> integer` | Aantal geladen personages. |
| `CHARACTERLIST:GetAll()  -> characterEntry[]` | Elk personage als lijst. |
| `CHARACTERLIST:GetByIndex(index)  -> characterEntry` | Het item op een 0-gebaseerde index, of nil indien buiten bereik. |
| `CHARACTERLIST:GetByName(folderName)  -> characterEntry` | Het item met deze mapnaam, of nil indien niet gevonden. |

### Personagelijstitem

Eén CHARACTERLIST-item: mapnaam, weergavenaam, zeldzaamheid, een personage-handle en de ontgrendelvoorwaarde.

<div class="callout warn">
De lijst is eigenaar van de gedeelde handle in de eigenschap Character; geef hem niet vrij. Laad er animaties op voordat je tekent.
</div>

| Methode | Beschrijving |
| --- | --- |
| `entry.FolderName  -> string` | Mapnaam; opslagbestanden gebruiken die als sleutel. |
| `entry.DisplayName  -> string` | Gelokaliseerde weergavenaam. |
| `entry.Rarity  -> string` | Zeldzaamheidsnaam (zie Naamplaatje-info-handle voor de lijst). |
| `entry.Character  -> character` | Personage-handle voor dit item. |
| `entry.UnlockCondition  -> unlockCondition` | De ontgrendelvoorwaarde (zie Ontgrendelvoorwaarde-handle). |

### PUCHICHARALIST

De lijst van elke geladen puchichara, plus de huidige selectie van elke speler.

<div class="callout warn">
De skin bouwt de lijst opnieuw op wanneer hij zijn puchichara-texturen laadt en geeft hem vrij bij het herladen van de skin, dus de global kan nil zijn terwijl ze niet zijn geladen. Zoekmethoden geven puchichara-handles terug.
</div>

| Methode | Beschrijving |
| --- | --- |
| `PUCHICHARALIST.Count  -> integer` | Aantal geladen puchichara's. |
| `PUCHICHARALIST:GetAll()  -> puchichara[]` | Elke puchichara als lijst. |
| `PUCHICHARALIST:GetByIndex(index)  -> puchichara` | De puchichara op een 0-gebaseerde index, of nil indien buiten bereik. |
| `PUCHICHARALIST:GetByName(folderName)  -> puchichara` | De puchichara met deze mapnaam, of nil indien niet gevonden. |
| `PUCHICHARALIST:GetPlayerPuchichara(player)  -> puchichara` | De puchichara die door een spelerslot is uitgerust, of nil als die niet kan worden opgelost. |

### Puchichara-handle

Eén puchichara: zijn texturen, gelokaliseerde naam en auteur, zeldzaamheid, mapnaam en ontgrendelvoorwaarde.

<div class="callout warn">
PUCHICHARALIST en sf:GetPuchichara geven deze handles terug. De lijst is eigenaar van de texturen; geef ze niet vrij. Een ontbrekende afbeelding levert een lege textuur op.
</div>

| Methode | Beschrijving |
| --- | --- |
| `puchi.tx  -> texture` | Spritesheet geladen uit Chara.png. |
| `puchi.render  -> texture` | Volledige render geladen uit Render.png. |
| `puchi.Name  -> string` | Gelokaliseerde weergavenaam. |
| `puchi.Author  -> string` | Gelokaliseerde auteursnaam. |
| `puchi.Rarity  -> string` | Zeldzaamheidsnaam (zie Naamplaatje-info-handle voor de lijst). |
| `puchi.FolderName  -> string` | Mapnaam; opslagbestanden gebruiken die als sleutel. |
| `puchi.UnlockCondition  -> unlockCondition` | De ontgrendelvoorwaarde (zie Ontgrendelvoorwaarde-handle). |
| `puchi:GetUnlockMessage()  -> string` | Verkorte vorm van `puchi.UnlockCondition:GetConditionMessage()`. |

## Spelstatus en ontgrendelingen

### PLAYSTATE

Live resultaten van de huidige of meest recente spelbeurt: beoordelingsaantallen, score, combo, clearcontroles, en Tower- en Dan-status.

<div class="callout warn">
De waarden komen van het gameplayscherm, dus ze zijn betekenisvol tijdens een spelbeurt en op de schermen die erop volgen. Spelerindices beginnen bij 0; de methoden controleren ze niet op bereik. De dan-controles evalueren altijd speler 0.
</div>

| Methode | Beschrijving |
| --- | --- |
| `PLAYSTATE.LastRegisteredFloor  -> integer` | Tower-modus: de laatst bereikte verdieping. |
| `PLAYSTATE.MaxNumberOfLives  -> integer` | Tower-modus: het maximale aantal levens. |
| `PLAYSTATE.CurrentNumberOfLives  -> integer` | Tower-modus: het huidige aantal levens. |
| `PLAYSTATE.InvincibilityDurationSpeedDependent  -> number` | Tower-modus: de onkwetsbaarheidsduur, aangepast aan de nummersnelheid. |
| `PLAYSTATE.InvincibilityDuration  -> integer` | Tower-modus: de basisonkwetsbaarheidsduur. |
| `PLAYSTATE:WasPlayEndedNormally()  -> bool` | Of de vorige spelbeurt tot het einde is uitgespeeld. |
| `PLAYSTATE:WasPlayAborted()  -> bool` | Of de speler de vorige spelbeurt voortijdig heeft afgebroken. |
| `PLAYSTATE:GetGoodCount(player)  -> integer` | Aantal Good-beoordelingen. |
| `PLAYSTATE:GetOkCount(player)  -> integer` | Aantal Ok-beoordelingen. |
| `PLAYSTATE:GetBadCount(player)  -> integer` | Aantal Bad-beoordelingen. |
| `PLAYSTATE:GetRollCount(player)  -> integer` | Aantal roffelslagen. |
| `PLAYSTATE:GetADLibCount(player)  -> integer` | Aantal geraakte ADLib-noten. |
| `PLAYSTATE:GetMissedADLibCount(player)  -> integer` | Aantal gemiste ADLib-noten. |
| `PLAYSTATE:GetBoomCount(player)  -> integer` | Aantal geraakte mijnnoten. |
| `PLAYSTATE:GetAvoidedBoomCount(player)  -> integer` | Aantal ontweken mijnnoten. |
| `PLAYSTATE:GetScore(player)  -> integer` | Huidige score. |
| `PLAYSTATE:GetCombo(player)  -> integer` | Huidige combo. |
| `PLAYSTATE:GetHighestCombo(player)  -> integer` | Hoogste bereikte combo. |
| `PLAYSTATE:IsClear(player)  -> bool` | Of de gauge de clearlijn haalt. |
| `PLAYSTATE:IsAssistedClear(player)  -> bool` | Of de spelbeurt een clear is terwijl een scoreverlagende mod actief is. |
| `PLAYSTATE:IsFullCombo(player)  -> bool` | Clear, niet assisted, zonder Bad-beoordelingen en zonder geraakte mijnen. |
| `PLAYSTATE:IsPerfect(player)  -> bool` | Full combo zonder Ok-beoordelingen. |
| `PLAYSTATE:IsAlive()  -> bool` | Tower-modus: of er nog levens over zijn. |
| `PLAYSTATE:IsPass()  -> bool` | Dan-modus: of de examenstatus geen mislukking is. |
| `PLAYSTATE:IsRedPass()  -> bool` | Dan-modus: of de examenstatus een standaard pass is. |
| `PLAYSTATE:IsGoldPass()  -> bool` | Dan-modus: of de examenstatus een gouden pass is. |
| `PLAYSTATE:IsDanClear()  -> bool` | Dan-modus: geslaagd en niet assisted. |
| `PLAYSTATE:IsDanFullCombo()  -> bool` | Dan-modus: dan-clear zonder Bad-beoordelingen en zonder geraakte mijnen. |
| `PLAYSTATE:IsDanPerfect()  -> bool` | Dan-modus: dan-full-combo zonder Ok-beoordelingen. |

### Ontgrendelvoorwaarde-handle

De ontgrendelvereiste van een naamplaatje, personage of puchichara.

<div class="callout warn">
De eigenschap UnlockCondition van naamplaatje-info-handles, personagelijstitems en puchichara-handles geeft deze handle terug. Een item zonder voorwaarde (HasCondition false) is standaard beschikbaar: IsUnlockable geeft true terug en de berichten zijn leeg. Het voorwaardenvocabulaire komt overeen met Unlock.json en ontgrendelvoorwaarden voor charts; zie de handleiding <a href="../guides/unlockables.md">Ontgrendelvoorwaarden voor charts</a>.
</div>

| Methode | Beschrijving |
| --- | --- |
| `cond.HasCondition  -> bool` | Of het item een ontgrendelvoorwaarde heeft. |
| `cond:GetConditionType()  -> string` | Het id van het voorwaardetype (bijvoorbeeld "ch", "cs", "gt", "gc" of "ig"), of een lege string. |
| `cond:GetCoinPrice()  -> integer` | Muntprijs van de voorwaarde, of 0. |
| `cond:GetConditionMessage()  -> string` | Gelokaliseerde beschrijving van de voorwaarde. |
| `cond:IsUnlockable(player)  -> bool` | Of de speler momenteel aan de voorwaarde voldoet. |
| `cond:GetBlockedMessage(player)  -> string` | Waarom de speler niet aan de voorwaarde voldoet, of een lege string wanneer eraan is voldaan. |

## Thema en taal

### THEME

De resolutie van de skin, thema-instellingen, gelokaliseerde strings op skinniveau en de definities van thema-instellingen.

<div class="callout warn">
De skin declareert thema-instellingen in ThemeSettings.json en slaat hun waarden op in de ThemeSettings.db3 ernaast. De getters geven instellingswaarden altijd als strings terug; een ontbrekende instelling geeft haar gedeclareerde standaardwaarde terug, of een lege string als er geen declaratie bestaat. GetThemeSettingForPlayer neemt een 1-gebaseerd spelernummer. Definitie-indices beginnen bij 0.
</div>

| Methode | Beschrijving |
| --- | --- |
| `THEME:GetResolution()  -> vector2` | De resolutie van de skin. |
| `THEME:GetThemeSetting(settingId)  -> string` | Waarde van een instelling met globale scope. |
| `THEME:GetThemeSettingForPlayer(settingId, player)  -> string` | Waarde van een instelling met save-scope voor de 1-gebaseerde speler, of haar standaardwaarde als het opslagbestand geen waarde heeft. |
| `THEME:GetSkinString(key)  -> string` | Gelokaliseerde string uit de map Locales van de skin: eerst de huidige taal, dan de standaardlocale van de skin, dan `[LOCALE NOT FOUND: key]`. |
| `THEME:GetDefinitionCount()  -> integer` | Aantal instellingsdefinities in ThemeSettings.json. |
| `THEME:GetDefinitionId(index)  -> string` | Id van de definitie op een 0-gebaseerde index, of een lege string. |
| `THEME:GetDefinitionScope(index)  -> string` | Scope van de definitie: "global" of "save". |
| `THEME:GetDefinitionType(index)  -> string` | Type van de definitie: "bool", "int", "double", "string" of "enum". |

### LANG

Gelokaliseerde spelstrings, taalwisseling en meertalige tekstwaarden.

<div class="callout warn">
GetString formatteert het item met eventuele extra argumenten. GetLanguageIds en GetLanguageNames geven .NET-arrays terug (vanaf 0, `.Length`); GetAvailableLanguages geeft een dictionary terug om te doorlopen met `:GetEnumerator()` (zie Data en persistentie). FromDict neemt een geparseerd JSON-object van JSONLOADER (het accepteert geen Lua-tabel); AsLocalizationData neemt een JsonNode van JSONLOADER:LoadJson.
</div>

| Methode | Beschrijving |
| --- | --- |
| `LANG:GetString(key, ...)  -> string` | De gelokaliseerde string voor een sleutel, met formatplaceholders ingevuld vanuit de extra argumenten. |
| `LANG:ChangeLanguage(id)  -> bool` | Wisselt de actieve taal als het id bestaat en verschilt van de huidige, en roept daarna `reloadLanguage` aan op elk geladen script; geeft terug of er is gewisseld. Het laat CONFIG.Language ongewijzigd. |
| `LANG:GetLanguageIds()  -> string[]` | Id's van de beschikbare talen. |
| `LANG:GetLanguageNames()  -> string[]` | Weergavenamen van de beschikbare talen, in dezelfde volgorde. |
| `LANG:GetAvailableLanguages()  -> dict` | Taal-id naar weergavenaam. |
| `LANG:GetExamName(type)  -> string` | Gelokaliseerde naam van een dan-examentype. |
| `LANG:AsLocalizationData(node)  -> localizationData` | Bouwt een lokalisatiewaarde uit een JsonNode van de vorm `{ "strings": { "<lang>": "text" } }`. |
| `LANG:FromDict(dict)  -> localizationData` | Bouwt een lokalisatiewaarde uit een geparseerd JSON-object dat taal-id's aan tekst koppelt. |
| `LANG:FromString(json)  -> localizationData` | Bouwt een lokalisatiewaarde uit een JSON-objectstring die taal-id's aan tekst koppelt; een lege waarde als de string niet parseert. |

```lua
local langs = LANG:GetAvailableLanguages()
local e = langs:GetEnumerator()
while e:MoveNext() do
    print(e.Current.Key, e.Current.Value)
end

local name = LANG:FromString('{"ja":"太鼓","default":"Taiko"}'):GetString("")
```

### Lokalisatiedata-handle

Een set strings met taal-id's als sleutel, die naar de huidige taal wordt opgelost.

<div class="callout warn">
LANG:AsLocalizationData, LANG:FromDict en LANG:FromString geven deze handle terug. Oplossingsvolgorde: het huidige taal-id, dan de sleutel "default", dan de terugvalwaarde die aan GetString is doorgegeven.
</div>

| Methode | Beschrijving |
| --- | --- |
| `loc:GetString(fallback)  -> string` | De tekst voor de huidige taal, of "default", of de terugvalwaarde. |
| `loc:SetString(langId, text)  -> nil` | Stelt de tekst voor een taal-id in. |
| `loc:GetAllStrings()  -> string[]` | Elke opgeslagen tekst, in willekeurige volgorde. |
