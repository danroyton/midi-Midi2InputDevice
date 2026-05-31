# Release erstellen – MidiController

Diese Anleitung beschreibt, wie ein neues Release auf GitHub veröffentlicht wird.  
Es gibt zwei Wege: **automatisch via GitHub Actions** (empfohlen) oder **manuell**.

---

## Inhaltsverzeichnis

- [Vorbereitung](#vorbereitung)
- [Weg A: Automatisch via GitHub Actions](#weg-a-automatisch-via-github-actions)
- [Weg B: Manuell (ohne GitHub Actions)](#weg-b-manuell-ohne-github-actions)
- [GitHub Actions Workflow einrichten](#github-actions-workflow-einrichten)
- [Release-Checkliste](#release-checkliste)
- [Versionsschema](#versionsschema)

---

## Vorbereitung

1. **Version festlegen** – Neue Versionsnummer nach dem Schema `v0.3.0` (siehe [Versionsschema](#versionsschema)).

2. **CHANGELOG aktualisieren** (falls vorhanden):
   ```markdown
   ## [0.3.0] – 2025-01-15
   ### Neu
   - Vollständiger Trigger-Editor
   - MIDI-Ausgabe in Pre/Post-Phase
   - Single-EXE Self-Contained Release
   ```

3. **Branch ist `main` und alle Änderungen sind eingecheckt:**
   ```powershell
   git status          # keine offenen Änderungen
   git log --oneline -5
   ```

4. **Build lokal prüfen:**
   ```powershell
   dotnet build Midi2InputDevice.slnx -c Release
   dotnet test
   ```

5. **Publish testen:**
   ```powershell
   dotnet publish MidiController.Frontend\MidiController.Frontend.csproj `
     /p:PublishProfile=SingleFile -c Release
   
   # Prüfen ob EXE da ist:
   dir MidiController.Frontend\bin\Release\publish\
   # Sollte MidiController.Frontend.exe enthalten
   ```

---

## Weg A: Automatisch via GitHub Actions

### Schritt 1 – GitHub Actions Workflow einrichten

Falls noch nicht vorhanden: [GitHub Actions Workflow einrichten](#github-actions-workflow-einrichten).

### Schritt 2 – Git-Tag setzen und pushen

```powershell
# Tag lokal setzen
git tag v0.3.0 -m "Release v0.3.0: Vollständiger Trigger-Editor, MIDI-Ausgabe, Single-EXE"

# Tag auf GitHub pushen (löst den Actions-Workflow aus)
git push origin v0.3.0
```

### Schritt 3 – GitHub Actions beobachten

1. Auf GitHub das Repository öffnen.
2. Reiter **Actions** → laufenden Workflow **„Build & Release"** anklicken.
3. Nach Abschluss erscheint das Release automatisch unter **Releases**.

### Schritt 4 – Release auf GitHub prüfen

1. Auf GitHub Reiter **Releases** öffnen.
2. Den neuen Release anklicken.
3. Prüfen:
   - Korrekter Tag und Titel (z. B. `v0.3.0`)
   - Dateianhang `MidiController-v0.3.0-win-x64.zip` vorhanden
   - ZIP enthält `MidiController.Frontend.exe`, `appsettings.json`, `appsettings.backend.json`

4. Optional: Release-Beschreibung manuell ergänzen (Klick auf **Edit**).

---

## Weg B: Manuell (ohne GitHub Actions)

### Schritt 1 – Single-File-EXE erstellen

```powershell
cd C:\Users\larsh\source\repos\danroyton\Midi2InputDevice

dotnet publish MidiController.Frontend\MidiController.Frontend.csproj `
  /p:PublishProfile=SingleFile -c Release
```

Ausgabe: `MidiController.Frontend\bin\Release\publish\`

### Schritt 2 – Release-ZIP packen

```powershell
$version = "v0.3.0"
$publishDir = "MidiController.Frontend\bin\Release\publish"
$zipName    = "MidiController-$version-win-x64.zip"

# Alle nötigen Dateien ins ZIP
Compress-Archive -Force `
  -Path "$publishDir\MidiController.Frontend.exe", `
        "$publishDir\appsettings.json", `
        "$publishDir\appsettings.backend.json" `
  -DestinationPath $zipName

Write-Host "ZIP erstellt: $zipName"
```

### Schritt 3 – Git-Tag setzen und pushen

```powershell
git tag v0.3.0 -m "Release v0.3.0"
git push origin v0.3.0
```

### Schritt 4 – GitHub Release anlegen

1. GitHub öffnen → Repository → Reiter **Releases** → **[Draft a new release]**

2. **„Choose a tag"**: Tag `v0.3.0` auswählen (den gerade gepushten).

3. **„Release title"** eintragen, z. B.:  
   `v0.3.0 – Vollständiger Trigger-Editor, MIDI-Ausgabe, Single-EXE`

4. **Beschreibung** eintragen (Markdown):
   ```markdown
   ## Was ist neu in v0.3.0?
   
   - ✅ Vollständiger Trigger-Editor (Pre/Post-Zuweisungen, Bedingungsblöcke, ELSE-Zweige)
   - ✅ MIDI-Ausgabe in Pre/Post-Phase (NoteOn, NoteOff, CC, ProgramChange, PitchBend)
   - ✅ Single-EXE Self-Contained Release (keine .NET-Installation nötig)
   - ✅ System-Tray blinkt nur bei tatsächlicher MIDI-Aktivität
   - ✅ Tastatur-Test-View
   
   ## Installation
   
   1. ZIP herunterladen und entpacken
   2. `MidiController.Frontend.exe` starten
   3. Kein Installer, keine .NET-Runtime erforderlich
   
   ## Hinweise
   
   - Benötigt Windows 10/11 (64-Bit)
   - Beim ersten Start ggf. SmartScreen-Warnung bestätigen
   ```

5. **„Attach binaries"**: Die vorher erstellte ZIP-Datei hochladen  
   (`MidiController-v0.3.0-win-x64.zip`).

6. **„Set as the latest release"** aktivieren.

7. Klick auf **[Publish release]**.

---

## GitHub Actions Workflow einrichten

Falls der Workflow `.github/workflows/release.yml` noch nicht vorhanden ist, diese Datei anlegen:

**Dateipfad:** `.github/workflows/release.yml`

```yaml
name: Build & Release

on:
  push:
    tags:
      - 'v*.*.*'

jobs:
  build-and-release:
    runs-on: windows-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: .NET 10 SDK einrichten
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Abhängigkeiten wiederherstellen
        run: dotnet restore Midi2InputDevice.slnx

      - name: Build
        run: dotnet build Midi2InputDevice.slnx -c Release --no-restore

      - name: Tests ausführen
        run: dotnet test Midi2InputDevice.slnx -c Release --no-build --verbosity normal

      - name: Single-File Publish
        run: |
          dotnet publish MidiController.Frontend/MidiController.Frontend.csproj `
            /p:PublishProfile=SingleFile -c Release

      - name: Release-ZIP erstellen
        shell: pwsh
        run: |
          $tag     = "${{ github.ref_name }}"
          $pubDir  = "MidiController.Frontend/bin/Release/publish"
          $zipName = "MidiController-$tag-win-x64.zip"
          Compress-Archive -Force `
            -Path "$pubDir/MidiController.Frontend.exe", `
                  "$pubDir/appsettings.json", `
                  "$pubDir/appsettings.backend.json" `
            -DestinationPath $zipName
          echo "ZIP_NAME=$zipName" >> $env:GITHUB_ENV

      - name: GitHub Release erstellen und ZIP hochladen
        uses: softprops/action-gh-release@v2
        with:
          files: ${{ env.ZIP_NAME }}
          generate_release_notes: true
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

### Workflow aktivieren

```powershell
# Workflow-Datei einzuchecken
git add .github/workflows/release.yml
git commit -m "ci: GitHub Actions Release-Workflow hinzufügen"
git push origin main
```

Ab jetzt löst jeder Tag `v*.*.*` einen automatischen Build und Release aus.

---

## Release-Checkliste

Vor jedem Release durchgehen:

- [ ] `main` ist aktuell (`git pull`)
- [ ] Keine uncommitteten Änderungen (`git status`)
- [ ] Build erfolgreich (`dotnet build -c Release`)
- [ ] Tests grün (`dotnet test`)
- [ ] Publish erfolgreich, EXE startet lokal
- [ ] CHANGELOG / Release-Beschreibung aktualisiert
- [ ] Git-Tag gesetzt und gepusht
- [ ] Release auf GitHub geprüft (Anhang vorhanden, Download-Link funktioniert)
- [ ] README.md zeigt die korrekte aktuelle Version

---

## Versionsschema

Das Projekt verwendet [Semantic Versioning](https://semver.org/):

```
v MAJOR . MINOR . PATCH
  │        │       └── Bugfix / kleine Korrekturen
  │        └────────── Neues Feature, rückwärtskompatibel
  └─────────────────── Inkompatible API-Änderung / großer Relaunch
```

Beispiele:
- `v0.3.0` → neues Feature-Release (Trigger-Editor)
- `v0.3.1` → Bugfix für v0.3.0
- `v0.4.0` → nächstes Feature-Release (virtuelle MIDI-Ports)
- `v1.0.0` → erster stabiler Windows-Dienst-Release

Tags immer im Format `v0.3.0` (mit `v`-Präfix) setzen – der Actions-Workflow erwartet dieses Format.
