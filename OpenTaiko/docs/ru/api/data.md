<!-- api/data.md -->

# Данные и хранение

Сохранение данных, переживающих перезапуск, запросы к файлам SQLite, чтение файлов, JSON и INI из папки модуля, совместное использование ресурсов между модулями, а также чтение и изменение конфигурации игры.

Относительные пути, передаваемые в DATABASE, SQL, STORAGE, JSONLOADER, INILOADER и SHARED, разрешаются относительно каталога выполняемого модуля (папки, содержащей его `Script.lua`).

Некоторые методы на этой странице возвращают коллекции .NET, которые ведут себя иначе, чем таблицы Lua:

- Массивы (`string[]`, `int[]`, `double[]`) начинаются с индекса 0 и предоставляют `.Length`.
- Словари (разобранный JSON, строки SQL, карты языков) принимают `d["key"]` (или `d[1]` для массивов, разобранных из JSON) и перечисляются через `d:GetEnumerator()`; `pairs` и `#` с ними не работают.

```lua
local files = STORAGE:GetFiles("maps", "*.json")
for i = 0, files.Length - 1 do
    print(files[i])
end

local e = dict:GetEnumerator()
while e:MoveNext() do
    print(e.Current.Key, e.Current.Value)
end
```

## База данных «ключ-значение»

### DATABASE

Открывает хранилища LMDB «ключ-значение», которые сохраняют строковые значения между перезапусками, либо внутри папки модуля, либо в общей папке данных игры.

<div class="callout warn">
Каждый Read и Write открывает и закрывает собственное окружение LMDB, поэтому каждый вызов дорог; кэшируйте значения в Lua и обновляйте кэш при записи. Внутри модулей только для чтения (ROActivity и фоны) DATABASE возвращает хранилища, чей Write записывает ошибку в журнал и ничего не делает.
</div>

| Метод | Описание |
| --- | --- |
| `DATABASE:OpenLocalDatabase(path)  -> database` | Открывает (при необходимости создавая) хранилище по пути относительно каталога модуля. |
| `DATABASE:OpenGlobalDatabase(path)  -> database` | Открывает (при необходимости создавая) хранилище в `Global/ApplicationData/LMDB/` в папке игры, общее для всех модулей. |

### Дескриптор базы данных

Хранилище «ключ-значение», возвращаемое DATABASE, сопоставляющее строковые ключи со строковыми значениями.

<div class="callout warn">
Значения только строковые; числа и логические значения преобразуйте сами (например через tostring и tonumber).
</div>

| Метод | Описание |
| --- | --- |
| `database:Write(key, value)  -> nil` | Сохраняет строку под ключом и фиксирует её. В модулях только для чтения записывает ошибку в журнал и ничего не делает. |
| `database:Read(key)  -> string` | Возвращает строку, сохранённую под ключом, или nil, если ключ отсутствует или чтение не удалось. |
| `database:Dispose()  -> nil` | Ничего не делает; дескриптор не удерживает открытых ресурсов. |

```lua
local db

function activate()
    db = DATABASE:OpenGlobalDatabase("GameStatus")
    if db:Read("new_user") == nil then
        db:Write("new_user", "true")
    end
end
```

## База данных SQL

### SQL

Открывает файл базы данных SQLite внутри каталога модуля для выполнения SQL-запросов.

| Метод | Описание |
| --- | --- |
| `SQL:OpenSQLDatabase(path)  -> sql` | Открывает базу данных SQLite по пути относительно каталога модуля. |

### Дескриптор SQL

Соединение SQLite, возвращаемое SQL:OpenSQLDatabase.

<div class="callout warn">
Query возвращает словарь с ключами 1..n; каждая строка — словарь с ключами по именам столбцов (см. примечание о коллекциях .NET в начале страницы). Неудачный запрос записывает ошибку в журнал и возвращает пустой результат. Query передаёт текст запроса в SQLite без изменений и без привязки параметров, поэтому экранируйте любое значение, которое вы в него подставляете.
</div>

