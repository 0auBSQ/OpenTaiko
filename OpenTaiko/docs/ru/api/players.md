<!-- api/players.md -->

# Игроки и профили

Файлы сохранений, таблички имени, персонажи, пучичары, состояние игры, темы и текущий язык.

Индексы игроков на этой странице везде начинаются с 0 (от 0 до 4), кроме THEME:GetThemeSettingForPlayer, где они начинаются с 1. Модули только для чтения (ROActivity и фоны) получают дескриптор файла сохранения, чьи методы записи записывают ошибку в журнал и ничего не делают; всё остальное здесь ведёт себя одинаково во всех видах модулей.

## Файлы сохранений

### GetSaveFile

Глобальная функция, возвращающая дескриптор файла сохранения для слота игрока.

<div class="callout warn">
Вызывайте её как обычную функцию (`GetSaveFile(0)`). Индекс вне диапазона записывает ошибку в журнал и возвращает nil. Каждый вызов создаёт новый дескриптор, который читает актуальные данные, поэтому кэшировать или освобождать нечего.
</div>

| Метод | Описание |
| --- | --- |
| `GetSaveFile(player)  -> saveFile` | Возвращает дескриптор файла сохранения для слота игрока с индексом от 0 или nil, если индекс вне диапазона. |

### Дескриптор файла сохранения

Профиль одного игрока: имя, монеты, разблокированные предметы, триггеры и счётчики, статистика клиров, экипированные персонаж, пучичара, табличка имени и титул дана.

<div class="callout warn">
Читайте свойства через точку (sf.Name, sf.Coins). Методы записи сохраняют изменения немедленно. Модули только для чтения блокируют следующее: SpendCoins, EarnCoins, UnlockNameplate, UnlockSong, присваивание SelectedHitsounds, SetGlobalTrigger, SetGlobalCounter, ChangeCharacter (возвращает false), UnlockPuchichara, ChangePuchichara, UnlockCharacter, ChangeDan, ChangeName и ChangeNameplate.
</div>

| Метод | Описание |
| --- | --- |
| `sf.Name  -> string` | Отображаемое имя игрока. |
| `sf.SaveId  -> integer` | Числовой идентификатор этого сохранения в базе данных. |
| `sf.SaveUID  -> string` | Уникальный строковый идентификатор этого сохранения. |
| `sf.NameplateInfo  -> nameplateInfo` | Экипированная табличка имени (см. «Дескриптор сведений о табличке имени») или табличка новичка по умолчанию, если сохранённый идентификатор неизвестен. |
| `sf.DanplateInfo  -> danplateInfo` | Текущий титул дана (см. «Дескриптор сведений о табличке дана»). |
| `sf.TotalPlaycount  -> integer` | Общее число прохождений в этом сохранении. |
| `sf.AIBattlePlaycount  -> integer` | Число прохождений в битвах с ИИ. |
| `sf.AIBattleWins  -> integer` | Число побед в битвах с ИИ. |
| `sf.Coins  -> integer` | Текущий баланс монет. |
| `sf.TotalEarnedCoins  -> integer` | Общее число монет, заработанных за всё время существования сохранения. |
| `sf:SpendCoins(price)  -> nil` | Списывает монеты (баланс никогда не опускается ниже 0) и сохраняет. |
| `sf:EarnCoins(amount)  -> nil` | Добавляет монеты к балансу и к общей заработанной сумме и сохраняет. |
| `sf:IsNameplateUnlocked(id)  -> bool` | Разблокирована ли табличка имени с этим идентификатором. |
| `sf:UnlockNameplate(id)  -> nil` | Разблокирует табличку имени и сохраняет (пустая операция, если уже разблокирована). |
| `sf:IsSongUnlocked(uniqueId)  -> bool` | Разблокирована ли песня с этим уникальным идентификатором. |
| `sf:UnlockSong(uniqueId)  -> nil` | Разблокирует песню и сохраняет (пустая операция, если уже разблокирована). |
| `sf.SelectedHitsounds  -> string` | Имя папки выбранного набора хитсаундов. Присваивание другого имени сохраняет его и перезагружает хитсаунды игрока. |
| `sf:GetGlobalTrigger(name)  -> bool` | Читает именованный логический триггер. |
| `sf:GetGlobalCounter(name)  -> number` | Читает именованный числовой счётчик. |
| `sf:SetGlobalTrigger(name, value)  -> nil` | Задаёт именованный логический триггер. |
| `sf:SetGlobalCounter(name, value)  -> nil` | Задаёт именованный числовой счётчик. |
| `sf:GetClearStatusCount(difficulty, clearStatus)  -> integer` | Число чартов сложности (от 0 Easy до 4 Extra Extreme), лучший статус клира которых равен ровно clearStatus (0 нет, 1 клир с помощью, 2 клир, 3 фулл-комбо, 4 перфект). 0 для аргументов вне диапазона. |
| `sf:GetDanBestPlay(node)  -> danBestPlay` | Лучшее прохождение без модов для узла дан-песни (см. «Дескриптор лучшего дан-прохождения»); дескриптор с HasRecord false, если записи нет. |
| `sf:GetCharacter()  -> character` | Дескриптор персонажа, привязанного к игроку этого слота (см. «Дескриптор персонажа»). |
| `sf.CharacterName  -> string` | Имя папки экипированного персонажа. |
| `sf:ChangeCharacter(folderName)  -> bool` | Экипирует персонажа с этим именем папки. Возвращает true, если персонаж теперь экипирован или уже был активен, и false, если ни у одного загруженного персонажа нет такого имени папки. |
| `sf:GetPuchichara()  -> puchichara` | Экипированная пучичара (см. «Дескриптор пучичары») или nil, если её не удаётся разрешить. |
| `sf:IsPuchicharaUnlocked(folderName)  -> bool` | Разблокирована ли пучичара с этим именем папки. |
| `sf:UnlockPuchichara(folderName)  -> nil` | Разблокирует пучичару и сохраняет (пустая операция, если уже разблокирована). |
| `sf:ChangePuchichara(folderName)  -> nil` | Экипирует пучичару с этим именем папки и сохраняет. Метод не проверяет имя. |
| `sf:IsCharacterUnlocked(folderName)  -> bool` | Разблокирован ли персонаж. Экипированный персонаж всегда считается разблокированным. |
| `sf:UnlockCharacter(folderName)  -> nil` | Разблокирует персонажа и сохраняет (пустая операция, если уже разблокирован). |
| `sf.DanTitleCount  -> integer` | Число доступных титулов дана, включая титул по умолчанию (всегда не меньше 1). |
| `sf:GetDanTitleByIndex(index)  -> danTitleEntry` | Титул дана по индексу от 0 (см. «Дескриптор записи титула дана»). Индекс 0 — титул по умолчанию; nil, если индекс вне диапазона. |
| `sf.SelectedDan  -> string` | Текст активного титула дана. |
| `sf:ChangeDan(title)  -> nil` | Делает указанный титул активным, копируя его флаги золота и статуса клира, если игрок его заработал, обновляет табличку имени и сохраняет. |
| `sf:ChangeName(name)  -> nil` | Меняет отображаемое имя, обновляет табличку имени и сохраняет. Пустые или неизменённые имена метод игнорирует. |
| `sf:ChangeNameplate(id)  -> nil` | Экипирует табличку имени с этим идентификатором, обновляет табличку и сохраняет. Идентификатор, отсутствующий в базе данных, очищает кэшированный текст титула. |

