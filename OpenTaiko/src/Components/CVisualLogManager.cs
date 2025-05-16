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
		while (this.cards.TryPeek(out var card) && card.IsExpired()) {
			this.cards.Dequeue();
		}
		int y = 0;
		foreach (var card in this.cards)
			y = card.Display(y);
	}

	private readonly Queue<LogCard> cards = new Queue<LogCard>();
}
