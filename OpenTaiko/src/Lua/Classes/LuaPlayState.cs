using System.Text.Json.Nodes;

namespace OpenTaiko {
	public class LuaPlayStateFunc {

		public int LastRegisteredFloor => CFloorManagement.LastRegisteredFloor;
		public int MaxNumberOfLives => CFloorManagement.MaxNumberOfLives;
		public int CurrentNumberOfLives => CFloorManagement.CurrentNumberOfLives;
		public double InvincibilityDurationSpeedDependent => CFloorManagement.InvincibilityDurationSpeedDependent;
		public int InvincibilityDuration => CFloorManagement.InvincibilityDuration;

		public LuaPlayStateFunc() {
		}
	}
}
