using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using FDK;
using ImGuiNET;

namespace OpenTaiko;

/*
 FOR DEBUGGING! This class is intended for developers only!
*/
public static class ImGuiDebugWindow {
	#region Public for debugging
	public static bool DisableKeyboardInputs = false;
	public static string OverrideBGPreset = "";
	#endregion

	private static bool showImGuiDemoWindow = false;
	private static Assembly assemblyinfo = Assembly.GetExecutingAssembly();

	private static long memoryReadTimer = 0;
	private static long pagedmemory = 0;
	private static long textureMemoryUsage = 0;
	private static long currentStageMemoryUsage = 0;

	private static int sortType = -1;
	private static readonly string[] sortNames = ["Memory Usage (Highest -> Lowest)", "Memory Usage (Lowest -> Highest)", "Pointer ID"];
	private static string reloadTexPath = "";

	private static Dictionary<int, string> nameplate_Rarities = new() {
		[0] = "Poor",
		[1] = "Common",
		[2] = "Uncommon",
		[3] = "Rare",
		[4] = "Epic",
		[5] = "Legendary",
		[6] = "Mythical"
	};

	private static Dictionary<string, string> nameplate_unlockCondition = new() {
		{ "ch (Coins here)", "ch" },
		{ "cs (Coins shop)", "cs" },
		{ "cm (Coins menu)", "cm" },
		{ "ce (Coins earned)", "ce" },
		{ "dp (Difficulty pass)", "dp" },
		{ "lp (Level pass)", "lp" },
		{ "sp (Song performance)", "sp" },
		{ "sg (Song genre (performance))", "sg" },
		{ "sc (Song charter (performance))", "sc" },
		{ "tp (Total plays)", "tp" },
		{ "ap (AI battle plays)", "ap" },
		{ "aw (AI battle wins)", "aw" }
	};
	private static int nameplate_ucId = 4;

	private static Dictionary<string, string> nameplate_unlockType = new() {
		{ "l (Less than)", "l" },
		{ "le (Less or equal)", "le" },
		{ "e (Equal)", "e" },
		{ "me (More or equal)", "me" },
		{ "m (More than)", "m" },
		{ "d (Different)", "d" },
	};
	private static int nameplate_utId = 3;

	private static string nameplate_unlockValues = "[]";
	private static string nameplate_unlockReferences = "[]";
	private static Dictionary<string, string> nameplate_Translations = [];
	private static string translation_id = "";

