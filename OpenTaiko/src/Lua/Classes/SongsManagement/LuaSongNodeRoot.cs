namespace OpenTaiko {
	internal class LuaSongNodeRoot : LuaSongNode {

		public void AppendChild(LuaSongNode _node) {
			this._children.Add(_node);
		}

		public LuaSongNodeRoot() : base() {

		}
	}
}
