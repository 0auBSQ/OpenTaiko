<!-- modules/core.md -->

# Core

자체 화면이 없는 작은 헬퍼들입니다. 이징 곡선, 색 계산, 테이블 유틸리티, JSON 인코더와 디코더, 그리고 현재 파일의 폴더. 로드될 때 엔진 상태를 건드리지 않으므로 읽기 전용 모듈을 포함한 어떤 모듈이든 require할 수 있습니다.

## Core.Easing

0에서 1 사이의 비율에 대한 이징 곡선으로, 자신의 시계(타임라인, 파티클의 경과 시간)로 움직이는 애니메이션에 씁니다. 엔진 시계로 움직이는 트윈이라면 `COUNTER` 전역의 SetEasing을 쓰는 방법도 있습니다. `hump`를 제외한 모든 곡선은 입력을 0..1로 제한하고 0을 0으로, 1을 1로 대응시킵니다.

| 함수 | 설명 |
| --- | --- |
| `Easing.linear(t)  -> number` | 제한된 비율 그대로. |
| `Easing.inQuad(t)`, `outQuad`, `inOutQuad`, `inCubic`, `outCubic`, `inOutCubic`, `inSine`, `outSine`, `inOutSine`, `inExpo`, `outExpo`  `-> number` | 이름 그대로의 일반적인 곡선. |
| `Easing.inBack(t, s?)`, `Easing.outBack(t, s?)  -> number` | 목표를 넘었다 돌아오는 곡선. `s`는 넘어가는 양입니다(기본값 1.7). |
| `Easing.inBounce(t)`, `Easing.outBounce(t)`, `Easing.outElastic(t)  -> number` | 튀는 곡선과 탄성 곡선. |
| `Easing.hump(t)  -> number` | 0에서 중간에 1까지 올라갔다가 0으로 돌아옵니다. 찌그러짐과 번쩍임에 씁니다. |
| `Easing.span(t, t0, dur)  -> number` | `t0`에서 시작해 `dur` 동안 이어지는 구간의 진행도를 0..1로 제한한 값. |
| `Easing.lerp(a, b, k)  -> number` | `a + (b - a) * k`. |
| `Easing.clamp01(t)  -> number` | `t`를 0..1로 제한한 값. |
| `Easing.get(kind, fn)  -> function` | 엔진의 이징 이름으로 고르는 곡선. `kind`는 `"IN"`, `"OUT"`, `"INOUT"` 중 하나, `fn`은 `"LINEAR"`, `"SINE"`, `"QUAD"`, `"CUBIC"`, `"EXPO"`, `"BACK"`, `"BOUNCE"`, `"ELASTIC"` 중 하나입니다. 곡선이 없는 조합은 QUAD 곡선을 줍니다. |

```lua
local Easing = require("Core.Easing")
local k = Easing.outBack(Easing.span(t, startTime, 0.4))
x = Easing.lerp(x0, x1, k)
```

## Core.Color

색 변환과 조정입니다. 색상(hue)을 포함한 모든 채널은 0에서 1까지의 실수입니다(색상은 0에서 1로 색상환을 한 바퀴 돕니다). 함수는 `r, g, b`를 개별 값으로 받고 반환합니다.

