# Modal Lua Customization Guide

## Methods

- (bool) isAnimationFinished()

Returns if the currently playing modal animation is finished, when returning "true" enter is pressable again to move to the next modal orreturn to the song select screen

- (void) registerNewModal(int player, int rarity, int modal_type, object modal_info, object modal_visual_ref)

Method called every time a new modal is registered including its related information

Includes the following arguments:
```
- (int) player: The player related to the modal (between 1 and 5)
- (int) rarity: The rarity of the unlocked asset (between 0 (Poor) and 6 (Mythical)), unused for Coins
- (int) modal_type: The type of unlocked asset (0: Coins, 1: Character, 2: Puchichara, 3: Title, 4: Song)

- (object) modal_info:  Asset related information, modal_type dependent:
> Coins -> (long) modal_info: The coin value of the play
> Character -> (CCharacter) modal_info: The unlocked character information data (never nil) 
> Puchichara -> (CPuchichara) modal_info: The unlocked puchichara information data (never nil)
> Title -> (NameplateUnlockable) modal_info: The unlocked nameplate information data (never nil)
> Song -> (SongNode?) modal_info: The unlocked song information data (can be nil)

- (object) modal_visual_ref:  Asset related visuals (or extra info), modal_type dependent:
> Coins -> (long) modal_visual_ref: The total count of coins after the play
> Character -> (CTexture?) modal_visual_ref: The character render as displayed in my room (can be nil)
> Puchichara -> nil
> Title -> (LuaNamePlateScript) modal_visual_ref: A reference to the Nameplate Lua script (never nil except if broken skin)
> Song -> (CTexture?) modal_visual_ref: The song preimage, with the size value set to the same value as in the song select menu (can be nil)
```

- (void) loadAssets()

Method where all resource allocation should be included, called at each skin reload and at launch

- (void) update()

The regular update loop, for example to increment time counters, plays before draw()

- (void) draw() 

For displayables, played at each result screen Draw() iteration

## Types

TBD
