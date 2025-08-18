local OFFSET_MODE_GAME = "game"
local OFFSET_MODE_GAME_BALLOON = "game_balloon"
local OFFSET_MODE_GAME_KUSUDAMA = "game_kusudama"
local OFFSET_MODE_MENU = "menu"
local OFFSET_MODE_RESULT = "result"
local GAME_SCALE = 1.25

local chara_config = {
	chara_version = "";
	resolution = nil;
	use_result_1p = false;

	heya_render_offset = nil;

	game_offset = nil;
	game_balloon_offset = nil;
	game_kusudama_offset = nil;
	game_motion_normal = nil;
	game_motion_clear = nil;
	game_motion_clear_max = nil;
	game_motion_gogo = nil;
	game_motion_gogo_max = nil;
	game_motion_miss = nil;
	game_motion_miss_down = nil;
	game_motion_10combo = nil;
	game_motion_10combo_max = nil;
	game_motion_cleared = nil;
	game_motion_failed = nil;
	game_motion_clearout = nil;
	game_motion_clearin = nil;
	game_motion_soulout = nil;
	game_motion_soulin = nil;
	game_motion_missin = nil;
	game_motion_missdownin = nil;
	game_motion_return = nil;
	game_motion_gogostart = nil;
	game_motion_gogostart_clear = nil;
	game_motion_gogostart_max = nil;
	game_motion_balloon_breaking = nil;
	game_motion_balloon_broke = nil;
	game_motion_balloon_miss = nil;
	game_motion_kusudama_breaking = nil;
	game_motion_kusudama_idle = nil;
	game_motion_kusudama_broke = nil;
	game_motion_kusudama_miss = nil;
	game_beat_normal = 1;
	game_beat_clear = 1;
	game_beat_clear_max = 1;
	game_beat_gogo = 1;
	game_beat_gogo_max = 1;
	game_beat_miss = 1;
	game_beat_miss_down = 1;
	game_beat_10combo = 1.5;
	game_beat_10combo_max = 1.5;
	game_beat_cleared = 1.5;
	game_beat_failed = 1.5;
	game_beat_clearout = 1.5;
	game_beat_clearin = 1.5;
	game_beat_soulout = 1.5;
	game_beat_soulin = 1.5;
	game_beat_missin = 1;
	game_beat_missdownin = 1;
	game_beat_return = 1.5;
	game_beat_gogostart = 1.5;
	game_beat_gogostart_clear = 1.5;
	game_beat_gogostart_max = 1.5;
	game_beat_balloon_breaking = 0.25;
	game_beat_balloon_broke = 1.5;
	game_beat_balloon_miss = 1.5;
	game_beat_kusudama_breaking = 0.25;
	game_beat_kusudama_idle = 0.25;
	game_beat_kusudama_broke = 1.5;
	game_beat_kusudama_miss = 1.5;

	menu_offset = nil;
	menu_motion_normal = nil;
	menu_motion_wait = nil;
	menu_motion_start = nil;
	menu_motion_select = nil;
	menu_beat_normal = 1;
	menu_beat_wait = 1;
	menu_beat_start = 1;
	menu_beat_select = 1;

	title_motion_normal = nil;
	title_motion_entry = nil;
	title_beat_normal = 1;
	title_beat_entry = 1;

	result_offset = nil;
	result_motion_normal = nil;
	result_motion_clear = nil;
	result_motion_failed_in = nil;
	result_motion_failed = nil;
	result_beat_normal = 1;
	result_beat_clear = 1;
	result_beat_failed_in = 1;
	result_beat_failed = 1;
}

local current_loop_animation = { nil, nil, nil, nil, nil }
local current_action_animation = { nil, nil, nil, nil, nil }
local current_animation = { nil, nil, nil, nil, nil }

local function counter_ended(lua_player_index)
	if current_action_animation[lua_player_index] == nil then
		return
	end

	current_action_animation[lua_player_index] = nil

	local animation = current_loop_animation[lua_player_index]
	if animation ~= nil then
		local counter = animation.counter[lua_player_index]
		counter:Reset()
	end
end

CharacterAnimation = {
	id = "";
	textures = { };
	motion = { };
	motion_length = 0;
	beat = 1;
	fallback_animation_name = "";
	offset_mode = "";
	scale = 1.0;
	counter = {};

	new = function(_id, _frames, _motion, _beat, _fallback_animation_name, _offset_mode, _scale)
		local obj = {}

		obj.id = _id
		obj.textures = _frames

		if #_frames ~= 0 then
			if _motion == nil then
				obj.motion = { }
				for i = 1, #_frames, 1 do
					obj.motion[i] = i
				end
			else
				obj.motion = _motion
			end
		else
			obj.motion = { }
		end

		obj.motion_length = #obj.motion

		obj.beat = _beat
		obj.fallback_animation_name = _fallback_animation_name
		obj.offset_mode = _offset_mode
		obj.counter = { }
		for i = 1, 5, 1 do
			obj.counter[i] = COUNTER:CreateCounter(1, obj.motion_length + 1, 1.0 / math.max(obj.motion_length, 1),
			function()
				counter_ended(i)
			end)
		end
		obj.scale = _scale

		return obj
	end;
}

local preview = nil
local render = nil

local animations = { }

local interval = { 1, 1, 1, 1, 1 }


