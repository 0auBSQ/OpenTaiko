<!-- modules/my-room.md -->

# 我的房间

我的房间是一个全局舞台，即 `Global/Modules/Stages/` 中的 `my_room`：玩家的房间，玩家可以在其中走动，用未知之塔、金库和金币商店给的东西布置它，并向在线访客开放。任何皮肤都能打开它，而且由于其数据保存在存档中，每个皮肤显示的都是同一个房间。

## 进入与离开

把共享字符串 `my_room_return` 设为我的房间应返回的舞台，然后退出到 `my_room`：

```lua
SHARED:SetSharedString("my_room_return", "my_menu")               -- back to my_menu
-- or, with the transition to play on the way back:
SHARED:SetSharedString("my_room_return", "my_menu|my_transition")
return Exit("stage", "my_room")
```

我的房间在进入时读取并清除这个字符串。字符串为空时，返回的去处是标题界面（`_title`）。

共享字符串 `my_room_place` 记住这是谁的房间以及玩家站在哪里，在离开房间又回来的过程中保持不变：占位矩阵（一件列出可前往舞台的家具）和舱体（一件打开另一个舞台的家具）都会通往房间之外。标题界面可以清除它（设为 `""`），让下次进入从门口开始。

Open-World Memories 为旧的 `Exit("stage", "myroom")` 调用和引擎的 `"heya"` 退出保留了一个转发舞台 `myroom`；它会在正式版中移除，因此新代码应退出到 `my_room`。

## 数据

| 键 | 类型 | 内容 |
| --- | --- | --- |
| `my_room` | 存档字符串 | 房间本身，以 JSON `{ v, room, jukebox }` 形式保存（`sf:GetGlobalString("my_room")`）。只为存档自己的槽位写入：绝不为在线访客或空槽位写入。 |
| `.myroom_<id>` | 计数器 | 其他舞台给予的家具：我的房间在下次进入时把它加入房间库存一次，并记录已经收取过的部分。 |
| `.myroom_held_<id>` | 计数器 | 存档拥有该家具的数量。我的房间会保持它最新；给予家具的舞台也会立即增加它。 |
| `.myroom_iw`, `.myroom_ih`, `.myroom_items` | 计数器 | 房间的宽度和高度以及已摆放的家具数量，由我的房间写入，供存档列表显示（只读取它们）。 |
| `myroom_thumb_<id>` | 共享纹理 | 商店或奖励家具的图片，在我的房间启动时生成，由购买和奖励弹窗显示。 |

要从你自己的舞台给予一件家具，请像 [Pagoda.Rewards](pagoda.md) 和 [Vault.Chests](vault.md) 那样，给两个计数器各加 1。在 `my_room` 字符串未设置期间，一个临时迁移会从 Open-World Memories 的旧存储中读取房间；它会在正式版中移除。

## 它所依赖的部分

- 它的菜单和对话框通过 [UI.Pick](ui.md) 使用皮肤的 UI 库，在首次使用时选定（Open-World Memories 中为 PopUI，未指定任何库的皮肤中为 SimpleUI），金币显示就是该库的金币控件。
- 它的字符串，包括家具名称（`lang/<code>/items.json`，以家具 id 为键），放在它自己的 `lang/` 文件夹中，共九种语言。
- 家具目录是舞台文件夹中的 `data/furniture.json`。
- 占位矩阵只列出 `Global/Modules/Stages/` 中存在的调试舞台。这些调试舞台以及电视使用的 `video_player` 只读活动也都是全局模块。
- 当 `Global/Modules/Stages/` 中存在舞台 `mysterypod`，或者皮肤自己的 `mysterypod` 舞台在其 `onStart` 中把共享字符串 `mysterypod_installed` 设为 `"1"` 时，舱体会打开该舞台。
- 在线访问使用房间 id `"myroom"` 及其载荷的第 2 版，与迁移之前相同。
