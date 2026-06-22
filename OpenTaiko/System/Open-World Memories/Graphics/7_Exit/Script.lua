local timer = 0
local speech = "Speech/en.png"
local char = "Character/1.png"
local id = 1

local pos = {
  { x=1058, y=131 },
  { x=925, y=25 },
  { x=1092, y=65 },
  { x=951, y=288 },
  { x=1142, y=17 },
  { x=1108, y=35 },
  { x=915, y=189 },
  { x=1168, y=21 }
}

local total_chars = 8

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
    elseif lang == "ru" then
      speech = "Speech/ru.png"
    elseif lang == "zh" then
      speech = "Speech/zh.png"
    end

    func:AddGraph(speech)

    -- Random character
    math.randomseed(os.time())
    id = math.random(total_chars)
    char = "Character/"..tostring(id)..".png"
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
    func:DrawGraph(pos[id].x, pos[id].y, char)
    --func:DrawNum(0, 0, timer)
end
