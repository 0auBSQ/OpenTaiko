<!-- guides/notes-skins.md -->

# Noten- en beoordelingspakketten toevoegen

Een skin kan meerdere looks bieden voor de noten en voor de beoordelingsteksten, en de speler kiest er een in de instellingen. Elke look is een pakket: een genummerde map onder `Graphics/5_Game/` van de skin. Een notenpakket bevat de noten, de roffels, de ballonnen, de raakeffecten en de ballon- en roffelbubbels; een beoordelingspakket bevat de beoordelingsteksten. Deze handleiding legt de mappen uit, de sheets die elk pakket bevat, het bestand `Definition.json` dat een pakket beschrijft, en hoe het spel aanvult wat een pakket weglaat.

## Voordat je begint

- Een eigen skin (zie [Skins en thema's toevoegen](skins.md)). De meegeleverde skin `System/Open-World Memories/` heeft vier Taiko-notenpakketten, één Bongo-notenpakket en vier beoordelingspakketten om van te kopiëren.
- Een afbeeldingseditor die PNG-bestanden met transparantie opslaat.
- De notencelgrootte van je skin: de lay-outwaarde `Game_Notes_Size=` (`195,195` in de meegeleverde skin). Elke cel van de notensheets op deze pagina heeft die grootte, behalve die van de reuzensheet.

## Stap 1: Maak de pakketmappen aan

Het spel zoekt drie mappen onder `Graphics/5_Game/` en behandelt elke genummerde submap daarin als één pakket, gesorteerd op nummer:

- `Notes Skin (Taiko)/`: notenpakketten voor Taiko-spelers.
- `Notes Skin (Bongo)/`: notenpakketten voor Bongo-spelers.
- `Judges Skin/`: pakketten met beoordelingsteksten, gebruikt door elke speler.

Pakket `1` van elke map is de standaard en de basis van de andere (Stap 7). Het mapnummer bepaalt alleen de volgorde van de pakketten; de instellingen tonen de naam uit `Definition.json`.

```
Graphics/5_Game/
  Notes Skin (Taiko)/
    1/                  <- de standaardlook, compleet
      Definition.json
      Notes.png
      ...
    2/                  <- een andere look: alleen de bestanden die verschillen
      Definition.json
      Notes.png
  Notes Skin (Bongo)/
    1/
  Judges Skin/
    1/
      Definition.json
      Judge.png
```

## Stap 2: Teken de Taiko-notensheets

Een sheet heeft één rij per animatieframe (Stap 5 bepaalt het aantal frames), en de cellen staan naast elkaar in de volgorde hieronder.

| Bestand | Inhoud |
| --- | --- |
| `HitCircle.png` | De raakcirkel, één cel. |
| `Notes.png` | Don, Ka, grote Don, grote Ka, KaDon, ADLib. |
| `Rolls.png` | De kop, het lijf en de staart van de kleine roffel, daarna de kop, het lijf en de staart van de grote roffel. Het spel rekt de lijfcel uit over de lengte van de roffel. |
| `Balloons.png` | De ballon (2 cellen breed), de kusudama (2 cellen breed), daarna de kop, het lijf en de staart van de lontroffel. Bongo-spelers zien deze ballonnen ook. |
| `ChipEffect.png` | Optioneel. De flits op de plek waar een geraakte noot landt, met de kolommen van `Notes.png`, één rij. Zonder dit bestand laat het spel de noot zelf flitsen. |
| `Notes_Partner.png` | Optioneel. De partnerlook van elke kolom van `Notes.png` (Stap 6). |
| `Notes_Giant.png` | Optioneel. De reuzenversies van Don, Ka, KaDon en ADLib (Stap 6). |
| `Preview.png` | Optioneel. De afbeelding die de instellingen voor het pakket tonen. Zonder dit bestand tonen ze `Notes.png`. |

Het pakket bevat ook de raakeffecten en de ballon- en roffelbubbels:

| Bestand | Inhoud |
| --- | --- |
| `Hit/Explosion.png` | De uitbarsting boven de raakcirkel, 7 vierkante frames per rij: een rij voor Good, een rij voor OK, en optioneel een rij voor Good en een rij voor OK op grote noten. Met 2 rijen gebruiken grote noten de eerste twee. |
| `Hit/Explosion_Big.png` | De extra uitbarsting boven een grote noot, één afbeelding, getekend voor beide beoordelingen. |
| `Hit/FireWorks.png` | De vonken die een wegvliegende grote noot achterlaat. |
| `Hit/Bomb.png` | De explosie van een geraakte bom of een mislukte lontroffel, 7 frames in één rij. |
| `Hit/Great/`, `Hit/Great_Big/`, `Hit/Good/`, `Hit/Good_Big/` | Optionele framemappen (`0.png` tot `14.png`) die het spel op de baan tekent bij een Good (`Great`) of een OK (`Good`) op een kleine of grote noot. |
| `Kusudama/` | De `Script.lua` van de kusudama en zijn afbeeldingen, waaronder `Kusudama_Number.png`. |
| `Fuseroll/` | `Fuse.png`, de bubbel van de lontroffel, en `Number_Fuse.png`, zijn cijfers. |
| `Breaking.png` | De afbeeldingen van de brekende ballonbubbel: 6 afbeeldingen van dezelfde hoogte, onder elkaar. |
| `Balloon.png` | De ballonbubbel. |
| `Roll.png` | De bubbel van de roffelteller, met zijn animatieframes naast elkaar. |

## Stap 3: Teken een Bongo-notenpakket

Een Bongo-pakket bevat alleen wat voor Bongo-spelers verschilt:

| Bestand | Inhoud |
| --- | --- |
| `Notes.png` | Rood, geel, roze, klap, ADLib. |
| `Rolls.png` | De rode, roze, gele en klaproffels, elk als kop, lijf en staart. |
| `HitCircle.png` | Optioneel. Zonder dit bestand zien Bongo-spelers de raakcirkel van het Taiko-pakket. |
| `ChipEffect.png`, `Notes_Partner.png` | Optioneel, met de kolommen van deze `Notes.png`. |
| `Notes_Giant.png` | Optioneel, met de kolommen van deze `Notes.png`. |
| `Preview.png` | Optioneel. |

De ballonnen, de raakeffecten en de bubbels komen altijd uit het geselecteerde Taiko-pakket.

## Stap 4: Teken een beoordelingspakket

Een beoordelingspakket bevat `Judge.png`, 5 rijen van dezelfde hoogte: Good, OK, Bad, ADLib en de Boom van de bom. Zijn `Definition.json` heeft alleen een `Name` nodig. Laat een rij transparant om die beoordeling te verbergen; de pakketten "(Without Good)" van de meegeleverde skin doen dit voor de rij Good.

## Stap 5: Schrijf Definition.json

`Definition.json` geeft het pakket een naam en beschrijft zijn sheets. Elk veld is optioneel; een pakket zonder dit bestand toont zijn mapnummer als naam en gebruikt de standaardwaarden hieronder.

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

| Veld | Standaard | Betekenis |
| --- | --- | --- |
| `Name` | het mapnummer | De naam in de instellingen, één string per taalcode, met `default` voor elke andere taal. |
| `Notes`, `Rolls`, `Balloons` | 1 frame, geen animatie | Het aantal rijen van de sheet (`Frames`) en zijn animatie (`Animation`). `Notes` bepaalt ook de rijen van `Notes_Partner.png` en `Notes_Giant.png`. |
| `FlyingNotesFrame` | `0` | De rij van `Notes.png` voor de noten die na een slag naar de gauge vliegen. |
| `PartnerNotesScale` | de vierkantswortel van 2 | De schaal van een kleine partnernoot wanneer het pakket geen `Notes_Partner.png` heeft (Stap 6). |
| `GiantNotesScale` | `2` | De schaal van een reuzennoot wanneer het pakket geen `Notes_Giant.png` heeft (Stap 6). |

`Animation` is een lijst van niveaus. Het spel gebruikt het niveau met de hoogste `FromCombo` die de combo van de speler heeft bereikt. Een tel heeft 4 stappen, en `Rows` geeft de rij die bij elke stap getekend wordt; een kortere lijst herhaalt zich, dus `[0, 1]` wisselt bij elke stap en `[0]` staat stil. `RowsOnEntry` vervangt `Rows` gedurende ongeveer twee derde seconde nadat de combo dat niveau bereikt. Zonder niveaus toont de sheet zijn eerste rij. De tabel van het voorbeeld is de animatie die het spel altijd heeft gebruikt, en de Simpele Modus van de instellingen houdt elke sheet op zijn eerste rij.

Twee optionele velden passen een niveau aan. `FromComboByDifficulty` stelt de combo van het niveau in voor sommige moeilijkheidsgraden, met de sleutels `Easy`, `Normal`, `Hard`, `Extreme` en `ExtraExtreme`; de andere moeilijkheidsgraden gebruiken `FromCombo`, dus `"FromCombo": 100, "FromComboByDifficulty": { "Easy": 30 }` laat het niveau in Easy beginnen bij 30 combo en in de andere bij 100. Een Tower-chart telt als Normal aan zijn Normal-kant en als Extreme aan zijn andere kanten, en een Dan gebruikt de moeilijkheidsgraad van het nummer dat gespeeld wordt.

`RowsByNote` geeft sommige noten hun eigen `Rows`, met de namen hieronder als sleutels; de andere noten houden `Rows`. In `Notes.png` wisselt `"Rows": [0, 2], "RowsByNote": { "BigDon": [1, 2] }` de rijen 0 en 2 af op elke noot behalve de grote Don, die de rijen 1 en 2 afwisselt. Het lijf en de staart van een roffel volgen zijn kop, en `RowsOnEntry` geldt nog steeds voor elke noot.

| Sheet | Nootnamen |
| --- | --- |
| `Notes.png`, Taiko | `Don`, `Ka`, `BigDon`, `BigKa`, `KaDon`, `ADLib` |
| `Notes.png`, Bongo | `Red`, `Yellow`, `Pink`, `Clap`, `ADLib` |
| `Rolls.png`, Taiko | `Roll`, `BigRoll` |
| `Rolls.png`, Bongo | `RedRoll`, `PinkRoll`, `YellowRoll`, `ClapRoll` |
| `Balloons.png` | `Balloon`, `Kusudama`, `FuseRoll` |

## Stap 6: Partner- en reuzennoten

Een partnernoot houdt in de meeste gevallen zijn grootte, maar de kleine (een kleine Don of Ka, een ADLib, een bom, elke Bongo-noot) krijgen een grote look. Een reuzennoot is veel groter en wiebelt langzaam. Elk heeft een eigen optionele sheet:

- `Notes_Partner.png` heeft de kolommen van `Notes.png` en evenveel rijen. Een kleine partnernoot gebruikt de cel van zijn eigen kolom.
- `Notes_Giant.png` heeft evenveel rijen als `Notes.png`. In een Taiko-pakket zijn de kolommen Don, Ka, KaDon en ADLib, omdat een reuzennoot er groot uitziet, wat zijn grootte ook is: een kleine Don en een grote Don gebruiken allebei de Don-cel. In een Bongo-pakket zijn de kolommen die van `Notes.png`. De celgrootte is de breedte van de sheet gedeeld door het aantal kolommen, dus een reuzencel kan groter zijn dan een notencel.

Zonder deze sheets bouwt het spel de looks op uit `Notes.png`:

| Noot | Partner, zonder `Notes_Partner.png` | Reus, zonder `Notes_Giant.png` |
| --- | --- | --- |
| Kleine Taiko-Don of -Ka | De grote Don of Ka | De grote Don of Ka, maal `GiantNotesScale` |
| Elke andere kleine noot | De noot, maal `PartnerNotesScale` | De noot, maal `PartnerNotesScale` en `GiantNotesScale` |
| Grote Don, grote Ka, KaDon | Ongewijzigd | De noot, maal `GiantNotesScale` |

Stel beide schalen in, ook wanneer het pakket beide sheets heeft: een skinner die het pakket kopieert en een sheet verwijdert, krijgt dan een redelijke grootte. Een geschaalde noot is een uitgerekte afbeelding en ziet er zachter uit dan een cel die op ware grootte is getekend.

<div class="callout warn">Het spel leest Notes_Partner.png, Notes_Giant.png en ChipEffect.png alleen uit het geselecteerde pakket. Ze moeten bij de eigen noten van het pakket passen, dus de kopieën van pakket 1 vullen ze nooit aan.</div>

## Stap 7: Laat pakket 1 de rest aanvullen

Wanneer het geselecteerde pakket een bestand mist, neemt het spel het uit pakket 1 van dezelfde map. Een pakket kan dus alleen bevatten wat het verandert: een pakket dat alleen een rij van `Hit/Explosion.png` leegmaakt, bevat een `Definition.json` en dat ene bestand. De regels:

- Het spel controleert elk bestand afzonderlijk, ook binnen mappen (`Hit/Great/3.png`). De kusudama is de uitzondering: een pakket zonder eigen map `Kusudama/` gebruikt de hele map van pakket 1.
- De terugval reikt alleen tot pakket 1. Een pakket dat een variant van pakket 3 is, moet eigen kopieën van de sheets van pakket 3 bevatten.
- Het spel neemt `Definition.json` nooit uit pakket 1, en de drie sheets van Stap 6 ook niet.
- Een Bongo-pakket valt terug op Bongo-pakket 1.

## Stap 8: Kies een pakket

Wanneer een skin minstens 2 pakketten in een van de drie mappen heeft, toont de themapagina van de instellingen eerst een sectie Noot-skins, met één keuze voor die map en de preview van het pakket eronder. Het spel laadt het gekozen pakket meteen en onthoudt de keuze voor de skin in `ThemeSettings.db3`. Met één pakket verschijnt de keuze niet, en gebruikt het spel dat pakket.

Een Lua-module leest een bestand van het geselecteerde pakket met `THEME:GetNotesSkinFile(kind, file)` (zie [Spelers en profielen](../api/players.md)). De meegeleverde nummerselectie gebruikt het om de beoordelingsteksten van haar replaylijst te tekenen:

```lua
local file = THEME:GetNotesSkinFile("judges", "Judge.png")   -- "5_Game/Judges Skin/2/Judge.png", of ""
if file == "" then file = "5_Game/Judge.png" end              -- een skin zonder beoordelingspakketten
```

## Skins zonder pakketten

Een skin zonder map `Notes Skin (Taiko)/` houdt de bestanden die hij had voordat pakketten bestonden: `Notes.png` (de raakcirkel, de 4 noten, de roffels en de ballon), `Swap.png` (KaDon), `Adlib.png`, `Kusu.png`, `FuseRoll.png`, `ChipEffect.png`, de map `10_Effects/Hit/` en de ballonbestanden van `11_Balloon/`. Zonder map `Notes Skin (Bongo)/` komen de Bongo-noten en -roffels uit `Notes_Konga.png`, en zonder map `Judges Skin/` komen de beoordelingen uit `5_Game/Judge.png`. Deze bestanden hebben geen partner- of reuzensheets, dus partner- en reuzennoten gebruiken de geschaalde looks van Stap 6.

## Probleemoplossing en opmerkingen

- De instellingen tonen de keuze niet: de map bevat één pakket, of de pakketmappen hebben geen nummers als naam.
- Een pakket toont de noten van een ander pakket: de terugval nam het bestand van pakket 1 omdat het pakket het mist, of de bestandsnaam verschilt in hoofdletters op een hoofdlettergevoelig systeem.
- Noten tonen het verkeerde frame of een afgesneden afbeelding: `Frames` komt niet overeen met de hoogte van de sheet gedeeld door de celhoogte.
- De partner- of reuzennoten van een gekopieerd pakket zien eruit als de oude: de kopie heeft `Notes_Partner.png` of `Notes_Giant.png` van het origineel behouden.
- Bongo-spelers zien geen noten: `Notes.png` ontbreekt zowel in het geselecteerde Bongo-pakket als in Bongo-pakket 1.
