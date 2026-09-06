<!-- api/graphics.md -->

# 그래픽과 텍스트

텍스처, 캔버스, 클리핑, 텍스트, 비디오, 색상, 그라디언트 맵, 크기. 모든 위치와 크기는 스크립트가 그리는 좌표 공간인 논리 화면 픽셀 단위입니다(`INPUT:GetSurfaceWidth()` / `GetSurfaceHeight()` 참고).

텍스처, 캔버스, 텍스트, 비디오, 그라디언트 핸들은 생성한 스크립트가 소유하며, 게임은 그 스크립트를 언로드할 때 핸들을 해제합니다. 핸들을 더 일찍 해제하려면 직접 `Dispose()`를 호출하십시오.

## 텍스처

### TEXTURE

이미지 파일을 텍스처 핸들로 로드합니다.

<div class="callout warn">
상대 경로는 스크립트 디렉터리를 기준으로 해석됩니다. FromAbsolutePath 변형은 전체 경로를 받습니다. 없는 파일은 아무것도 그리지 않는 빈 핸들을 반환합니다. CreateTexture는 비동기로 로드합니다. 백그라운드 디코드와 업로드가 끝날 때까지 핸들은 아무것도 그리지 않고 Width와 Height로 0을 보고합니다. 크기나 픽셀이 즉시 필요하면 CreateTextureSync를 사용하십시오. options 테이블은 { maxSize = N }을 받아 디코드 시 이미지를 축소해 긴 변이 최대 N픽셀이 되게 합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `TEXTURE:CreateTexture()  -> texture` | 이미지가 없는 빈 핸들을 만듭니다. |
| `TEXTURE:CreateTexture(path)  -> texture` | 스크립트 디렉터리 기준 상대 경로에서 이미지를 비동기로 로드합니다. |
| `TEXTURE:CreateTexture(path, options)  -> texture` | 같지만 options 테이블(`{ maxSize = N }`)을 받습니다. |
| `TEXTURE:CreateTextureSync(path)  -> texture` | 이미지를 동기로 로드합니다. 반환 시 크기와 픽셀을 사용할 수 있습니다. |
| `TEXTURE:CreateTextureFromAbsolutePath(path)  -> texture` | 전체 파일 시스템 경로에서 이미지를 로드합니다. |
| `TEXTURE:CreateTextureFromAbsolutePath(path, options)  -> texture` | 같지만 options 테이블(`{ maxSize = N }`)을 받습니다. |
| `TEXTURE:Exists(path)  -> bool` | 스크립트 디렉터리 기준 상대 경로에 파일이 있는지 반환합니다. |

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

### 텍스처 핸들

`TEXTURE` 팩토리, `text:GetText` / `GetVerticalText`, `video.Texture`가 반환하는 그릴 수 있는 2D 이미지입니다.

