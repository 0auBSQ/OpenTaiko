-- Pagoda.Progress: the pagoda's persistent save data, all in player 1's save counters and triggers:
-- the highest level cleared and reached, whether the opening story has played, the floors lost on,
-- the play count, the held mods, and the one-time structure migration.
--   pagoda_highest_level, pagoda_highest_reached, pagoda_plays, pagoda_structure_version (counters)
--   pagoda_story_first_seen, pagoda_lost_<level> (triggers)
--   pagoda_held_mods_lo / _hi (counters) + pagoda_held_mods (trigger)

local M = {}

-- ── Persistence helpers ───────────────────────────────────────────────────────

function M.highest_level()
    return math.max(1, math.floor(GetSaveFile(0):GetGlobalCounter("pagoda_highest_level")))
end

function M.set_highest_level(level)
    if level > M.highest_level() then
        GetSaveFile(0):SetGlobalCounter("pagoda_highest_level", level)
    end
end

-- the highest floor a challenge run reached (its encounter screen opened), which practice offers up to.
-- Saves from before it was kept count the floor after the highest cleared one as reached.
function M.highest_reached()
    local sf = GetSaveFile(0)
    local kept = math.floor(sf:GetGlobalCounter("pagoda_highest_reached"))
    local cleared = math.floor(sf:GetGlobalCounter("pagoda_highest_level"))
    return math.max(kept, cleared >= 1 and cleared + 1 or 0)
end

function M.set_highest_reached(level)
    if level > M.highest_reached() then
        GetSaveFile(0):SetGlobalCounter("pagoda_highest_reached", level)
    end
end

-- the opening story has played on this save. One-way boolean, so it lives
-- in a global trigger, named per story; saves from before the switch may still carry the old
-- "pagoda_story_seen" counter form.
function M.story_first_seen()
    local sf = GetSaveFile(0)
    if sf:GetGlobalTrigger("pagoda_story_first_seen") then return true end
    return sf:GetGlobalCounter("pagoda_story_seen") >= 1
end

function M.mark_story_first_seen()
    GetSaveFile(0):SetGlobalTrigger("pagoda_story_first_seen", true)
end

-- floors where a challenge run failed (one-way, one trigger per level)
local function lost_key(level)
    return "pagoda_lost_" .. math.floor(level)
end

function M.mark_lost(level)
    GetSaveFile(0):SetGlobalTrigger(lost_key(level), true)
end

function M.lost_before(level)
    return GetSaveFile(0):GetGlobalTrigger(lost_key(level)) == true
end

-- player 1's mod flags from before a pagoda play, held until the play returns: two 32-bit
-- counters, since a counter (a double) cannot hold all 64 bits, and a trigger saying they are held
function M.hold_mods(flags)
    local sf = GetSaveFile(0)
    sf:SetGlobalCounter("pagoda_held_mods_lo", flags & 0xFFFFFFFF)
    sf:SetGlobalCounter("pagoda_held_mods_hi", (flags >> 32) & 0xFFFFFFFF)
    sf:SetGlobalTrigger("pagoda_held_mods", true)
end

-- the held flags, or nil when none are held
function M.held_mods()
    local sf = GetSaveFile(0)
    if not sf:GetGlobalTrigger("pagoda_held_mods") then return nil end
    local lo = math.floor(sf:GetGlobalCounter("pagoda_held_mods_lo"))
    local hi = math.floor(sf:GetGlobalCounter("pagoda_held_mods_hi"))
    return (hi << 32) | lo
end

function M.drop_held_mods()
    local sf = GetSaveFile(0)
    if sf:GetGlobalTrigger("pagoda_held_mods") then sf:SetGlobalTrigger("pagoda_held_mods", false) end
end

-- how many times the pagoda was played: a challenge run counts once its fail screen appears
function M.plays()
    return math.max(0, math.floor(GetSaveFile(0):GetGlobalCounter("pagoda_plays")))
end

function M.count_play()
    GetSaveFile(0):SetGlobalCounter("pagoda_plays", M.plays() + 1)
end

-- One-time migration for the 題/扉/穹天 restructure (題 is now 道): the kyuu range grew from 5 to 10 levels and the
-- 扉/穹天 tiers shifted +5, so a saved highest_level from before this update must be renumbered or the
-- player would lose progress / start-point unlocks. Old level → new level:
--   kyuu 1..5 → 2, 3, 5, 8, 10   (the old kyuu pools now live at those positions)
--   anything ≥ 6 (扉/屋根) → +5
-- Version-gated so it runs exactly once; brand-new saves (highest 0) are left untouched.
function M.migrate_save()
    local sf = GetSaveFile(0)
    if math.floor(sf:GetGlobalCounter("pagoda_structure_version")) >= 2 then return end
    local old = math.floor(sf:GetGlobalCounter("pagoda_highest_level"))
    if old >= 1 then
        local KYUU = { [1] = 2, [2] = 3, [3] = 5, [4] = 8, [5] = 10 }
        local new  = (old <= 5) and (KYUU[old] or old) or (old + 5)
        sf:SetGlobalCounter("pagoda_highest_level", new)
    end
    sf:SetGlobalCounter("pagoda_structure_version", 2)
end

return M
