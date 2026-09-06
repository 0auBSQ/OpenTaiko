<!-- guides/characters.md -->

# Charaktere hinzufügen

Ein Charakter ist ein Ordner unter `Global/Characters/` im Installationsordner des Spiels. Jeder Unterordner, den das Spiel dort findet, wird zu einem auswählbaren Charakter. Ein Ordner enthält eine `Metadata.json` (Name, Seltenheit, Autor), eine `CharaConfig.txt` (Positionen und Animationstiming), den Animationsinhalt und optional `Effects.json`, `Unlock.json`, `Palettes.json` und Sprachclips. Der Animationsinhalt besteht entweder aus Ordnern mit nummerierten PNG-Frames, die das eingebaute Charakterskript des Spiels rendert, oder aus dem, was eine charakterspezifische `Script.lua` zu zeichnen wählt (die mitgelieferte 3D-Vorlage zeichnet ein glTF-Modell).

Kompatibilität: OpenTaiko 0.6.1 lädt für 0.6.0 erstellte Charaktere weiterhin ohne Änderungen. Diese Seite beschreibt den aktuellen Aufbau; verwenden Sie ihn für neue Charaktere.

## Bevor Sie beginnen

- OpenTaiko 0.6.1 ist installiert. Das Spiel liest Charaktere aus `Global/Characters/` neben der ausführbaren Datei des Spiels, und alle Skins nutzen sie gemeinsam.
- Ein Texteditor für JSON- und INI-artige Dateien.
- Für einen 2D-Charakter: die Grafik als nummerierte PNG-Frames (`0.png`, `1.png`, ...) mit transparentem Hintergrund exportiert, ein Ordner pro Animationszustand.
- Für einen 3D-Charakter: eine `model.glb` (binäres glTF) mit den Animationsclips und ein Standbild `Render.png`.
- Die mitgelieferten Ordner `01 - Template` (2D) und `01 - Template3D`. Kopieren Sie einen davon als Ausgangspunkt.

## Schritt 1: Erkennung, Reihenfolge und Identität verstehen

Beim Start listet das Spiel die Unterordner von `Global/Characters/` auf und erzeugt einen Charakter pro Ordner, in der Reihenfolge, in der das Dateisystem sie liefert. Das Spiel sortiert die Liste nicht, weshalb die mitgelieferten Ordner ein numerisches Präfix tragen (`00 - None`, `01 - Template`, `02 - Student (A)`, ...), um die Reihenfolge vorhersehbar zu halten. Behalten Sie `00 - None` an erster Stelle: Index 0 ist der leere Slot und der Rückfall, wenn ein gespeicherter Charakter fehlt.

Spielstände speichern den gewählten Charakter per Ordnername (`characterName`) und lösen ihn bei jedem Start erneut in einen Index auf. Das Hinzufügen oder Entfernen anderer Ordner beschädigt eine gespeicherte Auswahl nie, aber das Umbenennen eines Ordners lässt Spielstände, die darauf verwiesen haben, auf `00 - None` zurückfallen. Zwei Charaktere dürfen denselben Anzeigenamen haben; der Ordnername muss eindeutig sein.

Das Spiel enumeriert Charaktere einmal beim Start und erneut beim Neuladen des Skins; ein Ordner, der hinzugefügt wird, während das Spiel läuft, erscheint nach dem nächsten Start oder Neuladen des Skins.

## Schritt 2: Ordner und Metadata.json anlegen

Legen Sie einen Ordner wie `30 - MyChara` an und fügen Sie `Metadata.json` hinzu:

- `name`: Anzeigename. Entweder ein einfacher String oder ein lokalisiertes Objekt `{ "strings": { "default": "...", "ja": "...", ... } }`. `default` ist der Rückfall; die anderen Schlüssel sind Sprachcodes des Spiels.
- `rarity`: eines von `Poor`, `Common`, `Uncommon`, `Rare`, `Epic`, `Legendary`, `Mythical`. Die Seltenheit steuert nur die Farbe und die Stufe der Freischaltbenachrichtigung; jede Seltenheit hat einen Münzmultiplikator von 1.
- `author`: einfacher String oder lokalisiertes Objekt.
- `description`: optional, einfacher String oder lokalisiertes Objekt.
- `speechtext`: optional, ein Array aus sechs lokalisierten Objekten, die der Ergebnisbildschirm in der Sprechblase des Charakters anzeigt. Das Spiel wählt den Eintrag nach Ergebnis aus, in dieser Reihenfolge: gescheitert mit niedriger Gauge, gescheitert mit Gauge bei 40 % oder mehr, geschafft, geschafft mit voller Gauge, Full Combo, All Perfect. Geben Sie weniger als sechs an, wiederholt das Spiel den letzten.

