<!-- modules/custom-stages.md -->

# Eigene Stages

Eine eigene Stage ist eine Stage, die jeder bauen, teilen und installieren kann und die das Menü Eigene Stages eines Skins auflistet. Sie deklariert den Namespace `CStage` mit [STAGES:SetNamespace](../api/activities.md) und kehrt über den Shared String `custom_stage_return` zu der Stage zurück, die sie geöffnet hat. `Stages.CustomStages` erledigt beides und listet die eigenen Stages für ein Menü auf. `Global/Modules/Stages/custom_stage_tutorial` ist eine davon: eine Anleitung im Spiel zum Erstellen eigener Stages.

## Deklarieren

Oben in Script.lua, außerhalb jeder Funktion:

```lua
-- Global/Modules/Stages/lantern_walk/Script.lua, at the top
local CS = require("Stages.CustomStages")

CS.declare{
    title       = CS.allLanguages("menu", "title"),       -- lang/<code>/menu.json, every language
    description = { en = "A calm walk down a street of paper lanterns." },
}
```

`CS.declare(info)` ist `STAGES:SetNamespace("CStage", info)`. Die Menüs lesen zwei Felder von info:

| Feld | Zeigt |
| --- | --- |
| `title` | Den Namen der Stage in der Liste: ein String oder eine Tabelle nach Sprachcode. Der Ordnername, wenn er fehlt. |
| `description` | Eine oder zwei Zeilen unter der Liste, solange die Stage gewählt ist; optional. |

Eine Tabelle nach Sprachcode liefert den Text in der Sprache des Spiels, sonst auf Englisch, sonst in irgendeiner Sprache, die sie enthält. Weitere Felder gehören Ihnen: Die Menüs reichen info unverändert weiter.

## Der Weg zurück

Das Menü, das eine eigene Stage öffnet, setzt den Shared String `custom_stage_return` auf die Stage für den Rückweg, `"<stage>"` oder `"<stage>|<transition>"`, und wechselt dann zur eigenen Stage. Diese liest und leert den String beim Betreten und verlässt sich zu dieser Stage, oder zu `_title`, wenn der String leer ist:

```lua
local back, backTransition

function activate()
    back, backTransition = CS.takeReturn()   -- "_title" when the string is empty
end

function update(ts)
    if leaving then return Exit("stage", back, backTransition) end
end
```

Der String wird bei jedem Betreten gelesen. Eine eigene Stage, die zu einer ihrer anderen Stages wechselt und zurückkommt, setzt den String vor dem Verlassen erneut, wie Mein Zimmer es mit `my_room_return` tut.

## Stages.CustomStages

| Funktion | Beschreibung |
| --- | --- |
| `CS.declare(info)  -> boolean` | Deklariert die aufrufende Stage als eigene Stage: `STAGES:SetNamespace("CStage", info)`. |
| `CS.allLanguages(name, id)  -> table` | Der String `id` aus `lang/<code>/<name>.json` der aufrufenden Stage, aus jeder der neun Sprachen, die ihn hat, als Tabelle nach Sprachcode. Gedacht für `declare`, das läuft, bevor eine Sprache gewählt ist. |
| `CS.localized(value, code?)  -> string or nil` | Ein deklarierter Text in `code` (standardmäßig der Sprache des Spiels): ein String unverändert, eine Tabelle nach Sprachcode wie oben beschrieben. |
| `CS.list(code?)  -> table` | Die eigenen Stages, die das aufrufende Modul erreicht, nach Titel sortiert: `{ name, isGlobal, title, description, info }`, mit `title` und `description` bereits in der Sprache. |
| `CS.enter(name, back, transition?)` | Setzt `custom_stage_return` auf `back` und wechselt zur Stage `name`; schreiben Sie `return CS.enter(...)` in das update einer Stage. |
| `CS.takeReturn()  -> stage, transition` | Liest und leert `custom_stage_return`: die Stage (`_title`, wenn der String leer ist) und den Übergang (nil für den Standard). |
| `CS.open(o)`, `CS.update(ts)`, `CS.draw()`, `CS.isOpen()`, `CS.close()` | Die Liste als Dialog über dem Bildschirm des Aufrufers, gebaut mit der UI-Bibliothek des Skins ([UI.Pick](ui.md)). `update` gibt nil zurück, solange er offen ist, `"cancel"` oder den gewählten Eintrag von `CS.list`; in beiden Fällen ist der Dialog geschlossen. `o`: `title`, `ui` (eine Bibliothekstabelle oder ihr require-Name), `theme`, `sfx`, `navPlayer` (standardmäßig 0: Tastatur und Pad, nicht die Trommeln). Ist keine eigene Stage installiert, sagt er das über einem OK-Knopf. |
| `CS.NAMESPACE`, `CS.RETURN_KEY` | `"CStage"` und `"custom_stage_return"`. |

Seine Strings liegen in seinem eigenen `lang/`-Ordner, in den neun Sprachen ([Locale.i18n](locale.md)). Open-World Memories öffnet die Liste über den Eintrag Eigene Stages seines Titelbildschirms, über Einstellungen:

```lua
-- a menu stage: CS.open{ title = "Custom Stages" } when its entry is chosen, then
function update(ts)
    if CS.isOpen() then
        local r = CS.update(ts)
        if r ~= nil and r ~= "cancel" then return CS.enter(r.name, "_title") end
        return
    end
end

function draw()
    if CS.isOpen() then CS.draw() end
end
```

## Wo eine eigene Stage liegt

| Ordner | Läuft unter |
| --- | --- |
| `Global/Modules/Stages/<name>/` | Jedem Skin. |
| `System/<skin>/Modules/Stages/<name>/` | Nur diesem Skin. |

Gibt es einen Namen an beiden Orten, listet ein Menü im Skin die Stage des Skins und ein globales Menü die globale, also die Stage, die `Exit` öffnen würde. Das Spiel erzeugt die Stages beim Start und wenn es den Skin neu lädt; eine Stage, die bei laufendem Spiel installiert wird, erscheint also nach einem Neustart.

## Eine teilen

Eine eigene Stage ist ihr Ordner. Packen Sie den Ordner selbst als Zip, sodass das Archiv mit `<name>/Script.lua` beginnt, mit allem, was sie lädt, und den Credits ihrer Dateien. Spieler entpacken ihn nach `Global/Modules/Stages/` in ihrem Spielordner, oder nach `Modules/Stages/` eines Skins, und starten das Spiel neu. Ein Update fügt Dateien hinzu und ersetzt sie, löscht aber keine; wählen Sie also einen Ordnernamen, den keine mitgelieferte Stage verwendet.

## Das Tutorial

`custom_stage_tutorial` zeigt im Spiel die vier Dinge, die man wissen muss, um eine eigene Stage zu bauen und zu teilen: wohin ihr Ordner gehört (und wohin nicht), den Namespace `CStage`, alles Benutzte in ihrem Ordner und von außen nur die mitgelieferten Module, und wo diese Dokumentation zu lesen ist, im Tab Dokumentation des OpenTaiko Hub. Seine Seiten sind die Bilder seines `Textures/`-Ordners und die Texte seines `lang/`-Ordners, sodass sie sich Datei für Datei ersetzen lassen.
