<!-- guides/skins.md -->

# Skins und Themes hinzufügen

Ein Skin ist ein Ordner unter dem Verzeichnis `System/` des Spiels. Er liefert die Grafiken, Sounds, Schriften, Layout-Werte, Locale-Dateien und die Lua-Module, die jeden Bildschirm zeichnen. Diese Anleitung erklärt, was einen Ordner zu einem Skin macht, die Schlüssel der `SkinConfig.ini`, die Ordnerstruktur, den Lua-Modulbaum und seinen Lebenszyklus sowie die Installation und Auswahl eines Skins. Die API-Referenz behandelt die Lua-API selbst (Zeichnen, Sound, Eingabe und so weiter).

Ein Skin enthält auch die Lua-Module, die jeden Bildschirm ausführen. Das Spiel lädt bestimmte Module per Name und springt zu bestimmten Stages, sodass die Skin-Auswahl einen von Grund auf neu erstellten Skin zwar auflistet, das Spiel ihn aber nicht ausführen kann. Beginnen Sie mit einer Kopie des mitgelieferten Skins.

## Bevor Sie beginnen

- OpenTaiko 0.6.1 ist installiert, mit dem mitgelieferten Skin `System/Open-World Memories/`.
- Ein Texteditor für `SkinConfig.ini`, die eingebundenen `*Config.ini`-Dateien und die Lua-Module.
- Grundkenntnisse in Lua, wenn Sie das Verhalten von Bildschirmen ändern möchten. Ein reines Re-Texturing (Ersetzen von PNG- und OGG-Dateien und Bearbeiten von `.ini`-Werten) benötigt kein Lua.

## Schritt 1: Verstehen, was einen Ordner zu einem Skin macht

Beim Start listet das Spiel die Unterordner von `System/` auf. Ein Ordner zählt nur dann als Skin, wenn darin `Graphics/1_Title/Background.png` existiert; jeden anderen Ordner überspringt das Spiel. Fehlt der ausgewählte Skin-Ordner, fällt das Spiel auf `System/Default/` zurück, dann auf den ersten gültigen Skin in alphabetischer Reihenfolge, dann auf `System/` selbst.

Diese Prüfung sorgt nur dafür, dass der Ordner aufgelistet wird. Schritt 8 nennt die Module, die zusätzlich existieren müssen, bevor der Skin läuft.

```
System/
  Open-World Memories/         <- der mitgelieferte Skin
  My New Skin/                 <- Ihr Skin
    Graphics/
      1_Title/
        Background.png          <- erforderlich, damit der Ordner aufgelistet wird
    SkinConfig.ini
```

## Schritt 2: Den mitgelieferten Skin kopieren

Kopieren Sie `System/Open-World Memories/` in einen neuen Nachbarordner, zum Beispiel `System/My New Skin/`. Die Kopie enthält alles, was das Spiel braucht: `Graphics/`, `Sounds/`, `Fonts/`, `Locales/`, `Modules/`, `ThemeSettings.json`, `SkinConfig.ini` und die darin eingebundenen `*Config.ini`-Dateien. Der Ordnername ist die Identität des Skins (das Spiel speichert ihn als ausgewählten Skin, und die Skin-Auswahl zeigt ihn an); halten Sie ihn daher dateisystemsicher. Bearbeiten Sie dann `SkinConfig.ini`, damit die Metadaten Ihren Skin beschreiben.

## Schritt 3: SkinConfig.ini bearbeiten

`SkinConfig.ini` ist eine `Key=Value`-Datei, eine Einstellung pro Zeile. Der Parser entfernt führende Leerzeichen und Tabulatoren, behandelt Zeilen, die mit `;` beginnen, als Kommentare und liest eine Zeile nur, wenn sie genau ein `=` enthält. Der Schlüsselvergleich ist exakt, und der Parser ignoriert einen unbekannten Schlüssel, ohne einen Fehler zu melden. Die Schlüssel auf Skin-Ebene sind:

- `Name=`: Anzeigename. Nur Metadaten; der Ordnername wählt den Skin aus.
- `Version=`, `Creator=`: frei wählbare Strings (Standard `Unknown`). Das Spiel validiert sie nicht.
- `DefaultLocale=`: Locale-ID, die das Spiel verwendet, wenn die aktive Spielsprache keine Datei unter `Locales/` hat (Standard `en`).
- `Resolution=W,H`: die Auflösung, für die Sie die Layout-Werte erstellen (Standard `1280,720`). Der mitgelieferte Skin verwendet `1920,1080`.
- `Resolutions=`: auswählbare Render-Skalierungsfaktoren (Schritt 4).
- `AIBattleCharacter=`: Charakterordner für den KI-Gegner (Schritt 5).
- `FontName<LANG>=` und `BoxFontName<LANG>=`: Schriftdatei pro Spielsprache, wobei `<LANG>` der großgeschriebene Sprachcode ist (`EN`, `JA`, `FR`, `ES`, `NL`, `DE`, `RU`, `KO`, `ZH`). Der Pfad ist relativ zur Skin-Wurzel (ein absoluter Pfad funktioniert ebenfalls), und die Datei muss existieren, andernfalls verwirft der Parser den Schlüssel.

Jeder andere Schlüssel (`Game_*`, `Result_*`, `Title_*` und so weiter) ist ein Bildschirm-Layout-Wert. Derselbe Parser liest sie, weshalb sie in den eingebundenen Dateien aus Schritt 6 liegen können.

```ini
;Skin-Informationen
Name=My New Skin
DefaultLocale=en
Version=1.0.0
Creator=Your Name
Resolution=1920,1080
;Auswählbare Render-Skalierungsfaktoren (<=1; Dezimalzahl oder a/b-Bruch, kommagetrennt). 1 ist immer verfügbar und der Standard.
Resolutions=1,2/3,1/3
;Charakterordner für den KI-Kampf-Slot.
AIBattleCharacter=10v2 - AItritus
FontNameEN=Fonts/MPLUSRounded1c-Medium.ttf
FontNameJA=Fonts/MPLUSRounded1c-Medium.ttf
BoxFontNameEN=Fonts/MPLUSRounded1c-Regular.ttf
BoxFontNameJA=Fonts/MPLUSRounded1c-Regular.ttf
```

## Schritt 4: Die Option Resolutions

`Resolutions=` ist eine kommagetrennte Liste der Render-Skalierungsfaktoren, die das Einstellungsmenü anbietet. Das Spiel rendert mit `Resolution` mal dem gewählten Faktor und skaliert das Ergebnis auf das Fenster hoch; die Fenstergröße ändert sich nicht. Jedes Token ist eine Dezimalzahl (`0.5`) oder ein Bruch (`2/3`). Der Parser verwirft Tokens außerhalb des Bereichs 0 < Wert <= 1, nicht parsbare Tokens und Duplikate, fügt `1` hinzu, falls es fehlt, und sortiert die Liste mit `1` an erster Stelle. Das Trennzeichen muss ein Komma sein, weil ein Semikolon eine Kommentarzeile beginnt. Das Einstellungsmenü zeigt jeden Eintrag mit seiner Pixelgröße an, zum Beispiel `2/3 (1280x720)` bei einem 1920x1080-Skin.

```ini
Resolution=1920,1080
Resolutions=1,2/3,1/3
; ergibt die Optionen:
;   1     -> 1920x1080  (Standard)
;   2/3   -> 1280x720
;   1/3   -> 640x360
```

## Schritt 5: Die Option AIBattleCharacter

`AIBattleCharacter=` benennt den Ordner unter `Global/Characters/`, den das Spiel für den KI-Gegner im KI-Kampf-Modus verwendet. Der Standard ist `10v2 - AItritus`. Der benannte Ordner muss existieren.

```ini
;Charakterordner für den KI-Kampf-Slot.
AIBattleCharacter=10v2 - AItritus
```

## Schritt 6: Die Konfiguration mit #include aufteilen

Trifft der Parser auf eine Zeile der Form `#include SomeFile.ini`, liest er diese Datei an Ort und Stelle ein, rekursiv. Der Pfad ist relativ zur Skin-Wurzel. Die mitgelieferte `SkinConfig.ini` enthält nur die Metadaten- und Schriftschlüssel und bindet dann eine Datei pro Bildschirm ein. Behalten Sie diese Zeilen beim Kopieren des Skins bei, und bearbeiten Sie die einzelnen `*Config.ini`-Dateien, um einen Bildschirm anzupassen.

