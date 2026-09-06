<!-- api/players.md -->

# Players and profiles

Save files, nameplates, characters, puchicharas, play state, themes and the current language.

Player indices are 0-based (0 to 4) everywhere on this page except THEME:GetThemeSettingForPlayer, which is 1-based. Read-only modules (ROActivities and backgrounds) receive a save-file handle whose write methods log an error and do nothing; everything else here behaves the same in every module type.

## Save files

### GetSaveFile

Global function returning the save-file handle of a player slot.

<div class="callout warn">
Call it as a plain function (`GetSaveFile(0)`). An out-of-range index logs an error and returns nil. Each call creates a fresh handle that reads live data, so there is nothing to cache or dispose.
</div>

| Method | Description |
| --- | --- |
| `GetSaveFile(player)  -> saveFile` | Returns the save-file handle for the 0-based player slot, or nil if the index is out of range. |

### Save file handle

The profile of one player: name, coins, unlocked items, triggers and counters, clear statistics, equipped character, puchichara, nameplate and dan title.

<div class="callout warn">
Read properties with dot syntax (sf.Name, sf.Coins). Write methods persist immediately. Read-only modules block the following: SpendCoins, EarnCoins, UnlockNameplate, UnlockSong, assigning SelectedHitsounds, SetGlobalTrigger, SetGlobalCounter, ChangeCharacter (returns false), UnlockPuchichara, ChangePuchichara, UnlockCharacter, ChangeDan, ChangeName and ChangeNameplate.
</div>

| Method | Description |
| --- | --- |
| `sf.Name  -> string` | The player's displayed name. |
| `sf.SaveId  -> integer` | The numeric database id of this save. |
| `sf.SaveUID  -> string` | The unique string id of this save. |
| `sf.NameplateInfo  -> nameplateInfo` | The equipped nameplate (see Nameplate info handle), or the default beginner nameplate if the stored id is unknown. |
| `sf.DanplateInfo  -> danplateInfo` | The current dan title (see Danplate info handle). |
| `sf.TotalPlaycount  -> integer` | Total number of plays on this save. |
| `sf.AIBattlePlaycount  -> integer` | Number of AI battle plays. |
| `sf.AIBattleWins  -> integer` | Number of AI battle wins. |
| `sf.Coins  -> integer` | Current coin balance. |
| `sf.TotalEarnedCoins  -> integer` | Total coins earned over the life of the save. |
| `sf:SpendCoins(price)  -> nil` | Deducts coins (the balance never goes below 0) and persists. |
| `sf:EarnCoins(amount)  -> nil` | Adds coins to the balance and to the total earned, and persists. |
| `sf:IsNameplateUnlocked(id)  -> bool` | Whether the nameplate with this id is unlocked. |
| `sf:UnlockNameplate(id)  -> nil` | Unlocks a nameplate and persists (no-op if already unlocked). |
| `sf:IsSongUnlocked(uniqueId)  -> bool` | Whether the song with this unique id is unlocked. |
| `sf:UnlockSong(uniqueId)  -> nil` | Unlocks a song and persists (no-op if already unlocked). |
| `sf.SelectedHitsounds  -> string` | Folder name of the selected hitsound set. Assigning a different name persists it and reloads the player's hitsounds. |
| `sf:GetGlobalTrigger(name)  -> bool` | Reads a named boolean trigger. |
| `sf:GetGlobalCounter(name)  -> number` | Reads a named numeric counter. |
| `sf:SetGlobalTrigger(name, value)  -> nil` | Sets a named boolean trigger. |
| `sf:SetGlobalCounter(name, value)  -> nil` | Sets a named numeric counter. |
| `sf:GetClearStatusCount(difficulty, clearStatus)  -> integer` | Number of charts of a difficulty (0 Easy to 4 Extra Extreme) whose best clear status is exactly clearStatus (0 none, 1 assisted, 2 clear, 3 full combo, 4 perfect). 0 for out-of-range arguments. |
| `sf:GetDanBestPlay(node)  -> danBestPlay` | The no-mod best play for a dan song node (see Dan best play handle); a handle with HasRecord false if there is none. |
| `sf:GetCharacter()  -> character` | The player-bound character handle for this slot (see Character handle). |
| `sf.CharacterName  -> string` | Folder name of the equipped character. |
| `sf:ChangeCharacter(folderName)  -> bool` | Equips the character with this folder name. Returns true when the character is now equipped or was already active, false if no loaded character has this folder name. |
| `sf:GetPuchichara()  -> puchichara` | The equipped puchichara (see Puchichara handle), or nil if it cannot be resolved. |
| `sf:IsPuchicharaUnlocked(folderName)  -> bool` | Whether the puchichara with this folder name is unlocked. |
| `sf:UnlockPuchichara(folderName)  -> nil` | Unlocks a puchichara and persists (no-op if already unlocked). |
| `sf:ChangePuchichara(folderName)  -> nil` | Equips the puchichara with this folder name and persists. The method does not validate the name. |
| `sf:IsCharacterUnlocked(folderName)  -> bool` | Whether the character is unlocked. The equipped character always counts as unlocked. |
| `sf:UnlockCharacter(folderName)  -> nil` | Unlocks a character and persists (no-op if already unlocked). |
| `sf.DanTitleCount  -> integer` | Number of dan titles available, including the default title (always at least 1). |
| `sf:GetDanTitleByIndex(index)  -> danTitleEntry` | The dan title at a 0-based index (see Dan title entry handle). Index 0 is the default title; nil if out of range. |
| `sf.SelectedDan  -> string` | Text of the active dan title. |
| `sf:ChangeDan(title)  -> nil` | Makes the given title active, copying its gold and clear-status flags if it is one the player earned, refreshes the nameplate and persists. |
| `sf:ChangeName(name)  -> nil` | Changes the displayed name, refreshes the nameplate and persists. The method ignores empty or unchanged names. |
| `sf:ChangeNameplate(id)  -> nil` | Equips the nameplate with this id, refreshes the nameplate and persists. An id missing from the database clears the cached title text. |

