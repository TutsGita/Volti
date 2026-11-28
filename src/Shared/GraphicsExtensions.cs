// SPDX-License-Identifier: AGPL-3.0-only
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Volti {
  static class GraphicsExtensions {
    internal static GraphicsPath RoundedPath(float x, float y, float w, float h, float r) {
      var path = new GraphicsPath();
      float d = System.Math.Max(0.1f, System.Math.Min(r, System.Math.Min(w, h)));
      path.AddArc(x, y, d, d, 180, 90);
      path.AddArc(x + w - d, y, d, d, 270, 90);
      path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
      path.AddArc(x, y + h - d, d, d, 90, 90);
      path.CloseFigure();
      return path;
    }
    internal static void DrawRoundedRectangle(this Graphics g, Pen p, float x, float y, float w, float h, float r) {
      using (var path = RoundedPath(x, y, w, h, r)) g.DrawPath(p, path);
    }
    internal static void FillRoundedRectangle(this Graphics g, Brush b, float x, float y, float w, float h, float r) {
      using (var path = RoundedPath(x, y, w, h, r)) g.FillPath(b, path);
    }
    internal static void FillRoundedRectangle(this Graphics g, Color c, Rectangle bounds, float r) {
      using (var brush = new SolidBrush(c)) g.FillRoundedRectangle(brush, bounds.X, bounds.Y, bounds.Width, bounds.Height, r);
    }
    internal static void DrawRoundedRectangle(this Graphics g, Color c, Rectangle bounds, float r, float width = 1f) {
      using (var pen = new Pen(c, width)) g.DrawRoundedRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1, r);
    }
  }
}
