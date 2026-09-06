<!-- docs/README.md -->

# OpenTaiko-Dokumentation

<span class="badge-new">Spielversion 0.6.1</span>

<div class="callout warn">Diese Dokumentation befindet sich noch in der Überprüfung und kann sich bis zur Veröffentlichung noch ändern.</div>

OpenTaiko zeichnet fast jeden Bildschirm außerhalb des eigentlichen Spielgeschehens mit Lua-Skripten, die in einem Skin mitgeliefert werden: die Menüs, die Songauswahl, die Raum- und Story-Szenen, die Hintergründe, den Ergebnisbildschirm. Diese Seite dokumentiert die Lua-API, die diese Skripte verwenden, und führt durch die häufigsten Aufgaben, die ein Skin-Autor erledigen möchte.

## Wo Sie anfangen sollten

- Falls Sie noch kein Modul geschrieben haben, lesen Sie [So funktionieren Module](getting-started.md): die Ordnerstruktur eines Skins, die Modularten und die Lebenszyklus-Callbacks, die ein Skript erhält.
- Um eine Funktion nachzuschlagen, öffnen Sie die [API-Referenz](api/) und wählen Sie in der Seitenleiste eine Kategorie.
- Für ein konkretes Ziel führen die Anleitungen Schritt für Schritt durch [Charaktere](guides/characters.md), [Puchicharas](guides/puchicharas.md), [Skins und Themes](guides/skins.md) sowie [Chart-Freischaltungen](guides/unlockables.md).

## Was Sie bauen können

| Bereich | Wo es liegt | Was es ist |
| --- | --- | --- |
| Module | Der Ordner `Modules/` des Skins | Stages (vollständige Bildschirme mit eigener Eingabe, eigenem Zeichnen und eigenem Zustand: eine Songauswahl, ein Raum, eine Story-Szene), Activities (Unterbildschirme und Overlays wie Dialoge und Namensschilder) und Übergänge (die Überblendungen und Ladebildschirme zwischen Stages). |
| Hintergründe | Der Ordner `Graphics/` des Skins | Skripte, die einen Bildschirm dekorieren: den Startbildschirm, den Raum, den Gameplay-Hintergrund und den Mob, den Ergebnisbildschirm. |
| Skins und Themes | `System/`, neben dem Spiel | Ein vollständiges visuelles Paket, das ein Spieler installiert und auswählt, mit den Theme-Einstellungen, die es im Optionsbildschirm bereitstellt. |
| Charaktere und Puchicharas | `Global/Characters/` und `Global/PuchiChara/`, von allen Skins gemeinsam genutzt | Die Tänzer und die kleinen Maskottchen, die während des Spiels reagieren. |
| Chart-Freischaltungen | Der Songordner, neben dem Chart | Eine `Unlock.json`, die einen Song gesperrt hält, bis der Spieler ihn sich verdient hat. |

## Experimentelle Abschnitte

Einige Seiten tragen in der Seitenleiste die Kennzeichnung <span class="badge-exp">Experimentell</span>. Die Funktionen, die sie beschreiben, funktionieren in der aktuellen Version, aber ihre API kann sich zwischen Versionen noch ohne Übergangsfrist ändern. Ein Skin, der auf sie angewiesen ist, sollte angeben, für welche Spielversion er gedacht ist, und Sie sollten ihn nach jedem Update erneut testen.

## Sprachen

Die Dokumentation existiert in jeder Sprache, die OpenTaiko mitliefert; wählen Sie eine mit der Sprachauswahl oben in der Seitenleiste. Die Website zeigt eine Seite auf Englisch an, bis eine Übersetzung vorliegt.
