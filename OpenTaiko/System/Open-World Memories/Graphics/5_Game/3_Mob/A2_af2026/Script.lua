---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Ported from the old ScriptBG func: API to the ROActivity LuaTexture API.

--local debug_counter = 0

local mob_x = 0
local mob_front_y = 0
local mob_back_y = 0
local mob_back2_y = 0
local mob_height = 0
local mob_counter = 0
local mob_action_counter = 0
local mob_in_counter = 0
local mob_out_counter = 0

local mob_state = 0

local tx = {}

local function mobIn()
    mob_state = 1
    mob_in_counter = 0
end

local function mobOut()
    mob_state = 2
    mob_out_counter = 0
end

function onStart()
    tx["Mob_Front.png"] = TEXTURE:CreateTextureSync("Mob_Front.png")
    tx["Mob_Back.png"] = TEXTURE:CreateTextureSync("Mob_Back.png")
    tx["Mob_Back2.png"] = TEXTURE:CreateTextureSync("Mob_Back2.png")
    mob_height = tx["Mob_Front.png"].Height
end

function update(timestamp, state)

    --debug_counter = debug_counter + (fps.deltaTime)

    --if debug_counter > 2 then
    --    if mob_state == 0 then
    --        mobIn()
    --    else
    --        mobOut()
    --    end
    --    debug_counter = 0
    --end



    if mob_state == 3 and state.gauge[0] < 100 then
        mobOut()
    end

    if mob_state == 0 then

        if state.gauge[0] == 100 then
            mobIn()
        end

    elseif mob_state == 1 then

        local value = math.min(1, mob_in_counter + (2 * math.abs(state.bpm[0]) * fps.deltaTime / 60.0))
        if 0 * value == 0 * value then  -- finite
            mob_in_counter = value
            if value >= 1 then
                mob_state = 3
                mob_counter = 0.5
                mob_action_counter = 0
            end
        end

        local mobinValue = (1.0 - math.sin(mob_in_counter * math.pi / 2))
        mob_front_y = 1080 + (540 * mobinValue)
        mob_back_y = 1080 + (540 * mobinValue)
        mob_back2_y = 1080 + (540 * mobinValue)



    elseif mob_state == 2 then

        local value = math.min(1, mob_out_counter + (2 * math.abs(state.bpm[0]) * fps.deltaTime / 60.0))
        if 0 * value == 0 * value then  -- finite
            mob_out_counter = value
            if value >= 1 then
                mob_state = 0
            end
        end

        local mobOutValue = (1 - math.cos(mob_out_counter * math.pi))
        mob_front_y = 1080 + (540 * mobOutValue)
        mob_back_y = 1080 + (540 * mobOutValue)
        mob_back2_y = 1080 + (540 * mobOutValue)

    elseif mob_state == 3 then

        local value = math.fmod(mob_counter + (math.abs(state.bpm[0]) * fps.deltaTime / 60.0), 1)
        if 0 * value == 0 * value then  -- finite
            mob_counter = value
        end


        value = math.fmod(mob_action_counter + (12 / 13.0 * math.abs(state.bpm[0]) * fps.deltaTime / 60.0), 1)
        if 0 * value == 0 * value then  -- finite
            mob_action_counter = value
        end


        local mob_loop_value = (1.0 - math.sin(mob_counter * math.pi))
        mob_front_y = 1080 + (mob_loop_value * 45)
        mob_back_y = 1080 + (mob_loop_value * 70)
        mob_back2_y = 1080 + (mob_loop_value * 60)
    end
end

function draw(state)
    if mob_state == 0 then
    else
        tx["Mob_Back2.png"]:Draw(mob_x, mob_back2_y - mob_height)
        tx["Mob_Back.png"]:Draw(mob_x, mob_back_y - mob_height)
        tx["Mob_Front.png"]:Draw(mob_x, mob_front_y - mob_height)
    end
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end
