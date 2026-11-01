-- lang/nl.lua: the Dutch dictionary of intro_nokon. Keys are the exact English texts (Lib/i18n.lua looks them up when drawing).
-- Nokon, the host, is a pompous cat. Keep {Player N name}, %s and %d as they are (filled in at run time).
local T = {}

-- Host Nokon: the opening
T["Nokon"] = "Nokon"
T["Welcome to today's Intro Nokon session!\nI am Nokon and we are here for an Endurance session!"] = "Welkom bij de Intro Nokon-sessie van vandaag!\nIk ben Nokon en vandaag doen we een uithoudingssessie!"
T["Here today's challenger, {Player 1 name}, will show us their musical knowledge!\nLet's cheer them and wish them good luck!"] = "Vandaag bewijst {Player 1 name} zich met muziekkennis!\nEen daverend applaus en veel succes!"
T["Alright, quiz time!\nIt's shoooooowtime!"] = "Oké, quiztijd!\nHet is shoooooowtime!"
T["Welcome to today's Intro Nokon session!\nI am Nokon and we are here for a VS session!"] = "Welkom bij de Intro Nokon-sessie van vandaag!\nIk ben Nokon en vandaag doen we een VS-sessie!"
T["Let me present today's challengers..."] = "Laat me de uitdagers van vandaag voorstellen..."
T["Who will win? We'll know it very soon...\nLet's cheer them and wish them good luck!"] = "Wie gaat er winnen? Dat weten we heel snel...\nEen daverend applaus en veel succes!"

-- Correct answers (with the song title)
T["Correct, it was \"%s\"! Splendiferous!"] = "Correct, het was \"%s\"! Formidabel!"
T["\"%s\" it was! Greeeeeeat!"] = "Het was \"%s\"! Geweeeeeldig!"
T["Yes, \"%s\"! Good answer!"] = "Ja, \"%s\"! Goed antwoord!"
T["It was \"%s\"! Very for real!"] = "Het was \"%s\"! Echt echt!"
T["\"%s\" indeed! Sheeeeeeeesh!"] = "\"%s\" inderdaad! Sjeeeeeeesh!"

-- One-song endurance teasing lines
T["Do you feel smart? How will you save your score Scherlock?"] = "Voel je je slim? Hoe ga je je score opslaan, Sherlock?"
T["Are you enjoying the game? Perfect, because you are here forever!"] = "Vermaak je je? Perfect, want je blijft hier voor altijd!"
T["You did beat my score? Wait until you reach the results first..."] = "Heb je mijn score verslagen? Wacht eerst maar tot je bij de resultaten bent..."
T["..."] = "..."

-- Wrong answer / time out
T["Too bad! I guess I cannot bet on a losing horse!"] = "Jammer! Ik kan niet op een verliezend paard wedden!"
T["Come on, be for real!"] = "Kom op, doe eens serieus!"
T["So you prefer dogs? Sorry, we do not do that here!"] = "Dus je houdt meer van honden? Sorry, dat doen we hier niet!"
T["And that's a wrap! Time to end the show!"] = "En dat was het! Tijd om de show af te sluiten!"
T["No one found it?... Really?..."] = "Niemand heeft het geraden?... Echt?..."
T["And a skewer of loosers, chief!"] = "En een spies verliezers, chef!"
T["I have no words..."] = "Ik heb er geen woorden voor..."

-- VS win (winner name and song title)
T["And that's another W for %s! The song was \"%s\"!"] = "En weer een W voor %s! Het nummer was \"%s\"!"
T["%s takes the bag with \"%s\"! Brutal!"] = "%s sleept alles binnen met \"%s\"! Bruut!"
T["I always knew %s was the best! It was \"%s\"!"] = "Ik wist altijd al dat %s de beste was! Het was \"%s\"!"

-- UI and screen texts
T["Loading songs..."] = "Nummers laden..."
T["Round %d / %d"] = "Ronde %d / %d"
T["Game Over!"] = "Game over!"
T["Final Results!"] = "Eindresultaten!"
T["Score: %d"] = "Score: %d"
T["High Scores"] = "Topscores"
T["Decide: Play again   Cancel: Exit"] = "Bevestigen: opnieuw spelen   Annuleren: afsluiten"
T["Pick the correct song!"] = "Kies het juiste nummer!"
T["Choose the song genre!"] = "Kies het genre!"
T["%s wins %d point!"] = "%s wint %d punt!"
T["%s wins %d points!"] = "%s wint %d punten!"
T["It's a wrap!"] = "Dat was het!"
T["Let's set up the game show!"] = "Laten we de spelshow klaarzetten!"
T["Mode"] = "Modus"
T["Players"] = "Spelers"
T["Songs"] = "Nummers"
T["Round Count"] = "Aantal rondes"
T["The game can only be started if there are at least 5 available songs."] = "Het spel kan alleen starten als er minstens 5 nummers beschikbaar zijn."

return T
