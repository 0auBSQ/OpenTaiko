using System.Drawing;
using FDK;

namespace OpenTaiko;

internal class CSystemError : CStage {

	public bool GameCrashed = false;
	public string ErrorMessage = "";

	private CCachedFontRenderer _pfText = null;
	private TitleTextureKey _ttkText = null;

	public enum Errno {
		ENO_UNKNOWN = 0,
		ENO_NOAUDIODEVICE = 1,
		ENO_SKINNOTFOUND = 2,
		ENO_PADINITFAILED = 3,
		ENO_INPUTINITFAILED = 4,
		ENO_SONGLISTINITFAILED = 5
	};

	public void LoadError(Errno errno) {
		GameCrashed = true;

		// Head with the error code
		ErrorMessage = "\n\nError Code " + ((int)errno).ToString("0000") + "\n\n";
		ErrorMessage += "An error has occured.\n\n";

		switch (errno) {
			default:
			case Errno.ENO_UNKNOWN: {
					ErrorMessage += "Please try restarting OpenTaiko.";
					break;
				}
			case Errno.ENO_NOAUDIODEVICE: {
					ErrorMessage += "No audio device was found.\n";
					ErrorMessage += "Please ensure that you have an active audio output display on your machine.\n";
					ErrorMessage += "Additionally, check if your speakers or headset are not turned off.\n";
					ErrorMessage += "If this does not resolve the issue, please try the troubleshooting feature on your OS.";
					break;
				}
			case Errno.ENO_SKINNOTFOUND: {
					ErrorMessage += "No compatible skin was found.\n";
					ErrorMessage += "Please ensure that you have a compatible skin within your System folder.\n";
					ErrorMessage += "If you did not installed a skin, please do so through the OpenTaiko Hub (Skins tab).\n";
					ErrorMessage += "If this does not resolve the issue, please try updating your skins through the OpenTaiko Hub.\n";
					break;
				}
			case Errno.ENO_PADINITFAILED: {
					ErrorMessage += "The pad initialisation failed.\n";
					ErrorMessage += "Please try the troubleshooting feature on your OS.";
					break;
				}
			case Errno.ENO_INPUTINITFAILED: {
					ErrorMessage += "The input device initialisation failed.\n";
					ErrorMessage += "Please ensure that you are not using a faulty input device when launching the game.\n";
					ErrorMessage += "If the device seems to work on other tasks, please try the troubleshooting feature on your OS.";
					break;
				}
			case Errno.ENO_SONGLISTINITFAILED: {
					ErrorMessage += "The song list initialisation failed.\n";
					ErrorMessage += "Please try removing the songlist.db file within your OpenTaiko folder.";
					break;
				}

		};

		// Append a call to contact if necessary
		ErrorMessage += "\nIf the error persits, please contact us through the links provided on the OpenTaiko Hub.";
	}

	// Constructor

	public CSystemError() {
		base.eStageID = CStage.EStage.CRASH;
		base.ePhaseID = CStage.EPhase.Common_NORMAL;
		base.IsDeActivated = true;
	}

	// CStage Implementation

	public override void Activate() {
		// Allocate the resource exceptionally on Activate as CreateManagedResource is conditionned to PreloadAssets
		this._pfText = HPrivateFastFont.tInstantiateFont("", 20); // Force fallback font
		base.Activate();
	}
	public override void DeActivate() {
		base.DeActivate();
	}
	public override void CreateManagedResource() {
		base.CreateManagedResource();
	}
	public override void ReleaseManagedResource() {
		base.ReleaseManagedResource();
	}
	public override int Draw() {
		if (!base.IsDeActivated) {
			if (this._pfText != null) {
				this._ttkText = new TitleTextureKey(ErrorMessage, this._pfText, Color.White, Color.Black, 1280);
				CTexture tmpTex = TitleTextureKey.ResolveTitleTexture(this._ttkText);
				tmpTex.t2D描画(0, 0);
			}
		}
		return 0;
	}



}
