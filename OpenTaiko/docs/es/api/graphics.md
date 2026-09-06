<!-- api/graphics.md -->

# Gráficos y texto

Texturas, canvas, recorte, texto, vídeo, colores, mapas de gradiente y tamaños. Todas las posiciones y tamaños están en píxeles lógicos de pantalla, el espacio de coordenadas en el que dibujan los scripts (consulta `INPUT:GetSurfaceWidth()` / `GetSurfaceHeight()`).

El script que crea un handle de textura, canvas, texto, vídeo o gradiente es su propietario, y el juego libera el handle cuando descarga ese script. Llama a `Dispose()` tú mismo para liberar un handle antes.

## Texturas

### TEXTURE

Carga archivos de imagen en handles de textura.

<div class="callout warn">
Las rutas relativas se resuelven respecto al directorio del script; las variantes FromAbsolutePath reciben una ruta completa. Un archivo ausente devuelve un handle vacío que no dibuja nada. CreateTexture carga de forma asíncrona: el handle no dibuja nada y reporta Width y Height de 0 hasta que terminan la decodificación y la subida en segundo plano. Usa CreateTextureSync cuando necesites el tamaño o los píxeles de inmediato. La tabla de opciones acepta { maxSize = N } para reducir la imagen al decodificarla de modo que su lado más largo tenga como máximo N píxeles.
</div>

| Método | Descripción |
| --- | --- |
| `TEXTURE:CreateTexture()  -> texture` | Crea un handle vacío sin imagen. |
| `TEXTURE:CreateTexture(path)  -> texture` | Carga una imagen de forma asíncrona desde una ruta relativa al directorio del script. |
| `TEXTURE:CreateTexture(path, options)  -> texture` | Igual, con una tabla de opciones (`{ maxSize = N }`). |
| `TEXTURE:CreateTextureSync(path)  -> texture` | Carga una imagen de forma síncrona; el tamaño y los píxeles están disponibles al retornar. |
| `TEXTURE:CreateTextureFromAbsolutePath(path)  -> texture` | Carga una imagen desde una ruta completa del sistema de archivos. |
| `TEXTURE:CreateTextureFromAbsolutePath(path, options)  -> texture` | Igual, con una tabla de opciones (`{ maxSize = N }`). |
| `TEXTURE:Exists(path)  -> bool` | Devuelve si existe un archivo en la ruta relativa al directorio del script. |

```lua
local logo, jacket

function onStart()
    logo = TEXTURE:CreateTexture("Textures/Logo.png")
    jacket = TEXTURE:CreateTexture("Textures/Jacket.png", { maxSize = 512 })
end

function draw()
    logo:DrawAtAnchor(960, 540, "center")
end

function onDestroy()
    logo:Dispose()
    jacket:Dispose()
end
```

### Handle de textura

Una imagen 2D dibujable devuelta por la fábrica `TEXTURE`, por `text:GetText` / `GetVerticalText` y por `video.Texture`.

<div class="callout warn">
Nombres de ancla: topleft, top, topright, left, center, right, bottomleft, bottom, bottomright (sin distinguir mayúsculas; un nombre desconocido recurre a topleft). Los dibujos anclados tienen en cuenta la escala actual. Nombres de modo de mezcla: Normal, Add, Multi, Sub, Screen. Nombres de modo de envoltura: Edge, Border, Repeat, Mirror; los handles nuevos usan Repeat por defecto. En un handle vacío todos los métodos son operaciones nulas y los getters devuelven los valores por defecto listados abajo.
</div>

