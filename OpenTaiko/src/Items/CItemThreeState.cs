namespace OpenTaiko;

/// <summary>
/// 「スリーステート」（ON, OFF, 不定 の3状態）を表すアイテム。
/// </summary>
internal class CItemThreeState : CItemBase {
	// Properties

	public EState eCurrentState;
	public enum EState {
		ON,
		OFF,
		Indeterminate
	}


	// Constructor

	public CItemThreeState() {
		base.eType = CItemBase.EType.ONorOFForIndeterminateThreeState;
		this.eCurrentState = EState.Indeterminate;
	}
	public CItemThreeState(string strItemName, EState eInitialState)
		: this() {
		this.tInitialize(strItemName, eInitialState);
	}
	public CItemThreeState(string strItemName, EState eInitialState, string strDescriptionjp)
		: this() {
		this.tInitialize(strItemName, eInitialState, strDescriptionjp, strDescriptionjp);
	}
	public CItemThreeState(string strItemName, EState eInitialState, string strDescriptionjp, string strDescriptionen)
		: this() {
		this.tInitialize(strItemName, eInitialState, strDescriptionjp, strDescriptionen);
	}

	public CItemThreeState(string strItemName, EState eInitialState, CItemBase.EPanelType ePanelType)
		: this() {
		this.tInitialize(strItemName, eInitialState, ePanelType);
	}
	public CItemThreeState(string strItemName, EState eInitialState, CItemBase.EPanelType ePanelType, string strDescriptionjp)
		: this() {
		this.tInitialize(strItemName, eInitialState, ePanelType, strDescriptionjp, strDescriptionjp);
	}
	public CItemThreeState(string strItemName, EState eInitialState, CItemBase.EPanelType ePanelType, string strDescriptionjp, string strDescriptionen)
		: this() {
		this.tInitialize(strItemName, eInitialState, ePanelType, strDescriptionjp, strDescriptionen);
	}


	// CItemBase 実装

	public override void tEnterPressed() {
		this.tItemValueNextMove();
	}
	public override void tItemValueNextMove() {
		switch (this.eCurrentState) {
			case EState.ON:
				this.eCurrentState = EState.OFF;
				return;

			case EState.OFF:
				this.eCurrentState = EState.ON;
				return;

			case EState.Indeterminate:
				this.eCurrentState = EState.ON;
				return;
		}
	}
	public override void tItemValuePrevMove() {
		switch (this.eCurrentState) {
			case EState.ON:
				this.eCurrentState = EState.OFF;
				return;

			case EState.OFF:
				this.eCurrentState = EState.ON;
				return;

			case EState.Indeterminate:
				this.eCurrentState = EState.OFF;
				return;
		}
	}
	public void tInitialize(string strItemName, EState eInitialState) {
		this.tInitialize(strItemName, eInitialState, CItemBase.EPanelType.Normal);
	}
	public void tInitialize(string strItemName, EState eInitialState, string strDescriptionjp) {
		this.tInitialize(strItemName, eInitialState, CItemBase.EPanelType.Normal, strDescriptionjp, strDescriptionjp);
	}
	public void tInitialize(string strItemName, EState eInitialState, string strDescriptionjp, string strDescriptionen) {
		this.tInitialize(strItemName, eInitialState, CItemBase.EPanelType.Normal, strDescriptionjp, strDescriptionen);
	}

	public void tInitialize(string strItemName, EState eInitialState, CItemBase.EPanelType ePanelType) {
		this.tInitialize(strItemName, eInitialState, CItemBase.EPanelType.Normal, "", "");
	}
	public void tInitialize(string strItemName, EState eInitialState, CItemBase.EPanelType ePanelType, string strDescriptionjp) {
		this.tInitialize(strItemName, eInitialState, CItemBase.EPanelType.Normal, strDescriptionjp, strDescriptionjp);
	}
	public void tInitialize(string strItemName, EState eInitialState, CItemBase.EPanelType ePanelType, string strDescriptionjp, string strDescriptionen) {
		base.tInitialize(strItemName, ePanelType, strDescriptionjp, strDescriptionen);
		this.eCurrentState = eInitialState;
	}
	public override object objCurrentValue() {
		if (this.eCurrentState == EState.Indeterminate) {
			return "- -";
		} else {
			return this.eCurrentState.ToString();
		}
	}
	public override int GetIndex() {
		return (int)this.eCurrentState;
	}
	public override void SetIndex(int index) {
		switch (index) {
			case 0:
				this.eCurrentState = EState.ON;
				break;
			case 1:
				this.eCurrentState = EState.OFF;
				break;
			case 2:
				this.eCurrentState = EState.Indeterminate;
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}
}
