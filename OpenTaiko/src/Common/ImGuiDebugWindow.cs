using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;
using ImGuiNET;
using Silk.NET.SDL;

namespace OpenTaiko {
	public static class ImGuiDebugWindow {
		public static void Draw() {
			if (SampleFramework.Game.ImGuiController == null) return;

			if (ImGui.Begin("Debug Window###DEBUG")) {

				#region Debug Info
				ImGui.Text($"Game Version: {OpenTaiko.VERSION}");
				ImGui.Text($"FPS: {(OpenTaiko.FPS != null ? OpenTaiko.FPS.NowFPS : "???")}");
				ImGui.Text("Current Stage: " + OpenTaiko.r現在のステージ.eStageID.ToString() + " (StageID " + ((int)OpenTaiko.r現在のステージ.eStageID).ToString() + ")");
				#endregion

				ImGui.BeginTabBar("Tabs");

				#region Profile
				if (ImGui.BeginTabItem("Profile")) {
					ImGui.Text("Player Count: " + OpenTaiko.ConfigIni.nPlayerCount);
					for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
						if (ImGui.TreeNodeEx($"Player {i + 1}###TREE_PROFILE_{i}", ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.DefaultOpen)) {
							int save = i == 0 ? OpenTaiko.SaveFile : i;

							if (i == 1 && OpenTaiko.ConfigIni.bAIBattleMode)
								ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.8f, 1.0f, 1.0f), "2P is occupied. AI Battle is active.");

							ImGui.Text($"ID: {OpenTaiko.SaveFileInstances[save].data.SaveId}");
							ImGui.InputText("Name", ref OpenTaiko.SaveFileInstances[save].data.Name, 64);

							string preview = OpenTaiko.SaveFileInstances[save].data.TitleId == 0 ? "初心者" : OpenTaiko.Databases.DBNameplateUnlockables.data[OpenTaiko.SaveFileInstances[save].data.TitleId].nameplateInfo.cld.GetString("");

							if (ImGui.BeginCombo("Nameplate", preview)) {
								foreach (long id in OpenTaiko.Databases.DBNameplateUnlockables.data.Keys) {
									if (ImGui.Selectable(OpenTaiko.Databases.DBNameplateUnlockables.data[id].nameplateInfo.cld.GetString(""))) {
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

							if (ImGui.Button("Update")) {
								OpenTaiko.SaveFileInstances[save].tApplyHeyaChanges();
								OpenTaiko.NamePlate.tNamePlateRefreshTitles(save);
							}

							ImGui.NewLine();

							ImGui.Text($"Total Plays: {OpenTaiko.SaveFileInstances[save].data.TotalPlaycount}");
							ImGui.Text($"Coins: {OpenTaiko.SaveFileInstances[save].data.Medals} (Lifetime: {OpenTaiko.SaveFileInstances[save].data.TotalEarnedMedals})");

							ImGui.TreePop();
						}
					}
					ImGui.EndTabItem();
				}
				#endregion

				#region Stage
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
							ImGui.Text("Title: " + OpenTaiko.DTX.TITLE.GetString("???"));
							ImGui.Text("Subtitle: " + OpenTaiko.DTX.SUBTITLE.GetString("???"));
							if (!string.IsNullOrEmpty(OpenTaiko.DTX.MAKER)) {
								ImGui.Text("Charter: " + OpenTaiko.DTX.MAKER);
							}
							else {
								ImGui.TextDisabled("Charter: (None)");
							}
							ImGui.NewLine();

							ImGui.Text("BPM: " + OpenTaiko.DTX.BASEBPM + (OpenTaiko.DTX.listBPM.Count > 1 ? (" (Min: " + OpenTaiko.DTX.MinBPM + " / Max: " + OpenTaiko.DTX.MaxBPM + ")") : ""));

							ImGui.NewLine();

							ImGui.Text("Auto: " + OpenTaiko.ConfigIni.bAutoPlay[0]);
							break;
					}

					ImGui.EndTabItem();
				}
				#endregion

				ImGui.EndTabBar();

				ImGui.End();
			}
		}
	}
}
