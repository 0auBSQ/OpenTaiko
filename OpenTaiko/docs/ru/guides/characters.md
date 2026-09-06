<!-- guides/characters.md -->

# Добавление персонажа

Персонаж — это папка в `Global/Characters/` в папке установки игры. Каждая подпапка, которую игра там находит, становится одним доступным для выбора персонажем. Папка содержит `Metadata.json` (имя, редкость, автор), `CharaConfig.txt` (позиции и тайминг анимаций), содержимое анимаций и, при необходимости, `Effects.json`, `Unlock.json`, `Palettes.json` и голосовые реплики. Содержимое анимаций — это либо папки с пронумерованными PNG-кадрами, которые рисует встроенный скрипт персонажа игры, либо всё, что решит нарисовать собственный `Script.lua` персонажа (поставляемый 3D-шаблон рисует glTF-модель).

Совместимость: OpenTaiko 0.6.1 по-прежнему загружает персонажей, созданных для 0.6.0, без изменений. Эта страница описывает актуальную структуру; используйте её для новых персонажей.

## Прежде чем начать

- Установленный OpenTaiko 0.6.1. Игра читает персонажей из `Global/Characters/` рядом с исполняемым файлом игры, и все скины используют их совместно.
- Текстовый редактор для JSON и файлов в стиле INI.
- Для 2D-персонажа: графика, экспортированная в пронумерованные PNG-кадры (`0.png`, `1.png`, ...) с прозрачным фоном, по одной папке на состояние анимации.
- Для 3D-персонажа: `model.glb` (бинарный glTF) с клипами анимаций и статичное изображение `Render.png`.
- Поставляемые папки `01 - Template` (2D) и `01 - Template3D`. Скопируйте одну из них как отправную точку.

## Шаг 1: разберитесь в обнаружении, порядке и идентичности

При запуске игра перечисляет подпапки `Global/Characters/` и создаёт по одному персонажу на папку в том порядке, в котором их возвращает файловая система. Игра не сортирует список, поэтому поставляемые папки несут числовой префикс (`00 - None`, `01 - Template`, `02 - Student (A)`, ...), чтобы порядок оставался предсказуемым. Держите `00 - None` первой: индекс 0 — это пустой слот и запасной вариант, когда сохранённый персонаж отсутствует.

Файлы сохранений хранят выбранного персонажа по имени папки (`characterName`) и заново разрешают его в индекс при каждом запуске. Добавление или удаление других папок никогда не ломает сохранённый выбор, но переименование папки заставляет ссылавшиеся на неё сохранения откатиться к `00 - None`. Два персонажа могут иметь одинаковое отображаемое имя; имя папки должно быть уникальным.

Игра перечисляет персонажей один раз при запуске и снова при перезагрузке скина; папка, добавленная во время работы игры, появляется после следующего запуска или перезагрузки скина.

## Шаг 2: создайте папку и Metadata.json

Создайте папку, например `30 - MyChara`, и добавьте `Metadata.json`:

- `name`: отображаемое имя. Либо простая строка, либо локализованный объект `{ "strings": { "default": "...", "ja": "...", ... } }`. `default` — запасное значение; остальные ключи — коды языков игры.
- `rarity`: одно из `Poor`, `Common`, `Uncommon`, `Rare`, `Epic`, `Legendary`, `Mythical`. Редкость управляет только цветом и уровнем уведомления о разблокировке; у каждой редкости множитель монет равен 1.
- `author`: простая строка или локализованный объект.
- `description`: необязательно, простая строка или локализованный объект.
- `speechtext`: необязательный массив из шести локализованных объектов, которые экран результатов показывает в речевом пузыре персонажа. Игра выбирает запись по результату в следующем порядке: провал с низкой шкалой, провал со шкалой 40% и выше, клир, клир с полной шкалой, фулл-комбо, олл-перфект. Если вы задали меньше шести, игра повторяет последнюю.

Если `Metadata.json` отсутствует, персонаж всё равно загружается с именем `(None)`, редкостью `Common` и автором `(None)`.

```json
{
  "name": {
    "strings": {
      "default": "My Character",
      "ja": "マイキャラ"
    }
  },
  "rarity": "Common",
  "author": {
    "strings": {
      "default": "Your Name"
    }
  }
}
```

