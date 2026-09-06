<!-- api/songs.md -->

# Песни и чарты

Запрос списка песен, обход узлов и чартов, чтение результатов и сборка дан-курсов (экзаменов).

Соглашения, принятые на этой странице:

- Индексы сложностей начинаются с 0: 0 Easy, 1 Normal, 2 Hard, 3 Extreme (`Oni`), 4 Extra Extreme (`Edit`), 5 Tower, 6 Dan.
- Индексы игроков и файлов сохранений начинаются с 0 (0 — игрок 1).
- Члены, записанные через точку (`node.Title`), — свойства; члены, записанные через двоеточие (`node:GetChart(3)`), — методы.
- Некоторые члены возвращают коллекции C#. Список имеет `.Count` и индексируется от 0 (`list[0]`); массив имеет `.Length` и тоже индексируется от 0. Каждая запись ниже указывает, что именно возвращается.
- Список песен полон только после завершения перечисления песен. Запрашивайте его из обратного вызова `afterSongEnum()` (см. [Модули и жизненный цикл](activities.md)) или сначала проверяйте глобальную функцию `IsSongsEnumDone()`; она возвращает true после завершения перечисления.

## Запрос списка песен

### RequestSongList

Глобальная функция, которая строит навигируемый список песен из объекта настроек.

<div class="callout warn">
Доступна как обычная глобальная функция. Передайте ей объект настроек, созданный GenerateSongListSettings(). Вызов строит дерево песен один раз из песен, которые игра перечислила; дескриптор хранит объект настроек по ссылке, поэтому вы можете изменить поле и вызвать ReloadSongList() у дескриптора, чтобы перестроить дерево.
</div>

| Метод | Описание |
| --- | --- |
| `RequestSongList(settings)  -> song list handle` | Строит и возвращает дескриптор списка песен из указанных настроек списка песен. |

```lua
local settings = GenerateSongListSettings()
settings.AppendMainRandomBox = false
settings:SetExcludedGenreFolders({ "Dan", "Tower" })

local list = RequestSongList(settings)
local node = list:GetSelectedSongNode()
```

### GenerateSongListSettings

Глобальная функция, которая создаёт объект настроек списка песен со значениями по умолчанию.

| Метод | Описание |
| --- | --- |
| `GenerateSongListSettings()  -> song list settings` | Возвращает новый объект настроек списка песен со значениями полей по умолчанию. |

### Настройки списка песен

Объект конфигурации, управляющий тем, какие узлы включает список песен и как ведёт себя навигация.

<div class="callout warn">
Все члены ниже — публичные поля, которые Lua читает и записывает напрямую (settings.HideEmptyFolders = false), кроме двух методов-сеттеров, которые принимают таблицу Lua. ExcludedGenreFolders и MandatoryDifficultyList — массивы C#; задавайте их через методы-сеттеры.
</div>

