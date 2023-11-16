--func:DrawText(x, y, text)
--func:DrawNum(x, y, num)
--func:AddGraph("filename")
--func:DrawGraph(x, y, filename)
--func:SetOpacity(opacity, "filename")
--func:SetScale(xscale, yscale, "filename")
--func:SetColor(r, g, b, "filename")

--local debug_counter = 0

local mob_x = 0
local mob_y = 0
local mob_height = 0
local mob_counter = 0
local mob_in_counter = 0
local mob_out_counter = 0

local mob_state = 0

function mobIn()
    mob_state = 1
    mob_in_counter = 0
end

function mobOut()
    mob_state = 2
    mob_out_counter = 0
end

function init()
    func:AddGraph("Mob.png")
    mob_height = func:GetTextureHeight("Mob.png")
end

function update()

    --debug_counter = debug_counter + (deltaTime)

    --if debug_counter > 2 then
    --    if mob_state == 0 then
    --        mobIn()
    --    else
    --        mobOut()
    --    end
    --    debug_counter = 0
    --end

    mob_counter = mob_counter + (bpm[0] * deltaTime / 60.0)
    if mob_counter > 1 then 
        mob_counter = 0
    end

    


    if mob_state == 3 and gauge[0] < 100 then
        mobOut()
    end

    if mob_state == 0 then

        if gauge[0] == 100 then
            mobIn()
        end

    elseif mob_state == 1 then

        mob_in_counter = mob_in_counter + (bpm[0] * deltaTime / 30.0)
        if mob_in_counter > 1 then
            mob_state = 3
            mob_counter = 0.5
        end
        
        mob_y = 720 + (360 * (1.0 - math.sin(mob_in_counter * math.pi / 2)))

    elseif mob_state == 2 then

        mob_out_counter = mob_out_counter + (bpm[0] * deltaTime / 30.0)
        if mob_out_counter > 1 then
            mob_state = 0
        end
        
        mob_y = 720 + (360 * (1 - math.cos(mob_out_counter * math.pi)))

    elseif mob_state == 3 then
        mob_y = 720 + ((1.0 - math.sin(mob_counter * math.pi)) * 70)
    end
end

function draw()
    if mob_state == 0 then
    else
        func:DrawGraph(mob_x, mob_y - mob_height, "Mob.png")
    end
end
