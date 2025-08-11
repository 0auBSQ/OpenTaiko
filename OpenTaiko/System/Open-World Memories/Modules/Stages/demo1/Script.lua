local background = nil

local text = nil
local textTex = nil

local sounds = {}

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
		test = GetSaveFile(0)
		debugLog(tostring(test.TotalPlaycount))
		sounds.Cancel:Play()
		return Exit("title", nil)
	end
	if INPUT:Pressed("Decide") == true or INPUT:KeyboardPressed("Return") == true then
		sounds.Skip:Play()
		return Exit("stage", "demo2")
	end
end

function activate()
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
