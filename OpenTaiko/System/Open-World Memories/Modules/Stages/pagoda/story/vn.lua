-- Pagoda VN player on VN.Scene; play() runs a scene and reports "proceed" or "abort" through onDone.

local Scene   = require("VN.Scene")
local runner  = require("story/dialog_runner")
local assets  = require("encounter/assets")
local bgdefs  = require("story/backgrounds")
local effects = require("story/vn_effects")
local music   = require("story/vn_music")
local react   = require("story/react")
local Config  = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local TEX = "Textures/story/"

local UmeLayout = Layout.story.vn.ume
local UmeAnim   = Animation.story.vn.ume
local Typing    = Animation.story.vn.typing
local ShakeDir  = Animation.story.vn.shake_direction
local FADE_SEC  = Animation.story.vn.screen_fade_s
local BG_WAIT   = Animation.story.vn.background_wait_s     -- seconds a background may take to load before it shows anyway

local BOX = Config.plain(Layout.story.vn.box)
local Theme = Layout.story.vn.theme
local VN_THEME = {
    face      = Theme.face_color,
    face2     = Theme.face2_color,
    outline   = Theme.outline_color,
    namePill  = Theme.name_pill_color,
    namePill2 = Theme.name_pill2_color,
    nameText  = Theme.name_text_color,
    text      = Theme.text_color,
    choiceRow = Theme.choice_row_color,
    choiceSel = Theme.choice_sel_color,
    choiceText= Theme.choice_text_color,
}

local _tex = nil
local _scene = nil
local _faceCache = {}
local _umeMain = nil
local _sfx = {}                 -- blip / shock / roar
local _lineSound = false        -- the line in the box plays its own sound

-- resources
-- a layer naming an encounter asset draws that handle while the encounter holds it; other textures are disposed with the scene
local function openLayer(def)
    local tx = (def.asset ~= nil) and assets.get(def.asset) or nil
    if tx ~= nil and tx.Loaded then return tx, false end
    tx = TEXTURE:CreateTexture(def.path)
    if tx ~= nil then tx:SetWrapMode("Edge") end
    return tx, true
end

local function face(expr)
    if expr == nil then return nil end
    if _faceCache[expr] == nil then _faceCache[expr] = TEXTURE:CreateTexture(TEX .. "ume/face/" .. expr .. ".png") end
    return _faceCache[expr]
end

local function loadSfx(name)
    local ok, s = pcall(function() return SOUND:CreateSFX("Sounds/" .. name .. ".ogg") end)
    if ok and s ~= nil then return s end
    return nil
end

local function playSfx(name)
    local s = _sfx[name]
    if s ~= nil then s:Play() end
end

-- one blip per speaker, keyed on the node's speaker (line.who) so a masked "???" keeps its voice
local MONSTER_BLIPS = { "blip_monster", "blip_monster2", "blip_monster3" }
local NAMED_BLIPS = { "needle", "thorncushion", "counterplate", "pincerblaze", "startle",
                      "fray", "dragoon", "frangel", "zirr", "tobiraph" }
