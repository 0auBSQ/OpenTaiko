namespace OpenTaiko {
	internal class VersionComparer {
		public static int CompareVersions(string versionA, string versionB) {
			// Strip leading 'v' if present
			versionA = versionA.TrimStart('v', 'V');
			versionB = versionB.TrimStart('v', 'V');

			// Parse both versions
			Version vA = new Version(NormalizeVersion(versionA));
			Version vB = new Version(NormalizeVersion(versionB));

			return vA.CompareTo(vB);
		}

		private static string NormalizeVersion(string version) {
			// Ensure version string has at least 2 components: major.minor
			var parts = version.Split('.');
			while (parts.Length < 4) {
				version += ".0";
				parts = version.Split('.');
			}
			return string.Join(".", parts);
		}
	}

}
