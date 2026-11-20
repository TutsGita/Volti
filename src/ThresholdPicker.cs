// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Volti {
  sealed class ThresholdPicker : Panel {
    readonly TextBox input;
    readonly Button arrow;
    readonly ContextMenuStrip choices;
    Color fill;
    int minimum, maximum, current;
    internal event Action<int> Committed;

    internal ThresholdPicker(int left, string name) {
      SetStyle(ControlStyles.SupportsTransparentBackColor,true);
      SetBounds(left,98,150,32);
      input=new TextBox{BorderStyle=BorderStyle.None,Left=11,Top=7,Width=80,AccessibleName=name};
      arrow=new Button{Text="⌄",Left=114,Top=3,Width=31,Height=26,FlatStyle=FlatStyle.Flat,AccessibleName=name,Cursor=Cursors.Hand};
      arrow.FlatAppearance.BorderSize=0;
      choices=new ContextMenuStrip{Renderer=new BatteryMenuRenderer(),ShowImageMargin=false};
      Controls.Add(input);Controls.Add(arrow);
      arrow.Click+=(s,e)=>ShowChoices();
      input.Leave+=(s,e)=>CommitInput();
      input.KeyDown+=(s,e)=>{
        if(e.KeyCode==Keys.Enter){CommitInput();e.SuppressKeyPress=true;}
        if(e.KeyCode==Keys.Escape){ShowCurrent();e.SuppressKeyPress=true;}
        if(e.KeyCode==Keys.Down&&e.Alt){ShowChoices();e.SuppressKeyPress=true;}
      };
    }

    internal void SetRange(int min,int max,int value) {
      minimum=min;maximum=max;
      if(current!=value||input.Text.Length==0){current=value;ShowCurrent();}
    }

    void ShowCurrent(){input.Text=current.ToString(CultureInfo.CurrentCulture);}
    void CommitInput(){
      int value;
      if(int.TryParse(input.Text.Trim().TrimEnd('%'),out value)&&value>=minimum&&value<=maximum)Commit(value);
      else ShowCurrent();
    }
    void Commit(int value){
      if(value!=current&&Committed!=null)Committed(value);
      ShowCurrent();
    }
    void ShowChoices(){
      while(choices.Items.Count!=0)choices.Items[0].Dispose();
      for(int value=10;value<=100;value+=10){
        if(value<minimum||value>maximum)continue;
        int selected=value;
        var item=new ToolStripMenuItem(value+"%"){Checked=value==current,ForeColor=MainForm.Ink};
        item.Click+=(s,e)=>Commit(selected);
        choices.Items.Add(item);
      }
      if(choices.Items.Count==0)choices.Items.Add(new ToolStripMenuItem(L.T("No presets in range")){Enabled=false,ForeColor=MainForm.Ink});
      choices.Show(this,new Point(0,Height));
    }
    internal void ApplyPalette(){
      fill=MainForm.Mix(MainForm.Surface,MainForm.Card,0.55f);
      BackColor=Color.Transparent;
      input.BackColor=arrow.BackColor=fill;
      input.ForeColor=arrow.ForeColor=choices.ForeColor=MainForm.Ink;
      choices.BackColor=MainForm.Surface;
      arrow.FlatAppearance.MouseOverBackColor=MainForm.Mix(fill,MainForm.Accent,0.23f);
      arrow.FlatAppearance.MouseDownBackColor=MainForm.Mix(fill,MainForm.Accent,0.36f);
    }
    protected override void OnPaint(PaintEventArgs e){
      base.OnPaint(e);
      e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
      using(var brush=new SolidBrush(fill))
        e.Graphics.FillRoundedRectangle(brush,1,1,Width-3,Height-3,14);
      using(var pen=new Pen(MainForm.Mix(MainForm.Card,MainForm.Ink,0.45f)))
        e.Graphics.DrawRoundedRectangle(pen,1,1,Width-3,Height-3,14);
      using(var pen=new Pen(MainForm.Mix(MainForm.Card,MainForm.Ink,0.24f)))
        e.Graphics.DrawLine(pen,111,8,111,Height-9);
      TextRenderer.DrawText(e.Graphics,"%",Font,new Point(94,7),MainForm.Ink);
    }
    protected override void Dispose(bool disposing){if(disposing)choices.Dispose();base.Dispose(disposing);}
  }
}
