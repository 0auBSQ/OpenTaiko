local background = nil

local text = nil
local textTex = nil

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
		return Exit("title", nil)
	end
end

function activate()
	
end

function deactivate()

end

function onStart()
	background = TEXTURE:CreateTexture("Textures/Background.png")
	text = TEXT:Create(16)
	textTex = text:GetText("Do you remember me?...")
	
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
end