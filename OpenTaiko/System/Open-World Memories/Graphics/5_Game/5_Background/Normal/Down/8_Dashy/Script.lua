local rotation = 0.0

-- local Background = TEXTURE:CreateTexture("BG.png")
-- local Gear = TEXTURE:CreateTexture("Gears.png")

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("BG.png")
    func:AddGraph("Gears.png")
end

function update()
    rotation = rotation + (deltaTime * 8)
end

function draw()
    func:DrawGraph(0, 540, "BG.png")


    func:SetRotation(-rotation, "Gears.png")
    func:DrawGraphRectCenter(40, 969, 0, 0, 333, 310, "Gears.png")
    func:SetRotation(rotation, "Gears.png")
    func:DrawGraphRectCenter(1934, 556, 334, 0, 491, 457, "Gears.png")

    -- Background:Draw(0, 540)

    -- Gear:SetRotation(-rotation)
    -- Gear:DrawRectAtAnchor(40, 969, 0, 0, 333, 310, "center")
    -- Gear:SetRotation(rotation)
    -- Gear:DrawRectAtAnchor(1934, 556, 334, 0, 491, 457, "center")
end
