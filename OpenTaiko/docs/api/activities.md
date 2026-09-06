<!-- api/activities.md -->

# Modules and lifecycle

The engine calls a fixed set of callbacks in a module's Script.lua. This page lists them, together with the globals that drive activities, backgrounds and transitions, and the counter, camera and diagnostic helpers every module receives. If you have not written a module before, read [How modules work](../getting-started.md) first.

## Lifecycle callbacks

The engine looks up top-level global functions by name in each module's Script.lua and calls them at fixed points. The engine skips a callback you do not define. The module kind decides which callbacks the engine calls.

<div class="callout warn">
When a skin loads, the engine creates and starts modules in this order: Transitions, Stages, Activities, ROActivities. Within each kind, the engine runs every module's Script.lua first (its top-level code), then calls onStart on each of them. Activities and ROActivities are therefore not loaded yet while a stage's onStart runs, and a lookup there returns nil: look them up in activate. On skin change or exit, onDestroy runs on Stages, then ROActivities and Activities, then Transitions.
</div>

### Stages, Activities and ROActivities

| Method | Description |
| --- | --- |
| `onStart()` | Called once after the engine creates the module, which happens at boot and again whenever the engine loads or reloads the skin. Runs as a coroutine (see LOADING below); load assets here. |
| `activate(...)` | Stage: called each time the engine enters the stage, as a coroutine. Activity/ROActivity: the host calls it through `handle:Activate(...)` with its own arguments; the return values go back to the host. The engine refreshes the CHARACTERLIST and PUCHICHARALIST globals right before it runs. |
| `update(timestamp)` | Called every frame before draw; timestamp is the game clock in milliseconds. A stage stops receiving update once it has called Exit; draw keeps running through the fade-out. Activity/ROActivity: the host calls it through `handle:Update()`. |
| `draw(...)` | Called every frame. Activity/ROActivity: the host calls it through `handle:Draw(...)` with its own arguments; the return values go back to the host. |
| `deactivate(...)` | Stage: called when the engine leaves the stage. Activity/ROActivity: the host calls it through `handle:Deactivate(...)`, or the module calls it on itself through `DEACTIVATE()`; the return values go back to the host. |
| `afterSongEnum()` | Called every time song enumeration finishes, including at boot and after a soft or hard song reload, even when the module is not active. |
| `onDestroy()` | Called before the engine unloads the skin, so the module can free what it holds. |
| `reloadLanguage(lang)` | Called on every loaded module when the game language changes; lang is the new language code. |

### Backgrounds

Each screen hosts its own backgrounds. The Backgrounds section below describes the state argument and the event hooks.

| Method | Description |
| --- | --- |
| `onStart()` | Called once, synchronously, the first time the host activates the background. |
| `activate(state)` | Called each time the host activates the background; a re-activation does not run onStart again. |
| `update(timestamp, state)` | Called every frame (not while gameplay is paused); timestamp is `state.timeStamp` in milliseconds. |
| `draw(state)` | Called every frame. |
| `reloadLanguage(lang)` | Called when the game language changes. |

The engine does not call afterSongEnum or onDestroy on backgrounds; when the host disposes a background, the engine frees the resources it created.

### Transitions

| Method | Description |
| --- | --- |
| `onStart()` | Called once when the skin loads, before the stages and activities, as a coroutine. |
| `fadeOut(t)` | Draws the fade-out over the outgoing stage; t goes from 0 to 1. |
| `loading(progress, elapsed)` | Draws the loading screen; progress is 0 to 1, elapsed is the time in seconds since the load began. |
| `fadeIn(t)` | Draws the fade-in over the new stage; t goes from 0 to 1. |
| `onDestroy()` | Called before the engine unloads the skin, after the stages and activities. |
| `reloadLanguage(lang)` | Called when the game language changes. |

The Transitions section below describes the timing of the phases.

### Characters

