<!-- api/audio.md -->

# Audio

Loading and playing sounds, and reading the installed hitsound sets.

## SOUND

Creates sound handles from audio files. Each factory method assigns the sound to a volume group (sound effect, voice, song playback, or song preview), and the group decides which user volume setting applies.

<div class="callout warn">
Relative paths resolve against the script's directory; the FromAbsolutePath variants take a full path. The returned handle starts empty while the render thread builds the stream over the following frames; the handle queues Play and Set* calls made before then and applies them once the stream is ready. The game disposes handles when it unloads the script; call Dispose to free one earlier.
</div>

| Method | Description |
| --- | --- |
| `SOUND:CreateSFX(path)  -> sound` | Loads a sound effect. |
| `SOUND:CreateVoice(path)  -> sound` | Loads a voice clip. |
| `SOUND:CreateBGM(path)  -> sound` | Loads background music (song playback group). |
| `SOUND:CreatePreview(path)  -> sound` | Loads a song preview clip (song preview group). |
| `SOUND:CreateSFXFromAbsolutePath(path)  -> sound` | Loads a sound effect from a full path. |
| `SOUND:CreateVoiceFromAbsolutePath(path)  -> sound` | Loads a voice clip from a full path. |
| `SOUND:CreateBGMFromAbsolutePath(path)  -> sound` | Loads background music from a full path. |
| `SOUND:CreatePreviewFromAbsolutePath(path)  -> sound` | Loads a song preview clip from a full path. |

```lua
local bgm, decide

function onStart()
    bgm = SOUND:CreateBGM("Sounds/BGM.ogg")
    bgm:SetLoop(true)
    decide = SOUND:CreateSFX("Sounds/Decide.ogg")
end

function activate()
    bgm:Play()
end

function update()
    if INPUT:Pressed("Decide") then decide:Play() end
end

function deactivate()
    bgm:Stop()
end

function onDestroy()
    bgm:Dispose()
    decide:Dispose()
end
```

## Sound handle

A playable sound returned by the `SOUND` factory methods.

<div class="callout warn">
Until the stream is ready, the handle buffers Play and every Set* call, and the getters return the defaults listed below. Volume is a percentage (100 = the file's own level). Pan runs from -100 (left) through 0 (centre) to 100 (right). Positions and durations are in milliseconds. Play restarts the stream from the beginning, so seek after calling Play.
</div>

| Method | Description |
| --- | --- |
| `sound:Play()  -> nil` | Starts (or restarts) playback from the beginning. |
| `sound:Start()  -> nil` | Same as Play. |
| `sound:Pause()  -> nil` | Pauses playback. |
| `sound:Resume()  -> nil` | Resumes playback after Pause. |
| `sound:Stop()  -> nil` | Stops playback. |
| `sound:Reset()  -> nil` | Seeks back to the start. |
| `sound:SetLoop(loop)  -> nil` | Enables or disables looping. |
| `sound:GetLoop()  -> bool` | Returns whether looping is enabled. |
| `sound:SetVolumePercent(vol)  -> nil` | Sets the volume in percent. |
| `sound:GetVolumePercent()  -> number` | Returns the volume in percent (100 when not loaded). |
| `sound:SetVolume(vol)  -> nil` | Older name for SetVolumePercent (integer). |
| `sound:SetPan(pan)  -> nil` | Sets the stereo pan, -100..100. |
| `sound:GetPan()  -> int` | Returns the current pan (0 when not loaded). |
| `sound:SetSpeed(speed)  -> nil` | Sets the playback speed multiplier (1 = normal). |
| `sound:GetSpeed()  -> number` | Returns the playback speed multiplier (1 when not loaded). |
| `sound:SetTimestampMs(ms)  -> nil` | Seeks to a position in milliseconds. |
| `sound:SetTimestamp(ms)  -> nil` | Older name for SetTimestampMs. |
| `sound:GetTimestampMs()  -> number` | Returns the current position in milliseconds (0 when not loaded). |
| `sound.DurationMs  -> number` | Total duration in milliseconds (0 when not loaded). |
| `sound:GetDurationMs()  -> number` | Older name for DurationMs. |
| `sound.Loaded  -> bool` | True once the render thread has built the stream. |
| `sound.IsPlaying  -> bool` | True while the sound is playing. |
| `sound:IsFinished()  -> bool` | True once playback has reached the end (false while paused or not loaded). |
| `sound.Path  -> string` | The full path of the loaded file (empty until loaded). |
| `sound:Dispose()  -> nil` | Frees the sound. |

## HITSOUNDSLIST

Read-only list of the hitsound sets installed in the current skin (the game reads them from `Global/HitSounds/`).

<div class="callout warn">
Available in every script. Indices are 0-based. GetByName matches the set's folder name case-insensitively. Both lookups return nil when nothing matches.
</div>

| Method | Description |
| --- | --- |
| `HITSOUNDSLIST.Count  -> int` | Number of hitsound sets. |
| `HITSOUNDSLIST:GetByIndex(index)  -> hitsoundEntry` | Returns the entry at the given 0-based index, or nil. |
| `HITSOUNDSLIST:GetByName(name)  -> hitsoundEntry` | Returns the entry whose folder name matches, or nil. |

```lua
local don

function onStart()
    for i = 0, HITSOUNDSLIST.Count - 1 do
        local hs = HITSOUNDSLIST:GetByIndex(i)
        debugLog(hs.FolderName .. " -> " .. hs.DisplayName)
    end
    local taiko = HITSOUNDSLIST:GetByName("Taiko")
    if taiko then
        don = SOUND:CreateSFXFromAbsolutePath(taiko.DonPath)
    end
end

function onDestroy()
    if don then don:Dispose() end
end
```

## Hitsound entry

One hitsound set returned by `HITSOUNDSLIST:GetByIndex` or `GetByName`. All members are read-only.

| Method | Description |
| --- | --- |
| `hitsoundEntry.FolderName  -> string` | Folder name of the set (for example `"Taiko"`). |
| `hitsoundEntry.DisplayName  -> string` | Localised display name; falls back to the folder name. |
| `hitsoundEntry.DonPath  -> string` | Full path of the set's Don sound file. |
| `hitsoundEntry.KaPath  -> string` | Full path of the set's Ka sound file. |
