---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- landlord phone call: sells room tiers (data/tiers.json) for coins
-- lines: lang/<code>/dialogs.json under "landlord", EN as fallback

local Landlord = {}

local MET = "myroom_landlord_met"          -- save trigger: first call done

local EN = {
    name = "Landlord",
    first_call = { "So you are the new tenant...", "I hope you are not too noisy, you know, it is not very good for business...",
                   "The room is too small you say? Hmmm...", "Call me back, and be sure to bring shiny doubloons with you.",
                   "If you can pay a generous extra, I might consider it.", "Now get out, time is money, and this call is keeping me broke." },
    greetings = { "Oh, it is you. Let's see what we are on today." },
    pitch = { "For {cost} of your shinies, I can give you a {size} room. Deal?" },
    broke = { "... Wait", "Whatever, brokie.", "Goodbye." },
    accepted = { "Excellent, enjoy your bigger room, while I enjoy my bigger pockets..." },
    maxed = "Sorry, there is no bigger expansion available.",
    refuse_first = "That's unfortunate. Feel free to call me back if you reconsider.",
    refuse_second = "...",
    refused_out = { "... No, do not waste my time.", "Never call again." },
    choice_yes = "Deal",
    choice_no = "No deal",
}

local ctx = nil
local step = nil               -- "first" | "offer" | "end"
local refusals = 0             -- No answers this visit
local tierKey = nil            -- "2" | "3" | "4" | "more" for the running offer
local offer = nil              -- nextTierInfo of the running offer

-- c: save() -> save file, room() -> Room, extend(info) -> bool, coinBox (a purse)
--    dlgLoc(section, key, fallback), dlgList(section, key, ...) -> lines
function Landlord.init(c) ctx = c end
-- on entering the room: refusals reset
function Landlord.reset() refusals, step, tierKey, offer = 0, nil, nil, nil end
function Landlord.refusals() return refusals end
function Landlord.step() return step end

local function LL(key) return ctx.dlgLoc("landlord", key, EN[key] or key) end

-- lines at a dotted path under "landlord"; fallback list when missing
local function lines(path, fallback)
    local out = {}
    if ctx.dlgList then
        local keys = { "landlord" }
        for seg in path:gmatch("[^%.]+") do keys[#keys + 1] = seg end
        local ok, res = pcall(ctx.dlgList, table.unpack(keys))
        if ok and type(res) == "table" then out = res end
    end
    if #out == 0 then
        for i, s in ipairs(fallback or {}) do out[i] = s end
    end
    return out
end

local function fill(s, info)
    if info == nil then return s end
    s = s:gsub("{size}", info.iw .. "x" .. info.ih)
    s = s:gsub("{cost}", tostring(info.cost))
    return s
end

local function nodes(list, info)
    local out = {}
    for i, s in ipairs(list) do out[i] = { name = LL("name"), text = fill(s, info) } end
    return out
end

-- "more": every tier past 4
local function tierKeyFor(tier)
    if tier <= 4 then return tostring(tier) end
    return "more"
end

-- dialogue nodes for a new call
function Landlord.script()
    local sf, room = ctx.save(), ctx.room()
    if not sf:GetGlobalTrigger(MET) then
        step = "first"                              -- no offer on the first call
        return nodes(lines("first_call", EN.first_call))
    end
    if refusals >= 2 then
        step = "end"
        return nodes(lines("refused_out", EN.refused_out))
    end
    local greetings = lines("greetings", EN.greetings)
    local script = { { name = LL("name"), text = greetings[math.random(#greetings)] } }
    if not room:canExtend() then
        step = "end"
        script[#script + 1] = { name = LL("name"), text = LL("maxed") }
        return script
    end
    local info = room:nextTierInfo()
    tierKey, offer = tierKeyFor(room.tier + 1), info
    for _, n in ipairs(nodes(lines("tiers." .. tierKey .. ".pitch", EN.pitch), info)) do script[#script + 1] = n end
    if sf.Coins < info.cost then
        step = "end"                                -- not enough coins: no choice
        for _, n in ipairs(nodes(lines("tiers." .. tierKey .. ".broke", EN.broke), info)) do script[#script + 1] = n end
        return script
    end
    step = "offer"
    script[#script].choices = { { label = LL("choice_yes"), value = "yes" }, { label = LL("choice_no"), value = "no" } }
    return script
end

-- {sfx:purse} cue in the price line
function Landlord.showPurse()
    if offer == nil then return end
    ctx.coinBox:show(ctx.save().Coins)
    ctx.coinBox:setPrice(offer.cost)
end

-- after the dialogue: follow-up nodes, or nil when the call is over
function Landlord.onDone(result)
    if step == "first" then
        ctx.save():SetGlobalTrigger(MET, true)
        step = nil
        return nil
    end
    if step == "offer" then
        local key = tierKey or "more"
        step = "end"
        if result == "yes" then
            local info = ctx.room():nextTierInfo()
            if info and ctx.extend(info) then
                ctx.coinBox:pay(info.cost)
                return nodes(lines("tiers." .. key .. ".accepted", EN.accepted), info)
            end
            return nodes(lines("tiers." .. key .. ".broke", EN.broke), info)
        end
        refusals = refusals + 1
        ctx.coinBox:setPrice(nil)
        return { { name = LL("name"), text = LL(refusals >= 2 and "refuse_second" or "refuse_first") } }
    end
    step, offer = nil, nil
    ctx.coinBox:hide()
    return nil
end

return Landlord