| Método | Descripción |
| --- | --- |
| `texture:Draw(x, y)  -> nil` | Dibuja la textura completa con su esquina superior izquierda en (x, y). |
| `texture:DrawRect(x, y, rect_x, rect_y, rect_width, rect_height)  -> nil` | Dibuja un subrectángulo de origen (en píxeles de textura) con su esquina superior izquierda en (x, y). |
| `texture:DrawAtAnchor(x, y, anchor)  -> nil` | Dibuja la textura completa de modo que el punto de ancla indicado caiga en (x, y). |
| `texture:DrawRectAtAnchor(x, y, rect_x, rect_y, rect_width, rect_height, anchor)  -> nil` | Dibuja un subrectángulo de origen de modo que el punto de ancla indicado caiga en (x, y). |
| `texture.Loaded  -> bool` | Verdadero cuando el handle envuelve una textura. El juego lo establece en cuanto encuentra el archivo, antes de que termine una carga asíncrona. |
| `texture.Width  -> int` | Anchura en píxeles; -1 en un handle vacío, 0 mientras hay una carga asíncrona pendiente. |
| `texture.Height  -> int` | Altura en píxeles; -1 en un handle vacío, 0 mientras hay una carga asíncrona pendiente. |
| `texture.Pointer  -> int` | Id nativo de textura GL, o 0 si no hay ninguno. |
| `texture:GetScale()  -> vector2` | Escala de dibujo actual como vector2 (`X`, `Y`). |
| `texture:GetOpacity()  -> number` | Opacidad actual, 0..1; -1 en un handle vacío. |
| `texture:GetColor()  -> number, number, number` | Tinte actual como tres valores 0..1 (rojo, verde, azul). |
| `texture:GetRotation()  -> number` | Rotación actual en grados. |
| `texture:GetBlendMode()  -> string` | Nombre del modo de mezcla actual. |
| `texture:GetWrapMode()  -> string` | Nombre del modo de envoltura actual. |
| `texture:SetScale(scale_x, scale_y)  -> nil` | Establece la escala de dibujo horizontal y vertical (1 = tamaño original). |
| `texture:SetOpacity(opacity)  -> nil` | Establece la opacidad, 0..1. |
| `texture:SetColor(color)  -> nil` | Establece el tinte a partir de un valor `COLOR`. El handle ignora el alfa del color; establece la transparencia con SetOpacity. |
| `texture:SetColor(red, green, blue)  -> nil` | Establece el tinte a partir de tres valores 0..1. |
| `texture:SetRotation(angle)  -> nil` | Establece la rotación alrededor del centro de la textura, en grados. |
| `texture:SetBlendMode(mode)  -> nil` | Establece el modo de mezcla por nombre (sin distinguir mayúsculas). El handle ignora los nombres desconocidos. |
| `texture:SetWrapMode(mode)  -> nil` | Establece el modo de envoltura por nombre (sin distinguir mayúsculas). El handle ignora los nombres desconocidos. |
| `texture:SetUseNoiseEffect(enabled)  -> nil` | Cuando está activado, cada dibujado reemplaza el color de la textura por ruido aleatorio animado en escala de grises y conserva su alfa. |
| `texture:SetGradientMap(gradient)  -> nil` | Aplica un mapa `GRADIENT` a cada dibujo de esta textura (consulta GRADIENT). |
| `texture:ClearGradientMap()  -> nil` | Elimina el mapa de gradiente por textura. |
| `texture:Dispose()  -> nil` | Libera la textura. |

## Canvas y recorte

### CANVAS

Crea superficies de píxeles escribibles que Lua edita en la CPU y sube como una sola textura.

<div class="callout warn">
Un canvas es una textura cuyos píxeles establece Lua (SetPixel, FillRect, ...) y luego envía a la GPU con Upload. Sirve para el renderizado por software y para formas de interfaz horneadas, donde un script pinta y sube una vez y luego dibuja el resultado cada fotograma. Los canvas nuevos empiezan completamente transparentes.
</div>

| Método | Descripción |
| --- | --- |
| `CANVAS:CreateCanvas(width, height)  -> canvas` | Crea un canvas transparente del tamaño indicado (mínimo 1x1). |

