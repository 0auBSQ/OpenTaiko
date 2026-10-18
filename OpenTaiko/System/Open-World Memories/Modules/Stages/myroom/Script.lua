---@diagnostic disable: undefined-global, lowercase-global
-- myroom: My Room is the global stage my_room now (Global/Modules/Stages/my_room). This stage only
-- forwards there, for the callers that still open "myroom" (the engine's legacy "heya" exit among them).
-- A caller that named no way back returns to the title through My Room's door. Removed at the official
-- release, with my_room's reading of the rooms this stage kept in its folder (myroom/data.mdb).

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
