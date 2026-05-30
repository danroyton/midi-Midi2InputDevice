using MidiController.Domain.Interfaces;
using MidiController.Domain.Models;
using MidiController.Engine;

namespace MidiController.Host.Api;

internal static class ProfilesApi
{
    /// <summary>
    /// Registriert alle REST-Endpunkte unter /api/.
    /// </summary>
    internal static IEndpointRouteBuilder MapMidiControllerApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        // ── Profile ──────────────────────────────────────────────────────────

        api.MapGet("/profiles", async (IConfigStore store, CancellationToken ct) =>
        {
            var ids = await store.ListProfileIdsAsync(ct);
            return Results.Ok(ids);
        })
        .WithName("ListProfiles")
        .WithSummary("Listet alle gespeicherten Profile auf.");

        api.MapGet("/profiles/{id}", async (string id, IConfigStore store, CancellationToken ct) =>
        {
            var profile = await store.LoadProfileAsync(id, ct);
            return profile is null ? Results.NotFound() : Results.Ok(profile);
        })
        .WithName("GetProfile")
        .WithSummary("Lädt ein Profil anhand seiner ID.");

        api.MapPost("/profiles", async (Profile profile, IConfigStore store, CancellationToken ct) =>
        {
            await store.SaveProfileAsync(profile, ct);
            return Results.Created($"/api/profiles/{profile.ProfileId}", profile);
        })
        .WithName("SaveProfile")
        .WithSummary("Speichert ein Profil (erstellt oder überschreibt).");

        api.MapDelete("/profiles/{id}", async (string id, IConfigStore store, CancellationToken ct) =>
        {
            await store.DeleteProfileAsync(id, ct);
            return Results.NoContent();
        })
        .WithName("DeleteProfile")
        .WithSummary("Löscht ein Profil.");

        // ── Profil aktivieren ────────────────────────────────────────────────

        api.MapPost("/profiles/{id}/activate", async (
            string id,
            IConfigStore store,
            MappingEngine engine,
            CancellationToken ct) =>
        {
            var profile = await store.LoadProfileAsync(id, ct);
            if (profile is null) return Results.NotFound();
            engine.LoadProfile(profile);
            return Results.Ok(new { activated = id });
        })
        .WithName("ActivateProfile")
        .WithSummary("Aktiviert ein Profil in der Engine.");

        // ── MIDI-Geräte ──────────────────────────────────────────────────────

        api.MapGet("/devices", (IMidiDeviceManager devices) =>
            Results.Ok(devices.GetPhysicalDevices()))
        .WithName("ListDevices")
        .WithSummary("Listet alle verfügbaren physikalischen MIDI-Geräte mit Verbindungsstatus auf.");

        api.MapPost("/devices/{id}/connect", async (string id, IMidiDeviceManager devices, CancellationToken ct) =>
        {
            await devices.OpenDeviceAsync(Uri.UnescapeDataString(id), ct);
            return Results.Ok(new { connected = id });
        })
        .WithName("ConnectDevice")
        .WithSummary("Verbindet ein physikalisches MIDI-Gerät.");

        api.MapPost("/devices/{id}/disconnect", async (string id, IMidiDeviceManager devices, CancellationToken ct) =>
        {
            await devices.CloseDeviceAsync(Uri.UnescapeDataString(id), ct);
            return Results.Ok(new { disconnected = id });
        })
        .WithName("DisconnectDevice")
        .WithSummary("Trennt ein physikalisches MIDI-Gerät.");

        // ── Templates ────────────────────────────────────────────────────────

        api.MapGet("/templates", async (ITemplateStore store, CancellationToken ct) =>
            Results.Ok(await store.ListTemplateNamesAsync(ct)))
        .WithName("ListTemplates")
        .WithSummary("Listet alle gespeicherten Templates auf.");

        api.MapGet("/templates/conditions/{name}", async (
            string name, ITemplateStore store, CancellationToken ct) =>
        {
            var t = await store.LoadConditionBlockTemplateAsync(name, ct);
            return t is null ? Results.NotFound() : Results.Ok(t);
        })
        .WithName("GetConditionTemplate");

        api.MapGet("/templates/actions/{name}", async (
            string name, ITemplateStore store, CancellationToken ct) =>
        {
            var t = await store.LoadActionBlockTemplateAsync(name, ct);
            return t is null ? Results.NotFound() : Results.Ok(t);
        })
        .WithName("GetActionTemplate");

        api.MapPost("/templates/conditions", async (
            ConditionBlock template, ITemplateStore store, CancellationToken ct) =>
        {
            await store.SaveConditionBlockTemplateAsync(template, ct);
            return Results.Created($"/api/templates/conditions/{template.TemplateName}", template);
        })
        .WithName("SaveConditionTemplate");

        api.MapPost("/templates/actions", async (
            ActionBlock template, ITemplateStore store, CancellationToken ct) =>
        {
            await store.SaveActionBlockTemplateAsync(template, ct);
            return Results.Created($"/api/templates/actions/{template.TemplateName}", template);
        })
        .WithName("SaveActionTemplate");

        api.MapDelete("/templates/{name}", async (
            string name, ITemplateStore store, CancellationToken ct) =>
        {
            await store.DeleteTemplateAsync(name, ct);
            return Results.NoContent();
        })
        .WithName("DeleteTemplate");

        return app;
    }
}
