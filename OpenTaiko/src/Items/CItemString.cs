namespace OpenTaiko;

/// <summary>
/// A config-list item that holds a mutable string value.
/// The current string is shown as the item's value in the Draw() pipeline via obj現在値().
/// Increment _epoch in SetValue() to bust the texture cache so the new text is re-rendered.
/// </summary>
internal class CItemString : CItemBase {
	private string _value;
	private int _epoch;

	public string Value {
		get => _value;
		set {
			if (_value == value) return;
			_value = value;
			_epoch++;
		}
	}

	public CItemString(string label, string currentValue, string description)
		: base() {
		t初期化(label, EPanelType.Normal, description);
		e種別 = E種別.基本形;
		_value = currentValue;
		_epoch = 0;
	}

	public override object obj現在値() => _value ?? "";
	public override int GetIndex() => _epoch;
}
