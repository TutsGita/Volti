// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Volti {
  // A theme preview: its four colours, the battery gradient and the name.
  sealed class ThemeTile : ThemedControl {
    internal readonly ThemeInfo Info;
    readonly Font nameFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
    internal ThemeTile(ThemeInfo info) {
      Info = info;
      AccessibleName = L.T(info.Label);
      AccessibleRole = AccessibleRole.RadioButton;
      Size = Dpi.S(128, 86);
    }
    internal bool Selected { get { return Theme.Current == Info; } }
    protected override void OnPaint(PaintEventArgs e) {
      var g = e.Graphics;
      g.SmoothingMode = SmoothingMode.AntiAlias;
      g.Clear(ParentBack);
      var card = new Rectangle(1, 1, Width - 3, Height - 3);
      float radius = Dpi.S(14f);
      g.FillRoundedRectangle(Info.Surface, card, radius);
      var swatchArea = new Rectangle(card.X + Dpi.S(10), card.Y + Dpi.S(10), card.Width - Dpi.S(20), Dpi.S(30));
      g.FillRoundedRectangle(Info.Card, swatchArea, Dpi.S(10));
      int d = Dpi.S(16), x = swatchArea.X + Dpi.S(8);
      foreach (var color in new[] { Info.Accent, Info.Ink }) {
        using (var b = new SolidBrush(color)) g.FillEllipse(b, x, swatchArea.Y + (swatchArea.Height - d) / 2, d, d);
        x += d + Dpi.S(6);
      }
      var gradient = new Rectangle(x + Dpi.S(4), swatchArea.Y + swatchArea.Height / 2 - Dpi.S(3), swatchArea.Right - x - Dpi.S(14), Dpi.S(6));
      if (gradient.Width > 4)
        using (var brush = new LinearGradientBrush(gradient, Info.Low, Info.High, 0f)) {
          var blend = new ColorBlend { Colors = new[] { Info.Low, Info.Medium, Info.High }, Positions = new[] { 0f, 0.5f, 1f } };
          brush.InterpolationColors = blend;
          g.FillRoundedRectangle(brush, gradient.X, gradient.Y, gradient.Width, gradient.Height, gradient.Height);
        }
      TextRenderer.DrawText(g, L.T(Info.Label), nameFont, new Rectangle(card.X + Dpi.S(12), card.Bottom - Dpi.S(36), card.Width - Dpi.S(34), Dpi.S(30)),
        Info.Ink, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
      Color outline = Selected ? Theme.Accent : Hover ? Theme.Mix(ParentBack, Theme.Ink, 0.35f) : Theme.Mix(ParentBack, Theme.Ink, 0.12f);
      g.DrawRoundedRectangle(outline, card, radius, Selected || ShowFocus ? Dpi.S(2.5f) : 1f);
      if (Selected) {
        var badge = new Rectangle(card.Right - Dpi.S(26), card.Bottom - Dpi.S(28), Dpi.S(18), Dpi.S(18));
        using (var b = new SolidBrush(Theme.Accent)) g.FillEllipse(b, badge);
        using (var pen = new Pen(Theme.OnAccent, Dpi.S(2f)))
          g.DrawLines(pen, new[] { new PointF(badge.X + badge.Width * 0.28f, badge.Y + badge.Height * 0.52f),
            new PointF(badge.X + badge.Width * 0.44f, badge.Y + badge.Height * 0.68f), new PointF(badge.X + badge.Width * 0.74f, badge.Y + badge.Height * 0.34f) });
      }
    }
    protected override void Dispose(bool disposing) { if (disposing) nameFont.Dispose(); base.Dispose(disposing); }
  }

  sealed class ThemesPage : Control {
    readonly ScrollPanel scroll = new ScrollPanel();
    readonly List<ThemeTile> tiles = new List<ThemeTile>();
    internal event EventHandler ThemeChanged;

    internal ThemesPage() {
      Controls.Add(scroll);
      foreach (var info in Theme.All) {
        var tile = new ThemeTile(info);
        tile.Click += (s, e) => {
          if (Theme.Current == tile.Info) return;
          Settings.Theme = tile.Info.Name;
          Theme.Apply(tile.Info.Name);
          if (ThemeChanged != null) ThemeChanged(this, EventArgs.Empty);
        };
        tiles.Add(tile);
        scroll.Content.Controls.Add(tile);
      }
    }

    protected override void OnResize(EventArgs e) { base.OnResize(e); Arrange(); }

    void Arrange() {
      scroll.SetBounds(Dpi.S(14), 0, ClientSize.Width - Dpi.S(14), ClientSize.Height);
      int width = Math.Max(Dpi.S(140), ClientSize.Width - Dpi.S(40)), gap = Dpi.S(10);
      int columns = Math.Max(1, (width + gap) / (Dpi.S(120) + gap));
      int tileWidth = (width - gap * (columns - 1)) / columns;
      for (int i = 0; i < tiles.Count; i++) {
        tiles[i].SetBounds(Dpi.S(6) + (i % columns) * (tileWidth + gap), Dpi.S(8) + (i / columns) * (tiles[i].Height + gap), tileWidth, tiles[i].Height);
      }
      int rows = (tiles.Count + columns - 1) / columns;
      scroll.ContentHeight = Dpi.S(16) + rows * (Dpi.S(86) + gap);
    }

    internal void ApplyTheme() {
      BackColor = scroll.BackColor = scroll.Content.BackColor = Theme.Surface;
      Invalidate(true);
    }
  }
}
