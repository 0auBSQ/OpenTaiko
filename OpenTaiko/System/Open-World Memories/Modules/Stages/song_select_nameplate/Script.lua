local background = nil

local text16 = nil
local text24 = nil
local text32 = nil
local textTex16 = nil
local textTex24 = nil
local textTex32 = nil

local sounds = {}

local position = 0

function update(timestamp)
    position = position + (100 * fps.deltaTime)  -- 100 pixels per second
end

function draw()
	if background ~= nil then background:Draw(0, 0) end

    for i = 0, CONFIG.PlayerCount - 1 do
        NAMEPLATE:DrawPlayerNameplate(0 + i * 384, 980, 255, i)
    end

	if textTex16 ~= nil then
		textTex16:Draw(0,0)
	end
	if textTex24 ~= nil then
		textTex24:Draw(0,30)
	end
	if textTex32 ~= nil then
		textTex32:Draw(0,70)
	end
end

function update()
	if INPUT:Pressed("Cancel") == true or INPUT:KeyboardPressed("Escape") == true then
		return Exit("title", nil)
	end
end

function onStart()
	text16 = TEXT:Create(16)
	text24 = TEXT:Create(24)
	text32 = TEXT:Create(32)
	textTex16 = text16:GetText("0.6.1 testing... (16px)")
	textTex24 = text24:GetText("0.6.1 testing... (24px)")
	textTex32 = text32:GetText("0.6.1 testing... (32px)")

	background = TEXTURE:CreateTexture("Textures/Background.png")

	sounds.BGM = SOUND:CreateBGM("Sounds/BGM.ogg")
	sounds.BGM:SetLoop(true)
	sounds.Skip = SOUND:CreateSFX("Sounds/Skip.ogg")
	sounds.Cancel = SOUND:CreateSFX("Sounds/Cancel.ogg")
end

function onDestroy()
	if textTex ~= nil then
		textTex:Dispose()
	end
	for _, sound in pairs(sounds) do
		sound:Dispose()
	end
end
