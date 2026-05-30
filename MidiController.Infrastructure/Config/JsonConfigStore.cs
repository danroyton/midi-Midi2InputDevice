using System.Text.Json;
using MidiController.Domain.Interfaces;
using MidiController.Domain.Models;

namespace MidiController.Infrastructure.Config;

/// <summary>
/// Implementiert <see cref="IConfigStore"/> und <see cref="ITemplateStore"/> via JSON-Dateien.
/// Profile werden unter {basePath}/profiles/{profileId}.json gespeichert,
/// Templates unter {basePath}/templates/{name}.json.
/// </summary>
public sealed class JsonConfigStore : IConfigStore, ITemplateStore
{
    private readonly string _profilesDir;
    private readonly string _templatesDir;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public JsonConfigStore(string basePath)
    {
        _profilesDir  = Path.Combine(basePath, "profiles");
        _templatesDir = Path.Combine(basePath, "templates");

        Directory.CreateDirectory(_profilesDir);
        Directory.CreateDirectory(_templatesDir);
    }

    // ── IConfigStore ─────────────────────────────────────────────────────────

    public Task<IEnumerable<string>> ListProfileIdsAsync(CancellationToken ct = default)
    {
        var ids = Directory.EnumerateFiles(_profilesDir, "*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f));
        return Task.FromResult(ids);
    }

    public async Task<Profile?> LoadProfileAsync(string profileId, CancellationToken ct = default)
    {
        var path = ProfilePath(profileId);
        if (!File.Exists(path))
            return null;

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<Profile>(stream, _jsonOptions, ct);
    }

    public async Task SaveProfileAsync(Profile profile, CancellationToken ct = default)
    {
        var path = ProfilePath(profile.ProfileId);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, profile, _jsonOptions, ct);
    }

    public Task DeleteProfileAsync(string profileId, CancellationToken ct = default)
    {
        var path = ProfilePath(profileId);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    // ── ITemplateStore ────────────────────────────────────────────────────────

    public Task<IEnumerable<string>> ListTemplateNamesAsync(CancellationToken ct = default)
    {
        var names = Directory.EnumerateFiles(_templatesDir, "*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f));
        return Task.FromResult(names);
    }

    public async Task<ConditionBlock?> LoadConditionBlockTemplateAsync(string name, CancellationToken ct = default)
    {
        var path = TemplatePath(name, "cond");
        if (!File.Exists(path))
            return null;

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<ConditionBlock>(stream, _jsonOptions, ct);
    }

    public async Task<ActionBlock?> LoadActionBlockTemplateAsync(string name, CancellationToken ct = default)
    {
        var path = TemplatePath(name, "act");
        if (!File.Exists(path))
            return null;

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<ActionBlock>(stream, _jsonOptions, ct);
    }

    public async Task SaveConditionBlockTemplateAsync(ConditionBlock template, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(template.TemplateName, nameof(template));
        var path = TemplatePath(template.TemplateName, "cond");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, template, _jsonOptions, ct);
    }

    public async Task SaveActionBlockTemplateAsync(ActionBlock template, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(template.TemplateName, nameof(template));
        var path = TemplatePath(template.TemplateName, "act");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, template, _jsonOptions, ct);
    }

    public Task DeleteTemplateAsync(string name, CancellationToken ct = default)
    {
        foreach (var path in new[] { TemplatePath(name, "cond"), TemplatePath(name, "act") })
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        return Task.CompletedTask;
    }

    // ── Hilfsmethoden ─────────────────────────────────────────────────────────

    private string ProfilePath(string profileId) =>
        Path.Combine(_profilesDir, $"{profileId}.json");

    private string TemplatePath(string name, string kind) =>
        Path.Combine(_templatesDir, $"{name}.{kind}.json");
}
