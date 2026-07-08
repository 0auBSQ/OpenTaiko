---@diagnostic disable: undefined-global, undefined-field, lowercase-global
-- icons.lua — small procedurally-drawn icons (64×64 LuaCanvases) for the edit-mode UI: furniture,
-- flooring/paint swatches, the four category tabs, and the eraser / rotate / remove buttons. Drawn
-- with filled rects (+ a tiny disc/line helper) so the menu has real icons instead of text labels.

local floor, abs, max = math.floor, math.abs, math.max
local I = {}
I.SZ = 64
I.cv = {}

local function rect(c, x, y, w, h, r, g, b, a) c:FillRect(floor(x), floor(y), floor(w), floor(h), r, g, b, a or 255) end
local function pxl(c, x, y, r, g, b, a) c:FillRect(floor(x), floor(y), 1, 1, r, g, b, a or 255) end
local function disc(c, cx, cy, rad, r, g, b, a) for y = -rad, rad do for x = -rad, rad do if x * x + y * y <= rad * rad then pxl(c, cx + x, cy + y, r, g, b, a) end end end end
local function ring(c, cx, cy, rad, th, r, g, b, a) for y = -rad, rad do for x = -rad, rad do local d = x * x + y * y; if d <= rad * rad and d >= (rad - th) * (rad - th) then pxl(c, cx + x, cy + y, r, g, b, a) end end end end
local function line(c, x0, y0, x1, y1, r, g, b, a) local n = max(abs(x1 - x0), abs(y1 - y0)); for i = 0, n do pxl(c, x0 + (x1 - x0) * i / n, y0 + (y1 - y0) * i / n, r, g, b, a) end end

local function newIcon(name, drawFn)
    local c = CANVAS:CreateCanvas(I.SZ, I.SZ); c:Clear(0, 0, 0, 0); drawFn(c); c:Upload(); I.cv[name] = c
end

