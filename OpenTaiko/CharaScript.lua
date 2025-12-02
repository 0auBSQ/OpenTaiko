local OFFSET_MODE_MENU = "menu"
local OFFSET_MODE_RESULT = "result"
local OFFSET_MODE_GAME = "game"
local OFFSET_MODE_GAME_BALLOON = "game_balloon"
local OFFSET_MODE_GAME_KUSUDAMA = "game_kusudama"
local OFFSET_MODE_GAME_TOWER = "game_tower"

local voices = {}
local voice_files = {
	[CHARACTER.VOICE_END_FAILED] = "Sounds/Clear/Failed.ogg";
	[CHARACTER.VOICE_END_CLEAR] = "Sounds/Clear/Clear.ogg";
	[CHARACTER.VOICE_END_FULLCOMBO] = "Sounds/Clear/FullCombo.ogg";
	[CHARACTER.VOICE_END_ALLPERFECT] = "Sounds/Clear/AllPerfect.ogg";
	[CHARACTER.VOICE_END_AIBATTLE_WIN] = "Sounds/Clear/AIBattle_Win.ogg";
	[CHARACTER.VOICE_END_AIBATTLE_LOSE] = "Sounds/Clear/AIBattle_Lose.ogg";

	[CHARACTER.VOICE_MENU_SONGSELECT] = "Sounds/Menu/SongSelect.ogg";
	[CHARACTER.VOICE_MENU_SONGDECIDE] = "Sounds/Menu/SongDecide.ogg";
	[CHARACTER.VOICE_MENU_SONGDECIDE_AI] = "Sounds/Menu/SongDecide_AI.ogg";
	[CHARACTER.VOICE_MENU_DIFFSELECT] = "Sounds/Menu/DiffSelect.ogg";
	[CHARACTER.VOICE_MENU_DANSELECTSTART] = "Sounds/Menu/DanSelectStart.ogg";
	[CHARACTER.VOICE_MENU_DANSELECTPROMPT] = "Sounds/Menu/DanSelectPrompt.ogg";
	[CHARACTER.VOICE_MENU_DANSELECTCONFIRM] = "Sounds/Menu/DanSelectConfirm.ogg";

	[CHARACTER.VOICE_TITLE_SANKA] = "Sounds/Title/Sanka.ogg";

	[CHARACTER.VOICE_TOWER_MISS] = "Sounds/Tower/Miss.ogg";

	[CHARACTER.VOICE_RESULT_BESTSCORE] = "Sounds/Result/BestScore.ogg";
	[CHARACTER.VOICE_RESULT_CLEARFAILED] = "Sounds/Result/ClearFailed.ogg";
	[CHARACTER.VOICE_RESULT_CLEARSUCCESS] = "Sounds/Result/ClearSuccess.ogg";
	[CHARACTER.VOICE_RESULT_DANFAILED] = "Sounds/Result/DanFailed.ogg";
	[CHARACTER.VOICE_RESULT_DANREDPASS] = "Sounds/Result/DanRedPass.ogg";
	[CHARACTER.VOICE_RESULT_DANGOLDPASS] = "Sounds/Result/DanGoldPass.ogg";
}

local animation_dirs = {
	[CHARACTER.ANIM_GAME_NORMAL] = "Normal";
	[CHARACTER.ANIM_GAME_CLEAR] = "Clear";
	[CHARACTER.ANIM_GAME_MAX] = "Clear_Max";
	[CHARACTER.ANIM_GAME_GOGO] = "Gogo";
	[CHARACTER.ANIM_GAME_GOGO_MAX] = "Gogo_Max";
	[CHARACTER.ANIM_GAME_MISS] = "Miss";
	[CHARACTER.ANIM_GAME_MISS_DOWN] = "MissDown";
	[CHARACTER.ANIM_GAME_10COMBO] = "10combo";
	[CHARACTER.ANIM_GAME_10COMBO_MAX] = "10combo_Max";
	[CHARACTER.ANIM_GAME_CLEARED] = "Cleared";
	[CHARACTER.ANIM_GAME_FAILED] = "Failed";
	[CHARACTER.ANIM_GAME_CLEAR_OUT] = "Clearout";
	[CHARACTER.ANIM_GAME_CLEAR_IN] = "Clearin";
	[CHARACTER.ANIM_GAME_MAX_OUT] = "SoulOut";
	[CHARACTER.ANIM_GAME_MAX_IN] = "SoulIn";
	[CHARACTER.ANIM_GAME_MISS_IN] = "MissIn";
	[CHARACTER.ANIM_GAME_MISS_DOWN_IN] = "MissDownIn";
	[CHARACTER.ANIM_GAME_RETURN] = "Return";
	[CHARACTER.ANIM_GAME_GOGOSTART] = "GoGoStart";
	[CHARACTER.ANIM_GAME_GOGOSTART_CLEAR] = "GoGoStart_Clear";
	[CHARACTER.ANIM_GAME_GOGOSTART_MAX] = "GoGoStart_Max";
	[CHARACTER.ANIM_GAME_BALLOON_BREAKING] = "Balloon_Breaking";
	[CHARACTER.ANIM_GAME_BALLOON_BROKE] = "Balloon_Broke";
	[CHARACTER.ANIM_GAME_BALLOON_MISS] = "Balloon_Miss";
	[CHARACTER.ANIM_GAME_KUSUDAMA_BREAKING] = "Kusudama_Breaking";
	[CHARACTER.ANIM_GAME_KUSUDAMA_BROKE] = "Kusudama_Broke";
	[CHARACTER.ANIM_GAME_KUSUDAMA_MISS] = "Kusudama_Miss";
	[CHARACTER.ANIM_GAME_KUSUDAMA_IDLE] = "Kusudama_Idle";

	[CHARACTER.ANIM_GAME_TOWER_STANDING] = "Tower_Char/Standing";
	[CHARACTER.ANIM_GAME_TOWER_STANDING_TIRED] = "Tower_Char/Standing_Tired";
	[CHARACTER.ANIM_GAME_TOWER_CLIMBING] = "Tower_Char/Climbing";
	[CHARACTER.ANIM_GAME_TOWER_CLIMBING_TIRED] = "Tower_Char/Climbing_Tired";
	[CHARACTER.ANIM_GAME_TOWER_RUNNING] = "Tower_Char/Running";
	[CHARACTER.ANIM_GAME_TOWER_RUNNING_TIRED] = "Tower_Char/Running_Tired";
	[CHARACTER.ANIM_GAME_TOWER_CLEAR] = "Tower_Char/Clear";
	[CHARACTER.ANIM_GAME_TOWER_CLEAR_TIRED] = "Tower_Char/Clear_Tired";
	[CHARACTER.ANIM_GAME_TOWER_FAIL] = "Tower_Char/Fail";

	[CHARACTER.ANIM_MENU_WAIT] = "Menu_Wait";
	[CHARACTER.ANIM_MENU_START] = "Menu_Start";
	[CHARACTER.ANIM_MENU_NORMAL] = "Menu_Loop";
	[CHARACTER.ANIM_MENU_SELECT] = "Menu_Select";
	[CHARACTER.ANIM_ENTRY_NORMAL] = "Entry_Normal";
	[CHARACTER.ANIM_ENTRY_JUMP] = "Entry_Jump";

	[CHARACTER.ANIM_RESULT_NORMAL] = "Result_Normal";
	[CHARACTER.ANIM_RESULT_CLEAR] = "Result_Clear";
	[CHARACTER.ANIM_RESULT_FAILED_IN] = "Result_Failed_In";
	[CHARACTER.ANIM_RESULT_FAILED] = "Result_Failed";
}

