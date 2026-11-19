---@diagnostic disable: undefined-global, undefined-field
-- results_regular background, by player count and clear state.

local Bg = {}

local DIR = "Textures/Background/"
local AI_DIR = "Textures/AIBattle/"
local W, H = 1920, 1080
local NOKON_X, NOKON_Y = 1292, 532
local NOKON_TAIL_X, NOKON_TAIL_Y = 1840, 970
local NOKON_FPS = 14
local NOKON_LANGS = { ja = true, zh = true, fr = true }

local RED, YELLOW, GREEN, BLUE = { 255, 122, 162 }, { 255, 224, 122 }, { 179, 255, 202 }, { 122, 220, 255 }

-- { x, y, scale, rotation, speed, color }
local CONFETTI_1 = {
    { 190, 720, 1, 67, 1, RED }, { 380, 165, 0.9, 300, 1.23, YELLOW }, { 570, 989, 0.6, 40, -0.876, BLUE },
    { 760, 455, 0.9, 90, 1.25, GREEN }, { 950, 699, 0.5, 150, -1.06, YELLOW }, { 1140, 1, 0.8, 0, 1.43, GREEN },
    { 1330, 345, 0.4, 88, 1.123, RED }, { 1520, 960, 0.7, 888, -0.888, BLUE },
}
local CONFETTI_2 = {
    { 0, 280, 0.5, 256, 1.57, BLUE }, { 320, 500, 0.67, 0, -1.2, GREEN }, { 640, 1, 0.4, 150, 0.75, YELLOW },
    { 960, 120, 0.6, 80, 0.88, RED }, { 1280, 600, 0.5, 300, 1.1, YELLOW }, { 1600, 360, 0.66, 100, 0.81, GREEN },
    { 1920, 888, 0.888, 88, -0.888, BLUE },
}
-- { x, y, scale, rotation, speed }
local PARTICLE_1 = {
    { 150, 600, 0.8, 200, 1.1 }, { 573, 240, 0.88, 500, -1.6 }, { 888, 980, 0.6, 69, 1.2 },
    { 1000, 500, 0.9, 400, 0.8 }, { 1400, 100, 0.7, 800, 1.0 }, { 1700, 888, 0.888, 420, -0.888 },
}
local PARTICLE_2 = {
    { 400, 888, 0.75, 40, -1.1 }, { 700, 420, 0.81, 120, 0.9 }, { 1200, 700, 0.8, 200, -1.3 },
    { 1600, 246, 0.6, 88, -0.81 }, { 1850, 573, 0.7, 270, 1.2 },
}
-- { x, y, first ms }, every 10 s
local DROPS = { { 200, 300, 3000 }, { 1700, 400, 6000 }, { 1000, 900, 9000 } }

local tx = {}
local style = 0                 -- 0 1P, 1 1P on the 2P side, 2-5 player count
local cleared = {}
local players = 1
local ai, aiWin = false, false
local nokon = { style = 0, t = 0, result = 0 }
local c = {}                    -- the moving parts, in px or frames
local fade = 0                  -- 0 to 255, from the clear on

-- sky, clouds and frame sets by side: 1 the 1P side, 2 the 2P side, 3 three to five players
local SETS = {
    { "Sky_1P", "Clouds_1P", "Clouds_Small_1P", "Frame_1P" },
    { "Sky_2P", "Clouds_2P", "Clouds_Small_2P", "Frame_2P" },
    { "Sky_4P", "Clouds_4P", "Clouds_Small_4P" },
}

local function rect(t, x, y, rx, ry, rw, rh)
    if t ~= nil then t:DrawRect(x, y, rx, ry, rw, rh) end
end

local function loadSet(load, n)
    local s = {}
    for i, name in ipairs(SETS[n]) do s[i] = load(DIR .. name .. ".png") end
    tx["set" .. n] = s
end

