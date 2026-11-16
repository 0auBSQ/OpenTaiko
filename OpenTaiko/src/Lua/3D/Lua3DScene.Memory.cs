using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using FDK;

namespace OpenTaiko {
	// memory readouts and trimming
	public partial class Lua3DScene {
		/// <summary>"key=value" pairs, megabytes unless the key ends in N. Takes milliseconds: not every frame.</summary>
		public string GetMemoryReport() {
			var sb = new StringBuilder(512);
			void Mb(string k, long bytes) => sb.Append(k).Append('=').Append((bytes / 1048576.0).ToString("F1", CultureInfo.InvariantCulture)).Append(' ');
			void N(string k, long n) => sb.Append(k).Append('=').Append(n.ToString(CultureInfo.InvariantCulture)).Append(' ');
			AppendProcess(sb);
			if (_disposedValue) return sb.ToString().TrimEnd();
			long geom = 0, vn = 0;
			foreach (var o in _objects.Values) { geom += (long)o.D.Length * 8; if (o.VN != null) vn += (long)o.VN.Length * 8; }
			Mb("objGeom", geom); Mb("objNormals", vn); N("objN", _objects.Count);
			long tb = 0, sp = 0; int tn = 0, sn = 0;
			foreach (var e in _tex.Values) if (e.Pix != null) { tn++; tb += (long)e.Pix.Length * 4; }
			foreach (var e in _sprites.Values) if (e.Pix != null) { sn++; sp += (long)e.Pix.Length * 4; }
			Mb("sceneTex", tb); N("sceneTexN", tn); Mb("sceneSprite", sp); N("sceneSpriteN", sn);
			if (_gpuRasterizer != null) sb.Append(_gpuRasterizer.MemoryReport()).Append(' ');
			var (files, mg, mp) = GltfModel.CacheStats();
			N("modelFilesN", files); Mb("modelGeom", mg); Mb("modelPix", mp);
			return sb.ToString().TrimEnd();
		}

		/// <summary>Largest textures and sprites, one per line: kind, id, WxH, CPU MB, model file or "shared".</summary>
		public string GetTextureReport(int top) {
			var rows = new System.Collections.Generic.List<(long bytes, string line)>();
			var owner = new System.Collections.Generic.Dictionary<int, string>();
			foreach (var kv in _modelTexBlocks)
				for (int i = 0; i < kv.Value.count; i++) owner[kv.Value.first + i] = System.IO.Path.GetFileName(kv.Key);
			foreach (var kv in _sharedModelTex) owner[kv.Value] = "shared";
			var inv = CultureInfo.InvariantCulture;
			foreach (var kv in _tex) if (kv.Value.Pix != null) {
				long b = (long)kv.Value.Pix.Length * 4;
				rows.Add((b, string.Format(inv, "tex {0} {1}x{2} {3:F1} {4}", kv.Key, kv.Value.W, kv.Value.H, b / 1048576.0, owner.TryGetValue(kv.Key, out var o) ? o : "")));
			}
			foreach (var kv in _sprites) if (kv.Value.Pix != null) {
				long b = (long)kv.Value.Pix.Length * 4;
				rows.Add((b, string.Format(inv, "sprite {0} {1}x{2} {3:F1}", kv.Key, kv.Value.W, kv.Value.H, b / 1048576.0)));
			}
			rows.Sort((a, b) => b.bytes.CompareTo(a.bytes));
			var sb = new StringBuilder();
			for (int i = 0; i < rows.Count && i < Math.Max(1, top); i++) sb.Append(rows[i].line.TrimEnd()).Append((char)10);
			return sb.ToString();
		}

