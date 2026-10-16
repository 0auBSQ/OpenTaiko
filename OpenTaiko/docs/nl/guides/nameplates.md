<!-- guides/nameplates.md -->

# Naamplaatjes toevoegen

Een naamplaatjestitel is het tekstplaatje dat op het naamplaatje van een speler staat, samen met de spelersnaam en de dan-titel. Spelers ontgrendelen titels door te spelen, door ze te kopen of door ze van een script te krijgen, en rusten er een uit in My Room. Je kunt eigen titels toevoegen met een naamplaatjespakket: een map met een bestand `Nameplates.json` die je in `Global/Nameplates/` plaatst. Een skin kan een eigen pakket meeleveren. Het spel zet de naamplaatjes van elk pakket naast de meegeleverde in de lijst, in elk scherm dat naamplaatjes toont.

## Voordat je begint

- Een teksteditor die UTF-8 opslaat.
- Een pakket-id: een korte naam van kleine letters, cijfers en `_` die geen ander pakket gebruikt, bijvoorbeeld je eigen naam of de naam van je chartpakket. Het spel bouwt daaruit het id van elk naamplaatje in het pakket.
- Het spel leest de pakketten bij het opstarten en wanneer het een skin laadt. Herstart het spel na elke wijziging om die te zien.

## Stap 1: Maak de pakketmap

Maak een map in `Global/Nameplates/` (de map `Global` staat naast het spel). De mapnaam kies je zelf. Het pakket bevat één vereist bestand en een optionele map `Lang/` voor vertalingen (Stap 3):

```
Global/
  Nameplates/
    Example Pack/
      Nameplates.json
      Lang/
        fr.json
        ja.json
```

Een skin levert zijn pakket op dezelfde manier mee, als map `Nameplates/` in de skinroot (`System/<skin>/Nameplates/Nameplates.json`). Het spel laadt alleen het pakket van de actieve skin, dus de naamplaatjes van een skinpakket verdwijnen zolang een andere skin actief is.

## Stap 2: Schrijf Nameplates.json

`Nameplates.json` bevat het pakket-id en de lijst met naamplaatjes:

- `pack` (string, vereist): het pakket-id.
- `name` (object, optioneel): de naam van het pakket, die de pakketbalk van de pc in My Room toont, als `{ "strings": { "default": "...", "<code>": "..." } }`: de pakketbalk toont de naam van de huidige taal, anders `default`. Zonder naam toont de pakketbalk het pakket-id.
- `nameplates` (array, vereist): één object per naamplaatje, met deze velden:
  - `key` (string, vereist): een naam voor het naamplaatje, uniek binnen het pakket. Het volledige id van het naamplaatje is `<pack>:<key>`, bijvoorbeeld `example_pack:night_sky`. Opslagbestanden bewaren dit id, dus houd een sleutel ongewijzigd zodra spelers het naamplaatje kunnen hebben, en geef hem nooit aan een ander naamplaatje.
  - `title` (string, vereist): de titeltekst. Elke taal zonder vertaling toont die.
  - `type` (int, standaard 0): de afbeelding van het plaatje, een mapnummer in de naamplaatjestexturen van de skin. In de meegeleverde skin zijn de mappen `Modules/ROActivities/nameplate/Textures/Title/0` tot `45`, en voor een nummer zonder map tekent de skin geen plaatje. De tabel hieronder toont de plaatjes van de meegeleverde skin.
  - `rarity` (string, standaard `Common`): de naam van de zeldzaamheid (zie de tabel hieronder). Een onbekende naam wordt `Common`.
  - `unlock` (object, optioneel): de ontgrendelvoorwaarde, met dezelfde velden als in een `Unlock.json`: `condition`, `values`, `type` en `references` (de [Referentie voor ontgrendelvoorwaarden](unlockables-reference.md) geeft de voorwaarde-id's). Zonder `unlock` bezit elke speler het naamplaatje vanaf het begin.

Zeldzaamheidswaarden (de sterren zijn die van de meegeleverde skin):

| Zeldzaamheid | Sterren op het plaatje | Meldingsniveau |
| --- | --- | --- |
| <span class="rarity rarity-poor">Poor</span> | 0 | 0 |
| <span class="rarity rarity-common">Common</span> | 0 | 0 |
| <span class="rarity rarity-uncommon">Uncommon</span> | 0 | 1 |
| <span class="rarity rarity-rare">Rare</span> | 1 | 2 |
| <span class="rarity rarity-epic">Epic</span> | 2 | 3 |
| <span class="rarity rarity-legendary">Legendary</span> | 3 | 4 |
| <span class="rarity rarity-mythical">Mythical</span> | 4 | 4 |

Plaattypen (`type`) van de meegeleverde skin, Open-World Memories. De pc in My Room groepeert naamplaatjes per type, onder de sectienamen uit `Modules/Stages/myroom/lang/<code>/nameplate_types.json` van de skin. Een type zonder naam daarin verschijnt als "Plaattype" met zijn nummer, en de secties van de typen 10 en 38 tonen "???" tot de speler een naamplaatje van dat type bezit.

| Type | Naam | Effect |
| --- | --- | --- |
| 0 | Hout |  |
| 1 | Koper | Glans |
| 2 | Amethist | Glans |
| 3 | Natuur | Glans |
| 4 | Rood | Glans |
| 5 | Blauw | Glans |
| 6 | As | Glans |
| 7 | Regenboog | Geanimeerd |
| 8 | Ruimte |  |
| 9 | Diamant |  |
| 10 | Misery |  |
| 11 | Goud |  |
| 12 | Parel |  |
| 13 | Platina |  |
| 14 | Kohaku (personageplaatje) |  |
| 15 | Dashy |  |
| 16 | Blauw |  |
| 17 | Rood |  |
| 18 | Hemelpaars |  |
| 19 | AI |  |
| 20 | Hemel |  |
| 21 | Geel |  |
| 22 | Crème |  |
| 23 | Zee |  |
| 24 | Groen |  |
| 25 | Hoofdstuk I |  |
| 26 | Hoofdstuk II |  |
| 27 | Hoofdstuk III |  |
| 28 | OpTk Licht |  |
| 29 | OpTk Donker |  |
| 30 | NEP0 |  |
| 31 | Nacht |  |
| 32 | Hoofdstuk IV |  |
| 33 | Clear-prestatie |  |
| 34 | Full Combo-prestatie |  |
| 35 | Perfect-prestatie |  |
| 36 | Sjabloon |  |
| 37 | Hoofdstuk V |  |
| 38 | V-ENTURE |  |
| 39 | Clear-geschiedenis |  |
| 40 | Full Combo-geschiedenis |  |
| 41 | Perfect-geschiedenis |  |
| 42 | Ryuto Setsujin |  |
| 43 | Zonsopgang |  |
| 44 | Hoofdstuk VI |  |
| 45 | Een nieuwe wereld |  |

Het spel controleert de ontgrendelvoorwaarden na elke spelbeurt op het resultatenscherm. Wanneer de speler aan een ervan voldoet, ontgrendelt het spel het naamplaatje en toont het een melding. Muntvoorwaarden (`ch`, `cs`, `cm`) ontgrendelen alleen via een aankoop: in de meegeleverde skin koopt de speler het naamplaatje via de pc in My Room. De pc in My Room houdt vergrendelde Mythical-naamplaatjes ook uit zijn lijst tot de speler ze bezit.

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

## Stap 3: Voeg vertalingen toe

Vertalingen zijn optioneel. Elk bestand in de map `Lang/` van het pakket bevat de titels van één taal, één regel per naamplaatje, met de sleutels van het pakket (zonder het pakket-id). De bestandsnaam is de taalcode:

| Bestand | Taal |
| --- | --- |
| `de.json` | Duits |
| `en.json` | Engels |
| `es.json` | Spaans |
| `fr.json` | Frans |
| `ja.json` | Japans |
| `ko.json` | Koreaans |
| `nl.json` | Nederlands |
| `ru.json` | Russisch |
| `zh.json` | Chinees |

```json
{
  "night_sky": "Promeneur du ciel nocturne",
  "hello": "Bonjour"
}
```

Een naamplaatje dat in het bestand van een taal ontbreekt, toont in die taal zijn `title`.

## Stap 4: Test het in het spel

Start het spel en open de pc in My Room. Kies op het tabblad Naamplaatjes je pakket in de pakketbalk boven de lijst: de lijst toont de naamplaatjes van het pakket, de vergrendelde met hun ontgrendelhint. Rust er een uit en controleer de titel, het plaatje en de sterren op het naamplaatje van de speler. Wissel de taal van het spel om de vertalingen te controleren.

Het spel slaat een pakket over dat het niet kan lezen en toont een foutmelding die het pakket noemt. Een pakket zonder geldig `pack`-id, of met het id van een pakket dat het spel al heeft geladen, telt als onleesbaar. Een los naamplaatje zonder `key` of `title`, of met een sleutel die al in het pakket bestaat, slaat het spel over, en het schrijft de reden naar `OpenTaiko.log`.

## Naamplaatjes vanuit een script

Lua-scripts werken met dezelfde id's. Elke methode die een naamplaatje-id aanneemt, accepteert het volledige id als string; de meegeleverde naamplaatjes accepteren ook hun nummer. Een script kan een naamplaatje met de voorwaarde `ig` geven, dat nooit door spelen ontgrendelt:

```lua
local save = GetSaveFile(0)
local id = "example_pack:night_sky"
if not save:IsNameplateUnlocked(id) then
    save:UnlockNameplate(id)
end
save:ChangeNameplate(id)
```

UnlockNameplate toont geen melding, dus een script dat er een wil, toont die zelf. Zie [Spelers en profielen](../api/players.md) voor de naamplaatjesmethoden.

## Voorbeelden

Deze pakketten tonen veelvoorkomende patronen. Vervang de id's, titels en getallen door je eigen waarden.

**Een gratis naamplaatje.** Zonder `unlock` bezit elke speler het naamplaatje en kan hij het meteen uitrusten.

```json
{
  "pack": "example_pack",
  "nameplates": [
    { "key": "welcome", "title": "Welcome Aboard", "type": 0, "rarity": "Common" }
  ]
}
```

**Ontgrendelen door te spelen (`tp`).** Het naamplaatje ontgrendelt na 100 spelbeurten. `ap` (AI-gevechten), `aw` (gewonnen AI-gevechten) en `ce` (in totaal verdiende munten) gebruiken dezelfde indeling met één waarde.

```json
{
  "pack": "example_pack",
  "nameplates": [
    { "key": "regular", "title": "Regular Drummer", "type": 3, "rarity": "Uncommon",
      "unlock": { "condition": "tp", "values": [100] } }
  ]
}
```

**Een nummer uit je chartpakket clearen (`sp`).** Een chartpakket kan het clearen van zijn eigen nummers belonen. De referentie is het veld `id` van de `uniqueID.json` van het nummer, en `[3, 3]` vraagt om een full combo (3) op Extreme (3).

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

**Met munten kopen (`ch`).** De prijs is de enige waarde. In de meegeleverde skin koopt de speler het naamplaatje via de pc in My Room.

```json
{
  "pack": "example_pack",
  "nameplates": [
    { "key": "golden", "title": "Golden Touch", "type": 11, "rarity": "Rare",
      "unlock": { "condition": "ch", "values": [800] } }
  ]
}
```

**Een beloning die alleen een script geeft (`ig`).** Spelen ontgrendelt een `ig`-naamplaatje nooit, dus een script geeft het met UnlockNameplate (zie Naamplaatjes vanuit een script). Tot dan toont de pc in My Room er de algemene hint voor een ongeldige voorwaarde bij.

```json
{
  "pack": "example_story",
  "nameplates": [
    { "key": "finale", "title": "Curtain Call", "type": 19, "rarity": "Legendary",
      "unlock": { "condition": "ig", "values": [] } }
  ]
}
```

**Een skinpakket met vertalingen.** Een skin bewaart zijn pakket in `System/<skin>/Nameplates/`, met de vertalingen in `System/<skin>/Nameplates/Lang/`.

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

## Probleemoplossing en opmerkingen

- Het pakket verschijnt niet: de map staat niet direct in `Global/Nameplates/`, het bestand heet niet `Nameplates.json`, het `pack`-id ontbreekt of gebruikt andere tekens dan kleine letters, cijfers en `_`, of een ander pakket gebruikt al hetzelfde id. De foutmelding noemt het pakket.
- Een naamplaatje ontbreekt in de lijst: het heeft geen `key` of geen `title`, of zijn sleutel herhaalt een eerdere. `OpenTaiko.log` noemt het item.
- Een vertaling verschijnt niet: de sleutel in het `Lang`-bestand moet de sleutel van het pakket zonder het pakket-id zijn (`night_sky` voor `example_pack:night_sky`), en de bestandsnaam moet de taalcode zijn.
- Het plaatje toont geen afbeelding: de skin heeft geen map voor dat `type`.
- Online zien andere spelers de titel van een pakketnaamplaatje alleen als ze hetzelfde pakket hebben. Anders zien ze het naamplaatje van de speler zonder titel.
- Als je een pakket verwijdert, tonen spelers die een van zijn naamplaatjes droegen geen titel tot het pakket terug is. Hun ontgrendelingen blijven in het opslagbestand.
- De JSON-lezer van het spel accepteert commentaar en afsluitende komma's; strikte validators weigeren ze.