local function blipFor(node)
    if node == nil or node.speaker == "you" then return "blip_narrator" end
    if node.speaker == "ume" or node.voice == "Ume" then return "blip" end
    -- voices key off the authored name (node.voice), not the localized one
    local name = node.voice or node.name
    if name == nil or name == "" then return "blip_narrator" end
    local voiced = "blip_" .. name:lower()
    if _sfx[voiced] ~= nil then return voiced end
    local h = 0
    for i = 1, #name do h = (h + name:byte(i) * i) % 997 end
    return MONSTER_BLIPS[(h % #MONSTER_BLIPS) + 1]
end

-- the reactions' shake is the scene's own
local function startShake(amp, dur)
    if _scene ~= nil then _scene:shake(amp, dur) end
end

local function onFace(_, old, new)
    local was, now = react.forFace(old), react.forFace(new)
    if now ~= nil and now ~= was then react.start(now, playSfx, startShake, _lineSound) end
end

local function newScene(fonts)
    return Scene.new{
        box = {
            gfont = fonts.gfont, gfontName = fonts.gfontName,
            ui = "popui", theme = VN_THEME, box = BOX,
            portraitSize = 0,                 -- Ume is drawn behind the box instead
            cps = Typing.chars_per_s,
            -- the page sound, not the shared Decide sound
            sfx = { click = function() playSfx("page") end },
            blipEvery = Typing.blip_every_glyphs,
            onSfx = function(name)
                if _sfx[name] ~= nil then playSfx(name)
                else SHARED:GetSharedSound("Move"):Play() end
            end,
        },
        blip = blipFor,
        sound = playSfx,
        cast = {
            ume = {
                name = "Ume", expr = "neu0",
                body = function() return _umeMain end, face = face,
                faceX = UmeLayout.face_x, faceY = UmeLayout.face_y,
                side = UmeLayout.side, y = UmeLayout.y, scale = UmeLayout.scale,
                fade = UmeAnim.fade_s, bounce = UmeAnim.bounce_s, squash = UmeAnim.squash_share,
            },
        },
        backgrounds = {
            defs = bgdefs, path = TEX .. "bg/%s.png", open = openLayer, wait = BG_WAIT,
            -- effects load first: the layer borrows the handle of their art key
            prepare = function(def) effects.prepare(def) end,
            show = function(def) effects.show(def) end,
            update = function(dt) effects.update(dt) end,
            drawBehind = function(ox, oy) effects.drawBehind(ox, oy) end,
            drawOver = function(ox, oy) effects.draw(ox, oy) end,
            close = function() effects.close() end,
        },
        music = {
            play = function(key) music.play(key) end,
            fadeOut = function(sec) music.fadeOut(sec) end,
            playing = function() return music.playing() end,
            update = function(dt) music.update(dt) end,
            stop = function() music.stop() end,
        },
        cover = function() return _tex.black1 end,
        fadeSec = FADE_SEC,
        outcome = "proceed",
        tr = runner.tr,
        -- who = "you" speaks under the player's name; the cast under theirs
        speakerName = function(who, _, ctx)
            if who == "you" then return (ctx ~= nil and ctx.playerName) or "You" end
            if who == "ume" then return "Ume" end
            return who
        end,
        directions = {
            shake = function() GLOBALCAMERA:Shake(ShakeDir.amplitude_px, ShakeDir.duration_s) end,
        },
        hooks = {
            prose = function(line) _lineSound = react.hasSound(line) end,
            start = function(line)
                _lineSound = react.hasSound(line)
                if line.react ~= nil then react.start(line.react, playSfx, startShake, _lineSound) end
            end,
            expr = onFace,
            update = function(dt) react.update(dt) end,
            draw = function() react.draw() end,
            dispose = function() react.dispose() end,
        },
    }
end

-- called once by Script.lua; fonts = { gfont, gfontName } (LuaGlyphText)
function M.setup(tex, fonts)
    _tex = tex
    -- created here, not on first draw: a lazy create could show the body frames before the face
    if _umeMain == nil then _umeMain = TEXTURE:CreateTexture(TEX .. "ume/main.png") end
    face("neu0")
    for _, n in ipairs({ "blip", "blip_narrator", "blip_monster", "blip_monster2",
                         "blip_monster3", "page", "shock", "roar" }) do
        if _sfx[n] == nil then _sfx[n] = loadSfx(n) end
    end
    for _, n in ipairs(react.SOUNDS) do
        if _sfx[n] == nil then _sfx[n] = loadSfx(n) end
    end
    react.setup(tex)
    for _, n in ipairs(NAMED_BLIPS) do
        if _sfx["blip_" .. n] == nil then _sfx["blip_" .. n] = loadSfx("blip_" .. n) end
    end
    _scene = newScene(fonts)
end

function M.active() return _scene ~= nil and _scene:active() end

-- returns grey (0..1) and visible for the drawn background; nil = the stage default
function M.sky()
    if not M.active() then return nil end
    local bg = _scene:shownBackground()
    if bg == nil or bg.def.sky == nil then return nil end
    return bg.def.sky, true
end

function M.reset()
    if _scene ~= nil then _scene:reset() end
end

function M.dispose()
    if _scene ~= nil then _scene:dispose() end
    for _, t in pairs(_faceCache) do if t ~= nil then t:Dispose() end end
    _faceCache = {}
    if _umeMain ~= nil then _umeMain:Dispose() ; _umeMain = nil end
end

-- ctx carries startLevel / highest / playerName in, and outcome out.
function M.play(scene, ctx, onDone)
    _scene:play(scene, ctx, onDone)
end

function M.update(dt)
    if _scene == nil then return "done" end
    return _scene:update(dt)
end

function M.draw()
    if _scene ~= nil then _scene:draw() end
end

return M