| Метод | Описание |
| --- | --- |
| `sql:Query(query)  -> rows` | Выполняет SQL-текст и возвращает строки результата. |

```lua
local db

function activate()
    db = SQL:OpenSQLDatabase("Databases/Items.db3")
    local rows = db:Query("SELECT * FROM itempool WHERE Slot = 'regular'")
    for i = 1, rows.Count do
        print(rows[i]["Code"])
    end
end
```

## Файлы, JSON и INI

### STORAGE

Доступ к файлам с корнем в каталоге модуля, а также вспомогательные методы для обмена кодами онлайн-лобби.

<div class="callout warn">
WriteText пишет только внутри папки модуля: он отклоняет абсолютные пути и пути, выходящие за её пределы. ReadText принимает и абсолютный путь. Вспомогательные методы кодов лобби используют общую папку `Global/Lobbycodes/` рядом с исполняемым файлом. Модули только для чтения могут использовать каждый метод STORAGE.
</div>

| Метод | Описание |
| --- | --- |
| `STORAGE:GetFiles(dir, pattern)  -> string[]` | Перечисляет файлы в подкаталоге папки модуля, соответствующие шаблону поиска, например `"*.png"`. Записи — пути относительно папки модуля, включая подкаталог. Каталог должен существовать. |
| `STORAGE:FileExists(path)  -> bool` | Истина, если файл существует по пути относительно папки модуля. |
| `STORAGE:DirectoryExists(path)  -> bool` | Истина, если каталог существует по пути относительно папки модуля. |
| `STORAGE:WriteText(name, contents)  -> bool` | Записывает текст в файл внутри папки модуля, создавая подкаталоги. Возвращает false для абсолютных или выходящих за пределы путей, а также при сбое. |
| `STORAGE:ReadText(name)  -> string` | Возвращает необработанный текст файла (относительно папки модуля или по абсолютному пути) или nil, если он отсутствует или нечитаем. |
| `STORAGE:GetFullPath(name)  -> string` | Возвращает абсолютный путь файла внутри папки модуля или nil для пустого или абсолютного входного значения. |
| `STORAGE:WriteLobbyCode(name, contents)  -> bool` | Записывает файл в `Global/Lobbycodes/`. Возвращает false для пустых, абсолютных имён или имён с `..`, а также при сбое. |
| `STORAGE:RevealLobbyCodes()  -> bool` | Открывает файловый менеджер ОС в `Global/Lobbycodes/`. |
| `STORAGE:RevealInExplorer(name)  -> bool` | Открывает файловый менеджер ОС с выделенным указанным файлом модуля (Windows и macOS) или его папку на других системах. |

### JSONLOADER

Разбирает JSON-файлы и строки из каталога модуля.

<div class="callout warn">
Разобранные значения — словари .NET: объекты индексируются именами членов, массивы — ключами 1..n. Прямое обращение к отсутствующему ключу (`d["x"]`) возбуждает ошибку, поэтому для поиска, который может не удаться, используйте JsonGet. Числа становятся целыми или double; строки, логические значения и null отображаются на свои эквиваленты в Lua. LoadJson возвращает дерево JsonNode; индексируйте его как `node["member"]` и преобразуйте листья через ExtractNumber / ExtractText.
</div>

