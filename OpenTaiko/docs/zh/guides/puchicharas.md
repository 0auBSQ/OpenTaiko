<!-- guides/puchicharas.md -->

# 为 OpenTaiko 添加小角色

小角色（puchichara）是演奏时在魂槽旁边跳动的小伙伴。每个小角色是游戏安装目录下 `Global/PuchiChara/` 中的一个文件夹，包含一张两帧的精灵图集和几个可选的 JSON 文件。你不需要写代码：用正确的文件名创建文件夹，游戏会在下次启动时识别它。

兼容性：OpenTaiko 0.6.1 仍能无需修改地加载为 0.6.0 制作的小角色。

## 开始之前

- 已安装 OpenTaiko 0.6.1。游戏从游戏可执行文件旁的 `Global/PuchiChara/` 读取小角色，所有皮肤共享该文件夹。
- 一个能导出带透明通道 PNG 的图像编辑器。
- 一个用于编辑 JSON 文件的文本编辑器。
- 可选：一段用作 `Welcome.ogg` 的短 `.ogg` 片段。

## 第 1 步：创建文件夹

游戏在启动时列出 `Global/PuchiChara/` 的子文件夹，并把每一个视为一个小角色。文件夹名称即身份：玩家选择该小角色时，游戏把它写入存档，并以它为键记录解锁。请选择一个稳定的名称：重命名之后，现有选择会回退到第一个文件夹，记录的解锁也不再匹配。自带的文件夹使用排序前缀，例如 `00 - None`、`01a - OpenTaiko-Kun` 和 `02 - Bol`。游戏不会对列表排序，因此前缀让文件系统顺序保持可预测。第一个文件夹是存档引用了不再存在的文件夹时的回退项，因此请让 `00 - None` 保持在第一位。

```
Global/PuchiChara/
    00 - None/
    01a - OpenTaiko-Kun/
    02 - Bol/
    99 - MyMascot/          <-- 你的新文件夹
```

## 第 2 步：绘制精灵图集（Chara.png）

游戏从 `Chara.png` 绘制小伙伴，这是一张横向的精灵图集。帧布局来自皮肤值 `Game_PuchiChara`，默认为 `256,256,2`（帧宽、帧高、帧数），因此自带的图集是 512x256 像素：两个 256x256 的帧并排，帧 0 在左。演奏期间游戏在各帧之间循环并添加垂直跳动（菜单中为待机跳动，游戏中为随节拍跳动），因此两帧应当是同一角色的两个姿势。使用透明背景。`Chara.png` 缺失时条目仍会加载，但不绘制任何内容。

自带的文件夹还包含一个 `Chara.xcf`（GIMP 源文件）和一个 `PuchiConfig.txt`。游戏两者都不读取。

```ini
Chara.png : 512 x 256 PNG，透明背景
  +-----------------+-----------------+
  |    frame 0      |    frame 1      |
  |   256 x 256     |   256 x 256     |
  +-----------------+-----------------+
```

## 第 3 步：编写 Metadata.json

创建包含以下字段的 `Metadata.json`：

- `name`、`author`、`description`：各为纯字符串或本地化对象 `{ "strings": { "default": "...", "ja": "...", ... } }`，其中 `default` 是回退项，其他键是游戏语言代码。
- `rarity`：`Poor`、`Common`、`Uncommon`、`Rare`、`Epic`、`Legendary`、`Mythical` 之一。稀有度决定颜色和解锁通知的等级。每种稀有度的金币倍率都是 1，因此它不改变收益。未知的值表现得像 `Common`。

文件缺失时条目仍会加载，名称为 `(None)`，稀有度为 `Common`，作者为 `(None)`。

```json
{
    "name": {
        "strings": {
            "default": "MyMascot",
            "ja": "マイマスコット"
        }
    },
    "rarity": "Rare",
    "description": {
        "strings": {
            "default": "A friendly companion.\nWaves during play."
        }
    },
    "author": "YourName"
}
```

## 第 4 步（可选）：添加 Effects.json

