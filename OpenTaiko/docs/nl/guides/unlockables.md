<!-- guides/unlockables.md -->

# Ontgrendelvoorwaarden voor eigen charts

Je kunt een eigen nummer achter een voorwaarde vergrendelen (een muntprijs, andere nummers clearen, een totaal aantal spelbeurten, een verhaalvlag, enzovoort) door een bestand `Unlock.json` in de map van het nummer te plaatsen, naast zijn `.tja` en `uniqueID.json`. Wanneer het spel de nummerlijst opbouwt, leest het dat bestand en houdt het het nummer vergrendeld tot de speler aan de voorwaarde voldoet. Voorwaarden horen bij afzonderlijke nummers: `box.def` heeft geen ontgrendelsleutel, dus een genremap kun je op deze manier niet vergrendelen.

## Voordat je begint

- Een eigen chart die al in de nummerlijst verschijnt: een map onder `Songs/` met een `.tja` en, zodra het spel hem heeft gescand, een `uniqueID.json`.
- Een teksteditor die UTF-8 opslaat.
- Voor voorwaarden die naar andere nummers verwijzen: de `id`-waarde uit de `uniqueID.json` van elk nummer waarnaar wordt verwezen.
- Het spel bewaart ontgrendelvoortgang per opslagbestand, dus je moet in het spel aan de voorwaarde voldoen om het nummer te zien ontgrendelen. De optie `Ignore Song Unlockables` in de spelinstellingen behandelt elk nummer als ontgrendeld terwijl je test.

## Stap 1: Maak het bestand Unlock.json

Maak `Unlock.json` in de nummermap. Elk veld is optioneel en heeft een standaardwaarde:

- `hidden_index` (int, standaard 0): hoe de nummerlijst het vergrendelde nummer presenteert (zie de tabel hieronder). Het spel begrenst waarden op 0-3.
- `rarity` (string, standaard `Common`): de naam van de zeldzaamheid (zie de tabel hieronder). Die bepaalt de kleur van de zeldzaamheidsweergave en het niveau van de ontgrendelmelding. Een lege waarde wordt `Common`.
- `condition` (string, standaard `ch`): het voorwaarde-id (Stap 2).
- `values` (int-array, standaard `[100]`): de numerieke parameters van de voorwaarde.
- `type` (string, standaard `me`): de vergelijking die waardegebaseerde voorwaarden gebruiken: `l` kleiner dan, `le` kleiner dan of gelijk aan, `e` gelijk aan, `me` groter dan of gelijk aan, `m` groter dan, `d` verschillend. Muntvoorwaarden gebruiken altijd `me`.
- `references` (string-array, standaard `[""]`): nummer-id's, genrenamen, charternamen of vlagnamen, afhankelijk van de voorwaarde.
- `custom_unlock_text` (object, optioneel): vervangt de gegenereerde hinttekst. Een gelokaliseerd object `{ "strings": { "default": "...", "ja": "..." } }`; het spel gebruikt de sleutel voor de actieve taal, daarna `default`, en toont de gegenereerde tekst als geen van beide bestaat.

De sleutels zijn in kleine letters met onderstrepingstekens, precies zoals vermeld.

Waarden van de hidden index:

| Waarde | In de nummerlijst |
| --- | --- |
| 0 | Weergegeven met een slotpictogram; de audiopreview speelt |
| 1 | Vergrijsd; geen preview |
| 2 | Vergrijsd, met de titel en previewafbeelding onleesbaar gemaakt |
| 3 | Verborgen tot het is ontgrendeld |

Zeldzaamheidswaarden:

| Zeldzaamheid | Meldingsniveau |
| --- | --- |
| <span class="rarity rarity-poor">Poor</span> | 0 |
| <span class="rarity rarity-common">Common</span> | 0 |
| <span class="rarity rarity-uncommon">Uncommon</span> | 1 |
| <span class="rarity rarity-rare">Rare</span> | 2 |
| <span class="rarity rarity-epic">Epic</span> | 3 |
| <span class="rarity rarity-legendary">Legendary</span> | 4 |
| <span class="rarity rarity-mythical">Mythical</span> | 4 |

```json
{
  "hidden_index": 0,
  "rarity": "Common",
  "condition": "cm",
  "values": [500],
  "type": "me",
  "references": [""],
  "custom_unlock_text": {
    "strings": {
      "default": "Buy this song for 500 coins.",
      "ja": "500コインで解放できます。"
    }
  }
}
```

## Stap 2: Kies een voorwaarde

