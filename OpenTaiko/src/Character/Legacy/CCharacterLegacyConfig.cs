using FDK;

namespace OpenTaiko;

class CCharacterLegacyConfig {
	public int[] Resolution = new int[2] { 1280, 720 };
	public int[] Heya_Render_Offset = new int[2] { 0, 0 };
	public int[] Menu_Offset = new int[2] { 0, 0 };
	public int[] Result_Offset = new int[2] { 0, 0 };
	public int[] Game_Offset = new int[2] { 0, 0 };
	public int[] Game_Balloon_Offset = new int[2] { 0, 0 };
	public int[] Game_Kusudama_Offset = new int[2] { 0, 0 };
	public bool Result_UseResult1P = false;

	public int[]? Game_Motion_Normal = null;
	public int[]? Game_Motion_Clear = null;
	public int[]? Game_Motion_Clear_Max = null;
	public int[]? Game_Motion_GoGo = null;
	public int[]? Game_Motion_GoGo_Max = null;
	public int[]? Game_Motion_Miss = null;
	public int[]? Game_Motion_MissDown = null;
	public int[]? Game_Motion_10combo = null;
	public int[]? Game_Motion_10combo_Max = null;
	public int[]? Game_Motion_Cleared = null;
	public int[]? Game_Motion_Failed = null;
	public int[]? Game_Motion_Clearout = null;
	public int[]? Game_Motion_Clearin = null;
	public int[]? Game_Motion_Soulout = null;
	public int[]? Game_Motion_Soulin = null;
	public int[]? Game_Motion_MissIn = null;
	public int[]? Game_Motion_MissDownIn = null;
	public int[]? Game_Motion_Return = null;
	public int[]? Game_Motion_GoGoStart = null;
	public int[]? Game_Motion_GoGoStart_Clear = null;
	public int[]? Game_Motion_GoGoStart_Max = null;
	public int[]? Game_Motion_Balloon_Breaking = null;
	public int[]? Game_Motion_Balloon_Broke = null;
	public int[]? Game_Motion_Balloon_Miss = null;
	public int[]? Game_Motion_Kusudama_Breaking = null;
	public int[]? Game_Motion_Kusudama_Idle = null;
	public int[]? Game_Motion_Kusudama_Broke = null;
	public int[]? Game_Motion_Kusudama_Miss = null;
	public int[]? Game_Motion_Menu_Loop = null;
	public int[]? Game_Motion_Menu_Wait = null;
	public int[]? Game_Motion_Menu_Start = null;
	public int[]? Game_Motion_Menu_Select = null;
	public int[]? Game_Motion_Title_Normal = null;
	public int[]? Game_Motion_Title_Entry = null;
	public int[]? Game_Motion_Result_Normal = null;
	public int[]? Game_Motion_Result_Clear = null;
	public int[]? Game_Motion_Result_Failed_In = null;
	public int[]? Game_Motion_Result_Failed = null;
	public double Game_Beat_Normal = 1.0;
	public double Game_Beat_Clear = 1.0;
	public double Game_Beat_Clear_Max = 1.0;
	public double Game_Beat_GoGo = 1.0;
	public double Game_Beat_GoGo_Max = 1.0;
	public double Game_Beat_Miss = 1.0;
	public double Game_Beat_MissDown = 1.0;
	public double Game_Beat_10combo = 1.5;
	public double Game_Beat_10combo_Max = 1.5;
	public double Game_Beat_Cleared = 1.5;
	public double Game_Beat_Failed = 1.5;
	public double Game_Beat_Clearout = 1.5;
	public double Game_Beat_Clearin = 1.5;
	public double Game_Beat_Soulout = 1.5;
	public double Game_Beat_Soulin = 1.5;
	public double Game_Beat_MissIn = 1.0;
	public double Game_Beat_MissDownIn = 1.0;
	public double Game_Beat_Return = 1.5;
	public double Game_Beat_GoGoStart = 1.5;
	public double Game_Beat_GoGoStart_Clear = 1.5;
	public double Game_Beat_GoGoStart_Max = 1.5;
	public double Game_Beat_Balloon_Breaking = 0.25;
	public double Game_Beat_Balloon_Broke = 1.0;
	public double Game_Beat_Balloon_Miss = 1.0;
	public double Game_Beat_Kusudama_Breaking = 0.25;
	public double Game_Beat_Kusudama_Idle = 1.0;
	public double Game_Beat_Kusudama_Broke = 1.0;
	public double Game_Beat_Kusudama_Miss = 1.0;
	public double Game_Beat_Menu_Loop = 2.0;
	public double Game_Beat_Menu_Wait = 1.0;
	public double Game_Beat_Menu_Start = 1.0;
	public double Game_Beat_Menu_Select = 1.0;
	public double Game_Beat_Title_Normal = 1.0;
	public double Game_Beat_Title_Entry = 2.0;
	public double Game_Beat_Result_Normal = 1.0;
	public double Game_Beat_Result_Clear = 1.5;
	public double Game_Beat_Result_Failed_In = 1.0;
	public double Game_Beat_Result_Failed = 1.0;
	/*
	public int Entry_AnimationDuration = 1000;
	public int Normal_AnimationDuration = 1000;
	public int Menu_Loop_AnimationDuration = 1000;
	public int Menu_Select_AnimationDuration = 1000;
	public int Menu_Start_AnimationDuration = 1000;
	public int Menu_Wait_AnimationDuration = 1000;
	*/

