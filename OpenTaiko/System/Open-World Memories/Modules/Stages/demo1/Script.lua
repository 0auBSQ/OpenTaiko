local background = nil

local text = nil
local textTex = nil

local sounds = {}

local test_counter = nil
local existing = false

local function test_counter_ended()
	existing = true

	local test = GetSaveFile(0)
	debugLog(tostring(test.TotalPlaycount))
	sounds.Cancel:Play()
end

function draw()
	if background ~= nil then
		background:Draw(0,0)
	end
	if textTex ~= nil then
		textTex:Draw(200,200)
	end
end

function update()
	if INPUT:Pressed("Cancel") == true or INPUT:KeyboardPressed("Escape") == true then
		test_counter:Start()
	end
	if INPUT:KeyboardPressed("S") == true then
		sounds.Skip:Play()
		return Exit("stage", "demo2")
	end
	if existing then
		existing = false
		return Exit("title", nil)
	end

	test_counter:Tick()
end

function activate()
	existing = false
	test_counter = COUNTER:CreateCounter(0, 1.0, 1.0, test_counter_ended)

	sounds.BGM:Play()
end

function deactivate()
	sounds.BGM:Stop()
end

function onStart()
	background = TEXTURE:CreateTexture("Textures/Background.png")
	text = TEXT:Create(16)
	textTex = text:GetText("Do you remember me?...")

	sounds.BGM = SOUND:CreateBGM("Sounds/BGM.ogg")
	sounds.BGM:SetLoop(true)
	sounds.Skip = SOUND:CreateSFX("Sounds/Skip.ogg")
	sounds.Cancel = SOUND:CreateSFX("Sounds/Cancel.ogg")
end

function afterSongEnum()

end

function onDestroy()
	if background ~= nil then
		background:Dispose()
	end
	if textTex ~= nil then
		textTex:Dispose()
	end
	for _, sound in pairs(sounds) do
		sound:Dispose()
	end
end
