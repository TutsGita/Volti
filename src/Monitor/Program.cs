// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Volti {
  static class Program {
    [STAThread]
    static void Main() {
      bool owner;
      using (new Mutex(true, AppInfo.MonitorMutex, out owner)) {
        if (!owner) {
          // A second manual launch opens the panel of the running monitor.
          if (!AppInfo.HasSwitch(AppInfo.BackgroundSwitch))
            try { using (Process.Start(Path.Combine(Application.StartupPath, AppInfo.PanelExe))) {} }
            catch (System.ComponentModel.Win32Exception) {}
          return;
        }
        Win32.SetAppId(AppInfo.AppUserModelId);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MonitorApp());
      }
    }
  }
}
