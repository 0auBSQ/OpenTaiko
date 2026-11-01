namespace OpenTaiko;

/// <summary>
/// 「リスト」（複数の固定値からの１つを選択可能）を表すアイテム。
/// </summary>
internal class CItemList : CItemBase {
	// Properties

	public List<string> listItemValue;
	public int nCurrentSelectedItemNumber;


	// Constructor

	public CItemList() {
		base.eType = CItemBase.EType.List;
		this.nCurrentSelectedItemNumber = 0;
		this.listItemValue = new List<string>();
	}
	public CItemList(string strItemName)
		: this() {
		this.tInitialize(strItemName);
	}
	public CItemList(string strItemName, CItemBase.EPanelType ePanelType)
		: this() {
		this.tInitialize(strItemName, ePanelType);
	}
	public CItemList(string strItemName, CItemBase.EPanelType ePanelType, int nInitialIndexValue, params string[] argItemList)
		: this() {
		this.tInitialize(strItemName, ePanelType, nInitialIndexValue, argItemList);
	}
	public CItemList(string strItemName, CItemBase.EPanelType ePanelType, int nInitialIndexValue, string strDescriptionjp, params string[] argItemList)
		: this() {
		this.tInitialize(strItemName, ePanelType, nInitialIndexValue, strDescriptionjp, argItemList);
	}
	public CItemList(string strItemName, CItemBase.EPanelType ePanelType, int nInitialIndexValue, string strDescriptionjp, string strDescriptionen, params string[] argItemList)
		: this() {
		this.tInitialize(strItemName, ePanelType, nInitialIndexValue, strDescriptionjp, strDescriptionen, argItemList);
	}


	// CItemBase 実装

	public override void tEnterPressed() {
		this.tItemValueNextMove();
	}
	public override void tItemValueNextMove() {
		if (++this.nCurrentSelectedItemNumber >= this.listItemValue.Count) {
			this.nCurrentSelectedItemNumber = 0;
		}
	}
	public override void tItemValuePrevMove() {
		if (--this.nCurrentSelectedItemNumber < 0) {
			this.nCurrentSelectedItemNumber = this.listItemValue.Count - 1;
		}
	}
	public override void tInitialize(string strItemName, CItemBase.EPanelType ePanelType) {
		base.tInitialize(strItemName, ePanelType);
		this.nCurrentSelectedItemNumber = 0;
		this.listItemValue.Clear();
	}
	public void tInitialize(string strItemName, CItemBase.EPanelType ePanelType, int nInitialIndexValue, params string[] argItemList) {
		this.tInitialize(strItemName, ePanelType, nInitialIndexValue, "", "", argItemList);
	}
	public void tInitialize(string strItemName, CItemBase.EPanelType ePanelType, int nInitialIndexValue, string strDescriptionjp, params string[] argItemList) {
		this.tInitialize(strItemName, ePanelType, nInitialIndexValue, strDescriptionjp, strDescriptionjp, argItemList);
	}
	public void tInitialize(string strItemName, CItemBase.EPanelType ePanelType, int nInitialIndexValue, string strDescriptionjp, string strDescriptionen, params string[] argItemList) {
		base.tInitialize(strItemName, ePanelType, strDescriptionjp, strDescriptionen);
		this.nCurrentSelectedItemNumber = nInitialIndexValue;
		foreach (string str in argItemList) {
			this.listItemValue.Add(str);
		}
	}
	public override object objCurrentValue() {
		return this.listItemValue[nCurrentSelectedItemNumber];
	}
	public override int GetIndex() {
		return nCurrentSelectedItemNumber;
	}
	public override void SetIndex(int index) {
		nCurrentSelectedItemNumber = index;
	}
}




/// <summary>
/// 簡易コンフィグの「切り替え」に使用する、「リスト」（複数の固定値からの１つを選択可能）を表すアイテム。
/// e種別が違うのと、tEnter押下()で何もしない以外は、「リスト」そのまま。
/// </summary>
internal class CSwitchItemList : CItemList {
	// Constructor

	public CSwitchItemList() {
		base.eType = CItemBase.EType.SwitchList;
		this.nCurrentSelectedItemNumber = 0;
		this.listItemValue = new List<string>();
	}
	public CSwitchItemList(string strItemName)
		: this() {
		this.tInitialize(strItemName);
	}
	public CSwitchItemList(string strItemName, CItemBase.EPanelType ePanelType)
		: this() {
		this.tInitialize(strItemName, ePanelType);
	}
	public CSwitchItemList(string strItemName, CItemBase.EPanelType ePanelType, int nInitialIndexValue, params string[] argItemList)
		: this() {
		this.tInitialize(strItemName, ePanelType, nInitialIndexValue, argItemList);
	}
	public CSwitchItemList(string strItemName, CItemBase.EPanelType ePanelType, int nInitialIndexValue, string strDescriptionjp, params string[] argItemList)
		: this() {
		this.tInitialize(strItemName, ePanelType, nInitialIndexValue, strDescriptionjp, argItemList);
	}
	public CSwitchItemList(string strItemName, CItemBase.EPanelType ePanelType, int nInitialIndexValue, string strDescriptionjp, string strDescriptionen, params string[] argItemList)
		: this() {
		this.tInitialize(strItemName, ePanelType, nInitialIndexValue, strDescriptionjp, strDescriptionen, argItemList);
	}

	public override void tEnterPressed() {
		// this.t項目値を次へ移動();	// 何もしない
	}
}
