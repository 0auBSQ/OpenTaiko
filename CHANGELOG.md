# Changelog

## [0.6.0.39] - 2025-01-16 (Beta)

- The default font for most languages (when a skin doesn't include a font for that language) has been set to Noto Sans JP
- The font size for the Offset text on the Calibration screen can now be adjusted
- Fixed the user rename screen being off-center for resolutions other than 1920x1080
- Lua security update
- The default font for most languages (when a skin doesn't include a font for that language) has been set to Noto Sans JP
- The font size for the Offset text on the Calibration screen can now be adjusted
- Fixed the user rename screen being off-center for resolutions other than 1920x1080
- Lua security update

## [0.6.0.38] - 2025-01-11 (Beta)

- Correctly load clear sounds & pan according to player count
- Adjust Korean translations (AsPho)
- Correctly load clear sounds & pan according to player count
- Adjust Korean translations (AsPho)

## [0.6.0.37] - 2025-01-07 (Beta)

- [Fix] Saves.db3 failures no longer cause crashes
- [Fix] Prevent crashes if FX Script.lua has Lua errors
- [Fix] Gameplay debug info now shows at the right place
- [Feat] LogNotification messages now show on screen top (FX and BG Lua script error, preimage-not-found warning, complex number format warning, modal-window-not-closed error, TJA parsing error)
- [Feat] BG Lua script errors are now logged
- [Feat] TJA parsing errors are now logged and as warnings.
- [Feat] TextConsole messages now respect to font height
- [Fix] Saves.db3 failures no longer cause crashes
- [Fix] Prevent crashes if FX Script.lua has Lua errors
- [Fix] Gameplay debug info now shows at the right place
- [Feat] LogNotification messages now show on screen top (FX and BG Lua script error, preimage-not-found warning, complex number format warning, modal-window-not-closed error, TJA parsing error)
- [Feat] BG Lua script errors are now logged
- [Feat] TJA parsing errors are now logged and as warnings.
- [Feat] TextConsole messages now respect to font height

## [0.6.0.36] - 2025-01-06 (Beta)

- Fix "all your best were belong to the last player"
- Fix "all your best were belong to the last player"

## [0.6.0.35] - 2025-01-05 (Beta)

- Add Trigger & Thumbstick support for Gamepads
- Add Trigger & Thumbstick support for Gamepads

## [0.6.0.34] - 2025-01-05 (Beta)

- Support for custom song unlockable text
- No longer display "???" as unlockable message for Epic rarity assets
- Support for custom song unlockable text
- No longer display "???" as unlockable message for Epic rarity assets

## [0.6.0.33] - 2025-01-03 (Beta)

- Add Chapiter 4 and NEP03/4 unlocks
- Add Chapiter 4 and NEP03/4 unlocks

## [0.6.0.32] - 2025-01-02 (Beta)

- Allow input devices to be recognized after plugging in past startup
- Fix joystick devices failing to send inputs (Fixes tatacons)
- Unsupported keyboard key inputs no longer crash the game
- Devices being plugged/unplugged are now logged
- Allow input devices to be recognized after plugging in past startup
- Fix joystick devices failing to send inputs (Fixes tatacons)
- Unsupported keyboard key inputs no longer crash the game
- Devices being plugged/unplugged are now logged

## [0.6.0.31] - 2024-12-30 (Beta)

- Change OpenTaiko.log's encoding to UTF-8
- Fallback to SkiaSharp's default typeface if all other methods fail
- Log Egl related errors

## [0.6.0.30] - 2024-12-22 (Beta)

- Set default Graphics Device type on first boot & only show supported Graphics Devices per OS

## [0.6.0.29] - 2024-12-19 (Beta)

- Adds a Search by Text new folder with a similar appearance to Search by Difficulty (Case-insensitive)

## [0.6.0.28] - 2024-12-12 (Beta)

- Russian text of Fix Input Timestamp Inaccuracy
- Chinese text minor changes and lining
- Text revision reminders in other languages
- Japanese text fixes
- Korean text of unlock conditions in playing, punctuations
- Make Dutch text JSON line index consistent

## [0.6.0.27] - 2024-12-09 (Beta)

- [Feat] Make bar drumrolls stretchable
- [Feat] Detect and hide screen-obscuring bar drumrolls when any tips are out of screen
- [Fix] fix bar drumroll end stuck at judgement mark if occurred at 0ms since #START
- [Fix] fix fuze rolls stretched back when exploded
- [Fix] fix balloon-type notes not stayed on judgement mark vertically during their duration
- [Fix] fix wrong drawn position of rotated bar-type roll tails

## [0.6.0.26] - 2024-12-08 (Beta)

- Fix Buffered Inputs
- Fix input lag on VSync
- Fix inputs being fps-restricted

## [0.6.0.25] - 2024-12-06 (Beta)

- Prevent CCounter from halting due to very large BPM

## [0.6.0.24] - 2024-12-04 (Beta)

- Remove broken DTX legacy beatline chips processing (0x51)

## [0.6.0.23] - 2024-12-02 (Beta)

- Fix end of bar drumrolls wrongly snapped to 1/16th (only visually) in HB/BMScroll
- Fix incorrect roll duration, missing rolls, or unended chart for rolls across branch sections
- Force unended roll to end at the first non-roll note after it or #END

## [0.6.0.22] - 2024-12-01 (Beta)

- Reset bar drumroll redness on retry or training-mode pause

## [0.6.0.21] - 2024-11-18 (Beta)

- Fix confirm/cancel keyboard inputs ending name change early

## [0.6.0.20] - 2024-11-16 (Beta)

- [i18n] Changing Name Menu in Chinese and Russian

## [0.6.0.19] - 2024-11-15 (Beta)

- Allow users to change their name via. Heya menu

## [0.6.0.18] - 2024-11-15 (Beta)

- Add Chinese and Russian Instrument Names

## [0.6.0.17] - 2024-11-12 (Beta)

- Make HitSounds.json per-folder instead of one central file
- Initialize new JSON file if one does not exist in that folder, using folder name as Hitsound name
- Add localization support for Hitsounds

## [0.6.0.16] - 2024-11-04 (Beta)

- Readable error message on screen instead of crashes before the startup screen (Missing skin, no audio device found, etc)
- Few code translation from japanese to english
- Minor refactoring on the stage change code to avoid repetitions (OpenTaiko.cs)

## [0.6.0.15] - 2024-11-02 (Beta)

- Korean translation by AsPho

## [0.6.0.14] - 2024-11-02 (Beta)

- Additional Nameplate titles

## [0.6.0.13] - 2024-11-01 (Beta)

- Unlockables with fulfilled conditions can be obtained directly on the select screen/my room by Decide input

## [0.6.0.12] - 2024-10-31 (Beta)

- Fix TJA not inserting timing space for `,`-only measures

## [0.6.0.11] - 2024-10-31 (Beta)

- Fix TJA newline after last note symbol and before `,` caused wrong note-symbols-per-measure calculation

## [0.6.0.10] - 2024-10-30 (Beta)

- Fix Softlocked in difficulty selection when any non P1 player has no key binds     * Now menu keys control the lowerest index operable player
- Fix P1 could have only use menu keys but not drum keys, if the key binds differ     * Now both work
- Fix Up and Down arrow keys controlled multiple players at once in gameplay option     * Now menu keys only control the lowerest index operable player

## [0.6.0.9] - 2024-10-29 (Beta)

- Fix crash when exiting title screen by Esc

## [0.6.0.8] - 2024-10-29 (Beta)

- Additional Chinese and Russian translations

## [0.6.0.7] - 2024-10-29 (Beta)

- Fix judge text drop frame when prev one remove

## [0.6.0.6] - 2024-10-28 (Beta)

- Add optional offset values for characters on menus and result screen

## [0.6.0.5] - 2024-10-28 (Beta)

- Fix big note judgement preventing early BAD of big notes in Easy window

## [0.6.0.4] - 2024-10-27 (Beta)

- Deprecate unused textures
- Fix tall preimages not being scaled correctly
- Fix Sort Songs menu not updating lang values

## [0.6.0.3] - 2024-10-27 (Beta)

- Fix wrong roll speed in non-1x play speed

## [0.6.0.2] - 2024-10-27 (Beta)

- Fix crash due to undefined mob, dancer, & runner

## [0.6.0.1] - 2024-10-27 (Beta)

- Fix Mob, Dancer, Runner appearing despite empty array on the scene preset
- Chapter IV nameplates and make Chapter III all-songs nameplates accessible (condition changed from 999 songs to 34)
- Japanese translations for some nameplates

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