| Метод | Описание |
| --- | --- |
| `settings.AppendMainRandomBox  (bool, default true)` | Если true, список добавляет случайный бокс в свой корень. |
| `settings.AppendSubRandomBoxes  (bool, default true)` | Если true, список добавляет случайный бокс в конец каждой папки. |
| `settings.SubBackBoxFrequency  (int, default 7)` | Внутри каждой папки список вставляет бокс возврата в начало и после каждых N записей; 0 отключает генерируемые боксы возврата. |
| `settings.ExcludedGenreFolders  (string array)` | Имена папок жанров, которые список не включает. Задавайте его через SetExcludedGenreFolders. |
| `settings.RootGenreFolder  (string, default nil)` | Если задано, корнем списка становится первая папка (в глубину), жанр которой совпадает с этим именем; если nil, корень — верхний уровень. |
| `settings.RootGenreFolderNode  (song node, default nil)` | Форма RootGenreFolder в виде узла. Если задана, имеет приоритет над строкой, что позволяет различать папки с одинаковым именем жанра. |
| `settings.MandatoryDifficultyList  (Difficulty array, default nil)` | Сложности, которые песня должна иметь, чтобы попасть в список; nil означает отсутствие требования. Задавайте его через SetMandatoryDifficultyList. |
| `settings.MandatoryDifficultyMatchAll  (bool, default true)` | true требует все перечисленные сложности (И); false — хотя бы одну (ИЛИ). |
| `settings.HideEmptyFolders  (bool, default true)` | Скрывает папки, не содержащие ни одной видимой песни, рекурсивно. |
| `settings.FlattenOpenedFolders  (bool, default true)` | Если true, текущая страница — это всё дерево, где открытые папки развёрнуты на месте (закрытые папки считаются одиночными записями). Если false, страница содержит только соседей узла под курсором. |
| `settings.ModuloPagination  (bool, default true)` | Если true, GetSongNodeAtOffset циклически переходит по странице; если false, за любым из краёв возвращает nil. |
| `settings.ModuloMovement  (bool, default true)` | Если true, Move циклически переходит по странице; если false, упирается в любой из краёв. |
| `settings.ExcludeHiddenSongs  (bool, default true)` | Исключает песни, у которых HiddenIndex равен 3 (скрытые). |
| `settings.ExcludeLockedSongs  (bool, default false)` | Если true, страницы не включают заблокированные песни, так что навигация никогда на них не попадает. |
| `settings.IgnoreUnlockables  (bool, default false)` | Если true, список игнорирует ExcludeLockedSongs, а GetRandomNodeInFolder может возвращать заблокированные песни. Свойства узлов, такие как IsLocked, продолжают сообщать реальное состояние. |
| `settings:SetExcludedGenreFolders(table)  -> void` | Задаёт ExcludedGenreFolders из таблицы Lua со строками имён жанров. |
| `settings:SetMandatoryDifficultyList(table)  -> void` | Задаёт MandatoryDifficultyList из таблицы Lua с индексами сложностей. |

### Дескриптор списка песен

Навигируемое дерево песен, возвращаемое RequestSongList, с курсором, навигацией по папкам и поиском.

<div class="callout warn">
Методы поиска принимают функцию Lua, которая получает узел песни и возвращает логическое значение. Методы, возвращающие несколько узлов, возвращают список C# (.Count, индексация от 0).
</div>

| Метод | Описание |
| --- | --- |
| `list:ReloadSongList()  -> void` | Перестраивает всё дерево из текущих песен и настроек и переводит курсор на первый узел. |
| `list:GetRoot()  -> song node` | Возвращает корневой узел дерева. |
| `list:GetSelectedSongNode()  -> song node` | Возвращает узел под курсором или nil, если список пуст. |
| `list:GetSongNodeAtOffset(offset)  -> song node` | Возвращает узел с указанным смещением от курсора в пределах текущей страницы, с циклическим переходом или nil согласно ModuloPagination. |
| `list:Move(offset)  -> void` | Перемещает курсор на указанное смещение в пределах текущей страницы, с циклическим переходом или упором в край согласно ModuloMovement. |
| `list:OpenFolder()  -> bool` | Открывает папку под курсором и переводит курсор на её первого потомка; возвращает false, если курсор не стоит на закрытой непустой папке. |
| `list:CloseFolder()  -> bool` | Закрывает папку, содержащую курсор, и переводит курсор на эту папку; возвращает false, если закрывать нечего. Выход из виртуальной папки восстанавливает курсор, сохранённый OpenVirtualFolder. |
| `list:OpenVirtualFolder(baseFolder, songs, title)  -> bool` | Открывает временную папку с именем `title`, содержащую узлы песен из таблицы Lua `songs` (ключи 1..n), с генерируемыми боксами возврата и замыкающим случайным боксом, и переводит в неё курсор. `baseFolder` становится родителем виртуальной папки. Возвращает false, если таблица не содержит узлов песен. |
| `list:GetSongByUniqueId(id)  -> song node` | Возвращает первую песню, уникальный идентификатор которой совпадает, или nil. |
| `list:GetRandomNodeInFolder(node, recursive, predicate)  -> song node` | Выбирает случайную песню среди соседей `node` (страницы, содержащей его). С `recursive` (по умолчанию true) выбор охватывает и песни внутри соседних папок. Заблокированные песни он пропускает, если не задан IgnoreUnlockables. `predicate` необязателен. Возвращает nil, если ничего не подходит. |
| `list:SearchSongsByPredicate(predicate)  -> list of song nodes` | Возвращает каждый узел песни в дереве, для которого предикат возвращает true. |
| `list:SearchFirstSongByPredicate(predicate)  -> song node` | Возвращает первый узел песни, для которого предикат возвращает true, или nil. |
| `list:SearchNodesByPredicate(predicate)  -> list of song nodes` | Как SearchSongsByPredicate, но проверяет также папки и другие узлы, не являющиеся песнями. |

