namespace TJAPlayer3 {
	internal static class CStrジャンルtoNum {
		internal static int ForAC8_14(string strジャンル) {
			switch (strジャンル) {
				case "アニメ":
					return 0;
				case "ポップス":
					return 1;
				case "ゲームミュージック":
					return 2;
				case "ナムコオリジナル":
					return 3;
				case "クラシック":
					return 4;
				case "どうよう":
					return 5;
				case "バラエティ":
					return 6;
				case "ボーカロイド":
				case "VOCALOID":
					return 7;
				default:
					return 8;
			}
		}

		internal static int ForAC15(string strジャンル) {
			switch (strジャンル) {
				case "ポップス":
				case "J-POP":
				case "POPS":
				case "JPOP":
					return 0;
				case "アニメ":
					return 1;
				case "ボーカロイド":
				case "VOCALOID":
					return 2;
				case "キッズ":
				case "どうよう":
					return 3;
				case "バラエティ":
					return 4;
				case "クラシック":
					return 5;
				case "ゲームバラエティ":
				case "ゲームミュージック":
					return 6;
				case "ナムコオリジナル":
					return 7;
				default:
					return 8;
			}
		}
	}
}
