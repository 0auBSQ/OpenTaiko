using System.Text;
using NLua;

namespace OpenTaiko {
	public static class LuaSecurity {
		private const string luaScriptSecure = $$"""
loadfile = nil
dofile = nil

os = { time = os.time, difftime = os.difftime, date = os.date }
io = nil
debug = nil

CLRPackage = nil
luanet = nil

do
	local allowed = { config, searchers, searchpath }
	for k, _ in pairs(package) do
		if not allowed[k] then
			package[k] = nil
		end
	end
end
package.cpath = ""
package.path = ""
package.loaded = {}
package.preload = {}
package.searchers = {}

do
	local _readFile = _csReadFile
	local _packagePaths = _csPackagePaths
	package.searchers[1] = function(modname)
		return package.preload[modname], ":preload:"
	end
	package.searchers[2] = function(modname)
		local filename = modname:gsub("%.", "/")
		local tried = {}
		for i = 0, _packagePaths.Count - 1, 1 do
			local pattern = _packagePaths[i]
			local full = pattern:gsub("%?", filename)
			local content = _readFile(full)
			if content ~= nil then
				local fn, err = load(content, "@" .. full)
				if fn == nil then return err end
				return fn, full
			end
			tried[#tried + 1] = "no file '" .. full .. "'"
		end
		return table.concat(tried, "\n\t")
	end
end
_csReadFile = nil
_csPackagePaths = nil

import = function () end
""";

		public static void Secure(Lua lua, string directory) {
			string normalizedDirectory = directory.Replace('\\', '/');

			// Lua's built-in require uses fopen() which is ANSI code-page-limited on Windows.
			// Expose a C# reader so the custom searcher below can open files on any Unicode path.
			lua["_csReadFile"] = (Func<string, string?>)(path => {
				if (!FDK.CTexture.FileExistsCached(path)) return null;
				try { return File.ReadAllText(path, Encoding.UTF8); }
				catch { return null; }
			});

			// build path
			List<string> paths = new() { $"{normalizedDirectory}/?.lua" };
			bool underSkinDir = Path.GetRelativePath(OpenTaiko.ConfigIni.strSystemSkinSubfolderFullName, normalizedDirectory) != normalizedDirectory;
			if (underSkinDir)
				paths.Add($"{OpenTaiko.ConfigIni.strSystemSkinSubfolderFullName}Modules/Lib/?.lua");
			else
				paths.AddRange(OpenTaiko.GetMergedDirectories("", "Global/Lib/").Select(v => $"{v}?.lua"));
			lua["_csPackagePaths"] = paths;

			lua.DoString(luaScriptSecure, nameof(luaScriptSecure));

			// remove dropped references
			lua.State.GarbageCollector(KeraLua.LuaGC.Collect, 0);
		}
	}
}
