<!-- guides/characters.md -->

# 添加角色

角色是游戏安装目录下 `Global/Characters/` 中的一个文件夹。游戏在那里找到的每个子文件夹都会成为一个可选择的角色。文件夹中包含一个 `Metadata.json`（名称、稀有度、作者）、一个 `CharaConfig.txt`（位置和动画时序）、动画内容，以及可选的 `Effects.json`、`Unlock.json`、`Palettes.json` 和语音片段。动画内容要么是按编号命名的 PNG 帧文件夹，由游戏内置的角色脚本渲染，要么是每角色 `Script.lua` 自行选择绘制的任何内容（自带的 3D 模板绘制一个 glTF 模型）。

兼容性：OpenTaiko 0.6.1 仍能无需修改地加载为 0.6.0 制作的角色。本页描述的是当前结构，新角色请使用它。

## 开始之前

- 已安装 OpenTaiko 0.6.1。游戏从游戏可执行文件旁的 `Global/Characters/` 读取角色，所有皮肤共享它们。
- 一个用于编辑 JSON 和 INI 风格文件的文本编辑器。
- 2D 角色：导出为带透明背景、按编号命名的 PNG 帧（`0.png`、`1.png`、...），每个动画状态一个文件夹。
- 3D 角色：一个包含动画片段的 `model.glb`（二进制 glTF），以及一张静态的 `Render.png`。
- 自带的 `01 - Template`（2D）和 `01 - Template3D` 文件夹。复制其中之一作为起点。

## 第 1 步：了解发现、顺序与身份

启动时，游戏列出 `Global/Characters/` 的子文件夹，并按文件系统返回的顺序为每个文件夹创建一个角色。游戏不会对列表排序，因此自带的文件夹带有数字前缀（`00 - None`、`01 - Template`、`02 - Student (A)`、...）以保持顺序可预测。请让 `00 - None` 保持在第一位：索引 0 是空槽位，也是保存的角色缺失时的回退项。

存档按文件夹名称（`characterName`）存储所选角色，并在每次启动时重新解析为索引。添加或移除其他文件夹永远不会破坏已保存的选择，但重命名文件夹会使引用它的存档回退到 `00 - None`。两个角色可以共用一个显示名称；文件夹名称必须唯一。

游戏在启动时枚举角色一次，重新加载皮肤时再枚举一次；游戏运行期间添加的文件夹会在下次启动或重新加载皮肤后出现。

## 第 2 步：创建文件夹和 Metadata.json

创建一个文件夹，例如 `30 - MyChara`，并添加 `Metadata.json`：

- `name`：显示名称。可以是纯字符串，也可以是本地化对象 `{ "strings": { "default": "...", "ja": "...", ... } }`。`default` 是回退项；其他键是游戏语言代码。
- `rarity`：`Poor`、`Common`、`Uncommon`、`Rare`、`Epic`、`Legendary`、`Mythical` 之一。稀有度只控制颜色和解锁通知的等级；每种稀有度的金币倍率都是 1。
- `author`：纯字符串或本地化对象。
- `description`：可选，纯字符串或本地化对象。
- `speechtext`：可选，六个本地化对象组成的数组，结算界面把它们显示在角色的对话气泡中。游戏按结果挑选条目，顺序为：低魂槽失败、魂槽 40% 以上失败、过关、满魂槽过关、全连击、全良。给出的条目少于六个时，游戏会重复使用最后一个。

`Metadata.json` 缺失时，角色仍会加载，名称为 `(None)`，稀有度为 `Common`，作者为 `(None)`。

```json
{
  "name": {
    "strings": {
      "default": "My Character",
      "ja": "マイキャラ"
    }
  },
  "rarity": "Common",
  "author": {
    "strings": {
      "default": "Your Name"
    }
  }
}
```

## 第 3 步（2D 路线）：添加帧文件夹

