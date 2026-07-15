local OFFSET_MODE_MENU = "menu"
local OFFSET_MODE_RESULT = "result"
local OFFSET_MODE_GAME = "game"
local OFFSET_MODE_GAME_AI = "game_ai"
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

local animations = {}
local animation_defs = {}
local function load_animation_defs(C --[[chara_config]], B --[[animation_builder]]) return {
	[CHARACTER.ANIM_PREVIEW] = { builder = B.preview };
	[CHARACTER.ANIM_RENDER] = { builder = B.render };

	[CHARACTER.ANIM_GAME_NORMAL] = { dir = "Normal", offset = C.game_offset, motion = C.game_motion_normal, beat = C.game_beat_normal, builder = B.animation };
	[CHARACTER.ANIM_GAME_CLEAR] = { dir = "Clear", offset = C.game_offset, motion = C.game_motion_clear, beat = C.game_beat_clear, builder = B.animation };
	[CHARACTER.ANIM_GAME_MAX] = { dir = "Clear_Max", offset = C.game_offset, motion = C.game_motion_clear_max, beat = C.game_beat_clear_max, builder = B.animation };
	[CHARACTER.ANIM_GAME_GOGO] = { dir = "GoGo", offset = C.game_offset, motion = C.game_motion_gogo, beat = C.game_beat_gogo, builder = B.animation };
	[CHARACTER.ANIM_GAME_GOGO_MAX] = { dir = "GoGo_Max", offset = C.game_offset, motion = C.game_motion_gogo_max, beat = C.game_beat_gogo_max, builder = B.animation };
	[CHARACTER.ANIM_GAME_MISS] = { dir = "Miss", offset = C.game_offset, motion = C.game_motion_miss, beat = C.game_beat_miss, builder = B.animation };
	[CHARACTER.ANIM_GAME_MISS_DOWN] = { dir = "MissDown", offset = C.game_offset, motion = C.game_motion_miss_down, beat = C.game_beat_miss_down, builder = B.animation };
	[CHARACTER.ANIM_GAME_10COMBO] = { dir = "10combo" --[[Title-cased in 0.6.0]], offset = C.game_offset, motion = C.game_motion_10combo, beat = C.game_beat_10combo, builder = B.animation };
	[CHARACTER.ANIM_GAME_10COMBO_MAX] = { dir = "10combo_Max" --[[Title-cased in 0.6.0]], offset = C.game_offset, motion = C.game_motion_10combo_max, beat = C.game_beat_10combo_max, builder = B.animation };
	[CHARACTER.ANIM_GAME_CLEARED] = { dir = "Cleared", offset = C.game_offset, motion = C.game_motion_cleared, beat = C.game_beat_cleared, builder = B.animation };
	[CHARACTER.ANIM_GAME_FAILED] = { dir = "Failed", offset = C.game_offset, motion = C.game_motion_failed, beat = C.game_beat_failed, builder = B.animation };
	[CHARACTER.ANIM_GAME_CLEAR_OUT] = { dir = "ClearOut", offset = C.game_offset, motion = C.game_motion_clearout, beat = C.game_beat_clearout, builder = B.animation };
	[CHARACTER.ANIM_GAME_CLEAR_IN] = { dir = "Clearin" --[[Title-cased in 0.6.0]], offset = C.game_offset, motion = C.game_motion_clearin, beat = C.game_beat_clearin, builder = B.animation };
	[CHARACTER.ANIM_GAME_MAX_OUT] = { dir = "SoulOut", offset = C.game_offset, motion = C.game_motion_soulout, beat = C.game_beat_soulout, builder = B.animation };
	[CHARACTER.ANIM_GAME_MAX_IN] = { dir = "Soulin" --[[Title-cased in 0.6.0]], offset = C.game_offset, motion = C.game_motion_soulin, beat = C.game_beat_soulin, builder = B.animation };
	[CHARACTER.ANIM_GAME_MISS_IN] = { dir = "MissIn", offset = C.game_offset, motion = C.game_motion_missin, beat = C.game_beat_missin, builder = B.animation };
	[CHARACTER.ANIM_GAME_MISS_DOWN_IN] = { dir = "MissDownIn", offset = C.game_offset, motion = C.game_motion_missdownin, beat = C.game_beat_missdownin, builder = B.animation };
	[CHARACTER.ANIM_GAME_RETURN] = { dir = "Return", offset = C.game_offset, motion = C.game_motion_return, beat = C.game_beat_return, builder = B.animation };
	[CHARACTER.ANIM_GAME_GOGOSTART] = { dir = "GoGoStart", offset = C.game_offset, motion = C.game_motion_gogostart, beat = C.game_beat_gogostart, builder = B.animation };
	[CHARACTER.ANIM_GAME_GOGOSTART_CLEAR] = { dir = "GoGoStart_Clear", offset = C.game_offset, motion = C.game_motion_gogostart_clear, beat = C.game_beat_gogostart_clear, builder = B.animation };
	[CHARACTER.ANIM_GAME_GOGOSTART_MAX] = { dir = "GoGoStart_Max", offset = C.game_offset, motion = C.game_motion_gogostart_max, beat = C.game_beat_gogostart_max, builder = B.animation };
	[CHARACTER.ANIM_GAME_BALLOON_BREAKING] = { dir = "Balloon_Breaking", offset = C.game_balloon_offset, motion = C.game_motion_balloon_breaking, beat = C.game_beat_balloon_breaking, builder = B.animation };
	[CHARACTER.ANIM_GAME_BALLOON_BROKE] = { dir = "Balloon_Broke", offset = C.game_balloon_offset, motion = C.game_motion_balloon_broke, beat = C.game_beat_balloon_broke, builder = B.animation };
	[CHARACTER.ANIM_GAME_BALLOON_MISS] = { dir = "Balloon_Miss", offset = C.game_balloon_offset, motion = C.game_motion_balloon_miss, beat = C.game_beat_balloon_miss, builder = B.animation };
	[CHARACTER.ANIM_GAME_KUSUDAMA_BREAKING] = { dir = "Kusudama_Breaking", offset = C.game_kusudama_offset, motion = C.game_motion_kusudama_breaking, beat = C.game_beat_kusudama_breaking, builder = B.animation };
	[CHARACTER.ANIM_GAME_KUSUDAMA_BROKE] = { dir = "Kusudama_Broke", offset = C.game_kusudama_offset, motion = C.game_motion_kusudama_broke, beat = C.game_beat_kusudama_broke, builder = B.animation };
	[CHARACTER.ANIM_GAME_KUSUDAMA_MISS] = { dir = "Kusudama_Miss", offset = C.game_kusudama_offset, motion = C.game_motion_kusudama_miss, beat = C.game_beat_kusudama_miss, builder = B.animation };
	[CHARACTER.ANIM_GAME_KUSUDAMA_IDLE] = { dir = "Kusudama_Idle", offset = C.game_kusudama_offset, motion = C.game_motion_kusudama_idle, beat = C.game_beat_kusudama_idle, builder = B.animation };

	[CHARACTER.ANIM_GAME_TOWER_STANDING] = { dir = "Tower_Char/Standing", offset = C.game_chara_tower_offset, motion = C.game_motion_tower_standing, beat = C.game_beat_tower_standing, builder = B.animation };
	[CHARACTER.ANIM_GAME_TOWER_STANDING_TIRED] = { dir = "Tower_Char/Standing_Tired", offset = C.game_chara_tower_offset, motion = C.game_motion_tower_standing_tired, beat = C.game_beat_tower_standing_tired, builder = B.animation };
	[CHARACTER.ANIM_GAME_TOWER_CLIMBING] = { dir = "Tower_Char/Climbing", offset = C.game_chara_tower_offset, motion = C.game_motion_tower_climbing, beat = C.game_beat_tower_climbing, builder = B.animation };
	[CHARACTER.ANIM_GAME_TOWER_CLIMBING_TIRED] = { dir = "Tower_Char/Climbing_Tired", offset = C.game_chara_tower_offset, motion = C.game_motion_tower_climbing_tired, beat = C.game_beat_tower_climbing_tired, builder = B.animation };
	[CHARACTER.ANIM_GAME_TOWER_RUNNING] = { dir = "Tower_Char/Running", offset = C.game_chara_tower_offset, motion = C.game_motion_tower_running, beat = C.game_beat_tower_running, builder = B.animation };
	[CHARACTER.ANIM_GAME_TOWER_RUNNING_TIRED] = { dir = "Tower_Char/Running_Tired", offset = C.game_chara_tower_offset, motion = C.game_motion_tower_running_tired, beat = C.game_beat_tower_running_tired, builder = B.animation };
	[CHARACTER.ANIM_GAME_TOWER_CLEAR] = { dir = "Tower_Char/Clear", offset = C.game_chara_tower_offset, motion = C.game_motion_tower_clear, beat = C.game_beat_tower_clear, builder = B.animation };
	[CHARACTER.ANIM_GAME_TOWER_CLEAR_TIRED] = { dir = "Tower_Char/Clear_Tired", offset = C.game_chara_tower_offset, motion = C.game_motion_tower_clear_tired, beat = C.game_beat_tower_clear_tired, builder = B.animation };
	[CHARACTER.ANIM_GAME_TOWER_FAIL] = { dir = "Tower_Char/Fail", offset = C.game_chara_tower_offset, motion = C.game_motion_tower_fail, beat = C.game_beat_tower_fail, builder = B.animation };

	[CHARACTER.ANIM_MENU_WAIT] = { dir = "Menu_Wait", offset = C.menu_offset, motion = C.menu_motion_normal, beat = C.menu_beat_normal, builder = B.animation };
	[CHARACTER.ANIM_MENU_START] = { dir = "Menu_Start", offset = C.menu_offset, motion = C.menu_motion_start, beat = C.menu_beat_start, builder = B.animation };
	[CHARACTER.ANIM_MENU_NORMAL] = { dir = "Menu_Loop", offset = C.menu_offset, motion = C.menu_motion_normal, beat = C.menu_beat_normal, builder = B.animation };
	[CHARACTER.ANIM_MENU_SELECT] = { dir = "Menu_Select", offset = C.menu_offset, motion = C.menu_motion_select, beat = C.menu_beat_select, builder = B.animation };
	[CHARACTER.ANIM_ENTRY_NORMAL] = { dir = "Title_Normal", offset = C.menu_offset, motion = C.title_motion_normal, beat = C.title_beat_normal, builder = B.animation };
	[CHARACTER.ANIM_ENTRY_JUMP] = { dir = "Title_Entry", offset = C.menu_offset, motion = C.title_motion_entry, beat = C.title_beat_entry, builder = B.animation };

	[CHARACTER.ANIM_RESULT_NORMAL] = { dir = "Result_Normal", offset = C.result_offset, motion = C.result_motion_normal, beat = C.result_beat_normal, builder = B.animation };
	[CHARACTER.ANIM_RESULT_CLEAR] = { dir = "Result_Clear", offset = C.result_offset, motion = C.result_motion_clear, beat = C.result_beat_clear, builder = B.animation };
	[CHARACTER.ANIM_RESULT_FAILED_IN] = { dir = "Result_Failed_In", offset = C.result_offset, motion = C.result_motion_failed_in, beat = C.result_beat_failed_in, builder = B.animation };
	[CHARACTER.ANIM_RESULT_FAILED] = { dir = "Result_Failed", offset = C.result_offset, motion = C.result_motion_failed, beat = C.result_beat_failed, builder = B.animation };
} end

