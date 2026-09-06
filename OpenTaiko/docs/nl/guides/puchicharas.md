<!-- guides/puchicharas.md -->

# Een puchichara toevoegen

Een puchichara is de kleine metgezel die tijdens het spelen naast de gauge stuitert. Elke puchichara is één map onder `Global/PuchiChara/` in de installatiemap van het spel, met een spritesheet van twee frames en enkele optionele JSON-bestanden. Je schrijft geen code: maak de map met de juiste bestandsnamen aan en het spel pikt hem bij de volgende start op.

Compatibiliteit: OpenTaiko 0.6.1 laadt puchichara's die voor 0.6.0 zijn gemaakt nog steeds zonder wijzigingen.

## Voordat je begint

- OpenTaiko 0.6.1 geïnstalleerd. Het spel leest puchichara's uit `Global/PuchiChara/` naast het uitvoerbare bestand van het spel, en alle skins delen de map.
- Een afbeeldingsbewerker die PNG met transparantie exporteert.
- Een teksteditor voor de JSON-bestanden.
- Optioneel: een kort `.ogg`-fragment voor `Welcome.ogg`.

## Stap 1: Maak de map

Het spel somt bij het opstarten de submappen van `Global/PuchiChara/` op en behandelt elke submap als een puchichara. De mapnaam is de identiteit: het spel schrijft die naar het opslagbestand wanneer de speler de puchichara selecteert en registreert de ontgrendeling eronder. Kies een stabiele naam: na een hernoeming vallen bestaande selecties terug op de eerste map en komt de geregistreerde ontgrendeling niet meer overeen. De meegeleverde mappen gebruiken een sorteervoorvoegsel, bijvoorbeeld `00 - None`, `01a - OpenTaiko-Kun` en `02 - Bol`. Het spel sorteert de lijst niet, dus het voorvoegsel houdt de bestandssysteemvolgorde voorspelbaar. De eerste map is de terugval wanneer een opslagbestand naar een map verwijst die niet meer bestaat, dus houd `00 - None` als eerste.

```
Global/PuchiChara/
    00 - None/
    01a - OpenTaiko-Kun/
    02 - Bol/
    99 - MyMascot/          <-- je nieuwe map
```

## Stap 2: Teken de spritesheet (Chara.png)

