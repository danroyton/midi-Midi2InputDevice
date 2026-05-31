# ADR-001: Wahl der MIDI-Eingabe-Bibliothek

| Feld | Wert |
|---|---|
| Status | **Akzeptiert** |
| Datum | 2025-05-30 |
| Entscheider | Entwicklungsteam |

## Kontext

Das Backend muss MIDI-Events von physikalischen Geräten unter Windows empfangen. Die Bibliothek hat direkten Einfluss auf die Latenz und die Wartbarkeit.

## Optionen

### Option A: NAudio (`NAudio.Midi.MidiIn`)
- **Pro:** Weit verbreitet, .NET-nativ, aktiv gepflegt, NuGet-verfügbar.
- **Pro:** Direkte `winmm.dll`-Bindung; geringe Latenz (~1–2 ms).
- **Contra:** Kein Low-Level-Zugriff auf MIDI 2.0.
- **Contra:** Kein Multi-Client ohne virtuelle Ports.

### Option B: RtMidi.NET
- **Pro:** Cross-Platform (Windows/macOS/Linux).
- **Pro:** Etwas niedrigere Latenz durch RtMidi-C++-Backend.
- **Contra:** Weniger verbreitet, kleinere Community.
- **Contra:** Native DLL erforderlich (Deployment-Overhead).

### Option C: Windows MIDI Services (WinRT, Windows 11 24H2+)
- **Pro:** Offizielles Microsoft-SDK, MIDI 2.0, Multi-Client nativ.
- **Pro:** Niedrigste Latenz auf unterstützter Hardware.
- **Contra:** Nur Windows 11 24H2 und neuer.
- **Contra:** WinRT-Interop in .NET 10 Worker Service aufwändig.

## Entscheidung

**Option A (NAudio)** als primäre Bibliothek mit Abstraktionsschicht (`IMidiInputDevice`), die später gegen Option B oder C ausgetauscht werden kann.

## Begründung

- NAudio ist battle-tested, hat eine große Community und ist direkt über NuGet verfügbar.
- Die Latenz von ~1–2 ms ist für die Zielanwendung (Tastatureingaben) ausreichend.
- Durch die Abstraktionsschicht bleibt ein späterer Wechsel zu Windows MIDI Services möglich, sobald die Windows-11-Mindestanforderung akzeptabel ist.

## Konsequenzen

- `IMidiInputDevice` Interface muss definiert werden.
- NAudio wird als NuGet-Abhängigkeit aufgenommen (`NAudio`, `NAudio.WinMM`).
- MIDI 2.0 Features sind vorerst nicht verfügbar.
