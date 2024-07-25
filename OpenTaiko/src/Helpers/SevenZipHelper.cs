// http://www.nullskull.com/a/768/7zip-lzma-inmemory-compression-with-c.aspx
// http://www.7-zip.org/sdk.html

namespace SevenZip.Compression.LZMA {


	public static class SevenZipHelper {

		static int dictionary = 1 << 23;

		// static Int32 posStateBits = 2;
		// static Int32 litContextBits = 3; // for normal files
		// UInt32 litContextBits = 0; // for 32-bit data
		// static Int32 litPosBits = 0;
		// UInt32 litPosBits = 2; // for 32-bit data
		// static Int32 algorithm = 2;
		// static Int32 numFastBytes = 128;

		static bool eos = false;

		static CoderPropID[] propIDs =
				{
					CoderPropID.DictionarySize,
					CoderPropID.PosStateBits,
					CoderPropID.LitContextBits,
					CoderPropID.LitPosBits,
					CoderPropID.Algorithm,
					CoderPropID.NumFastBytes,
					CoderPropID.MatchFinder,
					CoderPropID.EndMarker
				};

		// these are the default properties, keeping it simple for now:
		static object[] properties =
				{
					(System.Int32)(dictionary),
					(System.Int32)(2),
					(System.Int32)(3),
					(System.Int32)(0),
					(System.Int32)(2),
					(System.Int32)(128),
					"bt4",
					eos
				};


		public static byte[] Compress(byte[] inputBytes) {
			byte[] retVal = null;
			SevenZip.Compression.LZMA.Encoder encoder = new SevenZip.Compression.LZMA.Encoder();
			encoder.SetCoderProperties(propIDs, properties);

			using (System.IO.MemoryStream strmInStream = new System.IO.MemoryStream(inputBytes)) {
				using (System.IO.MemoryStream strmOutStream = new System.IO.MemoryStream()) {
					encoder.WriteCoderProperties(strmOutStream);
					long fileSize = strmInStream.Length;
					for (int i = 0; i < 8; i++)
						strmOutStream.WriteByte((byte)(fileSize >> (8 * i)));

					encoder.Code(strmInStream, strmOutStream, -1, -1, null);
					retVal = strmOutStream.ToArray();
				} // End Using outStream

			} // End Using inStream 

			return retVal;
		} // End Function Compress


		public static byte[] Compress(string inFileName) {
			byte[] retVal = null;
			SevenZip.Compression.LZMA.Encoder encoder = new SevenZip.Compression.LZMA.Encoder();
			encoder.SetCoderProperties(propIDs, properties);

			using (System.IO.Stream strmInStream = new System.IO.FileStream(inFileName, System.IO.FileMode.Open, System.IO.FileAccess.Read)) {
				using (System.IO.MemoryStream strmOutStream = new System.IO.MemoryStream()) {
					encoder.WriteCoderProperties(strmOutStream);
					long fileSize = strmInStream.Length;
					for (int i = 0; i < 8; i++)
						strmOutStream.WriteByte((byte)(fileSize >> (8 * i)));

					encoder.Code(strmInStream, strmOutStream, -1, -1, null);
					retVal = strmOutStream.ToArray();
				} // End Using outStream

			} // End Using inStream 

			return retVal;
		} // End Function Compress


		public static void Compress(string inFileName, string outFileName) {
			SevenZip.Compression.LZMA.Encoder encoder = new SevenZip.Compression.LZMA.Encoder();
			encoder.SetCoderProperties(propIDs, properties);

			using (System.IO.Stream strmInStream = new System.IO.FileStream(inFileName, System.IO.FileMode.Open, System.IO.FileAccess.Read)) {

				using (System.IO.Stream strmOutStream = new System.IO.FileStream(outFileName, System.IO.FileMode.Create)) {
					encoder.WriteCoderProperties(strmOutStream);
					long fileSize = strmInStream.Length;
					for (int i = 0; i < 8; i++)
						strmOutStream.WriteByte((byte)(fileSize >> (8 * i)));

					encoder.Code(strmInStream, strmOutStream, -1, -1, null);

					strmOutStream.Flush();
					strmOutStream.Close();
				} // End Using outStream

			} // End Using inStream 

		} // End Function Compress




