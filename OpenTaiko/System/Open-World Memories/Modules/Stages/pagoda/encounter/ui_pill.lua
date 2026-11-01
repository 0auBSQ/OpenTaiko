-- The encounter menu's answer pill, shared by every pagoda menu.

local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local Look = Layout.encounter.ui_pill
local Anim = Animation.encounter.ui_pill

local M = {}

M.BASE_W = Look.base_w                          -- the encounter button's width, and its label's

local SEL_TINT  = Look.selected_tint
local DIM_TINT  = Look.dim_tint
local DIM_ALPHA = Look.dim_opacity
local BOUNCE_AMP, BOUNCE_SPEED = Anim.bounce_amp, Anim.bounce_freq
local GLOW_SX, GLOW_SY, GLOW_A = Look.glow_scale_x, Look.glow_scale_y, Look.glow_opacity  -- glow scale per BASE_W of pill width
local MARKER_GAP, MARKER_BOB, MARKER_SPEED = Look.marker_gap_px, Anim.marker_bob_px, Anim.marker_freq
local MARKER_ROT = Look.marker_above_rot_deg
local LABEL_GOLD = Look.label_gold_color
local LABEL_GREY = Look.label_grey_color

local _gold, _grey = nil, nil

local function ready(tx) return tx ~= nil and tx.Ready end

local function reset(tx)
    tx:SetScale(1, 1) ; tx:SetRotation(0) ; tx:SetOpacity(1)
    tx:SetColor(1, 1, 1) ; tx:SetBlendMode("Normal")
end

-- the label colour as a COLOR, for text drawn by a font
function M.labelColor(selected)
    if _gold == nil then
        _gold = Config.color(LABEL_GOLD)
        _grey = Config.color(LABEL_GREY)
    end
    return selected and _gold or _grey
end

-- the label colour as a tint, for a white label texture
function M.labelTint(selected)
    local c = selected and LABEL_GOLD or LABEL_GREY
    return c[1] / 255, c[2] / 255, c[3] / 255
end

-- bounced scale at time t, for something that is not a pill (an item icon)
function M.bounce(scale, t)
    return scale * (1 + BOUNCE_AMP * math.sin(t * BOUNCE_SPEED))
end

-- centred on (x, y) at texture scale (sx, sy)
function M.glowAt(glow, x, y, sx, sy, alpha)
    if not ready(glow) then return end
    glow:SetBlendMode("Add")
    glow:SetColor(SEL_TINT[1], SEL_TINT[2], SEL_TINT[3])
    glow:SetScale(sx, sy)
    glow:SetOpacity(GLOW_A * (alpha or 1))
    glow:DrawAtAnchor(x, y, "center")
    reset(glow)
end

-- the marker pointing down at (x, y) from above
function M.markerAbove(marker, x, y, scale, t, alpha)
    if not ready(marker) then return end
    local bob = MARKER_BOB * (0.5 + 0.5 * math.sin(t * MARKER_SPEED))
    marker:SetColor(SEL_TINT[1], SEL_TINT[2], SEL_TINT[3])
    marker:SetScale(scale, scale)
    marker:SetRotation(MARKER_ROT)
    marker:SetOpacity(alpha or 1)
    marker:DrawAtAnchor(x, y - MARKER_GAP - marker.Width * scale / 2 - bob, "center")
    reset(marker)
end

-- centred on (x, y) at base scale `scale`; t = seconds; alpha default 1; tex = { glow, marker, button };
-- body(s, r, g, b, a) draws the pill in place of tex.button (w px wide at scale 1); returns the bounced scale
function M.draw(tex, x, y, scale, selected, t, alpha, w, body)
    alpha = alpha or 1
    local button, glow, marker = tex.button, tex.glow, tex.marker
    if body == nil then w = ready(button) and button.Width or M.BASE_W end
    local s = scale
    if selected then s = scale * (1 + BOUNCE_AMP * math.sin(t * BOUNCE_SPEED)) end
    if selected and ready(glow) then
        glow:SetBlendMode("Add")
        glow:SetColor(SEL_TINT[1], SEL_TINT[2], SEL_TINT[3])
        glow:SetScale(GLOW_SX * scale * w / M.BASE_W, GLOW_SY * scale)
        glow:SetOpacity(GLOW_A * alpha)
        glow:DrawAtAnchor(x, y, "center")
        reset(glow)
    end
    local c = selected and SEL_TINT or DIM_TINT
    local a = (selected and 1 or DIM_ALPHA) * alpha
    if body ~= nil then
        body(s, c[1], c[2], c[3], a)
    elseif ready(button) then
        button:SetColor(c[1], c[2], c[3])
        button:SetOpacity(a)
        button:SetScale(s, s)
        button:DrawAtAnchor(x, y, "center")
        reset(button)
    end
    if selected and ready(marker) then
        local bob = MARKER_BOB * (0.5 + 0.5 * math.sin(t * MARKER_SPEED))
        marker:SetColor(SEL_TINT[1], SEL_TINT[2], SEL_TINT[3])
        marker:SetScale(scale, scale)
        marker:SetOpacity(alpha)
        marker:DrawAtAnchor(x - w * s / 2 - MARKER_GAP - marker.Width * scale / 2 - bob, y, "center")
        reset(marker)
    end
    return s
end

return M