A character's Script.lua defines a different set: loadAnimation, disposeAnimation, availableAnimation, setAnimationDuration, resetAnimationCounter, update, draw, getDrawSize, getHeyaRenderOffset, getAIBattlePosition, loadVoice, disposeVoice and playVoice. [Adding characters](../guides/characters.md) covers it.

### Exit

A function the engine registers only in Stage scripts; calling it asks the engine to leave the stage.

<div class="callout warn">
Available in Modules/Stages scripts only; their host drives Activities and ROActivities, which do not get it. Accepts 0 to 3 arguments and tolerates nil in any position. The call itself requests the exit; the bundled stages write `return Exit(...)` inside update so nothing else runs that frame. target is "title", "play", "stage" or "legacy"; nil or any other value means "title". When target is "stage", name is the Modules/Stages module to jump to; when target is "legacy", name is one of "heya", "config", "exit" or "onlinelounge" (any other value goes to the title). transition names a Modules/Transitions module; if you omit it or the engine cannot find it, the engine uses the module named "default", and if the skin has no transitions at all a plain black fade-out plays.
</div>

| Method | Description |
| --- | --- |
| `Exit(target?, name?, transition?)  -> number` | Requests the stage exit towards the given destination, optionally naming a target module and a transition module; returns 0. |

```lua
function update(timestamp)
    if INPUT:KeyboardPressed("S") then
        return Exit("stage", "demo2")          -- jump to Modules/Stages/demo2
    end
    if INPUT:Pressed("Cancel") then
        return Exit("title", nil, "nokon_curtain")   -- back to the title through a named transition
    end
end
```

### LOADING

Loading-bar helper for the callbacks that run as coroutines: onStart of every module kind, and a Stage's activate.

<div class="callout warn">
Defined as the LOADING global in every module. These callbacks run on an engine-owned coroutine that the engine resumes each frame: the engine yields automatically once a resume has used its time budget, and you can also yield yourself with coroutine.yield(progress) or LOADING:Tick(sub). Blocks you register with LOADING:Add run after the callback body returns, in order, and the bar advances after each one; a block's weight is its share of the bar (default 1). LOADING:Tick(sub) yields one frame from inside a block and reports a 0 to 1 fraction within that block. Outside a coroutine callback (an Activity or ROActivity activate, or any update or draw), LOADING:Tick raises a Lua error because there is nothing to yield to, and blocks queued with LOADING:Add never run.
</div>

| Method | Description |
| --- | --- |
| `LOADING:Add(fn)  -> nil` | Registers a load block to run after the callback returns. |
| `LOADING:Add(label, fn)  -> nil` | Registers a labeled load block. |
| `LOADING:Add(label, weight, fn)  -> nil` | Registers a labeled load block with an explicit weight. |
| `LOADING:Tick(sub)  -> nil` | Yields one frame inside a block, reporting a sub-progress fraction from 0 to 1 within the current block. |

```lua
function onStart()
    LOADING:Add("textures", 3, function()
        for i, name in ipairs(names) do
            tx[name] = TEXTURE:CreateTexture(name)
            LOADING:Tick(i / #names)
        end
    end)
    LOADING:Add("sounds", 1, function()
        bgm = SOUND:CreateBGM("Sounds/BGM.ogg")
    end)
end
```

## Activities

### ACTIVITY

Global for looking up a loaded Activity by name.

<div class="callout warn">
Registered as the ACTIVITY global in every module except ROActivities and backgrounds, where it is nil; those modules use ROACTIVITY. The engine loads Activities from Modules/Activities/{name}. ACTIVITY also exposes GetROActivity, which behaves like ROACTIVITY:GetROActivity.
</div>

| Method | Description |
| --- | --- |
| `ACTIVITY:GetActivity(name)  -> activity handle` | Returns the handle of the loaded Activity with the given folder name, or nil if none is loaded. |
| `ACTIVITY:GetROActivity(name)  -> activity handle` | Same as ROACTIVITY:GetROActivity. |

### ROACTIVITY