```lua
local panel

function onStart()
    panel = CANVAS:CreateCanvas(300, 80)
    panel:FillRect(0, 0, 300, 80, 20, 20, 40, 220)
    panel:FillCircle(40, 40, 24, 255, 200, 0, 255)
    panel:Upload()
end

function draw()
    panel:Draw(100, 100)
end

function onDestroy()
    panel:Dispose()
end
```

### Handle de canvas

Un búfer de píxeles RGBA escribible devuelto por `CANVAS:CreateCanvas`, dibujable como una textura.

<div class="callout warn">
Los argumentos de color r, g, b, a son enteros 0-255. Las ediciones de píxeles se acumulan en un rectángulo sucio y solo llegan a la GPU cuando llamas a Upload (BlitPacked sube por sí mismo). Las coordenadas de dibujo son enteros. Los nombres de ancla son los mismos que para el handle de textura.
</div>

| Método | Descripción |
| --- | --- |
| `canvas.Width  -> int` | Anchura en píxeles. |
| `canvas.Height  -> int` | Altura en píxeles. |
| `canvas.Pointer  -> int` | Id nativo de textura GL, o 0 si no hay ninguno. |
| `canvas:SetPixel(x, y, r, g, b, a)  -> nil` | Establece un píxel. El canvas ignora las coordenadas fuera de rango. |
| `canvas:FillRect(x, y, w, h, r, g, b, a)  -> nil` | Rellena un rectángulo alineado con los ejes, recortado al canvas. |
| `canvas:FillCircle(cx, cy, radius, r, g, b, a)  -> nil` | Rellena un disco del radio indicado en píxeles. |
| `canvas:StrokeLine(x0, y0, x1, y1, radius, r, g, b, a)  -> nil` | Pinta una línea gruesa como discos superpuestos del radio indicado. |
| `canvas:PasteTexture(texture, x, y)  -> nil` | Mezcla por alfa una textura sobre el canvas con su esquina superior izquierda en (x, y). El canvas lee de vuelta los píxeles de la textura desde la GPU una vez por handle de textura, una operación lenta que corresponde al código de preparación. |
| `canvas:PasteTextureTransformed(texture, x, y, scale, rotationDeg, anchor)  -> nil` | Mezcla por alfa una textura escalada por `scale` y rotada en sentido horario `rotationDeg`, con muestreo de vecino más cercano. Con el ancla "center" el centro de la textura cae en (x, y); cualquier otro valor coloca ahí su esquina superior izquierda. |
| `canvas:Clear(r, g, b, a)  -> nil` | Rellena todo el canvas con un color. |
| `canvas:ClearTransparent()  -> nil` | Reinicia todo el canvas a completamente transparente. |
| `canvas:CopyFrom(other)  -> nil` | Copia en este canvas los píxeles de otro canvas del mismo tamaño. La llamada no hace nada cuando los tamaños difieren. |
| `canvas:BlitPacked(data, count)  -> nil` | Rellena la superficie desde un array Lua indexado desde 1 de enteros empaquetados `0xRRGGBB` en orden por filas (los píxeles pasan a ser opacos; un valor negativo se convierte en un píxel transparente) y después sube. |
| `canvas:Upload()  -> nil` | Envía las ediciones pendientes (solo la región cambiada) a la GPU. Operación nula cuando nada cambió. |
| `canvas:Draw(x, y)  -> nil` | Dibuja el canvas con su esquina superior izquierda en (x, y). |
| `canvas:DrawRect(x, y, rect_x, rect_y, rect_width, rect_height)  -> nil` | Dibuja un subrectángulo de origen (en píxeles del canvas) con su esquina superior izquierda en (x, y). |
| `canvas:DrawAtAnchor(x, y, anchor)  -> nil` | Dibuja el canvas de modo que el punto de ancla indicado caiga en (x, y). |
| `canvas:SetScale(scale_x, scale_y)  -> nil` | Establece la escala de dibujo horizontal y vertical. |
| `canvas:SetOpacity(opacity)  -> nil` | Establece la opacidad, 0..1. |
| `canvas:SetColor(red, green, blue)  -> nil` | Establece el tinte a partir de tres valores 0..1. |
| `canvas:Dispose()  -> nil` | Libera la textura GPU y el búfer CPU del canvas. |

