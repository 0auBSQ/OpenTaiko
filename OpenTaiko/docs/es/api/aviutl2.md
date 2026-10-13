<!-- api/aviutl2.md -->

# Proyectos de AviUtl2

La global `AVIUTL2` reproduce archivos de proyecto guardados por el editor de vídeo AviUtl2 (`.aup2`). El juego lee el proyecto y dibuja sus objetos, movimientos y efectos en directo, así que un skin puede incluir una animación como un proyecto con sus imágenes en lugar de un vídeo renderizado.

El script que carga un proyecto es el propietario de su handle, y el juego libera el handle cuando descarga ese script. Llama a `Dispose()` tú mismo para liberarlo antes.

<div class="callout warn">
El juego dibuja el proyecto con su propio renderizador. Las posiciones, los tamaños, las rotaciones, las curvas de movimiento, el control de cámara y los efectos habituales siguen al editor. Los movimientos aleatorios y las sacudidas usan valores aleatorios distintos, y algunos efectos son aproximaciones. Después de cargar un proyecto, lee GetUnsupported y GetApproximated para ver qué ha omitido o simplificado el juego.
</div>

## Carga

### AVIUTL2

| Método | Descripción |
| --- | --- |
| `AVIUTL2:LoadProject(path)  -> project` | Abre un archivo de proyecto y devuelve su handle. La ruta es relativa al directorio del script. |
| `AVIUTL2:LoadProjectFromAbsolutePath(path)  -> project` | Abre un archivo de proyecto desde una ruta completa. |

<div class="callout warn">
Un proyecto guarda las rutas completas de la máquina en la que se guardó. El juego busca cada imagen, vídeo, sonido y modelo junto al archivo de proyecto, primero por las carpetas que la ruta guardada comparte con la del proyecto y después por el nombre de archivo. Mantén los archivos del proyecto en su carpeta o en subcarpetas de ella. Un proyecto ausente o ilegible devuelve un handle cuyo Loaded es false.
</div>

## Handle de proyecto

Un proyecto devuelto por `AVIUTL2:LoadProject`. Sus métodos de reproducción llevan los mismos nombres que los de un handle de vídeo.

### Reproducción

| Método | Descripción |
| --- | --- |
| `project:Start()  -> nil` | Inicia la reproducción desde el principio. |
| `project:Resume()  -> nil` | Reanuda la reproducción tras Pause. |
| `project:Pause()  -> nil` | Pausa la reproducción, sonidos incluidos. |
| `project:Stop()  -> nil` | Detiene la reproducción. |
| `project:Reset()  -> nil` | Vuelve al inicio. |
| `project.IsPlaying  -> bool` | True mientras el proyecto se reproduce y no ha llegado a su final. |
| `project:IsFinished()  -> bool` | True una vez que un proyecto que no se repite ha llegado a su final. |
| `project:GetTimestampMs()  -> number` | Posición actual en milisegundos. |
| `project:SetTimestampMs(ms)  -> nil` | Salta a una posición en milisegundos. |
| `project:GetFrame()  -> number` | Posición actual en fotogramas del proyecto. |
| `project:SetFrame(frame)  -> nil` | Salta a un fotograma. |
| `project:GetSpeed()  -> number` | Multiplicador de velocidad de reproducción actual (1 = normal). |
| `project:SetSpeed(speed)  -> nil` | Establece el multiplicador de velocidad de reproducción. |
| `project:GetLoop()  -> bool` | True cuando el proyecto vuelve a empezar al llegar a su final. |
| `project:SetLoop(loop)  -> nil` | Hace que el proyecto vuelva a empezar al llegar a su final. |
| `project:GetVolumePercent()  -> number` | Volumen de los sonidos del proyecto en porcentaje. |
| `project:SetVolumePercent(vol)  -> nil` | Fija el volumen de los sonidos del proyecto en porcentaje. |
| `project:Dispose()  -> nil` | Libera el proyecto, sus imágenes, vídeos y sonidos. |

### Dibujo

| Método | Descripción |
| --- | --- |
| `project:Draw(x, y)  -> nil` | Dibuja el fotograma actual con su esquina superior izquierda en (x, y), a la escala fijada por SetScale. |
| `project:DrawFit(x, y, w, h)  -> nil` | Dibuja el fotograma actual tan grande como permite el recuadro, conservando su proporción y centrado en el recuadro. |
| `project:SetScale(scaleX, scaleY)  -> nil` | Fija la escala que usa Draw (1 = el tamaño propio del proyecto). |
| `project:SetOpacity(opacity)  -> nil` | Fija la opacidad de todo el fotograma, de 0 a 1. |
| `project:SetBackground(r, g, b, a)  -> nil` | Rellena el fotograma detrás de los objetos con un color, cada valor de 0 a 255. Un alfa de 0 deja el fotograma transparente. |
| `project:ClearBackground()  -> nil` | Vuelve al fondo que pide el archivo de proyecto. |
| `project.HasProjectBackground  -> bool` | True cuando la escena tiene un color de fondo propio. |
| `project.ProjectBackground  -> int` | El color de fondo de la escena como 0xRRGGBB, o -1 cuando no tiene. |