```lua
local save = GetSaveFile(0)
local entry = CHARACTERLIST:GetByName("Aoi")
if entry and not save:IsCharacterUnlocked(entry.FolderName) then
    local cond = entry.UnlockCondition
    if cond:IsUnlockable(0) and cond:GetCoinPrice() <= save.Coins then
        save:SpendCoins(cond:GetCoinPrice())
        save:UnlockCharacter(entry.FolderName)
    end
end
```

## Таблички имени и титулы дана

### NAMEPLATE

Рисует таблички титула, таблички дана и полные таблички имени игроков.

<div class="callout warn">
Отрисовку выполняет ROActivity nameplate скина (Modules/ROActivities/nameplate), которая определяет графику и раскладку. Непрозрачность от 0 до 255. Текстовые параметры принимают текстуру, отрисованную текстовым объектом (см. «Графика и текст»); rarity — индекс: 0 Poor, 1 Common, 2 Uncommon, 3 Rare, 4 Epic, 5 Legendary, 6 Mythical.
</div>

| Метод | Описание |
| --- | --- |
| `NAMEPLATE:DrawTitlePlate(x, y, opacity, type, text, rarity, nameplateId)  -> nil` | Рисует табличку титула с указанным типом отображения, заранее отрисованной текстурой титула, индексом редкости и идентификатором таблички. |
| `NAMEPLATE:DrawDanPlate(x, y, opacity, danGrade, text)  -> nil` | Рисует табличку дана для указанного ранга с использованием заранее отрисованной текстуры титула. |
| `NAMEPLATE:DrawPlayerNameplate(x, y, opacity, player)  -> nil` | Рисует полную табличку имени слота игрока; красная или синяя сторона определяется настройкой стороны 1P в игре. |
| `NAMEPLATE:DrawNameplateTitleById(id, x, y, opacity, font)  -> nil` | Рендерит локализованный титул таблички с этим идентификатором с помощью текстового объекта и рисует его как табличку титула. Идентификатор должен существовать в базе данных табличек. |

### NAMEPLATESLIST

База данных всех известных игре табличек имени с поиском по индексу или идентификатору и фильтрацией.

<div class="callout warn">
Методы запроса возвращают дескрипторы сведений о табличке имени. FindWhere вызывает функцию Lua по одному разу на каждую табличку и оставляет записи, для которых она возвращает true.
</div>

| Метод | Описание |
| --- | --- |
| `NAMEPLATESLIST.Count  -> integer` | Число табличек в базе данных. |
| `NAMEPLATESLIST:GetByIndex(index)  -> nameplateInfo` | Табличка по позиции в базе данных от 0 или nil, если индекс вне диапазона. |
| `NAMEPLATESLIST:GetById(id)  -> nameplateInfo` | Табличка с этим идентификатором или nil, если не найдена. |
| `NAMEPLATESLIST:GetAll()  -> nameplateInfo[]` | Все таблички в виде списка. |
| `NAMEPLATESLIST:FindWhere(predicate)  -> nameplateInfo[]` | Таблички, для которых `predicate(info)` возвращает true. |

