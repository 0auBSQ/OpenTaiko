local ribbon0LoopWidth = 1566
local ribbon1LoopWidth = 1566

local ribbon0ScrollX = 0
local ribbon1ScrollX = 0

-- avoid unnecessary draw loops for 3+ players
local maxPlayerLoop = 0

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("BG.png")
    func:AddGraph("Ribbon0.png")
    func:AddGraph("Ribbon1.png")

    -- avoid unnecessary draw loops for 3+ players
    maxPlayerLoop = math.min(playerCount - 1, 1)

    -- random values to create initial depth
    ribbon0ScrollX = 250
    ribbon1ScrollX = 914
end

function update()
    ribbon0ScrollX = ribbon0ScrollX + (deltaTime * 59)
    if ribbon0ScrollX > ribbon0LoopWidth then
      ribbon0ScrollX = 0
    end
    ribbon1ScrollX = ribbon1ScrollX + (deltaTime * 27)
    if ribbon1ScrollX > ribbon1LoopWidth then
      ribbon1ScrollX = 0
    end
end


function draw()
    for player = 0, maxPlayerLoop do
        y = 0
        if player == 1 then
            y = 804
        end
        func:DrawGraph(0, y, "BG.png")
        for i = 0, 3 do
        func:DrawGraph((ribbon0LoopWidth * i) - ribbon0ScrollX, y, "Ribbon0.png")
        func:DrawGraph((ribbon1LoopWidth * i) - ribbon1ScrollX, y, "Ribbon1.png")
        end
    end
end
