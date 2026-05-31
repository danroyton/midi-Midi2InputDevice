# ADR-003: Virtuelle MIDI-Ports für Multi-Consumer-Zugriff

| Feld | Wert |
|---|---|
| Status | **Akzeptiert** |
| Datum | 2025-05-30 |
| Entscheider | Entwicklungsteam |

## Kontext

Windows-MIDI-Treiber (WinMM) erlauben standardmäßig nur **einen gleichzeitigen Consumer** pro physikalischem MIDI-Eingang. Wenn mehrere Applikationen (z.B. DAW + MidiController) gleichzeitig auf dasselbe Gerät zugreifen wollen, schlägt der zweite `midiInOpen`-Aufruf fehl.

## Optionen

### Option A: loopMIDI (Tobias Erichsen)
- Kostenloser Windows-Treiber für virtuelle MIDI-Loopback-Ports.
- Sehr weit verbreitet in der MIDI-Community.
- Kann über COM-Automatisierung (`LoopMIDI.COM`) oder direkt per WinMM-API angesprochen werden.
- **Pro:** Stabil, geringe Latenz, großes Vertrauen in der Community.
- **Contra:** Externe Abhängigkeit, muss separat installiert werden.
- **Contra:** Kein öffentlich dokumentiertes .NET-SDK.

### Option B: Windows MIDI Services (ab Windows 11 24H2)
- Native Multi-Client-Unterstützung ohne virtuellen Treiber.
- **Pro:** Kein externer Treiber nötig.
- **Contra:** Nur Windows 11 24H2+; breite Userbase nicht erreichbar.

### Option C: Eigener MIDI-Dispatch im Backend
- Das Backend öffnet das physikalische Gerät exklusiv und verteilt Events intern.
- Andere Applikationen erhalten Events über virtuelle Ports (erstellt via loopMIDI oder MIDI-Yoke).
- **Pro:** Maximale Kontrolle; das Backend ist der einzige Konsument des physikalischen Ports.
- **Contra:** Wenn das Backend nicht läuft, können andere Apps nicht auf das Gerät zugreifen.

## Entscheidung

**Option C** (eigener Dispatch) in Kombination mit **Option A** (loopMIDI für die virtuellen Ausgabe-Ports).

Das Backend öffnet das physikalische Gerät exklusiv. Events werden intern weitergeleitet und optional auf loopMIDI-Ports gespiegelt, sodass andere Applikationen die virtuellen Ports abonnieren können.

## Begründung

- Option C ist unabhängig von loopMIDI für die Kernfunktion; loopMIDI ist nur für das optionale Mirroring erforderlich.
- Das Backend als Single-Consumer des physikalischen Ports ermöglicht maximale Kontrolle und niedrigste Latenz.
- loopMIDI ist der De-facto-Standard für virtuelle MIDI-Ports unter Windows; die Installation kann im Setup automatisiert werden.

## Konsequenzen

- Das Backend-Setup prüft, ob loopMIDI installiert ist; falls nicht, wird eine Warnung angezeigt.
- `IVirtualMidiPort`-Abstraktionsschicht erlaubt später den Austausch gegen Windows MIDI Services.
- Dokumentation muss loopMIDI-Installation beschreiben.
- Wenn das Backend nicht läuft, ist der physikalische Port für andere Applikationen **nicht** zugänglich. Dieses Verhalten ist in der Dokumentation explizit zu kommunizieren.