function Bg.load(info, load)
    tx, c, fade = {}, {}, 0
    ai = info.IsAIBattle
    if ai then
        aiWin = info:IsCleared(0)
        tx.win = load(AI_DIR .. "Background_Win.png")
        tx.lose = load(AI_DIR .. "Background_Lose.png")
        tx.gradation = load(AI_DIR .. "Background_Gradation.png")
        return
    end
    players = math.max(1, info.PlayerCount)
    cleared = {}
    for p = 0, players - 1 do cleared[p] = info:IsCleared(p) end
    if players >= 3 then
        style = players
        loadSet(load, 3)
    elseif players == 2 then
        style = 2
        loadSet(load, 1)
        loadSet(load, 2)
    else
        style = (info.PlayerSide == 1) and 1 or 0
        loadSet(load, style + 1)
    end
    tx.confetti1 = load(DIR .. "Clear/Confetti_1.png")
    tx.confetti2 = load(DIR .. "Clear/Confetti_2.png")
    tx.skyFail = load(DIR .. "Sky_Fail.png")
    tx.fail = {}
    for i, name in ipairs({ "Rain_1", "Rain_2", "Storm_1", "Storm_2", "Storm_3", "Storm_4" }) do
        tx.fail[i] = load(DIR .. "Fail/" .. name .. ".png")
    end
    tx.particle1 = load(DIR .. "Particle_1.png")
    tx.particle2 = load(DIR .. "Particle_2.png")
    tx.drop = load(DIR .. "Drop.png")
    nokon = { style = 0, t = 0, result = 0 }
    if style == 3 or style == 5 then
        local lang = "en"
        pcall(function() lang = LANG:GetLanguageId() end)
        if not NOKON_LANGS[lang] then lang = "en" end
        local count = 0
        for p = 0, players - 1 do if cleared[p] then count = count + 1 end end
        -- 3 all cleared, 2 two or more, 1 fewer
        local folder, frames
        if count == players then nokon.style, folder, frames = 3, "Result_Great", 2
        elseif count >= 2 then nokon.style, folder, frames = 2, "Result_Good", 5
        else nokon.style, folder, frames = 1, "Result_Bad", 5 end
        nokon.idle, nokon.react = {}, {}
        for i = 0, 5 do nokon.idle[i] = load(DIR .. "Nokon/Idle/" .. lang .. "/" .. i .. ".png") end
        for i = 0, frames - 1 do nokon.react[i] = load(DIR .. "Nokon/" .. folder .. "/" .. lang .. "/" .. i .. ".png") end
        nokon.tail = load(DIR .. "Nokon/Tail.png")
    end
    for _, k in ipairs({ "confetti", "cloud", "rain1", "rain2", "storm" }) do c[k] = 0 end
end

-- dt in seconds; from clearAt ms on, the clear's layer fades in and Nokon reacts
function Bg.update(dt, clock, clearAt)
    if ai then return end
    c.confetti = c.confetti + dt * 108
    c.cloud = c.cloud + dt * 50
    c.rain1 = c.rain1 + dt * 550
    c.rain2 = c.rain2 + dt * 405
    c.storm = c.storm + dt * 50
    nokon.t = nokon.t + dt * NOKON_FPS
    if clock >= clearAt then
        fade = math.min(255, fade + dt * 510)
        nokon.result = nokon.result + dt * NOKON_FPS
    end
end

local function drawSky(x, y, rx, rw, n)
    local s = tx["set" .. n]
    if s == nil then return end
    rect(s[1], x, y, rx, 0, rw, H)
    rect(s[2], x, y, rx + c.cloud, 0, rw, H)
    rect(s[3], x, y, rx + c.cloud * 1.6, 0, rw, H)
    rect(s[4], x, y, rx, 0, rw, H)
end

local function drawFail(x, rw)
    local f = tx.fail
    rect(tx.skyFail, x, 0, x, 0, rw, H)
    rect(f[1], x, 0, x + c.rain1, -c.rain1 * 1.5, rw, H)
    rect(f[2], x, 0, x + c.rain2, -c.rain2 * 1.5, rw, H)
    rect(f[6], x, 0, x + c.storm * 0.7, 0, rw, H)
    rect(f[5], x, 0, x + c.storm * 0.8, 0, rw, H)
    rect(f[4], x, 0, x + c.storm * 0.9, 0, rw, H)
    rect(f[3], x, 0, x + c.storm, 0, rw, H)
end