local chara_config = {
	chara_version = "";
	resolution = VECTOR2:CreateVector2(1280, 720);
	legacy_mode = true;
	heya_render_offset = VECTOR2:CreateVector2(0, 0);
	menu_offset = { pos = VECTOR2:CreateVector2(0, 0); mode = OFFSET_MODE_MENU; v1_scale = 1; };
	menu_motion_normal = nil;
	menu_motion_wait = nil;
	menu_motion_start = nil;
	menu_motion_select = nil;
	menu_beat_normal = 2;
	menu_beat_wait = 1;
	menu_beat_start = 1;
	menu_beat_select = 2;

	title_motion_normal = nil;
	title_motion_entry = nil;
	title_beat_normal = 1;
	title_beat_entry = 1;

	game_offset = { pos = VECTOR2:CreateVector2(0, 0); mode = OFFSET_MODE_GAME; v1_scale = 1.25; };
	game_balloon_offset = { pos = VECTOR2:CreateVector2(0, 0); mode = OFFSET_MODE_GAME_BALLOON; v1_scale = 1; };
	game_kusudama_offset = { pos = VECTOR2:CreateVector2(0, 0); mode = OFFSET_MODE_GAME_KUSUDAMA; v1_scale = 1; };
	game_chara_tower_offset = { pos = VECTOR2:CreateVector2(0, 0); mode = OFFSET_MODE_GAME_TOWER; v1_scale = 1; };
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

	game_motion_tower_standing = nil;
	game_motion_tower_standing_tired = nil;
	game_motion_tower_climbing = nil;
	game_motion_tower_climbing_tired = nil;
	game_motion_tower_running = nil;
	game_motion_tower_running_tired = nil;
	game_motion_tower_clear = nil;
	game_motion_tower_clear_tired = nil;
	game_motion_tower_fail = nil;

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
	game_beat_balloon_broke = 2.0;
	game_beat_balloon_miss = 2.0;
	game_beat_kusudama_breaking = 0.25;
	game_beat_kusudama_idle = 1.0;
	game_beat_kusudama_broke = 2.0;
	game_beat_kusudama_miss = 2.0;

	game_beat_tower_standing = 1.0;
	game_beat_tower_standing_tired = 1.0;
	game_beat_tower_climbing = 1.0;
	game_beat_tower_climbing_tired = 1.0;
	game_beat_tower_running = 1.0;
	game_beat_tower_running_tired = 1.0;
	game_beat_tower_clear = 1.0;
	game_beat_tower_clear_tired = 1.0;
	game_beat_tower_fail = 1.0;


	result_offset = { pos = VECTOR2:CreateVector2(0, 0); mode = OFFSET_MODE_RESULT; v1_scale = 1; };
	result_motion_normal = nil;
	result_motion_clear = nil;
	result_motion_failed_in = nil;
	result_motion_failed = nil;
	result_beat_normal = 1;
	result_beat_clear = 1;
	result_beat_failed_in = 1;
	result_beat_failed = 1;
}

local function csarray_to_motiontable(csarray)
	local luatable = {}
	for i = 1, csarray.Length, 1 do
		luatable[i] = csarray[i - 1] + 1
	end
	return luatable
end