### Дескриптор сведений о табличке имени

Один титул таблички имени: локализованный текст, тип отображения, идентификатор, редкость и условие разблокировки.

<div class="callout warn">
Эти дескрипторы возвращают sf.NameplateInfo и NAMEPLATESLIST. Табличка новичка по умолчанию имеет идентификатор -1, редкость "Common" и не имеет условия разблокировки.
</div>

| Метод | Описание |
| --- | --- |
| `info.Title  -> string` | Текст титула на текущем языке. |
| `info.Type  -> integer` | Код типа отображения, передаваемый в NAMEPLATE:DrawTitlePlate. |
| `info.Id  -> integer` | Идентификатор таблички (-1 для таблички новичка по умолчанию). |
| `info.Rarity  -> string` | Имя редкости: "Poor", "Common", "Uncommon", "Rare", "Epic", "Legendary" или "Mythical". |
| `info.UnlockCondition  -> unlockCondition` | Условие разблокировки (см. «Дескриптор условия разблокировки»). |

### Дескриптор сведений о табличке дана

Активный титул дана игрока, как он показан на табличке имени.

<div class="callout warn">
Этот дескриптор возвращает sf.DanplateInfo. Значения отражают файл сохранения на момент чтения.
</div>

| Метод | Описание |
| --- | --- |
| `info.Title  -> string` | Текст активного титула дана. |
| `info.Gold  -> bool` | Заработал ли игрок активный титул золотой сдачей. |
| `info.ClearStatus  -> integer` | Код статуса клира активного титула. |

### Дескриптор записи титула дана

Один титул дана, который игрок может выбрать.

<div class="callout warn">
Эти записи возвращает sf:GetDanTitleByIndex. Индекс 0 — титул по умолчанию (не золотой, статус клира 0); последующие индексы — титулы, заработанные игроком.
</div>

| Метод | Описание |
| --- | --- |
| `entry.Title  -> string` | Текст титула. |
| `entry.IsGold  -> bool` | Заработал ли игрок титул золотой сдачей. |
| `entry.ClearStatus  -> integer` | Лучший статус клира, записанный для титула. |

### Дескриптор лучшего дан-прохождения

Лучшие результаты экзаменов одной записи дана.

<div class="callout warn">
Этот дескриптор возвращает sf:GetDanBestPlay. Проверьте HasRecord перед чтением экзаменов. GetExam возвращает массив .NET: индексируйте от 0 и читайте `.Length`.
</div>

| Метод | Описание |
| --- | --- |
| `play.HasRecord  -> bool` | Существует ли запись для песни. |
| `play:GetExam(slot)  -> int[]` | Лучшие результаты для слота экзамена от 1 до 7: одно значение для экзамена на весь курс, по одному на песню для экзаменов по песням. Пусто для отсутствующей записи или недействительного слота. |

## Персонажи и пучичары

### CHARACTER

Создаёт дескрипторы персонажей и предоставляет имена стандартных слотов анимаций и голосов.

<div class="callout warn">
CreateCharacter возвращает дескриптор, владеющий своими ресурсами; проверяйте IsValid и вызывайте Dispose по окончании работы. GetPlayerCharacter возвращает дескриптор, следующий за экипированным персонажем игрока и не требующий освобождения. GetPlayerGradientMap возвращает карту градиента (см. «Графика и текст»). Члены ANIM_* и VOICE_* — строки только для чтения; передавайте их в методы анимации и голоса дескриптора персонажа.
</div>