		// model file -> shared image ids; shared id -> user count
		private readonly System.Collections.Generic.Dictionary<string, int[]> _modelShared = new(StringComparer.OrdinalIgnoreCase);
		private readonly System.Collections.Generic.Dictionary<int, int> _sharedRefs = new();
		internal void ModelRegistered(string key, System.Collections.Generic.List<int>? shared) {
			if (_disposedValue || _modelShared.ContainsKey(key)) return;
			var ids = shared == null ? Array.Empty<int>() : shared.ToArray();
			_modelShared[key] = ids;
			foreach (int id in ids) _sharedRefs[id] = (_sharedRefs.TryGetValue(id, out int n) ? n : 0) + 1;
		}
		internal bool HoldsTexture(int id, int w, int h)
			=> _tex.TryGetValue(id, out var e) && e.W == w && e.H == h && (e.Pix != null || e.GpuOnly);
		private void ForgetSharedId(int id) {
			foreach (var kv in _sharedModelTex) if (kv.Value == id) { GltfModel.ForgetShared(kv.Key); break; }
		}
		internal void ModelUnregistered(string key) {
			if (_disposedValue) return;
			if (_modelTexBlocks.TryGetValue(key, out var b))
				for (int i = 0; i < b.count; i++) if (_tex.ContainsKey(b.first + i)) UnregisterTexture(b.first + i);
			if (_modelShared.Remove(key, out var ids))
				foreach (int id in ids) {
					int n = (_sharedRefs.TryGetValue(id, out int have) ? have : 1) - 1;
					if (n > 0) _sharedRefs[id] = n;
					else { _sharedRefs.Remove(id); if (_tex.ContainsKey(id)) UnregisterTexture(id); ForgetSharedId(id); }
				}
		}
		/// <summary>Frees all model textures; only with no model objects left. Register restores them.</summary>
		public void PurgeModelTextures() {
			if (_disposedValue) return;
			var ids = new System.Collections.Generic.List<int>();
			foreach (int id in _tex.Keys) if (id >= ModelTextureFirst) ids.Add(id);
			foreach (int id in ids) UnregisterTexture(id);
			foreach (var kv in _sharedModelTex) GltfModel.ForgetShared(kv.Key);
			_modelShared.Clear(); _sharedRefs.Clear();
		}

		internal int _gpuOnlyMin;
		/// <summary>Pictures of at least minPixels drop their CPU copy two frames after upload; 0 (default) = off.
		/// Those draw blank on CPU renderers, cannot feed RegisterTextureTinted, ignore later filter changes.</summary>
		public void SetGpuOnlyPixels(int minPixels) { _gpuOnlyMin = Math.Max(0, minPixels); }

		/// <summary>Shrinks emptied arrays, then collects above minSlackMb of slack. MB freed; call under a cover.</summary>
		public double TrimMemory(double minSlackMb) {
			if (!_disposedValue)
				foreach (var o in _objects.Values) {
					int need = o.N * PrimLayout.Stride(o.Kind);
					if (o.D.Length > need + need / 4 + 256) { if (need == 0) o.D = Array.Empty<double>(); else Array.Resize(ref o.D, need); }
					if (o.VN != null && !o.VnOn) o.VN = null;
				}
			long before = GC.GetGCMemoryInfo().TotalCommittedBytes;
			if (before - GC.GetTotalMemory(false) < minSlackMb * 1048576.0) return 0;
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false);
			return Math.Max(0, (before - GC.GetGCMemoryInfo().TotalCommittedBytes) / 1048576.0);
		}

		/// <summary>Compacting TrimMemory, no threshold: seconds on a paged-out heap, map unload only. MB freed.</summary>
		public double TrimMemoryCompacting() {
			TrimMemory(double.MaxValue);   // the emptied arrays only
			long before = GC.GetGCMemoryInfo().TotalCommittedBytes;
			System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
			return Math.Max(0, (before - GC.GetGCMemoryInfo().TotalCommittedBytes) / 1048576.0);
		}

		public void CollectInBackground() {
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: false, compacting: false);
		}

		internal bool _profileSync = true;
		/// <summary>true (default): SetProfile waits for the GPU per pass; false: CPU submit time only.</summary>
		public void SetProfileSync(bool on) { _profileSync = on; }

		/// <summary>GetMemoryReport's process keys only.</summary>
		public static string ProcessReport() {
			var sb = new StringBuilder(256);
			AppendProcess(sb);
			return sb.ToString().TrimEnd();
		}
		private static void AppendProcess(StringBuilder sb) {
			void Mb(string k, long bytes) => sb.Append(k).Append('=').Append((bytes / 1048576.0).ToString("F1", CultureInfo.InvariantCulture)).Append(' ');
			void N(string k, long n) => sb.Append(k).Append('=').Append(n.ToString(CultureInfo.InvariantCulture)).Append(' ');
			try {
				using var proc = Process.GetCurrentProcess();
				Mb("private", proc.PrivateMemorySize64); Mb("workingSet", proc.WorkingSet64);
			} catch (Exception) { }
			Mb("managed", GC.GetTotalMemory(false)); Mb("gcCommitted", GC.GetGCMemoryInfo().TotalCommittedBytes);
			Mb("tex2dGpu", CTexture.s_gpuTextureBytes); Mb("tex2dLive", CTexture.LiveBytes); N("tex2dN", CTexture.LiveCount);
			N("soundsN", CSound.SoundInstances.Count);
		}

		/// <summary>CPU bytes of the object's primitives and normals; 0 = unknown id.</summary>
		public double GetObjectBytes(int objId) {
			if (_disposedValue || !_objects.TryGetValue(objId, out var o)) return 0;
			return (double)o.D.Length * 8 + (o.VN != null ? (double)o.VN.Length * 8 : 0);
		}
	}
}