local function load_config()
	local chara_config_ini = INILOADER:LoadIni("CharaConfig.txt")

	chara_config.chara_version = chara_config_ini:GetString("Chara_Version", chara_config.chara_version)
	chara_config.legacy_mode = chara_config_ini:GetBool("Chara_LegacyMode", chara_config.legacy_mode)

	local ini_chara_resolution = chara_config_ini:GetIntArray("Chara_Resolution")
	if ini_chara_resolution.Length == 2 then
		chara_config.resolution = VECTOR2:CreateVector2(ini_chara_resolution[0], ini_chara_resolution[1])
	end

	local ini_heya_chara_render_offset = chara_config_ini:GetIntArray("Heya_Chara_Render_Offset")
	if ini_heya_chara_render_offset.Length == 2 then
		chara_config.heya_render_offset = VECTOR2:CreateVector2(ini_heya_chara_render_offset[0], ini_heya_chara_render_offset[1])
	end

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

	---Game+++++++++++++
	local ini_game_offset = chara_config_ini:GetIntArray("Game_Chara_Offset")
	if ini_game_offset.Length == 2 then
		chara_config.game_offset.pos = VECTOR2:CreateVector2(ini_game_offset[0], ini_game_offset[1])
	end

	local ini_game_chara_x = chara_config_ini:GetIntArray("Game_Chara_X")
	if ini_game_chara_x.Length >= 1 then
		chara_config.game_offset.pos.X = ini_game_chara_x[0]
	end

	local ini_game_chara_y = chara_config_ini:GetIntArray("Game_Chara_Y")
	if ini_game_chara_y.Length >= 1 then
		chara_config.game_offset.pos.Y = ini_game_chara_y[0]
	end

	local ini_game_balloon_offset = chara_config_ini:GetIntArray("Game_Chara_Balloon_Offset")
	if ini_game_balloon_offset.Length == 2 then
		chara_config.game_balloon_offset.pos = VECTOR2:CreateVector2(ini_game_balloon_offset[0], ini_game_balloon_offset[1])
	end

	local ini_game_balloon_x = chara_config_ini:GetIntArray("Game_Chara_Balloon_X")
	if ini_game_balloon_x.Length >= 1 then
		chara_config.game_balloon_offset.pos.X = ini_game_balloon_x[0]
	end

	local ini_game_balloon_y = chara_config_ini:GetIntArray("Game_Chara_Balloon_Y")
	if ini_game_balloon_y.Length >= 1 then
		chara_config.game_balloon_offset.pos.Y = ini_game_balloon_y[0]
	end

	local ini_game_kusudama_offset = chara_config_ini:GetIntArray("Game_Chara_Kusudama_Offset")
	if ini_game_kusudama_offset.Length == 2 then
		chara_config.game_kusudama_offset.pos = VECTOR2:CreateVector2(ini_game_kusudama_offset[0], ini_game_kusudama_offset[1])
	end

	local ini_game_kusudama_x = chara_config_ini:GetIntArray("Game_Chara_Kusudama_X")
	if ini_game_kusudama_x.Length >= 1 then
		chara_config.game_kusudama_offset.pos.X = ini_game_kusudama_x[0]
	end

	local ini_game_kusudama_y = chara_config_ini:GetIntArray("Game_Chara_Kusudama_Y")
	if ini_game_kusudama_y.Length >= 1 then
		chara_config.game_kusudama_offset.pos.Y = ini_game_kusudama_y[0]
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


	local ini_game_chara_tower_offset = chara_config_ini:GetIntArray("Game_Chara_Tower_Offset")
	if ini_game_chara_tower_offset.Length == 2 then
		chara_config.game_chara_tower_offset.pos = VECTOR2:CreateVector2(ini_game_chara_tower_offset[0], ini_game_chara_tower_offset[1])
	end

	local ini_game_motion_tower_standing = chara_config_ini:GetIntArray("Game_Chara_Motion_Tower_Standing")
	if ini_game_motion_tower_standing.Length >= 1 then
		chara_config.game_motion_tower_standing = csarray_to_motiontable(ini_game_motion_tower_standing)
		chara_config.game_motion_tower_standing_tired = chara_config.game_motion_tower_standing
	end

	local ini_game_motion_tower_standing_tired = chara_config_ini:GetIntArray("Game_Chara_Motion_Tower_Standing_Tired")
	if ini_game_motion_tower_standing_tired.Length >= 1 then
		chara_config.game_motion_tower_standing_tired = csarray_to_motiontable(ini_game_motion_tower_standing_tired)
	end

	local ini_game_motion_tower_climbing = chara_config_ini:GetIntArray("Game_Chara_Motion_Tower_Climbing")
	if ini_game_motion_tower_climbing.Length >= 1 then
		chara_config.game_motion_tower_climbing = csarray_to_motiontable(ini_game_motion_tower_climbing)
		chara_config.game_motion_tower_climbing_tired = chara_config.game_motion_tower_climbing
	end

	local ini_game_motion_tower_climbing_tired = chara_config_ini:GetIntArray("Game_Chara_Motion_Tower_Climbing_Tired")
	if ini_game_motion_tower_climbing_tired.Length >= 1 then
		chara_config.game_motion_tower_climbing_tired = csarray_to_motiontable(ini_game_motion_tower_climbing_tired)
	end

	local ini_game_motion_tower_clear = chara_config_ini:GetIntArray("Game_Chara_Motion_Tower_Clear")
	if ini_game_motion_tower_clear.Length >= 1 then
		chara_config.game_motion_tower_clear = csarray_to_motiontable(ini_game_motion_tower_clear)
		chara_config.game_motion_tower_clear_tired = chara_config.game_motion_tower_clear
	end

	local ini_game_motion_tower_clear_tired = chara_config_ini:GetIntArray("Game_Chara_Motion_Tower_Clear_Tired")
	if ini_game_motion_tower_clear_tired.Length >= 1 then
		chara_config.game_motion_tower_clear_tired = csarray_to_motiontable(ini_game_motion_tower_clear_tired)
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

	chara_config.game_beat_tower_standing = chara_config_ini:GetDouble("Game_Chara_Beat_Tower_Standing", chara_config.game_beat_tower_standing)
	chara_config.game_beat_tower_standing_tired = chara_config_ini:GetDouble("Game_Chara_Beat_Tower_Standing_Tired", chara_config.game_beat_tower_standing_tired)
	chara_config.game_beat_tower_climbing = chara_config_ini:GetDouble("Game_Chara_Beat_Tower_Climbing", chara_config.game_beat_tower_climbing)
	chara_config.game_beat_tower_climbing_tired = chara_config_ini:GetDouble("Game_Chara_Beat_Tower_Climbing_Tired", chara_config.game_beat_tower_climbing_tired)
	chara_config.game_beat_tower_fail = chara_config_ini:GetDouble("Game_Chara_Beat_Tower_Fail", chara_config.game_beat_tower_fail)
	chara_config.game_beat_tower_clear = chara_config_ini:GetDouble("Game_Chara_Beat_Tower_Clear", chara_config.game_beat_tower_clear)
	chara_config.game_beat_tower_clear_tired = chara_config_ini:GetDouble("Game_Chara_Beat_Tower_Clear_Tired", chara_config.game_beat_tower_clear_tired)
	--+++++++++++++

	---Menu+++++++++++++
	local ini_menu_offset = chara_config_ini:GetIntArray("Menu_Offset")
	if ini_menu_offset.Length == 2 then
		chara_config.menu_offset.pos = VECTOR2:CreateVector2(ini_menu_offset[0], ini_menu_offset[1])
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

	chara_config.menu_beat_normal = chara_config_ini:GetDouble("Chara_Menu_Loop_AnimationDuration", chara_config.menu_beat_normal * 500.0) / 500.0
	chara_config.menu_beat_normal = chara_config_ini:GetDouble("Menu_Chara_Beat_Normal", chara_config.menu_beat_normal)

	chara_config.menu_beat_wait = chara_config_ini:GetDouble("Chara_Menu_Wait_AnimationDuration", chara_config.menu_beat_wait * 500.0) / 500.0
	chara_config.menu_beat_wait = chara_config_ini:GetDouble("Menu_Chara_Beat_Wait", chara_config.menu_beat_wait)

	chara_config.menu_beat_start = chara_config_ini:GetDouble("Chara_Menu_Start_AnimationDuration", chara_config.menu_beat_start * 500.0) / 500.0
	chara_config.menu_beat_start = chara_config_ini:GetDouble("Menu_Chara_Beat_Start", chara_config.menu_beat_start)

	chara_config.menu_beat_select = chara_config_ini:GetDouble("Chara_Menu_Select_AnimationDuration", chara_config.menu_beat_select * 500.0) / 500.0
	chara_config.menu_beat_select = chara_config_ini:GetDouble("Menu_Chara_Beat_Select", chara_config.menu_beat_select)
	--+++++++++++++

	--Result+++++++++++++
	local ini_result_offset = chara_config_ini:GetIntArray("Result_Offset")
	if ini_result_offset.Length == 2 then
		chara_config.result_offset.pos = VECTOR2:CreateVector2(ini_result_offset[0], ini_result_offset[1])
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
load_config()