文件夹中没有 `Script.lua` 时，游戏用其内置脚本（游戏安装目录中的 `CharaScript.lua`）渲染角色。该脚本把每个动画状态映射到一个子文件夹，并从中加载 `0.png`、`1.png`、`2.png`、...。加载在第一个缺失的编号处停止，因此编号必须连续。

| 动画状态 | 文件夹 |
|---|---|
| Game/Normal、Game/Clear、Game/Max | `Normal`、`Clear`、`Clear_Max` |
| Game/Gogo、Game/Gogo_Max | `GoGo`、`GoGo_Max` |
| Game/Miss、Game/Miss_Down | `Miss`、`MissDown` |
| Game/10combo、Game/10combo_Max | `10combo`、`10combo_Max` |
| Game/Cleared、Game/Failed | `Cleared`、`Failed` |
| Game/Clear_In、Game/Clear_Out | `Clearin`、`ClearOut` |
| Game/Max_In、Game/Max_Out | `Soulin`、`SoulOut` |
| Game/Miss_In、Game/Miss_Down_In、Game/Return | `MissIn`、`MissDownIn`、`Return` |
| Game/GoGoStart、Game/GoGoStart_Clear、Game/GoGoStart_Max | `GoGoStart`、`GoGoStart_Clear`、`GoGoStart_Max` |
| Game/Balloon_Breaking、Game/Balloon_Broke、Game/Balloon_Miss | `Balloon_Breaking`、`Balloon_Broke`、`Balloon_Miss` |
| Game/Kusudama_Breaking、Game/Kusudama_Broke、Game/Kusudama_Miss、Game/Kusudama_Idle | `Kusudama_Breaking`、`Kusudama_Broke`、`Kusudama_Miss`、`Kusudama_Idle` |
| Game/Tower/Standing、Climbing、Running、Clear、Fail（以及 `_Tired` 变体） | `Tower_Char/Standing`、`Tower_Char/Climbing`、`Tower_Char/Running`、`Tower_Char/Clear`、`Tower_Char/Fail`（另有 `Tower_Char/Standing_Tired` 等） |
| Menu/Wait、Menu/Start、Menu/Normal、Menu/Select | `Menu_Wait`、`Menu_Start`、`Menu_Loop`、`Menu_Select` |
| Entry/Normal、Entry/Jump | `Title_Normal`、`Title_Entry` |
| Result/Normal、Result/Clear、Result/Failed_In、Result/Failed | `Result_Normal`、`Result_Clear`、`Result_Failed_In`、`Result_Failed` |

内置脚本从文件夹根目录读取两张静态图片：`Render.png`（完整尺寸的立绘，在游戏请求 Render 动画类型的任何地方绘制，例如房间中）和 `Preview.png`（缩略图；缺失时脚本使用 `Normal/0.png`）。

缺失的状态会回退到另一个状态，因此角色可以只提供一个子集。回退链为：Clear -> Normal，Max -> Clear，Miss -> Normal，Miss_Down -> Miss，Gogo -> Normal，Gogo_Max -> Gogo，10combo_Max -> 10combo，GoGoStart_Clear -> GoGoStart，GoGoStart_Max -> GoGoStart_Clear，塔的 `_Tired` 状态 -> 其普通状态，Tower/Fail -> Tower/Standing_Tired，Kusudama_Idle -> Normal，Menu/Wait -> Gogo，Menu/Start、Menu/Select 和 Entry/Jump -> 10combo，Menu/Normal、Entry/Normal 和 Result/Normal -> Normal，Result/Clear -> Clear，Result/Failed_In -> Miss_In，Result/Failed -> Miss。没有回退的状态（例如 Cleared、Failed、Return、气球状态）缺失时不绘制任何内容。一个可用角色的最低要求是 `Normal/0.png`。

```
30 - MyChara/
  Metadata.json
  CharaConfig.txt
  Render.png
  Normal/0.png 1.png 2.png ...
  Clear/0.png ...
  GoGo/0.png ...
  Miss/0.png ...
  Menu_Loop/0.png ...
  Result_Clear/0.png ...
  Sounds/                （可选的语音片段，见第 6 步）
```

