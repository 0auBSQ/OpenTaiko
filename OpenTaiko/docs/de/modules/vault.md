<!-- modules/vault.md -->

# Vault

Der Geheimtresor enthält drei Truhen, die mit Schlüsseln geöffnet werden, die der Spieler anderswo gewinnt (Pagoden-Clears, der Münzladen). Die `Vault`-Module enthalten die Schlüssel, die Belohnungspools der Truhen und die Spielstand-Schlüssel; sie zeichnen nichts. Ein Skin zeichnet seinen eigenen Tresorbildschirm darauf, und alle Skins teilen sich einen Tresor, weil sein Zustand im Spielstand liegt. Die Stage `secret_vault_rw` von Open-World Memories ist ein solcher Client.

## Die Module

| Modul | Inhalt |
| --- | --- |
| `Vault.Keys` | Die drei Schlüssel: ihre Anzahl, Namen und Bilder, und ob der Tresor offen ist. |
| `Vault.Chests` | Die drei Truhen, ihre gewichteten Belohnungspools und die Belohnungs-Pop-ups. |
| `Vault.Texts` | Die eigenen Strings der Module (`lang/<code>/vault.json`). |

## Vault.Keys

Ein Schlüssel wird über seine Nummer oder seine ID benannt: 1 `"simple"`, 2 `"gold"`, 3 `"optk"`. Jeder öffnet die Truhe mit derselben Nummer.

| Funktion | Beschreibung |
| --- | --- |
| `Keys.count(key)  -> number` | Wie viele dieser Schlüssel Spieler 1 besitzt. |
| `Keys.set(key, n)`, `Keys.add(key, n?)` | Setzt die Anzahl; addiert `n` (standardmäßig 1). |
| `Keys.name(key)  -> string` | Der Name des Schlüssels in der Sprache des Spiels. |
| `Keys.picture(key)  -> string` | Der absolute Pfad des Schlüsselbilds, für `TEXTURE:CreateTextureFromAbsolutePath` oder `SHARED:SetSharedTextureUsingAbsolutePath`. |
| `Keys.vaultOpen()  -> bool` | Ob der Tresor offen ist: der Trigger `.vault_opened`, den der skineigene Weg in den Tresor setzt. Solange er verschlossen ist, vergeben andere Module Münzen statt Schlüsseln. |
| `Keys.index(key)  -> number` | Die Nummer des Schlüssels. |

## Vault.Chests

Die Truhen sind 1 aus Holz, 2 aus Gold und 3 aus Platin. Das Öffnen einer Truhe verbraucht einen ihrer Schlüssel und lost eine Belohnung aus ihrem gewichteten Pool aus: nichts (der Schlüssel bricht), Münzen, den Schlüssel der nächsten Truhe, ein Namensschild, das dem Spieler fehlt (IDs 361 bis 368, 369 bis 375, 376 bis 380 je nach Truhe), ein Möbelstück für Mein Zimmer (ein Stück, das schon im Besitz ist, behält ein Fünftel seiner Chance) oder einen noch nicht gewonnenen Song aus dem Ordner `Secret Vault` des Soundtracks, nach seiner höchsten Chartstufe auf die Truhen verteilt.

| Funktion | Beschreibung |
| --- | --- |
| `Chests.enter()  -> bool` | Einmal pro Besuch, vor allem anderen: vergibt den kostenlosen einfachen Schlüssel des ersten Besuchs (gibt dann true zurück) und baut die Song-Pools auf. |
| `Chests.roll(i)  -> reward` | Öffnet Truhe `i` mit einem ihrer Schlüssel (prüfen Sie zuerst `keyCount`), speichert die Belohnung sofort und gibt sie zurück. |
| `Chests.showReward(reward)  -> bool` | Startet das Pop-up der Belohnung über die ROActivity `modal`; false für einen gebrochenen Schlüssel oder einen Schlüssel, die keines haben. |
| `Chests.keyCount(i)  -> number` | Die für Truhe `i` vorhandenen Schlüssel. |
| `Chests.name(i)  -> string` | Der Name der Truhe in der Sprache des Spiels. |
| `Chests.stock(i)  -> left, total` | Was in der Truhe noch zu gewinnen ist (Songs, Namensschilder, Möbel), und ihr Gesamtbestand. |
| `Chests.chestPool(i)  -> table` | Der gewichtete Pool der Truhe, wie er auf diesem Spielstand aussieht: Einträge `{ w, type, ... }`. |
| `Chests.songPool(i)`, `Chests.maxChartLevel(node)` | Die Songs der Truhe; die höchste Chartstufe eines Songs. |

Eine Belohnung ist `{ type, openedChest, ... }`, wobei `type` `"snap"`, `"coins"` (`amount`), `"key"` (`target`, die Truhennummer), `"nameplate"` (`id`, `info`), `"furniture"` (`id`, `rarity`) oder `"song"` (`node`) ist. Das Pop-up läuft wie die ROActivity `modal`: Rufen Sie ihr `Update` und `Draw` auf, solange `IsActive` gilt, wie es `Pagoda.Rewards.update` und `draw` tun. Ein Möbel-Pop-up zeigt die gemeinsame Textur `myroom_thumb_<id>`, die [Mein Zimmer](my-room.md) bereitstellt.

## Spielstand-Schlüssel

Alles wird im Spielstand von Spieler 1 gespeichert (`GetSaveFile(0)`):

| Schlüssel | Art | Inhalt |
| --- | --- | --- |
| `.vault_key_count_simple`, `.vault_key_count_gold`, `.vault_key_count_optk` | Zähler | Die vorhandenen Schlüssel. |
| `.vault_opened` | Trigger | Der Tresor ist offen (vom Skin gesetzt). |
| `.vault_first_key_given` | Trigger | Der Schlüssel des ersten Besuchs wurde vergeben. |
| `.vault_song_unlocked_<unique id>` | Trigger | Dieser `Secret Vault`-Song wurde gewonnen. |
| `.myroom_<id>`, `.myroom_held_<id>` | Zähler | Gewonnene Möbel: an Mein Zimmer übergeben, und im Besitz. |

Namensschilder und Münzen laufen über die eigenen Methoden des Spielstands (`UnlockNameplate`, `EarnCoins`).

```lua
local Chests = require("Vault.Chests")

function activate()
    if Chests.enter() then showFreeKeyNotice = true end
end

local function open(i)
    if Chests.keyCount(i) < 1 then return end
    local reward = Chests.roll(i)
    if not Chests.showReward(reward) then playSnap(reward) end
end
```
