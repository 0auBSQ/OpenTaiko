<!-- api/players.md -->

# Spieler und Profile

Spielstände, Namensschilder, Charaktere, Puchicharas, Spielzustand, Themes und die aktuelle Sprache.

Spielerindizes sind überall auf dieser Seite 0-basiert (0 bis 4), mit Ausnahme von THEME:GetThemeSettingForPlayer, das 1-basiert ist. Schreibgeschützte Module (ROActivities und Hintergründe) erhalten ein Spielstand-Handle, dessen Schreibmethoden einen Fehler protokollieren und nichts tun; alles andere hier verhält sich in jedem Modultyp gleich.

## Spielstände

### GetSaveFile

Globale Funktion, die das Spielstand-Handle eines Spielerslots zurückgibt.

<div class="callout warn">
Rufen Sie sie als einfache Funktion auf (`GetSaveFile(0)`). Ein Index außerhalb des Bereichs protokolliert einen Fehler und gibt nil zurück. Jeder Aufruf erzeugt ein frisches Handle, das Live-Daten liest, sodass es nichts zwischenzuspeichern oder freizugeben gibt.
</div>

| Methode | Beschreibung |
| --- | --- |
| `GetSaveFile(player)  -> saveFile` | Gibt das Spielstand-Handle für den 0-basierten Spielerslot zurück, oder nil, wenn der Index außerhalb des Bereichs liegt. |

### Spielstand-Handle

Das Profil eines Spielers: Name, Münzen, freigeschaltete Gegenstände, Trigger und Zähler, Clear-Statistiken, ausgerüsteter Charakter, Puchichara, Namensschild und Dan-Titel.

<div class="callout warn">
Lesen Sie Eigenschaften mit Punktsyntax (sf.Name, sf.Coins). Schreibmethoden speichern sofort. Schreibgeschützte Module blockieren die folgenden: SpendCoins, EarnCoins, UnlockNameplate, UnlockSong, das Zuweisen von SelectedHitsounds, SetGlobalTrigger, SetGlobalCounter, ChangeCharacter (gibt false zurück), UnlockPuchichara, ChangePuchichara, UnlockCharacter, ChangeDan, ChangeName und ChangeNameplate.
</div>

| Methode | Beschreibung |
| --- | --- |
| `sf.Name  -> string` | Der angezeigte Name des Spielers. |
| `sf.SaveId  -> integer` | Die numerische Datenbank-ID dieses Spielstands. |
| `sf.SaveUID  -> string` | Die eindeutige String-ID dieses Spielstands. |
| `sf.NameplateInfo  -> nameplateInfo` | Das ausgerüstete Namensschild (siehe Namensschild-Info-Handle), oder das Standard-Anfängernamensschild, wenn die gespeicherte ID unbekannt ist. |
| `sf.DanplateInfo  -> danplateInfo` | Der aktuelle Dan-Titel (siehe Dan-Schild-Info-Handle). |
| `sf.TotalPlaycount  -> integer` | Gesamtzahl der Spiele auf diesem Spielstand. |
| `sf.AIBattlePlaycount  -> integer` | Anzahl der KI-Kampf-Spiele. |
| `sf.AIBattleWins  -> integer` | Anzahl der KI-Kampf-Siege. |
| `sf.Coins  -> integer` | Aktueller Münzstand. |
| `sf.TotalEarnedCoins  -> integer` | Insgesamt über die Lebensdauer des Spielstands verdiente Münzen. |
| `sf:SpendCoins(price)  -> nil` | Zieht Münzen ab (der Stand fällt nie unter 0) und speichert. |
| `sf:EarnCoins(amount)  -> nil` | Fügt dem Stand und der Gesamtsumme Münzen hinzu und speichert. |
| `sf:IsNameplateUnlocked(id)  -> bool` | Ob das Namensschild mit dieser ID freigeschaltet ist. |
| `sf:UnlockNameplate(id)  -> nil` | Schaltet ein Namensschild frei und speichert (No-op, wenn bereits freigeschaltet). |
| `sf:IsSongUnlocked(uniqueId)  -> bool` | Ob der Song mit dieser eindeutigen ID freigeschaltet ist. |
| `sf:UnlockSong(uniqueId)  -> nil` | Schaltet einen Song frei und speichert (No-op, wenn bereits freigeschaltet). |
| `sf.SelectedHitsounds  -> string` | Ordnername des ausgewählten Hitsound-Sets. Das Zuweisen eines anderen Namens speichert ihn und lädt die Hitsounds des Spielers neu. |
| `sf:GetGlobalTrigger(name)  -> bool` | Liest einen benannten booleschen Trigger. |
| `sf:GetGlobalCounter(name)  -> number` | Liest einen benannten numerischen Zähler. |
| `sf:SetGlobalTrigger(name, value)  -> nil` | Setzt einen benannten booleschen Trigger. |
| `sf:SetGlobalCounter(name, value)  -> nil` | Setzt einen benannten numerischen Zähler. |
| `sf:GetClearStatusCount(difficulty, clearStatus)  -> integer` | Anzahl der Charts eines Schwierigkeitsgrads (0 Easy bis 4 Extra Extreme), deren bester Clear-Status genau clearStatus ist (0 keiner, 1 assistiert, 2 Clear, 3 Full Combo, 4 Perfect). 0 bei Argumenten außerhalb des Bereichs. |
| `sf:GetDanBestPlay(node)  -> danBestPlay` | Die Bestleistung ohne Mods für einen Dan-Song-Knoten (siehe Dan-Bestleistungs-Handle); ein Handle mit HasRecord false, wenn keine existiert. |
| `sf:GetCharacter()  -> character` | Das spielergebundene Charakter-Handle für diesen Slot (siehe Charakter-Handle). |
| `sf.CharacterName  -> string` | Ordnername des ausgerüsteten Charakters. |
| `sf:ChangeCharacter(folderName)  -> bool` | Rüstet den Charakter mit diesem Ordnernamen aus. Gibt true zurück, wenn der Charakter jetzt ausgerüstet ist oder bereits aktiv war, false, wenn kein geladener Charakter diesen Ordnernamen hat. |
| `sf:GetPuchichara()  -> puchichara` | Der ausgerüstete Puchichara (siehe Puchichara-Handle), oder nil, wenn er nicht aufgelöst werden kann. |
| `sf:IsPuchicharaUnlocked(folderName)  -> bool` | Ob der Puchichara mit diesem Ordnernamen freigeschaltet ist. |
| `sf:UnlockPuchichara(folderName)  -> nil` | Schaltet einen Puchichara frei und speichert (No-op, wenn bereits freigeschaltet). |
| `sf:ChangePuchichara(folderName)  -> nil` | Rüstet den Puchichara mit diesem Ordnernamen aus und speichert. Die Methode validiert den Namen nicht. |
| `sf:IsCharacterUnlocked(folderName)  -> bool` | Ob der Charakter freigeschaltet ist. Der ausgerüstete Charakter zählt immer als freigeschaltet. |
| `sf:UnlockCharacter(folderName)  -> nil` | Schaltet einen Charakter frei und speichert (No-op, wenn bereits freigeschaltet). |
| `sf.DanTitleCount  -> integer` | Anzahl der verfügbaren Dan-Titel, einschließlich des Standardtitels (immer mindestens 1). |
| `sf:GetDanTitleByIndex(index)  -> danTitleEntry` | Der Dan-Titel an einem 0-basierten Index (siehe Dan-Titel-Eintrag-Handle). Index 0 ist der Standardtitel; nil außerhalb des Bereichs. |
| `sf.SelectedDan  -> string` | Text des aktiven Dan-Titels. |
| `sf:ChangeDan(title)  -> nil` | Macht den angegebenen Titel aktiv, übernimmt dessen Gold- und Clear-Status-Flags, wenn es ein vom Spieler verdienter ist, aktualisiert das Namensschild und speichert. |
| `sf:ChangeName(name)  -> nil` | Ändert den angezeigten Namen, aktualisiert das Namensschild und speichert. Die Methode ignoriert leere oder unveränderte Namen. |
| `sf:ChangeNameplate(id)  -> nil` | Rüstet das Namensschild mit dieser ID aus, aktualisiert das Namensschild und speichert. Eine in der Datenbank fehlende ID leert den zwischengespeicherten Titeltext. |

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

