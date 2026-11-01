<!-- guides/unlockables-reference.md -->

# Referentie voor ontgrendelvoorwaarden

Nummers, personages, puchichara's en naamplaatjes gebruiken één formaat voor ontgrendelvoorwaarden. Nummers, personages en puchichara's bewaren het in een bestand `Unlock.json`, naamplaatjes in het object `unlock` van hun item in `Nameplates.json`. De handleidingen leggen uit waar elk bestand komt: [Ontgrendelvoorwaarden voor charts](unlockables.md), [Personages toevoegen](characters.md), [Puchichara's toevoegen](puchicharas.md) en [Naamplaatjes toevoegen](nameplates.md). Deze pagina geeft de velden, de voorwaarde-id's en de getallen die ze gebruiken.

## Voorwaardevelden

| Veld | Type | Betekenis |
| --- | --- | --- |
| `condition` | string | Het voorwaarde-id (zie Voorwaarde-id's). |
| `values` | int-array | De numerieke parameters van de voorwaarde. |
| `type` | string | De vergelijking die waardegebaseerde voorwaarden gebruiken (zie Vergelijkingstypen). |
| `references` | string-array | Nummer-id's, genrenamen, charternamen of vlagnamen, afhankelijk van de voorwaarde. |

De `Unlock.json` van een nummer voegt eigen velden toe (`hidden_index`, `rarity`, `custom_unlock_text`), die de handleiding [Ontgrendelvoorwaarden voor charts](unlockables.md) beschrijft.

## Voorwaarde-id's

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
| `andcomb` | De speler moet aan alle kindvoorwaarden voldoen (zie de gecombineerde voorbeelden in de handleiding [Ontgrendelvoorwaarden voor charts](unlockables.md)) | `[]` | één kindvoorwaarde per item, als JSON-string |
| `orcomb` | De speler moet aan minstens één kindvoorwaarde voldoen (zie de gecombineerde voorbeelden in de handleiding [Ontgrendelvoorwaarden voor charts](unlockables.md)) | `[]` | één kindvoorwaarde per item, als JSON-string |

Het spel controleert de aantallen waarden: `ch`/`cs`/`cm`/`ce`/`tp`/`ap`/`aw`/`gt`/`gc` hebben precies 1 waarde nodig, `sd` precies 2, `dp`/`lp` precies 3, en `sp`/`sg`/`sc` hebben 2 waarden per referentie nodig met evenveel referenties als paren. Een verkeerd aantal laat de voorwaarde mislukken met een foutmelding in het spel.

Alleen een expliciete aankoop in een scherm dat die aanbiedt, ontgrendelt een muntvoorwaarde. Het spel controleert alle andere voorwaarden na elke spelbeurt op het resultatenscherm; wanneer de speler aan een ervan voldoet, ontgrendelt het spel het item en toont het een melding.

## Vergelijkingstypen

| `type` | Vergelijking |
| --- | --- |
| `l` | kleiner dan |
| `le` | kleiner dan of gelijk aan |
| `e` | gelijk aan |
| `me` | groter dan of gelijk aan (standaard) |
| `m` | groter dan |
| `d` | verschillend |

Muntvoorwaarden gebruiken altijd `me`, ongeacht wat `type` aangeeft.

## Clearstatuswaarden

Een statusvereiste betekent deze status of beter.

| Waarde | Clearstatus |
| --- | --- |
| 0 | gespeeld |
| 1 | assisted clear |
| 2 | clear |
| 3 | full combo |
| 4 | all perfect |

## Moeilijkheidswaarden

| Waarde | Moeilijkheid |
| --- | --- |
| 0 | Easy |
| 1 | Normal |
| 2 | Hard |
| 3 | Extreme |
| 4 | Extra Extreme |

Voor `dp` telt moeilijkheid 3 ook Extra Extreme-charts mee. `dp` en `lp` tellen alleen gewone charts en slaan Dan- en Tower-charts over. `sp` accepteert `-1` als elke moeilijkheid.

## De waarde van de spelmods

Het opslagbestand bewaart de beste spelbeurten van een speler per chart, moeilijkheid en spelmods. De waarde van de spelmods pakt de mods van een spelbeurt in 8 bytes, te beginnen bij de laagste byte: een spelbeurt zonder mods geeft 8925478921.

| Byte | Mod | Waarden |
| --- | --- | --- |
| 1 (laagste) | Scrollsnelheid | 0 tot 99: 9 is 1,0x, elke stap is 0,1x |
| 2 | Stealth | 0 Off, 1 Doron, 2 Stealth, 3 Hidden, 4 Flashlight |
| 3 | Random | 0 Off, 1 Random, 2 Mirror, 3 SuperRandom, 4 MirrorRandom |
| 4 | Nummersnelheid | 2 tot 200: 20 is 1,0x, elke stap is 0,05x |
| 5 | Beoordelingsvenster | 0 Loose, 1 Lenient, 2 Normal, 3 Strict, 4 Rigorous |
| 6 | Beoordelingsmod | 0 uit, 1 Just, 2 Safe |
| 7 | Ongebruikt | 0 |
| 8 (hoogste) | Fun-mod | 0 None, 1 Avalanche, 2 Minesweeper, 3 Dynamic Beat |

Speciale mods (Flawless, Timed) maken geen deel uit van de waarde.
