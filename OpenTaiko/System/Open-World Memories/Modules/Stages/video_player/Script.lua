---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- video_player (debug stage)
-- ------------------------------------------------------------------------------------------------------
-- The boot intro in the video_player ROActivity, to try the audio-track handling: restart, 10 s back,
-- play/pause, 10 s ahead, stop, a timeline that can be clicked and dragged, a speed dropdown and a volume
-- slider. Esc closes the player, which brings the title screen back.

local VIDEO_PATH = info.dir .. "/../_boot/Videos/intro.mp4"

local player

function onStart() end

function activate()
    player = ROACTIVITY:GetROActivity("video_player")
    if player ~= nil then player:Activate(VIDEO_PATH) end
end

function deactivate()
    if player ~= nil and player.IsActive then player:Deactivate() end
    player = nil
end

function update(ts)
    if player == nil then return Exit("stage", "_title") end
    player:Update()
    if not player.IsActive then return Exit("stage", "_title") end
end

function draw()
    if player ~= nil and player.IsActive then player:Draw() end
end

function afterSongEnum() end
function onDestroy() end
