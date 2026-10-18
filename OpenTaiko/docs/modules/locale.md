<!-- modules/locale.md -->

# Locale

## Locale.i18n

A module's own translations, kept next to it rather than in the skin's `Locales/`. Texts live in `lang/<code>/<name>.json`, one file per feature, keyed by stable ids; `lang/en/` holds the English and is the fallback for every other language. The codes are the game's language ids: `en`, `de`, `es`, `fr`, `ja`, `ko`, `nl`, `ru`, `zh`.

```
Global/Modules/Stages/my_stage/
  Script.lua
  lang/
    en/menu.json      { "title": "Settings", "count": "%d items", "tips": ["One", "Two"] }
    ja/menu.json      { "title": "設定", "count": "%d 件" }
```

| Function or field | Description |
| --- | --- |
| `I18N.texts(name, dir?)  -> texts` | The texts of `lang/<code>/<name>.json`. `dir` is the folder that holds `lang/`: omit it (or pass `""`) in a stage or activity for its own folder, and pass `Core.Here`'s folder in a Lib file. The files are read on first use and kept. |
| `I18N.detect()` | Reads the game's current language and makes it the one every lookup uses. Call it in `activate`, which runs on every entry, so a language changed in the settings applies on the next visit. |
| `I18N.load(code)` | Uses the language `code` (a harness or a language preview). |
| `I18N.currentCode()  -> string` | The game's current language id, `"en"` when the engine gives none. |
| `I18N.code` | The language the lookups use now. |
| `I18N.localizedPath(path, code?)  -> string` | `<name>_<code>.<ext>` when a texture with that name exists next to `path`, else `path`: art with text baked in. |

| Method | Description |
| --- | --- |
| `texts:tr(id)  -> string` | The text of `id`; the id itself when no language has it, which makes the gap visible on screen. |
| `texts:trf(id, ...)  -> string` | The text of `id` used as a `string.format` pattern with the arguments. |
| `texts:get(...)  -> string` | The string at a key path (`texts:get("menu", "title")` for nested objects), nil when missing or empty. |
| `texts:list(...)  -> table` | The array of strings at a key path; each entry falls back to English on its own. |
| `texts:loc(...)  -> handle` | A handle whose `GetString(fallback)` reads the key path at call time, so a name cached at load follows a later language change. |
| `texts:node(...)  -> value` | The raw JSON node at a key path (current language, else English). |

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
The lookups follow `I18N.code`, which only changes when `detect` or `load` runs. A module that stays on screen while the player changes the language can define the global `reloadLanguage(lang)` and call `detect` there. The Pagoda, Vault and Shop text modules check the language themselves on every lookup.
</div>

Older modules use an exact-English dictionary instead: `lang/<code>.lua` returns a table from each English string to its translation, `detect` loads the one for the current language, and the functions `I18N.tr(s)` and `I18N.trf(fmt, ...)` look strings up in it. That form still works; new modules should use `texts`.
