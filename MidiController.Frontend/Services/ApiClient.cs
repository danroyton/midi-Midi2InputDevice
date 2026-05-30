using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace MidiControllerFrontend.Services;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public sealed record ProfileSummary(string ProfileId, string? Name);

public sealed record StatusResponse(
    string Status,
    int    GateValue,
    string GateState,
    DateTimeOffset Timestamp,
    double? AvgLatencyMs);

public sealed record DeviceInfo(string DeviceId, bool IsConnected);



// ── ApiClient ─────────────────────────────────────────────────────────────────

/// <summary>
/// Thin HTTP-Wrapper für alle Backend-REST-Aufrufe.
/// Alle Methoden fangen HttpRequestException ab und geben null / leere Listen zurück
/// damit ViewModels keinen Try/Catch brauchen.
/// </summary>
public sealed class ApiClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json =
        new(JsonSerializerDefaults.Web);

    public ApiClient(HttpClient http) => _http = http;

    // ── Status ───────────────────────────────────────────────────────────────

    public async Task<StatusResponse?> GetStatusAsync(CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<StatusResponse>("/api/status/", _json, ct); }
        catch { return null; }
    }

    public async Task<Dictionary<char, int>?> GetVariablesAsync(CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<Dictionary<char, int>>("/api/status/variables", _json, ct); }
        catch { return null; }
    }

    public async Task<bool> SetVariableAsync(char variable, int value, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.PutAsJsonAsync($"/api/status/variables/{variable}", new { value }, ct);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // ── Profile ──────────────────────────────────────────────────────────────

    public async Task<List<string>> ListProfileIdsAsync(CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<List<string>>("/api/profiles", _json, ct) ?? []; }
        catch { return []; }
    }

    public async Task<bool> CreateProfileAsync(string id, CancellationToken ct = default)
    {
        try
        {
            var profile = new { profileId = id, name = id, mappings = Array.Empty<object>() };
            var resp = await _http.PostAsJsonAsync("/api/profiles", profile, ct);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> ActivateProfileAsync(string id, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.PostAsync($"/api/profiles/{Uri.EscapeDataString(id)}/activate", null, ct);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // ── Devices ──────────────────────────────────────────────────────────────

    public async Task<List<DeviceInfo>> ListDevicesAsync(CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<List<DeviceInfo>>("/api/devices", _json, ct) ?? []; }
        catch { return []; }
    }

    public async Task<bool> ConnectDeviceAsync(string deviceId, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.PostAsync($"/api/devices/{Uri.EscapeDataString(deviceId)}/connect", null, ct);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> DisconnectDeviceAsync(string deviceId, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.PostAsync($"/api/devices/{Uri.EscapeDataString(deviceId)}/disconnect", null, ct);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // ── Templates ────────────────────────────────────────────────────────────

    public async Task<List<string>> ListTemplateNamesAsync(CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<List<string>>("/api/templates", _json, ct) ?? []; }
        catch { return []; }
    }

    public async Task<bool> DeleteTemplateAsync(string name, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.DeleteAsync($"/api/templates/{Uri.EscapeDataString(name)}", ct);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