| Метод | Описание |
| --- | --- |
| `CHARACTER:CreateCharacter(folderName)  -> character` | Загружает отдельного персонажа из Global/Characters/{folderName}. IsValid равно false, если папка не существует. |
| `CHARACTER:GetPlayerCharacter(player)  -> character` | Дескриптор, привязанный к слоту игрока, который при каждом вызове разрешает экипированного персонажа. |
| `CHARACTER:GetPlayerGradientMap(player)  -> gradientMap` | Градиент палитры, активный для слота игрока, или nil, если он не задан. |
| `CHARACTER.ANIM_PREVIEW  -> string` | Поза превью (меню и магазины). |
| `CHARACTER.ANIM_RENDER  -> string` | Поза полного рендера. |
| `CHARACTER.ANIM_GAME_NORMAL  -> string` | Игра, обычное состояние. |
| `CHARACTER.ANIM_GAME_CLEAR  -> string` | Игра, шкала в зоне клира. |
| `CHARACTER.ANIM_GAME_MAX  -> string` | Игра, полная шкала. |
| `CHARACTER.ANIM_GAME_GOGO  -> string` | Игра, гоу-гоу-тайм. |
| `CHARACTER.ANIM_GAME_GOGO_MAX  -> string` | Игра, гоу-гоу-тайм с полной шкалой. |
| `CHARACTER.ANIM_GAME_MISS  -> string` | Игра, промах. |
| `CHARACTER.ANIM_GAME_MISS_DOWN  -> string` | Игра, промах с низкой шкалой. |
| `CHARACTER.ANIM_GAME_10COMBO  -> string` | Игра, рубеж в 10 комбо. |
| `CHARACTER.ANIM_GAME_10COMBO_MAX  -> string` | Игра, рубеж в 10 комбо с полной шкалой. |
| `CHARACTER.ANIM_GAME_CLEARED  -> string` | Игра, песня пройдена. |
| `CHARACTER.ANIM_GAME_FAILED  -> string` | Игра, песня провалена. |
| `CHARACTER.ANIM_GAME_CLEAR_OUT  -> string` | Переход из состояния клира. |
| `CHARACTER.ANIM_GAME_CLEAR_IN  -> string` | Переход в состояние клира. |
| `CHARACTER.ANIM_GAME_MAX_OUT  -> string` | Переход из состояния полной шкалы. |
| `CHARACTER.ANIM_GAME_MAX_IN  -> string` | Переход в состояние полной шкалы. |
| `CHARACTER.ANIM_GAME_MISS_IN  -> string` | Переход в промах. |
| `CHARACTER.ANIM_GAME_MISS_DOWN_IN  -> string` | Переход в промах с низкой шкалой. |
| `CHARACTER.ANIM_GAME_RETURN  -> string` | Возврат в обычное состояние. |
| `CHARACTER.ANIM_GAME_GOGOSTART  -> string` | Вспышка начала гоу-гоу. |
| `CHARACTER.ANIM_GAME_GOGOSTART_CLEAR  -> string` | Вспышка начала гоу-гоу в состоянии клира. |
| `CHARACTER.ANIM_GAME_GOGOSTART_MAX  -> string` | Вспышка начала гоу-гоу с полной шкалой. |
| `CHARACTER.ANIM_GAME_BALLOON_BREAKING  -> string` | Удары по шару. |
| `CHARACTER.ANIM_GAME_BALLOON_BROKE  -> string` | Шар лопнул. |
| `CHARACTER.ANIM_GAME_BALLOON_MISS  -> string` | Шар пропущен. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_BREAKING  -> string` | Удары по кусудаме. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_BROKE  -> string` | Кусудама разбита. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_MISS  -> string` | Кусудама пропущена. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_IDLE  -> string` | Кусудама, ожидание. |
| `CHARACTER.ANIM_GAME_TOWER_STANDING  -> string` | Режим башни, стоит. |
| `CHARACTER.ANIM_GAME_TOWER_STANDING_TIRED  -> string` | Режим башни, стоит уставшим. |
| `CHARACTER.ANIM_GAME_TOWER_CLIMBING  -> string` | Режим башни, взбирается. |
| `CHARACTER.ANIM_GAME_TOWER_CLIMBING_TIRED  -> string` | Режим башни, взбирается уставшим. |
| `CHARACTER.ANIM_GAME_TOWER_RUNNING  -> string` | Режим башни, бежит. |
| `CHARACTER.ANIM_GAME_TOWER_RUNNING_TIRED  -> string` | Режим башни, бежит уставшим. |
| `CHARACTER.ANIM_GAME_TOWER_CLEAR  -> string` | Режим башни, клир. |
| `CHARACTER.ANIM_GAME_TOWER_CLEAR_TIRED  -> string` | Режим башни, клир уставшим. |
| `CHARACTER.ANIM_GAME_TOWER_FAIL  -> string` | Режим башни, провал. |
| `CHARACTER.ANIM_MENU_WAIT  -> string` | Меню, ожидание. |
| `CHARACTER.ANIM_MENU_START  -> string` | Меню, старт. |
| `CHARACTER.ANIM_MENU_NORMAL  -> string` | Меню, обычное состояние. |
| `CHARACTER.ANIM_MENU_SELECT  -> string` | Меню, выбор. |
| `CHARACTER.ANIM_ENTRY_NORMAL  -> string` | Экран входа, обычное состояние. |
| `CHARACTER.ANIM_ENTRY_JUMP  -> string` | Экран входа, прыжок. |
| `CHARACTER.ANIM_RESULT_NORMAL  -> string` | Результаты, обычное состояние. |
| `CHARACTER.ANIM_RESULT_CLEAR  -> string` | Результаты, клир. |
| `CHARACTER.ANIM_RESULT_FAILED_IN  -> string` | Результаты, вход в состояние провала. |
| `CHARACTER.ANIM_RESULT_FAILED  -> string` | Результаты, провал. |
| `CHARACTER.VOICE_END_FAILED  -> string` | Конец песни, провал. |
| `CHARACTER.VOICE_END_CLEAR  -> string` | Конец песни, клир. |
| `CHARACTER.VOICE_END_FULLCOMBO  -> string` | Конец песни, фулл-комбо. |
| `CHARACTER.VOICE_END_ALLPERFECT  -> string` | Конец песни, олл-перфект. |
| `CHARACTER.VOICE_END_AIBATTLE_WIN  -> string` | Конец песни, битва с ИИ выиграна. |
| `CHARACTER.VOICE_END_AIBATTLE_LOSE  -> string` | Конец песни, битва с ИИ проиграна. |
| `CHARACTER.VOICE_MENU_SONGSELECT  -> string` | Вход в выбор песни. |
| `CHARACTER.VOICE_MENU_SONGDECIDE  -> string` | Песня подтверждена. |
| `CHARACTER.VOICE_MENU_SONGDECIDE_AI  -> string` | Песня подтверждена в битве с ИИ. |
| `CHARACTER.VOICE_MENU_DIFFSELECT  -> string` | Выбор сложности. |
| `CHARACTER.VOICE_MENU_DANSELECTSTART  -> string` | Вход в выбор дана. |
| `CHARACTER.VOICE_MENU_DANSELECTPROMPT  -> string` | Подсказка в выборе дана. |
| `CHARACTER.VOICE_MENU_DANSELECTCONFIRM  -> string` | Дан-курс подтверждён. |
| `CHARACTER.VOICE_TITLE_SANKA  -> string` | Вход на титульном экране. |
| `CHARACTER.VOICE_TOWER_MISS  -> string` | Промах в режиме башни. |
| `CHARACTER.VOICE_RESULT_BESTSCORE  -> string` | Результаты, новый рекорд. |
| `CHARACTER.VOICE_RESULT_CLEARFAILED  -> string` | Результаты, провал. |
| `CHARACTER.VOICE_RESULT_CLEARSUCCESS  -> string` | Результаты, клир. |
| `CHARACTER.VOICE_RESULT_DANFAILED  -> string` | Результаты, дан не сдан. |
| `CHARACTER.VOICE_RESULT_DANREDPASS  -> string` | Результаты, дан сдан. |
| `CHARACTER.VOICE_RESULT_DANGOLDPASS  -> string` | Результаты, дан сдан на золото. |

