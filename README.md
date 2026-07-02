# ⚡ Windows Shutdown Timer

<img width="641" height="772" alt="image" src="https://github.com/user-attachments/assets/3bc26b3d-7545-4b3f-9d76-1ac512d1a339" />

A sleek desktop app that schedules a Windows shutdown in X minutes, displays a live countdown, and allows you to cancel it anytime.

This repository is a **monorepo** containing two independent implementations of the same app:

| Implementation      | Path                  | Stack                     |
| -------------------- | ---------------------- | -------------------------- |
| Python (original)     | [`python/`](python)     | Python 3 + CustomTkinter   |
| .NET (rewrite)         | [`dotnet/`](dotnet)     | .NET 8 + WinForms          |

Each implementation has its own README with setup, usage, and build instructions:

- [`python/README.md`](python/README.md) — usage · [`python/BUILD.md`](python/BUILD.md) — packaging into a standalone `.exe`
- [`dotnet/README.md`](dotnet/README.md) — usage and publishing a standalone `.exe`

## Repository structure

```
.
├── python/     Python + CustomTkinter implementation
├── dotnet/     .NET 8 + WinForms implementation ("SleepPlanner")
└── webpage/    Standalone landing page (Firebase-hosted), unrelated to either app
```

---

# 🚀 Features

Both implementations share the same feature set:

| Feature                   | Description                               |
| ------------------------- | ------------------------------------------ |
| Shutdown Timer             | Schedules a Windows shutdown after N minutes or at a specific clock time |
| Reminder Mode              | Popup notification instead of shutdown    |
| Countdown & Progress       | Live HH:MM:SS countdown with a progress bar |
| Cancel Anytime             | Abort a scheduled shutdown with one click |
| Sleep Duration OptionMenu | Presets (6–10h) + Custom mode              |
| Custom Sleep Input        | Free input (int or float)                 |
| Sleep Result Calculations | Remaining sleep & bedtimes                |
| Sleep-Cycle Ring          | Visual cycles & time markers              |
| Light/Dark Theme          | Switchable UI theme                        |
| Autostart                 | Starts with Windows (Task Scheduler in the .NET version) |

---

## 🖥 Shutdown & Reminder System

### **Shutdown Timer**
- Schedule a system shutdown after a selected number of minutes (or a target clock time).
- Visual countdown timer with hours, minutes, and seconds.
- Progress bar that fills as the timer runs.
- Full support for cancelling scheduled shutdowns.

### **Reminder Mode (No Shutdown)**
- A dedicated **"Nur Erinnerung"** button schedules a reminder instead of shutting down the PC.
- When the timer expires:
  - A popup notification is displayed.
  - The PC remains running.
- Useful for sleep reminders, study timers, or break notifications.

---

## 💤 Integrated Sleep Calculator

A full mini sleep-assistant built directly into the GUI.

### **Sleep Duration Selection**
- OptionMenu with predefined sleep durations: **6, 7, 8, 9, 10 hours**
- **Custom mode**: enter any sleep duration (e.g., 2h, 7.5h, 14h)
- Custom input field appears automatically when "Custom" is selected.

### **Sleep Time & Remaining Sleep Calculation**
- Enter a wake-up time (HH:MM).
- The tool calculates:
  - How much sleep you get if you go to bed *now*
  - When you need to go to bed for the selected sleep duration
  - Alternative bedtimes (±1 hour)
- Supports integer and floating-point durations.

---

## ⏰ Sleep Cycle Visualization (Sleep-Cycle Ring)

A circular visual representation of your upcoming night.

### Includes:
- A 24-hour circular clock visualization.
- Red marker: **current time**
- Green marker: **wake-up time**
- Tick marks every **90 minutes** (sleep cycles)
- Center text "Sleep Cycles"

---

## 🎛 UI & Interaction Improvements

- Custom sleep-duration input shows/hides automatically depending on mode.
- Light/Dark theme switching.
- Stronger validation for all inputs.
- Improved messaging in the UI.

---

## 🖥️ Preview

> Minimalist GUI with a minute input field, Start/Cancel buttons, status text, countdown timer, progress bar, and the sleep-cycle ring calculator (see screenshot above).

---

## ⚠️ Safety & Responsibility

- This app actually shuts down your computer — save your work first.
- In corporate environments, admin policies may block shutdown commands.
- Depending on your system policies, you may need to run the app as Administrator to allow Windows to execute or cancel shutdown commands successfully.

## 🛠️ Roadmap

- 🔔 Optional notification before shutdown
- ☠️ "Force shutdown" checkbox (use with caution)
- 🌐 Multilanguage support (EN/DE via config)

## 📄 License

MIT License (no `LICENSE` file has been added to the repository yet).

## 👤 Author

Mikhail Zhivoderov
