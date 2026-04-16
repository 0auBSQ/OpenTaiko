namespace OpenTaiko {
	/// <summary>
	/// Exposes mod-icon drawing to Lua scripts via the <c>MODICONS</c> global.
	/// Delegates to the <c>modicons</c> ROActivity.
	/// </summary>
	public class LuaModIconsFunc {
		/// <summary>Draws all mod icons for <paramref name="player"/> at (<paramref name="x"/>, <paramref name="y"/>)
		/// using the menu layout, via the <c>modicons</c> ROActivity.</summary>
		public void Draw(int player, int x, int y) {
			var ro = LuaROActivityWrapper.GetROActivity("modicons");
			if (ro != null && !ro.IsActive) ro.Activate();
			ro?.Draw(x, y, player, "menu");
		}
	}
}
