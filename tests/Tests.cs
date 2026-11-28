// SPDX-License-Identifier: AGPL-3.0-only
// Self-contained checks for parsers and state logic. Build with scripts/check.sh (Mono) or
// scripts/test.ps1 (Windows). Exit code 0 means every check passed.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Volti.Tests {
  static class Runner {
    static int failures, passed;

    static void Check(bool condition, string name) {
      if (condition) { passed++; return; }
      failures++;
      Console.WriteLine("FAIL " + name);
    }

    static void Equal<T>(T expected, T actual, string name) {
      Check(EqualityComparer<T>.Default.Equals(expected, actual), name + " (expected " + expected + ", got " + actual + ")");
    }

    static int Main(string[] args) {
      string root = args.Length > 0 ? args[0] : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", ".."));
      L.CatalogFolder = Path.Combine(root, "assets", "lang");
      foreach (var test in new Action[] { Receivers, HyperX, Jbl, Synapse, Readings, History, Catalog, Alerts, Updates, Themes }) {
        try { test(); }
        catch (Exception ex) { failures++; Console.WriteLine("FAIL " + test.Method.Name + ": " + ex); }
      }
      Translations(root);
      Console.WriteLine(passed + " passed, " + failures + " failed");
      return failures == 0 ? 0 : 1;
    }

    static void Receivers() {
      string name; int percent;
      Check(ReceiverReports.TrySharedMouse(new byte[] { 0x03, 0x10, 0x40, 0x01, 0x07 }, 5, out name, out percent) && name == "Attack Shark R1" && percent == 70, "R1 reports tens");
      Check(ReceiverReports.TrySharedMouse(new byte[] { 0x03, 0x85, 0x40, 0x01, 0x05 }, 5, out name, out percent) && name == "Attack Shark X6" && percent == 50, "X6 reports tens");
      Check(ReceiverReports.TrySharedMouse(new byte[] { 0x03, 0x20, 0x40, 0x01, 0x42 }, 5, out name, out percent) && name == "DELUX M600 Pro" && percent == 66, "M600 Pro percent");
      Check(!ReceiverReports.TrySharedMouse(new byte[] { 0x03, 0x20, 0x40, 0x02, 0x42 }, 5, out name, out percent), "status byte must be 01");
      Check(!ReceiverReports.TrySharedMouse(new byte[] { 0x03, 0x99, 0x40, 0x01, 0x42 }, 5, out name, out percent), "unknown model byte");
      Check(!ReceiverReports.TrySharedMouse(new byte[] { 0x03, 0x55, 0x40, 0x01, 0x65 }, 5, out name, out percent), "percent above 100");
      Equal(80, ReceiverReports.Rapoo(new byte[] { 0xBB, 0xB0, 0x00, 0x40, 0, 0, 80 }, 7), "Rapoo");
      Equal(-1, ReceiverReports.Rapoo(new byte[] { 0xBB, 0xB0, 0x00, 0x41, 0, 0, 80 }, 7), "Rapoo header");
      Equal(35, ReceiverReports.Quantum910(new byte[] { 0x08, 35 }, 2), "Quantum 910");
      Equal(-1, ReceiverReports.Quantum910(new byte[] { 0x08, 135 }, 2), "Quantum 910 range");
    }

    static void HyperX() {
      Equal(77, HyperXBattery.Stinger2Percent(new byte[] { 0x06, 0xFF, 0xBB, 0x02, 0, 0, 0, 77 }, 8), "Stinger 2");
      Equal(-1, HyperXBattery.Stinger2Percent(new byte[] { 0x06, 0xFF, 0xBB, 0x02, 0, 0, 0, 77 }, 7), "Stinger 2 short");
      var core = new byte[20];
      core[0] = 0xFF; core[1] = 0x12; core[2] = 0x05; core[9] = 3;
      core[11] = 4120 & 0xFF; core[12] = 4120 >> 8;
      Equal(99, HyperXBattery.CorePercent(core, true), "Core near full discharge curve");
      core[9] = 6;
      Equal(100, HyperXBattery.CorePercent(core, true), "Core charged");
      core[9] = 3; core[11] = 3600 & 0xFF; core[12] = 3600 >> 8;
      Equal(6, HyperXBattery.CorePercent(core, true), "Core low voltage");
      core[1] = 0x11;
      Equal(-1, HyperXBattery.CorePercent(core, true), "Core strict header");
      Equal(6, HyperXBattery.CorePercent(core, false), "Core relaxed header");
      Equal("HyperX Pulsefire Haste 2 Wireless", HyperXBattery.DeviceName(@"\\?\HID#VID_03F0&PID_0D97&MI_02&COL01#x"), "Haste 2 name");
      Equal("HyperX Cloud Stinger 2 Wireless", HyperXBattery.DeviceName(@"\\?\HID#VID_03F0&PID_0D93&MI_03&COL03#x"), "Stinger 2 name");
      Check(HyperXBattery.IsOriginalStinger(@"\\?\HID#VID_0951&PID_16DA&MI_00#x"), "original Stinger");
    }

    static void Jbl() {
      var reply = new byte[64];
      byte[] header = { 0x86, 0x00, 0xDD, 0x01, 0x00, 0, 0, 0x07, 0x00, 0x00, 0x00, 0x0D, 0x00, 0x01, 0x00, 0x80 | 42 };
      Array.Copy(header, reply, header.Length);
      int percent; bool charging;
      Check(ReceiverReaders.TryJblBattery(reply, 64, out percent, out charging) && percent == 42 && charging, "JBL charging reply");
      reply[10] = 0x80;
      Check(!ReceiverReaders.TryJblBattery(reply, 64, out percent, out charging), "JBL error status");
      Check(!ReceiverReaders.TryJblBattery(reply, 9, out percent, out charging), "JBL short reply");
    }

    static void Synapse() {
      var now = DateTime.UtcNow;
      string stamp = now.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
      string v4 = "[" + stamp + "] info: connectingDeviceData: [{\"name\":{\"en\":\"Razer Viper V2 Pro\"},\"serialNumber\":\"PM1\",\"powerStatus\":{\"level\":64,\"chargingStatus\":\"Charging\"}}," +
        "{\"name\":\"Old\",\"isConnected\":false,\"powerStatus\":{\"level\":10}}]\n";
      var rows = DeviceReading.Decode(SynapseBattery.ParseV4(v4, now)).ToList();
      Equal(1, rows.Count, "Synapse 4 skips disconnected devices");
      Check(rows.Count == 1 && rows[0].Id == "synapse4:PM1" && rows[0].Name == "Razer Viper V2 Pro" && rows[0].Level == "64  ⚡", "Synapse 4 row");
      string old = "[" + now.AddHours(-2).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "] info: connectingDeviceData: [{\"name\":\"X\",\"powerStatus\":{\"level\":5}}]\n";
      Check(SynapseBattery.ParseV4(old, now) == null, "Synapse 4 ignores expired lines");
      string v3 = stamp + " INFO 1 _OnBatteryLevelChanged Name: Razer Naga Pro Handle: 7 level 55 state 0\n" +
        stamp + " INFO 1 _OnBatteryLevelChanged Name: Razer Barracuda Handle: 9 level 30 state 1\n" +
        stamp + " INFO 1 _OnDeviceRemoved Handle: 9\n";
      rows = DeviceReading.Decode(SynapseBattery.ParseV3(v3, now)).ToList();
      Check(rows.Count == 1 && rows[0].Name == "Razer Naga Pro" && rows[0].Level == "55", "Synapse 3 level and removal");
    }

    static void Readings() {
      var row = new DeviceReading("BTH\\ID\twith tab", "Name\nwith newline", "50", "2026-01-01 10:00|30");
      var decoded = DeviceReading.Decode(row.Encode() + "\nbroken line\n").ToList();
      Check(decoded.Count == 1 && decoded[0].Id == row.Id && decoded[0].Name == row.Name && decoded[0].Observed == row.Observed, "reading round trip");
      int percent;
      Check(Battery.TryPercent("45%  ⚡", out percent) && percent == 45, "percent with charging");
      Check(!Battery.TryPercent("Disconnected", out percent), "status is not a percent");
      Equal("≈ 2 h 5 min remaining", Battery.Remaining(125), "remaining text");
      Equal("", Battery.Remaining(-1), "no estimate");
    }

    static void History() {
      string path = Path.Combine(Path.GetTempPath(), "volti-history-" + Guid.NewGuid().ToString("N") + ".json");
      try {
        var history = new BatteryHistory(path);
        var start = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Local);
        string last = "";
        for (int i = 0; i <= 10; i++) {
          // One percentage point every six minutes.
          last = history.Observe("mouse", (90 - i) + "%", start.AddMinutes(i * 6).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
          history.FinishObservation();
        }
        Check(last.EndsWith("|480"), "learned 80 % at 6 min per point (" + last + ")");
        Check(history.Observe("mouse", "79%  ⚡", start.AddMinutes(70).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)).IndexOf('|') < 0, "no estimate while charging");
        history.Save(true);
        Check(File.Exists(path) && new FileInfo(path).Length <= BatteryHistory.MaxHistoryBytes, "history saved within cap");
      } finally { if (File.Exists(path)) File.Delete(path); }
    }

    static void Catalog() {
      var snapshot = new PanelSnapshot {
        Mouse = "Disconnected", Headset = "Waiting for reading", Status = "Ready", MouseTime = "", HeadsetTime = "",
        Devices = string.Join("\n", new[] {
          new DeviceReading("xinput:2", "Controller 2", "Low", "2026-01-01 10:00").Encode(),
          new DeviceReading("receiver:X", "HyperX", "-1", "").Encode(),
          new DeviceReading("BTHENUM\\1", "Galaxy Buds2", "70  ⚡", "2026-01-01 10:00|-1").Encode()
        })
      };
      var list = DeviceCatalog.From(snapshot);
      Equal(4, list.Count, "disconnected mouse slot is hidden");
      Check(list[0].Slot == 1 && !list[0].HasPercent, "headset waiting");
      Check(list[1].Transport == Transport.XInput && list[1].Kind == DeviceKind.Controller && list[1].Percent == -1, "controller status");
      Check(list[2].Unavailable && list[2].DisplayLevel == Battery.NoValue, "unavailable reading");
      Check(list[3].Percent == 70 && list[3].Charging && list[3].Learning && list[3].Kind == DeviceKind.Earbuds, "bluetooth earbuds");
      Equal("10:00", list[3].ObservedTime, "observed time");
      Equal(DeviceKind.Mouse, DeviceCatalog.Slot(0, "50%", "").Kind, "mouse slot kind");
      Equal("Wireless mouse receiver", DeviceCatalog.Slot(0, "No response", "").Name, "unknown mouse name");
    }

    static void Alerts() {
      var alerts = new AlertTracker();
      alerts.SetLimits(0, 20, 5);
      Equal(AlertLevel.None, alerts.Check(0, "50%"), "above low");
      Equal(AlertLevel.Low, alerts.Check(0, "20%"), "enters low");
      Equal(AlertLevel.None, alerts.Check(0, "19%"), "low fires once");
      Equal(AlertLevel.Critical, alerts.Check(0, "5%"), "enters critical");
      Equal(AlertLevel.None, alerts.Check(0, "Disconnected"), "status keeps state");
      Equal(AlertLevel.None, alerts.Check(0, "4%"), "critical fires once");
      Equal(AlertLevel.None, alerts.Check(0, "30%"), "rearms when charged");
      Equal(AlertLevel.Low, alerts.Check(0, "15%"), "fires again after rearm");
    }

    static void Updates() {
      string json = "{\"tag_name\":\"v9.1.0\",\"body\":\"## Notes\\n- **Bold** [link](https://x)\\n- Second\",\"assets\":[{\"browser_download_url\":\"https://github.com/TutsGita/Volti/releases/download/v9.1.0/Volti-9.1.0-Setup.exe\"}]}";
      var release = Updater.Parse(json, new Version(0, 6, 0, 0));
      Check(release != null && release.SetupName == "Volti-9.1.0-Setup.exe", "newer release found");
      Check(release != null && release.Notes == "• Bold link" + Environment.NewLine + "• Second", "release highlights");
      Check(Updater.Parse(json, new Version(9, 1, 0, 0)) == null, "same version ignored");
      Check(Updater.Parse(json.Replace("Setup.exe", "Other.exe"), new Version(0, 6, 0, 0)) == null, "missing setup asset");
      Check(Updater.Parse(json.Replace("v9.1.0\",", "v9.1.0-beta\","), new Version(0, 6, 0, 0)) == null, "pre-release tag ignored");
      string hash = new string('a', 64);
      Equal(hash, Updater.ExpectedHash("x  other.zip\n" + hash + "  Volti-9.1.0-Setup.exe\n", "Volti-9.1.0-Setup.exe"), "checksum line");
    }

    static void Themes() {
      foreach (var theme in Theme.All) {
        Theme.Apply(theme.Name);
        Equal(theme.Low.ToArgb(), Theme.Battery(0).ToArgb(), theme.Name + " empty colour");
        Equal(theme.High.ToArgb(), Theme.Battery(100).ToArgb(), theme.Name + " full colour");
      }
      Theme.Apply("Unknown");
      Equal(Theme.DefaultName, Theme.Current.Name, "unknown theme falls back");
    }

    // Every literal passed to L.T or L.F must exist in each catalog.
    static void Translations(string root) {
      var keys = new SortedSet<string>(StringComparer.Ordinal);
      foreach (string file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        foreach (Match m in Regex.Matches(File.ReadAllText(file), @"\bL\.[TF]\(""((?:[^""\\]|\\.)*)"""))
          keys.Add(Regex.Unescape(m.Groups[1].Value));
      // Values translated through variables.
      keys.UnionWith(new[] { "Ready", "Monitor offline", "Bluetooth detection failed", "Disconnected", "Waiting for reading", "No response",
        "Access denied", "Empty", "Low", "Medium", "Full", "Overview", "Settings", "Themes", "Default order", "Lowest battery first", "Name",
        "General", "Language", "Devices", "About", "Start with Windows", "Listed in Windows Startup apps.", "Battery notifications",
        "Low and critical alerts for receivers that support them.", "Test battery notification", "Detect devices",
        "Search USB receivers, Bluetooth and Razer Synapse now.", "Tray icons", "Choose which icons stay next to the clock.", "Updates",
        "Stable releases are verified with SHA-256 before installing.", "No battery readings yet",
        "Connect a supported receiver or Bluetooth device, then select Detect devices.", "The monitor is not running, so readings are paused.",
        "Start monitor", "Black", "White" });
      foreach (string code in new[] { "pt-BR", "zh-CN" }) {
        var catalog = new System.Web.Script.Serialization.JavaScriptSerializer()
          .Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(root, "assets", "lang", code + ".json")));
        foreach (string key in keys) Check(catalog.ContainsKey(key), code + " translates \"" + key + "\"");
      }
    }
  }
}