## Namensschilder und Dan-Titel

### NAMEPLATE

Zeichnet Titelschilder, Dan-Schilder und vollständige Spieler-Namensschilder.

<div class="callout warn">
Die ROActivity nameplate des Skins (Modules/ROActivities/nameplate) übernimmt das Zeichnen und definiert Grafik und Layout. Die Deckkraft ist 0 bis 255. Textparameter nehmen eine aus einem Textobjekt gerenderte Textur (siehe Grafik und Text); rarity ist der Index 0 Poor, 1 Common, 2 Uncommon, 3 Rare, 4 Epic, 5 Legendary, 6 Mythical.
</div>

| Methode | Beschreibung |
| --- | --- |
| `NAMEPLATE:DrawTitlePlate(x, y, opacity, type, text, rarity, nameplateId)  -> nil` | Zeichnet ein Titelschild mit dem angegebenen Anzeigetyp, der vorgerenderten Titeltextur, dem Seltenheitsindex und der Namensschild-ID. |
| `NAMEPLATE:DrawDanPlate(x, y, opacity, danGrade, text)  -> nil` | Zeichnet ein Dan-Schild für den angegebenen Grad mit einer vorgerenderten Titeltextur. |
| `NAMEPLATE:DrawPlayerNameplate(x, y, opacity, player)  -> nil` | Zeichnet das vollständige Namensschild eines Spielerslots; die rote oder blaue Seite folgt der 1P-Seiteneinstellung des Spiels. |
| `NAMEPLATE:DrawNameplateTitleById(id, x, y, opacity, font)  -> nil` | Rendert den lokalisierten Titel des Namensschilds mit dieser ID mit einem Textobjekt und zeichnet ihn als Titelschild. Die ID muss in der Namensschild-Datenbank existieren. |

### NAMEPLATESLIST

Die Datenbank aller Namensschilder, die das Spiel kennt, mit Abfragen per Index oder ID und Filterung.

<div class="callout warn">
Abfragemethoden geben Namensschild-Info-Handles zurück. FindWhere ruft eine Lua-Funktion einmal pro Namensschild auf und behält die Einträge, für die sie true zurückgibt.
</div>

| Methode | Beschreibung |
| --- | --- |
| `NAMEPLATESLIST.Count  -> integer` | Anzahl der Namensschilder in der Datenbank. |
| `NAMEPLATESLIST:GetByIndex(index)  -> nameplateInfo` | Das Namensschild an einer 0-basierten Datenbankposition, oder nil außerhalb des Bereichs. |
| `NAMEPLATESLIST:GetById(id)  -> nameplateInfo` | Das Namensschild mit dieser ID, oder nil, wenn nicht gefunden. |
| `NAMEPLATESLIST:GetAll()  -> nameplateInfo[]` | Jedes Namensschild als Liste. |
| `NAMEPLATESLIST:FindWhere(predicate)  -> nameplateInfo[]` | Die Namensschilder, für die `predicate(info)` true zurückgibt. |

### Namensschild-Info-Handle

Ein Namensschildtitel: lokalisierter Text, Anzeigetyp, ID, Seltenheit und Freischaltbedingung.

