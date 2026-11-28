// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Windows.Forms;

namespace Volti {
  // Volti.UI.exe: shows the panel and mirrors the monitor's snapshots. Closing it ends the process;
  // monitoring continues in Volti.exe.
  sealed class PanelApp : ApplicationContext {
    readonly MonitorConnection connection;
    readonly Timer bridge = new Timer { Interval = 250 };
    readonly System.Drawing.Icon appIcon = AppIcon.Load();
    readonly PanelWindow window;
    bool stopping, closed;

    internal PanelApp(MonitorConnection connection) {
      this.connection = connection;
      L.Load();
      Theme.Load();
      window = new PanelWindow(connection, appIcon, () => CheckForUpdate(true));
      window.PreferencesChanged += (s, e) => connection.PreferencesChanged();
      window.FormClosed += (s, e) => { closed = true; ExitThread(); };
      connection.Poll();
      window.ShowSnapshot(connection.Snapshot, connection.Online);
      bridge.Tick += (s, e) => Sync();
      bridge.Start();
      window.Show();
      window.Activate();
      if (connection.IsDemo) return;
      Settings.MarkPanelOpened();
      CheckForUpdate(false);
    }

    void CheckForUpdate(bool manual) {
      Updater.Check(window, manual, () => stopping, () => { connection.ExitForUpdate(); ExitThread(); });
    }

    void Sync() {
      if (connection.ActivateRequested()) {
        if (window.WindowState == FormWindowState.Minimized) window.WindowState = FormWindowState.Normal;
        window.Show();
        window.Activate();
      }
      if (connection.Poll()) window.ShowSnapshot(connection.Snapshot, connection.Online);
    }

    protected override void ExitThreadCore() {
      stopping = true;
      bridge.Dispose();
      if (!closed && !window.IsDisposed) window.Dispose(); // A closed window is disposed by WinForms.
      connection.Dispose();
      appIcon.Dispose();
      base.ExitThreadCore();
    }
  }
}
