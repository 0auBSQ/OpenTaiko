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

		public void Display(int screenPosition) {
			if (timeSinceCreation.IsStoped) {
				// OpenTaiko.Timer was null. Reinitialize.
				InitTimeSinceCreation();
			}
			timeSinceCreation.Tick();

			// Display stuff here

			int x = 0;
			int y = 0 + (40 * screenPosition);

			OpenTaiko.actTextConsole?.Print(x, y, CTextConsole.EFontType.Cyan, msg);
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
		foreach (var (card, i) in this.cards.Select((x, i) => (x, i)))
			card.Display(i);
	}

	private readonly Queue<LogCard> cards = new Queue<LogCard>();
}
