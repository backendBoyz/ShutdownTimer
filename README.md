# ⚡ Windows Shutdown Timer

A desktop app that schedules a Windows shutdown (or a reminder) after X
minutes, shows a live countdown, and includes a small sleep-cycle
calculator. This repository is a monorepo containing two independent
implementations of the same app:

| Implementation     | Path                 | Stack                     |
| ------------------- | --------------------- | -------------------------- |
| Python (original)    | [`python/`](python)    | Python 3 + CustomTkinter   |
| .NET (rewrite)        | [`dotnet/`](dotnet)    | .NET 8 + WinForms          |

Each implementation has its own README with setup, usage, and build
instructions:

- [`python/README.md`](python/README.md) — usage · [`python/BUILD.md`](python/BUILD.md) — packaging into a standalone `.exe`
- [`dotnet/README.md`](dotnet/README.md) — usage and publishing a standalone `.exe`

## Repository structure

```
.
├── python/     Python + CustomTkinter implementation
├── dotnet/     .NET 8 + WinForms implementation ("SleepPlanner")
└── webpage/    Standalone landing page (Firebase-hosted), unrelated to either app
```

## Features (both versions)

- Schedule a shutdown or a reminder-only popup after N minutes or at a
  specific clock time.
- Live countdown (HH:MM:SS) with a progress bar.
- Cancel a scheduled shutdown at any time.
- Light/Dark theme toggle.
- Autostart with Windows (Task Scheduler in the .NET version).
- Integrated mini sleep calculator with a sleep-cycle ring visualization.

## License

MIT License (no `LICENSE` file has been added to the repository yet).

## Author

Mikhail Zhivoderov
