// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Volti {
  static class ControllerBattery {
    [StructLayout(LayoutKind.Sequential)]
    struct BatteryInfo { internal byte Type, Level; }
    [DllImport("xinput1_4.dll")]
    static extern uint XInputGetBatteryInformation(uint slot, byte device, out BatteryInfo battery);

    internal static string Level(byte type, byte level) {
      if(type!=2 && type!=3)return null; // Alkaline or NiMH. Wired and unknown types carry no usable level.
      switch(level){
        case 0:return "Empty";
        case 1:return "Low";
        case 2:return "Medium";
        case 3:return "Full";
        default:return null;
      }
    }
    internal static string Scan() {
      var rows=new List<string>();
      for(uint slot=0;slot<4;slot++){
        BatteryInfo info;
        if(XInputGetBatteryInformation(slot,0,out info)!=0)continue;
        var level=Level(info.Type,info.Level);
        if(level==null)continue;
        string number=(slot+1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        rows.Add(Convert.ToBase64String(Encoding.UTF8.GetBytes("xinput:"+number))+"\t"+
          Convert.ToBase64String(Encoding.UTF8.GetBytes("Controller "+number))+"\t"+level);
      }
      return string.Join("\n",rows.ToArray());
    }
  }
}
