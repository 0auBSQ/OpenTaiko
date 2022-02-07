# New Commands

- This document describes the new commands this simulator adds to tja files and `box.def`.

## Tower

- `LIFE` : (Int) Life count in-game, missing a note takes you one and triggers 2 seconds of invincibility frames. (Default : 5)

- `TOWERTYPE` : (Int) Tower skin to use in-game (`5_Game\20_Tower\Tower_Floors`) and within the result screen (`8_TowerResult\Tower`). (Default : 0)

## Dan 

- `DANTICK` : (Int between `[0, 5]`) Upper dan tick skin to use in the Dan chart select screen. (Default : 0)

- `DANTICKCOLOR` : (Hex) Color filter to apply on the dan tick, works especially well using a grayscale default texture.

- `EXAM(x):a,(y),(z),m` : (With (x) EXAM number between `]1, 7]`¹, (y) and (z) respectively red and gold pass criterias) Accuracy exam type, in percent. 

¹: `EXAM1` slot is gauge exclusive.
 
## box.def

- `BOXCOLOR` : (Hex) Color filter to apply on the box (Ensou song select screen).

- `BGCOLOR` : (Hex) Color filter to apply on the background (Ensou song select screen).

- `BACKCOLOR` : (Hex) Color of the text outline (Ensou song select screen).

- `FRONTCOLOR` : (Hex) Text color (Ensou song select screen).

- `BGTYPE` : (Int) Texture to use for the background (`3_SongSelect\Genre_Background`, Ensou song select screen, Default : Genre id or 0).

- `BOXTYPE` : (Int) Texture to use for the boxes (`3_SongSelect\Bar_Genre` and `3_SongSelect\Difficulty_Select\Difficulty_Back`, Ensou song select screen, Default : Genre id or 0).

- `BOXCHARA` : (Int) Texture to use for the boxes' characters (`3_SongSelect\Box_Chara`, Ensou song select screen, Default : Genre id or 0).