namespace OpenTaiko;

/// <summary>
/// 「トグル」（ON, OFF の2状態）を表すアイテム。
/// </summary>
internal class CItemToggle : CItemBase {
	// Properties

	public bool bON;

	// Constructor

	public CItemToggle() {
		base.eType = CItemBase.EType.ONorOFFToggle;
		this.bON = false;
	}
	public CItemToggle(string strItemName, bool bInitialState)
		: this() {
		this.tInitialize(strItemName, bInitialState);
	}
	public CItemToggle(string strItemName, bool bInitialState, string strDescriptionjp)
		: this() {
		this.tInitialize(strItemName, bInitialState, strDescriptionjp);
	}
	public CItemToggle(string strItemName, bool bInitialState, string strDescriptionjp, string strDescriptionen)
		: this() {
		this.tInitialize(strItemName, bInitialState, strDescriptionjp, strDescriptionen);
	}
	public CItemToggle(string strItemName, bool bInitialState, CItemBase.EPanelType ePanelType)
		: this() {
		this.tInitialize(strItemName, bInitialState, ePanelType);
	}
	public CItemToggle(string strItemName, bool bInitialState, CItemBase.EPanelType ePanelType, string strDescriptionjp)
		: this() {
		this.tInitialize(strItemName, bInitialState, ePanelType, strDescriptionjp);
	}
	public CItemToggle(string strItemName, bool bInitialState, CItemBase.EPanelType ePanelType, string strDescriptionjp, string strDescriptionen)
		: this() {
		this.tInitialize(strItemName, bInitialState, ePanelType, strDescriptionjp, strDescriptionen);
	}


	// CItemBase 実装

	public override void tEnterPressed() {
		this.tItemValueNextMove();
	}
	public override void tItemValueNextMove() {
		this.bON = !this.bON;
	}
	public override void tItemValuePrevMove() {
		this.tItemValueNextMove();
	}
	public void tInitialize(string strItemName, bool bInitialState) {
		this.tInitialize(strItemName, bInitialState, CItemBase.EPanelType.Normal);
	}
	public void tInitialize(string strItemName, bool bInitialState, string strDescriptionjp) {
		this.tInitialize(strItemName, bInitialState, CItemBase.EPanelType.Normal, strDescriptionjp, strDescriptionjp);
	}
	public void tInitialize(string strItemName, bool bInitialState, string strDescriptionjp, string strDescriptionen) {
		this.tInitialize(strItemName, bInitialState, CItemBase.EPanelType.Normal, strDescriptionjp, strDescriptionen);
	}

	public void tInitialize(string strItemName, bool bInitialState, CItemBase.EPanelType ePanelType) {
		this.tInitialize(strItemName, bInitialState, ePanelType, "", "");
	}
	public void tInitialize(string strItemName, bool bInitialState, CItemBase.EPanelType ePanelType, string strDescriptionjp) {
		this.tInitialize(strItemName, bInitialState, ePanelType, strDescriptionjp, strDescriptionjp);
	}
	public void tInitialize(string strItemName, bool bInitialState, CItemBase.EPanelType ePanelType, string strDescriptionjp, string strDescriptionen) {
		base.tInitialize(strItemName, ePanelType, strDescriptionjp, strDescriptionen);
		this.bON = bInitialState;
	}
	public override object objCurrentValue() {
		return (this.bON) ? "ON" : "OFF";
	}
	public override int GetIndex() {
		return (this.bON) ? 1 : 0;
	}
	public override void SetIndex(int index) {
		switch (index) {
			case 0:
				this.bON = false;
				break;
			case 1:
				this.bON = true;
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}
}