local chara_config = {}
local function load_chara_config_defs(L --[[chara_config_loader]]) return { -- need to be an ordered array for fallback dependencies
	{ key = "chara_version", default = "", config = "Chara_Version", loader = L.string };
	{ key = "resolution", default = VECTOR2:CreateVector2(1280, 720), config = "Chara_Resolution", loader = L.int_xy };
	{ key = "legacy_mode", default = true, config = "Chara_LegacyMode", loader = L.bool };
	{ key = "heya_render_offset", default = VECTOR2:CreateVector2(0, 0), config = "Heya_Chara_Render_Offset", loader = L.int_xy };

	{ key = "menu_offset", default = { pos = VECTOR2:CreateVector2(0, 0); mode = OFFSET_MODE_MENU; v1_scale = 1.0; },
		config = "Menu_Offset", loader = L.offset, v1_scale = "Menu_Chara_Scale" };

	{ key = "menu_motion_normal", default = nil, config = "Menu_Chara_Motion_Loop", loader = L.motion };
	{ key = "menu_motion_wait", default = nil, config = "Menu_Chara_Motion_Wait", loader = L.motion };
	{ key = "menu_motion_start", default = nil, config = "Menu_Chara_Motion_Start", loader = L.motion };
	{ key = "menu_motion_select", default = nil, config = "Menu_Chara_Motion_Select", loader = L.motion };
	{ key = "menu_beat_normal", default = 2, config = "Menu_Chara_Beat_Normal", loader = L.beat, duration = "Chara_Menu_Loop_AnimationDuration", ms_per_beat = 500.0 };
	{ key = "menu_beat_wait", default = 1, config = "Menu_Chara_Beat_Wait", loader = L.beat, duration = "Chara_Menu_Wait_AnimationDuration", ms_per_beat = 500.0 };
	{ key = "menu_beat_start", default = 1, config = "Menu_Chara_Beat_Start", loader = L.beat, duration = "Chara_Menu_Start_AnimationDuration", ms_per_beat = 500.0 };
	{ key = "menu_beat_select", default = 2, config = "Menu_Chara_Beat_Select", loader = L.beat, duration = "Chara_Menu_Select_AnimationDuration", ms_per_beat = 500.0 };

	{ key = "title_motion_normal", default = nil, config = "Title_Chara_Motion_Normal", loader = L.motion };
	{ key = "title_motion_entry", default = nil, config = "Title_Chara_Motion_Entry", loader = L.motion };
	{ key = "title_beat_normal", default = 1, config = "Title_Chara_Beat_Normal", loader = L.beat, duration = "Chara_Normal_AnimationDuration" };
	{ key = "title_beat_entry", default = 1, config = "Title_Chara_Beat_Entry", loader = L.beat, duration = "Chara_Entry_AnimationDuration" };

	{ key = "game_offset", default = { pos = VECTOR2:CreateVector2(0, 0); mode = OFFSET_MODE_GAME; v1_scale = 1.25 --[[1 / Game_Chara_Scale]]; },
		config = "Game_Chara_Offset", loader = L.offset, x_P1 = "Game_Chara_X", y_P1 = "Game_Chara_Y" };
	{ key = "game_ai_offset", default = { pos = VECTOR2:CreateVector2(0, 0); mode = OFFSET_MODE_GAME_AI; v1_scale = 1.25 --[[0.58 / Game_Chara_Scale_AI]]; } };
	{ key = "ai_positions_x", default = nil, config = "Game_Chara_X_AI", loader = L.ints };
	{ key = "ai_positions_y", default = nil, config = "Game_Chara_Y_AI", loader = L.ints };
	{ key = "game_balloon_offset", default = { pos = VECTOR2:CreateVector2(0, 0); mode = OFFSET_MODE_GAME_BALLOON; v1_scale = 1; },
		config = "Game_Chara_Balloon_Offset", loader = L.offset, x_P1 = "Game_Chara_Balloon_X", y_P1 = "Game_Chara_Balloon_Y" };
	{ key = "game_kusudama_offset", default = { pos = VECTOR2:CreateVector2(0, 0); mode = OFFSET_MODE_GAME_KUSUDAMA; v1_scale = 1; },
		config = "Game_Chara_Kusudama_Offset", loader = L.offset, x_P1 = "Game_Chara_Kusudama_X", y_P1 = "Game_Chara_Kusudama_Y" };
	{ key = "game_chara_tower_offset", default = { pos = VECTOR2:CreateVector2(0, 0); mode = OFFSET_MODE_GAME_TOWER; v1_scale = 1; },
		config = "Game_Chara_Tower_Offset", loader = L.offset };

	{ key = "game_motion_normal", default = nil, config = "Game_Chara_Motion_Normal", loader = L.motion };
	{ key = "game_motion_clear", default = nil, config = "Game_Chara_Motion_Clear", loader = L.motion };
	{ key = "game_motion_clear_max", default = nil, config = "Game_Chara_Motion_Clear_Max", loader = L.motion, fallbacks = { "game_motion_clear" } };
	{ key = "game_motion_gogo", default = nil, config = "Game_Chara_Motion_GoGo", loader = L.motion };
	{ key = "game_motion_gogo_max", default = nil, config = "Game_Chara_Motion_GoGo_Max", loader = L.motion, fallbacks = { "game_motion_gogo" } };
	{ key = "game_motion_miss", default = nil, config = "Game_Chara_Motion_Miss", loader = L.motion };
	{ key = "game_motion_miss_down", default = nil, config = "Game_Chara_Motion_Miss_Down", loader = L.motion };
	{ key = "game_motion_10combo", default = nil, config = "Game_Chara_Motion_10Combo", loader = L.motion };
	{ key = "game_motion_10combo_max", default = nil, config = "Game_Chara_Motion_10Combo_Max", loader = L.motion };
	{ key = "game_motion_cleared", default = nil, config = "Game_Chara_Motion_Cleared", loader = L.motion };
	{ key = "game_motion_failed", default = nil, config = "Game_Chara_Motion_Failed", loader = L.motion };
	{ key = "game_motion_clearout", default = nil, config = "Game_Chara_Motion_ClearOut", loader = L.motion };
	{ key = "game_motion_clearin", default = nil, config = "Game_Chara_Motion_ClearIn", loader = L.motion };
	{ key = "game_motion_soulout", default = nil, config = "Game_Chara_Motion_SoulOut", loader = L.motion };
	{ key = "game_motion_soulin", default = nil, config = "Game_Chara_Motion_SoulIn", loader = L.motion };
	{ key = "game_motion_missin", default = nil, config = "Game_Chara_Motion_MissIn", loader = L.motion };
	{ key = "game_motion_missdownin", default = nil, config = "Game_Chara_Motion_MissDownIn", loader = L.motion };
	{ key = "game_motion_return", default = nil, config = "Game_Chara_Motion_Return" };
	{ key = "game_motion_gogostart", default = nil, config = "Game_Chara_Motion_GoGoStart", loader = L.motion };
	{ key = "game_motion_gogostart_clear", default = nil, config = "Game_Chara_Motion_GoGoStart_Clear", loader = L.motion, fallbacks = { "game_motion_gogostart" } };
	{ key = "game_motion_gogostart_max", default = nil, config = "Game_Chara_Motion_GoGoStart_Max", loader = L.motion, fallbacks = { "game_motion_gogostart" } };
	{ key = "game_motion_balloon_breaking", default = nil, config = "Game_Chara_Motion_Balloon_Breaking", loader = L.motion };
	{ key = "game_motion_balloon_broke", default = nil, config = "Game_Chara_Motion_Balloon_Broke", loader = L.motion };
	{ key = "game_motion_balloon_miss", default = nil, config = "Game_Chara_Motion_Balloon_Miss", loader = L.motion };
	{ key = "game_motion_kusudama_breaking", default = nil, config = "Game_Chara_Motion_Kusudama_Breaking", loader = L.motion };
	{ key = "game_motion_kusudama_idle", default = nil, config = "Game_Chara_Motion_Kusudama_Idle", loader = L.motion };
	{ key = "game_motion_kusudama_broke", default = nil, config = "Game_Chara_Motion_Kusudama_Broke", loader = L.motion };
	{ key = "game_motion_kusudama_miss", default = nil, config = "Game_Chara_Motion_Kusudama_Miss", loader = L.motion };

	{ key = "game_motion_tower_standing", default = nil, config = "Game_Chara_Motion_Tower_Standing", loader = L.motion };
	{ key = "game_motion_tower_standing_tired", default = nil, config = "Game_Chara_Motion_Tower_Standing_Tired", loader = L.motion, fallbacks = { "game_motion_tower_standing" } };
	{ key = "game_motion_tower_climbing", default = nil, config = "Game_Chara_Motion_Tower_Climbing", loader = L.motion };
	{ key = "game_motion_tower_climbing_tired", default = nil, config = "Game_Chara_Motion_Tower_Climbing_Tired", loader = L.motion, fallbacks = { "game_motion_tower_climbing" } };
	{ key = "game_motion_tower_running", default = nil };
	{ key = "game_motion_tower_running_tired", default = nil };
	{ key = "game_motion_tower_clear", default = nil, config = "Game_Chara_Motion_Tower_Clear", loader = L.motion };
	{ key = "game_motion_tower_clear_tired", default = nil, config = "Game_Chara_Motion_Tower_Clear_Tired", loader = L.motion, fallbacks = { "game_motion_tower_clear" } };
	{ key = "game_motion_tower_fail", default = nil };

	{ key = "game_beat_normal", default = 1, config = "Game_Chara_Beat_Normal", loader = L.beat };
	{ key = "game_beat_clear", default = 1, config = "Game_Chara_Beat_Clear", loader = L.beat };
	{ key = "game_beat_clear_max", default = 1, config = "Game_Chara_Beat_ClearMax", loader = L.beat, fallbacks = { "game_beat_clear" } };
	{ key = "game_beat_gogo", default = 1, config = "Game_Chara_Beat_GoGo", loader = L.beat };
	{ key = "game_beat_gogo_max", default = 1, config = "Game_Chara_Beat_GoGoMax", loader = L.beat, fallbacks = { "game_beat_gogo" } };
	{ key = "game_beat_miss", default = 1, config = "Game_Chara_Beat_Miss", loader = L.beat };
	{ key = "game_beat_miss_down", default = 1, config = "Game_Chara_Beat_MissDown", loader = L.beat };
	{ key = "game_beat_10combo", default = 1.5, config = "Game_Chara_Beat_10Combo", loader = L.beat };
	{ key = "game_beat_10combo_max", default = 1.5, config = "Game_Chara_Beat_10ComboMax", loader = L.beat };
	{ key = "game_beat_cleared", default = 1.5, config = "Game_Chara_Beat_Cleared", loader = L.beat };
	{ key = "game_beat_failed", default = 1.5, config = "Game_Chara_Beat_Failed", loader = L.beat };
	{ key = "game_beat_clearout", default = 1.5, config = "Game_Chara_Beat_ClearOut", loader = L.beat };
	{ key = "game_beat_clearin", default = 1.5, config = "Game_Chara_Beat_ClearIn", loader = L.beat };
	{ key = "game_beat_soulout", default = 1.5, config = "Game_Chara_Beat_SoulOut", loader = L.beat };
	{ key = "game_beat_soulin", default = 1.5, config = "Game_Chara_Beat_SoulIn", loader = L.beat };
	{ key = "game_beat_missin", default = 1, config = "Game_Chara_Beat_MissIn", loader = L.beat };
	{ key = "game_beat_missdownin", default = 1, config = "Game_Chara_Beat_MissDownIn", loader = L.beat };
	{ key = "game_beat_return", default = 1.5, config = "Game_Chara_Beat_Return", loader = L.beat };
	{ key = "game_beat_gogostart", default = 1.5, config = "Game_Chara_Beat_GoGoStart", loader = L.beat };
	{ key = "game_beat_gogostart_clear", default = 1.5, config = "Game_Chara_Beat_GoGoStartClear", loader = L.beat };
	{ key = "game_beat_gogostart_max", default = 1.5, config = "Game_Chara_Beat_GoGoStartMax", loader = L.beat };
	{ key = "game_beat_balloon_breaking", default = 0.25, config = "Game_Chara_Beat_Balloon_Breaking", loader = L.beat };
	{ key = "game_beat_balloon_broke", default = 2.0, config = "Game_Chara_Beat_Balloon_Broke", loader = L.beat };
	{ key = "game_beat_balloon_miss", default = 2.0, config = "Game_Chara_Beat_Balloon_Miss", loader = L.beat };
	{ key = "game_beat_kusudama_breaking", default = 0.25, config = "Game_Chara_Beat_Kusudama_Breaking", loader = L.beat };
	{ key = "game_beat_kusudama_idle", default = 1.0, config = "Game_Chara_Beat_Kusudama_Idle", loader = L.beat };
	{ key = "game_beat_kusudama_broke", default = 2.0, config = "Game_Chara_Beat_Kusudama_Broke", loader = L.beat };
	{ key = "game_beat_kusudama_miss", default = 2.0, config = "Game_Chara_Beat_Kusudama_Miss", loader = L.beat };

	{ key = "game_beat_tower_standing", default = 1.0, config = "Game_Chara_Beat_Tower_Standing", loader = L.beat };
	{ key = "game_beat_tower_standing_tired", default = 1.0, config = "Game_Chara_Beat_Tower_Standing_Tired", loader = L.beat };
	{ key = "game_beat_tower_climbing", default = 1.0, config = "Game_Chara_Beat_Tower_Climbing", loader = L.beat };
	{ key = "game_beat_tower_climbing_tired", default = 1.0, config = "Game_Chara_Beat_Tower_Climbing_Tired", loader = L.beat };
	{ key = "game_beat_tower_running", default = 1.0 };
	{ key = "game_beat_tower_running_tired", default = 1.0 };
	{ key = "game_beat_tower_clear", default = 1.0, config = "Game_Chara_Beat_Tower_Clear", loader = L.beat };
	{ key = "game_beat_tower_clear_tired", default = 1.0, config = "Game_Chara_Beat_Tower_Clear_Tired", loader = L.beat };
	{ key = "game_beat_tower_fail", default = 1.0, config = "Game_Chara_Beat_Tower_Fail", loader = L.beat };

	{ key = "result_offset", default = { pos = VECTOR2:CreateVector2(0, 0); mode = OFFSET_MODE_RESULT; v1_scale = 1; },
		config = "Result_Offset", loader = L.offset };

	{ key = "result_motion_normal", default = nil, config = "Result_Chara_Motion_Normal", loader = L.motion };
	{ key = "result_motion_clear", default = nil, config = "Result_Chara_Motion_Clear", loader = L.motion };
	{ key = "result_motion_failed_in", default = nil, config = "Result_Chara_Motion_Failed_In", loader = L.motion };
	{ key = "result_motion_failed", default = nil, config = "Result_Chara_Motion_Failed", loader = L.motion };
	{ key = "result_beat_normal", default = 1, config = "Result_Chara_Beat_Normal", loader = L.beat, duration = "Chara_Result_Normal_AnimationDuration" };
	{ key = "result_beat_clear", default = 1, config = "Result_Chara_Beat_Clear", loader = L.beat, duration = "Chara_Result_Clear_AnimationDuration" };
	{ key = "result_beat_failed_in", default = 1, config = "Result_Chara_Beat_Failed_In", loader = L.beat, duration = "Chara_Result_Failed_In_AnimationDuration" };
	{ key = "result_beat_failed", default = 1, config = "Result_Chara_Beat_Failed", loader = L.beat, duration = "Chara_Result_Failed_AnimationDuration" };
} end

