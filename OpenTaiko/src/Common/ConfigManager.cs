using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OpenTaiko;

/// <summary>
/// Class for reading and writing configuration files.
/// </summary>
public static class ConfigManager {
	private static readonly JsonSerializerSettings Settings =
		new JsonSerializerSettings() {
			ObjectCreationHandling = ObjectCreationHandling.Auto,
			DefaultValueHandling = DefaultValueHandling.Include,
			NullValueHandling = NullValueHandling.Ignore,
			MissingMemberHandling = MissingMemberHandling.Ignore,
			Converters = new StringEnumConverter[] { new StringEnumConverter() }
		};

	/// <summary>
	/// Reads the configuration file. If the file does not exist, it will be created.
	/// </summary>
	/// <typeparam name="T">Type of the object to deserialize.</typeparam>
	/// <param name="filePath">File name.</param>
	/// <returns>Deserialized object.</returns>
	public static T GetConfig<T>(string filePath) where T : new() {
		var json = "";
		if (!System.IO.File.Exists(filePath)) {
			SaveConfig(new T(), filePath);
		}
		using (var stream = new System.IO.StreamReader(filePath, Encoding.UTF8)) {
			json = stream.ReadToEnd();
		}
		return JsonConvert.DeserializeObject<T>(json, Settings);
	}

	public static T? JsonParse<T>(string json) where T : new() {
		return JsonConvert.DeserializeObject<T>(json, Settings);
	}

	/// <summary>
	/// Writes the object to a file.
	/// </summary>
	/// <param name="obj">Object to serialize.</param>
	/// <param name="filePath">File name.</param>
	public static void SaveConfig(object obj, string filePath) {
		(new FileInfo(filePath)).Directory.Create();
		using (var stream = new System.IO.StreamWriter(filePath, false, Encoding.UTF8)) {
			stream.Write(JsonConvert.SerializeObject(obj, Formatting.None, Settings));
		}
	}

	public static string JsonStringify(object obj) {
		return JsonConvert.SerializeObject(obj, Formatting.None, Settings);
	}
}