	public CCharacterLegacyConfig(string str) {
		string[] delimiter = { "\n", "\r" };
		string[] strSingleLine = str.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

		foreach (string line in strSingleLine) {
			string[] splitedLine = line.Split('=');
			if (splitedLine.Length < 2) continue;

			string key = splitedLine[0];
			string value = splitedLine[1];
			switch (key) {
				case "Chara_Resolution": {
						string[] values = value.Split(',');
						Resolution[0] = int.Parse(values[0]);
						Resolution[1] = int.Parse(values[1]);
						break;
					}
				case "Heya_Chara_Render_Offset": {
						string[] values = value.Split(',');
						Heya_Render_Offset[0] = int.Parse(values[0]);
						Heya_Render_Offset[1] = int.Parse(values[1]);
						break;
					}
				case "Menu_Offset": {
						string[] values = value.Split(',');
						Menu_Offset[0] = int.Parse(values[0]);
						Menu_Offset[1] = int.Parse(values[1]);
						break;
					}
				case "Result_Offset": {
						string[] values = value.Split(',');
						Menu_Offset[0] = int.Parse(values[0]);
						Result_Offset[1] = int.Parse(values[1]);
						break;
					}
				case "Game_Chara_X": {
						string[] values = value.Split(',');
						Game_Offset[0] = int.Parse(values[0]);
						break;
					}
				case "Game_Chara_Y": {
						string[] values = value.Split(',');
						Game_Offset[1] = int.Parse(values[0]);
						break;
					}
				case "Game_Chara_Balloon_X": {
						string[] values = value.Split(',');
						Game_Balloon_Offset[0] = int.Parse(values[0]);
						break;
					}
				case "Game_Chara_Balloon_Y": {
						string[] values = value.Split(',');
						Game_Balloon_Offset[1] = int.Parse(values[0]);
						break;
					}
				case "Game_Chara_Kusudama_X": {
						string[] values = value.Split(',');
						Game_Kusudama_Offset[0] = int.Parse(values[0]);
						break;
					}
				case "Game_Chara_Kusudama_Y": {
						string[] values = value.Split(',');
						Game_Kusudama_Offset[1] = int.Parse(values[0]);
						break;
					}
				case "Result_UseResult1P": {
						Result_UseResult1P = FDK.CConversion.bONorOFF(value[0]);
						break;
					}
				case "Game_Chara_Motion_Normal": {
						Game_Motion_Normal = CConversion.StringToIntArray(value);
						break;
					}
				case "Game_Chara_Motion_10Combo": {
						Game_Motion_Normal = CConversion.StringToIntArray(value);
						break;
					}
				case "Game_Chara_Beat_Normal": {
						Game_Beat_Normal = double.Parse(value);
						break;
					}
				case "Chara_Entry_AnimationDuration": {
						Game_Beat_Title_Entry = int.Parse(value) / 1000.0;
						break;
					}
				case "Chara_Normal_AnimationDuration": {
						Game_Beat_Title_Normal = int.Parse(value) / 1000.0;
						break;
					}
				case "Chara_Menu_Loop_AnimationDuration": {
						Game_Beat_Menu_Loop = int.Parse(value) / 1000.0;
						break;
					}
				case "Chara_Menu_Select_AnimationDuration": {
						Game_Beat_Menu_Select = int.Parse(value) / 1000.0;
						break;
					}
				case "Chara_Menu_Start_AnimationDuration": {
						Game_Beat_Menu_Start = int.Parse(value) / 1000.0;
						break;
					}
				case "Chara_Menu_Wait_AnimationDuration": {
						Game_Beat_Menu_Wait = int.Parse(value) / 1000.0;
						break;
					}
				case "Chara_Result_Normal_AnimationDuration": {
						Game_Beat_Result_Normal = int.Parse(value) / 1000.0;
						break;
					}
				case "Chara_Result_Clear_AnimationDuration": {
						Game_Beat_Result_Clear = int.Parse(value) / 1000.0;
						break;
					}
				case "Chara_Result_Failed_In_AnimationDuration": {
						Game_Beat_Result_Failed_In = int.Parse(value) / 1000.0;
						break;
					}
				case "Chara_Result_Failed_AnimationDuration": {
						Game_Beat_Result_Failed = int.Parse(value) / 1000.0;
						break;
					}
			}

			/*
			if (line.StartsWith("Chara_Resolution=")) // required for Heya resolution compatibility
			{
				string[] values = line.Substring(17).Trim().Split(',');
				Resolution[0] = int.Parse(values[0]);
				Resolution[1] = int.Parse(values[1]);
			} else if (line.StartsWith("Heya_Chara_Render_Offset=")) {
				string[] values = line.Substring(25).Trim().Split(',');
				Heya_Render_Offset[0] = int.Parse(values[0]);
				Heya_Render_Offset[1] = int.Parse(values[1]);
			} else if (line.StartsWith("Menu_Offset=")) {
				string[] values = line.Substring(12).Trim().Split(',');
				Menu_Offset[0] = int.Parse(values[0]);
				Menu_Offset[1] = int.Parse(values[1]);
			} else if (line.StartsWith("Result_Offset=")) {
				string[] values = line.Substring(14).Trim().Split(',');
				Result_Offset[0] = int.Parse(values[0]);
				Result_Offset[1] = int.Parse(values[1]);
			} else if (line.StartsWith("Game_Chara_X=")) {
				string[] values = line.Substring(13).Trim().Split(',');
				Game_Offset[0] = int.Parse(values[0]);
			} else if (line.StartsWith("Game_Chara_Y=")) {
				string[] values = line.Substring(13).Trim().Split(',');
				Game_Offset[1] = int.Parse(values[0]);
			} else if (line.StartsWith("Game_Chara_Balloon_X=")) {
				string[] values = line.Substring(21).Trim().Split(',');
				Game_Balloon_Offset[0] = int.Parse(values[0]);
			} else if (line.StartsWith("Game_Chara_Balloon_Y=")) {
				string[] values = line.Substring(21).Trim().Split(',');
				Game_Balloon_Offset[1] = int.Parse(values[0]);
			} else if (line.StartsWith("Game_Chara_Kusudama_X=")) {
				string[] values = line.Substring(22).Trim().Split(',');
				Game_Kusudama_Offset[0] = int.Parse(values[0]);
			} else if (line.StartsWith("Game_Chara_Kusudama_Y=")) {
				string[] values = line.Substring(22).Trim().Split(',');
				Game_Kusudama_Offset[1] = int.Parse(values[0]);
			} else if (line.StartsWith("Result_UseResult1P=")) {
				Result_UseResult1P = FDK.CConversion.bONorOFF(line.Substring(19).Trim()[0]);
			}
			*/
		}
	}
}
