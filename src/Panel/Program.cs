// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Threading;
using System.Windows.Forms;

namespace Volti {
  static class Program {
    [STAThread]
    static void Main() {
      bool demo = AppInfo.HasSwitch("--demo");
      bool owner;
      using (new Mutex(true, demo ? AppInfo.PanelMutex + ".Demo" : AppInfo.PanelMutex, out owner)) {
        if (!owner) {
          // Bring the open panel forward instead of starting another one.
          if (!AppInfo.HasSwitch(AppInfo.BackgroundSwitch) && !demo)
            using (var link = new PanelLink()) link.Activate.Set();
          return;
        }
        Win32.SetAppId(AppInfo.AppUserModelId);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new PanelApp(demo ? (MonitorConnection)new DemoConnection() : new MonitorConnection()));
      }
    }
  }
}