Fehlt `Metadata.json`, wird der Charakter trotzdem mit dem Namen `(None)`, der Seltenheit `Common` und dem Autor `(None)` geladen.

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

## Schritt 3 (2D-Weg): Frame-Ordner hinzufügen

Hat der Ordner keine `Script.lua`, rendert das Spiel den Charakter mit seinem eingebauten Skript (`CharaScript.lua` im Installationsordner des Spiels). Dieses Skript ordnet jeden Animationszustand einem Unterordner zu und lädt daraus `0.png`, `1.png`, `2.png`, ... Das Laden stoppt beim ersten fehlenden Index, daher muss die Nummerierung lückenlos sein.

| Animationszustand | Ordner |
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
| Game/Tower/Standing, Climbing, Running, Clear, Fail (und die `_Tired`-Varianten) | `Tower_Char/Standing`, `Tower_Char/Climbing`, `Tower_Char/Running`, `Tower_Char/Clear`, `Tower_Char/Fail` (plus `Tower_Char/Standing_Tired` und so weiter) |
| Menu/Wait, Menu/Start, Menu/Normal, Menu/Select | `Menu_Wait`, `Menu_Start`, `Menu_Loop`, `Menu_Select` |
| Entry/Normal, Entry/Jump | `Title_Normal`, `Title_Entry` |
| Result/Normal, Result/Clear, Result/Failed_In, Result/Failed | `Result_Normal`, `Result_Clear`, `Result_Failed_In`, `Result_Failed` |

Das eingebaute Skript liest zwei Standbilder aus der Ordnerwurzel: `Render.png` (das Porträt in voller Größe, das überall dort gezeichnet wird, wo das Spiel den Animationstyp Render anfordert, zum Beispiel im Raum) und `Preview.png` (das Vorschaubild; fehlt es, verwendet das Skript `Normal/0.png`).

Fehlende Zustände fallen auf einen anderen Zustand zurück, sodass ein Charakter nur eine Teilmenge mitliefern kann. Die Rückfallkette lautet: Clear -> Normal, Max -> Clear, Miss -> Normal, Miss_Down -> Miss, Gogo -> Normal, Gogo_Max -> Gogo, 10combo_Max -> 10combo, GoGoStart_Clear -> GoGoStart, GoGoStart_Max -> GoGoStart_Clear, die Tower-`_Tired`-Zustände -> ihr normaler Zustand, Tower/Fail -> Tower/Standing_Tired, Kusudama_Idle -> Normal, Menu/Wait -> Gogo, Menu/Start und Menu/Select und Entry/Jump -> 10combo, Menu/Normal und Entry/Normal und Result/Normal -> Normal, Result/Clear -> Clear, Result/Failed_In -> Miss_In, Result/Failed -> Miss. Zustände ohne Rückfall (zum Beispiel Cleared, Failed, Return, die Ballon-Zustände) zeichnen nichts, wenn sie fehlen. Das Minimum für einen funktionierenden Charakter ist `Normal/0.png`.

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
  Sounds/                (optionale Sprachclips, siehe Schritt 6)
