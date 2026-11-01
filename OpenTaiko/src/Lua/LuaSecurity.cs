using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using NLua;

namespace OpenTaiko {
	public static class LuaSecurity {
		internal const string luaScriptSecure = $$"""
loadfile = nil
dofile = nil

-- engine objects show their documented members only; HiddenMemberNames lists, once per type, the names to hide
do
	local type, gmatch, realMetatable, hiddenNames = type, string.gmatch, debug.getmetatable, _csMemberHidden
	local registry = debug.getregistry()
	local index = registry.luaNet_indexfunction
	local hiddenSets = setmetatable({}, { __mode = "k" })
	local everything = {}
	local function filtered(obj, name)
		if type(name) == "string" then
			local meta = realMetatable(obj)
			if meta == nil then return nil end
			local set = hiddenSets[meta]
			if set == nil then
				local names = hiddenNames(obj)
				if names == nil then
					set = everything
				else
					set = {}
					for n in gmatch(names, "[^\n]+") do set[n] = true end
				end
				hiddenSets[meta] = set
			end
			if set == everything or set[name] then return nil end
		end
		return index(obj, name)
	end
	registry.luaNet_indexfunction = filtered
	for _, v in pairs(registry) do
		if type(v) == "table" and rawget(v, "__index") == index then
			rawset(v, "__index", filtered)
		end
	end
end
_csMemberHidden = nil

do
	local _load = load
	load = function(chunk, chunkname, _, ...)
		return _load(chunk, chunkname, "t", ...)
	end
end

local _loaded = package.loaded

os = { time = os.time, difftime = os.difftime, date = os.date }
io = nil
debug = nil

CLRPackage = nil
luanet = nil

-- require hands out what this table holds: keep only the libraries scripts are given
do
	local keep = { _G = true, coroutine = true, math = true, string = true, table = true, utf8 = true }
	for k in pairs(_loaded) do
		if not keep[k] then _loaded[k] = nil end
	end
	_loaded.os = os
end

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

		// fromGlobal: the script is a global module (Global/Modules), its require looks in Global/Modules/Lib before the skin's Modules/Lib
		public static void Secure(Lua lua, string directory, bool fromGlobal = false) {
			string normalizedDirectory = directory.Replace('\\', '/');

			// Lua's built-in require uses fopen() which is ANSI code-page-limited on Windows.
			// Expose a C# reader so the custom searcher below can open files on any Unicode path.
			lua["_csReadFile"] = (Func<string, string?>)(path => {
				if (!FDK.CTexture.FileExistsCached(path)) return null;
				try { return File.ReadAllText(path, Encoding.UTF8); }
				catch { return null; }
			});

			// build path: the script's own folder, then the skin's Modules/Lib and Global/Modules/Lib (that one first for a global module)
			List<string> paths = new() { $"{normalizedDirectory}/?.lua" };
			string skinLib = $"{OpenTaiko.ConfigIni.strSystemSkinSubfolderFullName}Modules/Lib/?.lua";
			List<string> globalLib = new() { Path.Combine(CSkin.GlobalModulesFolder("Lib"), "?.lua").Replace('\\', '/') };
			if (OpenTaiko.strBundleFolder != null)
				globalLib.Add(Path.Combine(OpenTaiko.strBundleFolder, "Global", "Modules", "Lib", "?.lua").Replace('\\', '/'));
			if (fromGlobal) {
				paths.AddRange(globalLib);
				paths.Add(skinLib);
			} else {
				paths.Add(skinLib);
				paths.AddRange(globalLib);
			}
			lua["_csPackagePaths"] = paths;
			lua["_csMemberHidden"] = (Func<object?, string?>)HiddenMemberNames;

			lua.DoString(luaScriptSecure, nameof(luaScriptSecure));

			// remove dropped references
			lua.State.GarbageCollector(KeraLua.LuaGC.Collect, 0);
		}

		private static readonly ConcurrentDictionary<Type, string?> hiddenNames = new();

		// GetType and the members whose value is a reflection type, one per line; null when the object is itself a reflection object (all hidden)
		internal static string? HiddenMemberNames(object? obj) {
			if (obj == null) return "";
			return hiddenNames.GetOrAdd(obj.GetType(), ComputeHiddenNames);
		}

		internal static bool IsMemberHidden(object? obj, string name) =>
			HiddenMemberNames(obj) is not { } names || names.Split('\n').Contains(name);

		private static string? ComputeHiddenNames(Type type) {
			if (IsReflectionType(type)) return null;
			HashSet<string> names = new() { nameof(object.GetType) };
			foreach (MemberInfo member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy)) {
				Type? valueType = member switch {
					MethodInfo method => method.ReturnType,
					PropertyInfo property => property.PropertyType,
					FieldInfo field => field.FieldType,
					_ => null,
				};
				if (valueType != null && Carries(valueType, 0)) names.Add(member.Name);
			}
			return string.Join('\n', names);
		}

		// the type itself, an array or by-ref of it, or a generic argument (List<Type>, Func<MethodInfo>)
		private static bool Carries(Type type, int depth) {
			if (IsReflectionType(type)) return true;
			if (depth > 4) return false;
			if (type.HasElementType && Carries(type.GetElementType()!, depth + 1)) return true;
			return type.IsGenericType && type.GetGenericArguments().Any(arg => Carries(arg, depth + 1));
		}

		private static bool IsReflectionType(Type type) =>
			typeof(MemberInfo).IsAssignableFrom(type) || typeof(Assembly).IsAssignableFrom(type)
			|| typeof(Module).IsAssignableFrom(type) || typeof(ParameterInfo).IsAssignableFrom(type)
			|| type == typeof(RuntimeTypeHandle) || type == typeof(RuntimeMethodHandle)
			|| type == typeof(RuntimeFieldHandle) || type == typeof(ModuleHandle)
			|| (type.Namespace is { } ns && (ns.StartsWith("System.Reflection") || ns.StartsWith("System.Runtime.Loader")));
	}
}
