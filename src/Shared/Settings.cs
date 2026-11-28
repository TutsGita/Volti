// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Volti {
  // Every preference lives under HKCU\Software\Volti. Value names are kept from earlier releases
  // so that existing profiles carry over unchanged.
  static class Settings {
    internal const string Key = "Software\\Volti";
    const string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    const string UninstallKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{C09F5E69-BD13-420C-AAB7-79630B2658CF}_is1";

    internal static object Read(string name, object fallback) {
      try {
        using (var key = Registry.CurrentUser.OpenSubKey(Key))
          return key == null ? fallback : key.GetValue(name, fallback);
      } catch (SecurityException) { return fallback; }
        catch (IOException) { return fallback; }
        catch (UnauthorizedAccessException) { return fallback; }
    }

    internal static string ReadString(string name, string fallback) {
      return Read(name, fallback) as string ?? fallback;
    }

    internal static int ReadInt(string name, int fallback) {
      try { return Convert.ToInt32(Read(name, fallback)); }
      catch (FormatException) { return fallback; }
      catch (InvalidCastException) { return fallback; }
      catch (OverflowException) { return fallback; }
    }

    internal static void Write(string name, object value) {
      using (var key = Registry.CurrentUser.CreateSubKey(Key)) key.SetValue(name, value);
    }

    internal static void Delete(string name) {
      using (var key = Registry.CurrentUser.CreateSubKey(Key)) key.DeleteValue(name, false);
    }

    // Appearance and language.
    internal static string Theme { get { return ReadString("Theme", null); } set { Write("Theme", value); } }
    internal static string Language { get { return ReadString("Language", "en"); } set { Write("Language", value); } }
    internal static bool AlertsEnabled { get { return ReadInt("BatteryAlerts", 1) != 0; } set { Write("BatteryAlerts", value ? 1 : 0); } }
    internal static bool PanelOpened { get { return ReadInt("PanelOpened", 0) == 1; } }
    internal static void MarkPanelOpened() { if (!PanelOpened) Write("PanelOpened", 1); }
    internal static string DeviceSort { get { return ReadString("DeviceSort", "default"); } set { Write("DeviceSort", value); } }

    // Per-device preferences are keyed by a hash so that identifiers with registry-hostile characters work.
    internal static string DeviceKey(string id) {
      using (var sha = SHA256.Create())
        return "Tray" + BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(id))).Replace("-", "");
    }

    internal static bool IsTrayVisible(string id) { return ReadInt(DeviceKey(id), 1) != 0; }
    internal static void SetTrayVisible(string id, bool visible) { Write(DeviceKey(id), visible ? 1 : 0); }

    internal static string Alias(string id) { return ReadString("Alias" + DeviceKey(id), ""); }
    internal static void SetAlias(string id, string alias) {
      alias = (alias ?? "").Trim();
      if (alias.Length == 0) Delete("Alias" + DeviceKey(id)); else Write("Alias" + DeviceKey(id), alias);
    }

    // Battery thresholds for the two receiver slots (0 = mouse, 1 = headset).
    internal static void LoadLimits(int slot, out int low, out int critical) {
      low = Math.Max(2, Clamp(ReadInt(slot + "Low", 20)));
      critical = Math.Min(low - 1, Clamp(ReadInt(slot + "Critical", 5)));
    }
    internal static void SaveLimit(int slot, string level, int value) { Write(slot + level, value); }
    static int Clamp(int value) { return Math.Max(1, Math.Min(100, value)); }

    // Start with Windows, through the per-user Run key.
    internal static bool StartupEnabled {
      get {
        try {
          using (var key = Registry.CurrentUser.OpenSubKey(RunKey)) return key != null && key.GetValue("Volti") != null;
        } catch (SecurityException) { return false; }
      }
    }

    internal static void SetStartup(bool enable) {
      using (var key = Registry.CurrentUser.CreateSubKey(RunKey)) {
        if (enable) key.SetValue("Volti", "\"" + Path.Combine(Application.StartupPath, AppInfo.MonitorExe) + "\" " + AppInfo.BackgroundSwitch);
        else key.DeleteValue("Volti", false);
      }
    }

    // True when this copy is the one registered by the installer, rather than a portable copy.
    internal static bool InstalledHere() {
      try {
        using (var root = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32))
        using (var key = root.OpenSubKey(UninstallKey)) {
          string location = key == null ? null : key.GetValue("InstallLocation") as string;
          char separator = Path.DirectorySeparatorChar;
          return location != null && string.Equals(location.TrimEnd(separator), Application.StartupPath.TrimEnd(separator), StringComparison.OrdinalIgnoreCase);
        }
      } catch (SecurityException) { return false; }
        catch (IOException) { return false; }
    }

    // Windows accent colour, used by the Black and White themes.
    internal static bool TryWindowsAccent(out System.Drawing.Color accent) {
      accent = System.Drawing.Color.Empty;
      try {
        using (var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\DWM")) {
          if (key == null) return false;
          int n = unchecked((int)Convert.ToUInt32(key.GetValue("AccentColor", 0xffd47800)));
          accent = System.Drawing.Color.FromArgb(n & 255, (n >> 8) & 255, (n >> 16) & 255);
          return true;
        }
      } catch (Exception) { return false; }
    }
  }
}