```lua
local results = list:SearchSongsByPredicate(function(node)
    return node:GetChart(3) ~= nil   -- есть чарт Extreme
end)
for i = 0, results.Count - 1 do
    local node = results[i]
end
```

## Узлы, чарты и результаты

### Узел песни

Одна запись в списке песен: песня, папка, бокс возврата или случайный бокс.

<div class="callout warn">
Узлы песен возвращают дескриптор списка песен, SONGMOUNT:ChosenSongNode() и DANBUILDER:GetSong(). Свойства доступны только для чтения. Свойства метаданных возвращают nil для узлов, не являющихся песнями. Перемещайтесь по списку через дескриптор списка песен (Move, OpenFolder, CloseFolder и методы поиска).
</div>

| Метод | Описание |
| --- | --- |
| `node.NotNull  (bool)` | Истина, если узел оборачивает настоящую запись списка песен. |
| `node.IsFolder  (bool)` | Истина, если узел — папка. |
| `node.IsRandom  (bool)` | Истина, если узел — случайный бокс. |
| `node.IsReturn  (bool)` | Истина, если узел — бокс возврата. |
| `node.IsSong  (bool)` | Истина, если узел — играбельная песня. |
| `node.SongCount  (int)` | Число непосредственных дочерних песен. |
| `node.RecursiveSongCount  (int)` | Число песен под этим узлом, включая подпапки. |
| `node.VisibleSongCount  (int)` | Число непосредственных дочерних песен, у которых HiddenIndex не равен 3. |
| `node.RecursiveVisibleSongCount  (int)` | Число видимых песен под этим узлом, включая подпапки. |
| `node.BoxType  (string)` | Строка стиля бокса папки или nil. |
| `node.BgType  (string)` | Строка стиля фона или nil. |
| `node.BoxChara  (string)` | Строка персонажа бокса или nil. |
| `node.ForeColor  (color)` | Цвет переднего плана узла или nil. |
| `node.BackColor  (color)` | Цвет фона узла или nil. |
| `node.BoxColor  (color)` | Цвет бокса узла или nil. |
| `node.Title  (string)` | Отображаемое название. Боксы возврата и случайные боксы возвращают локализованный текст «Return» / «Random», построенный из названия родительской папки. |
| `node.Subtitle  (string)` | Подзаголовок песни или nil. |
| `node.Genre  (string)` | Строка жанра или nil. |
| `node.UniqueId  (string)` | Уникальный идентификатор песни или nil. |
| `node.Maker  (string)` | Поле MAKER одной строкой или nil. |
| `node.Charters  (string array)` | Поле MAKER, разделённое по запятым. |
| `node.Side  (int)` | Значение SIDE: 0 обычная, 1 ex, 2 обе. |
| `node.Explicit  (bool)` | Истина, если песня помечена как explicit; nil для не-песен. |
| `node.HasVideo  (bool)` | Истина, если у песни есть фоновое видео; nil для не-песен. |
| `node.DemoStart  (int)` | Смещение превью BGM в миллисекундах. |
| `node.AudioPath  (string)` | Абсолютный путь к файлу BGM песни или пустая строка. |
| `node.HasPreimage  (bool)` | Истина, если песня объявляет превью-изображение (preimage). |
| `node.PreimagePath  (string)` | Абсолютный путь к превью-изображению. Сначала проверьте HasPreimage; без превью-изображения это лишь папка песни. |
| `node:GetPreimage()  -> texture` | Загружает превью-изображение с диска и возвращает новую текстуру или nil, если у песни его нет. Освободите текстуру, когда закончите с ней работать. |
| `node.ChartMd5  (string)` | MD5 файла чарта (шестнадцатеричный в верхнем регистре) или пустая строка. Остаётся одинаковым между установками; UniqueId — нет. |
| `node:GetChart(diff)  -> chart` | Возвращает чарт для указанного индекса сложности или nil, если у песни нет такого чарта. |
| `node:GetCustomCommand(key)  -> string` | Возвращает значение пользовательской команды глобальной области (заголовок с префиксом-точкой, размещённый до первого COURSE; ключ включает точку, например ".VAULT_NAME") или nil. |
| `node:GetCustomCommands()  -> dictionary` | Возвращает все пользовательские команды глобальной области как объект словаря C#. Для поиска предпочитайте GetCustomCommand. |
| `node.UnlockCondition  (unlock condition)` | Объект условия разблокировки (см. «Условие разблокировки»). |
| `node.UnlockText  (string)` | Пользовательский текст разблокировки, если песня его определяет, иначе сгенерированное сообщение об условии. |
| `node.IsLocked  (bool)` | Истина, если эта песня сейчас заблокирована; всегда false для не-песен. |
| `node.HiddenIndex  (int)` | Состояние отображения в системе разблокировки: 0 показана, 1 затенена, 2 размыта, 3 скрыта (0 для не-песен). |
| `node.Rarity  (string)` | Метка редкости; "Common" для песен без записи разблокировки, "-" для не-песен. |
| `node:Mount(p1diff, p2diff, p3diff, p4diff, p5diff)  -> bool` | Выбирает эту песню для игры с указанным индексом сложности для каждого игрока (по умолчанию 0). Проверяет только первые CONFIG.PlayerCount индексов и возвращает false, если узел не является песней или сложность активного игрока отсутствует или вне диапазона. |
| `node:MountIfNotLocked(p1diff, p2diff, p3diff, p4diff, p5diff)  -> bool` | То же, что Mount, но возвращает false без монтирования, если песня заблокирована. |