```

## Schritt 4: CharaConfig.txt schreiben

`CharaConfig.txt` ist eine `Key=Value`-Textdatei; Zeilen, die mit `;` beginnen, sind Kommentare. Das eingebaute Skript liest diese Schlüssel (die mitgelieferte 3D-Vorlage liest die Positionsschlüssel ebenfalls):

- `Chara_Resolution=W,H` (Standard `1280,720`): die Auflösung, für die Sie die Koordinaten unten erstellen. Das Spiel skaliert Positionen beim Zeichnen von dieser Auflösung auf die Skin-Auflösung.
- `Chara_LegacyMode` (Standard `1`): behält die Verankerungs- und Offset-Korrekturen von 0.6.0 bei. Aus älteren Versionen portierte Charaktere sind darauf angewiesen.
- `Game_Chara_X=...` / `Game_Chara_Y=...`: Gameplay-Position; das Skript verwendet den ersten Wert jeder Liste. `Game_Chara_Offset=X,Y` ist eine alternative Form.
- `Game_Chara_X_AI=...` / `Game_Chara_Y_AI=...`: ein Wert pro Spieler für den KI-Kampf. Sind beide Schlüssel vorhanden, ersetzen sie für diesen Charakter die KI-Kampf-Position des Skins.
- `Game_Chara_Balloon_X` / `Game_Chara_Balloon_Y`, `Game_Chara_Kusudama_X` / `Game_Chara_Kusudama_Y`: Positionen während Ballon- und Kusudama-Sequenzen (erster Wert wird verwendet). `Game_Chara_Balloon_Offset`, `Game_Chara_Kusudama_Offset` und `Game_Chara_Tower_Offset` nehmen ein `X,Y`-Paar.
- `Menu_Offset=X,Y`, `Menu_Chara_Scale`, `Result_Offset=X,Y`, `Heya_Chara_Render_Offset=X,Y`: Offsets für Menü, Ergebnis und das Render im Raum.
- `Game_Chara_Motion_<State>=0,1,2,...`: die Reihenfolge, in der die Frames eines Zustands abgespielt werden, als 0-basierte Frame-Indizes. Fehlt der Schlüssel, werden die Frames in Dateireihenfolge abgespielt. Zustandsnamen folgen den Ordnernamen, zum Beispiel `Game_Chara_Motion_Normal`, `Game_Chara_Motion_GoGo`, `Game_Chara_Motion_Miss_Down`, `Game_Chara_Motion_Balloon_Broke`, `Game_Chara_Motion_Tower_Climbing`.
- `Game_Chara_Beat_<State>=N`: über wie viele Beats sich ein Durchlauf des Zustands erstreckt, zum Beispiel `Game_Chara_Beat_Normal=1`, `Game_Chara_Beat_GoGo=2`.
- Menü-, Titel- und Ergebniszustände verwenden `Menu_Chara_Motion_Loop/Wait/Start/Select`, `Title_Chara_Motion_Normal/Entry`, `Result_Chara_Motion_Normal/Clear/Failed_In/Failed`, mit passenden `_Beat_`-Schlüsseln oder festen Dauern in Millisekunden: `Chara_Menu_Loop_AnimationDuration`, `Chara_Menu_Wait_AnimationDuration`, `Chara_Menu_Start_AnimationDuration`, `Chara_Menu_Select_AnimationDuration`, `Chara_Normal_AnimationDuration`, `Chara_Entry_AnimationDuration`, `Chara_Result_Normal_AnimationDuration`, `Chara_Result_Clear_AnimationDuration`, `Chara_Result_Failed_In_AnimationDuration`, `Chara_Result_Failed_AnimationDuration`.

Die vollständige Schlüsselliste mit Standardwerten ist die Tabelle `load_chara_config_defs` am Anfang der eingebauten `CharaScript.lua`. Das Skript ignoriert Schlüssel, die es nicht kennt, weshalb die mitgelieferte `01 - Template/CharaConfig.txt` auch einige skinseitige Schlüssel enthält, die in dieser Datei keine Wirkung haben.

```ini
Chara_Version=0.6.1.0
Chara_Resolution=1920,1080

;Charakter-X-Position (1P,2P)
Game_Chara_X=0,0
;Charakter-Y-Position (1P,2P)
Game_Chara_Y=0,805

;Frame-Reihenfolge und Beats pro Durchlauf im Normalzustand
Game_Chara_Motion_Normal=0,1,2,3,4,5,6
Game_Chara_Beat_Normal=1

