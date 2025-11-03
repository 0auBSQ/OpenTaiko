local M = {
	DBHook = nil
}

function M:GetScores()
	if M.DBHook == nil then
		M.DBHook = SQL:OpenSQLDatabase("Databases/Scores.db3")
	end

	local query = "SELECT player, score FROM highscores ORDER BY score DESC, id ASC LIMIT 8"

  return M.DBHook:Query(query)
end

function M:RegisterScore(playerName, score)
	if M.DBHook == nil then
		M.DBHook = SQL:OpenSQLDatabase("Databases/Scores.db3")
	end

  -- It is vulnerable to SQL Injections but doesn't matter much in the case of OpenTaiko's Intro Nokon minigame
  local query = string.format([[
		INSERT INTO highscores (player, score) VALUES ('%s', %d)
		ON CONFLICT(player) DO UPDATE SET
			score = excluded.score
		WHERE excluded.score > highscores.score;
	]], playerName, score)

	M.DBHook:Query(query)
end

return M