<div class="callout warn">
sf.NameplateInfo und NAMEPLATESLIST geben diese Handles zurück. Das Standard-Anfängernamensschild hat die ID -1, die Seltenheit "Common" und keine Freischaltbedingung.
</div>

| Methode | Beschreibung |
| --- | --- |
| `info.Title  -> string` | Titeltext in der aktuellen Sprache. |
| `info.Type  -> integer` | Anzeigetyp-Code, der an NAMEPLATE:DrawTitlePlate übergeben wird. |
| `info.Id  -> integer` | Namensschild-ID (-1 für das Standard-Anfängernamensschild). |
| `info.Rarity  -> string` | Seltenheitsname: "Poor", "Common", "Uncommon", "Rare", "Epic", "Legendary" oder "Mythical". |
| `info.UnlockCondition  -> unlockCondition` | Die Freischaltbedingung (siehe Freischaltbedingungs-Handle). |

### Dan-Schild-Info-Handle

Der aktive Dan-Titel des Spielers, wie er auf dem Namensschild angezeigt wird.

<div class="callout warn">
sf.DanplateInfo gibt dieses Handle zurück. Die Werte spiegeln den Spielstand zum Zeitpunkt des Lesens wider.
</div>

| Methode | Beschreibung |
| --- | --- |
| `info.Title  -> string` | Text des aktiven Dan-Titels. |
| `info.Gold  -> bool` | Ob der Spieler den aktiven Titel mit einem Gold-Bestehen verdient hat. |
| `info.ClearStatus  -> integer` | Clear-Status-Code des aktiven Titels. |

### Dan-Titel-Eintrag-Handle

Ein Dan-Titel, den der Spieler auswählen kann.

<div class="callout warn">
sf:GetDanTitleByIndex gibt diese Einträge zurück. Index 0 ist der Standardtitel (nicht Gold, Clear-Status 0); spätere Indizes sind Titel, die der Spieler verdient hat.
</div>

| Methode | Beschreibung |
| --- | --- |
| `entry.Title  -> string` | Titeltext. |
| `entry.IsGold  -> bool` | Ob der Spieler den Titel mit einem Gold-Bestehen verdient hat. |
| `entry.ClearStatus  -> integer` | Bester für den Titel aufgezeichneter Clear-Status. |

### Dan-Bestleistungs-Handle

Die besten Prüfungsergebnisse eines Dan-Datensatzes.

<div class="callout warn">
sf:GetDanBestPlay gibt dieses Handle zurück. Prüfen Sie HasRecord, bevor Sie Prüfungen lesen. GetExam gibt ein .NET-Array zurück: ab 0 indizieren und `.Length` lesen.
</div>

| Methode | Beschreibung |
| --- | --- |
| `play.HasRecord  -> bool` | Ob für den Song ein Datensatz existiert. |
| `play:GetExam(slot)  -> int[]` | Beste Ergebnisse für Prüfungsslot 1 bis 7: ein Wert für eine kursweite Prüfung, einer pro Song für Prüfungen pro Song. Leer bei fehlendem Datensatz oder ungültigem Slot. |

## Charaktere und Puchicharas

### CHARACTER

Erzeugt Charakter-Handles und stellt die Namen der Standard-Animations- und Stimmen-Slots bereit.

<div class="callout warn">
CreateCharacter gibt ein Handle zurück, das seine Ressourcen besitzt; prüfen Sie IsValid und rufen Sie Dispose auf, wenn Sie fertig sind. GetPlayerCharacter gibt ein Handle zurück, das dem ausgerüsteten Charakter des Spielers folgt und keine Freigabe benötigt. GetPlayerGradientMap gibt eine Gradient-Map zurück (siehe Grafik und Text). Die Member ANIM_* und VOICE_* sind schreibgeschützte Strings; übergeben Sie sie an die Animations- und Stimmenmethoden des Charakter-Handles.
</div>

