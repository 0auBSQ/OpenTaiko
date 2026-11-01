<!-- guides/notes-skins.md -->

# 添加音符包与判定包

皮肤可以为音符和判定文字提供多种外观，玩家在设置中选择其中一种。每种外观是一个包：皮肤 `Graphics/5_Game/` 下的一个编号文件夹。音符包包含音符、连打、气球、击打特效以及气球和连打的气泡；判定包包含判定文字。本指南说明这些文件夹、每个包包含的图集、描述包的 `Definition.json` 文件，以及游戏如何补齐包中缺少的内容。

## 开始之前

- 一个你自己的皮肤（见[添加皮肤与主题](skins.md)）。自带皮肤 `System/Open-World Memories/` 有 4 个太鼓音符包、1 个邦戈音符包和 4 个判定包可供复制。
- 一个能保存带透明度 PNG 文件的图像编辑器。
- 你皮肤的音符格子尺寸：布局值 `Game_Notes_Size=`（自带皮肤中为 `195,195`）。本页音符图集的每个格子都是这个尺寸，巨型音符图集的格子除外。

## 第 1 步：创建包文件夹

游戏在 `Graphics/5_Game/` 下查找三个文件夹，并把其中每个编号子文件夹视为一个包，按编号排序：

- `Notes Skin (Taiko)/`：太鼓玩家的音符包。
- `Notes Skin (Bongo)/`：邦戈玩家的音符包。
- `Judges Skin/`：判定文字包，所有玩家都使用。

每个文件夹中的包 `1` 是默认包，也是其他包的基础（第 7 步）。文件夹编号只决定包的顺序；设置中显示的是 `Definition.json` 中的名称。

```
Graphics/5_Game/
  Notes Skin (Taiko)/
    1/                  <- the default look, complete
      Definition.json
      Notes.png
      ...
    2/                  <- another look: only the files that differ
      Definition.json
      Notes.png
  Notes Skin (Bongo)/
    1/
  Judges Skin/
    1/
      Definition.json
      Judge.png
```

## 第 2 步：绘制太鼓音符图集

图集每个动画帧占一行（帧数在第 5 步设置），格子按下面的顺序并排排列。

| 文件 | 内容 |
| --- | --- |
| `HitCircle.png` | 判定圈，一个格子。 |
| `Notes.png` | 咚、咔、大咚、大咔、KaDon、ADLib。 |
| `Rolls.png` | 小连打的头部、身体和尾部，然后是大连打的头部、身体和尾部。游戏会沿连打拉伸身体格子。 |
| `Balloons.png` | 气球（2 个格子宽）、彩球（2 个格子宽），然后是导火线连打的头部、身体和尾部。邦戈玩家也会看到这些气球。 |
| `ChipEffect.png` | 可选。被击中的音符到达处显示的闪光，列与 `Notes.png` 相同，一行。没有它时，游戏让音符本身闪光。 |
| `Notes_Partner.png` | 可选。`Notes.png` 每一列的搭档外观（第 6 步）。 |
| `Notes_Giant.png` | 可选。巨型咚、咔、KaDon 和 ADLib（第 6 步）。 |
| `Preview.png` | 可选。设置中为该包显示的图片。没有它时显示 `Notes.png`。 |

包中还包含击打特效以及气球和连打的气泡：

| 文件 | 内容 |
| --- | --- |
| `Hit/Explosion.png` | 判定圈上的爆发效果，每行 7 个正方形帧：一行“良”，一行“可”，然后可选地有大音符的一行“良”和一行“可”。只有 2 行时，大音符使用前两行。 |
| `Hit/Explosion_Big.png` | 大音符上额外的爆发效果，一张图片，两种判定都会绘制。 |
| `Hit/FireWorks.png` | 飞行中的大音符留下的火花。 |
| `Hit/Bomb.png` | 被击中的炸弹或失败的导火线连打的爆炸，一行 7 帧。 |
| `Hit/Great/`、`Hit/Great_Big/`、`Hit/Good/`、`Hit/Good_Big/` | 可选的帧文件夹（`0.png` 到 `14.png`），在小音符或大音符上打出“良”（`Great`）或“可”（`Good`）时绘制在轨道上。 |
| `Kusudama/` | 彩球的 `Script.lua` 及其图片，包括 `Kusudama_Number.png`。 |
| `Fuseroll/` | 导火线连打的气泡 `Fuse.png`，以及它的数字 `Number_Fuse.png`。 |
| `Breaking.png` | 气球气泡破裂的图片：6 张高度相同的图片，上下排列。 |
| `Balloon.png` | 气球气泡。 |
| `Roll.png` | 连打计数器的气泡，动画帧并排排列。 |

## 第 3 步：绘制邦戈音符包

邦戈包只包含对邦戈玩家不同的部分：