### GRAPHICS

Recorte por tijera (scissor) para paneles con desplazamiento.

<div class="callout warn">
SetClip recibe coordenadas lógicas de pantalla y las asigna al viewport actual, así que el recorte es el mismo con escalado de renderizado y letterboxing. La GPU descarta cada píxel dibujado fuera del rectángulo entre SetClip y ClearClip. SetClip reemplaza el rectángulo anterior (no hay pila); mantén SetClip y ClearClip emparejados. El juego desactiva la tijera al inicio de cada fotograma.
</div>

| Método | Descripción |
| --- | --- |
| `GRAPHICS:SetClip(x, y, w, h)  -> nil` | Activa el recorte al rectángulo indicado. |
| `GRAPHICS:ClearClip()  -> nil` | Desactiva el recorte. |

```lua
GRAPHICS:SetClip(100, 200, 600, 400)
for i, row in ipairs(rows) do
    row.tex:Draw(100, 200 + (i - 1) * 48 - scrollY)
end
GRAPHICS:ClearClip()
```

## Texto

### TEXT

Crea renderizadores de fuente para la fuente principal del skin.

<div class="callout warn">
Create devuelve un handle de texto que renderiza cadenas completas a texturas en caché; úsalo para etiquetas que cambian poco. CreateGlyphCached devuelve un handle de texto por glifos que guarda en caché una textura por carácter y compone las cadenas al dibujar; úsalo para texto que cambia a menudo (temporizadores, puntuaciones, texto escrito) y para bloques con ajuste de línea. Los argumentos de estilo son cualquiera de bold, italic, underline, strikeout (sin distinguir mayúsculas; otros valores significan regular). Dentro de un mismo script, la versión de NLua incluida falla si el script llama al mismo método de fábrica primero sin argumento de estilo y luego con exactamente un argumento de estilo. O bien nunca pases un estilo, o pásalo siempre desde la primera llamada, o pasa dos tokens de estilo (por ejemplo "bold", "regular").
</div>

| Método | Descripción |
| --- | --- |
| `TEXT:Create(size, ...style)  -> text` | Crea un renderizador de texto de cadena completa con el tamaño en píxeles indicado. |
| `TEXT:CreateGlyphCached(size, ...style)  -> glyphText` | Crea un renderizador de texto compuesto por glifos con el tamaño en píxeles indicado. |

#### Etiquetas de color en línea

Ambos renderizadores respetan estas etiquetas dentro de la cadena. Las etiquetas se anidan; una etiqueta sin cerrar se extiende hasta el final de la cadena. Las funciones de medición las ignoran.

| Etiqueta | Efecto |
| --- | --- |
| `<c.#rrggbb>` ... `</c>` | Color de relleno. |
| `<c.#rrggbb.#rrggbb>` ... `</c>` | Color de relleno y color de contorno. |
| `<g.#rrggbb.#rrggbb>` ... `</g>` | Relleno con gradiente vertical (color superior, color inferior). |

### Handle de texto

Un renderizador de cadena completa devuelto por `TEXT:Create`.

<div class="callout warn">
GetText y GetVerticalText guardan en caché una textura por cada combinación única de cadena, color de relleno, color de contorno y límite de tamaño, y la conservan hasta que liberas el handle. Por tanto, una cadena que cambia cada fotograma añade una textura cada fotograma y filtra memoria de GPU; dibuja ese texto con DrawDirect o con un handle de texto por glifos. forecolor y backcolor son valores COLOR; los valores por defecto son un relleno blanco y un contorno negro opaco. El indicador centered centra cada línea de una cadena multilínea; la clave de caché lo excluye, así que la primera llamada para una cadena dada lo decide.
</div>

