using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// ── Regression guard for the songlist.db cache schema-version stamp ────────────────────────────────
	// songlist.db (de)serializes CSongListNode/CScore BY FIELD NAME. When those fields are renamed (as in
	// the Japanese→English pass), the reader does NOT throw — it leaves the renamed fields at their
	// defaults, so a stale cache loads with every renamed field empty (which blanked AudioPath/PreimagePath
	// for all songs). The cache is stamped with a fingerprint of the serialized type graph and discarded
	// when it doesn't match. These tests pin that the stamp round-trips, that the songs come back, and that
	// an old or mismatched cache is rejected so the list rebuilds automatically.
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
		public void Cache_KeepsTheSongs() {
			var node = new CSongListNode { songGenre = "Placeholder Genre", strMaker = "Placeholder Maker" };
			node.nLevel[3] = 8;
			var score = new CScore();
			score.FileInfo.FileAbsolutePath = "Songs/Placeholder/chart.tja";
			score.ChartInfo.Title = "Placeholder Title";
			node.score[3] = score;
			var dict = new Dictionary<string, CSongListNode> { ["Songs/Placeholder/chart.tja#hash"] = node };
			using var ms = new MemoryStream();
			CEnumSongs.WriteSongListCache(ms, dict);
			ms.Position = 0;

			var read = CEnumSongs.ReadSongListCache(ms);
			Assert.NotNull(read);
			var back = read["Songs/Placeholder/chart.tja#hash"];
			Assert.Equal("Placeholder Genre", back.songGenre);
			Assert.Equal("Placeholder Maker", back.strMaker);
			Assert.Equal(8, back.nLevel[3]);
			Assert.Equal("Songs/Placeholder/chart.tja", back.score[3].FileInfo.FileAbsolutePath);
			Assert.Equal("Placeholder Title", back.score[3].ChartInfo.Title);
			Assert.NotNull(back.DanSongs);   // restored to an empty list on load
		}

		[Fact]
		public void Cache_Discarded_WhenSignatureDiffers() {
			using var ms = new MemoryStream(Encoding.UTF8.GetBytes("{\"Signature\":\"stale-schema-signature\",\"Songs\":{}}"));
			Assert.Null(CEnumSongs.ReadSongListCache(ms));      // → caller rebuilds from disk
		}

		[Fact]
		public void Cache_FromBeforeNet10_IsNotRead() {
			// The BinaryFormatter cache (before .NET 10) starts with its stream header, not JSON: the read
			// throws, and CEnumSongs.Deserialize catches it and rebuilds from disk.
			using var ms = new MemoryStream(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x01, 0x00, 0x00, 0x00 });
			Assert.ThrowsAny<JsonException>(() => CEnumSongs.ReadSongListCache(ms));
		}
	}
}