| 文件 | 内容 |
| --- | --- |
| `Notes.png` | 红、黄、粉、拍手、ADLib。 |
| `Rolls.png` | 红、粉、黄和拍手连打，每种都有头部、身体和尾部。 |
| `HitCircle.png` | 可选。没有它时，邦戈玩家看到太鼓包的判定圈。 |
| `ChipEffect.png`、`Notes_Partner.png` | 可选，列与本包的 `Notes.png` 相同。 |
| `Notes_Giant.png` | 可选，列与本包的 `Notes.png` 相同。 |
| `Preview.png` | 可选。 |

气球、击打特效和气泡始终来自所选的太鼓包。

## 第 4 步：绘制判定包

判定包包含 `Judge.png`：5 行高度相同，依次是“良”、“可”、“不可”、ADLib 和炸弹的 Boom。它的 `Definition.json` 只需要 `Name`。把某一行留成透明即可隐藏该判定；自带皮肤的“(Without Good)”包就对“良”这一行这样做。

## 第 5 步：编写 Definition.json

`Definition.json` 为包命名并描述它的图集。所有字段都是可选的；没有该文件的包以文件夹编号作为名称，并使用下面的默认值。

```json
{
	"Name": { "strings": { "default": "Sky Spirits", "ja": "スカイスピリッツ" } },
	"Notes": {
		"Frames": 3,
		"Animation": [
			{ "FromCombo": 0, "Rows": [0] },
			{ "FromCombo": 50, "Rows": [1, 1, 0, 0], "RowsOnEntry": [0] },
			{ "FromCombo": 150, "Rows": [0, 1] },
			{ "FromCombo": 300, "Rows": [0, 2], "RowsOnEntry": [0, 1] }
		]
	},
	"Rolls": { "Frames": 3, "Animation": [] },
	"Balloons": { "Frames": 3, "Animation": [] },
	"FlyingNotesFrame": 1,
	"PartnerNotesScale": 1.4142,
	"GiantNotesScale": 2.0
}
```

| 字段 | 默认值 | 含义 |
| --- | --- | --- |
| `Name` | 文件夹编号 | 设置中显示的名称，每个语言代码一个字符串，`default` 用于其他所有语言。 |
| `Notes`、`Rolls`、`Balloons` | 1 帧，无动画 | 图集的行数（`Frames`）及其动画（`Animation`）。`Notes` 也决定 `Notes_Partner.png` 和 `Notes_Giant.png` 的行。 |
| `FlyingNotesFrame` | `0` | 击中后飞向计量槽的音符所用的 `Notes.png` 行。 |
| `PartnerNotesScale` | 2 的平方根 | 包中没有 `Notes_Partner.png` 时小搭档音符的缩放比例（第 6 步）。 |
| `GiantNotesScale` | `2` | 包中没有 `Notes_Giant.png` 时巨型音符的缩放比例（第 6 步）。 |

`Animation` 是一个档位列表。游戏使用玩家连击已达到的档位中 `FromCombo` 最高的那一个。一拍分为 4 段，`Rows` 给出每一段绘制的行；较短的列表会重复，因此 `[0, 1]` 每段交替，`[0]` 保持静止。连击达到某个档位后的约三分之二秒内，`RowsOnEntry` 代替 `Rows`。没有档位时，图集显示第一行。示例中的表就是游戏一直使用的动画，而设置中的简化模式会让每个图集都停在第一行。

两个可选字段可以调整档位。`FromComboByDifficulty` 为部分难度设定档位的连击数，键为 `Easy`、`Normal`、`Hard`、`Extreme` 和 `ExtraExtreme`；其他难度使用 `FromCombo`，因此 `"FromCombo": 100, "FromComboByDifficulty": { "Easy": 30 }` 让档位在 Easy 中从 30 连击开始，在其他难度中从 100 开始。塔的谱面在 Normal 侧算作 Normal，在其他侧算作 Extreme；段位使用正在演奏的歌曲的难度。

`RowsByNote` 为部分音符提供各自的 `Rows`，键为下表中的名称；其他音符保持 `Rows`。在 `Notes.png` 中，`"Rows": [0, 2], "RowsByNote": { "BigDon": [1, 2] }` 让除大咚以外的音符交替显示第 0 行和第 2 行，大咚则交替显示第 1 行和第 2 行。连打的身体和尾部跟随其头部，`RowsOnEntry` 仍然适用于所有音符。

| 图集 | 音符名称 |
| --- | --- |
| `Notes.png`, 太鼓 | `Don`, `Ka`, `BigDon`, `BigKa`, `KaDon`, `ADLib` |
| `Notes.png`, 邦戈 | `Red`, `Yellow`, `Pink`, `Clap`, `ADLib` |
| `Rolls.png`, 太鼓 | `Roll`, `BigRoll` |
| `Rolls.png`, 邦戈 | `RedRoll`, `PinkRoll`, `YellowRoll`, `ClapRoll` |
| `Balloons.png` | `Balloon`, `Kusudama`, `FuseRoll` |