## 第 4 步：编写 CharaConfig.txt

`CharaConfig.txt` 是一个 `Key=Value` 文本文件；以 `;` 开头的行是注释。内置脚本读取以下键（自带的 3D 模板也读取位置键）：

- `Chara_Resolution=W,H`（默认 `1280,720`）：你编写下面的坐标时所依据的分辨率。绘制时游戏会把位置从该分辨率缩放到皮肤分辨率。
- `Chara_LegacyMode`（默认 `1`）：保留 0.6.0 的锚定和偏移修正。从旧版本移植的角色依赖它。
- `Game_Chara_X=...` / `Game_Chara_Y=...`：演奏位置；脚本使用每个列表的第一个值。`Game_Chara_Offset=X,Y` 是另一种写法。
- `Game_Chara_X_AI=...` / `Game_Chara_Y_AI=...`：AI 对战中每个玩家一个值。两个键都存在时，它们会为此角色替换皮肤的 AI 对战位置。
- `Game_Chara_Balloon_X` / `Game_Chara_Balloon_Y`、`Game_Chara_Kusudama_X` / `Game_Chara_Kusudama_Y`：气球和花球段落期间的位置（使用第一个值）。`Game_Chara_Balloon_Offset`、`Game_Chara_Kusudama_Offset` 和 `Game_Chara_Tower_Offset` 接受一对 `X,Y`。
- `Menu_Offset=X,Y`、`Menu_Chara_Scale`、`Result_Offset=X,Y`、`Heya_Chara_Render_Offset=X,Y`：菜单、结算和房间立绘的偏移。
- `Game_Chara_Motion_<State>=0,1,2,...`：某个状态的帧播放顺序，为从 0 起的帧索引。省略时按文件顺序播放。状态名称遵循文件夹名称，例如 `Game_Chara_Motion_Normal`、`Game_Chara_Motion_GoGo`、`Game_Chara_Motion_Miss_Down`、`Game_Chara_Motion_Balloon_Broke`、`Game_Chara_Motion_Tower_Climbing`。
- `Game_Chara_Beat_<State>=N`：该状态一个循环跨越多少拍，例如 `Game_Chara_Beat_Normal=1`、`Game_Chara_Beat_GoGo=2`。
- 菜单、标题和结算状态使用 `Menu_Chara_Motion_Loop/Wait/Start/Select`、`Title_Chara_Motion_Normal/Entry`、`Result_Chara_Motion_Normal/Clear/Failed_In/Failed`，配以对应的 `_Beat_` 键或以毫秒计的固定时长：`Chara_Menu_Loop_AnimationDuration`、`Chara_Menu_Wait_AnimationDuration`、`Chara_Menu_Start_AnimationDuration`、`Chara_Menu_Select_AnimationDuration`、`Chara_Normal_AnimationDuration`、`Chara_Entry_AnimationDuration`、`Chara_Result_Normal_AnimationDuration`、`Chara_Result_Clear_AnimationDuration`、`Chara_Result_Failed_In_AnimationDuration`、`Chara_Result_Failed_AnimationDuration`。

带默认值的完整键列表是内置 `CharaScript.lua` 顶部的 `load_chara_config_defs` 表。脚本会忽略它不认识的键，因此自带的 `01 - Template/CharaConfig.txt` 中也包含一些在此文件里不起作用的皮肤侧键。

```ini
Chara_Version=0.6.1.0
Chara_Resolution=1920,1080

;角色 X 位置（1P,2P）
Game_Chara_X=0,0
;角色 Y 位置（1P,2P）
Game_Chara_Y=0,805

;Normal 状态的帧顺序和每循环拍数
Game_Chara_Motion_Normal=0,1,2,3,4,5,6
Game_Chara_Beat_Normal=1

;GoGo 的帧顺序和每循环拍数
Game_Chara_Motion_GoGo=0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15
Game_Chara_Beat_GoGo=2
```

