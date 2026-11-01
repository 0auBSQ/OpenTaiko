-- lang/fr.lua: the French dictionary of intro_nokon. Keys are the exact English texts (Lib/i18n.lua looks them up when drawing).
-- Nokon, the host, is a pompous cat. Keep {Player N name}, %s and %d as they are (filled in at run time).
local T = {}

-- Host Nokon: the opening
T["Nokon"] = "Nokon"
T["Welcome to today's Intro Nokon session!\nI am Nokon and we are here for an Endurance session!"] = "Bienvenue dans la session Intro Nokon du jour !\nJe suis Nokon, et aujourd'hui, c'est une session Endurance !"
T["Here today's challenger, {Player 1 name}, will show us their musical knowledge!\nLet's cheer them and wish them good luck!"] = "Aujourd'hui, {Player 1 name} relève le défi et va nous montrer sa culture musicale !\nUn tonnerre d'applaudissements, et bonne chance !"
T["Alright, quiz time!\nIt's shoooooowtime!"] = "Allez, place au quiz !\nC'est l'heure du shoooooow !"
T["Welcome to today's Intro Nokon session!\nI am Nokon and we are here for a VS session!"] = "Bienvenue dans la session Intro Nokon du jour !\nJe suis Nokon, et aujourd'hui, c'est une session VS !"
T["Let me present today's challengers..."] = "Permettez-moi de vous présenter l'affiche du jour..."
T["Who will win? We'll know it very soon...\nLet's cheer them and wish them good luck!"] = "Qui va gagner ? Nous le saurons très bientôt...\nUn tonnerre d'applaudissements, et bonne chance !"

-- Correct answers (with the song title)
T["Correct, it was \"%s\"! Splendiferous!"] = "Correct, c'était « %s » ! Splendifique !"
T["\"%s\" it was! Greeeeeeat!"] = "C'était « %s » ! Géniaaaaaal !"
T["Yes, \"%s\"! Good answer!"] = "Oui, « %s » ! Bonne réponse !"
T["It was \"%s\"! Very for real!"] = "C'était « %s » ! Grave pour de vrai !"
T["\"%s\" indeed! Sheeeeeeeesh!"] = "« %s », en effet ! Wouaaaaah !"

-- One-song endurance teasing lines
T["Do you feel smart? How will you save your score Scherlock?"] = "Vous vous prenez pour un génie ? Et comment comptez-vous sauvegarder votre score, Sherlock ?"
T["Are you enjoying the game? Perfect, because you are here forever!"] = "Vous vous amusez bien ? Parfait, car vous êtes ici pour toujours !"
T["You did beat my score? Wait until you reach the results first..."] = "Vous avez battu mon score ? Attendez déjà d'arriver aux résultats..."
T["..."] = "..."

-- Wrong answer / time out
T["Too bad! I guess I cannot bet on a losing horse!"] = "Dommage ! Je ne peux pas parier sur un cheval perdant !"
T["Come on, be for real!"] = "Allons, un peu de sérieux !"
T["So you prefer dogs? Sorry, we do not do that here!"] = "Vous préférez les chiens ? Pas de ça chez nous !"
T["And that's a wrap! Time to end the show!"] = "Et c'est dans la boîte ! Le show est terminé !"
T["No one found it?... Really?..."] = "Personne n'a trouvé ?... Vraiment ?..."
T["And a skewer of loosers, chief!"] = "Et une brochette de perdants, chef !"
T["I have no words..."] = "Je n'ai pas de mots..."

-- VS win (winner name and song title)
T["And that's another W for %s! The song was \"%s\"!"] = "Et une victoire de plus pour %s ! La chanson était « %s » !"
T["%s takes the bag with \"%s\"! Brutal!"] = "%s rafle tout avec « %s » ! Brutal !"
T["I always knew %s was the best! It was \"%s\"!"] = "J'ai toujours su que %s était au sommet ! C'était « %s » !"

-- UI and screen texts
T["Loading songs..."] = "Chargement des chansons..."
T["Round %d / %d"] = "Manche %d / %d"
T["Game Over!"] = "Partie terminée !"
T["Final Results!"] = "Résultats finaux !"
T["Score: %d"] = "Score : %d"
T["High Scores"] = "Meilleurs scores"
T["Decide: Play again   Cancel: Exit"] = "Valider : rejouer   Annuler : quitter"
T["Pick the correct song!"] = "Trouvez la bonne chanson !"
T["Choose the song genre!"] = "Choisissez le genre musical !"
T["%s wins %d point!"] = "%s gagne %d point !"
T["%s wins %d points!"] = "%s gagne %d points !"
T["It's a wrap!"] = "C'est dans la boîte !"
T["Let's set up the game show!"] = "Préparons le jeu télévisé !"
T["Mode"] = "Mode"
T["Players"] = "Joueurs"
T["Songs"] = "Chansons"
T["Round Count"] = "Nombre de manches"
T["The game can only be started if there are at least 5 available songs."] = "La partie ne peut commencer qu'avec au moins 5 chansons disponibles."

return T