| Método | Descripción |
| --- | --- |
| `text:GetText(text, centered, max_width, forecolor, backcolor)  -> texture` | Renderiza una cadena horizontal a una textura en caché. Todos los argumentos después de `text` son opcionales. Cuando la textura renderizada es más ancha que `max_width`, se dibuja comprimida horizontalmente para que quepa. |
| `text:GetVerticalText(text, centered, max_height, forecolor, backcolor)  -> texture` | Renderiza una cadena apilada verticalmente a una textura en caché. Cuando la textura renderizada es más alta que `max_height`, se dibuja comprimida verticalmente para que quepa. |
| `text:DrawDirect(text, x, y, r, g, b, opacity, spacing)  -> number` | Dibuja una cadena glifo a glifo desde una caché por carácter y devuelve la posición x tras el último glifo. `r`, `g`, `b` son 0-255 (255 por defecto), `opacity` es 0..1 (1 por defecto), `spacing` es la separación entre glifos en píxeles (-6 por defecto). |
| `text:Dispose()  -> nil` | Libera la fuente y todas las texturas en caché. |

```lua
local font, title

function onStart()
    font = TEXT:Create(28)
    title = font:GetText("Song select", true, 600)
end

function draw()
    title:DrawAtAnchor(960, 80, "center")
    font:DrawDirect(tostring(score), 40, 40, 255, 255, 0)
end

function onDestroy()
    font:Dispose()
end
```

### Handle de texto por glifos

Un renderizador compuesto por glifos devuelto por `TEXT:CreateGlyphCached`.

<div class="callout warn">
forecolor y backcolor son valores COLOR; los valores por defecto son un relleno blanco y un contorno negro. Todos los argumentos después de la posición son opcionales. maxWidth (0 = sin límite) comprime cada glifo horizontalmente para que la línea completa quepa. anchor usa los mismos nueve nombres que el handle de textura. '\n' inicia una nueva línea; Draw apila las líneas alineadas a la izquierda. Draw devuelve la coordenada x del borde derecho de la caja dibujada. La geometría de la caja coincide con las texturas de GetText (25 px de relleno a cada lado y debajo de la tinta), así que un texto por glifos y una textura de GetText dibujados en el mismo punto quedan alineados.
</div>

| Método | Descripción |
| --- | --- |
| `glyphText.LineHeight  -> number` | Interlineado del texto multilínea, en píxeles. |
| `glyphText.BoxHeight  -> number` | Altura de una caja de una línea (tinta más relleno), igual que una textura de GetText de una línea. |
| `glyphText:Measure(text, scale)  -> number` | Anchura de tinta de la línea más ancha, multiplicada por `scale` (1 por defecto). |
| `glyphText:Draw(text, x, y, forecolor, backcolor, opacity, scale, maxWidth, anchor, scaleY, rotationDeg)  -> number` | Dibuja el texto. `opacity` 0..1 (1 por defecto); `scale` (1 por defecto); `scaleY` cuando es > 0 sobrescribe la escala vertical; `rotationDeg` rota todo el bloque alrededor de (x, y). Devuelve la x del borde derecho de la caja. |
| `glyphText:SetClipY(y0, y1)  -> nil` | Restringe las siguientes llamadas a Draw sin rotación a la banda vertical y0..y1 en espacio de pantalla; Draw omite los glifos fuera de la banda y recorta los glifos en sus bordes. Pasa y1 <= y0 para desactivarlo. Los dibujos rotados ignoran la banda. |
| `glyphText:WrapToLines(text, wrapWidth, scale)  -> string[]` | Ajusta las líneas a `wrapWidth` y devuelve las líneas como cadenas simples (sin etiquetas). |
| `glyphText:MeasureWrapped(text, wrapWidth, scale, lineSpacing)  -> number` | Altura que usaría DrawWrapped, sin dibujar. `lineSpacing` multiplica el interlineado (1 por defecto). |
| `glyphText:DrawWrapped(text, x, y, wrapWidth, forecolor, backcolor, opacity, scale, lineSpacing)  -> number` | Ajusta las líneas a `wrapWidth` y dibuja alineado a la izquierda desde (x, y). Devuelve la altura dibujada. |
| `glyphText:Dispose()  -> nil` | Libera la fuente y todos los glifos en caché. |