local animation_offsets = {
	[CHARACTER.ANIM_GAME_NORMAL] = chara_config.game_offset;
	[CHARACTER.ANIM_GAME_CLEAR] = chara_config.game_offset;
	[CHARACTER.ANIM_GAME_MAX] = chara_config.game_offset;
	[CHARACTER.ANIM_GAME_GOGO] = chara_config.game_offset;
	[CHARACTER.ANIM_GAME_GOGO_MAX] = chara_config.game_offset;
	[CHARACTER.ANIM_GAME_MISS] = chara_config.game_offset;
	[CHARACTER.ANIM_GAME_MISS_DOWN] = chara_config.game_offset;
	[CHARACTER.ANIM_GAME_10COMBO] = chara_config.game_offset;
	[CHARACTER.ANIM_GAME_10COMBO_MAX] = chara_config.game_offset;
	[CHARACTER.ANIM_GAME_CLEARED] = chara_config.game_offset;
	[CHARACTER.ANIM_GAME_FAILED] = chara_config.game_offset;
	[CHARACTER.ANIM_GAME_CLEAR_OUT] = chara_config.game_offset;
	[CHARACTER.ANIM_GAME_CLEAR_IN] = chara_config.game_offset;
	[CHARACTER.ANIM_GAME_MAX_OUT] = chara_config.game_offset;
	[CHARACTER.ANIM_GAME_MAX_IN] = chara_config.game_offset;
	[CHARACTER.ANIM_GAME_MISS_IN] = chara_config.game_offset;
	[CHARACTER.ANIM_GAME_MISS_DOWN_IN] = chara_config.game_offset;
	[CHARACTER.ANIM_GAME_RETURN] = chara_config.game_offset;
	[CHARACTER.ANIM_GAME_GOGOSTART] = chara_config.game_offset;
	[CHARACTER.ANIM_GAME_GOGOSTART_CLEAR] = chara_config.game_offset;
	[CHARACTER.ANIM_GAME_GOGOSTART_MAX] = chara_config.game_offset;
	[CHARACTER.ANIM_GAME_BALLOON_BREAKING] = chara_config.game_balloon_offset;
	[CHARACTER.ANIM_GAME_BALLOON_BROKE] = chara_config.game_balloon_offset;
	[CHARACTER.ANIM_GAME_BALLOON_MISS] = chara_config.game_balloon_offset;
	[CHARACTER.ANIM_GAME_KUSUDAMA_BREAKING] = chara_config.game_kusudama_offset;
	[CHARACTER.ANIM_GAME_KUSUDAMA_BROKE] = chara_config.game_kusudama_offset;
	[CHARACTER.ANIM_GAME_KUSUDAMA_MISS] = chara_config.game_kusudama_offset;
	[CHARACTER.ANIM_GAME_KUSUDAMA_IDLE] = chara_config.game_kusudama_offset;

	[CHARACTER.ANIM_GAME_TOWER_STANDING] = chara_config.game_chara_tower_offset;
	[CHARACTER.ANIM_GAME_TOWER_STANDING_TIRED] = chara_config.game_chara_tower_offset;
	[CHARACTER.ANIM_GAME_TOWER_CLIMBING] = chara_config.game_chara_tower_offset;
	[CHARACTER.ANIM_GAME_TOWER_CLIMBING_TIRED] = chara_config.game_chara_tower_offset;
	[CHARACTER.ANIM_GAME_TOWER_RUNNING] = chara_config.game_chara_tower_offset;
	[CHARACTER.ANIM_GAME_TOWER_RUNNING_TIRED] = chara_config.game_chara_tower_offset;
	[CHARACTER.ANIM_GAME_TOWER_CLEAR] = chara_config.game_chara_tower_offset;
	[CHARACTER.ANIM_GAME_TOWER_CLEAR_TIRED] = chara_config.game_chara_tower_offset;
	[CHARACTER.ANIM_GAME_TOWER_FAIL] = chara_config.game_chara_tower_offset;

	[CHARACTER.ANIM_MENU_WAIT] = chara_config.menu_offset;
	[CHARACTER.ANIM_MENU_START] = chara_config.menu_offset;
	[CHARACTER.ANIM_MENU_NORMAL] = chara_config.menu_offset;
	[CHARACTER.ANIM_MENU_SELECT] = chara_config.menu_offset;
	[CHARACTER.ANIM_ENTRY_NORMAL] = chara_config.menu_offset;
	[CHARACTER.ANIM_ENTRY_JUMP] = chara_config.menu_offset;

	[CHARACTER.ANIM_RESULT_NORMAL] = chara_config.result_offset;
	[CHARACTER.ANIM_RESULT_CLEAR] = chara_config.result_offset;
	[CHARACTER.ANIM_RESULT_FAILED_IN] = chara_config.result_offset;
	[CHARACTER.ANIM_RESULT_FAILED] = chara_config.result_offset;
}

