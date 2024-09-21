using System.Diagnostics;
using FDK;

namespace OpenTaiko {
	internal class LogNotification {
		private static Queue<CLogNotification> Notifications = new Queue<CLogNotification>();

		public enum ENotificationType {
			EINFO,
			ESUCCESS,
			EWARNING,
			EERROR,
		}

		public class CLogNotification {
			public CLogNotification(ENotificationType nt, string msg) {
				NotificationType = nt;
				Message = msg;
			}

			public ENotificationType NotificationType = ENotificationType.EINFO;
			public string Message = "";
			public CCounter LifeTime = new CCounter(0, 1000, 1, OpenTaiko.Timer);
		}


		public static void PopError(string message) {
			Notifications.Enqueue(new CLogNotification(ENotificationType.EERROR, message));
			Trace.TraceError("<Runtime Error>: " + message);
		}

		public static void PopWarning(string message) {
			Notifications.Enqueue(new CLogNotification(ENotificationType.EWARNING, message));
			Trace.TraceWarning("<Runtime Warning>: " + message);
		}

		public static void PopSuccess(string message) {
			Notifications.Enqueue(new CLogNotification(ENotificationType.ESUCCESS, message));
			Trace.TraceInformation("<Runtime Success>: " + message);
		}

		public static void PopInfo(string message) {
			Notifications.Enqueue(new CLogNotification(ENotificationType.EINFO, message));
			Trace.TraceInformation("<Runtime Info>: " + message);
		}

		public static void Display() {
			while (Notifications.Count > 0 && Notifications.Peek().LifeTime.IsEnded) Notifications.Dequeue();
			// Add an optimized method to display the notifications here
		}
	}
}