-- y loops over 1400 px, k: fall speed
local function drawConfetti(t, list, k)
    if t == nil then return end
    for _, e in ipairs(list) do
        local x, y0, s, rot, v, col = e[1], e[2], e[3], e[4], e[5], e[6]
        t:SetScale(s * math.abs(math.sin(c.confetti * v / 108)), s)
        t:SetRotation(c.confetti * v + rot)
        t:SetColor(col[1] / 255, col[2] / 255, col[3] / 255)
        t:DrawAtAnchor(x, ((c.confetti * k + y0) % 1400) - 200, "center")
    end
end

local function drawClear()
    drawConfetti(tx.confetti2, CONFETTI_2, 0.7)
    drawConfetti(tx.confetti1, CONFETTI_1, 1)
end

-- rising, k: speed
local function drawParticles(t, list, k)
    if t == nil then return end
    for _, e in ipairs(list) do
        t:SetScale(e[3], e[3])
        t:SetRotation(c.confetti * e[5] + e[4])
        t:DrawAtAnchor(e[1], ((-c.confetti * k + e[2]) % -1400) + 1200, "center")
    end
end

local function drawNokon()
    if nokon.result == 0 then
        local t = nokon.idle[math.floor(nokon.t) % 6]
        if t ~= nil then t:Draw(NOKON_X, NOKON_Y) end
        return
    end
    local t
    if nokon.style >= 2 then
        if nokon.tail ~= nil then
            nokon.tail:SetRotation(math.sin(nokon.result / (NOKON_FPS / 4)) * 10)
            nokon.tail:DrawAtAnchor(NOKON_TAIL_X, NOKON_TAIL_Y, "center")
        end
        t = nokon.react[(math.floor(nokon.result) % 28 == 0) and 0 or 1]
    else
        t = nokon.react[math.min(math.floor(nokon.result), 4)]
    end
    if t ~= nil then t:Draw(NOKON_X, NOKON_Y) end
end

local function allCleared()
    for p = 0, players - 1 do if not cleared[p] then return false end end
    return true
end

-- clock: ms since the result started, for the drops
function Bg.draw(clock)
    if ai then
        local t = aiWin and tx.win or tx.lose
        if t ~= nil then t:Draw(0, 0) end
        if tx.gradation ~= nil then tx.gradation:Draw(0, 0) end
        return
    end
    local o = fade / 255
    for _, t in pairs({ tx.skyFail, tx.confetti1, tx.confetti2 }) do t:SetOpacity(o) end
    for _, t in pairs(tx.fail) do t:SetOpacity(o) end
    if style <= 1 then
        drawSky(0, 0, 0, W, style + 1)
        if cleared[0] then drawClear() else drawFail(0, W) end
    elseif style == 2 then
        drawSky(0, 0, 0, W / 2, 1)
        drawSky(W / 2, 0, W / 2, W / 2, 2)
        drawClear()
        if not cleared[0] then drawFail(0, W / 2) end
        if not cleared[1] then drawFail(W / 2, W / 2) end
    else
        drawSky(0, 0, 0, W, 3)
        if allCleared() then drawClear() end
    end
    local d = tx.drop
    if d ~= nil then
        for _, e in ipairs(DROPS) do
            local phase = (clock - e[3]) % 10000
            local work = math.min(1000, math.abs(phase - 1000))
            local grow = math.min(2000, math.abs(phase - 2000))
            d:SetOpacity((255 - work * 0.255) / 255)
            d:SetScale(1 - grow / 4000, 1 - grow / 4000)
            d:DrawAtAnchor(e[1], e[2], "center")
        end
    end
    drawParticles(tx.particle1, PARTICLE_1, 0.7)
    drawParticles(tx.particle2, PARTICLE_2, 0.6)
    if style == 3 or style == 5 then drawNokon() end
end

local function dispose(t)
    for _, v in pairs(t) do
        if type(v) == "table" then dispose(v)
        elseif v ~= nil then pcall(function() v:Dispose() end) end
    end
end

function Bg.dispose()
    dispose(tx)
    dispose({ nokon.idle or {}, nokon.react or {}, nokon.tail })
    tx = {}
    nokon = { style = 0, t = 0, result = 0 }
end

return Bg
