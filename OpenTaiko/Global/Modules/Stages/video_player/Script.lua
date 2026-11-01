---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- video_player (debug stage)
-- ------------------------------------------------------------------------------------------------------
-- The first song cutscene of the library in the video_player ROActivity, to try the audio-track handling:
-- restart, 10 s back, play/pause, 10 s ahead, stop, a timeline that can be clicked and dragged, a speed
-- dropdown and a volume slider. Esc closes the player, which brings My Room back. With no cutscene in the
-- library, My Room comes back at once.

-- drawn in 1920x1080: a skin of another resolution rescales it
STAGES:SetResolution(1920, 1080)

local NavInput = require("Input.NavInput")

local player
local started = false

-- the engine's comma split: "\," is a literal comma
local function firstField(s)
    local cur, i = {}, 1
    while i <= #s do
        local ch = s:sub(i, i)
        if ch == "\\" and s:sub(i + 1, i + 1) == "," then cur[#cur + 1] = ","; i = i + 2
        elseif ch == "," then break
        else cur[#cur + 1] = ch; i = i + 1 end
    end
    return table.concat(cur)
end

-- the absolute path of the first .CUTSCENE_INTRO or .CUTSCENE_OUTRO file a song names, nil when none does
local function firstCutScene()
    local path = nil
    pcall(function()
        local lsls = GenerateSongListSettings()
        lsls.ModuloPagination = false
        lsls.AppendMainRandomBox = false
        lsls.AppendSubRandomBoxes = false
        lsls.SubBackBoxFrequency = 0
        local list = RequestSongList(lsls)
        if list == nil then return end
        local res = list:SearchSongsByPredicate(function(n)
            local keep = false
            pcall(function() keep = n.IsSong end)
            return keep
        end)
        for i = 0, res.Count - 1 do
            local n = res[i]
            for _, cmd in ipairs({ ".CUTSCENE_INTRO", ".CUTSCENE_OUTRO" }) do
                local v = n:GetCustomCommand(cmd)
                local file = (v ~= nil and v ~= "") and firstField(v) or ""
                if file ~= "" then
                    for d = 0, 6 do
                        local c = n:GetChart(d)
                        local folder = c ~= nil and c.SongFolder or ""
                        if folder ~= nil and folder ~= "" then path = folder .. file; return end
                    end
                end
            end
        end
    end)
    return path
end

function onStart() end

function activate()
    player = ROACTIVITY:GetROActivity("video_player")
    started = false
end

function deactivate()
    if player ~= nil and player.IsActive then player:Deactivate() end
    player = nil
end

function update(ts)
    if player == nil then return Exit("stage", "my_room") end
    if not started then
        -- the song list exists once the enumeration is done
        if IsSongsEnumDone ~= nil and not IsSongsEnumDone() then
            if NavInput.cancel() then return Exit("stage", "my_room") end
            return
        end
        local path = firstCutScene()
        if path == nil then return Exit("stage", "my_room") end
        player:Activate(path)
        started = true
    end
    player:Update()
    if not player.IsActive then return Exit("stage", "my_room") end
end

function draw()
    if started and player ~= nil and player.IsActive then player:Draw() end
end

function afterSongEnum() end
function onDestroy() end