| Метод | Описание |
| --- | --- |
| `JSONLOADER:LoadJson(name)  -> JsonNode` | Разбирает JSON-файл относительно каталога модуля в дерево JsonNode. Возбуждает ошибку, если файл отсутствует. |
| `JSONLOADER:ExtractNumber(value)  -> number` | Преобразует лист JsonNode в число; возвращает 0 для nil или нечисловых значений. |
| `JSONLOADER:ExtractText(value)  -> string` | Преобразует лист JsonNode в строку; возвращает nil для nil. |
| `JSONLOADER:JsonParseFile(name)  -> dict` | Разбирает JSON-файл, корень которого — объект (пустой словарь, если файл пуст). Возбуждает ошибку, если файл отсутствует или корень не является объектом. |
| `JSONLOADER:JsonParseFileAny(name)  -> dict` | Разбирает JSON-файл, корень которого — объект или массив (относительный или абсолютный путь). Возвращает nil, если файл отсутствует или пуст. |
| `JSONLOADER:JsonParseString(json)  -> dict` | Разбирает JSON-строку, корень которой — объект (пустой словарь, если строка пуста). Возбуждает ошибку, если корень не является объектом. |
| `JSONLOADER:JsonParseStringAny(json)  -> dict` | Разбирает JSON-строку, корень которой — объект или массив. Возвращает nil для пустого или некорректного входа. |
| `JSONLOADER:JsonGet(dict, key)  -> value` | Ищет строковый ключ в объекте или целочисленный ключ в массиве; возвращает nil, если ключ отсутствует или dict не является разобранным значением. |
| `JSONLOADER:JsonCount(dict)  -> int` | Число членов разобранного объекта или массива, либо 0 для всего остального. |

```lua
local data = JSONLOADER:JsonParseFileAny("Config/layout.json")
local colors = data and JSONLOADER:JsonGet(data, "colors")
local n = JSONLOADER:JsonCount(colors)
for i = 1, n do
    print(JSONLOADER:JsonGet(colors, i))
end
```

### INILOADER

Загружает плоский файл `key=value` из каталога модуля.

<div class="callout warn">
Загрузчик разделяет каждую строку по первому `=`, пропускает строки без него и позволяет повторяющемуся ключу перезаписать предыдущее значение. Секций, комментариев и кавычек у него нет. Отсутствующий файл даёт пустой дескриптор.
</div>

| Метод | Описание |
| --- | --- |
| `INILOADER:LoadIni(name)  -> ini` | Читает файл `key=value` относительно каталога модуля. |

### Дескриптор INI

Разобранный INI-файл, возвращаемый INILOADER:LoadIni, с типизированными геттерами.

<div class="callout warn">
Геттеры возвращают переданное значение по умолчанию, если ключ отсутствует. Если ключ существует, но его значение не удаётся разобрать, числовые геттеры возвращают 0. Геттеры массивов разделяют значение по запятым и возвращают пустой массив, если ключ отсутствует.
</div>

| Метод | Описание |
| --- | --- |
| `ini:GetBool(key, default)  -> bool` | Истина, если значение разбирается как целое 1; значение по умолчанию, если ключ отсутствует. |
| `ini:GetInt(key, default)  -> int` | Значение как целое число. |
| `ini:GetDouble(key, default)  -> number` | Значение как double. |
| `ini:GetString(key, default)  -> string` | Необработанное строковое значение. |
| `ini:GetStringArray(key)  -> string[]` | Значение, разделённое по запятым. |
| `ini:GetIntArray(key)  -> int[]` | Значение, разделённое по запятым, каждая часть разобрана как целое (0, если не разбирается). |
| `ini:GetDoubleArray(key)  -> double[]` | Значение, разделённое по запятым, каждая часть разобрана как double (0, если не разбирается). |

## Общие ресурсы и конфигурация

### SHARED

Общее для всей игры хранилище текстур, звуков и строк, переживающее смену сцен, чтобы каждый модуль мог использовать ресурс, загруженный один раз (например при запуске).

<div class="callout warn">
Методы Set* загружают в фоновом потоке и подменяют ресурс в потоке рендеринга; необязательный обратный вызов onCreate получает новый дескриптор, как только тот установлен. Более новый Set* по тому же ключу отбрасывает ещё идущую загрузку. Подмена освобождает предыдущий ресурс, что делает недействительным любой дескриптор, полученный до перезагрузки; получите его заново через Get*. Варианты UsingAbsolutePath принимают полный путь. Дескрипторы текстур см. на странице «Графика и текст», дескрипторы звуков — на странице «Аудио».
</div>