### Чарт

Одна сложность песни: уровень, BPM, авторы, данные Tower и Dan, лучшие результаты и пользовательские команды.

<div class="callout warn">
GetChart(diff) узла песни возвращает чарт. Свойства доступны только для чтения. BPM, Life, TotalFloorCount, TowerType и DanTick возвращают nil, если у чарта нет информации о чарте. Difficulty и LevelIcon — объекты перечислений; сравнивайте их через DifficultyAsInt, IsPlus и IsMinus.
</div>

| Метод | Описание |
| --- | --- |
| `chart.NotNull  (bool)` | Истина, если чарт оборачивает настоящие данные чарта. |
| `chart.Parent  (song node)` | Узел песни, которому принадлежит этот чарт. |
| `chart.Difficulty  (enum)` | Сложность как объект перечисления. |
| `chart.DifficultyAsInt  (int)` | Индекс сложности. |
| `chart.Level  (int)` | Уровень в звёздах. |
| `chart.LevelDecimal  (number)` | Уровень с дробной частью (например 12.888) или целый уровень, если дробная часть не задана. |
| `chart.LevelFirstDecimal  (int)` | Первая десятичная цифра LevelDecimal (0-9). |
| `chart.LevelIcon  (enum)` | Значок уровня как объект перечисления. |
| `chart.IsPlus  (bool)` | Истина, если значок уровня — «plus». |
| `chart.IsMinus  (bool)` | Истина, если значок уровня — «minus». |
| `chart.NotesDesigner  (string)` | Поле NOTESDESIGNER одной строкой. |
| `chart.Charters  (string array)` | Поле NOTESDESIGNER, разделённое по запятым. |
| `chart.BPM  (number)` | Основной BPM или nil. |
| `chart.BaseBPM  (number)` | Базовый BPM или nil. |
| `chart.MinBPM  (number)` | Минимальный BPM или nil. |
| `chart.MaxBPM  (number)` | Максимальный BPM или nil. |
| `chart.Life  (int)` | Число жизней в Tower или nil. |
| `chart.TotalFloorCount  (int)` | Число этажей в Tower или nil. |
| `chart.TowerType  (string)` | Строка типа Tower или nil. |
| `chart.DanTick  (int)` | Значение деления таблички дана или nil. |
| `chart.DanTickColor  (color)` | Цвет деления таблички дана (белый, если у чарта нет информации о чарте). |
| `chart.DanSongs  (array of dan songs)` | Песни, составляющие этот Dan-чарт. |
| `chart.DanExams  (array of dan exams)` | Глобальные условия экзамена этого Dan-чарта. |
| `chart:GetSongExam(songIdx, examSlot)  -> dan exam` | Возвращает экзамен для конкретной песни по индексу песни от 1 и слоту экзамена от 1; у результата IsSet = false, если экзамена нет. |
| `chart:GetPlayerBestScore(save)  -> best score info` | Возвращает сводку лучшего прохождения указанного файла сохранения для этого чарта. |
| `chart:GetCustomCommand(key)  -> string` | Возвращает значение пользовательской команды области чарта (заголовок с префиксом-точкой внутри этого блока COURSE; ключ включает точку) или nil. |
| `chart:GetCustomCommands()  -> dictionary` | Возвращает все пользовательские команды области чарта как объект словаря C#. |
| `chart.SongFolder  (string)` | Абсолютный путь папки, содержащей файл чарта. |
| `chart.ChartPath  (string)` | Абсолютный путь к файлу чарта. |
| `chart.UniqueId  (string)` | Уникальный идентификатор песни или пустая строка. |
| `chart:Select(player)  -> bool` | Помечает этот чарт как выбранную сложность для указанного игрока; игрок 0 также задаёт выбранную песню. Возвращает false, если чарт недействителен. |