## 第 5 步（3D 路线）：提供 model.glb 和每角色的 Script.lua

角色文件夹中存在 `Script.lua` 时，它会完全替换内置脚本。游戏随后按名称调用以下全局函数：

- `loadAnimation(animationType)`、`disposeAnimation(animationType)`
- `availableAnimation(animationType)`，返回布尔值。游戏仍接受旧的拼写错误 `avaialbeAnimation`：它先尝试 `availableAnimation`，再回退到 `avaialbeAnimation`。自带的 3D 模板仍使用旧名称。
- `setAnimationDuration(animationType, durationMs)`、`resetAnimationCounter(animationType)`
- `update(delta, animationType, looping)`，非循环动画播放完毕时返回 `true`
- `draw(animationType, x, y, scaleX, scaleY, opacity, color, contextType, anchor, clipW, clipH, clipX, clipY, rotation, blendMode, wrapMode, gradientMap)`
- `getDrawSize(animationType)`，返回宽度和高度
- `getHeyaRenderOffset()`，返回 x 和 y；`getAIBattlePosition(player, charaScale)`，返回 x 和 y，或返回 `nil` 以使用皮肤的位置
- `loadVoice(voiceType)`、`disposeVoice(voiceType)`、`playVoice(voiceType)`

动画类型是 `CHARACTER.ANIM_*` 常量背后的字符串（`"Game/Normal"`、`"Menu/Normal"`、...），再加上两个特殊类型 `CHARACTER.ANIM_PREVIEW`（缩略图）和 `CHARACTER.ANIM_RENDER`（完整立绘）。语音类型是 `CHARACTER.VOICE_*` 常量。第 3 步的回退链同样适用于脚本化角色：游戏询问 `availableAnimation`，并沿备选项逐一尝试直到某个可用。

自带的 `01 - Template3D` 文件夹只包含 `CharaConfig.txt`、`Effects.json`、`Metadata.json`、`model.glb`、`Render.png` 和 `Script.lua`。它的脚本用 `MODEL:Load` 加载 `model.glb`，把它渲染到自己用 `SCENE3D:CreateScene` 创建的场景中，读取 `CharaConfig.txt` 的位置键，并在一张 `CLIP` 表中把每个动画类型映射到片段索引和拍数。要制作 3D 角色，复制该文件夹，替换 `model.glb` 和 `Render.png`，并编辑 `CLIP`，让每个类型指向你的模型中正确的片段索引。

```lua
-- 摘自 01 - Template3D/Script.lua
local CLIP = {
  [CHARACTER.ANIM_GAME_NORMAL] = { clip = 0,  beat = 1 };
  [CHARACTER.ANIM_GAME_CLEAR]  = { clip = 1,  beat = 1 };
  [CHARACTER.ANIM_GAME_GOGO]   = { clip = 2,  beat = 4 };
  [CHARACTER.ANIM_MENU_NORMAL] = { clip = 14, beat = 2 };
  [CHARACTER.ANIM_RESULT_CLEAR]= { clip = 19, beat = 1 };
  -- 模型支持的每个动画状态各一个条目
}

function loadAnimation(animationType)
  -- 构建片段 / 预览 / 立绘数据并标记为可用
end

function availableAnimation(animationType)
  return animations[animationType] ~= nil
end
```

## 第 6 步：可选文件：Effects.json、Unlock.json、Palettes.json、语音