| Methode | Beschreibung |
| --- | --- |
| `CHARACTER:CreateCharacter(folderName)  -> character` | Lädt einen eigenständigen Charakter aus Global/Characters/{folderName}. IsValid ist false, wenn der Ordner nicht existiert. |
| `CHARACTER:GetPlayerCharacter(player)  -> character` | Ein an einen Spielerslot gebundenes Handle, das bei jedem Aufruf den ausgerüsteten Charakter auflöst. |
| `CHARACTER:GetPlayerGradientMap(player)  -> gradientMap` | Der für einen Spielerslot aktive Paletten-Verlauf, oder nil, wenn keiner gesetzt ist. |
| `CHARACTER.ANIM_PREVIEW  -> string` | Vorschaupose (Menüs und Shops). |
| `CHARACTER.ANIM_RENDER  -> string` | Vollständige Render-Pose. |
| `CHARACTER.ANIM_GAME_NORMAL  -> string` | Gameplay, Normalzustand. |
| `CHARACTER.ANIM_GAME_CLEAR  -> string` | Gameplay, Gauge in der Clear-Zone. |
| `CHARACTER.ANIM_GAME_MAX  -> string` | Gameplay, Gauge voll. |
| `CHARACTER.ANIM_GAME_GOGO  -> string` | Gameplay, Go-Go-Time. |
| `CHARACTER.ANIM_GAME_GOGO_MAX  -> string` | Gameplay, Go-Go-Time mit voller Gauge. |
| `CHARACTER.ANIM_GAME_MISS  -> string` | Gameplay, Miss. |
| `CHARACTER.ANIM_GAME_MISS_DOWN  -> string` | Gameplay, Miss mit niedriger Gauge. |
| `CHARACTER.ANIM_GAME_10COMBO  -> string` | Gameplay, 10-Combo-Meilenstein. |
| `CHARACTER.ANIM_GAME_10COMBO_MAX  -> string` | Gameplay, 10-Combo-Meilenstein mit voller Gauge. |
| `CHARACTER.ANIM_GAME_CLEARED  -> string` | Gameplay, Song geschafft. |
| `CHARACTER.ANIM_GAME_FAILED  -> string` | Gameplay, Song nicht geschafft. |
| `CHARACTER.ANIM_GAME_CLEAR_OUT  -> string` | Übergang aus dem Clear-Zustand. |
| `CHARACTER.ANIM_GAME_CLEAR_IN  -> string` | Übergang in den Clear-Zustand. |
| `CHARACTER.ANIM_GAME_MAX_OUT  -> string` | Übergang aus dem Zustand mit voller Gauge. |
| `CHARACTER.ANIM_GAME_MAX_IN  -> string` | Übergang in den Zustand mit voller Gauge. |
| `CHARACTER.ANIM_GAME_MISS_IN  -> string` | Übergang in einen Miss. |
| `CHARACTER.ANIM_GAME_MISS_DOWN_IN  -> string` | Übergang in einen Miss mit niedriger Gauge. |
| `CHARACTER.ANIM_GAME_RETURN  -> string` | Rückkehr in den Normalzustand. |
| `CHARACTER.ANIM_GAME_GOGOSTART  -> string` | Go-Go-Start-Effekt. |
| `CHARACTER.ANIM_GAME_GOGOSTART_CLEAR  -> string` | Go-Go-Start-Effekt im Clear-Zustand. |
| `CHARACTER.ANIM_GAME_GOGOSTART_MAX  -> string` | Go-Go-Start-Effekt mit voller Gauge. |
| `CHARACTER.ANIM_GAME_BALLOON_BREAKING  -> string` | Ballon wird geschlagen. |
| `CHARACTER.ANIM_GAME_BALLOON_BROKE  -> string` | Ballon geplatzt. |
| `CHARACTER.ANIM_GAME_BALLOON_MISS  -> string` | Ballon verfehlt. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_BREAKING  -> string` | Kusudama wird geschlagen. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_BROKE  -> string` | Kusudama zerbrochen. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_MISS  -> string` | Kusudama verfehlt. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_IDLE  -> string` | Kusudama im Leerlauf. |
| `CHARACTER.ANIM_GAME_TOWER_STANDING  -> string` | Tower-Modus, stehend. |
| `CHARACTER.ANIM_GAME_TOWER_STANDING_TIRED  -> string` | Tower-Modus, stehend und erschöpft. |
| `CHARACTER.ANIM_GAME_TOWER_CLIMBING  -> string` | Tower-Modus, kletternd. |
| `CHARACTER.ANIM_GAME_TOWER_CLIMBING_TIRED  -> string` | Tower-Modus, kletternd und erschöpft. |
| `CHARACTER.ANIM_GAME_TOWER_RUNNING  -> string` | Tower-Modus, laufend. |
| `CHARACTER.ANIM_GAME_TOWER_RUNNING_TIRED  -> string` | Tower-Modus, laufend und erschöpft. |
| `CHARACTER.ANIM_GAME_TOWER_CLEAR  -> string` | Tower-Modus, Clear. |
| `CHARACTER.ANIM_GAME_TOWER_CLEAR_TIRED  -> string` | Tower-Modus, Clear und erschöpft. |
| `CHARACTER.ANIM_GAME_TOWER_FAIL  -> string` | Tower-Modus, gescheitert. |
| `CHARACTER.ANIM_MENU_WAIT  -> string` | Menü, wartend. |
| `CHARACTER.ANIM_MENU_START  -> string` | Menü, Start. |
| `CHARACTER.ANIM_MENU_NORMAL  -> string` | Menü, normal. |
| `CHARACTER.ANIM_MENU_SELECT  -> string` | Menü, Auswahl. |
| `CHARACTER.ANIM_ENTRY_NORMAL  -> string` | Einstiegsbildschirm, normal. |
| `CHARACTER.ANIM_ENTRY_JUMP  -> string` | Einstiegsbildschirm, Sprung. |
| `CHARACTER.ANIM_RESULT_NORMAL  -> string` | Ergebnis, normal. |
| `CHARACTER.ANIM_RESULT_CLEAR  -> string` | Ergebnis, Clear. |
| `CHARACTER.ANIM_RESULT_FAILED_IN  -> string` | Ergebnis, Übergang in den Gescheitert-Zustand. |
| `CHARACTER.ANIM_RESULT_FAILED  -> string` | Ergebnis, gescheitert. |
| `CHARACTER.VOICE_END_FAILED  -> string` | Songende, gescheitert. |
| `CHARACTER.VOICE_END_CLEAR  -> string` | Songende, geschafft. |
| `CHARACTER.VOICE_END_FULLCOMBO  -> string` | Songende, Full Combo. |
| `CHARACTER.VOICE_END_ALLPERFECT  -> string` | Songende, All Perfect. |
| `CHARACTER.VOICE_END_AIBATTLE_WIN  -> string` | Songende, KI-Kampf gewonnen. |
| `CHARACTER.VOICE_END_AIBATTLE_LOSE  -> string` | Songende, KI-Kampf verloren. |
| `CHARACTER.VOICE_MENU_SONGSELECT  -> string` | Songauswahl betreten. |
| `CHARACTER.VOICE_MENU_SONGDECIDE  -> string` | Song bestätigt. |
| `CHARACTER.VOICE_MENU_SONGDECIDE_AI  -> string` | Song im KI-Kampf bestätigt. |
| `CHARACTER.VOICE_MENU_DIFFSELECT  -> string` | Schwierigkeitsauswahl. |
| `CHARACTER.VOICE_MENU_DANSELECTSTART  -> string` | Dan-Auswahl betreten. |
| `CHARACTER.VOICE_MENU_DANSELECTPROMPT  -> string` | Aufforderung in der Dan-Auswahl. |
| `CHARACTER.VOICE_MENU_DANSELECTCONFIRM  -> string` | Dan-Kurs bestätigt. |
| `CHARACTER.VOICE_TITLE_SANKA  -> string` | Einstieg am Titelbildschirm. |
| `CHARACTER.VOICE_TOWER_MISS  -> string` | Miss im Tower-Modus. |
| `CHARACTER.VOICE_RESULT_BESTSCORE  -> string` | Ergebnis, neue Bestwertung. |
| `CHARACTER.VOICE_RESULT_CLEARFAILED  -> string` | Ergebnis, gescheitert. |
| `CHARACTER.VOICE_RESULT_CLEARSUCCESS  -> string` | Ergebnis, geschafft. |
| `CHARACTER.VOICE_RESULT_DANFAILED  -> string` | Ergebnis, Dan nicht bestanden. |
| `CHARACTER.VOICE_RESULT_DANREDPASS  -> string` | Ergebnis, Dan bestanden. |
| `CHARACTER.VOICE_RESULT_DANGOLDPASS  -> string` | Ergebnis, Dan mit Gold bestanden. |

