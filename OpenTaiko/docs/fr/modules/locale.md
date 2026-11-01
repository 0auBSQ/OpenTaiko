<!-- modules/locale.md -->

# Locale

## Locale.i18n

Les traductions propres à un module, rangées à côté de lui plutôt que dans les `Locales/` du skin. Les textes se trouvent dans `lang/<code>/<name>.json`, un fichier par fonctionnalité, indexés par des identifiants stables ; `lang/en/` contient l'anglais et sert de repli pour toutes les autres langues. Les codes sont les identifiants de langue du jeu : `en`, `de`, `es`, `fr`, `ja`, `ko`, `nl`, `ru`, `zh`.

```
Global/Modules/Stages/my_stage/
  Script.lua
  lang/
    en/menu.json      { "title": "Settings", "count": "%d items", "tips": ["One", "Two"] }
    ja/menu.json      { "title": "設定", "count": "%d 件" }
```

| Fonction ou champ | Description |
| --- | --- |
| `I18N.texts(name, dir?)  -> texts` | Les textes de `lang/<code>/<name>.json`. `dir` est le dossier qui contient `lang/` : omettez-le (ou passez `""`) dans un stage ou une activité pour utiliser son propre dossier, et passez le dossier de `Core.Here` dans un fichier de Lib. Les fichiers sont lus à la première utilisation puis conservés. |
| `I18N.detect()` | Lit la langue actuelle du jeu et en fait celle qu'utilise chaque recherche. Appelez-la dans `activate`, qui s'exécute à chaque entrée, pour qu'une langue changée dans les réglages s'applique à la visite suivante. |
| `I18N.load(code)` | Utilise la langue `code` (pour un banc d'essai ou un aperçu de langue). |
| `I18N.currentCode()  -> string` | L'identifiant de la langue actuelle du jeu, `"en"` quand le moteur n'en fournit aucun. |
| `I18N.code` | La langue qu'utilisent actuellement les recherches. |
| `I18N.localizedPath(path, code?)  -> string` | `<name>_<code>.<ext>` quand une texture de ce nom existe à côté de `path`, sinon `path` : pour les images où du texte est incrusté. |

| Méthode | Description |
| --- | --- |
| `texts:tr(id)  -> string` | Le texte de `id` ; l'identifiant lui-même quand aucune langue ne l'a, ce qui rend le manque visible à l'écran. |
| `texts:trf(id, ...)  -> string` | Le texte de `id` utilisé comme motif `string.format` avec les arguments. |
| `texts:get(...)  -> string` | La chaîne à un chemin de clés (`texts:get("menu", "title")` pour des objets imbriqués), nil si elle manque ou est vide. |
| `texts:list(...)  -> table` | Le tableau de chaînes à un chemin de clés ; chaque entrée se replie sur l'anglais indépendamment. |
| `texts:loc(...)  -> handle` | Un handle dont `GetString(fallback)` lit le chemin de clés au moment de l'appel, si bien qu'un nom mis en cache au chargement suit un changement de langue ultérieur. |
| `texts:node(...)  -> value` | Le nœud JSON brut à un chemin de clés (langue actuelle, sinon anglais). |

```lua
local I18N = require("Locale.i18n")
local T = I18N.texts("menu")
local title, count

function activate()
    I18N.detect()
    title = T:tr("title")
    count = T:trf("count", #items)
end
```

<div class="callout warn">
Les recherches suivent `I18N.code`, qui ne change que lorsque `detect` ou `load` s'exécute. Un module qui reste à l'écran pendant que le joueur change de langue peut définir la globale `reloadLanguage(lang)` et y appeler `detect`. Les modules de textes de Pagoda, Vault et Shop vérifient eux-mêmes la langue à chaque recherche.
</div>

Les modules plus anciens utilisent à la place un dictionnaire à l'anglais exact : `lang/<code>.lua` renvoie une table associant chaque chaîne anglaise à sa traduction, `detect` charge celle de la langue actuelle, et les fonctions `I18N.tr(s)` et `I18N.trf(fmt, ...)` y cherchent les chaînes. Cette forme fonctionne toujours ; les nouveaux modules doivent utiliser `texts`.
