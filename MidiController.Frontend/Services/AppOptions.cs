namespace MidiControllerFrontend.Services;

public sealed class BackendOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5000";
    public int SignalRReconnectIntervalMs { get; set; } = 3000;
}

public sealed class UiOptions
{
    public int LogMaxLines { get; set; } = 1000;
    public string Theme { get; set; } = "Dark";
}