local animation_motions = {
	[CHARACTER.ANIM_GAME_NORMAL] = chara_config.game_motion_normal;
	[CHARACTER.ANIM_GAME_CLEAR] = chara_config.game_motion_clear;
	[CHARACTER.ANIM_GAME_MAX] = chara_config.game_motion_clear_max;
	[CHARACTER.ANIM_GAME_GOGO] = chara_config.game_motion_gogo;
	[CHARACTER.ANIM_GAME_GOGO_MAX] = chara_config.game_motion_gogo_max;
	[CHARACTER.ANIM_GAME_MISS] = chara_config.game_motion_miss;
	[CHARACTER.ANIM_GAME_MISS_DOWN] = chara_config.game_motion_miss_down;
	[CHARACTER.ANIM_GAME_10COMBO] = chara_config.game_motion_10combo;
	[CHARACTER.ANIM_GAME_10COMBO_MAX] = chara_config.game_motion_10combo_max;
	[CHARACTER.ANIM_GAME_CLEARED] = chara_config.game_motion_cleared;
	[CHARACTER.ANIM_GAME_FAILED] = chara_config.game_motion_failed;
	[CHARACTER.ANIM_GAME_CLEAR_OUT] = chara_config.game_motion_clearout;
	[CHARACTER.ANIM_GAME_CLEAR_IN] = chara_config.game_motion_clearin;
	[CHARACTER.ANIM_GAME_MAX_OUT] = chara_config.game_motion_soulout;
	[CHARACTER.ANIM_GAME_MAX_IN] = chara_config.game_motion_soulin;
	[CHARACTER.ANIM_GAME_MISS_IN] = chara_config.game_motion_missin;
	[CHARACTER.ANIM_GAME_MISS_DOWN_IN] = chara_config.game_motion_missdownin;
	[CHARACTER.ANIM_GAME_RETURN] = chara_config.game_motion_return;
	[CHARACTER.ANIM_GAME_GOGOSTART] = chara_config.game_motion_gogostart;
	[CHARACTER.ANIM_GAME_GOGOSTART_CLEAR] = chara_config.game_motion_gogostart_clear;
	[CHARACTER.ANIM_GAME_GOGOSTART_MAX] = chara_config.game_motion_gogostart_max;
	[CHARACTER.ANIM_GAME_BALLOON_BREAKING] = chara_config.game_motion_balloon_breaking;
	[CHARACTER.ANIM_GAME_BALLOON_BROKE] = chara_config.game_motion_balloon_broke;
	[CHARACTER.ANIM_GAME_BALLOON_MISS] = chara_config.game_motion_balloon_miss;
	[CHARACTER.ANIM_GAME_KUSUDAMA_BREAKING] = chara_config.game_motion_kusudama_breaking;
	[CHARACTER.ANIM_GAME_KUSUDAMA_BROKE] = chara_config.game_motion_kusudama_idle;
	[CHARACTER.ANIM_GAME_KUSUDAMA_MISS] = chara_config.game_motion_kusudama_broke;
	[CHARACTER.ANIM_GAME_KUSUDAMA_IDLE] = chara_config.game_motion_kusudama_miss;

	[CHARACTER.ANIM_GAME_TOWER_STANDING] = chara_config.game_motion_tower_standing;
	[CHARACTER.ANIM_GAME_TOWER_STANDING_TIRED] = chara_config.game_motion_tower_standing_tired;
	[CHARACTER.ANIM_GAME_TOWER_CLIMBING] = chara_config.game_motion_tower_climbing;
	[CHARACTER.ANIM_GAME_TOWER_CLIMBING_TIRED] = chara_config.game_motion_tower_climbing_tired;
	[CHARACTER.ANIM_GAME_TOWER_RUNNING] = chara_config.game_motion_tower_running;
	[CHARACTER.ANIM_GAME_TOWER_RUNNING_TIRED] = chara_config.game_motion_tower_running_tired;
	[CHARACTER.ANIM_GAME_TOWER_CLEAR] = chara_config.game_motion_tower_clear;
	[CHARACTER.ANIM_GAME_TOWER_CLEAR_TIRED] = chara_config.game_motion_tower_clear_tired;
	[CHARACTER.ANIM_GAME_TOWER_FAIL] = chara_config.game_motion_tower_fail;

	[CHARACTER.ANIM_MENU_WAIT] = chara_config.menu_motion_normal;
	[CHARACTER.ANIM_MENU_START] = chara_config.menu_motion_start;
	[CHARACTER.ANIM_MENU_NORMAL] = chara_config.menu_motion_normal;
	[CHARACTER.ANIM_MENU_SELECT] = chara_config.menu_motion_select;
	[CHARACTER.ANIM_ENTRY_NORMAL] = chara_config.title_motion_normal;
	[CHARACTER.ANIM_ENTRY_JUMP] = chara_config.title_motion_entry;

	[CHARACTER.ANIM_RESULT_NORMAL] = chara_config.result_motion_normal;
	[CHARACTER.ANIM_RESULT_CLEAR] = chara_config.result_motion_clear;
	[CHARACTER.ANIM_RESULT_FAILED_IN] = chara_config.result_motion_failed_in;
	[CHARACTER.ANIM_RESULT_FAILED] = chara_config.result_motion_failed;
}

