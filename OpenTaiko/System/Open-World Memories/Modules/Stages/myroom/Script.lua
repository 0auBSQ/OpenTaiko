---@diagnostic disable: undefined-global, lowercase-global
-- myroom: forwards to the global stage my_room (callers of "myroom", the engine's "heya" exit)
-- no way back given: title through My Room's door; to remove at release with myroom/data.mdb

local BACK_KEY = "my_room_return"

function onStart() end
function activate() end
function deactivate() end

function update(ts)
    local ok, s = pcall(function() return SHARED:GetSharedString(BACK_KEY) end)
    if not ok or type(s) ~= "string" or s == "" then
        pcall(function() SHARED:SetSharedString(BACK_KEY, "_title|myroom_door") end)
    end
    return Exit("stage", "my_room")
end

function draw() end
function afterSongEnum() end
function onDestroy() end