<div class="callout warn">
앵커 이름: topleft, top, topright, left, center, right, bottomleft, bottom, bottomright(대소문자 구분 없음. 알 수 없는 이름은 topleft로 대체). 앵커 기준 그리기는 현재 스케일을 반영합니다. 블렌드 모드 이름: Normal, Add, Multi, Sub, Screen. 래핑 모드 이름: Edge, Border, Repeat, Mirror. 새 핸들의 기본값은 Repeat입니다. 빈 핸들에서는 모든 메서드가 no-op이며 getter는 아래에 나열된 기본값을 반환합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `texture:Draw(x, y)  -> nil` | 왼쪽 위를 (x, y)에 두고 텍스처 전체를 그립니다. |
| `texture:DrawRect(x, y, rect_x, rect_y, rect_width, rect_height)  -> nil` | 원본의 부분 사각형(텍스처 픽셀 단위)을 왼쪽 위가 (x, y)에 오도록 그립니다. |
| `texture:DrawAtAnchor(x, y, anchor)  -> nil` | 지정한 앵커 지점이 (x, y)에 오도록 텍스처 전체를 그립니다. |
| `texture:DrawRectAtAnchor(x, y, rect_x, rect_y, rect_width, rect_height, anchor)  -> nil` | 지정한 앵커 지점이 (x, y)에 오도록 원본의 부분 사각형을 그립니다. |
| `texture.Loaded  -> bool` | 핸들이 텍스처를 감싸고 있으면 true. 게임은 파일을 찾는 즉시, 비동기 로드가 끝나기 전에 이를 설정합니다. |
| `texture.Width  -> int` | 픽셀 너비. 빈 핸들이면 -1, 비동기 로드 대기 중이면 0. |
| `texture.Height  -> int` | 픽셀 높이. 빈 핸들이면 -1, 비동기 로드 대기 중이면 0. |
| `texture.Pointer  -> int` | 네이티브 GL 텍스처 id. 없으면 0. |
| `texture:GetScale()  -> vector2` | 현재 그리기 스케일을 vector2(`X`, `Y`)로 반환합니다. |
| `texture:GetOpacity()  -> number` | 현재 불투명도, 0..1. 빈 핸들이면 -1. |
| `texture:GetColor()  -> number, number, number` | 현재 틴트를 0..1 값 세 개(빨강, 초록, 파랑)로 반환합니다. |
| `texture:GetRotation()  -> number` | 현재 회전(도). |
| `texture:GetBlendMode()  -> string` | 현재 블렌드 모드 이름. |
| `texture:GetWrapMode()  -> string` | 현재 래핑 모드 이름. |
| `texture:SetScale(scale_x, scale_y)  -> nil` | 가로와 세로 그리기 스케일을 설정합니다(1 = 원본 크기). |
| `texture:SetOpacity(opacity)  -> nil` | 불투명도를 설정합니다, 0..1. |
| `texture:SetColor(color)  -> nil` | `COLOR` 값으로 틴트를 설정합니다. 핸들은 색상의 알파를 무시합니다. 투명도는 SetOpacity로 설정하십시오. |
| `texture:SetColor(red, green, blue)  -> nil` | 0..1 값 세 개로 틴트를 설정합니다. |
| `texture:SetRotation(angle)  -> nil` | 텍스처 중심을 기준으로 한 회전을 도 단위로 설정합니다. |
| `texture:SetBlendMode(mode)  -> nil` | 이름으로 블렌드 모드를 설정합니다(대소문자 구분 없음). 알 수 없는 이름은 핸들이 무시합니다. |
| `texture:SetWrapMode(mode)  -> nil` | 이름으로 래핑 모드를 설정합니다(대소문자 구분 없음). 알 수 없는 이름은 핸들이 무시합니다. |
| `texture:SetUseNoiseEffect(enabled)  -> nil` | 켜면 매 그리기마다 텍스처의 색을 애니메이션되는 무작위 회색조 노이즈로 대체하고 알파는 유지합니다. |
| `texture:SetGradientMap(gradient)  -> nil` | 이 텍스처의 모든 그리기에 `GRADIENT` 맵을 적용합니다(GRADIENT 참고). |
| `texture:ClearGradientMap()  -> nil` | 텍스처별 그라디언트 맵을 제거합니다. |
| `texture:Dispose()  -> nil` | 텍스처를 해제합니다. |

## 캔버스와 클리핑

### CANVAS

Lua가 CPU에서 편집하고 하나의 텍스처로 업로드하는 쓰기 가능한 픽셀 표면을 만듭니다.

<div class="callout warn">
캔버스는 Lua에서 픽셀을 설정한 뒤(SetPixel, FillRect, ...) Upload로 GPU에 밀어 넣는 텍스처입니다. 스크립트가 한 번 칠하고 업로드한 뒤 그 결과를 매 프레임 그리는 소프트웨어 렌더링과 UI 도형 베이킹에 알맞습니다. 새 캔버스는 완전히 투명한 상태로 시작합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `CANVAS:CreateCanvas(width, height)  -> canvas` | 지정한 크기(최소 1x1)의 투명한 캔버스를 만듭니다. |

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

