<!-- guides/notes-skins.md -->

# Adding Notes and Judges Packs

A skin can offer several looks for the notes and for the judgement texts, and the player picks one in the settings. Each look is a pack: a numbered folder under the skin's `Graphics/5_Game/`. A notes pack holds the notes, the rolls, the balloons, the hit effects and the balloon and roll bubbles; a judges pack holds the judgement texts. This guide explains the folders, the sheets each pack contains, the `Definition.json` file that describes a pack, and how the game fills in what a pack leaves out.

## Before you start

- A skin of your own (see [Adding skins and themes](skins.md)). The shipped skin `System/Open-World Memories/` has four Taiko notes packs, one Bongo notes pack and four judges packs to copy from.
- An image editor that saves PNG files with transparency.
- The note cell size of your skin: the `Game_Notes_Size=` layout value (`195,195` in the shipped skin). Every cell of the notes sheets on this page has that size, except the giant sheet's.

## Step 1: Create the pack folders

The game looks for three folders under `Graphics/5_Game/`, and treats each numbered subfolder inside them as one pack, sorted by number:

- `Notes Skin (Taiko)/`: notes packs for Taiko players.
- `Notes Skin (Bongo)/`: notes packs for Bongo players.
- `Judges Skin/`: judgement text packs, used by every player.

Pack `1` of each folder is the default and the base of the others (Step 7). The folder number only orders the packs; the settings show the name from `Definition.json`.

```
Graphics/5_Game/
  Notes Skin (Taiko)/
    1/                  <- the default look, complete
      Definition.json
      Notes.png
      ...
    2/                  <- another look: only the files that differ
      Definition.json
      Notes.png
  Notes Skin (Bongo)/
    1/
  Judges Skin/
    1/
      Definition.json
      Judge.png
```

## Step 2: Draw the Taiko notes sheets

A sheet has one row per animation frame (Step 5 sets the frame counts), and its cells sit side by side in the order below.

| File | Content |
| --- | --- |
| `HitCircle.png` | The hit circle, one cell. |
| `Notes.png` | Don, Ka, big Don, big Ka, KaDon, ADLib. |
| `Rolls.png` | The small roll's head, body and tail, then the big roll's head, body and tail. The game stretches the body cell along the roll. |
| `Balloons.png` | The balloon (2 cells wide), the kusudama (2 cells wide), then the fuse roll's head, body and tail. Bongo players see these balloons too. |
| `ChipEffect.png` | Optional. The flash shown where a hit note lands, with the columns of `Notes.png`, one row. Without it, the game flashes the note itself. |
| `Notes_Partner.png` | Optional. The partner look of each `Notes.png` column (Step 6). |
| `Notes_Giant.png` | Optional. The giant Don, Ka, KaDon and ADLib (Step 6). |
| `Preview.png` | Optional. The picture the settings show for the pack. Without it, they show `Notes.png`. |

The pack also holds the hit effects and the balloon and roll bubbles:

| File | Content |
| --- | --- |
| `Hit/Explosion.png` | The burst over the hit circle, 7 square frames per row: a row for Good, a row for OK, then optionally a row for Good and a row for OK on big notes. With 2 rows, big notes use the first two. |
| `Hit/Explosion_Big.png` | The extra burst over a big note, one picture, drawn for both judgements. |
| `Hit/FireWorks.png` | The sparks a flying big note leaves behind. |
| `Hit/Bomb.png` | The explosion of a hit bomb or a failed fuse roll, 7 frames in one row. |
| `Hit/Great/`, `Hit/Great_Big/`, `Hit/Good/`, `Hit/Good_Big/` | Optional frame folders (`0.png` to `14.png`) drawn on the lane for a Good (`Great`) or an OK (`Good`) on a small or big note. |
| `Kusudama/` | The kusudama's `Script.lua` and its pictures, including `Kusudama_Number.png`. |
| `Fuseroll/` | `Fuse.png`, the fuse roll's bubble, and `Number_Fuse.png`, its digits. |
| `Breaking.png` | The balloon bubble's breaking pictures: 6 pictures of the same height, one above the other. |
| `Balloon.png` | The balloon bubble. |
| `Roll.png` | The roll counter's bubble, its animation frames side by side. |