local function csarray_to_motiontable(csarray)
	local luatable = {}
	for i = 1, csarray.Length, 1 do
		luatable[i] = csarray[i - 1] + 1
	end
	return luatable
end

local chara_config_loader = {} -- (Def, Ini, ValueDefault)
function chara_config_loader.bool(D, I, V) return I:GetBool(D.config, V) end
function chara_config_loader.double(D, I, V) return I:GetDouble(D.config, V) end
function chara_config_loader.string(D, I, V) return I:GetString(D.config, V) end
function chara_config_loader.motion(D, I, V)
	local array = I:GetIntArray(D.config)
	return (array.Length >= 1) and csarray_to_motiontable(array) or V
end
function chara_config_loader.beat(D, I, V)
	local R = V
	if D.duration ~= nil then
		ms_per_beat = D.ms_per_beat
		if ms_per_beat == nil then ms_per_beat = 1000.0 end
		R = I:GetDouble(D.duration, V * ms_per_beat) / ms_per_beat
	end
	return I:GetDouble(D.config, R)
end
function chara_config_loader.int_xy(D, I, V)
	local array = I:GetIntArray(D.config)
	return (array.Length == 2) and VECTOR2:CreateVector2(array[0], array[1]) or V
end
function chara_config_loader.ints(D, I, V)
	local array = I:GetIntArray(D.config)
	return (array.Length >= 1) and array or V