### 캔버스 핸들

`CANVAS:CreateCanvas`가 반환하는, 텍스처처럼 그릴 수 있는 쓰기 가능한 RGBA 픽셀 버퍼입니다.

<div class="callout warn">
색상 인자 r, g, b, a는 0-255 정수입니다. 픽셀 편집은 더티 사각형에 누적되며 Upload를 호출할 때만 GPU에 도달합니다(BlitPacked는 스스로 업로드합니다). 그리기 좌표는 정수입니다. 앵커 이름은 텍스처 핸들과 같습니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `canvas.Width  -> int` | 픽셀 너비. |
| `canvas.Height  -> int` | 픽셀 높이. |
| `canvas.Pointer  -> int` | 네이티브 GL 텍스처 id. 없으면 0. |
| `canvas:SetPixel(x, y, r, g, b, a)  -> nil` | 픽셀 하나를 설정합니다. 범위 밖 좌표는 캔버스가 무시합니다. |
| `canvas:FillRect(x, y, w, h, r, g, b, a)  -> nil` | 축에 정렬된 사각형을 캔버스에 맞게 잘라 채웁니다. |
| `canvas:FillCircle(cx, cy, radius, r, g, b, a)  -> nil` | 지정한 픽셀 반지름의 원을 채웁니다. |
| `canvas:StrokeLine(x0, y0, x1, y1, radius, r, g, b, a)  -> nil` | 지정한 반지름의 원을 겹쳐 두꺼운 선을 그립니다. |
| `canvas:PasteTexture(texture, x, y)  -> nil` | 텍스처를 왼쪽 위가 (x, y)에 오도록 캔버스에 알파 블렌딩합니다. 캔버스는 텍스처의 픽셀을 텍스처 핸들마다 한 번 GPU에서 읽어 오며, 이는 느린 작업이므로 설정 코드에 두어야 합니다. |
| `canvas:PasteTextureTransformed(texture, x, y, scale, rotationDeg, anchor)  -> nil` | 최근접 샘플링을 사용해 `scale`로 스케일하고 `rotationDeg`만큼 시계 방향으로 회전한 텍스처를 알파 블렌딩합니다. anchor가 "center"이면 텍스처 중심이 (x, y)에 오고, 다른 값이면 왼쪽 위가 거기에 옵니다. |
| `canvas:Clear(r, g, b, a)  -> nil` | 캔버스 전체를 한 색으로 채웁니다. |
| `canvas:ClearTransparent()  -> nil` | 캔버스 전체를 완전 투명으로 재설정합니다. |
| `canvas:CopyFrom(other)  -> nil` | 같은 크기의 다른 캔버스 픽셀을 이 캔버스로 복사합니다. 크기가 다르면 이 호출은 아무것도 하지 않습니다. |
| `canvas:BlitPacked(data, count)  -> nil` | 행 우선 순서의 패킹된 `0xRRGGBB` 정수로 이루어진 1부터 시작하는 Lua 배열로 표면을 채우고(픽셀은 불투명이 되며, 음수 값은 투명 픽셀이 됨) 업로드합니다. |
| `canvas:Upload()  -> nil` | 대기 중인 편집(변경된 영역만)을 GPU에 밀어 넣습니다. 바뀐 것이 없으면 no-op입니다. |
| `canvas:Draw(x, y)  -> nil` | 왼쪽 위를 (x, y)에 두고 캔버스를 그립니다. |
| `canvas:DrawRect(x, y, rect_x, rect_y, rect_width, rect_height)  -> nil` | 원본의 부분 사각형(캔버스 픽셀 단위)을 왼쪽 위가 (x, y)에 오도록 그립니다. |
| `canvas:DrawAtAnchor(x, y, anchor)  -> nil` | 지정한 앵커 지점이 (x, y)에 오도록 캔버스를 그립니다. |
| `canvas:SetScale(scale_x, scale_y)  -> nil` | 가로와 세로 그리기 스케일을 설정합니다. |
| `canvas:SetOpacity(opacity)  -> nil` | 불투명도를 설정합니다, 0..1. |
| `canvas:SetColor(red, green, blue)  -> nil` | 0..1 값 세 개로 틴트를 설정합니다. |
| `canvas:Dispose()  -> nil` | 캔버스의 GPU 텍스처와 CPU 버퍼를 해제합니다. |