- `Effects.json`：`gauge`（`Normal`、`Hard` 或 `Extreme`；默认 `Normal`）选择魂槽类型。除非游戏强制使用普通魂槽，`Hard` 把金币收益乘以 1.5，`Extreme` 乘以 1.8。Minesweeper（扫雷）趣味 Mod 启用时，`bombFactor`（1-100，默认 20）是该 Mod 变成地雷的音符百分比，`fuseRollFactor`（0-100，默认 0）是它变成导火线连打的气球百分比。
- `Unlock.json`：存在时，角色会保持锁定直到玩家满足条件。格式和条件 id 与歌曲的一致；见解锁条件指南。玩家在房间界面购买金币条件；游戏在结算界面自动检查其他条件。自带示例：Kuro 使用 `{ "condition": "dp", "type": "me", "values": [3, 3, 10] }`（十首 Extreme 谱面达到全连击或更好），Aoi 使用 `{ "condition": "ch", "type": "me", "values": [200] }`（200 金币）。
- `Palettes.json`：玩家可以应用到角色上的调色板数组。每个条目有 `name`、`blend`（0-1）、`stops`（`[position, R, G, B]` 或 `[position, R, G, B, A]` 渐变色标的数组；请至少给出两个）和 `plays`，即解锁该调色板所需的使用此角色的演奏次数（0 或缺失表示立即可用）。`"stops": null` 的条目是不着色的默认项。
- 语音：内置脚本从角色文件夹内的固定路径加载 `.ogg` 文件，例如 `Sounds/Clear/Clear.ogg`、`Sounds/Clear/Failed.ogg`、`Sounds/Clear/FullCombo.ogg`、`Sounds/Clear/AllPerfect.ogg`、`Sounds/Menu/SongSelect.ogg`、`Sounds/Menu/SongDecide.ogg`、`Sounds/Menu/DiffSelect.ogg`、`Sounds/Title/Sanka.ogg`、`Sounds/Result/BestScore.ogg`、`Sounds/Result/ClearSuccess.ogg`、`Sounds/Result/ClearFailed.ogg`。完整列表是内置 `CharaScript.lua` 顶部的 `voice_files` 表。脚本会跳过缺失的文件。

```json
{
  "gauge": "Normal",
  "bombFactor": 20,
  "fuseRollFactor": 0
}
```

```json
{
  "condition": "ch",
  "type": "me",
  "values": [ 200 ]
}
```

```json
[
  { "name": "Default", "stops": null },
  { "name": "Green", "blend": 1.0, "stops": [ [0, 0, 0, 0], [0.25, 0, 255, 0] ], "plays": 10 }
]
```

## 第 7 步：重启并选择角色

重启游戏（或从设置中重新加载皮肤）。角色会出现在房间界面的角色列表中，锁定的角色会显示其解锁条件。Lua 舞台也可以通过 `CHARACTERLIST` 全局对象读取该列表，它公开每个条目的文件夹名称、显示名称、稀有度和解锁条件。

## 故障排除与注意事项

- 角色没有出现：检查文件夹是否直接位于 `Global/Characters/` 下，然后重启游戏。游戏只在启动时构建列表一次。
- 角色不绘制任何内容：`Normal/0.png` 缺失，或文件夹名称与第 3 步的表不一致。帧必须命名为 `0.png`、`1.png`、... 且没有间断；间断会让动画在该索引处结束，且不报错。
- 角色在屏幕外或尺寸不对：`Chara_Resolution` 必须与你编写位置值时所依据的分辨率一致。该键缺失时游戏假定为 `1280,720`。
- 只有部分动画播放：没有回退的状态（Cleared、Failed、Return、Balloon 和 Kusudama 状态）需要自己的文件夹。
- 3D 角色的所有动画都显示为不可用：`Script.lua` 必须定义 `availableAnimation`（或 `avaialbeAnimation`），并对已加载的类型返回 `true`。
- 存在的 `Script.lua` 会完全替换内置脚本。脚本化角色仍然可以加载按编号命名的 PNG 文件夹，但只有脚本自己加载它们才行。
- 存档引用的是文件夹名称，因此重命名玩家已经选择的文件夹会把他们的选择重置为空槽位。
- 自带的 JSON 文件包含尾随逗号。游戏的 JSON 解析器接受它们；严格的校验器会拒绝它们。
