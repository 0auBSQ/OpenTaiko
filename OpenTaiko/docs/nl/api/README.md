<!-- docs/api/README.md -->

# API-referentie

<span class="badge-new">Spelversie 0.6.1</span>

De referentie voor elke global die de Lua-runtime van OpenTaiko aan een skin beschikbaar stelt. Kies een categorie in de zijbalk, of begin met [Modules en levenscyclus](activities.md) als je nog geen module hebt geschreven.

## Hoe je een signatuur leest

Elke vermelding toont de functie zoals je haar vanuit Lua aanroept.

- Een dubbele punt betekent dat je de functie op een waarde aanroept en dat Lua die waarde als de verborgen `self` doorgeeft: `tex:Draw(x, y)` roep je aan op een textuur die je eerder hebt geladen.
- Een punt of een kale naam is een gewone aanroep, zoals `GetSaveFile(0)`.
- Het type na de pijl is wat de aanroep teruggeeft: `TEXTURE:CreateTexture(path) -> texture` geeft een handle terug die je bewaart en later tekent.

Globale namen zijn in hoofdletters (`TEXTURE`, `SOUND`, `INPUT`). De runtime biedt ze binnen elk modulescript aan; je maakt ze nooit zelf aan.

## Categorieën

| Categorie | Wat het omvat |
| --- | --- |
| [Modules en levenscyclus](activities.md) | De callbacks die een module ontvangt, en de helpers voor activities, achtergronden, transities en counters. |
| [Graphics en tekst](graphics.md) | Texturen, canvassen, clipping, tekstrendering, video, kleuren en gradiënten. |
| [Audio](audio.md) | Geluid laden en afspelen. |
| [Invoer](input.md) | Toetsenbord-, pad- en muisinvoer, en tekstinvoer op het scherm. |
| [Data en persistentie](data.md) | Data die een herstart overleeft, JSON en INI laden, gedeelde resources. |
| [Nummers en charts](songs.md) | De nummerlijst, nummerknopen en charts, scores, en het bouwen van dan-cursussen (examens). |
| [Spelers en profielen](players.md) | Opslagbestanden, naamplaatjes, personages, puchichara's, spelstatus, thema's en taal. |
| [Wiskunde](math.md) | Vectoren, matrices en quaternions. |
| [Online netwerken](networking.md) | De `NET`-global voor OpenTaiko Online-sessies. Momenteel experimenteel. |
| [3D-engine: rasterizerwereld](3d.md) | Scènes, objecten, modellen, lichten, camera's, sprites, hoogtekaarten en render targets. Momenteel experimenteel. |
| [3D-engine: raytracerwereld](3d-raytrace.md) | De path tracer: materialen, analytische primitieven en de luchtgradiënt. Momenteel experimenteel. |
| [3D-engine: fysica](3d-physics.md) | Fysicawereld, lichamen en voertuigen, colliders, raycasts en pathfinding. Momenteel experimenteel. |

## De badge Experimenteel

Functies gemarkeerd als <span class="badge-exp">Experimenteel</span> werken vandaag, maar kunnen tussen releases veranderen zonder afbouwperiode. De 3D-engine en online netwerken dragen momenteel deze badge; het plan is dat de 3D-engine met versie 1.0 de experimentele status verlaat. Als een skin van experimentele functies afhangt, test hem dan na elke update opnieuw en vermeld op welke spelversie hij is gericht.