| Метод | Описание |
| --- | --- |
| `SHARED:SetSharedString(key, value)  -> nil` | Сохраняет строку под ключом. |
| `SHARED:GetSharedString(key)  -> string` | Возвращает строку, сохранённую под ключом, или пустую строку. |
| `SHARED:GetSharedTexture(key)  -> texture` | Возвращает общую текстуру для ключа или пустую текстуру, если она не была задана. |
| `SHARED:GetSharedSound(key)  -> sound` | Возвращает общий звук для ключа или пустой звук, если он не был задан. |
| `SHARED:ClearSharedTexture(key)  -> nil` | Освобождает текстуру, сохранённую под ключом, и заменяет её пустой. |
| `SHARED:ClearSharedSound(key)  -> nil` | Освобождает звук, сохранённый под ключом, и заменяет его пустым. |
| `SHARED:SetSharedTexture(key, path, onCreate?)  -> nil` | Загружает текстуру в хранилище по пути относительно модуля. |
| `SHARED:SetSharedTexture(key, path, options, onCreate?)  -> nil` | То же, с таблицей параметров; `{ maxSize = N }` ограничивает длинную сторону декодированной текстуры N пикселями. |
| `SHARED:SetSharedTextureUsingAbsolutePath(key, path, onCreate?)  -> nil` | Загружает текстуру по абсолютному пути. |
| `SHARED:SetSharedTextureUsingAbsolutePath(key, path, options, onCreate?)  -> nil` | Загружает текстуру по абсолютному пути с таблицей параметров. |
| `SHARED:SetSharedSFX(key, path, onCreate?)  -> nil` | Загружает звуковой эффект по пути относительно модуля. |
| `SHARED:SetSharedBGM(key, path, onCreate?)  -> nil` | Загружает фоновую музыку (группа громкости воспроизведения песни) по пути относительно модуля. |
| `SHARED:SetSharedVoice(key, path, onCreate?)  -> nil` | Загружает голосовую реплику по пути относительно модуля. |
| `SHARED:SetSharedPreview(key, path, onCreate?)  -> nil` | Загружает фрагмент превью песни по пути относительно модуля. |
| `SHARED:SetSharedSFXUsingAbsolutePath(key, path, onCreate?)  -> nil` | Загружает звуковой эффект по абсолютному пути. |
| `SHARED:SetSharedBGMUsingAbsolutePath(key, path, onCreate?)  -> nil` | Загружает фоновую музыку по абсолютному пути. |
| `SHARED:SetSharedVoiceUsingAbsolutePath(key, path, onCreate?)  -> nil` | Загружает голосовую реплику по абсолютному пути. |
| `SHARED:SetSharedPreviewUsingAbsolutePath(key, path, onCreate?)  -> nil` | Загружает фрагмент превью песни по абсолютному пути. |

```lua
-- в стартовой сцене
function onStart()
    SHARED:SetSharedSFX("Decide", "Sounds/Decide.ogg")
end

-- в любом последующем модуле
SHARED:GetSharedSound("Decide"):Play()
```

### CONFIG

Читает и изменяет конфигурацию игры: число игроков, режимы, подсчёт очков, игровые моды для каждого игрока и уровни громкости.

<div class="callout warn">
Свойства используют синтаксис с точкой (`CONFIG.PlayerCount`), методы — синтаксис с двоеточием. Индексы игроков начинаются с 0 (от 0 до 4); сеттеры игнорируют индексы вне диапазона, а геттеры возвращают для них значение по умолчанию. Внутри модулей только для чтения (ROActivity и фоны) каждый сеттер записывает ошибку в журнал и ничего не делает. Изменения применяются в памяти немедленно; игра записывает Config.ini при нормальном закрытии.
</div>