### GRAPHICS

스크롤 패널을 위한 시저(scissor) 클리핑입니다.

<div class="callout warn">
SetClip은 논리 화면 좌표를 받아 현재 뷰포트에 대응시키므로, 렌더 스케일링과 레터박스 아래에서도 클리핑이 동일합니다. SetClip과 ClearClip 사이에 사각형 밖에 그려지는 모든 픽셀은 GPU가 버립니다. SetClip은 이전 사각형을 대체합니다(스택이 없음). SetClip과 ClearClip을 짝지어 두십시오. 게임은 매 프레임 시작 시 시저를 비활성화합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `GRAPHICS:SetClip(x, y, w, h)  -> nil` | 지정한 사각형으로 클리핑을 켭니다. |
| `GRAPHICS:ClearClip()  -> nil` | 클리핑을 끕니다. |

```lua
GRAPHICS:SetClip(100, 200, 600, 400)
for i, row in ipairs(rows) do
    row.tex:Draw(100, 200 + (i - 1) * 48 - scrollY)
end
GRAPHICS:ClearClip()
```

## 텍스트

### TEXT

스킨의 주 폰트를 위한 폰트 렌더러를 만듭니다.

<div class="callout warn">
Create는 문자열 전체를 캐시된 텍스처로 렌더링하는 텍스트 핸들을 반환합니다. 거의 바뀌지 않는 라벨에 사용하십시오. CreateGlyphCached는 문자마다 텍스처 하나를 캐시하고 그릴 때 문자열을 조합하는 글리프 텍스트 핸들을 반환합니다. 자주 바뀌는 텍스트(타이머, 스코어, 입력 중인 텍스트)와 자동 줄바꿈 블록에 사용하십시오. 스타일 인자는 bold, italic, underline, strikeout 중 아무것이나입니다(대소문자 구분 없음. 다른 값은 regular를 뜻함). 한 스크립트 안에서 같은 팩토리 메서드를 처음에는 스타일 인자 없이, 나중에는 스타일 인자 정확히 하나로 호출하면 번들된 NLua 버전이 실패합니다. 스타일을 전혀 넘기지 않거나, 첫 호출부터 항상 하나를 넘기거나, 스타일 토큰 두 개(예: "bold", "regular")를 넘기십시오.
</div>

| 메서드 | 설명 |
| --- | --- |
| `TEXT:Create(size, ...style)  -> text` | 지정한 픽셀 크기의 문자열 전체 텍스트 렌더러를 만듭니다. |
| `TEXT:CreateGlyphCached(size, ...style)  -> glyphText` | 지정한 픽셀 크기의 글리프 조합 텍스트 렌더러를 만듭니다. |

#### 인라인 색상 태그

두 렌더러 모두 문자열 안의 이 태그를 인식합니다. 태그는 중첩되며, 닫히지 않은 태그는 문자열 끝까지 이어집니다. 측정 함수는 태그를 무시합니다.

| 태그 | 효과 |
| --- | --- |
| `<c.#rrggbb>` ... `</c>` | 채우기 색. |
| `<c.#rrggbb.#rrggbb>` ... `</c>` | 채우기 색과 외곽선 색. |
| `<g.#rrggbb.#rrggbb>` ... `</g>` | 세로 그라디언트 채우기(위 색, 아래 색). |

### 텍스트 핸들

`TEXT:Create`가 반환하는 문자열 전체 렌더러입니다.