### Charakter-Handle

Ein zeichenbarer Charakter: spielt benannte Animationen und Stimmen ab und trägt einen Zeichenzustand pro Handle (Deckkraft, Skalierung, Einfärbung, Rotation, Blend- und Wrap-Modus, Paletten-Verlauf).

<div class="callout warn">
CHARACTER:GetPlayerCharacter, CHARACTER:CreateCharacter, sf:GetCharacter und die Eigenschaft Character eines Charakterlisten-Eintrags geben Charakter-Handles zurück. Nur Handles aus CreateCharacter besitzen ihre Ressourcen und benötigen Dispose. Das Handle speichert Set*-Werte und wendet sie bei jedem folgenden Zeichnen an; die Skalierungs- und Deckkraft-Argumente der Zeichenmethoden werden mit den gespeicherten Werten multipliziert. Die gespeicherte Deckkraft ist 0.0 bis 1.0, die Deckkraft pro Zeichnung ist 0 bis 255. Animations- und Stimmennamen sind die CHARACTER-Konstanten.
</div>

| Methode | Beschreibung |
| --- | --- |
| `char.IsValid  -> bool` | Ob das Handle auf einen geladenen Charakter auflöst. |
| `char.FolderName  -> string` | Ordnername, oder ein leerer String, wenn ungültig. |
| `char.FullPath  -> string` | Absoluter Ordnerpfad, oder ein leerer String, wenn ungültig. |
| `char.DisplayName  -> string` | Lokalisierter Anzeigename, mit Rückfall auf den Ordnernamen. |
| `char:SetPaletteGradient(stops, blend?)  -> nil` | Wendet einen Paletten-Verlauf an, der aus einer Tabelle mit mindestens zwei Farbstützstellen gebildet wird, mit optionaler Mischstärke (Standard 1.0). Spielergebundene Handles speichern den Verlauf auch auf dem Spielerslot. Das Übergeben von nil entfernt ihn. |
| `char:ClearPaletteGradient()  -> nil` | Entfernt den Paletten-Verlauf (und bei spielergebundenen Handles den Verlauf des Spielerslots). |
| `char:SetOpacity(opacity)  -> nil` | Gespeicherte Deckkraft, 0.0 transparent bis 1.0 deckend. |
| `char:SetScale(scaleX, scaleY)  -> nil` | Gespeicherte Skalierung; ein negatives X spiegelt horizontal. |
| `char:SetColor(color)  -> nil` | Gespeicherte Einfärbung aus einem Farbwert. |
| `char:SetColor(r, g, b)  -> nil` | Gespeicherte Einfärbung aus drei Kanälen 0.0 bis 1.0. |
| `char:SetRotation(degrees)  -> nil` | Gespeicherte Rotation in Grad. |
| `char:SetBlendMode(mode)  -> nil` | Gespeicherter Blend-Modus: "normal", "add", "multi", "sub" oder "screen". |
| `char:SetWrapMode(mode)  -> nil` | Gespeicherter Textur-Wrap-Modus: "edge", "border", "repeat" oder "mirror". |
| `char:GetScale()  -> vector2` | Gespeicherte Skalierung. |
| `char:GetColor()  -> tuple` | Gespeicherte Einfärbung als .NET-Tupel mit den Feldern Item1, Item2 und Item3 (Rot, Grün, Blau). |
| `char:GetRotation()  -> number` | Gespeicherte Rotation in Grad. |
| `char:GetBlendMode()  -> string` | Gespeicherter Blend-Modus. |
| `char:GetWrapMode()  -> string` | Gespeicherter Wrap-Modus. |
| `char:Draw(x, y, animation, scaleX?, scaleY?, opacity?)  -> nil` | Zeichnet die Animation bei x, y. Standardwerte: Skalierung 1, Deckkraft 255. |
| `char:DrawAtAnchor(x, y, animation, anchor?, scaleX?, scaleY?, opacity?)  -> nil` | Zeichnet die Animation mit dem benannten Ankerpunkt (Standard "bottom") bei x, y. |
| `char:DrawRect(x, y, w, h, animation, scaleX?, scaleY?, opacity?)  -> nil` | Zeichnet die Animation an der linken oberen Ecke des Rechtecks. Die Methode akzeptiert w und h für Layout-Code, aber sie beeinflussen das Zeichnen nicht. |
| `char:DrawRectAtAnchor(x, y, clipW, clipH, animation, opacity?, clipX?, clipY?)  -> nil` | Zeichnet die Animation mit ihrer linken oberen Ecke bei x, y, beschnitten auf ein Rechteck von clipW mal clipH mit Versatz clipX, clipY. Skalierung, Einfärbung und Rotation stammen ausschließlich aus dem gespeicherten Zustand. |
| `char:Update(animation, looping?)  -> bool` | Schreibt die Animation fort (standardmäßig in Schleife) und gibt zurück, ob sie noch läuft. |
| `char:LoadAnimation(animation)  -> nil` | Lädt die Frames der Animation. |
| `char:DisposeAnimation(animation)  -> nil` | Gibt die Frames der Animation frei. |
| `char:AvailableAnimation(animation)  -> bool` | Ob der Charakter die Animation bereitstellt. |
| `char:SetAnimationDuration(animation, duration)  -> nil` | Setzt die Abspieldauer der Animation. |
| `char:SetAnimationCyclesFromBPM(animation, bpm)  -> nil` | Setzt die Zykluslänge der Animation aus einer BPM. |
| `char:ResetAnimationCounter(animation)  -> nil` | Startet die Animation von ihrem ersten Frame neu. |
| `char:GetAnimationSize(animation)  -> vector2` | Gezeichnete Größe des aktuellen Frames der Animation in Skin-Auflösung, oder (0, 0), wenn nicht verfügbar. |
| `char:LoadVoice(voice)  -> nil` | Lädt einen Sprachclip. |
| `char:DisposeVoice(voice)  -> nil` | Gibt einen Sprachclip frei. |
| `char:PlayVoice(voice)  -> nil` | Spielt einen Sprachclip ab. |
| `char:Dispose()  -> nil` | Gibt die Ressourcen des Charakters frei (nur Handles aus CreateCharacter). |

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

