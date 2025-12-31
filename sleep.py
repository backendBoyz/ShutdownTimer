import os
import sys
import subprocess
import customtkinter as ctk
import tkinter as tk  # für Canvas
import tkinter.messagebox as messagebox
from datetime import datetime, timedelta
import math


# --- Windows shutdown helpers ---
def schedule_shutdown(seconds: int) -> bool:
    try:
        subprocess.run(["shutdown", "/s", "/t", str(seconds)], check=True)
        return True
    except subprocess.CalledProcessError:
        return False


def abort_shutdown() -> bool:
    try:
        subprocess.run(["shutdown", "/a"], check=True)
        return True
    except subprocess.CalledProcessError:
        return False


# --- Hilfsfunktion für Ressourcenpfade (PyInstaller) ---
def resource_path(rel_path: str) -> str:
    base = getattr(sys, "_MEIPASS", os.path.abspath("."))
    return os.path.join(base, rel_path)


class ShutdownTimerApp(ctk.CTk):
    def __init__(self):
        super().__init__()
        self.title("Shutdown Timer")
        self.geometry("650x750")  # leicht höher
        ctk.set_appearance_mode("system")
        ctk.set_default_color_theme("dark-blue")

        # State
        self.remaining_seconds = 0
        self.total_seconds = 0
        self.countdown_running = False
        self.countdown_job = None  # after()-Job-ID
        self.current_mode = None   # "shutdown" oder "reminder"

        # ===== Header =====
        header = ctk.CTkFrame(self)
        header.pack(padx=12, pady=(12, 6), fill="x")

        title_label = ctk.CTkLabel(
            header, text="Windows Shutdown Timer", font=("Segoe UI", 18, "bold")
        )
        title_label.grid(row=0, column=0, sticky="w")

        current_dark = ctk.get_appearance_mode().lower() == "dark"
        self.dark_var = ctk.BooleanVar(value=current_dark)
        self.theme_switch = ctk.CTkSwitch(
            header,
            text="Dark Mode" if current_dark else "Light Mode",
            variable=self.dark_var,
            command=self.on_toggle_theme,
        )
        self.theme_switch.grid(row=0, column=1, sticky="e")
        header.grid_columnconfigure(0, weight=1)

        # ===== Input row =====
        self.input_frame = ctk.CTkFrame(self)
        self.input_frame.pack(pady=6, padx=12, fill="x")

        minutes_label = ctk.CTkLabel(self.input_frame, text="Minuten bis Aktion:")
        minutes_label.grid(row=0, column=0, padx=(12, 8), pady=12, sticky="w")

        self.minutes_entry = ctk.CTkEntry(self.input_frame, placeholder_text="z. B. 60")
        self.minutes_entry.grid(row=0, column=1, padx=(0, 12), pady=12, sticky="we")
        self.input_frame.grid_columnconfigure(1, weight=1)

        # ===== Buttons =====
        buttons_frame = ctk.CTkFrame(self)
        buttons_frame.pack(pady=4, padx=12, fill="x")

        self.start_button = ctk.CTkButton(
            buttons_frame, text="Shutdown starten", command=self.on_start_shutdown
        )
        self.start_button.grid(row=0, column=0, padx=6, pady=8, sticky="we")

        self.reminder_button = ctk.CTkButton(
            buttons_frame,
            text="Nur Erinnerung",
            fg_color="#444444",
            command=self.on_start_reminder,
        )
        self.reminder_button.grid(row=0, column=1, padx=6, pady=8, sticky="we")

        self.abort_button = ctk.CTkButton(
            buttons_frame, text="Abbrechen", fg_color="gray", command=self.on_abort
        )
        self.abort_button.grid(row=0, column=2, padx=6, pady=8, sticky="we")

        buttons_frame.grid_columnconfigure(0, weight=1)
        buttons_frame.grid_columnconfigure(1, weight=1)
        buttons_frame.grid_columnconfigure(2, weight=1)

        # ===== Status & Countdown =====
        self.status_label = ctk.CTkLabel(self, text="Keine Aktion geplant.", wraplength=420)
        self.status_label.pack(pady=(10, 0))

        self.countdown_label = ctk.CTkLabel(self, text="", font=("Consolas", 18))
        self.countdown_label.pack(pady=(6, 12))

        # Progressbar im Container
        self.progress_container = ctk.CTkFrame(self)
        self.progress_container.pack(pady=(4, 4), padx=40, fill="x")
        self.progress = ctk.CTkProgressBar(self.progress_container, mode="determinate")
        self.progress.pack(fill="x")
        self.progress.set(0.0)

        # ===== Mini-Schlafrechner =====
        self.sleep_frame = ctk.CTkFrame(self)
        self.sleep_frame.pack(pady=(6, 10), padx=12, fill="x")

        sleep_title = ctk.CTkLabel(
            self.sleep_frame, text="Mini-Schlafrechner", font=("Segoe UI", 14, "bold")
        )
        sleep_title.grid(row=0, column=0, columnspan=2, sticky="w", padx=8, pady=(8, 4))

        wake_label = ctk.CTkLabel(self.sleep_frame, text="Weckerzeit (HH:MM):")
        wake_label.grid(row=1, column=0, padx=(8, 4), pady=4, sticky="w")

        self.wakeup_entry = ctk.CTkEntry(self.sleep_frame, placeholder_text="z. B. 06:30")
        self.wakeup_entry.grid(row=1, column=1, padx=(4, 8), pady=4, sticky="we")

        # Auswahl Schlafdauer
        hours_label = ctk.CTkLabel(self.sleep_frame, text="Gewünschte Schlafdauer (h):")
        hours_label.grid(row=2, column=0, padx=(8, 4), pady=4, sticky="w")

        self.sleep_hours_mode_var = ctk.StringVar(value="8")
        self.sleep_hours_menu = ctk.CTkOptionMenu(
            self.sleep_frame,
            values=[str(i) for i in range(6, 11)] + ["Custom"],
            variable=self.sleep_hours_mode_var,
            command=self.on_sleep_hours_mode_change,
        )
        self.sleep_hours_menu.grid(row=2, column=1, padx=(4, 8), pady=4, sticky="we")

        # Custom-Eingabe für Schlafdauer (nur bei "Custom" sichtbar)
        self.custom_sleep_hours_entry = ctk.CTkEntry(
            self.sleep_frame,
            placeholder_text="Eigene Stunden, z. B. 7.5 oder 14",
        )
        # Start: versteckt
        # self.custom_sleep_hours_entry.grid(row=3, column=0, columnspan=2, padx=8, pady=(2, 4), sticky="we")

        self.sleep_button = ctk.CTkButton(
            self.sleep_frame, text="Restschlaf berechnen", command=self.on_calc_sleep
        )
        self.sleep_button.grid(row=4, column=0, columnspan=2, padx=8, pady=6, sticky="we")

        self.sleep_result_label = ctk.CTkLabel(
            self.sleep_frame,
            text="",
            wraplength=420,
            font=("Segoe UI", 11),
            justify="left",
        )
        self.sleep_result_label.grid(row=5, column=0, columnspan=2, padx=8, pady=(2, 8), sticky="w")

        # ===== Sleep-Cycle-Ring (Canvas) =====
        self.sleep_canvas = tk.Canvas(
            self.sleep_frame,
            width=220,
            height=220,
            bg="#1a1a1a",
            highlightthickness=0,
        )
        self.sleep_canvas.grid(row=6, column=0, columnspan=2, padx=8, pady=(0, 10))

        self.sleep_frame.grid_columnconfigure(1, weight=1)

        # ===== Footer =====
        hint_label = ctk.CTkLabel(
            self,
            text=(
                "Hinweis: Bei Berechtigungsproblemen als Administrator starten.\n"
                "Reminder-Modus lässt den PC an. Wenig schlafen bleibt trotzdem ungesund."
            ),
            font=("Segoe UI", 10),
            justify="center",
        )
        hint_label.pack(pady=(0, 8))

        # Enter startet Shutdown-Timer standardmäßig
        self.bind("<Return>", lambda _e: self.on_start_shutdown())

    # ===== Theme Toggle =====
    def on_toggle_theme(self):
        new_mode = "dark" if self.dark_var.get() else "light"
        ctk.set_appearance_mode(new_mode)
        self.theme_switch.configure(text="Dark Mode" if new_mode == "dark" else "Light Mode")

    # ===== Schlafdauer-Mode-Handler =====
    def on_sleep_hours_mode_change(self, value: str):
        # Wenn "Custom": Eingabefeld anzeigen, sonst verstecken
        if value == "Custom":
            self.custom_sleep_hours_entry.grid(
                row=3, column=0, columnspan=2, padx=8, pady=(2, 4), sticky="we"
            )
        else:
            self.custom_sleep_hours_entry.grid_forget()

    # ===== App-Logik: gemeinsamer Start =====
    def _parse_minutes(self) -> int | None:
        minutes_str = self.minutes_entry.get().strip()
        try:
            minutes = int(minutes_str)
        except ValueError:
            self.status_label.configure(text="Bitte eine ganze Zahl in Minuten eingeben.")
            return None

        if minutes < 1:
            self.status_label.configure(text="Mindestens 1 Minute, sonst wird das sinnlos hektisch.")
            return None

        return minutes

    def _start_common(self, minutes: int, mode: str):
        seconds = minutes * 60

        if mode == "shutdown":
            if not schedule_shutdown(seconds):
                self.status_label.configure(
                    text="Konnte Shutdown nicht planen. Starte die App ggf. als Administrator."
                )
                return

        # State setzen
        self.total_seconds = seconds
        self.remaining_seconds = seconds
        self.countdown_running = True
        self.current_mode = mode

        # UI sperren
        self.start_button.configure(state="disabled")
        self.reminder_button.configure(state="disabled")
        self.minutes_entry.configure(state="disabled")

        if mode == "shutdown":
            self.status_label.configure(text=f"Shutdown geplant in {minutes} Minute(n).")
        else:
            self.status_label.configure(text=f"Reminder geplant in {minutes} Minute(n).")

        # Progress zurücksetzen
        self.progress.set(0.0)

        # Alten Timer killen, falls vorhanden
        if self.countdown_job is not None:
            try:
                self.after_cancel(self.countdown_job)
            except Exception:
                pass
            self.countdown_job = None

        self.update_countdown()  # erster Tick

    def on_start_shutdown(self):
        if self.countdown_running:
            return
        minutes = self._parse_minutes()
        if minutes is None:
            return
        self._start_common(minutes, mode="shutdown")

    def on_start_reminder(self):
        if self.countdown_running:
            return
        minutes = self._parse_minutes()
        if minutes is None:
            return
        self._start_common(minutes, mode="reminder")

    def on_abort(self):
        if self.current_mode == "shutdown":
            aborted = abort_shutdown()
            if aborted:
                self.status_label.configure(text="Geplanter Shutdown abgebrochen.")
            else:
                self.status_label.configure(
                    text="Kein geplanter Shutdown gefunden oder Abbruch fehlgeschlagen."
                )
        elif self.current_mode == "reminder":
            self.status_label.configure(text="Geplanter Reminder abgebrochen.")
        else:
            self.status_label.configure(text="Keine laufende Aktion zum Abbrechen.")

        self._reset_ui()

    def update_countdown(self):
        if not self.countdown_running:
            return

        m, s = divmod(self.remaining_seconds, 60)
        h, m = divmod(m, 60)
        self.countdown_label.configure(text=f"{h:02d}:{m:02d}:{s:02d}")

        # Fortschritt 0.0–1.0
        if self.total_seconds > 0:
            progress_value = 1.0 - (self.remaining_seconds / self.total_seconds)
            progress_value = max(0.0, min(1.0, progress_value))
            self.progress.set(progress_value)

        if self.remaining_seconds <= 0:
            self.progress.set(1.0)

            if self.current_mode == "shutdown":
                self.status_label.configure(text="Shutdown steht unmittelbar bevor.")
            elif self.current_mode == "reminder":
                self.status_label.configure(
                    text="Reminder ausgelöst: Zeit schlafen zu gehen."
                )
                try:
                    messagebox.showinfo(
                        "Schlaf-Reminder",
                        "Bruder, es ist Zeit schlafen zu gehen.\n"
                        "Schlaf geht nicht weg, aber dein Hirn schon.",
                    )
                    self.lift()
                    self.focus_force()
                except Exception:
                    pass

            self.countdown_running = False
            self.current_mode = None
            self.start_button.configure(state="normal")
            self.reminder_button.configure(state="normal")
            self.minutes_entry.configure(state="normal")
            return

        self.remaining_seconds -= 1
        self.countdown_job = self.after(1000, self.update_countdown)

    # ===== Mini-Schlafrechner-Logik =====
    def on_calc_sleep(self):
        time_str = self.wakeup_entry.get().strip()
        if not time_str:
            self.sleep_result_label.configure(text="Bitte eine Weckerzeit im Format HH:MM eingeben.")
            self.sleep_canvas.delete("all")
            return

        # gewünschte Schlafdauer bestimmen
        mode = self.sleep_hours_mode_var.get()
        desired_hours = 8.0

        if mode == "Custom":
            raw = self.custom_sleep_hours_entry.get().strip()
            if not raw:
                self.sleep_result_label.configure(
                    text="Bitte eigene Schlafdauer eingeben oder eine feste Zahl wählen."
                )
                self.sleep_canvas.delete("all")
                return
            try:
                desired_hours = float(raw.replace(",", "."))
            except ValueError:
                self.sleep_result_label.configure(
                    text="Eigene Schlafdauer konnte nicht gelesen werden. Beispiel: 7.5"
                )
                self.sleep_canvas.delete("all")
                return
        else:
            try:
                desired_hours = float(mode)
            except ValueError:
                desired_hours = 8.0

        if desired_hours <= 0:
            self.sleep_result_label.configure(
                text="Schlafdauer <= 0h ist kreativ, aber nutzlos. Trag was Sinnvolles ein."
            )
            self.sleep_canvas.delete("all")
            return

        try:
            now = datetime.now()
            t = datetime.strptime(time_str, "%H:%M")
            wake = now.replace(hour=t.hour, minute=t.minute, second=0, microsecond=0)

            # Falls die Zeit heute schon vorbei ist → auf morgen schieben
            if wake <= now:
                wake += timedelta(days=1)

            diff = wake - now
            total_minutes = int(diff.total_seconds() // 60)
            hours = total_minutes // 60
            minutes = total_minutes % 60

            bed_main_dt = wake - timedelta(hours=desired_hours)
            bed_main = bed_main_dt.strftime("%H:%M")

            # +/- 1h Varianten nur, wenn das nicht komplett absurd ist
            bed_minus = None
            bed_plus = None
            if desired_hours >= 1:
                bed_minus = (wake - timedelta(hours=desired_hours - 1)).strftime("%H:%M")
            bed_plus = (wake - timedelta(hours=desired_hours + 1)).strftime("%H:%M")

            dh_str = f"{desired_hours:g}"

            text_lines = [
                f"Wenn du JETZT schlafen gehst, bekommst du ca. {hours}h {minutes}min Schlaf "
                f"bis {wake.strftime('%H:%M')}.",
                f"Für ~{dh_str}h Schlaf: spätestens um {bed_main} ins Bett.",
            ]

            if bed_minus:
                text_lines.append(
                    f"Alternativ: {bed_minus} (~{desired_hours - 1:g}h) "
                    f"oder {bed_plus} (~{desired_hours + 1:g}h)."
                )
            else:
                text_lines.append(
                    f"Alternativ: {bed_plus} (~{desired_hours + 1:g}h)."
                )

            self.sleep_result_label.configure(text="\n".join(text_lines))

            # Sleep-Cycle-Ring zeichnen
            self.draw_sleep_cycle_ring(now, wake)
        except ValueError:
            self.sleep_result_label.configure(
                text="Zeit konnte nicht gelesen werden. Bitte HH:MM verwenden, z. B. 06:30."
            )
            self.sleep_canvas.delete("all")

    # ===== Sleep-Cycle-Ring Zeichnung =====
    def draw_sleep_cycle_ring(self, now: datetime, wake: datetime):
        c = self.sleep_canvas
        c.delete("all")

        w = int(c.cget("width"))
        h = int(c.cget("height"))
        cx, cy = w / 2, h / 2
        r_outer = min(w, h) / 2 - 10
        r_inner = r_outer - 15

        # Hintergrundkreis
        c.create_oval(
            cx - r_outer,
            cy - r_outer,
            cx + r_outer,
            cy + r_outer,
            outline="#555555",
            width=2,
            )

        # Hilfsfunktion: Zeit -> Winkel (24h-Kreis)
        def time_to_angle(dt: datetime) -> float:
            minutes = dt.hour * 60 + dt.minute
            frac = minutes / (24 * 60)  # Anteil des Tages
            return (frac * 360.0) - 90.0  # -90 = oben

        now_norm = now
        wake_norm = wake

        # Marker: JETZT (rot)
        self._draw_marker(c, cx, cy, r_inner, r_outer, time_to_angle(now_norm), color="#ff5555")

        # Marker: WECKER (grün)
        self._draw_marker(c, cx, cy, r_inner, r_outer, time_to_angle(wake_norm), color="#55ff55")

        # Schlafzyklen (90-Minuten-Schritte zwischen jetzt und Wecker)
        total_minutes = int((wake - now).total_seconds() // 60)
        num_cycles = max(1, total_minutes // 90)

        for i in range(num_cycles + 1):
            t = now + timedelta(minutes=90 * i)
            if t > wake:
                break
            angle = time_to_angle(t)
            self._draw_cycle_tick(c, cx, cy, r_inner + 5, r_outer - 5, angle, color="#aaaaaa")

        # Text in der Mitte
        c.create_text(
            cx,
            cy,
            text="Sleep\nCycles",
            fill="#dddddd",
            font=("Segoe UI", 10, "bold"),
            justify="center",
        )

    def _draw_marker(self, canvas, cx, cy, r_inner, r_outer, angle_deg, color="#ffffff"):
        angle_rad = math.radians(angle_deg)
        x1 = cx + r_inner * math.cos(angle_rad)
        y1 = cy + r_inner * math.sin(angle_rad)
        x2 = cx + r_outer * math.cos(angle_rad)
        y2 = cy + r_outer * math.sin(angle_rad)
        canvas.create_line(x1, y1, x2, y2, fill=color, width=3)

        canvas.create_oval(
            x2 - 3, y2 - 3, x2 + 3, y2 + 3,
            fill=color, outline=color
        )

    def _draw_cycle_tick(self, canvas, cx, cy, r_inner, r_outer, angle_deg, color="#aaaaaa"):
        angle_rad = math.radians(angle_deg)
        x1 = cx + r_inner * math.cos(angle_rad)
        y1 = cy + r_inner * math.sin(angle_rad)
        x2 = cx + r_outer * math.cos(angle_rad)
        y2 = cy + r_outer * math.sin(angle_rad)
        canvas.create_line(x1, y1, x2, y2, fill=color, width=1)

    # ===== Helpers =====
    def _reset_ui(self):
        self.countdown_running = False
        self.current_mode = None
        if self.countdown_job is not None:
            try:
                self.after_cancel(self.countdown_job)
            except Exception:
                pass
            self.countdown_job = None

        self.countdown_label.configure(text="")
        self.start_button.configure(state="normal")
        self.reminder_button.configure(state="normal")
        self.minutes_entry.configure(state="normal")
        self.progress.set(0.0)


if __name__ == "__main__":
    app = ShutdownTimerApp()
    app.mainloop()
