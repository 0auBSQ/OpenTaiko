---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- High scores of the Intro Nokon minigame: each profile's best Endurance score is its save's global counter
-- SCORE_KEY; the table shows the profiles of the player slots among the rivals of Databases/DefaultScores.json.

local M = {}

local SCORE_KEY     = ".intro_nokon_high_score"
local LIMIT         = 8
local SLOTS         = 5
local DEFAULTS_JSON = "Databases/DefaultScores.json"
local OLD_STORE     = "IntroNokon/HighScores"   -- the global LMDB table the scores were kept in before

local rivals = nil
local oldScores = nil

-- the rivals, { name, score } each
local function loadRivals()
	if rivals ~= nil then return rivals end
	rivals = {}
	local arr = JSONLOADER:JsonParseFileAny(DEFAULTS_JSON)
	if arr == nil then return rivals end
	for i = 1, JSONLOADER:JsonCount(arr) do
		local row = JSONLOADER:JsonGet(arr, i)
		if row ~= nil then
			rivals[#rivals + 1] = {
				name  = tostring(JSONLOADER:JsonGet(row, "player") or ""),
				score = math.floor(tonumber(tostring(JSONLOADER:JsonGet(row, "score") or 0)) or 0),
			}
		end
	end
	return rivals
end

-- the old table's scores by player name, the rivals' own rows left out; read once
local function loadOldScores()
	if oldScores ~= nil then return oldScores end
	oldScores = {}
	local isRival = {}
	for _, r in ipairs(loadRivals()) do isRival[r.name .. "\0" .. r.score] = true end
	pcall(function()
		local raw = DATABASE:OpenGlobalDatabase(OLD_STORE):Read("scores")
		local f = raw ~= nil and load("return " .. raw) or nil
		local list = f ~= nil and f() or nil
		for _, row in ipairs(type(list) == "table" and list or {}) do
			local name, score = tostring(row.player or ""), math.floor(tonumber(row.score) or 0)
			if not isRival[name .. "\0" .. score] then oldScores[name] = math.max(oldScores[name] or 0, score) end
		end
	end)
	return oldScores
end

-- a profile's best score, taking over its score from the old table the first time
local function best(sf)
	local score = math.floor(sf:GetGlobalCounter(SCORE_KEY))
	local old = loadOldScores()[sf.Name] or 0
	if old > score then
		sf:SetGlobalCounter(SCORE_KEY, old)
		score = old
	end
	return score
end

-- the top LIMIT of the rivals and the slots' profiles (each once, if they have a score), { name, score }, highest
-- first; a profile ties above a rival
function M:GetScores()
	local rows = {}
	local seen = {}
	for slot = 0, SLOTS - 1 do
		local ok, sf = pcall(GetSaveFile, slot)
		if ok and sf ~= nil and not seen[sf.SaveId] then
			seen[sf.SaveId] = true
			local score = best(sf)
			if score > 0 then rows[#rows + 1] = { name = sf.Name, score = score, order = #rows } end
		end
	end
	for _, r in ipairs(loadRivals()) do rows[#rows + 1] = { name = r.name, score = r.score, order = #rows } end
	table.sort(rows, function(a, b)
		if a.score ~= b.score then return a.score > b.score end
		return a.order < b.order
	end)
	local out = {}
	for i = 1, math.min(LIMIT, #rows) do out[i] = { name = rows[i].name, score = rows[i].score } end
	return out
end

-- keeps score as player 1's best when it beats it
function M:RegisterScore(score)
	local sf = GetSaveFile(0)
	if sf == nil then return end
	score = math.floor(tonumber(score) or 0)
	if score > best(sf) then sf:SetGlobalCounter(SCORE_KEY, score) end
end

return M
