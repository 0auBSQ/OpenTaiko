<p align="center">
  <img src="https://user-images.githubusercontent.com/58159635/140600257-f712fc48-d09a-4a5e-a78d-e7c65ca19b80.png">
</p>

日本語 : https://github.com/0auBSQ/OpenTaiko/blob/main/README-JA.md

中文：https://github.com/0auBSQ/OpenTaiko/blob/main/README-ZH.md

# OpenTaiko

Old TJAPlayer3-Develop-BSQ, a spiritual successor to TJAPlayer3.

- Current version： Pre v0.6.0 b2

- Discord : https://discord.gg/aA8scTvZ6B
- Japanese Discord : https://discord.gg/CJ4nTkpy7t

## Cautions before using （IMPORTANT）

- It is **YOUR RESPONSIBILITY** to use this software. The creator will not take responsibilities for any problems you got from using this software.

- Please research before asking people.

- If your PC can not maintain 60fps stably, it will not work well with the software.

- There will be no support other than the version listed above. If you are using the pre-release on the releases page, do note it is a testing release, and will not take responsibility for any problem caused using this release.

### Using this software on streams and videos

If there is an tag feature on the website you are using, tagging it as "OpenTaiko", "TJAPlayer3", or "TJAP3" will avoid confusion, and might raise video as similar content, so it is highly recommended.

The author of this software does not support breaking copyright laws, so please follow as per your country's copyright laws.
Additionally, the OpenTaiko team is strongly against the secondary distribution of skins trying to reproduce specific commercial video games.

### Editing source code/Secondary distribution

OpenTaiko is open source code with MIT license.
Under the MIT license, you are allowed to make edits, or secondary distribute, however this is all **YOUR RESPONSIBILITY**.
Also, under the used library's licenses, **PLEASE** include the "License" folder when editing or redistributing it.
Please follow the creators' licenses and rules for other skins or song packages.
OpenTaiko license does not apply in this case.

### Goals/Non-goals

**GOALS**

- Multiple fun ways to play Taiko.

- More customization for better skinning potential, and make "Everyone could play Taiko easily at their own style" true.

- Optimizations, bug fixes, and QOL features.

**NON-GOALS**

- Copying other games/commercial licenses accurately.

## Rules for posting Issues/Pull requests

Thank you for posting to Issue/Pull Request. It is very much appreciated.

- **PLEASE** follow Japan and France copyright laws in the post.

- **IMPORTANT**: Once you post an issue, please write the release version and recreation steps. If it is a crash, please attach the TJAPlayer3.log file.

- If you want CLang translations, please contact the software author on Discord beforehand.

### Feature requests

If you want a feature to be added please contact me on Discord beforehand.

If the feature request is good it might be added.

- **IMPORTANT**: Feature requests such as "Please recreate UI/UX as per following AC Nijiiro Version" will be denied and left without answer.

## Q＆A

- The song difficulty on the dan-i select screen is all Oni 10 stars!

```
In the .tja file, please add ",(Difficulty),(Course)" in the #NEXTSONG line.

Example：

Old： #NEXTSONG [TITLE],[SUBTITLE],[GENRE],[WAVE],[SCOREINIT],[SCOREDIFF]

New： #NEXTSONG [TITLE],[SUBTITLE],[GENRE],[WAVE],[SCOREINIT],[SCOREDIFF],[LEVEL],[COURSE]
```

- I can not go past the entry screen.

```
Hold the P key.
```

- I found a bug, what should I do?

```
Please make a new Issue if you find a bug.
```

- I cannot find the characters/puchicharas I added

```
From version 0.5.3.1, Characters and Puchicharas are loaded from the Global folder (outside of skins), please add them there.
```

## Update history

<details>
	<summary>v0.5.4</summary>

	- Fix multiple bugs

	- Online chart downloading via the Online Lounge

	- Voice support for characters and puchicharas

	- Multiple in-game hitsounds support

	- Context box for Random song select

	- Konga gamemode

	- PREIMAGE metadata support

	- Rework of in-game modifiers and modicons

	- Purple notes (G), Bomb notes (C) and fix Joined notes (A and B) and ADLIBs (F)

</details>

<details>
	<summary>v0.5.3.1</summary>

	- Fix multiple bugs

	- Global characters and puchicharas

	- Permanent recently played songs folder

	- Easy/Normal timing zones

	- Characters on menus and result screens

	- Song search by difficulty feature

</details>

<details>
	<summary>v0.5.3</summary>

	- Fix multiple bugs

	- 1st version of the Dan result screen

	- Dan chart supporting any count of songs

	- Support of 2P Side

	- Major 2P update (Please check discord for more details about it)

	- Dan charts are now also selectable from the Taiko mode song select screen

	- Add Modals

	- 1st unlockables update

	- Add Favorite songs folder

	- Add Database files (Name and Author names for Characters and Puchichara)

	- Chinese language support (WHMHammer)

	- Remove SlimDX dependencies (Mr Ojii)

	- Add SimpleStyle skin (feat. cien)

	- Automatically generated unique ID addition for each song

	- Fix Discord RPC

	- Fix several config options issues (l1m0n3)

