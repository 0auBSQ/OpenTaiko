---@diagnostic disable: undefined-global, undefined-field
-- Online.Media: the room's song on this machine. Finds it in the song list, reads its difficulty levels,
-- puts its jacket in the shared texture "preimage" and plays its preview in the shared sound "presound"
-- at the room's speed. Draws nothing. The functions take the lobby state (Online.Lobby.net) and write
-- iLackSong, songLevels, songPlus, songSubtitle, songBpm and the preview fields into it.
--
-- The shared string "presound_path" names the audio in "presound": a song select that leaves the same
-- song playing hands the preview back to the lobby, which keeps it going instead of restarting it.

local floor = math.floor

local M = {}

-- the jacket shown when the song is missing, relative to the calling stage (a missing file is ignored)
M.PLACEHOLDER = "Textures/preimage.png"

-- the song list, read once per Lua state on first use
local songList, songTried = nil, false
local function ensureSongList()
    if songList or songTried then return end
    songTried = true
    local ok, sl = pcall(function() return RequestSongList(GenerateSongListSettings()) end)
    if ok and sl then songList = sl end
end

-- the song node of a unique id, nil when this machine lacks it
function M.findSong(uid)
    ensureSongList()
    if not songList or not uid or uid == "" then return nil end
    local ok, node = pcall(function() return songList:GetSongByUniqueId(uid) end)
    if ok and node and node ~= false then return node end
    return nil
end
local findSong = M.findSong

function M.haveSong(uid) return findSong(uid) ~= nil end

function M.resolveTitle(uid)
    local node = findSong(uid); if not node then return nil end
    local ok, t = pcall(function() return node.Title end); return ok and t or nil
end

function M.stopPreview()
    pcall(function() SHARED:SetSharedPreview("presound", "Sounds/empty.ogg"); SHARED:SetSharedString("presound_path", "") end)
end

local function setBorder(t) pcall(function() t:SetWrapMode("Border") end) end
local function placeholderPreimage() pcall(function() SHARED:SetSharedTexture("preimage", M.PLACEHOLDER, setBorder) end) end

-- the preview at the room's speed from its demo start; when that audio is already playing (handed back by
-- the song select) it just keeps going
local function startPreview(st, node)
    local spd = st.song.speed and CONFIG.SONGSPEED:ToActual(st.song.speed) or 1
    st.previewSpeed = spd
    st.previewDemoStart = floor((node.DemoStart or 0) / spd)
    local cur = SHARED:GetSharedSound("presound")
    if SHARED:GetSharedString("presound_path") == node.AudioPath and cur ~= nil and cur.Loaded and cur.IsPlaying then
        cur:SetSpeed(spd); cur:SetVolume(100)
        st.previewLoaded = true
        return
    end
    M.stopPreview()
    SHARED:SetSharedString("presound_path", node.AudioPath)
    SHARED:SetSharedPreviewUsingAbsolutePath("presound", node.AudioPath, function(snd)
        snd:SetSpeed(spd); snd:SetVolume(100); snd:Play(); snd:SetTimestamp(st.previewDemoStart)
        st.previewLoaded = true
    end)
end

-- the room's song changed: its jacket, preview and levels; iLackSong when this machine lacks it
function M.load(st)
    st.songLevels, st.songPlus = {}, {}; st.iLackSong = true; st.songSubtitle = nil; st.songBpm = nil
    st.previewLoaded = false; st.previewDemoStart = 0; st.previewSpeed = 1.0; st.previewCool = 0
    if not st.song then M.stopPreview(); placeholderPreimage(); return end
    local node = findSong(st.song.uid)
    if not node then M.stopPreview(); st.iLackSong = true; placeholderPreimage(); return end
    st.iLackSong = false
    pcall(function() st.songSubtitle = node.Subtitle end)
    -- the shared "preimage" keeps the previous jacket until the new one is loaded, then swaps
    pcall(function()
        if node.HasPreimage then SHARED:SetSharedTextureUsingAbsolutePath("preimage", node.PreimagePath, setBorder)
        else placeholderPreimage() end
    end)
    for d = 0, 4 do
        pcall(function()
            local c = node:GetChart(d)
            if c ~= nil then
                st.songLevels[d] = c.Level; st.songPlus[d] = c.IsPlus == true
                if st.songBpm == nil then st.songBpm = c.BaseBPM end
            end
        end)
    end
    pcall(function() startPreview(st, node) end)
end

-- back from the song select without a new song: the room's jacket and preview again
function M.restore(st)
    if not st.song or st.iLackSong then return end
    local node = findSong(st.song.uid)
    if not node then return end
    pcall(function()
        if node.HasPreimage then SHARED:SetSharedTextureUsingAbsolutePath("preimage", node.PreimagePath, setBorder)
        else placeholderPreimage() end
    end)
    pcall(function() startPreview(st, node) end)
end

-- loops the preview (each frame on the lobby and results screens): replays it from the demo start when it
-- ends, with a short cooldown so one restart is not sought twice
function M.tick(st, dt)
    if st.iLackSong or not st.song or not st.previewLoaded then return end
    if (st.previewCool or 0) > 0 then st.previewCool = st.previewCool - dt; return end
    pcall(function()
        local snd = SHARED:GetSharedSound("presound")
        if snd and snd.Loaded and not snd.IsPlaying then
            snd:SetSpeed(st.previewSpeed or 1.0); snd:Play(); snd:SetTimestamp(st.previewDemoStart or 0)
            st.previewCool = 0.5
        end
    end)
end

return M
