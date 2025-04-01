﻿using FDK;

namespace OpenTaiko;

public class CStage : CActivity {
	internal EStage eStageID;
	public enum EStage {
		None,
		StartUp,
		Title,  // Title screen
		Options,
		Config,
		SongSelect,
		DanDojoSelect,
		SongLoading,
		Game,
		Results,
		ChangeSkin,                     // #28195 2011.5.4 yyagi
		Heya,
		TaikoTowers,
		BoukenTitle,
		BoukenMap,
		OnlineLounge,
		Encyclopedia,
		AIBattleMode,
		PlayerStats,
		ChartEditor,
		Toolbox,
		CutScene,
		TEMPLATE,           // No effect, for template class
		CRASH,              // Special case, for CSystemError
		CUSTOM,             // For custom stages in the future, generic with lua
		End
	}

	internal EPhase ePhaseID;
	public enum EPhase {
		Common_NORMAL,
		Common_FADEIN,
		Common_FADEOUT,
		Common_EXIT,
		Startup_0_CreateSystemSound,
		Startup_1_InitializeSonglist,
		Startup_2_EnumerateSongs,
		Startup_3_ApplyScoreCache,
		Startup_4_LoadSongsNotSeenInScoreCacheAndApplyThem,
		Startup_5_PostProcessSonglist,
		Startup_6_LoadTextures,
		Startup_Complete,
		Title_FadeIn,
		SongSelect_FadeInFromResults,
		SongSelect_FadeOutToCourseSelect, //2016.10.20 kairera0467
		SongSelect_FadeOutToNowLoading,
		SongLoading_LoadDTXFile,
		SongLoading_WaitToLoadWAVFile,
		SongLoading_LoadWAVFile,
		SongLoading_LoadBMPFile,
		SongLoading_WaitForSoundSystemBGM,
		Game_STAGE_FAILED,
		Game_STAGE_FAILED_FadeOut,
		Game_STAGE_CLEAR_FadeOut,
		Game_EndStage, //2016.07.15 kairera0467
		Game_Reload
	}
}
