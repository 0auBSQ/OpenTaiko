---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Mob 0: spectator crowd (front + 3 back layers) that hops in when the gauge fills and bobs to the BPM.
-- Ported from the old ScriptBG func: API to the ROActivity LuaTexture API.

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
    tx["Mob_Back2_0.png"] = TEXTURE:CreateTextureSync("Mob_Back2_0.png")
    tx["Mob_Back2_1.png"] = TEXTURE:CreateTextureSync("Mob_Back2_1.png")
    tx["Mob_Back3.png"] = TEXTURE:CreateTextureSync("Mob_Back3.png")
    mob_height = tx["Mob_Front.png"].Height   -- sync load makes the size valid immediately
end

function update(timestamp, state)
    if mob_state == 3 and state.gauge[0] < 100 then
        mobOut()
    end

    if mob_state == 0 then

        if state.gauge[0] == 100 then
            mobIn()
        end

    elseif mob_state == 1 then

        mob_in_counter = mob_in_counter + (state.bpm[0] * fps.deltaTime / 30.0)
        if mob_in_counter > 1 then
            mob_state = 3
            mob_counter = 0.5
            mob_action_counter = 0
        end

        local mobinValue = (1.0 - math.sin(mob_in_counter * math.pi / 2))
        mob_front_y = 1080 + (540 * mobinValue)
        mob_back_y = 1080 + (540 * mobinValue)
        mob_back2_y = 1080 + (540 * mobinValue)
        mob_back3_y = 1080 + (540 * mobinValue)



    elseif mob_state == 2 then

        mob_out_counter = mob_out_counter + (state.bpm[0] * fps.deltaTime / 30.0)
        if mob_out_counter > 1 then
            mob_state = 0
        end

        local mobOutValue = (1 - math.cos(mob_out_counter * math.pi))
        mob_front_y = 1080 + (540 * mobOutValue)
        mob_back_y = 1080 + (540 * mobOutValue)
        mob_back2_y = 1080 + (540 * mobOutValue)
        mob_back3_y = 1080 + (540 * mobOutValue)

    elseif mob_state == 3 then

        mob_counter = mob_counter + (state.bpm[0] * fps.deltaTime / 60.0)
        if mob_counter > 1 then
            mob_counter = 0
        end


        mob_action_counter = mob_action_counter + (state.bpm[0] * fps.deltaTime / 65.0)
        if mob_action_counter > 1 then
            mob_action_counter = 0
        end


        local mob_loop_value = (1.0 - math.sin(mob_counter * math.pi))
        mob_front_y = 1080 + (mob_loop_value * 45)
        mob_back_y = 1080 + (mob_loop_value * 70)
        mob_back2_y = 1080 + (mob_loop_value * 105)
        mob_back3_y = 1080 + (mob_loop_value * 80)
    end
end

function draw(state)
    if mob_state == 0 then
    else
        if mob_action_counter > 0.25 and mob_action_counter < 0.5 then
        tx["Mob_Back2_1.png"]:Draw(mob_x, mob_back2_y - mob_height)
        else
        tx["Mob_Back2_0.png"]:Draw(mob_x, mob_back2_y - mob_height)
        end

        tx["Mob_Back.png"]:Draw(mob_x, mob_back_y - mob_height)
        tx["Mob_Back3.png"]:Draw(mob_x, mob_back3_y - mob_height)
        tx["Mob_Front.png"]:Draw(mob_x, mob_front_y - mob_height)
    end
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end