Global for looking up a loaded read-only Activity (ROActivity) by name.

<div class="callout warn">
Registered as the ROACTIVITY global in every module. The engine loads ROActivities from Modules/ROActivities/{name} and gives them the read-only CONFIG, DATABASE and GetSaveFile globals, so their scripts cannot change game state (see the read-only modules section of How modules work). Activities and ROActivities are name-keyed singletons: one instance per folder, which every host shares.
</div>

| Method | Description |
| --- | --- |
| `ROACTIVITY:GetROActivity(name)  -> activity handle` | Returns the handle of the loaded ROActivity with the given folder name, or nil if none is loaded. |

### Activity handle

The object that ACTIVITY:GetActivity and ROACTIVITY:GetROActivity return. A host uses it to drive the module's callbacks.

<div class="callout warn">
Activate, Deactivate and Draw forward their arguments to the module's activate, deactivate and draw callbacks. Update calls update with the current game time in milliseconds. Each of them returns the values the callback returned as an array indexed from 0, or nil when the callback returned nothing or is undefined; read the first value with `result[0]`. Call invokes any global function that the module's script defines.
</div>

| Method | Description |
| --- | --- |
| `handle.IsActive  -> boolean` | True after Activate ran and until Deactivate (or the module's own DEACTIVATE()) ran. |
| `handle:Activate(...)  -> array` | Calls the module's activate callback with the given arguments. |
| `handle:Deactivate(...)  -> array` | Calls the module's deactivate callback with the given arguments. |
| `handle:Update()  -> array` | Calls the module's update callback with the current game time in milliseconds. |
| `handle:Draw(...)  -> array` | Calls the module's draw callback with the given arguments. |
| `handle:Call(functionName, ...)  -> array` | Calls the named global function of the module's script with the given arguments. |

```lua
local act = nil

function activate()
    if act == nil then act = ACTIVITY:GetActivity("song_select_core") end
    act:Activate()
end

function update(timestamp)
    local result = act:Update()
    local signal = result ~= nil and result[0] or nil
    if signal == "play" then return Exit("play", nil) end
    if signal == "cancel" then return Exit("title", nil) end
end

function draw()
    act:Draw()
end

function deactivate()
    act:Deactivate()
end
```

### DEACTIVATE

Function the engine registers inside Activity and ROActivity scripts; it lets the module deactivate itself.

<div class="callout warn">
Calling it marks the module inactive (handle.IsActive becomes false) and runs the module's own deactivate callback. The bundled dialogs call it when the player confirms or cancels, and the host watches IsActive to know the dialog closed.
</div>

| Method | Description |
| --- | --- |
| `DEACTIVATE(...)` | Deactivates the current module and calls its deactivate callback with the given arguments. |

### ROActivities hosted by the engine

The engine looks up some ROActivities by fixed folder name and drives them itself. A skin replaces one by shipping a Modules/ROActivities folder with that name; the engine call sites listed below fix the callbacks it must define. When one is missing, the engine does not draw the corresponding feature.

| Name | What the engine calls |
| --- | --- |
| `nameplate` | `activate(player, name, title, dan, data)` when a player's plate changes; `update()` once per frame; `draw(mode, ...)` with mode 0 = full plate `(x, y, opacity, player, side)`, 1 = dan plate `(x, y, opacity, danGrade, textTexture)`, 2 = title plate `(x, y, opacity, type, textTexture, rarity, nameplateId)`. |
| `modal` | `activate(player, rarity, modalType, ...)` for each queued unlock modal, then `update()` and `draw()` every frame. The script calls DEACTIVATE() to dismiss the modal; the engine then activates the next one. |
| `modicons` | `activate()` once, then `draw(x, y, player, layout, alpha)` with layout "menu" or "game". The MODICONS global wraps this. |
| `danplate` | `draw(x, y, opacity, danTick, r, g, b, titleText)` on the result screen and in dan courses. |
| `popup_menu` | `activate(title, items, fontSize, ...)` where items are the labels joined by newlines, followed by the PopupMenu skin positions; `draw(selected)` every frame; `deactivate()` on close. |
| `config_ui` | `activate(model)` with the settings model; `update()` every frame, returning "exit" to leave the settings screen; `draw()`; `reload(model)` through Call when the engine rebuilds the model; `deactivate()`. |
| `song_enum` | `activate()`, then `draw(isCommandSongDataGet, done, total)` every frame while the song scan runs; `deactivate()`. |

## Backgrounds

### Background module

A Script.lua that draws one screen background, gameplay layer, mob, clear animation or kusudama effect, hosted by the engine's own screens.

<div class="callout warn">
Backgrounds live outside the Modules folder, under the skin's Graphics folder in the directory of the screen they decorate, for example Graphics/0_Startup/Script.lua, Graphics/10_Heya/Script.lua, Graphics/6_Result/Script.lua, Graphics/5_Game/5_Background/Normal/Up/{variant}/Script.lua, Graphics/5_Game/5_Background/Normal/Down/{variant}/Script.lua, Graphics/5_Game/3_Mob/{variant}/Script.lua, Graphics/5_Game/9_End/{result}/Script.lua and Graphics/5_Game/11_Balloon/Kusudama/Script.lua. Where a folder holds several variants, the engine picks one at random (or from the chart's scene preset) each play. The host screen creates a background instance (the gameplay backgrounds each time the engine enters the game screen) and disposes it with the screen, so several are live at once during gameplay. A background script receives the same globals as an ROActivity (read-only CONFIG, DATABASE and GetSaveFile; no ACTIVITY). The event hooks below are optional, and the engine calls each one once when its event happens.
</div>

