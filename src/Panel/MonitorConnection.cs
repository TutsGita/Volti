// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Volti {
  // The panel's view of the tray monitor: snapshots, signals and the monitor process.
  class MonitorConnection : IDisposable {
    readonly PanelLink link;
    Process monitor;
    bool readPending = true;
    DateTime nextCheck;
    protected PanelSnapshot current = PanelSnapshot.Offline();

    internal MonitorConnection() { link = new PanelLink(); }
    protected MonitorConnection(bool noLink) { }

    internal PanelSnapshot Snapshot { get { return current; } }
    internal virtual bool Online { get { return monitor != null && !monitor.HasExited; } }
    internal virtual bool IsDemo { get { return false; } }

    // Called every 250 ms. Returns true when the snapshot changed.
    internal virtual bool Poll() {
      if (monitor != null && monitor.HasExited) { monitor.Dispose(); monitor = null; readPending = true; }
      bool updated = link.Updated.WaitOne(0);
      if (!updated && (!readPending || DateTime.UtcNow < nextCheck)) return false;
      nextCheck = DateTime.UtcNow.AddSeconds(1);
      var next = link.Read();
      if (next == null) { readPending = true; return false; }
      int id;
      if (int.TryParse(next.ProcessId, out id) && (monitor == null || monitor.Id != id)) Attach(id);
      readPending = monitor == null;
      if (readPending) next = PanelSnapshot.Offline();
      if (Same(current, next)) return false;
      current = next;
      return true;
    }

    void Attach(int id) {
      if (monitor != null) { monitor.Dispose(); monitor = null; }
      try {
        monitor = Process.GetProcessById(id);
        if (monitor.HasExited || !string.Equals(monitor.ProcessName, AppInfo.MonitorProcessName, StringComparison.OrdinalIgnoreCase)) {
          monitor.Dispose();
          monitor = null;
        }
      } catch (ArgumentException) { monitor = null; }
        catch (InvalidOperationException) { if (monitor != null) monitor.Dispose(); monitor = null; }
    }

    static bool Same(PanelSnapshot a, PanelSnapshot b) {
      return a.Mouse == b.Mouse && a.Headset == b.Headset && a.Status == b.Status && a.Devices == b.Devices &&
             a.MouseTime == b.MouseTime && a.HeadsetTime == b.HeadsetTime;
    }

    internal virtual bool ActivateRequested() { return link.Activate.WaitOne(0); }

    // Asks the monitor for a full scan. ScanFinished reports when it is done.
    internal virtual void Detect() { link.ScanDone.Reset(); link.Detect.Set(); }
    internal virtual bool ScanFinished() { return link.ScanDone.WaitOne(0) || !Online; }

    internal virtual void StartMonitor() {
      try {
        using (Process.Start(new ProcessStartInfo(Path.Combine(Application.StartupPath, AppInfo.MonitorExe), AppInfo.BackgroundSwitch) { UseShellExecute = false })) {}
      } catch (Win32Exception ex) { MessageBox.Show(ex.Message, AppInfo.Name); }
      readPending = true;
    }

    internal virtual void PreferencesChanged() { link.Preferences.Set(); }
    internal virtual void PreviewNotice() { link.Preview.Set(); }
    internal virtual void ExitForUpdate() { link.UpdateExit.Set(); }

    public virtual void Dispose() {
      if (monitor != null) monitor.Dispose();
      if (link != null) link.Dispose();
    }
  }

  // Sample devices for screenshots and for trying the panel without hardware: Volti.UI.exe --demo
  sealed class DemoConnection : MonitorConnection {
    bool online = true, changed = true;
    DateTime scanUntil;

    internal DemoConnection() : base(true) {
      online = !AppInfo.HasSwitch("--demo-offline");
      Build();
    }
    internal override bool Online { get { return online; } }
    internal override bool IsDemo { get { return true; } }

    void Build() {
      if (!online) { current = PanelSnapshot.Offline(); return; }
      string now = Battery.MinuteStamp(DateTime.UtcNow);
      current = new PanelSnapshot {
        Mouse = "64%", Headset = "18%  " + Battery.Charging, Status = "Ready",
        MouseTime = now + "|312", HeadsetTime = now,
        Devices = string.Join("\n", new[] {
          new DeviceReading("BTHENUM\\DEMO-BUDS", "Galaxy Buds2 Pro", "82", now + "|-1").Encode(),
          new DeviceReading("synapse4:DEMO", "Razer Basilisk V3 Pro", "41", now + "|1260").Encode(),
          new DeviceReading("xinput:1", "Controller 1", "Medium", now).Encode(),
          new DeviceReading("receiver:HYPERX-STINGER-0951-16DA", "HyperX Cloud Stinger Wireless (PS4)", "-1", "").Encode()
        })
      };
    }

    internal override bool Poll() { bool result = changed; changed = false; return result; }
    internal override bool ActivateRequested() { return false; }
    internal override void Detect() { scanUntil = DateTime.UtcNow.AddSeconds(2); }
    internal override bool ScanFinished() { return DateTime.UtcNow >= scanUntil; }
    internal override void StartMonitor() { online = true; Build(); changed = true; }
    internal void StopMonitor() { online = false; Build(); changed = true; }
    internal override void PreferencesChanged() {}
    internal override void PreviewNotice() {}
    internal override void ExitForUpdate() {}
    public override void Dispose() {}
  }
}
