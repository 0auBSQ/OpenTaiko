<!-- modules/locale.md -->

# Locale

## Locale.i18n

Die eigenen Übersetzungen eines Moduls, die neben ihm liegen statt in den `Locales/` des Skins. Texte liegen in `lang/<code>/<name>.json`, eine Datei pro Funktionsbereich, mit stabilen IDs als Schlüsseln; `lang/en/` enthält das Englische und ist der Rückfall für jede andere Sprache. Die Codes sind die Sprach-IDs des Spiels: `en`, `de`, `es`, `fr`, `ja`, `ko`, `nl`, `ru`, `zh`.

```
Global/Modules/Stages/my_stage/
  Script.lua
  lang/
    en/menu.json      { "title": "Settings", "count": "%d items", "tips": ["One", "Two"] }
    ja/menu.json      { "title": "設定", "count": "%d 件" }
```

| Funktion oder Feld | Beschreibung |
| --- | --- |
| `I18N.texts(name, dir?)  -> texts` | Die Texte von `lang/<code>/<name>.json`. `dir` ist der Ordner, der `lang/` enthält: Lassen Sie es in einer Stage oder Activity weg (oder übergeben Sie `""`), um deren eigenen Ordner zu verwenden, und übergeben Sie in einer Lib-Datei den Ordner von `Core.Here`. Die Dateien werden bei der ersten Verwendung gelesen und behalten. |
| `I18N.detect()` | Liest die aktuelle Sprache des Spiels und macht sie zu der, die jede Abfrage verwendet. Rufen Sie es in `activate` auf, das bei jedem Betreten läuft, sodass eine in den Einstellungen geänderte Sprache beim nächsten Besuch gilt. |
| `I18N.load(code)` | Verwendet die Sprache `code` (für ein Testgerüst oder eine Sprachvorschau). |
| `I18N.currentCode()  -> string` | Die aktuelle Sprach-ID des Spiels, `"en"`, wenn die Engine keine liefert. |
| `I18N.code` | Die Sprache, die die Abfragen gerade verwenden. |
| `I18N.localizedPath(path, code?)  -> string` | `<name>_<code>.<ext>`, wenn neben `path` eine Textur mit diesem Namen existiert, sonst `path`: für Grafiken mit eingebranntem Text. |

| Methode | Beschreibung |
| --- | --- |
| `texts:tr(id)  -> string` | Der Text von `id`; die ID selbst, wenn keine Sprache ihn hat, wodurch die Lücke auf dem Bildschirm sichtbar wird. |
| `texts:trf(id, ...)  -> string` | Der Text von `id`, als `string.format`-Muster mit den Argumenten verwendet. |
| `texts:get(...)  -> string` | Der String an einem Schlüsselpfad (`texts:get("menu", "title")` für verschachtelte Objekte), nil, wenn er fehlt oder leer ist. |
| `texts:list(...)  -> table` | Das Array von Strings an einem Schlüsselpfad; jeder Eintrag fällt für sich allein auf Englisch zurück. |
| `texts:loc(...)  -> handle` | Ein Handle, dessen `GetString(fallback)` den Schlüsselpfad zum Zeitpunkt des Aufrufs liest, sodass ein beim Laden zwischengespeicherter Name einem späteren Sprachwechsel folgt. |
| `texts:node(...)  -> value` | Der rohe JSON-Knoten an einem Schlüsselpfad (aktuelle Sprache, sonst Englisch). |

```lua
local I18N = require("Locale.i18n")
local T = I18N.texts("menu")
local title, count

function activate()
    I18N.detect()
    title = T:tr("title")
    count = T:trf("count", #items)
end
```

<div class="callout warn">
Die Abfragen folgen `I18N.code`, das sich nur ändert, wenn `detect` oder `load` läuft. Ein Modul, das auf dem Bildschirm bleibt, während der Spieler die Sprache wechselt, kann die globale Funktion `reloadLanguage(lang)` definieren und dort `detect` aufrufen. Die Textmodule von Pagoda, Vault und Shop prüfen die Sprache bei jeder Abfrage selbst.
</div>

Ältere Module verwenden stattdessen ein Wörterbuch mit exakten englischen Strings: `lang/<code>.lua` gibt eine Tabelle zurück, die jeden englischen String auf seine Übersetzung abbildet, `detect` lädt die für die aktuelle Sprache, und die Funktionen `I18N.tr(s)` und `I18N.trf(fmt, ...)` schlagen Strings darin nach. Diese Form funktioniert weiterhin; neue Module sollten `texts` verwenden.
