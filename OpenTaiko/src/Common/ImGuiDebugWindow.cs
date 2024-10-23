using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using FDK;
using ImGuiNET;
using SampleFramework;

namespace OpenTaiko;

/*
 FOR DEBUGGING! This class is intended for developers only!
*/
public static class ImGuiDebugWindow {
	private static bool showImGuiDemoWindow = false;
	private static Assembly assemblyinfo = Assembly.GetExecutingAssembly();

	private static long memoryReadTimer = 0;
	private static long pagedmemory = 0;
	private static int textureMemoryUsage = 0;
	private static int currentStageMemoryUsage = 0;

	private static int sortType = -1;
	private static readonly string[] sortNames = ["Memory Usage (Highest->Lowest)", "Memory Usage (Lowest->Highest)", "Pointer ID"];
	private static string reloadTexPath = "";
	public static void Draw() {
		if (SampleFramework.Game.ImGuiController == null) return;

		#region Fetch allocated memory
		if (SoundManager.PlayTimer.SystemTimeMs - memoryReadTimer > 5000) {
			memoryReadTimer = SoundManager.PlayTimer.SystemTimeMs;
			Task.Factory.StartNew(() => {
				using (Process process = Process.GetCurrentProcess()) {
					pagedmemory = process.PagedMemorySize64;
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

			ImGui.Separator();
			ImGui.Text($"Game Version: {OpenTaiko.VERSION}");
			ImGui.Text($"Allocated Memory: {pagedmemory} bytes ({String.Format("{0:0.###}", (float)pagedmemory / (1024 * 1024 * 1024))}GB)");
			ImGui.Text($"FPS: {(OpenTaiko.FPS != null ? OpenTaiko.FPS.NowFPS : "???")}");
			ImGui.Text("Current Stage: " + OpenTaiko.r現在のステージ.eStageID.ToString() + " (StageID " + ((int)OpenTaiko.r現在のステージ.eStageID).ToString() + ")");
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
				if (ImGui.TreeNodeEx(device.CurrentType.ToString() + " (ID " + device.ID + " / Name: " + device.Name + ")")) {
					switch (device.CurrentType) {
						case InputDeviceType.Keyboard:
							var keyboard = (CInputKeyboard)device;
							for (int i = 0; i < keyboard.KeyStates.Length; i++) {
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
								if (gamepad.KeyPressed(i)) { ImGui.Text((Silk.NET.Input.ButtonName)i + " Pressed!"); }
								if (gamepad.KeyPressing(i)) { ImGui.Text((Silk.NET.Input.ButtonName)i + " Pressing!"); }
								if (gamepad.KeyReleased(i)) { ImGui.Text((Silk.NET.Input.ButtonName)i + " Released!"); }
							}
							break;
						case InputDeviceType.Joystick:
							var joystick = (CInputJoystick)device;
							for (int i = 0; i < joystick.ButtonStates.Length; i++) {
								if (joystick.KeyPressed(i)) { ImGui.Text((Silk.NET.Input.ButtonName)i + " Pressed!"); }
								if (joystick.KeyPressing(i)) { ImGui.Text((Silk.NET.Input.ButtonName)i + " Pressing!"); }
								if (joystick.KeyReleased(i)) { ImGui.Text((Silk.NET.Input.ButtonName)i + " Released!"); }
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
						case InputDeviceType.Unknown:
							ImGui.TextDisabled("Unknown input device type.");
							ImGui.TextDisabled("GUID: " + device.GUID);
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

			ImGui.BeginDisabled(OpenTaiko.r現在のステージ.eStageID == CStage.EStage.Game);
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
					ImGui.InputText("Name", ref OpenTaiko.SaveFileInstances[save].data.Name, 64);

					if (ImGui.Button("Update")) {
						OpenTaiko.SaveFileInstances[save].tApplyHeyaChanges();
						OpenTaiko.NamePlate.tNamePlateRefreshTitles(save);
					}

					string preview = OpenTaiko.SaveFileInstances[save].data.TitleId == -1 ? "初心者" : OpenTaiko.Databases.DBNameplateUnlockables.data[OpenTaiko.SaveFileInstances[save].data.TitleId].nameplateInfo.cld.GetString("");

					if (ImGui.BeginCombo("Nameplate", preview)) {
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

					if (ImGui.BeginCombo("Dan Title", OpenTaiko.SaveFileInstances[save].data.Dan)) {
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

			switch (OpenTaiko.r現在のステージ.eStageID) {
				case CStage.EStage.SongSelect:
					System.Numerics.Vector4 normal = new System.Numerics.Vector4(1, 1, 1, 1);
					System.Numerics.Vector4 diff = new System.Numerics.Vector4(0.5f, 1, 0.5f, 1);

					ImGui.TextColored(OpenTaiko.ConfigIni.SongPlaybackSpeed == 1 ? normal : diff,
						String.Format("Song Playback Speed: {0:0.00}", OpenTaiko.ConfigIni.SongPlaybackSpeed));

					ImGui.TextColored(OpenTaiko.ConfigIni.bTokkunMode ? diff : normal,
						"Training Mode: " + OpenTaiko.ConfigIni.bTokkunMode);

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

							ImGui.Text("Hitsound: " + OpenTaiko.Skin.hsHitSoundsInformations.names[OpenTaiko.ConfigIni.nHitSounds[i]]);

							ImGui.TextColored(OpenTaiko.ConfigIni.nFunMods[i] == EFunMods.None ? normal : diff,
								"Fun Mods: " + OpenTaiko.ConfigIni.nFunMods[i].ToString());

							ImGui.TreePop();
						}
					}
					break;
				case CStage.EStage.Game:
					for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
						if (ImGui.TreeNodeEx($"Player {i + 1}###GAME_CHART_{i}", ImGuiTreeNodeFlags.Framed)) {

							Difficulty game_difficulty = OpenTaiko.DifficultyNumberToEnum(OpenTaiko.stageSongSelect.nChoosenSongDifficulty[i]);
							var dtx = OpenTaiko.GetDTX(i);

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

							ImGui.NewLine();

							ImGui.Text("ID: " + dtx.uniqueID.data.id);
							ImGui.Text("Title: " + dtx.TITLE.GetString(""));
							ImGui.Text("Subtitle: " + dtx.SUBTITLE.GetString(""));
							ImGui.Text("Charter: " + dtx.MAKER);

							ImGui.Text("BPM: " + dtx.BASEBPM + (dtx.listBPM.Count > 1 ? (" (Min: " + dtx.MinBPM + " / Max: " + dtx.MaxBPM + ")") : ""));
							if (dtx.listBPM.Count > 1) {
								if (ImGui.TreeNodeEx($"BPM List ({dtx.listBPM.Count})###GAME_BPM_LIST_{i}")) {
									foreach (CDTX.CBPM bpm in dtx.listBPM.Values) {
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
			if (OpenTaiko.r現在のステージ.eStageID != CStage.EStage.StartUp)
				CTextureListPopup(OpenTaiko.Tx.listTexture, "Show listTexture", "TEXTURE_ALL");
			else
				ImGui.TextDisabled("To prevent crash during enumeration,\nyou can not view the texture list during StartUp stage.");

			currentStageMemoryUsage = 0;

			#region Script.lua Memory Usage
			int index = 0;
			foreach (CLuaScript luascript in CLuaScript.listScripts)
				currentStageMemoryUsage += CTextureListPopup(luascript.listDisposables.OfType<CTexture>(),
					$"Module #{index}", $"MODULE{index++}_TEXTURES");

			switch (OpenTaiko.r現在のステージ.eStageID) {
				#region Game
				case CStage.EStage.Game:

					currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actBackground.UpScript,
						"Up Background", "TEXTURE_LUA_UPBG");
					currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actBackground.DownScript,
						"Down Background", "TEXTURE_LUA_DOWNBG");
					currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actMob.MobScript,
						"Mob", "TEXTURE_LUA_MOB");
					currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actBalloon.KusudamaScript,
						"Kusudama", "TEXTURE_LUA_KUSUDAMA");

					#region Endings
					switch ((Difficulty)OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0]) {
						case Difficulty.Tower:
							currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actEnd.Tower_DropoutScript,
								"Tower Dropout", "TEXTURE_LUA_TOWERDROPOUT");
							currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actEnd.Tower_TopReached_PassScript,
								"Tower Cleared", "TEXTURE_LUA_TOWERCLEAR");
							currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actEnd.Tower_TopReached_FullComboScript,
								"Tower Full Combo", "TEXTURE_LUA_TOWERFC");
							currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actEnd.Tower_TopReached_PerfectScript,
								"Tower Perfect Combo", "TEXTURE_LUA_TOWERPFC");
							break;
						case Difficulty.Dan:
							currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actEnd.Dan_FailScript,
								"Dan Clear Failed", "TEXTURE_LUA_DANFAILED");
							currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actEnd.Dan_Red_PassScript,
								"Dan Red Clear", "TEXTURE_LUA_DANCLEAR");
							currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actEnd.Dan_Red_FullComboScript,
								"Dan Red Full Combo", "TEXTURE_LUA_DANFC");
							currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actEnd.Dan_Red_PerfectScript,
								"Dan Red Perfect", "TEXTURE_LUA_DANPFC");
							currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actEnd.Dan_Red_PassScript,
								"Dan Gold Clear", "TEXTURE_LUA_DANGOLDCLEAR");
							currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actEnd.Dan_Red_FullComboScript,
								"Dan Gold Full Combo", "TEXTURE_LUA_DANGOLDFC");
							currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actEnd.Dan_Red_PerfectScript,
								"Dan Gold Perfect", "TEXTURE_LUA_DANGOLDPFC");
							break;
						default:
							if (OpenTaiko.ConfigIni.bAIBattleMode) {
								currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actEnd.AILoseScript,
									"AI Clear Failed", "TEXTURE_LUA_AIFAILED");
								currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actEnd.AIWinScript,
									"AI Cleared", "TEXTURE_LUA_AICLEAR");
								currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actEnd.AIWin_FullComboScript,
									"AI Full Combo", "TEXTURE_LUA_AIFC");
								currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actEnd.AIWin_PerfectScript,
									"AI Perfect Combo", "TEXTURE_LUA_AIPFC");
							} else {
								currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actEnd.FailedScript,
									"Clear Failed", "TEXTURE_LUA_GAMEFAILED");
								currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actEnd.ClearScript,
									"Cleared", "TEXTURE_LUA_GAMECLEAR");
								currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actEnd.FullComboScript,
									"Full Combo", "TEXTURE_LUA_GAMEFC");
								currentStageMemoryUsage += CTextureListPopup(OpenTaiko.stage演奏ドラム画面.actEnd.PerfectComboScript,
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
	private static int CTextureListPopup(IEnumerable<CTexture> textureList, string label, string id) {
		if (textureList == null) return 0;
		int memoryCount = GetTotalMemoryUsageFromCTextureList(textureList);

		if (ImGui.TreeNodeEx($"{label} Textures: ({textureList.Count()} / {String.Format("{0:0.###}", GetMemAllocationInMegabytes(memoryCount))}MB)###{id}")) {
			int index = 0;
			foreach (CTexture tex in textureList) {
				CTexturePopup(tex, $"Texture #{index} (Pointer: {(tex != null ? tex.Pointer : "null")})###{id}_{index++}");
			}
			ImGui.TreePop();
		}
		return memoryCount;
	}
	private static int CTextureListPopup(ScriptBG script, string label, string id) {
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
	private static float GetMemAllocationInMegabytes(int bytes) { return (float)bytes / (1024 * 1024); }
	private static float GetTextureMemAllocationInMegabytes(CTexture texture) {
		return (float)GetTextureMemAllocation(texture) / (1024 * 1024);
	}
	private static int GetTextureMemAllocation(CTexture texture) {
		return texture != null ? (texture.szTextureSize.Width * texture.szTextureSize.Height * 4) : 0;
	}
	private static Vector4 ColorToVector4(Color color) {
		return new Vector4((float)color.R / 255, (float)color.G / 255, (float)color.B / 255, (float)color.A / 255);
	}

	private static int GetTotalMemoryUsageFromCTextureList(IEnumerable<CTexture> textureList) {
		return textureList.Where(tex => tex != null).Sum(GetTextureMemAllocation);
	}
	private static int GetTotalMemoryUsageFromCTextureList(ScriptBG script) {
		return script != null ? GetTotalMemoryUsageFromCTextureList(script.Textures.Values) : 0;
	}
	#endregion

}
