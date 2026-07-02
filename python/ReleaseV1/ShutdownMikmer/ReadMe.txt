# Windows Shutdown Timer

A simple Windows utility to automatically shut down your PC or remind you to go to sleep after a set amount of time.

This tool exists because Windows *technically* supports shutdown timers, but hides them behind command-line nonsense.
This app provides a clean GUI for a very real everyday problem:
**not leaving your PC running all night.**

START THIS TOOL WITH ADMINISTRATOR RIGHTS!


---

## Features

- ⏱️ Shutdown after a custom number of minutes
- 🔔 Reminder mode (no shutdown, just a notification)
- 📉 Live countdown with progress bar
- 🌙 Prevents automatic standby while the timer is running
- 🛑 Cancel scheduled shutdown at any time
- 💤 Mini sleep calculator with visual sleep cycle ring
- 🖥️ Clean, minimal GUI

---

## How it works

- Shutdown mode uses the native Windows `shutdown` command
- Reminder mode keeps the PC running and shows a notification
- While the timer is active, the app **prevents automatic standby**
- Manual actions (e.g. clicking *Energiesparen*) are **not overridden**

This is a deliberate design decision to keep the tool lightweight and predictable.

---

## Limitations (Important)

- If the PC is **manually** put into sleep mode, the timer will stop
- The app does **not** wake the PC from sleep
- Windows Defender / SmartScreen may warn about the `.exe` (unsigned binary)

These are known limitations and intentional for the first release.

---

## Installation (Recommended)

Download the prebuilt Windows executable from the releases page.

No Python installation required.

1. Download the ZIP
2. Extract it
3. Run `ShutdownMikmer.exe`

---

## Running from source (optional)

If you want to run it via Python:

```bash
pip install customtkinter
python app.py
