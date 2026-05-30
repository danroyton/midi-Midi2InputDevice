using MidiController.Domain.State;
using MidiController.Engine.State;

namespace MidiController.Host.Api;

internal static class StatusApi
{
    internal static IEndpointRouteBuilder MapStatusApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/status");

        // GET /api/status – Laufzeitstatus (Ping + aktuelle Gate-Variable A)
        api.MapGet("/", (VariableStore store) =>
        {
            int gateValue = store.Get(EngineState.GateVariable);
            string gateState = gateValue switch
            {
                EngineState.GateActive  => "Active",
                EngineState.GatePaused  => "Paused",
                EngineState.GateBlocked => "Blocked",
                _                       => "Unknown"
            };
            return Results.Ok(new StatusResponse("ok", gateValue, gateState, DateTimeOffset.UtcNow));
        })
        .WithName("GetStatus")
        .WithSummary("Laufzeitstatus: Gate-Variable A und Verbindungscheck.");

        // GET /api/status/variables – Snapshot aller Variablen A–Z
        api.MapGet("/variables", (VariableStore store) =>
            Results.Ok(store.Snapshot()))
        .WithName("GetVariables")
        .WithSummary("Snapshot aller Zustandsvariablen A–Z.");

        // PUT /api/status/variables/{var} – Variable setzen (z.B. A für Gate-Steuerung)
        api.MapPut("/variables/{var}", (
            string var,
            SetVariableRequest req,
            VariableStore store) =>
        {
            if (var.Length != 1 || var[0] < 'A' || var[0] > 'Z')
                return Results.BadRequest("Variable muss ein einzelner Buchstabe A–Z sein.");

            char variable = char.ToUpperInvariant(var[0]);

            // Reservierte berechnete DD*-Werte dürfen nicht gesetzt werden
            if (EngineState.Aliases.ContainsKey(variable) &&
                variable is 'V' or 'W')
                return Results.BadRequest($"Variable {variable} ist schreibgeschützt (berechneter DD*-Wert).");

            store.Set(variable, req.Value);
            return Results.Ok(new SetVariableResponse(variable, store.Get(variable)));
        })
        .WithName("SetVariable")
        .WithSummary("Setzt den Wert einer Zustandsvariable (z.B. A=0 zum Aktivieren).");

        return app;
    }
}

/// <summary>Request-Body für PUT /api/status/variables/{var}</summary>
internal record SetVariableRequest(int Value);

/// <summary>Response für GET /api/status/</summary>
internal record StatusResponse(
    string         Status,
    int            GateValue,
    string         GateState,
    DateTimeOffset Timestamp);

/// <summary>Response für PUT /api/status/variables/{var}</summary>
internal record SetVariableResponse(char Variable, int Value);
