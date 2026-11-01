-- Ume presents this play's new items: his lines (encounter/script), then encounter/items/popup.

local script = require("encounter/script")
local cast   = require("encounter/cast")
local popup  = require("encounter/items/popup")

local M = {}

local CALM_FROM, CALM_TO = 11, 15               -- floors where Ume keeps his normal pose

local _queue = {}
local _texts, _ctx, _hooks = {}, {}, {}
local _step = "idle"                            -- idle | talk | popup
local _id = nil

-- the item's lines (none where Ume is absent); CALM_FROM..CALM_TO keep the normal pose
local function linesOf(t)
    local lines = t.present or {}
    local lv = _ctx.startLevel or 0
    if not cast.umeOn(lv) then return {} end
    if lv < CALM_FROM or lv > CALM_TO then return lines end
    local out = {}
    for i, l in ipairs(lines) do
        if l.sprite == "brave" then
            local c = {}
            for k, v in pairs(l) do c[k] = v end
            c.sprite = "normal"
            out[i] = c
        else
            out[i] = l
        end
    end
    return out
end

local function nextItem()
    _id = table.remove(_queue, 1)
    if _id == nil then _step = "idle" ; return end
    local t = _texts[_id] or {}
    if script.play(linesOf(t), _hooks, _ctx) == "running" then
        _step = "talk"
    else
        _step = "popup"
        popup.open(_id, t)
    end
end

-- ids: items to present, in order; texts: encounter/items/texts; ctx, hooks as for encounter/script
function M.start(ids, texts, ctx, hooks)
    _queue = {}
    for i, id in ipairs(ids or {}) do _queue[i] = id end
    _texts, _ctx, _hooks = texts or {}, ctx or {}, hooks or {}
    nextItem()
end

function M.active() return _step ~= "idle" end

-- true while Ume's lines are up
function M.talking() return _step == "talk" end

-- returns the id of an item whose popup just closed (once), or nil
function M.update(dt, nav, canAdvance)
    if _step == "talk" then
        local st = script.update(dt, canAdvance)
        if st ~= "running" then
            _step = "popup"
            popup.open(_id, _texts[_id])
        end
        return nil
    end
    if _step == "popup" and popup.update(dt, nav) then
        local done = _id
        popup.close()
        nextItem()
        return done
    end
    return nil
end

function M.draw(black)
    if _step == "popup" then popup.draw(black) end
end

function M.close()
    if _step == "popup" then popup.close() end
    _queue, _step, _id = {}, "idle", nil
end

return M
