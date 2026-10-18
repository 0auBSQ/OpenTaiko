<!-- modules/my-room.md -->

# Ma Chambre

Ma Chambre est un stage global, `my_room` dans `Global/Modules/Stages/` : la chambre du joueur, où il se promène, qu'il meuble avec ce que lui donnent la pagode, la chambre forte et la boutique de pièces, et qu'il ouvre aux visiteurs en ligne. Tout skin peut l'ouvrir, et tous les skins montrent la même chambre, parce que ses données vivent dans le fichier de sauvegarde.

## Entrer et sortir

Définissez la chaîne partagée `my_room_return` sur le stage vers lequel Ma Chambre doit revenir, puis sortez vers `my_room` :

```lua
SHARED:SetSharedString("my_room_return", "my_menu")               -- back to my_menu
-- or, with the transition to play on the way back:
SHARED:SetSharedString("my_room_return", "my_menu|my_transition")
return Exit("stage", "my_room")
```

Ma Chambre lit la chaîne et la vide à l'entrée. Quand elle est vide, le chemin du retour mène à l'écran titre (`_title`).

La chaîne partagée `my_room_place` retient à qui était la chambre et où se tenait le joueur, lors des allers-retours hors de la chambre : la Placeholder Matrix (un meuble qui liste des stages à visiter) et la capsule (un meuble qui ouvre un autre stage) mènent toutes deux hors de la chambre. Un écran titre peut la vider (la définir à `""`) pour que la visite suivante commence à la porte.

Open-World Memories garde un stage relais, `myroom`, pour les anciens appels `Exit("stage", "myroom")` et la sortie `"heya"` du moteur ; il disparaît à la sortie officielle, donc le nouveau code doit sortir vers `my_room`.

## Ses données

| Clé | Nature | Contenu |
| --- | --- | --- |
| `my_room` | chaîne de sauvegarde | La chambre, en JSON `{ v, room, jukebox }` (`sf:GetGlobalString("my_room")`). Écrite pour l'emplacement propre à la sauvegarde seulement : jamais pour un visiteur en ligne ni pour un emplacement vide. |
| `.myroom_<id>` | compteur | Les meubles qu'un autre stage donne : Ma Chambre les ajoute une fois au stock de la chambre, à la visite suivante, et tient un registre de ce qu'elle a déjà pris. |
| `.myroom_held_<id>` | compteur | Combien d'exemplaires de ce meuble possède la sauvegarde. Ma Chambre le tient à jour ; un stage qui donne un meuble l'augmente aussi immédiatement. |
| `.myroom_iw`, `.myroom_ih`, `.myroom_items` | compteurs | La largeur et la hauteur de la chambre et le nombre de meubles placés, écrits par Ma Chambre pour qu'une liste de sauvegardes les affiche (lecture seule). |
| `myroom_thumb_<id>` | texture partagée | L'image d'un meuble de boutique ou de récompense, créée au démarrage de Ma Chambre, qu'affichent les pop-ups d'achat et de récompense. |

Pour donner un meuble depuis votre propre stage, ajoutez 1 aux deux compteurs, comme le font [Pagoda.Rewards](pagoda.md) et [Vault.Chests](vault.md). Tant que la chaîne `my_room` n'est pas définie, une migration temporaire lit la chambre depuis l'ancien magasin d'Open-World Memories ; elle sera retirée à la sortie officielle.

## Sur quoi elle repose

- Ses menus et boîtes de dialogue utilisent la bibliothèque d'interface du skin via [UI.Pick](ui.md), choisie à la première utilisation (PopUI dans Open-World Memories, SimpleUI dans un skin qui n'en désigne aucune), et son affichage des pièces est le widget de pièces de cette bibliothèque.
- Ses chaînes, noms de meubles compris (`lang/<code>/items.json`, indexés par identifiant de meuble), se trouvent dans son propre dossier `lang/`, dans les neuf langues.
- Le catalogue des meubles est `data/furniture.json` dans le dossier du stage.
- La Placeholder Matrix ne liste que les stages de débogage présents dans `Global/Modules/Stages/`. Ces stages de débogage et la ROActivity `video_player` qu'utilise la télévision sont eux aussi des modules globaux.
- La capsule ouvre le stage `mysterypod` quand il existe dans `Global/Modules/Stages/`, ou quand le stage `mysterypod` propre à un skin définit la chaîne partagée `mysterypod_installed` à `"1"` dans son `onStart`.
- Les visites en ligne utilisent l'identifiant de chambre `"myroom"` avec la version 2 de sa charge utile, comme avant le déménagement.