<div class="callout warn">
Una escena sin color de fondo es transparente donde no se dibuja ningún objeto, así que el proyecto se dibuja sobre lo que el script haya dibujado antes. El editor muestra una escena así sobre negro. Dibuja primero un recuadro negro, o llama a SetBackground(0, 0, 0, 255), para obtener la misma imagen.
</div>

### Imágenes dibujadas por el script

Un script puede sustituir una imagen del proyecto por su propio dibujo, por ejemplo para mostrar la placa de nombre de un jugador dentro de un marco animado.

| Método | Descripción |
| --- | --- |
| `project:SetImageDrawer(fileName, width, height, draw)  -> nil` | Cada objeto que muestra el archivo de imagen `fileName` muestra lo que dibuja la función `draw`. El juego llama a `draw` cada fotograma con un área de dibujo de `width` por `height` cuya esquina superior izquierda es (0, 0). |
| `project:ClearImageDrawer(fileName)  -> nil` | Los objetos vuelven a mostrar el archivo de imagen. |

El juego llama a `draw(rank, layer)` una vez por cada capa que contiene un objeto así. `rank` cuenta esas capas desde 0, empezando por la superior, y `layer` es el número de la capa en el proyecto. Dentro de `draw`, usa las llamadas de dibujo habituales: el juego las redirige a la imagen, que después se mueve, se escala y se desvanece con el objeto.

<div class="callout warn">
La función debe pertenecer al script que llama a SetImageDrawer. Los bordes suaves dibujados dentro de `draw` salen algo más finos que en pantalla.
</div>

### Lectura del proyecto

| Método | Descripción |
| --- | --- |
| `project.Loaded  -> bool` | True cuando el archivo se ha leído. |
| `project.Width  -> int` | Anchura de la escena en píxeles, o -1 cuando no hay nada cargado. |
| `project.Height  -> int` | Altura de la escena en píxeles, o -1 cuando no hay nada cargado. |
| `project.FrameRate  -> number` | Fotogramas por segundo de la escena. |
| `project.FrameCount  -> int` | Duración de la escena en fotogramas. |
| `project.DurationMs  -> number` | Duración de la escena en milisegundos. |
| `project.SceneCount  -> int` | Número de escenas del proyecto. |
| `project:GetScene()  -> int` | Índice de la escena en reproducción. |
| `project:SetScene(index)  -> nil` | Reproduce otra escena del proyecto, desde su inicio. |
| `project:GetSceneName(index)  -> string` | Nombre de una escena. |
| `project.ObjectCount  -> int` | Número de objetos de la escena. |
| `project:GetProjectValue(key)  -> string` | Un ajuste del proyecto, tal como está escrito en el archivo. |
| `project:GetSceneValue(key)  -> string` | Un ajuste de la escena en reproducción. |
| `project:GetValue(section, key)  -> string` | Un ajuste de cualquier sección del archivo, por el nombre de la sección (`"12"` para un objeto, `"12.1"` para su segundo efecto). |
| `project:GetValueAtFrame(objectId, effect, key, frame)  -> number` | Un ajuste numérico del efecto de un objeto en un fotograma, con su movimiento aplicado. |

### Comprobar lo que se dibuja

| Método | Descripción |
| --- | --- |
| `project.UnsupportedCount  -> int` | Número de cosas que el proyecto usa y que el juego no dibuja, archivos ausentes incluidos. |
| `project:GetUnsupported()  -> string` | Esas cosas, una por línea. |
| `project.ApproximatedCount  -> int` | Número de cosas que el juego dibuja de forma simplificada. |
| `project:GetApproximated()  -> string` | Esas cosas, una por línea. |
| `project:AddScriptFolder(path)  -> nil` | Añade una carpeta de archivos de script de AviUtl que los efectos del proyecto pueden nombrar. El juego lee por sí solo una carpeta `Script` situada junto al archivo de proyecto. |
| `project.ScriptErrorCount  -> int` | Número de archivos de script que han fallado. |
| `project:GetScriptErrors()  -> string` | El primer error de cada script fallido, uno por línea. |
| `project.GraphicsMemoryMB  -> number` | Memoria gráfica que el proyecto ocupa actualmente para su propio dibujo, en megabytes. |
| `project:SetSurfaceBudgetMB(mb)  -> nil` | Fija cuánta memoria gráfica conserva el proyecto para reutilizarla entre fotogramas. |

<div class="callout warn">
Los archivos de script incluidos con un proyecto se ejecutan en un entorno cerrado: no pueden leer ni escribir archivos, iniciar programas ni cargar módulos nativos.
</div>

```lua
local intro
local box

function onStart()
    intro = AVIUTL2:LoadProject("Intro/intro.aup2")
    box = TEXTURE:CreateTexture("Intro/player_box.png")
    -- la placa de nombre de cada jugador, dibujada en los recuadros de jugador del proyecto
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