local animation_beats = {
	[CHARACTER.ANIM_GAME_NORMAL] = chara_config.game_beat_normal;
	[CHARACTER.ANIM_GAME_CLEAR] = chara_config.game_beat_clear;
	[CHARACTER.ANIM_GAME_MAX] = chara_config.game_beat_clear_max;
	[CHARACTER.ANIM_GAME_GOGO] = chara_config.game_beat_gogo;
	[CHARACTER.ANIM_GAME_GOGO_MAX] = chara_config.game_beat_gogo_max;
	[CHARACTER.ANIM_GAME_MISS] = chara_config.game_beat_miss;
	[CHARACTER.ANIM_GAME_MISS_DOWN] = chara_config.game_beat_miss_down;
	[CHARACTER.ANIM_GAME_10COMBO] = chara_config.game_beat_10combo;
	[CHARACTER.ANIM_GAME_10COMBO_MAX] = chara_config.game_beat_10combo_max;
	[CHARACTER.ANIM_GAME_CLEARED] = chara_config.game_beat_cleared;
	[CHARACTER.ANIM_GAME_FAILED] = chara_config.game_beat_failed;
	[CHARACTER.ANIM_GAME_CLEAR_OUT] = chara_config.game_beat_clearout;
	[CHARACTER.ANIM_GAME_CLEAR_IN] = chara_config.game_beat_clearin;
	[CHARACTER.ANIM_GAME_MAX_OUT] = chara_config.game_beat_soulout;
	[CHARACTER.ANIM_GAME_MAX_IN] = chara_config.game_beat_soulin;
	[CHARACTER.ANIM_GAME_MISS_IN] = chara_config.game_beat_missin;
	[CHARACTER.ANIM_GAME_MISS_DOWN_IN] = chara_config.game_beat_missdownin;
	[CHARACTER.ANIM_GAME_RETURN] = chara_config.game_beat_return;
	[CHARACTER.ANIM_GAME_GOGOSTART] = chara_config.game_beat_gogostart;
	[CHARACTER.ANIM_GAME_GOGOSTART_CLEAR] = chara_config.game_beat_gogostart_clear;
	[CHARACTER.ANIM_GAME_GOGOSTART_MAX] = chara_config.game_beat_gogostart_max;
	[CHARACTER.ANIM_GAME_BALLOON_BREAKING] = chara_config.game_beat_balloon_breaking;
	[CHARACTER.ANIM_GAME_BALLOON_BROKE] = chara_config.game_beat_balloon_broke;
	[CHARACTER.ANIM_GAME_BALLOON_MISS] = chara_config.game_beat_balloon_miss;
	[CHARACTER.ANIM_GAME_KUSUDAMA_BREAKING] = chara_config.game_beat_kusudama_breaking;
	[CHARACTER.ANIM_GAME_KUSUDAMA_BROKE] = chara_config.game_beat_kusudama_idle;
	[CHARACTER.ANIM_GAME_KUSUDAMA_MISS] = chara_config.game_beat_kusudama_broke;
	[CHARACTER.ANIM_GAME_KUSUDAMA_IDLE] = chara_config.game_beat_kusudama_miss;

	[CHARACTER.ANIM_GAME_TOWER_STANDING] = chara_config.game_beat_tower_standing;
	[CHARACTER.ANIM_GAME_TOWER_STANDING_TIRED] = chara_config.game_beat_tower_standing_tired;
	[CHARACTER.ANIM_GAME_TOWER_CLIMBING] = chara_config.game_beat_tower_climbing;
	[CHARACTER.ANIM_GAME_TOWER_CLIMBING_TIRED] = chara_config.game_beat_tower_climbing_tired;
	[CHARACTER.ANIM_GAME_TOWER_RUNNING] = chara_config.game_beat_tower_running;
	[CHARACTER.ANIM_GAME_TOWER_RUNNING_TIRED] = chara_config.game_beat_tower_running_tired;
	[CHARACTER.ANIM_GAME_TOWER_CLEAR] = chara_config.game_beat_tower_clear;
	[CHARACTER.ANIM_GAME_TOWER_CLEAR_TIRED] = chara_config.game_beat_tower_clear_tired;
	[CHARACTER.ANIM_GAME_TOWER_FAIL] = chara_config.game_beat_tower_fail;

	[CHARACTER.ANIM_MENU_WAIT] = chara_config.menu_beat_normal;
	[CHARACTER.ANIM_MENU_START] = chara_config.menu_beat_start;
	[CHARACTER.ANIM_MENU_NORMAL] = chara_config.menu_beat_normal;
	[CHARACTER.ANIM_MENU_SELECT] = chara_config.menu_beat_select;
	[CHARACTER.ANIM_ENTRY_NORMAL] = chara_config.title_beat_normal;
	[CHARACTER.ANIM_ENTRY_JUMP] = chara_config.title_beat_entry;

	[CHARACTER.ANIM_RESULT_NORMAL] = chara_config.result_beat_normal;
	[CHARACTER.ANIM_RESULT_CLEAR] = chara_config.result_beat_clear;
	[CHARACTER.ANIM_RESULT_FAILED_IN] = chara_config.result_beat_failed_in;
	[CHARACTER.ANIM_RESULT_FAILED] = chara_config.result_beat_failed;
}


local avaiable_animations = {}

local AnimationData = {
	new = function()
		local obj = {
			offset = VECTOR2:CreateVector2(0, 0);
			duration = 1000;
			beat = 1;
			value = 0.0;
			update = function(self, delta, looping)
				return false
			end;
			draw = function(self, x, y, scaleX, scaleY, opacity, color, flipX)
			end;
			dispose = function(self)
			end;
		}
		return obj
	end;
}

local animationdata_array = {}