| Id | Betekenis | `values` | `references` |
|----|---------|----------|--------------|
| `ch`, `cs`, `cm` | Aankoop met munten. Het spel evalueert de drie id's identiek (prijs in munten, `type` afgedwongen op `me`, nooit automatisch toegekend). `cm` is het id dat voor nummers is bedoeld; skinscripts kunnen het id lezen en gebruiken om te bepalen waar ze de aankoop aanbieden. | `[price]` | ongebruikt |
| `ce` | In totaal verdiende munten sinds het aanmaken van het opslagbestand | `[coins]` | ongebruikt |
| `tp` | Totaal aantal spelbeurten | `[plays]` | ongebruikt |
| `ap` | Aantal AI-gevechten | `[plays]` | ongebruikt |
| `aw` | Aantal gewonnen AI-gevechten | `[wins]` | ongebruikt |
| `sd` | Afzonderlijke charts die een clearstatus hebben bereikt | `[chart count, clear status]` | ongebruikt |
| `dp` | Charts van één moeilijkheid die een clearstatus hebben bereikt | `[difficulty, clear status, chart count]` | ongebruikt |
| `lp` | Charts van één sterrenniveau die een clearstatus hebben bereikt | `[level, clear status, chart count]` | ongebruikt |
| `sp` | Specifieke nummers die een clearstatus hebben bereikt | `[difficulty, clear status]` per nummer (`-1` = elke moeilijkheid) | één nummer-id per paar |
| `sg` | Nummers binnen benoemde genres die een clearstatus hebben bereikt | `[song count, clear status]` per genre | één genrenaam per paar |
| `sc` | Charts van benoemde charters die een clearstatus hebben bereikt | `[chart count, clear status]` per charter | één charternaam per paar |
| `gt` | Globale trigger (een benoemde aan/uit-vlag in het opslagbestand die scripts zetten) | `[1]` voor AAN, `[0]` voor UIT | `[trigger name]` |
| `gc` | Globale teller (een benoemd getal in het opslagbestand dat scripts zetten) | `[value]`, vergeleken met `type` | `[counter name]` |
| `ig` | Onmogelijk te verkrijgen: ontgrendelt nooit | geen | geen |
| `andcomb` | De speler moet aan alle kindvoorwaarden voldoen (zie de gecombineerde voorbeelden) | `[]` | één kindvoorwaarde per item, als JSON-string |
| `orcomb` | De speler moet aan minstens één kindvoorwaarde voldoen (zie de gecombineerde voorbeelden) | `[]` | één kindvoorwaarde per item, als JSON-string |

Clearstatuswaarden (een statusvereiste betekent deze status of beter):

| Waarde | Clearstatus |
| --- | --- |
| 0 | gespeeld |
| 1 | assisted clear |
| 2 | clear |
| 3 | full combo |
| 4 | all perfect |

Moeilijkheidswaarden:

| Waarde | Moeilijkheid |
| --- | --- |
| 0 | Easy |
| 1 | Normal |
| 2 | Hard |
| 3 | Extreme |
| 4 | Extra Extreme |

Voor `dp` telt moeilijkheid 3 ook Extra Extreme-charts mee. `dp` en `lp` tellen alleen gewone charts en slaan Dan- en Tower-charts over. `sp` accepteert `-1` als elke moeilijkheid.

Het spel controleert de aantallen waarden: `ch`/`cs`/`cm`/`ce`/`tp`/`ap`/`aw`/`gt`/`gc` hebben precies 1 waarde nodig, `sd` precies 2, `dp`/`lp` precies 3, en `sp`/`sg`/`sc` hebben 2 waarden per referentie nodig met evenveel referenties als paren. Een verkeerd aantal laat de voorwaarde mislukken met een foutmelding in het spel.

Alleen een expliciete aankoop in een scherm dat die aanbiedt, ontgrendelt een muntvoorwaarde (de meegeleverde skin biedt vergrendelde nummers te koop aan vanuit de nummerselectie, ongeacht welk munt-id je gebruikt). Het spel controleert alle andere voorwaarden na elke spelbeurt op het resultatenscherm; wanneer de speler aan een ervan voldoet, voegt het spel het nummer-id aan het opslagbestand toe en toont het een melding.

## Stap 3: Test het in het spel

Start het spel en open de nummerselectie. Afhankelijk van `hidden_index` toont het nummer een slot, is het vergrijsd, is het onleesbaar gemaakt, of ontbreekt het. Koop het of voldoe aan de voorwaarde, en bevestig daarna dat het na een herstart ontgrendeld blijft. Schakel `Ignore Song Unlockables` in de spelinstellingen in om elk slot te omzeilen terwijl je itereert.

