// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Volti {
  sealed class GlassScrollPanel : Panel {
    readonly Panel content = new Panel();
    readonly ScrollThumb bar;
    int contentHeight, offset, wheelRemainder;

    internal GlassScrollPanel() {
      bar = new ScrollThumb(this);
      Controls.Add(content);
      Controls.Add(bar);
    }

    internal Panel Content {get {return content;}}
    internal int ContentHeight {
      set {contentHeight = value; UpdateLayout();}
    }
    int Maximum {get {return Math.Max(0, contentHeight - ClientSize.Height);}}

    protected override void OnSizeChanged(EventArgs e) {
      base.OnSizeChanged(e);
      if(bar != null) UpdateLayout();
    }

    void UpdateLayout() {
      bar.Bounds = new Rectangle(Math.Max(0, ClientSize.Width - 19), 5, 14, Math.Max(0, ClientSize.Height - 10));
      content.Size = new Size(Math.Max(0, ClientSize.Width - 22), Math.Max(ClientSize.Height, contentHeight));
      bar.Visible = Maximum > 0;
      ScrollTo(offset);
    }

    void ScrollTo(int position) {
      offset = Math.Max(0, Math.Min(Maximum, position));
      content.Top = -offset;
      bar.Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e) {
      base.OnMouseWheel(e);
      if(Maximum == 0) return;
      var handled = e as HandledMouseEventArgs;
      if(handled != null) handled.Handled = true;
      wheelRemainder += e.Delta;
      int notches = wheelRemainder / 120;
      wheelRemainder %= 120;
      int lines = SystemInformation.MouseWheelScrollLines;
      ScrollTo(offset - notches * (lines < 0 ? ClientSize.Height : lines * 20));
    }

    internal void Reveal(Control child) {
      int top = child.Top;
      for(var parent = child.Parent; parent != null && parent != content; parent = parent.Parent)
        top += parent.Top;
      if(top < offset) ScrollTo(top);
      else if(top + child.Height > offset + ClientSize.Height)
        ScrollTo(top + child.Height - ClientSize.Height);
    }

    sealed class ScrollThumb : Control {
      readonly GlassScrollPanel viewport;
      int dragStart, startOffset;
      bool hover, dragging;
      internal ScrollThumb(GlassScrollPanel viewport) {
        this.viewport = viewport;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;
        AccessibleName = "Scroll";
      }

      int ThumbHeight {get {return Math.Min(Height, Math.Max(28, Height * viewport.ClientSize.Height / Math.Max(1, viewport.contentHeight)));}}
      int Travel {get {return Math.Max(0, Height - ThumbHeight);}}
      int ThumbTop {get {return (int)((long)Travel * viewport.offset / Math.Max(1, viewport.Maximum));}}

      protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);
        if(viewport.Maximum == 0 || Height < 8) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        bool highlighted = hover || Capture || Focused;
        Color track = MainForm.Mix(MainForm.Surface, MainForm.Ink, 0.08f);
        Color tint = MainForm.Mix(MainForm.Surface, MainForm.Ink, highlighted ? 0.55f : 0.36f);
        using(var brush = new SolidBrush(track))
          e.Graphics.FillRoundedRectangle(brush, 3, 0, 8, Height - 1, 8);
        var thumb = new Rectangle(3, ThumbTop, 8, ThumbHeight - 1);
        using(var gloss = new LinearGradientBrush(thumb, MainForm.Mix(tint, MainForm.Card, 0.35f), tint, LinearGradientMode.Horizontal))
          e.Graphics.FillRoundedRectangle(gloss, thumb.X, thumb.Y, thumb.Width, thumb.Height, thumb.Width);
      }

      protected override void OnMouseEnter(EventArgs e) {base.OnMouseEnter(e);hover = true;Invalidate();}
      protected override void OnMouseLeave(EventArgs e) {base.OnMouseLeave(e);hover = false;Invalidate();}
      protected override void OnGotFocus(EventArgs e) {base.OnGotFocus(e);Invalidate();}
      protected override void OnLostFocus(EventArgs e) {base.OnLostFocus(e);Invalidate();}
      protected override void OnMouseCaptureChanged(EventArgs e) {base.OnMouseCaptureChanged(e);if(!Capture)dragging=false;Invalidate();}
      protected override void OnMouseDown(MouseEventArgs e) {
        base.OnMouseDown(e);
        if(e.Button != MouseButtons.Left) return;
        Focus();
        if(e.Y < ThumbTop || e.Y >= ThumbTop + ThumbHeight) {
          viewport.ScrollTo(viewport.offset + (e.Y < ThumbTop ? -1 : 1) * viewport.ClientSize.Height);
          return;
        }
        dragStart = e.Y;
        startOffset = viewport.offset;
        dragging = true;
        Capture = true;
      }
      protected override void OnMouseMove(MouseEventArgs e) {
        base.OnMouseMove(e);
        if(dragging && Capture && Travel > 0)
          viewport.ScrollTo(startOffset + (int)((long)(e.Y - dragStart) * viewport.Maximum / Travel));
      }
      protected override void OnMouseUp(MouseEventArgs e) {
        base.OnMouseUp(e);
        if(e.Button == MouseButtons.Left) Capture = false;
      }
      protected override bool IsInputKey(Keys key) {
        switch(key & Keys.KeyCode) {
          case Keys.Up: case Keys.Down: case Keys.PageUp: case Keys.PageDown: case Keys.Home: case Keys.End: return true;
          default: return base.IsInputKey(key);
        }
      }
      protected override void OnKeyDown(KeyEventArgs e) {
        int position = viewport.offset;
        switch(e.KeyCode) {
          case Keys.Up: position -= 20; break;
          case Keys.Down: position += 20; break;
          case Keys.PageUp: position -= viewport.ClientSize.Height; break;
          case Keys.PageDown: position += viewport.ClientSize.Height; break;
          case Keys.Home: position = 0; break;
          case Keys.End: position = viewport.Maximum; break;
          default: base.OnKeyDown(e); return;
        }
        viewport.ScrollTo(position);
        e.Handled = true;
      }
    }
  }
}
