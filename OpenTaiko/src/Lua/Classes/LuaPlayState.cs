using System.Text.Json.Nodes;

namespace OpenTaiko {
	public class LuaPlayStateFunc {

		public static int LastRegisteredFloor => CFloorManagement.LastRegisteredFloor;
		public static int MaxNumberOfLives => CFloorManagement.MaxNumberOfLives;
		public static int CurrentNumberOfLives => CFloorManagement.CurrentNumberOfLives;
		public static double InvincibilityDurationSpeedDependent => CFloorManagement.InvincibilityDurationSpeedDependent;
		public static int InvincibilityDuration => CFloorManagement.InvincibilityDuration;

		public LuaPlayStateFunc() {
		}
	}
}