local function create_animation(id, dir_name, motion, beat, fallback_animation_name, offset_mode, _scale)
	local frames = { }

	if STORAGE:DirectoryExists(dir_name) then
		local files = STORAGE:GetFiles(dir_name, "*.png")

		for i = 1, files.Length, 1 do
			local file_path = dir_name.."/"..tostring(i - 1)..".png"
			if STORAGE:FileExists(file_path) then
				frames[i] = TEXTURE:CreateTexture(file_path)
			else
				break
			end
		end
	end

	local animation = CharacterAnimation.new(id, frames, motion, beat, fallback_animation_name, offset_mode, _scale)
	animations[id] = animation
end


local function get_animation(animation_type)
	local animation = animations[animation_type]

	return animation

	--if animation == nil then
	--	return nil
	--end

	--for i = 1, 5, 1 do
	--	if animation == nil then
	--		return nil
	--	elseif animation.motion_length ~= 0 then
	--		return animation
	--	elseif animation.fallback_animation_name ~= "" then
	--		animation = animations[animation.fallback_animation_name]
	--	else
	--		return nil
	--	end
	--end
end


local function csarray_to_motiontable(csarray)
	local luatable = {}
	for i = 1, csarray.Length, 1 do
		luatable[i] = csarray[i - 1] + 1
	end
	return luatable
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

	chara_config.chara_version = chara_config_ini:GetString("Chara_Version", chara_config.chara_version)

	local ini_chara_resolution = chara_config_ini:GetIntArray("Chara_Resolution")
	if ini_chara_resolution.Length == 2 then
		chara_config.resolution = VECTOR2:CreateVector2(ini_chara_resolution[0], ini_chara_resolution[1])
	end

	local ini_heya_chara_render_offset = chara_config_ini:GetIntArray("Heya_Chara_Render_Offset")
	if ini_heya_chara_render_offset.Length == 2 then
		chara_config.heya_render_offset = VECTOR2:CreateVector2(ini_heya_chara_render_offset[0], ini_heya_chara_render_offset[1])
	end

	---Game+++++++++++++
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

	local ini_game_motion_normal = chara_config_ini:GetIntArray("Game_Chara_Motion_Normal")
	if ini_game_motion_normal.Length >= 1 then
		chara_config.game_motion_normal = csarray_to_motiontable(ini_game_motion_normal)
	end

	local ini_game_motion_clear = chara_config_ini:GetIntArray("Game_Chara_Motion_Clear")
	if ini_game_motion_clear.Length >= 1 then
		chara_config.game_motion_clear = csarray_to_motiontable(ini_game_motion_clear)
		chara_config.game_motion_clear_max = chara_config.game_motion_clear
	end

	local ini_game_motion_clear_max = chara_config_ini:GetIntArray("Game_Chara_Motion_Clear_Max")
	if ini_game_motion_clear_max.Length >= 1 then
		chara_config.game_motion_clear_max = csarray_to_motiontable(ini_game_motion_clear_max)
	end

	local ini_game_motion_gogo = chara_config_ini:GetIntArray("Game_Chara_Motion_GoGo")
	if ini_game_motion_gogo.Length >= 1 then
		chara_config.game_motion_gogo = csarray_to_motiontable(ini_game_motion_gogo)
		chara_config.game_motion_gogo_max = chara_config.game_motion_gogo
	end

	local ini_game_motion_gogo_max = chara_config_ini:GetIntArray("Game_Chara_Motion_GoGo_Max")
	if ini_game_motion_gogo_max.Length >= 1 then
		chara_config.game_motion_gogo_max = csarray_to_motiontable(ini_game_motion_gogo_max)
	end

	local ini_game_motion_miss = chara_config_ini:GetIntArray("Game_Chara_Motion_Miss")
	if ini_game_motion_miss.Length >= 1 then
		chara_config.game_motion_miss = csarray_to_motiontable(ini_game_motion_miss)
	end

	local ini_game_motion_miss_down = chara_config_ini:GetIntArray("Game_Chara_Motion_Miss_Down")
	if ini_game_motion_miss_down.Length >= 1 then
		chara_config.game_motion_miss_down = csarray_to_motiontable(ini_game_motion_miss_down)
	end

	local ini_game_motion_10combo = chara_config_ini:GetIntArray("Game_Chara_Motion_10Combo")
	if ini_game_motion_10combo.Length >= 1 then
		chara_config.game_motion_miss_10combo = csarray_to_motiontable(ini_game_motion_10combo)
	end

	local ini_game_motion_10combo_max = chara_config_ini:GetIntArray("Game_Chara_Motion_10Combo_Max")
	if ini_game_motion_10combo_max.Length >= 1 then
		chara_config.game_motion_10combo_max = csarray_to_motiontable(ini_game_motion_10combo_max)
	end

	local ini_game_motion_cleared = chara_config_ini:GetIntArray("Game_Chara_Motion_Cleared")
	if ini_game_motion_cleared.Length >= 1 then
		chara_config.game_motion_cleared = csarray_to_motiontable(ini_game_motion_cleared)
	end

	local ini_game_motion_failed = chara_config_ini:GetIntArray("Game_Chara_Motion_Failed")
	if ini_game_motion_failed.Length >= 1 then
		chara_config.game_motion_failed = csarray_to_motiontable(ini_game_motion_failed)
	end

	local ini_game_motion_clearout = chara_config_ini:GetIntArray("Game_Chara_Motion_Clearout")
	if ini_game_motion_clearout.Length >= 1 then
		chara_config.game_motion_clearout = csarray_to_motiontable(ini_game_motion_clearout)
	end

	local ini_game_motion_clearin = chara_config_ini:GetIntArray("Game_Chara_Motion_ClearIn")
	if ini_game_motion_clearin.Length >= 1 then
		chara_config.game_motion_clearin = csarray_to_motiontable(ini_game_motion_clearin)
	end

	local ini_game_motion_soulout = chara_config_ini:GetIntArray("Game_Chara_Motion_Soulout")
	if ini_game_motion_soulout.Length >= 1 then
		chara_config.game_motion_soulout = csarray_to_motiontable(ini_game_motion_soulout)
	end

	local ini_game_motion_soulin = chara_config_ini:GetIntArray("Game_Chara_Motion_Soulin")
	if ini_game_motion_soulin.Length >= 1 then
		chara_config.game_motion_soulin = csarray_to_motiontable(ini_game_motion_soulin)
	end

	local ini_game_motion_missin = chara_config_ini:GetIntArray("Game_Chara_Motion_MissIn")
	if ini_game_motion_missin.Length >= 1 then
		chara_config.game_motion_missin = csarray_to_motiontable(ini_game_motion_missin)
	end

	local ini_game_motion_missdownin = chara_config_ini:GetIntArray("Game_Chara_Motion_MissDownIn")
	if ini_game_motion_missdownin.Length >= 1 then
		chara_config.game_motion_missdownin = csarray_to_motiontable(ini_game_motion_missdownin)
	end

	local ini_game_motion_gogostart = chara_config_ini:GetIntArray("Game_Chara_Motion_GoGoStart")
	if ini_game_motion_gogostart.Length >= 1 then
		chara_config.game_motion_gogostart = csarray_to_motiontable(ini_game_motion_gogostart)
		chara_config.game_motion_gogostart_clear = chara_config.game_motion_gogostart
		chara_config.game_motion_gogostart_max = chara_config.game_motion_gogostart
	end

	local ini_game_motion_gogostart_clear = chara_config_ini:GetIntArray("Game_Chara_Motion_GoGoStart_Clear")
	if ini_game_motion_gogostart_clear.Length >= 1 then
		chara_config.game_motion_gogostart_clear = csarray_to_motiontable(ini_game_motion_gogostart_clear)
	end

	local ini_game_motion_gogostart_max = chara_config_ini:GetIntArray("Game_Chara_Motion_GoGoStart_Max")
	if ini_game_motion_gogostart_max.Length >= 1 then
		chara_config.game_motion_gogostart_max = csarray_to_motiontable(ini_game_motion_gogostart_max)
	end

	local ini_game_motion_gogostart_max = chara_config_ini:GetIntArray("Game_Chara_Motion_GoGoStart_Max")
	if ini_game_motion_gogostart_max.Length >= 1 then
		chara_config.game_motion_gogostart_max = csarray_to_motiontable(ini_game_motion_gogostart_max)
	end

	local ini_game_motion_return = chara_config_ini:GetIntArray("Game_Chara_Motion_Return")
	if ini_game_motion_return.Length >= 1 then
		chara_config.game_motion_return = csarray_to_motiontable(ini_game_motion_return)
	end

	local ini_game_motion_balloon_breaking = chara_config_ini:GetIntArray("Game_Chara_Motion_Balloon_Breaking")
	if ini_game_motion_balloon_breaking.Length >= 1 then
		chara_config.game_motion_balloon_breaking = csarray_to_motiontable(ini_game_motion_balloon_breaking)
	end

	local ini_game_motion_balloon_broke = chara_config_ini:GetIntArray("Game_Chara_Motion_Balloon_Broke")
	if ini_game_motion_balloon_broke.Length >= 1 then
		chara_config.game_motion_balloon_broke = csarray_to_motiontable(ini_game_motion_balloon_broke)
	end

	local ini_game_motion_balloon_miss = chara_config_ini:GetIntArray("Game_Chara_Motion_Balloon_Miss")
	if ini_game_motion_balloon_miss.Length >= 1 then
		chara_config.game_motion_balloon_miss = csarray_to_motiontable(ini_game_motion_balloon_miss)
	end

	local ini_game_motion_kusudama_breaking = chara_config_ini:GetIntArray("Game_Chara_Motion_Kusudama_Breaking")
	if ini_game_motion_kusudama_breaking.Length >= 1 then
		chara_config.game_motion_kusudama_breaking = csarray_to_motiontable(ini_game_motion_kusudama_breaking)
	end

	local ini_game_motion_kusudama_idle = chara_config_ini:GetIntArray("Game_Chara_Motion_Kusudama_Idle")
	if ini_game_motion_kusudama_idle.Length >= 1 then
		chara_config.game_motion_kusudama_idle = csarray_to_motiontable(ini_game_motion_kusudama_idle)
	end

	local ini_game_motion_kusudama_broke = chara_config_ini:GetIntArray("Game_Chara_Motion_Kusudama_Broke")
	if ini_game_motion_kusudama_broke.Length >= 1 then
		chara_config.game_motion_kusudama_broke = csarray_to_motiontable(ini_game_motion_kusudama_broke)
	end

	local ini_game_motion_kusudama_miss = chara_config_ini:GetIntArray("Game_Chara_Motion_Kusudama_Miss")
	if ini_game_motion_kusudama_miss.Length >= 1 then
		chara_config.game_motion_kusudama_miss = csarray_to_motiontable(ini_game_motion_kusudama_miss)
	end

	chara_config.game_beat_normal = chara_config_ini:GetDouble("Game_Chara_Beat_Normal", chara_config.game_beat_normal)
	chara_config.game_beat_clear = chara_config_ini:GetDouble("Game_Chara_Beat_Clear", chara_config.game_beat_clear)
	chara_config.game_beat_clear_max = chara_config.game_beat_clear
	chara_config.game_beat_clear_max = chara_config_ini:GetDouble("Game_Chara_Beat_ClearMax", chara_config.game_beat_clear_max)
	chara_config.game_beat_gogo = chara_config_ini:GetDouble("Game_Chara_Beat_GoGo", chara_config.game_beat_gogo)
	chara_config.game_beat_gogo_max = chara_config_ini:GetDouble("Game_Chara_Beat_GoGo", chara_config.game_beat_gogo)
	chara_config.game_beat_gogo_max = chara_config.game_beat_gogo
	chara_config.game_beat_miss = chara_config_ini:GetDouble("Game_Chara_Beat_Miss", chara_config.game_beat_miss)
	chara_config.game_beat_miss_down = chara_config_ini:GetDouble("Game_Chara_Beat_MissDown", chara_config.game_beat_miss_down)
	chara_config.game_beat_10combo = chara_config_ini:GetDouble("Game_Chara_Beat_10Combo", chara_config.game_beat_10combo)
	chara_config.game_beat_10combo_max = chara_config_ini:GetDouble("Game_Chara_Beat_10ComboMax", chara_config.game_beat_10combo_max)
	chara_config.game_beat_cleared = chara_config_ini:GetDouble("Game_Chara_Beat_Cleared", chara_config.game_beat_cleared)
	chara_config.game_beat_failed = chara_config_ini:GetDouble("Game_Chara_Beat_Failed", chara_config.game_beat_failed)
	chara_config.game_beat_clearout = chara_config_ini:GetDouble("Game_Chara_Beat_ClearOut", chara_config.game_beat_clearout)
	chara_config.game_beat_clearin = chara_config_ini:GetDouble("Game_Chara_Beat_ClearIn", chara_config.game_beat_clearin)
	chara_config.game_beat_soulout = chara_config_ini:GetDouble("Game_Chara_Beat_SoulOut", chara_config.game_beat_soulout)
	chara_config.game_beat_soulin = chara_config_ini:GetDouble("Game_Chara_Beat_SoulIn", chara_config.game_beat_soulin)
	chara_config.game_beat_missin = chara_config_ini:GetDouble("Game_Chara_Beat_MissIn", chara_config.game_beat_missin)
	chara_config.game_beat_missdownin = chara_config_ini:GetDouble("Game_Chara_Beat_MissDownIn", chara_config.game_beat_missdownin)
	chara_config.game_beat_return = chara_config_ini:GetDouble("Game_Chara_Beat_Return", chara_config.game_beat_return)
	chara_config.game_beat_gogostart = chara_config_ini:GetDouble("Game_Chara_Beat_GoGoStart", chara_config.game_beat_gogostart)
	chara_config.game_beat_gogostart_clear = chara_config_ini:GetDouble("Game_Chara_Beat_GoGoStartClear", chara_config.game_beat_gogostart_clear)
	chara_config.game_beat_gogostart_max = chara_config_ini:GetDouble("Game_Chara_Beat_GoGoStartMax", chara_config.game_beat_gogostart_max)
	chara_config.game_beat_balloon_breaking = chara_config_ini:GetDouble("Game_Chara_Beat_Balloon_Breaking", chara_config.game_beat_balloon_breaking)
	chara_config.game_beat_balloon_broke = chara_config_ini:GetDouble("Game_Chara_Beat_Balloon_Broke", chara_config.game_beat_balloon_broke)
	chara_config.game_beat_balloon_miss = chara_config_ini:GetDouble("Game_Chara_Beat_Balloon_Miss", chara_config.game_beat_balloon_miss)
	chara_config.game_beat_kusudama_breaking = chara_config_ini:GetDouble("Game_Chara_Beat_Kusudama_Breaking", chara_config.game_beat_kusudama_breaking)
	chara_config.game_beat_kusudama_idle = chara_config_ini:GetDouble("Game_Chara_Beat_Kusudama_Idle", chara_config.game_beat_kusudama_idle)
	chara_config.game_beat_kusudama_broke = chara_config_ini:GetDouble("Game_Chara_Beat_Kusudama_Broke", chara_config.game_beat_kusudama_broke)
	chara_config.game_beat_kusudama_miss = chara_config_ini:GetDouble("Game_Chara_Beat_Kusudama_Miss", chara_config.game_beat_kusudama_miss)
	--+++++++++++++

	---Menu+++++++++++++
	local ini_menu_offset = chara_config_ini:GetIntArray("Menu_Offset")
	if ini_menu_offset.Length == 2 then
		chara_config.menu_offset = VECTOR2:CreateVector2(ini_menu_offset[0], ini_menu_offset[1])
	end

	local ini_menu_motion_normal = chara_config_ini:GetIntArray("Menu_Chara_Motion_Loop")
	if ini_menu_motion_normal.Length >= 1 then
		chara_config.menu_motion_normal = csarray_to_motiontable(ini_menu_motion_normal)
	end

	local ini_menu_motion_wait = chara_config_ini:GetIntArray("Menu_Chara_Motion_Wait")
	if ini_menu_motion_wait.Length >= 1 then
		chara_config.menu_motion_wait = csarray_to_motiontable(ini_menu_motion_wait)
	end

	local ini_menu_motion_start = chara_config_ini:GetIntArray("Menu_Chara_Motion_Start")
	if ini_menu_motion_start.Length >= 1 then
		chara_config.menu_motion_start = csarray_to_motiontable(ini_menu_motion_start)
	end

	local ini_menu_motion_select = chara_config_ini:GetIntArray("Menu_Chara_Motion_Select")
	if ini_menu_motion_select.Length >= 1 then
		chara_config.menu_motion_select = csarray_to_motiontable(ini_menu_motion_select)
	end

	chara_config.menu_beat_normal = chara_config_ini:GetDouble("Chara_Menu_Loop_AnimationDuration", chara_config.menu_beat_normal * 1000.0) / 1000.0
	chara_config.menu_beat_normal = chara_config_ini:GetDouble("Menu_Chara_Beat_Normal", chara_config.menu_beat_normal)

	chara_config.menu_beat_wait = chara_config_ini:GetDouble("Chara_Menu_Wait_AnimationDuration", chara_config.menu_beat_wait * 1000.0) / 1000.0
	chara_config.menu_beat_wait = chara_config_ini:GetDouble("Menu_Chara_Beat_Wait", chara_config.menu_beat_wait)

	chara_config.menu_beat_start = chara_config_ini:GetDouble("Chara_Menu_Start_AnimationDuration", chara_config.menu_beat_start * 1000.0) / 1000.0
	chara_config.menu_beat_start = chara_config_ini:GetDouble("Menu_Chara_Beat_Start", chara_config.menu_beat_start)

	chara_config.menu_beat_select = chara_config_ini:GetDouble("Chara_Menu_Select_AnimationDuration", chara_config.menu_beat_select * 1000.0) / 1000.0
	chara_config.menu_beat_select = chara_config_ini:GetDouble("Menu_Chara_Beat_Select", chara_config.menu_beat_select)
	--+++++++++++++

	--Title+++++++++++++
	local ini_title_motion_normal = chara_config_ini:GetIntArray("Title_Chara_Motion_Normal")
	if ini_title_motion_normal.Length >= 1 then
		chara_config.title_motion_normal = csarray_to_motiontable(ini_title_motion_normal)
	end

	local ini_title_motion_entry = chara_config_ini:GetIntArray("Title_Chara_Motion_Entry")
	if ini_title_motion_entry.Length >= 1 then
		chara_config.title_motion_entry = csarray_to_motiontable(ini_title_motion_entry)
	end

	chara_config.title_beat_normal = chara_config_ini:GetDouble("Chara_Normal_AnimationDuration", chara_config.title_beat_normal * 1000.0) / 1000.0
	chara_config.title_beat_normal = chara_config_ini:GetDouble("Title_Chara_Beat_Normal", chara_config.title_beat_normal)

	chara_config.title_beat_entry = chara_config_ini:GetDouble("Chara_Entry_AnimationDuration", chara_config.title_beat_entry * 1000.0) / 1000.0
	chara_config.title_beat_entry = chara_config_ini:GetDouble("Title_Chara_Beat_Entry", chara_config.menu_beat_select)
	--+++++++++++++

	--Result+++++++++++++
	local ini_result_offset = chara_config_ini:GetIntArray("Result_Offset")
	if ini_result_offset.Length == 2 then
		chara_config.result_offset = VECTOR2:CreateVector2(ini_result_offset[0], ini_result_offset[1])
	end

	local ini_result_motion_normal = chara_config_ini:GetIntArray("Result_Chara_Motion_Normal")
	if ini_result_motion_normal.Length >= 1 then
		chara_config.result_motion_normal = csarray_to_motiontable(ini_result_motion_normal)
	end

	local ini_result_motion_clear = chara_config_ini:GetIntArray("Result_Chara_Motion_Clear")
	if ini_result_motion_clear.Length >= 1 then
		chara_config.result_motion_clear = csarray_to_motiontable(ini_result_motion_clear)
	end

	local ini_result_motion_failed_in = chara_config_ini:GetIntArray("Result_Chara_Motion_Failed_In")
	if ini_result_motion_failed_in.Length >= 1 then
		chara_config.result_motion_failed_in = csarray_to_motiontable(ini_result_motion_failed_in)
	end

	local ini_result_motion_failed = chara_config_ini:GetIntArray("Result_Chara_Motion_Failed")
	if ini_result_motion_failed.Length >= 1 then
		chara_config.result_motion_failed = csarray_to_motiontable(ini_result_motion_failed)
	end

	chara_config.result_beat_normal = chara_config_ini:GetDouble("Chara_Result_Normal_AnimationDuration", chara_config.result_beat_normal * 1000.0) / 1000.0
	chara_config.result_beat_normal = chara_config_ini:GetDouble("Result_Chara_Beat_Normal", chara_config.result_beat_normal)

	chara_config.result_beat_clear = chara_config_ini:GetDouble("Chara_Result_Clear_AnimationDuration", chara_config.result_beat_clear * 1000.0) / 1000.0
	chara_config.result_beat_clear = chara_config_ini:GetDouble("Result_Chara_Motion_Clear", chara_config.result_beat_clear)

	chara_config.result_beat_failed_in = chara_config_ini:GetDouble("Chara_Result_Failed_In_AnimationDuration", chara_config.result_beat_failed_in * 1000.0) / 1000.0
	chara_config.result_beat_failed_in = chara_config_ini:GetDouble("Result_Chara_Motion_Failed_In", chara_config.result_beat_failed_in)

	chara_config.result_beat_failed = chara_config_ini:GetDouble("Chara_Result_Failed_AnimationDuration", chara_config.result_beat_failed * 1000.0) / 1000.0
	chara_config.result_beat_failed = chara_config_ini:GetDouble("Result_Chara_Motion_Failed", chara_config.result_beat_failed)
	--+++++++++++++
