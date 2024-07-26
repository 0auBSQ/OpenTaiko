using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TJAPlayer3 {
	/// <summary>
	/// 設定ファイル入出力クラス。
	/// </summary>
	public static class ConfigManager {
		private static readonly JsonSerializerSettings Settings =
			new JsonSerializerSettings() {
				ObjectCreationHandling = ObjectCreationHandling.Auto,
				DefaultValueHandling = DefaultValueHandling.Include,
				// ContractResolver = new CamelCasePropertyNamesContractResolver(),
				NullValueHandling = NullValueHandling.Ignore,
				MissingMemberHandling = MissingMemberHandling.Ignore,
				Converters = new StringEnumConverter[] { new StringEnumConverter() }
			};

		/// <summary>
		/// 設定ファイルの読み込みを行います。ファイルが存在しなかった場合、そのクラスの新規インスタンスを返します。
		/// </summary>
		/// <typeparam name="T">シリアライズしたクラス。</typeparam>
		/// <param name="filePath">ファイル名。</param>
		/// <returns>デシリアライズ結果。</returns>
		public static T GetConfig<T>(string filePath) where T : new() {
			var json = "";
			if (!System.IO.File.Exists(filePath)) {
				// ファイルが存在しないので
				SaveConfig(new T(), filePath);
			}
			using (var stream = new System.IO.StreamReader(filePath, Encoding.UTF8)) {
				json = stream.ReadToEnd();
			}
			return JsonConvert.DeserializeObject<T>(json, Settings);
		}

		/// <summary>
		/// 設定ファイルの書き込みを行います。
		/// </summary>
		/// <param name="obj">シリアライズするインスタンス。</param>
		/// <param name="filePath">ファイル名。</param>
		public static void SaveConfig(object obj, string filePath) {
			(new FileInfo(filePath)).Directory.Create();
			using (var stream = new System.IO.StreamWriter(filePath, false, Encoding.UTF8)) {
				stream.Write(JsonConvert.SerializeObject(obj, Formatting.None, Settings));
			}
		}
	}
}
