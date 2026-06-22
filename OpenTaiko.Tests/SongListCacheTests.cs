using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// ── Regression guard for the songlist.db cache schema-version stamp ────────────────────────────────
	// songlist.db is a BinaryFormatter cache that (de)serializes CSongListNode/CScore BY FIELD NAME. When
	// those fields are renamed (as in the Japanese→English pass), BinaryFormatter does NOT throw — it
	// silently leaves the renamed fields null, so a stale cache loads with every renamed field empty
	// (which blanked AudioPath/PreimagePath for all songs). The fix stamps the cache with a fingerprint
	// of the serialized type graph and discards the cache when it doesn't match. These tests pin that the
	// stamp round-trips and that an old/mismatched cache is rejected so the list rebuilds automatically.
#pragma warning disable SYSLIB0011
	public class SongListCacheTests {
		[Fact]
		public void Signature_IsNonEmpty_AndDeterministic() {
			Assert.False(string.IsNullOrEmpty(CEnumSongs.SongListCacheSchemaSignature));
			Assert.Equal(CEnumSongs.SongListCacheSchemaSignature, CEnumSongs.SongListCacheSchemaSignature);
		}

		[Fact]
		public void Cache_RoundTrips_WhenSignatureMatches() {
			var dict = new Dictionary<string, CSongListNode>();
			using var ms = new MemoryStream();
			CEnumSongs.WriteSongListCache(ms, dict);
			ms.Position = 0;
			Assert.NotNull(CEnumSongs.ReadSongListCache(ms));   // matching signature → cache is used
		}

		[Fact]
		public void Cache_Discarded_WhenLegacyHeaderless() {
			// Old format: the dictionary serialized with NO signature header (what shipped before the fix).
			var dict = new Dictionary<string, CSongListNode>();
			using var ms = new MemoryStream();
			new BinaryFormatter().Serialize(ms, dict);
			ms.Position = 0;
			Assert.Null(CEnumSongs.ReadSongListCache(ms));      // → caller rebuilds from disk
		}

		[Fact]
		public void Cache_Discarded_WhenSignatureDiffers() {
			var dict = new Dictionary<string, CSongListNode>();
			using var ms = new MemoryStream();
			var bf = new BinaryFormatter();
			bf.Serialize(ms, "stale-schema-signature");          // a different (older) schema fingerprint
			bf.Serialize(ms, dict);
			ms.Position = 0;
			Assert.Null(CEnumSongs.ReadSongListCache(ms));      // → caller rebuilds from disk
		}
	}
#pragma warning restore SYSLIB0011
}