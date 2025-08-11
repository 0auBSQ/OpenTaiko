local text = nil
local textTex = nil

local sounds = {}

function draw()
	if textTex ~= nil then
		textTex:Draw(400,400)
	end 
end

function update()
	if INPUT:Pressed("Cancel") == true or INPUT:KeyboardPressed("Escape") == true then
		sounds.Cancel:Play()
		return Exit("title", nil)
	end
	if INPUT:Pressed("Decide") == true or INPUT:KeyboardPressed("Return") == true then
		sounds.Skip:Play()
		return Exit("stage", "demo1")
	end
end

function activate()
	test = GetSaveFile(0)
	textTex = text:GetText("You played " .. tostring(test.TotalPlaycount) .. " charts...")
end

function deactivate()

end

function onStart()
	text = TEXT:Create(16)

	sounds.Skip = SOUND:CreateSFX("Sounds/Skip.ogg")
	sounds.Cancel = SOUND:CreateSFX("Sounds/Cancel.ogg")
end 

function afterSongEnum()

end

function onDestroy()
	if textTex ~= nil then
		textTex:Dispose()
	end
	for _, sound in pairs(sounds) do
		sound:Dispose()
	end
end
