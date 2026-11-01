namespace OpenTaiko;

/// <summary>
/// 「整数」を表すアイテム。
/// </summary>
internal class CItemInteger : CItemBase {
	// Properties

	public int nCurrentValue;
	public bool bValueFocus;


	// Constructor

	public CItemInteger() {
		base.eType = CItemBase.EType.Int;
		this.nMinValue = 0;
		this.nMaxValue = 0;
		this.nCurrentValue = 0;
		this.bValueFocus = false;
	}
	public CItemInteger(string strItemName, int nMinValue, int nMaxValue, int nInitialValue)
		: this() {
		this.tInitialize(strItemName, nMinValue, nMaxValue, nInitialValue);
	}
	public CItemInteger(string strItemName, int nMinValue, int nMaxValue, int nInitialValue, string strDescriptionjp)
		: this() {
		this.tInitialize(strItemName, nMinValue, nMaxValue, nInitialValue, strDescriptionjp);
	}
	public CItemInteger(string strItemName, int nMinValue, int nMaxValue, int nInitialValue, string strDescriptionjp, string strDescriptionen)
		: this() {
		this.tInitialize(strItemName, nMinValue, nMaxValue, nInitialValue, strDescriptionjp, strDescriptionen);
	}


	public CItemInteger(string strItemName, int nMinValue, int nMaxValue, int nInitialValue, CItemBase.EPanelType ePanelType)
		: this() {
		this.tInitialize(strItemName, nMinValue, nMaxValue, nInitialValue, ePanelType);
	}
	public CItemInteger(string strItemName, int nMinValue, int nMaxValue, int nInitialValue, CItemBase.EPanelType ePanelType, string strDescriptionjp)
		: this() {
		this.tInitialize(strItemName, nMinValue, nMaxValue, nInitialValue, ePanelType, strDescriptionjp);
	}
	public CItemInteger(string strItemName, int nMinValue, int nMaxValue, int nInitialValue, CItemBase.EPanelType ePanelType, string strDescriptionjp, string strDescriptionen)
		: this() {
		this.tInitialize(strItemName, nMinValue, nMaxValue, nInitialValue, ePanelType, strDescriptionjp, strDescriptionen);
	}


	// CItemBase 実装

	public override void tEnterPressed() {
		this.bValueFocus = !this.bValueFocus;
	}
	public override void tItemValueNextMove() {
		if (++this.nCurrentValue > this.nMaxValue) {
			this.nCurrentValue = this.nMaxValue;
		}
	}
	public override void tItemValuePrevMove() {
		if (--this.nCurrentValue < this.nMinValue) {
			this.nCurrentValue = this.nMinValue;
		}
	}
	public void tInitialize(string strItemName, int nMinValue, int nMaxValue, int nInitialValue) {
		this.tInitialize(strItemName, nMinValue, nMaxValue, nInitialValue, CItemBase.EPanelType.Normal, "", "");
	}
	public void tInitialize(string strItemName, int nMinValue, int nMaxValue, int nInitialValue, string strDescriptionjp) {
		this.tInitialize(strItemName, nMinValue, nMaxValue, nInitialValue, CItemBase.EPanelType.Normal, strDescriptionjp, strDescriptionjp);
	}
	public void tInitialize(string strItemName, int nMinValue, int nMaxValue, int nInitialValue, string strDescriptionjp, string strDescriptionen) {
		this.tInitialize(strItemName, nMinValue, nMaxValue, nInitialValue, CItemBase.EPanelType.Normal, strDescriptionjp, strDescriptionen);
	}


	public void tInitialize(string strItemName, int nMinValue, int nMaxValue, int nInitialValue, CItemBase.EPanelType ePanelType) {
		this.tInitialize(strItemName, nMinValue, nMaxValue, nInitialValue, ePanelType, "", "");
	}
	public void tInitialize(string strItemName, int nMinValue, int nMaxValue, int nInitialValue, CItemBase.EPanelType ePanelType, string strDescriptionjp) {
		this.tInitialize(strItemName, nMinValue, nMaxValue, nInitialValue, ePanelType, strDescriptionjp, strDescriptionjp);
	}
	public void tInitialize(string strItemName, int nMinValue, int nMaxValue, int nInitialValue, CItemBase.EPanelType ePanelType, string strDescriptionjp, string strDescriptionen) {
		base.tInitialize(strItemName, ePanelType, strDescriptionjp, strDescriptionen);
		this.nMinValue = nMinValue;
		this.nMaxValue = nMaxValue;
		this.nCurrentValue = nInitialValue;
		this.bValueFocus = false;
	}
	public override object objCurrentValue() {
		return this.nCurrentValue;
	}
	public override int GetIndex() {
		return this.nCurrentValue;
	}
	public override void SetIndex(int index) {
		this.nCurrentValue = index;
	}
	// その他

	#region [ private ]
	//-----------------
	private int nMinValue;
	private int nMaxValue;
	//-----------------
	#endregion
}
