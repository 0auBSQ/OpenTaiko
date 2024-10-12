# Changelog

## [0.6.0.0] - 2024-10-26 (Beta)

The first Beta version of OpenTaiko, including multiple structural changes!

(Note: The following logs also include all changes made on all 3 Pre 0.6.0 pre-releases (b1, b2 and b3))

### Major changes

- AI Battle Mode (b1)
- Regular Taiko mode playable up to 5 players (b1)
- 1080p (and any other resolution) support on skins (b1)
- Wider skinning support (b1)
- Port to .NET 7, including OpenGL support (b2)

### New features

- Kusudama support (9) (b1)
- Fuseroll support (D) (b1)
- Taiko Towers base select screen (b2)
- Diagonal/Vertical rolls support (b2)
- Wider STAGEPRESET support (b2)
- VTT lyric support (b2)
- Chart caching and faster song loading (b2)
- Folder system for Dan charts (b2)
- Character effects support (b2)
- Hard and Extreme gauges (b2)
- Major bug fixes
- Multiple UI/UX improvements
- Add extended .lua support including currently 2 new lua modules (Nameplates and Modals)
- Add support for unlockable songs
- Extended unlockables conditions support
- German translation from [@Morphclue](https://github.com/Morphclue)
- Russian translation from [@ExpedicHabbet](https://github.com/ExpedicHabbet).
- Dutch translation from [@ugyuu](https://www.youtube.com/@ugyuu).

### Changed features

- Fix easier gauges to have their actual clear percent (b2)
- Chart of 11 stars or more now have their special gauge based on clear % instead of fill/drop speed (b2)
- Deprecate score.ini files and move all scores to .sqlite3 databases

### Removed features

- Deprecate the "Open Encyclopedia" menu as it is deprecated and will be progressively replaced for html based documentation.

## [0.5.4.0] - 2022-07-01 (Alpha)

- Fix multiple bugs
- Online chart downloading via the Online Lounge
- Voice support for characters and puchicharas
- Multiple in-game hitsounds support
- Context box for Random song select
- Konga gamemode
- PREIMAGE metadata support
- Rework of in-game modifiers and modicons
- Purple notes (G), Bomb notes (C) and fix Joined notes (A and B) and ADLIBs (F)

## [0.5.3.1] - 2022-04-06 (Alpha)

- Fix multiple bugs
- Global characters and puchicharas
- Permanent recently played songs folder
- Easy/Normal timing zones
- Characters on menus and result screens
- Song search by difficulty feature

## [0.5.3.0] - 2022-03-09 (Alpha)

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

## [0.5.2.1] - 2021-12-04 (Alpha)

- Fix multiple bugs
- Add multiple levels of AI in addition of Auto
- Add Global offset
- Replace AUTO ROLL by Rolls speed

## [0.5.2.0] - 2021-11-26 (Alpha)

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

## [0.5.1.0] - 2021-11-04 (Alpha)

- Add animations to dan-i dojo
- Add game end screen and icons
- Bug fix
- Multiple language support
- UI improvements
- Multiple layouts of song select screen

## [0.5.0.0] - 2021-10-24 (Alpha)

The first public Alpha version of OpenTaiko!

- Taiko Tower features (Background+Result screen backbone)
- "TOWERTYPE" in Tower charts (USe multiple skins for playing Towercharts)
- Add accuracy exam in dan-i dojo
- Add "#BOXCOLOR", "#BOXTYPE", "#BGCOLOR", "#BGTYPE", "#BOXCHARA in box.def

## [0.4.3.0] (Closed Alpha)

- Add Taiko Tower (Gameplay)

## [0.4.2.0] (Closed Alpha)

- Fix multiple bug and crash on song select screen
- Fix COURSE:Tower crashes, however Taiko Tower menu, LIFE management, and result screen is not implemented yet.

## [0.4.1.0] (Closed Alpha)

- Fix multiple bug and crashes on song select screen

## [0.4.0.0] (Closed Alpha)

- EXAM5, 6, 7 implementation
- Fix crash with EXAM numbers having spaces between
- Better code structuring on Dan-i dojo

## [0.3.4.2] (Closed Alpha)

- Add petit-chara on Dan-i select screen

## [0.3.4.1] (Closed Alpha)

- Fix bug with Mob animation speed

## [0.3.4.0] (Closed Alpha)

- Save dan-i dojo results
- Add achievement plate on dan-i select screen

## [0.3.3.0] (Closed Alpha)

- Fix dan-i dojo gauge appearance
- Add backbone for dan-i dojo result screen

## [0.3.2.0] (Closed Alpha)

- Fix results saving multiple time

## [0.3.1.0] (Closed Alpha)

- Fix P2 scorerank not showing

## [0.3.0.0] (Closed Alpha)

- Show petit-chara in menu
- In Nameplate.json file players could select petit-chara separately

## [0.2.0.0] (Closed Alpha)

- Fix song select screen bug
- Fix main menu bugs

## [0.1.0.0] (Closed Alpha)

- Result screen animation