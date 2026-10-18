<!-- modules/my-room.md -->

# Mijn kamer

Mijn kamer is een globale stage, `my_room` in `Global/Modules/Stages/`: de kamer van de speler, waarin hij rondloopt, die hij inricht met wat de pagode, de kluis en de muntwinkel hem geven, en die hij openstelt voor online bezoekers. Elke skin kan haar openen, en elke skin toont dezelfde kamer, omdat haar data in het opslagbestand staat.

## Binnenkomen en vertrekken

Zet de gedeelde string `my_room_return` op de stage waar Mijn kamer naar terug moet, en ga dan naar `my_room`:

```lua
SHARED:SetSharedString("my_room_return", "my_menu")               -- back to my_menu
-- or, with the transition to play on the way back:
SHARED:SetSharedString("my_room_return", "my_menu|my_transition")
return Exit("stage", "my_room")
```

Mijn kamer leest de string bij binnenkomst en leegt hem. Is hij leeg, dan leidt de weg terug naar het titelscherm (`_title`).

De gedeelde string `my_room_place` onthoudt wiens kamer het was en waar de speler stond, over uitstapjes vanuit de kamer en terug: de Placeholder Matrix (een meubelstuk dat stages oplijst om te bezoeken) en de capsule (een meubelstuk dat een andere stage opent) leiden allebei de kamer uit. Een titelscherm kan hem leegmaken (op `""` zetten), zodat het volgende bezoek bij de deur begint.

Open-World Memories houdt een doorverwijsstage, `myroom`, voor de oude aanroepen `Exit("stage", "myroom")` en de exit `"heya"` van de engine; die verdwijnt bij de officiële release, dus nieuwe code hoort naar `my_room` te gaan.

## De data

| Sleutel | Soort | Inhoud |
| --- | --- | --- |
| `my_room` | opslagstring | De kamer, als JSON `{ v, room, jukebox }` (`sf:GetGlobalString("my_room")`). Alleen geschreven voor het eigen slot van het opslagbestand: nooit voor een online bezoeker of een leeg slot. |
| `.myroom_<id>` | teller | Meubels die een andere stage geeft: Mijn kamer voegt ze bij het volgende bezoek één keer toe aan de voorraad van de kamer en houdt bij wat ze al heeft overgenomen. |
| `.myroom_held_<id>` | teller | Hoeveel van dat stuk het opslagbestand bezit. Mijn kamer houdt de teller bij; een stage die een stuk geeft, verhoogt hem ook meteen. |
| `.myroom_iw`, `.myroom_ih`, `.myroom_items` | tellers | De breedte en hoogte van de kamer en het aantal geplaatste stukken, door Mijn kamer geschreven zodat een lijst van opslagbestanden ze kan tonen (alleen lezen). |
| `myroom_thumb_<id>` | gedeelde textuur | De afbeelding van een meubelstuk uit de winkel of uit een beloning, gemaakt wanneer Mijn kamer start, die aankoop- en beloningspop-ups tonen. |

Om vanuit je eigen stage een meubelstuk te geven, tel je 1 op bij beide tellers, zoals [Pagoda.Rewards](pagoda.md) en [Vault.Chests](vault.md) doen. Zolang de string `my_room` niet is ingesteld, leest een tijdelijke migratie de kamer uit de oude store van Open-World Memories; die wordt bij de officiële release verwijderd.

## Waarop het voortbouwt

- De menu's en dialogen gebruiken de UI-bibliotheek van de skin via [UI.Pick](ui.md), gekozen bij het eerste gebruik (PopUI in Open-World Memories, SimpleUI in een skin die er geen noemt), en de muntweergave is de munten-widget van die bibliotheek.
- De strings, inclusief de meubelnamen (`lang/<code>/items.json`, met het meubel-id als sleutel), staan in de eigen map `lang/`, in de negen talen.
- De meubelcatalogus is `data/furniture.json` in de map van de stage.
- De Placeholder Matrix toont alleen de debugstages die in `Global/Modules/Stages/` aanwezig zijn. Die debugstages en de ROActivity `video_player` die de tv gebruikt, zijn ook globale modules.
- De capsule opent de stage `mysterypod` als die in `Global/Modules/Stages/` bestaat, of als de eigen stage `mysterypod` van een skin in zijn `onStart` de gedeelde string `mysterypod_installed` op `"1"` zet.
- Online bezoeken gebruiken het kamer-id `"myroom"` met versie 2 van de payload, net als vóór de verhuizing.
