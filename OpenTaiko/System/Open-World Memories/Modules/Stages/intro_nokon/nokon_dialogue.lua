---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- nokon_dialogue.lua — intro_nokon's dialogue chrome on top of the shared Lib/dialogue box.
-- Public API kept from the original module (init/show/draw/update/isActive) so game.lua is unchanged:
--   Dialogue.show(blocks, onDone, bgOpacity)   blocks = array of {text=string, onConfirm=function|nil}
-- Presentation: Nokon2 slides in from the right + BgTile darkening stays here; the box itself is the
-- library's PopUI panel (showbiz red velvet + gold trim, name pill "Nokon") with glyph text — fully
-- UTF-8 so the Japanese localization renders correctly. The old Dialogue.png box is deprecated.

local DialogueLib = require("dialogue")   -- resolves to Modules/Lib/dialogue.lua
local I18N        = require("i18n")
local Config      = require("Core.Config")
local Layout      = Config.load("Config/layout.json")
local Animation   = Config.load("Config/animation.json")
local Dlg         = Layout.dialogue

local M = {}

local tx = {}

-- ── Layout constants ──────────────────────────────────────────────────────────
local NOKON_Y     = Dlg.nokon_y
local NOKON_X_IN  = Dlg.nokon_x_in
local NOKON_X_OUT = Dlg.nokon_x_out
local SLIDE_DUR   = Animation.dialogue.slide_s

local BOX = Config.plain(Dlg.box)

local CHARS_PER_SEC = Animation.dialogue.chars_per_s

local SCREEN_W, SCREEN_H = 1920, 1080

-- showbiz theme: red velvet face, gold trim, warm text
local THEME = Config.plain(Dlg.theme)

-- ── State ─────────────────────────────────────────────────────────────────────
local active   = false
local subState = "idle"    -- idle | slide_in | text | slide_out
local blocks, blockIdx, onDone, bgOpacity = {}, 0, nil, 0
local nokonX = NOKON_X_OUT
local slideCounter = nil
local dlg = nil
local gfont, gfontName = nil, nil

local function tileOverlay(opacity)
    local t = tx["bgtile"]
    if t == nil or t.Width == 0 or t.Height == 0 then return end
    local cols = math.ceil(SCREEN_W / t.Width) + 1
    local rows = math.ceil(SCREEN_H / t.Height) + 1
    t:SetOpacity(opacity)
    for c = 0, cols do
        for r = 0, rows do
            t:Draw(c * t.Width, r * t.Height)
        end
    end
    t:SetOpacity(1)
end

local function startBlock(idx)
    if idx > #blocks then
        subState = "slide_out"
        slideCounter = COUNTER:CreateCounterDuration(0, NOKON_X_OUT - NOKON_X_IN, SLIDE_DUR)
        slideCounter:Listen(function(v) nokonX = NOKON_X_IN + math.floor(v) end)
        slideCounter:Start()
        return
    end
    blockIdx = idx
    subState = "text"
    dlg:start({ { name = I18N.tr("Nokon"), text = blocks[idx].text or "" } })
end

-- ── Public API ────────────────────────────────────────────────────────────────

function M.init(t)
    tx = t
    -- single-arg CreateGlyphCached only (NLua params-cache pitfall — see nlua-params-cache-nre)
    gfont     = TEXT:CreateGlyphCached(Dlg.fonts.text)
    gfontName = TEXT:CreateGlyphCached(Dlg.fonts.name)
    dlg = DialogueLib.new({
        gfont = gfont, gfontName = gfontName,
        ui = "popui",
        theme = THEME,
        box = BOX,
        portraitSize = Dlg.portrait_size,
        cps = CHARS_PER_SEC,
        advanceInput = function() return INPUT:Pressed("Decide") end,
    })
end

function M.show(newBlocks, completeCb, opacity)
    blocks    = newBlocks
    onDone    = completeCb
    bgOpacity = opacity or 0
    active    = true
    nokonX    = NOKON_X_OUT
    subState  = "slide_in"
    slideCounter = COUNTER:CreateCounterDuration(0, NOKON_X_OUT - NOKON_X_IN, SLIDE_DUR)
    slideCounter:Listen(function(v) nokonX = NOKON_X_OUT - math.floor(v) end)
    slideCounter:Start()
end

function M.isActive() return active end

function M.draw()
    if not active then return end
    if bgOpacity > 0 then tileOverlay(bgOpacity) end
    if tx["Nokon2"] then tx["Nokon2"]:Draw(nokonX, NOKON_Y) end
    if subState == "text" then
        dlg:draw()
    end
end

function M.update()
    if not active then return end
    if slideCounter then slideCounter:Tick() end

    if subState == "slide_in" then
        if slideCounter and slideCounter.Value >= (NOKON_X_OUT - NOKON_X_IN) then
            slideCounter = nil
            nokonX = NOKON_X_IN
            startBlock(1)
        end
    elseif subState == "text" then
        if dlg:update(1 / 60) == "done" then
            local cb = blocks[blockIdx] and blocks[blockIdx].onConfirm
            if cb then cb() end
            startBlock(blockIdx + 1)
        end
    elseif subState == "slide_out" then
        if slideCounter and slideCounter.Value >= (NOKON_X_OUT - NOKON_X_IN) then
            slideCounter = nil
            nokonX   = NOKON_X_OUT
            active   = false
            subState = "idle"
            if onDone then onDone() end
        end
    end
end

function M.dispose()
    if dlg then dlg:dispose() end
    if gfont then gfont:Dispose(); gfont = nil end
    if gfontName then gfontName:Dispose(); gfontName = nil end
end

return M
