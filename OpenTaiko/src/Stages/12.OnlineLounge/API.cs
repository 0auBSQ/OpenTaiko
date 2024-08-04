using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OpenTaiko {
	internal class API {
		public class APICharterInfo {
			public string charter_name;
		}

		public class APIGenreInfo {
			public string genre;
		}

		public class APISongData {
			public int Id;
			public string SongTitle;
			public string SongSubtitle;
			public int D0; // Easy
			public int D1; // Normal
			public int D2; // Hard
			public int D3; // Extreme
			public int D4; // Extra
			public int D5; // Tower
			public int D6; // Dan
			public int Life; // Tower only
			public string Md5;

			// Additional
			public APICharterInfo charter;
			public APIGenreInfo Genre;

			[JsonIgnore]
			public bool DownloadNow;
		}

		#region [ContractResolver override for properties]



		public class SongContractResolver : DefaultContractResolver {
			private Dictionary<string, string> PropertyMappings { get; set; }

			private string GetAssignedLanguageValue(Dictionary<string, string> ens) {
				if (ens.ContainsKey(OpenTaiko.ConfigIni.sLang))
					return ens[OpenTaiko.ConfigIni.sLang];
				return ens["default"];
			}

			public SongContractResolver(DBCDN.CDNData cdnData) {
				this.PropertyMappings = new Dictionary<string, string>
				{
					{"SongTitle", GetAssignedLanguageValue(cdnData.Hooks.title)},
					{"SongSubtitle", GetAssignedLanguageValue(cdnData.Hooks.subtitle)},
					{"Md5", GetAssignedLanguageValue(cdnData.Hooks.md5)},
					{"Id", cdnData.Hooks.id},
					{"D0", cdnData.Hooks.difficulties[0]},
					{"D1", cdnData.Hooks.difficulties[1]},
					{"D2", cdnData.Hooks.difficulties[2]},
					{"D3", cdnData.Hooks.difficulties[3]},
					{"D4", cdnData.Hooks.difficulties[4]},
					{"D5", cdnData.Hooks.difficulties[5]},
					{"D6", cdnData.Hooks.difficulties[6]},
					{"Tower", cdnData.Hooks.life},
					{"Genre", cdnData.Hooks.genre},
					{"genre", GetAssignedLanguageValue(cdnData.Hooks.genreSub)},
				};
			}

			protected override string ResolvePropertyName(string propertyName) {
				string resolvedName = null;
				var resolved = this.PropertyMappings.TryGetValue(propertyName, out resolvedName);
				return (resolved) ? resolvedName : base.ResolvePropertyName(propertyName);
			}
		}

		#endregion

		public API(DBCDN.CDNData selectedCDN) {
			cdnData = selectedCDN;
			FetchedSongsList = new APISongData[0];
		}

		public APISongData[] FetchedSongsList;

		public void tLoadSongsFromInternalCDN() {
			string url = cdnData.BaseUrl + cdnData.SongList;


			var _fetched = GetCallAPI(url);

			_fetched.Wait();

			if (_fetched.Result != null)
				FetchedSongsList = _fetched.Result;


		}

		#region [private]

		private DBCDN.CDNData cdnData;

		private async Task<APISongData[]> GetCallAPI(string url) {
			try {
				using (HttpClient client = new HttpClient()) {
					var response = await client.GetAsync(url).ConfigureAwait(false);

					if (response != null) {
						var jsonString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

						var settings = new JsonSerializerSettings();
						settings.ContractResolver = new SongContractResolver(cdnData);
						settings.NullValueHandling = NullValueHandling.Ignore;
						return JsonConvert.DeserializeObject<APISongData[]>(jsonString, settings);
					}
				}
			} catch (Exception e) {
				//throw e;
			}
			return null;
		}


		#endregion

	}
}
