// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Volti {
  enum Glyph { None, Gear, Refresh, Pencil, Close, Minimize, Overview, Palette, Learning, Radio, Bluetooth, Cable, Bolt, Chevron, Sort, Info, Bell }

  // Outline icons on a 24-unit grid.
  static class Glyphs {
    internal static void Draw(Graphics g, Glyph glyph, Rectangle bounds, Color ink, int angle = 0, float stroke = 1.8f) {
      if (glyph == Glyph.None) return;
      var state = g.Save();
      try {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TranslateTransform(bounds.X, bounds.Y);
        g.ScaleTransform(bounds.Width / 24f, bounds.Height / 24f);
        if (angle != 0) { g.TranslateTransform(12, 12); g.RotateTransform(angle); g.TranslateTransform(-12, -12); }
        using (var pen = new Pen(ink, stroke)) {
          pen.StartCap = pen.EndCap = LineCap.Round;
          pen.LineJoin = LineJoin.Round;
          DrawShape(g, pen, glyph, ink);
        }
      } finally { g.Restore(state); }
    }

    static void DrawShape(Graphics g, Pen pen, Glyph glyph, Color ink) {
      switch (glyph) {
        case Glyph.Refresh:
          g.DrawArc(pen, 4, 4, 16, 16, 0, 310);
          g.DrawLines(pen, new[] { new Point(20, 4), new Point(20, 9), new Point(15, 9) });
          break;
        case Glyph.Pencil:
          g.DrawPolygon(pen, new[] { new Point(4, 20), new Point(5, 15), new Point(16, 4), new Point(20, 8), new Point(9, 19) });
          g.DrawLine(pen, 14, 6, 18, 10);
          break;
        case Glyph.Minimize: g.DrawLine(pen, 6, 12, 18, 12); break;
        case Glyph.Close: g.DrawLine(pen, 7, 7, 17, 17); g.DrawLine(pen, 17, 7, 7, 17); break;
        case Glyph.Overview:
          for (int i = 0; i < 4; i++) g.DrawRoundedRectangle(pen, 4 + i % 2 * 9, 4 + i / 2 * 9, 6, 6, 2);
          break;
        case Glyph.Palette:
          g.DrawArc(pen, 3, 3, 18, 18, 90, 285);
          g.DrawLine(pen, 12, 21, 12, 16); g.DrawLine(pen, 12, 16, 20, 16);
          g.DrawEllipse(pen, 7, 9, 2, 2); g.DrawEllipse(pen, 11, 6, 2, 2); g.DrawEllipse(pen, 15, 9, 2, 2);
          break;
        case Glyph.Learning:
          g.DrawEllipse(pen, 4, 4, 16, 16);
          g.DrawLines(pen, new[] { new Point(12, 7), new Point(12, 12), new Point(15, 14) });
          break;
        case Glyph.Radio:
          g.DrawArc(pen, 3, 7, 18, 13, 218, 104); g.DrawArc(pen, 7, 11, 10, 8, 215, 110); g.DrawEllipse(pen, 11, 16, 2, 2);
          break;
        case Glyph.Bluetooth:
          g.DrawLines(pen, new[] { new Point(6, 7), new Point(17, 16), new Point(12, 21), new Point(12, 3), new Point(17, 8), new Point(6, 17) });
          break;
        case Glyph.Cable: g.DrawRoundedRectangle(pen, 5, 7, 14, 10, 4); g.DrawLine(pen, 9, 12, 15, 12); break;
        case Glyph.Bolt:
          using (var brush = new SolidBrush(ink))
            g.FillPolygon(brush, new[] { new PointF(13.5f, 2), new PointF(5, 13.5f), new PointF(11, 13.5f), new PointF(10, 22), new PointF(19, 10), new PointF(13, 10) });
          break;
        case Glyph.Chevron: g.DrawLines(pen, new[] { new Point(7, 10), new Point(12, 15), new Point(17, 10) }); break;
        case Glyph.Sort:
          g.DrawLine(pen, 5, 7, 19, 7); g.DrawLine(pen, 5, 12, 15, 12); g.DrawLine(pen, 5, 17, 11, 17);
          break;
        case Glyph.Info:
          g.DrawEllipse(pen, 3, 3, 18, 18); g.DrawLine(pen, 12, 11, 12, 16); g.DrawLine(pen, 12, 7.5f, 12, 7.6f);
          break;
        case Glyph.Bell:
          g.DrawArc(pen, 6, 4, 12, 12, 180, 180);
          g.DrawLines(pen, new[] { new PointF(6, 10), new PointF(6, 16), new PointF(4, 18), new PointF(20, 18), new PointF(18, 16), new PointF(18, 10) });
          g.DrawLine(pen, 10, 21, 14, 21);
          break;
        default: // Gear
          var points = new PointF[32];
          for (int j = 0; j < points.Length; j++) {
            double a = (j + 0.5) * Math.PI / 16;
            float r = j % 4 == 0 || j % 4 == 3 ? 7.4f : 9.5f;
            points[j] = new PointF(12 + r * (float)Math.Cos(a), 12 + r * (float)Math.Sin(a));
          }
          g.DrawPolygon(pen, points);
          g.DrawEllipse(pen, 9, 9, 6, 6);
          break;
      }
    }

    internal static Glyph For(Transport transport) {
      switch (transport) {
        case Transport.Bluetooth: return Glyph.Bluetooth;
        case Transport.Usb: return Glyph.Radio;
        default: return Glyph.Cable;
      }
    }
  }
}