<div class="callout warn">
GetText와 GetVerticalText는 문자열, 채우기 색, 외곽선 색, 크기 제한의 고유한 조합마다 텍스처 하나를 캐시하고 핸들을 해제할 때까지 유지합니다. 따라서 매 프레임 바뀌는 문자열은 매 프레임 텍스처를 추가해 GPU 메모리를 누수시킵니다. 그런 텍스트는 DrawDirect나 글리프 텍스트 핸들로 그리십시오. forecolor와 backcolor는 COLOR 값이며 기본값은 흰 채우기와 불투명한 검은 외곽선입니다. centered 플래그는 여러 줄 문자열의 각 줄을 가운데 정렬합니다. 캐시 키에서 제외되므로 주어진 문자열에 대한 첫 호출이 이를 결정합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `text:GetText(text, centered, max_width, forecolor, backcolor)  -> texture` | 가로 문자열을 캐시된 텍스처로 렌더링합니다. `text` 뒤의 모든 인자는 선택 사항입니다. 렌더링된 텍스처가 `max_width`보다 넓으면 맞도록 가로로 눌러서 그려집니다. |
| `text:GetVerticalText(text, centered, max_height, forecolor, backcolor)  -> texture` | 세로로 쌓은 문자열을 캐시된 텍스처로 렌더링합니다. 렌더링된 텍스처가 `max_height`보다 높으면 맞도록 세로로 눌러서 그려집니다. |
| `text:DrawDirect(text, x, y, r, g, b, opacity, spacing)  -> number` | 문자별 캐시에서 글리프 단위로 문자열을 그리고 마지막 글리프 다음의 x 위치를 반환합니다. `r`, `g`, `b`는 0-255(기본값 255), `opacity`는 0..1(기본값 1), `spacing`은 글리프 사이 간격 픽셀(기본값 -6)입니다. |
| `text:Dispose()  -> nil` | 폰트와 캐시된 모든 텍스처를 해제합니다. |

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

### 글리프 텍스트 핸들

`TEXT:CreateGlyphCached`가 반환하는 글리프 조합 렌더러입니다.

<div class="callout warn">
forecolor와 backcolor는 COLOR 값이며 기본값은 흰 채우기와 검은 외곽선입니다. 위치 뒤의 모든 인자는 선택 사항입니다. maxWidth(0 = 제한 없음)는 줄 전체가 맞도록 각 글리프를 가로로 누릅니다. anchor는 텍스처 핸들과 같은 아홉 가지 이름을 사용합니다. '\n'은 새 줄을 시작하며, Draw는 줄을 왼쪽 정렬로 쌓습니다. Draw는 그려진 상자의 오른쪽 가장자리 x 좌표를 반환합니다. 상자 기하는 GetText 텍스처와 일치하므로(잉크의 양옆과 아래에 25px 여백) 같은 지점에 그린 글리프 텍스트와 GetText 텍스처가 정렬됩니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `glyphText.LineHeight  -> number` | 여러 줄 텍스트의 줄 간격(픽셀). |
| `glyphText.BoxHeight  -> number` | 한 줄 상자의 높이(잉크와 여백). 한 줄 GetText 텍스처와 일치합니다. |
| `glyphText:Measure(text, scale)  -> number` | 가장 넓은 줄의 잉크 너비에 `scale`(기본값 1)을 곱한 값. |
| `glyphText:Draw(text, x, y, forecolor, backcolor, opacity, scale, maxWidth, anchor, scaleY, rotationDeg)  -> number` | 텍스트를 그립니다. `opacity` 0..1(기본값 1); `scale`(기본값 1); `scaleY`가 0보다 크면 세로 스케일을 덮어씀; `rotationDeg`는 블록 전체를 (x, y)를 기준으로 회전. 상자의 오른쪽 가장자리 x를 반환합니다. |
| `glyphText:SetClipY(y0, y1)  -> nil` | 이후의 회전 없는 Draw 호출을 화면 공간의 세로 띠 y0..y1로 제한합니다. Draw는 띠 밖의 글리프를 건너뛰고 가장자리의 글리프를 잘라 그립니다. y1 <= y0을 넘기면 해제됩니다. 회전된 그리기는 띠를 무시합니다. |
| `glyphText:WrapToLines(text, wrapWidth, scale)  -> string[]` | `wrapWidth`에서 단어 단위로 줄바꿈하고 줄을 일반 문자열로 반환합니다(태그 제거됨). |
| `glyphText:MeasureWrapped(text, wrapWidth, scale, lineSpacing)  -> number` | 그리지 않고 DrawWrapped가 사용할 높이를 구합니다. `lineSpacing`은 줄 간격에 곱해집니다(기본값 1). |
| `glyphText:DrawWrapped(text, x, y, wrapWidth, forecolor, backcolor, opacity, scale, lineSpacing)  -> number` | `wrapWidth`에서 단어 단위로 줄바꿈하고 (x, y)에서 왼쪽 정렬로 그립니다. 그려진 높이를 반환합니다. |
| `glyphText:Dispose()  -> nil` | 폰트와 캐시된 모든 글리프를 해제합니다. |

