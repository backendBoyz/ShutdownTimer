using System.Globalization;

namespace SleepPlanner;

/// <summary>Panel mit Doppelpufferung gegen Flackern beim Neuzeichnen.</summary>
internal sealed class BufferedPanel : Panel
{
    public BufferedPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }
}

internal sealed class MainForm : Form
{
    // --- Eingaben / Buttons -------------------------------------------------
    private readonly TextBox _minutesBox = new();
    private readonly TextBox _timeBox = new();
    private readonly Button _startButton = new();
    private readonly Button _reminderButton = new();
    private readonly Button _abortButton = new();

    // --- Status / Countdown -------------------------------------------------
    private readonly Label _statusLabel = new();
    private readonly Label _countdownLabel = new();
    private readonly ProgressBar _progress = new();

    // --- Schlafrechner ------------------------------------------------------
    private readonly TextBox _wakeBox = new();
    private readonly ComboBox _hoursCombo = new();
    private readonly TextBox _customHoursBox = new();
    private readonly Button _calcButton = new();
    private readonly Label _sleepResultLabel = new();
    private readonly BufferedPanel _ringPanel = new();

    // --- Schalter -----------------------------------------------------------
    private readonly CheckBox _darkToggle = new();
    private readonly CheckBox _autostartToggle = new();

    // --- Zustand ------------------------------------------------------------
    private readonly System.Windows.Forms.Timer _countdownTimer = new() { Interval = 1000 };
    private int _remainingSeconds;
    private int _totalSeconds;
    private bool _countdownRunning;
    private string? _currentMode;
    private readonly bool _preventSleepEnabled = true;
    private readonly bool _keepDisplayOn = false;
    private bool _suppressAutostartEvent;

    // --- Ring-Zeichendaten --------------------------------------------------
    private bool _hasRing;
    private DateTime _ringNow;
    private DateTime _ringWake;

    public MainForm()
    {
        Text = "Windows Shutdown Timer";
        ClientSize = new Size(500, 760);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(460, 700);
        Font = new Font("Segoe UI", 9f);

        LoadAppIcon();

        BuildUi();

        _countdownTimer.Tick += (_, _) => CountdownTick();

        // Enter loest Shutdown aus (wie in der Python-Version).
        AcceptButton = _startButton;

        ApplyTheme(IsSystemDark());
    }

