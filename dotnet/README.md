# SleepPlanner — C# / .NET (WinForms)

Eine schlanke Neufassung des Tools in C# (.NET 8, WinForms). Funktional
identisch zur Python-Version, aber als natives Windows-Programm.

## Features
- Shutdown nach X Minuten **oder** zu einer festen Uhrzeit (HH:MM)
- Reminder-Modus (kein Shutdown, nur Hinweisfenster)
- Live-Countdown (HH:MM:SS) mit Fortschrittsbalken
- Verhindert automatisches Standby (`SetThreadExecutionState` via P/Invoke)
- Abbrechen jederzeit (`shutdown /a`)
- Mini-Schlafrechner mit Schlafzyklen-Ring (90-min-Zyklen)
- Dark-/Light-Umschaltung
- Autostart-Schalter (Task Scheduler, startet beim Anmelden mit Adminrechten)

## Voraussetzungen zum Bauen
- .NET 8 SDK (Windows): https://dotnet.microsoft.com/download
- Gebaut und ausgefuehrt wird unter **Windows** (WinForms ist Windows-only).

## Bauen & starten (zum Testen)
```powershell
dotnet run -c Release
```

## Veroeffentlichen (kleine, saubere App)

Die kleinste Variante ist **framework-abhaengig** als eine einzelne .exe.
Sie braucht das ".NET Desktop Runtime 8" auf dem Zielrechner, ist dafuer aber
nur wenige hundert KB gross:

```powershell
dotnet publish -c Release -r win-x64 --self-contained false ^
  -p:PublishSingleFile=true
```
Ergebnis: `bin\Release\net8.0-windows\win-x64\publish\SleepPlanner.exe`

Wenn der Zielrechner **kein** .NET installiert haben soll, baue
self-contained (groesser, ca. 60-90 MB; mit Kompression etwas kleiner):

```powershell
dotnet publish -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

## Hinweise
- Die App fordert beim Start **Administratorrechte** an (siehe `app.manifest`).
  Das ist noetig fuer den `shutdown`-Befehl und die Autostart-Registrierung.
- Die SmartScreen-/Defender-Warnung verschwindet dadurch **nicht** — dafuer
  braucht es ein Code-Signing-Zertifikat. Das ist sprachunabhaengig.
- Autostart legt eine geplante Aufgabe namens `SleepPlannerAutostart` an.
  Pruefen/loeschen kannst du sie in `taskschd.msc`.

## Projektdateien
- `SleepPlanner.csproj` — Projektdefinition
- `app.manifest` — fordert Adminrechte an
- `Program.cs` — Einstiegspunkt
- `Services.cs` — Win32-Interop, shutdown-Befehl, Autostart, Zeit-Mathematik
- `MainForm.cs` — Oberflaeche und Ablauflogik
