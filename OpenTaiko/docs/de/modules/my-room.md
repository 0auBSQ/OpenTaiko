<!-- modules/my-room.md -->

# Mein Zimmer

Mein Zimmer ist eine globale Stage, `my_room` in `Global/Modules/Stages/`: das Zimmer des Spielers, in dem er herumläuft, das er mit dem einrichtet, was ihm die Pagode, der Tresor und der Münzladen geben, und das er für Online-Besucher öffnet. Jeder Skin kann es öffnen, und jeder Skin zeigt dasselbe Zimmer, weil seine Daten im Spielstand liegen.

## Betreten und Verlassen

Setzen Sie den gemeinsamen String `my_room_return` auf die Stage, zu der Mein Zimmer zurückkehren soll, und wechseln Sie dann zu `my_room`:

```lua
SHARED:SetSharedString("my_room_return", "my_menu")               -- back to my_menu
-- or, with the transition to play on the way back:
SHARED:SetSharedString("my_room_return", "my_menu|my_transition")
return Exit("stage", "my_room")
```

Mein Zimmer liest den String beim Betreten und leert ihn. Ist er leer, führt der Rückweg zum Titelbildschirm (`_title`).

Der gemeinsame String `my_room_place` merkt sich, wessen Zimmer es war und wo der Spieler stand, über Ausflüge aus dem Zimmer und zurück: Die Placeholder Matrix (ein Möbelstück, das zu besuchende Stages auflistet) und die Kapsel (eines, das eine andere Stage öffnet) führen beide aus dem Zimmer hinaus. Ein Titelbildschirm kann ihn leeren (auf `""` setzen), damit der nächste Besuch an der Tür beginnt.

Open-World Memories behält eine Weiterleitungs-Stage, `myroom`, für die alten Aufrufe `Exit("stage", "myroom")` und den Exit `"heya"` der Engine; sie verschwindet mit der offiziellen Veröffentlichung, daher sollte neuer Code zu `my_room` wechseln.

## Seine Daten

| Schlüssel | Art | Inhalt |
| --- | --- | --- |
| `my_room` | Spielstand-String | Das Zimmer als JSON `{ v, room, jukebox }` (`sf:GetGlobalString("my_room")`). Nur für den eigenen Slot des Spielstands geschrieben: nie für einen Online-Besucher oder einen leeren Slot. |
| `.myroom_<id>` | Zähler | Möbel, die eine andere Stage vergibt: Mein Zimmer fügt sie beim nächsten Besuch einmal dem Bestand des Zimmers hinzu und führt Buch darüber, was es schon übernommen hat. |
| `.myroom_held_<id>` | Zähler | Wie viele Exemplare dieses Stücks der Spielstand besitzt. Mein Zimmer hält den Wert aktuell; eine Stage, die ein Stück vergibt, erhöht ihn ebenfalls sofort. |
| `.myroom_iw`, `.myroom_ih`, `.myroom_items` | Zähler | Breite und Höhe des Zimmers und die Anzahl der platzierten Stücke, von Mein Zimmer für die Anzeige in einer Spielstandliste geschrieben (nur lesen). |
| `myroom_thumb_<id>` | gemeinsame Textur | Das Bild eines Möbelstücks aus dem Laden oder einer Belohnung, beim Start von Mein Zimmer erzeugt, das Kauf- und Belohnungs-Pop-ups zeigen. |

Um aus Ihrer eigenen Stage ein Möbelstück zu vergeben, addieren Sie 1 zu beiden Zählern, wie es [Pagoda.Rewards](pagoda.md) und [Vault.Chests](vault.md) tun. Solange der String `my_room` nicht gesetzt ist, liest eine vorübergehende Migration das Zimmer aus dem alten Speicher von Open-World Memories; sie wird mit der offiziellen Veröffentlichung entfernt.

## Worauf es aufbaut

- Seine Menüs und Dialoge verwenden die UI-Bibliothek des Skins über [UI.Pick](ui.md), bei der ersten Verwendung gewählt (PopUI in Open-World Memories, SimpleUI in einem Skin, der keine nennt), und seine Münzanzeige ist das Münz-Widget dieser Bibliothek.
- Seine Strings, einschließlich der Möbelnamen (`lang/<code>/items.json`, mit der Möbel-ID als Schlüssel), liegen in seinem eigenen Ordner `lang/`, in den neun Sprachen.
- Der Möbelkatalog ist `data/furniture.json` im Ordner der Stage.
- Die Placeholder Matrix listet nur die Debug-Stages auf, die in `Global/Modules/Stages/` vorhanden sind. Diese Debug-Stages und die ROActivity `video_player`, die der Fernseher verwendet, sind ebenfalls globale Module.
- Die Kapsel öffnet die Stage `mysterypod`, wenn sie in `Global/Modules/Stages/` existiert oder wenn die eigene Stage `mysterypod` eines Skins in ihrem `onStart` den gemeinsamen String `mysterypod_installed` auf `"1"` setzt.
- Online-Besuche verwenden die Zimmer-ID `"myroom"` mit Version 2 ihrer Nutzdaten, wie vor dem Umzug.
