// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Volti {
  // The card shown while the pointer rests on a device's tray icon.
  sealed class TrayPreview : Form {
    readonly DeviceView device;
    readonly string name;
    readonly Color tint;
    readonly Font heading = new Font("Segoe UI", 10f, FontStyle.Bold);
    readonly Font level = new Font("Segoe UI", 18f, FontStyle.Bold);
    readonly Font detail = new Font("Segoe UI", 9f);

    protected override bool ShowWithoutActivation { get { return true; } }
    protected override CreateParams CreateParams {
      get { var p = base.CreateParams; p.ExStyle |= 0x08000020; return p; } // WS_EX_NOACTIVATE | WS_EX_TRANSPARENT
    }

    internal TrayPreview(Point anchor, DeviceView device) {
      this.device = device;
      name = device.DisplayName;
      tint = device.HasPercent ? Theme.Battery(device.Percent) : Theme.Accent;
      Text = name;
      DoubleBuffered = true;
      ShowInTaskbar = false;
      FormBorderStyle = FormBorderStyle.None;
      StartPosition = FormStartPosition.Manual;
      TopMost = true;
      BackColor = Theme.Card;
      ForeColor = Theme.Ink;
      ClientSize = Dpi.S(300, 114);
      Rectangle area = Screen.FromPoint(anchor).WorkingArea;
      int x = Math.Max(area.Left, Math.Min(area.Right - Width, anchor.X - Width / 2));
      int y = anchor.Y < area.Top ? area.Top + 8 : Math.Max(area.Top, Math.Min(area.Bottom - Height - 8, anchor.Y - Height - 14));
      Location = new Point(x, y);
      using (var path = GraphicsExtensions.RoundedPath(0, 0, Width, Height, Dpi.S(24))) Region = new Region(path);
    }

    protected override void OnPaint(PaintEventArgs e) {
      base.OnPaint(e);
      var g = e.Graphics;
      g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
      const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
      using (var dot = new SolidBrush(tint)) g.FillEllipse(dot, Dpi.S(15), Dpi.S(18), Dpi.S(7), Dpi.S(7));
      TextRenderer.DrawText(g, name, heading, Dpi.S(28, 10, 256, 22), ForeColor, flags);
      string value = device.DisplayLevel + (device.Charging ? "  " + Battery.Charging : "");
      TextRenderer.DrawText(g, value, level, Dpi.S(13, 35, 274, 34), tint, flags);
      var track = Dpi.S(15, 77, 270, 4);
      using (var b = new SolidBrush(Theme.Mix(BackColor, ForeColor, 0.2f))) g.FillRectangle(b, track);
      if (device.HasPercent)
        using (var b = new SolidBrush(tint)) g.FillRectangle(b, track.X, track.Y, track.Width * device.Percent / 100, track.Height);
      Color muted = Theme.Mix(BackColor, ForeColor, 0.8f);
      TextRenderer.DrawText(g, device.TransportLabel, detail, Dpi.S(13, 88, 135, 17), muted, flags);
      string right = device.RemainingMinutes >= 0 ? Battery.Remaining(device.RemainingMinutes) : device.HasPercent ? L.T("Battery available") : "";
      TextRenderer.DrawText(g, right, detail, Dpi.S(145, 88, 141, 17), muted, flags | TextFormatFlags.Right);
    }

    protected override void Dispose(bool disposing) {
      if (disposing) { heading.Dispose(); level.Dispose(); detail.Dispose(); }
      base.Dispose(disposing);
    }
  }
}
