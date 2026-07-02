# Windows Shutdown Timer (.NET / WinForms)

A rewrite of the [Python/CustomTkinter version](../python) as a native
Windows Forms app (`SleepPlanner`, .NET 8). Same feature set: shutdown/reminder
timer with countdown and progress bar, autostart via Task Scheduler, dark/light
theme, and the integrated sleep calculator with the sleep-cycle ring.

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Run from source

```bash
cd dotnet
dotnet run
```

The app requests administrator privileges on launch (see `app.manifest`) —
this is required for the `shutdown` command and for registering the
autostart task via `schtasks`.

## Build a standalone .exe

```bash
cd dotnet
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The executable is written to
`bin/Release/net8.0-windows/win-x64/publish/SleepPlanner.exe`.

For a framework-dependent build (smaller, requires the .NET 8 runtime on the
target machine), drop `--self-contained true` and `-r win-x64`:

```bash
dotnet publish -c Release
```

## Project layout

| File               | Purpose                                                        |
| ------------------ | ---------------------------------------------------------------|
| `Program.cs`        | Entry point (`Main`), starts the WinForms message loop.        |
| `MainForm.cs`       | UI: timer/reminder controls, countdown, sleep calculator, ring.|
| `Services.cs`       | `Native` (sleep prevention), `ShutdownService` (`shutdown` CLI), `AutostartManager` (`schtasks`), `TimeMath` helpers. |
| `app.manifest`      | Requests `requireAdministrator` execution level.                |
| `app.ico`           | Application icon (embedded resource + exe icon).                |

## Notes

- `bin/` and `obj/` are build output and are git-ignored — no manual cleanup
  needed.
- Autostart registers a Task Scheduler task (`SleepPlannerAutostart`) that
  runs at logon with the highest privileges, avoiding a UAC prompt on every
  boot.
