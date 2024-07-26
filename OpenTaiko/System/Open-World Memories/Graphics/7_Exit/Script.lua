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

local timer = 0
local speech = "Speech/en.png"
local char = "Character/1.png"

local center_pos = {540, 540}

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("Background.png")
    func:AddGraph("Logo.png")
    func:AddGraph("Circle1.png")
    func:AddGraph("Circle2.png")
    func:AddGraph("Circle3.png")
    func:AddGraph("Effect.png")

    -- Default to en if an unsupported language is being used
    if lang == "ja" then
      speech = "Speech/ja.png"
    elseif lang == "fr" then
      speech = "Speech/fr.png"
    elseif lang == "es" then
      speech = "Speech/es.png"
    elseif lang == "zh" then
      speech = "Speech/zh.png"
    end

    func:AddGraph(speech)

    --Use this later when more chars are made
    --math.randomseed(os.time())
    --char = "Character/"..tostring(math.random(5))..".png"
    func:AddGraph(char)

    timer = 0
end

function update()
    timer = timer + deltaTime
end

function draw()
    func:SetRotation(timer * 30, "Circle1.png")
    func:SetRotation(timer * -20, "Circle2.png")
    func:SetRotation(timer * 10, "Circle3.png")

    func:DrawGraph(0, 0, "Background.png")
    func:DrawGraph(0, 0, "Effect.png")
    func:DrawGraphCenter(center_pos[1], center_pos[2], "Circle3.png")
    func:DrawGraphCenter(center_pos[1], center_pos[2], "Circle2.png")
    func:DrawGraphCenter(center_pos[1], center_pos[2], "Circle1.png")
    func:DrawGraphCenter(center_pos[1], center_pos[2], "Logo.png")
    func:DrawGraphCenter(center_pos[1], center_pos[2], speech)
    func:DrawGraph(0, 0, char)
    func:DrawNum(0, 0, timer)
end