### Сведения о лучшем результате

Сводка лучшего прохождения одного чарта в файле сохранения.

<div class="callout warn">
Этот объект возвращает chart:GetPlayerBestScore(save). Все члены доступны только для чтения. Недействительный индекс сохранения даёт пустую запись.
</div>

| Метод | Описание |
| --- | --- |
| `info.ScoreRank  (int)` | Лучший достигнутый ранг по очкам. |
| `info.ClearStatus  (int)` | Лучший достигнутый статус клира. |
| `info.HighScore  (int)` | Рекорд по очкам. |
| `info.HasBeenPlayed  (bool)` | Истина, если у чарта есть хотя бы одно записанное прохождение, независимо от результата. |
| `info.PlayCount  (int)` | Общее число прохождений этого чарта со всеми вариантами модов вместе. |

## Дан-экзамены

### Дан-песня

Одна запись песни внутри дан-курса.

<div class="callout warn">
Элементы chart.DanSongs. Все члены доступны только для чтения.
</div>

| Метод | Описание |
| --- | --- |
| `dansong.Title  (string)` | Название песни. |
| `dansong.SubTitle  (string)` | Подзаголовок песни. |
| `dansong.Genre  (string)` | Жанр песни. |
| `dansong.Level  (int)` | Уровень песни в звёздах. |
| `dansong.Difficulty  (enum)` | Сложность как объект перечисления. |
| `dansong.DifficultyAsInt  (int)` | Индекс сложности. |

### Дан-экзамен

Одно условие «сдано/не сдано» дан-курса.

<div class="callout warn">
Элементы chart.DanExams или результат chart:GetSongExam(). Все члены доступны только для чтения. Значения TypeAsInt: 0 шкала, 1 оценки perfect, 2 оценки good, 3 оценки bad, 4 очки, 5 дроби, 6 удары, 7 комбо, 8 точность, 9 оценки ad-lib, 10 оценки мин. Значения RangeAsInt: 0 «не менее», 1 «менее».
</div>

| Метод | Описание |
| --- | --- |
| `danexam.IsSet  (bool)` | Истина, если этот слот экзамена включён. |
| `danexam.RedValue  (int)` | Красный порог (сдача). |
| `danexam.GoldValue  (int)` | Золотой порог. |
| `danexam.TypeAsInt  (int)` | Тип экзамена. |
| `danexam.RangeAsInt  (int)` | Направление сравнения. |

### DANBUILDER

Глобальный объект для сборки дан-курса в памяти из узлов песен, сложностей и условий экзамена с последующим монтированием для игры.

<div class="callout warn">
Доступен как глобальный объект DANBUILDER. Индексы песен и слотов начинаются с 1, кроме сложности, передаваемой в AddSong, которая является индексом сложности от 0. Слоты экзаменов идут от 1 до 7. Строки типов экзамена (без учёта регистра, краткая форма в скобках): "judgeperfect" (jp), "judgegood" (jg), "judgebad" (jb), "score" (s), "roll" (r), "hit" (h), "combo" (c), "accuracy" (a), "judgeadlib" (ja), "judgemine" (jm); любая другая строка означает шкалу. lessThan = true делает экзамен проверкой «менее», false — проверкой «не менее». Сборщик сохраняет состояние между вызовами; вызовите Clear() перед сборкой нового курса.
</div>