end
function chara_config_loader.int_P1(D, I, V)
	local array = I:GetIntArray(D.config)
	if array.Length >= 1 then return array[0] end
	return V
end
function chara_config_loader.offset(D, I, V)
	-- make a copy to keep the def table intact
	local R = { pos = VECTOR2:CreateVector2(V.pos.X, V.pos.Y); mode = V.mode; v1_scale = V.v1_scale; }
	R.pos = chara_config_loader.int_xy({ config = D.config }, I, R.pos)
	if D.x_P1 ~= nil then R.pos.X = chara_config_loader.int_P1({ config = D.x_P1 }, I, R.pos.X) end
	if D.y_P1 ~= nil then R.pos.Y = chara_config_loader.int_P1({ config = D.y_P1 }, I, R.pos.Y) end
	if D.v1_scale ~= nil then R.v1_scale = I:GetDouble(D.v1_scale, R.v1_scale) end
	return R
end

local function load_config()
	local chara_config_defs = load_chara_config_defs(chara_config_loader)
	local chara_config_ini = INILOADER:LoadIni("CharaConfig.txt")
	for i, v in ipairs(chara_config_defs) do
		-- query all fallback configs from the most falled back config
		-- because there is no Lua API to detect whether the config is defined
		local value = v.default
		local fallbacks = v.fallbacks or {}
		for i = #fallbacks, 1, -1 do
			local fallback = chara_config[fallbacks[i]]
			if fallback ~= nil then
				value = fallback
			end
		end
		if v.loader ~= nil then
			value = v:loader(chara_config_ini, value)
		end
		chara_config[v.key] = value
	end
