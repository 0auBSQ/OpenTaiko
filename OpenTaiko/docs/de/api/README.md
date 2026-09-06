<!-- docs/api/README.md -->

# API-Referenz

<span class="badge-new">Spielversion 0.6.1</span>

Die Referenz für jedes globale Objekt, das die OpenTaiko-Lua-Laufzeit einem Skin zur Verfügung stellt. Wählen Sie in der Seitenleiste eine Kategorie oder beginnen Sie mit [Module und Lebenszyklus](activities.md), falls Sie noch kein Modul geschrieben haben.

## Wie eine Signatur zu lesen ist

Jeder Eintrag zeigt die Funktion so, wie Sie sie aus Lua aufrufen.

- Ein Doppelpunkt bedeutet, dass Sie die Funktion auf einem Wert aufrufen und Lua diesen Wert als verborgenes `self` übergibt: `tex:Draw(x, y)` rufen Sie auf einer zuvor geladenen Textur auf.
- Ein Punkt oder ein bloßer Name ist ein einfacher Aufruf, etwa `GetSaveFile(0)`.
- Der Typ nach dem Pfeil ist der Rückgabewert des Aufrufs: `TEXTURE:CreateTexture(path) -> texture` liefert ein Handle, das Sie behalten und später zeichnen.

Globale Namen sind in Großbuchstaben geschrieben (`TEXTURE`, `SOUND`, `INPUT`). Die Laufzeitumgebung stellt sie in jedem Modulskript bereit; Sie erzeugen sie nie selbst.

## Kategorien

| Kategorie | Inhalt |
| --- | --- |
| [Module und Lebenszyklus](activities.md) | Die Callbacks, die ein Modul erhält, sowie die Hilfsobjekte für Activities, Hintergründe, Übergänge und Counter. |
| [Grafik und Text](graphics.md) | Texturen, Canvases, Clipping, Textdarstellung, Video, Farben und Verläufe. |
| [Audio](audio.md) | Laden und Abspielen von Sounds. |
| [Eingabe](input.md) | Tastatur-, Pad- und Zeigereingabe sowie Texteingabe auf dem Bildschirm. |
| [Daten und Persistenz](data.md) | Daten, die einen Neustart überdauern, Laden von JSON und INI, gemeinsam genutzte Ressourcen. |
| [Songs und Charts](songs.md) | Die Songliste, Song-Knoten und Charts, Scores und der Aufbau von Dan-Kursen (Prüfungen). |
| [Spieler und Profile](players.md) | Spielstände, Namensschilder, Charaktere, Puchicharas, Spielzustand, Themes und Sprache. |
| [Mathematik](math.md) | Vektoren, Matrizen und Quaternionen. |
| [Online-Netzwerk](networking.md) | Das globale Objekt `NET` für OpenTaiko-Online-Sitzungen. Derzeit experimentell. |
| [3D-Engine: Rasterizer-Welt](3d.md) | Szenen, Objekte, Modelle, Lichter, Kameras, Sprites, Höhenfelder und Render-Targets. Derzeit experimentell. |
| [3D-Engine: Raytracer-Welt](3d-raytrace.md) | Der Pathtracer: Materialien, analytische Primitive und der Himmelsverlauf. Derzeit experimentell. |
| [3D-Engine: Physik](3d-physics.md) | Physikwelt, Körper und Fahrzeuge, Collider, Raycasts und Wegfindung. Derzeit experimentell. |

## Die Kennzeichnung „Experimentell“

Funktionen, die als <span class="badge-exp">Experimentell</span> gekennzeichnet sind, funktionieren heute, können sich aber zwischen Versionen ohne Übergangsfrist ändern. Die 3D-Engine und das Online-Netzwerk tragen diese Kennzeichnung derzeit; geplant ist, dass die 3D-Engine mit Version 1.0 den experimentellen Status verlässt. Wenn ein Skin von experimentellen Funktionen abhängt, testen Sie ihn nach jedem Update erneut und geben Sie an, für welche Spielversion er gedacht ist.