자동 줄바꿈은 공백에서, CJK 문자 사이에서, 그리고 줄바꿈 너비보다 넓은 단어 안에서 끊습니다.

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

## 비디오

### VIDEO

비디오 파일을 재생 가능한 디코더 핸들로 로드합니다.

<div class="callout warn">
경로는 스크립트 디렉터리 기준 상대 경로입니다. 디코더는 백그라운드 스레드에서 열립니다. 준비될 때까지 핸들은 아무것도 그리지 않고 Start, 탐색, 속도 호출을 큐에 넣어 두었다가 디코더가 연결되면 적용합니다. 없는 파일은 빈 핸들을 반환합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `VIDEO:CreateVideo(path)  -> video` | 비디오 파일을 열고 핸들을 반환합니다. |

### 비디오 핸들

`VIDEO:CreateVideo`가 반환하는 비디오 디코더입니다.

<div class="callout warn">
위치는 밀리초, Duration은 초 단위입니다. 현재 프레임을 텍스처 핸들로 얻으려면 매 프레임 Texture를 읽으십시오. 그 핸들은 비디오 자체의 프레임 텍스처를 감싸므로 그리기만 하고 해제는 비디오 핸들에 맡기십시오. SetSpeed는 0 이하의 값을 무시합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `video:Start()  -> nil` | 재생을 시작합니다. |
| `video:Resume()  -> nil` | Pause 뒤 재생을 재개합니다. |
| `video:Pause()  -> nil` | 재생을 일시정지합니다. |
| `video:Stop()  -> nil` | 재생을 멈춥니다. |
| `video:Reset()  -> nil` | 처음으로 되돌립니다. |
| `video.Width  -> int` | 프레임 픽셀 너비. 디코더가 준비될 때까지 -1. |
| `video.Height  -> int` | 프레임 픽셀 높이. 디코더가 준비될 때까지 -1. |
| `video.Duration  -> number` | 전체 길이(초). 디코더가 준비될 때까지 1. |
| `video.DurationMs  -> number` | 전체 길이(밀리초). |
| `video.Texture  -> texture` | 현재 디코드된 프레임. |
| `video:IsFinished()  -> bool` | 스트림이 끝나고 남은 프레임이 없으면 true. |
| `video:GetTimestampMs()  -> number` | 현재 재생 위치(밀리초). |
| `video:SetTimestampMs(ms)  -> nil` | 밀리초 단위 위치로 탐색합니다. |
| `video:GetSpeed()  -> number` | 현재 재생 속도 배율(1 = 보통). |
| `video:SetSpeed(speed)  -> nil` | 재생 속도 배율을 설정합니다. |
| `video:GetPlayPosition()  -> number` | 현재 위치(초)(GetTimestampMs / 1000의 옛 이름). |
| `video:SetPlayPosition(seconds)  -> nil` | 초 단위 위치로 탐색합니다(SetTimestampMs의 옛 이름). |
| `video:GetPlaySpeed()  -> number` | GetSpeed의 옛 이름. |
| `video:SetPlaySpeed(speed)  -> nil` | SetSpeed의 옛 이름. |
| `video:Dispose()  -> nil` | 디코더와 프레임 텍스처를 해제합니다. |

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