## Шаг 3 (2D-вариант): добавьте папки кадров

Когда в папке нет `Script.lua`, игра рисует персонажа встроенным скриптом (`CharaScript.lua` в папке установки игры). Этот скрипт сопоставляет каждое состояние анимации с подпапкой и загружает из неё `0.png`, `1.png`, `2.png`, .... Загрузка останавливается на первом отсутствующем индексе, поэтому нумерация должна быть непрерывной.

| Состояние анимации | Папка |
|---|---|
| Game/Normal, Game/Clear, Game/Max | `Normal`, `Clear`, `Clear_Max` |
| Game/Gogo, Game/Gogo_Max | `GoGo`, `GoGo_Max` |
| Game/Miss, Game/Miss_Down | `Miss`, `MissDown` |
| Game/10combo, Game/10combo_Max | `10combo`, `10combo_Max` |
| Game/Cleared, Game/Failed | `Cleared`, `Failed` |
| Game/Clear_In, Game/Clear_Out | `Clearin`, `ClearOut` |
| Game/Max_In, Game/Max_Out | `Soulin`, `SoulOut` |
| Game/Miss_In, Game/Miss_Down_In, Game/Return | `MissIn`, `MissDownIn`, `Return` |
| Game/GoGoStart, Game/GoGoStart_Clear, Game/GoGoStart_Max | `GoGoStart`, `GoGoStart_Clear`, `GoGoStart_Max` |
| Game/Balloon_Breaking, Game/Balloon_Broke, Game/Balloon_Miss | `Balloon_Breaking`, `Balloon_Broke`, `Balloon_Miss` |
| Game/Kusudama_Breaking, Game/Kusudama_Broke, Game/Kusudama_Miss, Game/Kusudama_Idle | `Kusudama_Breaking`, `Kusudama_Broke`, `Kusudama_Miss`, `Kusudama_Idle` |
| Game/Tower/Standing, Climbing, Running, Clear, Fail (и варианты `_Tired`) | `Tower_Char/Standing`, `Tower_Char/Climbing`, `Tower_Char/Running`, `Tower_Char/Clear`, `Tower_Char/Fail` (плюс `Tower_Char/Standing_Tired` и так далее) |
| Menu/Wait, Menu/Start, Menu/Normal, Menu/Select | `Menu_Wait`, `Menu_Start`, `Menu_Loop`, `Menu_Select` |
| Entry/Normal, Entry/Jump | `Title_Normal`, `Title_Entry` |
| Result/Normal, Result/Clear, Result/Failed_In, Result/Failed | `Result_Normal`, `Result_Clear`, `Result_Failed_In`, `Result_Failed` |

Встроенный скрипт читает два статичных изображения из корня папки: `Render.png` (полноразмерный портрет, рисуемый везде, где игра запрашивает тип анимации Render, например в комнате) и `Preview.png` (миниатюра; при отсутствии скрипт использует `Normal/0.png`).

Отсутствующие состояния откатываются к другому состоянию, поэтому персонаж может поставлять лишь часть набора. Цепочка откатов: Clear -> Normal, Max -> Clear, Miss -> Normal, Miss_Down -> Miss, Gogo -> Normal, Gogo_Max -> Gogo, 10combo_Max -> 10combo, GoGoStart_Clear -> GoGoStart, GoGoStart_Max -> GoGoStart_Clear, состояния Tower `_Tired` -> их обычное состояние, Tower/Fail -> Tower/Standing_Tired, Kusudama_Idle -> Normal, Menu/Wait -> Gogo, Menu/Start, Menu/Select и Entry/Jump -> 10combo, Menu/Normal, Entry/Normal и Result/Normal -> Normal, Result/Clear -> Clear, Result/Failed_In -> Miss_In, Result/Failed -> Miss. Состояния без отката (например Cleared, Failed, Return, состояния шара) при отсутствии ничего не рисуют. Минимум для работающего персонажа — `Normal/0.png`.

```
30 - MyChara/
  Metadata.json
  CharaConfig.txt
  Render.png
  Normal/0.png 1.png 2.png ...
  Clear/0.png ...
  GoGo/0.png ...
  Miss/0.png ...
  Menu_Loop/0.png ...
  Result_Clear/0.png ...
  Sounds/                (необязательные голосовые реплики, см. шаг 6)
```

