<!-- modules/custom-stages.md -->

# Eigen stages

Een eigen stage is een stage die iedereen kan maken, delen en installeren, en die het menu Eigen stages van een skin toont. Hij declareert de naamruimte `CStage` met [STAGES:SetNamespace](../api/activities.md), en gaat via de gedeelde string `custom_stage_return` terug naar de stage die hem opende. `Stages.CustomStages` doet beide en somt de eigen stages op voor een menu. `Global/Modules/Stages/custom_stage_tutorial` is er een: een gids, in het spel, over het maken van eigen stages.

## Hem declareren

Bovenaan Script.lua, buiten elke functie:

```lua
-- Global/Modules/Stages/lantern_walk/Script.lua, at the top
local CS = require("Stages.CustomStages")

CS.declare{
    title       = CS.allLanguages("menu", "title"),       -- lang/<code>/menu.json, every language
    description = { en = "A calm walk down a street of paper lanterns." },
}
```

`CS.declare(info)` is `STAGES:SetNamespace("CStage", info)`. De menu's lezen twee velden van info:

| Veld | Toont |
| --- | --- |
| `title` | De naam van de stage in de lijst: een string, of een tabel per taalcode. De mapnaam als hij ontbreekt. |
| `description` | Een of twee regels onder de lijst zolang de stage geselecteerd is; optioneel. |

Een tabel per taalcode geeft de tekst in de taal van het spel, anders in het Engels, anders in een taal die hij heeft. Andere velden zijn van jou: de menu's geven info onaangeroerd door.

## De weg terug

Het menu dat een eigen stage opent, zet de gedeelde string `custom_stage_return` op de stage om naar terug te gaan, `"<stage>"` of `"<stage>|<transition>"`, en gaat dan naar de eigen stage. Die leest en wist de string bij binnenkomst, en vertrekt naar die stage, of naar `_title` als de string leeg is:

```lua
local back, backTransition

function activate()
    back, backTransition = CS.takeReturn()   -- "_title" when the string is empty
end

function update(ts)
    if leaving then return Exit("stage", back, backTransition) end
end
```

De string wordt bij elke binnenkomst gelezen. Een eigen stage die naar een van zijn andere stages gaat en terugkomt, zet de string opnieuw voordat hij vertrekt, zoals Mijn kamer doet met `my_room_return`.

## Stages.CustomStages

| Functie | Beschrijving |
| --- | --- |
| `CS.declare(info)  -> boolean` | Declareert de aanroepende stage als eigen stage: `STAGES:SetNamespace("CStage", info)`. |
| `CS.allLanguages(name, id)  -> table` | De string `id` uit `lang/<code>/<name>.json` van de aanroepende stage, uit elk van de negen talen die hem heeft, als tabel per taalcode. Bedoeld voor `declare`, dat draait voordat er een taal gekozen is. |
| `CS.localized(value, code?)  -> string or nil` | Een gedeclareerde tekst in `code` (standaard de taal van het spel): een string zoals hij is, een tabel per taalcode zoals hierboven beschreven. |
| `CS.list(code?)  -> table` | De eigen stages die de aanroepende module bereikt, op titel gesorteerd: `{ name, isGlobal, title, description, info }`, met `title` en `description` al in de taal. |
| `CS.enter(name, back, transition?)` | Zet `custom_stage_return` op `back` en gaat naar de stage `name`; schrijf `return CS.enter(...)` in de update van een stage. |
| `CS.takeReturn()  -> stage, transition` | Leest en wist `custom_stage_return`: de stage (`_title` als de string leeg is) en de overgang (nil voor de standaard). |
| `CS.open(o)`, `CS.update(ts)`, `CS.draw()`, `CS.isOpen()`, `CS.close()` | De lijst als dialoog boven het scherm van de aanroeper, gebouwd met de UI-bibliotheek van de skin ([UI.Pick](ui.md)). `update` geeft nil zolang hij open is, `"cancel"`, of het gekozen item van `CS.list`; in beide gevallen is de dialoog dicht. `o`: `title`, `ui` (een bibliotheektabel of zijn require-naam), `theme`, `sfx`, `navPlayer` (standaard 0: het toetsenbord en de controller, niet de trommels). Zonder geïnstalleerde eigen stage zegt hij dat boven een OK-knop. |
| `CS.NAMESPACE`, `CS.RETURN_KEY` | `"CStage"` en `"custom_stage_return"`. |

Zijn strings staan in zijn eigen map `lang/`, in de negen talen ([Locale.i18n](locale.md)). Open-World Memories opent de lijst vanuit het item Eigen stages van zijn titelscherm, boven Instellingen:

```lua
-- a menu stage: CS.open{ title = "Custom Stages" } when its entry is chosen, then
function update(ts)
    if CS.isOpen() then
        local r = CS.update(ts)
        if r ~= nil and r ~= "cancel" then return CS.enter(r.name, "_title") end
        return
    end
end

function draw()
    if CS.isOpen() then CS.draw() end
end
```

## Waar een eigen stage staat

| Map | Werkt onder |
| --- | --- |
| `Global/Modules/Stages/<name>/` | Elke skin. |
| `System/<skin>/Modules/Stages/<name>/` | Alleen die skin. |

Bestaat een naam op beide plekken, dan toont een menu in een skin de stage van de skin en een globaal menu de globale, de stage die `Exit` zou openen. Het spel maakt de stages bij het starten en bij het opnieuw laden van de skin, dus een stage die geïnstalleerd wordt terwijl het spel draait, verschijnt na een herstart.

## Er een delen

Een eigen stage is zijn map. Zip de map zelf, zodat het archief opent met `<name>/Script.lua`, met alles wat hij laadt en de credits van zijn bestanden. Spelers pakken hem uit in `Global/Modules/Stages/` in hun spelmap, of in `Modules/Stages/` van een skin, en starten het spel opnieuw. Een update voegt bestanden toe en vervangt ze, maar wist er geen, dus kies een mapnaam die geen meegeleverde stage gebruikt.

## De tutorial

`custom_stage_tutorial` toont in het spel de vier dingen die je moet weten om een eigen stage te maken en te delen: waar zijn map hoort (en waar niet), de naamruimte `CStage`, alles wat hij gebruikt in zijn map en van buiten alleen de meegeleverde modules, en waar je deze documentatie leest, het tabblad Documentatie van de OpenTaiko Hub. Zijn pagina's zijn de afbeeldingen van zijn map `Textures/` en de teksten van zijn map `lang/`, zodat ze bestand voor bestand te vervangen zijn.