end
load_config()

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
			draw = function(self, x, y, scaleX, scaleY, opacity, color, overrideOffset, anchor, clip_w, clip_h, clip_x, clip_y, rotation, blendMode, wrapMode)
			end;
			dispose = function(self)
			end;
		}
		return obj
	end;
}

-- flip is negative scale and flipping anchor point
local function flip_anchor(anchor, flipX, flipY)
	anchor = anchor:lower()
	if flipX then
		if anchor:find("left") then
			anchor = anchor:gsub("left", "right")
		elseif anchor:find("right") then
			anchor = anchor:gsub("right", "left")
		end
	end
	if flipY then
		if anchor:find("top") then
			anchor = anchor:gsub("top", "bottom")
		elseif anchor:find("bottom") then
			anchor = anchor:gsub("bottom", "top")
		end
	end
	return anchor
end

local animation_builder = {}

function animation_builder.animation(animation_def)
	local dir = animation_def.dir
	if dir == nil then
		return nil
	end


	local motion = animation_def.motion
	local beat = animation_def.beat
	if beat == nil then
		beat = 1
	end
	local offset = animation_def.offset
	if offset == nil then
		offset = { pos = VECTOR2:CreateVector2(0, 0); mode = OFFSET_MODE_MENU; v1_scale = 1.0; }
	end

	local dir_name = animation_def.dir

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
		animation_data.draw = function(self, x, y, scaleX, scaleY, opacity, color, overrideOffset, anchor, clip_w, clip_h, clip_x, clip_y, rotation, blendMode, wrapMode, player_count, ai_battle_mode)
			local frame = self.frames[self.frame_index]

			local offset = overrideOffset or self.offset
			if offset.mode == OFFSET_MODE_GAME and ai_battle_mode then
				offset = chara_config.game_ai_offset -- replace game_offset
			end
			local offsetX = offset.pos.X
			local offsetY = offset.pos.Y

			local theme_resolution = THEME:GetResolution()
			local baseScale = theme_resolution.Y / chara_config.resolution.Y
			-- negative scale here represents flipping around center, not around anchor
			local flipX = scaleX < 0
			local flipY = scaleY < 0
			scaleX = scaleX * baseScale
			scaleY = scaleY * baseScale

			if chara_config.legacy_mode then
				scaleX = scaleX * offset.v1_scale
				scaleY = scaleY * offset.v1_scale
			end

			offsetX = offsetX * (theme_resolution.X / chara_config.resolution.X)
			offsetY = offsetY * (theme_resolution.Y / chara_config.resolution.Y)

			if frame ~= nil then
				frame:SetScale(scaleX, scaleY)
				frame:SetOpacity(opacity / 255.0)
				frame:SetColor(color)
				if rotation and rotation ~= 0 then frame:SetRotation(rotation) end
				if blendMode then frame:SetBlendMode(blendMode) end
				if wrapMode then frame:SetWrapMode(wrapMode) end
				if anchor ~= nil then
					-- Anchor mode: skip legacy frame-size offset; (x,y) is the anchor point directly
					anchor = flip_anchor(anchor, flipX, flipY)
					if clip_w ~= nil and clip_w > 0 then
						-- Caller supplies crop values in theme-pixel units; divide by scale to get source pixels.
						local abs_sx = math.abs(scaleX)
						local abs_sy = math.abs(scaleY)
						local src_x = math.floor((clip_x or 0) / abs_sx)
						local src_y = math.floor((clip_y or 0) / abs_sy)
						local src_w = math.ceil(clip_w / abs_sx)
						local src_h = math.ceil((clip_h ~= nil and clip_h > 0) and clip_h / abs_sy or frame.Height)
						frame:DrawRectAtAnchor(x + offsetX, y + offsetY, src_x, src_y, src_w, src_h, anchor)
					else
						frame:DrawAtAnchor(x + offsetX, y + offsetY, anchor)
					end
				else
					local anchor = "bottom"
					if chara_config.legacy_mode then
						-- Legacy mode: 0.6.0's drawing coordinates are topleft-anchored in the following offset modes
						-- 0.6.1 OWM skin unified the drawing coordinates to be bottom-anchored with new anchor points.
						-- Due to the 0.6.1 anchor points are not directly convertible into 0.6.0 anchor points,
						-- tokkkom gave a set of offset constants in decimals, which effectively offset in 1080p pixels.
						if offset.mode == OFFSET_MODE_GAME then
							anchor = "topleft"
							offsetX = offsetX - 250 / 1080.0 --[[0.13020833333 * 1920 / 1080]] * theme_resolution.Y
							offsetY = offsetY - 276 / 1080.0 --[[0.25555555555]] * theme_resolution.Y
						elseif offset.mode == OFFSET_MODE_GAME_AI then
							-- bottomleft-anchored in 0.6.0
							anchor = "bottomleft"
						elseif offset.mode == OFFSET_MODE_GAME_BALLOON then
							anchor = "topleft"
							offsetX = offsetX - 470 / 1080.0 --[[0.27216666666 * 0.9 * 1920 / 1080]] * theme_resolution.Y
							offsetY = offsetY - 256 / 1080.0 --[[0.2629629629 * 0.9]] * theme_resolution.Y
						elseif offset.mode == OFFSET_MODE_GAME_KUSUDAMA then
							anchor = "topleft"
							offsetX = offsetX - 471 / 1080.0 --[[0.2555 * 0.96 * 1920 / 1080]] * theme_resolution.Y
							offsetY = offsetY - 265 / 1080.0 --[[0.2555 * 0.96]] * theme_resolution.Y
						elseif offset.mode == OFFSET_MODE_MENU then
							-- already bottom-anchored in 0.6.0, but takkkom gave the same corrections as OFFSET_MODE_GAME (not AI battle), cancelled by Komi
						elseif offset.mode == OFFSET_MODE_RESULT then
							-- topleft in 0.6.0, but takkkom gave no corrections
						end
					end
					anchor = flip_anchor(anchor, flipX, flipY)

					frame:DrawAtAnchor(x + offsetX, y + offsetY, anchor)
				end
				frame:SetOpacity(1.0)
				frame:SetRotation(0)
				frame:SetBlendMode("normal")
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

