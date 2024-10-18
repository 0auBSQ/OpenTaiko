using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using FDK;
using ImGuiNET;

namespace OpenTaiko {
	/*
	 FOR DEBUGGING! This class is intended for developers only!
	*/
	public static class ImGuiDebugWindow {
		private static bool showImGuiDemoWindow = false;
		private static Assembly assemblyinfo = Assembly.GetExecutingAssembly();

		private static long memoryReadTimer = 0;
		private static long pagedmemory = 0;
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
			ImGui.SetNextWindowSize(new System.Numerics.Vector2(400,300), ImGuiCond.FirstUseEver);
			if (ImGui.Begin("Debug Window (Toggle Visbility with F11)###DEBUG")) {

				#region Debug Info
				ImGui.Checkbox("Show ImGui Demo Window", ref showImGuiDemoWindow);
				if (showImGuiDemoWindow) { ImGui.ShowDemoWindow(); }

				ImGui.Separator();
				ImGui.Text($"Game Version: {OpenTaiko.VERSION}");
				ImGui.Text($"Allocated Memory: {pagedmemory} bytes ({String.Format("{0:0.###}",(float)pagedmemory / (1024 * 1024 * 1024))}GB)");
				ImGui.Text($"FPS: {(OpenTaiko.FPS != null ? OpenTaiko.FPS.NowFPS : "???")}");
				ImGui.Text("Current Stage: " + OpenTaiko.r現在のステージ.eStageID.ToString() + " (StageID " + ((int)OpenTaiko.r現在のステージ.eStageID).ToString() + ")");
				#endregion

				ImGui.BeginTabBar("Tabs");

				#region Tabs
				System();
				Profile();
				Stage();
				#endregion

				ImGui.EndTabBar();

				ImGui.End();
			}
		}
		private static void System() {
			if (ImGui.BeginTabItem("System")) {
				ImGui.TextWrapped($"Path: {(Environment.ProcessPath != null ? Environment.ProcessPath : "???")}");
				ImGui.NewLine();
				ImGui.Text($"OS Version: {Environment.OSVersion} ({RuntimeInformation.RuntimeIdentifier})");
				ImGui.Text($"OS Architecture: {RuntimeInformation.OSArchitecture}");
				ImGui.Text($"Framework Version: {RuntimeInformation.FrameworkDescription}");
				ImGui.Text($"Is Privileged: {Environment.IsPrivilegedProcess}");

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

						string preview = OpenTaiko.SaveFileInstances[save].data.TitleId == 0 ? "初心者" : OpenTaiko.Databases.DBNameplateUnlockables.data[OpenTaiko.SaveFileInstances[save].data.TitleId].nameplateInfo.cld.GetString("");

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
						switch (OpenTaiko.DifficultyNumberToEnum(OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0])) {
							case Difficulty.Dan:
								ImGui.SeparatorText("Dan Dojo Mode");
								break;
							case Difficulty.Tower:
								ImGui.SeparatorText("Tower Mode");
								break;
							default:
								ImGui.SeparatorText(OpenTaiko.ConfigIni.nGameType[0] == EGameType.Konga ? "Konga Mode" : "Taiko Mode");
								break;

						}
						for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++)
							ImGui.Text($"Auto Play ({i + 1}P): " + OpenTaiko.ConfigIni.bAutoPlay[i]);

						ImGui.NewLine();

						ImGui.Text("Title: " + OpenTaiko.DTX.TITLE.GetString("???"));
						ImGui.Text("Subtitle: " + OpenTaiko.DTX.SUBTITLE.GetString("???"));
						if (!string.IsNullOrEmpty(OpenTaiko.DTX.MAKER)) {
							ImGui.Text("Charter: " + OpenTaiko.DTX.MAKER);
						} else {
							ImGui.TextDisabled("Charter: (None)");
						}
						for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
							var dtx = OpenTaiko.GetDTX(i);

							ImGui.Text("BPM: " + dtx.BASEBPM + (dtx.listBPM.Count > 1 ? (" (Min: " + dtx.MinBPM + " / Max: " + dtx.MaxBPM + ")") : ""));
							if (dtx.listBPM.Count > 1) {
								if (ImGui.TreeNodeEx($"BPM List ({dtx.listBPM.Count})###GAME_BPM_LIST_{i}")) {
									foreach (CDTX.CBPM bpm in dtx.listBPM.Values) {
										ImGui.Text($"(Time: {String.Format("{0:0.#}s", (bpm.bpm_change_time / 1000))}) {bpm.dbBPM値}");
									}
									ImGui.TreePop();
								}
							}
						}
						break;
				}

				ImGui.EndTabItem();
			}
		}
	}
}
