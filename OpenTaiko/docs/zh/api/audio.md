<!-- api/audio.md -->

# 音频

加载和播放声音，以及读取已安装的打击音效组。

## SOUND

从音频文件创建声音句柄。每个工厂方法都把声音分配到一个音量组（音效、语音、歌曲播放或歌曲试听），由该组决定适用哪一项用户音量设置。

<div class="callout warn">
相对路径相对于脚本目录解析；FromAbsolutePath 变体接受完整路径。返回的句柄初始为空，渲染线程会在随后几帧内构建音频流；句柄会把在此之前进行的 Play 和 Set* 调用排队，并在音频流就绪后应用它们。游戏在卸载脚本时释放句柄；要提前释放请调用 Dispose。
</div>

| 方法 | 说明 |
| --- | --- |
| `SOUND:CreateSFX(path)  -> sound` | 加载一个音效。 |
| `SOUND:CreateVoice(path)  -> sound` | 加载一段语音。 |
| `SOUND:CreateBGM(path)  -> sound` | 加载背景音乐（歌曲播放组）。 |
| `SOUND:CreatePreview(path)  -> sound` | 加载一段歌曲试听片段（歌曲试听组）。 |
| `SOUND:CreateSFXFromAbsolutePath(path)  -> sound` | 从完整路径加载一个音效。 |
| `SOUND:CreateVoiceFromAbsolutePath(path)  -> sound` | 从完整路径加载一段语音。 |
| `SOUND:CreateBGMFromAbsolutePath(path)  -> sound` | 从完整路径加载背景音乐。 |
| `SOUND:CreatePreviewFromAbsolutePath(path)  -> sound` | 从完整路径加载一段歌曲试听片段。 |

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

## 声音句柄

由 `SOUND` 工厂方法返回的可播放声音。

<div class="callout warn">
在音频流就绪之前，句柄会缓冲 Play 和所有 Set* 调用，getter 返回下面列出的默认值。音量是百分比（100 = 文件自身的电平）。声像从 -100（左）经 0（中）到 100（右）。位置和时长以毫秒计。Play 会从头重新开始播放流，因此请在调用 Play 之后再跳转。
</div>

| 方法 | 说明 |
| --- | --- |
| `sound:Play()  -> nil` | 从头开始（或重新开始）播放。 |
| `sound:Start()  -> nil` | 与 Play 相同。 |
| `sound:Pause()  -> nil` | 暂停播放。 |
| `sound:Resume()  -> nil` | 在 Pause 之后恢复播放。 |
| `sound:Stop()  -> nil` | 停止播放。 |
| `sound:Reset()  -> nil` | 跳回开头。 |
| `sound:SetLoop(loop)  -> nil` | 启用或禁用循环。 |
| `sound:GetLoop()  -> bool` | 返回是否启用了循环。 |
| `sound:SetVolumePercent(vol)  -> nil` | 以百分比设置音量。 |
| `sound:GetVolumePercent()  -> number` | 返回百分比音量（未加载时为 100）。 |
| `sound:SetVolume(vol)  -> nil` | SetVolumePercent 的旧名称（整数）。 |
| `sound:SetPan(pan)  -> nil` | 设置立体声声像，-100..100。 |
| `sound:GetPan()  -> int` | 返回当前声像（未加载时为 0）。 |
| `sound:SetSpeed(speed)  -> nil` | 设置播放速度倍率（1 = 正常）。 |
| `sound:GetSpeed()  -> number` | 返回播放速度倍率（未加载时为 1）。 |
| `sound:SetTimestampMs(ms)  -> nil` | 跳转到以毫秒计的位置。 |
| `sound:SetTimestamp(ms)  -> nil` | SetTimestampMs 的旧名称。 |
| `sound:GetTimestampMs()  -> number` | 返回当前位置（毫秒）（未加载时为 0）。 |
| `sound.DurationMs  -> number` | 总时长（毫秒）（未加载时为 0）。 |
| `sound:GetDurationMs()  -> number` | DurationMs 的旧名称。 |
| `sound.Loaded  -> bool` | 渲染线程构建完音频流后为 true。 |
| `sound.IsPlaying  -> bool` | 声音正在播放时为 true。 |
| `sound:IsFinished()  -> bool` | 播放到达末尾后为 true（暂停或未加载时为 false）。 |
| `sound.Path  -> string` | 已加载文件的完整路径（加载前为空）。 |
| `sound:Dispose()  -> nil` | 释放声音。 |

## HITSOUNDSLIST

当前皮肤中已安装的打击音效组的只读列表（游戏从 `Global/HitSounds/` 读取它们）。

<div class="callout warn">
在每个脚本中可用。索引从 0 起。GetByName 以不区分大小写的方式匹配音效组的文件夹名称。两个查找方法在没有匹配时都返回 nil。
</div>

| 方法 | 说明 |
| --- | --- |
| `HITSOUNDSLIST.Count  -> int` | 打击音效组的数量。 |
| `HITSOUNDSLIST:GetByIndex(index)  -> hitsoundEntry` | 返回给定 0 起索引处的条目，或 nil。 |
| `HITSOUNDSLIST:GetByName(name)  -> hitsoundEntry` | 返回文件夹名称匹配的条目，或 nil。 |

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

## 打击音效条目

由 `HITSOUNDSLIST:GetByIndex` 或 `GetByName` 返回的一个打击音效组。所有成员均为只读。

| 方法 | 说明 |
| --- | --- |
| `hitsoundEntry.FolderName  -> string` | 音效组的文件夹名称（例如 `"Taiko"`）。 |
| `hitsoundEntry.DisplayName  -> string` | 本地化的显示名称；回退为文件夹名称。 |
| `hitsoundEntry.DonPath  -> string` | 该组的咚（Don）音效文件的完整路径。 |
| `hitsoundEntry.KaPath  -> string` | 该组的咔（Ka）音效文件的完整路径。 |
