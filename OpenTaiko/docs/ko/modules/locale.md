<!-- modules/locale.md -->

# Locale

## Locale.i18n

모듈 자체의 번역으로, 스킨의 `Locales/`가 아니라 모듈 옆에 둡니다. 텍스트는 `lang/<code>/<name>.json`에 기능마다 파일 하나씩, 고정된 id를 키로 하여 둡니다. `lang/en/`에는 영어가 있고 다른 모든 언어의 폴백이 됩니다. 코드는 게임의 언어 id인 `en`, `de`, `es`, `fr`, `ja`, `ko`, `nl`, `ru`, `zh`입니다.

```
Global/Modules/Stages/my_stage/
  Script.lua
  lang/
    en/menu.json      { "title": "Settings", "count": "%d items", "tips": ["One", "Two"] }
    ja/menu.json      { "title": "設定", "count": "%d 件" }
```

| 함수 또는 필드 | 설명 |
| --- | --- |
| `I18N.texts(name, dir?)  -> texts` | `lang/<code>/<name>.json`의 텍스트. `dir`은 `lang/`이 들어 있는 폴더입니다. 스테이지나 액티비티에서는 생략하면(또는 `""`를 넘기면) 자신의 폴더가 되고, Lib 파일에서는 `Core.Here`의 폴더를 넘깁니다. 파일은 처음 쓸 때 읽어서 보관합니다. |
| `I18N.detect()` | 게임의 현재 언어를 읽어 모든 조회가 쓰는 언어로 정합니다. 들어올 때마다 실행되는 `activate`에서 호출하면, 설정에서 바꾼 언어가 다음 방문에 적용됩니다. |
| `I18N.load(code)` | 언어 `code`를 사용합니다(테스트 하네스나 언어 미리보기용). |
| `I18N.currentCode()  -> string` | 게임의 현재 언어 id. 엔진이 아무것도 주지 않으면 `"en"`. |
| `I18N.code` | 조회가 지금 쓰는 언어. |
| `I18N.localizedPath(path, code?)  -> string` | `path` 옆에 그 이름의 텍스처가 있으면 `<name>_<code>.<ext>`, 없으면 `path`: 글자가 박힌 그림용입니다. |

| 메서드 | 설명 |
| --- | --- |
| `texts:tr(id)  -> string` | `id`의 텍스트. 어느 언어에도 없으면 id 자체를 반환하므로 빠진 곳이 화면에 드러납니다. |
| `texts:trf(id, ...)  -> string` | `id`의 텍스트를 `string.format` 패턴으로 삼아 인자와 함께 쓴 결과. |
| `texts:get(...)  -> string` | 키 경로의 문자열(중첩 객체에는 `texts:get("menu", "title")`). 없거나 비어 있으면 nil. |
| `texts:list(...)  -> table` | 키 경로의 문자열 배열. 각 항목은 개별적으로 영어로 폴백합니다. |
| `texts:loc(...)  -> handle` | 호출 시점에 키 경로를 읽는 `GetString(fallback)`을 가진 핸들로, 로드할 때 캐시한 이름이 나중의 언어 변경을 따라갑니다. |
| `texts:node(...)  -> value` | 키 경로의 원본 JSON 노드(현재 언어, 없으면 영어). |

```lua
local I18N = require("Locale.i18n")
local T = I18N.texts("menu")
local title, count

function activate()
    I18N.detect()
    title = T:tr("title")
    count = T:trf("count", #items)
end
```

<div class="callout warn">
조회는 `I18N.code`를 따르며, 이 값은 `detect`나 `load`가 실행될 때만 바뀝니다. 플레이어가 언어를 바꾸는 동안에도 화면에 남아 있는 모듈은 전역 `reloadLanguage(lang)`을 정의하고 그 안에서 `detect`를 호출할 수 있습니다. Pagoda, Vault, Shop의 텍스트 모듈은 조회할 때마다 직접 언어를 확인합니다.
</div>

예전 모듈은 대신 영어 원문을 키로 하는 사전을 씁니다. `lang/<code>.lua`가 각 영어 문자열에서 번역으로 가는 테이블을 반환하고, `detect`가 현재 언어의 사전을 로드하며, 함수 `I18N.tr(s)`와 `I18N.trf(fmt, ...)`가 그 안에서 문자열을 찾습니다. 이 방식도 계속 동작하지만, 새 모듈은 `texts`를 써야 합니다.
