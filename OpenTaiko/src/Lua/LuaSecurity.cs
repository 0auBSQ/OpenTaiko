using NLua;

namespace OpenTaiko {
	public static class LuaSecurity {
		public static void Secure(Lua lua, string directory) {

			string normalizedDirectory = directory.Replace('\\', '/');

			lua.DoString(@$"
for k, _ in pairs(os) do
	if k ~= ""time"" then
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
-- while #package.searchers > 0 do
--     table.remove(package.searchers);
-- end

import = function () end
");
		}
	}
}
