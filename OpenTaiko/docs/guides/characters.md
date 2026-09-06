<!-- guides/characters.md -->

# Adding a Character

A character is a folder under `Global/Characters/` in the game's install folder. Every subfolder the game finds there becomes one selectable character. A folder holds a `Metadata.json` (name, rarity, author), a `CharaConfig.txt` (positions and animation timing), the animation content, and optionally `Effects.json`, `Unlock.json`, `Palettes.json` and voice clips. The animation content is either folders of numbered PNG frames, which the game's built-in character script renders, or anything a per-character `Script.lua` chooses to draw (the shipped 3D template draws a glTF model).

Compatibility: OpenTaiko 0.6.1 still loads characters made for 0.6.0 without changes. This page describes the current layout; use it for new characters.

## Before you start

- OpenTaiko 0.6.1 installed. The game reads characters from `Global/Characters/` next to the game executable, and all skins share them.
- A text editor for JSON and INI-style files.
- For a 2D character: the art exported as numbered PNG frames (`0.png`, `1.png`, ...) with a transparent background, one folder per animation state.
- For a 3D character: a `model.glb` (binary glTF) containing the animation clips, and a still `Render.png`.
- The shipped `01 - Template` (2D) and `01 - Template3D` folders. Copy one of them as the starting point.

## Step 1: Understand discovery, order and identity

At boot the game lists the subfolders of `Global/Characters/` and creates one character per folder, in the order the file system returns them. The game does not sort the list, so the shipped folders carry a numeric prefix (`00 - None`, `01 - Template`, `02 - Student (A)`, ...) to keep the order predictable. Keep `00 - None` first: index 0 is the empty slot and the fallback when a saved character is missing.

Save files store the chosen character by folder name (`characterName`) and re-resolve it to an index on every boot. Adding or removing other folders never breaks a saved selection, but renaming a folder makes saves that referenced it fall back to `00 - None`. Two characters may share a display name; the folder name must be unique.

The game enumerates characters once at boot and again when the skin reloads; a folder added while the game is running appears after the next boot or skin reload.

## Step 2: Create the folder and Metadata.json

Create a folder such as `30 - MyChara` and add `Metadata.json`:

- `name`: display name. Either a plain string or a localized object `{ "strings": { "default": "...", "ja": "...", ... } }`. `default` is the fallback; the other keys are game language codes.
- `rarity`: one of `Poor`, `Common`, `Uncommon`, `Rare`, `Epic`, `Legendary`, `Mythical`. Rarity controls the colour and the unlock-notification tier only; every rarity has a coin multiplier of 1.
- `author`: plain string or localized object.
- `description`: optional, plain string or localized object.
- `speechtext`: optional array of six localized objects the results screen shows in the character's speech bubble. The game picks the entry by result, in this order: failed with a low gauge, failed with the gauge at 40% or more, cleared, cleared with a full gauge, full combo, all perfect. If you give fewer than six, the game repeats the last one.

If `Metadata.json` is missing the character still loads with name `(None)`, rarity `Common` and author `(None)`.

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

## Step 3 (2D path): Add the frame folders