`Effects.json` 赋予小角色演奏效果。文件缺失时所有字段都默认为关闭：

- `allpurple`（bool）：大咚和大咔音符变成任一鼓面都可以击打的紫色音符。
- `autoroll`（int）：连打和气球上每秒的自动击打次数。任何大于 0 的值都会把金币倍率设为 0。
- `showadlib`（bool）：显示隐藏的 ADLIB 音符。金币乘以 0.9。
- `splitlane`（bool）：把咚和咔音符绘制在不同的轨道上。

纯装饰性的小伙伴请省略此文件。

```json
{
    "allpurple": false,
    "autoroll": 0,
    "showadlib": false,
    "splitlane": false
}
```

## 第 5 步（可选）：添加 Welcome.ogg 和 Render.png

- `Welcome.ogg`：游戏加载到语音音量组中的一段语音片段。游戏内置的房间界面在玩家选中该小角色时播放它；Lua 舞台无法访问它，因此自带房间界面的皮肤不会播放它。
- `Render.png`：Lua 舞台可以作为立绘绘制的完整尺寸静态图片（`PUCHICHARALIST` 条目的 `render` 纹理）。它是一张任意尺寸的单张图片。自带的小角色都没有包含它。

两个文件都是可选的。

```
MyMascot/
    Chara.png       （必需，动画精灵图集）
    Metadata.json   （名称、稀有度、作者、描述）
    Effects.json    （可选的演奏效果）
    Unlock.json     （可选的解锁条件）
    Welcome.ogg     （可选的语音片段）
    Render.png      （可选的立绘）
```

## 第 6 步（可选）：添加解锁条件（Unlock.json）

没有 `Unlock.json` 时小角色立即可用。要锁定它，添加一个包含 `condition`、`type`、`values` 和 `references` 字段的 `Unlock.json`；格式和条件 id 与歌曲和角色相同（见解锁条件指南）。下面的示例在玩家累计获得 500 金币后解锁。自带示例：OpenTaiko-Kun 售价 100 金币（`"condition": "ch"`），Bol 要求过关 20 首谱师为 `bol` 的谱面（`"condition": "sc"`），Tinyfox 要求在分类 `Project Outfox Serenity` 中演奏一首歌曲（`"condition": "sg"`）。

玩家在房间界面购买金币条件。游戏在每次演奏后于结算界面检查其他条件；玩家满足某个条件时，游戏把小角色加入存档的已解锁列表并显示一条通知。

```json
{
    "condition": "ce",
    "type": "me",
    "values": [
        500
    ]
}
```

## 第 7 步：重启并选择它

重启游戏（或从设置中重新加载皮肤）以让游戏重建列表。打开房间界面，从小角色列表中挑选新条目。选中后它会在演奏时出现在魂槽旁边。如果演奏时没有出现小伙伴，请检查系统设置中的 `Draw PuchiChara` 选项是否已启用。

## 故障排除与注意事项

- 文件名必须完全一致：`Chara.png`、`Metadata.json`、`Effects.json`、`Unlock.json`、`Welcome.ogg`、`Render.png`。游戏会忽略命名错误的文件并使用默认值。
- 存档和解锁记录原样存储文件夹名称。重命名文件夹会使现有选择回退到第一个文件夹。
- 游戏按皮肤的 `Game_PuchiChara` 帧尺寸（默认 `256,256,2`）切分精灵图集。尺寸不同的图集它也按同样的数值切分，因此帧会被裁切或错位。如果皮肤覆盖了 `Game_PuchiChara`，请匹配该值。自带皮肤没有覆盖它。
- `autoroll` 大于 0 会把金币收益归零，`showadlib` 会把它降到 0.9，因此设置了其中任何一项的装饰性小伙伴会减少其玩家的收益。
- 自带的 JSON 文件包含尾随逗号。游戏的 JSON 解析器接受它们；严格的校验器会拒绝它们。
- 游戏在启动时和重新加载皮肤时构建列表一次。游戏运行期间添加的文件夹会在下次启动或重新加载皮肤后出现。