| Метод | Описание |
| --- | --- |
| `CONFIG.ConfigIsNew  -> bool (read-only)` | Истина, если игра создала конфигурацию при этом запуске. |
| `CONFIG.Language  -> string (read-only)` | Идентификатор языка, сохранённый в Config.ini. |
| `CONFIG.PlayerCount  -> int` | Число активных игроков. Сеттер игнорирует значения вне диапазона 1..5. |
| `CONFIG.IsAIBattleMode  -> bool` | Включён ли режим битвы с ИИ. |
| `CONFIG.AILevel  -> int` | Уровень сложности ИИ, при записи ограничивается диапазоном 1..10. |
| `CONFIG.IsTrainingMode  -> bool` | Включён ли режим тренировки. |
| `CONFIG.UseModernScoringMethod  -> bool` | Использует ли игра современный метод подсчёта очков (shin-uchi). |
| `CONFIG.UsedLegacyScoringMethod  -> int` | Поколение устаревшего подсчёта очков (см. `CONFIG.LEGACY_SCORING`), при записи ограничивается диапазоном 0..3. |
| `CONFIG.AreSongUnlockablesDisabled  -> bool (read-only)` | Игнорирует ли игра условия разблокировки песен. |
| `CONFIG.SongSpeed  -> int` | Скорость песни в двадцатых долях множителя: 20 — это 1.0x. При записи ограничивается диапазоном 2..200 (от 0.1x до 10x). |
| `CONFIG.MasterVolume  -> int` | Общая громкость, при записи ограничивается диапазоном 0..100. |
| `CONFIG.SoundEffectVolume  -> int` | Громкость звуковых эффектов, при записи ограничивается диапазоном 0..100. |
| `CONFIG.VoiceVolume  -> int` | Громкость голоса, при записи ограничивается диапазоном 0..100. |
| `CONFIG.SongVolume  -> int` | Громкость воспроизведения песни, при записи ограничивается диапазоном 0..100. |
| `CONFIG.PreviewVolume  -> int` | Громкость превью песни, при записи ограничивается диапазоном 0..100. |
| `CONFIG:GetGameType(player)  -> int` | Тип игры игрока (см. `CONFIG.GAMETYPE`); Taiko для индексов вне диапазона. |
| `CONFIG:SetGameType(player, gameType)  -> nil` | Задаёт тип игры игрока и игнорирует неопределённые значения. |
| `CONFIG:GetDefaultCourse(player)  -> int` | Сложность по умолчанию (см. `CONFIG.DEFAULT_COURSE`); Normal для индексов вне диапазона. Несмотря на аргумент player, одна глобальная настройка действует для всех игроков. |
| `CONFIG:SetDefaultCourse(player, difficulty)  -> nil` | Задаёт глобальную сложность по умолчанию, ограниченную диапазоном от Easy до значения на единицу больше Extra Extreme (объединённое отображение Extra/Extra-Extra). |
| `CONFIG:GetScrollSpeed(player)  -> int` | Значение скорости прокрутки игрока: 9 — это 1.0x, каждый шаг — 0.1x (см. `CONFIG.SCROLLSPEED`). 9 для индексов вне диапазона. |
| `CONFIG:SetScrollSpeed(player, speed)  -> nil` | Задаёт значение скорости прокрутки игрока, ограниченное диапазоном 0..99. |
| `CONFIG:GetTimingZone(player)  -> int` | Окно тайминга игрока: 0 Loose, 1 Lenient, 2 Normal, 3 Strict, 4 Rigorous. 2 для индексов вне диапазона. |
| `CONFIG:SetTimingZone(player, zone)  -> nil` | Задаёт окно тайминга игрока, ограниченное диапазоном 0..4. |
| `CONFIG:GetAutoStatus(player)  -> bool` | Истина, если игрок в режиме автоигры или смотрит реплей. |
| `CONFIG:SetAutoStatus(player, isAuto)  -> nil` | Включает или выключает автоигру для игрока. |
| `CONFIG:GetRandomMod(player)  -> int` | Мод Random игрока (см. `CONFIG.RANDOM`); Off для индексов вне диапазона. |
| `CONFIG:SetRandomMod(player, mode)  -> nil` | Задаёт мод Random игрока и игнорирует неопределённые значения. |
| `CONFIG:GetFunMod(player)  -> int` | Фан-мод игрока (см. `CONFIG.FUN`); None для индексов вне диапазона. |
| `CONFIG:SetFunMod(player, mod)  -> nil` | Задаёт фан-мод игрока и игнорирует неопределённые значения. |
| `CONFIG:GetStealthMod(player)  -> int` | Мод Stealth игрока (см. `CONFIG.STEALTH`); Off для индексов вне диапазона. |
| `CONFIG:SetStealthMod(player, mode)  -> nil` | Задаёт мод Stealth игрока и игнорирует неопределённые значения. |
| `CONFIG:GetJusticeMod(player)  -> int` | Мод оценки игрока: 0 выключен, 1 Just (Ok считается как Bad), 2 Safe (Bad считается как Ok). |
| `CONFIG:SetJusticeMod(player, mode)  -> nil` | Задаёт мод оценки игрока, ограниченный диапазоном 0..2. |
| `CONFIG:GetModFlags(player)  -> integer` | Упаковывает скорость прокрутки, Stealth, Random, скорость песни, окно тайминга, мод оценки и фан-мод в одно 64-битное значение (по байту на каждый). |
| `CONFIG:SetModFlags(player, flags)  -> nil` | Применяет к игроку значение, полученное из GetModFlags (скорость песни глобальная). |

