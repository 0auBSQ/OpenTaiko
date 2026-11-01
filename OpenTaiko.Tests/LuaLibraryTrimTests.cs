using System;
using System.Collections.Generic;
using NLua;
using Xunit;

namespace OpenTaiko.Tests;

// LuaSecurity's prelude: scripts get the trimmed standard libraries both as globals and through require,
// and load takes text chunks only
public class LuaLibraryTrimTests {
	private static Lua NewState() {
		var lua = new Lua();
		lua["_csReadFile"] = (Func<string, string?>)(_ => null);
		lua["_csPackagePaths"] = new List<string>();
		lua.DoString(LuaSecurity.luaScriptSecure, "prelude");
		return lua;
	}

	private static object Eval(Lua lua, string code) => lua.DoString(code)[0];

	[Theory]
	[InlineData("io")]
	[InlineData("debug")]
	public void RemovedLibrariesCannotBeRequired(string name) {
		using var lua = NewState();
		Assert.Equal(false, Eval(lua, $"return (pcall(require, '{name}'))"));
	}

	[Fact]
	public void RequireGivesTheTrimmedOs() {
		using var lua = NewState();
		Assert.Equal(true, Eval(lua, "return require('os') == os and os.execute == nil and os.remove == nil and os.time ~= nil"));
	}

	[Fact]
	public void KeptLibrariesStillRequire() {
		using var lua = NewState();
		Assert.Equal(true, Eval(lua, "return require('string') == string and require('math') == math and require('table') == table"));
	}

	[Fact]
	public void LoadKeepsTextChunksAndItsEnvironment() {
		using var lua = NewState();
		Assert.Equal(3L, Eval(lua, "return load('return 1 + 2')()"));
		Assert.Equal(7L, Eval(lua, "y = 7 return load('return y')()"));
		Assert.Equal(5L, Eval(lua, "return load('return x', 'c', 'bt', { x = 5 })()"));
	}

	[Fact]
	public void LoadRefusesPrecompiledChunks() {
		using var lua = NewState();
		Assert.Equal(true, Eval(lua, "return load(string.dump(function() return 1 end)) == nil"));
	}
}