		public static byte[] Decompress(string inFileName) {
			byte[] retVal = null;

			SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();

			using (System.IO.Stream strmInStream = new System.IO.FileStream(inFileName, System.IO.FileMode.Open, System.IO.FileAccess.Read)) {
				strmInStream.Seek(0, 0);

				using (System.IO.MemoryStream strmOutStream = new System.IO.MemoryStream()) {
					byte[] properties2 = new byte[5];
					if (strmInStream.Read(properties2, 0, 5) != 5)
						throw (new System.Exception("input .lzma is too short"));

					long outSize = 0;
					for (int i = 0; i < 8; i++) {
						int v = strmInStream.ReadByte();
						if (v < 0)
							throw (new System.Exception("Can't Read 1"));
						outSize |= ((long)(byte)v) << (8 * i);
					} //Next i

					decoder.SetDecoderProperties(properties2);

					long compressedSize = strmInStream.Length - strmInStream.Position;
					decoder.Code(strmInStream, strmOutStream, compressedSize, outSize, null);

					retVal = strmOutStream.ToArray();
				} // End Using newOutStream

			} // End Using newInStream

			return retVal;
		} // End Function Decompress 


		public static void Decompress(string inFileName, string outFileName) {
			SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();

			using (System.IO.Stream strmInStream = new System.IO.FileStream(inFileName, System.IO.FileMode.Open, System.IO.FileAccess.Read)) {
				strmInStream.Seek(0, 0);

				using (System.IO.Stream strmOutStream = new System.IO.FileStream(outFileName, System.IO.FileMode.Create)) {
					byte[] properties2 = new byte[5];
					if (strmInStream.Read(properties2, 0, 5) != 5)
						throw (new System.Exception("input .lzma is too short"));

					long outSize = 0;
					for (int i = 0; i < 8; i++) {
						int v = strmInStream.ReadByte();
						if (v < 0)
							throw (new System.Exception("Can't Read 1"));
						outSize |= ((long)(byte)v) << (8 * i);
					} // Next i 

					decoder.SetDecoderProperties(properties2);

					long compressedSize = strmInStream.Length - strmInStream.Position;
					decoder.Code(strmInStream, strmOutStream, compressedSize, outSize, null);

					strmOutStream.Flush();
					strmOutStream.Close();
				} // End Using newOutStream 

			} // End Using newInStream 

		} // End Function Decompress 


		public static byte[] Decompress(byte[] inputBytes) {
			byte[] retVal = null;

			SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();

			using (System.IO.MemoryStream strmInStream = new System.IO.MemoryStream(inputBytes)) {
				strmInStream.Seek(0, 0);

				using (System.IO.MemoryStream strmOutStream = new System.IO.MemoryStream()) {
					byte[] properties2 = new byte[5];
					if (strmInStream.Read(properties2, 0, 5) != 5)
						throw (new System.Exception("input .lzma is too short"));

					long outSize = 0;
					for (int i = 0; i < 8; i++) {
						int v = strmInStream.ReadByte();
						if (v < 0)
							throw (new System.Exception("Can't Read 1"));
						outSize |= ((long)(byte)v) << (8 * i);
					} // Next i 

					decoder.SetDecoderProperties(properties2);

					long compressedSize = strmInStream.Length - strmInStream.Position;
					decoder.Code(strmInStream, strmOutStream, compressedSize, outSize, null);

					retVal = strmOutStream.ToArray();
				} // End Using newOutStream 

			} // End Using newInStream 

			return retVal;
		} // End Function Decompress 


	} // End Class SevenZipHelper 


} // End Namespace SevenZip.Compression.LZMA 
