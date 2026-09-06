<!-- docs/README.md -->

# OpenTaiko-documentatie

<span class="badge-new">Spelversie 0.6.1</span>

<div class="callout warn">Deze documentatie wordt nog nagekeken en kan tot de release nog veranderen.</div>

OpenTaiko tekent bijna elk scherm buiten de kerngameplay met Lua-scripts die in een skin worden meegeleverd: de menu's, de nummerselectie, de kamer- en verhaalscènes, de achtergronden, het resultatenscherm. Deze site documenteert de Lua-API die deze scripts gebruiken en loopt de meest voorkomende dingen door die een skinner wil doen.

## Waar te beginnen

- Als je nog geen module hebt geschreven, lees dan [Hoe modules werken](getting-started.md): de mapindeling van een skin, de soorten modules en de levenscyclus-callbacks die een script ontvangt.
- Om een functie op te zoeken, open je de [API-referentie](api/) en kies je een categorie in de zijbalk.
- Voor een concreet doel lopen de handleidingen stap voor stap door [personages](guides/characters.md), [puchichara's](guides/puchicharas.md), [skins en thema's](guides/skins.md) en [ontgrendelvoorwaarden voor charts](guides/unlockables.md).

## Wat je kunt bouwen

| Gebied | Waar het staat | Wat het is |
| --- | --- | --- |
| Modules | De map `Modules/` van de skin | Stages (volledige schermen met eigen invoer, tekenwerk en status: een nummerselectie, een kamer, een verhaalscène), activities (subschermen en overlays zoals dialoogvensters en naamplaatjes) en transities (de fade- en laadschermen tussen stages). |
| Achtergronden | De map `Graphics/` van de skin | Scripts die een scherm aankleden: het opstartscherm, de kamer, de gameplay-achtergrond en de mob, het resultatenscherm. |
| Skins en thema's | `System/`, naast het spel | Een compleet visueel pakket dat een speler installeert en activeert, met de thema-instellingen die het in het optiescherm aanbiedt. |
| Personages en puchichara's | `Global/Characters/` en `Global/PuchiChara/`, gedeeld door elke skin | De dansers en de kleine mascottes die tijdens het spelen reageren. |
| Ontgrendelvoorwaarden voor charts | De nummermap, naast de chart | Een `Unlock.json` die een nummer vergrendeld houdt tot de speler het heeft verdiend. |

## Experimentele secties

Sommige pagina's dragen in de zijbalk een badge <span class="badge-exp">Experimenteel</span>. De functies die ze beschrijven werken in de huidige release, maar hun API kan tussen releases nog veranderen zonder overgangsperiode. Een skin die erop vertrouwt, moet vermelden op welke spelversie hij mikt, en je moet de skin na elke update opnieuw testen.

## Talen

De documentatie bestaat in elke taal waarin OpenTaiko wordt geleverd; kies er een met de taalkeuze bovenaan de zijbalk. De site toont een pagina in het Engels totdat er een vertaling bestaat.