Die Liste aller geladenen Charaktere.

<div class="callout warn">
Der Skin baut die Liste neu auf, wenn er seine Charaktere lädt, und gibt sie beim Neuladen des Skins frei, sodass das globale Objekt nil sein kann, solange keine Charaktere geladen sind. Abfragemethoden geben Charakterlisten-Einträge zurück.
</div>

| Methode | Beschreibung |
| --- | --- |
| `CHARACTERLIST.Count  -> integer` | Anzahl der geladenen Charaktere. |
| `CHARACTERLIST:GetAll()  -> characterEntry[]` | Jeder Charakter als Liste. |
| `CHARACTERLIST:GetByIndex(index)  -> characterEntry` | Der Eintrag an einem 0-basierten Index, oder nil außerhalb des Bereichs. |
| `CHARACTERLIST:GetByName(folderName)  -> characterEntry` | Der Eintrag mit diesem Ordnernamen, oder nil, wenn nicht gefunden. |

### Charakterlisten-Eintrag

Ein CHARACTERLIST-Eintrag: Ordnername, Anzeigename, Seltenheit, ein Charakter-Handle und die Freischaltbedingung.

<div class="callout warn">
Der Liste gehört das gemeinsam genutzte Handle in der Eigenschaft Character; geben Sie es nicht frei. Laden Sie Animationen darauf, bevor Sie zeichnen.
</div>

| Methode | Beschreibung |
| --- | --- |
| `entry.FolderName  -> string` | Ordnername; Spielstände verwenden ihn als Schlüssel. |
| `entry.DisplayName  -> string` | Lokalisierter Anzeigename. |
| `entry.Rarity  -> string` | Seltenheitsname (siehe Namensschild-Info-Handle für die Liste). |
| `entry.Character  -> character` | Charakter-Handle für diesen Eintrag. |
| `entry.UnlockCondition  -> unlockCondition` | Die Freischaltbedingung (siehe Freischaltbedingungs-Handle). |

### PUCHICHARALIST

Die Liste aller geladenen Puchicharas, plus die aktuelle Auswahl jedes Spielers.

<div class="callout warn">
Der Skin baut die Liste neu auf, wenn er seine Puchichara-Texturen lädt, und gibt sie beim Neuladen des Skins frei, sodass das globale Objekt nil sein kann, solange sie nicht geladen sind. Abfragemethoden geben Puchichara-Handles zurück.
</div>

| Methode | Beschreibung |
| --- | --- |
| `PUCHICHARALIST.Count  -> integer` | Anzahl der geladenen Puchicharas. |
| `PUCHICHARALIST:GetAll()  -> puchichara[]` | Jeder Puchichara als Liste. |
| `PUCHICHARALIST:GetByIndex(index)  -> puchichara` | Der Puchichara an einem 0-basierten Index, oder nil außerhalb des Bereichs. |
| `PUCHICHARALIST:GetByName(folderName)  -> puchichara` | Der Puchichara mit diesem Ordnernamen, oder nil, wenn nicht gefunden. |
| `PUCHICHARALIST:GetPlayerPuchichara(player)  -> puchichara` | Der von einem Spielerslot ausgerüstete Puchichara, oder nil, wenn er nicht aufgelöst werden kann. |

### Puchichara-Handle

Ein Puchichara: seine Texturen, lokalisierter Name und Autor, Seltenheit, Ordnername und Freischaltbedingung.

<div class="callout warn">
PUCHICHARALIST und sf:GetPuchichara geben diese Handles zurück. Der Liste gehören die Texturen; geben Sie sie nicht frei. Ein fehlendes Bild ergibt eine leere Textur.
</div>

| Methode | Beschreibung |
| --- | --- |
| `puchi.tx  -> texture` | Sprite-Sheet, geladen aus Chara.png. |
| `puchi.render  -> texture` | Vollständiges Render, geladen aus Render.png. |
| `puchi.Name  -> string` | Lokalisierter Anzeigename. |
| `puchi.Author  -> string` | Lokalisierter Autorenname. |
| `puchi.Rarity  -> string` | Seltenheitsname (siehe Namensschild-Info-Handle für die Liste). |
| `puchi.FolderName  -> string` | Ordnername; Spielstände verwenden ihn als Schlüssel. |
| `puchi.UnlockCondition  -> unlockCondition` | Die Freischaltbedingung (siehe Freischaltbedingungs-Handle). |
| `puchi:GetUnlockMessage()  -> string` | Abkürzung für `puchi.UnlockCondition:GetConditionMessage()`. |

