<!-- api/audio.md -->

# Audio

Carga y reproducción de sonidos, y lectura de los conjuntos de hitsounds instalados.

## SOUND

Crea handles de sonido a partir de archivos de audio. Cada método de fábrica asigna el sonido a un grupo de volumen (efecto de sonido, voz, reproducción de canción o vista previa de canción), y el grupo decide qué ajuste de volumen del usuario se aplica.

<div class="callout warn">
Las rutas relativas se resuelven respecto al directorio del script; las variantes FromAbsolutePath reciben una ruta completa. El handle devuelto empieza vacío mientras el hilo de renderizado construye el flujo durante los fotogramas siguientes; el handle encola las llamadas a Play y Set* hechas antes y las aplica cuando el flujo está listo. El juego libera los handles cuando descarga el script; llama a Dispose para liberar uno antes.
</div>

| Método | Descripción |
| --- | --- |
| `SOUND:CreateSFX(path)  -> sound` | Carga un efecto de sonido. |
| `SOUND:CreateVoice(path)  -> sound` | Carga un clip de voz. |
| `SOUND:CreateBGM(path)  -> sound` | Carga música de fondo (grupo de reproducción de canción). |
| `SOUND:CreatePreview(path)  -> sound` | Carga un clip de vista previa de canción (grupo de vista previa de canción). |
| `SOUND:CreateSFXFromAbsolutePath(path)  -> sound` | Carga un efecto de sonido desde una ruta completa. |
| `SOUND:CreateVoiceFromAbsolutePath(path)  -> sound` | Carga un clip de voz desde una ruta completa. |
| `SOUND:CreateBGMFromAbsolutePath(path)  -> sound` | Carga música de fondo desde una ruta completa. |
| `SOUND:CreatePreviewFromAbsolutePath(path)  -> sound` | Carga un clip de vista previa de canción desde una ruta completa. |

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

## Handle de sonido

Un sonido reproducible devuelto por los métodos de fábrica de `SOUND`.

<div class="callout warn">
Hasta que el flujo está listo, el handle almacena Play y todas las llamadas Set*, y los getters devuelven los valores por defecto listados abajo. El volumen es un porcentaje (100 = el nivel propio del archivo). El paneo va de -100 (izquierda) pasando por 0 (centro) hasta 100 (derecha). Las posiciones y duraciones están en milisegundos. Play reinicia el flujo desde el principio, así que busca la posición después de llamar a Play.
</div>

| Método | Descripción |
| --- | --- |
| `sound:Play()  -> nil` | Inicia (o reinicia) la reproducción desde el principio. |
| `sound:Start()  -> nil` | Igual que Play. |
| `sound:Pause()  -> nil` | Pausa la reproducción. |
| `sound:Resume()  -> nil` | Reanuda la reproducción tras Pause. |
| `sound:Stop()  -> nil` | Detiene la reproducción. |
| `sound:Reset()  -> nil` | Vuelve al inicio. |
| `sound:SetLoop(loop)  -> nil` | Activa o desactiva la repetición. |
| `sound:GetLoop()  -> bool` | Devuelve si la repetición está activada. |
| `sound:SetVolumePercent(vol)  -> nil` | Establece el volumen en porcentaje. |
| `sound:GetVolumePercent()  -> number` | Devuelve el volumen en porcentaje (100 cuando no está cargado). |
| `sound:SetVolume(vol)  -> nil` | Nombre antiguo de SetVolumePercent (entero). |
| `sound:SetPan(pan)  -> nil` | Establece el paneo estéreo, -100..100. |
| `sound:GetPan()  -> int` | Devuelve el paneo actual (0 cuando no está cargado). |
| `sound:SetSpeed(speed)  -> nil` | Establece el multiplicador de velocidad de reproducción (1 = normal). |
| `sound:GetSpeed()  -> number` | Devuelve el multiplicador de velocidad de reproducción (1 cuando no está cargado). |
| `sound:SetTimestampMs(ms)  -> nil` | Salta a una posición en milisegundos. |
| `sound:SetTimestamp(ms)  -> nil` | Nombre antiguo de SetTimestampMs. |
| `sound:GetTimestampMs()  -> number` | Devuelve la posición actual en milisegundos (0 cuando no está cargado). |
| `sound.DurationMs  -> number` | Duración total en milisegundos (0 cuando no está cargado). |
| `sound:GetDurationMs()  -> number` | Nombre antiguo de DurationMs. |
| `sound.Loaded  -> bool` | Verdadero una vez que el hilo de renderizado ha construido el flujo. |
| `sound.IsPlaying  -> bool` | Verdadero mientras el sonido se está reproduciendo. |
| `sound:IsFinished()  -> bool` | Verdadero una vez que la reproducción ha llegado al final (falso mientras está en pausa o no cargado). |
| `sound.Path  -> string` | La ruta completa del archivo cargado (vacía hasta que se carga). |
| `sound:Dispose()  -> nil` | Libera el sonido. |

## HITSOUNDSLIST

Lista de solo lectura de los conjuntos de hitsounds instalados en el skin actual (el juego los lee desde `Global/HitSounds/`).

<div class="callout warn">
Disponible en todos los scripts. Los índices empiezan en 0. GetByName compara el nombre de carpeta del conjunto sin distinguir mayúsculas. Ambas búsquedas devuelven nil cuando nada coincide.
</div>

| Método | Descripción |
| --- | --- |
| `HITSOUNDSLIST.Count  -> int` | Número de conjuntos de hitsounds. |
| `HITSOUNDSLIST:GetByIndex(index)  -> hitsoundEntry` | Devuelve la entrada en el índice indicado (desde 0), o nil. |
| `HITSOUNDSLIST:GetByName(name)  -> hitsoundEntry` | Devuelve la entrada cuyo nombre de carpeta coincide, o nil. |

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

## Entrada de hitsound

Un conjunto de hitsounds devuelto por `HITSOUNDSLIST:GetByIndex` o `GetByName`. Todos los miembros son de solo lectura.

| Método | Descripción |
| --- | --- |
| `hitsoundEntry.FolderName  -> string` | Nombre de carpeta del conjunto (por ejemplo `"Taiko"`). |
| `hitsoundEntry.DisplayName  -> string` | Nombre de visualización localizado; recurre al nombre de carpeta. |
| `hitsoundEntry.DonPath  -> string` | Ruta completa del archivo de sonido Don del conjunto. |
| `hitsoundEntry.KaPath  -> string` | Ruta completa del archivo de sonido Ka del conjunto. |
