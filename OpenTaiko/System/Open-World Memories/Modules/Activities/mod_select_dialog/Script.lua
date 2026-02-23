-- Reactive once the enter animation is done
local reactive = false
local player = 0

-- Fonts
local textSmall = nil
local text = nil
local textLarge = nil

-- Textures
local tx = {}

-- Sounds
local sounds = {}

-- Animations and counters
local ctx = {}

local bgpos = 1080
local bgtlop = 0

-- Add counter helper
local function startCounter(key, startVal, endVal, interval, mode, updateCallback, onFinish)
    local c = COUNTER:CreateCounter(startVal, endVal, interval, onFinish)
    if mode == "loop" then c:SetLoop(true)
    elseif mode == "bounce" then c:SetBounce(true) end
    if updateCallback then c:Listen(updateCallback) end
    c:Start()
    ctx[key] = c
    return c
end

local function updateTransitionVisuals(val)
    bgpos = val
    
    local bgOpacity = 255 - (val * (255 / 540))
    bgtlop = math.max(0, math.min(255, bgOpacity))
end

-- Saves ~8Mb (+ overhead) of ram
local function drawBg(opacity)
    tx["bgtile"]:SetOpacity((opacity*bgtlop) / 255)
    for i = 0, 10, 1 do
        for j = 0, 10, 1 do
            tx["bgtile"]:Draw(i*192,j*108)
        end
    end
end

function draw()
    drawBg(0.5)
    tx["bg"]:SetOpacity(bgtlop / 255)
    tx["bg"]:Draw(0, bgpos)
end

function update()
    for k, counter in pairs(ctx) do
        counter:Tick()
    end

    -- Placeholder
    if reactive == true then
        if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
            sounds.Cancel:Play()
            reactive = false
            startCounter("screen_transition", 0, 1080, 0.5/1080, "none", updateTransitionVisuals, function() 
                DEACTIVATE()
            end)
        end
    end
end

function activate(pl)
    debugLog(tostring(pl))
    player = pl

    sounds.Skip = SHARED:GetSharedSound("Skip")
	sounds.Cancel = SHARED:GetSharedSound("Cancel")
	sounds.Decide = SHARED:GetSharedSound("Decide")
	sounds.SongDecide = SHARED:GetSharedSound("SongDecide")

    startCounter("screen_transition", 1080, 0, -0.5/1080, "none", updateTransitionVisuals, function() 
		reactive = true
	end)
end

function deactivate()
    for k, counter in pairs(ctx) do
        counter = COUNTER:EmptyCounter()
    end

end

function onStart()
    textSmall = TEXT:Create(18)
	text = TEXT:Create(28)
	textLarge = TEXT:Create(40)

    tx["bg"] = TEXTURE:CreateTexture("Textures/Background.png")
    tx["bgtile"] = TEXTURE:CreateTexture("Textures/BgTile.png")
end

function onDestroy()
    if text ~= nil then
		text:Dispose()
	end
	if textSmall ~= nil then
		textSmall:Dispose()
	end
	if textLarge ~= nil then
		textLarge:Dispose()
	end

    for _, t in pairs(tx) do
		t:Dispose()
	end
end