# ADR-006: Communication Protocol between Frontend and Backend

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2025-05-30 |
| Decision makers | Development team |

## Context

Frontend and backend must communicate. The backend runs as a local process on the same machine. There are two communication patterns:

1. **Request/Response** — read/write configuration, enumerate devices, activate profiles.
2. **Push/Stream** — send raw MIDI data to the frontend in real time, status variable changes.

## Options

### Option A: REST (HTTP) + SignalR WebSocket
- REST for request/response; SignalR for streaming.
- **Pro:** Clearly separated responsibilities; REST is easy to test (Swagger/OpenAPI).
- **Pro:** SignalR provides automatic reconnect and transport fallback.
- **Pro:** Both available out of the box in ASP.NET Core.
- **Con:** Two protocols; frontend requires two client types.

### Option B: gRPC + gRPC server streaming
- Binary protocol, strongly typed (Protobuf).
- **Pro:** Lower serialisation overhead than JSON.
- **Con:** Overhead difference is negligible for localhost communication.
- **Con:** More complex tooling; WPF client integration less common.

### Option C: Named Pipes (IPC)
- Direct Windows IPC.
- **Pro:** Lowest latency for localhost communication.
- **Con:** Non-standard HTTP; harder to test and debug.
- **Con:** No browser access; harder to replace the frontend.

### Option D: WebSocket only (no REST)
- All messages over a single WebSocket channel.
- **Con:** Request/response pattern must be implemented manually (correlation IDs).
- **Con:** No standard tooling (Swagger etc.) usable.

## Decision

**Option A: ASP.NET Core Minimal API (REST/JSON) + SignalR.**

- **REST** for all CRUD operations and configuration.
- **SignalR** (WebSocket) for real-time streams: raw MIDI log and status variables.
- **OpenAPI/Swagger** enabled for easy development and testing.
- Port: `localhost:5173` (configurable in `appsettings.json`).

## Rationale

- Both technologies are natively integrated in ASP.NET Core — no additional framework needed.
- REST + OpenAPI allows independent development and manual testing of the backend.
- SignalR's automatic reconnect and transport fallback reduce client-side effort.
- REST endpoint latency is irrelevant for configuration operations.
- For the real-time stream (MIDI log), SignalR's WebSocket latency is sufficient since the log is display-only and is not on the critical injection path.

## Consequences

- Backend: `app.MapControllers()` / Minimal API + `app.MapHub<MidiLogHub>("/hubs/midilog")`.
- Frontend: `HttpClient` (REST) + `HubConnection` (SignalR).
- CORS is configured for `localhost`.
- API versioning: `/api/v1/` prefix; future breaking changes increment the version.
- Swagger UI available at `/swagger` in development mode.