## Шаг 4: напишите CharaConfig.txt

`CharaConfig.txt` — текстовый файл `Key=Value`; строки, начинающиеся с `;`, являются комментариями. Встроенный скрипт читает следующие ключи (поставляемый 3D-шаблон также читает ключи позиций):

- `Chara_Resolution=W,H` (по умолчанию `1280,720`): разрешение, для которого вы задаёте координаты ниже. Игра масштабирует позиции из этого разрешения в разрешение скина при отрисовке.
- `Chara_LegacyMode` (по умолчанию `1`): сохраняет привязку и поправки смещений версии 0.6.0. Персонажи, перенесённые из старых версий, на него полагаются.
- `Game_Chara_X=...` / `Game_Chara_Y=...`: позиция в игре; скрипт использует первое значение каждого списка. `Game_Chara_Offset=X,Y` — альтернативная форма.
- `Game_Chara_X_AI=...` / `Game_Chara_Y_AI=...`: по одному значению на игрока для битвы с ИИ. Когда присутствуют оба ключа, они заменяют позицию битвы с ИИ из скина для этого персонажа.
- `Game_Chara_Balloon_X` / `Game_Chara_Balloon_Y`, `Game_Chara_Kusudama_X` / `Game_Chara_Kusudama_Y`: позиции во время последовательностей шара и кусудамы (используется первое значение). `Game_Chara_Balloon_Offset`, `Game_Chara_Kusudama_Offset` и `Game_Chara_Tower_Offset` принимают пару `X,Y`.
- `Menu_Offset=X,Y`, `Menu_Chara_Scale`, `Result_Offset=X,Y`, `Heya_Chara_Render_Offset=X,Y`: смещения для меню, результатов и рендера в комнате.
- `Game_Chara_Motion_<State>=0,1,2,...`: порядок, в котором проигрываются кадры состояния, в виде индексов кадров от 0. Если опущен, кадры проигрываются в порядке файлов. Имена состояний повторяют имена папок, например `Game_Chara_Motion_Normal`, `Game_Chara_Motion_GoGo`, `Game_Chara_Motion_Miss_Down`, `Game_Chara_Motion_Balloon_Broke`, `Game_Chara_Motion_Tower_Climbing`.
- `Game_Chara_Beat_<State>=N`: сколько битов занимает один цикл состояния, например `Game_Chara_Beat_Normal=1`, `Game_Chara_Beat_GoGo=2`.
- Состояния меню, титульного экрана и результатов используют `Menu_Chara_Motion_Loop/Wait/Start/Select`, `Title_Chara_Motion_Normal/Entry`, `Result_Chara_Motion_Normal/Clear/Failed_In/Failed` с соответствующими ключами `_Beat_` или фиксированными длительностями в миллисекундах: `Chara_Menu_Loop_AnimationDuration`, `Chara_Menu_Wait_AnimationDuration`, `Chara_Menu_Start_AnimationDuration`, `Chara_Menu_Select_AnimationDuration`, `Chara_Normal_AnimationDuration`, `Chara_Entry_AnimationDuration`, `Chara_Result_Normal_AnimationDuration`, `Chara_Result_Clear_AnimationDuration`, `Chara_Result_Failed_In_AnimationDuration`, `Chara_Result_Failed_AnimationDuration`.

Полный список ключей со значениями по умолчанию — таблица `load_chara_config_defs` в начале встроенного `CharaScript.lua`. Неизвестные ему ключи скрипт игнорирует, поэтому поставляемый `01 - Template/CharaConfig.txt` содержит и несколько ключей со стороны скина, которые в этом файле не имеют эффекта.

```ini
Chara_Version=0.6.1.0
Chara_Resolution=1920,1080

;Позиция персонажа по X (1P,2P)
Game_Chara_X=0,0
;Позиция персонажа по Y (1P,2P)
Game_Chara_Y=0,805

;Порядок кадров обычного состояния и битов на цикл
Game_Chara_Motion_Normal=0,1,2,3,4,5,6
Game_Chara_Beat_Normal=1

;Порядок кадров GoGo и битов на цикл
Game_Chara_Motion_GoGo=0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15
Game_Chara_Beat_GoGo=2
```