-- ── bespoke item / tab / button icons ──────────────────────────────────────────────────────────
local function makeFixed()
    newIcon("desk", function(c)
        -- a bare desk: the PC bits moved to the standalone "computer" icon/item
        rect(c, 8, 30, 48, 7, 150, 102, 58); rect(c, 11, 37, 6, 19, 110, 74, 42); rect(c, 47, 37, 6, 19, 110, 74, 42)
    end)
    newIcon("computer", function(c)
        rect(c, 10, 16, 12, 24, 116, 120, 128)                              -- tower
        rect(c, 28, 12, 26, 20, 44, 46, 52); rect(c, 30, 14, 22, 16, 110, 170, 222) -- monitor
        rect(c, 36, 32, 8, 6, 60, 62, 70)                                   -- stand
        rect(c, 26, 42, 30, 6, 70, 72, 80)                                  -- keyboard
    end)
    newIcon("chair", function(c)
        rect(c, 18, 10, 26, 20, 170, 74, 74); rect(c, 16, 30, 30, 8, 188, 86, 86)
        rect(c, 18, 38, 5, 18, 70, 44, 44); rect(c, 41, 38, 5, 18, 70, 44, 44)
    end)
    newIcon("clock", function(c)
        disc(c, 32, 32, 22, 234, 234, 226); ring(c, 32, 32, 22, 3, 60, 60, 70)
        line(c, 32, 32, 32, 18, 40, 40, 50); line(c, 32, 32, 44, 36, 40, 40, 50)
    end)
    newIcon("table", function(c)
        rect(c, 8, 26, 48, 7, 150, 102, 58); rect(c, 12, 33, 6, 22, 110, 74, 42); rect(c, 46, 33, 6, 22, 110, 74, 42)
    end)
    newIcon("sofa", function(c)
        rect(c, 10, 20, 44, 12, 108, 128, 168); rect(c, 8, 32, 48, 16, 128, 148, 188)
        rect(c, 6, 28, 8, 20, 96, 116, 156); rect(c, 50, 28, 8, 20, 96, 116, 156)
        rect(c, 10, 48, 6, 8, 70, 50, 36); rect(c, 48, 48, 6, 8, 70, 50, 36)
    end)
    newIcon("bookshelf", function(c)
        rect(c, 14, 8, 36, 48, 150, 102, 58)
        rect(c, 18, 12, 28, 10, 170, 74, 74); rect(c, 18, 26, 28, 10, 108, 128, 168); rect(c, 18, 40, 28, 10, 96, 168, 88)
    end)
    newIcon("plant", function(c)
        rect(c, 24, 40, 16, 14, 170, 74, 74)
        disc(c, 32, 28, 12, 96, 168, 88); disc(c, 24, 22, 8, 74, 132, 74); disc(c, 40, 22, 8, 74, 132, 74)
    end)
    newIcon("floorlamp", function(c)
        rect(c, 30, 18, 4, 34, 98, 102, 112); rect(c, 22, 52, 20, 4, 98, 102, 112)
        rect(c, 20, 8, 24, 14, 238, 226, 200)
    end)
    newIcon("bed", function(c)
        rect(c, 8, 26, 48, 16, 238, 226, 200); rect(c, 8, 42, 48, 8, 150, 102, 58)
        rect(c, 10, 22, 16, 8, 255, 255, 250); rect(c, 30, 28, 24, 10, 108, 128, 168)
        rect(c, 6, 18, 6, 32, 110, 74, 42)
    end)
    newIcon("tv", function(c)
        rect(c, 10, 12, 44, 28, 44, 46, 52); rect(c, 14, 16, 36, 20, 110, 170, 222)
        rect(c, 22, 42, 20, 8, 150, 102, 58)
    end)
    newIcon("shelf", function(c)
        rect(c, 8, 34, 48, 6, 150, 102, 58)
        rect(c, 14, 18, 10, 16, 170, 74, 74); rect(c, 27, 22, 8, 12, 108, 128, 168); rect(c, 38, 20, 9, 14, 96, 168, 88)
    end)
    newIcon("poster", function(c)
        rect(c, 16, 8, 32, 48, 226, 150, 92); rect(c, 20, 14, 24, 12, 120, 168, 210); rect(c, 20, 40, 24, 6, 255, 255, 250)
    end)
    newIcon("painting", function(c)
        rect(c, 10, 12, 44, 40, 208, 172, 92); rect(c, 15, 17, 34, 30, 168, 190, 150)
        disc(c, 40, 24, 5, 240, 210, 120); rect(c, 18, 34, 18, 10, 96, 148, 88)
    end)
    newIcon("phone", function(c)
        rect(c, 20, 10, 24, 44, 44, 46, 52); rect(c, 24, 18, 16, 24, 116, 120, 128)
        rect(c, 16, 8, 6, 48, 234, 234, 228)
    end)
    -- category tabs
    newIcon("tab_furn", function(c) rect(c, 16, 12, 22, 18, 170, 74, 74); rect(c, 14, 30, 26, 8, 188, 86, 86); rect(c, 16, 38, 5, 16, 70, 44, 44); rect(c, 35, 38, 5, 16, 70, 44, 44) end)
    newIcon("tab_wall", function(c) rect(c, 12, 12, 40, 40, 150, 152, 160); ring(c, 32, 30, 12, 3, 240, 240, 230); disc(c, 32, 30, 9, 234, 234, 226); line(c, 32, 30, 32, 23, 40, 40, 50) end)
    newIcon("tab_floor", function(c) for gy = 0, 3 do for gx = 0, 3 do local d = ((gx + gy) % 2 == 0); rect(c, 8 + gx * 12, 8 + gy * 12, 12, 12, d and 150 or 110, d and 102 or 74, d and 58 or 42) end end end)
    newIcon("tab_paint", function(c) rect(c, 14, 14, 24, 16, 120, 130, 145); rect(c, 38, 18, 12, 8, 90, 96, 108); rect(c, 26, 30, 8, 26, 90, 96, 108); disc(c, 30, 52, 7, 200, 90, 120) end)
    -- buttons
    newIcon("eraser", function(c) rect(c, 14, 30, 34, 18, 230, 130, 160); rect(c, 14, 42, 34, 6, 235, 235, 235); rect(c, 12, 30, 36, 4, 200, 110, 140) end)
    newIcon("rotate", function(c) ring(c, 32, 32, 18, 4, 150, 215, 255); line(c, 46, 24, 50, 34, 150, 215, 255); line(c, 50, 34, 40, 30, 150, 215, 255) end)
    newIcon("move", function(c)
        line(c, 32, 12, 32, 52, 150, 255, 190); line(c, 12, 32, 52, 32, 150, 255, 190)
        line(c, 32, 12, 26, 20, 150, 255, 190); line(c, 32, 12, 38, 20, 150, 255, 190)
        line(c, 32, 52, 26, 44, 150, 255, 190); line(c, 32, 52, 38, 44, 150, 255, 190)
        line(c, 12, 32, 20, 26, 150, 255, 190); line(c, 12, 32, 20, 38, 150, 255, 190)
        line(c, 52, 32, 44, 26, 150, 255, 190); line(c, 52, 32, 44, 38, 150, 255, 190)
    end)
    newIcon("remove", function(c) rect(c, 22, 16, 20, 6, 220, 90, 90); rect(c, 18, 22, 28, 6, 220, 90, 90); rect(c, 22, 28, 20, 26, 200, 80, 80); line(c, 28, 32, 28, 50, 120, 40, 40); line(c, 36, 32, 36, 50, 120, 40, 40) end)
end

-- a flat colour swatch (for flooring / wall-paint items), cached by colour
function I.swatch(key, r, g, b)
    local name = "sw_" .. key
    if not I.cv[name] then
        newIcon(name, function(c)
            rect(c, 6, 6, 52, 52, r * 0.85, g * 0.85, b * 0.85); rect(c, 10, 10, 44, 44, r, g, b)
            rect(c, 10, 10, 44, 4, math.min(255, r + 40), math.min(255, g + 40), math.min(255, b + 40))
        end)
    end
    return I.cv[name]
end

function I.build() makeFixed() end
function I.get(name) return I.cv[name] end

-- draw an icon centred in a box (x,y,w,h)
function I.draw(name, x, y, w, h, opacity)
    local c = type(name) == "string" and I.cv[name] or name
    if not c then return end
    c:SetColor(1, 1, 1); c:SetOpacity(opacity or 1.0)
    c:SetScale(w / I.SZ, h / I.SZ); c:Draw(floor(x), floor(y))
end

return I
