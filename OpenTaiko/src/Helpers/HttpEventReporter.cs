using System.Net;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.IO;

namespace OpenTaiko;

/// <summary>
/// Reports game events over HTTP in real time.
/// </summary>
internal class HttpEventReporter(string host, int port) {
    public string host { get; private set; } = host;
    public int port { get; private set; } = port;

    public bool started { get; private set; } = false;

    private HttpListener? _listener;
    private readonly List<HttpListenerResponse> _clients = new();
    private readonly object _lockObj = new();

    public void StartListening() {
        if (this.started) return;
        this.started = true;

        Task.Run(async () => {
            try {
                this._listener = new HttpListener();
                this._listener.Prefixes.Add($"http://{this.host}:{this.port}/");
                this._listener.Start();
                Trace.TraceInformation($"[HttpEventReporter] Listening on http://{this.host}:{this.port}/");

                while (this._listener.IsListening) {
                    HttpListenerContext context = await this._listener.GetContextAsync();
                    this.HandleClient(context);
                }
            } catch (Exception ex) {
                Trace.TraceError($"[HttpEventReporter] Listener error: {ex.Message}");
            }
        });
    }

    private void HandleClient(HttpListenerContext context) {
        HttpListenerResponse response = context.Response;
        response.ContentType = "text/event-stream";
        response.Headers.Add("Cache-Control", "no-cache");
        response.Headers.Add("Connection", "keep-alive");
        response.Headers.Add("Access-Control-Allow-Origin", "*");

        try {
            byte[] init = Encoding.UTF8.GetBytes(": connected\n\n");
            response.OutputStream.Write(init, 0, init.Length);
            response.OutputStream.Flush();
        } catch (Exception ex) {
            Trace.TraceError($"[HttpEventReporter] Failed to send init to client: {ex.Message}");
            response.Close();
            return;
        }

        lock (this._lockObj) {
            this._clients.Add(response);
        }
        Trace.TraceInformation("[HttpEventReporter] Client connected.");
    }

    public void ReportNoteJudgement(ENoteJudge noteJudge, CChip? pChip) {
        EGameType gameType = pChip?.eGameType ?? EGameType.Taiko;
        if (!NotesManager.IsRedOrBlue(pChip, gameType)) { return; }
        this.Broadcast(new {
            type = "judgement",
            judgement = StringForSerailization(noteJudge)
        });
    }

    // Explicitly state known cases so that the event format is stable against future enum changes.
	static string StringForSerailization(ENoteJudge noteJudge) {
		return noteJudge switch {
			ENoteJudge.Perfect => "perfect",
			ENoteJudge.Great => "great",
			ENoteJudge.Good => "good",
			ENoteJudge.Poor => "poor",
			ENoteJudge.Miss => "miss",
			ENoteJudge.Bad => "bad",
			ENoteJudge.Auto => "auto",
			ENoteJudge.ADLIB => "adlib",
			ENoteJudge.Mine => "mine",
			_ => noteJudge.ToString(),
		};
	}

    public void ReportGameplayStart() {
        var tjaSummaries = Enumerable.Range(0, OpenTaiko.ConfigIni.nPlayerCount).Select(i => {
			CTja? tja = OpenTaiko.GetTJA(i);
            if (tja is null) return null;
			string fullPath = tja.strFullPath;
            string tjaContent = File.ReadAllText(fullPath);
            return new {
                player = i,
                tjaContent
            };
        }).Where(n => n is not null);

        this.Broadcast(new {
            type = "gameplay_start",
			tjaSummaries
		});
    }

    private void Broadcast(object data) {
        try {
            string json = JsonSerializer.Serialize(data);
            string eventString = $"data: {json}\n\n";
            byte[] buffer = Encoding.UTF8.GetBytes(eventString);

            lock (this._lockObj) {
                for (int i = this._clients.Count - 1; i >= 0; i--) {
                    try {
                        this._clients[i].OutputStream.Write(buffer, 0, buffer.Length);
                        this._clients[i].OutputStream.Flush();
                    } catch {
                        try { this._clients[i].Close(); } catch { } 
                        this._clients.RemoveAt(i);
                    }
                }
            }
        } catch (Exception ex) {
            Trace.TraceError($"[HttpEventReporter] Broadcast error: {ex.Message}");
        }
    }
}