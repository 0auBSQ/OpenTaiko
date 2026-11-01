using System;
using System.Collections.Generic;
using System.Reflection;
using NLua;
using Xunit;

namespace OpenTaiko.Tests;

// LuaSecurity's prelude: engine objects show their documented members, reflection members stay hidden
public class LuaMemberAccessTests {
	public class Sample {
		public int Count = 3;
		public string Name { get; set; } = "drum";
		public int Twice(int x) => x * 2;
		public Type Kind => typeof(Sample);
		public MethodInfo? Info() => null;
		public List<Type> Kinds() => new();
	}

	private static object Eval(Lua lua, string code) => lua.DoString(code)[0];

	[Fact]
	public void DocumentedMembersStillWork() {
		using var lua = LuaLibraryTrimTests.NewState();
		lua["obj"] = new Sample();
		Assert.Equal(3L, Eval(lua, "return obj.Count"));
		Assert.Equal("drum", Eval(lua, "return obj.Name"));
		Assert.Equal(8L, Eval(lua, "return obj:Twice(4)"));
		Assert.Equal("don", Eval(lua, "obj.Name = 'don' return obj.Name"));
		Assert.Equal(true, Eval(lua, "return tostring(obj) ~= nil"));
	}

	[Fact]
	public void EngineApiStillWorks() {
		using var lua = LuaLibraryTrimTests.NewState();
		lua["VECTOR2"] = new LuaVector2Func();
		Assert.Equal(5.0, Eval(lua, "return VECTOR2:CreateVector2(3, 4):Length()"));
		Assert.Equal(4.0, Eval(lua, "return VECTOR2:One():Add(VECTOR2:CreateVector2(3, 0)).X"));
	}

	[Fact]
	public void ListsAndDelegatesStillWork() {
		using var lua = LuaLibraryTrimTests.NewState();
		lua["list"] = new List<string> { "a", "b" };
		lua["twice"] = (Func<int, int>)(x => x * 2);
		Assert.Equal("b", Eval(lua, "return list[1]"));
		Assert.Equal(2L, Eval(lua, "return list.Count"));
		Assert.Equal(10L, Eval(lua, "return twice(5)"));
	}

	[Fact]
	public void DictionaryKeysStillWork() {
		using var lua = LuaLibraryTrimTests.NewState();
		lua["dict"] = new Dictionary<string, int> { ["peer1"] = 4, ["peer2"] = 6 };
		Assert.Equal(10L, Eval(lua, "return dict['peer1'] + dict.peer2"));
		Assert.Equal(true, Eval(lua, "return dict.GetType == nil"));
	}

	[Fact]
	public void GetTypeIsHidden() {
		using var lua = LuaLibraryTrimTests.NewState();
		lua["obj"] = new Sample();
		lua["vec"] = new LuaVector2Func();
		Assert.Equal(true, Eval(lua, "return obj.GetType == nil and vec.GetType == nil"));
		Assert.Equal(false, Eval(lua, "return (pcall(function() return obj:GetType() end))"));
	}

	[Fact]
	public void MembersOfReflectionTypesAreHidden() {
		using var lua = LuaLibraryTrimTests.NewState();
		lua["obj"] = new Sample();
		lua["twice"] = (Func<int, int>)(x => x * 2);
		Assert.Equal(true, Eval(lua, "return obj.Kind == nil and obj.Info == nil and obj.Kinds == nil and twice.Method == nil"));
	}

	[Fact]
	public void ObjectsHandedOutBeforeThePreludeAreCovered() {
		using var lua = LuaLibraryTrimTests.NewState();
		lua["list"] = new List<string>();
		Assert.Equal(true, Eval(lua, "return list.GetType == nil"));
	}

	[Fact]
	public void RedefinedGlobalsDoNotChangeWhatIsHidden() {
		using var lua = LuaLibraryTrimTests.NewState();
		lua["obj"] = new Sample();
		Assert.Equal(true, Eval(lua, "type = function() return 'number' end getmetatable = function() return {} end return obj.GetType == nil and obj.Count == 3"));
	}

	[Theory]
	[InlineData(typeof(Sample), "GetType", true)]
	[InlineData(typeof(Sample), "Kind", true)]
	[InlineData(typeof(Sample), "Kinds", true)]
	[InlineData(typeof(Sample), "Twice", false)]
	[InlineData(typeof(Sample), "Name", false)]
	[InlineData(typeof(LuaVector2Func), "CreateVector2", false)]
	public void PolicyPerMember(Type type, string name, bool hidden) {
		Assert.Equal(hidden, LuaSecurity.IsMemberHidden(Activator.CreateInstance(type), name));
	}
}
