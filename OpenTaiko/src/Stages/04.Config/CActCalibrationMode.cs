using System.Drawing;
using FDK;

namespace OpenTaiko {
	internal class CActCalibrationMode : CActivity {
		public CActCalibrationMode() { }

		public override void Activate() {
			//hitSound = TJAPlayer3.SoundManager.tCreateSound($@"Global{Path.DirectorySeparatorChar}HitSounds{Path.DirectorySeparatorChar}" + TJAPlayer3.Skin.hsHitSoundsInformations.don[0], ESoundGroup.SoundEffect);
			font = HPrivateFastFont.tInstantiateMainFont(30);
			base.Activate();
		}

		public override void DeActivate() {
			Stop();
			Offsets.Clear();
			font?.Dispose();
			offsettext?.Dispose();
			//hitSound?.tDispose();

			base.DeActivate();
		}

		public void Start() {
			CalibrateTick = new CCounter(0, 500, 1, OpenTaiko.Timer);
			UpdateText();
		}

		public void Stop() {
			CalibrateTick = new CCounter();
			Offsets.Clear();
			LastOffset = 0;
			buttonIndex = 1;
		}

		public int Update() {
			if (IsDeActivated || CalibrateTick.IsStoped)
				return 1;

			CalibrateTick.Tick();

			bool decide = OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.Decide) ||
							OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RRed) ||
							OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LRed) ||
				OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return);

			if (CalibrateTick.IsEnded) {
				OpenTaiko.Skin.calibrationTick.tPlay();
				CalibrateTick.Start(0, 500, 1, OpenTaiko.Timer);
			}

			if (OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LeftChange) ||
				OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LBlue) ||
				OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.LeftArrow)) {
				buttonIndex = Math.Max(buttonIndex - 1, 0);
				OpenTaiko.Skin.soundChangeSFX.tPlay();
			} else if (OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RightChange) ||
				  OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RBlue) ||
				  OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.RightArrow)) {
				buttonIndex = Math.Min(buttonIndex + 1, 2);
				OpenTaiko.Skin.soundChangeSFX.tPlay();
			} else if (buttonIndex == 0 && decide) // Cancel
			  {
				OpenTaiko.Skin.soundCancelSFX.tPlay();
				Stop();
			} else if (buttonIndex == 1 && decide) // Hit!
			  {
				//hitSound?.PlayStart();
				AddOffset();
				UpdateText();
			} else if (buttonIndex == 2 && decide) // Save
			  {
				OpenTaiko.ConfigIni.nGlobalOffsetMs = GetMedianOffset();
				OpenTaiko.stageコンフィグ.actList.iGlobalOffsetMs.n現在の値 = GetMedianOffset();
				OpenTaiko.Skin.soundDecideSFX.tPlay();
				Stop();

				return 0;
			} else if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.System.Cancel) ||
				  OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape)) {
				OpenTaiko.Skin.soundCancelSFX.tPlay();
				Stop();

				return 0;
			}

			return 0;
		}

		public override int Draw() {
			if (IsDeActivated || CalibrateTick.IsStoped)
				return 1;

			if (OpenTaiko.Tx.Tile_Black != null) {
				OpenTaiko.Tx.Tile_Black.Opacity = 128;
				for (int i = 0; i <= SampleFramework.GameWindowSize.Width; i += OpenTaiko.Tx.Tile_Black.szTextureSize.Width) {
					for (int j = 0; j <= SampleFramework.GameWindowSize.Height; j += OpenTaiko.Tx.Tile_Black.szTextureSize.Height) {
						OpenTaiko.Tx.Tile_Black.t2D描画(i, j);
					}
				}
				OpenTaiko.Tx.Tile_Black.Opacity = 255;
			}

			OpenTaiko.Tx.CalibrateBG?.t2D描画(OpenTaiko.Skin.Config_Calibration_Highlights[buttonIndex].X,
				OpenTaiko.Skin.Config_Calibration_Highlights[buttonIndex].Y,
				OpenTaiko.Skin.Config_Calibration_Highlights[buttonIndex]);
			OpenTaiko.Tx.CalibrateFG?.t2D描画(0, 0);

			OpenTaiko.Tx.Lane_Background_Main?.t2D描画(OpenTaiko.Skin.Game_Lane_X[0], OpenTaiko.Skin.Game_Lane_Y[0]);
			OpenTaiko.Tx.Lane_Background_Sub?.t2D描画(OpenTaiko.Skin.Game_Lane_Sub_X[0], OpenTaiko.Skin.Game_Lane_Sub_Y[0]);
			OpenTaiko.Tx.Taiko_Frame[2]?.t2D描画(OpenTaiko.Skin.Game_Taiko_Frame_X[0], OpenTaiko.Skin.Game_Taiko_Frame_Y[0]);

			OpenTaiko.Tx.Notes[0]?.t2D描画(OpenTaiko.Skin.nScrollFieldX[0], OpenTaiko.Skin.nScrollFieldY[0], new RectangleF(0, 0, OpenTaiko.Skin.Game_Notes_Size[0], OpenTaiko.Skin.Game_Notes_Size[1]));

			for (int x = OpenTaiko.Skin.nScrollFieldX[0]; x < SampleFramework.GameWindowSize.Width + 500; x += 500) {
				OpenTaiko.Tx.Bar?.t2D描画(
					(x - CalibrateTick.CurrentValue) + ((OpenTaiko.Skin.Game_Notes_Size[0] - OpenTaiko.Tx.Bar.szTextureSize.Width) / 2),
					OpenTaiko.Skin.nScrollFieldY[0],
					new Rectangle(0, 0, OpenTaiko.Tx.Bar.szTextureSize.Width, OpenTaiko.Skin.Game_Notes_Size[1])
					);
				OpenTaiko.Tx.Notes[0]?.t2D描画(
					(x - CalibrateTick.CurrentValue),
					OpenTaiko.Skin.nScrollFieldY[0],
					new Rectangle(OpenTaiko.Skin.Game_Notes_Size[0], OpenTaiko.Skin.Game_Notes_Size[1], OpenTaiko.Skin.Game_Notes_Size[0], OpenTaiko.Skin.Game_Notes_Size[1])
					);
			}

			if (OpenTaiko.P1IsBlue())
				OpenTaiko.Tx.Taiko_Background[4]?.t2D描画(OpenTaiko.Skin.Game_Taiko_Background_X[0], OpenTaiko.Skin.Game_Taiko_Background_Y[0]);
			else
				OpenTaiko.Tx.Taiko_Background[0]?.t2D描画(OpenTaiko.Skin.Game_Taiko_Background_X[0], OpenTaiko.Skin.Game_Taiko_Background_Y[0]);

			#region Calibration Info

			offsettext?.t2D描画(OpenTaiko.Skin.Config_Calibration_OffsetText[0] - offsettext.szTextureSize.Width, OpenTaiko.Skin.Config_Calibration_OffsetText[1]);

			OpenTaiko.actTextConsole.tPrint(OpenTaiko.Skin.Config_Calibration_InfoText[0], OpenTaiko.Skin.Config_Calibration_InfoText[1], CTextConsole.EFontType.Cyan,
				"MEDIAN OFFSET : " + GetMedianOffset() + "ms\n");
			OpenTaiko.actTextConsole.tPrint(OpenTaiko.Skin.Config_Calibration_InfoText[0], OpenTaiko.Skin.Config_Calibration_InfoText[1] + OpenTaiko.actTextConsole.nFontHeight, CTextConsole.EFontType.White,
				"MIN OFFSET    : " + GetLowestOffset() + "ms\n" +
				"MAX OFFSET    : " + GetHighestOffset() + "ms\n" +
				"LAST OFFSET   : " + LastOffset + "ms\n" +
				"OFFSET COUNT  : " + (Offsets != null ? Offsets.Count : 0));
			OpenTaiko.actTextConsole.tPrint(OpenTaiko.Skin.Config_Calibration_InfoText[0], OpenTaiko.Skin.Config_Calibration_InfoText[1] + (OpenTaiko.actTextConsole.nFontHeight * 5), CTextConsole.EFontType.White,
				"CURRENT OFFSET: " + CurrentOffset() + "ms");

			#endregion

			return 0;
		}

		public void AddOffset() { Offsets.Add(CurrentOffset()); LastOffset = CurrentOffset(); }

		public int GetMedianOffset() {
			if (Offsets != null)
				if (Offsets.Count > 0) {
					Offsets.Sort();
					return Offsets[Offsets.Count / 2];
				}
			return 0;
		}
		public int GetLowestOffset() {
			if (Offsets != null)
				return Offsets.Count > 0 ? Offsets.Min() : 0;
			return 0;
		}
		public int GetHighestOffset() {
			if (Offsets != null)
				return Offsets.Count > 0 ? Offsets.Max() : 0;
			return 0;
		}
		public int CurrentOffset() {
			return CalibrateTick.CurrentValue > 250 ? CalibrateTick.CurrentValue - 500 : CalibrateTick.CurrentValue;
		}

		private void UpdateText() {
			offsettext?.Dispose();
			offsettext = new CTexture(font.DrawText(CLangManager.LangInstance.GetString("SETTINGS_GAME_CALIBRATION_OFFSET", GetMedianOffset().ToString()), Color.White, Color.Black, null, 32));
		}

		public bool IsStarted { get { return CalibrateTick.IsStarted; } }
		#region Private
		private CCounter CalibrateTick = new CCounter();
		private List<int> Offsets = new List<int>();
		private int LastOffset = 0;
		private CCachedFontRenderer font;
		private CTexture offsettext;

		//private CSound hitSound;

		private int buttonIndex = 1;
		private Rectangle[] BGs = new Rectangle[3]
		{
			new Rectangle(371, 724, 371, 209),
			new Rectangle(774, 724, 371, 209),
			new Rectangle(1179, 724, 371, 209)
		};
		#endregion
	}
}
