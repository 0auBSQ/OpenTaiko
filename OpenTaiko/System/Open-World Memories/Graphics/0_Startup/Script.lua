--func:DrawText(x, y, text)
--func:DrawNum(x, y, num)
--func:AddGraph("filename")
--func:DrawGraph(x, y, filename)
--func:DrawRectGraph(x, y, rect_x, rect_y, rect_width, rect_height, filename)
--func:DrawGraphCenter(x, y, filename)
--func:DrawGraphRectCenter(x, y, rect_x, rect_y, rect_width, rect_height, filename)
--func:SetOpacity(opacity, "filename")
--func:SetRotation(angle, "fileName")
--func:SetScale(xscale, yscale, "filename")
--func:SetColor(r, g, b, "filename")

local loadingAnimeType = 0

local currentTime = 0
local frames = {9, 6}
local selectedChara = 1

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("Background.png")

    seed = os.time()
    math.randomseed(seed)
    selectedChara = math.random(2);

    if loadingAnimeType == 0 then
        -- func:AddGraph("OpTKIcon.png")
        -- func:AddGraph("0.png")
        -- func:AddGraph("1.png")
        -- func:AddGraph("2.png")
        -- func:AddGraph("3.png")
        -- func:AddGraph("4.png")
        -- func:AddGraph("5.png")
        -- func:AddGraph("6.png")
        for i = 0, frames[selectedChara] - 1 do
            func:AddGraph("Loading/"..tostring(selectedChara).."/"..tostring(i)..".png")
        end
    elseif loadingAnimeType == 1 then
    end
end

function update()
    if loadingAnimeType == 0 then
        currentTime = (currentTime + deltaTime)
        -- optkAngle = optkAngle + (360 * deltaTime)
    elseif loadingAnimeType == 1 then
    end
end

function draw()
    func:DrawGraph(0, 0, "Background.png")
    func:DrawText(0, 0, "selectedChara: "..tostring(selectedChara))
    func:DrawText(0, 16, "frames: "..tostring(frames[selectedChara]))
    if loadingAnimeType == 0 then
        func:DrawGraph(1657, 821, "Loading/"..tostring(selectedChara).."/"..tostring(math.floor(currentTime * 10) % frames[selectedChara])..".png")
        -- func:SetRotation(optkAngle, "OpTKIcon.png")
        -- func:DrawGraph(1720, 880, "OpTKIcon.png")
    elseif loadingAnimeType == 1 then
    end
end