	public static void Draw() {
		if (Game.ImGuiController == null) return;

		#region Fetch allocated memory
		if (SoundManager.PlayTimer.SystemTimeMs - memoryReadTimer > 5000) {
			memoryReadTimer = SoundManager.PlayTimer.SystemTimeMs;
			Task.Factory.StartNew(() => {
				using (Process process = Process.GetCurrentProcess()) {
					if (OperatingSystem.IsWindows())
						pagedmemory = process.PagedMemorySize64;
					else
						pagedmemory = process.WorkingSet64;
				}
			});
		}
		#endregion

		ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 0), ImGuiCond.FirstUseEver);
		ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 300), ImGuiCond.FirstUseEver);
		if (ImGui.Begin("Debug Window (Toggle Visbility with F11)###DEBUG")) {

			#region Debug Info
			ImGui.Checkbox("Show ImGui Demo Window", ref showImGuiDemoWindow);
			if (showImGuiDemoWindow) { ImGui.ShowDemoWindow(); }
			if (ImGui.Checkbox("Disable Keyboard Inputs to OpenTaiko", ref DisableKeyboardInputs))
				((CInputKeyboard)OpenTaiko.InputManager.Keyboard).IMGUI_WindowIsFocused = DisableKeyboardInputs;

			ImGui.Separator();
			ImGui.Text($"Game Version: {OpenTaiko.VERSION}");
			ImGui.Text($"Allocated Memory: {pagedmemory} bytes ({String.Format("{0:0.###}", (float)pagedmemory / (1024 * 1024 * 1024))}GB)");
			ImGui.Text($"FPS: {(OpenTaiko.FPS != null ? OpenTaiko.FPS.NowFPS : "???")}");
			ImGui.Text("Current Stage: " + OpenTaiko.rCurrentStage.eStageID.ToString() + " (StageID " + ((int)OpenTaiko.rCurrentStage.eStageID).ToString() + ")");
			#endregion

			ImGui.BeginTabBar("Tabs");

			#region Tabs
			System();
			Inputs();
			Profile();
			Stage();
			Textures();
			#endregion

			ImGui.EndTabBar();
			
			ImGui.End();
		}
	}
	#region Tabs
	private static void System() {
		if (ImGui.BeginTabItem("System")) {
			ImGui.TextWrapped($"Path: {(Environment.ProcessPath != null ? Environment.ProcessPath : "???")}");
			ImGui.NewLine();
			ImGui.Text($"OS Version: {Environment.OSVersion} ({RuntimeInformation.RuntimeIdentifier})");
			ImGui.Text($"OS Architecture: {RuntimeInformation.OSArchitecture}");
			ImGui.Text($"Framework Version: {RuntimeInformation.FrameworkDescription}");
			ImGui.NewLine();
			ImGui.Text("Graphics API: " + Game.GraphicsDeviceType_);
			ImGui.Text("Audio Device: " + OpenTaiko.SoundManager.GetCurrentSoundDeviceType());

			ImGui.EndTabItem();
		}
	}
	private static void Inputs() {
		if (ImGui.BeginTabItem("Inputs")) {

			ImGui.Text("Total Inputs Found: " + OpenTaiko.InputManager.InputDevices.Count());

			ImGui.NewLine();

			ImGui.Text("Input Count:");
			ImGui.Indent();
			ImGui.Text("Keyboard: " + OpenTaiko.InputManager.InputDevices.Count(device => device.CurrentType == InputDeviceType.Keyboard));
			ImGui.Text("Mouse: " + OpenTaiko.InputManager.InputDevices.Count(device => device.CurrentType == InputDeviceType.Mouse));
			ImGui.Text("Gamepad: " + OpenTaiko.InputManager.InputDevices.Count(device => device.CurrentType == InputDeviceType.Gamepad));
			ImGui.Text("Joystick: " + OpenTaiko.InputManager.InputDevices.Count(device => device.CurrentType == InputDeviceType.Joystick));
			ImGui.Text("MIDI: " + OpenTaiko.InputManager.InputDevices.Count(device => device.CurrentType == InputDeviceType.MidiIn));
			ImGui.Text("Unknown: " + OpenTaiko.InputManager.InputDevices.Count(device => device.CurrentType == InputDeviceType.Unknown));

			foreach (IInputDevice device in OpenTaiko.InputManager.InputDevices) {
				if (ImGui.TreeNodeEx(device.CurrentType.ToString() + " (ID " + device.ID + " / Name: " + device.Name + " / GUID: " + device.GUID + ")")) {
					switch (device.CurrentType) {
						case InputDeviceType.Keyboard:
							var keyboard = (CInputKeyboard)device;
							for (int i = 0; i < keyboard.ButtonStates.Length; i++) {
								if (keyboard.KeyPressed(i)) { ImGui.Text((SlimDXKeys.Key)i + " Pressed!"); }
								if (keyboard.KeyPressing(i)) { ImGui.Text((SlimDXKeys.Key)i + " Pressing!"); }
								if (keyboard.KeyReleased(i)) { ImGui.Text((SlimDXKeys.Key)i + " Released!"); }
							}
							break;
						case InputDeviceType.Mouse:
							var mouse = (CInputMouse)device;
							for (int i = 0; i < mouse.MouseStates.Length; i++) {
								if (mouse.KeyPressed(i)) { ImGui.Text((Silk.NET.Input.MouseButton)i + " Pressed!"); }
								if (mouse.KeyPressing(i)) { ImGui.Text((Silk.NET.Input.MouseButton)i + " Pressing!"); }
								if (mouse.KeyReleased(i)) { ImGui.Text((Silk.NET.Input.MouseButton)i + " Released!"); }
							}
							break;
						case InputDeviceType.Gamepad:
							var gamepad = (CInputGamepad)device;
							for (int i = 0; i < gamepad.ButtonStates.Length; i++) {
								if (gamepad.KeyPressed(i)) { ImGui.Text(gamepad.GetButtonName(i) + " Pressed!"); }
								if (gamepad.KeyPressing(i)) { ImGui.Text(gamepad.GetButtonName(i) + " Pressing!"); }
								if (gamepad.KeyReleased(i)) { ImGui.Text(gamepad.GetButtonName(i) + " Released!"); }
							}
							break;
						case InputDeviceType.Joystick:
							var joystick = (CInputJoystick)device;
							for (int i = 0; i < joystick.ButtonStates.Length; i++) {
								if (joystick.KeyPressed(i)) { ImGui.Text(joystick.GetButtonName(i) + " Pressed!"); }
								if (joystick.KeyPressing(i)) { ImGui.Text(joystick.GetButtonName(i) + " Pressing!"); }
								if (joystick.KeyReleased(i)) { ImGui.Text(joystick.GetButtonName(i) + " Released!"); }
							}
							break;
						case InputDeviceType.MidiIn:
							var midiin = (CInputMIDI)device;
							//for (int i = 0; i < midiin.InputEvents.Count; i++) {
							//	if (midiin.InputEvents[i].Pressed) { ImGui.Text(midiin.InputEvents[i].nKey + " Pressed!"); }
							//	if (midiin.KeyPressing(i)) { ImGui.Text("Pressing!"); }
							//	if (midiin.InputEvents[i].Released) { ImGui.Text(midiin.InputEvents[i].nKey + " Released!"); }
							//}
							ImGui.TextColored(new Vector4(1, 0, 0, 1), "MIDI input polling is currently disabled.");
							break;
					}
					ImGui.TreePop();
				}
			}

			ImGui.EndTabItem();
		}
	}
	private static void Profile() {
		if (ImGui.BeginTabItem("Profile")) {

			ImGui.BeginDisabled(OpenTaiko.rCurrentStage.eStageID == CStage.EStage.Game);
			int count = OpenTaiko.ConfigIni.nPlayerCount;
			if (ImGui.InputInt("Player Count", ref count))
				OpenTaiko.ConfigIni.nPlayerCount = Math.Clamp(count, 1, 5); // funny things can happen when the player count is set to 0
			ImGui.EndDisabled();

			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				if (ImGui.TreeNodeEx($"Player {i + 1}###TREE_PROFILE_{i}", ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.DefaultOpen)) {
					int save = i == 0 ? OpenTaiko.SaveFile : i;

					if (i == 1 && OpenTaiko.ConfigIni.bAIBattleMode)
						ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.8f, 1.0f, 1.0f), "2P is occupied. AI Battle is active.");

					ImGui.Text($"ID: {OpenTaiko.SaveFileInstances[save].data.SaveId}");
					ImGui.InputText($"Name###NAME{i}", ref OpenTaiko.SaveFileInstances[save].data.Name, 64);

					if (ImGui.Button($"Update###UPDATE_PROFILE{i}")) {
						OpenTaiko.SaveFileInstances[save].tApplyHeyaChanges();
						OpenTaiko.NamePlate.tNamePlateRefreshTitles(save);
					}

					string preview = OpenTaiko.SaveFileInstances[save].data.Title;

					if (ImGui.BeginCombo($"Nameplate###NAMEPLATE{i}", preview)) {
						if (ImGui.Selectable("(Clear Title)")) {
							OpenTaiko.SaveFileInstances[save].data.TitleId = -1;
							OpenTaiko.SaveFileInstances[save].data.Title = "";
							OpenTaiko.SaveFileInstances[save].data.TitleRarityInt = 1;
							OpenTaiko.SaveFileInstances[save].data.TitleType = 0;

							OpenTaiko.SaveFileInstances[save].tApplyHeyaChanges();
							OpenTaiko.NamePlate.tNamePlateRefreshTitles(save);
						}
						if (ImGui.Selectable("初心者")) {
							OpenTaiko.SaveFileInstances[save].data.TitleId = -1;
							OpenTaiko.SaveFileInstances[save].data.Title = "初心者";
							OpenTaiko.SaveFileInstances[save].data.TitleRarityInt = 1;
							OpenTaiko.SaveFileInstances[save].data.TitleType = 0;

							OpenTaiko.SaveFileInstances[save].tApplyHeyaChanges();
							OpenTaiko.NamePlate.tNamePlateRefreshTitles(save);
						}
						foreach (long id in OpenTaiko.Databases.DBNameplateUnlockables.data.Keys) {
							bool unlocked = OpenTaiko.SaveFileInstances[save].data.UnlockedNameplateIds.Contains((int)id);

							if (ImGui.Selectable(OpenTaiko.Databases.DBNameplateUnlockables.data[id].nameplateInfo.cld.GetString("") + (!unlocked ? " [Locked]" : ""), OpenTaiko.SaveFileInstances[save].data.TitleId == (int)id)) {
								var nameplate = OpenTaiko.Databases.DBNameplateUnlockables.data[id];
								OpenTaiko.SaveFileInstances[save].data.TitleId = (int)id;
								OpenTaiko.SaveFileInstances[save].data.Title = nameplate.nameplateInfo.cld.GetString("");
								OpenTaiko.SaveFileInstances[save].data.TitleRarityInt = HRarity.tRarityToLangInt(nameplate.rarity);
								OpenTaiko.SaveFileInstances[save].data.TitleType = nameplate.nameplateInfo.iType;

								OpenTaiko.SaveFileInstances[save].tApplyHeyaChanges();
								OpenTaiko.NamePlate.tNamePlateRefreshTitles(save);
							}
						}
						ImGui.EndCombo();
					}

					if (ImGui.TreeNodeEx($"Edit Nameplate###EDIT_NAMEPLATE{i}")) {

						ImGui.InputInt($"Title Id###TITLE_ID{i}", ref OpenTaiko.SaveFileInstances[save].data.TitleId);

						ImGui.InputText($"Title###TITLE{i}", ref OpenTaiko.SaveFileInstances[save].data.Title, 1024);

						ImGui.InputInt($"Title Rarity###TITLE_RARITY{i}", ref OpenTaiko.SaveFileInstances[save].data.TitleRarityInt);

						ImGui.InputInt($"Title Type###TITLE_TYPE{i}", ref OpenTaiko.SaveFileInstances[save].data.TitleType);

						if (ImGui.Button($"Update###UPDATE_NAMEPLATE{i}")) {
							OpenTaiko.SaveFileInstances[save].tApplyHeyaChanges();
							OpenTaiko.NamePlate.tNamePlateRefreshTitles(save);
						}

						#region NameplateUnlockables.db3
						ImGui.SeparatorText("Add to NameplateUnlockables.db3");

						if (ImGui.BeginCombo("Unlock Condition", nameplate_unlockCondition.Keys.ElementAt(nameplate_ucId))) {
							foreach (var item in nameplate_unlockCondition) {
								bool selected = nameplate_unlockCondition.Keys.ElementAt(nameplate_ucId) == item.Key;
								if (ImGui.Selectable(item.Key, selected)) {
									nameplate_ucId = nameplate_unlockCondition.ToList().IndexOf(item);
								}
							}
							ImGui.EndCombo();
						}

						if (ImGui.BeginCombo("Unlock Type", nameplate_unlockType.Keys.ElementAt(nameplate_utId))) {
							foreach (var item in nameplate_unlockType) {
								bool selected = nameplate_unlockType.Keys.ElementAt(nameplate_utId) == item.Key;
								if (ImGui.Selectable(item.Key, selected)) {
									nameplate_utId = nameplate_unlockType.ToList().IndexOf(item);
								}
							}
							ImGui.EndCombo();
						}

						ImGui.InputTextWithHint("Unlock Values", "[0,0,0]", ref nameplate_unlockValues, 256);

						ImGui.InputTextWithHint("Unlock References", "[\"songId\"]", ref nameplate_unlockReferences, 2048);

						ImGui.Text("Translations");
						foreach (var translation in nameplate_Translations) {
							string value = translation.Value;
							if (ImGui.InputText(translation.Key + $"###NAMEPLATE_TRANSLATE_{translation.Key.ToUpper()}", ref value, 1024)) {
								nameplate_Translations[translation.Key] = value;
							}
						}

						ImGui.InputText("Id to Add/Remove", ref translation_id, 32);
						if (ImGui.Button("Add")) {
							nameplate_Translations.TryAdd(translation_id, "");
						}
						if (ImGui.Button("Remove")) {
							nameplate_Translations.Remove(translation_id);
						}

						ImGui.SeparatorText("");

						if (ImGui.Button("Add Current Nameplate to Database###NAMEPLATE_DATABASE_ADD")) {
							OpenTaiko.Databases.DBNameplateUnlockables.AddToDatabase(
								OpenTaiko.SaveFileInstances[save].data.Title,
								OpenTaiko.SaveFileInstances[save].data.TitleType,
								nameplate_Rarities[OpenTaiko.SaveFileInstances[save].data.TitleRarityInt],
								nameplate_unlockCondition.Values.ToList()[nameplate_ucId],
								nameplate_unlockType.Values.ToList()[nameplate_utId],
								nameplate_unlockValues,
								nameplate_unlockReferences,
								nameplate_Translations
								);
						}
						#endregion

						ImGui.TreePop();

					}

					if (ImGui.BeginCombo($"Dan Title###DAN_TITLE{i}", OpenTaiko.SaveFileInstances[save].data.Dan)) {
						if (ImGui.Selectable("(Clear Dan)")) {
							OpenTaiko.SaveFileInstances[save].data.Dan = "";
							OpenTaiko.SaveFileInstances[save].data.DanGold = false;
							OpenTaiko.SaveFileInstances[save].data.DanType = 0;

							OpenTaiko.SaveFileInstances[save].tApplyHeyaChanges();
							OpenTaiko.NamePlate.tNamePlateRefreshTitles(save);
						}
						if (ImGui.Selectable("新人")) {
							OpenTaiko.SaveFileInstances[save].data.Dan = "新人";
							OpenTaiko.SaveFileInstances[save].data.DanGold = false;
							OpenTaiko.SaveFileInstances[save].data.DanType = 0;

							OpenTaiko.SaveFileInstances[save].tApplyHeyaChanges();
							OpenTaiko.NamePlate.tNamePlateRefreshTitles(save);

						}
						foreach (var dan in OpenTaiko.SaveFileInstances[save].data.DanTitles) {
							if (ImGui.Selectable(dan.Key)) {
								OpenTaiko.SaveFileInstances[save].data.Dan = dan.Key;
								OpenTaiko.SaveFileInstances[save].data.DanGold = dan.Value.isGold;
								OpenTaiko.SaveFileInstances[save].data.DanType = dan.Value.clearStatus;

								OpenTaiko.SaveFileInstances[save].tApplyHeyaChanges();
								OpenTaiko.NamePlate.tNamePlateRefreshTitles(save);
							}
						}

						ImGui.EndCombo();
					}

					if (ImGui.TreeNodeEx($"Edit Dan Title###EDIT_DAN_TITLE{i}")) {

						ImGui.InputText($"Title###DAN_TITLE{i}", ref OpenTaiko.SaveFileInstances[save].data.Dan, 16);

						ImGui.Checkbox($"Gold###DAN_GOLD{i}", ref OpenTaiko.SaveFileInstances[save].data.DanGold);

						string[] clear_types = ["Clear", "FC", "Perfect"];
						int clear_int = OpenTaiko.SaveFileInstances[save].data.DanType;
						if (ImGui.BeginCombo($"Clear Type###CLEAR_TYPE{i}", clear_types[clear_int])) {
							for (int clear = 0; clear < clear_types.Length; clear++) {
								if (ImGui.Selectable(clear_types[clear], clear_int == clear)) OpenTaiko.SaveFileInstances[save].data.DanType = clear;
							}
							ImGui.EndCombo();
						}

						if (ImGui.Button($"Update###UPDATE_DAN{i}")) {
							OpenTaiko.SaveFileInstances[save].tApplyHeyaChanges();
							OpenTaiko.NamePlate.tNamePlateRefreshTitles(save);
						}
						ImGui.TreePop();
					}


					int current_chara = OpenTaiko.SaveFileInstances[save].data.Character;
					if (OpenTaiko.rCurrentStage.eStageID == CStage.EStage.StartUp) {
						ImGui.TextDisabled("Character selection unavailable during StartUp stage.");
					}
					else if (ImGui.BeginCombo($"Select Character###SELECT_CHARACTER{i}", OpenTaiko.Tx.Characters[current_chara].metadata.tGetName())) {
						for (int chara = 0; chara < OpenTaiko.Tx.Characters.Length; chara++) {
							if (ImGui.Selectable(OpenTaiko.Tx.Characters[chara].metadata.tGetName(), current_chara == chara)) {
								OpenTaiko.Tx.ReloadCharacter(current_chara, chara, save);
								OpenTaiko.SaveFileInstances[save].data.Character = chara;

								OpenTaiko.SaveFileInstances[save].tUpdateCharacterName(OpenTaiko.Skin.Characters_DirName[chara]);
								OpenTaiko.Skin.voiceTitleSanka[save]?.tPlay();
								foreach (var animation in Enum.GetValues<CMenuCharacter.ECharacterAnimation>()) {
									CMenuCharacter.tMenuResetTimer(animation);
								}
								OpenTaiko.SaveFileInstances[save].tApplyHeyaChanges();
							}
						}
						ImGui.EndCombo();
					}

					ImGui.NewLine();

					ImGui.Text($"Total Plays: {OpenTaiko.SaveFileInstances[save].data.TotalPlaycount}");
					ImGui.Text($"Coins: {OpenTaiko.SaveFileInstances[save].data.Medals} (Lifetime: {OpenTaiko.SaveFileInstances[save].data.TotalEarnedMedals})");

					ImGui.TreePop();
				}
			}
			ImGui.EndTabItem();
		}
	}
	private static void Stage() {
		if (ImGui.BeginTabItem("Stage")) {

			switch (OpenTaiko.rCurrentStage.eStageID) {
				case CStage.EStage.SongSelect:
				case CStage.EStage.DanDojoSelect:
					System.Numerics.Vector4 normal = new System.Numerics.Vector4(1, 1, 1, 1);
					System.Numerics.Vector4 diff = new System.Numerics.Vector4(0.5f, 1, 0.5f, 1);

					if (OpenTaiko.rCurrentStage.eStageID == CStage.EStage.SongSelect && ImGui.TreeNodeEx("Current Song", ImGuiTreeNodeFlags.Framed)) {
						if (OpenTaiko.stageSongSelect.actSongList.rCurrentlySelectedSong != null) {
							CSongListNode song = OpenTaiko.stageSongSelect.actSongList.rCurrentlySelectedSong;

							ImGui.Text($"Index: {OpenTaiko.stageSongSelect.actSongList.nSelectSongIndex}");
							ImGui.Text($"Open Index: {song.Openindex}");
							ImGui.Text($"Is Root: {song.rParentNode == null}");
							ImGui.NewLine();
							ImGui.Text("Title: " + song.ldTitle.GetString("???"));
							ImGui.Text("Node Type: " + song.nodeType);
							if (song.nodeType == CSongListNode.ENodeType.SCORE) {
								if (ImGui.TreeNodeEx("Song Info")) {
									for (int i = 0; i < song.nLevel.Count(); i++) {
										if (song.nLevel[i] != -1) ImGui.Text($"{(Difficulty)i}: {song.nLevel[i]}");
									}
									if (song.nLevel[(int)Difficulty.Dan] != -1) {
										if (ImGui.TreeNodeEx("Dan Songs")) {
											for (int j = 0; j < song.DanSongs.Count; j++) {
												var dan_song = song.DanSongs[j];
												Vector4 is_hidden = dan_song.bTitleShow ? new(1, 0.5f, 1, 1) : new(1);
												ImGui.TextColored(is_hidden, $"Song {j+1}: {dan_song.Title}{(dan_song.bTitleShow ? " (Hidden)" : "")}");
												ImGui.Indent();
												ImGui.TextColored(is_hidden, $"Difficulty: {(Difficulty)dan_song.Difficulty}");
												ImGui.TextColored(is_hidden, $"Level: {dan_song.Level}");
												ImGui.TextColored(is_hidden, $"Subtitle: {dan_song.SubTitle}");
												ImGui.TextColored(is_hidden, $"Genre: {dan_song.Genre}");
												if (ImGui.TreeNodeEx($"Dan_C###DAN_C{j}")) {
													for (int i = 0; i < dan_song.Dan_C.Length; i++) {
														if (dan_song.Dan_C[i] != null) {
															var dan_c = dan_song.Dan_C[i];
															ImGui.Text($"Exam {i+1}: {dan_c.ExamType} ({dan_c.ExamRange} - {dan_c.GetValue()[0]} - {dan_c.GetValue()[1]})");
														}
														else
															ImGui.TextDisabled($"Exam {i+1}: null");
													}
													ImGui.TreePop();
												}
												ImGui.Unindent();
											}
											ImGui.TreePop();
										}
									}
									if (song.nLevel[(int)Difficulty.Tower] != -1) {
										ImGui.Text($"Side: {song.nSide}");
										ImGui.Text($"Floor Count: {song.score[5]?.譜面情報.nTotalFloor.ToString() ?? "???"}");
										ImGui.Text($"Life: {song.score[5]?.譜面情報.nLife.ToString() ?? "?"}");
									}
									ImGui.TreePop();
								}
							}
							ImGui.NewLine();
						}
						else {
							ImGui.TextDisabled("Current Song is null. How is this possible...?");
						}
						ImGui.TreePop();
					}

					ImGui.TextColored(OpenTaiko.ConfigIni.SongPlaybackSpeed == 1 ? normal : diff,
						String.Format("Song Playback Speed: {0:0.00}", OpenTaiko.ConfigIni.SongPlaybackSpeed));

					ImGui.TextColored(OpenTaiko.ConfigIni.bTokkunMode ? diff : normal,
						"Training Mode: " + OpenTaiko.ConfigIni.bTokkunMode);

					ImGui.InputText("Set BG Preset", ref OverrideBGPreset, 1024);
					ImGui.Text("Type any preset name to override for the next selected song.\nLeave blank to disable this.");

					ImGui.NewLine();

					for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {

						bool modifiedstats =
							OpenTaiko.ConfigIni.nScrollSpeed[i] != 9 ||
							OpenTaiko.ConfigIni.eSTEALTH[i] != EStealthMode.Off ||
							OpenTaiko.ConfigIni.eRandom[i] != ERandomMode.Off ||
							OpenTaiko.ConfigIni.nTimingZones[i] != 2 ||
							OpenTaiko.ConfigIni.bJust[i] != 0 ||
							OpenTaiko.ConfigIni.nGameType[i] != EGameType.Taiko ||
							OpenTaiko.ConfigIni.nFunMods[i] != EFunMods.None;

						if (ImGui.TreeNodeEx($"Player {i + 1} {(modifiedstats ? "*" : "")}###TREE_SONGSELECT_PLAYER_{i}", ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.DefaultOpen)) {

							ImGui.TextColored(OpenTaiko.ConfigIni.nScrollSpeed[i] == 9 ? normal : diff,
								String.Format("Scroll Speed: {0:0.0}", (OpenTaiko.ConfigIni.nScrollSpeed[i] + 1) / 10.0f));

							ImGui.TextColored(OpenTaiko.ConfigIni.eSTEALTH[i] == EStealthMode.Off ? normal : diff,
								"Stealth: " + OpenTaiko.ConfigIni.eSTEALTH[i].ToString());

							ImGui.TextColored(OpenTaiko.ConfigIni.eRandom[i] == ERandomMode.Off ? normal : diff,
								"Random: " + OpenTaiko.ConfigIni.eRandom[i].ToString());

							ImGui.TextColored(OpenTaiko.ConfigIni.nTimingZones[i] == 2 ? normal : diff,
								"Timing: " + CLangManager.LangInstance.GetString($"MOD_TIMING{OpenTaiko.ConfigIni.nTimingZones[i] + 1}"));

							string[] justice = ["None", "Just", "Safe"];
							ImGui.TextColored(OpenTaiko.ConfigIni.bJust[i] == 0 ? normal : diff,
								"Justice Mode: " + justice[OpenTaiko.ConfigIni.bJust[i]]);

							ImGui.TextColored(OpenTaiko.ConfigIni.nGameType[i] == EGameType.Taiko ? normal : diff,
								"Game Type: " + OpenTaiko.ConfigIni.nGameType[i].ToString());

							ImGui.TextColored(OpenTaiko.ConfigIni.bAutoPlay[i] ? diff : normal,
								"Auto: " + OpenTaiko.ConfigIni.bAutoPlay[i]);

							ImGui.Text("Hitsound: " + OpenTaiko.Skin.hsHitSoundsInformations.names[OpenTaiko.ConfigIni.nHitSounds[i]].GetString("???"));

							ImGui.TextColored(OpenTaiko.ConfigIni.nFunMods[i] == EFunMods.None ? normal : diff,
								"Fun Mods: " + OpenTaiko.ConfigIni.nFunMods[i].ToString());

							ImGui.TreePop();
						}
					}
					break;
				case CStage.EStage.Game:
					if (OpenTaiko.ConfigIni.bAIBattleMode) {
						int level = OpenTaiko.ConfigIni.nAILevel - 1;
						ImGui.TextColored(new(0.5f, 1, 1, 1), "AI Battle is Active.");
						ImGui.Text("AI Level: " + (level+1));
						ImGui.Indent();
						ImGui.Text("Current AI Performance:");
						ImGui.Text($"Good: {OpenTaiko.ConfigIni.apAIPerformances[level].nPerfectOdds}/1000 ({OpenTaiko.ConfigIni.apAIPerformances[level].nPerfectOdds / 10.0}％)");
						ImGui.Text($"Ok: {OpenTaiko.ConfigIni.apAIPerformances[level].nGoodOdds}/1000 ({OpenTaiko.ConfigIni.apAIPerformances[level].nGoodOdds / 10.0}％)");
						ImGui.Text($"Bad: {OpenTaiko.ConfigIni.apAIPerformances[level].nBadOdds}/1000 ({OpenTaiko.ConfigIni.apAIPerformances[level].nBadOdds / 10.0}％)");
						ImGui.Text($"Mine: {OpenTaiko.ConfigIni.apAIPerformances[level].nMineHitOdds}/1000 ({OpenTaiko.ConfigIni.apAIPerformances[level].nMineHitOdds / 10.0}％)");
						ImGui.Text($"Roll Speed: {OpenTaiko.ConfigIni.apAIPerformances[level].nRollSpeed}/s");
						ImGui.Unindent();
					}
					for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
						if (ImGui.TreeNodeEx($"Player {i + 1}###GAME_CHART_{i}", ImGuiTreeNodeFlags.Framed)) {

							Difficulty game_difficulty = OpenTaiko.DifficultyNumberToEnum(OpenTaiko.stageSongSelect.nChoosenSongDifficulty[i]);
							var dtx = OpenTaiko.GetTJA(i);

							switch (game_difficulty) {
								case Difficulty.Dan:
									ImGui.SeparatorText("Dan Dojo Mode");
									break;
								case Difficulty.Tower:
									ImGui.SeparatorText("Tower Mode");
									ImGui.Text("Side: " + dtx.SIDE);
									ImGui.Text("Life: " + dtx.LIFE);
									ImGui.Text("Floor Count: " + OpenTaiko.stageSongSelect.rNowSelectedSong.score[5].譜面情報.nTotalFloor);
									break;
								default:
									ImGui.SeparatorText(OpenTaiko.ConfigIni.nGameType[i] == EGameType.Konga ? "Konga Mode" : "Taiko Mode");
									break;

							}
							ImGui.TextColored(ColorToVector4(OpenTaiko.Skin.SongSelect_Difficulty_Colors[(int)game_difficulty]), $"Difficulty: {game_difficulty}");
							ImGui.Text($"Auto Play: " + OpenTaiko.ConfigIni.bAutoPlay[i]);

							var db現在時刻ms = dtx.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs);
							double play_time = dtx.TjaTimeToRawTjaTimeNote(db現在時刻ms);
							var play_bpm_points = new[] {
								CStage演奏画面共通.GetNowPBPMPoint(dtx, play_time, CTja.ECourse.eNormal),
								CStage演奏画面共通.GetNowPBPMPoint(dtx, play_time, CTja.ECourse.eExpert),
								CStage演奏画面共通.GetNowPBPMPoint(dtx, play_time, CTja.ECourse.eMaster),
							};
							float[] play_th16Beats = play_bpm_points.Select(bp => (float)CStage演奏画面共通.GetNowPBMTime(bp, play_time)).ToArray();
							for (int ib = 0; ib < 3; ++ib) {
								ImGui.Text($"{(CTja.ECourse)ib}: {play_time:0} ms, {play_th16Beats[ib] / 4:0.00} 16ths\n"
									+ $" {play_bpm_points[ib]}\n");
							}

							ImGui.NewLine();

							ImGui.Text("ID: " + dtx.uniqueID.data.id);
							ImGui.Text("Title: " + dtx.TITLE.GetString(""));
							ImGui.Text("Subtitle: " + dtx.SUBTITLE.GetString(""));
							ImGui.Text("Charter: " + dtx.MAKER);

							ImGui.Text("BPM: " + dtx.BASEBPM + (dtx.listBPM.Count > 1 ? (" (Min: " + dtx.MinBPM + " / Max: " + dtx.MaxBPM + ")") : ""));
							if (dtx.listBPM.Count > 1) {
								if (ImGui.TreeNodeEx($"BPM List ({dtx.listBPM.Count})###GAME_BPM_LIST_{i}")) {
									foreach (CTja.CBPM bpm in dtx.listBPM) {
										ImGui.Text($"(Time: {String.Format("{0:0.#}s", (bpm.bpm_change_time / 1000))}) {bpm.dbBPM値}");
									}
									ImGui.TreePop();
								}
							}

							ImGui.Text("Lyrics: " + (dtx.usingLyricsFile ? dtx.listLyric2.Count : dtx.listLyric.Count));

							ImGui.NewLine();

							ImGui.Text("Note Count: ");
							ImGui.Indent();
							ImGui.Text("Normal: " + dtx.nノーツ数_Branch[0] +
									   " / Expert: " + dtx.nノーツ数_Branch[1] +
									   " / Master: " + dtx.nノーツ数_Branch[2]);
							ImGui.Unindent();

							ImGui.TreePop();
						}

					}

					break;
			}

			ImGui.EndTabItem();
		}
	}
	private static void Textures() {
		if (ImGui.BeginTabItem("Textures")) {
			if (ImGui.BeginCombo("Change listTexture Sort###TEXTURE_TOTAL_SORT", sortType != -1 ? sortNames[sortType] : "(Default)")) {
				if (ImGui.Selectable(sortNames[0], sortType == 0)) {
					OpenTaiko.Tx.listTexture.Sort((tex1, tex2) => (tex2 != null ? tex2.szTextureSize.Width * tex2.szTextureSize.Height : -1).CompareTo(tex1 != null ? tex1.szTextureSize.Width * tex1.szTextureSize.Height : -1));
					sortType = 0;
				}
				if (ImGui.Selectable(sortNames[1], sortType == 1)) {
					OpenTaiko.Tx.listTexture.Sort((tex1, tex2) => (tex1 != null ? tex1.szTextureSize.Width * tex1.szTextureSize.Height : -1).CompareTo(tex2 != null ? tex2.szTextureSize.Width * tex2.szTextureSize.Height : -1));
					sortType = 1;
				}
				if (ImGui.Selectable(sortNames[2], sortType == 2)) {
					OpenTaiko.Tx.listTexture.Sort((tex1, tex2) => (tex1 != null ? (int)tex1.Pointer : -1).CompareTo(tex2 != null ? (int)tex2.Pointer : -1));
					sortType = 2;
				}
				ImGui.EndCombo();
			}
			CTextureListPopup(OpenTaiko.Tx.listTexture, "Show listTexture", "TEXTURE_ALL");

			currentStageMemoryUsage = 0;

			#region Script.lua Memory Usage
			int index = 0;
			foreach (CLuaScript luascript in CLuaScript.listScripts) {
				currentStageMemoryUsage += CTextureListPopup(luascript.listDisposables.OfType<CTexture>(),
					$"Module #{index}", $"MODULE{index++}_TEXTURES");
			}

			switch (OpenTaiko.rCurrentStage.eStageID) {
				#region Game
				case CStage.EStage.Game:

					currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actBackground.UpScript,
						"Up Background", "TEXTURE_LUA_UPBG");
					currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actBackground.DownScript,
						"Down Background", "TEXTURE_LUA_DOWNBG");
					currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actMob.MobScript,
						"Mob", "TEXTURE_LUA_MOB");
					currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actBalloon.KusudamaScript,
						"Kusudama", "TEXTURE_LUA_KUSUDAMA");

					#region Endings
					switch ((Difficulty)OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0]) {
						case Difficulty.Tower:
							currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actEnd.Tower_DropoutScript,
								"Tower Dropout", "TEXTURE_LUA_TOWERDROPOUT");
							currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actEnd.Tower_TopReached_PassScript,
								"Tower Cleared", "TEXTURE_LUA_TOWERCLEAR");
							currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actEnd.Tower_TopReached_FullComboScript,
								"Tower Full Combo", "TEXTURE_LUA_TOWERFC");
							currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actEnd.Tower_TopReached_PerfectScript,
								"Tower Perfect Combo", "TEXTURE_LUA_TOWERPFC");
							break;
						case Difficulty.Dan:
							currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actEnd.Dan_FailScript,
								"Dan Clear Failed", "TEXTURE_LUA_DANFAILED");
							currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actEnd.Dan_Red_PassScript,
								"Dan Red Clear", "TEXTURE_LUA_DANCLEAR");
							currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actEnd.Dan_Red_FullComboScript,
								"Dan Red Full Combo", "TEXTURE_LUA_DANFC");
							currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actEnd.Dan_Red_PerfectScript,
								"Dan Red Perfect", "TEXTURE_LUA_DANPFC");
							currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actEnd.Dan_Gold_PassScript,
								"Dan Gold Clear", "TEXTURE_LUA_DANGOLDCLEAR");
							currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actEnd.Dan_Gold_FullComboScript,
								"Dan Gold Full Combo", "TEXTURE_LUA_DANGOLDFC");
							currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actEnd.Dan_Gold_PerfectScript,
								"Dan Gold Perfect", "TEXTURE_LUA_DANGOLDPFC");
							break;
						default:
							if (OpenTaiko.ConfigIni.bAIBattleMode) {
								currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actEnd.AILoseScript,
									"AI Clear Failed", "TEXTURE_LUA_AIFAILED");
								currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actEnd.AIWinScript,
									"AI Cleared", "TEXTURE_LUA_AICLEAR");
								currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actEnd.AIWin_FullComboScript,
									"AI Full Combo", "TEXTURE_LUA_AIFC");
								currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actEnd.AIWin_PerfectScript,
									"AI Perfect Combo", "TEXTURE_LUA_AIPFC");
							} else {
								currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actEnd.FailedScript,
									"Clear Failed", "TEXTURE_LUA_GAMEFAILED");
								currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actEnd.ClearScript,
									"Cleared", "TEXTURE_LUA_GAMECLEAR");
								currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actEnd.FullComboScript,
									"Full Combo", "TEXTURE_LUA_GAMEFC");
								currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stageGameScreen.actEnd.PerfectComboScript,
									"Perfect Combo", "TEXTURE_LUA_GAMEPFC");
							}
							break;
					}
					#endregion

					#endregion

					break;
			}

			ImGui.Text("Script.lua Tex Memory Usage: " + GetMemAllocationInMegabytes(currentStageMemoryUsage) + "MB");
			#endregion

			ImGui.EndTabItem();
		}
	}
	#endregion

	#region ImGui Items
	private static void CTexturePopup(CTexture texture, string label) {
		if (ImGui.TreeNodeEx(label, ImGuiTreeNodeFlags.Framed)) {

			ImGui.BeginDisabled();
			ImGui.InputText("Path", ref reloadTexPath, 2048);
			if (ImGui.Button("Reload via. Path (To-do)")) {
				// To-do
			}
			ImGui.EndDisabled();

			ImGui.TreePop();
		}
		if (ImGui.IsItemHovered(ImGuiHoveredFlags.DelayNone)) {
			if (ImGui.BeginItemTooltip()) {
				if (DrawCTextureForImGui(texture, 800, 800)) {
					ImGui.Text("Pointer: " + texture.Pointer);
					ImGui.Text("Size: x" + texture.szTextureSize.Width + ",y" + texture.szTextureSize.Height);
					ImGui.Text("Memory allocated: " + String.Format("{0:0.###}", GetTextureMemAllocationInMegabytes(texture)) + "MB");
				} else {
					ImGui.TextDisabled("Texture is not loaded.");
				}
				ImGui.EndTooltip();
			}
		}
	}
	private static long CTextureListPopup(IEnumerable<CTexture> textureList, string label, string id) {
		if (textureList == null) return 0;
		try {
			long memoryCount = GetTotalMemoryUsageFromCTextureList(textureList);

			if (ImGui.TreeNodeEx($"{label} Textures: ({textureList.Count()} / {String.Format("{0:0.###}", GetMemAllocationInMegabytes(memoryCount))}MB)###{id}")) {
				int index = 0;
				try {
					foreach (CTexture tex in textureList) {
						CTexturePopup(tex, $"Texture #{index} (Pointer: {(tex != null ? tex.Pointer : "null")})###{id}_{index++}");
					}
				} catch (InvalidOperationException ex) {
					ImGui.Text("(updating...)");
				}
				ImGui.TreePop();
			}
			return memoryCount;
		} catch (InvalidOperationException ex) {
			ImGui.Text($"{label} Textures: (updating...)");
			return 0;
		}
	}
	private static long CTextureListPopup(ScriptBG script, string label, string id) {
		return script != null ? CTextureListPopup(script.Textures.Values, label, id) : 0;
	}
	private static bool DrawCTextureForImGui(CTexture texture) {
		if (texture == null) return false;
		return DrawCTextureForImGui(texture,
			new Vector2(texture.szTextureSize.Width, texture.szTextureSize.Height),
			new Vector2(0, 0), new Vector2(1, 1));
	}
	private static bool DrawCTextureForImGui(CTexture texture, int max_width, int max_height) {
		if (texture == null) return false;
		return DrawCTextureForImGui(texture, 0, 0,
			Math.Min(texture.szTextureSize.Width, max_width), Math.Min(texture.szTextureSize.Height, max_height));
	}
	private static bool DrawCTextureForImGui(CTexture texture, int x, int y, int width, int height) {
		return DrawCTextureForImGui(texture, new Rectangle(x, y, width, height));
	}
	private static bool DrawCTextureForImGui(CTexture texture, Rectangle rect) {
		if (texture == null) return false;
		return DrawCTextureForImGui(texture,
			new Vector2(rect.Width, rect.Height),
			new Vector2((float)rect.X / texture.szTextureSize.Width, (float)rect.Y / texture.szTextureSize.Height),
			new Vector2((float)rect.Right / texture.szTextureSize.Width, (float)rect.Bottom / texture.szTextureSize.Height));
	}
	/// <param name="image_size">Must be in pixels</param>
	/// <param name="pos">Value is typically between 0.0f and 1.0f</param>
	/// <param name="size">Value is typically between 0.0f and 1.0f</param>
	private static bool DrawCTextureForImGui(CTexture texture, Vector2 image_size, Vector2 pos, Vector2 size) {
		if (texture == null) return false;
		ImGui.Image((nint)texture.Pointer, image_size, pos, size);
		return true;
	}
	#endregion

	#region Helpers
	private static float GetMemAllocationInMegabytes(long bytes) { return (float)bytes / (1024 * 1024); }
	private static float GetTextureMemAllocationInMegabytes(CTexture texture) {
		return (float)GetTextureMemAllocation(texture) / (1024 * 1024);
	}
	private static long GetTextureMemAllocation(CTexture texture) {
		return texture != null ? (texture.szTextureSize.Width * texture.szTextureSize.Height * 4) : 0;
	}
	private static Vector4 ColorToVector4(Color color) {
		return new Vector4((float)color.R / 255, (float)color.G / 255, (float)color.B / 255, (float)color.A / 255);
	}

	private static long GetTotalMemoryUsageFromCTextureList(IEnumerable<CTexture> textureList) {
		return textureList.Where(tex => tex != null).Sum(GetTextureMemAllocation);
	}
	private static long GetTotalMemoryUsageFromCTextureList(ScriptBG script) {
		return script != null ? GetTotalMemoryUsageFromCTextureList(script.Textures.Values) : 0;
	}
	#endregion

}
