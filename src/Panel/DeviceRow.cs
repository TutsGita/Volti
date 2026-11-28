// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Volti {
  // One device card: icon, name, level, battery bar and details. Clicking it reveals its options.
  sealed class DeviceRow : Control {
    internal static readonly int CollapsedHeight = Dpi.S(82);
    internal DeviceView Device { get; private set; }
    internal bool Expanded { get; private set; }
    internal event EventHandler LayoutChanged;
    internal event EventHandler PreferencesChanged;

    readonly Font nameFont = new Font("Segoe UI", 10.5f, FontStyle.Bold);
    readonly Font levelFont = new Font("Segoe UI", 15f, FontStyle.Bold);
    readonly Font detailFont = new Font("Segoe UI", 9f);
    readonly Panel options = new Panel { Visible = false };
    readonly Toggle trayToggle;
    readonly Label trayLabel;
    readonly FlatButton rename, restore;
    readonly TextBox nameEditor = new TextBox { MaxLength = 64, Visible = false, BorderStyle = BorderStyle.FixedSingle };
    Label lowLabel, criticalLabel;
    Stepper low, critical;
    string displayName = "";
    bool hover, pencilHover;
    Rectangle pencilBounds;

    internal DeviceRow(DeviceView device) {
      SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
               ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
      TabStop = true;
      Cursor = Cursors.Hand;
      AccessibleRole = AccessibleRole.ListItem;

      trayLabel = new Label { Text = L.T("Show in tray"), AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
      trayToggle = new Toggle(L.T("Show in tray"));
      trayToggle.Set(Settings.IsTrayVisible(device.Id));
      trayToggle.CheckedChanged += (s, e) => { Settings.SetTrayVisible(Device.Id, trayToggle.Checked); Changed(); };
      rename = new FlatButton(L.T("Rename"), Glyph.Pencil, ButtonStyle.Secondary);
      rename.Click += (s, e) => BeginRename();
      restore = new FlatButton(L.T("Restore device name"), Glyph.Refresh, ButtonStyle.Subtle);
      restore.Click += (s, e) => { Settings.SetAlias(Device.Id, ""); SetDevice(Device); Changed(); };
      options.Controls.AddRange(new Control[] { trayLabel, trayToggle, rename, restore });
      if (device.HasAlerts) AddAlerts(device.Slot);
      Controls.Add(options);

      nameEditor.KeyDown += (s, e) => {
        if (e.KeyCode == Keys.Enter) { EndRename(true); e.SuppressKeyPress = true; }
        else if (e.KeyCode == Keys.Escape) { EndRename(false); e.SuppressKeyPress = true; }
      };
      nameEditor.Leave += (s, e) => EndRename(true);
      Controls.Add(nameEditor);
      Height = CollapsedHeight;
      SetDevice(device);
    }

    void AddAlerts(int slot) {
      lowLabel = new Label { Text = L.T("Low battery"), AutoEllipsis = true };
      criticalLabel = new Label { Text = L.T("Critical battery"), AutoEllipsis = true };
      low = new Stepper(L.T("Low battery"));
      critical = new Stepper(L.T("Critical battery"));
      Action refresh = () => {
        int lowLimit, criticalLimit;
        Settings.LoadLimits(slot, out lowLimit, out criticalLimit);
        low.SetRange(criticalLimit + 1, 100, lowLimit);
        critical.SetRange(1, lowLimit - 1, criticalLimit);
      };
      refresh();
      low.Committed += value => { Settings.SaveLimit(slot, "Low", value); refresh(); Changed(); };
      critical.Committed += value => { Settings.SaveLimit(slot, "Critical", value); refresh(); Changed(); };
      options.Controls.AddRange(new Control[] { lowLabel, criticalLabel, low, critical });
    }

    void Changed() { if (PreferencesChanged != null) PreferencesChanged(this, EventArgs.Empty); }

    internal void SetDevice(DeviceView device) {
      Device = device;
      displayName = device.DisplayName;
      // A tray icon only exists once the device reports a level.
      bool trayCapable = device.Slot >= 0 || !device.Unavailable;
      trayLabel.Visible = trayToggle.Visible = trayCapable;
      restore.Visible = Settings.Alias(device.Id).Length > 0;
      AccessibleName = displayName + ", " + device.DisplayLevel;
      AccessibleDescription = Detail();
      ArrangeOptions();
      Invalidate();
    }

    string Detail() {
      if (Device.Unavailable) return L.T("Battery reading unavailable");
      string text = Device.TransportLabel;
      if (Device.ObservedTime.Length > 0) text += "  ·  " + Device.ObservedTime;
      if (Device.Charging) text += "  ·  " + L.T("Charging");
      else if (Device.Learning) text += "  ·  " + L.T("Learning battery life");
      else if (Device.RemainingMinutes >= 0) text += "  ·  " + Battery.Remaining(Device.RemainingMinutes);
      return text;
    }

    internal void ToggleOptions() {
      Expanded = !Expanded;
      options.Visible = Expanded;
      ArrangeOptions();
      if (LayoutChanged != null) LayoutChanged(this, EventArgs.Empty);
      Invalidate();
    }

    void ArrangeOptions() {
      int pad = Dpi.S(16), y = Dpi.S(4), width = Math.Max(Dpi.S(200), Width - pad * 2);
      if (trayToggle.Visible) {
        trayToggle.Location = new Point(width - trayToggle.Width, y + Dpi.S(4));
        trayLabel.SetBounds(0, y, width - trayToggle.Width - Dpi.S(8), Dpi.S(34));
        y += Dpi.S(40);
      }
      if (low != null) {
        int column = (width - Dpi.S(12)) / 2;
        lowLabel.SetBounds(0, y, column, Dpi.S(22));
        criticalLabel.SetBounds(column + Dpi.S(12), y, column, Dpi.S(22));
        low.Location = new Point(0, y + Dpi.S(24));
        critical.Location = new Point(column + Dpi.S(12), y + Dpi.S(24));
        y += Dpi.S(66);
      }
      rename.SetBounds(0, y, rename.PreferredWidth, Dpi.S(32));
      restore.SetBounds(rename.Right + Dpi.S(8), y, restore.PreferredWidth, Dpi.S(32));
      y += Dpi.S(44);
      options.SetBounds(pad, CollapsedHeight, width, y);
      Height = Expanded ? CollapsedHeight + y : CollapsedHeight;
    }

    protected override void OnResize(EventArgs e) { base.OnResize(e); if (trayToggle != null) ArrangeOptions(); }

    internal void BeginRename() {
      nameEditor.Font = nameFont;
      nameEditor.BackColor = Theme.Raised;
      nameEditor.ForeColor = Theme.Ink;
      nameEditor.Text = displayName;
      nameEditor.SetBounds(Dpi.S(66), Dpi.S(10), Math.Max(Dpi.S(120), Width - Dpi.S(66) - Dpi.S(130)), Dpi.S(26));
      nameEditor.Visible = true;
      nameEditor.Focus();
      nameEditor.SelectAll();
    }

    void EndRename(bool save) {
      if (!nameEditor.Visible) return;
      nameEditor.Visible = false;
      if (save && nameEditor.Text.Trim() != displayName) {
        // Saving the original name, or an empty one, removes the alias.
        string text = nameEditor.Text.Trim();
        Settings.SetAlias(Device.Id, text == Device.Name ? "" : text);
        SetDevice(Device);
        Changed();
      }
      Focus();
    }

    internal void ApplyTheme() {
      BackColor = Theme.Card;
      ForeColor = Theme.Ink;
      options.BackColor = Theme.Card;
      foreach (Control child in options.Controls) {
        child.ForeColor = Theme.Ink;
        child.BackColor = Theme.Card;
        var stepper = child as Stepper;
        if (stepper != null) stepper.ApplyTheme();
      }
      Invalidate(true);
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = pencilHover = false; Invalidate(); }
    protected override void OnMouseMove(MouseEventArgs e) {
      base.OnMouseMove(e);
      bool over = pencilBounds.Contains(e.Location);
      if (over != pencilHover) { pencilHover = over; Invalidate(); }
    }
    protected override void OnMouseClick(MouseEventArgs e) {
      base.OnMouseClick(e);
      Focus();
      if (e.Button != MouseButtons.Left || e.Y > CollapsedHeight) return;
      if (pencilBounds.Contains(e.Location)) BeginRename(); else ToggleOptions();
    }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    protected override bool IsInputKey(Keys keyData) { return keyData == Keys.Space || keyData == Keys.Enter || base.IsInputKey(keyData); }
    protected override void OnKeyDown(KeyEventArgs e) {
      base.OnKeyDown(e);
      if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) { ToggleOptions(); e.Handled = true; }
      else if (e.KeyCode == Keys.F2) { BeginRename(); e.Handled = true; }
    }

    Color Tint { get { return Device.HasPercent ? Theme.Battery(Device.Percent) : Theme.Accent; } }

    protected override void OnPaint(PaintEventArgs e) {
      var g = e.Graphics;
      g.SmoothingMode = SmoothingMode.AntiAlias;
      Color back = Parent == null ? Theme.Surface : Parent.BackColor;
      g.Clear(back);
      var card = new Rectangle(0, 0, Width - 1, Height - 1);
      float radius = Dpi.S(14f);
      g.FillRoundedRectangle(Theme.Card, card, radius);
      g.DrawRoundedRectangle(Focused && ShowFocusCues ? Theme.Accent : hover ? Theme.Mix(Theme.Card, Theme.Ink, 0.22f) : Theme.Line, card, radius, Focused && ShowFocusCues ? Dpi.S(2f) : 1f);

      Color tint = Tint;
      int pad = Dpi.S(14);
      var iconBox = new Rectangle(pad, pad, Dpi.S(42), Dpi.S(42));
      g.FillRoundedRectangle(Theme.Mix(Theme.Card, tint, 0.18f), iconBox, Dpi.S(12));
      DeviceArt.Draw(g, Device.Kind, tint, Rectangle.Inflate(iconBox, -Dpi.S(9), -Dpi.S(9)));

      // Level on the right, with a bolt while charging.
      string level = Device.DisplayLevel;
      Font font = Device.HasPercent || Device.Unavailable ? levelFont : nameFont;
      int levelWidth = TextRenderer.MeasureText(g, level, font).Width;
      var levelBounds = new Rectangle(Width - pad - levelWidth, Dpi.S(8), levelWidth, Dpi.S(34));
      TextRenderer.DrawText(g, level, font, levelBounds, Device.HasPercent ? tint : Theme.Muted,
        TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
      int right = levelBounds.Left - Dpi.S(6);
      if (Device.Charging) {
        Glyphs.Draw(g, Glyph.Bolt, new Rectangle(right - Dpi.S(18), Dpi.S(16), Dpi.S(18), Dpi.S(18)), tint);
        right -= Dpi.S(22);
      }

      // Name and the pencil that appears on hover.
      int textLeft = iconBox.Right + Dpi.S(12);
      int nameMax = Math.Max(Dpi.S(40), right - textLeft - Dpi.S(30));
      int nameWidth = Math.Min(nameMax, TextRenderer.MeasureText(g, displayName, nameFont).Width);
      TextRenderer.DrawText(g, displayName, nameFont, new Rectangle(textLeft, Dpi.S(12), nameWidth, Dpi.S(24)), Theme.Ink,
        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
      pencilBounds = new Rectangle(textLeft + nameWidth + Dpi.S(4), Dpi.S(12), Dpi.S(24), Dpi.S(24));
      if (hover || Focused) {
        if (pencilHover) g.FillRoundedRectangle(Theme.Mix(Theme.Card, Theme.Ink, 0.12f), pencilBounds, Dpi.S(6));
        Glyphs.Draw(g, Glyph.Pencil, Rectangle.Inflate(pencilBounds, -Dpi.S(5), -Dpi.S(5)), pencilHover ? Theme.Ink : Theme.Muted);
      } else pencilBounds = Rectangle.Empty;

      // Details: connection, last reading time and the remaining-time estimate.
      int detailTop = Dpi.S(37);
      Glyph transport = Device.Learning && !Device.Charging ? Glyph.Learning : Glyphs.For(Device.Transport);
      Glyphs.Draw(g, transport, new Rectangle(textLeft, detailTop + Dpi.S(2), Dpi.S(15), Dpi.S(15)), Theme.Muted);
      TextRenderer.DrawText(g, Detail(), detailFont, new Rectangle(textLeft + Dpi.S(20), detailTop, Width - textLeft - Dpi.S(20) - pad - Dpi.S(22), Dpi.S(20)),
        Theme.Muted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
      var chevron = new Rectangle(Width - pad - Dpi.S(16), detailTop + Dpi.S(2), Dpi.S(16), Dpi.S(16));
      Glyphs.Draw(g, Glyph.Chevron, chevron, Theme.Muted, Expanded ? 180 : 0);

      // Battery bar.
      var track = new Rectangle(textLeft, Dpi.S(64), Width - textLeft - pad, Dpi.S(6));
      if (Device.HasPercent) {
        g.FillRoundedRectangle(Theme.Line, track, track.Height);
        if (Device.Percent > 0) {
          var fill = new Rectangle(track.X, track.Y, Math.Max(track.Height, track.Width * Device.Percent / 100), track.Height);
          g.FillRoundedRectangle(tint, fill, track.Height);
        }
      }
      if (Expanded)
        using (var pen = new Pen(Theme.Line)) g.DrawLine(pen, pad, CollapsedHeight - 1, Width - pad, CollapsedHeight - 1);
    }

    protected override void Dispose(bool disposing) {
      if (disposing) { nameFont.Dispose(); levelFont.Dispose(); detailFont.Dispose(); }
      base.Dispose(disposing);
    }
  }
}
