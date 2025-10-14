local sounds = {}

local video = nil

local text = nil
local text_enter = nil

local counter = nil

function draw()
	if video ~= nil then
		video.Texture:Draw(0,0)
	end

	if text_enter ~= nil then
		text_enter:DrawAtAnchor(960, 960, "Center")
	end
end

function update()
	counter:Tick()

	if INPUT:Pressed("Decide") == true or INPUT:KeyboardPressed("Return") == true then
		sounds.Skip:Play()
		return Exit("title", nil)
	end

	if text_enter ~= nil then
		text_enter:SetOpacity(counter.Value)
	end

	if video ~= nil and video.Duration > 0.0 and video:GetPlayPosition() >= math.floor(video.Duration) then
		return Exit("title", nil)
	end
end

function activate()
	text = TEXT:Create(32)
	text_enter = text:GetText("Press Enter to Start!")
	video = VIDEO:CreateVideo("Videos/intro.mp4")
	
	sounds.BGM = SOUND:CreateBGM("Sounds/BGM.ogg")
	--sounds.BGM:SetLoop(true)
	sounds.Skip = SOUND:CreateSFX("Sounds/Skip.ogg")

	counter = COUNTER:CreateCounter(0.0, 1.0, 1.0)
	counter:SetBounce(true)
	counter:Start()
	
	video:Start()
	sounds.BGM:Play()
end

function deactivate()
	if video ~= nil then
		video:Dispose()
	end
	for _, sound in pairs(sounds) do
		sound:Dispose()
	end
	if text ~= nil then
		text:Dispose()
	end
end

function onStart()
	SHARED:SetSharedSFX("Decide", "Sounds/Decide.ogg")
	SHARED:SetSharedSFX("Move", "Sounds/Move.ogg")
	SHARED:SetSharedSFX("Cancel", "Sounds/Cancel.ogg")
	SHARED:SetSharedSFX("Skip", "Sounds/Skip.ogg")
	SHARED:SetSharedSFX("SongDecide", "Sounds/SongDecide.ogg")
end

function afterSongEnum()
	if sounds ~= nil and sounds.BGM ~= nil and video ~= nil then
		sounds.BGM:SetTimestamp(video:GetPlayPosition() * 1000)
	end
end

function onDestroy()
	SHARED:ClearSharedSound("Decide")
	SHARED:ClearSharedSound("Move")
	SHARED:ClearSharedSound("Cancel")
	SHARED:ClearSharedSound("Skip")
	SHARED:ClearSharedSound("SongDecide")
end
