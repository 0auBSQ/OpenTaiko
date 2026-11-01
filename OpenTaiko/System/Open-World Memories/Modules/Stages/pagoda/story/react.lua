-- Dialog reactions ("surprise", "shock"): a screen flash, a sting, and a shake for "shock".

local Config = require("Core.Config")
local Animation = Config.load("Config/animation.json")

local M = {}

local Flash = Animation.story.react
local MIN_OPACITY = Flash.min_visible_opacity
local KINDS = {
    surprise = { a = Flash.surprise.peak_opacity, sec = Flash.surprise.duration_s, sfx = "surprise" },
    shock    = { a = Flash.shock.peak_opacity, sec = Flash.shock.duration_s, sfx = "shock",
                 shake = { Flash.shock.shake_amplitude_px, Flash.shock.shake_s } },
}
-- face / pose -> reaction when Ume turns to it
local FACES = { shock0 = "surprise", shock1 = "surprise", fear0 = "shock", fear2 = "shock", fear3 = "shock" }
local POSES = { scared = "shock" }

M.SOUNDS = { "surprise", "shock" }     -- Sounds/<name>.ogg the callers load

local _white = nil                    -- Script.lua's tex.flash
local _t, _sec, _a = 0, 0, 0

-- tex = the stage's shared textures; without them the flash already set stays
function M.setup(tex)
    if tex ~= nil and tex.flash ~= nil then _white = tex.flash end
end

function M.dispose()
    _t, _sec = 0, 0
end

-- reaction for turning to a face / pose (nil: none)
function M.forFace(expr) return expr and FACES[expr] or nil end
function M.forPose(pose) return pose and POSES[pose] or nil end

-- play(name) plays a sound, shake(amp, sec) shakes the screen; quiet = the line has its own sound
function M.start(kind, play, shake, quiet)
    local k = KINDS[kind]
    if k == nil then return end
    _t, _sec, _a = 0, k.sec, k.a
    if not quiet and play ~= nil then play(k.sfx) end
    if k.shake ~= nil and shake ~= nil then shake(k.shake[1], k.shake[2]) end
end

-- true when a line's text plays a sound of its own
function M.hasSound(line)
    return line ~= nil and type(line.text) == "string" and line.text:find("{sfx:", 1, true) ~= nil
end

function M.update(dt)
    if _t < _sec then _t = _t + dt end
end

function M.draw()
    if _t >= _sec or _white == nil or not _white.Ready then return end
    local u = _t / _sec
    local a = _a * (1 - u) * (1 - u)
    if a <= MIN_OPACITY then return end
    local res = THEME:GetResolution()
    _white:SetScale(res.X / math.max(1, _white.Width), res.Y / math.max(1, _white.Height))
    _white:SetOpacity(a)
    _white:Draw(0, 0)
    _white:SetOpacity(1) ; _white:SetScale(1, 1)
end

return M