| Метод | Описание |
| --- | --- |
| `DANBUILDER.SongCount  (int)` | Число добавленных на данный момент песен. |
| `DANBUILDER:AddSong(node, diff)  -> void` | Добавляет узел песни с указанным индексом сложности от 0. |
| `DANBUILDER:GetSong(i)  -> song node` | Возвращает узел песни по индексу i от 1 или nil. |
| `DANBUILDER:GetSongDiff(i)  -> int` | Возвращает индекс сложности, сохранённый для песни по индексу i от 1, или -1. |
| `DANBUILDER:SetTitle(title)  -> void` | Задаёт название курса (по умолчанию "Dynamic Dan"). |
| `DANBUILDER:SetSubtitle(subtitle)  -> void` | Задаёт подзаголовок курса. |
| `DANBUILDER:SetDanTick(tick)  -> void` | Задаёт значение деления таблички дана (по умолчанию 2). |
| `DANBUILDER:SetDanTickColor(r, g, b)  -> void` | Задаёт цвет деления таблички дана из компонентов 0-255 (по умолчанию белый). |
| `DANBUILDER:SetGlobalExam(slot, type, red, gold, lessThan)  -> void` | Задаёт экзамен на весь курс в указанном слоте. |
| `DANBUILDER:SetPerSongExam(songIndex, slot, type, red, gold, lessThan)  -> void` | Задаёт экзамен, применяемый к одной песне, по индексу песни от 1 и слоту. |
| `DANBUILDER:Clear()  -> void` | Удаляет все песни и экзамены и сбрасывает метаданные к значениям по умолчанию. |
| `DANBUILDER:Mount()  -> bool` | Собирает чарт курса в памяти и выбирает его для игры на сложности Dan для игрока 1; возвращает false, если в сборщике нет песен или сборка не удалась. |

```lua
DANBUILDER:Clear()
DANBUILDER:SetTitle("Custom course")
DANBUILDER:AddSong(list:GetSongByUniqueId(id1), 3)
DANBUILDER:AddSong(list:GetSongByUniqueId(id2), 3)
DANBUILDER:SetGlobalExam(1, "gauge", 90, 100, false)
DANBUILDER:SetPerSongExam(2, 2, "judgebad", 10, 5, true)
if DANBUILDER:Mount() then
    return Exit("play")
end
```

## Разблокировки, виртуальные слоты и значки модов

### Условие разблокировки

Описывает, что разблокирует песню и выполняет ли игрок условие в данный момент.

<div class="callout warn">
Этот объект возвращает node.UnlockCondition. HasCondition — свойство; остальное — методы. Песни без записи разблокировки сообщают HasCondition = false и IsUnlockable = true.
</div>

| Метод | Описание |
| --- | --- |
| `cond.HasCondition  (bool)` | Истина, если у песни есть явное условие разблокировки. |
| `cond:GetConditionMessage()  -> string` | Возвращает понятное человеку описание условия или пустую строку. |
| `cond:GetConditionType()  -> string` | Возвращает идентификатор типа условия (например "ch", "cs", "gt", "gc", "ig") или пустую строку. |
| `cond:GetCoinPrice()  -> int` | Возвращает стоимость в монетах или 0. |
| `cond:IsUnlockable(player)  -> bool` | Возвращает true, если указанный игрок выполняет условие. |
| `cond:GetBlockedMessage(player)  -> string` | Возвращает, почему игрок не выполняет условие, или пустую строку, если он его выполняет. |

### VIRTUALSLOTS

Глобальный объект для чтения и записи пяти виртуальных слотов персонажей (V1-V5) и для перенаправления позиции игрока на отображение визуала слота.

<div class="callout warn">
Доступен как глобальный объект VIRTUALSLOTS. Индексы слотов идут от 1 до 5; сеттеры игнорируют индексы вне диапазона, а геттеры возвращают для них значения по умолчанию. Эти методы ничего не записывают на диск. Слотом ИИ управляет движок, и через этот глобальный объект его редактировать нельзя.
</div>

