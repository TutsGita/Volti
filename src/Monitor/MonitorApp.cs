// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Volti {
  // Volti.exe: runs the readers, keeps the tray icons current, raises alerts and publishes
  // snapshots for the panel. Every member except the IReadingSink callbacks runs on the UI thread.
  sealed class MonitorApp : ApplicationContext, IReadingSink {
    readonly Control dispatcher = new Control();
    readonly PanelLink link = new PanelLink();
    readonly System.Windows.Forms.Timer bridge = new System.Windows.Forms.Timer { Interval = 250 };
    readonly BatteryHistory history = new BatteryHistory(Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Volti", "battery-history.json"));
    readonly AlertTracker alerts = new AlertTracker();
    readonly Icon appIcon = AppIcon.Load();
    readonly TrayIcons tray;
    readonly ReceiverReaders readers;
    readonly HyperXScanner hyperX;
    readonly DeviceChangeWindow deviceChanges;
    volatile bool stopping;

    // Receiver slots: 0 = DELUX mouse, 1 = JBL headset.
    readonly string[] slots = { Battery.NoValue, Battery.NoValue };
    readonly string[] slotTimes = { "", "" };
    readonly string[] queued = new string[2], queuedTimes = new string[2];
    readonly object queueLock = new object();
    readonly SortedDictionary<string, string> receiverRows = new SortedDictionary<string, string>();
    string bluetoothRows = "", synapseRows = "", status = "Ready";
    PanelSnapshot snapshot = PanelSnapshot.Offline();
    BatteryNotice notice;
    bool alertsEnabled, scanningBluetooth, scanningSynapse, reportScan, publishPending;
    DateTime nextReceiverScan, nextBluetoothScan, nextSynapseScan, hardwareChangedAt = DateTime.MaxValue;

    internal MonitorApp() {
      L.Load();
      Theme.Load();
      alerts.Load();
      alertsEnabled = Settings.AlertsEnabled;
      var unused = dispatcher.Handle; // Create the handle so background threads can post.
      tray = new TrayIcons(appIcon, LaunchPanel, ExitThread);
      readers = new ReceiverReaders(this);
      hyperX = new HyperXScanner(Post, SetReceiver);
      bridge.Tick += (s, e) => Sync();
      bridge.Start();
      Publish();
      DetectBluetooth();
      DetectSynapse();
      deviceChanges = new DeviceChangeWindow(() => hardwareChangedAt = DateTime.UtcNow.AddMilliseconds(650));
      DiscoverReceivers();
      if (AppInfo.HasSwitch(AppInfo.BackgroundSwitch)) return;
      // The first manual launch opens the panel; later launches only check for updates.
      if (!Settings.PanelOpened) LaunchPanel();
      else Updater.Check(null, false, () => stopping, ExitThread);
    }

    void Post(Action update) {
      if (stopping) return;
      try { dispatcher.BeginInvoke((Action)(() => { if (!stopping) update(); })); }
      catch (InvalidOperationException) {}
    }

    // IReadingSink, called from reader threads.
    public void SetSlot(int slot, string value) {
      int percent;
      string stamp = Battery.TryPercent(value, out percent) ? Battery.MinuteStamp(DateTime.UtcNow) : "";
      // Posting inside the lock keeps the UI thread's order equal to the queue's order.
      lock (queueLock) {
        if (queued[slot] == value && queuedTimes[slot] == stamp) return;
        queued[slot] = value;
        queuedTimes[slot] = stamp;
        Post(() => {
          bool changed = slots[slot] != value;
          slots[slot] = value;
          slotTimes[slot] = stamp;
          Publish();
          if (changed) CheckAlert(slot);
        });
      }
    }

    public void SetReceiver(string path, string name, int percent) {
      string id = "receiver:" + path.ToUpperInvariant();
      Post(() => {
        if (name == null) {
          if (!receiverRows.Remove(id)) return;
        } else {
          string row = new DeviceReading(id, name, percent.ToString(), percent < 0 ? "" : Battery.MinuteStamp(DateTime.UtcNow)).Encode();
          string previous;
          if (receiverRows.TryGetValue(id, out previous) && previous == row) return;
          receiverRows[id] = row;
        }
        Publish();
      });
    }

    public void ReceiverPaths(string[] paths) { Post(() => hyperX.Scan(paths)); }
    public void Note(string value) { Post(() => { status = value; Publish(); }); }

    string DiscoveredRows() {
      var feeds = new List<string>();
      if (bluetoothRows.Length > 0) feeds.Add(bluetoothRows);
      feeds.AddRange(receiverRows.Values);
      if (synapseRows.Length > 0) feeds.Add(synapseRows);
      return string.Join("\n", feeds);
    }

    // Adds remaining-time estimates, shares the snapshot with the panel and refreshes the tray.
    void Publish() {
      var rows = new List<string>();
      foreach (var row in DeviceReading.Decode(DiscoveredRows()))
        rows.Add(new DeviceReading(row.Id, row.Name, row.Level, history.Observe(row.Id, row.Level, row.Observed)).Encode());
      snapshot = new PanelSnapshot {
        Mouse = slots[0], Headset = slots[1], Status = status, Devices = string.Join("\n", rows),
        MouseTime = history.Observe(DeviceCatalog.MouseId, slots[0], slotTimes[0]),
        HeadsetTime = history.Observe(DeviceCatalog.HeadsetId, slots[1], slotTimes[1])
      };
      history.FinishObservation();
      publishPending = !link.Publish(snapshot.Mouse, snapshot.Headset, snapshot.Status, snapshot.Devices, snapshot.MouseTime, snapshot.HeadsetTime);
      tray.Update(DeviceCatalog.From(snapshot));
    }

    void Sync() {
      if (link.UpdateExit.WaitOne(0)) { ExitThread(); return; }
      if (publishPending) Publish();
      history.Save(false);
      DateTime now = DateTime.UtcNow;
      bool manual = link.Detect.WaitOne(0);
      if (manual) reportScan = true;
      bool everything = manual || now >= hardwareChangedAt;
      if (everything) hardwareChangedAt = DateTime.MaxValue;
      if (everything || now >= nextReceiverScan) DiscoverReceivers();
      if (everything || now >= nextBluetoothScan) DetectBluetooth();
      if (everything || now >= nextSynapseScan) DetectSynapse();
      if (reportScan && !readers.Discovering && !scanningBluetooth && !scanningSynapse && !hyperX.Busy) {
        reportScan = false;
        link.ScanDone.Set();
      }
      if (link.Preferences.WaitOne(0)) ReloadPreferences();
      if (link.Preview.WaitOne(0)) ShowNotice(false, 20, L.T("This is a low battery notification preview."));
    }

    void ReloadPreferences() {
      L.Load();
      Theme.Load();
      alerts.Load();
      alertsEnabled = Settings.AlertsEnabled;
      tray.ApplyLanguage();
      tray.ApplyTheme();
      tray.Update(DeviceCatalog.From(snapshot));
      if (!alertsEnabled && notice != null) { notice.Dispose(); notice = null; }
      CheckAlert(0);
      CheckAlert(1);
    }

    void DiscoverReceivers() {
      // Retry shortly when a pass is still running, for example after several hardware changes.
      if (!readers.Discover()) { hardwareChangedAt = DateTime.UtcNow.AddSeconds(1); return; }
      nextReceiverScan = DateTime.UtcNow.AddMinutes(1);
    }

    void DetectBluetooth() {
      if (scanningBluetooth) return;
      scanningBluetooth = true;
      nextBluetoothScan = DateTime.UtcNow.AddMinutes(1);
      new Thread(() => {
        string observed = Battery.MinuteStamp(DateTime.UtcNow);
        string controllers = ControllerBattery.Scan(observed);
        string rows;
        try { rows = BluetoothReader.Scan(observed); }
        catch (Exception) { rows = ""; Note("Bluetooth detection failed"); }
        if (controllers.Length > 0) rows = rows.Length == 0 ? controllers : rows + "\n" + controllers;
        Post(() => {
          scanningBluetooth = false;
          if (bluetoothRows == rows) return;
          bluetoothRows = rows;
          Publish();
        });
      }) { IsBackground = true, Name = "Bluetooth battery discovery" }.Start();
    }

    void DetectSynapse() {
      if (scanningSynapse) return;
      scanningSynapse = true;
      nextSynapseScan = DateTime.UtcNow.AddSeconds(30);
      ThreadPool.QueueUserWorkItem(_ => {
        string rows = SynapseBattery.Scan();
        Post(() => {
          scanningSynapse = false;
          if (synapseRows == rows) return;
          synapseRows = rows;
          Publish();
        });
      });
    }

    void CheckAlert(int slot) {
      AlertLevel level = alerts.Check(slot, slots[slot]);
      if (!alertsEnabled || level == AlertLevel.None) return;
      int percent;
      Battery.TryPercent(slots[slot], out percent);
      var device = DeviceCatalog.Slot(slot, slots[slot], slot == 0 ? snapshot.MouseTime : snapshot.HeadsetTime);
      string advice = level == AlertLevel.Critical ? "" : Battery.Remaining(device.RemainingMinutes);
      ShowNotice(level == AlertLevel.Critical, percent, device.DisplayName + "\n" + (advice.Length == 0 ? L.T("Connect the charger") : advice));
    }

    void ShowNotice(bool critical, int percent, string message) {
      if (notice != null) notice.Dispose();
      notice = new BatteryNotice(Screen.FromPoint(Cursor.Position), percent + L.T("% remaining"), message,
        critical ? Theme.Current.Low : Theme.Current.Medium, percent);
      notice.Show();
    }

    void LaunchPanel() {
      try {
        using (Process.Start(new ProcessStartInfo(Path.Combine(Application.StartupPath, AppInfo.PanelExe)) { UseShellExecute = false })) {}
      } catch (Exception ex) {
        tray.Visible().ShowBalloonTip(5000, AppInfo.Name, ex.Message, ToolTipIcon.Warning);
      }
    }

    protected override void ExitThreadCore() {
      stopping = true;
      bridge.Dispose();
      history.Save(true);
      if (notice != null) notice.Dispose();
      deviceChanges.DestroyHandle();
      tray.Dispose();
      link.Dispose();
      readers.Dispose();
      appIcon.Dispose();
      dispatcher.Dispose();
      base.ExitThreadCore();
    }
  }
}