## 第 6 步：搭档音符与巨型音符

搭档音符大多保持原有尺寸，但小音符（小咚或小咔、ADLib、炸弹、所有邦戈音符）会采用大的外观。巨型音符要大得多，并缓慢摇晃。两者各有一个可选图集：

- `Notes_Partner.png` 与 `Notes.png` 列相同、行数相同。小搭档音符使用自己所在列的格子。
- `Notes_Giant.png` 与 `Notes.png` 行数相同。在太鼓包中，它的列是咚、咔、KaDon 和 ADLib，因为巨型音符无论原本大小都显得很大：小咚和大咚都使用咚的格子。在邦戈包中，它的列与 `Notes.png` 相同。格子尺寸为图集宽度除以列数，因此巨型音符的格子可以比音符格子大。

没有这些图集时，游戏用 `Notes.png` 构建外观：

| 音符 | 搭档，无 `Notes_Partner.png` | 巨型，无 `Notes_Giant.png` |
| --- | --- | --- |
| 太鼓的小咚或小咔 | 大咚或大咔 | 大咚或大咔，乘以 `GiantNotesScale` |
| 其他小音符 | 该音符，乘以 `PartnerNotesScale` | 该音符，乘以 `PartnerNotesScale` 和 `GiantNotesScale` |
| 大咚、大咔、KaDon | 不变 | 该音符，乘以 `GiantNotesScale` |

即使包中两个图集都有，也请设置两个缩放比例：这样复制该包并删除某个图集的皮肤制作者仍能得到合适的尺寸。缩放后的音符是拉伸的图片，看起来比按该尺寸绘制的格子更模糊。

<div class="callout warn">游戏只从所选的包读取 Notes_Partner.png、Notes_Giant.png 和 ChipEffect.png。它们必须与该包自己的音符相匹配，因此包 1 中的副本永远不会代替它们。</div>

## 第 7 步：让包 1 补齐其余部分

所选的包缺少某个文件时，游戏从同一文件夹的包 1 中取用该文件。因此一个包可以只包含它更改的内容：只清除 `Hit/Explosion.png` 中一行的包，只包含一个 `Definition.json` 和那一个文件。规则如下：

- 游戏逐个检查每个文件，文件夹内的文件也是如此（`Hit/Great/3.png`）。彩球是例外：没有自己 `Kusudama/` 文件夹的包使用包 1 的整个文件夹。
- 回退只到达包 1。基于包 3 修改的包必须自带包 3 图集的副本。
- `Definition.json` 永远不会从包 1 取用，第 6 步的三个图集也不会。
- 邦戈包回退到邦戈包 1。

## 第 8 步：选择包

当皮肤的三个文件夹中任意一个至少有 2 个包时，设置的主题页面会首先显示“音符皮肤”分区，其中有该文件夹的一个选项，选项下方是包的预览。游戏会立即加载所选的包，并在 `ThemeSettings.db3` 中按皮肤记住这一选择。只有一个包时不会出现该选项，游戏使用那个包。

Lua 模块用 `THEME:GetNotesSkinFile(kind, file)` 读取所选包中的文件（见[玩家与档案](../api/players.md)）。自带的选歌界面用它绘制回放列表中的判定文字：

```lua
local file = THEME:GetNotesSkinFile("judges", "Judge.png")   -- "5_Game/Judges Skin/2/Judge.png", or ""
if file == "" then file = "5_Game/Judge.png" end              -- a skin without judges packs
```

## 没有包的皮肤

没有 `Notes Skin (Taiko)/` 文件夹的皮肤沿用包出现之前的文件：`Notes.png`（判定圈、4 种音符、连打和气球）、`Swap.png`（KaDon）、`Adlib.png`、`Kusu.png`、`FuseRoll.png`、`ChipEffect.png`、`10_Effects/Hit/` 文件夹以及 `11_Balloon/` 中的气球文件。没有 `Notes Skin (Bongo)/` 文件夹时，邦戈音符和连打来自 `Notes_Konga.png`；没有 `Judges Skin/` 文件夹时，判定来自 `5_Game/Judge.png`。这些文件没有搭档或巨型图集，因此搭档音符和巨型音符使用第 6 步的缩放外观。

## 故障排除与注意事项

- 设置中不显示选项：该文件夹只有一个包，或包文件夹不是以数字命名的。
- 某个包显示了另一个包的音符：该包缺少文件，回退取用了包 1 的文件；或在区分大小写的系统上文件名大小写不同。
- 音符显示错误的帧或被截断的图片：`Frames` 与图集高度除以格子高度的值不一致。
- 复制的包的搭档或巨型音符看起来和原来的一样：副本保留了原包的 `Notes_Partner.png` 或 `Notes_Giant.png`。
- 邦戈玩家看不到音符：所选的邦戈包和邦戈包 1 中都没有 `Notes.png`。
