<!-- api/audio.md -->

# Аудио

Загрузка и воспроизведение звуков, а также чтение установленных наборов хитсаундов.

## SOUND

Создаёт дескрипторы звуков из аудиофайлов. Каждый фабричный метод относит звук к группе громкости (звуковой эффект, голос, воспроизведение песни или превью песни), а группа определяет, какая пользовательская настройка громкости применяется.

<div class="callout warn">
Относительные пути разрешаются относительно каталога скрипта; варианты FromAbsolutePath принимают полный путь. Возвращаемый дескриптор изначально пуст, пока поток рендеринга создаёт аудиопоток в течение следующих кадров; вызовы Play и Set*, сделанные до этого, дескриптор ставит в очередь и применяет, как только аудиопоток готов. Игра освобождает дескрипторы при выгрузке скрипта; вызовите Dispose, чтобы освободить дескриптор раньше.
</div>

| Метод | Описание |
| --- | --- |
| `SOUND:CreateSFX(path)  -> sound` | Загружает звуковой эффект. |
| `SOUND:CreateVoice(path)  -> sound` | Загружает голосовую реплику. |
| `SOUND:CreateBGM(path)  -> sound` | Загружает фоновую музыку (группа воспроизведения песни). |
| `SOUND:CreatePreview(path)  -> sound` | Загружает фрагмент превью песни (группа превью песни). |
| `SOUND:CreateSFXFromAbsolutePath(path)  -> sound` | Загружает звуковой эффект по полному пути. |
| `SOUND:CreateVoiceFromAbsolutePath(path)  -> sound` | Загружает голосовую реплику по полному пути. |
| `SOUND:CreateBGMFromAbsolutePath(path)  -> sound` | Загружает фоновую музыку по полному пути. |
| `SOUND:CreatePreviewFromAbsolutePath(path)  -> sound` | Загружает фрагмент превью песни по полному пути. |

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

## Дескриптор звука

Воспроизводимый звук, возвращаемый фабричными методами `SOUND`.

<div class="callout warn">
Пока поток не готов, дескриптор буферизует Play и каждый вызов Set*, а геттеры возвращают значения по умолчанию, перечисленные ниже. Громкость задаётся в процентах (100 = собственный уровень файла). Панорама идёт от -100 (слева) через 0 (центр) до 100 (справа). Позиции и длительности задаются в миллисекундах. Play перезапускает поток с начала, поэтому перематывайте после вызова Play.
</div>

| Метод | Описание |
| --- | --- |
| `sound:Play()  -> nil` | Запускает (или перезапускает) воспроизведение с начала. |
| `sound:Start()  -> nil` | То же, что Play. |
| `sound:Pause()  -> nil` | Приостанавливает воспроизведение. |
| `sound:Resume()  -> nil` | Возобновляет воспроизведение после Pause. |
| `sound:Stop()  -> nil` | Останавливает воспроизведение. |
| `sound:Reset()  -> nil` | Перематывает на начало. |
| `sound:SetLoop(loop)  -> nil` | Включает или выключает зацикливание. |
| `sound:GetLoop()  -> bool` | Возвращает, включено ли зацикливание. |
| `sound:SetVolumePercent(vol)  -> nil` | Задаёт громкость в процентах. |
| `sound:GetVolumePercent()  -> number` | Возвращает громкость в процентах (100, если звук не загружен). |
| `sound:SetVolume(vol)  -> nil` | Старое имя для SetVolumePercent (целое число). |
| `sound:SetPan(pan)  -> nil` | Задаёт стереопанораму, -100..100. |
| `sound:GetPan()  -> int` | Возвращает текущую панораму (0, если звук не загружен). |
| `sound:SetSpeed(speed)  -> nil` | Задаёт множитель скорости воспроизведения (1 = обычная). |
| `sound:GetSpeed()  -> number` | Возвращает множитель скорости воспроизведения (1, если звук не загружен). |
| `sound:SetTimestampMs(ms)  -> nil` | Перематывает на позицию в миллисекундах. |
| `sound:SetTimestamp(ms)  -> nil` | Старое имя для SetTimestampMs. |
| `sound:GetTimestampMs()  -> number` | Возвращает текущую позицию в миллисекундах (0, если звук не загружен). |
| `sound.DurationMs  -> number` | Общая длительность в миллисекундах (0, если звук не загружен). |
| `sound:GetDurationMs()  -> number` | Старое имя для DurationMs. |
| `sound.Loaded  -> bool` | Истина после того, как поток рендеринга создал аудиопоток. |
| `sound.IsPlaying  -> bool` | Истина, пока звук воспроизводится. |
| `sound:IsFinished()  -> bool` | Истина после того, как воспроизведение дошло до конца (ложь во время паузы или если звук не загружен). |
| `sound.Path  -> string` | Полный путь загруженного файла (пустой до загрузки). |
| `sound:Dispose()  -> nil` | Освобождает звук. |

## HITSOUNDSLIST

Список только для чтения наборов хитсаундов, установленных в текущем скине (игра читает их из `Global/HitSounds/`).

<div class="callout warn">
Доступен в каждом скрипте. Индексы начинаются с 0. GetByName сопоставляет имя папки набора без учёта регистра. Оба поиска возвращают nil, если ничего не совпало.
</div>

| Метод | Описание |
| --- | --- |
| `HITSOUNDSLIST.Count  -> int` | Число наборов хитсаундов. |
| `HITSOUNDSLIST:GetByIndex(index)  -> hitsoundEntry` | Возвращает запись по указанному индексу от 0 или nil. |
| `HITSOUNDSLIST:GetByName(name)  -> hitsoundEntry` | Возвращает запись, имя папки которой совпадает, или nil. |

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

## Запись набора хитсаундов

Один набор хитсаундов, возвращаемый `HITSOUNDSLIST:GetByIndex` или `GetByName`. Все члены доступны только для чтения.

| Метод | Описание |
| --- | --- |
| `hitsoundEntry.FolderName  -> string` | Имя папки набора (например `"Taiko"`). |
| `hitsoundEntry.DisplayName  -> string` | Локализованное отображаемое имя; откатывается к имени папки. |
| `hitsoundEntry.DonPath  -> string` | Полный путь к звуковому файлу Don набора. |
| `hitsoundEntry.KaPath  -> string` | Полный путь к звуковому файлу Ka набора. |
