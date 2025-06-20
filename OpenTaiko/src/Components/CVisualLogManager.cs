using System.Diagnostics;
using FDK;

namespace OpenTaiko;

class CVisualLogManager {
	class LogCard {
		public LogCard(TraceEventType type, string message) {
			lct = type;
			msg = message;
			InitTimeSinceCreation();
		}

		private void InitTimeSinceCreation()
			=> timeSinceCreation = new CCounter(0, 10000, 1, OpenTaiko.Timer);

		public int Display(int y) {
			if (timeSinceCreation.IsStoped) {
				// OpenTaiko.Timer was null. Reinitialize.
				InitTimeSinceCreation();
			}
			timeSinceCreation.Tick();

			if (y >= GameWindowSize.Height)
				return y; // exceeded screen; skip drawing

			// Display stuff here
			if (OpenTaiko.actTextConsole != null) {
				y = OpenTaiko.actTextConsole.Print(0, y, CTextConsole.EFontType.Cyan, msg).y;
				y += OpenTaiko.actTextConsole.fontHeight + 24;
			}
			return y;
		}

		public bool IsExpired() {
			return timeSinceCreation.IsEnded;
		}

		private CCounter timeSinceCreation;
		private TraceEventType lct;
		private string msg;
	}

	public void PushCard(TraceEventType lct, string msg) {
		cards.Enqueue(new LogCard(lct, msg));
	}

	public void Display() {
		if (this.firstCard?.IsExpired() ?? true) {
			while (this.cards.TryDequeue(out this.firstCard) && this.firstCard.IsExpired())
				;
		}
		int y = 0;
		if (this.firstCard != null)
			y = this.firstCard.Display(y);
		try {
			foreach (var card in this.cards)
				y = card.Display(y);
		} catch (InvalidOperationException ex) {
			// item updated during drawing; skip
		}
	}

	private readonly Queue<LogCard> cards = new();
	private LogCard? firstCard = null;
}
