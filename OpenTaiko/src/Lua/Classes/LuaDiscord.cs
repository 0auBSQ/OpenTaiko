using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTaiko {
	public class LuaDiscordFunc {
		private DateTime _lastUsage;
		public void UpdateState(string state) {
			OpenTaiko.DiscordClient?.UpdateState(state);
		}
		public void UpdateDetails(string details) {
			OpenTaiko.DiscordClient?.UpdateDetails(details);
		}
		public void UpdateTimestamp(int duration = 0) {
			if (duration > 0) {
				var presence = OpenTaiko.DiscordClient?.CurrentPresence ?? new();
				presence.Timestamps = new(DateTime.UtcNow, new DateTimeOffset(DateTime.UtcNow, new(duration * 10000)).UtcDateTime);
				OpenTaiko.DiscordClient?.SetPresence(presence);
			}
		}
		public void ClearTimestamp() {
			OpenTaiko.DiscordClient?.UpdateClearTime();
		}
	}
}