end


function loadPreviewTextures()
	load_chara_config()

	if STORAGE:FileExists("Normal/0.png") then
		preview = TEXTURE:CreateTexture("Normal/0.png")
	end
	if STORAGE:FileExists("Render.png") then
		render = TEXTURE:CreateTexture("Render.png")
	end

	create_animation(CHARACTER.ANIM_GAME_NORMAL, "Normal", chara_config.game_motion_normal, chara_config.game_beat_normal, "", OFFSET_MODE_GAME, GAME_SCALE)
	create_animation(CHARACTER.ANIM_GAME_CLEAR, "Clear", chara_config.game_motion_clear, chara_config.game_beat_clear, CHARACTER.ANIM_GAME_NORMAL, OFFSET_MODE_GAME, GAME_SCALE)
	create_animation(CHARACTER.ANIM_GAME_MAX, "Clear_Max", chara_config.game_motion_clear_max, chara_config.game_beat_clear_max, CHARACTER.ANIM_GAME_CLEAR, OFFSET_MODE_GAME, GAME_SCALE)
	create_animation(CHARACTER.ANIM_GAME_GOGO, "GoGo", chara_config.game_motion_gogo, chara_config.game_beat_gogo, CHARACTER.ANIM_GAME_NORMAL, OFFSET_MODE_GAME, GAME_SCALE)
	create_animation(CHARACTER.ANIM_GAME_GOGO_MAX, "GoGo_Max", chara_config.game_motion_gogo_max, chara_config.game_beat_gogo_max, CHARACTER.ANIM_GAME_GOGO, OFFSET_MODE_GAME, GAME_SCALE)
	create_animation(CHARACTER.ANIM_GAME_MISS, "Miss", chara_config.game_motion_miss, chara_config.game_beat_miss, "", OFFSET_MODE_GAME, GAME_SCALE)
	create_animation(CHARACTER.ANIM_GAME_MISS_DOWN, "MissDown", chara_config.game_motion_miss_down, chara_config.game_beat_miss_down, "", OFFSET_MODE_GAME, GAME_SCALE)
	create_animation(CHARACTER.ANIM_GAME_10COMBO, "10combo", chara_config.game_motion_10combo, chara_config.game_beat_10combo, "", OFFSET_MODE_GAME, GAME_SCALE)
	create_animation(CHARACTER.ANIM_GAME_10COMBO_MAX, "10combo_Max", chara_config.game_motion_10combo_max, chara_config.game_beat_10combo_max, CHARACTER.ANIM_GAME_10COMBO, OFFSET_MODE_GAME, GAME_SCALE)
	create_animation(CHARACTER.ANIM_GAME_CLEARED, "Cleared", chara_config.game_motion_cleared, chara_config.game_beat_cleared, "", OFFSET_MODE_GAME, GAME_SCALE)
	create_animation(CHARACTER.ANIM_GAME_FAILED, "Failed", chara_config.game_motion_failed, chara_config.game_beat_failed, "", OFFSET_MODE_GAME, GAME_SCALE)
	create_animation(CHARACTER.ANIM_GAME_CLEAR_OUT, "Clearout", chara_config.game_motion_clearout, chara_config.game_beat_clearout, "", OFFSET_MODE_GAME, GAME_SCALE)
	create_animation(CHARACTER.ANIM_GAME_CLEAR_IN, "Clearin", chara_config.game_motion_clearin, chara_config.game_beat_clearin, "", OFFSET_MODE_GAME, GAME_SCALE)
	create_animation(CHARACTER.ANIM_GAME_MAX_OUT, "Soulout", chara_config.game_motion_soulout, chara_config.game_beat_soulout, "", OFFSET_MODE_GAME, GAME_SCALE)
	create_animation(CHARACTER.ANIM_GAME_MAX_IN, "Soulin", chara_config.game_motion_soulin, chara_config.game_beat_soulin, "", OFFSET_MODE_GAME, GAME_SCALE)
	create_animation(CHARACTER.ANIM_GAME_MISS_IN, "MissIn", chara_config.game_motion_missin, chara_config.game_beat_missin, "", OFFSET_MODE_GAME, GAME_SCALE)
	create_animation(CHARACTER.ANIM_GAME_MISS_DOWN_IN, "MissDownIn", chara_config.game_motion_missdownin, chara_config.game_beat_missdownin, "", OFFSET_MODE_GAME, GAME_SCALE)
	create_animation(CHARACTER.ANIM_GAME_RETURN, "Return", chara_config.game_motion_return, chara_config.game_beat_return, "", OFFSET_MODE_GAME, GAME_SCALE)
	create_animation(CHARACTER.ANIM_GAME_GOGOSTART, "GoGoStart", chara_config.game_motion_gogostart, chara_config.game_beat_gogostart, "", OFFSET_MODE_GAME, GAME_SCALE)
	create_animation(CHARACTER.ANIM_GAME_GOGOSTART_CLEAR, "GoGoStart_Clear", chara_config.game_motion_gogostart_clear, chara_config.game_beat_gogostart_clear, CHARACTER.ANIM_GAME_GOGOSTART, OFFSET_MODE_GAME, GAME_SCALE)
	create_animation(CHARACTER.ANIM_GAME_GOGOSTART_MAX, "GoGoStart_Max", chara_config.game_motion_gogostart_max, chara_config.game_beat_gogostart_max, CHARACTER.ANIM_GAME_GOGOSTART_CLEAR, OFFSET_MODE_GAME, GAME_SCALE)
	create_animation(CHARACTER.ANIM_GAME_BALLOON_BREAKING, "Balloon_Breaking", chara_config.game_motion_balloon_breaking, chara_config.game_beat_balloon_breaking, CHARACTER.ANIM_GAME_GOGO, OFFSET_MODE_GAME_BALLOON, 1.0)
	create_animation(CHARACTER.ANIM_GAME_BALLOON_BROKE, "Balloon_Broke", chara_config.game_motion_balloon_broke, chara_config.game_beat_balloon_broke, CHARACTER.ANIM_GAME_10COMBO, OFFSET_MODE_GAME_BALLOON, 1.0)
	create_animation(CHARACTER.ANIM_GAME_BALLOON_MISS, "Balloon_Miss", chara_config.game_motion_balloon_miss, chara_config.game_beat_balloon_miss, CHARACTER.ANIM_GAME_MISS, OFFSET_MODE_GAME_BALLOON, 1.0)
	create_animation(CHARACTER.ANIM_GAME_KUSUDAMA_BREAKING, "Kusudama_Breaking", chara_config.game_motion_kusudama_breaking, chara_config.game_beat_kusudama_breaking, CHARACTER.ANIM_GAME_BALLOON_BREAKING, OFFSET_MODE_GAME_KUSUDAMA, 1.0)
	create_animation(CHARACTER.ANIM_GAME_KUSUDAMA_IDLE, "Kusudama_Idle", chara_config.game_motion_kusudama_idle, chara_config.game_beat_kusudama_idle, CHARACTER.ANIM_GAME_NORMAL, OFFSET_MODE_GAME_KUSUDAMA, 1.0)
	create_animation(CHARACTER.ANIM_GAME_KUSUDAMA_BROKE, "Kusudama_Broke", chara_config.game_motion_kusudama_broke, chara_config.game_beat_kusudama_broke, CHARACTER.ANIM_GAME_BALLOON_BROKE, OFFSET_MODE_GAME_KUSUDAMA, 1.0)
	create_animation(CHARACTER.ANIM_GAME_KUSUDAMA_MISS, "Kusudama_Miss", chara_config.game_motion_kusudama_miss, chara_config.game_beat_kusudama_miss, CHARACTER.ANIM_GAME_BALLOON_MISS, OFFSET_MODE_GAME_KUSUDAMA, 1.0)

	create_animation(CHARACTER.ANIM_MENU_NORMAL, "Menu_Loop", chara_config.menu_motion_normal, chara_config.menu_beat_normal, CHARACTER.ANIM_GAME_NORMAL, OFFSET_MODE_MENU, 1.0)
	create_animation(CHARACTER.ANIM_MENU_WAIT, "Menu_Wait", chara_config.menu_motion_wait, chara_config.menu_beat_wait, CHARACTER.ANIM_MENU_NORMAL, OFFSET_MODE_MENU, 1.0)
	create_animation(CHARACTER.ANIM_MENU_START, "Menu_Start", chara_config.menu_motion_start, chara_config.menu_beat_start, CHARACTER.ANIM_GAME_10COMBO, OFFSET_MODE_MENU, 1.0)
	create_animation(CHARACTER.ANIM_MENU_SELECT, "Menu_Select", chara_config.menu_motion_select, chara_config.menu_beat_select, CHARACTER.ANIM_GAME_10COMBO_MAX, OFFSET_MODE_MENU, 1.0)

	create_animation(CHARACTER.ANIM_ENTRY_NORMAL, "Title_Normal", chara_config.title_motion_normal, chara_config.title_beat_normal, CHARACTER.ANIM_GAME_NORMAL, OFFSET_MODE_MENU, 1.0)
	create_animation(CHARACTER.ANIM_ENTRY_JUMP, "Title_Entry", chara_config.title_motion_entry, chara_config.title_beat_entry, CHARACTER.ANIM_GAME_10COMBO, OFFSET_MODE_MENU, 1.0)

	create_animation(CHARACTER.ANIM_RESULT_NORMAL, "Result_Normal", chara_config.result_motion_normal, chara_config.result_beat_normal, CHARACTER.ANIM_GAME_NORMAL, OFFSET_MODE_RESULT, 1.0)
	create_animation(CHARACTER.ANIM_RESULT_CLEAR, "Result_Clear", chara_config.result_motion_clear, chara_config.result_beat_clear, CHARACTER.ANIM_GAME_10COMBO, OFFSET_MODE_RESULT, 1.0)
	create_animation(CHARACTER.ANIM_RESULT_FAILED_IN, "Result_Failed_In", chara_config.result_motion_failed_in, chara_config.result_beat_failed_in, CHARACTER.ANIM_GAME_MISS_DOWN_IN, OFFSET_MODE_RESULT, 1.0)
	create_animation(CHARACTER.ANIM_RESULT_FAILED, "Result_Failed", chara_config.result_motion_failed, chara_config.result_beat_failed, CHARACTER.ANIM_GAME_MISS_DOWN, OFFSET_MODE_RESULT, 1.0)

	for key, animation in pairs(animations) do
		if animation.motion_length == 0 then
			local fallback_animation = animations[animation.fallback_animation_name]
			for i = 1, 5, 1 do

				if fallback_animation == nil then
					break
				elseif fallback_animation.motion_length ~= 0 then
					animation.textures = fallback_animation.textures
					animation.motion = fallback_animation.motion
					animation.motion_length = fallback_animation.motion_length
					animation.counter = fallback_animation.counter
					--animation.beat = fallback_animation.beat
					--animation.offset_mode = fallback_animation.offset_mode
					--animation.scale = fallback_animation.scale
					break
				else
					fallback_animation = animations[fallback_animation.fallback_animation_name]
				end

			end
		end
	end