When the folder has no `Script.lua`, the game renders the character with its built-in script (`CharaScript.lua` in the game's install folder). That script maps each animation state to a subfolder and loads `0.png`, `1.png`, `2.png`, ... from it. Loading stops at the first missing index, so numbering must be contiguous.

| Animation state | Folder |
|---|---|
| Game/Normal, Game/Clear, Game/Max | `Normal`, `Clear`, `Clear_Max` |
| Game/Gogo, Game/Gogo_Max | `GoGo`, `GoGo_Max` |
| Game/Miss, Game/Miss_Down | `Miss`, `MissDown` |
| Game/10combo, Game/10combo_Max | `10combo`, `10combo_Max` |
| Game/Cleared, Game/Failed | `Cleared`, `Failed` |
| Game/Clear_In, Game/Clear_Out | `Clearin`, `ClearOut` |
| Game/Max_In, Game/Max_Out | `Soulin`, `SoulOut` |
| Game/Miss_In, Game/Miss_Down_In, Game/Return | `MissIn`, `MissDownIn`, `Return` |
| Game/GoGoStart, Game/GoGoStart_Clear, Game/GoGoStart_Max | `GoGoStart`, `GoGoStart_Clear`, `GoGoStart_Max` |
| Game/Balloon_Breaking, Game/Balloon_Broke, Game/Balloon_Miss | `Balloon_Breaking`, `Balloon_Broke`, `Balloon_Miss` |
| Game/Kusudama_Breaking, Game/Kusudama_Broke, Game/Kusudama_Miss, Game/Kusudama_Idle | `Kusudama_Breaking`, `Kusudama_Broke`, `Kusudama_Miss`, `Kusudama_Idle` |
| Game/Tower/Standing, Climbing, Running, Clear, Fail (and the `_Tired` variants) | `Tower_Char/Standing`, `Tower_Char/Climbing`, `Tower_Char/Running`, `Tower_Char/Clear`, `Tower_Char/Fail` (plus `Tower_Char/Standing_Tired` and so on) |
| Menu/Wait, Menu/Start, Menu/Normal, Menu/Select | `Menu_Wait`, `Menu_Start`, `Menu_Loop`, `Menu_Select` |
| Entry/Normal, Entry/Jump | `Title_Normal`, `Title_Entry` |
| Result/Normal, Result/Clear, Result/Failed_In, Result/Failed | `Result_Normal`, `Result_Clear`, `Result_Failed_In`, `Result_Failed` |

The built-in script reads two still images from the folder root: `Render.png` (the full-size portrait, drawn wherever the game asks for the Render animation type, for example in the room) and `Preview.png` (the thumbnail; when absent, the script uses `Normal/0.png`).

Missing states fall back to another state, so a character can ship a subset. The fallback chain is: Clear -> Normal, Max -> Clear, Miss -> Normal, Miss_Down -> Miss, Gogo -> Normal, Gogo_Max -> Gogo, 10combo_Max -> 10combo, GoGoStart_Clear -> GoGoStart, GoGoStart_Max -> GoGoStart_Clear, the Tower `_Tired` states -> their normal state, Tower/Fail -> Tower/Standing_Tired, Kusudama_Idle -> Normal, Menu/Wait -> Gogo, Menu/Start and Menu/Select and Entry/Jump -> 10combo, Menu/Normal and Entry/Normal and Result/Normal -> Normal, Result/Clear -> Clear, Result/Failed_In -> Miss_In, Result/Failed -> Miss. States without a fallback (for example Cleared, Failed, Return, the balloon states) draw nothing when absent. The minimum for a working character is `Normal/0.png`.

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
  Sounds/                (optional voice clips, see Step 6)
```

## Step 4: Write CharaConfig.txt

`CharaConfig.txt` is a `Key=Value` text file; lines starting with `;` are comments. The built-in script reads these keys (the shipped 3D template reads the position keys as well):

- `Chara_Resolution=W,H` (default `1280,720`): the resolution you author the coordinates below against. The game scales positions from this resolution to the skin resolution at draw time.
- `Chara_LegacyMode` (default `1`): keeps the 0.6.0 anchoring and offset corrections. Characters ported from older versions rely on it.
- `Game_Chara_X=...` / `Game_Chara_Y=...`: gameplay position; the script uses the first value of each list. `Game_Chara_Offset=X,Y` is an alternative form.
- `Game_Chara_X_AI=...` / `Game_Chara_Y_AI=...`: one value per player for AI battle. When both keys are present they replace the skin's AI-battle position for this character.
- `Game_Chara_Balloon_X` / `Game_Chara_Balloon_Y`, `Game_Chara_Kusudama_X` / `Game_Chara_Kusudama_Y`: positions during balloon and kusudama sequences (first value used). `Game_Chara_Balloon_Offset`, `Game_Chara_Kusudama_Offset` and `Game_Chara_Tower_Offset` take an `X,Y` pair.
- `Menu_Offset=X,Y`, `Menu_Chara_Scale`, `Result_Offset=X,Y`, `Heya_Chara_Render_Offset=X,Y`: offsets for the menu, results and room render.
- `Game_Chara_Motion_<State>=0,1,2,...`: the order in which frames play for a state, as 0-based frame indices. When omitted the frames play in file order. State names follow the folder names, for example `Game_Chara_Motion_Normal`, `Game_Chara_Motion_GoGo`, `Game_Chara_Motion_Miss_Down`, `Game_Chara_Motion_Balloon_Broke`, `Game_Chara_Motion_Tower_Climbing`.
- `Game_Chara_Beat_<State>=N`: how many beats one loop of the state spans, for example `Game_Chara_Beat_Normal=1`, `Game_Chara_Beat_GoGo=2`.
- Menu, title and result states use `Menu_Chara_Motion_Loop/Wait/Start/Select`, `Title_Chara_Motion_Normal/Entry`, `Result_Chara_Motion_Normal/Clear/Failed_In/Failed`, with matching `_Beat_` keys or fixed durations in milliseconds: `Chara_Menu_Loop_AnimationDuration`, `Chara_Menu_Wait_AnimationDuration`, `Chara_Menu_Start_AnimationDuration`, `Chara_Menu_Select_AnimationDuration`, `Chara_Normal_AnimationDuration`, `Chara_Entry_AnimationDuration`, `Chara_Result_Normal_AnimationDuration`, `Chara_Result_Clear_AnimationDuration`, `Chara_Result_Failed_In_AnimationDuration`, `Chara_Result_Failed_AnimationDuration`.

The complete key list, with defaults, is the `load_chara_config_defs` table at the top of the built-in `CharaScript.lua`. The script ignores keys it does not know, so the shipped `01 - Template/CharaConfig.txt` also contains a few skin-side keys that have no effect in this file.

```ini
Chara_Version=0.6.1.0
Chara_Resolution=1920,1080

;Character X position (1P,2P)
Game_Chara_X=0,0
;Character Y position (1P,2P)
Game_Chara_Y=0,805

;Normal-state frame order and beats per loop
Game_Chara_Motion_Normal=0,1,2,3,4,5,6
Game_Chara_Beat_Normal=1

;GoGo frame order and beats per loop
Game_Chara_Motion_GoGo=0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15
Game_Chara_Beat_GoGo=2
```

## Step 5 (3D path): Ship model.glb and a per-character Script.lua

When `Script.lua` exists in the character folder it replaces the built-in script entirely. The game then calls these global functions by name:

- `loadAnimation(animationType)`, `disposeAnimation(animationType)`
- `availableAnimation(animationType)` returning a boolean. The game still accepts the older misspelling `avaialbeAnimation`: it tries `availableAnimation` first and falls back to `avaialbeAnimation`. The shipped 3D template still uses the old name.
- `setAnimationDuration(animationType, durationMs)`, `resetAnimationCounter(animationType)`
- `update(delta, animationType, looping)` returning `true` when a non-looping animation has finished
- `draw(animationType, x, y, scaleX, scaleY, opacity, color, contextType, anchor, clipW, clipH, clipX, clipY, rotation, blendMode, wrapMode, gradientMap)`
- `getDrawSize(animationType)` returning width and height
- `getHeyaRenderOffset()` returning x and y; `getAIBattlePosition(player, charaScale)` returning x and y, or `nil` to use the skin's position
- `loadVoice(voiceType)`, `disposeVoice(voiceType)`, `playVoice(voiceType)`

Animation types are the strings behind the `CHARACTER.ANIM_*` constants (`"Game/Normal"`, `"Menu/Normal"`, ...), plus the two special types `CHARACTER.ANIM_PREVIEW` (thumbnail) and `CHARACTER.ANIM_RENDER` (full portrait). Voice types are the `CHARACTER.VOICE_*` constants. The fallback chain from Step 3 applies to scripted characters too: the game asks `availableAnimation` and walks the alternatives until one is available.

The shipped `01 - Template3D` folder contains only `CharaConfig.txt`, `Effects.json`, `Metadata.json`, `model.glb`, `Render.png` and `Script.lua`. Its script loads `model.glb` with `MODEL:Load`, renders it into a scene it creates with `SCENE3D:CreateScene`, reads the position keys of `CharaConfig.txt`, and maps every animation type to a clip index and a beat count in a `CLIP` table. To make a 3D character, copy the folder, replace `model.glb` and `Render.png`, and edit `CLIP` so each type points at the right clip index of your model.

```lua
-- excerpt from 01 - Template3D/Script.lua
local CLIP = {
  [CHARACTER.ANIM_GAME_NORMAL] = { clip = 0,  beat = 1 };
  [CHARACTER.ANIM_GAME_CLEAR]  = { clip = 1,  beat = 1 };
  [CHARACTER.ANIM_GAME_GOGO]   = { clip = 2,  beat = 4 };
  [CHARACTER.ANIM_MENU_NORMAL] = { clip = 14, beat = 2 };
  [CHARACTER.ANIM_RESULT_CLEAR]= { clip = 19, beat = 1 };
  -- one entry per animation state the model supports
}

function loadAnimation(animationType)
  -- build the clip / preview / render data and mark it available
end

function availableAnimation(animationType)
  return animations[animationType] ~= nil
end
```

## Step 6: Optional files: Effects.json, Unlock.json, Palettes.json, voices

- `Effects.json`: `gauge` (`Normal`, `Hard` or `Extreme`; default `Normal`) selects the soul gauge type. `Hard` multiplies coin gains by 1.5 and `Extreme` by 1.8 unless the game forces the normal gauge. When the Minesweeper fun mod is active, `bombFactor` (1-100, default 20) is the percentage of notes the mod turns into bombs and `fuseRollFactor` (0-100, default 0) the percentage of balloons it turns into fuse rolls.
- `Unlock.json`: when present, the character stays locked until the player meets the condition. The format and condition ids match the ones for songs; see the unlockables guide. The player buys coin conditions in the room screen; the game checks the other conditions automatically on the results screen. Shipped examples: Kuro uses `{ "condition": "dp", "type": "me", "values": [3, 3, 10] }` (ten clears of Extreme charts with full combo or better) and Aoi uses `{ "condition": "ch", "type": "me", "values": [200] }` (200 coins).
- `Palettes.json`: an array of colour palettes the player can apply to the character. Each entry has `name`, `blend` (0-1), `stops` (an array of `[position, R, G, B]` or `[position, R, G, B, A]` gradient stops; give at least two) and `plays`, the number of plays with this character that unlock the palette (0 or absent means available immediately). An entry with `"stops": null` is the untinted default.
- Voices: the built-in script loads `.ogg` files from fixed paths inside the character folder, for example `Sounds/Clear/Clear.ogg`, `Sounds/Clear/Failed.ogg`, `Sounds/Clear/FullCombo.ogg`, `Sounds/Clear/AllPerfect.ogg`, `Sounds/Menu/SongSelect.ogg`, `Sounds/Menu/SongDecide.ogg`, `Sounds/Menu/DiffSelect.ogg`, `Sounds/Title/Sanka.ogg`, `Sounds/Result/BestScore.ogg`, `Sounds/Result/ClearSuccess.ogg`, `Sounds/Result/ClearFailed.ogg`. The full list is the `voice_files` table at the top of the built-in `CharaScript.lua`. The script skips missing files.

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

## Step 7: Restart and select the character

Restart the game (or reload the skin from the settings). The character appears in the room screen's character list, where locked characters show their unlock condition. Lua stages can also read the list through the `CHARACTERLIST` global, which exposes each entry's folder name, display name, rarity and unlock condition.

## Troubleshooting and notes

- The character does not appear: check that the folder is directly under `Global/Characters/` and restart the game. The game builds the list once at boot.
- The character draws nothing: `Normal/0.png` is missing, or the folder names do not match the table in Step 3. Frames must be named `0.png`, `1.png`, ... with no gaps; a gap ends the animation at that index without an error.
- The character is off-screen or the wrong size: `Chara_Resolution` must match the resolution you authored the position values for. When the key is absent the game assumes `1280,720`.
- Only part of the animation set plays: states without a fallback (Cleared, Failed, Return, Balloon and Kusudama states) need their own folder.
- A 3D character shows every animation as unavailable: `Script.lua` must define `availableAnimation` (or `avaialbeAnimation`) and return `true` for the loaded types.
- A present `Script.lua` replaces the built-in script completely. A scripted character can still load numbered PNG folders, but only if the script loads them itself.
- Saves reference the folder name, so renaming a folder that players have already selected resets their selection to the empty slot.
- The shipped JSON files contain trailing commas. The game's JSON parser accepts them; strict validators reject them.
