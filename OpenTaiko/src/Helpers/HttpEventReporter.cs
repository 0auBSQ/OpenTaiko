namespace OpenTaiko;

/// <summary>
/// Reports game events over HTTP in real time.
/// </summary>
internal class HttpEventReporter(string host, int port) {
	public string host { get; private set; } = host;
	public int port { get; private set; } = port;

    public bool started { get; private set; } = false;
    public bool connected { get; private set; } = false;

    public void StartListening() {
        // TODO: start an HTTP server. Call OnConnection once connected.
        this.started = true;
    }

    public void ReportNoteJudgement(ENoteJudge noteJudge) {
        if (!this.connected) { return; }
        // TODO: send a Server Sent Event. The event should be a JSON object.
    }

    public void ReportGameplayStart() {
        if (!this.connected) { return; }
        // TODO: send a Server Sent Event. The event should be a JSON object.
    }

    private void OnConnection() {
        this.connected = true;
    }
}
