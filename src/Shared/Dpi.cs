// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Drawing;

namespace Volti {
  // Layout sizes are written for 96 DPI and scaled once for the primary display.
  static class Dpi {
    static float factor = -1;
    internal static float Factor {
      get {
        if (factor < 0) {
          try { using (var g = Graphics.FromHwnd(IntPtr.Zero)) factor = Math.Max(1f, g.DpiX / 96f); }
          catch (Exception) { factor = 1f; }
        }
        return factor;
      }
    }
    internal static int S(int value) { return (int)Math.Round(value * Factor); }
    internal static float S(float value) { return value * Factor; }
    internal static Size S(int width, int height) { return new Size(S(width), S(height)); }
    internal static Rectangle S(int x, int y, int width, int height) { return new Rectangle(S(x), S(y), S(width), S(height)); }
  }
}
