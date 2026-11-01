using System;
using System.Collections.Generic;
using System.Globalization;

namespace OpenTaiko.AviUtl2 {
	// Effects that belong to a camera control's objects.
	internal static class Aup2FiltersCamera {
		public static void Register(Dictionary<string, Aup2FilterFn> map) {
			map["ライトブルーム(カメラ制御)"] = Bloom;
		}

		// the brightest parts above this share of full brightness glow
		private const double BloomThreshold = 60;

		// The bright parts of the picture glow: of the frame drawn so far when it is an object of its own,
		// of the object when it is one of its effects.
		private static void Bloom(Aup2Context c) {
			double strength = c.V("強さ", 100), reach = c.V("拡散", 30);
			if (!double.IsFinite(strength) || !double.IsFinite(reach) || strength <= 0 || reach < 0) return;
			var inv = CultureInfo.InvariantCulture;
			c.RunEffect("発光", new KeyValuePair<string, string>[] {
				new("強さ", strength.ToString("0.###", inv)),
				new("拡散", reach.ToString("0.###", inv)),
				new("しきい値", BloomThreshold.ToString("0.###", inv)),
			});
		}
	}
}
