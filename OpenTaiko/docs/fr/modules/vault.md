<!-- modules/vault.md -->

# Vault

La chambre forte secrète contient trois coffres, que l'on ouvre avec des clés que le joueur gagne ailleurs (réussites dans la pagode, la boutique de pièces). Les modules `Vault` contiennent les clés, les pools de récompenses des coffres et les clés de sauvegarde ; ils ne dessinent rien. Un skin dessine son propre écran de chambre forte par-dessus, et tous les skins partagent une même chambre forte, parce que son état vit dans le fichier de sauvegarde. Le stage `secret_vault_rw` d'Open-World Memories est l'un de ces clients.

## Les modules

| Module | Ce qu'il contient |
| --- | --- |
| `Vault.Keys` | Les trois clés : leur nombre, leurs noms et leurs images, et si la chambre forte est ouverte. |
| `Vault.Chests` | Les trois coffres, leurs pools de récompenses pondérés et les pop-ups de récompense. |
| `Vault.Texts` | Les chaînes propres aux modules (`lang/<code>/vault.json`). |

## Vault.Keys

Une clé se désigne par son numéro ou son identifiant : 1 `"simple"`, 2 `"gold"`, 3 `"optk"`. Chacune ouvre le coffre du même numéro.

| Fonction | Description |
| --- | --- |
| `Keys.count(key)  -> number` | Combien de clés de ce type possède le joueur 1. |
| `Keys.set(key, n)`, `Keys.add(key, n?)` | Définit le nombre ; ajoute `n` (1 par défaut). |
| `Keys.name(key)  -> string` | Le nom de la clé dans la langue du jeu. |
| `Keys.picture(key)  -> string` | Le chemin absolu de l'image de la clé, pour `TEXTURE:CreateTextureFromAbsolutePath` ou `SHARED:SetSharedTextureUsingAbsolutePath`. |
| `Keys.vaultOpen()  -> bool` | Si la chambre forte est ouverte : le déclencheur `.vault_opened`, que définit le chemin propre au skin vers la chambre forte. Tant qu'elle est fermée, les autres modules donnent des pièces au lieu de clés. |
| `Keys.index(key)  -> number` | Le numéro de la clé. |

## Vault.Chests

Les coffres sont 1 en bois, 2 en or et 3 en platine. En ouvrir un dépense l'une de ses clés et tire une récompense dans son pool pondéré : rien (la clé se brise), des pièces, la clé du coffre suivant, une plaque de nom que le joueur n'a pas (identifiants 361 à 368, 369 à 375, 376 à 380 selon le coffre), un meuble de Ma Chambre (un meuble déjà possédé garde un cinquième de sa probabilité), ou une chanson pas encore gagnée du dossier `Secret Vault` de la bande-son, répartie dans les coffres selon son plus haut niveau de partition.

| Fonction | Description |
| --- | --- |
| `Chests.enter()  -> bool` | Une fois par visite, avant toute autre chose : donne la clé simple gratuite de la première visite (renvoie alors true) et construit les pools de chansons. |
| `Chests.roll(i)  -> reward` | Ouvre le coffre `i` avec l'une de ses clés (vérifiez d'abord `keyCount`), enregistre immédiatement la récompense et la renvoie. |
| `Chests.showReward(reward)  -> bool` | Lance le pop-up de la récompense via la ROActivity `modal` ; false pour une clé brisée ou une clé, qui n'en ont pas. |
| `Chests.keyCount(i)  -> number` | Les clés possédées pour le coffre `i`. |
| `Chests.name(i)  -> string` | Le nom du coffre dans la langue du jeu. |
| `Chests.stock(i)  -> left, total` | Ce qu'il reste à gagner dans le coffre (chansons, plaques de nom, meubles) et son total. |
| `Chests.chestPool(i)  -> table` | Le pool pondéré du coffre tel qu'il se présente sur cette sauvegarde : des entrées `{ w, type, ... }`. |
| `Chests.songPool(i)`, `Chests.maxChartLevel(node)` | Les chansons du coffre ; le plus haut niveau de partition d'une chanson. |

Une récompense est `{ type, openedChest, ... }`, `type` valant `"snap"`, `"coins"` (`amount`), `"key"` (`target`, le numéro du coffre), `"nameplate"` (`id`, `info`), `"furniture"` (`id`, `rarity`) ou `"song"` (`node`). Le pop-up fonctionne comme la ROActivity `modal` : appelez son `Update` et son `Draw` tant que `IsActive` est vrai, comme le font `Pagoda.Rewards.update` et `draw`. Un pop-up de meuble affiche la texture partagée `myroom_thumb_<id>` que fournit [Ma Chambre](my-room.md).

## Clés de sauvegarde

Tout est conservé dans la sauvegarde du joueur 1 (`GetSaveFile(0)`) :

| Clé | Nature | Contenu |
| --- | --- | --- |
| `.vault_key_count_simple`, `.vault_key_count_gold`, `.vault_key_count_optk` | compteurs | Les clés possédées. |
| `.vault_opened` | déclencheur | La chambre forte est ouverte (défini par le skin). |
| `.vault_first_key_given` | déclencheur | La clé de la première visite a été donnée. |
| `.vault_song_unlocked_<unique id>` | déclencheur | Cette chanson de `Secret Vault` a été gagnée. |
| `.myroom_<id>`, `.myroom_held_<id>` | compteurs | Les meubles gagnés : transmis à Ma Chambre, et possédés. |

Les plaques de nom et les pièces passent par les méthodes propres à la sauvegarde (`UnlockNameplate`, `EarnCoins`).

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
