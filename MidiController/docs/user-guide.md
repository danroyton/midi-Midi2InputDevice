# MidiController – User Guide

**Version:** v0.3
**Platform:** Windows 10 / 11 (64-bit)
**Contact / Issues:** https://github.com/danroyton/midi-Midi2InputDevice/issues

---

## Table of Contents

1. [System Requirements](#1-system-requirements)
2. [Installation and First Launch](#2-installation-and-first-launch)
3. [User Interface Overview](#3-user-interface-overview)
4. [Connecting MIDI Devices](#4-connecting-midi-devices)
5. [Managing Profiles](#5-managing-profiles)
6. [Creating and Editing Triggers](#6-creating-and-editing-triggers)
7. [Understanding the Mapping Engine](#7-understanding-the-mapping-engine)
8. [Keyboard Test](#8-keyboard-test)
9. [MIDI Log](#9-midi-log)
10. [System Tray](#10-system-tray)
11. [Frequently Asked Questions (FAQ)](#11-frequently-asked-questions-faq)
12. [Troubleshooting](#12-troubleshooting)

---

## 1. System Requirements

| Requirement | Details |
|---|---|
| **Operating System** | Windows 10 or Windows 11 (64-bit) |
| **.NET Runtime** | Not required – bundled in the EXE |
| **MIDI Hardware** | At least one physical MIDI device (USB or DIN via driver) |
| **RAM** | 64 MB (recommended: 128 MB) |
| **Disk Space** | approx. 120 MB for the application |

---

## 2. Installation and First Launch

### 2.1 Download

1. Download the latest release from **Releases** on GitHub:
   https://github.com/danroyton/midi-Midi2InputDevice/releases/latest

2. Extract the ZIP archive to any location, e.g.:
   `C:\Program Files\MidiController\`

3. The extracted folder contains at least:
   ```
   MidiController.Frontend.exe
   appsettings.json
   appsettings.backend.json
   ```

### 2.2 First Launch

1. Double-click **`MidiController.Frontend.exe`** to start.
   Windows Defender SmartScreen may show a warning on first launch → click **"Run anyway"**.

2. The application opens the main window and automatically starts the backend service in the background.

3. The **status icon** in the top right shows the connection status:
   - 🟢 Green = Backend started, profile active
   - 🟠 Orange = Backend started, gate locked (default on first launch)
   - 🔴 Red = Backend not yet ready (wait a moment)

4. On **first launch**, no profile is active. Create a profile first (→ Section 5).

### 2.3 Configuration Files

Profile and template data are stored under `%APPDATA%\MidiController\`
(typically: `C:\Users\<Username>\AppData\Roaming\MidiController\`).

The file `appsettings.backend.json` next to the EXE controls backend settings
(port, data path). It usually does not need to be changed.

---

## 3. User Interface Overview

```
┌─────────────────────────────────────────────────────────────────┐
│  🎵 MidiController   [Profile: gaming ▼]  [Activate]  [●]     │
├─────────┬───────────────────────────────────────────────────────┤
│ Devices │                                                       │
│ Mappings│            Content Area                               │
│Templates│                                                       │
│   Log   │                                                       │
│  Status │                                                       │
│  Test   │                                                       │
└─────────┴───────────────────────────────────────────────────────┘
```

| Element | Description |
|---|---|
| **Profile dropdown** | Select or switch the active profile |
| **Activate button** | Enable processing (Gate A=0) |
| **Status indicator (●)** | Green/Yellow/Orange/Red – backend connection status |
| **Left navigation** | Switch between the six main sections |

---

## 4. Connecting MIDI Devices

### 4.1 Open the Devices View

Navigation → **Devices**

### 4.2 Open a Device

1. The list shows all MIDI input devices recognized by the operating system.
2. Click **[Open]** next to the desired device.
3. Status changes to **"Connected"**.

> **Tip:** If a device does not appear, check the MIDI driver and reload the list with **[Refresh]**.

### 4.3 Disconnect a Device

Click **[Close]** next to the connected device.

---

## 5. Managing Profiles

A **profile** groups all trigger configurations for a specific use case
(e.g. "Gaming", "Video Editing", "Stream Control").

### 5.1 Create a Profile

1. Select **"+ New Profile…"** in the profile dropdown.
2. Enter a name and confirm.
3. The new profile appears in the dropdown.

### 5.2 Activate a Profile

1. Select the profile in the dropdown.
2. Click **[Activate]** → Gate is set to A=0, triggers are evaluated.

### 5.3 Delete a Profile

Only possible when no profile is active:
Select the profile in the dropdown → click **[Delete Profile]**.

---

## 6. Creating and Editing Triggers

A **trigger** defines: *which MIDI event* fires *which key action*.

### 6.1 Create a New Trigger

1. Navigation → **Mappings**.
2. Click **[+ New]**.
3. The trigger editor opens.

### 6.2 Define the MIDI Source

| Field | Meaning |
|---|---|
| **Device** | Which MIDI device should fire? |
| **Event Type** | `NoteOn`, `NoteOff`, `ControlChange`, `ProgramChange`, … |
| **Channel** | MIDI channel 1–16 |
| **Data1** | Note number or CC number (0–127) |
| **Match** | `Variable` (always), `Data2` (fixed value), `DD2Positive` / `DD2Negative` (for rotary encoders) |

> **Tip:** Use **[▶ Record]** to fire a MIDI event directly on the device – the editor fills in Device, Type, Channel and Data1 automatically.

### 6.3 Global Pre-Assignments

Variables to be set **before** evaluating all condition blocks.
Typical use: set a lock variable to prevent a second trigger from running simultaneously.

Click **[+ Assignment]**:
- **Variable**: Select one of the status variables A–Z
- **Source**: `Fixed` (fixed value) | another variable | MIDI data value

### 6.4 Condition Blocks

Condition blocks decide **whether** an action is executed.

- Between blocks: **AND** logic (all must be satisfied).
- Within a block: **OR** logic (at least one condition must be satisfied).
- Each block can reference a **template** or define its own conditions inline.
- An **ELSE branch** per block allows alternative actions on failure.

Condition format: `[Left] [Operator] [Right]`

| Sources (Left/Right) | Description |
|---|---|
| `Fixed` | Fixed numeric value |
| `MidiData1` / `MidiData2` | Raw MIDI data bytes of the triggering event |
| `DeltaData2` | Difference of the Data2 value compared to the previous event |
| `DD2Positive` / `DD2Negative` | Magnitude of the positive / negative change |
| `A`…`Z` | Status variables |

Operators: `=`, `≠`, `<`, `≤`, `>`, `≥`

### 6.5 Actions

An action sends a key press to Windows.

| Field | Description |
|---|---|
| **Keys** | Modifier (Ctrl, Alt, Shift, Win) + main key |
| **X (Repeat)** | How many times is the key pressed? Source: `Fixed`, `DD2Positive`, variable A–Z, … |
| **Y (KeyDuration)** | How long is the key held (ms)? |
| **Z (Pause)** | Pause after the key press (ms) |
| **State Assignments** | Set variables after this key press |

> **Rotary encoder tip:** For a rotary encoder (e.g. volume), create two actions:
> - Action 1: `VolumeUp`, X = `DD2Positive` → turning right increases volume
> - Action 2: `VolumeDown`, X = `DD2Negative` → turning left decreases volume

### 6.6 Global Post-Assignments

Variables to be set **after** all actions (once, regardless of condition blocks).
Typical use: release the lock variable.

### 6.7 Save a Trigger

Click **[Save]**. The trigger appears in the trigger list of the Mappings view.

---

## 7. Understanding the Mapping Engine

### 7.1 Activation Gate (Variable A)

Processing only runs when **A = 0** (active).

| Value | State | Icon |
|---|---|---|
| `0` | Active – triggers are evaluated | 🟢 |
| `1` | Paused – events are received but not evaluated | 🟡 |
| `2` | Locked – default on startup; no reception | 🟠 |

Gate control: Status view → **Activate / Pause / Lock** buttons
or via the tray icon context menu.

### 7.2 Status Variables A–Z

- 26 state variables (A to Z), value range: −127 to +127
- Can be read and written in trigger actions
- Reserved variables (not for general use):

| Variable | Alias | Meaning |
|---|---|---|
| A | `ActiveListen` | Activation gate (0/1/2) |
| X | `Repeat` | Default value for repetitions |
| Y | `KeyDuration` | Default value for key hold duration (ms) |
| Z | `Pause` | Default value for pause after key press (ms) |

### 7.3 Delta Tracking

The following values are automatically computed for each MIDI event:

| Value | Description |
|---|---|
| `DeltaData2` | Difference: current Data2 − previous event Data2 |
| `DD2Positive` | Value if `DeltaData2 > 0`; otherwise 0 |
| `DD2Negative` | Magnitude if `DeltaData2 < 0`; otherwise 0 |

Ideal for relative rotary encoders (endless knobs).

---

## 8. Keyboard Test

Navigation → **Test**

Here you can **test key presses directly** without MIDI hardware:

1. Enable modifier checkboxes (Ctrl, Alt, Shift, Win)
2. Enter the main key (e.g. `T` or `VolumeUp`)
3. Set Y (duration) and Z (pause)
4. Click **[▶ Send]**

The last sent key is shown in the **"Last Action"** section.

---

## 9. MIDI Log

Navigation → **Log**

The MIDI log shows all received MIDI events in real time:

- **Device filter**: Show only events from a specific device
- Maximum 1,000 rows; older entries are discarded
- **[Clear]**: Clear the list
- **Right-click** on a row → "Create trigger for this event"
  (opens the trigger editor with pre-filled fields)

---

## 10. System Tray

The tray icon appears in the Windows taskbar (bottom right) as soon as the application is started.

**Colors:**

| Color | Meaning |
|---|---|
| 🟢 Green | Connected + active (A=0) |
| 🟡 Yellow | Connected + paused (A=1) |
| 🟠 Orange | Connected + locked (A=2) |
| 🔴 Red | Backend unreachable |

**Blinking:** The icon blinks briefly on each received MIDI event (only on actual activity).

**Context menu (right-click):**
- `Activate (A=0)` / `Pause (A=1)` / `Lock (A=2)` → switch gate directly
- `Show Window` → open main window
- `Exit` → close application (backend is stopped)

**Minimizing the window:** The main window can be closed – the application continues running in the tray.
Re-open by double-clicking the tray icon or selecting "Show Window" from the context menu.

---

## 11. Frequently Asked Questions (FAQ)

**Q: My MIDI device does not appear in the Devices list.**
A: Make sure the MIDI driver is installed and the device appears in Device Manager. Then click "Refresh" in the Devices view.

**Q: Keyboard inputs are not sent to my game/application.**
A: The target window must have focus. MidiController itself must not be in the foreground. If in doubt, bring the target application to the foreground and then trigger from the MIDI device.

**Q: The gate is locked (A=2) after launch – why?**
A: This is the default behavior on first launch. Click "Activate" in the Status tab or via the tray context menu to set A=0.

**Q: Where are my profiles stored? Can I back them up?**
A: Profiles are stored under `%APPDATA%\MidiController\profiles\` as JSON files. Simply copy this folder.

**Q: Can I use multiple MIDI devices simultaneously?**
A: Yes. Open multiple devices in the Devices view. Each trigger can be assigned to a specific device.

**Q: How do I change the port the backend runs on?**
A: In `appsettings.backend.json` (next to the EXE), adjust the `Urls` value, e.g.:
`"Urls": "http://localhost:5001"`. Also set the same value in `appsettings.json` under `Backend.BaseUrl`.

**Q: Can I run the application as a Windows service?**
A: This is not fully supported in the current version. `SendInput` does not work in service mode (Session 0) without session forwarding. Planned for v1.0.

---

## 12. Troubleshooting

### Backend does not start (status remains Red)

1. Check if port 5000 is already in use:
   ```powershell
   netstat -ano | findstr :5000
   ```
2. If occupied, change the port in `appsettings.backend.json`.
3. Check the event log: Windows Event Viewer → Application log.
4. Launch `MidiController.Frontend.exe` with administrator rights (for testing purposes only).

### MIDI events are received but no keyboard input is sent

1. In the **Status tab**, make sure A=0 (Green).
2. Check the trigger definition: Device, Channel, Data1 must exactly match the MIDI log.
3. In the **MIDI Log**, verify that the event is actually received.
4. Check that the target window has focus.

### Trigger fires but the wrong key arrives

1. In the **Test tab**, send the desired key directly and verify in the target window.
2. Make sure no modifier key is unintentionally set.
3. For media keys (`VolumeUp`, `MediaPlayPause`, etc.), make sure the target application handles these special keys.

### High CPU usage

The application should consume < 1% CPU when idle. High load during many MIDI events is normal.
For consistently high load without MIDI activity, please open an [issue on GitHub](https://github.com/danroyton/midi-Midi2InputDevice/issues).

---

*This guide applies to MidiController v0.3. Differences may exist for older or newer versions.*