function animation_builder.preview()
	animation_data = AnimationData.new()

	local preview = nil
	if STORAGE:FileExists("Preview.png") then
		preview = TEXTURE:CreateTexture("Preview.png")
	elseif STORAGE:FileExists("Normal/0.png") then
		preview = TEXTURE:CreateTexture("Normal/0.png")
	end
	animation_data.preview = preview

	animation_data.draw = function(self, x, y, scaleX, scaleY, opacity, color, overrideOffset, anchor, clip_w, clip_h, clip_x, clip_y, rotation, blendMode, wrapMode, player_count, ai_battle_mode)
		local theme_resolution = THEME:GetResolution()
		local baseScale = theme_resolution.Y / chara_config.resolution.Y
		scaleX = scaleX * baseScale
		scaleY = scaleY * baseScale

		local frame = self.preview
		if frame ~= nil then
			frame:SetScale(scaleX, scaleY)
			frame:SetOpacity(opacity / 255.0)
			frame:SetColor(color)
			if rotation and rotation ~= 0 then frame:SetRotation(rotation) end
			if blendMode then frame:SetBlendMode(blendMode) end
			if wrapMode then frame:SetWrapMode(wrapMode) end
			frame:DrawAtAnchor(x, y, "center")
			frame:SetOpacity(1.0)
			frame:SetRotation(0)
			frame:SetBlendMode("normal")
		end
	end
	animation_data.dispose = function(self)
		if self.preview ~= nil then
			self.preview:Dispose()
		end
	end

	return animation_data
