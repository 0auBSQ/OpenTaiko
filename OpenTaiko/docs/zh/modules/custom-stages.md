<!-- modules/custom-stages.md -->

# 自定义舞台

自定义舞台是任何人都能制作、分享和安装的舞台，并会出现在皮肤的自定义舞台菜单中。它用 [STAGES:SetNamespace](../api/activities.md) 声明命名空间 `CStage`，并通过共享字符串 `custom_stage_return` 回到打开它的舞台。`Stages.CustomStages` 负责这两件事，并为菜单列出自定义舞台。`Global/Modules/Stages/custom_stage_tutorial` 就是其中之一：一份在游戏内介绍如何制作自定义舞台的指南。

## 声明

在 Script.lua 顶部、任何函数之外：

```lua
-- Global/Modules/Stages/lantern_walk/Script.lua, at the top
local CS = require("Stages.CustomStages")

CS.declare{
    title       = CS.allLanguages("menu", "title"),       -- lang/<code>/menu.json, every language
    description = { en = "A calm walk down a street of paper lanterns." },
}
STAGES:SetResolution(1920, 1080)
```

旁边的 `STAGES:SetResolution(1920, 1080)` 告诉引擎舞台绘制所用的分辨率，这样不同分辨率的皮肤会缩放它，而不会把它裁掉。

`CS.declare(info)` 即 `STAGES:SetNamespace("CStage", info)`。菜单读取 info 的两个字段：

| 字段 | 显示 |
| --- | --- |
| `title` | 列表中的舞台名：字符串，或按语言代码组织的表。缺少时使用文件夹名。 |
| `description` | 选中该舞台时列表下方的一两行文字；可选。 |

按语言代码组织的表会给出游戏语言的文本，没有则给英文，再没有则给表中任意一种语言。其他字段由你自由使用：菜单会原样传递 info。

## 返回路径

打开自定义舞台的菜单会把共享字符串 `custom_stage_return` 设为要返回的舞台，即 `"<stage>"` 或 `"<stage>|<transition>"`，然后进入自定义舞台。自定义舞台在进入时读取并清空该字符串，离开时前往那个舞台，字符串为空时前往 `_title`：

```lua
local back, backTransition

function activate()
    back, backTransition = CS.takeReturn()   -- "_title" when the string is empty
end

function update(ts)
    if leaving then return Exit("stage", back, backTransition) end
end
```

每次进入都会读取该字符串。前往自己的另一个舞台再回来的自定义舞台，要在离开前重新设置该字符串，就像我的房间对 `my_room_return` 所做的那样。

## Stages.CustomStages

| 函数 | 说明 |
| --- | --- |
| `CS.declare(info)  -> boolean` | 将调用舞台声明为自定义舞台：`STAGES:SetNamespace("CStage", info)`。 |
| `CS.allLanguages(name, id)  -> table` | 从调用舞台的 `lang/<code>/<name>.json` 中，取出九种语言里每种拥有的字符串 `id`，组成按语言代码组织的表。专为 `declare` 设计，它在选定语言之前运行。 |
| `CS.localized(value, code?)  -> string or nil` | 以 `code`（默认为游戏语言）返回声明的文本：字符串原样返回，按语言代码组织的表如上所述。 |
| `CS.list(code?)  -> table` | 调用模块能到达的自定义舞台，按标题排序：`{ name, isGlobal, title, description, info }`，其中 `title` 和 `description` 已是对应语言。 |
| `CS.enter(name, back, transition?)` | 把 `custom_stage_return` 设为 `back` 并进入舞台 `name`；在舞台的 update 中写 `return CS.enter(...)`。 |
| `CS.takeReturn()  -> stage, transition` | 读取并清空 `custom_stage_return`：返回舞台（字符串为空时为 `_title`）和过场（默认过场为 nil）。 |
| `CS.open(o)`, `CS.update(ts)`, `CS.draw()`, `CS.isOpen()`, `CS.close()` | 以对话框形式覆盖在调用者画面上的列表，用皮肤的 UI 库（[UI.Pick](ui.md)）构建。`update` 在打开期间返回 nil，或返回 `"cancel"`，或返回所选的 `CS.list` 条目；两种情况下对话框都会关闭。`o`：`title`、`ui`（库表或其 require 名称）、`theme`、`sfx`、`navPlayer`（默认 0：键盘和手柄，不含太鼓）。未安装任何自定义舞台时，它会在确定按钮上方说明这一点。 |
| `CS.NAMESPACE`, `CS.RETURN_KEY` | `"CStage"` 和 `"custom_stage_return"`。 |

它的字符串位于自己的 `lang/` 文件夹中，共九种语言（[Locale.i18n](locale.md)）。Open-World Memories 从标题画面中“设置”上方的自定义舞台条目打开列表：

```lua
-- a menu stage: CS.open{ title = "Custom Stages" } when its entry is chosen, then
function update(ts)
    if CS.isOpen() then
        local r = CS.update(ts)
        if r ~= nil and r ~= "cancel" then return CS.enter(r.name, "_title") end
        return
    end
end

function draw()
    if CS.isOpen() then CS.draw() end
end
```

## 自定义舞台放在哪里

| 文件夹 | 运行于 |
| --- | --- |
| `Global/Modules/Stages/<name>/` | 所有皮肤。 |
| `System/<skin>/Modules/Stages/<name>/` | 仅该皮肤。 |

两处都有同名舞台时，皮肤中的菜单列出皮肤的舞台，全局菜单列出全局舞台，即 `Exit` 会打开的那个。游戏在启动时以及重新加载皮肤时创建舞台，所以在游戏运行中安装的舞台会在重启后出现。

## 分享舞台

自定义舞台就是它的文件夹。把文件夹本身压缩为 zip，使压缩包以 `<name>/Script.lua` 开头，并包含它加载的所有内容和素材的署名。玩家把它解压到游戏文件夹的 `Global/Modules/Stages/`，或某个皮肤的 `Modules/Stages/`，然后重启游戏。更新会添加和替换文件，但不会删除任何文件，所以请选择内置舞台没有使用的文件夹名。

## 教程

`custom_stage_tutorial` 在游戏中展示制作并分享自定义舞台需要知道的四件事：文件夹放在哪里（以及不能放在哪里）、`CStage` 命名空间、所用的一切都放在自己的文件夹中且外部只用内置模块，以及在哪里阅读本文档，即 OpenTaiko Hub 的“文档”标签。它的每一页都是 `Textures/` 文件夹中的图片和 `lang/` 文件夹中的文本，因此可以逐个文件替换。
