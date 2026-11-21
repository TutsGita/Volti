// SPDX-License-Identifier: AGPL-3.0-only
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Volti {
  class RoundedButton : Button {
    internal Color[] Swatches;
    internal bool Selected;

    internal RoundedButton(){
      SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor,true);
      Cursor=Cursors.Hand;
    }

    protected override void OnPaint(PaintEventArgs e){
      if(Width<4||Height<4)return;
      e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
      bool hovered=Enabled&&ClientRectangle.Contains(PointToClient(MousePosition));
      Color fill=MainForm.Mix(MainForm.Card,MainForm.Accent,hovered?0.20f:0.06f);
      bool primary=Equals(Tag,"primary");
      if(primary)fill=hovered?MainForm.Mix(MainForm.Accent,Color.White,0.18f):MainForm.Accent;
      else if(Selected)fill=MainForm.Mix(MainForm.Card,MainForm.Accent,0.23f);
      Color outline=Selected||Focused?MainForm.Accent:MainForm.Mix(fill,MainForm.Ink,0.17f);
      using(var brush=new SolidBrush(fill))
        e.Graphics.FillRoundedRectangle(brush,1,1,Width-3,Height-3,12);
      using(var pen=new Pen(outline))
        e.Graphics.DrawRoundedRectangle(pen,1,1,Width-3,Height-3,12);
      var label=ClientRectangle;
      if(Swatches!=null){
        int width=(Width-24)/Swatches.Length;
        for(int i=0;i<Swatches.Length;i++)
          using(var brush=new SolidBrush(Swatches[i]))
            e.Graphics.FillRoundedRectangle(brush,12+i*width,13,width-4,Height/2-7,8);
        label.Y=Height/2+5;
        label.Height=Height-label.Y-5;
      }
      TextRenderer.DrawText(e.Graphics,Text,Font,label,primary?(fill.GetBrightness()>0.48f?Color.FromArgb(33,21,47):Color.White):ForeColor,
        TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
    }
    protected override void OnMouseEnter(System.EventArgs e){base.OnMouseEnter(e);Invalidate();}
    protected override void OnMouseLeave(System.EventArgs e){base.OnMouseLeave(e);Invalidate();}
  }
}