### Дескриптор персонажа

Рисуемый персонаж: проигрывает именованные анимации и голоса и несёт собственное состояние отрисовки (непрозрачность, масштаб, оттенок, поворот, режимы смешивания и адресации, градиент палитры).

<div class="callout warn">
Дескрипторы персонажей возвращают CHARACTER:GetPlayerCharacter, CHARACTER:CreateCharacter, sf:GetCharacter и свойство Character записи списка персонажей. Только дескрипторы из CreateCharacter владеют своими ресурсами и требуют Dispose. Дескриптор сохраняет значения Set* и применяет их при каждой последующей отрисовке; аргументы масштаба и непрозрачности методов отрисовки умножаются на сохранённые значения. Сохранённая непрозрачность — от 0.0 до 1.0, непрозрачность при отрисовке — от 0 до 255. Имена анимаций и голосов — константы CHARACTER.
</div>

| Метод | Описание |
| --- | --- |
| `char.IsValid  -> bool` | Разрешается ли дескриптор в загруженного персонажа. |
| `char.FolderName  -> string` | Имя папки или пустая строка, если недействителен. |
| `char.FullPath  -> string` | Абсолютный путь папки или пустая строка, если недействителен. |
| `char.DisplayName  -> string` | Локализованное отображаемое имя с откатом к имени папки. |
| `char:SetPaletteGradient(stops, blend?)  -> nil` | Применяет градиент палитры, построенный из таблицы как минимум из двух опорных цветов, с необязательной степенью смешивания (по умолчанию 1.0). Дескрипторы, привязанные к игроку, также сохраняют градиент в слоте игрока. Передача nil очищает его. |
| `char:ClearPaletteGradient()  -> nil` | Убирает градиент палитры (и градиент слота игрока для дескрипторов, привязанных к игроку). |
| `char:SetOpacity(opacity)  -> nil` | Сохранённая непрозрачность, от 0.0 прозрачно до 1.0 непрозрачно. |
| `char:SetScale(scaleX, scaleY)  -> nil` | Сохранённый масштаб; отрицательный X зеркалит по горизонтали. |
| `char:SetColor(color)  -> nil` | Сохранённый оттенок из значения цвета. |
| `char:SetColor(r, g, b)  -> nil` | Сохранённый оттенок из трёх каналов от 0.0 до 1.0. |
| `char:SetRotation(degrees)  -> nil` | Сохранённый поворот в градусах. |
| `char:SetBlendMode(mode)  -> nil` | Сохранённый режим смешивания: "normal", "add", "multi", "sub" или "screen". |
| `char:SetWrapMode(mode)  -> nil` | Сохранённый режим адресации текстуры: "edge", "border", "repeat" или "mirror". |
| `char:GetScale()  -> vector2` | Сохранённый масштаб. |
| `char:GetColor()  -> tuple` | Сохранённый оттенок в виде кортежа .NET с полями Item1, Item2 и Item3 (красный, зелёный, синий). |
| `char:GetRotation()  -> number` | Сохранённый поворот в градусах. |
| `char:GetBlendMode()  -> string` | Сохранённый режим смешивания. |
| `char:GetWrapMode()  -> string` | Сохранённый режим адресации. |
| `char:Draw(x, y, animation, scaleX?, scaleY?, opacity?)  -> nil` | Рисует анимацию в x, y. По умолчанию: масштаб 1, непрозрачность 255. |
| `char:DrawAtAnchor(x, y, animation, anchor?, scaleX?, scaleY?, opacity?)  -> nil` | Рисует анимацию так, чтобы именованная точка якоря (по умолчанию "bottom") попала в x, y. |
| `char:DrawRect(x, y, w, h, animation, scaleX?, scaleY?, opacity?)  -> nil` | Рисует анимацию в левом верхнем углу прямоугольника. Метод принимает w и h для кода раскладки, но на отрисовку они не влияют. |
| `char:DrawRectAtAnchor(x, y, clipW, clipH, animation, opacity?, clipX?, clipY?)  -> nil` | Рисует анимацию с левым верхним углом в x, y, отсекая по прямоугольнику clipW на clipH со смещением clipX, clipY. Масштаб, оттенок и поворот берутся только из сохранённого состояния. |
| `char:Update(animation, looping?)  -> bool` | Продвигает анимацию (по умолчанию зациклена) и возвращает, проигрывается ли она ещё. |
| `char:LoadAnimation(animation)  -> nil` | Загружает кадры анимации. |
| `char:DisposeAnimation(animation)  -> nil` | Освобождает кадры анимации. |
| `char:AvailableAnimation(animation)  -> bool` | Предоставляет ли персонаж эту анимацию. |
| `char:SetAnimationDuration(animation, duration)  -> nil` | Задаёт длительность воспроизведения анимации. |
| `char:SetAnimationCyclesFromBPM(animation, bpm)  -> nil` | Задаёт длину цикла анимации из BPM. |
| `char:ResetAnimationCounter(animation)  -> nil` | Перезапускает анимацию с первого кадра. |
| `char:GetAnimationSize(animation)  -> vector2` | Отрисованный размер текущего кадра анимации в разрешении скина или (0, 0), если недоступен. |
| `char:LoadVoice(voice)  -> nil` | Загружает голосовую реплику. |
| `char:DisposeVoice(voice)  -> nil` | Освобождает голосовую реплику. |
| `char:PlayVoice(voice)  -> nil` | Проигрывает голосовую реплику. |
| `char:Dispose()  -> nil` | Освобождает ресурсы персонажа (только для дескрипторов из CreateCharacter). |