| Метод | Описание |
| --- | --- |
| `VIRTUALSLOTS:GetCharacter(slot)  -> string` | Возвращает имя папки персонажа слота или "None". |
| `VIRTUALSLOTS:SetCharacter(slot, folderName)  -> void` | Задаёт имя папки персонажа слота. |
| `VIRTUALSLOTS:GetPuchichara(slot)  -> string` | Возвращает имя папки пучичары слота или "None". |
| `VIRTUALSLOTS:SetPuchichara(slot, folderName)  -> void` | Задаёт имя папки пучичары слота. |
| `VIRTUALSLOTS:GetNameplateName(slot)  -> string` | Возвращает имя игрока на табличке слота или "VSlot". |
| `VIRTUALSLOTS:SetNameplateName(slot, name)  -> void` | Задаёт имя игрока на табличке слота. |
| `VIRTUALSLOTS:GetNameplateTitle(slot)  -> string` | Возвращает текст титула на табличке слота. |
| `VIRTUALSLOTS:SetNameplateTitle(slot, title)  -> void` | Задаёт текст титула на табличке слота. |
| `VIRTUALSLOTS:GetNameplateDan(slot)  -> string` | Возвращает текст дана на табличке слота. |
| `VIRTUALSLOTS:SetNameplateDan(slot, dan)  -> void` | Задаёт текст дана на табличке слота. |
| `VIRTUALSLOTS:SetNameplateById(slot, nameplateId)  -> void` | Применяет табличку из базы данных табличек по идентификатору: задаёт текст титула, тип и редкость. Неизвестный идентификатор только записывает идентификатор. |
| `VIRTUALSLOTS:SetNameplateType(slot, type)  -> void` | Задаёт тип титула таблички (индекс стиля) напрямую. |
| `VIRTUALSLOTS:SetNameplateRarity(slot, rarity)  -> void` | Задаёт индекс редкости титула таблички напрямую. |
| `VIRTUALSLOTS:SetNameplateDanType(slot, danType)  -> void` | Задаёт тип таблички дана. |
| `VIRTUALSLOTS:SetNameplateDanGold(slot, gold)  -> void` | Задаёт, выглядит ли табличка дана золотой. |
| `VIRTUALSLOTS:MountSlot(playerSpot, slotInfo)  -> void` | Заставляет позицию игрока 1-5 отображать визуал `slotInfo`: "1P"-"5P" (файл сохранения игрока), "AI" или "V1"-"V5". Переопределение действует до следующего вызова MountSlot для этой позиции. |

### MODICONS

Глобальный объект для отрисовки значков активных модов игрока в позиции экрана.

<div class="callout warn">
Доступен как глобальный объект MODICONS. Отрисовку выполняет ROActivity modicons; первый вызов Draw активирует её.
</div>

| Метод | Описание |
| --- | --- |
| `MODICONS:Draw(player, x, y, alpha)  -> void` | Рисует значки модов указанного игрока в (x, y) с использованием раскладки меню; alpha необязателен (по умолчанию 255). |

## Реплеи и выбранная песня

### REPLAY

Глобальный объект для перечисления сохранённых реплеев чарта и запуска воспроизведения одного из них.

<div class="callout warn">
Доступен как глобальный объект REPLAY. ListReplays возвращает массив C# заголовков реплеев (.Length, индексация от 0). Watch загружает реплей и готовит воспроизведение только для следующей игры; игра применяет моды реплея в памяти и затем восстанавливает прежние моды. songFolder и chartPath берутся из SongFolder и ChartPath чарта.
</div>

| Метод | Описание |
| --- | --- |
| `REPLAY:ListReplays(songFolder, uniqueId, difficulty, topN, chartPath)  -> array of replay headers` | Возвращает до topN реплеев чарта и сложности, отсортированных по очкам. chartPath позволяет списку вычислить ChecksumMismatch. |
| `REPLAY:ListReplays(songFolder, uniqueId, difficulty, topN)  -> array of replay headers` | То же без пути к чарту (список пропускает ChecksumMismatch). |
| `REPLAY:ListReplaysAsync(songFolder, uniqueId, difficulty, topN, chartPath)  -> replay list handle` | Выполняет то же перечисление в фоновом потоке и возвращает дескриптор для опроса. |
| `REPLAY:Watch(filepath, chartPath)  -> bool` | Загружает файл реплея и готовит воспроизведение для следующей игры; возвращает false, если файл не загружается или реплей не воспроизводим. chartPath включает внутриигровые предупреждения «недействительный реплей». |
| `REPLAY:Watch(filepath)  -> bool` | То же без пути к чарту. |
| `REPLAY.MODFLAG  (object)` | Битовые значения для ModFlags: None (0), Mirror (1), Random (2), SuperRandom (4), Invisible (8), PerfectMemory (16), Avalanche (32), Minesweeper (64), Just (128), Safe (256), DynamicBeat (512). |