| Method | Description |
| --- | --- |
| `clearIn(player)` | Gameplay Up and Down backgrounds: the player's gauge reached the clear zone. |
| `clearOut(player)` | Gameplay Up and Down backgrounds: the player's gauge dropped out of the clear zone. |
| `playEndAnime(player)` | Clear animations (Graphics/5_Game/9_End): the end animation starts for the player. |
| `kusuIn()` / `kusuBroke()` / `kusuMiss()` | Kusudama: the balloon appears, the player breaks it, or the player misses it. |
| `skipAnime()` | Result background: the player skipped the result animation. |

### Background state

The object the host passes to a background's activate, update and draw.

<div class="callout warn">
One instance per host, which the host updates in place each frame. The array fields share the engine's per-player arrays and are indexed from 0 (`state.gauge[0]` is player 1). Only gameplay hosts refresh the gameplay fields; other hosts leave them at their defaults, and timeStamp stays at -1 outside gameplay. The state carries no frame timing: read the fps global.
</div>

| Method | Description |
| --- | --- |
| `state.playerCount  -> number` | Number of players. |
| `state.p1IsBlue  -> boolean` | True when player 1 uses the blue side. |
| `state.lang  -> string` | Current language code. |
| `state.simplemode  -> boolean` | True when Simple Mode is on. |
| `state.puchicharaRarities  -> string[]` | Rarity of each player's puchichara. |
| `state.characterRarities  -> string[]` | Rarity of each player's character. |
| `state.isClear  -> boolean[]` | Whether each player is currently in the clear zone. |
| `state.gauge  -> number[]` | Each player's gauge value. |
| `state.bpm  -> number[]` | Each player's current BPM. |
| `state.gogo  -> boolean[]` | Whether each player is in go-go time. |
| `state.towerNightNum  -> number` | Tower day-to-night factor from 0 to 1. |
| `state.battleState  -> number` | AI battle state code. |
| `state.battleWin  -> boolean` | True when the player is winning the AI battle. |
| `state.timeStamp  -> number` | Chart-synced time in seconds; -1 outside gameplay. |
| `state.paused  -> boolean` | True while gameplay is paused. |
| `state.player  -> number` | The player a per-player host (clear animations) is drawing for. |

