<!-- docs/api/README.md -->

# API reference

<span class="badge-new">Game version 0.6.1</span>

The reference for every global the OpenTaiko Lua runtime exposes to a skin. Pick a category from the sidebar, or start with [Modules and lifecycle](activities.md) if you have not written a module yet.

## How to read a signature

Each entry shows the function the way you call it from Lua.

- A colon means you call the function on a value, and Lua passes that value as the hidden `self`: you call `tex:Draw(x, y)` on a texture you loaded earlier.
- A dot or a bare name is a plain call, such as `GetSaveFile(0)`.
- The type after the arrow is what the call returns: `TEXTURE:CreateTexture(path) -> texture` returns a handle you keep and draw later.

Global names are upper case (`TEXTURE`, `SOUND`, `INPUT`). The runtime provides them inside every module script; you never create them.

## Categories

| Category | What it covers |
| --- | --- |
| [Modules and lifecycle](activities.md) | The callbacks a module receives, and the helpers for activities, backgrounds, transitions and counters. |
| [Graphics and text](graphics.md) | Textures, canvases, clipping, text rendering, video, colors and gradients. |
| [Audio](audio.md) | Sound loading and playback. |
| [Input](input.md) | Keyboard, pad and pointer input, and on-screen text entry. |
| [Data and persistence](data.md) | Data that survives a restart, JSON and INI loading, shared resources. |
| [Songs and charts](songs.md) | The song list, song nodes and charts, scores, and dan (exam) building. |
| [Players and profiles](players.md) | Save files, nameplates, characters, puchicharas, play state, themes and language. |
| [Math](math.md) | Vectors, matrices and quaternions. |
| [Online networking](networking.md) | The `NET` global for OpenTaiko Online sessions. Currently experimental. |
| [3D engine: rasterizer world](3d.md) | Scenes, objects, models, lights, cameras, sprites, heightmaps and render targets. Currently experimental. |
| [3D engine: raytracer world](3d-raytrace.md) | The path tracer: materials, analytic primitives and the sky gradient. Currently experimental. |
| [3D engine: physics](3d-physics.md) | Physics world, bodies and vehicles, colliders, raycasts and pathfinding. Currently experimental. |

## The Experimental badge

Functions marked <span class="badge-exp">Experimental</span> work today but can change between releases without a deprecation period. The 3D engine and online networking currently carry the badge; the plan is for the 3D engine to leave experimental status with version 1.0. If a skin depends on experimental functions, test it again after each update and state which game version it targets.
