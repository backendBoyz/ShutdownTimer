using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace SleepPlanner;

/// <summary>Standby-Steuerung ueber die Win32-API.</summary>
internal static class Native
{
    [Flags]
    private enum ExecutionState : uint
    {
        Continuous = 0x80000000,
        SystemRequired = 0x00000001,
        DisplayRequired = 0x00000002,
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

    /// <summary>Verhindert Standby. keepDisplayOn=false -> Display darf aus gehen.</summary>
    public static bool PreventSleep(bool keepDisplayOn = false)
    {
        var flags = ExecutionState.Continuous | ExecutionState.SystemRequired;
        if (keepDisplayOn) flags |= ExecutionState.DisplayRequired;
        return SetThreadExecutionState(flags) != 0;
    }

    /// <summary>Hebt die Sleep-Blockierung wieder auf.</summary>
    public static bool AllowSleep()
    {
        return SetThreadExecutionState(ExecutionState.Continuous) != 0;
    }
}

/// <summary>Kapselt den nativen Windows-Befehl <c>shutdown</c>.</summary>
internal static class ShutdownService
{
    private static bool Run(string fileName, params string[] args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            foreach (var a in args) psi.ArgumentList.Add(a);
            using var p = Process.Start(psi);
            if (p is null) return false;
            p.WaitForExit();
            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public static bool Schedule(int seconds) => Run("shutdown", "/s", "/t", seconds.ToString(CultureInfo.InvariantCulture));

    public static bool Abort() => Run("shutdown", "/a");
}

/// <summary>
/// Autostart ueber den Windows Task Scheduler. Die Aufgabe laeuft beim Anmelden
/// mit hoechsten Rechten (elevated), damit keine UAC-Abfrage noetig ist.
/// </summary>
internal static class AutostartManager
{
    public const string TaskName = "SleepPlannerAutostart";

    private static bool RunSchtasks(params string[] args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            foreach (var a in args) psi.ArgumentList.Add(a);
            using var p = Process.Start(psi);
            if (p is null) return false;
            p.WaitForExit();
            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsEnabled() => RunSchtasks("/query", "/tn", TaskName);

    public static bool Enable()
    {
        // Pfad in Anfuehrungszeichen, falls er Leerzeichen enthaelt.
        string trCommand = "\"" + Application.ExecutablePath + "\"";
        return RunSchtasks(
            "/create",
            "/tn", TaskName,
            "/tr", trCommand,
            "/sc", "onlogon",
            "/rl", "highest",
            "/f");
    }

    public static bool Disable() => RunSchtasks("/delete", "/tn", TaskName, "/f");
}

/// <summary>Reine, GUI-freie Hilfsfunktionen (gut testbar).</summary>
internal static class TimeMath
{
    /// <summary>Parst "HH:MM" tolerant (auch "6:30"). Gibt false bei Unsinn.</summary>
    public static bool TryParseHhmm(string s, out int hour, out int minute)
    {
        hour = 0;
        minute = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;
        var parts = s.Split(':');
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out hour)) return false;
        if (!int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out minute)) return false;
        return hour is >= 0 and <= 23 && minute is >= 0 and <= 59;
    }

    /// <summary>Naechstes Auftreten der Uhrzeit ab <paramref name="now"/>.</summary>
    public static DateTime NextOccurrence(DateTime now, int hour, int minute)
    {
        var target = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
        if (target <= now) target = target.AddDays(1);
        return target;
    }

    /// <summary>Stundenzahl huebsch formatieren: 8 -> "8", 7.5 -> "7.5".</summary>
    public static string FormatHours(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
}
