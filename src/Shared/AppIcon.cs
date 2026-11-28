// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Volti {
  static class AppIcon {
    // assets/Volti.ico, then the icon embedded in the executable, then the system default.
    internal static Icon Load() {
      try { return new Icon(Path.Combine(Application.StartupPath, "assets", "Volti.ico")); }
      catch (IOException) {} catch (ArgumentException) {} catch (System.ComponentModel.Win32Exception) {}
      try {
        var embedded = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        if (embedded != null) return embedded;
      } catch (ArgumentException) {} catch (IOException) {} catch (System.ComponentModel.Win32Exception) {} catch (NotImplementedException) {}
      return (Icon)SystemIcons.Application.Clone();
    }
  }
}