local function create_animation(animationType)
	local dir = animation_dirs[animationType]
	if dir == nil then
		return nil
	end


	local motion = animation_motions[animationType]
	local beat = animation_beats[animationType]
	if beat == nil then
		beat = 1
	end
	local offset = animation_offsets[animationType]
	if offset == nil then
		offset = { pos = VECTOR2:CreateVector2(0, 0); mode = OFFSET_MODE_MENU; v1_scale = 1.0; }
	end

	local dir_name = animation_dirs[animationType]

	local animation_data = nil
	if STORAGE:DirectoryExists(dir_name) then
		animation_data = AnimationData.new()
		animation_data.offset = offset
		animation_data.beat = beat

		local files = STORAGE:GetFiles(dir_name, "*.png")
		local frames = {}
		for i = 1, files.Length, 1 do
			local file_path = dir_name.."/"..tostring(i - 1)..".png"
			if STORAGE:FileExists(file_path) then
				frames[i] = TEXTURE:CreateTexture(file_path)
			else
				break
			end
		end

		animation_data.motion = {}
		if motion ~= nil then
			animation_data.motion = motion
		else
			for i = 1, #frames, 1 do
				animation_data.motion[i] = i
			end
		end

		animation_data.frames = frames
		animation_data.frame_index = 0
		animation_data.update = function(self, delta, looping)
			local speed = 1000 / self.duration / self.beat

			self.value = self.value + (speed * delta)
			if looping then
				self.value = self.value - math.floor(self.value)
			else
				self.value = math.min(self.value, 1)
			end

			local length = #self.motion
			local motion_index = math.min(math.floor(self.value * length), length - 1) + 1
			self.frame_index = self.motion[motion_index]

			if looping then
				return false
			else
				return self.value >= 1
			end
		end
		animation_data.draw = function(self, x, y, scaleX, scaleY, opacity, color, flipX)
			local frame = self.frames[self.frame_index]
			if flipX then
				scaleX = -scaleX
			end

			local offsetX = self.offset.pos.X
			local offsetY = self.offset.pos.Y

			local theme_resolution = THEME:GetResolution()
			local baseScale = theme_resolution.Y / chara_config.resolution.Y
			scaleX = scaleX * baseScale
			scaleY = scaleY * baseScale

			if chara_config.legacy_mode then
				scaleX = scaleX * self.offset.v1_scale
				scaleY = scaleY * self.offset.v1_scale
			end

			offsetX = offsetX * (theme_resolution.X / chara_config.resolution.X)
			offsetY = offsetY * (theme_resolution.Y / chara_config.resolution.Y)

			if frame ~= nil then
				if chara_config.legacy_mode then
					if self.offset.mode == OFFSET_MODE_GAME then
						offsetX = offsetX + (((frame.Width / 2.0) - (0.13020833333 * chara_config.resolution.X)) * scaleX)
						offsetY = offsetY + ((frame.Height - (0.25555555555 * chara_config.resolution.Y)) * scaleY)
					elseif self.offset.mode == OFFSET_MODE_GAME_BALLOON then
						offsetX = offsetX + (((frame.Width / 2.0) - (0.27216666666 * chara_config.resolution.X)) * scaleX)
						offsetY = offsetY + ((frame.Height - (0.26296296296 * chara_config.resolution.Y)) * scaleY)
					elseif self.offset.mode == OFFSET_MODE_GAME_KUSUDAMA then
						offsetX = offsetX + (((frame.Width / 2.0) - (0.2555 * chara_config.resolution.X)) * scaleX)
						offsetY = offsetY + ((frame.Height - (0.2555 * chara_config.resolution.Y)) * scaleY)
					end
				end

				x = x + offsetX
				y = y + offsetY

				frame:SetScale(scaleX, scaleY)
				frame:SetOpacity(opacity / 255.0)
				frame:SetColor(color)
				frame:DrawAtAnchor(x, y, "bottom")
			end
		end
		animation_data.dispose = function(self)
			for i = 1, #self.frames, 1 do
				self.frames[i]:Dispose()
			end
			self.frames = nil
		end
	end

	return animation_data
end

local function create_preview()
	animation_data = AnimationData.new()

	local preview = nil
	if STORAGE:FileExists("Preview.png") then
		preview = TEXTURE:CreateTexture("Preview.png")
	elseif STORAGE:FileExists("Normal/0.png") then
		preview = TEXTURE:CreateTexture("Normal/0.png")
	end
	animation_data.preview = preview

	animation_data.draw = function(self, x, y, scaleX, scaleY, opacity, color, flipX)
		if flipX then
			scaleX = -scaleX
		end

		local theme_resolution = THEME:GetResolution()
		local baseScale = theme_resolution.Y / chara_config.resolution.Y
		scaleX = scaleX * baseScale
		scaleY = scaleY * baseScale

		local frame = self.preview
		if frame ~= nil then
			frame:SetScale(scaleX, scaleY)
			frame:SetOpacity(opacity / 255.0)
			frame:SetColor(color)
			frame:DrawAtAnchor(x, y, "center")
		end
	end
	animation_data.dispose = function(self)
		if self.preview ~= nil then
			self.preview:Dispose()
		end
	end

	return animation_data
end

local function create_render()
	animation_data = AnimationData.new()
	if STORAGE:FileExists("Render.png") then
		animation_data.render = TEXTURE:CreateTexture("Render.png")
	end

	animation_data.draw = function(self, x, y, scaleX, scaleY, opacity, color, flipX)
		if flipX then
			scaleX = -scaleX
		end

		local offsetX = chara_config.heya_render_offset.X
		local offsetY = chara_config.heya_render_offset.Y

		local theme_resolution = THEME:GetResolution()
		local baseScale = theme_resolution.Y / chara_config.resolution.Y
		scaleX = scaleX * baseScale
		scaleY = scaleY * baseScale
		offsetX = offsetX * (theme_resolution.X / chara_config.resolution.X)
		offsetY = offsetY * (theme_resolution.Y / chara_config.resolution.Y)

		local frame = self.render
		if frame ~= nil then
			x = x + offsetX
			y = y + offsetY
			frame:SetScale(scaleX, scaleY)
			frame:SetOpacity(opacity / 255.0)
			frame:SetColor(color)
			frame:Draw(x, y)
		end
	end
	animation_data.dispose = function(self)
		if self.render ~= nil then
			self.render:Dispose()
		end
	end

	return animation_data
end

