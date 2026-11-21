// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Volti {
  sealed class GearButton : RoundedButton {
    protected override void OnPaint(PaintEventArgs e){
      base.OnPaint(e);
      e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
      float scale=e.Graphics.DpiX/96f;
      float x=ClientSize.Width/2f,y=ClientSize.Height/2f;
      var points=new PointF[32];
      for(int i=0;i<points.Length;i++){
        double angle=(i-1.5)*Math.PI/16;
        float radius=(i%4==1||i%4==2?12f:9.4f)*scale;
        points[i]=new PointF(x+radius*(float)Math.Cos(angle),y+radius*(float)Math.Sin(angle));
      }
      using(var path=new GraphicsPath()){
        path.AddPolygon(points);
        path.AddEllipse(x-4*scale,y-4*scale,8*scale,8*scale);
        using(var brush=new SolidBrush(ForeColor))e.Graphics.FillPath(brush,path);
      }
    }
  }
}
