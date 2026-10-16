using System;
using System.IO;
using System.Text;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// The glTF loader on tiny synthetic files written to the temp folder: node order (a child listed before
	// its parent), interleaved accessors, node matrices and a .gltf with its buffer in a data URI.
	public class GltfLoaderTests {
		[Fact]
		public void NodeOrderPutsParentsFirstAndCutsCycles() {
			int[] parent = { 2, -1, 1, 0 };   // 1 is the root, then 2, 0, 3
			Assert.Equal(new[] { 1, 2, 0, 3 }, GltfModel.NodeOrder(parent));

			int[] cycle = { 1, 0, -1 };       // 0 and 1 parent each other
			int[] order = GltfModel.NodeOrder(cycle);
			Assert.Equal(3, order.Length);
			Assert.Contains(-1, new[] { cycle[0], cycle[1] });
			foreach (int i in order) if (cycle[i] >= 0) Assert.True(Array.IndexOf(order, cycle[i]) < Array.IndexOf(order, i));
		}

		// one triangle (0,0,0) (1,0,0) (0,1,0) in node 0, placed at z 2 under node 1, which comes after it
		private const string Accessors = @"""accessors"":[{""bufferView"":0,""componentType"":5126,""count"":3,""type"":""VEC3""},
			{""bufferView"":1,""componentType"":5123,""count"":3,""type"":""SCALAR""}],
			""meshes"":[{""primitives"":[{""attributes"":{""POSITION"":0},""indices"":1}]}],""scenes"":[{""nodes"":[1]}]";

		private static byte[] TriangleBin(int stride) {
			var b = new byte[stride * 3 + 8];
			float[] p = { 0, 0, 0, 1, 0, 0, 0, 1, 0 };
			for (int v = 0; v < 3; v++)
				for (int k = 0; k < 3; k++) BitConverter.GetBytes(p[v * 3 + k]).CopyTo(b, v * stride + k * 4);
			for (int i = 0; i < 3; i++) BitConverter.GetBytes((ushort)i).CopyTo(b, stride * 3 + i * 2);
			return b;
		}

		private static (double, double, double, double, double, double) Bounds(string ext, byte[] content) {
			string path = Path.Combine(Path.GetTempPath(), "gltf-test-" + Guid.NewGuid().ToString("N") + ext);
			File.WriteAllBytes(path, content);
			try { return GltfModel.Load(path).GetBounds(); }
			finally { File.Delete(path); }
		}

		[Fact]
		public void ChildBeforeParentIsPlacedByItsParent() {
			var bin = TriangleBin(12);
			string json = @"{""asset"":{""version"":""2.0""},""buffers"":[{""byteLength"":" + bin.Length + @"}],
				""bufferViews"":[{""buffer"":0,""byteOffset"":0,""byteLength"":36},{""buffer"":0,""byteOffset"":36,""byteLength"":6}],"
				+ Accessors + @",""nodes"":[{""mesh"":0,""translation"":[0,0,2]},{""translation"":[10,0,0],""children"":[0]}]}";
			var j = Encoding.UTF8.GetBytes(json + new string(' ', (4 - json.Length % 4) % 4));
			var glb = new MemoryStream();
			var w = new BinaryWriter(glb);
			w.Write(0x46546C67u); w.Write(2u); w.Write((uint)(28 + j.Length + bin.Length));
			w.Write((uint)j.Length); w.Write(0x4E4F534Au); w.Write(j);
			w.Write((uint)bin.Length); w.Write(0x004E4942u); w.Write(bin);
			w.Flush();

			Assert.Equal((10.0, 0.0, 2.0, 11.0, 1.0, 2.0), Bounds(".glb", glb.ToArray()));
		}

		[Fact]
		public void GltfWithDataUriInterleavedPositionsAndMatrixNode() {
			// positions every 16 bytes (4 unused), parent matrix = translate (10,0,0) * rotate 90 deg about Y * scale 2
			var bin = TriangleBin(16);
			string json = @"{""asset"":{""version"":""2.0""},
				""buffers"":[{""byteLength"":" + bin.Length + @",""uri"":""data:application/octet-stream;base64," + Convert.ToBase64String(bin) + @"""}],
				""bufferViews"":[{""buffer"":0,""byteOffset"":0,""byteLength"":48,""byteStride"":16},{""buffer"":0,""byteOffset"":48,""byteLength"":6}],"
				+ Accessors + @",""nodes"":[{""mesh"":0,""translation"":[0,0,2]},
				{""matrix"":[0,0,-2,0, 0,2,0,0, 2,0,0,0, 10,0,0,1],""children"":[0]}]}";

			var (x0, y0, z0, x1, y1, z1) = Bounds(".gltf", Encoding.UTF8.GetBytes(json));
			Assert.Equal(14, x0, 9); Assert.Equal(0, y0, 9); Assert.Equal(-2, z0, 9);
			Assert.Equal(14, x1, 9); Assert.Equal(2, y1, 9); Assert.Equal(0, z1, 9);
		}
	}
}
