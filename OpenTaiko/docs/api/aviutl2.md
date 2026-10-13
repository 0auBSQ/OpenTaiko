<!-- api/aviutl2.md -->

# AviUtl2 projects

The `AVIUTL2` global plays project files saved by the AviUtl2 video editor (`.aup2`). The game reads the project and draws its objects, movements and effects live, so a skin can ship an animation as a project and its pictures in place of a rendered video.

The script that loads a project owns its handle, and the game disposes the handle when it unloads that script. Call `Dispose()` yourself to free it earlier.

<div class="callout warn">
The game draws the project with its own renderer. Positions, sizes, rotations, movement curves, camera control and the common effects follow the editor. Random movements and shakes use different random values, and some effects are approximations. After loading a project, read GetUnsupported and GetApproximated to see what the game left out or simplified.
</div>

## Loading

### AVIUTL2

| Method | Description |
| --- | --- |
| `AVIUTL2:LoadProject(path)  -> project` | Opens a project file and returns its handle. The path is relative to the script directory. |
| `AVIUTL2:LoadProjectFromAbsolutePath(path)  -> project` | Opens a project file from a full path. |

<div class="callout warn">
A project stores the full paths of the machine that saved it. The game looks for each picture, video, sound and model next to the project file, first by the folders the stored path shares with the project's own, then by file name. Keep the project's files in its folder or in subfolders of it. A missing or unreadable project returns a handle whose Loaded is false.
</div>

## Project handle

A project returned by `AVIUTL2:LoadProject`. Its playback methods carry the same names as a video handle's.

### Playback

| Method | Description |
| --- | --- |
| `project:Start()  -> nil` | Starts playback from the beginning. |
| `project:Resume()  -> nil` | Resumes playback after Pause. |
| `project:Pause()  -> nil` | Pauses playback, sounds included. |
| `project:Stop()  -> nil` | Stops playback. |
| `project:Reset()  -> nil` | Seeks back to the start. |
| `project.IsPlaying  -> bool` | True while the project is playing and has not reached its end. |
| `project:IsFinished()  -> bool` | True once a project that does not loop has reached its end. |
| `project:GetTimestampMs()  -> number` | Current position in milliseconds. |
| `project:SetTimestampMs(ms)  -> nil` | Seeks to a position in milliseconds. |
| `project:GetFrame()  -> number` | Current position in frames of the project. |
| `project:SetFrame(frame)  -> nil` | Seeks to a frame. |
| `project:GetSpeed()  -> number` | Current playback speed multiplier (1 = normal). |
| `project:SetSpeed(speed)  -> nil` | Sets the playback speed multiplier. |
| `project:GetLoop()  -> bool` | True when the project starts again at its end. |
| `project:SetLoop(loop)  -> nil` | Makes the project start again at its end. |
| `project:GetVolumePercent()  -> number` | Volume of the project's sounds in percent. |
| `project:SetVolumePercent(vol)  -> nil` | Sets the volume of the project's sounds in percent. |
| `project:Dispose()  -> nil` | Frees the project, its pictures, videos and sounds. |

### Drawing

