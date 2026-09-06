<!-- guides/puchicharas.md -->

# Adding a Puchichara to OpenTaiko

A puchichara is the small companion that bounces next to the gauge during play. Each puchichara is one folder under `Global/PuchiChara/` in the game's install folder, holding a two-frame sprite sheet and a few optional JSON files. You write no code: create the folder with the right file names and the game picks it up at the next start.

Compatibility: OpenTaiko 0.6.1 still loads puchicharas made for 0.6.0 without changes.

## Before you start

- OpenTaiko 0.6.1 installed. The game reads puchicharas from `Global/PuchiChara/` next to the game executable, and all skins share the folder.
- An image editor that exports PNG with transparency.
- A text editor for the JSON files.
- Optional: a short `.ogg` clip for `Welcome.ogg`.

## Step 1: Create the folder

The game lists the subfolders of `Global/PuchiChara/` at boot and treats each one as a puchichara. The folder name is the identity: the game writes it to the save file when the player selects the puchichara and records the unlock under it. Pick a stable name: after a rename, existing selections fall back to the first folder and the recorded unlock no longer matches. The shipped folders use a sorting prefix, for example `00 - None`, `01a - OpenTaiko-Kun` and `02 - Bol`. The game does not sort the list, so the prefix keeps the file-system order predictable. The first folder is the fallback when a save references a folder that no longer exists, so keep `00 - None` first.

```
Global/PuchiChara/
    00 - None/
    01a - OpenTaiko-Kun/
    02 - Bol/
    99 - MyMascot/          <-- your new folder
```

## Step 2: Draw the sprite sheet (Chara.png)

The game draws the companion from `Chara.png`, a horizontal sprite sheet. The frame layout comes from the skin value `Game_PuchiChara`, which defaults to `256,256,2` (frame width, frame height, frame count), so the shipped sheets are 512x256 pixels: two 256x256 frames side by side, frame 0 on the left. During play the game cycles through the frames and adds a vertical bounce (idle bounce in menus, beat-synced bounce in game), so the two frames should be two poses of the same character. Use a transparent background. If `Chara.png` is missing the entry still loads but draws nothing.

The shipped folders also contain a `Chara.xcf` (GIMP source) and a `PuchiConfig.txt`. The game reads neither.

```ini
Chara.png : 512 x 256 PNG, transparency
  +-----------------+-----------------+
  |    frame 0      |    frame 1      |
  |   256 x 256     |   256 x 256     |
  +-----------------+-----------------+
```

## Step 3: Write Metadata.json

Create `Metadata.json` with these fields:

- `name`, `author`, `description`: each a plain string or a localized object `{ "strings": { "default": "...", "ja": "...", ... } }` where `default` is the fallback and the other keys are game language codes.
- `rarity`: one of `Poor`, `Common`, `Uncommon`, `Rare`, `Epic`, `Legendary`, `Mythical`. Rarity sets the colour and the unlock-notification tier. Every rarity has a coin multiplier of 1, so it does not change earnings. An unknown value behaves like `Common`.

If the file is absent the entry loads with name `(None)`, rarity `Common` and author `(None)`.

```json
{
    "name": {
        "strings": {
            "default": "MyMascot",
            "ja": "マイマスコット"
        }
    },
    "rarity": "Rare",
    "description": {
        "strings": {
            "default": "A friendly companion.\nWaves during play."
        }
    },
    "author": "YourName"
}
```

## Step 4 (optional): Add Effects.json

`Effects.json` gives the puchichara gameplay effects. All fields default to off when the file is absent:

- `allpurple` (bool): large don and ka notes become purple notes that accept either drum.
- `autoroll` (int): automatic hits per second on drum rolls and balloons. Any value above 0 sets the coin multiplier to 0.
- `showadlib` (bool): shows hidden ADLIB notes. Multiplies coins by 0.9.
- `splitlane` (bool): draws don and ka notes on separate lanes.

Leave the file out for a purely cosmetic companion.

```json
{
    "allpurple": false,
    "autoroll": 0,
    "showadlib": false,
    "splitlane": false
}
```

## Step 5 (optional): Add Welcome.ogg and Render.png

- `Welcome.ogg`: a voice clip the game loads in the Voice sound group. The game's built-in room screen plays it when the player selects the puchichara; Lua stages cannot reach it, so a skin with its own room screen does not play it.
- `Render.png`: a full-size still image that Lua stages can draw as a portrait (the `render` texture of a `PUCHICHARALIST` entry). It is a single image of any size. None of the shipped puchicharas include one.

Both files are optional.

```
MyMascot/
    Chara.png       (required, the animated sprite sheet)
    Metadata.json   (name, rarity, author, description)
    Effects.json    (optional gameplay effects)
    Unlock.json     (optional unlock condition)
    Welcome.ogg     (optional voice clip)
    Render.png      (optional portrait)
```

## Step 6 (optional): Add an unlock condition (Unlock.json)

Without `Unlock.json` the puchichara is available immediately. To lock it, add an `Unlock.json` with the fields `condition`, `type`, `values` and `references`; the format and the condition ids are the same as for songs and characters (see the unlockables guide). The example below unlocks once the player has earned 500 coins in total. Shipped examples: OpenTaiko-Kun costs 100 coins (`"condition": "ch"`), Bol requires 20 cleared charts by the charter `bol` (`"condition": "sc"`), and Tinyfox requires one song played in the genre `Project Outfox Serenity` (`"condition": "sg"`).

The player buys coin conditions in the room screen. The game checks the other conditions on the results screen after each play; when the player meets one, it adds the puchichara to the save file's unlocked list and shows a notification.

```json
{
    "condition": "ce",
    "type": "me",
    "values": [
        500
    ]
}
```

## Step 7: Restart and select it

Restart the game (or reload the skin from the settings) so the game rebuilds the list. Open the room screen and pick the new entry from the puchichara list. Once selected it appears during play next to the gauge. If no companion appears during play, check that the `Draw PuchiChara` option in the system settings is enabled.

## Troubleshooting and notes

- File names are exact: `Chara.png`, `Metadata.json`, `Effects.json`, `Unlock.json`, `Welcome.ogg`, `Render.png`. The game ignores a misnamed file and applies the default.
- Save files and unlock records store the folder name verbatim. Renaming a folder makes existing selections fall back to the first folder.
- The game slices the sprite sheet with the skin's `Game_PuchiChara` frame size (default `256,256,2`). It slices a sheet of a different size with the same numbers, so the frames come out cropped or misaligned. If a skin overrides `Game_PuchiChara`, match that value. The shipped skin does not override it.
- `autoroll` above 0 zeroes coin gains and `showadlib` reduces them to 0.9, so a cosmetic companion that sets either one reduces its player's earnings.
- The shipped JSON files contain trailing commas. The game's JSON parser accepts them; strict validators reject them.
- The game builds the list once at boot and on skin reload. A folder added while the game is running appears after the next boot or skin reload.
