<!-- modules/locale.md -->

# Locale

## Locale.i18n

模块自己的翻译，放在模块旁边，而不是皮肤的 `Locales/` 中。文本放在 `lang/<code>/<name>.json` 中，每个功能一个文件，以稳定的 id 为键；`lang/en/` 存放英文，并作为所有其他语言的回退。代码是游戏的语言 id：`en`、`de`、`es`、`fr`、`ja`、`ko`、`nl`、`ru`、`zh`。

```
Global/Modules/Stages/my_stage/
  Script.lua
  lang/
    en/menu.json      { "title": "Settings", "count": "%d items", "tips": ["One", "Two"] }
    ja/menu.json      { "title": "設定", "count": "%d 件" }
```

| 函数或字段 | 说明 |
| --- | --- |
| `I18N.texts(name, dir?)  -> texts` | `lang/<code>/<name>.json` 的文本。`dir` 是包含 `lang/` 的文件夹：在舞台或活动中省略它（或传入 `""`）表示其自己的文件夹，在 Lib 文件中则传入 `Core.Here` 给出的文件夹。文件在首次使用时读取并保留。 |
| `I18N.detect()` | 读取游戏的当前语言，并把它设为所有查找使用的语言。请在每次进入都会运行的 `activate` 中调用它，这样在设置中更改的语言会在下次进入时生效。 |
| `I18N.load(code)` | 使用语言 `code`（用于测试工具或语言预览）。 |
| `I18N.currentCode()  -> string` | 游戏当前的语言 id；引擎没有给出时为 `"en"`。 |
| `I18N.code` | 查找当前使用的语言。 |
| `I18N.localizedPath(path, code?)  -> string` | 当 `path` 旁边存在该名称的纹理时返回 `<name>_<code>.<ext>`，否则返回 `path`：用于烘焙了文字的图片。 |

| 方法 | 说明 |
| --- | --- |
| `texts:tr(id)  -> string` | `id` 的文本；没有任何语言包含它时返回 id 本身，使缺失之处在屏幕上一目了然。 |
| `texts:trf(id, ...)  -> string` | 把 `id` 的文本用作 `string.format` 模式并代入参数的结果。 |
| `texts:get(...)  -> string` | 某个键路径上的字符串（嵌套对象用 `texts:get("menu", "title")`），缺失或为空时为 nil。 |
| `texts:list(...)  -> table` | 某个键路径上的字符串数组；每一项各自回退到英文。 |
| `texts:loc(...)  -> handle` | 一个句柄，其 `GetString(fallback)` 在调用时读取该键路径，因此加载时缓存的名称能跟随之后的语言更改。 |
| `texts:node(...)  -> value` | 某个键路径上的原始 JSON 节点（当前语言，否则英文）。 |

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
查找遵循 `I18N.code`，它只在 `detect` 或 `load` 运行时改变。在玩家更改语言时仍停留在屏幕上的模块，可以定义全局函数 `reloadLanguage(lang)` 并在其中调用 `detect`。Pagoda、Vault 和 Shop 的文本模块在每次查找时都会自行检查语言。
</div>

较旧的模块改用以英文原文为键的字典：`lang/<code>.lua` 返回一张从每个英文字符串映射到其译文的表，`detect` 加载当前语言的那一张，函数 `I18N.tr(s)` 和 `I18N.trf(fmt, ...)` 在其中查找字符串。这种形式仍然可用；新模块应使用 `texts`。