## Step 3: Draw a Bongo notes pack

A Bongo pack holds only what differs for Bongo players:

| File | Content |
| --- | --- |
| `Notes.png` | Red, yellow, pink, clap, ADLib. |
| `Rolls.png` | The red, pink, yellow and clap rolls, each as head, body and tail. |
| `HitCircle.png` | Optional. Without it, Bongo players see the Taiko pack's hit circle. |
| `ChipEffect.png`, `Notes_Partner.png` | Optional, with the columns of this `Notes.png`. |
| `Notes_Giant.png` | Optional, with the columns of this `Notes.png`. |
| `Preview.png` | Optional. |

The balloons, the hit effects and the bubbles always come from the selected Taiko pack.

## Step 4: Draw a judges pack

A judges pack holds `Judge.png`, 5 rows of the same height: Good, OK, Bad, ADLib and the bomb's Boom. Its `Definition.json` only needs a `Name`. Leave a row transparent to hide that judgement; the shipped skin's "(Without Good)" packs do this for the Good row.

## Step 5: Write Definition.json

`Definition.json` names the pack and describes its sheets. Every field is optional; a pack without the file shows its folder number as its name and uses the defaults below.

```json
{
	"Name": { "strings": { "default": "Sky Spirits", "ja": "スカイスピリッツ" } },
	"Notes": {
		"Frames": 3,
		"Animation": [
			{ "FromCombo": 0, "Rows": [0] },
			{ "FromCombo": 50, "Rows": [1, 1, 0, 0], "RowsOnEntry": [0] },
			{ "FromCombo": 150, "Rows": [0, 1] },
			{ "FromCombo": 300, "Rows": [0, 2], "RowsOnEntry": [0, 1] }
		]
	},
	"Rolls": { "Frames": 3, "Animation": [] },
	"Balloons": { "Frames": 3, "Animation": [] },
	"FlyingNotesFrame": 1,
	"PartnerNotesScale": 1.4142,
	"GiantNotesScale": 2.0
}
```

| Field | Default | Meaning |
| --- | --- | --- |
| `Name` | the folder number | The name in the settings, one string per language code, with `default` for every other language. |
| `Notes`, `Rolls`, `Balloons` | 1 frame, no animation | The sheet's row count (`Frames`) and its animation (`Animation`). `Notes` also sets the rows of `Notes_Partner.png` and `Notes_Giant.png`. |
| `FlyingNotesFrame` | `0` | The `Notes.png` row of the notes that fly to the gauge after a hit. |
| `PartnerNotesScale` | the square root of 2 | The scale of a small partner note when the pack has no `Notes_Partner.png` (Step 6). |
| `GiantNotesScale` | `2` | The scale of a giant note when the pack has no `Notes_Giant.png` (Step 6). |

`Animation` is a list of tiers. The game uses the tier with the highest `FromCombo` that the player's combo has reached. A beat has 4 steps, and `Rows` gives the row drawn at each step; a shorter list repeats, so `[0, 1]` alternates on every step and `[0]` stays still. `RowsOnEntry` replaces `Rows` for about two thirds of a second after the combo reaches that tier. Without tiers, the sheet shows its first row. The example's table is the animation the game has always used, and the simple mode of the settings keeps every sheet on its first row.

## Step 6: Partner and giant notes

A partner note keeps its size in most cases, but the small ones (a small Don or Ka, an ADLib, a bomb, every Bongo note) take a big look. A giant note is much larger and slowly wobbles. Each has its own optional sheet:

- `Notes_Partner.png` has the columns of `Notes.png` and as many rows. A small partner note uses the cell of its own column.
- `Notes_Giant.png` has as many rows as `Notes.png`. In a Taiko pack its columns are Don, Ka, KaDon and ADLib, because a giant note looks big whatever its size: a small Don and a big Don both use the Don cell. In a Bongo pack its columns are those of `Notes.png`. The cell size is the sheet's width divided by its column count, so a giant cell can be larger than a note cell.

Without these sheets, the game builds the looks from `Notes.png`:

| Note | Partner, without `Notes_Partner.png` | Giant, without `Notes_Giant.png` |
| --- | --- | --- |
| Taiko small Don or Ka | The big Don or Ka | The big Don or Ka, times `GiantNotesScale` |
| Any other small note | The note, times `PartnerNotesScale` | The note, times `PartnerNotesScale` and `GiantNotesScale` |
| Big Don, big Ka, KaDon | Unchanged | The note, times `GiantNotesScale` |

Set both scales even when the pack has both sheets: a skinner who copies the pack and removes a sheet then gets a sensible size. A scaled note is a stretched picture and looks softer than a cell drawn at size.

<div class="callout warn">The game reads Notes_Partner.png, Notes_Giant.png and ChipEffect.png from the selected pack only. They must match the pack's own notes, so pack 1's copies never fill in for them.</div>

## Step 7: Let pack 1 fill in the rest

When the selected pack lacks a file, the game takes it from pack 1 of the same folder. A pack can therefore hold only what it changes: a pack that only clears a row of `Hit/Explosion.png` contains a `Definition.json` and that one file. The rules:

- The game checks each file on its own, inside folders too (`Hit/Great/3.png`). The kusudama is the exception: a pack without its own `Kusudama/` folder uses pack 1's whole folder.
- The fallback only reaches pack 1. A pack that varies pack 3 must carry its own copies of pack 3's sheets.
- `Definition.json` is never taken from pack 1, and neither are the three sheets of Step 6.
- A Bongo pack falls back to Bongo pack 1.

## Step 8: Choose a pack

When a skin has at least 2 packs in one of the three folders, the Theme page of the settings shows a Notes Skins section first, with one choice for that folder and the pack's preview under it. The game loads the chosen pack at once and remembers the choice for the skin in `ThemeSettings.db3`. With a single pack the choice does not appear, and the game uses that pack.

A Lua module reads a file of the selected pack with `THEME:GetNotesSkinFile(kind, file)` (see [Players and profiles](../api/players.md)). The shipped song select uses it to draw the judgement texts of its replay list:

```lua
local file = THEME:GetNotesSkinFile("judges", "Judge.png")   -- "5_Game/Judges Skin/2/Judge.png", or ""
if file == "" then file = "5_Game/Judge.png" end              -- a skin without judges packs
```

## Skins without packs

A skin without a `Notes Skin (Taiko)/` folder keeps the files it had before packs existed: `Notes.png` (the hit circle, the 4 notes, the rolls and the balloon), `Swap.png` (KaDon), `Adlib.png`, `Kusu.png`, `FuseRoll.png`, `ChipEffect.png`, the `10_Effects/Hit/` folder and the balloon files of `11_Balloon/`. Without a `Notes Skin (Bongo)/` folder, Bongo notes and rolls come from `Notes_Konga.png`, and without a `Judges Skin/` folder the judgements come from `5_Game/Judge.png`. These files have no partner or giant sheets, so partner and giant notes use the scaled looks of Step 6.

## Troubleshooting and notes

- The settings do not show the choice: the folder holds a single pack, or the pack folders are not named with numbers.
- A pack shows another pack's notes: the fallback took pack 1's file because the pack lacks it, or the file name differs in case on a case-sensitive system.
- Notes show the wrong frame or a cut picture: `Frames` does not match the sheet's height divided by the cell height.
- The partner or giant notes of a copied pack look like the old ones: the copy kept `Notes_Partner.png` or `Notes_Giant.png` from the original.
- Bongo players see no notes: `Notes.png` is missing from both the selected Bongo pack and Bongo pack 1.