## 색상, 그라디언트, 크기

### COLOR

색상 값을 만듭니다.

<div class="callout warn">
채널은 0-255입니다. 색상을 받는 텍스처, 캔버스, 텍스트 메서드가 이 값을 받습니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `COLOR:CreateColorFromRGBA(r, g, b, a)  -> color` | 빨강, 초록, 파랑과 선택적 알파(기본값 255)로 색상을 만듭니다. |
| `COLOR:CreateColorFromARGB(a, r, g, b)  -> color` | 알파, 빨강, 초록, 파랑으로 색상을 만듭니다. |
| `COLOR:CreateColorFromHex(value)  -> color` | 접두어 없는 `AARRGGBB` 16진수 문자열(예: `"ff2080ff"`)을 파싱합니다. 여섯 자리 문자열은 알파 0이 됩니다. 파싱할 수 없는 문자열은 불투명한 흰색이 됩니다. |

### 색상 핸들

`COLOR` 팩토리가 반환하는 색상 값입니다.

| 메서드 | 설명 |
| --- | --- |
| `color.R  -> int` | 빨강 채널, 0-255(읽기/쓰기). |
| `color.G  -> int` | 초록 채널, 0-255(읽기/쓰기). |
| `color.B  -> int` | 파랑 채널, 0-255(읽기/쓰기). |
| `color.A  -> int` | 알파 채널, 0-255(읽기/쓰기). |

### GRADIENT

그라디언트 맵은 각 픽셀의 휘도를 색상 램프에 대응시켜 텍스처 그리기를 다시 색칠합니다. 텍스처 하나에 또는 모든 그리기에 적용할 수 있습니다.

<div class="callout warn">
Create는 최소 두 개의 정지점(stop) 테이블과 블렌드 양을 받습니다. 각 정지점은 { position 0-1, r 0-255, g 0-255, b 0-255 [, a 0-255] } 배열이고, 블렌드 양은 0 = 효과 없음, 1 = 완전 대체, 그 사이 값은 혼합입니다(기본값 1). 정지점이 두 개 미만이면 오류가 발생합니다. 동일한 정지점과 블렌드는 프로그램 전체에서 캐시된 GPU 텍스처 하나를 재사용합니다. texture:SetGradientMap으로 텍스처 하나에, SetActive로 모든 텍스처 그리기에 적용합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `GRADIENT:Create(stops, blend)  -> gradient` | 색상 정지점과 블렌드 양으로 그라디언트 맵을 만듭니다(또는 재사용합니다). |
| `GRADIENT:SetActive(gradient)  -> nil` | ClearActive까지 모든 텍스처 그리기에 그라디언트를 적용합니다. |
| `GRADIENT:ClearActive()  -> nil` | 전역 그라디언트 맵을 제거합니다. |

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

### 그라디언트 핸들

`GRADIENT:Create`가 반환하는 그라디언트 맵입니다.

| 메서드 | 설명 |
| --- | --- |
| `gradient.BlendStrength  -> number` | 블렌드 양, 0..1(읽기 전용). |
| `gradient:Dispose()  -> nil` | 핸들을 놓습니다. 공유되는 GPU 텍스처는 캐시에 남습니다. |

### SIZE

너비/높이 값 객체를 만듭니다.

| 메서드 | 설명 |
| --- | --- |
| `SIZE:CreateSize(width, height)  -> size` | 크기 객체를 만듭니다. |

### 크기 핸들

`SIZE:CreateSize`가 반환하는 너비와 높이 쌍입니다.

| 메서드 | 설명 |
| --- | --- |
| `size.Width  -> int` | 너비(읽기/쓰기). |
| `size.Height  -> int` | 높이(읽기/쓰기). |
