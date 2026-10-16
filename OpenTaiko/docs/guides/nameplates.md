<!-- guides/nameplates.md -->

# Adding Nameplates

A nameplate title is the text plate shown on a player's nameplate together with the player name and dan title. Players unlock titles by playing, buying them or receiving them from a script, and equip one in My Room. You can add your own titles with a nameplate pack: a folder with a `Nameplates.json` file that you place in `Global/Nameplates/`. A skin can ship a pack of its own. The game lists the nameplates of every pack next to the shipped ones, in every screen that shows nameplates.

## Before you start

- A text editor that saves UTF-8.
- A pack id: a short name made of lowercase letters, digits and `_` that no other pack uses, for example your name or your chart pack's name. The game builds the id of every nameplate in the pack from it.
- The game reads the packs when it starts and when it loads a skin. Restart the game after each change to see it.

## Step 1: Create the pack folder

Create a folder inside `Global/Nameplates/` (the `Global` folder sits next to the game). The folder name is free. The pack holds one required file and an optional `Lang/` folder for translations (Step 3):

```
Global/
  Nameplates/
    Example Pack/
      Nameplates.json
      Lang/
        fr.json
        ja.json
```

A skin ships its pack the same way, as a `Nameplates/` folder at the root of the skin (`System/<skin>/Nameplates/Nameplates.json`). The game only loads the pack of the active skin, so the nameplates of a skin pack disappear while another skin is active.

## Step 2: Write Nameplates.json

`Nameplates.json` holds the pack id and the list of nameplates:

- `pack` (string, required): the pack id.
- `name` (object, optional): the pack's name, shown on the pack strip of the PC in My Room, as `{ "strings": { "default": "...", "<code>": "..." } }`: the strip shows the name for the current language, else `default`. Without it, the strip shows the pack id.
- `nameplates` (array, required): one object per nameplate, with these fields:
  - `key` (string, required): a name for the nameplate, unique inside the pack. The nameplate's full id is `<pack>:<key>`, for example `example_pack:night_sky`. Save files store this id, so do not change a key once players can have the nameplate, and never give it to another nameplate.
  - `title` (string, required): the title text. Every language without a translation shows it.
  - `type` (int, default 0): the plate art, a folder number in the skin's nameplate textures. In the shipped skin the folders are `Modules/ROActivities/nameplate/Textures/Title/0` to `45`, and a number without a folder draws no plate. The table below lists the shipped skin's plates.
  - `rarity` (string, default `Common`): the rarity name (see the table below). An unknown name becomes `Common`.
  - `unlock` (object, optional): the unlock condition, with the same fields as in an `Unlock.json`: `condition`, `values`, `type` and `references` (see the [Unlockables reference](unlockables-reference.md) for the condition ids). Without `unlock`, every player owns the nameplate from the start.

