using System.Diagnostics;

namespace FDK {
	public class CTraceLogListener : TraceListener {
		public CTraceLogListener(StreamWriter stream) {
			this.LogStreamWriter = stream;
		}

		public override void Flush() {
			if (this.LogStreamWriter != null) {
				try {
					this.LogStreamWriter.Flush();
				} catch (ObjectDisposedException) {
				}
			}
		}
		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message) {
			if (this.LogStreamWriter != null) {
				try {
					this.LogEventType(eventType);
					this.LogIndent();
					this.LogStreamWriter.WriteLine(message);
				} catch (ObjectDisposedException) {
				}
			}
		}
		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args) {
			if (this.LogStreamWriter != null) {
				try {
					this.LogEventType(eventType);
					this.LogIndent();
					this.LogStreamWriter.WriteLine(string.Format(format, args));
				} catch (ObjectDisposedException) {
				}
			}
		}
		public override void Write(string message) {
			if (this.LogStreamWriter != null) {
				try {
					this.LogStreamWriter.Write(message);
				} catch (ObjectDisposedException) {
				}
			}
		}
		public override void WriteLine(string message) {
			if (this.LogStreamWriter != null) {
				try {
					this.LogStreamWriter.WriteLine(message);
				} catch (ObjectDisposedException) {
				}
			}
		}

		protected override void Dispose(bool disposing) {
			if (this.LogStreamWriter != null) {
				try {
					this.LogStreamWriter.Close();
				} catch {
				}
				this.LogStreamWriter = null;
			}
			base.Dispose(disposing);
		}

		#region [ private ]
		//-----------------
		private StreamWriter LogStreamWriter;

		private void LogEventType(TraceEventType eventType) {
			if (this.LogStreamWriter != null) {
				try {
					var now = DateTime.Now;
					this.LogStreamWriter.Write(string.Format("{0:D4}/{1:D2}/{2:D2} {3:D2}:{4:D2}:{5:D2}.{6:D3} ", new object[] { now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Millisecond }));
					switch (eventType) {
						case TraceEventType.Error:
							this.LogStreamWriter.Write("[ERROR] ");
							return;

						case (TraceEventType.Error | TraceEventType.Critical):
							return;

						case TraceEventType.Warning:
							this.LogStreamWriter.Write("[WARNING] ");
							return;

						case TraceEventType.Information:
							break;

						default:
							return;
					}
					this.LogStreamWriter.Write("[INFO] ");
				} catch (ObjectDisposedException) {
				}
			}
		}
		private void LogIndent() {
			if ((this.LogStreamWriter != null) && (base.IndentLevel > 0)) {
				try {
					for (int i = 0; i < base.IndentLevel; i++)
						this.LogStreamWriter.Write("    ");
				} catch (ObjectDisposedException) {
				}
			}
		}
		//-----------------
		#endregion
	}
}
