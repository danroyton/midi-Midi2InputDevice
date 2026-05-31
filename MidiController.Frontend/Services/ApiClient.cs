using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

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

// ── Trigger DTOs ──────────────────────────────────────────────────────────────

public sealed record TriggerDto(
    string  TriggerId,
    string  DeviceId,
    string  EventType,
    int     Channel,
    int?    Data1Filter,
    string  MatchMode,   // Variable | Data2 | DeltaData2 | DD2Positive | DD2Negative
    int     MatchValue,
    StateAssignmentDto[]  GlobalPreAssignments,
    ConditionBlockDto[]   ConditionBlocks,
    ActionBlockDto[]      Actions,
    StateAssignmentDto[]  GlobalPostAssignments,
    TriggerConfigDto?     ElseConfig
);

public sealed record TriggerConfigDto(
    ConditionBlockDto[]  ConditionBlocks,
    ActionBlockDto[]     Actions,
    StateAssignmentDto[] GlobalPostAssignments
);

public sealed record ConditionBlockDto(
    string?        TemplateName,
    ConditionDto[] Conditions
);

public sealed record ConditionDto(
    string Left,
    string Op,
    string RightSource,
    int    RightFixed
);

public sealed record ActionBlockDto(
    string?              TemplateName,
    string[]             KeyCombination,
    string               XSource, int XFixed,
    string               YSource, int YFixed,
    string               ZSource, int ZFixed,
    StateAssignmentDto[] StateAssignments
);

public sealed record StateAssignmentDto(
    char   Variable,
    string Source,
    int    FixedValue
);


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

    public async Task<bool> DeleteProfileAsync(string id, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.DeleteAsync($"/api/profiles/{Uri.EscapeDataString(id)}", ct);
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

    // ── Triggers ─────────────────────────────────────────────────────────────

    public async Task<List<TriggerDto>> ListTriggersAsync(string profileId, CancellationToken ct = default)
    {
        try { return await _http.GetFromJsonAsync<List<TriggerDto>>($"/api/profiles/{Uri.EscapeDataString(profileId)}/triggers", _json, ct) ?? []; }
        catch { return []; }
    }

    public async Task<TriggerDto?> CreateTriggerAsync(string profileId, TriggerDto trigger, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync($"/api/profiles/{Uri.EscapeDataString(profileId)}/triggers", trigger, ct);
            if (resp.IsSuccessStatusCode)
                return await resp.Content.ReadFromJsonAsync<TriggerDto>(_json, ct);
            return null;
        }
        catch { return null; }
    }

    public async Task<bool> UpdateTriggerAsync(string profileId, TriggerDto trigger, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.PutAsJsonAsync(
                $"/api/profiles/{Uri.EscapeDataString(profileId)}/triggers/{Uri.EscapeDataString(trigger.TriggerId)}",
                trigger, ct);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> DeleteTriggerAsync(string profileId, string triggerId, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.DeleteAsync(
                $"/api/profiles/{Uri.EscapeDataString(profileId)}/triggers/{Uri.EscapeDataString(triggerId)}", ct);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