```
; Ende der SkinConfig.ini (mitgelieferter Skin, in dieser Reihenfolge)
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

## Schritt 7: Die Skin-Ordnerstruktur kennenlernen

Mit dem mitgelieferten Skin als Referenz enthält die Skin-Wurzel:

- `Graphics/`: Bilder, gruppiert in nummerierten Ordnern pro Bildschirm (`0_Startup`, `1_Title`, `2_Config`, `3_DaniSelect`, `5_Game`, `6_Result`, `7_DanResult`, `7_Exit`, `8_TowerResult`, `10_Heya`, `12_OnlineLounge`, `13_TowerSelect`, `15_OpenEncyclopedia`) plus einige gemeinsame Bilder auf oberster Ebene. Animierte Hintergründe sind `Script.lua`-Dateien, die neben den Bildern des Ordners liegen, zu dem sie gehören (zum Beispiel `Graphics/0_Startup/Script.lua` und die Ordner unter `Graphics/5_Game/5_Background/`).
- `Sounds/`: Systemsounds und BGM, die das Spiel über feste Dateinamen lädt, zum Beispiel `Sounds/Move.ogg`, `Sounds/Decide.ogg`, `Sounds/Cancel.ogg`, `Sounds/BGM/Title.ogg`, `Sounds/BGM/SongSelect.ogg`, `Sounds/BGM/Result.ogg`. Fehlt eine Datei, wird dieser Sound nicht abgespielt.
- `Fonts/`: die `.ttf`-Dateien, auf die die `FontName`-Schlüssel verweisen.
- `Locales/`: eine JSON-Datei pro Sprache (`en.json`, `ja.json`, ...) mit der Form `{ "Entries": { "KEY": "text" } }`. Diese Strings beschriften die eigenen Einstellungen des Skins, und Lua liest sie über `THEME:GetSkinString(key)`. Fehlt ein Schlüssel in der aktiven Sprache, schlägt das Spiel ihn in der `DefaultLocale`-Datei nach.
- `Modules/`: der Lua-Modulbaum (Schritt 8).
- `ThemeSettings.json`: ein Array von Einstellungen, die der Optionsbildschirm unter Theme-Einstellungen anzeigt. Jeder Eintrag hat `id`, `type` (`bool`, `int`, `double`, `string` oder `enum`), `scope` (`global`, der Standard, oder `save` für einen Wert pro Spielstand), lokalisierte `label` und `description`, `default` sowie je nach Typ `min`/`max` oder `options`.
- `SkinConfig.ini` und die eingebundenen `*Config.ini`-Dateien.
- `README.txt`, `LICENSE.md`, `Licenses/`: Dateien zur Namensnennung. Das Spiel liest sie nicht.

```
My New Skin/
  SkinConfig.ini
  ThemeSettings.json
  Graphics/           Bilder nach Bildschirm; einige Ordner enthalten eine Hintergrund-Script.lua
  Sounds/             .ogg-Systemsounds mit festen Namen und BGM/
  Fonts/              .ttf-Dateien, benannt durch die FontName-Schlüssel
  Locales/            en.json, ja.json, ... ({ "Entries": { ... } })
  Modules/            der Lua-Modulbaum (Schritt 8)
  <screen>Config.ini  Layout-Dateien, eingebunden über #include
