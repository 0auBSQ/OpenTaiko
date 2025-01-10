using System.Diagnostics;

namespace OpenTaiko;

internal class LogNotification {
	public static void PopError(string message) {
		OpenTaiko.VisualLogManager?.PushCard(TraceEventType.Error, message);
		Trace.TraceError("<Runtime Error>: " + message);
	}

	public static void PopWarning(string message) {
		OpenTaiko.VisualLogManager?.PushCard(TraceEventType.Warning, message);
		Trace.TraceWarning("<Runtime Warning>: " + message);
	}

	public static void PopSuccess(string message) {
		OpenTaiko.VisualLogManager?.PushCard(TraceEventType.Verbose, message);
		Trace.TraceInformation("<Runtime Success>: " + message);
	}

	public static void PopInfo(string message) {
		OpenTaiko.VisualLogManager?.PushCard(TraceEventType.Information, message);
		Trace.TraceInformation("<Runtime Info>: " + message);
	}
}
