local background = nil
local sounds = {}

function draw()
	if background ~= nil then
		background:Draw(0,0)
	end

end

function update()
	if INPUT:Pressed("Decide") == true or INPUT:KeyboardPressed("Return") == true then
		sounds.Skip:Play()
		return Exit("title", nil)
	end
end

function activate()
	background = TEXTURE:CreateTexture("Textures/Background.png")
	
	sounds.BGM = SOUND:CreateBGM("Sounds/BGM.ogg")
	sounds.BGM:SetLoop(true)
	sounds.Skip = SOUND:CreateSFX("Sounds/Skip.ogg")
	
	sounds.BGM:Play()
end

function deactivate()
	if background ~= nil then
		background:Dispose()
	end
	for _, sound in pairs(sounds) do
		sound:Dispose()
	end
end

function onStart()

end

function afterSongEnum()

end

function onDestroy()
	
end
