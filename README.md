# ⚡ Shutdown Timer (Windows, Python + CustomTkinter)

<img width="641" height="772" alt="image" src="https://github.com/user-attachments/assets/3bc26b3d-7545-4b3f-9d76-1ac512d1a339" />


A sleek desktop app that schedules a Windows shutdown in X minutes, displays a live countdown, and allows you to cancel it anytime.  
Built with [CustomTkinter].

| Feature                   | Description                               |
| ------------------------- | ----------------------------------------- |
| Reminder Mode             | Popup notification instead of shutdown    |
| Sleep Duration OptionMenu | Presets + Custom mode                     |
| Custom Sleep Input        | Free input (int or float)                 |
| Sleep Result Calculations | Remaining sleep & bedtimes                |
| Sleep-Cycle Ring          | Visual cycles & time markers              |
| UI Enhancements           | Improved layout, theme switch, validation |
| Internal Logic            | More robust time & cycle handling         |


---

# 🚀 Features Added in the Latest Version

This version significantly expands the application beyond a simple shutdown timer.  
Below is a structured summary of all new features added.

---

## 🖥 Shutdown & Reminder System

### **Shutdown Timer**
- Schedule a system shutdown after a selected number of minutes.
- Visual countdown timer with hours, minutes, and seconds.
- Progress bar that fills as the timer runs.
- Full support for cancelling scheduled shutdowns.

### **Reminder Mode (No Shutdown)**
- New button **“Nur Erinnerung”** that schedules a reminder instead of shutting down the PC.
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
- Custom input field appears automatically when “Custom” is selected.

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
- Center text “Sleep Cycles”

---

## 🎛 UI & Interaction Improvements

- Custom sleep-duration input shows/hides automatically depending on mode.
- Resized window to support new layout.
- Light/Dark theme switching.
- Stronger validation for all inputs.
- Improved messaging in the UI.

---

## 🔧 Technical Notes

- Built with CustomTkinter + Tkinter Canvas.
- Reminder notifications use `tkinter.messagebox`.
- For PyInstaller builds (no console window):
  ```bash
  pyinstaller -w --noconsole your_script.py


## ⚙️ Features

- ⏱️ Countdown in HH:MM:SS format with a progress bar  
- 🌗 Switchable Light/Dark Mode  
- ⌨️ Press **Enter** to start immediately  
- 🛑 Cancel a scheduled shutdown with one click  
- 🔧 PyInstaller-friendly resource paths (`resource_path`)  
- 🪟 Uses native Windows commands (`shutdown /s /t` and `shutdown /a`)

---

## 🖥️ Preview

> Minimalist GUI with a minute input field, Start/Cancel buttons, status text, countdown timer, and progress bar.

---

## 📋 Requirements

- **OS:** Windows 10 or 11  
- **Python:** 3.9+ (3.11 recommended)  
- **Dependencies:**
  - `customtkinter`  
  - `tkinter` (included in standard Python installations on Windows)

---

## 🚀 Installation

```bash
# 1️⃣ Clone the repository
git clone https://github.com/YOUR-USERNAME/YOUR-REPO.git
cd YOUR-REPO

# 2️⃣ (Optional) Create a virtual environment
python -m venv .venv
.venv\Scripts\activate

# 3️⃣ Install dependencies
pip install customtkinter
```
▶️ Usage
```
python main.py
```

Enter the number of minutes until shutdown (e.g. 60).

Click Start to schedule the shutdown.

The countdown and progress bar will start running.

Click Cancel to abort the planned shutdown.

Keyboard shortcut:

Enter starts the timer.

💡 Note: Depending on your system policies, you may need to run the app as Administrator
to allow Windows to execute or cancel shutdown commands successfully.

🧱 Building an Executable (PyInstaller)

The script is fully prepared for PyInstaller (resource_path support).
To create a single-file executable:
```
pip install pyinstaller

pyinstaller ^
  --name "ShutdownTimer" ^
  --onefile ^
  --noconsole ^
  main.py

```
The built executable will be located in:

dist/ShutdownTimer.exe

🔨 Build Notes

If you add extra resources, include them with:

```
--add-data "path;."
```
If Windows Defender flags the EXE, sign it or exclude it manually
(false positives are common).

| Component               | Description                                                                |
| ----------------------- | -------------------------------------------------------------------------- |
| **GUI**                 | `customtkinter`                                                            |
| **Shutdown scheduling** | `subprocess.run(["shutdown", "/s", "/t", <seconds>], check=True)`          |
| **Shutdown abort**      | `subprocess.run(["shutdown", "/a"], check=True)`                           |
| **Countdown**           | Uses `after(1000, ...)` for 1-second updates                               |
| **Progress**            | 0.0–1.0 based on remaining time                                            |
| **State management**    | `remaining_seconds`, `total_seconds`, `countdown_running`, `countdown_job` |


🧩 Troubleshooting
“Could not schedule shutdown”

Run the app as Administrator.

Check Group Policies — shutdown may be restricted.

“No scheduled shutdown found” when canceling

No active timer or another process modified the shutdown schedule.

Countdown not updating

Verify after() isn’t called multiple times.
This code safely cancels old jobs before starting new ones.

⚠️ Safety & Responsibility

This app actually shuts down your computer — save your work first.

In corporate environments, admin policies may block shutdown commands.

🛠️ Roadmap

🔔 Optional notification before shutdown

☠️ “Force shutdown” checkbox (use with caution)

🌐 Multilanguage support (EN/DE via config)

📄 License

MIT License
See LICENSE
 for details.

👤 Author

Mikhail Zhivoderov