| Method | Description |
| --- | --- |
| `project:Draw(x, y)  -> nil` | Draws the current frame with its top left corner at (x, y), at the scale set by SetScale. |
| `project:DrawFit(x, y, w, h)  -> nil` | Draws the current frame as large as the box allows, keeping its shape, centred in the box. |
| `project:SetScale(scaleX, scaleY)  -> nil` | Sets the scale Draw uses (1 = the project's own size). |
| `project:SetOpacity(opacity)  -> nil` | Sets the opacity of the whole frame, from 0 to 1. |
| `project:SetBackground(r, g, b, a)  -> nil` | Fills the frame behind the objects with a colour, each value from 0 to 255. An alpha of 0 leaves the frame see-through. |
| `project:ClearBackground()  -> nil` | Goes back to the background the project file asks for. |
| `project.HasProjectBackground  -> bool` | True when the scene has a background colour of its own. |
| `project.ProjectBackground  -> int` | The scene's background colour as 0xRRGGBB, or -1 when it has none. |

<div class="callout warn">
A scene with no background colour is see-through where no object is drawn, so the project draws over whatever the script drew before it. The editor shows such a scene on black. Draw a black box first, or call SetBackground(0, 0, 0, 255), to get the same picture.
</div>

### Pictures drawn by the script

A script can replace a picture of the project with its own drawing, for example to show a player's nameplate inside an animated frame.

| Method | Description |
| --- | --- |
| `project:SetImageDrawer(fileName, width, height, draw)  -> nil` | Every object that shows the image file `fileName` shows what the function `draw` draws. The game calls `draw` each frame with a drawing area `width` by `height` whose top left corner is (0, 0). |
| `project:ClearImageDrawer(fileName)  -> nil` | The objects show the image file again. |

The game calls `draw(rank, layer)` once for each layer that holds such an object. `rank` counts those layers from 0, starting at the topmost, and `layer` is the layer's number in the project. Inside `draw`, use the usual drawing calls: the game redirects them into the picture, which then moves, scales and fades with the object.

<div class="callout warn">
The function must belong to the script that calls SetImageDrawer. Soft edges drawn inside `draw` come out slightly thinner than on screen.
</div>

### Reading the project

| Method | Description |
| --- | --- |
| `project.Loaded  -> bool` | True when the file was read. |
| `project.Width  -> int` | Width of the scene in pixels, or -1 when nothing is loaded. |
| `project.Height  -> int` | Height of the scene in pixels, or -1 when nothing is loaded. |
| `project.FrameRate  -> number` | Frames per second of the scene. |
| `project.FrameCount  -> int` | Length of the scene in frames. |
| `project.DurationMs  -> number` | Length of the scene in milliseconds. |
| `project.SceneCount  -> int` | Number of scenes in the project. |
| `project:GetScene()  -> int` | Index of the scene being played. |
| `project:SetScene(index)  -> nil` | Plays another scene of the project, from its start. |
| `project:GetSceneName(index)  -> string` | Name of a scene. |
| `project.ObjectCount  -> int` | Number of objects in the scene. |
| `project:GetProjectValue(key)  -> string` | A setting of the project, as written in the file. |
| `project:GetSceneValue(key)  -> string` | A setting of the scene being played. |
| `project:GetValue(section, key)  -> string` | A setting of any section of the file, by the section's name (`"12"` for an object, `"12.1"` for its second effect). |
| `project:GetValueAtFrame(objectId, effect, key, frame)  -> number` | A numeric setting of an object's effect at a frame, with its movement applied. |

### Checking what is drawn

| Method | Description |
| --- | --- |
| `project.UnsupportedCount  -> int` | Number of things the project uses that the game does not draw, missing files included. |
| `project:GetUnsupported()  -> string` | Those things, one per line. |
| `project.ApproximatedCount  -> int` | Number of things the game draws in a simplified way. |
| `project:GetApproximated()  -> string` | Those things, one per line. |
| `project:AddScriptFolder(path)  -> nil` | Adds a folder of AviUtl script files the project's effects can name. The game reads a `Script` folder next to the project file by itself. |
| `project.ScriptErrorCount  -> int` | Number of script files that failed. |
| `project:GetScriptErrors()  -> string` | The first error of each failed script, one per line. |
| `project.GraphicsMemoryMB  -> number` | Graphics memory the project currently holds for its own drawing, in megabytes. |
| `project:SetSurfaceBudgetMB(mb)  -> nil` | Sets how much graphics memory the project keeps for reuse between frames. |

<div class="callout warn">
Script files shipped with a project run in a closed environment: they cannot read or write files, start programs or load native modules.
</div>

```lua
local intro
local box

function onStart()
    intro = AVIUTL2:LoadProject("Intro/intro.aup2")
    box = TEXTURE:CreateTexture("Intro/player_box.png")
    -- the nameplate of each player, drawn into the project's player boxes
    intro:SetImageDrawer("player_box.png", 520, 216, function(rank)
        box:Draw(0, 0)
        NAMEPLATE:DrawPlayerNameplate(170, 88, 255, rank)
    end)
end

function activate()
    intro:SetBackground(0, 0, 0, 255)
    intro:Start()
end

function draw()
    intro:DrawFit(0, 0, 1920, 1080)
    if intro:IsFinished() then Exit("title", nil) end
end

function deactivate()
    intro:Stop()
end

function onDestroy()
    intro:Dispose()
    box:Dispose()
end
```
