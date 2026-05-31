# ADR-006: Kommunikationsprotokoll zwischen Frontend und Backend

| Feld | Wert |
|---|---|
| Status | **Akzeptiert** |
| Datum | 2025-05-30 |
| Entscheider | Entwicklungsteam |

## Kontext

Frontend und Backend müssen kommunizieren. Das Backend läuft als lokaler Prozess auf demselben Rechner. Es gibt zwei Kommunikationsmuster:

1. **Request/Response** – Konfiguration lesen/schreiben, Geräte auflisten, Profile aktivieren.
2. **Push/Stream** – MIDI-Rohdaten in Echtzeit ans Frontend senden, Status-Variablen-Änderungen.

## Optionen

### Option A: REST (HTTP) + SignalR WebSocket
- REST für Request/Response; SignalR für Streaming.
- **Pro:** Klar getrennte Verantwortlichkeiten; REST ist einfach testbar (Swagger/OpenAPI).
- **Pro:** SignalR bietet automatisches Reconnect, Fallback-Transporte.
- **Pro:** Beide in ASP.NET Core out-of-the-box verfügbar.
- **Contra:** Zwei Protokolle; Frontend benötigt zwei Client-Typen.

### Option B: gRPC + gRPC-ServerStreaming
- Binärprotokoll, stark typisiert (Protobuf).
- **Pro:** Niedrigere Serialisierungs-Overhead als JSON.
- **Contra:** Für Localhost irrelevanter Overhead-Unterschied.
- **Contra:** Aufwändigeres Tooling; WPF-Client-Integration weniger verbreitet.

### Option C: Named Pipes (IPC)
- Direkte Windows-IPC.
- **Pro:** Niedrigste Latenz für Localhost-Kommunikation.
- **Contra:** Kein Standard-HTTP; schwieriger zu testen und zu debuggen.
- **Contra:** Kein Browser-Zugriff; erschwerter Austausch des Frontends.

### Option D: Nur WebSocket (kein REST)
- Alle Nachrichten über einen WebSocket-Kanal.
- **Contra:** Request/Response-Muster muss manuell implementiert werden (Correlation IDs).
- **Contra:** Kein Standard-Tooling (Swagger etc.) nutzbar.

## Entscheidung

**Option A: ASP.NET Core Minimal API (REST/JSON) + SignalR.**

- **REST** für alle CRUD-Operationen und Konfiguration.
- **SignalR** (WebSocket) für Echtzeit-Streams: MIDI-Roh-Log und Status-Variablen.
- **OpenAPI/Swagger** wird aktiviert für einfache Entwicklung und Tests.
- Port: `localhost:5173` (konfigurierbar in `appsettings.json`).

## Begründung

- Beide Technologien sind in ASP.NET Core nativ integriert – kein zusätzliches Framework.
- REST + OpenAPI erlaubt unabhängige Entwicklung und manuelle Tests des Backends.
- SignalR's automatisches Reconnect und Transport-Fallback reduzieren den Client-seitigen Aufwand.
- Die Latenz der REST-Endpunkte ist für Konfigurationsoperationen irrelevant.
- Für den Echtzeit-Stream (MIDI-Log) ist die WebSocket-Latenz von SignalR ausreichend, da das Log nur zur Anzeige dient und nicht auf dem kritischen Injection-Pfad liegt.

## Konsequenzen

- Backend: `app.MapControllers()` / Minimal API + `app.MapHub<MidiLogHub>("/hubs/midilog")`.
- Frontend: `HttpClient` (REST) + `HubConnection` (SignalR).
- CORS wird für `localhost` konfiguriert.
- API-Versionierung: `/api/v1/` Präfix; zukünftige Breaking Changes inkrementieren die Version.
- Swagger UI unter `/swagger` im Development-Modus verfügbar.
