<!-- modules/my-room.md -->

# My Room

My Room is a global stage, `my_room` in `Global/Modules/Stages/`: the player's room, which they walk around, furnish with what the pagoda, the vault and the coin shop give them, and open to online visitors. Any skin can open it, and every skin shows the same room, because its data lives in the save file.

## Entering and leaving

Set the shared string `my_room_return` to the stage My Room should go back to, then exit to `my_room`:

```lua
SHARED:SetSharedString("my_room_return", "my_menu")               -- back to my_menu
-- or, with the transition to play on the way back:
SHARED:SetSharedString("my_room_return", "my_menu|my_transition")
return Exit("stage", "my_room")
```

My Room reads the string and clears it on entry. When it is empty, the way back is the title screen (`_title`).

The shared string `my_room_place` remembers whose room it was and where the player stood, across trips from the room and back: the placeholder matrix (a piece of furniture that lists stages to visit) and the pod (one that opens another stage) both lead out of the room. A title screen can clear it (set it to `""`) so the next visit starts at the door.

Open-World Memories keeps a forwarder stage, `myroom`, for the old `Exit("stage", "myroom")` calls and the engine's `"heya"` exit; it goes away at the official release, so new code should exit to `my_room`.

## Its data

| Key | Kind | Holds |
| --- | --- | --- |
| `my_room` | save string | The room, as JSON `{ v, room, jukebox }` (`sf:GetGlobalString("my_room")`). Written for the save's own slot only: never for an online visitor or an empty slot. |
| `.myroom_<id>` | counter | Furniture another stage gives: My Room adds it to the room's stock once, on the next visit, and keeps a ledger of what it already took. |
| `.myroom_held_<id>` | counter | How many of that piece the save owns. My Room keeps it up to date; a stage that gives a piece raises it at once too. |
| `.myroom_iw`, `.myroom_ih`, `.myroom_items` | counters | The room's width and height and the number of placed pieces, written by My Room for a save list to show (read them only). |
| `myroom_thumb_<id>` | shared texture | The picture of a shop or reward piece of furniture, made when My Room starts, which purchase and reward pop-ups show. |

To give a piece of furniture from your own stage, add 1 to both counters, as [Pagoda.Rewards](pagoda.md) and [Vault.Chests](vault.md) do. While the `my_room` string is unset, a temporary migration reads the room from Open-World Memories' old store; it is removed at the official release.

## What it builds on

- Its menus and dialogs use the skin's UI library through [UI.Pick](ui.md), chosen on first use (PopUI in Open-World Memories, SimpleUI in a skin that names none), and its coin display is that library's coins widget.
- Its strings, the furniture names included (`lang/<code>/items.json`, keyed by furniture id), live in its own `lang/` folder, in the nine languages.
- The furniture catalogue is `data/furniture.json` in the stage's folder.
- The placeholder matrix lists only the debug stages present in `Global/Modules/Stages/`. These debug stages and the `video_player` ROActivity the TV uses are global modules too.
- The pod opens the stage `mysterypod` when it exists in `Global/Modules/Stages/`, or when a skin's own `mysterypod` stage sets the shared string `mysterypod_installed` to `"1"` in its `onStart`.
- Online visits use the room id `"myroom"` with version 2 of its payload, the same as before the move.