Het spel tekent de metgezel uit `Chara.png`, een horizontale spritesheet. De frame-indeling komt uit de skinwaarde `Game_PuchiChara`, die standaard `256,256,2` is (framebreedte, framehoogte, aantal frames), dus de meegeleverde sheets zijn 512x256 pixels: twee frames van 256x256 naast elkaar, frame 0 links. Tijdens het spelen doorloopt het spel de frames en voegt het een verticaal stuiteren toe (rustig stuiteren in menu's, op de beat gesynchroniseerd stuiteren in het spel), dus de twee frames horen twee poses van hetzelfde personage te zijn. Gebruik een transparante achtergrond. Als `Chara.png` ontbreekt, laadt het item nog steeds, maar tekent het niets.

De meegeleverde mappen bevatten ook een `Chara.xcf` (GIMP-bron) en een `PuchiConfig.txt`. Het spel leest geen van beide.

```ini
Chara.png : 512 x 256 PNG, transparantie
  +-----------------+-----------------+
  |    frame 0      |    frame 1      |
  |   256 x 256     |   256 x 256     |
  +-----------------+-----------------+
```

## Stap 3: Schrijf Metadata.json

Maak `Metadata.json` met deze velden:

- `name`, `author`, `description`: elk een gewone string of een gelokaliseerd object `{ "strings": { "default": "...", "ja": "...", ... } }` waarbij `default` de terugval is en de andere sleutels speltaalcodes zijn.
- `rarity`: een van `Poor`, `Common`, `Uncommon`, `Rare`, `Epic`, `Legendary`, `Mythical`. Zeldzaamheid bepaalt de kleur en het niveau van de ontgrendelmelding. Elke zeldzaamheid heeft een muntvermenigvuldiger van 1, dus ze verandert de verdiensten niet. Een onbekende waarde gedraagt zich als `Common`.

Als het bestand afwezig is, laadt het item met naam `(None)`, zeldzaamheid `Common` en auteur `(None)`.

```json
{
    "name": {
        "strings": {
            "default": "MyMascot",
            "ja": "マイマスコット"
        }
    },
    "rarity": "Rare",
    "description": {
        "strings": {
            "default": "A friendly companion.\nWaves during play."
        }
    },
    "author": "YourName"
}
```

## Stap 4 (optioneel): Voeg Effects.json toe

`Effects.json` geeft de puchichara gameplay-effecten. Alle velden staan standaard uit wanneer het bestand afwezig is:

- `allpurple` (bool): grote don- en ka-noten worden paarse noten die beide trommels accepteren.
- `autoroll` (int): automatische slagen per seconde op roffels en ballonnen. Elke waarde boven 0 zet de muntvermenigvuldiger op 0.
- `showadlib` (bool): toont verborgen ADLIB-noten. Vermenigvuldigt de munten met 0,9.
- `splitlane` (bool): tekent don- en ka-noten op afzonderlijke banen.

Laat het bestand weg voor een puur cosmetische metgezel.

```json
{
    "allpurple": false,
    "autoroll": 0,
    "showadlib": false,
    "splitlane": false
}
```

## Stap 5 (optioneel): Voeg Welcome.ogg en Render.png toe

- `Welcome.ogg`: een stemfragment dat het spel in de geluidsgroep Voice laadt. Het ingebouwde kamerscherm van het spel speelt het af wanneer de speler de puchichara selecteert; Lua-stages kunnen er niet bij, dus een skin met een eigen kamerscherm speelt het niet af.
- `Render.png`: een stilstaande afbeelding op volledige grootte die Lua-stages als portret kunnen tekenen (de `render`-textuur van een `PUCHICHARALIST`-item). Het is één enkele afbeelding van willekeurige grootte. Geen van de meegeleverde puchichara's bevat er een.

Beide bestanden zijn optioneel.

```
MyMascot/
    Chara.png       (vereist, de geanimeerde spritesheet)
    Metadata.json   (naam, zeldzaamheid, auteur, beschrijving)
    Effects.json    (optionele gameplay-effecten)
    Unlock.json     (optionele ontgrendelvoorwaarde)
    Welcome.ogg     (optioneel stemfragment)
    Render.png      (optioneel portret)
```

## Stap 6 (optioneel): Voeg een ontgrendelvoorwaarde toe (Unlock.json)

Zonder `Unlock.json` is de puchichara meteen beschikbaar. Om hem te vergrendelen, voeg je een `Unlock.json` toe met de velden `condition`, `type`, `values` en `references`; het formaat en de voorwaarde-id's zijn dezelfde als voor nummers en personages (zie de handleiding over ontgrendelvoorwaarden). Het onderstaande voorbeeld ontgrendelt zodra de speler in totaal 500 munten heeft verdiend. Meegeleverde voorbeelden: OpenTaiko-Kun kost 100 munten (`"condition": "ch"`), Bol vereist 20 gecleardde charts van de charter `bol` (`"condition": "sc"`), en Tinyfox vereist één gespeeld nummer in het genre `Project Outfox Serenity` (`"condition": "sg"`).

De speler koopt muntvoorwaarden in het kamerscherm. Het spel controleert de andere voorwaarden na elke spelbeurt op het resultatenscherm; wanneer de speler aan een ervan voldoet, voegt het de puchichara toe aan de ontgrendellijst van het opslagbestand en toont het een melding.

```json
{
    "condition": "ce",
    "type": "me",
    "values": [
        500
    ]
}
```

## Stap 7: Herstart en selecteer hem

Herstart het spel (of herlaad de skin vanuit de instellingen) zodat het spel de lijst opnieuw opbouwt. Open het kamerscherm en kies het nieuwe item uit de puchichara-lijst. Eenmaal geselecteerd verschijnt hij tijdens het spelen naast de gauge. Als er tijdens het spelen geen metgezel verschijnt, controleer dan of de optie `Draw PuchiChara` in de systeeminstellingen is ingeschakeld.

## Probleemoplossing en opmerkingen

- Bestandsnamen luisteren nauw: `Chara.png`, `Metadata.json`, `Effects.json`, `Unlock.json`, `Welcome.ogg`, `Render.png`. Het spel negeert een verkeerd genoemd bestand en past de standaardwaarde toe.
- Opslagbestanden en ontgrendelrecords slaan de mapnaam letterlijk op. Een map hernoemen laat bestaande selecties terugvallen op de eerste map.
- Het spel snijdt de spritesheet met de framegrootte `Game_PuchiChara` van de skin (standaard `256,256,2`). Een sheet van een andere grootte snijdt het met dezelfde getallen, zodat de frames bijgesneden of verkeerd uitgelijnd uitkomen. Als een skin `Game_PuchiChara` overschrijft, houd je die waarde aan. De meegeleverde skin overschrijft het niet.
- `autoroll` boven 0 zet de muntwinst op nul en `showadlib` verlaagt die tot 0,9, dus een cosmetische metgezel die een van beide instelt, verlaagt de inkomsten van zijn speler.
- De meegeleverde JSON-bestanden bevatten afsluitende komma's. De JSON-parser van het spel accepteert die; strikte validators weigeren ze.
- Het spel bouwt de lijst eenmaal bij het opstarten en bij het herladen van de skin op. Een map die je toevoegt terwijl het spel draait, verschijnt na de volgende opstart of herlaadbeurt van de skin.
