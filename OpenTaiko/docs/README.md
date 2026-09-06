<!-- docs/README.md -->

# OpenTaiko Documentation

<span class="badge-new">Game version 0.6.1</span>

<div class="callout warn">This documentation is still under review and may change until the release.</div>

OpenTaiko draws almost every screen outside of core gameplay with Lua scripts that ship inside a skin: the menus, the song select, the room and story scenes, the backgrounds, the results screen. This site documents the Lua API those scripts use and walks through the most common things a skinner wants to do.

## Where to start

- If you have not written a module yet, read [How modules work](getting-started.md): the folder layout of a skin, the module types, and the lifecycle callbacks a script receives.
- To look up a function, open the [API reference](api/) and pick a category from the sidebar.
- For a concrete goal, the guides walk through [characters](guides/characters.md), [puchicharas](guides/puchicharas.md), [skins and themes](guides/skins.md) and [chart unlockables](guides/unlockables.md) step by step.

## What you can build

| Area | Where it lives | What it is |
| --- | --- | --- |
| Modules | The skin's `Modules/` folder | Stages (full screens with their own input, drawing and state: a song select, a room, a story scene), activities (sub-screens and overlays such as dialogs and nameplates) and transitions (the fade and loading screens between stages). |
| Backgrounds | The skin's `Graphics/` folder | Scripts that decorate a screen: the startup screen, the room, the gameplay backdrop and mob, the results screen. |
| Skins and themes | `System/`, next to the game | A complete visual package a player installs and switches to, with the theme settings it exposes in the options screen. |
| Characters and puchicharas | `Global/Characters/` and `Global/PuchiChara/`, shared by every skin | The dancers and the small mascots that react during play. |
| Chart unlockables | The song folder, next to the chart | An `Unlock.json` that keeps a song locked until the player earns it. |

## Experimental sections

Some pages carry an <span class="badge-exp">Experimental</span> badge in the sidebar. The features they describe work in the current release, but their API can still change between releases without a deprecation period. A skin that relies on them should state which game version it targets, and you should test it again after each update.

## Languages

The documentation exists in every language OpenTaiko ships with; pick one with the language selector at the top of the sidebar. The site shows a page in English until a translation exists.
