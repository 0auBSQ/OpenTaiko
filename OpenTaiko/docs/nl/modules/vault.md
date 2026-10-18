<!-- modules/vault.md -->

# Vault

De geheime kluis bevat drie kisten, die je opent met sleutels die de speler elders wint (clears in de pagode, de muntwinkel). De `Vault`-modules bevatten de sleutels, de beloningspools van de kisten en de opslagsleutels; ze tekenen niets. Een skin tekent zijn eigen kluisscherm erbovenop, en alle skins delen één kluis, omdat de status ervan in het opslagbestand staat. De stage `secret_vault_rw` van Open-World Memories is zo'n client.

## De modules

| Module | Wat het bevat |
| --- | --- |
| `Vault.Keys` | De drie sleutels: hun aantallen en namen, en of de kluis open is. |
| `Vault.Chests` | De drie kisten, hun gewogen beloningspools en de beloningspop-ups. |
| `Vault.Texts` | De eigen strings van de modules (`lang/<code>/vault.json`). |

## Vault.Keys

Een sleutel wordt aangeduid met zijn nummer of zijn id: 1 `"simple"`, 2 `"gold"`, 3 `"optk"`. Elke sleutel opent de kist met hetzelfde nummer.

| Functie | Beschrijving |
| --- | --- |
| `Keys.count(key)  -> number` | Hoeveel van die sleutel speler 1 heeft. |
| `Keys.set(key, n)`, `Keys.add(key, n?)` | Stelt het aantal in; telt `n` op (standaard 1). |
| `Keys.name(key)  -> string` | De naam van de sleutel in de taal van het spel. |
| `Keys.vaultOpen()  -> bool` | Of de kluis open is: de trigger `.vault_opened`, die de eigen weg van de skin naar de kluis zet. Zolang ze dicht is, geven andere modules munten in plaats van sleutels. |
| `Keys.index(key)  -> number` | Het nummer van de sleutel. |

## Vault.Chests

De kisten zijn 1 van hout, 2 van goud en 3 van platina. Een kist openen kost een van haar sleutels en trekt één beloning uit haar gewogen pool: niets (de sleutel breekt), munten, de sleutel van de volgende kist, een naamplaatje dat de speler mist (id's 361 tot 368, 369 tot 375, 376 tot 380 per kist), een meubelstuk voor Mijn kamer (een stuk dat al in bezit is, houdt een vijfde van zijn kans), of een nog niet gewonnen nummer uit de map `Secret Vault` van de soundtrack, over de kisten verdeeld op zijn hoogste chartniveau.

| Functie | Beschrijving |
| --- | --- |
| `Chests.enter()  -> bool` | Eén keer per bezoek, vóór al het andere: geeft de gratis eenvoudige sleutel van het eerste bezoek (geeft dan true terug) en bouwt de nummerpools op. |
| `Chests.roll(i)  -> reward` | Opent kist `i` met een van haar sleutels (controleer eerst `keyCount`), slaat de beloning meteen op en geeft die terug. |
| `Chests.showReward(reward)  -> bool` | Start de pop-up van de beloning via de ROActivity `modal`; false voor een gebroken sleutel of een sleutel, die er geen hebben. |
| `Chests.keyCount(i)  -> number` | De sleutels die voor kist `i` in bezit zijn. |
| `Chests.name(i)  -> string` | De naam van de kist in de taal van het spel. |
| `Chests.stock(i)  -> left, total` | Wat er in de kist nog te winnen valt (nummers, naamplaatjes, meubels) en het totaal. |
| `Chests.chestPool(i)  -> table` | De gewogen pool van de kist zoals die op dit opslagbestand is: items `{ w, type, ... }`. |
| `Chests.songPool(i)`, `Chests.maxChartLevel(node)` | De nummers van de kist; het hoogste chartniveau van een nummer. |

Een beloning is `{ type, openedChest, ... }`, waarbij `type` `"snap"`, `"coins"` (`amount`), `"key"` (`target`, het kistnummer), `"nameplate"` (`id`, `info`), `"furniture"` (`id`, `rarity`) of `"song"` (`node`) is. De pop-up loopt zoals de ROActivity `modal`: roep haar `Update` en `Draw` aan zolang `IsActive` geldt, zoals `Pagoda.Rewards.update` en `draw` doen. Een meubelpop-up toont de gedeelde textuur `myroom_thumb_<id>` die [Mijn kamer](my-room.md) levert.

## Opslagsleutels

Alles wordt bewaard in het opslagbestand van speler 1 (`GetSaveFile(0)`):

| Sleutel | Soort | Inhoud |
| --- | --- | --- |
| `.vault_key_count_simple`, `.vault_key_count_gold`, `.vault_key_count_optk` | tellers | De sleutels in bezit. |
| `.vault_opened` | trigger | De kluis is open (gezet door de skin). |
| `.vault_first_key_given` | trigger | De sleutel van het eerste bezoek is gegeven. |
| `.vault_song_unlocked_<unique id>` | trigger | Dat `Secret Vault`-nummer is gewonnen. |
| `.myroom_<id>`, `.myroom_held_<id>` | tellers | Gewonnen meubels: doorgegeven aan Mijn kamer, en in bezit. |

Naamplaatjes en munten lopen via de eigen methoden van het opslagbestand (`UnlockNameplate`, `EarnCoins`).

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
