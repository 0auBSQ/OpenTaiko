<!-- modules/custom-stages.md -->

# Custom stages

A custom stage is a stage anyone can make, share and install, and that a skin's Custom Stages menu lists. It declares the namespace `CStage` with [STAGES:SetNamespace](../api/activities.md), and goes back through the shared string `custom_stage_return` to the stage that opened it. `Stages.CustomStages` does both, and lists the custom stages for a menu. `Global/Modules/Stages/custom_stage_tutorial` is one: a guide, inside the game, to making custom stages.

## Declaring it

At the top of Script.lua, outside any function:

```lua
-- Global/Modules/Stages/lantern_walk/Script.lua, at the top
local CS = require("Stages.CustomStages")

CS.declare{
    title       = CS.allLanguages("menu", "title"),       -- lang/<code>/menu.json, every language
    description = { en = "A calm walk down a street of paper lanterns." },
}
```

`CS.declare(info)` is `STAGES:SetNamespace("CStage", info)`. The menus read two fields of info:

| Field | Shows |
| --- | --- |
| `title` | The stage's name in the list: a string, or a table by language code. The folder name when it is missing. |
| `description` | A line or two under the list while the stage is selected; optional. |

A table by language code gives the text in the game's language, else in English, else in any language it has. Other fields are yours: the menus hand info over untouched.

## The way back

The menu that opens a custom stage sets the shared string `custom_stage_return` to the stage to go back to, `"<stage>"` or `"<stage>|<transition>"`, then exits to the custom stage. The custom stage reads and clears the string on entry, and leaves for that stage, or for `_title` when the string is empty:

```lua
local back, backTransition

function activate()
    back, backTransition = CS.takeReturn()   -- "_title" when the string is empty
end

function update(ts)
    if leaving then return Exit("stage", back, backTransition) end
end
```

The string is read on every entry. A custom stage that leaves for another of its stages and comes back sets the string again before it leaves, as My Room does with `my_room_return`.

## Stages.CustomStages

| Function | Description |
| --- | --- |
| `CS.declare(info)  -> boolean` | Declares the calling stage a custom stage: `STAGES:SetNamespace("CStage", info)`. |
| `CS.allLanguages(name, id)  -> table` | The string `id` of the calling stage's `lang/<code>/<name>.json`, from each of the nine languages that has it, as a table by language code. Made for `declare`, which runs before any language is chosen. |
| `CS.localized(value, code?)  -> string or nil` | A declared text in `code` (by default the game's language): a string as it is, a table by language code as described above. |
| `CS.list(code?)  -> table` | The custom stages the calling module reaches, ordered by title: `{ name, isGlobal, title, description, info }`, with `title` and `description` already in the language. |
| `CS.enter(name, back, transition?)` | Sets `custom_stage_return` to `back` and exits to the stage `name`; write `return CS.enter(...)` in a stage's update. |
| `CS.takeReturn()  -> stage, transition` | Reads and clears `custom_stage_return`: the stage (`_title` when the string is empty) and the transition (nil for the default one). |
| `CS.open(o)`, `CS.update(ts)`, `CS.draw()`, `CS.isOpen()`, `CS.close()` | The list as a dialog over the caller's screen, built with the skin's UI library ([UI.Pick](ui.md)). `update` returns nil while it is up, `"cancel"`, or the chosen entry of `CS.list`; the dialog is closed in both cases. `o`: `title`, `ui` (a library table or its require name), `theme`, `sfx`, `navPlayer` (by default 0: the keyboard and the pad, not the drums). With no custom stage installed, it says so above an OK button. |
| `CS.NAMESPACE`, `CS.RETURN_KEY` | `"CStage"` and `"custom_stage_return"`. |

Its strings live in its own `lang/` folder, in the nine languages ([Locale.i18n](locale.md)). Open-World Memories opens the list from the Custom Stages entry of its title screen, above Settings:

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

## Where a custom stage lives

| Folder | Runs under |
| --- | --- |
| `Global/Modules/Stages/<name>/` | Every skin. |
| `System/<skin>/Modules/Stages/<name>/` | That skin only. |

When a name exists in both places, a menu in a skin lists the skin's stage and a global menu the global one, the stage `Exit` would open. The game creates the stages when it starts and when it loads the skin again, so a stage installed while the game runs appears after a restart.

## Sharing one

A custom stage is its folder. Zip the folder itself, so the archive opens on `<name>/Script.lua`, with everything it loads and the credits of its assets. Players extract it into `Global/Modules/Stages/` in their game folder, or into a skin's `Modules/Stages/`, and restart the game. An update adds and replaces files but deletes none, so choose a folder name no shipped stage uses.

## The tutorial

`custom_stage_tutorial` shows, in the game, the four things to know to make a custom stage and share it: where its folder goes (and where not), the `CStage` namespace, keeping everything it uses inside its folder with only the provided modules from outside, and where to read this documentation, the Documentation tab of the OpenTaiko Hub. Its pages are the pictures of its `Textures/` folder and the texts of its `lang/` folder, so they can be replaced one file at a time.
