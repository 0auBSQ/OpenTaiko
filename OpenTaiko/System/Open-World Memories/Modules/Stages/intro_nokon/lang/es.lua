-- lang/es.lua: the Spanish dictionary of intro_nokon. Keys are the exact English texts (Lib/i18n.lua looks them up when drawing).
-- Nokon, the host, is a pompous cat. Keep {Player N name}, %s and %d as they are (filled in at run time).
local T = {}

-- Host Nokon: the opening
T["Nokon"] = "Nokon"
T["Welcome to today's Intro Nokon session!\nI am Nokon and we are here for an Endurance session!"] = "¡Les damos la bienvenida a la sesión de Intro Nokon de hoy!\n¡Soy Nokon y hoy toca una sesión de Resistencia!"
T["Here today's challenger, {Player 1 name}, will show us their musical knowledge!\nLet's cheer them and wish them good luck!"] = "¡Hoy {Player 1 name} acepta el reto y nos mostrará sus conocimientos musicales!\n¡Un gran aplauso y mucha suerte!"
T["Alright, quiz time!\nIt's shoooooowtime!"] = "¡Muy bien, hora del quiz!\n¡Es la hora del shoooooow!"
T["Welcome to today's Intro Nokon session!\nI am Nokon and we are here for a VS session!"] = "¡Les damos la bienvenida a la sesión de Intro Nokon de hoy!\n¡Soy Nokon y hoy toca una sesión VS!"
T["Let me present today's challengers..."] = "Permítanme presentarles el cartel de hoy..."
T["Who will win? We'll know it very soon...\nLet's cheer them and wish them good luck!"] = "¿Quién ganará? Lo sabremos muy pronto...\n¡Un gran aplauso y mucha suerte!"

-- Correct answers (with the song title)
T["Correct, it was \"%s\"! Splendiferous!"] = "¡Correcto, era \"%s\"! ¡Espléndido!"
T["\"%s\" it was! Greeeeeeat!"] = "¡Era \"%s\"! ¡Geniaaaaaal!"
T["Yes, \"%s\"! Good answer!"] = "¡Sí, \"%s\"! ¡Buena respuesta!"
T["It was \"%s\"! Very for real!"] = "¡Era \"%s\"! ¡Muy de verdad!"
T["\"%s\" indeed! Sheeeeeeeesh!"] = "¡\"%s\", en efecto! ¡Uaaaaaau!"

-- One-song endurance teasing lines
T["Do you feel smart? How will you save your score Scherlock?"] = "¿Te crees un genio? ¿Y cómo vas a guardar tu puntuación, Sherlock?"
T["Are you enjoying the game? Perfect, because you are here forever!"] = "¿Disfrutas del juego? ¡Perfecto, porque te quedas aquí para siempre!"
T["You did beat my score? Wait until you reach the results first..."] = "¿Superaste mi puntuación? Espera a llegar primero a los resultados..."
T["..."] = "..."

-- Wrong answer / time out
T["Too bad! I guess I cannot bet on a losing horse!"] = "¡Qué pena! ¡No puedo apostar por un caballo perdedor!"
T["Come on, be for real!"] = "¡Venga ya, un poco de seriedad!"
T["So you prefer dogs? Sorry, we do not do that here!"] = "¿Así que prefieres los perros? ¡Lo siento, aquí no hacemos eso!"
T["And that's a wrap! Time to end the show!"] = "¡Y hasta aquí! ¡Se acabó el show!"
T["No one found it?... Really?..."] = "¿Nadie lo adivinó?... ¿En serio?..."
T["And a skewer of loosers, chief!"] = "¡Y una brocheta de perdedores, jefe!"
T["I have no words..."] = "No tengo palabras..."

-- VS win (winner name and song title)
T["And that's another W for %s! The song was \"%s\"!"] = "¡Y otra victoria para %s! ¡La canción era \"%s\"!"
T["%s takes the bag with \"%s\"! Brutal!"] = "¡%s se lo lleva todo con \"%s\"! ¡Brutal!"
T["I always knew %s was the best! It was \"%s\"!"] = "¡Siempre supe que %s estaba en la cima! ¡Era \"%s\"!"

-- UI and screen texts
T["Loading songs..."] = "Cargando canciones..."
T["Round %d / %d"] = "Ronda %d / %d"
T["Game Over!"] = "¡Fin del juego!"
T["Final Results!"] = "¡Resultados finales!"
T["Score: %d"] = "Puntuación: %d"
T["High Scores"] = "Mejores puntuaciones"
T["Decide: Play again   Cancel: Exit"] = "Confirmar: jugar otra vez   Cancelar: salir"
T["Pick the correct song!"] = "¡Elige la canción correcta!"
T["Choose the song genre!"] = "¡Elige el género musical!"
T["%s wins %d point!"] = "¡%s gana %d punto!"
T["%s wins %d points!"] = "¡%s gana %d puntos!"
T["It's a wrap!"] = "¡Hasta aquí!"
T["Let's set up the game show!"] = "¡Preparemos el concurso!"
T["Mode"] = "Modo"
T["Players"] = "Jugadores"
T["Songs"] = "Canciones"
T["Round Count"] = "Número de rondas"
T["The game can only be started if there are at least 5 available songs."] = "El juego solo puede empezar si hay al menos 5 canciones disponibles."

return T