```lua
local flags = header.ModFlags
local mirrored = (flags & REPLAY.MODFLAG.Mirror) ~= 0
```

### Дескриптор списка реплеев

Дескриптор, возвращаемый REPLAY:ListReplaysAsync.

<div class="callout warn">
Опрашивайте IsDone каждый кадр; как только оно станет true, читайте Result.
</div>

| Метод | Описание |
| --- | --- |
| `handle.IsDone  (bool)` | Истина после завершения фонового перечисления. |
| `handle.Result  (array of replay headers)` | Перечисленные реплеи (пусто, пока IsDone не станет true). |

### Заголовок реплея

Метаданные одного сохранённого реплея.

<div class="callout warn">
Элементы массива, возвращаемого REPLAY:ListReplays или полем Result дескриптора списка реплеев. Все члены доступны только для чтения.
</div>

| Метод | Описание |
| --- | --- |
| `rep.FilePath  (string)` | Абсолютный путь к файлу реплея; передайте его в REPLAY:Watch. |
| `rep.PlayerName  (string)` | Имя игрока, записавшего реплей. |
| `rep.Score  (int)` | Итоговые очки. |
| `rep.ClearStatus  (int)` | Статус клира прохождения. |
| `rep.ScoreRank  (int)` | Ранг по очкам прохождения. |
| `rep.Good  (int)` | Число оценок Good (perfect). |
| `rep.Ok  (int)` | Число оценок Ok. |
| `rep.Bad  (int)` | Число оценок Bad (промах). |
| `rep.Roll  (int)` | Число ударов по дробям. |
| `rep.MaxCombo  (int)` | Максимальное комбо. |
| `rep.Boom  (int)` | Число задетых мин. |
| `rep.ADLib  (int)` | Число попаданий по ad-lib. |
| `rep.ModFlags  (int)` | Битовая маска использованных модов (см. REPLAY.MODFLAG). |
| `rep.ScrollSpeed  (int)` | Настройка скорости прокрутки в прохождении. |
| `rep.SongSpeed  (int)` | Настройка скорости песни в прохождении. |
| `rep.JudgeStrictness  (int)` | Настройка окна тайминга в прохождении. |
| `rep.Date  (string)` | Дата прохождения в формате "yyyy-MM-dd HH:mm". |
| `rep.Timestamp  (int)` | Дата прохождения в виде сырых тиков. |
| `rep.ChartUniqueID  (string)` | Уникальный идентификатор чарта. |
| `rep.ChartDifficulty  (int)` | Индекс сложности записанной игры. |
| `rep.ChartChecksum  (string)` | MD5 чарта, сохранённый вместе с реплеем. |
| `rep.RandomSeed  (int)` | Зерно перемешивания нот или -1, если файл его не хранит. |
| `rep.GameMode  (int)` | Игровой режим записанной игры. |
| `rep.GameVersion  (int)` | Версия игры, записавшая реплей. |
| `rep.Watchable  (bool)` | Истина, если игра может достоверно воспроизвести реплей. |
| `rep.UnwatchableReason  (string)` | Почему реплей нельзя воспроизвести, когда Watchable равно false. |
| `rep.OldVersion  (bool)` | Истина, если реплей записала более старая версия игры. |
| `rep.ChecksumMismatch  (bool)` | Истина, если файл чарта больше не соответствует записи (вычисляется, только когда вы передали путь к чарту). |

### SONGMOUNT

Глобальный объект только для чтения с песней, выбранной для игры в данный момент.

<div class="callout warn">
Доступен как глобальный объект SONGMOUNT. Отражает состояние, заданное Mount() узла песни, Select() чарта или DANBUILDER:Mount().
</div>

| Метод | Описание |
| --- | --- |
| `SONGMOUNT:ChosenUniqueId()  -> string` | Возвращает уникальный идентификатор выбранной песни или пустую строку. |
| `SONGMOUNT:ChosenDifficulty()  -> int` | Возвращает индекс сложности, выбранный для игрока 1. |
| `SONGMOUNT:ChosenSongNode()  -> song node` | Возвращает выбранную песню как узел песни (без потомков) или nil, если ничего не выбрано. |
