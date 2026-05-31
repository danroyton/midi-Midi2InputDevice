# MidiController – Benutzerhandbuch

**Version:** v0.3  
**Plattform:** Windows 10 / 11 (64-Bit)  
**Kontakt / Issues:** https://github.com/danroyton/midi-Midi2InputDevice/issues

---

## Inhaltsverzeichnis

1. [Systemanforderungen](#1-systemanforderungen)
2. [Installation und erster Start](#2-installation-und-erster-start)
3. [Benutzeroberfläche im Überblick](#3-benutzeroberfläche-im-überblick)
4. [MIDI-Geräte verbinden](#4-midi-geräte-verbinden)
5. [Profile verwalten](#5-profile-verwalten)
6. [Trigger erstellen und bearbeiten](#6-trigger-erstellen-und-bearbeiten)
7. [Die Mapping-Engine verstehen](#7-die-mapping-engine-verstehen)
8. [Tastatur-Test](#8-tastatur-test)
9. [MIDI-Log](#9-midi-log)
10. [System-Tray](#10-system-tray)
11. [Häufige Fragen (FAQ)](#11-häufige-fragen-faq)
12. [Fehlerbehebung](#12-fehlerbehebung)

---

## 1. Systemanforderungen

| Anforderung | Details |
|---|---|
| **Betriebssystem** | Windows 10 oder Windows 11 (64-Bit) |
| **.NET Runtime** | Nicht erforderlich – in der EXE enthalten |
| **MIDI-Hardware** | Mindestens ein physikalisches MIDI-Gerät (USB oder DIN über Treiber) |
| **Arbeitsspeicher** | 64 MB RAM (Empfehlung: 128 MB) |
| **Festplatte** | ca. 120 MB für die Anwendung |

---

## 2. Installation und erster Start

### 2.1 Herunterladen

1. Die neueste Version unter **Releases** auf GitHub herunterladen:  
   https://github.com/danroyton/midi-Midi2InputDevice/releases/latest

2. Das ZIP-Archiv an einen beliebigen Ort entpacken, z. B.:  
   `C:\Programme\MidiController\`

3. Der entpackte Ordner enthält mindestens:
   ```
   MidiController.Frontend.exe
   appsettings.json
   appsettings.backend.json
   ```

### 2.2 Erster Start

1. **`MidiController.Frontend.exe`** per Doppelklick starten.  
   Windows Defender SmartScreen kann beim ersten Start eine Warnung anzeigen → **„Trotzdem ausführen"** klicken.

2. Die Anwendung öffnet das Hauptfenster und startet automatisch den Backend-Dienst im Hintergrund.

3. Das **Status-Icon** oben rechts zeigt den Verbindungsstatus:
   - 🟢 Grün = Backend gestartet, Profil aktiv
   - 🟠 Orange = Backend gestartet, Gate gesperrt (Standard beim ersten Start)
   - 🔴 Rot = Backend noch nicht bereit (kurz warten)

4. Beim **ersten Start** ist kein Profil aktiv. Zuerst ein Profil anlegen (→ Abschnitt 5).

### 2.3 Konfigurationsdateien

Profil- und Template-Daten werden unter `%APPDATA%\MidiController\` gespeichert  
(typisch: `C:\Users\<Benutzername>\AppData\Roaming\MidiController\`).

Die Datei `appsettings.backend.json` neben der EXE steuert Backend-Einstellungen  
(Port, Datenpfad). In der Regel muss sie nicht geändert werden.

---

## 3. Benutzeroberfläche im Überblick

```
┌─────────────────────────────────────────────────────────────────┐
│  🎵 MidiController   [Profil: gaming ▼]  [Aktivieren]  [●]     │
├─────────┬───────────────────────────────────────────────────────┤
│ Devices │                                                       │
│ Mappings│            Inhaltsbereich                             │
│Templates│                                                       │
│   Log   │                                                       │
│  Status │                                                       │
│  Test   │                                                       │
└─────────┴───────────────────────────────────────────────────────┘
```

| Element | Beschreibung |
|---|---|
| **Profil-Dropdown** | Aktives Profil auswählen oder wechseln |
| **Aktivieren-Button** | Verarbeitung einschalten (Gate A=0) |
| **Status-Indikator (●)** | Grün/Gelb/Orange/Rot – Backend-Verbindungsstatus |
| **Navigation links** | Wechsel zwischen den sechs Hauptbereichen |

---

## 4. MIDI-Geräte verbinden

### 4.1 Devices-View öffnen

Navigation → **Devices**

### 4.2 Gerät öffnen

1. Die Liste zeigt alle vom Betriebssystem erkannten MIDI-Eingabegeräte.
2. Klick auf **[Öffnen]** neben dem gewünschten Gerät.
3. Status wechselt zu **„Verbunden"**.

> **Tipp:** Wenn ein Gerät nicht erscheint, MIDI-Treiber prüfen und die Liste per **[Aktualisieren]** neu laden.

### 4.3 Gerät trennen

Klick auf **[Schließen]** neben dem verbundenen Gerät.

---

## 5. Profile verwalten

Ein **Profil** fasst alle Trigger-Konfigurationen für einen Anwendungsfall zusammen  
(z. B. „Gaming", „Video-Bearbeitung", „Stream-Steuerung").

### 5.1 Profil anlegen

1. Im Profil-Dropdown **„+ Neues Profil…"** wählen.
2. Namen eingeben und bestätigen.
3. Das neue Profil erscheint im Dropdown.

### 5.2 Profil aktivieren

1. Im Dropdown das Profil auswählen.
2. **[Aktivieren]** klicken → Gate wird auf A=0 gesetzt, Trigger werden ausgewertet.

### 5.3 Profil löschen

Nur möglich, wenn kein Profil aktiv ist:  
Im Dropdown das Profil auswählen → **[Profil löschen]** klicken.

---

## 6. Trigger erstellen und bearbeiten

Ein **Trigger** definiert: *Welches MIDI-Event* löst *welche Tastenaktion* aus.

### 6.1 Neuen Trigger anlegen

1. Navigation → **Mappings**.
2. Klick auf **[+ Neu]**.
3. Der Trigger-Editor öffnet sich.

### 6.2 MIDI-Quelle definieren

| Feld | Bedeutung |
|---|---|
| **Gerät** | Welches MIDI-Gerät soll auslösen? |
| **Event-Typ** | `NoteOn`, `NoteOff`, `ControlChange`, `ProgramChange`, … |
| **Kanal** | MIDI-Kanal 1–16 |
| **Data1** | Note-Nummer oder CC-Nummer (0–127) |
| **Match** | `Variable` (immer), `Data2` (fester Wert), `DD2Positive` / `DD2Negative` (nur bei Drehreglern) |

> **Tipp:** Mit **[▶ Mitschneiden]** ein MIDI-Event direkt am Gerät auslösen – der Editor füllt Gerät, Typ, Kanal und Data1 automatisch aus.

### 6.3 Globale Pre-Zuweisungen

Variablen, die **vor** der Auswertung aller Prüfblöcke gesetzt werden sollen.  
Typischer Einsatz: Sperrvariable setzen, damit kein zweiter Trigger gleichzeitig ausgeführt wird.

Klick auf **[+ Zuweisung]**:
- **Variable**: Eine der Status-Variablen A–Z auswählen
- **Quelle**: `Fixed` (fester Wert) | Andere Variable | MIDI-Datenwert

### 6.4 Prüfblöcke (Bedingungen)

Prüfblöcke entscheiden, **ob** eine Aktion ausgeführt wird.

- Zwischen Blöcken gilt **UND** (alle müssen erfüllt sein).
- Innerhalb eines Blocks gilt **ODER** (mindestens eine Bedingung muss erfüllt sein).
- Jeder Block kann ein **Template** referenzieren oder eigene Bedingungen inline definieren.
- Ein **ELSE-Zweig** pro Block ermöglicht alternative Aktionen bei Fehlschlag.

Bedingungsformat: `[Links] [Operator] [Rechts]`

| Quellen (Links/Rechts) | Beschreibung |
|---|---|
| `Fixed` | Fester Zahlenwert |
| `MidiData1` / `MidiData2` | Rohe MIDI-Datenbytes des auslösenden Events |
| `DeltaData2` | Differenz des Data2-Werts zum vorherigen Event |
| `DD2Positive` / `DD2Negative` | Betrag der positiven / negativen Änderung |
| `A`…`Z` | Status-Variablen |

Operatoren: `=`, `≠`, `<`, `≤`, `>`, `≥`

### 6.5 Aktionen

Eine Aktion sendet einen Tastendruck an Windows.

| Feld | Beschreibung |
|---|---|
| **Tasten** | Modifier (Strg, Alt, Shift, Win) + Haupttaste |
| **X (Repeat)** | Wie oft wird die Taste gedrückt? Quelle: `Fixed`, `DD2Positive`, Variable A–Z, … |
| **Y (KeyDuration)** | Wie lang wird die Taste gehalten (ms)? |
| **Z (Pause)** | Pause nach dem Tastendruck (ms) |
| **State-Zuweisungen** | Variablen nach diesem Tastendruck setzen |

> **Drehregler-Tipp:** Für einen Drehregler (z. B. Lautstärke) zwei Aktionen anlegen:  
> - Aktion 1: `VolumeUp`, X = `DD2Positive` → dreht man rechts, wird Lautstärke erhöht  
> - Aktion 2: `VolumeDown`, X = `DD2Negative` → dreht man links, wird Lautstärke verringert

### 6.6 Globale Post-Zuweisungen

Variablen, die **nach** allen Aktionen gesetzt werden (einmalig, unabhängig von Prüfblöcken).  
Typischer Einsatz: Sperrvariable wieder freigeben.

### 6.7 Trigger speichern

Klick auf **[Speichern]**. Der Trigger erscheint in der Trigger-Liste der Mappings-View.

---

## 7. Die Mapping-Engine verstehen

### 7.1 Aktivierungs-Gate (Variable A)

Die Verarbeitung läuft nur, wenn **A = 0** (aktiv).

| Wert | Zustand | Icon |
|---|---|---|
| `0` | Aktiv – Trigger werden ausgewertet | 🟢 |
| `1` | Pausiert – Events werden empfangen, aber nicht ausgewertet | 🟡 |
| `2` | Gesperrt – Default beim Start; kein Empfang | 🟠 |

Gate-Steuerung: Status-View → Schaltflächen **Aktivieren / Pause / Sperren**  
oder über das Tray-Icon-Kontextmenü.

### 7.2 Status-Variablen A–Z

- 26 Zustandsvariablen (A bis Z), Wertebereich: −127 bis +127
- Können in Trigger-Aktionen gelesen und geschrieben werden
- Reservierte Variablen (nicht für allgemeine Zwecke):

| Variable | Alias | Bedeutung |
|---|---|---|
| A | `ActiveListen` | Aktivierungs-Gate (0/1/2) |
| X | `Repeat` | Standardwert für Wiederholungen |
| Y | `KeyDuration` | Standardwert für Tastenhaltedauer (ms) |
| Z | `Pause` | Standardwert für Pause nach Tastendruck (ms) |

### 7.3 Delta-Tracking

Bei jedem MIDI-Event werden automatisch berechnet:

| Wert | Beschreibung |
|---|---|
| `DeltaData2` | Differenz: Data2 aktuell − Data2 vorheriges Event |
| `DD2Positive` | Wert, wenn `DeltaData2 > 0`; sonst 0 |
| `DD2Negative` | Betrag, wenn `DeltaData2 < 0`; sonst 0 |

Ideal für relative Drehregler (Endlosknöpfe).

---

## 8. Tastatur-Test

Navigation → **Test**

Hier können Tastendrücke **direkt getestet** werden, ohne MIDI-Hardware:

1. Modifier-Checkboxen aktivieren (Strg, Alt, Shift, Win)
2. Haupttaste eingeben (z. B. `T` oder `VolumeUp`)
3. Y (Dauer), Z (Pause) einstellen
4. **[▶ Senden]** klicken

Die zuletzt gesendete Taste wird im Bereich **„Letzte Aktion"** angezeigt.

---

## 9. MIDI-Log

Navigation → **Log**

Der MIDI-Log zeigt alle empfangenen MIDI-Events in Echtzeit:

- **Gerätefilter**: Nur Events eines bestimmten Geräts anzeigen
- Maximal 1.000 Zeilen; ältere Einträge werden verworfen
- **[Leeren]**: Liste leeren
- **Rechtsklick** auf eine Zeile → „Trigger für dieses Event anlegen"  
  (öffnet den Trigger-Editor mit vorausgefüllten Feldern)

---

## 10. System-Tray

Das Tray-Icon erscheint in der Windows-Taskleiste unten rechts, sobald die Anwendung gestartet ist.

**Farben:**

| Farbe | Bedeutung |
|---|---|
| 🟢 Grün | Verbunden + aktiv (A=0) |
| 🟡 Gelb | Verbunden + pausiert (A=1) |
| 🟠 Orange | Verbunden + gesperrt (A=2) |
| 🔴 Rot | Backend nicht erreichbar |

**Blinken:** Das Icon blinkt kurz bei jedem empfangenen MIDI-Event (nur bei tatsächlicher Aktivität).

**Kontextmenü (Rechtsklick):**
- `Aktivieren (A=0)` / `Pause (A=1)` / `Sperren (A=2)` → Gate direkt umschalten
- `Fenster anzeigen` → Hauptfenster öffnen
- `Beenden` → Anwendung beenden (Backend wird gestoppt)

**Fenster minimieren:** Das Hauptfenster kann geschlossen werden – die Anwendung läuft im Tray weiter.  
Per Doppelklick auf das Tray-Icon oder „Fenster anzeigen" im Kontextmenü wieder öffnen.

---

## 11. Häufige Fragen (FAQ)

**F: Mein MIDI-Gerät erscheint nicht in der Devices-Liste.**  
A: Sicherstellen, dass der MIDI-Treiber installiert ist und das Gerät im Geräte-Manager erscheint. Dann „Aktualisieren" in der Devices-View klicken.

**F: Die Tastatur-Eingaben werden nicht an mein Spiel/Programm gesendet.**  
A: Das Zielfenster muss den Fokus haben. MidiController selbst darf nicht im Vordergrund sein. Im Zweifelsfall das Zielspiel in den Vordergrund bringen und dann am MIDI-Gerät auslösen.

**F: Das Gate ist gesperrt (A=2) nach dem Start – warum?**  
A: Das ist das Standardverhalten beim ersten Start. Im Status-Tab oder per Tray-Kontextmenü auf „Aktivieren" klicken, um A=0 zu setzen.

**F: Wo werden meine Profile gespeichert? Kann ich sie sichern?**  
A: Profile liegen unter `%APPDATA%\MidiController\profiles\` als JSON-Dateien. Einfach diesen Ordner kopieren.

**F: Kann ich mehrere MIDI-Geräte gleichzeitig verwenden?**  
A: Ja. In der Devices-View mehrere Geräte öffnen. Jeder Trigger kann einem bestimmten Gerät zugewiesen sein.

**F: Wie ändere ich den Port, auf dem das Backend läuft?**  
A: In `appsettings.backend.json` (neben der EXE) den Wert `Urls` anpassen, z. B.:  
`"Urls": "http://localhost:5001"`. Gleichzeitig in `appsettings.json` unter `Backend.BaseUrl` denselben Wert setzen.

**F: Kann ich die Anwendung als Windows-Dienst betreiben?**  
A: Das ist in der aktuellen Version nicht vollständig unterstützt. `SendInput` funktioniert im Dienst-Modus (Session 0) ohne Session-Weiterleitung nicht. Geplant für v1.0.

---

## 12. Fehlerbehebung

### Backend startet nicht (Status dauerhaft Rot)

1. Prüfen ob Port 5000 bereits belegt ist:  
   ```powershell
   netstat -ano | findstr :5000
   ```
2. Falls belegt, Port in `appsettings.backend.json` ändern.
3. Event-Log prüfen: Windows-Ereignisanzeige → Anwendungsprotokoll.
4. `MidiController.Frontend.exe` mit Administrator-Rechten starten (einmalig zum Testen).

### MIDI-Events kommen an, aber keine Tastatureingaben

1. Im **Status-Tab** sicherstellen, dass A=0 (Grün).
2. Trigger-Definition prüfen: Gerät, Kanal, Data1 müssen exakt mit dem MIDI-Log übereinstimmen.
3. Im **MIDI-Log** prüfen, ob das Event tatsächlich empfangen wird.
4. Prüfen, ob das Zielfenster den Fokus hat.

### Trigger wird ausgelöst, aber Taste kommt falsch an

1. Im **Test-Tab** die gewünschte Taste direkt senden und im Zielfenster prüfen.
2. Sicherstellen, dass keine Modifier-Taste unbeabsichtigt gesetzt ist.
3. Bei Medientasten (`VolumeUp`, `MediaPlayPause` etc.) sicherstellen, dass das Zielprogramm diese Sondertasten verarbeitet.

### Hohe CPU-Last

Die Anwendung sollte im Leerlauf < 1 % CPU verbrauchen. Hohe Last bei vielen MIDI-Events ist normal.  
Bei dauerhaft hoher Last ohne MIDI-Aktivität: bitte ein [Issue auf GitHub](https://github.com/danroyton/midi-Midi2InputDevice/issues) erstellen.

---

*Dieses Handbuch gilt für MidiController v0.3. Für ältere oder neuere Versionen können Abweichungen bestehen.*