</details>

<details>
	<summary>v0.5.2.1</summary>

	- Fix multiple bugs

	- Add multiple levels of AI in addition of Auto

	- Add Global offset

	- Replace AUTO ROLL by Rolls speed

</details>

<details>
	<summary>v0.5.2</summary>

	- Taiko Heya features

	- Custom nameplates and character feature

	- Make medals obtainable

	- Make dan-i title unlockable

	- Add multiple step textures

	- Add Spanish translation (funnym0th)

	- Add "Random option"

	- UX/UI improvements

	- Fast song loading

	- Fix branched charts

</details>

<details>
	<summary>v0.5.1</summary>

	- Add animations to dan-i dojo

	- Add game end screen and icons

	- Bug fix

	- Multiple language support

	- UI improvements

	- Multiple layouts of song select screen

</details>

<details>
	<summary>v0.5.0</summary>

	- Taiko Tower features (Background+Result screen backbone)

	- "TOWERTYPE" in Tower charts (USe multiple skins for playing Towercharts)

	- Add accuracy exam in dan-i dojo

	- Add "#BOXCOLOR", "#BOXTYPE", "#BGCOLOR", "#BGTYPE", "#BOXCHARA in box.def

</details>

<details>
	<summary>v0.4.3</summary>

	- Add Taiko Tower (Gameplay)

</details>

<details>
	<summary>v0.4.2</summary>

	- Fix multiple bug and crash on song select screen

	- Fix COURSE:Tower crashes, however Taiko Tower menu, LIFE management, and result screen is not implemented yet.

</details>

<details>
	<summary>v0.4.1</summary>

	- Fix multiple bug and crashes on song select screen

</details>

<details>
	<summary>v0.4.0</summary>

	- EXAM5, 6, 7 implementation

	- Fix crash with EXAM numbers having spaces between

	- Better code structuring on Dan-i dojo

</details>

<details>
	<summary>v0.3.4.2</summary>

	- Add petit-chara on Dan-i select screen

</details>

<details>
	<summary>v0.3.4.1</summary>

	- Fix bug with Mob animation speed

</details>

<details>
	<summary>v0.3.4</summary>

	- Save dan-i dojo results

	- Add achievement plate on dan-i select screen

</details>

<details>
	<summary>v0.3.3</summary>

	- Fix dan-i dojo gauge appearance

	- Add backbone for dan-i dojo result screen

</details>

<details>
	<summary>v0.3.2</summary>

	- Fix results saving multiple time

</details>

<details>
	<summary>v0.3.1</summary>

	- Fix P2 scorerank not showing

</details>

<details>
	<summary>v0.3.0</summary>

	- Show petit-chara in menu

	- In Nameplate.json file players could select petit-chara separately

</details>

<details>
	<summary>v0.2.0</summary>

	- Fix song select screen bug

	- Fix main menu bugs

</details>

<details>
	<summary>v0.1.0</summary>

	- Result screen animation

</details>

## Credits

> * [Takkkom/Major OpenTaiko features (1080p support, AI Battle mode, 5P mode and so on)](https://github.com/Takkkom)
> * [AkiraChnl/OpenTaiko Icon](https://github.com/AkiraChnl)(@akirach_jp)
> * [Reichisama/OpenTaiko 0.6.0 Icon](https://twitter.com/himikoreichi135)(@himikoreichi135)
> * [cien/OpenTaiko Logo/Various Default Skin Assets](https://twitter.com/CienpixeL)(@CienpixeL)
> * [funnym0th/OpenTaiko Spanish Translation](https://github.com/funnym0th) (@funnym0th)
> * [basketballsmash/English README Translation](https://twitter.com/basketballsmash)(@basketballsmash)
> * [Meowgister/OpenTaiko English Translation](https://www.youtube.com/channel/UCDi5puZaJLMUA6OgIAb7rmQ)
> * [WHMHammer/OpenTaiko Chinese Translation](https://github.com/whmhammer)(@WHMHammer)
> * [Aioilight/TJAPlayer3](https://github.com/aioilight/TJAPlayer3)(@aioilight)
> * [TwoPointZero/TJAPlayer3](https://github.com/twopointzero/TJAPlayer3)(@twopointzero)
> * [KabanFriends/TJAPlayer3](https://github.com/KabanFriends/TJAPlayer3/tree/features)(@KabanFriends)
> * [Mr-Ojii/TJAPlayer3-f](https://github.com/Mr-Ojii/TJAPlayer3-f)(@Mr-Ojii)
> * [Akasoko/TJAPlayer3](https://github.com/Akasoko-Master/TJAPlayer3)(@AkasokoR)
> * [FROM/DTXMaina](https://github.com/DTXMania)(@DTXMania)
> * [Kairera0467/TJAP2fPC](https://github.com/kairera0467/TJAP2fPC)(@Kairera0467)
> * [touhourenren/TJAPlayer3-Develop-Rewrite](https://github.com/touhourenren)