    // ======================================================================
    //  UI-Aufbau
    // ======================================================================
    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoScroll = true,
            Padding = new Padding(12),
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // ---- Kopfbereich --------------------------------------------------
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 8),
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var title = new Label
        {
            Text = "Windows Shutdown Timer",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 4, 0, 0),
        };

        _darkToggle.Text = "Dark Mode";
        _darkToggle.AutoSize = true;
        _darkToggle.Anchor = AnchorStyles.Right;
        _darkToggle.CheckedChanged += (_, _) => ApplyTheme(_darkToggle.Checked);

        header.Controls.Add(title, 0, 0);
        header.Controls.Add(_darkToggle, 1, 0);

        _autostartToggle.Text = "Mit Windows starten";
        _autostartToggle.AutoSize = true;
        _autostartToggle.Margin = new Padding(0, 4, 0, 0);
        _suppressAutostartEvent = true;
        _autostartToggle.Checked = AutostartManager.IsEnabled();
        _suppressAutostartEvent = false;
        _autostartToggle.CheckedChanged += OnToggleAutostart;
        header.Controls.Add(_autostartToggle, 0, 1);
        header.SetColumnSpan(_autostartToggle, 2);

        root.Controls.Add(header);

        // ---- Eingabebereich (Minuten + Uhrzeit) ---------------------------
        var input = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(8),
        };
        input.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        input.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _minutesBox.PlaceholderText = "z. B. 60";
        _minutesBox.Dock = DockStyle.Fill;
        _timeBox.PlaceholderText = "z. B. 23:30";
        _timeBox.Dock = DockStyle.Fill;

        input.Controls.Add(MakeLabel("Minuten bis Aktion:"), 0, 0);
        input.Controls.Add(_minutesBox, 1, 0);
        input.Controls.Add(MakeLabel("Oder Uhrzeit (HH:MM):"), 0, 1);
        input.Controls.Add(_timeBox, 1, 1);
        root.Controls.Add(input);

        // ---- Buttons ------------------------------------------------------
        var buttons = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 8),
        };
        for (int i = 0; i < 3; i++)
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

        StyleButton(_startButton, "Shutdown starten");
        _startButton.Click += (_, _) => OnStart("shutdown");
        StyleButton(_reminderButton, "Nur Erinnerung");
        _reminderButton.Click += (_, _) => OnStart("reminder");
        StyleButton(_abortButton, "Abbrechen");
        _abortButton.Click += (_, _) => OnAbort();

        buttons.Controls.Add(_startButton, 0, 0);
        buttons.Controls.Add(_reminderButton, 1, 0);
        buttons.Controls.Add(_abortButton, 2, 0);
        root.Controls.Add(buttons);

        // ---- Status + Countdown + Fortschritt -----------------------------
        _statusLabel.Text = "Keine Aktion geplant.";
        _statusLabel.AutoSize = true;
        _statusLabel.MaximumSize = new Size(440, 0);
        _statusLabel.Margin = new Padding(0, 6, 0, 0);
        _statusLabel.Anchor = AnchorStyles.None;
        root.Controls.Add(_statusLabel);

        _countdownLabel.Text = "";
        _countdownLabel.Font = new Font("Consolas", 18f);
        _countdownLabel.AutoSize = true;
        _countdownLabel.Anchor = AnchorStyles.None;
        _countdownLabel.Margin = new Padding(0, 6, 0, 6);
        root.Controls.Add(_countdownLabel);

        _progress.Dock = DockStyle.Top;
        _progress.Minimum = 0;
        _progress.Maximum = 100;
        _progress.Value = 0;
        _progress.Height = 16;
        _progress.Margin = new Padding(28, 4, 28, 10);
        _progress.Style = ProgressBarStyle.Continuous;
        root.Controls.Add(_progress);

        // ---- Schlafrechner ------------------------------------------------
        var sleep = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(8),
        };
        sleep.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        sleep.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var sleepTitle = new Label
        {
            Text = "Mini-Schlafrechner",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 4, 0, 6),
        };
        sleep.Controls.Add(sleepTitle, 0, 0);
        sleep.SetColumnSpan(sleepTitle, 2);

        _wakeBox.PlaceholderText = "z. B. 06:30";
        _wakeBox.Dock = DockStyle.Fill;
        sleep.Controls.Add(MakeLabel("Weckerzeit (HH:MM):"), 0, 1);
        sleep.Controls.Add(_wakeBox, 1, 1);

        _hoursCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _hoursCombo.Dock = DockStyle.Fill;
        for (int hrs = 6; hrs <= 10; hrs++)
            _hoursCombo.Items.Add(hrs.ToString(CultureInfo.InvariantCulture));
        _hoursCombo.Items.Add("Custom");
        _hoursCombo.SelectedItem = "8";
        _hoursCombo.SelectedIndexChanged += OnHoursModeChanged;
        sleep.Controls.Add(MakeLabel("Gewuenschte Schlafdauer (h):"), 0, 2);
        sleep.Controls.Add(_hoursCombo, 1, 2);

        _customHoursBox.PlaceholderText = "Eigene Stunden, z. B. 7.5 oder 14";
        _customHoursBox.Dock = DockStyle.Fill;
        _customHoursBox.Visible = false;
        sleep.Controls.Add(_customHoursBox, 1, 3);

        StyleButton(_calcButton, "Restschlaf berechnen");
        _calcButton.Click += (_, _) => OnCalcSleep();
        _calcButton.Margin = new Padding(3, 8, 3, 6);
        sleep.Controls.Add(_calcButton, 0, 4);
        sleep.SetColumnSpan(_calcButton, 2);

        _sleepResultLabel.Text = "";
        _sleepResultLabel.AutoSize = true;
        _sleepResultLabel.MaximumSize = new Size(440, 0);
        _sleepResultLabel.Margin = new Padding(0, 2, 0, 8);
        sleep.Controls.Add(_sleepResultLabel, 0, 5);
        sleep.SetColumnSpan(_sleepResultLabel, 2);

        _ringPanel.Size = new Size(220, 220);
        _ringPanel.BackColor = ColorTranslator.FromHtml("#1a1a1a");
        _ringPanel.Anchor = AnchorStyles.None;
        _ringPanel.Margin = new Padding(0, 0, 0, 10);
        _ringPanel.Paint += DrawSleepCycleRing;
        sleep.Controls.Add(_ringPanel, 0, 6);
        sleep.SetColumnSpan(_ringPanel, 2);

        root.Controls.Add(sleep);

        // ---- Hinweis ------------------------------------------------------
        var hint = new Label
        {
            Text = "Hinweis: Bei Berechtigungsproblemen als Administrator starten.\n" +
                   "Reminder-Modus laesst den PC an. Wenig schlafen bleibt trotzdem ungesund.",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleCenter,
            Anchor = AnchorStyles.None,
            Font = new Font("Segoe UI", 8.5f),
            Margin = new Padding(0, 4, 0, 8),
        };
        root.Controls.Add(hint);

        Controls.Add(root);
    }

    private void LoadAppIcon()
    {
        try
        {
            using var stream = System.Reflection.Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("SleepPlanner.app.ico");
            if (stream is not null)
                Icon = new Icon(stream);
        }
        catch
        {
            // Kein Icon gefunden -> Standardicon verwenden.
        }
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(3, 7, 8, 3),
    };

    private static void StyleButton(Button b, string text)
    {
        b.Text = text;
        b.Dock = DockStyle.Fill;
        b.Height = 34;
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.Margin = new Padding(3, 0, 3, 0);
        b.UseVisualStyleBackColor = false;
    }

    // ======================================================================
    //  Theme
    // ======================================================================
    private static bool IsSystemDark()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var v = key?.GetValue("AppsUseLightTheme");
            if (v is int i) return i == 0;
        }
        catch
        {
            // ignorieren -> Standard unten
        }
        return true; // Default: dunkel
    }

    private void ApplyTheme(bool dark)
    {
        _darkToggle.Text = dark ? "Dark Mode" : "Light Mode";
        if (_darkToggle.Checked != dark) _darkToggle.Checked = dark;

        Color bg = dark ? ColorTranslator.FromHtml("#1e1e1e") : ColorTranslator.FromHtml("#f0f0f0");
        Color panelBg = dark ? ColorTranslator.FromHtml("#2b2b2b") : ColorTranslator.FromHtml("#e6e6e6");
        Color fg = dark ? Color.White : Color.Black;
        Color inputBg = dark ? ColorTranslator.FromHtml("#3a3a3a") : Color.White;

        BackColor = bg;
        ForeColor = fg;
        ApplyThemeRecursive(this, fg, panelBg, inputBg);

        // Akzentfarben der Buttons setzen (unabhaengig vom Theme).
        SetButtonColors(_startButton, ColorTranslator.FromHtml("#1f6aa5"), Color.White);
        SetButtonColors(_reminderButton, ColorTranslator.FromHtml("#444444"), Color.White);
        SetButtonColors(_abortButton, ColorTranslator.FromHtml("#6e6e6e"), Color.White);
        SetButtonColors(_calcButton, ColorTranslator.FromHtml("#1f6aa5"), Color.White);
    }

    private void ApplyThemeRecursive(Control parent, Color fg, Color panelBg, Color inputBg)
    {
        foreach (Control c in parent.Controls)
        {
            switch (c)
            {
                case Button:
                    break; // separat behandelt
                case TextBox tb:
                    tb.BackColor = inputBg;
                    tb.ForeColor = fg;
                    tb.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case ComboBox cb:
                    cb.BackColor = inputBg;
                    cb.ForeColor = fg;
                    cb.FlatStyle = FlatStyle.Flat;
                    break;
                case ProgressBar:
                    break;
                case Label or CheckBox:
                    c.ForeColor = fg;
                    c.BackColor = Color.Transparent;
                    break;
                case BufferedPanel:
                    break; // Ring behaelt seinen dunklen Hintergrund
                case TableLayoutPanel:
                    c.BackColor = Color.Transparent;
                    c.ForeColor = fg;
                    break;
                default:
                    c.ForeColor = fg;
                    break;
            }

            if (c.HasChildren)
                ApplyThemeRecursive(c, fg, panelBg, inputBg);
        }
    }

    private static void SetButtonColors(Button b, Color back, Color fore)
    {
        b.BackColor = back;
        b.ForeColor = fore;
        b.FlatAppearance.MouseOverBackColor = ControlPaint.Light(back);
        b.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(back);
    }

    // ======================================================================
    //  Autostart
    // ======================================================================
    private void OnToggleAutostart(object? sender, EventArgs e)
    {
        if (_suppressAutostartEvent) return;

        if (_autostartToggle.Checked)
        {
            if (AutostartManager.Enable())
            {
                _statusLabel.Text = "Autostart aktiviert (startet beim Anmelden mit Administratorrechten).";
            }
            else
            {
                _statusLabel.Text = "Autostart konnte nicht aktiviert werden. Starte die App als Administrator.";
                _suppressAutostartEvent = true;
                _autostartToggle.Checked = false;
                _suppressAutostartEvent = false;
            }
        }
        else
        {
            _statusLabel.Text = AutostartManager.Disable()
                ? "Autostart deaktiviert."
                : "Autostart konnte nicht deaktiviert werden.";
        }
    }

    // ======================================================================
    //  Schlafdauer-Auswahl
    // ======================================================================
    private void OnHoursModeChanged(object? sender, EventArgs e)
    {
        _customHoursBox.Visible = (_hoursCombo.SelectedItem as string) == "Custom";
    }

    // ======================================================================
    //  Eingabe -> Minuten (Uhrzeit-Feld hat Vorrang)
    // ======================================================================
    private int? ParseMinutes()
    {
        string timeStr = _timeBox.Text.Trim();
        if (timeStr.Length > 0)
        {
            if (!TimeMath.TryParseHhmm(timeStr, out int hh, out int mm))
            {
                _statusLabel.Text = "Bitte eine Uhrzeit im Format HH:MM eingeben.";
                return null;
            }
            var now = DateTime.Now;
            var target = TimeMath.NextOccurrence(now, hh, mm);
            int minutes = (int)((target - now).TotalSeconds / 60);
            return Math.Max(1, minutes);
        }

        string minutesStr = _minutesBox.Text.Trim();
        if (!int.TryParse(minutesStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            _statusLabel.Text = "Bitte eine ganze Zahl in Minuten eingeben.";
            return null;
        }
        if (parsed < 1)
        {
            _statusLabel.Text = "Mindestens 1 Minute, sonst wird das sinnlos hektisch.";
            return null;
        }
        return parsed;
    }

    // ======================================================================
    //  Start / Abbruch
    // ======================================================================
    private void OnStart(string mode)
    {
        if (_countdownRunning) return;
        int? minutes = ParseMinutes();
        if (minutes is null) return;
        StartCommon(minutes.Value, mode);
    }

    private void StartCommon(int minutes, string mode)
    {
        int seconds = minutes * 60;

        if (mode == "shutdown" && !ShutdownService.Schedule(seconds))
        {
            _statusLabel.Text = "Konnte Shutdown nicht planen. Starte die App ggf. als Administrator.";
            return;
        }

        _totalSeconds = seconds;
        _remainingSeconds = seconds;
        _countdownRunning = true;
        _currentMode = mode;

        if (_preventSleepEnabled && !Native.PreventSleep(_keepDisplayOn))
        {
            _statusLabel.Text = "Hinweis: Konnte Standby-Blocker nicht setzen. Timer kann bei Standby haengen bleiben.";
        }

        _startButton.Enabled = false;
        _reminderButton.Enabled = false;
        _minutesBox.Enabled = false;
        _timeBox.Enabled = false;

        string targetClock = DateTime.Now.AddSeconds(seconds).ToString("HH:mm", CultureInfo.InvariantCulture);
        _statusLabel.Text = mode == "shutdown"
            ? $"Shutdown geplant in {minutes} Minute(n) (um {targetClock} Uhr)."
            : $"Reminder geplant in {minutes} Minute(n) (um {targetClock} Uhr).";

        _progress.Value = 0;

        _countdownTimer.Stop();
        _countdownTimer.Start();
        CountdownTick(); // sofort einmal anzeigen
    }

    private void OnAbort()
    {
        if (_currentMode == "shutdown")
        {
            _statusLabel.Text = ShutdownService.Abort()
                ? "Geplanter Shutdown abgebrochen."
                : "Kein geplanter Shutdown gefunden oder Abbruch fehlgeschlagen.";
        }
        else if (_currentMode == "reminder")
        {
            _statusLabel.Text = "Geplanter Reminder abgebrochen.";
        }
        else
        {
            _statusLabel.Text = "Keine laufende Aktion zum Abbrechen.";
        }
        ResetUi();
    }

    // ======================================================================
    //  Countdown
    // ======================================================================
    private void CountdownTick()
    {
        if (!_countdownRunning) return;

        int rem = _remainingSeconds;
        int s = rem % 60;
        int totalMin = rem / 60;
        int h = totalMin / 60;
        int m = totalMin % 60;
        _countdownLabel.Text = $"{h:D2}:{m:D2}:{s:D2}";

        if (_totalSeconds > 0)
        {
            double pv = 1.0 - (double)_remainingSeconds / _totalSeconds;
            pv = Math.Max(0.0, Math.Min(1.0, pv));
            _progress.Value = (int)Math.Round(pv * 100);
        }

        if (_remainingSeconds <= 0)
        {
            _progress.Value = 100;
            if (_currentMode == "shutdown")
            {
                _statusLabel.Text = "Shutdown steht unmittelbar bevor.";
            }
            else if (_currentMode == "reminder")
            {
                _statusLabel.Text = "Reminder ausgeloest: Zeit schlafen zu gehen.";
                try
                {
                    TopMost = true;
                    BringToFront();
                    MessageBox.Show(
                        this,
                        "Bruder, es ist Zeit schlafen zu gehen.\n" +
                        "Schlaf geht nicht weg, aber dein Hirn schon.",
                        "Schlaf-Reminder",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    TopMost = false;
                    Activate();
                }
                catch
                {
                    // ignorieren
                }
            }
            ResetUi();
            return;
        }

        _remainingSeconds -= 1;
    }

    private void ResetUi()
    {
        _countdownRunning = false;
        _currentMode = null;
        _countdownTimer.Stop();
        _countdownLabel.Text = "";
        _startButton.Enabled = true;
        _reminderButton.Enabled = true;
        _minutesBox.Enabled = true;
        _timeBox.Enabled = true;
        _progress.Value = 0;
        if (_preventSleepEnabled) Native.AllowSleep();
    }

    // ======================================================================
    //  Schlafrechner
    // ======================================================================
    private void OnCalcSleep()
    {
        string timeStr = _wakeBox.Text.Trim();
        if (timeStr.Length == 0)
        {
            _sleepResultLabel.Text = "Bitte eine Weckerzeit im Format HH:MM eingeben.";
            ClearRing();
            return;
        }

        string mode = (_hoursCombo.SelectedItem as string) ?? "8";
        double desiredHours = 8;
        if (mode == "Custom")
        {
            string raw = _customHoursBox.Text.Trim();
            if (raw.Length == 0)
            {
                _sleepResultLabel.Text = "Bitte eigene Schlafdauer eingeben oder eine feste Zahl waehlen.";
                ClearRing();
                return;
            }
            if (!double.TryParse(raw.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out desiredHours))
            {
                _sleepResultLabel.Text = "Eigene Schlafdauer konnte nicht gelesen werden. Beispiel: 7.5";
                ClearRing();
                return;
            }
        }
        else
        {
            if (!double.TryParse(mode, NumberStyles.Float, CultureInfo.InvariantCulture, out desiredHours))
                desiredHours = 8;
        }

        if (desiredHours <= 0)
        {
            _sleepResultLabel.Text = "Schlafdauer <= 0h ist kreativ, aber nutzlos. Trag was Sinnvolles ein.";
            ClearRing();
            return;
        }

        if (!TimeMath.TryParseHhmm(timeStr, out int hh, out int mm))
        {
            _sleepResultLabel.Text = "Zeit konnte nicht gelesen werden. Bitte HH:MM verwenden, z. B. 06:30.";
            ClearRing();
            return;
        }

        var now = DateTime.Now;
        var wake = TimeMath.NextOccurrence(now, hh, mm);
        var diff = wake - now;
        int totalMinutes = (int)(diff.TotalSeconds / 60);
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;

        string bedMain = wake.AddHours(-desiredHours).ToString("HH:mm", CultureInfo.InvariantCulture);
        string? bedMinus = null;
        if (desiredHours >= 1)
            bedMinus = wake.AddHours(-(desiredHours - 1)).ToString("HH:mm", CultureInfo.InvariantCulture);
        string bedPlus = wake.AddHours(-(desiredHours + 1)).ToString("HH:mm", CultureInfo.InvariantCulture);

        string dhStr = TimeMath.FormatHours(desiredHours);
        var lines = new List<string>
        {
            $"Wenn du JETZT schlafen gehst, bekommst du ca. {hours}h {minutes}min Schlaf bis {wake:HH\\:mm}.",
            $"Fuer ~{dhStr}h Schlaf: spaetestens um {bedMain} ins Bett.",
        };
        if (bedMinus is not null)
            lines.Add($"Alternativ: {bedMinus} (~{TimeMath.FormatHours(desiredHours - 1)}h) oder {bedPlus} (~{TimeMath.FormatHours(desiredHours + 1)}h).");
        else
            lines.Add($"Alternativ: {bedPlus} (~{TimeMath.FormatHours(desiredHours + 1)}h).");

        _sleepResultLabel.Text = string.Join("\n", lines);

        _ringNow = now;
        _ringWake = wake;
        _hasRing = true;
        _ringPanel.Invalidate();
    }

    private void ClearRing()
    {
        _hasRing = false;
        _ringPanel.Invalidate();
    }

    // ======================================================================
    //  Schlafzyklen-Ring (GDI+)
    // ======================================================================
    private void DrawSleepCycleRing(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(_ringPanel.BackColor);
        if (!_hasRing) return;

        int w = _ringPanel.ClientSize.Width;
        int h = _ringPanel.ClientSize.Height;
        float cx = w / 2f;
        float cy = h / 2f;
        float rOuter = Math.Min(w, h) / 2f - 10f;
        float rInner = rOuter - 15f;

        using (var outline = new Pen(ColorTranslator.FromHtml("#555555"), 2f))
        {
            g.DrawEllipse(outline, cx - rOuter, cy - rOuter, rOuter * 2, rOuter * 2);
        }

        DrawMarker(g, cx, cy, rInner, rOuter, TimeToAngle(_ringNow), ColorTranslator.FromHtml("#ff5555"));
        DrawMarker(g, cx, cy, rInner, rOuter, TimeToAngle(_ringWake), ColorTranslator.FromHtml("#55ff55"));

        int totalMinutes = (int)((_ringWake - _ringNow).TotalSeconds / 60);
        int numCycles = Math.Max(1, totalMinutes / 90);
        for (int i = 0; i <= numCycles; i++)
        {
            var t = _ringNow.AddMinutes(90.0 * i);
            if (t > _ringWake) break;
            DrawCycleTick(g, cx, cy, rInner + 5, rOuter - 5, TimeToAngle(t), ColorTranslator.FromHtml("#aaaaaa"));
        }

        using var textBrush = new SolidBrush(ColorTranslator.FromHtml("#dddddd"));
        using var textFont = new Font("Segoe UI", 10f, FontStyle.Bold);
        using var fmt = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };
        g.DrawString("Sleep\nCycles", textFont, textBrush, new RectangleF(0, 0, w, h), fmt);
    }

    private static double TimeToAngle(DateTime dt)
    {
        int minutes = dt.Hour * 60 + dt.Minute;
        double frac = minutes / 1440.0;
        return frac * 360.0 - 90.0;
    }

    private static void DrawMarker(Graphics g, float cx, float cy, float rInner, float rOuter, double angleDeg, Color color)
    {
        double rad = angleDeg * Math.PI / 180.0;
        float x1 = cx + rInner * (float)Math.Cos(rad);
        float y1 = cy + rInner * (float)Math.Sin(rad);
        float x2 = cx + rOuter * (float)Math.Cos(rad);
        float y2 = cy + rOuter * (float)Math.Sin(rad);
        using var pen = new Pen(color, 3f);
        g.DrawLine(pen, x1, y1, x2, y2);
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, x2 - 3, y2 - 3, 6, 6);
    }

    private static void DrawCycleTick(Graphics g, float cx, float cy, float rInner, float rOuter, double angleDeg, Color color)
    {
        double rad = angleDeg * Math.PI / 180.0;
        float x1 = cx + rInner * (float)Math.Cos(rad);
        float y1 = cy + rInner * (float)Math.Sin(rad);
        float x2 = cx + rOuter * (float)Math.Cos(rad);
        float y2 = cy + rOuter * (float)Math.Sin(rad);
        using var pen = new Pen(color, 1f);
        g.DrawLine(pen, x1, y1, x2, y2);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        if (_preventSleepEnabled) Native.AllowSleep();
    }
}