```lua
local chara = CHARACTER:GetPlayerCharacter(0)
chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL)

function update()
    chara:Update(CHARACTER.ANIM_MENU_NORMAL)
end

function draw()
    chara:DrawAtAnchor(960, 1000, CHARACTER.ANIM_MENU_NORMAL, "bottom")
end
```

### CHARACTERLIST

Список всех загруженных персонажей.

<div class="callout warn">
Скин перестраивает список, когда загружает своих персонажей, и освобождает его при перезагрузке скина, поэтому глобальный объект может быть nil, пока персонажи не загружены. Методы запроса возвращают записи списка персонажей.
</div>

| Метод | Описание |
| --- | --- |
| `CHARACTERLIST.Count  -> integer` | Число загруженных персонажей. |
| `CHARACTERLIST:GetAll()  -> characterEntry[]` | Все персонажи в виде списка. |
| `CHARACTERLIST:GetByIndex(index)  -> characterEntry` | Запись по индексу от 0 или nil, если индекс вне диапазона. |
| `CHARACTERLIST:GetByName(folderName)  -> characterEntry` | Запись с этим именем папки или nil, если не найдена. |

### Запись списка персонажей

Одна запись CHARACTERLIST: имя папки, отображаемое имя, редкость, дескриптор персонажа и условие разблокировки.

<div class="callout warn">
Общим дескриптором в свойстве Character владеет список; не освобождайте его. Загружайте на нём анимации перед отрисовкой.
</div>

| Метод | Описание |
| --- | --- |
| `entry.FolderName  -> string` | Имя папки; файлы сохранений используют его как ключ. |
| `entry.DisplayName  -> string` | Локализованное отображаемое имя. |
| `entry.Rarity  -> string` | Имя редкости (список см. в «Дескриптор сведений о табличке имени»). |
| `entry.Character  -> character` | Дескриптор персонажа для этой записи. |
| `entry.UnlockCondition  -> unlockCondition` | Условие разблокировки (см. «Дескриптор условия разблокировки»). |

### PUCHICHARALIST

Список всех загруженных пучичар, а также текущий выбор каждого игрока.

<div class="callout warn">
Скин перестраивает список, когда загружает текстуры пучичар, и освобождает его при перезагрузке скина, поэтому глобальный объект может быть nil, пока они не загружены. Методы запроса возвращают дескрипторы пучичар.
</div>

| Метод | Описание |
| --- | --- |
| `PUCHICHARALIST.Count  -> integer` | Число загруженных пучичар. |
| `PUCHICHARALIST:GetAll()  -> puchichara[]` | Все пучичары в виде списка. |
| `PUCHICHARALIST:GetByIndex(index)  -> puchichara` | Пучичара по индексу от 0 или nil, если индекс вне диапазона. |
| `PUCHICHARALIST:GetByName(folderName)  -> puchichara` | Пучичара с этим именем папки или nil, если не найдена. |
| `PUCHICHARALIST:GetPlayerPuchichara(player)  -> puchichara` | Пучичара, экипированная слотом игрока, или nil, если её не удаётся разрешить. |

### Дескриптор пучичары

Одна пучичара: её текстуры, локализованные имя и автор, редкость, имя папки и условие разблокировки.

<div class="callout warn">
Эти дескрипторы возвращают PUCHICHARALIST и sf:GetPuchichara. Текстурами владеет список; не освобождайте их. Отсутствующее изображение даёт пустую текстуру.
</div>