local animation_buildrs = {
	[CHARACTER.ANIM_PREVIEW] = create_preview;
	[CHARACTER.ANIM_RENDER] = create_render;

	[CHARACTER.ANIM_GAME_NORMAL] = create_animation;
	[CHARACTER.ANIM_GAME_CLEAR] = create_animation;
	[CHARACTER.ANIM_GAME_MAX] = create_animation;
	[CHARACTER.ANIM_GAME_GOGO] = create_animation;
	[CHARACTER.ANIM_GAME_GOGO_MAX] = create_animation;
	[CHARACTER.ANIM_GAME_MISS] = create_animation;
	[CHARACTER.ANIM_GAME_MISS_DOWN] = create_animation;
	[CHARACTER.ANIM_GAME_10COMBO] = create_animation;
	[CHARACTER.ANIM_GAME_10COMBO_MAX] = create_animation;
	[CHARACTER.ANIM_GAME_CLEARED] = create_animation;
	[CHARACTER.ANIM_GAME_FAILED] = create_animation;
	[CHARACTER.ANIM_GAME_CLEAR_OUT] = create_animation;
	[CHARACTER.ANIM_GAME_CLEAR_IN] = create_animation;
	[CHARACTER.ANIM_GAME_MAX_OUT] = create_animation;
	[CHARACTER.ANIM_GAME_MAX_IN] = create_animation;
	[CHARACTER.ANIM_GAME_MISS_IN] = create_animation;
	[CHARACTER.ANIM_GAME_MISS_DOWN_IN] = create_animation;
	[CHARACTER.ANIM_GAME_RETURN] = create_animation;
	[CHARACTER.ANIM_GAME_GOGOSTART] = create_animation;
	[CHARACTER.ANIM_GAME_GOGOSTART_CLEAR] = create_animation;
	[CHARACTER.ANIM_GAME_GOGOSTART_MAX] = create_animation;
	[CHARACTER.ANIM_GAME_BALLOON_BREAKING] = create_animation;
	[CHARACTER.ANIM_GAME_BALLOON_BROKE] = create_animation;
	[CHARACTER.ANIM_GAME_BALLOON_MISS] = create_animation;
	[CHARACTER.ANIM_GAME_KUSUDAMA_BREAKING] = create_animation;
	[CHARACTER.ANIM_GAME_KUSUDAMA_BROKE] = create_animation;
	[CHARACTER.ANIM_GAME_KUSUDAMA_MISS] = create_animation;
	[CHARACTER.ANIM_GAME_KUSUDAMA_IDLE] = create_animation;

	[CHARACTER.ANIM_GAME_TOWER_STANDING] = create_animation;
	[CHARACTER.ANIM_GAME_TOWER_STANDING_TIRED] = create_animation;
	[CHARACTER.ANIM_GAME_TOWER_CLIMBING] = create_animation;
	[CHARACTER.ANIM_GAME_TOWER_CLIMBING_TIRED] = create_animation;
	[CHARACTER.ANIM_GAME_TOWER_RUNNING] = create_animation;
	[CHARACTER.ANIM_GAME_TOWER_RUNNING_TIRED] = create_animation;
	[CHARACTER.ANIM_GAME_TOWER_CLEAR] = create_animation;
	[CHARACTER.ANIM_GAME_TOWER_CLEAR_TIRED] = create_animation;
	[CHARACTER.ANIM_GAME_TOWER_FAIL] = create_animation;

	[CHARACTER.ANIM_MENU_WAIT] = create_animation;
	[CHARACTER.ANIM_MENU_START] = create_animation;
	[CHARACTER.ANIM_MENU_NORMAL] = create_animation;
	[CHARACTER.ANIM_MENU_SELECT] = create_animation;
	[CHARACTER.ANIM_ENTRY_NORMAL] = create_animation;
	[CHARACTER.ANIM_ENTRY_JUMP] = create_animation;

	[CHARACTER.ANIM_RESULT_NORMAL] = create_animation;
	[CHARACTER.ANIM_RESULT_CLEAR] = create_animation;
	[CHARACTER.ANIM_RESULT_FAILED_IN] = create_animation;
	[CHARACTER.ANIM_RESULT_FAILED] = create_animation;
}

function avaialbeAnimation(animationType)
	local flag = avaiable_animations[animationType]
	if flag ~= nil and flag == true then
		return true
	else
		return false
	end
end

function loadAnimation(animationType)
	animationdata_array[animationType] = animation_buildrs[animationType](animationType)

	if animationdata_array[animationType] ~= nil then
		avaiable_animations[animationType] = true
	end
end

function disposeAnimation(animationType)
	local animation_data = animationdata_array[animationType]
	if animation_data ~= nil then
		animation_data:dispose()
	end

	avaiable_animations[animationType] = false
end

function setAnimationDuration(animationType, duration)
	local animation_data = animationdata_array[animationType]
	if animation_data ~= nil then
		animation_data.duration = duration
	end
end

function resetAnimationCounter(animationType)
	local animation_data = animationdata_array[animationType]
	if animation_data ~= nil then
		animation_data.value = 0
	end
end

function loadVoice(voiceType)
	if voice_files[voiceType] ~= nil then
		voices[voiceType] = SOUND:CreateVoice(voice_files[voiceType])
	end
end

function disposeVoice(voiceType)
	local voice = voices[voiceType]
	if voice ~= nil then
		voice:Dispose()
	end
end

function playVoice(voiceType)
	local voice = voices[voiceType]
	if voice ~= nil then
		voice:Play()
	end
end

function update(delta, animationType, looping)
	if not avaialbeAnimation(animationType) then
		return false
	end

	local animation = animationdata_array[animationType]
	if animation ~= nil then
		return animation:update(delta, looping)
	else
		return false
	end
end

function draw(animationType, x, y, scaleX, scaleY, opacity, color, flipX)
	if not avaialbeAnimation(animationType) then
		return
	end

	local animation = animationdata_array[animationType]
	if animation ~= nil then
		animation:draw(x, y, scaleX, scaleY, opacity, color, flipX)
	end
end