Таблицы констант в CONFIG дают имена целочисленным значениям выше. `SONGSPEED` и `SCROLLSPEED` также преобразуют сохранённые значения в множители и обратно.

| Член | Описание |
| --- | --- |
| `CONFIG.SONGSPEED.Normal  -> int` | 20, сохранённое значение для 1.0x. |
| `CONFIG.SONGSPEED:ToActual(value)  -> number` | Преобразует сохранённую скорость песни в её множитель (value / 20). |
| `CONFIG.SONGSPEED:FromActual(multiplier)  -> int` | Преобразует множитель в ближайшую сохранённую скорость песни. |
| `CONFIG.SCROLLSPEED.Normal  -> int` | 9, сохранённое значение для 1.0x. |
| `CONFIG.SCROLLSPEED:ToActual(value)  -> number` | Преобразует сохранённую скорость прокрутки в её множитель ((value + 1) / 10). |
| `CONFIG.SCROLLSPEED:FromActual(multiplier)  -> int` | Преобразует множитель в ближайшую сохранённую скорость прокрутки. |
| `CONFIG.GAMETYPE` | `Taiko`, `Konga`. |
| `CONFIG.DEFAULT_COURSE` | `Easy`, `Normal`, `Hard`, `Oni`, `Edit`. |
| `CONFIG.LEGACY_SCORING` | `Gen1Oni`, `Gen1_2`, `Gen2`, `Gen3`. |
| `CONFIG.RANDOM` | `Off`, `Random`, `Mirror`, `SuperRandom`, `MirrorRandom`. |
| `CONFIG.STEALTH` | `Off`, `Doron`, `Stealth`. |
| `CONFIG.FUN` | `None`, `Avalanche`, `Minesweeper`, `DynamicBeat`, `Total`. |
| `CONFIG.JUSTICE` | `None`, `Just`, `Safe`. |

```lua
local multiplier = CONFIG.SONGSPEED:ToActual(CONFIG.SongSpeed)
if CONFIG:GetRandomMod(0) == CONFIG.RANDOM.Mirror then
    -- зеркальный чарт
end
```
