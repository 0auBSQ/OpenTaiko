---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- Stages.CustomStages — the custom stages: every stage that declares the namespace "CStage" with
-- STAGES:SetNamespace, a list of them a menu can open, and the way back a custom stage follows.
--
--   in a custom stage, at the top of Script.lua:
--     local CS = require("Stages.CustomStages")
--     CS.declare{ title = CS.allLanguages("mystage", "title"), description = "..." }
--     function activate() back, backTransition = CS.takeReturn() end      -- "_title" when none was set
--     ... return Exit("stage", back, backTransition)
--   in a menu stage:
--     CS.open{ title = "Custom Stages" }      -- then CS.update(ts) -> nil | "cancel" | entry; CS.draw()
--     return CS.enter(entry.name, "_title")    -- the way back: "<stage>" or "<stage>|<transition>"

local HERE = require("Core.Here")(...)
local I18N = require("Locale.i18n")
local Pick = require("UI.Pick")

local T = I18N.texts("customstages", HERE)

local CS = {}

CS.NAMESPACE = "CStage"
CS.RETURN_KEY = "custom_stage_return"
CS.LANGUAGES = { "en", "de", "es", "fr", "ja", "ko", "nl", "ru", "zh" }

local FALLBACK = {
    title = "Custom Stages",
    empty = "No custom stages are installed yet.",
    empty_hint = "Put a stage folder in Global/Modules/Stages, then restart the game.",
    ok = "OK",
}

function CS.text(id)
    local code = I18N.currentCode()
    if I18N.code ~= code then I18N.load(code) end
    return T:get(id) or FALLBACK[id] or id
end

-- ── the namespace ───────────────────────────────────────────────────────────────────────────────

-- declares the calling stage a custom stage; info is what the list shows: title and description, each a
-- string or a table by language code ({ en = "...", ja = "..." })
function CS.declare(info)
    return STAGES:SetNamespace(CS.NAMESPACE, info or {})
end

-- one string of the calling stage's lang/<code>/<name>.json in every language that has it, as a table by
-- language code (for declare, which runs before any language is chosen)
function CS.allLanguages(name, id)
    local out = {}
    for _, code in ipairs(CS.LANGUAGES) do
        pcall(function()
            local doc = JSONLOADER:JsonParseFileAny("lang/" .. code .. "/" .. name .. ".json")
            local s = doc ~= nil and JSONLOADER:JsonGet(doc, id) or nil
            if type(s) == "string" and s ~= "" then out[code] = s end
        end)
    end
    return out
end

-- a declared text: a string as it is, a table by language code in the game's language, else English,
-- else the first language that has it
function CS.localized(v, code)
    if type(v) == "string" then return v ~= "" and v or nil end
    if type(v) ~= "table" then return nil end
    code = code or I18N.currentCode()
    local s = v[code]
    if type(s) == "string" and s ~= "" then return s end
    for _, c in ipairs(CS.LANGUAGES) do
        s = v[c]
        if type(s) == "string" and s ~= "" then return s end
    end
    for _, s2 in pairs(v) do
        if type(s2) == "string" and s2 ~= "" then return s2 end
    end
    return nil
end

-- the custom stages the calling module reaches, by title: { { name, isGlobal, title, description, info } }
function CS.list(code)
    local out = {}
    local ok, l = pcall(function() return STAGES:ListNamespace(CS.NAMESPACE) end)
    if not ok or type(l) ~= "table" then return out end
    for _, e in ipairs(l) do
        local info = type(e.info) == "table" and e.info or {}
        out[#out + 1] = { name = e.name, isGlobal = e.isGlobal, info = info,
                          title = CS.localized(info.title, code) or e.name,
                          description = CS.localized(info.description, code) }
    end
    table.sort(out, function(a, b)
        local ta, tb = a.title:lower(), b.title:lower()
        if ta ~= tb then return ta < tb end
        return a.name < b.name
    end)
    return out
end

-- ── the way back ────────────────────────────────────────────────────────────────────────────────

