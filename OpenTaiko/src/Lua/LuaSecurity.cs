using System.Text;
using NLua;

namespace OpenTaiko {
	public static class LuaSecurity {
		public static void Secure(Lua lua, string directory) {
			string normalizedDirectory = directory.Replace('\\', '/');

			// Lua's built-in require uses fopen() which is ANSI code-page-limited on Windows.
			// Expose a C# reader so the custom searcher below can open files on any Unicode path.
			lua["_csReadFile"] = (Func<string, string?>)(path => {
				try { return File.ReadAllText(path, Encoding.UTF8); }
				catch { return null; }
			});

			lua.DoString(@$"
for k, _ in pairs(os) do
	if k ~= ""time"" and k ~= ""date"" then
	os[k] = nil
	end
end
for k, _ in pairs(io) do
	io[k] = nil
end
io = nil
for k, _ in pairs(debug) do
	debug[k] = nil
end
debug = nil

package.path = ""{normalizedDirectory}/?.lua;""

do
	local _readFile = _csReadFile
	package.searchers[2] = function(modname)
		local filename = modname:gsub(""%."", ""/"")
		local tried = {{}}
		for pattern in package.path:gmatch(""[^;]+"") do
			local full = pattern:gsub(""%?"", filename)
			local content = _readFile(full)
			if content ~= nil then
				local fn, err = load(content, ""@"" .. full)
				if fn == nil then return err end
				return fn, full
			end
			tried[#tried + 1] = ""no file '"" .. full .. ""'""
		end
		return table.concat(tried, ""\n\t"")
	end
end
_csReadFile = nil

import = function () end
");
		}
	}
}
