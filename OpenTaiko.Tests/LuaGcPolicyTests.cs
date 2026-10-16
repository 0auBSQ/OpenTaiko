using System;
using NLua;
using Xunit;

namespace OpenTaikoTests {
	// CLuaScript runs no collection step per Lua call: the shipped Lua build's own collector has to keep a heap
	// bounded while a per-frame function allocates garbage on every call.
	public class LuaGcPolicyTests {
		[Fact]
		public void AllocatingEveryCall_HeapStaysWithinAboutTwiceItsLiveData() {
			using var lua = new Lua();
			lua.DoString(@"
keep = {}
for i = 1, 20000 do keep[i] = { i, tostring(i) } end
function frame()
  local t = {}
  for i = 1, 200 do t[i] = { x = i, s = 'v' .. i } end
  return #t
end");
			lua.State.GarbageCollector(KeraLua.LuaGC.Collect, 0);
			double live = lua.State.GarbageCollector(KeraLua.LuaGC.Count, 0);
			using var frame = (LuaFunction)lua["frame"];
			double peak = 0;
			for (int f = 0; f < 3000; f++) {
				frame.Call();
				peak = Math.Max(peak, lua.State.GarbageCollector(KeraLua.LuaGC.Count, 0));
			}
			Assert.NotEqual(0, lua.State.GarbageCollector(KeraLua.LuaGC.IsRunning, 0));
			Assert.True(peak < live * 2.5 + 64, $"live {live} KB, peak {peak} KB");
		}
	}
}
