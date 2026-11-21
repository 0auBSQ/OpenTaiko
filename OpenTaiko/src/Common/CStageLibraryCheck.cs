using System.Drawing;
using FDK;

namespace OpenTaiko;

// startup prompt, one screen per native library that failed to load
internal class CStageLibraryCheck : CStage {
	public const int Waiting = 0, Start = 1, Quit = 2;

	private const int Margin = 60;
	private const int ChoiceGap = 80;

	private readonly List<NativeLibraryCheck.Failure> _failures;
	private int _shown = -1;
	private bool _yes = true;

	private CCachedFontRenderer? _font;
	private CTexture? _message;
	private readonly CTexture?[] _choices = new CTexture?[4];   // yes, yes selected, no, no selected

	public CStageLibraryCheck(List<NativeLibraryCheck.Failure> failures) {
		base.eStageID = EStage.LibraryCheck;
		base.ePhaseID = EPhase.Common_NORMAL;
		base.IsDeActivated = true;
		_failures = failures;
	}

	public override void Activate() {
		_font = HPrivateFastFont.tInstantiateFont("", 20);   // fallback font: the skin font may not be usable yet
		_choices[0] = Render("  Yes  ", Color.Gray);
		_choices[1] = Render("> Yes <", Color.White);
		_choices[2] = Render("  No  ", Color.Gray);
		_choices[3] = Render("> No <", Color.White);
		_shown = -1;
		_yes = true;
		base.Activate();
	}

	public override void DeActivate() {
		OpenTaiko.tDisposeSafely(ref _message);
		for (int i = 0; i < _choices.Length; i++) OpenTaiko.tDisposeSafely(ref _choices[i]);
		_font?.Dispose();
		_font = null;
		base.DeActivate();
	}

	// returns Waiting, Start or Quit
	public override int Draw() {
		if (base.IsDeActivated || _font == null || _failures.Count == 0) return Start;

		if (_shown < 0) Show(0);

		if (Left()) _yes = true;
		else if (Right()) _yes = false;
		else if (Decide()) {
			if (!_yes) return Quit;
			if (_shown + 1 >= _failures.Count) return Start;
			Show(_shown + 1);
		}

		float scale = 1f;
		if (_message != null) {
			int room = GameWindowSize.Width - 2 * Margin;
			scale = Math.Min(1f, (float)room / Math.Max(1, _message.szImageSize.Width));
			_message.tSetScale(scale, scale);
			_message.t2DDraw(Margin, Margin);
		}

		float y = Margin + (_message?.szImageSize.Height ?? 0) * scale + ChoiceGap / 2f;
		CTexture? yes = _choices[_yes ? 1 : 0], no = _choices[_yes ? 2 : 3];
		float x = Margin;
		if (yes != null) {
			yes.tSetScale(scale, scale);
			yes.t2DDraw(x, y);
			x += yes.szImageSize.Width * scale + ChoiceGap * scale;
		}
		if (no != null) {
			no.tSetScale(scale, scale);
			no.t2DDraw(x, y);
		}
		return Waiting;
	}

	private void Show(int index) {
		_shown = index;
		_yes = true;
		var failure = _failures[index];
		string text = $"{failure.FileName} has failed to load\n"
			+ "(this may be due to a wrong setup or to the library being blocked by an external service like Smart App Control).\n\n"
			+ "Starting OpenTaiko with it disabled will have the following side effects:\n"
			+ string.Concat(failure.SideEffects.Select(e => $"- {e}\n"))
			+ "\nStart OpenTaiko anyways?";
		OpenTaiko.tDisposeSafely(ref _message);
		_message = Render(text, Color.White);
	}

	// base renderer: the cached one keeps the bitmap it returns
	private CTexture? Render(string text, Color color) {
		using var bmp = ((CFontRenderer)_font!).DrawText(text, color, Color.Black, null, 30);
		return OpenTaiko.tTextureCreate(bmp);
	}

	private static bool Left()
		=> OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.LeftArrow)
		|| OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.LeftChange)
		|| OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.LBlue);

	private static bool Right()
		=> OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.RightArrow)
		|| OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.RightChange)
		|| OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.RBlue);

	// same decide inputs as the popup menus
	private static bool Decide()
		=> OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.Decide)
		|| OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.RD)
		|| OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.LC)
		|| OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.LRed)
		|| OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.RRed)
		|| (OpenTaiko.ConfigIni.bEnterIsNotUsedInKeyAssignments && OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return));
}
