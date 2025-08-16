
local preview = nil
local render = nil

local chara_config = {
	chara_version = "";
	resolution = nil;
	heya_render_offset = nil;
	menu_offset = nil;
	result_offset = nil;
	game_offset = nil;
	game_balloon_offset = nil;
	game_kusudama_offset = nil;
	use_result_1p = false
}

CharacterAnimation = {
	new = function(textures, motion, beat, fallback_animation_name)
	end;

	test = function()
	end
}

local animations = {  }


local function create_animation(id, dir_name, motion, beat, fallback_animation_name)

end


local function load_chara_config()
	chara_config.chara_version = ""
	chara_config.resolution = VECTOR2:CreateVector2(1280, 720)
	chara_config.heya_render_offset = VECTOR2:CreateVector2(0, 0)
	chara_config.menu_offset = VECTOR2:CreateVector2(0, 0)
	chara_config.result_offset = VECTOR2:CreateVector2(0, 0)
	chara_config.game_offset = VECTOR2:CreateVector2(0, 0)
	chara_config.game_balloon_offset = VECTOR2:CreateVector2(0, 0)
	chara_config.game_kusudama_offset = VECTOR2:CreateVector2(0, 0)
	chara_config.use_result_1p = false

	local chara_config_ini = INILOADER:LoadIni("CharaConfig.txt")

	chara_config.chara_version = chara_config_ini:GetString("Chara_Version")

	local ini_chara_resolution = chara_config_ini:GetIntArray("Chara_Resolution")
	if ini_chara_resolution.Length == 2 then
		chara_config.resolution = VECTOR2:CreateVector2(ini_chara_resolution[0], ini_chara_resolution[1])
	end

	local ini_heya_chara_render_offset = chara_config_ini:GetIntArray("Heya_Chara_Render_Offset")
	if ini_heya_chara_render_offset.Length == 2 then
		chara_config.heya_render_offset = VECTOR2:CreateVector2(ini_heya_chara_render_offset[0], ini_heya_chara_render_offset[1])
	end

	local ini_menu_offset = chara_config_ini:GetIntArray("Menu_Offset")
	if ini_menu_offset.Length == 2 then
		chara_config.menu_offset = VECTOR2:CreateVector2(ini_menu_offset[0], ini_menu_offset[1])
	end

	local ini_result_offset = chara_config_ini:GetIntArray("Result_Offset")
	if ini_result_offset.Length == 2 then
		chara_config.result_offset = VECTOR2:CreateVector2(ini_result_offset[0], ini_result_offset[1])
	end

	local ini_game_offset = chara_config_ini:GetIntArray("Game_Offset")
	if ini_game_offset.Length == 2 then
		chara_config.game_offset = VECTOR2:CreateVector2(ini_game_offset[0], ini_game_offset[1])
	end

	local ini_game_chara_x = chara_config_ini:GetIntArray("Game_Chara_X")
	if ini_game_chara_x.Length >= 1 then
		chara_config.game_offset.X = ini_game_chara_x[0]
	end

	local ini_game_chara_y = chara_config_ini:GetIntArray("Game_Chara_Y")
	if ini_game_chara_y.Length >= 1 then
		chara_config.game_offset.Y = ini_game_chara_y[0]
	end

	local ini_game_balloon_offset = chara_config_ini:GetIntArray("Game_Balloon_Offset")
	if ini_game_balloon_offset.Length == 2 then
		chara_config.game_balloon_offset = VECTOR2:CreateVector2(ini_game_balloon_offset[0], ini_game_balloon_offset[1])
	end

	local ini_game_balloon_x = chara_config_ini:GetIntArray("Game_Chara_Balloon_X")
	if ini_game_balloon_x.Length >= 1 then
		chara_config.game_balloon_offset.X = ini_game_balloon_x[0]
	end

	local ini_game_balloon_y = chara_config_ini:GetIntArray("Game_Chara_Balloon_Y")
	if ini_game_balloon_y.Length >= 1 then
		chara_config.game_balloon_offset.Y = ini_game_balloon_y[0]
	end

	local ini_game_kusudama_offset = chara_config_ini:GetIntArray("Game_Kusudama_Offset")
	if ini_game_kusudama_offset.Length == 2 then
		chara_config.game_balloon_offset = VECTOR2:CreateVector2(ini_game_kusudama_offset[0], ini_game_kusudama_offset[1])
	end

	local ini_game_kusudama_x = chara_config_ini:GetIntArray("Game_Chara_Kusudama_X")
	if ini_game_kusudama_x.Length >= 1 then
		chara_config.game_kusudama_offset.X = ini_game_kusudama_x[0]
	end

	local ini_game_kusudama_y = chara_config_ini:GetIntArray("Game_Chara_Kusudama_Y")
	if ini_game_kusudama_y.Length >= 1 then
		chara_config.game_kusudama_offset.Y = ini_game_kusudama_y[0]
	end
end


function loadPreviewTextures()
	load_chara_config()

	preview = TEXTURE:CreateTexture("Normal/0.png")
	render = TEXTURE:CreateTexture("Render.png")
end


function loadStoryTextures()
end


function loadGeneralTextures()
end


function disposePreviewTextures()
	if preview ~= nil then
		preview:Dispose()
	end
	if render ~= nil then
		render:Dispose()
	end
end


function disposeStoryTextures()
end


function disposeGeneralTextures()
end


function update(player)
end


function draw(player, x, y, scaleX, scaleY, opacity, color, flipX)
	if flipX then
		scaleX = scaleX * -1
	end

	local theme_resolution = THEME:GetResolution()
	local baseScale = theme_resolution.Y / chara_config.resolution.Y
end


function drawPreview(x, y, scaleX, scaleY, opacity, color, flipX)
	if flipX then
		scaleX = scaleX * -1
	end

	local theme_resolution = THEME:GetResolution()
	local baseScale = theme_resolution.Y / chara_config.resolution.Y

	if preview ~= nil then
		preview:SetScale(baseScale * scaleX, baseScale * scaleY)
		preview:SetOpacity(opacity / 255.0)
		preview:SetColor(color)

		preview:DrawAtAnchor(x, y, "Center")
	end
end


function drawHeyaRender(x, y, scaleX, scaleY, opacity, color, flipX)
	if flipX then
		scaleX = scaleX * -1
	end

	local theme_resolution = THEME:GetResolution()
	local baseScale = theme_resolution.Y / chara_config.resolution.Y

	if render ~= nil then
		render:SetScale(baseScale * scaleX, baseScale * scaleY)
		render:SetOpacity(opacity / 255.0)
		render:SetColor(color)

		x = x + (chara_config.heya_render_offset.X * render:GetScale().X)
		y = y + (chara_config.heya_render_offset.Y * render:GetScale().Y)

		render:Draw(x, y)
	end
end


function setLoopAnimation(player, animationType, loo)
end


function playAnimation(player, animationType)
end


function playVoice(voiceType)
end


function setAnimationDuration(player, ms)
end


function setAnimationCyclesToBPM(player, bpm)
end
