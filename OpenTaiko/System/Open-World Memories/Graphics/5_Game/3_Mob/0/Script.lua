--func:DrawText(x, y, text)
--func:DrawNum(x, y, num)
--func:AddGraph("filename")
--func:DrawGraph(x, y, filename)
--func:SetOpacity(opacity, "filename")
--func:SetScale(xscale, yscale, "filename")
--func:SetColor(r, g, b, "filename")

--local debug_counter = 0

local mob_x = 0
local mob_front_y = 0
local mob_back_y = 0
local mob_back2_y = 0
local mob_back3_y = 0
local mob_height = 0
local mob_counter = 0
local mob_action_counter = 0
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
    func:AddGraph("Mob_Front.png")
    func:AddGraph("Mob_Back_0.png")
    func:AddGraph("Mob_Back_1.png")
    func:AddGraph("Mob_Back2_0.png")
    func:AddGraph("Mob_Back2_1.png")
    func:AddGraph("Mob_Back3_0.png")
    func:AddGraph("Mob_Back3_1.png")
    mob_height = func:GetTextureHeight("Mob_Front.png")
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
            mob_action_counter = 0
        end

        mobinValue = (1.0 - math.sin(mob_in_counter * math.pi / 2))
        mob_front_y = 1080 + (540 * mobinValue)
        mob_back_y = 1080 + (540 * mobinValue)
        mob_back2_y = 1080 + (540 * mobinValue)
        mob_back3_y = 1080 + (540 * mobinValue)



    elseif mob_state == 2 then

        mob_out_counter = mob_out_counter + (bpm[0] * deltaTime / 30.0)
        if mob_out_counter > 1 then
            mob_state = 0
        end

        mobOutValue = (1 - math.cos(mob_out_counter * math.pi))
        mob_front_y = 1080 + (540 * mobOutValue)
        mob_back_y = 1080 + (540 * mobOutValue)
        mob_back2_y = 1080 + (540 * mobOutValue)
        mob_back3_y = 1080 + (540 * mobOutValue)

    elseif mob_state == 3 then

        mob_counter = mob_counter + (bpm[0] * deltaTime / 60.0)
        if mob_counter > 1 then
            mob_counter = 0
        end


        mob_action_counter = mob_action_counter + (bpm[0] * deltaTime / 65.0)
        if mob_action_counter > 1 then
            mob_action_counter = 0
        end


        mob_loop_value = (1.0 - math.sin(mob_counter * math.pi))
        mob_front_y = 1080 + (mob_loop_value * 45)
        mob_back_y = 1080 + (mob_loop_value * 70)
        mob_back2_y = 1080 + (mob_loop_value * 105)
        mob_back3_y = 1080 + (mob_loop_value * 80)
    end
end

function draw()
    if mob_state == 0 then
    else
        if mob_action_counter > 0.25 and mob_action_counter < 0.5 then
        func:DrawGraph(mob_x, mob_back2_y - mob_height, "Mob_Back2_1.png")
            func:DrawGraph(mob_x, mob_back_y - mob_height, "Mob_Back_1.png")
        else
        func:DrawGraph(mob_x, mob_back2_y - mob_height, "Mob_Back2_0.png")
            func:DrawGraph(mob_x, mob_back_y - mob_height, "Mob_Back_0.png")
        end
        if (mob_counter > 0.500 and mob_counter < 0.6) or (mob_counter > 0.7 and mob_counter < 0.8) then
            func:DrawGraph(mob_x, mob_back3_y - mob_height, "Mob_Back3_1.png")
        else
            func:DrawGraph(mob_x, mob_back3_y - mob_height, "Mob_Back3_0.png")
        end
        func:DrawGraph(mob_x, mob_front_y - mob_height, "Mob_Front.png")
    end
end