## Transitions

### Transition module

A Modules/Transitions/{name}/Script.lua that draws the fade-out, loading and fade-in phases between two stages.

<div class="callout warn">
The third argument of Exit selects the transition; the engine uses "default" when the call names none or the engine cannot find the name. The load into gameplay after Exit("play") always uses the transition named "song_loading", or "default" when the skin has none. The engine drives the phases in order: it calls fadeOut(t) every frame over the outgoing stage until t reaches 1, then unmounts the outgoing stage and loads the new one while calling loading(progress, elapsed) every frame, then calls fadeIn(t) over the new stage until t reaches 1. Each fade lasts 0.5 seconds unless the script sets FADE_OUT_SECONDS or FADE_IN_SECONDS; the engine ignores a value that is not a positive number. On a stage switch, the engine only calls loading once the load has taken longer than 0.5 seconds; before that it calls fadeOut(1) so short loads do not flash a loading screen. The song-loading path shows the loading phase immediately.
</div>

| Method | Description |
| --- | --- |
| `FADE_OUT_SECONDS  -> number` | Optional top-level global: length of the fade-out in seconds. |
| `FADE_IN_SECONDS  -> number` | Optional top-level global: length of the fade-in in seconds. |

```lua
FADE_OUT_SECONDS = 0.3
FADE_IN_SECONDS = 0.3

local pixel = nil

local function cover(alpha)
    pixel:SetColor(0, 0, 0)
    pixel:SetOpacity(alpha)
    pixel:SetScale(8000, 8000)
    pixel:Draw(0, 0)
end

function onStart()
    pixel = TEXTURE:CreateTexture("pixel.png")
end

function fadeOut(t) cover(t) end
function fadeIn(t) cover(1.0 - t) end
function loading(progress, elapsed) cover(1.0) end

function onDestroy()
    if pixel ~= nil then pixel:Dispose() end
end
```

## Timing and camera

### COUNTER

Factory for animation counters that move a value from a begin to an end value over time.

<div class="callout warn">
Registered as the COUNTER global. A counter only advances when you call Tick, by the frame delta divided by interval, where interval is the number of seconds per unit of value. The sign of interval must match the direction: positive when end is greater than begin, negative when it is smaller. If the signs do not match, the counter swaps the ends and finishes on its first tick. CreateCounterDuration takes the total duration and picks the sign itself. A zero interval or equal begin and end values ends the counter on its first tick. The counter calls the optional ended function when the value reaches the end, and once per completed cycle when looping or bouncing.
</div>

| Method | Description |
| --- | --- |
| `COUNTER:CreateCounter(begin, end, interval, ended?)  -> counter` | Creates a counter that moves from begin to end at interval seconds per unit, calling ended on completion. |
| `COUNTER:CreateCounterDuration(begin, end, seconds, ended?)  -> counter` | Creates a counter that moves from begin to end over the given number of seconds; returns an empty counter if seconds is not positive or begin equals end. |
| `COUNTER:EmptyCounter()  -> counter` | Creates an inert counter whose value stays at 0, usable as a placeholder. |

### Counter handle

A counter created by COUNTER.

<div class="callout warn">
Read Value each frame and call Tick each frame to advance it; a counter you have not started, or that has stopped, ignores Tick. Begin, End and Interval are readable and writable fields. SetLoop and SetBounce exclude each other. SetEasing only shapes the reported Value; the counter still advances linearly underneath. Listeners receive the current value on every tick, including the final one.
</div>

