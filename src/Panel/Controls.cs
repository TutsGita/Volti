// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace Volti {
  enum ButtonStyle { Primary, Secondary, Subtle, Caption, Close, Tab }

  // Owner-drawn controls read Theme at paint time, so a theme change only needs Invalidate.
  abstract class ThemedControl : Control {
    protected bool Hover, Pressed;
    protected ThemedControl() {
      SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
               ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
      Cursor = Cursors.Hand;
    }
    protected Color ParentBack { get { return Parent == null || Parent.BackColor.A == 0 ? Theme.Surface : Parent.BackColor; } }
    protected bool ShowFocus { get { return Focused && ShowFocusCues; } }
    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); Hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); Hover = Pressed = false; Invalidate(); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { Pressed = true; Invalidate(); } }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); Pressed = false; Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); }
    protected override bool IsInputKey(Keys keyData) { return keyData == Keys.Space || keyData == Keys.Enter || base.IsInputKey(keyData); }
    protected override void OnKeyDown(KeyEventArgs e) {
      base.OnKeyDown(e);
      if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) { e.Handled = true; OnClick(EventArgs.Empty); }
    }
    protected void PaintFocus(Graphics g, Rectangle bounds, float radius) {
      if (ShowFocus) g.DrawRoundedRectangle(Theme.Accent, bounds, radius, Dpi.S(2f));
    }
  }

  sealed class FlatButton : ThemedControl {
    internal ButtonStyle Style = ButtonStyle.Secondary;
    internal Glyph Glyph;
    internal int GlyphAngle;
    internal bool Selected;
    readonly Font tabFont;

    internal FlatButton(string text, Glyph glyph = Glyph.None, ButtonStyle style = ButtonStyle.Secondary) {
      Text = text;
      AccessibleName = text;
      AccessibleRole = AccessibleRole.PushButton;
      Glyph = glyph;
      Style = style;
      Height = Dpi.S(34);
      tabFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
    }

    protected override void Dispose(bool disposing) { if (disposing) tabFont.Dispose(); base.Dispose(disposing); }

    // Width that fits the text and glyph.
    internal int PreferredWidth {
      get {
        int text = Text.Length == 0 ? 0 : TextRenderer.MeasureText(Text, Style == ButtonStyle.Tab ? tabFont : Font).Width;
        int glyph = Glyph == Glyph.None ? 0 : Dpi.S(18) + (text > 0 ? Dpi.S(8) : 0);
        return Math.Max(Height, text + glyph + Dpi.S(text > 0 ? 26 : 10));
      }
    }

    protected override void OnPaint(PaintEventArgs e) {
      var g = e.Graphics;
      g.SmoothingMode = SmoothingMode.AntiAlias;
      g.Clear(ParentBack);
      var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
      float radius = Dpi.S(10f);
      Color back = Color.Empty, ink = Theme.Ink;
      switch (Style) {
        case ButtonStyle.Primary:
          back = Pressed ? Theme.Mix(Theme.Accent, Theme.Surface, 0.25f) : Hover ? Theme.Mix(Theme.Accent, Theme.Ink, 0.12f) : Theme.Accent;
          ink = Theme.OnAccent;
          break;
        case ButtonStyle.Secondary:
          back = Pressed ? Theme.Pressed : Hover ? Theme.Hover : Theme.Mix(Theme.Card, Theme.Ink, 0.06f);
          break;
        case ButtonStyle.Subtle:
          if (Hover || Pressed || Selected) back = Pressed ? Theme.Pressed : Selected ? Theme.Mix(ParentBack, Theme.Accent, 0.22f) : Theme.Mix(ParentBack, Theme.Ink, 0.10f);
          break;
        case ButtonStyle.Caption:
          radius = 0;
          if (Hover) back = Theme.Mix(ParentBack, Theme.Ink, Pressed ? 0.18f : 0.10f);
          break;
        case ButtonStyle.Close:
          radius = 0;
          if (Hover) { back = Pressed ? Color.FromArgb(160, 20, 28) : Color.FromArgb(196, 43, 28); ink = Color.White; }
          break;
        case ButtonStyle.Tab:
          ink = Selected ? Theme.Ink : Hover ? Theme.Mix(Theme.Muted, Theme.Ink, 0.5f) : Theme.Muted;
          break;
      }
      if (!Enabled) ink = Theme.Mix(ParentBack, ink, 0.45f);
      if (!back.IsEmpty) {
        if (radius > 0) g.FillRoundedRectangle(back, bounds, radius);
        else using (var b = new SolidBrush(back)) g.FillRectangle(b, ClientRectangle);
      }
      if (Style == ButtonStyle.Tab && Selected)
        using (var b = new SolidBrush(Theme.Accent)) g.FillRoundedRectangle(b, Dpi.S(10), Height - Dpi.S(3), Width - Dpi.S(20), Dpi.S(3), Dpi.S(3));

      Font font = Style == ButtonStyle.Tab ? tabFont : Font;
      int glyphSize = Style == ButtonStyle.Caption || Style == ButtonStyle.Close ? Dpi.S(16) : Dpi.S(18);
      int textWidth = Text.Length == 0 ? 0 : TextRenderer.MeasureText(Text, font).Width;
      int content = (Glyph == Glyph.None ? 0 : glyphSize + (textWidth > 0 ? Dpi.S(8) : 0)) + textWidth;
      int x = Math.Max(Dpi.S(6), (Width - content) / 2);
      if (Glyph != Glyph.None) {
        Glyphs.Draw(g, Glyph, new Rectangle(x, (Height - glyphSize) / 2, glyphSize, glyphSize), ink, GlyphAngle);
        x += glyphSize + Dpi.S(8);
      }
      if (textWidth > 0)
        TextRenderer.DrawText(g, Text, font, new Rectangle(x, 0, Width - x - Dpi.S(4), Height), ink,
          TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
      PaintFocus(g, Rectangle.Inflate(bounds, -1, -1), radius);
    }
  }

  // An on/off switch. Space, Enter and clicks toggle it.
  sealed class Toggle : ThemedControl {
    bool on;
    internal event EventHandler CheckedChanged;
    internal Toggle(string name) {
      AccessibleName = name;
      AccessibleRole = AccessibleRole.CheckButton;
      Size = Dpi.S(46, 26);
    }
    internal bool Checked {
      get { return on; }
      set { if (on == value) return; on = value; AccessibleDescription = on ? "On" : "Off"; Invalidate(); if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty); }
    }
    // Sets the state without raising CheckedChanged.
    internal void Set(bool value) { on = value; Invalidate(); }
    protected override void OnClick(EventArgs e) { base.OnClick(e); Checked = !Checked; }
    protected override void OnPaint(PaintEventArgs e) {
      var g = e.Graphics;
      g.SmoothingMode = SmoothingMode.AntiAlias;
      g.Clear(ParentBack);
      var track = new Rectangle(1, 1, Width - 3, Height - 3);
      Color fill = on ? Theme.Accent : Theme.Mix(ParentBack, Theme.Ink, Hover ? 0.28f : 0.20f);
      g.FillRoundedRectangle(fill, track, track.Height);
      int knob = track.Height - Dpi.S(8);
      int x = on ? track.Right - knob - Dpi.S(4) : track.Left + Dpi.S(4);
      using (var b = new SolidBrush(on ? Theme.OnAccent : Theme.Ink)) g.FillEllipse(b, x, track.Top + Dpi.S(4), knob, knob);
      PaintFocus(g, track, track.Height);
    }
  }

  // Percentage selector with − and + buttons in five-point steps and a typed value.
  sealed class Stepper : UserControl {
    readonly FlatButton decrease, increase;
    readonly TextBox input;
    int minimum, maximum, current;
    internal event Action<int> Committed;

    internal Stepper(string name) {
      AccessibleName = name;
      Size = Dpi.S(132, 34);
      decrease = new FlatButton("", Glyph.Minimize, ButtonStyle.Secondary) { AccessibleName = L.T("Decrease") };
      increase = new FlatButton("+", Glyph.None, ButtonStyle.Secondary) { AccessibleName = L.T("Increase") };
      input = new TextBox { BorderStyle = BorderStyle.None, TextAlign = HorizontalAlignment.Center, AccessibleName = name };
      decrease.SetBounds(0, 0, Dpi.S(34), Dpi.S(34));
      increase.SetBounds(Width - Dpi.S(34), 0, Dpi.S(34), Dpi.S(34));
      input.SetBounds(Dpi.S(40), Dpi.S(8), Width - Dpi.S(80), Dpi.S(20));
      decrease.Click += (s, e) => Step(-1);
      increase.Click += (s, e) => Step(1);
      input.Leave += (s, e) => CommitInput();
      input.KeyDown += (s, e) => {
        if (e.KeyCode == Keys.Enter) CommitInput();
        else if (e.KeyCode == Keys.Escape) ShowCurrent();
        else if (e.KeyCode == Keys.Up) Step(1);
        else if (e.KeyCode == Keys.Down) Step(-1);
        else return;
        e.SuppressKeyPress = true;
      };
      Controls.AddRange(new Control[] { decrease, input, increase });
    }

    internal void SetRange(int min, int max, int value) {
      minimum = min; maximum = max; current = value;
      ShowCurrent();
      decrease.Enabled = current > minimum;
      increase.Enabled = current < maximum;
    }

    internal int Value { get { return current; } }
    void ShowCurrent() { input.Text = current.ToString(CultureInfo.CurrentCulture) + " %"; }
    void Step(int delta) { Commit(Math.Max(minimum, Math.Min(maximum, current + delta * 5))); }
    void CommitInput() {
      int value;
      if (int.TryParse(input.Text.Replace("%", "").Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out value) && value >= minimum && value <= maximum) Commit(value);
      else ShowCurrent();
    }
    void Commit(int value) {
      if (value != current && Committed != null) Committed(value);
      ShowCurrent();
    }

    internal void ApplyTheme() {
      BackColor = Parent == null ? Theme.Card : Parent.BackColor;
      input.BackColor = BackColor;
      input.ForeColor = Theme.Ink;
      Invalidate(true);
    }
  }

  // Vertical scrolling container with a slim themed scrollbar. Children go into Content.
  sealed class ScrollPanel : Control {
    internal readonly Panel Content = new Panel();
    int offset, contentHeight;
    bool dragging;
    int dragStart, dragOffset;
    readonly int bar = Dpi.S(6);

    internal ScrollPanel() {
      SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
      Controls.Add(Content);
      Content.Location = Point.Empty;
      Content.MouseWheel += (s, e) => ScrollBy(-e.Delta * Dpi.S(48) / 120);
    }

    internal int ContentHeight {
      get { return contentHeight; }
      set { contentHeight = value; Arrange(); }
    }
    internal int ViewWidth { get { return Width - bar - Dpi.S(6); } }

    protected override void OnResize(EventArgs e) { base.OnResize(e); Arrange(); }
    protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); ScrollBy(-e.Delta * Dpi.S(48) / 120); }

    // Unhandled wheel messages from children bubble up to Content.
    int Max { get { return Math.Max(0, contentHeight - Height); } }

    internal void ScrollBy(int delta) {
      int next = Math.Max(0, Math.Min(Max, offset + delta));
      if (next == offset) return;
      offset = next;
      Arrange();
    }

    internal void Reveal(Control control) {
      var top = Content.PointToClient(control.Parent.PointToScreen(control.Location)).Y;
      if (top < offset) ScrollBy(top - offset - Dpi.S(8));
      else if (top + control.Height > offset + Height) ScrollBy(top + control.Height - offset - Height + Dpi.S(8));
    }

    void Arrange() {
      offset = Math.Max(0, Math.Min(Max, offset));
      Content.SetBounds(0, -offset, Math.Max(0, Width - bar - Dpi.S(2)), Math.Max(contentHeight, Height));
      Invalidate();
    }

    Rectangle Thumb {
      get {
        if (contentHeight <= Height || Height <= 0) return Rectangle.Empty;
        int h = Math.Max(Dpi.S(28), Height * Height / contentHeight);
        int y = Max == 0 ? 0 : (Height - h) * offset / Max;
        return new Rectangle(Width - bar - Dpi.S(2), y, bar, h);
      }
    }

    protected override void OnPaint(PaintEventArgs e) {
      e.Graphics.Clear(BackColor);
      var thumb = Thumb;
      if (thumb.IsEmpty) return;
      e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
      e.Graphics.FillRoundedRectangle(Theme.Mix(BackColor, Theme.Ink, dragging ? 0.45f : 0.25f), thumb, bar);
    }

    protected override void OnMouseDown(MouseEventArgs e) {
      base.OnMouseDown(e);
      var thumb = Thumb;
      if (thumb.IsEmpty || e.X < Width - bar - Dpi.S(6)) return;
      if (!thumb.Contains(e.Location)) { ScrollBy(e.Y < thumb.Top ? -Height : Height); return; }
      dragging = true; dragStart = e.Y; dragOffset = offset; Capture = true; Invalidate();
    }
    protected override void OnMouseMove(MouseEventArgs e) {
      base.OnMouseMove(e);
      if (!dragging) return;
      var thumb = Thumb;
      int track = Height - thumb.Height;
      if (track > 0) ScrollBy(dragOffset + (e.Y - dragStart) * Max / track - offset);
    }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); dragging = false; Capture = false; Invalidate(); }
  }
}