## Voorbeelden

Dit zijn de voorwaardepatronen die de meegeleverde nummers gebruiken, geschreven als `Unlock.json`-bestanden. Vervang de id's, namen en getallen door je eigen waarden.

**Het nummer met munten kopen (`cm`).** Het meest voorkomende patroon. De prijs is de enige waarde, en het spel negeert `type`. `hidden_index` 0 houdt het nummer zichtbaar, zodat de speler het kan vinden en kopen. De gegenereerde hint vermeldt de prijs al, dus je hebt geen `custom_unlock_text` nodig.

```json
{
  "hidden_index": 0,
  "rarity": "Uncommon",
  "condition": "cm",
  "values": [500]
}
```

**Totaal aantal spelbeurten (`tp`).** De meegeleverde hoofdstukken openen hun nummers een voor een met oplopende aantallen spelbeurten (10, 15, 20 enzovoort), zodat de speler altijd een volgend nummer binnen bereik heeft. `ap` (AI-gevechten), `aw` (gewonnen AI-gevechten) en `ce` (in totaal verdiende munten) gebruiken dezelfde indeling met één waarde.

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "tp",
  "values": [50]
}
```

**Een specifiek nummer clearen (`sp`).** Het patroon dat de meeste meegeleverde nummers gebruiken voor een vervolg of een remix. `-1` als moeilijkheid accepteert een clear op elke moeilijkheid; het id is het veld `id` van de `uniqueID.json` van het nummer waarnaar wordt verwezen (het spel genereert een id van 64 tekens de eerste keer dat het een nummer zonder id scant, en meegeleverde nummers kunnen een handgeschreven id dragen). Het voorbeeld ontgrendelt nadat de speler het nummer waarnaar wordt verwezen heeft gecleard.

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "sp",
  "values": [-1, 2],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU"]
}
```

**Een full combo op meerdere nummers (`sp`).** Elk extra nummer voegt een waardepaar en een referentie toe, en de speler moet aan elk nummer waarnaar wordt verwezen voldoen. Het voorbeeld vereist een full combo (3) op twee nummers.

```json
{
  "hidden_index": 2,
  "rarity": "Rare",
  "condition": "sp",
  "values": [-1, 3, -1, 3],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU", "4aJ2I19XyEG2cEA9tlmdnZSz2H43OKsVBPLi52UfRhSjDGNgTGGqhmqkbWfPDoyw"]
}
```

**Nummers van een genre clearen (`sg`).** Elk item is `[song count, clear status]` met de genrenaam in `references`. Het genre is het genre dat het spel registreert wanneer de speler het nummer speelt: de `#GENRE` van de `box.def` van de omsluitende map, of de eigen `#GENRE` van de chart wanneer de map er geen heeft. De meegeleverde hoofdstukken gebruiken dit voor hun medleys. Het voorbeeld ontgrendelt nadat de speler 10 afzonderlijke nummers van het genre heeft gecleard.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "sg",
  "values": [10, 2],
  "references": ["OpenTaiko Chapter II"]
}
```

**Charts van een charter clearen (`sc`).** Dezelfde indeling, met `#NOTESDESIGNER`-namen als referenties. Het voorbeeld vereist 5 geclearde charts van één charter; een tweede charter voegt nog een waardepaar en referentie toe.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "sc",
  "values": [5, 2],
  "references": ["bol"]
}
```

**Een aantal afzonderlijke charts clearen (`sd`).** Telt elke chart die het opslagbestand op de gegeven status of beter heeft staan. Het voorbeeld vereist 10 full combo's.

```json
{
  "hidden_index": 2,
  "rarity": "Rare",
  "condition": "sd",
  "values": [10, 3]
}
```

**Charts van een moeilijkheid of sterrenniveau clearen (`dp` / `lp`).** `dp` telt charts van één moeilijkheid, `lp` charts van één sterrenniveau. Het eerste voorbeeld vereist 20 geclearde Extreme-charts, het tweede één geclearde chart van 7 sterren.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "dp",
  "values": [3, 2, 20]
}
```

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "lp",
  "values": [7, 2, 1]
}
```

**Een van twee voorwaarden (`orcomb`).** `values` is leeg en elk item van `references` is een complete kindvoorwaarde (`condition`, `type`, `values`, `references`), geschreven als JSON-string. De meegeleverde nummers gebruiken dit om een kortere weg te bieden: genoeg nummers spelen, of betalen. `orcomb` gebruikt de goedkoopste vervulde tak wanneer het de prijs van een aankoop bepaalt.

```json
{
  "hidden_index": 0,
  "rarity": "Common",
  "condition": "orcomb",
  "values": [],
  "references": [
    "{\"condition\": \"tp\", \"type\": \"me\", \"values\": [100]}",
    "{\"condition\": \"cm\", \"type\": \"me\", \"values\": [200]}"
  ]
}
```

**Meerdere voorwaarden tegelijk (`andcomb`).** Dezelfde indeling; de speler moet aan elk kind voldoen. Kinderen mogen zelf combinaties zijn, en `andcomb` telt de muntprijzen binnen een combinatie op. Het voorbeeld vereist een clear van één nummer en een clear van een willekeurige chart van 7 sterren.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "andcomb",
  "values": [],
  "references": [
    "{\"condition\": \"sp\", \"type\": \"me\", \"values\": [-1, 2], \"references\": [\"4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU\"]}",
    "{\"condition\": \"lp\", \"type\": \"me\", \"values\": [7, 2, 1], \"references\": [\"\"]}"
  ]
}
```