```lua
local save = GetSaveFile(0)
local entry = CHARACTERLIST:GetByName("Aoi")
if entry and not save:IsCharacterUnlocked(entry.FolderName) then
    local cond = entry.UnlockCondition
    if cond:IsUnlockable(0) and cond:GetCoinPrice() <= save.Coins then
        save:SpendCoins(cond:GetCoinPrice())
        save:UnlockCharacter(entry.FolderName)
    end
end
```

## Nameplates and dan titles

### NAMEPLATE

Draws title plates, dan plates and complete player nameplates.

<div class="callout warn">
The skin's nameplate ROActivity (Modules/ROActivities/nameplate) does the drawing and defines the artwork and layout. Opacity is 0 to 255. Text parameters take a texture rendered from a text object (see Graphics and text); rarity is the index 0 Poor, 1 Common, 2 Uncommon, 3 Rare, 4 Epic, 5 Legendary, 6 Mythical.
</div>

| Method | Description |
| --- | --- |
| `NAMEPLATE:DrawTitlePlate(x, y, opacity, type, text, rarity, nameplateId)  -> nil` | Draws a title plate with the given display type, pre-rendered title texture, rarity index and nameplate id. |
| `NAMEPLATE:DrawDanPlate(x, y, opacity, danGrade, text)  -> nil` | Draws a dan plate for the given grade using a pre-rendered title texture. |
| `NAMEPLATE:DrawPlayerNameplate(x, y, opacity, player)  -> nil` | Draws the complete nameplate of a player slot; the red or blue side follows the game's 1P side setting. |
| `NAMEPLATE:DrawNameplateTitleById(id, x, y, opacity, font)  -> nil` | Renders the localized title of the nameplate with this id using a text object and draws it as a title plate. The id must exist in the nameplate database. |

### NAMEPLATESLIST

The database of every nameplate the game knows, with lookups by index or id and filtering.

<div class="callout warn">
Query methods return nameplate info handles. FindWhere calls a Lua function once per nameplate and keeps the entries for which it returns true.
</div>

| Method | Description |
| --- | --- |
| `NAMEPLATESLIST.Count  -> integer` | Number of nameplates in the database. |
| `NAMEPLATESLIST:GetByIndex(index)  -> nameplateInfo` | The nameplate at a 0-based database position, or nil if out of range. |
| `NAMEPLATESLIST:GetById(id)  -> nameplateInfo` | The nameplate with this id, or nil if not found. |
| `NAMEPLATESLIST:GetAll()  -> nameplateInfo[]` | Every nameplate as a list. |
| `NAMEPLATESLIST:FindWhere(predicate)  -> nameplateInfo[]` | The nameplates for which `predicate(info)` returns true. |

### Nameplate info handle

One nameplate title: localized text, display type, id, rarity and unlock condition.

<div class="callout warn">
sf.NameplateInfo and NAMEPLATESLIST return these handles. The default beginner nameplate has id -1, rarity "Common" and no unlock condition.
</div>