end

function animation_builder.render()
	animation_data = AnimationData.new()
	if STORAGE:FileExists("Render.png") then
		animation_data.render = TEXTURE:CreateTexture("Render.png")
	end

	animation_data.draw = function(self, x, y, scaleX, scaleY, opacity, color, overrideOffset, anchor, clip_w, clip_h, clip_x, clip_y, rotation, blendMode, wrapMode, player_count, ai_battle_mode)
		local theme_resolution = THEME:GetResolution()
		local baseScale = theme_resolution.Y / chara_config.resolution.Y
		scaleX = scaleX * baseScale
		scaleY = scaleY * baseScale

		local frame = self.render
		if frame ~= nil then
			frame:SetScale(scaleX, scaleY)
			frame:SetOpacity(opacity / 255.0)
			frame:SetColor(color)
			if rotation and rotation ~= 0 then frame:SetRotation(rotation) end
			if blendMode then frame:SetBlendMode(blendMode) end
			if wrapMode then frame:SetWrapMode(wrapMode) end
			if clip_w ~= nil and clip_w > 0 then
				-- The caller supplies all four crop values in theme-pixel units.
				-- Divide by the combined scale to obtain source-texture pixel coordinates.
				local abs_sx = math.abs(scaleX)
				local abs_sy = math.abs(scaleY)
				local src_x = math.floor((clip_x or 0) / abs_sx)
				local src_y = math.floor((clip_y or 0) / abs_sy)
				local src_w = math.ceil(clip_w / abs_sx)
				local src_h = math.ceil((clip_h ~= nil and clip_h > 0) and clip_h / abs_sy or frame.Height)
				frame:DrawRectAtAnchor(x, y, src_x, src_y, src_w, src_h, "topleft")
			else
				frame:Draw(x, y)
			end
			frame:SetOpacity(1.0)
			frame:SetRotation(0)
			frame:SetBlendMode("normal")
		end
	end
	animation_data.dispose = function(self)
		if self.render ~= nil then
			self.render:Dispose()
		end
	end

	return animation_data
