---@diagnostic disable: undefined-global  -- TEXTURE/fps/STORAGE/JSONLOADER injected by CLuaScript at runtime
-- Tower down background "Event": the sky, scrolling darker as the climb goes on, and the tower itself, whose
-- floors slide down under the player at every new floor.
--
-- This folder is one complete tower look. The chart picks it with TOWERTYPE:Event (a preset or a random pick
-- otherwise), and the tower select, the loading screen and the result compose the same tower from its pieces:
--   Sky_Gradient.png   the sky strip, its bottom the day and its top the night
--   Base/BaseN.png     the floors, cycled every ten floors
--   Deco/DecoN.png     a decoration over each floor, cycled every floor (optional)
--   Top.png            the roof, one more floor over the last one
--   Config.json        the layout: body_x/body_y the current floor's bottom centre, deco_x/deco_y its
--                      decoration's, move_x/move_y how far a floor slides when the next one is reached

local towerUpProgress = 0
local lastNightNum = 0
local skyHeight = 7434

local layout = { body_x = 960, body_y = 1014, deco_x = 690, deco_y = 960, move_x = 0, move_y = 432 }
local tx = {}
local bases, decos, top = {}, {}, nil

local lastFloor = nil   -- a new floor starts the slide
local slide = 0         -- 0..1, the current floor sliding down as the next one comes into view
local slideSpeed = 0    -- per second: a slide lasts two beats

local function loadSequence(dir, prefix)
    local list, i = {}, 0
    while STORAGE:FileExists(dir .. prefix .. i .. ".png") do
        list[#list + 1] = TEXTURE:CreateTextureSync(dir .. prefix .. i .. ".png")
        i = i + 1
    end
    return list
end

local function pick(list, index)
    if #list == 0 then return nil end
    return list[(index % #list) + 1]
end

function clearIn(player)

end

function clearOut(player)

end

function onStart()
    tx["Sky_Gradient.png"] = TEXTURE:CreateTextureSync("Sky_Gradient.png")
    if STORAGE:FileExists("Config.json") then
        local cfg = JSONLOADER:JsonParseFileAny("Config.json")
        for k, _ in pairs(layout) do
            local v = tonumber(JSONLOADER:JsonGet(cfg, k))
            if v ~= nil then layout[k] = v end
        end
    end
    bases = loadSequence("Base/", "Base")
    decos = loadSequence("Deco/", "Deco")
    if STORAGE:FileExists("Top.png") then top = TEXTURE:CreateTextureSync("Top.png") end
end

function activate(state)
    lastFloor, slide = nil, 0
end

function update(timestamp, state)
    towerUpProgress = towerUpProgress + ((fps.deltaTime * (state.bpm[0] / 120)) / 140);
    if towerUpProgress > 1 then
      towerUpProgress = 1
    elseif towerUpProgress > lastNightNum then
      towerUpProgress = lastNightNum
    end

    if state.towerNightNum ~= lastNightNum then
      towerUpProgress = lastNightNum
      lastNightNum = state.towerNightNum
    end

    local floor = state.towerFloor
    if lastFloor ~= nil and floor > lastFloor then
        local beat = (60 / math.max(1, state.bpm[0])) / ((CONFIG.SongSpeed or 20) / 20)
        slide, slideSpeed = 0, 1 / (2 * beat)
    end
    lastFloor = floor
    if slide < 1 then slide = math.min(1, slide + fps.deltaTime * slideSpeed) end
end

-- the current floor (or the roof on the last one) with its decoration, then the next floor sliding in above
-- it, cut so it never reaches over the lane
local function drawTower(state)
    local floor, maxFloor = state.towerFloor, state.towerMaxFloor
    local dx, dy = slide * layout.move_x, slide * layout.move_y

    -- the piece indexes of the first floor skip one, as the gameplay always did
    local baseIndex, decoIndex = math.floor(floor / 10), floor
    local nextBaseIndex, nextDecoIndex = math.floor((floor + 1) / 10), floor + 1
    if floor == 0 and #decos > 1 then decoIndex = decoIndex + 1 end
    if floor == 0 and #bases > 1 then baseIndex = baseIndex + 1 end

    local current = (floor < maxFloor) and pick(bases, baseIndex) or top
    if current ~= nil then current:DrawAtAnchor(layout.body_x + dx, layout.body_y + dy, "bottom") end
    local deco = pick(decos, decoIndex)
    if deco ~= nil then deco:DrawAtAnchor(layout.deco_x + dx, layout.deco_y + dy, "bottom") end

    local nextPiece = nil
    if floor + 1 < maxFloor then nextPiece = pick(bases, nextBaseIndex)
    elseif floor + 1 == maxFloor then nextPiece = top end
    if nextPiece ~= nil then
        local originY = math.floor(layout.move_y - dy)
        nextPiece:DrawRectAtAnchor(layout.body_x - layout.move_x + dx, layout.body_y - layout.move_y + dy,
            0, originY, nextPiece.Width, nextPiece.Height - originY, "bottom")
    end
    if floor + 1 <= maxFloor then
        local nextDeco = pick(decos, nextDecoIndex)
        if nextDeco ~= nil then
            nextDeco:DrawAtAnchor(layout.deco_x - layout.move_x + dx, layout.deco_y - layout.move_y + dy, "bottom")
        end
    end
end

function draw(state)
    tx["Sky_Gradient.png"]:DrawRect(0, 540, 0, skyHeight - (towerUpProgress * skyHeight), 1920, 540);
    drawTower(state)
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
    for _, t in ipairs(bases) do t:Dispose() end
    for _, t in ipairs(decos) do t:Dispose() end
    if top ~= nil then top:Dispose() end
    bases, decos, top = {}, {}, nil
end