## Шаг 5 (3D-вариант): поставьте model.glb и собственный Script.lua персонажа

Когда в папке персонажа есть `Script.lua`, он полностью заменяет встроенный скрипт. Игра тогда вызывает по имени следующие глобальные функции:

- `loadAnimation(animationType)`, `disposeAnimation(animationType)`
- `availableAnimation(animationType)`, возвращающая логическое значение. Игра всё ещё принимает старое написание с опечаткой `avaialbeAnimation`: она сначала пробует `availableAnimation` и откатывается к `avaialbeAnimation`. Поставляемый 3D-шаблон по-прежнему использует старое имя.
- `setAnimationDuration(animationType, durationMs)`, `resetAnimationCounter(animationType)`
- `update(delta, animationType, looping)`, возвращающая `true`, когда незацикленная анимация закончилась
- `draw(animationType, x, y, scaleX, scaleY, opacity, color, contextType, anchor, clipW, clipH, clipX, clipY, rotation, blendMode, wrapMode, gradientMap)`
- `getDrawSize(animationType)`, возвращающая ширину и высоту
- `getHeyaRenderOffset()`, возвращающая x и y; `getAIBattlePosition(player, charaScale)`, возвращающая x и y или `nil`, чтобы использовать позицию из скина
- `loadVoice(voiceType)`, `disposeVoice(voiceType)`, `playVoice(voiceType)`

Типы анимаций — это строки, стоящие за константами `CHARACTER.ANIM_*` (`"Game/Normal"`, `"Menu/Normal"`, ...), плюс два специальных типа `CHARACTER.ANIM_PREVIEW` (миниатюра) и `CHARACTER.ANIM_RENDER` (полный портрет). Типы голосов — константы `CHARACTER.VOICE_*`. Цепочка откатов из шага 3 действует и для скриптовых персонажей: игра запрашивает `availableAnimation` и перебирает альтернативы, пока одна не окажется доступной.

Поставляемая папка `01 - Template3D` содержит только `CharaConfig.txt`, `Effects.json`, `Metadata.json`, `model.glb`, `Render.png` и `Script.lua`. Её скрипт загружает `model.glb` через `MODEL:Load`, рендерит её в сцену, которую создаёт через `SCENE3D:CreateScene`, читает ключи позиций из `CharaConfig.txt` и сопоставляет каждый тип анимации с индексом клипа и числом битов в таблице `CLIP`. Чтобы сделать 3D-персонажа, скопируйте папку, замените `model.glb` и `Render.png` и отредактируйте `CLIP` так, чтобы каждый тип указывал на правильный индекс клипа вашей модели.

```lua
-- фрагмент из 01 - Template3D/Script.lua
local CLIP = {
  [CHARACTER.ANIM_GAME_NORMAL] = { clip = 0,  beat = 1 };
  [CHARACTER.ANIM_GAME_CLEAR]  = { clip = 1,  beat = 1 };
  [CHARACTER.ANIM_GAME_GOGO]   = { clip = 2,  beat = 4 };
  [CHARACTER.ANIM_MENU_NORMAL] = { clip = 14, beat = 2 };
  [CHARACTER.ANIM_RESULT_CLEAR]= { clip = 19, beat = 1 };
  -- по одной записи на каждое состояние анимации, которое поддерживает модель
}

function loadAnimation(animationType)
  -- постройте данные клипа / превью / рендера и пометьте их доступными
end

function availableAnimation(animationType)
  return animations[animationType] ~= nil
end
```

## Шаг 6: необязательные файлы: Effects.json, Unlock.json, Palettes.json, голоса