```

## Schritt 8: Der Modules-Baum und die vom Spiel benötigten Module

Beim Laden des Skins scannt das Spiel vier Unterordner von `Modules/` und behandelt jeden direkten Unterordner darin als ein Modul, dessen Einstiegsdatei `Script.lua` ist:

- `Modules/Transitions/`: Übergänge, die zwischen Stages ablaufen. Das Spiel lädt sie zuerst, damit sie beim ersten Stage-Wechsel bereitstehen.
- `Modules/Stages/`: vollständige Bildschirme. Sie betreten eine Stage mit `Exit("stage", "<folder name>")`.
- `Modules/Activities/`: wiederverwendbare Unterbildschirme, die über einer Stage liegen (zum Beispiel `confirm_dialog`, `mod_select_dialog`, `song_select_core`).
- `Modules/ROActivities/`: schreibgeschützte Overlays, die das Spiel direkt steuert.

Das Spiel scannt `Modules/Lib/` nicht. Dateien dort laden Sie mit `require`: Der Suchpfad eines Moduls ist sein eigener Ordner, gefolgt von `Modules/Lib/`, sodass `require("dialogue")` zu `Modules/Lib/dialogue.lua` auflöst. Sie können Stages und Activities auch unter `Global/Stages/` und `Global/Activities/` im Installationsordner des Spiels ablegen; diese lädt das Spiel für jeden Skin.

Innerhalb jeder Kategorie erzeugt das Spiel zuerst jedes Modul und führt dann `onStart` auf jedem aus, in der Reihenfolge Transitions, Stages, Activities, ROActivities.

Das Spiel schlägt diese Module per Name nach, und der mitgelieferte Skin stellt sie alle bereit:

- Stages `_boot` und `_title`. Das Spiel bricht mit einem Fehler ab, wenn eine davon fehlt.
- ROActivities `modal`, `config_ui`, `nameplate`, `popup_menu`, `modicons`, `song_enum` und `danplate`.
- Transitions `default` und `song_loading`. `song_loading` läuft ab, während das Spiel einen Song lädt; das Spiel verwendet `default`, wenn `Exit` keinen Übergang benennt oder einen nicht existierenden benennt. Ein Skin ganz ohne Übergangsmodule fällt auf ein einfaches schwarzes Ausblenden zurück.

Behalten Sie all diese beim Erstellen eines Skins bei; fügen Sie Ihre eigenen Module daneben hinzu.

```
Modules/
  Transitions/   <name>/Script.lua   (zuerst geladen; "default" und "song_loading" werden vom Spiel verwendet)
  Stages/        <name>/Script.lua   ("_boot" und "_title" erforderlich)
  Activities/    <name>/Script.lua
  ROActivities/  <name>/Script.lua   (modal, config_ui, nameplate, popup_menu, modicons, song_enum, danplate erforderlich)
  Lib/           gemeinsame .lua-Dateien, erreichbar über require, nicht gescannt
```

## Schritt 9: Die Script.lua einer Stage und ihr Lebenszyklus

`Script.lua` läuft einmal, wenn das Spiel das Modul erzeugt, mit den bereits definierten globalen Objekten der Engine (`TEXTURE`, `SOUND`, `INPUT`, `CONFIG`, `THEME` und den übrigen). Das Spiel schlägt dann globale Funktionen per Name nach und ruft sie auf. Für eine Stage:

- `onStart()`: einmal, wenn der Skin geladen wird. Läuft als Coroutine, sodass aufwendiges Laden `coroutine.yield()` oder die `LOADING`-Hilfsfunktionen aufrufen kann, um die Arbeit hinter dem Ladebalken auf mehrere Frames zu verteilen.
- `activate()`: jedes Mal, wenn das Spiel die Stage betritt. Ebenfalls eine Coroutine. Das Spiel aktualisiert die globalen Objekte `CHARACTERLIST` und `PUCHICHARALIST` unmittelbar davor; bauen Sie also alles, was von ihnen abhängt, hier auf. In `onStart` sind sie noch leer.
- `update(timestamp)`: jeden Frame. Geben Sie `Exit(target, name, transition)` zurück, um die Stage zu verlassen. `target` ist `"title"`, `"play"`, `"stage"` (mit `name` = ein Stage-Ordner) oder `"legacy"` (mit `name` = `heya`, `config`, `exit` oder `onlinelounge`); `transition` ist ein Ordner unter `Modules/Transitions/` und ist standardmäßig `default`.
- `draw()`: jeden Frame.
- `deactivate()`: wenn das Spiel die Stage verlässt.
- `afterSongEnum()`: wenn die Enumeration der Songliste abgeschlossen ist.
- `onDestroy()`: wenn das Spiel den Skin abbaut.

Alle sind optional; eine Funktion, die Sie nicht definieren, überspringt das Spiel. Activities, ROActivities und Transitions folgen demselben Muster mit ihren eigenen Hook-Sätzen.

```lua
-- Modules/Stages/my_stage/Script.lua
function onStart()
  -- einmalige Einrichtung; darf bei aufwendigem Laden coroutine.yield() aufrufen