;Frame-Reihenfolge und Beats pro Durchlauf bei GoGo
Game_Chara_Motion_GoGo=0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15
Game_Chara_Beat_GoGo=2
```

## Schritt 5 (3D-Weg): model.glb und eine charakterspezifische Script.lua mitliefern

Existiert `Script.lua` im Charakterordner, ersetzt sie das eingebaute Skript vollständig. Das Spiel ruft dann diese globalen Funktionen per Name auf:

- `loadAnimation(animationType)`, `disposeAnimation(animationType)`
- `availableAnimation(animationType)`, gibt einen Boolean zurück. Das Spiel akzeptiert weiterhin die ältere Falschschreibung `avaialbeAnimation`: Es versucht zuerst `availableAnimation` und fällt auf `avaialbeAnimation` zurück. Die mitgelieferte 3D-Vorlage verwendet noch den alten Namen.
- `setAnimationDuration(animationType, durationMs)`, `resetAnimationCounter(animationType)`
- `update(delta, animationType, looping)`, gibt `true` zurück, wenn eine nicht schleifende Animation beendet ist
- `draw(animationType, x, y, scaleX, scaleY, opacity, color, contextType, anchor, clipW, clipH, clipX, clipY, rotation, blendMode, wrapMode, gradientMap)`
- `getDrawSize(animationType)`, gibt Breite und Höhe zurück
- `getHeyaRenderOffset()`, gibt x und y zurück; `getAIBattlePosition(player, charaScale)`, gibt x und y zurück, oder `nil`, um die Position des Skins zu verwenden
- `loadVoice(voiceType)`, `disposeVoice(voiceType)`, `playVoice(voiceType)`

Animationstypen sind die Strings hinter den Konstanten `CHARACTER.ANIM_*` (`"Game/Normal"`, `"Menu/Normal"`, ...), plus die beiden Sondertypen `CHARACTER.ANIM_PREVIEW` (Vorschaubild) und `CHARACTER.ANIM_RENDER` (vollständiges Porträt). Stimmentypen sind die Konstanten `CHARACTER.VOICE_*`. Die Rückfallkette aus Schritt 3 gilt auch für geskriptete Charaktere: Das Spiel fragt `availableAnimation` und durchläuft die Alternativen, bis eine verfügbar ist.

Der mitgelieferte Ordner `01 - Template3D` enthält nur `CharaConfig.txt`, `Effects.json`, `Metadata.json`, `model.glb`, `Render.png` und `Script.lua`. Sein Skript lädt `model.glb` mit `MODEL:Load`, rendert es in eine Szene, die es mit `SCENE3D:CreateScene` erzeugt, liest die Positionsschlüssel aus `CharaConfig.txt` und ordnet jeden Animationstyp in einer Tabelle `CLIP` einem Clip-Index und einer Beat-Anzahl zu. Um einen 3D-Charakter zu erstellen, kopieren Sie den Ordner, ersetzen `model.glb` und `Render.png` und bearbeiten `CLIP` so, dass jeder Typ auf den richtigen Clip-Index Ihres Modells zeigt.

```lua
-- Auszug aus 01 - Template3D/Script.lua
local CLIP = {
  [CHARACTER.ANIM_GAME_NORMAL] = { clip = 0,  beat = 1 };
  [CHARACTER.ANIM_GAME_CLEAR]  = { clip = 1,  beat = 1 };
  [CHARACTER.ANIM_GAME_GOGO]   = { clip = 2,  beat = 4 };
  [CHARACTER.ANIM_MENU_NORMAL] = { clip = 14, beat = 2 };
  [CHARACTER.ANIM_RESULT_CLEAR]= { clip = 19, beat = 1 };
  -- ein Eintrag pro Animationszustand, den das Modell unterstützt
}

function loadAnimation(animationType)
  -- Clip-, Vorschau- und Render-Daten aufbauen und als verfügbar markieren
end

function availableAnimation(animationType)
  return animations[animationType] ~= nil