- `Effects.json`: `gauge` (`Normal`, `Hard` или `Extreme`; по умолчанию `Normal`) выбирает тип шкалы души. `Hard` умножает получаемые монеты на 1.5, а `Extreme` на 1.8, если игра не навязывает обычную шкалу. Когда активен фан-мод Minesweeper, `bombFactor` (1-100, по умолчанию 20) — процент нот, которые мод превращает в бомбы, а `fuseRollFactor` (0-100, по умолчанию 0) — процент шаров, которые он превращает в дроби-фитили.
- `Unlock.json`: если присутствует, персонаж остаётся заблокированным, пока игрок не выполнит условие. Формат и идентификаторы условий совпадают с песенными; см. руководство по разблокировке. Условия с монетами игрок покупает на экране комнаты; остальные условия игра проверяет автоматически на экране результатов. Поставляемые примеры: Kuro использует `{ "condition": "dp", "type": "me", "values": [3, 3, 10] }` (десять клиров чартов Extreme с фулл-комбо или лучше), а Aoi использует `{ "condition": "ch", "type": "me", "values": [200] }` (200 монет).
- `Palettes.json`: массив цветовых палитр, которые игрок может применить к персонажу. Каждая запись содержит `name`, `blend` (0-1), `stops` (массив опорных точек градиента `[position, R, G, B]` или `[position, R, G, B, A]`; задайте не менее двух) и `plays` — число игр этим персонажем, которое разблокирует палитру (0 или отсутствие означает доступность сразу). Запись со `"stops": null` — неокрашенный вариант по умолчанию.
- Голоса: встроенный скрипт загружает файлы `.ogg` по фиксированным путям внутри папки персонажа, например `Sounds/Clear/Clear.ogg`, `Sounds/Clear/Failed.ogg`, `Sounds/Clear/FullCombo.ogg`, `Sounds/Clear/AllPerfect.ogg`, `Sounds/Menu/SongSelect.ogg`, `Sounds/Menu/SongDecide.ogg`, `Sounds/Menu/DiffSelect.ogg`, `Sounds/Title/Sanka.ogg`, `Sounds/Result/BestScore.ogg`, `Sounds/Result/ClearSuccess.ogg`, `Sounds/Result/ClearFailed.ogg`. Полный список — таблица `voice_files` в начале встроенного `CharaScript.lua`. Отсутствующие файлы скрипт пропускает.

```json
{
  "gauge": "Normal",
  "bombFactor": 20,
  "fuseRollFactor": 0
}
```

```json
{
  "condition": "ch",
  "type": "me",
  "values": [ 200 ]
}
```

```json
[
  { "name": "Default", "stops": null },
  { "name": "Green", "blend": 1.0, "stops": [ [0, 0, 0, 0], [0.25, 0, 255, 0] ], "plays": 10 }
]
```

## Шаг 7: перезапустите игру и выберите персонажа

Перезапустите игру (или перезагрузите скин из настроек). Персонаж появится в списке персонажей на экране комнаты, где заблокированные персонажи показывают своё условие разблокировки. Lua-сцены также могут читать список через глобальный объект `CHARACTERLIST`, который предоставляет имя папки, отображаемое имя, редкость и условие разблокировки каждой записи.

## Устранение неполадок и примечания

- Персонаж не появляется: проверьте, что папка лежит непосредственно в `Global/Characters/`, и перезапустите игру. Игра строит список один раз при запуске.
- Персонаж ничего не рисует: отсутствует `Normal/0.png` или имена папок не совпадают с таблицей из шага 3. Кадры должны называться `0.png`, `1.png`, ... без пропусков; пропуск завершает анимацию на этом индексе без ошибки.
- Персонаж за пределами экрана или неправильного размера: `Chara_Resolution` должно совпадать с разрешением, для которого вы задали значения позиций. Когда ключ отсутствует, игра предполагает `1280,720`.
- Проигрывается только часть набора анимаций: состояниям без отката (Cleared, Failed, Return, состояния шара и кусудамы) нужна собственная папка.
- 3D-персонаж показывает все анимации как недоступные: `Script.lua` должен определять `availableAnimation` (или `avaialbeAnimation`) и возвращать `true` для загруженных типов.
- Присутствующий `Script.lua` полностью заменяет встроенный скрипт. Скриптовый персонаж всё ещё может загружать папки с пронумерованными PNG, но только если скрипт загружает их сам.
- Сохранения ссылаются на имя папки, поэтому переименование папки, которую игроки уже выбрали, сбрасывает их выбор на пустой слот.
- Поставляемые JSON-файлы содержат завершающие запятые. JSON-парсер игры их принимает; строгие валидаторы их отклоняют.
