-- 3D floor numbers; the art is greyscale, so SetColor tints the face and the border stays dark.

local Rainbow = require("Rainbow")
local Config  = require("Core.Config")
local Layout  = Config.load("Config/layout.json")
local metrics = Layout.selectui.metrics

local N = {}

local DIR = "Textures/selector/"
local GAP = Layout.selectui.numbers.gap

local _tex = nil

function N.setup()
    if _tex ~= nil then return end
    _tex = {}
    for d = 0, 9 do _tex[d] = TEXTURE:CreateTexture(DIR .. "number/" .. d .. ".png") end
end

function N.dispose()
    if _tex == nil then return end
    for d = 0, 9 do _tex[d]:Dispose() end
    _tex = nil
end

local function digit_at(text, i)
    local d = text:byte(i) - 48
    if d < 0 or d > 9 then return nil end
    return d, metrics.digits[d + 1]
end

-- ink box of the run in unscaled px: its width, and how far it reaches above and below the baseline
local function run_box(text)
    local pen, last, up, down = 0, nil, 0, 0
    for i = 1, #text do
        local _, m = digit_at(text, i)
        if m ~= nil then
            if last ~= nil then pen = pen + (last.ink_r - last.ink_l + GAP) end
            last = m
            up = math.max(up, m.base_y - m.ink_t)
            down = math.max(down, m.ink_b - m.base_y)
        end
    end
    if last == nil then return 0, 0, 0 end
    return pen + (last.ink_r - last.ink_l), up, down
end

function N.width(text, scale)
    return (run_box(text)) * (scale or 1)
end

-- height of the ink box centre above the baseline, unscaled
function N.rise(text)
    local _, up, down = run_box(text)
    return (up - down) * 0.5
end

local function draw_digit(tx, x, y, s, r, g, b, alpha)
    tx:SetScale(s, s)
    tx:SetColor(r, g, b)
    tx:SetOpacity(alpha)
    tx:Draw(x, y)
    tx:SetScale(1, 1)
    tx:SetColor(1, 1, 1)
    tx:SetOpacity(1)
end

-- text: digits only; (cx, cy): centre of the run's ink box, kept in place by the scale.
-- rainbow: Rainbow.draw options for one spectrum across the run (sx, sy, x0, span and opacity
-- filled in here), or nil for the r, g, b tint. Returns the drawn ink box: left, top, width, height.
function N.draw(text, cx, cy, scale, r, g, b, alpha, rainbow)
    if _tex == nil then return cx, cy, 0, 0 end
    local s = scale or 1
    alpha = alpha or 1
    local w, up, down = run_box(text)
    local left = cx - w * s * 0.5
    local base = cy + (up - down) * 0.5 * s
    if rainbow ~= nil then
        rainbow.sx, rainbow.sy = s, s
        rainbow.x0, rainbow.span = left, w * s
        rainbow.opacity = alpha
    end
    local pen, last = 0, nil
    for i = 1, #text do
        local d, m = digit_at(text, i)
        if m ~= nil then
            if last ~= nil then pen = pen + (last.ink_r - last.ink_l + GAP) end
            last = m
            local tx = _tex[d]
            local x, y = left + (pen - m.ink_l) * s, base - m.base_y * s
            if rainbow ~= nil then
                Rainbow.draw(tx, x, y, rainbow)
            elseif tx.Ready then
                draw_digit(tx, x, y, s, r or 1, g or 1, b or 1, alpha)
            end
        end
    end
    return left, base - up * s, w * s, (up + down) * s
end

return N
