<p align="center">
  <img src="https://user-images.githubusercontent.com/58159635/140600257-f712fc48-d09a-4a5e-a78d-e7c65ca19b80.png">
</p>

# OpenTaiko

TJAPlayer3-Develop-ReWrite的分叉项目，读取TJA文件的太鼓模拟器

前身：TJAPlayer3-Develop-BSQ

- 当前版本：v0.5.4

- Discord：https://discord.gg/aA8scTvZ6B

（译者注：由README-EN.md翻译。最近一次更新时间：2023年2月14日）

（译者注：编译指南：[https://github.com/l1m0n3/OpenTaiko/wiki/How-to-build-OpenTaiko-without-using-Visual-Studio-(on-Windows)](https://github.com/l1m0n3/OpenTaiko/wiki/How-to-build-OpenTaiko-without-using-Visual-Studio-(on-Windows))）

## 使用前注意事项（重要！！！）

- 您**全权**对您对本软件的使用负责。开发者不为您使用本软件造成的任何问题承担任何责任。

- 当前本软件无“官方”皮肤。若您使用的非官方皮肤有任何Bug，请先联系皮肤作者再联系本软件开发者。本软件不提供任何类AC皮肤或它们的分支。

- 在向他人寻求帮助前请先自行研究。

- 若您的计算机不能维持稳定60fps帧率，则本软件不适合它。

- 以上列出版本以外的版本不会得到任何支持。注意若您在使用pre版本，由于其非正式版本，我们不为造成的任何问题承担任何责任。

### 在直播或视频中使用本软件

若您在视频共享网站、直播服务、网站或博客上使用本软件，请确保您明确说明本软件不是万代南梦宫官方软件，并确保其不被与其它太鼓模拟器混淆。

此外，若您使用的网站有标签功能，请打上“OpenTaiko”“TJAPlayer3-Develop-BSQ”或“TJAP3-BSQ”标签以防止与其它模拟器混淆。这也会使您的视频与其它类似视频被一同归类，因此我们强烈建议您打上这些标签。

本软件作者并不支持违反版权法律的行为。请您遵守您所在国家的版权法律。

### 编辑源代码/再发布

OpenTaiko是一个使用MIT许可证的开源软件。在MIT许可证下，您可以编辑或再发布。但您须**自行承担**全部责任。此外，再编辑或再发布时**请**将"License"文件夹包含在您的库许可证内。请同样遵守其它皮肤或曲目包作者的许可证。OpenTaiko许可不适用于这些情况。

### 目标/非目标

**目标**

- 各种有趣的太鼓演奏的方式。

- 加强定制更好的皮肤的可行性，使得每个人都能轻松地以他们自己的风格游玩太鼓成为现实。

- 优化、修复及提升体验

**非目标**

- 精准复制其它游戏或商业许可（例如AC虹色版）。

## 关于发布Issue或Pull Request的规则

我们十分感谢您发布Issue或Pull Request。

- **请**遵守日本和法国版权法律。

- **重要！！！**：当您发布issue时，请写下您使用的版本和问题的复现步骤。如果是游戏崩溃，请附上TJAPlayer3.log文件。

- 如果您需要翻译，请提前在Discord上联系作者。

### 功能需求

如果您需要添加功能，请先在Discord上联系作者。

需求的功能如果好的话可能会被添加。

- **重要！！！**：类似于“请重建与AC虹色版完全一致的用户界面”的请求会被直接否决，不会得到回答。

## 常见问题与

- 段位选择界面上的所有曲目都是10星魔王！

```
请在.tja文件内的“NEXTSONG”行添加“,(Difficulty),(Course)”。

示例：

原：#NEXTSONG [TITLE],[SUBTITLE],[GENRE],[WAVE],[SCOREINIT],[SCOREDIFF]

现：#NEXTSONG [TITLE],[SUBTITLE],[GENRE],[WAVE],[SCOREINIT],[SCOREDIFF],[LEVEL],[COURSE]
```

- 我卡在了入口界面

```
按住P键
```

- 我发现了Bug。我该怎么办？

```
当你发现Bug时请提交issue。
```

- 我无法进入“太鼓塔”菜单。

```
“太鼓塔”菜单尚未被添加。
请从演奏模式中选择塔曲目。
```

- 游戏报“Tower_Floors”错误但未找到错误。

```
文件夹结构不应为“System/Graphics”，
而是“System/(Skin name)/Graphics”。
```

- 我找不到我自行添加的角色和迷你角色

```
从0.5.3.1版本开始，角色和迷你角色将由skin文件夹外的Global文件夹读取。请把它们放在那里。
```

## 更新历史

<details>
	<summary>v0.5.4</summary>

	- 修复了多个问题

	- 可以在线下载谱面

	- 支持角色和迷你角色各自的声音

	- 支持在游戏中更换音色

	- 选曲界面新增随机选曲选项

	- 新增康加鼓模式

	- 支持PREIMAGE元数据

	- 更改演奏模式及其图标

	- 新增紫音符（G）、炸弹音符（C），修复牵手音符（A、B）和隐藏音符（F）

</details>

<details>
	<summary>v0.5.3.1</summary>

	- 修复了多个问题

	- 全局化角色和迷你角色

	- 永久性的“最近演奏的曲目”文件夹

	- 简单/普通难度计时区

	- 主菜单及结果画面上的角色

	- 增加按难度搜索曲目

</details>

<details>
	<summary>v0.5.3</summary>

	- 修复了多个Bug

	- 段位结果界面的第一个版本

	- 段位扑面支持任意数量的曲目

	- 对2P Side的支持

	- 重大2P更新（请在Discord中查看更多信息）

	- 现可在演奏模式的选曲界面选择段位谱面

	- 添加了弹出框

	- 第一次【可解锁内容】更新

	- 添加了最爱曲目文件夹

	- 添加了数据库文件（角色与迷你角色的名称及作者名称）

	- 中文支持（WHMHammer）

	- 移除了SlimDX依赖（Mr Ojii）

	- 添加了简单风格皮肤（由cien制作）

	- 自动为每首歌生成唯一标识符

	- 修复了Discord RPC

	- 修复了几个配置文件问题（l1m0n3）

</details>

<details>
	<summary>v0.5.2.1</summary>

	- 修复了多个Bug

	- 在自动模式之外添加了多个AI级别

	- 添加了全局偏移量设置

	- 将自动滚奏替换为了滚奏速度

</details>

<details>
	<summary>v0.5.2</summary>

	- 太鼓部屋功能

	- 自定义名片和角色功能

	- 使金币可获得

	- 使段位称号可解锁

	- 增加多步贴图

	- 增加西班牙语翻译 (由funnym0th制作)

	- 增加“随机选项”

	- UX/UI改善

	- 加速歌曲加载

	- 修复有谱面分歧的谱面

</details>

<details>
	<summary>v0.5.1</summary>

	- 在段位道场中增加动画

	- 增加游戏退出界面和图标

	- 修复问题

	- 多语言支持

	- UI改善

	- 选曲界面增加其他多种样式

</details>

<details>
	<summary>v0.5.0</summary>

	- 太鼓Tower功能 (Background+Result screen backbone)

	- Tower谱面增加“TOWERTYPE”（用以在Tower难度中使用多种皮肤）

	- 段位道场增加准确率考核目标

	- box.def中增加“#BOXCOLOR”、“#BOXTYPE”、“#BGCOLOR”、“#BGTYPE”和“#BOXCHARA”

</details>

<details>
	<summary>v0.4.3</summary>

	- 增加太鼓Tower（游玩）

</details>

<details>
	<summary>v0.4.2</summary>

	- 修复选曲界面的多个问题及崩溃

	- 修复Tower难度崩溃，但未引入太鼓Tower菜单、LIFE管理和结算界面

</details>

<details>
	<summary>v0.4.1</summary>

	- 修复选曲界面的多个问题及崩溃

</details>

<details>
	<summary>v0.4.0</summary>

	- 引入EXAM5、6、7

	- 修复EXAM和数字间有空格导致的崩溃问题

	- 段位道场模块使用更好的代码结构

</details>

<details>
	<summary>v0.3.4.2</summary>

	- 段位道场选择界面中增加迷你角色

</details>

<details>
	<summary>v0.3.4.1</summary>

	- 修复人群动作速度的问题

</details>

<details>
	<summary>v0.3.4</summary>

	- 保存段位道场结果

	- 段位道场选择界面中增加成就展示板

</details>

<details>
	<summary>v0.3.3</summary>

	- 修复段位道场计量槽显示的问题

	- 为段位道场结算画面增加底板

</details>

<details>
	<summary>v0.3.2</summary>

	- 修复结果保存多次的问题

</details>

<details>
	<summary>v0.3.1</summary>

	- 修复P2得分排名不显示的问题

</details>

<details>
	<summary>v0.3.0</summary>

	- 在菜单显示迷你角色

	- 在Nameplate.json文件中，玩家可以分别选择各自的迷你角色

</details>

<details>
	<summary>v0.2.0</summary>

	- 修复选曲界面问题

	- 修复主菜单问题

</details>

<details>
	<summary>v0.1.0</summary>

	- 结算界面动画

</details>

## 致谢

> * [Takkkom/Major OpenTaiko features (1080p support, AI Battle mode, 5P mode and so on)](https://github.com/Takkkom)
> * [AkiraChnl/OpenTaiko图标](https://github.com/AkiraChnl)(@akirach_jp)
> * [cien/OpenTaiko标志/多项默认皮肤资源](https://twitter.com/CienpixeL)(@CienpixeL)
> * [funnym0th/OpenTaiko西班牙文翻译](https://github.com/funnym0th) (@funnym0th)
> * [basketballsmash/英文README翻译](https://twitter.com/basketballsmash)(@basketballsmash)
> * [Meowgister/OpenTaiko英文翻译](https://www.youtube.com/channel/UCDi5puZaJLMUA6OgIAb7rmQ)
> * [WHMHammer/OpenTaiko中文翻译](https://github.com/whmhammer)(@WHMHammer)
> * [Aioilight/TJAPlayer3](https://github.com/aioilight/TJAPlayer3)(@aioilight)
> * [TwoPointZero/TJAPlayer3](https://github.com/twopointzero/TJAPlayer3)(@twopointzero)
> * [KabanFriends/TJAPlayer3](https://github.com/KabanFriends/TJAPlayer3/tree/features)(@KabanFriends)
> * [Mr-Ojii/TJAPlayer3-f](https://github.com/Mr-Ojii/TJAPlayer3-f)(@Mr-Ojii)
> * [Akasoko/TJAPlayer3](https://github.com/Akasoko-Master/TJAPlayer3)(@AkasokoR)
> * [FROM/DTXMaina](https://github.com/DTXMania)(@DTXMania)
> * [Kairera0467/TJAP2fPC](https://github.com/kairera0467/TJAP2fPC)(@Kairera0467)
> * [touhourenren/TJAPlayer3-Develop-Rewrite](https://github.com/touhourenren)
