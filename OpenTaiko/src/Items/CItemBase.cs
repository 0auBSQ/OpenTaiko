using System.Globalization;

namespace OpenTaiko;

/// <summary>
/// すべてのアイテムの基本クラス。
/// </summary>
internal class CItemBase {
	// Properties

	public EPanelType ePanelType;
	public enum EPanelType {
		Normal,
		Other
	}

	public EType eType;
	public enum EType {
		BasicForm,
		ONorOFFToggle,
		ONorOFForIndeterminateThreeState,
		Int,
		List,
		SwitchList
	}

	public string strItemName;
	public string strName {
		get {
			return strItemName;

		}
	}

	public string strDescriptionText;
	public string strDescription {
		get {
			return strDescriptionText;

		}
	}


	// Constructor

	public CItemBase() {
		this.strItemName = "";
		this.strDescriptionText = "";
	}
	public CItemBase(string strItemName)
		: this() {
		this.tInitialize(strItemName);
	}
	public CItemBase(string strItemName, string strDescriptionjp)
		: this() {
		this.tInitialize(strItemName, strDescriptionjp);
	}
	public CItemBase(string strItemName, string strDescriptionjp, string strDescriptionen)
		: this() {
		this.tInitialize(strItemName, strDescriptionjp, strDescriptionen);
	}

	public CItemBase(string strItemName, EPanelType ePanelType)
		: this() {
		this.tInitialize(strItemName, ePanelType);
	}
	public CItemBase(string strItemName, EPanelType ePanelType, string strDescriptionjp)
		: this() {
		this.tInitialize(strItemName, ePanelType, strDescriptionjp);
	}
	public CItemBase(string strItemName, EPanelType ePanelType, string strDescriptionjp, string strDescriptionen)
		: this() {
		this.tInitialize(strItemName, ePanelType, strDescriptionjp, strDescriptionen);
	}


	// メソッド；子クラスで実装する

	public virtual void tEnterPressed() {
	}
	public virtual void tItemValueNextMove() {
	}
	public virtual void tItemValuePrevMove() {
	}
	public virtual void tInitialize(string strItemName) {
		this.tInitialize(strItemName, EPanelType.Normal);
	}
	public virtual void tInitialize(string strItemName, string strDescriptionjp) {
		this.tInitialize(strItemName, EPanelType.Normal, strDescriptionjp, strDescriptionjp);
	}
	public virtual void tInitialize(string strItemName, string strDescriptionjp, string strDescriptionen) {
		this.tInitialize(strItemName, EPanelType.Normal, strDescriptionjp, strDescriptionen);
	}

	public virtual void tInitialize(string strItemName, EPanelType ePanelType) {
		this.tInitialize(strItemName, ePanelType, "", "");
	}
	public virtual void tInitialize(string strItemName, EPanelType ePanelType, string strDescriptionjp) {
		this.tInitialize(strItemName, ePanelType, strDescriptionjp, strDescriptionjp);
	}
	public virtual void tInitialize(string strItemName, EPanelType ePanelType, string strDescriptionjp, string strDescriptionen) {
		this.strItemName = strItemName;
		this.ePanelType = ePanelType;
		this.strDescriptionText = (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja") ? strDescriptionjp : strDescriptionen;
	}
	public virtual object objCurrentValue() {
		return null;
	}

	public string tGetValueText() {
		object value = objCurrentValue();
		return value == null ? "" : value.ToString();
	}
	public virtual int GetIndex() {
		return 0;
	}
	public virtual void SetIndex(int index) {
	}
}