## Spielzustand und Freischaltungen

### PLAYSTATE

Live-Ergebnisse des aktuellen oder letzten Spiels: Wertungszahlen, Score, Combo, Clear-Prüfungen sowie Tower- und Dan-Status.

<div class="callout warn">
Die Werte stammen vom Spielbildschirm und sind daher während eines Spiels und auf den darauf folgenden Bildschirmen aussagekräftig. Spielerindizes sind 0-basiert; die Methoden prüfen sie nicht auf den Bereich. Die Dan-Prüfungen werten immer Spieler 0 aus.
</div>

| Methode | Beschreibung |
| --- | --- |
| `PLAYSTATE.LastRegisteredFloor  -> integer` | Tower-Modus: das zuletzt erreichte Stockwerk. |
| `PLAYSTATE.MaxNumberOfLives  -> integer` | Tower-Modus: die maximale Anzahl Leben. |
| `PLAYSTATE.CurrentNumberOfLives  -> integer` | Tower-Modus: die aktuelle Anzahl Leben. |
| `PLAYSTATE.InvincibilityDurationSpeedDependent  -> number` | Tower-Modus: die an die Songgeschwindigkeit angepasste Unverwundbarkeitsdauer. |
| `PLAYSTATE.InvincibilityDuration  -> integer` | Tower-Modus: die Basis-Unverwundbarkeitsdauer. |
| `PLAYSTATE:WasPlayEndedNormally()  -> bool` | Ob das vorherige Spiel bis zum Ende lief. |
| `PLAYSTATE:WasPlayAborted()  -> bool` | Ob der Spieler das vorherige Spiel vorzeitig beendet hat. |
| `PLAYSTATE:GetGoodCount(player)  -> integer` | Anzahl der Good-Wertungen. |
| `PLAYSTATE:GetOkCount(player)  -> integer` | Anzahl der Ok-Wertungen. |
| `PLAYSTATE:GetBadCount(player)  -> integer` | Anzahl der Bad-Wertungen. |
| `PLAYSTATE:GetRollCount(player)  -> integer` | Anzahl der Trommelwirbel-Treffer. |
| `PLAYSTATE:GetADLibCount(player)  -> integer` | Anzahl der getroffenen ADLib-Noten. |
| `PLAYSTATE:GetMissedADLibCount(player)  -> integer` | Anzahl der verfehlten ADLib-Noten. |
| `PLAYSTATE:GetBoomCount(player)  -> integer` | Anzahl der getroffenen Minen-Noten. |
| `PLAYSTATE:GetAvoidedBoomCount(player)  -> integer` | Anzahl der vermiedenen Minen-Noten. |
| `PLAYSTATE:GetScore(player)  -> integer` | Aktueller Score. |
| `PLAYSTATE:GetCombo(player)  -> integer` | Aktuelle Combo. |
| `PLAYSTATE:GetHighestCombo(player)  -> integer` | Höchste erreichte Combo. |
| `PLAYSTATE:IsClear(player)  -> bool` | Ob die Gauge die Clear-Linie erreicht. |
| `PLAYSTATE:IsAssistedClear(player)  -> bool` | Ob das Spiel ein Clear ist, während ein scoremindernder Mod aktiv ist. |
| `PLAYSTATE:IsFullCombo(player)  -> bool` | Clear, nicht assistiert, ohne Bad-Wertungen und ohne getroffene Minen. |
| `PLAYSTATE:IsPerfect(player)  -> bool` | Full Combo ohne Ok-Wertungen. |
| `PLAYSTATE:IsAlive()  -> bool` | Tower-Modus: ob noch Leben übrig sind. |
| `PLAYSTATE:IsPass()  -> bool` | Dan-Modus: ob der Prüfungsstatus kein Nichtbestehen ist. |
| `PLAYSTATE:IsRedPass()  -> bool` | Dan-Modus: ob der Prüfungsstatus ein normales Bestehen ist. |
| `PLAYSTATE:IsGoldPass()  -> bool` | Dan-Modus: ob der Prüfungsstatus ein Gold-Bestehen ist. |
| `PLAYSTATE:IsDanClear()  -> bool` | Dan-Modus: bestanden und nicht assistiert. |
| `PLAYSTATE:IsDanFullCombo()  -> bool` | Dan-Modus: Dan-Clear ohne Bad-Wertungen und ohne getroffene Minen. |
| `PLAYSTATE:IsDanPerfect()  -> bool` | Dan-Modus: Dan-Full-Combo ohne Ok-Wertungen. |

### Freischaltbedingungs-Handle

Die Freischaltanforderung eines Namensschilds, Charakters oder Puchicharas.

<div class="callout warn">
Die Eigenschaft UnlockCondition von Namensschild-Info-Handles, Charakterlisten-Einträgen und Puchichara-Handles gibt dieses Handle zurück. Ein Gegenstand ohne Bedingung (HasCondition false) ist standardmäßig verfügbar: IsUnlockable gibt true zurück und die Meldungen sind leer. Das Bedingungsvokabular entspricht dem von Unlock.json und Chart-Freischaltungen; siehe die Anleitung <a href="../guides/unlockables.md">Chart-Freischaltungen</a>.
</div>

| Methode | Beschreibung |
| --- | --- |
| `cond.HasCondition  -> bool` | Ob der Gegenstand eine Freischaltbedingung hat. |
| `cond:GetConditionType()  -> string` | Die Bedingungstyp-ID (zum Beispiel "ch", "cs", "gt", "gc" oder "ig"), oder ein leerer String. |
| `cond:GetCoinPrice()  -> integer` | Münzpreis der Bedingung, oder 0. |
| `cond:GetConditionMessage()  -> string` | Lokalisierte Beschreibung der Bedingung. |
| `cond:IsUnlockable(player)  -> bool` | Ob der Spieler die Bedingung gerade erfüllt. |
| `cond:GetBlockedMessage(player)  -> string` | Warum der Spieler die Bedingung nicht erfüllt, oder ein leerer String, wenn sie erfüllt ist. |

