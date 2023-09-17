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

local incounter = 0
local bgZoom = 0

local bg_width = 1280
local bg_height = 720

local dan_in_width = 640
local dan_in_height = 720

local dan_text_width = 226
local dan_text_height = 226

local dan_text_x = { 300, 980, 300, 980 }
local dan_text_y = { 198, 198, 522, 522 }
local dan_text_appearStamps = { 1645, 2188, 2646, 3152 }

local dan_in_move = 0

local x = 640
local y = 360

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("Background.png")
    func:AddGraph("Dan_In.png")
    func:AddGraph("Dan_Text.png")
end

function update()
    incounter = incounter + (1000.0 * deltaTime)
    if incounter > 6000 then
        incounter = 6000
    end

    bgZoom = math.min(1.14, math.max(1, ((incounter / 3834.0) ^ 0.5)))

    if incounter >= 3834 then
        dan_in_move = math.min((((incounter - 3834) / 2166.0) * 4.0), 1) * dan_in_width
    end
end

function draw()
    func:SetScale(bgZoom, bgZoom, "Background.png")

    func:DrawGraphCenter(x, y, "Background.png")

    if incounter < 6000 then
        func:DrawRectGraph(0 - dan_in_move, 0, 0, 0, dan_in_width, dan_in_height, "Dan_In.png")
        func:DrawRectGraph(dan_in_width + dan_in_move, 0, dan_in_width, 0, dan_in_width, dan_in_height, "Dan_In.png")
        
        if incounter <= 3834 then
            for i = 1, 4 do
                if incounter >= dan_text_appearStamps[i] then
                    value = math.min(255, incounter - dan_text_appearStamps[i])
                    func:SetOpacity(value, "Dan_Text.png")

                    ratio = (255 - value) / 400 + 1
                    func:SetScale(ratio, ratio, "Dan_Text.png")

                    func:DrawGraphRectCenter(dan_text_x[i], dan_text_y[i], dan_text_width * i, 0, dan_text_width, dan_text_height, "Dan_Text.png")
                end
            end
        end
    end

    func:DrawNum(0, 0, incounter)
end