**Verhaalvlag met een eigen hint (`gt`).** `gt` leest een benoemde aan/uit-vlag uit het opslagbestand. Lua-scripts (verhaalscènes, cutscenes) zetten de vlaggen, dus gebruik `gt` alleen wanneer een script dat je beheert de vlag zet. `values` is `[1]` voor AAN of `[0]` voor UIT; `references` bevat de vlagnaam. Een verhaalontgrendeling heeft geen vanzelfsprekende gegenereerde hint, dus `custom_unlock_text` komt hier van pas. Je kunt elke taalsleutel opgeven; `default` is de terugval.

```json
{
  "hidden_index": 3,
  "rarity": "Legendary",
  "condition": "gt",
  "values": [1],
  "references": ["story_done"],
  "custom_unlock_text": {
    "strings": {
      "default": "Finish the story to unlock this song.",
      "ja": "ストーリーをクリアするとこの曲が解放されます。"
    }
  }
}
```

**Geheim nummer met een raadsel.** De meegeleverde geheime nummers combineren een hoge `hidden_index` (de titel en preview blijven onleesbaar, of het nummer blijft afwezig tot het is ontgrendeld) met een `custom_unlock_text` die op de voorwaarde zinspeelt in plaats van die te noemen. Eronder werkt elke voorwaarde; het voorbeeld verbergt een full-combovereiste achter een raadsel.

```json
{
  "hidden_index": 2,
  "rarity": "Epic",
  "condition": "sp",
  "values": [-1, 3],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU"],
  "custom_unlock_text": {
    "strings": {
      "default": "Perfect the storm that came before this one."
    }
  }
}
```

## Probleemoplossing en opmerkingen

- Het nummer is niet vergrendeld: het bestand heet niet `Unlock.json`, het staat niet in de map die de `.tja` en `uniqueID.json` bevat, of `Ignore Song Unlockables` staat aan.
- De hint zegt dat de voorwaarde ongeldig is: het aantal waarden komt niet overeen met de voorwaarde (zie Stap 2), of `sp`/`sg`/`sc` hebben een ander aantal referenties dan waardeparen.
- Een sleutel heeft geen effect: de sleutels zijn `hidden_index`, `rarity`, `condition`, `values`, `type`, `references`, `custom_unlock_text`, allemaal in kleine letters. Het spel negeert al het andere en past de standaardwaarde toe.
- `sp` ontgrendelt nooit: de referentie moet het `uniqueID.json`-id van het doelnummer zijn (een titel of bestandsnaam komt nooit overeen), en de speler moet de moeilijkheid en status kunnen bereiken (status 3 is een full combo, dus een chart die de speler alleen heeft gecleard telt niet).
- `sg` ontgrendelt nooit: de genrenaam moet overeenkomen met het genre dat het spel voor de gespeelde nummers registreert (de `#GENRE` van de `box.def` van de map, of de `#GENRE` van de chart wanneer er geen mapgenre is).
- `gt`/`gc` ontgrendelen nooit: niets zet de benoemde vlag. De chart kan die niet zelf zetten.
- Niets ontgrendelt `ig` tijdens het spelen. Gebruik het alleen voor inhoud die een ander systeem toekent.
- `custom_unlock_text` moet een `{ "strings": { ... } }`-object zijn; een kale string werkt niet.
- De meegeleverde `Unlock.json`-bestanden bevatten afsluitende komma's. De JSON-parser van het spel accepteert die; strikte validators weigeren ze.
