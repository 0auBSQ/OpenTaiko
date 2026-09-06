<!-- guides/puchicharas.md -->

# Puchicharas hinzufügen

Ein Puchichara ist der kleine Begleiter, der während des Spiels neben der Gauge hüpft. Jeder Puchichara ist ein Ordner unter `Global/PuchiChara/` im Installationsordner des Spiels, der ein Sprite-Sheet mit zwei Frames und einige optionale JSON-Dateien enthält. Sie schreiben keinen Code: Legen Sie den Ordner mit den richtigen Dateinamen an, und das Spiel erkennt ihn beim nächsten Start.

Kompatibilität: OpenTaiko 0.6.1 lädt für 0.6.0 erstellte Puchicharas weiterhin ohne Änderungen.

## Bevor Sie beginnen

- OpenTaiko 0.6.1 ist installiert. Das Spiel liest Puchicharas aus `Global/PuchiChara/` neben der ausführbaren Datei des Spiels, und alle Skins nutzen den Ordner gemeinsam.
- Ein Bildeditor, der PNG mit Transparenz exportiert.
- Ein Texteditor für die JSON-Dateien.
- Optional: ein kurzer `.ogg`-Clip für `Welcome.ogg`.

## Schritt 1: Ordner anlegen

Das Spiel listet beim Start die Unterordner von `Global/PuchiChara/` auf und behandelt jeden als Puchichara. Der Ordnername ist die Identität: Das Spiel schreibt ihn in den Spielstand, wenn der Spieler den Puchichara auswählt, und hält die Freischaltung unter ihm fest. Wählen Sie einen stabilen Namen: Nach einer Umbenennung fallen bestehende Auswahlen auf den ersten Ordner zurück, und die festgehaltene Freischaltung passt nicht mehr. Die mitgelieferten Ordner verwenden ein Sortierpräfix, zum Beispiel `00 - None`, `01a - OpenTaiko-Kun` und `02 - Bol`. Das Spiel sortiert die Liste nicht, sodass das Präfix die Dateisystemreihenfolge vorhersehbar hält. Der erste Ordner ist der Rückfall, wenn ein Spielstand auf einen nicht mehr existierenden Ordner verweist; behalten Sie daher `00 - None` an erster Stelle.

```
Global/PuchiChara/
    00 - None/
    01a - OpenTaiko-Kun/
    02 - Bol/
    99 - MyMascot/          <-- Ihr neuer Ordner
```

## Schritt 2: Sprite-Sheet zeichnen (Chara.png)

Das Spiel zeichnet den Begleiter aus `Chara.png`, einem horizontalen Sprite-Sheet. Das Frame-Layout stammt aus dem Skin-Wert `Game_PuchiChara`, der standardmäßig `256,256,2` ist (Frame-Breite, Frame-Höhe, Frame-Anzahl), sodass die mitgelieferten Sheets 512x256 Pixel groß sind: zwei 256x256-Frames nebeneinander, Frame 0 links. Während des Spiels wechselt das Spiel zwischen den Frames und fügt ein vertikales Hüpfen hinzu (Leerlaufhüpfen in Menüs, beatsynchrones Hüpfen im Spiel), sodass die beiden Frames zwei Posen desselben Charakters sein sollten. Verwenden Sie einen transparenten Hintergrund. Fehlt `Chara.png`, wird der Eintrag trotzdem geladen, zeichnet aber nichts.

Die mitgelieferten Ordner enthalten außerdem eine `Chara.xcf` (GIMP-Quelldatei) und eine `PuchiConfig.txt`. Das Spiel liest keine davon.

```ini
Chara.png : 512 x 256 PNG, Transparenz
  +-----------------+-----------------+
  |    Frame 0      |    Frame 1      |
  |   256 x 256     |   256 x 256     |
  +-----------------+-----------------+
```

## Schritt 3: Metadata.json schreiben

Legen Sie `Metadata.json` mit diesen Feldern an:

- `name`, `author`, `description`: jeweils ein einfacher String oder ein lokalisiertes Objekt `{ "strings": { "default": "...", "ja": "...", ... } }`, wobei `default` der Rückfall ist und die anderen Schlüssel Sprachcodes des Spiels sind.
- `rarity`: eines von `Poor`, `Common`, `Uncommon`, `Rare`, `Epic`, `Legendary`, `Mythical`. Die Seltenheit legt die Farbe und die Stufe der Freischaltbenachrichtigung fest. Jede Seltenheit hat einen Münzmultiplikator von 1, sodass sie die Einnahmen nicht verändert. Ein unbekannter Wert verhält sich wie `Common`.

Fehlt die Datei, wird der Eintrag mit dem Namen `(None)`, der Seltenheit `Common` und dem Autor `(None)` geladen.

```json
{
    "name": {
        "strings": {
            "default": "MyMascot",
            "ja": "マイマスコット"
        }
    },
    "rarity": "Rare",
    "description": {
        "strings": {
            "default": "A friendly companion.\nWaves during play."
        }
    },
    "author": "YourName"
}
```

## Schritt 4 (optional): Effects.json hinzufügen

`Effects.json` gibt dem Puchichara Gameplay-Effekte. Alle Felder sind standardmäßig aus, wenn die Datei fehlt:

- `allpurple` (bool): Große Don- und Ka-Noten werden zu violetten Noten, die beide Trommeln akzeptieren.
- `autoroll` (int): automatische Treffer pro Sekunde bei Trommelwirbeln und Ballons. Jeder Wert über 0 setzt den Münzmultiplikator auf 0.
- `showadlib` (bool): zeigt versteckte ADLIB-Noten an. Multipliziert Münzen mit 0,9.
- `splitlane` (bool): zeichnet Don- und Ka-Noten auf getrennten Spuren.

Lassen Sie die Datei bei einem rein kosmetischen Begleiter weg.

```json
{
    "allpurple": false,
    "autoroll": 0,
    "showadlib": false,
    "splitlane": false
}
```

## Schritt 5 (optional): Welcome.ogg und Render.png hinzufügen

- `Welcome.ogg`: ein Sprachclip, den das Spiel in der Lautstärkegruppe Stimme lädt. Der eingebaute Raum-Bildschirm des Spiels spielt ihn ab, wenn der Spieler den Puchichara auswählt; Lua-Stages können nicht darauf zugreifen, sodass ein Skin mit eigenem Raum-Bildschirm ihn nicht abspielt.
- `Render.png`: ein Standbild in voller Größe, das Lua-Stages als Porträt zeichnen können (die `render`-Textur eines `PUCHICHARALIST`-Eintrags). Es ist ein einzelnes Bild beliebiger Größe. Keiner der mitgelieferten Puchicharas enthält eines.

Beide Dateien sind optional.

```
MyMascot/
    Chara.png       (erforderlich, das animierte Sprite-Sheet)
    Metadata.json   (Name, Seltenheit, Autor, Beschreibung)
    Effects.json    (optionale Gameplay-Effekte)
    Unlock.json     (optionale Freischaltbedingung)
    Welcome.ogg     (optionaler Sprachclip)
    Render.png      (optionales Porträt)
```

## Schritt 6 (optional): Freischaltbedingung hinzufügen (Unlock.json)

Ohne `Unlock.json` ist der Puchichara sofort verfügbar. Um ihn zu sperren, fügen Sie eine `Unlock.json` mit den Feldern `condition`, `type`, `values` und `references` hinzu; Format und Bedingungs-IDs sind dieselben wie bei Songs und Charakteren (siehe die Anleitung zu Freischaltungen). Das Beispiel unten schaltet frei, sobald der Spieler insgesamt 500 Münzen verdient hat. Mitgelieferte Beispiele: OpenTaiko-Kun kostet 100 Münzen (`"condition": "ch"`), Bol verlangt 20 geschaffte Charts des Chart-Autors `bol` (`"condition": "sc"`), und Tinyfox verlangt einen gespielten Song im Genre `Project Outfox Serenity` (`"condition": "sg"`).

Münzbedingungen kauft der Spieler im Raum-Bildschirm. Die anderen Bedingungen prüft das Spiel nach jedem Spiel auf dem Ergebnisbildschirm; erfüllt der Spieler eine, fügt es den Puchichara zur Freischaltliste des Spielstands hinzu und zeigt eine Benachrichtigung an.

```json
{
    "condition": "ce",
    "type": "me",
    "values": [
        500
    ]
}
```

## Schritt 7: Neu starten und auswählen

Starten Sie das Spiel neu (oder laden Sie den Skin aus den Einstellungen neu), damit das Spiel die Liste neu aufbaut. Öffnen Sie den Raum-Bildschirm und wählen Sie den neuen Eintrag aus der Puchichara-Liste. Einmal ausgewählt, erscheint er während des Spiels neben der Gauge. Erscheint während des Spiels kein Begleiter, prüfen Sie, ob die Option `Draw PuchiChara` in den Systemeinstellungen aktiviert ist.

## Fehlerbehebung und Hinweise

- Dateinamen sind exakt: `Chara.png`, `Metadata.json`, `Effects.json`, `Unlock.json`, `Welcome.ogg`, `Render.png`. Das Spiel ignoriert eine falsch benannte Datei und wendet den Standard an.
- Spielstände und Freischaltdatensätze speichern den Ordnernamen unverändert. Das Umbenennen eines Ordners lässt bestehende Auswahlen auf den ersten Ordner zurückfallen.
- Das Spiel zerlegt das Sprite-Sheet mit der Frame-Größe `Game_PuchiChara` des Skins (Standard `256,256,2`). Ein Sheet anderer Größe zerlegt es mit denselben Zahlen, sodass die Frames beschnitten oder verschoben herauskommen. Wenn ein Skin `Game_PuchiChara` überschreibt, richten Sie sich nach diesem Wert. Der mitgelieferte Skin überschreibt es nicht.
- `autoroll` über 0 setzt Münzgewinne auf null, und `showadlib` reduziert sie auf 0,9, sodass ein kosmetischer Begleiter, der eines von beiden setzt, die Einnahmen seines Spielers verringert.
- Die mitgelieferten JSON-Dateien enthalten nachgestellte Kommas. Der JSON-Parser des Spiels akzeptiert sie; strenge Validatoren lehnen sie ab.
- Das Spiel baut die Liste einmal beim Start und beim Neuladen des Skins auf. Ein Ordner, der hinzugefügt wird, während das Spiel läuft, erscheint nach dem nächsten Start oder Neuladen des Skins.