Rarity values (the stars are the shipped skin's):

| Rarity | Stars on the plate | Notification tier |
| --- | --- | --- |
| <span class="rarity rarity-poor">Poor</span> | 0 | 0 |
| <span class="rarity rarity-common">Common</span> | 0 | 0 |
| <span class="rarity rarity-uncommon">Uncommon</span> | 0 | 1 |
| <span class="rarity rarity-rare">Rare</span> | 1 | 2 |
| <span class="rarity rarity-epic">Epic</span> | 2 | 3 |
| <span class="rarity rarity-legendary">Legendary</span> | 3 | 4 |
| <span class="rarity rarity-mythical">Mythical</span> | 4 | 4 |

Plate types (`type`) of the shipped skin, Open-World Memories. The PC in My Room groups nameplates by type, under the section names of the skin's `Modules/Stages/myroom/lang/<code>/nameplate_types.json`. A type without a name there shows as "Plate Type" and its number, and the sections of types 10 and 38 read "???" until the player owns a nameplate of that type.

| Type | Name | Effect |
| --- | --- | --- |
| 0 | Wood |  |
| 1 | Copper | Shine |
| 2 | Amethyst | Shine |
| 3 | Nature | Shine |
| 4 | Red | Shine |
| 5 | Blue | Shine |
| 6 | Ash | Shine |
| 7 | Rainbow | Animated |
| 8 | Space |  |
| 9 | Diamond |  |
| 10 | Misery |  |
| 11 | Gold |  |
| 12 | Pearl |  |
| 13 | Platinum |  |
| 14 | Kohaku (Character plate) |  |
| 15 | Dashy |  |
| 16 | Blue |  |
| 17 | Red |  |
| 18 | Sky Purple |  |
| 19 | AI |  |
| 20 | Sky |  |
| 21 | Yellow |  |
| 22 | Cream |  |
| 23 | Sea |  |
| 24 | Green |  |
| 25 | Chapter I |  |
| 26 | Chapter II |  |
| 27 | Chapter III |  |
| 28 | OpTk Light |  |
| 29 | OpTk Dark |  |
| 30 | NEP0 |  |
| 31 | Night |  |
| 32 | Chapter IV |  |
| 33 | Clear Performance |  |
| 34 | Full Combo Performance |  |
| 35 | Perfect Performance |  |
| 36 | Template |  |
| 37 | Chapter V |  |
| 38 | V-ENTURE |  |
| 39 | Clear History |  |
| 40 | Full Combo History |  |
| 41 | Perfect History |  |
| 42 | Ryuto Setsujin |  |
| 43 | Sunrise |  |
| 44 | Chapter VI |  |
| 45 | A new world |  |

The game checks the unlock conditions on the results screen after each play. When the player meets one, the game unlocks the nameplate and shows a notification. Coin conditions (`ch`, `cs`, `cm`) only unlock through a purchase: in the shipped skin, the player buys the nameplate from the PC in My Room. The PC in My Room also keeps locked Mythical nameplates out of its list until the player owns them.

```json
{
  "pack": "example_pack",
  "name": { "strings": { "default": "Example Pack", "fr": "Pack d'exemple" } },
  "nameplates": [
    {
      "key": "night_sky",
      "title": "Night Sky Walker",
      "type": 8,
      "rarity": "Rare",
      "unlock": { "condition": "tp", "values": [50] }
    },
    {
      "key": "hello",
      "title": "Hello There"
    }
  ]
}
```

## Step 3: Add translations

Translations are optional. Each file in the pack's `Lang/` folder holds the titles of one language, one line per nameplate, with the pack's keys (without the pack id). The file name is the language code:

| File | Language |
| --- | --- |
| `de.json` | German |
| `en.json` | English |
| `es.json` | Spanish |
| `fr.json` | French |
| `ja.json` | Japanese |
| `ko.json` | Korean |
| `nl.json` | Dutch |
| `ru.json` | Russian |
| `zh.json` | Chinese |

```json
{
  "night_sky": "Promeneur du ciel nocturne",
  "hello": "Bonjour"
}
```

A nameplate missing from a language's file shows its `title` in that language.

## Step 4: Test it in game

Start the game and open the PC in My Room. In the nameplates tab, pick your pack on the strip above the list: the list shows the pack's nameplates, the locked ones with their unlock hint. Equip one, and check its title, plate and stars on the player's nameplate. Switch the game language to check the translations.

The game skips a pack it cannot read and shows an error notification that names it. A pack without a valid `pack` id, or with the id of a pack the game already loaded, counts as unreadable. The game skips a single nameplate without a `key` or a `title`, or with a key that already exists in the pack, and writes the reason to `OpenTaiko.log`.

## Nameplates from a script

Lua scripts work with the same ids. Every method that takes a nameplate id accepts the full id as a string; for a shipped nameplate, the methods also accept its number. A script can give a nameplate that has the `ig` condition, which play never unlocks:

```lua
local save = GetSaveFile(0)
local id = "example_pack:night_sky"
if not save:IsNameplateUnlocked(id) then
    save:UnlockNameplate(id)
end
save:ChangeNameplate(id)
```

UnlockNameplate shows no notification, so a script that wants one shows it itself. See [Players and profiles](../api/players.md) for the nameplate methods.

## Examples

These packs show common patterns. Replace the ids, titles and numbers with your own.

**A free nameplate.** Without `unlock`, every player owns the nameplate and can equip it right away.

```json
{
  "pack": "example_pack",
  "nameplates": [
    { "key": "welcome", "title": "Welcome Aboard", "type": 0, "rarity": "Common" }
  ]
}
```

**Unlock by playing (`tp`).** The nameplate unlocks after 100 plays. `ap` (AI battle plays), `aw` (AI battle wins) and `ce` (coins earned in total) use the same single-value layout.

```json
{
  "pack": "example_pack",
  "nameplates": [
    { "key": "regular", "title": "Regular Drummer", "type": 3, "rarity": "Uncommon",
      "unlock": { "condition": "tp", "values": [100] } }
  ]
}
```

**Clear a song of your chart pack (`sp`).** A chart pack can give a nameplate for clearing one of its songs. The reference is the `id` field of the song's `uniqueID.json`, and `[3, 3]` asks for a full combo (3) on Extreme (3).

```json
{
  "pack": "example_charts",
  "nameplates": [
    { "key": "storm_fc", "title": "Storm Chaser", "type": 33, "rarity": "Epic",
      "unlock": { "condition": "sp", "values": [3, 3],
                  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU"] } }
  ]
}
```

**Buy it with coins (`ch`).** The price is the single value. In the shipped skin, the player buys the nameplate from the PC in My Room.

```json
{
  "pack": "example_pack",
  "nameplates": [
    { "key": "golden", "title": "Golden Touch", "type": 11, "rarity": "Rare",
      "unlock": { "condition": "ch", "values": [800] } }
  ]
}
```

**A reward that only a script gives (`ig`).** Play never unlocks an `ig` nameplate, so a script gives it with UnlockNameplate (see Nameplates from a script). Until then, the PC in My Room shows the generic invalid-condition hint for it.

```json
{
  "pack": "example_story",
  "nameplates": [
    { "key": "finale", "title": "Curtain Call", "type": 19, "rarity": "Legendary",
      "unlock": { "condition": "ig", "values": [] } }
  ]
}
```

**A skin pack with translations.** A skin keeps its pack in `System/<skin>/Nameplates/`, with its translations in `System/<skin>/Nameplates/Lang/`.

```json
{
  "pack": "example_skin",
  "nameplates": [
    { "key": "visitor", "title": "First Visit", "type": 22, "rarity": "Common",
      "unlock": { "condition": "tp", "values": [1] } }
  ]
}
```

```json
{
  "visitor": "初めての訪問"
}
```

## Troubleshooting and notes

- The pack does not appear: the folder is not directly inside `Global/Nameplates/`, the file is not named `Nameplates.json`, the `pack` id is missing or uses other characters than lowercase letters, digits and `_`, or another pack already uses the same id. The error notification names the pack.
- A nameplate is missing from the list: it has no `key` or no `title`, or its key repeats an earlier one. `OpenTaiko.log` names the entry.
- A translation does not show: the key in the `Lang` file must be the pack's key without the pack id (`night_sky` for `example_pack:night_sky`), and the file name must be the language code.
- The plate shows no art: the skin has no folder for that `type`.
- Online, other players see the title of a pack nameplate only when they have the same pack. Otherwise they see the player's nameplate without a title.
- If you remove a pack, the plates of players who wore one of its nameplates show no title until the pack is back. Their unlocks stay in the save file.
- The game's JSON reader accepts comments and trailing commas; strict validators reject them.
