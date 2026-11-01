-- Ume's items on the encounter screen (rules: encounter/items/state); driven by floor_enter.

local assets     = require("encounter/assets")
local data       = require("encounter/items/data")
local texts      = require("encounter/items/texts")
local state      = require("encounter/items/state")
local icons      = require("encounter/items/icons")
local tooltip    = require("encounter/items/tooltip")
local intro      = require("encounter/items/intro")
local popup      = require("encounter/items/popup")
local charm      = require("encounter/items/charm")
local tambourine = require("encounter/items/tambourine")
local songbars   = require("encounter/songbars")
local Config     = require("Core.Config")
local Animation = Config.load("Config/animation.json")

local M = {}

assets.add(data)

local EFFECTS = { charm = charm, tambourine = tambourine }
local SFX = { refill = "Sounds/result/item_refill.ogg", new = "Sounds/result/item_get.ogg" }
local NEW_HOLD = Animation.encounter.items.init.new_hold_s  -- s of the new icon burst before the song bars come in

local _black = nil
local _open = false
local _ctx = {}
local _lineHooks = {}
local _present = {}                             -- ids presented on this visit
local _new = nil                                -- the presented ids, once the presentation is over
local _hold = 0
local _effect = nil
local _tails = {}                               -- effects done with the menu, still fading out
local _sfx = {}

-- tex: the stage's resident textures; fonts: the stage's glyph fonts
function M.setup(tex, fonts)
    _black = tex ~= nil and tex.black1 or nil
    tooltip.setup(fonts)
    popup.setup(fonts)
end

local function has(list, id)
    for _, v in ipairs(list) do if v == id then return true end end
    return false
end

-- encounter/assets keys of a visit; none when there is no item
function M.keys(level, ctx)
    local owned, present = state.owned(), state.toPresent()
    local all = {}
    for _, id in ipairs(owned) do all[#all + 1] = id end
    for _, id in ipairs(present) do all[#all + 1] = id end
    if #all == 0 then return {} end
    local keys, seen = {}, {}
    local function add(list)
        for _, k in ipairs(list) do
            if not seen[k] then seen[k] = true ; keys[#keys + 1] = k end
        end
    end
    add(icons.keys(all))
    add(tooltip.keys())
    if has(all, "charm") then add(charm.keys()) end
    if has(all, "tambourine") then add(tambourine.keys()) end
    for _, id in ipairs(present) do add(popup.keys(id)) end
    return keys
end

local function play(name)
    local s = _sfx[name]
    if s ~= nil then s:Play() end
end

-- ctx: the visit's context (ctx.reroll(keepMystery) -> a new roll of the floor's songs);
-- hooks = { lines = dialog hooks for Ume's lines (encounter/script), shake = fn(px, sec) }.
-- The keys are opened in encounter/assets.
function M.open(level, ctx, hooks)
    M.close()
    _ctx, hooks = ctx or {}, hooks or {}
    _present = state.toPresent()
    local refilled = state.replenish(level)
    local owned = state.owned()
    if #owned == 0 and #_present == 0 then return end
    _open = true
    _new, _hold = nil, 0
    icons.open(owned, state.ready, refilled)
    tooltip.open(texts, (assets.get("items_tip")))
    if has(owned, "charm") or has(_present, "charm") then charm.open() end
    if has(owned, "tambourine") or has(_present, "tambourine") then tambourine.open(level, _ctx.reroll, hooks.shake) end
    for n, p in pairs(SFX) do
        local ok, s = pcall(function() return SOUND:CreateSFX(p) end)
        if ok then _sfx[n] = s end
    end
    _lineHooks = hooks.lines or {}
end

function M.introPending() return _open and #_present > 0 end

function M.startIntro()
    intro.start(_present, texts, _ctx, _lineHooks)
end

-- canAdvance as for encounter/script.update; returns "running", then "done" once the new icons are in
function M.updateIntro(dt, nav, canAdvance)
    local id = intro.update(dt, nav, canAdvance)
    if id ~= nil then
        state.present(id)
        icons.add(id)
        _new = _new or {}
        _new[id] = true
    end
    if intro.active() then return "running" end
    if not icons.shown() then
        icons.show(_new)
        play("new")
    end
    _hold = _hold + dt
    if _hold >= NEW_HOLD then return "done" end
    return "running"
end

-- true while Ume's lines are up
function M.talking() return intro.talking() end

-- shows the icons, with the song bars
function M.show()
    if _open then icons.show(_new) end
end

-- the items the menu can focus, left to right
function M.slots()
    if not _open then return {} end
    return state.owned()
end

local function usable(id)
    if not state.ready(id) or not songbars.settled() then return false end
    if id == "charm" then return songbars.anyHidden() end
    if id == "tambourine" then return _ctx.reroll ~= nil end
    return false
end

-- true while an item's effect holds the menu
function M.busy()
    return _effect ~= nil and _effect.busy()
end

-- true when its effect starts; an earlier effect no longer holding the menu fades out on its own
function M.use(id)
    if M.busy() or not usable(id) then
        SHARED:GetSharedSound("Error"):Play()
        icons.deny(id)
        return false
    end
    SHARED:GetSharedSound("Decide"):Play()
    state.spend(id)
    icons.spend(id)
    if _effect ~= nil and _effect ~= EFFECTS[id] then _tails[#_tails + 1] = _effect end
    _effect = EFFECTS[id]
    _effect.start()
    return true
end

-- focus: the focused item's id or nil
function M.update(dt, focus)
    if not _open then return end
    icons.update(dt, function() play("refill") end)
    tooltip.update(dt, focus)
    for i = #_tails, 1, -1 do
        local e = _tails[i]
        e.update(dt)
        if not e.active() then table.remove(_tails, i) end
    end
    if _effect ~= nil then
        _effect.update(dt)
        if not _effect.active() then _effect = nil end
    end
end

-- drawn under the song bars
function M.drawUnder()
    for _, e in ipairs(_tails) do
        if e == charm then charm.drawUnder(_black) end
    end
    if _effect == charm then charm.drawUnder(_black) end
end

-- drawn over the song bars
function M.drawOver()
    for _, e in ipairs(_tails) do e.drawOver() end
    if _effect ~= nil then _effect.drawOver() end
end

function M.drawIcons(focus)
    if not _open then return end
    icons.draw(focus)
    tooltip.draw()
end

function M.drawPopup()
    intro.draw(_black)
end

function M.close()
    intro.close()
    icons.close()
    tooltip.close()
    charm.close()
    tambourine.close()
    for _, s in pairs(_sfx) do s:Stop() ; s:Dispose() end
    _sfx = {}
    _open, _effect, _tails, _present, _new, _ctx, _lineHooks = false, nil, {}, {}, nil, {}, {}
end

function M.dispose()
    M.close()
    popup.dispose()
end

return M
