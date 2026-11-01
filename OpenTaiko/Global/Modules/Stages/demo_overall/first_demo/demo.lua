-- demo_overall/first_demo/demo.lua — the first demo using Lua stages: a background, a video, music
-- and a counter. Cancel/Escape plays a sound after a second and goes back to My Room.

local D = {}

local DIR = "first_demo/"

local background = nil
local bg_video = nil

local text = nil
local textTex = nil

local sounds = {}

local test_counter = nil
local existing = false

local function test_counter_ended()
	existing = true

	bg_video:Stop()

	local test = GetSaveFile(0)
	debugLog(tostring(test.TotalPlaycount))
	sounds.Cancel:Play()
end

function D.draw()
	if background ~= nil then
		background:Draw(0,0)
	end

	if bg_video ~= nil then
		bg_video.Texture:Draw(0,0)
	end

	if textTex ~= nil then
		textTex:Draw(200,200)
	end
end

function D.update()
	if INPUT:Pressed("Cancel") == true or INPUT:KeyboardPressed("Escape") == true then
		test_counter:Start()
	end
	if existing then
		existing = false
		return Exit("stage", "my_room")
	end

	test_counter:Tick()
end

function D.activate()
	existing = false
	test_counter = COUNTER:CreateCounter(0, 1.0, 1.0, test_counter_ended)

	bg_video:Reset()
	bg_video:Start()

	sounds.BGM:Play()
end

function D.deactivate()
	bg_video:Stop()

	sounds.BGM:Stop()
end

function D.onStart()
	background = TEXTURE:CreateTexture(DIR .. "Textures/Background.png")
	bg_video = VIDEO:CreateVideo(DIR .. "Videos/funny.mp4")
	text = TEXT:Create(16)
	textTex = text:GetText("Do you remember me?...")

	sounds.BGM = SOUND:CreateBGM(DIR .. "Sounds/BGM.ogg")
	sounds.BGM:SetLoop(true)
	sounds.Skip = SOUND:CreateSFX(DIR .. "Sounds/Skip.ogg")
	sounds.Cancel = SOUND:CreateSFX(DIR .. "Sounds/Cancel.ogg")
end

function D.onDestroy()
	if background ~= nil then
		background:Dispose()
	end
	if bg_video ~= nil then
		bg_video:Dispose()
	end
	if textTex ~= nil then
		textTex:Dispose()
	end
	for _, sound in pairs(sounds) do
		sound:Dispose()
	end
end

return D