## Theme und Sprache

### THEME

Die Auflösung des Skins, Theme-Einstellungen, skinweite lokalisierte Strings und die Definitionen der Theme-Einstellungen.

<div class="callout warn">
Der Skin deklariert Theme-Einstellungen in ThemeSettings.json und speichert ihre Werte in der ThemeSettings.db3 daneben. Die Getter geben Einstellungswerte immer als Strings zurück; eine fehlende Einstellung gibt ihren deklarierten Standardwert zurück, oder einen leeren String, wenn keine Deklaration existiert. GetThemeSettingForPlayer nimmt eine 1-basierte Spielernummer. Definitionsindizes sind 0-basiert.
</div>

| Methode | Beschreibung |
| --- | --- |
| `THEME:GetResolution()  -> vector2` | Die Auflösung des Skins. |
| `THEME:GetThemeSetting(settingId)  -> string` | Wert einer Einstellung mit globalem Geltungsbereich. |
| `THEME:GetThemeSettingForPlayer(settingId, player)  -> string` | Wert einer Einstellung mit Spielstand-Geltungsbereich für den 1-basierten Spieler, oder ihr Standardwert, wenn der Spielstand keinen Wert hat. |
| `THEME:GetSkinString(key)  -> string` | Lokalisierter String aus dem Locales-Ordner des Skins: zuerst die aktuelle Sprache, dann die Standard-Locale des Skins, dann `[LOCALE NOT FOUND: key]`. |
| `THEME:GetDefinitionCount()  -> integer` | Anzahl der Einstellungsdefinitionen in ThemeSettings.json. |
| `THEME:GetDefinitionId(index)  -> string` | ID der Definition an einem 0-basierten Index, oder ein leerer String. |
| `THEME:GetDefinitionScope(index)  -> string` | Geltungsbereich der Definition: "global" oder "save". |
| `THEME:GetDefinitionType(index)  -> string` | Typ der Definition: "bool", "int", "double", "string" oder "enum". |

### LANG

Lokalisierte Spielstrings, Sprachwechsel und mehrsprachige Textwerte.

<div class="callout warn">
GetString formatiert den Eintrag mit den zusätzlichen Argumenten. GetLanguageIds und GetLanguageNames geben .NET-Arrays zurück (0-basiert, `.Length`); GetAvailableLanguages gibt ein Dictionary zurück, das mit `:GetEnumerator()` aufgezählt wird (siehe Daten und Persistenz). FromDict nimmt ein von JSONLOADER geparstes JSON-Objekt (eine Lua-Tabelle akzeptiert es nicht); AsLocalizationData nimmt einen JsonNode aus JSONLOADER:LoadJson.
</div>

| Methode | Beschreibung |
| --- | --- |
| `LANG:GetString(key, ...)  -> string` | Der lokalisierte String für einen Schlüssel, mit aus den zusätzlichen Argumenten gefüllten Formatplatzhaltern. |
| `LANG:ChangeLanguage(id)  -> bool` | Wechselt die aktive Sprache, wenn die ID existiert und sich von der aktuellen unterscheidet, und ruft dann `reloadLanguage` auf jedem geladenen Skript auf; gibt zurück, ob gewechselt wurde. CONFIG.Language lässt es unverändert. |
| `LANG:GetLanguageIds()  -> string[]` | IDs der verfügbaren Sprachen. |
| `LANG:GetLanguageNames()  -> string[]` | Anzeigenamen der verfügbaren Sprachen, in derselben Reihenfolge. |
| `LANG:GetAvailableLanguages()  -> dict` | Sprach-ID zu Anzeigename. |
| `LANG:GetExamName(type)  -> string` | Lokalisierter Name eines Dan-Prüfungstyps. |
| `LANG:AsLocalizationData(node)  -> localizationData` | Baut einen Lokalisierungswert aus einem JsonNode der Form `{ "strings": { "<lang>": "text" } }`. |
| `LANG:FromDict(dict)  -> localizationData` | Baut einen Lokalisierungswert aus einem geparsten JSON-Objekt, das Sprach-IDs auf Text abbildet. |
| `LANG:FromString(json)  -> localizationData` | Baut einen Lokalisierungswert aus einem JSON-Objekt-String, der Sprach-IDs auf Text abbildet; ein leerer Wert, wenn der String nicht geparst werden kann. |

```lua
local langs = LANG:GetAvailableLanguages()
local e = langs:GetEnumerator()
while e:MoveNext() do
    print(e.Current.Key, e.Current.Value)
end

local name = LANG:FromString('{"ja":"太鼓","default":"Taiko"}'):GetString("")
```

### Lokalisierungsdaten-Handle

Ein Satz von Strings, indiziert nach Sprach-ID, der zur aktuellen Sprache auflöst.

<div class="callout warn">
LANG:AsLocalizationData, LANG:FromDict und LANG:FromString geben dieses Handle zurück. Auflösungsreihenfolge: die aktuelle Sprach-ID, dann der Schlüssel "default", dann der an GetString übergebene Rückfallwert.
</div>

| Methode | Beschreibung |
| --- | --- |
| `loc:GetString(fallback)  -> string` | Der Text für die aktuelle Sprache, oder "default", oder der Rückfallwert. |
| `loc:SetString(langId, text)  -> nil` | Setzt den Text für eine Sprach-ID. |
| `loc:GetAllStrings()  -> string[]` | Jeder gespeicherte Text, in keiner bestimmten Reihenfolge. |
