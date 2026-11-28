// SPDX-License-Identifier: AGPL-3.0-only
using System.Reflection;

[assembly: AssemblyTitle("Volti")]
[assembly: AssemblyProduct("Volti")]
[assembly: AssemblyVersion("0.6.0.0")]
[assembly: AssemblyInformationalVersion("0.6.0")]

namespace Volti {
  // Values shared by the tray monitor (Volti.exe) and the panel (Volti.UI.exe).
  static class AppInfo {
    internal const string Name = "Volti";
    internal const string MonitorExe = "Volti.exe";
    internal const string PanelExe = "Volti.UI.exe";
    internal const string MonitorProcessName = "Volti";
    internal const string AppUserModelId = "TutsGita.Volti";
    internal const string Repository = "TutsGita/Volti";
    internal const string RepositoryUrl = "https://github.com/TutsGita/Volti";
    internal const string MonitorMutex = "Local\\Volti.App";
    internal const string PanelMutex = "Local\\Volti.Panel";
    internal const string BackgroundSwitch = "--background";

    internal static string Version {
      get {
        var version = typeof(AppInfo).Assembly.GetName().Version;
        return version.ToString(3);
      }
    }

    internal static bool HasSwitch(string name) {
      return System.Array.IndexOf(System.Environment.GetCommandLineArgs(), name) >= 0;
    }
  }
}
