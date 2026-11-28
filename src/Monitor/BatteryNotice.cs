// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Volti {
  // Pill-shaped low battery notice in the corner of the working area. Closes itself after 6.5 s.
  sealed class BatteryNotice : Form {
    readonly Timer timer = new Timer { Interval = 6500 };
    readonly Color tint;
    readonly int percent;
    readonly Font titleFont = new Font("Segoe UI", 11f, FontStyle.Bold);
    readonly Font bodyFont = new Font("Segoe UI", 9f);
    readonly string title, message;
    Rectangle closeBounds;
    bool closeHover;

    protected override bool ShowWithoutActivation { get { return true; } }

    internal BatteryNotice(Screen screen, string title, string message, Color tint, int percent) {
      this.title = title;
      this.message = message;
      this.tint = tint;
      this.percent = Math.Max(0, Math.Min(100, percent));
      Text = AppInfo.Name;
      ShowInTaskbar = false;
      FormBorderStyle = FormBorderStyle.None;
      TopMost = true;
      DoubleBuffered = true;
      StartPosition = FormStartPosition.Manual;
      BackColor = Theme.Card;
      ForeColor = Theme.Ink;
      ClientSize = Dpi.S(364, 95);
      using (var path = Outline(0)) Region = new Region(path);
      Rectangle area = screen.WorkingArea;
      Location = new Point(area.Right - Width - Dpi.S(16), area.Bottom - Height - Dpi.S(16));
      closeBounds = Dpi.S(315, 32, 27, 27);
      AccessibleName = title;
      AccessibleDescription = message;
      timer.Tick += (s, e) => Close();
      timer.Start();
    }

    GraphicsPath Outline(int inset) {
      int d = Height - 1 - inset * 2;
      var path = new GraphicsPath();
      path.AddArc(inset, inset, d, d, 90, 180);
      path.AddArc(Width - 1 - inset - d, inset, d, d, 270, 180);
      path.CloseFigure();
      return path;
    }

    protected override void OnMouseMove(MouseEventArgs e) {
      base.OnMouseMove(e);
      bool hover = closeBounds.Contains(e.Location);
      if (hover != closeHover) { closeHover = hover; Cursor = hover ? Cursors.Hand : Cursors.Default; Invalidate(closeBounds); }
    }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); closeHover = false; Invalidate(closeBounds); }
    protected override void OnMouseClick(MouseEventArgs e) { base.OnMouseClick(e); Close(); }

    protected override void OnPaint(PaintEventArgs e) {
      base.OnPaint(e);
      var g = e.Graphics;
      g.SmoothingMode = SmoothingMode.AntiAlias;
      using (var path = Outline(Dpi.S(3)))
      using (var ring = new Pen(Theme.Mix(BackColor, tint, 0.27f), Dpi.S(3f))) g.DrawPath(ring, path);
      using (var pen = new Pen(tint, Dpi.S(2.5f)))
      using (var fill = new SolidBrush(tint)) {
        pen.StartCap = pen.EndCap = LineCap.Round;
        int d = Height - Dpi.S(7);
        float s = Dpi.S(3f);
        g.DrawArc(pen, s, s, d, d, 180, Math.Min(90, 16 + percent * 2));
        if (percent > 35) g.DrawLine(pen, s + d / 2f, s, s + d / 2f + (Width - d - Dpi.S(10)) * (percent - 35) / 65f, s);
        // Battery outline with its fill level.
        g.DrawRoundedRectangle(pen, Dpi.S(21f), Dpi.S(36f), Dpi.S(34f), Dpi.S(21f), Dpi.S(7f));
        g.DrawLine(pen, Dpi.S(59f), Dpi.S(43f), Dpi.S(59f), Dpi.S(50f));
        if (percent > 0) g.FillRectangle(fill, Dpi.S(26), Dpi.S(41), Math.Max(1, Dpi.S(24) * percent / 100), Dpi.S(11));
      }
      const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak;
      TextRenderer.DrawText(g, title, titleFont, Dpi.S(69, 14, 231, 27), tint, flags | TextFormatFlags.VerticalCenter);
      TextRenderer.DrawText(g, message, bodyFont, Dpi.S(69, 43, 231, 39), ForeColor, flags);
      if (closeHover) g.FillRoundedRectangle(Theme.Mix(BackColor, ForeColor, 0.12f), closeBounds, Dpi.S(8));
      Glyphs.Draw(g, Glyph.Close, Rectangle.Inflate(closeBounds, -Dpi.S(4), -Dpi.S(4)), ForeColor);
    }

    protected override void Dispose(bool disposing) {
      if (disposing) { timer.Dispose(); titleFont.Dispose(); bodyFont.Dispose(); }
      base.Dispose(disposing);
    }
  }
}