end

function activate()
  -- läuft bei jedem Betreten der Stage
end

function update(ts)
  if INPUT:Pressed("Cancel") then
    return Exit("stage", "_title")   -- diese Stage verlassen
  end
  return nil
end

function draw()
  -- Rendern pro Frame
end

function deactivate() end
function afterSongEnum() end
function onDestroy() end
```

## Schritt 10: Ein Modul mit lang/ lokalisieren

Ein Modul kann seine eigenen Übersetzungen in einem Unterordner `lang/` neben `Script.lua` halten. Da der eigene Ordner des Moduls auf seinem `require`-Pfad liegt, löst `require("lang.ja")` zu `lang/ja.lua` auf. Der mitgelieferte Skin tut dies für seine größeren Stages (zum Beispiel `Modules/Stages/myroom/lang/ja.lua` und `Modules/Stages/intro_nokon/lang/ja.lua`) über die Hilfsdatei `Modules/Lib/i18n.lua`. Dies ist getrennt vom skinweiten Ordner `Locales/` aus Schritt 7.

```
Modules/Stages/my_stage/
  Script.lua
  lang/
    ja.lua        -- require("lang.ja")
```

## Schritt 11: Skin installieren und auswählen

Legen Sie den Ordner unter `System/` ab. Öffnen Sie die Einstellungen, gehen Sie zum Abschnitt Darstellung und wählen Sie den Skin in der Option Skin; die Auswahl listet jeden gültigen Skin nach Ordnername auf und zeigt seine `Graphics/1_Title/Background.png` als Vorschaubild. Wenn Sie den Skin wechseln, baut das Spiel den aktuellen ab, lädt den neuen und lädt alle seine Lua-Module hinter einem Ladebalken neu.

Das Spiel schreibt die Wahl als `SkinPath=` in `Config.ini`, relativ zu `System/`. Es schreibt den bloßen Ordnernamen (zum Beispiel `SkinPath=My New Skin\` unter Windows) und akzeptiert auch die im Kommentar der Datei gezeigte Form `./My New Skin/`.

```ini
; In Config.ini (geschrieben, wenn ein Skin im Spiel ausgewählt wird):
; Skin-Ordnerpfad, relativ zu System/
SkinPath=My New Skin\
```

## Fehlerbehebung und Hinweise

- Die Auswahl listet den Skin nicht auf: `Graphics/1_Title/Background.png` fehlt, oder der Ordner liegt nicht direkt unter `System/`.
- Das Spiel meldet direkt nach der Auswahl des Skins einen Fehler: Ein erforderliches Modul fehlt (Schritt 8), oder eines davon hat einen Lua-Fehler ausgelöst. Testen Sie einen Skin, indem Sie zu ihm wechseln.
- Ein `SkinConfig.ini`-Schlüssel hat keine Wirkung: Der Schlüssel ist falsch geschrieben, die Zeile enthält mehr als ein `=`, oder der Wert konnte nicht geparst werden. Der Parser ignoriert unbekannte Schlüssel, ohne sie zu melden.
- `Resolutions=` zeigt nur `1`: Die Liste hat Semikolons verwendet (ein Kommentarzeichen), oder jeder Wert lag außerhalb von 0 < Wert <= 1.
- Ein Schriftschlüssel hat keine Wirkung: Der Dateipfad existiert relativ zur Skin-Wurzel nicht.
- `CHARACTERLIST` oder `PUCHICHARALIST` ist in `onStart` leer: Das Spiel befüllt sie, nachdem es die Module erzeugt hat. Verwenden Sie sie aus `activate`.
- Das Umbenennen des Skin-Ordners ändert seine Identität; `SkinPath` in `Config.ini` muss auf den neuen Namen zeigen.
- `Name=`, `Version=` und `Creator=` sind rein informativ. Das Spiel führt keine Kompatibilitätsprüfung auf ihnen durch.