| Method | Description |
| --- | --- |
| `info.Title  -> string` | Title text in the current language. |
| `info.Type  -> integer` | Display type code passed to NAMEPLATE:DrawTitlePlate. |
| `info.Id  -> integer` | Nameplate id (-1 for the default beginner nameplate). |
| `info.Rarity  -> string` | Rarity name: "Poor", "Common", "Uncommon", "Rare", "Epic", "Legendary" or "Mythical". |
| `info.UnlockCondition  -> unlockCondition` | The unlock condition (see Unlock condition handle). |

### Danplate info handle

The player's active dan title as shown on the nameplate.

<div class="callout warn">
sf.DanplateInfo returns this handle. Values reflect the save file at the time of reading.
</div>

| Method | Description |
| --- | --- |
| `info.Title  -> string` | Text of the active dan title. |
| `info.Gold  -> bool` | Whether the player earned the active title with a gold pass. |
| `info.ClearStatus  -> integer` | Clear-status code of the active title. |

### Dan title entry handle

One dan title the player can select.

<div class="callout warn">
sf:GetDanTitleByIndex returns these entries. Index 0 is the default title (not gold, clear status 0); later indices are titles the player earned.
</div>

| Method | Description |
| --- | --- |
| `entry.Title  -> string` | Title text. |
| `entry.IsGold  -> bool` | Whether the player earned the title with a gold pass. |
| `entry.ClearStatus  -> integer` | Best clear status recorded for the title. |

### Dan best play handle

The best exam results of one dan record.

<div class="callout warn">
sf:GetDanBestPlay returns this handle. Check HasRecord before reading exams. GetExam returns a .NET array: index from 0 and read `.Length`.
</div>

| Method | Description |
| --- | --- |
| `play.HasRecord  -> bool` | Whether a record exists for the song. |
| `play:GetExam(slot)  -> int[]` | Best scores for exam slot 1 to 7: one value for a whole-course exam, one per song for per-song exams. Empty for a missing record or an invalid slot. |

## Characters and puchicharas

### CHARACTER

Creates character handles and exposes the names of the standard animation and voice slots.

<div class="callout warn">
CreateCharacter returns a handle that owns its resources; check IsValid and call Dispose when done. GetPlayerCharacter returns a handle that follows the player's equipped character and needs no disposal. GetPlayerGradientMap returns a gradient map (see Graphics and text). The ANIM_* and VOICE_* members are read-only strings; pass them to the character handle's animation and voice methods.
</div>

