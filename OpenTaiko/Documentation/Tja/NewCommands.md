# New Commands

- This document describes the new commands this simulator adds to tja files and `box.def`.

Current version : v0.5.4

## Notes

- `A` : Partner big Don (Equivalent to `3`)

- `B` : Partner big Ka (Equivalent to `4`)

- `C` : Bomb/Mine, hitting it breaks the combo and substracts 4% to the soul gauge

- `F` : ADLIB invisible note, has no effect on score nor combo but adds 1 to the count of coins earned at the end of the game (+10 coins max per game)

- `G` : (Taiko) Purple note, require to hit both Ka and Don (Konga) Pink note (Equivalent to `A` and `3`)

## General Metadata

- `PREIMAGE` : ((Relative) Path) Song cover jacket image 

## Tower

- `LIFE` : (Int) Life count in-game, missing a note takes you one and triggers 2 seconds of invincibility frames. (Default : 5)

- `TOWERTYPE` : (Int) Tower skin to use in-game (`5_Game\20_Tower\Tower_Floors`) and within the result screen (`8_TowerResult\Tower`). (Default : 0)

## Dan 

- `DANTICK` : (Int between `[0, 5]`) Upper dan tick skin to use in the Dan chart select screen. (Default : 0)

- `DANTICKCOLOR` : (Hex) Color filter to apply on the dan tick, works especially well using a grayscale default texture.

- `EXAM(x):a,(y),(z),m` : (With (x) EXAM number between `]1, 7]`(1), (y) and (z) respectively red and gold pass criterias) Accuracy exam type, in percent. 

(1): `EXAM1` slot is gauge exclusive.

### Extra Exams

- `a` : Accuracy, in percent

- `jm` : Judge mines, number of mines hit

- `ja` : Judge ADLIBs, number of ADLIBs hit
 
## box.def

- `BOXCOLOR` : (Hex) Color filter to apply on the box (Ensou song select screen).

- `BGCOLOR` : (Hex) Color filter to apply on the background (Ensou song select screen).

- `BACKCOLOR` : (Hex) Color of the text outline (Ensou song select screen).

- `FRONTCOLOR` : (Hex) Text color (Ensou song select screen).

- `BGTYPE` : (Int) Texture to use for the background (`3_SongSelect\Genre_Background`, Ensou song select screen, Default : Genre id or 0).

- `BOXTYPE` : (Int) Texture to use for the boxes (`3_SongSelect\Bar_Genre` and `3_SongSelect\Difficulty_Select\Difficulty_Back`, Ensou song select screen, Default : Genre id or 0).

- `BOXCHARA` : (Int) Texture to use for the boxes' characters (`3_SongSelect\Box_Chara`, Ensou song select screen, Default : Genre id or 0).