end

animation_defs = load_animation_defs(chara_config, animation_builder)

local available_animations = {}

function availableAnimation(animationType)
	local flag = available_animations[animationType]
	if flag ~= nil and flag == true then
		return true
	else
		return false
	end
end

function loadAnimation(animationType)
	animations[animationType] = animation_defs[animationType]:builder()

	if animations[animationType] ~= nil then
		available_animations[animationType] = true
	end
end

function disposeAnimation(animationType)
	local animation_data = animations[animationType]
	if animation_data ~= nil then
		animation_data:dispose()
	end

	available_animations[animationType] = false
end

function setAnimationDuration(animationType, duration)
	local animation_data = animations[animationType]
	if animation_data ~= nil then
		animation_data.duration = duration
	end
end

function resetAnimationCounter(animationType)
	local animation_data = animations[animationType]
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
	if not availableAnimation(animationType) then
		return false
	end

	local animation = animations[animationType]
	if animation ~= nil then
		return animation:update(delta, looping)
	else
		return false
	end
end

function draw(animationType, x, y, scaleX, scaleY, opacity, color, contextType, anchor, clip_w, clip_h, clip_x, clip_y, rotation, blendMode, wrapMode, gradientMap)
	-- 2 for 1/2 players (unimplemented: 4 for 3/4 players, 5 for 5 players)
	local player_count = (CONFIG.PlayerCount <= 2) and 2 or (CONFIG.PlayerCount <= 4) and 4 or 5
	local ai_battle_mode = CONFIG.IsAIBattleMode
	if not availableAnimation(animationType) then
		return
	end

	local animation = animations[animationType]
	if animation ~= nil then
		local overrideOffset = nil
		if contextType ~= nil and contextType ~= animationType then
			overrideOffset = animation_defs[contextType].offset
		end
		if gradientMap ~= nil then GRADIENT:SetActive(gradientMap) end
		animation:draw(x, y, scaleX, scaleY, opacity, color, overrideOffset, anchor, clip_w, clip_h, clip_x, clip_y, rotation, blendMode, wrapMode, player_count, ai_battle_mode)
		if gradientMap ~= nil then GRADIENT:ClearActive() end
	end
end

-- Returns the drawn dimensions (W, H) of the current animation frame scaled to the theme resolution.
-- Handles both regular animations (.frames) and the render animation (.render).
function getDrawSize(animationType)
	if not availableAnimation(animationType) then
		return 0, 0
	end
	local animation = animations[animationType]
	if animation == nil then return 0, 0 end

	local theme_resolution = THEME:GetResolution()
	local baseScale = theme_resolution.Y / chara_config.resolution.Y

	local frame = nil
	if animation.render ~= nil then
		-- ANIM_RENDER stores the texture in .render instead of .frames
		frame = animation.render
	elseif animation.frames ~= nil then
		local fi = animation.frame_index
		if fi == nil or fi == 0 then fi = 1 end
		frame = animation.frames[fi]
	end

	if frame == nil then return 0, 0 end
	return frame.Width * baseScale, frame.Height * baseScale
end

-- Returns the heya render offset in theme pixels (already scaled to the current resolution).
-- Called by C# (CStageHeya) so the offset can be applied on the C# side.
function getHeyaRenderOffset()
	local theme_resolution = THEME:GetResolution()
	local scaledX = chara_config.heya_render_offset.X * (theme_resolution.X / chara_config.resolution.X)
	local scaledY = chara_config.heya_render_offset.Y * (theme_resolution.Y / chara_config.resolution.Y)
	return scaledX, scaledY
end

-- Returns the AI battle base position (theme pixels) for the given 0-based player index
-- charaScale: the C#-side scale applied to the character (Game_Chara_Scale_AI).
-- Returns nil when Game_Chara_X_AI / Game_Chara_Y_AI are absent in CharaConfig.txt.
function getAIBattlePosition(player, charaScale)
	if chara_config.ai_positions_x == nil or chara_config.ai_positions_y == nil then
		return nil
	end
	if chara_config.ai_positions_x.Length <= player or chara_config.ai_positions_y.Length <= player then
		return nil
	end
	charaScale = charaScale or 1.0
	local theme_resolution = THEME:GetResolution()
	local rx = theme_resolution.X / chara_config.resolution.X
	local ry = theme_resolution.Y / chara_config.resolution.Y
	-- Desired final position in theme pixels.
	local final_x = chara_config.ai_positions_x[player] * rx
	local final_y = chara_config.ai_positions_y[player] * ry
	return final_x, final_y
end
