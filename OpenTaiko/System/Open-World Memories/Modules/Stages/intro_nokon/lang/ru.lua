-- lang/ru.lua: the Russian dictionary of intro_nokon. Keys are the exact English texts (Lib/i18n.lua looks them up when drawing).
-- Nokon, the host, is a pompous cat. Keep {Player N name}, %s and %d as they are (filled in at run time).
local T = {}

-- Host Nokon: the opening
T["Nokon"] = "Нокон"
T["Welcome to today's Intro Nokon session!\nI am Nokon and we are here for an Endurance session!"] = "Добро пожаловать на сегодняшнее Интро Нокон!\nЯ Нокон, и сегодня у нас сессия на выносливость!"
T["Here today's challenger, {Player 1 name}, will show us their musical knowledge!\nLet's cheer them and wish them good luck!"] = "Сегодня {Player 1 name} покажет нам свои музыкальные знания!\nПоддержим аплодисментами и пожелаем удачи!"
T["Alright, quiz time!\nIt's shoooooowtime!"] = "Итак, время викторины!\nШоооооу начинается!"
T["Welcome to today's Intro Nokon session!\nI am Nokon and we are here for a VS session!"] = "Добро пожаловать на сегодняшнее Интро Нокон!\nЯ Нокон, и сегодня у нас сессия VS!"
T["Let me present today's challengers..."] = "Позвольте представить сегодняшних участников..."
T["Who will win? We'll know it very soon...\nLet's cheer them and wish them good luck!"] = "Кто победит? Скоро узнаем...\nПоддержим аплодисментами и пожелаем удачи!"

-- Correct answers (with the song title)
T["Correct, it was \"%s\"! Splendiferous!"] = "Верно, это была «%s»! Великолепнейше!"
T["\"%s\" it was! Greeeeeeat!"] = "Это была «%s»! Потряяяяясающе!"
T["Yes, \"%s\"! Good answer!"] = "Да, «%s»! Хороший ответ!"
T["It was \"%s\"! Very for real!"] = "Это была «%s»! Реально по-настоящему!"
T["\"%s\" indeed! Sheeeeeeeesh!"] = "И правда «%s»! Ваааааау!"

-- One-song endurance teasing lines
T["Do you feel smart? How will you save your score Scherlock?"] = "Думаете, вы гений? А как вы собираетесь сохранить свой счёт, Шерлок?"
T["Are you enjoying the game? Perfect, because you are here forever!"] = "Нравится игра? Отлично, ведь вы здесь навсегда!"
T["You did beat my score? Wait until you reach the results first..."] = "Вы побили мой рекорд? Сначала доберитесь до результатов..."
T["..."] = "..."

-- Wrong answer / time out
T["Too bad! I guess I cannot bet on a losing horse!"] = "Как жаль! Не могу же я ставить на проигрышную лошадь!"
T["Come on, be for real!"] = "Ну же, давайте серьёзнее!"
T["So you prefer dogs? Sorry, we do not do that here!"] = "Так вы любите собак? Простите, у нас такое не принято!"
T["And that's a wrap! Time to end the show!"] = "Вот и всё! Пора заканчивать шоу!"
T["No one found it?... Really?..."] = "Никто не угадал?.. Серьёзно?.."
T["And a skewer of loosers, chief!"] = "И шашлычок из проигравших, шеф!"
T["I have no words..."] = "У меня нет слов..."

-- VS win (winner name and song title)
T["And that's another W for %s! The song was \"%s\"!"] = "%s снова побеждает! Это была песня «%s»!"
T["%s takes the bag with \"%s\"! Brutal!"] = "%s забирает всё с песней «%s»! Жёстко!"
T["I always knew %s was the best! It was \"%s\"!"] = "Ну конечно, %s лучше всех! Это была «%s»!"

-- UI and screen texts
T["Loading songs..."] = "Загрузка песен..."
T["Round %d / %d"] = "Раунд %d / %d"
T["Game Over!"] = "Игра окончена!"
T["Final Results!"] = "Итоговые результаты!"
T["Score: %d"] = "Счёт: %d"
T["High Scores"] = "Рекорды"
T["Decide: Play again   Cancel: Exit"] = "Подтвердить: сыграть снова   Отмена: выход"
T["Pick the correct song!"] = "Выберите правильную песню!"
T["Choose the song genre!"] = "Выберите жанр песни!"
T["%s wins %d point!"] = "%s: +%d к счёту!"
T["%s wins %d points!"] = "%s: +%d к счёту!"
T["It's a wrap!"] = "Вот и всё!"
T["Let's set up the game show!"] = "Подготовим игровое шоу!"
T["Mode"] = "Режим"
T["Players"] = "Игроки"
T["Songs"] = "Песни"
T["Round Count"] = "Число раундов"
T["The game can only be started if there are at least 5 available songs."] = "Игру можно начать, только если доступно не меньше 5 песен."

return T