| Метод | Описание |
| --- | --- |
| `puchi.tx  -> texture` | Спрайт-лист, загруженный из Chara.png. |
| `puchi.render  -> texture` | Полный рендер, загруженный из Render.png. |
| `puchi.Name  -> string` | Локализованное отображаемое имя. |
| `puchi.Author  -> string` | Локализованное имя автора. |
| `puchi.Rarity  -> string` | Имя редкости (список см. в «Дескриптор сведений о табличке имени»). |
| `puchi.FolderName  -> string` | Имя папки; файлы сохранений используют его как ключ. |
| `puchi.UnlockCondition  -> unlockCondition` | Условие разблокировки (см. «Дескриптор условия разблокировки»). |
| `puchi:GetUnlockMessage()  -> string` | Сокращение для `puchi.UnlockCondition:GetConditionMessage()`. |

## Состояние игры и разблокировки

### PLAYSTATE

Актуальные результаты текущего или последнего прохождения: число оценок, очки, комбо, проверки клира, состояние башни и дана.

<div class="callout warn">
Значения берутся с игрового экрана, поэтому они осмысленны во время прохождения и на следующих за ним экранах. Индексы игроков начинаются с 0; методы не проверяют их на диапазон. Проверки дана всегда оценивают игрока 0.
</div>

| Метод | Описание |
| --- | --- |
| `PLAYSTATE.LastRegisteredFloor  -> integer` | Режим башни: последний достигнутый этаж. |
| `PLAYSTATE.MaxNumberOfLives  -> integer` | Режим башни: максимальное число жизней. |
| `PLAYSTATE.CurrentNumberOfLives  -> integer` | Режим башни: текущее число жизней. |
| `PLAYSTATE.InvincibilityDurationSpeedDependent  -> number` | Режим башни: длительность неуязвимости с поправкой на скорость песни. |
| `PLAYSTATE.InvincibilityDuration  -> integer` | Режим башни: базовая длительность неуязвимости. |
| `PLAYSTATE:WasPlayEndedNormally()  -> bool` | Дошло ли предыдущее прохождение до конца. |
| `PLAYSTATE:WasPlayAborted()  -> bool` | Прервал ли игрок предыдущее прохождение досрочно. |
| `PLAYSTATE:GetGoodCount(player)  -> integer` | Число оценок Good. |
| `PLAYSTATE:GetOkCount(player)  -> integer` | Число оценок Ok. |
| `PLAYSTATE:GetBadCount(player)  -> integer` | Число оценок Bad. |
| `PLAYSTATE:GetRollCount(player)  -> integer` | Число ударов по дробям. |
| `PLAYSTATE:GetADLibCount(player)  -> integer` | Число попаданий по нотам ADLib. |
| `PLAYSTATE:GetMissedADLibCount(player)  -> integer` | Число пропущенных нот ADLib. |
| `PLAYSTATE:GetBoomCount(player)  -> integer` | Число задетых мин. |
| `PLAYSTATE:GetAvoidedBoomCount(player)  -> integer` | Число избегнутых мин. |
| `PLAYSTATE:GetScore(player)  -> integer` | Текущие очки. |
| `PLAYSTATE:GetCombo(player)  -> integer` | Текущее комбо. |
| `PLAYSTATE:GetHighestCombo(player)  -> integer` | Наибольшее достигнутое комбо. |
| `PLAYSTATE:IsClear(player)  -> bool` | Достигает ли шкала линии клира. |
| `PLAYSTATE:IsAssistedClear(player)  -> bool` | Является ли прохождение клиром при активном моде, снижающем очки. |
| `PLAYSTATE:IsFullCombo(player)  -> bool` | Клир без помощи, без оценок Bad и без задетых мин. |
| `PLAYSTATE:IsPerfect(player)  -> bool` | Фулл-комбо без оценок Ok. |
| `PLAYSTATE:IsAlive()  -> bool` | Режим башни: остались ли жизни. |
| `PLAYSTATE:IsPass()  -> bool` | Режим дана: не является ли статус экзамена провалом. |
| `PLAYSTATE:IsRedPass()  -> bool` | Режим дана: является ли статус экзамена обычной сдачей. |
| `PLAYSTATE:IsGoldPass()  -> bool` | Режим дана: является ли статус экзамена золотой сдачей. |
| `PLAYSTATE:IsDanClear()  -> bool` | Режим дана: сдано и без помощи. |
| `PLAYSTATE:IsDanFullCombo()  -> bool` | Режим дана: дан-клир без оценок Bad и без задетых мин. |
| `PLAYSTATE:IsDanPerfect()  -> bool` | Режим дана: дан-фулл-комбо без оценок Ok. |

### Дескриптор условия разблокировки

Требование разблокировки таблички имени, персонажа или пучичары.

<div class="callout warn">
Этот дескриптор возвращает свойство UnlockCondition дескрипторов сведений о табличке имени, записей списка персонажей и дескрипторов пучичар. Предмет без условия (HasCondition false) доступен по умолчанию: IsUnlockable возвращает true, а сообщения пусты. Словарь условий совпадает с Unlock.json и разблокировкой чартов; см. руководство <a href="../guides/unlockables.md">Разблокировка чартов</a>.
</div>