El ajuste de línea corta en los espacios, entre caracteres CJK y dentro de una palabra más ancha que el ancho de ajuste.

```lua
local font, white

function onStart()
    font = TEXT:CreateGlyphCached(24)
    white = COLOR:CreateColorFromRGBA(255, 255, 255)
end

function draw()
    font:Draw("Time " .. string.format("%.1f", t), 960, 40, white, nil, 1, 1, 0, "center")
    font:DrawWrapped(description, 200, 300, 800, white)
end

function onDestroy()
    font:Dispose()
end
```

## Vídeo

### VIDEO

Carga archivos de vídeo en handles de decodificador reproducibles.

<div class="callout warn">
Las rutas son relativas al directorio del script. El decodificador se abre en un hilo en segundo plano; hasta que está listo, el handle no dibuja nada, encola las llamadas a Start, de búsqueda y de velocidad, y las aplica cuando el decodificador se conecta. Un archivo ausente devuelve un handle vacío.
</div>

| Método | Descripción |
| --- | --- |
| `VIDEO:CreateVideo(path)  -> video` | Abre un archivo de vídeo y devuelve su handle. |

### Handle de vídeo

Un decodificador de vídeo devuelto por `VIDEO:CreateVideo`.

<div class="callout warn">
Las posiciones están en milisegundos; Duration está en segundos. Lee Texture cada fotograma para obtener el fotograma actual como handle de textura. Ese handle envuelve la textura de fotograma propia del vídeo: dibújala y deja que el handle de vídeo se encargue de liberarla. SetSpeed ignora los valores de 0 o menos.
</div>

| Método | Descripción |
| --- | --- |
| `video:Start()  -> nil` | Inicia la reproducción. |
| `video:Resume()  -> nil` | Reanuda la reproducción tras Pause. |
| `video:Pause()  -> nil` | Pausa la reproducción. |
| `video:Stop()  -> nil` | Detiene la reproducción. |
| `video:Reset()  -> nil` | Vuelve al inicio. |
| `video.Width  -> int` | Anchura del fotograma en píxeles, o -1 hasta que el decodificador esté listo. |
| `video.Height  -> int` | Altura del fotograma en píxeles, o -1 hasta que el decodificador esté listo. |
| `video.Duration  -> number` | Duración total en segundos (1 hasta que el decodificador esté listo). |
| `video.DurationMs  -> number` | Duración total en milisegundos. |
| `video.Texture  -> texture` | El fotograma decodificado actual. |
| `video:IsFinished()  -> bool` | Verdadero una vez que el flujo ha terminado y no quedan fotogramas. |
| `video:GetTimestampMs()  -> number` | Posición de reproducción actual en milisegundos. |
| `video:SetTimestampMs(ms)  -> nil` | Salta a una posición en milisegundos. |
| `video:GetSpeed()  -> number` | Multiplicador de velocidad de reproducción actual (1 = normal). |
| `video:SetSpeed(speed)  -> nil` | Establece el multiplicador de velocidad de reproducción. |
| `video:GetPlayPosition()  -> number` | Posición actual en segundos (nombre antiguo de GetTimestampMs / 1000). |
| `video:SetPlayPosition(seconds)  -> nil` | Salta a una posición en segundos (nombre antiguo de SetTimestampMs). |
| `video:GetPlaySpeed()  -> number` | Nombre antiguo de GetSpeed. |
| `video:SetPlaySpeed(speed)  -> nil` | Nombre antiguo de SetSpeed. |
| `video:Dispose()  -> nil` | Libera el decodificador y su textura de fotograma. |

