-- lang/de.lua: the German dictionary of intro_nokon. Keys are the exact English texts (Lib/i18n.lua looks them up when drawing).
-- Nokon, the host, is a pompous cat. Keep {Player N name}, %s and %d as they are (filled in at run time).
local T = {}

-- Host Nokon: the opening
T["Nokon"] = "Nokon"
T["Welcome to today's Intro Nokon session!\nI am Nokon and we are here for an Endurance session!"] = "Willkommen zur heutigen Runde Intro Nokon!\nIch bin Nokon, und heute steht eine Ausdauer-Runde an!"
T["Here today's challenger, {Player 1 name}, will show us their musical knowledge!\nLet's cheer them and wish them good luck!"] = "Heute zeigt uns {Player 1 name} das eigene Musikwissen!\nApplaus, Applaus, und viel Glück!"
T["Alright, quiz time!\nIt's shoooooowtime!"] = "Also gut, Quizzeit!\nEs ist Shoooooowtime!"
T["Welcome to today's Intro Nokon session!\nI am Nokon and we are here for a VS session!"] = "Willkommen zur heutigen Runde Intro Nokon!\nIch bin Nokon, und heute steht eine VS-Runde an!"
T["Let me present today's challengers..."] = "Darf ich vorstellen: die heutige Besetzung..."
T["Who will win? We'll know it very soon...\nLet's cheer them and wish them good luck!"] = "Wer wird gewinnen? Wir erfahren es schon bald...\nApplaus, Applaus, und viel Glück!"

-- Correct answers (with the song title)
T["Correct, it was \"%s\"! Splendiferous!"] = "Richtig, es war „%s“! Großartigst!"
T["\"%s\" it was! Greeeeeeat!"] = "Es war „%s“! Super-duuuuper!"
T["Yes, \"%s\"! Good answer!"] = "Ja, „%s“! Gute Antwort!"
T["It was \"%s\"! Very for real!"] = "Es war „%s“! Voll echt!"
T["\"%s\" indeed! Sheeeeeeeesh!"] = "„%s“, genau! Krasssssss!"

-- One-song endurance teasing lines
T["Do you feel smart? How will you save your score Scherlock?"] = "Fühlst du dich schlau? Und wie willst du deinen Punktestand speichern, Sherlock?"
T["Are you enjoying the game? Perfect, because you are here forever!"] = "Macht dir das Spiel Spaß? Perfekt, denn du bleibst für immer hier!"
T["You did beat my score? Wait until you reach the results first..."] = "Du hast meinen Punktestand geschlagen? Warte erst mal, bis du bei den Ergebnissen bist..."
T["..."] = "..."

-- Wrong answer / time out
T["Too bad! I guess I cannot bet on a losing horse!"] = "Schade! Auf ein lahmes Pferd kann ich nicht setzen!"
T["Come on, be for real!"] = "Jetzt aber mal im Ernst!"
T["So you prefer dogs? Sorry, we do not do that here!"] = "Du magst also lieber Hunde? Tut mir leid, so was gibt es hier nicht!"
T["And that's a wrap! Time to end the show!"] = "Und das war's! Zeit, die Show zu beenden!"
T["No one found it?... Really?..."] = "Niemand hat es erraten?... Wirklich?..."
T["And a skewer of loosers, chief!"] = "Und einmal Verlierer-Spieß, Chef!"
T["I have no words..."] = "Mir fehlen die Worte..."

-- VS win (winner name and song title)
T["And that's another W for %s! The song was \"%s\"!"] = "Und noch ein Sieg für %s! Der Song war „%s“!"
T["%s takes the bag with \"%s\"! Brutal!"] = "%s räumt mit „%s“ alles ab! Brutal!"
T["I always knew %s was the best! It was \"%s\"!"] = "Ich wusste immer, dass %s unschlagbar ist! Es war „%s“!"

-- UI and screen texts
T["Loading songs..."] = "Songs werden geladen..."
T["Round %d / %d"] = "Runde %d / %d"
T["Game Over!"] = "Spiel vorbei!"
T["Final Results!"] = "Endergebnis!"
T["Score: %d"] = "Punkte: %d"
T["High Scores"] = "Highscores"
T["Decide: Play again   Cancel: Exit"] = "Bestätigen: Nochmal spielen   Abbrechen: Beenden"
T["Pick the correct song!"] = "Wähle den richtigen Song!"
T["Choose the song genre!"] = "Wähle das Genre!"
T["%s wins %d point!"] = "%s erhält %d Punkt!"
T["%s wins %d points!"] = "%s erhält %d Punkte!"
T["It's a wrap!"] = "Das war's!"
T["Let's set up the game show!"] = "Bereiten wir die Spielshow vor!"
T["Mode"] = "Modus"
T["Players"] = "Spieler"
T["Songs"] = "Songs"
T["Round Count"] = "Rundenzahl"
T["The game can only be started if there are at least 5 available songs."] = "Das Spiel kann nur mit mindestens 5 verfügbaren Songs gestartet werden."

return T
