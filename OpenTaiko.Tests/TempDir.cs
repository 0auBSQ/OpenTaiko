using System;
using System.IO;

namespace OpenTaikoTests;

public sealed class TempDir : IDisposable {
	public TempDir(string prefix) {
		this.Dir = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(this.Dir);
	}
	public void Dispose() {
		try {
			Directory.Delete(this.Dir, true);
		} catch (IOException) { }
	}
	public string Dir { get; }
	public static implicit operator string(TempDir dir) => dir.Dir;
}