end
```

## Schritt 6: Optionale Dateien: Effects.json, Unlock.json, Palettes.json, Stimmen

- `Effects.json`: `gauge` (`Normal`, `Hard` oder `Extreme`; Standard `Normal`) wählt den Typ der Soul-Gauge. `Hard` multipliziert Münzgewinne mit 1,5 und `Extreme` mit 1,8, sofern das Spiel nicht die normale Gauge erzwingt. Ist der Fun-Mod Minesweeper aktiv, ist `bombFactor` (1-100, Standard 20) der Prozentsatz der Noten, die der Mod zu Bomben macht, und `fuseRollFactor` (0-100, Standard 0) der Prozentsatz der Ballons, die er zu Zündschnur-Wirbeln macht.
- `Unlock.json`: wenn vorhanden, bleibt der Charakter gesperrt, bis der Spieler die Bedingung erfüllt. Format und Bedingungs-IDs entsprechen denen bei Songs; siehe die Anleitung zu Freischaltungen. Münzbedingungen kauft der Spieler im Raum-Bildschirm; die anderen Bedingungen prüft das Spiel automatisch auf dem Ergebnisbildschirm. Mitgelieferte Beispiele: Kuro verwendet `{ "condition": "dp", "type": "me", "values": [3, 3, 10] }` (zehn Clears von Extreme-Charts mit Full Combo oder besser) und Aoi verwendet `{ "condition": "ch", "type": "me", "values": [200] }` (200 Münzen).
- `Palettes.json`: ein Array von Farbpaletten, die der Spieler auf den Charakter anwenden kann. Jeder Eintrag hat `name`, `blend` (0-1), `stops` (ein Array von Verlaufsstützstellen `[position, R, G, B]` oder `[position, R, G, B, A]`; geben Sie mindestens zwei an) und `plays`, die Anzahl der Spiele mit diesem Charakter, die die Palette freischaltet (0 oder fehlend bedeutet sofort verfügbar). Ein Eintrag mit `"stops": null` ist der ungefärbte Standard.
- Stimmen: Das eingebaute Skript lädt `.ogg`-Dateien aus festen Pfaden innerhalb des Charakterordners, zum Beispiel `Sounds/Clear/Clear.ogg`, `Sounds/Clear/Failed.ogg`, `Sounds/Clear/FullCombo.ogg`, `Sounds/Clear/AllPerfect.ogg`, `Sounds/Menu/SongSelect.ogg`, `Sounds/Menu/SongDecide.ogg`, `Sounds/Menu/DiffSelect.ogg`, `Sounds/Title/Sanka.ogg`, `Sounds/Result/BestScore.ogg`, `Sounds/Result/ClearSuccess.ogg`, `Sounds/Result/ClearFailed.ogg`. Die vollständige Liste ist die Tabelle `voice_files` am Anfang der eingebauten `CharaScript.lua`. Fehlende Dateien überspringt das Skript.

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

## Schritt 7: Neu starten und den Charakter auswählen

Starten Sie das Spiel neu (oder laden Sie den Skin aus den Einstellungen neu). Der Charakter erscheint in der Charakterliste des Raum-Bildschirms, wo gesperrte Charaktere ihre Freischaltbedingung anzeigen. Lua-Stages können die Liste auch über das globale Objekt `CHARACTERLIST` lesen, das für jeden Eintrag Ordnername, Anzeigename, Seltenheit und Freischaltbedingung bereitstellt.

## Fehlerbehebung und Hinweise

- Der Charakter erscheint nicht: Prüfen Sie, dass der Ordner direkt unter `Global/Characters/` liegt, und starten Sie das Spiel neu. Das Spiel baut die Liste einmal beim Start auf.
- Der Charakter zeichnet nichts: `Normal/0.png` fehlt, oder die Ordnernamen stimmen nicht mit der Tabelle in Schritt 3 überein. Frames müssen `0.png`, `1.png`, ... ohne Lücken heißen; eine Lücke beendet die Animation an diesem Index ohne Fehler.
- Der Charakter ist außerhalb des Bildschirms oder hat die falsche Größe: `Chara_Resolution` muss der Auflösung entsprechen, für die Sie die Positionswerte erstellt haben. Fehlt der Schlüssel, nimmt das Spiel `1280,720` an.
- Nur ein Teil des Animationssatzes wird abgespielt: Zustände ohne Rückfall (Cleared, Failed, Return, Ballon- und Kusudama-Zustände) brauchen einen eigenen Ordner.
- Ein 3D-Charakter zeigt jede Animation als nicht verfügbar: `Script.lua` muss `availableAnimation` (oder `avaialbeAnimation`) definieren und für die geladenen Typen `true` zurückgeben.
- Eine vorhandene `Script.lua` ersetzt das eingebaute Skript vollständig. Ein geskripteter Charakter kann weiterhin nummerierte PNG-Ordner laden, aber nur, wenn das Skript sie selbst lädt.
- Spielstände verweisen auf den Ordnernamen; das Umbenennen eines Ordners, den Spieler bereits ausgewählt haben, setzt ihre Auswahl daher auf den leeren Platz zurück.
- Die mitgelieferten JSON-Dateien enthalten nachgestellte Kommas. Der JSON-Parser des Spiels akzeptiert sie; strenge Validatoren lehnen sie ab.