| 함수 | 설명 |
| --- | --- |
| `Color.rgbToHsv(r, g, b)  -> h, s, v` | RGB에서 HSV로. |
| `Color.hsvToRgb(h, s, v)  -> r, g, b` | HSV에서 RGB로. |
| `Color.rgbToHsl(r, g, b)  -> h, s, l` | RGB에서 HSL로. |
| `Color.hslToRgb(h, s, l)  -> r, g, b` | HSL에서 RGB로. |
| `Color.mix(r, g, b, r2, g2, b2, t)  -> r, g, b` | 선형 혼합: `t` = 0이면 첫 번째 색 그대로, 1이면 두 번째 색이 됩니다. |
| `Color.lighten(r, g, b, amount)  -> r, g, b` | HSL 명도를 남은 범위의 `amount`만큼 흰색 쪽으로 옮깁니다(1 = 흰색). 색상과 채도는 그대로입니다. |
| `Color.darken(r, g, b, amount)  -> r, g, b` | HSL 명도를 `amount`만큼 검은색 쪽으로 옮깁니다(1 = 검은색). |
| `Color.saturate(r, g, b, factor, add?)  -> r, g, b` | HSV 채도에 `factor`를 곱하고 `add`를 더한 뒤 1로 제한합니다. |
| `Color.brighten(r, g, b, factor)  -> r, g, b` | HSV 명도에 `factor`를 곱하고 1로 제한합니다. |
| `Color.ramp(from, to, n)  -> table` | `{r, g, b}`에서 `{r, g, b}`까지 단계적으로 변하는 `n`개의 색을 `{r, g, b}`의 목록으로 반환합니다. |
| `Color.fromEngine(col)  -> r, g, b, a` | `COLOR` 객체의 채널을 실수로 반환합니다. |
| `Color.toEngine(r, g, b, a?)  -> color` | 실수로 만든 `COLOR` 객체(알파 기본값 1). |
| `Color.rampEngine(fromCol, toCol, n)  -> table` | 두 `COLOR` 객체 사이를 잇는 `n`개의 `COLOR` 객체로 된 램프. |
| `Color.clamp01(v)  -> number` | `v`를 0..1로 제한한 값. |

## Core.Util

| 함수 | 설명 |
| --- | --- |
| `Util.clamp(v, lo, hi)  -> number` | `v`를 `lo`와 `hi` 사이로 제한한 값. |
| `Util.lerp(a, b, t)  -> number` | 선형 보간. |
| `Util.round(v)  -> number` | 가장 가까운 정수로 반올림합니다(0.5는 올림). |
| `Util.deepcopy(o)  -> value` | Lua 테이블의 깊은 복사본. 메타테이블과 순환 참조도 포함합니다. |
| `Util.merge(base, over)  -> table` | `over`를 병합한 `base`의 복사본. 하위 테이블은 재귀적으로 병합되고 다른 값은 교체됩니다. |
| `Util.cloneTable(dict)  -> table` | .NET 딕셔너리(파싱된 JSON 객체, SQL 행 집합)를 재귀적으로 Lua 테이블에 복사한 것. |

## Core.Json

Lua 값에서 JSON 텍스트로, 또 그 반대로 순수 Lua로 변환합니다. 파일을 .NET 딕셔너리로 파싱하는 `JSONLOADER`와 달리 Lua 테이블을 다루므로, 세이브 문자열(`sf:SetGlobalString`)이나 데이터베이스 값에 보관하는 데이터에 알맞습니다.

| 함수 | 설명 |
| --- | --- |
| `Json.encode(value)  -> string` | 키가 1..n인 테이블은 배열로, 그 밖의 테이블은 키를 정렬한 객체로 기록하므로 같은 값은 항상 같은 텍스트가 됩니다. 함수와 userdata는 객체에서 빠지고, 정수는 소수점 없이 기록됩니다. 자기 자신을 담은 테이블은 오류를 일으킵니다. |
| `Json.decode(text)  -> value` 또는 `nil, message` | JSON 텍스트를 파싱합니다. JSON `null`은 `Json.null`이 되며, 구문 오류가 있으면 nil과 위치가 담긴 메시지를 반환합니다. |
| `Json.null` | JSON `null`을 나타내는 값. 인코딩하면 다시 `null`이 됩니다. |

```lua
local Json = require("Core.Json")
local sf = GetSaveFile(0)
local data = Json.decode(sf:GetGlobalString("my_feature")) or { v = 1, items = {} }
data.items[#data.items + 1] = "lamp"
sf:SetGlobalString("my_feature", Json.encode(data))
```

## Core.Here

자신을 호출한 파일의 폴더를, `require`가 그 파일에 넘기는 인자에서 구해 반환하는 함수입니다. Lib 파일은 이것으로 자신의 에셋을 찾습니다([에셋은 호출한 모듈 기준으로 해석된다](README.md) 참고).

| 함수 | 설명 |
| --- | --- |
| `require("Core.Here")(...)  -> string` | 파일의 절대 폴더. 슬래시로 구분하고 끝에 `/`가 붙습니다. 파일이 `require`로 로드되지 않았으면 nil. `...`가 모듈 이름과 파일 경로를 담고 있는 파일의 최상위에서 호출하십시오. |
