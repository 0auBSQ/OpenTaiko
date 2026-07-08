---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- High-score persistence for the Intro Nokon minigame.
--
-- Scores live in a GLOBAL LMDB store under Global/ApplicationData/LMDB, OUTSIDE the skin, so a skin
-- update never wipes them. The old store (Databases/Scores.db3) sat inside the skin and was replaced
-- on every update, losing the player's high scores. On first run -- or if the store is empty/corrupt --
-- it is seeded from the shipped Databases/DefaultScores.json.

local M = {
	db    = nil,   -- cached LMDB handle (each op opens/closes its own env internally)
	cache = nil,   -- array of { player = string, score = int }, unsorted
}

local STORE         = "IntroNokon/HighScores"   -- Global/ApplicationData/LMDB/IntroNokon/HighScores
local KEY           = "scores"
local LIMIT         = 8
local DEFAULTS_JSON = "Databases/DefaultScores.json"

local function openDB()
	if M.db == nil then
		M.db = DATABASE:OpenGlobalDatabase(STORE)
	end
	return M.db
end

-- Persist as a Lua-table literal (same convention as iso_demo2/saveData.lua). Player names go through
-- %q so quotes/specials round-trip safely -- which also removes the old SQL-injection risk.
local function serialize(list)
	local parts = { "{" }
	for _, row in ipairs(list) do
		parts[#parts + 1] = string.format("{player=%q,score=%d},",
			tostring(row.player or ""), math.floor(tonumber(row.score) or 0))
	end
	parts[#parts + 1] = "}"
	return table.concat(parts)
end

local function deserialize(str)
	local f = load("return " .. str)
	if not f then return nil end
	local ok, res = pcall(f)
	if ok and type(res) == "table" then return res end
	return nil
end

-- Read the shipped defaults (JSON array of { player, score }) into the internal { player, score } shape.
local function loadDefaults()
	local out = {}
	local arr = JSONLOADER:JsonParseFileAny(DEFAULTS_JSON)
	if arr == nil then return out end
	for i = 1, JSONLOADER:JsonCount(arr) do
		local row = JSONLOADER:JsonGet(arr, i)
		if row ~= nil then
			out[#out + 1] = {
				player = tostring(JSONLOADER:JsonGet(row, "player") or ""),
				score  = math.floor(tonumber(tostring(JSONLOADER:JsonGet(row, "score") or 0)) or 0),
			}
		end
	end
	return out
end

-- Load the score list into M.cache, seeding + persisting the defaults on first run.
local function ensureLoaded()
	if M.cache ~= nil then return M.cache end
	openDB()
	local raw    = M.db:Read(KEY)
	local scores = raw ~= nil and deserialize(raw) or nil
	if scores == nil then
		scores = loadDefaults()
		M.db:Write(KEY, serialize(scores))
	end
	M.cache = scores
	return scores
end

-- Top-LIMIT scores as an array of { name = string, score = int }, highest first
-- (ties keep insertion order, matching the old "ORDER BY score DESC, id ASC").
function M:GetScores()
	local list = ensureLoaded() or {}
	local sorted = {}
	for i, row in ipairs(list) do
		sorted[#sorted + 1] = { name = row.player, score = row.score, order = i }
	end
	table.sort(sorted, function(a, b)
		if a.score ~= b.score then return a.score > b.score end
		return a.order < b.order
	end)
	local out = {}
	for i = 1, math.min(LIMIT, #sorted) do
		out[i] = { name = sorted[i].name, score = sorted[i].score }
	end
	return out
end

-- Upsert the player's best score, then persist. Keeps the higher of the old/new score per player.
function M:RegisterScore(playerName, score)
	local list = ensureLoaded() or {}
	playerName = tostring(playerName or "")
	score      = math.floor(tonumber(score) or 0)
	for _, row in ipairs(list) do
		if row.player == playerName then
			if score > row.score then row.score = score end
			M.db:Write(KEY, serialize(list))
			return
		end
	end
	list[#list + 1] = { player = playerName, score = score }
	M.db:Write(KEY, serialize(list))
end

return M