```lua
local intro

function onStart()
    intro = VIDEO:CreateVideo("Videos/intro.mp4")
end

function activate()
    intro:Start()
end

function draw()
    intro.Texture:Draw(0, 0)
    if intro:IsFinished() then Exit("title", nil) end
end

function deactivate()
    intro:Stop()
end

function onDestroy()
    intro:Dispose()
end
```

## Color, gradiente y tamaño

### COLOR

Crea valores de color.

<div class="callout warn">
Los canales son 0-255. Los métodos de textura, canvas y texto que reciben un color aceptan estos valores.
</div>

| Método | Descripción |
| --- | --- |
| `COLOR:CreateColorFromRGBA(r, g, b, a)  -> color` | Crea un color a partir de rojo, verde, azul y un alfa opcional (255 por defecto). |
| `COLOR:CreateColorFromARGB(a, r, g, b)  -> color` | Crea un color a partir de alfa, rojo, verde y azul. |
| `COLOR:CreateColorFromHex(value)  -> color` | Analiza una cadena hexadecimal `AARRGGBB` sin prefijo (por ejemplo `"ff2080ff"`). Una cadena de seis dígitos produce alfa 0. Una cadena no analizable produce blanco opaco. |

### Handle de color

Un valor de color devuelto por la fábrica `COLOR`.

| Método | Descripción |
| --- | --- |
| `color.R  -> int` | Canal rojo, 0-255 (lectura/escritura). |
| `color.G  -> int` | Canal verde, 0-255 (lectura/escritura). |
| `color.B  -> int` | Canal azul, 0-255 (lectura/escritura). |
| `color.A  -> int` | Canal alfa, 0-255 (lectura/escritura). |

### GRADIENT

Los mapas de gradiente recolorean los dibujos de texturas asignando la luminancia de cada píxel a una rampa de color, ya sea en una textura o en todos los dibujos.

<div class="callout warn">
Create recibe una tabla de al menos dos paradas, cada una un array { posición 0-1, r 0-255, g 0-255, b 0-255 [, a 0-255] }, y una cantidad de mezcla (0 = sin efecto, 1 = reemplazo total, los valores intermedios mezclan; 1 por defecto). Menos de dos paradas lanza un error. Unas paradas y una mezcla idénticas reutilizan una única textura GPU en caché durante todo el programa. Aplícalo a una sola textura con texture:SetGradientMap, o a todos los dibujos de texturas con SetActive.
</div>

| Método | Descripción |
| --- | --- |
| `GRADIENT:Create(stops, blend)  -> gradient` | Construye (o reutiliza) un mapa de gradiente a partir de paradas de color y una cantidad de mezcla. |
| `GRADIENT:SetActive(gradient)  -> nil` | Aplica el gradiente a todos los dibujos de texturas hasta ClearActive. |
| `GRADIENT:ClearActive()  -> nil` | Elimina el mapa de gradiente global. |

```lua
local sepia

function onStart()
    sepia = GRADIENT:Create({
        { 0.0,  40,  20,   0 },
        { 1.0, 255, 230, 180 },
    }, 0.8)
end

function draw()
    GRADIENT:SetActive(sepia)
    background:Draw(0, 0)
    GRADIENT:ClearActive()
end

function onDestroy()
    sepia:Dispose()
end
```

### Handle de gradiente

Un mapa de gradiente devuelto por `GRADIENT:Create`.

| Método | Descripción |
| --- | --- |
| `gradient.BlendStrength  -> number` | Cantidad de mezcla, 0..1 (solo lectura). |
| `gradient:Dispose()  -> nil` | Libera el handle. La textura GPU compartida permanece en caché. |

### SIZE

Crea objetos de valor de anchura/altura.

| Método | Descripción |
| --- | --- |
| `SIZE:CreateSize(width, height)  -> size` | Crea un objeto de tamaño. |

### Handle de tamaño

Un par de anchura y altura devuelto por `SIZE:CreateSize`.

| Método | Descripción |
| --- | --- |
| `size.Width  -> int` | Anchura (lectura/escritura). |
| `size.Height  -> int` | Altura (lectura/escritura). |
