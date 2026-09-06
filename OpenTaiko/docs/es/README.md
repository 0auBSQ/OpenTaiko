<!-- docs/README.md -->

# Documentación de OpenTaiko

<span class="badge-new">Versión del juego 0.6.1</span>

<div class="callout warn">Esta documentación todavía está en revisión y puede cambiar hasta el lanzamiento.</div>

OpenTaiko dibuja casi todas las pantallas ajenas al juego principal con scripts Lua incluidos dentro de un skin: los menús, la selección de canciones, las escenas de la habitación y de la historia, los fondos, la pantalla de resultados. Este sitio documenta la API Lua que usan esos scripts y recorre las tareas más habituales de quien crea un skin.

## Por dónde empezar

- Si todavía no has escrito un módulo, lee [Cómo funcionan los módulos](getting-started.md): la estructura de carpetas de un skin, los tipos de módulo y los callbacks del ciclo de vida que recibe un script.
- Para buscar una función, abre la [referencia de la API](api/) y elige una categoría en la barra lateral.
- Para un objetivo concreto, las guías explican paso a paso los [personajes](guides/characters.md), los [puchicharas](guides/puchicharas.md), los [skins y temas](guides/skins.md) y los [desbloqueables de charts](guides/unlockables.md).

## Qué puedes crear

| Área | Dónde vive | Qué es |
| --- | --- | --- |
| Módulos | La carpeta `Modules/` del skin | Stages (pantallas completas con su propia entrada, dibujo y estado: una selección de canciones, una habitación, una escena de historia), activities (subpantallas y superposiciones como los diálogos y las placas de nombre) y transiciones (las pantallas de fundido y de carga entre stages). |
| Fondos | La carpeta `Graphics/` del skin | Scripts que decoran una pantalla: la pantalla de arranque, la habitación, el fondo de juego y el mob, la pantalla de resultados. |
| Skins y temas | `System/`, junto al juego | Un paquete visual completo que un jugador instala y activa, con los ajustes de tema que expone en la pantalla de opciones. |
| Personajes y puchicharas | `Global/Characters/` y `Global/PuchiChara/`, compartidas por todos los skins | Los bailarines y las pequeñas mascotas que reaccionan durante la partida. |
| Desbloqueables de charts | La carpeta de la canción, junto al chart | Un `Unlock.json` que mantiene bloqueada una canción hasta que el jugador se la gana. |

## Secciones experimentales

Algunas páginas llevan una etiqueta <span class="badge-exp">Experimental</span> en la barra lateral. Las funciones que describen funcionan en la versión actual, pero su API todavía puede cambiar entre versiones sin un periodo de obsolescencia previa. Un skin que dependa de ellas debería indicar a qué versión del juego está destinado, y deberías volver a probarlo tras cada actualización.

## Idiomas

La documentación existe en todos los idiomas que incluye OpenTaiko; elige uno con el selector de idioma en la parte superior de la barra lateral. El sitio muestra una página en inglés hasta que exista su traducción.