| Method | Description |
| --- | --- |
| `CHARACTER:CreateCharacter(folderName)  -> character` | Loads a standalone character from Global/Characters/{folderName}. IsValid is false if the folder does not exist. |
| `CHARACTER:GetPlayerCharacter(player)  -> character` | A handle bound to a player slot that resolves the equipped character on every call. |
| `CHARACTER:GetPlayerGradientMap(player)  -> gradientMap` | The palette gradient active for a player slot, or nil if none is set. |
| `CHARACTER.ANIM_PREVIEW  -> string` | Preview pose (menus and shops). |
| `CHARACTER.ANIM_RENDER  -> string` | Full render pose. |
| `CHARACTER.ANIM_GAME_NORMAL  -> string` | Gameplay, normal state. |
| `CHARACTER.ANIM_GAME_CLEAR  -> string` | Gameplay, gauge in the clear zone. |
| `CHARACTER.ANIM_GAME_MAX  -> string` | Gameplay, gauge full. |
| `CHARACTER.ANIM_GAME_GOGO  -> string` | Gameplay, go-go time. |
| `CHARACTER.ANIM_GAME_GOGO_MAX  -> string` | Gameplay, go-go time with a full gauge. |
| `CHARACTER.ANIM_GAME_MISS  -> string` | Gameplay, miss. |
| `CHARACTER.ANIM_GAME_MISS_DOWN  -> string` | Gameplay, miss with a low gauge. |
| `CHARACTER.ANIM_GAME_10COMBO  -> string` | Gameplay, 10-combo milestone. |
| `CHARACTER.ANIM_GAME_10COMBO_MAX  -> string` | Gameplay, 10-combo milestone with a full gauge. |
| `CHARACTER.ANIM_GAME_CLEARED  -> string` | Gameplay, song cleared. |
| `CHARACTER.ANIM_GAME_FAILED  -> string` | Gameplay, song failed. |
| `CHARACTER.ANIM_GAME_CLEAR_OUT  -> string` | Transition out of the clear state. |
| `CHARACTER.ANIM_GAME_CLEAR_IN  -> string` | Transition into the clear state. |
| `CHARACTER.ANIM_GAME_MAX_OUT  -> string` | Transition out of the full-gauge state. |
| `CHARACTER.ANIM_GAME_MAX_IN  -> string` | Transition into the full-gauge state. |
| `CHARACTER.ANIM_GAME_MISS_IN  -> string` | Transition into a miss. |
| `CHARACTER.ANIM_GAME_MISS_DOWN_IN  -> string` | Transition into a low-gauge miss. |
| `CHARACTER.ANIM_GAME_RETURN  -> string` | Return to the normal state. |
| `CHARACTER.ANIM_GAME_GOGOSTART  -> string` | Go-go start burst. |
| `CHARACTER.ANIM_GAME_GOGOSTART_CLEAR  -> string` | Go-go start burst in the clear state. |
| `CHARACTER.ANIM_GAME_GOGOSTART_MAX  -> string` | Go-go start burst with a full gauge. |
| `CHARACTER.ANIM_GAME_BALLOON_BREAKING  -> string` | Balloon being hit. |
| `CHARACTER.ANIM_GAME_BALLOON_BROKE  -> string` | Balloon popped. |
| `CHARACTER.ANIM_GAME_BALLOON_MISS  -> string` | Balloon missed. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_BREAKING  -> string` | Kusudama being hit. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_BROKE  -> string` | Kusudama broken. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_MISS  -> string` | Kusudama missed. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_IDLE  -> string` | Kusudama idle. |
| `CHARACTER.ANIM_GAME_TOWER_STANDING  -> string` | Tower mode, standing. |
| `CHARACTER.ANIM_GAME_TOWER_STANDING_TIRED  -> string` | Tower mode, standing while tired. |
| `CHARACTER.ANIM_GAME_TOWER_CLIMBING  -> string` | Tower mode, climbing. |
| `CHARACTER.ANIM_GAME_TOWER_CLIMBING_TIRED  -> string` | Tower mode, climbing while tired. |
| `CHARACTER.ANIM_GAME_TOWER_RUNNING  -> string` | Tower mode, running. |
| `CHARACTER.ANIM_GAME_TOWER_RUNNING_TIRED  -> string` | Tower mode, running while tired. |
| `CHARACTER.ANIM_GAME_TOWER_CLEAR  -> string` | Tower mode, clear. |
| `CHARACTER.ANIM_GAME_TOWER_CLEAR_TIRED  -> string` | Tower mode, clear while tired. |
| `CHARACTER.ANIM_GAME_TOWER_FAIL  -> string` | Tower mode, fail. |
| `CHARACTER.ANIM_MENU_WAIT  -> string` | Menu, waiting. |
| `CHARACTER.ANIM_MENU_START  -> string` | Menu, start. |
| `CHARACTER.ANIM_MENU_NORMAL  -> string` | Menu, normal. |
| `CHARACTER.ANIM_MENU_SELECT  -> string` | Menu, selection. |
| `CHARACTER.ANIM_ENTRY_NORMAL  -> string` | Entry screen, normal. |
| `CHARACTER.ANIM_ENTRY_JUMP  -> string` | Entry screen, jump. |
| `CHARACTER.ANIM_RESULT_NORMAL  -> string` | Results, normal. |
| `CHARACTER.ANIM_RESULT_CLEAR  -> string` | Results, clear. |
| `CHARACTER.ANIM_RESULT_FAILED_IN  -> string` | Results, entering the failed state. |
| `CHARACTER.ANIM_RESULT_FAILED  -> string` | Results, failed. |
| `CHARACTER.VOICE_END_FAILED  -> string` | Song end, failed. |
| `CHARACTER.VOICE_END_CLEAR  -> string` | Song end, cleared. |
| `CHARACTER.VOICE_END_FULLCOMBO  -> string` | Song end, full combo. |
| `CHARACTER.VOICE_END_ALLPERFECT  -> string` | Song end, all perfect. |
| `CHARACTER.VOICE_END_AIBATTLE_WIN  -> string` | Song end, AI battle won. |
| `CHARACTER.VOICE_END_AIBATTLE_LOSE  -> string` | Song end, AI battle lost. |
| `CHARACTER.VOICE_MENU_SONGSELECT  -> string` | Song select entered. |
| `CHARACTER.VOICE_MENU_SONGDECIDE  -> string` | Song confirmed. |
| `CHARACTER.VOICE_MENU_SONGDECIDE_AI  -> string` | Song confirmed in AI battle. |
| `CHARACTER.VOICE_MENU_DIFFSELECT  -> string` | Difficulty select. |
| `CHARACTER.VOICE_MENU_DANSELECTSTART  -> string` | Dan select entered. |
| `CHARACTER.VOICE_MENU_DANSELECTPROMPT  -> string` | Dan select prompt. |
| `CHARACTER.VOICE_MENU_DANSELECTCONFIRM  -> string` | Dan course confirmed. |
| `CHARACTER.VOICE_TITLE_SANKA  -> string` | Title screen entry. |
| `CHARACTER.VOICE_TOWER_MISS  -> string` | Tower mode miss. |
| `CHARACTER.VOICE_RESULT_BESTSCORE  -> string` | Results, new best score. |
| `CHARACTER.VOICE_RESULT_CLEARFAILED  -> string` | Results, failed. |
| `CHARACTER.VOICE_RESULT_CLEARSUCCESS  -> string` | Results, cleared. |
| `CHARACTER.VOICE_RESULT_DANFAILED  -> string` | Results, dan failed. |
| `CHARACTER.VOICE_RESULT_DANREDPASS  -> string` | Results, dan passed. |
| `CHARACTER.VOICE_RESULT_DANGOLDPASS  -> string` | Results, dan passed with gold. |