-- leaves for a custom stage (from a stage's update: return CS.enter(...)); back is where it goes back to,
-- "<stage>" or "<stage>|<transition>"
function CS.enter(name, back, transition)
    pcall(function() SHARED:SetSharedString(CS.RETURN_KEY, back or "") end)
    return Exit("stage", name, transition)
end

-- reads and clears the way back (call it on entry): the stage ("_title" when none was set) and the
-- transition (nil for the default)
function CS.takeReturn()
    local ok, s = pcall(function() return SHARED:GetSharedString(CS.RETURN_KEY) end)
    pcall(function() SHARED:SetSharedString(CS.RETURN_KEY, "") end)
    s = (ok and type(s) == "string") and s or ""
    local stage, transition = s:match("^([^|]*)|?(.*)$")
    return (stage ~= nil and stage ~= "") and stage or "_title",
           (transition ~= nil and transition ~= "") and transition or nil
end

-- ── the list ────────────────────────────────────────────────────────────────────────────────────

local ROW, PANEL_W, VISIBLE = 84, 1100, 7
local DESC_SIZE, DESC_LINES = 24, 2

local ui, menu = nil, nil
local entries = {}
local fresh, picked, closing = false, nil, false
local descY, descW = 0, 0

function CS.isOpen() return ui ~= nil end

function CS.close()
    if ui then pcall(function() ui:disposeWidgets() end) end
    ui, menu, entries, picked, closing = nil, nil, {}, nil, false
end

-- o: title, ui (a UI library table or its require name; default UI.Pick), theme, sfx,
-- navPlayer (as NavInput.getPn takes it; default 0, the keyboard and the pad without the drums)
function CS.open(o)
    o = o or {}
    CS.close()
    entries = CS.list()
    local lib = type(o.ui) == "table" and o.ui or Pick(o.ui)
    ui = lib.new{ theme = o.theme, sfx = o.sfx, navPlayer = o.navPlayer or 0 }
    local title = o.title or CS.text("title")
    if #entries == 0 then
        local w, h = 1000, 330
        local x, y = (1920 - w) / 2, (1080 - h) / 2
        local panel = ui:panel{ x = x, y = y, w = w, h = h, title = title }
        local cx, cy, cw = panel:content()
        ui:label{ x = cx + cw / 2, y = cy + 20, text = CS.text("empty"), size = 30, align = "center", maxWidth = cw }
        ui:label{ x = cx + cw / 2, y = cy + 76, text = CS.text("empty_hint"), size = 22, align = "center", maxWidth = cw }
        local okb = ui:button{ text = CS.text("ok"), x = cx + cw / 2 - 120, y = cy + 140, w = 240, h = 76, accent = true,
                               onClick = function() closing = true end }
        ui:focus(okb)
    else
        local items = {}
        for i, e in ipairs(entries) do items[i] = { text = e.title, value = i } end
        local mh = math.min(#items, VISIBLE) * ROW
        local h = 110 + mh + 30 + DESC_LINES * 34 + 30
        local x, y = (1920 - PANEL_W) / 2, math.floor((1080 - h) / 2)
        ui:panel{ x = x, y = y, w = PANEL_W, h = h, title = title }
        menu = ui:menu{ x = x + 36, y = y + 100, w = PANEL_W - 72, h = mh, rowHeight = ROW, items = items,
                        scrollbar = #items > VISIBLE, onSelect = function(_, it) picked = entries[it.value] end }
        descY, descW = y + 100 + mh + 30, PANEL_W - 96
        ui:_setFocusIndex(1)
    end
    fresh, picked, closing = true, nil, false
end

-- nil while the list is up, "cancel", or the chosen entry (the list is closed in both cases)
function CS.update(ts)
    if ui == nil then return nil end
    if fresh then fresh = false; return nil end
    local r = ui:update(ts)
    if picked ~= nil then
        local e = picked
        CS.close()
        return e
    end
    if r == "cancel" or closing then CS.close(); return "cancel" end
    return nil
end

function CS.draw()
    if ui == nil then return end
    ui:draw()
    if menu == nil then return end
    local e = entries[menu.selected]
    if e == nil or e.description == nil then return end
    local c = ui.theme.colors
    local bg = ui.theme.textOutline == false and { 255, 255, 255, 0 } or { 255, 255, 255, 150 }
    local lines = ui:wrapLines(DESC_SIZE, e.description, descW)
    for i = 1, math.min(#lines, DESC_LINES) do
        ui:drawTextEx(DESC_SIZE, lines[i], 960, descY + (i - 1) * 34, c.text, bg, 1, 1, descW, "top")
    end
end

return CS