end


function loadStoryTextures()
end


function loadGeneralTextures()

end


function disposePreviewTextures()
	for key, value in pairs(animations) do
		for j = 1, #value.textures, 1 do
			value.textures[j]:Dispose()
		end
	end
	animations = { }

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
	local lua_player_index = player + 1

	if current_action_animation[lua_player_index] ~= nil then
		current_animation[lua_player_index] = current_action_animation[lua_player_index]
	else
		current_animation[lua_player_index] = current_loop_animation[lua_player_index]
	end

	local animation = current_animation[lua_player_index]
	if animation ~= nil then
		local counter = animation.counter[lua_player_index]
		local length = animation.motion_length

		counter:SetInterval(interval[lua_player_index] * animation.beat / math.max(length, 1))
		counter:Tick()
	end
end


function draw(player, x, y, scaleX, scaleY, opacity, color, flipX)
	local lua_player_index = player + 1

	if flipX then
		scaleX = scaleX * -1
	end

	local theme_resolution = THEME:GetResolution()
	local baseScale = theme_resolution.Y / chara_config.resolution.Y

	local frame = preview
	local animation = current_animation[lua_player_index]

	if animation ~= nil then

		local length = animation.motion_length
		local counter = animation.counter[lua_player_index]
		local motion_index = math.floor(counter.Value)
		motion_index = math.max(math.min(motion_index, length), 1)

		local frame_index = animation.motion[motion_index]

		frame = animation.textures[frame_index]

		if frame ~= nil then
			frame:SetScale(baseScale * scaleX * animation.scale, baseScale * scaleY * animation.scale)
			frame:SetOpacity(opacity / 255.0)
			frame:SetColor(color)
		end

		if animation.offset_mode == OFFSET_MODE_GAME then
			x = x + chara_config.game_offset.X * baseScale
			y = y + chara_config.game_offset.Y * baseScale

			if frame ~= nil then
				x = x + (((frame.Width / 2.0) - (0.13020833333 * chara_config.resolution.X)) * frame:GetScale().X)
				y = y + ((frame.Height - (0.25555555555 * chara_config.resolution.Y)) * frame:GetScale().Y)
			end
		elseif animation.offset_mode == OFFSET_MODE_GAME_BALLOON then
			x = x + chara_config.game_balloon_offset.X * baseScale
			y = y + chara_config.game_balloon_offset.Y * baseScale

			if frame ~= nil then
				x = x + (((frame.Width / 2.0) - (0.27216666666 * chara_config.resolution.X)) * frame:GetScale().X)
				y = y + ((frame.Height - (0.26296296296 * chara_config.resolution.Y)) * frame:GetScale().Y)
			end
		elseif animation.offset_mode == OFFSET_MODE_GAME_KUSUDAMA then
			x = x + chara_config.game_kusudama_offset.X * baseScale
			y = y + chara_config.game_kusudama_offset.Y * baseScale

			if frame ~= nil then
				x = x + (((frame.Width / 2.0) - (0.2555 * chara_config.resolution.X)) * frame:GetScale().X)
				y = y + ((frame.Height - (0.2555 * chara_config.resolution.Y)) * frame:GetScale().Y)
			end
		elseif animation.offset_mode == OFFSET_MODE_MENU then
			x = x + chara_config.menu_offset.X * baseScale
			y = y + chara_config.menu_offset.Y * baseScale
		elseif animation.offset_mode == OFFSET_MODE_RESULT then
			x = x + chara_config.result_offset.X * baseScale
			y = y + chara_config.result_offset.Y * baseScale
		end
	end

	if frame ~= nil then
		frame:DrawAtAnchor(x, y, "bottom")
	end
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


function setLoopAnimation(player, animationType, loop)
	local lua_player_index = player + 1

	local next_loop_animation = get_animation(animationType)
	if next_loop_animation ~= nil then
		local counter = next_loop_animation.counter[lua_player_index]

		counter:SetLoop(loop)
		counter:Start()

		current_loop_animation[lua_player_index] = next_loop_animation
	end
end


function playAnimation(player, animationType)
	local lua_player_index = player + 1

	local next_animation = get_animation(animationType)
	if next_animation ~= nil then
		local counter = next_animation.counter[lua_player_index]

		counter:SetLoop(false)
		counter:Start()

		current_action_animation[lua_player_index] = next_animation
	end
end


function playVoice(voiceType)
end


function setAnimationDuration(player, ms)
	local lua_player_index = player + 1
	interval[lua_player_index] = ms / 1000.0
end


function setAnimationCyclesToBPM(player, bpm)
	local lua_player_index = player + 1
	interval[lua_player_index] = (60.0 / math.abs(bpm))
end