### Character handle

A drawable character: plays named animations and voices and carries per-handle draw state (opacity, scale, tint, rotation, blend and wrap mode, palette gradient).

<div class="callout warn">
CHARACTER:GetPlayerCharacter, CHARACTER:CreateCharacter, sf:GetCharacter and a character list entry's Character property return character handles. Only handles from CreateCharacter own their resources and need Dispose. The handle stores Set* values and applies them on every following draw; the scale and opacity arguments of the draw methods multiply with the stored values. Stored opacity is 0.0 to 1.0, per-draw opacity is 0 to 255. Animation and voice names are the CHARACTER constants.
</div>

| Method | Description |
| --- | --- |
| `char.IsValid  -> bool` | Whether the handle resolves to a loaded character. |
| `char.FolderName  -> string` | Folder name, or an empty string if invalid. |
| `char.FullPath  -> string` | Absolute folder path, or an empty string if invalid. |
| `char.DisplayName  -> string` | Localized display name, falling back to the folder name. |
| `char:SetPaletteGradient(stops, blend?)  -> nil` | Applies a palette gradient built from a table of at least two color stops, with an optional blend amount (default 1.0). Player-bound handles also store the gradient on the player slot. Passing nil clears it. |
| `char:ClearPaletteGradient()  -> nil` | Removes the palette gradient (and the player slot's gradient for player-bound handles). |
| `char:SetOpacity(opacity)  -> nil` | Stored opacity, 0.0 transparent to 1.0 opaque. |
| `char:SetScale(scaleX, scaleY)  -> nil` | Stored scale; a negative X mirrors horizontally. |
| `char:SetColor(color)  -> nil` | Stored tint from a color value. |
| `char:SetColor(r, g, b)  -> nil` | Stored tint from three 0.0 to 1.0 channels. |
| `char:SetRotation(degrees)  -> nil` | Stored rotation in degrees. |
| `char:SetBlendMode(mode)  -> nil` | Stored blend mode: "normal", "add", "multi", "sub" or "screen". |
| `char:SetWrapMode(mode)  -> nil` | Stored texture wrap mode: "edge", "border", "repeat" or "mirror". |
| `char:GetScale()  -> vector2` | Stored scale. |
| `char:GetColor()  -> tuple` | Stored tint as a .NET tuple with fields Item1, Item2 and Item3 (red, green, blue). |
| `char:GetRotation()  -> number` | Stored rotation in degrees. |
| `char:GetBlendMode()  -> string` | Stored blend mode. |
| `char:GetWrapMode()  -> string` | Stored wrap mode. |
| `char:Draw(x, y, animation, scaleX?, scaleY?, opacity?)  -> nil` | Draws the animation at x, y. Defaults: scale 1, opacity 255. |
| `char:DrawAtAnchor(x, y, animation, anchor?, scaleX?, scaleY?, opacity?)  -> nil` | Draws the animation with the named anchor point (default "bottom") placed at x, y. |
| `char:DrawRect(x, y, w, h, animation, scaleX?, scaleY?, opacity?)  -> nil` | Draws the animation at the rectangle's top-left corner. The method accepts w and h for layout code, but they do not affect the drawing. |
| `char:DrawRectAtAnchor(x, y, clipW, clipH, animation, opacity?, clipX?, clipY?)  -> nil` | Draws the animation with its top-left corner at x, y, clipped to a clipW by clipH rectangle offset by clipX, clipY. Scale, tint and rotation come from the stored state only. |
| `char:Update(animation, looping?)  -> bool` | Advances the animation (looping by default) and returns whether it is still playing. |
| `char:LoadAnimation(animation)  -> nil` | Loads the animation's frames. |
| `char:DisposeAnimation(animation)  -> nil` | Frees the animation's frames. |
| `char:AvailableAnimation(animation)  -> bool` | Whether the character provides the animation. |
| `char:SetAnimationDuration(animation, duration)  -> nil` | Sets the animation's playback duration. |
| `char:SetAnimationCyclesFromBPM(animation, bpm)  -> nil` | Sets the animation's cycle length from a BPM. |
| `char:ResetAnimationCounter(animation)  -> nil` | Restarts the animation from its first frame. |
| `char:GetAnimationSize(animation)  -> vector2` | Drawn size of the animation's current frame at skin resolution, or (0, 0) if unavailable. |
| `char:LoadVoice(voice)  -> nil` | Loads a voice clip. |
| `char:DisposeVoice(voice)  -> nil` | Frees a voice clip. |
| `char:PlayVoice(voice)  -> nil` | Plays a voice clip. |
| `char:Dispose()  -> nil` | Releases the character's resources (handles from CreateCharacter only). |

```lua
local chara = CHARACTER:GetPlayerCharacter(0)
chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL)

function update()
    chara:Update(CHARACTER.ANIM_MENU_NORMAL)
end

function draw()
    chara:DrawAtAnchor(960, 1000, CHARACTER.ANIM_MENU_NORMAL, "bottom")
end
```

### CHARACTERLIST

The list of every loaded character.

<div class="callout warn">
The skin rebuilds the list when it loads its characters and disposes it on skin reload, so the global can be nil while no characters are loaded. Query methods return character list entries.
</div>

| Method | Description |
| --- | --- |
| `CHARACTERLIST.Count  -> integer` | Number of loaded characters. |
| `CHARACTERLIST:GetAll()  -> characterEntry[]` | Every character as a list. |
| `CHARACTERLIST:GetByIndex(index)  -> characterEntry` | The entry at a 0-based index, or nil if out of range. |
| `CHARACTERLIST:GetByName(folderName)  -> characterEntry` | The entry with this folder name, or nil if not found. |

### Character list entry

One CHARACTERLIST entry: folder name, display name, rarity, a character handle and the unlock condition.

<div class="callout warn">
The list owns the shared handle in the Character property; do not dispose it. Load animations on it before drawing.
</div>

| Method | Description |
| --- | --- |
| `entry.FolderName  -> string` | Folder name; save files use it as the key. |
| `entry.DisplayName  -> string` | Localized display name. |
| `entry.Rarity  -> string` | Rarity name (see Nameplate info handle for the list). |
| `entry.Character  -> character` | Character handle for this entry. |
| `entry.UnlockCondition  -> unlockCondition` | The unlock condition (see Unlock condition handle). |

### PUCHICHARALIST

The list of every loaded puchichara, plus each player's current selection.

<div class="callout warn">
The skin rebuilds the list when it loads its puchichara textures and disposes it on skin reload, so the global can be nil while they are not loaded. Query methods return puchichara handles.
</div>

| Method | Description |
| --- | --- |
| `PUCHICHARALIST.Count  -> integer` | Number of loaded puchichara. |
| `PUCHICHARALIST:GetAll()  -> puchichara[]` | Every puchichara as a list. |
| `PUCHICHARALIST:GetByIndex(index)  -> puchichara` | The puchichara at a 0-based index, or nil if out of range. |
| `PUCHICHARALIST:GetByName(folderName)  -> puchichara` | The puchichara with this folder name, or nil if not found. |
| `PUCHICHARALIST:GetPlayerPuchichara(player)  -> puchichara` | The puchichara equipped by a player slot, or nil if it cannot be resolved. |

### Puchichara handle

One puchichara: its textures, localized name and author, rarity, folder name and unlock condition.

<div class="callout warn">
PUCHICHARALIST and sf:GetPuchichara return these handles. The list owns the textures; do not dispose them. A missing image gives an empty texture.
</div>

| Method | Description |
| --- | --- |
| `puchi.tx  -> texture` | Sprite sheet loaded from Chara.png. |
| `puchi.render  -> texture` | Full render loaded from Render.png. |
| `puchi.Name  -> string` | Localized display name. |
| `puchi.Author  -> string` | Localized author name. |
| `puchi.Rarity  -> string` | Rarity name (see Nameplate info handle for the list). |
| `puchi.FolderName  -> string` | Folder name; save files use it as the key. |
| `puchi.UnlockCondition  -> unlockCondition` | The unlock condition (see Unlock condition handle). |
| `puchi:GetUnlockMessage()  -> string` | Shortcut for `puchi.UnlockCondition:GetConditionMessage()`. |

## Play state and unlocks

### PLAYSTATE

Live results of the current or most recent play: judgement counts, score, combo, clear checks, and tower and dan status.

<div class="callout warn">
The values come from the gameplay screen, so they are meaningful during a play and on the screens that follow it. Player indices are 0-based; the methods do not range-check them. The dan checks always evaluate player 0.
</div>

| Method | Description |
| --- | --- |
| `PLAYSTATE.LastRegisteredFloor  -> integer` | Tower mode: the last floor reached. |
| `PLAYSTATE.MaxNumberOfLives  -> integer` | Tower mode: the maximum number of lives. |
| `PLAYSTATE.CurrentNumberOfLives  -> integer` | Tower mode: the current number of lives. |
| `PLAYSTATE.InvincibilityDurationSpeedDependent  -> number` | Tower mode: the invincibility duration adjusted for song speed. |
| `PLAYSTATE.InvincibilityDuration  -> integer` | Tower mode: the base invincibility duration. |
| `PLAYSTATE:WasPlayEndedNormally()  -> bool` | Whether the previous play ran to the end. |
| `PLAYSTATE:WasPlayAborted()  -> bool` | Whether the player quit the previous play early. |
| `PLAYSTATE:GetGoodCount(player)  -> integer` | Number of Good judgements. |
| `PLAYSTATE:GetOkCount(player)  -> integer` | Number of Ok judgements. |
| `PLAYSTATE:GetBadCount(player)  -> integer` | Number of Bad judgements. |
| `PLAYSTATE:GetRollCount(player)  -> integer` | Number of drumroll hits. |
| `PLAYSTATE:GetADLibCount(player)  -> integer` | Number of ADLib notes hit. |
| `PLAYSTATE:GetMissedADLibCount(player)  -> integer` | Number of ADLib notes missed. |
| `PLAYSTATE:GetBoomCount(player)  -> integer` | Number of mine notes hit. |
| `PLAYSTATE:GetAvoidedBoomCount(player)  -> integer` | Number of mine notes avoided. |
| `PLAYSTATE:GetScore(player)  -> integer` | Current score. |
| `PLAYSTATE:GetCombo(player)  -> integer` | Current combo. |
| `PLAYSTATE:GetHighestCombo(player)  -> integer` | Highest combo reached. |
| `PLAYSTATE:IsClear(player)  -> bool` | Whether the gauge meets the clear line. |
| `PLAYSTATE:IsAssistedClear(player)  -> bool` | Whether the play is a clear while a score-reducing mod is active. |
| `PLAYSTATE:IsFullCombo(player)  -> bool` | Clear, not assisted, with no Bad judgements and no mines hit. |
| `PLAYSTATE:IsPerfect(player)  -> bool` | Full combo with no Ok judgements. |
| `PLAYSTATE:IsAlive()  -> bool` | Tower mode: whether lives remain. |
| `PLAYSTATE:IsPass()  -> bool` | Dan mode: whether the exam status is not a failure. |
| `PLAYSTATE:IsRedPass()  -> bool` | Dan mode: whether the exam status is a standard pass. |
| `PLAYSTATE:IsGoldPass()  -> bool` | Dan mode: whether the exam status is a gold pass. |
| `PLAYSTATE:IsDanClear()  -> bool` | Dan mode: passed and not assisted. |
| `PLAYSTATE:IsDanFullCombo()  -> bool` | Dan mode: dan clear with no Bad judgements and no mines hit. |
| `PLAYSTATE:IsDanPerfect()  -> bool` | Dan mode: dan full combo with no Ok judgements. |

### Unlock condition handle

The unlock requirement of a nameplate, character or puchichara.

<div class="callout warn">
The UnlockCondition property of nameplate info handles, character list entries and puchichara handles returns this handle. An item without a condition (HasCondition false) is available by default: IsUnlockable returns true and the messages are empty. The condition vocabulary matches Unlock.json and chart unlockables; see the <a href="../guides/unlockables.md">Chart unlockables</a> guide.
</div>

| Method | Description |
| --- | --- |
| `cond.HasCondition  -> bool` | Whether the item has an unlock condition. |
| `cond:GetConditionType()  -> string` | The condition type id (for example "ch", "cs", "gt", "gc" or "ig"), or an empty string. |
| `cond:GetCoinPrice()  -> integer` | Coin price of the condition, or 0. |
| `cond:GetConditionMessage()  -> string` | Localized description of the condition. |
| `cond:IsUnlockable(player)  -> bool` | Whether the player currently meets the condition. |
| `cond:GetBlockedMessage(player)  -> string` | Why the player does not meet the condition, or an empty string when met. |

## Theme and language

### THEME

The skin's resolution, theme settings, skin-scoped localized strings and the theme-setting definitions.

<div class="callout warn">
The skin declares theme settings in ThemeSettings.json and stores their values in ThemeSettings.db3 next to it. The getters always return setting values as strings; a missing setting returns its declared default, or an empty string if no declaration exists. GetThemeSettingForPlayer takes a 1-based player number. Definition indices are 0-based.
</div>

| Method | Description |
| --- | --- |
| `THEME:GetResolution()  -> vector2` | The skin's resolution. |
| `THEME:GetThemeSetting(settingId)  -> string` | Value of a global-scope setting. |
| `THEME:GetThemeSettingForPlayer(settingId, player)  -> string` | Value of a save-scope setting for the 1-based player, or its default if the save has no value. |
| `THEME:GetSkinString(key)  -> string` | Localized string from the skin's Locales folder: current language first, then the skin's default locale, then `[LOCALE NOT FOUND: key]`. |
| `THEME:GetDefinitionCount()  -> integer` | Number of setting definitions in ThemeSettings.json. |
| `THEME:GetDefinitionId(index)  -> string` | Id of the definition at a 0-based index, or an empty string. |
| `THEME:GetDefinitionScope(index)  -> string` | Scope of the definition: "global" or "save". |
| `THEME:GetDefinitionType(index)  -> string` | Type of the definition: "bool", "int", "double", "string" or "enum". |

### LANG

Localized game strings, language switching and multi-language text values.

<div class="callout warn">
GetString formats the entry with any extra arguments. GetLanguageIds and GetLanguageNames return .NET arrays (0-based, `.Length`); GetAvailableLanguages returns a dictionary to enumerate with `:GetEnumerator()` (see Data and persistence). FromDict takes a parsed JSON object from JSONLOADER (it does not accept a Lua table); AsLocalizationData takes a JsonNode from JSONLOADER:LoadJson.
</div>

| Method | Description |
| --- | --- |
| `LANG:GetString(key, ...)  -> string` | The localized string for a key, with format placeholders filled from the extra arguments. |
| `LANG:ChangeLanguage(id)  -> bool` | Switches the active language if the id exists and differs from the current one, then calls `reloadLanguage` on every loaded script; returns whether it switched. It leaves CONFIG.Language unchanged. |
| `LANG:GetLanguageIds()  -> string[]` | Ids of the available languages. |
| `LANG:GetLanguageNames()  -> string[]` | Display names of the available languages, in the same order. |
| `LANG:GetAvailableLanguages()  -> dict` | Language id to display name. |
| `LANG:GetExamName(type)  -> string` | Localized name of a dan exam type. |
| `LANG:AsLocalizationData(node)  -> localizationData` | Builds a localization value from a JsonNode of the form `{ "strings": { "<lang>": "text" } }`. |
| `LANG:FromDict(dict)  -> localizationData` | Builds a localization value from a parsed JSON object mapping language ids to text. |
| `LANG:FromString(json)  -> localizationData` | Builds a localization value from a JSON object string mapping language ids to text; an empty value if the string does not parse. |

```lua
local langs = LANG:GetAvailableLanguages()
local e = langs:GetEnumerator()
while e:MoveNext() do
    print(e.Current.Key, e.Current.Value)
end

local name = LANG:FromString('{"ja":"太鼓","default":"Taiko"}'):GetString("")
```

### Localization data handle

A set of strings keyed by language id that resolves to the current language.

<div class="callout warn">
LANG:AsLocalizationData, LANG:FromDict and LANG:FromString return this handle. Resolution order: the current language id, then the "default" key, then the fallback passed to GetString.
</div>

| Method | Description |
| --- | --- |
| `loc:GetString(fallback)  -> string` | The text for the current language, or "default", or the fallback. |
| `loc:SetString(langId, text)  -> nil` | Sets the text for a language id. |
| `loc:GetAllStrings()  -> string[]` | Every stored text, in no particular order. |