| Method | Description |
| --- | --- |
| `counter.Value  -> number` | The current value, with easing applied if set; assigning it jumps the counter. |
| `counter.Begin  -> number` | The start value (readable and writable). |
| `counter.End  -> number` | The end value (readable and writable). |
| `counter.Interval  -> number` | Seconds per unit of value (readable and writable). |
| `counter:Start()  -> nil` | Resets the value to Begin and starts ticking. |
| `counter:Resume()  -> nil` | Starts ticking without resetting the value. |
| `counter:Stop()  -> nil` | Stops ticking. |
| `counter:Pause()  -> nil` | Same as Stop. |
| `counter:Reset()  -> nil` | Sets the value back to Begin without changing whether it ticks. |
| `counter:Tick()  -> nil` | Advances the value by one frame, calling listeners and the ended function as appropriate. |
| `counter:SetLoop(loop)  -> nil` | Wraps back to Begin when the value reaches the end; turns bouncing off. |
| `counter:SetBounce(bounce)  -> nil` | Reverses direction when the value reaches either end; turns looping off. |
| `counter:GetLoop()  -> boolean` | Whether looping is on. |
| `counter:GetBounce()  -> boolean` | Whether bouncing is on. |
| `counter:SetEasing(type, function)  -> nil` | Applies an easing curve to the reported value; type is IN, OUT, INOUT or OUTIN and function is LINEAR, SINE, QUAD, CUBIC, QUART, QUINT, EXPO, CIRC, ELASTIC, BACK or BOUNCE (case-insensitive; the counter ignores an unknown name). |
| `counter:ClearEasing()  -> nil` | Removes the easing. |
| `counter:Listen(listener)  -> nil` | Registers a function called with the current value on each tick. |
| `counter:ClearListeners()  -> nil` | Removes all listeners. |

```lua
local fade = nil

function activate()
    fade = COUNTER:CreateCounterDuration(0, 1, 0.5, function() debugLog("fade done") end)
    fade:SetEasing("OUT", "QUAD")
    fade:Start()
end

function update(timestamp)
    fade:Tick()
end

function draw()
    background:SetOpacity(fade.Value)
    background:Draw(0, 0)
end
```

### GLOBALCAMERA

The whole-screen 2D camera: it pans, zooms and rotates the entire rendered frame and adds a decaying screen shake.

<div class="callout warn">
Registered as the GLOBALCAMERA global. It drives the same screen transform as the TJA #CAMERA commands and affects everything drawn, including blitted 3D scenes. Offsets are in 1280x720 reference pixels, rotation is in degrees and a zoom of 1 means no scaling. The base transform persists until you change it: call Update(dt) every frame to apply it and advance the shake, and Reset() when you leave the stage so it does not carry over into the next one. You set a 3D scene's own camera on the scene object.
</div>

| Method | Description |
| --- | --- |
| `GLOBALCAMERA:SetOffset(x, y)  -> nil` | Pans the screen by (x, y) pixels. |
| `GLOBALCAMERA:SetZoom(sx, sy)  -> nil` | Zooms the screen with separate X and Y factors. |
| `GLOBALCAMERA:SetUniformZoom(s)  -> nil` | Zooms the screen uniformly. |
| `GLOBALCAMERA:SetRotation(deg)  -> nil` | Rotates the screen about its centre. |
| `GLOBALCAMERA:GetOffsetX()  -> number` | The base X offset. |
| `GLOBALCAMERA:GetOffsetY()  -> number` | The base Y offset. |
| `GLOBALCAMERA:GetZoomX()  -> number` | The X zoom factor. |
| `GLOBALCAMERA:GetZoomY()  -> number` | The Y zoom factor. |
| `GLOBALCAMERA:GetRotation()  -> number` | The base rotation in degrees. |
| `GLOBALCAMERA:Shake(amplitudePx, seconds, rotAmpDeg?)  -> nil` | Starts a shake that decays linearly from the given pixel amplitude over the given seconds, with an optional rotational wobble in degrees. The camera ignores a call with seconds of 0 or less; a new shake replaces the current one only when its amplitude is equal or larger. |
| `GLOBALCAMERA.IsShaking  -> boolean` | True while a shake is still decaying. |
| `GLOBALCAMERA:Update(dt)  -> nil` | Advances the shake by dt seconds (clamped to 0.25) and applies the base transform plus shake to the screen. |
| `GLOBALCAMERA:Reset()  -> nil` | Recentres the camera, resets zoom and rotation and stops any shake. |

