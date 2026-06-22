using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using Xunit.Abstractions;
using Xunit.Sdk;

// Generate the grouped HTML test report FROM INSIDE the test run — no separate project, no MSBuild
// step, no .trx needed. xunit lets an assembly nominate its own test framework; ours wraps the
// execution message sink, records every test result as it happens, and writes the report when the
// assembly finishes. Pure C#, runs identically on Windows / Linux / macOS.
[assembly: Xunit.TestFramework("OpenTaikoTests.ReportingTestFramework", "OpenTaiko.Tests")]

namespace OpenTaikoTests {
	public sealed class ReportingTestFramework : XunitTestFramework {
		public ReportingTestFramework(IMessageSink sink) : base(sink) { }
		protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
			=> new ReportingExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
	}

	public sealed class ReportingExecutor : XunitTestFrameworkExecutor {
		public ReportingExecutor(AssemblyName assemblyName, ISourceInformationProvider sip, IMessageSink dms)
			: base(assemblyName, sip, dms) { }
		protected override void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink sink, ITestFrameworkExecutionOptions options)
			=> base.RunTestCases(testCases, new ReportingSink(sink), options);
	}

	// Wraps the real execution sink: forwards every message untouched (so VSTest still reports), while
	// collecting results and writing the HTML report on ITestAssemblyFinished.
	internal sealed class ReportingSink : Xunit.LongLivedMarshalByRefObject, IMessageSink {
		private readonly IMessageSink _inner;
		private readonly object _lock = new();
		private readonly List<Rec> _recs = new();
		private readonly DateTime _start = DateTime.Now;

		public ReportingSink(IMessageSink inner) { _inner = inner; }

		private struct Rec { public string Cls, Name, Outcome, Msg, Stack; public double Ms; }

		public bool OnMessage(IMessageSinkMessage message) {
			try {
				if (message is ITestPassed p) Add(p, "Passed", null, null);
				else if (message is ITestFailed f) Add(f, "Failed", string.Join("\n", f.Messages ?? new string[0]), string.Join("\n", f.StackTraces ?? new string[0]));
				else if (message is ITestSkipped s) Add(s, "Skipped", s.Reason, null);
				else if (message is ITestAssemblyFinished) Write();
			} catch { /* reporting must never break a test run */ }
			return _inner.OnMessage(message);
		}

		private void Add(ITestResultMessage m, string outcome, string msg, string stack) {
			string cls = "(unknown)";
			try { cls = m.TestCase.TestMethod.TestClass.Class.Name; } catch { }
			string name = m.Test.DisplayName ?? "";
			string prefix = cls + ".";
			if (name.StartsWith(prefix)) name = name.Substring(prefix.Length);
			lock (_lock) _recs.Add(new Rec { Cls = cls, Name = name, Outcome = outcome, Msg = msg ?? "", Stack = stack ?? "", Ms = (double)m.ExecutionTime * 1000.0 });
		}

		// <proj>/bin/<cfg>/net8.0/OpenTaiko.Tests.dll → <proj>/TestResults
		private static string OutputDir() {
			try { return Path.Combine(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..")), "TestResults"); }
			catch { return AppContext.BaseDirectory; }
		}

		private void Write() {
			var finish = DateTime.Now;
			List<Rec> recs;
			lock (_lock) recs = new List<Rec>(_recs);
			// dated filename so every run is kept (sortable): OpenTaiko.Tests_2026-06-14_00-37-12.html
			string file = "OpenTaiko.Tests_" + _start.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture) + ".html";
			string path = Path.Combine(OutputDir(), file);
			try { TestReportHtml.Write(path, recs.Select(r => (r.Cls, r.Name, r.Outcome, r.Ms, r.Msg, r.Stack)), _start, finish); }
			catch { }
		}
	}

	// Builds the grouped-by-class HTML (same look the project has always used).
	internal static class TestReportHtml {
		private static string Enc(string v) => WebUtility.HtmlEncode(v ?? "");

		public static void Write(string htmlPath, IEnumerable<(string Cls, string Name, string Outcome, double Ms, string Msg, string Stack)> records,
			DateTime start, DateTime finish) {
			var inv = CultureInfo.InvariantCulture;
			var groups = new Dictionary<string, List<(string Name, string Outcome, double Ms, string Msg, string Stack)>>();
			int total = 0, passed = 0, failed = 0, skipped = 0;
			foreach (var r in records) {
				total++;
				if (r.Outcome == "Passed") passed++; else if (r.Outcome == "Failed") failed++; else skipped++;
				if (!groups.ContainsKey(r.Cls)) groups[r.Cls] = new List<(string, string, double, string, string)>();
				groups[r.Cls].Add((r.Name, r.Outcome, r.Ms, r.Msg, r.Stack));
			}

			string titleStamp = start.ToString("yyyy-MM-dd HH:mm", inv);
			string execText = start.ToString("dddd, dd MMM yyyy 'at' HH:mm:ss", inv);
			double dur = (finish - start).TotalSeconds;
			if (dur >= 0) execText += string.Format(inv, " &middot; ran in {0:N1}s", dur);
			int pct = total > 0 ? (int)Math.Round(100.0 * passed / total) : 0;

			var sb = new StringBuilder();
			sb.Append("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\"><title>OpenTaiko.Tests &mdash; " + titleStamp + "</title><style>" + Css + "</style></head><body>");
			sb.Append("<h1>OpenTaiko.Tests &mdash; results by category</h1>");
			sb.Append("<div class='stamp'>Executed <b>" + execText + "</b> (local time)</div>");
			sb.Append("<div class='summary'>");
			sb.Append("<div class='pill'><b>" + total + "</b>total</div>");
			sb.Append("<div class='pill pass'><b>" + passed + "</b>passed</div>");
			sb.Append("<div class='pill fail'><b>" + failed + "</b>failed</div>");
			sb.Append("<div class='pill skip'><b>" + skipped + "</b>skipped</div>");
			sb.Append("<div class='pill'><b>" + pct + "%</b>pass rate</div></div>");

			foreach (var cls in groups.Keys.OrderBy(k => k)) {
				var rows = groups[cls];
				int gFail = rows.Count(x => x.Outcome == "Failed");
				int gTot = rows.Count;
				int dot = cls.LastIndexOf('.');
				string shortName = dot >= 0 ? cls.Substring(dot + 1) : cls;
				string file = shortName + ".cs";
				string openAttr = gFail > 0 ? " open" : "";
				string badgeCls = gFail > 0 ? "bad" : "ok";
				string badgeTxt = gFail > 0 ? (gFail + " / " + gTot + " failed") : (gTot + " passed");
				sb.Append("<details class='cat'" + openAttr + "><summary>" + Enc(shortName) + " <span class='badge " + badgeCls + "'>" + badgeTxt + "</span> <span class='file'>" + Enc(file) + "</span></summary><table>");
				foreach (var t in rows.OrderBy(x => x.Name)) {
					bool isFail = t.Outcome == "Failed";
					string rowCls = isFail ? " class='f'" : "";
					string ico = isFail ? "<span class='ico-bad'>&#10008;</span>" : (t.Outcome == "Passed" ? "<span class='ico-ok'>&#10004;</span>" : "<span class='ico-skip'>&#9679;</span>");
					string durTxt = string.Format(inv, "{0:N0} ms", t.Ms);
					sb.Append("<tr" + rowCls + "><td class='st'>" + ico + "</td><td>" + Enc(t.Name) + "</td><td class='dur'>" + durTxt + "</td></tr>");
					if (isFail) sb.Append("<tr" + rowCls + "><td></td><td colspan='2'><div class='err'>" + Enc(t.Msg) + "<pre>" + Enc(t.Stack) + "</pre></div></td></tr>");
				}
				sb.Append("</table></details>");
			}
			sb.Append("</body></html>");

			string dir = Path.GetDirectoryName(htmlPath);
			if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
			File.WriteAllText(htmlPath, sb.ToString(), new UTF8Encoding(false));
			Console.WriteLine("HTML test report: " + htmlPath + "  (" + passed + "/" + total + " passed across " + groups.Count + " categories)");
		}

		private const string Css = @"
:root{--ok:#1faa59;--bad:#e23b3b;--skip:#c79b1e;--bg:#fafafa;--card:#fff;--line:#e2e2e2;--mut:#666}
body{font-family:Segoe UI,Calibri,Arial,sans-serif;background:var(--bg);color:#1a1a1a;margin:0;padding:24px}
h1{font-size:20px;margin:0 0 4px}
.stamp{color:var(--mut);font-size:13px;margin-bottom:16px} .stamp b{color:#1a1a1a;font-weight:600}
.summary{display:flex;gap:10px;flex-wrap:wrap;margin-bottom:20px}
.pill{border-radius:8px;padding:8px 14px;font-size:13px;background:var(--card);border:1px solid var(--line)}
.pill b{font-size:16px;display:block}
.pass b{color:var(--ok)} .fail b{color:var(--bad)} .skip b{color:var(--skip)}
details.cat{background:var(--card);border:1px solid var(--line);border-radius:10px;margin:0 0 12px;overflow:hidden}
details.cat>summary{cursor:pointer;padding:12px 16px;font-weight:600;font-size:15px;list-style:none;display:flex;align-items:center;gap:10px}
details.cat>summary::-webkit-details-marker{display:none}
.badge{font-size:12px;font-weight:600;border-radius:6px;padding:2px 8px}
.badge.ok{background:#e7f6ed;color:var(--ok)} .badge.bad{background:#fdeaea;color:var(--bad)}
.file{color:var(--mut);font-weight:400;font-size:12px}
table{width:100%;border-collapse:collapse;font-size:13px}
td{padding:7px 16px;border-top:1px solid var(--line)}
td.st{width:28px;text-align:center} td.dur{width:90px;text-align:right;color:var(--mut);font-variant-numeric:tabular-nums}
tr.f td{background:#fff7f7}
.err{margin:4px 16px 10px;background:#fff0f0;border:1px solid #f3caca;border-radius:6px;padding:8px 10px;font-size:12px}
.err pre{white-space:pre-wrap;margin:6px 0 0;color:#7a1f1f}
.ico-ok{color:var(--ok)} .ico-bad{color:var(--bad)} .ico-skip{color:var(--skip)}";
	}
}