| Метод | Описание |
| --- | --- |
| `cond.HasCondition  -> bool` | Есть ли у предмета условие разблокировки. |
| `cond:GetConditionType()  -> string` | Идентификатор типа условия (например "ch", "cs", "gt", "gc" или "ig") или пустая строка. |
| `cond:GetCoinPrice()  -> integer` | Цена условия в монетах или 0. |
| `cond:GetConditionMessage()  -> string` | Локализованное описание условия. |
| `cond:IsUnlockable(player)  -> bool` | Выполняет ли игрок условие в данный момент. |
| `cond:GetBlockedMessage(player)  -> string` | Почему игрок не выполняет условие, или пустая строка, если оно выполнено. |

## Тема и язык

### THEME

Разрешение скина, настройки темы, локализованные строки в области скина и определения настроек темы.

<div class="callout warn">
Скин объявляет настройки темы в ThemeSettings.json и хранит их значения в ThemeSettings.db3 рядом с ним. Геттеры всегда возвращают значения настроек строками; отсутствующая настройка возвращает объявленное значение по умолчанию или пустую строку, если объявления нет. GetThemeSettingForPlayer принимает номер игрока от 1. Индексы определений начинаются с 0.
</div>

| Метод | Описание |
| --- | --- |
| `THEME:GetResolution()  -> vector2` | Разрешение скина. |
| `THEME:GetThemeSetting(settingId)  -> string` | Значение настройки глобальной области. |
| `THEME:GetThemeSettingForPlayer(settingId, player)  -> string` | Значение настройки области сохранения для игрока с номером от 1 или её значение по умолчанию, если в сохранении значения нет. |
| `THEME:GetSkinString(key)  -> string` | Локализованная строка из папки Locales скина: сначала текущий язык, затем локаль скина по умолчанию, затем `[LOCALE NOT FOUND: key]`. |
| `THEME:GetDefinitionCount()  -> integer` | Число определений настроек в ThemeSettings.json. |
| `THEME:GetDefinitionId(index)  -> string` | Идентификатор определения по индексу от 0 или пустая строка. |
| `THEME:GetDefinitionScope(index)  -> string` | Область определения: "global" или "save". |
| `THEME:GetDefinitionType(index)  -> string` | Тип определения: "bool", "int", "double", "string" или "enum". |

### LANG

Локализованные строки игры, переключение языка и многоязычные текстовые значения.

<div class="callout warn">
GetString форматирует запись любыми дополнительными аргументами. GetLanguageIds и GetLanguageNames возвращают массивы .NET (от 0, `.Length`); GetAvailableLanguages возвращает словарь, перечисляемый через `:GetEnumerator()` (см. «Данные и хранение»). FromDict принимает разобранный JSON-объект из JSONLOADER (таблицу Lua он не принимает); AsLocalizationData принимает JsonNode из JSONLOADER:LoadJson.
</div>

| Метод | Описание |
| --- | --- |
| `LANG:GetString(key, ...)  -> string` | Локализованная строка для ключа, с заполнителями формата, заполненными из дополнительных аргументов. |
| `LANG:ChangeLanguage(id)  -> bool` | Переключает активный язык, если идентификатор существует и отличается от текущего, затем вызывает `reloadLanguage` у каждого загруженного скрипта; возвращает, произошло ли переключение. CONFIG.Language оставляет без изменений. |
| `LANG:GetLanguageIds()  -> string[]` | Идентификаторы доступных языков. |
| `LANG:GetLanguageNames()  -> string[]` | Отображаемые имена доступных языков, в том же порядке. |
| `LANG:GetAvailableLanguages()  -> dict` | Идентификатор языка в отображаемое имя. |
| `LANG:GetExamName(type)  -> string` | Локализованное имя типа дан-экзамена. |
| `LANG:AsLocalizationData(node)  -> localizationData` | Строит значение локализации из JsonNode вида `{ "strings": { "<lang>": "text" } }`. |
| `LANG:FromDict(dict)  -> localizationData` | Строит значение локализации из разобранного JSON-объекта, сопоставляющего идентификаторы языков с текстом. |
| `LANG:FromString(json)  -> localizationData` | Строит значение локализации из строки JSON-объекта, сопоставляющего идентификаторы языков с текстом; пустое значение, если строка не разбирается. |

```lua
local langs = LANG:GetAvailableLanguages()
local e = langs:GetEnumerator()
while e:MoveNext() do
    print(e.Current.Key, e.Current.Value)
end

local name = LANG:FromString('{"ja":"太鼓","default":"Taiko"}'):GetString("")
```

### Дескриптор данных локализации

Набор строк с ключами по идентификаторам языков, который разрешается в текущий язык.

<div class="callout warn">
Этот дескриптор возвращают LANG:AsLocalizationData, LANG:FromDict и LANG:FromString. Порядок разрешения: идентификатор текущего языка, затем ключ "default", затем запасное значение, переданное в GetString.
</div>

| Метод | Описание |
| --- | --- |
| `loc:GetString(fallback)  -> string` | Текст для текущего языка, или "default", или запасное значение. |
| `loc:SetString(langId, text)  -> nil` | Задаёт текст для идентификатора языка. |
| `loc:GetAllStrings()  -> string[]` | Все сохранённые тексты в произвольном порядке. |