```lua
function update(timestamp)
    if INPUT:Pressed("LRed") or INPUT:Pressed("RRed") then GLOBALCAMERA:Shake(18, 0.35) end
    GLOBALCAMERA:Update(fps.deltaTime)
end

function deactivate()
    GLOBALCAMERA:Reset()
end
```

## Song enumeration

Two global functions report the state of the song scan. Use them together with the afterSongEnum callback.

| Method | Description |
| --- | --- |
| `IsSongsEnumerating()  -> boolean` | True while a song enumeration pass is running. |
| `IsSongsEnumDone()  -> boolean` | True once the song scan has finished. It is false before the scan starts and while it runs; IsSongsEnumerating is false in both the not-started and the finished state, so check this function to know the list is ready. |

## Diagnostics

### info

Read-only object with basic game state and the module's own directory.

<div class="callout warn">
Registered as the info global and created per module. Each field computes its value on access; online queries the operating system each time you read it.
</div>

| Method | Description |
| --- | --- |
| `info.playerCount  -> number` | The configured number of players. |
| `info.lang  -> string` | The current language code. |
| `info.simplemode  -> boolean` | True when Simple Mode is on. |
| `info.p1IsBlue  -> boolean` | True when player 1 uses the blue side. |
| `info.online  -> boolean` | True when a usable network interface is available. |
| `info.dir  -> string` | The directory of this module. |

### fps

Read-only object with the frame timing and a high-resolution clock.

| Method | Description |
| --- | --- |
| `fps.deltaTime  -> number` | Seconds elapsed since the previous frame. |
| `fps.fps  -> number` | The current measured frames per second. |
| `fps.ms  -> number` | A monotonic clock in milliseconds, for timing sections of Lua by taking differences. |

### debugLog

| Method | Description |
| --- | --- |
| `debugLog(message)  -> nil` | Writes the string to the engine trace log with a prefix that marks it as a Lua log. |

## Other globals

The engine registers these globals in every module; their own pages document them.

| Global | Page |
| --- | --- |
| `GetSaveFile(player)` | [Players and profiles](players.md). Returns a read-only handle inside ROActivities and backgrounds. |
| `RequestSongList(settings)`, `GenerateSongListSettings()` | [Songs and charts](songs.md). |
| `MODICONS` | [Songs and charts](songs.md). |
| `CONFIG`, `DATABASE`, `SHARED`, `STORAGE`, `JSONLOADER`, `INILOADER`, `SQL` | [Data and persistence](data.md). |
| `TEXTURE`, `CANVAS`, `GRAPHICS`, `TEXT`, `VIDEO`, `COLOR`, `GRADIENT`, `SIZE` | [Graphics and text](graphics.md). |
| `SOUND`, `HITSOUNDSLIST` | [Audio](audio.md). |
| `INPUT` | [Input](input.md). |
| `NAMEPLATE`, `NAMEPLATESLIST`, `CHARACTER`, `CHARACTERLIST`, `PUCHICHARALIST`, `PLAYSTATE`, `THEME`, `LANG` | [Players and profiles](players.md). |
| `VECTOR`, `VECTOR2`, `VECTOR3`, `VECTOR4`, `MATRIX`, `MATRIX2`, `MATRIX3`, `MATRIX4`, `QUATERNION` | [Math](math.md). |
| `SONGMOUNT`, `REPLAY`, `DANBUILDER`, `VIRTUALSLOTS` | [Songs and charts](songs.md). |
| `NET` | [Online networking](networking.md). |
| `SCENE3D`, `MODEL`, `PHYSICS`, `COLLIDERS`, `PATHFIND`, `HEIGHTMAP` | [3D engine: rasterizer world](3d.md), [3D engine: raytracer world](3d-raytrace.md), [3D engine: physics](3d-physics.md) <span class="badge-exp">Experimental</span>. |
