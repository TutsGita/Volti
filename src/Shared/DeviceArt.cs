// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;

namespace Volti {
  enum DeviceKind { Battery, Mouse, Headset, Keyboard, Controller, Speaker, Earbuds, Watch }

  static class DeviceArt {
    internal static DeviceKind Kind(string id,string name){
      if(id.StartsWith("xinput:",StringComparison.OrdinalIgnoreCase))return DeviceKind.Controller;
      if(Has(name,@"\b(keyboard|teclado|keychron|huntsman|blackwidow)\b"))return DeviceKind.Keyboard;
      if(Has(name,@"\b(earbuds?|earphones?|airpods?|buds\w*)\b"))return DeviceKind.Earbuds;
      if(Has(name,@"\b(headphones?|headset|fone|stinger|quantum|cloud)\b"))return DeviceKind.Headset;
      if(Has(name,@"\b(mouse|trackball|pulsefire|rapoo|deathadder|basilisk|cobra|viper)\b"))return DeviceKind.Mouse;
      if(Has(name,@"\b(gamepad|controller|dualsense|dualshock|controle)\b"))return DeviceKind.Controller;
      if(Has(name,@"\b(speaker|soundbar|flip|boombox|soundlink)\b"))return DeviceKind.Speaker;
      return Has(name,@"\b(watch|smartwatch|relógio)\b")?DeviceKind.Watch:DeviceKind.Battery;
    }

    static bool Has(string name,string pattern){
      return Regex.IsMatch(name,pattern,RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    }

    internal static void Draw(Graphics g,DeviceKind kind,Color color,Rectangle bounds){
      var state=g.Save();
      try{
        g.SmoothingMode=SmoothingMode.AntiAlias;
        g.TranslateTransform(bounds.X,bounds.Y);
        g.ScaleTransform(bounds.Width/32f,bounds.Height/32f);
        using(var pen=new Pen(color,2.4f)){
          pen.StartCap=pen.EndCap=LineCap.Round;
          pen.LineJoin=LineJoin.Round;
          DrawDevice(g,pen,kind);
        }
      }finally{g.Restore(state);}
    }

    static void DrawDevice(Graphics g,Pen pen,DeviceKind kind){
      switch(kind){
        case DeviceKind.Headset:
          g.DrawArc(pen,4,4,24,24,180,180);
          g.DrawRoundedRectangle(pen,4,16,6,10,4);g.DrawRoundedRectangle(pen,22,16,6,10,4);
          break;
        case DeviceKind.Mouse:
          g.DrawRoundedRectangle(pen,7,3,18,26,14);g.DrawLine(pen,16,8,16,13);
          break;
        case DeviceKind.Keyboard:
          g.DrawRoundedRectangle(pen,3,7,26,18,5);
          for(int x=8;x<=24;x+=4){g.DrawLine(pen,x,12,x,12.5f);g.DrawLine(pen,x,16,x,16.5f);}
          g.DrawLine(pen,10,21,22,21);
          break;
        case DeviceKind.Controller:
          g.DrawRoundedRectangle(pen,3,9,26,15,9);
          g.DrawLine(pen,8,16,16,16);g.DrawLine(pen,12,12,12,20);
          g.DrawEllipse(pen,20,13,2,2);g.DrawEllipse(pen,24,17,2,2);
          break;
        case DeviceKind.Speaker:
          g.DrawRoundedRectangle(pen,7,3,18,26,5);
          g.DrawEllipse(pen,14,7,4,4);g.DrawEllipse(pen,11,15,10,10);
          break;
        case DeviceKind.Earbuds:
          g.DrawRoundedRectangle(pen,5,7,8,8,6);g.DrawRoundedRectangle(pen,19,7,8,8,6);
          g.DrawLine(pen,11,15,11,24);g.DrawLine(pen,21,15,21,24);
          break;
        case DeviceKind.Watch:
          g.DrawRoundedRectangle(pen,8,7,16,18,7);
          g.DrawLine(pen,12,3,20,3);g.DrawLine(pen,12,29,20,29);
          g.DrawLines(pen,new[]{new Point(16,11),new Point(16,16),new Point(19,18)});
          break;
        default:
          g.DrawRoundedRectangle(pen,3,9,24,14,5);g.DrawLine(pen,30,14,30,18);
          g.DrawLine(pen,8,16,14,16);
          break;
      }
    }
  }
}
